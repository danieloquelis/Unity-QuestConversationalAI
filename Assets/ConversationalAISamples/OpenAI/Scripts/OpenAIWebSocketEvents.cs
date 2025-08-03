using Newtonsoft.Json;

namespace OpenAI
{
    public class BaseEvent
    {
        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public class SessionCreatedEvent : BaseEvent
    {
        [JsonProperty("session_id")] public string SessionId { get; set; }
    }

    public class SessionUpdatedEvent : BaseEvent
    {
        [JsonProperty("session")] public object Session { get; set; }
    }

    public class ErrorEvent : BaseEvent
    {
        [JsonProperty("code")]    public string Code { get; set; }
        [JsonProperty("message")] public string Message { get; set; }
    }

    public class InputAudioTranscriptDone : BaseEvent
    {
        [JsonProperty("text")] public string Text { get; set; }
    }

    public class ResponseAudioDelta : BaseEvent
    {
        [JsonProperty("delta")] public string DeltaBase64 { get; set; }
    }

    public class ResponseAudioTranscriptDelta : BaseEvent
    {
        [JsonProperty("delta")] public string DeltaText { get; set; }
    }
}