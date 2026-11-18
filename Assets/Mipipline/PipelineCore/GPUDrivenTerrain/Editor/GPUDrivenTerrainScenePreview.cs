using System.Collections.Generic;
using MPipeline;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Refresh the source camera's depth when the Game tab is not repainting.</summary>
[InitializeOnLoad]
public static class GPUDrivenTerrainScenePreview
{
    static readonly HashSet<Camera> Refreshed = new HashSet<Camera>();
    static double nextUpdate;
    // Explicit validation owns its rendering order while this is set.
    public static bool Suspend { get; set; }

    static GPUDrivenTerrainScenePreview() => EditorApplication.update += Update;

    static void Update()
    {
        if (Suspend || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || EditorApplication.isUpdating || SceneView.lastActiveSceneView == null
            || SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D11
            || EditorApplication.timeSinceStartup < nextUpdate) return;
        nextUpdate = EditorApplication.timeSinceStartup + 0.1;
        Refreshed.Clear();
        bool repaint = false;
        foreach (GPUTerrain terrain in Object.FindObjectsOfType<GPUTerrain>())
        {
            if (!terrain.isActiveAndEnabled || !terrain.gpuRendering || !terrain.hiZOcclusion) continue;
            Camera observer = SceneView.lastActiveSceneView.camera;
            Camera source = terrain.GetCullingCamera(observer);
            if (source == null) continue;
            if (source != observer && Refreshed.Add(source) && terrain.NeedsHiZDepthRefresh(source))
            {
                // Called from editor update, never from a render callback. Preserve
                // target, projection and pose: the published depth must describe
                // the real source camera, including its actual Game-view aspect.
                source.Render();
            }
            repaint = true;
        }
        if (repaint) SceneView.RepaintAll();
    }
}
