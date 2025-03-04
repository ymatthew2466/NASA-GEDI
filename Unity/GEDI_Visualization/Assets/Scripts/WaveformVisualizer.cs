using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

using TriangleNet.Geometry;
using TriangleNet.Topology;
using TriangleNet.Meshing;


public enum RHValue
{
    RH2,
    RH50,
    RH98
}
public static class Globals
{
    public const float SCALE = 0.02f;
}

public class WaveformVisualizer : MonoBehaviour
{
    [Header("Data and Material")]
    public CSVParser csvParser;
    public Material waveformMaterial; // Material for waveform cylinders
    public Material terrainMaterial;  // Material for ground terrain
    public Material wireframeMaterial;

    [Header("Scaling Factors")]
    public float positionScale = 0.0001f;  // Scale factor for position (Unity units per meter)
    public float elevationScale = 0.0001f; // Scale factor for elevation
    public float cylinderSum = 25.0f;  // target sum for waveform normalization

    [Header("Grid Configuration")]
    [Range(0.000001f, 10f)]
    public float gridCellSize = 1f;  // Size of each grid cell

    [Header("RH Value Selection")]
    public RHValue selectedRHField = RHValue.RH98; // Selected RH

    private Dictionary<Vector2Int, Vector3> gridPositions = new Dictionary<Vector2Int, Vector3>();

    [Header("Visualization Options")]
    public bool visualizePopulated = false; // Visualize the most populated cell

    [Header("Filtering Options")]
    public float waveformHeightThreshold = 5f; // Minimum height in meters for waveforms
    public float waveformEnergyThreshold = 5f; // Only render parts of the waveform above this energy

    private Dictionary<Vector2Int, Vector3> terrainPoints = new Dictionary<Vector2Int, Vector3>();
    [Header("Terrain Visualization")]
    public bool useWireframe = true; // Whether to render terrain as wireframe

    private float referenceLatitude;
    private float referenceLongitude;
    private float referenceElevation;

    void Start()
    {
        if (csvParser != null)
        {
            if (csvParser.getDataPoints() == null || csvParser.getDataPoints().Count == 0)
            {
                csvParser.loadCSV();
            }

            List<CSVParser.GEDIDataPoint> dataPoints = csvParser.getDataPoints();
            if (dataPoints != null && dataPoints.Count > 0)
            {
                referenceLatitude = dataPoints[0].latitude;
                referenceLongitude = dataPoints[0].longitude;
                referenceElevation = dataPoints[0].elevation;

                VisualizeData(dataPoints);
            }
            else
            {
                Debug.LogError("Data points are null or empty.");
            }
        }
        else
        {
            Debug.LogError("CSVParser is not assigned.");
        }
    }

    private float GetSelectedRHValue(CSVParser.GEDIDataPoint point)
    {
        switch (selectedRHField)
        {
            case RHValue.RH2:
                return point.rh2;
            case RHValue.RH50:
                return point.rh50;
            case RHValue.RH98:
                return point.rh98;
            default:
                return point.rh98;
        }
    }

    private Vector3 LatLong2Unity(float latitude, float longitude, float elevation)
    {
        float latDiff = latitude - referenceLatitude;
        float lonDiff = longitude - referenceLongitude;
        float elevDiff = elevation - referenceElevation;

        float latInMeters = latDiff * 111000f;
        float cosLat = Mathf.Cos(referenceLatitude * Mathf.Deg2Rad);
        float lonInMeters = lonDiff * 111000f * cosLat;

        float x = lonInMeters * positionScale * Globals.SCALE;
        float y = elevDiff * elevationScale * Globals.SCALE;
        float z = latInMeters * positionScale * Globals.SCALE;

        // return new Vector3(x, elevation*0.01f, z);
        return new Vector3(x, elevation*0.25f, z);
    }

    // New helper method to calculate direction from ground to ISS
    // private Vector3 CalculateISSDirection(CSVParser.GEDIDataPoint dataPoint)
    // {
    //     // Calculate raw direction vector in geographic coordinates
    //     float latDiff = dataPoint.instrumentLat - dataPoint.lowestLat;
    //     float lonDiff = dataPoint.instrumentLon - dataPoint.lowestLon;
    //     float elevDiff = dataPoint.instrumentAlt - (dataPoint.lowestElev + dataPoint.wgs84Elevation);

