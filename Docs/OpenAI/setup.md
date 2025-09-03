# OpenAI Real-time Conversational AI - Setup

Unity package for integrating OpenAI's real-time conversation API into your Unity projects.

## Installation

### Via Unity Package Manager

1. Open Unity Package Manager
2. Add package from git URL: `https://github.com/danieloquelis/Unity-QuestConversationalAI.git?path=/com.convai.openai`

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

### Creating Custom Tools

Create a MonoBehaviour with public async Task<JObject> methods that match your tool names:

```csharp
public class MyCustomTool : MonoBehaviour
{
    public async Task<JObject> MyTool_Action(JObject args)
    {
        // Extract parameters from args
        var value = args?.Value<string>("parameter");

        // Perform your action
        Debug.Log($"Tool called with: {value}");

        // Return result
        await Task.Yield();
        return new JObject { ["success"] = true };
    }
}
```

### Tool Schema

Create a JSON schema file defining your tools:

```json
{
  "tools": [
    {
      "type": "function",
      "name": "MyTool_Action",
      "description": "Description of what this tool does",
      "parameters": {
        "type": "object",
        "properties": {
          "parameter": { "type": "string" }
        },
        "required": ["parameter"]
      }
    }
  ]
}
```

### Registering Tools

1. Assign your tool MonoBehaviour to the `AgentToolsBinding` prefab
2. Reference your JSON schema file in the tool binding configuration
3. The system will automatically detect and register methods matching the schema

## Events

### RealtimeConversationManager Events

- `onAgentTranscript`: Fired when AI speaks (provides transcript text)
- `onUserTranscript`: Fired when user speaks (provides transcript text)
- `onUserSpeaking`: Fired when user speaking state changes (bool)
- `onAgentSpeaking`: Fired when AI speaking state changes (bool)
