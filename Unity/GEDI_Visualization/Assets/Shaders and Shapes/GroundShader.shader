Shader "Custom/GroundTerrainShader"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.2, 0.8, 0.2, 1) // Default green
        _ElevationColors ("Elevation Colors", Color) = (0,0.6,0,1) // Placeholder, handled in code
        _Texture ("Terrain Texture", 2D) = "white" {}
        _UseTexture ("Use Texture", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float3 normal : TEXCOORD2;
                float2 uv : TEXCOORD0;
            };

            sampler2D _Texture;
            float4 _BaseColor;
            float _UseTexture;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Normalize elevation (assuming Y is up)
                float normalizedHeight = saturate(i.worldPos.y / 50.0); // Adjust 50.0 based on max elevation

                // Define gradient colors based on elevation
                fixed3 colorLow = fixed3(0.1, 0.6, 0.1);      // Dark Green
                fixed3 colorMid = fixed3(0.3, 0.8, 0.3);      // Light Green
                fixed3 colorHigh = fixed3(0.8, 0.8, 0.8);     // Gray
                fixed3 colorPeak = fixed3(1.0, 1.0, 1.0);     // White

                fixed3 baseColor;

                if (normalizedHeight < 0.5)
                {
                    // Interpolate between low and mid
                    float t = normalizedHeight / 0.5;
                    baseColor = lerp(colorLow, colorMid, t);
                }
                else if (normalizedHeight < 0.8)
                {
                    // Interpolate between mid and high
                    float t = (normalizedHeight - 0.5) / 0.3;
                    baseColor = lerp(colorMid, colorHigh, t);
                }
                else
                {
                    // Interpolate between high and peak
                    float t = (normalizedHeight - 0.8) / 0.2;
                    baseColor = lerp(colorHigh, colorPeak, t);
                }

                // Optional Texture
                fixed3 textureColor = tex2D(_Texture, i.uv).rgb;
                baseColor = lerp(baseColor, textureColor, _UseTexture);

                // Lighting calculations
                float3 normal = normalize(i.normal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = saturate(dot(normal, lightDir));

                // Enhanced shadows
                float ambient = 0.2; // Reduced ambient for deeper shadows
                float diffuse = pow(NdotL, 2.0); // Increased contrast

                fixed3 finalColor = baseColor * (ambient + diffuse);

                return fixed4(finalColor, 1.0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}