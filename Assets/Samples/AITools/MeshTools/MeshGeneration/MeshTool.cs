using System;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json.Linq;
using GLTFast;

namespace MeshTools
{
    /// <summary>
    /// Main mesh tool component that handles mesh creation, manipulation, and communication
    /// with the Python mesh tool server. This is the primary interface for AI agents.
    /// </summary>
    public class MeshTool : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private WebSocketMeshClient webSocketClient;
        
        [Header("Mesh Generation")]
        [SerializeField] private Transform meshParent;
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private bool addColliders = true;
        
        [Header("Import Settings")]
        [Tooltip("Use GLTFast for better quality meshes (default), or raw JSON for simpler meshes")]
        [SerializeField] private bool useGLTFast = true;

        public WebSocketMeshClient Client => webSocketClient;
        public bool IsConnected => webSocketClient != null && webSocketClient.IsConnected;
        public bool UseGLTFast => useGLTFast;

        private void Awake()
        {
            // Auto-setup components if not assigned
            if (webSocketClient == null)
                webSocketClient = GetComponent<WebSocketMeshClient>() ?? gameObject.AddComponent<WebSocketMeshClient>();

            // Create mesh parent if not assigned
            if (meshParent == null)
            {
                var parentGO = new GameObject("Generated Meshes");
                parentGO.transform.SetParent(transform);
                meshParent = parentGO.transform;
            }
        }

        #region Primitive Creation

        /// <summary>
        /// Create a primitive mesh (box, sphere, cylinder, capsule)
        /// </summary>
        public async Task<GameObject> CreatePrimitiveAsync(string meshId, string primitiveType, JObject parameters = null)
        {
            try
            {
                Debug.Log($"[MeshTool] CreatePrimitiveAsync called - MeshID: '{meshId}', Type: '{primitiveType}', Parameters: {parameters}");
                
                var requestParams = new JObject
                {
                    ["mesh_id"] = meshId,
                    ["type"] = primitiveType
                };

                // Add specific parameters based on primitive type
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        requestParams[param.Key] = param.Value;
                    }
                }

                Debug.Log($"[MeshTool] Sending to Python server: {requestParams}");

                var response = await webSocketClient.SendCommandAsync("create_primitive", requestParams);
                
