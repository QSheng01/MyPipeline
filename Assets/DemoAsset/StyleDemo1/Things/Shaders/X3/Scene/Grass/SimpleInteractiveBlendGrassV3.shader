Shader "ShiYue/URP/Grass/SimpleInteractiveBlendGrassV3"
{
    Properties
    {
        [CustomGroup] _BaseSetting("基础设置", float) = 1
        [CustomTexture(_BaseSetting)]_MainTex("渐变控制图", 2D) = "white" {}
        [CustomTexture(_BaseSetting)]_Noisemap ("噪波贴图", 2D) = "black" {}
        [CustomObject(_BaseSetting)]_CusColor ("自定义颜色", Color) = (1,1,1,1)
        [CustomObject(_BaseSetting)]_Smoothness("光滑度",Range(0.03, 0.97)) = 0.2
        [CustomObject(_BaseSetting)]_CustomY ("自定义草高度倍率", float) = 1.8

        [CustomGroup] _ColorSetting("颜色设置", float) = 1
        //_TopColor("顶部颜色", Color) = (0.5,0.5,0.5,1)
        //_BottomColor("底部颜色", Color) = (0.2, 0.5, 0.15, 1)
        //_TopWeight("顶部颜色权重",Range(0,1)) = 0.8
        //_BottomWeight("底部颜色权重",Range(0,1)) = 0.4
        [CustomObject(_ColorSetting)]_GrassTipOffSet("尖部颜色偏移值", Range(0, 1)) = 0.01
        [CustomObject(_ColorSetting)]_GiRatio("接受GI的强度",Range(0.0,1)) = 1.0
        [CustomObject(_ColorSetting)]_LightIntensityRatio("接受灯光的强度",Range(0,1)) = 1
        [CustomObject(_ColorSetting)]_GrassTipLighting ("底部与顶部颜色混合过渡", Range(0, 10)) = 0.5
        //		[CustomObject(_ColorSetting)]_TerrainBlendHeight("地表混合地面高度",Range(0.01,0.5)) = 0.25
        //		[CustomObject(_ColorSetting)]_TerrainBlendRange("地表混合过渡区域大小",Range(0.01,2)) = 0.25
        [CustomObject(_ColorSetting)]_TerrainBlendHeight("地表混合",Range(0.01,2)) = 0.25



        [CustomGroup] _RandomColorSetting("随机颜色设置", float) = 1
        [CustomObject(_RandomColorSetting)]_RootColorScale ("根据噪波图R通道信息刷色混合比例", Range(0.01, 10)) = 0.01
        [CustomObject(_RandomColorSetting)]_RootColorOffset ("根据噪波图R通道信息刷色混合偏移", Range(-1, 1)) = 0

        //		_DiffuseNormalShift("漫反射法线偏移",Range(0,10)) = 0
        //		_DiffuseNormalScale ("漫反射法线偏移强度", Range(0,1)) = 0
        //_SpecularNormalShift("高光法线偏移",Range(0,1)) = 0.8
        //_NoiseTilling ("Noise Tilling", Vector) = (1,1,1,1)
        [CustomGroup] _HightLightSetting("高光设置", float) = 1
        [CustomObject(_HightLightSetting)]_GrassHighLighting ("高光过渡", Range(0, 10)) = 2.5
        [CustomObject(_HightLightSetting)]_GrassHighLightingOffect ("高光偏移", Range(-0.5, 0.5)) = 0
        [CustomObject(_HightLightSetting)]_GrassHighLightingRange ("高光范围", Range(0, 10)) = 0.5
        [CustomObject(_HightLightSetting)]_GrassHighLightingInt ("高光强度", Range(0, 8)) = 4
        [CustomObject(_HightLightSetting)]_AO ("根部AO", Range(0, 8)) = 0.25
        [CustomObject(_HightLightSetting)]_AddLightIntensity("点光接受强度", Range(0, 1)) = 0.5

        [CustomGroup] _SSSSetting("SSS设置", float) = 1
        [CustomObject(_SSSSetting)]_SSSValue ("透光深度", Float) = 1
        [CustomObject(_SSSSetting)]_SSSPower ("透光过渡", Float) = 1
        [CustomObject(_SSSSetting)]_SSSScale ("透光大小", Float) = 1

        [CustomGroup] _WindSetting("风力设置", float) = 1
        [CustomTexture(_WindSetting)][NoScaleOffset] _WindMap("风噪声图", 2D) = "black" {}
        [CustomObject(_WindSetting)]_WindWaveCol ("风浪颜色",Color) = (1,1,1,0)
        [CustomObject(_WindSetting)]_WindDirection("风力方向", vector) = (1,0,0,0)
        [CustomObject(_WindSetting)]_WindSpeed("风速", Range(0.0, 10.0)) = 3.0
        [CustomObject(_WindSetting)]_WindGustStrength("风强", Range(0.0, 2)) = 0.5
        [CustomObject(_WindSetting)]_WindGustFreq("扰乱程度", Range(0.0, 10.0)) = 2
        [CustomObject(_WindSetting)]_WindBack("回摆",Range(0,1)) = 0

        [CustomObject(_WindSetting)]_WindWaveRange("风浪范围", Range(0,1)) = 0.75
        [CustomObject(_WindSetting)]_WindWaveInt("风浪颜色强度", Range(0,0.5)) = 1

        [CustomGroup] _ScanZoneSetting("区域氛围设置", float) = 1
        [CustomObject(_ScanZoneSetting)]_ScanZoneLerp ("区域氛围范围", Range(0,1)) = 1

        //		[Header(Bend)]
        //		_BendPushStrength("Push Strength", Range(0.0, 1.0)) = 1.0
        //		_BendFlattenStrength("Flatten Strength", Range(0.0, 1.0)) = 1.0
        //		_PerspectiveCorrection("Perspective Correction", Range(0.0, 1.0)) = 0.0

        [HideInInspector]_grassHeight("_grassHeight",Float) = 1
        [HideInInspector][Enum(UnityEngine.Rendering.CompareFunction)]_ZTest("_ZTest",Float) = 4
        [HideInInspector][Enum(Off,0,On,1)]_ZWrite("_ZWrite",Float) = 1

        [CustomGroup] _StencilSetting("模板设置",float) = 1.0
        [CustomObject(_StencilSetting)] _StencilComp ("Stencil Comparison", Float) = 8
        //[CustomObject(_StencilSetting)] _Stencil ("Stencil ID", Float) = 1    
        [CustomObject(_StencilSetting)] _StencilOp ("Stencil Operation", Float) = 2
        [CustomObject(_StencilSetting)] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [CustomObject(_StencilSetting)] _StencilReadMask ("Stencil Read Mask", Float) = 255

    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/CloudShadow.hlsl"
    #include "Libraries/Input.hlsl"
    #include "Libraries/Common.hlsl"
    #include "Libraries/Wind.hlsl"
    #include "Libraries/Bending.hlsl"
    //#include "Libraries/GrassTranslucency.hlsl"
    #include "Libraries/Color.hlsl"

    CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        float4 _WindDirection;
        float4 _CloudGustTint;
        float4 _Noisemap_ST;
        float4 _CusColor;
        half _GiRatio;
        // half4 _TerrainBlendColor;
        // half4 _BottomColor;
        // half4 _TopColor;	
        half4 _WindWaveCol;
        float _WindSpeed;
        float _WindGustStrength;
        float _WindGustFreq;
        float _WindBack;

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
        half _TerrainBlendRange;
        //float _grassHeight;
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

        half _AO;
        half _AddLightIntensity;

        half _ScanZoneLerp;
        half _CustomY;

        // LOD 有关的
        half3 _LODDistance;
        half _NowLOD;
    CBUFFER_END

    TEXTURE2D(_Noisemap);
    SAMPLER(sampler_Noisemap);
    
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

    VertexOutput SetVertexInfo(inout Attributes input, inout Varyings output, float allScale)
    {

        VertexOutput data = (VertexOutput)0;
        
	    float3 color = float3(UNITY_MATRIX_M[3][0], UNITY_MATRIX_M[3][1], UNITY_MATRIX_M[3][2]);
	    float scale = UNITY_MATRIX_M[1][1];
     
	    float4x4 objectToWorldMaterix = mul(_localToWorldMatrix, UNITY_MATRIX_M);
	    float4 positionOS = input.positionOS;
	    if (allScale > 0.5)
	        positionOS.xz *= allScale;
        positionOS.y *= _CustomY;
     
	    data.color = DecodeUint2Color(DecodeFloat2Uint(UNITY_MATRIX_M[3][0]));
	    data.randomColor = DecodeUint2Color(DecodeFloat2Uint(UNITY_MATRIX_M[3][1]));
	    data.terrainColor = DecodeUint2Color(DecodeFloat2Uint(UNITY_MATRIX_M[3][2]));
     
	    data.positionOS = positionOS;
	    data.positionWS = mul(objectToWorldMaterix, positionOS).xyz;
	    data.normalWS = TransformObjectToWorldNormal(input.normalOS);//input.positionOS;//mul((float3x3)objectToWorldMaterix, input.normalOS);
	    data.pivotWS = float3(objectToWorldMaterix[0][3], objectToWorldMaterix[1][3], objectToWorldMaterix[2][3]);
        
        float3 wPos = data.positionWS;
        float3 pivotWS = data.pivotWS;
        
        WindSettings wind = PopulateWindSettings(0, _WindSpeed, _WindDirection, AO_MASK, _WindGustStrength,
                                    _WindGustFreq);

        #if lodLevel
			float3 windVec = GetSimplerWind(wPos, pivotWS, wind, input.positionOS.y);
        #else
        float4 windVec = float4(wPos + wind.direction * SampleGustMap(wPos, wind) * input.positionOS.y, 1);
        #endif

        // 配置碰撞
        BendSettings bending = PopulateBendSettings(BEND_MASK, _BendPushStrength, _BendFlattenStrength, _PerspectiveCorrection);
        float4 bendVec = GetBendOffset(wPos, pivotWS, bending);
        wPos.xyz = lerp(windVec.xyz, bendVec.xyz, bendVec.a);
        
        // 法线
        float3 normal = TransformObjectToWorldNormal(input.normalOS);

        // 其他数据
        float2 sampleUV2 = float2(wPos.x / 10, wPos.z / 10);
        sampleUV2 += float2(_Time.x * 2, 0);
        float4 WaveRipplingSample = SAMPLE_TEXTURE2D_LOD(_Noisemap, sampler_Noisemap, sampleUV2, 0);
        wPos.x -= sin(0.1 * WaveRipplingSample) * 2 * input.uv.y * 2;
        output.normalWS = normal;
        output.positionCS = TransformWorldToHClip(wPos);
        output.gust = WaveRipplingSample.a;
        
        data.wPos = wPos;
        return data;
    }
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"
        }

        //https://docs.unity.cn/cn/current/Manual/SL-Stencil.html
        Stencil
        {
            Ref 1//[_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }


        // PASS 0
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            Cull Off
            ZTest [_ZTest]
            ZWrite [_ZWrite]

            HLSLPROGRAM
            // #pragma target 3.0
            // #pragma prefer_hlslcc gles

            #pragma vertex vert
            #pragma fragment frag

            //#pragma multi_compile _ DIRLIGHTMAP_COMBINED
            //#pragma multi_compile _ SHADOWS_SHADOWMASK
            //#pragma multi_compile _ LIGHTMAP_ON
            //#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING	
            // #pragma multi_compile _ ENABLE_INTERACTIVEBLENDGRASS	
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            // #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS

            //#pragma multi_compile _ _PBR_LIGHTING
            //#pragma shader_feature_local _CLOUD_SHADOW_ON
            // #pragma shader_feature _ _CLOUD_SHADOW_ON
            #pragma multi_compile _ _PEROBJECTSHADOW
            // #pragma multi_compile _ _TerrainBlend_ON
            // #pragma multi_compile _ _SCANZONE_ON

            #define  _ENVIRONMENTREFLECTIONS_OFF
            #define  LIGHTPROBE_SH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            // #pragma instancing_options renderinglayer assumeuniformscaling procedural:ConfigureProcedural

            #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
			StructuredBuffer<float4x4> _PointMatrix;
            #endif

            void ConfigureProcedural()
            {
                #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
			    unity_ObjectToWorld = 0.0;
			    unity_ObjectToWorld = _PointMatrix[unity_InstanceID];
                #endif
            }

            #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/Light/SimpleAdditionalLight.hlsl"
            #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/ScanZoneCore.hlsl"
            #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/X3/Scene/ShaderInclude/ShaderOtherFunction.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);


            TEXTURE2D(_TerrainColor);
            SAMPLER(sampler_TerrainColor);
            TEXTURE2D_X_FLOAT(_TerrainDepth);
            SAMPLER(sampler_TerrainDepth);
            TEXTURE2D(_TerrainNormal);
            SAMPLER(sampler_TerrainNormal);


            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(half4, _giColor)
                UNITY_DEFINE_INSTANCED_PROP(half4, _terrainNormalWS)
                UNITY_DEFINE_INSTANCED_PROP(half, _allScale)
                //UNITY_DEFINE_INSTANCED_PROP(half, _LOD)
            UNITY_INSTANCING_BUFFER_END(Props)

            #define lodLevel  0
            #undef EVALUATE_SH_VERTEX
            #define _SCANZONE_ON

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                // necessary only if you want to access instanced properties in the fragment Shader.

                half allScale = UNITY_ACCESS_INSTANCED_PROP(Props, _allScale);
                
                VertexOutput data = SetVertexInfo(input, output, allScale);
     
                float2 noiseUV = data.pivotWS.xz * _Noisemap_ST.xy + _Noisemap_ST.zw;
                float3 noise = SAMPLE_TEXTURE2D_LOD(_Noisemap, sampler_Noisemap, noiseUV, 0).rgb;
                float posOffset = ObjectPosRand01();
                float blend = saturate(noise.x / _RootColorScale + _RootColorOffset);
     
                output.color = lerp(data.color, data.randomColor, blend) * _CusColor;
                output.color = ApplyVertexColor(input.positionOS, data.wPos, output.color.rgb, AO_MASK, _AO, 0.1,
                                                   float4(output.color.rgb, 0.15), posOffset);
     
                output.uvAndEdge.xy = TRANSFORM_TEX(input.uv, _MainTex);
                output.positionWSAndFogFactor = float4(data.wPos, ComputeFogFactor(output.positionCS.z));
                    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);
     
     
                #ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
					output.shadowCoord = TransformWorldToShadowCoord(data.wPos);
                #endif
     
                // 望月版吸色
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.screenPos.z = -TransformWorldToView(data.wPos).z;
                float2 screenUV = output.screenPos.xy / output.screenPos.w;
                output.terrainColor = SAMPLE_TEXTURE2D_LOD(_TerrainColor, sampler_TerrainColor, screenUV, 0);
                return output;
            }

            half4 frag(Varyings input, half facing : VFACE) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                //return input.normalWS.xyzz;
                // half4 bakedGI = UNITY_ACCES_INSTANCED_PROP(Props, _giColor);
                // half4 decodeInstructions = half4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0h, 0.0h);
                //
                // if(bakedGI.a < 1)
                // 	bakedGI.rgb = DecodeLightmap(bakedGI,decodeInstructions);					

                #if defined(SHADOWS_SHADOWMASK)
					half mask = input.color.a;
                #else
                half mask = 1.0;
                #endif

                // rongqingmei 测试
                // return input.color;
                // return input.terrainColor;

                float3 positionWS = input.positionWSAndFogFactor.xyz;

                // 根据 LOD 距离，配置渐隐效果
                // 配置摄像机到草距离
                // if(_NowLOD == 1)
                // {
                // 	// return half4(1,1,1,1);
                // 	half tDistance = length(GetCameraPositionWS() - positionWS);
                // 	half lodCutLine = _LODDistance[_NowLOD];
                // 	// 配置过渡，tDistance 从 lodCutLine - 10, 过渡到 lodCutLine，结果值由 1 过渡到0.5;
                // 	half lerpValue = saturate(remap(tDistance, lodCutLine - 1.0h, lodCutLine + 1.0h, 1.0f, 0.7f));
                // 	
                // 	// return half4(lerpValue.xxx, 1.0f);
                // 	DitherObject(lerpValue, input.positionCS);
                // }

                float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
                half weight = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uvAndEdge.xy).r;
                half3 albedo = input.color.xyz;
                half3 topTipColor = albedo * (1 + _GrassTipOffSet);
                albedo = lerp(albedo, topTipColor, saturate(pow(weight, _GrassTipLighting))).rgb;
                // half3 color_Copy = albedo;
                //             ScanZone_SceneColor(albedo.rgb, positionWS);
                //             albedo = lerp(color_Copy, albedo, _ScanZoneLerp);

                SurfaceData surfaceData = (SurfaceData)0;
                InputData inputData = (InputData)0;
                BRDFData brdfData;
                half3 highLightNormal = half3(0, 1, 0);
                inputData.positionWS = positionWS;
                inputData.normalWS.xyz = input.normalWS.xyz;
                //lerp(input.normalWS.xyz,half3(0,1,0),length(GetCameraPositionWS() - inputData.positionWS)/30);

                inputData.viewDirectionWS = SafeNormalize(GetCameraPositionWS() - positionWS);
                inputData.shadowCoord = shadowCoord;
                inputData.fogCoord = input.positionWSAndFogFactor.w;
                inputData.bakedGI = SampleSHVertex(highLightNormal);
                // 接受 gi 的强度
                half4 tWhite = half4(inputData.bakedGI.rrr, 1.0h);
                inputData.bakedGI = lerp(tWhite, inputData.bakedGI, _GiRatio);
                inputData.shadowMask = mask;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                Light mainLight = GetMainLight(shadowCoord, positionWS, half4(mask, mask, mask, mask));

                #if defined(_SCREEN_SPACE_OCCLUSION)
                AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV);
                mainLight.color *= aoFactor.directAmbientOcclusion;
                surfaceData.occlusion = min(surfaceData.occlusion, aoFactor.indirectAmbientOcclusion);
                #endif

                //混合逐对象投影
                #if defined(_PEROBJECTSHADOW)
					half pershadow = SAMPLE_TEXTURE2D_X(_ScreenSpaceShadowmapTexture, sampler_ScreenSpaceShadowmapTexture, input.screenPos.xy / input.screenPos.w).r;
					mainLight.shadowAttenuation = min(mainLight.shadowAttenuation, pershadow);
                #endif


                // 计算阴影的 mainLightColor 不能乘以 _LightIntensityRatio，不然阴影颜色会错误
                // 宝藏草
                // if(mainLight.shadowAttenuation < 0.5h)
                // {
                // 	// 灯光变弱，阴影也需要变弱
                // 	mainLight.shadowAttenuation /=_LightIntensityRatio;
                // 	// 有阴影的地方，地表吸色也需要变暗
                // 	input.terrainColor.rgb *= mainLight.shadowAttenuation;
                // }

                // 望月地形混合
                half grassblend = 0;
                half3 terrainColor = 0;
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                half3 terrainNormal = SAMPLE_TEXTURE2D(_TerrainNormal, sampler_TerrainNormal, screenUV).rgb;
                terrainNormal = terrainNormal * 2 - 1;

                //input.normalWS = terrainNormal;
                float terrainDepth = SAMPLE_TEXTURE2D_X(_TerrainDepth, sampler_TerrainDepth, screenUV).r;
                terrainDepth = LinearEyeDepth(terrainDepth, _ZBufferParams);
                half blend = saturate(abs(terrainDepth - input.screenPos.z) / _TerrainBlendHeight);
                blend = sqrt(blend);
                grassblend = saturate(blend);
                highLightNormal = lerp(terrainNormal, highLightNormal, blend);
                //inputData.bakedGI = SampleSHVertex(highLightNormal);
                //inputData.bakedGI = SampleSH(highLightNormal);

                // 接受 Gi 的强度
                // inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
                inputData.bakedGI = SampleSH(inputData.normalWS);

                tWhite = half4(inputData.bakedGI.rrr, 1.0h);
                inputData.bakedGI = lerp(tWhite, inputData.bakedGI, _GiRatio);

                //inputData.bakedGI = SampleSH_Grass(highLightNormal, GIData.SHAr, GIData.SHAg, GIData.SHAb, GIData.SHBr, GIData.SHBg, GIData.SHBb, GIData.SHC);
                // ScanZone_SceneColor(input.terrainColor.rgb, positionWS);
                surfaceData.albedo = input.terrainColor.rgb;
                surfaceData.alpha = 1;
                surfaceData.smoothness = 0;
                surfaceData.metallic = 0;
                surfaceData.specular = half3(0.0h, 0.0h, 0.0h);

                // NOTE: can modify alpha
                InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular,
                             surfaceData.smoothness, surfaceData.alpha, brdfData);

                //TerrainLighting
                terrainColor = LightingPhysicallyBased(brdfData, mainLight, terrainNormal, inputData.viewDirectionWS,
            false);
                terrainColor.rgb += brdfData.diffuse * inputData.bakedGI.rgb;
                // return half4(surfaceData.albedo, 1.0f);
                // return half4(terrainColor, 1.0f);
                // return half4(inputData.bakedGI.rgb, 1.0f);

                // 正常渲染部分
                mainLight.color *= _LightIntensityRatio;
                surfaceData.albedo = albedo;
                surfaceData.alpha = 1;
                surfaceData.smoothness = _Smoothness;
                surfaceData.metallic = 0;
                surfaceData.specular = half3(0.0h, 0.0h, 0.0h);
                surfaceData.occlusion = 1;
                // NOTE: can modify alpha
                InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular,
                            surfaceData.smoothness, surfaceData.alpha, brdfData);
                //TerrainLighting
                brdfData.diffuse = albedo;


                // half3 gi =GlobalIllumination(brdfData,inputData.bakedGI.rgb,1,highLightNormal,inputData.viewDirectionWS);
                half3 gi = brdfData.diffuse * inputData.bakedGI.rgb * surfaceData.occlusion;

                half NdotL = saturate(dot(inputData.normalWS, mainLight.direction) * 0.5 + 0.5);
                half3 radiance = mainLight.color * (mainLight.shadowAttenuation * NdotL);
                half3 brdf = brdfData.diffuse * mainLight.color * mainLight.shadowAttenuation;

                float3 halfDir = normalize(mainLight.direction.xyz + inputData.viewDirectionWS);
                float NdotH = saturate(dot(highLightNormal + half3(0, _GrassHighLightingOffect, 0), halfDir));

                float3 specular = pow(NdotH, (10 - _GrassHighLightingRange) * 12.8 + 5) * mainLight.shadowAttenuation;
                specular = lerp(0, specular, saturate(pow(input.uvAndEdge.y, _GrassHighLighting)));
                specular = pow(specular, 4);
                float waveColor = input.gust;
                clip(waveColor - 0.15);
                specular = smoothstep(0, 1, specular) * _GrassHighLightingInt * waveColor.r;
                brdf += specular;
                half3 color = brdf + gi;
                // ScanZone_SceneColor(_WindWaveCol.rgb, inputData.positionWS);
                //风场颜色
                half3 windcol = _WindWaveInt * saturate(
                    smoothstep(1 - 2 * _WindWaveRange, 1, input.gust) * _WindWaveCol.rgb) * saturate(
                    pow(input.uvAndEdge.y, _GrassHighLighting)); //有风浪的范围

                float3 N_Shift = inputData.normalWS * _SSSValue + mainLight.direction;
                half BackLight = saturate(
                    pow(saturate(dot(-N_Shift, inputData.viewDirectionWS)), _SSSPower) * _SSSScale);
                half3 SSS = BackLight * radiance * brdfData.diffuse;

                // 混合地形，望月版
                // color += (SSS + windcol * mainLight.shadowAttenuation);
                color = lerp(min(terrainColor / 1.0f, 1), color, grassblend);
                
                // 混合地形，宝藏版
                color += (SSS + windcol  * mainLight.shadowAttenuation);
                // color = lerp(min(input.terrainColor.rgb, 1), color,saturate(input.terrainColor.w));
                // color = mainLight.shadowAttenuation.xxx;

                // 最终草的颜色
                // 混合 SSAO
                // #ifdef _SCREEN_SPACE_OCCLUSION
                //     half aoSSAO = SampleAmbientOcclusion(inputData.normalizedScreenSpaceUV);
                //     color = lerp(color * aoSSAO, color, grassblend);
                //    #endif 

                #ifdef _ADDITIONAL_LIGHTS

                #if USE_FORWARD_PLUS
					uint pixelLightCount =0;
					uint lightIndex; 
					ClusterIterator _urp_internal_clusterIterator = ClusterInit(GetNormalizedScreenSpaceUV(input.positionCS), positionWS, 0); 
					[loop]
					while (ClusterNext(_urp_internal_clusterIterator, lightIndex))
					{ 
						lightIndex += URP_FP_DIRECTIONAL_LIGHTS_COUNT; 
						FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
						Light light = GetAdditionalLight(lightIndex, positionWS);
						half atten = light.distanceAttenuation * light.shadowAttenuation;
						half3 LightLambert = LightingLambert(light.color,light.direction, normalize(float3(0,1,0))) * color * atten * _AddLightIntensity;
						color += LightLambert;			
					}
                #else
				
                    uint pixelLightCount = GetAdditionalLightsCount();
                    for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                    {
                        Light light = GetAdditionalLight(lightIndex, positionWS);
                        half atten = light.distanceAttenuation * light.shadowAttenuation;
                    	half3 LightLambert = LightingLambert(light.color,light.direction, normalize(float3(0,1,0))) * color * atten * _AddLightIntensity;
                        color += LightLambert;
                    }
                #endif

                #endif
                color = saturate(MixFog(color, input.positionWSAndFogFactor.w));
                return half4(color, 1);
            }
            ENDHLSL
        }

        // PASS 1
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ZWrite On
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            // #pragma prefer_hlslcc gles
            // #pragma exclude_renderers d3d11_9x
            // #pragma target 2.0


            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            // #pragma multi_compile _ ENABLE_INTERACTIVEBLENDGRASS
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/Light/SimpleAdditionalLight.hlsl"

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(half, _allScale)
            UNITY_INSTANCING_BUFFER_END(Props)

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                // necessary only if you want to access instanced properties in the fragment Shader.

                half allScale = UNITY_ACCESS_INSTANCED_PROP(Props, _allScale);
                
                // 配置顶点信息
                SetVertexInfo(input, output, allScale);

                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                return 0;
            }
            ENDHLSL
        }

        // PASS 2
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            ZWrite On
            Cull Off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            // #pragma prefer_hlslcc gles
            // #pragma exclude_renderers d3d11_9x
            // #pragma target 2.0

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
             
            // #pragma multi_compile _ ENABLE_INTERACTIVEBLENDGRASS
            #pragma vertex DepthNormalVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/Light/SimpleAdditionalLight.hlsl"
            // Assets/DemoAsset/StyleDemo1/Things/Shaders/X3/Scene/ShaderInclude/ShaderOtherFunction.hlsl

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(half, _allScale)
            UNITY_INSTANCING_BUFFER_END(Props)

            float Dither32(float2 Pos)
            {
                float Ret = dot(float3(Pos.xy, 0.5f), float3(0.40625f, 0.15625f, 0.46875f));
                return frac(Ret);
            }

            void DitherObject(float t, float3 positionCS)
            {
                float mask = t;
                clip(mask - Dither32(positionCS.xy));
            }

            Varyings DepthNormalVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                // necessary only if you want to access instanced properties in the fragment Shader.
                half allScale = UNITY_ACCESS_INSTANCED_PROP(Props, _allScale);

                // 配置顶点信息
                SetVertexInfo(input, output, allScale);
                
                return output;
            }

            void DepthOnlyFragment(Varyings input, out half4 outNormalWS : SV_Target0)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                outNormalWS = half4(normalWS, 0.0);
            }
            ENDHLSL
        }
    }
}

