using UnityEngine;

namespace LineArtTools
{
	/// <summary>
	/// Handle to a created stroke. Keeps identity and renderer reference.
	/// </summary>
	public sealed class StrokeHandle
	{
		public string Id { get; }
		public LineRenderer Renderer { get; }

		public StrokeHandle(string id, LineRenderer renderer)
		{
			Id = id;
			Renderer = renderer;
		}
	}
}


