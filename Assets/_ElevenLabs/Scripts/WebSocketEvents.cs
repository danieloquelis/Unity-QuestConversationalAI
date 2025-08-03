using Newtonsoft.Json;
using System.Collections.Generic;

namespace ElevenLabs
{
    public class BaseEvent
    {
        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public class ConversationInitiationMetadataEvent : BaseEvent
    {
        [JsonProperty("conversation_initiation_metadata_event")]
        public ConversationInitiationMetadataEventData ConversationInitiationMetadata { get; set; }
    }
    public class ConversationInitiationMetadataEventData
    {
        [JsonProperty("conversation_id")]
        public string ConversationId { get; set; }
        [JsonProperty("agent_output_audio_format")]
        public string AgentOutputAudioFormat { get; set; }
        [JsonProperty("user_input_audio_format")]
        public string UserInputAudioFormat { get; set; }
    }

    public class UserTranscriptEvent : BaseEvent
    {
        [JsonProperty("user_transcription_event")]
        public UserTranscriptionEventData UserTranscriptionEvent { get; set; }
    }
    public class UserTranscriptionEventData
    {
        [JsonProperty("user_transcript")]
        public string UserTranscript { get; set; }
    }

    public class AgentResponseEvent : BaseEvent
    {
        [JsonProperty("agent_response_event")]
        public AgentResponseEventData AgentResponseEventData { get; set; }
    }
    public class AgentResponseEventData
    {
        [JsonProperty("agent_response")]
        public string AgentResponse { get; set; }
    }

    public class VadScoreEvent : BaseEvent
    {
        [JsonProperty("vad_score_event")]
        public VadScoreEventData VadScoreEventData { get; set; }
    }

    public class VadScoreEventData
    {
        [JsonProperty("vad_score")]
        public float VadScore { get; set; }
    }

    public class AgentResponseCorrectionEvent : BaseEvent
    {
        [JsonProperty("agent_response_correction_event")]
        public AgentResponseCorrectionEventData AgentResponseCorrectionEventData { get; set; }
    }
    public class AgentResponseCorrectionEventData
    {
        [JsonProperty("corrected_response")]
        public string CorrectedResponse { get; set; }
    }

    public class AudioResponseEvent : BaseEvent
    {
        [JsonProperty("audio_event")]
        public AudioEventData AudioEvent { get; set; }
    }
    public class AudioEventData
    {
        [JsonProperty("audio_base_64")]
        public string AudioBase64 { get; set; }
        [JsonProperty("event_id")]
        public int EventId { get; set; }
    }

    public class InterruptionEvent : BaseEvent
    {
        [JsonProperty("interruption_event")]
        public InterruptionEventData InterruptionEventData { get; set; }
    }
    public class InterruptionEventData
    {
        [JsonProperty("reason")]
        public string Reason { get; set; }
    }

    public class PingEvent : BaseEvent
    {
        [JsonProperty("ping_event")]
        public PingEventData PingEventData { get; set; }
    }
    public class PingEventData
    {
        [JsonProperty("event_id")]
        public int EventId { get; set; }
        [JsonProperty("ping_ms")]
        public int? PingMs { get; set; }
    }

    public class ClientToolCallEvent : BaseEvent
    {
        [JsonProperty("client_tool_call")]
        public ClientToolCallData ClientToolCall { get; set; }
    }
    public class ClientToolCallData
    {
        [JsonProperty("tool_name")]
        public string ToolName { get; set; }
        [JsonProperty("tool_call_id")]
        public string ToolCallId { get; set; }
        [JsonProperty("parameters")]
        public Dictionary<string, object> Parameters { get; set; }
    }

    public class InternalVadScoreEvent : BaseEvent
    {
        [JsonProperty("vad_event")]
        public VadEventData VadEvent { get; set; }
    }
    public class VadEventData
    {
        [JsonProperty("score")]
        public float Score { get; set; }
    }

    public class InternalTurnProbabilityEvent : BaseEvent
    {
        [JsonProperty("turn_event")]
        public TurnEventData TurnEvent { get; set; }
    }
    public class TurnEventData
    {
        [JsonProperty("probability")]
        public float Probability { get; set; }
    }

    public class InternalTentativeAgentResponseEvent : BaseEvent
    {
        [JsonProperty("tentative_agent_response_internal_event")]
        public TentativeAgentResponseInternalEventData TentativeAgentResponseInternalEvent { get; set; }
    }
    public class TentativeAgentResponseInternalEventData
    {
        [JsonProperty("tentative_agent_response")]
        public string TentativeAgentResponse { get; set; }
    }
} 