                if (response.Value<bool>("success"))
                {
                    Debug.Log($"MeshTool: Created {primitiveType} '{meshId}' with {response.Value<int>("vertices_count")} vertices");
                    
                    // Create a placeholder Unity primitive first
                    var meshObject = CreateUnityPrimitive(primitiveType, meshId, parameters);
                    
                    // Then update it with actual mesh data from server
                    _ = UpdateMeshFromServer(meshId);
                    
                    return meshObject;
                }
                else
                {
                    var error = response.Value<string>("error");
                    Debug.LogError($"MeshTool: Failed to create {primitiveType}: {error}");
                    return null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"MeshTool: Exception creating {primitiveType}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create a complex mesh (torus, icosphere, etc.)
        /// </summary>
        public async Task<GameObject> CreateComplexMeshAsync(string meshId, string meshType, JObject parameters = null)
        {
            try
            {
                var requestParams = new JObject
                {
                    ["mesh_id"] = meshId,
                    ["type"] = meshType
                };

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        requestParams[param.Key] = param.Value;
                    }
                }

                var response = await webSocketClient.SendCommandAsync("create_complex_mesh", requestParams);
                
                if (response.Value<bool>("success"))
                {
                    Debug.Log($"MeshTool: Created complex mesh '{meshId}' with {response.Value<int>("vertices_count")} vertices");
                    
                    // Create a placeholder first
                    var meshObject = CreatePlaceholderMesh(meshId, meshType);
                    
                    // Then update with actual mesh data from server
                    _ = UpdateMeshFromServer(meshId);
                    
                    return meshObject;
                }
                else
                {
                    var error = response.Value<string>("error");
                    Debug.LogError($"MeshTool: Failed to create complex mesh: {error}");
                    return null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"MeshTool: Exception creating complex mesh: {e.Message}");
                return null;
            }
        }

        #endregion

        #region Boolean Operations

        /// <summary>
        /// Perform boolean operation between two meshes
        /// </summary>
        public async Task<bool> BooleanOperationAsync(string meshA, string meshB, string operation, string resultId)
        {
            try
            {
                Debug.Log($"[MeshTool] BooleanOperationAsync called - MeshA: '{meshA}', MeshB: '{meshB}', Operation: '{operation}', ResultID: '{resultId}'");
                
                var requestParams = new JObject
                {
                    ["mesh_a"] = meshA,
                    ["mesh_b"] = meshB,
                    ["operation"] = operation,
                    ["result_id"] = resultId
                };

                Debug.Log($"[MeshTool] Sending to Python server: {requestParams}");
                var response = await webSocketClient.SendCommandAsync("boolean_operation", requestParams);
                Debug.Log($"[MeshTool] Python server response: {response}");
                
                if (response.Value<bool>("success"))
                {
                    Debug.Log($"MeshTool: Boolean {operation} successful. Result: {response.Value<int>("vertices_count")} vertices");
                    
                    // Hide the input meshes to avoid visual overlap
                    var meshAObject = MeshRegistry.GetMeshObject(meshA);
                    var meshBObject = MeshRegistry.GetMeshObject(meshB);
                    
                    if (meshAObject != null)
                    {
                        meshAObject.SetActive(false);
                        Debug.Log($"MeshTool: Hidden input mesh '{meshA}' after boolean operation");
                    }
                    
                    if (meshBObject != null)
                    {
                        meshBObject.SetActive(false);
                        Debug.Log($"MeshTool: Hidden input mesh '{meshB}' after boolean operation");
                    }
                    
                    // Update Unity representation
                    await UpdateMeshFromServer(resultId);
                    return true;
                }
                else
                {
                    var error = response.Value<string>("error");
                    Debug.LogError($"MeshTool: Boolean operation failed: {error}");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"MeshTool: Exception in boolean operation: {e.Message}");
                return false;
            }
        }

        #endregion

        #region Mesh Transformation

        /// <summary>
        /// Transform a mesh (translate, rotate, scale)
        /// </summary>
        public async Task<bool> TransformMeshAsync(string meshId, Vector3? translate = null, 
            Vector3? rotateAxis = null, float? rotateAngle = null, Vector3? scale = null, string resultId = null)
        {
            try
            {
                var requestParams = new JObject
                {
                    ["mesh_id"] = meshId
                };

                if (translate.HasValue)
                    requestParams["translate"] = MeshDataConverter.Vector3ToJson(translate.Value);

                if (rotateAxis.HasValue && rotateAngle.HasValue)
                {
                    requestParams["rotate"] = new JObject
                    {
                        ["axis"] = MeshDataConverter.Vector3ToJson(rotateAxis.Value),
                        ["angle"] = rotateAngle.Value
                    };
                }

                if (scale.HasValue)
                    requestParams["scale"] = MeshDataConverter.Vector3ToJson(scale.Value);

                if (!string.IsNullOrEmpty(resultId))
                    requestParams["result_id"] = resultId;

                var response = await webSocketClient.SendCommandAsync("transform_mesh", requestParams);
                
                if (response.Value<bool>("success"))
                {
                    var targetId = resultId ?? meshId;
                    Debug.Log($"MeshTool: Transform successful for '{targetId}'");
                    
                    await UpdateMeshFromServer(targetId);
                    return true;
                }
                else
                {
                    var error = response.Value<string>("error");
                    Debug.LogError($"MeshTool: Transform failed: {error}");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"MeshTool: Exception in transform: {e.Message}");
                return false;
            }
        }

        #endregion

        #region Mesh Export/Import

        /// <summary>
        /// Export mesh to file
        /// </summary>
        public async Task<string> ExportMeshAsync(string meshId, string filename = null, string format = "glb")
        {
            try
            {
                var requestParams = new JObject
                {
                    ["mesh_id"] = meshId,
                    ["format"] = format
                };

                if (!string.IsNullOrEmpty(filename))
                    requestParams["filename"] = filename;

                var response = await webSocketClient.SendCommandAsync("save_mesh_file", requestParams);
                
                if (response.Value<bool>("success"))
                {
                    var filepath = response.Value<string>("filepath");
                    Debug.Log($"MeshTool: Exported '{meshId}' to {filepath}");
                    return filepath;
                }
                else
                {
                    var error = response.Value<string>("error");
                    Debug.LogError($"MeshTool: Export failed: {error}");
                    return null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"MeshTool: Exception in export: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get mesh information from server
        /// </summary>
        public async Task<JObject> GetMeshInfoAsync(string meshId)
        {
            try
            {
                var requestParams = new JObject { ["mesh_id"] = meshId };
                var response = await webSocketClient.SendCommandAsync("get_mesh_info", requestParams);
                
                if (response.Value<bool>("success"))
                {
                    return response;
                }
                else
                {
                    Debug.LogError($"MeshTool: Failed to get mesh info: {response.Value<string>("error")}");
                    return null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"MeshTool: Exception getting mesh info: {e.Message}");
                return null;
            }
        }

        #endregion

        #region Mesh Management

        /// <summary>
        /// Delete a mesh from the server and Unity
        /// </summary>
        public async Task<bool> DeleteMeshAsync(string meshId)
        {
            try
            {
                var requestParams = new JObject { ["mesh_id"] = meshId };
                var response = await webSocketClient.SendCommandAsync("delete_mesh", requestParams);
                
                if (response.Value<bool>("success"))
                {
                    // Remove from Unity registry
                    MeshRegistry.UnregisterMesh(meshId, destroy: true);
                    Debug.Log($"MeshTool: Deleted mesh '{meshId}'");
                    return true;
                }
                else
                {
                    Debug.LogError($"MeshTool: Failed to delete mesh: {response.Value<string>("error")}");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"MeshTool: Exception deleting mesh: {e.Message}");
                return false;
            }
        }

        #endregion

        #region Helper Methods

        private GameObject CreateUnityPrimitive(string primitiveType, string meshId, JObject parameters)
        {
            PrimitiveType unityPrimitive;
            switch (primitiveType.ToLower())
            {
                case "box": unityPrimitive = PrimitiveType.Cube; break;
                case "sphere": unityPrimitive = PrimitiveType.Sphere; break;
                case "cylinder": unityPrimitive = PrimitiveType.Cylinder; break;
                case "capsule": unityPrimitive = PrimitiveType.Capsule; break;
                default: unityPrimitive = PrimitiveType.Cube; break;
            }

            var meshObject = GameObject.CreatePrimitive(unityPrimitive);
            meshObject.name = meshId;
            meshObject.transform.SetParent(meshParent);

            // Apply parameters if available
            if (parameters != null)
            {
                ApplyPrimitiveParameters(meshObject, primitiveType, parameters);
            }

            // Setup material and collider
            SetupMeshObject(meshObject, meshId);
            
            return meshObject;
        }

        private GameObject CreatePlaceholderMesh(string meshId, string meshType)
        {
            // Create a simple placeholder for complex meshes
            var meshObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            meshObject.name = $"{meshId} ({meshType})";
            meshObject.transform.SetParent(meshParent);
            
            SetupMeshObject(meshObject, meshId);
            
            return meshObject;
        }

        private void ApplyPrimitiveParameters(GameObject meshObject, string primitiveType, JObject parameters)
        {
            var transform = meshObject.transform;
            
            // Apply scaling based on parameters
            switch (primitiveType.ToLower())
            {
                case "box":
                    var extents = parameters["extents"] as JArray;
                    if (extents != null && extents.Count >= 3)
                    {
                        transform.localScale = new Vector3(
                            extents[0].Value<float>(),
                            extents[1].Value<float>(),
                            extents[2].Value<float>()
                        );
                    }
                    break;
                    
                case "sphere":
                    var radius = parameters.Value<float?>("radius");
                    if (radius.HasValue)
                    {
                        var scale = radius.Value * 2f; // Unity sphere has radius 0.5
                        transform.localScale = Vector3.one * scale;
                    }
                    break;
                    
                case "cylinder":
                    var cylRadius = parameters.Value<float?>("radius");
                    var height = parameters.Value<float?>("height");
                    if (cylRadius.HasValue || height.HasValue)
                    {
                        var scaleX = cylRadius.HasValue ? cylRadius.Value * 2f : 1f;
                        var scaleY = height.HasValue ? height.Value : 1f;
                        transform.localScale = new Vector3(scaleX, scaleY, scaleX);
                    }
                    break;
            }
        }

        private void SetupMeshObject(GameObject meshObject, string meshId)
        {
            // Apply default material if provided
            var renderer = meshObject.GetComponent<MeshRenderer>();
            if (renderer != null && defaultMaterial != null)
            {
                renderer.material = defaultMaterial;
            }
            // If no material is assigned, Unity will use the default material automatically

            // Add collider if requested
            if (addColliders && meshObject.GetComponent<Collider>() == null)
            {
                meshObject.AddComponent<MeshCollider>();
            }

            // Register in mesh registry
            MeshRegistry.RegisterMesh(meshId, meshObject);
        }

        private async Task UpdateMeshFromServer(string meshId)
        {
            try
            {
                if (useGLTFast)
                {
                    // Method 1: Use GLTFast for better quality (default)
                    await UpdateMeshFromServerGLTF(meshId);
                }
                else
                {
                    // Method 2: Use raw mesh data (fallback)
                    await UpdateMeshFromServerRaw(meshId);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"MeshTool: Error updating mesh '{meshId}' from server: {e.Message}");
                
                // If GLTFast fails, try raw data as fallback
                if (useGLTFast)
                {
                    Debug.Log($"MeshTool: GLTFast failed for '{meshId}', trying raw mesh data...");
                    try
                    {
                        await UpdateMeshFromServerRaw(meshId);
                    }
                    catch (Exception fallbackError)
                    {
                        Debug.LogError($"MeshTool: Raw mesh fallback also failed: {fallbackError.Message}");
                    }
                }
            }
        }

        private async Task UpdateMeshFromServerGLTF(string meshId)
        {
            // Get GLB data from server
            var response = await webSocketClient.SendCommandAsync("export_mesh", new JObject
            {
                ["mesh_id"] = meshId,
                ["format"] = "glb" // Get GLB file data
            });

            if (response.Value<bool>("success"))
            {
                var glbData = response.Value<string>("data"); // base64 encoded GLB
                var glbBytes = System.Convert.FromBase64String(glbData);
                
                await LoadGLBFromBytes(meshId, glbBytes);
            }
            else
            {
                throw new Exception($"Failed to get GLB data: {response.Value<string>("error")}");
            }
        }

        private async Task UpdateMeshFromServerRaw(string meshId)
        {
            // Get raw mesh data from server
            var response = await webSocketClient.SendCommandAsync("export_mesh", new JObject
            {
                ["mesh_id"] = meshId,
                ["format"] = "obj" // Get raw mesh data + vertices/faces
            });

            if (response.Value<bool>("success"))
            {
                var vertices = response["vertices"] as JArray;
                var faces = response["faces"] as JArray;
                var normals = response["normals"] as JArray;

                if (vertices != null && faces != null)
                {
                    // Convert to Unity mesh using raw data
                    var unityMesh = MeshDataConverter.JsonToMesh(response);
                    
                    // Find the existing GameObject or create a new one
                    var meshObject = MeshRegistry.GetMeshObject(meshId);
                    if (meshObject == null)
                    {
                        // Create new GameObject for new mesh (e.g., transformed meshes)
                        meshObject = new GameObject(meshId);
                        if (meshParent != null)
                            meshObject.transform.SetParent(meshParent);
                        
                        // Add mesh components
                        meshObject.AddComponent<MeshFilter>();
                        meshObject.AddComponent<MeshRenderer>();
                        
                        // Register the new mesh object
                        MeshRegistry.RegisterMesh(meshId, meshObject);
                        Debug.Log($"MeshTool: Created new GameObject for mesh '{meshId}' (raw data)");
                    }
                    
                    var meshFilter = meshObject.GetComponent<MeshFilter>();
                    if (meshFilter != null)
                    {
                        meshFilter.mesh = unityMesh;
                        Debug.Log($"MeshTool: Updated mesh '{meshId}' with {vertices.Count} vertices from server (raw data)");
                    }
                }
            }
            else
            {
                throw new Exception($"Failed to get raw mesh data: {response.Value<string>("error")}");
            }
        }

        private async Task LoadGLBFromBytes(string meshId, byte[] glbBytes)
        {
            var gltfImport = new GltfImport();
            
            var success = await gltfImport.Load(glbBytes);
            if (success)
            {
                // Find the existing GameObject or create a new one
                var meshObject = MeshRegistry.GetMeshObject(meshId);
                if (meshObject == null)
                {
                    // Create new GameObject for new mesh (e.g., transformed meshes)
                    meshObject = new GameObject(meshId);
                    if (meshParent != null)
                        meshObject.transform.SetParent(meshParent);
                    
                    // Register the new mesh object
                    MeshRegistry.RegisterMesh(meshId, meshObject);
                    Debug.Log($"MeshTool: Created new GameObject for mesh '{meshId}'");
                }
                
                // Clear existing mesh components
                var existingMeshFilter = meshObject.GetComponent<MeshFilter>();
                var existingMeshRenderer = meshObject.GetComponent<MeshRenderer>();
                
                if (existingMeshFilter != null) DestroyImmediate(existingMeshFilter);
                if (existingMeshRenderer != null) DestroyImmediate(existingMeshRenderer);
                
                // Clear any existing children
                for (int i = meshObject.transform.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(meshObject.transform.GetChild(i).gameObject);
                }
                
                // Instantiate the GLTF content as children
                var instantiator = new GameObjectInstantiator(gltfImport, meshObject.transform);
                await gltfImport.InstantiateMainSceneAsync(instantiator);
                
                Debug.Log($"MeshTool: Updated mesh '{meshId}' using GLTFast with {glbBytes.Length} bytes");
            }
            else
            {
                throw new Exception("Failed to load GLB data with GLTFast");
            }
        }

        #endregion
    }
}
