#ifndef CHARACTERS_HAIR_INPUT_HLSL
#define CHARACTERS_HAIR_INPUT_HLSL
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Characters_StructData.hlsl"

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

TEXTURE2D(_BumpMaskMap);
SAMPLER(sampler_BumpMaskMap);

TEXTURE2D(_LightMap);
SAMPLER(sampler_LightMap);

TEXTURE2D(_ShadowMap);
SAMPLER(sampler_ShadowMap);

TEXTURE2D(_AnisotropyShapeMap);
SAMPLER(sampler_AnisotropyShapeMap);

TEXTURE2D(_AnisotropyLineMap);
SAMPLER(sampler_AnisotropyLineMap);

//场景深度图
TEXTURE2D_X_FLOAT(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);


#if _EFFECTFRESNEL_ON
        TEXTURE2D(_EffectMaskMap);
        SAMPLER(sampler_EffectMaskMap);
#endif

CBUFFER_START(UnityPerMaterial)

float4 _BaseMap_ST, _BumpMaskMap_TexelSize,
       _AnisotropyShapeMap_ST, _AnisotropyLineMap_ST;

half4 _BaseColor, _AoColor, //基础设置
      // _PbrSpecularColor,//PBR高光设置
      _HairSpecularCol, _HairSecondarySpecularCol, //各向异性高光
      _WholeEmissionColor, //自发光设置
      _RimColor, _BlackRimColor, //背光边缘光
      _CharacterShadowColor, //投影设置
      // _CharacterLightDir, _CharacterLightColor, _CharacterLightData0, _CharacterAmbientColor, _CharacterEnvColor, _CharacterSpecularColor,//角色灯光脚本
      _EffectFresnelColor, //Fresnel边缘光设置(特效专用)
      _HitRimColor, //受击边缘光设置
      _OutlineColor; //描边设置


float _HairSpread1, _HairSpecularShift, _HairSpecularStrength, _HairSpecularExponent, _HairSpread2,
      _HairSecondarySpecularShift_Black, _HairSecondarySpecularStrength, _HairSecondarySpecularExponent,
      _AnisotropyLinePos, _AnisotropyLineRange, //各向异性高光
      _RimDepthWidth, //背光边缘光设置  _Test1, _Test2,
      _CustomShadowNormalBias, _CustomShadowDepthBias, //内置投影偏移
      _HairShadowOffset, _HairShadowOffsetLeftRight, _HairShadowOffsetTopDown, //刘海投影设置
      _Outline_Width, _Offset_Z; //描边设置


half _BumpStrength, _AoStrength, _AoColorBlendAlbedo, //基础设置
     _RampYOffset, _RampXOffset, _DiffuseAddShadow, _UseOtherLight, _UseDiffuseBlendAO, //阴影设置
     // _Roughness, _PbrSpecularBrightness,//PBR高光设置
     _AnisotropyLineIntensity, //各向异性高光
     _SHBrightness, //环境光设置
     // _useRim, _RimLambertMaskThreshold, _RimThreshold, //背光边缘光
     _useRim, _RimDirReverse, _RimLambertMaskThreshold, _RimThreshold, _BlackRimThreshold, _RimColorBlendAlbedo,
     _RimLambertMaskBrightness, //菲涅尔边缘光设置 _RimSoftness,
     // _MultipleLightIntensity, _MultipleLightBlend,//多光源设置
     _ShadowIntensity, //投影设置
     _EffectFresnelFactor, _EffectFresnelPower, _EffectFresnelThreshold, _EffectFresnelSmooth, //Fresnel边缘光设置(特效专用)
     _HitRimFactor, _HitRimPower, _HitRimThreshold, _HitRimSmooth; //受击边缘光设置

CBUFFER_END

struct LightColorAddData
{
    //灯光脚本内容
    half3 indirectSpecularColor;
};

