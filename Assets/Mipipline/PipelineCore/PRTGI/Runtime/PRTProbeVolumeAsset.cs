using System;
using System.IO;
using UnityEngine;

namespace Illusion.Rendering.PRTGI
{
    [PreferBinarySerialization]
    public class PRTProbeVolumeAsset : ScriptableObject
    {
        [SerializeField] private bool hasValidData;
        [SerializeField] private PRTProbeGrid grid;
        [SerializeField] private PRTBakeSignature signature;
        [SerializeField] private Bounds geometryBounds;
        [SerializeField] private int sectorWidth;
        [SerializeField, HideInInspector] private PRTProbeMetadata[] probes = Array.Empty<PRTProbeMetadata>();
        [SerializeField, HideInInspector] private PRTSectorData[] sectors = Array.Empty<PRTSectorData>();
        [SerializeField, HideInInspector] private PRTSectorIndex[] sectorIndex = Array.Empty<PRTSectorIndex>();
        [SerializeField, HideInInspector] private string streamingDirectory;
        [NonSerialized] private int debugSectorIndex = -1;
        [NonSerialized] private PRTSectorData debugSector;
        public PRTProbeGrid Grid => grid;
        public PRTBakeSignature Signature => signature;
        public Bounds GeometryBounds => geometryBounds;
        public int SectorWidth => sectorWidth;
        public PRTProbeMetadata[] Probes => probes;
        public PRTSectorData[] Sectors => sectors;
        public bool IsStreamed => !string.IsNullOrEmpty(streamingDirectory) && sectorIndex?.Length > 0;
        public int SectorCount => IsStreamed ? sectorIndex.Length : sectors.Length;
        public PRTSectorIndex SectorInfo(int index)
        {
            if (IsStreamed) return sectorIndex[index];
            var s = sectors[index];
            return new PRTSectorIndex { coordinate = s.coordinate, bounds = s.surfelBounds,
                probeCount = s.probeIds.Length, surfelCount = s.surfels.Length,
                brickCount = s.bricks.Length, factorCount = s.factors.Length,
                skyWordCount = s.skyVisibility.Length };
        }
        public string StreamingDirectory => streamingDirectory;
        public long CompressedFileBytes
        {
            get
            {
                long bytes = 0;
                if (IsStreamed) foreach (var sector in sectorIndex) bytes += sector.fileBytes;
                return bytes;
            }
        }
        public string SectorPath(int index)
        {
            if (!IsStreamed || index < 0 || index >= sectorIndex.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (Application.streamingAssetsPath.Contains("://"))
                return Application.streamingAssetsPath.TrimEnd('/') + "/PRTGI/" + streamingDirectory +
                    "/" + PRTSectorFile.FileName(index);
            return Path.Combine(Application.streamingAssetsPath, "PRTGI", streamingDirectory,
                PRTSectorFile.FileName(index));
        }
        public PRTSectorData ReadSector(int index)
        {
            if (!IsStreamed) return sectors[index];
            if (debugSectorIndex == index && debugSector != null) return debugSector;
            debugSector = PRTSectorFile.Decode(File.ReadAllBytes(SectorPath(index)), sectorIndex[index]);
            debugSectorIndex = index;
            return debugSector;
        }
        internal PRTSectorData DecodeSector(int index, byte[] bytes) => PRTSectorFile.Decode(bytes, sectorIndex[index]);
        public Vector2Int SectorCoordinate(int index) => IsStreamed ? sectorIndex[index].coordinate : sectors[index].coordinate;
        public Bounds SectorBounds(int index) => IsStreamed ? sectorIndex[index].bounds : sectors[index].surfelBounds;
        public long SectorRawBytes(int index) => IsStreamed ? PRTSectorFile.RawBytes(sectorIndex[index]) :
            (long)sectors[index].probeIds.Length * (4 + PRTProbeData.Stride) +
            (long)sectors[index].surfels.Length * Surfel.Stride +
            (long)sectors[index].bricks.Length * SurfelIndices.Stride +
            (long)sectors[index].factors.Length * BrickFactor.Stride +
            (long)sectors[index].skyVisibility.Length * 4;
        public int ContentVersion { get; private set; }
        public int SectorIndex(int globalProbeId)
        {
            int x = globalProbeId / (grid.count.y * grid.count.z);
            int z = globalProbeId % grid.count.z;
            return x / sectorWidth * ((grid.count.z + sectorWidth - 1) / sectorWidth) + z / sectorWidth;
        }
        public int LocalProbeIndex(int globalProbeId)
        {
            var sector = ReadSector(SectorIndex(globalProbeId));
            return Array.BinarySearch(sector.probeIds, globalProbeId);
        }
        public bool HasValidData => hasValidData;
        public bool TryValidate(out string reason)
        {
            if (!hasValidData) { reason = "The probe volume has not been baked."; return false; }
            if (!IsStreamed)
                return PRTDataValidation.Validate(grid, sectorWidth, signature.sampleCount, probes, sectors, out reason);
            if (grid.ProbeCount != probes.Length || sectorWidth < 1 || sectorIndex.Length !=
                ((grid.count.x + sectorWidth - 1) / sectorWidth) * ((grid.count.z + sectorWidth - 1) / sectorWidth))
            { reason = "PRT streamed sector index does not match the probe grid."; return false; }
            bool localFiles = !Application.streamingAssetsPath.Contains("://");
            for (int i = 0; i < sectorIndex.Length; i++)
            {
                var s = sectorIndex[i];
                if (s.probeCount < 1 || s.fileBytes < 1 || s.sha256?.Length != 64 ||
                    s.coordinate != new Vector2Int(i / ((grid.count.z + sectorWidth - 1) / sectorWidth),
                        i % ((grid.count.z + sectorWidth - 1) / sectorWidth)) ||
                    (localFiles && !File.Exists(SectorPath(i))))
                { reason = $"PRT streamed sector {i} is missing or has invalid metadata."; return false; }
            }
            reason = null;
            return true;
        }
        public void SetBakedData(PRTProbeGrid layout, PRTBakeSignature inputSignature, Bounds bounds, int width, PRTProbeMetadata[] metadata, PRTSectorData[] data)
        {
            if (!PRTDataValidation.Validate(layout, width, inputSignature.sampleCount, metadata, data, out string reason)) throw new ArgumentException(reason, nameof(data));
            grid = layout;
            signature = inputSignature;
            geometryBounds = bounds;
            sectorWidth = width;
            probes = metadata;
            sectors = data;
            sectorIndex = Array.Empty<PRTSectorIndex>();
            streamingDirectory = null;
            hasValidData = true;
            debugSector = null; debugSectorIndex = -1;
            ContentVersion++;
        }
        public void SetStreamedData(PRTProbeGrid layout, PRTBakeSignature inputSignature, Bounds bounds,
            int width, PRTProbeMetadata[] metadata, PRTSectorIndex[] index, string directory)
        {
            if (index == null || string.IsNullOrEmpty(directory) || metadata == null ||
                metadata.Length != layout.ProbeCount) throw new ArgumentException("Invalid streamed PRT index.");
            grid = layout; signature = inputSignature; geometryBounds = bounds;
            sectorWidth = width; probes = metadata; sectorIndex = index;
            streamingDirectory = directory; sectors = Array.Empty<PRTSectorData>();
            hasValidData = true;
            debugSector = null; debugSectorIndex = -1;
            ContentVersion++;
        }
        public void Clear()
        {
            hasValidData = false;
            probes = Array.Empty<PRTProbeMetadata>();
            sectors = Array.Empty<PRTSectorData>();
            sectorIndex = Array.Empty<PRTSectorIndex>();
            streamingDirectory = null;
            debugSector = null; debugSectorIndex = -1;
            ContentVersion++;
        }
    }
}
