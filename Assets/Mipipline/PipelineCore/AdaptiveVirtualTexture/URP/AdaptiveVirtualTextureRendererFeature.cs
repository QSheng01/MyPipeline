using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NoOvertime.VirtualTexture
{
    public sealed class AdaptiveVirtualTextureRendererFeature : ScriptableRendererFeature
    {
        private PreparePass _prepare;
        private FinishPass _finish;

        public override void Create()
        {
            _prepare = new PreparePass { renderPassEvent = RenderPassEvent.BeforeRenderingGbuffer };
            _finish = new FinishPass { renderPassEvent = RenderPassEvent.AfterRenderingOpaques };
            AdaptiveVirtualTextureRuntime.EnsureHook();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!AdaptiveVirtualTextureRuntime.IsActive && !ClipmapTexture.UseClipmap)
                return;

            CameraType cameraType = renderingData.cameraData.cameraType;
            if (cameraType == CameraType.Preview || cameraType == CameraType.Reflection)
                return;

            renderer.EnqueuePass(_prepare);
            renderer.EnqueuePass(_finish);
        }

        private sealed class PreparePass : ScriptableRenderPass
        {
            public PreparePass()
            {
                profilingSampler = new ProfilingSampler("AdaptiveVirtualTexture.Prepare");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get("AdaptiveVirtualTexture.Prepare");
                AdaptiveVirtualTextureRuntime.Tick(cmd, renderingData.cameraData.camera);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        private sealed class FinishPass : ScriptableRenderPass
        {
            public FinishPass()
            {
                profilingSampler = new ProfilingSampler("AdaptiveVirtualTexture.Finish");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get("AdaptiveVirtualTexture.Finish");
                AdaptiveVirtualTextureRuntime.EndCamera(cmd);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }
    }
}
