#ifndef _MAINCHARACTERS_FACE_FORWARDPASS_HLSL_
#define _MAINCHARACTERS_FACE_FORWARDPASS_HLSL_

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/Common.hlsl"
#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/AdditionalInput.hlsl"
#include "../../Scene/ShaderInclude/ShaderOtherFunction.hlsl"
#include "Characters_AdditionalLightingData.hlsl"
#include "Characters_StructData.hlsl"
#include "Characters_Function.hlsl"

struct a2v
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
};

struct v2f
{
    float4 positionCS : SV_POSITION;
    float4 uv : TEXCOORD0; // x,y UV; z:FDotL w:Sign
    float4 positionWS : TEXCOORD1; //w: fogCoord
    float3 normalWS :TEXCOORD2;
    float4 shadowCoord : TEXCOORD3;
    half3 vertexSH : TEXCOORD4;
    float4 positionNDC : TEXCOORD5;
};


//
//
// #define unity_LinearColorSpaceDielectricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04) // standard dielectric reflectivity coef at incident angle (= 4%)
//
// inline half LinearOneMinusReflectivityFromMetallic(half metallic)
// {
//     half oneMinusDielectricSpec = unity_LinearColorSpaceDielectricSpec.a;
//     return oneMinusDielectricSpec - metallic * oneMinusDielectricSpec;
// }


inline void InitializeLightVectorData(float3 positionWS,float3 normalWS, Light light, inout TextureData textureData,
                                      out LightVectorData lightVectorData)
{
    lightVectorData = (LightVectorData)0;

    //==============向量==============
    lightVectorData.positionWS = positionWS.xyz;
    lightVectorData.normalWS = normalize(normalWS);
    lightVectorData.normalWS_Source = normalize(normalWS);

    //向量准备
    lightVectorData.viewDirectionWS = normalize(GetWorldSpaceViewDir(lightVectorData.positionWS));
    lightVectorData.lightDirWS = light.direction;
}

inline void InitializeLighColorData(Light light,half3 bakeGI,half fog, half shadowStrength, TextureData textureData, LightVectorData lightVectorData,
                                    out LightColorData lighColorData)
{
    lighColorData = (LightColorData)0;

    //基础颜色
    //half oneMinusReflectivity = LinearOneMinusReflectivityFromMetallic(0.0h);
    half oneMinusReflectivity = OneMinusReflectivityMetallic(0.0h);
    lighColorData.diffuseAlbedo = textureData.albedo * oneMinusReflectivity;

    lighColorData.grazingTerm = saturate(1.0h + 1.0h - oneMinusReflectivity);

    lighColorData.specularColor = lerp(kDieletricSpec.rgb, textureData.albedo, 0);
    // lighColorData.specularColor *= _PBRSpecColor.rgb;

    lighColorData.bakedGI = bakeGI;

    lighColorData.fogCoord = fog;

    //光照颜色
    // lighColorData.lightColor = _MainLightColor.rgb;
    lighColorData.lightColor = light.color;


    //投影计算
    lighColorData.atten = 1;
    //主光投影
    // #ifndef _RECEIVE_MAIN_SHADOWS_OFF
    lighColorData.atten = light.shadowAttenuation * light.distanceAttenuation;
    // #endif

    //逐对象投影
    // #if _RECEIVE_PEROBJECT_SHADOWS
    half pershadow = SAMPLE_TEXTURE2D_X(_ScreenSpaceShadowmapTexture, sampler_ScreenSpaceShadowmapTexture,
                                        textureData.screenUV).r;
    half4 shadowParams = GetMainLightShadowParams();
    // real shadowStrength = shadowParams.x;
    // pershadow = LerpWhiteTo(pershadow, shadowStrength);
    lighColorData.atten = min(lighColorData.atten, pershadow);
    // #endif
    lighColorData.atten = LerpWhiteTo(lighColorData.atten, shadowStrength);

    lighColorData.multipleLightBrightness = 10.0h;
    lighColorData.multipleLightRange = 0.5h;
    lighColorData.multipleLightSoftness = 1.0h;
}



