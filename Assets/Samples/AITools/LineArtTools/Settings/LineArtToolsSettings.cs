using UnityEngine;

namespace LineArtTools
{
	/// <summary>
	/// Global settings and limits for LineArtTools. Stored as a ScriptableObject.
	/// </summary>
	[CreateAssetMenu(fileName = "LineArtToolsSettings", menuName = "LineArtTools/Settings", order = 1)]
	public sealed class LineArtToolsSettings : ScriptableObject
	{
		private static LineArtToolsSettings _cachedInstance;

		// Defaults
		[Header("Style Defaults")]
		public Color defaultLineColor = Color.white;
		[Min(0f)] public float defaultLineWidth = 0.01f; // meters
		public bool defaultDepthTest = true;
		public bool defaultDashed = false;
		[Min(0f)] public float defaultDashSize = 0.05f;
		[Min(0f)] public float defaultGapSize = 0.05f;

		[Header("Limits")]
		[Min(0f)] public float minLineWidth = 0.001f;
		[Min(0f)] public float maxLineWidth = 0.05f;
		[Min(0f)] public float minSizeMeters = 0.001f;
		[Min(0f)] public float maxSizeMeters = 2.0f;
		[Min(3)] public int minCircleSegments = 8;
		[Min(3)] public int maxCircleSegments = 128;

		[Header("Pooling")]
		[Min(0)] public int initialLineRendererPool = 64;
		[Min(0)] public int maxLineRendererPool = 256;

		[Header("Materials (Optional Overrides)")]
		public Material defaultLineMaterial; // Unlit/alpha-blended
		public Material dashedLineMaterial;  // Optional dashed effect

		/// <summary>
		/// Loads settings from Resources if present, else returns an in-memory default instance.
		/// </summary>
		public static LineArtToolsSettings Instance
		{
			get
			{
				if (_cachedInstance != null) return _cachedInstance;
				_cachedInstance = Resources.Load<LineArtToolsSettings>("LineArtToolsSettings");
				if (_cachedInstance == null)
				{
					_cachedInstance = CreateInstance<LineArtToolsSettings>();
				}
				return _cachedInstance;
			}
		}
	}
}


