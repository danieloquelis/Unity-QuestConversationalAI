using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#else
using System.Net.WebSockets;
using System.Threading;
#endif

namespace MeshTools
{
    /// <summary>
    /// WebSocket client for communicating with the Python mesh tool server.
    /// Handles connection, message sending, and response parsing.
    /// </summary>
    public class WebSocketMeshClient : MonoBehaviour
    {
        [Header("Connection Settings")]
        [SerializeField] private string serverUrl = "ws://localhost:8765";
        [SerializeField] private bool autoConnect = true;
        [SerializeField] private float reconnectDelay = 5f;

        public bool IsConnected { get; private set; }
        public string ServerUrl => serverUrl;
        public event Action<bool> OnConnectionChanged;
        public event Action<string> OnError;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void WebSocketConnect(string url);
        
        [DllImport("__Internal")]
        private static extern void WebSocketSend(string message);
        
        [DllImport("__Internal")]
        private static extern void WebSocketClose();
#else
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cancellationTokenSource;
#endif

        private readonly Queue<TaskCompletionSource<JObject>> _pendingRequests = new Queue<TaskCompletionSource<JObject>>();
        private readonly StringBuilder _messageBuffer = new StringBuilder();

        private void Start()
        {
            if (autoConnect)
            {
                ConnectAsync();
            }
        }

        private void OnDestroy()
        {
            DisconnectAsync();
        }

        /// <summary>
        /// Connect to the mesh tool server
        /// </summary>
        public async void ConnectAsync()
        {
            try
            {
                await ConnectInternal();
            }
            catch (Exception e)
            {
                Debug.LogError($"MeshTools: Failed to connect to server: {e.Message}");
                OnError?.Invoke($"Connection failed: {e.Message}");
                
                if (autoConnect)
                {
                    // Retry connection after delay
                    await Task.Delay(TimeSpan.FromSeconds(reconnectDelay));
                    ConnectAsync();
                }
            }
        }

        private async Task ConnectInternal()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var url = serverUrl;
            WebSocketConnect(url);
            IsConnected = true;
#else
            _cancellationTokenSource = new CancellationTokenSource();
            _webSocket = new ClientWebSocket();
            
            var uri = new Uri(serverUrl);
            await _webSocket.ConnectAsync(uri, _cancellationTokenSource.Token);
            
            IsConnected = true;
            
            // Start listening for messages
            _ = Task.Run(ListenForMessages);
#endif
            
            OnConnectionChanged?.Invoke(true);
            Debug.Log($"MeshTools: Connected to server at {serverUrl}");
        }

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        public async void DisconnectAsync()
        {
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                WebSocketClose();
#else
                if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
                
                _cancellationTokenSource?.Cancel();
                _webSocket?.Dispose();
                _cancellationTokenSource?.Dispose();
#endif
                
                IsConnected = false;
                OnConnectionChanged?.Invoke(false);
                Debug.Log("MeshTools: Disconnected from server");
            }
            catch (Exception e)
            {
                Debug.LogError($"MeshTools: Error during disconnect: {e.Message}");
            }
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        private async Task ListenForMessages()
        {
            var buffer = new byte[8192]; // Increased buffer size
            
            try
            {
                while (_webSocket.State == WebSocketState.Open && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);
                    
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var messageFragment = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        _messageBuffer.Append(messageFragment);
                        
                        // If this is the end of the message, process it
                        if (result.EndOfMessage)
                        {
                            var completeMessage = _messageBuffer.ToString();
                            _messageBuffer.Clear();
                            HandleMessage(completeMessage);
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"MeshTools: Error listening for messages: {e.Message}");
                OnError?.Invoke($"Listen error: {e.Message}");
            }
            finally
            {
                IsConnected = false;
                OnConnectionChanged?.Invoke(false);
            }
        }
#endif

        private void HandleMessage(string message)
        {
            try
            {
                // Debug log for very large messages
                if (message.Length > 1000)
                {
                    Debug.Log($"MeshTools: Received large message ({message.Length} chars)");
                }
                
                var response = JObject.Parse(message);
                
                // Complete the next pending request
                if (_pendingRequests.Count > 0)
                {
                    var tcs = _pendingRequests.Dequeue();
                    tcs.SetResult(response);
                }
                else
                {
                    Debug.LogWarning("MeshTools: Received message but no pending requests");
                }
            }
            catch (JsonReaderException e)
            {
                Debug.LogError($"MeshTools: JSON parse error: {e.Message}");
                Debug.LogError($"MeshTools: Message length: {message.Length}");
                Debug.LogError($"MeshTools: Message preview: {message.Substring(0, Math.Min(200, message.Length))}...");
                OnError?.Invoke($"JSON parse error: {e.Message}");
                
                // Complete pending request with error
                if (_pendingRequests.Count > 0)
                {
                    var tcs = _pendingRequests.Dequeue();
                    tcs.SetResult(new JObject { ["success"] = false, ["error"] = "JSON parse error" });
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"MeshTools: Error handling message: {e.Message}");
                OnError?.Invoke($"Message handling error: {e.Message}");
                
                // Complete pending request with error
                if (_pendingRequests.Count > 0)
                {
                    var tcs = _pendingRequests.Dequeue();
                    tcs.SetResult(new JObject { ["success"] = false, ["error"] = "Message handling error" });
                }
            }
        }

        /// <summary>
        /// Send a command to the mesh tool server and wait for response
        /// </summary>
        public async Task<JObject> SendCommandAsync(string command, JObject parameters = null)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Not connected to mesh tool server");
            }

            var request = new JObject
            {
                ["command"] = command,
                ["params"] = parameters ?? new JObject()
            };

            var tcs = new TaskCompletionSource<JObject>();
            _pendingRequests.Enqueue(tcs);

            try
            {
                var message = request.ToString(Formatting.None);
                
#if UNITY_WEBGL && !UNITY_EDITOR
                WebSocketSend(message);
#else
                var bytes = Encoding.UTF8.GetBytes(message);
                await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
#endif

                // Wait for response with timeout
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException("Request timed out");
                }

                return await tcs.Task;
            }
            catch (Exception e)
            {
                // Remove the pending request on error
                if (_pendingRequests.Count > 0)
                    _pendingRequests.Dequeue();
                    
                throw new Exception($"Failed to send command '{command}': {e.Message}", e);
            }
        }

        /// <summary>
        /// Check if the server is reachable
        /// </summary>
        public async Task<bool> PingServerAsync()
        {
            try
            {
                var response = await SendCommandAsync("list_meshes");
                return response.Value<bool>("success");
            }
            catch
            {
                return false;
            }
        }
    }
}
