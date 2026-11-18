using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Baozang.Rendering.VolumetricClouds
{
    /// <summary>
    /// URP port of the HDRP volumetric clouds.
    /// Compute-based ray marching with a multi-resolution + temporal reprojection pipeline
    /// (1/4 res checkerboard trace -> 1/2 res temporal accumulation -> full res upscale/composite).
    ///
    /// All authoring parameters live on the <see cref="VolumetricCloudsVolume"/> post-process
    /// (Volume Override) component and are read from the volume stack each frame. This feature only
    /// holds the GPU resources (compute shaders / noise / blue-noise textures) and render scheduling.
    /// </summary>
    [DisallowMultipleRendererFeature("Volumetric Clouds")]
    public class VolumetricCloudsRenderFeature : ScriptableRendererFeature
    {
        /// <summary>
        /// Trace/intermediate resolution tier. The trace->intermediate ratio is always 2x (checkerboard),
        /// only the base scale changes. Higher tiers cost more but reduce half-res upsample edge shimmer
        /// (e.g. alpha-test foliage inside local clouds).
        /// </summary>
        public enum ResolutionTier
        {
            [InspectorName("Quarter (1/4 trace, 1/2 intermediate) - Fastest")]
            Quarter,
            [InspectorName("Half (1/2 trace, full intermediate) - Clearer")]
            Half,
        }

        [Serializable]
        public class Settings
        {
            [Header("Enable")]
            [Tooltip("Enable this render feature. Per-camera cloud visibility is controlled by the Volume Enable option.")]
            public bool enabled = true;
            public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

            [Header("Resolution")]
            [Tooltip("Ray marching / reprojection resolution tier. Quarter = 1/4 trace + 1/2 intermediate. Half = 1/2 trace + full intermediate.")]
            public ResolutionTier resolutionTier = ResolutionTier.Quarter;

            [Tooltip("Full-resolution depth-aware composite. The final fragment pass reads full-resolution scene depth and upscales the cloud texture with depth guidance.")]
            public bool fullResolutionComposite = true;

            [Tooltip("Debug: skip the third pass (temporal reprojection) and feed the raw second-pass (RenderClouds) trace directly into the final composite. No temporal accumulation/denoise, so expect checkerboard shimmer, noise and trace-resolution detail. Use it to compare what reprojection contributes or to isolate reprojection issues.")]
            public bool skipTemporalReprojection = false;

            [Header("Reconstruction Quality (RDR2-style)")]
            [Tooltip("F1: Reject temporal neighborhood samples by scene depth difference to reduce motion ghosting.")]
            public bool neighborhoodDepthRejection = true;
            [Range(0.02f, 1f)]
            [Tooltip("F1: Relative linear depth threshold for rejecting neighborhood samples.")]
            public float depthRejectionThreshold = 0.1f;
            [Tooltip("F2: Use 2x2 tile min/max depth neighborhood analysis to drive checkerboard ray placement.")]
            public bool minMaxDepthRayPlacement = true;
            [Range(0.05f, 1f)]
            [Tooltip("F2: Relative linear depth threshold used to decide whether near/far tile depths are covered by neighboring rays.")]
            public float rayPlacementDepthThreshold = 0.25f;
            [Tooltip("F2 variant: single-channel checkerboard min/max depth. Requires Min Max Depth Ray Placement.")]
            public bool checkerboardMinMaxDepth = false;
            [Tooltip("F3: Use blue-noise jitter during full-resolution upscale.")]
            public bool blueNoiseUpscale = true;

            [Header("Resources")]
            public ComputeShader volumetricCloudsCS;
            public ComputeShader noiseGenCS;
            [Tooltip("Optional. If empty, Shader.Find is used at runtime. Assign manually or add to Always Included Shaders for builds.")]
            public Shader depthDownsampleShader;

            [Header("Textures (Optional; generated at runtime if empty)")]
            [Tooltip("Shape noise 3D texture, equivalent to HDRP _Worley128RGBA.")]
            public Texture shapeNoiseTexture;
            [Tooltip("Erosion noise 3D texture, equivalent to HDRP _ErosionNoise.")]
            public Texture erosionNoiseTexture;

            [Header("Blue Noise (Optional; all three textures required)")]
            [Tooltip("HDRP _OwenScrambledTexture")]
            public Texture owenScrambledTexture;
            [Tooltip("HDRP _ScramblingTileXSPP")]
            public Texture scramblingTileXSPP;
            [Tooltip("HDRP _RankingTileXSPP")]
            public Texture rankingTileXSPP;
        }

        public Settings settings = new Settings();
        private VolumetricCloudsRenderPass m_Pass;

        public override void Create()
        {
            // URP calls Create() again every time a setting changes in the inspector. Release the
            // previous pass' GPU resources first, otherwise every tweak leaks textures/history RTs
            // (tens of MB each) and rapidly exhausts GPU memory -> editor crash.
            m_Pass?.Dispose();
            m_Pass = new VolumetricCloudsRenderPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!settings.enabled)
                return;
            if (settings.volumetricCloudsCS == null || settings.noiseGenCS == null)
                return;
            // Only render for game/scene cameras.
            var camType = renderingData.cameraData.cameraType;
            if (camType != CameraType.Game && camType != CameraType.SceneView)
                return;

            m_Pass.renderPassEvent = settings.renderPassEvent;
            m_Pass.ConfigureInput(ScriptableRenderPassInput.Depth);
            renderer.EnqueuePass(m_Pass);
        }

        protected override void Dispose(bool disposing)
        {
            m_Pass?.Dispose();
            m_Pass = null;
        }
    }
}
