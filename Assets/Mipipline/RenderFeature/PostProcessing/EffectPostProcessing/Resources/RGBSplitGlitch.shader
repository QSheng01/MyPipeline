Shader "Post/Effects/RGBSplitGlitch"
{
    Properties
    {
    	_MainTex ("Main Tex", 2D) = "white"{}

		_Params("Params",Vector)=(0,0,0,0)
		_Params2("Params2",Vector)=(0,0,0,0)
    }

    	HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    	#include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

		TEXTURE2D(_MainTex);
    	SAMPLER(sampler_MainTex);

		CBUFFER_START(UnityPerMaterial)
	    uniform half4 _Params;
	    uniform half3 _Params2;
		CBUFFER_END

    	#define _Fading _Params.x
	    #define _Amount _Params.y
	    #define _Speed _Params.z
	    #define _CenterFading _Params.w

	    #define _AmountR _Params2.y
	    #define _AmountB _Params2.z

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

    	v2f VertDefault(v2input v)
        {
            v2f o;
        	o.positionCS=TransformObjectToHClip(v.positionOS.xyz);
        	o.uv=v.uv;

            return o;
        }

        half4 Frag_Horizontal(v2f i): SV_Target
	    {

	    	float2 uv = i.uv.xy;
	    	half time = _Time.x * 6 * _Speed;
	    	half splitAmount = (1.0 + sin(time)) * 0.5;
	    	splitAmount *= 1.0 + sin(time * 2) * 0.5;
	    	splitAmount = pow(splitAmount, 3.0);
	    	splitAmount *= 0.05;
	    	float distance = length(uv - float2(0.5, 0.5));
	    	splitAmount *=  _Fading * _Amount;
	    	splitAmount *= lerp(1, distance, _CenterFading);

	    	half colorR = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(uv.x + splitAmount * _AmountR, uv.y)).r;
	    	half3 sceneColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
	    	half colorB = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(uv.x - splitAmount * _AmountB, uv.y)).b;

	    	half3 splitColor = half3(colorR, sceneColor.g, colorB);
	    	half3 finalColor = lerp(sceneColor.rgb, splitColor, _Fading);

	    	return half4(finalColor, 1.0);

	    }

	    half4 Frag_Vertical(v2f i) : SV_Target
	    {

	    	float2 uv = i.uv.xy;
	    	half time = _Time.x * 6 * _Speed;
	    	half splitAmount = (1.0 + sin(time)) * 0.5;
	    	splitAmount *= 1.0 + sin(time * 2) * 0.5;
	    	splitAmount = pow(splitAmount, 3.0);
	    	splitAmount *= 0.05;
	    	float distance = length(uv - float2(0.5, 0.5));
	    	splitAmount *= _Fading * _Amount;
	    	splitAmount *= lerp(1, distance, _CenterFading);

	    	half colorR = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(uv.x , uv.y + splitAmount * _AmountR)).r;
	    	half3 sceneColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
	    	half colorB = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(uv.x, uv.y - splitAmount * _AmountB)).b;

	    	half3 splitColor = half3(colorR, sceneColor.g, colorB);
	    	half3 finalColor = lerp(sceneColor.rgb, splitColor, _Fading);

	    	return half4(finalColor, 1.0);

	    }

	    half4 Frag_Horizontal_Vertical(v2f i) : SV_Target
	    {

	    	float2 uv = i.uv.xy;
	    	half time = _Time.x * 6 * _Speed;
	    	half splitAmount = (1.0 + sin(time)) * 0.5;
	    	splitAmount *= 1.0 + sin(time * 2) * 0.5;
	    	splitAmount = pow(splitAmount, 3.0);
	    	splitAmount *= 0.05;
	    	float distance = length(uv - float2(0.5, 0.5));
	    	splitAmount *= _Fading * _Amount;
	    	splitAmount *= lerp(1, distance, _CenterFading);

	    	float splitAmountR = splitAmount * _AmountR;
	    	float splitAmountB = splitAmount * _AmountB;

	    	half colorR = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(uv.x + splitAmountR, uv.y + splitAmountR)).r;
	    	half3 sceneColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
	    	half colorB = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(uv.x - splitAmountB, uv.y - splitAmountB)).b;

	    	half3 splitColor = half3(colorR, sceneColor.g, colorB);
	    	half3 finalColor = lerp(sceneColor.rgb, splitColor, _Fading);

	    	return half4(finalColor, 1.0);

	    }

		ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			HLSLPROGRAM

			#pragma vertex VertDefault
			#pragma fragment Frag_Horizontal

			ENDHLSL

		}

		Pass
		{
			HLSLPROGRAM
			#pragma vertex VertDefault
			#pragma fragment Frag_Vertical

			ENDHLSL

		}

		Pass
		{
			HLSLPROGRAM

			#pragma vertex VertDefault
			#pragma fragment Frag_Horizontal_Vertical

			ENDHLSL

		}
	}
}
