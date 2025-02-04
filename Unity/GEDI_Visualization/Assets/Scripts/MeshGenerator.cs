using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MeshGenerator : MonoBehaviour
{
    public Material wireframeMaterial; // Assign this in the Inspector
    public Material mainMeshMaterial;  // Assign this in the Inspector

    private MeshFilter mainMeshFilter;
    private MeshRenderer mainMeshRenderer;

    void Awake()
    {
        mainMeshFilter = GetComponent<MeshFilter>();
        mainMeshRenderer = GetComponent<MeshRenderer>();

        // Assign the main mesh material
        if (mainMeshMaterial != null)
        {
            mainMeshRenderer.material = mainMeshMaterial;
        }
        else
        {
            Debug.LogWarning("Main Mesh Material not assigned.");
        }
    }

    public void GenerateMesh(Dictionary<Vector2Int, CSVParser.GEDIDataPoint> gridDataPoints, float gridCellSize)
    {
        // Clear existing mesh
        Mesh mesh = new Mesh();
        mainMeshFilter.mesh = mesh;

        // Extract grid dimensions
        int minX = int.MaxValue, maxX = int.MinValue;
        int minZ = int.MaxValue, maxZ = int.MinValue;

        foreach (var key in gridDataPoints.Keys)
        {
            if (key.x < minX) minX = key.x;
            if (key.x > maxX) maxX = key.x;
            if (key.y < minZ) minZ = key.y;
            if (key.y > maxZ) maxZ = key.y;
        }

        int gridWidth = maxX - minX + 1;
        int gridHeight = maxZ - minZ + 1;

        // Create arrays for vertices and triangles
        Vector3[,] verticesGrid = new Vector3[gridWidth, gridHeight];
        bool[,] vertexExists = new bool[gridWidth, gridHeight];

        // Fill the vertices grid
        foreach (var kvp in gridDataPoints)
        {
            Vector2Int gridPos = kvp.Key;
            CSVParser.GEDIDataPoint point = kvp.Value;

            int xIndex = gridPos.x - minX;
            int zIndex = gridPos.y - minZ;

            Vector3 pos = LatLong2Unity(point.latitude, point.longitude, point.elevation);
            float rhLevel = point.rh2 * 0.1f; // Adjust the height scaling as needed

            verticesGrid[xIndex, zIndex] = new Vector3(pos.x, pos.y + rhLevel, pos.z);
            vertexExists[xIndex, zIndex] = true;
        }

        // Collect vertices and triangles
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        Dictionary<Vector2Int, int> vertexIndices = new Dictionary<Vector2Int, int>();

        for (int z = 0; z < gridHeight; z++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                if (vertexExists[x, z])
                {
                    // Add vertex
                    vertices.Add(verticesGrid[x, z]);
                    vertexIndices.Add(new Vector2Int(x, z), vertices.Count - 1);

                    // Create triangles if neighboring vertices exist
                    if (x > 0 && z > 0)
                    {
                        if (vertexExists[x - 1, z] && vertexExists[x, z - 1] && vertexExists[x - 1, z - 1])
                        {
                            AddTriangles(triangles, vertexIndices, x, z);
                        }
                    }
                }
            }
        }

        // Assign vertices and triangles to the mesh
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        // Recalculate normals and bounds for proper lighting and culling
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Generate wireframe lines
        GenerateWireframe(mesh);
    }

    private void AddTriangles(List<int> triangles, Dictionary<Vector2Int, int> vertexIndices, int x, int z)
    {
        Vector2Int current = new Vector2Int(x, z);
        Vector2Int left = new Vector2Int(x - 1, z);
        Vector2Int down = new Vector2Int(x, z - 1);
        Vector2Int downLeft = new Vector2Int(x - 1, z - 1);

        // First triangle
        triangles.Add(vertexIndices[current]);
        triangles.Add(vertexIndices[down]);
        triangles.Add(vertexIndices[downLeft]);

        // Second triangle
        triangles.Add(vertexIndices[downLeft]);
        triangles.Add(vertexIndices[left]);
        triangles.Add(vertexIndices[current]);
    }

    private void GenerateWireframe(Mesh mesh)
    {
        // Extract triangles
        int[] tris = mesh.triangles;
        Vector3[] verts = mesh.vertices;

        // Use a HashSet to store unique edges
        HashSet<Edge> edges = new HashSet<Edge>();

        for (int i = 0; i < tris.Length; i += 3)
        {
            Edge e1 = new Edge(tris[i], tris[i + 1]);
            Edge e2 = new Edge(tris[i + 1], tris[i + 2]);
            Edge e3 = new Edge(tris[i + 2], tris[i]);

            edges.Add(e1);
            edges.Add(e2);
            edges.Add(e3);
        }

        // Create line vertices
        List<Vector3> lineVertices = new List<Vector3>();
        foreach (var edge in edges)
        {
            lineVertices.Add(verts[edge.v1]);
            lineVertices.Add(verts[edge.v2]);
        }

        // Check if a wireframe GameObject already exists
        Transform existingWireframe = transform.Find("Wireframe");
        GameObject wireframeGO;
        if (existingWireframe != null)
        {
            wireframeGO = existingWireframe.gameObject;
            // Remove existing MeshFilter and MeshRenderer to avoid duplicates
            DestroyImmediate(wireframeGO.GetComponent<MeshFilter>());
            DestroyImmediate(wireframeGO.GetComponent<MeshRenderer>());
        }
        else
        {
            // Create a new GameObject for the wireframe
            wireframeGO = new GameObject("Wireframe");
            wireframeGO.transform.parent = this.transform;
        }

        // Add MeshFilter and MeshRenderer
        MeshFilter mf = wireframeGO.GetComponent<MeshFilter>();
        if (mf == null)
        {
            mf = wireframeGO.AddComponent<MeshFilter>();
        }

        MeshRenderer mr = wireframeGO.GetComponent<MeshRenderer>();
        if (mr == null)
        {
            mr = wireframeGO.AddComponent<MeshRenderer>();
        }

        // Assign the line mesh
        Mesh lineMesh = new Mesh();
        lineMesh.SetVertices(lineVertices);
        // Create indices for lines: each pair of vertices is a separate line
        int[] indices = new int[lineVertices.Count];
        for (int i = 0; i < lineVertices.Count; i++)
        {
            indices[i] = i;
        }
        lineMesh.SetIndices(indices, MeshTopology.Lines, 0);
        mf.mesh = lineMesh;

        // Assign the wireframe material
        if (wireframeMaterial != null)
        {
            mr.material = wireframeMaterial;
        }
        else
        {
            Debug.LogWarning("Wireframe Material not assigned.");
        }
    }

    private Vector3 LatLong2Unity(float latitude, float longitude, float elevation)
    {
        float scale = 25; // Use the same scale as in WaveformVisualizer
        float x = longitude * scale;
        float y = elevation * (1 / scale);
        float z = latitude * scale;
        return new Vector3(x, y, z);
    }

    // Helper struct for edges
    private struct Edge
    {
        public int v1;
        public int v2;

        public Edge(int a, int b)
        {
            if (a < b)
            {
                v1 = a;
                v2 = b;
            }
            else
            {
                v1 = b;
                v2 = a;
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Edge))
                return false;
            Edge other = (Edge)obj;
            return v1 == other.v1 && v2 == other.v2;
        }

        public override int GetHashCode()
        {
            return v1.GetHashCode() ^ v2.GetHashCode();
        }
    }
}