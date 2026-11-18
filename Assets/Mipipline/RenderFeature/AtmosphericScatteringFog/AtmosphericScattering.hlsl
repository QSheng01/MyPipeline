#ifndef UNITY_ATMOSPHERIC_SCATTERING_INCLUDED
#define UNITY_ATMOSPHERIC_SCATTERING_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "AtmosphericScattering_Input.hlsl"
// #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"

// TOD混合反射球
TEXTURECUBE(_TOD_ReflectionProbe_0);        SAMPLER(sampler_TOD_ReflectionProbe_0);
TEXTURECUBE(_TOD_ReflectionProbe_1);        SAMPLER(sampler_TOD_ReflectionProbe_1);
float _TOD_RPWeight;

// 太阳角度和月亮角度
float3 _SunDirectionWS;
float3 _MoonDirectionWS;

#define ENVCONSTANTS_CONVOLUTION_MIP_COUNT (7)

float GetCurrentExposureMultiplier()
{
    return _ProbeExposureScale;
}

real TransmittanceFromOpticalDepth(real opticalDepth)
{
    return exp(-opticalDepth);
}

float4 SampleSkyTexture(float3 texCoord, float lod)
{
    //return SAMPLE_TEXTURECUBE_ARRAY_LOD(_SkyboxTexture, s_trilinear_clamp_sampler, texCoord, sliceIndex, lod);

    float4 probe0 = SAMPLE_TEXTURECUBE_LOD(_TOD_ReflectionProbe_0, sampler_TOD_ReflectionProbe_0, texCoord, lod);
    float4 probe1 = SAMPLE_TEXTURECUBE_LOD(_TOD_ReflectionProbe_1, sampler_TOD_ReflectionProbe_1, texCoord, lod);
    return lerp(probe0, probe1, saturate(_TOD_RPWeight));
    // return SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0,samplerunity_SpecCube0,texCoord,lod);

    //return LOAD_TEXTURE2D_X_LOD(_SkyboxTexture, pixelCoords, lod);
}

float3 GetFogColor(float3 V, float fragDist)
{
    float3 color = _FogColor.rgb;

    if (_FogColorMode == 1)
    {
        // Based on Uncharted 4 "Mip Sky Fog" trick: http://advances.realtimerendering.com/other/2016/naughty_dog/NaughtyDog_TechArt_Final.pdf
        float mipLevel = (1.0 - _MipFogParameters.z * saturate((fragDist - _MipFogParameters.x) / (_MipFogParameters.y - _MipFogParameters.x))) * (ENVCONSTANTS_CONVOLUTION_MIP_COUNT - 1);
        // For the atmospheric scattering, we use the GGX convoluted version of the cubemap. That matches the of the idnex 0
        color *= SampleSkyTexture(-V, mipLevel).rgb; // '_FogColor' is the tint
        //color *=LOAD_TEXTURE2D_X_LOD(_SkyboxTexture, positionSS, mipLevel);
    }

    return color;
}

