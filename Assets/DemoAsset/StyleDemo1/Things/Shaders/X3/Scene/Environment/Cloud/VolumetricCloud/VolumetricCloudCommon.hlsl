#ifndef SHIYUE_VOLUMETRIC_CLOUD_COMMON
#define SHIYUE_VOLUMETRIC_CLOUD_COMMON

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

TEXTURE2D(_VolumetricCloudMinDepthTex);       SAMPLER(sampler_VolumetricCloudMinDepthTex);
TEXTURE2D(_VolumetricCloudCoarseHitTex);      SAMPLER(sampler_VolumetricCloudCoarseHitTex);
TEXTURE2D(_VolumetricCloudRefinedHitTex);     SAMPLER(sampler_VolumetricCloudRefinedHitTex);
TEXTURE2D(_VolumetricCloudColorTex);          SAMPLER(sampler_VolumetricCloudColorTex);

TEXTURE2D(_VolumetricCloudMapTex);            SAMPLER(sampler_VolumetricCloudMapTex);
TEXTURE3D(_VolumetricCloudDistanceFieldAtlasTex);     SAMPLER(sampler_VolumetricCloudDistanceFieldAtlasTex);
TEXTURE3D(_VolumetricCloudLightingColorTex);           SAMPLER(sampler_VolumetricCloudLightingColorTex);
TEXTURE3D(_VolumetricCloudLightingDataTex);            SAMPLER(sampler_VolumetricCloudLightingDataTex);
TEXTURE3D(_VolumetricCloudCoverageTex);                SAMPLER(sampler_VolumetricCloudCoverageTex);
TEXTURE3D(_VolumetricCloudAmbientOcclusionTex);        SAMPLER(sampler_VolumetricCloudAmbientOcclusionTex);
TEXTURE3D(_VolumetricCloudDistanceIndirectionTex);     SAMPLER(sampler_VolumetricCloudDistanceIndirectionTex);
TEXTURE3D(_VolumetricCloudDistanceFallbackTex);        SAMPLER(sampler_VolumetricCloudDistanceFallbackTex);
TEXTURE2D(_VolumetricCloudWarpNoiseTex);               SAMPLER(sampler_VolumetricCloudWarpNoiseTex);

float3 _VolumetricCloudBoundsMin;
float3 _VolumetricCloudBoundsMax;
float4 _VolumetricCloudFrameParams;       // xy: frame size, zw: inv frame size
float4 _VolumetricCloudStepParams;        // x: coarse, y: refine, z: final, w: refine range
float4 _VolumetricCloudDensityParams;     // x: density, y: coverage, z: extinction, w: max distance
float4 _VolumetricCloudLightingParams;    // x: light intensity, y: silver lining, z: phase g, w: detail strength
float4 _VolumetricCloudWindParams;        // xyz: wind dir, w: speed
float4 _VolumetricCloudLightDir;
float4 _VolumetricCloudLightColor;
float4 _VolumetricCloudCloudColor;
float4 _VolumetricCloudAmbientColor;
float _VolumetricCloudUseTextureInputs;
float _VolumetricCloudBaseTiling;
float _VolumetricCloudDetailTiling;
float3 _VolumetricCloudRayBasis0;
float3 _VolumetricCloudRayBasis1;
float3 _VolumetricCloudRayBasis2;
float4 _VolumetricCloudCaptureParams0;    // x: depth scale, y: warp strength, z: atlas scale, w: brick scale
float4 _VolumetricCloudCaptureParams1;    // xyz: AABB/brick padding params, w: lighting brick interpolation
float4 _VolumetricCloudCaptureParams2;    // x: hit bias, y: trace threshold scale, z: scene end padding
float4 _VolumetricCloudCaptureParams3;    // x: shadow density scale, y: base opacity bias, z: fallback scale, w: atlas scale

