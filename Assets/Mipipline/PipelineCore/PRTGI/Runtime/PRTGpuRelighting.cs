using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Collections;

namespace Illusion.Rendering.PRTGI
{
    // URP 14 compute adapter. The original brick/probe transfer buffers keep their baked layout.
    internal sealed class PRTGpuRelighting : IPRTRelightRuntime
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct LightData
        {
            public Vector4 positionType, directionRange, colorSpot, attenuation;
            public uint renderingLayers, objectLayers;
            public int shadowIndex;
            public uint visibilityEpoch;
            public uint cacheIndex, flags;
        }

        private sealed class SectorBuffers : IDisposable
        {
            public readonly ComputeBuffer surfels, bricks, factors, probes, ids, sky, radiance, shadowCache;
            public readonly int BrickCount, ProbeCount, SurfelCount;
            public readonly Bounds SurfelBounds;
            private readonly ComputeBuffer[] _uploadBuffers;
            private Array[] _uploadData;
            private int _uploadIndex, _uploadOffset;
            public readonly long Bytes;
            public readonly int ShadowedLights;
            public bool Ready { get; private set; }
            public bool Evicting;
            public bool ReadbackFailed;
            public bool Relit;
            public long LastUsed;

            public SectorBuffers(PRTSectorData sector, int shadowedLights, uint[] previousCache)
            {
                BrickCount = sector.bricks.Length;
                ProbeCount = sector.probeIds.Length;
                SurfelCount = sector.surfels.Length;
                SurfelBounds = sector.surfelBounds;
                ShadowedLights = shadowedLights;
                surfels = Allocate(sector.surfels.Length, Surfel.Stride);
                bricks = Allocate(sector.bricks.Length, SurfelIndices.Stride);
                factors = Allocate(sector.factors.Length, BrickFactor.Stride);
                probes = Allocate(sector.probes.Length, PRTProbeData.Stride);
                ids = Allocate(sector.probeIds.Length, sizeof(int));
                sky = Allocate(sector.skyVisibility.Length, sizeof(uint));
                radiance = new ComputeBuffer(Mathf.Max(1, sector.bricks.Length), 16);
                int entries = checked(shadowedLights * sector.surfels.Length);
                shadowCache = Allocate(entries, sizeof(uint));
                _uploadBuffers = new[] { surfels, bricks, factors, probes, ids, sky, shadowCache };
                _uploadData = new Array[] { sector.surfels, sector.bricks, sector.factors, sector.probes,
                    sector.probeIds, sector.skyVisibility,
                    previousCache != null && previousCache.Length == entries ? previousCache : new uint[entries] };
                Bytes = Estimate(sector, shadowedLights);
            }

            public static long Estimate(PRTSectorData data, int shadowedLights) =>
                (long)Mathf.Max(1, data.surfels.Length) * Surfel.Stride +
                (long)Mathf.Max(1, data.bricks.Length) * (SurfelIndices.Stride + 16) +
                (long)Mathf.Max(1, data.factors.Length) * BrickFactor.Stride +
                (long)Mathf.Max(1, data.probes.Length) * PRTProbeData.Stride +
                (long)Mathf.Max(1, data.probeIds.Length) * sizeof(int) +
                (long)Mathf.Max(1, data.skyVisibility.Length) * sizeof(uint) +
                (long)Mathf.Max(1, shadowedLights * data.surfels.Length) * sizeof(uint);

            public static long Estimate(PRTSectorIndex data, int shadowedLights) =>
                (long)Mathf.Max(1, data.surfelCount) * Surfel.Stride +
                (long)Mathf.Max(1, data.brickCount) * (SurfelIndices.Stride + 16) +
                (long)Mathf.Max(1, data.factorCount) * BrickFactor.Stride +
                (long)Mathf.Max(1, data.probeCount) * (PRTProbeData.Stride + sizeof(int)) +
                (long)Mathf.Max(1, data.skyWordCount) * sizeof(uint) +
                (long)Mathf.Max(1, shadowedLights * data.surfelCount) * sizeof(uint);

            private static ComputeBuffer Allocate(int count, int stride) =>
                new ComputeBuffer(Mathf.Max(1, count), stride);

            public int Upload(int budget)
            {
                int used = 0;
                while (_uploadIndex < _uploadBuffers.Length)
                {
                    Array values = _uploadData[_uploadIndex];
                    ComputeBuffer buffer = _uploadBuffers[_uploadIndex];
                    int left = values.Length - _uploadOffset;
                    if (left == 0) { _uploadIndex++; _uploadOffset = 0; continue; }
                    int count = Mathf.Min(left, (budget - used) / buffer.stride);
                    if (count <= 0) break;
                    buffer.SetData(values, _uploadOffset, _uploadOffset, count);
                    _uploadOffset += count;
                    used += count * buffer.stride;
                }
                Ready = _uploadIndex == _uploadBuffers.Length;
                if (Ready) _uploadData = null;
                return used;
            }

