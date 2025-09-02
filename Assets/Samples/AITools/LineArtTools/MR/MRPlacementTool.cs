using UnityEngine;
using Meta.XR;
using Meta.XR.MRUtilityKit;

namespace LineArtTools
{
	public enum MRSurfaceKind { Table, Floor, Any }

	public static class MRPlacementTool
	{
		public static Pose PlaceOnNearestSurface(Transform sceneRoot, MRSurfaceKind surface = MRSurfaceKind.Table, float maxDistanceMeters = 3f, Transform originOverride = null, Vector3? originWorldPos = null)
		{
			if (sceneRoot == null) return new Pose(Vector3.zero, Quaternion.identity);
			// Prefer MRUK room nearest table surface
			var mruk = MRUK.Instance;
			var room = mruk != null ? mruk.GetCurrentRoom() : null;
			if (room != null)
			{
				var origin = originWorldPos.HasValue ? originWorldPos.Value : (originOverride != null ? originOverride.position : sceneRoot.position);
				var label = MRUKAnchor.SceneLabels.TABLE;
				if (surface == MRSurfaceKind.Floor) label = MRUKAnchor.SceneLabels.FLOOR;
				if (surface == MRSurfaceKind.Any) label = 0; // empty filter includes all
				var filter = surface == MRSurfaceKind.Any ? new LabelFilter() : new LabelFilter(label);
				var dist = room.TryGetClosestSurfacePosition(origin, out var surfacePos, out var anchor, out var normal, filter);
				if (dist < Mathf.Infinity && dist <= maxDistanceMeters)
				{
					sceneRoot.position = surfacePos + normal * 0.01f;
					sceneRoot.rotation = Quaternion.FromToRotation(Vector3.up, normal);
					// Parent under anchor for stability if available
					if (anchor != null)
					{
						sceneRoot.SetParent(anchor.transform, true);
					}
					return new Pose(sceneRoot.position, sceneRoot.rotation);
				}
			}
			return new Pose(sceneRoot.position, sceneRoot.rotation);
		}

		public static Pose PlaceUsingEnvironmentRaycast(Transform sceneRoot, Transform rayOrigin, Vector3? rayDirection = null, float maxDistanceMeters = 3f)
		{
			if (sceneRoot == null || rayOrigin == null)
			{
				return new Pose(sceneRoot != null ? sceneRoot.position : Vector3.zero,
					sceneRoot != null ? sceneRoot.rotation : Quaternion.identity);
			}
			var mgr = Object.FindAnyObjectByType<EnvironmentRaycastManager>(FindObjectsInactive.Include);
			if (mgr == null)
			{
				// Create and enable one at runtime
				var go = new GameObject("EnvironmentRaycastManager(Auto)");
				mgr = go.AddComponent<EnvironmentRaycastManager>();
				go.SetActive(true);
			}
			Vector3 dir = rayDirection.HasValue ? rayDirection.Value.normalized : rayOrigin.forward;
			var ray = new Ray(rayOrigin.position, dir);
			if (mgr.Raycast(ray, out var hit, maxDistanceMeters) && hit.status == EnvironmentRaycastHitStatus.Hit)
			{
				sceneRoot.position = hit.point + hit.normal * 0.01f;
				sceneRoot.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
			}
			return new Pose(sceneRoot.position, sceneRoot.rotation);
		}
	}
}