//		Pass
//		{
//			Name "ForwardLit"
//			Tags { "LightMode" = "UniversalForwardLOD1" }
//			Cull Off
//
//			HLSLPROGRAM
//
//			// #pragma target 3.0
//			// #pragma prefer_hlslcc gles
//
//			#pragma vertex vert
//			#pragma fragment frag
//
//			// #pragma multi_compile _ ENABLE_INTERACTIVEBLENDGRASS	
//
//			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
//			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
//			// #pragma multi_compile _ _ADDITIONAL_LIGHTS
//			//#pragma multi_compile _ _PBR_LIGHTING
//			// #pragma shader_feature_local _CLOUD_SHADOW_ON
//			// #pragma shader_feature _ _CLOUD_SHADOW_ON
//			// #pragma multi_compile _ _PEROBJECTSHADOW
//			// Tone Mapping
//           //#pragma multi_compile _ _TONEMAP_ACES _TONEMAP_NEUTRAL
//			// #pragma multi_compile _ LIMITDODGESPACE
//
//			#define  _ENVIRONMENTREFLECTIONS_OFF
//			#pragma multi_compile_fog
//			#pragma multi_compile_instancing
//			
//	
//			#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/Light/SimpleAdditionalLight.hlsl"	
//
//			#include "../ShaderLibrary/ScanZoneCore.hlsl"
// 
//			
//			TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
//			TEXTURE2D(_Noisemap);  SAMPLER(sampler_Noisemap);
//
//			UNITY_INSTANCING_BUFFER_START(Props)
//			UNITY_DEFINE_INSTANCED_PROP(half4, _giColor)
//			UNITY_DEFINE_INSTANCED_PROP(half4, _terrainNormalWS)
//			UNITY_DEFINE_INSTANCED_PROP(half, _allScale)
//			//UNITY_DEFINE_INSTANCED_PROP(half, _LOD)
//			UNITY_INSTANCING_BUFFER_END(Props)
//
//
//			#define lodLevel  0
//			Varyings vert(Attributes input)
//			{
//				Varyings output = (Varyings)0;
//
//				UNITY_SETUP_INSTANCE_ID(input);
//				UNITY_TRANSFER_INSTANCE_ID(input, output); // necessary only if you want to access instanced properties in the fragment Shader.
//
//				half allScale = UNITY_ACCESS_INSTANCED_PROP(Props, _allScale);
//				//output.backGI= UNITY_ACCESS_INSTANCED_PROP(Props, _giColor);
//				VertexOutput data = GetVertexData(input, allScale, _CustomY);
//
//				float3 wPos = data.positionWS;
//				float3 pivotWS = data.pivotWS;
//				float2 noiseUV = pivotWS.xz* _Noisemap_ST.xy+_Noisemap_ST.zw;
//				WindSettings wind = PopulateWindSettings(0, _WindSpeed, _WindDirection, AO_MASK, _WindGustStrength , _WindGustFreq);
//				
//				#if lodLevel
//					float3 windVec = GetSimplerWind(wPos, pivotWS, wind, input.positionOS.y);
//				#else
//				    float4 windVec = GetWindOffset(_WindBack,wPos, pivotWS, wind,output.specular.w);
//				#endif
//
//				//暂时屏蔽，后续启用
//				//#if ENABLE_INTERACTIVEBLENDGRASS
//				//BendSettings bending = PopulateBendSettings(BEND_MASK, _BendPushStrength, _BendFlattenStrength, _PerspectiveCorrection);
//				//float4 bendVec = GetBendOffset(wPos, pivotWS, bending);
//				//#else
//				//float4 bendVec = float4(0,0,0,0);
//				//#endif
//				//wPos.xyz = lerp(windVec.xyz, bendVec.xyz, bendVec.a);
//				wPos.xyz = windVec.xyz;
//				//output.normalWS = UNITY_ACCESS_INSTANCED_PROP(Props, _terrainNormalWS)*2 - 1;
//				
//				 float3 normal = TransformObjectToWorldNormal(input.normalOS);
//				//grass Normal
//				float3 noise = SAMPLE_TEXTURE2D_LOD(_Noisemap, sampler_Noisemap, noiseUV, 0).rgb;
//					
//				//half3 N = normalize(half3(0, 1, 0) + _DiffuseNormalScale  * noise *saturate(pow(input.uv.y,_DiffuseNormalShift)));//random normal per grass
//
//				output.normalWS = normal;
//				
//				
//				output.positionCS = TransformWorldToHClip(wPos);
//				half blend = saturate(noise.x/_RootColorScale + _RootColorOffset);
//				output.color = lerp(data.color , data.randomColor, blend);
//
//				// float3 hsvColor = 
//				// hsvColor.x = RotateHue(hsvColor.x+(noise.x *2)*(50.0h/360.0h), 0.0, 1.0);
//				// output.color.rgb = HsvToRgb(hsvColor);
//				
//				output.uvAndEdge.xy = TRANSFORM_TEX(input.uv, _MainTex);
//				output.positionWSAndFogFactor = float4(wPos, ComputeFogFactor(output.positionCS.z));
//				OUTPUT_SH(output.normalWS.xyz, output.vertexSH);
//				
//				
//				#ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
//					output.shadowCoord = TransformWorldToShadowCoord(wPos);
//				#endif
//				
//				return output;
//			}
//
//			half4 frag(Varyings input, half facing : VFACE) : SV_Target
//			{
//				
//				UNITY_SETUP_INSTANCE_ID(input);			
//				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);	
//				
//				#if defined(SHADOWS_SHADOWMASK)
//					half mask = input.color.a;
//				#else
//					half mask = 1.0;
//				#endif
//
//
//				float3 positionWS = input.positionWSAndFogFactor.xyz;
//
//				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
//					float4 shadowCoord = input.shadowCoord;
//				#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
//					float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
//				#else
//					float4 shadowCoord = float4(0, 0, 0, 0);
//				#endif
//				
//
//				half weight = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uvAndEdge.xy).r;
//				
//				// half4 bottomCol = lerp(input.color, _BottomColor, _BottomWeight);
//				// half4 topCol = lerp(input.color, _TopColor, _TopWeight);
//
//				half3 albedo = input.color.xyz;
//				half3 topTipColor = albedo * (1+_GrassTipOffSet);
//				albedo = lerp(albedo, topTipColor, saturate(pow(weight,_GrassTipLighting))).rgb;
//				ScanZone_SceneColor(albedo, positionWS);
//				
//				//SurfaceData surfaceData = (SurfaceData)0;
//				InputData inputData =(InputData)0;
//				//BRDFData brdfData;
//				half3 highLightNormal = half3(0,1,0);
//				inputData.positionWS = positionWS;
//				inputData.normalWS.xyz =input.normalWS.xyz;//lerp(input.normalWS.xyz,half3(0,1,0),length(GetCameraPositionWS() - inputData.positionWS)/30);
//				
//				inputData.viewDirectionWS =  SafeNormalize(GetCameraPositionWS() - positionWS);
//				inputData.shadowCoord = shadowCoord;
//				inputData.fogCoord = input.positionWSAndFogFactor.w;
//				//inputData.bakedGI = SampleSHVertex(highLightNormal);
//				inputData.bakedGI = SampleSH(highLightNormal);
//				inputData.shadowMask =mask;
//				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
//
//				Light mainLight = GetMainLight(shadowCoord,positionWS,half4(mask,mask,mask,mask));
//				
//			    // #if _PEROBJECTSHADOW
//			    // half pershadow = SampleScreenSpaceShadowmap(float4(inputData.normalizedScreenSpaceUV,1,1));//
//			    // mainLight.shadowAttenuation = min(mainLight.shadowAttenuation, pershadow);
//			    // #endif
//
//				//全局云影
//                // #if defined(_CLOUD_SHADOW_ON)
//                // GlobalCloudShadow(inputData.positionWS,mainLight.shadowAttenuation);
//                // #endif 
//				
//				half specular=lerp(kDieletricSpec.rgb, albedo, 0);
//				mainLight.color *=_LightIntensityRatio;
//				/*surfaceData.albedo = albedo;
//				surfaceData.alpha = 1;
//				surfaceData.smoothness = _Smoothness;
//				surfaceData.metallic = 0;
//				surfaceData.specular =half3(0.0h,0.0h,0.0h); 
//
//				// NOTE: can modify alpha
//				InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);*/
//				//TerrainLighting
//				// brdfData.diffuse = albedo;
//
//
//				// #if LIMITDODGESPACE
//			 //    LimitDodgeSpace_SceneColor(albedo.rgb,inputData.positionWS);
//			 //    #endif
//
//			    //half3 gi =GlobalIllumination(brdfData,inputData.bakedGI.rgb,1,highLightNormal,inputData.viewDirectionWS);
//				
//				half3 gi =albedo* inputData.bakedGI;
//				
//				half NdotL = saturate(dot(inputData.normalWS, mainLight.direction)*0.5+0.5);
//			    half3 radiance = mainLight.color * (mainLight.shadowAttenuation * NdotL);
//			    half3 brdf = albedo * mainLight.color * mainLight.shadowAttenuation;
//
//                float3 halfDir = normalize(mainLight.direction.xyz+inputData.viewDirectionWS);  
//				float NdotH = saturate(dot(highLightNormal+half3(0,_GrassHighLightingOffect,0), halfDir));
//				float highlight=saturate(pow(input.uvAndEdge.y,_GrassHighLighting));
//				brdf+= mainLight.color * mainLight.shadowAttenuation * specular * pow(NdotH, _GrassHighLightingRange* 12.8)*highlight*_GrassHighLightingInt;
//	
//				/*#ifndef _SPECULARHIGHLIGHTS_OFF
//			    //[branch] if (!specularHighlightsOff)
//			    //{
//			
//			       brdf += mainLight.color * mainLight.shadowAttenuation * brdfData.specular * (DirectBRDFSpecular(brdfData, highLightNormal, mainLight.direction, inputData.viewDirectionWS)) * saturate(pow(input.uvAndEdge.y,_GrassHighLighting));
//				
//					 //brdf += (LightingSpecular(mainLight.color * mainLight.shadowAttenuation,mainLight.direction,highLightNormal,inputData.viewDirectionWS,brdfData.specular.xyzz,exp2(10 * surfaceData.smoothness + 1))).xyzz;
//				//}
//				#endif // _SPECULARHIGHLIGHTS_OFF*/
//				
//				half3 color = brdf + gi;
//				ScanZone_SceneColor(_WindWaveCol.rgb, inputData.positionWS);
//				half3 windcol = _WindWaveInt*saturate(smoothstep(1-2*_WindWaveRange,1,input.specular.w) * _WindWaveCol.rgb)*highlight;   //有风浪的范围
//				
//				float3 N_Shift = inputData.normalWS * _SSSValue +  mainLight.direction; 
//				half BackLight = saturate(pow(saturate(dot(-N_Shift, inputData.viewDirectionWS)), _SSSPower) * _SSSScale);
//				half3 SSS = BackLight * radiance *albedo ;
//				//return SSS.xyzz;
//				color += (SSS + windcol  *mainLight.shadowAttenuation);  
//
//				color = MixFog(color, input.positionWSAndFogFactor.w);
//				 return half4(color, 1);
//			}
//			ENDHLSL	
//		}
//	}
//
////	FallBack "Hidden/ShiYue/FallbackError"
//}