            public void Dispose()
            {
                surfels.Dispose(); bricks.Dispose(); factors.Dispose(); probes.Dispose();
                ids.Dispose(); sky.Dispose(); radiance.Dispose(); shadowCache.Dispose();
            }
        }

        private sealed class CameraPublication : IDisposable
        {
            public readonly RenderTexture Texture;
            public readonly Vector3Int[] Minimum;
            public readonly uint[] Revisions;
            public long LastUsed;
            public bool Initialized;

            public CameraPublication(PRTCascadeLayout layout, int sectorCount)
            {
                Minimum = new Vector3Int[layout.CascadeCount];
                Revisions = new uint[sectorCount];
                Texture = new RenderTexture(layout.Slots.x, layout.Slots.z, 0, RenderTextureFormat.ARGBHalf)
                {
                    name = "PRT camera SH", dimension = TextureDimension.Tex3D,
                    volumeDepth = layout.TextureDepth, enableRandomWrite = true,
                    wrapModeU = TextureWrapMode.Repeat, wrapModeV = TextureWrapMode.Repeat,
                    wrapModeW = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.HideAndDontSave
                };
                if (!Texture.Create()) throw new InvalidOperationException("PRT GPU relight could not allocate a camera 3D SH atlas.");
            }

            public void Dispose()
            {
                if (!Texture) return;
                Texture.Release();
                if (Application.isPlaying) UnityEngine.Object.Destroy(Texture);
                else UnityEngine.Object.DestroyImmediate(Texture);
            }
        }

        private readonly PRTProbeVolume _volume;
        private readonly PRTProbeVolumeAsset _asset;
        private readonly PRTSectorStream _stream;
        private readonly ComputeShader _shader;
        private readonly int _clear, _bricks, _probes, _pyramidKernel, _publish;
        private readonly PRTSectorScheduler _scheduler;
        private readonly SectorBuffers[] _sectors;
        private readonly List<(int index, SectorBuffers buffers, AsyncGPUReadbackRequest request)> _evictions = new();
        private readonly Dictionary<int, uint[]> _savedCache = new();
        private readonly Dictionary<int, long> _savedCacheUsage = new();
        private long _savedCacheBytes;
        private readonly HashSet<int> _dispatchedThisTick = new();
        private readonly List<int> _updatedSectorIds = new();
        private readonly Dictionary<Camera, CameraPublication> _publications = new();
        private CameraPublication _activePublication;
        private long _renderTick;
        private readonly PRTEnvironmentCapture _environment;
        private readonly ComputeBuffer _committed, _directions, _metadata, _ready, _debugOffsets;
        private readonly ComputeBuffer _shadowStats;
        private static readonly uint[] EmptyShadowStats = new uint[5];
        private readonly ComputeBuffer[] _pyramid;
        private readonly uint[] _revisions;
        private ComputeBuffer _lights;
        private readonly List<LightData> _lightData = new List<LightData>();
        private readonly Dictionary<int, (int hash, uint epoch)> _lightEpochs = new();
        private uint _nextEpoch;
        private long _total;
        private long _residentBytes, _peakBytes, _usage;
        private int _shadowedLights, _frameUploadBytes;
        private string _pressure;
        private int _lastRelightFrame = -1;
        private bool _statsPending, _disposed;
        private int _shadowStatsTick;
        private bool _shadowDebugPending;
        private int _lastShadowDebugTick = -1000000;
        private string _shadowDebugLightName;
        private uint _shadowDebugEpoch;
        private uint _shadowDebugCacheIndex = uint.MaxValue;

        public Texture Coefficients => _activePublication?.Texture;
        internal ComputeBuffer DebugSH => _committed;
        internal ComputeBuffer DebugMetadata => _metadata;
        internal ComputeBuffer DebugOffsets => _debugOffsets;
        public PRTCascadeLayout Layout { get; }
        public string Backend => "GPU Compute";
        public long TotalSectorUpdates => _total;
        public int NearSector => _scheduler.NearSector;
        public int BackgroundSector => _scheduler.BackgroundSector;
        public int UpdatedSectorsThisFrame { get; private set; }
        public long ResidentBytes => _residentBytes;
        public long PeakResidentBytes => _peakBytes;
        public int FrameUploadBytes => _frameUploadBytes;
        public int UploadingSectors { get; private set; }
        public int EvictingSectors => _evictions.Count;
        public string ResidencyPressure => _pressure;
        public int PublishedCellsThisFrame { get; private set; }
        public long CachedCpuSectorBytes => _stream.CachedBytes + _savedCacheBytes;
        public int PendingCpuSectors => _stream.PendingCount;

