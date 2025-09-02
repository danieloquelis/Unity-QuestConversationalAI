using System;
using System.Collections.Generic;
using UnityEngine;

namespace LineArtTools
{
	/// <summary>
	/// Generic runtime animation primitives for any registered id (Transform).
	/// Designed to be driven by an agent via JSON through AgentDispatcher.
	/// </summary>
	public sealed class AnimationTool : MonoBehaviour
	{
		private sealed class MoveTrack
		{
			public Transform Target;
			public Vector3 Start;
			public Vector3 End;
			public float T0;
			public float Duration;
			public Func<float, float> Ease;
			public string Channel;
		}

		private sealed class TranslateTrack
		{
			public Transform Target;
			public Vector3 Start;
			public Vector3 End;
			public float T0;
			public float Duration;
			public Func<float, float> Ease;
			public string Channel;
		}

		private sealed class OscPosTrack
		{
			public Transform Target;
			public Vector3 BaseLocal;
			public Vector3 Axis;
			public float Amplitude;
			public float Frequency;
			public float Phase;
			public string Channel;
		}

		private sealed class OscRotTrack
		{
			public Transform Target;
			public Quaternion BaseLocal;
			public Vector3 Axis;
			public float AmplitudeDeg;
			public float Frequency;
			public float Phase;
			public string Channel;
		}

		private sealed class SpinTrack
		{
			public Transform Target;
			public Vector3 Axis;
			public float DegPerSec;
			public string Channel;
		}

		private readonly List<MoveTrack> _move = new List<MoveTrack>();
		private readonly List<TranslateTrack> _translate = new List<TranslateTrack>();
		private readonly List<OscPosTrack> _oscPos = new List<OscPosTrack>();
		private readonly List<OscRotTrack> _oscRot = new List<OscRotTrack>();
		private readonly List<SpinTrack> _spin = new List<SpinTrack>();

		private static Func<float, float> EaseFrom(string ease)
		{
			switch ((ease ?? "linear").ToLowerInvariant())
			{
				case "easein": return t => t * t;
				case "easeout": return t => 1f - (1f - t) * (1f - t);
				case "easeinout": return t => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
				default: return t => t;
			}
		}

		private static Transform GetTarget(string id)
		{
			return GlobalRegistry.GetTransform(id);
		}

		public void MoveTo(string id, Vector3 targetPos, float duration, string channel = "default", string ease = "linear")
		{
			var t = GetTarget(id); if (t == null) return;
			_move.Add(new MoveTrack { Target = t, Start = t.position, End = targetPos, T0 = Time.time, Duration = Mathf.Max(0.0001f, duration), Ease = EaseFrom(ease), Channel = channel });
		}

		public void TranslateBy(string id, Vector3 delta, float duration, string channel = "default", string ease = "linear")
		{
			var t = GetTarget(id); if (t == null) return;
			_translate.Add(new TranslateTrack { Target = t, Start = t.position, End = t.position + delta, T0 = Time.time, Duration = Mathf.Max(0.0001f, duration), Ease = EaseFrom(ease), Channel = channel });
		}

		public void OscillatePosition(string id, Vector3 axis, float amplitude, float frequency, float phase = 0f, string channel = "oscPos")
		{
			var t = GetTarget(id); if (t == null) return;
			_oscPos.Add(new OscPosTrack { Target = t, BaseLocal = t.localPosition, Axis = axis.normalized, Amplitude = amplitude, Frequency = frequency, Phase = phase, Channel = channel });
		}

		public void OscillateRotation(string id, Vector3 axis, float amplitudeDeg, float frequency, float phase = 0f, string channel = "oscRot")
		{
			var t = GetTarget(id); if (t == null) return;
			_oscRot.Add(new OscRotTrack { Target = t, BaseLocal = t.localRotation, Axis = axis.normalized, AmplitudeDeg = amplitudeDeg, Frequency = frequency, Phase = phase, Channel = channel });
		}

		public void Spin(string id, Vector3 axis, float degPerSec, string channel = "spin")
		{
			var t = GetTarget(id); if (t == null) return;
			_spin.Add(new SpinTrack { Target = t, Axis = axis.normalized, DegPerSec = degPerSec, Channel = channel });
		}

		public void Stop(string id, string channel = null)
		{
			Predicate<string> match = channel == null ? _ => true : c => c == channel;
			_move.RemoveAll(m => m.Target == GetTarget(id) && match(m.Channel));
			_translate.RemoveAll(m => m.Target == GetTarget(id) && match(m.Channel));
			_oscPos.RemoveAll(m => m.Target == GetTarget(id) && match(m.Channel));
			_oscRot.RemoveAll(m => m.Target == GetTarget(id) && match(m.Channel));
			_spin.RemoveAll(m => m.Target == GetTarget(id) && match(m.Channel));
		}

		private void Update()
		{
			float now = Time.time;
			for (int i = _move.Count - 1; i >= 0; i--)
			{
				var tr = _move[i]; if (tr.Target == null) { _move.RemoveAt(i); continue; }
				float u = Mathf.Clamp01((now - tr.T0) / tr.Duration);
				tr.Target.position = Vector3.LerpUnclamped(tr.Start, tr.End, tr.Ease(u));
				if (u >= 1f) _move.RemoveAt(i);
			}
			for (int i = _translate.Count - 1; i >= 0; i--)
			{
				var tr = _translate[i]; if (tr.Target == null) { _translate.RemoveAt(i); continue; }
				float u = Mathf.Clamp01((now - tr.T0) / tr.Duration);
				tr.Target.position = Vector3.LerpUnclamped(tr.Start, tr.End, tr.Ease(u));
				if (u >= 1f) _translate.RemoveAt(i);
			}

			// Accumulate oscillations per target
			var posOffsets = new Dictionary<Transform, Vector3>();
			foreach (var tr in _oscPos)
			{
				if (tr.Target == null) continue;
				float s = Mathf.Sin(2f * Mathf.PI * tr.Frequency * now + tr.Phase);
				var off = tr.Axis * (tr.Amplitude * s);
				posOffsets.TryGetValue(tr.Target, out var cur);
				posOffsets[tr.Target] = cur + off;
			}
			foreach (var kv in posOffsets)
			{
				var targetTracks = _oscPos.Find(t => t.Target == kv.Key);
				if (kv.Key != null && targetTracks != null)
				{
					// Use the base of the first track for this target as reference
					var basePos = targetTracks.BaseLocal;
					kv.Key.localPosition = basePos + kv.Value;
				}
			}

			var rotOffsets = new Dictionary<Transform, Quaternion>();
			foreach (var tr in _oscRot)
			{
				if (tr.Target == null) continue;
				float s = Mathf.Sin(2f * Mathf.PI * tr.Frequency * now + tr.Phase);
				var rot = Quaternion.AngleAxis(tr.AmplitudeDeg * s, tr.Axis);
				rotOffsets.TryGetValue(tr.Target, out var cur);
				rotOffsets[tr.Target] = cur == default ? rot : cur * rot;
			}
			foreach (var tr in _oscRot)
			{
				if (tr.Target == null) continue;
				if (rotOffsets.TryGetValue(tr.Target, out var q))
				{
					tr.Target.localRotation = tr.BaseLocal * q;
				}
			}

			foreach (var sp in _spin)
			{
				if (sp.Target == null) continue;
				sp.Target.localRotation = Quaternion.AngleAxis(sp.DegPerSec * Time.deltaTime, sp.Axis) * sp.Target.localRotation;
			}
		}
	}
}


