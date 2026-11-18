using MPipeline;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GPUDrivenTerrainSetup
{
    private const string Root = "Assets/Mipipline/PipelineCore/GPUDrivenTerrain/";

    [MenuItem("Tools/GPU Terrain/Enable in Current Scene")]
    public static void EnableInCurrentScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        int count = 0;
        foreach (var root in scene.GetRootGameObjects())
        foreach (Terrain terrain in root.GetComponentsInChildren<Terrain>(true))
        {
            if (terrain.materialTemplate == null || terrain.materialTemplate.shader.name != "URP/TerrainMaterialIDVT") continue;
            var gpu = terrain.GetComponent<GPUTerrain>();
            if (gpu == null) gpu = Undo.AddComponent<GPUTerrain>(terrain.gameObject);
            Undo.RecordObject(gpu, "Enable GPU Terrain");
            var serialized = new SerializedObject(gpu);
            serialized.FindProperty("buildShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ComputeShader>(Root + "Resources/GPUTerrainBuild.compute");
            serialized.FindProperty("terrainShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(Root + "Shaders/GPUTerrainAVT.shader");
            serialized.ApplyModifiedProperties();
            gpu.gpuRendering = true;
            gpu.enabled = true;
            EditorUtility.SetDirty(gpu);
            count++;
        }
        if (count == 0) throw new System.InvalidOperationException("The current scene has no Terrain with the AVT material.");
        EditorSceneManager.MarkSceneDirty(scene);
        SceneView.RepaintAll();
        Debug.Log($"GPU Terrain enabled on {count} terrain(s) in the current scene: {scene.path}");
    }
}