        public PRTGpuRelighting(PRTProbeVolume volume, ComputeShader shader)
        {
            _volume = volume;
            _asset = volume.asset;
            _stream = new PRTSectorStream(_asset);
            _shader = shader;
            _environment = new PRTEnvironmentCapture();
            _clear = shader.FindKernel("CSClear");
            _bricks = shader.FindKernel("CSBricks");
            _probes = shader.FindKernel("CSProbes");
            _pyramidKernel = shader.FindKernel("CSPyramid");
            _publish = shader.FindKernel("CSPublish");
            if (!shader.IsSupported(_clear) || !shader.IsSupported(_bricks) || !shader.IsSupported(_probes) ||
                !shader.IsSupported(_pyramidKernel) || !shader.IsSupported(_publish))
                throw new NotSupportedException("PRT GPU relight kernels failed to compile for the active graphics API.");
            _scheduler = new PRTSectorScheduler(_asset);
            _sectors = new SectorBuffers[_asset.SectorCount];
            _revisions = new uint[_sectors.Length];
            PRTProbeGrid grid = _asset.Grid;
            Layout = new PRTCascadeLayout(grid, volume.voxelProbeSize, volume.cascadeCount);
            _committed = new ComputeBuffer(grid.ProbeCount * 9, 16);
            _committed.SetData(new Vector4[grid.ProbeCount * 9]);
            _ready = new ComputeBuffer(grid.ProbeCount, sizeof(uint));
            _ready.SetData(new uint[grid.ProbeCount]);
            _shadowStats = new ComputeBuffer(5, sizeof(uint));
            _shadowStats.SetData(EmptyShadowStats);
            _metadata = new ComputeBuffer(grid.ProbeCount, sizeof(uint));
            var validity = new uint[grid.ProbeCount];
            _debugOffsets = new ComputeBuffer(grid.ProbeCount, 16);
            var offsets = new Vector4[grid.ProbeCount];
            for (int i = 0; i < validity.Length; i++) validity[i] = _asset.Probes[i].validity;
            for (int i = 0; i < offsets.Length; i++) offsets[i] = _asset.Probes[i].captureOffset;
            _metadata.SetData(validity);
            _debugOffsets.SetData(offsets);
            _pyramid = new ComputeBuffer[Layout.CascadeCount];
            for (int level = 1; level < _pyramid.Length; level++)
            {
                Vector3Int size = Layout.LevelCount(level);
                int count = checked(size.x * size.y * size.z * 9);
                _pyramid[level] = new ComputeBuffer(count, 16);
                _pyramid[level].SetData(new Vector4[count]);
            }
            var directions = PRTBakeSampling.GenerateDirections(_asset.Signature.sampleCount, _asset.Signature.seed);
            _directions = new ComputeBuffer(directions.Length, 16);
            _directions.SetData(directions);
            _lights = new ComputeBuffer(1, Marshal.SizeOf<LightData>());
        }