struct CloudVertexInput
{
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct CloudVaryings
{
    float4 positionCS : SV_POSITION;
    float2 texcoord : TEXCOORD0;
    float4 rayData : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};

struct CloudRay
{
    float3 origin;
    float3 direction;
    float invLength;
    float footprint;
};

CloudVaryings CloudVert(CloudVertexInput input)
{
    CloudVaryings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float x = input.vertexID == 0 ? -3.0 : 1.0;
    float y = input.vertexID == 2 ? 3.0 : -1.0;
    output.positionCS = float4(x, y, 0.0, 1.0);
    output.texcoord = output.positionCS.xy * 0.5 + 0.5;
#if UNITY_UV_STARTS_AT_TOP
    output.texcoord.y = 1.0 - output.texcoord.y;
#endif

    float3 rayVector = _VolumetricCloudRayBasis0 + _VolumetricCloudRayBasis1 * output.texcoord.x + _VolumetricCloudRayBasis2 * output.texcoord.y;
    output.rayData.xyz = rayVector;
    output.rayData.w = 0.5 * length(_VolumetricCloudRayBasis1 + _VolumetricCloudRayBasis2) / max(length(_VolumetricCloudFrameParams.xy), 1.0);
    return output;
}

float CloudRawFarDepth()
{
#if defined(UNITY_REVERSED_Z)
    return 0.0;
#else
    return 1.0;
#endif
}

float3 CloudFarWorldPosition(float2 uv)
{
    return ComputeWorldSpacePosition(uv, CloudRawFarDepth(), UNITY_MATRIX_I_VP);
}

CloudRay BuildCloudRay(float4 rayData)
{
    CloudRay ray;
    ray.origin = _WorldSpaceCameraPos.xyz;
    float3 rayVector = rayData.xyz;
    ray.invLength = rsqrt(max(dot(rayVector, rayVector), 1e-8));
    ray.direction = rayVector * ray.invLength;
    ray.footprint = rayData.w;
    return ray;
}

float3 CloudSceneWorldPosition(float2 uv, float rawDepth)
{
    return ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
}

float CloudSceneDistance(float2 uv, float rawDepth)
{
#if defined(UNITY_REVERSED_Z)
    if (rawDepth <= 0.000001)
    {
        return _VolumetricCloudDensityParams.w;
    }
#else
    if (rawDepth >= 0.999999)
    {
        return _VolumetricCloudDensityParams.w;
    }
#endif

    return min(distance(_WorldSpaceCameraPos.xyz, CloudSceneWorldPosition(uv, rawDepth)), _VolumetricCloudDensityParams.w);
}

float2 IntersectCloudBox(float3 origin, float3 direction)
{
    float3 dirSign = float3(direction.x >= 0.0 ? 1.0 : -1.0, direction.y >= 0.0 ? 1.0 : -1.0, direction.z >= 0.0 ? 1.0 : -1.0);
    float3 safeDirection = max(abs(direction), 1e-5) * dirSign;
    float3 invDirection = rcp(safeDirection);
    float3 t0 = (_VolumetricCloudBoundsMin - origin) * invDirection;
    float3 t1 = (_VolumetricCloudBoundsMax - origin) * invDirection;
    float3 tMin3 = min(t0, t1);
    float3 tMax3 = max(t0, t1);
    float tMin = max(max(tMin3.x, tMin3.y), tMin3.z);
    float tMax = min(min(tMax3.x, tMax3.y), tMax3.z);
    return float2(max(tMin, 0.0), tMax);
}

float DecodeCaptureDistance(float reciprocalDepth, float invRayLength)
{
    return _VolumetricCloudCaptureParams0.x / max(reciprocalDepth, 1e-6) / max(invRayLength, 1e-6);
}

float EncodeCaptureDistance(float distanceT, float invRayLength)
{
    float normalizedDistance = max(distanceT * invRayLength / max(_VolumetricCloudCaptureParams0.x, 1e-6), 0.0);
    return normalizedDistance > 0.0 ? rcp(normalizedDistance) : 0.0;
}

float Min4(float4 value)
{
    return min(min(value.x, value.y), min(value.z, value.w));
}

float4 GatherSceneRawDepth(float2 uv)
{
    float2 texel = _VolumetricCloudFrameParams.zw;
    return float4(
        SampleSceneDepth(uv + texel * float2(-0.5, -0.5)),
        SampleSceneDepth(uv + texel * float2(0.5, -0.5)),
        SampleSceneDepth(uv + texel * float2(-0.5, 0.5)),
        SampleSceneDepth(uv + texel * float2(0.5, 0.5)));
}

float GatherMinCloudDepthFallback(float2 uv)
{
    float2 texel = _VolumetricCloudFrameParams.zw * 4.0;
    float4 samples = float4(
        SAMPLE_TEXTURE2D_LOD(_VolumetricCloudMinDepthTex, sampler_VolumetricCloudMinDepthTex, uv + texel * float2(-0.5, -0.5), 0).r,
        SAMPLE_TEXTURE2D_LOD(_VolumetricCloudMinDepthTex, sampler_VolumetricCloudMinDepthTex, uv + texel * float2(0.5, -0.5), 0).r,
        SAMPLE_TEXTURE2D_LOD(_VolumetricCloudMinDepthTex, sampler_VolumetricCloudMinDepthTex, uv + texel * float2(-0.5, 0.5), 0).r,
        SAMPLE_TEXTURE2D_LOD(_VolumetricCloudMinDepthTex, sampler_VolumetricCloudMinDepthTex, uv + texel * float2(0.5, 0.5), 0).r);
    return max(Min4(samples), 0.0);
}

float3 CaptureBoundsMin()
{
    return _VolumetricCloudBoundsMin + _VolumetricCloudCaptureParams1.xxx;
}

float3 CaptureBoundsMax()
{
    return _VolumetricCloudBoundsMax - _VolumetricCloudCaptureParams1.xxx;
}

float Hash13(float3 p)
{
    p = frac(p * 0.1031);
    p += dot(p, p.yzx + 33.33);
    return frac((p.x + p.y) * p.z);
}

float CloudScreenJitter(float2 uv)
{
    float2 pixel = floor(uv * _VolumetricCloudFrameParams.xy);
    return Hash13(float3(pixel, _Time.y));
}

float ValueNoise(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);

