#ifndef UNIVERSAL_FORWARD_LIT_TREE_PASS_INCLUDED
#define UNIVERSAL_FORWARD_LIT_TREE_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

// GLES2 has limited amount of interpolators
#if defined(_PARALLAXMAP) && !defined(SHADER_API_GLES)
#define REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR
#endif

#if (defined(_NORMALMAP) || (defined(_PARALLAXMAP) && !defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR))) || defined(_DETAIL)
#define REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR
#endif

// keep this file in sync with LitGBufferPass.hlsl

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 color        : COLOR;
    float4 tangentOS    : TANGENT;
    float2 texcoord     : TEXCOORD0;
    float2 staticLightmapUV   : TEXCOORD1;
    float2 dynamicLightmapUV  : TEXCOORD2;
    float2 texcoord3    : TEXCOORD3;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv                       : TEXCOORD0;

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    float3 positionWS               : TEXCOORD1;
#endif

    float3 normalWS                 : TEXCOORD2;
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    half4 tangentWS                : TEXCOORD3;    // xyz: tangent, w: sign
#endif

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    half4 fogFactorAndVertexLight   : TEXCOORD5; // x: fogFactor, yzw: vertex light
#else
    half  fogFactor                 : TEXCOORD5;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord              : TEXCOORD6;
#endif

#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirTS                : TEXCOORD7;
#endif

    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 8);
#ifdef DYNAMICLIGHTMAP_ON
    float2  dynamicLightmapUV : TEXCOORD9; // Dynamic lightmap UVs
#endif

    float4 positionCS               : SV_POSITION;
    float4 screenPos                : VAR_SCREENPOS;

    half4 Mask1                     : TEXCOORD9;
    half4 sphereSH_halfLamb              : TEXCOORD10;
    half4 sphere_NormalWS           : TEXCOORD11;
    half4 color           : TEXCOORD12;
    half4 vertexColor           : TEXCOORD13;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(Varyings input, out InputData inputData)
{
    inputData = (InputData)0;

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    inputData.positionWS = input.positionWS;
#endif

    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

    inputData.normalWS = input.normalWS;


    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = viewDirWS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif
#ifdef _ADDITIONAL_LIGHTS_VERTEX
    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
#else
    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
#endif

#if defined(DYNAMICLIGHTMAP_ON)
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, inputData.normalWS);
#else
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
#endif

    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

    #if defined(DEBUG_DISPLAY)
    #if defined(DYNAMICLIGHTMAP_ON)
    inputData.dynamicLightmapUV = input.dynamicLightmapUV;
    #endif
    #if defined(LIGHTMAP_ON)
    inputData.staticLightmapUV = input.staticLightmapUV;
    #else
    inputData.vertexSH = input.vertexSH;
    #endif
    #endif

    inputData.positionCS = input.positionCS;
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Used in Standard (Physically Based) shader
half3 ComputeRingLight(float2 screenUV, half ringOffset, half3 ringColor, half3 worldNormal,
    half lightAttenuation, TEXTURE2D_PARAM(depthTexture, depthSampler))
{
    float depth = LinearEyeDepth(SAMPLE_TEXTURE2D(depthTexture, depthSampler, screenUV).r, _ZBufferParams);
    float offset = 0.01 * ringOffset;
    float up = LinearEyeDepth(SAMPLE_TEXTURE2D(depthTexture, depthSampler, screenUV + float2(0, offset)).r, _ZBufferParams);
    float left = LinearEyeDepth(SAMPLE_TEXTURE2D(depthTexture, depthSampler, screenUV + float2(-offset, 0)).r, _ZBufferParams);
    float right = LinearEyeDepth(SAMPLE_TEXTURE2D(depthTexture, depthSampler, screenUV + float2(offset, 0)).r, _ZBufferParams);
    half edge = saturate((max(max(up, left), right) - depth - 20.0) * 0.01);
    half facing = saturate(dot(normalize(worldNormal), GetMainLight().direction) * 0.5h + 0.5h);
    return edge * facing * facing * ringColor * lightAttenuation;
}

Varyings LitPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 pivot = float3(UNITY_MATRIX_M[0][3], UNITY_MATRIX_M[1][3], UNITY_MATRIX_M[2][3]);
    input.positionOS.xyz = LeafWind(input.positionOS.xyz, pivot, TEXTURE2D_ARGS(_NoiseWind, sampler_NoiseWind), _Time.y, _WindParam);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

    // normalWS and tangentWS already normalize.
    // this is required to avoid skewing the direction during interpolation
    // also required for per-vertex lighting and SH evaluation
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);

    half fogFactor = 0;
    #if !defined(_FOG_FRAGMENT)
        fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
    #endif

    output.uv = input.texcoord;

    // already normalized from normal transform to WS.
    output.normalWS = normalInput.normalWS;
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR) || defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    real sign = input.tangentOS.w * GetOddNegativeScale();
    half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
#endif
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    output.tangentWS = tangentWS;
#endif

#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
    half3 viewDirTS = GetViewDirectionTangentSpace(tangentWS, output.normalWS, viewDirWS);
    output.viewDirTS = viewDirTS;
#endif

    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
#ifdef DYNAMICLIGHTMAP_ON
    output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif
    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);
#ifdef _ADDITIONAL_LIGHTS_VERTEX
    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
#else
    output.fogFactor = fogFactor;
