using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MPipeline
{
    /// <summary>URP bridge for procedural cluster rendering.</summary>
    public sealed class ClusterRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private ComputeShader cullingShader;
        [SerializeField] private ComputeShader clusterLightCullingShader;
        [SerializeField] private Material clusterMaterial;
        [SerializeField] private bool enableClusterLighting = true;
        [SerializeField, Tooltip("Draw one cluster-light grid depth slice in the Scene view (editor only).")]
        private bool showClusterLightGridInSceneView;
        [SerializeField, Range(0, ClusterLightCulling.GridZ - 1)]
        private int clusterLightGridDepthSlice = 16;
        [SerializeField, Tooltip("Draw all logarithmic depth boundaries instead of only the selected slice.")]
        private bool showAllClusterLightGridDepthSlices = true;
        [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

        public bool ShowClusterLightGridInSceneView => showClusterLightGridInSceneView && enableClusterLighting;
        public int ClusterLightGridDepthSlice => Mathf.Clamp(clusterLightGridDepthSlice, 0, ClusterLightCulling.GridZ - 1);
        public bool ShowAllClusterLightGridDepthSlices => showAllClusterLightGridDepthSlices;

        private ClusterRenderPass pass;
        private ClusterShadowPass shadowPass;
        private bool createdMaterial;

        public override void Create()
        {
            pass?.Dispose();
            pass = null;
            if (createdMaterial && clusterMaterial != null)
            {
                if (Application.isPlaying) Destroy(clusterMaterial); else DestroyImmediate(clusterMaterial);
                clusterMaterial = null;
                createdMaterial = false;
            }
            if (cullingShader == null) cullingShader = Resources.Load<ComputeShader>("ClusterCulling");
            if (clusterLightCullingShader == null)
                clusterLightCullingShader = Resources.Load<ComputeShader>("ClusterLightCulling");
            if (clusterMaterial == null) clusterMaterial = Resources.Load<Material>("ClusterProcedural");
            if (clusterMaterial == null)
            {
                Shader shader = Shader.Find("MPipeline/Cluster/URP Lit");
                if (shader != null) { clusterMaterial = new Material(shader); createdMaterial = true; }
            }

            pass = new ClusterRenderPass(
                cullingShader,
                clusterLightCullingShader,
                clusterMaterial,
                enableClusterLighting)
            {
                // URP depth copy runs at 300 and Hi-Z builds at 301. Submit
                // cluster culling and drawing at 302 so each dependency has a
                // strict render-pass ordering even if feature order changes.
                renderPassEvent = (RenderPassEvent)Mathf.Max(
                    (int)renderPassEvent,
                    (int)RenderPassEvent.AfterRenderingOpaques + 2)
            };
            shadowPass = new ClusterShadowPass(clusterMaterial);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (pass == null) Create();
            if (renderingData.cameraData.cameraType == CameraType.Preview) return;
            renderer.EnqueuePass(shadowPass);
            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            pass?.Dispose();
            pass = null;
            shadowPass = null;
            if (createdMaterial && clusterMaterial != null)
            {
                if (Application.isPlaying) Destroy(clusterMaterial); else DestroyImmediate(clusterMaterial);
            }
            clusterMaterial = null;
            createdMaterial = false;
        }

        private sealed class ClusterShadowPass : ScriptableRenderPass
        {
            private static readonly RTHandle CurrentActive = RTHandles.Alloc(BuiltinRenderTextureType.CurrentActive);
            private readonly Material material;
            public ClusterShadowPass(Material material)
            {
                this.material = material;
                renderPassEvent = (RenderPassEvent)((int)RenderPassEvent.BeforeRenderingShadows + 1);
                ConfigureTarget(CurrentActive);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (renderingData.cameraData.renderType != CameraRenderType.Base
                    || !renderingData.shadowData.supportsMainLightShadows
                    || renderingData.shadowData.mainLightShadowCascadesCount <= 0
                    || renderingData.lightData.mainLightIndex < 0
                    || renderingData.lightData.mainLightIndex >= renderingData.lightData.visibleLights.Length)
                    return;

                int lightIndex = renderingData.lightData.mainLightIndex;
                VisibleLight light = renderingData.lightData.visibleLights[lightIndex];
                if (light.light == null || light.light.shadows == LightShadows.None) return;

                int cascades = renderingData.shadowData.mainLightShadowCascadesCount;
                int atlasWidth = renderingData.shadowData.mainLightShadowmapWidth;
                int atlasHeight = renderingData.shadowData.mainLightShadowmapHeight;
                int resolution = ShadowUtils.GetMaxTileResolutionInAtlas(atlasWidth, atlasHeight, cascades);
                int targetHeight = cascades == 2 ? atlasHeight >> 1 : atlasHeight;
                CommandBuffer command = CommandBufferPool.Get("MPipeline Cluster Shadow Casting");
                command.SetGlobalDepthBias(1f, 2.5f);

                for (int cascade = 0; cascade < cascades; ++cascade)
                {
                    Vector4 split;
                    ShadowSliceData slice;
                    if (!ShadowUtils.ExtractDirectionalLightMatrix(ref renderingData.cullResults, ref renderingData.shadowData,
                        lightIndex, cascade, atlasWidth, targetHeight, resolution, light.light.shadowNearPlane, out split, out slice))
                        continue;
                    Vector4 bias = ShadowUtils.GetShadowBias(ref light, lightIndex, ref renderingData.shadowData, slice.projectionMatrix, resolution);
                    ShadowUtils.SetupShadowCasterConstantBuffer(command, ref light, bias);
                    command.SetViewport(new Rect(slice.offsetX, slice.offsetY, resolution, resolution));
                    command.SetViewProjectionMatrices(slice.viewMatrix, slice.projectionMatrix);
                    foreach (ClusterRuntimeRenderer renderer in ClusterRuntimeRenderer.ActiveRenderers)
                        if (renderer != null) renderer.RecordShadowDraw(command, material);
                }
                command.SetGlobalDepthBias(0f, 0f);
                context.ExecuteCommandBuffer(command);
                CommandBufferPool.Release(command);
            }
        }

        private sealed class ClusterRenderPass : ScriptableRenderPass
        {
            private readonly ComputeShader cullingShader;
            private readonly Material material;
            private readonly ClusterLightCulling clusterLights;
            private readonly bool enableClusterLighting;
            public ClusterRenderPass(
                ComputeShader cullingShader,
                ComputeShader clusterLightCullingShader,
                Material material,
                bool enableClusterLighting)
            {
                this.cullingShader = cullingShader;
                this.material = material;
                this.enableClusterLighting = enableClusterLighting;
                clusterLights = new ClusterLightCulling(clusterLightCullingShader);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                Camera camera = renderingData.cameraData.camera;
                if (camera == null) return;
                CommandBuffer command = CommandBufferPool.Get("MPipeline Cluster Rendering");
                bool hasClusterLights = enableClusterLighting && clusterLights.Prepare(
                        command,
                        camera,
                        renderingData.lightData.visibleLights,
                        renderingData.lightData.mainLightIndex);
                command.SetGlobalInt("_ClusterLightEnabled", hasClusterLights ? 1 : 0);
                command.SetGlobalInt("_ClusterLightCount", hasClusterLights ? clusterLights.LightCount : 0);
                command.SetGlobalVector("_ClusterGridParams", clusterLights.GridParams);
                if (hasClusterLights)
                {
                    command.SetGlobalBuffer("_ClusterLights", clusterLights.LightsBuffer);
                    command.SetGlobalBuffer("_ClusterLightIndices", clusterLights.IndicesBuffer);
                }
                foreach (ClusterRuntimeRenderer renderer in ClusterRuntimeRenderer.ActiveRenderers)
                {
                    if (renderer != null)
                    {
                        Camera cullingCamera = renderer.GetCullingCamera(camera);
                        ClusterHiZResources.Data hiZ;
                        bool usesDifferentCullingCamera = cullingCamera != camera;
                        bool hasHiZ = usesDifferentCullingCamera
                            ? ClusterHiZResources.TryGetLatest(cullingCamera, out hiZ)
                            : ClusterHiZResources.TryGet(cullingCamera, out hiZ);
                        renderer.RecordDraw(
                            command,
                            camera,
                            cullingShader,
                            material,
                            hasHiZ ? hiZ.Texture : null,
                            hasHiZ ? hiZ.ScreenSize : Vector4.zero,
                            hasHiZ ? hiZ.MaxMip : 0,
                            hasHiZ ? hiZ.MipOffsets : null,
                            hasHiZ ? hiZ.MipSizes : null,
                            hasHiZ ? cullingCamera : null);
                    }
                }
                context.ExecuteCommandBuffer(command);
                CommandBufferPool.Release(command);
            }

            public void Dispose()
            {
                clusterLights.Dispose();
            }
        }
    }
}
