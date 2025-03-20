using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required for UI elements
using System.Linq;
using System;

using TriangleNet.Geometry;
using TriangleNet.Topology;
using TriangleNet.Meshing;


using  GEDIGlobals; // loading global params and structs

public enum RHValue
{
    RH2,
    RH50,
    RH98
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

    [Header("RH Value Selection")]
    public RHValue selectedRHField = RHValue.RH98; // Selected RH

    private Dictionary<Vector2Int, Vector3> gridPositions = new Dictionary<Vector2Int, Vector3>();

    [Header("Filtering Options")]
    public float waveformHeightThreshold = 5f; // Minimum height in meters for waveforms
    public float waveformEnergyThreshold = 5f; // Only render parts of the waveform above this energy

    private Dictionary<Vector2Int, TerrainPoint> terrainPoints = new Dictionary<Vector2Int, TerrainPoint>();
    [Header("Terrain Visualization")]
    public Texture2D terrainTexture;

    //// handles the wireframe mode of GEDI terrain
    public Button WireframeToggleGEDITerrain;
    private GameObject terrainGEDI;
    private bool enableWireframeGEDI = false; // Whether to render terrain as wireframe
    private Mesh terrainWireframeGEDI;
    private Mesh terrainSolidGEDI;

    //// handles the wireframe mode of Tandem X terrain
    ///
    // public Button WireframeToggleTandemX;   // use when Tandem-X terrain is implemented
    // TODO
    

    [Tooltip("Geographic bounds: [West, East, South, North]")]
    public Vector4 textureGeoBounds = new Vector4(-71.5f, -71.4f, -46.5f, -46.6f); // left, right, bottom, top
                        // (lng>-71.5) & (lng<-71.4) & (lat>46.5) & (lat<46.6)
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
                // Debug.Log($"reference height {referenceElevation}");

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

        float x = lonInMeters * positionScale * Params.SCALE;
        float y = elevDiff * elevationScale * Params.SCALE;
        float z = latInMeters * positionScale * Params.SCALE;


        // EXTREME ELEVATION
        return new Vector3(x, y, z);
        // return new Vector3(x, elevation*0.75f, z);
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
        // bool hasSignificantValues = false;
        // for (int i = 0; i < dataPoint.rawWaveformValues.Length; i++)
        // {
        //     if (dataPoint.rawWaveformValues[i] >= waveformEnergyThreshold)
        //     {
        //         hasSignificantValues = true;
        //         break;
        //     }
        // }

        // if (!hasSignificantValues)
        // {
        //     Debug.Log("No portion of the waveform found above threshold. Skipping.");
        //     return;
        // }

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
        Mesh mesh = GenerateCylinderMesh(normalizedValues, dataPoint.rawWaveformLengths, dataPoint.rawWaveformPositions , height, slantDirection);
        meshFilter.mesh = mesh;

        // float bottomOffset = 0.1172f * 76.8f * Params.SCALE; // CHECK
        Vector3 bottomPosition = new Vector3(
            position.x,
            position.y,
            // position.y - bottomOffset, // adjust Y to be at true bottom
            position.z
        );

        Vector2Int gridKey = new Vector2Int(
            Mathf.RoundToInt(position.x * 1000),
            Mathf.RoundToInt(position.z * 1000)
        );
        

        terrainPoints[gridKey] = new TerrainPoint(
            bottomPosition,         // unity pos
            dataPoint.latitude,
            dataPoint.longitude,
            dataPoint.elevation
        );
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

    // private Mesh GenerateCylinderMesh(float[] waveformValues, int[] segmentLengths, float height, Vector3 slantDirection)
    // {
    //     List<Vector3> vertices = new List<Vector3>();
    //     List<int> triangles = new List<int>();
    //     List<Vector2> uvs = new List<Vector2>();
    //     List<Vector2> heights = new List<Vector2>();

    //     int circleResolution = 12;
    //     float angleIncrement = Mathf.PI * 2 / circleResolution;

    //     // calc total height
    //     float actualHeight = 76.8f;
    //     float totalHeight = actualHeight * Globals.SCALE;  // CHECK
    //     float heightPerSegment = totalHeight / waveformValues.Length;
        
    //     // vertices for each layer
    //     for (int i = 0; i <= waveformValues.Length; i++)
    //     {
    //         float normalizedHeight = i / (float)waveformValues.Length;
    //         float y = (1.0f - normalizedHeight - 0.1172f) * totalHeight;
            
    //         // calculate slant offset - the offset increases with height
    //         Vector3 slantOffset = slantDirection * y;
            
