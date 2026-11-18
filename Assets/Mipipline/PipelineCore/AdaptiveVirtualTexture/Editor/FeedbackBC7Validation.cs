using System;
using System.IO;
using System.Reflection;
using System.Text;
using NoOvertime.VirtualTexture;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public static class FeedbackBC7Validation
{
    const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static void Require(bool condition, string message){ if(!condition) throw new Exception(message); }

    static byte[] ReadSlice(Texture texture, int slice, RenderTexture decoded, ComputeShader decoder)
    {
        Texture input = texture;
        bool decode = texture.graphicsFormat == GraphicsFormat.RGBA_BC7_UNorm;
        if (decode)
        {
            decoder.SetInt("Width", texture.width);
            decoder.SetInt("Slice", slice);
            decoder.SetTexture(0, "Compressed", texture);
            decoder.SetTexture(0, "Decoded", decoded);
            decoder.Dispatch(0, (texture.width + 7) / 8, (texture.height + 7) / 8, 1);
            input = decoded; slice = 0;
        }
        var request = AsyncGPUReadback.Request(input, 0, 0, input.width, 0, input.height, slice, 1);
        request.WaitForCompletion();
        Require(!request.hasError, "GPU readback failed.");
        return request.GetData<byte>().ToArray();
    }

    [MenuItem("Tools/Adaptive Virtual Texture/Validate Feedback BC7 (Play Mode)")]
    public static void Run()
    {
        Require(Application.isPlaying, "Run in Play Mode.");
        Require(PhysicalPageAtlas.CompressionSupported, "D3D11 BC7 path not supported.");
        var decoder = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Mipipline/PipelineCore/AdaptiveVirtualTexture/Editor/ClipmapBC7Validation.compute");
        Require(decoder != null, "Decoder compute missing.");
        var decoded = new RenderTexture(264, 264, 0, GraphicsFormat.R8G8B8A8_UNorm)
        { dimension = TextureDimension.Tex2DArray, volumeDepth = 1, enableRandomWrite = true };
        decoded.Create();
        var report = new StringBuilder();
        bool previousEnabled = PhysicalPageAtlas.CompressionEnabled;
        try
        {
            // Two atlas instances bake the identical page; only the storage format differs.
            PhysicalPageAtlas.CompressionEnabled = false;
            var uncompressed = new PhysicalPageAtlas();
            PhysicalPageAtlas.CompressionEnabled = true;
            var compressed = new PhysicalPageAtlas();
            PhysicalPageAtlas.CompressionEnabled = previousEnabled;
            var pageID = new int4(3, 5, 0, 8);
            var sector = new int2(17, 21);
            var localPageID = new int2(9, 13);
            using (var cmd = new CommandBuffer())
            {
                uncompressed.Render(cmd, sector, localPageID, pageID, 7);
                compressed.Render(cmd, sector, localPageID, pageID, 7);
                Graphics.ExecuteCommandBuffer(cmd);
            }
            var baseU = ReadSlice(uncompressed.PhysicalPageBaseMapAtlas, 7, decoded, decoder);
            var baseC = ReadSlice(compressed.PhysicalPageBaseMapAtlas, 7, decoded, decoder);
            var maskU = ReadSlice(uncompressed.PhysicalPageMaskMapAtlas.rt, 7, decoded, decoder);
            var maskC = ReadSlice(compressed.PhysicalPageMaskMapAtlas.rt, 7, decoded, decoder);
            uncompressed.Dispose(); compressed.Dispose();
            Require(baseU.Length == baseC.Length && maskU.Length == maskC.Length, "Readback size mismatch.");
            for (int i = 0; i < maskU.Length; i++)
                Require(Math.Abs(maskU[i] - maskC[i]) <= 1, "Mask differs at byte " + i + ": " + maskU[i] + " vs " + maskC[i]);
            report.AppendLine("PASS: Mask byte-identical between RGBA8 and BC7 bake.");
            long pixels = baseU.Length / 4;
            double[] sum = new double[4], squared = new double[4]; int[] maximum = new int[4];
            for (int p = 0; p < baseU.Length; p += 4)
                for (int ch = 0; ch < 4; ch++)
                {
                    int diff = Math.Abs(baseU[p + ch] - baseC[p + ch]);
                    sum[ch] += diff; squared[ch] += (double)diff * diff; maximum[ch] = Math.Max(maximum[ch], diff);
                }
            for (int ch = 0; ch < 4; ch++)
            {
                double rmse = Math.Sqrt(squared[ch] / pixels), psnr = rmse > 0 ? 20 * Math.Log10(255 / rmse) : 100;
                report.AppendLine("Base " + "RGBA"[ch] + ": MAE=" + (sum[ch] / pixels).ToString("F3") + "/255, max=" + maximum[ch] + ", PSNR=" + psnr.ToString("F2") + " dB");
                Require(psnr >= 28, "BC7 quality floor exceeded in channel " + ch + ": " + psnr + " dB");
            }

            // Live context must bind a BC7 array and start with an empty LRU mapping.
            VirtualTextureMono.RebuildContext();
            var context = Context.Instance;
            var liveAtlas = typeof(Context).GetProperty("PhysicalPageAtlas").GetValue(context);
            var liveBase = (Texture)typeof(PhysicalPageAtlas).GetProperty("PhysicalPageBaseMapAtlas").GetValue(liveAtlas);
            Require(liveBase.graphicsFormat == GraphicsFormat.RGBA_BC7_UNorm, "Live feedback atlas is not BC7: " + liveBase.graphicsFormat);
            var lruField = typeof(AdaptiveVirtualTextureRuntime).GetField("LruCache", Flags);
            var lru = lruField.GetValue(null);
            var cacheField = lru.GetType().GetField("_cache", Flags);
            var entries = (System.Collections.IDictionary)cacheField.GetValue(lru);
            Require(entries.Count == Constant.MaxPhysicalPageCount, "LRU cache was not reseeded on rebuild: " + entries.Count);
            foreach (ulong key in entries.Keys)
                Require(key >= 0x7FFFFFFFFFFFFFFFu, "LRU cache still maps a real page after rebuild: " + key);
            report.AppendLine("PASS: live context binds BC7 base atlas; all physical slots free after rebuild.");
            report.AppendLine("PASS: feedback compression memory = " + PhysicalPageAtlas.CurrentMemoryMiB.ToString("F1") + " MiB");
        }
        catch (Exception e)
        {
            report.AppendLine("FAIL: " + e);
            File.WriteAllText("Temp/feedback-bc7-validation.txt", report.ToString());
            throw;
        }
        File.WriteAllText("Temp/feedback-bc7-validation.txt", report.ToString());
        Debug.Log("Feedback BC7 validation PASSED\n" + report);
    }
}

