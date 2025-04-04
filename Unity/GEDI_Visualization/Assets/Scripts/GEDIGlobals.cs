using UnityEngine;
using System;
using System.Collections.Generic;

namespace GEDIGlobals
{

    public class Params
    {
        public const float SCALE = 0.01f;
        public const float TerrainScale = 0.4f;

    }

    public class TerrainPoint
    {
        public Vector3 position;
        public float latitude;
        public float longitude;
        public float elevation;
        
        public TerrainPoint(Vector3 pos, float lat, float lon, float elev)
        {
            position = pos;
            latitude = lat;
            longitude = lon;
            elevation = elev;
        }
    }
}

    