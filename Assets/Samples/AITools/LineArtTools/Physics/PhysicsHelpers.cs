using UnityEngine;

namespace LineArtTools
{
	/// <summary>
	/// Simple physics helpers for agent-accessible primitives.
	/// </summary>
	public static class PhysicsHelpers
	{
		public static Rigidbody AddBallBody(GameObject host, float radius, float mass, float bounciness)
		{
			radius = Validation.ClampSizeMeters(radius);
			mass = Mathf.Max(0.0001f, mass);
			bounciness = Mathf.Clamp01(bounciness);

			var rb = host.GetComponent<Rigidbody>();
			if (rb == null) rb = host.AddComponent<Rigidbody>();
			rb.mass = mass;
			rb.useGravity = true;
			rb.isKinematic = false;

			var col = host.GetComponent<SphereCollider>();
			if (col == null) col = host.AddComponent<SphereCollider>();
			col.isTrigger = false;
			col.radius = radius;

			var mat = new PhysicsMaterial("BallMat") { bounciness = bounciness, bounceCombine = PhysicsMaterialCombine.Maximum };
			col.material = mat;
			return rb;
		}

		public static void AttachFixed(Rigidbody a, Rigidbody b)
		{
			if (a == null || b == null) return;
			var j = a.gameObject.AddComponent<FixedJoint>();
			j.connectedBody = b;
			j.breakForce = Mathf.Infinity;
			j.breakTorque = Mathf.Infinity;
		}
	}
}