        public void Tick(Camera camera, CommandBuffer cmd, ref RenderingData renderingData)
        {
            cmd.BeginSample("PRTGI GPU Relight");
            _frameUploadBytes = 0;
            _shadowStatsTick++;
            _pressure = null;
            CollectEvictions();
            Layout.Recenter(camera.transform.position);
            if (!_publications.TryGetValue(camera, out _activePublication))
            {
                _activePublication = new CameraPublication(Layout, _sectors.Length);
                _publications.Add(camera, _activePublication);
            }
            _activePublication.LastUsed = ++_renderTick;
            bool updateRelight = !Application.isPlaying || _lastRelightFrame != Time.frameCount;
            PRTProbeGrid grid = _asset.Grid;
            Vector3Int slots = Layout.Slots;
            cmd.SetComputeIntParams(_shader, "_PRTGridDimensions", grid.count.x, grid.count.y, grid.count.z);
            cmd.SetComputeIntParams(_shader, "_PRTSlots", slots.x, slots.y, slots.z);
            cmd.SetComputeIntParam(_shader, "_PRTCascadeCount", Layout.CascadeCount);
            cmd.SetComputeTextureParam(_shader, _clear, "_coefficientVoxel3D", _activePublication.Texture);
            UpdatedSectorsThisFrame = 0;
            _dispatchedThisTick.Clear();
            _updatedSectorIds.Clear();
            if (updateRelight)
            {
                UploadLights(ref renderingData);
                _environment.Refresh(cmd, renderingData.cameraData.GetViewMatrix(), renderingData.cameraData.GetProjectionMatrix());
                cmd.SetComputeTextureParam(_shader, _probes, "_PRTEnvironment", _environment.Cube);
                cmd.SetComputeFloatParam(_shader, "_PRTEnvironmentIntensity", _environment.Intensity);
                cmd.SetComputeIntParam(_shader, "_PRTLightCount", _lightData.Count);
                cmd.SetComputeIntParam(_shader, "_PRTDirectionCount", _directions.count);
                cmd.SetComputeIntParam(_shader, "_PRTShadowEnabled", _volume.enableRelightShadow ? 1 : 0);
                cmd.SetComputeIntParam(_shader, "_PRTCollectStats", _volume.enableShadowCacheStats ? 1 : 0);
                if (_volume.enableShadowCacheStats) cmd.SetBufferData(_shadowStats, EmptyShadowStats);
                var mainShadow = Shader.GetGlobalTexture("_MainLightShadowmapTexture");
                var additionalShadow = Shader.GetGlobalTexture("_AdditionalLightsShadowmapTexture");
                if (mainShadow) cmd.SetComputeTextureParam(_shader, _bricks, "_MainLightShadowmapTexture", mainShadow);
                if (additionalShadow) cmd.SetComputeTextureParam(_shader, _bricks, "_AdditionalLightsShadowmapTexture", additionalShadow);
                cmd.SetComputeBufferParam(_shader, _bricks, "_PRTCommittedSH", _committed);
                cmd.SetComputeBufferParam(_shader, _bricks, "_PRTReady", _ready);
                cmd.SetComputeBufferParam(_shader, _bricks, "_PRTValidityMasks", _metadata);
                cmd.SetComputeBufferParam(_shader, _bricks, "_PRTLights", _lights);
                cmd.SetComputeBufferParam(_shader, _bricks, "_PRTShadowStats", _shadowStats);
                cmd.SetComputeBufferParam(_shader, _probes, "_PRTCommittedSH", _committed);
                cmd.SetComputeBufferParam(_shader, _probes, "_PRTReady", _ready);
                cmd.SetComputeBufferParam(_shader, _probes, "_PRTDirections", _directions);
                int uploadBudget = checked(Mathf.Max(1, _volume.uploadBudgetMiB) * 1048576);
                long residentBudget = (long)Mathf.Max(1, _volume.sectorBudgetMiB) * 1048576;
                var requested = _scheduler.Select(camera.transform.position, Mathf.Max(1, _volume.sectorsPerFrame));
                foreach (int index in requested)
                {
                    SectorBuffers sector = RequestSector(index, requested, residentBudget);
                    if (sector == null) continue;
                    sector.LastUsed = ++_usage;
                    if (!sector.Ready)
                        _frameUploadBytes += sector.Upload(uploadBudget - _frameUploadBytes);
                    if (!sector.Ready) continue;
                    cmd.SetComputeIntParam(_shader, "_PRTBrickCount", sector.BrickCount);
                    cmd.SetComputeIntParam(_shader, "_PRTProbeCount", sector.ProbeCount);
                    cmd.SetComputeIntParam(_shader, "_PRTSurfelCount", sector.SurfelCount);
                    cmd.SetComputeVectorParam(_shader, "_PRTSurfelMin", sector.SurfelBounds.min);
                    cmd.SetComputeVectorParam(_shader, "_PRTSurfelSize", sector.SurfelBounds.size);
                    cmd.SetComputeBufferParam(_shader, _bricks, "_PRTSurfels", sector.surfels);
                    cmd.SetComputeBufferParam(_shader, _bricks, "_PRTBricks", sector.bricks);
                    cmd.SetComputeBufferParam(_shader, _bricks, "_PRTWorldShadowCache", sector.shadowCache);
                    cmd.SetComputeBufferParam(_shader, _bricks, "_PRTBrickRadiance", sector.radiance);
                    if (sector.BrickCount > 0)
                        cmd.DispatchCompute(_shader, _bricks, (sector.BrickCount + 31) / 32, 1, 1);
                    cmd.SetComputeBufferParam(_shader, _probes, "_PRTProbes", sector.probes);
                    cmd.SetComputeBufferParam(_shader, _probes, "_PRTProbeIds", sector.ids);
                    cmd.SetComputeBufferParam(_shader, _probes, "_PRTFactors", sector.factors);
                    cmd.SetComputeBufferParam(_shader, _probes, "_PRTSkyVisibility", sector.sky);
                    cmd.SetComputeBufferParam(_shader, _probes, "_PRTBrickRadiance", sector.radiance);
                    cmd.DispatchCompute(_shader, _probes, (sector.ProbeCount + 31) / 32, 1, 1);
                    _scheduler.Updated(index);
                    _dispatchedThisTick.Add(index);
                    _updatedSectorIds.Add(index);
                    _revisions[index]++;
                    sector.Relit = true;
                    UpdatedSectorsThisFrame++;
                    _total++;
                }
                _lastRelightFrame = Time.frameCount;
                long cpuBudget = (long)Mathf.Max(1, _volume.cpuSectorBudgetMiB) * 1048576;
                TrimSavedCache(cpuBudget);
                _stream.Trim(Math.Max(1, cpuBudget - _savedCacheBytes), _scheduler.NearSector);
                if (!string.IsNullOrEmpty(_stream.LastError)) _pressure = _stream.LastError;
                if (_volume.enableShadowCacheStats && !_statsPending &&
                    _shadowStatsTick % Mathf.Max(1, _volume.shadowCacheStatsReadbackInterval) == 0)
                {
                    _statsPending = true;
                    cmd.RequestAsyncReadback(_shadowStats, request =>
                    {
                        _statsPending = false;
                        if (_disposed || !_volume || request.hasError) return;
                        var values = request.GetData<uint>();
                        if (values.Length >= 5)
                            _volume.UpdateShadowStats(values[0], values[1], values[2], values[3], values[4]);
                    });
                }
                RequestShadowDebug(cmd);
            }
            UploadingSectors = 0;
            foreach (SectorBuffers sector in _sectors)
                if (sector != null && !sector.Ready && !sector.Evicting) UploadingSectors++;
            BuildPyramid(cmd);
            Publish(cmd);
            PruneUnusedCameras(camera);
            cmd.EndSample("PRTGI GPU Relight");
        }

