Shader "Post/Effects/ColorAdjustment"
{
    Properties
    {
		_MainTex ("Texture", 2D) = "white" {}
		[HDR]_ColorFilter ("ColorFilter",Color)=(1,1,1,1)
    	_Saturation("Saturation",Range(-100,100))=0
    }

    SubShader
    {
    	Tags { "RenderPipeline"="UniversalPipeline" }
    	HLSLINCLUDE
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    	    #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

			CBUFFER_START(UnityPerMaterial)

            half4 _ColorFilter;
        	half _Saturation;
    		half _Contrast;
    		half _Brightness;

			CBUFFER_END
		ENDHLSL

        Pass {
			Name "ColorAdjustment"
			Tags
			{
				"LightMode"="UniversalForward"
			}
			Cull Off ZWrite Off ZTest Always

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

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

                OUT.positionCS	= TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv			= IN.uv;

                return OUT;
            }

			TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

			// Reference: https://github.com/EpicGames/UnrealEngine/blob/release/Engine/Shaders/Private/PostProcessCombineLUTs.usf
			float3 ColorCorrect(float3 color, float saturation, float contrast, float exposture)
			{
			    float luma = Luminance(color);
			    color = max(0, lerp(luma, color, saturation));
			    color = pow(color * (1.0 / 0.18), contrast) * 0.18;
			    color = color * pow(2.0, exposture);
			    return color;
			}

			// ACES tone mapping curve fit to go from HDR to LDR
			// Reference: https://knarkowicz.wordpress.com/2016/01/06/aces-filmic-tone-mapping-curve/
			float3 ACESFilm(float3 x)
			{
			    float a = 2.51f;
			    float b = 0.03f;
			    float c = 2.43f;
			    float d = 0.59f;
			    float e = 0.14f;
			    return clamp((x * (a * x + b)) / (x * (c * x + d) + e), 0.0, 1.0);
			}

            half4 frag(v2f IN) : SV_Target{

            	half3 originalColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).rgb;

            	half saturation=_Saturation/100.0+1.0;
            	half luma = GetLuminance(originalColor);
            	//finalColor= luma.xxx + (saturation ) * (finalColor - luma.xxx);

            	half3 finalColor = ColorCorrect(originalColor, _Saturation, _Contrast, _Brightness);
            	finalColor = finalColor * _ColorFilter.rgb;
            	return half4(finalColor, 1.0f);
            }

			ENDHLSL
		}
    }
}