    float n000 = Hash13(i + float3(0, 0, 0));
    float n100 = Hash13(i + float3(1, 0, 0));
    float n010 = Hash13(i + float3(0, 1, 0));
    float n110 = Hash13(i + float3(1, 1, 0));
    float n001 = Hash13(i + float3(0, 0, 1));
    float n101 = Hash13(i + float3(1, 0, 1));
    float n011 = Hash13(i + float3(0, 1, 1));
    float n111 = Hash13(i + float3(1, 1, 1));

    float nx00 = lerp(n000, n100, f.x);
    float nx10 = lerp(n010, n110, f.x);
    float nx01 = lerp(n001, n101, f.x);
    float nx11 = lerp(n011, n111, f.x);
    float nxy0 = lerp(nx00, nx10, f.y);
    float nxy1 = lerp(nx01, nx11, f.y);
    return lerp(nxy0, nxy1, f.z);
}

float ProceduralCloudNoise(float3 p)
{
    float value = 0.0;
    value += ValueNoise(p) * 0.55;
    value += ValueNoise(p * 2.03 + 17.1) * 0.30;
    value += ValueNoise(p * 4.01 + 53.7) * 0.15;
    return value;
}

float NormalizedHeight(float3 worldPos)
{
    return saturate((worldPos.y - _VolumetricCloudBoundsMin.y) / max(_VolumetricCloudBoundsMax.y - _VolumetricCloudBoundsMin.y, 1e-4));
}

float HeightGradient(float height01)
{
    float bottom = smoothstep(0.02, 0.18, height01);
    float top = 1.0 - smoothstep(0.72, 1.0, height01);
    return saturate(bottom * top);
}

float2 CloudWarpUV(float3 worldPos)
{
    float2 uv0 = float2(
        0.0932 * _VolumetricCloudBaseTiling + 0.1300 * (worldPos.y + worldPos.x * 0.2),
        0.1865 * _VolumetricCloudBaseTiling + 0.0650 * (-worldPos.z + worldPos.y * 0.2));
    float2 uv1 = float2(
        0.3730 * _VolumetricCloudBaseTiling + 0.0325 * (-worldPos.x + worldPos.z * 0.2),
        1.3520 * _VolumetricCloudBaseTiling + worldPos.y * 0.2);

    float2 n0 = SAMPLE_TEXTURE2D_LOD(_VolumetricCloudWarpNoiseTex, sampler_VolumetricCloudWarpNoiseTex, uv0 * 0.1592, 0).xy;
    float2 n1 = SAMPLE_TEXTURE2D_LOD(_VolumetricCloudWarpNoiseTex, sampler_VolumetricCloudWarpNoiseTex, uv1 * 0.1592, 0).xy;
    return float2(n0.x * 0.25 + n0.y * 0.5 + n1.x, n1.y);
}

