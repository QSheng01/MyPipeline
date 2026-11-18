#ifndef CHARACTERS_HAIR_FORWARD_PASS_HLSL
#define CHARACTERS_HAIR_FORWARD_PASS_HLSL

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/Common.hlsl"
#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/AdditionalInput.hlsl"
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
    float4 uv : TEXCOORD0; //xy:UV，zw:Matcap UV
    float4 positionWS : TEXCOORD1; //w: fogCoord
    float4 TBN1 : TEXCOORD2; //w: viewDir.x
    float4 TBN2 : TEXCOORD3; //w: viewDir.y
    float4 TBN3 : TEXCOORD4; //w: viewDir.z
    float4 shadowCoord : TEXCOORD5;
    half3 vertexSH : TEXCOORD6;
    float4 positionNDC : TEXCOORD7;
};

//=================================================================//
//                          初始化数据                              //
//=================================================================//


inline void InitializeLightVectorData(v2f i, inout TextureData textureData, out LightVectorData lightVectorData)
{
    lightVectorData = (LightVectorData)0;

    //==============向量==============
    lightVectorData.positionWS = i.positionWS.xyz;
    lightVectorData.positionCS = i.positionCS;
    lightVectorData.positionNDC = i.positionNDC;

    //向量准备
    lightVectorData.normalWS = normalize(
        TransformTangentToWorld(textureData.normalTS.xyz, half3x3(i.TBN1.xyz, i.TBN2.xyz, i.TBN3.xyz)));
    lightVectorData.viewDirectionWS = normalize(float3(i.TBN1.w, i.TBN2.w, i.TBN3.w));
    lightVectorData.lightDirWS = _MainLightPosition.xyz;
    // lightVectorData.lightDirWS = _CharacterLightDir.xyz;


    //各向异性高光所需向量
    lightVectorData.normalWS_Source = normalize(i.TBN3.xyz);
    lightVectorData.tangentWS = Orthonormalize(i.TBN1.xyz, lightVectorData.normalWS.xyz);
    lightVectorData.bitangentWS = normalize(i.TBN2.xyz);
}

// #define unity_LinearColorSpaceDielectricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04) // standard dielectric reflectivity coef at incident angle (= 4%)
//
//
// inline half LinearOneMinusReflectivityFromMetallic(half metallic)
// {
//     half oneMinusDielectricSpec = unity_LinearColorSpaceDielectricSpec.a;
//     return oneMinusDielectricSpec - metallic * oneMinusDielectricSpec;
// }

inline void InitializeLighColorData(Light light,float4 positionCS,half3 bakeGI,half fog, half shadowStrength, inout TextureData textureData, LightVectorData lightVectorData,
                                    out LightColorData lighColorData)
{
    lighColorData = (LightColorData)0;

    //基础颜色
    half oneMinusReflectivity = OneMinusReflectivityMetallic(0.0h);
    lighColorData.diffuseAlbedo = textureData.albedo * oneMinusReflectivity;

    lighColorData.grazingTerm = saturate(1.0h + 1.0h - oneMinusReflectivity);

    lighColorData.specularColor = 1.0h;

    lighColorData.bakedGI = bakeGI;
    lighColorData.fogCoord = fog;

    //光照颜色
    lighColorData.lightColor = _MainLightColor.rgb;

    //投影计算
    lighColorData.atten = 1;

    lighColorData.atten =light.shadowAttenuation * light.distanceAttenuation;
    // half4 shadowMask = unity_ProbesOcclusion;
    // lighColorData.atten =  MainLightPCFShadow(i.shadowCoord, lightVectorData.positionWS, shadowMask, _MainLightOcclusionProbes);
    //lighColorData.atten =  MainLightPCFRealtimeShadow(i.shadowCoord, lightVectorData.positionWS);
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


    // lighColorData.specularColor *= _PBRSpecColor.rgb;

    #if defined(_LIGHT_COOKIES)
				real3 cookieColor = SampleMainLightCookie(lightVectorData.positionWS);
				light.color *= cookieColor;
    #endif

    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(GetNormalizedScreenSpaceUV(positionCS),
                                                                   textureData.ao/*surfData.occlusion*/);
    #if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT)
				if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_AMBIENT_OCCLUSION))
				{
					lighColorData.lightColor *= aoFactor.directAmbientOcclusion;
				}
    #endif

    textureData.ao = min(aoFactor.indirectAmbientOcclusion, textureData.ao); //HBAO

    // #if _RECEIVE_SHADOWS_OFF
    // lighColorData.addLightAtten = 1;
    // #endif

    lighColorData.multipleLightBrightness = 10.0h;
    lighColorData.multipleLightRange = 0.5h;
    lighColorData.multipleLightSoftness = 1.0h;
}


