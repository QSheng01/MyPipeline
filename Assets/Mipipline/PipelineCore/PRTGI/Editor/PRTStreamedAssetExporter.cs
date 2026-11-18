using System;
using System.IO;
using Illusion.Rendering.PRTGI;
using UnityEditor;
using UnityEngine;

namespace Illusion.Rendering.PRTGI.Editor
{
    internal static class PRTStreamedAssetExporter
    {
        [MenuItem("Tools/PRTGI/Convert Selected Asset to Streamed Sectors")]
        private static void ConvertSelected() => Convert(Selection.activeObject as PRTProbeVolumeAsset);

        [MenuItem("Tools/PRTGI/Convert Selected Asset to Streamed Sectors", true)]
        private static bool CanConvert() => Selection.activeObject is PRTProbeVolumeAsset asset &&
            asset.HasValidData && !asset.IsStreamed;

        internal static void Convert(PRTProbeVolumeAsset asset)
        {
            string reason = "Missing asset or already streamed.";
            if (!asset || asset.IsStreamed || !asset.TryValidate(out reason))
                throw new InvalidOperationException("PRT embedded asset is not ready for conversion: " + reason);
            string assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(assetPath)) throw new InvalidOperationException("Save the PRT asset before exporting sectors.");
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            string generation = Guid.NewGuid().ToString("N");
            string relative = guid + "/" + generation;
            string parent = Path.Combine(Application.streamingAssetsPath, "PRTGI", guid);
            string destination = Path.Combine(parent, generation);
            string staging = destination + ".writing";
            if (Directory.Exists(staging) || Directory.Exists(destination))
                throw new IOException("PRT export generation already exists.");
            Directory.CreateDirectory(parent);
            Directory.CreateDirectory(staging);
            PRTSectorData[] sectors = asset.Sectors;
            var index = new PRTSectorIndex[sectors.Length];
            try
            {
                for (int i = 0; i < sectors.Length; i++)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("Export PRT Sectors",
                        $"Compress and verify sector {i + 1}/{sectors.Length}", (float)i / sectors.Length))
                        throw new OperationCanceledException("PRT sector export cancelled.");
                    byte[] bytes = PRTSectorFile.Encode(sectors[i]);
                    index[i] = PRTSectorFile.Describe(sectors[i], bytes);
                    string path = Path.Combine(staging, PRTSectorFile.FileName(i));
                    File.WriteAllBytes(path, bytes);
                    PRTSectorData roundTrip = PRTSectorFile.Decode(File.ReadAllBytes(path), index[i]);
                    if (roundTrip.probeIds.Length != sectors[i].probeIds.Length ||
                        roundTrip.surfels.Length != sectors[i].surfels.Length ||
                        roundTrip.factors.Length != sectors[i].factors.Length)
                        throw new InvalidDataException("PRT sector export verification failed at " + i);
                }
                // The previous serialized asset remains recoverable even after Unity resaves the index.
                string backupFolder = Path.Combine(Application.dataPath, "Mipipline", "PipelineCore",
                    "PRTGI", "OriginalBakeBackup~");
                Directory.CreateDirectory(backupFolder);
                string backup = Path.Combine(backupFolder, $"{Path.GetFileNameWithoutExtension(assetPath)}-before-stream-{generation}.asset");
                AssetDatabase.SaveAssets();
                File.Copy(assetPath, backup);
                File.Copy(assetPath + ".meta", backup + ".meta");
                Directory.Move(staging, destination);
                asset.SetStreamedData(asset.Grid, asset.Signature, asset.GeometryBounds,
                    asset.SectorWidth, asset.Probes, index, relative);
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                Debug.Log($"PRTGI: exported {index.Length} sector files to StreamingAssets/PRTGI/{relative}.", asset);
            }
            catch
            {
                // If the index was not saved, the old embedded asset is still valid. Keep any
                // completed generation recoverable for diagnosis; remove only our staging files.
                if (Directory.Exists(staging)) Directory.Delete(staging, true);
                throw;
            }
            finally { EditorUtility.ClearProgressBar(); }
        }
    }
}
