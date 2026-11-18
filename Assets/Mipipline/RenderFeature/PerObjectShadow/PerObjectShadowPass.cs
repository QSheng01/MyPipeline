// Gavin_KG presents

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SYPackages.PerObjectShadow
{
    public class PerObjectShadowPass : ScriptableRenderPass
    {
        public static readonly string DEPTHONLYPASS = "DepthOnly";

        RTHandle m_PerObjectShadowMapAtlas;

        const int k_ShadowmapBufferBits = 32;

        private static readonly int AtlasTextureID = Shader.PropertyToID(PerObjectShadowImpl.s_PerObjectShadowMapAtlas);

        public PerObjectShadowPass(RenderPassEvent renderPassEvent)
        {
            profilingSampler = new ProfilingSampler(nameof(PerObjectShadowPass));
            this.renderPassEvent = renderPassEvent;
        }

        // OnCameraSetup和Configure理论上应该只重写一个
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var ctrl = PerObjectShadowCtrlManager.Instance;
            if (ctrl == null || !ctrl.IsPoSValid() || ctrl.CurrentCtrl is null)
                return;
            var camera = renderingData.cameraData.camera;
            ctrl.Update(camera);

            Vector2Int rtRes = ctrl.GetAtlasResolution();

            if (rtRes.x <= 0 || rtRes.y <= 0)
            {
                return;
            }

            ShadowUtils.ShadowRTReAllocateIfNeeded(ref m_PerObjectShadowMapAtlas, rtRes.x, rtRes.y,
                k_ShadowmapBufferBits, name: PerObjectShadowImpl.s_PerObjectShadowMapAtlas);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = renderingData.commandBuffer;
            var ctrl = PerObjectShadowCtrlManager.Instance;
            if (ctrl == null || !ctrl.IsPoSValid())
            {
                return;
            }

            var setting = ctrl.perObjectShadowSetting;
            var impl = ctrl.perObjectShadowImpl;
            if (setting == null || impl == null)
            {
                return;
            }

            using (new ProfilingScope(cmd, profilingSampler))
            {
                // TODO 先做容错
                if (m_PerObjectShadowMapAtlas == null)
                {
                    return;
                }

                CoreUtils.SetRenderTarget(cmd, m_PerObjectShadowMapAtlas, RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store, ClearFlag.Depth, Color.black);
                cmd.SetGlobalTexture(AtlasTextureID, m_PerObjectShadowMapAtlas); // for resolve pass

                foreach (PerObjectShadowImpl.SliceData sliceData in impl.SliceDataList)
                {
                    if (!sliceData.ShouldRender)
                    {
                        continue;
                    }

                    var slice = sliceData.sliceDataPerFrame.rect;
                    cmd.SetViewport(new Rect(slice.x, slice.y, slice.width, slice.height));

                    cmd.SetViewProjectionMatrices(sliceData.sliceDataPerFrame.shadowViewMatrix,
                        sliceData.sliceDataPerFrame.shadowProjMatrix);

                    var entity = sliceData.entity;
                    if (entity.RenderContexts == null || entity.RenderContexts.Count < 1)
                    {
                        continue;
                    }

                    foreach (PerObjectShadowRenderContext r in entity.RenderContexts)
                    {
                        if (r != null && r.Renderer && r.Renderer.gameObject.activeInHierarchy && r.Renderer.enabled &&
                            r.shaderPass >= 0)
                            cmd.DrawRenderer(r.Renderer, r.Material, r.submeshIndex, r.shaderPass);
                    }
                }
            }
        }

        public void Dispose(bool disposing)
        {
            m_PerObjectShadowMapAtlas?.Release();
        }
    }
}