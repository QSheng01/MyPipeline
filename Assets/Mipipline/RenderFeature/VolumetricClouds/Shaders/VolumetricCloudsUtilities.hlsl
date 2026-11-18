#ifndef URP_VOLUMETRIC_CLOUDS_UTILITIES_H
#define URP_VOLUMETRIC_CLOUDS_UTILITIES_H

#include "VolumetricCloudsCommon.hlsl"

// Tunables ported from HDRP
#define NUM_MULTI_SCATTERING_OCTAVES 2
#define PHASE_FUNCTION_STRUCTURE float2
#define CLOUD_DETAIL_MIP_OFFSET 0.0
#define CLOUD_LUT_MIP_OFFSET 1.0
#define CLOUD_DENSITY_TRESHOLD 0.001f
#define EMPTY_STEPS_BEFORE_LARGE_STEPS 8
#define FORWARD_ECCENTRICITY 0.7
#define BACKWARD_ECCENTRICITY 0.7
#define MIN_EROSION_DISTANCE 3000.0
#define MAX_EROSION_DISTANCE 100000.0
#define NOISE_TEXTURE_NORMALIZATION_FACTOR 100000.0f
#define MAX_SKYBOX_VOLUMETRIC_CLOUDS_DISTANCE 50000.0f
#define LIGHT_STEP_MAXIMAL_SIZE 1000.0f

//--------------------------------------------------------------------------------------------------
// Intersections (earth centered at (0, -_EarthRadius, 0) relative to the camera)
//--------------------------------------------------------------------------------------------------
int RaySphereIntersection(float3 startWS, float3 dir, float radius, out float2 result)
{
    float3 startPS = startWS + float3(0, _EarthRadius, 0);
    float a = dot(dir, dir);
    float b = 2.0 * dot(dir, startPS);
    float c = dot(startPS, startPS) - (radius * radius);
    float d = (b * b) - 4.0 * a * c;
    result = 0.0;
    int numSolutions = 0;
    if (d >= 0.0)
    {
        float sqrtD = sqrt(d);
        float q = -0.5 * (b + FastSign(b) * sqrtD);
        result = float2(c / q, q / a);
        numSolutions = 2;
        if (result.x < 0.0) { numSolutions--; result.x = result.y; }
        if (result.y < 0.0) numSolutions--;
    }
    return numSolutions;
}

bool RaySphereIntersection(float3 startWS, float3 dir, float radius)
{
    float3 startPS = startWS + float3(0, _EarthRadius, 0);
    float a = dot(dir, dir);
    float b = 2.0 * dot(dir, startPS);
    float c = dot(startPS, startPS) - (radius * radius);
    float d = (b * b) - 4.0 * a * c;
    bool flag = false;
    if (d >= 0.0)
    {
        float sqrtD = sqrt(d);
        float q = -0.5 * (b + FastSign(b) * sqrtD);
        float2 result = float2(c / q, q / a);
        flag = result.x > 0.0 || result.y > 0.0;
    }
    return flag;
}

//--------------------------------------------------------------------------------------------------
// Lighting structures
//--------------------------------------------------------------------------------------------------
struct EnvironmentLighting
{
    float3 sunDirection;
    float3 sunColor0;
    float3 sunColor1;
    float3 ambientTermTop;
    float3 ambientTermBottom;
    float  cosAngle;
    PHASE_FUNCTION_STRUCTURE phaseFunction;
};

struct CloudRay
{
    float  depthValue;
    float3 originWS;
    float3 direction;
    float  maxRayLength;
    float  insideClouds;
    float  toEarthCenter;
    float  integrationNoise;
    EnvironmentLighting envLighting;
};

float HenyeyGreenstein(float cosAngle, float g)
{
    float g2 = g * g;
    return (1.0 / (4.0 * PI)) * (1.0 - g2) / PositivePow(1.0 + g2 - 2.0 * g * cosAngle, 1.5);
}

