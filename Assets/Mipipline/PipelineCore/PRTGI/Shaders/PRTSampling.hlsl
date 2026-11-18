#ifndef PIPELINE_CORE_PRT_SAMPLING_INCLUDED
#define PIPELINE_CORE_PRT_SAMPLING_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SphericalHarmonics.hlsl"

TEXTURE3D(_coefficientVoxel3D);
SAMPLER(sampler_coefficientVoxel3D);
float _PRTEnabled;
float3 _PRTGridOrigin;
float3 _PRTGridCount;
float _PRTGridSpacing;
float _PRTIndirectFill;
float3 _PRTSlotCount;
int _PRTCascadeCount;
float3 _PRTCascadeCenter;
float4 _PRTCascadeMin[4];
float4 _PRTCascadeWindowCount[4];
float4 _PRTCascadeLevelCount[4];

float PRTCascadeWeight(int level, float3 node, float3 minimum, float3 count)
{
    if (level + 1 >= _PRTCascadeCount) return 1.0;
    float3 camera = (_PRTCascadeCenter - _PRTGridOrigin) / (_PRTGridSpacing * exp2((float)level));
    camera = clamp(camera, minimum, minimum + count - 1.0);
    float weight = 1.0;
    float3 domain = _PRTCascadeLevelCount[level].xyz;
    [unroll] for (int axis = 0; axis < 3; axis++)
    {
        if (domain[axis] <= count[axis]) continue;
        float radius = max((count[axis] - 1.0) * 0.5 - 1.0, 0.0);
        weight = min(weight, saturate(min(node[axis] - (camera[axis] - radius),
            camera[axis] + radius - node[axis])));
    }
    return weight;
}

bool PRTSampleCascade(int level, float3 baseNode, float3 normal, out float3 diffuse, out float blend)
{
    diffuse = 0;
    blend = 0;
    float3 minimum = _PRTCascadeMin[level].xyz;
    float3 count = _PRTCascadeWindowCount[level].xyz;
    float3 node = baseNode / exp2((float)level);
    float3 margin = (1.0 - step(1.5, count)) * 0.5;
    if (any(count < 1.0) || any(node < minimum - margin) || any(node > minimum + count - 1.0 + margin)) return false;
    blend = PRTCascadeWeight(level, node, minimum, count);
    if (blend <= 0) return false;
    float3 bounded = clamp(node, minimum, minimum + count - 1.0);
    float3 local = float3(frac(bounded.x / _PRTSlotCount.x) * _PRTSlotCount.x,
        bounded.y - minimum.y, frac(bounded.z / _PRTSlotCount.z) * _PRTSlotCount.z);
    float depth = _PRTSlotCount.y * 9.0 * _PRTCascadeCount;
    float3 uv = float3((local.x + 0.5) / _PRTSlotCount.x,
        (local.z + 0.5) / _PRTSlotCount.z,
        (local.y + 0.5 + level * 9.0 * _PRTSlotCount.y) / depth);
    float4 first = SAMPLE_TEXTURE3D_LOD(_coefficientVoxel3D, sampler_coefficientVoxel3D, uv, 0);
    if (first.a <= 1e-6) return false;
    float3 n = normalize(float3(normal.x, normal.z, normal.y));
    diffuse = first.rgb * kSHBasisCoef[0];
    float stride = _PRTSlotCount.y / depth;
    uv.z += stride;
    diffuse += SAMPLE_TEXTURE3D_LOD(_coefficientVoxel3D, sampler_coefficientVoxel3D, uv, 0).rgb * (kSHBasisCoef[1] * n.y * kClampedCosineCoefs[1]);
    uv.z += stride;
    diffuse += SAMPLE_TEXTURE3D_LOD(_coefficientVoxel3D, sampler_coefficientVoxel3D, uv, 0).rgb * (kSHBasisCoef[2] * n.z * kClampedCosineCoefs[2]);
    uv.z += stride;
    diffuse += SAMPLE_TEXTURE3D_LOD(_coefficientVoxel3D, sampler_coefficientVoxel3D, uv, 0).rgb * (kSHBasisCoef[3] * n.x * kClampedCosineCoefs[3]);
    uv.z += stride;
    diffuse += SAMPLE_TEXTURE3D_LOD(_coefficientVoxel3D, sampler_coefficientVoxel3D, uv, 0).rgb * (kSHBasisCoef[4] * n.x * n.y * kClampedCosineCoefs[4]);
    uv.z += stride;
    diffuse += SAMPLE_TEXTURE3D_LOD(_coefficientVoxel3D, sampler_coefficientVoxel3D, uv, 0).rgb * (kSHBasisCoef[5] * n.y * n.z * kClampedCosineCoefs[5]);
    uv.z += stride;
    diffuse += SAMPLE_TEXTURE3D_LOD(_coefficientVoxel3D, sampler_coefficientVoxel3D, uv, 0).rgb * (kSHBasisCoef[6] * (3 * n.z * n.z - 1) * kClampedCosineCoefs[6]);
    uv.z += stride;
    diffuse += SAMPLE_TEXTURE3D_LOD(_coefficientVoxel3D, sampler_coefficientVoxel3D, uv, 0).rgb * (kSHBasisCoef[7] * n.x * n.z * kClampedCosineCoefs[7]);
    uv.z += stride;
    diffuse += SAMPLE_TEXTURE3D_LOD(_coefficientVoxel3D, sampler_coefficientVoxel3D, uv, 0).rgb * (kSHBasisCoef[8] * (n.x * n.x - n.y * n.y) * kClampedCosineCoefs[8]);
    diffuse = max(diffuse / first.a, 0);
    return true;
}

half3 PRTSampleGI(float3 positionWS, half3 normalWS, half3 fallback)
{
    if (_PRTEnabled < 0.5 || _PRTGridSpacing <= 0 || any(_PRTGridCount < 1)) return fallback;
    float3 baseNode = (positionWS - _PRTGridOrigin) / _PRTGridSpacing;
    float3 domainMargin = (1.0 - step(1.5, _PRTGridCount)) * 0.5;
    if (any(baseNode < -domainMargin) || any(baseNode > _PRTGridCount - 1.0 + domainMargin)) return fallback;
    float3 color = 0;
    float accumulated = 0;
    [loop] for (int level = 0; level < _PRTCascadeCount && accumulated < 1.0; level++)
    {
        float3 diffuse;
        float blend;
        if (!PRTSampleCascade(level, baseNode, normalWS, diffuse, blend)) continue;
        float contribution = (1.0 - accumulated) * blend;
        color += contribution * diffuse;
        accumulated += contribution;
    }
    if (accumulated <= 1e-4) return fallback;
    float3 irradiance = max(color / accumulated, 0);
    return irradiance + saturate(_PRTIndirectFill) * max((float3)fallback - irradiance, 0);
}

#endif
