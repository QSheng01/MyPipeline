using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MPipeline
{
    /// <summary>Scene-only visualization of the same view-space buckets used by ClusterLightCulling.</summary>
    [InitializeOnLoad]
    internal static class ClusterLightGridSceneView
    {
        static ClusterLightGridSceneView()
        {
            SceneView.duringSceneGui += Draw;
        }

        private static void Draw(SceneView sceneView)
        {
            if (sceneView == null || sceneView.camera == null || Event.current.type != EventType.Repaint)
                return;

            ClusterRendererFeature feature = GetActiveFeature();
            if (feature == null || !feature.ShowClusterLightGridInSceneView)
                return;

            Camera camera = FindGameCamera();
            if (camera == null || camera.orthographic)
                return; // The lighting grid uses perspective projection.

            const int xCount = ClusterLightCulling.GridX;
            const int yCount = ClusterLightCulling.GridY;
            const int zCount = ClusterLightCulling.GridZ;
            int slice = feature.ClusterLightGridDepthSlice;
            float near = Mathf.Max(camera.nearClipPlane, 0.0001f);
            float far = Mathf.Max(camera.farClipPlane, near + 0.0001f);
            float ratio = far / near;
            Matrix4x4 projection = camera.projectionMatrix;
            float inverseX = Mathf.Abs(projection.m00) > 0.0001f ? 1f / Mathf.Abs(projection.m00) : 1f;
            float inverseY = Mathf.Abs(projection.m11) > 0.0001f ? 1f / Mathf.Abs(projection.m11) : 1f;

            Matrix4x4 oldMatrix = Handles.matrix;
            Color oldColor = Handles.color;
            Handles.matrix = camera.cameraToWorldMatrix;
            try
            {
                int firstSlice = feature.ShowAllClusterLightGridDepthSlices ? 0 : slice;
                int lastSlice = feature.ShowAllClusterLightGridDepthSlices ? zCount - 1 : slice;
                for (int z = firstSlice; z <= lastSlice; ++z)
                {
                    float depth = near * Mathf.Pow(ratio, z / (float)zCount);
                    bool selected = z == slice;
                    Handles.color = selected
                        ? new Color(1f, 0.75f, 0.1f, 0.95f)
                        : new Color(0.1f, 0.65f, 1f, feature.ShowAllClusterLightGridDepthSlices ? 0.3f : 0.75f);
                    for (int x = 0; x <= xCount; ++x)
                        Handles.DrawLine(Corner(x, 0, depth, inverseX, inverseY),
                            Corner(x, yCount, depth, inverseX, inverseY));
                    for (int y = 0; y <= yCount; ++y)
                        Handles.DrawLine(Corner(0, y, depth, inverseX, inverseY),
                            Corner(xCount, y, depth, inverseX, inverseY));
                }

                // Draw the Z-direction cell edges.  A complete grid has one edge
                // for every XY lattice point between every adjacent depth slice.
                if (feature.ShowAllClusterLightGridDepthSlices)
                {
                    Handles.color = new Color(0.15f, 0.85f, 1f, 0.22f);
                    for (int z = 0; z < zCount; ++z)
                    {
                        float depth0 = near * Mathf.Pow(ratio, z / (float)zCount);
                        float depth1 = near * Mathf.Pow(ratio, (z + 1f) / zCount);
                        for (int y = 0; y <= yCount; ++y)
                            for (int x = 0; x <= xCount; ++x)
                                Handles.DrawLine(Corner(x, y, depth0, inverseX, inverseY),
                                    Corner(x, y, depth1, inverseX, inverseY));
                    }
                }
            }
            finally
            {
                Handles.matrix = oldMatrix;
                Handles.color = oldColor;
            }
        }

        private static Vector3 Corner(int x, int y, float depth, float inverseX, float inverseY)
        {
            return new Vector3(
                (x / (float)ClusterLightCulling.GridX * 2f - 1f) * depth * inverseX,
                (y / (float)ClusterLightCulling.GridY * 2f - 1f) * depth * inverseY,
                -depth);
        }

        private static Camera FindGameCamera()
        {
            Camera main = Camera.main;
            if (main != null && main.isActiveAndEnabled && main.cameraType == CameraType.Game)
                return main;

            Camera[] cameras = Camera.allCameras;
            for (int i = 0; i < cameras.Length; ++i)
            {
                Camera candidate = cameras[i];
                if (candidate != null && candidate.isActiveAndEnabled
                    && candidate.cameraType == CameraType.Game)
                    return candidate;
            }
            return null;
        }

        private static ClusterRendererFeature GetActiveFeature()
        {
            UniversalRenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null)
                return null;

            SerializedObject serialized = new SerializedObject(pipeline);
            SerializedProperty rendererData = serialized.FindProperty("m_RendererDataList");
            SerializedProperty defaultIndex = serialized.FindProperty("m_DefaultRendererIndex");
            if (rendererData == null || defaultIndex == null || rendererData.arraySize == 0)
                return null;

            int index = Mathf.Clamp(defaultIndex.intValue, 0, rendererData.arraySize - 1);
            ScriptableRendererData data = rendererData.GetArrayElementAtIndex(index).objectReferenceValue as ScriptableRendererData;
            if (data == null)
                return null;
            foreach (ScriptableRendererFeature item in data.rendererFeatures)
            {
                ClusterRendererFeature feature = item as ClusterRendererFeature;
                if (feature != null && feature.isActive)
                    return feature;
            }
            return null;
        }
    }
}
