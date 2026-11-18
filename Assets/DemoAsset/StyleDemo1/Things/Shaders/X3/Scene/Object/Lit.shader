Shader "ShiYue/URP/Lit"
{
    Properties
    {
        // Specular vs Metallic workflow
        [HideInInspector] _WorkflowMode("WorkflowMode", Float) = 1.0

        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)

        [Toggle(_ALPHATEST_ON)]_OpenAlphaClip("开启AlphaTest", Float) = 1.0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _GlossMapScale("Smoothness Scale", Range(0.0, 1.0)) = 1.0
        _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0

        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}

        _SpecColor("Specular", Color) = (0.2, 0.2, 0.2)
        _SpecGlossMap("Specular", 2D) = "white" {}

        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _EnvironmentReflections("Environment Reflections", Float) = 1.0
        
        _EnvironmentReflectionsStrength("Environment Reflections Strength",Range(0,1))=1.0
        [Toggle] _ENVCUBEMAPS ("Open EnvCubmap", float) = 0
        [NoScaleOffset]_EnvironmentCubemap("Environment Cubemap",Cube)="Skybox"  {}

        _BumpScale("Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}

        _SecTex("Sec Texture", 2D) = "white" {} 

        _Parallax("Scale", Range(0.005, 0.08)) = 0.005
        _ParallaxMap("Height Map", 2D) = "black" {}

        _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
//        _OcclusionMap("Occlusion", 2D) = "white" {}
        [HDR]_EmissionStrength("EmissionStrength", Color) = (0,0,0,0)
        
        [HDR] _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}

        _DetailMask("Detail Mask", 2D) = "white" {}
        _DetailAlbedoMapScale("Scale", Range(0.0, 2.0)) = 1.0
        _DetailAlbedoMap("Detail Albedo x2", 2D) = "linearGrey" {}
        _DetailNormalMapScale("Scale", Range(0.0, 2.0)) = 1.0
        [Normal] _DetailNormalMap("Normal Map", 2D) = "bump" {}
        _DetailMetallicGlossMap("Detail Metallic", 2D) = "white" {}
        _DetailMetallicGlossMapInstensity("nstensity", Range(0.0, 2.0)) = 0.0
        
		[Enum(UnityEngine.Rendering.CompareFunction)]_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		[Enum(UnityEngine.Rendering.StencilOp)]_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255
        
        //特效参数
        [Toggle(_SIMPLE_WIND)]_SIMPLE_WIND ("Simple Wind", Float) = 0
        [Toggle(_DISSLOVE_ON)]_Disslove("Open Disslove",float)=0.0
        _DissloveTex("溶解贴图",  2D)= "white" {}
        _DissloveThreshold("溶解程度", Range(0,1))= 0
        _DissloveEdgeLength("溶解边缘宽度", Range(0,0.4))= 0
        [HDR]_DissloveEdgeColor("溶解边缘颜色",Color)= (0,0,0,0)
        
        [Toggle(_WINDSIN_ON)]_Wind("Open Wind",float)=0.0
        _WindStrength ("Wind Strength(XYZ)", Vector) = (0,0,0,1)
        _WindSpeed ("Wind Speed", Range(0, 10)) = 1
        _FrontDir("前后振幅", Range(0, 1)) = 0.1
        _UpDir("上下振幅", Range(0, 1)) = 0.1
        _RightDir("左右振幅", Range(0, 1)) = 0.1
        
        
        [Toggle(_TerrainBlend_ON)]_TerrainBlend("Open TerrainBlend",float)=0.0
        _TerrainBlendHeight("Terrain Blend Height",Range(0.01,2)) = 0.25 
        _TerrainBlendStrength("TerrainBlendStrength",Range(0,2)) = 1.0
        
        [Toggle(_UV_Anim)]_UV_Anim("Open UV Animation",float) = 0.0
        _UVAnimSpeed("UV Anim Speed", vector) = (0,0,0,0)

        [Toggle(_Trans_ON)]_Trans("Open Trans", float) = 0.0
        _EnvCubeMap ("EnvironmentCubeMap", Cube) = "block" {}
        _ReflectionIntensity("反射强度", Float) = 1
        _BlurReflection("模糊程度", Range( 0 , 8)) = 0
        
         [Toggle(_AmbientOcclusionByUV2)]_AmbientOcclusionByUV2("Ambient Occlusion By UV2", float) = 0.0
        
        _FogClose("_FogClose", Range( 0 , 1)) = 0
        // SRP batching compatibility for Clear Coat (Not used in Lit)
        [HideInInspector] _ClearCoatMask("_ClearCoatMask", Float) = 0.0
        [HideInInspector] _ClearCoatSmoothness("_ClearCoatSmoothness", Float) = 0.0
 
        // Blending state
        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _Blend("__blend", Float) = 0.0
        [HideInInspector] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _Cull("__cull", Float) = 2.0

        _ReceiveShadows("Receive Shadows", Float) = 1.0
        // Editmode props
        [HideInInspector] _QueueOffset("Queue offset", Float) = 0.0

        // ObsoleteProperties
        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
        [HideInInspector] _Color("Base Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _GlossMapScale("Smoothness", Float) = 0.0
        [HideInInspector] _Glossiness("Smoothness", Float) = 0.0
        [HideInInspector] _GlossyReflections("EnvironmentReflections", Float) = 0.0

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
        Tags{"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"}
//        LOD 300
        HLSLINCLUDE
            // #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/Wind/SimpleWind.hlsl"
        ENDHLSL
        //_Stencil为1即不受特效贴花影响
        Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp]
			ReadMask [_StencilWriteMask]
			WriteMask [_StencilWriteMask]
		}

        // ------------------------------------------------------------------
        //  Forward pass. Shades all light in a single pass. GI + emission + Fog
        Pass
        {
            // Lightmode matches the ShaderPassName set in UniversalRenderPipeline.cs. SRPDefaultUnlit and passes with
            // no LightMode tag are also rendered by Universal Render Pipeline
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward"}

            //Blend[_SrcBlend][_DstBlend]
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite ON
            Cull Off

            HLSLPROGRAM

            // -------------------------------------
            // Material Keywords
            //#pragma shader_feature _NORMALMAP
            //打包需要额外开启的keyword
            #pragma shader_feature _ _NORMALMAP
            #pragma shader_feature _ _METALLICSPECGLOSSMAP
            // #pragma shader_feature _ _OCCLUSIONMAP
           
            #pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature _ _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature _ _ENVCUBEMAPS_ON

            #pragma shader_feature _ _EMISSION
            #pragma shader_feature _ _ALPHATEST_ON
            #pragma shader_feature _ _ALPHAPREMULTIPLY_ON
            // #pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            
            //#pragma shader_feature _ _PARALLAXMAP
            // #pragma shader_feature _ _DETAIL_MULX2 _DETAIL_SCALED
           
            #pragma shader_feature _ _SPECULAR_SETUP
            #pragma shader_feature _ _RECEIVE_SHADOWS_OFF

            #pragma shader_feature _ _UV_Anim
            #pragma shader_feature _ _TerrainBlend_ON
            #pragma shader_feature _ _WINDSIN_ON
            #pragma shader_feature _ _Trans_ON
            //#pragma shader_feature __ _CLOUD_SHADOW_ON
            //#pragma multi_compile _ LIMITDODGESPACE
            // #pragma multi_compile _ _EFFECTSCENE_LIGHTS
            // #pragma multi_compile _ _SIMPLE_WIND
            // #pragma multi_compile _ _DISSLOVE_ON
            // -------------------------------------
            // Universal Pipeline keywords
            //#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            //默认都算光照试试
            #define _MAIN_LIGHT_SHADOWS_CASCADE
            #define _MAIN_LIGHT_SHADOWS
            // #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            //#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE 
            //#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            // #pragma multi_compile_fragment _ _LIGHT_LAYERS
            //#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            // #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            //#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            //#pragma multi_compile _ SHADOWS_SHADOWMASK

            //#pragma multi_compile _ ENVIRONMENT_DEPTH_ON

            // -------------------------------------
            // Unity defined keywords
            //#pragma multi_compile _ DIRLIGHTMAP_COMBINED
            //#pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            //#pragma multi_compile _ _CLUSTER_LIGHTS
            //--------------------------------------
            // GPU Instancing
            //#pragma multi_compile_instancing
            //#pragma multi_compile _ DOTS_INSTANCING_ON

             //--------------------------------------
            // Tone Mapping
            //#pragma multi_compile _ _TONEMAP_ACES _TONEMAP_NEUTRAL
           // #pragma multi_compile _ LIMITDODGESPACE
            //#pragma multi_compile _ _DITHER    //遮挡剔除
            // #pragma multi_compile _ _SCANZONE_ON //色相偏移
           
            
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/AdditionalInput.hlsl"
            #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/Common.hlsl"
            #include "LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
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
            // #pragma exclude_renderers gles gles3 glcore
            // #pragma target 4.5

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature _ALPHATEST_ON
            #pragma shader_feature _ _WINDSIN_ON
            // #pragma multi_compile _ _DISSLOVE_ON
            // #pragma multi_compile _ _SIMPLE_WIND

            //--------------------------------------
            // GPU Instancing
            //#pragma multi_compile_instancing
            //#pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "LitInput.hlsl"
            #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/CustomShadowCasterPass.hlsl"
            ENDHLSL
        }

