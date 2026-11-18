Shader "ZhongMoDi/RoleHairLit"
{
    Properties
    {
        [Space(10)]
        [Enum(OFF, 0, FRONT, 1, BACK, 2)] _MainCullMode ("Main Cull Mode", int) = 2
        [Toggle(_ALPHATEST_ON)]_AlphaClip ("Alpha Clip", Float) = 0
        _Cutoff ("Cutoff", Range(0, 1)) = 1
        
        [Space(10)]
        [Header(IsSelfLight)]
        [Toggle(_)] _IsNeedSelfLight ("是否需要调试白光", Float) = 0
        _SelfLightIntensity ("SelfLightIntensity", Float) = 3
        
        [Space(10)]
        [Header(MainTextures)]
        _AlbedoTex ("AlbedoTex", 2D) = "white" { }
        _AlphaTex ("AlphaTex", 2D) = "white" { }
        _BaseColor ("BaseColor", Color) = (1, 1, 1, 1)
        _BaseColorPow ("BaseColorPow", Range(1, 2)) = 1
        _NormalTex ("NormalTex", 2D) = "white" { }
        [Toggle(_)] _IsNeedOrmTex ("是否需要材质贴图", Float) = 1
        _OrmTex ("OrmTex", 2D) = "white" { }
        _RampTex ("RampTex", 2D) = "white" { }
        _RampColorNoLStrength ("RampColorNoLStrength", Range(0, 1)) = 1
        
        [Space(10)]
        [Header(DiffuseColorLightAndDark)]
        _AlbedoDarkStrength ("AlbedoDarkStrength", Range(0.01, 1)) = 0.8
        _AlbedoDarkSaturation ("AlbedoDarkSaturation", Range(0, 1)) = 1
        
        [Space(10)]
        [Header(Diffuse)]
        _DiffuseDarkStrength ("DiffuseDarkStrength(暗部程度 no use)", Range(0.01, 1)) = 1
        _DayStrength ("DayStrength", Range(0, 1)) = 1
        
        [Space(10)]
        [Header(Specular)]
        _SpecularStrength ("SpecularStrength", Range(0, 2)) = 1
        _DiffuseBlendEffect ("DiffuseBlendEffect(alpha w)", Range(0, 1)) = 1
        _SpecularTrick_Flatten ("SpecularTrick_Flatten", Range(0, 1)) = 0
        
        [Space(10)]
        [Header(SpecularRefine)]
        _SpecularRefineF0Tex ("SpecularRefineF0Tex", 2D) = "white" { }
        _SpecularMask ("SpecularMask", 2D) = "white" { }
        _ViewDirYOffset ("ViewDirYOffset", Range(-2, 2)) = 0
        _BiNormalOffset_specularLut ("BiNormalOffset_specularLut", Range(-1, 1)) = 0
        _LutVPowStrength ("LutVPowStrength", Range(1, 3)) = 1
        _SpecularMaskStrength ("SpecularMaskStrength", Range(0, 1)) = 0
        _SpecularBackF0 ("SpecularBackF0", Color) = (1, 1, 1, 1)
        _SpecularBackF0_ToHPowStrength ("SpecularBackF0_ToHPowStrength", Range(0, 2)) = 1
        
        [Space(10)]
        [Header(MainLight)]
        _MainLightColor_dark ("OtherLightColor(暗部颜色)", Color) = (1, 1, 1, 1)
        
        [Space(10)]
        [Header(OtherLight)]
        _OtherLightColor ("OtherLightColor(暗部颜色)", Color) = (1, 1, 1, 1)
        _OtherLightOffset ("OtherLightOffset", Range(-1, 1)) = 0
        _OtherLightStrength ("OtherLightStrength", Range(0, 2)) = 1
        _OtherLightStrength_Offset ("OtherLightStrength_Offset", Range(-1, 1)) = 0
        _OtherLightResultStrength_day1 ("OtherLightResultStrength_Day1", Range(0, 1)) = 0.3
        _OtherLightResultStrength_day0 ("OtherLightResultStrength_Day0", Range(0, 1)) = 0.7
        
        [Space(10)]
        [Header(Shadow)]
        _ShadowDiffuseColor ("阴影颜色", Color) = (1, 1, 1, 1)
        _ShadowSmooth ("阴影过渡", Range(0, 1)) = 0
        _ShadowStrength ("阴影强度", Range(0, 1)) = 1
        _ShadowOffset ("shadowOffset", Range(-1, 1)) = 0
        _ShadowCenter ("ShadowCenter", Range(0, 1)) = 0.5
        _ShadowSmoothness ("ShadowSmoothness", Range(0, 1)) = 0.35
        
        [Space(10)]
        [Header(Normal)]
        [Toggle(_)] _IsNeedNormalMap ("是否需要法向贴图", Float) = 1
        _BumpScale ("BumpScale", Range(0, 2)) = 1
        
        [Space(10)]
        [Header(Rim(fresnel)]
        _ForwardDirStrength ("ForwardDirStrength(view dir是否偏移)", Range(0, 1)) = 0
        _NoFStrength ("NoFStrength", Range(0, 1)) = 1
        _NoFPowStrength ("NoFPowStrength", Range(1, 3)) = 1
        _RimLightArea ("RimLightArea", Range(0, 1)) = 1
        _RimLightColor ("RimLightColor", Color) = (1, 1, 1, 1)
        _RimLightStrength ("RimLightStrength", Range(0, 2)) = 1
        _RimLightDiffuseColorEffect ("RimLightDiffuseColorEffect", Range(0, 1)) = 0.1
        
        [Space(10)]
        [Header(Rim(NoLxz)]
        _RimLightNoLxzStrength ("RimLightStrength", Range(0, 2)) = 1
        
        [Space(10)]
        [Header(Ao)]
        _AoStrength ("AoStrength", Range(1, 3)) = 1
        _SelfAoShadowStrength ("SelfAoShadowStrength", Range(0, 1)) = 1
        
        [Space(10)]
        [Header(BackLight)]
        _BackLightStrength ("BackLightStrength", Range(0, 1)) = 1
        
        [Space(10)]
        [Header(Outline)]
        _OutlineWidth ("Width", Range(0, 10)) = 0.5
        _ZBias ("ZBias", Range(0, 0.2)) = 0.001
        _OutlineColor ("OutlineColor", Color) = (1, 1, 1, 1)
        _OutLineStrength ("OutLineStrength", Range(0, 2)) = 0.8
        _ZMinRefine ("ZMinRefine", Range(0, 1)) = 0.4
        [Toggle(_)] _IsHair ("头发开关", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_MainCullMode]
            Blend One Zero

            HLSLPROGRAM

            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct VertexInput
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float3 texcoord2 : TEXCOORD2;
                float2 staticLightmapUV : TEXCOORD3;
            };

            struct VertexOutput
            {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 posWS : TEXCOORD1;
                float4 posCS : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                float3 tangenWS : TEXCOORD4;
                float3 bitangenWS : TEXCOORD5;
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 6);
            };

            TEXTURE2D(_AlbedoTex); SAMPLER(sampler_AlbedoTex); float4 _AlbedoTex_ST;
            TEXTURE2D(_OrmTex); SAMPLER(sampler_OrmTex);
            TEXTURE2D(_RampTex); SAMPLER(sampler_RampTex);
            TEXTURE2D(_NormalTex); SAMPLER(sampler_NormalTex); float4 _NormalTex_ST;
            TEXTURE2D(_SpecularRefineF0Tex); SAMPLER(sampler_SpecularRefineF0Tex);
            TEXTURE2D(_SpecularMask); SAMPLER(sampler_SpecularMask); float4 _SpecularMask_ST;

            CBUFFER_START(UnityPerMaterial)
                half _IsNeedOrmTex;

                // light
                half _IsNeedSelfLight;
                float _SelfLightIntensity;

                // albedo
                float4 _BaseColor;
                float _BaseColorPow;
                float _RampColorNoLStrength;

                // 法向
                half _IsNeedNormalMap;
                float _BumpScale;

                // mainLight
                float3 _MainLightColor_dark;

                // otherLight
                float3 _OtherLightColor;
                float _OtherLightOffset;
                float _OtherLightStrength;
                float _OtherLightStrength_Offset;
                float _OtherLightResultStrength_day1;
                float _OtherLightResultStrength_day0;

                // shadow
                float _ShadowStrength;
                float _ShadowOffset;
                float _ShadowCenter;
                float _ShadowSmoothness;

                // rim refine部分
                float _ForwardDirStrength;
                float _NoFStrength;
                float _NoFPowStrength;

                // rim
                float _RimLightArea;
                float3 _RimLightColor;
                float _RimLightStrength;
                float _RimLightDiffuseColorEffect;
                float _RimLightNoLxzStrength;

                // ao
                float _AoStrength;
                float _SelfAoShadowStrength;

                // DiffuseColorLightAndDark
                float _AlbedoDarkStrength;
                float _AlbedoDarkSaturation;

                // Diffuse
                float _DiffuseDarkStrength;
                float _DayStrength;

                // specular
                float _SpecularStrength;
                float _DiffuseBlendEffect;
                float _ViewDirYOffset;
                float _BiNormalOffset_specularLut;
                float _LutVPowStrength;
                float _SpecularMaskStrength;
                float3 _SpecularBackF0;
                float _SpecularBackF0_ToHPowStrength;
                float _SpecularTrick_Flatten;

                // backLight
                float _BackLightStrength;

                // test
                float4 _FaceCenter;

            CBUFFER_END

            VertexOutput Vert(VertexInput v)
            {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.uv0.y = 1 - o.uv0.y;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.posWS = mul(GetObjectToWorldMatrix(), v.vertex);
                o.posCS = TransformWorldToHClip(o.posWS.xyz);
                
                o.normalWS = TransformObjectToWorldNormal(v.normal);
                o.tangenWS = normalize(mul(GetObjectToWorldMatrix(), float4(v.tangent.xyz, 0.0)).xyz);
                o.bitangenWS = normalize(cross(o.normalWS, o.tangenWS) * v.tangent.w * unity_WorldTransformParams.w);
                
                OUTPUT_LIGHTMAP_UV(v.staticLightmapUV, unity_LightmapST, o.staticLightmapUV);
                OUTPUT_SH(o.normalWS.xyz, o.vertexSH);
                
                return o;
            }


            float3 UnpackNormalFromTex(float4 normalTex_var, float scale = 1.0)
            {
                float3 normal;
                normal.xy = normalTex_var.xy * 2.0 - 1.0;
                normal.z = max(1.0e-16, sqrt(1.0 - saturate(dot(normal.xy, normal.xy))));
                normal.xy *= scale;
                return normal;
            }

            float3 UnpackNormalFromTex1(float4 normalTex_var, float scale = 1.0)
            {
                float3 normal;
                normal.xy = normalTex_var.xy * 2.0 - 1.0;
                normal.z = max(1.0e-16, sqrt(1.0 - saturate(dot(normal.xy, normal.xy))));
                normal.z = normal.z * 0.5 + 0.5;
                normal.xy *= scale;
                return normal;
            }


            float MyCalculateLightStrength(float3 color)
            {
                float3 lightCoefficient = float3(0.212672904, 0.715152204, 0.0721750036);
                return dot(color, lightCoefficient);
            }

            float MySmoothstep(float x)
            {
                float t = x * - 2 + 3;
                float x2 = x * x;
                t = t * x2;
                return t;
            }

            float SigmoidSharp(float x, float center, float sharp)
            {
                float s;
                s = 1.0 / (1.0 + pow(100000.0, (-3.0 * sharp * (x - center))));
                return s;
            };

            float4 Frag(VertexOutput input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                
                float3 color = float3(0, 0, 0);
                float4 mainTex_var = SAMPLE_TEXTURE2D(_AlbedoTex, sampler_AlbedoTex, input.uv0 * _AlbedoTex_ST.xy + _AlbedoTex_ST.zw);              
                float4 ormTex_var = SAMPLE_TEXTURE2D(_OrmTex, sampler_OrmTex, input.uv0);
                float4 normalTex_var = SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, input.uv0 * _NormalTex_ST.xy + _NormalTex_ST.zw);
                ormTex_var = lerp(float4(0, 1, 1, 0), ormTex_var, _IsNeedOrmTex);
                

                Light mainLight = GetMainLight();
                float3 mainLightColor = mainLight.color;
                float mainLightIntensity = max(0.001, (0.299 * mainLightColor.r + 0.587 * mainLightColor.g + 0.114 * mainLightColor.b));
                mainLightColor = mainLightColor / mainLightIntensity;

                
                mainLightColor = lerp(mainLightColor, float3(1, 1, 1), _IsNeedSelfLight);
                mainLightIntensity = lerp(mainLightIntensity, _SelfLightIntensity, _IsNeedSelfLight);

                float mainLightColorStrength = MyCalculateLightStrength(mainLightColor * _MainLightColor_dark);
                float3 mainLightDir = _MainLightPosition;
                
                float3 mainLightDir_xz = normalize(float3(mainLightDir.x, 6.10351562e-05, mainLightDir.z));


                float3x3 tangentTransform = float3x3(input.tangenWS, input.bitangenWS, input.normalWS);
                float3 normalTex_processed = UnpackNormalFromTex(normalTex_var, _BumpScale);
                float3 normalWS = lerp(input.normalWS, normalize(mul(normalTex_processed, tangentTransform)), _IsNeedNormalMap);

                float facing = isFrontFace ? 1.0 : - 1.0;
                normalWS = normalWS * facing;
                
                float3 normalTex_Hprocessed = UnpackNormalFromTex(normalTex_var.zwzw, _BumpScale);
                float3 HNormalWS = lerp(input.normalWS, normalize(mul(normalTex_Hprocessed, tangentTransform)), _IsNeedNormalMap);
                
                
                float3 sphereNormal = normalize(input.posWS.xyz - _FaceCenter.xyz);
                
                HNormalWS = lerp(sphereNormal, HNormalWS, ormTex_var.x);
                
                
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - input.posWS.xyz);
                
                float3 cameraForward = UNITY_MATRIX_V[2].xyz;
                cameraForward = normalize(cameraForward);
                viewDir = lerp(viewDir, cameraForward, _ForwardDirStrength);
                viewDir = normalize(viewDir);
                float NoV = dot(normalWS, viewDir);
                
                float3 worldUp = float3(0, 1, 0);
                float3 cameraRight = cross(worldUp, cameraForward);
                cameraRight = normalize(cameraRight + float3(1e-6, 0, 0));


                float specularMask = SAMPLE_TEXTURE2D(_SpecularMask, sampler_SpecularMask, input.uv0 * _SpecularMask_ST.xy + _SpecularMask_ST.zw).x;
                
                
                float3 hairTempTangent = float3(0, 0, 1);
                
                float3 hairTempBiTangent = HNormalWS.zxy * hairTempTangent.yzx - HNormalWS * hairTempTangent;
                
                
                float3 hairBiTangent = lerp(hairTempBiTangent, input.tangenWS.yzx, ormTex_var.x);
                
                
                
                float3 hariBinormal = HNormalWS.yzx * hairBiTangent.yzx - hairBiTangent * HNormalWS.zxy;
                
                
                float3 baseColor = mainTex_var.xyz * _BaseColor.xyz;
                baseColor = pow(baseColor, _BaseColorPow);
                float3 baseColor_dark = baseColor * _AlbedoDarkStrength;
                float baseColor_dark_strength = MyCalculateLightStrength(baseColor_dark);
                baseColor_dark = lerp(baseColor_dark_strength.xxx, baseColor_dark, _AlbedoDarkSaturation);


                float roughness = 1 - ormTex_var.w;
                float metallic = 0;
                float reflectivity = ormTex_var.g;
                float ao = ormTex_var.b;
                ao = pow(ao, _AoStrength);
                float3 F0 = 0.04 * reflectivity.xxx + metallic * (baseColor - reflectivity.xxx * 0.04);
                
                float energyDistribution_metallic = 0.96 - 0.96 * metallic;
                float3 mainDiffuseColor_Light = baseColor * energyDistribution_metallic;
                float3 mainDiffuseColor_Dark = baseColor_dark * energyDistribution_metallic;

                
                float shadowAttenuation = 1;
                float2 screenUV = GetNormalizedScreenSpaceUV(input.pos.xy);
                shadowAttenuation = SAMPLE_TEXTURE2D(_ScreenSpaceShadowmapTexture, sampler_PointClamp, screenUV).x;
                // shadowAttenuation = min(shadowAttenuation, SamplePerObjectScreenSpaceShadowmap(screenUV));
                float shadowScene = (SigmoidSharp(shadowAttenuation, _ShadowCenter, _ShadowSmoothness) + _ShadowOffset) * _ShadowStrength ;
                shadowScene = saturate(shadowScene);
                shadowScene = lerp(1, shadowScene, ormTex_var.x);

                
                float NoL = dot(normalWS, mainLightDir);
                float3 mainDiffuseColor_moreDark = mainDiffuseColor_Dark;
                float3 mainDiffuseColor_Dark_attention = mainDiffuseColor_Dark * 0.65;
                float mainDiffuseColor_Lgiht_Strength = MyCalculateLightStrength(mainDiffuseColor_Light);
                float mainDiffuseColor_Dark_Strength = MyCalculateLightStrength(mainDiffuseColor_Dark_attention);


                float2 cameraForward_xz = normalize(cameraForward.xz);
                float backLight = saturate(-dot(cameraForward_xz, mainLightDir_xz.xz));
                
                float backLight_y = saturate(-abs(cameraForward.y) + 0.75);
                backLight_y = MySmoothstep(backLight_y);
                backLight = backLight * backLight_y;

                
                float rampNoL = 0.5 - 0.5 * NoL * NoL;
                float NoL_rampFinal = rampNoL * backLight + NoL;
                NoL_rampFinal = max(NoL_rampFinal, -1);
                NoL_rampFinal = min(NoL_rampFinal, 1);
                NoL_rampFinal = NoL_rampFinal * 0.5 + 0.5;
                
                float4 rampColor = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(NoL_rampFinal, 0.5));
                rampColor.w *= _RampColorNoLStrength;
                
                float rampColor_max = max(max(rampColor.x, rampColor.y), rampColor.z);
                float rampColor_min = min(min(rampColor.x, rampColor.y), rampColor.z);
                float rampColor_xyzStrength = rampColor_max - rampColor_min;
                
                float NoF = dot(normalWS, cameraForward);
                NoF = NoF * 0.5 + 0.5;
                float rampColor_NoF = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(NoF, 0.5)).w;
                rampColor_NoF *= _NoFStrength;
                rampColor_NoF = pow(rampColor_NoF, _NoFPowStrength);


                float ao_shadow = ao * shadowScene;
                float min_shadowEffect = min(ao, shadowScene);
                min_shadowEffect = min(min_shadowEffect, rampColor.w);
                float ao_shadow_NoFRamp = ao_shadow * rampColor_NoF;

                
                float3 otherLightDir = float3(0, 1, 0);
                float otherLightNoL = dot(otherLightDir, normalWS);
                otherLightNoL = saturate(otherLightNoL + _OtherLightOffset);
                otherLightNoL = otherLightNoL * _OtherLightStrength + _OtherLightStrength_Offset;
                
                float3 otherLightColor_dark = _OtherLightColor;
                float3 otherLightColor = lerp(otherLightColor_dark, float3(1, 1, 1), min_shadowEffect);
                float3 otherLightResult = otherLightColor * otherLightNoL;

                
                float3 otherLightResult_day1 = otherLightResult * _OtherLightResultStrength_day1;
                float3 otherLightResult_day0 = otherLightResult * _OtherLightResultStrength_day0;
                
                
                
                float3 mainLightColor_new = lerp(mainLightColorStrength.xxx, mainLightColor, min_shadowEffect) * mainLightIntensity;
                
                float3 mainLightColor_final = lerp(otherLightResult_day0, mainLightColor_new + otherLightResult_day1, _DayStrength);

                
                float3 mainDiffuseColor_Dark_final = lerp(mainDiffuseColor_Dark_Strength.xxx, mainDiffuseColor_Dark_attention, 1.2);
                
                float3 mainDiffuseColor_Dark_lerp = lerp(mainDiffuseColor_Dark_final, mainDiffuseColor_moreDark, saturate(ao_shadow_NoFRamp + rampColor.w));
                
                float3 mainDiffuseBrdf = lerp(mainDiffuseColor_Dark_lerp, mainDiffuseColor_Light, min_shadowEffect);

                float3 rampColor_xyzEffect = rampColor * rampColor_xyzStrength + 1 - rampColor_xyzStrength;
                float3 mainDiffuseBrdf_rampColor = mainDiffuseBrdf * rampColor_xyzEffect;
                
                float mainDiffuseBrdf_strength = MyCalculateLightStrength(mainDiffuseBrdf);
                float mainDiffuseBrdf_rampColor_strength = MyCalculateLightStrength(mainDiffuseBrdf_rampColor);
                float rampColor_control = mainDiffuseBrdf_strength / max(0.01, mainDiffuseBrdf_rampColor_strength);
                rampColor_control = max(0, rampColor_control);
                rampColor_control = min(1.5, rampColor_control);
                
                
                float3 mainDiffuseColor_Light_final = lerp(mainDiffuseColor_Lgiht_Strength.xxx, mainDiffuseColor_Light, 1.2);
                
                
                float3 mainDiffuseBrdf_lowLight = lerp(mainDiffuseColor_moreDark, mainDiffuseColor_Light_final, ao_shadow_NoFRamp);
                
                float3 mainDiffuseBrdf_final = lerp(mainDiffuseBrdf_lowLight, mainDiffuseBrdf_rampColor * rampColor_control, _DayStrength);
                
                
                float3 mainDiffuseResult = mainLightColor_final * mainDiffuseBrdf_final;


                float3 hairViewDir = normalize(viewDir + float3(0, _ViewDirYOffset * (1 - ormTex_var.x), 0));
                float3 hairHalfDir = normalize(hairViewDir + mainLightDir);

                float dotRight = dot(HNormalWS, cameraRight);
                float3 cylinderNormal = normalize(HNormalWS - dotRight * cameraRight);
                float3 flatHNormal = normalize(lerp(HNormalWS, cylinderNormal, _SpecularTrick_Flatten));
                float3 fakeTangent = normalize(cross(float3(0, 1, 0), flatHNormal));
                float3 hairBiNormal_flatten = normalize(cross(flatHNormal, fakeTangent));
                float3 hairBiNormal_lut = normalize(lerp(hairBiNormal_flatten, hariBinormal, ormTex_var.x) + HNormalWS * _BiNormalOffset_specularLut);
                
                
                float ToH_lut = dot(hairHalfDir, hairBiNormal_lut);
                //return ToH_lut;
                
                float lutUV_u = 1 - ToH_lut * ToH_lut;
                lutUV_u = max(0.0001, sqrt(lutUV_u));
                
                lutUV_u = 200 * log2(lutUV_u);
                
                lutUV_u = saturate(exp2(lutUV_u) * reflectivity);

                
                float2 viewDirProj = float2(dot(viewDir, cameraRight), dot(viewDir, cameraForward));
                float2 HNormalProj = float2(dot(HNormalWS, cameraRight), dot(HNormalWS, cameraForward));
                float VoHN_horizontal = saturate(dot(viewDirProj, HNormalProj));
                VoHN_horizontal = pow(VoHN_horizontal, _LutVPowStrength);
                float directionMask = step(0.0, ToH_lut);
                float lutUV_v = VoHN_horizontal * VoHN_horizontal * directionMask;
                
                float4 SpecularRefineF0Tex_var = SAMPLE_TEXTURE2D(_SpecularRefineF0Tex, sampler_SpecularRefineF0Tex, float2(lutUV_u, lutUV_v));
                
                
                float specularMaskEffect = 1 + reflectivity * (1 - specularMask) * pow(sqrt(1 - ToH_lut * ToH_lut), trunc(200));
                mainDiffuseResult *= max(specularMaskEffect * _SpecularMaskStrength, 1) ;

                
                float3 lutF0 = SpecularRefineF0Tex_var.xyz * F0;
                float3 backF0 = _SpecularBackF0 * ormTex_var.w * pow(sqrt(1 - ToH_lut * ToH_lut), trunc(200 * _SpecularBackF0_ToHPowStrength));
                float3 finalF0 = lutF0 * 7 + backF0;

                
                float ao_shadow_lowLight = lerp(ao_shadow_NoFRamp, min_shadowEffect, _DayStrength);
                float selfAoShadowEffect = lerp(_SelfAoShadowStrength, 1, ao_shadow_lowLight);

                
                float3 mainLightSpecularResult = mainLightColor_final * selfAoShadowEffect * finalF0;
                //  return float4(finalF0.xyz, 1);
                // float3 secondHairViewDir = normalize(viewDir + float3(0, _SecondSpecularViewOffset * (1 - ormTex_var.x), 0));
                // float3 secondHairHalfDir = normalize(secondHairViewDir + mainLightDir);
                // float secondToH = dot(secondHairHalfDir, hairBiNormal_lut);
                // float secondSpecularShape = sqrt(saturate(1 - secondToH * secondToH));
                // secondSpecularShape = pow(max(secondSpecularShape, 0.0001), max(1.0, 200.0 * (1.0 - _SecondSpecularPower)));
                // float secondSpecularMask = lerp(1.0, specularMask, _SpecularMaskBlend);
                // float3 secondSpecularResult = mainLightColor_final * selfAoShadowEffect * _SpecularBackF0 * secondSpecularShape * secondSpecularMask * _SecondSpecularIntensity;
                // mainLightSpecularResult += secondSpecularResult * _SecondSpecularBlend;

                float HNormalStrength = 0.75;
                float FHightLightPos = 0.4;
                float HightLightLength = 0.85;
                float4 NHColor = SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, input.uv0 * _NormalTex_ST.xy + _NormalTex_ST.zw);
                float3 normalTex_Hprocessed1 = UnpackNormalFromTex1(NHColor.zwzw, _BumpScale);
           
                float3 HNormalWS1 = normalize(mul(normalTex_Hprocessed1, tangentTransform));
                float4 PColor = SAMPLE_TEXTURE2D(_OrmTex, sampler_OrmTex, input.uv0);
                float NormalStrength = 1.0;

                 
                float3 hairTempTangent1 = float3(0, 0, 1);
                float3 hairTempBiTangent1 = HNormalWS1.zxy * hairTempTangent1.yzx - HNormalWS1 * hairTempTangent1;
                float3 hairBiTangent1 = lerp(input.tangenWS.yzx, hairTempBiTangent1, ormTex_var.x);
                
                
                
                float3 hariBinormal1 = HNormalWS1.yzx * hairBiTangent1.yzx - hairBiTangent1 * HNormalWS1.zxy;
                //return float4(input.tangenWS, 1);


                
                float diffuseSpecularBlend = 1 - _DiffuseBlendEffect * (1 - mainTex_var.w);
                float3 mainLightResult = mainDiffuseResult * diffuseSpecularBlend + mainLightSpecularResult * _SpecularStrength;
                 
                
                float rimStart = _RimLightArea * - 0.6 + 0.8;
                float rimEnd = _RimLightArea * - 0.4 + 0.9;
                float rimWidth = rimEnd - rimStart;
                float rimt = ((1.0 - NoV) - rimStart) / rimWidth;
                
                rimt = saturate(rimt);
                float rimArea = rimt * rimt * (3.0 - 2.0 * rimt);
                
                float3 rimLight = rimArea * _RimLightColor * _RimLightStrength;
                float3 rimLight_effectd = rimLight * min(ao, shadowScene);
                
                
                float3 rimLight_brdf = lerp(0.25, mainDiffuseColor_Light, _RimLightDiffuseColorEffect);
                float3 rimLightResult = rimLight_brdf * rimLight_effectd;

                
                
                float3 rim_mainLight = lerp(1, mainLightColor * mainLightIntensity, _DayStrength);
                float NoLxz = dot(normalWS, mainLightDir_xz);
                
                float NoLxz_refine = 0.5 - (0.5 * NoLxz - 1) * NoLxz;
                NoLxz_refine *= _DayStrength;
                
                float3 rim_mainLightResult = rim_mainLight * NoLxz_refine;
                
                float NoV_mask = saturate(5.0 * (0.4 - NoV));
                NoV_mask = MySmoothstep(NoV_mask);
                rim_mainLightResult *= NoV_mask;
                //  rim_mainLightResult *= min(ao, shadowScene);
                // rim_mainLightResult *= max(float3(0.150000006, 0.150000006, 0.150000006), mainDiffuseColor_Light);
                rim_mainLightResult *= _RimLightNoLxzStrength;
                float3 rimLight_finalResult = rimLightResult + rim_mainLightResult;
                //    return float4(rim_mainLightResult.rgb, 1);
                //return float4(rimLight_finalResult, 1);
                
                float3 resultColor = mainLightResult + max(rimLight_finalResult, 0);

                
                
                float4 finalColor = float4(resultColor, 1);
                return finalColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            ColorMask R
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            // #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            struct Attributes
            {
                float4 position : POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                #if defined(_ALPHATEST_ON)
                    float2 uv : TEXCOORD0;
                #endif
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_AlphaTex); SAMPLER(sampler_AlphaTex);

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                #if defined(_ALPHATEST_ON)
                    output.uv = input.texcoord;
                    output.uv.y = 1 - output.uv.y;
                #endif
                output.positionCS = TransformObjectToHClip(input.position.xyz);
                return output;
            }

            half DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                #if defined(_ALPHATEST_ON)
                    Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_AlphaTex, sampler_AlphaTex)).r, _BaseColor, _Cutoff);
                #endif

                #if defined(LOD_FADE_CROSSFADE)
                    LODFadeCrossFade(input.positionCS);
                #endif
                
                return input.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            // -------------------------------------
            // Render State Commands
            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma multi_compile _ _DepthNormalTest
            // -------------------------------------
            // Shader Stages
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Material Keywords
            // #pragma shader_feature_local _NORMALMAP
            // #pragma shader_feature_local _PARALLAXMAP
            // #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            // #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            // -------------------------------------
            // Unity defined keywords
            // #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // -------------------------------------
            // Universal Pipeline keywords
            // #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            // #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Includes
            // #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 tangentOS : TANGENT;
                float2 texcoord : TEXCOORD0;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                float2 uv : TEXCOORD1;

                half3 normalWS : TEXCOORD2;

                #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
                    half4 tangentWS : TEXCOORD4;    // xyz: tangent, w: sign
                #endif

                half3 viewDirWS : TEXCOORD5;

                #if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
                    half3 viewDirTS : TEXCOORD8;
                #endif

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            
            TEXTURE2D(_AlphaTex); SAMPLER(sampler_AlphaTex);
            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.uv = input.texcoord;
                output.uv.y = 1 - output.uv.y;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normal, input.tangentOS);

                output.normalWS = half3(normalInput.normalWS);
                #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR) || defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
                    float sign = input.tangentOS.w * float(GetOddNegativeScale());
                    half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
                #endif

                #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
                    output.tangentWS = tangentWS;
                #endif

                return output;
            }

            void DepthNormalsFragment(
                Varyings input
                , out half4 outNormalWS : SV_Target0
                #ifdef _WRITE_RENDERING_LAYERS
                    , out float4 outRenderingLayers : SV_Target1
                #endif
            )
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                #if defined(_ALPHATEST_ON)
                    Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_AlphaTex, sampler_AlphaTex)).r, _BaseColor, _Cutoff);
                #endif

                #if defined(LOD_FADE_CROSSFADE)
                    LODFadeCrossFade(input.positionCS);
                #endif


                #if defined(_NORMALMAP) || defined(_DETAIL)
                    float sgn = input.tangentWS.w;      // should be either +1 or -1
                    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
                    float3 normalTS = SampleNormal(input.uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);

                    #if defined(_DETAIL)
                        half detailMask = SAMPLE_TEXTURE2D(_DetailMask, sampler_DetailMask, input.uv).a;
                        float2 detailUv = input.uv * _DetailAlbedoMap_ST.xy + _DetailAlbedoMap_ST.zw;
                        normalTS = ApplyDetailNormal(detailUv, normalTS, detailMask);
                    #endif

                    float3 normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));
                #else
                    float3 normalWS = input.normalWS;
                #endif

                outNormalWS = half4(NormalizeNormalPerPixel(normalWS), 0.0);
                

                #ifdef _WRITE_RENDERING_LAYERS
                    uint renderingLayers = GetMeshRenderingLayer();
                    outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
                #endif
            }

            ENDHLSL
        }
    }
}
