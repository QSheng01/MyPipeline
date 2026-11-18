#ifndef STYLIZED_SKY_LIB
#define STYLIZED_SKY_LIB

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#define MIE_G (-0.990)
#define MIE_G2 0.9801
#define SKY_GROUND_THRESHOLD 0.005

half _HorizonTilt;
half _HorizonFalloff;
half _HorizonSunWidth;
half _HorizonSunSharp;
half _HorizonSunHeight;
half _HorizonSunStrength;
half4 _HorizonColor2;

half remap(half x, half t1, half t2, half s1, half s2)
{
    return (x - t1) / (t2 - t1) * (s2 - s1) + s1;
}

float3 RotateAroundYInDegrees (float3 vertex, float degrees)
{
    float alpha = degrees * PI / 180.0;
    float sina, cosa;
    sincos(alpha, sina, cosa);
    float2x2 m = float2x2(cosa, -sina, sina, cosa);
    return float3(mul(m, vertex.xz), vertex.y).xzy;
}

float2 ToRadialCoords(float3 coords)
{
    float3 normalizedCoords = normalize(coords);
    float latitude = acos(normalizedCoords.y);
    float longitude = atan2(normalizedCoords.z, normalizedCoords.x);
    float2 sphereCoords = float2(longitude, latitude) * float2(0.5/PI, 1.0/PI);
    return float2(0.5,1.0) - sphereCoords;
}

float2 CloudUV(float3 pos) 
{
    float2 tc = ToRadialCoords(pos);
    if (tc.x > 1.0)
        return half2(0,0);
    tc.x = fmod(tc.x, 1);
    tc.y*=2;
    return tc;
}

//计算光晕
half calcLightAttenuation(half3 lightPos, half3 ray, half halo)
{
    half3 delta = lightPos - ray;
    half dist = length(delta);
    half spot = 1.0 - smoothstep(0.0, halo, dist);
    return spot * spot;
}

void SunReckon(inout half3 color, float3 worldPos, float3 sunPos, float dotViewUp, half4 sunColor, half litRadius,
               half4 litColor, half sunHaloExponent, half sunMask, half groundMask = 1)
{
    half dotViewSun = saturate(dot(worldPos, sunPos.xyz));
    half bellCurve = pow(dotViewSun, sunHaloExponent);
    half horizonSoften = 1 - pow(1 - saturate(dotViewUp), 5);
    half3 sunHalo = saturate(bellCurve * horizonSoften) * litColor.rgb;
    half sun = pow(dotViewSun, 10000 * litRadius);
    sun = saturate(sun * 25);
    sun *= sunMask;
    sunColor.a *= groundMask;
    color = lerp(color, sunColor.rgb, sun*sunColor.a) + sunHalo;
}


void MoonReckon(inout half3 color, half4 moonColor, half3 haloColor,half3 moonPos, half3 ray, half halo, half groundMask)
{
    half3 moonHalo = calcLightAttenuation(moonPos.xyz, ray, halo) * haloColor.rgb;
    moonColor.a *= groundMask;
    color = lerp(color, moonColor.rgb, moonColor.a) + moonHalo;
}

void GradientSkyReckon(inout half3 color, float dotViewUp, half3 skybottomColor, half3 skytopColor,
                       float SkyGradientExponent)
{
    half gradient = 1.0 - saturate(dotViewUp);
    gradient = pow(gradient, SkyGradientExponent);
    gradient = 1 - saturate(gradient);
    color = lerp(skybottomColor.rgb, skytopColor.rgb, gradient);
}

void StarsReckon(inout half3 color,half3 starColor)
{
    color += starColor;
}

void GroundReckon(inout half3 combined, half dotViewUp,half3 skyBottomColor) //float3 ray)
{
    half y = 1 - saturate(dotViewUp);
    y = pow(y, 50);
    // float y = -ray.y / SKY_GROUND_THRESHOLD;
    combined = lerp(combined, skyBottomColor.rgb, saturate(y));
}

void EdgeHorizonReckon(inout half3 combined, half dotViewUp,half3 skyEdgeTopColor,half3 skyEdgeBottomColor, half horizonLineExponent,half horizonPower, half contribution)
{
    //这样算会花屏
    //half edgeskyhorizon = saturate(1.0 - abs(dotViewUp));
    half edgeskyhorizon = saturate(abs(dotViewUp));
    edgeskyhorizon = smoothstep(horizonLineExponent,0,edgeskyhorizon);
    edgeskyhorizon = pow(edgeskyhorizon,horizonPower);
    half3 skyEdgeCol = lerp(skyEdgeTopColor.rgb, skyEdgeBottomColor.rgb, edgeskyhorizon);
    combined = lerp(combined, skyEdgeCol.rgb, edgeskyhorizon * contribution);
}

void CloudReckon(inout half3 combined, half3 CloudCol, half3 cloudmask)
{
    combined = lerp(combined,CloudCol * 1.5,cloudmask.g);
    combined = lerp(combined,CloudCol * 0.5,cloudmask.b);
    combined = lerp(combined,CloudCol ,cloudmask.r);
    //combined = (combined + CloudCol , CloudCol, cloudmask);
}
#endif
