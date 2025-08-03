using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NativeWebSocket;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace OpenAI
{
    public class RealtimeConversationManager : MonoBehaviour
    {
        #region Inspector

        [Header("Model")]
        [SerializeField] private string model = "gpt-4o-realtime-preview-2025-06-03";

        [Header("Optional system prompt")]
        [TextArea(2, 4)]
        [SerializeField] private string systemPrompt =
            "You are a helpful assistant who answers briefly.";

        [Header("Startup")]
        [SerializeField] private bool startOnAwake = true;

        [Header("Dependencies")]
        [SerializeField] private OpenAIConfig       config;
        [SerializeField] private MicrophoneStreamer micStreamer;
        [SerializeField] private PcmAudioPlayer     audioPlayer;

        [Header("Unity Events")]
        public UnityEvent<string> onAgentTranscript;
        public UnityEvent<string> onUserTranscript;
        public UnityEvent<bool>   onUserSpeaking;
        public UnityEvent<bool>   onAgentSpeaking;

        #endregion

        #region Internals

        private WebSocket _ws;
        private bool      _sessionReady;
        private bool      _agentCurrentlySpeaking;

        #endregion

        #region Unity lifecycle

        private void Start()
        {
            if (startOnAwake) StartAgent();
        }

        private void Update()
        {
            _ws?.DispatchMessageQueue();
        }

        private async void OnDisable()         => await Shutdown();
        private async void OnApplicationQuit() => await Shutdown();

        #endregion

        #region Public API

        public async void StartAgent()
        {
            try
            {
                var url = $"{config.realtimeConvWebsocketUrl}?model={Uri.EscapeDataString(model)}";

                var headers = new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {config.apiKey}" },
                    { "OpenAI-Beta",   "realtime=v1" }
                };

                _ws = new WebSocket(url, headers);
                _ws.OnOpen    += () => Debug.Log("[OpenAI] WS connected.");
                _ws.OnClose   += code => Debug.Log($"[OpenAI] WS closed ({code})");
                _ws.OnMessage += OnSocketMessage;

                micStreamer.OnAudioChunk += SendMicChunk;

                await _ws.Connect();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OpenAI] Startup failed: {ex}");
                enabled = false;
            }
        }

        #endregion

        #region Shutdown

        private async Task Shutdown()
        {
            micStreamer.OnAudioChunk -= SendMicChunk;
            micStreamer.StopStreaming();
            audioPlayer.StopImmediately();

            if (_ws != null && _ws.State != WebSocketState.Closed)
                await _ws.Close();
        }

        #endregion

        #region WebSocket receive

        private void OnSocketMessage(byte[] raw)
        {
            var text    = Encoding.UTF8.GetString(raw);
            var baseEvt = JsonConvert.DeserializeObject<BaseEvent>(text);

            switch (baseEvt.Type)
            {
                case "session.created":
                    _sessionReady = true;
                    SendSystemPrompt();
                    micStreamer.StartStreaming();
                    break;

                case "conversation.item.input_audio_transcription.completed":
                    var user = JsonConvert.DeserializeObject<InputAudioTranscriptDone>(text);
                    onUserTranscript?.Invoke(user.Text);
                    break;

                case "response.audio.delta":
                    if (!_agentCurrentlySpeaking)
                    {
                        _agentCurrentlySpeaking = true;
                        onAgentSpeaking?.Invoke(true);
                    }
                    var audio = JsonConvert.DeserializeObject<ResponseAudioDelta>(text);
                    audioPlayer.EnqueueBase64Audio(audio.DeltaBase64);
                    break;

                case "response.audio.done":
                    _agentCurrentlySpeaking = false;
                    onAgentSpeaking?.Invoke(false);
                    break;

                case "response.audio_transcript.delta":
                    var tx = JsonConvert.DeserializeObject<ResponseAudioTranscriptDelta>(text);
                    onAgentTranscript?.Invoke(tx.DeltaText);
                    break;

                case "input_audio_buffer.speech_started":
                    onUserSpeaking?.Invoke(true);
                    break;

                case "input_audio_buffer.speech_stopped":
                    onUserSpeaking?.Invoke(false);
                    break;

                case "error":
                    var err = JsonConvert.DeserializeObject<ErrorEvent>(text);
                    Debug.LogError($"[OpenAI] ERROR {err.Code}: {err.Message}");
                    break;

                default:
                    Debug.Log($"[OpenAI] Unhandled: {baseEvt.Type}");
                    break;
            }
        }

        #endregion

        #region WebSocket send

        private void SendMicChunk(string b64)
        {
            if (!_sessionReady || _ws.State != WebSocketState.Open) return;

            var payload = new Dictionary<string, object>
            {
                { "type",  "input_audio_buffer.append" },
                { "audio", b64 }
            };

            _ = _ws.SendText(JsonConvert.SerializeObject(payload));
        }

        private void SendSystemPrompt()
        {
            if (string.IsNullOrWhiteSpace(systemPrompt)) return;

            var upd = new Dictionary<string, object>
            {
                { "type", "session.update" },
                { "session", new Dictionary<string, object>
                    {
                        { "instructions", systemPrompt }
                    }
                }
            };

            _ = _ws.SendText(JsonConvert.SerializeObject(upd));
        }

        #endregion
    }
}
