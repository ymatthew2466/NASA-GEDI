using UnityEngine;
using System;
using System.Collections.Generic;


using GEDIGlobals;

public class WaveformTools
{

    public static Mesh GenerateCylinderMesh(float[] waveformValues, int[] segmentLengths, float[] physicalPositions, Vector3 slantDirection)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector2> heights = new List<Vector2>(); // UV2 for height data
        
        int circleResolution = 12;
        float angleIncrement = Mathf.PI * 2 / circleResolution;
        
        // calc actual height
        float actualHeight = 76.8f;
        float totalHeight = actualHeight * Params.TerrainScale;
        
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

            radius = radius * Params.SCALE * 0.3f;
            radius = Math.Clamp(radius, 0f, 120f * Params.SCALE);
            
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


    public static Vector3 CalculateISSDirection(CSVParser.GEDIDataPoint dataPoint)
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

    public static void NormalizeWaveform(float[] waveformValues, float targetSum)
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


}