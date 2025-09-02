using System;
using System.Globalization;
using UnityEngine;

namespace LineArtTools
{
	/// <summary>
	/// Hex color parsing (#rrggbb or #rrggbbaa) and safe utilities.
	/// </summary>
	public static class ColorUtil
	{
		public static bool TryParseHtml(string hex, out Color color)
		{
			color = Color.white;
			if (string.IsNullOrEmpty(hex)) return false;
			hex = hex.Trim();
			if (hex.StartsWith("#")) hex = hex.Substring(1);

			if (hex.Length == 6)
			{
				if (TryParseByte(hex.Substring(0, 2), out var r) &&
					TryParseByte(hex.Substring(2, 2), out var g) &&
					TryParseByte(hex.Substring(4, 2), out var b))
				{
					color = new Color(r / 255f, g / 255f, b / 255f, 1f);
					return true;
				}
			}
			else if (hex.Length == 8)
			{
				if (TryParseByte(hex.Substring(0, 2), out var r) &&
					TryParseByte(hex.Substring(2, 2), out var g) &&
					TryParseByte(hex.Substring(4, 2), out var b) &&
					TryParseByte(hex.Substring(6, 2), out var a))
				{
					color = new Color(r / 255f, g / 255f, b / 255f, a / 255f);
					return true;
				}
			}
			return false;
		}

		private static bool TryParseByte(string hex2, out byte value)
		{
			return byte.TryParse(hex2, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
		}
	}
}


