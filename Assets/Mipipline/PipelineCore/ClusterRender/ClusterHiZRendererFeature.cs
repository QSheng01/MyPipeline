using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MPipeline
{
    /// <summary>
    /// Builds a camera depth pyramid and publishes it as global Hi-Z resources.
    /// Add this feature to the same URP renderer as ClusterRendererFeature.
    /// </summary>
    public sealed class ClusterHiZRendererFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private ComputeShader buildShader;

        [SerializeField]
        private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

        private ClusterHiZPass pass;

        public override void Create()
        {
            if (buildShader == null)
                buildShader = Resources.Load<ComputeShader>("ClusterHiZBuild");

            pass = new ClusterHiZPass(buildShader)
            {
                // URP's CopyDepth pass normally runs at AfterRenderingOpaques.
                // Execute one slot later so _CameraDepthTexture contains this
                // camera's current opaque depth rather than pooled old data.
                renderPassEvent = (RenderPassEvent)Mathf.Max(
                    (int)renderPassEvent,
                    (int)RenderPassEvent.AfterRenderingOpaques + 1)
            };
        }

        public override void AddRenderPasses(
            ScriptableRenderer renderer,
            ref RenderingData renderingData)
        {
            if (pass == null)
                Create();
            if (renderingData.cameraData.cameraType == CameraType.Preview)
                return;

            // URP collects pass input requirements while AddRenderPasses is running.
            // Register this here so it schedules _CameraDepthTexture before Hi-Z.
            pass.ConfigureInput(ScriptableRenderPassInput.Depth);
            renderer.EnqueuePass(pass);
        }

        public override void SetupRenderPasses(
            ScriptableRenderer renderer,
            in RenderingData renderingData)
        {
            if (pass == null || renderingData.cameraData.cameraType == CameraType.Preview)
                return;

            pass.Setup(renderer.cameraDepthTargetHandle);
        }

        protected override void Dispose(bool disposing)
        {
            pass?.Dispose();
            pass = null;
            ClusterHiZResources.Clear();
            Shader.SetGlobalTexture(ClusterHiZResources.TextureId, null);
            Shader.SetGlobalVector(ClusterHiZResources.ScreenSizeId, Vector4.zero);
            Shader.SetGlobalInt(ClusterHiZResources.MaxMipId, 0);
        }

        private sealed class ClusterHiZPass : ScriptableRenderPass
        {
            private readonly ComputeShader buildShader;
            private readonly Material copyDepthMaterial;
            private readonly Dictionary<int, ClusterHiZBuilder> builders =
                new Dictionary<int, ClusterHiZBuilder>(4);
            private RTHandle cameraDepthTarget;

            public ClusterHiZPass(ComputeShader shader)
            {
                buildShader = shader;
                Shader copyDepthShader = Shader.Find("Hidden/Universal Render Pipeline/CopyDepth");
                if (copyDepthShader != null)
                    copyDepthMaterial = CoreUtils.CreateEngineMaterial(copyDepthShader);
                ConfigureInput(ScriptableRenderPassInput.Depth);
            }

            public void Setup(RTHandle depthTarget)
            {
                cameraDepthTarget = depthTarget;
            }

            public override void Execute(
                ScriptableRenderContext context,
                ref RenderingData renderingData)
            {
                Camera camera = renderingData.cameraData.camera;
                if (camera == null || buildShader == null || !SystemInfo.supportsComputeShaders)
                {
                    ClusterHiZResources.Invalidate(camera);
                    return;
                }

                int cameraId = camera.GetInstanceID();
                ClusterHiZBuilder builder;
                if (!builders.TryGetValue(cameraId, out builder))
                {
                    builder = new ClusterHiZBuilder(buildShader);
                    builders.Add(cameraId, builder);
                }

                RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
                CommandBuffer command = CommandBufferPool.Get("MPipeline Cluster Hi-Z");
                int msaaSamples = cameraDepthTarget != null && cameraDepthTarget.rt != null
                    ? Mathf.Max(1, cameraDepthTarget.rt.antiAliasing)
                    : Mathf.Max(1, descriptor.msaaSamples);
                bool sourceYFlipped = cameraDepthTarget != null
                    && renderingData.cameraData.IsHandleYFlipped(cameraDepthTarget);
                bool built = descriptor.width > 0
                    && descriptor.height > 0
                    && builder.Build(
                        command,
                        descriptor.width,
                        descriptor.height,
                        msaaSamples,
                        sourceYFlipped,
                        cameraDepthTarget,
                        copyDepthMaterial);
                if (built)
                {
                    ClusterHiZResources.Publish(
                        camera,
                        builder.Texture,
                        builder.ScreenSize,
                        builder.MaxMip,
                        builder.MipOffsets,
                        builder.MipSizes);
                    command.SetGlobalTexture(ClusterHiZResources.TextureId, builder.Texture);
                    command.SetGlobalVector(ClusterHiZResources.ScreenSizeId, builder.ScreenSize);
                    command.SetGlobalInt(ClusterHiZResources.MaxMipId, builder.MaxMip);
                    command.SetGlobalInt(ClusterHiZResources.ReversedZId, SystemInfo.usesReversedZBuffer ? 1 : 0);
                }
                else
                {
                    ClusterHiZResources.Invalidate(camera);
                    command.SetGlobalTexture(ClusterHiZResources.TextureId, Texture2D.blackTexture);
                    command.SetGlobalVector(ClusterHiZResources.ScreenSizeId, Vector4.zero);
                    command.SetGlobalInt(ClusterHiZResources.MaxMipId, 0);
                }

                context.ExecuteCommandBuffer(command);
                CommandBufferPool.Release(command);
            }

            public void Dispose()
            {
                foreach (ClusterHiZBuilder builder in builders.Values)
                    builder.Dispose();
                builders.Clear();
                CoreUtils.Destroy(copyDepthMaterial);
            }

        }

        private sealed class ClusterHiZBuilder : IDisposable
        {
            private static readonly int CameraDepthTextureId = Shader.PropertyToID("_CameraDepthTexture");
            private static readonly int CameraDepthAttachmentId = Shader.PropertyToID("_CameraDepthAttachment");
            private static readonly int DepthMipChainId = Shader.PropertyToID("_DepthMipChain");
            private static readonly int SourceOffsetAndLimitId = Shader.PropertyToID("_SourceOffsetAndLimit");
            private static readonly int DestinationOffsetAndSizeId = Shader.PropertyToID("_DestinationOffsetAndSize");
            private static readonly int HiZReversedZId = Shader.PropertyToID("_HiZReversedZ");
            private static readonly int HiZSourceYFlippedId = Shader.PropertyToID("_HiZSourceYFlipped");

            private readonly ComputeShader buildShader;
            private readonly int copyDepthKernel;
            private readonly int buildMipKernel;
            private RenderTexture depthPyramid;
            private RTHandle copiedDepth;
            private int width;
            private int height;
            private int atlasWidth;
            private int atlasHeight;
            private int mipCount;
            private Vector4[] mipOffsets;
            private Vector4[] mipSizes;

            public ClusterHiZBuilder(ComputeShader shader)
            {
                buildShader = shader;
                copyDepthKernel = buildShader.HasKernel("CopyDepth") ? buildShader.FindKernel("CopyDepth") : -1;
                buildMipKernel = buildShader.HasKernel("BuildMip") ? buildShader.FindKernel("BuildMip") : -1;
            }

            public RenderTexture Texture => depthPyramid;
            public int MaxMip => Mathf.Max(0, mipCount - 1);
            public Vector4 ScreenSize => new Vector4(width, height, 1f / Mathf.Max(1, width), 1f / Mathf.Max(1, height));
            public Vector4 AtlasSize => new Vector4(atlasWidth, atlasHeight, 1f / Mathf.Max(1, atlasWidth), 1f / Mathf.Max(1, atlasHeight));
            public Vector4[] MipOffsets => mipOffsets;
            public Vector4[] MipSizes => mipSizes;

            public bool Build(
                CommandBuffer command,
                int requestedWidth,
                int requestedHeight,
                int msaaSamples,
                bool sourceYFlipped,
                RTHandle cameraDepthTarget,
                Material copyDepthMaterial)
            {
                if (copyDepthMaterial == null
                    || cameraDepthTarget == null
                    || cameraDepthTarget.rt == null
                    || copyDepthKernel < 0
                    || buildMipKernel < 0
                    || command == null)
                    return false;

                if (!EnsureTexture(requestedWidth, requestedHeight))
                    return false;

                command.SetComputeIntParam(buildShader, HiZReversedZId, SystemInfo.usesReversedZBuffer ? 1 : 0);
                // URP's copy shader normalizes the depth attachment into the
                // RFloat target, including any source Y flip. The compute
                // stage reads that normalized texture without another flip.
                command.SetComputeIntParam(buildShader, HiZSourceYFlippedId, 0);
                ConfigureCopyDepthMaterial(copyDepthMaterial, msaaSamples);
                command.SetGlobalTexture(CameraDepthAttachmentId, cameraDepthTarget.nameID);
                CoreUtils.SetRenderTarget(command, copiedDepth, ClearFlag.None);
                Vector4 scaleBias = sourceYFlipped
                    ? new Vector4(1f, -1f, 0f, 1f)
                    : new Vector4(1f, 1f, 0f, 0f);
                Blitter.BlitTexture(command, cameraDepthTarget, scaleBias, copyDepthMaterial, 0);

                command.SetComputeTextureParam(buildShader, copyDepthKernel, CameraDepthTextureId, copiedDepth);
                command.SetComputeTextureParam(buildShader, copyDepthKernel, DepthMipChainId, depthPyramid);
                command.SetComputeVectorParam(
                    buildShader,
                    DestinationOffsetAndSizeId,
                    new Vector4(mipOffsets[0].x, mipOffsets[0].y, mipSizes[0].x, mipSizes[0].y));
                command.DispatchCompute(buildShader, copyDepthKernel, Mathf.CeilToInt(width / 8f), Mathf.CeilToInt(height / 8f), 1);

                for (int mip = 1; mip < mipCount; ++mip)
                {
                    Vector4 source = mipSizes[mip - 1];
                    Vector4 destination = mipSizes[mip];
                    command.SetComputeTextureParam(buildShader, buildMipKernel, DepthMipChainId, depthPyramid);
                    command.SetComputeVectorParam(
                        buildShader,
                        SourceOffsetAndLimitId,
                        new Vector4(mipOffsets[mip - 1].x, mipOffsets[mip - 1].y, source.x, source.y));
                    command.SetComputeVectorParam(
                        buildShader,
                        DestinationOffsetAndSizeId,
                        new Vector4(mipOffsets[mip].x, mipOffsets[mip].y, destination.x, destination.y));
                    command.DispatchCompute(
                        buildShader,
                        buildMipKernel,
                        Mathf.CeilToInt(destination.x / 8f),
                        Mathf.CeilToInt(destination.y / 8f),
                        1);
                }
                return true;
            }

            private static void ConfigureCopyDepthMaterial(Material material, int msaaSamples)
            {
                material.DisableKeyword("_DEPTH_MSAA_2");
                material.DisableKeyword("_DEPTH_MSAA_4");
                material.DisableKeyword("_DEPTH_MSAA_8");
                material.DisableKeyword("_OUTPUT_DEPTH");
                if (msaaSamples >= 8)
                    material.EnableKeyword("_DEPTH_MSAA_8");
                else if (msaaSamples >= 4)
                    material.EnableKeyword("_DEPTH_MSAA_4");
                else if (msaaSamples >= 2)
                    material.EnableKeyword("_DEPTH_MSAA_2");
            }

            public void Dispose() => ReleaseTexture();

            private bool EnsureTexture(int requestedWidth, int requestedHeight)
            {
                if (depthPyramid != null
                    && depthPyramid.IsCreated()
                    && copiedDepth != null
                    && copiedDepth.rt != null
                    && copiedDepth.rt.IsCreated()
                    && width == requestedWidth
                    && height == requestedHeight)
                    return true;

                ReleaseTexture();
                width = requestedWidth;
                height = requestedHeight;
                BuildPackedMipLayout();
                RenderTextureDescriptor descriptor = new RenderTextureDescriptor(atlasWidth, atlasHeight, RenderTextureFormat.RFloat, 0)
                {
                    autoGenerateMips = false,
                    bindMS = false,
                    enableRandomWrite = true,
                    msaaSamples = 1,
                    useMipMap = false
                };
                depthPyramid = new RenderTexture(descriptor)
                {
                    name = "MPipeline Cluster Hi-Z Depth Pyramid",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                if (!depthPyramid.Create())
                {
                    ReleaseTexture();
                    return false;
                }

                if (depthPyramid.mipmapCount != 1)
                {
                    Debug.LogError($"Cluster Hi-Z packed atlas unexpectedly allocated {depthPyramid.mipmapCount} hardware mip levels.");
                    ReleaseTexture();
                    return false;
                }

                RenderTextureDescriptor copiedDepthDescriptor = new RenderTextureDescriptor(
                    requestedWidth,
                    requestedHeight,
                    RenderTextureFormat.RFloat,
                    0)
                {
                    autoGenerateMips = false,
                    bindMS = false,
                    enableRandomWrite = false,
                    msaaSamples = 1,
                    useMipMap = false
                };
                copiedDepth = RTHandles.Alloc(
                    copiedDepthDescriptor,
                    FilterMode.Point,
                    TextureWrapMode.Clamp,
                    name: "MPipeline Cluster Hi-Z Resolved Depth");
                if (copiedDepth == null || copiedDepth.rt == null || !copiedDepth.rt.IsCreated())
                {
                    ReleaseTexture();
                    return false;
                }

                return mipCount > 0;
            }

            private void BuildPackedMipLayout()
            {
                const int MaxMipLevels = 16;
                mipOffsets = new Vector4[MaxMipLevels];
                mipSizes = new Vector4[MaxMipLevels];

                int currentWidth = width;
                int currentHeight = height;
                atlasWidth = width;
                atlasHeight = height;
                mipCount = 1;
                mipSizes[0] = new Vector4(currentWidth, currentHeight, 0f, 0f);

                while ((currentWidth > 1 || currentHeight > 1) && mipCount < MaxMipLevels)
                {
                    int nextWidth = Mathf.Max(1, (currentWidth + 1) >> 1);
                    int nextHeight = Mathf.Max(1, (currentHeight + 1) >> 1);
                    int previousMip = mipCount - 1;
                    Vector4 previousOffset = mipOffsets[previousMip];
                    Vector4 previousSize = mipSizes[previousMip];
                    Vector2Int nextOffset = (mipCount & 1) != 0
                        ? new Vector2Int((int)previousOffset.x, (int)(previousOffset.y + previousSize.y))
                        : new Vector2Int((int)(previousOffset.x + previousSize.x), (int)previousOffset.y);

                    mipOffsets[mipCount] = new Vector4(nextOffset.x, nextOffset.y, 0f, 0f);
                    mipSizes[mipCount] = new Vector4(nextWidth, nextHeight, 0f, 0f);
                    atlasWidth = Mathf.Max(atlasWidth, nextOffset.x + nextWidth);
                    atlasHeight = Mathf.Max(atlasHeight, nextOffset.y + nextHeight);
                    currentWidth = nextWidth;
                    currentHeight = nextHeight;
                    mipCount++;
                }

                if (currentWidth > 1 || currentHeight > 1)
                    Debug.LogError($"Cluster Hi-Z packed atlas exceeded {MaxMipLevels} mip levels for {width}x{height}.");
            }

            private void ReleaseTexture()
            {
                if (depthPyramid == null)
                    return;
                if (depthPyramid.IsCreated())
                    depthPyramid.Release();
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(depthPyramid);
                else
                    UnityEngine.Object.DestroyImmediate(depthPyramid);
                depthPyramid = null;
                if (copiedDepth != null)
                {
                    RTHandles.Release(copiedDepth);
                    copiedDepth = null;
                }
                width = 0;
                height = 0;
                atlasWidth = 0;
                atlasHeight = 0;
                mipCount = 0;
                mipOffsets = null;
                mipSizes = null;
            }
        }
    }
}
