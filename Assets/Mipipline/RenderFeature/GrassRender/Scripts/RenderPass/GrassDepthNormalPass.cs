using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Grass
{
    public class GrassDepthNormalPass : ScriptableRenderPass
    {
        readonly int m_DepthNormalPassIdx;

        public GrassDepthNormalPass(RenderPassEvent evt, int depthNormalPassIdx = 0)
        {
            renderPassEvent = evt;
            m_DepthNormalPassIdx = depthNormalPassIdx;
            profilingSampler = new ProfilingSampler(nameof(GrassDepthNormalPass));
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var renderer = renderingData.cameraData.renderer as UniversalRenderer;
            renderer?.GetDepthNormalPrepass()?.AddCustomPreDrawFunc(DrawGrass);
        }

        void DrawGrass(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, profilingSampler))
            {
                try
                {
                    GrassRenderer.Instance.RenderGrass(cameraData, cmd, m_DepthNormalPassIdx, true);
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to render grass!\n" + e.Message + e.StackTrace);
                }
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // We have to implement the abstract method, but the real execution is in DrawGrass method
        }
    }
}