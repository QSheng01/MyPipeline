using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MPipeline;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

/// <summary>Checks real source-camera pixels and observer GPU lists in Edit mode.</summary>
public static class GPUDrivenTerrainEditHiZValidation
{
    const string Output = "GPUDrivenTerrainValidation";
    const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    static object Buffers(GPUTerrain gpu, Camera camera)
    {
        object builder = typeof(GPUTerrain).GetField("builder", Flags).GetValue(gpu);
        return ((IDictionary)builder.GetType().GetField("cameras", Flags).GetValue(builder))[camera];
    }
    static ComputeBuffer Buffer(object buffers, string name) => (ComputeBuffer)buffers.GetType().GetField(name, Flags).GetValue(buffers);

    static uint[] Counts(GPUTerrain gpu, Camera camera)
    {
        object buffers = Buffers(gpu, camera);
        var stats = new uint[4]; Buffer(buffers, "Stats").GetData(stats);
        var args = new uint[5]; Buffer(buffers, "Args").GetData(args);
        Require(stats[0] == stats[1] + stats[3] && stats[3] == args[1] && stats[2] >= stats[0], "GPU counters do not match the indirect draw.");
        return stats;
    }

    static string[] VisibleSet(GPUTerrain gpu, Camera camera)
    {
        object buffers = Buffers(gpu, camera);
        int count = (int)Counts(gpu, camera)[3];
        var patches = new uint[count * 4];
        if (count != 0) Buffer(buffers, "Visible").GetData(patches, 0, 0, patches.Length);
        return Enumerable.Range(0, count).Select(i => string.Join(",", patches.Skip(i * 4).Take(4))).OrderBy(x => x, StringComparer.Ordinal).ToArray();
    }

    static Color32[] Capture(RenderTexture target, string name)
    {
        var old = RenderTexture.active;
        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        try
        {
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
            File.WriteAllBytes(Path.Combine(Output, name + ".png"), image.EncodeToPNG());
            return image.GetPixels32();
        }
        finally { RenderTexture.active = old; Object.DestroyImmediate(image); }
    }

    static void Compare(GPUTerrain gpu, Camera source, Camera observer, string label, bool requireReduction, StringBuilder report)
    {
        gpu.hiZOcclusion = false;
        source.Render(); observer.Render(); source.Render();
        var baseline = Capture(source.targetTexture, label + "-game-off");
        var before = Counts(gpu, source);
        observer.Render(); Capture(observer.targetTexture, label + "-scene-off");
        gpu.hiZOcclusion = true;
        for (int i = 0; i < 10; i++)
        {
            source.Render();
            gpu.TryGetDiagnostics(source, out var diagnostics);
            if (diagnostics.HiZActive) break;
        }
        gpu.TryGetDiagnostics(source, out var sourceDiagnostics);
        Require(sourceDiagnostics.HiZActive, "Source Hi-Z inactive: " + sourceDiagnostics.HiZStatus);
        var actual = Capture(source.targetTexture, label + "-game-on");
        int changed = 0;
        for (int i = 0; i < baseline.Length; i++) if (!baseline[i].Equals(actual[i])) changed++;
        Require(changed == 0, label + ": Hi-Z changed " + changed + " source-camera pixels.");
        observer.Render();
        gpu.TryGetDiagnostics(observer, out var observerDiagnostics);
        Require(observerDiagnostics.HiZActive && observerDiagnostics.SourceCamera == source.name, "Observer did not use the source camera's Hi-Z.");
        var after = Counts(gpu, observer);
        Require(after[0] == before[0] && (!requireReduction || after[1] > 0), "Unexpected frustum/occlusion counts.");
        Require(VisibleSet(gpu, source).SequenceEqual(VisibleSet(gpu, observer)), "Game and Scene visible patch sets differ for the same depth and pose.");
        Capture(observer.targetTexture, label + "-scene-on");
        report.AppendLine($"PASS {label}: frustum {after[0]}, Hi-Z rejected {after[1]}, visible {after[3]}; source pixels identical; Scene uses the matching source-camera visible set.");
    }

