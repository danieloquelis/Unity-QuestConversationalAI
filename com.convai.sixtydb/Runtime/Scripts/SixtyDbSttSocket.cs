using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NativeWebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace SixtyDb
{
    // Wraps wss://api.60db.ai/ws/stt (browser mode, linear PCM 16k).
    // Lifecycle:
    //   connect()
    //     OnOpen -> server emits connection_established
    //     -> send 'start' -> server emits 'connected' -> OnReady
    //   per mic chunk: send { type: 'audio', audio: <b64>, encoding: 'linear', sample_rate: 16000 }
    //   server emits: speech_started, transcription (is_final + speech_final), session_stopped
    public sealed class SixtyDbSttSocket
    {
        public event Action OnReady;
        public event Action OnSpeechStarted;
        public event Action<string> OnPartialTranscript;
        public event Action<string> OnFinalTranscript;
        public event Action<string> OnError;
        public event Action OnClose;

        private readonly SixtyDbConfig _config;
        private WebSocket _ws;
        private bool _ready;

        public WebSocket Underlying => _ws;
        public bool IsReady => _ready;

        public SixtyDbSttSocket(SixtyDbConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task Connect()
        {
            var url = $"{_config.sttWebsocketUrl.TrimEnd('/')}?apiKey={Uri.EscapeDataString(_config.apiKey)}";
            _ws = new WebSocket(url);
            _ws.OnOpen    += HandleOpen;
            _ws.OnMessage += HandleMessage;
            _ws.OnError   += msg => OnError?.Invoke(msg);
            _ws.OnClose   += _ => { _ready = false; OnClose?.Invoke(); };
            await _ws.Connect();
        }

        public void DispatchMessageQueue() => _ws?.DispatchMessageQueue();

        public void SendAudioChunk(string base64Pcm)
        {
            if (_ws == null || _ws.State != WebSocketState.Open || !_ready) return;
            var payload = new Dictionary<string, object>
            {
                { "type", "audio" },
                { "audio", base64Pcm },
                { "encoding", "linear" },
                { "sample_rate", _config.sttSampleRate },
            };
            _ = _ws.SendText(JsonConvert.SerializeObject(payload));
        }

        public async Task Close()
        {
            try
            {
                if (_ws != null && _ws.State == WebSocketState.Open)
                {
                    // Best-effort 'stop' for billing summary; ignore failures.
                    await _ws.SendText("{\"type\":\"stop\"}");
                    await _ws.Close();
                }
            }
            catch { /* socket already gone */ }
        }

        private void HandleOpen()
        {
            // Wait for the server's 'connection_established' before sending 'start'.
            // That hand-off is done inside HandleMessage to keep the state machine local.
        }

        private void HandleMessage(byte[] bytes)
        {
            var text = Encoding.UTF8.GetString(bytes);
            JObject msg;
            try { msg = JObject.Parse(text); }
            catch { return; }

            switch (msg["type"]?.ToString())
            {
                case "connecting":
                    return;
                case "connection_established":
                    SendStart();
                    return;
                case "connected":
                    _ready = true;
                    OnReady?.Invoke();
                    return;
                case "speech_started":
                    OnSpeechStarted?.Invoke();
                    return;
                case "transcription":
                {
                    var transcript = msg["transcript"]?.ToString() ?? msg["text"]?.ToString();
                    if (string.IsNullOrEmpty(transcript)) return;
                    var isFinal     = (bool?)msg["is_final"] ?? false;
                    var speechFinal = (bool?)msg["speech_final"] ?? false;
                    if (isFinal && speechFinal) OnFinalTranscript?.Invoke(transcript);
                    else OnPartialTranscript?.Invoke(transcript);
                    return;
                }
                case "session_stopped":
                    _ready = false;
                    return;
                case "error":
                    OnError?.Invoke(msg["message"]?.ToString() ?? "unknown STT error");
                    return;
            }
        }

        private void SendStart()
        {
            var payload = new Dictionary<string, object>
            {
                { "type", "start" },
                { "encoding", "linear" },
                { "sample_rate", _config.sttSampleRate },
                { "utterance_end_ms", 500 },
                { "continuous_mode", true },
            };
            _ = _ws.SendText(JsonConvert.SerializeObject(payload));
        }
    }
}
