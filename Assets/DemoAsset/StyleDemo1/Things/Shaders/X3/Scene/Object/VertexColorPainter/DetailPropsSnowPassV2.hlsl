#ifndef UNIVERSAL_DETAILPROPSSNOW_PASS_INCLUDED
#define UNIVERSAL_DETAILPROPSSNOW_PASS_INCLUDED

//#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/CustomLighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/ScanZoneCore.hlsl"
#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/EnvironmentDepthCore.hlsl"
#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages//ShaderLibrary/TerrainBlend.hlsl"

#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/Effects_Addition.hlsl"

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"



struct Attributes
{
    float4 positionOS   : POSITION;
    float4 color        : COLOR;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 texcoord     : TEXCOORD0;
    float2 lightmapUV   : TEXCOORD1;
    uint vertexID       : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv                       : TEXCOORD0;
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    float3 positionWS               : TEXCOORD2;
#endif

    float3 normalWS                 : TEXCOORD3;
// #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    float4 tangentWS                : TEXCOORD4;    // xyz: tangent, w: sign
// #endif
    float3 viewDirWS                : TEXCOORD5;

    float3 bitangentWS              : TEXCOORD6;

    half4 fogFactorAndVertexLight   : TEXCOORD7; // x: fogFactor, yzw: vertex light

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord              : TEXCOORD8;
#endif

#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    float3 viewDirTS                : TEXCOORD9;
#endif

#if defined(_TERRAINBLEND_ON)
    float4 screenPos                : TEXCOORD10;
#endif

    float4 positionCS               : SV_POSITION;
    float4 color                    : COLOR;
    float4 normal                   : TEXCOORD11;

    // 材质混合
    // half2 blendV2                   : TEXCOORD12;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    inputData.positionWS = input.positionWS;
#endif

    half3 viewDirWS = SafeNormalize(input.viewDirWS);
#if defined(_NORMALMAP)
    float sgn = input.tangentWS.w;      // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    inputData.normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));
#else
    inputData.normalWS = input.normalWS;
#endif

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = viewDirWS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif

    inputData.fogCoord = input.fogFactorAndVertexLight.x;
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
    inputData.bakedGI = EnvironmentLightDepthGI(SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS),input.positionWS);
    //inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////
StructuredBuffer<float3> _BaseNormals;
StructuredBuffer<float4> _BaseTangents;

float3 ToBaseTangentSpace(uint vid, float3 n)
{
    float3 base_normal = _BaseNormals[vid];
    float4 base_tangent = _BaseTangents[vid];
    float3 base_binormal = normalize(cross(base_normal, base_tangent.xyz) * base_tangent.w);
    float3x3 tbn = float3x3(base_tangent.xyz, base_binormal, base_normal);
    //return normalize(mul(n, transpose(tbn)));
    return base_normal * 2 + 0.4;
}

// Used in Standard (Physically Based) shader
Varyings LitPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    // 配置 Blend 贴图混合
    // VertexBlendV2(output.blendV2, input.texcoord, input.color);

    #ifdef _SIMPLE_WIND
        SimpleWind(input.color.rgb, float3(UNITY_MATRIX_M[0][3], UNITY_MATRIX_M[1][3], UNITY_MATRIX_M[2][3]), input.positionOS.xyz);
    #endif
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    half3 viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
    half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

    output.uv = input.texcoord;
    output.normalWS = normalInput.normalWS;
    output.viewDirWS = viewDirWS;
    real sign = input.tangentOS.w * GetOddNegativeScale();
    half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
    output.tangentWS = tangentWS;

#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirTS = GetViewDirectionTangentSpace(tangentWS, output.normalWS, viewDirWS);
    output.viewDirTS = viewDirTS;
#endif

    OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    output.positionWS = vertexInput.positionWS;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif

    output.positionCS = vertexInput.positionCS;
    #if defined(_TERRAINBLEND_ON)
    output.screenPos = ComputeScreenPos(vertexInput.positionCS);
    output.screenPos.z = -TransformWorldToView(output.positionWS).z;
    #endif
    output.color = input.color;

    return output;
}

half3 ApplyTonemap(half3 input)
{
    #if _TONEMAP_ACES
    float3 aces = unity_to_ACES(input);
    input = AcesTonemap(aces);
    return saturate(input);
    #elif _TONEMAP_NEUTRAL
    input = NeutralTonemap(input);
    return saturate(input);
    #endif
    return  input;

}

