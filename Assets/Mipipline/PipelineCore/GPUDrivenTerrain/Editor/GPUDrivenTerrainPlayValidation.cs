using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MPipeline;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public static class GPUDrivenTerrainPlayValidation
{
    const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    const string Output = "GPUDrivenTerrainValidation";
    static bool running;
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }

    [MenuItem("Tools/GPU Terrain/Validate Hi-Z and Movement (Play Mode)")]
    static async void RunMenu()
    {
        try { await Run(); }
        catch (Exception error) { Debug.LogException(error); }
    }

    static async Task Frames(int count)
    {
        for (int i = 0; i < count; i++)
        {
            int frame = Time.frameCount;
            var timeout = DateTime.UtcNow.AddSeconds(10);
            while (Time.frameCount == frame)
            {
                Require(Application.isPlaying && DateTime.UtcNow < timeout, "Play mode stopped or no frames rendered (unpause the editor).");
                await Task.Delay(1);
            }
        }
    }

    static uint Count(GPUTerrain gpu, Camera camera)
    {
        object builder = typeof(GPUTerrain).GetField("builder", Flags).GetValue(gpu);
        var dict = (IDictionary)builder.GetType().GetField("cameras", Flags).GetValue(builder);
        object buffers = dict[camera];
        var args = (ComputeBuffer)buffers.GetType().GetField("Args", Flags).GetValue(buffers);
        var values = new uint[5]; args.GetData(values); return values[1];
    }

    static Color32[] Capture(RenderTexture target, string name)
    {
        var previous = RenderTexture.active;
        var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        try
        {
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); texture.Apply();
            File.WriteAllBytes(Path.Combine(Output, name + ".png"), texture.EncodeToPNG());
            return texture.GetPixels32();
        }
        finally { RenderTexture.active = previous; Object.DestroyImmediate(texture); }
    }

    static async Task CompareHiZ(GPUTerrain gpu, Camera camera, RenderTexture target, string label, StringBuilder report, bool requireReduction)
    {
        gpu.hiZOcclusion = false; await Frames(16);
        Color32[] baseline = Capture(target, label + "-off"); uint before = Count(gpu, camera);
        gpu.hiZOcclusion = true; await Frames(16);
        for (int i = 0; !gpu.LastUsedHiZ && i < 16; i++) await Frames(1);
        Require(gpu.LastUsedHiZ, "Hi-Z did not activate for a static camera.");
        Color32[] actual = Capture(target, label + "-on"); uint after = Count(gpu, camera);
        int changed = 0;
        for (int i = 0; i < baseline.Length; i++) if (!baseline[i].Equals(actual[i])) changed++;
        Require(changed == 0, label + " changed " + changed + " visible pixels.");
        Require(after <= before && (!requireReduction || after < before), "Hi-Z did not conservatively reduce the patch list.");
        report.AppendLine($"PASS {label}: {before} -> {after} patches; 0 differing pixels at {target.width}x{target.height}.");
    }

    public static async Task Run()
    {
        Require(!running && Application.isPlaying, "Run once in Play mode.");
        var gpu = Object.FindObjectOfType<GPUTerrain>(); var camera = Camera.main;
        Require(gpu != null && camera != null && SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11, "Current validation requires Main Camera, GPU Terrain and D3D11.");
        running = true;
        Directory.CreateDirectory(Output);
        var report = new StringBuilder(); var errors = new List<string>();
        string settings = JsonUtility.ToJson(gpu);
        Vector3 position = camera.transform.position; Quaternion rotation = camera.transform.rotation;
        var target = camera.targetTexture;
        var data = camera.GetUniversalAdditionalCameraData();
        int renderer = new SerializedObject(data).FindProperty("m_RendererIndex").intValue;
        var rt = new RenderTexture(1280, 720, 24); rt.Create();
        GameObject cube = null; Material material = null;
        Application.LogCallback listener = (message, stack, type) => { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message); };
        Application.logMessageReceived += listener;
        try
        {
            // Compare geometry visibility without depending on asynchronous VT
            // material-page warm-up at near-ground camera positions.
            gpu.gpuRendering = true; gpu.collectStatistics = false; gpu.frustumCulling = true; gpu.debugLOD = true;
            camera.targetTexture = rt; data.SetRenderer(0);
            await CompareHiZ(gpu, camera, rt, "hiz", report, false);
            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Temporary Hi-Z validation occluder"; cube.hideFlags = HideFlags.HideAndDontSave;
            cube.transform.SetPositionAndRotation(position + camera.transform.forward * 130 + camera.transform.right * 40, rotation);
            cube.transform.localScale = new Vector3(140, 100, 8);
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")); material.SetColor("_BaseColor", Color.gray);
            cube.GetComponent<Renderer>().sharedMaterial = material;
            cube.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            await CompareHiZ(gpu, camera, rt, "occluder-hiz", report, true);
            data.SetRenderer(1);
            await CompareHiZ(gpu, camera, rt, "deferred-occluder-hiz", report, true);
            Object.DestroyImmediate(cube); cube = null;
            data.SetRenderer(0); gpu.hiZOcclusion = false;
            await Frames(16);
            gpu.hiZOcclusion = true;
            int historicalFrames = 0;
            for (int i = 0; i < 120; i++)
            {
                camera.transform.SetPositionAndRotation(position + new Vector3(i * 2, 0, i), rotation * Quaternion.Euler(0, i * .3f, 0));
                // Observe the render of the changed pose, not the preceding camera frame.
                await Frames(1);
                Require(gpu.IsReady, gpu.Status);
                if (i > 0 && gpu.LastUsedHiZ) historicalFrames++;
            }
            report.AppendLine($"PASS 120 movement frames: renderer stayed ready; historical Hi-Z observations={historicalFrames}.");
            Require(historicalFrames == 0, "Historical depth was used while the camera moved.");
            Require(errors.Count == 0, "Runtime errors: " + string.Join("; ", errors));
            report.AppendLine("PASS no runtime errors; settings/camera/target restored; temporary occluder removed.");
        }
        catch (Exception error) { report.AppendLine("FAIL " + error); throw; }
        finally
        {
            Application.logMessageReceived -= listener;
            if (cube != null) Object.DestroyImmediate(cube);
            if (material != null) Object.DestroyImmediate(material);
            if (gpu != null) JsonUtility.FromJsonOverwrite(settings, gpu);
            if (camera != null) { camera.transform.SetPositionAndRotation(position, rotation); camera.targetTexture = target; data.SetRenderer(renderer); }
            Object.DestroyImmediate(rt);
            File.WriteAllText(Path.Combine(Output, "play-validation.txt"), report.ToString());
            Debug.Log(report.ToString()); running = false;
        }
    }
}