        private void RequestShadowDebug(CommandBuffer cmd)
        {
            if (_volume.VisualizationMode != PRTProbeVisualizationMode.ShadowCache || _shadowDebugPending ||
                _shadowDebugCacheIndex == uint.MaxValue || _shadowStatsTick - _lastShadowDebugTick < _volume.ShadowDebugReadbackInterval)
                return;
            int selected = _volume.SelectedProbeIndex;
            int sectorIndex = selected >= 0 && selected < _asset.Grid.ProbeCount
                ? _asset.SectorIndex(selected) : _scheduler.NearSector;
            SectorBuffers sector = _sectors[sectorIndex];
            if (sector == null || !sector.Ready || sector.Evicting || sector.SurfelCount == 0) return;
            int surfelCount = sector.SurfelCount;
            int offset = checked((int)_shadowDebugCacheIndex * surfelCount);
            if (offset + surfelCount > sector.shadowCache.count) return;
            uint epoch = _shadowDebugEpoch;
            string name = _shadowDebugLightName;
            int frame = Time.frameCount;
            _lastShadowDebugTick = _shadowStatsTick;
            _shadowDebugPending = true;
            cmd.RequestAsyncReadback(sector.shadowCache, request =>
            {
                _shadowDebugPending = false;
                if (_disposed || !_volume || request.hasError) return;
                var values = request.GetData<uint>();
                if (offset + surfelCount > values.Length) return;
                var snapshot = new uint[surfelCount];
                for (int i = 0; i < surfelCount; i++) snapshot[i] = values[offset + i];
                _volume.UpdateShadowDebug(sectorIndex, name, epoch, frame, snapshot);
            });
        }

        private SectorBuffers RequestSector(int index, IReadOnlyList<int> requested, long budget)
        {
            SectorBuffers resident = _sectors[index];
            if (resident != null)
            {
                if (resident.Evicting) return null;
                if (resident.ShadowedLights == _shadowedLights) return resident;
                QueueEviction(index, resident);
                return null;
            }
            PRTSectorIndex info = _asset.SectorInfo(index);
            long bytes = SectorBuffers.Estimate(info, _shadowedLights);
            if (bytes > budget)
            {
                _pressure = $"Sector {index} needs {bytes} bytes, exceeding its {budget}-byte resident budget.";
                _scheduler.Skip(index);
                return null;
            }
            if (_residentBytes + bytes > budget)
            {
                SectorBuffers oldest = null;
                int oldestIndex = -1;
                for (int i = 0; i < _sectors.Length; i++)
                {
                    var candidate = _sectors[i];
                    if (candidate == null || candidate.Evicting || candidate.ReadbackFailed ||
                        i == index || _dispatchedThisTick.Contains(i)) continue;
                    if (requested.Contains(i) && !candidate.Relit) continue;
                    if (oldest == null || candidate.LastUsed < oldest.LastUsed)
                    { oldest = candidate; oldestIndex = i; }
                }
                if (oldest != null) QueueEviction(oldestIndex, oldest);
                _pressure = $"Sector {index} is waiting for residency: {_residentBytes}/{budget} bytes allocated.";
                return null;
            }
            PRTSectorData transport = _stream.Request(index);
            if (transport == null) return null;
            _savedCache.TryGetValue(index, out uint[] saved);
            if (saved != null) _savedCacheBytes -= (long)saved.Length * sizeof(uint);
            resident = new SectorBuffers(transport, _shadowedLights, saved);
            _savedCache.Remove(index);
            _savedCacheUsage.Remove(index);
            _sectors[index] = resident;
            _residentBytes += resident.Bytes;
            _peakBytes = Math.Max(_peakBytes, _residentBytes);
            return resident;
        }

