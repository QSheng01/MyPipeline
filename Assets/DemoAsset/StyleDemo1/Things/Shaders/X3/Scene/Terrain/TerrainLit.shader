Shader "Universal Render Pipeline/Terrain/Lit"
{
    Properties
    {
        [ToggleUI] _EnableHeightBlend("EnableHeightBlend", Float) = 1.0
        _HeightTransition("Height Transition", Range(0, 1.0)) = 0.0
        // Layer count is passed down to guide height-blend enable/disable, due
        // to the fact that heigh-based blend will be broken with multipass.
        [HideInInspector] [PerRendererData] _NumLayersCount ("Total Layer Count", Float) = 1.0

        // set by terrain engine
        [HideInInspector] _Control("Control (RGBA)", 2D) = "red" {}
        [HideInInspector] _Splat3("Layer 3 (A)", 2D) = "grey" {}
        [HideInInspector] _Splat2("Layer 2 (B)", 2D) = "grey" {}
        [HideInInspector] _Splat1("Layer 1 (G)", 2D) = "grey" {}
        [HideInInspector] _Splat0("Layer 0 (R)", 2D) = "grey" {}
        [HideInInspector] _Normal3("Normal 3 (A)", 2D) = "bump" {}
        [HideInInspector] _Normal2("Normal 2 (B)", 2D) = "bump" {}
        [HideInInspector] _Normal1("Normal 1 (G)", 2D) = "bump" {}
        [HideInInspector] _Normal0("Normal 0 (R)", 2D) = "bump" {}
        [HideInInspector] _Mask3("Mask 3 (A)", 2D) = "grey" {}
        [HideInInspector] _Mask2("Mask 2 (B)", 2D) = "grey" {}
        [HideInInspector] _Mask1("Mask 1 (G)", 2D) = "grey" {}
        [HideInInspector] _Mask0("Mask 0 (R)", 2D) = "grey" {}
        [HideInInspector][Gamma] _Metallic0("Metallic 0", Range(0.0, 1.0)) = 0.0
        [HideInInspector][Gamma] _Metallic1("Metallic 1", Range(0.0, 1.0)) = 0.0
        [HideInInspector][Gamma] _Metallic2("Metallic 2", Range(0.0, 1.0)) = 0.0
        [HideInInspector][Gamma] _Metallic3("Metallic 3", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Smoothness0("Smoothness 0", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Smoothness1("Smoothness 1", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Smoothness2("Smoothness 2", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Smoothness3("Smoothness 3", Range(0.0, 1.0)) = 0.5

        // used in fallback on old cards & base map
        [HideInInspector] _MainTex("BaseMap (RGB)", 2D) = "grey" {}
        [HideInInspector] _BaseColor("Main Color", Color) = (1,1,1,1)

        [HideInInspector] _TerrainHolesTexture("Holes Map (RGB)", 2D) = "white" {}

        [ToggleUI] _EnableInstancedPerPixelNormal("Enable Instanced per-pixel normal", Float) = 1.0
    }

    HLSLINCLUDE

    // #pragma multi_compile_fragment __ _ALPHATEST_ON

    ENDHLSL

    SubShader
    {
        Tags { "Queue" = "Geometry-100" "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "IgnoreProjector" = "False" "TerrainCompatible" = "True"}

        Pass
        {
            
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM

            #pragma vertex SplatmapVert
            #pragma fragment SplatmapFragment 

            #define _METALLICSPECGLOSSMAP 1
            #define _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A 1
            #define _SCREEN_SPACE_OCCLUSION 1
            #define _PEROBJECTSHADOW 1
            #define _MAIN_LIGHT_SHADOWS_CASCADE 1
            #define _OpenCloudShadow 1
            
            // -------------------------------------
            // Universal Pipeline keywords
            // #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            // #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            // #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            // #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _SHADOWS_SOFT 
            // #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            // #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            // #pragma multi_compile_fragment _ _LIGHT_LAYERS
            // #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _FORWARD_PLUS  
            // #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            // -------------------------------------
            // Unity defined keywords
            // #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            // #pragma multi_compile _ LIGHTMAP_ON
            // #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fog
            // #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

            #pragma shader_feature_local_fragment _TERRAIN_BLEND_HEIGHT
            #pragma multi_compile _ _NORMALMAP
            #pragma multi_compile_local_fragment _ _MASKMAP
            // Sample normal in pixel shader when doing instancing
            #pragma shader_feature_local _TERRAIN_INSTANCED_PERPIXEL_NORMAL
            // #pragma multi_compile _ _RECEIVE_SHADOWS_OFF

            //��Ŀ�ڲ�
            // #pragma multi_compile _ _SCANZONE_ON //ɫ��ƫ��

            // 草的吸色有关代码, 不会收集进正式包
            #pragma shader_feature _ _GRASSDRAW

            // #pragma multi_compile _  _PEROBJECTSHADOW
            
            #define _NORMALMAP
      
            #include "TerrainLitInput.hlsl"
            #include "TerrainLitPasses.hlsl"
            
            ENDHLSL
        }

        Pass
        {
            
            Name "ForwardLit"
            Tags { "LightMode" = "PrePassColorAndDepth" }
            HLSLPROGRAM

            #pragma vertex SplatmapVert
            #pragma fragment SplatmapColorFragment 

            #define _METALLICSPECGLOSSMAP 1
            #define _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A 1
            #define _MAIN_LIGHT_SHADOWS_CASCADE 1
            // -------------------------------------
            // Universal Pipeline keywords
            // #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            // #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            // #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            // #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            // #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            // #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            // #pragma multi_compile_fragment _ _LIGHT_LAYERS
            // #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _FORWARD_PLUS
            // #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            // -------------------------------------
            // Unity defined keywords
            // #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            // #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fog
            // #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

            #pragma shader_feature_local_fragment _TERRAIN_BLEND_HEIGHT
            #pragma multi_compile _ _NORMALMAP
            #pragma multi_compile_local_fragment _ _MASKMAP
            // Sample normal in pixel shader when doing instancing
            #pragma shader_feature_local _TERRAIN_INSTANCED_PERPIXEL_NORMAL
            #define _RECEIVE_SHADOWS_OFF

            //��Ŀ�ڲ�
            // #pragma multi_compile _ _SCANZONE_ON //ɫ��ƫ��
            #define _NORMALMAP
      
            #include "TerrainLitInput.hlsl"
            #include "TerrainLitPasses.hlsl"
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ColorMask 0

            HLSLPROGRAM

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

            // -------------------------------------
            // Universal Pipeline keywords

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "TerrainLitInput.hlsl"
            #include "TerrainLitPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

            #include "TerrainLitInput.hlsl"
            #include "TerrainLitPasses.hlsl"
            ENDHLSL
        }

        // This pass is used when drawing to a _CameraNormalsTexture texture
//        Pass
//        {
//            Name "DepthNormals"
//            Tags{"LightMode" = "DepthNormals"}
//
//            ZWrite On
//
//            HLSLPROGRAM
//            #pragma vertex DepthNormalOnlyVertex
//            #pragma fragment DepthNormalOnlyFragment
//
//            #pragma shader_feature_local _NORMALMAP
//            // #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
//
//            #pragma multi_compile_instancing
//            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
//
//            #include "TerrainLitInput.hlsl"
//            #include "TerrainLitDepthNormalsPass.hlsl"
//            ENDHLSL
//        }

        Pass
        {
            Name "SceneSelectionPass"
            Tags { "LightMode" = "SceneSelectionPass" }

            HLSLPROGRAM

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

            #define SCENESELECTIONPASS
            #include "TerrainLitInput.hlsl"
            #include "TerrainLitPasses.hlsl"
            ENDHLSL
        }

        // This pass it not used during regular rendering, only for lightmap baking.
        Pass
        {
            Name "Meta"
            Tags{"LightMode" = "Meta"}

            Cull Off

            HLSLPROGRAM
            #pragma vertex TerrainVertexMeta
            #pragma fragment TerrainFragmentMeta

            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
            #pragma shader_feature EDITOR_VISUALIZATION
            #define _METALLICSPECGLOSSMAP 1
            #define _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A 1

            #include "TerrainLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitMetaPass.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "TerrainMRTOnly"
            Tags { "LightMode" = "TerrainMRTOnly" }
            HLSLPROGRAM

            #pragma vertex SplatmapVert
            #pragma fragment MRTSplatmapFragment

            #define _METALLICSPECGLOSSMAP 1
            #define _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A 1

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

            #pragma shader_feature_local_fragment _TERRAIN_BLEND_HEIGHT
            #pragma multi_compile _ _NORMALMAP
            #pragma multi_compile_local_fragment _ _MASKMAP
            // Sample normal in pixel shader when doing instancing
            #pragma shader_feature_local _TERRAIN_INSTANCED_PERPIXEL_NORMAL
            #define TERRAIN_MRT 1

            #include "TerrainLitInput.hlsl"
            #include "TerrainLitPasses.hlsl"
            ENDHLSL
        }

        // This pass is used when drawing to a _CameraNormalsTexture texture
        Pass
        {
            Name "DepthNormals"  
            Tags{"LightMode" = "DepthNormals"}

            ZWrite On

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthNormalOnlyVertex
            #pragma fragment DepthNormalOnlyFragment

            #pragma multi_compile _ _NORMALMAP
            #pragma multi_compile _ _DepthNormalTest
            // #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitDepthNormalsPass.hlsl"
            ENDHLSL
        }

//        Pass
//        {
//            Name "TerrainMRTOnlyAdd"
//            Tags { "LightMode" = "TerrainMRTOnlyAdd" }
//            Blend One One
//            HLSLPROGRAM
//
//            #pragma vertex SplatmapVert
//            #pragma fragment MRTSplatmapFragment
//
//            #define _METALLICSPECGLOSSMAP 1
//            #define _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A 1
//
//            // -------------------------------------
//            // Universal Pipeline keywords
//            #pragma multi_compile_instancing
//            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
//
//            #pragma shader_feature_local_fragment _TERRAIN_BLEND_HEIGHT
//            #pragma shader_feature_local _NORMALMAP
//            #pragma shader_feature_local_fragment _MASKMAP
//            // Sample normal in pixel shader when doing instancing
//            #pragma shader_feature_local _TERRAIN_INSTANCED_PERPIXEL_NORMAL
//            #define TERRAIN_MRT 1
//            #define TERRAIN_SPLAT_ADDPASS
//
//            #include "TerrainLitInput.hlsl"
//            #include "TerrainLitPasses.hlsl"
//            ENDHLSL
//        }
        
//        Pass
//        {
//            Name "TerrainMRTOnlyAdd"
//            Tags { "LightMode" = "TerrainMRTOnlyAdd" }
//            Blend One One
//            HLSLPROGRAM
//            // #pragma target 4.5
//
//            #pragma vertex SplatmapVert
//            #pragma fragment MRTSplatmapFragment
//
//            #define _METALLICSPECGLOSSMAP 1
//            #define _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A 1
//
//            // -------------------------------------
//            // Universal Pipeline keywords
//            #pragma multi_compile_instancing
//            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
//
//            #pragma shader_feature_local_fragment _TERRAIN_BLEND_HEIGHT
//            #pragma shader_feature_local _NORMALMAP
//            #pragma shader_feature_local_fragment _MASKMAP
//            // Sample normal in pixel shader when doing instancing
//            #pragma shader_feature_local _TERRAIN_INSTANCED_PERPIXEL_NORMAL
//            #define TERRAIN_MRT 1
//            #define TERRAIN_SPLAT_ADDPASS
//
//            #include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitInput.hlsl"
//            #include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitPasses.hlsl"
//            ENDHLSL
//        }
        
        UsePass "Hidden/Nature/Terrain/Utilities/PICKING"
    }
    Dependency "AddPassShader" = "Hidden/Universal Render Pipeline/Terrain/Lit (Add Pass)2"
//    Dependency "BaseMapShader" = "Hidden/Universal Render Pipeline/Terrain/Lit (Base Pass)" 
    Dependency "BaseMapGenShader" = "Hidden/Universal Render Pipeline/Terrain/Lit (Basemap Gen)" 

    CustomEditor "UnityEditor.Rendering.Universal.TerrainLitShaderGUI"

    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
