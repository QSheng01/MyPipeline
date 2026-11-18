#ifndef UNIVERSAL_DETAILPROPSSNOW_INPUT_INCLUDED
#define UNIVERSAL_DETAILPROPSSNOW_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

CBUFFER_START(UnityPerMaterial)
half4 _BaseMap_ST;
half _BaseSmoothness;
half _BaseAOIntensity;
half _BumpScale;
half4 _BaseColor;
half _TopNoiseUVScale;
half _TopSmoothness;
half _TopUVScale;
half _TopIntensity;
half _TopOffset;
half _TopContrast;
half _TopNormal_Intensity;
half4 _TopColor;
half4 _EmissionColor;
half4 _DissloveEdgeColor;
half4 _DissloveTex_ST;
half _DissloveThreshold;
half _DissloveEdgeLength;
half _EmissionAndAlbedoBlender;
half _DetailUVScale;
half _DetailAlbedoIntensity;
half _DetailNormalMapIntensity;
// half _TerrainBlendIntensity;
half _EnvironmentReflectionsStrength;
half _Cutoff;
half _BaseMetallic;
half _FogClose;
half _TerrainBlendHeight;
half4 _EffectFresnelColor;
half _EffectFresnelFactor;
half _EffectFresnelPower;
half _EffectFresnelThreshold;
half _EffectFresnelSmooth;
CBUFFER_END

TEXTURE2D(_BaseAlbedoASmoothness);        SAMPLER(sampler_BaseAlbedoASmoothness);
TEXTURE2D(_BaseNormalMap);
// SAMPLER(sampler_BaseNormalMap);
TEXTURE2D(_BaseAOANoiseMask);
// SAMPLER(sampler_BaseAOANoiseMask);
TEXTURE2D(_TopAlbedoASmoothness);        SAMPLER(sampler_TopAlbedoASmoothness);
TEXTURE2D(_TopNormalMap);        SAMPLER(sampler_TopNormalMap);
TEXTURE2D(_DetailAlbedo);        SAMPLER(sampler_DetailAlbedo);
TEXTURE2D(_DetailNormalMap);        SAMPLER(sampler_DetailNormalMap);
TEXTURE2D(_NormalObj);           SAMPLER(sampler_NormalObj);
TEXTURE2D(_Tangent);           SAMPLER(sampler_Tangent);

#if _EFFECTFRESNEL_ON
TEXTURE2D(_EffectMaskMap);
SAMPLER(sampler_EffectMaskMap);
#endif
// Returns clear coat parameters
// .x/.r == mask
// .y/.g == smoothness
half2 SampleClearCoat(float2 uv)
{
#if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
    half2 clearCoatMaskSmoothness = half2(_ClearCoatMask, _ClearCoatSmoothness);

#if defined(_CLEARCOATMAP)
    clearCoatMaskSmoothness *= SAMPLE_TEXTURE2D(_ClearCoatMap, sampler_ClearCoatMap, uv).rg;
#endif

    return clearCoatMaskSmoothness;
#else
    return half2(0.0, 1.0);
#endif  // _CLEARCOAT
}

float3 Unity_Blend_Overlay_float4(float3 Base, float3 Blend, float Opacity)
{
    float3 result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
    float3 result2 = 2.0 * Base * Blend;
    float3 zeroOrOne = step(Base, 0.5);
    float3 Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
    Out = lerp(Base, Out, Opacity);
    return Out;
}

float3 NormalTangentToWorld (float3 normalTS, float3 normalWS, float4 tangentWS, float3 bitangentWS) {
    float3x3 tangentToWorld = float3x3(tangentWS.xyz,bitangentWS.xyz,normalWS);
        //CreateTangentToWorld(normalWS, tangentWS.xyz, tangentWS.w);
    return TransformTangentToWorld(normalTS, tangentToWorld);
}

half remap(half x, half t1, half t2, half s1, half s2)
{
    return (x - t1) / (t2 - t1) * (s2 - s1) + s1;
}


