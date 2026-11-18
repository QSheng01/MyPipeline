#ifndef URP_TERRAIN_MATERIAL_ID_VT_PASSES_INCLUDED
#define URP_TERRAIN_MATERIAL_ID_VT_PASSES_INCLUDED

#include "Assets/Mipipline/PipelineCore/AdaptiveVirtualTexture/ShaderLibrary/AvtTerrainInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#if defined(GPU_DRIVEN_TERRAIN)
#include "Assets/Mipipline/PipelineCore/GPUDrivenTerrain/Shaders/GPUTerrainGeometry.hlsl"
#endif

#if defined(AVT_PASS_FORWARD) || defined(AVT_PASS_GBUFFER)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"
#include "Assets/Mipipline/PipelineCore/AdaptiveVirtualTexture/ShaderLibrary/TerrainMaterialIDUtility.hlsl"
#include "Assets/Mipipline/PipelineCore/AdaptiveVirtualTexture/ShaderLibrary/VirtualTexture.hlsl"
#include "Assets/Mipipline/PipelineCore/AdaptiveVirtualTexture/ShaderLibrary/ClipmapTexture.hlsl"

TEXTURE2D_ARRAY(_BaseMapArray);
TEXTURE2D_ARRAY(_MaskMapArray);
TEXTURE2D(_SplatMap);
TEXTURE2D(_GlobalMap);
SAMPLER(sampler_GlobalMap);
SamplerState sampler_linear_repeat;
// _NearBlendDistance / _FarBlendDistance live in UnityPerMaterial, see AvtTerrainInput.hlsl.
#endif

struct AvtAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct AvtVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float2 uv : TEXCOORD2;
#if defined(GPU_DRIVEN_TERRAIN)
    nointerpolation uint terrainLOD : TEXCOORD3;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void ApplyAvtGeometry(inout AvtAttributes input)
{
#if defined(GPU_DRIVEN_TERRAIN)
    uint index = 0;
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) || defined(UNITY_INSTANCING_ENABLED)
    index = unity_InstanceID;
#endif
    ApplyGPUTerrain(input.positionOS, input.normalOS, input.texcoord, index);
#else
    TerrainInstancing(input.positionOS, input.normalOS, input.texcoord);
#endif
}

AvtVaryings AvtVert(AvtAttributes input)
{
    AvtVaryings output = (AvtVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    ApplyAvtGeometry(input);
    VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
#if defined(GPU_DRIVEN_TERRAIN)
    uint index = 0;
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) || defined(UNITY_INSTANCING_ENABLED)
    index = unity_InstanceID;
#endif
    output.terrainLOD = _GPUTerrainPatches[index].z;
#endif
    output.positionCS = positionInputs.positionCS;
    output.positionWS = positionInputs.positionWS;
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.uv = input.texcoord;
    return output;
}

#if defined(AVT_PASS_FORWARD) || defined(AVT_PASS_GBUFFER)
void EvaluateTerrain(AvtVaryings input, out float3 albedo, out float3 normalWS, out float smoothness, out float occlusion)
{
    float3 positionWS = GetAbsolutePositionWS(input.positionWS);
    float4 baseMap, maskMap;
#if defined(_CLIPMAP_TEXTURE)
    SampleClipmap(positionWS, baseMap, maskMap);
#elif defined(_VIRTUAL_TEXTURE)
    SampleVT(input.positionWS, baseMap, maskMap);
#else
    float2 vertexPosX, vertexPosY, vertexPosZ, vertexPosW;
    float4 weight = RectGrid(positionWS.xz, vertexPosX, vertexPosY, vertexPosZ, vertexPosW);
    float4 slot;
    slot.x = LOAD_TEXTURE2D_LOD(_SplatMap, vertexPosX, 0).r * 255.5;
    slot.y = LOAD_TEXTURE2D_LOD(_SplatMap, vertexPosY, 0).r * 255.5;
    slot.z = LOAD_TEXTURE2D_LOD(_SplatMap, vertexPosZ, 0).r * 255.5;
    slot.w = LOAD_TEXTURE2D_LOD(_SplatMap, vertexPosW, 0).r * 255.5;
    SampleMaterialID(_BaseMapArray, _MaskMapArray, sampler_linear_repeat, positionWS.xz, slot, weight, baseMap, maskMap);
#endif

    float3 normalTS = UnpackNormalAG(float4(1.0, maskMap.g, 1.0, maskMap.r));
    float4 globalMap = SAMPLE_TEXTURE2D(_GlobalMap, sampler_GlobalMap, input.uv);
    float distanceToCamera = length(GetCameraPositionWS() - positionWS);
    float globalBlendWeight = saturate((distanceToCamera - _NearBlendDistance) / max(_FarBlendDistance, 0.001));
    BlendWithGlobalMap(globalMap.rgb, globalBlendWeight, baseMap, maskMap, albedo, smoothness, occlusion);
#if defined(_CLIPMAP_TEXTURE)
    if (ClipmapDebug != 0)
    {
        albedo = ClipmapDebugColor(positionWS.xz);
        smoothness = 0.15;
        occlusion = 1;
    }
#endif
    normalTS = lerp(normalTS, float3(0, 0, 1), globalBlendWeight);

#if defined(GPU_DRIVEN_TERRAIN)
    float3 geoNormalWS = GPUTerrainNormal(input.uv);
#elif defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
    float2 sampleCoords = (input.uv / _TerrainHeightmapRecipSize.zw + 0.5) * _TerrainHeightmapRecipSize.xy;
    float3 geoNormalWS = TransformObjectToWorldNormal(normalize(SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_TerrainNormalmapTexture, sampleCoords).rgb * 2 - 1));