inline void CalculateLightVectorData(inout LightVectorData lightVectorData)
{
    //灯光脚本修改光照数据
    // lightColorAddData.indirectSpecularColor = 1;
    half3 diffuseDir = lightVectorData.lightDirWS;
    half3 specularDir = lightVectorData.lightDirWS;

    //添加一个对向的灯光方向增强头发体积感
    half3 normalVS = TransformWorldToViewDir(lightVectorData.normalWS);
    half3 otherLightDir = TransformWorldToViewDir(diffuseDir);
    otherLightDir.x *= -1;

    // //各向同性向量
    //    lightVectorData.isotropyViewDir = normalize(GetIsotropyCameraPos(lightVectorData.positionWS) - lightVectorData.positionWS);
    //    lightVectorData.isotropyHalfDir = normalize(lightVectorData.isotropyViewDir + specularDir);

    float3 halfDir = SafeNormalize(specularDir + lightVectorData.viewDirectionWS);
    lightVectorData.halfDir = halfDir;

    //向量运算
    half NdotL = dot(lightVectorData.normalWS, diffuseDir);

    //辅助光计算
    half otherLight_halfLambert = dot(otherLightDir, normalVS) * 0.5 + 0.5;
    otherLight_halfLambert = lerp(1, otherLight_halfLambert, _UseOtherLight);
    lightVectorData.otherLight_halfLambert = otherLight_halfLambert;

    NdotL *= lightVectorData.otherLight_halfLambert; //混合辅助光的阴影

    lightVectorData.NdotL = NdotL;
    lightVectorData.halfLambert = NdotL * 0.5 + 0.5;

    lightVectorData.LdotH = max(0, dot(specularDir, halfDir));
    lightVectorData.LdotV = dot(specularDir, lightVectorData.viewDirectionWS);

    lightVectorData.NdotH = max(0, dot(lightVectorData.normalWS.xyz, halfDir.xyz));
    lightVectorData.NdotVNoClamp = dot(lightVectorData.normalWS, lightVectorData.viewDirectionWS);
    lightVectorData.NdotV = max(0, lightVectorData.NdotVNoClamp);
}


