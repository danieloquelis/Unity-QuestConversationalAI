using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SixtyDb
{
    // Generic registry for 60db agent tools. Mirrors com.convai.openai's
    // AgentToolRegistry so existing scene wiring patterns port directly.
    //
    // Tool specs use the OpenAI-compatible JSON shape — 60db's
    // /v1/chat/completions accepts the same `tool` array.
    public static class SixtyDbToolRegistry
    {
        public delegate Task<JObject> ToolHandler(JObject args);

        private static readonly Dictionary<string, ToolHandler> NameToHandler =
            new(StringComparer.OrdinalIgnoreCase);
        private static readonly List<JObject> ToolSpecs = new();

        public static void Register(string name, ToolHandler handler, JObject toolSpec = null)
        {
            if (string.IsNullOrWhiteSpace(name) || handler == null) return;
            NameToHandler[name] = handler;
            if (toolSpec != null) ToolSpecs.Add(toolSpec);
        }

        public static bool TryGetHandler(string name, out ToolHandler handler) =>
            NameToHandler.TryGetValue(name, out handler);

        public static JArray GetToolsSpec() => new JArray(ToolSpecs);

        public static bool HasTools => ToolSpecs.Count > 0;
    }
}
