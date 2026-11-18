#ifndef CLOUDSHADOW_INCLUDE
#define CLOUDSHADOW_INCLUDE
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

TEXTURE2D(_CloudNoiseMap);       SAMPLER(sampler_CloudNoiseMap);

CBUFFER_START(GlobalCloud)
// float2   _CloudMoveUV;
half4    _CloudShadowColor;
half4    _CloudData;
half _CloudMapThreshold;
half _CloudShadowLOD;
half _GlobalUVScale;
half _GlobalRotation;
float _FadeDistanceNear;
float _FadeDistanceFar;
half2    _CloudMoveUV;
half     _CloudShadowSize;
half     _CloudMapDepth;
CBUFFER_END

/*struct CustomShadow
{
    half4 shadowColor;
    half shadowThreshold;
    half shadowSmooth;
    half shadowInstensity;
};*/

// void InitializeCustonShadow(inout CustomShadow shadow)
// {
//     shadow.shadowColor = _ShadowColor;
//     shadow.shadowThreshold = _ShadowThreshold;
//     shadow.shadowSmooth = _ShadowSmooth;
//     shadow.shadowInstensity = _ShadowInstensity;
// }

inline void GlobalCloudShadow(float3 positionWS, half4 cloudData,inout half mainLightShadow)
{
    //cloudData:XY-运动方向、Z-云影缩放、W-强度
    float2 UV = positionWS.xz / cloudData.z + _Time.x * cloudData.xy;
    half CloudShadow = SAMPLE_TEXTURE2D(_CloudNoiseMap,sampler_CloudNoiseMap, UV).r;
    mainLightShadow =  mainLightShadow * saturate(cloudData.w * CloudShadow);
}


/*inline half GlobalCloudShadow(float3 positionWS,half mainLightShadow)
{
    //cloudData:XY-运动方向、Z-云影缩放、W-强度
    float2 UV = positionWS.xz / _CloudData.z + _Time.x * _CloudData.xy;
    half CloudShadow = SAMPLE_TEXTURE2D(_CloudNoiseMap,sampler_CloudNoiseMap, UV).r;
    CloudShadow =  mainLightShadow * saturate(lerp(0.6, CloudShadow,_CloudData.w));
    return CloudShadow;
}*/

// inline void GlobalCloudShadow(float3 positionWS,inout half CloudBlendShadow)
// {
//     float2 UV = positionWS.xz / _CloudData.z + _Time.x * _CloudData.xy;
//     float2 ChangeUV = positionWS.xz / _CloudChangeData.z + _Time.x * _CloudChangeData.xy;
//     half CloudShadow = SAMPLE_TEXTURE2D_LOD(_CloudNoiseMap,sampler_CloudNoiseMap, UV, _CloudShadowLOD).r ;
//     
//     //half3 color = lerp(0,(smoothstep(0,_CloudMapThreshold,CloudShadow.r )) * (1 - _CloudShadowColor), _CloudShadowColor.a * _CloudShadowStrength);
//     CloudBlendShadow = min(CloudBlendShadow,LerpWhiteTo((1-saturate( smoothstep(_CloudMapThreshold,1,CloudShadow))), _CloudData.w));
//    // CloudBlendShadow *=  (1-saturate( smoothstep(_CloudMapThreshold,1,CloudShadow)  * _CloudData.w));
// }

void Custom_Rotate_float(float2 uv, float2 center, float Rotation, out float2 OutRotate)
{
    Rotation *= 3.1415926f/180.0f;
    uv -= center;
    float s = sin(Rotation);
    float c = cos(Rotation);
    float2x2 rMatrix = float2x2(c, -s, s, c);
    rMatrix *= 0.5;
    rMatrix += 0.5;
    rMatrix = rMatrix * 2 - 1;
    uv.xy = mul(uv.xy, rMatrix);
    uv += center;
    OutRotate = uv;
}

float Calculate_Fode(float3 positionWS)
{
    float3 posOS = TransformWorldToObject(positionWS);
    float3 posView = mul(UNITY_MATRIX_MV, float4(posOS, 1)).xyz;
    float distance = length(posView);
    return 1 - saturate((distance - _FadeDistanceNear) / (_FadeDistanceFar - _FadeDistanceNear));
}

inline void GlobalCloudShadow(float3 positionWS, inout half CloudBlendShadow)
{
    float fade = Calculate_Fode(positionWS);
    float2 PrePos = positionWS.xz * _GlobalUVScale;
    float2 PreRot;
    Custom_Rotate_float(PrePos, float2(0.5, 0.5), _GlobalRotation, PreRot);
    float2 uv01 = PreRot + _Time.x  * _CloudData.xy;
    float CloudShadow = SAMPLE_TEXTURE2D_LOD(_CloudNoiseMap, sampler_CloudNoiseMap, uv01, _CloudShadowLOD).r;
    CloudShadow *= fade;
    CloudBlendShadow = min(CloudBlendShadow,LerpWhiteTo((1-saturate( smoothstep(_CloudMapThreshold,1,CloudShadow))), _CloudData.w));
}

inline void GlobalCloudShadow(float3 positionWS, float fade, inout half CloudBlendShadow)
{
    float3 posOS = TransformWorldToObject(positionWS);
    float3 posView = mul(UNITY_MATRIX_MV, posOS).xyz;
    float dis = length(posView);
    float2 PrePos = positionWS.xz * _GlobalUVScale;
    float2 PreRot;
    Custom_Rotate_float(PrePos, float2(0.5, 0.5), _GlobalRotation, PreRot);
    float2 uv01 = PreRot + _Time.x  * _CloudData.xy;
    float CloudShadow = SAMPLE_TEXTURE2D_LOD(_CloudNoiseMap, sampler_CloudNoiseMap, uv01, _CloudShadowLOD).r;
    CloudShadow *= fade;
    CloudBlendShadow = min(CloudBlendShadow,LerpWhiteTo((1-saturate( smoothstep(_CloudMapThreshold,1,CloudShadow))), _CloudData.w));
}

// 简易世界坐标云影
inline void GlobalSimpleCloudShadow(inout half shadowAttenuation, half3 positionWS, half weight)
{
    // 采样 shadow
    half4 CloudShadow = SAMPLE_TEXTURE2D_LOD(_CloudNoiseMap, sampler_CloudNoiseMap, positionWS.xz / _CloudShadowSize + _Time.y * _CloudMoveUV, 0);

    // 整体偏移移
    half offsetCloudShadow = saturate(CloudShadow.r + _CloudMapDepth);

    // 草的顶部底部波动
    half threshold = lerp(_CloudMapThreshold * 0.6h, _CloudMapThreshold, weight);
    
    // 参考吉卜力风格，只需要变动 R G通道，R通道变动多，G通道变动少
    half thresholdG = lerp(1.0h, offsetCloudShadow, threshold);
    shadowAttenuation *= thresholdG;

    shadowAttenuation = saturate(shadowAttenuation);
}

#endif