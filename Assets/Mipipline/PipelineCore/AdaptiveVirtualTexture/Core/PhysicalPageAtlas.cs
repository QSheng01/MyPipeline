using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace NoOvertime.VirtualTexture
{
    public class PhysicalPageAtlas : IDisposable
    {
        // BC7 feedback cache is enabled by default on supported D3D11 devices, mirroring the clipmap path.
        public static bool CompressionEnabled = true;
        public static bool CompressionSupported => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11
            && SystemInfo.supportsComputeShaders && SystemInfo.SupportsTextureFormat(TextureFormat.BC7)
            && SystemInfo.IsFormatSupported(GraphicsFormat.R32G32B32A32_UInt, FormatUsage.LoadStore)
            && (SystemInfo.copyTextureSupport & CopyTextureSupport.DifferentTypes) != 0
            && (SystemInfo.copyTextureSupport & CopyTextureSupport.RTToTexture) != 0;
        public static bool CompressionActive => CompressionEnabled && CompressionSupported;
        public static float CurrentMemoryMiB
        {
            get
            {
                float page = Constant.PageSizeWithBorder * (float)Constant.PageSizeWithBorder;
                float compressed = CompressionActive ? 1f : 4f;
                float baseBytes = page * Constant.MaxPhysicalPageCount * compressed;
                float maskBytes = page * Constant.MaxPhysicalPageCount * 4f;
                float scratchBytes = CompressionActive ? page * 4f : 0f;
                return (baseBytes + maskBytes + scratchBytes) / (1024f * 1024f);
            }
        }

        private readonly bool _compressed;
        private readonly RTHandle _physicalPageBaseMapAtlas;
        private readonly Texture2DArray _physicalPageBaseMapCompressed;
        private readonly RTHandle _physicalPageBaseMapScratch;
        private readonly RTHandle _physicalPageMaskMapAtlas;
        private readonly RenderTexture _compressionBlocks;
        private readonly ComputeShader _compressionShader;
        private readonly uint[] _paramsArray = new uint[6];
        private readonly ComputeBuffer _paramsBuffer;

        // ReSharper disable once ConvertConstructorToMemberInitializers
        public PhysicalPageAtlas()
        {
            _compressed = CompressionActive;
            _physicalPageMaskMapAtlas = RTHandles.Alloc(Constant.PageSizeWithBorder, Constant.PageSizeWithBorder, Constant.MaxPhysicalPageCount,
                colorFormat: GraphicsFormat.R8G8B8A8_UNorm, dimension: TextureDimension.Tex2DArray, filterMode: FilterMode.Trilinear, wrapMode: TextureWrapMode.Clamp, anisoLevel: 8, enableRandomWrite: true);
            if (_compressed)
            {
                // BC7 is not a valid render-target format, so the compressed atlas is a plain texture array:
                // bake one page into the RGBA8 scratch layer, encode it and copy the raw 128-bit blocks in.
                _physicalPageBaseMapCompressed = new Texture2DArray(Constant.PageSizeWithBorder, Constant.PageSizeWithBorder, Constant.MaxPhysicalPageCount, TextureFormat.BC7, false, true)
                { name = "AVT_FeedbackBC7Base", filterMode = FilterMode.Trilinear, wrapMode = TextureWrapMode.Clamp, anisoLevel = 8, hideFlags = HideFlags.HideAndDontSave };
                _physicalPageBaseMapCompressed.Apply(false, true); // No retained CPU shadow copy; only GPU region copies follow.
                _physicalPageBaseMapScratch = RTHandles.Alloc(Constant.PageSizeWithBorder, Constant.PageSizeWithBorder, 1,
                    colorFormat: GraphicsFormat.R8G8B8A8_UNorm, dimension: TextureDimension.Tex2DArray, filterMode: FilterMode.Point, wrapMode: TextureWrapMode.Clamp, enableRandomWrite: true,
                    name: "AVT_FeedbackBaseScratch");
                _compressionBlocks = new RenderTexture(Constant.PageSizeWithBorder / 4, Constant.PageSizeWithBorder / 4, 0, GraphicsFormat.R32G32B32A32_UInt)
                { name = "AVT_FeedbackBC7BlockBits", enableRandomWrite = true, filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave };
                _compressionBlocks.Create();
                _compressionShader = Resources.Load<ComputeShader>("ClipmapBC7");
            }
            else
            {
                _physicalPageBaseMapAtlas = RTHandles.Alloc(Constant.PageSizeWithBorder, Constant.PageSizeWithBorder, Constant.MaxPhysicalPageCount,
                    colorFormat: GraphicsFormat.R8G8B8A8_UNorm, dimension: TextureDimension.Tex2DArray, filterMode: FilterMode.Trilinear, wrapMode: TextureWrapMode.Clamp, anisoLevel: 8, enableRandomWrite: true);
            }
            _paramsBuffer = new ComputeBuffer(6, 4, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        }

        public void Dispose()
        {
            RTHandles.Release(_physicalPageMaskMapAtlas);
            if (_physicalPageBaseMapAtlas != null)
                RTHandles.Release(_physicalPageBaseMapAtlas);
            if (_physicalPageBaseMapScratch != null)
                RTHandles.Release(_physicalPageBaseMapScratch);
            if (_compressionBlocks != null)
                _compressionBlocks.Release();
            if (_physicalPageBaseMapCompressed != null)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(_physicalPageBaseMapCompressed);
                else
                    UnityEngine.Object.DestroyImmediate(_physicalPageBaseMapCompressed);
            }
            _paramsBuffer.Dispose();
        }

        public Texture PhysicalPageBaseMapAtlas => _compressed ? _physicalPageBaseMapCompressed : (Texture)_physicalPageBaseMapAtlas;
        public RTHandle PhysicalPageMaskMapAtlas => _physicalPageMaskMapAtlas;

        public void Render(CommandBuffer cmd, in int2 sector, in int2 localPageID, in int4 pageID, in int index)
        {
            _paramsArray[0] = (uint)sector.x;
            _paramsArray[1] = (uint)sector.y;
            _paramsArray[2] = (uint)localPageID.x;
            _paramsArray[3] = (uint)localPageID.y;
            _paramsArray[4] = (uint)pageID.z; // mip
            _paramsArray[5] = (uint)pageID.w; // virtual image size log2
            var compute = Context.Instance.Resource.renderingPhysicalPageCompute;
            cmd.SetBufferData(_paramsBuffer, _paramsArray);
            cmd.SetComputeBufferParam(compute, 0, Constant.ParamsID, _paramsBuffer);
            cmd.SetComputeIntParam(compute, Constant.TargetIndexID, index);
            cmd.SetComputeIntParam(compute, "_basePageScratch", _compressed ? 1 : 0);
            //cmd.SetGlobalTexture(Constant.SplatMapID, Context.Instance.RuntimeResource.splatMap);
            cmd.SetComputeTextureParam(compute, 0, Constant.SplatMapID, Context.Instance.RuntimeResource.splatMap);
            cmd.SetComputeTextureParam(compute, 0, Constant.BaseMapArrayID, Context.Instance.RuntimeResource.baseMapArray);
            cmd.SetComputeTextureParam(compute, 0, Constant.MaskMapArrayID, Context.Instance.RuntimeResource.maskMapArray);
            cmd.SetComputeTextureParam(compute, 0, Constant.RWPhysicalPageBaseMapAtlasID, _compressed ? (Texture)_physicalPageBaseMapScratch : (Texture)_physicalPageBaseMapAtlas);
            cmd.SetComputeTextureParam(compute, 0, Constant.RWPhysicalPageMaskMapAtlasID, _physicalPageMaskMapAtlas);
            cmd.DispatchCompute(compute, 0, Constant.PageSizeWithBorder / 4, Constant.PageSizeWithBorder / 4, 1);
            if (_compressed)
            {
                int blocks = Constant.PageSizeWithBorder / 4;
                cmd.SetComputeIntParam(_compressionShader, "PagePitch", Constant.PageSizeWithBorder);
                cmd.SetComputeTextureParam(_compressionShader, 0, "Source", _physicalPageBaseMapScratch);
                cmd.SetComputeTextureParam(_compressionShader, 0, "Blocks", _compressionBlocks);
                cmd.DispatchCompute(_compressionShader, 0, (blocks + 7) / 8, (blocks + 7) / 8, 1);
                // D3D11 compatible raw-bit copy: one uint4 texel becomes one 4x4 BC7 block.
                // Source region uses block units; destination offsets use BC7 pixel units.
                cmd.CopyTexture(_compressionBlocks, 0, 0, 0, 0, blocks, blocks,
                    _physicalPageBaseMapCompressed, index, 0, 0, 0);
            }
        }
    }
}