using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

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

        return new Vector3(x, elevation*0.01f, z);
        // return new Vector3(x, elevation, z);
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

        Vector2Int gridKey = new Vector2Int(
            Mathf.RoundToInt(position.x * 1000),
            Mathf.RoundToInt(position.z * 1000)
        );
        
        terrainPoints[gridKey] = position;
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
        float totalHeight = 76.8f * Globals.SCALE;
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
                uvs.Add(new Vector2(j / (float)circleResolution, normalizedHeight));
                heights.Add(new Vector2(totalHeight, 0));
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

        CreateTerrainMesh(terrainPoints);
        Debug.Log($"lat: {referenceLatitude}, long: {referenceLongitude}");
    }

    // private void CreateTerrainMesh(Dictionary<Vector2Int, Vector3> points)
    // {
    //     if (points.Count == 0)
    //     {
    //         Debug.LogWarning("No points to create terrain from!");
    //         return;
    //     }

    //     float minX = float.MaxValue, maxX = float.MinValue;
    //     float minZ = float.MaxValue, maxZ = float.MinValue;
    //     float minY = float.MaxValue, maxY = float.MinValue;

    //     foreach (var point in points.Values)
    //     {
    //         minX = Mathf.Min(minX, point.x);
    //         maxX = Mathf.Max(maxX, point.x);
    //         minZ = Mathf.Min(minZ, point.z);
    //         maxZ = Mathf.Max(maxZ, point.z);
    //         minY = Mathf.Min(minY, point.y);
    //         maxY = Mathf.Max(maxY, point.y);
    //     }

    //     int resolution = 200;
    //     float terrainWidth = maxX - minX;
    //     float terrainLength = maxZ - minZ;
    //     float cellSizeX = terrainWidth / (resolution - 1);
    //     float cellSizeZ = terrainLength / (resolution - 1);

    //     Vector3[] vertices = new Vector3[resolution * resolution];
    //     int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
    //     Vector2[] uvs = new Vector2[vertices.Length];

    //     float influenceRadius = Mathf.Max(terrainWidth, terrainLength) / 10f;
    //     float falloffFactor = 75f;

    //     for (int z = 0; z < resolution; z++)
    //     {
    //         for (int x = 0; x < resolution; x++)
    //         {
    //             float xPos = minX + x * cellSizeX;
    //             float zPos = minZ + z * cellSizeZ;
    //             int index = z * resolution + x;

    //             float height = CalculateRBFHeight(new Vector3(xPos, 0, zPos), points.Values.ToList(), 
    //                                         influenceRadius, falloffFactor, minY);

    //             vertices[index] = new Vector3(xPos, height, zPos);
    //             uvs[index] = new Vector2(x / (float)(resolution - 1), z / (float)(resolution - 1));
    //         }
    //     }

    //     int tris = 0;
    //     for (int z = 0; z < resolution - 1; z++)
    //     {
    //         for (int x = 0; x < resolution - 1; x++)
    //         {
    //             int vertexIndex = z * resolution + x;

    //             triangles[tris] = vertexIndex;
    //             triangles[tris + 1] = vertexIndex + resolution;
    //             triangles[tris + 2] = vertexIndex + 1;
    //             triangles[tris + 3] = vertexIndex + 1;
    //             triangles[tris + 4] = vertexIndex + resolution;
    //             triangles[tris + 5] = vertexIndex + resolution + 1;

    //             tris += 6;
    //         }
    //     }

    //     GameObject terrainObject = new GameObject("Terrain");
    //     MeshFilter meshFilter = terrainObject.AddComponent<MeshFilter>();
    //     MeshRenderer meshRenderer = terrainObject.AddComponent<MeshRenderer>();
    //     meshRenderer.material = terrainMaterial;

    //     Mesh mesh = new Mesh();
    //     mesh.vertices = vertices;
    //     mesh.triangles = triangles;
    //     mesh.uv = uvs;
    //     mesh.RecalculateNormals();

    //     meshFilter.mesh = mesh;
    // }
    
    // WIRE FRAME MODE
    private void CreateTerrainMesh(Dictionary<Vector2Int, Vector3> points)
    {
        if (points.Count == 0)
        {
            Debug.LogWarning("No points to create terrain from!");
            return;
        }

        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var point in points.Values)
        {
            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            minZ = Mathf.Min(minZ, point.z);
            maxZ = Mathf.Max(maxZ, point.z);
            minY = Mathf.Min(minY, point.y);
            maxY = Mathf.Max(maxY, point.y);
        }

        int resolution = 200;
        float terrainWidth = maxX - minX;
        float terrainLength = maxZ - minZ;
        float cellSizeX = terrainWidth / (resolution - 1);
        float cellSizeZ = terrainLength / (resolution - 1);

        Vector3[] vertices = new Vector3[resolution * resolution];
        Vector2[] uvs = new Vector2[vertices.Length];

        float influenceRadius = Mathf.Max(terrainWidth, terrainLength) / 10f;
        float falloffFactor = 75f;

        // generate vertices
        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float xPos = minX + x * cellSizeX;
                float zPos = minZ + z * cellSizeZ;
                int index = z * resolution + x;

                float height = CalculateRBFHeight(new Vector3(xPos, 0, zPos), points.Values.ToList(), 
                                            influenceRadius, falloffFactor, minY);

                vertices[index] = new Vector3(xPos, height, zPos);
                uvs[index] = new Vector2(x / (float)(resolution - 1), z / (float)(resolution - 1));
            }
        }

        // line indices for the wireframe
        List<int> lines = new List<int>();

        // horizontal lines
        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution - 1; x++)
            {
                int index = z * resolution + x;
                lines.Add(index);
                lines.Add(index + 1);
            }
        }

        // vertical lines
        for (int x = 0; x < resolution; x++)
        {
            for (int z = 0; z < resolution - 1; z++)
            {
                int index = z * resolution + x;
                lines.Add(index);
                lines.Add(index + resolution);
            }
        }

        GameObject terrainObject = new GameObject("TerrainWireframe");
        MeshFilter meshFilter = terrainObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = terrainObject.AddComponent<MeshRenderer>();
        meshRenderer.material = terrainMaterial;

        // mesh
        Mesh mesh = new Mesh();
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.SetIndices(lines.ToArray(), MeshTopology.Lines, 0);
        mesh.uv = uvs;
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;

        // render wireframes
        terrainMaterial.SetFloat("_Wireframe", 1);
        if (terrainMaterial.HasProperty("_WireframeColor"))
        {
            terrainMaterial.SetColor("_WireframeColor", Color.white);
        }
    }

    private float CalculateRBFHeight(Vector3 position, List<Vector3> points, 
                                float influenceRadius, float falloffFactor, float defaultHeight)
    {
        float totalWeight = 0f;
        float weightedHeight = 0f;
        float maxInfluenceDistance = influenceRadius * 2f;

        foreach (Vector3 point in points)
        {
            float distance = Vector2.Distance(
                new Vector2(position.x, position.z),
                new Vector2(point.x, point.z)
            );

            if (distance < maxInfluenceDistance)
            {
                float weight = Mathf.Exp(-falloffFactor * (distance * distance) / 
                                    (influenceRadius * influenceRadius));

                weightedHeight += point.y * weight;
                totalWeight += weight;
            }
        }

        return totalWeight > 0 ? weightedHeight / totalWeight : defaultHeight;
    }
}
