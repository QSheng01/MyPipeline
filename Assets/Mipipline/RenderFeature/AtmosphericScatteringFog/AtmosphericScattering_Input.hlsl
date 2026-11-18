#ifndef UNITY_ATMOSPHERIC_SCATTERING_INPUT_INCLUDED
#define UNITY_ATMOSPHERIC_SCATTERING_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

// TEXTURE2D_X(_CameraDepthTexture);
// SAMPLER(sampler_CameraDepthTexture);
// TEXTURE2D_X(_CameraOpaqueTexture);

TEXTURE2D_X(_SkyboxTexture);
// SAMPLER(sampler_CameraOpaqueTexture);//

CBUFFER_START(OqaqueAtmosphericScatteringBuffer)
float _MaxFogDistance;
// float _DownSampleParameters;
float _HeightFogBaseExtinction;
float _HeightFogBaseHeight;
float _ProbeExposureScale;
float _FogColorMode;
float _JitterStrength;
float _SunFallOff;
float _MoonFallOff;
float _FogStartDistance;
float4 _SunColor;
float4 _MoonColor;
float2 _HeightFogExponents;
float4 _FogColor;
float4 _FogEndColor;
float4 _HeightFogBaseScattering;
float4 _MipFogParameters;


float4x4 _PixelCoordToViewDirWS;
CBUFFER_END

#endif