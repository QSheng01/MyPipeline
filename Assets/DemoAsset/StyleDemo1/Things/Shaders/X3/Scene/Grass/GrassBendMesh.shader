Shader "ShiYue/URP/Grass/Grass Bend Mesh"
{
	Properties
	{
		_Params("Parameters", vector) = (1,0,0,0)
	}

	HLSLINCLUDE
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

	CBUFFER_START(UnityPerMaterial)
	float4 _Params;
	CBUFFER_END

	struct Attributes
	{
		float3 positionOS : POSITION;
		float3 normalOS : NORMAL;
		float2 uv : TEXCOORD0;
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};

	struct Varyings
	{
		float4 positionCS : SV_POSITION;
		float3 positionWS : TEXCOORD0;
		float2 texcoord	  : TEXCOORD1;
		UNITY_VERTEX_INPUT_INSTANCE_ID
		UNITY_VERTEX_OUTPUT_STEREO
	};

	float3 GetPivotPos()
	{
		return float3(unity_ObjectToWorld[0].w, unity_ObjectToWorld[1].w, unity_ObjectToWorld[2].w);
	}

	Varyings vert(Attributes input)
	{
		Varyings output = (Varyings)0;

		UNITY_SETUP_INSTANCE_ID(input);
		UNITY_TRANSFER_INSTANCE_ID(input, output);
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

		input.positionOS *= _Params.w;

		float3 worldPos = TransformObjectToWorld(input.positionOS);	
		output.positionWS = worldPos;
		output.positionCS = TransformWorldToHClip(worldPos);
		output.texcoord = input.uv;

		return output;
	}

	half4 frag(Varyings input) : SV_Target
	{
		UNITY_SETUP_INSTANCE_ID(input);
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		// 获取模型的中心点世界坐标
		float3 center = GetPivotPos();
		// 当前点的坐标
		float3 worldPos = input.positionWS;
		float2 worldDir = normalize(worldPos.xz - center.xz);
		half2 uv = 2*(input.texcoord - 0.5);
		half strength = 1 - sqrt(dot(uv,uv));

		float2 dir = worldDir * 0.5 + 0.5;

		float height = ((input.positionWS.y) + _Params.y);	
	
		float mask = _Params.x * strength;
		//return float4(dir.x,  dir.y, 0, 1);
		// 根据这个点，到中心点的 x, y 坐标绘制成一张图
		return float4(dir.x, height, dir.y, mask);
	}
	ENDHLSL

	SubShader
	{
		Pass
		{
			Tags { "RenderType" = "GrassBender" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"}

			Blend Off
			ZWrite Off
			ZTest LEqual
			Cull Off

			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma multi_compile_instancing

			#pragma vertex vert
			#pragma fragment frag
			ENDHLSL
		}
		Pass
		{
			Tags { "RenderType" = "GrassBender" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"}

			Blend SrcAlpha  OneMinusSrcAlpha
			ZWrite Off
			ZTest LEqual
			Cull Off

			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma multi_compile_instancing

			#pragma vertex vert
			#pragma fragment frag
			ENDHLSL
		}
	}

	FallBack "Hidden/InternalErrorShader"
}