#ifndef SIMPLERADDITIONALlIGHT
#define SIMPLERADDITIONALlIGHT

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

#define Light2D 0
#define Light3D 1
#define MAX_VISIBLE_SIMPLELIGHTS  2

struct SimpleLight
{
	half3   color;
	half3   specular;
	half3	direction;
	half	distanceSqr;
	half    distanceAttenuation;	
};

struct SimpleBRDFData
{
	half3 specular;
	half perceptualRoughness;
	half roughness;
	half roughness2;

	half normalizationTerm;     
	half roughness2MinusOne;   
};

float4 _SimpleLightCount;
float4 _SimpleMainLightPosition;
float4 _SimpleMainLightColor;

float4 _SimpleLightPosition[MAX_VISIBLE_SIMPLELIGHTS];
float4 _SimpleLightColor[MAX_VISIBLE_SIMPLELIGHTS];
float4 _SimpleSpecularColor[MAX_VISIBLE_SIMPLELIGHTS];

uint GetSimpleLightCount()
{
	return floor(_SimpleLightCount.x);
}

half TransformWeight(half3 color)
{
	half brightness = min(0.5, max(color.r, max(color.g, color.b)));
	return saturate(rcp(1 + 5 * brightness));
}

half SimpleDistanceAttenuation(float distanceSqr, float lightRange)
{
	float lightRangeSqr = lightRange * lightRange;

	//线性衰减算法
	//float atten =  -saturate(distanceSqr / lightRangeSqr) + 1;
	//悬崖式衰减算法
	//float atten = rcp(saturate(distanceSqr / lightRangeSqr) + 1) / 0.5 - 1;
	//float atten = saturate(-log2((distanceSqr / lightRangeSqr) + 1) + 1);
	float atten = saturate(rcp(10 + 10 * (distanceSqr / lightRangeSqr)));

	return atten;
}

SimpleLight GetSimpleLight(uint i, float3 lightPositionWS, float3 positionWS, float range,float lightType)
{
	SimpleLight light;

	float flag = step(0.99, lightType);
	lightPositionWS.y *= flag;
	positionWS.y *= flag;

	float3 lightVector = lightPositionWS.xyz - positionWS;
	half distanceSqr = max(dot(lightVector, lightVector), HALF_MIN);
	half3 lightDirection = half3(lightVector * rsqrt(distanceSqr));

	light.color = _SimpleLightColor[i].rgb;
	light.specular = _SimpleSpecularColor[i].rgb;
	light.direction = lightDirection;
	light.distanceSqr = distanceSqr;
	light.distanceAttenuation = SimpleDistanceAttenuation(distanceSqr, range);

	return light;
}

SimpleLight GetSimpleMainLight()
{
	SimpleLight light;

	light.direction = _SimpleMainLightPosition.xyz;	
	light.color = _SimpleMainLightColor.rgb;
	light.specular = 1;
	light.distanceAttenuation = 1;
	light.distanceSqr = 1;

	return light;
}

half3 SimpleLightingFinalColor(half3 lightColor, half3 lightDirectionWS,half lightAttenuation, half3 normalWS,
							   half3 diffuse, half3 specular, half occlusion)
{
	half NdotL = saturate(dot(normalWS, lightDirectionWS));
	return (diffuse + specular) * lightColor * lightAttenuation * occlusion;
}

half3 SimplerLightingSpecular(half3 lightColor, half3 lightDir, half3 normal, half3 viewDir, half specularGloss, half smoothness)
{
	float3 halfVec = SafeNormalize(float3(lightDir) + float3(viewDir));
	half NdotH = saturate(dot(normal, halfVec));
	half modifier = pow(NdotH, specularGloss);
	half3 specularReflection = modifier * smoothness;
	return lightColor * specularReflection;
}

void InitializeSimpleBRDFData(half3 albedo, half metallic, half3 specular, half smoothness, half alpha, out SimpleBRDFData outBRDFData)
{
	half oneMinusReflectivity = OneMinusReflectivityMetallic(metallic);
	half reflectivity = 1.0 - oneMinusReflectivity;
	outBRDFData.specular = lerp(kDieletricSpec.rgb, albedo, metallic);
	outBRDFData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(smoothness);
	outBRDFData.roughness = max(PerceptualRoughnessToRoughness(outBRDFData.perceptualRoughness), HALF_MIN);
	outBRDFData.roughness2 = outBRDFData.roughness * outBRDFData.roughness;

	outBRDFData.normalizationTerm = outBRDFData.roughness * 4.0h + 2.0h;
	outBRDFData.roughness2MinusOne = outBRDFData.roughness2 - 1.0h;
}

