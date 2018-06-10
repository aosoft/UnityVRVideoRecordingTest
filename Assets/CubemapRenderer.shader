Shader "Unlit/CubemapRenderer"
{
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			#pragma multi_compile _ LINEAR_TO_SRGB LINEAR_TO_BT709
			
			#include "UnityCG.cginc"

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

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = v.vertex;
				o.uv = float2(v.vertex.x * 0.5 + 0.5, 0.5 + v.vertex.y * 0.5);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float3 color = tex2D(_MainTex, i.uv).xyz;

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
