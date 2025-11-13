# ElevenLabs Conversational AI - Setup

Unity package for integrating ElevenLabs' conversational AI with real-time speech-to-speech capabilities.

## Installation

### Prerequisites

Before installing this package, add **NativeWebSocket**:

1. Open Unity Package Manager (Window → Package Manager)
2. Click **+** → **Add package from git URL**
3. Enter: `https://github.com/endel/NativeWebSocket.git#upm`
4. Click **Add**

### Install Package

1. Open Unity Package Manager (Window → Package Manager)
2. Click **+** → **Add package from git URL**
3. Enter: `https://github.com/danieloquelis/Unity-QuestConversationalAI.git?path=/com.convai.elevenlabs`
4. Click **Add**

## Setup

### 1. Create ElevenLabs Account & Agent

1. Visit [ElevenLabs](https://elevenlabs.io) and create an account
2. Navigate to the [Conversational AI section](https://elevenlabs.io/docs/api-reference/conversational-ai)
3. Create a new Conversational Agent in the dashboard
4. Configure the agent's personality and behavior
5. **Important**: Make the agent **secured**
6. Note down the Agent ID for Unity configuration

### 2. Get API Key

1. In your ElevenLabs dashboard, generate an API key
2. Ensure the key has access to **speech-to-speech** functionality
3. Copy the API key for Unity configuration

### 3. Configure Unity

1. In Unity, go to `Assets/Resources/`
2. Right-click → Create → ElevenLabs → Config
3. Add your API key to the configuration asset
4. Save the configuration

## Core Prefabs

### AgentConversationManager

The main prefab for handling ElevenLabs conversational AI.

**Setup:**

1. Drag `AgentConversationManager` prefab into your scene
2. Assign your ElevenLabs config to the `config` field
3. Set your Agent ID in the `agentId` field
4. Configure `startOnAwake` if you want automatic conversation start
5. Run the scene and start speaking

**Key Properties:**
| Property | Type | Description |
| -------- | ---- | ----------- |
| `agentId` | `string` | Your ElevenLabs agent identifier |
| `config` | `ElevenLabsConfig` | Configuration asset with API key |
| `startOnAwake` | `bool` | Begin conversation automatically |
| `micStreamer` | `MicrophoneStreamer` | Microphone input component |
| `audioPlayer` | `PcmAudioPlayer` | Audio output component |

## Events

### AgentConversationManager Events

- `onAgentVadScore`: Fired with agent voice activity detection score (float)
- `onAgentTranscript`: Fired when agent speaks (provides transcript text)
- `onUserTranscript`: Fired when user speaks (provides transcript text)
