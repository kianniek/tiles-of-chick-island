Shader "Custom/PostFX/LineMerger"
{
    Properties
    {
        [HideInInspector] _MainTex("Texture (Screen)", 2D) = "white" {}

        _FogDistance("Fog Distance", Float) = 1
        _FogEffect("Fog Effect", Float) = 1
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Tags 
        { 
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Assets/Scripts/Rendering/AlphaBlend.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float _Width;
            float _Height;

            float _FogDistance;
            float _FogEffect;

            float4 _BackgroundColor;

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _EdgeTex;
            uniform float4 _EdgeTex_ST;

            sampler2D _CameraDepthTexture;
            uniform float4 _CameraDepthTexture_ST;

            v2f vert (appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample original and edge color
                float4 originColor = tex2D(_MainTex, i.uv);
                float4 edgeColor = tex2D(_EdgeTex, i.uv);

                // blend edge w/ original color
                float4 returnColor = alphaBlend(edgeColor, originColor);

                // apply fog
                float depth = tex2D(_CameraDepthTexture, i.uv).r;
                depth = clamp(1 - (depth * 1000 - _FogDistance), 0, 1);
                returnColor = lerp(returnColor, _BackgroundColor, depth * _FogEffect);

                // return result
                return returnColor;
            }
            ENDCG
        }
    }
}
