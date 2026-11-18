using MPipeline;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GPUTerrain)), CanEditMultipleObjects]
public sealed class GPUDrivenTerrainEditor : Editor
{
    public override bool RequiresConstantRepaint() => target != null && ((GPUTerrain)target).collectStatistics;

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck()) SceneView.RepaintAll();

        var terrain = (GPUTerrain)target;
        if (targets.Length == 1)
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("Bake Height + Normal From Current TerrainData"))
                GPUTerrainBaker.Bake(terrain);
            if (terrain.heightSource == GPUTerrain.HeightSource.BakedTextures && !terrain.BakedTexturesValid)
                EditorGUILayout.HelpBox("Baked textures must match the TerrainData heightmap resolution (RFloat height and RGHalf normal). Re-bake before using this mode.", MessageType.Error);
            else if (terrain.heightSource == GPUTerrain.HeightSource.BakedTextures && terrain.BakedHeightStale)
                EditorGUILayout.HelpBox("TerrainData heights changed after baking. Rendering uses the current TerrainData until you re-bake height and normal textures.", MessageType.Warning);
            else if (terrain.heightSource == GPUTerrain.HeightSource.BakedTextures)
                EditorGUILayout.HelpBox("Rendering from baked textures. Re-bake after editing TerrainData heights; the collider still follows TerrainData.", MessageType.Info);
        }
        if (terrain.sceneViewUsesGameCamera)
        {
            Camera camera = terrain.sceneViewCullingCamera != null ? terrain.sceneViewCullingCamera : Camera.main;
            EditorGUILayout.HelpBox(camera != null
                ? "Scene 视图当前使用 " + camera.name + " 的 LOD、视锥和 Hi-Z 深度。编辑模式会自动更新源摄像机深度；移动 Scene 摄像机可观察被剔除的区域。青色线框为剔除视锥。"
                : "未找到 Main Camera 或指定摄像机，当前回退到 Scene 摄像机。",
                camera != null ? MessageType.Info : MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox("Scene 视图当前按自己的摄像机选择 LOD 和裁剪，因此仍可看到主摄像机视野外的地形。开启 Scene View Uses Game Camera 可观察主摄像机的剔除结果。", MessageType.Info);
        }
        DrawDiagnostics(terrain, Camera.main, "Game");
        if (SceneView.lastActiveSceneView != null)
            DrawDiagnostics(terrain, SceneView.lastActiveSceneView.camera, "Scene");
        if (!terrain.collectStatistics)
            EditorGUILayout.HelpBox("Collect Statistics 已关闭；剔除继续执行，数量停止更新。", MessageType.Info);
    }

    static void DrawDiagnostics(GPUTerrain terrain, Camera camera, string label)
    {
        if (!terrain.TryGetDiagnostics(camera, out var stats)) return;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(label + " / " + stats.SourceCamera, EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Hi-Z", stats.HiZStatus);
        if (stats.HasStatistics)
        {
            EditorGUILayout.LabelField("视锥内 → Hi-Z 剔除 → 最终绘制", $"{stats.Frustum} → {stats.Occluded} → {stats.Visible}");
            EditorGUILayout.LabelField("四叉树总地块", stats.Selected.ToString());
        }
        else EditorGUILayout.LabelField("统计", "等待 GPU 异步回读");
    }
}
