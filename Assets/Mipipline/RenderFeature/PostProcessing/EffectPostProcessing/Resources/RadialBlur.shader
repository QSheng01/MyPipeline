Shader "Post/Effects/RadiaBlur"
{
	Properties {
		_MainTex ("Texture", 2D) = "white" {}

		_BlurCenter           ("BlurCenter",           Vector) = (1,1,1,1)
		_RadialBlurParameter  ("RadialBlurParameter",  Vector) = (1,1,1,1)

		_ColorOffsetParameter ("ColorOffsetParmeter",  Vector) = (1,1,1,1)
	}
	SubShader {
		Tags { "RenderPipeline"="UniversalPipeline" }

		HLSLINCLUDE
			#pragma multi_compile _ _RGBSPLIT

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

			TEXTURE2D(_MainTex);				SAMPLER(sampler_MainTex);

			CBUFFER_START(UnityPerMaterial)

			float4 _RadialBlurParameter;
			/// x : blurStrength
			/// y : blurRadius
			/// z : iterations
			/// w : definition

			float4 _ColorOffsetParameter;
			/// x : ROffset
			/// y : GOffset
			/// z : BOffset
			/// w : RGBSplitStrength

			float2 _BlurCenter;

			CBUFFER_END

			struct v2input
			{
			    float4 positionOS       : POSITION;
			    float2 uv               : TEXCOORD0;
			    UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
			    float4 positionCS : SV_POSITION;
			    float2 uv   : TEXCOORD0;
			    UNITY_VERTEX_OUTPUT_STEREO
			};

	     	v2f vert(v2input IN) {
                v2f OUT;

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv  = IN.uv;

               return OUT;
           }

		     real2 RGBSplitUVOffset(real2 uv, real strength)
            {
                float2 coords = 2.0 * uv - 1.0;
                float2 end = uv - coords * dot(coords, coords) * strength;
                float2 delta = (end - uv) / 3.0;

                return delta;

                ///
                /// R: uv
                /// G: delta + uv
                /// B: delta * 2 + uv
            }

		ENDHLSL

		Pass {
			Name "RadialBlur_P0"
			Tags
			{
				"LightMode"="UniversalForward"
			}
			Cull Off ZWrite Off ZTest Always

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag



			half4 frag(v2f IN) : SV_Target {

				half4 outColor = 0;

				float blurStrength = _RadialBlurParameter.x;

				//UNITY_LOOP
				UNITY_UNROLLX(20)
				for (float j = 0.000000000; j < 1.00000000; j += 0.05)
				{
					float  alpha = j * 0.2;

					float  tiling = 0;
					float2 offset = float2(0, 0);
					float2 uv     = float2(0, 0);

					tiling = 1 - alpha;
					offset = _BlurCenter * alpha;

					uv = IN.uv * tiling + offset;
					uv = lerp(IN.uv, uv, blurStrength);

					outColor += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
				}

				outColor *= 0.05;
				return outColor;
			}
			ENDHLSL
		}

		Pass {
			Name "RadialBlur_P1"
			Tags
			{
				"LightMode"="UniversalForward"
			}
			Cull Off ZWrite Off ZTest Always

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			TEXTURE2D(_RadiaBlurTex);		SAMPLER(sampler_RadiaBlurTex);

			half4 frag(v2f IN) : SV_Target {

				half4 col          = SAMPLE_TEXTURE2D(_MainTex,  sampler_MainTex,  IN.uv);

				float  blurRadius     = _RadialBlurParameter.y;
				float  definition     = _RadialBlurParameter.a;

				float alpha  = length((IN.uv - _BlurCenter.xy)) * 2;
				alpha += blurRadius;

			#ifdef _RGBSPLIT

				float rOffset = _ColorOffsetParameter.r;
				float gOffset = _ColorOffsetParameter.g;
				float bOffset = _ColorOffsetParameter.b;

				float strength = _ColorOffsetParameter.w;

				float2 delta = RGBSplitUVOffset(IN.uv, strength);

				half4 radiaBlurCol = half4(0,0,0,1);

				radiaBlurCol.r = SAMPLE_TEXTURE2D(_RadiaBlurTex, sampler_RadiaBlurTex, IN.uv + delta * rOffset * 0.5 ).r;
				radiaBlurCol.g = SAMPLE_TEXTURE2D(_RadiaBlurTex, sampler_RadiaBlurTex, IN.uv + delta * gOffset       ).g;
				radiaBlurCol.b = SAMPLE_TEXTURE2D(_RadiaBlurTex, sampler_RadiaBlurTex, IN.uv + delta * bOffset * 2   ).b;

			#else
				half4 radiaBlurCol = SAMPLE_TEXTURE2D(_RadiaBlurTex, sampler_RadiaBlurTex, IN.uv);

			#endif

				radiaBlurCol = lerp(col, radiaBlurCol, saturate(alpha));
				col          = col * (1 - definition) + radiaBlurCol * definition;

                return col;
			}
			ENDHLSL
		}
	}
}