using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenAI
{
    public class BaseEvent
    {
        [JsonProperty("type")] public string Type { get; set; }
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
        [JsonProperty("error")] public ErrorBody Error { get; set; }
    }

    public class ErrorBody
    {
        [JsonProperty("code")]    public string Code    { get; set; }
        [JsonProperty("message")] public string Message { get; set; }
        [JsonProperty("type")]    public string ErrType { get; set; }
        [JsonProperty("param")]   public string Param   { get; set; }
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

    public class ResponseToolCall : BaseEvent
    {
        [JsonProperty("name")]           public string  Name          { get; set; }
        [JsonProperty("arguments")]      public string  ArgumentsJson { get; set; }
        [JsonProperty("arguments_json")] public JObject ArgumentsObj  { get; set; }
        [JsonProperty("call_id")]        public string  CallId        { get; set; }

        public string GetArgsJson()
        {
            if (!string.IsNullOrEmpty(ArgumentsJson)) return ArgumentsJson;
            return ArgumentsObj != null ? ArgumentsObj.ToString(Formatting.None) : "{}";
        }
    }
}