half3 SimpleDirectBDRF(SimpleBRDFData brdfData, half3 normalWS, half3 lightDirectionWS, half3 viewDirectionWS)
{
	float3 halfDir = SafeNormalize(float3(lightDirectionWS) + float3(viewDirectionWS));

	float NoH = saturate(dot(normalWS, halfDir));
	half LoH = saturate(dot(lightDirectionWS, halfDir));

	float d = NoH * NoH * brdfData.roughness2MinusOne + 1.00001f;

	half LoH2 = LoH * LoH;
	half specularTerm = brdfData.roughness2 / ((d * d) * max(0.1h, LoH2) * brdfData.normalizationTerm);

	specularTerm = specularTerm - HALF_MIN;
	specularTerm = clamp(specularTerm, 0.0, 100.0); // Prevent FP16 overflow on mobiles

	return specularTerm * brdfData.specular;
}

half3 SimpleDirectBDRF(BRDFData brdfData, half3 normalWS, half3 lightDirectionWS, half3 viewDirectionWS)
{
	float3 halfDir = SafeNormalize(float3(lightDirectionWS)+float3(viewDirectionWS));

	float NoH = saturate(dot(normalWS, halfDir));
	half LoH = saturate(dot(lightDirectionWS, halfDir));

	float d = NoH * NoH * brdfData.roughness2MinusOne + 1.00001f;

	half LoH2 = LoH * LoH;
	half specularTerm = brdfData.roughness2 / ((d * d) * max(0.1h, LoH2) * brdfData.normalizationTerm);

	specularTerm = specularTerm - HALF_MIN;
	specularTerm = clamp(specularTerm, 0.0, 100.0); // Prevent FP16 overflow on mobiles

	return specularTerm * brdfData.specular;
}

half3 SampleSimpleLightmap(float2 lightmapUV, half3 normalWS)
{
	half4 decodeInstructions = half4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0h, 0.0h);
	half4 transformCoords = half4(1, 1, 0, 0);

	lightmapUV = lightmapUV * transformCoords.xy + transformCoords.zw;
	float4 encodedIlluminance = SAMPLE_TEXTURE2D(unity_Lightmap, samplerunity_Lightmap, lightmapUV).rgba;
	float3 illuminance = DecodeLightmap(encodedIlluminance, decodeInstructions);

	return illuminance;
}

#ifdef LIGHTMAP_ON
#define NEW_SAMPLE_GI(lmName, shName, normalWSName) SampleSimpleLightmap(lmName, normalWSName)
#else
#define NEW_SAMPLE_GI(lmName, shName, normalWSName) SampleSHPixel(shName, normalWSName)
#endif

half3 SimpleLighting(float3 positionWS, float3 normalWS, half3 albedo, half3 specular, half occlusion, half3 emission, half lightType)
{
	half3 color = albedo;

#ifdef _SIMPLE_ADDITIONAL_LIGHT
	uint customLightCount = GetSimpleLightCount();
	for (uint customLightItr = 0; customLightItr < customLightCount; customLightItr++)
	{
		float3 lightPositionWS = _SimpleLightPosition[customLightItr].xyz;
		float lightRange = _SimpleLightPosition[customLightItr].w;
		SimpleLight customLight = GetSimpleLight(customLightItr, lightPositionWS, positionWS, lightRange, lightType);
		color += SimpleLightingFinalColor(customLight.color, customLight.direction, customLight.distanceAttenuation, normalWS, albedo, 0, occlusion * TransformWeight(albedo));
	}
#endif

	return color;
}

half3 SimpleLighting(InputData inputData, half3 albedo, half3 specular, half occlusion, half3 emission, half lightType)
{
	return SimpleLighting(inputData.positionWS, inputData.normalWS, albedo, specular, occlusion, emission, lightType);
}

half3 SimpleLightingSpecular(BRDFData brdfData, float3 positionWS, float3 normalWS, half3 viewDirectionWS, half3 albedo, half occlusion, half lightType)
{
	half3 color = 0;
	uint customLightCount = GetSimpleLightCount();
	for (uint customLightItr = 0; customLightItr < customLightCount; customLightItr++)
	{
		float3 lightPositionWS = _SimpleLightPosition[customLightItr].xyz;
		float lightRange = _SimpleLightPosition[customLightItr].w;

		SimpleLight customLight = GetSimpleLight(customLightItr, lightPositionWS, positionWS, lightRange, lightType);

		float3 lightVector = lightPositionWS.xyz - positionWS;
		half distanceSqr = max(dot(lightVector, lightVector), HALF_MIN);
		half3 lightDirection = half3(lightVector * rsqrt(distanceSqr));

		half3 specular = SimpleDirectBDRF(brdfData, normalWS, lightDirection, viewDirectionWS);
		color += SimpleLightingFinalColor(customLight.color, customLight.direction, customLight.distanceAttenuation, normalWS, albedo, specular, occlusion * TransformWeight(albedo));
	}

	return color;
}

