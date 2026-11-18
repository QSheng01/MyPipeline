Shader "VFXTools/Editor/WhiteGray_Built_In"
{
	Properties
	{
		_luminosityAmount("GrasyScaleAmount",range(0,1)) = 0
	}

	SubShader
	{
		LOD 0
		Tags { "RenderType"="Transparent" "Queue"="Transparent+1100" }
		Blend off
		AlphaToMask Off
		Cull Off
		ColorMask RGBA
		ZWrite Off
		ZTest Always
		
		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct VertexInput
			{
				float4 vertex	: POSITION;
				float2 uv		: TEXCOORD0;
			};

			struct VertexOutput
			{
				float4 clipPos		: SV_POSITION;
				float2 uv			: TEXCOORD0;
				float4 srcPos		: TEXCOORD1;
			};
			half _luminosityAmount;
			sampler2D _SrcCapTex;
			sampler2D _Source;
			VertexOutput vert(VertexInput v)
			{
				VertexOutput o = (VertexOutput)0;

				o.clipPos = UnityObjectToClipPos(v.vertex.xyz);
				o.srcPos = ComputeScreenPos(o.clipPos);
				o.uv = v.uv;
				return o;
			}

			half4 frag ( VertexOutput i  ) : SV_Target
			{
				float2 srcuv = i.srcPos.xy/i.srcPos.w;
			#if UNITY_UV_STARTS_AT_TOP
				srcuv.y = 1 - srcuv.y;
			#endif
				float4 tex = tex2D(_Source, srcuv.xy);
				float WhiteGray = dot( tex.rgb, float3( 0.299, 0.587, 0.114 ));
				half3 finalcolor = lerp(tex.rgb,WhiteGray.xxx,1);
				return half4( finalcolor, 0);
			}
			ENDCG
		}
	}
}