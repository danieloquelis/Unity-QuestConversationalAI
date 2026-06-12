using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace SixtyDb
{
    // REST client for POST /v1/chat/completions. OpenAI-compatible payload,
    // so the tool array is passed through verbatim.
    //
    // Returns a small (content, toolCalls) tuple; SixtyDbConvManager owns the
    // tool-execution loop (dispatch → tool message → re-call until content).
    public sealed class SixtyDbChatClient
    {
        private readonly SixtyDbConfig _config;

        public SixtyDbChatClient(SixtyDbConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public struct Result
        {
            public string Content;
            public List<ToolCall> ToolCalls;
        }

        public struct ToolCall
        {
            public string Id;
            public string Name;
            public string ArgumentsJson;
        }

        public async Task<Result> CompleteAsync(
            List<JObject> messages,
            JArray tools,
            string model = null,
            float temperature = 0.7f,
            int topK = 20)
        {
            if (string.IsNullOrWhiteSpace(_config.apiKey))
                throw new InvalidOperationException("60db apiKey is not set in the config asset.");

            var body = new JObject
            {
                ["model"]       = model ?? _config.llmModel,
                ["messages"]    = new JArray(messages),
                ["stream"]      = false,
                ["temperature"] = temperature,
                ["top_k"]       = topK,
                ["chat_template_kwargs"] = new JObject { ["enable_thinking"] = false },
            };
            if (tools != null && tools.Count > 0) body["tool"] = tools;

            var url = $"{_config.apiBase.TrimEnd('/')}/v1/chat/completions";
            using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            var raw = Encoding.UTF8.GetBytes(body.ToString(Formatting.None));
            req.uploadHandler = new UploadHandlerRaw(raw) { contentType = "application/json" };
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Authorization", $"Bearer {_config.apiKey}");
            req.SetRequestHeader("Accept", "application/json");

            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
                throw new InvalidOperationException(
                    $"[SixtyDbChatClient] HTTP {(int)req.responseCode}: {req.error} / {req.downloadHandler?.text}");

            var json = JObject.Parse(req.downloadHandler.text);
            var msg = json["choices"]?[0]?["message"] as JObject ?? new JObject();

            var result = new Result
            {
                Content = msg["content"]?.ToString() ?? string.Empty,
                ToolCalls = new List<ToolCall>(),
            };

            if (msg["tool_calls"] is JArray toolCalls)
            {
                foreach (var tc in toolCalls)
                {
                    if (tc is not JObject obj) continue;
                    var fn = obj["function"] as JObject;
                    result.ToolCalls.Add(new ToolCall
                    {
                        Id            = obj["id"]?.ToString(),
                        Name          = fn?["name"]?.ToString(),
                        ArgumentsJson = fn?["arguments"]?.ToString() ?? "{}",
                    });
                }
            }

            return result;
        }

        // Convenience: build a {role, content} message in the shape /v1/chat/completions expects.
        public static JObject UserMessage(string text)   => new() { ["role"] = "user",   ["content"] = text };
        public static JObject SystemMessage(string text) => new() { ["role"] = "system", ["content"] = text };
        public static JObject AssistantMessage(string text) => new() { ["role"] = "assistant", ["content"] = text };
        public static JObject ToolMessage(string toolCallId, string name, string resultJson) =>
            new() { ["role"] = "tool", ["tool_call_id"] = toolCallId, ["name"] = name, ["content"] = resultJson };
    }
}
