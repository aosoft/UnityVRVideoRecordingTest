// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Conversion/CubemapToOtherProjection" {
  Properties {
		_MainTex ("Cubemap (RGB)", CUBE) = "" {}
	}

	Subshader {
		Pass {
			ZTest Always Cull Off ZWrite Off
			Fog { Mode off }      

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma fragmentoption ARB_precision_hint_fastest
				#pragma multi_compile _ PROJ_FISHEYE 
				#pragma multi_compile _ ANGLEFUNC_EQUISOLIDANGLE ANGLEFUNC_ORTHGONAL
				//#pragma fragmentoption ARB_precision_hint_nicest
				#include "UnityCG.cginc"

				#define PI     3.141592653589793
				#define TWOPI  6.283185307179587
				#define PIDIV2 1.570796326794897

				struct v2f {
					float4 pos : POSITION;
					float2 uv : TEXCOORD0;
				};
		
				samplerCUBE _MainTex;
				uniform float4x4 _Matrix;
				uniform float4 _PositionScaleOffset;
				uniform float4 _UVScaleOffset;

				v2f vert( appdata_img v )
				{
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.pos.xy = o.pos * _PositionScaleOffset.xy + _PositionScaleOffset.zw;
					o.uv = v.texcoord.xy * _UVScaleOffset.xy + _UVScaleOffset.zw;

					return o;
				}
		
				fixed4 frag(v2f i) : COLOR 
				{
					float4 unit = float4(0, 0, 0, 1);

#if PROJ_FISHEYE

					float len = length(i.uv);
					if (len > 1.0)
					{
						return fixed4(0.0, 0.0, 0.0, 1.0);
					}

#if ANGLEFUNC_EQUISOLIDANGLE
					float ang_yz = 2.0 * asin(len / 2.0);
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
					float theta = i.uv.y;
					float phi = i.uv.x;

					unit.x = sin(phi) * sin(theta) * -1;
					unit.y = cos(theta) * -1;
					unit.z = cos(phi) * sin(theta) * -1;
#endif
					return texCUBE(_MainTex, mul(_Matrix, unit).xyz);
				}
			ENDCG
		}
	}
	Fallback Off
}