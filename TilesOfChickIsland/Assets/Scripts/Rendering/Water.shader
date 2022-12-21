// Inspired on: 
// - https://roystan.net/articles/toon-water/ 
// - https://danielilett.com/2020-04-05-tut5-3-urp-stylised-water/

Shader "Custom/Water"
{
    Properties
    {
        [Header(Water Variables)]
        _WaterColor("Shallow Water Color", Color) = (1,1,1,1)

        [Header(Flow Variables)]
        _FlowTexture("Flow Texture", 2D) = "white" {}
        _FlowSize("Flow Size", Float) = 2
        _FlowStrength("Flow Strength", Float) = 0.05
        _FlowSpeed("Flow Speed", Float) = 0.01

        [Header(Foam Variables)][Space(10)]
        [NoScaleOffset] _FoamTexture("Foam Texture", 2D) = "white" {}
        _SurfaceFoamColor("Surface Foam Color", Color) = (1,1,1,1)
        _LightFoamColor("Light Foam Color", Color) = (1,1,1,1)
        _DarkFoamColor("Dark Foam Color", Color) = (1,1,1,1)
        _FoamDistance("Foam Distance", Float) = 0.4
        
        [Header(Wave Variables)][Space(10)]
        _WaveSpeed("Wave Speed", Float) = 1
        _WaveHeight("Wave Height", Float) = 0.1

        [Header(Big Blob Variables)] [Space(10)]
        _BlobColor("Blob Color", Color) = (1,1,1,1)
        _CellSize("Cell Size", float) = 1
        _ScrollSpeed("Scroll Speed", float) = 1
        _Levels("Levels", float) = 6
        _Seed("Seed", float) = 0
        _Bias("Bias", float) = 0.5
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "Render" = "Water"
        }

        Pass
        {
            Tags
            {
                "LightMode" = "ForwardBase"
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Assets/Scripts/Rendering/ClassicNoise3D.hlsl"
            #include "Assets/Scripts/Rendering/AlphaBlend.hlsl"

            float4 _WaterColor;

            sampler2D _CameraDepthTexture;

            float _WaveSpeed;
            float _WaveHeight;

            sampler2D _FlowTexture;
            float4 _FlowTexture_ST;
            float _FlowSize;
            float _FlowStrength;
            float _FlowSpeed;

            sampler2D _FoamTexture;
            float4 _FoamTexture_ST;
            float _FoamDistance;
            float4 _SurfaceFoamColor;
            float4 _LightFoamColor;
            float4 _DarkFoamColor;

            float4 _BlobColor;
            float _CellSize;
            float _ScrollSpeed;
            float _Levels;
            float _Seed;
            float _Bias;            

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD2;
                float3 worldPos : TEXCOORD6;
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
            };

            v2f vert(appdata v)
            {
                v2f o;

                // move y position up and down, all vertices move up and down w/ the same amount
                float4 movedVertex = v.vertex + float4(0, (sin(v.vertex.y + _Time.y * _WaveSpeed) * _WaveHeight) + _WaveHeight, 0, 0);

                o.uv = TRANSFORM_TEX(v.uv, _FlowTexture);
                o.vertex = UnityObjectToClipPos(movedVertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                o.worldPos = movedVertex;
                o.normal = UnityObjectToWorldNormal(v.normal);

                return o;
            }

            float4 frag(v2f i) : SV_TARGET
            {
                // sample flow map, moves over time
                float2 modifiedUV = i.uv + (_Time.y * (_FlowSpeed / _FlowSize));
                half4 flowNormal = tex2D(_FlowTexture, modifiedUV) * _FlowStrength;

                // sample foam, have two different samples w/ small offset
                // the foam is moved by the flow map!
                float2 foamUV = (flowNormal + i.uv) * _FlowSize;
                fixed4 foamSample1 = tex2D(_FoamTexture, foamUV);
                fixed4 foamSample2 = tex2D(_FoamTexture, foamUV + float2(0.1, 0.1));

                // determine depth values, used for foam at edges
                float existingDepth = tex2Dproj(_CameraDepthTexture, (UNITY_PROJ_COORD(i.screenPos) + flowNormal)).r;
                float existingDepthLinear = LinearEyeDepth(existingDepth);
                float depthDifference = existingDepthLinear - i.screenPos.w;
                float foamCutoff = step(0.1, saturate(depthDifference / _FoamDistance));

                // calculate noise blobs, different water colors 
                float3 value = i.worldPos / _CellSize;
                value.y += (_Time.y) * _ScrollSpeed + _Seed;
                float noise = cnoise(value) + 0.5;
                noise = frac(noise * _Levels);
                noise = step(_Bias, noise);
                float4 noiseColor = noise * _BlobColor;

                // blend watercolors
                float4 noiseWaterColor = alphaBlend(noiseColor, _WaterColor);

                // determine water / foam color
                float4 waterColor = lerp(_SurfaceFoamColor, lerp(lerp(noiseWaterColor, _DarkFoamColor, foamSample2), _LightFoamColor, foamSample1), foamCutoff);

                return waterColor;
            }
            ENDCG
        }

        // Shadow casting support.
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