EnvironmentLighting EvaluateEnvironmentLighting(CloudRay ray)
{
    EnvironmentLighting lighting;
    lighting.sunDirection = _SunDirection.xyz;
    lighting.sunColor0 = _SunLightColor.xyz;
    lighting.sunColor1 = _SunLightColor.xyz;
    lighting.ambientTermTop = _CloudAmbientTop;
    lighting.ambientTermBottom = _CloudAmbientBottom;
    lighting.cosAngle = dot(ray.direction, lighting.sunDirection);

    float forwardP = HenyeyGreenstein(lighting.cosAngle, FORWARD_ECCENTRICITY * PositivePow(_MultiScattering, 0));
    float backwardsP = HenyeyGreenstein(lighting.cosAngle, -BACKWARD_ECCENTRICITY * PositivePow(_MultiScattering, 0));
    lighting.phaseFunction[0] = forwardP + backwardsP;

    forwardP = HenyeyGreenstein(lighting.cosAngle, FORWARD_ECCENTRICITY * PositivePow(_MultiScattering, 1));
    backwardsP = HenyeyGreenstein(lighting.cosAngle, -BACKWARD_ECCENTRICITY * PositivePow(_MultiScattering, 1));
    lighting.phaseFunction[1] = forwardP + backwardsP;

    return lighting;
}

float DensityRemap(float x, float a, float b, float c, float d)
{
    return (((x - a) / (b - a)) * (d - c)) + c;
}

float PowderEffect(float cloudDensity, float cosAngle, float intensity)
{
    float powderEffect = 1.0 - exp(-cloudDensity * 4.0);
    powderEffect = saturate(powderEffect * 2.0);
    return lerp(1.0, lerp(1.0, powderEffect, smoothstep(0.5, -0.5, cosAngle)), intensity);
}

//--------------------------------------------------------------------------------------------------
// Ray building helpers
//--------------------------------------------------------------------------------------------------
// Reconstruct a world-space view direction from a pixel coordinate.
// Uses the URP convention (invViewProj built from the GPU projection matrix), so no manual Y flip.
float3 GetCloudViewDirWS(float2 positionSS, float2 screenSize)
{
    float2 uv = (positionSS + 0.5) * (1.0 / screenSize);
    // Any depth along the ray gives the same direction for a perspective camera.
    float3 posWS = ComputeWorldSpacePosition(uv, 0.5, _CloudsInvVP);
    return normalize(posWS - _CloudsCameraPosWS);
}

//--------------------------------------------------------------------------------------------------
// Ray march range
//--------------------------------------------------------------------------------------------------
struct RayMarchRange
{
    float start;
    float distance;
};

// Full 3-case intersection (ported from HDRP's LOCAL path). Handles the camera being below,
// inside or above the cloud shell. Distant clouds are the special case originWS = 0 (insideClouds = -1).
bool GetCloudVolumeIntersection(float3 originWS, float3 dir, float insideClouds, float toEarthCenter, out RayMarchRange rayMarchRange)
{
    ZERO_INITIALIZE(RayMarchRange, rayMarchRange);

    float2 intersectionInter, intersectionOuter;
    int numInterInner = RaySphereIntersection(originWS, dir, _LowestCloudAltitude + _EarthRadius, intersectionInter);
    int numInterOuter = RaySphereIntersection(originWS, dir, _HighestCloudAltitude + _EarthRadius, intersectionOuter);
    bool intersectEarth = RaySphereIntersection(originWS, dir, insideClouds < -1.5 ? toEarthCenter : _EarthRadius);

    bool intersect = numInterInner > 0 || numInterOuter > 0;

    // Below the lower cloud bound
    if (insideClouds < -0.5)
    {
        rayMarchRange.start = intersectionInter.x;
        rayMarchRange.distance = intersectionOuter.x - intersectionInter.x;
    }
    // Inside the cloud layer
    else if (insideClouds == 0.0)
    {
        rayMarchRange.start = 0;
        if (intersectEarth)
            rayMarchRange.distance = intersectionInter.x;
        else if (numInterInner == 0)
            rayMarchRange.distance = intersectionOuter.x;
        else
            rayMarchRange.distance = intersectionInter.x;
    }
    // Above the cloud layer
    else
    {
        rayMarchRange.start = intersectionOuter.x;
        if (intersectEarth)
            rayMarchRange.distance = intersectionInter.x - intersectionOuter.x;
        else
        {
            if (numInterInner == 0)
                rayMarchRange.distance = intersectionOuter.y - intersectionOuter.x;
            else
                rayMarchRange.distance = intersectionInter.x - intersectionOuter.x;
        }
    }

    rayMarchRange.distance = clamp(rayMarchRange.distance, 0.0, _MaxRayMarchingDistance);
    return intersect && rayMarchRange.distance > 0.0;
}

