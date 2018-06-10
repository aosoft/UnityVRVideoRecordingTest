Shader "Unlit/CubemapToOtherProjection"
{
	Properties
	{
		_MainTex("Cubemap (RGB)", CUBE) = "" {}
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ PROJ_FISHEYE 
			#pragma multi_compile _ ANGLEFUNC_EQUISOLIDANGLE ANGLEFUNC_ORTHGONAL
			#pragma multi_compile _ LINEAR_TO_SRGB LINEAR_TO_BT709

			#include "UnityCG.cginc"

			#define PI     3.141592653589793
			#define TWOPI  6.283185307179587
			#define PIDIV2 1.570796326794897
			#define SIN45  0.707106781186548

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

			samplerCUBE _MainTex;
			uniform float4x4 _Matrix;
			uniform float4 _PositionScaleOffset;
			uniform float4 _UVScaleOffset;
			uniform float _FishEyeDiameterScale;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.vertex.xy = o.vertex * _PositionScaleOffset.xy + _PositionScaleOffset.zw;
				o.uv = v.uv.xy * _UVScaleOffset.xy + _UVScaleOffset.zw;

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float4 unit = float4(0, 0, 0, 1);

#if PROJ_FISHEYE

				float len = length(i.uv) * _FishEyeDiameterScale;
				if (len > 1.0)
				{
					return fixed4(0.0, 0.0, 0.0, 1.0);
				}

#if ANGLEFUNC_EQUISOLIDANGLE
				float ang_yz = 2.0 * asin(len * SIN45);
#elif ANGLEFUNC_ORTHGONAL
				float ang_yz = asin(len);
#else
				float ang_yz = len * PIDIV2;
#endif

				float ang_yx = atan2(i.uv.y, i.uv.x);
				float sin_ang_yz = sin(ang_yz);
				unit.x = cos(ang_yx) * sin_ang_yz;
				unit.y = sin(ang_yx) * sin_ang_yz;
				unit.z = cos(ang_yz);
#else
				unit.x = -sin(i.uv.x) * sin(i.uv.y);
				unit.y = -cos(i.uv.y);
				unit.z = -cos(i.uv.x) * sin(i.uv.y);
#endif

				float3 color = texCUBE(_MainTex, mul(_Matrix, unit).xyz);
#if LINEAR_TO_SRGB
				//	Linear to sRGB
				color = color < 0.0031308 ? 12.92 * color : 1.055 * pow(color, 1.0 / 2.4) - 0.055;
#elif LINEAR_TO_BT709
				//	Linear to BT.709
				color = color < 0.018 ? 4.5 * color : 1.099 * pow(color, 0.45) - 0.099;
#endif
				return float4(color, 1.0);
			}
			ENDCG
		}
	}
}
