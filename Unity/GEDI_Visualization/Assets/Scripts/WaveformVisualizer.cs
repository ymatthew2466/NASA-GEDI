using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required for UI elements
using System.Linq;
using System;

using TriangleNet.Geometry;
using TriangleNet.Topology;
using TriangleNet.Meshing;
using System.Collections;

using  GEDIGlobals; // loading global params and structs



public class WaveformVisualizer : MonoBehaviour
{
    [Header("Data and Material")]
    public CSVParser csvParser;
    public Material waveformMaterial; // Material for waveform cylinders
    public Material terrainMaterial;  // Material for ground terrain
    public Material wireframeMaterial;

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

    private int gediTerrainDisplayState = 0; // 0 = Solid, 1 = Wireframe, 2 = Off
    private const int GEDI_STATE_SOLID = 0;
    private const int GEDI_STATE_WIREFRAME = 1;
    private const int GEDI_STATE_OFF = 2;
    private const int GEDI_NUM_STATES = 3; // Total number of states


    private bool enableWireframeGEDI = false; // Whether to render terrain as wireframe
    private Mesh terrainWireframeGEDI;
    private Mesh terrainSolidGEDI;

    //// handles the wireframe mode of Tandem X terrain
    ///
    // public Button WireframeToggleTandemX;   // use when Tandem-X terrain is implemented
    // TODO
    

    [Tooltip("Geographic bounds: [West, East, South, North]")]
    public Vector4 geoBounds = new Vector4(-71.5f, -71.4f, -46.5f, -46.6f); // left, right, bottom, top
    public Vector4 textureGeoBounds = new Vector4(-71.5f, -71.4f, -46.5f, -46.6f); // left, right, bottom, top
                        // (lng>-71.5) & (lng<-71.4) & (lat>46.5) & (lat<46.6)
    private float referenceLatitude;
    private float referenceLongitude;
    private float referenceElevation;

    
    

    IEnumerator Start()
    {
        referenceLongitude = (geoBounds.x + geoBounds.y)/2f;
        referenceLatitude = (geoBounds.z + geoBounds.w)/2f;
        referenceElevation = 50f;  // placeholder

        if (csvParser != null)
        {
            if (csvParser.getDataPoints() == null || csvParser.getDataPoints().Count == 0)
            {
                yield return StartCoroutine(csvParser.loadCSV());
            }

            List<CSVParser.GEDIDataPoint> dataPoints = csvParser.getDataPoints();
            if (dataPoints != null && dataPoints.Count > 0)
            {
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

    private Vector3 LatLong2Unity(float latitude, float longitude, float elevation)
    {
        float latDiff = latitude - referenceLatitude;
        float lonDiff = longitude - referenceLongitude;
        float elevDiff = elevation - referenceElevation;

        float latInMeters = latDiff * 111000f;
        float cosLat = Mathf.Cos(referenceLatitude * Mathf.Deg2Rad);
        float lonInMeters = lonDiff * 111000f * cosLat;

        float x = lonInMeters * Params.SCALE;
        float y = elevDiff * Params.TerrainScale;
        float z = latInMeters * Params.SCALE;

        // EXTREME ELEVATION
        return new Vector3(x, y, z);
        // return new Vector3(x, elevation*0.75f, z);
    }


    private void CreateCylinder(Vector3 position, CSVParser.GEDIDataPoint dataPoint)
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
        // WaveformTools.NormalizeWaveform(normalizedValues, cylinderSum);

        // calc direction from ground to ISS
        Vector3 slantDirection = WaveformTools.CalculateISSDirection(dataPoint);
        
        // cylinder mesh with slant
        Mesh mesh = WaveformTools.GenerateCylinderMesh(normalizedValues, dataPoint.rawWaveformLengths, dataPoint.rawWaveformPositions, slantDirection);
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
            Vector3 position = LatLong2Unity(point.latitude, point.longitude, point.elevation);
            CreateCylinder(position, point);
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
        // float minLat = pointList.Min(p => p.latitude);
        // float maxLat = pointList.Max(p => p.latitude);
        // float minLon = pointList.Min(p => p.longitude);
        // float maxLon = pointList.Max(p => p.longitude);
        
        // Debug.Log($"Geographic bounds - Lat: {minLat} to {maxLat}, Lon: {minLon} to {maxLon}");

        // save both solid and wireframe as member variables
        terrainSolidGEDI = GEDITerrainCreator.generateSolid(polygon, pointMap, textureGeoBounds);
        terrainWireframeGEDI = GEDITerrainCreator.generateWireframe(polygon, pointMap);
        
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
        // Cycle through states: 0 -> 1 -> 2 -> 0
        gediTerrainDisplayState = (gediTerrainDisplayState + 1) % GEDI_NUM_STATES;

        if (terrainGEDI == null) return; 

        MeshFilter meshFilter = terrainGEDI.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = terrainGEDI.GetComponent<MeshRenderer>();
        Text buttonText = WireframeToggleGEDITerrain.GetComponentInChildren<Text>();

        if (meshFilter == null || meshRenderer == null || buttonText == null)
        {
            Debug.LogError("Missing components on terrainGEDI or Button Text.");
            return;
        }

        switch (gediTerrainDisplayState)
        {
            case GEDI_STATE_SOLID: // State 0: Solid
                buttonText.text = "GEDI Terrain (Solid)";
                meshFilter.mesh = terrainSolidGEDI;
                meshRenderer.material = terrainMaterial;
                terrainGEDI.SetActive(true); // Gameobject active
                break;

            case GEDI_STATE_WIREFRAME: // State 1: Wireframe
                buttonText.text = "GEDI Terrain (Wireframe)";
                meshFilter.mesh = terrainWireframeGEDI;
                meshRenderer.material = wireframeMaterial;
                terrainGEDI.SetActive(true); // Gameobject active
                break;

            case GEDI_STATE_OFF: // State 2: Off
                buttonText.text = "GEDI Terrain (Off)";
                terrainGEDI.SetActive(false); // Hide GameObject
                break;
        }
    }
    


}