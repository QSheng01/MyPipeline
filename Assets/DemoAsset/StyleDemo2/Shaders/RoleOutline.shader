Shader "ZhongMoDi/RoleOutline"
{
    Properties
    {
        _AlbedoTex ("Texture", 2D) = "white" { }
        _NormalTex ("NormalTex", 2D) = "white" { }
        _BumpScale ("BumpScale", Range(0, 2)) = 1
        _RampTex ("RampTex", 2D) = "white" { }
        _OutlineWidth ("Width", Range(0, 10)) = 1.0
        _ZBias ("ZBias", Range(0, 0.2)) = 0.0
        _DayStrength ("DayStrength", Range(0, 1)) = 1
        _AlbedoDarkStrength ("AlbedoDarkStrength", Range(0.01, 1)) = 0.8
        _OtherLightColor ("OtherLightColor(暗部颜色)", Color) = (1, 1, 1, 1)
        _OtherLightResultStrength_day1 ("OtherLightResultStrength_Day1", Range(0, 1)) = 0.3
        _OtherLightResultStrength_day0 ("OtherLightResultStrength_Day0", Range(0, 1)) = 0.7
        
        _OutlineColor ("OutlineColor", Color) = (1, 1, 1, 1)
        _OutLineStrength ("OutLineStrength", Range(0, 2)) = 1
        _ZMinRefine ("ZMinRefine", Range(0, 1)) = 0.4
        
        [Space(10)]
        [Header(IsSelfLight)]
        [Toggle(_)] _IsNeedSelfLight ("是否需要调试白光", Float) = 0
        _SelfLightIntensity ("SelfLightIntensity", Float) = 3
        
        [Header(IsHair)]
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

         
            ZWrite On

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex ToonOutlineVert
            #pragma fragment ToonOutlineFrag

            #pragma multi_compile _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _GPU_LIGHTS_CLUSTER
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


            struct Attributes
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 uv : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_AlbedoTex);
            SAMPLER(sampler_AlbedoTex);
            TEXTURE2D(_NormalTex); SAMPLER(sampler_NormalTex);
            TEXTURE2D(_RampTex); SAMPLER(sampler_RampTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _AlbedoTex_ST;
                float4 _NormalTex_ST;
                float4 _RampTex_ST;
                float _OutlineWidth;
                float _ZBias;
                float _AlbedoDarkStrength;
                float _DayStrength;
                float3 _OutlineColor;
                float _OutLineStrength;
                float _ZMinRefine;
                float _BumpScale;
                
                half _IsNeedSelfLight;
                float _SelfLightIntensity;
                float3 _OtherLightColor;
                float _OtherLightResultStrength_day1;
                float _OtherLightResultStrength_day0;

                
                half _IsHair;

            CBUFFER_END

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

            Varyings ToonOutlineVert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float4 scaledScreenParams = GetScaledScreenParams();
                float ScaleX = abs(scaledScreenParams.x / scaledScreenParams.y);

                o.positionHCS = TransformObjectToHClip(v.vertex.xyz);
                o.positionWS = TransformObjectToWorld(v.vertex.xyz);

                float3 tangentOS = v.tangent.xyz;
                float3 normalOS = v.normal.xyz;
                float3 biTangentOS = cross(normalOS, tangentOS) * v.tangent.w * GetOddNegativeScale();
                o.normalWS = TransformObjectToWorldNormal(normalOS);
                o.tangentWS = normalize(mul(GetObjectToWorldMatrix(), float4(v.tangent.xyz, 0.0)).xyz);
                o.bitangentWS = normalize(cross(o.normalWS, o.tangentWS) * v.tangent.w * unity_WorldTransformParams.w);

                float weightN = sqrt(saturate(1.0 - dot(v.uv1.xy, v.uv1.xy)));
                float3 smoothNormalOS = v.uv1.x * v.tangent.xyz + v.uv1.y * biTangentOS + weightN * v.normal;

                float3 smoothNormalVS = mul((float3x3)UNITY_MATRIX_IT_MV, smoothNormalOS);

                
                float2 clipNormal2D = mul((float2x3)UNITY_MATRIX_P, smoothNormalVS);
                
                float2 normal2D = normalize(clipNormal2D);
                normal2D.x /= ScaleX;

                float depthScale = o.positionHCS.w;
                float outlineDepthClamp = min(depthScale, 20.0);

                float depthRefine = lerp(1, _ZMinRefine, smoothstep(1, 12, depthScale));
                
                
                float2 offset = normal2D * (_OutlineWidth * outlineDepthClamp * 0.01 * depthRefine);

               // o.positionHCS.xy += offset;

                // #if defined(UNITY_REVERSED_Z)
                    
                //     o.positionHCS.z -= _ZBias * 1;
                // #else
                    
                //     o.positionHCS.z += _ZBias * 1;
                // #endif
                
                o.uv.xy = v.uv;
                o.uv.y = 1 - o.uv.y;
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


            float4 ToonOutlineFrag(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target0
            {
             
                float3 positionWS = input.positionWS;

                float4 normalTex_var = SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, input.uv * _NormalTex_ST.xy + _NormalTex_ST.zw);
                float3x3 tangentTransform = float3x3(input.tangentWS, input.bitangentWS, input.normalWS);
                float3 normalTex_processed = UnpackNormalFromTex(normalTex_var, _BumpScale);
                float3 normalWS = normalize(mul(normalTex_processed, tangentTransform));
                
                float facing = isFrontFace ? 1.0 : - 1.0;
                normalWS = normalWS * facing;
                normalWS = lerp(input.normalWS, normalWS, _IsHair);

                Light mainLight = GetMainLight();
                float3 mainLightColor = mainLight.color;
                float mainLightIntensity = max(0.001, (0.299 * mainLightColor.r + 0.587 * mainLightColor.g + 0.114 * mainLightColor.b));
                mainLightColor = mainLightColor / mainLightIntensity;

                mainLightColor = lerp(mainLightColor, float3(1, 1, 1), _IsNeedSelfLight);
                mainLightIntensity = lerp(mainLightIntensity, _SelfLightIntensity, _IsNeedSelfLight);
                
                float mainLightColorStrength = MyCalculateLightStrength(mainLightColor);
                float3 mainLightDir = _MainLightPosition;
                
                float3 mainLightDir_xz = normalize(float3(mainLightDir.x, 6.10351562e-05, mainLightDir.z));

                float halfLambert = 0.5 * dot(normalWS, mainLightDir) + 0.5;
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - input.positionWS.xyz);
                float3 halfDir = normalize(viewDir + mainLightDir);
                
                float3 cameraForward = UNITY_MATRIX_V[2].xyz;
                cameraForward = normalize(cameraForward);
                
                float3 worldUp = float3(0, 1, 0);
                float3 cameraRight = cross(worldUp, cameraForward);
                cameraRight = normalize(cameraRight + float3(1e-6, 0, 0));


                float metallic = 0;
                
                float4 mainTex_var = SAMPLE_TEXTURE2D(_AlbedoTex, sampler_AlbedoTex, input.uv);
                float3 baseColor = mainTex_var.rgb;
                float3 baseColor_dark = baseColor * _AlbedoDarkStrength;
                float energyDistribution_metallic = 0.96 - 0.96 * metallic;
                float3 mainDiffuseColor_Light = baseColor * energyDistribution_metallic;
                float3 mainDiffuseColor_Dark = baseColor_dark * energyDistribution_metallic;
                float3 mainDiffuseColor_Dark_attention = mainDiffuseColor_Dark * 0.65;

                float NoL = dot(normalWS, mainLightDir);
                
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
                rampColor.w *= 1;
                
                float rampColor_max = max(max(rampColor.x, rampColor.y), rampColor.z);
                float rampColor_min = min(min(rampColor.x, rampColor.y), rampColor.z);
                float rampColor_xyzStrength = rampColor_max - rampColor_min;
                
                float NoF = dot(normalWS, cameraForward);
                float SmoothNoF = saturate((NoF - 0.25) * 1.33);
                SmoothNoF = MySmoothstep(SmoothNoF);
                
                
                float3 otherLightDir = float3(0, 1, 0);
                float otherLightNoL = dot(otherLightDir, normalWS);
                
                float3 otherLightColor_dark = _OtherLightColor;
                float3 otherLightColor = lerp(otherLightColor_dark, float3(1, 1, 1), rampColor.w);
                float3 otherLightResult = otherLightColor * otherLightNoL;
                
                
                float3 otherLightResult_day1 = otherLightResult * _OtherLightResultStrength_day1;
                float3 otherLightResult_day0 = otherLightResult * _OtherLightResultStrength_day0;

                float3 mainLightColor_new = lerp(mainLightColorStrength.xxx, mainLightColor, rampColor.w) * mainLightIntensity;
                
                float3 mainLightColor_final = lerp(otherLightResult_day0, mainLightColor_new + otherLightResult_day1, _DayStrength);

                
                float3 mainDiffuseColor_Dark_final = mainDiffuseColor_Dark_attention;
                
                float3 mainDiffuseColor_Dark_lerp = lerp(mainDiffuseColor_Dark_final, mainDiffuseColor_Dark, saturate(SmoothNoF + rampColor.w));
                
                float3 mainDiffuseBrdf = lerp(mainDiffuseColor_Dark_lerp, mainDiffuseColor_Light, rampColor.w);
                
                
                float3 rampColor_xyzEffect = rampColor * rampColor_xyzStrength + 1 - rampColor_xyzStrength;
                float3 mainDiffuseBrdf_rampColor = mainDiffuseBrdf * rampColor_xyzEffect;
                
                float mainDiffuseBrdf_strength = MyCalculateLightStrength(mainDiffuseBrdf);
                float mainDiffuseBrdf_rampColor_strength = MyCalculateLightStrength(mainDiffuseBrdf_rampColor);
                float rampColor_control = mainDiffuseBrdf_strength / max(0.01, mainDiffuseBrdf_rampColor_strength);
                rampColor_control = max(0, rampColor_control);
                rampColor_control = min(1.5, rampColor_control);

                
                float3 mainDiffuseColor_Light_final = mainDiffuseColor_Light;
                
                
                float3 mainDiffuseBrdf_lowLight = lerp(mainDiffuseColor_Dark, mainDiffuseColor_Light_final, SmoothNoF);
                
                float3 mainDiffuseBrdf_final = lerp(mainDiffuseBrdf_lowLight, mainDiffuseBrdf_rampColor * rampColor_control, _DayStrength);

                
                float3 mainDiffuseResult = mainLightColor_final * mainDiffuseBrdf_final;
                
                return float4(mainDiffuseResult * _OutlineColor * _OutLineStrength, 1);
            }
            ENDHLSL
        }
    }
}
