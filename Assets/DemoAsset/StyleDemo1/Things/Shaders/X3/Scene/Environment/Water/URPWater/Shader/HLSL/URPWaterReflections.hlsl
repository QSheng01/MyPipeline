#ifndef URPWATER_REFlECTIONS_INCLUDED
#define URPWATER_REFlECTIONS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "URPWaterVariables.hlsl"
#include "URPWaterHelpers.hlsl"

half4 _ReflectionColor;
// 一级反射相关
half3 _FirstReflectionColor;
half _FirstReflectionTurnDark;
half _FirstReflectionLightRange;
half _FirstReflectionMostLight;

float3 SampleReflections(float3 worldNormal, float3 worldViewDir, float2 screenUV)
{
	float3 reflection = 0;
	float2 refOffset = 0;
 //    #if _REFLECTIONMODE_CUBEMAP
 //    	float3 reflectVector = reflect(-worldViewDir, worldNormal);
 //    	reflection = SAMPLE_TEXTURECUBE(_CubemapTexture, sampler_CubemapTexture, reflectVector).rgb;
 //    #elif _REFLECTIONMODE_PROBES
 //    	float3 reflectVector = reflect(-worldViewDir, worldNormal);
 //    	reflection = GlossyEnvironmentReflection(reflectVector, _ReflectionRoughness * 0.1, 1);
 //    #elif _REFLECTIONMODE_REALTIME
 //    
 //    	/*
 //    	// get the perspective projection
 //    	float2 p11_22 = float2(unity_CameraInvProjection._11, unity_CameraInvProjection._22) * 10;
 //    	// conver the uvs into view space by "undoing" projection
 //    	float3 viewDir = -(float3((screenUV * 2 - 1) / p11_22, -1));
 //    
 //    	half3 viewNormal = mul(worldNormal, (float3x3)GetWorldToViewMatrix()).xyz;
 //    	half3 reflectVector = reflect(-viewDir, viewNormal);
 //    	*/
 //    
 //    
 //    	float3 reflectVector = reflect(-worldViewDir, worldNormal);
 //    
 //    	float2 reflectionUV = screenUV + worldNormal.zx * float2(0.02, 0.15);
 //    	reflection = SAMPLE_TEXTURE2D_LOD(_ReflectionTexture, sampler_ScreenTextures_linear_clamp, reflectionUV, 6 * _ReflectionRoughness).rgb;//planar reflection
	// #elif _REFLECTIONMODE_PR
 //        float3 reflectVector = reflect(-worldViewDir, worldNormal);
 //        float2 reflectionUV = screenUV + worldNormal.zx * float2(0.02, 0.15);
 //        reflection = SAMPLE_TEXTURE2D(_PlanarReflectionTex, sampler_PlanarReflectionTex, reflectionUV); 
 //    #endif

	// #if _REFLECTIONMODE_CUBEMAP
	//     float3 reflectVector = reflect(-worldViewDir, worldNormal);
	//     reflection = SAMPLE_TEXTURECUBE(_CubemapTexture, sampler_CubemapTexture, reflectVector).rgb;
	// #elif _REFLECTIONMODE_PR	
	//     float3 reflectVector = reflect(-worldViewDir, worldNormal);
	//     float2 reflectionUV = screenUV + worldNormal.zx * float2(_PlanarReflectionDistortion.xx);
	//     reflection = SAMPLE_TEXTURE2D(_PlanarReflectionTex, sampler_PlanarReflectionTex, reflectionUV);
	// #elif _REFLECTIONMODE_SSPR
	    float2 offsets = worldNormal.xz*worldViewDir.y;
	    half4 skyboxcol = SAMPLE_TEXTURE2D(_SkyboxReflectionTex,sampler_SkyboxReflectionTex,screenUV+offsets); 
	    half3 reflDir = reflect(-worldViewDir, worldNormal);
	    half4 reflectionColor = SAMPLE_TEXTURE2D(_WaterReflectionTex,sampler_WaterReflectionTex,screenUV + worldNormal.xz * float2(_PlanarReflectionDistortion.xx));
	    float far = _ProjectionParams.z;
	    reflection = lerp(lerp(_DepthColor.rgb, skyboxcol.rgb, _SSPRLerpScale), reflectionColor.rgb, reflectionColor.a*1);
	// #endif

	reflection = reflection * _ReflectionColor;
	
	return reflection;
}


void ComputeReflections(inout GlobalData data, Varyings IN) 
{

//#if _REFLECTIONMODE_CUBEMAP || _REFLECTIONMODE_PROBES || _REFLECTIONMODE_REALTIME || _REFLECTIONMODE_PR

	// Reflection Distortion
	float3 reflectionNormal = data.worldNormal;
	float3 n = IN.worldNormal;//float3(0, 1, 0);
	n.xz *= _ReflectionDistortion * 10;
	reflectionNormal = lerp(n, reflectionNormal, _ReflectionDistortion);

	// Fresnel Distortion
	#if _NORMAL_FADE_ON
	float fresnelFade = IN.paramsA.x * _ReflectionFresnelNormal;
	#else
	float fresnelFade = _ReflectionFresnelNormal;
	#endif

	float3 fresnelNormal = lerp(IN.worldNormal, data.worldNormal, fresnelFade);
	float fresnel = 1 - dot(fresnelNormal, data.worldViewDir);
	fresnel = pow(saturate(fresnel), _ReflectionFresnel);

	float3 reflection = SampleReflections(reflectionNormal, data.worldViewDir, data.screenUV.xy);

	float reflectionMask = _ReflectionIntensity;
	#if _FOAM_ON
	reflectionMask *= 1 - data.foamMask * 2;
	#endif
	//fresnel = smoothstep(0, _ReflectionFresnel, saturate(fresnel));
	
	float3 finalReflection = lerp(data.finalColor, saturate(reflection), fresnel * saturate(reflectionMask));

	#if _DOUBLE_SIDED_ON
	data.finalColor = lerp(data.finalColor, finalReflection, data.vFace);
	#else
	data.finalColor = finalReflection;
	#endif

//#endif
	data.finalColor += data.addLight;


	// 测试一下深水颜色叠加反射
	// rongqingmei 测试
	float3 depthReflectionNormal = lerp(data.worldNormal, IN.worldNormal, 0.8f);
	float3 reflectVector = reflect(-data.worldViewDir, depthReflectionNormal);
	float3 depthReflection = SAMPLE_TEXTURECUBE(_CubemapTexture, sampler_CubemapTexture, reflectVector).rgb;
	// 反射光的调整
	// depthReflection *= half4(0.7,0.5,0.3,1.0);
	depthReflection *= _FirstReflectionColor;
	// 继续调整反射光对水体的影响方式
	// depthReflection = (depthReflection - 0.6f) * 0.30f;
	depthReflection = (depthReflection - _FirstReflectionTurnDark) * _FirstReflectionLightRange;
	// 最亮的阈值
	// float mostLightValue = 0.4f;
	float mostLightValue = _FirstReflectionMostLight;
	depthReflection = depthReflection * step(depthReflection, mostLightValue) + mostLightValue * step(mostLightValue, depthReflection);
	depthReflection = lerp(0, depthReflection, (1-fresnel) * saturate(reflectionMask));
	data.finalColor += depthReflection;
	
}

#endif