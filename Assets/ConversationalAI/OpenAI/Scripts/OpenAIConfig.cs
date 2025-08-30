using UnityEngine;

namespace OpenAI
{
    [CreateAssetMenu(menuName = "OpenAI/Config", fileName = "OpenAIConfig")]
    public class OpenAIConfig : ScriptableObject
    {
        public string apiKey;
        public string realtimeConvWebsocketUrl = "wss://api.openai.com/v1/realtime";
    }
}
