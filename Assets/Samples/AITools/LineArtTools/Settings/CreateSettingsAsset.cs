#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace LineArtTools
{
	public static class CreateSettingsAsset
	{
		[InitializeOnLoadMethod]
		private static void CreateIfMissing()
		{
			var existing = Resources.Load<LineArtToolsSettings>("LineArtToolsSettings");
			if (existing != null) return;
			var settings = ScriptableObject.CreateInstance<LineArtToolsSettings>();
			settings.defaultLineColor = Color.white;
			settings.defaultLineWidth = 0.012f;
			settings.defaultDepthTest = true;
			if (!AssetDatabase.IsValidFolder("Assets/Resources"))
			{
				AssetDatabase.CreateFolder("Assets", "Resources");
			}
			AssetDatabase.CreateAsset(settings, "Assets/Resources/LineArtToolsSettings.asset");
			AssetDatabase.SaveAssets();
		}

		[MenuItem("Tools/LineArtTools/Create Settings Asset")]
		public static void CreateOrSelect()
		{
			var existing = Resources.Load<LineArtToolsSettings>("LineArtToolsSettings");
			if (existing != null)
			{
				Selection.activeObject = existing;
				EditorGUIUtility.PingObject(existing);
				return;
			}

			var settings = ScriptableObject.CreateInstance<LineArtToolsSettings>();
			settings.defaultLineColor = Color.white;
			settings.defaultLineWidth = 0.012f;
			settings.defaultDepthTest = true;

			if (!AssetDatabase.IsValidFolder("Assets/Resources"))
			{
				AssetDatabase.CreateFolder("Assets", "Resources");
			}
			var path = "Assets/Resources/LineArtToolsSettings.asset";
			AssetDatabase.CreateAsset(settings, path);
			AssetDatabase.SaveAssets();
			Selection.activeObject = settings;
			EditorGUIUtility.PingObject(settings);
		}
	}
}
#endif