half3 SampleNormal3(float2 uv, TEXTURE2D_PARAM(bumpMap, sampler_bumpMap), half scale = 1.0h)
{
    #ifdef _NORMALMAP
    half4 n =SAMPLE_TEXTURE2D(bumpMap, sampler_bumpMap, uv);
    n.b=1;
    // #if BUMP_SCALE_NOT_SUPPORTED
    // return UnpackNormal(n);
    //#else
    return UnpackNormalScale(n, scale);
    ///#endif
    #else
    return half3(0.0h, 0.0h, 1.0h);
    #endif
}

half3 NormalBlend(half3 normal01, half3 normal02)
{
    return normalize(half3(normal01.xy * normal02.z + normal02.xy * normal01.z, normal01.z * normal02.z));
}
//DYF 直接从另一个shader中搬过来的，以后这种好多地方都用到的需求可以整合到一个文件中

inline void GetEffectFresnelColor_Scene(float2 uv, float NdotV, inout half3 finalColor)
{//特效边缘光计算
    #if _EFFECTFRESNEL_ON
    half3 effectFresnelColor = 0;
    
    half effectFresnelMask = SAMPLE_TEXTURE2D(_EffectMaskMap, sampler_EffectMaskMap, uv).r;
    
    half effectFresnel = pow(1 - NdotV, _EffectFresnelPower);
    half effectFresnelvalue = smoothstep(_EffectFresnelThreshold, _EffectFresnelThreshold + _EffectFresnelSmooth, effectFresnel);
    effectFresnelColor = _EffectFresnelColor.rgb * effectFresnelvalue * _EffectFresnelFactor;
    effectFresnelColor *= effectFresnelMask;

    finalColor += effectFresnelColor;
    #endif
}
inline void InitializeDetailPropsSnowSurfaceData(float2 uv,float4 color, float3 normalWS, float4 tangentWS, float3 bitangentWS, out SurfaceData outSurfaceData)
{
    // outSurfaceData.alpha = 1;
    half4 baseAlbedo = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseAlbedoASmoothness, sampler_BaseAlbedoASmoothness)) * _BaseColor;
    half3 baseNormal = SampleNormal3(uv, TEXTURE2D_ARGS(_BaseNormalMap, sampler_BaseAlbedoASmoothness), _BumpScale);
    half4 basePBRInf = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseAOANoiseMask, sampler_BaseAlbedoASmoothness));
    half baseAO = lerp(1, basePBRInf.b, _BaseAOIntensity);
    half baseSmoothness = basePBRInf.a + remap(_BaseSmoothness, 0, 1, -1, 1);
    half4 detailAlbedo = SampleAlbedoAlpha(uv * _DetailUVScale, TEXTURE2D_ARGS(_DetailAlbedo, sampler_DetailAlbedo)) * 2;
    half3 detailNormal = SampleNormal3(uv * _DetailUVScale, TEXTURE2D_ARGS(_DetailNormalMap, sampler_DetailNormalMap), _DetailNormalMapIntensity);
    half4 topAlbedo = SampleAlbedoAlpha(uv * _TopUVScale, TEXTURE2D_ARGS(_TopAlbedoASmoothness, sampler_TopAlbedoASmoothness)) * _TopColor;
    half3 topNormal = SampleNormal3(uv * _TopUVScale, TEXTURE2D_ARGS(_TopNormalMap, sampler_TopNormalMap), _TopNormal_Intensity);
    half topSmoothness = topAlbedo.a * _TopSmoothness;
    
    //half topSmoothness=topAlbedo.a + (_TopSmoothness-0.5)*2;//remap(_TopSmoothness,0,1,-1,1)
    
    half topAO = 1;
    half topfactor = abs(saturate(NormalTangentToWorld(baseNormal, normalWS, tangentWS, bitangentWS).g) + _TopOffset);
    topfactor = saturate(pow(topfactor,_TopContrast * 19 + 1) * _TopIntensity);
    
    baseNormal = NormalBlend(baseNormal, detailNormal);
    //topNormal = NormalBlend(topNormal, baseNormal2);
    half3 finalNormal = baseNormal;
    half finalSmoothness = baseSmoothness;
    
    baseAlbedo.rgb = lerp(baseAlbedo, Unity_Blend_Overlay_float4(baseAlbedo,detailAlbedo,1), _DetailAlbedoIntensity);
    
    half4 finalAlbedo;
    #if defined(_OpenRGBCombineTop)
    _TopContrast*=0.5;
    half albedoLerp = smoothstep(0+_TopContrast,1-_TopContrast,baseAlbedo.a);
    finalAlbedo = lerp(baseAlbedo, topAlbedo, albedoLerp);
    finalNormal = lerp(baseNormal,topNormal,baseAlbedo.a);
    finalSmoothness = lerp(baseSmoothness,topSmoothness,baseAlbedo.a);
    #else
    finalAlbedo = lerp(baseAlbedo, topAlbedo, topfactor * topAO);
    finalNormal = lerp(baseNormal,topNormal,topfactor);
    finalSmoothness = lerp(baseSmoothness,topSmoothness,topfactor);
    #endif
    
    half finalAO = lerp(baseAO, lerp(baseAO,1,0.8), topfactor);

    outSurfaceData.albedo = finalAlbedo.rgb;
    outSurfaceData.alpha = baseAlbedo.a;
    AlphaDiscard(outSurfaceData.alpha, _Cutoff);
    
    outSurfaceData.metallic = basePBRInf.r * _BaseMetallic;
    outSurfaceData.specular = half3(0.0h, 0.0h, 0.0h);

    outSurfaceData.smoothness = finalSmoothness;
    outSurfaceData.normalTS = finalNormal;
    outSurfaceData.occlusion = finalAO.r;
    outSurfaceData.emission = lerp(_EmissionColor.xyz, _EmissionColor.xyz * finalAlbedo.rgb, _EmissionAndAlbedoBlender) * basePBRInf.g;
    
    

