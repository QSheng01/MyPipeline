using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NoOvertime.VirtualTexture
{
    /// <summary>
    /// Drives the HDRP AVT state machine from URP. Feedback is armed here and bound
    /// inside the actual forward or deferred draw, after URP has set the camera target.
    /// </summary>
    public static class AdaptiveVirtualTextureRuntime
    {
        private enum Stage
        {
            Waiting,
            ReallocateVirtualPage,
            FeedBack,
            RequestAsyncReadBack,
            ReadingBack,
            Deduplicate,
            SortPageID,
            RenderingPage
        }

        private static Stage _stage = Stage.Waiting;
        private static int _advancedFrame = -1;
        private static int _readingBackTimeout;
        private static bool _feedbackArmed;
        private static bool _readingBack;
        private static int _renderPageIndex;
        private static readonly LRUCache<ulong, int> LruCache = CreateCache();
        private static readonly List<KeyValuePair<int2, int4>> DelayRemoveSectors = new List<KeyValuePair<int2, int4>>();
        private static readonly Dictionary<int2, (int4 oldInfo, int4 newInfo)> DelayRemoveImages = new Dictionary<int2, (int4, int4)>();
        private static readonly Stack<int3> TravelStack = new Stack<int3>();
        private static readonly List<int2> RequestSectors = new List<int2>(Constant.MaxPreloadSector);
        private static NativeHashSet<uint> _deduplicateSet;
        private static JobHandle _deduplicateJobHandle;
        private static bool _deduplicateJobScheduled;
        private static readonly PackedPageIDComparer PageComparer = new PackedPageIDComparer();
        private static int2 _lastCameraSector = new int2(int.MinValue, int.MinValue);
        private static float2 _lastCameraPositionXZ = new float2(float.MinValue, float.MinValue);
        private static readonly ProfilerMarker TickMarker = new ProfilerMarker("AdaptiveVirtualTexture.Tick");

        public static bool IsActive => Context.Instance.IsInitialized;

        public static void EnsureHook()
        {
            AdaptiveVirtualTextureDrawHook.BeforeDrawRenderers = BindFeedback;
            AdaptiveVirtualTextureDrawHook.AfterDrawRenderers = UnbindFeedback;
        }

        public static void Shutdown()
        {
            if (_readingBack)
            {
                AsyncGPUReadback.WaitAllRequests();
                _readingBack = false;
            }
            CompleteDeduplicateJob();
            _feedbackArmed = false;
            _readingBack = false;
            _stage = Stage.Waiting;
            LruCache.Clear();
            SeedFreeSlots(LruCache);
            _renderPageIndex = 0;
            _lastCameraSector = new int2(int.MinValue, int.MinValue);
            _lastCameraPositionXZ = new float2(float.MinValue, float.MinValue);
            if (Context.Instance.PageIDOutputTexture != null)
            {
                RTHandles.Release(Context.Instance.PageIDOutputTexture);
                Context.Instance.PageIDOutputTexture = null;
            }

            ReleaseReadbackArray();
            ReleaseDeduplicateBuffers();
        }

        public static void Tick(CommandBuffer cmd, Camera camera)
        {
            if (camera == null)
                return;

            if (ClipmapTexture.UseClipmap)
            {
                if (IsActive)
                {
                    cmd.DisableKeyword(Context.Instance.VirtualTextureKeyword);
                    cmd.DisableKeyword(Context.Instance.OutputPageIDTextureKeyword);
                    _feedbackArmed = false;
                }
                ClipmapTexture.RenderActive(cmd, camera);
                return;
            }

            ClipmapTexture.DisableKeyword(cmd);
            if (!IsActive)
                return;

            using (TickMarker.Auto())
            {
                BindGlobals(cmd);
                cmd.EnableKeyword(Context.Instance.VirtualTextureKeyword);
                if (!ShouldAdvance(camera))
                    return;

                switch (_stage)
                {
                    case Stage.Waiting:
                        _stage = Stage.ReallocateVirtualPage;
                        break;
                    case Stage.ReallocateVirtualPage:
                        Reallocate(cmd, camera);
                        _stage = Stage.FeedBack;
                        break;
                    case Stage.FeedBack:
                        PrepareFeedback(cmd, camera);
                        _stage = Stage.RequestAsyncReadBack;
                        break;
                    case Stage.RequestAsyncReadBack:
                        RequestReadback(cmd);
                        _stage = Stage.ReadingBack;
                        break;
                    case Stage.ReadingBack:
                        _readingBackTimeout++;
                        if (_readingBackTimeout > Constant.ReadingBackTimeout)
                            _stage = Stage.Waiting;
                        break;
                    case Stage.Deduplicate:
                        Deduplicate();
                        _stage = Stage.SortPageID;
                        break;
                    case Stage.SortPageID:
                        CompleteDeduplicateJob();
                        Context.Instance.ResolvedPageID.Sort(PageComparer);
                        _renderPageIndex = 0;
                        RenderPages(cmd);
                        _stage = Stage.RenderingPage;
                        break;
                    case Stage.RenderingPage:
                        RenderPages(cmd);
                        if (_renderPageIndex >= Context.Instance.ResolvedPageID.Length)
                            _stage = Stage.Waiting;
                        break;
                }
            }
        }

        public static void EndCamera(CommandBuffer cmd)
        {
            ClipmapTexture.DisableKeyword(cmd);
            if (!IsActive)
                return;

            _feedbackArmed = false;
            cmd.DisableKeyword(Context.Instance.OutputPageIDTextureKeyword);
            cmd.DisableKeyword(Context.Instance.VirtualTextureKeyword);
            cmd.ClearRandomWriteTargets();
        }

        private static void BindFeedback(CommandBuffer cmd)
        {
            if (!IsActive || !_feedbackArmed || Context.Instance.PageIDOutputTexture == null)
                return;

            cmd.EnableKeyword(Context.Instance.VirtualTextureKeyword);
            cmd.EnableKeyword(Context.Instance.OutputPageIDTextureKeyword);
            cmd.SetRandomWriteTarget(7, Context.Instance.PageIDOutputTexture);
        }

        private static void UnbindFeedback(CommandBuffer cmd)
        {
            if (!IsActive)
                return;

            cmd.ClearRandomWriteTargets();
            cmd.DisableKeyword(Context.Instance.OutputPageIDTextureKeyword);
        }

        private static bool ShouldAdvance(Camera camera)
        {
            if (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView)
                return false;
            if (_advancedFrame == Time.renderedFrameCount)
                return false;
            if (Application.isPlaying && camera.cameraType != CameraType.Game && Camera.main != null)
                return false;

            _advancedFrame = Time.renderedFrameCount;
            return true;
        }

        private static void BindGlobals(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(Constant.Sector2VirtualImageInfoTextureID, Context.Instance.Sector2VirtualImageInfoTexture.RT);
            cmd.SetGlobalTexture(Constant.IndirectionTextureID, Context.Instance.IndirectionTexture.RT);
            cmd.SetGlobalTexture(Constant.PhysicalPageBaseMapAtlasID, Context.Instance.PhysicalPageAtlas.PhysicalPageBaseMapAtlas);
            cmd.SetGlobalTexture(Constant.PhysicalPageMaskMapAtlasID, Context.Instance.PhysicalPageAtlas.PhysicalPageMaskMapAtlas);
        }

        private static void PrepareFeedback(CommandBuffer cmd, Camera camera)
        {
            int width = math.max(camera.pixelWidth, 1);
            int height = math.max(camera.pixelHeight, 1);
            int frame = Time.renderedFrameCount % 64;
            int dither = Constant.BayerDither8X8[frame / Constant.PageIDTextureDownscale, frame % Constant.PageIDTextureDownscale];
            var size = new Vector2Int(width / Constant.PageIDTextureDownscale + 1, height / Constant.PageIDTextureDownscale + 1);
            AllocatePageIDTexture(size);
            cmd.SetComputeTextureParam(Context.Instance.Resource.clearPageIDOutputTextureCompute, 0, Constant.RWPageIDOutputTexture, Context.Instance.PageIDOutputTexture);
            cmd.DispatchCompute(Context.Instance.Resource.clearPageIDOutputTextureCompute, 0, size.x / 8 + 1, size.y / 8 + 1, 1);
            cmd.SetGlobalInteger("VirtualDitherX", dither / Constant.PageIDTextureDownscale);
            cmd.SetGlobalInteger("VirtualDitherY", dither % Constant.PageIDTextureDownscale);
            _feedbackArmed = true;
        }

        private static void AllocatePageIDTexture(Vector2Int size)
        {
            var current = Context.Instance.PageIDOutputTexture;
            if (current != null && current.referenceSize != size)
            {
                if (_readingBack)
                    return;
                // The in-flight job still references ReadBackArray; disposing it first invalidates the job's owner.
                CompleteDeduplicateJob();
                RTHandles.Release(current);
                Context.Instance.PageIDOutputTexture = null;
                ReleaseReadbackArray();
            }

            if (Context.Instance.PageIDOutputTexture == null)
            {
                Context.Instance.PageIDOutputTexture = RTHandles.Alloc(size.x, size.y, colorFormat: GraphicsFormat.R32_UInt, enableRandomWrite: true, name: "AVT_PageIDOutput");
                Context.Instance.ReadBackArray = new NativeArray<uint>(size.x * size.y, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }
        }

        private static void ReleaseReadbackArray()
        {
            if (!Context.Instance.ReadBackArray.IsCreated)
                return;
            try
            {
                Context.Instance.ReadBackArray.Dispose();
            }
            catch (InvalidOperationException)
            {
                Context.Instance.ReadBackArray = default;
            }
        }

        private static void RequestReadback(CommandBuffer cmd)
        {
            _feedbackArmed = false;
            cmd.DisableKeyword(Context.Instance.OutputPageIDTextureKeyword);
            if (Context.Instance.PageIDOutputTexture == null || !Context.Instance.ReadBackArray.IsCreated)
            {
                _stage = Stage.Waiting;
                return;
            }

            _readingBackTimeout = 0;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref Context.Instance.ReadBackArray, AtomicSafetyHandle.Create());
#endif
            _readingBack = true;
            cmd.RequestAsyncReadbackIntoNativeArray(ref Context.Instance.ReadBackArray, Context.Instance.PageIDOutputTexture, 0, GraphicsFormat.R32_UInt, OnReadback);
        }

        private static void OnReadback(AsyncGPUReadbackRequest request)
        {
            _readingBack = false;
            if (!Context.Instance.IsInitialized || request.hasError)
            {
                _stage = Stage.Waiting;
                return;
            }

            _stage = Stage.Deduplicate;
        }

        private static void Deduplicate()
        {
            if (!_deduplicateSet.IsCreated)
                _deduplicateSet = new NativeHashSet<uint>(Constant.MaxDeduplicatedPageCount, Allocator.Persistent);

            var job = new DeduplicateJob(_deduplicateSet, Context.Instance.ReadBackArray, Context.Instance.ResolvedPageID, Context.Instance.ImageInfo);
            _deduplicateJobHandle = job.Schedule();
            _deduplicateJobScheduled = true;
        }

        private static void CompleteDeduplicateJob()
        {
            if (!_deduplicateJobScheduled)
                return;
            _deduplicateJobHandle.Complete();
            _deduplicateJobScheduled = false;
        }

        private static void ReleaseDeduplicateBuffers()
        {
            if (_deduplicateSet.IsCreated)
                _deduplicateSet.Dispose();
            _deduplicateSet = default;
        }

        private static void RenderPages(CommandBuffer cmd)
        {
            int renderingCount = 0;
            var pages = Context.Instance.ResolvedPageID;
            while (_renderPageIndex < pages.Length && renderingCount < Constant.RenderingPagePerFrame && Context.Instance.IndirectionTexture.IsUpdateArrayFarFromFull())
            {
                uint packedPageID = pages[_renderPageIndex++];
                int4 pageID = Utility.UnpackPageID(packedPageID);
                int virtualPageSize = 1 << pageID.w;
                int2 indirectionPageXZ = ((pageID.xy << pageID.z) >> pageID.w) << pageID.w;
                if (!Context.Instance.ImageInfo2Sector.TryGetValue(new int3(indirectionPageXZ, virtualPageSize), out int2 sector))
                    continue;

                int2 localPageID = (pageID.xy << pageID.z) - indirectionPageXZ;
                ulong lruKey = Utility.EncodeLRUKey(sector, localPageID, pageID.z, pageID.w);
                if (!LruCache.Touch(lruKey, out int slot))
                {
                    bool removed = LruCache.RemoveLast(out ulong removeKey, out slot);
                    Assert.IsTrue(removed, "LRU Cache怎么搞成空的了");
                    if (removeKey < 0x7FFFFFFFFFFFFFFFu)
                        InvalidateRemovedPage(removeKey);

                    Assert.IsTrue(LruCache.Insert(lruKey, slot), "LRU Cache插入失败");
                    Context.Instance.PhysicalPageAtlas.Render(cmd, sector, localPageID, pageID, slot);
                    renderingCount++;
                }

                Context.Instance.IndirectionTexture.AddUpdateArray(pageID, slot);
            }

            Context.Instance.IndirectionTexture.UpdateIndirectionTexture(cmd);
        }

        private static void InvalidateRemovedPage(ulong removeKey)
        {
            var decoded = Utility.DecodeLRUKey(removeKey);
            int2 virtualPageID = decoded.Item1;
            int physicalMip = decoded.Item2;
            int2 removeSector = (virtualPageID << physicalMip) / Constant.MaxVirtualPageSize;
            if (!Context.Instance.AllSectors.TryGetValue(removeSector, out int4 imageInfo) || imageInfo.w <= 0)
                return;

            int2 localVirtualPageID = (virtualPageID << physicalMip) % Constant.MaxVirtualPageSize;
            int virtualPageSizeLog = math.ceillog2(imageInfo.w);
            int mip = math.max(physicalMip - Constant.MaxVirtualPageSizeShift + virtualPageSizeLog, 0);
            var removePageID = new int4((imageInfo.yz >> mip) + (localVirtualPageID >> physicalMip), mip, virtualPageSizeLog);
            Context.Instance.IndirectionTexture.AddUpdateArray(removePageID, 65535);
        }

        private static void Reallocate(CommandBuffer cmd, Camera camera)
        {
            Vector3 cameraPosition = camera.transform.position;
            if (CenterSectorChanged(cameraPosition))
                UpdateRequestSectors(cmd);
            if (CameraPositionChanged(cameraPosition))
                ReallocateVirtualImage(cmd);
        }

        private static bool CenterSectorChanged(float3 cameraPosition)
        {
            int2 cameraSector = (int2)cameraPosition.xz >> Constant.SectorSizeShift;
            if (cameraSector.x == _lastCameraSector.x && cameraSector.y == _lastCameraSector.y)
                return false;
            _lastCameraSector = cameraSector;
            return true;
        }

        private static void UpdateRequestSectors(CommandBuffer cmd)
        {
            RequestSectors.Clear();
            TravelStack.Push(new int3(0, 0, Context.Instance.TerrainSize >> Constant.SectorSizeShift));
            while (TravelStack.Count > 0)
            {
                int3 currentNode = TravelStack.Pop();
                int halfSize = currentNode.z >> 1;
                float sqrDistance = math.lengthsq(currentNode.xy - _lastCameraSector + halfSize);
                if (currentNode.z == 1)
                {
                    if (sqrDistance < Constant.SectorPreloadDistance * Constant.SectorPreloadDistance)
                        RequestSectors.Add(new int2(currentNode.x, currentNode.y));
                }
                else if (sqrDistance < math.lengthsq(new float2(halfSize + Constant.SectorPreloadDistance, halfSize + Constant.SectorPreloadDistance)))
                {
                    TravelStack.Push(new int3(currentNode.x + halfSize, currentNode.y + halfSize, halfSize));
                    TravelStack.Push(new int3(currentNode.x, currentNode.y + halfSize, halfSize));
                    TravelStack.Push(new int3(currentNode.x + halfSize, currentNode.y, halfSize));
                    TravelStack.Push(new int3(currentNode.x, currentNode.y, halfSize));
                }
            }

            DelayRemoveSectors.Clear();
            foreach (var pair in Context.Instance.AllSectors)
            {
                if (!RequestSectors.Contains(pair.Key))
                    DelayRemoveSectors.Add(pair);
            }

            foreach (var pair in DelayRemoveSectors)
            {
                int2 removeSector = pair.Key;
                int4 imageInfo = pair.Value;
                Context.Instance.AllSectors.Remove(removeSector);
                Context.Instance.VirtualImageAtlas.RemoveImage(removeSector);
                Context.Instance.ImageInfo2Sector.Remove(imageInfo.yzw);
                Context.Instance.ImageInfo.Remove(imageInfo.yzw);
                if (imageInfo.w > 0)
                    Context.Instance.IndirectionTexture.RemoveVirtualImage(cmd, imageInfo);
            }

            foreach (int2 sector in RequestSectors)
            {
                if (!Context.Instance.AllSectors.ContainsKey(sector))
                    Context.Instance.AllSectors.Add(sector, int4.zero);
            }
        }

        private static bool CameraPositionChanged(float3 cameraPosition)
        {
            float2 cameraPositionXZ = cameraPosition.xz;
            if (math.lengthsq(_lastCameraPositionXZ - cameraPositionXZ) < Constant.CameraPositionSqrDeltaThreshold)
                return false;
            _lastCameraPositionXZ = cameraPositionXZ;
            return true;
        }

        private static void ReallocateVirtualImage(CommandBuffer cmd)
        {
            DelayRemoveImages.Clear();
            foreach (var pair in Context.Instance.AllSectors)
            {
                int2 sector = pair.Key;
                int4 oldImageInfo = pair.Value;
                int virtualImageSize = Utility.CalculateTargetImageSize(sector * 64 + 32, _lastCameraPositionXZ);
                int oldVirtualImageSize = oldImageInfo.w << Constant.PageSizeShift;
                if (virtualImageSize == oldVirtualImageSize)
                    continue;

                Context.Instance.VirtualImageAtlas.InsertImage(sector, virtualImageSize, out int4 imageInfo);
                DelayRemoveImages[sector] = (oldImageInfo, imageInfo);
            }

            foreach (var pair in DelayRemoveImages)
            {
                int2 sector = pair.Key;
                int4 oldImageInfo = pair.Value.oldInfo;
                int4 newImageInfo = pair.Value.newInfo;
                Context.Instance.ImageInfo2Sector.Remove(oldImageInfo.yzw);
                Context.Instance.ImageInfo.Remove(oldImageInfo.yzw);
                Context.Instance.AllSectors.Remove(sector);
                if (newImageInfo.w > 0)
                {
                    Context.Instance.ImageInfo2Sector[newImageInfo.yzw] = sector;
                    Context.Instance.ImageInfo.Add(newImageInfo.yzw);
                    Context.Instance.AllSectors[sector] = newImageInfo;
                }

                if (oldImageInfo.w > 0)
                    Context.Instance.IndirectionTexture.RemapVirtualImage(cmd, oldImageInfo, newImageInfo);
                Context.Instance.VirtualImageAtlas.RemoveImage(oldImageInfo);
            }

            foreach (var pair in Context.Instance.AllSectors)
            {
                int4 imageInfo = pair.Value;
                uint virtualPageSizeLog = (uint)math.ceillog2(imageInfo.w);
                uint encoded = ((uint)imageInfo.y << 20) + ((uint)imageInfo.z << 8) + virtualPageSizeLog;
                Context.Instance.Sector2VirtualImageInfoTexture.UpdateSector(pair.Key, encoded);
            }

            Context.Instance.Sector2VirtualImageInfoTexture.UpdateSector2VirtualImageInfo(cmd);
        }

        private static LRUCache<ulong, int> CreateCache()
        {
            var cache = new LRUCache<ulong, int>(Constant.MaxPhysicalPageCount);
            SeedFreeSlots(cache);
            return cache;
        }

        // Every physical slot starts as a sentinel entry so RemoveLast always has a victim to hand out.
        // Keys stay above 0x7FFF... so InvalidateRemovedPage can tell them apart from real pages.
        private static void SeedFreeSlots(LRUCache<ulong, int> cache)
        {
            for (int i = 0; i < Constant.MaxPhysicalPageCount; i++)
                cache.Insert(ulong.MaxValue - (ulong)i, i);
        }

        [BurstCompile]
        private struct DeduplicateJob : IJob
        {
            private NativeHashSet<uint> _deduplicateSet;
            [ReadOnly] private readonly NativeArray<uint> _readBackArray;
            private NativeList<uint> _deduplicatedPageID;
            [ReadOnly] private NativeHashSet<int3> _imageInfo;

            public DeduplicateJob(NativeHashSet<uint> deduplicateSet, NativeArray<uint> readBackArray, NativeList<uint> deduplicatedPageID, NativeHashSet<int3> imageInfo)
            {
                _deduplicateSet = deduplicateSet;
                _readBackArray = readBackArray;
                _deduplicatedPageID = deduplicatedPageID;
                _imageInfo = imageInfo;
            }

            public void Execute()
            {
                _deduplicateSet.Clear();
                _deduplicatedPageID.Clear();
                foreach (var packedPageID in _readBackArray)
                {
                    if (packedPageID == 0) continue;
                    _deduplicateSet.Add(packedPageID);
                }

                foreach (var imageInfo in _imageInfo)
                {
                    var virtualImageSizeLog = math.ceillog2(imageInfo.z);
                    uint additionalPageID =
                        (((uint)(imageInfo.x >> virtualImageSizeLog) & 0xFFF) << 20) +
                        (((uint)(imageInfo.y >> virtualImageSizeLog) & 0xFFF) << 8) +
                        ((uint)virtualImageSizeLog << 4) +
                        (uint)virtualImageSizeLog;
                    _deduplicateSet.Add(additionalPageID);
                }

                foreach (var pageID in _deduplicateSet)
                {
                    _deduplicatedPageID.Add(pageID);
                }
            }
        }

        [BurstCompile]
        private struct PackedPageIDComparer : IComparer<uint>
        {
            public int Compare(uint x, uint y)
            {
                int4 key0 = Utility.UnpackPageID(x);
                int4 key1 = Utility.UnpackPageID(y);
                return (key0.w - key0.z) - (key1.w - key1.z);
            }
        }
    }
}
