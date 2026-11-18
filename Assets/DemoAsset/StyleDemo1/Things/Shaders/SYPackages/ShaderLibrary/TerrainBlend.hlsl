#ifndef UNIVERSAL_TERRAIN_BLEND_INCLUDED
#define UNIVERSAL_TERRAIN_BLEND_INCLUDED

TEXTURE2D(_TerrainColor); SAMPLER(sampler_TerrainColor);
TEXTURE2D_FLOAT(_TerrainDepth);  SAMPLER(sampler_TerrainDepth);
TEXTURE2D(_TerrainNormal); SAMPLER(sampler_TerrainNormal);

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/ScanZoneCore.hlsl"

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"
 
// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

/*#if defined (_TerrainBlend_ON)
    TEXTURE2D_ARRAY(_MainMap);	SAMPLER(sampler_MainMap);
	TEXTURE2D_ARRAY(_MaskMap);	SAMPLER(sampler_MaskMap);
	TEXTURE2D(_Control);		SAMPLER(sampler_Control);
    TEXTURE2D(_HeightMap);      SAMPLER(sampler_HeightMap);
#endif*/

/*void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{

}*/

void TerrainBlend(float3 worldPos,float4 screenPos,half terrainBlendHeight,inout half smoothness,inout half3 albedo,inout float3 normalWS)
{
	float distanceToCamera = distance(worldPos, _WorldSpaceCameraPos);
	if(distanceToCamera < 100)
	{
		float2 screenUV = screenPos.xy/screenPos.w;
		half4 terrain = SAMPLE_TEXTURE2D(_TerrainColor, sampler_TerrainColor, screenUV);
		half3 terrainNormal = SAMPLE_TEXTURE2D(_TerrainNormal, sampler_TerrainNormal, screenUV).rgb;
		terrainNormal = terrainNormal * 2 - 1;

		float terrainDepth = SAMPLE_TEXTURE2D_X(_TerrainDepth, sampler_TerrainDepth, screenUV).r;
		terrainDepth = LinearEyeDepth(terrainDepth,_ZBufferParams);
		half blend = saturate(abs(terrainDepth - screenPos.z) / (terrainBlendHeight * 5));
		ScanZone_SceneColor(terrain.rgb);
		smoothness = lerp(terrain.a,smoothness,blend);
		albedo = lerp(terrain.rgb,albedo, blend);
		normalWS = lerp(terrainNormal,normalWS, blend);
	}
}

// half3 SampleSH2(half3 normalWS)
// {
// 	// LPPV is not supported in Ligthweight Pipeline
// 	real4 SHCoefficients[7];
// 	SHCoefficients[0] = unity_SHAr2;
// 	SHCoefficients[1] = unity_SHAg2;
// 	SHCoefficients[2] = unity_SHAb2;
// 	SHCoefficients[3] = unity_SHBr2;
// 	SHCoefficients[4] = unity_SHBg2;
// 	SHCoefficients[5] = unity_SHBb2;
// 	SHCoefficients[6] = unity_SHC2;
//
// 	return max(half3(0, 0, 0), SampleSH9(SHCoefficients, normalWS));
// }

void TerrainBlendWithMetallic(float3 worldPos,float4 screenPos,half terrainBlendHeight, inout SurfaceData surfaceData, inout InputData inputData)
{
	float distanceToCamera = distance(worldPos, _WorldSpaceCameraPos);
	if(distanceToCamera < 100)
	{
		float2 screenUV = screenPos.xy/screenPos.w;
		half4 terrain = SAMPLE_TEXTURE2D(_TerrainColor, sampler_TerrainColor, screenUV);
		half4 terrainNormal = SAMPLE_TEXTURE2D(_TerrainNormal, sampler_TerrainNormal, screenUV);
		terrainNormal = (terrainNormal * 2 - 1);

		float terrainDepth = SAMPLE_TEXTURE2D_X(_TerrainDepth, sampler_TerrainDepth, screenUV).r;
		terrainDepth = LinearEyeDepth(terrainDepth,_ZBufferParams);
		half blend = saturate(abs(terrainDepth - screenPos.z) / (terrainBlendHeight));
		// ScanZone_SceneColor(terrain.rgb);
		
		surfaceData.smoothness = lerp(terrain.a ,surfaceData.smoothness, blend);
		surfaceData.albedo = lerp(terrain.rgb, surfaceData.albedo, blend );
		
		inputData.normalWS = lerp(terrainNormal.rgb, inputData.normalWS, blend);
		inputData.bakedGI = SampleSH(inputData.normalWS);
	}
}

#endif
