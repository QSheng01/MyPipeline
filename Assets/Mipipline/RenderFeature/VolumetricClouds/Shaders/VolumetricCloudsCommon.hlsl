#ifndef URP_VOLUMETRIC_CLOUDS_COMMON_H
#define URP_VOLUMETRIC_CLOUDS_COMMON_H

// URP / SRP core includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

// Samplers (Unity auto-generates these from the name pattern)
SAMPLER(sampler_LinearClamp);
SAMPLER(sampler_LinearRepeat);
SAMPLER(sampler_TrilinearRepeat);
SAMPLER(sampler_PointClamp);

//--------------------------------------------------------------------------------------------------
// Uniforms (set from C# on the compute shader). Kept close to HDRP naming for portability.
//--------------------------------------------------------------------------------------------------

// Geometry of the cloud layer
float   _HighestCloudAltitude;
float   _LowestCloudAltitude;
float   _EarthRadius;
float2  _CloudRangeSquared;
float   _MaxRayMarchingDistance;
float   _MaxCloudDistance;
float   _NormalizationFactor;

// Ray marching quality
int     _NumPrimarySteps;
int     _NumLightSteps;

// Cloud map / wind animation
float4  _CloudMapTiling;
float2  _WindDirection;
float2  _WindVector;
float2  _ShapeNoiseOffset;
float   _VerticalShapeNoiseOffset;
float   _VerticalShapeWindDisplacement;
float   _VerticalErosionWindDisplacement;
float   _LargeWindSpeed;
float   _MediumWindSpeed;
float   _SmallWindSpeed;
float   _AltitudeDistortion;

// Lighting
float4  _SunLightColor;
float4  _SunDirection;
float   _MultiScattering;
float   _ErosionOcclusion;
float   _PowderEffectIntensity;
float4  _ScatteringTint;
float3  _CloudAmbientTop;
float3  _CloudAmbientBottom;

// Modeling
float   _DensityMultiplier;
float   _ShapeFactor;
float   _ErosionFactor;
float   _ShapeScale;
float   _ErosionScale;
float   _ErosionFactorCompensation;

// Temporal
float   _TemporalAccumulationFactor;
float   _CloudHistoryInvalidation;
int     _AccumulationFrameIndex;
int     _SubPixelIndex;
int     _EnableIntegration;
int     _LowResolutionEvaluation;

// Fade
float   _FadeInStart;
float   _FadeInDistance;

// Resolutions (xy = size, zw = 1/size)
float4  _FinalScreenSize;
float4  _IntermediateScreenSize;
float4  _TraceScreenSize;
float2  _HistoryViewportSize;
float2  _HistoryBufferSize;

// Camera
float3  _CloudsCameraPosWS;
float4x4 _CloudsInvVP;
float4x4 _CloudsVP;
float4x4 _CloudsPrevVP;

// Compositing
float   _ImprovedTransmittanceBlend;
float3  _CloudFogColor;
float   _CloudFogDistance;   // distance at which fog fully saturates (0 = disabled)

// Local vs distant clouds
int     _LocalClouds;        // 1 = camera can fly through clouds (depth occlusion), 0 = skybox clouds
float   _CloudNearPlane;
float   _CloudFarPlane;

// RDR2-style reconstruction quality toggles (0 = old logic, 1 = new logic)
int     _UseDepthRejection;      // F1: reject neighborhood AABB samples with large depth difference
int     _UseMinMaxDepth;         // F2: min/max depth neighborhood analysis drives checkerboard ray placement
int     _UseBlueNoiseUpscale;    // F3: blue-noise dithered upscale with transmittance-weighted kernel
float   _DepthRejectionThreshold;// relative linear-depth difference above which a neighbor is rejected
float   _RayPlacementDepthThreshold; // F2: relative linear-depth diff to consider a tile extreme "uncovered" by a neighbor's ray
int     _CheckerboardMinMaxDepth;    // F2 variant: emulate RDR2 single-channel checkerboard min/max (each cell exposes only min OR max by parity)
float4  _CloudsZBufferParams;    // for LinearEyeDepth() inside compute (Unity _ZBufferParams equivalent)

// Distance based weights for bilateral upsampling (3x3, per 2x2 subregion => 12 entries)
float4  _DistanceBasedWeights[12];

//--------------------------------------------------------------------------------------------------
// Static textures
//--------------------------------------------------------------------------------------------------
Texture3D<float>    _Worley128RGBA;   // shape noise
Texture3D<float>    _ErosionNoise;    // erosion noise
Texture2D<float4>   _CloudMapTexture; // r=coverage, g=rain, b=cloudType, a=maxHeight
Texture2D<float3>   _CloudLutTexture; // r=density, g=erosion, b=ao

// Blue-noise dithered sequence textures (HDRP assets). If not provided, a hash is used instead.
int                 _UseBlueNoise;
Texture2D<float>    _OwenScrambledTexture;
Texture2D<float>    _ScramblingTileXSPP;
Texture2D<float>    _RankingTileXSPP;

// Blue-noise dithered sequence sampler (ported from HDRP RaytracingSampling.hlsl).
// "A Low-Discrepancy Sampler that Distributes Monte Carlo Errors as a Blue Noise in Screen Space" (Heitz et al.)
float GetBNDSequenceSample(uint2 pixelCoord, uint sampleIndex, uint sampleDimension)
{
    pixelCoord = pixelCoord & 127;
    sampleIndex = sampleIndex & 255;
    sampleDimension = sampleDimension & 255;

    uint rankingIndex = (pixelCoord.x + pixelCoord.y * 128) * 8 + (sampleDimension & 7);
    uint rankedSampleIndex = sampleIndex ^ clamp((uint)(_RankingTileXSPP[uint2(rankingIndex & 127, rankingIndex / 128)] * 256.0), 0, 255);

    uint value = clamp((uint)(_OwenScrambledTexture[uint2(sampleDimension, rankedSampleIndex)] * 256.0), 0, 255);

    uint scramblingIndex = (pixelCoord.x + pixelCoord.y * 128) * 8 + (sampleDimension & 7);
    float scramblingValue = min(_ScramblingTileXSPP[uint2(scramblingIndex & 127, scramblingIndex / 128)], 0.999);
    value = value ^ uint(scramblingValue * 256.0);

    return (scramblingValue + value) / 256.0;
}

//--------------------------------------------------------------------------------------------------
// URP compatibility shims (HDRP uses these names)
//--------------------------------------------------------------------------------------------------
#define GetCurrentExposureMultiplier() (1.0)
#define GetInversePreviousExposureMultiplier() (1.0)

// Distant clouds only in this port: camera-relative origin, earth centered under the camera.
#define DISTANT_VOLUMETRIC_CLOUDS 1

#endif // URP_VOLUMETRIC_CLOUDS_COMMON_H
