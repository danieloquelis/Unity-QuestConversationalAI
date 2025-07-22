using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json;

public class AgentConversationManager : MonoBehaviour
{
    public string agentId = "<your_agent_id>";
    public MicrophoneStreamer micStreamer;
    public PcmAudioPlayer audioPlayer;

    private WebSocket websocket;

    private string url => $"wss://api.elevenlabs.io/v1/convai/conversation?agent_id={agentId}";

    async void Start()
    {
        websocket = new WebSocket(url);

        websocket.OnOpen += () =>
        {
            Debug.Log("WebSocket connected");
            SendInitiationData();
            micStreamer.StartStreaming();
        };

        websocket.OnMessage += (bytes) =>
        {
            string message = Encoding.UTF8.GetString(bytes);
            Debug.Log(message);
            HandleMessage(message);
        };

        websocket.OnClose += (code) =>
        {
            Debug.Log("WebSocket closed");
            micStreamer.StopStreaming();
            audioPlayer.Stop();
        };

        micStreamer.OnAudioChunk += async (chunk) =>
        {
            if (!audioPlayer.IsPlaying)
            {
                var payload = new Dictionary<string, object>
                {
                    { "user_audio_chunk", chunk },
                };
                string json = JsonConvert.SerializeObject(payload);
                Debug.Log($"Audiochunk -> {json}");
                await websocket.SendText(json);
            }
        };

        await websocket.Connect();
    }

    void Update()
    {
        websocket?.DispatchMessageQueue();
    }

    private async void SendInitiationData()
    {
        var payload = new Dictionary<string, object>
        {
            { "type", "conversation_initiation_client_data" }
        };

        string json = JsonConvert.SerializeObject(payload);
        await websocket.SendText(json);
    }

    private async void HandleMessage(string message)
    {
        var baseEvent = JsonConvert.DeserializeObject<ElevenLabs.BaseEvent>(message);
        switch (baseEvent.Type)
        {
            case "ping":
            {
                var pingEvent = JsonConvert.DeserializeObject<ElevenLabs.PingEvent>(message);
                int delayMs = pingEvent.PingEventData?.PingMs ?? 0;
                int eventId = pingEvent.PingEventData?.EventId ?? 0;

                if (delayMs > 0)
                    await System.Threading.Tasks.Task.Delay(delayMs);

                var pong = new Dictionary<string, object>
                {
                    { "type", "pong" },
                    { "event_id", eventId }
                };
                string pongJson = JsonConvert.SerializeObject(pong);
                await websocket.SendText(pongJson);
                break;
            }
            case "audio":
                var audioEvent = JsonConvert.DeserializeObject<ElevenLabs.AudioResponseEvent>(message);
                if (audioEvent.AudioEvent != null && audioEvent.AudioEvent.AudioBase64 != null)
                {
                    audioPlayer.EnqueueBase64Audio(audioEvent.AudioEvent.AudioBase64);
                }
                break;
            // Add other cases as needed for other event types
            default:
                Debug.Log($"Unhandled event type: {baseEvent.Type}");
                break;
        }
    }

    private void OnApplicationQuit()
    {
        websocket?.Close();
    }
}
