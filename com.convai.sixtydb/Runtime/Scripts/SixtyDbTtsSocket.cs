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
    // Wraps wss://api.60db.ai/ws/tts. Lifecycle:
    //   Connect()
    //     OnOpen -> server emits connection_established -> OnReady
    //   Speak(text):
    //     CreateContext if needed -> context_created
    //     send_text + flush_context -> audio_chunk frames -> flush_completed
    //   Interrupt(): close_context (drops the in-flight reply, used for barge-in).
    public sealed class SixtyDbTtsSocket
    {
        public event Action OnReady;
        public event Action OnSpeakStart;
        public event Action OnSpeakEnd;
        public event Action<string> OnAudioChunk;
        public event Action<string> OnError;
        public event Action OnClose;

        private readonly SixtyDbConfig _config;
        private readonly string _voiceId;
        private WebSocket _ws;
        private string _contextId;
        private TaskCompletionSource<bool> _contextReady;
        private int _chunksSinceFlush;

        public WebSocket Underlying => _ws;
        public bool ContextActive => !string.IsNullOrEmpty(_contextId);

        public SixtyDbTtsSocket(SixtyDbConfig config, string voiceId)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _voiceId = string.IsNullOrWhiteSpace(voiceId) ? config.defaultVoiceId : voiceId;
        }

        public async Task Connect()
        {
            var url = $"{_config.ttsWebsocketUrl.TrimEnd('/')}?apiKey={Uri.EscapeDataString(_config.apiKey)}";
            _ws = new WebSocket(url);
            _ws.OnMessage += HandleMessage;
            _ws.OnError   += msg => OnError?.Invoke(msg);
            _ws.OnClose   += _ => { _contextId = null; OnClose?.Invoke(); };
            await _ws.Connect();
        }

        public void DispatchMessageQueue() => _ws?.DispatchMessageQueue();

        public async Task Speak(string text)
        {
            if (_ws == null || _ws.State != WebSocketState.Open) return;
            if (string.IsNullOrEmpty(text)) return;

            if (string.IsNullOrEmpty(_contextId))
                await EnsureContext();

            _chunksSinceFlush = 0;
            await _ws.SendText(JsonConvert.SerializeObject(new Dictionary<string, object>
            {
                { "type", "send_text" }, { "text", text }, { "context_id", _contextId },
            }));
            await _ws.SendText(JsonConvert.SerializeObject(new Dictionary<string, object>
            {
                { "type", "flush_context" }, { "context_id", _contextId },
            }));
        }

        // Cancel the current reply mid-speech. Used by SixtyDbConvManager for
        // barge-in when the STT WS reports speech_started.
        public void Interrupt()
        {
            if (_ws == null || _ws.State != WebSocketState.Open || string.IsNullOrEmpty(_contextId))
                return;
            var json = JsonConvert.SerializeObject(new Dictionary<string, object>
            {
                { "type", "close_context" }, { "context_id", _contextId },
            });
            _ = _ws.SendText(json);
            _contextId = null;
        }

        public async Task Close()
        {
            Interrupt();
            try { if (_ws != null && _ws.State == WebSocketState.Open) await _ws.Close(); }
            catch { /* socket already gone */ }
        }

        private Task EnsureContext()
        {
            _contextReady = new TaskCompletionSource<bool>();
            _ = _ws.SendText(JsonConvert.SerializeObject(new Dictionary<string, object>
            {
                { "type", "create_context" },
                { "voice_id", _voiceId },
                { "audio_encoding", "LINEAR16" },
                { "sample_rate_hertz", _config.ttsSampleRate },
            }));
            return _contextReady.Task;
        }

        private void HandleMessage(byte[] bytes)
        {
            var text = Encoding.UTF8.GetString(bytes);
            JObject msg;
            try { msg = JObject.Parse(text); }
            catch { return; }

            switch (msg["type"]?.ToString())
            {
                case "connection_established":
                    OnReady?.Invoke();
                    return;
                case "context_created":
                    _contextId = msg["context_id"]?.ToString();
                    _contextReady?.TrySetResult(true);
                    return;
                case "audio_chunk":
                {
                    var b64 = msg["audioContent"]?.ToString() ?? msg["audio"]?.ToString();
                    if (string.IsNullOrEmpty(b64)) return;
                    if (_chunksSinceFlush == 0) OnSpeakStart?.Invoke();
                    _chunksSinceFlush++;
                    OnAudioChunk?.Invoke(b64);
                    return;
                }
                case "flush_completed":
                    OnSpeakEnd?.Invoke();
                    _chunksSinceFlush = 0;
                    return;
                case "error":
                    OnError?.Invoke(msg["message"]?.ToString() ?? "unknown TTS error");
                    return;
            }
        }
    }
}
