using UnityEngine;

namespace LineArtTools
{
	public sealed class ScoreTrigger : MonoBehaviour
	{
		private void OnTriggerEnter(Collider other)
		{
			var rb = other.attachedRigidbody;
			if (rb != null && Vector3.Dot(rb.linearVelocity, Vector3.down) > 0.5f)
			{
				Debug.Log("Score!");
			}
		}
	}
}


