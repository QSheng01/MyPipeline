Shader "ShiYue/URP/PostProcessing/DualKawaseBlur"
{
	Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
    	[Hidden]_BlurRadius("模糊半径", Range(0,3.0)) = 1
    }
	
	HLSLINCLUDE
	
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
	
	TEXTURE2D (_MainTex);
	SAMPLER(sampler_MainTex);
	float4 _MainTex_TexelSize;
	float _BlurRadius;
 
	struct appdata
    {
		float4 positionOS: POSITION;
        float2 uv: TEXCOORD0;
    };
	
	struct v2f
	{
		float4 pos: SV_POSITION;
		float2 uv: TEXCOORD0;
	};
	
	struct v2f_DownSample
	{
		float4 positionCS: SV_POSITION;
		float2 uv: TEXCOORD0;
		float4 uv01: TEXCOORD1;
		float4 uv23: TEXCOORD2;
	};
	
	
	struct v2f_UpSample
	{
		float4 positionCS: SV_POSITION;
		float4 uv01: TEXCOORD0;
		float4 uv23: TEXCOORD1;
		float4 uv45: TEXCOORD2;
		float4 uv67: TEXCOORD3;
	};
	
	
	v2f_DownSample Vert_DownSample(appdata v)
	{
		v2f_DownSample o = (v2f_DownSample)0;
		o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
		float2 uv = v.uv;
		
		_MainTex_TexelSize *= 0.5;
		o.uv = uv;
		o.uv01.xy = uv - _MainTex_TexelSize * float2(1 + _BlurRadius, 1 + _BlurRadius);//top right
		o.uv01.zw = uv + _MainTex_TexelSize * float2(1 + _BlurRadius, 1 + _BlurRadius);//bottom left
		o.uv23.xy = uv - float2(_MainTex_TexelSize.x, -_MainTex_TexelSize.y) * float2(1 + _BlurRadius, 1 + _BlurRadius);//top left
		o.uv23.zw = uv + float2(_MainTex_TexelSize.x, -_MainTex_TexelSize.y) * float2(1 + _BlurRadius, 1 + _BlurRadius);//bottom right
		
		return o;
	}
	
	half4 Frag_DownSample(v2f_DownSample i): SV_Target
	{
		half4 sum = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * 4;
		sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv01.xy);
		sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv01.zw);
		sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv23.xy);
		sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv23.zw);
		
		return sum * 0.125;
	}
	
	
	v2f_UpSample Vert_UpSample(appdata v)
	{
		v2f_UpSample o;
		o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
		float2 uv = v.uv;
		
		_MainTex_TexelSize *= 0.5;
		_BlurRadius = 1 + _BlurRadius;
		
		o.uv01.xy = uv + float2(-_MainTex_TexelSize.x * 2, 0) * _BlurRadius;
		o.uv01.zw = uv + float2(-_MainTex_TexelSize.x, _MainTex_TexelSize.y) * _BlurRadius;
		o.uv23.xy = uv + float2(0, _MainTex_TexelSize.y * 2) * _BlurRadius;
		o.uv23.zw = uv + _MainTex_TexelSize.xy * _BlurRadius;
		o.uv45.xy = uv + float2(_MainTex_TexelSize.x * 2, 0) * _BlurRadius;
		o.uv45.zw = uv + float2(_MainTex_TexelSize.x, -_MainTex_TexelSize.y) * _BlurRadius;
		o.uv67.xy = uv + float2(0, -_MainTex_TexelSize.y * 2) * _BlurRadius;
		o.uv67.zw = uv - _MainTex_TexelSize * _BlurRadius;
		
		return o;
	}
	
	half4 Frag_UpSample(v2f_UpSample i): SV_Target
	{
		half4 sum = 0;
		sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv01.xy);
		sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv01.zw) * 2;
		sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv23.xy);
		sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv23.zw) * 2;
		sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv45.xy);
		sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv45.zw) * 2;
		sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv67.xy);
		sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv67.zw) * 2;
		
		return sum * 0.0833;
	}
	
	ENDHLSL
	
	SubShader
	{
		Cull Off ZWrite Off ZTest Always
		
		Pass
		{
			HLSLPROGRAM
			
			#pragma vertex Vert_DownSample
			#pragma fragment Frag_DownSample
			
			ENDHLSL
			
		}
		
		Pass
		{
			HLSLPROGRAM
			
			#pragma vertex Vert_UpSample
			#pragma fragment Frag_UpSample
			
			ENDHLSL
			
		}
	}
}