#endif

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    output.positionWS = vertexInput.positionWS;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif

    output.positionCS = vertexInput.positionCS;

    half3 lightDir = GetMainLight().direction;
    half3 lightColor = GetMainLight().color;
    half3 sphereNormal_OS = half3(input.color.x, input.color.y, input.color.z) * 2 - 1;
    sphereNormal_OS.x *= -1;
    half3 sphereNormalWS = TransformObjectToWorldDir(sphereNormal_OS);
    output.sphere_NormalWS.xyz = sphereNormalWS;
    half halfLamb = dot(lightDir, sphereNormalWS.xyz) * 0.5 + 0.5;
    half halfLamb_Up = dot(half3(0, 1, 0), sphereNormalWS.xyz) * 0.5 + 0.5;
    output.sphereSH_halfLamb.xyz = SampleSH(sphereNormalWS.xyz); // 顶点采样SH给片源用
    output.sphereSH_halfLamb.w = lerp(halfLamb_Up, halfLamb, saturate(length(lightColor))); // 如果灯光亮度降低，这个效果也会逐渐变成顶光

    output.Mask1 = half4(input.staticLightmapUV.y, 0, 0, 0); // 顶光遮挡
    output.screenPos = ComputeScreenPos(output.positionCS);
    
    output.Mask1.y = 1; // 未使用
    output.Mask1.z = input.staticLightmapUV.x; // 上下过度
    output.Mask1.w = input.dynamicLightmapUV.x; // 区域性点缀色

    output.color.rgb = MixColor(output.Mask1, input.texcoord.xy);
    output.color.a = lerp(_AOIntensity, 1, input.color.a);

    output.vertexColor = input.color;
    
    return output;
}

// Used in Standard (Physically Based) shader
void LitPassFragment(
    Varyings input
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out float4 outRenderingLayers : SV_Target1
#endif
)
{
    outColor = 0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    // rongqingmei测试AO
    // outColor = half4(input.Mask1.yyy, 1.0f);
    // return;
    
#if defined(_PARALLAXMAP)
#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirTS = input.viewDirTS;
#else
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    half3 viewDirTS = GetViewDirectionTangentSpace(input.tangentWS, input.normalWS, viewDirWS);
#endif
    ApplyPerPixelDisplacement(viewDirTS, input.uv);
#endif

    InputData inputData;
    InitializeInputData(input, inputData);
    float2 screenUV = input.screenPos.xy / input.screenPos.w;

    TreeSurfaceData(inputData); // 这里很多计算可以移到顶点去
    _BaseColor.rgb = input.color.rgb; // 顶点计算了很多颜色，这里全部混到BaseColor里

    if(_SMP_SMI.w)
    {
        SideHide(_Cutoff, screenUV, inputData.positionWS, inputData.viewDirectionWS);
    }


    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, surfaceData);

    half bakeSkyAO = 1;
    half3 GIColor = inputData.bakedGI;
    half GIAO = 1;
    // 树木暂时不用GI
    // GIColor = SampleProbeVolume(half4(inputData.normalWS,1),inputData.positionWS, GIAO, GIColor,bakeSkyAO);
    // surfaceData.occlusion *= GIAO;
    surfaceData.occlusion *= input.Mask1.y * input.color.a;


    half4 shadowMask = CalculateShadowMask(inputData);
    // // hbao 修改 这里对 SSAO 做特殊处理，直接后面直接乘以 AO 来达到风格化的效果
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);

    #if defined(_CUSTOM_PCF_SHADOW)
        Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
    #else
        Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
    #endif
    surfaceData.emission *= mainLight.shadowAttenuation;

    // 测试AO


    // sss and ringLight Mask
    half3 sssNormal = input.normalWS;
    half sssMask = FastSSS(inputData.viewDirectionWS, mainLight.direction, sssNormal, _SSSOffset, _SSSPow, mainLight.shadowAttenuation);

    half3 albedoColorWhithSH = lerp(surfaceData.albedo.xyz * input.sphereSH_halfLamb.xyz, input.sphereSH_halfLamb.xyz, 0.1);
    surfaceData.albedo = lerp(albedoColorWhithSH, surfaceData.albedo, saturate(input.sphereSH_halfLamb.w));

    surfaceData.emission *= mainLight.shadowAttenuation;
    half3 ringLight = ComputeRingLight(screenUV, _RingOffset, _RingColor, input.normalWS, mainLight.shadowAttenuation,
        TEXTURE2D_ARGS(_CameraDepthTexture, sampler_CameraDepthTexture));
    surfaceData.emission += ringLight;

    half3 sss = sssMask * surfaceData.albedo * _SSSColor;
    surfaceData.emission += sss;

    surfaceData.emission *= saturate(length(mainLight.color));

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif


    SETUP_DEBUG_TEXTURE_DATA(inputData, input.uv, _BaseMap);

#ifdef _DBUFFER
    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
#endif



    // return;
    half4 color = UniversalFragmentPBR(inputData, surfaceData);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.a = OutputAlpha(color.a, IsSurfaceTypeTransparent(_Surface));

    outColor = half4(surfaceData.occlusion.xxx, 1);

    // outColor = half4(sss.xyz, 1);
    outColor = lerp(color, half4(GIColor, 1), debugGI2);

#ifdef _WRITE_RENDERING_LAYERS
    uint renderingLayers = GetMeshRenderingLayer();
    outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
#endif
}

#endif