        private void QueueEviction(int index, SectorBuffers sector)
        {
            if (sector.Evicting) return;
            sector.Evicting = true;
            _evictions.Add((index, sector, AsyncGPUReadback.Request(sector.shadowCache)));
        }

        private void CollectEvictions()
        {
            for (int i = _evictions.Count - 1; i >= 0; i--)
            {
                var pending = _evictions[i];
                if (!pending.request.done) continue;
                if (pending.request.hasError)
                {
                    pending.buffers.Evicting = false;
                    pending.buffers.ReadbackFailed = true;
                    _pressure = $"Sector {pending.index} shadow-cache readback failed; retaining resident buffers.";
                    _evictions.RemoveAt(i);
                    continue;
                }
                var source = pending.request.GetData<uint>();
                var cache = new uint[source.Length];
                source.CopyTo(cache);
                if (_savedCache.TryGetValue(pending.index, out var previous))
                    _savedCacheBytes -= (long)previous.Length * sizeof(uint);
                _savedCache[pending.index] = cache;
                _savedCacheUsage[pending.index] = ++_usage;
                _savedCacheBytes += (long)cache.Length * sizeof(uint);
                _sectors[pending.index] = null;
                _residentBytes -= pending.buffers.Bytes;
                pending.buffers.Dispose();
                _evictions.RemoveAt(i);
            }
        }

        private void TrimSavedCache(long budget)
        {
            while (_savedCacheBytes > budget && _savedCache.Count > 0)
            {
                int oldest = -1;
                long stamp = long.MaxValue;
                foreach (var pair in _savedCacheUsage)
                    if (pair.Value < stamp) { oldest = pair.Key; stamp = pair.Value; }
                if (oldest < 0) break;
                _savedCacheBytes -= (long)_savedCache[oldest].Length * sizeof(uint);
                _savedCache.Remove(oldest);
                _savedCacheUsage.Remove(oldest);
            }
        }

        private void BuildPyramid(CommandBuffer cmd)
        {
            if (_updatedSectorIds.Count == 0) return;
            cmd.SetComputeBufferParam(_shader, _pyramidKernel, "_PRTCommittedSH", _committed);
            cmd.SetComputeBufferParam(_shader, _pyramidKernel, "_PRTReady", _ready);
            cmd.SetComputeBufferParam(_shader, _pyramidKernel, "_PRTValidityMasks", _metadata);
            foreach (int sectorIndex in _updatedSectorIds)
            {
                int width = _asset.SectorWidth;
                Vector2Int coordinate = _asset.SectorCoordinate(sectorIndex);
                Vector3Int low = new Vector3Int(coordinate.x * width, 0, coordinate.y * width);
                Vector3Int high = Vector3Int.Min(low + new Vector3Int(width, _asset.Grid.count.y, width), _asset.Grid.count);
                for (int level = 1; level < Layout.CascadeCount; level++)
                {
                    Vector3Int source = Layout.LevelCount(level - 1), target = Layout.LevelCount(level);
                    low = new Vector3Int(low.x / 2, low.y / 2, low.z / 2);
                    high = Vector3Int.Min(target, new Vector3Int(high.x / 2 + 1, high.y / 2 + 1, high.z / 2 + 1));
                    Vector3Int size = high - low;
                    cmd.SetComputeIntParams(_shader, "_PRTSourceCount", source.x, source.y, source.z);
                    cmd.SetComputeIntParams(_shader, "_PRTTargetCount", target.x, target.y, target.z);
                    cmd.SetComputeIntParams(_shader, "_PRTPyramidOffset", low.x, low.y, low.z);
                    cmd.SetComputeIntParams(_shader, "_PRTPyramidSize", size.x, size.y, size.z);
                    cmd.SetComputeIntParam(_shader, "_PRTFromProbes", level == 1 ? 1 : 0);
                    cmd.SetComputeBufferParam(_shader, _pyramidKernel, "_PRTPyramidSource", level == 1 ? _committed : _pyramid[level - 1]);
                    cmd.SetComputeBufferParam(_shader, _pyramidKernel, "_PRTPyramidTarget", _pyramid[level]);
                    cmd.DispatchCompute(_shader, _pyramidKernel,
                        (size.x + 3) / 4, (size.y + 3) / 4, (size.z + 3) / 4);
                }
            }
        }

