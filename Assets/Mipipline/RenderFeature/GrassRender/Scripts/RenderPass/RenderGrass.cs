using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Grass
{
    public class RenderGrass : ScriptableRendererFeature
    {
        [Tooltip("草地裁剪 Compute Shader (GrassCulling.compute), 不填则禁用 Hi-Z")]
        public ComputeShader cullingComputeShader;

        RenderGrassPass m_ScriptablePass;
        GrassDepthNormalPass m_ScriptablePrePass;
        GrassHiZPass m_HiZPass;

        public override void Create()
        {
            name = "RenderGrass";
            m_ScriptablePass = new RenderGrassPass(RenderPassEvent.AfterRenderingOpaques, 0);
            m_ScriptablePrePass = new GrassDepthNormalPass(RenderPassEvent.BeforeRenderingPrePasses, 2);
            m_HiZPass = new GrassHiZPass();
            m_HiZPass.Setup();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game &&
                renderingData.cameraData.cameraType != CameraType.SceneView)
                return;
            if (renderingData.cameraData.camera == null ||
                renderingData.cameraData.renderType == CameraRenderType.Overlay)
            {
                return;
            }

            // Hi-Z 必须在草渲染之前 Enqueue, 同一 Event 下先入队先执行
            // 这样 Hi-Z 采集的深度只含场景几何体, 不含草 → 避免自遮挡
            renderer.EnqueuePass(m_HiZPass);
            GrassRenderer.Instance?.BindHiZPass(m_HiZPass);

            renderer.EnqueuePass(m_ScriptablePass);
            renderer.EnqueuePass(m_ScriptablePrePass);
        }

        protected override void Dispose(bool disposing)
        {
            m_HiZPass?.Dispose();
        }
    }
}
