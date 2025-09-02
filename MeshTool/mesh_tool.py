#!/usr/bin/env python3
"""
Mesh Tool API Server
A websocket-based API for creating and manipulating 3D meshes using trimesh.
Designed to be consumed by Unity applications.
"""

import asyncio
import json
import logging
import traceback
from typing import Dict, Any, Optional, List, Tuple
import websockets
import trimesh
import numpy as np
from trimesh.primitives import Sphere, Box, Cylinder, Capsule
from trimesh.creation import extrude_polygon
import base64
import io

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class MeshToolServer:
    def __init__(self, host: str = "localhost", port: int = 8765):
        self.host = host
        self.port = port
        self.meshes: Dict[str, trimesh.Trimesh] = {}
        
    async def handle_client(self, websocket, path):
        """Handle incoming websocket connections and messages"""
        logger.info(f"Client connected from {websocket.remote_address}")
        
        try:
            async for message in websocket:
                try:
                    data = json.loads(message)
                    response = await self.process_command(data)
                    await websocket.send(json.dumps(response))
                except json.JSONDecodeError:
                    error_response = {
                        "success": False,
                        "error": "Invalid JSON format"
                    }
                    await websocket.send(json.dumps(error_response))
                except Exception as e:
                    error_response = {
                        "success": False,
                        "error": str(e),
                        "traceback": traceback.format_exc()
                    }
                    await websocket.send(json.dumps(error_response))
                    
        except websockets.exceptions.ConnectionClosed:
            logger.info("Client disconnected")
        except Exception as e:
            logger.error(f"Error handling client: {e}")

    async def process_command(self, data: Dict[str, Any]) -> Dict[str, Any]:
        """Process incoming commands and return responses"""
        command = data.get("command")
        params = data.get("params", {})
        
        if command == "create_primitive":
            return await self.create_primitive(params)
        elif command == "boolean_operation":
            return await self.boolean_operation(params)
        elif command == "extrude":
            return await self.extrude_mesh(params)
        elif command == "bevel":
            return await self.bevel_mesh(params)
        elif command == "get_mesh_info":
            return await self.get_mesh_info(params)
        elif command == "export_mesh":
            return await self.export_mesh(params)
        elif command == "save_mesh_file":
            return await self.save_mesh_file(params)
        elif command == "list_meshes":
            return await self.list_meshes()
        elif command == "delete_mesh":
            return await self.delete_mesh(params)
        elif command == "transform_mesh":
            return await self.transform_mesh(params)
        elif command == "create_complex_mesh":
            return await self.create_complex_mesh(params)
        else:
            return {
                "success": False,
                "error": f"Unknown command: {command}"
            }

    async def create_primitive(self, params: Dict[str, Any]) -> Dict[str, Any]:
        """Create primitive meshes (sphere, box, cylinder, etc.)"""
        try:
            mesh_id = params.get("mesh_id", f"mesh_{len(self.meshes)}")
            primitive_type = params.get("type", "box")
            
            if primitive_type == "box":
                extents = params.get("extents", [1.0, 1.0, 1.0])
                mesh = Box(extents=extents)
            elif primitive_type == "sphere":
                radius = params.get("radius", 1.0)
                subdivisions = params.get("subdivisions", 2)
                mesh = Sphere(radius=radius, subdivisions=subdivisions)
            elif primitive_type == "cylinder":
                radius = params.get("radius", 1.0)
                height = params.get("height", 2.0)
                sections = params.get("sections", 32)
                mesh = Cylinder(radius=radius, height=height, sections=sections)
            elif primitive_type == "capsule":
                radius = params.get("radius", 1.0)
                height = params.get("height", 2.0)
                mesh = Capsule(radius=radius, height=height)
            else:
                return {
                    "success": False,
                    "error": f"Unknown primitive type: {primitive_type}"
                }
            
            # Apply transform if provided
            if "transform" in params:
                transform_matrix = np.array(params["transform"]).reshape(4, 4)
                mesh.apply_transform(transform_matrix)
            
            self.meshes[mesh_id] = mesh
            
            return {
                "success": True,
                "mesh_id": mesh_id,
                "vertices_count": len(mesh.vertices),
                "faces_count": len(mesh.faces),
                "bounds": mesh.bounds.tolist()
            }
            
        except Exception as e:
            return {
                "success": False,
                "error": str(e)
            }

    async def boolean_operation(self, params: Dict[str, Any]) -> Dict[str, Any]:
        """Perform boolean operations between meshes"""
        try:
            mesh_a_id = params.get("mesh_a")
            mesh_b_id = params.get("mesh_b")
            operation = params.get("operation", "union")  # union, difference, intersection
            result_id = params.get("result_id", f"result_{len(self.meshes)}")
            
            if mesh_a_id not in self.meshes or mesh_b_id not in self.meshes:
                return {
                    "success": False,
                    "error": "One or both meshes not found"
                }
            
            mesh_a = self.meshes[mesh_a_id]
            mesh_b = self.meshes[mesh_b_id]
            
            try:
                # Process meshes to ensure they're clean and watertight
                mesh_a = mesh_a.process()
                mesh_b = mesh_b.process()
                
                logger.info(f"Mesh A watertight: {mesh_a.is_watertight}, Mesh B watertight: {mesh_b.is_watertight}")
                
                # Try direct manifold3d API first (this works!)
                try:
                    import manifold3d as m3d
                    logger.info("Using direct manifold3d API")
                    
                    # Create manifold objects with correct data types
                    cube_manifold = m3d.Manifold(m3d.Mesh(mesh_a.vertices.astype(np.float32), mesh_a.faces.astype(np.uint32)))
                    cylinder_manifold = m3d.Manifold(m3d.Mesh(mesh_b.vertices.astype(np.float32), mesh_b.faces.astype(np.uint32)))
                    
                    # Perform operation
                    if operation == "union":
                        result_manifold = cube_manifold + cylinder_manifold
                    elif operation == "difference":
                        result_manifold = cube_manifold - cylinder_manifold
                    elif operation == "intersection":
                        result_manifold = cube_manifold ^ cylinder_manifold
                    else:
                        raise Exception(f"Unknown operation: {operation}")
                    
                    # Convert back to trimesh
                    result_mesh = result_manifold.to_mesh()
                    result = trimesh.Trimesh(vertices=result_mesh.vert_properties, faces=result_mesh.tri_verts)
                    logger.info("Direct manifold3d boolean operation succeeded")
                    
                except ImportError:
                    logger.warning("manifold3d not available, trying other methods")
                    # Try with Blender engine if available
                    try:
                        logger.info("Attempting boolean operation with Blender engine")
                        if trimesh.interfaces.blender.exists:
                            result = trimesh.interfaces.blender.boolean([mesh_a, mesh_b], operation=operation)
                            logger.info("Blender engine boolean operation succeeded")
                        else:
                            raise Exception("Blender not available")
                            
                    except Exception as blender_error:
                        logger.warning(f"Blender engine failed: {blender_error}")
                        
                        # Final fallback to basic trimesh operations
                        logger.info("Falling back to basic trimesh operations")
                        if operation == "union":
                            result = mesh_a.union(mesh_b)
                        elif operation == "difference":
                            result = mesh_a.difference(mesh_b)
                        elif operation == "intersection":
                            result = mesh_a.intersection(mesh_b)
                        else:
                            raise Exception(f"Unknown operation: {operation}")
                            
                except Exception as manifold_error:
                    logger.warning(f"Direct manifold3d failed: {manifold_error}")
                    
                    # Try with Blender engine if available
                    try:
                        logger.info("Attempting boolean operation with Blender engine")
                        if trimesh.interfaces.blender.exists:
                            result = trimesh.interfaces.blender.boolean([mesh_a, mesh_b], operation=operation)
                            logger.info("Blender engine boolean operation succeeded")
                        else:
                            raise Exception("Blender not available")
                            
                    except Exception as blender_error:
                        logger.warning(f"Blender engine failed: {blender_error}")
                        
                        # Final fallback to basic trimesh operations
                        logger.info("Falling back to basic trimesh operations")
                        if operation == "union":
                            result = mesh_a.union(mesh_b)
                        elif operation == "difference":
                            result = mesh_a.difference(mesh_b)
                        elif operation == "intersection":
                            result = mesh_a.intersection(mesh_b)
                        else:
                            raise Exception(f"Unknown operation: {operation}")
                    
                # Handle different result types
                if result is None:
                    raise Exception("Boolean operation produced None result")
                
                # If result is a Scene (multiple meshes), get the first mesh
                if hasattr(result, 'geometry') and hasattr(result, 'graph'):
                    # It's a Scene object, extract the first mesh
                    if len(result.geometry) == 0:
                        raise Exception("Boolean operation produced empty scene")
                    # Get the first geometry from the scene
                    first_geom_name = list(result.geometry.keys())[0]
                    result = result.geometry[first_geom_name]
                    logger.info("Extracted mesh from Scene object")
                
                # Now check if it's a valid mesh
                if not hasattr(result, 'vertices') or len(result.vertices) == 0:
                    raise Exception("Boolean operation produced empty result")
                    
            except Exception as bool_error:
                logger.error(f"Boolean operation failed: {str(bool_error)}")
                
                # Fallback for union only
                if operation == "union":
                    # Fallback to simple concatenation for union
                    result = mesh_a + mesh_b
                    logger.info("Using mesh concatenation as union fallback")
                else:
                    return {
                        "success": False,
                        "error": f"Boolean {operation} failed: {str(bool_error)}. Manifold3d is installed but may have compatibility issues."
                    }
            
            if result is None or len(result.vertices) == 0:
                return {
                    "success": False,
                    "error": "Boolean operation resulted in empty mesh"
                }
            
            self.meshes[result_id] = result
            
            return {
                "success": True,
                "result_id": result_id,
                "vertices_count": len(result.vertices),
                "faces_count": len(result.faces),
                "bounds": result.bounds.tolist()
            }
            
        except Exception as e:
            return {
                "success": False,
                "error": str(e)
            }

    async def extrude_mesh(self, params: Dict[str, Any]) -> Dict[str, Any]:
        """Extrude a 2D polygon to create a 3D mesh"""
        try:
            mesh_id = params.get("mesh_id", f"extruded_{len(self.meshes)}")
            polygon_points = params.get("polygon", [[0, 0], [1, 0], [1, 1], [0, 1]])
            height = params.get("height", 1.0)
            
            # Create polygon from points
            from shapely.geometry import Polygon
            polygon = Polygon(polygon_points)
            
            # Extrude the polygon
            mesh = extrude_polygon(polygon, height)
            
            self.meshes[mesh_id] = mesh
            
            return {
                "success": True,
                "mesh_id": mesh_id,
                "vertices_count": len(mesh.vertices),
                "faces_count": len(mesh.faces),
                "bounds": mesh.bounds.tolist()
            }
            
        except Exception as e:
            return {
                "success": False,
                "error": str(e)
            }

    async def bevel_mesh(self, params: Dict[str, Any]) -> Dict[str, Any]:
        """Apply bevel operation to mesh edges"""
        try:
            mesh_id = params.get("mesh_id")
            bevel_distance = params.get("distance", 0.1)
            
            if mesh_id not in self.meshes:
                return {
                    "success": False,
                    "error": "Mesh not found"
                }
            
            mesh = self.meshes[mesh_id]
            
            # Simple bevel approximation using smoothing
            # For more complex beveling, you might need additional libraries
            smoothed = mesh.smoothed()
            
            # Scale down slightly to simulate bevel
            scale_factor = 1.0 - bevel_distance
            transform = np.eye(4)
            transform[:3, :3] *= scale_factor
            smoothed.apply_transform(transform)
            
            result_id = params.get("result_id", f"beveled_{mesh_id}")
            self.meshes[result_id] = smoothed
            
            return {
                "success": True,
                "result_id": result_id,
                "vertices_count": len(smoothed.vertices),
                "faces_count": len(smoothed.faces),
                "bounds": smoothed.bounds.tolist()
            }
            
        except Exception as e:
            return {
                "success": False,
                "error": str(e)
            }

    async def transform_mesh(self, params: Dict[str, Any]) -> Dict[str, Any]:
        """Apply transformations to a mesh"""
        try:
            mesh_id = params.get("mesh_id")
            
            if mesh_id not in self.meshes:
                return {
                    "success": False,
                    "error": "Mesh not found"
                }
            
            mesh = self.meshes[mesh_id].copy()
            
            # Apply translation
            if "translate" in params:
                translation = np.array(params["translate"])
                mesh.apply_translation(translation)
            
            # Apply rotation
            if "rotate" in params:
                rotation = params["rotate"]
                if "axis" in rotation and "angle" in rotation:
                    axis = np.array(rotation["axis"])
                    angle = rotation["angle"]
                    mesh.apply_transform(trimesh.transformations.rotation_matrix(angle, axis))
            
            # Apply scale
            if "scale" in params:
                scale = params["scale"]
                if isinstance(scale, (int, float)):
                    # Uniform scaling
                    transform = np.eye(4)
                    transform[:3, :3] *= scale
                    mesh.apply_transform(transform)
                else:
                    # Non-uniform scaling
                    transform = np.eye(4)
                    scale_array = np.array(scale)
                    transform[0, 0] = scale_array[0]
                    transform[1, 1] = scale_array[1] 
                    transform[2, 2] = scale_array[2]
                    mesh.apply_transform(transform)
            
            result_id = params.get("result_id", f"transformed_{mesh_id}")
            self.meshes[result_id] = mesh
            
            return {
                "success": True,
                "result_id": result_id,
                "vertices_count": len(mesh.vertices),
                "faces_count": len(mesh.faces),
                "bounds": mesh.bounds.tolist()
            }
            
        except Exception as e:
            return {
                "success": False,
                "error": str(e)
            }

    async def create_complex_mesh(self, params: Dict[str, Any]) -> Dict[str, Any]:
        """Create more complex meshes like torus, icosphere, etc."""
        try:
            mesh_id = params.get("mesh_id", f"complex_{len(self.meshes)}")
            mesh_type = params.get("type")
            
            if mesh_type == "torus":
                major_radius = params.get("major_radius", 1.0)
                minor_radius = params.get("minor_radius", 0.3)
                major_sections = params.get("major_sections", 32)
                minor_sections = params.get("minor_sections", 16)
                
                # Create torus using parametric equations
                u = np.linspace(0, 2 * np.pi, major_sections)
                v = np.linspace(0, 2 * np.pi, minor_sections)
                u, v = np.meshgrid(u, v)
                
                x = (major_radius + minor_radius * np.cos(v)) * np.cos(u)
                y = (major_radius + minor_radius * np.cos(v)) * np.sin(u)
                z = minor_radius * np.sin(v)
                
                vertices = np.column_stack([x.flatten(), y.flatten(), z.flatten()])
                
                # Create faces (simplified triangulation)
                faces = []
                for i in range(major_sections - 1):
                    for j in range(minor_sections - 1):
                        idx = i * minor_sections + j
                        faces.append([idx, idx + 1, idx + minor_sections])
                        faces.append([idx + 1, idx + minor_sections + 1, idx + minor_sections])
                
                mesh = trimesh.Trimesh(vertices=vertices, faces=faces)
                
            elif mesh_type == "icosphere":
                radius = params.get("radius", 1.0)
                subdivisions = params.get("subdivisions", 2)
                mesh = trimesh.creation.icosphere(subdivisions=subdivisions, radius=radius)
                
            else:
                return {
                    "success": False,
                    "error": f"Unknown complex mesh type: {mesh_type}"
                }
            
            self.meshes[mesh_id] = mesh
            
            return {
                "success": True,
                "mesh_id": mesh_id,
                "vertices_count": len(mesh.vertices),
                "faces_count": len(mesh.faces),
                "bounds": mesh.bounds.tolist()
            }
            
        except Exception as e:
            return {
                "success": False,
                "error": str(e)
            }

    async def get_mesh_info(self, params: Dict[str, Any]) -> Dict[str, Any]:
        """Get information about a mesh"""
        try:
            mesh_id = params.get("mesh_id")
            
            if mesh_id not in self.meshes:
                return {
                    "success": False,
                    "error": "Mesh not found"
                }
            
            mesh = self.meshes[mesh_id]
            
            return {
                "success": True,
                "mesh_id": mesh_id,
                "vertices_count": len(mesh.vertices),
                "faces_count": len(mesh.faces),
                "bounds": mesh.bounds.tolist(),
                "volume": float(mesh.volume),
                "surface_area": float(mesh.area),
                "is_watertight": mesh.is_watertight,
                "center_mass": mesh.center_mass.tolist()
            }
            
        except Exception as e:
            return {
                "success": False,
                "error": str(e)
            }

    async def export_mesh(self, params: Dict[str, Any]) -> Dict[str, Any]:
        """Export mesh data in various formats"""
        try:
            mesh_id = params.get("mesh_id")
            format_type = params.get("format", "obj")  # obj, stl, ply, etc.
            
            if mesh_id not in self.meshes:
                return {
                    "success": False,
                    "error": "Mesh not found"
                }
            
            mesh = self.meshes[mesh_id]
            
            # Export to string buffer
            export_data = mesh.export(file_type=format_type)
            
            # Encode as base64 for JSON transmission
            if isinstance(export_data, str):
                encoded_data = base64.b64encode(export_data.encode()).decode()
            else:
                encoded_data = base64.b64encode(export_data).decode()
            
            return {
                "success": True,
                "mesh_id": mesh_id,
                "format": format_type,
                "data": encoded_data,
                "vertices": mesh.vertices.tolist(),
                "faces": mesh.faces.tolist(),
                "normals": mesh.vertex_normals.tolist() if hasattr(mesh, 'vertex_normals') else []
            }
            
        except Exception as e:
            return {
                "success": False,
                "error": str(e)
            }

    async def save_mesh_file(self, params: Dict[str, Any]) -> Dict[str, Any]:
        """Save mesh to file on disk"""
        try:
            mesh_id = params.get("mesh_id")
            filename = params.get("filename")
            format_type = params.get("format", "glb")  # glb, obj, stl, ply
            output_dir = params.get("output_dir", "./output")
            
            if mesh_id not in self.meshes:
                return {
                    "success": False,
                    "error": "Mesh not found"
                }
            
            mesh = self.meshes[mesh_id]
            
            # Create output directory if it doesn't exist
            import os
            os.makedirs(output_dir, exist_ok=True)
            
            # Generate filename if not provided
            if not filename:
                filename = f"{mesh_id}.{format_type}"
            elif not filename.endswith(f".{format_type}"):
                filename = f"{filename}.{format_type}"
            
            filepath = os.path.join(output_dir, filename)
            
            # Export and save the mesh
            mesh.export(filepath)
            
            return {
                "success": True,
                "mesh_id": mesh_id,
                "filepath": os.path.abspath(filepath),
                "format": format_type,
                "file_size": os.path.getsize(filepath)
            }
            
        except Exception as e:
            return {
                "success": False,
                "error": str(e)
            }

    async def list_meshes(self) -> Dict[str, Any]:
        """List all available meshes"""
        try:
            mesh_list = []
            for mesh_id, mesh in self.meshes.items():
                mesh_list.append({
                    "id": mesh_id,
                    "vertices_count": len(mesh.vertices),
                    "faces_count": len(mesh.faces),
                    "bounds": mesh.bounds.tolist()
                })
            
            return {
                "success": True,
                "meshes": mesh_list,
                "total_count": len(self.meshes)
            }
            
        except Exception as e:
            return {
                "success": False,
                "error": str(e)
            }

    async def delete_mesh(self, params: Dict[str, Any]) -> Dict[str, Any]:
        """Delete a mesh from memory"""
        try:
            mesh_id = params.get("mesh_id")
            
            if mesh_id not in self.meshes:
                return {
                    "success": False,
                    "error": "Mesh not found"
                }
            
            del self.meshes[mesh_id]
            
            return {
                "success": True,
                "message": f"Mesh {mesh_id} deleted successfully"
            }
            
        except Exception as e:
            return {
                "success": False,
                "error": str(e)
            }

    async def start_server(self):
        """Start the websocket server"""
        logger.info(f"Starting Mesh Tool Server on {self.host}:{self.port}")
        
        async with websockets.serve(self.handle_client, self.host, self.port):
            logger.info("Server started successfully!")
            logger.info("Available commands:")
            logger.info("- create_primitive: Create basic shapes (box, sphere, cylinder, capsule)")
            logger.info("- create_complex_mesh: Create complex shapes (torus, icosphere)")
            logger.info("- boolean_operation: Union, difference, intersection operations")
            logger.info("- extrude: Extrude 2D polygons to 3D")
            logger.info("- bevel: Apply bevel operations")
            logger.info("- transform_mesh: Apply transformations (translate, rotate, scale)")
            logger.info("- get_mesh_info: Get mesh information")
            logger.info("- export_mesh: Export mesh data")
            logger.info("- list_meshes: List all meshes")
            logger.info("- delete_mesh: Delete a mesh")
            
            # Keep server running
            await asyncio.Future()  # Run forever

def main():
    """Main entry point"""
    server = MeshToolServer()
    try:
        asyncio.run(server.start_server())
    except KeyboardInterrupt:
        logger.info("Server stopped by user")
    except Exception as e:
        logger.error(f"Server error: {e}")

if __name__ == "__main__":
    main()
