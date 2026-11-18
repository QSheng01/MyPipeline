Shader "X3/Scene/Lit_DepthOnly" {
    Properties {
        [CustomTexture(_SurfaceSetting)][MainTexture][NoScaleOffset] _BaseMap ("BaseMap", 2D) = "white" { }
    }
    SubShader {
//            Tags { "RenderPipeline" = "UniversalPipeline"}
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        
        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        half _Cutoff;
        
        half _LocalWindStrength;
        float _LocalWindOffset;
        half _WindSpeed;
        
        TEXTURE2D(_NoiseMap);
        SAMPLER(sampler_NoiseMap);
        
        ENDHLSL
            
        Pass {
            Name "DepthOnly"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM

            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Material Keywords
            // #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile _ _WIND
            //#pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // Assets/DemoAsset/StyleDemo1/Things/Shaders/X3/Scene/ShaderInclude/LeafInput.hlsl
            // #include "ShaderInclude/LeafInput.hlsl"

            struct Attributes {
                float4 position : POSITION;
                float2 texcoord : TEXCOORD0;
                float3 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthOnlyVertex(Attributes input) {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.position.xyz);

                // 风造成的摆动
                float2 WindUV = (_Time.xx * _WindSpeed + positionWS.xz / _LocalWindOffset);
                input.position.xz += SAMPLE_TEXTURE2D_LOD(_NoiseMap, sampler_NoiseMap, WindUV, 0) * _LocalWindStrength * input.color.g;
                positionWS = TransformObjectToWorld(input.position.xyz);

                output.uv = input.texcoord;
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_TARGET {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half alpha = color.a;
                // #if defined(_ALPHATEST_ON)
                    clip(alpha - _Cutoff - 0.2f);
                // clip(alpha - _Cutoff);
                // #endif
                return 0;
            }
            ENDHLSL

        }   
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
//    CustomEditor "CustomShaderEditor.CustomShaderGUI"
}