    //         // use last value for top cap for radius
    //         float radius = i < waveformValues.Length ? 
    //             waveformValues[i] : 
    //             waveformValues[waveformValues.Length - 1];

    //         // vertices for the circle
    //         for (int j = 0; j < circleResolution; j++)
    //         {
    //             float angle = j * angleIncrement;
    //             float x = radius * Mathf.Cos(angle);
    //             float z = radius * Mathf.Sin(angle);

    //             // apply slant offset
    //             vertices.Add(new Vector3(x + slantOffset.x, y, z + slantOffset.z));
    //             // uvs.Add(new Vector2(j / (float)circleResolution, normalizedHeight));  // CHECK

    //             float flippedUV = 1.0f - normalizedHeight;
    //             uvs.Add(new Vector2(j / (float)circleResolution, flippedUV));
    //             heights.Add(new Vector2(totalHeight, 0)); 
    //             // heights.Add(new Vector2(actualHeight, 0)); // CHECK
    //         }
    //     }

    //     // generate triangles
    //     for (int i = 0; i < waveformValues.Length; i++)
    //     {
    //         int baseIndex = i * circleResolution;
    //         for (int j = 0; j < circleResolution; j++)
    //         {
    //             int nextJ = (j + 1) % circleResolution;
                
    //             // 1st triangle (ccw)
    //             triangles.Add(baseIndex + j);
    //             triangles.Add(baseIndex + nextJ);
    //             triangles.Add(baseIndex + circleResolution + j);
                
    //             // 2nd triangle
    //             triangles.Add(baseIndex + nextJ);
    //             triangles.Add(baseIndex + circleResolution + nextJ);
    //             triangles.Add(baseIndex + circleResolution + j);
    //         }
    //     }

    //     // add top and bottom caps
    //     int bottomStart = 0;
    //     int topStart = vertices.Count - circleResolution;

    //     // bottom cap
    //     for (int i = 1; i < circleResolution - 1; i++)
    //     {
    //         triangles.Add(bottomStart);
    //         triangles.Add(bottomStart + i);
    //         triangles.Add(bottomStart + i + 1);
    //     }

    //     // top cap
    //     for (int i = 1; i < circleResolution - 1; i++)
    //     {
    //         triangles.Add(topStart);
    //         triangles.Add(topStart + i + 1);
    //         triangles.Add(topStart + i);
    //     }

    //     Mesh mesh = new Mesh
    //     {
    //         name = "WaveformCylinderMesh",
    //         vertices = vertices.ToArray(),
    //         triangles = triangles.ToArray(),
    //         uv = uvs.ToArray(),
    //         uv2 = heights.ToArray()
    //     };
    //     mesh.RecalculateNormals();

    //     return mesh;
    // }


    private Mesh GenerateCylinderMesh(float[] waveformValues, int[] segmentLengths, float[] physicalPositions, float height, Vector3 slantDirection)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector2> heights = new List<Vector2>(); // UV2 for height data
        
        int circleResolution = 12;
        float angleIncrement = Mathf.PI * 2 / circleResolution;
        
        // calc actual height
        float actualHeight = 76.8f;
        float totalHeight = actualHeight * Params.SCALE;
        
        // precalculate the original total number of samples
        int originalTotalSamples = 0;
        for (int i = 0; i < segmentLengths.Length; i++) {
            originalTotalSamples += segmentLengths[i];
        }
        
        // accumulated original samples
        // int accumulatedSamples = 0;
        
