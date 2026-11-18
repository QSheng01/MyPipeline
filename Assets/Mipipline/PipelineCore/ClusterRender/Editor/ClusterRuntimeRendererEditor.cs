using UnityEditor;
using UnityEngine;

namespace MPipeline
{
    [CustomEditor(typeof(ClusterRuntimeRenderer))]
    public sealed class ClusterRuntimeRendererEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ClusterRuntimeRenderer renderer = (ClusterRuntimeRenderer)target;
            ClusterGeometryAsset geometry = renderer.geometry;
            int total = geometry == null ? 0 : geometry.ClusterCount;
            int visible = renderer.LastVisibleClusterCount;
            float visibleRatio = total <= 0 ? 0f : visible / (float)total;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("GPU Culling Debug", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle("Last Draw Used GPU Culling", renderer.LastDrawUsedGpuCulling);
                EditorGUILayout.Toggle("Last Draw Used Hi-Z Occlusion", renderer.LastDrawUsedHiZOcclusionCulling);
                EditorGUILayout.Toggle("Readback Pending", renderer.CullingStatsPending);
                EditorGUILayout.IntField("Visible Clusters", visible);
                EditorGUILayout.IntField("Total Clusters", total);
                EditorGUILayout.Slider("Visible Ratio", visibleRatio, 0f, 1f);
            }

            if (renderer.debugCullingStats && !SystemInfo.supportsAsyncGPUReadback)
            {
                EditorGUILayout.HelpBox(
                    "This platform does not support AsyncGPUReadback, so the visible cluster count cannot be read back.",
                    MessageType.Warning);
            }

            if (renderer.useGpuFrustumCulling && !renderer.LastDrawUsedGpuCulling)
            {
                EditorGUILayout.HelpBox(
                    "GPU culling was requested, but the last draw did not use it. Check that ClusterRendererFeature is on the active URP Renderer and that ClusterCulling.compute is assigned or available in Resources.",
                    MessageType.Info);
            }

            if (renderer.useHiZOcclusionCulling && !renderer.LastDrawUsedHiZOcclusionCulling)
            {
                EditorGUILayout.HelpBox(
                    "Hi-Z occlusion was requested, but the last draw did not use it. It requires GPU frustum culling, a valid URP depth texture, and the same camera for rendering and culling.",
                    MessageType.Info);
            }

            if (renderer.debugCullingStats && Application.isPlaying)
                Repaint();
        }
    }
}
