using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

public class UIBlurBackgroundRenderFeature : ScriptableRendererFeature
{
    class UIBlurBackgroundPass : ScriptableRenderPass
    {
        readonly Settings m_Settings;
        RTHandle m_CameraTarget;
        RTHandle m_Buffer0;
        RTHandle m_Buffer1;

        static readonly int s_BlurRadius = Shader.PropertyToID("_BlurOffset");
        static readonly int s_UIBlurBgTexture = Shader.PropertyToID("_UIBlurBgTexture");

        // Setting 中的参数
        private Material blitMat;

        public UIBlurBackgroundPass(Settings settings)
        {
            m_Settings = settings;
            blitMat = settings.blitMat;
            profilingSampler = new ProfilingSampler(nameof(UIBlurBackgroundPass));
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            float downScaleInv = 1f / m_Settings.downScaling;
            descriptor.width = (int)(descriptor.width * downScaleInv);
            descriptor.height = (int)(descriptor.height * downScaleInv);
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = 0;

            RenderingUtils.ReAllocateIfNeeded(ref m_Buffer0, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_BlurBuffer0");
            RenderingUtils.ReAllocateIfNeeded(ref m_Buffer1, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_BlurBuffer1");

            m_CameraTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (blitMat == null) return;

            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, profilingSampler))
            {
                Render(cmd, m_Settings.iterations, m_Settings.blurRadius, 0);
            }
        }

        // Cao Qiang TODO: cmd.Blit will set LoadAction of dest to Load. Try to call SetRenderTarget and use BlitTexture to make it DontCare
        private void Render(CommandBuffer cmd, int iterations, float blurSpread, int blitPass)
        {
            RTHandle source = m_CameraTarget;
            RTHandle destination = m_Buffer0;

            // 第一次会Downsample, 可以扩大blur范围downScaling倍，以获得更强的模糊效果
            cmd.SetGlobalFloat(s_BlurRadius, blurSpread * m_Settings.downScaling);
            cmd.Blit(source, destination, blitMat, blitPass);

            // 开始迭代
            for (int i = 1; i < iterations; i++)
            {
                source = destination;
                destination = ReferenceEquals(source, m_Buffer0) ? m_Buffer1 : m_Buffer0;

                cmd.SetGlobalFloat(s_BlurRadius, (i + 1) * blurSpread);
                cmd.Blit(source, destination, blitMat, blitPass);
            }

            Shader.SetGlobalTexture(s_UIBlurBgTexture, destination);
        }

        internal void ClearRT()
        {
            m_Buffer0?.Release();
            m_Buffer1?.Release();
        }
    }

    [Serializable]
    public class Settings
    {
        public Material blitMat;

        [Tooltip("降采样倍数。")]
        [Range(1.0f, 8.0f)]
        public float downScaling = 3.0f;

        [Tooltip("场景模糊迭代次数。")]
        [Range(1.0f, 4.0f)]
        public int iterations = 3;

        [Tooltip("场景模糊扩散系数。")]
        [Range(0.1f, 5.0f)]
        public float blurRadius = 0.8f;
    }

    [SerializeField]
    public Settings m_Settings;

    UIBlurBackgroundPass m_Pass;

    public override void Create()
    {
        m_Pass = new UIBlurBackgroundPass(m_Settings);
        m_Pass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // 检测是否有足够的UI控件
        int activeUIBlur = 0;
        var uiBlurEntities = UIBlurBackgroundEntityManager.GetUIBlurEntities();
        bool needToClearNullEntity = false;

        foreach (var uiBlurBgEntity in uiBlurEntities)
        {
            if (uiBlurBgEntity)
            {
                if (uiBlurBgEntity.gameObject.activeInHierarchy)
                    activeUIBlur++;
            }
            else
                needToClearNullEntity = true;
        }

        if (needToClearNullEntity)
            UIBlurBackgroundEntityManager.ClearNullEntities();

        if (activeUIBlur > 0 && m_Settings.blitMat)
        {
            SetDefaultValue();
            renderer.EnqueuePass(m_Pass);
        }
        else
        {
            // 释放RT
            m_Pass.ClearRT();
        }
    }

    // Cao Qiang TODO: 根据性能分级设置参数
    public void SetDefaultValue()
    {
        // 配置参数
        /*
        m_Settings.downScaling = 3.64f;
        m_Settings.iterationsWorldMode = 4;
        m_Settings.blurSpreadWorldMode = 0.8f;
        */
    }
}