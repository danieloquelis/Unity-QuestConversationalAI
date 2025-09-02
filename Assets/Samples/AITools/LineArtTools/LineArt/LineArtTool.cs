using System.Collections.Generic;
using UnityEngine;

namespace LineArtTools
{
	/// <summary>
	/// Generic line-art drawing operations with runtime registry and transform bindings.

	/// </summary>
	public sealed class LineArtTool : MonoBehaviour
	{
		private readonly Dictionary<string, StrokeHandle> _strokes = new Dictionary<string, StrokeHandle>();
		private readonly Dictionary<string, (Transform a, Transform b)> _endpointBindings = new Dictionary<string, (Transform a, Transform b)>();
		private readonly Dictionary<string, (Transform t, Vector3 offset)> _followBindings = new Dictionary<string, (Transform t, Vector3 offset)>();

		private LineRendererPool _pool;
		private Transform _runtimeRoot;

		private void Awake()
		{
			EnsureInitialized();
		}

		private void EnsureInitialized()
		{
			if (_runtimeRoot == null)
			{
				_runtimeRoot = new GameObject("LineArt_Runtime").transform;
				_runtimeRoot.SetParent(transform, false);
			}
			if (_pool == null)
			{
				_pool = new LineRendererPool("LineRendererPool", _runtimeRoot);
			}
		}

		private void LateUpdate()
		{
			// Update bound endpoints
			foreach (var kv in _endpointBindings)
			{
				if (!_strokes.TryGetValue(kv.Key, out var handle)) continue;
				var (a, b) = kv.Value;
				if (a == null || b == null || handle.Renderer == null) continue;
				handle.Renderer.positionCount = 2;
				handle.Renderer.SetPosition(0, a.position);
				handle.Renderer.SetPosition(1, b.position);
			}

			// Update follow bindings
			foreach (var kv in _followBindings)
			{
				if (!_strokes.TryGetValue(kv.Key, out var handle)) continue;
				var (t, offset) = kv.Value;
				if (t == null || handle.Renderer == null) continue;
				var p = t.TransformPoint(offset);
				if (handle.Renderer.positionCount == 1)
				{
					handle.Renderer.SetPosition(0, p);
				}
				else
				{
					// For shapes with multiple vertices, translate by delta from current centroid to target point
					var count = handle.Renderer.positionCount;
					if (count == 0) continue;
					Vector3 centroid = Vector3.zero;
					for (int i = 0; i < count; i++) centroid += handle.Renderer.GetPosition(i);
					centroid /= count;
					var delta = p - centroid;
					for (int i = 0; i < count; i++) handle.Renderer.SetPosition(i, handle.Renderer.GetPosition(i) + delta);
				}
			}
		}

		private static void ApplyStyle(LineRenderer lr, float width, Color color)
		{
			if (lr == null) return;
			var s = LineArtToolsSettings.Instance;
			var w = Validation.ClampLineWidth(width);
			lr.startWidth = w;
			lr.endWidth = w;
			lr.startColor = color;
			lr.endColor = color;
			if (lr.material == null)
			{
				if (s.defaultLineMaterial != null)
				{
					lr.material = s.defaultLineMaterial;
				}
				else
				{
					lr.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
				}
			}
			if (lr.material != null)
			{
				lr.material.color = color;
			}
		}

		// PRIMITIVES
		public StrokeHandle LineSegment(string id, Vector3 a, Vector3 b, float width, Color color)
		{
			EnsureInitialized();
			width = Validation.ClampLineWidth(width);
			var lr = AcquireRenderer(id);
			lr.positionCount = 2;
			lr.SetPosition(0, a);
			lr.SetPosition(1, b);
			ApplyStyle(lr, width, color);
			return _strokes[id];
		}

		private sealed class PolylineState
		{
			public readonly List<Vector3> Vertices = new List<Vector3>();
			public float Width;
			public Color Color;
		}

		private readonly Dictionary<string, PolylineState> _polyInProgress = new Dictionary<string, PolylineState>();

		public StrokeHandle PolylineBegin(string id, float width, Color color)
		{
			EnsureInitialized();
			width = Validation.ClampLineWidth(width);
			_polyInProgress[id] = new PolylineState { Width = width, Color = color };
			var lr = AcquireRenderer(id);
			lr.positionCount = 0;
			ApplyStyle(lr, width, color);
			return _strokes[id];
		}

		public void PolylineAddVertex(string id, Vector3 p)
		{
			if (!_polyInProgress.TryGetValue(id, out var st)) return;
			st.Vertices.Add(p);
			if (_strokes.TryGetValue(id, out var handle))
			{
				var lr = handle.Renderer;
				lr.positionCount = st.Vertices.Count;
				lr.SetPosition(st.Vertices.Count - 1, p);
			}
		}

