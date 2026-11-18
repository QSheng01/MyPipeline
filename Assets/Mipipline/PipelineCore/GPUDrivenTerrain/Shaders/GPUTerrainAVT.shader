Shader "MPipeline/GPU Terrain AVT"
{
    Properties
    {
        _BaseMapArray ("Base Map Array", 2DArray) = "white" {}
        _MaskMapArray ("Mask Map Array", 2DArray) = "white" {}
        _SplatMap ("Material ID", 2D) = "black" {}
        _GlobalMap ("Global Map", 2D) = "white" {}
        _NearBlendDistance ("Near Blend Distance", Float) = 50
        _FarBlendDistance ("Far Blend Distance", Float) = 200
        [HideInInspector] _EnableInstancedPerPixelNormal("Instanced per pixel normal", Float) = 1
        [HideInInspector] _TerrainHolesTexture("Holes Map", 2D) = "white" {}
        [HideInInspector] _MainTex("Albedo", 2D) = "white" {}
        [HideInInspector] _Color("Color", Color) = (1,1,1,1)
        [HideInInspector] _Cutoff("Cutoff", Float) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "UniversalMaterialType" = "Lit"
            "DisableBatching" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 5.0
            #pragma vertex AvtVert
            #pragma fragment AvtForwardFrag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:SetupGPUTerrain nolightprobe nolightmap
            
            #pragma multi_compile _ _VIRTUAL_TEXTURE
            #pragma multi_compile _ _CLIPMAP_TEXTURE
            #pragma multi_compile _ _OUTPUT_PAGE_ID_TEXTURE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            #define AVT_PASS_FORWARD 1
            #define GPU_DRIVEN_TERRAIN 1
            #include "Assets/Mipipline/PipelineCore/AdaptiveVirtualTexture/Shaders/URPTerrainMaterialIDVTPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "GBuffer"
            Tags { "LightMode" = "UniversalGBuffer" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 5.0
            #pragma exclude_renderers gles3 glcore
            #pragma vertex AvtVert
            #pragma fragment AvtGBufferFrag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:SetupGPUTerrain nolightprobe nolightmap
            
            #pragma multi_compile _ _VIRTUAL_TEXTURE
            #pragma multi_compile _ _CLIPMAP_TEXTURE
            #pragma multi_compile _ _OUTPUT_PAGE_ID_TEXTURE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED
            #define AVT_PASS_GBUFFER 1
            #define GPU_DRIVEN_TERRAIN 1
            #include "Assets/Mipipline/PipelineCore/AdaptiveVirtualTexture/Shaders/URPTerrainMaterialIDVTPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma target 5.0
            #pragma vertex AvtShadowVert
            #pragma fragment AvtShadowFrag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:SetupGPUTerrain nolightprobe nolightmap
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #define AVT_PASS_SHADOW 1
            #define GPU_DRIVEN_TERRAIN 1
            #include "Assets/Mipipline/PipelineCore/AdaptiveVirtualTexture/Shaders/URPTerrainMaterialIDVTPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 5.0
            #pragma vertex AvtDepthVert
            #pragma fragment AvtDepthFrag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:SetupGPUTerrain nolightprobe nolightmap
            #define AVT_PASS_DEPTH 1
            #define GPU_DRIVEN_TERRAIN 1
            #include "Assets/Mipipline/PipelineCore/AdaptiveVirtualTexture/Shaders/URPTerrainMaterialIDVTPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma target 5.0
            #pragma vertex AvtDepthNormalsVert
            #pragma fragment AvtDepthNormalsFrag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:SetupGPUTerrain nolightprobe nolightmap
            
            #define AVT_PASS_DEPTH_NORMALS 1
            #define GPU_DRIVEN_TERRAIN 1
            #include "Assets/Mipipline/PipelineCore/AdaptiveVirtualTexture/Shaders/URPTerrainMaterialIDVTPasses.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}