#else
    float3 geoNormalWS = normalize(input.normalWS);
#endif
    float3 tangentWS = normalize(cross(geoNormalWS, float3(0, 0, 1)));
    float3 bitangentWS = cross(geoNormalWS, tangentWS);
    normalWS = NormalizeNormalPerPixel(normalize(tangentWS * normalTS.x + bitangentWS * normalTS.y + geoNormalWS * normalTS.z));

#if defined(_OUTPUT_PAGE_ID_TEXTURE)
    OutputPageID(positionWS, (uint2)input.positionCS.xy);
#endif
}

void BuildInputData(AvtVaryings input, float3 normalWS, out InputData inputData)
{
    inputData = (InputData)0;
    inputData.positionWS = input.positionWS;
    inputData.positionCS = input.positionCS;
    inputData.normalWS = normalWS;
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
    inputData.fogCoord = ComputeFogFactor(input.positionCS.z);
    inputData.vertexLighting = 0;
    inputData.bakedGI = SampleSH(normalWS);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = 1;
}

SurfaceData BuildSurface(float3 albedo, float smoothness, float occlusion)
{
    SurfaceData surface = (SurfaceData)0;
    surface.albedo = albedo;
    surface.metallic = 0;
    surface.specular = 0;
    surface.smoothness = smoothness;
    surface.occlusion = occlusion;
    surface.alpha = 1;
    surface.normalTS = float3(0, 0, 1);
    return surface;
}
#endif

#if defined(AVT_PASS_FORWARD)
#if !defined(GPU_DRIVEN_TERRAIN)
[earlydepthstencil]
#endif
half4 AvtForwardFrag(AvtVaryings input) : SV_Target
{
#if defined(GPU_DRIVEN_TERRAIN)
    GPUTerrainClipHoles(input.uv);
    if (_GPUTerrainDebug != 0) return half4(GPUTerrainDebugColor(input.uv, input.terrainLOD), 1);
#endif
    UNITY_SETUP_INSTANCE_ID(input);
#if defined(_CLIPMAP_TEXTURE)
    if (ClipmapDebug != 0)
        return half4((half3)ClipmapDebugColor(GetAbsolutePositionWS(input.positionWS).xz), 1);
#endif
    float3 albedo, normalWS;
    float smoothness, occlusion;
    EvaluateTerrain(input, albedo, normalWS, smoothness, occlusion);
    InputData inputData;
    BuildInputData(input, normalWS, inputData);
    SurfaceData surface = BuildSurface(albedo, smoothness, occlusion);
    half4 color = UniversalFragmentPBR(inputData, surface);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    return color;
}
#endif

