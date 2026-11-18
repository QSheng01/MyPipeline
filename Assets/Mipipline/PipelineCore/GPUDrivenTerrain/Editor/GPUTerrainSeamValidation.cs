using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using MPipeline;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>Compare actual displaced fine-edge vertices with adjacent coarse mesh edges.</summary>
public static class GPUTerrainSeamValidation
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    const string Output = "GPUDrivenTerrainValidation/seam-validation.txt";

    struct EdgePair
    {
        internal int Fine, Coarse, FineEdge, CoarseEdge, Delta;
    }

    static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }

    static int ValidateView(GPUTerrain gpu, Camera camera, ComputeShader shader, StringBuilder report, string name)
    {
        camera.Render();
        object builder = typeof(GPUTerrain).GetField("builder", Flags).GetValue(gpu);
        var cameras = (IDictionary)builder.GetType().GetField("cameras", Flags).GetValue(builder);
        object cameraBuffers = cameras[camera];
        var buffer = (ComputeBuffer)cameraBuffers.GetType().GetField("Shadow", Flags).GetValue(cameraBuffers);
        var argsBuffer = (ComputeBuffer)cameraBuffers.GetType().GetField("ShadowArgs", Flags).GetValue(cameraBuffers);
        var args = new uint[5]; argsBuffer.GetData(args);
        int count = (int)args[1], grid = gpu.PatchGridSize;
        Require(count > 0 && count <= gpu.PatchCapacity, "Invalid patch count.");
        var patches = new uint[count * 4]; buffer.GetData(patches, 0, 0, patches.Length);
        var coverage = new int[grid * grid];
        for (int i = 0; i < count; i++)
        {
            int x = (int)patches[i * 4], y = (int)patches[i * 4 + 1], side = 1 << (int)patches[i * 4 + 2];
            Require(x >= 0 && y >= 0 && x + side <= grid && y + side <= grid, "Patch out of bounds.");
            for (int z = y; z < y + side; z++) for (int xx = x; xx < x + side; xx++)
            {
                int cell = z * grid + xx;
                Require(coverage[cell] == 0, "Patch coverage overlaps.");
                coverage[cell] = i + 1;
            }
        }

        var pairs = new List<EdgePair>(512);
        int maximumDelta = 0;
        for (int i = 0; i < count; i++)
        {
            int x = (int)patches[i * 4], y = (int)patches[i * 4 + 1];
            int lod = (int)patches[i * 4 + 2], side = 1 << lod;
            uint packed = patches[i * 4 + 3];
            for (int edge = 0; edge < 4; edge++)
            {
                int delta = (int)((packed >> (edge * 8)) & 255);
                if (delta == 0) continue;
                int adjacentX = edge == 0 ? x - 1 : edge == 2 ? x + side : x;
                int adjacentY = edge == 1 ? y - 1 : edge == 3 ? y + side : y;
                Require(adjacentX >= 0 && adjacentY >= 0 && adjacentX < grid && adjacentY < grid,
                    "LOD transition points outside terrain.");
                int neighbor = coverage[adjacentY * grid + adjacentX] - 1;
                Require(neighbor >= 0 && (int)patches[neighbor * 4 + 2] == lod + delta,
                    "LOD map points to the wrong coarse patch.");
                pairs.Add(new EdgePair { Fine = i, Coarse = neighbor, FineEdge = edge,
                    CoarseEdge = (edge + 2) & 3, Delta = delta });
                maximumDelta = Math.Max(maximumDelta, delta);
            }
        }
        if (pairs.Count == 0)
        {
            report.AppendLine($"PASS {name}: {count} patches, no LOD transitions at this camera distance.");
            return 0;
        }

        var queries = new Vector2Int[pairs.Count * 2];
        for (int i = 0; i < pairs.Count; i++)
        {
            queries[i * 2] = new Vector2Int(pairs[i].Fine, pairs[i].FineEdge);
            queries[i * 2 + 1] = new Vector2Int(pairs[i].Coarse, pairs[i].CoarseEdge);
        }
        var points = new Vector4[queries.Length * 17];
        int kernel = shader.FindKernel("SeamEdges");
        using (var input = new ComputeBuffer(queries.Length, 8))
        using (var output = new ComputeBuffer(points.Length, 16))
        {
            input.SetData(queries);
            shader.SetBuffer(kernel, "EdgeQueries", input);
            shader.SetBuffer(kernel, "EdgePositions", output);
            shader.SetBuffer(kernel, "_GPUTerrainPatches", buffer);
            shader.SetTexture(kernel, "_GPUTerrainHeight", gpu.HeightTexture);
            shader.SetTexture(kernel, "_GPUTerrainNormal", gpu.UsingBakedTextures
                ? (Texture)gpu.bakedNormal : Texture2D.normalTexture);
            shader.SetVector("_GPUTerrainOrigin", gpu.transform.position);
            shader.SetVector("_GPUTerrainSize", gpu.GetComponent<Terrain>().terrainData.size);
            shader.SetVector("_GPUTerrainInfo", new Vector4(grid,
                gpu.GetComponent<Terrain>().terrainData.heightmapResolution, 0, 16));
            shader.SetInt("_GPUTerrainStitch", 1);
            shader.SetInt("_GPUTerrainUseBakedNormal", gpu.UsingBakedTextures ? 1 : 0);
            shader.SetInt("EdgeQueryCount", queries.Length);
            shader.Dispatch(kernel, (points.Length + 63) / 64, 1, 1);
            output.GetData(points);
        }

        float maximumGap = 0;
        int comparisons = 0;
        for (int i = 0; i < pairs.Count; i++)
        {
            EdgePair pair = pairs[i];
            int fineOffset = i * 34, coarseOffset = fineOffset + 17;
            bool alongZ = pair.FineEdge == 0 || pair.FineEdge == 2;
            for (int v = 0; v < 17; v++)
            {
                Vector3 fine = points[fineOffset + v];
                float coordinate = alongZ ? fine.z : fine.x;
                int segment = -1;
                for (int c = 0; c < 16; c++)
                {
                    float start = alongZ ? points[coarseOffset + c].z : points[coarseOffset + c].x;
                    float end = alongZ ? points[coarseOffset + c + 1].z : points[coarseOffset + c + 1].x;
                    if (coordinate >= start - 0.002f && coordinate <= end + 0.002f)
                    { segment = c; break; }
                }
                Require(segment >= 0, "Fine edge falls outside neighboring coarse edge.");
                Vector3 a = points[coarseOffset + segment], b = points[coarseOffset + segment + 1];
                float startCoordinate = alongZ ? a.z : a.x;
                float endCoordinate = alongZ ? b.z : b.x;
                Vector3 coarseSurface = Vector3.Lerp(a, b, Mathf.Clamp01(
                    (coordinate - startCoordinate) / (endCoordinate - startCoordinate)));
                maximumGap = Mathf.Max(maximumGap, Vector3.Distance(fine, coarseSurface));
                comparisons++;
            }
        }
        Require(maximumGap < 0.01f, $"Actual fine/coarse edge gap {maximumGap:F5} m at {name}.");
        report.AppendLine($"PASS {name}: {count} patches, {pairs.Count} LOD transition edges, "
            + $"{comparisons} actual shader edge vertices, max LOD delta {maximumDelta}, maximum 3D gap {maximumGap:F6} m.");
        return pairs.Count;
    }

    [MenuItem("Tools/GPU Terrain/Validate LOD Seams")]
    public static void Run()
    {
        Require(!Application.isPlaying, "Run this validation in Edit mode.");
        var gpu = Object.FindObjectOfType<GPUTerrain>(); var camera = Camera.main;
        Require(gpu != null && camera != null, "Current scene needs GPUTerrain and Main Camera.");
        var shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(
            "Assets/Mipipline/PipelineCore/GPUDrivenTerrain/Editor/GPUTerrainValidation.compute");
        Require(shader != null, "Geometry validation compute shader missing.");
        Vector3 position = camera.transform.position; Quaternion rotation = camera.transform.rotation;
        bool hiZ = gpu.hiZOcclusion, stitch = gpu.stitchEdges;
        float lodDistance = gpu.lodDistance;
        var report = new StringBuilder();
        try
        {
            gpu.hiZOcclusion = false;
            gpu.stitchEdges = true;
            int transitions = ValidateView(gpu, camera, shader, report, "current camera");
            var terrain = gpu.GetComponent<Terrain>();
            Vector3 near = new Vector3(1309, terrain.SampleHeight(new Vector3(1309, 0, 1069)) + 15, 1069);
            camera.transform.SetPositionAndRotation(near, Quaternion.Euler(30, 20, 0));
            transitions += ValidateView(gpu, camera, shader, report, "near terrain");
            camera.transform.SetPositionAndRotation(new Vector3(1024, 1000, 1024), Quaternion.Euler(70, 125, 0));
            transitions += ValidateView(gpu, camera, shader, report, "far terrain");
            gpu.lodDistance = 3f;
            camera.transform.SetPositionAndRotation(position, rotation);
            transitions += ValidateView(gpu, camera, shader, report, "LOD distance 3");
            Require(transitions > 0, "No LOD seam was validated.");
            report.AppendLine("PASS ALL: tested GPU displaced positions at every sampled LOD transition edge.");
        }
        catch (Exception error) { report.AppendLine("FAIL: " + error); throw; }
        finally
        {
            gpu.hiZOcclusion = hiZ; gpu.stitchEdges = stitch; gpu.lodDistance = lodDistance;
            camera.transform.SetPositionAndRotation(position, rotation);
            camera.Render(); SceneView.RepaintAll();
            Directory.CreateDirectory(Path.GetDirectoryName(Output));
            File.WriteAllText(Output, report.ToString());
            Debug.Log(report.ToString());
        }
    }
}
