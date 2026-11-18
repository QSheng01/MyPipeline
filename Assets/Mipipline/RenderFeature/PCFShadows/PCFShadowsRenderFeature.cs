using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PCFShadowsRenderPassFeature : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }

    CustomRenderPass m_ScriptablePass;
    [Serializable]
    public class Settings
    {
        [Header("全局模糊系数。")]
        [Range(0f, 0.25f)]
        public float NGSS_SHADOWS_SOFTNESS = 0.15f;

        [Header("级联模糊系数。")]
        [Range(0f, 1.0f)]
        public float NGSS_CASCADES_SOFTNESS_NORMALIZATION = 1f;

        // [Tooltip("Bais")]
        // [Range(0.0001f, 0.5f)]
        // public float NGSS_BAIS = 0.2f;

        [Header("模糊精确度,步骤二，正式执行模糊。")]
        [Range(4, 32)]
        public float NGSS_FILTER_SAMPLERS_DIR = 8.0f;

        [Header("噪点图。")]
        public Texture2D NGSS_NOISE_TEXTURE;

        public float NoiseTile = 1.0f;

        [Header("噪点图影响。")]
        [Range(0, 1)]
        public float NGSS_NOISE_TO_DITHERING_SCALE_DIR;
    }

    [SerializeField]
    public Settings m_Settings;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass();

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

        float shadowCascades = 4.0f; // shadowCascades

        if (shadowCascades > 1.0f)
        {
            Shader.SetGlobalFloat("NGSS_CASCADES_SOFTNESS_NORMALIZATION", m_Settings.NGSS_CASCADES_SOFTNESS_NORMALIZATION);
            Shader.SetGlobalFloat("NGSS_CASCADES_COUNT", shadowCascades);
            Shader.SetGlobalVector("NGSS_CASCADES_SPLITS", shadowCascades == 2 ? new Vector4(QualitySettings.shadowCascade2Split, 1f, 1f, 1f) : new Vector4(QualitySettings.shadowCascade4Split.x, QualitySettings.shadowCascade4Split.y, QualitySettings.shadowCascade4Split.z, 1f));
        }

        // Shader.SetGlobalFloat("NGSS_TEST_SAMPLERS_DIR", m_Settings.NGSS_SAMPLING_TEST);
        Shader.SetGlobalFloat("NGSS_FILTER_SAMPLERS_DIR", m_Settings.NGSS_FILTER_SAMPLERS_DIR);

        //Noise
        Shader.SetGlobalFloat("NGSS_NOISE_TO_DITHERING_SCALE_DIR", m_Settings.NGSS_NOISE_TO_DITHERING_SCALE_DIR);

        //Scale global softness over distance (to maintain the similar softness when texel size changes
        Shader.SetGlobalFloat("NGSS_GLOBAL_SOFTNESS", QualitySettings.shadowProjection == ShadowProjection.CloseFit ? m_Settings.NGSS_SHADOWS_SOFTNESS : m_Settings.NGSS_SHADOWS_SOFTNESS * 2.0f / (QualitySettings.shadowDistance * 0.66f) * (shadowCascades == 2.0f ? 1.5f : shadowCascades == 4.0f ? 1.0f : 0.25f));

        if (m_Settings.NGSS_NOISE_TEXTURE == null) { m_Settings.NGSS_NOISE_TEXTURE = Resources.Load<Texture2D>("BlueNoise_R8_8"); }
        Shader.SetGlobalTexture("_BlueNoiseTextureDir", m_Settings.NGSS_NOISE_TEXTURE);


        Shader.SetGlobalVector("_BlueNoiseTextureDir_TexelSize", new Vector4(m_Settings.NoiseTile, m_Settings.NoiseTile, 0, 0));


        Shader.SetGlobalVector("_CustomPCFBias", Vector4.zero);
        // if (NGSS_CASCADES_BLENDING && shadowCascades > 1) { Shader.EnableKeyword("NGSS_USE_CASCADE_BLENDING"); Shader.SetGlobalFloat("NGSS_CASCADE_BLEND_DISTANCE", NGSS_CASCADES_BLENDING_VALUE * 0.125f); } else { Shader.DisableKeyword("NGSS_USE_CASCADE_BLENDING"); }
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


