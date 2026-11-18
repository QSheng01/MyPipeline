Shader "Unlit/GenerateClouds"
{
    Properties
    {
        Color_7eff2e7fc27440719d0a871a39dabdd3("Tint", Color) = (0.8584461, 0.9356887, 0.9528302, 0)
        Vector1_2a375228e8d3437382814520b0cd8644("Density", Range(0.1, 4)) = 2
        Vector1_5f4b185241524942830132fc71554a33("Ambient Intensity", Range(0, 2)) = 1
        Vector1_2d1f70e8efb04843b1f8184ae06925e0("Thickness", Float) = 100
        Vector1_33982692("HeightFogRange", Float) = 150
        Vector1_10DA9AB3("HeighFogOffset", Float) = 0
        Vector2_65050940ec244ae3bf42db206c2dccca("Start/End Depth Fade", Vector) = (0, 1, 0, 0)
        [NoScaleOffset]Texture2D_4B382D9C("Map1 (R:Left/G:Up/B:Right/A:Down)", 2D) = "white" {}
        [NoScaleOffset]Texture2D_3DC46A50("Map2 (R:Front/G:Back/B:Depth/A:Alpha)", 2D) = "white" {}
        [Toggle]BOOLEAN_EAB072D9159A48C685623C0E39EA73B0("Soft Particles", Float) = 0
        [Toggle]BOOLEAN_9A9F210EF4D44680B8F0CB7ACD99DFBB("Blue Noise", Float) = 0
        [NonModifiableTextureData][NoScaleOffset]_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D("Texture2D", 2D) = "white" {}
        [HideInInspector]_QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector]_QueueControl("_QueueControl", Float) = -1
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
//            "UniversalMaterialType" = "Unlit"
            "Queue"="Transparent-1"
//            "Queue"="AlphaTest+1"
            
//            "DisableBatching"="False"
//            "ShaderGraphShader"="true"
//            "ShaderGraphTargetId"="UniversalUnlitSubTarget"
        }
        Pass
        {
            Name "Universal Forward"
            Tags
            {
                "LightMode" = "UniveralCloud"
            }
        
        // Render State
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ZTest LEqual
        ZWrite Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma multi_compile_instancing
        #pragma multi_compile_fog
        #pragma instancing_options renderinglayer
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile _ LIGHTMAP_ON
        #pragma multi_compile _ DIRLIGHTMAP_COMBINED
        #pragma shader_feature _ _SAMPLE_GI
        #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
        #pragma multi_compile_fragment _ DEBUG_DISPLAY
        #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
        #pragma shader_feature_local _ BOOLEAN_EAB072D9159A48C685623C0E39EA73B0_ON
        #pragma shader_feature_local _ BOOLEAN_9A9F210EF4D44680B8F0CB7ACD99DFBB_ON
        #pragma multi_compile_local _ RECONSTRUCT
        
        #if defined(BOOLEAN_EAB072D9159A48C685623C0E39EA73B0_ON) && defined(BOOLEAN_9A9F210EF4D44680B8F0CB7ACD99DFBB_ON)
            #define KEYWORD_PERMUTATION_0
        #elif defined(BOOLEAN_EAB072D9159A48C685623C0E39EA73B0_ON)
            #define KEYWORD_PERMUTATION_1
        #elif defined(BOOLEAN_9A9F210EF4D44680B8F0CB7ACD99DFBB_ON)
            #define KEYWORD_PERMUTATION_2
        #else
            #define KEYWORD_PERMUTATION_3
        #endif
        
        
        // Defines
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_NORMAL
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_TANGENT
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_TEXCOORD0
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define VARYINGS_NEED_POSITION_WS
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define VARYINGS_NEED_NORMAL_WS
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define VARYINGS_NEED_TANGENT_WS
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define VARYINGS_NEED_TEXCOORD0
        #endif
        
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_UNLIT
        #define _FOG_FRAGMENT 1
        #define _SURFACE_TYPE_TRANSPARENT 1
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define REQUIRE_DEPTH_TEXTURE
        #endif
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 positionOS : POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 normalOS : NORMAL;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 tangentOS : TANGENT;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 uv0 : TEXCOORD0;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
            #endif
        };
        struct Varyings
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 positionCS : SV_POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 positionWS;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 normalWS;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 tangentWS;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 texCoord0;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
            #endif
        };
        struct SurfaceDescriptionInputs
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 WorldSpaceNormal;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 WorldSpaceTangent;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 WorldSpaceBiTangent;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 WorldSpaceViewDirection;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpacePosition;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 WorldSpacePosition;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 ScreenPosition;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float2 NDCPosition;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float2 PixelPosition;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 uv0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 TimeParameters;
            #endif
        };
        struct VertexDescriptionInputs
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpaceNormal;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpaceTangent;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpacePosition;
            #endif
        };
        struct PackedVaryings
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 positionCS : SV_POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 tangentWS : INTERP0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 texCoord0 : INTERP1;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 positionWS : INTERP2;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 normalWS : INTERP3;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
            #endif
        };
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.tangentWS.xyzw = input.tangentWS;
            output.texCoord0.xyzw = input.texCoord0;
            output.positionWS.xyz = input.positionWS;
            output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.tangentWS = input.tangentWS.xyzw;
            output.texCoord0 = input.texCoord0.xyzw;
            output.positionWS = input.positionWS.xyz;
            output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        #endif
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D_TexelSize;
        float4 Color_7eff2e7fc27440719d0a871a39dabdd3;
        float Vector1_2a375228e8d3437382814520b0cd8644;
        float Vector1_5f4b185241524942830132fc71554a33;
        float Vector1_2d1f70e8efb04843b1f8184ae06925e0;
        float Vector1_33982692;
        float Vector1_10DA9AB3;
        float2 Vector2_65050940ec244ae3bf42db206c2dccca;
        float4 Texture2D_4B382D9C_TexelSize;
        float4 Texture2D_3DC46A50_TexelSize;
        float4x4 _BA_SkyboxMatrix;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D);
        SAMPLER(sampler_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D);
        TEXTURE2D(Texture2D_4B382D9C);
        SAMPLER(samplerTexture2D_4B382D9C);
        TEXTURE2D(Texture2D_3DC46A50);
        SAMPLER(samplerTexture2D_3DC46A50);
        float _BA_SkyboxRatio;
        float4 _BA_CloudData;
        
        // Graph Includes
        #include "Assets/SYPackages/ShiyueFlexiblePipline/FlexiblePackages/3DSkybox/Shaders/UtilityGraphs/CustomLighting.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Normalize_float3(float3 In, out float3 Out)
        {
            Out = normalize(In);
        }
        
        void Unity_Negate_half3(half3 In, out half3 Out)
        {
            Out = -1 * In;
        }
        
        void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
        {
            Out = dot(A, B);
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
        {
            Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
        }
        
        void Unity_MatrixConstruction_Row_float (float4 M0, float4 M1, float4 M2, float4 M3, out float4x4 Out4x4, out float3x3 Out3x3, out float2x2 Out2x2)
        {
            Out4x4 = float4x4(M0.x, M0.y, M0.z, M0.w, M1.x, M1.y, M1.z, M1.w, M2.x, M2.y, M2.z, M2.w, M3.x, M3.y, M3.z, M3.w);
            Out3x3 = float3x3(M0.x, M0.y, M0.z, M1.x, M1.y, M1.z, M2.x, M2.y, M2.z);
            Out2x2 = float2x2(M0.x, M0.y, M1.x, M1.y);
        }
        
        void Unity_Branch_half3(half Predicate, half3 True, half3 False, out half3 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Branch_half(half Predicate, half True, half False, out half Out)
        {
            Out = Predicate ? True : False;
        }
        
        struct Bindings_GetMainLight_2ff6b2f9d6ff940dfa76d3c0d3f9c2ae_half
        {
        };
        
        void SG_GetMainLight_2ff6b2f9d6ff940dfa76d3c0d3f9c2ae_half(float3 PosisionWS_shadows, half Shadows, Bindings_GetMainLight_2ff6b2f9d6ff940dfa76d3c0d3f9c2ae_half IN, out half3 Direction_1, out half3 Color_2, out half Distance_Attenuation_3, out half Shadow_Attenuation_4)
        {
        half _Property_83b0ca4bb8be486292ac152fdc964bbb_Out_0_Boolean = Shadows;
        float3 _Property_b0870e822b434bb682becc8dba97af13_Out_0_Vector3 = PosisionWS_shadows;
        half3 _MainLightCustomFunction_e3f5c53147a845a4ae3955da5a4556a6_Direction_1_Vector3;
        half3 _MainLightCustomFunction_e3f5c53147a845a4ae3955da5a4556a6_Color_2_Vector3;
        half _MainLightCustomFunction_e3f5c53147a845a4ae3955da5a4556a6_DistanceAtten_3_Float;
        half _MainLightCustomFunction_e3f5c53147a845a4ae3955da5a4556a6_ShadowAtten_4_Float;
        MainLight_half(_Property_b0870e822b434bb682becc8dba97af13_Out_0_Vector3, _MainLightCustomFunction_e3f5c53147a845a4ae3955da5a4556a6_Direction_1_Vector3, _MainLightCustomFunction_e3f5c53147a845a4ae3955da5a4556a6_Color_2_Vector3, _MainLightCustomFunction_e3f5c53147a845a4ae3955da5a4556a6_DistanceAtten_3_Float, _MainLightCustomFunction_e3f5c53147a845a4ae3955da5a4556a6_ShadowAtten_4_Float);
        half3 _MainLightCustomFunction_7e2be557229449ba91e955c4804551f2_Direction_1_Vector3;
        half3 _MainLightCustomFunction_7e2be557229449ba91e955c4804551f2_Color_2_Vector3;
        half _MainLightCustomFunction_7e2be557229449ba91e955c4804551f2_DistanceAtten_3_Float;
        half _MainLightCustomFunction_7e2be557229449ba91e955c4804551f2_ShadowAtten_4_Float;
        MainLight_half(_MainLightCustomFunction_7e2be557229449ba91e955c4804551f2_Direction_1_Vector3, _MainLightCustomFunction_7e2be557229449ba91e955c4804551f2_Color_2_Vector3, _MainLightCustomFunction_7e2be557229449ba91e955c4804551f2_DistanceAtten_3_Float, _MainLightCustomFunction_7e2be557229449ba91e955c4804551f2_ShadowAtten_4_Float);
        half3 _Branch_207e005c167c40fdb9101a28fc4f95b8_Out_3_Vector3;
        Unity_Branch_half3(_Property_83b0ca4bb8be486292ac152fdc964bbb_Out_0_Boolean, _MainLightCustomFunction_e3f5c53147a845a4ae3955da5a4556a6_Direction_1_Vector3, _MainLightCustomFunction_7e2be557229449ba91e955c4804551f2_Direction_1_Vector3, _Branch_207e005c167c40fdb9101a28fc4f95b8_Out_3_Vector3);
        half3 _Branch_1a60dec68ac24bc9a0ed5e320aa55c21_Out_3_Vector3;
        Unity_Branch_half3(_Property_83b0ca4bb8be486292ac152fdc964bbb_Out_0_Boolean, _MainLightCustomFunction_e3f5c53147a845a4ae3955da5a4556a6_Color_2_Vector3, _MainLightCustomFunction_7e2be557229449ba91e955c4804551f2_Color_2_Vector3, _Branch_1a60dec68ac24bc9a0ed5e320aa55c21_Out_3_Vector3);
        half _Branch_3ac29ca8debd497b8c3657dd145fd1f5_Out_3_Float;
        Unity_Branch_half(_Property_83b0ca4bb8be486292ac152fdc964bbb_Out_0_Boolean, _MainLightCustomFunction_e3f5c53147a845a4ae3955da5a4556a6_DistanceAtten_3_Float, _MainLightCustomFunction_7e2be557229449ba91e955c4804551f2_DistanceAtten_3_Float, _Branch_3ac29ca8debd497b8c3657dd145fd1f5_Out_3_Float);
        half _Branch_c39962fa80b14a2c988108bebd64b8b5_Out_3_Float;
        Unity_Branch_half(_Property_83b0ca4bb8be486292ac152fdc964bbb_Out_0_Boolean, _MainLightCustomFunction_e3f5c53147a845a4ae3955da5a4556a6_ShadowAtten_4_Float, _MainLightCustomFunction_7e2be557229449ba91e955c4804551f2_ShadowAtten_4_Float, _Branch_c39962fa80b14a2c988108bebd64b8b5_Out_3_Float);
        Direction_1 = _Branch_207e005c167c40fdb9101a28fc4f95b8_Out_3_Vector3;
        Color_2 = _Branch_1a60dec68ac24bc9a0ed5e320aa55c21_Out_3_Vector3;
        Distance_Attenuation_3 = _Branch_3ac29ca8debd497b8c3657dd145fd1f5_Out_3_Float;
        Shadow_Attenuation_4 = _Branch_c39962fa80b14a2c988108bebd64b8b5_Out_3_Float;
        }
        
        void Unity_Multiply_float3x3_float3(float3x3 A, float3 B, out float3 Out)
        {
            Out = mul(A, B);
        }
        
        void Unity_Comparison_Greater_float(float A, float B, out float Out)
        {
            Out = A > B ? 1 : 0;
        }
        
        void Unity_Branch_float(float Predicate, float True, float False, out float Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
        Out = A * B;
        }
        
        void Unity_OneMinus_float(float In, out float Out)
        {
            Out = 1 - In;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
        Out = A * B;
        }
        
        struct Bindings_ComputeLightmap_e43f2878967dc15488276fdf96a2a634_float
        {
        };
        
        void SG_ComputeLightmap_e43f2878967dc15488276fdf96a2a634_float(float3 Vector3_9655A41F, float Vector1_f2d1bb31b65f491b983d70953e90dc1b, float Vector1_a7dc919dc3914d68820d1d4a7acd18d3, float Vector1_68860be670804442b8003a5507c34868, float Vector1_0f718eb5ca284ababccf9f0579ddb459, float Vector1_61c23c13d0084c0580a431ec04603dea, float Vector1_7b99b0043d4e49bc85916b8f52cf0731, Bindings_ComputeLightmap_e43f2878967dc15488276fdf96a2a634_float IN, out float Lightmap_0)
        {
        float3 _Property_ce5e9d55f39c454a8ab806bfc12c14aa_Out_0_Vector3 = Vector3_9655A41F;
        float _Split_e13693a230a34ab3986dad4b4569e534_R_1_Float = _Property_ce5e9d55f39c454a8ab806bfc12c14aa_Out_0_Vector3[0];
        float _Split_e13693a230a34ab3986dad4b4569e534_G_2_Float = _Property_ce5e9d55f39c454a8ab806bfc12c14aa_Out_0_Vector3[1];
        float _Split_e13693a230a34ab3986dad4b4569e534_B_3_Float = _Property_ce5e9d55f39c454a8ab806bfc12c14aa_Out_0_Vector3[2];
        float _Split_e13693a230a34ab3986dad4b4569e534_A_4_Float = 0;
        float _Comparison_1e5e00116d854b06af88c0fd460d1dc4_Out_2_Boolean;
        Unity_Comparison_Greater_float(_Split_e13693a230a34ab3986dad4b4569e534_R_1_Float, 0, _Comparison_1e5e00116d854b06af88c0fd460d1dc4_Out_2_Boolean);
        float _Property_2e9c288ce7e84f52b897fb7a72f43e78_Out_0_Float = Vector1_a7dc919dc3914d68820d1d4a7acd18d3;
        float _Property_3a182b1eb3724a8188d1afeb263574e9_Out_0_Float = Vector1_f2d1bb31b65f491b983d70953e90dc1b;
        float _Branch_aeed904fb23d46d9912defd4c7a4fb67_Out_3_Float;
        Unity_Branch_float(_Comparison_1e5e00116d854b06af88c0fd460d1dc4_Out_2_Boolean, _Property_2e9c288ce7e84f52b897fb7a72f43e78_Out_0_Float, _Property_3a182b1eb3724a8188d1afeb263574e9_Out_0_Float, _Branch_aeed904fb23d46d9912defd4c7a4fb67_Out_3_Float);
        float _Comparison_9cecd5f696d141079c57564c06d6f2f8_Out_2_Boolean;
        Unity_Comparison_Greater_float(_Split_e13693a230a34ab3986dad4b4569e534_G_2_Float, 0, _Comparison_9cecd5f696d141079c57564c06d6f2f8_Out_2_Boolean);
        float _Property_11c03cd032ff41be8984dcc68f22630d_Out_0_Float = Vector1_68860be670804442b8003a5507c34868;
        float _Property_5c949d5857b34549b8f81dd5f1aa2e2b_Out_0_Float = Vector1_0f718eb5ca284ababccf9f0579ddb459;
        float _Branch_86ad0a5a70284ed691378e8e2c4fbebe_Out_3_Float;
        Unity_Branch_float(_Comparison_9cecd5f696d141079c57564c06d6f2f8_Out_2_Boolean, _Property_11c03cd032ff41be8984dcc68f22630d_Out_0_Float, _Property_5c949d5857b34549b8f81dd5f1aa2e2b_Out_0_Float, _Branch_86ad0a5a70284ed691378e8e2c4fbebe_Out_3_Float);
        float _Comparison_033a823dcf5d4dfcb5b2de42cd5a6ef6_Out_2_Boolean;
        Unity_Comparison_Greater_float(_Split_e13693a230a34ab3986dad4b4569e534_B_3_Float, 0, _Comparison_033a823dcf5d4dfcb5b2de42cd5a6ef6_Out_2_Boolean);
        float _Property_d90c9e08c6d74912a268e5d56924fc12_Out_0_Float = Vector1_f2d1bb31b65f491b983d70953e90dc1b;
        float _Property_934c7456f2df4a62bb7afaaa61d427c0_Out_0_Float = Vector1_a7dc919dc3914d68820d1d4a7acd18d3;
        float _Add_25457a2c84844176be6346e3a7256033_Out_2_Float;
        Unity_Add_float(_Property_d90c9e08c6d74912a268e5d56924fc12_Out_0_Float, _Property_934c7456f2df4a62bb7afaaa61d427c0_Out_0_Float, _Add_25457a2c84844176be6346e3a7256033_Out_2_Float);
        float _Property_530c9025c6094d17a698f0dcc469ab09_Out_0_Float = Vector1_68860be670804442b8003a5507c34868;
        float _Property_b937847fb65d4877a697164e42edcab5_Out_0_Float = Vector1_0f718eb5ca284ababccf9f0579ddb459;
        float _Add_a204922169ba4fe1a355a57b6e17b8db_Out_2_Float;
        Unity_Add_float(_Property_530c9025c6094d17a698f0dcc469ab09_Out_0_Float, _Property_b937847fb65d4877a697164e42edcab5_Out_0_Float, _Add_a204922169ba4fe1a355a57b6e17b8db_Out_2_Float);
        float _Add_9e0c65317448445fbd34e99bfbcd9292_Out_2_Float;
        Unity_Add_float(_Add_25457a2c84844176be6346e3a7256033_Out_2_Float, _Add_a204922169ba4fe1a355a57b6e17b8db_Out_2_Float, _Add_9e0c65317448445fbd34e99bfbcd9292_Out_2_Float);
        float _Multiply_170935fccad0476a9cffa68f100474a7_Out_2_Float;
        Unity_Multiply_float_float(0.25, _Add_9e0c65317448445fbd34e99bfbcd9292_Out_2_Float, _Multiply_170935fccad0476a9cffa68f100474a7_Out_2_Float);
        float _Power_82b43d2499284a5584b1b3af44f4522b_Out_2_Float;
        Unity_Power_float(_Multiply_170935fccad0476a9cffa68f100474a7_Out_2_Float, 1, _Power_82b43d2499284a5584b1b3af44f4522b_Out_2_Float);
        float _Multiply_1d0ab76e4d9044e3a86b72a9530b3594_Out_2_Float;
        Unity_Multiply_float_float(_Power_82b43d2499284a5584b1b3af44f4522b_Out_2_Float, 1, _Multiply_1d0ab76e4d9044e3a86b72a9530b3594_Out_2_Float);
        float _Property_59a2a6186be74ebd8f14604da37a4e6a_Out_0_Float = Vector1_7b99b0043d4e49bc85916b8f52cf0731;
        #if defined(RECONSTRUCT)
        float _Reconstruct_ea4d77a8026046389ad824a57617aa6c_Out_0_Float = _Multiply_1d0ab76e4d9044e3a86b72a9530b3594_Out_2_Float;
        #else
        float _Reconstruct_ea4d77a8026046389ad824a57617aa6c_Out_0_Float = _Property_59a2a6186be74ebd8f14604da37a4e6a_Out_0_Float;
        #endif
        float _OneMinus_d9a8403f4fbb443587c8c8cf65e4386b_Out_1_Float;
        Unity_OneMinus_float(_Power_82b43d2499284a5584b1b3af44f4522b_Out_2_Float, _OneMinus_d9a8403f4fbb443587c8c8cf65e4386b_Out_1_Float);
        float _Property_e29a8bfbd8314e05a19b6dc2e83d80e4_Out_0_Float = Vector1_61c23c13d0084c0580a431ec04603dea;
        #if defined(RECONSTRUCT)
        float _Reconstruct_d303a71765764f86bafd424e496f53d9_Out_0_Float = _OneMinus_d9a8403f4fbb443587c8c8cf65e4386b_Out_1_Float;
        #else
        float _Reconstruct_d303a71765764f86bafd424e496f53d9_Out_0_Float = _Property_e29a8bfbd8314e05a19b6dc2e83d80e4_Out_0_Float;
        #endif
        float _Branch_84ce803260cc450a9e4b0b3233c63858_Out_3_Float;
        Unity_Branch_float(_Comparison_033a823dcf5d4dfcb5b2de42cd5a6ef6_Out_2_Boolean, _Reconstruct_ea4d77a8026046389ad824a57617aa6c_Out_0_Float, _Reconstruct_d303a71765764f86bafd424e496f53d9_Out_0_Float, _Branch_84ce803260cc450a9e4b0b3233c63858_Out_3_Float);
        float4 _Combine_3ef3559aca414f209f80d4fdeff0dc1f_RGBA_4_Vector4;
        float3 _Combine_3ef3559aca414f209f80d4fdeff0dc1f_RGB_5_Vector3;
        float2 _Combine_3ef3559aca414f209f80d4fdeff0dc1f_RG_6_Vector2;
        Unity_Combine_float(_Branch_aeed904fb23d46d9912defd4c7a4fb67_Out_3_Float, _Branch_86ad0a5a70284ed691378e8e2c4fbebe_Out_3_Float, _Branch_84ce803260cc450a9e4b0b3233c63858_Out_3_Float, 0, _Combine_3ef3559aca414f209f80d4fdeff0dc1f_RGBA_4_Vector4, _Combine_3ef3559aca414f209f80d4fdeff0dc1f_RGB_5_Vector3, _Combine_3ef3559aca414f209f80d4fdeff0dc1f_RG_6_Vector2);
        float3 _Property_1d2958867d61407db62ad0c7604522c3_Out_0_Vector3 = Vector3_9655A41F;
        float3 _Multiply_cee5f49181424d4289c6b0e8c7bb7eaa_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Property_1d2958867d61407db62ad0c7604522c3_Out_0_Vector3, _Property_1d2958867d61407db62ad0c7604522c3_Out_0_Vector3, _Multiply_cee5f49181424d4289c6b0e8c7bb7eaa_Out_2_Vector3);
        float3 _Multiply_097a099125e64ffdb11753f3740116b4_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Combine_3ef3559aca414f209f80d4fdeff0dc1f_RGB_5_Vector3, _Multiply_cee5f49181424d4289c6b0e8c7bb7eaa_Out_2_Vector3, _Multiply_097a099125e64ffdb11753f3740116b4_Out_2_Vector3);
        float _Split_a6dc1283949b437c8da1331a910ab018_R_1_Float = _Multiply_097a099125e64ffdb11753f3740116b4_Out_2_Vector3[0];
        float _Split_a6dc1283949b437c8da1331a910ab018_G_2_Float = _Multiply_097a099125e64ffdb11753f3740116b4_Out_2_Vector3[1];
        float _Split_a6dc1283949b437c8da1331a910ab018_B_3_Float = _Multiply_097a099125e64ffdb11753f3740116b4_Out_2_Vector3[2];
        float _Split_a6dc1283949b437c8da1331a910ab018_A_4_Float = 0;
        float _Add_ce8a59efcd114a4f970997e85ddff9cc_Out_2_Float;
        Unity_Add_float(_Split_a6dc1283949b437c8da1331a910ab018_R_1_Float, _Split_a6dc1283949b437c8da1331a910ab018_G_2_Float, _Add_ce8a59efcd114a4f970997e85ddff9cc_Out_2_Float);
        float _Add_506ca34f7e3042c98424b6595837e0a6_Out_2_Float;
        Unity_Add_float(_Add_ce8a59efcd114a4f970997e85ddff9cc_Out_2_Float, _Split_a6dc1283949b437c8da1331a910ab018_B_3_Float, _Add_506ca34f7e3042c98424b6595837e0a6_Out_2_Float);
        Lightmap_0 = _Add_506ca34f7e3042c98424b6595837e0a6_Out_2_Float;
        }
        
        void Unity_Maximum_float(float A, float B, out float Out)
        {
            Out = max(A, B);
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        void Unity_Fog_float(out float4 Color, out float Density, float3 Position)
        {
            SHADERGRAPH_FOG(Position, Color, Density);
        }
        
        void Unity_Divide_float(float A, float B, out float Out)
        {
            Out = A / B;
        }
        
        void Unity_Exponential2_float(float In, out float Out)
        {
            Out = exp2(In);
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Blend_Screen_float(float Base, float Blend, out float Out, float Opacity)
        {
            Out = 1.0 - (1.0 - Blend) * (1.0 - Base);
            Out = lerp(Base, Out, Opacity);
        }
        
        struct Bindings_URPExponentialFog_a30e9a21a0b9141cbaaf335f79e3f00f_float
        {
        float3 ObjectSpacePosition;
        float3 WorldSpacePosition;
        };
        
        void SG_URPExponentialFog_a30e9a21a0b9141cbaaf335f79e3f00f_float(float Vector1_548c23b8a28a4acba12d5e42e8de4dd5, float Vector1_8a01e7e97fac46d3a084c24499c15b97, float4x4 Matrix4_a983a30e479e43a8b1f83b5f8372c5cd, float Vector1_8d1279a725f04004abeec8eb29628190, Bindings_URPExponentialFog_a30e9a21a0b9141cbaaf335f79e3f00f_float IN, out float3 Color_2, out float Density_1)
        {
        float4 _Fog_86d7de5382804677a9f8a6a7344cd56d_Color_0_Vector4;
        float _Fog_86d7de5382804677a9f8a6a7344cd56d_Density_1_Float;
        Unity_Fog_float(_Fog_86d7de5382804677a9f8a6a7344cd56d_Color_0_Vector4, _Fog_86d7de5382804677a9f8a6a7344cd56d_Density_1_Float, IN.ObjectSpacePosition);
        float _Property_2cee890886a049a8841a04b43b9c3c45_Out_0_Float = Vector1_8d1279a725f04004abeec8eb29628190;
        float _Divide_e16e72a863c843eb9cb0ac9e2124bb84_Out_2_Float;
        Unity_Divide_float(_Fog_86d7de5382804677a9f8a6a7344cd56d_Density_1_Float, _Property_2cee890886a049a8841a04b43b9c3c45_Out_0_Float, _Divide_e16e72a863c843eb9cb0ac9e2124bb84_Out_2_Float);
        float _Multiply_bc9c608a44464ec997b959515ad7a79b_Out_2_Float;
        Unity_Multiply_float_float(_Divide_e16e72a863c843eb9cb0ac9e2124bb84_Out_2_Float, _Divide_e16e72a863c843eb9cb0ac9e2124bb84_Out_2_Float, _Multiply_bc9c608a44464ec997b959515ad7a79b_Out_2_Float);
        float _OneMinus_c9dea61bb1d140a1bc8c5473a80c2d67_Out_1_Float;
        Unity_OneMinus_float(_Multiply_bc9c608a44464ec997b959515ad7a79b_Out_2_Float, _OneMinus_c9dea61bb1d140a1bc8c5473a80c2d67_Out_1_Float);
        float _Exponential_a3837813a78044a18b0bd627883f4d75_Out_1_Float;
        Unity_Exponential2_float(_OneMinus_c9dea61bb1d140a1bc8c5473a80c2d67_Out_1_Float, _Exponential_a3837813a78044a18b0bd627883f4d75_Out_1_Float);
        float _Split_93d0ab1c074f48ea9aa174cdff5e4b2b_R_1_Float = IN.WorldSpacePosition[0];
        float _Split_93d0ab1c074f48ea9aa174cdff5e4b2b_G_2_Float = IN.WorldSpacePosition[1];
        float _Split_93d0ab1c074f48ea9aa174cdff5e4b2b_B_3_Float = IN.WorldSpacePosition[2];
        float _Split_93d0ab1c074f48ea9aa174cdff5e4b2b_A_4_Float = 0;
        float4 _Combine_d3998e4489754cf5a0c127962cfc7786_RGBA_4_Vector4;
        float3 _Combine_d3998e4489754cf5a0c127962cfc7786_RGB_5_Vector3;
        float2 _Combine_d3998e4489754cf5a0c127962cfc7786_RG_6_Vector2;
        Unity_Combine_float(_Split_93d0ab1c074f48ea9aa174cdff5e4b2b_R_1_Float, _Split_93d0ab1c074f48ea9aa174cdff5e4b2b_G_2_Float, _Split_93d0ab1c074f48ea9aa174cdff5e4b2b_B_3_Float, 1, _Combine_d3998e4489754cf5a0c127962cfc7786_RGBA_4_Vector4, _Combine_d3998e4489754cf5a0c127962cfc7786_RGB_5_Vector3, _Combine_d3998e4489754cf5a0c127962cfc7786_RG_6_Vector2);
        float _Split_447a21f9b4ed4de289a42265b8854724_R_1_Float = _Combine_d3998e4489754cf5a0c127962cfc7786_RGBA_4_Vector4[0];
        float _Split_447a21f9b4ed4de289a42265b8854724_G_2_Float = _Combine_d3998e4489754cf5a0c127962cfc7786_RGBA_4_Vector4[1];
        float _Split_447a21f9b4ed4de289a42265b8854724_B_3_Float = _Combine_d3998e4489754cf5a0c127962cfc7786_RGBA_4_Vector4[2];
        float _Split_447a21f9b4ed4de289a42265b8854724_A_4_Float = _Combine_d3998e4489754cf5a0c127962cfc7786_RGBA_4_Vector4[3];
        float _Split_1acdb116d9fa41b8be55b3978ea78a48_R_1_Float = _WorldSpaceCameraPos[0];
        float _Split_1acdb116d9fa41b8be55b3978ea78a48_G_2_Float = _WorldSpaceCameraPos[1];
        float _Split_1acdb116d9fa41b8be55b3978ea78a48_B_3_Float = _WorldSpaceCameraPos[2];
        float _Split_1acdb116d9fa41b8be55b3978ea78a48_A_4_Float = 0;
        float _Property_e7bcdd9fa3e94e5d9b702f6e76c0d010_Out_0_Float = Vector1_8d1279a725f04004abeec8eb29628190;
        float _OneMinus_1a8c58b3e02947d5ad383443d81f8958_Out_1_Float;
        Unity_OneMinus_float(_Property_e7bcdd9fa3e94e5d9b702f6e76c0d010_Out_0_Float, _OneMinus_1a8c58b3e02947d5ad383443d81f8958_Out_1_Float);
        float _Multiply_b97098fcc3b7445aad787123ce7661eb_Out_2_Float;
        Unity_Multiply_float_float(_Split_1acdb116d9fa41b8be55b3978ea78a48_G_2_Float, _OneMinus_1a8c58b3e02947d5ad383443d81f8958_Out_1_Float, _Multiply_b97098fcc3b7445aad787123ce7661eb_Out_2_Float);
        float _Subtract_3b4ccdfd32ea4cb3ac423610dc19ec70_Out_2_Float;
        Unity_Subtract_float(_Split_447a21f9b4ed4de289a42265b8854724_G_2_Float, _Multiply_b97098fcc3b7445aad787123ce7661eb_Out_2_Float, _Subtract_3b4ccdfd32ea4cb3ac423610dc19ec70_Out_2_Float);
        float _Property_88a60358336e43d1b71d0e155f5a2c3f_Out_0_Float = Vector1_548c23b8a28a4acba12d5e42e8de4dd5;
        float _Property_da0c12e8a0534ab4a6afee1d15bb433e_Out_0_Float = Vector1_8d1279a725f04004abeec8eb29628190;
        float _Multiply_b91da133531c42a9b19158928c5930f1_Out_2_Float;
        Unity_Multiply_float_float(_Property_88a60358336e43d1b71d0e155f5a2c3f_Out_0_Float, _Property_da0c12e8a0534ab4a6afee1d15bb433e_Out_0_Float, _Multiply_b91da133531c42a9b19158928c5930f1_Out_2_Float);
        float _Subtract_eb72265b4d4a464b8a144339c6d13e5a_Out_2_Float;
        Unity_Subtract_float(_Subtract_3b4ccdfd32ea4cb3ac423610dc19ec70_Out_2_Float, _Multiply_b91da133531c42a9b19158928c5930f1_Out_2_Float, _Subtract_eb72265b4d4a464b8a144339c6d13e5a_Out_2_Float);
        float _Property_c7a5428b3f884bb798b63d2303a1a207_Out_0_Float = Vector1_8a01e7e97fac46d3a084c24499c15b97;
        float _Property_328059683b4144a399b419257f7df7e6_Out_0_Float = Vector1_8d1279a725f04004abeec8eb29628190;
        float _Multiply_2dfb6cd355d74d25adaa81134b3f0d46_Out_2_Float;
        Unity_Multiply_float_float(_Property_c7a5428b3f884bb798b63d2303a1a207_Out_0_Float, _Property_328059683b4144a399b419257f7df7e6_Out_0_Float, _Multiply_2dfb6cd355d74d25adaa81134b3f0d46_Out_2_Float);
        float _Maximum_7b5e96f356504e53985bfda714bf891f_Out_2_Float;
        Unity_Maximum_float(_Multiply_2dfb6cd355d74d25adaa81134b3f0d46_Out_2_Float, 0, _Maximum_7b5e96f356504e53985bfda714bf891f_Out_2_Float);
        float _Divide_c590caeb182647889ab25c0491be6524_Out_2_Float;
        Unity_Divide_float(1, _Maximum_7b5e96f356504e53985bfda714bf891f_Out_2_Float, _Divide_c590caeb182647889ab25c0491be6524_Out_2_Float);
        float _Multiply_c1516d20c0914e0e8ca560f3f5b4b066_Out_2_Float;
        Unity_Multiply_float_float(_Subtract_eb72265b4d4a464b8a144339c6d13e5a_Out_2_Float, _Divide_c590caeb182647889ab25c0491be6524_Out_2_Float, _Multiply_c1516d20c0914e0e8ca560f3f5b4b066_Out_2_Float);
        float _Saturate_be305bdfd34e40f28be591169dad704c_Out_1_Float;
        Unity_Saturate_float(_Multiply_c1516d20c0914e0e8ca560f3f5b4b066_Out_2_Float, _Saturate_be305bdfd34e40f28be591169dad704c_Out_1_Float);
        float _Blend_1d78d20d334a4cb1bef83f720c3abc74_Out_2_Float;
        Unity_Blend_Screen_float(_Exponential_a3837813a78044a18b0bd627883f4d75_Out_1_Float, _Saturate_be305bdfd34e40f28be591169dad704c_Out_1_Float, _Blend_1d78d20d334a4cb1bef83f720c3abc74_Out_2_Float, 1);
        Color_2 = (_Fog_86d7de5382804677a9f8a6a7344cd56d_Color_0_Vector4.xyz);
        Density_1 = _Blend_1d78d20d334a4cb1bef83f720c3abc74_Out_2_Float;
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        struct Bindings_MixFog_5251c54db4a6a478695a0390a4619eb9_float
        {
        float3 ObjectSpacePosition;
        float3 WorldSpacePosition;
        };
        
        void SG_MixFog_5251c54db4a6a478695a0390a4619eb9_float(float3 Vector3_f69a590ed6c84f36b0fee2268ca8fc03, float Vector1_dcd94c39cc78471aa6a3a1b27833bba1, float Vector1_65f4d0dc61ee403db0f9bfa041bed070, float4x4 Matrix4_5f3038a237414f29a1174d7598403333, float Vector1_eebb9e601111455e9605b88f3456f319, Bindings_MixFog_5251c54db4a6a478695a0390a4619eb9_float IN, out float3 Output_1)
        {
        float _Property_ee6090b3fec14429b97d420e65b6fa37_Out_0_Float = Vector1_65f4d0dc61ee403db0f9bfa041bed070;
        float _Property_bf717ffc5d574360858594b6ebce261d_Out_0_Float = Vector1_dcd94c39cc78471aa6a3a1b27833bba1;
        float4x4 _Property_bf1dffda22534014a73fd016c6ef049f_Out_0_Matrix4 = Matrix4_5f3038a237414f29a1174d7598403333;
        float _Property_d36d86c8e5ab4a86927c82837e601838_Out_0_Float = Vector1_eebb9e601111455e9605b88f3456f319;
        Bindings_URPExponentialFog_a30e9a21a0b9141cbaaf335f79e3f00f_float _URPExponentialFog_fc9b60a95ad444c88c6b1be317899593;
        _URPExponentialFog_fc9b60a95ad444c88c6b1be317899593.ObjectSpacePosition = IN.ObjectSpacePosition;
        _URPExponentialFog_fc9b60a95ad444c88c6b1be317899593.WorldSpacePosition = IN.WorldSpacePosition;
        float3 _URPExponentialFog_fc9b60a95ad444c88c6b1be317899593_Color_2_Vector3;
        float _URPExponentialFog_fc9b60a95ad444c88c6b1be317899593_Density_1_Float;
        SG_URPExponentialFog_a30e9a21a0b9141cbaaf335f79e3f00f_float(_Property_ee6090b3fec14429b97d420e65b6fa37_Out_0_Float, _Property_bf717ffc5d574360858594b6ebce261d_Out_0_Float, _Property_bf1dffda22534014a73fd016c6ef049f_Out_0_Matrix4, _Property_d36d86c8e5ab4a86927c82837e601838_Out_0_Float, _URPExponentialFog_fc9b60a95ad444c88c6b1be317899593, _URPExponentialFog_fc9b60a95ad444c88c6b1be317899593_Color_2_Vector3, _URPExponentialFog_fc9b60a95ad444c88c6b1be317899593_Density_1_Float);
        float3 _Property_7b5efc935cf04f3d8d6d30256e3c2385_Out_0_Vector3 = Vector3_f69a590ed6c84f36b0fee2268ca8fc03;
        float _Saturate_7aee755c2d2748979413e4d140fd121d_Out_1_Float;
        Unity_Saturate_float(_URPExponentialFog_fc9b60a95ad444c88c6b1be317899593_Density_1_Float, _Saturate_7aee755c2d2748979413e4d140fd121d_Out_1_Float);
        float3 _Lerp_bb4fea0611124207b10efb240fd9fd48_Out_3_Vector3;
        Unity_Lerp_float3(_URPExponentialFog_fc9b60a95ad444c88c6b1be317899593_Color_2_Vector3, _Property_7b5efc935cf04f3d8d6d30256e3c2385_Out_0_Vector3, (_Saturate_7aee755c2d2748979413e4d140fd121d_Out_1_Float.xxx), _Lerp_bb4fea0611124207b10efb240fd9fd48_Out_3_Vector3);
        Output_1 = _Lerp_bb4fea0611124207b10efb240fd9fd48_Out_3_Vector3;
        }
        
        void Unity_Length_float3(float3 In, out float Out)
        {
            Out = length(In);
        }
        
        void Unity_Branch_float2(float Predicate, float2 True, float2 False, out float2 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
        Out = A * B;
        }
        
        void Unity_Add_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A + B;
        }
        
        void Unity_Fraction_float2(float2 In, out float2 Out)
        {
            Out = frac(In);
        }
        
        void Unity_RandomRange_float(float2 Seed, float Min, float Max, out float Out)
        {
             float randomno =  frac(sin(dot(Seed, float2(12.9898, 78.233)))*43758.5453);
             Out = lerp(Min, Max, randomno);
        }
        
        void Unity_Floor_float2(float2 In, out float2 Out)
        {
            Out = floor(In);
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        struct Bindings_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float
        {
        float2 NDCPosition;
        float3 TimeParameters;
        };
        
        void SG_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float(float2 Vector2_caefb2173a93413babdc548e42b92647, float Boolean_d17c4aa779b140eb89358932ea6663e5, float Vector1_397d6d54d391448f841b568356acd9d5, Bindings_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float IN, out float Output_1, out float3 Normal_2)
        {
        float _Property_721476b2541f47d9a2a9269e3083e0d6_Out_0_Float = Vector1_397d6d54d391448f841b568356acd9d5;
        float _Multiply_7969691e0cc14256bab4e3e287352010_Out_2_Float;
        Unity_Multiply_float_float(_Property_721476b2541f47d9a2a9269e3083e0d6_Out_0_Float, 0.321, _Multiply_7969691e0cc14256bab4e3e287352010_Out_2_Float);
        float _Property_81032f19979848909a7ca400e4b48342_Out_0_Boolean = Boolean_d17c4aa779b140eb89358932ea6663e5;
        float2 _Vector2_fd3f4c200f834792b2a20662ee9e881a_Out_0_Vector2 = float2(IN.TimeParameters.x, IN.TimeParameters.x);
        float2 _Branch_0fc6b47a1c3b42de9b4e91943c99b893_Out_3_Vector2;
        Unity_Branch_float2(_Property_81032f19979848909a7ca400e4b48342_Out_0_Boolean, _Vector2_fd3f4c200f834792b2a20662ee9e881a_Out_0_Vector2, float2(0, 0), _Branch_0fc6b47a1c3b42de9b4e91943c99b893_Out_3_Vector2);
        float2 _Multiply_2a72b24c4beb4af4911a02dc52727555_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Branch_0fc6b47a1c3b42de9b4e91943c99b893_Out_3_Vector2, float2(0.018, 0.32), _Multiply_2a72b24c4beb4af4911a02dc52727555_Out_2_Vector2);
        float2 _Add_66ffbf97cca14199972fa0e0db881b16_Out_2_Vector2;
        Unity_Add_float2((_Multiply_7969691e0cc14256bab4e3e287352010_Out_2_Float.xx), _Multiply_2a72b24c4beb4af4911a02dc52727555_Out_2_Vector2, _Add_66ffbf97cca14199972fa0e0db881b16_Out_2_Vector2);
        float2 _Fraction_396a3fa0ff464affbc41bb38a673dcc2_Out_1_Vector2;
        Unity_Fraction_float2(_Add_66ffbf97cca14199972fa0e0db881b16_Out_2_Vector2, _Fraction_396a3fa0ff464affbc41bb38a673dcc2_Out_1_Vector2);
        float _Split_63ceb6dee6e54027b8f1467e4376cee2_R_1_Float = _Fraction_396a3fa0ff464affbc41bb38a673dcc2_Out_1_Vector2[0];
        float _Split_63ceb6dee6e54027b8f1467e4376cee2_G_2_Float = _Fraction_396a3fa0ff464affbc41bb38a673dcc2_Out_1_Vector2[1];
        float _Split_63ceb6dee6e54027b8f1467e4376cee2_B_3_Float = 0;
        float _Split_63ceb6dee6e54027b8f1467e4376cee2_A_4_Float = 0;
        float _RandomRange_484c84f4919c4cd8a517b36c15a038ea_Out_3_Float;
        Unity_RandomRange_float((_Split_63ceb6dee6e54027b8f1467e4376cee2_R_1_Float.xx), 0, _ScreenParams.x, _RandomRange_484c84f4919c4cd8a517b36c15a038ea_Out_3_Float);
        float _RandomRange_b63dcf8e65f740fab3cc51e40e1d5685_Out_3_Float;
        Unity_RandomRange_float((_Split_63ceb6dee6e54027b8f1467e4376cee2_G_2_Float.xx), 0, _ScreenParams.y, _RandomRange_b63dcf8e65f740fab3cc51e40e1d5685_Out_3_Float);
        float2 _Vector2_04d66487473642d79da224f4089554dd_Out_0_Vector2 = float2(_RandomRange_484c84f4919c4cd8a517b36c15a038ea_Out_3_Float, _RandomRange_b63dcf8e65f740fab3cc51e40e1d5685_Out_3_Float);
        float4 _ScreenPosition_a0254f37c23d45ffaf168c1d3439736b_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
        float4 _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RGBA_4_Vector4;
        float3 _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RGB_5_Vector3;
        float2 _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RG_6_Vector2;
        Unity_Combine_float(_ScreenParams.x, _ScreenParams.y, 0, 0, _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RGBA_4_Vector4, _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RGB_5_Vector3, _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RG_6_Vector2);
        float2 _Multiply_3e3b6c94f64b4ade9247e71c6d2f98d6_Out_2_Vector2;
        Unity_Multiply_float2_float2((_ScreenPosition_a0254f37c23d45ffaf168c1d3439736b_Out_0_Vector4.xy), _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RG_6_Vector2, _Multiply_3e3b6c94f64b4ade9247e71c6d2f98d6_Out_2_Vector2);
        float2 _Floor_4ba12f9eb3474734b0137ba643429b23_Out_1_Vector2;
        Unity_Floor_float2(_Multiply_3e3b6c94f64b4ade9247e71c6d2f98d6_Out_2_Vector2, _Floor_4ba12f9eb3474734b0137ba643429b23_Out_1_Vector2);
        float2 _Add_278d532c14154882bf1d47dde4d55ad3_Out_2_Vector2;
        Unity_Add_float2(_Vector2_04d66487473642d79da224f4089554dd_Out_0_Vector2, _Floor_4ba12f9eb3474734b0137ba643429b23_Out_1_Vector2, _Add_278d532c14154882bf1d47dde4d55ad3_Out_2_Vector2);
        float2 _Property_663e26b847554df8a237e6b5af6c8681_Out_0_Vector2 = Vector2_caefb2173a93413babdc548e42b92647;
        float2 _Divide_4576c5b640be4a1cba36f7b52c41b042_Out_2_Vector2;
        Unity_Divide_float2(_Add_278d532c14154882bf1d47dde4d55ad3_Out_2_Vector2, _Property_663e26b847554df8a237e6b5af6c8681_Out_0_Vector2, _Divide_4576c5b640be4a1cba36f7b52c41b042_Out_2_Vector2);
        float2 _Fraction_4942b1a15b0a4a019e485aa4d5bcb166_Out_1_Vector2;
        Unity_Fraction_float2(_Divide_4576c5b640be4a1cba36f7b52c41b042_Out_2_Vector2, _Fraction_4942b1a15b0a4a019e485aa4d5bcb166_Out_1_Vector2);
        float4 _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D).GetTransformedUV(_Fraction_4942b1a15b0a4a019e485aa4d5bcb166_Out_1_Vector2) );
        float _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_R_4_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4.r;
        float _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_G_5_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4.g;
        float _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_B_6_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4.b;
        float _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_A_7_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4.a;
        float _Split_95c0c3c67c5148a4945a0e0a8192275f_R_1_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4[0];
        float _Split_95c0c3c67c5148a4945a0e0a8192275f_G_2_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4[1];
        float _Split_95c0c3c67c5148a4945a0e0a8192275f_B_3_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4[2];
        float _Split_95c0c3c67c5148a4945a0e0a8192275f_A_4_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4[3];
        float3 _Vector3_54f2a95424284f65912dd46705fb9b33_Out_0_Vector3 = float3(_Split_95c0c3c67c5148a4945a0e0a8192275f_R_1_Float, _Split_95c0c3c67c5148a4945a0e0a8192275f_G_2_Float, _Split_95c0c3c67c5148a4945a0e0a8192275f_B_3_Float);
        float3 _Normalize_56d642ffdf80459aa5137d04c28ad137_Out_1_Vector3;
        Unity_Normalize_float3(_Vector3_54f2a95424284f65912dd46705fb9b33_Out_0_Vector3, _Normalize_56d642ffdf80459aa5137d04c28ad137_Out_1_Vector3);
        Output_1 = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_G_5_Float;
        Normal_2 = _Normalize_56d642ffdf80459aa5137d04c28ad137_Out_1_Vector3;
        }
        
        void Unity_Preview_float(float In, out float Out)
        {
            Out = In;
        }
        
        void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
        {
            Out = smoothstep(Edge1, Edge2, In);
        }
        
        void Unity_Minimum_float(float A, float B, out float Out)
        {
            Out = min(A, B);
        };
        
        void Unity_SceneDepth_Linear01_float(float4 UV, out float Out)
        {
            Out = Linear01Depth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
        }
        
        struct Bindings_SoftParticles_18333bb3c326140abbd08c558de3bf83_float
        {
        float4 ScreenPosition;
        float2 NDCPosition;
        };
        
        void SG_SoftParticles_18333bb3c326140abbd08c558de3bf83_float(float Vector1_b12c77296e1146fa9f4d315fc85d04ab, float Vector1_0414f85da44b41c9983c5044458d5297, float Vector1_0695185d2cd141a68dcd8c1a2daa3c40, Bindings_SoftParticles_18333bb3c326140abbd08c558de3bf83_float IN, out float OutVector1_1)
        {
        float _SceneDepth_276a1c9e05034cdfa6529a8931fb79f7_Out_1_Float;
        Unity_SceneDepth_Linear01_float(float4(IN.NDCPosition.xy, 0, 0), _SceneDepth_276a1c9e05034cdfa6529a8931fb79f7_Out_1_Float);
        float _Multiply_6ce0bce77db34631b737de6ad4fa0e04_Out_2_Float;
        Unity_Multiply_float_float(_SceneDepth_276a1c9e05034cdfa6529a8931fb79f7_Out_1_Float, _ProjectionParams.z, _Multiply_6ce0bce77db34631b737de6ad4fa0e04_Out_2_Float);
        float _Property_6a749dd773734fab83f490ee0fb3c7f0_Out_0_Float = Vector1_0695185d2cd141a68dcd8c1a2daa3c40;
        float4 _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4 = IN.ScreenPosition;
        float _Split_f05c829a2fa34687a4c46a8484cc4eab_R_1_Float = _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4[0];
        float _Split_f05c829a2fa34687a4c46a8484cc4eab_G_2_Float = _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4[1];
        float _Split_f05c829a2fa34687a4c46a8484cc4eab_B_3_Float = _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4[2];
        float _Split_f05c829a2fa34687a4c46a8484cc4eab_A_4_Float = _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4[3];
        float _Add_39d32d1e2d4e46efa943593378a7b5d4_Out_2_Float;
        Unity_Add_float(_Property_6a749dd773734fab83f490ee0fb3c7f0_Out_0_Float, _Split_f05c829a2fa34687a4c46a8484cc4eab_A_4_Float, _Add_39d32d1e2d4e46efa943593378a7b5d4_Out_2_Float);
        float _Subtract_0db08fa14d48402dbe0edd1a2df1d281_Out_2_Float;
        Unity_Subtract_float(_Multiply_6ce0bce77db34631b737de6ad4fa0e04_Out_2_Float, _Add_39d32d1e2d4e46efa943593378a7b5d4_Out_2_Float, _Subtract_0db08fa14d48402dbe0edd1a2df1d281_Out_2_Float);
        float _Property_a1e60e28f8174d88ba77f34de3664a02_Out_0_Float = Vector1_b12c77296e1146fa9f4d315fc85d04ab;
        float _Property_72d03f4fff3e4c5f8607d2320b27c4d0_Out_0_Float = Vector1_0414f85da44b41c9983c5044458d5297;
        float2 _Vector2_d5e84c16e02d464b97da10162141cf76_Out_0_Vector2 = float2(_Property_a1e60e28f8174d88ba77f34de3664a02_Out_0_Float, _Property_72d03f4fff3e4c5f8607d2320b27c4d0_Out_0_Float);
        float _Remap_41d3d1abc133496d946826964dcf67e5_Out_3_Float;
        Unity_Remap_float(_Subtract_0db08fa14d48402dbe0edd1a2df1d281_Out_2_Float, _Vector2_d5e84c16e02d464b97da10162141cf76_Out_0_Vector2, float2 (0, 1), _Remap_41d3d1abc133496d946826964dcf67e5_Out_3_Float);
        float _Saturate_6dc9e8b8bd6a4471aba1733b5fa31a0d_Out_1_Float;
        Unity_Saturate_float(_Remap_41d3d1abc133496d946826964dcf67e5_Out_3_Float, _Saturate_6dc9e8b8bd6a4471aba1733b5fa31a0d_Out_1_Float);
        OutVector1_1 = _Saturate_6dc9e8b8bd6a4471aba1733b5fa31a0d_Out_1_Float;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float Alpha;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _Property_c2828c2b3b9343e9b9f6966966dcdbe0_Out_0_Vector4 = Color_7eff2e7fc27440719d0a871a39dabdd3;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            half3 _MainLightCustomFunction_8870fa9bc91542d0a2b3b835afc4d160_Direction_1_Vector3;
            half3 _MainLightCustomFunction_8870fa9bc91542d0a2b3b835afc4d160_Color_2_Vector3;
            half _MainLightCustomFunction_8870fa9bc91542d0a2b3b835afc4d160_DistanceAtten_3_Float;
            half _MainLightCustomFunction_8870fa9bc91542d0a2b3b835afc4d160_ShadowAtten_4_Float;
            MainLight_half(_MainLightCustomFunction_8870fa9bc91542d0a2b3b835afc4d160_Direction_1_Vector3, _MainLightCustomFunction_8870fa9bc91542d0a2b3b835afc4d160_Color_2_Vector3, _MainLightCustomFunction_8870fa9bc91542d0a2b3b835afc4d160_DistanceAtten_3_Float, _MainLightCustomFunction_8870fa9bc91542d0a2b3b835afc4d160_ShadowAtten_4_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float3 _Normalize_6b38c5a3cffb4dca97a262f7e17e7a9a_Out_1_Vector3;
            Unity_Normalize_float3(IN.WorldSpaceViewDirection, _Normalize_6b38c5a3cffb4dca97a262f7e17e7a9a_Out_1_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            half3 _Negate_1b6b777fc47d4e3999403cd69f9a1edd_Out_1_Vector3;
            Unity_Negate_half3(_MainLightCustomFunction_8870fa9bc91542d0a2b3b835afc4d160_Direction_1_Vector3, _Negate_1b6b777fc47d4e3999403cd69f9a1edd_Out_1_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _DotProduct_68ed5c9b09234ffdb3de22d96beb0782_Out_2_Float;
            Unity_DotProduct_float3(_Normalize_6b38c5a3cffb4dca97a262f7e17e7a9a_Out_1_Vector3, _Negate_1b6b777fc47d4e3999403cd69f9a1edd_Out_1_Vector3, _DotProduct_68ed5c9b09234ffdb3de22d96beb0782_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Saturate_c06a279c60524824ae0e4c72d7397e9a_Out_1_Float;
            Unity_Saturate_float(_DotProduct_68ed5c9b09234ffdb3de22d96beb0782_Out_2_Float, _Saturate_c06a279c60524824ae0e4c72d7397e9a_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Power_6aa1633458314359af0d2028500ae133_Out_2_Float;
            Unity_Power_float(_Saturate_c06a279c60524824ae0e4c72d7397e9a_Out_1_Float, 150, _Power_6aa1633458314359af0d2028500ae133_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Remap_d2403b2388124cad8739c0807bf74a2d_Out_3_Float;
            Unity_Remap_float(_Power_6aa1633458314359af0d2028500ae133_Out_2_Float, float2 (0, 1), float2 (1, 2), _Remap_d2403b2388124cad8739c0807bf74a2d_Out_3_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4x4 _MatrixConstruction_d937186230a944779e67bd584476956e_var4x4_4_Matrix4;
            float3x3 _MatrixConstruction_d937186230a944779e67bd584476956e_var3x3_5_Matrix3;
            float2x2 _MatrixConstruction_d937186230a944779e67bd584476956e_var2x2_6_Matrix2;
            Unity_MatrixConstruction_Row_float((float4(IN.WorldSpaceTangent, 1.0)), (float4(IN.WorldSpaceBiTangent, 1.0)), (float4(IN.WorldSpaceNormal, 1.0)), float4 (0, 0, 0, 0), _MatrixConstruction_d937186230a944779e67bd584476956e_var4x4_4_Matrix4, _MatrixConstruction_d937186230a944779e67bd584476956e_var3x3_5_Matrix3, _MatrixConstruction_d937186230a944779e67bd584476956e_var2x2_6_Matrix2);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            Bindings_GetMainLight_2ff6b2f9d6ff940dfa76d3c0d3f9c2ae_half _GetMainLight_1ece9407584e4056b9346816ccf0ea19;
            half3 _GetMainLight_1ece9407584e4056b9346816ccf0ea19_Direction_1_Vector3;
            half3 _GetMainLight_1ece9407584e4056b9346816ccf0ea19_Color_2_Vector3;
            half _GetMainLight_1ece9407584e4056b9346816ccf0ea19_DistanceAttenuation_3_Float;
            half _GetMainLight_1ece9407584e4056b9346816ccf0ea19_ShadowAttenuation_4_Float;
            SG_GetMainLight_2ff6b2f9d6ff940dfa76d3c0d3f9c2ae_half(float3 (0, 0, 0), 0, _GetMainLight_1ece9407584e4056b9346816ccf0ea19, _GetMainLight_1ece9407584e4056b9346816ccf0ea19_Direction_1_Vector3, _GetMainLight_1ece9407584e4056b9346816ccf0ea19_Color_2_Vector3, _GetMainLight_1ece9407584e4056b9346816ccf0ea19_DistanceAttenuation_3_Float, _GetMainLight_1ece9407584e4056b9346816ccf0ea19_ShadowAttenuation_4_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float3 _Multiply_4ecad25e3a0346509b51bd1e9195f870_Out_2_Vector3;
            Unity_Multiply_float3x3_float3(_MatrixConstruction_d937186230a944779e67bd584476956e_var3x3_5_Matrix3, _GetMainLight_1ece9407584e4056b9346816ccf0ea19_Direction_1_Vector3, _Multiply_4ecad25e3a0346509b51bd1e9195f870_Out_2_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float3 _Normalize_8684a681837240098a2a06a04687e43c_Out_1_Vector3;
            Unity_Normalize_float3(_Multiply_4ecad25e3a0346509b51bd1e9195f870_Out_2_Vector3, _Normalize_8684a681837240098a2a06a04687e43c_Out_1_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            UnityTexture2D _Property_a94933ad99104a7e829e26333c3da8ab_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(Texture2D_4B382D9C);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_a94933ad99104a7e829e26333c3da8ab_Out_0_Texture2D.tex, _Property_a94933ad99104a7e829e26333c3da8ab_Out_0_Texture2D.samplerstate, _Property_a94933ad99104a7e829e26333c3da8ab_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_R_4_Float = _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_RGBA_0_Vector4.r;
            float _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_G_5_Float = _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_RGBA_0_Vector4.g;
            float _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_B_6_Float = _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_RGBA_0_Vector4.b;
            float _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_A_7_Float = _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_RGBA_0_Vector4.a;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            UnityTexture2D _Property_06ae7b2647934302a2bc77f5a16cfac2_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(Texture2D_3DC46A50);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_06ae7b2647934302a2bc77f5a16cfac2_Out_0_Texture2D.tex, _Property_06ae7b2647934302a2bc77f5a16cfac2_Out_0_Texture2D.samplerstate, _Property_06ae7b2647934302a2bc77f5a16cfac2_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_R_4_Float = _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4.r;
            float _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_G_5_Float = _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4.g;
            float _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_B_6_Float = _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4.b;
            float _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_A_7_Float = _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4.a;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            Bindings_ComputeLightmap_e43f2878967dc15488276fdf96a2a634_float _ComputeLightmap_089b9b63089148c684b0d015a451e22e;
            float _ComputeLightmap_089b9b63089148c684b0d015a451e22e_Lightmap_0_Float;
            SG_ComputeLightmap_e43f2878967dc15488276fdf96a2a634_float(_Normalize_8684a681837240098a2a06a04687e43c_Out_1_Vector3, _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_R_4_Float, _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_B_6_Float, _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_G_5_Float, _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_A_7_Float, _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_R_4_Float, _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_G_5_Float, _ComputeLightmap_089b9b63089148c684b0d015a451e22e, _ComputeLightmap_089b9b63089148c684b0d015a451e22e_Lightmap_0_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_71376bfef41e4c60b2854418f222f581_Out_0_Float = Vector1_2a375228e8d3437382814520b0cd8644;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float Slider_0778109a2833445aa2b5c95029b54a3a = 0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Remap_2877d604dc3f4d819395fecb16454d7e_Out_3_Float;
            Unity_Remap_float(Slider_0778109a2833445aa2b5c95029b54a3a, float2 (0, 1), float2 (0.5, 1), _Remap_2877d604dc3f4d819395fecb16454d7e_Out_3_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Multiply_ce1a0a31791c4653a930ba16a5fa049a_Out_2_Float;
            Unity_Multiply_float_float(_Property_71376bfef41e4c60b2854418f222f581_Out_0_Float, _Remap_2877d604dc3f4d819395fecb16454d7e_Out_3_Float, _Multiply_ce1a0a31791c4653a930ba16a5fa049a_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Maximum_fa17ffed7e7645d5813075804f1deda3_Out_2_Float;
            Unity_Maximum_float(_Multiply_ce1a0a31791c4653a930ba16a5fa049a_Out_2_Float, 0.001, _Maximum_fa17ffed7e7645d5813075804f1deda3_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Power_385054020c344800ba53e4a12bd4d51d_Out_2_Float;
            Unity_Power_float(_ComputeLightmap_089b9b63089148c684b0d015a451e22e_Lightmap_0_Float, _Maximum_fa17ffed7e7645d5813075804f1deda3_Out_2_Float, _Power_385054020c344800ba53e4a12bd4d51d_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Multiply_7c8fc9c61f1c472ab4e2e46f549f9456_Out_2_Float;
            Unity_Multiply_float_float(_Remap_d2403b2388124cad8739c0807bf74a2d_Out_3_Float, _Power_385054020c344800ba53e4a12bd4d51d_Out_2_Float, _Multiply_7c8fc9c61f1c472ab4e2e46f549f9456_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float3 _Multiply_8ba7011ffa514036a1cc3f66c3c861ff_Out_2_Vector3;
            Unity_Multiply_float3_float3(_MainLightCustomFunction_8870fa9bc91542d0a2b3b835afc4d160_Color_2_Vector3, (_Multiply_7c8fc9c61f1c472ab4e2e46f549f9456_Out_2_Float.xxx), _Multiply_8ba7011ffa514036a1cc3f66c3c861ff_Out_2_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float3 _Transform_7b3f1c3a74c4464b8bbfbe86674e2d74_Out_1_Vector3;
            {
                float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
                _Transform_7b3f1c3a74c4464b8bbfbe86674e2d74_Out_1_Vector3 = TransformWorldToTangent(float3 (0, 1, 0).xyz, tangentTransform, true);
            }
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            Bindings_ComputeLightmap_e43f2878967dc15488276fdf96a2a634_float _ComputeLightmap_153ec09869964725a3e39aa055c13ab4;
            float _ComputeLightmap_153ec09869964725a3e39aa055c13ab4_Lightmap_0_Float;
            SG_ComputeLightmap_e43f2878967dc15488276fdf96a2a634_float(_Transform_7b3f1c3a74c4464b8bbfbe86674e2d74_Out_1_Vector3, _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_R_4_Float, _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_B_6_Float, _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_G_5_Float, _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_A_7_Float, _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_R_4_Float, _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_G_5_Float, _ComputeLightmap_153ec09869964725a3e39aa055c13ab4, _ComputeLightmap_153ec09869964725a3e39aa055c13ab4_Lightmap_0_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float3 _Vector3_0634952406fb4234b0c76357309bdb8d_Out_0_Vector3 = float3(1, _ComputeLightmap_153ec09869964725a3e39aa055c13ab4_Lightmap_0_Float, 1);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float3 _Normalize_b5cfd2e7a337486f80594836f10e5b5a_Out_1_Vector3;
            Unity_Normalize_float3(_Vector3_0634952406fb4234b0c76357309bdb8d_Out_0_Vector3, _Normalize_b5cfd2e7a337486f80594836f10e5b5a_Out_1_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float3 _SampleSHCustomFunction_0f11693f40e84149be7b8b8b11e19aad_Ambient_1_Vector3;
            SampleSH_float(_Normalize_b5cfd2e7a337486f80594836f10e5b5a_Out_1_Vector3, _SampleSHCustomFunction_0f11693f40e84149be7b8b8b11e19aad_Ambient_1_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_2e8862f5767141c387da6757663a9bf8_Out_0_Float = Vector1_5f4b185241524942830132fc71554a33;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float3 _Multiply_9e12d4562d2f483b89d99177abfa48b3_Out_2_Vector3;
            Unity_Multiply_float3_float3(_SampleSHCustomFunction_0f11693f40e84149be7b8b8b11e19aad_Ambient_1_Vector3, (_Property_2e8862f5767141c387da6757663a9bf8_Out_0_Float.xxx), _Multiply_9e12d4562d2f483b89d99177abfa48b3_Out_2_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float3 _Add_d027fefb26c6403ab640ba13310ba3b0_Out_2_Vector3;
            Unity_Add_float3(_Multiply_8ba7011ffa514036a1cc3f66c3c861ff_Out_2_Vector3, _Multiply_9e12d4562d2f483b89d99177abfa48b3_Out_2_Vector3, _Add_d027fefb26c6403ab640ba13310ba3b0_Out_2_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float3 _Multiply_d0c8a59906c34fa993df3216c4ed5b37_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_c2828c2b3b9343e9b9f6966966dcdbe0_Out_0_Vector4.xyz), _Add_d027fefb26c6403ab640ba13310ba3b0_Out_2_Vector3, _Multiply_d0c8a59906c34fa993df3216c4ed5b37_Out_2_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_40abfff51702420992215a6b0160134d_Out_0_Float = Vector1_33982692;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_f8c400911f384e54a29a04532285877f_Out_0_Float = Vector1_10DA9AB3;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4x4 _Property_40a3954879c843e0a13d646287556ad7_Out_0_Matrix4 = _BA_SkyboxMatrix;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_583b965013e541d9a6b3923989e8fcc5_Out_0_Float = _BA_SkyboxRatio;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            Bindings_MixFog_5251c54db4a6a478695a0390a4619eb9_float _MixFog_3c1fce250a8a4fb2be1e03a174a7c9c4;
            _MixFog_3c1fce250a8a4fb2be1e03a174a7c9c4.ObjectSpacePosition = IN.ObjectSpacePosition;
            _MixFog_3c1fce250a8a4fb2be1e03a174a7c9c4.WorldSpacePosition = IN.WorldSpacePosition;
            float3 _MixFog_3c1fce250a8a4fb2be1e03a174a7c9c4_Output_1_Vector3;
            SG_MixFog_5251c54db4a6a478695a0390a4619eb9_float(_Multiply_d0c8a59906c34fa993df3216c4ed5b37_Out_2_Vector3, _Property_40abfff51702420992215a6b0160134d_Out_0_Float, _Property_f8c400911f384e54a29a04532285877f_Out_0_Float, _Property_40a3954879c843e0a13d646287556ad7_Out_0_Matrix4, _Property_583b965013e541d9a6b3923989e8fcc5_Out_0_Float, _MixFog_3c1fce250a8a4fb2be1e03a174a7c9c4, _MixFog_3c1fce250a8a4fb2be1e03a174a7c9c4_Output_1_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _OneMinus_a90ee5ec5cfd410780133317beeee956_Out_1_Float;
            Unity_OneMinus_float(_SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_A_7_Float, _OneMinus_a90ee5ec5cfd410780133317beeee956_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_fbf5240d6a974443afb86e71299332dc_Out_0_Float = Vector1_2a375228e8d3437382814520b0cd8644;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Power_4d5b0fe5fe894f37bf2bf51adfe6403f_Out_2_Float;
            Unity_Power_float(_OneMinus_a90ee5ec5cfd410780133317beeee956_Out_1_Float, _Property_fbf5240d6a974443afb86e71299332dc_Out_0_Float, _Power_4d5b0fe5fe894f37bf2bf51adfe6403f_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _OneMinus_12776925fcba4b8f8c431735c34d4ef0_Out_1_Float;
            Unity_OneMinus_float(_Power_4d5b0fe5fe894f37bf2bf51adfe6403f_Out_2_Float, _OneMinus_12776925fcba4b8f8c431735c34d4ef0_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Length_e04efe434e9a4ecc998e1dd3c3d5e90e_Out_1_Float;
            Unity_Length_float3(SHADERGRAPH_OBJECT_POSITION, _Length_e04efe434e9a4ecc998e1dd3c3d5e90e_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            Bindings_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679;
            _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679.NDCPosition = IN.NDCPosition;
            _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679.TimeParameters = IN.TimeParameters;
            float _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Output_1_Float;
            float3 _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Normal_2_Vector3;
            SG_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float(float2 (256, 256), 1, _Length_e04efe434e9a4ecc998e1dd3c3d5e90e_Out_1_Float, _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679, _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Output_1_Float, _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Normal_2_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Remap_bbf4dcb1a53440238a83cbaea629f9a2_Out_3_Float;
            Unity_Remap_float(_SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Output_1_Float, float2 (0, 1), float2 (0.8, 1.1), _Remap_bbf4dcb1a53440238a83cbaea629f9a2_Out_3_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4 = _BA_CloudData;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Split_3bcb0174a9954d879a71e473885b9e1b_R_1_Float = _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4[0];
            float _Split_3bcb0174a9954d879a71e473885b9e1b_G_2_Float = _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4[1];
            float _Split_3bcb0174a9954d879a71e473885b9e1b_B_3_Float = _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4[2];
            float _Split_3bcb0174a9954d879a71e473885b9e1b_A_4_Float = _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4[3];
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Multiply_211f37f5b555434d956f42443155bc4a_Out_2_Float;
            Unity_Multiply_float_float(_Remap_bbf4dcb1a53440238a83cbaea629f9a2_Out_3_Float, _Split_3bcb0174a9954d879a71e473885b9e1b_A_4_Float, _Multiply_211f37f5b555434d956f42443155bc4a_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4 = _BA_CloudData;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_R_1_Float = _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4[0];
            float _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_G_2_Float = _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4[1];
            float _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_B_3_Float = _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4[2];
            float _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_A_4_Float = _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4[3];
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            #if defined(BOOLEAN_9A9F210EF4D44680B8F0CB7ACD99DFBB_ON)
            float _BlueNoise_b07e75159dec4ad49e7f81e3592b900e_Out_0_Float = _Multiply_211f37f5b555434d956f42443155bc4a_Out_2_Float;
            #else
            float _BlueNoise_b07e75159dec4ad49e7f81e3592b900e_Out_0_Float = _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_A_4_Float;
            #endif
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _OneMinus_21be261ae35940709d0db1914ee9855c_Out_1_Float;
            Unity_OneMinus_float(_BlueNoise_b07e75159dec4ad49e7f81e3592b900e_Out_0_Float, _OneMinus_21be261ae35940709d0db1914ee9855c_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Add_2f041a64479546ff91a26ed1dbaffeee_Out_2_Float;
            Unity_Add_float(_OneMinus_21be261ae35940709d0db1914ee9855c_Out_1_Float, 0.1, _Add_2f041a64479546ff91a26ed1dbaffeee_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Preview_d4d26446fc904b76afe70165bc2d684f_Out_1_Float;
            Unity_Preview_float(_SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_B_6_Float, _Preview_d4d26446fc904b76afe70165bc2d684f_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Smoothstep_3d813f1fadfa4b9b8587408892645438_Out_3_Float;
            Unity_Smoothstep_float(_OneMinus_21be261ae35940709d0db1914ee9855c_Out_1_Float, _Add_2f041a64479546ff91a26ed1dbaffeee_Out_2_Float, _Preview_d4d26446fc904b76afe70165bc2d684f_Out_1_Float, _Smoothstep_3d813f1fadfa4b9b8587408892645438_Out_3_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Minimum_9603d95647104c398dcf64d0bf690e71_Out_2_Float;
            Unity_Minimum_float(_OneMinus_12776925fcba4b8f8c431735c34d4ef0_Out_1_Float, _Smoothstep_3d813f1fadfa4b9b8587408892645438_Out_3_Float, _Minimum_9603d95647104c398dcf64d0bf690e71_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float2 _Property_4d3f52434f0e4f098cc877f3f4dadbf9_Out_0_Vector2 = Vector2_65050940ec244ae3bf42db206c2dccca;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Split_8604fb34fc04432c845c2474347347cc_R_1_Float = _Property_4d3f52434f0e4f098cc877f3f4dadbf9_Out_0_Vector2[0];
            float _Split_8604fb34fc04432c845c2474347347cc_G_2_Float = _Property_4d3f52434f0e4f098cc877f3f4dadbf9_Out_0_Vector2[1];
            float _Split_8604fb34fc04432c845c2474347347cc_B_3_Float = 0;
            float _Split_8604fb34fc04432c845c2474347347cc_A_4_Float = 0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _OneMinus_2253c4aebc784af4b7bfe91e392d3d1c_Out_1_Float;
            Unity_OneMinus_float(_Preview_d4d26446fc904b76afe70165bc2d684f_Out_1_Float, _OneMinus_2253c4aebc784af4b7bfe91e392d3d1c_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_a7bc7d6aca7a4351a54c042414c8dca3_Out_0_Float = Vector1_2d1f70e8efb04843b1f8184ae06925e0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Multiply_521b54ef01d8415cacbbab05d9ef91b4_Out_2_Float;
            Unity_Multiply_float_float(_OneMinus_2253c4aebc784af4b7bfe91e392d3d1c_Out_1_Float, _Property_a7bc7d6aca7a4351a54c042414c8dca3_Out_0_Float, _Multiply_521b54ef01d8415cacbbab05d9ef91b4_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            Bindings_SoftParticles_18333bb3c326140abbd08c558de3bf83_float _SoftParticles_33d22c55e97e4bbf9658984efedb651d;
            _SoftParticles_33d22c55e97e4bbf9658984efedb651d.ScreenPosition = IN.ScreenPosition;
            _SoftParticles_33d22c55e97e4bbf9658984efedb651d.NDCPosition = IN.NDCPosition;
            float _SoftParticles_33d22c55e97e4bbf9658984efedb651d_OutVector1_1_Float;
            SG_SoftParticles_18333bb3c326140abbd08c558de3bf83_float(_Split_8604fb34fc04432c845c2474347347cc_R_1_Float, _Split_8604fb34fc04432c845c2474347347cc_G_2_Float, _Multiply_521b54ef01d8415cacbbab05d9ef91b4_Out_2_Float, _SoftParticles_33d22c55e97e4bbf9658984efedb651d, _SoftParticles_33d22c55e97e4bbf9658984efedb651d_OutVector1_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Multiply_603283fc9007453b86c0e29e22dc1fdf_Out_2_Float;
            Unity_Multiply_float_float(_Minimum_9603d95647104c398dcf64d0bf690e71_Out_2_Float, _SoftParticles_33d22c55e97e4bbf9658984efedb651d_OutVector1_1_Float, _Multiply_603283fc9007453b86c0e29e22dc1fdf_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            #if defined(BOOLEAN_EAB072D9159A48C685623C0E39EA73B0_ON)
            float _SoftParticles_e87b688bcc4a4c4387199157c1c53214_Out_0_Float = _Multiply_603283fc9007453b86c0e29e22dc1fdf_Out_2_Float;
            #else
            float _SoftParticles_e87b688bcc4a4c4387199157c1c53214_Out_0_Float = _Minimum_9603d95647104c398dcf64d0bf690e71_Out_2_Float;
            #endif
            #endif
            surface.BaseColor = _MixFog_3c1fce250a8a4fb2be1e03a174a7c9c4_Output_1_Vector3;
            surface.Alpha = _SoftParticles_e87b688bcc4a4c4387199157c1c53214_Out_0_Float;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpaceNormal =                          input.normalOS;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpaceTangent =                         input.tangentOS.xyz;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpacePosition =                        input.positionOS;
        #endif
        
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        float3 unnormalizedNormalWS = input.normalWS;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        const float renormFactor = 1.0 / length(unnormalizedNormalWS);
        #endif
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        // use bitangent on the fly like in hdrp
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0)* GetOddNegativeScale();
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        float3 bitang = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);
        #endif
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
        #endif
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        // to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        // This                is explained in section 2.2 in "surface gradient based bump mapping framework"
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.WorldSpaceBiTangent = renormFactor * bitang;
        #endif
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.WorldSpacePosition = input.positionWS;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);
        #endif
        
        
            #if UNITY_UV_STARTS_AT_TOP
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
        #endif
        
            #else
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
        #endif
        
            #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.NDCPosition.y = 1.0f - output.NDCPosition.y;
        #endif
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.uv0 = input.texCoord0;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
        #endif
        
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "DepthNormalsOnly"
            Tags
            {
                "LightMode" = "DepthNormalsOnly-disable"
            }
        
        // Render State
        Cull Off
        ZTest LEqual
        ZWrite On
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
        #pragma shader_feature_local _ BOOLEAN_EAB072D9159A48C685623C0E39EA73B0_ON
        #pragma shader_feature_local _ BOOLEAN_9A9F210EF4D44680B8F0CB7ACD99DFBB_ON
        
        #if defined(BOOLEAN_EAB072D9159A48C685623C0E39EA73B0_ON) && defined(BOOLEAN_9A9F210EF4D44680B8F0CB7ACD99DFBB_ON)
            #define KEYWORD_PERMUTATION_0
        #elif defined(BOOLEAN_EAB072D9159A48C685623C0E39EA73B0_ON)
            #define KEYWORD_PERMUTATION_1
        #elif defined(BOOLEAN_9A9F210EF4D44680B8F0CB7ACD99DFBB_ON)
            #define KEYWORD_PERMUTATION_2
        #else
            #define KEYWORD_PERMUTATION_3
        #endif
        
        
        // Defines
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_NORMAL
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_TANGENT
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_TEXCOORD0
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define VARYINGS_NEED_POSITION_WS
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define VARYINGS_NEED_NORMAL_WS
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define VARYINGS_NEED_TEXCOORD0
        #endif
        
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHNORMALSONLY
        #define _SURFACE_TYPE_TRANSPARENT 1
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define REQUIRE_DEPTH_TEXTURE
        #endif
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 positionOS : POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 normalOS : NORMAL;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 tangentOS : TANGENT;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 uv0 : TEXCOORD0;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
            #endif
        };
        struct Varyings
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 positionCS : SV_POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 positionWS;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 normalWS;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 texCoord0;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
            #endif
        };
        struct SurfaceDescriptionInputs
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 WorldSpacePosition;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 ScreenPosition;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float2 NDCPosition;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float2 PixelPosition;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 uv0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 TimeParameters;
            #endif
        };
        struct VertexDescriptionInputs
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpaceNormal;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpaceTangent;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpacePosition;
            #endif
        };
        struct PackedVaryings
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 positionCS : SV_POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 texCoord0 : INTERP0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 positionWS : INTERP1;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 normalWS : INTERP2;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
            #endif
        };
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.texCoord0.xyzw = input.texCoord0;
            output.positionWS.xyz = input.positionWS;
            output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.texCoord0.xyzw;
            output.positionWS = input.positionWS.xyz;
            output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        #endif
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D_TexelSize;
        float4 Color_7eff2e7fc27440719d0a871a39dabdd3;
        float Vector1_2a375228e8d3437382814520b0cd8644;
        float Vector1_5f4b185241524942830132fc71554a33;
        float Vector1_2d1f70e8efb04843b1f8184ae06925e0;
        float Vector1_33982692;
        float Vector1_10DA9AB3;
        float2 Vector2_65050940ec244ae3bf42db206c2dccca;
        float4 Texture2D_4B382D9C_TexelSize;
        float4 Texture2D_3DC46A50_TexelSize;
        float4x4 _BA_SkyboxMatrix;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D);
        SAMPLER(sampler_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D);
        TEXTURE2D(Texture2D_4B382D9C);
        SAMPLER(samplerTexture2D_4B382D9C);
        TEXTURE2D(Texture2D_3DC46A50);
        SAMPLER(samplerTexture2D_3DC46A50);
        float _BA_SkyboxRatio;
        float4 _BA_CloudData;
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_OneMinus_float(float In, out float Out)
        {
            Out = 1 - In;
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Length_float3(float3 In, out float Out)
        {
            Out = length(In);
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
        Out = A * B;
        }
        
        void Unity_Branch_float2(float Predicate, float2 True, float2 False, out float2 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
        Out = A * B;
        }
        
        void Unity_Add_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A + B;
        }
        
        void Unity_Fraction_float2(float2 In, out float2 Out)
        {
            Out = frac(In);
        }
        
        void Unity_RandomRange_float(float2 Seed, float Min, float Max, out float Out)
        {
             float randomno =  frac(sin(dot(Seed, float2(12.9898, 78.233)))*43758.5453);
             Out = lerp(Min, Max, randomno);
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Floor_float2(float2 In, out float2 Out)
        {
            Out = floor(In);
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        void Unity_Normalize_float3(float3 In, out float3 Out)
        {
            Out = normalize(In);
        }
        
        struct Bindings_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float
        {
        float2 NDCPosition;
        float3 TimeParameters;
        };
        
        void SG_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float(float2 Vector2_caefb2173a93413babdc548e42b92647, float Boolean_d17c4aa779b140eb89358932ea6663e5, float Vector1_397d6d54d391448f841b568356acd9d5, Bindings_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float IN, out float Output_1, out float3 Normal_2)
        {
        float _Property_721476b2541f47d9a2a9269e3083e0d6_Out_0_Float = Vector1_397d6d54d391448f841b568356acd9d5;
        float _Multiply_7969691e0cc14256bab4e3e287352010_Out_2_Float;
        Unity_Multiply_float_float(_Property_721476b2541f47d9a2a9269e3083e0d6_Out_0_Float, 0.321, _Multiply_7969691e0cc14256bab4e3e287352010_Out_2_Float);
        float _Property_81032f19979848909a7ca400e4b48342_Out_0_Boolean = Boolean_d17c4aa779b140eb89358932ea6663e5;
        float2 _Vector2_fd3f4c200f834792b2a20662ee9e881a_Out_0_Vector2 = float2(IN.TimeParameters.x, IN.TimeParameters.x);
        float2 _Branch_0fc6b47a1c3b42de9b4e91943c99b893_Out_3_Vector2;
        Unity_Branch_float2(_Property_81032f19979848909a7ca400e4b48342_Out_0_Boolean, _Vector2_fd3f4c200f834792b2a20662ee9e881a_Out_0_Vector2, float2(0, 0), _Branch_0fc6b47a1c3b42de9b4e91943c99b893_Out_3_Vector2);
        float2 _Multiply_2a72b24c4beb4af4911a02dc52727555_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Branch_0fc6b47a1c3b42de9b4e91943c99b893_Out_3_Vector2, float2(0.018, 0.32), _Multiply_2a72b24c4beb4af4911a02dc52727555_Out_2_Vector2);
        float2 _Add_66ffbf97cca14199972fa0e0db881b16_Out_2_Vector2;
        Unity_Add_float2((_Multiply_7969691e0cc14256bab4e3e287352010_Out_2_Float.xx), _Multiply_2a72b24c4beb4af4911a02dc52727555_Out_2_Vector2, _Add_66ffbf97cca14199972fa0e0db881b16_Out_2_Vector2);
        float2 _Fraction_396a3fa0ff464affbc41bb38a673dcc2_Out_1_Vector2;
        Unity_Fraction_float2(_Add_66ffbf97cca14199972fa0e0db881b16_Out_2_Vector2, _Fraction_396a3fa0ff464affbc41bb38a673dcc2_Out_1_Vector2);
        float _Split_63ceb6dee6e54027b8f1467e4376cee2_R_1_Float = _Fraction_396a3fa0ff464affbc41bb38a673dcc2_Out_1_Vector2[0];
        float _Split_63ceb6dee6e54027b8f1467e4376cee2_G_2_Float = _Fraction_396a3fa0ff464affbc41bb38a673dcc2_Out_1_Vector2[1];
        float _Split_63ceb6dee6e54027b8f1467e4376cee2_B_3_Float = 0;
        float _Split_63ceb6dee6e54027b8f1467e4376cee2_A_4_Float = 0;
        float _RandomRange_484c84f4919c4cd8a517b36c15a038ea_Out_3_Float;
        Unity_RandomRange_float((_Split_63ceb6dee6e54027b8f1467e4376cee2_R_1_Float.xx), 0, _ScreenParams.x, _RandomRange_484c84f4919c4cd8a517b36c15a038ea_Out_3_Float);
        float _RandomRange_b63dcf8e65f740fab3cc51e40e1d5685_Out_3_Float;
        Unity_RandomRange_float((_Split_63ceb6dee6e54027b8f1467e4376cee2_G_2_Float.xx), 0, _ScreenParams.y, _RandomRange_b63dcf8e65f740fab3cc51e40e1d5685_Out_3_Float);
        float2 _Vector2_04d66487473642d79da224f4089554dd_Out_0_Vector2 = float2(_RandomRange_484c84f4919c4cd8a517b36c15a038ea_Out_3_Float, _RandomRange_b63dcf8e65f740fab3cc51e40e1d5685_Out_3_Float);
        float4 _ScreenPosition_a0254f37c23d45ffaf168c1d3439736b_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
        float4 _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RGBA_4_Vector4;
        float3 _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RGB_5_Vector3;
        float2 _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RG_6_Vector2;
        Unity_Combine_float(_ScreenParams.x, _ScreenParams.y, 0, 0, _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RGBA_4_Vector4, _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RGB_5_Vector3, _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RG_6_Vector2);
        float2 _Multiply_3e3b6c94f64b4ade9247e71c6d2f98d6_Out_2_Vector2;
        Unity_Multiply_float2_float2((_ScreenPosition_a0254f37c23d45ffaf168c1d3439736b_Out_0_Vector4.xy), _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RG_6_Vector2, _Multiply_3e3b6c94f64b4ade9247e71c6d2f98d6_Out_2_Vector2);
        float2 _Floor_4ba12f9eb3474734b0137ba643429b23_Out_1_Vector2;
        Unity_Floor_float2(_Multiply_3e3b6c94f64b4ade9247e71c6d2f98d6_Out_2_Vector2, _Floor_4ba12f9eb3474734b0137ba643429b23_Out_1_Vector2);
        float2 _Add_278d532c14154882bf1d47dde4d55ad3_Out_2_Vector2;
        Unity_Add_float2(_Vector2_04d66487473642d79da224f4089554dd_Out_0_Vector2, _Floor_4ba12f9eb3474734b0137ba643429b23_Out_1_Vector2, _Add_278d532c14154882bf1d47dde4d55ad3_Out_2_Vector2);
        float2 _Property_663e26b847554df8a237e6b5af6c8681_Out_0_Vector2 = Vector2_caefb2173a93413babdc548e42b92647;
        float2 _Divide_4576c5b640be4a1cba36f7b52c41b042_Out_2_Vector2;
        Unity_Divide_float2(_Add_278d532c14154882bf1d47dde4d55ad3_Out_2_Vector2, _Property_663e26b847554df8a237e6b5af6c8681_Out_0_Vector2, _Divide_4576c5b640be4a1cba36f7b52c41b042_Out_2_Vector2);
        float2 _Fraction_4942b1a15b0a4a019e485aa4d5bcb166_Out_1_Vector2;
        Unity_Fraction_float2(_Divide_4576c5b640be4a1cba36f7b52c41b042_Out_2_Vector2, _Fraction_4942b1a15b0a4a019e485aa4d5bcb166_Out_1_Vector2);
        float4 _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D).GetTransformedUV(_Fraction_4942b1a15b0a4a019e485aa4d5bcb166_Out_1_Vector2) );
        float _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_R_4_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4.r;
        float _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_G_5_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4.g;
        float _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_B_6_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4.b;
        float _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_A_7_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4.a;
        float _Split_95c0c3c67c5148a4945a0e0a8192275f_R_1_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4[0];
        float _Split_95c0c3c67c5148a4945a0e0a8192275f_G_2_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4[1];
        float _Split_95c0c3c67c5148a4945a0e0a8192275f_B_3_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4[2];
        float _Split_95c0c3c67c5148a4945a0e0a8192275f_A_4_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4[3];
        float3 _Vector3_54f2a95424284f65912dd46705fb9b33_Out_0_Vector3 = float3(_Split_95c0c3c67c5148a4945a0e0a8192275f_R_1_Float, _Split_95c0c3c67c5148a4945a0e0a8192275f_G_2_Float, _Split_95c0c3c67c5148a4945a0e0a8192275f_B_3_Float);
        float3 _Normalize_56d642ffdf80459aa5137d04c28ad137_Out_1_Vector3;
        Unity_Normalize_float3(_Vector3_54f2a95424284f65912dd46705fb9b33_Out_0_Vector3, _Normalize_56d642ffdf80459aa5137d04c28ad137_Out_1_Vector3);
        Output_1 = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_G_5_Float;
        Normal_2 = _Normalize_56d642ffdf80459aa5137d04c28ad137_Out_1_Vector3;
        }
        
        void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
        {
            Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Preview_float(float In, out float Out)
        {
            Out = In;
        }
        
        void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
        {
            Out = smoothstep(Edge1, Edge2, In);
        }
        
        void Unity_Minimum_float(float A, float B, out float Out)
        {
            Out = min(A, B);
        };
        
        void Unity_SceneDepth_Linear01_float(float4 UV, out float Out)
        {
            Out = Linear01Depth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        struct Bindings_SoftParticles_18333bb3c326140abbd08c558de3bf83_float
        {
        float4 ScreenPosition;
        float2 NDCPosition;
        };
        
        void SG_SoftParticles_18333bb3c326140abbd08c558de3bf83_float(float Vector1_b12c77296e1146fa9f4d315fc85d04ab, float Vector1_0414f85da44b41c9983c5044458d5297, float Vector1_0695185d2cd141a68dcd8c1a2daa3c40, Bindings_SoftParticles_18333bb3c326140abbd08c558de3bf83_float IN, out float OutVector1_1)
        {
        float _SceneDepth_276a1c9e05034cdfa6529a8931fb79f7_Out_1_Float;
        Unity_SceneDepth_Linear01_float(float4(IN.NDCPosition.xy, 0, 0), _SceneDepth_276a1c9e05034cdfa6529a8931fb79f7_Out_1_Float);
        float _Multiply_6ce0bce77db34631b737de6ad4fa0e04_Out_2_Float;
        Unity_Multiply_float_float(_SceneDepth_276a1c9e05034cdfa6529a8931fb79f7_Out_1_Float, _ProjectionParams.z, _Multiply_6ce0bce77db34631b737de6ad4fa0e04_Out_2_Float);
        float _Property_6a749dd773734fab83f490ee0fb3c7f0_Out_0_Float = Vector1_0695185d2cd141a68dcd8c1a2daa3c40;
        float4 _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4 = IN.ScreenPosition;
        float _Split_f05c829a2fa34687a4c46a8484cc4eab_R_1_Float = _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4[0];
        float _Split_f05c829a2fa34687a4c46a8484cc4eab_G_2_Float = _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4[1];
        float _Split_f05c829a2fa34687a4c46a8484cc4eab_B_3_Float = _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4[2];
        float _Split_f05c829a2fa34687a4c46a8484cc4eab_A_4_Float = _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4[3];
        float _Add_39d32d1e2d4e46efa943593378a7b5d4_Out_2_Float;
        Unity_Add_float(_Property_6a749dd773734fab83f490ee0fb3c7f0_Out_0_Float, _Split_f05c829a2fa34687a4c46a8484cc4eab_A_4_Float, _Add_39d32d1e2d4e46efa943593378a7b5d4_Out_2_Float);
        float _Subtract_0db08fa14d48402dbe0edd1a2df1d281_Out_2_Float;
        Unity_Subtract_float(_Multiply_6ce0bce77db34631b737de6ad4fa0e04_Out_2_Float, _Add_39d32d1e2d4e46efa943593378a7b5d4_Out_2_Float, _Subtract_0db08fa14d48402dbe0edd1a2df1d281_Out_2_Float);
        float _Property_a1e60e28f8174d88ba77f34de3664a02_Out_0_Float = Vector1_b12c77296e1146fa9f4d315fc85d04ab;
        float _Property_72d03f4fff3e4c5f8607d2320b27c4d0_Out_0_Float = Vector1_0414f85da44b41c9983c5044458d5297;
        float2 _Vector2_d5e84c16e02d464b97da10162141cf76_Out_0_Vector2 = float2(_Property_a1e60e28f8174d88ba77f34de3664a02_Out_0_Float, _Property_72d03f4fff3e4c5f8607d2320b27c4d0_Out_0_Float);
        float _Remap_41d3d1abc133496d946826964dcf67e5_Out_3_Float;
        Unity_Remap_float(_Subtract_0db08fa14d48402dbe0edd1a2df1d281_Out_2_Float, _Vector2_d5e84c16e02d464b97da10162141cf76_Out_0_Vector2, float2 (0, 1), _Remap_41d3d1abc133496d946826964dcf67e5_Out_3_Float);
        float _Saturate_6dc9e8b8bd6a4471aba1733b5fa31a0d_Out_1_Float;
        Unity_Saturate_float(_Remap_41d3d1abc133496d946826964dcf67e5_Out_3_Float, _Saturate_6dc9e8b8bd6a4471aba1733b5fa31a0d_Out_1_Float);
        OutVector1_1 = _Saturate_6dc9e8b8bd6a4471aba1733b5fa31a0d_Out_1_Float;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            UnityTexture2D _Property_06ae7b2647934302a2bc77f5a16cfac2_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(Texture2D_3DC46A50);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_06ae7b2647934302a2bc77f5a16cfac2_Out_0_Texture2D.tex, _Property_06ae7b2647934302a2bc77f5a16cfac2_Out_0_Texture2D.samplerstate, _Property_06ae7b2647934302a2bc77f5a16cfac2_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_R_4_Float = _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4.r;
            float _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_G_5_Float = _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4.g;
            float _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_B_6_Float = _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4.b;
            float _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_A_7_Float = _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4.a;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _OneMinus_a90ee5ec5cfd410780133317beeee956_Out_1_Float;
            Unity_OneMinus_float(_SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_A_7_Float, _OneMinus_a90ee5ec5cfd410780133317beeee956_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_fbf5240d6a974443afb86e71299332dc_Out_0_Float = Vector1_2a375228e8d3437382814520b0cd8644;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Power_4d5b0fe5fe894f37bf2bf51adfe6403f_Out_2_Float;
            Unity_Power_float(_OneMinus_a90ee5ec5cfd410780133317beeee956_Out_1_Float, _Property_fbf5240d6a974443afb86e71299332dc_Out_0_Float, _Power_4d5b0fe5fe894f37bf2bf51adfe6403f_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _OneMinus_12776925fcba4b8f8c431735c34d4ef0_Out_1_Float;
            Unity_OneMinus_float(_Power_4d5b0fe5fe894f37bf2bf51adfe6403f_Out_2_Float, _OneMinus_12776925fcba4b8f8c431735c34d4ef0_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Length_e04efe434e9a4ecc998e1dd3c3d5e90e_Out_1_Float;
            Unity_Length_float3(SHADERGRAPH_OBJECT_POSITION, _Length_e04efe434e9a4ecc998e1dd3c3d5e90e_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            Bindings_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679;
            _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679.NDCPosition = IN.NDCPosition;
            _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679.TimeParameters = IN.TimeParameters;
            float _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Output_1_Float;
            float3 _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Normal_2_Vector3;
            SG_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float(float2 (256, 256), 1, _Length_e04efe434e9a4ecc998e1dd3c3d5e90e_Out_1_Float, _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679, _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Output_1_Float, _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Normal_2_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Remap_bbf4dcb1a53440238a83cbaea629f9a2_Out_3_Float;
            Unity_Remap_float(_SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Output_1_Float, float2 (0, 1), float2 (0.8, 1.1), _Remap_bbf4dcb1a53440238a83cbaea629f9a2_Out_3_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4 = _BA_CloudData;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Split_3bcb0174a9954d879a71e473885b9e1b_R_1_Float = _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4[0];
            float _Split_3bcb0174a9954d879a71e473885b9e1b_G_2_Float = _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4[1];
            float _Split_3bcb0174a9954d879a71e473885b9e1b_B_3_Float = _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4[2];
            float _Split_3bcb0174a9954d879a71e473885b9e1b_A_4_Float = _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4[3];
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Multiply_211f37f5b555434d956f42443155bc4a_Out_2_Float;
            Unity_Multiply_float_float(_Remap_bbf4dcb1a53440238a83cbaea629f9a2_Out_3_Float, _Split_3bcb0174a9954d879a71e473885b9e1b_A_4_Float, _Multiply_211f37f5b555434d956f42443155bc4a_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4 = _BA_CloudData;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_R_1_Float = _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4[0];
            float _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_G_2_Float = _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4[1];
            float _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_B_3_Float = _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4[2];
            float _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_A_4_Float = _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4[3];
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            #if defined(BOOLEAN_9A9F210EF4D44680B8F0CB7ACD99DFBB_ON)
            float _BlueNoise_b07e75159dec4ad49e7f81e3592b900e_Out_0_Float = _Multiply_211f37f5b555434d956f42443155bc4a_Out_2_Float;
            #else
            float _BlueNoise_b07e75159dec4ad49e7f81e3592b900e_Out_0_Float = _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_A_4_Float;
            #endif
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _OneMinus_21be261ae35940709d0db1914ee9855c_Out_1_Float;
            Unity_OneMinus_float(_BlueNoise_b07e75159dec4ad49e7f81e3592b900e_Out_0_Float, _OneMinus_21be261ae35940709d0db1914ee9855c_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Add_2f041a64479546ff91a26ed1dbaffeee_Out_2_Float;
            Unity_Add_float(_OneMinus_21be261ae35940709d0db1914ee9855c_Out_1_Float, 0.1, _Add_2f041a64479546ff91a26ed1dbaffeee_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Preview_d4d26446fc904b76afe70165bc2d684f_Out_1_Float;
            Unity_Preview_float(_SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_B_6_Float, _Preview_d4d26446fc904b76afe70165bc2d684f_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Smoothstep_3d813f1fadfa4b9b8587408892645438_Out_3_Float;
            Unity_Smoothstep_float(_OneMinus_21be261ae35940709d0db1914ee9855c_Out_1_Float, _Add_2f041a64479546ff91a26ed1dbaffeee_Out_2_Float, _Preview_d4d26446fc904b76afe70165bc2d684f_Out_1_Float, _Smoothstep_3d813f1fadfa4b9b8587408892645438_Out_3_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Minimum_9603d95647104c398dcf64d0bf690e71_Out_2_Float;
            Unity_Minimum_float(_OneMinus_12776925fcba4b8f8c431735c34d4ef0_Out_1_Float, _Smoothstep_3d813f1fadfa4b9b8587408892645438_Out_3_Float, _Minimum_9603d95647104c398dcf64d0bf690e71_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float2 _Property_4d3f52434f0e4f098cc877f3f4dadbf9_Out_0_Vector2 = Vector2_65050940ec244ae3bf42db206c2dccca;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Split_8604fb34fc04432c845c2474347347cc_R_1_Float = _Property_4d3f52434f0e4f098cc877f3f4dadbf9_Out_0_Vector2[0];
            float _Split_8604fb34fc04432c845c2474347347cc_G_2_Float = _Property_4d3f52434f0e4f098cc877f3f4dadbf9_Out_0_Vector2[1];
            float _Split_8604fb34fc04432c845c2474347347cc_B_3_Float = 0;
            float _Split_8604fb34fc04432c845c2474347347cc_A_4_Float = 0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _OneMinus_2253c4aebc784af4b7bfe91e392d3d1c_Out_1_Float;
            Unity_OneMinus_float(_Preview_d4d26446fc904b76afe70165bc2d684f_Out_1_Float, _OneMinus_2253c4aebc784af4b7bfe91e392d3d1c_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_a7bc7d6aca7a4351a54c042414c8dca3_Out_0_Float = Vector1_2d1f70e8efb04843b1f8184ae06925e0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Multiply_521b54ef01d8415cacbbab05d9ef91b4_Out_2_Float;
            Unity_Multiply_float_float(_OneMinus_2253c4aebc784af4b7bfe91e392d3d1c_Out_1_Float, _Property_a7bc7d6aca7a4351a54c042414c8dca3_Out_0_Float, _Multiply_521b54ef01d8415cacbbab05d9ef91b4_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            Bindings_SoftParticles_18333bb3c326140abbd08c558de3bf83_float _SoftParticles_33d22c55e97e4bbf9658984efedb651d;
            _SoftParticles_33d22c55e97e4bbf9658984efedb651d.ScreenPosition = IN.ScreenPosition;
            _SoftParticles_33d22c55e97e4bbf9658984efedb651d.NDCPosition = IN.NDCPosition;
            float _SoftParticles_33d22c55e97e4bbf9658984efedb651d_OutVector1_1_Float;
            SG_SoftParticles_18333bb3c326140abbd08c558de3bf83_float(_Split_8604fb34fc04432c845c2474347347cc_R_1_Float, _Split_8604fb34fc04432c845c2474347347cc_G_2_Float, _Multiply_521b54ef01d8415cacbbab05d9ef91b4_Out_2_Float, _SoftParticles_33d22c55e97e4bbf9658984efedb651d, _SoftParticles_33d22c55e97e4bbf9658984efedb651d_OutVector1_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Multiply_603283fc9007453b86c0e29e22dc1fdf_Out_2_Float;
            Unity_Multiply_float_float(_Minimum_9603d95647104c398dcf64d0bf690e71_Out_2_Float, _SoftParticles_33d22c55e97e4bbf9658984efedb651d_OutVector1_1_Float, _Multiply_603283fc9007453b86c0e29e22dc1fdf_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            #if defined(BOOLEAN_EAB072D9159A48C685623C0E39EA73B0_ON)
            float _SoftParticles_e87b688bcc4a4c4387199157c1c53214_Out_0_Float = _Multiply_603283fc9007453b86c0e29e22dc1fdf_Out_2_Float;
            #else
            float _SoftParticles_e87b688bcc4a4c4387199157c1c53214_Out_0_Float = _Minimum_9603d95647104c398dcf64d0bf690e71_Out_2_Float;
            #endif
            #endif
            surface.Alpha = _SoftParticles_e87b688bcc4a4c4387199157c1c53214_Out_0_Float;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpaceNormal =                          input.normalOS;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpaceTangent =                         input.tangentOS.xyz;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpacePosition =                        input.positionOS;
        #endif
        
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.WorldSpacePosition = input.positionWS;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);
        #endif
        
        
            #if UNITY_UV_STARTS_AT_TOP
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
        #endif
        
            #else
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
        #endif
        
            #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.NDCPosition.y = 1.0f - output.NDCPosition.y;
        #endif
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.uv0 = input.texCoord0;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
        #endif
        
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthNormalsOnlyPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
        
        // Render State
        Cull Off
        ZTest LEqual
        ZWrite On
        ColorMask 0
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
        #pragma shader_feature_local _ BOOLEAN_EAB072D9159A48C685623C0E39EA73B0_ON
        #pragma shader_feature_local _ BOOLEAN_9A9F210EF4D44680B8F0CB7ACD99DFBB_ON
        
        #if defined(BOOLEAN_EAB072D9159A48C685623C0E39EA73B0_ON) && defined(BOOLEAN_9A9F210EF4D44680B8F0CB7ACD99DFBB_ON)
            #define KEYWORD_PERMUTATION_0
        #elif defined(BOOLEAN_EAB072D9159A48C685623C0E39EA73B0_ON)
            #define KEYWORD_PERMUTATION_1
        #elif defined(BOOLEAN_9A9F210EF4D44680B8F0CB7ACD99DFBB_ON)
            #define KEYWORD_PERMUTATION_2
        #else
            #define KEYWORD_PERMUTATION_3
        #endif
        
        
        // Defines
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_NORMAL
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_TANGENT
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_TEXCOORD0
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define VARYINGS_NEED_POSITION_WS
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define VARYINGS_NEED_NORMAL_WS
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define VARYINGS_NEED_TEXCOORD0
        #endif
        
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_SHADOWCASTER
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define REQUIRE_DEPTH_TEXTURE
        #endif
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 positionOS : POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 normalOS : NORMAL;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 tangentOS : TANGENT;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 uv0 : TEXCOORD0;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
            #endif
        };
        struct Varyings
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 positionCS : SV_POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 positionWS;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 normalWS;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 texCoord0;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
            #endif
        };
        struct SurfaceDescriptionInputs
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 WorldSpacePosition;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 ScreenPosition;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float2 NDCPosition;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float2 PixelPosition;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 uv0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 TimeParameters;
            #endif
        };
        struct VertexDescriptionInputs
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpaceNormal;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpaceTangent;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpacePosition;
            #endif
        };
        struct PackedVaryings
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 positionCS : SV_POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 texCoord0 : INTERP0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 positionWS : INTERP1;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 normalWS : INTERP2;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
            #endif
        };
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.texCoord0.xyzw = input.texCoord0;
            output.positionWS.xyz = input.positionWS;
            output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.texCoord0.xyzw;
            output.positionWS = input.positionWS.xyz;
            output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        #endif
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D_TexelSize;
        float4 Color_7eff2e7fc27440719d0a871a39dabdd3;
        float Vector1_2a375228e8d3437382814520b0cd8644;
        float Vector1_5f4b185241524942830132fc71554a33;
        float Vector1_2d1f70e8efb04843b1f8184ae06925e0;
        float Vector1_33982692;
        float Vector1_10DA9AB3;
        float2 Vector2_65050940ec244ae3bf42db206c2dccca;
        float4 Texture2D_4B382D9C_TexelSize;
        float4 Texture2D_3DC46A50_TexelSize;
        float4x4 _BA_SkyboxMatrix;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D);
        SAMPLER(sampler_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D);
        TEXTURE2D(Texture2D_4B382D9C);
        SAMPLER(samplerTexture2D_4B382D9C);
        TEXTURE2D(Texture2D_3DC46A50);
        SAMPLER(samplerTexture2D_3DC46A50);
        float _BA_SkyboxRatio;
        float4 _BA_CloudData;
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_OneMinus_float(float In, out float Out)
        {
            Out = 1 - In;
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Length_float3(float3 In, out float Out)
        {
            Out = length(In);
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
        Out = A * B;
        }
        
        void Unity_Branch_float2(float Predicate, float2 True, float2 False, out float2 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
        Out = A * B;
        }
        
        void Unity_Add_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A + B;
        }
        
        void Unity_Fraction_float2(float2 In, out float2 Out)
        {
            Out = frac(In);
        }
        
        void Unity_RandomRange_float(float2 Seed, float Min, float Max, out float Out)
        {
             float randomno =  frac(sin(dot(Seed, float2(12.9898, 78.233)))*43758.5453);
             Out = lerp(Min, Max, randomno);
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Floor_float2(float2 In, out float2 Out)
        {
            Out = floor(In);
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        void Unity_Normalize_float3(float3 In, out float3 Out)
        {
            Out = normalize(In);
        }
        
        struct Bindings_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float
        {
        float2 NDCPosition;
        float3 TimeParameters;
        };
        
        void SG_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float(float2 Vector2_caefb2173a93413babdc548e42b92647, float Boolean_d17c4aa779b140eb89358932ea6663e5, float Vector1_397d6d54d391448f841b568356acd9d5, Bindings_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float IN, out float Output_1, out float3 Normal_2)
        {
        float _Property_721476b2541f47d9a2a9269e3083e0d6_Out_0_Float = Vector1_397d6d54d391448f841b568356acd9d5;
        float _Multiply_7969691e0cc14256bab4e3e287352010_Out_2_Float;
        Unity_Multiply_float_float(_Property_721476b2541f47d9a2a9269e3083e0d6_Out_0_Float, 0.321, _Multiply_7969691e0cc14256bab4e3e287352010_Out_2_Float);
        float _Property_81032f19979848909a7ca400e4b48342_Out_0_Boolean = Boolean_d17c4aa779b140eb89358932ea6663e5;
        float2 _Vector2_fd3f4c200f834792b2a20662ee9e881a_Out_0_Vector2 = float2(IN.TimeParameters.x, IN.TimeParameters.x);
        float2 _Branch_0fc6b47a1c3b42de9b4e91943c99b893_Out_3_Vector2;
        Unity_Branch_float2(_Property_81032f19979848909a7ca400e4b48342_Out_0_Boolean, _Vector2_fd3f4c200f834792b2a20662ee9e881a_Out_0_Vector2, float2(0, 0), _Branch_0fc6b47a1c3b42de9b4e91943c99b893_Out_3_Vector2);
        float2 _Multiply_2a72b24c4beb4af4911a02dc52727555_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Branch_0fc6b47a1c3b42de9b4e91943c99b893_Out_3_Vector2, float2(0.018, 0.32), _Multiply_2a72b24c4beb4af4911a02dc52727555_Out_2_Vector2);
        float2 _Add_66ffbf97cca14199972fa0e0db881b16_Out_2_Vector2;
        Unity_Add_float2((_Multiply_7969691e0cc14256bab4e3e287352010_Out_2_Float.xx), _Multiply_2a72b24c4beb4af4911a02dc52727555_Out_2_Vector2, _Add_66ffbf97cca14199972fa0e0db881b16_Out_2_Vector2);
        float2 _Fraction_396a3fa0ff464affbc41bb38a673dcc2_Out_1_Vector2;
        Unity_Fraction_float2(_Add_66ffbf97cca14199972fa0e0db881b16_Out_2_Vector2, _Fraction_396a3fa0ff464affbc41bb38a673dcc2_Out_1_Vector2);
        float _Split_63ceb6dee6e54027b8f1467e4376cee2_R_1_Float = _Fraction_396a3fa0ff464affbc41bb38a673dcc2_Out_1_Vector2[0];
        float _Split_63ceb6dee6e54027b8f1467e4376cee2_G_2_Float = _Fraction_396a3fa0ff464affbc41bb38a673dcc2_Out_1_Vector2[1];
        float _Split_63ceb6dee6e54027b8f1467e4376cee2_B_3_Float = 0;
        float _Split_63ceb6dee6e54027b8f1467e4376cee2_A_4_Float = 0;
        float _RandomRange_484c84f4919c4cd8a517b36c15a038ea_Out_3_Float;
        Unity_RandomRange_float((_Split_63ceb6dee6e54027b8f1467e4376cee2_R_1_Float.xx), 0, _ScreenParams.x, _RandomRange_484c84f4919c4cd8a517b36c15a038ea_Out_3_Float);
        float _RandomRange_b63dcf8e65f740fab3cc51e40e1d5685_Out_3_Float;
        Unity_RandomRange_float((_Split_63ceb6dee6e54027b8f1467e4376cee2_G_2_Float.xx), 0, _ScreenParams.y, _RandomRange_b63dcf8e65f740fab3cc51e40e1d5685_Out_3_Float);
        float2 _Vector2_04d66487473642d79da224f4089554dd_Out_0_Vector2 = float2(_RandomRange_484c84f4919c4cd8a517b36c15a038ea_Out_3_Float, _RandomRange_b63dcf8e65f740fab3cc51e40e1d5685_Out_3_Float);
        float4 _ScreenPosition_a0254f37c23d45ffaf168c1d3439736b_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
        float4 _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RGBA_4_Vector4;
        float3 _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RGB_5_Vector3;
        float2 _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RG_6_Vector2;
        Unity_Combine_float(_ScreenParams.x, _ScreenParams.y, 0, 0, _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RGBA_4_Vector4, _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RGB_5_Vector3, _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RG_6_Vector2);
        float2 _Multiply_3e3b6c94f64b4ade9247e71c6d2f98d6_Out_2_Vector2;
        Unity_Multiply_float2_float2((_ScreenPosition_a0254f37c23d45ffaf168c1d3439736b_Out_0_Vector4.xy), _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RG_6_Vector2, _Multiply_3e3b6c94f64b4ade9247e71c6d2f98d6_Out_2_Vector2);
        float2 _Floor_4ba12f9eb3474734b0137ba643429b23_Out_1_Vector2;
        Unity_Floor_float2(_Multiply_3e3b6c94f64b4ade9247e71c6d2f98d6_Out_2_Vector2, _Floor_4ba12f9eb3474734b0137ba643429b23_Out_1_Vector2);
        float2 _Add_278d532c14154882bf1d47dde4d55ad3_Out_2_Vector2;
        Unity_Add_float2(_Vector2_04d66487473642d79da224f4089554dd_Out_0_Vector2, _Floor_4ba12f9eb3474734b0137ba643429b23_Out_1_Vector2, _Add_278d532c14154882bf1d47dde4d55ad3_Out_2_Vector2);
        float2 _Property_663e26b847554df8a237e6b5af6c8681_Out_0_Vector2 = Vector2_caefb2173a93413babdc548e42b92647;
        float2 _Divide_4576c5b640be4a1cba36f7b52c41b042_Out_2_Vector2;
        Unity_Divide_float2(_Add_278d532c14154882bf1d47dde4d55ad3_Out_2_Vector2, _Property_663e26b847554df8a237e6b5af6c8681_Out_0_Vector2, _Divide_4576c5b640be4a1cba36f7b52c41b042_Out_2_Vector2);
        float2 _Fraction_4942b1a15b0a4a019e485aa4d5bcb166_Out_1_Vector2;
        Unity_Fraction_float2(_Divide_4576c5b640be4a1cba36f7b52c41b042_Out_2_Vector2, _Fraction_4942b1a15b0a4a019e485aa4d5bcb166_Out_1_Vector2);
        float4 _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D).GetTransformedUV(_Fraction_4942b1a15b0a4a019e485aa4d5bcb166_Out_1_Vector2) );
        float _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_R_4_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4.r;
        float _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_G_5_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4.g;
        float _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_B_6_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4.b;
        float _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_A_7_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4.a;
        float _Split_95c0c3c67c5148a4945a0e0a8192275f_R_1_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4[0];
        float _Split_95c0c3c67c5148a4945a0e0a8192275f_G_2_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4[1];
        float _Split_95c0c3c67c5148a4945a0e0a8192275f_B_3_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4[2];
        float _Split_95c0c3c67c5148a4945a0e0a8192275f_A_4_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4[3];
        float3 _Vector3_54f2a95424284f65912dd46705fb9b33_Out_0_Vector3 = float3(_Split_95c0c3c67c5148a4945a0e0a8192275f_R_1_Float, _Split_95c0c3c67c5148a4945a0e0a8192275f_G_2_Float, _Split_95c0c3c67c5148a4945a0e0a8192275f_B_3_Float);
        float3 _Normalize_56d642ffdf80459aa5137d04c28ad137_Out_1_Vector3;
        Unity_Normalize_float3(_Vector3_54f2a95424284f65912dd46705fb9b33_Out_0_Vector3, _Normalize_56d642ffdf80459aa5137d04c28ad137_Out_1_Vector3);
        Output_1 = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_G_5_Float;
        Normal_2 = _Normalize_56d642ffdf80459aa5137d04c28ad137_Out_1_Vector3;
        }
        
        void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
        {
            Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Preview_float(float In, out float Out)
        {
            Out = In;
        }
        
        void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
        {
            Out = smoothstep(Edge1, Edge2, In);
        }
        
        void Unity_Minimum_float(float A, float B, out float Out)
        {
            Out = min(A, B);
        };
        
        void Unity_SceneDepth_Linear01_float(float4 UV, out float Out)
        {
            Out = Linear01Depth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        struct Bindings_SoftParticles_18333bb3c326140abbd08c558de3bf83_float
        {
        float4 ScreenPosition;
        float2 NDCPosition;
        };
        
        void SG_SoftParticles_18333bb3c326140abbd08c558de3bf83_float(float Vector1_b12c77296e1146fa9f4d315fc85d04ab, float Vector1_0414f85da44b41c9983c5044458d5297, float Vector1_0695185d2cd141a68dcd8c1a2daa3c40, Bindings_SoftParticles_18333bb3c326140abbd08c558de3bf83_float IN, out float OutVector1_1)
        {
        float _SceneDepth_276a1c9e05034cdfa6529a8931fb79f7_Out_1_Float;
        Unity_SceneDepth_Linear01_float(float4(IN.NDCPosition.xy, 0, 0), _SceneDepth_276a1c9e05034cdfa6529a8931fb79f7_Out_1_Float);
        float _Multiply_6ce0bce77db34631b737de6ad4fa0e04_Out_2_Float;
        Unity_Multiply_float_float(_SceneDepth_276a1c9e05034cdfa6529a8931fb79f7_Out_1_Float, _ProjectionParams.z, _Multiply_6ce0bce77db34631b737de6ad4fa0e04_Out_2_Float);
        float _Property_6a749dd773734fab83f490ee0fb3c7f0_Out_0_Float = Vector1_0695185d2cd141a68dcd8c1a2daa3c40;
        float4 _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4 = IN.ScreenPosition;
        float _Split_f05c829a2fa34687a4c46a8484cc4eab_R_1_Float = _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4[0];
        float _Split_f05c829a2fa34687a4c46a8484cc4eab_G_2_Float = _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4[1];
        float _Split_f05c829a2fa34687a4c46a8484cc4eab_B_3_Float = _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4[2];
        float _Split_f05c829a2fa34687a4c46a8484cc4eab_A_4_Float = _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4[3];
        float _Add_39d32d1e2d4e46efa943593378a7b5d4_Out_2_Float;
        Unity_Add_float(_Property_6a749dd773734fab83f490ee0fb3c7f0_Out_0_Float, _Split_f05c829a2fa34687a4c46a8484cc4eab_A_4_Float, _Add_39d32d1e2d4e46efa943593378a7b5d4_Out_2_Float);
        float _Subtract_0db08fa14d48402dbe0edd1a2df1d281_Out_2_Float;
        Unity_Subtract_float(_Multiply_6ce0bce77db34631b737de6ad4fa0e04_Out_2_Float, _Add_39d32d1e2d4e46efa943593378a7b5d4_Out_2_Float, _Subtract_0db08fa14d48402dbe0edd1a2df1d281_Out_2_Float);
        float _Property_a1e60e28f8174d88ba77f34de3664a02_Out_0_Float = Vector1_b12c77296e1146fa9f4d315fc85d04ab;
        float _Property_72d03f4fff3e4c5f8607d2320b27c4d0_Out_0_Float = Vector1_0414f85da44b41c9983c5044458d5297;
        float2 _Vector2_d5e84c16e02d464b97da10162141cf76_Out_0_Vector2 = float2(_Property_a1e60e28f8174d88ba77f34de3664a02_Out_0_Float, _Property_72d03f4fff3e4c5f8607d2320b27c4d0_Out_0_Float);
        float _Remap_41d3d1abc133496d946826964dcf67e5_Out_3_Float;
        Unity_Remap_float(_Subtract_0db08fa14d48402dbe0edd1a2df1d281_Out_2_Float, _Vector2_d5e84c16e02d464b97da10162141cf76_Out_0_Vector2, float2 (0, 1), _Remap_41d3d1abc133496d946826964dcf67e5_Out_3_Float);
        float _Saturate_6dc9e8b8bd6a4471aba1733b5fa31a0d_Out_1_Float;
        Unity_Saturate_float(_Remap_41d3d1abc133496d946826964dcf67e5_Out_3_Float, _Saturate_6dc9e8b8bd6a4471aba1733b5fa31a0d_Out_1_Float);
        OutVector1_1 = _Saturate_6dc9e8b8bd6a4471aba1733b5fa31a0d_Out_1_Float;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            UnityTexture2D _Property_06ae7b2647934302a2bc77f5a16cfac2_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(Texture2D_3DC46A50);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_06ae7b2647934302a2bc77f5a16cfac2_Out_0_Texture2D.tex, _Property_06ae7b2647934302a2bc77f5a16cfac2_Out_0_Texture2D.samplerstate, _Property_06ae7b2647934302a2bc77f5a16cfac2_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_R_4_Float = _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4.r;
            float _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_G_5_Float = _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4.g;
            float _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_B_6_Float = _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4.b;
            float _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_A_7_Float = _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4.a;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _OneMinus_a90ee5ec5cfd410780133317beeee956_Out_1_Float;
            Unity_OneMinus_float(_SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_A_7_Float, _OneMinus_a90ee5ec5cfd410780133317beeee956_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_fbf5240d6a974443afb86e71299332dc_Out_0_Float = Vector1_2a375228e8d3437382814520b0cd8644;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Power_4d5b0fe5fe894f37bf2bf51adfe6403f_Out_2_Float;
            Unity_Power_float(_OneMinus_a90ee5ec5cfd410780133317beeee956_Out_1_Float, _Property_fbf5240d6a974443afb86e71299332dc_Out_0_Float, _Power_4d5b0fe5fe894f37bf2bf51adfe6403f_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _OneMinus_12776925fcba4b8f8c431735c34d4ef0_Out_1_Float;
            Unity_OneMinus_float(_Power_4d5b0fe5fe894f37bf2bf51adfe6403f_Out_2_Float, _OneMinus_12776925fcba4b8f8c431735c34d4ef0_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Length_e04efe434e9a4ecc998e1dd3c3d5e90e_Out_1_Float;
            Unity_Length_float3(SHADERGRAPH_OBJECT_POSITION, _Length_e04efe434e9a4ecc998e1dd3c3d5e90e_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            Bindings_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679;
            _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679.NDCPosition = IN.NDCPosition;
            _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679.TimeParameters = IN.TimeParameters;
            float _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Output_1_Float;
            float3 _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Normal_2_Vector3;
            SG_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float(float2 (256, 256), 1, _Length_e04efe434e9a4ecc998e1dd3c3d5e90e_Out_1_Float, _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679, _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Output_1_Float, _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Normal_2_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Remap_bbf4dcb1a53440238a83cbaea629f9a2_Out_3_Float;
            Unity_Remap_float(_SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Output_1_Float, float2 (0, 1), float2 (0.8, 1.1), _Remap_bbf4dcb1a53440238a83cbaea629f9a2_Out_3_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4 = _BA_CloudData;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Split_3bcb0174a9954d879a71e473885b9e1b_R_1_Float = _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4[0];
            float _Split_3bcb0174a9954d879a71e473885b9e1b_G_2_Float = _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4[1];
            float _Split_3bcb0174a9954d879a71e473885b9e1b_B_3_Float = _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4[2];
            float _Split_3bcb0174a9954d879a71e473885b9e1b_A_4_Float = _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4[3];
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Multiply_211f37f5b555434d956f42443155bc4a_Out_2_Float;
            Unity_Multiply_float_float(_Remap_bbf4dcb1a53440238a83cbaea629f9a2_Out_3_Float, _Split_3bcb0174a9954d879a71e473885b9e1b_A_4_Float, _Multiply_211f37f5b555434d956f42443155bc4a_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4 = _BA_CloudData;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_R_1_Float = _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4[0];
            float _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_G_2_Float = _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4[1];
            float _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_B_3_Float = _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4[2];
            float _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_A_4_Float = _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4[3];
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            #if defined(BOOLEAN_9A9F210EF4D44680B8F0CB7ACD99DFBB_ON)
            float _BlueNoise_b07e75159dec4ad49e7f81e3592b900e_Out_0_Float = _Multiply_211f37f5b555434d956f42443155bc4a_Out_2_Float;
            #else
            float _BlueNoise_b07e75159dec4ad49e7f81e3592b900e_Out_0_Float = _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_A_4_Float;
            #endif
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _OneMinus_21be261ae35940709d0db1914ee9855c_Out_1_Float;
            Unity_OneMinus_float(_BlueNoise_b07e75159dec4ad49e7f81e3592b900e_Out_0_Float, _OneMinus_21be261ae35940709d0db1914ee9855c_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Add_2f041a64479546ff91a26ed1dbaffeee_Out_2_Float;
            Unity_Add_float(_OneMinus_21be261ae35940709d0db1914ee9855c_Out_1_Float, 0.1, _Add_2f041a64479546ff91a26ed1dbaffeee_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Preview_d4d26446fc904b76afe70165bc2d684f_Out_1_Float;
            Unity_Preview_float(_SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_B_6_Float, _Preview_d4d26446fc904b76afe70165bc2d684f_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Smoothstep_3d813f1fadfa4b9b8587408892645438_Out_3_Float;
            Unity_Smoothstep_float(_OneMinus_21be261ae35940709d0db1914ee9855c_Out_1_Float, _Add_2f041a64479546ff91a26ed1dbaffeee_Out_2_Float, _Preview_d4d26446fc904b76afe70165bc2d684f_Out_1_Float, _Smoothstep_3d813f1fadfa4b9b8587408892645438_Out_3_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Minimum_9603d95647104c398dcf64d0bf690e71_Out_2_Float;
            Unity_Minimum_float(_OneMinus_12776925fcba4b8f8c431735c34d4ef0_Out_1_Float, _Smoothstep_3d813f1fadfa4b9b8587408892645438_Out_3_Float, _Minimum_9603d95647104c398dcf64d0bf690e71_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float2 _Property_4d3f52434f0e4f098cc877f3f4dadbf9_Out_0_Vector2 = Vector2_65050940ec244ae3bf42db206c2dccca;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Split_8604fb34fc04432c845c2474347347cc_R_1_Float = _Property_4d3f52434f0e4f098cc877f3f4dadbf9_Out_0_Vector2[0];
            float _Split_8604fb34fc04432c845c2474347347cc_G_2_Float = _Property_4d3f52434f0e4f098cc877f3f4dadbf9_Out_0_Vector2[1];
            float _Split_8604fb34fc04432c845c2474347347cc_B_3_Float = 0;
            float _Split_8604fb34fc04432c845c2474347347cc_A_4_Float = 0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _OneMinus_2253c4aebc784af4b7bfe91e392d3d1c_Out_1_Float;
            Unity_OneMinus_float(_Preview_d4d26446fc904b76afe70165bc2d684f_Out_1_Float, _OneMinus_2253c4aebc784af4b7bfe91e392d3d1c_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_a7bc7d6aca7a4351a54c042414c8dca3_Out_0_Float = Vector1_2d1f70e8efb04843b1f8184ae06925e0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Multiply_521b54ef01d8415cacbbab05d9ef91b4_Out_2_Float;
            Unity_Multiply_float_float(_OneMinus_2253c4aebc784af4b7bfe91e392d3d1c_Out_1_Float, _Property_a7bc7d6aca7a4351a54c042414c8dca3_Out_0_Float, _Multiply_521b54ef01d8415cacbbab05d9ef91b4_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            Bindings_SoftParticles_18333bb3c326140abbd08c558de3bf83_float _SoftParticles_33d22c55e97e4bbf9658984efedb651d;
            _SoftParticles_33d22c55e97e4bbf9658984efedb651d.ScreenPosition = IN.ScreenPosition;
            _SoftParticles_33d22c55e97e4bbf9658984efedb651d.NDCPosition = IN.NDCPosition;
            float _SoftParticles_33d22c55e97e4bbf9658984efedb651d_OutVector1_1_Float;
            SG_SoftParticles_18333bb3c326140abbd08c558de3bf83_float(_Split_8604fb34fc04432c845c2474347347cc_R_1_Float, _Split_8604fb34fc04432c845c2474347347cc_G_2_Float, _Multiply_521b54ef01d8415cacbbab05d9ef91b4_Out_2_Float, _SoftParticles_33d22c55e97e4bbf9658984efedb651d, _SoftParticles_33d22c55e97e4bbf9658984efedb651d_OutVector1_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Multiply_603283fc9007453b86c0e29e22dc1fdf_Out_2_Float;
            Unity_Multiply_float_float(_Minimum_9603d95647104c398dcf64d0bf690e71_Out_2_Float, _SoftParticles_33d22c55e97e4bbf9658984efedb651d_OutVector1_1_Float, _Multiply_603283fc9007453b86c0e29e22dc1fdf_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            #if defined(BOOLEAN_EAB072D9159A48C685623C0E39EA73B0_ON)
            float _SoftParticles_e87b688bcc4a4c4387199157c1c53214_Out_0_Float = _Multiply_603283fc9007453b86c0e29e22dc1fdf_Out_2_Float;
            #else
            float _SoftParticles_e87b688bcc4a4c4387199157c1c53214_Out_0_Float = _Minimum_9603d95647104c398dcf64d0bf690e71_Out_2_Float;
            #endif
            #endif
            surface.Alpha = _SoftParticles_e87b688bcc4a4c4387199157c1c53214_Out_0_Float;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpaceNormal =                          input.normalOS;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpaceTangent =                         input.tangentOS.xyz;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpacePosition =                        input.positionOS;
        #endif
        
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.WorldSpacePosition = input.positionWS;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);
        #endif
        
        
            #if UNITY_UV_STARTS_AT_TOP
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
        #endif
        
            #else
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
        #endif
        
            #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.NDCPosition.y = 1.0f - output.NDCPosition.y;
        #endif
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.uv0 = input.texCoord0;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
        #endif
        
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "GBuffer"
            Tags
            {
                "LightMode" = "UniversalGBuffer"
            }
        
        // Render State
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ZTest LEqual
        ZWrite Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 4.5
        #pragma exclude_renderers gles gles3 glcore
        #pragma multi_compile_instancing
        #pragma multi_compile_fog
        #pragma instancing_options renderinglayer
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
        #pragma multi_compile _ LOD_FADE_CROSSFADE
        #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
        #pragma shader_feature_local _ BOOLEAN_EAB072D9159A48C685623C0E39EA73B0_ON
        #pragma shader_feature_local _ BOOLEAN_9A9F210EF4D44680B8F0CB7ACD99DFBB_ON
        #pragma multi_compile_local _ RECONSTRUCT
        
        #if defined(BOOLEAN_EAB072D9159A48C685623C0E39EA73B0_ON) && defined(BOOLEAN_9A9F210EF4D44680B8F0CB7ACD99DFBB_ON)
            #define KEYWORD_PERMUTATION_0
        #elif defined(BOOLEAN_EAB072D9159A48C685623C0E39EA73B0_ON)
            #define KEYWORD_PERMUTATION_1
        #elif defined(BOOLEAN_9A9F210EF4D44680B8F0CB7ACD99DFBB_ON)
            #define KEYWORD_PERMUTATION_2
        #else
            #define KEYWORD_PERMUTATION_3
        #endif
        
        
        // Defines
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_NORMAL
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_TANGENT
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_TEXCOORD0
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define VARYINGS_NEED_POSITION_WS
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define VARYINGS_NEED_NORMAL_WS
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define VARYINGS_NEED_TANGENT_WS
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define VARYINGS_NEED_TEXCOORD0
        #endif
        
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_GBUFFER
        #define _SURFACE_TYPE_TRANSPARENT 1
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define REQUIRE_DEPTH_TEXTURE
        #endif
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 positionOS : POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 normalOS : NORMAL;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 tangentOS : TANGENT;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 uv0 : TEXCOORD0;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
            #endif
        };
        struct Varyings
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 positionCS : SV_POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 positionWS;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 normalWS;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 tangentWS;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 texCoord0;
            #endif
            #if !defined(LIGHTMAP_ON)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 sh;
            #endif
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
            #endif
        };
        struct SurfaceDescriptionInputs
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 WorldSpaceNormal;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 WorldSpaceTangent;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 WorldSpaceBiTangent;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 WorldSpaceViewDirection;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpacePosition;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 WorldSpacePosition;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 ScreenPosition;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float2 NDCPosition;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float2 PixelPosition;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 uv0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 TimeParameters;
            #endif
        };
        struct VertexDescriptionInputs
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpaceNormal;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpaceTangent;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpacePosition;
            #endif
        };
        struct PackedVaryings
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 positionCS : SV_POSITION;
            #endif
            #if !defined(LIGHTMAP_ON)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 sh : INTERP0;
            #endif
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 tangentWS : INTERP1;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 texCoord0 : INTERP2;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 positionWS : INTERP3;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 normalWS : INTERP4;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
            #endif
        };
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            #if !defined(LIGHTMAP_ON)
            output.sh = input.sh;
            #endif
            output.tangentWS.xyzw = input.tangentWS;
            output.texCoord0.xyzw = input.texCoord0;
            output.positionWS.xyz = input.positionWS;
            output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            #if !defined(LIGHTMAP_ON)
            output.sh = input.sh;
            #endif
            output.tangentWS = input.tangentWS.xyzw;
            output.texCoord0 = input.texCoord0.xyzw;
            output.positionWS = input.positionWS.xyz;
            output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        #endif
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D_TexelSize;
        float4 Color_7eff2e7fc27440719d0a871a39dabdd3;
        float Vector1_2a375228e8d3437382814520b0cd8644;
        float Vector1_5f4b185241524942830132fc71554a33;
        float Vector1_2d1f70e8efb04843b1f8184ae06925e0;
        float Vector1_33982692;
        float Vector1_10DA9AB3;
        float2 Vector2_65050940ec244ae3bf42db206c2dccca;
        float4 Texture2D_4B382D9C_TexelSize;
        float4 Texture2D_3DC46A50_TexelSize;
        float4x4 _BA_SkyboxMatrix;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D);
        SAMPLER(sampler_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D);
        TEXTURE2D(Texture2D_4B382D9C);
        SAMPLER(samplerTexture2D_4B382D9C);
        TEXTURE2D(Texture2D_3DC46A50);
        SAMPLER(samplerTexture2D_3DC46A50);
        float _BA_SkyboxRatio;
        float4 _BA_CloudData;
        
        // Graph Includes
        #include "Assets/SYPackages/ShiyueFlexiblePipline/FlexiblePackages/3DSkybox/Shaders/UtilityGraphs/CustomLighting.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Normalize_float3(float3 In, out float3 Out)
        {
            Out = normalize(In);
        }
        
        void Unity_Negate_half3(half3 In, out half3 Out)
        {
            Out = -1 * In;
        }
        
        void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
        {
            Out = dot(A, B);
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
        {
            Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
        }
        
        void Unity_MatrixConstruction_Row_float (float4 M0, float4 M1, float4 M2, float4 M3, out float4x4 Out4x4, out float3x3 Out3x3, out float2x2 Out2x2)
        {
            Out4x4 = float4x4(M0.x, M0.y, M0.z, M0.w, M1.x, M1.y, M1.z, M1.w, M2.x, M2.y, M2.z, M2.w, M3.x, M3.y, M3.z, M3.w);
            Out3x3 = float3x3(M0.x, M0.y, M0.z, M1.x, M1.y, M1.z, M2.x, M2.y, M2.z);
            Out2x2 = float2x2(M0.x, M0.y, M1.x, M1.y);
        }
        
        void Unity_Branch_half3(half Predicate, half3 True, half3 False, out half3 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Branch_half(half Predicate, half True, half False, out half Out)
        {
            Out = Predicate ? True : False;
        }
        
        struct Bindings_GetMainLight_2ff6b2f9d6ff940dfa76d3c0d3f9c2ae_half
        {
        };
        
        void SG_GetMainLight_2ff6b2f9d6ff940dfa76d3c0d3f9c2ae_half(float3 PosisionWS_shadows, half Shadows, Bindings_GetMainLight_2ff6b2f9d6ff940dfa76d3c0d3f9c2ae_half IN, out half3 Direction_1, out half3 Color_2, out half Distance_Attenuation_3, out half Shadow_Attenuation_4)
        {
        half _Property_83b0ca4bb8be486292ac152fdc964bbb_Out_0_Boolean = Shadows;
        float3 _Property_b0870e822b434bb682becc8dba97af13_Out_0_Vector3 = PosisionWS_shadows;
        half3 _MainLightCustomFunction_e3f5c53147a845a4ae3955da5a4556a6_Direction_1_Vector3;
        half3 _MainLightCustomFunction_e3f5c53147a845a4ae3955da5a4556a6_Color_2_Vector3;
        half _MainLightCustomFunction_e3f5c53147a845a4ae3955da5a4556a6_DistanceAtten_3_Float;
        half _MainLightCustomFunction_e3f5c53147a845a4ae3955da5a4556a6_ShadowAtten_4_Float;
        MainLight_half(_Property_b0870e822b434bb682becc8dba97af13_Out_0_Vector3, _MainLightCustomFunction_e3f5c53147a845a4ae3955da5a4556a6_Direction_1_Vector3, _MainLightCustomFunction_e3f5c53147a845a4ae3955da5a4556a6_Color_2_Vector3, _MainLightCustomFunction_e3f5c53147a845a4ae3955da5a4556a6_DistanceAtten_3_Float, _MainLightCustomFunction_e3f5c53147a845a4ae3955da5a4556a6_ShadowAtten_4_Float);
        half3 _MainLightCustomFunction_7e2be557229449ba91e955c4804551f2_Direction_1_Vector3;
        half3 _MainLightCustomFunction_7e2be557229449ba91e955c4804551f2_Color_2_Vector3;
        half _MainLightCustomFunction_7e2be557229449ba91e955c4804551f2_DistanceAtten_3_Float;
        half _MainLightCustomFunction_7e2be557229449ba91e955c4804551f2_ShadowAtten_4_Float;
        MainLight_half(_MainLightCustomFunction_7e2be557229449ba91e955c4804551f2_Direction_1_Vector3, _MainLightCustomFunction_7e2be557229449ba91e955c4804551f2_Color_2_Vector3, _MainLightCustomFunction_7e2be557229449ba91e955c4804551f2_DistanceAtten_3_Float, _MainLightCustomFunction_7e2be557229449ba91e955c4804551f2_ShadowAtten_4_Float);
        half3 _Branch_207e005c167c40fdb9101a28fc4f95b8_Out_3_Vector3;
        Unity_Branch_half3(_Property_83b0ca4bb8be486292ac152fdc964bbb_Out_0_Boolean, _MainLightCustomFunction_e3f5c53147a845a4ae3955da5a4556a6_Direction_1_Vector3, _MainLightCustomFunction_7e2be557229449ba91e955c4804551f2_Direction_1_Vector3, _Branch_207e005c167c40fdb9101a28fc4f95b8_Out_3_Vector3);
        half3 _Branch_1a60dec68ac24bc9a0ed5e320aa55c21_Out_3_Vector3;
        Unity_Branch_half3(_Property_83b0ca4bb8be486292ac152fdc964bbb_Out_0_Boolean, _MainLightCustomFunction_e3f5c53147a845a4ae3955da5a4556a6_Color_2_Vector3, _MainLightCustomFunction_7e2be557229449ba91e955c4804551f2_Color_2_Vector3, _Branch_1a60dec68ac24bc9a0ed5e320aa55c21_Out_3_Vector3);
        half _Branch_3ac29ca8debd497b8c3657dd145fd1f5_Out_3_Float;
        Unity_Branch_half(_Property_83b0ca4bb8be486292ac152fdc964bbb_Out_0_Boolean, _MainLightCustomFunction_e3f5c53147a845a4ae3955da5a4556a6_DistanceAtten_3_Float, _MainLightCustomFunction_7e2be557229449ba91e955c4804551f2_DistanceAtten_3_Float, _Branch_3ac29ca8debd497b8c3657dd145fd1f5_Out_3_Float);
        half _Branch_c39962fa80b14a2c988108bebd64b8b5_Out_3_Float;
        Unity_Branch_half(_Property_83b0ca4bb8be486292ac152fdc964bbb_Out_0_Boolean, _MainLightCustomFunction_e3f5c53147a845a4ae3955da5a4556a6_ShadowAtten_4_Float, _MainLightCustomFunction_7e2be557229449ba91e955c4804551f2_ShadowAtten_4_Float, _Branch_c39962fa80b14a2c988108bebd64b8b5_Out_3_Float);
        Direction_1 = _Branch_207e005c167c40fdb9101a28fc4f95b8_Out_3_Vector3;
        Color_2 = _Branch_1a60dec68ac24bc9a0ed5e320aa55c21_Out_3_Vector3;
        Distance_Attenuation_3 = _Branch_3ac29ca8debd497b8c3657dd145fd1f5_Out_3_Float;
        Shadow_Attenuation_4 = _Branch_c39962fa80b14a2c988108bebd64b8b5_Out_3_Float;
        }
        
        void Unity_Multiply_float3x3_float3(float3x3 A, float3 B, out float3 Out)
        {
            Out = mul(A, B);
        }
        
        void Unity_Comparison_Greater_float(float A, float B, out float Out)
        {
            Out = A > B ? 1 : 0;
        }
        
        void Unity_Branch_float(float Predicate, float True, float False, out float Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
        Out = A * B;
        }
        
        void Unity_OneMinus_float(float In, out float Out)
        {
            Out = 1 - In;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
        Out = A * B;
        }
        
        struct Bindings_ComputeLightmap_e43f2878967dc15488276fdf96a2a634_float
        {
        };
        
        void SG_ComputeLightmap_e43f2878967dc15488276fdf96a2a634_float(float3 Vector3_9655A41F, float Vector1_f2d1bb31b65f491b983d70953e90dc1b, float Vector1_a7dc919dc3914d68820d1d4a7acd18d3, float Vector1_68860be670804442b8003a5507c34868, float Vector1_0f718eb5ca284ababccf9f0579ddb459, float Vector1_61c23c13d0084c0580a431ec04603dea, float Vector1_7b99b0043d4e49bc85916b8f52cf0731, Bindings_ComputeLightmap_e43f2878967dc15488276fdf96a2a634_float IN, out float Lightmap_0)
        {
        float3 _Property_ce5e9d55f39c454a8ab806bfc12c14aa_Out_0_Vector3 = Vector3_9655A41F;
        float _Split_e13693a230a34ab3986dad4b4569e534_R_1_Float = _Property_ce5e9d55f39c454a8ab806bfc12c14aa_Out_0_Vector3[0];
        float _Split_e13693a230a34ab3986dad4b4569e534_G_2_Float = _Property_ce5e9d55f39c454a8ab806bfc12c14aa_Out_0_Vector3[1];
        float _Split_e13693a230a34ab3986dad4b4569e534_B_3_Float = _Property_ce5e9d55f39c454a8ab806bfc12c14aa_Out_0_Vector3[2];
        float _Split_e13693a230a34ab3986dad4b4569e534_A_4_Float = 0;
        float _Comparison_1e5e00116d854b06af88c0fd460d1dc4_Out_2_Boolean;
        Unity_Comparison_Greater_float(_Split_e13693a230a34ab3986dad4b4569e534_R_1_Float, 0, _Comparison_1e5e00116d854b06af88c0fd460d1dc4_Out_2_Boolean);
        float _Property_2e9c288ce7e84f52b897fb7a72f43e78_Out_0_Float = Vector1_a7dc919dc3914d68820d1d4a7acd18d3;
        float _Property_3a182b1eb3724a8188d1afeb263574e9_Out_0_Float = Vector1_f2d1bb31b65f491b983d70953e90dc1b;
        float _Branch_aeed904fb23d46d9912defd4c7a4fb67_Out_3_Float;
        Unity_Branch_float(_Comparison_1e5e00116d854b06af88c0fd460d1dc4_Out_2_Boolean, _Property_2e9c288ce7e84f52b897fb7a72f43e78_Out_0_Float, _Property_3a182b1eb3724a8188d1afeb263574e9_Out_0_Float, _Branch_aeed904fb23d46d9912defd4c7a4fb67_Out_3_Float);
        float _Comparison_9cecd5f696d141079c57564c06d6f2f8_Out_2_Boolean;
        Unity_Comparison_Greater_float(_Split_e13693a230a34ab3986dad4b4569e534_G_2_Float, 0, _Comparison_9cecd5f696d141079c57564c06d6f2f8_Out_2_Boolean);
        float _Property_11c03cd032ff41be8984dcc68f22630d_Out_0_Float = Vector1_68860be670804442b8003a5507c34868;
        float _Property_5c949d5857b34549b8f81dd5f1aa2e2b_Out_0_Float = Vector1_0f718eb5ca284ababccf9f0579ddb459;
        float _Branch_86ad0a5a70284ed691378e8e2c4fbebe_Out_3_Float;
        Unity_Branch_float(_Comparison_9cecd5f696d141079c57564c06d6f2f8_Out_2_Boolean, _Property_11c03cd032ff41be8984dcc68f22630d_Out_0_Float, _Property_5c949d5857b34549b8f81dd5f1aa2e2b_Out_0_Float, _Branch_86ad0a5a70284ed691378e8e2c4fbebe_Out_3_Float);
        float _Comparison_033a823dcf5d4dfcb5b2de42cd5a6ef6_Out_2_Boolean;
        Unity_Comparison_Greater_float(_Split_e13693a230a34ab3986dad4b4569e534_B_3_Float, 0, _Comparison_033a823dcf5d4dfcb5b2de42cd5a6ef6_Out_2_Boolean);
        float _Property_d90c9e08c6d74912a268e5d56924fc12_Out_0_Float = Vector1_f2d1bb31b65f491b983d70953e90dc1b;
        float _Property_934c7456f2df4a62bb7afaaa61d427c0_Out_0_Float = Vector1_a7dc919dc3914d68820d1d4a7acd18d3;
        float _Add_25457a2c84844176be6346e3a7256033_Out_2_Float;
        Unity_Add_float(_Property_d90c9e08c6d74912a268e5d56924fc12_Out_0_Float, _Property_934c7456f2df4a62bb7afaaa61d427c0_Out_0_Float, _Add_25457a2c84844176be6346e3a7256033_Out_2_Float);
        float _Property_530c9025c6094d17a698f0dcc469ab09_Out_0_Float = Vector1_68860be670804442b8003a5507c34868;
        float _Property_b937847fb65d4877a697164e42edcab5_Out_0_Float = Vector1_0f718eb5ca284ababccf9f0579ddb459;
        float _Add_a204922169ba4fe1a355a57b6e17b8db_Out_2_Float;
        Unity_Add_float(_Property_530c9025c6094d17a698f0dcc469ab09_Out_0_Float, _Property_b937847fb65d4877a697164e42edcab5_Out_0_Float, _Add_a204922169ba4fe1a355a57b6e17b8db_Out_2_Float);
        float _Add_9e0c65317448445fbd34e99bfbcd9292_Out_2_Float;
        Unity_Add_float(_Add_25457a2c84844176be6346e3a7256033_Out_2_Float, _Add_a204922169ba4fe1a355a57b6e17b8db_Out_2_Float, _Add_9e0c65317448445fbd34e99bfbcd9292_Out_2_Float);
        float _Multiply_170935fccad0476a9cffa68f100474a7_Out_2_Float;
        Unity_Multiply_float_float(0.25, _Add_9e0c65317448445fbd34e99bfbcd9292_Out_2_Float, _Multiply_170935fccad0476a9cffa68f100474a7_Out_2_Float);
        float _Power_82b43d2499284a5584b1b3af44f4522b_Out_2_Float;
        Unity_Power_float(_Multiply_170935fccad0476a9cffa68f100474a7_Out_2_Float, 1, _Power_82b43d2499284a5584b1b3af44f4522b_Out_2_Float);
        float _Multiply_1d0ab76e4d9044e3a86b72a9530b3594_Out_2_Float;
        Unity_Multiply_float_float(_Power_82b43d2499284a5584b1b3af44f4522b_Out_2_Float, 1, _Multiply_1d0ab76e4d9044e3a86b72a9530b3594_Out_2_Float);
        float _Property_59a2a6186be74ebd8f14604da37a4e6a_Out_0_Float = Vector1_7b99b0043d4e49bc85916b8f52cf0731;
        #if defined(RECONSTRUCT)
        float _Reconstruct_ea4d77a8026046389ad824a57617aa6c_Out_0_Float = _Multiply_1d0ab76e4d9044e3a86b72a9530b3594_Out_2_Float;
        #else
        float _Reconstruct_ea4d77a8026046389ad824a57617aa6c_Out_0_Float = _Property_59a2a6186be74ebd8f14604da37a4e6a_Out_0_Float;
        #endif
        float _OneMinus_d9a8403f4fbb443587c8c8cf65e4386b_Out_1_Float;
        Unity_OneMinus_float(_Power_82b43d2499284a5584b1b3af44f4522b_Out_2_Float, _OneMinus_d9a8403f4fbb443587c8c8cf65e4386b_Out_1_Float);
        float _Property_e29a8bfbd8314e05a19b6dc2e83d80e4_Out_0_Float = Vector1_61c23c13d0084c0580a431ec04603dea;
        #if defined(RECONSTRUCT)
        float _Reconstruct_d303a71765764f86bafd424e496f53d9_Out_0_Float = _OneMinus_d9a8403f4fbb443587c8c8cf65e4386b_Out_1_Float;
        #else
        float _Reconstruct_d303a71765764f86bafd424e496f53d9_Out_0_Float = _Property_e29a8bfbd8314e05a19b6dc2e83d80e4_Out_0_Float;
        #endif
        float _Branch_84ce803260cc450a9e4b0b3233c63858_Out_3_Float;
        Unity_Branch_float(_Comparison_033a823dcf5d4dfcb5b2de42cd5a6ef6_Out_2_Boolean, _Reconstruct_ea4d77a8026046389ad824a57617aa6c_Out_0_Float, _Reconstruct_d303a71765764f86bafd424e496f53d9_Out_0_Float, _Branch_84ce803260cc450a9e4b0b3233c63858_Out_3_Float);
        float4 _Combine_3ef3559aca414f209f80d4fdeff0dc1f_RGBA_4_Vector4;
        float3 _Combine_3ef3559aca414f209f80d4fdeff0dc1f_RGB_5_Vector3;
        float2 _Combine_3ef3559aca414f209f80d4fdeff0dc1f_RG_6_Vector2;
        Unity_Combine_float(_Branch_aeed904fb23d46d9912defd4c7a4fb67_Out_3_Float, _Branch_86ad0a5a70284ed691378e8e2c4fbebe_Out_3_Float, _Branch_84ce803260cc450a9e4b0b3233c63858_Out_3_Float, 0, _Combine_3ef3559aca414f209f80d4fdeff0dc1f_RGBA_4_Vector4, _Combine_3ef3559aca414f209f80d4fdeff0dc1f_RGB_5_Vector3, _Combine_3ef3559aca414f209f80d4fdeff0dc1f_RG_6_Vector2);
        float3 _Property_1d2958867d61407db62ad0c7604522c3_Out_0_Vector3 = Vector3_9655A41F;
        float3 _Multiply_cee5f49181424d4289c6b0e8c7bb7eaa_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Property_1d2958867d61407db62ad0c7604522c3_Out_0_Vector3, _Property_1d2958867d61407db62ad0c7604522c3_Out_0_Vector3, _Multiply_cee5f49181424d4289c6b0e8c7bb7eaa_Out_2_Vector3);
        float3 _Multiply_097a099125e64ffdb11753f3740116b4_Out_2_Vector3;
        Unity_Multiply_float3_float3(_Combine_3ef3559aca414f209f80d4fdeff0dc1f_RGB_5_Vector3, _Multiply_cee5f49181424d4289c6b0e8c7bb7eaa_Out_2_Vector3, _Multiply_097a099125e64ffdb11753f3740116b4_Out_2_Vector3);
        float _Split_a6dc1283949b437c8da1331a910ab018_R_1_Float = _Multiply_097a099125e64ffdb11753f3740116b4_Out_2_Vector3[0];
        float _Split_a6dc1283949b437c8da1331a910ab018_G_2_Float = _Multiply_097a099125e64ffdb11753f3740116b4_Out_2_Vector3[1];
        float _Split_a6dc1283949b437c8da1331a910ab018_B_3_Float = _Multiply_097a099125e64ffdb11753f3740116b4_Out_2_Vector3[2];
        float _Split_a6dc1283949b437c8da1331a910ab018_A_4_Float = 0;
        float _Add_ce8a59efcd114a4f970997e85ddff9cc_Out_2_Float;
        Unity_Add_float(_Split_a6dc1283949b437c8da1331a910ab018_R_1_Float, _Split_a6dc1283949b437c8da1331a910ab018_G_2_Float, _Add_ce8a59efcd114a4f970997e85ddff9cc_Out_2_Float);
        float _Add_506ca34f7e3042c98424b6595837e0a6_Out_2_Float;
        Unity_Add_float(_Add_ce8a59efcd114a4f970997e85ddff9cc_Out_2_Float, _Split_a6dc1283949b437c8da1331a910ab018_B_3_Float, _Add_506ca34f7e3042c98424b6595837e0a6_Out_2_Float);
        Lightmap_0 = _Add_506ca34f7e3042c98424b6595837e0a6_Out_2_Float;
        }
        
        void Unity_Maximum_float(float A, float B, out float Out)
        {
            Out = max(A, B);
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        void Unity_Fog_float(out float4 Color, out float Density, float3 Position)
        {
            SHADERGRAPH_FOG(Position, Color, Density);
        }
        
        void Unity_Divide_float(float A, float B, out float Out)
        {
            Out = A / B;
        }
        
        void Unity_Exponential2_float(float In, out float Out)
        {
            Out = exp2(In);
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Blend_Screen_float(float Base, float Blend, out float Out, float Opacity)
        {
            Out = 1.0 - (1.0 - Blend) * (1.0 - Base);
            Out = lerp(Base, Out, Opacity);
        }
        
        struct Bindings_URPExponentialFog_a30e9a21a0b9141cbaaf335f79e3f00f_float
        {
        float3 ObjectSpacePosition;
        float3 WorldSpacePosition;
        };
        
        void SG_URPExponentialFog_a30e9a21a0b9141cbaaf335f79e3f00f_float(float Vector1_548c23b8a28a4acba12d5e42e8de4dd5, float Vector1_8a01e7e97fac46d3a084c24499c15b97, float4x4 Matrix4_a983a30e479e43a8b1f83b5f8372c5cd, float Vector1_8d1279a725f04004abeec8eb29628190, Bindings_URPExponentialFog_a30e9a21a0b9141cbaaf335f79e3f00f_float IN, out float3 Color_2, out float Density_1)
        {
        float4 _Fog_86d7de5382804677a9f8a6a7344cd56d_Color_0_Vector4;
        float _Fog_86d7de5382804677a9f8a6a7344cd56d_Density_1_Float;
        Unity_Fog_float(_Fog_86d7de5382804677a9f8a6a7344cd56d_Color_0_Vector4, _Fog_86d7de5382804677a9f8a6a7344cd56d_Density_1_Float, IN.ObjectSpacePosition);
        float _Property_2cee890886a049a8841a04b43b9c3c45_Out_0_Float = Vector1_8d1279a725f04004abeec8eb29628190;
        float _Divide_e16e72a863c843eb9cb0ac9e2124bb84_Out_2_Float;
        Unity_Divide_float(_Fog_86d7de5382804677a9f8a6a7344cd56d_Density_1_Float, _Property_2cee890886a049a8841a04b43b9c3c45_Out_0_Float, _Divide_e16e72a863c843eb9cb0ac9e2124bb84_Out_2_Float);
        float _Multiply_bc9c608a44464ec997b959515ad7a79b_Out_2_Float;
        Unity_Multiply_float_float(_Divide_e16e72a863c843eb9cb0ac9e2124bb84_Out_2_Float, _Divide_e16e72a863c843eb9cb0ac9e2124bb84_Out_2_Float, _Multiply_bc9c608a44464ec997b959515ad7a79b_Out_2_Float);
        float _OneMinus_c9dea61bb1d140a1bc8c5473a80c2d67_Out_1_Float;
        Unity_OneMinus_float(_Multiply_bc9c608a44464ec997b959515ad7a79b_Out_2_Float, _OneMinus_c9dea61bb1d140a1bc8c5473a80c2d67_Out_1_Float);
        float _Exponential_a3837813a78044a18b0bd627883f4d75_Out_1_Float;
        Unity_Exponential2_float(_OneMinus_c9dea61bb1d140a1bc8c5473a80c2d67_Out_1_Float, _Exponential_a3837813a78044a18b0bd627883f4d75_Out_1_Float);
        float _Split_93d0ab1c074f48ea9aa174cdff5e4b2b_R_1_Float = IN.WorldSpacePosition[0];
        float _Split_93d0ab1c074f48ea9aa174cdff5e4b2b_G_2_Float = IN.WorldSpacePosition[1];
        float _Split_93d0ab1c074f48ea9aa174cdff5e4b2b_B_3_Float = IN.WorldSpacePosition[2];
        float _Split_93d0ab1c074f48ea9aa174cdff5e4b2b_A_4_Float = 0;
        float4 _Combine_d3998e4489754cf5a0c127962cfc7786_RGBA_4_Vector4;
        float3 _Combine_d3998e4489754cf5a0c127962cfc7786_RGB_5_Vector3;
        float2 _Combine_d3998e4489754cf5a0c127962cfc7786_RG_6_Vector2;
        Unity_Combine_float(_Split_93d0ab1c074f48ea9aa174cdff5e4b2b_R_1_Float, _Split_93d0ab1c074f48ea9aa174cdff5e4b2b_G_2_Float, _Split_93d0ab1c074f48ea9aa174cdff5e4b2b_B_3_Float, 1, _Combine_d3998e4489754cf5a0c127962cfc7786_RGBA_4_Vector4, _Combine_d3998e4489754cf5a0c127962cfc7786_RGB_5_Vector3, _Combine_d3998e4489754cf5a0c127962cfc7786_RG_6_Vector2);
        float _Split_447a21f9b4ed4de289a42265b8854724_R_1_Float = _Combine_d3998e4489754cf5a0c127962cfc7786_RGBA_4_Vector4[0];
        float _Split_447a21f9b4ed4de289a42265b8854724_G_2_Float = _Combine_d3998e4489754cf5a0c127962cfc7786_RGBA_4_Vector4[1];
        float _Split_447a21f9b4ed4de289a42265b8854724_B_3_Float = _Combine_d3998e4489754cf5a0c127962cfc7786_RGBA_4_Vector4[2];
        float _Split_447a21f9b4ed4de289a42265b8854724_A_4_Float = _Combine_d3998e4489754cf5a0c127962cfc7786_RGBA_4_Vector4[3];
        float _Split_1acdb116d9fa41b8be55b3978ea78a48_R_1_Float = _WorldSpaceCameraPos[0];
        float _Split_1acdb116d9fa41b8be55b3978ea78a48_G_2_Float = _WorldSpaceCameraPos[1];
        float _Split_1acdb116d9fa41b8be55b3978ea78a48_B_3_Float = _WorldSpaceCameraPos[2];
        float _Split_1acdb116d9fa41b8be55b3978ea78a48_A_4_Float = 0;
        float _Property_e7bcdd9fa3e94e5d9b702f6e76c0d010_Out_0_Float = Vector1_8d1279a725f04004abeec8eb29628190;
        float _OneMinus_1a8c58b3e02947d5ad383443d81f8958_Out_1_Float;
        Unity_OneMinus_float(_Property_e7bcdd9fa3e94e5d9b702f6e76c0d010_Out_0_Float, _OneMinus_1a8c58b3e02947d5ad383443d81f8958_Out_1_Float);
        float _Multiply_b97098fcc3b7445aad787123ce7661eb_Out_2_Float;
        Unity_Multiply_float_float(_Split_1acdb116d9fa41b8be55b3978ea78a48_G_2_Float, _OneMinus_1a8c58b3e02947d5ad383443d81f8958_Out_1_Float, _Multiply_b97098fcc3b7445aad787123ce7661eb_Out_2_Float);
        float _Subtract_3b4ccdfd32ea4cb3ac423610dc19ec70_Out_2_Float;
        Unity_Subtract_float(_Split_447a21f9b4ed4de289a42265b8854724_G_2_Float, _Multiply_b97098fcc3b7445aad787123ce7661eb_Out_2_Float, _Subtract_3b4ccdfd32ea4cb3ac423610dc19ec70_Out_2_Float);
        float _Property_88a60358336e43d1b71d0e155f5a2c3f_Out_0_Float = Vector1_548c23b8a28a4acba12d5e42e8de4dd5;
        float _Property_da0c12e8a0534ab4a6afee1d15bb433e_Out_0_Float = Vector1_8d1279a725f04004abeec8eb29628190;
        float _Multiply_b91da133531c42a9b19158928c5930f1_Out_2_Float;
        Unity_Multiply_float_float(_Property_88a60358336e43d1b71d0e155f5a2c3f_Out_0_Float, _Property_da0c12e8a0534ab4a6afee1d15bb433e_Out_0_Float, _Multiply_b91da133531c42a9b19158928c5930f1_Out_2_Float);
        float _Subtract_eb72265b4d4a464b8a144339c6d13e5a_Out_2_Float;
        Unity_Subtract_float(_Subtract_3b4ccdfd32ea4cb3ac423610dc19ec70_Out_2_Float, _Multiply_b91da133531c42a9b19158928c5930f1_Out_2_Float, _Subtract_eb72265b4d4a464b8a144339c6d13e5a_Out_2_Float);
        float _Property_c7a5428b3f884bb798b63d2303a1a207_Out_0_Float = Vector1_8a01e7e97fac46d3a084c24499c15b97;
        float _Property_328059683b4144a399b419257f7df7e6_Out_0_Float = Vector1_8d1279a725f04004abeec8eb29628190;
        float _Multiply_2dfb6cd355d74d25adaa81134b3f0d46_Out_2_Float;
        Unity_Multiply_float_float(_Property_c7a5428b3f884bb798b63d2303a1a207_Out_0_Float, _Property_328059683b4144a399b419257f7df7e6_Out_0_Float, _Multiply_2dfb6cd355d74d25adaa81134b3f0d46_Out_2_Float);
        float _Maximum_7b5e96f356504e53985bfda714bf891f_Out_2_Float;
        Unity_Maximum_float(_Multiply_2dfb6cd355d74d25adaa81134b3f0d46_Out_2_Float, 0, _Maximum_7b5e96f356504e53985bfda714bf891f_Out_2_Float);
        float _Divide_c590caeb182647889ab25c0491be6524_Out_2_Float;
        Unity_Divide_float(1, _Maximum_7b5e96f356504e53985bfda714bf891f_Out_2_Float, _Divide_c590caeb182647889ab25c0491be6524_Out_2_Float);
        float _Multiply_c1516d20c0914e0e8ca560f3f5b4b066_Out_2_Float;
        Unity_Multiply_float_float(_Subtract_eb72265b4d4a464b8a144339c6d13e5a_Out_2_Float, _Divide_c590caeb182647889ab25c0491be6524_Out_2_Float, _Multiply_c1516d20c0914e0e8ca560f3f5b4b066_Out_2_Float);
        float _Saturate_be305bdfd34e40f28be591169dad704c_Out_1_Float;
        Unity_Saturate_float(_Multiply_c1516d20c0914e0e8ca560f3f5b4b066_Out_2_Float, _Saturate_be305bdfd34e40f28be591169dad704c_Out_1_Float);
        float _Blend_1d78d20d334a4cb1bef83f720c3abc74_Out_2_Float;
        Unity_Blend_Screen_float(_Exponential_a3837813a78044a18b0bd627883f4d75_Out_1_Float, _Saturate_be305bdfd34e40f28be591169dad704c_Out_1_Float, _Blend_1d78d20d334a4cb1bef83f720c3abc74_Out_2_Float, 1);
        Color_2 = (_Fog_86d7de5382804677a9f8a6a7344cd56d_Color_0_Vector4.xyz);
        Density_1 = _Blend_1d78d20d334a4cb1bef83f720c3abc74_Out_2_Float;
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        struct Bindings_MixFog_5251c54db4a6a478695a0390a4619eb9_float
        {
        float3 ObjectSpacePosition;
        float3 WorldSpacePosition;
        };
        
        void SG_MixFog_5251c54db4a6a478695a0390a4619eb9_float(float3 Vector3_f69a590ed6c84f36b0fee2268ca8fc03, float Vector1_dcd94c39cc78471aa6a3a1b27833bba1, float Vector1_65f4d0dc61ee403db0f9bfa041bed070, float4x4 Matrix4_5f3038a237414f29a1174d7598403333, float Vector1_eebb9e601111455e9605b88f3456f319, Bindings_MixFog_5251c54db4a6a478695a0390a4619eb9_float IN, out float3 Output_1)
        {
        float _Property_ee6090b3fec14429b97d420e65b6fa37_Out_0_Float = Vector1_65f4d0dc61ee403db0f9bfa041bed070;
        float _Property_bf717ffc5d574360858594b6ebce261d_Out_0_Float = Vector1_dcd94c39cc78471aa6a3a1b27833bba1;
        float4x4 _Property_bf1dffda22534014a73fd016c6ef049f_Out_0_Matrix4 = Matrix4_5f3038a237414f29a1174d7598403333;
        float _Property_d36d86c8e5ab4a86927c82837e601838_Out_0_Float = Vector1_eebb9e601111455e9605b88f3456f319;
        Bindings_URPExponentialFog_a30e9a21a0b9141cbaaf335f79e3f00f_float _URPExponentialFog_fc9b60a95ad444c88c6b1be317899593;
        _URPExponentialFog_fc9b60a95ad444c88c6b1be317899593.ObjectSpacePosition = IN.ObjectSpacePosition;
        _URPExponentialFog_fc9b60a95ad444c88c6b1be317899593.WorldSpacePosition = IN.WorldSpacePosition;
        float3 _URPExponentialFog_fc9b60a95ad444c88c6b1be317899593_Color_2_Vector3;
        float _URPExponentialFog_fc9b60a95ad444c88c6b1be317899593_Density_1_Float;
        SG_URPExponentialFog_a30e9a21a0b9141cbaaf335f79e3f00f_float(_Property_ee6090b3fec14429b97d420e65b6fa37_Out_0_Float, _Property_bf717ffc5d574360858594b6ebce261d_Out_0_Float, _Property_bf1dffda22534014a73fd016c6ef049f_Out_0_Matrix4, _Property_d36d86c8e5ab4a86927c82837e601838_Out_0_Float, _URPExponentialFog_fc9b60a95ad444c88c6b1be317899593, _URPExponentialFog_fc9b60a95ad444c88c6b1be317899593_Color_2_Vector3, _URPExponentialFog_fc9b60a95ad444c88c6b1be317899593_Density_1_Float);
        float3 _Property_7b5efc935cf04f3d8d6d30256e3c2385_Out_0_Vector3 = Vector3_f69a590ed6c84f36b0fee2268ca8fc03;
        float _Saturate_7aee755c2d2748979413e4d140fd121d_Out_1_Float;
        Unity_Saturate_float(_URPExponentialFog_fc9b60a95ad444c88c6b1be317899593_Density_1_Float, _Saturate_7aee755c2d2748979413e4d140fd121d_Out_1_Float);
        float3 _Lerp_bb4fea0611124207b10efb240fd9fd48_Out_3_Vector3;
        Unity_Lerp_float3(_URPExponentialFog_fc9b60a95ad444c88c6b1be317899593_Color_2_Vector3, _Property_7b5efc935cf04f3d8d6d30256e3c2385_Out_0_Vector3, (_Saturate_7aee755c2d2748979413e4d140fd121d_Out_1_Float.xxx), _Lerp_bb4fea0611124207b10efb240fd9fd48_Out_3_Vector3);
        Output_1 = _Lerp_bb4fea0611124207b10efb240fd9fd48_Out_3_Vector3;
        }
        
        void Unity_Length_float3(float3 In, out float Out)
        {
            Out = length(In);
        }
        
        void Unity_Branch_float2(float Predicate, float2 True, float2 False, out float2 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
        Out = A * B;
        }
        
        void Unity_Add_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A + B;
        }
        
        void Unity_Fraction_float2(float2 In, out float2 Out)
        {
            Out = frac(In);
        }
        
        void Unity_RandomRange_float(float2 Seed, float Min, float Max, out float Out)
        {
             float randomno =  frac(sin(dot(Seed, float2(12.9898, 78.233)))*43758.5453);
             Out = lerp(Min, Max, randomno);
        }
        
        void Unity_Floor_float2(float2 In, out float2 Out)
        {
            Out = floor(In);
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        struct Bindings_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float
        {
        float2 NDCPosition;
        float3 TimeParameters;
        };
        
        void SG_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float(float2 Vector2_caefb2173a93413babdc548e42b92647, float Boolean_d17c4aa779b140eb89358932ea6663e5, float Vector1_397d6d54d391448f841b568356acd9d5, Bindings_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float IN, out float Output_1, out float3 Normal_2)
        {
        float _Property_721476b2541f47d9a2a9269e3083e0d6_Out_0_Float = Vector1_397d6d54d391448f841b568356acd9d5;
        float _Multiply_7969691e0cc14256bab4e3e287352010_Out_2_Float;
        Unity_Multiply_float_float(_Property_721476b2541f47d9a2a9269e3083e0d6_Out_0_Float, 0.321, _Multiply_7969691e0cc14256bab4e3e287352010_Out_2_Float);
        float _Property_81032f19979848909a7ca400e4b48342_Out_0_Boolean = Boolean_d17c4aa779b140eb89358932ea6663e5;
        float2 _Vector2_fd3f4c200f834792b2a20662ee9e881a_Out_0_Vector2 = float2(IN.TimeParameters.x, IN.TimeParameters.x);
        float2 _Branch_0fc6b47a1c3b42de9b4e91943c99b893_Out_3_Vector2;
        Unity_Branch_float2(_Property_81032f19979848909a7ca400e4b48342_Out_0_Boolean, _Vector2_fd3f4c200f834792b2a20662ee9e881a_Out_0_Vector2, float2(0, 0), _Branch_0fc6b47a1c3b42de9b4e91943c99b893_Out_3_Vector2);
        float2 _Multiply_2a72b24c4beb4af4911a02dc52727555_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Branch_0fc6b47a1c3b42de9b4e91943c99b893_Out_3_Vector2, float2(0.018, 0.32), _Multiply_2a72b24c4beb4af4911a02dc52727555_Out_2_Vector2);
        float2 _Add_66ffbf97cca14199972fa0e0db881b16_Out_2_Vector2;
        Unity_Add_float2((_Multiply_7969691e0cc14256bab4e3e287352010_Out_2_Float.xx), _Multiply_2a72b24c4beb4af4911a02dc52727555_Out_2_Vector2, _Add_66ffbf97cca14199972fa0e0db881b16_Out_2_Vector2);
        float2 _Fraction_396a3fa0ff464affbc41bb38a673dcc2_Out_1_Vector2;
        Unity_Fraction_float2(_Add_66ffbf97cca14199972fa0e0db881b16_Out_2_Vector2, _Fraction_396a3fa0ff464affbc41bb38a673dcc2_Out_1_Vector2);
        float _Split_63ceb6dee6e54027b8f1467e4376cee2_R_1_Float = _Fraction_396a3fa0ff464affbc41bb38a673dcc2_Out_1_Vector2[0];
        float _Split_63ceb6dee6e54027b8f1467e4376cee2_G_2_Float = _Fraction_396a3fa0ff464affbc41bb38a673dcc2_Out_1_Vector2[1];
        float _Split_63ceb6dee6e54027b8f1467e4376cee2_B_3_Float = 0;
        float _Split_63ceb6dee6e54027b8f1467e4376cee2_A_4_Float = 0;
        float _RandomRange_484c84f4919c4cd8a517b36c15a038ea_Out_3_Float;
        Unity_RandomRange_float((_Split_63ceb6dee6e54027b8f1467e4376cee2_R_1_Float.xx), 0, _ScreenParams.x, _RandomRange_484c84f4919c4cd8a517b36c15a038ea_Out_3_Float);
        float _RandomRange_b63dcf8e65f740fab3cc51e40e1d5685_Out_3_Float;
        Unity_RandomRange_float((_Split_63ceb6dee6e54027b8f1467e4376cee2_G_2_Float.xx), 0, _ScreenParams.y, _RandomRange_b63dcf8e65f740fab3cc51e40e1d5685_Out_3_Float);
        float2 _Vector2_04d66487473642d79da224f4089554dd_Out_0_Vector2 = float2(_RandomRange_484c84f4919c4cd8a517b36c15a038ea_Out_3_Float, _RandomRange_b63dcf8e65f740fab3cc51e40e1d5685_Out_3_Float);
        float4 _ScreenPosition_a0254f37c23d45ffaf168c1d3439736b_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
        float4 _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RGBA_4_Vector4;
        float3 _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RGB_5_Vector3;
        float2 _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RG_6_Vector2;
        Unity_Combine_float(_ScreenParams.x, _ScreenParams.y, 0, 0, _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RGBA_4_Vector4, _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RGB_5_Vector3, _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RG_6_Vector2);
        float2 _Multiply_3e3b6c94f64b4ade9247e71c6d2f98d6_Out_2_Vector2;
        Unity_Multiply_float2_float2((_ScreenPosition_a0254f37c23d45ffaf168c1d3439736b_Out_0_Vector4.xy), _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RG_6_Vector2, _Multiply_3e3b6c94f64b4ade9247e71c6d2f98d6_Out_2_Vector2);
        float2 _Floor_4ba12f9eb3474734b0137ba643429b23_Out_1_Vector2;
        Unity_Floor_float2(_Multiply_3e3b6c94f64b4ade9247e71c6d2f98d6_Out_2_Vector2, _Floor_4ba12f9eb3474734b0137ba643429b23_Out_1_Vector2);
        float2 _Add_278d532c14154882bf1d47dde4d55ad3_Out_2_Vector2;
        Unity_Add_float2(_Vector2_04d66487473642d79da224f4089554dd_Out_0_Vector2, _Floor_4ba12f9eb3474734b0137ba643429b23_Out_1_Vector2, _Add_278d532c14154882bf1d47dde4d55ad3_Out_2_Vector2);
        float2 _Property_663e26b847554df8a237e6b5af6c8681_Out_0_Vector2 = Vector2_caefb2173a93413babdc548e42b92647;
        float2 _Divide_4576c5b640be4a1cba36f7b52c41b042_Out_2_Vector2;
        Unity_Divide_float2(_Add_278d532c14154882bf1d47dde4d55ad3_Out_2_Vector2, _Property_663e26b847554df8a237e6b5af6c8681_Out_0_Vector2, _Divide_4576c5b640be4a1cba36f7b52c41b042_Out_2_Vector2);
        float2 _Fraction_4942b1a15b0a4a019e485aa4d5bcb166_Out_1_Vector2;
        Unity_Fraction_float2(_Divide_4576c5b640be4a1cba36f7b52c41b042_Out_2_Vector2, _Fraction_4942b1a15b0a4a019e485aa4d5bcb166_Out_1_Vector2);
        float4 _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D).GetTransformedUV(_Fraction_4942b1a15b0a4a019e485aa4d5bcb166_Out_1_Vector2) );
        float _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_R_4_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4.r;
        float _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_G_5_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4.g;
        float _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_B_6_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4.b;
        float _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_A_7_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4.a;
        float _Split_95c0c3c67c5148a4945a0e0a8192275f_R_1_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4[0];
        float _Split_95c0c3c67c5148a4945a0e0a8192275f_G_2_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4[1];
        float _Split_95c0c3c67c5148a4945a0e0a8192275f_B_3_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4[2];
        float _Split_95c0c3c67c5148a4945a0e0a8192275f_A_4_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4[3];
        float3 _Vector3_54f2a95424284f65912dd46705fb9b33_Out_0_Vector3 = float3(_Split_95c0c3c67c5148a4945a0e0a8192275f_R_1_Float, _Split_95c0c3c67c5148a4945a0e0a8192275f_G_2_Float, _Split_95c0c3c67c5148a4945a0e0a8192275f_B_3_Float);
        float3 _Normalize_56d642ffdf80459aa5137d04c28ad137_Out_1_Vector3;
        Unity_Normalize_float3(_Vector3_54f2a95424284f65912dd46705fb9b33_Out_0_Vector3, _Normalize_56d642ffdf80459aa5137d04c28ad137_Out_1_Vector3);
        Output_1 = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_G_5_Float;
        Normal_2 = _Normalize_56d642ffdf80459aa5137d04c28ad137_Out_1_Vector3;
        }
        
        void Unity_Preview_float(float In, out float Out)
        {
            Out = In;
        }
        
        void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
        {
            Out = smoothstep(Edge1, Edge2, In);
        }
        
        void Unity_Minimum_float(float A, float B, out float Out)
        {
            Out = min(A, B);
        };
        
        void Unity_SceneDepth_Linear01_float(float4 UV, out float Out)
        {
            Out = Linear01Depth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
        }
        
        struct Bindings_SoftParticles_18333bb3c326140abbd08c558de3bf83_float
        {
        float4 ScreenPosition;
        float2 NDCPosition;
        };
        
        void SG_SoftParticles_18333bb3c326140abbd08c558de3bf83_float(float Vector1_b12c77296e1146fa9f4d315fc85d04ab, float Vector1_0414f85da44b41c9983c5044458d5297, float Vector1_0695185d2cd141a68dcd8c1a2daa3c40, Bindings_SoftParticles_18333bb3c326140abbd08c558de3bf83_float IN, out float OutVector1_1)
        {
        float _SceneDepth_276a1c9e05034cdfa6529a8931fb79f7_Out_1_Float;
        Unity_SceneDepth_Linear01_float(float4(IN.NDCPosition.xy, 0, 0), _SceneDepth_276a1c9e05034cdfa6529a8931fb79f7_Out_1_Float);
        float _Multiply_6ce0bce77db34631b737de6ad4fa0e04_Out_2_Float;
        Unity_Multiply_float_float(_SceneDepth_276a1c9e05034cdfa6529a8931fb79f7_Out_1_Float, _ProjectionParams.z, _Multiply_6ce0bce77db34631b737de6ad4fa0e04_Out_2_Float);
        float _Property_6a749dd773734fab83f490ee0fb3c7f0_Out_0_Float = Vector1_0695185d2cd141a68dcd8c1a2daa3c40;
        float4 _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4 = IN.ScreenPosition;
        float _Split_f05c829a2fa34687a4c46a8484cc4eab_R_1_Float = _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4[0];
        float _Split_f05c829a2fa34687a4c46a8484cc4eab_G_2_Float = _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4[1];
        float _Split_f05c829a2fa34687a4c46a8484cc4eab_B_3_Float = _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4[2];
        float _Split_f05c829a2fa34687a4c46a8484cc4eab_A_4_Float = _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4[3];
        float _Add_39d32d1e2d4e46efa943593378a7b5d4_Out_2_Float;
        Unity_Add_float(_Property_6a749dd773734fab83f490ee0fb3c7f0_Out_0_Float, _Split_f05c829a2fa34687a4c46a8484cc4eab_A_4_Float, _Add_39d32d1e2d4e46efa943593378a7b5d4_Out_2_Float);
        float _Subtract_0db08fa14d48402dbe0edd1a2df1d281_Out_2_Float;
        Unity_Subtract_float(_Multiply_6ce0bce77db34631b737de6ad4fa0e04_Out_2_Float, _Add_39d32d1e2d4e46efa943593378a7b5d4_Out_2_Float, _Subtract_0db08fa14d48402dbe0edd1a2df1d281_Out_2_Float);
        float _Property_a1e60e28f8174d88ba77f34de3664a02_Out_0_Float = Vector1_b12c77296e1146fa9f4d315fc85d04ab;
        float _Property_72d03f4fff3e4c5f8607d2320b27c4d0_Out_0_Float = Vector1_0414f85da44b41c9983c5044458d5297;
        float2 _Vector2_d5e84c16e02d464b97da10162141cf76_Out_0_Vector2 = float2(_Property_a1e60e28f8174d88ba77f34de3664a02_Out_0_Float, _Property_72d03f4fff3e4c5f8607d2320b27c4d0_Out_0_Float);
        float _Remap_41d3d1abc133496d946826964dcf67e5_Out_3_Float;
        Unity_Remap_float(_Subtract_0db08fa14d48402dbe0edd1a2df1d281_Out_2_Float, _Vector2_d5e84c16e02d464b97da10162141cf76_Out_0_Vector2, float2 (0, 1), _Remap_41d3d1abc133496d946826964dcf67e5_Out_3_Float);
        float _Saturate_6dc9e8b8bd6a4471aba1733b5fa31a0d_Out_1_Float;
        Unity_Saturate_float(_Remap_41d3d1abc133496d946826964dcf67e5_Out_3_Float, _Saturate_6dc9e8b8bd6a4471aba1733b5fa31a0d_Out_1_Float);
        OutVector1_1 = _Saturate_6dc9e8b8bd6a4471aba1733b5fa31a0d_Out_1_Float;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float Alpha;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _Property_c2828c2b3b9343e9b9f6966966dcdbe0_Out_0_Vector4 = Color_7eff2e7fc27440719d0a871a39dabdd3;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            half3 _MainLightCustomFunction_8870fa9bc91542d0a2b3b835afc4d160_Direction_1_Vector3;
            half3 _MainLightCustomFunction_8870fa9bc91542d0a2b3b835afc4d160_Color_2_Vector3;
            half _MainLightCustomFunction_8870fa9bc91542d0a2b3b835afc4d160_DistanceAtten_3_Float;
            half _MainLightCustomFunction_8870fa9bc91542d0a2b3b835afc4d160_ShadowAtten_4_Float;
            MainLight_half(_MainLightCustomFunction_8870fa9bc91542d0a2b3b835afc4d160_Direction_1_Vector3, _MainLightCustomFunction_8870fa9bc91542d0a2b3b835afc4d160_Color_2_Vector3, _MainLightCustomFunction_8870fa9bc91542d0a2b3b835afc4d160_DistanceAtten_3_Float, _MainLightCustomFunction_8870fa9bc91542d0a2b3b835afc4d160_ShadowAtten_4_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float3 _Normalize_6b38c5a3cffb4dca97a262f7e17e7a9a_Out_1_Vector3;
            Unity_Normalize_float3(IN.WorldSpaceViewDirection, _Normalize_6b38c5a3cffb4dca97a262f7e17e7a9a_Out_1_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            half3 _Negate_1b6b777fc47d4e3999403cd69f9a1edd_Out_1_Vector3;
            Unity_Negate_half3(_MainLightCustomFunction_8870fa9bc91542d0a2b3b835afc4d160_Direction_1_Vector3, _Negate_1b6b777fc47d4e3999403cd69f9a1edd_Out_1_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _DotProduct_68ed5c9b09234ffdb3de22d96beb0782_Out_2_Float;
            Unity_DotProduct_float3(_Normalize_6b38c5a3cffb4dca97a262f7e17e7a9a_Out_1_Vector3, _Negate_1b6b777fc47d4e3999403cd69f9a1edd_Out_1_Vector3, _DotProduct_68ed5c9b09234ffdb3de22d96beb0782_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Saturate_c06a279c60524824ae0e4c72d7397e9a_Out_1_Float;
            Unity_Saturate_float(_DotProduct_68ed5c9b09234ffdb3de22d96beb0782_Out_2_Float, _Saturate_c06a279c60524824ae0e4c72d7397e9a_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Power_6aa1633458314359af0d2028500ae133_Out_2_Float;
            Unity_Power_float(_Saturate_c06a279c60524824ae0e4c72d7397e9a_Out_1_Float, 150, _Power_6aa1633458314359af0d2028500ae133_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Remap_d2403b2388124cad8739c0807bf74a2d_Out_3_Float;
            Unity_Remap_float(_Power_6aa1633458314359af0d2028500ae133_Out_2_Float, float2 (0, 1), float2 (1, 2), _Remap_d2403b2388124cad8739c0807bf74a2d_Out_3_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4x4 _MatrixConstruction_d937186230a944779e67bd584476956e_var4x4_4_Matrix4;
            float3x3 _MatrixConstruction_d937186230a944779e67bd584476956e_var3x3_5_Matrix3;
            float2x2 _MatrixConstruction_d937186230a944779e67bd584476956e_var2x2_6_Matrix2;
            Unity_MatrixConstruction_Row_float((float4(IN.WorldSpaceTangent, 1.0)), (float4(IN.WorldSpaceBiTangent, 1.0)), (float4(IN.WorldSpaceNormal, 1.0)), float4 (0, 0, 0, 0), _MatrixConstruction_d937186230a944779e67bd584476956e_var4x4_4_Matrix4, _MatrixConstruction_d937186230a944779e67bd584476956e_var3x3_5_Matrix3, _MatrixConstruction_d937186230a944779e67bd584476956e_var2x2_6_Matrix2);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            Bindings_GetMainLight_2ff6b2f9d6ff940dfa76d3c0d3f9c2ae_half _GetMainLight_1ece9407584e4056b9346816ccf0ea19;
            half3 _GetMainLight_1ece9407584e4056b9346816ccf0ea19_Direction_1_Vector3;
            half3 _GetMainLight_1ece9407584e4056b9346816ccf0ea19_Color_2_Vector3;
            half _GetMainLight_1ece9407584e4056b9346816ccf0ea19_DistanceAttenuation_3_Float;
            half _GetMainLight_1ece9407584e4056b9346816ccf0ea19_ShadowAttenuation_4_Float;
            SG_GetMainLight_2ff6b2f9d6ff940dfa76d3c0d3f9c2ae_half(float3 (0, 0, 0), 0, _GetMainLight_1ece9407584e4056b9346816ccf0ea19, _GetMainLight_1ece9407584e4056b9346816ccf0ea19_Direction_1_Vector3, _GetMainLight_1ece9407584e4056b9346816ccf0ea19_Color_2_Vector3, _GetMainLight_1ece9407584e4056b9346816ccf0ea19_DistanceAttenuation_3_Float, _GetMainLight_1ece9407584e4056b9346816ccf0ea19_ShadowAttenuation_4_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float3 _Multiply_4ecad25e3a0346509b51bd1e9195f870_Out_2_Vector3;
            Unity_Multiply_float3x3_float3(_MatrixConstruction_d937186230a944779e67bd584476956e_var3x3_5_Matrix3, _GetMainLight_1ece9407584e4056b9346816ccf0ea19_Direction_1_Vector3, _Multiply_4ecad25e3a0346509b51bd1e9195f870_Out_2_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float3 _Normalize_8684a681837240098a2a06a04687e43c_Out_1_Vector3;
            Unity_Normalize_float3(_Multiply_4ecad25e3a0346509b51bd1e9195f870_Out_2_Vector3, _Normalize_8684a681837240098a2a06a04687e43c_Out_1_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            UnityTexture2D _Property_a94933ad99104a7e829e26333c3da8ab_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(Texture2D_4B382D9C);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_a94933ad99104a7e829e26333c3da8ab_Out_0_Texture2D.tex, _Property_a94933ad99104a7e829e26333c3da8ab_Out_0_Texture2D.samplerstate, _Property_a94933ad99104a7e829e26333c3da8ab_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_R_4_Float = _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_RGBA_0_Vector4.r;
            float _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_G_5_Float = _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_RGBA_0_Vector4.g;
            float _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_B_6_Float = _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_RGBA_0_Vector4.b;
            float _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_A_7_Float = _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_RGBA_0_Vector4.a;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            UnityTexture2D _Property_06ae7b2647934302a2bc77f5a16cfac2_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(Texture2D_3DC46A50);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_06ae7b2647934302a2bc77f5a16cfac2_Out_0_Texture2D.tex, _Property_06ae7b2647934302a2bc77f5a16cfac2_Out_0_Texture2D.samplerstate, _Property_06ae7b2647934302a2bc77f5a16cfac2_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_R_4_Float = _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4.r;
            float _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_G_5_Float = _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4.g;
            float _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_B_6_Float = _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4.b;
            float _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_A_7_Float = _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4.a;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            Bindings_ComputeLightmap_e43f2878967dc15488276fdf96a2a634_float _ComputeLightmap_089b9b63089148c684b0d015a451e22e;
            float _ComputeLightmap_089b9b63089148c684b0d015a451e22e_Lightmap_0_Float;
            SG_ComputeLightmap_e43f2878967dc15488276fdf96a2a634_float(_Normalize_8684a681837240098a2a06a04687e43c_Out_1_Vector3, _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_R_4_Float, _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_B_6_Float, _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_G_5_Float, _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_A_7_Float, _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_R_4_Float, _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_G_5_Float, _ComputeLightmap_089b9b63089148c684b0d015a451e22e, _ComputeLightmap_089b9b63089148c684b0d015a451e22e_Lightmap_0_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_71376bfef41e4c60b2854418f222f581_Out_0_Float = Vector1_2a375228e8d3437382814520b0cd8644;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float Slider_0778109a2833445aa2b5c95029b54a3a = 0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Remap_2877d604dc3f4d819395fecb16454d7e_Out_3_Float;
            Unity_Remap_float(Slider_0778109a2833445aa2b5c95029b54a3a, float2 (0, 1), float2 (0.5, 1), _Remap_2877d604dc3f4d819395fecb16454d7e_Out_3_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Multiply_ce1a0a31791c4653a930ba16a5fa049a_Out_2_Float;
            Unity_Multiply_float_float(_Property_71376bfef41e4c60b2854418f222f581_Out_0_Float, _Remap_2877d604dc3f4d819395fecb16454d7e_Out_3_Float, _Multiply_ce1a0a31791c4653a930ba16a5fa049a_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Maximum_fa17ffed7e7645d5813075804f1deda3_Out_2_Float;
            Unity_Maximum_float(_Multiply_ce1a0a31791c4653a930ba16a5fa049a_Out_2_Float, 0.001, _Maximum_fa17ffed7e7645d5813075804f1deda3_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Power_385054020c344800ba53e4a12bd4d51d_Out_2_Float;
            Unity_Power_float(_ComputeLightmap_089b9b63089148c684b0d015a451e22e_Lightmap_0_Float, _Maximum_fa17ffed7e7645d5813075804f1deda3_Out_2_Float, _Power_385054020c344800ba53e4a12bd4d51d_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Multiply_7c8fc9c61f1c472ab4e2e46f549f9456_Out_2_Float;
            Unity_Multiply_float_float(_Remap_d2403b2388124cad8739c0807bf74a2d_Out_3_Float, _Power_385054020c344800ba53e4a12bd4d51d_Out_2_Float, _Multiply_7c8fc9c61f1c472ab4e2e46f549f9456_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float3 _Multiply_8ba7011ffa514036a1cc3f66c3c861ff_Out_2_Vector3;
            Unity_Multiply_float3_float3(_MainLightCustomFunction_8870fa9bc91542d0a2b3b835afc4d160_Color_2_Vector3, (_Multiply_7c8fc9c61f1c472ab4e2e46f549f9456_Out_2_Float.xxx), _Multiply_8ba7011ffa514036a1cc3f66c3c861ff_Out_2_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float3 _Transform_7b3f1c3a74c4464b8bbfbe86674e2d74_Out_1_Vector3;
            {
                float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
                _Transform_7b3f1c3a74c4464b8bbfbe86674e2d74_Out_1_Vector3 = TransformWorldToTangent(float3 (0, 1, 0).xyz, tangentTransform, true);
            }
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            Bindings_ComputeLightmap_e43f2878967dc15488276fdf96a2a634_float _ComputeLightmap_153ec09869964725a3e39aa055c13ab4;
            float _ComputeLightmap_153ec09869964725a3e39aa055c13ab4_Lightmap_0_Float;
            SG_ComputeLightmap_e43f2878967dc15488276fdf96a2a634_float(_Transform_7b3f1c3a74c4464b8bbfbe86674e2d74_Out_1_Vector3, _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_R_4_Float, _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_B_6_Float, _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_G_5_Float, _SampleTexture2D_bc45df87dece4bc28b3b42b7800dd6e0_A_7_Float, _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_R_4_Float, _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_G_5_Float, _ComputeLightmap_153ec09869964725a3e39aa055c13ab4, _ComputeLightmap_153ec09869964725a3e39aa055c13ab4_Lightmap_0_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float3 _Vector3_0634952406fb4234b0c76357309bdb8d_Out_0_Vector3 = float3(1, _ComputeLightmap_153ec09869964725a3e39aa055c13ab4_Lightmap_0_Float, 1);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float3 _Normalize_b5cfd2e7a337486f80594836f10e5b5a_Out_1_Vector3;
            Unity_Normalize_float3(_Vector3_0634952406fb4234b0c76357309bdb8d_Out_0_Vector3, _Normalize_b5cfd2e7a337486f80594836f10e5b5a_Out_1_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float3 _SampleSHCustomFunction_0f11693f40e84149be7b8b8b11e19aad_Ambient_1_Vector3;
            SampleSH_float(_Normalize_b5cfd2e7a337486f80594836f10e5b5a_Out_1_Vector3, _SampleSHCustomFunction_0f11693f40e84149be7b8b8b11e19aad_Ambient_1_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_2e8862f5767141c387da6757663a9bf8_Out_0_Float = Vector1_5f4b185241524942830132fc71554a33;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float3 _Multiply_9e12d4562d2f483b89d99177abfa48b3_Out_2_Vector3;
            Unity_Multiply_float3_float3(_SampleSHCustomFunction_0f11693f40e84149be7b8b8b11e19aad_Ambient_1_Vector3, (_Property_2e8862f5767141c387da6757663a9bf8_Out_0_Float.xxx), _Multiply_9e12d4562d2f483b89d99177abfa48b3_Out_2_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float3 _Add_d027fefb26c6403ab640ba13310ba3b0_Out_2_Vector3;
            Unity_Add_float3(_Multiply_8ba7011ffa514036a1cc3f66c3c861ff_Out_2_Vector3, _Multiply_9e12d4562d2f483b89d99177abfa48b3_Out_2_Vector3, _Add_d027fefb26c6403ab640ba13310ba3b0_Out_2_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float3 _Multiply_d0c8a59906c34fa993df3216c4ed5b37_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_c2828c2b3b9343e9b9f6966966dcdbe0_Out_0_Vector4.xyz), _Add_d027fefb26c6403ab640ba13310ba3b0_Out_2_Vector3, _Multiply_d0c8a59906c34fa993df3216c4ed5b37_Out_2_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_40abfff51702420992215a6b0160134d_Out_0_Float = Vector1_33982692;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_f8c400911f384e54a29a04532285877f_Out_0_Float = Vector1_10DA9AB3;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4x4 _Property_40a3954879c843e0a13d646287556ad7_Out_0_Matrix4 = _BA_SkyboxMatrix;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_583b965013e541d9a6b3923989e8fcc5_Out_0_Float = _BA_SkyboxRatio;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            Bindings_MixFog_5251c54db4a6a478695a0390a4619eb9_float _MixFog_3c1fce250a8a4fb2be1e03a174a7c9c4;
            _MixFog_3c1fce250a8a4fb2be1e03a174a7c9c4.ObjectSpacePosition = IN.ObjectSpacePosition;
            _MixFog_3c1fce250a8a4fb2be1e03a174a7c9c4.WorldSpacePosition = IN.WorldSpacePosition;
            float3 _MixFog_3c1fce250a8a4fb2be1e03a174a7c9c4_Output_1_Vector3;
            SG_MixFog_5251c54db4a6a478695a0390a4619eb9_float(_Multiply_d0c8a59906c34fa993df3216c4ed5b37_Out_2_Vector3, _Property_40abfff51702420992215a6b0160134d_Out_0_Float, _Property_f8c400911f384e54a29a04532285877f_Out_0_Float, _Property_40a3954879c843e0a13d646287556ad7_Out_0_Matrix4, _Property_583b965013e541d9a6b3923989e8fcc5_Out_0_Float, _MixFog_3c1fce250a8a4fb2be1e03a174a7c9c4, _MixFog_3c1fce250a8a4fb2be1e03a174a7c9c4_Output_1_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _OneMinus_a90ee5ec5cfd410780133317beeee956_Out_1_Float;
            Unity_OneMinus_float(_SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_A_7_Float, _OneMinus_a90ee5ec5cfd410780133317beeee956_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_fbf5240d6a974443afb86e71299332dc_Out_0_Float = Vector1_2a375228e8d3437382814520b0cd8644;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Power_4d5b0fe5fe894f37bf2bf51adfe6403f_Out_2_Float;
            Unity_Power_float(_OneMinus_a90ee5ec5cfd410780133317beeee956_Out_1_Float, _Property_fbf5240d6a974443afb86e71299332dc_Out_0_Float, _Power_4d5b0fe5fe894f37bf2bf51adfe6403f_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _OneMinus_12776925fcba4b8f8c431735c34d4ef0_Out_1_Float;
            Unity_OneMinus_float(_Power_4d5b0fe5fe894f37bf2bf51adfe6403f_Out_2_Float, _OneMinus_12776925fcba4b8f8c431735c34d4ef0_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Length_e04efe434e9a4ecc998e1dd3c3d5e90e_Out_1_Float;
            Unity_Length_float3(SHADERGRAPH_OBJECT_POSITION, _Length_e04efe434e9a4ecc998e1dd3c3d5e90e_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            Bindings_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679;
            _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679.NDCPosition = IN.NDCPosition;
            _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679.TimeParameters = IN.TimeParameters;
            float _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Output_1_Float;
            float3 _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Normal_2_Vector3;
            SG_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float(float2 (256, 256), 1, _Length_e04efe434e9a4ecc998e1dd3c3d5e90e_Out_1_Float, _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679, _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Output_1_Float, _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Normal_2_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Remap_bbf4dcb1a53440238a83cbaea629f9a2_Out_3_Float;
            Unity_Remap_float(_SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Output_1_Float, float2 (0, 1), float2 (0.8, 1.1), _Remap_bbf4dcb1a53440238a83cbaea629f9a2_Out_3_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4 = _BA_CloudData;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Split_3bcb0174a9954d879a71e473885b9e1b_R_1_Float = _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4[0];
            float _Split_3bcb0174a9954d879a71e473885b9e1b_G_2_Float = _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4[1];
            float _Split_3bcb0174a9954d879a71e473885b9e1b_B_3_Float = _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4[2];
            float _Split_3bcb0174a9954d879a71e473885b9e1b_A_4_Float = _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4[3];
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Multiply_211f37f5b555434d956f42443155bc4a_Out_2_Float;
            Unity_Multiply_float_float(_Remap_bbf4dcb1a53440238a83cbaea629f9a2_Out_3_Float, _Split_3bcb0174a9954d879a71e473885b9e1b_A_4_Float, _Multiply_211f37f5b555434d956f42443155bc4a_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4 = _BA_CloudData;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_R_1_Float = _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4[0];
            float _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_G_2_Float = _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4[1];
            float _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_B_3_Float = _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4[2];
            float _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_A_4_Float = _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4[3];
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            #if defined(BOOLEAN_9A9F210EF4D44680B8F0CB7ACD99DFBB_ON)
            float _BlueNoise_b07e75159dec4ad49e7f81e3592b900e_Out_0_Float = _Multiply_211f37f5b555434d956f42443155bc4a_Out_2_Float;
            #else
            float _BlueNoise_b07e75159dec4ad49e7f81e3592b900e_Out_0_Float = _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_A_4_Float;
            #endif
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _OneMinus_21be261ae35940709d0db1914ee9855c_Out_1_Float;
            Unity_OneMinus_float(_BlueNoise_b07e75159dec4ad49e7f81e3592b900e_Out_0_Float, _OneMinus_21be261ae35940709d0db1914ee9855c_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Add_2f041a64479546ff91a26ed1dbaffeee_Out_2_Float;
            Unity_Add_float(_OneMinus_21be261ae35940709d0db1914ee9855c_Out_1_Float, 0.1, _Add_2f041a64479546ff91a26ed1dbaffeee_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Preview_d4d26446fc904b76afe70165bc2d684f_Out_1_Float;
            Unity_Preview_float(_SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_B_6_Float, _Preview_d4d26446fc904b76afe70165bc2d684f_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Smoothstep_3d813f1fadfa4b9b8587408892645438_Out_3_Float;
            Unity_Smoothstep_float(_OneMinus_21be261ae35940709d0db1914ee9855c_Out_1_Float, _Add_2f041a64479546ff91a26ed1dbaffeee_Out_2_Float, _Preview_d4d26446fc904b76afe70165bc2d684f_Out_1_Float, _Smoothstep_3d813f1fadfa4b9b8587408892645438_Out_3_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Minimum_9603d95647104c398dcf64d0bf690e71_Out_2_Float;
            Unity_Minimum_float(_OneMinus_12776925fcba4b8f8c431735c34d4ef0_Out_1_Float, _Smoothstep_3d813f1fadfa4b9b8587408892645438_Out_3_Float, _Minimum_9603d95647104c398dcf64d0bf690e71_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float2 _Property_4d3f52434f0e4f098cc877f3f4dadbf9_Out_0_Vector2 = Vector2_65050940ec244ae3bf42db206c2dccca;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Split_8604fb34fc04432c845c2474347347cc_R_1_Float = _Property_4d3f52434f0e4f098cc877f3f4dadbf9_Out_0_Vector2[0];
            float _Split_8604fb34fc04432c845c2474347347cc_G_2_Float = _Property_4d3f52434f0e4f098cc877f3f4dadbf9_Out_0_Vector2[1];
            float _Split_8604fb34fc04432c845c2474347347cc_B_3_Float = 0;
            float _Split_8604fb34fc04432c845c2474347347cc_A_4_Float = 0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _OneMinus_2253c4aebc784af4b7bfe91e392d3d1c_Out_1_Float;
            Unity_OneMinus_float(_Preview_d4d26446fc904b76afe70165bc2d684f_Out_1_Float, _OneMinus_2253c4aebc784af4b7bfe91e392d3d1c_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_a7bc7d6aca7a4351a54c042414c8dca3_Out_0_Float = Vector1_2d1f70e8efb04843b1f8184ae06925e0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Multiply_521b54ef01d8415cacbbab05d9ef91b4_Out_2_Float;
            Unity_Multiply_float_float(_OneMinus_2253c4aebc784af4b7bfe91e392d3d1c_Out_1_Float, _Property_a7bc7d6aca7a4351a54c042414c8dca3_Out_0_Float, _Multiply_521b54ef01d8415cacbbab05d9ef91b4_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            Bindings_SoftParticles_18333bb3c326140abbd08c558de3bf83_float _SoftParticles_33d22c55e97e4bbf9658984efedb651d;
            _SoftParticles_33d22c55e97e4bbf9658984efedb651d.ScreenPosition = IN.ScreenPosition;
            _SoftParticles_33d22c55e97e4bbf9658984efedb651d.NDCPosition = IN.NDCPosition;
            float _SoftParticles_33d22c55e97e4bbf9658984efedb651d_OutVector1_1_Float;
            SG_SoftParticles_18333bb3c326140abbd08c558de3bf83_float(_Split_8604fb34fc04432c845c2474347347cc_R_1_Float, _Split_8604fb34fc04432c845c2474347347cc_G_2_Float, _Multiply_521b54ef01d8415cacbbab05d9ef91b4_Out_2_Float, _SoftParticles_33d22c55e97e4bbf9658984efedb651d, _SoftParticles_33d22c55e97e4bbf9658984efedb651d_OutVector1_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Multiply_603283fc9007453b86c0e29e22dc1fdf_Out_2_Float;
            Unity_Multiply_float_float(_Minimum_9603d95647104c398dcf64d0bf690e71_Out_2_Float, _SoftParticles_33d22c55e97e4bbf9658984efedb651d_OutVector1_1_Float, _Multiply_603283fc9007453b86c0e29e22dc1fdf_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            #if defined(BOOLEAN_EAB072D9159A48C685623C0E39EA73B0_ON)
            float _SoftParticles_e87b688bcc4a4c4387199157c1c53214_Out_0_Float = _Multiply_603283fc9007453b86c0e29e22dc1fdf_Out_2_Float;
            #else
            float _SoftParticles_e87b688bcc4a4c4387199157c1c53214_Out_0_Float = _Minimum_9603d95647104c398dcf64d0bf690e71_Out_2_Float;
            #endif
            #endif
            surface.BaseColor = _MixFog_3c1fce250a8a4fb2be1e03a174a7c9c4_Output_1_Vector3;
            surface.Alpha = _SoftParticles_e87b688bcc4a4c4387199157c1c53214_Out_0_Float;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpaceNormal =                          input.normalOS;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpaceTangent =                         input.tangentOS.xyz;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpacePosition =                        input.positionOS;
        #endif
        
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        float3 unnormalizedNormalWS = input.normalWS;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        const float renormFactor = 1.0 / length(unnormalizedNormalWS);
        #endif
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        // use bitangent on the fly like in hdrp
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0)* GetOddNegativeScale();
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        float3 bitang = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);
        #endif
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
        #endif
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        // to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        // This                is explained in section 2.2 in "surface gradient based bump mapping framework"
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.WorldSpaceBiTangent = renormFactor * bitang;
        #endif
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.WorldSpacePosition = input.positionWS;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpacePosition = TransformWorldToObject(input.positionWS);
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);
        #endif
        
        
            #if UNITY_UV_STARTS_AT_TOP
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
        #endif
        
            #else
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
        #endif
        
            #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.NDCPosition.y = 1.0f - output.NDCPosition.y;
        #endif
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.uv0 = input.texCoord0;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
        #endif
        
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitGBufferPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "SceneSelectionPass"
            Tags
            {
                "LightMode" = "SceneSelectionPass"
            }
        
        // Render State
        Cull Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        // PassKeywords: <None>
        #pragma shader_feature_local _ BOOLEAN_EAB072D9159A48C685623C0E39EA73B0_ON
        #pragma shader_feature_local _ BOOLEAN_9A9F210EF4D44680B8F0CB7ACD99DFBB_ON
        
        #if defined(BOOLEAN_EAB072D9159A48C685623C0E39EA73B0_ON) && defined(BOOLEAN_9A9F210EF4D44680B8F0CB7ACD99DFBB_ON)
            #define KEYWORD_PERMUTATION_0
        #elif defined(BOOLEAN_EAB072D9159A48C685623C0E39EA73B0_ON)
            #define KEYWORD_PERMUTATION_1
        #elif defined(BOOLEAN_9A9F210EF4D44680B8F0CB7ACD99DFBB_ON)
            #define KEYWORD_PERMUTATION_2
        #else
            #define KEYWORD_PERMUTATION_3
        #endif
        
        
        // Defines
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_NORMAL
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_TANGENT
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_TEXCOORD0
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define VARYINGS_NEED_POSITION_WS
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define VARYINGS_NEED_TEXCOORD0
        #endif
        
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        #define SCENESELECTIONPASS 1
        #define ALPHA_CLIP_THRESHOLD 1
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define REQUIRE_DEPTH_TEXTURE
        #endif
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 positionOS : POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 normalOS : NORMAL;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 tangentOS : TANGENT;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 uv0 : TEXCOORD0;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
            #endif
        };
        struct Varyings
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 positionCS : SV_POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 positionWS;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 texCoord0;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
            #endif
        };
        struct SurfaceDescriptionInputs
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 WorldSpacePosition;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 ScreenPosition;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float2 NDCPosition;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float2 PixelPosition;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 uv0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 TimeParameters;
            #endif
        };
        struct VertexDescriptionInputs
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpaceNormal;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpaceTangent;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpacePosition;
            #endif
        };
        struct PackedVaryings
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 positionCS : SV_POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 texCoord0 : INTERP0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 positionWS : INTERP1;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
            #endif
        };
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.texCoord0.xyzw = input.texCoord0;
            output.positionWS.xyz = input.positionWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.texCoord0.xyzw;
            output.positionWS = input.positionWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        #endif
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D_TexelSize;
        float4 Color_7eff2e7fc27440719d0a871a39dabdd3;
        float Vector1_2a375228e8d3437382814520b0cd8644;
        float Vector1_5f4b185241524942830132fc71554a33;
        float Vector1_2d1f70e8efb04843b1f8184ae06925e0;
        float Vector1_33982692;
        float Vector1_10DA9AB3;
        float2 Vector2_65050940ec244ae3bf42db206c2dccca;
        float4 Texture2D_4B382D9C_TexelSize;
        float4 Texture2D_3DC46A50_TexelSize;
        float4x4 _BA_SkyboxMatrix;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D);
        SAMPLER(sampler_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D);
        TEXTURE2D(Texture2D_4B382D9C);
        SAMPLER(samplerTexture2D_4B382D9C);
        TEXTURE2D(Texture2D_3DC46A50);
        SAMPLER(samplerTexture2D_3DC46A50);
        float _BA_SkyboxRatio;
        float4 _BA_CloudData;
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_OneMinus_float(float In, out float Out)
        {
            Out = 1 - In;
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Length_float3(float3 In, out float Out)
        {
            Out = length(In);
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
        Out = A * B;
        }
        
        void Unity_Branch_float2(float Predicate, float2 True, float2 False, out float2 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
        Out = A * B;
        }
        
        void Unity_Add_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A + B;
        }
        
        void Unity_Fraction_float2(float2 In, out float2 Out)
        {
            Out = frac(In);
        }
        
        void Unity_RandomRange_float(float2 Seed, float Min, float Max, out float Out)
        {
             float randomno =  frac(sin(dot(Seed, float2(12.9898, 78.233)))*43758.5453);
             Out = lerp(Min, Max, randomno);
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Floor_float2(float2 In, out float2 Out)
        {
            Out = floor(In);
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        void Unity_Normalize_float3(float3 In, out float3 Out)
        {
            Out = normalize(In);
        }
        
        struct Bindings_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float
        {
        float2 NDCPosition;
        float3 TimeParameters;
        };
        
        void SG_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float(float2 Vector2_caefb2173a93413babdc548e42b92647, float Boolean_d17c4aa779b140eb89358932ea6663e5, float Vector1_397d6d54d391448f841b568356acd9d5, Bindings_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float IN, out float Output_1, out float3 Normal_2)
        {
        float _Property_721476b2541f47d9a2a9269e3083e0d6_Out_0_Float = Vector1_397d6d54d391448f841b568356acd9d5;
        float _Multiply_7969691e0cc14256bab4e3e287352010_Out_2_Float;
        Unity_Multiply_float_float(_Property_721476b2541f47d9a2a9269e3083e0d6_Out_0_Float, 0.321, _Multiply_7969691e0cc14256bab4e3e287352010_Out_2_Float);
        float _Property_81032f19979848909a7ca400e4b48342_Out_0_Boolean = Boolean_d17c4aa779b140eb89358932ea6663e5;
        float2 _Vector2_fd3f4c200f834792b2a20662ee9e881a_Out_0_Vector2 = float2(IN.TimeParameters.x, IN.TimeParameters.x);
        float2 _Branch_0fc6b47a1c3b42de9b4e91943c99b893_Out_3_Vector2;
        Unity_Branch_float2(_Property_81032f19979848909a7ca400e4b48342_Out_0_Boolean, _Vector2_fd3f4c200f834792b2a20662ee9e881a_Out_0_Vector2, float2(0, 0), _Branch_0fc6b47a1c3b42de9b4e91943c99b893_Out_3_Vector2);
        float2 _Multiply_2a72b24c4beb4af4911a02dc52727555_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Branch_0fc6b47a1c3b42de9b4e91943c99b893_Out_3_Vector2, float2(0.018, 0.32), _Multiply_2a72b24c4beb4af4911a02dc52727555_Out_2_Vector2);
        float2 _Add_66ffbf97cca14199972fa0e0db881b16_Out_2_Vector2;
        Unity_Add_float2((_Multiply_7969691e0cc14256bab4e3e287352010_Out_2_Float.xx), _Multiply_2a72b24c4beb4af4911a02dc52727555_Out_2_Vector2, _Add_66ffbf97cca14199972fa0e0db881b16_Out_2_Vector2);
        float2 _Fraction_396a3fa0ff464affbc41bb38a673dcc2_Out_1_Vector2;
        Unity_Fraction_float2(_Add_66ffbf97cca14199972fa0e0db881b16_Out_2_Vector2, _Fraction_396a3fa0ff464affbc41bb38a673dcc2_Out_1_Vector2);
        float _Split_63ceb6dee6e54027b8f1467e4376cee2_R_1_Float = _Fraction_396a3fa0ff464affbc41bb38a673dcc2_Out_1_Vector2[0];
        float _Split_63ceb6dee6e54027b8f1467e4376cee2_G_2_Float = _Fraction_396a3fa0ff464affbc41bb38a673dcc2_Out_1_Vector2[1];
        float _Split_63ceb6dee6e54027b8f1467e4376cee2_B_3_Float = 0;
        float _Split_63ceb6dee6e54027b8f1467e4376cee2_A_4_Float = 0;
        float _RandomRange_484c84f4919c4cd8a517b36c15a038ea_Out_3_Float;
        Unity_RandomRange_float((_Split_63ceb6dee6e54027b8f1467e4376cee2_R_1_Float.xx), 0, _ScreenParams.x, _RandomRange_484c84f4919c4cd8a517b36c15a038ea_Out_3_Float);
        float _RandomRange_b63dcf8e65f740fab3cc51e40e1d5685_Out_3_Float;
        Unity_RandomRange_float((_Split_63ceb6dee6e54027b8f1467e4376cee2_G_2_Float.xx), 0, _ScreenParams.y, _RandomRange_b63dcf8e65f740fab3cc51e40e1d5685_Out_3_Float);
        float2 _Vector2_04d66487473642d79da224f4089554dd_Out_0_Vector2 = float2(_RandomRange_484c84f4919c4cd8a517b36c15a038ea_Out_3_Float, _RandomRange_b63dcf8e65f740fab3cc51e40e1d5685_Out_3_Float);
        float4 _ScreenPosition_a0254f37c23d45ffaf168c1d3439736b_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
        float4 _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RGBA_4_Vector4;
        float3 _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RGB_5_Vector3;
        float2 _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RG_6_Vector2;
        Unity_Combine_float(_ScreenParams.x, _ScreenParams.y, 0, 0, _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RGBA_4_Vector4, _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RGB_5_Vector3, _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RG_6_Vector2);
        float2 _Multiply_3e3b6c94f64b4ade9247e71c6d2f98d6_Out_2_Vector2;
        Unity_Multiply_float2_float2((_ScreenPosition_a0254f37c23d45ffaf168c1d3439736b_Out_0_Vector4.xy), _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RG_6_Vector2, _Multiply_3e3b6c94f64b4ade9247e71c6d2f98d6_Out_2_Vector2);
        float2 _Floor_4ba12f9eb3474734b0137ba643429b23_Out_1_Vector2;
        Unity_Floor_float2(_Multiply_3e3b6c94f64b4ade9247e71c6d2f98d6_Out_2_Vector2, _Floor_4ba12f9eb3474734b0137ba643429b23_Out_1_Vector2);
        float2 _Add_278d532c14154882bf1d47dde4d55ad3_Out_2_Vector2;
        Unity_Add_float2(_Vector2_04d66487473642d79da224f4089554dd_Out_0_Vector2, _Floor_4ba12f9eb3474734b0137ba643429b23_Out_1_Vector2, _Add_278d532c14154882bf1d47dde4d55ad3_Out_2_Vector2);
        float2 _Property_663e26b847554df8a237e6b5af6c8681_Out_0_Vector2 = Vector2_caefb2173a93413babdc548e42b92647;
        float2 _Divide_4576c5b640be4a1cba36f7b52c41b042_Out_2_Vector2;
        Unity_Divide_float2(_Add_278d532c14154882bf1d47dde4d55ad3_Out_2_Vector2, _Property_663e26b847554df8a237e6b5af6c8681_Out_0_Vector2, _Divide_4576c5b640be4a1cba36f7b52c41b042_Out_2_Vector2);
        float2 _Fraction_4942b1a15b0a4a019e485aa4d5bcb166_Out_1_Vector2;
        Unity_Fraction_float2(_Divide_4576c5b640be4a1cba36f7b52c41b042_Out_2_Vector2, _Fraction_4942b1a15b0a4a019e485aa4d5bcb166_Out_1_Vector2);
        float4 _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D).GetTransformedUV(_Fraction_4942b1a15b0a4a019e485aa4d5bcb166_Out_1_Vector2) );
        float _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_R_4_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4.r;
        float _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_G_5_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4.g;
        float _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_B_6_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4.b;
        float _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_A_7_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4.a;
        float _Split_95c0c3c67c5148a4945a0e0a8192275f_R_1_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4[0];
        float _Split_95c0c3c67c5148a4945a0e0a8192275f_G_2_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4[1];
        float _Split_95c0c3c67c5148a4945a0e0a8192275f_B_3_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4[2];
        float _Split_95c0c3c67c5148a4945a0e0a8192275f_A_4_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4[3];
        float3 _Vector3_54f2a95424284f65912dd46705fb9b33_Out_0_Vector3 = float3(_Split_95c0c3c67c5148a4945a0e0a8192275f_R_1_Float, _Split_95c0c3c67c5148a4945a0e0a8192275f_G_2_Float, _Split_95c0c3c67c5148a4945a0e0a8192275f_B_3_Float);
        float3 _Normalize_56d642ffdf80459aa5137d04c28ad137_Out_1_Vector3;
        Unity_Normalize_float3(_Vector3_54f2a95424284f65912dd46705fb9b33_Out_0_Vector3, _Normalize_56d642ffdf80459aa5137d04c28ad137_Out_1_Vector3);
        Output_1 = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_G_5_Float;
        Normal_2 = _Normalize_56d642ffdf80459aa5137d04c28ad137_Out_1_Vector3;
        }
        
        void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
        {
            Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Preview_float(float In, out float Out)
        {
            Out = In;
        }
        
        void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
        {
            Out = smoothstep(Edge1, Edge2, In);
        }
        
        void Unity_Minimum_float(float A, float B, out float Out)
        {
            Out = min(A, B);
        };
        
        void Unity_SceneDepth_Linear01_float(float4 UV, out float Out)
        {
            Out = Linear01Depth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        struct Bindings_SoftParticles_18333bb3c326140abbd08c558de3bf83_float
        {
        float4 ScreenPosition;
        float2 NDCPosition;
        };
        
        void SG_SoftParticles_18333bb3c326140abbd08c558de3bf83_float(float Vector1_b12c77296e1146fa9f4d315fc85d04ab, float Vector1_0414f85da44b41c9983c5044458d5297, float Vector1_0695185d2cd141a68dcd8c1a2daa3c40, Bindings_SoftParticles_18333bb3c326140abbd08c558de3bf83_float IN, out float OutVector1_1)
        {
        float _SceneDepth_276a1c9e05034cdfa6529a8931fb79f7_Out_1_Float;
        Unity_SceneDepth_Linear01_float(float4(IN.NDCPosition.xy, 0, 0), _SceneDepth_276a1c9e05034cdfa6529a8931fb79f7_Out_1_Float);
        float _Multiply_6ce0bce77db34631b737de6ad4fa0e04_Out_2_Float;
        Unity_Multiply_float_float(_SceneDepth_276a1c9e05034cdfa6529a8931fb79f7_Out_1_Float, _ProjectionParams.z, _Multiply_6ce0bce77db34631b737de6ad4fa0e04_Out_2_Float);
        float _Property_6a749dd773734fab83f490ee0fb3c7f0_Out_0_Float = Vector1_0695185d2cd141a68dcd8c1a2daa3c40;
        float4 _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4 = IN.ScreenPosition;
        float _Split_f05c829a2fa34687a4c46a8484cc4eab_R_1_Float = _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4[0];
        float _Split_f05c829a2fa34687a4c46a8484cc4eab_G_2_Float = _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4[1];
        float _Split_f05c829a2fa34687a4c46a8484cc4eab_B_3_Float = _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4[2];
        float _Split_f05c829a2fa34687a4c46a8484cc4eab_A_4_Float = _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4[3];
        float _Add_39d32d1e2d4e46efa943593378a7b5d4_Out_2_Float;
        Unity_Add_float(_Property_6a749dd773734fab83f490ee0fb3c7f0_Out_0_Float, _Split_f05c829a2fa34687a4c46a8484cc4eab_A_4_Float, _Add_39d32d1e2d4e46efa943593378a7b5d4_Out_2_Float);
        float _Subtract_0db08fa14d48402dbe0edd1a2df1d281_Out_2_Float;
        Unity_Subtract_float(_Multiply_6ce0bce77db34631b737de6ad4fa0e04_Out_2_Float, _Add_39d32d1e2d4e46efa943593378a7b5d4_Out_2_Float, _Subtract_0db08fa14d48402dbe0edd1a2df1d281_Out_2_Float);
        float _Property_a1e60e28f8174d88ba77f34de3664a02_Out_0_Float = Vector1_b12c77296e1146fa9f4d315fc85d04ab;
        float _Property_72d03f4fff3e4c5f8607d2320b27c4d0_Out_0_Float = Vector1_0414f85da44b41c9983c5044458d5297;
        float2 _Vector2_d5e84c16e02d464b97da10162141cf76_Out_0_Vector2 = float2(_Property_a1e60e28f8174d88ba77f34de3664a02_Out_0_Float, _Property_72d03f4fff3e4c5f8607d2320b27c4d0_Out_0_Float);
        float _Remap_41d3d1abc133496d946826964dcf67e5_Out_3_Float;
        Unity_Remap_float(_Subtract_0db08fa14d48402dbe0edd1a2df1d281_Out_2_Float, _Vector2_d5e84c16e02d464b97da10162141cf76_Out_0_Vector2, float2 (0, 1), _Remap_41d3d1abc133496d946826964dcf67e5_Out_3_Float);
        float _Saturate_6dc9e8b8bd6a4471aba1733b5fa31a0d_Out_1_Float;
        Unity_Saturate_float(_Remap_41d3d1abc133496d946826964dcf67e5_Out_3_Float, _Saturate_6dc9e8b8bd6a4471aba1733b5fa31a0d_Out_1_Float);
        OutVector1_1 = _Saturate_6dc9e8b8bd6a4471aba1733b5fa31a0d_Out_1_Float;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            UnityTexture2D _Property_06ae7b2647934302a2bc77f5a16cfac2_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(Texture2D_3DC46A50);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_06ae7b2647934302a2bc77f5a16cfac2_Out_0_Texture2D.tex, _Property_06ae7b2647934302a2bc77f5a16cfac2_Out_0_Texture2D.samplerstate, _Property_06ae7b2647934302a2bc77f5a16cfac2_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_R_4_Float = _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4.r;
            float _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_G_5_Float = _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4.g;
            float _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_B_6_Float = _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4.b;
            float _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_A_7_Float = _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4.a;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _OneMinus_a90ee5ec5cfd410780133317beeee956_Out_1_Float;
            Unity_OneMinus_float(_SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_A_7_Float, _OneMinus_a90ee5ec5cfd410780133317beeee956_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_fbf5240d6a974443afb86e71299332dc_Out_0_Float = Vector1_2a375228e8d3437382814520b0cd8644;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Power_4d5b0fe5fe894f37bf2bf51adfe6403f_Out_2_Float;
            Unity_Power_float(_OneMinus_a90ee5ec5cfd410780133317beeee956_Out_1_Float, _Property_fbf5240d6a974443afb86e71299332dc_Out_0_Float, _Power_4d5b0fe5fe894f37bf2bf51adfe6403f_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _OneMinus_12776925fcba4b8f8c431735c34d4ef0_Out_1_Float;
            Unity_OneMinus_float(_Power_4d5b0fe5fe894f37bf2bf51adfe6403f_Out_2_Float, _OneMinus_12776925fcba4b8f8c431735c34d4ef0_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Length_e04efe434e9a4ecc998e1dd3c3d5e90e_Out_1_Float;
            Unity_Length_float3(SHADERGRAPH_OBJECT_POSITION, _Length_e04efe434e9a4ecc998e1dd3c3d5e90e_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            Bindings_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679;
            _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679.NDCPosition = IN.NDCPosition;
            _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679.TimeParameters = IN.TimeParameters;
            float _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Output_1_Float;
            float3 _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Normal_2_Vector3;
            SG_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float(float2 (256, 256), 1, _Length_e04efe434e9a4ecc998e1dd3c3d5e90e_Out_1_Float, _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679, _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Output_1_Float, _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Normal_2_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Remap_bbf4dcb1a53440238a83cbaea629f9a2_Out_3_Float;
            Unity_Remap_float(_SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Output_1_Float, float2 (0, 1), float2 (0.8, 1.1), _Remap_bbf4dcb1a53440238a83cbaea629f9a2_Out_3_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4 = _BA_CloudData;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Split_3bcb0174a9954d879a71e473885b9e1b_R_1_Float = _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4[0];
            float _Split_3bcb0174a9954d879a71e473885b9e1b_G_2_Float = _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4[1];
            float _Split_3bcb0174a9954d879a71e473885b9e1b_B_3_Float = _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4[2];
            float _Split_3bcb0174a9954d879a71e473885b9e1b_A_4_Float = _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4[3];
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Multiply_211f37f5b555434d956f42443155bc4a_Out_2_Float;
            Unity_Multiply_float_float(_Remap_bbf4dcb1a53440238a83cbaea629f9a2_Out_3_Float, _Split_3bcb0174a9954d879a71e473885b9e1b_A_4_Float, _Multiply_211f37f5b555434d956f42443155bc4a_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4 = _BA_CloudData;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_R_1_Float = _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4[0];
            float _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_G_2_Float = _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4[1];
            float _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_B_3_Float = _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4[2];
            float _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_A_4_Float = _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4[3];
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            #if defined(BOOLEAN_9A9F210EF4D44680B8F0CB7ACD99DFBB_ON)
            float _BlueNoise_b07e75159dec4ad49e7f81e3592b900e_Out_0_Float = _Multiply_211f37f5b555434d956f42443155bc4a_Out_2_Float;
            #else
            float _BlueNoise_b07e75159dec4ad49e7f81e3592b900e_Out_0_Float = _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_A_4_Float;
            #endif
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _OneMinus_21be261ae35940709d0db1914ee9855c_Out_1_Float;
            Unity_OneMinus_float(_BlueNoise_b07e75159dec4ad49e7f81e3592b900e_Out_0_Float, _OneMinus_21be261ae35940709d0db1914ee9855c_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Add_2f041a64479546ff91a26ed1dbaffeee_Out_2_Float;
            Unity_Add_float(_OneMinus_21be261ae35940709d0db1914ee9855c_Out_1_Float, 0.1, _Add_2f041a64479546ff91a26ed1dbaffeee_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Preview_d4d26446fc904b76afe70165bc2d684f_Out_1_Float;
            Unity_Preview_float(_SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_B_6_Float, _Preview_d4d26446fc904b76afe70165bc2d684f_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Smoothstep_3d813f1fadfa4b9b8587408892645438_Out_3_Float;
            Unity_Smoothstep_float(_OneMinus_21be261ae35940709d0db1914ee9855c_Out_1_Float, _Add_2f041a64479546ff91a26ed1dbaffeee_Out_2_Float, _Preview_d4d26446fc904b76afe70165bc2d684f_Out_1_Float, _Smoothstep_3d813f1fadfa4b9b8587408892645438_Out_3_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Minimum_9603d95647104c398dcf64d0bf690e71_Out_2_Float;
            Unity_Minimum_float(_OneMinus_12776925fcba4b8f8c431735c34d4ef0_Out_1_Float, _Smoothstep_3d813f1fadfa4b9b8587408892645438_Out_3_Float, _Minimum_9603d95647104c398dcf64d0bf690e71_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float2 _Property_4d3f52434f0e4f098cc877f3f4dadbf9_Out_0_Vector2 = Vector2_65050940ec244ae3bf42db206c2dccca;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Split_8604fb34fc04432c845c2474347347cc_R_1_Float = _Property_4d3f52434f0e4f098cc877f3f4dadbf9_Out_0_Vector2[0];
            float _Split_8604fb34fc04432c845c2474347347cc_G_2_Float = _Property_4d3f52434f0e4f098cc877f3f4dadbf9_Out_0_Vector2[1];
            float _Split_8604fb34fc04432c845c2474347347cc_B_3_Float = 0;
            float _Split_8604fb34fc04432c845c2474347347cc_A_4_Float = 0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _OneMinus_2253c4aebc784af4b7bfe91e392d3d1c_Out_1_Float;
            Unity_OneMinus_float(_Preview_d4d26446fc904b76afe70165bc2d684f_Out_1_Float, _OneMinus_2253c4aebc784af4b7bfe91e392d3d1c_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_a7bc7d6aca7a4351a54c042414c8dca3_Out_0_Float = Vector1_2d1f70e8efb04843b1f8184ae06925e0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Multiply_521b54ef01d8415cacbbab05d9ef91b4_Out_2_Float;
            Unity_Multiply_float_float(_OneMinus_2253c4aebc784af4b7bfe91e392d3d1c_Out_1_Float, _Property_a7bc7d6aca7a4351a54c042414c8dca3_Out_0_Float, _Multiply_521b54ef01d8415cacbbab05d9ef91b4_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            Bindings_SoftParticles_18333bb3c326140abbd08c558de3bf83_float _SoftParticles_33d22c55e97e4bbf9658984efedb651d;
            _SoftParticles_33d22c55e97e4bbf9658984efedb651d.ScreenPosition = IN.ScreenPosition;
            _SoftParticles_33d22c55e97e4bbf9658984efedb651d.NDCPosition = IN.NDCPosition;
            float _SoftParticles_33d22c55e97e4bbf9658984efedb651d_OutVector1_1_Float;
            SG_SoftParticles_18333bb3c326140abbd08c558de3bf83_float(_Split_8604fb34fc04432c845c2474347347cc_R_1_Float, _Split_8604fb34fc04432c845c2474347347cc_G_2_Float, _Multiply_521b54ef01d8415cacbbab05d9ef91b4_Out_2_Float, _SoftParticles_33d22c55e97e4bbf9658984efedb651d, _SoftParticles_33d22c55e97e4bbf9658984efedb651d_OutVector1_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Multiply_603283fc9007453b86c0e29e22dc1fdf_Out_2_Float;
            Unity_Multiply_float_float(_Minimum_9603d95647104c398dcf64d0bf690e71_Out_2_Float, _SoftParticles_33d22c55e97e4bbf9658984efedb651d_OutVector1_1_Float, _Multiply_603283fc9007453b86c0e29e22dc1fdf_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            #if defined(BOOLEAN_EAB072D9159A48C685623C0E39EA73B0_ON)
            float _SoftParticles_e87b688bcc4a4c4387199157c1c53214_Out_0_Float = _Multiply_603283fc9007453b86c0e29e22dc1fdf_Out_2_Float;
            #else
            float _SoftParticles_e87b688bcc4a4c4387199157c1c53214_Out_0_Float = _Minimum_9603d95647104c398dcf64d0bf690e71_Out_2_Float;
            #endif
            #endif
            surface.Alpha = _SoftParticles_e87b688bcc4a4c4387199157c1c53214_Out_0_Float;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpaceNormal =                          input.normalOS;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpaceTangent =                         input.tangentOS.xyz;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpacePosition =                        input.positionOS;
        #endif
        
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.WorldSpacePosition = input.positionWS;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);
        #endif
        
        
            #if UNITY_UV_STARTS_AT_TOP
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
        #endif
        
            #else
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
        #endif
        
            #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.NDCPosition.y = 1.0f - output.NDCPosition.y;
        #endif
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.uv0 = input.texCoord0;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
        #endif
        
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "ScenePickingPass"
            Tags
            {
                "LightMode" = "Picking"
            }
        
        // Render State
        Cull Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        // PassKeywords: <None>
        #pragma shader_feature_local _ BOOLEAN_EAB072D9159A48C685623C0E39EA73B0_ON
        #pragma shader_feature_local _ BOOLEAN_9A9F210EF4D44680B8F0CB7ACD99DFBB_ON
        
        #if defined(BOOLEAN_EAB072D9159A48C685623C0E39EA73B0_ON) && defined(BOOLEAN_9A9F210EF4D44680B8F0CB7ACD99DFBB_ON)
            #define KEYWORD_PERMUTATION_0
        #elif defined(BOOLEAN_EAB072D9159A48C685623C0E39EA73B0_ON)
            #define KEYWORD_PERMUTATION_1
        #elif defined(BOOLEAN_9A9F210EF4D44680B8F0CB7ACD99DFBB_ON)
            #define KEYWORD_PERMUTATION_2
        #else
            #define KEYWORD_PERMUTATION_3
        #endif
        
        
        // Defines
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_NORMAL
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_TANGENT
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define ATTRIBUTES_NEED_TEXCOORD0
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define VARYINGS_NEED_POSITION_WS
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define VARYINGS_NEED_TEXCOORD0
        #endif
        
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        #define SCENEPICKINGPASS 1
        #define ALPHA_CLIP_THRESHOLD 1
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        #define REQUIRE_DEPTH_TEXTURE
        #endif
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 positionOS : POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 normalOS : NORMAL;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 tangentOS : TANGENT;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 uv0 : TEXCOORD0;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
            #endif
        };
        struct Varyings
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 positionCS : SV_POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 positionWS;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 texCoord0;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
            #endif
        };
        struct SurfaceDescriptionInputs
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 WorldSpacePosition;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 ScreenPosition;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float2 NDCPosition;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float2 PixelPosition;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 uv0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 TimeParameters;
            #endif
        };
        struct VertexDescriptionInputs
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpaceNormal;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpaceTangent;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 ObjectSpacePosition;
            #endif
        };
        struct PackedVaryings
        {
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 positionCS : SV_POSITION;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float4 texCoord0 : INTERP0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             float3 positionWS : INTERP1;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
            #endif
        };
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.texCoord0.xyzw = input.texCoord0;
            output.positionWS.xyz = input.positionWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.texCoord0.xyzw;
            output.positionWS = input.positionWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        #endif
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D_TexelSize;
        float4 Color_7eff2e7fc27440719d0a871a39dabdd3;
        float Vector1_2a375228e8d3437382814520b0cd8644;
        float Vector1_5f4b185241524942830132fc71554a33;
        float Vector1_2d1f70e8efb04843b1f8184ae06925e0;
        float Vector1_33982692;
        float Vector1_10DA9AB3;
        float2 Vector2_65050940ec244ae3bf42db206c2dccca;
        float4 Texture2D_4B382D9C_TexelSize;
        float4 Texture2D_3DC46A50_TexelSize;
        float4x4 _BA_SkyboxMatrix;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D);
        SAMPLER(sampler_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D);
        TEXTURE2D(Texture2D_4B382D9C);
        SAMPLER(samplerTexture2D_4B382D9C);
        TEXTURE2D(Texture2D_3DC46A50);
        SAMPLER(samplerTexture2D_3DC46A50);
        float _BA_SkyboxRatio;
        float4 _BA_CloudData;
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_OneMinus_float(float In, out float Out)
        {
            Out = 1 - In;
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Length_float3(float3 In, out float Out)
        {
            Out = length(In);
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
        Out = A * B;
        }
        
        void Unity_Branch_float2(float Predicate, float2 True, float2 False, out float2 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
        Out = A * B;
        }
        
        void Unity_Add_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A + B;
        }
        
        void Unity_Fraction_float2(float2 In, out float2 Out)
        {
            Out = frac(In);
        }
        
        void Unity_RandomRange_float(float2 Seed, float Min, float Max, out float Out)
        {
             float randomno =  frac(sin(dot(Seed, float2(12.9898, 78.233)))*43758.5453);
             Out = lerp(Min, Max, randomno);
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Floor_float2(float2 In, out float2 Out)
        {
            Out = floor(In);
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        void Unity_Normalize_float3(float3 In, out float3 Out)
        {
            Out = normalize(In);
        }
        
        struct Bindings_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float
        {
        float2 NDCPosition;
        float3 TimeParameters;
        };
        
        void SG_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float(float2 Vector2_caefb2173a93413babdc548e42b92647, float Boolean_d17c4aa779b140eb89358932ea6663e5, float Vector1_397d6d54d391448f841b568356acd9d5, Bindings_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float IN, out float Output_1, out float3 Normal_2)
        {
        float _Property_721476b2541f47d9a2a9269e3083e0d6_Out_0_Float = Vector1_397d6d54d391448f841b568356acd9d5;
        float _Multiply_7969691e0cc14256bab4e3e287352010_Out_2_Float;
        Unity_Multiply_float_float(_Property_721476b2541f47d9a2a9269e3083e0d6_Out_0_Float, 0.321, _Multiply_7969691e0cc14256bab4e3e287352010_Out_2_Float);
        float _Property_81032f19979848909a7ca400e4b48342_Out_0_Boolean = Boolean_d17c4aa779b140eb89358932ea6663e5;
        float2 _Vector2_fd3f4c200f834792b2a20662ee9e881a_Out_0_Vector2 = float2(IN.TimeParameters.x, IN.TimeParameters.x);
        float2 _Branch_0fc6b47a1c3b42de9b4e91943c99b893_Out_3_Vector2;
        Unity_Branch_float2(_Property_81032f19979848909a7ca400e4b48342_Out_0_Boolean, _Vector2_fd3f4c200f834792b2a20662ee9e881a_Out_0_Vector2, float2(0, 0), _Branch_0fc6b47a1c3b42de9b4e91943c99b893_Out_3_Vector2);
        float2 _Multiply_2a72b24c4beb4af4911a02dc52727555_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Branch_0fc6b47a1c3b42de9b4e91943c99b893_Out_3_Vector2, float2(0.018, 0.32), _Multiply_2a72b24c4beb4af4911a02dc52727555_Out_2_Vector2);
        float2 _Add_66ffbf97cca14199972fa0e0db881b16_Out_2_Vector2;
        Unity_Add_float2((_Multiply_7969691e0cc14256bab4e3e287352010_Out_2_Float.xx), _Multiply_2a72b24c4beb4af4911a02dc52727555_Out_2_Vector2, _Add_66ffbf97cca14199972fa0e0db881b16_Out_2_Vector2);
        float2 _Fraction_396a3fa0ff464affbc41bb38a673dcc2_Out_1_Vector2;
        Unity_Fraction_float2(_Add_66ffbf97cca14199972fa0e0db881b16_Out_2_Vector2, _Fraction_396a3fa0ff464affbc41bb38a673dcc2_Out_1_Vector2);
        float _Split_63ceb6dee6e54027b8f1467e4376cee2_R_1_Float = _Fraction_396a3fa0ff464affbc41bb38a673dcc2_Out_1_Vector2[0];
        float _Split_63ceb6dee6e54027b8f1467e4376cee2_G_2_Float = _Fraction_396a3fa0ff464affbc41bb38a673dcc2_Out_1_Vector2[1];
        float _Split_63ceb6dee6e54027b8f1467e4376cee2_B_3_Float = 0;
        float _Split_63ceb6dee6e54027b8f1467e4376cee2_A_4_Float = 0;
        float _RandomRange_484c84f4919c4cd8a517b36c15a038ea_Out_3_Float;
        Unity_RandomRange_float((_Split_63ceb6dee6e54027b8f1467e4376cee2_R_1_Float.xx), 0, _ScreenParams.x, _RandomRange_484c84f4919c4cd8a517b36c15a038ea_Out_3_Float);
        float _RandomRange_b63dcf8e65f740fab3cc51e40e1d5685_Out_3_Float;
        Unity_RandomRange_float((_Split_63ceb6dee6e54027b8f1467e4376cee2_G_2_Float.xx), 0, _ScreenParams.y, _RandomRange_b63dcf8e65f740fab3cc51e40e1d5685_Out_3_Float);
        float2 _Vector2_04d66487473642d79da224f4089554dd_Out_0_Vector2 = float2(_RandomRange_484c84f4919c4cd8a517b36c15a038ea_Out_3_Float, _RandomRange_b63dcf8e65f740fab3cc51e40e1d5685_Out_3_Float);
        float4 _ScreenPosition_a0254f37c23d45ffaf168c1d3439736b_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
        float4 _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RGBA_4_Vector4;
        float3 _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RGB_5_Vector3;
        float2 _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RG_6_Vector2;
        Unity_Combine_float(_ScreenParams.x, _ScreenParams.y, 0, 0, _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RGBA_4_Vector4, _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RGB_5_Vector3, _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RG_6_Vector2);
        float2 _Multiply_3e3b6c94f64b4ade9247e71c6d2f98d6_Out_2_Vector2;
        Unity_Multiply_float2_float2((_ScreenPosition_a0254f37c23d45ffaf168c1d3439736b_Out_0_Vector4.xy), _Combine_1a40b12158f94fa2863d9768e3bb0bd8_RG_6_Vector2, _Multiply_3e3b6c94f64b4ade9247e71c6d2f98d6_Out_2_Vector2);
        float2 _Floor_4ba12f9eb3474734b0137ba643429b23_Out_1_Vector2;
        Unity_Floor_float2(_Multiply_3e3b6c94f64b4ade9247e71c6d2f98d6_Out_2_Vector2, _Floor_4ba12f9eb3474734b0137ba643429b23_Out_1_Vector2);
        float2 _Add_278d532c14154882bf1d47dde4d55ad3_Out_2_Vector2;
        Unity_Add_float2(_Vector2_04d66487473642d79da224f4089554dd_Out_0_Vector2, _Floor_4ba12f9eb3474734b0137ba643429b23_Out_1_Vector2, _Add_278d532c14154882bf1d47dde4d55ad3_Out_2_Vector2);
        float2 _Property_663e26b847554df8a237e6b5af6c8681_Out_0_Vector2 = Vector2_caefb2173a93413babdc548e42b92647;
        float2 _Divide_4576c5b640be4a1cba36f7b52c41b042_Out_2_Vector2;
        Unity_Divide_float2(_Add_278d532c14154882bf1d47dde4d55ad3_Out_2_Vector2, _Property_663e26b847554df8a237e6b5af6c8681_Out_0_Vector2, _Divide_4576c5b640be4a1cba36f7b52c41b042_Out_2_Vector2);
        float2 _Fraction_4942b1a15b0a4a019e485aa4d5bcb166_Out_1_Vector2;
        Unity_Fraction_float2(_Divide_4576c5b640be4a1cba36f7b52c41b042_Out_2_Vector2, _Fraction_4942b1a15b0a4a019e485aa4d5bcb166_Out_1_Vector2);
        float4 _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D).tex, UnityBuildTexture2DStructNoScale(_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D).samplerstate, UnityBuildTexture2DStructNoScale(_Texture2DAsset_36aff2641eb842bfb8ac1025d9bb12f9_Out_0_Texture2D).GetTransformedUV(_Fraction_4942b1a15b0a4a019e485aa4d5bcb166_Out_1_Vector2) );
        float _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_R_4_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4.r;
        float _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_G_5_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4.g;
        float _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_B_6_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4.b;
        float _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_A_7_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4.a;
        float _Split_95c0c3c67c5148a4945a0e0a8192275f_R_1_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4[0];
        float _Split_95c0c3c67c5148a4945a0e0a8192275f_G_2_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4[1];
        float _Split_95c0c3c67c5148a4945a0e0a8192275f_B_3_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4[2];
        float _Split_95c0c3c67c5148a4945a0e0a8192275f_A_4_Float = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_RGBA_0_Vector4[3];
        float3 _Vector3_54f2a95424284f65912dd46705fb9b33_Out_0_Vector3 = float3(_Split_95c0c3c67c5148a4945a0e0a8192275f_R_1_Float, _Split_95c0c3c67c5148a4945a0e0a8192275f_G_2_Float, _Split_95c0c3c67c5148a4945a0e0a8192275f_B_3_Float);
        float3 _Normalize_56d642ffdf80459aa5137d04c28ad137_Out_1_Vector3;
        Unity_Normalize_float3(_Vector3_54f2a95424284f65912dd46705fb9b33_Out_0_Vector3, _Normalize_56d642ffdf80459aa5137d04c28ad137_Out_1_Vector3);
        Output_1 = _SampleTexture2D_312136e49fa74f4d830f16fc3ac32326_G_5_Float;
        Normal_2 = _Normalize_56d642ffdf80459aa5137d04c28ad137_Out_1_Vector3;
        }
        
        void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
        {
            Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Preview_float(float In, out float Out)
        {
            Out = In;
        }
        
        void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
        {
            Out = smoothstep(Edge1, Edge2, In);
        }
        
        void Unity_Minimum_float(float A, float B, out float Out)
        {
            Out = min(A, B);
        };
        
        void Unity_SceneDepth_Linear01_float(float4 UV, out float Out)
        {
            Out = Linear01Depth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        struct Bindings_SoftParticles_18333bb3c326140abbd08c558de3bf83_float
        {
        float4 ScreenPosition;
        float2 NDCPosition;
        };
        
        void SG_SoftParticles_18333bb3c326140abbd08c558de3bf83_float(float Vector1_b12c77296e1146fa9f4d315fc85d04ab, float Vector1_0414f85da44b41c9983c5044458d5297, float Vector1_0695185d2cd141a68dcd8c1a2daa3c40, Bindings_SoftParticles_18333bb3c326140abbd08c558de3bf83_float IN, out float OutVector1_1)
        {
        float _SceneDepth_276a1c9e05034cdfa6529a8931fb79f7_Out_1_Float;
        Unity_SceneDepth_Linear01_float(float4(IN.NDCPosition.xy, 0, 0), _SceneDepth_276a1c9e05034cdfa6529a8931fb79f7_Out_1_Float);
        float _Multiply_6ce0bce77db34631b737de6ad4fa0e04_Out_2_Float;
        Unity_Multiply_float_float(_SceneDepth_276a1c9e05034cdfa6529a8931fb79f7_Out_1_Float, _ProjectionParams.z, _Multiply_6ce0bce77db34631b737de6ad4fa0e04_Out_2_Float);
        float _Property_6a749dd773734fab83f490ee0fb3c7f0_Out_0_Float = Vector1_0695185d2cd141a68dcd8c1a2daa3c40;
        float4 _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4 = IN.ScreenPosition;
        float _Split_f05c829a2fa34687a4c46a8484cc4eab_R_1_Float = _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4[0];
        float _Split_f05c829a2fa34687a4c46a8484cc4eab_G_2_Float = _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4[1];
        float _Split_f05c829a2fa34687a4c46a8484cc4eab_B_3_Float = _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4[2];
        float _Split_f05c829a2fa34687a4c46a8484cc4eab_A_4_Float = _ScreenPosition_07829bce787c467c8132fdc7149be44e_Out_0_Vector4[3];
        float _Add_39d32d1e2d4e46efa943593378a7b5d4_Out_2_Float;
        Unity_Add_float(_Property_6a749dd773734fab83f490ee0fb3c7f0_Out_0_Float, _Split_f05c829a2fa34687a4c46a8484cc4eab_A_4_Float, _Add_39d32d1e2d4e46efa943593378a7b5d4_Out_2_Float);
        float _Subtract_0db08fa14d48402dbe0edd1a2df1d281_Out_2_Float;
        Unity_Subtract_float(_Multiply_6ce0bce77db34631b737de6ad4fa0e04_Out_2_Float, _Add_39d32d1e2d4e46efa943593378a7b5d4_Out_2_Float, _Subtract_0db08fa14d48402dbe0edd1a2df1d281_Out_2_Float);
        float _Property_a1e60e28f8174d88ba77f34de3664a02_Out_0_Float = Vector1_b12c77296e1146fa9f4d315fc85d04ab;
        float _Property_72d03f4fff3e4c5f8607d2320b27c4d0_Out_0_Float = Vector1_0414f85da44b41c9983c5044458d5297;
        float2 _Vector2_d5e84c16e02d464b97da10162141cf76_Out_0_Vector2 = float2(_Property_a1e60e28f8174d88ba77f34de3664a02_Out_0_Float, _Property_72d03f4fff3e4c5f8607d2320b27c4d0_Out_0_Float);
        float _Remap_41d3d1abc133496d946826964dcf67e5_Out_3_Float;
        Unity_Remap_float(_Subtract_0db08fa14d48402dbe0edd1a2df1d281_Out_2_Float, _Vector2_d5e84c16e02d464b97da10162141cf76_Out_0_Vector2, float2 (0, 1), _Remap_41d3d1abc133496d946826964dcf67e5_Out_3_Float);
        float _Saturate_6dc9e8b8bd6a4471aba1733b5fa31a0d_Out_1_Float;
        Unity_Saturate_float(_Remap_41d3d1abc133496d946826964dcf67e5_Out_3_Float, _Saturate_6dc9e8b8bd6a4471aba1733b5fa31a0d_Out_1_Float);
        OutVector1_1 = _Saturate_6dc9e8b8bd6a4471aba1733b5fa31a0d_Out_1_Float;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            UnityTexture2D _Property_06ae7b2647934302a2bc77f5a16cfac2_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(Texture2D_3DC46A50);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_06ae7b2647934302a2bc77f5a16cfac2_Out_0_Texture2D.tex, _Property_06ae7b2647934302a2bc77f5a16cfac2_Out_0_Texture2D.samplerstate, _Property_06ae7b2647934302a2bc77f5a16cfac2_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
            float _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_R_4_Float = _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4.r;
            float _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_G_5_Float = _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4.g;
            float _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_B_6_Float = _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4.b;
            float _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_A_7_Float = _SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_RGBA_0_Vector4.a;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _OneMinus_a90ee5ec5cfd410780133317beeee956_Out_1_Float;
            Unity_OneMinus_float(_SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_A_7_Float, _OneMinus_a90ee5ec5cfd410780133317beeee956_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_fbf5240d6a974443afb86e71299332dc_Out_0_Float = Vector1_2a375228e8d3437382814520b0cd8644;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Power_4d5b0fe5fe894f37bf2bf51adfe6403f_Out_2_Float;
            Unity_Power_float(_OneMinus_a90ee5ec5cfd410780133317beeee956_Out_1_Float, _Property_fbf5240d6a974443afb86e71299332dc_Out_0_Float, _Power_4d5b0fe5fe894f37bf2bf51adfe6403f_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _OneMinus_12776925fcba4b8f8c431735c34d4ef0_Out_1_Float;
            Unity_OneMinus_float(_Power_4d5b0fe5fe894f37bf2bf51adfe6403f_Out_2_Float, _OneMinus_12776925fcba4b8f8c431735c34d4ef0_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Length_e04efe434e9a4ecc998e1dd3c3d5e90e_Out_1_Float;
            Unity_Length_float3(SHADERGRAPH_OBJECT_POSITION, _Length_e04efe434e9a4ecc998e1dd3c3d5e90e_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            Bindings_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679;
            _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679.NDCPosition = IN.NDCPosition;
            _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679.TimeParameters = IN.TimeParameters;
            float _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Output_1_Float;
            float3 _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Normal_2_Vector3;
            SG_SceenSpaceBlueNoise_304e4306f97a94e5393c8404430514e6_float(float2 (256, 256), 1, _Length_e04efe434e9a4ecc998e1dd3c3d5e90e_Out_1_Float, _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679, _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Output_1_Float, _SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Normal_2_Vector3);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Remap_bbf4dcb1a53440238a83cbaea629f9a2_Out_3_Float;
            Unity_Remap_float(_SceenSpaceBlueNoise_a5377e9df68b4fcdb3a90de8eedc4679_Output_1_Float, float2 (0, 1), float2 (0.8, 1.1), _Remap_bbf4dcb1a53440238a83cbaea629f9a2_Out_3_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4 = _BA_CloudData;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Split_3bcb0174a9954d879a71e473885b9e1b_R_1_Float = _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4[0];
            float _Split_3bcb0174a9954d879a71e473885b9e1b_G_2_Float = _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4[1];
            float _Split_3bcb0174a9954d879a71e473885b9e1b_B_3_Float = _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4[2];
            float _Split_3bcb0174a9954d879a71e473885b9e1b_A_4_Float = _Property_66d2ce3395874defaf845d95e4ecb705_Out_0_Vector4[3];
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Multiply_211f37f5b555434d956f42443155bc4a_Out_2_Float;
            Unity_Multiply_float_float(_Remap_bbf4dcb1a53440238a83cbaea629f9a2_Out_3_Float, _Split_3bcb0174a9954d879a71e473885b9e1b_A_4_Float, _Multiply_211f37f5b555434d956f42443155bc4a_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float4 _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4 = _BA_CloudData;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_R_1_Float = _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4[0];
            float _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_G_2_Float = _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4[1];
            float _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_B_3_Float = _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4[2];
            float _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_A_4_Float = _Property_1861d3a3e4494652a2a761d644654775_Out_0_Vector4[3];
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            #if defined(BOOLEAN_9A9F210EF4D44680B8F0CB7ACD99DFBB_ON)
            float _BlueNoise_b07e75159dec4ad49e7f81e3592b900e_Out_0_Float = _Multiply_211f37f5b555434d956f42443155bc4a_Out_2_Float;
            #else
            float _BlueNoise_b07e75159dec4ad49e7f81e3592b900e_Out_0_Float = _Split_fd29132f0bcf47e2bf02cb8cd0ffd453_A_4_Float;
            #endif
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _OneMinus_21be261ae35940709d0db1914ee9855c_Out_1_Float;
            Unity_OneMinus_float(_BlueNoise_b07e75159dec4ad49e7f81e3592b900e_Out_0_Float, _OneMinus_21be261ae35940709d0db1914ee9855c_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Add_2f041a64479546ff91a26ed1dbaffeee_Out_2_Float;
            Unity_Add_float(_OneMinus_21be261ae35940709d0db1914ee9855c_Out_1_Float, 0.1, _Add_2f041a64479546ff91a26ed1dbaffeee_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Preview_d4d26446fc904b76afe70165bc2d684f_Out_1_Float;
            Unity_Preview_float(_SampleTexture2D_8a2a7b5ec7c04933a113f8f4ca7a1903_B_6_Float, _Preview_d4d26446fc904b76afe70165bc2d684f_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Smoothstep_3d813f1fadfa4b9b8587408892645438_Out_3_Float;
            Unity_Smoothstep_float(_OneMinus_21be261ae35940709d0db1914ee9855c_Out_1_Float, _Add_2f041a64479546ff91a26ed1dbaffeee_Out_2_Float, _Preview_d4d26446fc904b76afe70165bc2d684f_Out_1_Float, _Smoothstep_3d813f1fadfa4b9b8587408892645438_Out_3_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Minimum_9603d95647104c398dcf64d0bf690e71_Out_2_Float;
            Unity_Minimum_float(_OneMinus_12776925fcba4b8f8c431735c34d4ef0_Out_1_Float, _Smoothstep_3d813f1fadfa4b9b8587408892645438_Out_3_Float, _Minimum_9603d95647104c398dcf64d0bf690e71_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float2 _Property_4d3f52434f0e4f098cc877f3f4dadbf9_Out_0_Vector2 = Vector2_65050940ec244ae3bf42db206c2dccca;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Split_8604fb34fc04432c845c2474347347cc_R_1_Float = _Property_4d3f52434f0e4f098cc877f3f4dadbf9_Out_0_Vector2[0];
            float _Split_8604fb34fc04432c845c2474347347cc_G_2_Float = _Property_4d3f52434f0e4f098cc877f3f4dadbf9_Out_0_Vector2[1];
            float _Split_8604fb34fc04432c845c2474347347cc_B_3_Float = 0;
            float _Split_8604fb34fc04432c845c2474347347cc_A_4_Float = 0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _OneMinus_2253c4aebc784af4b7bfe91e392d3d1c_Out_1_Float;
            Unity_OneMinus_float(_Preview_d4d26446fc904b76afe70165bc2d684f_Out_1_Float, _OneMinus_2253c4aebc784af4b7bfe91e392d3d1c_Out_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Property_a7bc7d6aca7a4351a54c042414c8dca3_Out_0_Float = Vector1_2d1f70e8efb04843b1f8184ae06925e0;
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Multiply_521b54ef01d8415cacbbab05d9ef91b4_Out_2_Float;
            Unity_Multiply_float_float(_OneMinus_2253c4aebc784af4b7bfe91e392d3d1c_Out_1_Float, _Property_a7bc7d6aca7a4351a54c042414c8dca3_Out_0_Float, _Multiply_521b54ef01d8415cacbbab05d9ef91b4_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            Bindings_SoftParticles_18333bb3c326140abbd08c558de3bf83_float _SoftParticles_33d22c55e97e4bbf9658984efedb651d;
            _SoftParticles_33d22c55e97e4bbf9658984efedb651d.ScreenPosition = IN.ScreenPosition;
            _SoftParticles_33d22c55e97e4bbf9658984efedb651d.NDCPosition = IN.NDCPosition;
            float _SoftParticles_33d22c55e97e4bbf9658984efedb651d_OutVector1_1_Float;
            SG_SoftParticles_18333bb3c326140abbd08c558de3bf83_float(_Split_8604fb34fc04432c845c2474347347cc_R_1_Float, _Split_8604fb34fc04432c845c2474347347cc_G_2_Float, _Multiply_521b54ef01d8415cacbbab05d9ef91b4_Out_2_Float, _SoftParticles_33d22c55e97e4bbf9658984efedb651d, _SoftParticles_33d22c55e97e4bbf9658984efedb651d_OutVector1_1_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            float _Multiply_603283fc9007453b86c0e29e22dc1fdf_Out_2_Float;
            Unity_Multiply_float_float(_Minimum_9603d95647104c398dcf64d0bf690e71_Out_2_Float, _SoftParticles_33d22c55e97e4bbf9658984efedb651d_OutVector1_1_Float, _Multiply_603283fc9007453b86c0e29e22dc1fdf_Out_2_Float);
            #endif
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
            #if defined(BOOLEAN_EAB072D9159A48C685623C0E39EA73B0_ON)
            float _SoftParticles_e87b688bcc4a4c4387199157c1c53214_Out_0_Float = _Multiply_603283fc9007453b86c0e29e22dc1fdf_Out_2_Float;
            #else
            float _SoftParticles_e87b688bcc4a4c4387199157c1c53214_Out_0_Float = _Minimum_9603d95647104c398dcf64d0bf690e71_Out_2_Float;
            #endif
            #endif
            surface.Alpha = _SoftParticles_e87b688bcc4a4c4387199157c1c53214_Out_0_Float;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpaceNormal =                          input.normalOS;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpaceTangent =                         input.tangentOS.xyz;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ObjectSpacePosition =                        input.positionOS;
        #endif
        
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
        
        
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.WorldSpacePosition = input.positionWS;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.ScreenPosition = ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);
        #endif
        
        
            #if UNITY_UV_STARTS_AT_TOP
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
        #endif
        
            #else
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
        #endif
        
            #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.NDCPosition.y = 1.0f - output.NDCPosition.y;
        #endif
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.uv0 = input.texCoord0;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3)
        output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
        #endif
        
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
    }
    CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
    CustomEditorForRenderPipeline "UnityEditor.ShaderGraphUnlitGUI" "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"
    FallBack "Hidden/Shader Graph/FallbackError"
}