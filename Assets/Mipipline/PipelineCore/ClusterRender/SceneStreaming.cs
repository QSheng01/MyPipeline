using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MPipeline
{
    /// <summary>
    /// Compatibility wrapper for the old streaming component.
    /// URP references a baked asset directly and the renderer feature owns GPU buffers.
    /// </summary>
    [RequireComponent(typeof(ClusterRuntimeRenderer))]
    public sealed class SceneStreaming : MonoBehaviour
    {
        public enum State
        {
            Unloaded,
            Loaded,
            Loading
        }

        public ClusterGeometryAsset clusterAsset;
        public Material material;
        public bool loadOnEnable = true;
        public State state { get; private set; } = State.Unloaded;

        public int clusterCount => clusterAsset == null ? 0 : clusterAsset.ClusterCount;

        private ClusterRuntimeRenderer runtimeRenderer;

        private void Awake()
        {
            runtimeRenderer = GetComponent<ClusterRuntimeRenderer>();
            SyncRenderer();
        }

        private void OnEnable()
        {
            if (runtimeRenderer == null)
                runtimeRenderer = GetComponent<ClusterRuntimeRenderer>();

            SyncRenderer();
            if (loadOnEnable)
                Load();
        }

        private void OnValidate()
        {
            if (runtimeRenderer == null)
                runtimeRenderer = GetComponent<ClusterRuntimeRenderer>();
            SyncRenderer();
        }

        private void OnDisable()
        {
            if (runtimeRenderer != null)
                runtimeRenderer.enabled = false;
        }

        private void SyncRenderer()
        {
            if (runtimeRenderer == null)
                return;

            runtimeRenderer.geometry = clusterAsset;
            runtimeRenderer.materialOverride = material;
            runtimeRenderer.enabled = loadOnEnable && isActiveAndEnabled && clusterAsset != null;
        }

        public void Load()
        {
            state = clusterAsset != null ? State.Loaded : State.Unloaded;
            if (runtimeRenderer != null)
                runtimeRenderer.enabled = state == State.Loaded;
        }

        public void Unload()
        {
            state = State.Unloaded;
            if (runtimeRenderer != null)
                runtimeRenderer.enabled = false;
        }

        public IEnumerator Generate()
        {
            Load();
            yield return null;
        }

        public IEnumerator Delete()
        {
            Unload();
            yield return null;
        }

        public static IEnumerator Separate(SceneStreaming parent, List<SceneStreaming> children)
        {
            if (children != null)
            {
                foreach (SceneStreaming child in children)
                    if (child != null)
                        child.Load();
            }

            if (parent != null)
                parent.Unload();

            yield return null;
        }

        public static IEnumerator Combine(SceneStreaming parent, List<SceneStreaming> allScenes)
        {
            if (allScenes != null)
            {
                foreach (SceneStreaming scene in allScenes)
                    if (scene != null)
                        scene.Unload();
            }

            if (parent != null)
                parent.Load();

            yield return null;
        }
    }
}
