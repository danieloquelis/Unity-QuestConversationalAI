using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace ElevenLabs
{
    public sealed class ElevenLabsClient
    {
        private static ElevenLabsClient _instance;
        public static ElevenLabsClient Instance =>
            _instance ?? throw new InvalidOperationException(
                "Call ElevenLabsClient.Create(config) before first use.");

        public static ElevenLabsClient Create(ElevenLabsConfig config)
        {
            if (!config) throw new ArgumentNullException(nameof(config));
            _instance = new ElevenLabsClient(config);
            return _instance;
        }
        
        private readonly ElevenLabsConfig _config;

        private ElevenLabsClient(ElevenLabsConfig config) => _config = config;
        
        public async Task<string> GetAgentWebsocketUrlAsync(string agentId)
        {
            if (string.IsNullOrWhiteSpace(agentId))
                throw new ArgumentException("agentId must not be empty.", nameof(agentId));

            if (string.IsNullOrWhiteSpace(_config.apiKey))
                throw new InvalidOperationException("ElevenLabs apiKey is not set in the config asset.");

            if (string.IsNullOrWhiteSpace(_config.signedWebsocketUrl))
                throw new InvalidOperationException("signedWebsocketUrl is not set in the config asset.");

            var requestUrl = $"{_config.signedWebsocketUrl}?agent_id={Uri.EscapeDataString(agentId)}";

            using var req = UnityWebRequest.Get(requestUrl);
            req.SetRequestHeader("xi-api-key", _config.apiKey);

            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
                throw new InvalidOperationException(
                    $"[GetAgentWebsocketUrl] HTTP {(int)req.responseCode}: {req.error}");

            var payload = JsonConvert.DeserializeObject<SignedUrlResponse>(req.downloadHandler.text);
            if (payload == null || string.IsNullOrWhiteSpace(payload.signedUrl))
                throw new InvalidOperationException("[GetAgentWebsocketUrl] No 'signed_url' in response.");

            return payload.signedUrl;
        }
        
        [Serializable]
        private class SignedUrlResponse
        {
            [JsonProperty("signed_url")]
            public string signedUrl;
        }
    }
}
