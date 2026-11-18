using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace NoOvertime.VirtualTexture
{
    /// <summary>Fixed physical page grids, toroidal slot reuse and a job-built virtual-to-physical page table.</summary>
    [ExecuteAlways]
    public sealed class ClipmapTexture : MonoBehaviour
    {
        public const int MaxLevels = Constant.MaxVirtualPageSizeShift + 1;
        public const int PageBorder = Constant.BorderSize;
        public static bool UseClipmap;
        public static bool DebugColors;

        [Header("Quality")]
        [Tooltip("Nine density grades matching Feedback Mip0..Mip8, from 1024 to 4 texels/m. Coverage is still a bounded page window.")]
        public bool matchFeedbackQuality = true;
        [Range(1, 8)] public int anisotropy = 8;
        [Range(0.02f, 0.3f)] public float transitionWidth = 0.1f;
        [Header("Partition")]
        [Range(256, 2048)] public int resolution = 2048;
        [Range(1, MaxLevels)] public int levelCount = MaxLevels;
        [Tooltip("L0 window side in meters, excluding physical page borders. Each level doubles this size.")]
        [Range(0.25f, 2048f)] public float finestWorldSize = 2f;
        [Header("Compression")]
        [Tooltip("GPU BC7 compression for baked Base RGBA pages on D3D11. Mask stays RGBA8 to preserve normals, AO and roughness. Unsupported devices use RGBA8.")]
        public bool compressBaseMap = true;
        [Header("Pages")]
        [Tooltip("Interior texels per physical page; rounded to a power of two. Pages per axis = resolution / tileSize.")]
        public int tileSize = 128;
        [Tooltip("Maximum dirty pages baked each frame across all levels; the permanent tail is initialized separately.")]
        public int tilesPerFrame = 64;
        public Texture2D splatMap;
        public Texture2DArray baseMapArray;
        public Texture2DArray maskMapArray;
        public ComputeShader bakeShader;

        private static ClipmapTexture _active;
        private static GlobalKeyword _keyword;
        private static bool _keywordReady;
        private RenderTexture _base, _mask;
        private Texture2DArray _indirection;
        private Texture2DArray _compressedBase;
        private RenderTexture _compressionBlocks;
        private ComputeShader _compressionShader;
        private bool _bakedCompression;
        public bool CompressionSupported => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11
            && SystemInfo.supportsComputeShaders && SystemInfo.SupportsTextureFormat(TextureFormat.BC7)
            && SystemInfo.IsFormatSupported(GraphicsFormat.R32G32B32A32_UInt, FormatUsage.LoadStore)
            && (SystemInfo.copyTextureSupport & CopyTextureSupport.DifferentTypes) != 0
            && (SystemInfo.copyTextureSupport & CopyTextureSupport.RTToTexture) != 0;
        public bool CompressionActive => _compressedBase != null;
        public Texture BaseCache => _compressedBase != null ? (Texture)_compressedBase : _base;
        public Texture MaskCache => _mask;
        private NativeArray<PageData> _pages;
        private NativeArray<Color32> _tableColors;
        private NativeArray<Vector2Int> _anchors;
        private readonly Level[] _levels = new Level[MaxLevels + 1];
        private readonly Vector4[] _levelParams = new Vector4[MaxLevels + 1];
        private int _advancedFrame = -1, _bakedResolution = -1, _bakedLevelCount = -1, _bakedPageWidth;
        private int _pagesPerAxis, _pagesPerLayer, _atlasWidth;
        private float _bakedFinestWorldSize = -1, _seenFinestWorldSize = -1, _stableSince = -1;
        private Vector2 _terrainOrigin, _terrainSize, _bakedTerrainOrigin, _bakedTerrainSize;
        private Texture _sourceBase, _sourceMask, _sourceSplat;
        private bool _tableDirty, _loggedMissingShader;

        private struct Level
        {
            public Vector2Int anchor;
            public float worldSize;
            public bool initialized;
            public int readyPages;
        }
        // Stored by immutable physical slot. The slot's world page owner changes only when recycled.
        private struct PageData
        {
            public Vector2Int worldPage;
            public int assigned;
            public int ready;
        }
        private struct IndirectionJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<PageData> pages;
            [ReadOnly] public NativeArray<Vector2Int> anchors;
            [WriteOnly] public NativeArray<Color32> colors;
            public int perAxis, perLayer;
            public void Execute(int index)
            {
                int layer = index / perLayer, local = index % perLayer;
                Vector2Int world = anchors[layer] + new Vector2Int(local % perAxis, local / perAxis);
                int px = Mod(world.x, perAxis), py = Mod(world.y, perAxis);
                PageData page = pages[layer * perLayer + py * perAxis + px];
                bool valid = page.assigned != 0 && page.ready != 0 && page.worldPage == world;
                // Write logical order, not physical order: each job index exclusively owns its output.
                colors[index] = new Color32((byte)px, (byte)py, valid ? (byte)255 : (byte)0, (byte)layer);
            }
        }

        public Vector2 FinestCenter => _levels[0].initialized ? LevelOrigin(0) + Vector2.one * (_levels[0].worldSize * 0.5f) : Vector2.zero;
        public float FinestTexelsPerMeter => resolution / finestWorldSize;
        public int PagesPerAxis => resolution / Mathf.ClosestPowerOfTwo(Mathf.Clamp(tileSize, 32, resolution));
        public int DirtyPageCount { get; private set; }
        public int LastBakedPageCount { get; private set; }
        public int LastReassignedPageCount { get; private set; }
        public Texture2DArray IndirectionTexture => _indirection;
        public float CacheMemoryMiB
        {
            get
            {
                float width = resolution + PagesPerAxis * 2 * PageBorder;
                bool compressed = _base != null ? _bakedCompression : compressBaseMap && CompressionSupported;
                float bytes = width * width * (levelCount + 1) * (compressed ? 5 : 8);
                if (compressed) { float pitch = tileSize + 2 * PageBorder; bytes += pitch * pitch * 5; }
                return (bytes + PagesPerAxis * PagesPerAxis * (levelCount + 1) * 4) / (1024f * 1024f);
            }
        }
        public int ReadyLevelCount
        {
            get { int n = 0; for (int i = 0; i < Mathf.Max(0, _bakedLevelCount); i++) if (_levels[i].readyPages == _pagesPerLayer) n++; return n; }
        }
        public bool PartitionRebuildPending => _base == null || _bakedResolution != resolution || _bakedLevelCount != levelCount
            || _bakedPageWidth != tileSize || _bakedCompression != (compressBaseMap && CompressionSupported) || Mathf.Abs(_bakedFinestWorldSize - finestWorldSize) > 0.0001f || DirtyPageCount != 0;
        public float LevelWorldSize(int level) => finestWorldSize * (1 << Mathf.Clamp(level, 0, MaxLevels - 1));
        public Color32 GetPageMapping(int level, int x, int y)
        {
            if (!_tableColors.IsCreated || level < 0 || level > _bakedLevelCount || x < 0 || y < 0 || x >= _pagesPerAxis || y >= _pagesPerAxis) return default;
            return _tableColors[level * _pagesPerLayer + y * _pagesPerAxis + x];
        }
        public Vector2Int GetPageAnchor(int level) => _levels[Mathf.Clamp(level, 0, MaxLevels)].anchor;

        [ContextMenu("Apply Feedback Quality")]
        public void ApplyFeedbackQuality()
        {
            matchFeedbackQuality = true; 
            resolution = 2048; 
            finestWorldSize = 2;
            levelCount = MaxLevels; 
            tilesPerFrame = 64; 
            anisotropy = 8;
        }
        private void OnEnable()
        {
            var layers = GetComponent<AdaptiveVirtualTextureLayerSet>();
            if (layers != null)
            {
                if (splatMap == null) splatMap = layers.splatMap;
                if (baseMapArray == null) baseMapArray = layers.baseMapArray;
                if (maskMapArray == null) maskMapArray = layers.maskMapArray;
            }
            _active = this; 
            _advancedFrame = -1;
        }
        private void OnDisable() { if (_active == this) _active = null; ReleaseTextures(); }
        private void OnValidate() => NormalizeSettings();
        private void NormalizeSettings()
        {
            if (matchFeedbackQuality) { resolution = 2048; finestWorldSize = resolution / (float)Constant.MaxTexelDensity; levelCount = MaxLevels; }
            resolution = Mathf.ClosestPowerOfTwo(Mathf.Clamp(resolution, 256, 2048));
            levelCount = Mathf.Clamp(levelCount, 1, MaxLevels);
            finestWorldSize = Mathf.Clamp(finestWorldSize, 0.25f, 2048f);
            tileSize = Mathf.ClosestPowerOfTwo(Mathf.Clamp(tileSize, 32, Mathf.Min(256, resolution)));
            tilesPerFrame = Mathf.Clamp(tilesPerFrame, 1, 256);
            anisotropy = Mathf.Clamp(anisotropy, 1, 8);
            transitionWidth = Mathf.Clamp(transitionWidth, 0.02f, 0.3f);
        }
        public static void DisableKeyword(CommandBuffer cmd)
        {
            if (!_keywordReady) return;
            cmd.SetGlobalInt("ClipmapDebug", 0);
            cmd.DisableKeyword(_keyword);
        }
        public static void RenderActive(CommandBuffer cmd, Camera camera)
        {
            if (_active == null || camera == null) { DisableKeyword(cmd); return; }
            _active.Render(cmd, camera);
        }
        private void Render(CommandBuffer cmd, Camera camera)
        {
            if (!_keywordReady) { _keyword = GlobalKeyword.Create("_CLIPMAP_TEXTURE"); _keywordReady = true; }
            if (!IsDrivingCamera(camera)) { DisableKeyword(cmd); return; }
            if (bakeShader == null || splatMap == null || baseMapArray == null || maskMapArray == null)
            {
                if (!_loggedMissingShader) { Debug.LogError("Clipmap requires its baker and terrain source textures.", this); _loggedMissingShader = true; }
                DisableKeyword(cmd); return;
            }
            SyncPartition();
            if (_advancedFrame != Time.renderedFrameCount)
            {
                _advancedFrame = Time.renderedFrameCount;
                UpdatePages(cmd, camera.transform.position);
            }
            Bind(cmd); cmd.EnableKeyword(_keyword);
        }
        private static bool IsDrivingCamera(Camera camera)
        {
            if (camera == null) return false;
            if (!Application.isPlaying) return camera.cameraType == CameraType.Game || camera.cameraType == CameraType.SceneView;
            if (camera.cameraType != CameraType.Game) return false;
            return Camera.main == null || camera == Camera.main;
        }
        private void ReadTerrainBounds()
        {
            var terrain = GetComponent<Terrain>();
            if (terrain == null) terrain = Terrain.activeTerrain;
            if (terrain != null && terrain.terrainData != null)
            {
                Vector3 origin = terrain.transform.position, size = terrain.terrainData.size;
                _terrainOrigin = new Vector2(origin.x, origin.z); _terrainSize = new Vector2(size.x, size.z);
            }
            else
            {
                var vt = GetComponent<VirtualTextureMono>();
                float size = vt != null ? vt.terrainSize : Mathf.Max(splatMap.width, splatMap.height);
                _terrainOrigin = Vector2.zero; _terrainSize = Vector2.one * Mathf.Max(1, size);
            }
        }
        private void SyncPartition()
        {
            NormalizeSettings(); ReadTerrainBounds();
            if (compressBaseMap && CompressionSupported && _compressionShader == null)
                _compressionShader = Resources.Load<ComputeShader>("ClipmapBC7");
            bool compression = compressBaseMap && CompressionSupported && _compressionShader != null;
            bool textureChanged = _base == null || _bakedResolution != resolution || _bakedLevelCount != levelCount || _bakedPageWidth != tileSize || _bakedCompression != compression;
            bool sizeChanged = Mathf.Abs(_bakedFinestWorldSize - finestWorldSize) > 0.0001f;
            bool sourceChanged = _sourceBase != baseMapArray || _sourceMask != maskMapArray || _sourceSplat != splatMap
                || _bakedTerrainOrigin != _terrainOrigin || _bakedTerrainSize != _terrainSize;
            if (!textureChanged && !sizeChanged && !sourceChanged) { _stableSince = -1; return; }
            if (!textureChanged && !sourceChanged && sizeChanged)
            {
                if (_stableSince < 0 || Mathf.Abs(_seenFinestWorldSize - finestWorldSize) > 0.0001f)
                { _seenFinestWorldSize = finestWorldSize; _stableSince = Time.realtimeSinceStartup; return; }
                if (Time.realtimeSinceStartup - _stableSince < 0.15f) return;
            }
            if (textureChanged)
            {
                ReleaseTextures();
                _pagesPerAxis = resolution / tileSize; 
                _pagesPerLayer = _pagesPerAxis * _pagesPerAxis;
                _atlasWidth = _pagesPerAxis * (tileSize + 2 * PageBorder);
                _bakedCompression = compression;
                _base = CreateArray("AVT_ClipmapPageBase"); _mask = CreateArray("AVT_ClipmapPageMask");
                if (compression)
                {
                    _compressedBase = new Texture2DArray(_atlasWidth, _atlasWidth, levelCount + 1, TextureFormat.BC7, false, true)
                    { name = "AVT_ClipmapBC7Base", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
                    _compressedBase.Apply(false, true); // No retained CPU shadow copy; only GPU region copies follow.
                    int blockWidth = (tileSize + 2 * PageBorder) / 4;
                    _compressionBlocks = new RenderTexture(blockWidth, blockWidth, 0, GraphicsFormat.R32G32B32A32_UInt)
                    { name = "AVT_BC7BlockBits", enableRandomWrite = true, filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave };
                    _compressionBlocks.Create();
                }
                int count = (levelCount + 1) * _pagesPerLayer;
                _pages = new NativeArray<PageData>(count, Allocator.Persistent);
                _tableColors = new NativeArray<Color32>(count, Allocator.Persistent);
                _anchors = new NativeArray<Vector2Int>(levelCount + 1, Allocator.Persistent);
                _indirection = new Texture2DArray(_pagesPerAxis, _pagesPerAxis, levelCount + 1, TextureFormat.RGBA32, false, true)
                { name = "AVT_ClipmapIndirection", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            }
            for (int i = 0; i < _pages.Length; i++) _pages[i] = default;
            for (int i = 0; i < _levels.Length; i++) _levels[i] = default;
            _bakedResolution = resolution; 
            _bakedLevelCount = levelCount;
            _bakedPageWidth = tileSize;
            _bakedFinestWorldSize = finestWorldSize;
            _bakedTerrainOrigin = _terrainOrigin;
            _bakedTerrainSize = _terrainSize;
            _sourceBase = baseMapArray;
            _sourceMask = maskMapArray;
            _sourceSplat = splatMap;
            _stableSince = -1; 
            _advancedFrame = -1;
            _tableDirty = true;
        }
        private RenderTexture CreateArray(string name)
        {
            bool scratch = _bakedCompression && name == "AVT_ClipmapPageBase";
            int width = scratch ? tileSize + 2 * PageBorder : _atlasWidth;
            var rt = new RenderTexture(width, width, 0, GraphicsFormat.R8G8B8A8_UNorm)
            {
                name = name, dimension = TextureDimension.Tex2DArray, volumeDepth = scratch ? 1 : levelCount + 1,
                enableRandomWrite = true, useMipMap = false, filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave
            };
            rt.Create(); return rt;
        }
        private Vector2 LevelOrigin(int level) => _bakedTerrainOrigin + (Vector2)_levels[level].anchor * (_levels[level].worldSize / _pagesPerAxis);
        private Vector2Int DesiredAnchor(Vector3 cameraPosition, float worldSize)
        {
            float pageWorldSize = worldSize / _pagesPerAxis;
            Vector2 local = new Vector2(cameraPosition.x, cameraPosition.z) - _bakedTerrainOrigin;
            return new Vector2Int(
                Mathf.Clamp(Mathf.FloorToInt(local.x / pageWorldSize) - _pagesPerAxis / 2, 0, Mathf.Max(0, Mathf.CeilToInt(_bakedTerrainSize.x / pageWorldSize) - _pagesPerAxis)),
                Mathf.Clamp(Mathf.FloorToInt(local.y / pageWorldSize) - _pagesPerAxis / 2, 0, Mathf.Max(0, Mathf.CeilToInt(_bakedTerrainSize.y / pageWorldSize) - _pagesPerAxis)));
        }
        private void CollectDirtyPages(int layer, Vector2Int anchor, float worldSize)
        {
            ref Level level = ref _levels[layer];
            if (level.initialized && level.anchor == anchor) return;
            level.anchor = anchor; 
            level.worldSize = worldSize; 
            level.initialized = true; 
            level.readyPages = 0;
            _anchors[layer] = anchor;
            for (int y = 0; y < _pagesPerAxis; y++) for (int x = 0; x < _pagesPerAxis; x++)
            {
                Vector2Int world = anchor + new Vector2Int(x, y);
                int slot = layer * _pagesPerLayer + Mod(world.y, _pagesPerAxis) * _pagesPerAxis + Mod(world.x, _pagesPerAxis);
                PageData page = _pages[slot];
                if (page.assigned == 0 || page.worldPage != world)
                {
                    // Absolute modulo is equivalent to accumulating rectOffset, including negative moves and teleports.
                    page = new PageData { worldPage = world, assigned = 1, ready = 0 };
                    _pages[slot] = page; 
                    LastReassignedPageCount++;
                }
                if (page.ready != 0) level.readyPages++;
            }
            _tableDirty = true;
        }
        private void UpdatePages(CommandBuffer cmd, Vector3 cameraPosition)
        {
            LastBakedPageCount = 0; LastReassignedPageCount = 0;
            int kernel = bakeShader.FindKernel("KMain"), tail = _bakedLevelCount;
            cmd.SetComputeIntParam(bakeShader, "_pageWidth", _bakedPageWidth);
            cmd.SetComputeIntParam(bakeShader, "_pageBorder", PageBorder);
            cmd.SetComputeIntParam(bakeShader, "_basePageScratch", _bakedCompression ? 1 : 0);
            cmd.SetComputeTextureParam(bakeShader, kernel, "_SplatMap", splatMap);
            cmd.SetComputeTextureParam(bakeShader, kernel, "_BaseMapArray", baseMapArray);
            cmd.SetComputeTextureParam(bakeShader, kernel, "_MaskMapArray", maskMapArray);
            cmd.SetComputeTextureParam(bakeShader, kernel, "_clipmapBase", _base);
            cmd.SetComputeTextureParam(bakeShader, kernel, "_clipmapMask", _mask);
            if (!_levels[tail].initialized)
            {
                CollectDirtyPages(tail, Vector2Int.zero, Mathf.Max(_bakedTerrainSize.x, _bakedTerrainSize.y));
                for (int slot = 0; slot < _pagesPerLayer; slot++) BakePage(cmd, kernel, tail, slot);
            }
            for (int layer = 0; layer < tail; layer++)
            {
                float size = _bakedFinestWorldSize * (1 << layer);
                CollectDirtyPages(layer, DesiredAnchor(cameraPosition, size), size);
            }
            int budget = tilesPerFrame;
            for (int layer = tail - 1; layer >= 0; layer--)
                for (int slot = 0; slot < _pagesPerLayer && budget > 0; slot++)
                    if (_pages[layer * _pagesPerLayer + slot].ready == 0)
                    { BakePage(cmd, kernel, layer, slot); budget--; LastBakedPageCount++; }
            DirtyPageCount = 0;
            for (int i = 0; i < tail; i++) DirtyPageCount += _pagesPerLayer - _levels[i].readyPages;
            if (_tableDirty) UpdateIndirection();
        }
        private void BakePage(CommandBuffer cmd, int kernel, int layer, int slot)
        {
            int index = layer * _pagesPerLayer + slot;
            PageData page = _pages[index];
            float texel = _levels[layer].worldSize / _bakedResolution;
            Vector2 origin = _bakedTerrainOrigin + (Vector2)page.worldPage * (_bakedPageWidth * texel);
            int pitch = _bakedPageWidth + 2 * PageBorder;
            cmd.SetComputeIntParam(bakeShader, "_level", layer);
            cmd.SetComputeVectorParam(bakeShader, "_pageWorldOrigin", new Vector4(origin.x, origin.y, 0, 0));
            cmd.SetComputeVectorParam(bakeShader, "_pagePhysicalOrigin", new Vector4((slot % _pagesPerAxis) * pitch, (slot / _pagesPerAxis) * pitch, 0, 0));
            cmd.SetComputeFloatParam(bakeShader, "_metersPerTexel", texel);
            cmd.DispatchCompute(bakeShader, kernel, (pitch + 7) / 8, (pitch + 7) / 8, 1);
            if (_bakedCompression)
            {
                int blocks = pitch / 4;
                cmd.SetComputeIntParam(_compressionShader, "PagePitch", pitch);
                cmd.SetComputeTextureParam(_compressionShader, 0, "Source", _base);
                cmd.SetComputeTextureParam(_compressionShader, 0, "Blocks", _compressionBlocks);
                cmd.DispatchCompute(_compressionShader, 0, (blocks + 7) / 8, (blocks + 7) / 8, 1);
                // D3D11 compatible raw-bit copy: one uint4 texel becomes one 4x4 BC7 block.
                // Source region uses block units; destination offsets use BC7 pixel units.
                cmd.CopyTexture(_compressionBlocks, 0, 0, 0, 0, blocks, blocks,
                    _compressedBase, layer, 0, (slot % _pagesPerAxis) * pitch, (slot / _pagesPerAxis) * pitch);
            }
            // Publish only after bake -> optional encode -> copy have been recorded in draw order.
            page.ready = 1; _pages[index] = page; _levels[layer].readyPages++; _tableDirty = true;
        }
        private void UpdateIndirection()
        {
            var job = new IndirectionJob { pages = _pages, anchors = _anchors, colors = _tableColors, perAxis = _pagesPerAxis, perLayer = _pagesPerLayer };
            job.Schedule(_tableColors.Length, 64).Complete();
            for (int layer = 0; layer <= _bakedLevelCount; layer++)
                _indirection.SetPixelData(_tableColors, 0, layer, layer * _pagesPerLayer);
            _indirection.Apply(false, false); _tableDirty = false;
        }
        private void Bind(CommandBuffer cmd)
        {
            for (int i = 0; i <= _bakedLevelCount; i++)
            {
                Vector2 origin = LevelOrigin(i);
                _levelParams[i] = new Vector4(origin.x, origin.y, _levels[i].worldSize, _levels[i].worldSize / _bakedResolution);
            }
            cmd.SetGlobalTexture("ClipmapBase", BaseCache);
            cmd.SetGlobalTexture("ClipmapMask", _mask);
            cmd.SetGlobalTexture("ClipmapIndirection", _indirection);
            // Fixed capacity with new IDs: never inherit a legacy global array's capacity/meaning.
            cmd.SetGlobalVectorArray("ClipmapPageWindows10", _levelParams);
            cmd.SetGlobalVector("ClipmapPageLayout", new Vector4(_bakedPageWidth, _pagesPerAxis, PageBorder, _atlasWidth));
            cmd.SetGlobalVector("ClipmapSampling", new Vector4(_bakedResolution, transitionWidth, anisotropy, 0));
            cmd.SetGlobalInt("ClipmapLevelCount", _bakedLevelCount); cmd.SetGlobalInt("ClipmapDebug", DebugColors ? 1 : 0);
        }
        private void ReleaseTextures()
        {
            Release(ref _base); Release(ref _mask); Release(ref _compressionBlocks);
            if (_compressedBase != null) { if (Application.isPlaying) Destroy(_compressedBase); else DestroyImmediate(_compressedBase); _compressedBase = null; }
            if (_indirection != null) { if (Application.isPlaying) Destroy(_indirection); else DestroyImmediate(_indirection); _indirection = null; }
            if (_pages.IsCreated) _pages.Dispose();
            if (_tableColors.IsCreated) _tableColors.Dispose();
            if (_anchors.IsCreated) _anchors.Dispose();
            _bakedResolution = -1; _bakedLevelCount = -1; _advancedFrame = -1;
            DirtyPageCount = 0;
        }
        private static void Release(ref RenderTexture rt)
        {
            if (rt == null) return; rt.Release();
            if (Application.isPlaying) Destroy(rt); else DestroyImmediate(rt); rt = null;
        }
        private static int Mod(int value, int length) => (value % length + length) % length;
    }
}
