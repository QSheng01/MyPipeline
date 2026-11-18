Shader "VFXTools/Editor/WhiteGray_URP"
{
	Properties
	{
		[HideInInspector] _MainTex ("Albedo (RGB)", 2D) = "white" {}
		_luminosityAmount("GrasyScaleAmount",range(0,1)) = 0
	}

	SubShader
	{
		LOD 0
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
		Cull Back
		AlphaToMask Off
		Blend One Zero, One Zero
        ZWrite On
        ZTest LEqual
        Offset 0 , 0
        ColorMask RGBA
		
		Pass
		{
			Name "Forward"
			Tags { "LightMode"="UniversalForward" }
			CGPROGRAM
			
			#pragma multi_compile_instancing
			#define ASE_SRP_VERSION 999999
			#define REQUIRE_OPAQUE_TEXTURE 1

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

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
			};
			half _luminosityAmount;
			sampler2D _MainTex;
			VertexOutput vert(VertexInput v)
			{
				VertexOutput o = (VertexOutput)0;

				o.clipPos = UnityObjectToClipPos(v.vertex.xyz);
				o.uv = v.uv;
				return o;
			}

			half4 frag ( VertexOutput i  ) : SV_Target
			{
				#if UNITY_UV_STARTS_AT_TOP
					i.uv.y = 1 - i.uv.y;
				#endif
				float4 tex = tex2D(_MainTex,i.uv);
				float WhiteGray = dot( tex.rgb, float3( 0.299, 0.587, 0.114 ));
				half3 finalcolor = lerp(tex.rgb,WhiteGray.xxx,_luminosityAmount);
				return half4( finalcolor, 1 );
			}
			ENDCG
		}
	}
}