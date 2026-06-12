using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace SixtyDb
{
    // 60db conversation orchestrator. Mirrors the public surface of
    // ElevenLabs.AgentConversationManager and OpenAI.RealtimeConversationManager
    // so existing scene wiring (UnityEvents, PcmAudioPlayer, MicrophoneStreamer)
    // ports straight across.
    //
    // Per-turn loop:
    //   user mic chunk  ->  SttSocket
    //   final transcript -> SixtyDbChatClient (with tool registry)
    //     |- tool_calls present -> dispatch via SixtyDbToolRegistry -> re-call
    //   reply.content   ->  TtsSocket -> audio_chunk -> PcmAudioPlayer
    //
    // Barge-in: when STT emits speech_started while a TTS context is active,
    // close that context immediately + clear the audio player queue.
    public class SixtyDbConvManager : MonoBehaviour
    {
        [Header("Voice")]
        [Tooltip("60db voice id. Leave blank to use SixtyDbConfig.defaultVoiceId.")]
        [SerializeField] private string voiceId = "";

        [Header("System prompt")]
        [TextArea(2, 8)]
        [SerializeField] private string systemPrompt =
            "You are a helpful VR assistant. Keep replies under two short sentences " +
            "unless the user explicitly asks for depth. Sound human.";

        [Header("Startup")]
        [SerializeField] private bool startOnAwake = true;

        [Header("Behaviour")]
        [Tooltip("When true, the user speaking interrupts the agent (closes TTS context, clears playback).")]
        [SerializeField] private bool bargeInEnabled = true;
        [Tooltip("Max tool-call round-trips per turn before falling back to plain content.")]
        [SerializeField] private int maxToolRounds = 2;

        [Header("Dependencies")]
        [SerializeField] private SixtyDbConfig config;
        [SerializeField] private MicrophoneStreamer micStreamer;
        [SerializeField] private PcmAudioPlayer audioPlayer;

        [Header("Events")]
        public UnityEvent<string> onAgentTranscript;
        public UnityEvent<string> onUserTranscript;
        public UnityEvent<string> onUserPartialTranscript;
        public UnityEvent<bool>   onAgentSpeaking;

        private SixtyDbSttSocket _stt;
        private SixtyDbTtsSocket _tts;
        private SixtyDbChatClient _chat;
        private readonly List<JObject> _history = new();
        private bool _busy;

        private void Start()
        {
            if (startOnAwake) StartAgent();
        }

        private void Update()
        {
            _stt?.DispatchMessageQueue();
            _tts?.DispatchMessageQueue();
        }

        private async void OnDisable()         => await Shutdown();
        private async void OnApplicationQuit() => await Shutdown();

        // Same external surface as the OpenAI / ElevenLabs managers.
        public async void StartAgent()
        {
            try
            {
                if (!config) throw new InvalidOperationException("SixtyDbConfig is not assigned.");
                if (audioPlayer == null) throw new InvalidOperationException("PcmAudioPlayer is not assigned.");
                if (micStreamer == null) throw new InvalidOperationException("MicrophoneStreamer is not assigned.");

                _history.Clear();
                _history.Add(SixtyDbChatClient.SystemMessage(systemPrompt));

                _chat = new SixtyDbChatClient(config);
                _stt  = new SixtyDbSttSocket(config);
                _tts  = new SixtyDbTtsSocket(config, voiceId);

                WireSttEvents();
                WireTtsEvents();
                micStreamer.OnAudioChunk += OnMicChunk;

                await Task.WhenAll(_stt.Connect(), _tts.Connect());
                micStreamer.StartStreaming();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SixtyDb] Startup failed: {ex}");
                enabled = false;
            }
        }

        public async void InterruptNow()
        {
            _tts?.Interrupt();
            audioPlayer.StopImmediately();
            onAgentSpeaking?.Invoke(false);
            await Task.Yield();
        }

        private async Task Shutdown()
        {
            try
            {
                micStreamer.OnAudioChunk -= OnMicChunk;
                micStreamer?.StopStreaming();
                audioPlayer?.StopImmediately();
                if (_stt != null) await _stt.Close();
                if (_tts != null) await _tts.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SixtyDb] Shutdown error: {ex}");
            }
        }

        private void OnMicChunk(string base64Pcm) => _stt?.SendAudioChunk(base64Pcm);

        private void WireSttEvents()
        {
            _stt.OnSpeechStarted += () =>
            {
                if (!bargeInEnabled) return;
                if (_tts == null || !_tts.ContextActive) return;
                _tts.Interrupt();
                audioPlayer.StopImmediately();
                onAgentSpeaking?.Invoke(false);
            };

            _stt.OnPartialTranscript += text => onUserPartialTranscript?.Invoke(text);

            _stt.OnFinalTranscript += async text =>
            {
                if (_busy || string.IsNullOrWhiteSpace(text)) return;
                onUserTranscript?.Invoke(text);
                _history.Add(SixtyDbChatClient.UserMessage(text));

                _busy = true;
                try { await RunTurn(); }
                catch (Exception ex) { Debug.LogError($"[SixtyDb] Turn failed: {ex}"); }
                finally { _busy = false; }
            };

            _stt.OnError += msg => Debug.LogError($"[SixtyDb] STT error: {msg}");
        }

        private void WireTtsEvents()
        {
            _tts.OnAudioChunk += audioPlayer.EnqueueBase64Audio;
            _tts.OnSpeakStart += () => onAgentSpeaking?.Invoke(true);
            _tts.OnSpeakEnd   += () => onAgentSpeaking?.Invoke(false);
            _tts.OnError      += msg => Debug.LogError($"[SixtyDb] TTS error: {msg}");
        }

        private async Task RunTurn()
        {
            var tools = SixtyDbToolRegistry.HasTools ? SixtyDbToolRegistry.GetToolsSpec() : null;

            for (var round = 0; round < maxToolRounds; round++)
            {
                var reply = await _chat.CompleteAsync(_history, tools);

                if (reply.ToolCalls.Count == 0)
                {
                    var content = reply.Content ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(content)) return;
                    _history.Add(SixtyDbChatClient.AssistantMessage(content));
                    onAgentTranscript?.Invoke(content);
                    await _tts.Speak(content);
                    return;
                }

                // Append the assistant's tool-call turn, then run each tool locally
                // and append the result so the next round can incorporate it.
                _history.Add(SixtyDbChatClient.AssistantMessage(reply.Content ?? string.Empty));
                foreach (var call in reply.ToolCalls)
                {
                    var argsJson = call.ArgumentsJson;
                    JObject args;
                    try { args = JObject.Parse(string.IsNullOrEmpty(argsJson) ? "{}" : argsJson); }
                    catch { args = new JObject(); }

                    string resultJson;
                    try
                    {
                        if (SixtyDbToolRegistry.TryGetHandler(call.Name, out var handler))
                        {
                            var res = await handler(args);
                            resultJson = (res ?? new JObject()).ToString(Newtonsoft.Json.Formatting.None);
                        }
                        else
                        {
                            resultJson = $"{{\"error\":\"unknown tool '{call.Name}'\"}}";
                        }
                    }
                    catch (Exception ex)
                    {
                        resultJson = $"{{\"error\":\"{ex.Message}\"}}";
                    }
                    _history.Add(SixtyDbChatClient.ToolMessage(call.Id, call.Name, resultJson));
                }
            }
            Debug.LogWarning("[SixtyDb] Tool round budget exhausted; aborting turn.");
        }
    }
}
