using UnityEngine;
using UnityEngine.Rendering;

namespace Illusion.Rendering.PRTGI
{
    public enum PRTProbeVisualizationMode { None, ProbeGrid, ProbeGridWithVirtualOffset, ProbeRadiance, ShadowCache }
    public enum PRTSelectedProbeDebugMode { IrradianceSphere, SphereDistribution, SampleDirection, Surfel, SurfelBrickGrid }

    // Preserves the Unity 6 scene's authoring data for the URP 14 bake and relight path.
    [ExecuteAlways]
    [AddComponentMenu("Rendering/PRT Probe Volume")]
    public sealed class PRTProbeVolume : MonoBehaviour
    {
        [Range(1, 128)] public int probeSizeX = 8;
        [Range(1, 64)] public int probeSizeY = 4;
        [Range(1, 128)] public int probeSizeZ = 8;
        [Range(0.1f, 100f)] public float probeGridSize = 2f;
        public bool enableBakePreprocess = true;
        public Vector3 virtualOffset;
        [Range(0, 1)] public float geometryBias = 0.1f;
        [Range(0, 1)] public float rayOriginBias = 0.1f;
        [Min(1)] public int sectorWidth = 4;
        public bool enableRelightShadow = true;
        public bool enableShadowCacheStats;
        [Min(1)] public int shadowCacheStatsReadbackInterval = 10;
        [Min(1)] public int sectorsPerFrame = 2;
        [Min(1)] public int sectorBudgetMiB = 256;
        [Min(1)] public int cpuSectorBudgetMiB = 32;
        [Min(1)] public int uploadBudgetMiB = 4;
        public Vector3Int voxelProbeSize = new Vector3Int(10, 5, 10);
        [Range(1, 4)] public int cascadeCount = 3;
        public PRTProbeVolumeAsset asset;
        [Tooltip("Use this scene's baked PRTGI for PipelineCore/PRTGI/Lit materials. When disabled, they use URP's regular GI.")]
        public bool usePRTGIForSceneObjects = true;
        public bool useGpuRelighting = true;
        [Range(0f, 1f)] public float indirectFill = 0.3f;

        public bool RuntimeRelightActive { get; private set; }
        public string RuntimeRelightBackend { get; private set; }
        public int RuntimeCascadeCount { get; private set; }
        public Vector3Int RuntimeCascadeSlots { get; private set; }
        public int RuntimeNearSector { get; private set; } = -1;
        public int RuntimeBackgroundSector { get; private set; }
        public int RuntimeUpdatedSectorsThisFrame { get; private set; }
        public long RuntimeTotalSectorUpdates { get; private set; }
        public int RuntimeLastRelightFrame { get; private set; } = -1;
        public float RuntimeRelightMilliseconds { get; private set; }
        public long RuntimeResidentBytes { get; private set; }
        public long RuntimeCachedCpuSectorBytes { get; private set; }
        public int RuntimePendingCpuSectors { get; private set; }
        public long RuntimePeakResidentBytes { get; private set; }
        public int RuntimeFrameUploadBytes { get; private set; }
        public int RuntimeUploadingSectors { get; private set; }
        public int RuntimeEvictingSectors { get; private set; }
        public int RuntimePublishedCells { get; private set; }
        public string RuntimeResidencyPressure { get; private set; }
        public bool RuntimeShadowStatsValid { get; private set; }
        public uint RuntimeShadowEvaluated { get; private set; }
        public uint RuntimeShadowCacheHits { get; private set; }
        public uint RuntimeShadowUncovered { get; private set; }
        public uint RuntimeShadowUnknown { get; private set; }
        public uint RuntimeShadowMapSamples { get; private set; }
        public bool HasShadowCacheDebugSnapshot { get; private set; }
        public int LatestShadowCacheDebugFrameIndex { get; private set; }
        public int ShadowPreviewSector { get; private set; } = -1;
        public string ShadowPreviewLightName { get; private set; }
        public uint ShadowPreviewEpoch { get; private set; }
        private uint[] _shadowDebugCache;

