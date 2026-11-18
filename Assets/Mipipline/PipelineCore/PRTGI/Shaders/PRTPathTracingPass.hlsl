#ifndef PIPELINE_CORE_PRT_PATH_TRACING_PASS_INCLUDED
#define PIPELINE_CORE_PRT_PATH_TRACING_PASS_INCLUDED

// The Unity 6 material payload is not available in URP 14. Evaluate the same Lit
// surface here and write the subset consumed by the PRT capture ray generation.
static float g_PRTBakeUVFootprint;
void PRTBakeClip(float value) { }
#define clip(value) PRTBakeClip(value)
#define ddx(value) ((value) * 0)
#define ddy(value) ((value) * 0)

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "UnityRayTracingMeshUtils.cginc"

float2 PRTBakeGradX(float2 uv) { return float2(g_PRTBakeUVFootprint, 0); }
float2 PRTBakeGradY(float2 uv) { return float2(0, g_PRTBakeUVFootprint); }
#undef PLATFORM_SAMPLE_TEXTURE2D
#define PLATFORM_SAMPLE_TEXTURE2D(textureName, samplerName, coord2) \
    textureName.SampleGrad(samplerName, coord2, PRTBakeGradX(coord2), PRTBakeGradY(coord2))
#undef PLATFORM_SAMPLE_TEXTURE2D_BIAS
#define PLATFORM_SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, coord2, bias) \
    textureName.SampleGrad(samplerName, coord2, PRTBakeGradX(coord2) * exp2(bias), PRTBakeGradY(coord2) * exp2(bias))

#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
#include "Assets/Mipipline/PipelineCore/PRTGI/Shaders/PRTBakePayload.hlsl"

StructuredBuffer<uint4> _PRTBakeInstances;
StructuredBuffer<uint2> _PRTBakeMaterialKeys;
float _PRTBakeConeSpread;

float2 PRTBakeUV(BuiltInTriangleIntersectionAttributes attributes)
{
    uint3 indices = UnityRayTracingFetchTriangleIndices(PrimitiveIndex());
    if (!UnityRayTracingHasVertexAttribute(kVertexAttributeTexCoord0)) return 0;
    float2 a = UnityRayTracingFetchVertexAttribute2(indices.x, kVertexAttributeTexCoord0);
    float2 b = UnityRayTracingFetchVertexAttribute2(indices.y, kVertexAttributeTexCoord0);
    float2 c = UnityRayTracingFetchVertexAttribute2(indices.z, kVertexAttributeTexCoord0);
    float2 bary = attributes.barycentrics;
    return a * (1.0 - bary.x - bary.y) + b * bary.x + c * bary.y;
}

uint PRTBakeMaterialKey()
{
    uint4 instance = _PRTBakeInstances[InstanceID()];
    uint start = unity_MeshInfo_RT[0].indexStart;
    for (uint i = instance.z; i < instance.z + instance.w; i++)
        if (_PRTBakeMaterialKeys[i].x == start) return _PRTBakeMaterialKeys[i].y;
    return 0;
}

uint PRTBakeHash(uint value)
{
    value ^= value >> 16;
    value *= 0x7feb352du;
    value ^= value >> 15;
    value *= 0x846ca68bu;
    return value ^ (value >> 16);
}

[shader("anyhit")]
void PathTracingAnyHit(inout PRTBakePayload payload : SV_RayPayload,
    BuiltInTriangleIntersectionAttributes attributes : SV_IntersectionAttributes)
{
    g_PRTBakeUVFootprint = 0;
    float2 uv = TRANSFORM_TEX(PRTBakeUV(attributes), _BaseMap);
    float alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).a * _BaseColor.a;
#if defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)
    alpha = _BaseColor.a;
#endif
#if defined(_ALPHATEST_ON)
    if (alpha < _Cutoff) { IgnoreHit(); return; }
#endif
    if (_Surface > 0.5)
    {
        uint seed = PRTBakeHash(payload.seed ^ (InstanceID() * 0x85ebca6bu) ^ PrimitiveIndex());
        if ((seed & 0xffffffu) * (1.0 / 16777216.0) >= saturate(alpha)) IgnoreHit();
    }
}