		public StrokeHandle PolylineEnd(string id, bool closed = false)
		{
			if (!_polyInProgress.TryGetValue(id, out var st)) return _strokes.ContainsKey(id) ? _strokes[id] : null;
			_polyInProgress.Remove(id);
			if (!_strokes.TryGetValue(id, out var handle)) return null;
			var lr = handle.Renderer;
			if (st.Vertices.Count == 0) return handle;
			if (closed && st.Vertices.Count >= 2)
			{
				st.Vertices.Add(st.Vertices[0]);
			}
			lr.positionCount = st.Vertices.Count;
			for (int i = 0; i < st.Vertices.Count; i++) lr.SetPosition(i, st.Vertices[i]);
			return handle;
		}

		public StrokeHandle Circle(string id, Vector3 center, float radius, float width, Color color, int segments = 32)
		{
			EnsureInitialized();
			width = Validation.ClampLineWidth(width);
			radius = Validation.ClampSizeMeters(radius);
			segments = Validation.ClampCircleSegments(segments);

			var lr = AcquireRenderer(id);
			ApplyStyle(lr, width, color);

			if (segments < 3) segments = 3;
			lr.positionCount = segments + 1; // closed loop
			float step = Mathf.PI * 2f / segments;
			for (int i = 0; i <= segments; i++)
			{
				float a = i * step;
				var p = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius + center;
				lr.SetPosition(i, p);
			}
			return _strokes[id];
		}

		public StrokeHandle BezierQuadratic(string id, Vector3 p0, Vector3 p1, Vector3 p2, float width, Color color, int segments = 32)
		{
			EnsureInitialized();
			width = Validation.ClampLineWidth(width);
			segments = Validation.ClampCircleSegments(segments);
			if (segments < 1) segments = 1;
			var lr = AcquireRenderer(id);
			ApplyStyle(lr, width, color);
			lr.positionCount = segments + 1;
			for (int i = 0; i <= segments; i++)
			{
				float t = (float)i / segments;
				float u = 1f - t;
				var p = u * u * p0 + 2f * u * t * p1 + t * t * p2;
				lr.SetPosition(i, p);
			}
			return _strokes[id];
		}

		public StrokeHandle BezierCubic(string id, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float width, Color color, int segments = 32)
		{
			EnsureInitialized();
			width = Validation.ClampLineWidth(width);
			segments = Validation.ClampCircleSegments(segments);
			if (segments < 1) segments = 1;
			var lr = AcquireRenderer(id);
			ApplyStyle(lr, width, color);
			lr.positionCount = segments + 1;
			for (int i = 0; i <= segments; i++)
			{
				float t = (float)i / segments;
				float u = 1f - t;
				var p = u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
				lr.SetPosition(i, p);
			}
			return _strokes[id];
		}

		// BINDINGS
		public void BindEndpoints(string id, Transform aTarget, Transform bTarget)
		{
			_endpointBindings[id] = (aTarget, bTarget);
		}

		public void FollowPoint(string id, Transform target, Vector3 offset)
		{
			_followBindings[id] = (target, offset);
		}

		// STYLE
		public void SetStyle(string id, float? width = null, Color? color = null, bool? depthTest = null, bool dashed = false, float dashSize = 0.05f, float gapSize = 0.05f)
		{
			if (!_strokes.TryGetValue(id, out var handle) || handle.Renderer == null) return;
			var lr = handle.Renderer;
			if (width.HasValue)
			{
				var w = Validation.ClampLineWidth(width.Value);
				lr.startWidth = w; lr.endWidth = w;
			}
			if (color.HasValue)
			{
				lr.startColor = color.Value; lr.endColor = color.Value;
				if (lr.material != null) lr.material.color = color.Value;
			}
			// depthTest and dashed could be implemented with material switches/shader keywords
		}

		// GROUP / XFORM (minimal placeholders)
		public void Group(string id, params string[] childIds) { /* grouping can be metadata-only initially */ }
		public void Xform(string id, Vector3? translate = null, Vector3? rotateEuler = null, Vector3? scale = null)
		{
			if (!_strokes.TryGetValue(id, out var handle) || handle.Renderer == null) return;
			var lr = handle.Renderer;
			if (translate.HasValue)
			{
				var count = lr.positionCount;
				for (int i = 0; i < count; i++) lr.SetPosition(i, lr.GetPosition(i) + translate.Value);
			}
			if (rotateEuler.HasValue)
			{
				var rot = Quaternion.Euler(rotateEuler.Value);
				var count = lr.positionCount;
				for (int i = 0; i < count; i++) lr.SetPosition(i, rot * lr.GetPosition(i));
			}
			if (scale.HasValue)
			{
				var scl = scale.Value;
				var count = lr.positionCount;
				for (int i = 0; i < count; i++)
				{
					var p = lr.GetPosition(i);
					lr.SetPosition(i, new Vector3(p.x * scl.x, p.y * scl.y, p.z * scl.z));
				}
			}
		}

		// INTERNALS
		private LineRenderer AcquireRenderer(string id)
		{
			EnsureInitialized();
			if (!_strokes.TryGetValue(id, out var handle) || handle.Renderer == null)
			{
				var lr = _pool.Get();
				handle = new StrokeHandle(id, lr);
				_strokes[id] = handle;
			}
			return handle.Renderer;
		}
	}
}


