using System.Collections.Generic;
using UnityEngine;

namespace MPipeline
{
    /// <summary>Camera-scoped hand-off for independent Hi-Z producers and consumers.</summary>
    internal static class ClusterHiZResources
    {
        internal struct Data
        {
            internal RenderTexture Texture;
            internal Vector4 ScreenSize;
            internal int MaxMip;
            internal Vector4[] MipOffsets;
            internal Vector4[] MipSizes;
            // Provenance for consumers that explicitly opt into historical depth.
            internal int Frame;
            internal Matrix4x4 CameraMatrix;
            internal Matrix4x4 ViewProjection;
            internal RenderTexture Target;
            internal Vector2Int CameraPixelSize;
            internal double PublishedTime;
        }

        private struct Entry { internal Data Data; internal int Frame; }
        private static readonly Dictionary<int, Entry> Entries = new Dictionary<int, Entry>(8);

        internal static readonly int TextureId = Shader.PropertyToID("_MPipelineHiZDepthTexture");
        internal static readonly int ScreenSizeId = Shader.PropertyToID("_MPipelineHiZScreenSize");
        internal static readonly int MaxMipId = Shader.PropertyToID("_MPipelineHiZMaxMip");
        internal static readonly int ReversedZId = Shader.PropertyToID("_MPipelineHiZReversedZ");

        internal static void Publish(
            Camera camera,
            RenderTexture texture,
            Vector4 screenSize,
            int maxMip,
            Vector4[] mipOffsets,
            Vector4[] mipSizes)
        {
            if (camera == null || texture == null) return;
            Entries[camera.GetInstanceID()] = new Entry
            {
                Data = new Data
                {
                    Texture = texture,
                    ScreenSize = screenSize,
                    MaxMip = maxMip,
                    MipOffsets = mipOffsets,
                    MipSizes = mipSizes,
                    Frame = Time.frameCount,
                    CameraMatrix = camera.projectionMatrix * camera.worldToCameraMatrix,
                    // The packed atlas follows the render-texture projection, as does
                    // the existing ClusterRuntimeRenderer consumer.
                    ViewProjection = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true) * camera.worldToCameraMatrix,
                    Target = camera.targetTexture,
                    CameraPixelSize = new Vector2Int(camera.pixelWidth, camera.pixelHeight),
                    PublishedTime = Time.realtimeSinceStartupAsDouble
                },
                Frame = Time.frameCount
            };
        }

        internal static bool TryGet(Camera camera, out Data data)
        {
            Entry entry;
            if (camera != null && Entries.TryGetValue(camera.GetInstanceID(), out entry)
                && entry.Frame == Time.frameCount && entry.Data.Texture != null)
            { data = entry.Data; return true; }
            data = default;
            return false;
        }

        internal static bool TryGetLatest(Camera camera, out Data data)
        {
            Entry entry;
            if (camera != null
                && Entries.TryGetValue(camera.GetInstanceID(), out entry)
                && entry.Data.Texture != null)
            {
                data = entry.Data;
                return true;
            }
            data = default;
            return false;
        }

        internal static void Invalidate(Camera camera)
        { if (camera != null) Entries.Remove(camera.GetInstanceID()); }

        internal static void Clear() => Entries.Clear();
    }
}
