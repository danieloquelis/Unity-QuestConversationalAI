using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MeshTools
{
    /// <summary>
    /// Agent-accessible bridge exposing MeshTools functionality.
    /// Methods conform to ToolBindings expectations: Task<JObject> Fn(JObject args).
    /// </summary>
    public sealed class MeshToolsAgentBridge : MonoBehaviour
    {
        [SerializeField] private MeshTool meshTool;

        private void Awake()
        {
            if (meshTool == null)
                meshTool = GetComponent<MeshTool>() ?? gameObject.AddComponent<MeshTool>();
        }

        #region Primitive Creation

        /// <summary>
        /// Create a box primitive
        /// </summary>
        public async Task<JObject> MeshTool_CreateBox(JObject args)
        {
            try
            {
                Debug.Log($"[AI->MeshTool] CreateBox called with args: {args}");
                
                var id = args.Value<string>("id");
                var extents = args["extents"] as JArray ?? new JArray { 1, 1, 1 };
                
                Debug.Log($"[AI->MeshTool] CreateBox - ID: {id}, Extents: [{extents[0]}, {extents[1]}, {extents[2]}]");
                
                var parameters = new JObject { ["extents"] = extents };
                var meshObject = await meshTool.CreatePrimitiveAsync(id, "box", parameters);
                
                var result = new JObject 
                { 
                    ["success"] = meshObject != null,
                    ["mesh_id"] = id,
                    ["message"] = meshObject != null ? "Box created successfully" : "Failed to create box"
                };
                
                Debug.Log($"[AI->MeshTool] CreateBox result: {result}");
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AI->MeshTool] CreateBox error: {e.Message}\nStackTrace: {e.StackTrace}");
                return new JObject 
                { 
                    ["success"] = false, 
                    ["error"] = e.Message 
                };
            }
        }

        /// <summary>
        /// Create a sphere primitive
        /// </summary>
        public async Task<JObject> MeshTool_CreateSphere(JObject args)
        {
            try
            {
                var id = args.Value<string>("id");
                var radius = args.Value<float?>("radius") ?? 1f;
                var subdivisions = args.Value<int?>("subdivisions") ?? 2;
                
                var parameters = new JObject 
                { 
                    ["radius"] = radius,
                    ["subdivisions"] = subdivisions
                };
                
                var meshObject = await meshTool.CreatePrimitiveAsync(id, "sphere", parameters);
                
                return new JObject 
                { 
                    ["success"] = meshObject != null,
                    ["mesh_id"] = id,
                    ["message"] = meshObject != null ? "Sphere created successfully" : "Failed to create sphere"
                };
            }
            catch (Exception e)
            {
                return new JObject 
                { 
                    ["success"] = false, 
                    ["error"] = e.Message 
                };
            }
        }

        /// <summary>
        /// Create a cylinder primitive
        /// </summary>
        public async Task<JObject> MeshTool_CreateCylinder(JObject args)
        {
            try
            {
                Debug.Log($"[AI->MeshTool] CreateCylinder called with args: {args}");
                
                var id = args.Value<string>("id");
                var radius = args.Value<float?>("radius") ?? 1f;
                var height = args.Value<float?>("height") ?? 2f;
                var sections = args.Value<int?>("sections") ?? 32;
                
                Debug.Log($"[AI->MeshTool] CreateCylinder - ID: {id}, Radius: {radius}, Height: {height}, Sections: {sections}");
                
                var parameters = new JObject 
                { 
                    ["radius"] = radius,
                    ["height"] = height,
                    ["sections"] = sections
                };
                
                var meshObject = await meshTool.CreatePrimitiveAsync(id, "cylinder", parameters);
                
                var result = new JObject 
                { 
                    ["success"] = meshObject != null,
                    ["mesh_id"] = id,
                    ["message"] = meshObject != null ? "Cylinder created successfully" : "Failed to create cylinder"
                };
                
                Debug.Log($"[AI->MeshTool] CreateCylinder result: {result}");
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AI->MeshTool] CreateCylinder error: {e.Message}\nStackTrace: {e.StackTrace}");
                return new JObject 
                { 
                    ["success"] = false, 
                    ["error"] = e.Message 
                };
            }
        }

        /// <summary>
        /// Create a capsule primitive
        /// </summary>
        public async Task<JObject> MeshTool_CreateCapsule(JObject args)
        {
            try
            {
                var id = args.Value<string>("id");
                var radius = args.Value<float?>("radius") ?? 1f;
                var height = args.Value<float?>("height") ?? 2f;
                
                var parameters = new JObject 
                { 
                    ["radius"] = radius,
                    ["height"] = height
                };
                
                var meshObject = await meshTool.CreatePrimitiveAsync(id, "capsule", parameters);
                
                return new JObject 
                { 
                    ["success"] = meshObject != null,
                    ["mesh_id"] = id,
                    ["message"] = meshObject != null ? "Capsule created successfully" : "Failed to create capsule"
                };
            }
            catch (Exception e)
            {
                return new JObject 
                { 
                    ["success"] = false, 
                    ["error"] = e.Message 
                };
            }
        }

        #endregion

        #region Complex Meshes

        /// <summary>
        /// Create a torus mesh
        /// </summary>
        public async Task<JObject> MeshTool_CreateTorus(JObject args)
        {
            try
            {
                var id = args.Value<string>("id");
                var majorRadius = args.Value<float?>("major_radius") ?? 1f;
                var minorRadius = args.Value<float?>("minor_radius") ?? 0.3f;
                var majorSections = args.Value<int?>("major_sections") ?? 32;
                var minorSections = args.Value<int?>("minor_sections") ?? 16;
                
                var parameters = new JObject 
                { 
                    ["major_radius"] = majorRadius,
                    ["minor_radius"] = minorRadius,
                    ["major_sections"] = majorSections,
                    ["minor_sections"] = minorSections
                };
                
                var meshObject = await meshTool.CreateComplexMeshAsync(id, "torus", parameters);
                
                return new JObject 
                { 
                    ["success"] = meshObject != null,
                    ["mesh_id"] = id,
                    ["message"] = meshObject != null ? "Torus created successfully" : "Failed to create torus"
                };
            }
            catch (Exception e)
            {
                return new JObject 
                { 
                    ["success"] = false, 
                    ["error"] = e.Message 
                };
            }
        }

        /// <summary>
        /// Create an icosphere mesh
        /// </summary>
        public async Task<JObject> MeshTool_CreateIcosphere(JObject args)
        {
            try
            {
                var id = args.Value<string>("id");
                var radius = args.Value<float?>("radius") ?? 1f;
                var subdivisions = args.Value<int?>("subdivisions") ?? 2;
                
                var parameters = new JObject 
                { 
                    ["radius"] = radius,
                    ["subdivisions"] = subdivisions
                };
                
                var meshObject = await meshTool.CreateComplexMeshAsync(id, "icosphere", parameters);
                
                return new JObject 
                { 
                    ["success"] = meshObject != null,
                    ["mesh_id"] = id,
                    ["message"] = meshObject != null ? "Icosphere created successfully" : "Failed to create icosphere"
                };
            }
            catch (Exception e)
            {
                return new JObject 
                { 
                    ["success"] = false, 
                    ["error"] = e.Message 
                };
            }
        }

        #endregion

        #region Boolean Operations

        /// <summary>
        /// Perform boolean union operation
        /// </summary>
        public async Task<JObject> MeshTool_BooleanUnion(JObject args)
        {
            try
            {
                var meshA = args.Value<string>("mesh_a");
                var meshB = args.Value<string>("mesh_b");
                var resultId = args.Value<string>("result_id");
                
                var success = await meshTool.BooleanOperationAsync(meshA, meshB, "union", resultId);
                
                return new JObject 
                { 
                    ["success"] = success,
                    ["result_id"] = resultId,
                    ["message"] = success ? "Boolean union completed" : "Boolean union failed"
                };
            }
            catch (Exception e)
            {
                return new JObject 
                { 
                    ["success"] = false, 
                    ["error"] = e.Message 
                };
            }
        }

        /// <summary>
        /// Perform boolean difference operation
        /// </summary>
        public async Task<JObject> MeshTool_BooleanDifference(JObject args)
        {
            try
            {
                Debug.Log($"[AI->MeshTool] BooleanDifference called with args: {args}");
                
                var meshA = args.Value<string>("mesh_a");
                var meshB = args.Value<string>("mesh_b");
                var resultId = args.Value<string>("result_id");
                var hideInputs = args.Value<bool?>("hide_inputs") ?? true; // Default to hiding inputs
                
                Debug.Log($"[AI->MeshTool] BooleanDifference - MeshA: '{meshA}', MeshB: '{meshB}', ResultID: '{resultId}', HideInputs: {hideInputs}");
                
                var success = await meshTool.BooleanOperationAsync(meshA, meshB, "difference", resultId);
                
                // Handle input mesh visibility
                if (success && !hideInputs)
                {
                    // Re-enable the input meshes if user wants to keep them visible
                    var meshAObject = MeshTools.MeshRegistry.GetMeshObject(meshA);
                    var meshBObject = MeshTools.MeshRegistry.GetMeshObject(meshB);
                    
                    if (meshAObject != null) meshAObject.SetActive(true);
                    if (meshBObject != null) meshBObject.SetActive(true);
                    
                    Debug.Log($"[AI->MeshTool] Kept input meshes visible as requested");
                }
                
                var result = new JObject 
                { 
                    ["success"] = success,
                    ["result_id"] = resultId,
                    ["message"] = success ? "Boolean difference completed" : "Boolean difference failed"
                };
                
                Debug.Log($"[AI->MeshTool] BooleanDifference result: {result}");
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AI->MeshTool] BooleanDifference error: {e.Message}\nStackTrace: {e.StackTrace}");
                return new JObject 
                { 
                    ["success"] = false, 
                    ["error"] = e.Message 
                };
            }
        }

        /// <summary>
        /// Perform boolean intersection operation
        /// </summary>
        public async Task<JObject> MeshTool_BooleanIntersection(JObject args)
        {
            try
            {
                var meshA = args.Value<string>("mesh_a");
                var meshB = args.Value<string>("mesh_b");
                var resultId = args.Value<string>("result_id");
                
                var success = await meshTool.BooleanOperationAsync(meshA, meshB, "intersection", resultId);
                
                return new JObject 
                { 
                    ["success"] = success,
                    ["result_id"] = resultId,
                    ["message"] = success ? "Boolean intersection completed" : "Boolean intersection failed"
                };
            }
            catch (Exception e)
            {
                return new JObject 
                { 
                    ["success"] = false, 
                    ["error"] = e.Message 
                };
            }
        }

        #endregion

        #region Transformations

        /// <summary>
        /// Transform a mesh (translate, rotate, scale)
        /// </summary>
        public async Task<JObject> MeshTool_TransformMesh(JObject args)
        {
            try
            {
                var meshId = args.Value<string>("mesh_id");
                var resultId = args.Value<string>("result_id");
                
                Vector3? translate = null;
                Vector3? rotateAxis = null;
                float? rotateAngle = null;
                Vector3? scale = null;
                
                // Parse translation
                if (args["translate"] is JArray translateArray && translateArray.Count >= 3)
                {
                    translate = new Vector3(
                        translateArray[0].Value<float>(),
                        translateArray[1].Value<float>(),
                        translateArray[2].Value<float>()
                    );
                }
                
                // Parse rotation
                if (args["rotate"] is JObject rotateObj)
                {
                    if (rotateObj["axis"] is JArray axisArray && axisArray.Count >= 3)
                    {
                        rotateAxis = new Vector3(
                            axisArray[0].Value<float>(),
                            axisArray[1].Value<float>(),
                            axisArray[2].Value<float>()
                        );
                    }
                    rotateAngle = rotateObj.Value<float?>("angle");
                }
                
                // Parse scale
                if (args["scale"] is JArray scaleArray && scaleArray.Count >= 3)
                {
                    scale = new Vector3(
                        scaleArray[0].Value<float>(),
                        scaleArray[1].Value<float>(),
                        scaleArray[2].Value<float>()
                    );
                }
                else if (args["scale"] is JValue scaleValue)
                {
                    var uniformScale = scaleValue.Value<float>();
                    scale = Vector3.one * uniformScale;
                }
                
                var success = await meshTool.TransformMeshAsync(meshId, translate, rotateAxis, rotateAngle, scale, resultId);
                
                return new JObject 
                { 
                    ["success"] = success,
                    ["mesh_id"] = resultId ?? meshId,
                    ["message"] = success ? "Transform completed" : "Transform failed"
                };
            }
            catch (Exception e)
            {
                return new JObject 
                { 
                    ["success"] = false, 
                    ["error"] = e.Message 
                };
            }
        }

        #endregion

        #region Mesh Management

        /// <summary>
        /// Export mesh to file
        /// </summary>
        public async Task<JObject> MeshTool_ExportMesh(JObject args)
        {
            try
            {
                var meshId = args.Value<string>("mesh_id");
                var filename = args.Value<string>("filename");
                var format = args.Value<string>("format") ?? "glb";
                
                var filepath = await meshTool.ExportMeshAsync(meshId, filename, format);
                
                return new JObject 
                { 
                    ["success"] = !string.IsNullOrEmpty(filepath),
                    ["filepath"] = filepath,
                    ["message"] = !string.IsNullOrEmpty(filepath) ? "Export completed" : "Export failed"
                };
            }
            catch (Exception e)
            {
                return new JObject 
                { 
                    ["success"] = false, 
                    ["error"] = e.Message 
                };
            }
        }

        /// <summary>
        /// Get mesh information
        /// </summary>
        public async Task<JObject> MeshTool_GetMeshInfo(JObject args)
        {
            try
            {
                var meshId = args.Value<string>("mesh_id");
                var info = await meshTool.GetMeshInfoAsync(meshId);
                
                if (info != null)
                {
                    return info;
                }
                else
                {
                    return new JObject 
                    { 
                        ["success"] = false,
                        ["error"] = "Failed to get mesh info"
                    };
                }
            }
            catch (Exception e)
            {
                return new JObject 
                { 
                    ["success"] = false, 
                    ["error"] = e.Message 
                };
            }
        }

        /// <summary>
        /// Delete a mesh
        /// </summary>
        public async Task<JObject> MeshTool_DeleteMesh(JObject args)
        {
            try
            {
                Debug.Log($"[AI->MeshTool] DeleteMesh called with args: {args}");
                
                var meshId = args.Value<string>("mesh_id");
                
                Debug.Log($"[AI->MeshTool] DeleteMesh - MeshID: '{meshId}'");
                
                // Delete from server
                var success = await meshTool.DeleteMeshAsync(meshId);
                
                // Remove from Unity registry if server deletion was successful
                if (success)
                {
                    MeshRegistry.UnregisterMesh(meshId, destroy: true);
                    Debug.Log($"[AI->MeshTool] Removed mesh '{meshId}' from Unity registry");
                }
                
                var result = new JObject 
                { 
                    ["success"] = success,
                    ["mesh_id"] = meshId,
                    ["message"] = success ? "Mesh deleted successfully" : "Failed to delete mesh"
                };
                
                Debug.Log($"[AI->MeshTool] DeleteMesh result: {result}");
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AI->MeshTool] DeleteMesh error: {e.Message}\nStackTrace: {e.StackTrace}");
                return new JObject 
                { 
                    ["success"] = false, 
                    ["error"] = e.Message 
                };
            }
        }

        /// <summary>
        /// Check connection status
        /// </summary>
        public async Task<JObject> MeshTool_GetConnectionStatus(JObject args)
        {
            await Task.Yield();
            
            return new JObject 
            { 
                ["success"] = true,
                ["connected"] = meshTool.IsConnected,
                ["server_url"] = meshTool.Client?.ServerUrl ?? "ws://localhost:8765"
            };
        }

        /// <summary>
        /// Show or hide a mesh by ID
        /// </summary>
        public async Task<JObject> MeshTool_SetMeshVisibility(JObject args)
        {
            try
            {
                Debug.Log($"[AI->MeshTool] SetMeshVisibility called with args: {args}");
                
                var meshId = args.Value<string>("mesh_id");
                var visible = args.Value<bool>("visible");
                
                var meshObject = MeshRegistry.GetMeshObject(meshId);
                if (meshObject != null)
                {
                    meshObject.SetActive(visible);
                    
                    var result = new JObject 
                    { 
                        ["success"] = true,
                        ["mesh_id"] = meshId,
                        ["visible"] = visible,
                        ["message"] = $"Mesh visibility set to {visible}"
                    };
                    
                    Debug.Log($"[AI->MeshTool] SetMeshVisibility result: {result}");
                    return result;
                }
                else
                {
                    return new JObject 
                    { 
                        ["success"] = false,
                        ["error"] = $"Mesh '{meshId}' not found"
                    };
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AI->MeshTool] SetMeshVisibility error: {e.Message}\nStackTrace: {e.StackTrace}");
                return new JObject 
                { 
                    ["success"] = false, 
                    ["error"] = e.Message 
                };
            }
        }



        #endregion
    }
}
