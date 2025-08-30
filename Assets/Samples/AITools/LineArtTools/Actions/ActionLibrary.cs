using System.Collections.Generic;
using UnityEngine;

namespace LineArtTools
{
	public enum AgentAction { Idle, WalkTo, Pickup, DribbleOnce, Shoot, Jump, Punch, Kick, Die, Celebrate }

	public struct ActionParams
	{
		public Vector3? TargetPos;
		public string TargetId;
		public float Speed;
		public float Power;
		public float Arc; // 0..1 upward bias
		public string Hand; // "left" or "right"
	}

	public sealed class ActionLibrary : MonoBehaviour
	{
		private sealed class WalkTask
		{
			public Transform Root;
			public Vector3 Target;
			public float Speed;
		}

		private readonly List<WalkTask> _walkTasks = new List<WalkTask>();
		private readonly Dictionary<string, (Rigidbody body, string hand)> _heldByCharacter = new Dictionary<string, (Rigidbody, string)>();
		private readonly Dictionary<Transform, Vector3> _lastPositions = new Dictionary<Transform, Vector3>();
		private readonly Dictionary<Transform, Vector3> _velocities = new Dictionary<Transform, Vector3>();

		private void LateUpdate()
		{
			// simple hand velocity estimation
			foreach (var ch in _heldByCharacter)
			{
				var handle = GlobalRegistry.GetCharacter(ch.Key);
				if (handle == null) continue;
				var handT = (ch.Value.hand == "left") ? handle.Bones.HandL : handle.Bones.HandR;
				if (handT == null) continue;
				var pos = handT.position;
				if (_lastPositions.TryGetValue(handT, out var last))
				{
					_velocities[handT] = (pos - last) / Mathf.Max(Time.deltaTime, 1e-4f);
				}
				_lastPositions[handT] = pos;
			}

			// walk tasks
			for (int i = _walkTasks.Count - 1; i >= 0; i--)
			{
				var t = _walkTasks[i];
				if (t.Root == null) { _walkTasks.RemoveAt(i); continue; }
				var current = t.Root.position;
				var next = Vector3.MoveTowards(current, t.Target, t.Speed * Mathf.Max(Time.deltaTime, 1e-4f));
				t.Root.position = next;
				if ((next - t.Target).sqrMagnitude < 0.0004f) _walkTasks.RemoveAt(i);
			}
		}

		public void PlayAction(string id, AgentAction action, ActionParams p = default)
		{
			var handle = GlobalRegistry.GetCharacter(id);
			if (handle == null) return;
			if (string.IsNullOrEmpty(p.Hand)) p.Hand = "right";
			if (p.Speed <= 0f) p.Speed = 1f;
			if (p.Power <= 0f) p.Power = 1f;

			switch (action)
			{
				case AgentAction.Idle:
					break;
				case AgentAction.WalkTo:
					if (p.TargetPos.HasValue)
					{
						_walkTasks.Add(new WalkTask { Root = handle.Root, Target = p.TargetPos.Value, Speed = p.Speed });
					}
					break;
				case AgentAction.Pickup:
					if (!string.IsNullOrEmpty(p.TargetId))
					{
						var targetT = GlobalRegistry.GetTransform(p.TargetId);
						var handT = (p.Hand == "left") ? handle.Bones.HandL : handle.Bones.HandR;
						if (targetT != null && handT != null)
						{
							var rb = targetT.GetComponent<Rigidbody>();
							if (rb == null) rb = targetT.gameObject.AddComponent<Rigidbody>();
							rb.isKinematic = false;
							PhysicsHelpers.AttachFixed(handT.GetComponent<Rigidbody>(), rb);
							_heldByCharacter[id] = (rb, p.Hand);
						}
					}
					break;
				case AgentAction.Shoot:
					if (_heldByCharacter.TryGetValue(id, out var held))
					{
						var handT = (held.hand == "left") ? handle.Bones.HandL : handle.Bones.HandR;
						if (handT != null && held.body != null)
						{
							var dir = p.TargetPos.HasValue ? (p.TargetPos.Value - handT.position).normalized : handT.forward;
							var upBias = Vector3.up * Mathf.Clamp01(p.Arc) * 0.5f;
							var v = (dir + upBias).normalized * (2.5f * p.Power);
							Release(id, held.hand);
							held.body.linearVelocity = v;
						}
					}
					break;
				case AgentAction.Die:
					handle.SetRagdoll(true);
					var rbHip = handle.Bones.Hip.GetComponent<Rigidbody>();
					if (rbHip != null)
					{
						rbHip.AddForce(Vector3.back * 2f, ForceMode.VelocityChange);
					}
					break;
			}
		}

		public void Release(string id, string hand = "right")
		{
			if (!_heldByCharacter.TryGetValue(id, out var held)) return;
			var handle = GlobalRegistry.GetCharacter(id);
			if (handle == null) return;
			var handT = (hand == "left") ? handle.Bones.HandL : handle.Bones.HandR;
			if (handT == null) return;
			var joint = handT.GetComponent<FixedJoint>();
			if (joint != null)
			{
				Destroy(joint);
			}
			// apply throw if we have velocity estimate
			if (_velocities.TryGetValue(handT, out var v) && held.body != null)
			{
				held.body.linearVelocity = v;
			}
			_heldByCharacter.Remove(id);
		}
	}
}


