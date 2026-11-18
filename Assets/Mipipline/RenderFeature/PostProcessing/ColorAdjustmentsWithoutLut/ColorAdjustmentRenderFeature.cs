using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class ColorAdjustmentRenderFeature : ScriptableRendererFeature
{
    class ColorAdjustmentRenderPass : ScriptableRenderPass
    {
        Settings m_Settings;
        RenderTargetIdentifier m_source;
        private RenderTargetHandle m_Buffer0;//缓冲区1

        // 常量
        public static readonly int _Custom_ColorAdjustmentsColor = Shader.PropertyToID("_ColorAdjustmentsColor");
        public static readonly int _Custom_ColorAdjustmentsParams = Shader.PropertyToID("_ColorAdjustmentsParams");
        
        // Setting 中的参数
        private Color color;
        private float brightness;
        private float contrast;
        private float saturation;
        private Material blitMat;
        
        public ColorAdjustmentRenderPass(Settings settings)
        {
            m_Settings = settings;
            color = settings.color;
            brightness = settings.brightness;
            contrast = settings.contrast;
            saturation = settings.saturation;
            blitMat = settings.blitMat;
        }
        
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            m_Buffer0.Init("_Buffer0");
            m_source = renderingData.cameraData.renderer.cameraColorTarget;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (blitMat == null)
            {
                return;
            }
            
            CommandBuffer cmd = CommandBufferPool.Get("ColorAdjustmentRenderFeature");
            
            // 配置临时RT
            cmd.GetTemporaryRT(m_Buffer0.id, 
                renderingData.cameraData.cameraTargetDescriptor.width, 
                renderingData.cameraData.cameraTargetDescriptor.height, 
                0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);
            

            // 配置数据
            blitMat.SetColor(_Custom_ColorAdjustmentsColor, (Color)color);
            blitMat.SetVector(_Custom_ColorAdjustmentsParams, new Vector4((float)brightness, (float)contrast, (float)saturation, 1));

            // 执行Blit
            cmd.Blit(m_source, m_Buffer0.Identifier());
            cmd.Blit(m_Buffer0.Identifier(), m_source, blitMat);

            // 生效cmd和销毁cmd
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
        
        
        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(m_Buffer0.id);
        }
    }

    [Serializable]
    public class Settings
    {
        public bool isOpen = true;

        // 材质
        public Material blitMat;
        
        [Tooltip("修改画面色调。")]
        public Color color = Color.white;

        [Tooltip("修改画面亮度。")]
        [Range(0.0f, 5.0f)]
        public float brightness = 1f;

        [Tooltip("修改画面对比度。")] 
        [Range(0.5f, 2.0f)]
        public float contrast = 1.0f;

        [Tooltip("修改画面饱和度。")] 
        [Range(0.0f, 2.0f)]
        public float saturation = 1.0f;
    }

    [SerializeField]
    public Settings m_Settings;
    ColorAdjustmentRenderPass m_pass;


    public override void Create()
    {
        m_pass = new ColorAdjustmentRenderPass(m_Settings);
        m_pass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_Settings.blitMat && m_Settings.isOpen)
        {
            renderer.EnqueuePass(m_pass);
        }
    }


}