//--------------------------------------------------------------------------------------------------
// Cloud modeling
//--------------------------------------------------------------------------------------------------
struct CloudCoverageData
{
    float2 coverage;
    float  rainClouds;
    float  cloudType;
    float  maxCloudHeight;
};

struct CloudProperties
{
    float density;
    float ambientOcclusion;
    float height;
    float sigmaT;
};

bool PointInsideCloudVolume(float3 positionPS)
{
    float toEarthCenter2 = dot(positionPS, positionPS);
    return toEarthCenter2 < _CloudRangeSquared.y && toEarthCenter2 > _CloudRangeSquared.x;
}

float EvaluateNormalizedCloudHeight(float3 positionPS)
{
    return (length(positionPS) - (_LowestCloudAltitude + _EarthRadius)) / ((_HighestCloudAltitude + _EarthRadius) - (_LowestCloudAltitude + _EarthRadius));
}

float3 AnimateCloudMapPosition(float3 positionPS)
{
    return positionPS + float3(_WindVector.x, 0.0, _WindVector.y) * _LargeWindSpeed;
}

float3 AnimateBaseNoisePosition(float3 positionPS)
{
    positionPS.y += (positionPS.x / 3.0f + positionPS.z / 7.0f);
    return positionPS + float3(_WindVector.x, 0.0, _WindVector.y) * _MediumWindSpeed + float3(0.0, _VerticalShapeWindDisplacement, 0.0);
}

float3 AnimateFineNoisePosition(float3 positionPS)
{
    return positionPS + float3(_WindVector.x, 0.0, _WindVector.y) * _SmallWindSpeed + float3(0.0, _VerticalErosionWindDisplacement, 0.0);
}

void GetCloudCoverageData(float3 positionPS, out CloudCoverageData data)
{
    float2 normalizedPosition = AnimateCloudMapPosition(positionPS).xz / _NormalizationFactor * _CloudMapTiling.xy + _CloudMapTiling.zw - 0.5;
    float4 cloudMapData = SAMPLE_TEXTURE2D_LOD(_CloudMapTexture, sampler_LinearRepeat, float2(normalizedPosition), 0);
    data.coverage = float2(cloudMapData.x, cloudMapData.x * cloudMapData.x);
    data.rainClouds = cloudMapData.y;
    data.cloudType = cloudMapData.z;
    data.maxCloudHeight = cloudMapData.w;
}

