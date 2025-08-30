using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace OpenAI
{
    /// <summary>
    /// Generic registry for agent tools used by GPT Realtime. Other systems can register tools at runtime.
    /// This file is independent from LineArtTools and safe to share across scenes.
    /// </summary>
    public static class AgentToolRegistry
    {
        public delegate Task<JObject> ToolHandler(JObject args);

        private static readonly Dictionary<string, ToolHandler> _nameToHandler = new Dictionary<string, ToolHandler>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<JObject> _toolSpecs = new List<JObject>();

        public static void Register(string name, ToolHandler handler, JObject toolSpec = null)
        {
            if (string.IsNullOrWhiteSpace(name) || handler == null) return;
            _nameToHandler[name] = handler;
            if (toolSpec != null)
            {
                // Expecting a JSON schema per OpenAI Realtime/Responses tools format
                _toolSpecs.Add(toolSpec);
            }
        }

        public static bool TryGetHandler(string name, out ToolHandler handler) => _nameToHandler.TryGetValue(name, out handler);

        public static JArray GetToolsSpec()
        {
            return new JArray(_toolSpecs);
        }
    }
}


