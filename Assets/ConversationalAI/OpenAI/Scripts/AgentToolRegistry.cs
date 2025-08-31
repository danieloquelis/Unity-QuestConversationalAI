using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace OpenAI
{
    /// <summary>
    /// Generic registry for agent tools used by GPT Realtime. Other systems can register tools at runtime.
    /// </summary>
    public static class AgentToolRegistry
    {
        public delegate Task<JObject> ToolHandler(JObject args);

        private static readonly Dictionary<string, ToolHandler> NameToHandler = new (StringComparer.OrdinalIgnoreCase);
        private static readonly List<JObject> ToolSpecs = new ();

        public static void Register(string name, ToolHandler handler, JObject toolSpec = null)
        {
            if (string.IsNullOrWhiteSpace(name) || handler == null) return;
            NameToHandler[name] = handler;
            if (toolSpec != null)
            {
                // Expecting a JSON schema per OpenAI Realtime/Responses tools format
                ToolSpecs.Add(toolSpec);
            }
        }

        public static bool TryGetHandler(string name, out ToolHandler handler) => NameToHandler.TryGetValue(name, out handler);

        public static JArray GetToolsSpec()
        {
            return new JArray(ToolSpecs);
        }
    }
}


