using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MPipeline;
using NoOvertime.VirtualTexture;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

/// <summary>Explicit GPU readback tests, never part of the renderer's frame loop.</summary>
public static class GPUDrivenTerrainValidation
{
    const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    const string Output = "GPUDrivenTerrainValidation";
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    static T Field<T>(object value, string field) => (T)value.GetType().GetField(field, Flags).GetValue(value);
    static object Builder(GPUTerrain terrain) => Field<object>(terrain, "builder");
    static object Buffers(GPUTerrain terrain, Camera camera) => Field<IDictionary>(Builder(terrain), "cameras")[camera];
    static uint[] Args(object buffers, string name)
    { var result = new uint[5]; Field<ComputeBuffer>(buffers, name).GetData(result); return result; }
    static uint[] Patches(object buffers, string name, int count)
    { var result = new uint[count * 4]; if (count > 0) Field<ComputeBuffer>(buffers, name).GetData(result, 0, 0, result.Length); return result; }

    static string[] VisibleSet(GPUTerrain gpu, Camera camera)
    {
        object buffers = Buffers(gpu, camera);
        int count = (int)Args(buffers, "Args")[1];
        uint[] patches = Patches(buffers, "Visible", count);
        return Enumerable.Range(0, count).Select(i => $"{patches[i * 4]},{patches[i * 4 + 1]},{patches[i * 4 + 2]},{patches[i * 4 + 3]}")
            .OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    [MenuItem("Tools/GPU Terrain/Validate Scene View Culling")]
    public static void ValidateSceneViewCulling()
    {
        var gpu = Object.FindObjectOfType<GPUTerrain>();
        Camera game = Camera.main;
        Camera observer = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.camera : null;
        Require(gpu != null && game != null && observer != null, "GPU Terrain, Main Camera and an open Scene view are required.");
        string settings = JsonUtility.ToJson(gpu);
        Vector3 gamePosition = game.transform.position, observerPosition = observer.transform.position;
        Quaternion gameRotation = game.transform.rotation, observerRotation = observer.transform.rotation;
        var report = new StringBuilder();
        try
        {
            gpu.gpuRendering = true; gpu.hiZOcclusion = false; gpu.frustumCulling = true;
            gpu.sceneViewUsesGameCamera = true; gpu.sceneViewCullingCamera = null;
            game.Render(); observer.Render();
            var gameSet = VisibleSet(gpu, game);
            Require(VisibleSet(gpu, observer).SequenceEqual(gameSet), "Scene view does not draw the game's visible patch set.");
            report.AppendLine($"PASS current views: Main and Scene each submit the same {gameSet.Length} visible patches, including matching LOD/seam data.");

            observer.transform.SetPositionAndRotation(observerPosition + new Vector3(400, 500, -300), Quaternion.Euler(70, 110, 0));
            observer.Render();
            Require(VisibleSet(gpu, observer).SequenceEqual(gameSet), "Moving the Scene observer changed game-camera selection.");
            Require(VisibleSet(gpu, game).SequenceEqual(gameSet), "Scene observer overwrote Game buffers.");
            report.AppendLine("PASS moving the Scene camera preserves the main-camera visible set and leaves Game buffers untouched.");

            game.transform.rotation = gameRotation * Quaternion.Euler(0, 180, 0);
            game.Render(); observer.Render();
            var rotated = VisibleSet(gpu, game);
            Require(VisibleSet(gpu, observer).SequenceEqual(rotated), "Scene view did not follow the rotated game camera.");
            report.AppendLine($"PASS rotating Main Camera: Scene matches its updated {rotated.Length} visible patches.");

            var terrain = gpu.GetComponent<Terrain>();
            game.transform.SetPositionAndRotation(terrain.transform.position + terrain.terrainData.size * 0.5f
                + Vector3.up * (terrain.terrainData.size.y + 1000), Quaternion.Euler(-90, 0, 0));
            game.Render(); observer.Render();
            Require(VisibleSet(gpu, game).Length == 0 && VisibleSet(gpu, observer).Length == 0, "Terrain was not fully rejected when the source camera looked away above the terrain.");
            report.AppendLine("PASS looking above/away from terrain: both cameras submit 0 color patches.");

            observer.transform.SetPositionAndRotation(observerPosition, observerRotation);
            gpu.sceneViewUsesGameCamera = false;
            observer.Render();
            Require(gpu.GetCullingCamera(observer) == observer && VisibleSet(gpu, observer).Length > 0, "Independent Scene-camera mode did not restore terrain visibility.");
            report.AppendLine($"PASS independent Scene mode restores {VisibleSet(gpu, observer).Length} patches while Main still looks away.");
            report.AppendLine("PASS ALL; camera poses, component settings and current scene preserved.");
        }
        catch (Exception error) { report.AppendLine("FAIL: " + error); throw; }
        finally
        {
            JsonUtility.FromJsonOverwrite(settings, gpu);
            game.transform.SetPositionAndRotation(gamePosition, gameRotation);
            observer.transform.SetPositionAndRotation(observerPosition, observerRotation);
            game.Render(); observer.Render(); SceneView.RepaintAll();
            Directory.CreateDirectory(Output);
            File.WriteAllText(Path.Combine(Output, "scene-culling-validation.txt"), report.ToString());
            Debug.Log(report.ToString());
        }
    }

    public static void Capture(Camera camera, string name)
    {
        camera.Render();
        var previous = RenderTexture.active;
        var texture = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.RGB24, false);
        try
        {
            RenderTexture.active = camera.targetTexture;
            texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); texture.Apply();
            Directory.CreateDirectory(Output);
            File.WriteAllBytes(Path.Combine(Output, name + ".png"), texture.EncodeToPNG());
        }
        finally { RenderTexture.active = previous; Object.DestroyImmediate(texture); }
    }

    static int ValidatePartition(GPUTerrain gpu, Camera camera, StringBuilder report, string label)
    {
        camera.Render();
        object buffers = Buffers(gpu, camera);
        int count = (int)Args(buffers, "ShadowArgs")[1];
        int visible = (int)Args(buffers, "Args")[1];
        Require(count > 0 && count <= gpu.PatchCapacity, "Invalid selected patch count.");
        Require(visible <= count, "Visible count exceeds selected count.");
        uint[] patches = Patches(buffers, "Shadow", count);
        var coverage = new int[gpu.PatchCapacity];
        int grid = gpu.PatchGridSize, nodeGrid = gpu.GridSize;
        var expectedLOD = new uint[nodeGrid * nodeGrid];
        for (int p = 0; p < count; p++)
        {
            int x = (int)patches[p * 4], z = (int)patches[p * 4 + 1], lod = (int)patches[p * 4 + 2], side = 1 << lod;
            Require(x % side == 0 && z % side == 0 && x + side <= grid && z + side <= grid, "Invalid quadtree alignment.");
            for (int y = z; y < z + side; y++) for (int xx = x; xx < x + side; xx++)
            { int cell = y * grid + xx; coverage[cell]++; expectedLOD[(y / 8) * nodeGrid + xx / 8] = (uint)lod; }
        }
        Require(coverage.All(v => v == 1), "Quadtree has gaps or overlaps.");
        var read = AsyncGPUReadback.Request(Field<RenderTexture>(buffers, "LodMap")); read.WaitForCompletion();
        Require(!read.hasError, "LOD map readback failed.");
        Require(read.GetData<uint>().ToArray().SequenceEqual(expectedLOD), "LOD map differs from selected partition.");
        int transitions = 0;
        for (int p = 0; p < count; p++)
        {
            int x = (int)patches[p * 4], z = (int)patches[p * 4 + 1], lod = (int)patches[p * 4 + 2], side = 1 << lod;
            int[] nx = { x / 8 - 1, x / 8, (x + side) / 8, x / 8 }, nz = { z / 8, z / 8 - 1, z / 8, (z + side) / 8 };
            for (int edge = 0; edge < 4; edge++)
            {
                bool atNodeEdge = edge == 0 ? x % (8 * side) == 0
                    : edge == 1 ? z % (8 * side) == 0
                    : edge == 2 ? x % (8 * side) == 7 * side
                    : z % (8 * side) == 7 * side;
                int expected = !atNodeEdge || nx[edge] < 0 || nz[edge] < 0 || nx[edge] >= nodeGrid || nz[edge] >= nodeGrid
                    ? 0 : Math.Max(0, (int)expectedLOD[nz[edge] * nodeGrid + nx[edge]] - lod);
                Require(((patches[p * 4 + 3] >> (edge * 8)) & 255) == expected, "Incorrect neighbouring LOD delta.");
                if (expected > 0) transitions++;
            }
        }
        Require(count % 64 == 0, "Each final node must expand into exactly 8x8 patches.");
        report.AppendLine($"PASS {label}: {count / 64} leaves × 64 = {count} patches, visible={visible}, stitched edges={transitions}; complete non-overlapping partition, GPU LOD map and neighbour deltas agree.");
        if (gpu.frustumCulling && !gpu.hiZOcclusion)
        {
            var source = gpu.GetComponent<Terrain>();
            var minMaxBuffer = Field<ComputeBuffer>(Builder(gpu), "minMax");
            var ranges = new Vector2[minMaxBuffer.count]; minMaxBuffer.GetData(ranges);
            var frustum = GeometryUtility.CalculateFrustumPlanes(camera);
            Func<int, int, int, Vector2> rangeAt = (x, z, lod) =>
            {
                int n = grid >> lod;
                int offset = (grid * grid * 4 - n * n * 4) / 3;
                return ranges[offset + (z >> lod) * n + (x >> lod)];
            };
            var expected = new System.Collections.Generic.HashSet<string>();
            var conservative = new System.Collections.Generic.HashSet<string>();
            for (int p = 0; p < count; p++)
            {
                int x = (int)patches[p * 4], z = (int)patches[p * 4 + 1], lod = (int)patches[p * 4 + 2], side = 1 << lod;
                Vector2 range = rangeAt(x, z, lod);
                int[] nx = { x / 8 - 1, x / 8, (x + side) / 8, x / 8 }, nz = { z / 8, z / 8 - 1, z / 8, (z + side) / 8 };
                for (int edge = 0; edge < 4; edge++)
                {
                    int delta = (int)((patches[p * 4 + 3] >> (edge * 8)) & 255);
                    if (delta == 0) continue;
                    Vector2 neighbour = rangeAt(nx[edge] * 8, nz[edge] * 8, lod + delta + 3);
                    range.x = Mathf.Min(range.x, neighbour.x); range.y = Mathf.Max(range.y, neighbour.y);
                }
                Vector3 a = source.transform.position + new Vector3(x * source.terrainData.size.x / grid, range.x * source.terrainData.size.y - gpu.boundsPadding, z * source.terrainData.size.z / grid);
                Vector3 b = source.transform.position + new Vector3((x + side) * source.terrainData.size.x / grid, range.y * source.terrainData.size.y + gpu.boundsPadding, (z + side) * source.terrainData.size.z / grid);
                var box = new Bounds((a + b) * 0.5f, b - a);
                if (GeometryUtility.TestPlanesAABB(frustum, box)) expected.Add($"{x},{z},{lod}");
                // GPU fused multiply/add differs at exact plane contact. Rendering
                // deliberately keeps a 1 cm plane margin; allow 2 cm in this oracle.
                box.Expand(0.04f);
                if (GeometryUtility.TestPlanesAABB(frustum, box)) conservative.Add($"{x},{z},{lod}");
            }
            uint[] actual = Patches(buffers, "Visible", visible);
            for (int p = 0; p < visible; p++)
            {
                string key = $"{actual[p * 4]},{actual[p * 4 + 1]},{actual[p * 4 + 2]}";
                Require(conservative.Remove(key), "Unexpected or duplicate visible patch."); expected.Remove(key);
            }
            Require(expected.Count == 0, "GPU frustum discarded a CPU-visible AABB.");
            report.AppendLine("PASS " + label + ": GPU frustum/AABB set agrees with Unity CPU test within 2 cm plane tolerance; no false rejections, including stitched-edge bounds.");
        }
        return count;
    }

    static void ValidateHeightAndGeometry(GPUTerrain gpu, Camera camera, StringBuilder report)
    {
        var source = gpu.GetComponent<Terrain>();
        var data = source.terrainData;
        int res = data.heightmapResolution;
        float[,] cpu = data.GetHeights(0, 0, res, res);
        var request = AsyncGPUReadback.Request(gpu.HeightTexture); request.WaitForCompletion();
        Require(!request.hasError, "Height readback failed.");
        var heights = request.GetData<float>();
        float maxError = 0;
        for (int y = 0; y < res; y += 17) for (int x = 0; x < res; x += 13)
            maxError = Mathf.Max(maxError, Mathf.Abs(heights[y * res + x] - cpu[y, x]) * data.size.y);
        Require(maxError < 0.002f, "GPU height does not match TerrainData: " + maxError);
        var bounds = Field<ComputeBuffer>(Builder(gpu), "minMax");
        var ranges = new Vector2[bounds.count]; bounds.GetData(ranges);
        int grid = gpu.PatchGridSize, step = (res - 1) / grid;
        for (int z = 0; z < grid; z++) for (int x = 0; x < grid; x++)
        {
            Vector2 range = ranges[z * grid + x];
            for (int y = z * step; y <= (z + 1) * step; y++) for (int xx = x * step; xx <= (x + 1) * step; xx++)
                Require(cpu[y, xx] >= range.x - 0.000001f && cpu[y, xx] <= range.y + 0.000001f, "Min/max excludes a height sample.");
        }
        object buffers = Buffers(gpu, camera);
        int total = (int)Args(buffers, "ShadowArgs")[1];
        uint[] patches = Patches(buffers, "Shadow", total);
        var samples = new System.Collections.Generic.List<uint>(512);
        for (int i = 0; i < total && samples.Count < 256; i++)
            if (patches[i * 4 + 3] != 0) samples.Add((uint)i);
        for (int i = 0; i < total && samples.Count < 512; i += Math.Max(1, total / 512))
            if (!samples.Contains((uint)i)) samples.Add((uint)i);
        int count = samples.Count;
        var shader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Mipipline/PipelineCore/GPUDrivenTerrain/Editor/GPUTerrainValidation.compute");
        using (var output = new ComputeBuffer(count * 289, 16))
        using (var indices = new ComputeBuffer(count, 4))
        {
            indices.SetData(samples);
            shader.SetBuffer(0, "Positions", output);
            shader.SetBuffer(0, "TestPatchIndices", indices);
            shader.SetBuffer(0, "_GPUTerrainPatches", Field<ComputeBuffer>(buffers, "Shadow"));
            shader.SetTexture(0, "_GPUTerrainHeight", gpu.HeightTexture);
            shader.SetTexture(0, "_GPUTerrainNormal", gpu.UsingBakedTextures ? (Texture)gpu.bakedNormal : Texture2D.normalTexture);
            shader.SetInt("_GPUTerrainUseBakedNormal", gpu.UsingBakedTextures ? 1 : 0);
            shader.SetVector("_GPUTerrainOrigin", source.transform.position);
            shader.SetVector("_GPUTerrainSize", data.size);
            shader.SetVector("_GPUTerrainInfo", new Vector4(grid, res, 0, 16));
            shader.SetInt("_GPUTerrainStitch", 1); shader.SetInt("PatchCount", count);
            shader.Dispatch(0, (count * 289 + 63) / 64, 1, 1);
            var positions = new Vector4[count * 289]; output.GetData(positions);
            int seamVertices = 0;
            float geometryError = 0;
            for (int p = 0; p < count; p++) for (int v = 0; v < 289; v++)
            {
                int index = (int)samples[p] * 4;
                int vx = v % 17, vz = v / 17, lod = (int)patches[index + 2];
                float sampleX = (patches[index] + vx * (1 << lod) / 16f) * step;
                float sampleZ = (patches[index + 1] + vz * (1 << lod) / 16f) * step;
                float expected = cpu[Mathf.RoundToInt(sampleZ), Mathf.RoundToInt(sampleX)];
                int edge = -1;
                if (vx == 0 && (patches[index + 3] & 255) > 0) edge = 0;
                else if (vx == 16 && ((patches[index + 3] >> 16) & 255) > 0) edge = 2;
                else if (vz == 0 && ((patches[index + 3] >> 8) & 255) > 0) edge = 1;
                else if (vz == 16 && (patches[index + 3] >> 24) > 0) edge = 3;
                if (edge >= 0)
                {
                    int delta = (int)((patches[index + 3] >> (edge * 8)) & 255);
                    int spacing = step * (1 << (lod + delta)) / 16;
                    bool zAxis = edge == 0 || edge == 2;
                    float coord = zAxis ? sampleZ : sampleX;
                    int low = Mathf.FloorToInt(coord / spacing) * spacing, high = Math.Min(low + spacing, res - 1);
                    float a = zAxis ? cpu[low, (int)sampleX] : cpu[(int)sampleZ, low];
                    float b = zAxis ? cpu[high, (int)sampleX] : cpu[(int)sampleZ, high];
                    expected = Mathf.Lerp(a, b, (coord - low) / spacing);
                    seamVertices++;
                }
                Vector4 actual = positions[p * 289 + v];
                float worldY = source.transform.position.y + expected * data.size.y;
                geometryError = Mathf.Max(geometryError, Mathf.Abs(actual.y - worldY));
                Require(Mathf.Abs(actual.x - (source.transform.position.x + sampleX * data.size.x / (res - 1))) < 0.002f
                    && Mathf.Abs(actual.z - (source.transform.position.z + sampleZ * data.size.z / (res - 1))) < 0.002f, "Geometry XZ drift.");
            }
            Require(seamVertices > 0, "No LOD transitions tested.");
            Require(geometryError < 0.003f, "Stitched vertex differs from independent CPU height interpolation: " + geometryError);
            report.AppendLine($"PASS height/geometry: height error <= {maxError:F6} m; all finest-patch bounds contain every source sample; {positions.Length} real shader vertices and {seamVertices} seam vertices checked, height error <= {geometryError:F6} m.");
        }
    }

    [MenuItem("Tools/GPU Terrain/Validate Current Scene")]
    public static void Run()
    {
        var gpu = Object.FindObjectOfType<GPUTerrain>(); var camera = Camera.main;
        Require(gpu != null && camera != null, "GPU Terrain and Main Camera required.");
        string settings = JsonUtility.ToJson(gpu);
        Vector3 position = camera.transform.position; Quaternion rotation = camera.transform.rotation;
        RenderTexture target = camera.targetTexture;
        bool clipmap = ClipmapTexture.UseClipmap, clipmapDebug = ClipmapTexture.DebugColors;
        var cameraData = camera.GetUniversalAdditionalCameraData();
        var serializedCamera = new SerializedObject(cameraData);
        int rendererIndex = serializedCamera.FindProperty("m_RendererIndex").intValue;
        var rt = new RenderTexture(1280, 720, 24);
        var report = new StringBuilder();
        report.AppendLine($"Unity {Application.unityVersion}; {SystemInfo.graphicsDeviceName}; {SystemInfo.graphicsDeviceType}; play={Application.isPlaying}");
        report.AppendLine("Scene: " + camera.gameObject.scene.path);
        try
        {
            camera.targetTexture = rt;
            gpu.gpuRendering = true; gpu.hiZOcclusion = false; gpu.collectStatistics = false; gpu.debugLOD = false; gpu.stitchEdges = true;
            gpu.frustumCulling = true;
            ClipmapTexture.DebugColors = false;
            int normal = ValidatePartition(gpu, camera, report, "current camera");
            ValidateHeightAndGeometry(gpu, camera, report);
            object first = Buffers(gpu, camera);
            uint[] selectedBefore = Patches(first, "Shadow", normal);
            var secondObject = new GameObject("GPU Terrain Validation Camera") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var second = secondObject.AddComponent<Camera>(); second.CopyFrom(camera); second.enabled = false;
                second.transform.SetPositionAndRotation(new Vector3(50, 900, 50), Quaternion.Euler(45, 45, 0));
                ValidatePartition(gpu, second, report, "second camera");
                Require(!ReferenceEquals(first, Buffers(gpu, second)), "Cameras share mutable GPU buffers.");
                Require(selectedBefore.SequenceEqual(Patches(first, "Shadow", normal)), "Second camera changed Main Camera buffers.");
                report.AppendLine("PASS camera isolation: rendering another view preserves Main Camera selection.");
            }
            finally { Object.DestroyImmediate(secondObject); }
            gpu.frustumCulling = false;
            ValidatePartition(gpu, camera, report, "frustum disabled");
            Require(Args(Buffers(gpu, camera), "Args")[1] == Args(Buffers(gpu, camera), "ShadowArgs")[1], "Unculled draw missed patches.");
            gpu.frustumCulling = true;
            var terrain = gpu.GetComponent<Terrain>();
            camera.transform.position = new Vector3(1309, terrain.SampleHeight(new Vector3(1309, 0, 1069)) + 10, 1069);
            int near = ValidatePartition(gpu, camera, report, "near ground");
            camera.transform.SetPositionAndRotation(new Vector3(1024, 3000, 1024), Quaternion.Euler(90, 0, 0));
            int far = ValidatePartition(gpu, camera, report, "far overview");
            Require(near > far, "LOD does not refine near the ground.");
            camera.transform.SetPositionAndRotation(new Vector3(-100, 180, -100), Quaternion.Euler(25, 45, 0));
            ValidatePartition(gpu, camera, report, "outside terrain boundary");
            camera.transform.SetPositionAndRotation(position, rotation);
            for (int mode = 0; mode < 2; mode++)
            {
                ClipmapTexture.UseClipmap = mode != 0;
                for (int path = 0; path < 2; path++)
                {
                    cameraData.SetRenderer(path);
                    Capture(camera, (mode == 0 ? "feedback" : "clipmap") + (path == 0 ? "-forward" : "-deferred"));
                }
            }
            gpu.debugLOD = true; cameraData.SetRenderer(0);
            Capture(camera, "lod-debug");
            gpu.debugLOD = false;
            var collider = terrain.GetComponent<TerrainCollider>();
            gpu.gpuRendering = false; camera.Render();
            Require(terrain.drawHeightmap && collider != null && collider.terrainData == terrain.terrainData, "Unity Terrain fallback/collider not preserved.");
            gpu.gpuRendering = true; camera.Render();
            Require(!terrain.drawHeightmap && gpu.IsReady, "GPU path did not resume.");
            var errors = ShaderUtil.GetShaderMessages(Shader.Find("MPipeline/GPU Terrain AVT")).Where(m => m.severity == ShaderCompilerMessageSeverity.Error).ToArray();
            Require(errors.Length == 0, "GPU terrain shader errors: " + string.Join("; ", errors.Select(e => e.message)));
            Require(ShaderUtil.GetComputeShaderMessages(Resources.Load<ComputeShader>("GPUTerrainBuild")).All(m => m.severity != ShaderCompilerMessageSeverity.Error), "Terrain compute shader errors.");
            report.AppendLine("PASS Feedback/Clipmap x Forward/Deferred captures, LOD debug capture, collider retained, Unity Terrain toggle restores drawing, GPU resumes, no terrain shader errors.");
            report.AppendLine("PASS ALL");
        }
        catch (Exception error) { report.AppendLine("FAIL: " + error); throw; }
        finally
        {
            JsonUtility.FromJsonOverwrite(settings, gpu);
            camera.transform.SetPositionAndRotation(position, rotation); camera.targetTexture = target;
            cameraData.SetRenderer(rendererIndex);
            ClipmapTexture.UseClipmap = clipmap; ClipmapTexture.DebugColors = clipmapDebug;
            Object.DestroyImmediate(rt);
            Directory.CreateDirectory(Output); File.WriteAllText(Path.Combine(Output, "validation.txt"), report.ToString());
            Debug.Log(report.ToString());
        }
    }
}
