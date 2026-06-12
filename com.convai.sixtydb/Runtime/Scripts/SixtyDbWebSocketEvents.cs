using Newtonsoft.Json;

namespace SixtyDb
{
    // POCOs mapped to the JSON envelope used by 60db's /ws/stt and /ws/tts
    // streams. Mirrors the role of ElevenLabsWebSocketEvents / OpenAIWebSocketEvents.
    // Field names lifted verbatim from https://docs.60db.ai/websocket-api/{stt,tts}.

    public class SixtyDbBaseEvent
    {
        [JsonProperty("type")] public string Type { get; set; }
    }

    // ---- STT ----------------------------------------------------------------

    public class SttConnectedEvent : SixtyDbBaseEvent {}

    public class SttSpeechStartedEvent : SixtyDbBaseEvent {}

    public class SttTranscriptionEvent : SixtyDbBaseEvent
    {
        [JsonProperty("transcript")]   public string Transcript   { get; set; }
        [JsonProperty("text")]         public string Text         { get; set; }
        [JsonProperty("is_final")]     public bool   IsFinal      { get; set; }
        [JsonProperty("speech_final")] public bool   SpeechFinal  { get; set; }
    }

    public class SttSessionStoppedEvent : SixtyDbBaseEvent {}

    public class SttErrorEvent : SixtyDbBaseEvent
    {
        [JsonProperty("message")] public string Message { get; set; }
    }

    // ---- TTS ----------------------------------------------------------------

    public class TtsContextCreatedEvent : SixtyDbBaseEvent
    {
        [JsonProperty("context_id")] public string ContextId { get; set; }
    }

    public class TtsAudioChunkEvent : SixtyDbBaseEvent
    {
        [JsonProperty("audioContent")] public string AudioBase64 { get; set; }
        [JsonProperty("audio")]        public string AudioFallback { get; set; }

        public string ResolveAudio() =>
            !string.IsNullOrEmpty(AudioBase64) ? AudioBase64 : AudioFallback;
    }

    public class TtsFlushCompletedEvent : SixtyDbBaseEvent {}

    public class TtsErrorEvent : SixtyDbBaseEvent
    {
        [JsonProperty("message")] public string Message { get; set; }
    }
}