        internal void UpdateShadowDebug(int sector, string lightName, uint epoch, int frame, uint[] cache)
        {
            ShadowPreviewSector = sector;
            ShadowPreviewLightName = lightName;
            ShadowPreviewEpoch = epoch;
            LatestShadowCacheDebugFrameIndex = frame;
            _shadowDebugCache = cache;
            HasShadowCacheDebugSnapshot = true;
        }

        public bool TryGetShadowCacheSummary(int probeIndex, out float mean, out int cached, out int uncovered)
        {
            mean = 0f;
            cached = uncovered = 0;
            if (!HasShadowCacheDebugSnapshot || !asset || !asset.HasValidData ||
                probeIndex < 0 || probeIndex >= asset.Probes.Length || asset.SectorIndex(probeIndex) != ShadowPreviewSector)
                return false;
            var sector = asset.ReadSector(ShadowPreviewSector);
            var probe = sector.probes[asset.LocalProbeIndex(probeIndex)];
            for (int factor = probe.factorStart; factor < probe.factorStart + probe.factorCount; factor++)
            {
                var brick = sector.bricks[sector.factors[factor].BrickIndex];
                for (int index = brick.start; index < brick.start + brick.count; index++)
                {
                    if (index >= _shadowDebugCache.Length) continue;
                    uint entry = _shadowDebugCache[index];
                    if ((entry >> 8) == ShadowPreviewEpoch)
                    {
                        cached++;
                        mean += (entry & 255u) / 255f;
                    }
                    else { uncovered++; mean += 1f; }
                }
            }
            int total = cached + uncovered;
            if (total == 0) return false;
            mean /= total;
            return true;
        }

        public bool TryGetShadowSurfel(int sector, int index, out float visibility, out bool cached)
        {
            visibility = 1f;
            cached = false;
            if (!HasShadowCacheDebugSnapshot || sector != ShadowPreviewSector || _shadowDebugCache == null ||
                index < 0 || index >= _shadowDebugCache.Length) return false;
            uint entry = _shadowDebugCache[index];
            cached = (entry >> 8) == ShadowPreviewEpoch;
            if (cached) visibility = (entry & 255u) / 255f;
            return true;
        }

        internal void UpdateShadowStats(uint evaluated, uint hits, uint uncovered, uint unknown, uint mapSamples)
        {
            RuntimeShadowStatsValid = true;
            RuntimeShadowEvaluated = evaluated;
            RuntimeShadowCacheHits = hits;
            RuntimeShadowUncovered = uncovered;
            RuntimeShadowUnknown = unknown;
            RuntimeShadowMapSamples = mapSamples;
        }

        internal void ReportRelight(IPRTRelightRuntime relight, float milliseconds)
        {
            RuntimeRelightActive = true;
            RuntimeRelightBackend = relight.Backend;
            RuntimeCascadeCount = relight.Layout.CascadeCount;
            RuntimeCascadeSlots = relight.Layout.Slots;
            RuntimeNearSector = relight.NearSector;
            RuntimeBackgroundSector = relight.BackgroundSector;
            RuntimeUpdatedSectorsThisFrame = relight.UpdatedSectorsThisFrame;
            RuntimeTotalSectorUpdates = relight.TotalSectorUpdates;
            RuntimeLastRelightFrame = Time.frameCount;
            RuntimeRelightMilliseconds = milliseconds;
            RuntimeResidentBytes = relight.ResidentBytes;
            RuntimeCachedCpuSectorBytes = relight.CachedCpuSectorBytes;
            RuntimePendingCpuSectors = relight.PendingCpuSectors;
            RuntimePeakResidentBytes = relight.PeakResidentBytes;
            RuntimeFrameUploadBytes = relight.FrameUploadBytes;
            RuntimeUploadingSectors = relight.UploadingSectors;
            RuntimeEvictingSectors = relight.EvictingSectors;
            RuntimePublishedCells = relight.PublishedCellsThisFrame;
            RuntimeResidencyPressure = relight.ResidencyPressure;
        }

        internal void StopRelight()
        {
            RuntimeRelightActive = false;
            RuntimeRelightBackend = null;
            HasShadowCacheDebugSnapshot = false;
            _shadowDebugCache = null;
            _debugSH = _debugMetadata = _debugOffsets = null;
        }

