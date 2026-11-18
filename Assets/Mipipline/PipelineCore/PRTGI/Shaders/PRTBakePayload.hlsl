#ifndef PIPELINE_CORE_PRT_BAKE_PAYLOAD_INCLUDED
#define PIPELINE_CORE_PRT_BAKE_PAYLOAD_INCLUDED

struct PRTBakePayload
{
    float distance;
    float3 normal;
    float3 albedo;
    uint renderingLayerMask;
    uint objectLayerMask;
    uint materialKey;
    uint frontFacing;
    float3 faceNormal;
    uint seed;
};

struct PRTCaptureSample
{
    float3 position;
    float3 normal;
    float3 albedo;
    uint flags;
    uint renderingLayerMask;
    uint objectLayerMask;
    uint materialKey;
    int nearestProbe;
};

#endif