float SampleTraceDistanceField273280(float3 worldPos)
{
    if (_VolumetricCloudUseTextureInputs < 0.5)
    {
        float density = ProceduralCloudNoise(worldPos * max(_VolumetricCloudBaseTiling, 1e-6));
        return lerp(24.0, 0.5, saturate(density - _VolumetricCloudDensityParams.y));
    }

    float2 warp = CloudWarpUV(worldPos);
    worldPos.y += _VolumetricCloudCaptureParams0.y * (warp.x - 1.0) * 2.0;
    float distanceBias = -_VolumetricCloudCaptureParams0.y * 0.3 * warp.y;

    float3 brickCoord = (worldPos - _VolumetricCloudBoundsMin) * _VolumetricCloudCaptureParams0.w;
    float3 brickIndex = floor(brickCoord);
    float3 boundsSize = max(_VolumetricCloudBoundsMax - _VolumetricCloudBoundsMin, 1e-4);
    float3 brickCenter = (brickIndex + 0.5) * (16.0 / boundsSize);
    float2 indirection = SAMPLE_TEXTURE3D_LOD(_VolumetricCloudDistanceIndirectionTex, sampler_VolumetricCloudDistanceIndirectionTex, brickCenter, 0).xy;

    if (indirection.x + indirection.y == 0.0)
    {
        return SAMPLE_TEXTURE3D_LOD(_VolumetricCloudDistanceFallbackTex, sampler_VolumetricCloudDistanceFallbackTex, brickCenter, 0).x * _VolumetricCloudCaptureParams3.z;
    }

    float3 local = frac(brickCoord);
    local = local * (1.0 - _VolumetricCloudCaptureParams1.z) + _VolumetricCloudCaptureParams1.z * 0.5;
    float2 atlasXY = (indirection * 255.0 + local.xy) * _VolumetricCloudCaptureParams0.z;
    float3 atlasUVW = float3(atlasXY, local.z);
    float fieldDistance = SAMPLE_TEXTURE3D_LOD(_VolumetricCloudDistanceFieldAtlasTex, sampler_VolumetricCloudDistanceFieldAtlasTex, atlasUVW, 0).x * _VolumetricCloudCaptureParams3.w;
    return fieldDistance + distanceBias;
}

float SampleDistanceFieldForShading287(float3 worldPos)
{
    return SampleTraceDistanceField273280(worldPos);
}

float SampleCloudDensity(float3 worldPos)
{
    float distanceField = SampleDistanceFieldForShading287(worldPos);
    float density = saturate(_VolumetricCloudCaptureParams3.y - _VolumetricCloudCaptureParams3.x * distanceField);
    density *= HeightGradient(NormalizedHeight(worldPos));
    return density * _VolumetricCloudDensityParams.x;
}

float2 IntersectCaptureCloudBox(float3 origin, float3 direction, float startT, float endT)
{
    float3 boundsMin = CaptureBoundsMin();
    float3 boundsMax = CaptureBoundsMax();
    float3 dirSign = float3(direction.x >= 0.0 ? 1.0 : -1.0, direction.y >= 0.0 ? 1.0 : -1.0, direction.z >= 0.0 ? 1.0 : -1.0);
    float3 invDirection = rcp(max(abs(direction), 1e-5) * dirSign);
    float3 nearT = (boundsMin - origin) * invDirection;
    float3 farT = (boundsMax - origin) * invDirection;
    float3 t0 = min(nearT, farT);
    float3 t1 = max(nearT, farT);
    float enterT = max(max(t0.x, t0.y), t0.z);
    float exitT = min(min(t1.x, t1.y), t1.z);

    if (enterT > exitT)
    {
        return float2(exitT, exitT);
    }

    startT = max(min(startT, exitT), enterT);
    endT = min(endT, exitT);
    return float2(startT, endT);
}

