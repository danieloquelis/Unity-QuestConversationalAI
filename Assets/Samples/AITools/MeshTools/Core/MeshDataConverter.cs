using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace MeshTools
{
    /// <summary>
    /// Utility class for converting between Unity mesh data and JSON format
    /// used by the Python mesh tool server.
    /// </summary>
    public static class MeshDataConverter
    {
        /// <summary>
        /// Convert Unity mesh to JSON format for the mesh tool server
        /// </summary>
        public static JObject MeshToJson(Mesh mesh)
        {
            if (mesh == null) return null;

            var vertices = new JArray();
            foreach (var vertex in mesh.vertices)
            {
                vertices.Add(new JArray { vertex.x, vertex.y, vertex.z });
            }

            var faces = new JArray();
            var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                faces.Add(new JArray { triangles[i], triangles[i + 1], triangles[i + 2] });
            }

            var normals = new JArray();
            if (mesh.normals != null && mesh.normals.Length > 0)
            {
                foreach (var normal in mesh.normals)
                {
                    normals.Add(new JArray { normal.x, normal.y, normal.z });
                }
            }

            var uvs = new JArray();
            if (mesh.uv != null && mesh.uv.Length > 0)
            {
                foreach (var uv in mesh.uv)
                {
                    uvs.Add(new JArray { uv.x, uv.y });
                }
            }

            return new JObject
            {
                ["vertices"] = vertices,
                ["faces"] = faces,
                ["normals"] = normals,
                ["uvs"] = uvs
            };
        }

        /// <summary>
        /// Convert JSON mesh data from server to Unity Mesh
        /// </summary>
        public static Mesh JsonToMesh(JObject meshData)
        {
            if (meshData == null) return null;

            var mesh = new Mesh();

            // Convert vertices
            var verticesArray = meshData["vertices"] as JArray;
            if (verticesArray != null)
            {
                var vertices = new List<Vector3>();
                foreach (var vertexArray in verticesArray)
                {
                    var v = vertexArray as JArray;
                    if (v != null && v.Count >= 3)
                    {
                        vertices.Add(new Vector3(
                            v[0].Value<float>(),
                            v[1].Value<float>(),
                            v[2].Value<float>()
                        ));
                    }
                }
                mesh.vertices = vertices.ToArray();
            }

            // Convert faces (triangles)
            var facesArray = meshData["faces"] as JArray;
            if (facesArray != null)
            {
                var triangles = new List<int>();
                foreach (var faceArray in facesArray)
                {
                    var f = faceArray as JArray;
                    if (f != null && f.Count >= 3)
                    {
                        triangles.Add(f[0].Value<int>());
                        triangles.Add(f[1].Value<int>());
                        triangles.Add(f[2].Value<int>());
                    }
                }
                mesh.triangles = triangles.ToArray();
            }

            // Convert normals if available
            var normalsArray = meshData["normals"] as JArray;
            if (normalsArray != null && normalsArray.Count > 0)
            {
                var normals = new List<Vector3>();
                foreach (var normalArray in normalsArray)
                {
                    var n = normalArray as JArray;
                    if (n != null && n.Count >= 3)
                    {
                        normals.Add(new Vector3(
                            n[0].Value<float>(),
                            n[1].Value<float>(),
                            n[2].Value<float>()
                        ));
                    }
                }
                if (normals.Count == mesh.vertexCount)
                {
                    mesh.normals = normals.ToArray();
                }
            }

            // Convert UVs if available
            var uvsArray = meshData["uvs"] as JArray;
            if (uvsArray != null && uvsArray.Count > 0)
            {
                var uvs = new List<Vector2>();
                foreach (var uvArray in uvsArray)
                {
                    var uv = uvArray as JArray;
                    if (uv != null && uv.Count >= 2)
                    {
                        uvs.Add(new Vector2(
                            uv[0].Value<float>(),
                            uv[1].Value<float>()
                        ));
                    }
                }
                if (uvs.Count == mesh.vertexCount)
                {
                    mesh.uv = uvs.ToArray();
                }
            }

            // Recalculate bounds and normals if needed
            mesh.RecalculateBounds();
            if (mesh.normals == null || mesh.normals.Length == 0)
            {
                mesh.RecalculateNormals();
            }

            return mesh;
        }

        /// <summary>
        /// Convert Unity Vector3 to JSON array
        /// </summary>
        public static JArray Vector3ToJson(Vector3 vector)
        {
            return new JArray { vector.x, vector.y, vector.z };
        }

        /// <summary>
        /// Convert JSON array to Unity Vector3
        /// </summary>
        public static Vector3 JsonToVector3(JArray array)
        {
            if (array == null || array.Count < 3) return Vector3.zero;
            
            return new Vector3(
                array[0].Value<float>(),
                array[1].Value<float>(),
                array[2].Value<float>()
            );
        }

        /// <summary>
        /// Convert Unity transform matrix to JSON array
        /// </summary>
        public static JArray TransformToJson(Transform transform)
        {
            var matrix = transform.localToWorldMatrix;
            var result = new JArray();
            
            for (int row = 0; row < 4; row++)
            {
                var rowArray = new JArray();
                for (int col = 0; col < 4; col++)
                {
                    rowArray.Add(matrix[row, col]);
                }
                result.Add(rowArray);
            }
            
            return result;
        }

        /// <summary>
        /// Get mesh bounds as JSON object
        /// </summary>
        public static JObject BoundsToJson(Bounds bounds)
        {
            return new JObject
            {
                ["center"] = Vector3ToJson(bounds.center),
                ["size"] = Vector3ToJson(bounds.size),
                ["min"] = Vector3ToJson(bounds.min),
                ["max"] = Vector3ToJson(bounds.max)
            };
        }
    }
}
