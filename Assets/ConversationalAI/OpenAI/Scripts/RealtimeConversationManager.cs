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
    public class RealtimeConversationManager : MonoBehaviour
    {
        #region Inspector

        [Header("Model")]
        [SerializeField] private string model = "gpt-realtime";

        private enum Voice { Alloy, Verse, Custom }

        [Header("Voice")]
        [SerializeField] private Voice voice = Voice.Alloy;
        [SerializeField] private string customVoice = "";

        [Header("System prompt")]
        [TextArea(2, 8)]
        [SerializeField] private string systemPrompt = "You are a helpful assistant who answers briefly.";

        [Header("Startup")]
        [SerializeField] private bool startOnAwake = true;

        [Header("Dependencies")]
        [SerializeField] private OpenAIConfig       config;
        [SerializeField] private MicrophoneStreamer micStreamer;
        [SerializeField] private PcmAudioPlayer     audioPlayer;

        [Header("Thinking SFX")]
        [SerializeField] private AudioSource thinkingAudioSource;
        [SerializeField] private AudioClip thinkingLoopClip;
        [Range(0f, 1f)]
        [SerializeField] private float thinkingLoopVolume = 0.25f;

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
        private bool      _responseActive;
        private bool      _responseCreateSent;
        private double    _lastCreateSentTime;
        private const double MinCreateIntervalSec = 0.2;

        private readonly ToolCallState _toolCalls = new ();
        private int _thinkingDepth = 0;

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
                InitializeWebSocket();
                RegisterSocketCallbacks();
                micStreamer.OnAudioChunk += SendMicChunk;
                await _ws.Connect();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OpenAI] Startup failed: {ex}");
                enabled = false;
            }
        }

        public void InterruptNow()
        {
            TryCancelAssistantResponse();
        }

        #endregion

        #region Shutdown

        private async Task Shutdown()
        {
            try
            {
                micStreamer.OnAudioChunk -= SendMicChunk;
                micStreamer.StopStreaming();
                audioPlayer.StopImmediately();
                StopThinkingSfx(true);

                if (_ws != null)
                {
                    if (_ws.State == WebSocketState.Open)
                        await _ws.Close();
                    _ws = null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OpenAI] Shutdown error: {ex}");
            }
        }

        #endregion

        #region WebSocket receive

        private void OnSocketMessage(byte[] raw)
        {
            var text = Encoding.UTF8.GetString(raw);

            BaseEvent baseEvt = null;
            try { baseEvt = JsonConvert.DeserializeObject<BaseEvent>(text); }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OpenAI] Failed to parse event: {ex}\n{text}");
                return;
            }

            switch (baseEvt?.Type)
            {
                case "session.created":
                    HandleSessionCreated();
                    break;

                case "session.updated":
                    HandleSessionUpdated();
                    break;

                case "input_audio_buffer.speech_started":
                    HandleSpeechStarted();
                    break;

                case "input_audio_buffer.speech_stopped":
                    HandleSpeechStopped();
                    break;

                case "input_audio_buffer.committed":
                    HandleInputCommitted();
                    break;

                case "conversation.item.input_audio_transcription.delta":
                    HandleInputTranscriptDelta(text);
                    break;

                case "conversation.item.input_audio_transcription.completed":
                    HandleInputTranscriptCompleted(text);
                    break;

                case "conversation.item.created":
                    HandleConversationItemCreated();
                    break;

                case "response.audio.delta":
                    HandleResponseAudioDelta(text);
                    break;

                case "response.audio.done":
                    HandleResponseAudioDone();
                    break;

                case "response.audio_transcript.delta":
                    HandleResponseTranscriptDelta(text);
                    break;

                case "response.audio_transcript.done":
                case "response.content_part.added":
                case "response.content_part.done":
                    HandleNoop();
                    break;

                case "response.created":
                    HandleResponseCreated();
                    break;

                case "response.done":
                case "response.cancelled":
                    HandleResponseFinished();
                    break;

                case "response.function_call_arguments.delta":
                    HandleFunctionArgsDelta(text);
                    break;

                case "response.function_call_arguments.done":
                    HandleNoop();
                    break;

                case "response.output_item.added":
                    HandleOutputItemAdded(text);
                    break;

                case "response.output_item.done":
                    HandleOutputItemDone(text);
                    break;

                case "rate_limits.updated":
                    HandleNoop();
                    break;

                case "error":
                    HandleError(text);
                    break;

                default:
                    Debug.Log($"[OpenAI] Unhandled event type: {baseEvt?.Type}");
                    break;
            }
        }

        #endregion

        #region WebSocket send

        private void SendMicChunk(string b64)
        {
            if (!_sessionReady || _ws == null || _ws.State != WebSocketState.Open) return;

            var payload = new Dictionary<string, object>
            {
                { "type",  "input_audio_buffer.append" },
                { "audio", b64 }
            };

            _ = _ws.SendText(JsonConvert.SerializeObject(payload));
        }

        private void SendSessionUpdate()
        {
            // server VAD on, but we create responses ourselves at speech stop
            var session = new JObject
            {
                ["instructions"] = systemPrompt ?? string.Empty,
                ["modalities"]   = new JArray("text", "audio"),
                ["voice"]        = ResolveVoiceString(),
                ["input_audio_transcription"] = new JObject { ["model"] = "gpt-4o-mini-transcribe" },
                ["turn_detection"] = new JObject
                {
                    ["type"] = "server_vad",
                    ["create_response"] = false
                },
                ["tools"] = AgentToolRegistry.GetToolsSpec()
            };

            var upd = new JObject { ["type"] = "session.update", ["session"] = session };
            _ = _ws.SendText(upd.ToString(Formatting.None));
        }

        private void SendResponseCreate()
        {
            if (_ws == null || _ws.State != WebSocketState.Open) return;
            if (_responseActive || _responseCreateSent) return;

            var now = Time.realtimeSinceStartupAsDouble;
            if (now - _lastCreateSentTime < MinCreateIntervalSec) return;

            _lastCreateSentTime = now;
            _responseCreateSent = true;

            var commit = new JObject { ["type"] = "input_audio_buffer.commit" };
            _ = _ws.SendText(commit.ToString(Formatting.None));

            var create = new JObject { ["type"] = "response.create" };
            _ = _ws.SendText(create.ToString(Formatting.None));
        }

        private void TryCancelAssistantResponse()
        {
            if (_ws == null || _ws.State != WebSocketState.Open) return;

            var cancel = new JObject { ["type"] = "response.cancel" };
            _ = _ws.SendText(cancel.ToString(Formatting.None));

            audioPlayer.StopImmediately();
            StopThinkingSfx(true);

            if (_agentCurrentlySpeaking)
            {
                _agentCurrentlySpeaking = false;
                onAgentSpeaking?.Invoke(false);
            }
            _responseActive = false;
            _responseCreateSent = false;
        }

        #endregion

        #region Tool execution

        private async Task ExecuteToolCallIfReady(string callId)
        {
            try
            {
                if (!_toolCalls.TryGetName(callId, out var toolName))
                {
                    Debug.LogWarning($"[OpenAI] function_call done without known name (call_id={callId})");
                    StopThinkingSfx();
                    return;
                }

                var argsJson = _toolCalls.GetArgsJson(callId) ?? "{}";

                if (!AgentToolRegistry.TryGetHandler(toolName, out var handler))
                {
                    Debug.LogWarning($"[OpenAI] Tool not registered: {toolName}");
                    await SendToolOutputAndContinue(callId, "");
                    return;
                }

                JObject args;
                try { args = string.IsNullOrWhiteSpace(argsJson) ? new JObject() : JObject.Parse(argsJson); }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[OpenAI] Tool args parse failed for {toolName}: {ex.Message}\n{argsJson}");
                    args = new JObject();
                }

                object resultAny = await handler(args);

                string outputStr;
                if (resultAny == null) outputStr = string.Empty;
                else if (resultAny is string s) outputStr = s;
                else if (resultAny is JToken jt) outputStr = jt.ToString(Formatting.None);
                else outputStr = JsonConvert.SerializeObject(resultAny, Formatting.None);

                await SendToolOutputAndContinue(callId, outputStr);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OpenAI] Tool execution failed: {ex}");
                await SendToolOutputAndContinue(callId, JsonConvert.SerializeObject(new { ok = false, error = ex.Message }));
            }
            finally
            {
                _toolCalls.Clear(callId);
                StopThinkingSfx();
            }
        }

        private async Task SendToolOutputAndContinue(string callId, string outputStr)
        {
            if (_ws == null || _ws.State != WebSocketState.Open) return;

            var toolResultItem = new JObject
            {
                ["type"] = "conversation.item.create",
                ["item"] = new JObject
                {
                    ["type"]    = "function_call_output",
                    ["call_id"] = callId ?? string.Empty,
                    ["output"]  = outputStr ?? string.Empty
                }
            };
            await _ws.SendText(toolResultItem.ToString(Formatting.None));

            var create = new JObject { ["type"] = "response.create" };
            await _ws.SendText(create.ToString(Formatting.None));
        }

        #endregion

        #region Helpers

        private string ResolveVoiceString()
        {
            switch (voice)
            {
                case Voice.Alloy:  return "alloy";
                case Voice.Verse:  return "verse";
                case Voice.Custom: return string.IsNullOrWhiteSpace(customVoice) ? "alloy" : customVoice.Trim();
                default:           return "alloy";
            }
        }

        private void StartThinkingSfx()
        {
            _thinkingDepth++;
            if (thinkingAudioSource == null || thinkingLoopClip == null) return;

            if (!thinkingAudioSource.isPlaying)
            {
                thinkingAudioSource.clip   = thinkingLoopClip;
                thinkingAudioSource.loop   = true;
                thinkingAudioSource.volume = thinkingLoopVolume;
                thinkingAudioSource.Play();
            }
        }

        private void StopThinkingSfx(bool forceStopAll = false)
        {
            if (forceStopAll) _thinkingDepth = 0;
            else _thinkingDepth = Mathf.Max(0, _thinkingDepth - 1);

            if (_thinkingDepth == 0 && thinkingAudioSource != null && thinkingAudioSource.isPlaying)
            {
                thinkingAudioSource.Stop();
            }
        }

        #endregion

        #region Socket helpers

        private void InitializeWebSocket()
        {
            var url = $"{config.realtimeConvWebsocketUrl}?model={Uri.EscapeDataString(model)}";
            var headers = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {config.apiKey}" },
                { "OpenAI-Beta",   "realtime=v1" }
            };
            _ws = new WebSocket(url, headers);
        }

        private void RegisterSocketCallbacks()
        {
            _ws.OnOpen    += () => Debug.Log("[OpenAI] WS connected.");
            _ws.OnClose   += code => Debug.Log($"[OpenAI] WS closed ({code})");
            _ws.OnError   += err  => Debug.LogError($"[OpenAI] WS error: {err}");
            _ws.OnMessage += OnSocketMessage;
        }

        #endregion

        #region Event handlers (parsed)

        private void HandleSessionCreated()
        {
            _sessionReady = true;
            SendSessionUpdate();
            micStreamer.StartStreaming();
        }

        private void HandleSessionUpdated() { }

        private void HandleSpeechStarted() => onUserSpeaking?.Invoke(true);

        private void HandleSpeechStopped()
        {
            onUserSpeaking?.Invoke(false);
            SendResponseCreate();
        }

        private static void HandleInputCommitted() { }

        private void HandleInputTranscriptDelta(string json)
        {
            try
            {
                var jo = JObject.Parse(json);
                var delta = jo["delta"]?.ToString();
                if (!string.IsNullOrEmpty(delta)) onUserTranscript?.Invoke(delta);
            } catch {}
        }

        private void HandleInputTranscriptCompleted(string json)
        {
            try
            {
                var user = JsonConvert.DeserializeObject<InputAudioTranscriptDone>(json);
                onUserTranscript?.Invoke(user?.Text ?? "");
            } catch {}
        }

        private static void HandleConversationItemCreated() { }

        private void HandleResponseAudioDelta(string json)
        {
            if (!_agentCurrentlySpeaking)
            {
                _agentCurrentlySpeaking = true;
                onAgentSpeaking?.Invoke(true);
            }
            try
            {
                var audio = JsonConvert.DeserializeObject<ResponseAudioDelta>(json);
                if (!string.IsNullOrEmpty(audio?.DeltaBase64))
                    audioPlayer.EnqueueBase64Audio(audio.DeltaBase64);
            } catch { 
                // ignore
            }
        }

        private void HandleResponseAudioDone()
        {
            _agentCurrentlySpeaking = false;
            onAgentSpeaking?.Invoke(false);
        }

        private void HandleResponseTranscriptDelta(string json)
        {
            try
            {
                var tx = JsonConvert.DeserializeObject<ResponseAudioTranscriptDelta>(json);
                if (!string.IsNullOrEmpty(tx?.DeltaText))
                    onAgentTranscript?.Invoke(tx.DeltaText);
            } catch {}
        }

        private void HandleResponseCreated()
        {
            _responseActive = true;
            _responseCreateSent = false;
        }

        private void HandleResponseFinished()
        {
            _responseActive = false;
            _responseCreateSent = false;
        }

        private void HandleFunctionArgsDelta(string json)
        {
            try
            {
                var obj    = JObject.Parse(json);
                var callId = obj["call_id"]?.ToString();
                var delta  = obj["delta"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(callId)) _toolCalls.AppendArgs(callId, delta);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OpenAI] parse arguments.delta failed: {ex}");
            }
        }

        private void HandleOutputItemAdded(string json)
        {
            try
            {
                var obj  = JObject.Parse(json);
                var item = obj["item"] as JObject;
                if (item?["type"]?.ToString() == "function_call")
                {
                    var callId = item["call_id"]?.ToString();
                    var name   = item["name"]?.ToString();
                    if (!string.IsNullOrEmpty(callId) && !string.IsNullOrEmpty(name))
                        _toolCalls.SetToolName(callId, name);

                    StartThinkingSfx();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OpenAI] parse output_item.added failed: {ex}");
            }
        }

        private void HandleOutputItemDone(string json)
        {
            try
            {
                var obj  = JObject.Parse(json);
                var item = obj["item"] as JObject;
                if (item?["type"]?.ToString() == "function_call")
                {
                    var callId = item["call_id"]?.ToString();
                    if (!string.IsNullOrEmpty(callId)) _ = ExecuteToolCallIfReady(callId);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OpenAI] parse output_item.done failed: {ex}");
            }
        }

        private void HandleError(string json)
        {
            var err = JsonConvert.DeserializeObject<ErrorEvent>(json);
            var code = err?.Error?.Code    ?? "(no code)";
            var msg  = err?.Error?.Message ?? "(no message)";
            Debug.LogError($"[OpenAI] ERROR {code}: {msg}");
            if (code == "conversation_already_has_active_response")
            {
                _responseActive = true;
                _responseCreateSent = false;
            }
            StopThinkingSfx();
        }

        private static void HandleNoop() { }

        #endregion
    }
}
