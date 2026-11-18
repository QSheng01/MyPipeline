using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SYPackages.PerObjectShadow
{
    public class PerObjectShadowRendererFeature : ScriptableRendererFeature
    {
        PerObjectShadowPass perObjectShadowPass;
        PerObjectShadowResolvePass m_PerObjectShadowResolvePass;

        // exec per frame
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Preview && renderingData.cameraData.renderType != CameraRenderType.Overlay)
            {
                renderer.EnqueuePass(perObjectShadowPass);
                renderer.EnqueuePass(m_PerObjectShadowResolvePass);
            }
        }

        // exec when added to pipeline
        public override void Create()
        {
            // Cao Qiang: 必须小于等于AfterRenderingShadows！因为该Pass调用了SetViewProjectionMatrices, 设置完后也没恢复，会导致后面RenderOpaque时VP矩阵变化，深度判断错乱
            perObjectShadowPass = new PerObjectShadowPass(RenderPassEvent.AfterRenderingShadows);

            m_PerObjectShadowResolvePass = new PerObjectShadowResolvePass(RenderPassEvent.AfterRenderingGbuffer);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            perObjectShadowPass?.Dispose(disposing);
            m_PerObjectShadowResolvePass?.Dispose(disposing);
        }
    }
}