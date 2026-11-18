using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace MPipeline
{
    /// <summary>GPU terrain height and LOD texture allocation.</summary>
    internal static class TextureUtility
    {
        internal static RenderTexture CreateHeight(int resolution)
        {
            var texture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear)
            {
                name = "GPU Terrain Height", enableRandomWrite = true,
                filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            if (!texture.Create()) throw new System.InvalidOperationException("Cannot allocate GPU terrain height texture.");
            return texture;
        }

        internal static RenderTexture CreateLODMap(int nodeGrid)
        {
            var texture = new RenderTexture(nodeGrid, nodeGrid, 0, GraphicsFormat.R32_UInt)
            {
                name = "GPU Terrain Node LOD Map", enableRandomWrite = true,
                filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave
            };
            if (!texture.Create()) throw new System.InvalidOperationException("Cannot allocate GPU terrain LOD map.");
            return texture;
        }
    }
}