    //     // Convert to meters
    //     float latInMeters = latDiff * 111000f;
    //     float cosLat = Mathf.Cos(dataPoint.lowestLat * Mathf.Deg2Rad);
    //     float lonInMeters = lonDiff * 111000f * cosLat;

    //     // Create normalized direction vector
    //     Vector3 direction = new Vector3(lonInMeters, elevDiff, latInMeters).normalized;
        
    //     // Project direction into Unity's coordinate system orientation
    //     // Note: we're only using this for direction, not for absolute positioning
    //     return new Vector3(direction.x, direction.y * 0.01f, direction.z);
    // }

    private Vector3 CalculateISSDirection(CSVParser.GEDIDataPoint dataPoint)
    {   
        // testing purposes only
        float slantAmplification = 20f;


        // raw direction vector in geographic coordinates
        float latDiff = dataPoint.instrumentLat - dataPoint.lowestLat;
        float lonDiff = dataPoint.instrumentLon - dataPoint.lowestLon;
        float elevDiff = dataPoint.instrumentAlt - (dataPoint.lowestElev + dataPoint.wgs84Elevation);

        // conv to meters
        float latInMeters = latDiff * 111000f;
        float cosLat = Mathf.Cos(dataPoint.lowestLat * Mathf.Deg2Rad);
        float lonInMeters = lonDiff * 111000f * cosLat;
        Vector3 rawDirection = new Vector3(lonInMeters, elevDiff, latInMeters);
        
        // extract horizontal and vertical
        float horizontalMagnitude = Mathf.Sqrt(rawDirection.x * rawDirection.x + rawDirection.z * rawDirection.z);
        float verticalComponent = rawDirection.y;
        
        // exaggerate horizontal by testing slant amp for testing
        float amplifiedHorizontal = horizontalMagnitude * slantAmplification;
        float directionRatio = amplifiedHorizontal / horizontalMagnitude;
        Vector3 amplifiedDirection = new Vector3(
            rawDirection.x * directionRatio,
            verticalComponent,
            rawDirection.z * directionRatio
        ).normalized;
        
        // elevation * 0.01, but not sure if correct
        return new Vector3(amplifiedDirection.x, amplifiedDirection.y * 0.01f, amplifiedDirection.z);
    }


    private bool IsHeightValid(float height)
    {
        return height >= waveformHeightThreshold;
    }

    private void CreateCylinder(Vector3 position, CSVParser.GEDIDataPoint dataPoint, float height)
    {
        // Check if any values are above threshold
        bool hasSignificantValues = false;
        for (int i = 0; i < dataPoint.rawWaveformValues.Length; i++)
        {
            if (dataPoint.rawWaveformValues[i] >= waveformEnergyThreshold)
            {
                hasSignificantValues = true;
                break;
            }
        }

        if (!hasSignificantValues)
        {
            Debug.Log("No portion of the waveform found above threshold. Skipping.");
            return;
        }

        GameObject waveformObject = new GameObject("WaveformCylinder");
        waveformObject.transform.position = position;

        MeshFilter meshFilter = waveformObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = waveformObject.AddComponent<MeshRenderer>();
        meshRenderer.material = waveformMaterial;

        // Normalize the waveform values
        float[] normalizedValues = new float[dataPoint.rawWaveformValues.Length];
        Array.Copy(dataPoint.rawWaveformValues, normalizedValues, dataPoint.rawWaveformValues.Length);
        NormalizeWaveform(normalizedValues, cylinderSum);

        // calc direction from ground to ISS
        Vector3 slantDirection = CalculateISSDirection(dataPoint);
        
        // cylinder mesh with slant
        Mesh mesh = GenerateCylinderMesh(normalizedValues, dataPoint.rawWaveformLengths, height, slantDirection);
        meshFilter.mesh = mesh;

        float bottomOffset = 0.1172f * 76.8f * Globals.SCALE; // CHECK
        Vector3 bottomPosition = new Vector3(
            position.x,
            position.y - bottomOffset, // adjust Y to be at true bottom
            position.z
        );

        Vector2Int gridKey = new Vector2Int(
            Mathf.RoundToInt(position.x * 1000),
            Mathf.RoundToInt(position.z * 1000)
        );
        
        // terrainPoints[gridKey] = position;
        terrainPoints[gridKey] = bottomPosition; // CHECK
    }

