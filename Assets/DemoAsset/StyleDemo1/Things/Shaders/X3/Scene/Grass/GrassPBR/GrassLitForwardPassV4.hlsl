#ifndef GRASSUNIVERSAL_FORWARD_LIT_PASS_INCLUDED
#define GRASSUNIVERSAL_FORWARD_LIT_PASS_INCLUDED

// #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/Grass/GrassPBR/GrassLighting.hlsl"
// #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/ScanZoneCore.hlsl"

TEXTURE2D(_TerrainNormal);SAMPLER(sampler_TerrainNormal);

half _SSAOIntensity;
half _SSAOWeight;
half4 _BottomColor;
half3 _NoiseColor0;
half3 _NoiseColor1;
half4 _BottomColor2;
half4 _BottomColor3;
half _FresnelRange;
half _FresnelIntensity;
half _FresnelBlendRange;
half _FresnelBlendLoc;
half _FresnelSingleRange;
half _FresnelSingleBlendRange;
half _FresnelSingleBlendLoc;
half _FresnelSingleIntensity;

////////////////////////////////////////////////////////////////////////////////
/// PBR lighting...
////////////////////////////////////////////////////////////////////////////////

void CustomFresnel(inout half4 finalColor, InputData inputData, Light mainLight, half positionOSY, half3 positionPiv)
{
    // 获取地形的Normal
    inputData.positionCS = TransformWorldToHClip(positionPiv);
    float4 screenPos = ComputeScreenPos(inputData.positionCS);
    float2 screenUV = screenPos.xy / screenPos.w;
    half3 terrainNormal = SAMPLE_TEXTURE2D(_TerrainNormal, sampler_TerrainNormal, screenUV).rgb;
    terrainNormal = terrainNormal * 2.0f - 1.0f;
    
    // 计算效果
    float3 ndir = terrainNormal; // 地表法线
    float3 vdir = normalize(inputData.viewDirectionWS);
    //点积结果
    float ndotv = saturate(1-dot(ndir,vdir));         //菲涅尔 1 - n dot v
    
    // 整体的高光菲尼尔
    float3 fresnel = pow(ndotv, _FresnelRange) * mainLight.shadowAttenuation * _FresnelIntensity;//inputData.bakedGI;
    // 配置过渡区大小，以及过渡区位置
    fresnel = lerp(0, saturate(fresnel), saturate((positionOSY - _FresnelBlendLoc) * _FresnelBlendRange));

    
    // rongqingmei测试
    // finalColor.rgb = fresnel;
    // return;
    
    finalColor.rgb += fresnel;
    // return;
    
    // 草的高光效果
    //归一化世界空间法线
    half3 worldNormal = normalize(terrainNormal);
    //归一化世界空间光照方向
    half3 worldLightDir = normalize(mainLight.direction);
    //获取归一化世界空间视角方向
    half3 worldViewDir = normalize(_WorldSpaceCameraPos.xyz - inputData.positionWS);;
    
    //使用Blinn-Phong模型计算高光反射
    //归一化视角方向和光照方向之和
    half3 halfDir = normalize(worldLightDir + worldViewDir);
    half3 spec = mainLight.color * mainLight.shadowAttenuation * pow(saturate(dot(worldNormal,halfDir)), _FresnelSingleRange);

    // phong模型
    // half3 reflectDir = normalize(reflect(-worldLightDir, worldNormal));
    // half3 spec = mainLight.color * mainLight.shadowAttenuation * pow(saturate(dot(reflectDir, worldViewDir)), _FresnelSingleRange);
    
    // 配置过渡区大小，以及过渡区位置
    spec = lerp(0, spec, saturate((positionOSY - _FresnelSingleBlendLoc) * _FresnelSingleBlendRange));
    // 强度
    spec *= _FresnelSingleIntensity;

    // rongqingmei测试
    // finalColor.rgb = spec;
    // return;
    
    finalColor.rgb += spec;
}