        // vertices for each layer
        for (int i = 0; i <= waveformValues.Length; i++)
        {
            // calc height using segment lengths
            float physicalHeightRatio;
            if (i == waveformValues.Length) {
                physicalHeightRatio = 1.0f; // top cap
            } else {
                physicalHeightRatio = physicalPositions[i];
            }
            
            // calc y pos with ground at 11.72% from bottom
            float y = (1.0f - physicalHeightRatio - 0.1172f) * totalHeight;
            
            // calc slant offset
            Vector3 slantOffset = slantDirection * y;
            
            // last value for top cap for radius
            float radius = i < waveformValues.Length ? 
                waveformValues[i] : 
                waveformValues[waveformValues.Length - 1];
            
            // create vertices for this circle
            for (int j = 0; j < circleResolution; j++)
            {
                float angle = j * angleIncrement;
                float x = radius * Mathf.Cos(angle);
                float z = radius * Mathf.Sin(angle);
                
                // slant offset
                vertices.Add(new Vector3(x + slantOffset.x, y, z + slantOffset.z));
                
                // UV mapping using physical height ratio
                float u = j / (float)circleResolution;
                float v = physicalHeightRatio; // physical height ratio for v
                uvs.Add(new Vector2(u, v));
                
                // actual height in meters (not scaled) in UV2
                heights.Add(new Vector2(actualHeight, 0));
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

        Mesh mesh = new Mesh {
            name = "WaveformCylinderMesh",
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            uv = uvs.ToArray(),
            uv2 = heights.ToArray() // Include UV2 with height data
        };
        mesh.RecalculateNormals();
    
        return mesh;
    }

    public void VisualizeData(List<CSVParser.GEDIDataPoint> dataPoints)
    {
        if (dataPoints == null || dataPoints.Count == 0)
        {
            Debug.LogWarning("No data points to visualize.");
            return;
        }

        terrainPoints.Clear();
        int validCount = 0;
        int totalCount = dataPoints.Count;

        // Debug.Log($"Processing {dataPoints.Count} total waveforms...");

        foreach (var point in dataPoints)
        {
            float selectedRH = GetSelectedRHValue(point);

            if (!IsHeightValid(selectedRH))
            {
                continue; // skip waveforms below threshold
            }

            Vector3 position = LatLong2Unity(point.latitude, point.longitude, point.elevation);
            CreateCylinder(position, point, selectedRH);
            validCount++;
        }

        // Debug.Log($"Created {validCount} waveforms out of {totalCount} total points.");
        CreateTerrainMeshDELNET(terrainPoints);
        // Debug.Log($"Reference coordinates - Lat: {referenceLatitude}, Long: {referenceLongitude}");
    }


    

    private void CreateTerrainMeshDELNET(Dictionary<Vector2Int, TerrainPoint> points)
    {
        if (points.Count < 3)
        {
            Debug.LogWarning("Not enough points for triangulation (minimum 3 required)!");
            return;
        }

        
        Polygon polygon = new Polygon();  // create a polygon for triangulation
        Dictionary<int, TerrainPoint> pointMap = new Dictionary<int, TerrainPoint>(); // dict k: vertex ID, v: terrain points
        
        // add vertices to the polygon (still using Unity positions for geometry)
        var pointList = points.Values.ToList();
        int id = 0;
        foreach (var point in pointList)
        {
            polygon.Add(new Vertex(point.position.x, point.position.z, id));
            pointMap[id] = point;
            id++;
        }
        
        // create mesh with quality constraints
        // IMesh mesh = polygon.Triangulate();
        
        // create Unity mesh
        // calculate geographic bounds for UV mapping
        float minLat = pointList.Min(p => p.latitude);
        float maxLat = pointList.Max(p => p.latitude);
        float minLon = pointList.Min(p => p.longitude);
        float maxLon = pointList.Max(p => p.longitude);
        
        Debug.Log($"Geographic bounds - Lat: {minLat} to {maxLat}, Lon: {minLon} to {maxLon}");

        // save both solid and wireframe as member variables
        terrainSolidGEDI = GEDITerrain.generateSolid(polygon, pointMap, textureGeoBounds);
        terrainWireframeGEDI = GEDITerrain.generateWireframe(polygon, pointMap);
        
        // if a texture is assigned
        if (terrainTexture != null) {
            terrainMaterial.mainTexture = terrainTexture;
            // Debug.Log($"Applied texture to terrain with geo bounds: {textureGeoBounds}");
        }


        // use solid as default
        terrainGEDI = new GameObject("DelaunayTerrainSolid");
        MeshFilter meshFilter = terrainGEDI.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = terrainGEDI.AddComponent<MeshRenderer>();
        meshFilter.mesh = terrainSolidGEDI;
        meshRenderer.material = terrainMaterial;

        
        WireframeToggleGEDITerrain.onClick.AddListener(ToggleWireframeGEDI);
    }

    
    void ToggleWireframeGEDI()
    {
        enableWireframeGEDI = !enableWireframeGEDI;
        MeshFilter meshFilter = terrainGEDI.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = terrainGEDI.GetComponent<MeshRenderer>();

        if (enableWireframeGEDI)
        {
            WireframeToggleGEDITerrain.GetComponentInChildren<Text>().text = "GEDI Terrain (Wireframe)";
            meshFilter.mesh = terrainWireframeGEDI;
            meshRenderer.material = wireframeMaterial;
        }
        else
        {
            WireframeToggleGEDITerrain.GetComponentInChildren<Text>().text = "GEDI Terrain (Solid)";
            meshFilter.mesh = terrainSolidGEDI;
            meshRenderer.material = terrainMaterial;
        }

    }
}