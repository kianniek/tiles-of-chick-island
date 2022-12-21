Shader "Custom/Cartoon"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Main Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "Render" = "Color"
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
            #pragma multi_compile_fwdbase     

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float3 viewDir : TEXCOORD1;
                SHADOW_COORDS(2)
            };

            v2f vert (appdata v)
            {
                v2f o;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.viewDir = WorldSpaceViewDir(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                TRANSFER_SHADOW(o)

                return o;
            }

            float4 frag (v2f i) : SV_TARGET
            {
                float attenuation = LIGHT_ATTENUATION(i);

                float NdotL = saturate(dot(i.normal, _WorldSpaceLightPos0.xyz)) * attenuation;
                float toonL = step(0.5, NdotL);

                fixed4 sample = tex2D(_MainTex, i.uv);
                fixed4 color = sample * _Color;

                half4 shadow = UNITY_LIGHTMODEL_AMBIENT;
                half3 resultColor = lerp(shadow.rgb * color.rgb, _LightColor0.xyz * toonL * color.rgb, toonL);

                return half4(resultColor, 1);
            }
            ENDCG
        }

        // Shadow casting support.
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
    

}