inline void CalculateLightVectorData(inout LightVectorData lightVectorData)
{
    half3 diffuseDir = lightVectorData.lightDirWS;
    half3 specularDir = lightVectorData.lightDirWS;

    //向量运算
    half NdotL = dot(lightVectorData.normalWS, diffuseDir);

    lightVectorData.NdotL = NdotL;
    lightVectorData.halfLambert = NdotL * 0.5 + 0.5;

    float3 halfDir = SafeNormalize(specularDir + lightVectorData.viewDirectionWS);
    lightVectorData.halfDir = halfDir;

    lightVectorData.NdotH = max(0, dot(lightVectorData.normalWS.xyz, halfDir.xyz));
    lightVectorData.NdotVNoClamp = dot(lightVectorData.normalWS, lightVectorData.viewDirectionWS);
    lightVectorData.NdotV = max(0, lightVectorData.NdotVNoClamp);
}


inline void GetMultipleLightColor(TextureData textureData, LightVectorData lightVectorData,
                                  inout LightColorData lighColorData, inout half3 finalColor)
{
    //多光源计算
    //==================多光源==================
    half3 MultipleLightColor = finalColor; //此时只算了Diffuse

    #if _ADDITIONAL_LIGHTS
            	uint pixelLightCount = GetAdditionalLightsCount();
			    for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
			    {
			    	//==================新多光源计算========================
			    	//灯光
			    	int perObjectLightIndex = GetPerObjectLightIndex(lightIndex);
					Light light = GetAdditionalPerObjectLight(perObjectLightIndex, lightVectorData.positionWS);

			    	//衰减后的灯光颜色
			    	half3 currentLightColor = light.color * light.distanceAttenuation * light.shadowAttenuation;

			    	//阴影
			    	half diffuseTerm = dot(lightVectorData.normalWS, light.direction);
			    	diffuseTerm = diffuseTerm * 0.5h + 0.5h;
			    	
			    	diffuseTerm = LinearStep(lighColorData.multipleLightRange, lighColorData.multipleLightRange + lighColorData.multipleLightSoftness, diffuseTerm);

			    	//最终多光源颜色
			    	half3 multipleLDiffuseColor = currentLightColor * Luminance(lighColorData.diffuseAlbedo);
			    	multipleLDiffuseColor *= diffuseTerm;
			    	MultipleLightColor += multipleLDiffuseColor;
			    }
            	half maxRGBValue = Luminance(MultipleLightColor);
            	maxRGBValue /= lighColorData.multipleLightBrightness;
		        MultipleLightColor = maxRGBValue > 1.0h ? (MultipleLightColor / maxRGBValue) : MultipleLightColor;
    #endif

    finalColor = MultipleLightColor;
}


inline void GetShadowColor(TextureData textureData,CharactersShadowData shadowData, LightColorData lighColorData, half3 diffuseTerm,
                           inout half3 finalColor)
{
    //投影颜色计算
    finalColor = lerp(finalColor * shadowData.shadowColor.rgb, finalColor, lighColorData.atten);
}

inline void GetHeavyShadowWeight(TextureData textureData,HeavyShadowData heavyShadowData, LightVectorData lightVectorData, LightColorData lighColorData,
                                 inout half3 finalColor)
{
    //重阴影颜色计算
    //重阴影
    half heavyShadowWeight = smoothstep(heavyShadowData.heavyRange, heavyShadowData.heavyRange + heavyShadowData.heavySoftness,
                                        lightVectorData.NdotV);

    heavyShadowWeight = lerp(heavyShadowWeight, 1, lighColorData.atten);
    heavyShadowWeight = LerpWhiteTo(heavyShadowWeight, heavyShadowData.heavyShaderIntensity);
    heavyShadowWeight = lerp(1, heavyShadowWeight, textureData.lightMap.b);

    finalColor = lerp(finalColor * heavyShadowData.heavyShadowColor.rgb, finalColor, heavyShadowWeight);
}