inline void GetDiffuseColor(TextureData textureData,DiffuseData diffuseData, LightVectorData lightVectorData, LightColorData lighColorData,
                            out half3 diffuseTerm, inout half3 finalColor)
{
    //漫反射计算


    half halfLambertOffset = 0.5 + diffuseData.rampXOffset;
    half oneMinusHalfLambertOffset = 1 - halfLambertOffset;


    //偏移半兰伯特
    half radiance = lightVectorData.NdotL;

    radiance = radiance * halfLambertOffset + oneMinusHalfLambertOffset;
    radiance *= lerp(1.0h, textureData.ao, diffuseData.useDiffuseBlendAO * (1 - lightVectorData.halfLambert)); //叠加AO

    radiance *= lerp(1, lighColorData.atten, diffuseData.diffuseAddShadow); //叠加自投影


    // radiance = lerp(radiance, 0.5 * (radiance + textureData.occlusion), _DiffuseAddAO);


    // half2 rampID = half2(radiance, lerp(_RampYOffset, textureData.rampID + _RampYOffset, _UseRampID));
    half2 rampID = half2(radiance, diffuseData.rampYOffset);
    rampID.y = frac(rampID.y);

    diffuseTerm = GetShadowMap(rampID).rgb;

    finalColor = lighColorData.diffuseAlbedo * diffuseTerm;
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

inline void GetAnisotropySpecularColor(TextureData textureData, LightVectorData lightVectorData,
                                       LightColorData lighColorData, half3 diffuseTerm, inout half3 finalColor)
{
	KajiyaKayData kajiyaKayData;
	InitializeKajiyakayData(kajiyaKayData);
    //头发各向异性高光计算
    // half anisotropySpecularMask = GetChannel(textureData.alpha, textureData.normalTS.b, textureData.lightMap, _SPECMASK);
    // half anisotropySpecular2Mask = GetChannel(textureData.alpha, textureData.normalTS.b, textureData.lightMap, _SPECMASK2, 0);
    half anisotropySpecularMask = textureData.lightMap.r;
    half anisotropyLineMask = textureData.lightMap.b;
	
    half4 detailNormal = GetAnisotropyShapeMap(textureData.uv.xy);


    half3 T = lightVectorData.bitangentWS.xyz;

    float2 jitter = (detailNormal.x - 0.5) * float2(kajiyaKayData.hairSpread1, kajiyaKayData.hairSpread2);

    //各向异性高光1偏移
    float3 t1 = ShiftTangent(T, lightVectorData.normalWS.xyz, _HairSpecularShift + jitter.x);

    //各向异性高光2偏移
    // float anisotropySpecular2Offset = lerp(_HairSecondarySpecularShift_Black, _HairSecondarySpecularShift_White, anisotropySpecular2Mask);//让 陈 刘海偏上，后脑勺偏下
    float anisotropySpecular2Offset = kajiyaKayData.hairSecondarySpecularShift_Black; //让 陈 刘海偏上，后脑勺偏下
    float3 t2 = ShiftTangent(T, lightVectorData.normalWS.xyz, anisotropySpecular2Offset + jitter.y);


    float3 halfDir = lightVectorData.halfDir;
    // #if _USEIOTROPY
    // halfDir = lerp(lightVectorData.isotropyHalfDir, lightVectorData.halfDir, anisotropySpecular2Mask);
    // #endif

    float3 hairSpec1 = kajiyaKayData.hairSpecularStrength * D_KajiyaKay(t1, halfDir, kajiyaKayData.hairSpecularExponent);
    float3 hairSpec2 = kajiyaKayData.hairSecondarySpecularStrength * D_KajiyaKay(t2, halfDir, kajiyaKayData.hairSecondarySpecularStrength);

    float3 F = F_Schlick(half3(0.2, 0.2, 0.2), lightVectorData.LdotH);

    float geomNdotV = dot(lightVectorData.normalWS_Source, lightVectorData.viewDirectionWS.xyz);
    float scatterFresnel1 = pow(saturate(-lightVectorData.LdotV), 9.0) * pow(
        saturate(1.0 - geomNdotV * geomNdotV), 12.0);
    float scatterFresnel2 = saturate(PositivePow((1.0 - geomNdotV), 20.0));

    half transmittance = 0.01;
    half scatterFresnel = (scatterFresnel1 + 0.01 * scatterFresnel2) * transmittance;

    F *= 0.25;

    half3 specularTerm1 = F * hairSpec1;
    specularTerm1 += scatterFresnel;

    half3 specularTerm2 = F * hairSpec2;
    specularTerm2 += scatterFresnel;


    // half3 specularRamp = SAMPLE_TEXTURE2D(_AnisotropyRampMap, sampler_AnisotropyRampMap, half2(saturate(specularTerm1.x), 0.5)).rgb;

    // half3 specularCol1 = specularTerm1 * specularRamp * _HairSpecularCol.rgb;
    half3 specularCol1 = specularTerm1 * kajiyaKayData.hairSpecularCol.rgb;
    half3 specularCol2 = specularTerm2 * kajiyaKayData.hairSecondarySpecularCol.rgb;


    // specularCol2 = lerp(specularCol2 * saturate(specularTerm2 - specularTerm1), specularCol2, anisotropySpecular2Mask);
    specularCol2 = specularCol2 * saturate(specularTerm2 - specularTerm1);

    half3 specularCol = specularCol1 + specularCol2;
    // specularCol *= max(0, lightVectorData.NdotL);
    specularCol *= anisotropySpecularMask;
    specularCol *= textureData.ao;
    specularCol *= lighColorData.atten;
    specularCol *= lighColorData.specularColor;


    // #if _USEIOTROPY
    // specularCol *= lerp(lightVectorData.NdotVNoClamp * lightVectorData.NdotVNoClamp, 1, anisotropySpecular2Mask);
    // #endif


    //高光描线
    half3 anisotropyLineColor = finalColor;

    half3 anisotropyLine =GetAnisotropyLineMap(textureData.uv.xy);
    anisotropyLine = 1 - anisotropyLine;

    //遮罩
    float3 t3 = ShiftTangent(T, lightVectorData.normalWS.xyz, kajiyaKayData.anisotropyLinePos + jitter.x);
    half anisotropyLineWeight = saturate(D_KajiyaKay(t3, halfDir, kajiyaKayData.anisotropyLineRange).x);
    anisotropyLineWeight *= anisotropySpecularMask; //高光遮罩
    anisotropyLineWeight *= anisotropyLineMask; //描边遮罩
    // anisotropyLineWeight *= 1 - anisotropySpecular2Mask;//后脑勺遮罩


    anisotropyLine = LerpWhiteTo(anisotropyLine, kajiyaKayData.anisotropyLineIntensity); //强度

    anisotropyLineColor = lerp(finalColor, finalColor * anisotropyLine, anisotropyLineWeight);


    half3 anisotropySpecularColor = finalColor + specularCol;

    finalColor = lerp(anisotropyLineColor, anisotropySpecularColor, saturate(specularTerm1 + specularTerm2));
    finalColor *= lighColorData.lightColor;
}

inline void GetGIColor(TextureData textureData, LightVectorData lightVectorData, LightColorData lighColorData,
                       inout half3 finalColor)
{
    //GI计算
	GIData gidata;
	InitializeGIData(gidata);
    //漫反射
    half3 indirectDiffuse = lighColorData.diffuseAlbedo * lighColorData.bakedGI;
    indirectDiffuse *= gidata.sHBrightness;

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
    // giColor *= textureData.ao;

	
    half3 aoColor = lerp(gidata.aoColor.rgb, gidata.aoColor.rgb * textureData.albedo, gidata.aoColorBlendAlbedo);
    giColor *= lerp(aoColor, 1.0, textureData.ao);

    finalColor += giColor;
}

inline void GetBacklightRimColor(TextureData textureData,CharactersBlackRimLightData blackRimLightData, LightVectorData lightVectorData, LightColorData lighColorData,
                                 inout half3 finalColor)
{
    //背光边缘光计算

    // //背光遮罩计算
    // float3 N_Shift = lightVectorData.normalWS + lightVectorData.lightDirWS;
    // half backlightValue = saturate(dot(-N_Shift, lightVectorData.viewDirectionWS));
    //
    // half VdotL = dot(lightVectorData.viewDirectionWS, lightVectorData.lightDirWS);
    // VdotL = VdotL * 0.5 + 0.5;
    //
    // //边缘光计算
    // half fresnel = 1 - lightVectorData.NdotV;
    // fresnel = Pow4(fresnel);
    //
    // half backlightRimTerm  = smoothstep(_RimThreshold, _RimThreshold + 0.5, fresnel);
    //
    //
    // //光照方向背光遮罩
    // half backlightMask = LinearStep(_RimLambertMaskThreshold, _RimLambertMaskThreshold + 1, lightVectorData.halfLambert) * backlightValue;
    // backlightMask *= lighColorData.atten;
    //
    //
    // //光照方向背光强度
    // half BacklightDirIntensity = 1 - VdotL;
    // BacklightDirIntensity = LinearStep(0.5, 0.9, BacklightDirIntensity);
    // BacklightDirIntensity *= BacklightDirIntensity;
    // BacklightDirIntensity *= BacklightDirIntensity;
    //
    //
    // half3 backlightColor = _RimColor.rgb * backlightRimTerm;
    // backlightColor *= BacklightDirIntensity;
    // backlightColor *= backlightMask;
    //
    // backlightColor *= _useRim;
    //
    // finalColor += backlightColor;


    //=======================================================================
    //边缘光遮罩
    half3 backlightDir = lightVectorData.lightDirWS;
    half lightDir = _RimDirReverse * 2.0h - 1.0h;
    backlightDir.x *= lightDir;
    backlightDir.z *= lightDir;

    half halfLambert = dot(lightVectorData.normalWS, backlightDir) * 0.5h + 0.5h;
    halfLambert = LinearStep(blackRimLightData.blackRimLambertMaskThreshold, blackRimLightData.blackRimLambertMaskThreshold + 0.0h, halfLambert);
    // halfLambert = LinearStep(0.5h, 1.0h, halfLambert);

    //==光照深度遮罩==
    //让正光面和逆光面的深度朝两个方向偏移
    half depthLightOffsetDir = sign(halfLambert * 2.0h - 1.0h);
    backlightDir.x *= depthLightOffsetDir;
    backlightDir.z *= depthLightOffsetDir;

    float3 reverseLightDepthUV_WS = lightVectorData.positionWS + backlightDir * blackRimLightData.blackRimDepthWidth; //朝光照方向进行偏移
    // float3 reverseLightDepthUV_WS = lightVectorData.positionWS + lightVectorData.normalWS_Source * _RimDepthWidth;//朝法线方向进行偏移
    float4 reverseLightDepthUV_CS = TransformWorldToHClip(reverseLightDepthUV_WS);
    float4 reverseLightDepthUV_NDC = ComputeScreenPos(reverseLightDepthUV_CS);

    float screenDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture,
                                           reverseLightDepthUV_NDC.xy / reverseLightDepthUV_NDC.w).r;
    screenDepth = LinearEyeDepth(screenDepth, _ZBufferParams);

    float reverseLightDepth = reverseLightDepthUV_NDC.z / reverseLightDepthUV_NDC.w;
    reverseLightDepth = LinearEyeDepth(reverseLightDepth, _ZBufferParams);


    float currentPosDepth = lightVectorData.positionNDC.z / lightVectorData.positionNDC.w;
    currentPosDepth = LinearEyeDepth(currentPosDepth, _ZBufferParams);

    float differentDepth = smoothstep(0, 1, screenDepth - reverseLightDepth);
    // float differentDepth = smoothstep(_Test1, _Test1 + _Test2, screenDepth - currentPosDepth);


    half blackRimBrightness = lerp(_RimLambertMaskBrightness, 1, halfLambert);


    //Fresnel范围
    half fresnel = 1 - lightVectorData.NdotV;
    fresnel = Pow4(fresnel);

    half rimThreshold = 1.0h - lerp(_BlackRimThreshold, _RimThreshold, halfLambert);
    half backlightRimTerm = smoothstep(rimThreshold, rimThreshold + 0.5h, fresnel);

    //边缘光颜色
    half3 rimColor = lerp(_BlackRimColor.rgb, _RimColor.rgb, halfLambert);

    half3 backlightColor = lerp(rimColor, rimColor * textureData.albedo, _RimColorBlendAlbedo);
    backlightColor *= backlightRimTerm;
    backlightColor *= blackRimBrightness;
    backlightColor *= differentDepth;
    backlightColor *= _useRim;

    #if _USERIM
				finalColor += backlightColor;
    #endif

    // finalColor = backlightRimTerm;
    // finalColor = differentDepth;
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


inline void GetFogColor(v2f i, LightColorData lighColorData, inout half3 finalColor)
{
 //    //雾效
 //    #ifdef _WORLDFOG
	// WarFogSample(finalColor, i.positionWS.xyz);
 //    #endif

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

    float3 tangentWS = TransformObjectToWorldDir(i.tangentOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(i.normalOS);
    float3 binormalWS = normalize(cross(normalWS, tangentWS)) * sign;

    float3 viewDir = GetWorldSpaceViewDir(positionWS);

    o.TBN1 = float4(tangentWS, viewDir.x);
    o.TBN2 = float4(binormalWS, viewDir.y);
    o.TBN3 = float4(normalWS, viewDir.z);

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

    o.positionNDC = ComputeScreenPos(o.positionCS);

    return o;
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

    CharactersShadowData shadowData;
    InitializeShadowData(shadowData);


    

    LightVectorData lightVectorData;
    InitializeLightVectorData(i, textureData, lightVectorData);

    LightColorData lighColorData;
    InitializeLighColorData(mainLight,i.positionCS, i.vertexSH.rgb,i.positionWS.w,shadowData.shaderIntensity, textureData, lightVectorData, lighColorData);

    CharacterLightControllerData(lightVectorData, lighColorData);
    CalculateLightVectorData(lightVectorData);

    //================最终颜色计算==================
    half3 finalColor = 0;
    half3 diffuseTerm = 0;

	DiffuseData diffuseData;
	InitializeDiffuseData(diffuseData);
    GetDiffuseColor(textureData,diffuseData, lightVectorData, lighColorData, diffuseTerm, finalColor); //漫反射计算
    // GetMultipleLightColor(textureData, lightVectorData, lighColorData, finalColor);//多光源计算
    GetShadowColor(textureData,shadowData, lighColorData, diffuseTerm, finalColor); //投影颜色计算
    GetAnisotropySpecularColor(textureData, lightVectorData, lighColorData, diffuseTerm, finalColor); //各向高光计算
    GetGIColor(textureData, lightVectorData, lighColorData, finalColor); //GI计算
    GetMultipleLightColor(textureData, lightVectorData, lighColorData, finalColor); //多光源计算
	
    //GetBacklightRimColor(textureData, lightVectorData, lighColorData, finalColor); //背光边缘光计算
	
    GetEffectFresnelColor_Scene(textureData, lightVectorData, lighColorData, finalColor); //特效边缘光计算

	CharactersHitRimLightData hitRimLightData;
	InitializeHitRimLightData(hitRimLightData);
	
    GetHitRimColor_Scene(textureData,hitRimLightData, lightVectorData, lighColorData, finalColor); //受击边缘光
    GetFogColor(i, lighColorData, finalColor); //雾效计算

    //特效专用自发光（不使用遮罩）
    finalColor += _WholeEmissionColor.rgb;

    return half4(finalColor, textureData.alpha);
}


#endif
