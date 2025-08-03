using UnityEngine;

namespace ElevenLabs
{
    [CreateAssetMenu(menuName = "ElevenLabs/Config", fileName = "ElevenLabsConfig")]
    public class ElevenLabsConfig : ScriptableObject
    {
        public string apiKey;
        public string websocketUrl = "wss://api.elevenlabs.io/v1";
        public string signedWebsocketUrl = "https://api.elevenlabs.io/v1/convai/conversation/get_signed_url";
    }
}
