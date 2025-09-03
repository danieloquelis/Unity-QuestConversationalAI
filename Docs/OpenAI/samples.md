# OpenAI Sample Scenes

Sample scenes demonstrating different OpenAI real-time conversation capabilities. Clone the [full repository](https://github.com/yourusername/Unity-QuestConversationalAI) to access these samples.

## Sample Scenes

### 1. RealtimeConvSample

**Location**: `Assets/Samples/OpenAI/RealtimeConvSample/`

Basic real-time conversation implementation with visual feedback.

https://github.com/user-attachments/assets/a4f17a45-d40a-4a1f-8cf8-f1cc60617571

**Features:**

- Speech-to-speech conversation
- 3D orb visualization
- Configurable AI prompts

**Setup:**

1. Open `RealtimeConvSample.unity`
2. Configure OpenAI settings on RealtimeConversationManager
3. Run and start speaking

### 2. SimpleVoiceActionSample

**Location**: `Assets/Samples/OpenAI/SimpleVoiceActionSample/`

Voice-controlled actions with function calling.

https://github.com/user-attachments/assets/b7cae009-da9b-437c-b876-b5a64e2ffd24

**Features:**

- Voice commands for object manipulation
- Custom tool integration
- JSON schema-based actions

**How it works:**

- `VoiceActionTool.cs` implements methods that match the AI function calls
- `ActionsSchema.json` defines the available tools for the AI
- Unity Events connect AI calls to scene actions

**Setup:**

1. Open `SimpleVoiceActionsSample.unity`
2. Configure OpenAI settings
3. Try voice commands like "can you make sping the cube?"

### 3. PCAConversationalAISample

**Location**: `Assets/Samples/OpenAI/PCAConversationalAISample/`

Conversational AI with visual input capabilities.

https://github.com/user-attachments/assets/fd5ff4bf-4af5-4e95-a876-ff79dc3d3bf5

**Features:**

- Camera integration
- Image-based conversations
- Real-time visual processing

**Setup:**

1. Open `PCAConversationalAISample.unity`
2. Configure OpenAI settings
3. Point camera at objects and ask the AI about what it sees

### 4. AgenticToolsSample

**Location**: `Assets/Samples/OpenAI/AgenticToolsSample/`

Advanced tool integration with mesh and line art generation.


https://github.com/user-attachments/assets/00370cdf-7f4c-4b68-8d85-89843b6502ec


**Scenes:**

- `LineArtToolsSample.unity`: AI-generated line art
- `MeshToolsSample.unity`: 3D mesh generation tools

**Features:**

- AI-controlled content generation
- Advanced tool integration
- Creative AI applications

**Setup:**

1. Open desired sample scene
2. Configure OpenAI settings
3. Ask the AI to generate content based on your descriptions

**Additional Setup for MeshToolsSample:**

For the `MeshToolsSample.unity` scene, you need to run the Python mesh tool server locally:

1. **Prerequisites**: Ensure you have Python 3.11 installed
2. **Navigate to MeshTool folder**: Open terminal and go to `MeshTool/` directory
3. **Install dependencies**:
   ```bash
   pip install -r requirements.txt
   ```
4. **Run the mesh tool server**:
   ```bash
   python mesh_tool.py
   ```
5. **Verify server is running**: You should see "Server started successfully!" on port 8765
6. **Now run the Unity scene**: The MeshToolsSample will connect to the local Python server

The Python server provides advanced 3D mesh generation capabilities that Unity calls through WebSocket communication.

## Quick Start Guide

### Running Any Sample

1. **Prerequisites**: Ensure you have an OpenAI API key
2. **Open Scene**: Navigate to the sample scene you want to try
3. **Configure**: Set up your OpenAI config asset in the scene
4. **Test**: Press Play and interact with voice