#if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
    half2 clearCoat = SampleClearCoat(uv);
    outSurfaceData.clearCoatMask       = clearCoat.r;
    outSurfaceData.clearCoatSmoothness = clearCoat.g;
#else
    outSurfaceData.clearCoatMask       = 0.0h;
    outSurfaceData.clearCoatSmoothness = 0.0h;
#endif
}
inline void InitializeDetailPropsSnowSurfaceDatalod1(float2 uv,float4 color, float3 normalWS, float4 tangentWS, float3 bitangentWS, out SurfaceData outSurfaceData)
{
    outSurfaceData.alpha = 1;
    half4 baseAlbedo = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseAlbedoASmoothness, sampler_BaseAlbedoASmoothness)) * _BaseColor;
    half3 baseNormal = SampleNormal3(uv, TEXTURE2D_ARGS(_BaseNormalMap, sampler_BaseAlbedoASmoothness), _BumpScale);
    half4 basePBRInf = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseAOANoiseMask, sampler_BaseAlbedoASmoothness));
    half baseAO = lerp(1, basePBRInf.b, _BaseAOIntensity);
    half baseSmoothness = basePBRInf.a + remap(_BaseSmoothness, 0, 1, -1, 1);
    half4 detailAlbedo = SampleAlbedoAlpha(uv * _DetailUVScale, TEXTURE2D_ARGS(_DetailAlbedo, sampler_DetailAlbedo)) * 2;
    half3 detailNormal = SampleNormal3(uv * _DetailUVScale, TEXTURE2D_ARGS(_DetailNormalMap, sampler_DetailNormalMap), _DetailNormalMapIntensity);
    half4 topAlbedo = SampleAlbedoAlpha(uv * _TopUVScale, TEXTURE2D_ARGS(_TopAlbedoASmoothness, sampler_TopAlbedoASmoothness)) * _TopColor;
    half3 topNormal = SampleNormal3(uv * _TopUVScale, TEXTURE2D_ARGS(_TopNormalMap, sampler_TopNormalMap), _TopNormal_Intensity);
    half topSmoothness = topAlbedo.a * _TopSmoothness;
    
    //half topSmoothness=topAlbedo.a + (_TopSmoothness-0.5)*2;//remap(_TopSmoothness,0,1,-1,1)
    
    half topAO = 1;
    half topfactor = abs(saturate(NormalTangentToWorld(baseNormal, normalWS, tangentWS, bitangentWS).g) + _TopOffset);
    topfactor = saturate(pow(topfactor,_TopContrast * 19 + 1) * _TopIntensity);
    
    baseNormal = NormalBlend(baseNormal, detailNormal);
    //topNormal = NormalBlend(topNormal, baseNormal2);
    half3 finalNormal = baseNormal;
    half finalSmoothness = baseSmoothness;
    
    baseAlbedo.rgb = lerp(baseAlbedo, Unity_Blend_Overlay_float4(baseAlbedo,detailAlbedo,1), _DetailAlbedoIntensity);
    
    half4 finalAlbedo;
    #if defined(_OpenRGBCombineTop)
    _TopContrast*=0.5;
    half albedoLerp = smoothstep(0+_TopContrast,1-_TopContrast,baseAlbedo.a);
    finalAlbedo = lerp(baseAlbedo, topAlbedo, albedoLerp);
    finalNormal = lerp(baseNormal,topNormal,baseAlbedo.a);
    finalSmoothness = lerp(baseSmoothness,topSmoothness,baseAlbedo.a);
    #else
    finalAlbedo = lerp(baseAlbedo, topAlbedo, topfactor * topAO);
    finalNormal = lerp(baseNormal,topNormal,topfactor);
    finalSmoothness = lerp(baseSmoothness,topSmoothness,topfactor);
    #endif
    
    half finalAO = lerp(baseAO, lerp(baseAO,1,0.8), topfactor);

    outSurfaceData.albedo = finalAlbedo.rgb;
    outSurfaceData.metallic = basePBRInf.r * _BaseMetallic;
    outSurfaceData.specular = half3(0.0h, 0.0h, 0.0h);

    outSurfaceData.smoothness = finalSmoothness;
    outSurfaceData.normalTS = finalNormal;
    outSurfaceData.occlusion = finalAO.r;
    outSurfaceData.emission = basePBRInf.g * _EmissionColor.xyz;

