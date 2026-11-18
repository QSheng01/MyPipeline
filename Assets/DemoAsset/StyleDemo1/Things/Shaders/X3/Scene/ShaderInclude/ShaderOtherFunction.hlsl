#ifndef SHADER_OTHER_FUNCTION
#define SHADER_OTHER_FUNCTION

half GetLuminance(half3 linearRgb)
{
    return dot(linearRgb, half3(0.2126729, 0.7151522, 0.0721750));
}

bool IsGammaSpace()
{
    #ifdef UNITY_COLORSPACE_GAMMA
        return true;
    #else
        return false;
    #endif
}

float4 _HeightFog;
half4 _HeightFogColorButton, _HeightFogColorMiddle, _HeightFogColorTop;
TEXTURE2D(_BlackFogMask); SAMPLER(sampler_BlackFogMask);

///////////////////////////////////////////////////////////////////////////////////////
// 新版战争迷雾
///////////////////////////////////////////////////////////////////////////////////////
TEXTURE2D(_WarFogMask);
SAMPLER(sampler_WarFogMask);
float4 _WarFogMask_ST;
half4 _WarFogMapWHOffset;
float4 _WarFogCharaPosAndSale;

// 颜色和深浅
half4 _WarFogColor;
half _WarFogDeep;

// 临时开关
half _FogMaskOpen;

// 迷雾中心的世界坐标
half4 _WarFogCenterWS;
// 玩家视野
half _PlayerSignDist;

// 重映射函数
// 当 x = t1 时，y = s1；
// 当 x = t2 时，y = s2；
half remap(half x, half t1, half t2, half s1, half s2)
{
    return (x - t1) / (t2 - t1) * (s2 - s1) + s1;
}

void WarFogSample(inout half3 color, float3 positionWS)
{
    // // sample the warfog
    // // 映射原点为玩家的原点 positionWS.x - _WarFogCharaPosAndSale.x
    // // 映射大小为世界坐标移动100格，步进图移动1.0
    float2 warFogBigMapUv = float2((positionWS.x - _WarFogCharaPosAndSale.x + _WarFogCharaPosAndSale.z / 2.0) / _WarFogCharaPosAndSale.z, (positionWS.z  - _WarFogCharaPosAndSale.y + _WarFogCharaPosAndSale.w / 2.0) / _WarFogCharaPosAndSale.w);
    
    half4 warFogMask = SAMPLE_TEXTURE2D(_WarFogMask, sampler_WarFogMask, warFogBigMapUv);
    
    // 采样的阴影
    half tFogMask = 1 - warFogMask.r;
    
    tFogMask *= _FogMaskOpen;
    
    // // 叠加颜色
    color.rgb = lerp(color.rgb, _WarFogColor.rgb, _WarFogDeep * tFogMask);
}
/////////////////////////////////////////////////////////////////////////////////////////

float4 GetWorldFogUV(float3 worldPos)
{
    float4 uv = 0;//uv的xy用于采样步进结果，zw用来采样那个圈，防止圈定位精度不够抖动。
    return uv;
}

float4 GetWorldFogUV(float3 worldPos, out half4 wfUvBefore)
{
    wfUvBefore = 0;
    float4 uv = 0;//uv的xy用于采样步进结果，zw用来采样那个圈，防止圈定位精度不够抖动。
    return uv;
}

void AlphaBlendWorldFog(inout half3 color, inout half Alpha, float4 uv)
{
    // Alpha *= worldFog; 
}

void AlphaBlendWorldFog(inout half3 color, inout half Alpha, float4 uv, half4 wfUvBefore)
{
    // 计算 Alpha
    // Alpha *= worldFog; 
}

void AlphaBlendOtherFunction(inout half3 color, inout half Alpha, float4 uv, half4 wfUvBefore)
{
    #ifdef _WORLDFOG
    AlphaBlendWorldFog(color, Alpha, uv, wfUvBefore);
    #endif
}

void AlphaBlendOtherFunction(inout half3 color, inout half Alpha, float4 uv)
{
    #ifdef _WORLDFOG
        AlphaBlendWorldFog(color, Alpha, uv);
    #endif
}

void AdditiveWorldFog(inout half3 Color, float4 uv, half4 wfUvBefore)
{
    // Color =lerp(_WorldFogMainColor.xyz * Color, Color, worldFog);
}

void AdditiveWorldFog(inout half3 Color, float4 uv)
{
    // Color =lerp(_WorldFogMainColor.xyz * Color, Color, worldFog);
}


void GetWorldFogColor(inout half3 color, float4 uv, half4 wfUvBefore)
{
    // color = lerp(_WorldFogMainColor.xyz, color, saturate(worldFog));
}

void GetWorldFogColor(inout half3 color, float4 uv)
{
    // color = lerp(_WorldFogMainColor.xyz, color, worldFog);
}