        private void Publish(CommandBuffer cmd)
        {
            Vector3Int slots = Layout.Slots;
            PublishedCellsThisFrame = 0;
            CameraPublication publication = _activePublication;
            bool rebuild = !publication.Initialized;
            for (int level = 0; level < Layout.CascadeCount && !rebuild; level++)
            {
                Vector3Int step = Layout.Minimum(level) - publication.Minimum[level];
                Vector3Int size = Layout.WindowCount(level);
                rebuild = step.y != 0 || Mathf.Abs(step.x) >= size.x || Mathf.Abs(step.z) >= size.z;
            }
            if (rebuild)
                cmd.DispatchCompute(_shader, _clear, (slots.x + 3) / 4, (slots.y + 3) / 4, (slots.z + 3) / 4);
            cmd.SetComputeBufferParam(_shader, _publish, "_PRTCommittedSH", _committed);
            cmd.SetComputeBufferParam(_shader, _publish, "_PRTReady", _ready);
            cmd.SetComputeBufferParam(_shader, _publish, "_PRTValidityMasks", _metadata);
            cmd.SetComputeTextureParam(_shader, _publish, "_coefficientVoxel3D", publication.Texture);
            for (int level = 0; level < Layout.CascadeCount; level++)
            {
                Vector3Int source = Layout.LevelCount(level), minimum = Layout.Minimum(level), size = Layout.WindowCount(level);
                cmd.SetComputeIntParam(_shader, "_PRTPublishLevel", level);
                cmd.SetComputeBufferParam(_shader, _publish, "_PRTPyramidSource", level == 0 ? _committed : _pyramid[level]);
                if (rebuild) PublishRegion(cmd, source, minimum, size, minimum);
                else
                {
                    Vector3Int step = minimum - publication.Minimum[level];
                    if (step.x != 0)
                        PublishRegion(cmd, source, new Vector3Int(step.x > 0 ? publication.Minimum[level].x + size.x : minimum.x,
                            minimum.y, minimum.z), new Vector3Int(Mathf.Abs(step.x), size.y, size.z), minimum);
                    if (step.z != 0)
                        PublishRegion(cmd, source, new Vector3Int(minimum.x, minimum.y,
                            step.z > 0 ? publication.Minimum[level].z + size.z : minimum.z),
                            new Vector3Int(size.x, size.y, Mathf.Abs(step.z)), minimum);
                    for (int sector = 0; sector < _revisions.Length; sector++)
                    {
                        if (publication.Revisions[sector] == _revisions[sector]) continue;
                        Vector2Int coordinate = _asset.SectorCoordinate(sector);
                        int width = _asset.SectorWidth;
                        var low = new Vector3Int(coordinate.x * width, 0, coordinate.y * width);
                        var high = Vector3Int.Min(low + new Vector3Int(width, _asset.Grid.count.y, width), _asset.Grid.count);
                        for (int coarse = 1; coarse <= level; coarse++)
                        {
                            low = new Vector3Int(low.x / 2, low.y / 2, low.z / 2);
                            high = Vector3Int.Min(Layout.LevelCount(coarse),
                                new Vector3Int(high.x / 2 + 1, high.y / 2 + 1, high.z / 2 + 1));
                        }
                        Vector3Int start = Vector3Int.Max(minimum, low);
                        Vector3Int end = Vector3Int.Min(minimum + size, high);
                        Vector3Int region = end - start;
                        if (region.x > 0 && region.y > 0 && region.z > 0)
                            PublishRegion(cmd, source, start, region, minimum);
                    }
                }
                publication.Minimum[level] = minimum;
            }
            Array.Copy(_revisions, publication.Revisions, _revisions.Length);
            publication.Initialized = true;
        }

        private void PruneUnusedCameras(Camera current)
        {
            if (_renderTick % 60 != 0) return;
            var expired = new List<Camera>();
            foreach (var pair in _publications)
                if (pair.Key != current && (!pair.Key || _renderTick - pair.Value.LastUsed > 120))
                    expired.Add(pair.Key);
            foreach (Camera camera in expired)
            {
                _publications[camera].Dispose();
                _publications.Remove(camera);
            }
        }

        private void PublishRegion(CommandBuffer cmd, Vector3Int source, Vector3Int start,
            Vector3Int count, Vector3Int windowMinimum)
        {
            cmd.SetComputeIntParams(_shader, "_PRTSourceCount", source.x, source.y, source.z);
            cmd.SetComputeIntParams(_shader, "_PRTPublishMinimum", start.x, start.y, start.z);
            cmd.SetComputeIntParams(_shader, "_PRTPublishCount", count.x, count.y, count.z);
            cmd.SetComputeIntParams(_shader, "_PRTPublishWindowMinimum", windowMinimum.x, windowMinimum.y, windowMinimum.z);
            cmd.DispatchCompute(_shader, _publish, (count.x + 7) / 8, (count.y + 3) / 4, (count.z + 7) / 8);
            PublishedCellsThisFrame += count.x * count.y * count.z;
        }

