//-----------------------------------------------------------------------------
// Copyright(C) Yan Verde - All Rights Reserved
// Copyright protected under Unity Asset Store EULA
// Refer to https://unity3d.com/legal/as_terms for more informations
// -----------------------------------------------------------------------------
// URP Water
// Author : Yan Verde
// Date : April 10, 2021
// -----------------------------------------------------------------------------

#ifndef URPWATER_VARIABLES_INCLUDED
#define URPWATER_VARIABLES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"   
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
	#define UNITY_DECLARE_SCREENSPACE_TEXTURE(textureName) TEXTURE2D_ARRAY(textureName);
	#define UNITY_DECLARE_SCREENSPACE_TEXTURE_FLOAT(textureName) TEXTURE2D_ARRAY_FLOAT(textureName);
		//UNITY_SAMPLE_TEX2DARRAY(tex, float3((uv).xy, (float)unity_StereoEyeIndex))
	#define UNITY_SAMPLE_SCREENSPACE_TEXTURE(tex, samplerName, uv) SAMPLE_TEXTURE2D_ARRAY(tex, samplerName, uv.xy, (float)unity_StereoEyeIndex);
	#define UNITY_SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord2, lod) SAMPLE_TEXTURE2D_ARRAY_LOD(textureName, samplerName, coord2, (float)unity_StereoEyeIndex, lod);
#else
	#define UNITY_DECLARE_SCREENSPACE_TEXTURE(tex) TEXTURE2D(tex);
	#define UNITY_DECLARE_SCREENSPACE_TEXTURE_FLOAT(tex) TEXTURE2D_FLOAT(tex);
	#define UNITY_SAMPLE_SCREENSPACE_TEXTURE(tex, samplerName, uv) SAMPLE_TEXTURE2D(tex, samplerName, uv);
	#define UNITY_SAMPLE_TEXTURE2D_LOD(tex, samplerName, coord2, lod) SAMPLE_TEXTURE2D_LOD(tex, samplerName, coord2, lod);
#endif


#define URP_WATER_PI 3.1415926535

TEXTURE2D(_CausticsTex);
TEXTURE2D(_FoamTex);
TEXTURE2D(_NormalMapA);
TEXTURE2D(_NormalMapB);
TEXTURE2D(_FlowMap);

//UNITY_DECLARE_SCREENSPACE_TEXTURE_FLOAT(_CameraDepthTexture); // VR
//UNITY_DECLARE_SCREENSPACE_TEXTURE(_CameraOpaqueTexture); // VR

TEXTURE2D(_RefractionColor);

SAMPLER(sampler_CameraOpaqueTexture_linear_clamp);
SAMPLER(sampler_ScreenTextures_linear_clamp);
SAMPLER(sampler_pointTextures_point_clamp);

TEXTURE2D(_ReflectionTexture);

TEXTURE2D(_PlanarReflectionTex);
SAMPLER(sampler_PlanarReflectionTex);

TEXTURECUBE(_CubemapTexture);
SAMPLER(sampler_CubemapTexture);

TEXTURE2D(_SkyboxReflectionTex);     SAMPLER(sampler_SkyboxReflectionTex);
TEXTURE2D(_WaterReflectionTex);     SAMPLER(sampler_WaterReflectionTex);


SamplerState URPWater_trilinear_repeat_sampler;
SamplerState URPWater_linear_repeat_sampler;
SamplerState URPWater_linear_clamp_sampler;

float4 _CameraOpaqueTexture_TexelSize;
float4 _CameraDepthTexture_TexelSize;
float4 _DynamicEffectsTexture_TexelSize;
float4 _DynamicEffectsParams;


CBUFFER_START(UnityPerMaterial)

float4 _NormalMapA_ST;
float4 _NormalMapASpeeds;
float4 _NormalMapATilings;
float4 _NormalMapB_ST;
float4 _NormalMapBSpeeds;
float4 _NormalMapBTilings;
float4 _FlowTiling;
float4 _CausticsTiling;
float4 _Color;
float4 _DepthColor;
float4 _UnderWaterColor;
//float4 _AmbientColor;
float4 _SpecColor;
float4 _CausticsSpeed;
float4 _ScatteringColor;
float4 _FoamColor;
float4 _FoamTiling;

