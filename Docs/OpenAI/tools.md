# Custom Tools Guide

Custom tools allow the AI to perform actions in your Unity scene. The AI decides when to call your tools based on the conversation.

## 30-Second Checklist

- [ ] Create C# script with `public async Task<JObject> ToolName(JObject args)` method
- [ ] Create JSON file with tool definition
- [ ] Attach script to GameObject
- [ ] Configure ToolBindings: Set `Tools Json` and `Target`
- [ ] Method names in C# match JSON exactly

**That's it!** The AI can now call your tools.

---

## Quick Example

**What you want:** AI controls a cube's rotation when you say "spin the cube" or "stop spinning".

**What you need:**
1. C# script with tool methods
2. JSON file describing the tools
3. Connect them in Unity

---

## Step 1: Create Your Tool Script

Create a MonoBehaviour with methods that match this signature:

```csharp
public async Task<JObject> ToolName(JObject args)
```

**Example - CubeController.cs:**

```csharp
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class CubeController : MonoBehaviour
{
    // Method names MUST match your JSON schema
    public async Task<JObject> Cube_StartSpin(JObject args)
    {
        // Your logic here
        transform.Rotate(0, 90, 0);

        await Task.Yield();
        return new JObject { ["success"] = true };
    }

    public async Task<JObject> Cube_StopSpin(JObject args)
    {
        // Your logic here
        transform.rotation = Quaternion.identity;

        await Task.Yield();
        return new JObject { ["success"] = true };
    }

    public async Task<JObject> Cube_SetSpeed(JObject args)
    {
        // Extract parameters from args
        var speed = args?.Value<float?>("speed") ?? 90f;

        // Your logic here
        Debug.Log($"Setting speed to {speed}");

        await Task.Yield();
        return new JObject { ["success"] = true, ["speed"] = speed };
    }
}
```

**Key Rules:**
- Method must be `public`
- Must return `Task<JObject>` or `JObject`
- Must accept exactly one parameter: `JObject args`
- Method name must match JSON schema exactly (case-insensitive)

---

## Step 2: Create JSON Schema

Create a `.json` file in your Assets folder describing each tool.

**Example - CubeTools.json:**

```json
{
  "tools": [
    {
      "type": "function",
      "name": "Cube_StartSpin",
      "description": "Start spinning the cube",
      "parameters": {
        "type": "object",
        "properties": {}
      }
    },
    {
      "type": "function",
      "name": "Cube_StopSpin",
      "description": "Stop the cube from spinning",
      "parameters": {
        "type": "object",
        "properties": {}
      }
    },
    {
      "type": "function",
      "name": "Cube_SetSpeed",
      "description": "Set the cube rotation speed in degrees per second",
      "parameters": {
        "type": "object",
        "properties": {
          "speed": {
            "type": "number",
            "description": "Rotation speed in degrees/second"
          }
        },
        "required": ["speed"]
      }
    }
  ]
}
```

**Key Rules:**
- `name` must match your C# method name exactly
- `description` tells the AI when to use this tool
- `parameters` defines what data the tool needs
- Use clear descriptions - the AI uses them to decide when to call tools

---

## Step 3: Wire Up in Unity

### 3.1 Attach Your Script
1. Select the GameObject you want to control
2. Add Component → Your script (e.g., `CubeController`)

### 3.2 Configure ToolBindings

Find or create a GameObject with the `ToolBindings` component:

**Inspector Settings:**
- **Tools Json**: Drag your JSON file (e.g., `CubeTools.json`)
- **Target**: Drag the GameObject with your tool script
- **Method Prefix**: Leave empty (optional)
- **Register On Awake**: ✓ Checked

### 3.3 Test

Run your scene and talk to the AI:
- "Start spinning the cube"
- "Stop the cube"
- "Set the speed to 180 degrees per second"

---

## Common Patterns

### Pattern 1: Simple Action (No Parameters)

```csharp
public async Task<JObject> Light_TurnOn(JObject args)
{
    GetComponent<Light>().enabled = true;
    await Task.Yield();
    return new JObject { ["ok"] = true };
}
```

