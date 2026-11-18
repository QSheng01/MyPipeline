#ifndef UNIVERSAL_SHIYUE_DEPTH_ONLY_PASS_INCLUDED
#define UNIVERSAL_SHIYUE_DEPTH_ONLY_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/Common.hlsl"
#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/AdditionalInput.hlsl"
struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 texcoord     : TEXCOORD0;
    float2 lightmapUV   : TEXCOORD1;
    half4 color    : COLOR;
    float2 texcoord2    : TEXCOORD2;
    float2 texcoord3    : TEXCOORD3;
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
    #ifdef _SIMPLE_WIND
        SimpleWind(input.color, float3(UNITY_MATRIX_M[0][3], UNITY_MATRIX_M[1][3], UNITY_MATRIX_M[2][3]), input.positionOS.xyz);
    #endif
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

    // normalWS and tangentWS already normalize.
    // this is required to avoid skewing the direction during interpolation
    // also required for per-vertex lighting and SH evaluation
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    #ifdef _WINDSIN_ON
    //wind1.0
    // float2 WindUV = (_Time.yy + vertexInput.positionWS.xz) * _WindSpeed / 25;
    //input.positionOS.xyz += sin(_Time.y * _WindSpeed+input.texcoord2.x*PI*_WindStrength.w+ unity_ObjectToWorld._14_24_34)*input.color.r* _WindStrength.xyz;
    //(SAMPLE_TEXTURE2D_LOD(_NoiseMap, sampler_NoiseMap, WindUV, 0).xyz * 2 - 0.5) * _WindStrength*input.color.r;
    //vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

    //wind2.0
    float vertexAll = (input.positionOS.x + input.positionOS.y + input.positionOS.z) * 0.333;
    float sinInf = sin(_Time.y * _WindSpeed + vertexAll);
    float vertexColor = clamp(abs(input.color.r), 0, 1);
    input.positionOS.x += _RightDir * sinInf * vertexColor;
    input.positionOS.y += _FrontDir * sinInf * vertexColor;
    input.positionOS.z += _UpDir * sinInf * vertexColor;
    vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    #endif

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.positionCS = vertexInput.positionCS;
    output.screenPos = ComputeScreenPos(output.positionCS);
    return output;
}

half4 DepthOnlyFragment(Varyings input) : SV_TARGET
{
    DitherThreshold(input.screenPos,input.positionCS.xy,_DitherTimer);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
   // half alpha =SampleMainTex(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a*_Color.a;
    half alpha =SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap, input.uv).a;
    #if defined(_ALPHATEST_ON)
    clip(alpha - _Cutoff);
    
    #endif
    #if _DISSLOVE_ON
    DissloveAlpha(input.uv,_DissloveEdgeLength,_DissloveThreshold);
    #endif
    
    return 0;
}
#endif