    [MenuItem("Tools/GPU Terrain/Validate Hi-Z in Scene View (Edit Mode)")]
    public static void Run()
    {
        Require(!EditorApplication.isPlaying, "Run this validation in Edit mode.");
        var gpu = Object.FindObjectOfType<GPUTerrain>(); var source = Camera.main;
        var observer = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.camera : null;
        Require(gpu != null && source != null && observer != null, "GPU Terrain, Main Camera and Scene view are required.");
        Directory.CreateDirectory(Output);
        bool suspended = GPUDrivenTerrainScenePreview.Suspend;
        GPUDrivenTerrainScenePreview.Suspend = true;
        string settings = JsonUtility.ToJson(gpu);
        Vector3 position = source.transform.position;
        Quaternion rotation = source.transform.rotation, observerRotation = observer.transform.rotation;
        var sourceTarget = source.targetTexture; var observerTarget = observer.targetTexture;
        var additional = source.GetUniversalAdditionalCameraData();
        int renderer = new SerializedObject(additional).FindProperty("m_RendererIndex").intValue;
        var gameRT = new RenderTexture(1280, 720, 24); var sceneRT = new RenderTexture(1174, 600, 24);
        var report = new StringBuilder();
        GameObject cube = null; Material material = null;
        try
        {
            gpu.gpuRendering = true; gpu.frustumCulling = true; gpu.sceneViewUsesGameCamera = true; gpu.sceneViewCullingCamera = source;
            // Flat LOD colors make pixel equality independent of AVT page warm-up.
            gpu.debugLOD = true; gpu.collectStatistics = false;
            source.targetTexture = gameRT; observer.targetTexture = sceneRT; additional.SetRenderer(0);
            Compare(gpu, source, observer, "edit-natural-hiz", false, report);

            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Temporary edit Hi-Z occluder"; cube.hideFlags = HideFlags.HideAndDontSave;
            cube.transform.SetPositionAndRotation(position + source.transform.forward * 8 + source.transform.right * 3, rotation);
            cube.transform.localScale = new Vector3(6, 6, .2f);
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit")); material.SetColor("_BaseColor", Color.gray);
            cube.GetComponent<Renderer>().sharedMaterial = material;
            cube.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Compare(gpu, source, observer, "edit-occluder-hiz", true, report);
            additional.SetRenderer(1);
            Compare(gpu, source, observer, "edit-deferred-hiz", true, report);

            string[] previous = VisibleSet(gpu, observer);
            observer.transform.rotation = Quaternion.Euler(-90, 0, 0);
            observer.Render(); observer.Render();
            Require(VisibleSet(gpu, observer).SequenceEqual(previous), "Observer's sky depth contaminated the source-camera occlusion test.");
            report.AppendLine("PASS observer depth isolation: turning Scene toward the sky leaves source-camera Hi-Z selection unchanged.");

            source.transform.position = position + Vector3.right * 50;
            observer.Render();
            gpu.TryGetDiagnostics(observer, out var moved);
            Require(!moved.HiZActive && Counts(gpu, observer)[1] == 0, "Changed source pose reused stale depth.");
            source.Render(); observer.Render();
            gpu.TryGetDiagnostics(observer, out var refreshed);
            Require(refreshed.HiZActive, "Observer did not resume Hi-Z after source depth refresh.");
            report.AppendLine("PASS source movement invalidates historical depth and fresh source rendering re-enables Scene Hi-Z.");
            report.AppendLine("PASS ALL");
        }
        catch (Exception error) { report.AppendLine("FAIL: " + error); throw; }
        finally
        {
            if (cube != null) Object.DestroyImmediate(cube);
            if (material != null) Object.DestroyImmediate(material);
            JsonUtility.FromJsonOverwrite(settings, gpu);
            source.transform.SetPositionAndRotation(position, rotation); observer.transform.rotation = observerRotation;
            source.targetTexture = sourceTarget; observer.targetTexture = observerTarget; additional.SetRenderer(renderer);
            Object.DestroyImmediate(gameRT); Object.DestroyImmediate(sceneRT);
            GPUDrivenTerrainScenePreview.Suspend = suspended;
            SceneView.RepaintAll();
            File.WriteAllText(Path.Combine(Output, "edit-hiz-validation.txt"), report.ToString());
            Debug.Log(report.ToString());
        }
    }
}
