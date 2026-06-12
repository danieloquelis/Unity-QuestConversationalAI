using UnityEngine;

namespace SixtyDb
{
    // ScriptableObject holding 60db credentials and endpoint overrides.
    // Mirrors the role of ElevenLabsConfig / OpenAIConfig in the sibling packages.
    [CreateAssetMenu(menuName = "SixtyDb/Config", fileName = "SixtyDbConfig")]
    public class SixtyDbConfig : ScriptableObject
    {
        [Header("Authentication")]
        public string apiKey;

        [Header("Endpoints (override only if you're on staging / self-hosted)")]
        public string apiBase = "https://api.60db.ai";
        public string sttWebsocketUrl = "wss://api.60db.ai/ws/stt";
        public string ttsWebsocketUrl = "wss://api.60db.ai/ws/tts";

        [Header("Model defaults")]
        public string llmModel = "60db-tiny";
        public string defaultVoiceId = "fbb75ed2-975a-40c7-9e06-38e30524a9a1";

        [Header("Audio")]
        [Tooltip("Sample rate sent to /ws/stt. 60db browser mode expects 16 kHz linear PCM.")]
        public int sttSampleRate = 16_000;

        [Tooltip("Sample rate requested from /ws/tts. 60db returns LINEAR16 at this rate.")]
        public int ttsSampleRate = 24_000;
    }
}
