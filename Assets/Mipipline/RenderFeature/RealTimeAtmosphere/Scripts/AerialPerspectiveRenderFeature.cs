using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AerialPerspectiveRenderFeature : ScriptableRendererFeature
{
    public Material m_AerialPerspectiveMaterial;
    private CustomRenderPass m_Pass;

    private class CustomRenderPass : ScriptableRenderPass
    {
        public Material material;
        private RTHandle m_Source;
        private RTHandle m_Temporary;

        public CustomRenderPass()
        {
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            m_Source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            RenderingUtils.ReAllocateIfNeeded(ref m_Temporary, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "Aerial Perspective Temporary");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null || m_Source?.rt == null || m_Temporary?.rt == null)
                return;

            var cmd = CommandBufferPool.Get("Aerial Perspective");
            Blitter.BlitCameraTexture(cmd, m_Source, m_Temporary, material, 0);
            Blitter.BlitCameraTexture(cmd, m_Temporary, m_Source);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Release()
        {
            m_Temporary?.Release();
            m_Temporary = null;
        }
    }

    public override void Create()
    {
        m_Pass?.Release();
        m_Pass = new CustomRenderPass { renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType != CameraType.Game ||
            renderingData.cameraData.renderType != CameraRenderType.Base)
            return;

        m_Pass.material = m_AerialPerspectiveMaterial;
        renderer.EnqueuePass(m_Pass);
    }

    protected override void Dispose(bool disposing)
    {
        m_Pass?.Release();
        m_Pass = null;
    }
}