        // Serialized editor settings from the original component.
        [SerializeField] private int bakeSampleCount = 2048;
        [SerializeField] private float surfelMergeDistance = 0.25f;
        [SerializeField] private int bakeSeed;
        [SerializeField] private int debugMode;
        [SerializeField] private int selectedProbeIndex = -1;
        [SerializeField] private int selectedProbeDebugMode;
        [SerializeField] private float probeHandleSize = 1f;
        [SerializeField] private int shadowCacheDebugReadbackInterval = 10;
        [SerializeField] private bool shadowCacheDebugShowLabels = true;
        [SerializeField] private float shadowCacheDebugSurfelSize = 0.05f;

        public int BakeSampleCount => bakeSampleCount;
        public uint BakeSeed => unchecked((uint)bakeSeed);
        public float SurfelMergeDistance => surfelMergeDistance;
        public int SelectedProbeIndex => selectedProbeIndex;
        public PRTProbeVisualizationMode VisualizationMode => (PRTProbeVisualizationMode)debugMode;
        public PRTSelectedProbeDebugMode SelectedProbeMode => (PRTSelectedProbeDebugMode)selectedProbeDebugMode;
        public int ShadowDebugReadbackInterval => Mathf.Max(1, shadowCacheDebugReadbackInterval);
        public bool ShadowDebugShowLabels => shadowCacheDebugShowLabels;
        public float ShadowDebugSurfelSize => shadowCacheDebugSurfelSize;
        public PRTProbeGrid BakeGrid => new PRTProbeGrid
        {
            origin = transform.position,
            min = Vector3Int.zero,
            count = new Vector3Int(probeSizeX, probeSizeY, probeSizeZ),
            spacing = probeGridSize
        };

        private ComputeBuffer _debugSH, _debugMetadata, _debugOffsets;
        private Material _debugRadianceMaterial;
#if UNITY_EDITOR
        private MaterialPropertyBlock _debugRadianceProperties;
        private static Mesh _debugRadianceSphere;

        internal void SetRadianceDebugBuffers(ComputeBuffer sh, ComputeBuffer metadata, ComputeBuffer offsets)
        {
            _debugSH = sh;
            _debugMetadata = metadata;
            _debugOffsets = offsets;
        }

        private void OnEnable() => RenderPipelineManager.beginCameraRendering += DrawProbeRadiance;

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= DrawProbeRadiance;
            _debugSH = _debugMetadata = _debugOffsets = null;
            if (!_debugRadianceMaterial) return;
            if (Application.isPlaying) Destroy(_debugRadianceMaterial);
            else DestroyImmediate(_debugRadianceMaterial);
            _debugRadianceMaterial = null;
        }

