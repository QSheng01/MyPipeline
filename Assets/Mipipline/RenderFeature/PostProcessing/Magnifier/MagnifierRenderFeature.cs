using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class MagnifierRenderFeature : ScriptableRendererFeature
{
    class MagnifierRenderPass : ScriptableRenderPass
    {
        Settings m_Settings;
        RenderTargetIdentifier m_source;
        RenderTargetIdentifier m_dest;
        private CanvasScaler canvasScaler;
        
        public MagnifierRenderPass(Settings settings)
        {
            m_Settings = settings;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            m_source = renderingData.cameraData.renderer.cameraColorTarget;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("MagnifierRenderFeature");
            
            // 配置 Canvas 缩放系数
            if (!canvasScaler)
            {
                canvasScaler = GameObject.FindObjectOfType<CanvasScaler>();
            }
            if (!canvasScaler) return;
            
            // 配置缩放系数
            Canvas canvas = canvasScaler.GetComponent<Canvas>();
            float canvasScaleNum = canvas.scaleFactor;
            
            // 配置裁剪数据
            Vector4 posAndRect = new Vector4();
            posAndRect.x = m_Settings.centerScreenPos.x / renderingData.cameraData.camera.pixelWidth;
            posAndRect.y = m_Settings.centerScreenPos.y / renderingData.cameraData.camera.pixelHeight;
            posAndRect.z = m_Settings.rect.x / m_Settings.scale / renderingData.cameraData.camera.pixelWidth;
            posAndRect.w = m_Settings.rect.y / m_Settings.scale / renderingData.cameraData.camera.pixelHeight;
            m_Settings.blitMat.SetVector("_MagnifierPosAndRect", posAndRect);
            
            // 执行Blit
            cmd.Blit(m_source, m_Settings.renderTexture, m_Settings.blitMat);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    [Serializable]
    public class Settings
    {
        public bool isOpen = true;
        
        // 放大倍数
        public float scale = 1.0f;
        
        // 输出Rt
        public RenderTexture renderTexture;
        
        // 像素点中央, 左下角为起点
        public Vector2 centerScreenPos = new Vector2(100.0f, 100.0f);

        // RectTransform 的宽高
        public Vector2 rect = new Vector2(100.0f, 100.0f);
        
        // 放大系数
        // public float canvasScale = 1.0f;
        
        // 材质
        public Material blitMat;
    }

    [SerializeField]
    public Settings m_Settings;
    MagnifierRenderPass m_pass;


    public override void Create()
    {
        m_pass = new MagnifierRenderPass(m_Settings);
        m_pass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_Settings.renderTexture && m_Settings.blitMat && m_Settings.isOpen)
        {
            renderer.EnqueuePass(m_pass);
        }
    }


}