// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Conversion/CubemapToEquidistanceProjection" {
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
				#pragma multi_compile _ RENDER_LEFT RENDER_RIGHT RENDER_TOP RENDER_BOTTOM
				//#pragma fragmentoption ARB_precision_hint_nicest
				#include "UnityCG.cginc"

				#define PI    3.141592653589793
				#define TWOPI 6.283185307179587

				struct v2f {
					float4 pos : POSITION;
					float2 uv : TEXCOORD0;
				};
		
				samplerCUBE _MainTex;
				uniform float4x4 _Matrix;

				v2f vert( appdata_img v )
				{
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
#if RENDER_LEFT
					o.pos.x = o.pos.x / 2 - 0.5;
#elif RENDER_RIGHT
					o.pos.x = o.pos.x / 2 + 0.5;
#elif RENDER_TOP
					o.pos.y = o.pos.y / 2 - 0.5;
#elif RENDER_BOTTOM
					o.pos.y = o.pos.y / 2 + 0.5;
#endif
					o.uv = (v.texcoord.xy - float2(0.5, 0.5)) * float2(PI, PI);
					return o;
				}
		
				fixed4 frag(v2f i) : COLOR 
				{
					float r = length(i.uv);
					float4 unit = float4(0, 0, 0, 1);

					if (r > PI/2)
					{
						return fixed4(0.0, 0.0, 0.0, 1.0);
					}

					float angle = atan2(i.uv.y, i.uv.x);
					float sin_r = sin(r);
					unit.x = cos(angle) * sin_r;
					unit.y = sin(angle) * sin_r;
					unit.z = cos(r);

					return texCUBE(_MainTex, mul(_Matrix, unit).xyz);
				}
			ENDCG
		}
	}
	Fallback Off
}