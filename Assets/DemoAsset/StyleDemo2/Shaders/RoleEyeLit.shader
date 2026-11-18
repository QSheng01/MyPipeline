Shader "ZhongMoDi/RoleEyeLit"
{
    Properties
    {
        [Space(10)]
        [Enum(OFF, 0, FRONT, 1, BACK, 2)] _MainCullMode ("Main Cull Mode", int) = 2
        
        [Space(10)]
        [Header(IsSelfLight)]
        [Toggle(_)] _IsNeedSelfLight ("是否需要调试白光", Float) = 0
        _SelfLightIntensity ("SelfLightIntensity", Float) = 3
        
        [Space(10)]
        [Header(Facial)]
        [Toggle(_FACIAL_ON)]_FacialFeature ("FacialFeature", Float) = 0
        
        [Space(10)]
        [Header(MainTextures)]
        _AlbedoTex ("AlbedoTex", 2D) = "white" { }
        _BaseColor ("BaseColor", Color) = (1, 1, 1, 1)
        _BaseColorPow ("BaseColorPow", Range(1, 2)) = 1
        _RampTex ("RampTex", 2D) = "white" { }
        _RampColorNoLStrength ("RampColorNoLStrength", Range(0, 1)) = 1
        _SpecularMatcap ("SpecularMatcap", 2D) = "white" { }
        _LutColorTex ("LutColorTex", 2D) = "white" { }
        
        [Space(10)]
        [Header(Parallax)]
        _ParallaxDepth ("ParallaxDepth", Range(0, 1)) = 1
        _CorneaBumpStrength ("CorneaBumpStrength", Range(0, 10)) = 1
        
        [Space(10)]
        [Header(Trick)]
        _SpecularTrickColor ("SpecularTrickColor", Color) = (1, 1, 1, 1)
        _EyeInTrickColor ("EyeInTrickColor", Color) = (1, 1, 1, 1)
        
        
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
        _DiffuseDarkStrength ("DiffuseDarkStrength(暗部程度)", Range(0.01, 1)) = 1
        _DayStrength ("DayStrength", Range(0, 1)) = 1
        _NoLOffset ("NoLOffset", Range(-1, 1)) = 0
        _MainDiffuseColorLightStrength ("MainDiffuseColorLightStrength", Range(1, 2)) = 1
        
        [Space(10)]
        [Header(Specular)]
        _SpecularStrength ("SpecularStrength", Range(0, 2)) = 1
        _SpecularColor ("SpecularColor", Color) = (1, 1, 1, 1)
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
        [Header(Ao)]
        _AoStrength ("AoStrength", Range(1, 3)) = 1
        _SelfAoShadowStrength ("SelfAoShadowStrength", Range(0, 1)) = 1
        
        [Space(10)]
        [Header(BackLight)]
        _BackLightStrength ("BackLightStrength", Range(0, 1)) = 1
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
            #pragma vertex Vert
            #pragma fragment Frag

            // 关键字
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FACIAL_ON


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
                float2 uv1 : TEXCOORD7;
                float2 uv2 : TEXCOORD8;
            };

            // 贴图变量管理
            TEXTURE2D(_AlbedoTex); SAMPLER(sampler_AlbedoTex); float4 _AlbedoTex_ST;
            TEXTURE2D(_SpecularMatcap); SAMPLER(sampler_SpecularMatcap);
            TEXTURE2D(_RampTex); SAMPLER(sampler_RampTex);
            TEXTURE2D(_LutColorTex); SAMPLER(sampler_LutColorTex);

            CBUFFER_START(UnityPerMaterial)
                // sphere normal
                float4 _FaceCenter;
                // face dir
                // float4 _FaceForward;
                // float4 _FaceRight;
                // float4 _FaceUp;

                // parallax
                float _ParallaxDepth;
                float _CorneaBumpStrength;

                // light
                half _IsNeedSelfLight;
                float _SelfLightIntensity;

                // albedo
                float4 _BaseColor;
                float _BaseColorPow;
                float _RampColorNoLStrength;

                // PBR
                float _ReflectivityStrength;
                float _Roughness;

                // mainLight
                float3 _MainLightColor_dark;

                // trick
                float3 _SpecularTrickColor;
                float3 _EyeInTrickColor;

                // otherLight
                float3 _OtherLightColor;
                float _OtherLightOffset;
                float _OtherLightStrength;
                float _OtherLightStrength_Offset;
                float _OtherLightResultStrength_day1;
                float _OtherLightResultStrength_day0;

                // ao
                float _AoStrength;
                float _SelfAoShadowStrength;

                // DiffuseColorLightAndDark
                float _AlbedoDarkStrength;
                float _AlbedoDarkSaturation;

                // Diffuse
                float _DiffuseDarkStrength;
                float _DayStrength;
                float _NoLOffset;
                float _MainDiffuseColorLightStrength;

                // specular
                float _SpecularStrength;
                float _DiffuseBlendEffect;
                float3 _SpecularColor;

                // backLight
                float _BackLightStrength;

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
                o.uv1 = v.texcoord1;
                OUTPUT_LIGHTMAP_UV(v.staticLightmapUV, unity_LightmapST, o.staticLightmapUV)
                OUTPUT_SH(o.normalWS.xyz, o.vertexSH);
                
                o.uv2 = v.texcoord2;
                
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

            float4 Frag(VertexOutput input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
               
                float3 color = float3(0, 0, 0);

                Light mainLight = GetMainLight();
                float3 mainLightColor = mainLight.color;
                float mainLightIntensity = max(0.001, (0.299 * mainLightColor.r + 0.587 * mainLightColor.g + 0.114 * mainLightColor.b));
                mainLightColor = mainLightColor / mainLightIntensity;
                
                mainLightColor = lerp(mainLightColor, float3(1, 1, 1), _IsNeedSelfLight);
                mainLightIntensity = lerp(mainLightIntensity, _SelfLightIntensity, _IsNeedSelfLight);
                mainLightIntensity *= 1.6 / 1.6;

                float mainLightColorStrength = MyCalculateLightStrength(mainLightColor * _MainLightColor_dark);
                float3 mainLightDir = _MainLightPosition;

                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - input.posWS.xyz);
                float3 cameraForward = UNITY_MATRIX_V[2].xyz;
                
                float3 worldUp = float3(0, 1, 0);
                float3 cameraRight = cross(worldUp, cameraForward);
                cameraRight = normalize(cameraRight + float3(1e-6, 0, 0));

                float2 fracUV = frac(input.uv0);
                float2 eyeCenterAreaUV = fracUV - float2(0.5, 0.5);
                float eyeCenterArea = step(0.25, dot(eyeCenterAreaUV, eyeCenterAreaUV));
                float distSq = dot(eyeCenterAreaUV, eyeCenterAreaUV);
                
                float3 tangentWS = normalize(input.tangenWS);
                float3 bitangentWS = normalize(input.bitangenWS);
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirTS;
                viewDirTS.x = dot(tangentWS, viewDir);
                viewDirTS.y = dot(bitangentWS, viewDir);
                viewDirTS.z = dot(normalWS, viewDir);

                float2 offsetDir = normalize(viewDirTS).xy;
                float2 parallaxOffset = offsetDir * _ParallaxDepth * float2(1.0, 0.25);
                float maskRaw = saturate(-5.0 * (distSq - 0.25));
                float parallaxMask = maskRaw * maskRaw * (3.0 - 2.0 * maskRaw);
                float2 finalEyeUV = input.uv0 - parallaxOffset * parallaxMask;

                #ifdef _FACIAL_ON
                    finalEyeUV = input.uv0;
                #endif


                float4 mainTex_var = SAMPLE_TEXTURE2D(_AlbedoTex, sampler_AlbedoTex, finalEyeUV);
              
                float3 albedo = mainTex_var.xyz * _BaseColor;
                float3 albedo_dark = albedo * _AlbedoDarkStrength;
                
                float2 centeredUV = eyeCenterAreaUV * 2.0;
                float uvSq = dot(centeredUV, centeredUV);
                float zHemi = sqrt(max(0.0, 1.0 - min(1.0, uvSq)));
                zHemi = max(1e-16, zHemi);

                float2 corneaTS_xy = 0.125 * _CorneaBumpStrength * centeredUV;
                float3 corneaNormalTS = float3(corneaTS_xy, zHemi);
                
                corneaNormalTS = lerp(corneaNormalTS, float3(0, 0, 1), eyeCenterArea);
                corneaNormalTS.x = -corneaNormalTS.x;

                float3 customNormalWS = corneaNormalTS.x * tangentWS +
                corneaNormalTS.y * bitangentWS +
                corneaNormalTS.z * normalWS;
                float3 corneaNormalWS = normalize(customNormalWS);
                
                float metallic = 0;
                float energyDistribution_metallic = 0.96 - 0.96 * metallic;
                float3 mainDiffuseColor_Light = albedo * energyDistribution_metallic * _MainDiffuseColorLightStrength;
                float3 mainDiffuseColor_dark = albedo_dark * energyDistribution_metallic;
             
                #ifdef _FACIAL_ON
                    
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
                    mainDiffuseColor_dark = albedo_lutRefine * energyDistribution_metallic * _DiffuseDarkStrength;

                    corneaNormalWS = input.normalWS;
                #endif
                 

                float3 _FaceForward = normalize(TransformObjectToWorld(float3(0, 0, 1)));
                float3 _FaceRight = normalize(TransformObjectToWorld(float3(1, 0, 0)));
                float3 _FaceUp = normalize(cross(_FaceForward, _FaceRight));
                float3 _FaceCenter = TransformObjectToWorld(float3(0, 0, 0));

                float3 lightDirFS;
                lightDirFS.x = dot(mainLightDir, _FaceRight);
                lightDirFS.y = dot(mainLightDir, _FaceUp);
                lightDirFS.z = dot(mainLightDir, _FaceForward);
                float2 lightDirFS_XZ = normalize(lightDirFS.xz + float2(0.0, 1e-6));
                float3 flattenedLightDirWS = _FaceRight * lightDirFS_XZ.x + _FaceForward * lightDirFS_XZ.y;
                flattenedLightDirWS = normalize(flattenedLightDirWS);
                
                float NoL = dot(corneaNormalWS, flattenedLightDirWS) + _NoLOffset;
                NoL = max(NoL, -1);
                NoL = min(NoL, 1);
                float2 rampUV = float2(NoL * 0.5 + 0.5, 0.5);
                float4 rampTex_var = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, rampUV);
                float3 rampColor = rampTex_var.xyz;
                float rampNoL = rampTex_var.w;
                float rampColor_max = max(max(rampColor.x, rampColor.y), rampColor.z);
                float rampColor_min = min(min(rampColor.x, rampColor.y), rampColor.z);
                float rampColor_xyzStrength = rampColor_max - rampColor_min;
                
                float NoF = dot(corneaNormalWS, cameraForward);
                float2 rampUV_NoF = float2(NoF * 0.5 + 0.5, 0.5);
                float rampTex_NoF = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, rampUV_NoF).w;
                
                float3 specularTrickColor = lerp(1, _SpecularTrickColor * 2.5, eyeCenterArea);
                float3 eyeInTrickColor = lerp(1, _EyeInTrickColor * 2.5, mainTex_var.w);
                float3 trickAlbedo = specularTrickColor * eyeInTrickColor;
          
                #ifdef _FACIAL_ON

                    trickAlbedo = 1;
                    
                #endif
                
                float otherLightNoL = 0;
                otherLightNoL = saturate(otherLightNoL + _OtherLightOffset);
                otherLightNoL = otherLightNoL * _OtherLightStrength + _OtherLightStrength_Offset;
                float3 otherLightColor_dark = _OtherLightColor;
                float3 otherLightColor = lerp(otherLightColor_dark, float3(1, 1, 1), rampNoL);
                float3 otherLightResult = otherLightColor * otherLightNoL;
                float3 otherLightResult_day1 = otherLightResult * _OtherLightResultStrength_day1;
                float3 otherLightResult_day0 = otherLightResult * _OtherLightResultStrength_day0;

                float3 mainLightColor_new = lerp(mainLightColorStrength.xxx, mainLightColor, rampNoL) * mainLightIntensity;
                float3 mainLightColor_final = lerp(otherLightResult_day0, mainLightColor_new + otherLightResult_day1, _DayStrength);
                
                float3 mainDiffuseColor_darkindark = mainDiffuseColor_dark * 0.65;
                float mainDiffuseColor_Lgiht_Strength = MyCalculateLightStrength(mainDiffuseColor_Light);
                float mainDiffuseColor_Dark_Strength = MyCalculateLightStrength(mainDiffuseColor_darkindark);
                float3 mainDiffuseColor_Dark_final = lerp(mainDiffuseColor_Dark_Strength.xxx, mainDiffuseColor_darkindark, 1.2);
                float3 mainDiffuseColor_Dark_lerp = lerp(mainDiffuseColor_Dark_final, mainDiffuseColor_dark, saturate(rampTex_NoF + rampNoL));
                float3 mainDiffuseBrdf = lerp(mainDiffuseColor_Dark_lerp, mainDiffuseColor_Light * trickAlbedo, rampNoL);
  
                float3 rampColor_xyzEffect = rampColor * rampColor_xyzStrength + 1 - rampColor_xyzStrength;
                float3 mainDiffuseBrdf_rampColor = mainDiffuseBrdf * rampColor_xyzEffect;
                float3 mainDiffuseColor_Light_final = lerp(mainDiffuseColor_Lgiht_Strength.xxx, mainDiffuseColor_Light, 1.2);
                float3 mainDiffuseBrdf_lowLight = lerp(mainDiffuseColor_dark, mainDiffuseColor_Light_final, rampTex_NoF);
                float3 mainDiffuseBrdf_final = lerp(mainDiffuseBrdf_lowLight, mainDiffuseBrdf_rampColor, _DayStrength);
                
                float3 mainDiffuseResult = mainLightColor_final * mainDiffuseBrdf_final;

                float3 mainSpecularResult = 0;
                #ifndef _FACIAL_ON
                    float DayDarkEffect = lerp(rampTex_NoF, rampNoL, _DayStrength);

                    
                    float3 corneaNormalVS = mul((float3x3)UNITY_MATRIX_V, corneaNormalWS);
                    corneaNormalVS = normalize(corneaNormalVS);
                    float2 matcapUV = corneaNormalVS.xy * 0.5 + 0.5;
                    
                    float4 specularMatcapVar = SAMPLE_TEXTURE2D(_SpecularMatcap, sampler_SpecularMatcap, matcapUV);
                    float3 specularColor = specularMatcapVar.xyz;
                    float specularW = specularMatcapVar.w;

                    
                    float3 specularBrdf = specularColor * _SpecularStrength + _SpecularColor * specularW;
                    
                    float specularDarkEffect = DayDarkEffect * 0.5 + 0.5;
                    specularDarkEffect *= lerp(_SelfAoShadowStrength, 1, DayDarkEffect);
                    
                    mainSpecularResult = mainLightColor_final * specularBrdf * specularDarkEffect;
                    
                #endif

                 
                float3 mainLightResult = mainSpecularResult + mainDiffuseResult * lerp(1, mainTex_var.w, _DiffuseBlendEffect);
              
                float4 finalColor = float4(mainLightResult, 1);
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
