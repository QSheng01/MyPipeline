Shader "ZhongMoDi/RoleFaceLit"
{
    Properties
    {
        [Space(10)]
        [Enum(OFF, 0, FRONT, 1, BACK, 2)] _MainCullMode ("Main Cull Mode", int) = 2
         _StencilRef ("_StencilRef", Range(0, 255)) = 0
        
        [Space(10)]
        [Header(IsSelfLight)]
        [Toggle(_)] _IsNeedSelfLight ("是否需要调试白光", Float) = 0
        _SelfLightIntensity ("SelfLightIntensity", Float) = 3
        
        [Space(10)]
        [Header(MainTextures)]
        _AlbedoTex ("AlbedoTex", 2D) = "white" { }
        _BaseColor ("BaseColor", Color) = (1, 1, 1, 1)
        _BaseColorPow ("BaseColorPow", Range(1, 2)) = 1
        _RampTex ("RampTex", 2D) = "white" { }
        _RampColorNoLStrength ("RampColorNoLStrength", Range(0, 1)) = 1
        _LutColorTex ("LutColorTex", 2D) = "white" { }
        
        [Space(10)]
        [Header(Sdf)]
        _SdfTex ("SdfTex", 2D) = "white" { }
        _SdfRefineTex ("SdfRefineTex", 2D) = "white" { }
        
        [Space(10)]
        [Header(Trick)]
        _TrickTex ("TrickTex", 2D) = "white" { }
        [Enum(emoji0, 0, emoji1, 1, emoji2, 2, emoji3, 3)] _TrickType ("TrickType", int) = 0
        _TrickStrength ("TrickStrength", Range(0, 1)) = 1
        
        [Space(10)]
        [Header(SSS)]
        _SSSColor ("SSSColor", Color) = (1, 1, 1, 1)
        _SSSArea ("SSSArea", Range(0, 1)) = 0.5
        
        [Space(10)]
        [Header(PBR)]
        _ReflectivityStrength ("ReflectivityStrength", Range(0.01, 2)) = 1
        _Roughness ("Roughness", Range(0, 1)) = 0.5
        
        [Space(10)]
        [Header(DiffuseColorLightAndDarkInAlbedo)]
        _AlbedoDarkStrength ("AlbedoDarkStrength(暗部albedo)", Range(0.01, 1)) = 0.8
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
        [Header(Rim(frenesel)]
        _ForwardDirStrength ("ForwardDirStrength(view dir是否偏移)", Range(0, 1)) = 0
        _RimLightArea ("RimLightArea", Range(0, 1)) = 1
        _RimLightColor ("RimLightColor", Color) = (1, 1, 1, 1)
        _RimLightStrength ("RimLightStrength", Range(0, 2)) = 1
        _RimLightDiffuseColorEffect ("RimLightDiffuseColorEffect", Range(0, 1)) = 0.1
        _RimMaskStrength ("RimMaskStrength", Range(0, 1)) = 1
        
        [Space(10)]
        [Header(Rim(NoLxz)]
        _RimLightNoLxzStrength ("RimLightNoLxzStrength", Range(0, 1)) = 0.8
        
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
        _ZBias ("ZBias", Range(0, 0.2)) = 0.0
        _OutlineColor ("OutlineColor", Color) = (1, 1, 1, 1)
        _OutLineStrength ("OutLineStrength", Range(0, 2)) = 0.8
        _ZMinRefine ("ZMinRefine", Range(0, 1)) = 0.4
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 100
        
        Stencil
        {
            Ref [_StencilRef]
            Comp Always     // 总是通过模板测试
            Pass Replace    // 写入参考值到模板缓冲区，替换掉原来的值

        }
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
                float3 tangentWS : TEXCOORD4;
                float3 bitangentWS : TEXCOORD5;
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 6);
                float2 uv1 : TEXCOORD7;
                float2 uv2 : TEXCOORD8;
            };

            // 贴图变量管理
            TEXTURE2D(_AlbedoTex); SAMPLER(sampler_AlbedoTex); float4 _AlbedoTex_ST;
            TEXTURE2D(_LutColorTex); SAMPLER(sampler_LutColorTex);
            TEXTURE2D(_RampTex); SAMPLER(sampler_RampTex);
            TEXTURE2D(_SdfTex); SAMPLER(sampler_SdfTex);
            TEXTURE2D(_TrickTex); SAMPLER(sampler_TrickTex);
            TEXTURE2D(_SdfRefineTex); SAMPLER(sampler_SdfRefineTex);

            CBUFFER_START(UnityPerMaterial)
                // emoji trick
                float _TrickType;
                float _TrickStrength;

                // sphere normal
                //float4 _FaceCenter;
                // face dir
                // float4 _FaceForward;
                // float4 _FaceRight;
                // float4 _FaceUp;

                // light
                half _IsNeedSelfLight;
                float _SelfLightIntensity;

                // albedo
                float4 _BaseColor;
                float _BaseColorPow;
                float _RampColorNoLStrength;

                // SSS
                float3 _SSSColor;
                float _SSSArea;

                // PBR
                float _ReflectivityStrength;
                float _Roughness;

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

                float _RimMaskStrength;

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

                // backLight
                float _BackLightStrength;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.uv0 = input.texcoord0;
                output.uv0.y = 1 - input.texcoord0.y;
                output.pos = TransformObjectToHClip(input.vertex.xyz);
                output.posWS = mul(GetObjectToWorldMatrix(), input.vertex);
                output.posCS = TransformWorldToHClip(output.posWS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normal);
                output.tangentWS = normalize(mul(GetObjectToWorldMatrix(), float4(input.tangent.xyz, 0.0)).xyz);
                output.bitangentWS = normalize(cross(output.normalWS, output.tangentWS) * input.tangent.w * unity_WorldTransformParams.w);
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

            float3 linear2sRGB(float3 color)
            {
                // 1. 处理线性段：对于极暗区域，sRGB 曲线是一条斜率为 12.92 的直线
                float3 linearSegment = color * 12.9200001;
                // 2. 处理曲线段：公式为 1.055 * color^(1/2.4) - 0.055
                // 注意：代码中使用了 abs 防止 log2 负数报错
                float3 exponent = log2(abs(color));
                exponent *= 0.416666657; // 约等于 1/2.4
                float3 curveSegment = exp2(exponent);
                curveSegment = curveSegment * 1.05499995 - 0.0549999997;

                // 3. 分段选择：如果原始颜色 <= 0.0031308，使用线性段，否则使用曲线段
                // 对应原代码中的 cmp(0.0031308 >= r4.zxy) 和 r7.xyz ? r5.xyz : r6.xyz
                bool3 isLinear = color <= 0.00313080009;
                
                float3 result;
                result.x = isLinear.x ? linearSegment.x : curveSegment.x;
                result.y = isLinear.y ? linearSegment.y : curveSegment.y;
                result.z = isLinear.z ? linearSegment.z : curveSegment.z;

                return result;
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
                float3 albedo = mainTex_var.xyz * _BaseColor;
               
                float4 sdfRefineTex_var = SAMPLE_TEXTURE2D(_SdfRefineTex, sampler_SdfRefineTex, input.uv0);
                
                
                float emojiType = _TrickType * 0.5;
                float2 emojiUV = float2(frac(abs(emojiType)), floor(emojiType) * 0.5);
                float4 trickTex_var = SAMPLE_TEXTURE2D(_TrickTex, sampler_TrickTex, input.uv0 * 0.5 + emojiUV);
                float3 trickColor = trickTex_var.xyz ;

                albedo = lerp(albedo, trickColor, trickTex_var.w * _TrickStrength);

                Light mainLight = GetMainLight();
                float3 mainLightColor = mainLight.color;
                float mainLightIntensity = max(0.001, (0.299 * mainLightColor.r + 0.587 * mainLightColor.g + 0.114 * mainLightColor.b));
                mainLightColor = mainLightColor / mainLightIntensity;

                
                mainLightColor = lerp(mainLightColor, float3(1, 1, 1), _IsNeedSelfLight);
                mainLightIntensity = lerp(mainLightIntensity, _SelfLightIntensity, _IsNeedSelfLight);
                mainLightIntensity *= 1.6 / 1.6;

                float mainLightColorStrength = MyCalculateLightStrength(mainLightColor * _MainLightColor_dark);
                float3 mainLightDir = _MainLightPosition;
                float3 mainLightDir_xz = normalize(float3(mainLightDir.x, 6.10351562e-05, mainLightDir.z));


                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - input.posWS.xyz);
                float3 halfDir = normalize(viewDir + mainLightDir);
                
                float3 cameraForward = UNITY_MATRIX_V[2].xyz;
                cameraForward = normalize(cameraForward);
                viewDir = lerp(viewDir, cameraForward, _ForwardDirStrength);
                viewDir = normalize(viewDir);
                
                float3 worldUp = float3(0, 1, 0);
                float3 cameraRight = cross(worldUp, cameraForward);
                cameraRight = normalize(cameraRight + float3(1e-6, 0, 0));
                
                
                float facing = isFrontFace ? 1.0 : - 1.0;
                float3 _FaceForward = normalize(TransformObjectToWorld(float3(0, 0, 1)));
                float3 _FaceRight = normalize(TransformObjectToWorld(float3(1, 0, 0)));
                float3 _FaceUp = normalize(cross(_FaceForward, _FaceRight));
                float3 _FaceCenter = TransformObjectToWorld(float3(0, 0, 0));



                
                float3 sphereNormal = normalize(input.posWS.xyz - _FaceCenter.xyz);
                float3 sphereDir = normalize(float3(sphereNormal.x, 6.10351562e-05, sphereNormal.z));
                
                float3 objnormal_xz = normalize(float3(input.normalWS.x, 6.10351562e-05, input.normalWS.z));
                float3 normalWS_xz = lerp(sphereDir, objnormal_xz, sdfRefineTex_var.y);

                
                float3 cameraForward_faceDir = normalize(float3(dot(cameraForward, _FaceRight), dot(cameraForward, _FaceUp), dot(cameraForward, _FaceForward)));
                float headFDotCamerF = cameraForward_faceDir.z * rsqrt(dot(cameraForward_faceDir.xz, cameraForward_faceDir.xz));
                
                
                float view_sssStrength = lerp(saturate(headFDotCamerF + 0.5), 1, sdfRefineTex_var.y) * sdfRefineTex_var.x;
                
                
                float NoV = dot(input.normalWS, viewDir);
                float sss_NoV = saturate(NoV);
                sss_NoV = sss_NoV * 0.85 + 0.15;
                float sss_area = saturate(_SSSArea * view_sssStrength * (1.0 - sss_NoV));
                
                float3 sssColorEffect = lerp(float3(1.0, 1.0, 1.0), _SSSColor, sss_area);
                float3 albedo_sssRefine = albedo * sssColorEffect;
                
                float metallic = 0;
                float energyDistribution_metallic = 0.96 - 0.96 * metallic;
                float3 mainDiffuseColor_Light = albedo_sssRefine * energyDistribution_metallic;
                float reflectivity = 0.04 * _ReflectivityStrength;
                float3 F0 = float3(1, 1, 1) * reflectivity;
                float roughness2 = max(0.0078125, _Roughness * _Roughness);
                
                
                float3 albedo_sRGB = linear2sRGB(albedo.brg);
                float2 lut_uv = albedo_sRGB.xz * float2(31, 0.96875);
                float lut_uv_floorx = floor(lut_uv.x);
                float2 lut_uv_yz = albedo_sRGB.yz * float2(0.0302734375, 0.96875) + float2(0.00048828125, 0.015625);
                float lut_uv_x = lut_uv_floorx * 0.03125 + lut_uv_yz.x;
                float2 lut_uv_final = float2(lut_uv_x, 1 - lut_uv_yz.y);
                
                float lutTex_lerp = albedo_sRGB.x * 31 - lut_uv_floorx;
                float3 lutTex_var1 = SAMPLE_TEXTURE2D(_LutColorTex, sampler_LutColorTex, lut_uv_final).xyz;
                float3 lutTex_var2 = SAMPLE_TEXTURE2D(_LutColorTex, sampler_LutColorTex, lut_uv_final + float2(0.03125, 0.015625)).xyz;

                float3 albedo_lutRefine = lerp(lutTex_var1, lutTex_var2, lutTex_lerp);
                
                float3 mainDiffuseColor_dark = albedo_lutRefine * energyDistribution_metallic * _AlbedoDarkStrength;
                
                
                float3 mainDiffuseColor_darkindark = mainDiffuseColor_dark * 0.65;
             

                float3 mainLightDir_xz_faceDir = normalize(float3(dot(mainLightDir, _FaceRight), 6.10351562e-05, dot(mainLightDir, _FaceForward)));
                float sdf_uvFlag = step(0, mainLightDir_xz_faceDir.x);
                
                float sdf_u = sdf_uvFlag * (2 * input.uv0.x - 1) + 1 - input.uv0.x;
                
                float sdf_v = input.uv0.y;
                float4 sdfTex_var = SAMPLE_TEXTURE2D(_SdfTex, sampler_SdfTex, float2(sdf_u, sdf_v));
                
                float faceNoL_compensation = -0.5 * mainLightDir_xz_faceDir.z * mainLightDir_xz_faceDir.z + 0.5;
                
                
                float2 cameraForward_xz = normalize(cameraForward.xz);
                float backLight = saturate(-dot(cameraForward_xz, mainLightDir_xz.xz)) * saturate(-mainLightDir_xz_faceDir.z);
                float faceNoL = mainLightDir_xz_faceDir.z + backLight * faceNoL_compensation;
                float halfFaceNoL = faceNoL * 0.5;
                   
                faceNoL = saturate(-faceNoL * 0.5 + 0.5);

                
                float sdf_min = max(0, 2 * faceNoL - 1);
                float sdf_width = min(1, 2 * faceNoL) - sdf_min;
                float sdf_smoothVar = saturate((0.5 * (sdfTex_var.x + sdfTex_var.y) - sdf_min) / sdf_width);
                float sdf_backLight = halfFaceNoL * ceil(halfFaceNoL);
                sdf_smoothVar = sdf_smoothVar * sdf_smoothVar * (3 - 2 * sdf_smoothVar);
                float sdf_NoL = abs(-sdf_smoothVar - sdf_backLight) * 2 - 1;
            

                float NoL = dot(input.normalWS, mainLightDir);
                float ramp_NoL = lerp(sdf_NoL, NoL, sdfRefineTex_var.y);
                float ramp_u = ramp_NoL * 0.5 + 0.5;
                float ramp_v = 0.5;
                float4 rampTex_var = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(ramp_u, ramp_v));
                float3 rampColor = rampTex_var.xyz;
                
                float rampColor_max = max(max(rampColor.x, rampColor.y), rampColor.z);
                float rampColor_min = min(min(rampColor.x, rampColor.y), rampColor.z);
                float rampColor_xyzStrength = rampColor_max - rampColor_min;
                
                
                
                float shadowAttenuation = 1;
                float2 screenUV = GetNormalizedScreenSpaceUV(input.pos.xy);
                shadowAttenuation = SAMPLE_TEXTURE2D(_ScreenSpaceShadowmapTexture, sampler_PointClamp, screenUV).x;
                // shadowAttenuation = min(shadowAttenuation, SamplePerObjectScreenSpaceShadowmap(screenUV));
                float shadowScene = (SigmoidSharp(shadowAttenuation, _ShadowCenter, _ShadowSmoothness) + _ShadowOffset) * _ShadowStrength ;
                shadowScene = saturate(shadowScene);
                
                float shadowArea = MySmoothstep(saturate(-2 * cameraForward_faceDir.z)) * sdfRefineTex_var.z;
                shadowArea = max(sdfRefineTex_var.y, shadowArea);
                
                float shadowEffect = lerp(1, shadowScene, shadowArea);

                
                float ao = mainTex_var.w;
                float ao_shadow = ao * shadowEffect;
                float min_shadowAoNoLEffect = min(ao, shadowEffect);
                min_shadowAoNoLEffect = min(min_shadowAoNoLEffect, rampTex_var.w);

                float otherLightNoL = 0;
                otherLightNoL = saturate(otherLightNoL + _OtherLightOffset);
                otherLightNoL = otherLightNoL * _OtherLightStrength + _OtherLightStrength_Offset;
                
                float3 otherLightColor_dark = _OtherLightColor;
                float3 otherLightColor = lerp(otherLightColor_dark, float3(1, 1, 1), min_shadowAoNoLEffect);
                float3 otherLightResult = otherLightColor * otherLightNoL;
                
                float3 otherLightResult_day1 = otherLightResult * _OtherLightResultStrength_day1;
                float3 otherLightResult_day0 = otherLightResult * _OtherLightResultStrength_day0;
                
                
                float3 mainLightColor_new = lerp(mainLightColorStrength.xxx, mainLightColor, min_shadowAoNoLEffect) * mainLightIntensity;
                
                float3 mainLightColor_final = lerp(otherLightResult_day0, mainLightColor_new + otherLightResult_day1, _DayStrength);

                 
                float mainDiffuseColor_Lgiht_Strength = MyCalculateLightStrength(mainDiffuseColor_Light);
                float mainDiffuseColor_Dark_Strength = MyCalculateLightStrength(mainDiffuseColor_darkindark);
                
                float3 mainDiffuseColor_Dark_final = lerp(mainDiffuseColor_Dark_Strength.xxx, mainDiffuseColor_darkindark, 1.2);
                
                float3 mainDiffuseColor_Dark_lerp = lerp(mainDiffuseColor_Dark_final, mainDiffuseColor_dark, saturate(ao_shadow + rampTex_var.w));
                
                float3 mainDiffuseBrdf = lerp(mainDiffuseColor_Dark_lerp, mainDiffuseColor_Light, min_shadowAoNoLEffect);
  
                float3 rampColor_xyzEffect = rampColor * rampColor_xyzStrength + 1 - rampColor_xyzStrength;
                float3 mainDiffuseBrdf_rampColor = mainDiffuseBrdf * rampColor_xyzEffect;
                
                float mainDiffuseBrdf_strength = MyCalculateLightStrength(mainDiffuseBrdf);
                float mainDiffuseBrdf_rampColor_strength = MyCalculateLightStrength(mainDiffuseBrdf_rampColor);
                float rampColor_control = mainDiffuseBrdf_strength / max(0.01, mainDiffuseBrdf_rampColor_strength);
                rampColor_control = max(0, rampColor_control);
                rampColor_control = min(1.5, rampColor_control);
                
                float3 mainDiffuseColor_Light_final = lerp(mainDiffuseColor_Lgiht_Strength.xxx, mainDiffuseColor_Light, 1.2);
                
                float3 mainDiffuseBrdf_lowLight = lerp(mainDiffuseColor_dark, mainDiffuseColor_Light_final, ao_shadow);
                
                float3 mainDiffuseBrdf_final = lerp(mainDiffuseBrdf_lowLight, mainDiffuseBrdf_rampColor * rampColor_control, _DayStrength);
                
                
                float3 mainDiffuseResult = mainLightColor_final * mainDiffuseBrdf_final;

                float ao_shadow_lowLight = lerp(ao_shadow, min_shadowAoNoLEffect, _DayStrength);
                float selfAoShadowEffect = lerp(_SelfAoShadowStrength, 1, ao_shadow_lowLight);

                
                float forwardLightDir_y = lerp(0.5, mainLightDir.y, _DayStrength);
                float3 forwardLightDir = normalize(float3(cameraForward.x, forwardLightDir_y, cameraForward.z));
                float3 mainLightDir_new = mainLightDir * _DayStrength + 2 * forwardLightDir;
                float3 halfDir_new = viewDir * (2 + _DayStrength) + mainLightDir_new;
                halfDir_new = normalize(halfDir_new);
                float NoH = dot(input.normalWS, halfDir_new);

                
                
                float a2 = roughness2 * roughness2;
                float specular_D = (NoH * a2 - NoH) * NoH + 1;
                specular_D *= specular_D;
                specular_D = a2 / specular_D;

                
                float specular_V = NoV * 2 + roughness2 + 9.99999975e-05;
                specular_V = 0.5 / specular_V;

                
                float specular_DV = specular_D * specular_V - 6.10351562e-05;
                specular_DV = max(0, specular_DV);
                specular_DV = min(20, specular_DV);
                float3 specular_brdf = specular_DV * F0;
                
                float3 specularLight = mainLightColor_final * selfAoShadowEffect * (ao_shadow_lowLight * 0.5 + 0.5);
                float testEnergyArea = selfAoShadowEffect * (ao_shadow_lowLight * 0.5 + 0.5);
                float3 mainLightSpeuclarResult = specularLight * specular_brdf;
                mainLightSpeuclarResult *= _SpecularStrength;

                
                float3 mainLightResult = mainDiffuseResult + mainLightSpeuclarResult;
               
                
                float mainLightResult_Strength = MyCalculateLightStrength(mainLightResult);
                mainLightResult_Strength = -0.5 + mainLightResult_Strength;
                mainLightResult_Strength = max(0, mainLightResult_Strength);
                mainLightResult_Strength = min(0.5, mainLightResult_Strength);

                
                float sdf_z = lerp( - (sdfTex_var.z * 2 - 1), sdfTex_var.z * 2 - 1, sdf_uvFlag);
                float3 sdfDir = float3(sdf_z, 6.10351562e-05, 1 - abs(sdf_z));
                sdfDir = normalize(sdfDir);
                
                float3 faceSpace_x = float3(_FaceRight.x, _FaceUp.x, _FaceForward.x);
                float3 faceSpace_y = float3(_FaceRight.y, _FaceUp.y, _FaceForward.y);
                float3 faceSpace_z = float3(_FaceRight.z, _FaceUp.z, _FaceForward.z);
                float3 sdfNormal = normalize(float3(dot(faceSpace_x, sdfDir), dot(faceSpace_y, sdfDir), dot(faceSpace_z, sdfDir)));
                float3 normalWS = lerp(sdfNormal, input.normalWS, sdfRefineTex_var.y);
                
                
                
                float rim_NoV = dot(normalWS, viewDir);
                   
                float headFDotCamerF_rim = saturate(-0.9 + headFDotCamerF * 10);
                headFDotCamerF_rim = MySmoothstep(headFDotCamerF_rim);
                
                float rimStart = _RimLightArea * - 0.6 + 0.8;
                float rimEnd = _RimLightArea * - 0.4 + 0.9;
                float rimWidth = rimEnd - rimStart;
                float rimt = ((1.0 - rim_NoV) - rimStart) / rimWidth;
            
                rimt = saturate(rimt);
                float rimArea = rimt * rimt * (3.0 - 2.0 * rimt);
     
                
                float rimArea_mask = sdfRefineTex_var.w;
            
                float rimArea_final = lerp(rimArea, rimArea_mask, _RimMaskStrength);
           
                float3 rimLight = rimArea_final * 1 * _RimLightColor * _RimLightStrength;
            //return float4(headFDotCamerF_rim.xxx, 1);
                float3 rimLight_effectd = rimLight * min(ao, shadowScene);
                
                
                float3 rimLight_brdf = ((mainDiffuseColor_Light - 0.25) * _RimLightDiffuseColorEffect + 0.25);
                
                float camSideFlag = step(0, dot(viewDir, _FaceRight));
                float3 targetRimDir = lerp(_FaceRight, -_FaceRight, camSideFlag);
                float rimHalfArea = saturate(dot(cameraRight, normalWS));
                float3 rimLightResult = rimLight_brdf * rimLight_effectd * rimHalfArea;

                float3 resultColor = rimLightResult + mainLightResult;
                
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
}
