using System;
using System.IO;
using MPipeline;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>One-time GPU bake of the current TerrainData into persistent Unity texture assets.</summary>
public static class GPUTerrainBaker
{
    [MenuItem("Tools/GPU Terrain/Bake Height And Normals")]
    public static void BakeCurrentScene()
    {
        var selected = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<GPUTerrain>() : null;
        GPUTerrain terrain = selected != null ? selected : Object.FindObjectOfType<GPUTerrain>();
        if (terrain == null) throw new InvalidOperationException("The current scene has no GPUTerrain component.");
        Bake(terrain);
    }

    public static void Bake(GPUTerrain component)
    {
        if (Application.isPlaying) throw new InvalidOperationException("Bake terrain textures in Edit mode.");
        if (component == null) throw new ArgumentNullException(nameof(component));
        var terrain = component.GetComponent<Terrain>();
        if (terrain == null || terrain.terrainData == null) throw new InvalidOperationException("TerrainData is missing.");
        ComputeShader shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(
            "Assets/Mipipline/PipelineCore/GPUDrivenTerrain/Resources/GPUTerrainBuild.compute");
        if (shader == null || !SystemInfo.supportsComputeShaders || !SystemInfo.supportsAsyncGPUReadback)
            throw new InvalidOperationException("Baking requires compute shaders and GPU readback.");

        int resolution = terrain.terrainData.heightmapResolution;
        int copy = shader.FindKernel("CopyHeight"), normal = shader.FindKernel("BakeNormal");
        RenderTexture heightRT = null, normalRT = null;
        try
        {
            heightRT = new RenderTexture(resolution, resolution, 0, GraphicsFormat.R32_SFloat)
            { name = "Terrain Height Bake", enableRandomWrite = true };
            normalRT = new RenderTexture(resolution, resolution, 0, GraphicsFormat.R16G16_SFloat)
            { name = "Terrain Normal Bake", enableRandomWrite = true };
            if (!heightRT.Create() || !normalRT.Create()) throw new InvalidOperationException("Unable to allocate bake targets.");
            using (var command = new CommandBuffer { name = "GPU Terrain Bake Height And Normals" })
            {
                command.SetComputeIntParam(shader, "_Resolution", resolution);
                command.SetComputeVectorParam(shader, "_Size", terrain.terrainData.size);
                command.SetComputeTextureParam(shader, copy, "_SourceHeight", terrain.terrainData.heightmapTexture);
                command.SetComputeTextureParam(shader, copy, "_HeightOutput", heightRT);
                command.DispatchCompute(shader, copy, (resolution + 7) / 8, (resolution + 7) / 8, 1);
                command.SetComputeTextureParam(shader, normal, "_Height", heightRT);
                command.SetComputeTextureParam(shader, normal, "_NormalOutput", normalRT);
                command.DispatchCompute(shader, normal, (resolution + 7) / 8, (resolution + 7) / 8, 1);
                Graphics.ExecuteCommandBuffer(command);
            }

            var heightRead = AsyncGPUReadback.Request(heightRT, 0, TextureFormat.RFloat);
            var normalRead = AsyncGPUReadback.Request(normalRT, 0, TextureFormat.RGHalf);
            heightRead.WaitForCompletion(); normalRead.WaitForCompletion();
            if (heightRead.hasError || normalRead.hasError) throw new InvalidOperationException("GPU bake readback failed.");

            string terrainPath = AssetDatabase.GetAssetPath(terrain.terrainData);
            if (string.IsNullOrEmpty(terrainPath)) throw new InvalidOperationException("Save the TerrainData asset before baking.");
            string parent = Path.GetDirectoryName(terrainPath).Replace('\\', '/');
            string folder = parent + "/BakedGPUHeight";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(parent, "BakedGPUHeight");
            string name = Path.GetFileNameWithoutExtension(terrainPath);
            string heightPath = folder + "/" + name + "_Height.asset";
            string normalPath = folder + "/" + name + "_Normal.asset";
            Texture2D bakedHeight = GetTexture(heightPath, resolution, TextureFormat.RFloat);
            Texture2D bakedNormal = GetTexture(normalPath, resolution, TextureFormat.RGHalf);
            bakedHeight.SetPixelData(heightRead.GetData<float>(), 0);
            bakedNormal.SetPixelData(normalRead.GetData<ushort>(), 0);
            bakedHeight.Apply(false, false);
            bakedNormal.Apply(false, false);
            bakedHeight.wrapMode = bakedNormal.wrapMode = TextureWrapMode.Clamp;
            bakedHeight.filterMode = bakedNormal.filterMode = FilterMode.Bilinear;
            EditorUtility.SetDirty(bakedHeight);
            EditorUtility.SetDirty(bakedNormal);
            Undo.RecordObject(component, "Use baked terrain height and normal textures");
            component.bakedHeight = bakedHeight;
            component.bakedNormal = bakedNormal;
            component.heightSource = GPUTerrain.HeightSource.BakedTextures;
            component.MarkBakedHeightCurrent();
            EditorUtility.SetDirty(component);
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
            AssetDatabase.SaveAssets();
            SceneView.RepaintAll();
            Debug.Log($"GPU terrain baked: {resolution}² RFloat height → {heightPath}; RGHalf XZ normal → {normalPath}. Existing asset GUIDs are reused on rebake.", component);
        }
        finally
        {
            if (heightRT != null) Object.DestroyImmediate(heightRT);
            if (normalRT != null) Object.DestroyImmediate(normalRT);
        }
    }

    private static Texture2D GetTexture(string path, int resolution, TextureFormat format)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null)
        {
            if (!existing.isReadable) throw new InvalidOperationException($"Bake asset is not readable: {path}.");
            if ((existing.width != resolution || existing.height != resolution || existing.format != format)
                && !existing.Reinitialize(resolution, resolution, format, false))
                throw new InvalidOperationException($"Cannot reinitialize existing bake asset: {path}.");
            return existing;
        }
        if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            throw new InvalidOperationException("Bake asset path is occupied: " + path);
        var texture = new Texture2D(resolution, resolution, format, false, true)
        { name = Path.GetFileNameWithoutExtension(path), filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        AssetDatabase.CreateAsset(texture, path);
        return texture;
    }
}
