#ifndef EFFECTS_ADDITION_INCLUDE
#define EFFECTS_ADDITION_INCLUDE

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_DissloveTex);
SAMPLER(sampler_DissloveTex);

TEXTURE2D(_ResidentDisTex);
SAMPLER(sampler_ResidentDisTex);
// half4 _DissloveTex_ST;
// half _DissloveThreshold;
// half _DissloveEdgeLength;
// half4 _DissloveEdgeColor;
void LitDisslove(float2 uv,half dissloveEdgeLength,half dissloveThreshold,half4 dissloveEdgeColor,inout half3 color)
{
    half cutout =uv.y;// SAMPLE_TEXTURE2D(_DissloveTex,sampler_DissloveTex,uv).r;
    cutout += 0.001h;

    //溶解剔除
    half dissolutionValue = dissloveThreshold * 2.0h;
    half dissolutionValue1 = dissolutionValue - dissloveEdgeLength;//随机溶解
    clip(cutout - dissolutionValue1);

    //随机溶解
    half dissolutionColorWeight = ceil(saturate(cutout - dissolutionValue));

    color = lerp(dissloveEdgeColor.rgb, color, dissolutionColorWeight * dissloveEdgeColor.a);

    //望月计算
    // clip(cutout+dissloveEdgeLength - dissloveThreshold*lerp(1.001,1.001+dissloveEdgeLength,dissloveThreshold));
    //
    // float degree = 1-saturate(max(0.001,(cutout+dissloveEdgeLength - dissloveThreshold*lerp(1.001,1.001+dissloveEdgeLength,dissloveThreshold))) / dissloveEdgeLength);
    // color = lerp(color, dissloveEdgeColor.rgb,degree*dissloveEdgeColor.a);
}

void Disslove(float2 uv,half dissloveEdgeLength,half dissloveThreshold,half4 dissloveEdgeColor,inout half3 color)
{
    half cutout = SAMPLE_TEXTURE2D(_DissloveTex,sampler_DissloveTex,uv).r;
    cutout += 0.001h;

    //溶解剔除
    half dissolutionValue = dissloveThreshold * 2.0h;
    half dissolutionValue1 = dissolutionValue - dissloveEdgeLength;//随机溶解
    clip(cutout - dissolutionValue1);

    //随机溶解
    half dissolutionColorWeight = ceil(saturate(cutout - dissolutionValue));

    color = lerp(dissloveEdgeColor.rgb, color, dissolutionColorWeight * dissloveEdgeColor.a);

    //望月计算
    // clip(cutout+dissloveEdgeLength - dissloveThreshold*lerp(1.001,1.001+dissloveEdgeLength,dissloveThreshold));

    // float degree = 1-saturate(max(0.001,(cutout+dissloveEdgeLength - dissloveThreshold*lerp(1.001,1.001+dissloveEdgeLength,dissloveThreshold))) / dissloveEdgeLength);
    // color = lerp(color, dissloveEdgeColor.rgb,degree*dissloveEdgeColor.a);
}

void DissloveAlpha(float2 uv,half dissloveEdgeLength,half dissloveThreshold)
{
    half cutout = SAMPLE_TEXTURE2D(_DissloveTex,sampler_DissloveTex,uv).r;
    cutout += 0.001h;

    //溶解剔除
    half dissolutionValue = dissloveThreshold * 2.0h;
    half dissolutionValue1 = dissolutionValue - dissloveEdgeLength;//随机溶解
    clip(cutout - dissolutionValue1);

    //望月计算
    // clip(cutout+dissloveEdgeLength - dissloveThreshold*lerp(1.001,1.001+dissloveEdgeLength,dissloveThreshold));
}

half3 Fresnel(half3 bump,half3 viewDir, half fresnelF0,half4 fresnelColor)
{
    half3 fresnel= saturate(pow(1 - abs(dot(bump,viewDir)), fresnelF0.x))*fresnelColor.rgb*fresnelColor.a;
    return fresnel;
}

void DirDisslove(float4 uv,half dissloveEdgeLength,half dissloveThreshold,half4 dissloveEdgeColor,inout  half3 color,float _ResidentBlankingDis,float _ResidentBlankingRange)
{
    half mask = 0.5 + (uv.y - _ResidentBlankingDis) / _ResidentBlankingRange;
    half cutout = SAMPLE_TEXTURE2D(_ResidentDisTex,sampler_ResidentDisTex,uv.zw).r+mask;
    half cutValue = cutout+dissloveEdgeLength - dissloveThreshold*lerp(1.001,1.001+dissloveEdgeLength,dissloveThreshold);
    clip(cutValue);
    float degree = 1-saturate(max(0.001,cutValue) / dissloveEdgeLength);
    color = lerp(color, dissloveEdgeColor.rgb,degree*dissloveEdgeColor.a);
}



void DirDissloveAlpha(float4 uv,half dissloveEdgeLength,half dissloveThreshold,float _ResidentBlankingDis,float _ResidentBlankingRange)
{
    half mask = 0.5 + (uv.y - _ResidentBlankingDis) / _ResidentBlankingRange;
    half cutout = SAMPLE_TEXTURE2D(_ResidentDisTex,sampler_ResidentDisTex,uv.zw).r+mask;
    half cutValue = cutout+dissloveEdgeLength - dissloveThreshold*lerp(1.001,1.001+dissloveEdgeLength,dissloveThreshold);
    clip(cutValue);
}

CBUFFER_START(WeaknessBuffer)
half _WeaknessPower;
half4 _WeaknessRimColor;
half4 _WeaknessBaseColor;
CBUFFER_END
void Weakness(half3 bump,half3 viewDir,half mask,inout half3 color)
{
    half fresnel=saturate(pow(1 - abs(dot(bump,viewDir)), _WeaknessPower));
    color=lerp(color,color*_WeaknessBaseColor.rgb,mask*_WeaknessBaseColor.a);
    color=lerp(color,_WeaknessRimColor.rgb,fresnel*mask*_WeaknessRimColor.a);
}

CBUFFER_START(TeleportBuffer)
half3 _SliceNormal;
float3 _SliceCenter;
half4 _ClipEdgeColor;
half _ClipEdgeWidth;
half _ClipOpen;
CBUFFER_END
void TeleportClip(float3 worldPos,inout half3 color)
{
    float sliceSide = dot(_SliceNormal,worldPos- _SliceCenter) * _ClipOpen;
    half edgeLerp = 1 - saturate(sliceSide *_ClipEdgeWidth);
    color = lerp(color, _ClipEdgeColor, edgeLerp * _ClipOpen);
    clip(sliceSide);
}
void TeleportClipAlpha(float3 worldPos)
{
    float sliceSide = dot(_SliceNormal,worldPos- _SliceCenter) * _ClipOpen;
    clip(sliceSide);
}

#endif