#ifndef UNIVERSAL_SHIYUE_COMMON_INCLUDED
#define UNIVERSAL_SHIYUE_COMMON_INCLUDED

half debugGI2;

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"

// SAMPLER(sampler_LinearClamp);
// SAMPLER(sampler_LinearRepeat);
// SAMPLER(sampler_PointClamp);
// SAMPLER(sampler_PointRepeat);

// TOD混合反射球
TEXTURECUBE(_TOD_ReflectionProbe_0);        SAMPLER(sampler_TOD_ReflectionProbe_0);
TEXTURECUBE(_TOD_ReflectionProbe_1);        SAMPLER(sampler_TOD_ReflectionProbe_1);

/// \brief 计算暗部颜色暗度
half ComputeLambertColor(half3 sphereNormalWS, half3 lightDir, half offset, half intensity)
{
    half halfLambert = dot(lightDir, sphereNormalWS);
    halfLambert += offset;
    halfLambert = saturate(halfLambert);
    half lambert = lerp(intensity, 1, halfLambert);
    return 1; // 暂时弃用这个功能，因为这样的Trick不是全时段通用的，换一个时段都会不合适。考虑后面和TOD一起控制？
}

 // we have to support both orthographic and perspective camera projection
 // static uniform branch depends on unity_OrthoParams.w
 //  Works in all cases.
float LinearEyeDepth(float deviceDepth)
{
    float depth = deviceDepth;
    #if UNITY_REVERSED_Z
        deviceDepth = 1.0 - deviceDepth;
    #endif
  	float ortho = (_ProjectionParams.z - _ProjectionParams.y) * deviceDepth  + _ProjectionParams.y;
    float depthZ =  lerp(LinearEyeDepth(depth,_ZBufferParams), ortho, unity_OrthoParams.w);
    
    return depthZ;
}

float Linear01Depth(float deviceDepth)
{
    float depth = deviceDepth;
    #if UNITY_REVERSED_Z
        deviceDepth = 1.0 - deviceDepth;
    #endif
  	float ortho = deviceDepth;
    float depthZ =  lerp(Linear01Depth(depth,_ZBufferParams), ortho, unity_OrthoParams.w);
    
    return depthZ;
}


inline half3 Unity_Rotate_About_Axis_Degrees(half3 originalDir,half3 Axis, half rotation)
{
    rotation = radians(rotation);
    float s = sin(rotation);
    float c = cos(rotation);
    float one_minus_c = 1.0 - c;
    Axis = normalize(Axis);

    half3x3 rot_mat = { one_minus_c * Axis.x * Axis.x + c,            one_minus_c * Axis.x * Axis.y - Axis.z * s,     one_minus_c * Axis.z * Axis.x + Axis.y * s,
                        one_minus_c * Axis.x * Axis.y + Axis.z * s,   one_minus_c * Axis.y * Axis.y + c,              one_minus_c * Axis.y * Axis.z - Axis.x * s,
                        one_minus_c * Axis.z * Axis.x - Axis.y * s,   one_minus_c * Axis.y * Axis.z + Axis.x * s,     one_minus_c * Axis.z * Axis.z + c
                      };
    return mul(rot_mat,  originalDir);
}

// half LinearStep(half minValue, half maxValue, half In)
// {
//     return saturate((In-minValue) / (maxValue - minValue));
// }

inline float Dither4x4Bayer( int x, int y )
{
    const float dither[ 16 ] = {
                1,  9,  3, 11,
                13,  5, 15,  7,
                4, 12,  2, 10,
                16,  8, 14,  6 };
    int r = y * 4 + x;
    return dither[r] / 16; // same # of instructions as pre-dividing due to compiler magic
}

inline float Dither8x8Bayer( int x, int y )
{
    const float dither[ 64 ] = {
        0, 32,  8, 40,  2, 34, 10, 42,
        48, 16, 56, 24, 50, 18, 58, 26,
        12, 44,  4, 36, 14, 46,  6, 38,
        60, 28, 52, 20, 62, 30, 54, 22,
         3, 35, 11, 43,  1, 33,  9, 41,
        51, 19, 59, 27, 49, 17, 57, 25,
        15, 47,  7, 39, 13, 45,  5, 37,
        63, 31, 55, 23, 61, 29, 53, 21};
    int r = y * 8 + x;
    return dither[r] / 64; // same # of instructions as pre-dividing due to compiler magic
}

