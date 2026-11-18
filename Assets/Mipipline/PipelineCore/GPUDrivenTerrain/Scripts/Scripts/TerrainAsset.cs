using UnityEngine;

namespace MPipeline
{
    /// <summary>
    /// Runtime counterpart of the reference TerrainAsset. The existing scene
    /// already owns its TerrainData, AVT material and textures, so no duplicate
    /// ScriptableObject or imported demo terrain is required.
    /// </summary>
    internal sealed class TerrainAsset
    {
        internal const int PatchesPerNode = 8;
        internal const int PatchMeshGridCount = 16;

        internal readonly TerrainData Data;
        internal readonly Material SourceMaterial;
        internal readonly ComputeShader ComputeShader;
        internal readonly Shader RenderShader;
        internal readonly Texture2D BakedHeight;
        internal readonly Texture2D BakedNormal;
        private readonly Terrain terrain;
        internal readonly int MaxLOD;
        internal readonly int NodeGrid;

        internal int PatchGrid => NodeGrid * PatchesPerNode;
        internal int MaxPatches => PatchGrid * PatchGrid;
        internal bool UseBaked => BakedHeight != null && BakedNormal != null;
        internal Vector3 Origin => terrain.transform.position;
        internal Vector3 WorldSize => Data.size;

        internal TerrainAsset(Terrain terrain, ComputeShader computeShader, Shader renderShader,
            int maxLOD, Texture2D bakedHeight = null, Texture2D bakedNormal = null)
        {
            Data = terrain.terrainData;
            SourceMaterial = terrain.materialTemplate;
            this.terrain = terrain;
            ComputeShader = computeShader;
            RenderShader = renderShader;
            BakedHeight = bakedHeight;
            BakedNormal = bakedNormal;
            MaxLOD = maxLOD;
            NodeGrid = 1 << maxLOD;
        }
    }
}
