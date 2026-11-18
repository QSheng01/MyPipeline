#ifndef URPWATER_COMMON_INCLUDED
#define URPWATER_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "URPWaterVariables.hlsl"

Varyings vert(Attributes v)
{
	Varyings OUT;

	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_TRANSFER_INSTANCE_ID(v, OUT);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

	//ComputeWaves(v);

	float3 worldPos = TransformObjectToWorld(v.vertex.xyz);

	// #if _WORLD_UV
		v.texcoord.xy = worldPos.xz * 0.1;
	// #endif


	OUT.pos = TransformObjectToHClip(v.vertex.xyz);
	OUT.worldPos = TransformObjectToWorld(v.vertex.xyz);
	OUT.color = v.color;

	VertexNormalInputs vertexTBN = GetVertexNormalInputs(v.normal, v.tangent);

	OUT.normal = float4(vertexTBN.normalWS, worldPos.x);
	OUT.tangent = float4(vertexTBN.tangentWS, worldPos.y);
	OUT.bitangent = float4(vertexTBN.bitangentWS, worldPos.z);


#if _DISPLACEMENTMODE_GERSTNER
	OUT.worldNormal = OUT.normal.xyz;
#else
	OUT.worldNormal = OUT.normal.xyz;

#endif
	OUT.screenCoord = ComputeScreenPos(OUT.pos);
	OUT.screenCoord.z = ComputePixelDepth(worldPos); // ComputeEyeDepth


	//NormalMap UVa
	#if _NORMALSMODE_FLOWMAP
		OUT.texcoord.xy = v.texcoord.xy * _NormalMapATilings.xy;
		OUT.texcoord.zw = v.texcoord.xy * _FlowTiling.xy + _FlowTiling.zw;
	#else
		OUT.texcoord = DualAnimatedUV(v.texcoord, _NormalMapATilings, _NormalMapASpeeds);
	#endif

	#if _NORMALSMODE_DUAL || _FOAM_ON
		OUT.texcoord1 = float4(0, 0, 0, 0);
	#endif

	#if _NORMALSMODE_DUAL
		OUT.texcoord1.xy = AnimatedUV(v.texcoord.xy, _NormalMapBTilings.xy, _NormalMapBSpeeds.xy).xy;
	#endif

	// FoamUV
	#if _FOAM_ON
		OUT.texcoord1.zw = v.texcoord.xy * _FoamTiling.xy;
	#endif

	//UNITY_TRANSFER_FOG(OUT, OUT.pos);

	return OUT;
}

// FRONT_FACE_TYPE vFace : FRONT_FACE_SEMANTIC;

float4 frag(Varyings IN) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(IN);
	float3 positionCS = TransformWorldToHClip(IN.worldPos);
    float fogCoord = ComputeFogFactor(positionCS.z);
	GlobalData data;
	InitializeGlobalData(data, IN);
	ComputeNormals(data, IN);
	ComputeRefractionData(data);
	ComputeScattering(data, IN);

	// 测试焦散
	
	ComputeFoam(data, IN);
	
	ComputeLighting(data, IN);
	
	ComputeReflections(data, IN);
	
	ComputeAlpha(data, IN);
	float4 output;
	output.rgb = data.finalColor.rgb;
	output.a = 1;
	output.rgb = MixFog(output.rgb, fogCoord);
	return output;
}


#endif