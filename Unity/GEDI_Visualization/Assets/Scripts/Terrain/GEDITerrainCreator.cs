using UnityEngine;
using System;
using System.Collections.Generic;

using GEDIGlobals;
using TriangleNet.Geometry;

public class GEDITerrainCreator
{

    public static Mesh generateWireframe(Polygon polygon, Dictionary<int, TerrainPoint> pointMap)
    {
        // build wireframe mesh
        List<Vector3> wireframeVertices = new List<Vector3>();
        List<int> lineIndices = new List<int>();
        HashSet<string> processedEdges = new HashSet<string>();

        
        foreach (var triangle in polygon.Triangulate().Triangles)
        {
            for (int i = 0; i < 3; i++)
            {
                int v1ID = triangle.GetVertexID(i);
                int v2ID = triangle.GetVertexID((i + 1) % 3);
                
                string edgeKey1 = $"{v1ID}-{v2ID}";
                string edgeKey2 = $"{v2ID}-{v1ID}";
                
                if (!processedEdges.Contains(edgeKey1) && !processedEdges.Contains(edgeKey2))
                {
                    TerrainPoint p1 = pointMap[v1ID];
                    TerrainPoint p2 = pointMap[v2ID];
                    
                    wireframeVertices.Add(p1.position);
                    wireframeVertices.Add(p2.position);
                    
                    lineIndices.Add(wireframeVertices.Count - 2);
                    lineIndices.Add(wireframeVertices.Count - 1);
                    
                    processedEdges.Add(edgeKey1);
                }
            }
        }
        
        Mesh unityMesh = new Mesh();
        unityMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        unityMesh.vertices = wireframeVertices.ToArray();
        unityMesh.SetIndices(lineIndices.ToArray(), MeshTopology.Lines, 0);
        unityMesh.RecalculateBounds();

        return unityMesh;
    }

    public static Mesh generateSolid(Polygon polygon, Dictionary<int, TerrainPoint> pointMap, Vector4 textureGeoBounds)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        
        // create vertex to index mapping
        Dictionary<int, int> vertexIndexMap = new Dictionary<int, int>();
        
        foreach (var triangle in polygon.Triangulate().Triangles)
        {
            // collect the three vertex indices for this triangle
            int[] vertIndices = new int[3];
            
            for (int i = 0; i < 3; i++)
            {
                int vertexID = triangle.GetVertexID(i);
                
                // if we haven't processed this vertex yet
                if (!vertexIndexMap.ContainsKey(vertexID))
                {
                    TerrainPoint point = pointMap[vertexID];
                    vertices.Add(point.position);
                    
                    // create UV based on geographic position
                    // float u = (point.longitude - minLon) / (maxLon - minLon);
                    // float v = (point.latitude - minLat) / (maxLat - minLat);

                    // UV based on texture bounds
                    float u = (point.longitude - textureGeoBounds.x) / (textureGeoBounds.y - textureGeoBounds.x);
                    float v = (point.latitude - textureGeoBounds.z) / (textureGeoBounds.w - textureGeoBounds.z);
                    
                    uvs.Add(new Vector2(u, v));
                    
                    vertexIndexMap[vertexID] = vertices.Count - 1;
                }
                
                // store this vertex index
                vertIndices[i] = vertexIndexMap[vertexID];
            }
            
            // add indices
            triangles.Add(vertIndices[0]);
            triangles.Add(vertIndices[2]);
            triangles.Add(vertIndices[1]);
        }
        
        Mesh unityMesh = new Mesh();
        unityMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        unityMesh.vertices = vertices.ToArray();
        unityMesh.triangles = triangles.ToArray();
        unityMesh.uv = uvs.ToArray();
        unityMesh.RecalculateNormals();

        return unityMesh;
    }
}