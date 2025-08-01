using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NativeWebSocket;
using Newtonsoft.Json;
using UnityEngine;

namespace ElevenLabs
{
    public class AgentConversationManager : MonoBehaviour
    {
        [Header("Agent Configuration")]
        [SerializeField] private string agentId = "<your_agent_id>";

        [Header("Dependencies")]
        [SerializeField] private ElevenLabsConfig config;
        [SerializeField] private MicrophoneStreamer micStreamer;
        [SerializeField] private PcmAudioPlayer audioPlayer;

        private WebSocket _websocket;
        private Coroutine _activityPingRoutine;
        
        private async void Start()
        {
            try
            {
                ElevenLabsClient.Create(config);
                var wsUrl = await ElevenLabsClient.Instance.GetAgentWebsocketUrlAsync(agentId);
                InitializeWebSocket(wsUrl);
                
                micStreamer.OnAudioChunk += OnMicChunk;
                await _websocket.Connect();
            }
            catch (Exception e)
            {
                Debug.LogError($"[AgentConversationManager] Startup failed: {e}");
                enabled = false;
            }
        }

        private void Update() => _websocket?.DispatchMessageQueue();

        private async void OnApplicationQuit()
        {
            await SafeCloseSocket();
        }
        
        private async void OnDisable()
        {
            if (_activityPingRoutine != null)
                StopCoroutine(_activityPingRoutine);

            await SafeCloseSocket();
            micStreamer.OnAudioChunk -= OnMicChunk;
        }
        
        private async void OnMicChunk(string chunk)
        {
            if (_websocket?.State != WebSocketState.Open) return;
            var payload = new Dictionary<string, object> { { "user_audio_chunk", chunk } };
            await _websocket.SendText(JsonConvert.SerializeObject(payload));
        }
        
        private void InitializeWebSocket(string url)
        {
            _websocket = new WebSocket(url);
            _websocket.OnOpen   += HandleWebSocketOpen;
            _websocket.OnMessage += HandleRawMessage;
            _websocket.OnClose  += HandleWebSocketClose;
        }

        private async void HandleWebSocketOpen()
        {
            Debug.Log("WebSocket connected");

            await SendInitiationData(); 
            micStreamer.StartStreaming();      

            _activityPingRoutine = StartCoroutine(ActivityPing());
        }

        private void HandleWebSocketClose(WebSocketCloseCode code)
        {
            Debug.Log($"WebSocket closed ({code})");

            micStreamer.StopStreaming();
            audioPlayer.StopImmediately();

            if (_activityPingRoutine != null)
                StopCoroutine(_activityPingRoutine);

            micStreamer.OnAudioChunk -= OnMicChunk; 
        }

        private async Task SafeCloseSocket()
        {
            try
            {
                if (_websocket != null && _websocket.State != WebSocketState.Closed)
                {
                    await _websocket.Close();
                }
            }
            catch { /* ignored */ }
        }
        
        private async Task SendInitiationData()
        {
            var init = new Dictionary<string, object>
            {
                { "type", "conversation_initiation_client_data" }
            };
            await _websocket.SendText(JsonConvert.SerializeObject(init));
        }

        private IEnumerator ActivityPing()
        {
            var json = JsonConvert.SerializeObject(new Dictionary<string, object> { { "type", "user_activity" } });
            while (_websocket is { State: WebSocketState.Open })
            {
                _ = _websocket.SendText(json); 
                yield return new WaitForSecondsRealtime(20f);
            }
        }

        /* ---------------------- Inbound events --------------------------- */

        private void HandleRawMessage(byte[] bytes)
        {
            HandleMessage(Encoding.UTF8.GetString(bytes));
        }

        private async void HandleMessage(string message)
        {
            var evt = JsonConvert.DeserializeObject<BaseEvent>(message);

            switch (evt.Type)
            {
                case "ping":
                    await HandlePingEvent(message);
                    break;
                case "audio":
                    HandleAudioEvent(message);
                    break;
                case "interruption":
                    audioPlayer.StopImmediately();
                    break;
                default:
                    Debug.Log($"Unhandled event type: {evt.Type}");
                    break;
            }
        }

        private async Task HandlePingEvent(string msg)
        {
            var ping = JsonConvert.DeserializeObject<PingEvent>(msg);

            var delay   = ping.PingEventData?.PingMs  ?? 0;
            var eventId = ping.PingEventData?.EventId ?? 0;
            if (delay > 0) await Task.Delay(delay);

            var pong = new Dictionary<string, object>
            {
                { "type", "pong" },
                { "event_id", eventId }
            };
            await _websocket.SendText(JsonConvert.SerializeObject(pong));
        }

        private void HandleAudioEvent(string msg)
        {
            var ar = JsonConvert.DeserializeObject<AudioResponseEvent>(msg);
            if (!string.IsNullOrEmpty(ar.AudioEvent?.AudioBase64))
                audioPlayer.EnqueueBase64Audio(ar.AudioEvent.AudioBase64);
        }
    }
}