        private void DrawProbeRadiance(ScriptableRenderContext context, Camera camera)
        {
            if (VisualizationMode != PRTProbeVisualizationMode.ProbeRadiance ||
                !isActiveAndEnabled || !asset || !asset.HasValidData || !asset.Grid.Equals(BakeGrid) ||
                _debugSH == null || _debugMetadata == null || _debugOffsets == null ||
                camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection)
                return;
            if (!_debugRadianceMaterial)
            {
                var shader = Shader.Find("Hidden/PipelineCore/PRTProbeSHDebug");
                if (!shader || !shader.isSupported) return;
                _debugRadianceMaterial = new Material(shader)
                { name = "PRT probe irradiance debug", hideFlags = HideFlags.HideAndDontSave };
            }
            if (!_debugRadianceSphere)
            {
                var primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                primitive.hideFlags = HideFlags.HideAndDontSave;
                _debugRadianceSphere = primitive.GetComponent<MeshFilter>().sharedMesh;
                DestroyImmediate(primitive);
            }
            _debugRadianceProperties ??= new MaterialPropertyBlock();
            _debugRadianceProperties.SetBuffer("_prtDebugProbeSH", _debugSH);
            _debugRadianceProperties.SetBuffer("_prtDebugProbeMetadata", _debugMetadata);
            _debugRadianceProperties.SetBuffer("_prtDebugProbeOffsets", _debugOffsets);
            Vector3 origin = asset.Grid.origin + (Vector3)asset.Grid.min * asset.Grid.spacing;
            _debugRadianceProperties.SetVector("_prtDebugGridOrigin", new Vector4(origin.x, origin.y, origin.z, asset.Grid.spacing));
            bool hideSelected = selectedProbeIndex >= 0 &&
                SelectedProbeMode != PRTSelectedProbeDebugMode.IrradianceSphere;
            Vector3Int count = asset.Grid.count;
            _debugRadianceProperties.SetVector("_prtDebugGridCount",
                new Vector4(count.x, count.y, count.z, hideSelected ? selectedProbeIndex : -1));
            _debugRadianceProperties.SetFloat("_prtDebugProbeScale", probeHandleSize);
            var bounds = new Bounds(origin + (Vector3)(count - Vector3Int.one) * asset.Grid.spacing * 0.5f,
                (Vector3)(count - Vector3Int.one) * asset.Grid.spacing + Vector3.one * probeHandleSize * 2f);
            var parameters = new RenderParams(_debugRadianceMaterial)
            {
                camera = camera, layer = gameObject.layer, matProps = _debugRadianceProperties, worldBounds = bounds,
                shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false
            };
            Graphics.RenderMeshPrimitives(parameters, _debugRadianceSphere, 0, asset.Grid.ProbeCount);
        }
#else
        internal void SetRadianceDebugBuffers(ComputeBuffer sh, ComputeBuffer metadata, ComputeBuffer offsets) { }
#endif

        [System.NonSerialized] private Vector3[] _placementPreview;
        [System.NonSerialized] private bool[] _placementValidity;

        public void SetPlacementPreview(Vector3[] positions, bool[] validity)
        {
            _placementPreview = positions;
            _placementValidity = validity;
        }

        private void OnDrawGizmos()
        {
            if (probeSizeX < 1 || probeSizeY < 1 || probeSizeZ < 1) return;
            var mode = (PRTProbeVisualizationMode)debugMode;
            if (mode == PRTProbeVisualizationMode.None && _placementPreview == null) return;
            Gizmos.color = new Color(0.25f, 0.85f, 1f, 0.8f);
            var size = new Vector3(probeSizeX - 1, probeSizeY - 1, probeSizeZ - 1) * probeGridSize;
            Gizmos.DrawWireCube(transform.position + size * 0.5f, size);

            if (mode != PRTProbeVisualizationMode.None)
            {
                var grid = BakeGrid;
                bool hasData = asset && asset.HasValidData && asset.Grid.Equals(grid);
                int stride = Mathf.Max(1, grid.ProbeCount / 1024);
                for (int i = 0; i < grid.ProbeCount; i += stride)
                {
                    Vector3 position = grid.GetPosition(i);
                    bool valid = true;
                    if (hasData)
                    {
                        var metadata = asset.Probes[i];
                        valid = (metadata.validity >> 24) > 127;
                        if (mode == PRTProbeVisualizationMode.ProbeGridWithVirtualOffset ||
                            mode == PRTProbeVisualizationMode.ProbeRadiance)
                            position += metadata.captureOffset;
                    }
                    if (mode == PRTProbeVisualizationMode.ShadowCache)
                        Gizmos.color = TryGetShadowCacheSummary(i, out float mean, out _, out _)
                            ? Color.Lerp(new Color(0.9f, 0.2f, 0.1f), Color.green, mean) : Color.gray;
                    else Gizmos.color = mode == PRTProbeVisualizationMode.ProbeRadiance && _debugSH == null
                        ? Color.gray : valid ? new Color(0.3f, 0.9f, 1f, 0.8f) : Color.red;
                    if (mode != PRTProbeVisualizationMode.ProbeRadiance || _debugSH == null || !_debugRadianceMaterial)
                        Gizmos.DrawSphere(position, Mathf.Max(0.025f, probeGridSize * probeHandleSize * 0.035f));
                }
                if (selectedProbeIndex >= 0 && selectedProbeIndex < grid.ProbeCount)
                {
                    Vector3 selected = grid.GetPosition(selectedProbeIndex);
                    if (hasData && mode != PRTProbeVisualizationMode.ProbeGrid &&
                        mode != PRTProbeVisualizationMode.ShadowCache)
                        selected += asset.Probes[selectedProbeIndex].captureOffset;
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(selected, Mathf.Max(0.1f, probeGridSize * probeHandleSize * 0.18f));
                    if (hasData && (mode == PRTProbeVisualizationMode.ProbeRadiance ||
                                    mode == PRTProbeVisualizationMode.ShadowCache))
                        DrawSelectedProbeDetails(selected, mode);
                }
            }

            if (_placementPreview == null || _placementValidity == null) return;
            int previewStride = Mathf.Max(1, _placementPreview.Length / 1024);
            for (int i = 0; i < _placementPreview.Length; i += previewStride)
            {
                Gizmos.color = _placementValidity[i] ? Color.green : Color.red;
                Gizmos.DrawSphere(_placementPreview[i], Mathf.Max(0.03f, probeGridSize * 0.04f));
            }
        }