    private void NormalizeWaveform(float[] waveformValues, float targetSum)
    {
        float sumValue = 0f;
        for (int i = 0; i < waveformValues.Length; i++)
        {
            sumValue += waveformValues[i];
        }

        if (sumValue > 0)
        {
            float scaleFactor = targetSum / sumValue;
            for (int i = 0; i < waveformValues.Length; i++)
            {
                waveformValues[i] *= scaleFactor;
            }
        }
        else
        {
            float equalValue = targetSum / waveformValues.Length;
            for (int i = 0; i < waveformValues.Length; i++)
            {
                waveformValues[i] = equalValue;
            }
        }
    }

    private Mesh GenerateCylinderMesh(float[] waveformValues, int[] segmentLengths, float height, Vector3 slantDirection)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector2> heights = new List<Vector2>();

        int circleResolution = 12;
        float angleIncrement = Mathf.PI * 2 / circleResolution;

        // calc total height
        float actualHeight = 76.8f;
        float totalHeight = actualHeight * Globals.SCALE;  // CHECK
        float heightPerSegment = totalHeight / waveformValues.Length;
        
        // vertices for each layer
        for (int i = 0; i <= waveformValues.Length; i++)
        {
            float normalizedHeight = i / (float)waveformValues.Length;
            float y = (1.0f - normalizedHeight - 0.1172f) * totalHeight;
            
            // calculate slant offset - the offset increases with height
            Vector3 slantOffset = slantDirection * y;
            
            // use last value for top cap for radius
            float radius = i < waveformValues.Length ? 
                waveformValues[i] : 
                waveformValues[waveformValues.Length - 1];

            // vertices for the circle
            for (int j = 0; j < circleResolution; j++)
            {
                float angle = j * angleIncrement;
                float x = radius * Mathf.Cos(angle);
                float z = radius * Mathf.Sin(angle);

                // apply slant offset
                vertices.Add(new Vector3(x + slantOffset.x, y, z + slantOffset.z));
                // uvs.Add(new Vector2(j / (float)circleResolution, normalizedHeight));  // CHECK

                float flippedUV = 1.0f - normalizedHeight;
                uvs.Add(new Vector2(j / (float)circleResolution, flippedUV));
                heights.Add(new Vector2(totalHeight, 0)); 
                // heights.Add(new Vector2(actualHeight, 0)); // CHECK
            }
        }

        // generate triangles
        for (int i = 0; i < waveformValues.Length; i++)
        {
            int baseIndex = i * circleResolution;
            for (int j = 0; j < circleResolution; j++)
            {
                int nextJ = (j + 1) % circleResolution;
                
                // 1st triangle (ccw)
                triangles.Add(baseIndex + j);
                triangles.Add(baseIndex + nextJ);
                triangles.Add(baseIndex + circleResolution + j);
                
                // 2nd triangle
                triangles.Add(baseIndex + nextJ);
                triangles.Add(baseIndex + circleResolution + nextJ);
                triangles.Add(baseIndex + circleResolution + j);
            }
        }

        // add top and bottom caps
        int bottomStart = 0;
        int topStart = vertices.Count - circleResolution;

        // bottom cap
        for (int i = 1; i < circleResolution - 1; i++)
        {
            triangles.Add(bottomStart);
            triangles.Add(bottomStart + i);
            triangles.Add(bottomStart + i + 1);
        }

        // top cap
        for (int i = 1; i < circleResolution - 1; i++)
        {
            triangles.Add(topStart);
            triangles.Add(topStart + i + 1);
            triangles.Add(topStart + i);
        }

        Mesh mesh = new Mesh
        {
            name = "WaveformCylinderMesh",
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            uv = uvs.ToArray(),
            uv2 = heights.ToArray()
        };
        mesh.RecalculateNormals();

