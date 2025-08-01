using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json;

public class AgentConversationManager : MonoBehaviour
{
    [Header("ElevenLabs Agent Id")]
    public string agentId = "<your_agent_id>";

    [Header("Scene References")]
    public MicrophoneStreamer micStreamer;
    public PcmAudioPlayer   audioPlayer;

    private WebSocket websocket;
    private Coroutine activityPingRoutine;

    private string Url => $"wss://api.elevenlabs.io/v1/convai/conversation?agent_id={agentId}";

    /**********************************************************************/
    /*                          Life-cycle                                */
    /**********************************************************************/

    private async void Start()
    {
        websocket = new WebSocket(Url);

        websocket.OnOpen += () =>
        {
            Debug.Log("WebSocket connected");
            SendInitiationData();
            micStreamer.StartStreaming();
            activityPingRoutine = StartCoroutine(ActivityPing());
        };

        websocket.OnMessage += HandleRawMessage;
        websocket.OnClose   += code =>
        {
            Debug.Log($"WebSocket closed ({code})");
            micStreamer.StopStreaming();
            audioPlayer.StopImmediately();
            if (activityPingRoutine != null) StopCoroutine(activityPingRoutine);
        };

        micStreamer.OnAudioChunk += async chunk =>
        {
            // Send the mic chunk immediately – server side will decide
            var payload = new Dictionary<string, object>
            {
                { "user_audio_chunk", chunk }
            };
            await websocket.SendText(JsonConvert.SerializeObject(payload));
        };

        await websocket.Connect();
    }

    private void Update() => websocket?.DispatchMessageQueue();

    private async void OnApplicationQuit()
    {
        try { await websocket?.Close(); } catch { /* ignored */ }
    }

    /**********************************************************************/
    /*                          Outbound                                  */
    /**********************************************************************/

    private async void SendInitiationData()
    {
        var payload = new Dictionary<string, object>
        {
            { "type", "conversation_initiation_client_data" }
        };
        await websocket.SendText(JsonConvert.SerializeObject(payload));
    }

    private IEnumerator ActivityPing()
    {
        var activity = new Dictionary<string, object> { { "type", "user_activity" } };
        var json = JsonConvert.SerializeObject(activity);

        while (websocket != null && websocket.State == WebSocketState.Open)
        {
            _ = websocket.SendText(json);           // don’t await inside IEnumerator
            yield return new WaitForSecondsRealtime(20f);
        }
    }


    /**********************************************************************/
    /*                          Inbound                                   */
    /**********************************************************************/

    private void HandleRawMessage(byte[] bytes)
    {
        string message = Encoding.UTF8.GetString(bytes);
        HandleMessage(message);
    }

    private async void HandleMessage(string message)
    {
        var baseEvent = JsonConvert.DeserializeObject<ElevenLabs.BaseEvent>(message);

        switch (baseEvent.Type)
        {
            /*–––––––– Ping / Pong ––––––––*/
            case "ping":
            {
                var ping = JsonConvert.DeserializeObject<ElevenLabs.PingEvent>(message);
                int delay   = ping.PingEventData?.PingMs  ?? 0;
                int eventId = ping.PingEventData?.EventId ?? 0;

                if (delay > 0) await System.Threading.Tasks.Task.Delay(delay);

                var pong = new Dictionary<string, object>
                {
                    { "type",     "pong" },
                    { "event_id", eventId }
                };
                await websocket.SendText(JsonConvert.SerializeObject(pong));
                break;
            }

            /*–––––––– Audio chunk ––––––––*/
            case "audio":
            {
                var audio = JsonConvert.DeserializeObject<ElevenLabs.AudioResponseEvent>(message);
                if (audio.AudioEvent?.AudioBase64 != null)
                    audioPlayer.EnqueueBase64Audio(audio.AudioEvent.AudioBase64);
                break;
            }

            /*–––––––– Agent interrupted ––––––––*/
            case "interruption":
            {
                // server says the user started speaking ⇒ stop TTS immediately
                audioPlayer.StopImmediately();   // flush queue + stop playback
                break;
            }

            /*–––––––– Other events ––––––––*/
            default:
                Debug.Log($"Unhandled event type: {baseEvent.Type}");
                break;
        }
    }
}
