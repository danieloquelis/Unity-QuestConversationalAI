using UnityEngine;

namespace LineArtTools
{
	public struct BoneHandles
	{
		public Transform Hip, Chest, Head, HandL, HandR, FootL, FootR;
	}

	public sealed class CharacterHandle
	{
		public string Id { get; }
		public Transform Root { get; }
		public BoneHandles Bones { get; }

		public CharacterHandle(string id, Transform root, BoneHandles bones)
		{
			Id = id; Root = root; Bones = bones;
		}

		public void SetRagdoll(bool on)
		{
			foreach (var rb in Root.GetComponentsInChildren<Rigidbody>())
			{
				rb.isKinematic = !on;
			}
		}

		public void SetStyle(Color color, float width)
		{
			foreach (var lr in Root.GetComponentsInChildren<LineRenderer>())
			{
				lr.startColor = color; lr.endColor = color;
				lr.startWidth = width; lr.endWidth = width;
				if (lr.material != null) lr.material.color = color;
			}
		}
	}

	public sealed class CharacterFactory : MonoBehaviour
	{
		[SerializeField] private LineArtTool lineArtTool;

		private void EnsureLineArt()
		{
			if (lineArtTool != null) return;
			lineArtTool = GetComponent<LineArtTool>();
			if (lineArtTool == null) lineArtTool = gameObject.AddComponent<LineArtTool>();
		}

		public CharacterHandle CreateStickFigure(string id, Color color, float scale = 1f)
		{
			EnsureLineArt();
			scale = Mathf.Max(0.1f, scale);

			var root = new GameObject($"Character_{id}").transform;
			root.SetParent(transform, false);

			// Build simple kinematic bone chain with joints
			BoneHandles bones = default;
			bones.Hip = CreateBone(root, "Hip", Vector3.zero, 0.05f * scale);
			bones.Chest = CreateBone(root, "Chest", new Vector3(0, 0.25f * scale, 0), 0.05f * scale);
			bones.Head = CreateBone(root, "Head", new Vector3(0, 0.45f * scale, 0), 0.05f * scale);
			bones.HandL = CreateBone(root, "HandL", new Vector3(-0.2f * scale, 0.25f * scale, 0), 0.04f * scale);
			bones.HandR = CreateBone(root, "HandR", new Vector3(0.2f * scale, 0.25f * scale, 0), 0.04f * scale);
			bones.FootL = CreateBone(root, "FootL", new Vector3(-0.1f * scale, -0.25f * scale, 0), 0.04f * scale);
			bones.FootR = CreateBone(root, "FootR", new Vector3(0.1f * scale, -0.25f * scale, 0), 0.04f * scale);

			// Joints (simple hierarchy)
			ConnectFixed(bones.Hip, bones.Chest);
			ConnectFixed(bones.Chest, bones.Head);
			ConnectFixed(bones.Chest, bones.HandL);
			ConnectFixed(bones.Chest, bones.HandR);
			ConnectFixed(bones.Hip, bones.FootL);
			ConnectFixed(bones.Hip, bones.FootR);

			// Lines
			var w = LineArtToolsSettings.Instance.defaultLineWidth;
			lineArtTool.LineSegment(id + ":torso", bones.Hip.position, bones.Chest.position, w, color);
			lineArtTool.BindEndpoints(id + ":torso", bones.Hip, bones.Chest);

			// Neck line (chest -> head) to visually connect head
			lineArtTool.LineSegment(id + ":neck", bones.Chest.position, bones.Head.position, w, color);
			lineArtTool.BindEndpoints(id + ":neck", bones.Chest, bones.Head);

			lineArtTool.LineSegment(id + ":armL", bones.Chest.position, bones.HandL.position, w, color);
			lineArtTool.BindEndpoints(id + ":armL", bones.Chest, bones.HandL);

			lineArtTool.LineSegment(id + ":armR", bones.Chest.position, bones.HandR.position, w, color);
			lineArtTool.BindEndpoints(id + ":armR", bones.Chest, bones.HandR);

			lineArtTool.LineSegment(id + ":legL", bones.Hip.position, bones.FootL.position, w, color);
			lineArtTool.BindEndpoints(id + ":legL", bones.Hip, bones.FootL);

			lineArtTool.LineSegment(id + ":legR", bones.Hip.position, bones.FootR.position, w, color);
			lineArtTool.BindEndpoints(id + ":legR", bones.Hip, bones.FootR);

			// Head circle: bind around head position using centroid-follow so it stays centered
			lineArtTool.Circle(id + ":head", bones.Head.position, 0.09f * scale, w, color, 24);
			lineArtTool.FollowPoint(id + ":head", bones.Head, Vector3.zero);

			var handle = new CharacterHandle(id, root, bones);
			// Default to kinematic (ragdoll off)
			handle.SetRagdoll(false);

			GlobalRegistry.RegisterCharacter(id, handle);
			GlobalRegistry.RegisterTransform(id, root);
			// Register bone ids for agent addressing
			GlobalRegistry.RegisterTransform(id + ":Hip", bones.Hip);
			GlobalRegistry.RegisterTransform(id + ":Chest", bones.Chest);
			GlobalRegistry.RegisterTransform(id + ":Head", bones.Head);
			GlobalRegistry.RegisterTransform(id + ":HandL", bones.HandL);
			GlobalRegistry.RegisterTransform(id + ":HandR", bones.HandR);
			GlobalRegistry.RegisterTransform(id + ":FootL", bones.FootL);
			GlobalRegistry.RegisterTransform(id + ":FootR", bones.FootR);
			return handle;
		}

		private static Transform CreateBone(Transform parent, string name, Vector3 localPos, float radius)
		{
			var go = new GameObject(name);
			go.transform.SetParent(parent, false);
			go.transform.localPosition = localPos;
			var rb = go.AddComponent<Rigidbody>();
			rb.isKinematic = true;
			var col = go.AddComponent<SphereCollider>();
			col.radius = Mathf.Max(Validation.ClampSizeMeters(radius), 0.01f);
			col.isTrigger = true; // invisible hit area
			return go.transform;
		}

		private static void ConnectFixed(Transform a, Transform b)
		{
			var rbA = a.GetComponent<Rigidbody>();
			var rbB = b.GetComponent<Rigidbody>();
			if (rbA == null || rbB == null) return;
			var j = a.gameObject.AddComponent<FixedJoint>();
			j.connectedBody = rbB;
			j.enableCollision = false;
		}
	}
}


