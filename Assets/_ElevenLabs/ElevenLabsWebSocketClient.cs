using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json;

public class ElevenLabsWebSocketClient : MonoBehaviour
{
    private WebSocket websocket;
    [SerializeField] private string agentId = "<your_agent_id>";
    private string url => $"wss://api.elevenlabs.io/v1/convai/conversation?agent_id={agentId}";

    async void Start()
    {
        websocket = new WebSocket(url);

        websocket.OnOpen += () => {
            Debug.Log("WebSocket connection opened");
            SendInitiationData();
        };

        websocket.OnMessage += (bytes) => {
            var message = Encoding.UTF8.GetString(bytes);
            Debug.Log("WebSocket message: " + message);
            HandleMessage(message);
        };

        websocket.OnError += (err) => Debug.LogError("WebSocket Error: " + err);
        websocket.OnClose += (code) => Debug.Log("WebSocket Closed: " + code);

        await websocket.Connect();
    }

    void Update()
    {
        if (websocket != null)
            websocket.DispatchMessageQueue();
    }

    private async void SendInitiationData()
    {
        var payload = new Dictionary<string, object>
        {
            { "event", "conversation_initiation_client_data" },
            { "data", new Dictionary<string, object>
                {
                    { "name", "Unity Client" },
                    { "version", Application.unityVersion },
                    { "device", SystemInfo.deviceModel }
                }
            }
        };

        string json = JsonConvert.SerializeObject(payload);
        await websocket.SendText(json);
    }

    private async void HandleMessage(string message)
    {
        try
        {
            var parsed = JsonConvert.DeserializeObject<Dictionary<string, object>>(message);
            if (parsed.TryGetValue("event", out var evtObj) && evtObj is string evt)
            {
                if (evt == "ping")
                {
                    var pongPayload = new Dictionary<string, object>
                    {
                        { "event", "pong" }
                    };

                    string json = JsonConvert.SerializeObject(pongPayload);
                    await websocket.SendText(json);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error parsing message: " + ex);
        }
    }

    private void OnApplicationQuit()
    {
        websocket.Close();
    }
}
