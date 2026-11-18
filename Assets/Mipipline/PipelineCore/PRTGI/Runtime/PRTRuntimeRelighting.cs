using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Illusion.Rendering.PRTGI
{
    // URP 14 CPU relight path. The baked brick transfer and sky visibility retain the source format.
    internal sealed class PRTRuntimeRelighting : IPRTRelightRuntime
    {
        private readonly PRTProbeVolume _volume;
        private readonly PRTProbeVolumeAsset _asset;
        private readonly PRTSectorStream _stream;
        private readonly Texture3D _atlas;
        private readonly Color[] _pixels;
        private readonly Vector3[] _previous;
        private readonly Vector3[] _current;
        private readonly bool[] _ready;
        private readonly Vector4[][] _pyramid;
        private readonly Vector4[] _directions;
        private readonly Vector3[][] _skyRadiance;
        private readonly Vector3[] _skyColors;
        private int _environmentHash;
        private bool _hasEnvironment;
        private readonly Vector3[] _coefficients = new Vector3[9];
        private readonly List<Light> _sceneLights = new List<Light>();
        private int _backgroundSector;
        private bool _backgroundTurn;
        private long _totalSectorUpdates;
        private int _lastRelightFrame = -1;

        public Texture Coefficients => _atlas;
        public PRTCascadeLayout Layout { get; }
        public string Backend => "CPU";
        public long TotalSectorUpdates => _totalSectorUpdates;
        public int NearSector { get; private set; } = -1;
        public int BackgroundSector => _backgroundSector;
        public int UpdatedSectorsThisFrame { get; private set; }
        public long ResidentBytes => 0;
        public long CachedCpuSectorBytes => _stream.CachedBytes;
        public int PendingCpuSectors => _stream.PendingCount;
        public long PeakResidentBytes => 0;
        public int FrameUploadBytes => 0;
        public int UploadingSectors => _stream.PendingCount;
        public int EvictingSectors => 0;
        public int PublishedCellsThisFrame => Layout.Slots.x * Layout.Slots.y * Layout.Slots.z * Layout.CascadeCount;
        public string ResidencyPressure => _stream.LastError;

        internal PRTRuntimeRelighting(PRTProbeVolume volume)
        {
            _volume = volume;
            _asset = volume.asset;
            _stream = new PRTSectorStream(_asset);
            PRTProbeGrid grid = _asset.Grid;
            Layout = new PRTCascadeLayout(grid, volume.voxelProbeSize, volume.cascadeCount);
            int count = grid.ProbeCount;
            _previous = new Vector3[count * 9];
            _current = new Vector3[count * 9];
            _ready = new bool[count];
            _pyramid = new Vector4[Layout.CascadeCount][];
            for (int level = 1; level < Layout.CascadeCount; level++)
            {
                Vector3Int size = Layout.LevelCount(level);
                _pyramid[level] = new Vector4[checked(size.x * size.y * size.z * 9)];
            }
            _directions = PRTBakeSampling.GenerateDirections(_asset.Signature.sampleCount, _asset.Signature.seed);
            _skyRadiance = new Vector3[_asset.SectorCount][];
            _skyColors = new Vector3[_directions.Length];
            _pixels = new Color[checked(Layout.Slots.x * Layout.Slots.z * Layout.TextureDepth)];
            _atlas = new Texture3D(Layout.Slots.x, Layout.Slots.z, Layout.TextureDepth, TextureFormat.RGBAHalf, false)
            {
                name = "PRT camera SH CPU",
                wrapModeU = TextureWrapMode.Repeat,
                wrapModeV = TextureWrapMode.Repeat,
                wrapModeW = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            _atlas.SetPixels(_pixels);
            _atlas.Apply(false, false);
        }

        public void Tick(Camera camera, CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (!_volume || !_asset || !_asset.HasValidData) return;
            UpdateEnvironment();
            Layout.Recenter(camera.transform.position);
            UpdatedSectorsThisFrame = 0;
            if (!Application.isPlaying || _lastRelightFrame != Time.frameCount)
            {
                _sceneLights.Clear();
                foreach (var light in UnityEngine.Object.FindObjectsOfType<Light>())
                    if (light.isActiveAndEnabled && light.gameObject.scene == _volume.gameObject.scene)
                        _sceneLights.Add(light);
                int sectors = _asset.SectorCount;
                var grid = _asset.Grid;
                var coordinate = (camera.transform.position - grid.origin) / grid.spacing - (Vector3)grid.min;
                int x = Mathf.Clamp(Mathf.FloorToInt(coordinate.x), 0, grid.count.x - 1);
                int z = Mathf.Clamp(Mathf.FloorToInt(coordinate.z), 0, grid.count.z - 1);
                NearSector = _asset.SectorIndex(x * grid.count.y * grid.count.z + z);
                int budget = Mathf.Clamp(_volume.sectorsPerFrame, 1, sectors);
                if (budget == 1)
                {
                    int selected = _backgroundTurn ? _backgroundSector : NearSector;
                    _backgroundTurn = !_backgroundTurn;
                    UpdateSector(selected);
                }
                else
                {
                    int background = _backgroundSector;
                    UpdateSector(NearSector);
                    if (background != NearSector) UpdateSector(background);
                    int requested = background == NearSector ? 1 : 2;
                    for (int offset = 1; requested < budget && offset < sectors; offset++)
                    {
                        int sector = (background + offset) % sectors;
                        if (sector != NearSector && sector != background)
                        {
                            UpdateSector(sector);
                            requested++;
                        }
                    }
                }
                Array.Copy(_current, _previous, _current.Length);
                _lastRelightFrame = Time.frameCount;
            }
            BuildPyramid();
            Publish();
            _atlas.SetPixels(_pixels);
            _atlas.Apply(false, false);
        }

        private void UpdateSector(int index)
        {
            PRTSectorData sector = _stream.Request(index);
            if (sector == null) return;
            RelightSector(index, sector, _sceneLights);
            UpdatedSectorsThisFrame++;
            _totalSectorUpdates++;
            if (index == _backgroundSector)
                _backgroundSector = (_backgroundSector + 1) % _asset.SectorCount;
            _stream.Trim((long)Mathf.Max(1, _volume.cpuSectorBudgetMiB) * 1048576, NearSector);
        }

        private void RelightSector(int sectorIndex, PRTSectorData sector, List<Light> lights)
        {
            var radiance = new Vector3[sector.bricks.Length];
            for (int brick = 0; brick < radiance.Length; brick++)
            {
                var range = sector.bricks[brick];
                Vector3 sum = Vector3.zero;
                for (int i = range.start; i < range.start + range.count; i++)
                {
                    Surfel surfel = sector.surfels[i];
                    Vector3 position = surfel.Position(sector.surfelBounds);
                    Vector3 normal = surfel.Normal;
                    Vector3 direct = EvaluateLights(position, normal, range, lights);
                    Vector3 bounce = Vector3.zero;
                    int nearest = surfel.nearestProbe;
                    if (nearest >= 0 && nearest < _asset.Grid.ProbeCount && _ready[nearest])
                    {
                        Vector2 mask = PRTProbeValidity.Unpack(_asset.Probes[nearest].validity);
                        if (mask.y > 0f)
                            bounce = EvaluateIrradiance(_previous, nearest, normal) * mask.x;
                    }
                    sum += Vector3.Scale(surfel.Albedo, direct + bounce);
                }
                radiance[brick] = range.count > 0 ? sum / range.count : Vector3.zero;
            }
            Vector3[] sky = _skyRadiance[sectorIndex] ?? (_skyRadiance[sectorIndex] = BuildSkyRadiance(sector));
            for (int local = 0; local < sector.probeIds.Length; local++)
            {
                int global = sector.probeIds[local];
                _ready[global] = true;
                var probe = sector.probes[local];
                Array.Clear(_coefficients, 0, _coefficients.Length);
                for (int factorIndex = probe.factorStart; factorIndex < probe.factorStart + probe.factorCount; factorIndex++)
                {
                    var factor = sector.factors[factorIndex];
                    Vector3 value = radiance[factor.BrickIndex];
                    for (int c = 0; c < 9; c++) _coefficients[c] += value * factor[c];
                }
                for (int c = 0; c < 9; c++)
                {
                    Vector3 result = _coefficients[c] + sky[local * 9 + c];
                    _current[global * 9 + c] = result;
                }
            }
        }

        private static int Index(Vector3Int node, Vector3Int count) =>
            (node.x * count.y + node.y) * count.z + node.z;

        private Vector4 BaseCoefficient(int probe, int coefficient)
        {
            Vector2 mask = _ready[probe] ? PRTProbeValidity.Unpack(_asset.Probes[probe].validity) : Vector2.zero;
            Vector3 value = _previous[probe * 9 + coefficient] * (mask.x * mask.y);
            return new Vector4(value.x, value.y, value.z, mask.y);
        }

        private void BuildPyramid()
        {
            for (int level = 1; level < Layout.CascadeCount; level++)
            {
                Vector3Int fine = Layout.LevelCount(level - 1), coarse = Layout.LevelCount(level);
                for (int x = 0; x < coarse.x; x++) for (int y = 0; y < coarse.y; y++) for (int z = 0; z < coarse.z; z++)
                {
                    int target = Index(new Vector3Int(x, y, z), coarse);
                    float weightTotal = 0;
                    for (int c = 0; c < 9; c++) _pyramid[level][target * 9 + c] = Vector4.zero;
                    for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++) for (int dz = -1; dz <= 1; dz++)
                    {
                        var node = new Vector3Int(x * 2 + dx, y * 2 + dy, z * 2 + dz);
                        if (node.x < 0 || node.y < 0 || node.z < 0 || node.x >= fine.x || node.y >= fine.y || node.z >= fine.z) continue;
                        float weight = (dx == 0 ? 0.5f : 0.25f) * (dy == 0 ? 0.5f : 0.25f) * (dz == 0 ? 0.5f : 0.25f);
                        weightTotal += weight;
                        int source = Index(node, fine);
                        for (int c = 0; c < 9; c++)
                            _pyramid[level][target * 9 + c] += weight *
                                (level == 1 ? BaseCoefficient(source, c) : _pyramid[level - 1][source * 9 + c]);
                    }
                    if (weightTotal > 0)
                        for (int c = 0; c < 9; c++) _pyramid[level][target * 9 + c] /= weightTotal;
                }
            }
        }

        private void Publish()
        {
            Array.Clear(_pixels, 0, _pixels.Length);
            Vector3Int slots = Layout.Slots;
            for (int level = 0; level < Layout.CascadeCount; level++)
            {
                Vector3Int minimum = Layout.Minimum(level), count = Layout.WindowCount(level), domain = Layout.LevelCount(level);
                for (int x = 0; x < count.x; x++) for (int y = 0; y < count.y; y++) for (int z = 0; z < count.z; z++)
                {
                    int source = Index(minimum + new Vector3Int(x, y, z), domain);
                    for (int c = 0; c < 9; c++)
                    {
                        Vector4 value = level == 0 ? BaseCoefficient(source, c) : _pyramid[level][source * 9 + c];
                        int slice = y + (level * 9 + c) * slots.y;
                        int slotX = (minimum.x + x) % slots.x, slotZ = (minimum.z + z) % slots.z;
                        _pixels[(slice * slots.z + slotZ) * slots.x + slotX] = new Color(value.x, value.y, value.z, value.w);
                    }
                }
            }
        }

        private void UpdateEnvironment()
        {
            var probe = RenderSettings.ambientProbe;
            var hash = new HashCode();
            hash.Add(RenderSettings.ambientMode);
            hash.Add(RenderSettings.ambientIntensity);
            hash.Add(RenderSettings.ambientLight);
            hash.Add(RenderSettings.skybox ? RenderSettings.skybox.ComputeCRC() : 0);
            for (int channel = 0; channel < 3; channel++)
                for (int coefficient = 0; coefficient < 9; coefficient++)
                    hash.Add(probe[channel, coefficient]);
            int current = hash.ToHashCode();
            if (_hasEnvironment && current == _environmentHash) return;
            _hasEnvironment = true;
            _environmentHash = current;
            Array.Clear(_skyRadiance, 0, _skyRadiance.Length);
            bool flat = RenderSettings.ambientMode == AmbientMode.Flat;
            bool skybox = RenderSettings.ambientMode == AmbientMode.Skybox && RenderSettings.skybox;
            for (int channel = 0; channel < 3; channel++)
                for (int coefficient = 1; coefficient < 9; coefficient++)
                    probe[channel, coefficient] *= coefficient < 4 ? 1.5f : 4f;
            var positions = new Vector3[_directions.Length];
            var colors = new Color[_directions.Length];
            for (int i = 0; i < positions.Length; i++) positions[i] = _directions[i];
            if (!flat) probe.Evaluate(positions, colors);
            Color flatColor = RenderSettings.ambientLight.linear;
            for (int i = 0; i < colors.Length; i++)
            {
                Color value = flat ? flatColor : colors[i];
                float intensity = skybox ? RenderSettings.ambientIntensity : 1f;
                _skyColors[i] = new Vector3(Mathf.Max(0, value.r), Mathf.Max(0, value.g), Mathf.Max(0, value.b)) * intensity;
            }
        }

        private Vector3[] BuildSkyRadiance(PRTSectorData sector)
        {
            var result = new Vector3[sector.probeIds.Length * 9];
            int words = PRTSkyVisibility.Words(_directions.Length);
            for (int probe = 0; probe < sector.probeIds.Length; probe++)
                for (int d = 0; d < _directions.Length; d++)
                {
                    if ((sector.skyVisibility[probe * words + d / 32] & (1u << (d & 31))) == 0) continue;
                    Vector3 direction = _directions[d];
                    for (int c = 0; c < 9; c++)
                        result[probe * 9 + c] += _skyColors[d] * (_directions[d].w * Basis(c, direction));
                }
            return result;
        }

        private static Vector3 EvaluateLights(Vector3 point, Vector3 normal, SurfelIndices range, List<Light> lights)
        {
            Vector3 result = Vector3.zero;
            foreach (Light light in lights)
            {
                if (!light || !light.isActiveAndEnabled || (light.cullingMask & (int)range.objectLayerMask) == 0 ||
                    (light.renderingLayerMask & range.renderingLayerMask) == 0) continue;
                Vector3 direction;
                float attenuation = 1f;
                if (light.type == LightType.Directional) direction = -light.transform.forward;
                else if (light.type == LightType.Point || light.type == LightType.Spot)
                {
                    Vector3 delta = light.transform.position - point;
                    float distance = delta.magnitude;
                    if (distance > light.range || distance < 0.0001f) continue;
                    direction = delta / distance;
                    attenuation = Mathf.Pow(Mathf.Clamp01(1f - distance * distance / (light.range * light.range)), 2f);
                    attenuation /= Mathf.Max(1f, distance * distance);
                    if (light.type == LightType.Spot)
                    {
                        float cutoff = Mathf.Cos(light.spotAngle * 0.5f * Mathf.Deg2Rad);
                        attenuation *= Mathf.SmoothStep(0f, 1f,
                            Mathf.InverseLerp(cutoff, Mathf.Min(1f, cutoff + 0.1f),
                                Vector3.Dot(light.transform.forward, -direction)));
                    }
                }
                else continue;
                float cosine = Mathf.Max(0f, Vector3.Dot(normal, direction));
                Color color = light.color.linear * (light.intensity * light.bounceIntensity * attenuation * cosine / Mathf.PI);
                result += new Vector3(color.r, color.g, color.b);
            }
            return result;
        }

        private static Vector3 EvaluateIrradiance(Vector3[] coefficients, int probe, Vector3 normal)
        {
            Vector3 result = Vector3.zero;
            for (int c = 0; c < 9; c++)
                result += coefficients[probe * 9 + c] * Basis(c, normal) * (c == 0 ? 1f : c < 4 ? 2f / 3f : 0.25f);
            return new Vector3(Mathf.Max(0f, result.x), Mathf.Max(0f, result.y), Mathf.Max(0f, result.z));
        }

        private static float Basis(int c, Vector3 n)
        {
            // Source SH basis uses x,z,y: world Y is the up axis.
            float x = n.x, y = n.z, z = n.y;
            switch (c)
            {
                case 0: return 0.2820947918f;
                case 1: return -0.4886025119f * y;
                case 2: return 0.4886025119f * z;
                case 3: return -0.4886025119f * x;
                case 4: return 1.0925484306f * x * y;
                case 5: return -1.0925484306f * y * z;
                case 6: return 0.3153915653f * (3f * z * z - 1f);
                case 7: return -1.0925484306f * x * z;
                default: return 0.5462742153f * (x * x - y * y);
            }
        }

        public void Dispose()
        {
            _stream.Dispose();
            if (_atlas)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(_atlas);
                else UnityEngine.Object.DestroyImmediate(_atlas);
            }
        }
    }
}
