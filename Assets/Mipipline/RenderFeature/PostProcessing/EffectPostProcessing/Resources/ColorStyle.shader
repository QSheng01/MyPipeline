Shader "Post/Effects/ColorStyle"
{
    Properties
    {
		_MainTex ("Texture", 2D) = "white" {}

		_BlackAreaColor ("黑色部分颜色", Color) = (0,0,0,1)
    	_WhiteAreaColor ("白色部分颜色", Color) = (1,1,1,1)

    	_ColorStyleParameter ("颜色属性参数", Vector) = (0,0,0,1)
    }

    SubShader
    {
    	Tags { "RenderPipeline"="UniversalPipeline" }
    	HLSLINCLUDE
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    	    #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

			CBUFFER_START(UnityPerMaterial)
			half3 _BlackAreaColor;
            half3 _WhiteAreaColor;

    		float4 _ColorStyleParameter;

			CBUFFER_END
		ENDHLSL

        Pass {
			Name "ColorStyle"
			Tags
			{
				"LightMode"="UniversalForward"
			}
			Cull Off ZWrite Off ZTest Always

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_local _ _INVERT

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

            half4 frag(v2f IN) : SV_Target{

                half3 finalColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).rgb;;

            	float grayCenter      = _ColorStyleParameter.x;
            	float grayOffset      = _ColorStyleParameter.y;
            	float colorStyleALpha = _ColorStyleParameter.a;

            	float gray = Luminance(finalColor);

    			float left  = saturate(grayCenter - grayOffset);
    			float right = saturate(grayCenter + grayOffset);

    			gray = smoothstep(left, right, gray);

            	half3 blackWhiteColor=half3(0,0,0);

        #ifdef _INVERT
            	blackWhiteColor = lerp(_WhiteAreaColor, _BlackAreaColor, gray);
        #else
            	blackWhiteColor = lerp(_BlackAreaColor, _WhiteAreaColor, gray);
        #endif

                finalColor = lerp(finalColor, blackWhiteColor, colorStyleALpha);

                return half4(finalColor, 1.0);
            }

			ENDHLSL
		}
    }
}
