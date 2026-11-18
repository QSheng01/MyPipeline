using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using UnityEngine;

namespace Illusion.Rendering.PRTGI
{
    [Serializable]
    public struct PRTSectorIndex
    {
        public Vector2Int coordinate;
        public Bounds bounds;
        public int probeCount, surfelCount, brickCount, factorCount, skyWordCount;
        public long fileBytes;
        public string sha256;
    }

    public static class PRTSectorFile
    {
        private const uint Magic = 0x31545250; // PRT1
        private const int Version = 1;
        public static string FileName(int index) => $"sector_{index:D4}.prts";

        public static byte[] Encode(PRTSectorData sector)
        {
            using var output = new MemoryStream();
            using (var zip = new DeflateStream(output, System.IO.Compression.CompressionLevel.Optimal, true))
            using (var writer = new BinaryWriter(zip))
            {
                writer.Write(Magic); writer.Write(Version);
                writer.Write(sector.coordinate.x); writer.Write(sector.coordinate.y);
                Vector(writer, sector.surfelBounds.center); Vector(writer, sector.surfelBounds.size);
                writer.Write(sector.probeIds.Length);
                foreach (int id in sector.probeIds) writer.Write(id);
                writer.Write(sector.surfels.Length);
                foreach (var s in sector.surfels)
                { writer.Write(s.positionXY); writer.Write(s.positionZAlbedoRG); writer.Write(s.normalAlbedoB); writer.Write(s.nearestProbe); }
                writer.Write(sector.bricks.Length);
                foreach (var b in sector.bricks)
                { writer.Write(b.start); writer.Write(b.count); writer.Write(b.renderingLayerMask); writer.Write(b.objectLayerMask); }
                writer.Write(sector.factors.Length);
                foreach (var f in sector.factors)
                { writer.Write(f.brickSh0); writer.Write(f.sh12); writer.Write(f.sh34); writer.Write(f.sh56); writer.Write(f.sh78); }
                writer.Write(sector.probes.Length);
                foreach (var p in sector.probes)
                { writer.Write(p.factorStart); writer.Write(p.factorCount); Vector(writer, p.captureOffset); writer.Write(p.validity); }
                writer.Write(sector.skyVisibility.Length);
                foreach (uint word in sector.skyVisibility) writer.Write(word);
            }
            return output.ToArray();
        }

        public static PRTSectorData Decode(byte[] bytes, PRTSectorIndex index)
        {
            if (bytes == null || bytes.LongLength != index.fileBytes ||
                !string.Equals(Digest(bytes), index.sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("PRT sector length or SHA-256 differs from the index.");
            using var input = new MemoryStream(bytes, false);
            using var zip = new DeflateStream(input, CompressionMode.Decompress);
            using var reader = new BinaryReader(zip);
            if (reader.ReadUInt32() != Magic || reader.ReadInt32() != Version)
                throw new InvalidDataException("Unsupported PRT sector format.");
            var data = new PRTSectorData
            {
                coordinate = new Vector2Int(reader.ReadInt32(), reader.ReadInt32()),
                surfelBounds = new Bounds(ReadVector(reader), ReadVector(reader))
            };
            data.probeIds = new int[Count(reader, index.probeCount)];
            for (int i = 0; i < data.probeIds.Length; i++) data.probeIds[i] = reader.ReadInt32();
            data.surfels = new Surfel[Count(reader, index.surfelCount)];
            for (int i = 0; i < data.surfels.Length; i++) data.surfels[i] = new Surfel
            { positionXY = reader.ReadUInt32(), positionZAlbedoRG = reader.ReadUInt32(),
              normalAlbedoB = reader.ReadUInt32(), nearestProbe = reader.ReadInt32() };
            data.bricks = new SurfelIndices[Count(reader, index.brickCount)];
            for (int i = 0; i < data.bricks.Length; i++) data.bricks[i] = new SurfelIndices
            { start = reader.ReadInt32(), count = reader.ReadInt32(),
              renderingLayerMask = reader.ReadUInt32(), objectLayerMask = reader.ReadUInt32() };
            data.factors = new BrickFactor[Count(reader, index.factorCount)];
            for (int i = 0; i < data.factors.Length; i++) data.factors[i] = new BrickFactor
            { brickSh0 = reader.ReadUInt32(), sh12 = reader.ReadUInt32(), sh34 = reader.ReadUInt32(),
              sh56 = reader.ReadUInt32(), sh78 = reader.ReadUInt32() };
            data.probes = new PRTProbeData[Count(reader, index.probeCount)];
            for (int i = 0; i < data.probes.Length; i++) data.probes[i] = new PRTProbeData
            { factorStart = reader.ReadInt32(), factorCount = reader.ReadInt32(),
              captureOffset = ReadVector(reader), validity = reader.ReadUInt32() };
            data.skyVisibility = new uint[Count(reader, index.skyWordCount)];
            for (int i = 0; i < data.skyVisibility.Length; i++) data.skyVisibility[i] = reader.ReadUInt32();
            if (data.coordinate != index.coordinate || data.surfelBounds != index.bounds || zip.ReadByte() != -1)
                throw new InvalidDataException("PRT sector metadata or trailing bytes differ from the index.");
            return data;
        }

        public static PRTSectorIndex Describe(PRTSectorData sector, byte[] bytes) => new PRTSectorIndex
        {
            coordinate = sector.coordinate, bounds = sector.surfelBounds,
            probeCount = sector.probeIds.Length, surfelCount = sector.surfels.Length,
            brickCount = sector.bricks.Length, factorCount = sector.factors.Length,
            skyWordCount = sector.skyVisibility.Length, fileBytes = bytes.LongLength, sha256 = Digest(bytes)
        };

        internal static long RawBytes(PRTSectorIndex s) => (long)s.probeCount * (4 + PRTProbeData.Stride) +
            (long)s.surfelCount * Surfel.Stride + (long)s.brickCount * SurfelIndices.Stride +
            (long)s.factorCount * BrickFactor.Stride + (long)s.skyWordCount * 4;

        private static string Digest(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        private static int Count(BinaryReader reader, int expected)
        {
            int count = reader.ReadInt32();
            if (count != expected || count < 0) throw new InvalidDataException("PRT sector element count differs from the index.");
            return count;
        }
        private static void Vector(BinaryWriter writer, Vector3 value)
        { writer.Write(value.x); writer.Write(value.y); writer.Write(value.z); }
        private static Vector3 ReadVector(BinaryReader reader) =>
            new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }
}