float TraceCaptureDistance273280(float4 rayData, float startT, float endT, int maxSteps)
{
    CloudRay ray = BuildCloudRay(rayData);
    float2 rayRange = IntersectCaptureCloudBox(ray.origin, ray.direction, startT, endT);

    float t = rayRange.x;

    [loop]
    for (int i = 0; i < maxSteps; ++i)
    {
        if (t >= rayRange.y)
        {
            break;
        }

        float stepDistance = SampleTraceDistanceField273280(ray.origin + ray.direction * t);
        t += stepDistance;

        float threshold = abs(t) * ray.footprint + _VolumetricCloudCaptureParams2.x;
        if (stepDistance <= threshold)
        {
            return max(t - threshold, 0.0);
        }
    }

    return 0.0;
}

float MarchCloudHitEvent273(float2 uv, float4 rayData, int stepCount)
{
    CloudRay ray = BuildCloudRay(rayData);
    float minDepthRaw = GatherMinCloudDepthFallback(uv);
    float startT = _VolumetricCloudCaptureParams0.x / max(ray.invLength, 1e-6);
    float endT = DecodeCaptureDistance(max(minDepthRaw, 1e-6), ray.invLength) + _VolumetricCloudCaptureParams2.z;
    float hitT = TraceCaptureDistance273280(rayData, startT, endT, stepCount);
    return hitT > 0.0 && hitT < endT ? EncodeCaptureDistance(hitT, ray.invLength) : 0.0;
}

float MarchCloudHitEvent280(float2 uv, float4 rayData, float coarseHit, int stepCount)
{
    CloudRay ray = BuildCloudRay(rayData);
    float minDepthRaw = GatherMinCloudDepthFallback(uv);
    float startT = DecodeCaptureDistance(coarseHit, ray.invLength);
    float endT = DecodeCaptureDistance(max(minDepthRaw, 1e-6), ray.invLength) + _VolumetricCloudCaptureParams2.z;
    float hitT = TraceCaptureDistance273280(rayData, startT, endT, stepCount);
    return hitT > 0.0 ? EncodeCaptureDistance(hitT, ray.invLength) : 0.0;
}

float HenyeyGreenstein(float cosTheta, float g)
{
    float g2 = g * g;
    return (1.0 - g2) / max(12.56637 * pow(abs(1.0 + g2 - 2.0 * g * cosTheta), 1.5), 1e-4);
}

float3 CaptureVolumeUVW(float3 worldPos)
{
    float3 boundsSize = max(_VolumetricCloudBoundsMax - _VolumetricCloudBoundsMin, 1e-4);
    float3 uvw = saturate((worldPos - _VolumetricCloudBoundsMin) / boundsSize);
    uvw.z = uvw.z * 0.75 + 0.125;
    return uvw;
}

float3 CaptureCloudMapUV(float3 worldPos)
{
    float3 boundsSize = max(_VolumetricCloudBoundsMax - _VolumetricCloudBoundsMin, 1e-4);
    float3 uvw = saturate((worldPos - _VolumetricCloudBoundsMin) / boundsSize);
    return float3(uvw.xz, uvw.y);
}

// Event 287 里，着色属性（颜色/数据/覆盖）和距离场共用同一套稀疏砖块 indirection：
// 先在 brick 中心查 indirection，再算出 atlas 坐标，属性图都在这个 atlasUVW 上采样。
bool CaptureBrickAtlasUVW(float3 worldPos, out float3 atlasUVW)
{
    float3 brickCoord = (worldPos - _VolumetricCloudBoundsMin) * _VolumetricCloudCaptureParams0.w;
    float3 brickIndex = floor(brickCoord);
    float3 boundsSize = max(_VolumetricCloudBoundsMax - _VolumetricCloudBoundsMin, 1e-4);
    float3 brickCenter = (brickIndex + 0.5) * (16.0 / boundsSize);
    float2 indirection = SAMPLE_TEXTURE3D_LOD(_VolumetricCloudDistanceIndirectionTex, sampler_VolumetricCloudDistanceIndirectionTex, brickCenter, 0).xy;

    if (indirection.x + indirection.y == 0.0)
    {
        atlasUVW = 0.0;
        return false;
    }

    float3 local = frac(brickCoord);
    local = local * (1.0 - _VolumetricCloudCaptureParams1.z) + _VolumetricCloudCaptureParams1.z * 0.5;
    float2 atlasXY = (indirection * 255.0 + local.xy) * _VolumetricCloudCaptureParams0.z;
    atlasUVW = float3(atlasXY, local.z);
    return true;
}

