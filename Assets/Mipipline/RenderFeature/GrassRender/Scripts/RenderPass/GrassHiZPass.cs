using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Grass
{
    /// <summary>
    /// 独立的 Hi-Z 深度金字塔生成 Pass
    ///
    /// Mip chain 生成策略 (第三版 — 完全独立纹理链):
    ///   为每一级 mip 分配独立临时 RT, 降采样链 "读 RT[i-1] → 写 RT[i]"
    ///   读与写始终是不同纹理, 彻底规避 D3D11 SRV/RTV 或 SRV/UAV 冲突
    ///   完成后将每个临时 RT 整体 CopyTexture 到 _hiZRT 对应 mip
    ///   
    ///   之前尝试过的方案及其失败原因:
    ///   1. Compute UAV: 同一纹理同时做 SRV+UAV → D3D11 解绑 SRV → Load 全 0
    ///   2. 单个 scratch RT + CopyTexture(子区域): CopyTexture 静默失败
    ///   3. 直接 SetRenderTarget(hiZ, mip): 同一纹理 SRV+RTV → D3D11 解绑 SRV
    /// </summary>
    public class GrassHiZPass : ScriptableRenderPass
    {
        readonly ProfilingSampler _profilingSampler = new ProfilingSampler("GrassHiZ");

        RenderTexture _hiZRT;
        int _hiZWidth;
        int _hiZHeight;
        int _hiZMipCount;

        Material _copyDepthMat;
        bool _initialized;

        static readonly int _HiZTextureProp  = Shader.PropertyToID("_HiZTexture");
        static readonly int _HiZSrcMipProp   = Shader.PropertyToID("_HiZSrcMip");

        const int kMaxMips = 7;
        static readonly int[] s_mipRTIds;

        static GrassHiZPass()
        {
            s_mipRTIds = new int[kMaxMips];
            for (int i = 0; i < kMaxMips; i++)
                s_mipRTIds[i] = Shader.PropertyToID("_HiZMipRT" + i);
        }

        public GrassHiZPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public void Setup()
        {
            var shader = Shader.Find("Hidden/Grass/CopyDepth");
            if (shader != null)
                _copyDepthMat = CoreUtils.CreateEngineMaterial(shader);
            _initialized = _copyDepthMat != null;
        }

        public RenderTexture HiZTexture => _hiZRT;
        public int HiZWidth   => _hiZWidth;
        public int HiZHeight  => _hiZHeight;
        public int HiZMipCount => _hiZMipCount;

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!_initialized || _copyDepthMat == null)
                return;

            var cam = renderingData.cameraData.camera;
            EnsureHiZRT(cam.pixelWidth, cam.pixelHeight);

            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                // ═══ 步骤 1: CopyDepth → _hiZRT mip 0 ═══
                cmd.SetRenderTarget(_hiZRT, 0, CubemapFace.Unknown, -1);
                cmd.SetViewport(new Rect(0, 0, _hiZWidth, _hiZHeight));
                cmd.DrawProcedural(Matrix4x4.identity, _copyDepthMat, 0, MeshTopology.Triangles, 3);

                // ═══ 步骤 2: 独立纹理链降采样 ═══
                cmd.SetGlobalFloat(_HiZSrcMipProp, 0);

                // 2a: 为每级 mip 分配独立临时 RT
                int mipCount = Mathf.Min(_hiZMipCount, kMaxMips);
                for (int mip = 1; mip < mipCount; mip++)
                {
                    int w = Mathf.Max(1, _hiZWidth >> mip);
                    int h = Mathf.Max(1, _hiZHeight >> mip);
                    cmd.GetTemporaryRT(s_mipRTIds[mip], w, h, 0,
                        FilterMode.Point, RenderTextureFormat.RFloat);
                }

                // 2b: 降采样链 — 读 source(mip 0) → 写 dest(独立 RT)
                //     source: mip==1 时为 _hiZRT, 其余为上一级临时 RT
                for (int mip = 1; mip < mipCount; mip++)
                {
                    int w = Mathf.Max(1, _hiZWidth >> mip);
                    int h = Mathf.Max(1, _hiZHeight >> mip);

                    var src = (mip == 1)
                        ? new RenderTargetIdentifier(_hiZRT)
                        : new RenderTargetIdentifier(s_mipRTIds[mip - 1]);

                    cmd.SetGlobalTexture(_HiZTextureProp, src);
                    cmd.SetRenderTarget(s_mipRTIds[mip]);
                    cmd.SetViewport(new Rect(0, 0, w, h));
                    cmd.DrawProcedural(Matrix4x4.identity, _copyDepthMat, 1,
                        MeshTopology.Triangles, 3);
                }

                // 2c: 全量 CopyTexture 回 _hiZRT 对应 mip
                for (int mip = 1; mip < mipCount; mip++)
                {
                    cmd.CopyTexture(
                        new RenderTargetIdentifier(s_mipRTIds[mip]), 0, 0,
                        new RenderTargetIdentifier(_hiZRT), 0, mip);
                }

                // 2d: 释放临时 RT
                for (int mip = 1; mip < mipCount; mip++)
                    cmd.ReleaseTemporaryRT(s_mipRTIds[mip]);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        void EnsureHiZRT(int screenWidth, int screenHeight)
        {
            // 基础分辨率固定 1024, mip 最多 7 级 (1024 → 512 → 256 → 128 → 64 → 32 → 16)
            int w = 1024;
            int h = 1024;

            if (_hiZRT != null && _hiZWidth == w && _hiZHeight == h)
                return;

            ReleaseHiZRT();

            _hiZWidth  = w;
            _hiZHeight = h;
            _hiZMipCount = Mathf.Min(Mathf.FloorToInt(Mathf.Log(Mathf.Max(w, h), 2)) + 1, kMaxMips);

            _hiZRT = new RenderTexture(w, h, 0, RenderTextureFormat.RFloat)
            {
                useMipMap        = true,
                autoGenerateMips = false,
                filterMode       = FilterMode.Point,
                name             = "GrassHiZPyramid",
            };
            _hiZRT.Create();
        }

        public void ReleaseHiZRT()
        {
            if (_hiZRT != null)
            {
                _hiZRT.Release();
                Object.DestroyImmediate(_hiZRT);
                _hiZRT = null;
            }
            _hiZWidth = 0;
            _hiZHeight = 0;
            _hiZMipCount = 0;
        }

        public void Dispose()
        {
            ReleaseHiZRT();
            CoreUtils.Destroy(_copyDepthMat);
            _copyDepthMat = null;
        }
    }
}
