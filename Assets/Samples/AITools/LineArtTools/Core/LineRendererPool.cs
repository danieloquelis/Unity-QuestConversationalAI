using System.Collections.Generic;
using UnityEngine;

namespace LineArtTools
{
	/// <summary>
	/// Simple pool for LineRenderer components to reduce allocations.
	/// </summary>
	public sealed class LineRendererPool
	{
		private readonly Transform _poolRoot;
		private readonly Stack<LineRenderer> _available = new Stack<LineRenderer>();
		private int _createdCount;

		public LineRendererPool(string poolName, Transform parent)
		{
			var go = new GameObject(poolName);
			go.hideFlags = HideFlags.HideInHierarchy;
			go.transform.SetParent(parent, false);
			_poolRoot = go.transform;
			Prewarm();
		}

		private void Prewarm()
		{
			var settings = LineArtToolsSettings.Instance;
			for (int i = 0; i < settings.initialLineRendererPool; i++)
			{
				_available.Push(CreateNew());
			}
		}

		private LineRenderer CreateNew()
		{
			_createdCount++;
			var go = new GameObject($"LineRenderer_{_createdCount}");
			go.transform.SetParent(_poolRoot, false);
			var lr = go.AddComponent<LineRenderer>();
			ConfigureDefaults(lr);
			return lr;
		}

		private static void ConfigureDefaults(LineRenderer lr)
		{
			var s = LineArtToolsSettings.Instance;
			lr.useWorldSpace = true;
			lr.loop = false;
			lr.textureMode = LineTextureMode.Stretch;
			lr.numCornerVertices = 0;
			lr.numCapVertices = 0;
			lr.positionCount = 0;
			lr.startWidth = s.defaultLineWidth;
			lr.endWidth = s.defaultLineWidth;
			lr.startColor = s.defaultLineColor;
			lr.endColor = s.defaultLineColor;
			// Material selection with robust fallbacks (avoid null shader on device)
			Material chosen = s.defaultLineMaterial;
			if (chosen == null)
			{
				var urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
				if (urpUnlit != null)
				{
					chosen = new Material(urpUnlit);
				}
				else
				{
					var spritesDefault = Shader.Find("Sprites/Default");
					if (spritesDefault != null)
					{
						chosen = new Material(spritesDefault);
					}
				}
			}
			if (chosen != null)
			{
				chosen.color = s.defaultLineColor;
				lr.material = chosen;
			}
		}

		public LineRenderer Get()
		{
			if (_available.Count > 0)
			{
				var lr = _available.Pop();
				lr.gameObject.SetActive(true);
				return lr;
			}
			if (_createdCount < LineArtToolsSettings.Instance.maxLineRendererPool)
			{
				var lr = CreateNew();
				lr.gameObject.SetActive(true);
				return lr;
			}
			// Pool exhausted; still create a temp instance to avoid failure
			var temp = CreateNew();
			temp.gameObject.SetActive(true);
			return temp;
		}

		public void Release(LineRenderer lr)
		{
			if (lr == null) return;
			lr.positionCount = 0;
			lr.gameObject.SetActive(false);
			lr.transform.SetParent(_poolRoot, false);
			_available.Push(lr);
		}
	}
}


