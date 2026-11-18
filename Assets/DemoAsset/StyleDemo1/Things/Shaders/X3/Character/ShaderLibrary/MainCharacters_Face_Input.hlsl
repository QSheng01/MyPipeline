#ifndef _MAINCHARACTERS_FACE_INPUT_HLSL_
#define _MAINCHARACTERS_FACE_INPUT_HLSL_


#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Characters_StructData.hlsl"
TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
TEXTURE2D(_LightMap);
SAMPLER(sampler_LightMap);
TEXTURE2D(_RampTex);
SAMPLER(sampler_RampTex);


#if _EFFECTFRESNEL_ON
        TEXTURE2D(_EffectMaskMap);
        SAMPLER(sampler_EffectMaskMap);
#endif


CBUFFER_START(UnityPerMaterial)

float4 _BaseMap_ST; //基础设置

half4 _BaseColor, //基础设置
      _HColor, _DarkColor, _SDFShadowColor, //阴影设置
      _EmissionColor, //自发光设置
      _RimColor, //背光边缘光
      _CharacterShadowColor, //投影设置
      _HairShadowColor, //刘海投影设置
      _HeavyShadowColor, //重阴影设置
      // _CharacterLightDir, _CharacterLightColor, _CharacterLightData0, _CharacterAmbientColor, _CharacterEnvColor, _CharacterSpecularColor,//角色灯光脚本
      _EffectFresnelColor, //Fresnel边缘光设置(特效专用)
      _HitRimColor, //受击边缘光设置
      _OutlineColor; //描边设置


float3 _FaceForward, _FaceRight; //SDF脸部阴影


float _UseSpecialSDF, _SpecialSdfAcceleration_Front, _SpecialSdfAcceleration_Black, _SpecialSdfRange, //SDF脸部阴影
      _CustomShadowNormalBias, _CustomShadowDepthBias, //内置投影偏移
      _Outline_Width, _Offset_Z; //描边设置


// half _BumpStrength,//基础设置
half //_Reversal, _FaceArea,//SDF脸部阴影
    _UseHalfLambert1, _RampUV_Y, _RampSize, //阴影设置
    // _Roughness, _PbrSpecularBrightness,//PBR高光设置
    _SHBrightness, //环境光设置
    _EmissionID, _Emission_Instensity, _Emission_Lerp, //自发光通道
    _useRim, _RimColorBlendAlbedo, _RimLambertMaskThreshold, _RimThreshold, _RimSoftness, //背光边缘光
    // _MultipleLightIntensity, _MultipleLightBlend,//多光源设置
    _ShadowIntensity, //投影设置
    _HeavyShadowRange, _HeavyShadowSoftness, _HeavyShadowIntensity, //重阴影设置
    _EffectFresnelFactor, _EffectFresnelPower, _EffectFresnelThreshold, _EffectFresnelSmooth, //Fresnel边缘光设置(特效专用)
    _HitRimFactor, _HitRimPower, _HitRimThreshold, _HitRimSmooth; //受击边缘光设置

CBUFFER_END

struct SDFData
{
    half rampSize;
    half usehalflambert;
    half rampUV_Y;
    half SpecialSdfAcceleration_Front;
    half SpecialSdfAcceleration_Black;
    half SpecialSdfRange;
    half UseSpecialSDF;
    half3 darkColor;
    half3 brightColor;
};

struct HeavyShadowData
{
    half heavyShaderIntensity;
    half heavyRange;
    half heavySoftness;
    half3 heavyShadowColor;
};

struct EffectFresnelData
{
    half effectFresnelMask;
    half effectFresnelFactor;
    half effectFresnelPower;
    half effectFresnelThreshold;
    half effectFresnelSmooth; //Fresnel边缘光设置(特效专用)
    half3 effectFresnelColor;
};



