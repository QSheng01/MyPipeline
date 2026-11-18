#ifndef AVT_MATERIAL_BAKE_INCLUDED
#define AVT_MATERIAL_BAKE_INCLUDED
#include "TerrainMaterialIDCompute.hlsl"

Texture2D<unorm float> _SplatMap;
TEXTURE2D_ARRAY(_BaseMapArray);
TEXTURE2D_ARRAY(_MaskMapArray);
SamplerState sampler_linear_repeat;

// Preserve Feedback's authored mip bias: its maximum virtual density is 1024 texels/m.
// Both caches must evaluate the same material at the same world footprint.
float AVTSourceMip(float metersPerTexel)
{
    return max(log2(max(metersPerTexel * 1024.0, 1.0)), 0.0) * 0.1;
}
float AVTMaterialSlot(float2 position)
{
    uint width, height;
    _SplatMap.GetDimensions(width, height);
    return _SplatMap.Load(int3(clamp(int2(position), int2(0, 0), int2(width, height) - 1), 0)).r * 255.5;
}
void BakeTerrainMaterial(float2 positionWS, float metersPerTexel, out float4 baseMap, out float4 maskMap)
{
    float2 a, b, c, d;
    float4 weight = RectGrid(positionWS, a, b, c, d);
    float4 slots = float4(AVTMaterialSlot(a), AVTMaterialSlot(b), AVTMaterialSlot(c), AVTMaterialSlot(d));
    float2 uv = positionWS * 0.1;
    float mip = AVTSourceMip(metersPerTexel);
    float4 b0 = SAMPLE_TEXTURE2D_ARRAY_LOD(_BaseMapArray, sampler_linear_repeat, uv, slots.x, mip);
    float4 b1 = SAMPLE_TEXTURE2D_ARRAY_LOD(_BaseMapArray, sampler_linear_repeat, uv, slots.y, mip);
    float4 b2 = SAMPLE_TEXTURE2D_ARRAY_LOD(_BaseMapArray, sampler_linear_repeat, uv, slots.z, mip);
    float4 b3 = SAMPLE_TEXTURE2D_ARRAY_LOD(_BaseMapArray, sampler_linear_repeat, uv, slots.w, mip);
    float4 m0 = SAMPLE_TEXTURE2D_ARRAY_LOD(_MaskMapArray, sampler_linear_repeat, uv, slots.x, mip);
    float4 m1 = SAMPLE_TEXTURE2D_ARRAY_LOD(_MaskMapArray, sampler_linear_repeat, uv, slots.y, mip);
    float4 m2 = SAMPLE_TEXTURE2D_ARRAY_LOD(_MaskMapArray, sampler_linear_repeat, uv, slots.z, mip);
    float4 m3 = SAMPLE_TEXTURE2D_ARRAY_LOD(_MaskMapArray, sampler_linear_repeat, uv, slots.w, mip);
    weight = CalculateBlendWeight(weight, abs(float4(b0.a, b1.a, b2.a, b3.a)) + HALF_EPS, 1);
    baseMap = Blend(b0, b1, b2, b3, weight);
    maskMap = Blend(m0, m1, m2, m3, weight);
}
#endif