#if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
    half2 clearCoat = SampleClearCoat(uv);
    outSurfaceData.clearCoatMask       = clearCoat.r;
    outSurfaceData.clearCoatSmoothness = clearCoat.g;
#else
    outSurfaceData.clearCoatMask       = 0.0h;
    outSurfaceData.clearCoatSmoothness = 0.0h;
#endif
}


inline void InitializeDetailPropsSnowSurfaceData_LOD0(float2 uv,float4 color, float3 normalWS, float4 tangentWS, float3 bitangentWS, out SurfaceData outSurfaceData)
{
    outSurfaceData.alpha = 1;
    half4 baseAlbedo = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseAlbedoASmoothness, sampler_BaseAlbedoASmoothness));
    half3 baseNormal = SampleNormal3(uv, TEXTURE2D_ARGS(_BaseNormalMap, sampler_BaseAlbedoASmoothness), _BumpScale * 2);
    half4 basePBRInf = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseAOANoiseMask, sampler_BaseAlbedoASmoothness));
    
    half baseAO = lerp(1, basePBRInf.b, _BaseAOIntensity);
    half baseSmoothness = basePBRInf.a + remap(_BaseSmoothness, 0, 1, -1, 1);
    
    half3 detailNormal = SampleNormal3(uv * _DetailUVScale, TEXTURE2D_ARGS(_DetailNormalMap, sampler_DetailNormalMap), _DetailNormalMapIntensity);
    
    half topAO = 1;
    
    half topfactor = abs(saturate(NormalTangentToWorld(baseNormal, normalWS, tangentWS, bitangentWS).g) + _TopOffset);
    topfactor = saturate(pow(topfactor,_TopContrast * 19 + 1) * _TopIntensity);

    half finalAO = lerp(baseAO, lerp(baseAO,1,0.8), topfactor);

    // half finalSmoothness=lerp(baseSmoothness,topSmoothness,topfactor);

    outSurfaceData.albedo = baseAlbedo.rgb;
    outSurfaceData.metallic = basePBRInf.r * _BaseMetallic;
    outSurfaceData.specular = half3(0.0h, 0.0h, 0.0h);

    outSurfaceData.smoothness = baseSmoothness;
    outSurfaceData.normalTS = baseNormal;
    outSurfaceData.occlusion = finalAO.r;
    outSurfaceData.emission = basePBRInf.g * _EmissionColor.xyz;

#if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
    half2 clearCoat = SampleClearCoat(uv);
    outSurfaceData.clearCoatMask       = clearCoat.r;
    outSurfaceData.clearCoatSmoothness = clearCoat.g;