void AdditiveOtherFunction(inout half3 Color, float4 uv, half4 wfUvBefore)
{
    #ifdef _WORLDFOG
        AdditiveWorldFog(Color, uv, wfUvBefore);
    #endif
}

void AdditiveOtherFunction(inout half3 Color, float4 uv)
{
    #ifdef _WORLDFOG
        AdditiveWorldFog(Color, uv);
    #endif
}
///////////////////////////////////////////////////////////////////////////////////////////////////////////////


void HeightFog(inout half3 col, float3 worldPos)
{
    float fogTopHeight = _HeightFog.x;
    float fogTopSmooth = _HeightFog.y;
    float fogBottomHeight = _HeightFog.z;
    float fogBottomSmooth = _HeightFog.w;

    half topLerpRange = smoothstep( worldPos.y - fogTopSmooth, worldPos.y + fogTopSmooth, fogTopHeight);
    half bottomLerpRange = smoothstep( worldPos.y - fogBottomSmooth, worldPos.y + fogBottomSmooth, fogBottomHeight);
    //half4 lerpColor = lerp(_HeightFogColorTop, _HeightFogColorButton, lerpRange);
    half4 topLerpColor = _HeightFogColorTop * _HeightFogColorTop.a * (1 - topLerpRange) + _HeightFogColorMiddle * _HeightFogColorMiddle.a * topLerpRange;
    half4 bottomLerpColor = topLerpColor * topLerpColor.a * (1 - bottomLerpRange) + _HeightFogColorButton * _HeightFogColorButton.a * bottomLerpRange;
    
    col = col * (1 - bottomLerpColor.a) + bottomLerpColor.xyz * bottomLerpColor.a;
}

void Swing(inout float3 positionOS, float4 swing, float mask)
{
    float3 center = float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23);
    float3 s = cos(_Time.y * swing.w + center) * swing.xyz * mask;
    positionOS.xyz += s;
}

void LeafSwing(inout float3 positionWS, float4 swing, float mask, float power = 1)
{
    float3 s = cos(_Time.y * swing.w + positionWS.xyz) * swing.xyz * mask;
    positionWS.xyz += s * power;
}

float4 _WindDirSpeedStrength;
float4 _WindFFTSpace;

float4 GetWindData(float3 pos)
{
    float2 center = pos.xz;
    center /= _WindFFTSpace.z;
    center -= _Time.yy * _WindDirSpeedStrength.z;
    float4 fft = cos(center.xyxy * _WindFFTSpace.xyxy);
    float f = fft.x * fft.y * fft.z * fft.w;
    float2 dir = _WindDirSpeedStrength.xy;

    float4 data = float4(dir.x, dir.y, f, _WindDirSpeedStrength.w);

    return data;
}

TEXTURE2D(_OtherShadowmap); SAMPLER(sampler_OtherShadowmap);
float4 _OtherShadowmap_ST;
float3 _OtherShadowmap_OffSet;//xy为坐标偏移,z为风力坐标缩放

float GetOtherShadowmap(float3 worldPos)
{
    #ifdef _WIND

        float2 offset = _OtherShadowmap_ST.zz;

        #if _SHADESHADOWS
            float4 wind = GetWindData(worldPos * _OtherShadowmap_OffSet.z);
            float t = wind.z * 0.5f + 0.5f;
            offset = wind.xy * t * wind.w * _OtherShadowmap_ST.z;
        #elif _CLOUDSHADOWS
            float t = _Time.y * _WindDirSpeedStrength.z;
            offset = _WindDirSpeedStrength.xy * t * _WindDirSpeedStrength.w * _OtherShadowmap_ST.z;
        #else
            offset = _OtherShadowmap_ST.y;
        #endif
        
        worldPos.xz -= offset;

    #endif

    worldPos.xz -= _OtherShadowmap_OffSet.xy;
    worldPos.xz /= (_OtherShadowmap_ST.xy);

    float2 uv = worldPos.xz+float2(0.5,0.5);

    half shadow = SAMPLE_TEXTURE2D(_OtherShadowmap, sampler_OtherShadowmap, uv).x;
    //shadow = saturate((_OtherShadowmap_ST.w * shadow ) + shadow);
    shadow += _OtherShadowmap_ST.w;
    return saturate(shadow);
}