        private void DrawSelectedProbeDetails(Vector3 position, PRTProbeVisualizationMode mode)
        {
            int sectorId = asset.SectorIndex(selectedProbeIndex);
            var sector = asset.ReadSector(sectorId);
            var probe = sector.probes[asset.LocalProbeIndex(selectedProbeIndex)];
            PRTSelectedProbeDebugMode detail = (PRTSelectedProbeDebugMode)selectedProbeDebugMode;
            if (mode == PRTProbeVisualizationMode.ProbeRadiance && detail == PRTSelectedProbeDebugMode.IrradianceSphere)
                return;

            // A probe can reference thousands of merged surfels. Sample across the entire factor range,
            // so limiting the Gizmo count does not bias the preview toward the first few bricks.
            int total = 0;
            for (int factor = probe.factorStart; factor < probe.factorStart + probe.factorCount; factor++)
                total += sector.bricks[sector.factors[factor].BrickIndex].count;
            const int maxGizmos = 1024;
            int stride = Mathf.Max(1, Mathf.CeilToInt(total / (float)maxGizmos));
            int ordinal = 0;
            for (int factor = probe.factorStart; factor < probe.factorStart + probe.factorCount; factor++)
            {
                var brick = sector.bricks[sector.factors[factor].BrickIndex];
                Bounds bounds = default;
                bool hasBounds = false;
                for (int index = brick.start; index < brick.start + brick.count; index++)
                {
                    Surfel surfel = sector.surfels[index];
                    Vector3 point = surfel.Position(sector.surfelBounds);
                    if (mode == PRTProbeVisualizationMode.ProbeRadiance && detail == PRTSelectedProbeDebugMode.SurfelBrickGrid)
                    {
                        if (!hasBounds) { bounds = new Bounds(point, Vector3.zero); hasBounds = true; }
                        else bounds.Encapsulate(point);
                    }
                    if (ordinal++ % stride != 0) continue;
                    if (mode == PRTProbeVisualizationMode.ShadowCache)
                    {
                        TryGetShadowSurfel(sectorId, index, out float visibility, out bool cached);
                        Gizmos.color = cached ? Color.Lerp(Color.red, Color.green, visibility) : Color.gray;
                        Gizmos.DrawSphere(point, Mathf.Max(0.005f, shadowCacheDebugSurfelSize));
                    }
                    else if (detail == PRTSelectedProbeDebugMode.SphereDistribution)
                    {
                        Vector3 direction = point - position;
                        if (direction.sqrMagnitude <= 1e-8f) continue;
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawSphere(position + direction.normalized, 0.025f * probeHandleSize);
                    }
                    else if (detail == PRTSelectedProbeDebugMode.SampleDirection)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(position, point);
                        Gizmos.DrawSphere(point, 0.05f);
                    }
                    else
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawSphere(point, 0.05f);
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(point, point + surfel.Normal * 0.25f);
                    }
                }
                if (hasBounds)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireCube(bounds.center, bounds.size + Vector3.one * 0.1f);
                    Gizmos.DrawLine(position, bounds.center);
                }
            }
        }
    }
}