struct DiffuseData
{
    half rampXOffset;
    half rampYOffset;
    half useDiffuseBlendAO;
    half diffuseAddShadow;
};

struct KajiyaKayData
{
    float hairSpread1;
    float hairSpread2;
    float hairSpecularShift;
    float hairSecondarySpecularShift_Black;
    float hairSpecularStrength;
    float hairSecondarySpecularStrength;
    float hairSpecularExponent;
    float hairSecondarySpecularExponent;
    float anisotropyLinePos;
    float anisotropyLineRange;
    float anisotropyLineIntensity;
    half3 hairSpecularCol;
    half3 hairSecondarySpecularCol;
};

struct GIData
{
    half sHBrightness;
    half aoColorBlendAlbedo;
    half3 aoColor;
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


inline void InitializeTextureData(float4 uv, float4 screenUV, out TextureData textureData)
{
    textureData = (TextureData)0;
    textureData.uv = uv;
    textureData.screenUV = screenUV.xy / screenUV.w;

    //==============颜色==============
    //固有色、Alpha
    half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv.xy);

    textureData.albedo = albedoAlpha.rgb * _BaseColor.rgb;
    textureData.alpha = albedoAlpha.a * _BaseColor.a;


    //==============法线==============
    half4 normalMask = SAMPLE_TEXTURE2D(_BumpMaskMap, sampler_BumpMaskMap, uv.xy);

    half4 tangentNormal = half4(0, normalMask.y, 0, normalMask.x);
    tangentNormal = _BumpMaskMap_TexelSize.x == 1 ? half4(0, 0.5, 0, 0.5) : tangentNormal; //如果贴图为空
    tangentNormal.xyz = UnpackNormalAG(tangentNormal, _BumpStrength);
    // tangentNormal.xyz = UnpackNormalAG(tangentNormal);

    textureData.normalTS = tangentNormal.xyz;


    //==============遮罩图==============
    textureData.lightMap = SAMPLE_TEXTURE2D(_LightMap, sampler_LightMap, uv.xy);
    // half4 maskMap = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, i.uv.xy);// R:RampID G:环境高光强度 B:自发光 A:空

    // half metallic = GetChannel(textureData.alpha, textureData.normalTS.b, textureData.lightMap, _METAL);
    // metallic = saturate(metallic * _MetallicScale + _MetallicBias);
    // textureData.metallic = metallic;
    textureData.metallic = 0;

    // half smoothness = GetChannel(textureData.alpha, textureData.normalTS.b, textureData.lightMap, _GLOSS);
    // smoothness = saturate(smoothness * _SmoothnessScale + _SmoothnessBias);
    // textureData.smoothness = smoothness;
    textureData.smoothness = 0.5;


    // half occlusion = GetChannel(textureData.alpha, textureData.normalTS.b, textureData.lightMap, _AO);
    // occlusion = LerpWhiteTo(occlusion,_OcclusionStrength);
    // textureData.occlusion = occlusion;
    textureData.ao = textureData.lightMap.g;


    // #if _EMISSION
    //     #if _EMISSIONMAP
    //     textureData.emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, i.uv.xy).rgb;
    //     #else
    //     textureData.emission = GetChannel(textureData.alpha, textureData.normalTS.b, textureData.lightMap, _EmissionID).xxx;
    //     #endif
    //     textureData.emission = textureData.emission * lerp(textureData.albedo.rgb, _EmissionColor.rgb, _Emission_Lerp) * _Emission_Instensity;
    // #else
    // textureData.emission = 0;
    // #endif
    textureData.emission = 0;


    // textureData.rampID = GetChannel(textureData.alpha, textureData.normalTS.b, textureData.lightMap, 3);
    textureData.rampID = 0;

    //有smoothness 就不用这个了？
    // half roughness = 0.5;
    // roughness *= roughness;
    // roughness = max(HALF_MIN_SQRT, roughness);
    // textureData.roughness = roughness;
}

