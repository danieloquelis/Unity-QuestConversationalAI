using UnityEngine;

namespace LineArtTools
{
	/// <summary>
	/// Utility functions for clamping and validating parameters to safe ranges.
	/// </summary>
	public static class Validation
	{
		public static float ClampSizeMeters(float value)
		{
			var s = LineArtToolsSettings.Instance;
			return Mathf.Clamp(value, s.minSizeMeters, s.maxSizeMeters);
		}

		public static float ClampLineWidth(float value)
		{
			var s = LineArtToolsSettings.Instance;
			return Mathf.Clamp(value, s.minLineWidth, s.maxLineWidth);
		}

		public static int ClampCircleSegments(int segments)
		{
			var s = LineArtToolsSettings.Instance;
			return Mathf.Clamp(segments, s.minCircleSegments, s.maxCircleSegments);
		}
	}
}