// Used in Standard (Physically Based) shader
half4 LitPassFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    SurfaceData surfaceData;

    // rongqingmei
    // return half4(input.color.rrr, 1.0f);
    float2 mainMapUV = TRANSFORM_TEX(input.uv, _BaseAlbedoASmoothness);
    InitializeDetailPropsSnowSurfaceData(mainMapUV, input.uv.xy, input.color, input.normalWS.xyz, input.tangentWS, input.bitangentWS, surfaceData);

    #if defined(_DITHER)
    float4 postionCS = TransformWorldToHClip(input.positionWS);
    float4 scrPos = ComputeScreenPos(postionCS);
    DitherThreshold(scrPos,_DitherTimer);
    #endif

    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);

    #if defined(_TERRAINBLEND_ON)
    TerrainBlendWithMetallic(input.positionWS,input.screenPos,_TerrainBlendHeight, surfaceData, inputData);
    #endif

    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);

    #ifdef _DBUFFER
    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
    #endif

    half4 color = UniversalFragmentPBR(inputData, surfaceData);

    half fogCoord = lerp(inputData.fogCoord, 1, _FogClose);
    color.rgb = MixFog(color.rgb, fogCoord);
    return color;
}
//
// half4 LitPassFragment_LOD0(Varyings input) : SV_Target
// {
//     UNITY_SETUP_INSTANCE_ID(input);
//     UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
//     SurfaceData surfaceData;
//
//     InitializeDetailPropsSnowSurfaceData_LOD0(input.uv, input.color, input.normalWS.xyz, input.tangentWS, input.bitangentWS, surfaceData);
//
//     #if defined(_DITHER)
//         float4 postionCS = TransformWorldToHClip(input.positionWS);
//         float4 scrPos = ComputeScreenPos(postionCS);
//         DitherThreshold(scrPos,_DitherTimer);
//     #endif
//
//
//     InputData inputData;
//     InitializeInputData(input, surfaceData.normalTS, inputData);
//
//     #if defined(_TERRAINBLEND_ON)
//     TerrainBlendWithMetallic(input.positionWS,input.screenPos,_TerrainBlendHeight, surfaceData, inputData);
//     #endif
//
//     ScanZone_SceneColor(surfaceData.albedo.rgb, input.positionWS);
//
//     half4 color = UniversalFragmentPBR(inputData, surfaceData);
//
//     color.rgb = MixFog(color.rgb, inputData.fogCoord);
//     //return half4(inputData.bakedGI.rgb, 1);
//     #if !LIMITDODGESPACE
//     color.rgb = ApplyTonemap(color.rgb);
//     #endif
//     //return float4(surfaceData.albedo, 1);
//     return color;
// }
//
// half4 LitPassFragmentLOD1(Varyings input) : SV_Target
// {
//     UNITY_SETUP_INSTANCE_ID(input);
//     UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
//     SurfaceData surfaceData;
//
//     InitializeDetailPropsSnowSurfaceDataLOD1(input.uv, input.color, input.normalWS.xyz, input.tangentWS, input.bitangentWS, surfaceData);
//     ScanZone_SceneColor(surfaceData.albedo.rgb, input.positionWS);
//
//     //#if defined(_TERRAINBLEND_ON)
//     //TerrainBlend(input.screenPos,_TerrainBlendHeight,surfaceData.smoothness,surfaceData.albedo,input.normalWS);
//     //#endif
//
//     InputData inputData;
//     InitializeInputData(input, surfaceData.normalTS, inputData);
//     half4 color = UniversalFragmentPBR(inputData, surfaceData);
//     color.rgb = MixFog(color.rgb, inputData.fogCoord);
//     return color;
// }
//
// half4 LitPassFragmentLOD2(Varyings input) : SV_Target
// {
//     UNITY_SETUP_INSTANCE_ID(input);
//     UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
//     SurfaceData surfaceData;
//     InitializeDetailPropsSnowSurfaceData(input.uv, input.color, input.normalWS.xyz, input.tangentWS, input.bitangentWS, surfaceData);
//
//     InputData inputData;
//     InitializeInputData(input, surfaceData.normalTS, inputData);
//     half4 color = input.color;
//     color.rgb = MixFog(color.rgb, inputData.fogCoord);
//
//     #if !LIMITDODGESPACE
//     color.rgb = ApplyTonemap(color.rgb);
//     #endif
//
//     return color;
// }

#endif