inline void GetGIColor(TextureData textureData,half bakeGIIntensity, LightVectorData lightVectorData, LightColorData lighColorData,
                       inout half3 finalColor)
{
    //GI计算

    //漫反射
    half3 indirectDiffuse = lighColorData.diffuseAlbedo * lighColorData.bakedGI;
    indirectDiffuse *= bakeGIIntensity;

    //高光
    half3 indirectSpecular = 0;

    // #if defined(_GLOSSYREFLECTIONS_ON)
    //     half3 reflectVector = reflect(-lightVectorData.viewDirectionWS, lightVectorData.normalWS);
    //     half fresnelTerm = Pow4(1.0 - lightVectorData.NdotV);
    //
    //     half mip = PerceptualRoughnessToMipmapLevel(1 - textureData.smoothness);
    //
    //     #ifdef _CUSTOM_ENV_CUBE  
    //         half4 encodedIrradiance = SAMPLE_TEXTURECUBE_LOD(_EvnCubemap, sampler_EvnCubemap, reflectVector, mip);
    //     #else
    //         half4 encodedIrradiance = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector, mip);
    //     #endif
    //
    //     #if !defined(UNITY_USE_NATIVE_HDR)
    //         indirectSpecular = DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
    //     #else
    //         indirectSpecular = encodedIrradiance.rgb;
    //     #endif
    //
    //     #if UNITY_COLORSPACE_GAMMA
    //         indirectSpecular = FastSRGBToLinear(indirectSpecular);
    //     #endif
    //
    //     float surfaceReduction = 1.0 / (textureData.roughness * textureData.roughness + 1.0);
    //
    //     indirectSpecular *= _CubemapColor.rgb;
    //     indirectSpecular *= _EnvExposure;
    //     indirectSpecular *= surfaceReduction;
    //     indirectSpecular *= lerp(lighColorData.specularColor, lighColorData.grazingTerm, fresnelTerm);
    //     indirectSpecular *= lightColorAddData.indirectSpecularColor;
    // #endif

    //最终颜色
    half3 giColor = indirectDiffuse + indirectSpecular;
    // giColor *= textureData.occlusion;

    finalColor += giColor;
}

inline void GetEmissionColor(TextureData textureData, LightVectorData lightVectorData, LightColorData lighColorData,
                             inout half3 finalColor)
{
    //自发光计算
    finalColor += textureData.emission;
}

inline void GetBacklightRimColor(TextureData textureData,CharactersRimLightData rimLigthData, LightVectorData lightVectorData, LightColorData lighColorData,
                                 inout half3 finalColor)
{
    //背光边缘光计算
    //边缘光计算
    half fresnel = 1 - lightVectorData.NdotV;
    fresnel = Pow4(fresnel);

    half backlightRimTerm = smoothstep(rimLigthData.rimThreshold, rimLigthData.rimThreshold + rimLigthData.rimSoftness, fresnel);

    //边缘光遮罩
    half3 backlightDir = lightVectorData.lightDirWS;
    backlightDir.x *= -1.0h;
    backlightDir.z *= -1.0h;

    half halfLambert = dot(lightVectorData.normalWS, backlightDir) * 0.5 + 0.5;
    halfLambert = LinearStep(rimLigthData.rimMask, 1, halfLambert);

    half3 backlightColor = lerp(rimLigthData.rimColor.rgb, rimLigthData.rimColor.rgb * textureData.albedo, rimLigthData.rimColorWeight);
    backlightColor *= backlightRimTerm;
    backlightColor *= halfLambert;
    backlightColor *= rimLigthData.rimIntensity;

    finalColor += backlightColor;
}

inline void GetEffectFresnelColor_Scene(TextureData textureData, LightVectorData lightVectorData,
                                        LightColorData lighColorData, inout half3 finalColor)
{
    //特效边缘光计算
    #if _EFFECTFRESNEL_ON
		EffectFresnelData effectFresnelData;
		InitializeEffectFresnelData(textureData.uv,effectFresnelData);
	    half3 effectFresnelColor = 0;
	    
	    half effectFresnelMask = effectFresnelData.effectFresnelMask;
	    half effectFresnel = pow(1 - lightVectorData.NdotV, effectFresnelData.effectFresnelPower);
	    half effectFresnelvalue = smoothstep(effectFresnelData.effectFresnelThreshold, effectFresnelData.effectFresnelThreshold + effectFresnelData.effectFresnelSmooth, effectFresnel);
	    effectFresnelColor = effectFresnelData.effectFresnelColor.rgb * effectFresnelvalue * effectFresnelData.effectFresnelFactor;
	    effectFresnelColor *= effectFresnelMask;
	    finalColor += effectFresnelColor;
    #endif
}

