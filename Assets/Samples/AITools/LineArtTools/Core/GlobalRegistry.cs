using System.Collections.Generic;
using UnityEngine;

namespace LineArtTools
{
	/// <summary>
	/// Simple global registry for agent-accessible objects.
	/// </summary>
	public static class GlobalRegistry
	{
		private static readonly Dictionary<string, Transform> _idToTransform = new Dictionary<string, Transform>();
		private static readonly Dictionary<string, CharacterHandle> _idToCharacter = new Dictionary<string, CharacterHandle>();

		public static void RegisterTransform(string id, Transform t)
		{
			if (string.IsNullOrEmpty(id) || t == null) return;
			_idToTransform[id] = t;
		}

		public static Transform GetTransform(string id)
		{
			if (string.IsNullOrEmpty(id)) return null;
			_idToTransform.TryGetValue(id, out var t);
			return t;
		}

		public static void RegisterCharacter(string id, CharacterHandle handle)
		{
			if (string.IsNullOrEmpty(id) || handle == null) return;
			_idToCharacter[id] = handle;
		}

		public static CharacterHandle GetCharacter(string id)
		{
			if (string.IsNullOrEmpty(id)) return null;
			_idToCharacter.TryGetValue(id, out var h);
			return h;
		}
	}
}