//=================================================================//
//                          初始化数据                              //
//=================================================================//
inline void InitializeTextureData(float4 uv, float4 screenUV, out TextureData textureData)
{
    textureData = (TextureData)0;

    textureData.uv = uv;
    textureData.screenUV = screenUV.xy/screenUV.w;

    //==============颜色==============
    //固有色、Alpha
    half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv.xy);

    textureData.albedo = albedoAlpha.rgb * _BaseColor.rgb;
    textureData.alpha = albedoAlpha.a * _BaseColor.a;

    half emissionValue = textureData.lightMap.g;
    textureData.emission = lerp(_EmissionColor.rgb, _EmissionColor.rgb * textureData.albedo, _Emission_Lerp);
    textureData.emission *= emissionValue;
    textureData.emission *= _Emission_Instensity;

    textureData.rampID = 0;
}

inline void InitializeSDFData(out SDFData sdfData)
{
    sdfData = (SDFData)0;
    sdfData.rampSize = _RampSize;
    sdfData.usehalflambert= _UseHalfLambert1;
    sdfData.rampUV_Y = _RampUV_Y;
    sdfData.darkColor = _DarkColor.rgb;
    sdfData.brightColor = _HColor.rgb;
    sdfData.SpecialSdfAcceleration_Front = _SpecialSdfAcceleration_Front;
    sdfData.SpecialSdfAcceleration_Black = _SpecialSdfAcceleration_Black;
    sdfData.SpecialSdfRange = _SpecialSdfRange;
    sdfData.UseSpecialSDF = _UseSpecialSDF;
}

inline void InitializeShadowData(out CharactersShadowData shadowData)
{
    shadowData = (CharactersShadowData)0;
    shadowData.shaderIntensity = _ShadowIntensity;
    shadowData.shadowColor = _CharacterShadowColor.rgb;
}

inline void InitializeHeavyShadowData(out HeavyShadowData heavyShadowData)
{
    heavyShadowData = (HeavyShadowData)0;
    heavyShadowData.heavyShaderIntensity = _HeavyShadowIntensity;
    heavyShadowData.heavyShadowColor = _HeavyShadowColor.rgb;
    heavyShadowData.heavyRange = _HeavyShadowRange;
    heavyShadowData.heavySoftness = _HeavyShadowSoftness;
}
inline void InitializeRimLightData(out CharactersRimLightData rimLightData)
{
    rimLightData = (CharactersRimLightData)0;
    rimLightData.rimColor = _RimColor.rgb;
    rimLightData.rimMask = _RimLambertMaskThreshold;
    rimLightData.rimThreshold = _RimThreshold;
    rimLightData.rimSoftness = _RimSoftness;
    rimLightData.rimColorWeight = _RimColorBlendAlbedo;
}

inline void InitializeHitRimLightData(out CharactersHitRimLightData hitRimLightData)
{
    hitRimLightData = (CharactersHitRimLightData)0;
    hitRimLightData.hitRimColor = _HitRimColor.rgb;
    hitRimLightData.hitRimThreshold = _HitRimThreshold;
    hitRimLightData.hitRimSmooth = _HitRimSmooth;
    hitRimLightData.hitRimFactor = _HitRimFactor;
    hitRimLightData.hitRimPower = _HitRimPower;
}


inline void InitializeEffectFresnelData(float2 uv,out EffectFresnelData effectFresnelData)
{
    #if _EFFECTFRESNEL_ON
    effectFresnelData = (EffectFresnelData)0;
    effectFresnelData.effectFresnelMask = SAMPLE_TEXTURE2D(_EffectMaskMap, sampler_EffectMaskMap, uv).r;
    effectFresnelData.effectFresnelFactor = _EffectFresnelFactor;
    effectFresnelData.effectFresnelPower = _EffectFresnelPower;
    effectFresnelData.effectFresnelThreshold = _EffectFresnelThreshold;
    effectFresnelData.effectFresnelSmooth = _EffectFresnelSmooth;
    effectFresnelData.effectFresnelColor = _EffectFresnelColor.rgb;
    #endif
}


half GetSHBrightness()
{
    return _SHBrightness;
}


inline half4 GetLightMap(float2 uv,float sign)
{
    return SAMPLE_TEXTURE2D(_LightMap, sampler_LightMap, uv * float2(-sign, 1));
}

inline half4 GetRampMap(float2 uv)
{
    return SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, uv);
}









#endif