float Dither32(float2 Pos, float frameIndexMod4) {
    uint3 k0 = uint3(13, 5, 15);
    //float Ret = dot( float3(Pos.xy, frameIndexMod4 + 0.5f), k0 / 32.0f);
    float Ret = dot( float3(Pos.xy, 0.5f), k0 / 32.0f);
    return frac(Ret);
}

void DitherThreshold2(half4 scrPos,half ditherTimer)
{
    float2x2 thresholdMatrix =
    {  7.0 / 17.0,  9.0 / 17.0,
      13.0 / 17.0,  5.0 / 17.0,
    };

    float2x2 _RowAccess = { 1,0, 0,1};

    float2 screenUV = scrPos.xy / scrPos.w;
    screenUV *= _ScreenParams.xy;
    screenUV*=half2(0.2,0.2);
    //screenUV.xy = frac(abs(screenUV.xy));
    //clip(screenUV<0);
    clip(1 - ditherTimer - thresholdMatrix[fmod(screenUV.x, 2)] * _RowAccess[fmod(screenUV.y, 2)]);
}

void DitherThreshold3(half4 scrPos,half ditherTimer)
{
    float3x3 thresholdMatrix =
    {  1.0 / 17.0,  9.0 / 17.0, 11.0 / 17.0,
      13.0 / 17.0,  5.0 / 17.0, 7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0
    };

    float3x3 _RowAccess = { 1,0,0 ,0,1,0, 0,0,1};

    float2 screenUV = scrPos.xy / scrPos.w;
    screenUV *= _ScreenParams.xy;
    screenUV*=half2(0.2,0.2);
    //screenUV.xy = frac(abs(screenUV.xy));
    //clip(screenUV<0);
    clip(1 - ditherTimer - thresholdMatrix[fmod(screenUV.x, 3)] * _RowAccess[fmod(screenUV.y, 3)]);
}
void DitherThreshold(half4 scrPos,half ditherTimer)
{
    float4x4 thresholdMatrix =
    {  1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
      13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
       4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
      16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };

    float4x4 _RowAccess = { 1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1 };
    float2 screenUV = (scrPos.xy) / scrPos.ww;
    screenUV *= _ScaledScreenParams.xy;
    clip(1 - ditherTimer - thresholdMatrix[fmod(screenUV.x, 4)] * _RowAccess[fmod(screenUV.y, 4)]);
}


void DitherThreshold(half4 scrPos,float2 positionCS,half ditherTimer)
{
     float4x4 thresholdMatrix =
     {  1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
       13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
       16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
     };
    
     float4x4 _RowAccess = { 1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1 };
     float2 screenUV = (scrPos.xy) / scrPos.ww;
     screenUV *= _ScaledScreenParams.xy;
     clip(1 - ditherTimer - thresholdMatrix[fmod(screenUV.x, 4)] * _RowAccess[fmod(screenUV.y, 4)]);
}

void DitheringTransition(uint2 fadeMaskSeed, float ditherFactor)
{
    // Generate a spatially varying pattern.
    // Unfortunately, varying the pattern with time confuses the TAA, increasing the amount of noise.
    float p = GenerateHashedRandomFloat(fadeMaskSeed);
    float sgn = ditherFactor > 0 ? 1.0f : -1.0f;
    // This preserves the symmetry s.t. if LOD 0 has f = x, LOD 1 has f = -x.
    float f = ditherFactor  - abs(CopySign(p, ditherFactor)) * sgn; 
    clip(f);
}

inline float DecodeFloatRG( float2 enc ) {
    float2 kDecodeDot = float2(1.0, 1/255.0);
    return dot( enc, kDecodeDot );
}

half FastSSS(half3 viewDir, half3 lightDir, half3 normalWS, half sssOffset, half sssPow, half shadow)
{
    half vDotNL = dot(viewDir, -(lightDir + normalWS));
    vDotNL = saturate(vDotNL + sssOffset) * shadow;
    vDotNL = pow(vDotNL, sssPow);
    return vDotNL;
}

// half3 GetTODReflectionProbe(half3 texCoord, int lod)
// {
//     half3 probe0 = SAMPLE_TEXTURECUBE_LOD(_TOD_ReflectionProbe_0, sampler_TOD_ReflectionProbe_0, texCoord,lod);
//     half3 probe1 = SAMPLE_TEXTURECUBE_LOD(_TOD_ReflectionProbe_1, sampler_TOD_ReflectionProbe_1, texCoord,lod);
//
//     return lerp(probe0, probe1, _TOD_RPWeight);
// }

#endif
