using System.Collections.Generic;
using System.Text;

namespace OpenAI
{
	/// <summary>
	/// Tracks tool-call names and incremental argument chunks by call_id.
	/// </summary>
	internal sealed class ToolCallState
	{
		private readonly Dictionary<string, StringBuilder> _argsByCallId = new Dictionary<string, StringBuilder>();
		private readonly Dictionary<string, string>        _nameByCallId = new Dictionary<string, string>();

		public void AppendArgs(string callId, string delta)
		{
			if (string.IsNullOrEmpty(callId)) return;
			if (!_argsByCallId.TryGetValue(callId, out var sb))
			{
				sb = new StringBuilder();
				_argsByCallId[callId] = sb;
			}
			sb.Append(delta ?? string.Empty);
		}

		public void SetToolName(string callId, string name)
		{
			if (string.IsNullOrEmpty(callId) || string.IsNullOrEmpty(name)) return;
			_nameByCallId[callId] = name;
		}

		public bool TryGetName(string callId, out string name)
		{
			return _nameByCallId.TryGetValue(callId, out name);
		}

		public string GetArgsJson(string callId)
		{
			if (callId == null) return null;
			return _argsByCallId.TryGetValue(callId, out var sb) ? sb.ToString() : null;
		}

		public void Clear(string callId)
		{
			if (string.IsNullOrEmpty(callId)) return;
			_argsByCallId.Remove(callId);
			_nameByCallId.Remove(callId);
		}
	}
}