half4 GrassUniversalFragmentPBR(InputData inputData, SurfaceData surfaceData, float positionOSY, half3 positionPiv)
{
    #if defined(_SPECULARHIGHLIGHTS_OFF)
    bool specularHighlightsOff = true;
    #else
    bool specularHighlightsOff = false;
    #endif
    BRDFData brdfData;

    // NOTE: can modify "surfaceData"...
    InitializeBRDFData(surfaceData, brdfData);

    // 置空 occlusion
	surfaceData.occlusion = half4(1,1,1,1);
    
    // 计算PBR光照前，先把normal置空
    half realNormalWS = inputData.normalWS;
    inputData.normalWS = half3(0.0h,1.0h,0.0h);
				
    #if defined(DEBUG_DISPLAY)
    half4 debugColor;

    if (CanDebugOverrideOutputColor(inputData, surfaceData, brdfData, debugColor))
    {
        return debugColor;
    }
    #endif

    // Clear-coat calculation...
    BRDFData brdfDataClearCoat = CreateClearCoatBRDFData(surfaceData, brdfData);
    half4 shadowMask = CalculateShadowMask(inputData);
    // // hbao 修改 这里对 SSAO 做特殊处理，直接后面直接乘以 AO 来达到风格化的效果
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
	
	// 底部颜色权重，控制AO程度
	aoFactor.indirectAmbientOcclusion = lerp(aoFactor.indirectAmbientOcclusion, _SSAOIntensity, saturate(1.0f - positionOSY * _SSAOWeight + 0.0f));
	// return aoFactor.directAmbientOcclusion.rrrr;
	
    uint meshRenderingLayers = GetMeshRenderingLayer();
    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
	// return mainLight.shadowAttenuation.rrrr;
	
    //混合逐对象投影
    #if defined(_PEROBJECTSHADOW)
    half pershadow = SAMPLE_TEXTURE2D_X(_ScreenSpaceShadowmapTexture, sampler_ScreenSpaceShadowmapTexture, inputData.normalizedScreenSpaceUV).r;
    mainLight.shadowAttenuation = min(mainLight.shadowAttenuation, pershadow);
    #endif
    
	// 获取云的噪点图采样
	GlobalSimpleCloudShadow(mainLight.shadowAttenuation, inputData.positionWS, saturate(0.3h));
	
    // NOTE: We don't apply AO to the GI here because it's done in the lighting calculation below...
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

    LightingData lightingData = CreateLightingData(inputData, surfaceData);

    lightingData.giColor = GlobalIllumination(brdfData, brdfDataClearCoat, surfaceData.clearCoatMask,
                                              inputData.bakedGI, aoFactor.indirectAmbientOcclusion, inputData.positionWS,
                                              inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV);
#ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
#endif
    {
        lightingData.mainLightColor = LightingPhysicallyBased(brdfData, brdfDataClearCoat,
                                                              mainLight,
                                                              inputData.normalWS, inputData.viewDirectionWS,
                                                              surfaceData.clearCoatMask, specularHighlightsOff);
    }
    
    #if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_FORWARD_PLUS
    for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);

#ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
        {
            lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
                                                                          inputData.normalWS, inputData.viewDirectionWS,
                                                                          surfaceData.clearCoatMask, specularHighlightsOff);
        }
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);

#ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
        {
            lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
                                                                          inputData.normalWS, inputData.viewDirectionWS,
                                                                          surfaceData.clearCoatMask, specularHighlightsOff);
        }
    LIGHT_LOOP_END
    #endif

    #if defined(_ADDITIONAL_LIGHTS_VERTEX)
    lightingData.vertexLightingColor += inputData.vertexLighting * brdfData.diffuse;
    #endif

#if REAL_IS_HALF
    // Clamp any half.inf+ to HALF_MAX
    half4 finalColor = CalculateFinalColor(lightingData, surfaceData.alpha);
    finalColor = min(finalColor, HALF_MAX);
#else
    half4 finalColor = CalculateFinalColor(lightingData, surfaceData.alpha);
#endif

    // 自定义菲尼尔高光
    CustomFresnel(finalColor, inputData, mainLight, positionOSY, positionPiv);
    
    return finalColor;
}

			

#endif
