Shader "ZhongMoDi/RoleEyeMask"
{
    Properties
    {
        [Enum(OFF, 0, FRONT, 1, BACK, 2)] _MainCullMode ("Main Cull Mode", Int) = 2
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clip", Float) = 0
        _Cutoff ("Cutoff", Range(0, 1)) = 0.5

        [Space(10)]
        [Header(MainTextures)]
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _MaskToAlphaBlend ("Mask To Alpha Blend", Range(0, 1)) = 0

        [Space(10)]
        [Header(Fragment Rebuild)]
        _OcclusionStrength ("Occlusion Strength", Range(0, 1)) = 0
        _FinalTintColor ("Final Tint Color", Color) = (1, 1, 1, 1)
        _FinalTintBlend ("Final Tint Blend", Range(0, 1)) = 0
        _FinalTintWeight ("Final Tint Weight", Range(0, 1)) = 0
        _FinalWhiteBlendScale ("Final White Blend Scale", Range(0, 2)) = 1

        [Space(10)]
        [Header(RenderDoc Replay)]
        _UseRoleInstanceData ("Use Original Instance Data", Float) = 1
        _TextureMipBias ("Texture Mip Bias", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_MainCullMode]
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _FinalTintColor;
                float _Cutoff;
                float _MaskToAlphaBlend;
                float _OcclusionStrength;
                float _FinalTintBlend;
                float _FinalTintWeight;
                float _FinalWhiteBlendScale;
                float _UseRoleInstanceData;
                float _TextureMipBias;
            CBUFFER_END

            struct RoleInstanceData
            {
                float4x4 objectMatrix;
                float4 flags;
                float4 skinOffsets;
                float4x4 secondaryMatrix;
                float4 secondaryPositionMode;
                float4 unused0;
                float4 unused1;
                float4 unused2;
                float4 unused3;
                float4 unused4;
            };

            StructuredBuffer<RoleInstanceData> _RoleInstanceData;
            StructuredBuffer<float4> _RoleSkinningBuffer;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 blendWeight : TEXCOORD1;
                uint4 blendIndex : TEXCOORD2;
                uint instanceID : SV_InstanceID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float3 TransformByRows(float3 value, float4x4 transformMatrix)
            {
                return float3(
                    dot(float3(transformMatrix._m00, transformMatrix._m01, transformMatrix._m02), value),
                    dot(float3(transformMatrix._m10, transformMatrix._m11, transformMatrix._m12), value),
                    dot(float3(transformMatrix._m20, transformMatrix._m21, transformMatrix._m22), value));
            }

            float3 TransformPositionByRows(float3 value, float4x4 transformMatrix)
            {
                return TransformByRows(value, transformMatrix) + float3(transformMatrix._m03, transformMatrix._m13, transformMatrix._m23);
            }

            float3 SkinPosition(RoleInstanceData instanceData, Attributes input)
            {
                uint rawFlags = asuint(instanceData.flags.w);
                uint boneCount = rawFlags & 4294967247u;

                if (((rawFlags & 32u) == 0u) || (boneCount == 0u))
                {
                    return input.positionOS;
                }

                uint4 positionOffset = input.blendIndex * 3u + asuint(instanceData.skinOffsets.x) + 3u;

                float4 row0 = _RoleSkinningBuffer[positionOffset.x];
                float4 row1 = _RoleSkinningBuffer[positionOffset.x + 1u];
                float4 row2 = _RoleSkinningBuffer[positionOffset.x + 2u];

                if (boneCount >= 2u)
                {
                    uint p1 = positionOffset.y;
                    row0 = row0 * input.blendWeight.x + _RoleSkinningBuffer[p1] * input.blendWeight.y;
                    row1 = row1 * input.blendWeight.x + _RoleSkinningBuffer[p1 + 1u] * input.blendWeight.y;
                    row2 = row2 * input.blendWeight.x + _RoleSkinningBuffer[p1 + 2u] * input.blendWeight.y;
                }

                if (boneCount >= 4u)
                {
                    uint p2 = positionOffset.z;
                    uint p3 = positionOffset.w;
                    row0 += _RoleSkinningBuffer[p2] * input.blendWeight.z + _RoleSkinningBuffer[p3] * input.blendWeight.w;
                    row1 += _RoleSkinningBuffer[p2 + 1u] * input.blendWeight.z + _RoleSkinningBuffer[p3 + 1u] * input.blendWeight.w;
                    row2 += _RoleSkinningBuffer[p2 + 2u] * input.blendWeight.z + _RoleSkinningBuffer[p3 + 2u] * input.blendWeight.w;
                }

                float4 positionOS = float4(input.positionOS, 1.0);
                return float3(dot(row0, positionOS), dot(row1, positionOS), dot(row2, positionOS));
            }

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS;
                positionWS = TransformObjectToWorld(input.positionOS);
             

                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.uv.y = 1 - output.uv.y;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
             
                float4 baseSample = SAMPLE_TEXTURE2D_BIAS(_BaseMap, sampler_BaseMap, input.uv, _TextureMipBias);
                
                float4 alphaMaskSample = float4(1.0, 1.0, 1.0, baseSample.r);
                float4 color = lerp(baseSample, alphaMaskSample, _MaskToAlphaBlend) * _BaseColor;

                // The original PS subtracts a clustered-light/volumetric occlusion term before final compositing.
                color.a *= 1.0 - saturate(_OcclusionStrength);

                float tintMix = saturate(_FinalTintBlend * _FinalTintWeight);
                float3 tintedColor = lerp(color.rgb, _FinalTintColor.rgb, tintMix);
                float whiteBlend = saturate(color.a * _BaseColor.a * (1.0 - tintMix) * _FinalWhiteBlendScale);
                color.rgb = lerp(1.0.xxx, tintedColor, whiteBlend);

                #if defined(_ALPHATEST_ON)
                    clip(color.a - _Cutoff);
                #endif

                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