// Optical depth between two endpoints.
real OpticalDepthHeightFog(real baseExtinction, real baseHeight, real2 heightExponents,
                           real cosZenith, real startHeight, real intervalLength,out float h)
{
    // Height fog is composed of two slices of optical depth:
    // - homogeneous fog below 'baseHeight': d = k * t
    // - exponential fog above 'baseHeight': d = Integrate[k * e^(-(h + z * x) / H) dx, {x, 0, t}]
    real H          = heightExponents.y;
    real rcpH       = heightExponents.x;
    real Z          = cosZenith;
    real absZ       = max(abs(cosZenith), 0.01f);
    real rcpAbsZ    = rcp(absZ);

    real endHeight  = startHeight + intervalLength * Z;
    real minHeight  = min(startHeight, endHeight);
    h          = max(minHeight - baseHeight, 0);

    real homFogDist = clamp((baseHeight - minHeight) * rcpAbsZ, 0, intervalLength);
    real expFogDist = intervalLength - homFogDist;
    real expFogMult = exp(-h * rcpH) * (1 - exp(-expFogDist * absZ * rcpH)) * (rcpAbsZ * H);

    return baseExtinction * (homFogDist + expFogMult);

}
void EvaluateAtmosphericScattering(PositionInputs posInput, float3 V, out float3 color, out float3 opacity)
{
    color = opacity = 0;

    float fogFragDist = distance(posInput.positionWS, GetCurrentViewPosition());

    float4 volFog = float4(0.0, 0.0, 0.0, 0.0);

    //float expFogStart = 0.0f;
    float expFogStart =_FogStartDistance;

    // TODO: if 'posInput.linearDepth' is computed using 'posInput.positionWS',
    // and the latter resides on the far plane, the computation will be numerically unstable.
    float distDelta = fogFragDist - expFogStart;
    //exp
    //distDelta = exp(_FogStartDistance * fogFragDist/_MaxFogDistance);
    if ((distDelta > 0))
    {
        // Apply the distant (fallback) fog.
        float3 positionWS = GetCurrentViewPosition() - V * expFogStart;

        const float3 magic = float3( 0.06711056, 0.00583715, 52.9829189 );
        float jitter = frac( magic.z * frac( dot( posInput.positionSS.xy, magic.xy ) ) );


        float startHeight = positionWS.y + jitter * _JitterStrength;
        float cosZenith = -V.y;

        // For both homogeneous and exponential media,
        // Integrate[Transmittance[x] * Scattering[x], {x, 0, t}] = Albedo * Opacity[t].
        // Note that pulling the incoming radiance (which is affected by the fog) out of the
        // integral is wrong, as it means that shadow rays are not volumetrically shadowed.
        // This will result in fog looking overly bright.
        float heightoffset;

        float3 volAlbedo = _HeightFogBaseScattering.xyz / _HeightFogBaseExtinction;
        float odFallback = OpticalDepthHeightFog(_HeightFogBaseExtinction, _HeightFogBaseHeight,
                                                 _HeightFogExponents, cosZenith, startHeight, distDelta,heightoffset);
        float trFallback = TransmittanceFromOpticalDepth(odFallback);
        float trCamera = 1 - volFog.a;


        float3 fogColor=GetFogColor(V, fogFragDist);

        #if _CONSTANTCOLOR
        float mipmap = fogFragDist;
        fogColor = lerp(fogColor, _FogEndColor.rgb,pow(max(0,1-exp(-heightoffset * _HeightFogExponents.x)*abs(cosZenith)),clamp((_FogEndColor.a), 0, 100) * fogFragDist * 0.001));
        //fogColor = lerp(fogColor, _FogEndColor.rgb,pow(max(0,1-exp(-heightoffset * _HeightFogExponents.x)*abs(cosZenith)),_FogEndColor.a));
        #endif

        #if _SUNCOLOR_ON
        // float fall=pow(max(0,dot(-V,_MainLightPosition.xyz)*0.5+0.5),_SunFallOff);
        // fogColor=lerp(fogColor,_SunColor.rgb,fall*_SunColor.a);

        // 获取太阳光影响
        float fall=pow(max(0,dot(-V, _SunDirectionWS.xyz)*0.5+0.5),_SunFallOff);
        fogColor=lerp(fogColor,_SunColor.rgb,fall*_SunColor.a);

        // 获取月亮光影响
        float fallMoon=pow(max(0,dot(-V,_MoonDirectionWS.xyz)*0.5+0.5),_MoonFallOff);
        fogColor=lerp(fogColor,_MoonColor.rgb,fallMoon*_MoonColor.a);

        #endif

        volFog.rgb += trCamera * fogColor * volAlbedo * (1 -      // * GetCurrentExposureMultiplier()
            trFallback);
        volFog.a = 1 - (trCamera * trFallback);
    }

    color = volFog.rgb; // Already pre-exposed
    opacity = volFog.a;
}


half3 MixAtmosphericScatteringFog(inout half3 fragColor, float3 positionWS)
{
    half  opacity = 0;
    half3 color = 0;
    float3 ViewDir = positionWS -GetCurrentViewPosition();
    float fogFragDist = length(ViewDir);
    half3 V = normalize(ViewDir);
    float4 volFog = float4(0.0, 0.0, 0.0, 0.0);

    float expFogStart = 0.0f;

    // TODO: if 'posInput.linearDepth' is computed using 'posInput.positionWS',
    // and the latter resides on the far plane, the computation will be numerically unstable.
    float distDelta = fogFragDist - expFogStart;

    if ((distDelta > 0))
    {
        // Apply the distant (fallback) fog.
        float3 positionWS = GetCurrentViewPosition() - V * expFogStart;
        float startHeight = positionWS.y;
        float cosZenith = -V.y;

        // For both homogeneous and exponential media,
        // Integrate[Transmittance[x] * Scattering[x], {x, 0, t}] = Albedo * Opacity[t].
        // Note that pulling the incoming radiance (which is affected by the fog) out of the
        // integral is wrong, as it means that shadow rays are not volumetrically shadowed.
        // This will result in fog looking overly bright.

        float heightoffset;

        float3 volAlbedo = _HeightFogBaseScattering.xyz / _HeightFogBaseExtinction;
        float odFallback = OpticalDepthHeightFog(_HeightFogBaseExtinction, _HeightFogBaseHeight,
                                                 _HeightFogExponents, cosZenith, startHeight, distDelta,heightoffset);
        float trFallback = TransmittanceFromOpticalDepth(odFallback);
        float trCamera = 1 - volFog.a;


        float3 fogColor=GetFogColor(V, fogFragDist);

        #if _CONSTANTCOLOR
        fogColor = lerp(fogColor, _FogEndColor.rgb,pow(max(0,1-exp(-heightoffset * _HeightFogExponents.x)*abs(cosZenith)),_FogEndColor.a));
        #endif

        #if _SUNCOLOR_ON
        float fall=pow(max(0,dot(-V,_MainLightPosition.xyz)*0.5+0.5),_SunFallOff);
        fogColor=lerp(fogColor,_SunColor.rgb,fall*_SunColor.a);
        #endif

        volFog.rgb += trCamera * fogColor * volAlbedo * (1 -      // * GetCurrentExposureMultiplier()
            trFallback);
        volFog.a = 1 - (trCamera * trFallback);
    }

    color = volFog.rgb; // Already pre-exposed
    opacity = volFog.a;

    fragColor = half3(color + (1 - opacity) * fragColor.rgb);// Premultiplied alpha (over operator), preserve alpha for the alpha channel for compositing
    // fragColor = 1;
}


#endif