```json
{
  "type": "function",
  "name": "Light_TurnOn",
  "description": "Turn on the light",
  "parameters": {
    "type": "object",
    "properties": {}
  }
}
```

### Pattern 2: Action with Parameters

```csharp
public async Task<JObject> Light_SetColor(JObject args)
{
    var colorName = args?.Value<string>("color") ?? "white";

    Color color = colorName.ToLower() switch
    {
        "red" => Color.red,
        "blue" => Color.blue,
        "green" => Color.green,
        _ => Color.white
    };

    GetComponent<Light>().color = color;

    await Task.Yield();
    return new JObject { ["ok"] = true, ["color"] = colorName };
}
```

```json
{
  "type": "function",
  "name": "Light_SetColor",
  "description": "Change the light color",
  "parameters": {
    "type": "object",
    "properties": {
      "color": {
        "type": "string",
        "enum": ["red", "blue", "green", "white"],
        "description": "The color to set"
      }
    },
    "required": ["color"]
  }
}
```

### Pattern 3: Using UnityEvents

```csharp
using UnityEngine.Events;

public class DoorController : MonoBehaviour
{
    public UnityEvent onOpen;
    public UnityEvent onClose;

    public async Task<JObject> Door_Open(JObject args)
    {
        onOpen?.Invoke();
        await Task.Yield();
        return new JObject { ["ok"] = true };
    }

    public async Task<JObject> Door_Close(JObject args)
    {
        onClose?.Invoke();
        await Task.Yield();
        return new JObject { ["ok"] = true };
    }
}
```

Then wire up Unity animations, sounds, or other components via the Inspector!

---

## Troubleshooting

### Tool Not Found

```
[OpenAI] No method found for tool 'MyTool' on target 'MyObject'
```

**Fix:**
- Check method name matches JSON exactly
- Method must be `public`
- Method signature: `Task<JObject> MethodName(JObject args)`
- Script is attached to the Target GameObject

### AI Never Calls My Tool

**Fix:**
- Improve tool `description` in JSON - be specific about when to use it
- Test by directly asking: "Use the [tool name] tool"
- Check Console for `[AgentToolRegistry] Registered tool: ...` messages

### Tools Not Registering

**Fix:**
- Ensure `ToolBindings` component exists in scene
- Check `Tools Json` and `Target` are assigned
- `Register On Awake` is checked
- Check Console for errors during startup

---

## Best Practices

1. **Clear Names**: Use descriptive method names like `Light_TurnOn`, not just `TurnOn`
2. **Good Descriptions**: Write descriptions that explain *when* to use the tool
3. **Return Status**: Always return `{ "ok": true }` or `{ "ok": false, "error": "..." }`
4. **Validate Input**: Check `args` for null and provide defaults
5. **Keep It Simple**: Each tool should do one thing well

---

## Example: Complete Setup

**GameController.cs:**
```csharp
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

public class GameController : MonoBehaviour
{
    public UnityEvent onGameStart;
    public UnityEvent onGamePause;

    public async Task<JObject> Game_Start(JObject args)
    {
        onGameStart?.Invoke();
        await Task.Yield();
        return new JObject { ["ok"] = true };
    }

    public async Task<JObject> Game_Pause(JObject args)
    {
        onGamePause?.Invoke();
        await Task.Yield();
        return new JObject { ["ok"] = true };
    }
}
```

**GameTools.json:**
```json
{
  "tools": [
    {
      "type": "function",
      "name": "Game_Start",
      "description": "Start or resume the game",
      "parameters": { "type": "object", "properties": {} }
    },
    {
      "type": "function",
      "name": "Game_Pause",
      "description": "Pause the game",
      "parameters": { "type": "object", "properties": {} }
    }
  ]
}
```

**Unity Setup:**
1. Create empty GameObject: "GameManager"
2. Add `GameController` script
3. Wire up `onGameStart` and `onGamePause` events to your game logic
4. Find `ToolBindings` in scene
5. Set `Tools Json` = GameTools.json
6. Set `Target` = GameManager
7. Done!
