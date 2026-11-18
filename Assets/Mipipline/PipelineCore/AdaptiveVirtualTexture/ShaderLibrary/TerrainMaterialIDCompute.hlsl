#ifndef TERRAIN_MATERIAL_ID_COMPUTE_INCLUDED
#define TERRAIN_MATERIAL_ID_COMPUTE_INCLUDED

#define MULTIPLE_TYPE_BLEND3(type) \
type Blend(type input0, type input1, type input2, type input3, float4 weight) { \
return input0*weight.x + input1*weight.y + input2*weight.z + input3*weight.w; \
}
MULTIPLE_TYPE_BLEND3(float)
MULTIPLE_TYPE_BLEND3(float2)
MULTIPLE_TYPE_BLEND3(float3)
MULTIPLE_TYPE_BLEND3(float4)

float4 CalculateBlendWeight(float4 weight, float4 height, float heightBlendBias = 0.1)
{
    weight *= height;
    const float maxWeight = max(max(max(max(weight.x, weight.y), weight.z), weight.w) - heightBlendBias, 0);
    weight = float4(max(weight.x - maxWeight, 0), max(weight.y - maxWeight, 0), max(weight.z - maxWeight, 0), max(weight.w - maxWeight, 0));
    return weight / (weight.x + weight.y + weight.z + weight.w);
}

float4 RectGrid(float2 uv, out float2 vertexPosX, out float2 vertexPosY, out float2 vertexPosZ, out float2 vertexPosW)
{
    const float2 base = floor(uv + 0.5);
    vertexPosW = base + 0.5;
    vertexPosX = base - 0.5;
    vertexPosY = float2(vertexPosW.x - 1.0, vertexPosW.y);
    vertexPosZ = float2(vertexPosW.x, vertexPosW.y - 1.0);
    const float2 offset = uv + 0.5 - base;
    const float2 oneMinusOffset = 1.0 - offset;
    return float4(oneMinusOffset.x * oneMinusOffset.y, oneMinusOffset.x * offset.y, offset.x * oneMinusOffset.y, offset.x * offset.y);
}

#endif