#if defined(AVT_PASS_GBUFFER)
#if !defined(GPU_DRIVEN_TERRAIN)
[earlydepthstencil]
#endif
FragmentOutput AvtGBufferFrag(AvtVaryings input)
{
#if defined(GPU_DRIVEN_TERRAIN)
    GPUTerrainClipHoles(input.uv);
    if (_GPUTerrainDebug != 0)
    {
        InputData data;
        BuildInputData(input, normalize(input.normalWS), data);
        BRDFData brdf;
        half alpha = 1;
        InitializeBRDFData(0, 0, 0, 0, alpha, brdf);
        return BRDFDataToGbuffer(brdf, data, 0, GPUTerrainDebugColor(input.uv, input.terrainLOD), 1);
    }
#endif
    UNITY_SETUP_INSTANCE_ID(input);
#if defined(_CLIPMAP_TEXTURE)
    if (ClipmapDebug != 0)
    {
        float3 debugColor = ClipmapDebugColor(GetAbsolutePositionWS(input.positionWS).xz);
        InputData inputData;
        BuildInputData(input, normalize(input.normalWS), inputData);
        BRDFData brdfData;
        half debugAlpha = 1;
        InitializeBRDFData(0, 0, 0, 0, debugAlpha, brdfData);
        return BRDFDataToGbuffer(brdfData, inputData, 0, debugColor, 1);
    }
#endif
    float3 albedo, normalWS;
    float smoothness, occlusion;
    EvaluateTerrain(input, albedo, normalWS, smoothness, occlusion);
    InputData inputData;
    BuildInputData(input, normalWS, inputData);
    SurfaceData surface = BuildSurface(albedo, smoothness, occlusion);
    BRDFData brdfData;
    InitializeBRDFData(surface.albedo, surface.metallic, surface.specular, surface.smoothness, surface.alpha, brdfData);
    return BRDFDataToGbuffer(brdfData, inputData, surface.smoothness, surface.emission, surface.occlusion);
}
#endif

#if defined(AVT_PASS_SHADOW) || defined(AVT_PASS_DEPTH) || defined(AVT_PASS_DEPTH_NORMALS)
struct AvtLeanVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};

float3 _LightDirection;
float3 _LightPosition;
#endif

#if defined(AVT_PASS_SHADOW)
AvtLeanVaryings AvtShadowVert(AvtAttributes input)
{
    AvtLeanVaryings output = (AvtLeanVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    ApplyAvtGeometry(input);
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
#if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif
    float4 clipPos = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
#if UNITY_REVERSED_Z
    clipPos.z = min(clipPos.z, UNITY_NEAR_CLIP_VALUE);
#else
    clipPos.z = max(clipPos.z, UNITY_NEAR_CLIP_VALUE);
#endif
    output.positionCS = clipPos;
    output.uv = input.texcoord;
    return output;
}

half4 AvtShadowFrag(AvtLeanVaryings input) : SV_Target
{
#if defined(GPU_DRIVEN_TERRAIN)
    GPUTerrainClipHoles(input.uv);
#endif
    return 0;
}
#endif

#if defined(AVT_PASS_DEPTH)
AvtLeanVaryings AvtDepthVert(AvtAttributes input)
{
    AvtLeanVaryings output = (AvtLeanVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    ApplyAvtGeometry(input);
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv = input.texcoord;
    return output;
}

half4 AvtDepthFrag(AvtLeanVaryings input) : SV_Target
{
#if defined(GPU_DRIVEN_TERRAIN)
    GPUTerrainClipHoles(input.uv);
#endif
    return input.positionCS.z;
}
#endif

#if defined(AVT_PASS_DEPTH_NORMALS)
AvtLeanVaryings AvtDepthNormalsVert(AvtAttributes input)
{
    AvtLeanVaryings output = (AvtLeanVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    ApplyAvtGeometry(input);
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv = input.texcoord;
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    return output;
}

half4 AvtDepthNormalsFrag(AvtLeanVaryings input) : SV_Target
{
#if defined(GPU_DRIVEN_TERRAIN)
    GPUTerrainClipHoles(input.uv);
#endif
#if defined(GPU_DRIVEN_TERRAIN)
    float3 normalWS = GPUTerrainNormal(input.uv);
#elif defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
    float2 sampleCoords = (input.uv / _TerrainHeightmapRecipSize.zw + 0.5) * _TerrainHeightmapRecipSize.xy;
    float3 normalWS = TransformObjectToWorldNormal(normalize(SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_TerrainNormalmapTexture, sampleCoords).rgb * 2 - 1));
#else
    float3 normalWS = normalize(input.normalWS);
#endif
    return half4(normalWS * 0.5 + 0.5, 0);
}
#endif

#endif