inline void GetSDFThread_20240221(Light mainLight,SDFData sdfData, out half2 result) //, out float acosFdotL)
{
    //===================SDF曲线优化===================
    float2 frontDir_XZ = normalize(TransformObjectToWorldDir(_FaceForward, false).xz);
    float2 rightDir_XZ = normalize(TransformObjectToWorldDir(_FaceRight, false).xz);
    float2 lightDir_XZ = normalize(mainLight.direction.xz);

    //half reversal = _Reversal * 2 - 1;
    half reversal = -1.0f;

    float RdotL = dot(rightDir_XZ, lightDir_XZ) * reversal; //模型水平右向方向与灯光的夹角值，1为灯光在模型右边，-1为灯光在模型左边
    float FdotL = dot(frontDir_XZ, lightDir_XZ);

    float angle = acos(FdotL);
    //acosFdotL = angle;//用于自投影深度偏移

    angle *= 0.31831f; // angle *= 57.29578f;  // angle /= 180;


    float x = saturate(angle);

    //_SpecialSdfAcceleration:自定义正面阴影范围加速度（5）
    half specialSdfAcceleration_Front = sdfData.SpecialSdfAcceleration_Front * 9 + 1;

    float origin = 0.584803547643f / specialSdfAcceleration_Front; //0.584803547643f：0.2f的立方根。用于保证 x:0 = y:0
    // float shadowPos = lerp(0.9f, 0.5f, _SpecialSdfBlackRange);//背光面阴影范围(0.5f)
    float shadowPos = 0.5f; //背光面阴影范围(0.5f)

    float leftMask = min(0.0f, x - origin) * specialSdfAcceleration_Front; //正面阴影范围加速变大区

    float blackAcceleration = 0.908560296416f / (lerp(1.0f, shadowPos, sdfData.SpecialSdfAcceleration_Black * 0.9f) -
        shadowPos); //0.908560296416f：(1-0.25f)的立方根。用于保证 x:1 = y≥1
    float rightMask = max(0.0f, x - shadowPos) * blackAcceleration; //背面阴影范围加速变大区

    float transitionMask = x / shadowPos; //阴影缓慢过渡区

    float leftCurve = leftMask * leftMask * leftMask + sdfData.SpecialSdfRange; // + 0.2f;//正面阴影最大范围
    float rightCurve = rightMask * rightMask * rightMask + sdfData.SpecialSdfRange + 0.05; // + 0.25f;//背面阴影最小范围

    float finalSdfValue = lerp(leftCurve, rightCurve, transitionMask); //最终曲线
    finalSdfValue = lerp(angle, finalSdfValue, min(1.0f, sdfData.UseSpecialSDF * specialSdfAcceleration_Front)); //开关

    result.x = finalSdfValue;


    half sign = 2 * step(RdotL, 0.0f) - 1.0f; // 计算灯光在左边还是右边,大于0是右手灯光，小于0是左右灯光

    result.y = sign;
}


inline void GetFogColor(v2f i, LightColorData lighColorData, inout half3 finalColor)
{
    //雾效
    #ifdef _WORLDFOG
	WarFogSample(finalColor, i.positionWS.xyz);
    #endif
    finalColor.rgb = MixFog(finalColor, lighColorData.fogCoord);
}

//=============================END=================================//


v2f vert(a2v i)
{
    v2f o;

    float3 positionWS = TransformObjectToWorld(i.positionOS.xyz).xyz;
    o.positionWS.xyz = positionWS;
    o.positionCS = TransformWorldToHClip(positionWS);

    //TBN矩阵
    float sign = i.tangentOS.w;

    // float3 tangentWS   = TransformObjectToWorldDir(i.tangentOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(i.normalOS);
    // float3 binormalWS  = normalize(cross(normalWS, tangentWS)) * sign;


    o.normalWS = normalWS;

    o.uv.xy = i.uv;
    // o.uv.zw = i.uv * _AnisotropyMap_ST.xy + _AnisotropyMap_ST.zw;

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
			    o.shadowCoord = TransformWorldToShadowCoord(positionWS.xyz);
    #else
    o.shadowCoord = 0;
    #endif

    //环境光
    o.vertexSH.xyz = SampleSH(normalWS.xyz).xyz;

    o.positionWS.w = ComputeFogFactor(o.positionCS.z);

    Light mainLight = GetCharacterMainLight();

	SDFData sdfData;
	InitializeSDFData(sdfData);
	
    GetSDFThread_20240221(mainLight,sdfData, o.uv.zw);

    o.positionNDC = ComputeScreenPos(o.positionCS);

    return o;
}