        private void UploadLights(ref RenderingData renderingData)
        {
            _lightData.Clear();
            _shadowedLights = 0;
            _shadowDebugCacheIndex = uint.MaxValue;
            _shadowDebugLightName = null;
            var visibleLights = renderingData.lightData.visibleLights;
            int mainIndex = renderingData.lightData.mainLightIndex;
            foreach (Light light in UnityEngine.Object.FindObjectsOfType<Light>())
            {
                if (!light.isActiveAndEnabled || light.gameObject.scene != _volume.gameObject.scene ||
                    light.type != LightType.Directional && light.type != LightType.Point && light.type != LightType.Spot) continue;
                float type = light.type == LightType.Directional ? 0 : light.type == LightType.Point ? 1 : 2;
                Color color = light.color.linear * (light.intensity * light.bounceIntensity);
                if (light.useColorTemperature)
                    color *= Mathf.CorrelatedColorTemperatureToRGB(light.colorTemperature);
                float cutoff = Mathf.Cos(light.spotAngle * 0.5f * Mathf.Deg2Rad);
                int shadowIndex = -1;
                bool castsShadow = _volume.enableRelightShadow && light.shadows != LightShadows.None && light.shadowStrength > 0f;
                uint cacheIndex = uint.MaxValue, epoch = 0;
                if (castsShadow)
                {
                    cacheIndex = (uint)_shadowedLights++;
                    shadowIndex = -3;
                    int id = light.GetInstanceID();
                    int hash = HashCode.Combine(light.transform.position, light.transform.rotation,
                        light.range, light.spotAngle, light.shadowStrength, light.cullingMask);
                    if (!_lightEpochs.TryGetValue(id, out var old) || old.hash != hash)
                    {
                        _nextEpoch = (_nextEpoch + 1) & 0xffffffu;
                        if (_nextEpoch == 0) _nextEpoch = 1;
                        old = (hash, _nextEpoch);
                        _lightEpochs[id] = old;
                    }
                    epoch = old.epoch;
                }
                if (castsShadow)
                    for (int i = 0; i < visibleLights.Length; i++)
                        if (visibleLights[i].light == light)
                        {
                            shadowIndex = i == mainIndex ? -2 : i < mainIndex || mainIndex < 0 ? i : i - 1;
                            break;
                        }
                if (castsShadow && shadowIndex >= -2 && _shadowDebugCacheIndex == uint.MaxValue)
                {
                    _shadowDebugCacheIndex = cacheIndex;
                    _shadowDebugLightName = light.name;
                    _shadowDebugEpoch = epoch;
                }
                _lightData.Add(new LightData
                {
                    positionType = new Vector4(light.transform.position.x, light.transform.position.y,
                        light.transform.position.z, type),
                    directionRange = new Vector4(-light.transform.forward.x, -light.transform.forward.y,
                        -light.transform.forward.z, light.range),
                    colorSpot = new Vector4(color.r, color.g, color.b, 0),
                    attenuation = new Vector4(cutoff, Mathf.Min(1f, cutoff + 0.1f), 0, 0),
                    renderingLayers = unchecked((uint)light.renderingLayerMask),
                    objectLayers = unchecked((uint)light.cullingMask),
                    shadowIndex = shadowIndex,
                    cacheIndex = cacheIndex,
                    visibilityEpoch = epoch,
                    flags = castsShadow ? 1u : 0u
                });
            }
            if (_lightData.Count > _lights.count)
            {
                _lights.Dispose();
                _lights = new ComputeBuffer(_lightData.Count, Marshal.SizeOf<LightData>());
            }
            if (_lightData.Count > 0) _lights.SetData(_lightData);
        }

        public void Dispose()
        {
            _disposed = true;
            foreach (var pending in _evictions)
            {
                pending.request.WaitForCompletion();
                pending.buffers.Dispose();
                _sectors[pending.index] = null;
            }
            _evictions.Clear();
            _savedCache.Clear();
            _savedCacheUsage.Clear();
            _stream.Dispose();
            _environment?.Dispose();
            _disposed = true;
            foreach (var sector in _sectors) sector?.Dispose();
            _committed?.Dispose();
            _metadata?.Dispose();
            _debugOffsets?.Dispose();
            _ready?.Dispose();
            _shadowStats?.Dispose();
            foreach (var level in _pyramid) level?.Dispose();
            _directions?.Dispose();
            _lights?.Dispose();
            foreach (var publication in _publications.Values) publication.Dispose();
            _publications.Clear();
            _activePublication = null;
        }
    }
}
