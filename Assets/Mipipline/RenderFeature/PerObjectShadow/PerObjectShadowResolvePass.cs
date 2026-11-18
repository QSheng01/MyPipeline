// Gavin_KG presents

using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace SYPackages.PerObjectShadow
{
    public class PerObjectShadowResolvePass : ScriptableRenderPass
    {
        RTHandle m_ScreenSpaceShadowMapTexture;
        readonly MaterialPropertyBlock m_Block = new();

        static readonly int s_ScreenSpaceShadowMapTextureID =
            Shader.PropertyToID(PerObjectShadowImpl.s_ScreenSpaceShadowmap);

        private static readonly int s_ShadowBiasID = Shader.PropertyToID("_POShadowBias");
        private static readonly int s_ShadowStrengthID = Shader.PropertyToID("shadowStrength");

        private static readonly string SOFTSHADOW = "_SOFTSHADOW_ON";
        private static readonly string PEROBJECTSHADOW = "_PEROBJECTSHADOW";

        public PerObjectShadowResolvePass(RenderPassEvent renderPassEvent)
        {
            profilingSampler = new ProfilingSampler(nameof(PerObjectShadowResolvePass));
            this.renderPassEvent = renderPassEvent;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            var setting = PerObjectShadowCtrlManager.Instance.perObjectShadowSetting;
            if (setting == null)
                return;
            var descriptor = cameraTextureDescriptor;
            descriptor.width = Mathf.FloorToInt(descriptor.width * setting.screenSpaceMapRenderScale);
            descriptor.height = Mathf.FloorToInt(descriptor.height * setting.screenSpaceMapRenderScale);
            descriptor.depthBufferBits = 0;
            descriptor.graphicsFormat = GraphicsFormat.R8_UNorm;
            RenderingUtils.ReAllocateIfNeeded(ref m_ScreenSpaceShadowMapTexture, descriptor,
                FilterMode.Bilinear, TextureWrapMode.Clamp, name: PerObjectShadowImpl.s_ScreenSpaceShadowmap);

            ConfigureTarget(m_ScreenSpaceShadowMapTexture);
            ConfigureClear(ClearFlag.Color, Color.white);
        }

        void SetDefaultScreenSpaceShadowMap(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(s_ScreenSpaceShadowMapTextureID, Texture2D.whiteTexture);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = renderingData.commandBuffer;

            var ctrl = PerObjectShadowCtrlManager.Instance;
            if (ctrl == null || !ctrl.IsPoSValid())
            {
                SetDefaultScreenSpaceShadowMap(cmd);
                return;
            }

            var impl = ctrl.perObjectShadowImpl;
            if (impl == null)
            {
                SetDefaultScreenSpaceShadowMap(cmd);
                return;
            }

            using (new ProfilingScope(cmd, profilingSampler))
            {
                cmd.SetGlobalTexture(s_ScreenSpaceShadowMapTextureID, m_ScreenSpaceShadowMapTexture);
                Matrix4x4[] worldToAtlasUVMatrixArray = impl.WorldToAtlasUVMatrixArray;

                if (worldToAtlasUVMatrixArray.Length != 0)
                {
                    Matrix4x4[] modelArray = impl.FrustumLocalToWorldMatrixArray;
                    Vector4[] sliceUVOffsetExtendArray = impl.SliceUVOffsetExtendArray;

                    m_Block.SetMatrixArray(PerObjectShadowImpl.s_WorldToUVMatrix, worldToAtlasUVMatrixArray);
                    m_Block.SetVectorArray(PerObjectShadowImpl.s_SliceUVOffsetExtend, sliceUVOffsetExtendArray);

                    cmd.SetGlobalVector(PerObjectShadowImpl.s_TexelSize, impl.TexelSize);

                    var setting = ctrl.perObjectShadowSetting;
                    setting.resolveMat.SetFloat(s_ShadowBiasID, setting.shadowBias);
                    setting.resolveMat.SetFloat(s_ShadowStrengthID, setting.ShadowStrength);

                    if (setting.solfShadow)
                        setting.resolveMat.EnableKeyword(SOFTSHADOW);
                    else
                        setting.resolveMat.DisableKeyword(SOFTSHADOW);

                    cmd.DrawMeshInstanced(PerObjectShadowImpl.FrustumCube, 0, setting.resolveMat, 0, modelArray,
                        modelArray.Length, m_Block);
                    CoreUtils.SetKeyword(cmd, PEROBJECTSHADOW, true);
                }
                else
                    SetDefaultScreenSpaceShadowMap(cmd);
            }
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            CoreUtils.SetKeyword(cmd, PEROBJECTSHADOW, false);
        }

        public void Dispose(bool disposing)
        {
            m_ScreenSpaceShadowMapTexture?.Release();
        }
    }
}