void EvaluateCloudProperties(float3 positionWS, float noiseMipOffset, float erosionMipOffset, bool cheapVersion, bool lightSampling,
                             out CloudProperties properties)
{
    float3 positionPS = positionWS + float3(0, _EarthRadius, 0);
    ZERO_INITIALIZE(CloudProperties, properties);
    properties.ambientOcclusion = 1.0;

    if (!PointInsideCloudVolume(positionPS) || positionPS.y < 0.0f)
        return;

    properties.height = EvaluateNormalizedCloudHeight(positionPS);

    float3 baseNoiseSamplingCoordinates = float3(AnimateBaseNoisePosition(positionPS).xzy / NOISE_TEXTURE_NORMALIZATION_FACTOR) * _ShapeScale - float3(_ShapeNoiseOffset.x, _ShapeNoiseOffset.y, _VerticalShapeNoiseOffset);
    baseNoiseSamplingCoordinates += properties.height * float3(_WindDirection.x, _WindDirection.y, 0.0f) * _AltitudeDistortion;

    float lowFrequencyNoise = SAMPLE_TEXTURE3D_LOD(_Worley128RGBA, sampler_TrilinearRepeat, baseNoiseSamplingCoordinates.xyz, noiseMipOffset);

    float shapeFactor = lerp(0.1, 1.0, _ShapeFactor);
    float erosionFactor = _ErosionFactor;

    CloudCoverageData cloudCoverageData;
    GetCloudCoverageData(positionPS, cloudCoverageData);

    if (cloudCoverageData.coverage.x <= CLOUD_DENSITY_TRESHOLD || cloudCoverageData.maxCloudHeight < properties.height)
        return;

    float3 densityErosionAO = SAMPLE_TEXTURE2D_LOD(_CloudLutTexture, sampler_LinearClamp, float2(cloudCoverageData.cloudType, properties.height), CLOUD_LUT_MIP_OFFSET);

    shapeFactor = shapeFactor * densityErosionAO.y;
    erosionFactor = erosionFactor * densityErosionAO.y;

    lowFrequencyNoise = lerp(1.0, lowFrequencyNoise, shapeFactor);
    float base_cloud = 1.0 - densityErosionAO.x * cloudCoverageData.coverage.x * (1.0 - shapeFactor);
    base_cloud = saturate(DensityRemap(lowFrequencyNoise, base_cloud, 1.0, 0.0, 1.0)) ;

    properties.ambientOcclusion = densityErosionAO.z;
    properties.sigmaT = lerp(0.04, 0.12, cloudCoverageData.rainClouds);

    float ambientOcclusionBlend = saturate(1.0 - max(erosionFactor, shapeFactor) * 0.5);
    properties.ambientOcclusion = lerp(1.0, properties.ambientOcclusion, ambientOcclusionBlend);

    if (!cheapVersion)
    {
        float3 fineNoiseSamplingCoordinates = AnimateFineNoisePosition(positionPS) / NOISE_TEXTURE_NORMALIZATION_FACTOR * _ErosionScale;
        float highFrequencyNoise = 1.0 - SAMPLE_TEXTURE3D_LOD(_ErosionNoise, sampler_LinearRepeat, fineNoiseSamplingCoordinates, CLOUD_DETAIL_MIP_OFFSET + erosionMipOffset).x;
        highFrequencyNoise = lerp(0.0, highFrequencyNoise, erosionFactor * 0.75f * cloudCoverageData.coverage.x * _ErosionFactorCompensation);
        base_cloud = DensityRemap(base_cloud, highFrequencyNoise, 1.0, 0.0, 1.0);
        properties.ambientOcclusion = saturate(properties.ambientOcclusion - sqrt(highFrequencyNoise * _ErosionOcclusion));
    }

    if (lightSampling)
        base_cloud -= erosionFactor * 0.1;

    base_cloud = max(0, base_cloud);
    properties.density = base_cloud * _DensityMultiplier;
}

//--------------------------------------------------------------------------------------------------
// Ray marching
//--------------------------------------------------------------------------------------------------
struct VolumetricRayResult
{
    float3 inScattering;
    float  transmittance;
    float  meanDistance;
    bool   invalidRay;
};

bool GetCloudVolumeIntersection_Light(float3 originWS, float3 dir, out float totalDistance)
{
    float2 intersection;
    RaySphereIntersection(originWS, dir, _HighestCloudAltitude + _EarthRadius, intersection);
    bool intersectEarth = RaySphereIntersection(originWS, dir, _EarthRadius);
    totalDistance = intersection.x;
    return !intersectEarth;
}

float3 EvaluateSunLuminance(float3 positionWS, float3 sunDirection, float3 sunColor, float powderEffect, PHASE_FUNCTION_STRUCTURE phaseFunction)
{
    float totalLightDistance = 0.0;
    float3 luminance = float3(0.0, 0.0, 0.0);

    if (GetCloudVolumeIntersection_Light(positionWS, sunDirection, totalLightDistance))
    {
        totalLightDistance = clamp(totalLightDistance, 0, _NumLightSteps * LIGHT_STEP_MAXIMAL_SIZE);
        totalLightDistance += 5.0f;

        float intervalSize = totalLightDistance / (float)_NumLightSteps;
        float extinctionSum = 0;

        for (int j = 0; j < _NumLightSteps; j++)
        {
            float dist = intervalSize * (0.25 + j);
            float3 currentSamplePointWS = positionWS + sunDirection * dist;
            CloudProperties lightRayCloudProperties;
            EvaluateCloudProperties(currentSamplePointWS, 3.0f * j / _NumLightSteps, 0.0, true, true, lightRayCloudProperties);
            extinctionSum += max(lightRayCloudProperties.density * lightRayCloudProperties.sigmaT, 1e-6);
        }

        float3 sunColorXPowderEffect = sunColor * powderEffect;
        float3 extinction = intervalSize * _ScatteringTint.xyz * extinctionSum;
        for (int o = 0; o < NUM_MULTI_SCATTERING_OCTAVES; ++o)
        {
            float msFactor = PositivePow(_MultiScattering, o);
            float3 transmittance = exp(-extinction * msFactor);
            luminance += transmittance * sunColorXPowderEffect * phaseFunction[o] * msFactor;
        }
    }
    return luminance;
}