        return mesh;
    }

    public void VisualizeData(List<CSVParser.GEDIDataPoint> dataPoints)
    {
        gridPositions.Clear();

        Dictionary<Vector2Int, List<CSVParser.GEDIDataPoint>> grid = new Dictionary<Vector2Int, List<CSVParser.GEDIDataPoint>>();

        foreach (var point in dataPoints)
        {
            int xIndex = Mathf.FloorToInt((point.longitude - referenceLongitude) / gridCellSize);
            int yIndex = Mathf.FloorToInt((point.latitude - referenceLatitude) / gridCellSize);
            Vector2Int gridIndex = new Vector2Int(xIndex, yIndex);

            if (!grid.ContainsKey(gridIndex))
            {
                grid[gridIndex] = new List<CSVParser.GEDIDataPoint>();
            }
            grid[gridIndex].Add(point);
        }
        
        Debug.Log($"Total Grid Cells: {grid.Count}");

        if (visualizePopulated)
        {
            if (grid.Count == 0)
            {
                Debug.LogWarning("No grid cells to process.");
                return;
            }

            var mostPopulatedCell = grid.OrderByDescending(cell => cell.Value.Count).First();
            Vector2Int targetGridIndex = mostPopulatedCell.Key;
            List<CSVParser.GEDIDataPoint> targetCellPoints = mostPopulatedCell.Value;
            string lats = "";

            Debug.Log($"Most Populated Grid Cell: {targetGridIndex} with {targetCellPoints.Count} points.");

            foreach (var point in targetCellPoints)
            {
                float selectedRH = GetSelectedRHValue(point);

                if (!IsHeightValid(selectedRH))
                {
                    Debug.Log($"Skipping waveform at ({point.latitude}, {point.longitude}) with height {selectedRH}m below threshold.");
                    continue;
                }
                Vector3 position = LatLong2Unity(point.latitude, point.longitude, point.elevation);
                lats = lats + ", " + point.latitude;

                CreateCylinder(position, point, selectedRH);
            }
            lats = lats.Substring(2);
        }
        else
        {
            foreach (var cell in grid)
            {
                Vector2Int gridIndex = cell.Key;
                List<CSVParser.GEDIDataPoint> cellPoints = cell.Value;

                if (cellPoints == null || cellPoints.Count == 0)
                    continue;

                CSVParser.GEDIDataPoint selectedPoint = cellPoints[UnityEngine.Random.Range(0, cellPoints.Count)];
                float selectedRH = GetSelectedRHValue(selectedPoint);

                if (!IsHeightValid(selectedRH))
                {
                    Debug.Log($"Skipping waveform at ({selectedPoint.latitude}, {selectedPoint.longitude}) with height {selectedRH}m below threshold.");
                    continue;
                }

                Vector3 position = LatLong2Unity(selectedPoint.latitude, selectedPoint.longitude, selectedPoint.elevation);
                CreateCylinder(position, selectedPoint, selectedRH);
            }
        }
        CreateTerrainMeshDELNET(terrainPoints);
        // CreateTerrainMesh(terrainPoints);
        // CreateTerrainMeshDEL(terrainPoints);
        Debug.Log($"lat: {referenceLatitude}, long: {referenceLongitude}");
    }


    private void CreateTerrainMeshDELNET(Dictionary<Vector2Int, Vector3> points)
    {
        if (points.Count < 3)
        {
            Debug.LogWarning("Not enough points for triangulation (minimum 3 required)!");
            return;
        }

        // Create a polygon for triangulation
        Polygon polygon = new Polygon();
        var pointList = points.Values.ToList();
        
        // Dictionary to map vertex IDs to original points
        Dictionary<int, Vector3> pointMap = new Dictionary<int, Vector3>();
        
        // Add vertices to the polygon
        int id = 0;
        foreach (var point in pointList)
        {
            polygon.Add(new Vertex(point.x, point.z, id));
            pointMap[id] = point;
            id++;
        }
        
        // Create mesh with quality constraints
        var mesh = polygon.Triangulate();
        
        // Create Unity mesh
        GameObject terrainObject = new GameObject("DelaunayTerrain");
        MeshFilter meshFilter = terrainObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = terrainObject.AddComponent<MeshRenderer>();
        
        // Find bounds for UV mapping
        float minX = pointList.Min(p => p.x);
        float maxX = pointList.Max(p => p.x);
        float minZ = pointList.Min(p => p.z);
        float maxZ = pointList.Max(p => p.z);
        float width = maxX - minX;
        float length = maxZ - minZ;
        
        Mesh unityMesh;
        
        if (useWireframe)
        {
            // Create wireframe material
            meshRenderer.material = wireframeMaterial;
            
            // Build wireframe mesh
            List<Vector3> wireframeVertices = new List<Vector3>();
            List<int> lineIndices = new List<int>();
            HashSet<string> processedEdges = new HashSet<string>();
            
            foreach (var triangle in mesh.Triangles)
            {
                for (int i = 0; i < 3; i++)
                {
                    int v1ID = triangle.GetVertexID(i);
                    int v2ID = triangle.GetVertexID((i + 1) % 3);
                    
                    string edgeKey1 = $"{v1ID}-{v2ID}";
                    string edgeKey2 = $"{v2ID}-{v1ID}";
                    
                    if (!processedEdges.Contains(edgeKey1) && !processedEdges.Contains(edgeKey2))
                    {
                        Vector3 p1 = pointMap[v1ID];
                        Vector3 p2 = pointMap[v2ID];
                        
                        wireframeVertices.Add(p1);
                        wireframeVertices.Add(p2);
                        
                        lineIndices.Add(wireframeVertices.Count - 2);
                        lineIndices.Add(wireframeVertices.Count - 1);
                        
                        processedEdges.Add(edgeKey1);
                    }
                }
            }
            
            unityMesh = new Mesh();
            unityMesh.vertices = wireframeVertices.ToArray();
            unityMesh.SetIndices(lineIndices.ToArray(), MeshTopology.Lines, 0);
            unityMesh.RecalculateBounds();
        }
        else
        {
            // Build solid mesh
            meshRenderer.material = terrainMaterial;
            
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();
            
            // Create vertex to index mapping
            Dictionary<int, int> vertexIndexMap = new Dictionary<int, int>();
            
            // Process all triangles
            foreach (var triangle in mesh.Triangles)
            {
                // Collect the three vertex indices for this triangle
                int[] vertIndices = new int[3];
                
                for (int i = 0; i < 3; i++)
                {
                    int vertexID = triangle.GetVertexID(i);
                    
                    // If we haven't processed this vertex yet
                    if (!vertexIndexMap.ContainsKey(vertexID))
                    {
                        Vector3 originalPoint = pointMap[vertexID];
                        vertices.Add(originalPoint);
                        
                        // Create UV based on position
                        float u = (originalPoint.x - minX) / width;
                        float v = (originalPoint.z - minZ) / length;
                        uvs.Add(new Vector2(u, v));
                        
                        vertexIndexMap[vertexID] = vertices.Count - 1;
                    }
                    
                    // Store this vertex index
                    vertIndices[i] = vertexIndexMap[vertexID];
                }
                
                // Add indices in REVERSE order to fix orientation
                triangles.Add(vertIndices[0]);
                triangles.Add(vertIndices[2]);
                triangles.Add(vertIndices[1]);
            }
            
            unityMesh = new Mesh();
            unityMesh.vertices = vertices.ToArray();
            unityMesh.triangles = triangles.ToArray();
            unityMesh.uv = uvs.ToArray();
            unityMesh.RecalculateNormals();
        }
        
        meshFilter.mesh = unityMesh;
    }


    private void CreateTerrainMeshDEL(Dictionary<Vector2Int, Vector3> points)
    {
        if (points.Count < 3)
        {
            Debug.LogWarning("Not enough points for triangulation (minimum 3 required)!");
            return;
        }

        // Step 1: Extract points and create map for lookup
        List<Vector3> pointsList = points.Values.ToList();
        List<Vector2> points2D = pointsList.Select(p => new Vector2(p.x, p.z)).ToList();
        Dictionary<Vector2, Vector3> pointMap = new Dictionary<Vector2, Vector3>();

        for (int i = 0; i < pointsList.Count; i++)
        {
            pointMap[points2D[i]] = pointsList[i];
        }

        // Step 2: Perform Delaunay triangulation
        List<Triangle> triangles = BowyerWatson(points2D);

        // Step 3: Create mesh based on wireframe setting
        GameObject terrainObject = new GameObject("DelaunayTerrain");
        MeshFilter meshFilter = terrainObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = terrainObject.AddComponent<MeshRenderer>();

        if (useWireframe)
        {
            // Use wireframe material
            if (wireframeMaterial != null)
            {
                meshRenderer.material = wireframeMaterial;
            }
            else
            {
                Debug.LogWarning("Wireframe material is not assigned! Using default terrain material.");
                meshRenderer.material = terrainMaterial;
            }

            // Create wireframe mesh
            List<Vector3> wireframeVertices = new List<Vector3>();
            List<int> lineIndices = new List<int>();
            HashSet<string> processedEdges = new HashSet<string>();

            foreach (var triangle in triangles)
            {
                // Skip super-triangle vertices
                if (IsSuperTriangleVertex(triangle.vertices[0], points2D) ||
                    IsSuperTriangleVertex(triangle.vertices[1], points2D) ||
                    IsSuperTriangleVertex(triangle.vertices[2], points2D))
                    continue;

                for (int i = 0; i < 3; i++)
                {
                    Vector2 p1 = triangle.vertices[i];
                    Vector2 p2 = triangle.vertices[(i + 1) % 3];

                    // Get 3D points
                    Vector3 v1 = pointMap[p1];
                    Vector3 v2 = pointMap[p2];

                    // Create edge key
                    string edgeKey1 = $"{p1.x},{p1.y}-{p2.x},{p2.y}";
                    string edgeKey2 = $"{p2.x},{p2.y}-{p1.x},{p1.y}";

                    // Add edge if not already processed
                    if (!processedEdges.Contains(edgeKey1) && !processedEdges.Contains(edgeKey2))
                    {
                        wireframeVertices.Add(v1);
                        wireframeVertices.Add(v2);

                        lineIndices.Add(wireframeVertices.Count - 2);
                        lineIndices.Add(wireframeVertices.Count - 1);

                        processedEdges.Add(edgeKey1);
                        processedEdges.Add(edgeKey2);
                    }
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = wireframeVertices.ToArray();
            mesh.SetIndices(lineIndices.ToArray(), MeshTopology.Lines, 0);
            mesh.RecalculateBounds();

            meshFilter.mesh = mesh;
        }
        else
        {
            // Use solid material
            meshRenderer.material = terrainMaterial;

            // Create solid mesh
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangleIndices = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            // Find bounds for UV mapping
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            foreach (var point in pointsList)
            {
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minZ = Mathf.Min(minZ, point.z);
                maxZ = Mathf.Max(maxZ, point.z);
            }

            float width = maxX - minX;
            float length = maxZ - minZ;

            // Process each triangle and add to mesh
            foreach (var triangle in triangles)
            {
                // Skip super-triangle vertices
                if (IsSuperTriangleVertex(triangle.vertices[0], points2D) ||
                    IsSuperTriangleVertex(triangle.vertices[1], points2D) ||
                    IsSuperTriangleVertex(triangle.vertices[2], points2D))
                    continue;

                for (int i = 0; i < 3; i++)
                {
                    Vector2 point2D = triangle.vertices[i];
                    Vector3 point3D = pointMap[point2D];

                    vertices.Add(point3D);

                    // Calculate UV
                    float u = (point3D.x - minX) / width;
                    float v = (point3D.z - minZ) / length;
                    uvs.Add(new Vector2(u, v));

                    triangleIndices.Add(vertices.Count - 1);
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangleIndices.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;
        }
    }


    private bool IsSuperTriangleVertex(Vector2 vertex, List<Vector2> originalPoints)
    {
        // Check if this vertex was part of the super-triangle (not an original point)
        return !originalPoints.Contains(vertex);
    }

    // A class to represent a triangle in the triangulation
    private class Triangle
    {
        public List<Vector2> vertices = new List<Vector2>(3);
        public List<Edge> edges = new List<Edge>(3);

        public Triangle(Vector2 a, Vector2 b, Vector2 c)
        {
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);

            edges.Add(new Edge(a, b));
            edges.Add(new Edge(b, c));
            edges.Add(new Edge(c, a));
        }

        public bool ContainsPoint(Vector2 point)
        {
            // Check if the point is inside this triangle's circumcircle
            Vector2 a = vertices[0];
            Vector2 b = vertices[1];
            Vector2 c = vertices[2];

            float ab = a.x * a.x + a.y * a.y;
            float cd = b.x * b.x + b.y * b.y;
            float ef = c.x * c.x + c.y * c.y;

            float circum_x = (ab * (c.y - b.y) + cd * (a.y - c.y) + ef * (b.y - a.y)) / 
                            (a.x * (c.y - b.y) + b.x * (a.y - c.y) + c.x * (b.y - a.y)) / 2;
            float circum_y = (ab * (c.x - b.x) + cd * (a.x - c.x) + ef * (b.x - a.x)) / 
                            (a.y * (c.x - b.x) + b.y * (a.x - c.x) + c.y * (b.x - a.x)) / 2;

            Vector2 circum = new Vector2(circum_x, circum_y);
            float circum_radius = Vector2.Distance(a, circum);
            float dist = Vector2.Distance(point, circum);

            return dist <= circum_radius;
        }
    }

    private class Edge
    {
        public Vector2 a, b;

        public Edge(Vector2 a, Vector2 b)
        {
            this.a = a;
            this.b = b;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            Edge other = (Edge)obj;
            return (a == other.a && b == other.b) || (a == other.b && b == other.a);
        }

        public override int GetHashCode()
        {
            return a.GetHashCode() ^ b.GetHashCode();
        }
    }

    private List<Triangle> BowyerWatson(List<Vector2> points)
    {
        // Create a super-triangle that contains all points
        float minX = points.Min(p => p.x);
        float minY = points.Min(p => p.y);
        float maxX = points.Max(p => p.x);
        float maxY = points.Max(p => p.y);

        float dx = (maxX - minX) * 10;
        float dy = (maxY - minY) * 10;

        Vector2 v1 = new Vector2(minX - dx, minY - dy * 3);
        Vector2 v2 = new Vector2(minX - dx, maxY + dy * 3);
        Vector2 v3 = new Vector2(maxX + dx * 3, minY - dy);

        List<Triangle> triangulation = new List<Triangle>();
        triangulation.Add(new Triangle(v1, v2, v3));

        // Add points one at a time to the triangulation
        foreach (var point in points)
        {
            List<Triangle> badTriangles = new List<Triangle>();

            // Find all triangles where the point is inside the circumcircle
            foreach (var triangle in triangulation)
            {
                if (triangle.ContainsPoint(point))
                {
                    badTriangles.Add(triangle);
                }
            }

            // Find the boundary of the polygonal hole
            List<Edge> polygon = new List<Edge>();
            
            foreach (var triangle in badTriangles)
            {
                foreach (var edge in triangle.edges)
                {
                    bool isShared = false;
                    
                    foreach (var otherTriangle in badTriangles)
                    {
                        if (triangle == otherTriangle)
                            continue;
                            
                        foreach (var otherEdge in otherTriangle.edges)
                        {
                            if (edge.Equals(otherEdge))
                            {
                                isShared = true;
                                break;
                            }
                        }
                        
                        if (isShared)
                            break;
                    }
                    
                    if (!isShared)
                        polygon.Add(edge);
                }
            }

            // Remove the bad triangles
            foreach (var triangle in badTriangles)
            {
                triangulation.Remove(triangle);
            }

            // Re-triangulate the hole
            foreach (var edge in polygon)
            {
                Triangle newTriangle = new Triangle(edge.a, edge.b, point);
                triangulation.Add(newTriangle);
            }
        }

        // Remove triangles that contain vertices of the super-triangle
        for (int i = triangulation.Count - 1; i >= 0; i--)
        {
            Triangle triangle = triangulation[i];
            if (triangle.vertices.Contains(v1) || triangle.vertices.Contains(v2) || triangle.vertices.Contains(v3))
            {
                triangulation.RemoveAt(i);
            }
        }

        return triangulation;
    }

}