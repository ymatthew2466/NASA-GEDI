using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required for UI elements

public class TerrainMapper : MonoBehaviour
{
    [Header("Terrain Objects")]
    public MeshFilter[] involvedObjects;

    [Header("Terrain Scale")]
    public float heightMultiplier = 1f;

    [Header("Terrain Control")]
    public Button hmapToggle;
    public Dropdown hmapDropdown; // Assign in the inspector
    public Texture2D[] textures; // Assign your textures in the inspector

    private float minHeight;
    private float maxHeight;
    private Texture2D hMap;
    private bool enableHeightMap=true;
    private float default_radius = 0.5f;

    void Start()
    {
        hMap = textures[0];
        // hmapDropdown.onValueChanged.AddListener(delegate { OnDropdownValueChanged(hmapDropdown); });
        hmapToggle.onClick.AddListener(ToggleHeightMap);
        var colors = hmapToggle.colors;
        colors.selectedColor = new Color(0.1f, 0.2f, 0.9f);
        colors.normalColor = colors.selectedColor;
        hmapToggle.colors = colors;
        UpdateHeightMap();
    }

    void ToggleHeightMap()
    {
        // Debug.Log("hihihi");
        enableHeightMap = !enableHeightMap;

        var colors = hmapToggle.colors;
        if (enableHeightMap)
        {
            colors.selectedColor = new Color(0.1f, 0.2f, 0.9f);
            colors.normalColor = colors.selectedColor;
            hmapToggle.GetComponentInChildren<Text>().text = "HeightMap: On";
        }
        else
        {
            colors.selectedColor = new Color(0.2f, 0.5f, 0.8f);
            colors.normalColor = colors.selectedColor;
            hmapToggle.GetComponentInChildren<Text>().text = "HeightMap: Off";
        }

        hmapToggle.colors = colors;
        UpdateHeightMap();
    }

    void OnDropdownValueChanged(Dropdown dropdown)
    {
        // Change the texture of the target object based on the selected dropdown index
        if (dropdown.value < textures.Length)
        {
            hMap = textures[dropdown.value];
        }

        UpdateHeightMap();
    }

    private void UpdateHeightMap()
    {
        foreach (MeshFilter meshFilter in involvedObjects)
        {
            if (enableHeightMap) EnableHeightMap(meshFilter);
            else EnablePlainMap(meshFilter);
        }
    }

    void EnableHeightMap(MeshFilter meshFilter)
    {
        minHeight = 0.1f;
        maxHeight = 2f;

        Mesh mesh = meshFilter.mesh;

        Vector3[] vertices = mesh.vertices;
        Vector2[] uvs = mesh.uv;

        for (int i = 0; i < vertices.Length; i++)
        {
            float u = uvs[i].x;
            float v = uvs[i].y;

            float elevation = hMap.GetPixelBilinear(u, v).r;
            float height = Mathf.Min(maxHeight, (1 + elevation * heightMultiplier * 0.00131f) * default_radius);
            vertices[i] = vertices[i].normalized * height;
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
    }

    void EnablePlainMap(MeshFilter meshFilter)
    {
        Mesh mesh = meshFilter.mesh;

        Vector3[] vertices = mesh.vertices;

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = vertices[i].normalized * default_radius;
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
    }
}
