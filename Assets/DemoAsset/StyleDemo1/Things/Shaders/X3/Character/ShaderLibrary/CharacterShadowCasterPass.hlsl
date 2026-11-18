#ifndef UNIVERSAL_SHIYUE_CHARACTER_SHADOW_CASTER_PASS_INCLUDED
#define UNIVERSAL_SHIYUE_CHARACTER_SHADOW_CASTER_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

float3 _LightDirection;


struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 texcoord     : TEXCOORD0;
    float3 color        : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv           : TEXCOORD0;
    float4 positionCS   : SV_POSITION;
};

/**
 * \brief 自定义阴影偏移
 * \param positionWS 
 * \param normalWS 
 * \param lightDirection 
 * \return 
 */
float3 CustomApplyShadowBias(float3 positionWS, float3 normalWS, float3 lightDirection)
{
    float invNdotL = 1.0 - saturate(dot(lightDirection, normalWS));
    float scale = invNdotL * _CustomShadowNormalBias * _ShadowBias.y;

    // normal bias is negative since we want to apply an inset normal offset
    positionWS = lightDirection * _CustomShadowDepthBias.xxx * _ShadowBias.xxx + positionWS;
    positionWS = normalWS * scale.xxx + positionWS;
    return positionWS;
}

float4 GetShadowPositionHClip(Attributes input)
{
    //风效
    #if defined (_WIND_ON)
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    float2 WindUV =  (_Time.yy + vertexInput.positionWS.xz) * _WindSpeed/25;
    input.positionOS.xyz += SAMPLE_TEXTURE2D_LOD(_NoiseMap, sampler_NoiseMap, WindUV,0)  * _WindStrength * input.color.g;
    #endif

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

    float4 positionCS = TransformWorldToHClip(CustomApplyShadowBias(positionWS, normalWS, _LightDirection));

#if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
#else
    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
#endif

    return positionCS;
}

Varyings ShadowPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);

    output.uv = input.texcoord;
    output.positionCS = GetShadowPositionHClip(input);
    return output;
}

half4 ShadowPassFragment(Varyings input) : SV_TARGET
{
    half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a; 
    #if defined(_ALPHATEST_ON)
    clip(alpha - _Cutoff);
    #endif
    return 0;
}

void transparencyClip(float alpha, float2 screenPos)
{
    float4x4 thresholdMatrix =
    {
        1.0 / 17.0, 9.0 / 17.0, 3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0, 5.0 / 17.0, 15.0 / 17.0, 7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0, 2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0, 8.0 / 17.0, 14.0 / 17.0, 6.0 / 17.0
    };

    clip(alpha - thresholdMatrix[fmod(screenPos.x, 4)][fmod(screenPos.y, 4)]);
}

half4 ShadowPassFragmentBlend(Varyings input) : SV_TARGET
{
    #if defined(_ALPHATEST_ON)
        half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
        clip(alpha - _Cutoff);
// #elif _ALPHABLEND_ON
        // float2 screenPos = input.positionCS.xy / (input.positionCS.w + 0.0001);
        // transparencyClip(alpha, screenPos);
    #endif
    return 0;
}

#endif
