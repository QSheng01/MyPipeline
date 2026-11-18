using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SYPackages.PostProcessing.OilPainting
{
    [Serializable]
    class OilPaintingSettings
    {
        [Range(5f, 20f)]
        public int binCount = 20;
        [Range(0f, 5f)]
        public float radius = 2;
        [Range(0f, 1000)]
        public float farStart = 1f;
        [Range(0f, 1000)]
        public float farEnd = 50f;

        public Quality quality;

        public enum Quality
        {
            Low,
            High,
        }
    }

    public class OilPaintingRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] OilPaintingSettings m_Settings = new();

        [SerializeField] [HideInInspector] [Reload("PostProcessing/OilPainting/OilPainting.shader")]
        Shader m_Shader;

        OilPaintingPass m_OilPaintingPass;
        Material m_Material;

        public override void Create()
        {
#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, "Assets/SYPackages");
#endif
            m_OilPaintingPass ??= new OilPaintingPass();
            m_OilPaintingPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.postProcessEnabled == false)
                return;
            var cameraType = renderingData.cameraData.cameraType;
            if ((cameraType & (CameraType.Game | CameraType.SceneView)) == 0)
                return;

            var mat = GetMaterial();
            if (mat == null)
            {
                Debug.LogErrorFormat(
                    $"{nameof(OilPaintingRendererFeature)}.AddRenderPasses: Missing material. {name} render pass will not be added.");
            }

            if (m_OilPaintingPass.Setup(m_Material, m_Settings))
                renderer.EnqueuePass(m_OilPaintingPass);
        }

        Material GetMaterial()
        {
            if (m_Material == null && m_Shader != null)
                m_Material = CoreUtils.CreateEngineMaterial(m_Shader);
            return m_Material;
        }

        protected override void Dispose(bool disposing)
        {
            m_OilPaintingPass?.Dispose();
            m_OilPaintingPass = null;
            CoreUtils.Destroy(m_Material);
        }

        class OilPaintingPass : ScriptableRenderPass
        {
            Material m_Material;
            OilPaintingSettings m_Settings;

            RTHandle m_OilPaintingStepTarget;
            RTHandle m_CameraTarget;

            static readonly int s_TexelSize = Shader.PropertyToID("_TexelSize");
            static readonly int s_Radius = Shader.PropertyToID("_Radius");
            static readonly int s_IntensityBinCount = Shader.PropertyToID("_IntensityBinCount");
            static readonly int s_Step = Shader.PropertyToID("_Step");
            static readonly int s_FarStart = Shader.PropertyToID("_FarStart");
            static readonly int s_FarEnd = Shader.PropertyToID("_FarEnd");

            public OilPaintingPass()
            {
                profilingSampler = new ProfilingSampler(nameof(OilPaintingPass));
            }
            public bool Setup(Material material, OilPaintingSettings settings)
            {
                m_Material = material;
                m_Settings = settings;
                return settings.radius > 0;
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
                descriptor.msaaSamples = 1;
                descriptor.depthBufferBits = 0;

                if (m_Settings.quality == OilPaintingSettings.Quality.Low)
                    RenderingUtils.ReAllocateIfNeeded(ref m_OilPaintingStepTarget, descriptor, FilterMode.Bilinear,
                        TextureWrapMode.Clamp,
                        name: "OilPaintingStepTarget");
                else
                    m_OilPaintingStepTarget?.Release();

                m_CameraTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = renderingData.commandBuffer;
                using (new ProfilingScope(cmd, profilingSampler))
                {
                    var rtSize = m_CameraTarget.referenceSize;
                    m_Material.SetVector(s_TexelSize, new Vector4(1f / rtSize.x, 1f / rtSize.y));
                    m_Material.SetFloat(s_Radius, m_Settings.radius);
                    m_Material.SetFloat(s_IntensityBinCount, m_Settings.binCount);
                    float step = m_Settings.radius;
                    step = Mathf.Min(1f, step);
                    step = Mathf.Max(step, 0.5f);

                    m_Material.SetFloat(s_Step, step);
                    m_Material.SetFloat(s_FarStart, m_Settings.farStart);
                    m_Material.SetFloat(s_FarEnd, m_Settings.farEnd);

                    if (m_Settings.quality == OilPaintingSettings.Quality.High)
                        Execute1Pass(ref renderingData);
                    else
                        Execute2Pass(ref renderingData);
                }
            }

            void Execute1Pass(ref RenderingData renderingData)
            {
                CommandBuffer cmd = renderingData.commandBuffer;
                Blit(cmd, ref renderingData, m_Material, 0);
            }

            void Execute2Pass(ref RenderingData renderingData)
            {
                CommandBuffer cmd = renderingData.commandBuffer;
                Blitter.BlitCameraTexture(cmd, m_CameraTarget, m_OilPaintingStepTarget, RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store, m_Material, 1);
                Blitter.BlitCameraTexture(cmd, m_OilPaintingStepTarget, m_CameraTarget, RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store, m_Material, 2);
            }

            public void Dispose()
            {
                m_OilPaintingStepTarget?.Release();
            }
        }
    }
}