#else
    outSurfaceData.clearCoatMask       = 0.0h;
    outSurfaceData.clearCoatSmoothness = 0.0h;
#endif
}



inline void InitializeDetailPropsSnowSurfaceDataLOD1(float2 uv,float4 color, float3 normalWS, float4 tangentWS, float3 bitangentWSout, SurfaceData outSurfaceData)
{
    outSurfaceData.alpha=1;
    half4 baseAlbedo= SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseAlbedoASmoothness, sampler_BaseAlbedoASmoothness));
    half3 baseNormal = SampleNormal3(uv, TEXTURE2D_ARGS(_BaseNormalMap, sampler_BaseAlbedoASmoothness), 1);
    half3 baseNormal2 = SampleNormal3(uv, TEXTURE2D_ARGS(_BaseNormalMap, sampler_BaseAlbedoASmoothness), _BumpScale*0.5);
    half baseAO=lerp(1, SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseAOANoiseMask, sampler_BaseAlbedoASmoothness)).b,_BaseAOIntensity); 
    half baseSmoothness=baseAlbedo.a + remap(_BaseSmoothness,0,1,-1,1);
    
    half4 detailAlbedo = SampleAlbedoAlpha(uv*_DetailUVScale, TEXTURE2D_ARGS(_DetailAlbedo, sampler_DetailAlbedo))*2;
    half3 detailNormal = SampleNormal3(uv*_DetailUVScale, TEXTURE2D_ARGS(_DetailNormalMap, sampler_DetailNormalMap), _DetailNormalMapIntensity);
    
    half4 topAlbedo=SampleAlbedoAlpha(uv*_TopUVScale, TEXTURE2D_ARGS(_TopAlbedoASmoothness, sampler_TopAlbedoASmoothness))*_TopColor;
    //half3 topNormal = SampleNormal3(uv*_TopUVScale, TEXTURE2D_ARGS(_TopNormalMap, sampler_TopNormalMap), _TopNormal_Intensity);
    
    half topAO=1;
    // #if defined(_TOPENABLENOISE_ON)
    // topAO=1-SampleAlbedoAlpha(uv*_TopNoiseUVScale, TEXTURE2D_ARGS(_BaseAOANoiseMask, sampler_BaseAOANoiseMask)).a;
    // #endif
    half topSmoothness=topAlbedo.a+remap(_TopSmoothness,0,1,-1,1);
    
    half topfactor=abs(saturate(NormalTangentToWorld(baseNormal, normalWS,tangentWS, bitangentWSout).g) + _TopOffset);
    topfactor=saturate(pow(topfactor,_TopContrast * 19 + 1) * _TopIntensity);

    //baseNormal=BlendNormal(baseNormal,detailNormal);
    //topNormal = BlendNormal(topNormal, baseNormal2);
    half3 finalNormal = normalize(lerp(baseNormal, baseNormal2, topfactor * topAO));
    
    baseAlbedo.rgb = lerp(baseAlbedo, Unity_Blend_Overlay_float4(baseAlbedo,detailAlbedo,1),_DetailAlbedoIntensity);
    half4 finalAlbedo=lerp(baseAlbedo,topAlbedo,topfactor*topAO);
    half finalAO=lerp(baseAO,lerp(baseAO,1,0.8),topfactor);

     half finalSmoothness=lerp(baseSmoothness,topSmoothness,topfactor);

    outSurfaceData.albedo = baseAlbedo.rgb;
    
    outSurfaceData.metallic = 0;
    outSurfaceData.specular = half3(0.0h, 0.0h, 0.0h);

    outSurfaceData.smoothness = finalSmoothness;
    outSurfaceData.normalTS = baseNormal;
    outSurfaceData.occlusion = finalAO.r;
    outSurfaceData.emission = 0;

#if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
    half2 clearCoat = SampleClearCoat(uv);
    outSurfaceData.clearCoatMask       = clearCoat.r;
    outSurfaceData.clearCoatSmoothness = clearCoat.g;
#else
    outSurfaceData.clearCoatMask       = 0.0h;
    outSurfaceData.clearCoatSmoothness = 0.0h;
#endif
}


#endif 
