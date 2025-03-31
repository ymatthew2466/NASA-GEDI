using System;
using UnityEngine;
using UnityEngine.UI; // Required for UI elements
using System.Collections.Generic;
using TriangleNet.Geometry;

using GEDIGlobals;
public class DEMTerrainCreator : MonoBehaviour
{
    [Header("DEM")]
    public Material terrainMaterial;  // Material for ground terrain
    public Texture2D demSource;
    public Texture2D demTexture;  // Texture for ground terrain
    
    [Tooltip("Geographic bounds [West, East, South, North]")]
    public Vector4 geoBounds = new Vector4(-71.5f, -71.4f, -46.6f, -46.5f);
    public Vector4 textureBounds = new Vector4(-71.5f, -71.4f, -46.6f, -46.5f);
    
    [Tooltip("Multiplier to convert DEM values to Unity units (affects terrain elevation).")]
    public int resolution = 256;
    public Button ToggleDemTerrain;

    private GameObject terrainDEM;
    private bool showDemTerrain;
    private Mesh terrainMesh;

    private float referenceLatitude;
    private float referenceLongitude;
    private float referenceElevation;
    void Start()
    {
        if (demSource == null)
        {
            Debug.LogError("DEM source not assigned!");
            return;
        }
        
        terrainDEM = new GameObject("DemTerrain");
        MeshFilter meshFilter = terrainDEM.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = terrainDEM.AddComponent<MeshRenderer>();

        referenceLongitude = (geoBounds.x + geoBounds.y)/2f;
        referenceLatitude = (geoBounds.z + geoBounds.w)/2f;
        referenceElevation = 50f;

        float height = (geoBounds.w - geoBounds.z) * 111000f * Params.SCALE;
        float cosLat = Mathf.Cos(geoBounds.z * Mathf.Deg2Rad);
        float width = (geoBounds.y - geoBounds.x) * 111000f * cosLat * Params.SCALE;

        terrainMaterial.mainTexture = demTexture;
        
        terrainMesh = GenerateTerrain(demSource, resolution);
        meshFilter.mesh = terrainMesh;
        meshRenderer.material = terrainMaterial;

        terrainDEM.transform.localScale = new Vector3(width, 1, height);

        float translate_x = (geoBounds.x - referenceLongitude) * 111000f * Params.SCALE;
        float translate_y = (0 - referenceElevation) * Params.TerrainScale;
        float translate_z = (geoBounds.z - referenceLatitude) * 111000f * cosLat * Params.SCALE;
        terrainDEM.transform.Translate(translate_x, translate_y, translate_z, Space.World);

        showDemTerrain = true;
        ToggleTerrain();
        ToggleDemTerrain.onClick.AddListener(ToggleTerrain);
    }

    public Mesh GenerateTerrain(Texture2D demSrc, int resolution)
    {
        int verticesPerSide = resolution;
        
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int z = 0; z < verticesPerSide; z++)
        {
            for (int x = 0; x < verticesPerSide; x++)
            {
                // normalize grid coordinates in the range [0, 1]
                float u = x / (float)(verticesPerSide - 1);
                float v = z / (float)(verticesPerSide - 1);

                // using the grid's normalized u,v as image coordinates.
                float demValue = demSrc.GetPixelBilinear(u, v).r * Params.TerrainScale;
                // Debug.Log(demValue);

                vertices.Add(new Vector3(u, demValue, v));

                float world_u = (u * (geoBounds.y - geoBounds.x) + geoBounds.x - textureBounds.x ) / (textureBounds.y - textureBounds.x);
                float world_v = (v * (geoBounds.w - geoBounds.z) + geoBounds.z - textureBounds.z ) / (textureBounds.w - textureBounds.z);
                uvs.Add(new Vector2(world_u, world_v));
            }
        }

        // Create triangles (two per quad).
        for (int z = 0; z < verticesPerSide - 1; z++)
        {
            for (int x = 0; x < verticesPerSide - 1; x++)
            {
                int topLeft = z * verticesPerSide + x;
                int topRight = topLeft + 1;
                int bottomLeft = (z + 1) * verticesPerSide + x;
                int bottomRight = bottomLeft + 1;

                triangles.Add(topLeft);
                triangles.Add(bottomLeft);
                triangles.Add(topRight);
                triangles.Add(topRight);
                triangles.Add(bottomLeft);
                triangles.Add(bottomRight);
            }
        }

        // Create and assign the mesh.
        Mesh unityMesh = new Mesh();
        unityMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        unityMesh.vertices = vertices.ToArray();
        unityMesh.triangles = triangles.ToArray();
        unityMesh.uv = uvs.ToArray();
        unityMesh.RecalculateNormals();
        return unityMesh;
    }

    void ToggleTerrain()
    {
        showDemTerrain = !showDemTerrain;
        terrainDEM.SetActive(showDemTerrain);

        if (showDemTerrain){
            ToggleDemTerrain.GetComponentInChildren<Text>().text = "Terrain-X (on)";
        }
        else{
            ToggleDemTerrain.GetComponentInChildren<Text>().text = "Terrain-X (off)";
        }
            
    }
}
