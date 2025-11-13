# OpenAI Real-time Conversational AI - Setup

Unity package for integrating OpenAI's real-time conversation API into your Unity projects.

## Installation

### Prerequisites

Before installing this package, you must add **NativeWebSocket** to your project:

1. Open your project's `Packages/manifest.json` file
2. Add the following to the `dependencies` section:
```json
"com.endel.nativewebsocket": "https://github.com/endel/NativeWebSocket.git#upm"
```
3. Save the file and return to Unity (it will auto-import)

### Via Unity Package Manager

1. Open Unity Package Manager
2. Add package from git URL: `https://github.com/danieloquelis/Unity-QuestConversationalAI.git?path=/com.convai.openai`
3. Newtonsoft-Json will be installed automatically

## Setup

### 1. Get OpenAI API Key

1. Create an account at [OpenAI Platform](https://platform.openai.com)
2. Navigate to [API Keys](https://platform.openai.com/api-keys)
3. Generate a new API key

### 2. Configure Unity

1. In Unity, go to `Assets/Resources/`
2. Right-click → Create → OpenAI → Config
3. Paste your API key in the configuration asset
4. Save the configuration

## Core Prefabs

### RealtimeConversationManager

The main prefab for handling OpenAI real-time conversations.

**Setup:**

1. Drag `RealtimeConversationManager` prefab into your scene
2. Assign your OpenAI config asset to the `config` field
3. Configure the `systemPrompt` for AI behavior
4. Run the scene and start speaking

**Key Properties:**
| Property | Type | Description |
| -------------- | -------------- | -------------------------------- |
| `config` | `OpenAIConfig` | Configuration asset with API key |
| `systemPrompt` | `string` | AI behavior instructions |
| `autoStart` | `bool` | Start conversation automatically |

### AgentToolsBinding

Prefab for managing custom tools that the AI can invoke during conversations.

**Setup:**

1. Drag `AgentToolsBinding` prefab into your scene
2. Assign your tool scripts to the binding component
3. Configure tool schema JSON files

## Custom Tools

Custom tools let the AI perform actions in your scene. See the [Complete Tools Guide](tools.md) for detailed examples.

### Quick Setup

**1. Create a C# script with tool methods:**
```csharp
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class MyTools : MonoBehaviour
{
    // Method signature: public async Task<JObject> ToolName(JObject args)
    public async Task<JObject> Light_TurnOn(JObject args)
    {
        GetComponent<Light>().enabled = true;
        await Task.Yield();
        return new JObject { ["ok"] = true };
    }
}
```

**2. Create a JSON schema:**
```json
{
  "tools": [{
    "type": "function",
    "name": "Light_TurnOn",
    "description": "Turn on the light",
    "parameters": { "type": "object", "properties": {} }
  }]
}
```

**3. Wire up in Unity:**
- Attach `MyTools` script to a GameObject
- In `ToolBindings` component:
  - Set `Tools Json` → your JSON file
  - Set `Target` → GameObject with your script

**See [tools.md](tools.md) for complete guide with patterns and troubleshooting.**

## Events

### RealtimeConversationManager Events

- `onAgentTranscript`: Fired when AI speaks (provides transcript text)
- `onUserTranscript`: Fired when user speaks (provides transcript text)
- `onUserSpeaking`: Fired when user speaking state changes (bool)
- `onAgentSpeaking`: Fired when AI speaking state changes (bool)
