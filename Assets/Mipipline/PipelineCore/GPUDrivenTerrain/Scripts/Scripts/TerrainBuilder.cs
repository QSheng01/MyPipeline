using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MPipeline
{
    /// <summary>
    /// Follows the upstream TerrainBuilder pipeline: height bounds, GPU
    /// quadtree traversal, LOD map, 8x8 patch expansion and indirect args.
    /// Camera-local append queues keep Game and Scene draws independent.
    /// </summary>
    internal sealed class TerrainBuilder : IDisposable
    {
        private readonly GPUTerrain owner;
        internal readonly TerrainAsset asset;
        private readonly ComputeShader buildShader;
        internal readonly Mesh mesh;
        internal readonly RenderTexture height;
        internal readonly ComputeBuffer minMax;
        internal readonly Dictionary<Camera, CameraBuffers> cameras = new Dictionary<Camera, CameraBuffers>();
        private readonly List<Camera> expired = new List<Camera>();
        private readonly Plane[] planes = new Plane[6];
        private readonly Vector4[] planeVectors = new Vector4[6];
        private readonly int grid, maxLOD, resolution;
        private bool heightDirty = true;
        private double heightUpdatedAt;
        private int generation;
        private int copyHeight, buildMinMax, reduceMinMax, seed, buildDispatch, buildLodDispatch, traverse, buildLodMap, buildPatches;

        internal int GridSize => grid;
        internal int PatchGridSize => asset.PatchGrid;
        internal int PatchCapacity => asset.MaxPatches;
        internal Mesh PatchMesh => mesh;
        internal Texture HeightTexture => asset.UseBaked ? (Texture)asset.BakedHeight : height;
        internal bool IsReady => HeightTexture != null && mesh != null && minMax != null;
        internal void InvalidateHeight() => heightDirty = true;

        internal TerrainBuilder(GPUTerrain owner, TerrainAsset asset)
        {
            this.owner = owner;
            this.asset = asset;
            buildShader = asset.ComputeShader;
            grid = asset.NodeGrid;
            maxLOD = asset.MaxLOD;
            resolution = asset.Data.heightmapResolution;
            copyHeight = buildShader.FindKernel("CopyHeight");
            buildMinMax = buildShader.FindKernel("BuildMinMax");
            reduceMinMax = buildShader.FindKernel("ReduceMinMax");
            seed = buildShader.FindKernel("Seed");
            buildDispatch = buildShader.FindKernel("BuildDispatch");
            buildLodDispatch = buildShader.FindKernel("BuildLodDispatch");
            traverse = buildShader.FindKernel("TraverseQuadTree");
            buildLodMap = buildShader.FindKernel("BuildLodMap");
            buildPatches = buildShader.FindKernel("BuildPatches");
            if (!asset.UseBaked) height = TextureUtility.CreateHeight(resolution);
            minMax = new ComputeBuffer((PatchCapacity * 4 - 1) / 3, 8);
            mesh = MeshUtility.CreatePlaneMesh(TerrainAsset.PatchMeshGridCount);
        }

        internal bool TryGetDiagnostics(Camera camera, out GPUTerrain.CullingDiagnostics diagnostics)
        {
            if (camera != null && cameras.TryGetValue(camera, out var buffers))
            { diagnostics = buffers.Diagnostics; return true; }
            diagnostics = default;
            return false;
        }

        internal sealed class CameraBuffers : IDisposable
        {
            internal readonly ComputeBuffer A, B, Leaves, Visible, Shadow, Count, Dispatch, LodDispatch, Args, ShadowArgs, Stats;
            internal readonly RenderTexture LodMap;
            internal readonly MaterialPropertyBlock Properties = new MaterialPropertyBlock();
            internal readonly MaterialPropertyBlock ShadowProperties = new MaterialPropertyBlock();
            internal bool Pending, Disposed;
            internal int Draws;
            internal double NextStatistics;
            internal GPUTerrain.CullingDiagnostics Diagnostics;

            internal CameraBuffers(int capacity, int grid, uint indexCount)
            {
                int nodeCapacity = capacity / (TerrainAsset.PatchesPerNode * TerrainAsset.PatchesPerNode);
                A = new ComputeBuffer(nodeCapacity, 16, ComputeBufferType.Append);
                B = new ComputeBuffer(nodeCapacity, 16, ComputeBufferType.Append);
                Leaves = new ComputeBuffer(nodeCapacity, 16, ComputeBufferType.Append);
                Visible = new ComputeBuffer(capacity, 16, ComputeBufferType.Append);
                Shadow = new ComputeBuffer(capacity, 16, ComputeBufferType.Append);
                Count = new ComputeBuffer(1, 4, ComputeBufferType.Raw);
                Stats = new ComputeBuffer(4, 4, ComputeBufferType.Raw);
                Dispatch = new ComputeBuffer(3, 4, ComputeBufferType.IndirectArguments);
                LodDispatch = new ComputeBuffer(3, 4, ComputeBufferType.IndirectArguments);
                LodDispatch.SetData(new uint[] { 0, 1, 1 });
                Args = new ComputeBuffer(5, 4, ComputeBufferType.IndirectArguments);
                ShadowArgs = new ComputeBuffer(5, 4, ComputeBufferType.IndirectArguments);
                var args = new uint[] { indexCount, 0, 0, 0, 0 };
                Args.SetData(args); ShadowArgs.SetData(args);
                LodMap = TextureUtility.CreateLODMap(grid);
            }

            public void Dispose()
            {
                Disposed = true;
                A.Release(); B.Release(); Leaves.Release(); Visible.Release(); Shadow.Release();
                Count.Release(); Stats.Release(); Dispatch.Release(); LodDispatch.Release(); Args.Release(); ShadowArgs.Release();
                CoreUtils.Destroy(LodMap);
            }
        }

        private void RebuildHeight(CommandBuffer cmd)
        {
            cmd.SetComputeIntParam(buildShader, "_Resolution", resolution);
            cmd.SetComputeIntParam(buildShader, "_PatchGrid", PatchGridSize);
            if (!asset.UseBaked)
            {
                cmd.SetComputeTextureParam(buildShader, copyHeight, "_SourceHeight", asset.Data.heightmapTexture);
                cmd.SetComputeTextureParam(buildShader, copyHeight, "_HeightOutput", height);
                cmd.DispatchCompute(buildShader, copyHeight, (resolution + 7) / 8, (resolution + 7) / 8, 1);
            }
            cmd.SetComputeTextureParam(buildShader, buildMinMax, "_Height", HeightTexture);
            cmd.SetComputeBufferParam(buildShader, buildMinMax, "_MinMax", minMax);
            cmd.DispatchCompute(buildShader, buildMinMax, (PatchGridSize + 7) / 8, (PatchGridSize + 7) / 8, 1);
            cmd.SetComputeBufferParam(buildShader, reduceMinMax, "_MinMax", minMax);
            for (int lod = 1; lod <= maxLOD + 3; lod++)
            {
                int n = PatchGridSize >> lod;
                cmd.SetComputeIntParam(buildShader, "_ReduceLOD", lod);
                cmd.DispatchCompute(buildShader, reduceMinMax, (n + 7) / 8, (n + 7) / 8, 1);
            }
            heightDirty = false;
            heightUpdatedAt = Time.realtimeSinceStartupAsDouble;
        }

        private void PrepareDispatch(CommandBuffer cmd, CameraBuffers buffers, ComputeBuffer input)
        {
            cmd.CopyCounterValue(input, buffers.Count, 0);
            cmd.SetComputeBufferParam(buildShader, buildDispatch, "_Count", buffers.Count);
            cmd.SetComputeBufferParam(buildShader, buildDispatch, "_Dispatch", buffers.Dispatch);
            cmd.DispatchCompute(buildShader, buildDispatch, 1, 1, 1);
        }

        internal void RecordSelection(Camera camera, Camera cullingCamera, CameraBuffers b)
        {
            CommandBuffer cmd = CommandBufferPool.Get("GPU Terrain: quadtree, LOD map, cull");
            try
            {
                cmd.SetComputeIntParam(buildShader, "_Grid", grid);
                cmd.SetComputeIntParam(buildShader, "_PatchGrid", PatchGridSize);
                cmd.SetComputeIntParam(buildShader, "_MaxLOD", maxLOD);
                cmd.SetComputeVectorParam(buildShader, "_Origin", asset.Origin);
                cmd.SetComputeVectorParam(buildShader, "_Size", asset.Data.size);
                // The Scene camera remains the drawing camera. Only LOD selection
                // and culling use the selected game camera in observation mode.
                cmd.SetComputeVectorParam(buildShader, "_CameraPosition", cullingCamera.transform.position);
                cmd.SetComputeFloatParam(buildShader, "_LodDistance", owner.lodDistance);
                cmd.SetComputeFloatParam(buildShader, "_BoundsPadding", owner.boundsPadding);
                bool rebuiltHeight = heightDirty;
                if (heightDirty) RebuildHeight(cmd);
                cmd.SetBufferCounterValue(b.A, 0); cmd.SetBufferCounterValue(b.Leaves, 0);
                cmd.SetBufferCounterValue(b.Visible, 0); cmd.SetBufferCounterValue(b.Shadow, 0);
                cmd.SetComputeBufferParam(buildShader, seed, "_NextNodes", b.A);
                cmd.SetComputeBufferParam(buildShader, seed, "_Stats", b.Stats);
                cmd.DispatchCompute(buildShader, seed, 1, 1, 1);
                cmd.SetComputeBufferParam(buildShader, traverse, "_MinMax", minMax);
                cmd.SetComputeBufferParam(buildShader, traverse, "_Count", b.Count);
                cmd.SetComputeBufferParam(buildShader, traverse, "_Leaves", b.Leaves);
                ComputeBuffer input = b.A, output = b.B;
                for (int lod = maxLOD; lod >= 0; lod--)
                {
                    PrepareDispatch(cmd, b, input);
                    cmd.SetBufferCounterValue(output, 0);
                    cmd.SetComputeBufferParam(buildShader, traverse, "_Nodes", input);
                    cmd.SetComputeBufferParam(buildShader, traverse, "_NextNodes", output);
                    cmd.DispatchCompute(buildShader, traverse, b.Dispatch, 0);
                    var swap = input; input = output; output = swap;
                }
                cmd.CopyCounterValue(b.Leaves, b.Count, 0);
                cmd.SetComputeBufferParam(buildShader, buildLodDispatch, "_Count", b.Count);
                cmd.SetComputeBufferParam(buildShader, buildLodDispatch, "_Dispatch", b.LodDispatch);
                cmd.DispatchCompute(buildShader, buildLodDispatch, 1, 1, 1);
                cmd.SetComputeBufferParam(buildShader, buildLodMap, "_Count", b.Count);
                cmd.SetComputeBufferParam(buildShader, buildLodMap, "_LeafRead", b.Leaves);
                cmd.SetComputeTextureParam(buildShader, buildLodMap, "_LodMap", b.LodMap);
                cmd.DispatchCompute(buildShader, buildLodMap, b.LodDispatch, 0);
                cmd.SetComputeBufferParam(buildShader, buildPatches, "_Count", b.Count);
                cmd.SetComputeBufferParam(buildShader, buildPatches, "_LeafRead", b.Leaves);
                cmd.SetComputeBufferParam(buildShader, buildPatches, "_MinMax", minMax);
                cmd.SetComputeBufferParam(buildShader, buildPatches, "_Visible", b.Visible);
                cmd.SetComputeBufferParam(buildShader, buildPatches, "_Shadow", b.Shadow);
                cmd.SetComputeBufferParam(buildShader, buildPatches, "_Stats", b.Stats);
                cmd.SetComputeTextureParam(buildShader, buildPatches, "_LodMap", b.LodMap);
                GeometryUtility.CalculateFrustumPlanes(cullingCamera, planes);
                for (int i = 0; i < 6; i++) planeVectors[i] = new Vector4(planes[i].normal.x, planes[i].normal.y, planes[i].normal.z, planes[i].distance);
                cmd.SetComputeVectorArrayParam(buildShader, "_Planes", planeVectors);
                cmd.SetComputeIntParam(buildShader, "_FrustumCulling", owner.frustumCulling ? 1 : 0);
                BindHiZ(cmd, camera, cullingCamera, b, rebuiltHeight);
                cmd.DispatchCompute(buildShader, buildPatches, b.LodDispatch, 0);
                cmd.CopyCounterValue(b.Visible, b.Args, 4);
                cmd.CopyCounterValue(b.Shadow, b.ShadowArgs, 4);
                cmd.CopyCounterValue(b.Shadow, b.Stats, 8);
                cmd.CopyCounterValue(b.Visible, b.Stats, 12);
                Graphics.ExecuteCommandBuffer(cmd);
            }
            finally { CommandBufferPool.Release(cmd); }
        }

        private void BindHiZ(CommandBuffer cmd, Camera camera, Camera cullingCamera, CameraBuffers buffers, bool rebuiltHeight)
        {
            bool available = ClusterHiZResources.TryGetLatest(cullingCamera, out ClusterHiZResources.Data data);
            bool self = camera == cullingCamera;
            bool freshFrame = data.Frame == Time.frameCount - 1 || (!self && data.Frame == Time.frameCount);
            bool use = false;
            string reason;
            if (!owner.hiZOcclusion) reason = "开关关闭";
            else if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D11) reason = "当前仅启用 D3D11";
            else if (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView) reason = "当前摄像机类型未启用";
            else if (!available || !data.Texture.IsCreated()) reason = "等待 " + cullingCamera.name + " 的深度（检查 Hi-Z Renderer Feature）";
            else if (rebuiltHeight || data.PublishedTime < heightUpdatedAt) reason = "高度已更新，等待新深度";
            else if (data.CameraMatrix != cullingCamera.projectionMatrix * cullingCamera.worldToCameraMatrix) reason = "源摄像机已移动，等待新深度";
            else if (data.Target != cullingCamera.targetTexture || data.CameraPixelSize != new Vector2Int(cullingCamera.pixelWidth, cullingCamera.pixelHeight)) reason = "源摄像机目标或尺寸已变化";
            else if (Application.isPlaying
                ? !freshFrame
                : Time.realtimeSinceStartupAsDouble - data.PublishedTime > 1.0) reason = "源深度已过期，等待更新";
            // Only a camera consuming its own history needs a full refresh. An
            // observer neither produces nor advances the source camera's history.
            else if (self && buffers.Draws++ % 8 == 0) reason = "周期完整刷新";
            else { reason = "已启用：" + cullingCamera.name; use = true; }
            buffers.Diagnostics.SourceCamera = cullingCamera.name;
            buffers.Diagnostics.HiZActive = use;
            buffers.Diagnostics.HiZStatus = reason;
            cmd.SetComputeIntParam(buildShader, "_UseHiZ", use ? 1 : 0);
            cmd.SetComputeTextureParam(buildShader, buildPatches, "_HiZ", use ? (Texture)data.Texture : Texture2D.blackTexture);
            if (use)
            {
                cmd.SetComputeVectorParam(buildShader, "_HiZScreenSize", data.ScreenSize);
                cmd.SetComputeVectorArrayParam(buildShader, "_HiZMipOffsets", data.MipOffsets);
                cmd.SetComputeVectorArrayParam(buildShader, "_HiZMipSizes", data.MipSizes);
                cmd.SetComputeIntParam(buildShader, "_HiZMaxMip", data.MaxMip);
                cmd.SetComputeMatrixParam(buildShader, "_HiZVP", data.ViewProjection);
                cmd.SetComputeIntParam(buildShader, "_ReversedZ", SystemInfo.usesReversedZBuffer ? 1 : 0);
                cmd.SetComputeFloatParam(buildShader, "_HiZBias", owner.hiZDepthBias);
            }
            if (camera == Camera.main) { owner.LastUsedHiZ = use; owner.HiZStatus = reason; }
        }

        internal void BindProperties(MaterialPropertyBlock properties, ComputeBuffer patches)
        {
            properties.SetBuffer("_GPUTerrainPatches", patches);
            properties.SetTexture("_GPUTerrainHeight", HeightTexture);
            properties.SetTexture("_GPUTerrainNormal", asset.UseBaked ? (Texture)asset.BakedNormal : Texture2D.normalTexture);
            properties.SetInt("_GPUTerrainUseBakedNormal", asset.UseBaked ? 1 : 0);
            properties.SetTexture("_GPUTerrainHoles", asset.Data.holesTexture);
            properties.SetVector("_GPUTerrainOrigin", asset.Origin);
            properties.SetVector("_GPUTerrainSize", asset.Data.size);
            properties.SetVector("_GPUTerrainInfo", new Vector4(PatchGridSize, resolution, maxLOD, TerrainAsset.PatchMeshGridCount));
            properties.SetInt("_GPUTerrainStitch", owner.stitchEdges ? 1 : 0);
            properties.SetInt("_GPUTerrainDebug", owner.debugLOD ? 1 : 0);
        }

        internal void RequestStatistics(Camera camera, CameraBuffers buffers)
        {
            if (!owner.collectStatistics || !SystemInfo.supportsAsyncGPUReadback
                || buffers.Pending || Time.realtimeSinceStartupAsDouble < buffers.NextStatistics) return;
            buffers.NextStatistics = Time.realtimeSinceStartupAsDouble + 0.25;
            buffers.Pending = true;
            int version = generation;
            AsyncGPUReadback.Request(buffers.Stats, request =>
            {
                buffers.Pending = false;
                if (generation == version && !buffers.Disposed && !request.hasError)
                {
                    var values = request.GetData<uint>();
                    buffers.Diagnostics.HasStatistics = true;
                    buffers.Diagnostics.Frustum = (int)values[0];
                    buffers.Diagnostics.Occluded = (int)values[1];
                    buffers.Diagnostics.Selected = (int)values[2];
                    buffers.Diagnostics.Visible = (int)values[3];
                    if (camera != null && camera == Camera.main)
                    {
                        owner.FrustumPatches = (int)values[0]; owner.HiZCulledPatches = (int)values[1];
                        owner.SelectedPatches = (int)values[2]; owner.VisiblePatches = (int)values[3];
                    }
                }
            });
        }

        internal CameraBuffers Dispatch(Camera camera, Camera cullingCamera)
        {
            expired.Clear();
            foreach (var entry in cameras) if (entry.Key == null) expired.Add(entry.Key);
            foreach (Camera dead in expired) { cameras[dead].Dispose(); cameras.Remove(dead); }
            if (!cameras.TryGetValue(camera, out var buffers))
            {
                buffers = new CameraBuffers(PatchCapacity, grid, mesh.GetIndexCount(0));
                cameras.Add(camera, buffers);
            }
            RecordSelection(camera, cullingCamera, buffers);
            return buffers;
        }

        public void Dispose()
        {
            generation++;
            foreach (var buffers in cameras.Values) buffers.Dispose();
            cameras.Clear();
            minMax.Release();
            CoreUtils.Destroy(height);
            CoreUtils.Destroy(mesh);
        }
    }
}
