#ifndef UNIVERSAL_SHIYUE_DEPTH_ONLY_PASS_INCLUDED
#define UNIVERSAL_SHIYUE_DEPTH_ONLY_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/Common.hlsl"
#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/AdditionalInput.hlsl"
struct Attributes
{
    float4 position     : POSITION;
    float2 texcoord     : TEXCOORD0;
    float3 color        : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv           : TEXCOORD0;
    float4 positionCS   : SV_POSITION;
    float4 screenPos    : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings DepthOnlyVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    //风效
    #if defined (_WIND_ON)
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.position.xyz);
    float2 WindUV =  (_Time.yy + vertexInput.positionWS.xz) * _WindSpeed/25;
    input.position.xyz += SAMPLE_TEXTURE2D_LOD(_NoiseMap, sampler_NoiseMap, WindUV,0)  * _WindStrength * input.color.g;
    #endif

    output.uv = input.texcoord;
    output.positionCS = TransformObjectToHClip(input.position.xyz);
    output.screenPos = ComputeScreenPos(output.positionCS);
    return output;
}

half4 DepthOnlyFragment(Varyings input) : SV_TARGET
{
    DitherThreshold(input.screenPos,input.positionCS.xy,GetDitherStrength());
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a*_BaseColor.a;
    #if defined(_ALPHATEST_ON)
    clip(alpha - _Cutoff);
    #endif
    return 0;
}





#endif