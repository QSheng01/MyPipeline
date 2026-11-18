Shader "ShiYue/URP/VertexColorPainterShader"
{ 
    Properties
    {
        [CustomGroup] _BaseSetting("基础设置",float) = 1.0
        [CustomTexture(_BaseSetting)][MainTexture]     _BaseAlbedoASmoothness                        ("主纹理图  (A Smoothness)",2D) = "white"{}
        [CustomObject(_BaseSetting)]                                  _BaseColor                                    ("主纹理色",Color) = (1,1,1,1)
        [CustomObject(_BaseSetting)]_ShadowMapColor                                                                   ("阴影颜色", Color) = (0.0, 0.0, 0.0, 1.0)
        
        [CustomGroup] _BaseNormalSetting("基础设置法线设置",float) = 1.0
        [CustomTexture(_BaseNormalSetting,_NORMALMAP)][NoScaleOffset]            _BaseNormalMap                                ("主纹理法线",2D) = "bump"{}
        [CustomObject(_BaseNormalSetting)]                            _BumpScale                                    ("法线强度",Range(0,2)) = 1
        
        [CustomGroup] _BaseMixSetting("基础功能设置",float) = 1.0
        [CustomTexture(_BaseMixSetting)][NoScaleOffset]               _BaseAOANoiseMask                             ("主纹理功能R:金属度，G:自发光，B:AO，A:光滑度",2D) = "white"{}
        [CustomObject(_BaseMixSetting)][HDR]                          _EmissionColor                                ("自发光颜色", Color) = (0,0,0,1)
        [CustomObject(_BaseMixSetting)]                               _EmissionAndAlbedoBlender                     ("自发光颜色混合固有色强度", Range(0.0, 1.0)) = 0.0
        [CustomObject(_BaseMixSetting)]                               _BaseMetallic                                 ("金属度", Range(0.0, 1.0)) = 0
        [CustomObject(_BaseMixSetting)]                               _BaseSmoothness                               ("光滑度",Range(0,1)) = 0.5 
        [CustomObject(_BaseMixSetting)]                               _BaseAOIntensity                              ("主纹理中AO强度",Range(0,1)) = 0.5
    	[CustomObject(_BaseMixSetting)]								  _HBAOColor								    ("HBAO颜色", Color) = (0,0,0,1)
        
//        [CustomGroup] _DetailSetting("细节纹理设置",float) = 1.0         
//        [CustomTexture(_DetailSetting)][NoScaleOffset]                _DetailAlbedo                                 ("细节纹理图",2D) = "white"{}
//        [CustomObject(_DetailSetting)]                                _DetailUVScale                                ("细节纹理UV尺度",Range(0,40)) = 10
//        [CustomObject(_DetailSetting)]                                _DetailAlbedoIntensity                        ("细节纹理强度",Range(0,1)) = 0
//        [CustomTexture(_DetailSetting)][NoScaleOffset]                _DetailNormalMap                              ("细节法线图",2D) = "bump"{}
//        [CustomObject(_DetailSetting)]                                _DetailNormalMapIntensity                     ("细节法线强度",Range(0,2)) = 1
    
        [CustomGroup] _TerrainBlendSetting("地表混合设置",float) = 1.0         
        [CustomToggle(_TerrainBlendSetting, _TERRAINBLEND_ON)]        _TERRAINBLEND_ON                              ("开启地表混合",Float)=0
        [CustomObject(_TerrainBlendSetting)]                          _TerrainBlendHeight                           ("地表混合高度",Range(0,5))=0.3
        
        
        [CustomGroup] _EnvSetting("环境反射设置",float) = 1.0        
        [CustomObject(_EnvSetting)]                                  _EnvironmentReflectionsStrength                ("环境反射设置",Range(0,1))=1.0
        [HideInInspector]_BaseMap("BaseMap",2D)="white"{}
       
        [CustomGroup] _RGBCombineSetting("通道混合覆盖设置",float) = 1.0    
        [CustomToggle(_RGBCombineSetting, _OpenRGBCombineTop)]       _OpenRGBCombineTop                             ("开启通道混合覆盖模式", float) = 0
        
        [CustomGroup] _WindSetting("风效设置",float) = 0.0
        [CustomToggle(_WindSetting, _SIMPLE_WIND)]             _SIMPLE_WIND                                   ("开启风效", float) = 0
        
        [HideInInspector] _FogClose("_FogClose", Range( 0 , 1)) = 0
        
    	// --------------------------混合贴图相关-------------------------------
        [CustomGroup] _BlendSetting("刷色混合设置", Float) = 1.0
        // 第一张混合图
        [CustomToggle(_BlendSetting, _BlendMap_ON)] _BlendToggle("开启刷色混合图",float)=0.0
        [CustomTexture(_BlendSetting._BlendMap_ON)] _BlendAlbedo01Map("混合图", 2D) = "white" {}
        [CustomTexture(_BlendSetting._BlendMap_ON)] _BlendNormal01Map("混合法线图", 2D) = "bump" {}
	    [CustomObject(_BlendSetting._BlendMap_ON)] _BlendNormal01Scale("法线程度", float) = 1.0
    	[CustomTexture(_BlendSetting._BlendMap_ON)] _BlendMask01Map("混合功能R:金属度，G:自发光，B:AO，A:光滑度", 2D) = "white" {}
//    	[CustomTexture(_BlendSetting._BlendMap_ON)] _BlendNoise01Map("扰动边缘噪点图", 2D) = "white" {}
    	
    	[CustomEnum(_BlendSetting._BlendMap_ON,R,0,G,1,B,2,A,3)] _BlendChannel("顶点色影响通道", Int) = 0
    	[CustomObject(_BlendSetting._BlendMap_ON)] _BlendTilingR("混合图Tiling", Range(0.5,5)) = 1.0
    	[CustomObject(_BlendSetting._BlendMap_ON)] _BlendAmoutR("混合图权重", Range(0,1)) = 1.0
    	[CustomObject(_BlendSetting._BlendMap_ON)] _FalloffR("混合图边缘柔和程度", Range(0.0001,1)) = 1.0
    	
//    	[CustomObject(_BlendSetting._BlendMap_ON)] _BlendNoiseTilingR("边缘噪点Tiling", Range(0.1,10)) = 4.0
//    	[CustomObject(_BlendSetting._BlendMap_ON)] _BlendNoiseFalloffR("边缘噪点影响柔和", Range(0.1,10)) = 10.0
//    	[CustomObject(_BlendSetting._BlendMap_ON)] _BlendNoiseDepthR("边缘噪点影响深浅", Range(-2.0,2.0)) = 2.0
    	
    	
        // 第二张混合图
//        [CustomTexture(_BlendSetting._BlendMap_ON)] _BlendAlbedo02Map("混合图(二)", 2D) = "white" {}
//        [CustomTexture(_BlendSetting._BlendMap_ON)] _BlendNormal02Map("混合法线图(二)", 2D) = "bump" {}
//    	[CustomTexture(_BlendSetting._BlendMap_ON)] _BlendMask02Map("混合PBR信息图(二)", 2D) = "bump" {}
//        [CustomObject(_BlendSetting._BlendMap_ON)] _BlendNormal02Scale("法线程度(二)", float) = 1.0
//    	[CustomObject(_BlendSetting._BlendMap_ON)] _BlendTilingG("混合图Tiling(二)", Range(0.5,5)) = 1.0
//    	[CustomObject(_BlendSetting._BlendMap_ON)] _BlendAmoutG("混合图权重(二)", Range(0,1)) = 1.0
//    	[CustomObject(_BlendSetting._BlendMap_ON)] _FalloffG("混合图高度边缘权重(二)", Range(0,1)) = 1.0
    	
        //渲染设置
		[HideInInspector] _Cutoff("透明裁剪", Range(0.0, 1.0)) = 0.5
		[HideInInspector] _Surface("_surface", Float) = 0.0
		[HideInInspector] _TransparentShadow("__TransparentShadow", Float) = 0.0//在CustomShaderGUINew 238行附近
		[HideInInspector] _AlphaClip("_alphaclip", Float) = 0.0
        [HideInInspector] _Mode ("__mode", Float) = 0.0
		[HideInInspector] _Cull ("__cull", Float) = 2   //[Enum(Cull Off,0, Cull Front,1, Cull Back,2)]
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0
		[HideInInspector] _ZTest ("__zt", Float) = 4.0

        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
        
        //[HideInInspector]_DitherTimer("DitherTimer",Range(0,1))=1
        
    }

    SubShader
    {
        // Universal Pipeline tag is required. If Universal render pipeline is not set in the graphics settings
        // this Subshader will fail. One can add a subshader below or fallback to Standard built-in to make this
        // material work with both Universal Render Pipeline and Builtin Unity Pipeline
        Tags{"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "IgnoreProjector" = "True"}
        
//        LOD 300

        // ------------------------------------------------------------------
        //  Forward pass. Shades all light in a single pass. GI + emission + Fog 
        HLSLINCLUDE
            #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/Wind/SimpleWind.hlsl"
        ENDHLSL
        Pass
        {
            // Lightmode matches the ShaderPassName set in UniversalRenderPipeline.cs. SRPDefaultUnlit and passes with
            // no LightMode tag are also rendered by Universal Render Pipeline   abc  
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward"}         

            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull [_Cull] //Scale.x = -1,先Culloff保证效果
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
 
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            //打包需要额外开启的keyword
            #define _NORMALMAP 1
            #define ENVIRONMENT_REFLECTION_ON 1
            #define _PEROBJECTSHADOW 1
            #define _MAIN_LIGHT_SHADOWS_CASCADE 1
            #pragma shader_feature _ _ENVCUBEMAPS_ON
            #pragma shader_feature _ _ALPHATEST_ON
			#pragma shader_feature_local _ _TERRAINBLEND_ON
            #pragma shader_feature _ _CLOUD_SHADOW_ON
            #pragma shader_feature _ _OpenRGBCombineTop
            #pragma shader_feature _ _SIMPLE_WIND//简易风效
            #pragma multi_compile _ _DISSLOVE_ON
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS  
            #pragma multi_compile _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile _ _SHADOWS_SOFT 
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_local_fragment _ _BlendMap_ON
            
            #pragma multi_compile_fog

            
            //--------------------------------------
            #pragma vertex LitPassVertex 
            #pragma fragment LitPassFragment
            #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/AdditionalInput.hlsl"
            #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/Common.hlsl"
            
            #include "DetailPropsSnowInputV2.hlsl"
            #include "DetailPropsSnowPassV2.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature _ _ALPHATEST_ON
            #pragma shader_feature _ _SIMPLE_WIND//简易风效
            
            #pragma multi_compile _ _DISSLOVE_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            // #pragma multi_compile _ DOTS_INSTANCING_ON

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
			#if defined(LOD_FADE_CROSSFADE)
			    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
			#endif

            #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/Effects_Addition.hlsl"
            #include "DetailPropsSnowInputV2.hlsl"

            // #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"

            float3 _LightDirection;
			float3 _LightPosition;

            struct Attributes
			{
			    float4 positionOS   : POSITION;
			    float3 normalOS     : NORMAL;
			    float2 texcoord     : TEXCOORD0;
			    UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
			    float2 uv       : TEXCOORD0;
				float2 mainMapUV : TEXCOORD1;
			    float4 positionCS   : SV_POSITION;
			    UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			float4 GetShadowPositionHClip(Attributes input)
			{
			    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
			    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

			    #if _CASTING_PUNCTUAL_LIGHT_SHADOW
			    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
			    #else
			    float3 lightDirectionWS = _LightDirection;
			    #endif

			    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

			    #if UNITY_REVERSED_Z
			    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
			    #else
			    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
			    #endif

			    return positionCS;
			}

			Varyings ShadowPassVertex(Attributes input)
			{
			    Varyings output;
			    UNITY_SETUP_INSTANCE_ID(input);
			    UNITY_TRANSFER_INSTANCE_ID(input, output);

				output.mainMapUV = TRANSFORM_TEX(input.texcoord, _BaseAlbedoASmoothness);
			    output.uv = input.texcoord;
				
			    output.positionCS = GetShadowPositionHClip(input);
			    return output;
			}

			half4 ShadowPassFragment(Varyings input) : SV_TARGET
			{
			    UNITY_SETUP_INSTANCE_ID(input);

			    //特效接口
			    #if _DISSLOVE_ON 
			    DissloveAlpha(input.uv.xy * _DissloveTex_ST.xy + _DissloveTex_ST.zw, _DissloveEdgeLength,_DissloveThreshold);
			    #endif

			    #if defined(_ALPHATEST_ON)
			    Alpha(SampleAlbedoAlpha(input.mainMapUV, TEXTURE2D_ARGS(_BaseAlbedoASmoothness, sampler_BaseAlbedoASmoothness)).a, _BaseColor, _Cutoff);
			    #endif

			    #if defined(LOD_FADE_CROSSFADE)
			    LODFadeCrossFade(input.positionCS);
			    #endif

			    return 0;
			}
            
            ENDHLSL
        }

    	Pass
		{
			Name "DepthOnly"
			Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0
            Cull[_Cull]

			HLSLPROGRAM

			#define SHADERPASS_DEPTHONLY

			// -------------------------------------
			// Material Keywords
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma shader_feature _ _WIND_ON
			#pragma shader_feature _SCALEMAP
			#pragma shader_feature _BILLBOARD
			#pragma shader_feature _FADING

            #pragma shader_feature _ _ALPHATEST_ON
			#pragma multi_compile _ _DISSLOVE_ON
			
			// #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/StylizeTree/Shaders/Libraries/Input.hlsl"
			// #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/StylizeTree/Shaders/Libraries/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
			
			#include "DetailPropsSnowInputV2.hlsl"
			#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/Effects_Addition.hlsl"
            #include "DetailPropsSnowPassV2.hlsl"
            // #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/Wind/SimpleWind.hlsl"
			#pragma vertex LitPassVertex
			#pragma fragment DepthOnlyFragment
            
            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
            	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            	//特效接口
			    #if _DISSLOVE_ON
			    DissloveAlpha(input.uv.xy * _DissloveTex_ST.xy + _DissloveTex_ST.zw, _DissloveEdgeLength,_DissloveThreshold);
			    #endif

            	#if defined(_ALPHATEST_ON)
			    Alpha(SampleAlbedoAlpha(input.mainMapUV, TEXTURE2D_ARGS(_BaseAlbedoASmoothness, sampler_BaseAlbedoASmoothness)).a, _BaseColor, _Cutoff);
			    #endif

            	//望月剔除近处物件的Clip
            	// float alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a;
            	// //clip(alpha - _Cutoff);
            	// float distanceToCamera = distance(input.positionWS, GetCameraPositionWS());
            	// half leth = min(1, 80 / distanceToCamera);
            	// clip(lerp(alpha, (distanceToCamera - 6) / 2, saturate( 6 - distanceToCamera)) - _Cutoff * leth);

            	return 0;
            }
			ENDHLSL
		}

        // This pass is used when drawing to a _CameraNormalsTexture texture
        Pass
        {
        Name "DepthNormals" 
        Tags{"LightMode" = "DepthNormals"}

        ZWrite On
        Cull[_Cull]

        HLSLPROGRAM
        // #pragma exclude_renderers gles gles3 glcore
        // #pragma target 4.5

        #pragma vertex DepthNormalsVertex
        #pragma fragment DepthNormalsFragment

        // -------------------------------------
        // Material Keywords
        //#pragma shader_feature_local _NORMALMAP
        #pragma shader_feature_local _ _TERRAINBLEND_ON
        #define _NORMALMAP 1
        #pragma shader_feature _ _ALPHATEST_ON
        #pragma multi_compile _ _DISSLOVE_ON

        //--------------------------------------
        // GPU Instancing
        #pragma multi_compile_instancing
        

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#if defined(LOD_FADE_CROSSFADE)
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
		#endif
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
        
        #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/Effects_Addition.hlsl"
        #include "DetailPropsSnowInputV2.hlsl"
        // #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
		#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages//ShaderLibrary/TerrainBlend.hlsl"
        
		struct Attributes
		{
		    float4 positionOS     : POSITION;
		    float4 tangentOS      : TANGENT;
		    float2 texcoord     : TEXCOORD0;
		    float3 normal       : NORMAL;
		    UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		struct Varyings
		{
		    float4 positionCS   : SV_POSITION;
		    float2 uv       : TEXCOORD1;
		    float3 normalWS     : TEXCOORD2;
			float3 positionWS   : TEXCOORD3;
			float4 screenPos                : TEXCOORD4;

		    UNITY_VERTEX_INPUT_INSTANCE_ID
		    UNITY_VERTEX_OUTPUT_STEREO
		};

		void TerrainBlendWithClipZ(inout float4 positionCS, float3 worldPos,float4 screenPos,half terrainBlendHeight)
		{
			float distanceToCamera = distance(worldPos, _WorldSpaceCameraPos);
			if(distanceToCamera < 100) 
			{
				float2 screenUV = screenPos.xy/screenPos.w;
				float terrainDepth = SAMPLE_TEXTURE2D_X_LOD(_TerrainDepth, sampler_TerrainDepth, screenUV, 0).r;
				float terrainClipZ = terrainDepth * 2.0f - 1.0f;
				terrainDepth = LinearEyeDepth(terrainDepth, _ZBufferParams);
				
				half blend = saturate(abs(terrainDepth - screenPos.z) / (terrainBlendHeight));

				// clip(blend - 1.0f);
				positionCS.z = lerp(terrainClipZ, positionCS.z, blend);
			}
		}
        
		void TerrainBlendWithNormal(inout float4 positionCS, float3 worldPos,float4 screenPos,half terrainBlendHeight,inout float4 normalWS)
		{
			float distanceToCamera = distance(worldPos, _WorldSpaceCameraPos);
			if(distanceToCamera < 100) 
			{
				float2 screenUV = screenPos.xy/screenPos.w;
				half4 terrainNormal = SAMPLE_TEXTURE2D(_TerrainNormal, sampler_TerrainNormal, screenUV);
				terrainNormal = (terrainNormal * 2 - 1);

				float terrainDepth = SAMPLE_TEXTURE2D_X(_TerrainDepth, sampler_TerrainDepth, screenUV).r;
				terrainDepth = LinearEyeDepth(terrainDepth, _ZBufferParams);
				half blend = saturate(abs(terrainDepth - screenPos.z) / (terrainBlendHeight));
				normalWS.rgb = lerp(terrainNormal.rgb, normalWS, blend);
			}
		}
        
		Varyings DepthNormalsVertex(Attributes input)
		{
		    Varyings output = (Varyings)0;
		    UNITY_SETUP_INSTANCE_ID(input);
		    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);


		    output.uv = input.texcoord;

		    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

		    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normal, input.tangentOS);
		    output.normalWS = NormalizeNormalPerVertex(normalInput.normalWS);
			output.positionWS = TransformObjectToWorld(input.positionOS.xyz);

			#if defined(_TERRAINBLEND_ON)
				output.screenPos = ComputeScreenPos(output.positionCS);
				output.screenPos.z = -TransformWorldToView(output.positionWS).z;
			#endif

			// TerrainBlendWithClipZ(output.positionCS, output.positionWS, output.screenPos, _TerrainBlendHeight);
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
		        Alpha(SampleAlbedoAlpha(input.mainMapUV, TEXTURE2D_ARGS(_BaseAlbedoASmoothness, sampler_BaseAlbedoASmoothness)).a, _BaseColor, _Cutoff);
		    #endif

		    //特效接口
		    #if _DISSLOVE_ON
		    DissloveAlpha(input.uv.xy * _DissloveTex_ST.xy + _DissloveTex_ST.zw, _DissloveEdgeLength,_DissloveThreshold);
		    #endif

		    #if defined(LOD_FADE_CROSSFADE)
		        LODFadeCrossFade(input.positionCS);
		    #endif
			
		    #if defined(_GBUFFER_NORMALS_OCT)
		    float3 normalWS = normalize(input.normalWS);
		    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);           // values between [-1, +1], must use fp32 on some platforms.
		    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);   // values between [ 0,  1]
		    half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);      // values between [ 0,  1]
		    outNormalWS = half4(packedNormalWS, 0.0);
		    #else
		    float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
		    outNormalWS = half4(normalWS, 0.0);
		    #endif

		    #if defined(_TERRAINBLEND_ON)
		    TerrainBlendWithNormal(input.positionCS, input.positionWS, input.screenPos, _TerrainBlendHeight, outNormalWS);
		    #endif
			
		    #ifdef _WRITE_RENDERING_LAYERS
		        uint renderingLayers = GetMeshRenderingLayer();
		        outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
		    #endif
		}
        
        ENDHLSL
        }

        // This pass it not used during regular rendering, only for lightmap baking.
