using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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
                csvParser.loadCSV();  // Load CSV if data is null
            }

            List<CSVParser.GEDIDataPoint> dataPoints = csvParser.getDataPoints();
            if (dataPoints != null && dataPoints.Count > 0)
            {
                // init reference point
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
        // delta from reference point
        float latDiff = latitude - referenceLatitude;
        float lonDiff = longitude - referenceLongitude;
        float elevDiff = elevation - referenceElevation;

        // convert lat delta into meters
        float latInMeters = latDiff * 111000f; // approx meters per degree latitude
        // convert long delta to meters, and account for Earth curvature
        float cosLat = Mathf.Cos(referenceLatitude * Mathf.Deg2Rad);
        float lonInMeters = lonDiff * 111000f * cosLat;

        float x = lonInMeters * positionScale * Globals.SCALE;
        float y = elevDiff * elevationScale * Globals.SCALE;
        float z = latInMeters * positionScale * Globals.SCALE;

        // return new Vector3(x, y, z);
        return new Vector3(x, elevation*0.01f, z);
    }

    private bool IsHeightValid(float height)
    {
        return height >= waveformHeightThreshold;
    }

    public void VisualizeData(List<CSVParser.GEDIDataPoint> dataPoints)
    {
        gridPositions.Clear();

        // dict to hold grid cells
        Dictionary<Vector2Int, List<CSVParser.GEDIDataPoint>> grid = new Dictionary<Vector2Int, List<CSVParser.GEDIDataPoint>>();

        foreach (var point in dataPoints)
        {
            // calculate grid indices
            int xIndex = Mathf.FloorToInt((point.longitude - referenceLongitude) / gridCellSize);
            int yIndex = Mathf.FloorToInt((point.latitude - referenceLatitude) / gridCellSize);
            Vector2Int gridIndex = new Vector2Int(xIndex, yIndex);

            if (!grid.ContainsKey(gridIndex))
            {
                grid[gridIndex] = new List<CSVParser.GEDIDataPoint>();
            }
            grid[gridIndex].Add(point);

            // // Convert latitude to string for substring comparison
            // string targetLatitude = "1.6798";
            // if (!point.latitude.ToString().Contains(targetLatitude))
            // {
            //     continue;
            // }
            // Vector3 position = LatLong2Unity(point.latitude, point.longitude, point.elevation);
            // float selectedRH = GetSelectedRHValue(point);
            // CreateCylinder(position, point.rawWaveform, selectedRH);
        }
        
        Debug.Log($"Total Grid Cells: {grid.Count}");

        if (visualizePopulated)
        {
            if (grid.Count == 0)
            {
                Debug.LogWarning("No grid cells to process.");
                return;
            }

            // most populated grid cell
            var mostPopulatedCell = grid.OrderByDescending(cell => cell.Value.Count).First();
            Vector2Int targetGridIndex = mostPopulatedCell.Key;
            List<CSVParser.GEDIDataPoint> targetCellPoints = mostPopulatedCell.Value;
            string lats = "";

            Debug.Log($"Most Populated Grid Cell: {targetGridIndex} with {targetCellPoints.Count} points.");

            // all waveforms in the most populated cell
            foreach (var point in targetCellPoints)
            {
                float selectedRH = GetSelectedRHValue(point);

                // validate height before visualization
                if (!IsHeightValid(selectedRH))
                {
                    Debug.Log($"Skipping waveform at ({point.latitude}, {point.longitude}) with height {selectedRH}m below threshold.");
                    continue;
                }
                Vector3 position = LatLong2Unity(point.latitude, point.longitude, point.elevation);
                lats = lats + ", " + point.latitude;

                CreateCylinder(position, point.rawWaveform, selectedRH);
                // Debug.Log($"Waveform at ({point.latitude}, {point.longitude})");

            }
            lats = lats.Substring(2);
            // Debug.Log(lats);
        }
        else
        {
            // visualize one random waveform per grid cell
            foreach (var cell in grid)
            {
                Vector2Int gridIndex = cell.Key;
                List<CSVParser.GEDIDataPoint> cellPoints = cell.Value;

                if (cellPoints == null || cellPoints.Count == 0)
                    continue;

                // one random waveform from the cell
                CSVParser.GEDIDataPoint selectedPoint = cellPoints[Random.Range(0, cellPoints.Count)];

                float selectedRH = GetSelectedRHValue(selectedPoint);

                // skip waveforms that don't meet the threshold
                if (!IsHeightValid(selectedRH))
                {
                    Debug.Log($"Skipping waveform at ({selectedPoint.latitude}, {selectedPoint.longitude}) with height {selectedRH}m below threshold.");
                    continue; 
                }

                Vector3 position = LatLong2Unity(selectedPoint.latitude, selectedPoint.longitude, selectedPoint.elevation);

                CreateCylinder(position, selectedPoint.rawWaveform, selectedRH);
            }
        }
        CreateTerrainMesh(terrainPoints);
        Debug.Log($"lat: {referenceLatitude}, long: {referenceLongitude}");
    }

    // private void CreateCylinder(Vector3 position, string rawWaveform, float height)
    // {
    //     // parse waveform data
    //     float[] waveformValues = ParseWaveform(rawWaveform);

    //     GameObject waveformObject = new GameObject("WaveformCylinder");
    //     waveformObject.transform.position = position;

    //     MeshFilter meshFilter = waveformObject.AddComponent<MeshFilter>();
    //     MeshRenderer meshRenderer = waveformObject.AddComponent<MeshRenderer>();
    //     meshRenderer.material = waveformMaterial;

    //     Mesh mesh = GenerateCylinderMesh(waveformValues, height);
    //     meshFilter.mesh = mesh;
    // }

    private void CreateCylinder(Vector3 position, string rawWaveform, float height)
    {
        float[] waveformValues = ParseRawWaveform(rawWaveform);

        // first index where amplitude is above the threshold
        int startIndex = 0;
        bool foundSignificantValue = false;
        for (int i = 0; i < waveformValues.Length; i++)
        {
            if (waveformValues[i] >= waveformEnergyThreshold)
            {
                startIndex = i;
                foundSignificantValue = true;
                break;
            }
        }

        if (!foundSignificantValue)
        {
            Debug.Log("No portion of the waveform found above threshold. Skipping.");
            return;
        }

        // truncate the waveform to start from the significant portion
        float[] truncatedWaveform = new float[waveformValues.Length - startIndex];
        System.Array.Copy(waveformValues, startIndex, truncatedWaveform, 0, truncatedWaveform.Length);

        // normalize the truncated waveform
        NormalizeWaveform(truncatedWaveform, cylinderSum);

        // truncated waveform is too short, skip
        if (truncatedWaveform.Length < 2)
        {
            Debug.Log("Truncated waveform is too short. Skipping.");
            return;
        }

        GameObject waveformObject = new GameObject("WaveformCylinder");
        waveformObject.transform.position = position;

        MeshFilter meshFilter = waveformObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = waveformObject.AddComponent<MeshRenderer>();
        meshRenderer.material = waveformMaterial;

        Mesh mesh = GenerateCylinderMesh(truncatedWaveform, height);
        meshFilter.mesh = mesh;

        // Add point to terrain points using more precise grid key
        Vector2Int gridKey = new Vector2Int(
            Mathf.RoundToInt(position.x * 1000),
            Mathf.RoundToInt(position.z * 1000)
        );
        
        // Store the ground position (not the top of the waveform)
        terrainPoints[gridKey] = position;
    }

    private float[] ParseRawWaveform(string waveform)
    {
        string[] values = waveform.Split(',');
        float[] waveformValues = new float[values.Length];

        for (int i = 0; i < values.Length; i++)
        {
            if (!float.TryParse(values[i], out waveformValues[i]))
            {
                Debug.LogWarning($"Unable to parse value: {values[i]}");
                waveformValues[i] = 0f;
            }
        }

        return waveformValues;
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
            // If sum is zero, distribute evenly
            float equalValue = targetSum / waveformValues.Length;
            for (int i = 0; i < waveformValues.Length; i++)
            {
                waveformValues[i] = equalValue;
            }
        }
    }


    private float[] ParseWaveform(string waveform)
    {
        string[] values = waveform.Split(',');
        float[] waveformValues = new float[values.Length];
        float sumValue = 0f;

        // sum of waveform
        for (int i = 0; i < values.Length; i++)
        {
            if (float.TryParse(values[i], out float parsedValue))
            {
                waveformValues[i] = parsedValue;
                sumValue += waveformValues[i];
            }
            else
            {
                Debug.LogWarning($"Unable to parse value: {values[i]}");
                waveformValues[i] = 0f;
            }
        }

        // normalize
        if (sumValue > 0)
        {
            float scaleFactor = cylinderSum / sumValue;
            for (int i = 0; i < waveformValues.Length; i++)
            {
                waveformValues[i] *= scaleFactor;
            }
        }
        else
        {
            // sum is zero, set all values to an equal fraction
            float equalValue = cylinderSum / waveformValues.Length;
            for (int i = 0; i < waveformValues.Length; i++)
            {
                waveformValues[i] = equalValue;
            }
        }

        return waveformValues;
    }
    private Mesh GenerateCylinderMesh(float[] waveformValues, float height)
    {
        int segments = waveformValues.Length;  // Number of layers
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector2> heights = new List<Vector2>();

        int circleResolution = 12;  // Number of points in the cross section
        float angleIncrement = Mathf.PI * 2 / circleResolution;


        // Generate vertices and UVs for each cross section
        for (int i = 0; i < segments; i++)
        {
            float radius = waveformValues[i];
            // Invert the vertical order:
            float y = ((segments - 1 - i) / (float)(segments - 1) - 0.1172f) * 76.8f * Globals.SCALE;
            
            // float normalizedHeight = (segments > 1) ? i / (float)(segments - 1) : 0f;
            float normalizedHeight = (segments > 1) ? (segments - 1 - i) / (float)(segments - 1) : 0f;

            for (int j = 0; j < circleResolution; j++)
            {
                float angle = j * angleIncrement;
                float x = radius * Mathf.Cos(angle);
                float z = radius * Mathf.Sin(angle);

                vertices.Add(new Vector3(x, y, z));
                uvs.Add(new Vector2(0, normalizedHeight));
                heights.Add(new Vector2(76.8f, 0));
            }
        }

        // Generate triangles between consecutive cross sections
        for (int i = 0; i < segments - 1; i++)
        {
            int startIndex = i * circleResolution;
            int nextIndex = (i + 1) * circleResolution;

            for (int j = 0; j < circleResolution; j++)
            {
                int current = startIndex + j;
                int next = startIndex + (j + 1) % circleResolution;
                int top = nextIndex + j;
                int topNext = nextIndex + (j + 1) % circleResolution;

                triangles.Add(current);
                triangles.Add(next);
                triangles.Add(top);

                triangles.Add(next);
                triangles.Add(topNext);
                triangles.Add(top);
            }
        }

        // Create mesh
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

    private void CreateTerrainMesh(Dictionary<Vector2Int, Vector3> points)
    {
        if (points.Count == 0)
        {
            Debug.LogWarning("No points to create terrain from!");
            return;
        }

        // find bounds
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

        // resolution of terrain
        int resolution = 200;
        float terrainWidth = maxX - minX;
        float terrainLength = maxZ - minZ;
        float cellSizeX = terrainWidth / (resolution - 1);
        float cellSizeZ = terrainLength / (resolution - 1);

        // mesh data arrays
        Vector3[] vertices = new Vector3[resolution * resolution];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
        Vector2[] uvs = new Vector2[vertices.Length];

        // RBF interpolation
        float influenceRadius = Mathf.Max(terrainWidth, terrainLength) / 10f; // control smoothness
        float falloffFactor = 75f; // controls how quickly the influence falls off with distance

        // generate vertices
        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float xPos = minX + x * cellSizeX;
                float zPos = minZ + z * cellSizeZ;
                int index = z * resolution + x;

                // calc height using RBF interpolation
                float height = CalculateRBFHeight(new Vector3(xPos, 0, zPos), points.Values.ToList(), 
                                            influenceRadius, falloffFactor, minY);

                vertices[index] = new Vector3(xPos, height, zPos);
                uvs[index] = new Vector2(x / (float)(resolution - 1), z / (float)(resolution - 1));
            }
        }

        // generate triangles
        int tris = 0;
        for (int z = 0; z < resolution - 1; z++)
        {
            for (int x = 0; x < resolution - 1; x++)
            {
                int vertexIndex = z * resolution + x;

                triangles[tris] = vertexIndex;
                triangles[tris + 1] = vertexIndex + resolution;
                triangles[tris + 2] = vertexIndex + 1;
                triangles[tris + 3] = vertexIndex + 1;
                triangles[tris + 4] = vertexIndex + resolution;
                triangles[tris + 5] = vertexIndex + resolution + 1;

                tris += 6;
            }
        }

        // create mesh
        GameObject terrainObject = new GameObject("Terrain");
        MeshFilter meshFilter = terrainObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = terrainObject.AddComponent<MeshRenderer>();
        meshRenderer.material = terrainMaterial;

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
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
                // calculate weight using fall off function
                float weight = Mathf.Exp(-falloffFactor * (distance * distance) / 
                                    (influenceRadius * influenceRadius));

                weightedHeight += point.y * weight;
                totalWeight += weight;
            }
        }

        return totalWeight > 0 ? weightedHeight / totalWeight : defaultHeight;
    }
}