half3 SimpleLightingPBR(InputData inputData, half3 albedo, half metallic, half smoothness, half3 specular,half occlusion, half3 emission, float lightType)
{	
	BRDFData brdfData;
	half alpha = 1;
	InitializeBRDFData(albedo, metallic, specular, smoothness, alpha, brdfData);

	Light mainLight = GetMainLight(inputData.shadowCoord);
	MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

	half3 color = brdfData.diffuse * inputData.bakedGI;
	color += LightingPhysicallyBased(brdfData, mainLight, inputData.normalWS, inputData.viewDirectionWS);

#ifdef _ADDITIONAL_LIGHTS
	uint pixelLightCount = GetAdditionalLightsCount();
	for (uint i = 0u; i < pixelLightCount; ++i)
	{
		Light light = GetAdditionalLight(i, inputData.positionWS);
		color += LightingPhysicallyBased(brdfData, light, inputData.normalWS, inputData.viewDirectionWS);
	}
#endif

#ifdef _SIMPLE_ADDITIONAL_LIGHT
	color += SimpleLightingSpecular(brdfData, inputData.positionWS, inputData.normalWS, inputData.viewDirectionWS, color, occlusion, lightType);
#endif 

	return color;
}

//Blinn-phong shading with SH
half3 SimpleLighting(Light light, half3 normalWS, half3 viewDirectionWS, half3 bakedGI, half3 albedo)
{
	half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);

	half3 color = (bakedGI + LightingLambert(attenuatedLightColor, light.direction, normalWS)) * albedo;

	return color;
}

// General function to apply lighting based on the configured mode
half3 ApplyLighting(SurfaceData surfaceData, Light mainLight, half3 vertexLight, half3 bakedGI, half3 normalWS, half3 positionWS,half4 shadowMask)
{
	half3 viewDirectionWS = SafeNormalize(GetCameraPositionWS() - positionWS);

#if defined _PBR_LIGHTING
	BRDFData brdfData;
	InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, 0, surfaceData.smoothness, surfaceData.alpha, brdfData);

	half3 color = GlobalIllumination(brdfData, bakedGI, surfaceData.occlusion, normalWS, viewDirectionWS);
	color += LightingPhysicallyBased(brdfData, mainLight, normalWS, viewDirectionWS);
#else
	MixRealtimeAndBakedGI(mainLight, normalWS, bakedGI);
	half3 color = SimpleLighting(mainLight, normalWS, viewDirectionWS, bakedGI, surfaceData.albedo);
	
#endif

#if _ADDITIONAL_LIGHTS
	#if defined _PBR_LIGHTING
		//BRDFData brdfData;
		InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, 0, surfaceData.smoothness, surfaceData.alpha, brdfData);
	#endif

	uint additionalLightsCount = GetAdditionalLightsCount();
	for (uint i = 0u; i < additionalLightsCount; ++i)
	{
		Light light = GetAdditionalLight(i, positionWS, shadowMask);
		#if defined _PBR_LIGHTING
			color += LightingPhysicallyBased(brdfData, light, normalWS, viewDirectionWS);
		#else
			color += SimpleLighting(light, normalWS, viewDirectionWS, 0, surfaceData.albedo);
		#endif
	}
#endif

#ifdef _ADDITIONAL_LIGHTS_VERTEX
	color += vertexLight;
#endif

	color += surfaceData.emission;

	return color;
}

half3 GrassPBRLighting(SurfaceData surfaceData, Light mainLight, half3 bakedGI,half3 specularNormal, half3 diffuseNormal, half3 positionWS,half specularStrength)
{
	half3 viewDirectionWS = SafeNormalize(GetCameraPositionWS() - positionWS);

	BRDFData brdfData;
	InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, 0, surfaceData.smoothness, surfaceData.alpha, brdfData);	

	half oneMinusReflectivity = OneMinusReflectivityMetallic(0);
	brdfData.diffuse = oneMinusReflectivity * surfaceData.albedo;

	half3 specular = DirectBDRF(brdfData,specularNormal, mainLight.direction,viewDirectionWS) * mainLight.distanceAttenuation * mainLight.shadowAttenuation * specularStrength;

	MixRealtimeAndBakedGI(mainLight, diffuseNormal, bakedGI);
	half3 diffuse = SimpleLighting(mainLight, diffuseNormal, viewDirectionWS, bakedGI, surfaceData.albedo) ;

	half3 color = (specular > diffuse) ? specular : diffuse;

#if _ADDITIONAL_LIGHTS
	#if defined _PBR_LIGHTING
		//BRDFData brdfData;
		InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, 0, surfaceData.smoothness, surfaceData.alpha, brdfData);
	#endif

	uint additionalLightsCount = GetAdditionalLightsCount();
	for (uint i = 0u; i < additionalLightsCount; ++i)
	{
		Light light = GetAdditionalLight(i, positionWS);
		#if defined _PBR_LIGHTING
			color += LightingPhysicallyBased(brdfData, light, diffuseNormal, viewDirectionWS);
		#else
			color += SimpleLighting(light, diffuseNormal, viewDirectionWS, 0, surfaceData.albedo);
		#endif
	}
#endif

#ifdef _ADDITIONAL_LIGHTS_VERTEX
	color += vertexLight;
#endif

	color += surfaceData.emission;

	return color;
}

#endif
