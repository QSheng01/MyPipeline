Shader "ShiYue/URP/Grass/Grass BendRenderer"
{
	SubShader
		{
			Tags { "RenderType" = "Opaque" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }

			Pass
			{
				ZWrite Off
				ZTest Always
				Cull Off

				HLSLPROGRAM
				#pragma prefer_hlslcc gles
				#pragma exclude_renderers d3d11_9x

				#pragma vertex vert
				#pragma fragment frag

				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
				
				struct Attributes
				{
					float4 positionOS : POSITION;
					float2 uv : TEXCOORD0;
				};
			
				struct Varyings
				{
					float4 positionCS : SV_POSITION;
					float2 uv : TEXCOORD0;
				};

				TEXTURE2D(_BendMapInput);
				SAMPLER(sampler_BendMapInput);

				TEXTURE2D(_BendMap);
				SAMPLER(sampler_BendMap);

				Varyings vert(Attributes input)
				{
					Varyings output = (Varyings)0;

					output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
					output.uv = input.uv;

#ifdef FLIP_UV
					output.uv.y = 1 - output.uv.y;
#endif

					return output;
				}

				half4 frag(Varyings input) : SV_Target
				{
					half4 c = SAMPLE_TEXTURE2D(_BendMap, sampler_BendMap, input.uv);

					half4 col = SAMPLE_TEXTURE2D(_BendMapInput, sampler_BendMapInput, input.uv);

					float param = step(0.01, length(col.rb - half2(0.5, 0.5)));

					c.rgb = lerp(c.rgb, col.rgb, param);

					c.a += 0.01;
				
					return c;
				}
			ENDHLSL
		}
	}
	FallBack "Hidden/InternalErrorShader"
}