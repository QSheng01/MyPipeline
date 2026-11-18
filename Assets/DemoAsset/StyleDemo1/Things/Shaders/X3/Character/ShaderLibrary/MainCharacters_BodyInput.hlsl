#ifndef MAINCHARACTERS_BODYINPUT
#define MAINCHARACTERS_BODYINPUT

	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

    //基础设置
    TEXTURE2D(_BaseMap);
    SAMPLER(sampler_BaseMap);
    
    TEXTURE2D(_BumpMaskMap);
    SAMPLER(sampler_BumpMaskMap);

    TEXTURE2D(_RampMap);
    SAMPLER(sampler_RampMap);

    // TEXTURE2D(_MatCapMap);
    // SAMPLER(sampler_MatCapMap);

	TEXTURECUBE(_EvnCubemap);
	SAMPLER(sampler_EvnCubemap);
    
    //PBR参数设置
    TEXTURE2D(_LightMap);
    SAMPLER(sampler_LightMap);
    
    TEXTURE2D(_MaskMap);
    SAMPLER(sampler_MaskMap);

    //逐对象投影
	TEXTURE2D(_ScreenSpaceShadowMap);
    SAMPLER(sampler_ScreenSpaceShadowMap);

    //UI界面自定义边缘光
	TEXTURE2D_X_FLOAT(_CurrentCameraDepthTexture);            SAMPLER(sampler_CurrentCameraDepthTexture);

	//场景深度图
	TEXTURE2D_X_FLOAT(_CameraDepthTexture);            SAMPLER(sampler_CameraDepthTexture);

    #if _EFFECTFRESNEL_ON
    TEXTURE2D(_EffectMaskMap);
    SAMPLER(sampler_EffectMaskMap);
    #endif

	//Fur
	TEXTURE2D(_LayerTex);
	SAMPLER(sampler_LayerTex);

	TEXTURE2D(_FlowMap);
	SAMPLER(sampler_FlowMap);

	TEXTURE2D(_XFurGroomData);
	SAMPLER(sampler_XFurGroomData);

	// TEXTURE2D(_LayerNormalTex);
	// SAMPLER(sampler_LayerNormalTex);

    CBUFFER_START(UnityPerMaterial)

    float4x4 _CustomShowMatrix_V, _CustomShowMatrix_P;

	float4 _BumpMaskMap_TexelSize;

	half4 _ShadowColor;
	half4 _LayerTex_ST; 
	half4 _FlowMap_ST;

    half4 _BaseColor,//基础设置
    _CharacterLightDir, _CharacterLightColor, _CharacterLightData, _CharacterAmbientColor, _characterBakeAmbientColor, _CharacterEnvColor, _CharacterSpecularColor, _CharacterMultipleLight,//角色灯光脚本
    _SpecularColor,//双层高光设置
     _EnvColor,//环境光设置
    // _CharacterUIAmbientColor, _EnvColor,//环境光设置
	custom_SHAr, custom_SHAg, custom_SHAb, custom_SHBr, custom_SHBg, custom_SHBb, custom_SHC,//环境光设置
    _EmissionColor, _WholeEmissionColor,//自发光设置
    // _RimColor, _RimDir,//深度边缘光设置
	_RimLightDir, _RimColor,//菲涅尔边缘光设置
    _SilkStockingSkinColor, _SilkStockingDarkSideColor, _SilkStockingDarkFresnelColor, _SilkStockingRimColor,//丝袜设置
    _AnisotropySpecularColor,//各向异性高光设置
    _CharacterLightUiDir,//UI灯光方向
    // _CharacterShadowColor,//投影设置
	_HeavyShadowColor,//重阴影设置
    _PlaneShadowColor,//地面投影设置
    _EffectFresnelColor,//Fresnel边缘光设置(特效专用)
    _HitRimColor,//受击边缘光设置
    _TransparentFresnelColor,//半透菲涅尔
	_BumpMaskMap_ST,
	_XFurGroomData_ST,
	_XFurGroomData_TexelSize,
	_OutlineColor,//描边设置
	_FurEdgeColor;//毛发边缘光颜色

	half3 _Gravity;//毛发重力
	half3 _FabricScatterColor;
	half3 _OcclusionColor;

    float _SpecularYPosOffset, //高光设置
	_EnvRotate,//环境光设置
	_RimDepthWidth,//背光边缘光设置  _Test1, _Test2,
	_ObjectCoordinateAxisHeight, _PlaneShadowDistanceDisappearPos, _PlaneShadowDistanceDisappearSoftness, _PlaneShadowHeightOffset, _PlaneShadowHorizontalOffset,//地面投影设置
	_CustomShadowNormalBias, _CustomShadowDepthBias;//Unity默认光照投影
    
    half _BumpStrength,//基础设置
	_Metallic, _Smoothness,//遮罩图设置
    // _RampYOffset,//漫反射阴影设置
    _SpecularBlendAlbedoIntensity, _SpecularInstensity, _MetaIntensity,//双层高光设置
    // _SpecularInstensity, _SpecularBlendAlbedoIntensity, _UseNdVSpecular, _NdVSpecularWeight, _NdVSpecularInstensity,//双层高光设置
    _SHBrightness, _EnvBlendAlbedoIntensity, _EnvBrightness,//环境光设置
    // _EmissionInstensity, _EmissionBlendAlbedoIntensity,//自发光设置
    _EmissionInstensity,//自发光设置
    // _UseRimColorOrBaseMap2, _RimLightPower2, _RimLightEdgeSmooth2, _RimLightThreshold2, _RimLightContribution2, _RimCustom,//深度边缘光设置
	// _useRim, _RimColorBlend, _RimLambertMaskThreshold, _RimThreshold, _RimSoftness,//菲涅尔边缘光设置
	// _useRim, _RimLambertMaskThreshold, _RimThreshold,//菲涅尔边缘光设置
	_useRim, _RimLambertMaskThreshold, _RimThreshold, _RimColorBlendAlbedo,//菲涅尔边缘光设置 _RimSoftness,
    _SilkStockingSkinColorBrightness, _SilkStockingDarkSideBrightness, _SilkStockingDarkSideRange, _SilkStockingDarkSideSoftness, _SilkStockingDarkFresnelContrast, _SilkStockingDarkFresnelIntensity, _SilkStockingDarkFresnelRange, _SilkStockingDarkFresnelSoftness, _SilkStockingRimBrightness, _SilkStockingRimWidth,//丝袜设置
	_AnisotropyIntensity, _AnisotropyBrightness, _AnisotropySpecularBlendAlbedoIntensity,//各向异性高光设置
    _MultipleLightIntensity, _MultipleLightBlend,//多光源设置
    // _ShadowColorOffset, _ShadowIntensity,//投影设置
    _ShadowIntensity,//投影设置
	_UseShadowMask , _HeavyShadowRange, _HeavyShadowSoftness, _HeavyShadowIntensity,//重阴影设置
    _EffectFresnelFactor, _EffectFresnelPower, _EffectFresnelThreshold, _EffectFresnelSmooth,//Fresnel边缘光设置(特效专用)
    _HitRimFactor, _HitRimPower, _HitRimThreshold, _HitRimSmooth,//受击边缘光设置
    _CharacterTransparentValue, _TransparentFresnelRange, _TransparentFresnelBrightness,//半透菲涅尔
	_Outline_Width, _Offset_Z;

	//Fur
	half _SuoNormal;
	half _GravityStrength;
	half _FurLength;
	half _CutoffEnd;
	half _EdgeFade;
	half _UVOffset;
	half _ShadowLerp;
    half _BumpScale;
    half _Noise;
	half  _FabricScatterScale;
	half _FresnelLV;
	half _FurEdgePow;
	half _FurEdgeAtten;
	half _AlphaCutout;
    CBUFFER_END

	half _FUR_OFFSET;

#endif
