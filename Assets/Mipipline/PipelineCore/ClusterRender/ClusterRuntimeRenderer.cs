using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace MPipeline
{
    /// <summary>
    /// Runtime owner for one baked cluster asset.
    /// The URP renderer feature calls RecordDraw once per camera and per active instance.
    /// </summary>
    public sealed class ClusterRuntimeRenderer : MonoBehaviour
    {
        private static readonly List<ClusterRuntimeRenderer> activeRenderers = new List<ClusterRuntimeRenderer>(64);

        private static readonly int ClusterBufferId = Shader.PropertyToID("clusterBuffer");
        private static readonly int VerticesBufferId = Shader.PropertyToID("verticesBuffer");
        private static readonly int VisibleIndicesId = Shader.PropertyToID("visibleIndices");
        private static readonly int ArgsBufferId = Shader.PropertyToID("argsBuffer");
        private static readonly int ClusterCountId = Shader.PropertyToID("_ClusterCount");
        private static readonly int FrustumPlanesId = Shader.PropertyToID("_FrustumPlanes");
        private static readonly int LocalToWorldId = Shader.PropertyToID("_LocalToWorld");
        private static readonly int ClusterDebugModeId = Shader.PropertyToID("_ClusterDebugMode");
        private static readonly int TriangleMaterialId = Shader.PropertyToID("_TriangleMaterialBuffer");
        private static readonly int MaterialPropertiesId = Shader.PropertyToID("_MaterialBuffer");
        private static readonly int MaterialTexturesId = Shader.PropertyToID("_ClusterMaterialTextures");
        private static readonly int UseBakedMaterialsId = Shader.PropertyToID("_UseBakedMaterials");
        private static readonly int HiZDepthTextureId = Shader.PropertyToID("_HiZDepthTexture");
        private static readonly int HiZScreenSizeId = Shader.PropertyToID("_HiZScreenSize");
        private static readonly int ViewProjectionId = Shader.PropertyToID("_ViewProjection");
        private static readonly int HiZMaxMipId = Shader.PropertyToID("_HiZMaxMip");
        private static readonly int HiZMipOffsetsId = Shader.PropertyToID("_HiZMipOffsets");
        private static readonly int HiZMipSizesId = Shader.PropertyToID("_HiZMipSizes");
        private static readonly int HiZOcclusionBiasId = Shader.PropertyToID("_HiZOcclusionBias");
        private static readonly int UseHiZOcclusionId = Shader.PropertyToID("_UseHiZOcclusion");
        private static readonly int HiZReversedZId = Shader.PropertyToID("_HiZReversedZ");

        public ClusterGeometryAsset geometry;
        public Material materialOverride;
        public bool useGpuFrustumCulling = true;
        public bool drawInSceneView = true;
        [Tooltip("When rendering the Scene view, use the Game view MainCamera frustum for cluster culling.")]
        public bool useGameCameraCullingInSceneView = false;
        [Tooltip("Display a stable debug color for each cluster instead of the regular material lighting.")]
        public bool debugClusterColors = false;
        [Tooltip("Draw cluster AABBs that pass the current Scene view frustum test.")]
        public bool debugClusterBounds = false;
        [Tooltip("Also draw clusters rejected by the current Scene view frustum test in red.")]
        public bool debugShowCulledBounds = false;
        [Tooltip("Read back the indirect draw count so the Inspector can show whether GPU frustum culling is active.")]
        public bool debugCullingStats = false;
        [Tooltip("Reject clusters hidden behind opaque geometry already present in the URP camera depth texture.")]
        public bool useHiZOcclusionCulling = false;
        [Tooltip("Positive depth bias keeps Hi-Z rejection conservative and helps avoid popping from depth precision differences.")]
        [Range(0f, 0.02f)]
        public float hiZOcclusionBias = 0.001f;

        internal static List<ClusterRuntimeRenderer> ActiveRenderers => activeRenderers;
        internal static int ActiveCount => activeRenderers.Count;
        public int LastVisibleClusterCount => lastVisibleClusterCount;
        public int LastTotalClusterCount => lastTotalClusterCount;
        public bool LastDrawUsedGpuCulling => lastDrawUsedGpuCulling;
        public bool LastDrawUsedHiZOcclusionCulling => lastDrawUsedHiZOcclusionCulling;
        public bool CullingStatsPending => cullingStatsPending;
        public string LastCullingCameraName => lastCullingCameraName;
        public string LastHiZCameraName => lastHiZCameraName;
        public string LastHiZTextureName => lastHiZTextureName;

        private ComputeBuffer clusterBuffer;
        private ComputeBuffer verticesBuffer;
        private ComputeBuffer visibleIndicesBuffer;
        private ComputeBuffer argsBuffer;
        private ComputeBuffer triangleMaterialBuffer;
        private ComputeBuffer materialPropertiesBuffer;
        private Texture2DArray materialTextures;
        private Material runtimeMaterial;
        private Material sourceMaterial;
        private ClusterGeometryAsset uploadedGeometry;
        private int uploadedClusterCount;
        private ComputeShader cachedCullingShader;
        private int clearKernel = -1;
        private int cullKernel = -1;
        private int lastVisibleClusterCount;
        private int lastTotalClusterCount;
        private bool lastDrawUsedGpuCulling;
        private bool lastDrawUsedHiZOcclusionCulling;
        private bool cullingStatsPending;
        private string lastCullingCameraName = "";
        private string lastHiZCameraName = "";
        private string lastHiZTextureName = "";
        private static readonly uint[] FullDrawArgs =
        {
            ClusterGeometryAsset.VerticesPerCluster,
            0,
            0,
            0
        };
        private static readonly Vector4[] DefaultHiZMipOffsets = CreateDefaultHiZData();
        private static readonly Vector4[] DefaultHiZMipSizes = CreateDefaultHiZData();

        private void OnEnable()
        {
            if (!activeRenderers.Contains(this))
                activeRenderers.Add(this);
        }

        private void OnDisable()
        {
            activeRenderers.Remove(this);
            ReleaseBuffers();
            ReleaseMaterial();
        }

        private void OnDestroy()
        {
            activeRenderers.Remove(this);
            ReleaseBuffers();
            ReleaseMaterial();
        }

        private void OnValidate()
        {
            if (uploadedGeometry != geometry)
                ReleaseBuffers();
            if (sourceMaterial != materialOverride)
                ReleaseMaterial();
        }

        private void OnDrawGizmos()
        {
            if (!debugClusterBounds || geometry == null || geometry.Clusters == null)
                return;

            Camera camera = Camera.current;
            if (camera == null)
                return;

            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            Gizmos.matrix = transform.localToWorldMatrix;

            Cluster[] clusters = geometry.Clusters;
            for (int i = 0; i < clusters.Length; ++i)
            {
                Cluster cluster = clusters[i];
                bool visible = frustumPlanes == null || IsClusterVisible(cluster, frustumPlanes);
                if (!visible && !debugShowCulledBounds)
                    continue;

                Gizmos.color = visible
                    ? new Color(0f, 1f, 1f, 0.85f)
                    : new Color(1f, 0.15f, 0.05f, 0.35f);
                Gizmos.DrawWireCube(cluster.position, cluster.extent * 2f);
            }

            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }

        internal Camera GetCullingCamera(Camera renderCamera)
        {
            if (renderCamera == null
                || renderCamera.cameraType != CameraType.SceneView
                || !useGameCameraCullingInSceneView)
                return renderCamera;

            Camera gameCamera = Camera.main;
            return gameCamera != null && gameCamera.isActiveAndEnabled
                ? gameCamera
                : renderCamera;
        }

        private bool IsClusterVisible(Cluster cluster, Plane[] frustumPlanes)
        {
            Matrix4x4 localToWorld = transform.localToWorldMatrix;
            Vector3 center = localToWorld.MultiplyPoint3x4(cluster.position);
            Vector3 extent = new Vector3(
                Mathf.Abs(localToWorld.m00) * cluster.extent.x
                    + Mathf.Abs(localToWorld.m01) * cluster.extent.y
                    + Mathf.Abs(localToWorld.m02) * cluster.extent.z,
                Mathf.Abs(localToWorld.m10) * cluster.extent.x
                    + Mathf.Abs(localToWorld.m11) * cluster.extent.y
                    + Mathf.Abs(localToWorld.m12) * cluster.extent.z,
                Mathf.Abs(localToWorld.m20) * cluster.extent.x
                    + Mathf.Abs(localToWorld.m21) * cluster.extent.y
                    + Mathf.Abs(localToWorld.m22) * cluster.extent.z);

            for (int i = 0; i < frustumPlanes.Length; ++i)
            {
                Plane plane = frustumPlanes[i];
                float radius = Vector3.Dot(new Vector3(
                    Mathf.Abs(plane.normal.x),
                    Mathf.Abs(plane.normal.y),
                    Mathf.Abs(plane.normal.z)), extent);
                if (Vector3.Dot(plane.normal, center) + plane.distance < -radius)
                    return false;
            }

            return true;
        }

        internal bool IsRenderable(Camera camera)
        {
            if (!enabled || !gameObject.activeInHierarchy || geometry == null || geometry.ClusterCount == 0)
                return false;
            if (camera != null && camera.cameraType == CameraType.SceneView && !drawInSceneView)
                return false;
            return true;
        }

        internal bool WantsHiZOcclusion(Camera renderCamera)
        {
            return useHiZOcclusionCulling
                && useGpuFrustumCulling
                && IsRenderable(renderCamera)
                && GetCullingCamera(renderCamera) == renderCamera;
        }

        internal void RecordDraw(
            CommandBuffer command,
            Camera camera,
            ComputeShader cullingShader,
            Material fallbackMaterial,
            RenderTexture hiZTexture,
            Vector4 hiZScreenSize,
            int hiZMaxMip,
            Vector4[] hiZMipOffsets,
            Vector4[] hiZMipSizes,
            Camera hiZCamera)
        {
            if (!IsRenderable(camera))
                return;

            EnsureBuffers();
            EnsureMaterial(fallbackMaterial);
            if (runtimeMaterial == null)
                return;

            int clusterCount = geometry.ClusterCount;
            Camera cullingCamera = GetCullingCamera(camera);
            bool gpuCullingActive = useGpuFrustumCulling
                && cullingShader != null
                && SystemInfo.supportsComputeShaders
                && cullingCamera != null
                && EnsureCullingKernels(cullingShader);
            bool hiZOcclusionActive = useHiZOcclusionCulling
                && hiZTexture != null
                && hiZCamera == cullingCamera
                && gpuCullingActive;
            lastCullingCameraName = cullingCamera != null ? cullingCamera.name : "";
            lastHiZCameraName = hiZCamera != null ? hiZCamera.name : "";
            lastHiZTextureName = hiZTexture != null ? hiZTexture.name : "";
            hiZMipOffsets = hiZOcclusionActive && hiZMipOffsets != null
                ? hiZMipOffsets
                : DefaultHiZMipOffsets;
            hiZMipSizes = hiZOcclusionActive && hiZMipSizes != null
                ? hiZMipSizes
                : DefaultHiZMipSizes;
            if (gpuCullingActive)
            {
                Vector4[] planes = GetFrustumPlanes(cullingCamera);

                command.SetComputeBufferParam(cullingShader, clearKernel, ArgsBufferId, argsBuffer);
                command.DispatchCompute(cullingShader, clearKernel, 1, 1, 1);
                command.SetComputeVectorArrayParam(cullingShader, FrustumPlanesId, planes);
                command.SetComputeMatrixParam(cullingShader, LocalToWorldId, transform.localToWorldMatrix);
                command.SetComputeFloatParam(
                    cullingShader,
                    UseHiZOcclusionId,
                    hiZOcclusionActive ? 1f : 0f);
                // The Cull kernel declares the Hi-Z texture even when the
                // feature is disabled. Always bind a valid resource so Unity
                // does not submit an incomplete compute dispatch.
                command.SetComputeTextureParam(
                    cullingShader,
                    cullKernel,
                    HiZDepthTextureId,
                    hiZOcclusionActive ? hiZTexture : Texture2D.blackTexture);
                command.SetComputeVectorParam(
                    cullingShader,
                    HiZScreenSizeId,
                    hiZOcclusionActive ? hiZScreenSize : Vector4.zero);
                command.SetComputeIntParam(cullingShader, HiZMaxMipId, hiZOcclusionActive ? hiZMaxMip : 0);
                command.SetComputeVectorArrayParam(cullingShader, HiZMipOffsetsId, hiZMipOffsets);
                command.SetComputeVectorArrayParam(cullingShader, HiZMipSizesId, hiZMipSizes);
                command.SetComputeFloatParam(
                    cullingShader,
                    HiZOcclusionBiasId,
                    hiZOcclusionActive ? hiZOcclusionBias : 0f);
                command.SetComputeIntParam(
                    cullingShader,
                    HiZReversedZId,
                    SystemInfo.usesReversedZBuffer ? 1 : 0);
                if (hiZOcclusionActive)
                {
                    command.SetComputeMatrixParam(
                        cullingShader,
                        ViewProjectionId,
                        GL.GetGPUProjectionMatrix(
                            cullingCamera.projectionMatrix,
                            true) * cullingCamera.worldToCameraMatrix);
                }
                command.SetComputeIntParam(cullingShader, ClusterCountId, clusterCount);
                command.SetComputeBufferParam(cullingShader, cullKernel, ClusterBufferId, clusterBuffer);
                command.SetComputeBufferParam(cullingShader, cullKernel, VisibleIndicesId, visibleIndicesBuffer);
                command.SetComputeBufferParam(cullingShader, cullKernel, ArgsBufferId, argsBuffer);
                command.DispatchCompute(cullingShader, cullKernel, Mathf.CeilToInt(clusterCount / 64f), 1, 1);
            }
            else
            {
                FullDrawArgs[1] = (uint)clusterCount;
                argsBuffer.SetData(FullDrawArgs);
            }

            lastTotalClusterCount = clusterCount;
            lastDrawUsedGpuCulling = gpuCullingActive;
            lastDrawUsedHiZOcclusionCulling = hiZOcclusionActive;
            RequestCullingStatsIfNeeded(command, gpuCullingActive, clusterCount);

            runtimeMaterial.SetBuffer(VisibleIndicesId, visibleIndicesBuffer);
            runtimeMaterial.SetBuffer(VerticesBufferId, verticesBuffer);
            BindBakedMaterials();
            runtimeMaterial.SetFloat(ClusterDebugModeId, debugClusterColors ? 1f : 0f);
            command.DrawProceduralIndirect(
                transform.localToWorldMatrix,
                runtimeMaterial,
                0,
                MeshTopology.Triangles,
                argsBuffer,
                0);
        }

        /// <summary>
        /// Submit all cluster triangles to the currently bound URP shadow
        /// atlas.  Shadow rendering is not part of the camera culling results
        /// (procedural geometry has no Renderer), so this draw intentionally
        /// bypasses GPU frustum/Hi-Z culling.
        /// </summary>
        internal void RecordShadowDraw(CommandBuffer command, Material fallbackMaterial)
        {
            if (command == null || !IsRenderable(null))
                return;

            EnsureBuffers();
            EnsureMaterial(fallbackMaterial);
            if (runtimeMaterial == null)
                return;

            FullDrawArgs[1] = (uint)geometry.ClusterCount;
            argsBuffer.SetData(FullDrawArgs);
            runtimeMaterial.SetBuffer(VisibleIndicesId, visibleIndicesBuffer);
            runtimeMaterial.SetBuffer(VerticesBufferId, verticesBuffer);
            command.DrawProceduralIndirect(
                transform.localToWorldMatrix,
                runtimeMaterial,
                1,
                MeshTopology.Triangles,
                argsBuffer,
                0);
        }

        private void EnsureBuffers()
        {
            int clusterCount = geometry.ClusterCount;
            if (uploadedGeometry == geometry && uploadedClusterCount == clusterCount)
                return;

            ReleaseBuffers();
            clusterBuffer = new ComputeBuffer(clusterCount, UnsafeUtility.SizeOf<Cluster>());
            verticesBuffer = new ComputeBuffer(geometry.PointCount, UnsafeUtility.SizeOf<Point>());
            visibleIndicesBuffer = new ComputeBuffer(clusterCount, sizeof(uint));
            argsBuffer = new ComputeBuffer(4, sizeof(uint), ComputeBufferType.IndirectArguments);

            clusterBuffer.SetData(geometry.Clusters);
            verticesBuffer.SetData(geometry.Points);
            visibleIndicesBuffer.SetData(CreateSequentialIndices(clusterCount));
            BuildBakedMaterials();
            uploadedGeometry = geometry;
            uploadedClusterCount = clusterCount;
        }

        private bool EnsureCullingKernels(ComputeShader shader)
        {
            if (cachedCullingShader == shader && clearKernel >= 0 && cullKernel >= 0)
                return true;

            cachedCullingShader = shader;
            clearKernel = -1;
            cullKernel = -1;
            if (shader == null
                || !shader.HasKernel("ClearArgs")
                || !shader.HasKernel("Cull"))
                return false;

            clearKernel = shader.FindKernel("ClearArgs");
            cullKernel = shader.FindKernel("Cull");
            return clearKernel >= 0 && cullKernel >= 0;
        }

        private void EnsureMaterial(Material fallbackMaterial)
        {
            Material requested = materialOverride != null ? materialOverride : fallbackMaterial;
            if (requested == null)
                return;
            if (runtimeMaterial != null && sourceMaterial == requested)
                return;

            ReleaseMaterial();
            runtimeMaterial = new Material(requested)
            {
                name = requested.name + " (Cluster Runtime)"
            };
            sourceMaterial = requested;
        }

        private void ReleaseBuffers()
        {
            DisposeBuffer(ref clusterBuffer);
            DisposeBuffer(ref verticesBuffer);
            DisposeBuffer(ref visibleIndicesBuffer);
            DisposeBuffer(ref argsBuffer);
            DisposeBuffer(ref triangleMaterialBuffer);
            DisposeBuffer(ref materialPropertiesBuffer);
            if (materialTextures != null)
            {
                materialTextures = null;
            }
            uploadedGeometry = null;
            uploadedClusterCount = 0;
            cachedCullingShader = null;
            clearKernel = -1;
            cullKernel = -1;
            lastVisibleClusterCount = 0;
            lastTotalClusterCount = 0;
            lastDrawUsedGpuCulling = false;
            lastDrawUsedHiZOcclusionCulling = false;
            lastCullingCameraName = "";
            lastHiZCameraName = "";
            lastHiZTextureName = "";
            cullingStatsPending = false;
        }

        private void BuildBakedMaterials()
        {
            int[] indices = geometry.TriangleMaterials;
            ClusterGeometryAsset.MaterialData[] table = geometry.MaterialProperties;
            if (table == null || table.Length == 0 || indices == null
                || indices.Length != geometry.ClusterCount * ClusterGeometryAsset.TrianglesPerCluster
                || geometry.BaseMapArray == null || !SystemInfo.supports2DArrayTextures)
                return;
            for (int i = 0; i < indices.Length; ++i)
                if (indices[i] < 0 || indices[i] >= table.Length)
                {
                    Debug.LogError("Cluster asset contains an invalid triangle material index.", geometry);
                    return;
                }
            materialTextures = geometry.BaseMapArray;
            triangleMaterialBuffer = new ComputeBuffer(indices.Length, sizeof(int));
            triangleMaterialBuffer.SetData(indices);
            materialPropertiesBuffer = new ComputeBuffer(table.Length, 48);
            materialPropertiesBuffer.SetData(table);
        }

        private void BindBakedMaterials()
        {
            bool ready = triangleMaterialBuffer != null && materialPropertiesBuffer != null
                && materialTextures != null;
            runtimeMaterial.SetFloat(UseBakedMaterialsId, ready ? 1f : 0f);
            if (!ready) return;
            runtimeMaterial.SetBuffer(TriangleMaterialId, triangleMaterialBuffer);
            runtimeMaterial.SetBuffer(MaterialPropertiesId, materialPropertiesBuffer);
            runtimeMaterial.SetTexture(MaterialTexturesId, materialTextures);
        }

        private void ReleaseMaterial()
        {
            if (runtimeMaterial != null)
            {
                if (Application.isPlaying)
                    Destroy(runtimeMaterial);
                else
                    DestroyImmediate(runtimeMaterial);
            }
            runtimeMaterial = null;
            sourceMaterial = null;
        }

        private static void DisposeBuffer(ref ComputeBuffer buffer)
        {
            if (buffer == null)
                return;
            buffer.Dispose();
            buffer = null;
        }

        private static uint[] CreateSequentialIndices(int count)
        {
            uint[] indices = new uint[count];
            for (int i = 0; i < indices.Length; ++i)
                indices[i] = (uint)i;
            return indices;
        }

        private static Vector4[] GetFrustumPlanes(Camera camera)
        {
            Plane[] sourcePlanes = GeometryUtility.CalculateFrustumPlanes(camera);
            Vector4[] planes = new Vector4[sourcePlanes.Length];
            for (int i = 0; i < sourcePlanes.Length; ++i)
            {
                Plane plane = sourcePlanes[i];
                planes[i] = new Vector4(plane.normal.x, plane.normal.y, plane.normal.z, plane.distance);
            }
            return planes;
        }

        private void RequestCullingStatsIfNeeded(CommandBuffer command, bool gpuCullingActive, int clusterCount)
        {
            if (!debugCullingStats)
            {
                lastVisibleClusterCount = gpuCullingActive ? lastVisibleClusterCount : clusterCount;
                cullingStatsPending = false;
                return;
            }

            if (!gpuCullingActive || !SystemInfo.supportsAsyncGPUReadback)
            {
                lastVisibleClusterCount = clusterCount;
                cullingStatsPending = false;
                return;
            }

            if (cullingStatsPending)
                return;

            cullingStatsPending = true;
            command.RequestAsyncReadback(argsBuffer, sizeof(uint), sizeof(uint), request =>
            {
                cullingStatsPending = false;
                if (request.hasError)
                    return;

                uint visibleCount = request.GetData<uint>()[0];
                lastVisibleClusterCount = (int)Math.Min(visibleCount, (uint)clusterCount);
            });
        }

        private static Vector4[] CreateDefaultHiZData()
        {
            Vector4[] data = new Vector4[16];
            for (int i = 0; i < data.Length; ++i)
                data[i] = new Vector4(i == 0 ? 1f : 0f, i == 0 ? 1f : 0f, 0f, 0f);
            return data;
        }
    }
}
