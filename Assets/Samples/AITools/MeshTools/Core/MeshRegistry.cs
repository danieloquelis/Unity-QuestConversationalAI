using System.Collections.Generic;
using UnityEngine;

namespace MeshTools
{
    /// <summary>
    /// Global registry for mesh objects created by AI agents.
    /// Tracks meshes by ID for easy reference and manipulation.
    /// </summary>
    public static class MeshRegistry
    {
        private static readonly Dictionary<string, GameObject> _idToMeshObject = new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, MeshFilter> _idToMeshFilter = new Dictionary<string, MeshFilter>();
        private static readonly Dictionary<string, MeshRenderer> _idToMeshRenderer = new Dictionary<string, MeshRenderer>();

        /// <summary>
        /// Register a mesh GameObject with an ID
        /// </summary>
        public static void RegisterMesh(string id, GameObject meshObject)
        {
            if (string.IsNullOrEmpty(id) || meshObject == null) return;
            
            _idToMeshObject[id] = meshObject;
            
            var meshFilter = meshObject.GetComponent<MeshFilter>();
            if (meshFilter != null)
                _idToMeshFilter[id] = meshFilter;
                
            var meshRenderer = meshObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
                _idToMeshRenderer[id] = meshRenderer;
        }

        /// <summary>
        /// Get mesh GameObject by ID
        /// </summary>
        public static GameObject GetMeshObject(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _idToMeshObject.TryGetValue(id, out var obj);
            return obj;
        }

        /// <summary>
        /// Get MeshFilter by ID
        /// </summary>
        public static MeshFilter GetMeshFilter(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _idToMeshFilter.TryGetValue(id, out var filter);
            return filter;
        }

        /// <summary>
        /// Get MeshRenderer by ID
        /// </summary>
        public static MeshRenderer GetMeshRenderer(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _idToMeshRenderer.TryGetValue(id, out var renderer);
            return renderer;
        }

        /// <summary>
        /// Unregister and optionally destroy a mesh
        /// </summary>
        public static void UnregisterMesh(string id, bool destroy = false)
        {
            if (string.IsNullOrEmpty(id)) return;

            if (destroy && _idToMeshObject.TryGetValue(id, out var obj) && obj != null)
            {
                Object.DestroyImmediate(obj);
            }

            _idToMeshObject.Remove(id);
            _idToMeshFilter.Remove(id);
            _idToMeshRenderer.Remove(id);
        }

        /// <summary>
        /// Get all registered mesh IDs
        /// </summary>
        public static string[] GetAllMeshIds()
        {
            var ids = new string[_idToMeshObject.Count];
            _idToMeshObject.Keys.CopyTo(ids, 0);
            return ids;
        }

        /// <summary>
        /// Clear all registered meshes
        /// </summary>
        public static void Clear(bool destroyObjects = false)
        {
            if (destroyObjects)
            {
                foreach (var obj in _idToMeshObject.Values)
                {
                    if (obj != null)
                        Object.DestroyImmediate(obj);
                }
            }

            _idToMeshObject.Clear();
            _idToMeshFilter.Clear();
            _idToMeshRenderer.Clear();
        }

        /// <summary>
        /// Check if a mesh ID is registered
        /// </summary>
        public static bool HasMesh(string id)
        {
            return !string.IsNullOrEmpty(id) && _idToMeshObject.ContainsKey(id);
        }
    }
}