[shader("closesthit")]
void PathTracingClosestHit(inout PRTBakePayload payload : SV_RayPayload,
    BuiltInTriangleIntersectionAttributes attributes : SV_IntersectionAttributes)
{
    uint3 indices = UnityRayTracingFetchTriangleIndices(PrimitiveIndex());
    float3 p0 = UnityRayTracingFetchVertexAttribute3(indices.x, kVertexAttributePosition);
    float3 p1 = UnityRayTracingFetchVertexAttribute3(indices.y, kVertexAttributePosition);
    float3 p2 = UnityRayTracingFetchVertexAttribute3(indices.z, kVertexAttributePosition);
    float3 faceOS = normalize(cross(p1 - p0, p2 - p0));
    float3x3 toWorld = (float3x3)ObjectToWorld3x4();
    float3x3 toObject = (float3x3)WorldToObject3x4();
    float3 faceWS = normalize(mul(faceOS, toObject));
    bool front = dot(faceWS, WorldRayDirection()) < 0;

    float3 normalOS = faceOS;
    if (UnityRayTracingHasVertexAttribute(kVertexAttributeNormal))
    {
        float3 a = UnityRayTracingFetchVertexAttribute3(indices.x, kVertexAttributeNormal);
        float3 b = UnityRayTracingFetchVertexAttribute3(indices.y, kVertexAttributeNormal);
        float3 c = UnityRayTracingFetchVertexAttribute3(indices.z, kVertexAttributeNormal);
        float2 bary = attributes.barycentrics;
        normalOS = normalize(a * (1.0 - bary.x - bary.y) + b * bary.x + c * bary.y);
    }
    float3 vertexNormalWS = normalize(mul(normalOS, toObject));
    float4 tangentOS = float4(0, 0, 0, 1);
    if (UnityRayTracingHasVertexAttribute(kVertexAttributeTangent))
    {
        float4 a = UnityRayTracingFetchVertexAttribute4(indices.x, kVertexAttributeTangent);
        float4 b = UnityRayTracingFetchVertexAttribute4(indices.y, kVertexAttributeTangent);
        float4 c = UnityRayTracingFetchVertexAttribute4(indices.z, kVertexAttributeTangent);
        float2 bary = attributes.barycentrics;
        tangentOS = a * (1.0 - bary.x - bary.y) + b * bary.x + c * bary.y;
    }
    float3 tangentWS = mul(toWorld, tangentOS.xyz);
    if (dot(tangentWS, tangentWS) < 1e-8)
        tangentWS = cross(abs(vertexNormalWS.y) < 0.9 ? float3(0, 1, 0) : float3(1, 0, 0), vertexNormalWS);
    tangentWS = normalize(tangentWS);
    float tangentSign = (tangentOS.w < 0 ? -1.0 : 1.0) * (determinant(toWorld) < 0 ? -1.0 : 1.0);
    float3 bitangentWS = cross(vertexNormalWS, tangentWS) * tangentSign;

    float2 uv0 = PRTBakeUV(attributes);
    float2 uv = TRANSFORM_TEX(uv0, _BaseMap);
    float2 u0 = UnityRayTracingHasVertexAttribute(kVertexAttributeTexCoord0)
        ? UnityRayTracingFetchVertexAttribute2(indices.x, kVertexAttributeTexCoord0) : 0;
    float2 u1 = UnityRayTracingHasVertexAttribute(kVertexAttributeTexCoord0)
        ? UnityRayTracingFetchVertexAttribute2(indices.y, kVertexAttributeTexCoord0) : 0;
    float2 u2 = UnityRayTracingHasVertexAttribute(kVertexAttributeTexCoord0)
        ? UnityRayTracingFetchVertexAttribute2(indices.z, kVertexAttributeTexCoord0) : 0;
    float uvArea = abs((u1.x - u0.x) * (u2.y - u0.y) - (u2.x - u0.x) * (u1.y - u0.y));
    float3 edge0 = mul(toWorld, p1 - p0), edge1 = mul(toWorld, p2 - p0);
    float worldArea = length(cross(edge0, edge1));
    g_PRTBakeUVFootprint = sqrt(uvArea / max(worldArea, 1e-20)) *
        (RayTCurrent() * _PRTBakeConeSpread) / max(abs(dot(WorldRayDirection(), faceWS)), 1e-4);

#if defined(_PARALLAXMAP)
    half3 viewTS = GetViewDirectionTangentSpace(half4(tangentWS, tangentSign), vertexNormalWS, -WorldRayDirection());
    ApplyPerPixelDisplacement(viewTS, uv);
#endif
    SurfaceData surface;
    InitializeStandardLitSurfaceData(uv, surface);
    float3 normalWS = normalize(tangentWS * surface.normalTS.x +
        bitangentWS * surface.normalTS.y + vertexNormalWS * surface.normalTS.z);
    if (dot(normalWS, vertexNormalWS) * (front ? 1.0 : -1.0) < 0) normalWS = -normalWS;

    uint4 instance = _PRTBakeInstances[InstanceID()];
    payload.distance = RayTCurrent();
    payload.normal = normalWS;
    payload.frontFacing = front ? 1u : 0u;
    payload.faceNormal = faceWS;
    payload.albedo = max(surface.albedo * (1.0 - surface.metallic), 0);
#if defined(_SPECULAR_SETUP)
    payload.albedo = max(surface.albedo, 0);
#endif
    payload.renderingLayerMask = instance.x;
    payload.objectLayerMask = instance.y;
    payload.materialKey = PRTBakeMaterialKey();
}

#endif
