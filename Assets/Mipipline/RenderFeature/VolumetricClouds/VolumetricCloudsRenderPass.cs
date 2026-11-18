using Sirenix.Utilities;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Baozang.Rendering.VolumetricClouds
{
    public class VolumetricCloudsRenderPass : ScriptableRenderPass
    {
        const float k_EarthRadius = 6378100.0f;
        const int k_ShapeNoiseRes = 64;
        const int k_ErosionNoiseRes = 32;
        const int k_CloudMapRes = 256;
        const int k_LutRes = 32;

        readonly VolumetricCloudsRenderFeature.Settings m_Settings;
        readonly ProfilingSampler m_Sampler = new ProfilingSampler("Volumetric Clouds");

        // Baked static resources
        RenderTexture m_ShapeNoise;
        RenderTexture m_ErosionNoise;
        Texture2D m_CloudMap;
        Texture2D m_CloudLut;

        // Fragment material used to downsample the global camera depth (compute can't read it)
        Material m_DepthDownsampleMaterial;
        const string k_DepthDownsampleShader = "Hidden/Baozang/VolumetricCloudsDepthDownsample";

        // Fragment material for the optional full-resolution depth-aware composite (needs the global
        // full-res _CameraDepthTexture, which compute cannot read).
        Material m_CompositeMaterial;
        const string k_CompositeShader = "Hidden/Baozang/VolumetricCloudsComposite";
        float m_MaxCloudDistance;

        // Transient per-frame RTs
        RTHandle m_TraceLighting;
        RTHandle m_TraceDepth;
        RTHandle m_HalfResDepth;
        RTHandle m_ColorCopy;
        RTHandle m_UpscaleResult;

        // Per camera history
        class CamHistory
        {
            public RTHandle[] history0 = new RTHandle[2];
            public RTHandle[] history1 = new RTHandle[2];
            public Matrix4x4 prevVP;
            public int frameIndex;
            public bool valid;
            public int width, height;
        }
        readonly Dictionary<int, CamHistory> m_Histories = new Dictionary<int, CamHistory>();

        // Wind animation state
        float m_LastTime = -1f;
        Vector2 m_CloudOffset;
        float m_VerticalShapeOffset;
        float m_VerticalErosionOffset;

        // Authoring parameters come from the Volume stack (resolved per-frame in Execute).
        VolumetricCloudsVolume m_Volume;

        // Dirty tracking so the procedural cloud map / LUT are only rebuilt when their inputs change.
        float m_LastCoverage = float.NaN;
        int m_LastLutHash;

        // Shader IDs
        static class ID
        {
            public static readonly int _NoiseOutput = Shader.PropertyToID("_NoiseOutput");
            public static readonly int _NoiseResolution = Shader.PropertyToID("_NoiseResolution");

            public static readonly int _Worley128RGBA = Shader.PropertyToID("_Worley128RGBA");
            public static readonly int _ErosionNoise = Shader.PropertyToID("_ErosionNoise");
            public static readonly int _CloudMapTexture = Shader.PropertyToID("_CloudMapTexture");
            public static readonly int _CloudLutTexture = Shader.PropertyToID("_CloudLutTexture");
            public static readonly int _UseBlueNoise = Shader.PropertyToID("_UseBlueNoise");
            public static readonly int _OwenScrambledTexture = Shader.PropertyToID("_OwenScrambledTexture");
            public static readonly int _ScramblingTileXSPP = Shader.PropertyToID("_ScramblingTileXSPP");
            public static readonly int _RankingTileXSPP = Shader.PropertyToID("_RankingTileXSPP");
            public static readonly int _CloudsFullResTexelSize = Shader.PropertyToID("_CloudsFullResTexelSize");

            public static readonly int _CloudsLightingTextureRW = Shader.PropertyToID("_CloudsLightingTextureRW");
            public static readonly int _CloudsDepthTextureRW = Shader.PropertyToID("_CloudsDepthTextureRW");
            public static readonly int _CloudsLightingTexture = Shader.PropertyToID("_CloudsLightingTexture");
            public static readonly int _CloudsDepthTexture = Shader.PropertyToID("_CloudsDepthTexture");
            public static readonly int _HalfResDepthBuffer = Shader.PropertyToID("_HalfResDepthBuffer");
            public static readonly int _HistoryVolumetricClouds0Texture = Shader.PropertyToID("_HistoryVolumetricClouds0Texture");
            public static readonly int _HistoryVolumetricClouds1Texture = Shader.PropertyToID("_HistoryVolumetricClouds1Texture");
            public static readonly int _CloudsAdditionalTextureRW = Shader.PropertyToID("_CloudsAdditionalTextureRW");
            public static readonly int _VolumetricCloudsTexture = Shader.PropertyToID("_VolumetricCloudsTexture");
            public static readonly int _DepthStatusTexture = Shader.PropertyToID("_DepthStatusTexture");
            public static readonly int _CameraColorCopy = Shader.PropertyToID("_CameraColorCopy");
            public static readonly int _VolumetricCloudsUpscaleTextureRW = Shader.PropertyToID("_VolumetricCloudsUpscaleTextureRW");
            public static readonly int _UseDepthRejection = Shader.PropertyToID("_UseDepthRejection");
            public static readonly int _UseMinMaxDepth = Shader.PropertyToID("_UseMinMaxDepth");
            public static readonly int _UseBlueNoiseUpscale = Shader.PropertyToID("_UseBlueNoiseUpscale");
            public static readonly int _DepthRejectionThreshold = Shader.PropertyToID("_DepthRejectionThreshold");
            public static readonly int _RayPlacementDepthThreshold = Shader.PropertyToID("_RayPlacementDepthThreshold");
            public static readonly int _CheckerboardMinMaxDepth = Shader.PropertyToID("_CheckerboardMinMaxDepth");
            public static readonly int _CheckerboardDepthOutput = Shader.PropertyToID("_CheckerboardDepthOutput");
            public static readonly int _CloudsZBufferParams = Shader.PropertyToID("_CloudsZBufferParams");

            // CB
            public static readonly int _HighestCloudAltitude = Shader.PropertyToID("_HighestCloudAltitude");
            public static readonly int _LowestCloudAltitude = Shader.PropertyToID("_LowestCloudAltitude");
            public static readonly int _EarthRadius = Shader.PropertyToID("_EarthRadius");
            public static readonly int _CloudRangeSquared = Shader.PropertyToID("_CloudRangeSquared");
            public static readonly int _MaxRayMarchingDistance = Shader.PropertyToID("_MaxRayMarchingDistance");
            public static readonly int _MaxCloudDistance = Shader.PropertyToID("_MaxCloudDistance");
            public static readonly int _NormalizationFactor = Shader.PropertyToID("_NormalizationFactor");
            public static readonly int _NumPrimarySteps = Shader.PropertyToID("_NumPrimarySteps");
            public static readonly int _NumLightSteps = Shader.PropertyToID("_NumLightSteps");
            public static readonly int _CloudMapTiling = Shader.PropertyToID("_CloudMapTiling");
            public static readonly int _WindDirection = Shader.PropertyToID("_WindDirection");
            public static readonly int _WindVector = Shader.PropertyToID("_WindVector");
            public static readonly int _ShapeNoiseOffset = Shader.PropertyToID("_ShapeNoiseOffset");
            public static readonly int _VerticalShapeNoiseOffset = Shader.PropertyToID("_VerticalShapeNoiseOffset");
            public static readonly int _VerticalShapeWindDisplacement = Shader.PropertyToID("_VerticalShapeWindDisplacement");
            public static readonly int _VerticalErosionWindDisplacement = Shader.PropertyToID("_VerticalErosionWindDisplacement");
            public static readonly int _LargeWindSpeed = Shader.PropertyToID("_LargeWindSpeed");
            public static readonly int _MediumWindSpeed = Shader.PropertyToID("_MediumWindSpeed");
            public static readonly int _SmallWindSpeed = Shader.PropertyToID("_SmallWindSpeed");
            public static readonly int _AltitudeDistortion = Shader.PropertyToID("_AltitudeDistortion");
            public static readonly int _SunLightColor = Shader.PropertyToID("_SunLightColor");
            public static readonly int _SunDirection = Shader.PropertyToID("_SunDirection");
            public static readonly int _MultiScattering = Shader.PropertyToID("_MultiScattering");
            public static readonly int _ErosionOcclusion = Shader.PropertyToID("_ErosionOcclusion");
            public static readonly int _PowderEffectIntensity = Shader.PropertyToID("_PowderEffectIntensity");
            public static readonly int _ScatteringTint = Shader.PropertyToID("_ScatteringTint");
            public static readonly int _CloudAmbientTop = Shader.PropertyToID("_CloudAmbientTop");
            public static readonly int _CloudAmbientBottom = Shader.PropertyToID("_CloudAmbientBottom");
            public static readonly int _DensityMultiplier = Shader.PropertyToID("_DensityMultiplier");
            public static readonly int _ShapeFactor = Shader.PropertyToID("_ShapeFactor");
            public static readonly int _ErosionFactor = Shader.PropertyToID("_ErosionFactor");
            public static readonly int _ShapeScale = Shader.PropertyToID("_ShapeScale");
            public static readonly int _ErosionScale = Shader.PropertyToID("_ErosionScale");
            public static readonly int _ErosionFactorCompensation = Shader.PropertyToID("_ErosionFactorCompensation");
            public static readonly int _TemporalAccumulationFactor = Shader.PropertyToID("_TemporalAccumulationFactor");
            public static readonly int _CloudHistoryInvalidation = Shader.PropertyToID("_CloudHistoryInvalidation");
            public static readonly int _AccumulationFrameIndex = Shader.PropertyToID("_AccumulationFrameIndex");
            public static readonly int _SubPixelIndex = Shader.PropertyToID("_SubPixelIndex");
            public static readonly int _EnableIntegration = Shader.PropertyToID("_EnableIntegration");
            public static readonly int _LowResolutionEvaluation = Shader.PropertyToID("_LowResolutionEvaluation");
            public static readonly int _FadeInStart = Shader.PropertyToID("_FadeInStart");
            public static readonly int _FadeInDistance = Shader.PropertyToID("_FadeInDistance");
            public static readonly int _FinalScreenSize = Shader.PropertyToID("_FinalScreenSize");
            public static readonly int _IntermediateScreenSize = Shader.PropertyToID("_IntermediateScreenSize");
            public static readonly int _TraceScreenSize = Shader.PropertyToID("_TraceScreenSize");
            public static readonly int _HistoryViewportSize = Shader.PropertyToID("_HistoryViewportSize");
            public static readonly int _HistoryBufferSize = Shader.PropertyToID("_HistoryBufferSize");
            public static readonly int _CloudsCameraPosWS = Shader.PropertyToID("_CloudsCameraPosWS");
            public static readonly int _CloudsInvVP = Shader.PropertyToID("_CloudsInvVP");
            public static readonly int _CloudsVP = Shader.PropertyToID("_CloudsVP");
            public static readonly int _CloudsPrevVP = Shader.PropertyToID("_CloudsPrevVP");
            public static readonly int _ImprovedTransmittanceBlend = Shader.PropertyToID("_ImprovedTransmittanceBlend");
            public static readonly int _CloudFogColor = Shader.PropertyToID("_CloudFogColor");
            public static readonly int _CloudFogDistance = Shader.PropertyToID("_CloudFogDistance");
            public static readonly int _LocalClouds = Shader.PropertyToID("_LocalClouds");
            public static readonly int _LocalCloudsF = Shader.PropertyToID("_LocalCloudsF");
            public static readonly int _CloudNearPlane = Shader.PropertyToID("_CloudNearPlane");
            public static readonly int _CloudFarPlane = Shader.PropertyToID("_CloudFarPlane");
            public static readonly int _UpscaleSourceSize = Shader.PropertyToID("_UpscaleSourceSize");
            public static readonly int _HalfResDepthSize = Shader.PropertyToID("_HalfResDepthSize");
            public static readonly int _SkipReprojection = Shader.PropertyToID("_SkipReprojection");
            public static readonly int _CompositeSkipDepthWeight = Shader.PropertyToID("_CompositeSkipDepthWeight");
        }

        public VolumetricCloudsRenderPass(VolumetricCloudsRenderFeature.Settings settings)
        {
            m_Settings = settings;
        }

        //------------------------------------------------------------------------------------------
        // Baking of static resources
        //------------------------------------------------------------------------------------------
        void BakeResourcesIfNeeded(CommandBuffer cmd, in VolumetricCloudsVolume.EffectiveShape eff)
        {
            var noiseCS = m_Settings.noiseGenCS;

            // Shape noise: bake only if the user did not provide a texture.
            if (m_Settings.shapeNoiseTexture == null && (m_ShapeNoise == null || !m_ShapeNoise.IsCreated()))
            {
                m_ShapeNoise = AllocNoiseVolume(m_ShapeNoise, k_ShapeNoiseRes, "CloudShapeNoise");
                int shapeKernel = noiseCS.FindKernel("BakeShapeNoise");
                cmd.SetComputeTextureParam(noiseCS, shapeKernel, ID._NoiseOutput, m_ShapeNoise);
                cmd.SetComputeFloatParam(noiseCS, ID._NoiseResolution, k_ShapeNoiseRes);
                cmd.DispatchCompute(noiseCS, shapeKernel, DivRoundUp(k_ShapeNoiseRes, 4), DivRoundUp(k_ShapeNoiseRes, 4), DivRoundUp(k_ShapeNoiseRes, 4));
            }

            // Erosion noise
            if (m_Settings.erosionNoiseTexture == null && (m_ErosionNoise == null || !m_ErosionNoise.IsCreated()))
            {
                m_ErosionNoise = AllocNoiseVolume(m_ErosionNoise, k_ErosionNoiseRes, "CloudErosionNoise");
                int erosionKernel = noiseCS.FindKernel("BakeErosionNoise");
                cmd.SetComputeTextureParam(noiseCS, erosionKernel, ID._NoiseOutput, m_ErosionNoise);
                cmd.SetComputeFloatParam(noiseCS, ID._NoiseResolution, k_ErosionNoiseRes);
                cmd.DispatchCompute(noiseCS, erosionKernel, DivRoundUp(k_ErosionNoiseRes, 4), DivRoundUp(k_ErosionNoiseRes, 4), DivRoundUp(k_ErosionNoiseRes, 4));
            }

            // Cloud map: build procedurally only if the user did not provide one; rebuild when the
            // coverage slider changes.
            if (m_Volume.cloudMap.value == null)
            {
                float coverage = m_Volume.cloudCoverage.value;
                if (m_CloudMap == null || coverage != m_LastCoverage)
                {
                    BuildCloudMap(coverage);
                    m_LastCoverage = coverage;
                }
            }

            // Cloud LUT: built from the effective profile curves; rebuild when the curves change.
            if (m_Volume.cloudLut.value == null)
            {
                int lutHash = CurveHash(eff.densityCurve) * 486187739
                            ^ CurveHash(eff.erosionCurve) * 16777619
                            ^ CurveHash(eff.ambientOcclusionCurve);
                if (m_CloudLut == null || lutHash != m_LastLutHash)
                {
                    BuildCloudLut(eff);
                    m_LastLutHash = lutHash;
                }
            }
        }

        static int CurveHash(AnimationCurve c)
        {
            if (c == null) return 0;
            int h = 17;
            h = h * 31 + c.length;
            for (int i = 0; i < c.length; i++)
            {
                var k = c[i];
                h = h * 31 + k.time.GetHashCode();
                h = h * 31 + k.value.GetHashCode();
                h = h * 31 + k.inTangent.GetHashCode();
                h = h * 31 + k.outTangent.GetHashCode();
            }
            return h;
        }

        static RenderTexture AllocNoiseVolume(RenderTexture existing, int res, string name)
        {
            if (existing != null && existing.IsCreated() && existing.width == res)
                return existing;
            if (existing != null) existing.Release();

            var desc = new RenderTextureDescriptor(res, res, RenderTextureFormat.RHalf, 0)
            {
                dimension = TextureDimension.Tex3D,
                volumeDepth = res,
                enableRandomWrite = true,
                msaaSamples = 1
            };
            var rt = new RenderTexture(desc) { name = name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear };
            rt.Create();
            return rt;
        }

        // Tiling fbm on Perlin noise (period p keeps the map seamless).
        static float Fbm(float u, float v, float baseFreq, float ox, float oy, int octaves)
        {
            float sum = 0f, amp = 0.5f, freq = baseFreq, norm = 0f;
            for (int o = 0; o < octaves; o++)
            {
                sum += amp * Mathf.PerlinNoise(u * freq + ox, v * freq + oy);
                norm += amp;
                amp *= 0.5f; freq *= 2f;
            }
            return sum / Mathf.Max(norm, 1e-4f);
        }

        void BuildCloudMap(float baseCoverage)
        {
            if (m_CloudMap == null)
                m_CloudMap = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None) { name = "Default Cloud Map Texture" };
            m_CloudMap.SetPixel(0, 0, new Color(0.9f, 0.0f, 0.25f, 1.0f));
            m_CloudMap.Apply(false, false);
        }

        // Vertical density gradient control points (a<b<c<d over normalized height) for the three
        // canonical cloud archetypes, à la Nubis / GPU Pro. density(h) = smoothstep(a,b,h)*(1-smoothstep(c,d,h)).
        static readonly Vector4 k_GradientStratus = new Vector4(0.00f, 0.06f, 0.13f, 0.22f); // low & thin
        static readonly Vector4 k_GradientCumulus = new Vector4(0.00f, 0.18f, 0.45f, 0.70f); // rounded, billowy
        static readonly Vector4 k_GradientCumulonimbus = new Vector4(0.00f, 0.06f, 0.82f, 1.00f); // tall, full column

        static float DensityGradient(float h, Vector4 p)
        {
            return SmoothStep01(p.x, p.y, h) * (1f - SmoothStep01(p.z, p.w, h));
        }

        void BuildCloudLut(in VolumetricCloudsVolume.EffectiveShape eff)
        {
            if (m_CloudLut == null)
                m_CloudLut = new Texture2D(1, k_LutRes, TextureFormat.RGBAHalf, false, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "CloudLut" };

            var pixels = new Color[k_LutRes * 1];
            var densityCurve = eff.densityCurve;
            var erosionCurve = eff.erosionCurve;
            var ambientOcclusionCurve = eff.ambientOcclusionCurve;
            if (densityCurve == null || densityCurve.length == 0)
            {
                for (int i = 0; i < k_LutRes; i++)
                    pixels[i] = Color.white;
            }
            else
            {
                float step = 1.0f / (k_LutRes - 1f);

                for (int i = 0; i < k_LutRes; i++)
                {
                    float currTime = step * i;
                    float density = (i == 0 || i == k_LutRes - 1) ? 0 : Mathf.Clamp(densityCurve.Evaluate(currTime), 0.0f, 1.0f);
                    float erosion = Mathf.Clamp(erosionCurve.Evaluate(currTime), 0.0f, 1.0f);
                    float ambientOcclusion = Mathf.Clamp(1.0f - ambientOcclusionCurve.Evaluate(currTime), 0.0f, 1.0f);
                    pixels[i] = new Color(density, erosion, ambientOcclusion, 1.0f);
                }
            }          
            m_CloudLut.SetPixels(pixels);
            m_CloudLut.Apply(false, false);
        }

        //------------------------------------------------------------------------------------------
        // Execute
        //------------------------------------------------------------------------------------------
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cs = m_Settings.volumetricCloudsCS;
            if (cs == null)
                return;

            // Authoring parameters come from the Volume stack. Skip entirely if there is no active
            // volumetric clouds override in the current volume profile.
            var stack = VolumeManager.instance != null ? VolumeManager.instance.stack : null;
            m_Volume = stack != null ? stack.GetComponent<VolumetricCloudsVolume>() : null;
            if (m_Volume == null || !m_Volume.IsActive())
                return;

            if (m_DepthDownsampleMaterial == null)
            {
                var shader = m_Settings.depthDownsampleShader != null ? m_Settings.depthDownsampleShader : Shader.Find(k_DepthDownsampleShader);
                if (shader == null)
                {
                    Debug.LogError("[VolumetricClouds] Depth downsample shader not found: " + k_DepthDownsampleShader);
                    return;
                }
                m_DepthDownsampleMaterial = CoreUtils.CreateEngineMaterial(shader);
            }

            if (m_Settings.fullResolutionComposite && m_CompositeMaterial == null)
            {
                var shader = Shader.Find(k_CompositeShader);
                if (shader != null)
                    m_CompositeMaterial = CoreUtils.CreateEngineMaterial(shader);
                else
                    Debug.LogWarning("[VolumetricClouds] Composite shader not found, falling back to compute upscale: " + k_CompositeShader);
            }

            ref var cameraData = ref renderingData.cameraData;
            var camera = cameraData.camera;
            var desc = cameraData.cameraTargetDescriptor;
            int finalW = desc.width;
            int finalH = desc.height;
            if (finalW <= 0 || finalH <= 0)
                return;

            // Resolution tier. The trace->intermediate ratio stays 2x (checkerboard) either way.
            //  Quarter: trace = 1/4, intermediate = 1/2  (upscale intermediate->full is 2x, cheapest)
            //  Half:    trace = 1/2, intermediate = full  (upscale is 1:1 -> no half-res edge shimmer)
            int intermediateW, intermediateH, traceW, traceH;
            var resolutionTier = m_Settings.resolutionTier;
            if (resolutionTier == VolumetricCloudsRenderFeature.ResolutionTier.Half)
            {
                intermediateW = finalW;
                intermediateH = finalH;
                traceW = Mathf.Max(1, Mathf.CeilToInt(finalW * 0.5f));
                traceH = Mathf.Max(1, Mathf.CeilToInt(finalH * 0.5f));
            }
            else
            {
                intermediateW = Mathf.Max(1, Mathf.CeilToInt(finalW * 0.5f));
                intermediateH = Mathf.Max(1, Mathf.CeilToInt(finalH * 0.5f));
                traceW = Mathf.Max(1, Mathf.CeilToInt(finalW * 0.25f));
                traceH = Mathf.Max(1, Mathf.CeilToInt(finalH * 0.25f));
            }

            var eff = m_Volume.GetEffectiveShape();

            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_Sampler))
            {
                BakeResourcesIfNeeded(cmd, eff);

                // Checkerboard min/max only applies together with the min/max ray-placement path.
                bool cbDepth = m_Settings.minMaxDepthRayPlacement && m_Settings.checkerboardMinMaxDepth;

                // Allocate transient RTs
                ReAlloc(ref m_TraceLighting, traceW, traceH, GraphicsFormat.R16G16B16A16_SFloat, true, "CloudTraceLighting");
                ReAlloc(ref m_TraceDepth, traceW, traceH, GraphicsFormat.R32_SFloat, true, "CloudTraceDepth");
                // Depth downsample target: single channel R32 in checkerboard (memory-saving) mode,
                // otherwise two channels (R = min/far, G = max/near).
                ReAlloc(ref m_HalfResDepth, intermediateW, intermediateH,
                    cbDepth ? GraphicsFormat.R32_SFloat : GraphicsFormat.R32G32_SFloat, false, "CloudHalfResDepth");
                ReAlloc(ref m_ColorCopy, finalW, finalH, GraphicsFormat.R16G16B16A16_SFloat, false, "CloudColorCopy");
                // The compute upscale writes to an intermediate RW target; the fragment composite writes
                // straight into the camera color, so it doesn't need one.
                bool useFragmentComposite = m_Settings.fullResolutionComposite && m_CompositeMaterial != null;
                if (!useFragmentComposite)
                    ReAlloc(ref m_UpscaleResult, finalW, finalH, GraphicsFormat.R16G16B16A16_SFloat, true, "CloudUpscaleResult");

                // Per-camera history
                var hist = GetHistory(cmd, camera.GetInstanceID(), intermediateW, intermediateH);
                int curIdx = hist.frameIndex & 1;
                int prevIdx = 1 - curIdx;

                // Matrices
                Matrix4x4 view = cameraData.GetViewMatrix();
                Matrix4x4 proj = cameraData.GetProjectionMatrix();
                Matrix4x4 gpuProj = GL.GetGPUProjectionMatrix(proj, true);
                Matrix4x4 vp = gpuProj * view;
                Matrix4x4 invVP = vp.inverse;
                if (!hist.valid) hist.prevVP = vp;

                UpdateWind();
                SetGlobalParams(cmd, cs, camera, finalW, finalH, intermediateW, intermediateH, traceW, traceH,
                    invVP, vp, hist.prevVP, hist.valid, hist.frameIndex, eff, ref renderingData);

                // 1) Depth downscale (fragment pass: only material shaders can read the global _CameraDepthTexture)
                cmd.SetGlobalVector(ID._CloudsFullResTexelSize, new Vector4(1f / finalW, 1f / finalH, finalW, finalH));
                m_DepthDownsampleMaterial.SetFloat(ID._CheckerboardDepthOutput, cbDepth ? 1f : 0f);
                CoreUtils.SetRenderTarget(cmd, m_HalfResDepth);
                CoreUtils.DrawFullScreen(cmd, m_DepthDownsampleMaterial);

                // 2) Ray march at trace res
                int kRender = cs.FindKernel("RenderClouds");

                // Prefer user-assigned textures, otherwise fall back to the runtime-generated ones.
                Texture shapeTex = m_Settings.shapeNoiseTexture != null ? m_Settings.shapeNoiseTexture : m_ShapeNoise;
                Texture erosionTex = m_Settings.erosionNoiseTexture != null ? m_Settings.erosionNoiseTexture : m_ErosionNoise;
                Texture mapTex = m_Volume.cloudMap.value != null ? m_Volume.cloudMap.value : m_CloudMap;
                Texture lutTex = m_Volume.cloudLut.value != null ? m_Volume.cloudLut.value : m_CloudLut;

                cmd.SetComputeTextureParam(cs, kRender, ID._Worley128RGBA, shapeTex);
                cmd.SetComputeTextureParam(cs, kRender, ID._ErosionNoise, erosionTex);
                cmd.SetComputeTextureParam(cs, kRender, ID._CloudMapTexture, mapTex);
                cmd.SetComputeTextureParam(cs, kRender, ID._CloudLutTexture, lutTex);

                // Blue noise (only if all three textures are provided)
                bool useBlueNoise = m_Settings.owenScrambledTexture != null && m_Settings.scramblingTileXSPP != null && m_Settings.rankingTileXSPP != null;
                cmd.SetComputeIntParam(cs, ID._UseBlueNoise, useBlueNoise ? 1 : 0);
                cmd.SetComputeTextureParam(cs, kRender, ID._OwenScrambledTexture, useBlueNoise ? m_Settings.owenScrambledTexture : Texture2D.blackTexture);
                cmd.SetComputeTextureParam(cs, kRender, ID._ScramblingTileXSPP, useBlueNoise ? m_Settings.scramblingTileXSPP : Texture2D.blackTexture);
                cmd.SetComputeTextureParam(cs, kRender, ID._RankingTileXSPP, useBlueNoise ? m_Settings.rankingTileXSPP : Texture2D.blackTexture);

                cmd.SetComputeTextureParam(cs, kRender, ID._CloudsLightingTextureRW, m_TraceLighting);
                cmd.SetComputeTextureParam(cs, kRender, ID._CloudsDepthTextureRW, m_TraceDepth);
                cmd.SetComputeTextureParam(cs, kRender, ID._HalfResDepthBuffer, m_HalfResDepth);
                cmd.DispatchCompute(cs, kRender, DivRoundUp(traceW, 8), DivRoundUp(traceH, 8), 1);

                // 3) Temporal reprojection at intermediate res. Optionally skipped (debug): the raw
                //    second-pass (RenderClouds) trace is fed straight into the final composite, so the
                //    composite reads the trace textures at trace resolution instead of the history buffers.
                bool skipReproject = m_Settings.skipTemporalReprojection;
                if (!skipReproject)
                {
                    int kReproject = cs.FindKernel("ReprojectClouds");
                    cmd.SetComputeTextureParam(cs, kReproject, ID._CloudsLightingTexture, m_TraceLighting);
                    cmd.SetComputeTextureParam(cs, kReproject, ID._CloudsDepthTexture, m_TraceDepth);
                    cmd.SetComputeTextureParam(cs, kReproject, ID._HalfResDepthBuffer, m_HalfResDepth);
                    cmd.SetComputeTextureParam(cs, kReproject, ID._HistoryVolumetricClouds0Texture, hist.history0[prevIdx]);
                    cmd.SetComputeTextureParam(cs, kReproject, ID._HistoryVolumetricClouds1Texture, hist.history1[prevIdx]);
                    cmd.SetComputeTextureParam(cs, kReproject, ID._CloudsLightingTextureRW, hist.history0[curIdx]);
                    cmd.SetComputeTextureParam(cs, kReproject, ID._CloudsAdditionalTextureRW, hist.history1[curIdx]);
                    cmd.DispatchCompute(cs, kReproject, DivRoundUp(intermediateW, 8), DivRoundUp(intermediateH, 8), 1);
                }

                // Composite source: reprojected history (intermediate res) or raw trace (trace res).
                var srcLighting = skipReproject ? m_TraceLighting : hist.history0[curIdx];
                var srcStatus   = skipReproject ? m_TraceDepth   : hist.history1[curIdx];
                int srcW = skipReproject ? traceW : intermediateW;
                int srcH = skipReproject ? traceH : intermediateH;

                // 4) Upscale + composite. Copy the current camera color out first so we can read it.
                var cameraColor = cameraData.renderer.cameraColorTargetHandle;
                Blitter.BlitCameraTexture(cmd, cameraColor, m_ColorCopy);

                if (useFragmentComposite)
                {
                    // Full-resolution, depth-aware composite: reads the global full-res _CameraDepthTexture
                    // (compute can't) so the half-res cloud aligns to geometry edges. Cheap (one fullscreen
                    // pass) and much steadier on high-frequency silhouettes than a plain half-res upscale.
                    m_CompositeMaterial.SetTexture(ID._VolumetricCloudsTexture, srcLighting);
                    m_CompositeMaterial.SetTexture(ID._DepthStatusTexture, srcStatus);
                    m_CompositeMaterial.SetTexture(ID._CameraColorCopy, m_ColorCopy);
                    m_CompositeMaterial.SetTexture(ID._OwenScrambledTexture, useBlueNoise ? m_Settings.owenScrambledTexture : Texture2D.blackTexture);
                    m_CompositeMaterial.SetTexture(ID._ScramblingTileXSPP, useBlueNoise ? m_Settings.scramblingTileXSPP : Texture2D.blackTexture);
                    m_CompositeMaterial.SetTexture(ID._RankingTileXSPP, useBlueNoise ? m_Settings.rankingTileXSPP : Texture2D.blackTexture);
                    m_CompositeMaterial.SetVector(ID._FinalScreenSize, new Vector4(finalW, finalH, 1f / finalW, 1f / finalH));
                    m_CompositeMaterial.SetVector(ID._IntermediateScreenSize, new Vector4(srcW, srcH, 1f / srcW, 1f / srcH));
                    m_CompositeMaterial.SetVector(ID._CloudsZBufferParams, GetZBufferParams(camera));
                    m_CompositeMaterial.SetVector(ID._CloudFogColor, (Vector4)m_Volume.fogColor.value.linear);
                    m_CompositeMaterial.SetFloat(ID._CloudFogDistance, m_Volume.fogDistance.value);
                    m_CompositeMaterial.SetFloat(ID._MaxCloudDistance, m_MaxCloudDistance);
                    m_CompositeMaterial.SetFloat(ID._ImprovedTransmittanceBlend, m_Volume.perceptualBlending.value);
                    m_CompositeMaterial.SetFloat(ID._LocalCloudsF, m_Volume.localClouds.value ? 1f : 0f);
                    m_CompositeMaterial.SetFloat(ID._CompositeSkipDepthWeight, skipReproject ? 1f : 0f);
                    m_CompositeMaterial.SetInt(ID._UseBlueNoise, useBlueNoise ? 1 : 0);
                    m_CompositeMaterial.SetInt(ID._UseBlueNoiseUpscale, m_Settings.blueNoiseUpscale ? 1 : 0);
                    m_CompositeMaterial.SetInt(ID._AccumulationFrameIndex, hist.frameIndex);

                    CoreUtils.SetRenderTarget(cmd, cameraColor);
                    CoreUtils.DrawFullScreen(cmd, m_CompositeMaterial);
                }
                else
                {
                    int kUpscale = cs.FindKernel("UpscaleAndCombine");
                    cmd.SetComputeVectorParam(cs, ID._UpscaleSourceSize, new Vector4(srcW, srcH, 1f / srcW, 1f / srcH));
                    cmd.SetComputeFloatParam(cs, ID._SkipReprojection, skipReproject ? 1f : 0f);
                    cmd.SetComputeTextureParam(cs, kUpscale, ID._VolumetricCloudsTexture, srcLighting);
                    cmd.SetComputeTextureParam(cs, kUpscale, ID._DepthStatusTexture, srcStatus);
                    cmd.SetComputeTextureParam(cs, kUpscale, ID._HalfResDepthBuffer, m_HalfResDepth);
                    cmd.SetComputeTextureParam(cs, kUpscale, ID._CameraColorCopy, m_ColorCopy);
                    cmd.SetComputeTextureParam(cs, kUpscale, ID._VolumetricCloudsUpscaleTextureRW, m_UpscaleResult);
                    cmd.DispatchCompute(cs, kUpscale, DivRoundUp(finalW, 8), DivRoundUp(finalH, 8), 1);

                    Blitter.BlitCameraTexture(cmd, m_UpscaleResult, cameraColor);
                }

                // Advance history
                hist.prevVP = vp;
                hist.valid = true;
                hist.frameIndex++;
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        //------------------------------------------------------------------------------------------
        // Parameter upload
        //------------------------------------------------------------------------------------------
        void SetGlobalParams(CommandBuffer cmd, ComputeShader cs, Camera camera,
            int finalW, int finalH, int interW, int interH, int traceW, int traceH,
            Matrix4x4 invVP, Matrix4x4 vp, Matrix4x4 prevVP, bool historyValid, int frameIndex,
            in VolumetricCloudsVolume.EffectiveShape eff, ref RenderingData renderingData)
        {
            // Clamp to sane minimums: a zero altitude range makes the cloud-shell thickness 0, which
            // divides by zero in EvaluateNormalizedCloudHeight (NaN) when the slider is dragged to 0.
            float lowestAlt = Mathf.Max(1f, eff.bottomAltitude);
            float highestAlt = lowestAlt + Mathf.Max(1f, eff.altitudeRange);
            float earthR = k_EarthRadius;

            cmd.SetComputeFloatParam(cs, ID._LowestCloudAltitude, lowestAlt);
            cmd.SetComputeFloatParam(cs, ID._HighestCloudAltitude, highestAlt);
            cmd.SetComputeFloatParam(cs, ID._EarthRadius, earthR);
            cmd.SetComputeVectorParam(cs, ID._CloudRangeSquared, new Vector4(Sqr(lowestAlt + earthR), Sqr(highestAlt + earthR), 0, 0));

            float midRadius = (lowestAlt + highestAlt) * 0.5f;
            float normFactor = Mathf.Sqrt(Sqr(earthR + midRadius) - earthR * earthR);
            cmd.SetComputeFloatParam(cs, ID._NormalizationFactor, normFactor);

            int numPrimarySteps = m_Volume.numPrimarySteps.value;
            float maxRayMarch = Mathf.Min(eff.altitudeRange / 8f * numPrimarySteps, camera.farClipPlane);
            cmd.SetComputeFloatParam(cs, ID._MaxRayMarchingDistance, maxRayMarch);
            float absoluteHighest = highestAlt + earthR;
            m_MaxCloudDistance = Mathf.Sqrt(absoluteHighest * absoluteHighest - earthR * earthR);
            cmd.SetComputeFloatParam(cs, ID._MaxCloudDistance, m_MaxCloudDistance);

            cmd.SetComputeIntParam(cs, ID._NumPrimarySteps, numPrimarySteps);
            cmd.SetComputeIntParam(cs, ID._NumLightSteps, m_Volume.numLightSteps.value);

            float cloudMapWorldSize = m_Volume.cloudMapWorldSize.value > 0f ? m_Volume.cloudMapWorldSize.value : normFactor;
            Vector2 cloudMapOffset = m_Volume.cloudMapWorldOffset.value;
            float cloudMapScale = normFactor / Mathf.Max(cloudMapWorldSize, 1f);
            cmd.SetComputeVectorParam(cs, ID._CloudMapTiling, new Vector4(cloudMapScale, cloudMapScale,
                1f - cloudMapOffset.x / Mathf.Max(cloudMapWorldSize, 1f),
                1f - cloudMapOffset.y / Mathf.Max(cloudMapWorldSize, 1f)));

            float theta = m_Volume.orientation.value / 180f * Mathf.PI;
            Vector3 shapeOffset = m_Volume.shapeOffset.value;
            cmd.SetComputeVectorParam(cs, ID._WindDirection, new Vector4(-Mathf.Cos(theta), -Mathf.Sin(theta), 0, 0));
            cmd.SetComputeVectorParam(cs, ID._WindVector, new Vector4(m_CloudOffset.x, m_CloudOffset.y, 0, 0));
            // HDRP splits the world-space shape offset into horizontal (xz) and vertical (y) parts.
            cmd.SetComputeVectorParam(cs, ID._ShapeNoiseOffset, new Vector4(shapeOffset.x, shapeOffset.z, 0, 0));
            cmd.SetComputeFloatParam(cs, ID._VerticalShapeNoiseOffset, shapeOffset.y);
            cmd.SetComputeFloatParam(cs, ID._VerticalShapeWindDisplacement, m_VerticalShapeOffset);
            cmd.SetComputeFloatParam(cs, ID._VerticalErosionWindDisplacement, m_VerticalErosionOffset);
            cmd.SetComputeFloatParam(cs, ID._LargeWindSpeed, m_Volume.cloudMapSpeedMultiplier.value);
            cmd.SetComputeFloatParam(cs, ID._MediumWindSpeed, m_Volume.shapeSpeedMultiplier.value);
            cmd.SetComputeFloatParam(cs, ID._SmallWindSpeed, m_Volume.erosionSpeedMultiplier.value);
            cmd.SetComputeFloatParam(cs, ID._AltitudeDistortion, m_Volume.altitudeDistortion.value * 0.25f);

            // Lighting
            Vector3 sunDir = Vector3.up;
            Color sunColor = Color.black;
            int mainIdx = renderingData.lightData.mainLightIndex;
            if (mainIdx >= 0 && mainIdx < renderingData.lightData.visibleLights.Length)
            {
                var vl = renderingData.lightData.visibleLights[mainIdx];
                Vector3 fwd = vl.localToWorldMatrix.GetColumn(2);
                sunDir = (-fwd).normalized;
                sunColor = vl.finalColor;
            }
          
            cmd.SetComputeVectorParam(cs, ID._SunDirection, new Vector4(sunDir.x, sunDir.y, sunDir.z, 0));
            cmd.SetComputeVectorParam(cs, ID._SunLightColor, (Vector4)(sunColor * m_Volume.sunLightDimmer.value));
           
            cmd.SetComputeFloatParam(cs, ID._MultiScattering, 1f - m_Volume.multiScattering.value * 0.95f);
            cmd.SetComputeFloatParam(cs, ID._ErosionOcclusion, m_Volume.erosionOcclusion.value);
            cmd.SetComputeFloatParam(cs, ID._PowderEffectIntensity, m_Volume.powderEffectIntensity.value);
            Color scatter = Color.white - m_Volume.scatteringTint.value * 0.75f;
            scatter = sunColor.NormalizeRGB();
            scatter = Color.white - Color.white * 0.75f;
            cmd.SetComputeVectorParam(cs, ID._ScatteringTint, (Vector4)scatter);

            Color ambTop = RenderSettings.ambientSkyColor.linear;
            Color ambBottom = RenderSettings.ambientGroundColor.linear;
            if (ambTop.maxColorComponent < 0.001f && ambBottom.maxColorComponent < 0.001f)
            {
                ambTop = new Color(0.5f, 0.6f, 0.75f);
                ambBottom = new Color(0.2f, 0.2f, 0.25f);
            }
            cmd.SetComputeVectorParam(cs, ID._CloudAmbientTop, (Vector4)(ambTop * m_Volume.ambientLightProbeDimmer.value));
            cmd.SetComputeVectorParam(cs, ID._CloudAmbientBottom, (Vector4)(ambBottom * m_Volume.ambientLightProbeDimmer.value));

            // Modeling
            cmd.SetComputeFloatParam(cs, ID._DensityMultiplier, eff.densityMultiplier * eff.densityMultiplier * 2f);
            cmd.SetComputeFloatParam(cs, ID._ShapeFactor, eff.shapeFactor);
            cmd.SetComputeFloatParam(cs, ID._ErosionFactor, eff.erosionFactor);
            cmd.SetComputeFloatParam(cs, ID._ShapeScale, eff.shapeScale);
            cmd.SetComputeFloatParam(cs, ID._ErosionScale, eff.erosionScale);
            cmd.SetComputeFloatParam(cs, ID._ErosionFactorCompensation, 0.75f);

            // Temporal
            cmd.SetComputeFloatParam(cs, ID._TemporalAccumulationFactor, m_Volume.temporalAccumulationFactor.value);
            cmd.SetComputeFloatParam(cs, ID._CloudHistoryInvalidation, historyValid ? 1f : 0f);
            cmd.SetComputeIntParam(cs, ID._AccumulationFrameIndex, frameIndex);
            cmd.SetComputeIntParam(cs, ID._SubPixelIndex, frameIndex % 4);
            cmd.SetComputeIntParam(cs, ID._EnableIntegration, 1);
            cmd.SetComputeIntParam(cs, ID._LowResolutionEvaluation, 1);

            // Fade
            float fadeStart, fadeDist;
            if (m_Volume.fadeInMode.value == VolumetricCloudsVolume.CloudFadeInMode.Manual)
            {
                fadeStart = m_Volume.fadeInStart.value;
                fadeDist = Mathf.Max(m_Volume.fadeInDistance.value, 1f);
            }
            else
            {
                fadeStart = Mathf.Max((highestAlt - lowestAlt) * 0.2f, camera.nearClipPlane);
                fadeDist = (highestAlt - lowestAlt) * 0.3f;
            }
            cmd.SetComputeFloatParam(cs, ID._FadeInStart, fadeStart);
            cmd.SetComputeFloatParam(cs, ID._FadeInDistance, fadeDist);

            // Resolutions
            cmd.SetComputeVectorParam(cs, ID._FinalScreenSize, new Vector4(finalW, finalH, 1f / finalW, 1f / finalH));
            cmd.SetComputeVectorParam(cs, ID._IntermediateScreenSize, new Vector4(interW, interH, 1f / interW, 1f / interH));
            cmd.SetComputeVectorParam(cs, ID._TraceScreenSize, new Vector4(traceW, traceH, 1f / traceW, 1f / traceH));
            // _HalfResDepthBuffer is always at intermediate resolution; the upscale source size is set
            // separately per-frame (equals intermediate normally, or trace when skipping reprojection).
            cmd.SetComputeVectorParam(cs, ID._HalfResDepthSize, new Vector4(interW, interH, 1f / interW, 1f / interH));
            cmd.SetComputeVectorParam(cs, ID._HistoryViewportSize, new Vector4(interW, interH, 0, 0));
            cmd.SetComputeVectorParam(cs, ID._HistoryBufferSize, new Vector4(interW, interH, 0, 0));

            // Camera
            cmd.SetComputeVectorParam(cs, ID._CloudsCameraPosWS, camera.transform.position);
            cmd.SetComputeMatrixParam(cs, ID._CloudsInvVP, invVP);
            cmd.SetComputeMatrixParam(cs, ID._CloudsVP, vp);
            cmd.SetComputeMatrixParam(cs, ID._CloudsPrevVP, prevVP);

            // Compositing
            cmd.SetComputeFloatParam(cs, ID._ImprovedTransmittanceBlend, m_Volume.perceptualBlending.value);
            cmd.SetComputeVectorParam(cs, ID._CloudFogColor, (Vector4)m_Volume.fogColor.value.linear);
            cmd.SetComputeFloatParam(cs, ID._CloudFogDistance, m_Volume.fogDistance.value);

            // Local vs distant
            cmd.SetComputeIntParam(cs, ID._LocalClouds, m_Volume.localClouds.value ? 1 : 0);
            cmd.SetComputeFloatParam(cs, ID._CloudNearPlane, camera.nearClipPlane);
            cmd.SetComputeFloatParam(cs, ID._CloudFarPlane, camera.farClipPlane);

            // RDR2-style reconstruction quality toggles (0 = old logic).
            cmd.SetComputeIntParam(cs, ID._UseDepthRejection, m_Settings.neighborhoodDepthRejection ? 1 : 0);
            cmd.SetComputeIntParam(cs, ID._UseMinMaxDepth, m_Settings.minMaxDepthRayPlacement ? 1 : 0);
            cmd.SetComputeIntParam(cs, ID._UseBlueNoiseUpscale, m_Settings.blueNoiseUpscale ? 1 : 0);
            cmd.SetComputeFloatParam(cs, ID._DepthRejectionThreshold, m_Settings.depthRejectionThreshold);
            cmd.SetComputeFloatParam(cs, ID._RayPlacementDepthThreshold, m_Settings.rayPlacementDepthThreshold);
            // Checkerboard depth only applies alongside the min/max ray-placement path (and drives the R32 RT).
            bool cbDepth = m_Settings.minMaxDepthRayPlacement && m_Settings.checkerboardMinMaxDepth;
            cmd.SetComputeIntParam(cs, ID._CheckerboardMinMaxDepth, cbDepth ? 1 : 0);
            cmd.SetComputeVectorParam(cs, ID._CloudsZBufferParams, GetZBufferParams(camera));
        }

        // Rebuilds Unity's _ZBufferParams so LinearEyeDepth() works inside the compute shader
        // (per-camera constant buffers are not visible to standalone compute dispatches).
        static Vector4 GetZBufferParams(Camera camera)
        {
            float n = camera.nearClipPlane;
            float f = camera.farClipPlane;
            float fn = f / n;
            Vector4 z = new Vector4(1f - fn, fn, (1f - fn) / f, fn / f);
            if (SystemInfo.usesReversedZBuffer)
            {
                z.x = -1f + fn;
                z.y = 1f;
                z.z = z.x / f;
                z.w = 1f / f;
            }
            return z;
        }

        void UpdateWind()
        {
            float now = Time.time;
            if (m_LastTime < 0f) { m_LastTime = now; return; }
            float dt = now - m_LastTime;
            m_LastTime = now;

            float theta = m_Volume.orientation.value / 180f * Mathf.PI;
            Vector2 windDir = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
            // km/h -> m/s (0.277778). Minus to move in the right direction.
            m_CloudOffset += 0.277778f * dt * m_Volume.globalWindSpeed.value * -windDir;

            // Vertical wind displacement for the shape / erosion noise (HDRP verticalXWindSpeed).
            m_VerticalShapeOffset += dt * m_Volume.verticalShapeWindSpeed.value;
            m_VerticalErosionOffset += dt * m_Volume.verticalErosionWindSpeed.value;
        }

        //------------------------------------------------------------------------------------------
        // History management
        //------------------------------------------------------------------------------------------
        CamHistory GetHistory(CommandBuffer cmd, int camId, int w, int h)
        {
            if (!m_Histories.TryGetValue(camId, out var hist))
            {
                hist = new CamHistory();
                m_Histories.Add(camId, hist);
            }

            bool resized = hist.width != w || hist.height != h || hist.history0[0] == null;
            if (resized)
            {
                for (int i = 0; i < 2; i++)
                {
                    ReAlloc(ref hist.history0[i], w, h, GraphicsFormat.R16G16B16A16_SFloat, true, "CloudHistory0_" + i);
                    ReAlloc(ref hist.history1[i], w, h, GraphicsFormat.R16G16B16A16_SFloat, true, "CloudHistory1_" + i);
                    cmd.SetRenderTarget(hist.history0[i]);
                    cmd.ClearRenderTarget(false, true, Color.clear);
                    cmd.SetRenderTarget(hist.history1[i]);
                    cmd.ClearRenderTarget(false, true, Color.clear);
                }
                hist.width = w; hist.height = h;
                hist.valid = false;
                hist.frameIndex = 0;
            }
            return hist;
        }

        //------------------------------------------------------------------------------------------
        // Helpers
        //------------------------------------------------------------------------------------------
        static int DivRoundUp(int x, int div) => (x + div - 1) / div;
        static float Sqr(float x) => x * x;

        // GLSL-style smoothstep(edge0, edge1, x) -> 0..1 (Unity's Mathf.SmoothStep interpolates
        // between edge0 and edge1 instead, which is NOT what we want for building the LUT/map).
        static float SmoothStep01(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        static void ReAlloc(ref RTHandle handle, int w, int h, GraphicsFormat fmt, bool randomWrite, string name)
        {
            if (handle != null && handle.rt != null && handle.rt.width == w && handle.rt.height == h
                && handle.rt.graphicsFormat == fmt)
                return;
            handle?.Release();
            handle = RTHandles.Alloc(w, h, 1,
                colorFormat: fmt,
                filterMode: FilterMode.Bilinear,
                wrapMode: TextureWrapMode.Clamp,
                dimension: TextureDimension.Tex2D,
                enableRandomWrite: randomWrite,
                name: name);
        }

        public void Dispose()
        {
            m_TraceLighting?.Release();
            m_TraceDepth?.Release();
            m_HalfResDepth?.Release();
            m_ColorCopy?.Release();
            m_UpscaleResult?.Release();
            m_TraceLighting = m_TraceDepth = m_HalfResDepth = m_ColorCopy = m_UpscaleResult = null;

            foreach (var kv in m_Histories)
            {
                for (int i = 0; i < 2; i++)
                {
                    kv.Value.history0[i]?.Release();
                    kv.Value.history1[i]?.Release();
                }
            }
            m_Histories.Clear();

            if (m_ShapeNoise != null) { m_ShapeNoise.Release(); m_ShapeNoise = null; }
            if (m_ErosionNoise != null) { m_ErosionNoise.Release(); m_ErosionNoise = null; }
            if (m_CloudMap != null) { Object.DestroyImmediate(m_CloudMap); m_CloudMap = null; }
            if (m_CloudLut != null) { Object.DestroyImmediate(m_CloudLut); m_CloudLut = null; }
            if (m_DepthDownsampleMaterial != null) { CoreUtils.Destroy(m_DepthDownsampleMaterial); m_DepthDownsampleMaterial = null; }
            if (m_CompositeMaterial != null) { CoreUtils.Destroy(m_CompositeMaterial); m_CompositeMaterial = null; }
        }
    }
}