//        Pass
//        {
//            // Lightmode matches the ShaderPassName set in UniversalRenderPipeline.cs. SRPDefaultUnlit and passes with
//            // no LightMode tag are also rendered by Universal Render Pipeline
//            Name "GBuffer"
//            Tags{"LightMode" = "UniversalGBuffer"} 
//
//            ZWrite[_ZWrite]
//            ZTest LEqual
//            Cull[_Cull]
//
//            HLSLPROGRAM
//            #pragma exclude_renderers gles gles3 glcore
//            #pragma target 4.5
//
//            // -------------------------------------
//            // Material Keywords
//            #pragma shader_feature _NORMALMAP
//            #pragma shader_feature _ALPHATEST_ON
//            //#pragma shader_feature _ALPHAPREMULTIPLY_ON
//            #pragma shader_feature _EMISSION
//            #pragma shader_feature _METALLICSPECGLOSSMAP
//            //#pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
//           //#pragma shader_feature _OCCLUSIONMAP
//            //#pragma shader_feature _PARALLAXMAP
//            #pragma shader_feature _ _DETAIL_MULX2 _DETAIL_SCALED
//
//            #pragma shader_feature _SPECULARHIGHLIGHTS_OFF
//            #pragma shader_feature _ENVIRONMENTREFLECTIONS_OFF
//            #pragma shader_feature _ENVCUBEMAPS_ON
//            
//            #pragma shader_feature _SPECULAR_SETUP
//            #pragma shader_feature _RECEIVE_SHADOWS_OFF
//
//            // -------------------------------------
//            // Universal Pipeline keywords
//            //#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
//            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE 
//            //#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
//            //#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
//            #pragma multi_compile _ _SHADOWS_SOFT
//            //#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
//            // -------------------------------------
//            // Unity defined keywords
//           // #pragma multi_compile _ DIRLIGHTMAP_COMBINED
//            #pragma multi_compile _ LIGHTMAP_ON
//            //#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
//
//            //--------------------------------------
//            // GPU Instancing
//            #pragma multi_compile_instancing
//            //#pragma multi_compile _ DOTS_INSTANCING_ON
//
//            #pragma vertex LitGBufferPassVertex
//            #pragma fragment LitGBufferPassFragment
//
//            #include "LitInput.hlsl"
//            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitGBufferPass.hlsl"
//            ENDHLSL
//        }

        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            // #pragma exclude_renderers gles gles3 glcore
            // #pragma target 4.5

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature _ALPHATEST_ON
            #pragma shader_feature _WINDSIN_ON
            // #pragma multi_compile _ _DISSLOVE_ON
            // #pragma multi_compile _ _SIMPLE_WIND
            //--------------------------------------
            // GPU Instancing
            //#pragma multi_compile_instancing
            //#pragma multi_compile _ DOTS_INSTANCING_ON

            #include "LitInput.hlsl"
            #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals" 
            Tags{"LightMode" = "DepthNormals"}

            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Material Keywords
            //#pragma shader_feature_local _NORMALMAP
            #define _NORMALMAP 1
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile _ _DepthNormalTest

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #include "LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }

    }

    FallBack "Hidden/ShiYue/FallbackError"
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.StylizedLitShader"
     //CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShader"
}
