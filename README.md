# Unity Quest Conversational AI

A Unity project showcasing **real-time conversational AI** with speech-to-speech capabilities for Meta Quest Mixed Reality headsets. This project demonstrates how to implement truly real-time voice interactions, bypassing the traditional text-to-speech → text-to-speech pipeline that introduces significant latency.

## Project Overview

As AI technology evolves, most conversational AI examples focus on text-to-speech, but the traditional process of speech-to-text → AI processing → text-to-speech introduces significant delays. This project showcases how to implement **true real-time speech-to-speech interactions** using WebSocket connections for bidirectional, low-latency communication.

### ElevenLabs Demo

https://github.com/user-attachments/assets/f09c0677-662b-4782-954c-8dd8d563d960

### OpenAI Demo

https://github.com/user-attachments/assets/f8d7c13e-5d02-4b15-9bea-eb8ead26bdba

## Features

- **Real-time Speech-to-Speech**: Direct voice interaction without text conversion delays
- **WebSocket Communication**: Bidirectional, low-latency communication with AI services
- **Mixed Reality Ready**: Optimized for Meta Quest Mixed Reality headsets with Meta SDK integration
- **Visual Feedback**: Dynamic 3D orb visualization that responds to speech patterns
- **Multiple AI Providers**: Support for both ElevenLabs and OpenAI real-time APIs
- **Cross-Platform**: Works on both desktop and Meta Quest devices

## Requirements

### Software Requirements

- **Unity**: 6000.0.53f1 (LTS version)
- **Meta SDK**: 77.0.0
- **Target Platform**: Meta Quest 3 Mixed Reality (tested)

### Dependencies

- **ORBS VFX PACK**: Included in `Assets/_ExternalAssets/Orbs/`
- **NativeWebSocket**: For WebSocket communication
- **Universal Render Pipeline**: For optimized rendering

### API Requirements

- **ElevenLabs**: API key with speech-to-speech access
- **OpenAI**: API key with real-time conversation access (beta)

## Getting Started

### Prerequisites

1. **Unity Setup**

   - Install Unity 6000.0.53f1 (LTS)
   - Open the project in Unity
   - Ensure Meta SDK 77.0.0 is properly configured

2. **Meta Quest Setup**
   - Enable Developer Mode on your Meta Quest Mixed Reality device
   - Install Meta Quest Developer Hub
   - Configure your device for development

### Project Structure

```
Assets/
├── ConversationalAISamples/
│   ├── ElevenLabs/           # ElevenLabs implementation
│   ├── OpenAI/              # OpenAI implementation
│   └── Common/              # Shared components
├── _ExternalAssets/
│   └── Orbs/                # VFX pack for visual feedback
└── Resources/               # Configuration files
```

## ElevenLabs Implementation

### Setup Instructions

1. **Create ElevenLabs Account**

   - Visit [ElevenLabs](https://elevenlabs.io) and create an account
   - Navigate to the [Conversational AI section](https://elevenlabs.io/docs/api-reference/conversational-ai)

2. **Create a Conversational Agent**

   - Go to the ElevenLabs dashboard
   - Create a new Conversational Agent
   - Configure the agent's personality and behavior
   - **Important**: Make the agent **secured**
   - Note down the Agent ID for later use

3. **Get API Key**

   - In your ElevenLabs dashboard, generate an API key
   - Ensure the key has access to **speech-to-speech** functionality
   - Copy the API key for Unity configuration

4. **Unity Configuration**
   - In Unity, navigate to `Assets/Resources/`
   - Right-click → Create → ElevenLabs → Config
   - Add your API key to the configuration
   - Open the scene: `Assets/ConversationalAISamples/ElevenLabs/ElevenLabsConvAISample`
   - Select the `AgentConversationManager` GameObject in the hierarchy
   - Drag and drop the ElevenLabs config to the configuration field
   - Enter your Agent ID in the designated field

## OpenAI Implementation

### Setup Instructions

1. **Create OpenAI Account**

   - Visit [OpenAI](https://platform.openai.com) and create a developer account
   - Navigate to the [API Keys section](https://platform.openai.com/api-keys)

2. **Generate API Key**

   - Create a new API key
   - **Note**: OpenAI's real-time conversation API is currently in beta
   - Ensure you have access to the real-time conversation features

3. **Unity Configuration**
   - In Unity, navigate to `Assets/Resources/`
   - Right-click → Create → OpenAI → Config
   - Paste your API key in the configuration
   - Open the scene: `Assets/ConversationalAISamples/OpenAI/OpenAIRealtimeConvSample.unity`
   - Select the conversation manager GameObject
   - Drag and drop the OpenAI config to the configuration field
   - Optionally modify the model ID (leave blank for default)
   - Customize the conversation prompt as needed

## Usage

### Testing

1. **Desktop Testing**

   - Press Play in Unity Editor
   - Use your computer's microphone for testing
   - The orb will visualize speech patterns

2. **Meta Quest Testing**
   - Build the project for Android/Quest
   - Install on your Meta Quest Mixed Reality device
   - Grant microphone permissions when prompted
   - Use the Quest's built-in microphone

### Starting Conversations

- **ElevenLabs**: The agent may start the conversation automatically, or you can greet it
- **OpenAI**: Begin by speaking naturally - the AI will respond based on the configured prompt
- **Visual Feedback**: The 3D orb will vibrate and animate based on speech patterns

## Technical Details

### Architecture

- **WebSocket Communication**: Real-time bidirectional communication
- **Audio Streaming**: Continuous audio input/output processing
- **VFX Integration**: Dynamic orb visualization using the ORBS VFX pack
- **Mixed Reality Optimization**: Optimized for Meta Quest performance

### Key Components

- **MicrophoneStreamer**: Handles real-time audio input
- **PcmAudioPlayer**: Manages audio output
- **AgentConversationManager**: Coordinates AI interactions
- **Orb Visualization**: Provides visual feedback for speech

### Permissions

The project requires microphone access. This is configured in:

```
Assets/Plugins/Android/AndroidManifest.xml
```

## Troubleshooting

### Common Issues

1. **Microphone Not Working**

   - Ensure microphone permissions are granted
   - Check AndroidManifest.xml configuration
   - Verify device microphone functionality

2. **API Connection Issues**

   - Verify API keys are correct
   - Check internet connectivity
   - Ensure API quotas haven't been exceeded

3. **Performance Issues on Quest**
   - Reduce VFX complexity if needed
   - Check Unity's profiler for bottlenecks
   - Ensure proper Quest Mixed Reality optimization settings

### Debug Information

- Check Unity Console for error messages
- Monitor WebSocket connection status
- Verify audio input/output levels

## License

This project is provided as-is for educational and demonstration purposes.

## Contributing

Feel free to submit issues, feature requests, or pull requests to improve this project.

## Support

For questions or issues:

- Check the troubleshooting section above
- Review the API documentation for ElevenLabs and OpenAI
- Ensure all dependencies are properly installed

---

**Note**: This project demonstrates cutting-edge real-time AI conversation technology. The OpenAI real-time conversation API is in beta, so functionality may vary. Always refer to the latest API documentation for the most current information.
