namespace MightyTerrainMesh
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    public class MTDetailLayerData
    {
        public GameObject prototype;
        public float minWidth;
        public float maxWidth;
        public float minHeight;
        public float maxHeight;
        public float noiseSpread;
        public Color healthyColor;
        public Color dryColor;
        public int maxDensity;
        public bool waterFloating;
    }

    public class MTData : ScriptableObject
    {
        // Shared material for every terrain patch. Texturing is resolved by AVT from world position,
        // so patches need no per-patch texture state of their own.
        public Material TerrainMaterial;
        public TextAsset TreeData;
        public int MeshDataPack;
        public string MeshPrefix;
        public TextAsset HeightMap;
        public Vector3 HeightmapScale;
        public int HeightmapResolution;
        public MTDetailLayerData[] DetailPrototypes;
        public int DetailWidth;
        public int DetailHeight;
        public int DetailResolutionPerPatch;
        public TextAsset DetailLayers;
    }
}
