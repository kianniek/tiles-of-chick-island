Shader "Custom/HiddenGeneralTexture"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_ColorTex("_MainTex", 2D) = "white" {}
	}

	CGINCLUDE
	#include "UnityCG.cginc"
	#include "Assets/Scripts/Rendering/AlphaBlend.hlsl"
	ENDCG

	SubShader
	{
		Tags
		{
			"Render" = "Color"
		}

		Pass
		{
			CGPROGRAM
			
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 viewNormal : NORMAL;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct f2s
			{
				fixed4 color0 : SV_Target0;
				fixed4 color1 : SV_Target1;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			float4 _Color;

			v2f vert(appdata v)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.viewNormal = COMPUTE_VIEW_NORMAL;

				return o;
			}

			f2s frag(v2f i)
			{
				fixed4 sample = tex2D(_MainTex, i.uv);
				fixed4 color = sample * _Color;

				// create return struct
				f2s f;
				f.color0 = color;
				f.color1 = float4(normalize(i.viewNormal) * 0.5 + 0.5, 1);

				// and return it!
				return f;
			}
			ENDCG
		}
	}

	SubShader
	{
		Tags
		{
			"Render" = "Water"
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 viewNormal : NORMAL;
			};

			struct f2s
			{
				fixed4 color0 : SV_Target0;
				fixed4 color1 : SV_Target1;
			};

			float _WaveSpeed;
			float _WaveHeight;

			v2f vert(appdata v)
			{
				v2f o;
				
				// move y position up and down, all vertices move up and down w/ the same amount
				float4 movedVertex = v.vertex + float4(0, (sin(v.vertex.y + _Time.y * _WaveSpeed) * _WaveHeight) + _WaveHeight, 0, 0);

				o.vertex = UnityObjectToClipPos(movedVertex);
				o.viewNormal = COMPUTE_VIEW_NORMAL;

				return o;
			}

			f2s frag(v2f i)
			{
				// create return struct
				f2s f;
				f.color0 = float4(0, 0, 1, 1); // only this one matters due to ColorMask
				f.color1 = float4(normalize(i.viewNormal) * 0.5 + 0.5, 1);

				// and return it!
				return f;
			}
			ENDCG
		}
	}

	SubShader
	{
		Tags
		{
			"Render" = "Tile"
		}

		Pass
		{
			CGPROGRAM
			
			#pragma vertex vert
			#pragma fragment frag

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 viewNormal : NORMAL;
			};

			struct f2s
			{
				fixed4 color0 : SV_Target0;
				fixed4 color1 : SV_Target1;
			};

			sampler2D _MaskTexture;
			float4 _MaskTexture_ST;

			float4 _Color;
			float4 _SecColor;

			v2f vert(appdata v)
			{
				v2f o;
				
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MaskTexture);
				o.viewNormal = COMPUTE_VIEW_NORMAL;

				return o;
			}

			f2s frag(v2f i)
			{
				// calculate color to return
				fixed4 sample = tex2D(_MaskTexture, i.uv);
				fixed4 color;
				if (sample.r == 0)
					color = _Color;
				else
					color = _SecColor;

				// create return struct
				f2s f;
				f.color0 = _Color;
				f.color1 = float4(normalize(i.viewNormal) * 0.5 + 0.5, 1);

				// and return it!
				return f;
			}
			ENDCG
		}
	}
}