inline void GetDiffuseColor(TextureData textureData,SDFData sdfDafa, LightVectorData lightVectorData, LightColorData lighColorData,
							inout half3 finalColor)
{
	//漫反射计算
	half FdotL = textureData.uv.z;
	half sign = textureData.uv.w;

	half4 lightMap = GetLightMap(textureData.uv,sign);

	half diffuseTerm = smoothstep(FdotL - sdfDafa.rampSize, FdotL, lightMap.r);
	diffuseTerm *= lighColorData.atten;


	half radiance = lerp(lightVectorData.NdotL, lightVectorData.halfLambert, sdfDafa.usehalflambert);

	float2 rampUV = float2(radiance, sdfDafa.rampUV_Y);

	half3 rampColor =GetRampMap(rampUV).rgb; 

	rampColor = lighColorData.diffuseAlbedo.rgb * rampColor * lighColorData.lightColor.rgb;
	rampColor *= lighColorData.atten;

	half3 sdfColor = lerp(sdfDafa.darkColor.rgb, sdfDafa.brightColor.rgb, diffuseTerm);

	//让眼睛阴影不红
	half3 eyeDiffuse = clamp(lightVectorData.halfLambert, 0.5, 1);
	eyeDiffuse *= lighColorData.atten;

	sdfColor = lerp(sdfColor, eyeDiffuse, textureData.lightMap.a);

	sdfColor *= lighColorData.lightColor.rgb * textureData.albedo.rgb;

	//脖子阴影，处理接缝问题
	half3 diffuseColor = lerp(sdfColor, rampColor, textureData.lightMap.b);

	finalColor = diffuseColor;
}


//=================================================================//
//                          片元着色器                              //
//=================================================================//

half4 frag(v2f i) : SV_Target
{
	
    DitherThreshold(i.positionNDC, i.positionCS.xy, GetDitherStrength());

	//half4 shadowMask = unity_ProbesOcclusion;
	half4 shadowMask =half4(1,1,1,1);
	// // hbao 修改 这里对 SSAO 做特殊处理，直接后面直接乘以 AO 来达到风格化的效果
	AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(i.positionNDC, 1);

	uint meshRenderingLayers = GetMeshRenderingLayer();
	
	Light mainLight = GetCharacterMainLight(i.shadowCoord, i.positionWS, shadowMask,aoFactor);
	
    //================基础数据计算==================
    TextureData textureData;
    InitializeTextureData(i.uv,i.positionNDC, textureData);
	SDFData sdfData;
	InitializeSDFData(sdfData);
	
	CharactersShadowData shadowData;
	InitializeShadowData(shadowData);

	HeavyShadowData heavyShadowData;
    InitializeHeavyShadowData(heavyShadowData);
	
    LightVectorData lightVectorData;
    InitializeLightVectorData(i.positionWS,i.normalWS,mainLight, textureData, lightVectorData);

    LightColorData lighColorData;

    InitializeLighColorData(mainLight, i.vertexSH.rgb,i.positionWS.w,shadowData.shaderIntensity, textureData, lightVectorData, lighColorData);

    CharacterLightControllerData(lightVectorData, lighColorData);
    CalculateLightVectorData(lightVectorData);


	half bakeGIIntensity = GetSHBrightness();
    //================最终颜色计算==================
    half3 finalColor = 0;
    half3 diffuseTerm = 0;


    GetDiffuseColor(textureData,sdfData, lightVectorData, lighColorData, finalColor); //漫反射计算
    // GetMultipleLightColor(textureData, lightVectorData, lighColorData, finalColor);//多光源计算
    GetShadowColor(textureData,shadowData, lighColorData, diffuseTerm, finalColor); //投影颜色计算
    GetGIColor(textureData,bakeGIIntensity, lightVectorData, lighColorData, finalColor); //GI计算
    GetHeavyShadowWeight(textureData,heavyShadowData, lightVectorData, lighColorData, finalColor); //重阴影计算
    GetMultipleLightColor(textureData, lightVectorData, lighColorData, finalColor); //多光源计算

	CharactersRimLightData rimLightData;
	InitializeRimLightData(rimLightData);
    GetBacklightRimColor(textureData,rimLightData, lightVectorData, lighColorData, finalColor); //背光边缘光计算
	
    GetEmissionColor(textureData, lightVectorData, lighColorData, finalColor); //自发光计算
    GetEffectFresnelColor_Scene(textureData, lightVectorData, lighColorData, finalColor); //特效边缘光计算

	CharactersHitRimLightData hitRimLightData;
    InitializeHitRimLightData(hitRimLightData);
	
    GetHitRimColor_Scene(textureData,hitRimLightData, lightVectorData, lighColorData, finalColor); //受击边缘光
    GetFogColor(i, lighColorData, finalColor); //雾效计算

    return half4(finalColor, textureData.alpha);
}
#endif
