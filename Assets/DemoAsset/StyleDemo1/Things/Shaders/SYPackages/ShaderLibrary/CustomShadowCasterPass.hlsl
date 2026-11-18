#ifndef UNIVERSAL_SHIYUE_SHADOW_CASTER_PASS_INCLUDED
#define UNIVERSAL_SHIYUE_SHADOW_CASTER_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

float3 _LightDirection;

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 texcoord     : TEXCOORD0;
    float3 color        : COLOR;
    float2 texcoord2    : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 uv           : TEXCOORD0;
    float4 positionCS   : SV_POSITION;
};

float4 GetShadowPositionHClip(Attributes input)
{
    #ifdef _SIMPLE_WIND
        SimpleWind(input.color, float3(UNITY_MATRIX_M[0][3], UNITY_MATRIX_M[1][3], UNITY_MATRIX_M[2][3]), input.positionOS.xyz);
    #endif
    //风效
    #if defined (_WIND_ON)
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    float2 WindUV =  (_Time.yy + vertexInput.positionWS.xz) * _WindSpeed/25;
    input.positionOS.xyz += (SAMPLE_TEXTURE2D_LOD(_NoiseMap, sampler_NoiseMap, WindUV,0).xyz * 2 - 0.5)  * _WindStrength *input.color.r;
    #elif defined (_WINDSIN_ON)
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    input.positionOS.xyz += sin(_Time.y * _WindSpeed+input.texcoord2.x*PI*_WindStrength.w+ unity_ObjectToWorld._14_24_34)*input.color.r* _WindStrength.xyz;
    #endif

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

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
    #ifdef _SIMPLE_WIND
    SimpleWind(input.color, float3(UNITY_MATRIX_M[0][3], UNITY_MATRIX_M[1][3], UNITY_MATRIX_M[2][3]), input.positionOS.xyz);
    #endif
    output.uv.xy = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.uv.zw = input.texcoord2.xy;
    output.positionCS = GetShadowPositionHClip(input);
    return output;
}


half4 ShadowPassFragment(Varyings input) : SV_TARGET
{
    //Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);  
    half alpha =SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap, input.uv).a;//*_BaseColor.a;//SampleMainTex(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a * _Color.a; 
    #if defined(_ALPHATEST_ON)
    clip(alpha - _Cutoff);
    #endif
    
   #if _RESIDENTEFFECT_ON
    float2 uv = input.uv.zw;
    DirDissloveAlpha(float4(uv.xy,uv.xy* _ResidentDisTex_ST.xy + _ResidentDisTex_ST.zw+_Time.y*_ResidentDissloveSpeed.xy),_ResidentDissloveEdgeLength,_ResidentDissloveThreshold, _ResidentBlankingDis, _ResidentBlankingRange);
    #endif
    
    #if _DISSLOVE_ON
    DissloveAlpha(input.uv,_DissloveEdgeLength,_DissloveThreshold);
    #endif
    
    return 0;
}

#endif