void EvaluateCloud(CloudProperties cloudProperties, EnvironmentLighting envLighting,
                   float3 currentPositionWS, float stepSize, float relativeRayDistance,
                   inout VolumetricRayResult volumetricRay)
{
    const float extinction = cloudProperties.density * cloudProperties.sigmaT;
    const float transmittance = exp(-extinction * stepSize);

    float powder_effect = PowderEffect(cloudProperties.density, envLighting.cosAngle, _PowderEffectIntensity);
    float3 sunColor = lerp(envLighting.sunColor0, envLighting.sunColor1, relativeRayDistance);
    float3 totalLuminance = EvaluateSunLuminance(currentPositionWS, envLighting.sunDirection, sunColor, powder_effect, envLighting.phaseFunction);
   // totalLuminance += lerp(envLighting.ambientTermBottom, envLighting.ambientTermTop, cloudProperties.height) * cloudProperties.ambientOcclusion;

    const float3 integScatt = (totalLuminance - totalLuminance * transmittance);
    volumetricRay.inScattering += integScatt * volumetricRay.transmittance;
    volumetricRay.transmittance *= transmittance;
}

float DensityFadeValue(float distanceToCamera)
{
    return saturate((distanceToCamera - _FadeInStart) / (_FadeInStart + _FadeInDistance));
}

float ErosionMipOffset(float distanceToCamera)
{
    return lerp(0.0, 4.0, saturate((distanceToCamera - MIN_EROSION_DISTANCE) / (MAX_EROSION_DISTANCE - MIN_EROSION_DISTANCE)));
}

VolumetricRayResult TraceVolumetricRay(CloudRay cloudRay)
{
    VolumetricRayResult volumetricRay;
    volumetricRay.inScattering = 0.0;
    volumetricRay.transmittance = 1.0;
    volumetricRay.meanDistance = _MaxCloudDistance;
    volumetricRay.invalidRay = true;

    RayMarchRange rayMarchRange;
    if (GetCloudVolumeIntersection(cloudRay.originWS, cloudRay.direction, cloudRay.insideClouds, cloudRay.toEarthCenter, rayMarchRange))
    {
        if (cloudRay.maxRayLength >= rayMarchRange.start)
        {
            volumetricRay.meanDistance = 0.0;
            float totalDistance = min(rayMarchRange.distance, cloudRay.maxRayLength - rayMarchRange.start);

            cloudRay.envLighting = EvaluateEnvironmentLighting(cloudRay);

            float stepS = totalDistance / (float)_NumPrimarySteps;
            int currentIndex = 0;
            float meanDistanceDivider = 0.0f;
            float3 currentPositionWS = cloudRay.originWS + rayMarchRange.start * cloudRay.direction;
            float currentDistance = 0;
            bool activeSampling = true;
            int sequentialEmptySamples = 0;

            while (currentIndex < _NumPrimarySteps && currentDistance < totalDistance)
            {
                float densityAttenuationValue = DensityFadeValue(rayMarchRange.start + currentDistance);
                float erosionMipOffset = ErosionMipOffset(rayMarchRange.start + currentDistance);

                if (activeSampling)
                {
                    CloudProperties cloudProperties;
                    EvaluateCloudProperties(currentPositionWS, 0.0f, erosionMipOffset, false, false, cloudProperties);
                    cloudProperties.density *= densityAttenuationValue;

                    if (cloudProperties.density > CLOUD_DENSITY_TRESHOLD)
                    {
                        float transmitanceXdensity = volumetricRay.transmittance * cloudProperties.density;
                        volumetricRay.meanDistance += (rayMarchRange.start + currentDistance) * transmitanceXdensity;
                        meanDistanceDivider += transmitanceXdensity;

                        EvaluateCloud(cloudProperties, cloudRay.envLighting, currentPositionWS, stepS, currentDistance / totalDistance, volumetricRay);

                        if (volumetricRay.transmittance < 0.003)
                        {
                            volumetricRay.transmittance = 0.0;
                            break;
                        }
                        sequentialEmptySamples = 0;
                    }
                    else
                        sequentialEmptySamples++;

                    if (sequentialEmptySamples == EMPTY_STEPS_BEFORE_LARGE_STEPS)
                        activeSampling = false;

                    float relativeStepSize = lerp(cloudRay.integrationNoise, 1.0, saturate(currentIndex));
                    currentPositionWS += cloudRay.direction * stepS * relativeStepSize;
                    currentDistance += stepS * relativeStepSize;
                }
                else
                {
                    CloudProperties cloudProperties;
                    EvaluateCloudProperties(currentPositionWS, 1.0f, 0.0, true, false, cloudProperties);
                    cloudProperties.density *= densityAttenuationValue;

                    if (cloudProperties.density < CLOUD_DENSITY_TRESHOLD)
                    {
                        currentPositionWS += cloudRay.direction * stepS * 2.0f;
                        currentDistance += stepS * 2.0f;
                    }
                    else
                    {
                        currentPositionWS -= cloudRay.direction * stepS;
                        currentDistance -= stepS;
                        activeSampling = true;
                        sequentialEmptySamples = 0;
                    }
                }
                currentIndex++;
            }

            if (volumetricRay.meanDistance == 0.0)
                volumetricRay.invalidRay = true;
            else
            {
                volumetricRay.meanDistance /= meanDistanceDivider;
                volumetricRay.invalidRay = false;
            }
        }
    }
    return volumetricRay;
}