float4 SampleCaptureCloudColor(float3 worldPos)
{
    float3 atlasUVW;
    if (!CaptureBrickAtlasUVW(worldPos, atlasUVW))
    {
        // 空砖块：此处无云数据
        return float4(_VolumetricCloudCloudColor.rgb, 0.0);
    }

    // 通道解码严格对齐 Event 287 反汇编：
    //   coverage : _705.x
    //   color    : _711 -> rgb^2 / a
    //   data     : _716 -> x, y^2, w * exp2(255*z - 128)
    float coverage = SAMPLE_TEXTURE3D_LOD(_VolumetricCloudCoverageTex, sampler_VolumetricCloudCoverageTex, atlasUVW, 0).x;
    float4 colorPacked = SAMPLE_TEXTURE3D_LOD(_VolumetricCloudLightingColorTex, sampler_VolumetricCloudLightingColorTex, atlasUVW, 0);
    float4 dataPacked = SAMPLE_TEXTURE3D_LOD(_VolumetricCloudLightingDataTex, sampler_VolumetricCloudLightingDataTex, atlasUVW, 0);

    float3 lightingColor = (colorPacked.rgb * colorPacked.rgb) / max(colorPacked.w, 1e-4);
    float shadeScalar = dataPacked.x;
    float hdrTerm = dataPacked.w * exp2(255.0 * dataPacked.z - 128.0);

    float3 directColor = lightingColor * _VolumetricCloudLightColor.rgb * _VolumetricCloudLightingParams.x;
    float3 ambientColor = _VolumetricCloudAmbientColor.rgb * shadeScalar;
    float3 color = directColor + ambientColor + hdrTerm * _VolumetricCloudCloudColor.rgb;
    return float4(color, saturate(coverage));
}

float3 MarchCloudLighting(float3 startPos, float3 rayDir, float hitDistance, int stepCount, out float alpha)
{
    float stepLength = max(_VolumetricCloudStepParams.w / max(stepCount, 1), 0.25);
    float t = max(hitDistance - stepLength, 0.0);
    float transmittance = 1.0;
    float3 scattering = 0.0;
    float3 lightDir = normalize(_VolumetricCloudLightDir.xyz);
    float phase = HenyeyGreenstein(dot(rayDir, lightDir), _VolumetricCloudLightingParams.z);
    float silver = pow(saturate(dot(rayDir, lightDir)), 8.0) * _VolumetricCloudLightingParams.y;

    [loop]
    for (int i = 0; i < stepCount; ++i)
    {
        float3 positionWS = startPos + rayDir * t;
        float density = SampleCloudDensity(positionWS);
        float4 captureColor = SampleCaptureCloudColor(positionWS);
        float opacity = saturate(max(density, captureColor.a) * _VolumetricCloudDensityParams.z * stepLength);
        float lightVisibility = exp(-density * _VolumetricCloudCaptureParams2.y);
        float3 phaseLight = _VolumetricCloudLightColor.rgb * _VolumetricCloudLightingParams.x * (phase * 4.0 + silver);
        float3 sampleColor = captureColor.rgb + phaseLight * lightVisibility;

        scattering += transmittance * opacity * sampleColor;
        float extinction = density * _VolumetricCloudDensityParams.z * stepLength;
        transmittance *= exp(-extinction);

        if (transmittance < 0.02)
        {
            break;
        }

        t += stepLength;
    }

    alpha = saturate(1.0 - transmittance);
    return scattering;
}

#endif
