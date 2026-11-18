using Illusion.Rendering.PRTGI;
using UnityEditor;
using UnityEngine;

namespace Illusion.Rendering.PRTGI.Editor
{
    [CustomEditor(typeof(PRTProbeVolumeAsset))]
    internal sealed class PRTVolumeAssetEditor : UnityEditor.Editor
    {
        private bool _memoryOpen;
        private int _validatedVersion = -1;
        private bool _valid;
        private string _reason;

        public override void OnInspectorGUI()
        {
            var asset = (PRTProbeVolumeAsset)target;
            if (_validatedVersion != asset.ContentVersion)
            {
                _valid = asset.TryValidate(out _reason);
                _validatedVersion = asset.ContentVersion;
            }
            if (!_valid)
            {
                EditorGUILayout.HelpBox(_reason, MessageType.Warning);
                return;
            }

            PRTProbeGrid grid = asset.Grid;
            long metadata = asset.Probes.LongLength * 16;
            long surfels = 0, bricks = 0, transfers = 0, probeRanges = 0, ids = 0, sky = 0;
            long surfelCount = 0, brickCount = 0, factorCount = 0, probeCount = 0, skyCount = 0;
            for (int i = 0; i < asset.SectorCount; i++)
            {
                PRTSectorIndex s = asset.SectorInfo(i);
                surfelCount += s.surfelCount; brickCount += s.brickCount;
                factorCount += s.factorCount; probeCount += s.probeCount; skyCount += s.skyWordCount;
                surfels += (long)s.surfelCount * Surfel.Stride;
                bricks += (long)s.brickCount * SurfelIndices.Stride;
                transfers += (long)s.factorCount * BrickFactor.Stride;
                probeRanges += (long)s.probeCount * PRTProbeData.Stride;
                ids += (long)s.probeCount * 4;
                sky += (long)s.skyWordCount * 4;
            }

            EditorGUILayout.LabelField("Probes", $"{grid.ProbeCount:N0} ({grid.count.x} × {grid.count.y} × {grid.count.z}, spacing {grid.spacing:0.##} m)");
            EditorGUILayout.LabelField("Sectors", $"{asset.SectorCount} ({asset.SectorWidth} × {asset.SectorWidth} columns)");
            EditorGUILayout.LabelField("Sampling", $"{asset.Signature.sampleCount:N0} directions, seed {asset.Signature.seed}");
            EditorGUILayout.LabelField("Backend", asset.Signature.backend);
            EditorGUILayout.LabelField("Transport Memory", FormatBytes(metadata + surfels + bricks + transfers + probeRanges + ids + sky));
            if (asset.IsStreamed)
            {
                EditorGUILayout.LabelField("Disk Sector Files", FormatBytes(asset.CompressedFileBytes));
                EditorGUILayout.LabelField("Storage", "StreamingAssets/PRTGI/" + asset.StreamingDirectory);
                EditorGUILayout.HelpBox("The index and probe metadata are in this asset. Sector transport is loaded from disk on demand.", MessageType.Info);
            }
            else EditorGUILayout.HelpBox("Embedded transport: use Tools/PRTGI/Convert Selected Asset to Streamed Sectors.", MessageType.Warning);

            _memoryOpen = EditorGUILayout.Foldout(_memoryOpen, "Memory Breakdown", true);
            if (!_memoryOpen) return;
            EditorGUI.indentLevel++;
            Row("Probe Metadata", asset.Probes.Length, metadata);
            Row("Geometry Surfels", surfelCount, surfels);
            Row("Bricks", brickCount, bricks);
            Row("SH9 Transfers", factorCount, transfers);
            Row("Sector Probe Ranges", probeCount, probeRanges);
            Row("Sector Probe IDs", probeCount, ids);
            Row("Sky Visibility Words", skyCount, sky);
            EditorGUI.indentLevel--;
        }

        private static void Row(string label, long count, long bytes) =>
            EditorGUILayout.LabelField(label, $"{count:N0} · {FormatBytes(bytes)}");

        private static string FormatBytes(long bytes) => bytes >= 1048576
            ? $"{bytes / 1048576.0:0.##} MiB" : bytes >= 1024
                ? $"{bytes / 1024.0:0.#} KiB" : $"{bytes} B";
    }
}