//--------------------------------------------------------------------------------------------------
// Shared temporal helpers
//--------------------------------------------------------------------------------------------------
int ComputeCheckerBoardIndex(int2 traceCoord, int subPixelIndex)
{
    int localOffset = ((traceCoord.x & 1) + (traceCoord.y & 1)) & 1;
    int checkerBoardLocation = (subPixelIndex + localOffset) & 0x3;
    return checkerBoardLocation;
}

uint2 HalfResolutionIndexToOffset(uint index)
{
    return uint2(index & 0x1, index / 2);
}

float EvaluateFinalTransmittance(float3 color, float transmittance)
{
    float luminance = Luminance(color);
    float resultLuminance = luminance / (1.0 + luminance) * transmittance;
    resultLuminance = resultLuminance / (1.0 - resultLuminance);
    float finalTransmittance = max(resultLuminance / luminance, pow(transmittance, 6));
    return luminance > 0.0 ? lerp(transmittance, finalTransmittance, _ImprovedTransmittanceBlend) : transmittance;
}

// Reprojection of the cloud position.
// positionFlag = 1 => finite world position at 'cloudDistance' (local clouds, includes parallax/translation)
// positionFlag = 0 => point at infinity in the view direction (distant clouds, camera rotation only)
float2 EvaluateCloudMotionVectors(float2 coord, float2 screenSize, float cloudDistance, float positionFlag)
{
    float3 dir = GetCloudViewDirWS(coord, screenSize);
    float3 worldPos = _CloudsCameraPosWS + dir * cloudDistance;
    float4 world = float4(worldPos, positionFlag); // w = 0 => direction, w = 1 => position
    float4 prevClipPos = mul(_CloudsPrevVP, world);
    float4 curClipPos = mul(_CloudsVP, world);
    float2 previousPositionCS = prevClipPos.xy / prevClipPos.w;
    float2 positionCS = curClipPos.xy / curClipPos.w;
    // Convert from clip (-1..1) to NDC (0..1) delta
    return (positionCS - previousPositionCS) * 0.5;
}

// AABB clip of the history towards the current 3x3 neighborhood (ghosting reduction).
float4 ClipCloudsToRegion(float4 history, float4 minimum, float4 maximum, inout float validityFactor)
{
    float clampedTransmittance = clamp(history.w, minimum.w, maximum.w);
    float3 center  = 0.5 * (maximum.xyz + minimum.xyz);
    float3 extents = 0.5 * (maximum.xyz - minimum.xyz) + 1e-4;
    float3 offset = history.xyz - center;
    float3 v_unit = offset.xyz / extents.xyz;
    float3 absUnit = abs(v_unit);
    float maxUnit = Max3(absUnit.x, absUnit.y, absUnit.z);
    validityFactor *= maxUnit > 1.0 ? 0.5 : 1.0;
    if (maxUnit > 1.0)
        return float4(center + (offset / maxUnit), clampedTransmittance);
    else
        return float4(history.xyz, clampedTransmittance);
}

#endif // URP_VOLUMETRIC_CLOUDS_UTILITIES_H
