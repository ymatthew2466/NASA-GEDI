using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class DEMTerrainCreator : MonoBehaviour
{
    [Header("DEM")]
    public Texture2D demTexture;
    
    [Tooltip("Geographic bounds [West, East, South, North]")]
    public Vector4 geoBounds = new Vector4(-71.5f, -71.4f, -46.6f, -46.5f);
    
    [Tooltip("Multiplier to convert DEM values to Unity units (affects terrain elevation).")]

    [Header("Terrain Dimensions // REMOVE LATER")]
    public float terrainWidth = 1000f;
    
    public float terrainDepth = 1000f;
    
    [Tooltip("Number of vertices along one side")]
    public int resolution = 256;

    private Mesh mesh;

    void Start()
    {
        if (demTexture == null)
        {
            Debug.LogError("DEM texture not assigned!");
            return;
        }
        GenerateTerrain();
    }

    void GenerateTerrain()
    {
        int verticesPerSide = resolution;
        Vector3[] vertices = new Vector3[verticesPerSide * verticesPerSide];
        Vector2[] uvs = new Vector2[verticesPerSide * verticesPerSide];
        int[] triangles = new int[(verticesPerSide - 1) * (verticesPerSide - 1) * 6];

        int index = 0;
        for (int z = 0; z < verticesPerSide; z++)
        {
            for (int x = 0; x < verticesPerSide; x++)
            {
                // normalize grid coordinates in the range [0, 1]
                float u = x / (float)(verticesPerSide - 1);
                float v = z / (float)(verticesPerSide - 1);

                // map normalized grid position to flat world space
                // NOTE: MUST CHANGE TO ACTUAL GEOGRAPHIC BOUNDS, NO SCALING
                float posX = (u - 0.5f) * terrainWidth;
                float posZ = (v - 0.5f) * terrainDepth;

                // map grid u,v to geographic coordinates
                // [West, East, South, North])
                float longitude = Mathf.Lerp(geoBounds.x, geoBounds.y, u);
                float latitude = Mathf.Lerp(geoBounds.z, geoBounds.w, v);

                // using the grid's normalized u,v as image coordinates.
                float demValue = demTexture.GetPixelBilinear(u, v).r;
                float posY = demValue;

                vertices[index] = new Vector3(posX, posY, posZ);
                uvs[index] = new Vector2(u, v);
                index++;
            }
        }

        // Create triangles (two per quad).
        int triIndex = 0;
        for (int z = 0; z < verticesPerSide - 1; z++)
        {
            for (int x = 0; x < verticesPerSide - 1; x++)
            {
                int topLeft = z * verticesPerSide + x;
                int topRight = topLeft + 1;
                int bottomLeft = (z + 1) * verticesPerSide + x;
                int bottomRight = bottomLeft + 1;

                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = topRight;

                triangles[triIndex++] = topRight;
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = bottomRight;
            }
        }

        // Create and assign the mesh.
        mesh = new Mesh
        {
            name = "DEM Terrain Mesh",
            vertices = vertices,
            triangles = triangles,
            uv = uvs
        };

        mesh.RecalculateNormals();

        MeshFilter mf = GetComponent<MeshFilter>();
        mf.mesh = mesh;
    }
}