inline void InitializeShadowData(out CharactersShadowData shadowData)
{
    shadowData = (CharactersShadowData)0;
    shadowData.shaderIntensity = _ShadowIntensity;
    shadowData.shadowColor = _CharacterShadowColor.rgb;
}

inline void InitializeDiffuseData(out DiffuseData diffuseData)
{
    diffuseData = (DiffuseData)0;
    diffuseData.rampXOffset = _RampXOffset;
    diffuseData.rampYOffset = _RampYOffset;
    diffuseData.useDiffuseBlendAO = _UseDiffuseBlendAO;
    diffuseData.diffuseAddShadow = _DiffuseAddShadow;
}

inline void InitializeKajiyakayData(out KajiyaKayData kajiyaKayData)
{
    kajiyaKayData = (KajiyaKayData)0;
    kajiyaKayData.hairSpread1 = _HairSpread1;
    kajiyaKayData.hairSpread2 = _HairSpread2;
    kajiyaKayData.hairSpecularShift = _HairSpecularShift;
    kajiyaKayData.hairSecondarySpecularShift_Black = _HairSecondarySpecularShift_Black;
    kajiyaKayData.hairSpecularStrength = _HairSpecularStrength;
    kajiyaKayData.hairSecondarySpecularStrength = _HairSecondarySpecularStrength;
    kajiyaKayData.hairSpecularExponent = _HairSpecularExponent;
    kajiyaKayData.hairSecondarySpecularExponent = _HairSecondarySpecularExponent;
    kajiyaKayData.hairSpecularCol = _HairSpecularCol.rgb;
    kajiyaKayData.hairSecondarySpecularCol = _HairSecondarySpecularCol.rgb;
    kajiyaKayData.anisotropyLinePos = _AnisotropyLinePos;
    kajiyaKayData.anisotropyLineRange = _AnisotropyLineRange;
    kajiyaKayData.anisotropyLineIntensity = _AnisotropyLineIntensity;
}

inline void InitializeGIData(out GIData giData)
{
   giData = (GIData)0;
    giData.sHBrightness = _SHBrightness;
    giData.aoColorBlendAlbedo = _AoColorBlendAlbedo;
    giData.aoColor = _AoColor.rgb;
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

inline void InitializeHitRimLightData(out CharactersHitRimLightData hitRimLightData)
{
    hitRimLightData = (CharactersHitRimLightData)0;
    hitRimLightData.hitRimColor = _HitRimColor.rgb;
    hitRimLightData.hitRimThreshold = _HitRimThreshold;
    hitRimLightData.hitRimSmooth = _HitRimSmooth;
    hitRimLightData.hitRimFactor = _HitRimFactor;
    hitRimLightData.hitRimPower = _HitRimPower;
}

inline void InitializeBlackRimLightData(out CharactersBlackRimLightData blackRimLightData)
{
    blackRimLightData = (CharactersBlackRimLightData)0;
    blackRimLightData.blackRimLambertMaskThreshold = _RimLambertMaskThreshold;
    blackRimLightData.blackRimDepthWidth = _RimDepthWidth;
    
}


inline half4 GetShadowMap(float2 uv)
{
    return SAMPLE_TEXTURE2D(_ShadowMap, sampler_ShadowMap, uv);
}

inline half4 GetAnisotropyShapeMap(float2 uv)
{
    float2 anisotropyShapeMapUV = TRANSFORM_TEX(uv, _AnisotropyShapeMap);
    return SAMPLE_TEXTURE2D(_AnisotropyShapeMap, sampler_AnisotropyShapeMap, anisotropyShapeMapUV);
}

inline half4 GetAnisotropyLineMap(float2 uv)
{
    float2 anisotropyLineMapUV = TRANSFORM_TEX(uv, _AnisotropyLineMap);
    return SAMPLE_TEXTURE2D(_AnisotropyLineMap, sampler_AnisotropyLineMap, anisotropyLineMapUV);
}


#endif
