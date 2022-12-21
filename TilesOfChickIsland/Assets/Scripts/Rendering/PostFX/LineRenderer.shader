Shader "Custom/PostFX/LineRenderer"
{
    Properties
    {
        _Color("Line Color", Color) = (0, 0, 0, 1)

        [Space(5)]

        _KernelExponent("Kernel Exponent", Float) = 1
        
        [Space(5)]

        _DepthSensitivity("Depth Sensitivity", Float) = 1
        _DepthThreshold("Depth Threshold", Float) = 1
        _DepthWeight("Depth Weight", Float) = 1

        [Space(5)]

        _NormalSensitivity("Normal Sensitivity", Float) = 1
        _NormalThreshold("Normal Threshold", Float) = 1
        _NormalWeight("Normal Weight", Float) = 1

        [Space(5)]

        _FlatSensitivity("Flat Sensitivity", Float) = 1
        _FlatThreshold("Flat Threshold", Float) = 1
        _FlatWeight("Flat Weight", Float) = 1

        [Space(5)]

        _DepthNormalThreshold("Depth Normal Threshold", Float) = 1
        _DepthNormalThresholdScale("Depth Normal Threshold Scale", Float) = 1
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Stencil
        {
            Ref 10
            Comp NotEqual
            Pass Replace
        }

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Assets/Scripts/Rendering/AlphaBlend.hlsl"

            // samplers
            sampler2D _CameraNormalsTexture;
            uniform float4 _CameraNormalsTexture_ST;

            sampler2D _CameraDepthTexture;
            uniform float4 _CameraDepthTexture_ST;

            sampler2D _CameraColorTexture;
            uniform float4 _CameraColorTexture_ST;

            float _Width;
            float _Height;
            float4x4 _ClipToView;

            float4 _Color;
            float4 _BackgroundColor;

            float _KernelExponent;
            float _SampleDistance;

            float _DepthSensitivity;
            float _DepthThreshold;
            float _DepthWeight;

            float _NormalSensitivity;
            float _NormalThreshold;
            float _NormalWeight;

            float _FlatSensitivity;
            float _FlatThreshold;
            float _FlatWeight;

            float _DepthNormalThreshold;
            float _DepthNormalThresholdScale;

            float getEdge(sampler2D s, float2 uv, float sensitivity)
            {
                // determine offset on sample
                float uvDist = (_SampleDistance * float2(1 / _Width, 1 / _Height)).x;

                // take 5 samples: center, left, right, up and down
                float4 c = tex2D(s, uv);
                float4 l = tex2D(s, uv - float2(uvDist, 0));
                float4 r = tex2D(s, uv + float2(uvDist, 0));
                float4 u = tex2D(s, uv + float2(0, uvDist));
                float4 d = tex2D(s, uv - float2(0, uvDist));

                // apply kernel
                float4 result = 0;
                result += c * -4;
                result += l;
                result += r;
                result += u;
                result += d;

                return pow(saturate(sqrt(dot(result, result)) * sensitivity), _KernelExponent);
            }

            float4 getEdgeColor(float edge, float threshold)
            {
                if (edge < threshold)
                    return 0;
                else
                    return _Color;
            }

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct vout
            {
                float4 vertex : SV_Position;
                float2 uv : TEXCOORD0;
                float3 viewSpaceDir : TEXCOORD1;
            };

            vout vert(appdata v)
            {
                vout o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _CameraColorTexture);
                o.viewSpaceDir = mul(_ClipToView, o.vertex).xyz;

                return o;
            }

            float4 frag(vout i) : SV_Target
            {
                // sample colors
                float3 flat = tex2D(_CameraColorTexture, i.uv).rgb;
                float3 normal = tex2D(_CameraNormalsTexture, i.uv).rgb;
                float depth = tex2D(_CameraDepthTexture, i.uv).r;

                // get edges
                float flatEdge = getEdge(_CameraColorTexture, i.uv, _FlatSensitivity);
                float normalEdge = getEdge(_CameraNormalsTexture, i.uv, _NormalSensitivity);
                float depthEdge = getEdge(_CameraDepthTexture, i.uv, _DepthSensitivity);

                 // calculate the view normal to modulate depth threshold based on the diff between camera's view normal and normal of the surface
                 float3 viewNormal = normal * 2 - 1;
                 float NdotV = 1 - dot(viewNormal, -i.viewSpaceDir);

                 // calculate new normal threshold, rescaling NdotV between 0 and 1 with a lower bound cutoff
                 float normalThreshold01 = saturate((NdotV - _DepthNormalThreshold) / (1 - _DepthNormalThreshold));

                 // again, scale it: from [lowerbound,1] to [1,upperbound] 
                 float normalThreshold = normalThreshold01 * _DepthNormalThresholdScale + 1;

                 // since the depth buffer isn't linear: recalculate threshold based on it's depth
                 // multiply it by the normalThreshold to ignore steep surfaces
                 float depthThreshold = _DepthThreshold * depth * normalThreshold;

                 // calculate each edge color
                 float4 depthEdgeColor = getEdgeColor(depthEdge, depthThreshold);
                 float4 flatEdgeColor = getEdgeColor(flatEdge, _FlatThreshold);
                 float4 normalEdgeColor = getEdgeColor(normalEdge, _NormalThreshold);

                 // weigthed average
                 float4 edgeColor = ((depthEdgeColor * _DepthWeight) +
                     (flatEdgeColor * _FlatWeight) +
                     (normalEdgeColor * _NormalWeight)) / (_DepthWeight + _FlatWeight + _NormalWeight);

                 // black origin color? that's the sky
                 if (flat.r == 0 && flat.b == 0 && flat.g == 0)
                 {
                     flat = _BackgroundColor.rgb;
                     edgeColor = float4(0, 0, 0, 0);
                 }

                 // blue flat color? that's the water, decrease lines
                 if(flat.r == 0 && flat.b == 1 && flat.g == 0) 
                 {
                     edgeColor *= 0.3f;
                 }

                 // return edge
                 return edgeColor;
            }
            ENDCG
        }
    }
}