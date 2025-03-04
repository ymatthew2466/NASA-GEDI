Shader "Custom/WaveformShader"
{
    Properties
    {
        _NoiseTex ("Noise Texture 1", 2D) = "white" {}
        _NoiseIntensity ("Noise Intensity 1", Range(0, 5)) = 2.5
        _NoiseScale ("Noise Scale 1", Range(0.1, 10.0)) = 0.1

        _NoiseTex2 ("Noise Texture 2", 2D) = "black" {}
        _NoiseIntensity2 ("Noise Intensity 2", Range(0, 2)) = 1.0
        _NoiseScale2 ("Noise Scale 2", Range(0.05, 0.5)) = 0.2

        _StripeFrequency ("Stripe Frequency", Range(0.0, 300)) = 100.0
        _StripeMin("Stripe Min Brightness", Range(0.0, 1)) = 0.65
        _StripeMax("Stripe Max Brightness", Range(0.0, 1)) = 0.85
        _AmbientLight("Ambient Light", Float) = 0.15
        _DiffuseLight("Diffuse Strength", Float) = 0.8
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _NoiseTex;
            float _NoiseIntensity;
            float _NoiseScale;

            sampler2D _NoiseTex2;
            float _NoiseIntensity2;
            float _NoiseScale2;

            float _StripeFrequency;
            float _StripeMin;
            float _StripeMax;
            float _AmbientLight;
            float _DiffuseLight;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1; // heights
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float2 localPosXZ : TEXCOORD2;
                float2 uv2 : TEXCOORD3; // heights
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                o.uv = v.uv;
                o.localPosXZ = v.vertex.xz;
                o.uv2 = v.uv2; // heights
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // normalized height in [0,1]
                float normalizedHeight = i.uv.y;

                // actual height in meters
                float totalHeight = i.uv2.x;
                float currentHeight = normalizedHeight * totalHeight;

                // static height threshold
                // float height1 = 5.0; 
                // float height2 = 15.0;
                // float height3 = 30.0;
                // float height4 = 50.0; 

                // Update these lines in your shader
                float height1 = 5.0 * 0.02; // 0.1
                float height2 = 15.0 * 0.02; // 0.3
                float height3 = 30.0 * 0.02; // 0.6
                float height4 = 50.0 * 0.02; // 1.0


                // float height5 = 20.0;
                // above: red

                // colors
                // fixed3 color1 = fixed3(1.0, 0.3, 0.3);        // Red
                fixed3 color1 = fixed3(1.0, 0.0, 0.0);        // Red
                fixed3 color2 = fixed3(0.97, 0.98, 0.157);    // Yellow
                fixed3 color3 = fixed3(0.3, 1.0, 0.3);        // Green
                fixed3 color4 = fixed3(0.3, 0.3, 1.0);        // Blue
                fixed3 color5 = fixed3(1.0, 0.45, 0.925);     // Pink

                // fixed3 color1 = fixed3(1.0, 0.6, 0.0);        // Orange
                // fixed3 color2 = fixed3(0.6, 0.6, 0.6);        // Gray
                // fixed3 color3 = fixed3(0.3, 1.0, 0.3);        // Green
                // fixed3 color4 = fixed3(0.97, 0.98, 0.157);    // Yellow
                // fixed3 color5 = fixed3(1.0, 0.45, 0.925);     // Pink
                // fixed3 color6 = fixed3(1.0, 0.3, 0.3);        // Red

                fixed3 baseColor;

                if (currentHeight < height1)
                {
                    baseColor = color1;
                }
                else if (currentHeight < height2)
                {
                    float t = (currentHeight - height1) / (height2 - height1);
                    baseColor = lerp(color1, color2, t);
                }
                else if (currentHeight < height3)
                {
                    float t = (currentHeight - height2) / (height3 - height2);
                    baseColor = lerp(color2, color3, t);
                }
                else if (currentHeight < height4)
                {
                    float t = (currentHeight - height3) / (height4 - height3);
                    baseColor = lerp(color3, color4, t);
                }
                else
                {
                    float t = (currentHeight - height4) / (height4 - height3);
                    baseColor = lerp(color4, color5, t);

                }
                // else if (currentHeight < height5)
                // {
                //     float t = (currentHeight - height4) / (height5 - height4);
                //     baseColor = lerp(color4, color5, t);
                // }
                // else
                // {
                //     float t = (currentHeight - height5) / (height5 - height4);
                //     baseColor = lerp(color5, color6, t);
                // }

                // Calculate stripe based on absolute height
                float stripePhase = floor(currentHeight * _StripeFrequency);
                float stripe = fmod(stripePhase, 2.0) < 1.0 ? _StripeMax : _StripeMin;

                // Sample first noise texture
                float2 noiseUV1 = i.localPosXZ * _NoiseScale;
                float noiseValue1 = tex2D(_NoiseTex, noiseUV1).r;
                float noise1 = noiseValue1 * _NoiseIntensity;

                // Sample second noise texture
                float2 noiseUV2 = i.localPosXZ * _NoiseScale2;
                float noiseValue2 = tex2D(_NoiseTex2, noiseUV2).r;
                float noise2 = noiseValue2 * _NoiseIntensity2;

                // Combine noises
                float combinedNoise = noise1 + noise2;

                // Final color with stripes and noise
                fixed3 finalColor = baseColor * stripe + combinedNoise;
                finalColor = saturate(finalColor);

                // Diffuse lighting
                float NdotL = saturate(dot(i.normal, normalize(_WorldSpaceLightPos0.xyz)));
                float ambient = _AmbientLight;
                float diffuse = pow(NdotL, _DiffuseLight);
                finalColor *= (ambient + diffuse);

                return fixed4(finalColor, 1.0);
            }
            ENDCG
        }
    }
}
