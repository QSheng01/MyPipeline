Shader "ZhongMoDi/RoleLit"
{
    Properties
    {
        [Space(10)]
        [Enum(OFF, 0, FRONT, 1, BACK, 2)] _MainCullMode ("Main Cull Mode", int) = 2
        [Toggle(_ALPHATEST_ON)]_AlphaClip ("Alpha Clip", Float) = 0
        _Cutoff ("Cutoff", Range(0, 1)) = 1
        
        [Space(10)]
        [Header(Transparent Settings)]
        [Toggle(_TRANSPARENT_ON)]_TransParentToon ("TransParentToon", Float) = 0
        _CustomAlpha ("Custom Alpha (整体透明度)", Range(0, 1)) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1 // 默认 1 (One)
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0 // 默认 0 (Zero)
        [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Float) = 1 // 默认 1 (On)
        _EmissionColor ("Emission Color", Color) = (1, 1, 1, 1)
        _EmissionColorStrength ("Emission Color Strength", Range(0, 10)) = 1
        _EmissionTex ("Emission Tex", 2D) = "white" { }
        
        [Space(10)]
        [Header(IsSelfLight)]
        [Toggle(_)] _IsNeedSelfLight ("是否需要调试白光", Float) = 0
        _SelfLightIntensity ("SelfLightIntensity", Float) = 3
        
        [Space(10)]
        [Header(MainTextures)]
        _AlbedoTex ("AlbedoTex", 2D) = "white" { }
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
        
        [Space(10)]
        [Header(SpecularRefine)]
        [Toggle(_SPECULARREFINE_ON)]_SpecularRefine ("SpecularRefine", Float) = 0
        _SpecularRefineF0Tex ("SpecularRefineF0Tex", 2D) = "white" { }
        _RefineF0U_lerp ("RefineF0U_lerp", Range(0, 1)) = 0
        _SpecularRefineColorTex ("SpecularRefineColorTex", 2D) = "white" { }
        _SpecularRefineColor ("SpecularRefineColor", Color) = (1, 1, 1, 1)
        _SpecularRefineColorStrength ("SpecularRefineColorStrength", Range(0, 1)) = 1
        
        [Space(10)]
        [Header(sss)]
        [Toggle(_)] _IsNeedSss ("是否需要SSS", Float) = 0
        _SssColor ("SssColor", Color) = (1, 1, 1, 1)
        _SssPowStrength ("SssPowStrength", Range(0, 5)) = 1
        
        
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
        [Header(EnvLight)]
        _EnvColor ("EnvColor", Color) = (1, 1, 1, 1)
        _EnvMap ("EnvMap", Cube) = "black" { }
        _EnvLightStrength ("EnvLightStrength", Range(0, 1)) = 1
        _EnvRotation ("Env Rotation", Range(0, 360)) = 0
        
        [Space(10)]
        [Header(Outline)]
        _OutlineWidth ("Width", Range(0, 10)) = 0.5
        _ZBias ("ZBias", Range(0, 0.2)) = 0.0
        _OutlineColor ("OutlineColor", Color) = (1, 1, 1, 1)
        _OutLineStrength ("OutLineStrength", Range(0, 2)) = 0.8
        _ZMinRefine ("ZMinRefine", Range(0, 1)) = 0.4
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Cull [_MainCullMode]
            Blend [_SrcBlend] [_DstBlend]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // 贴图变量管理
            TEXTURE2D(_AlbedoTex); SAMPLER(sampler_AlbedoTex);
            TEXTURE2D(_OrmTex); SAMPLER(sampler_OrmTex);
            TEXTURE2D(_RampTex); SAMPLER(sampler_RampTex);
            TEXTURE2D(_NormalTex); SAMPLER(sampler_NormalTex);
            TEXTURE2D(_EmissionTex);
            TEXTURE2D(_SpecularRefineF0Tex);
            TEXTURE2D(_SpecularRefineColorTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _AlbedoTex_ST;
                float4 _NormalTex_ST;
                half _IsNeedOrmTex;
                float _CustomAlpha;

                // trans
                float3 _EmissionColor;
                float _EmissionColorStrength;

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
                
                float _RefineF0U_lerp;
                float3 _SpecularRefineColor;
                float _SpecularRefineColorStrength;

                // backLight
                float _BackLightStrength;

                // envLight
                float3 _EnvColor;
                TEXTURECUBE(_EnvMap);
                float _EnvLightStrength;
                float _EnvRotation;

                // sss
                half _IsNeedSss;
                float3 _SssColor;
                float _SssPowStrength;

            CBUFFER_END


            struct Attributes
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float3 texcoord2 : TEXCOORD2;
                float2 staticLightmapUV : TEXCOORD3;
            };


            struct Varyings
            {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 posWS : TEXCOORD1;
                float4 posCS : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                float3 tangenWS : TEXCOORD4;
                float3 bitangenWS : TEXCOORD5;
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 6);
                float2 uv1 : TEXCOORD7;
                float2 uv2 : TEXCOORD8;
            };


            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.uv0 = input.texcoord0;
                output.uv0.y = 1 - input.texcoord0.y;
                output.pos = TransformObjectToHClip(input.vertex.xyz);
                output.posWS = mul(GetObjectToWorldMatrix(), input.vertex);
                output.posCS = TransformWorldToHClip(output.posWS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normal);
                output.tangenWS = normalize(mul(GetObjectToWorldMatrix(), float4(input.tangent.xyz, 0.0)).xyz);
                output.bitangenWS = normalize(cross(output.normalWS, output.tangenWS) * input.tangent.w * unity_WorldTransformParams.w);
                output.uv1 = input.texcoord1;
                
                OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
                OUTPUT_SH(output.normalWS.xyz, output.vertexSH);
                
                output.uv2 = input.texcoord2;
                return output;
            }

            float3 UnpackNormalFromTex(float4 normalTex_var, float scale = 1.0)
            {
                float3 normal;
                normal.xy = normalTex_var.xy * 2.0 - 1.0;
                normal.z = max(1.0e-16, sqrt(1.0 - saturate(dot(normal.xy, normal.xy))));
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

            float4 Frag(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
            
                float3 color = float3(0, 0, 0);
                float4 mainTex_var = SAMPLE_TEXTURE2D(_AlbedoTex, sampler_AlbedoTex, input.uv0 * _AlbedoTex_ST.xy + _AlbedoTex_ST.zw);
                    
                float4 ormTex_var = SAMPLE_TEXTURE2D(_OrmTex, sampler_OrmTex, input.uv0);
                float4 normalTex_var = SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, input.uv0 * _NormalTex_ST.xy + _NormalTex_ST.zw);

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

                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - input.posWS.xyz);
                float3 cameraForward = UNITY_MATRIX_V[2].xyz;
                cameraForward = normalize(cameraForward);
                viewDir = lerp(viewDir, cameraForward, _ForwardDirStrength);
                viewDir = normalize(viewDir);

                float3 worldUp = float3(0, 1, 0);
                float3 cameraRight = cross(worldUp, cameraForward);
                cameraRight = normalize(cameraRight + float3(1e-6, 0, 0));

                float3 baseColor = mainTex_var.xyz * _BaseColor.xyz;
                baseColor = pow(baseColor, _BaseColorPow);

                float NoV_sss = saturate(dot(normalWS, viewDir));
                float sss_area = pow(1.05 - NoV_sss, 1 + mainTex_var.w * _SssPowStrength);
                sss_area = min(0.9, sss_area) * step(mainTex_var.w, 0.99);
                baseColor = lerp(baseColor, _SssColor, lerp(0, sss_area, _IsNeedSss));

                float3 baseColor_dark = baseColor * _AlbedoDarkStrength;
                float baseColor_dark_strength = MyCalculateLightStrength(baseColor_dark);
                baseColor_dark = lerp(baseColor_dark_strength.xxx, baseColor_dark, _AlbedoDarkSaturation);

                float roughness = 1 - ormTex_var.w;
                float roughness2 = max(roughness * roughness, 0.0078);
                float metallic = ormTex_var.r;
                float reflectivity = ormTex_var.g;
                float ao = ormTex_var.b;
                float2 screenAOUV = GetNormalizedScreenSpaceUV(input.pos.xy);
                AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(screenAOUV);
                
                ao = min(ao, aoFactor.indirectAmbientOcclusion);
                ao = pow(ao, _AoStrength);
               // float3 F0 = 0.04 * reflectivity.xxx + metallic * (baseColor - reflectivity.xxx * 0.04);

                float3 dielectricF0 = 0.04 * reflectivity.xxx;
                float3 F0 = lerp(dielectricF0, baseColor, metallic);

                float energyDistribution_metallic = 0.96 - 0.96 * metallic;
                float3 mainDiffuseColor_Light = baseColor * energyDistribution_metallic;
                
                float3 mainDiffuseColor_Dark = baseColor_dark * energyDistribution_metallic;
                
                float shadowAttenuation = 1;
                float2 screenUV = GetNormalizedScreenSpaceUV(input.pos.xy);
                shadowAttenuation = SAMPLE_TEXTURE2D(_ScreenSpaceShadowmapTexture, sampler_PointClamp, screenUV).x;
                // shadowAttenuation = min(shadowAttenuation, SamplePerObjectScreenSpaceShadowmap(screenUV));
                float shadowScene = (SigmoidSharp(shadowAttenuation, _ShadowCenter, _ShadowSmoothness) + _ShadowOffset) * _ShadowStrength ;
                shadowScene = saturate(shadowScene);
                // return shadowScene;

                float NoL = dot(normalWS, mainLight.direction);
                float3 mainDiffuseColor_moreDark = mainDiffuseColor_Dark ;
                float3 mainDiffuseColor_Dark_attention = mainDiffuseColor_Dark * 0.65 ;
                
                float mainDiffuseColor_Lgiht_Strength = MyCalculateLightStrength(mainDiffuseColor_Light);
                float mainDiffuseColor_Dark_Strength = MyCalculateLightStrength(mainDiffuseColor_Dark_attention);

                float2 cameraForward_xz = normalize(cameraForward.xz);
                float backLight = saturate(-dot(cameraForward_xz, mainLight.direction.xz));
                
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

                //mainLightColorStrength.xxx
                float3 mainLightColor_new = lerp(mainLightColorStrength.xxx, mainLightColor, min_shadowEffect) * mainLightIntensity;//mainLightIntensity;
                
                float3 mainLightColor_final = lerp(otherLightResult_day0, mainLightColor_new + otherLightResult_day1, _DayStrength);

                float3 mainDiffuseColor_Dark_final = lerp(mainDiffuseColor_Dark_Strength.xxx, mainDiffuseColor_Dark_attention, 1.2);
                
                float3 mainDiffuseColor_Dark_lerp = lerp(mainDiffuseColor_Dark_final, mainDiffuseColor_moreDark, saturate(ao_shadow_NoFRamp + rampColor.w));
                
                float3 mainDiffuseBrdf = lerp(mainDiffuseColor_Dark_lerp, mainDiffuseColor_Light, min_shadowEffect);

                //float3 rampColor_xyzEffect = rampColor * rampColor_xyzStrength + 1 - rampColor_xyzStrength;
                float3 rampColor_xyzEffect = lerp(1.0, rampColor.rgb, rampColor_xyzStrength);
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

                float ao_shadow_lowLight = lerp(ao_shadow_NoFRamp, min_shadowEffect, _DayStrength);
                float selfAoShadowEffect = lerp(_SelfAoShadowStrength, 1, ao_shadow_lowLight);

                
                float forwardLightDir_y = lerp(0.5, mainLight.direction.y, _DayStrength);
                float3 forwardLightDir = normalize(float3(cameraForward.x, forwardLightDir_y, cameraForward.z));
                float NoV = saturate(dot(normalWS, viewDir));
                float3 mainLightDir_new = mainLightDir * _DayStrength + 2 * forwardLightDir;
                float3 halfDir_new = viewDir * (2 + _DayStrength) + mainLightDir_new;
                halfDir_new = normalize(halfDir_new);
                float NoH = dot(normalWS, halfDir_new);

                
                
                float a2 = roughness2 * roughness2;
                float specular_D = (NoH * a2 - NoH) * NoH + 1;
                specular_D *= specular_D;
                specular_D = a2 / specular_D;

                
                float specular_V = NoV * 2 + roughness2 + 9.99999975e-05;
                specular_V = 0.5 / specular_V;

                
                #ifdef _SPECULARREFINE_ON

                    float f0Refine_u = lerp(specular_D * roughness2, NoV * NoV, _RefineF0U_lerp);
                    float f0Refine_v = roughness * (1 - ao);

                    float4 F0RefineTex_var = SAMPLE_TEXTURE2D(_SpecularRefineF0Tex, sampler_AlbedoTex, float2(f0Refine_u, 1 - f0Refine_v));
                    F0 *= F0RefineTex_var.xyz;
                #endif

                float specular_DV = specular_D * specular_V - 6.10351562e-05;
                specular_DV = max(0, specular_DV);
                specular_DV = min(20, specular_DV);
                float3 specular_brdf = specular_DV * F0;
                
                float3 specularLight = mainLightColor_final * selfAoShadowEffect * (ao_shadow_lowLight * 0.5 + 0.5);
                float3 mainLightSpeuclarResult = specularLight * specular_brdf;
                mainLightSpeuclarResult *= _SpecularStrength;
                
                
                float diffuseSpecularBlend = lerp(1, mainTex_var.w, _DiffuseBlendEffect);
                float3 mainLightResult = mainDiffuseResult * diffuseSpecularBlend + mainLightSpeuclarResult;
               
                float mainLightResult_Strength = MyCalculateLightStrength(mainLightResult);
                mainLightResult_Strength = -0.5 + mainLightResult_Strength;
                mainLightResult_Strength = max(0, mainLightResult_Strength);
                mainLightResult_Strength = min(0.5, mainLightResult_Strength);


                float roughness4 = roughness2 * roughness2;
                float roughness6 = roughness4 * roughness2;
                float NoV2 = NoV * NoV;
                float NoV3 = NoV2 * NoV;
                float fit_A = 3.32707 * NoV + 0.0365463;
                float fit_B = -9.04755 * NoV + 9.0632;
                float IBLspecular_brdf1 = fit_A + fit_B * roughness2;

                float fitX = 3.59685 * NoV2 - 1.36772 * NoV3 + 1.0;
                float fitY = 9.22949 * NoV3 - 16.3174 * NoV2 + 9.04401;
                float fitZ = -20.2123 * NoV3 + 19.7886 * NoV2 + 5.56589;
                float3 nvFactors = float3(fitX, fitY, fitZ);
                float IBLspecular_brdf2 = dot(nvFactors, float3(1, roughness2, roughness6));

                float IBLspecular_brdf = IBLspecular_brdf1 / IBLspecular_brdf2;

                
                float scale_fit_part1 = dot(float2(-1.28514, 1.0), float2(NoV, 0.990440011));
                float scale_fit_part2 = dot(float2(1.0, -0.75591), float2(1.29678, NoV));
                float env_scale = dot(float2(scale_fit_part1, scale_fit_part2), float2(1, roughness2));
                float bias_fit_x = dot(float3(2.92338, 59.4188, 1.0), float3(NoV, NoV3, 1.0));
                float bias_fit_y = dot(float3(1.0, -27.0302, 222.592), float3(20.3225, NoV, NoV3));
                float bias_fit_z = dot(float3(626.130, 316.627, 1.0), float3(NoV, NoV3, 121.563004));
                float bias_denominator = dot(float3(bias_fit_x, bias_fit_y, bias_fit_z), float3(1, roughness2, roughness6));
                float env_bias = env_scale / bias_denominator;

                float3 IBLspecular_brdf_final = IBLspecular_brdf * F0 + env_bias;
                float IBLspecular_brdf_final_noF0 = IBLspecular_brdf + env_bias;
                
                
                float directionalAlbedo = IBLspecular_brdf_final_noF0;
                float energyLossFactor = (1.0 - directionalAlbedo) / directionalAlbedo;
                float3 ms_compensation = F0 * energyLossFactor;
                float3 final_ibl_brdf = IBLspecular_brdf_final * (1.0 + ms_compensation);
               
                
                float3 reflectDir = reflect(-viewDir, normalWS);
                reflectDir.x = -reflectDir.x;
                
                reflectDir.z = -reflectDir.z;

                float angle = _EnvRotation * 0.0174532925;
                float s, c;
                sincos(angle, s, c);

                float3 rotatedDir;
                rotatedDir.x = reflectDir.x * c - reflectDir.z * s;
                rotatedDir.y = reflectDir.y;
                rotatedDir.z = reflectDir.x * s + reflectDir.z * c;
                
                float envMap_level = log2(max(0.01, roughness));
                envMap_level = envMap_level * 1.2 + 5.0;
                float3 envMap_color = SAMPLE_TEXTURECUBE_LOD(_EnvMap, sampler_LinearRepeat, rotatedDir, envMap_level);
                envMap_color *= _EnvColor;
                
                
                float3 indirLightSpecular = envMap_color * final_ibl_brdf * _EnvLightStrength * selfAoShadowEffect;
                
                float rimStart = _RimLightArea * - 0.6 + 0.8;
                float rimEnd = _RimLightArea * - 0.4 + 0.9;
                float rimWidth = rimEnd - rimStart;
                float rimt = ((1.0 - NoV) - rimStart) / rimWidth;
                rimt = saturate(rimt);
                float rimArea = rimt * rimt * (3.0 - 2.0 * rimt);
                float3 rimLight = rimArea * _RimLightColor * _RimLightStrength;
                float3 rimLight_effectd = rimLight * min(ao, shadowScene);
                
                
                float3 rimLight_brdf = ((mainDiffuseColor_Light - 0.25) * _RimLightDiffuseColorEffect + 0.25);
                
                float3 rimLightResult = rimLight_brdf * rimLight_effectd;

                
                //mainLightIntensity
                float3 rim_mainLight = lerp(1, mainLightColor * mainLightIntensity, _DayStrength);
                float NoLxz = dot(normalWS, mainLight.direction.xz);
                float NoLxz_refine = 0.5 - (0.5 * NoLxz - 1) * NoLxz;
                NoLxz_refine *= _DayStrength;
                float3 rim_mainLightResult = rim_mainLight * NoLxz_refine;
                float NoV_mask = saturate(5.0 * (0.4 - NoV));
                NoV_mask = MySmoothstep(NoV_mask);
                rim_mainLightResult *= NoV_mask;
                rim_mainLightResult *= min(ao, shadowScene);
                rim_mainLightResult *= max(float3(0.150000006, 0.150000006, 0.150000006), mainDiffuseColor_Light);
                rim_mainLightResult *= _RimLightNoLxzStrength;
                float3 rimLight_finalResult = rimLightResult + rim_mainLightResult;

                
                #ifdef _SPECULARREFINE_ON

                    float4 specularRefineColorTex_var = SAMPLE_TEXTURE2D(_SpecularRefineColorTex, sampler_AlbedoTex, i.uv0);
                    float3 specularRefineResult = specularRefineColorTex_var.xyz * _SpecularRefineColor * _SpecularRefineColorStrength * diffuseSpecularBlend;
                    mainLightResult += specularRefineResult;
                    
                #endif
                
                float3 resultColor = mainLightResult + indirLightSpecular + max(rimLight_finalResult, 0);
               
                float finalAlpha = 1;

                #ifdef _TRANSPARENT_ON
                    finalAlpha = mainTex_var.w * _CustomAlpha;
                    float3 emissionColor = SAMPLE_TEXTURE2D(_EmissionTex, sampler_LinearClamp, i.uv0).xyz;
                    
                    
                    resultColor += _EmissionColor * _EmissionColorStrength * emissionColor * lerp(1, mainTex_var.w, 0.8);
                    
                #endif
                
                float4 finalColor = float4(resultColor, finalAlpha);
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
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
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


            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
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
                    Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
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

    FallBack Off
}
