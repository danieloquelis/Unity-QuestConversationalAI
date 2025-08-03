using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NativeWebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace OpenAI
{
    /// <summary>
    /// Unity 6 client for the OpenAI Realtime (beta) WebSocket API.
    ///  ✔ sends input_audio_buffer.append events (24 kHz PCM16 in Base64)
    ///  ✔ waits for session.created before starting the mic
    ///  ✔ plays response.audio.delta chunks on the fly
    ///  ✔ emits agent/user transcript events
    /// </summary>
    public class RealtimeConversationManager : MonoBehaviour
    {
        /* ------------------------------ Inspector ------------------------------ */

        [Header("Model")]
        [SerializeField] private string model = "gpt-4o-realtime-preview-2025-06-03";
        [SerializeField] private bool startOnAwake = true;

        [Header("Dependencies")]
        [SerializeField] private OpenAIConfig     config;
        [SerializeField] private MicrophoneStreamer micStreamer;
        [SerializeField] private PcmAudioPlayer     audioPlayer;

        [Header("Unity Events")]
        public UnityEvent<string> onAgentTranscript;
        public UnityEvent<string> onUserTranscript;
        public UnityEvent<float>  onAgentVadScore;

        /* ------------------------------ Internals ------------------------------ */

        private WebSocket  _ws;
        private bool       _sessionReady;

        /* ---------------------------------------------------------------------- */
        /*                             Life-cycle                                 */
        /* ---------------------------------------------------------------------- */

        private void Start()
        {
            if (startOnAwake) StartAgent();
        }

        private void Update() => _ws?.DispatchMessageQueue();

        private async void OnDisable()  { await GracefulShutdown(); }
        private async void OnApplicationQuit() { await GracefulShutdown(); }

        public async void StartAgent()
        {
            try
            {
                var url = $"{config.realtimeConvWebsocketUrl}?model={Uri.EscapeDataString(model)}";

                /* ------------ OpenAI requires two headers ------------------ */
                var headers = new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {config.apiKey}" },
                    { "OpenAI-Beta",  "realtime=v1" }
                };

                _ws = new WebSocket(url, headers);
                _ws.OnOpen    += OnSocketOpen;
                _ws.OnClose   += OnSocketClose;
                _ws.OnMessage += OnSocketMessage;

                /* delegate for mic chunks ---------------------------------- */
                micStreamer.OnAudioChunk += HandleMicChunk;

                await _ws.Connect();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OpenAI-Realtime] Failed to start: {ex}");
                enabled = false;
            }
        }

        /* ---------------------------------------------------------------------- */
        /*                             WebSocket                                  */
        /* ---------------------------------------------------------------------- */

        private async Task GracefulShutdown()
        {
            if (_ws != null && _ws.State != WebSocketState.Closed)
                await _ws.Close();

            micStreamer.OnAudioChunk -= HandleMicChunk;
            micStreamer.StopStreaming();
            audioPlayer.StopImmediately();
        }

        private void OnSocketOpen()
        {
            Debug.Log("[OpenAI-Realtime] WS connected, waiting for session.created …");
        }

        private void OnSocketClose(WebSocketCloseCode code)
        {
            Debug.Log($"[OpenAI-Realtime] WS closed ({code})");
            micStreamer.StopStreaming();
            audioPlayer.StopImmediately();
            micStreamer.OnAudioChunk -= HandleMicChunk;
        }

        private void OnSocketMessage(byte[] raw)
        {
            var json = Encoding.UTF8.GetString(raw);
            var evt  = JObject.Parse(json);
            string type = evt.Value<string>("type");
            Debug.Log($"EVENT ===> {evt}");
            switch (type)
            {
                /* -------- connection / housekeeping -------- */
                case "session.created":
                    HandleSessionCreated();
                    break;
                /* -------- user → server echo  -------------- */
                case "conversation.item.input_audio_transcription.completed":
                    ForwardUserTranscript(evt);
                    break;

                /* -------- assistant response --------------- */
                case "response.audio.delta":
                    PlayAgentAudioDelta(evt);
                    break;
                case "response.audio_transcript.delta":
                    ForwardAgentTranscriptDelta(evt);
                    break;
                case "response.audio.done":
                    /* nothing to do – clip already queued */
                    break;

                /* -------- optional VAD scores -------------- */
                case "input_audio_buffer.speech_started":
                case "input_audio_buffer.speech_stopped":
                    /* could drive UI, ignored here */
                    break;

                default:
                    Debug.Log($"[OpenAI-Realtime] Unhandled event: {type}");
                    break;
            }
        }

        private void HandleMicChunk(string base64)
        {
            if (!_sessionReady || _ws.State != WebSocketState.Open) return;

            var payload = new Dictionary<string, object>
            {
                { "type",  "input_audio_buffer.append" },
                { "audio", base64 }
            };
            _ = _ws.SendText(JsonConvert.SerializeObject(payload));
        }

        private void HandleSessionCreated()
        {
            Debug.Log("[OpenAI-Realtime] session.created received – starting mic.");

            /* Optionally tweak default session config (e.g., switch VAD off/on) */
            var update = new Dictionary<string, object>
            {
                { "type", "session.update" },
                { "session", new Dictionary<string, object>
                    {
                        { "modalities",         new[] { "audio", "text" } },
                        { "input_audio_format",  "pcm16" },
                        { "output_audio_format", "pcm16" },
                        { "turn_detection", new Dictionary<string,object>
                            { { "type", "server_vad" } } }
                    }
                }
            };
            _ = _ws.SendText(JsonConvert.SerializeObject(update));

            _sessionReady = true;
            micStreamer.StartStreaming();
        }

        private void ForwardUserTranscript(JObject evt)
        {
            var delta = evt.SelectToken("$.text")?.ToString();
            if (!string.IsNullOrEmpty(delta))
                onUserTranscript?.Invoke(delta);
        }

        /* ---------------------------------------------------------------------- */
        /*                      Assistant-side helpers                            */
        /* ---------------------------------------------------------------------- */

        private void PlayAgentAudioDelta(JObject evt)
        {
            string deltaB64 = evt.Value<string>("delta");
            if (!string.IsNullOrEmpty(deltaB64))
                audioPlayer.EnqueueBase64Audio(deltaB64);
        }

        private void ForwardAgentTranscriptDelta(JObject evt)
        {
            var text = evt.Value<string>("delta");
            if (!string.IsNullOrEmpty(text))
                onAgentTranscript?.Invoke(text);
        }
    }
}
