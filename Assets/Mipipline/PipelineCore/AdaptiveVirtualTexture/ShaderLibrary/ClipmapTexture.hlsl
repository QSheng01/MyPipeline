#ifndef CLIPMAP_TEXTURE_INCLUDED
#define CLIPMAP_TEXTURE_INCLUDED
#define CLIPMAP_MAX_LEVELS 9
Texture2DArray<float4> ClipmapBase;
Texture2DArray<float4> ClipmapMask;
Texture2DArray<float4> ClipmapIndirection;
SamplerState sampler_ClipmapBase;
float4 ClipmapPageWindows10[10]; // world minimum XZ, window side, meters/texel; last is permanent tail
float4 ClipmapPageLayout; // page interior, pages/axis, border, physical atlas width
float4 ClipmapSampling; // logical resolution, transition fraction, max anisotropy, unused
int ClipmapLevelCount;
int ClipmapDebug;

float3 ClipmapLevelColor(float level)
{
    float3 p = abs(frac(level / (float)CLIPMAP_MAX_LEVELS + float3(1, 2.0/3.0, 1.0/3.0)) * 6 - 3);
    return saturate(p - 1);
}
float ClipmapOutside(float2 positionXZ, int level)
{
    float4 window = ClipmapPageWindows10[level];
    float2 edge = abs(positionXZ - (window.xy + window.z * 0.5)) / max(window.z * 0.5, 0.0001);
    return max(edge.x, edge.y);
}
float ClipmapContainment(float2 positionXZ, int level, float footprint)
{
    float4 window = ClipmapPageWindows10[level];
    float2 edge = abs(positionXZ - (window.xy + window.z * 0.5));
    float margin = window.z * 0.5 - max(edge.x, edge.y) - window.w * 2 - footprint * 0.5;
    return saturate(margin / max(window.z * 0.5 * ClipmapSampling.y, 0.0001));
}
float ClipmapLookup(float2 positionXZ, int level, out float3 physicalUV)
{
    float4 window = ClipmapPageWindows10[level];
    float pageWidth = ClipmapPageLayout.x;
    float2 texel = (positionXZ - window.xy) / max(window.w, 0.000001);
    if (level == ClipmapLevelCount)
        texel = clamp(texel, 0.0, ClipmapSampling.x - 0.001);
    float valid = all(texel >= 0) && all(texel < ClipmapSampling.x) ? 1.0 : 0.0;
    int2 localPage = clamp((int2)floor(texel / pageWidth), int2(0, 0), int2(ClipmapPageLayout.yy) - 1);
    // Point load is essential: table channels encode byte indices, not interpolated colors.
    uint4 entry = (uint4)round(ClipmapIndirection.Load(int4(localPage, level, 0)) * 255.0);
    valid *= entry.b > 0 ? 1.0 : 0.0;
    float2 inPage = texel - float2(localPage) * pageWidth;
    float pitch = pageWidth + 2 * ClipmapPageLayout.z;
    physicalUV = float3((float2(entry.rg) * pitch + ClipmapPageLayout.z + inPage + 0.5) / ClipmapPageLayout.w, entry.a);
    return valid;
}
float SampleClipmapLevel(float2 positionXZ, int level, out float4 baseMap, out float4 maskMap)
{
    float3 uv;
    float valid = ClipmapLookup(positionXZ, level, uv);
    baseMap = SAMPLE_TEXTURE2D_ARRAY_LOD(ClipmapBase, sampler_ClipmapBase, uv.xy, (uint)uv.z, 0);
    maskMap = SAMPLE_TEXTURE2D_ARRAY_LOD(ClipmapMask, sampler_ClipmapBase, uv.xy, (uint)uv.z, 0);
    return valid;
}
float SampleClipmapFiltered(float2 positionXZ, int level, float2 majorAxis, float minorLength,
    out float4 baseMap, out float4 maskMap)
{
    int taps = clamp((int)ceil(length(majorAxis) / max(minorLength, ClipmapPageWindows10[level].w)), 1, (int)ClipmapSampling.z);
    baseMap = 0; maskMap = 0;
    float valid = 1;
    [loop] for (int tap = 0; tap < taps; tap++)
    {
        float offset = (tap + 0.5) / taps - 0.5;
        float4 b, m;
        // Every anisotropic tap resolves its own page. Missing support falls back as a whole.
        valid *= SampleClipmapLevel(positionXZ + majorAxis * offset, level, b, m);
        baseMap += b; maskMap += m;
    }
    baseMap /= taps; maskMap /= taps;
    return valid;
}
void SampleClipmap(float3 positionWS, out float4 baseMap, out float4 maskMap)
{
    int count = clamp(ClipmapLevelCount, 0, CLIPMAP_MAX_LEVELS);
    float2 dx = ddx(positionWS.xz), dy = ddy(positionWS.xz);
    float2 majorAxis = dot(dx, dx) > dot(dy, dy) ? dx : dy;
    float majorLength = max(length(majorAxis), 0.000001);
    float minorLength = max(abs(dx.x * dy.y - dx.y * dy.x) / majorLength, majorLength / max(ClipmapSampling.z, 1.0));
    float lod = clamp(log2(max(minorLength / max(ClipmapPageWindows10[0].w, 0.000001), 1.0)), 0.0, (float)max(count - 1, 0));
    baseMap = 0; maskMap = 0;
    float remaining = 1;
    [loop] for (int level = min((int)floor(lod), count); level < count; level++)
    {
        float weight = ClipmapContainment(positionWS.xz, level, majorLength) * saturate(level + 1.0 - lod);
        if (weight <= 0) continue;
        float4 b, m;
        weight *= SampleClipmapFiltered(positionWS.xz, level, majorAxis, minorLength, b, m);
        baseMap += b * (remaining * weight); maskMap += m * (remaining * weight);
        remaining *= 1 - weight;
        if (remaining < 0.0001) return;
    }
    float4 b, m;
    SampleClipmapFiltered(positionWS.xz, count, majorAxis, minorLength, b, m);
    baseMap += b * remaining; maskMap += m * remaining;
}
float3 ClipmapDebugColor(float2 positionXZ)
{
    int count = clamp(ClipmapLevelCount, 0, CLIPMAP_MAX_LEVELS);
    int fine = count;
    float outside = 0;
    [loop] for (int level = 0; level < count; level++)
    {
        float3 physicalUV;
        if (ClipmapLookup(positionXZ, level, physicalUV) > 0.5) { fine = level; outside = ClipmapOutside(positionXZ, level); break; }
    }
    if (fine == count) return float3(0.22, 0.22, 0.22);
    float width = max(fwidth(outside) * 3, 0.002);
    float3 color = ClipmapLevelColor(fine);
    color = lerp(color, 0.02, smoothstep(1 - width, 1 - width * 0.5, outside));
    return lerp(color, 1.0, smoothstep(1 - width * 0.4, 1, outside));
}
#endif
