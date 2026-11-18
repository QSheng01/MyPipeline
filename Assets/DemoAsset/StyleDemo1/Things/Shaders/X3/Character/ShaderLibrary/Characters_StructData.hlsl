#ifndef __CHARACTER_STRUCTDATA_HLSL__
#define __CHARACTER_STRUCTDATA_HLSL__

//=================================================================//
//                          特殊结构体                              //
//=================================================================//
struct TextureData
{
    float4 uv;
    float2 screenUV;

    //原贴图
    half4 lightMap;

    //颜色
    half3 albedo;
    half alpha;

    half3 normalTS;
    

    half3 emission;
    half rampID;

    float metallic;
    float smoothness;
    half ao;
    
};

struct CharactersShadowData
{
    half shaderIntensity;
    half3 shadowColor;
};

struct CharactersRimLightData
{
    half rimIntensity;
    half rimMask;
    half rimThreshold;
    half rimSoftness;
    half rimColorWeight;
    half3 rimColor;
};
//可以不要？
struct CharactersBlackRimLightData
{
    half blackRimIntensity;
    half blackRimLambertMaskThreshold;
    half blackRimDepthWidth;
    half blackRimThreshold;
    half blackRimBlockEnd;
    half3 blackRimColor;
};

struct CharactersHitRimLightData
{
    half hitRimPower;
    half hitRimFactor;
    half hitRimThreshold;
    half hitRimSmooth;
    half3 hitRimColor;
};


struct LightVectorData
{
    //坐标
    float3 positionWS;
    float4 positionCS;
    float4 positionNDC;
    //向量
    half3 normalWS;
    half3 tangentWS;
    half3 bitangentWS;
    half3 normalWS_Source;
    half3 lightDirWS;
    half3 viewDirectionWS;
    half3 halfDir;

    

    half NdotL;
    half halfLambert;
    half NdotH;
    half NdotVNoClamp;
    half NdotV;
    half LdotH;
    half LdotV;
    half otherLight_halfLambert;
    
};

struct LightColorData
{
    //基础颜色
    half3 bakedGI;
    float fogCoord;

    //光照颜色
    half3 lightColor;
    half atten;

    half3 diffuseAlbedo;
    half grazingTerm;
    half3 specularColor;

    //多光源设置
    half multipleLightBrightness;
    half multipleLightRange;
    half multipleLightSoftness;
};

//=============================END=================================//

#endif