/*        Pass
        {
            Name "Meta"
            Tags{"LightMode" = "Meta"}

            Cull Off

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            #pragma vertex UniversalVertexMeta
            #pragma fragment UniversalFragmentMeta

            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local _ALPHATEST_ON

            #pragma shader_feature_local_fragment _SPECGLOSSMAP

            #include "DetailPropsSnowInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitMetaPass.hlsl"

            ENDHLSL
        }*/
//        Pass
//        {
//            Name "Universal2D"
//            Tags{ "LightMode" = "Universal2D" }
//
//            Blend[_SrcBlend][_DstBlend]
//            ZWrite[_ZWrite]
//            Cull[_Cull]
//
//            HLSLPROGRAM
//            #pragma exclude_renderers gles gles3 glcore
//            #pragma target 4.5
//
//            #pragma vertex vert
//            #pragma fragment frag
//            //#pragma shader_feature_local _ALPHATEST_ON
//            //#pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
//
//            #include "DetailPropsSnowInput.hlsl"
//            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Universal2D.hlsl"
//            ENDHLSL
//        }
    }

//    SubShader
//    {
//        // Universal Pipeline tag is required. If Universal render pipeline is not set in the graphics settings
//        // this Subshader will fail. One can add a subshader below or fallback to Standard built-in to make this
//        // material work with both Universal Render Pipeline and Builtin Unity Pipeline
//        Tags{"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "IgnoreProjector" = "True" "ShaderModel"="2.0"}
//        LOD 300
//
//        // ------------------------------------------------------------------
//        //  Forward pass. Shades all light in a single pass. GI + emission + Fog
//        Pass
//        {
//            // Lightmode matches the ShaderPassName set in UniversalRenderPipeline.cs. SRPDefaultUnlit and passes with
//            // no LightMode tag are also rendered by Universal Render Pipeline
//            Name "ForwardLit"
//            Tags{"LightMode" = "UniversalForward"}
//
//            Blend[_SrcBlend][_DstBlend]
//            ZWrite[_ZWrite]
//            Cull[_Cull]
//
//            HLSLPROGRAM
//            //#pragma only_renderers gles gles3 glcore d3d11
//            //不支持 d3d11能剩不少变体
//            #pragma only_renderers gles gles3 glcore
//            #pragma target 2.0
//
//            // -------------------------------------
//            // Material Keywords
//            #pragma shader_feature_local _NORMALMAP
//            //打包需要额外开启的keyword
//            //#define _NORMALMAP 1
//           
//            // #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
//            //#pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
//            #pragma shader_feature_local_fragment _ENVCUBEMAPS_ON
//
//            // #pragma shader_feature_local _ALPHATEST_ON
//            // #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
//
//            // #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
//			#pragma shader_feature_local _ _TERRAINBLEND_ON
//            // #pragma shader_feature_local _ _TOPENABLENOISE_ON
//            
//            // #pragma multi_compile _ _PEROBJECTSHADOW
//            // #pragma multi_compile _ LIMITDODGESPACE
//            // #pragma multi_compile _ _EFFECTSCENE_LIGHTS
//            
//            // -------------------------------------
//            // Universal Pipeline keywords
//            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
//            //#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
//            #pragma multi_compile _  _ADDITIONAL_LIGHTS
//            // #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
//            #pragma multi_compile_fragment _ _SHADOWS_SOFT
//           // #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
//            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
//            #pragma multi_compile _ SHADOWS_SHADOWMASK
//
//            
//            // -------------------------------------
//            // Unity defined keywords
//            //#pragma multi_compile _ DIRLIGHTMAP_COMBINED
//            #pragma multi_compile _ LIGHTMAP_ON
//            #pragma multi_compile_fog
//
//             //--------------------------------------
//            // Tone Mapping
//            //#pragma multi_compile _ _TONEMAP_ACES _TONEMAP_NEUTRAL
//            // #pragma multi_compile _ _DITHER    //遮挡剔除
//            // #pragma multi_compile _ _SCANZONE_ON //色相偏移
//            //--------------------------------------
//            // GPU Instancing
//            #pragma multi_compile_instancing
//            // #pragma multi_compile _ DOTS_INSTANCING_ON
//            #pragma vertex LitPassVertex
//            #pragma fragment LitPassFragment
//
//            #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/AdditionalInput.hlsl"
//            #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/Common.hlsl"
//            #include "DetailPropsSnowInput.hlsl"
//            #include "DetailPropsSnowPass.hlsl"
//            
//            ENDHLSL
//        }
//
//        Pass
//        {
//            Name "ShadowCaster"
//            Tags{"LightMode" = "ShadowCaster"}
//
//            ZWrite On
//            ZTest LEqual
//            ColorMask 0
//            Cull[_Cull]
//
//            HLSLPROGRAM
//             //#pragma only_renderers gles gles3 glcore d3d11
//			#pragma only_renderers gles gles3 glcore
//            #pragma target 2.0
//
//            //--------------------------------------
//            // GPU Instancing
//            #pragma multi_compile_instancing
//
//            // -------------------------------------
//            // Material Keywords
//            // #pragma shader_feature_local _ALPHATEST_ON
//
//            #pragma vertex ShadowPassVertex
//            #pragma fragment ShadowPassFragment
//
//            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
//            
//            #include "DetailPropsSnowInput.hlsl"
//            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
//            ENDHLSL
//        }
//		Pass
//		{
//			Name "DepthOnly"
//			Tags{"LightMode" = "DepthOnly"}
//
//            ZWrite On
//            ColorMask 0
//            Cull[_Cull]
//
//			HLSLPROGRAM
//
//			#define SHADERPASS_DEPTHONLY
//
//			// -------------------------------------
//			// Material Keywords
//			// #pragma multi_compile _ LOD_FADE_CROSSFADE
//			#pragma shader_feature _ _WIND_ON
//			#pragma shader_feature_local_vertex _SCALEMAP
//			#pragma shader_feature_local_vertex _BILLBOARD
//			#pragma shader_feature_local_fragment _FADING
//			#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/StylizeTree/Shaders/Libraries/Input.hlsl"
//			#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/StylizeTree/Shaders/Libraries/Common.hlsl"
//            // #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/Wind/SimpleWind.hlsl"
//			#pragma vertex DepthOnlyVertex
//			#pragma fragment DepthOnlyFragment
//
//
//			struct Varyings
//            {
//            	float2 uv           : TEXCOORD0;
//            	float4 positionCS   : SV_POSITION;
//            // #ifdef _ALPHATEST_ON
//            	float4 positionWS   : TEXCOORD1;
//            // #endif
//            
//            	#ifdef SHADERPASS_DEPTHNORMALS
//            	float3 normalWS		: TEXCOORD2;
//            	#endif
//            	UNITY_VERTEX_INPUT_INSTANCE_ID
//            	UNITY_VERTEX_OUTPUT_STEREO
//            };
//            
//            Varyings DepthOnlyVertex(Attributes input)
//            {
//            	Varyings output = (Varyings)0;
//            	UNITY_SETUP_INSTANCE_ID(input);
//            	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
//            	output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
//            	
//            	VertexInputs vertexInputs = GetVertexInputs(input);
//            	VertexOutput vertexData = GetVertexOutput(vertexInputs);
//
//            	output.positionCS = vertexData.positionCS;
//            // #ifdef _ALPHATEST_ON
//            	output.positionWS.xyz = vertexData.positionWS;
//            // #endif
//            	#ifdef SHADERPASS_DEPTHNORMALS
//            	output.normalWS = vertexData.normalWS;
//            	#endif
//            	return output;
//            }
//            
//            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
//            {
//            	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
//
//            	//望月剔除近处物件的Clip
//            	// float alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a;
//            	//clip(alpha - _Cutoff);
//            	// float distanceToCamera = distance(input.positionWS, GetCameraPositionWS());
//            	// half leth = min(1, 80 / distanceToCamera);
//            	// clip(lerp(alpha, (distanceToCamera - 6) / 2, saturate( 6 - distanceToCamera)) - _Cutoff * leth);
//
//            	return 0;
//            }
//			ENDHLSL
//		}
//        
//        
////        Pass
////        {
////            Name "DepthOnly"
////            Tags{"LightMode" = "DepthOnly"}
////
////            ZWrite On
////            ColorMask 0
////            Cull[_Cull]
////
////            HLSLPROGRAM
////             //#pragma only_renderers gles gles3 glcore d3d11
////			#pragma only_renderers gles gles3 glcore
////            #pragma target 2.0
////
////            //--------------------------------------
////            // GPU Instancing
////            #pragma multi_compile_instancing
////
////            #pragma vertex DepthOnlyVertex
////            #pragma fragment DepthOnlyFragment
////
////            // -------------------------------------
////            // Material Keywords
////            // #pragma shader_feature_local _ALPHATEST_ON
////
////            #include "DetailPropsSnowInput.hlsl"
////            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
////            ENDHLSL
////        }
//
//        // This pass is used when drawing to a _CameraNormalsTexture texture
////        Pass
////        {
////            Name "DepthNormals"
////            Tags{"LightMode" = "DepthNormals"}
////
////            ZWrite On
////            Cull[_Cull]
////
////            HLSLPROGRAM
////             //#pragma only_renderers gles gles3 glcore d3d11
////			// #pragma only_renderers gles gles3 glcore
////            #pragma target 2.0
////
////            #pragma vertex DepthNormalsVertex
////            #pragma fragment DepthNormalsFragment
////
////            // -------------------------------------
////            // Material Keywords
////            #pragma shader_feature_local _NORMALMAP
////            //#define _NORMALMAP 1
////            // #pragma shader_feature_local _ALPHATEST_ON
////
////            //--------------------------------------
////            // GPU Instancing
////            #pragma multi_compile_instancing
////
////            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
////
////            #include "DetailPropsSnowInput.hlsl"
////            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
////            ENDHLSL
////        }
//
//        // This pass it not used during regular rendering, only for lightmap baking.
////        Pass
////        {
////            Name "Meta"
////            Tags{"LightMode" = "Meta"}
////
////            Cull Off
////
////            HLSLPROGRAM
////             //#pragma only_renderers gles gles3 glcore d3d11
////			#pragma only_renderers gles gles3 glcore
////            #pragma target 2.0
////
////            #pragma vertex UniversalVertexMeta
////            #pragma fragment UniversalFragmentMeta
////
////            // #pragma shader_feature_local_fragment _SPECULAR_SETUP
////            // #pragma shader_feature_local _ALPHATEST_ON
////
////            #pragma shader_feature_local_fragment _SPECGLOSSMAP
////
////            #include "DetailPropsSnowInput.hlsl"
////            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitMetaPass.hlsl"
////
////            ENDHLSL
////        }
//        Pass
//        {
//            Name "Universal2D"
//            Tags{ "LightMode" = "Universal2D" }
//
//            Blend[_SrcBlend][_DstBlend]
//            ZWrite[_ZWrite]
//            Cull[_Cull]
//
//            HLSLPROGRAM
//            //#pragma only_renderers gles gles3 glcore d3d11
//			#pragma only_renderers gles gles3 glcore
//            #pragma target 2.0
//
//            #pragma vertex vert
//            #pragma fragment frag
//            // #pragma shader_feature_local _ALPHATEST_ON
//            // #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
//
//            #include "DetailPropsSnowInput.hlsl"
//            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Universal2D.hlsl"
//            ENDHLSL
//        }
//    }
	CustomEditor "CustomShaderEditor.CustomShaderGUI" 
//    FallBack "Hidden/ShiYue/FallbackError"
}
