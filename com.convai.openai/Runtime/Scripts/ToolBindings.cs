using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace OpenAI
{
	/// <summary>
	/// Binds a JSON tools definition to runtime handlers on a target GameObject via reflection.
	/// - toolsJson: a TextAsset containing either {"tools":[...]} or a raw JSON array [...]
	/// - target: GameObject containing MonoBehaviours that implement methods named toolName (optionally prefixed)
	///   Accepted method signatures per tool: Task<JObject> Fn(JObject args), JObject Fn(JObject args), void Fn(JObject args)
	/// </summary>
	public sealed class ToolBindings : MonoBehaviour
	{
		[SerializeField] private TextAsset toolsJson;
		[SerializeField] private GameObject target;
		[SerializeField] private string methodPrefix = string.Empty;
		[SerializeField] private bool registerOnAwake = true;

		private void Awake()
		{
			if (registerOnAwake) RegisterAll();
		}

		public void RegisterAll()
		{
			if (toolsJson == null) { Debug.LogWarning("[OpenAI] ToolBindings.toolsJson is null"); return; }
			if (target == null) { Debug.LogWarning("[OpenAI] ToolBindings.target is null"); return; }

			JArray toolsArray;
			try
			{
				var root = JToken.Parse(toolsJson.text);
				toolsArray = root is JArray arr ? arr : (root["tools"] as JArray);
				if (toolsArray == null) { Debug.LogError("[OpenAI] toolsJson must be a JSON array or contain a 'tools' array"); return; }
			}
			catch (Exception ex)
			{
				Debug.LogError($"[OpenAI] Invalid toolsJson: {ex.Message}");
				return;
			}

			var behaviours = target.GetComponents<MonoBehaviour>();

			foreach (JObject spec in toolsArray.OfType<JObject>())
			{
				var name = spec["name"]?.ToString();
				if (string.IsNullOrWhiteSpace(name)) continue;
				var methodName = string.IsNullOrEmpty(methodPrefix) ? name : methodPrefix + name;

				// Find a method on any behaviour (case-insensitive)
				MethodInfo found = null;
				MonoBehaviour owner = null;
				foreach (var mb in behaviours)
				{
					var methods = mb.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					var mi = methods.FirstOrDefault(m => string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase)
							&& m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(JObject));
					if (mi != null) { found = mi; owner = mb; break; }
				}

				if (found == null)
				{
					Debug.LogWarning($"[OpenAI] No method found for tool '{name}' on target '{target.name}' (expected: {methodName}(JObject))");
					continue;
				}

				AgentToolRegistry.Register(name, async args =>
				{
					try
					{
						var res = found.Invoke(owner, new object[] { args ?? new JObject() });
						if (res is Task<JObject> tj) return await tj;
						if (res is JObject jo) return jo;
						return new JObject();
					}
					catch (Exception ex)
					{
						return new JObject { ["ok"] = false, ["error"] = ex.Message };
					}
				}, spec);
			}
		}
	}
}


