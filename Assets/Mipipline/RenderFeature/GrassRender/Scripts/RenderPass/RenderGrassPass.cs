using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Grass
{
    class RenderGrassPass : ScriptableRenderPass
    {
        private int shaderpass = 0;

        public RenderGrassPass(RenderPassEvent evt, int shaderpass = 0)
        {
            this.renderPassEvent = evt;
            this.shaderpass = shaderpass;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(cameraData.camera.scene.name)) return;
#endif

            var cmd = CommandBufferPool.Get();
            cmd.name = "RenderGrass";
#if UNITY_EDITOR
            UnityEngine.Profiling.Profiler.BeginSample("grassRender.render");
#endif
            bool depthWrite = !renderingData.cameraData.renderer.useDepthPriming;
            GrassRenderer.Instance.RenderGrass(cameraData, cmd, shaderpass, depthWrite);
#if UNITY_EDITOR
            UnityEngine.Profiling.Profiler.EndSample();
#endif
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
    }
}