float GetNoiseShadowmap(float3 worldPos)
{
    worldPos.xz *= _OtherShadowmap_ST.xy;
    worldPos.xz += _OtherShadowmap_OffSet.xy;
    float2 uv = worldPos.xz;
    float2 uv2 = float2(worldPos.x, -worldPos.z);

    float2 center = uv;//pos.xz;
    center /= _WindFFTSpace.z;
    center -= _Time.yy * _WindDirSpeedStrength.z;
    float4 fft = cos(center.xyxy * _WindFFTSpace.xyxy);
    float f = fft.x * fft.y * fft.z * fft.w;
    f = f * 2 - 1;

    half2 offset = _WindDirSpeedStrength.xy * _WindDirSpeedStrength.w * _OtherShadowmap_ST.z * f;
    half2 offset1 = half2(_WindDirSpeedStrength.x, -_WindDirSpeedStrength.y) * _WindDirSpeedStrength.w * _OtherShadowmap_ST.z * f;

    uv -= offset;
    uv2 += offset1;

    half shadow = SAMPLE_TEXTURE2D(_OtherShadowmap, sampler_OtherShadowmap, uv).x;
    half shadow1 = SAMPLE_TEXTURE2D(_OtherShadowmap, sampler_OtherShadowmap, uv2).x;
    half mask = (_OtherShadowmap_ST.w * shadow ) + shadow;
    half mask1 = (_OtherShadowmap_ST.w * shadow1) + shadow1;
    return saturate(mask*mask1);
}

float GetOtherShadowmap(float3 worldPos, float4 wind)
{
    worldPos.xz *= _OtherShadowmap_ST.xx;

    float2 uv = worldPos.xz;

    #ifdef _WIND

        float2 offset;

        #if _SHADESHADOWS
            float t = wind.z * 0.5f + 0.5f;
            offset = wind.xy * t * wind.w * _OtherShadowmap_ST.y;
        #elif _CLOUDSHADOWS
            float t = _Time.y * _WindDirSpeedStrength.z;
            offset = _WindDirSpeedStrength.xy * t * _WindDirSpeedStrength.w * _OtherShadowmap_ST.y;
        #else
            offset = _OtherShadowmap_ST.y;
        #endif

        uv -= offset;

    #endif

    half shadow = SAMPLE_TEXTURE2D(_OtherShadowmap, sampler_OtherShadowmap, uv).x;
    return saturate(shadow + _OtherShadowmap_ST.z);
}

float Dither32(float2 Pos)
{
    float Ret = dot(float3(Pos.xy, 0.5f), float3(0.40625f, 0.15625f, 0.46875f));
    return frac(Ret);
}

// float2 _DitherValue;

void DitherObject(float t, float3 positionCS)
{
    // float2 csp = screenPos.xy / screenPos.w;
    // csp -= 0.5h;
    // float mask = length(csp);
    // mask = smoothstep(_DitherValue.x, _DitherValue.y, mask);

    float mask = t;

    clip(mask - Dither32(positionCS.xy));
}

half DitherObjectAlpha(float t, float3 positionCS)
{
    half mask = t;
    half tAlpha = step(Dither32(positionCS.xy), mask);
    return tAlpha;
}

// 刷草工具的吸取颜色有关
#ifdef _GRASSDRAW
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
int grassDrawMode;
float4 GetGrassDrawColor(float3 vertexSH, float3 albedo, float3 normalWS, float4 shadowMask)
{
    float3 lightUVOrSH = (1, 1, 1);
    #if defined(LIGHTMAP_ON)
    lightUVOrSH.xy = i.lightmapUV;
    #else
    lightUVOrSH = vertexSH;
    #endif
    if(grassDrawMode == 4) // 草的吸色Color
        {
        return float4(albedo, 1.0f);
        }
    if (grassDrawMode == 7)
    {
        return float4(normalWS * 0.5f + 0.5f, 1.0f);
    }
    if(grassDrawMode == 14) // 草的吸色 ShadowMask
        {
        return shadowMask;
        }
    if(grassDrawMode == 15) // 草的吸色 lightMap
        {
        #ifdef LIGHTMAP_ON
        return SAMPLE_TEXTURE2D(unity_Lightmap, samplerunity_Lightmap, lightUVOrSH.xy).rgba;
        #else
        return half4(SampleSHPixel(lightUVOrSH, normalWS),1);
        #endif
        }
    return float4(albedo, 1.0f);
}

//望月草 shader 的吸色
float4 GetGrassDrawColor2(float3 vertexSH, float3 albedo, float3 normalWS, float4 shadowMask)
{
    float3 lightUVOrSH = (1, 1, 1);
    #if defined(LIGHTMAP_ON)
    lightUVOrSH.xy = i.lightmapUV;
    #else
    lightUVOrSH = vertexSH;
    #endif
    if(grassDrawMode == 4) // 草的吸色 Color
        {
        return float4(albedo, 1.0f);
        }
    if (grassDrawMode == 7)
    {
        return float4(normalWS * 0.5f + 0.5f, 1.0f);
    }
    if(grassDrawMode == 14) // 草的吸色 ShadowMask
        {
            return float4(normalWS * 0.5f + 0.5f, 1.0f);;
        }
    if(grassDrawMode == 15) // 草的吸色 lightMap
        {
            float4(normalWS * 0.5f + 0.5f, 1.0f);
        }
    return float4(albedo, 1.0f);
}
#endif

#endif