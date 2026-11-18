using System;
using System.IO;
using System.Text;
using MPipeline;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class GPUTerrainBakeValidation
{
    const string Output = "GPUDrivenTerrainValidation";
    const string ShaderPath = "Assets/Mipipline/PipelineCore/GPUDrivenTerrain/Editor/GPUTerrainValidation.compute";

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    static float[] ReadHeight(Texture texture)
    {
        var read = AsyncGPUReadback.Request(texture, 0, TextureFormat.RFloat);
        read.WaitForCompletion();
        Require(!read.hasError, "Height readback failed.");
        return read.GetData<float>().ToArray();
    }

    static Vector4[] ReadShaderNormals(GPUTerrain component, ComputeShader shader)
    {
        const int axis = 65;
        var values = new Vector4[axis * axis];
        using (var buffer = new ComputeBuffer(values.Length, 16))
        {
            int kernel = shader.FindKernel("NormalSamples");
            var data = component.GetComponent<Terrain>().terrainData;
            shader.SetBuffer(kernel, "Normals", buffer);
            shader.SetTexture(kernel, "_GPUTerrainHeight", component.HeightTexture);
            shader.SetTexture(kernel, "_GPUTerrainNormal", component.UsingBakedTextures
                ? (Texture)component.bakedNormal : Texture2D.normalTexture);
            shader.SetVector("_GPUTerrainSize", data.size);
            shader.SetVector("_GPUTerrainInfo", new Vector4(component.PatchGridSize,
                data.heightmapResolution, 0, 16));
            shader.SetInt("_GPUTerrainUseBakedNormal", component.UsingBakedTextures ? 1 : 0);
            shader.Dispatch(kernel, (axis + 7) / 8, (axis + 7) / 8, 1);
            buffer.GetData(values);
        }
        return values;
    }

    static uint SelectedPatchCount(GPUTerrain component, Camera camera)
    {
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var builder = typeof(GPUTerrain).GetField("builder", flags).GetValue(component);
        var dictionary = (System.Collections.IDictionary)builder.GetType().GetField("cameras", flags).GetValue(builder);
        var cameraBuffers = dictionary[camera];
        var args = new uint[5];
        ((ComputeBuffer)cameraBuffers.GetType().GetField("ShadowArgs", flags).GetValue(cameraBuffers)).GetData(args);
        return args[1];
    }

    [MenuItem("Tools/GPU Terrain/Validate Baked Height And Normals")]
    public static void Run()
    {
        Require(!Application.isPlaying, "Run in Edit mode.");
        var component = Object.FindObjectOfType<GPUTerrain>();
        var camera = Camera.main;
        Require(component != null && camera != null && component.BakedTexturesValid && !component.BakedHeightStale,
            "Bake the current TerrainData height and normals first.");
        var shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(ShaderPath);
        Require(shader != null, "Normal validation shader missing.");
        var report = new StringBuilder();
        GPUTerrain.HeightSource previous = component.heightSource;
        bool hiZ = component.hiZOcclusion;
        try
        {
            component.hiZOcclusion = false;
            component.heightSource = GPUTerrain.HeightSource.RuntimeTerrainData;
            camera.Render();
            Require(!component.UsingBakedTextures, "Runtime mode did not select TerrainData.");
            float[] runtimeHeight = ReadHeight(component.HeightTexture);
            Vector4[] runtimeNormals = ReadShaderNormals(component, shader);
            uint runtimePatches = SelectedPatchCount(component, camera);

            component.heightSource = GPUTerrain.HeightSource.BakedTextures;
            camera.Render();
            Require(component.UsingBakedTextures, "Baked mode did not select the saved assets.");
            float[] bakedHeight = ReadHeight(component.HeightTexture);
            Vector4[] bakedNormals = ReadShaderNormals(component, shader);
            int resolution = component.GetComponent<Terrain>().terrainData.heightmapResolution;
            Require(runtimeHeight.Length == bakedHeight.Length && bakedHeight.Length == resolution * resolution,
                "Height resolution changed between modes.");
            float maxHeight = 0;
            for (int i = 0; i < bakedHeight.Length; i++)
                maxHeight = Mathf.Max(maxHeight, Mathf.Abs(runtimeHeight[i] - bakedHeight[i]));
            Require(maxHeight <= 0.000001f, "Baked height differs from runtime height: " + maxHeight);
            report.AppendLine($"PASS height: {resolution}² pixels, max normalized difference {maxHeight:R}; baked RFloat asset equals runtime height.");

            float maxAngle = 0;
            for (int i = 0; i < runtimeNormals.Length; i++)
            {
                Vector3 a = runtimeNormals[i], b = bakedNormals[i];
                maxAngle = Mathf.Max(maxAngle, Vector3.Angle(a, b));
            }
            Require(maxAngle < 0.75f, "Baked normals diverge from shader generated normals: " + maxAngle);
            report.AppendLine($"PASS normal: {runtimeNormals.Length} shader samples, max angular difference {maxAngle:F5} degrees (RGHalf XZ with reconstructed Y).");

            uint bakedPatches = SelectedPatchCount(component, camera);
            Require(runtimePatches == bakedPatches, "Height source changed quadtree patch count.");
            report.AppendLine($"PASS quadtree: {bakedPatches} selected patches with either height source.");
            report.AppendLine("PASS ALL");
        }
        catch (Exception error) { report.AppendLine("FAIL " + error); throw; }
        finally
        {
            component.heightSource = previous;
            component.hiZOcclusion = hiZ;
            camera.Render();
            Directory.CreateDirectory(Output);
            File.WriteAllText(Path.Combine(Output, "bake-validation.txt"), report.ToString());
            Debug.Log(report.ToString());
        }
    }
}
