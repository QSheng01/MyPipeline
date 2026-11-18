Shader "ShiYue/URP/Grass/IndirectRenderGrassV2"
{
    Properties
    {
        //_MainTex ("渐变控制图", 2D) = "white" {}

        [Header(Color)]
        [CustomObject(_BaseSetting)]_GrassHeight ("草高度", Float) = 0
        [CustomObject(_BaseSetting)]_ColorCover ("整体颜色偏移", Color) = (1, 1, 1, 1)
        [CustomObject(_BaseSetting)]_BottomColor ("中上部颜色", Color) = (0.2, 0.5, 0.15, 1)
        [CustomObject(_BaseSetting)]_BottomColor3 ("根部颜色", Color) = (0.2, 0.5, 0.15, 1)
        [CustomObject(_BaseSetting)]_BottomColor2 ("根部吸色颜色偏移", Color) = (1, 1, 1, 1)
        [CustomObject(_BaseSetting)]_BottomWeight ("地表吸色，过渡范围", Range(0, 10)) = 0.4
        [CustomObject(_BaseSetting)]_BottomLocation ("地表吸色，过渡位置", Range(-2, 2)) = 0.4
        [CustomObject(_BaseSetting)]_RootSoftness ("根部半透", Range(0, 3)) = 2

        [CustomObject(_BaseSetting)]_ReflectionIntensity ("反射强度_近", Range(0, 1)) = 0.5
        [CustomObject(_BaseSetting)]_ReflectionIntensity_Far ("反射强度_远", Range(0, 1)) = 1
        [CustomTexture(_BaseSetting)]_GrassCoverMap ("Grass Map Cover （草的贴图albedo）", 2D) = "black" { }
        [CustomObject(_BaseSetting)]_GrassCoverMapLerp ("Grass Map Cover Lerp （草贴图固有色与吸色的差值）", Range(0, 1)) = 0.8
        [CustomTexture(_BaseSetting)]_GrassColorOffsetMask ("Grass Color Offset Mask （草颜色偏移遮罩）", 2D) = "black" { }
        [CustomTexture(_BaseSetting)]_GrassColorOffsetMask2 ("Grass Color offset Mask （草全局随机布点的噪音图2）", 2D) = "black" { }
        [CustomObject(_BaseSetting)]_GrassColorOffsetColorMul ("Grass Color offset Mask Color Mul （草全局随机布点的颜色 ， alpha 影响颜色浓度）", Color) = (1, 1, 1, .2)
        [CustomObject(_BaseSetting)]_GrassColorOffsetColorMul2 ("Grass Color offset Mask Color Mul2 （草全局随机布点的颜色2 ， alpha 影响颜色浓度）", Color) = (1, 1, 1, .2)
        _GrassTipOffSet ("Tip Color Offset", Range(0, 1)) = 0.01
        _LightIntensityRatio ("接受灯光的强度", Range(0, 1)) = 1
        _GrassTipLighting ("底部与顶部颜色混合过渡", Range(0, 10)) = 0.5
        _TerrainBlendHeight ("地表混合", Range(0.01, 2)) = 0.25
        
        [Header(RandomColor)]
        _RootColorScale ("Root Color Scale", Range(0.01, 10)) = 0.01
        _RootColorOffset ("Root Color Offset", Range(-1, 1)) = 0
        
        _Smoothness ("光滑度", Range(0.03, 0.97)) = 0.2
        //		_DiffuseNormalShift ("漫反射法线偏移", Range(0,10)) = 0
        //		_DiffuseNormalScale ("漫反射法线偏移强度", Range(0,1)) = 0
        //_SpecularNormalShift ("高光法线偏移", Range(0,1)) = 0.8
        [Header(DiffuseNormal)] _Noisemap ("Noisemap", 2D) = "black" { }
        //_NoiseTilling ("Noise Tilling", Vector) = (1,1,1,1)
        [Header(HighLight)]
        _GrassHighLighting ("高光过渡", Range(0, 10)) = 2.5
        _GrassHighLightingOffect ("高光偏移", Range(-0.5, 0.5)) = 0
        _GrassHighLightingRange ("高光范围", Range(0, 10)) = 0.5
        _GrassHighLightingInt ("高光强度", Range(1, 8)) = 4
        [Header(SSSLight)]
        _SSSValue ("透光深度", Float) = 1
        _SSSPower ("透光过渡", Float) = 1
        _SSSScale ("透光大小", Float) = 1
        
        [Header(Wind)]
        [NoScaleOffset] _WindMap ("风噪声图", 2D) = "black" { }
        [CustomObject(_WindSetting)]_WindSpeed ("风速", Range(0.0, 10.0)) = 3.0
        [CustomObject(_WindSetting)]_WindDirection ("风力方向", vector) = (1, 0, 0, 0)
        [CustomObject(_WindSetting)]_WindGustStrength ("Wind Global Strength（风强）", Range(0.0, 5)) = 0.5
        [CustomObject(_WindSetting)]_WindGustFreq ("Wind Freq （扰乱程度）", Range(0.0, 10.0)) = 2
        [CustomObject(_WindSetting)]_WindBack ("回摆", Range(0, 1)) = 0
        
        [CustomObject(_WindSetting)]_WindGlobaltilling ("WindGlobaltilling （全局风噪音贴图 tilling）", vector) = (0.02, 0.02, 0, 0)
        [CustomObject(_WindSetting)]_WindGlobalspeed ("WindGlobalspeed （全局风速度）", Range(-5, 5)) = -2.8
        [CustomObject(_WindSetting)]_WindGustTint ("Max Gusting tint （全局风高亮亮度）", Range(0.0, 1)) = 0.1
        [CustomObject(_WindSetting)]_ReflectionColorDensity ("Reflection Color Density(（全局风高亮颜色浓度）", Range(0.0, 1)) = 0.5
        [CustomObject(_WindSetting)]_WindGustTopRageProp ("Gusting tint Top Rage Prop （全局风影响草上面和下面的权重差值）", Range(0.0, 3.0)) = 0.3

        _WindAmbientStrength ("Ambient Strength", Range(0.0, 1.0)) = 0.2
        _WindVertexRand ("Vertex randomization", Range(0.0, 1.0)) = 0.6
        _WindObjectRand ("Object randomization", Range(0.0, 1.0)) = 0.5
        _WindRandStrength ("Random per-object strength", Range(0.0, 1.0)) = 0.5
        _WindSwinging ("Swinging", Range(0.0, 1.0)) = 0.15
        _WindGustSpeed ("Gusting Speed", Float) = 4
        
        
        
        [Header(Bend)]
        _BendPushStrength ("Push Strength", Range(0.0, 1.0)) = 1.0
        _BendFlattenStrength ("Flatten Strength", Range(0.0, 1.0)) = 1.0
        _PerspectiveCorrection ("Perspective Correction", Range(0.0, 1.0)) = 0.0

        [HideInInspector]_grassHeight ("_grassHeight", Float) = 1
    }
    
    HLSLINCLUDE
    
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/CloudShadow.hlsl"
    #include "Libraries/Input.hlsl"
    #include "Libraries/Common.hlsl"
    #include "Libraries/Wind.hlsl"
    #include "Libraries/Bending.hlsl"
    
    CBUFFER_START(UnityPerMaterial)
        sampler2D _GrassCoverMap;
        sampler2D _GrassColorOffsetMask;
        sampler2D _GrassColorOffsetMask2;
        half _GrassHeight;
        float4 _MainTex_ST;
        float4 _WindDirection;
        float4 _CloudGustTint;
        float4 _Noisemap_ST;
        // half4 _BottomColor;
        // half4 _TopColor;
        half4 _WindWaveCol;
        float _WindSpeed;
        float _WindGustStrength;
        float _WindGustFreq;
        float _WindBack;
        float4 _WindGlobaltilling;
        float _WindGlobalspeed;

        half _WindWaveRange;
        half _WindWaveInt;
        
        float _BendPushStrength;
        float _BendFlattenStrength;
        float _PerspectiveCorrection;
        half _TopWeight;
        //half3 _HightLightColor;
        half _HightLight;
        half _Metallic;
        half _Smoothness;
        // half _DiffuseNormalShift;
        //half _SpecularNormalShift;
        half _BottomWeight;
        half _TerrainBlendHeight;
        float _grassHeight;
        // half _DiffuseNormalScale;
        half _GrassTipLighting;
        half _GrassHighLighting;
        half _GrassHighLightingOffect;
        half _GrassHighLightingRange;
        half _GrassHighLightingInt;
        half _SSSValue;
        half _SSSPower;
        half _SSSScale;

        half _LightIntensityRatio;
        half _GrassTipOffSet;
        half _RootColorScale;
        half _RootColorOffset;
        half4 _ColorCover;
        half _BottomLocation;
        half4 _BottomColor;
        half4 _BottomColor2;
        half4 _BottomColor3;
        half _RootSoftness;
        half _WindGustTint;
        half _ReflectionColorDensity;
        half _ReflectionIntensity;
        half _ReflectionIntensity_Far;

        half _GrassCoverMapLerp;
        half4 _GrassColorOffsetColorMul;
        half4 _GrassColorOffsetColorMul2;
        float _WindGustTopRageProp;

        float4 _GrassCoverMap_ST;
        float4 _GrassColorOffsetMask_ST;
        float4 _GrassColorOffsetMask2_ST;

        half _WindAmbientStrength;
        half _WindVertexRand;
        half _WindObjectRand;
        half _WindRandStrength;
        half _WindSwinging;
        float _WindGustSpeed;
    CBUFFER_END



    float4 _DLSSJitter;
    float Linear01Depth(float deviceDepth)
    {
        float depth = deviceDepth;
        #if UNITY_REVERSED_Z
            deviceDepth = 1.0 - deviceDepth;
        #endif
        float ortho = deviceDepth;
        float depthZ = lerp(Linear01Depth(depth, _ZBufferParams), ortho, unity_OrthoParams.w);

        return depthZ;
    }
    inline float LinearEyeDepth(float z)
    {
        return 1.0 / (_ZBufferParams.z * z + _ZBufferParams.w);
    }

    float3 GetWorldNormal(float3 normalOS, float4x4 m)
    {
        m._m11 = 1.0;
        m._m30 = 0.0;
        m._m31 = 0.0;
        m._m32 = 0.0;
        m._m33 = 1.0;
        float3x3 rot = (float3x3)m;
        float3 normalWS = normalize(mul(rot, normalOS));
        return normalWS;
    }

    float3 SizeNoise(float3 pivotWS, float3 posOS)
    {
        float2 noiseUV2 = pivotWS.xz * 0.1;
        half colorNoise2 = SAMPLE_TEXTURE2D_LOD(_WindMap, sampler_WindMap, noiseUV2, 0).r;
        colorNoise2 = lerp(-0.35, 1.3, colorNoise2);
        posOS.y += colorNoise2 * saturate(posOS.y);

        return posOS;
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM

            #pragma target 4.5
            #pragma prefer_hlslcc gles

            #pragma vertex vert
            #pragma fragment frag

            //#pragma multi_compile _ DIRLIGHTMAP_COMBINED
            //#pragma multi_compile _ SHADOWS_SHADOWMASK
            //#pragma multi_compile _ LIGHTMAP_ON
            //#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ ENABLE_INTERACTIVEBLENDGRASS

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            //#pragma multi_compile _ _PBR_LIGHTING
            //#pragma shader_feature_local _CLOUD_SHADOW_ON
            #pragma shader_feature __ _CLOUD_SHADOW_ON
            #pragma multi_compile _ _PEROBJECTSHADOW
            #pragma multi_compile _ _TerrainBlend_ON
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            // Tone Mapping
            //#pragma multi_compile _ _TONEMAP_ACES _TONEMAP_NEUTRAL
            //#pragma multi_compile _ LIMITDODGESPACE

            #define  _ENVIRONMENTREFLECTIONS_OFF
            #pragma multi_compile_fog
            //#pragma multi_compile_instancing
            #pragma multi_compile INSTANCING_ON

            #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/Light/SimpleAdditionalLight.hlsl"
            #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/ScanZoneCore.hlsl"
            //#include "Packages/com.unity.render-pipelines.universal/Shaders/CustomPostProcessing/LimitDodgeSpace.hlsl"
            
            //TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_Noisemap);  SAMPLER(sampler_Noisemap);

            TEXTURE2D(_TerrainColor); SAMPLER(sampler_TerrainColor);
            TEXTURE2D_X_FLOAT(_TerrainDepth);  SAMPLER(sampler_TerrainDepth);
            TEXTURE2D(_TerrainNormal); SAMPLER(sampler_TerrainNormal);
            
            #if SHADER_TARGET >= 45
                uniform uint _ArgsOffset;
                StructuredBuffer<uint> _ArgsBuffer;
                StructuredBuffer<float4x4> _IndirectMatrix;
                StructuredBuffer<SHL2Data> _IndirectSHL2;
            #endif

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(half4, _giColor)
            UNITY_DEFINE_INSTANCED_PROP(half4, _terrainNormalWS)
            UNITY_DEFINE_INSTANCED_PROP(half, _allScale)
            //UNITY_DEFINE_INSTANCED_PROP(half, _LOD)
            UNITY_INSTANCING_BUFFER_END(Props)

            real3 SHEvalLinearL0L1_Grass(real3 N, real4 shAr, real4 shAg, real4 shAb)
            {
                real4 vA = real4(N, 1.0);
                real3 x1;
                x1.r = dot(shAr, vA);
                x1.g = dot(shAg, vA);
                x1.b = dot(shAb, vA);
                return x1;
            }

            real3 SHEvalLinearL2_Grass(real3 N, real4 shBr, real4 shBg, real4 shBb, real4 shC)
            {
                real3 x2;
                real4 vB = N.xyzz * N.yzzx;
                x2.r = dot(shBr, vB);
                x2.g = dot(shBg, vB);
                x2.b = dot(shBb, vB);
                real vC = N.x * N.x - N.y * N.y;
                real3 x3 = shC.rgb * vC;
                return x2 + x3;
            }

            float3 SampleSH9_Grass(float4 SHCoefficients[7], float3 N)
            {
                float4 shAr = SHCoefficients[0];
                float4 shAg = SHCoefficients[1];
                float4 shAb = SHCoefficients[2];
                float4 shBr = SHCoefficients[3];
                float4 shBg = SHCoefficients[4];
                float4 shBb = SHCoefficients[5];
                float4 shCr = SHCoefficients[6];

                float3 res = SHEvalLinearL0L1_Grass(N, shAr, shAg, shAb);
                res += SHEvalLinearL2_Grass(N, shBr, shBg, shBb, shCr);
                return res;
            }

            float3 SampleSH_Grass(float3 normalWS, float4 SHAr, float4 SHAg, float4 SHAb, float4 SHBr, float4 SHBg, float4 SHBb, float4 SHC)
            {
                float4 SHCoefficients[7];
                SHCoefficients[0] = SHAr;
                SHCoefficients[1] = SHAg;
                SHCoefficients[2] = SHAb;
                SHCoefficients[3] = SHBr;
                SHCoefficients[4] = SHBg;
                SHCoefficients[5] = SHBb;
                SHCoefficients[6] = SHC;
                return max(float3(0, 0, 0), SampleSH9_Grass(SHCoefficients, normalWS));
            }

            
            
            Varyings vert(Attributes input, uint instanceID : SV_InstanceID)
            {
                Varyings output = (Varyings)0;

                #if SHADER_TARGET >= 45
                    #if defined(SHADER_API_METAL)
                        uint index = instanceID;
                    #else
                        uint index = instanceID + _ArgsBuffer[_ArgsOffset];
                    #endif
                    float4x4 m = _IndirectMatrix[index];
                #else
                    float4x4 m = 0;
                #endif
                
                half allScale = UNITY_ACCESS_INSTANCED_PROP(Props, _allScale);
                
                //  float posOffset = ObjectPosRand01();
                WindSettings wind = PopulateWindSettings(0, _WindSpeed, _WindDirection, AO_MASK,
                _WindGustStrength, _WindGustFreq, _WindBack, _WindGlobaltilling.xy, _WindGlobalspeed);
                half bendAmount = 0;
                VertexOutput data = GetVertexData(input, m, allScale, wind, _GrassHeight, bendAmount);
                //  data.positionOS.xyz = SizeNoise(data.pivotWS, data.positionOS);
                //  WindSettings wind = PopulateWindSettings(0, _WindSpeed, _WindDirection, AO_MASK,
                //  _WindGustStrength, _WindGustFreq, _WindBack, _WindGlobaltilling.xy, _WindGlobalspeed);
                
                //   data.positionOS.xyz = GetSimplerWind2(data.positionOS, input.uv.xy, wind, data.pivotWS, m, bendAmount);
                //  data.positionOS.y *= 0.75;

                //data.positionWS = mul(m, data.positionOS).xyz;
                float3 wPos = data.positionWS;
                float3 pivotWS = data.pivotWS;
                float2 noiseUV = pivotWS.xz * _Noisemap_ST.xy + _Noisemap_ST.zw;
                //WindSettings wind = PopulateWindSettings(0, _WindSpeed, _WindDirection, AO_MASK, _WindGustStrength, _WindGustFreq);
                //  float4 windVec = GetWindOffset(_WindBack, wPos, pivotWS, wind, output.specular.w);

                

                #if ENABLE_INTERACTIVEBLENDGRASS
                    BendSettings bending = PopulateBendSettings(BEND_MASK, _BendPushStrength, _BendFlattenStrength, _PerspectiveCorrection);
                    float4 bendVec = GetBendOffset(wPos, pivotWS, bending);
                #else
                    float4 bendVec = float4(0, 0, 0, 0);
                #endif
                //wPos.xyz = lerp(wPos.xyz, bendVec.xyz, 1);
                wPos = lerp(wPos.xyz, bendVec.xyz, bendVec.a);
                output.gust = SampleGustMap((wPos.xyz), wind);
                //wPos.xyz = lerp(windVec.xyz, bendVec.xyz, bendVec.a);
                
                //output.normalWS = UNITY_ACCESS_INSTANCED_PROP(Props, _terrainNormalWS)*2 - 1;
                
                float3 normal = GetWorldNormal(input.normalOS, m);//TransformObjectToWorldNormal(input.normalOS);
                //grass Normal
                float3 noise = SAMPLE_TEXTURE2D_LOD(_Noisemap, sampler_Noisemap, noiseUV, 0).rgb;
                
                //half3 N = normalize(half3(0, 1, 0) + _DiffuseNormalScale  * noise *saturate(pow(input.uv.y,_DiffuseNormalShift)));//random normal per grass

                output.normalWS = normal;
                data.positionOS.xyz = data.positionOS.xyz;
                output.positionOS.w = input.lightmapUV.x;
                output.positionWS = float4(data.orignPositionWS, 0) ;
                output.positionCS = TransformWorldToHClip(wPos);

                
                // float3 hsvColor =
                // hsvColor.x = RotateHue(hsvColor.x+(noise.x *2)*(50.0h/360.0h), 0.0, 1.0);
                // output.color.rgb = HsvToRgb(hsvColor);
                
                output.uvAndEdge.xy = TRANSFORM_TEX(input.uv, _MainTex);
                output.uv = float4(input.uv, 0, 0);
                output.positionWSAndFogFactor = float4(wPos, ComputeFogFactor(output.positionCS.z));
                OUTPUT_SH(output.normalWS.xyz, output.vertexSH);
                
                
                #ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
                    output.shadowCoord = TransformWorldToShadowCoord(wPos);
                #endif
                
                // #if _TerrainBlend_ON
                //     output.screenPos = ComputeScreenPos(output.positionCS);
                //     output.screenPos.z = -TransformWorldToView(wPos).z;
                //     float2 screenUV = output.screenPos.xy / output.screenPos.w;
                //     output.terrainColor = SAMPLE_TEXTURE2D_LOD(_TerrainColor, sampler_TerrainColor, screenUV, 0);
                // #endif

                half distanceLerp = saturate((output.positionCS.w - 10) * 0.01); // 远处的就保持地表色
                output.terrainColor.a = distanceLerp;
                _BottomColor2 = lerp(_BottomColor2, 1, distanceLerp);
                half3 terrainColor = data.color * _BottomColor2;
                
                half3 albedo = lerp(_BottomColor * terrainColor, terrainColor, distanceLerp);
                //albedo *= lerp(_GrassColorOffsetColorMul, _GrassColorOffsetColorMul2, colorNoise);
                albedo = lerp(_BottomColor3, albedo, input.lightmapUV.x);

                // albedo = distanceLerp;
                albedo = lerp(terrainColor, albedo, saturate(output.positionOS.w * _BottomWeight));
                
                output.terrainColor.rgb = data.color;
                output.color.rgb = albedo * _ColorCover;
                output.color.rgb = lerp(output.color, terrainColor, distanceLerp);
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.var_0.w = bendAmount;
                //output.color = float4(bendVec);
                half3 viewDir = normalize(GetWorldSpaceViewDir(output.positionWSAndFogFactor.xyz));
                output.var_0.xyz = reflect(viewDir, output.normalWS);
                return output;
            }

            inline float Dither8x8Bayer(int x, int y)
            {
                const float dither[ 64 ] = {
                    0, 32, 8, 40, 2, 34, 10, 42,
                    48, 16, 56, 24, 50, 18, 58, 26,
                    12, 44, 4, 36, 14, 46, 6, 38,
                    60, 28, 52, 20, 62, 30, 54, 22,
                    3, 35, 11, 43, 1, 33, 9, 41,
                    51, 19, 59, 27, 49, 17, 57, 25,
                    15, 47, 7, 39, 13, 45, 5, 37,
                    63, 31, 55, 23, 61, 29, 53, 21
                };
                int r = y * 8 + x;
                return dither[r] / 64; // same # of instructions as pre-dividing due to compiler magic

            }

            // 目前只用在ForwardPass中
            half3 RootDither(float4 screenPos, half rootSoftness = 1.0h)
            {
                float2 screenUV = screenPos.xy / screenPos.w;
                float localDepth = screenPos.w;
                float terrainDepth = SAMPLE_TEXTURE2D(_TerrainDepth, sampler_LinearClamp, screenUV);
                terrainDepth = LinearEyeDepth(terrainDepth, _ZBufferParams);
                half softMask = saturate((terrainDepth - localDepth) * rootSoftness);
                // Dither
                float2 screenUV_Raw = screenUV * _ScreenParams.xy;
                screenUV_Raw -= _DLSSJitter.xy * _ScreenParams.xy * 16;
                half dither = Dither8x8Bayer(fmod(screenUV_Raw.x, 8), fmod(screenUV_Raw.y, 8));
                // dither += 0.2;
                half mask = step(dither, softMask);

                //clip(mask - 0.1);
                
                return softMask;
            }

            half4 frag(Varyings input, uint instanceID : SV_InstanceID, half facing : VFACE) : SV_Target
            {
                
                #if SHADER_TARGET >= 45
                    #if defined(SHADER_API_METAL)
                        uint index = instanceID;
                    #else
                        uint index = instanceID + _ArgsBuffer[_ArgsOffset];
                    #endif
                    SHL2Data GIData = _IndirectSHL2[index];
                #else
                    SHL2Data GIData = 0;
                #endif
                
                //UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                InputData inputData = (InputData)0;
                half3 highLightNormal = half3(0, 1, 0);
                inputData.positionWS = input.positionWSAndFogFactor.xyz;
                inputData.positionCS = input.positionCS;
                inputData.normalWS.xyz = input.normalWS.xyz;
                // inputData.normalWS.xyz = highLightNormal;
                inputData.viewDirectionWS = SafeNormalize(GetCameraPositionWS() - input.positionWSAndFogFactor.xyz);
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWSAndFogFactor.xyz);
                inputData.shadowCoord = shadowCoord;
                inputData.fogCoord = input.positionWSAndFogFactor.w;
                inputData.bakedGI = SampleSH(inputData.normalWS);
                // 接受 gi 的强度
                half mask = 1.0;
                inputData.shadowMask = mask;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                half height = input.positionOS.w;

                half dither = RootDither(input.screenPos, _RootSoftness);
                
                half4 shadowMask = CalculateShadowMask(inputData);

                half bakeSkyAO = 1;
                half oc = 1;
                half3 GIColor = inputData.bakedGI;
                // half3 GIColor = SampleProbeVolume(half4(inputData.normalWS,1),inputData.positionWS, oc, inputData.bakedGI,bakeSkyAO);

                
                // // hbao 修改 这里对 SSAO 做特殊处理，直接后面直接乘以 AO 来达到风格化的效果
                AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData.normalizedScreenSpaceUV, oc);
                Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);

                half distanceToCame = input.terrainColor.a;
                
                // SurfaceData
                Light light = GetMainLight();
                half3 reflectV = reflect(inputData.viewDirectionWS, inputData.normalWS);
                half lamb = saturate(dot(-light.direction, reflectV));
                lamb *= Pow4(lamb);
                lamb *= Pow4(lamb);
                // 这里需要把阴影拿出来 乘进去才对，
                half3 heighLightAlbedo = (lamb * light.color) * mainLight.shadowAttenuation;

                // 全局风高亮部分设置于Albedo  reflectSH
                // half3 reflectSH = lerp(input.gust, 0.9 ,remap(0,1,0.5,0.9,SampleSH(input.var_0.x))) * SampleSH(input.var_0.xyz) *  _WindGustTint + heighLightAlbedo ;
                //half3 reflectSH = SampleSH(input.var_0.xyz) + heighLightAlbedo;
                half3 reflectSH = input.gust * _WindGustTint * SampleSH(input.var_0.xyz * (1 - _ReflectionColorDensity));
                //这里直接用sh控制流光颜色了，不用单独的tint
                //reflectSH *= _ReflectionColor ;
                
                half reflectionIntensity = lerp(_ReflectionIntensity, _ReflectionIntensity_Far, saturate(distanceToCame * 3));
                //blendamount 顶部的高亮mask
                half bendAmount = (input.var_0.w * input.var_0.w * input.var_0.w) * reflectionIntensity;
                bendAmount *= mainLight.shadowAttenuation;
                
                // half3 albedo = lerp(input.color, reflectSH, saturate(bendAmount));
                half3 grassMapColor = tex2D(_GrassCoverMap, input.uv).rgb;
                grassMapColor = lerp(input.color, grassMapColor, _GrassCoverMapLerp);
                
                half3 albedo = lerp(grassMapColor, reflectSH, saturate(bendAmount * _WindGustTopRageProp));
                //  return float4((saturate(input.var_0.w) ).rrr,1);
                // 全局草颜色局部颜色偏移计算
                float2 maskUV = input.positionWS.xz * _GrassColorOffsetMask_ST.xy + _GrassColorOffsetMask_ST.zw;
                float mask1 = tex2D(_GrassColorOffsetMask, maskUV).r;
                float mask2 = tex2D(_GrassColorOffsetMask2, maskUV).r;
                //half3 maskCol = lerp(_GrassColorOffsetColorMul, _GrassColorOffsetColorMul2, mask2);
                //return half4(maskCol, 1);
                half3 maskColF = (mask1 - (mask1 * mask2)) * _GrassColorOffsetColorMul + (mask1 * mask2) * _GrassColorOffsetColorMul2 + albedo * (1 - mask1);
                float maskColFLerp = lerp(_GrassColorOffsetColorMul.a, _GrassColorOffsetColorMul2.a, (mask1 * mask2));
                albedo = lerp(albedo, maskColF, maskColFLerp);
                //albedo *= lerp(_GrassColorOffsetColorMul, _GrassColorOffsetColorMul2, colorNoise);
                
                SurfaceData surfaceData = (SurfaceData)0;
                // surfaceData.albedo = albedo + heighLightAlbedo * albedo * 0.2;
                surfaceData.albedo = albedo + reflectSH * input.var_0.w + heighLightAlbedo * albedo * 0.2;
                surfaceData.alpha = 1;
                half smooth = lerp(0.2, 0.5, input.var_0.w);
                surfaceData.smoothness = lerp(smooth, 0, distanceToCame) * height; // 远处和地表的数值一致
                surfaceData.metallic = 0;
                surfaceData.specular = 0;
                surfaceData.occlusion = oc;
                
                half4 color = 1;
                // surfaceData.albedo = input.terrainColor;
                color = UniversalFragmentPBR(inputData, surfaceData);
                // 混合雾
                // return half4(inputData.normalWS.rgb, 1);
                color.rgb = saturate(MixFog(color, input.positionWSAndFogFactor.w));
                
                #ifdef _WORLDFOG
                    float3 positionWS = input.positionWSAndFogFactor.xyz;
                    WarFogSample(color.rgb, positionWS);
                #endif
                
                return color;
            }
            ENDHLSL
        }
        
        
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 4.5

            //--------------------------------------
            // GPU Instancing
            //#pragma multi_compile_instancing
            #pragma multi_compile INSTANCING_ON

            #pragma multi_compile _ ENABLE_INTERACTIVEBLENDGRASS
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/Light/SimpleAdditionalLight.hlsl"

            #if SHADER_TARGET >= 45
                uniform uint _ArgsOffset;
                StructuredBuffer<uint> _ArgsBuffer;
                StructuredBuffer<float4x4> _IndirectMatrix;
            #endif

            Varyings DepthOnlyVertex(Attributes input, uint instanceID : SV_InstanceID)
            {
                Varyings output = (Varyings)0;

                #if SHADER_TARGET >= 45
                    #if defined(SHADER_API_METAL)
                        uint index = instanceID;
                    #else
                        uint index = instanceID + _ArgsBuffer[_ArgsOffset];
                    #endif
                    float4x4 m = _IndirectMatrix[index];
                #else
                    float4x4 m = 0;
                #endif
                
                VertexOutput data = GetVertexData(input, m, 1);

                float3 wPos = data.positionWS;
                float3 pivotWS = data.pivotWS;

                // WindSettings wind = PopulateWindSettings(0, _WindSpeed, _WindDirection, AO_MASK, _WindGustStrength, _WindGustFreq);
                // float4 windVec = GetWindOffset(_WindBack, wPos, pivotWS, wind, output.specular.w);

                // #if ENABLE_INTERACTIVEBLENDGRASS
                //     BendSettings bending = PopulateBendSettings(BEND_MASK, _BendPushStrength, _BendFlattenStrength, _PerspectiveCorrection);
                //     float4 bendVec = GetBendOffset(wPos, pivotWS, bending);
                // #else
                //     float4 bendVec = float4(0, 0, 0, 0);
                // #endif

                //float3 offsets = lerp(windVec.xyz, bendVec.xyz, bendVec.a);
                //wPos.xyz += bendVec.xyz;
                //wPos.xyz += offsets.xyz;
                wPos.xyz = wPos.xyz;
                //wPos.xyz = lerp(windVec.xyz, bendVec.xyz, bendVec.a);

                output.normalWS = data.normalWS;
                output.positionCS = TransformWorldToHClip(wPos);
                
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                return 0;
            }

            ENDHLSL
        }

        Pass
        {

            
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 4.5

            //--------------------------------------
            // GPU Instancing
            //#pragma multi_compile_instancing
            #pragma multi_compile INSTANCING_ON

            #pragma multi_compile _ ENABLE_INTERACTIVEBLENDGRASS
            #pragma vertex DepthOnlyertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/Light/SimpleAdditionalLight.hlsl"

            #if SHADER_TARGET >= 45
                uniform uint _ArgsOffset;
                StructuredBuffer<uint> _ArgsBuffer;
                StructuredBuffer<float4x4> _IndirectMatrix;
            #endif

            Varyings DepthOnlyertex(Attributes input, uint instanceID : SV_InstanceID)
            {
                Varyings output = (Varyings)0;

                #if SHADER_TARGET >= 45
                    #if defined(SHADER_API_METAL)
                        uint index = instanceID;
                    #else
                        uint index = instanceID + _ArgsBuffer[_ArgsOffset];
                    #endif
                    float4x4 m = _IndirectMatrix[index];
                #else
                    float4x4 m = 0;
                #endif
                
                WindSettings wind = PopulateWindSettings(0, _WindSpeed, _WindDirection, AO_MASK,
                _WindGustStrength, _WindGustFreq, _WindBack, _WindGlobaltilling.xy, _WindGlobalspeed);
                 half bendAmount = 0;
                VertexOutput data = GetVertexData(input, m, 1, wind, _GrassHeight, bendAmount);
                
                float3 wPos = data.positionWS;
                float3 pivotWS = data.pivotWS;

                // WindSettings wind = PopulateWindSettings(0, _WindSpeed, _WindDirection, AO_MASK, _WindGustStrength, _WindGustFreq);
                // float4 windVec = GetWindOffset(_WindBack, wPos, pivotWS, wind, output.specular.w);

                #if ENABLE_INTERACTIVEBLENDGRASS
                    BendSettings bending = PopulateBendSettings(BEND_MASK, _BendPushStrength, _BendFlattenStrength, _PerspectiveCorrection);
                    float4 bendVec = GetBendOffset(wPos, pivotWS, bending);
                #else
                    float4 bendVec = float4(0, 0, 0, 0);
                #endif

                wPos =  bendVec.xyz;
                //wPos.xyz += bendVec.xyz;
                //wPos.xyz += offsets.xyz;
                //wPos.xyz = lerp(windVec.xyz, bendVec.xyz, bendVec.a);
                //output.normalWS = data.normalWS;
                output.normalWS = GetWorldNormal(input.normalOS, m);//TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(wPos);
                
                return output;
            }

            void DepthOnlyFragment(Varyings input, out half4 outNormalWS : SV_Target0)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                // float3 normalWS = NormalizeNormalPerPixel(input.normalWS);

                //half dither = RootDither(input.screenPos, _RootSoftness);
                
                float3 normalWS = normalize(input.normalWS);
                outNormalWS = half4(normalWS, 0.0);
            }

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
