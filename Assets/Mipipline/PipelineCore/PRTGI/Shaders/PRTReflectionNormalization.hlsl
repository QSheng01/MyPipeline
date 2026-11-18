#ifndef PIPELINE_CORE_PRT_REFLECTION_NORMALIZATION_INCLUDED
#define PIPELINE_CORE_PRT_REFLECTION_NORMALIZATION_INCLUDED

#define PRT_REFLECTION_LIMIT 8
int _PRTReflectionCount;
float4 _PRTReflectionParams; // min factor, max factor, unused, blend weight
float4 _PRTReflectionL0L1[PRT_REFLECTION_LIMIT];
float4 _PRTReflectionL2[PRT_REFLECTION_LIMIT];
float4 _PRTReflectionL2Last[PRT_REFLECTION_LIMIT];
float4 _PRTReflectionBoxMin[PRT_REFLECTION_LIMIT];
float4 _PRTReflectionBoxMax[PRT_REFLECTION_LIMIT];

float PRTReferenceLuminance(int index, float3 normal)
{
    float4 l01 = _PRTReflectionL0L1[index];
    float4 l2 = _PRTReflectionL2[index];
    float4 v = normal.xyzz * normal.yzzx;
    return l01.x + dot(l01.yzw, normal) + dot(l2, v) +
        _PRTReflectionL2Last[index].x * (normal.x * normal.x - normal.y * normal.y);
}

half3 PRTNormalizeReflection(half3 shaded, InputData inputData, SurfaceData surfaceData)
{
    if (_PRTEnabled < 0.5 || _PRTReflectionCount == 0 || _PRTReflectionParams.w <= 0) return shaded;
    half3 reflectionDir = reflect(-inputData.viewDirectionWS, inputData.normalWS);
    half3 lighting = PRTSampleGI(inputData.positionWS, reflectionDir, half3(-1, -1, -1));
    if (lighting.x < 0) return shaded;
    float localLuminance = dot(lighting, float3(0.2126, 0.7152, 0.0722));
    float factor = 0, totalWeight = 0;
    [loop] for (int index = 0; index < _PRTReflectionCount; index++)
    {
        float3 low = inputData.positionWS - _PRTReflectionBoxMin[index].xyz;
        float3 high = _PRTReflectionBoxMax[index].xyz - inputData.positionWS;
        float distance = max(_PRTReflectionBoxMin[index].w, 0.001);
        float weight = saturate(min(min(min(low.x, low.y), low.z), min(min(high.x, high.y), high.z)) / distance);
        weight = min(weight, 1.0 - totalWeight);
        if (weight <= 0) continue;
        float reference = max(PRTReferenceLuminance(index, inputData.normalWS), 1e-4);
        factor += weight * clamp(localLuminance / reference,
            _PRTReflectionParams.x, _PRTReflectionParams.y);
        totalWeight += weight;
    }
    factor = lerp(1.0, factor + (1.0 - totalWeight), _PRTReflectionParams.w);
    BRDFData brdfData;
    InitializeBRDFData(surfaceData, brdfData);
    half NoV = saturate(dot(inputData.normalWS, inputData.viewDirectionWS));
    half fresnel = Pow4(1.0 - NoV);
    half3 envSpecular = GlossyEnvironmentReflection(reflectionDir, inputData.positionWS,
        brdfData.perceptualRoughness, 1.0h, inputData.normalizedScreenSpaceUV);
    half3 reflected = EnvironmentBRDF(brdfData, half3(0, 0, 0), envSpecular, fresnel);
    AmbientOcclusionFactor ao = CreateAmbientOcclusionFactor(inputData, surfaceData);
    return max(shaded + reflected * ((factor - 1.0) * ao.indirectAmbientOcclusion), 0);
}

#endif
