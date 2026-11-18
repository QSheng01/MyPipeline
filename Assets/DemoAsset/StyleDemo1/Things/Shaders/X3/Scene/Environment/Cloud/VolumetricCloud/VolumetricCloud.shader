Shader "ShiYue/URP/Cloud/VolumetricCloud"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "Min Depth"

            HLSLPROGRAM
            #pragma vertex CloudVert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "VolumetricCloudCommon.hlsl"

            float Fragment(CloudVaryings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float2 texel = _VolumetricCloudFrameParams.zw;
                float minRawDepth = 1.0;

                [unroll]
                for (int gatherIndex = 0; gatherIndex < 5; ++gatherIndex)
                {
                    float2 offset = gatherIndex == 1 ? float2(-2.0, 0.0) :
                        gatherIndex == 2 ? float2(2.0, 0.0) :
                        gatherIndex == 3 ? float2(0.0, 2.0) :
                        gatherIndex == 4 ? float2(0.0, -2.0) : 0.0;
                    minRawDepth = min(minRawDepth, Min4(GatherSceneRawDepth(uv + offset * texel)));
                }

                return minRawDepth;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Coarse March"

            HLSLPROGRAM
            #pragma vertex CloudVert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "VolumetricCloudCommon.hlsl"

            float Fragment(CloudVaryings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return MarchCloudHitEvent273(input.texcoord, input.rayData, (int)_VolumetricCloudStepParams.x);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Refine March"

            HLSLPROGRAM
            #pragma vertex CloudVert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "VolumetricCloudCommon.hlsl"

            float Fragment(CloudVaryings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float coarseHit = SAMPLE_TEXTURE2D_LOD(_VolumetricCloudCoarseHitTex, sampler_VolumetricCloudCoarseHitTex, uv, 0).r;
                if (coarseHit <= 0.0)
                {
                    return 0.0;
                }

                return MarchCloudHitEvent280(uv, input.rayData, coarseHit, (int)_VolumetricCloudStepParams.y);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Final Cloud"
            ZWrite On

            HLSLPROGRAM
            #pragma vertex CloudVert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "VolumetricCloudCommon.hlsl"

            struct CloudFragmentOutput
            {
                float4 color : SV_Target;
                float depth : SV_Depth;
            };

            CloudFragmentOutput Fragment(CloudVaryings input)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                CloudFragmentOutput output;
                float2 uv = input.texcoord;

                float refinedHit = SAMPLE_TEXTURE2D_LOD(_VolumetricCloudRefinedHitTex, sampler_VolumetricCloudRefinedHitTex, uv, 0).r;
                if (refinedHit <= 0.0)
                {
                    output.color = 0.0;
                    output.depth = CloudRawFarDepth();
                    return output;
                }

                CloudRay ray = BuildCloudRay(input.rayData);
                float hitDistance = DecodeCaptureDistance(refinedHit, ray.invLength);
                float jitter = CloudScreenJitter(uv);
                hitDistance += (jitter - 0.5) * max(_VolumetricCloudStepParams.w * 0.05, 1.0);

                float alpha;
                float3 color = MarchCloudLighting(ray.origin, ray.direction, hitDistance, (int)_VolumetricCloudStepParams.z, alpha);
                output.color = float4(color, alpha);
                output.depth = EncodeCaptureDistance(hitDistance, ray.invLength);
                return output;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Composite"

            HLSLPROGRAM
            #pragma vertex CloudVert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "VolumetricCloudCommon.hlsl"

            SAMPLER(sampler_BlitTexture);

            float4 Fragment(CloudVaryings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float4 sceneColor = _BlitTexture.SampleLevel(sampler_BlitTexture, uv, 0);
                float4 cloud = SAMPLE_TEXTURE2D(_VolumetricCloudColorTex, sampler_VolumetricCloudColorTex, uv);
                float3 color = lerp(sceneColor.rgb, sceneColor.rgb + cloud.rgb, saturate(cloud.a));
                return float4(color, sceneColor.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
