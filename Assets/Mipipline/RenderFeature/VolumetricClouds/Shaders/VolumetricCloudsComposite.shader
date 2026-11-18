// Full-resolution, depth-aware composite of the half-resolution volumetric clouds (HDRP-style).
// Runs as a fragment pass so it can read URP's global _CameraDepthTexture at full resolution
// (compute cannot). Each full-res pixel weights the half-res cloud taps by depth similarity to its
// true scene depth, which aligns the cloud to geometry silhouettes and reduces the edge shimmer you
// get from a plain bilinear upscale of a half-res occlusion buffer (e.g. alpha-test foliage placed
// inside local clouds). Self-contained (no Blit.hlsl) so it doesn't drag in the post-processing
// sampler dependencies; driven via CoreUtils.DrawFullScreen.
Shader "Hidden/Baozang/VolumetricCloudsComposite"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "CloudsComposite"
            ZTest Always ZWrite Off Cull Off Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            Texture2D<float4> _VolumetricCloudsTexture;  // half-res cloud lighting (rgb + transmittance in .a)
            Texture2D<float4> _DepthStatusTexture;       // half-res status: .y = scene depth, .z = cloud depth
            Texture2D<float4> _CameraColorCopy;          // full-res copy of the camera color
            Texture2D<float>  _OwenScrambledTexture;
            Texture2D<float>  _ScramblingTileXSPP;
            Texture2D<float>  _RankingTileXSPP;

            float4 _FinalScreenSize;                     // xy = full-res size, zw = 1/size
            float4 _IntermediateScreenSize;              // xy = half-res size, zw = 1/size
            float4 _CloudsZBufferParams;
            float4 _CloudFogColor;
            float  _CloudFogDistance;
            float  _MaxCloudDistance;
            float  _ImprovedTransmittanceBlend;
            float  _LocalCloudsF;                        // 1 = local clouds, 0 = distant
            float  _CompositeSkipDepthWeight;            // 1 = skip-reproject (raw trace source, no scene-depth status): plain bilinear
            int    _UseBlueNoise;
            int    _UseBlueNoiseUpscale;
            int    _AccumulationFrameIndex;

            struct Attributes { uint vertexID : SV_VertexID; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                o.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return o;
            }

            float FinalTransmittance(float3 color, float transmittance)
            {
                float luminance = Luminance(color);
                if (luminance <= 0.0)
                    return transmittance;
                float resultLuminance = luminance / (1.0 + luminance) * transmittance;
                resultLuminance = resultLuminance / (1.0 - resultLuminance);
                float finalT = max(resultLuminance / luminance, pow(transmittance, 6));
                return lerp(transmittance, finalT, _ImprovedTransmittanceBlend);
            }

            float RelativeDepthDelta(float fullDepthRaw, float tapDepthRaw)
            {
                bool fullIsBackground = (fullDepthRaw == UNITY_RAW_FAR_CLIP_VALUE);
                bool tapIsBackground = (tapDepthRaw == UNITY_RAW_FAR_CLIP_VALUE);
                if (fullIsBackground || tapIsBackground)
                    return (fullIsBackground == tapIsBackground) ? 0.0 : 1e6;

                float fullLin = LinearEyeDepth(fullDepthRaw, _CloudsZBufferParams);
                float tapLin = LinearEyeDepth(tapDepthRaw, _CloudsZBufferParams);
                return abs(fullLin - tapLin) / max(fullLin, 1.0);
            }

            float Hash21(float2 p, int frame)
            {
                p += frame * 0.61803398875;
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float GetBNDSequenceSample(uint2 pixelCoord, uint sampleIndex, uint sampleDimension)
            {
                pixelCoord = pixelCoord & 127;
                sampleIndex = sampleIndex & 255;
                sampleDimension = sampleDimension & 255;

                uint rankingIndex = (pixelCoord.x + pixelCoord.y * 128) * 8 + (sampleDimension & 7);
                uint rankedSampleIndex = sampleIndex ^ clamp((uint)(_RankingTileXSPP.Load(int3((int)(rankingIndex & 127), (int)(rankingIndex / 128), 0)) * 256.0), 0, 255);

                uint value = clamp((uint)(_OwenScrambledTexture.Load(int3((int)sampleDimension, (int)rankedSampleIndex, 0)) * 256.0), 0, 255);

                uint scramblingIndex = (pixelCoord.x + pixelCoord.y * 128) * 8 + (sampleDimension & 7);
                float scramblingValue = min(_ScramblingTileXSPP.Load(int3((int)(scramblingIndex & 127), (int)(scramblingIndex / 128), 0)), 0.999);
                value = value ^ uint(scramblingValue * 256.0);

                return (scramblingValue + value) / 256.0;
            }

            float2 UpscaleJitter(uint2 fullCoord)
            {
                if (_UseBlueNoiseUpscale == 0)
                    return float2(0.0, 0.0);

                if (_UseBlueNoise != 0)
                {
                    uint frame = (uint)_AccumulationFrameIndex & 15;
                    return float2(
                        frac(GetBNDSequenceSample(fullCoord, frame, 0) + frame * 1.618034) - 0.5,
                        frac(GetBNDSequenceSample(fullCoord.yx, frame, 1) + frame * 1.618034) - 0.5);
                }

                return float2(
                    Hash21((float2)fullCoord, _AccumulationFrameIndex) - 0.5,
                    Hash21((float2)fullCoord.yx + 19.7, _AccumulationFrameIndex) - 0.5);
            }

            // Depth-aware upsample of the intermediate cloud, guided by the full-res scene depth.
            // Smooth regions use bilinear filtering; discontinuities snap to the depth-closest tap to
            // avoid leaking half-res cloud opacity across foliage and other alpha-test silhouettes.
            float4 DepthAwareUpsample(float2 uv, uint2 fullCoord, float fullDepthRaw)
            {
                float2 halfCoordF = uv * _IntermediateScreenSize.xy - 0.5;
                float upscaleAmount = max(_FinalScreenSize.x * _IntermediateScreenSize.z, _FinalScreenSize.y * _IntermediateScreenSize.w);
                float jitterScale = saturate(upscaleAmount - 1.0);
                if (jitterScale > 0.0)
                {
                    int2 cloudDepthCoord = clamp((int2)round(halfCoordF), int2(0, 0), (int2)_IntermediateScreenSize.xy - 1);
                    // Skip-reproject source is the raw trace depth (cloud distance in .x); history1 stores it in .z.
                    float cloudDepth = (_CompositeSkipDepthWeight != 0.0)
                        ? _DepthStatusTexture.Load(int3(cloudDepthCoord, 0)).x
                        : _DepthStatusTexture.Load(int3(cloudDepthCoord, 0)).z;
                    float kernel = lerp(1.0, 0.35, saturate(cloudDepth / max(_MaxCloudDistance, 1.0))) * jitterScale;
                    halfCoordF += UpscaleJitter(fullCoord) * kernel;
                }

                int2 baseC = (int2)floor(halfCoordF);
                float2 f = frac(halfCoordF);

                float4 sum = 0.0;
                float wsum = 0.0;
                float bestDelta = 1e20;
                float4 bestCloud = float4(0.0, 0.0, 0.0, 1.0);
                bool allDepthsMatch = true;
                const float kDepthMatchThreshold = 0.1;

                [unroll]
                for (int y = 0; y <= 1; ++y)
                [unroll]
                for (int x = 0; x <= 1; ++x)
                {
                    int2 c = clamp(baseC + int2(x, y), 0, (int2)_IntermediateScreenSize.xy - 1);
                    float bilW = ((x == 0) ? (1.0 - f.x) : f.x) * ((y == 0) ? (1.0 - f.y) : f.y);

                    float4 cloud = _VolumetricCloudsTexture.Load(int3(c, 0));

                    // Skip-reproject: raw trace source has no scene-depth status -> plain bilinear
                    // (allDepthsMatch stays true, so we return the bilinear sum below).
                    if (_CompositeSkipDepthWeight == 0.0)
                    {
                        float tapSceneRaw = _DepthStatusTexture.Load(int3(c, 0)).y;
                        float delta = RelativeDepthDelta(fullDepthRaw, tapSceneRaw);
                        if (delta > kDepthMatchThreshold)
                            allDepthsMatch = false;
                        if (delta < bestDelta)
                        {
                            bestDelta = delta;
                            bestCloud = cloud;
                        }
                    }

                    float w = bilW + 1e-6;
                    sum += cloud * w;
                    wsum += w;
                }

                return allDepthsMatch ? (sum / wsum) : bestCloud;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                int2 fullCoord = (int2)input.positionCS.xy;
                float4 currentColor = _CameraColorCopy.Load(int3(fullCoord, 0));

                float fullDepthRaw = SampleSceneDepth(input.uv);
                bool localClouds = (_LocalCloudsF != 0.0);
                bool fullIsBackground = (fullDepthRaw == UNITY_RAW_FAR_CLIP_VALUE);

                // Local clouds overlay every pixel; distant clouds are only visible on the sky.
                float4 clouds;
                if (localClouds || fullIsBackground)
                    clouds = DepthAwareUpsample(input.uv, (uint2)fullCoord, fullDepthRaw);
                else
                    clouds = float4(0.0, 0.0, 0.0, 1.0);
                clouds.w = saturate(clouds.w);

                // Optional distance fog on the clouds.
                if (_CloudFogDistance > 0.0)
                {
                    float fogFactor = saturate(_MaxCloudDistance / _CloudFogDistance);
                    clouds.xyz = lerp(clouds.xyz, _CloudFogColor.xyz, fogFactor * (1.0 - clouds.w));
                }

                float ft = FinalTransmittance(currentColor.rgb, clouds.w);
                return float4(currentColor.xyz * ft + clouds.xyz, currentColor.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