float _CausticsStart;
float _CausticsEnd;
float _CausticsDistortion;
float _Distortion;
float _Smoothness;
float _NormalMapAIntensity;
float _NormalMapBIntensity;
float _FlowSpeed;
float _FlowIntensity;
float _DepthStart;
float _DepthEnd;
float _ReflectionDistortion;
float _ReflectionFresnelNormal;
float _ReflectionRoughness;
float _PlanarReflectionDistortion;
float _EdgeSize;
float _CausticsIntensity;
float _ReflectionFresnel;
float _ReflectionIntensity;
float _FoamSize;
float _FoamRippleDistance;
float _FoamRippleSize;
float _FoamRippleSpeed;
float _FoamDistortion;
float _FoamCapsRangeMin;
float _FoamCapsRangeMax;
float _FoamCapsIntensity;
float _ScatteringIntensity;
float _ScatteringRangeMin;
float _ScatteringRangeMax;

float _WaveStartDistance;
float _WaveEndDistance;

float _Tess;
float _SSPRLerpScale;
CBUFFER_END


struct Attributes
{
	float4 vertex		: POSITION;
	float4 color		: COLOR;
	float3 normal		: NORMAL;
	float4 tangent 		: TANGENT;
	float2 texcoord		: TEXCOORD0;

	#if _DISPLACEMENTMODE_GERSTNER
	float waveHeight : TEXCOORD1;
	#endif

	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 pos				 :     SV_POSITION;
	float4 color			 :     COLOR;
	float3 worldNormal		 :     NORMAL;
	float3 worldPos          :     TEXCOORD0;
	float4 texcoord			 :     TEXCOORD1;

	#if _NORMALSMODE_DUAL || _FOAM_ON
	float4 texcoord1		 :     TEXCOORD2;
	#endif

	float4 screenCoord	     :     TEXCOORD4;
	half4 normal	         :     TEXCOORD5;   
	half4 tangent	         :     TEXCOORD6;   
	half4 bitangent          :     TEXCOORD7;   
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};


struct GlobalData 
{
	float depth;		// Remapped Depth
	float sceneDepth;	// Linear Depth
	float rawDepthDst;	// Raw Depth Distorted
	float pixelDepth;
	float foamMask;
	float2 refractionOffset;
	float2 refractionUV;
	float3 finalColor;
	float4 refractionData;	// RGB: Refraction Color A: Refraction Depth
	float3 clearColor;		// RGB: Clear Color
	float3 shadowColor;
	float3 worldPosition;
	float3 worldNormal;
	float3 worldViewDir;
	float4 screenUV;
	

	#if _SCATTERING_ON
	float3 scattering;
	#endif

	Light mainLight;
	float3 addLight;
	real3x3 tangentToWorld;

	// Debug purpose only
	float4 debug;
};

void InitializeGlobalData(inout GlobalData data, Varyings IN)
{
	data.depth = 0;
	data.sceneDepth = 0;
	data.rawDepthDst = 0;
	data.pixelDepth = IN.screenCoord.z;
	data.foamMask = 0;
	data.refractionOffset = float2(0, 0);
	data.refractionUV = float2(0, 0);
	data.refractionData = float4(0, 0, 0 ,0);
	data.clearColor = float3(1, 1, 1);
	data.finalColor = float3(1, 1, 1);
	data.shadowColor = float3(1, 1, 1);
	data.worldPosition = float3(IN.normal.w, IN.tangent.w, IN.bitangent.w);
	data.worldNormal = float3(0, 1, 0);
	data.worldViewDir = GetWorldSpaceNormalizeViewDir(data.worldPosition); //SafeNormalize(_WorldSpaceCameraPos.xyz - data.worldPosition);
	data.screenUV = float4(IN.screenCoord.xyz / IN.screenCoord.w, IN.pos.z); //ComputeScreenPos(TransformWorldToHClip(data.worldPosition), _ProjectionParams.x);
	

	#if _SCATTERING_ON
	data.scattering = float3(0,0,0);
	#endif

	data.mainLight = GetMainLight(TransformWorldToShadowCoord(data.worldPosition));
	data.addLight = float3(0, 0, 0);
	data.tangentToWorld = float3x3(IN.tangent.xyz, IN.bitangent.xyz, IN.normal.xyz);

	data.debug = float4 (0, 0, 0, 1);
}



#endif