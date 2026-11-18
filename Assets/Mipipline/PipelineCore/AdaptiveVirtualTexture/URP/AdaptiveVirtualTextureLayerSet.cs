using UnityEngine;

namespace NoOvertime.VirtualTexture
{
    /// <summary>
    /// Assigns the imported Texture2DArray assets to the terrain material and AVT baker.
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(-50)]
    public class AdaptiveVirtualTextureLayerSet : MonoBehaviour
    {
        public Texture2DArray baseMapArray;
        public Texture2DArray maskMapArray;
        public Texture2D splatMap;
        public Texture2D globalMap;
        public VirtualTextureMono target;
        public Material terrainMaterial;

        private void OnEnable()
        {
            Apply();
        }

        public void Apply()
        {
            if (terrainMaterial != null)
            {
                if (baseMapArray != null)
                    terrainMaterial.SetTexture("_BaseMapArray", baseMapArray);
                if (maskMapArray != null)
                    terrainMaterial.SetTexture("_MaskMapArray", maskMapArray);
                if (splatMap != null)
                    terrainMaterial.SetTexture("_SplatMap", splatMap);
                if (globalMap != null)
                    terrainMaterial.SetTexture("_GlobalMap", globalMap);
            }

            if (target == null || baseMapArray == null || maskMapArray == null)
                return;

            target.runtimeResource = new RuntimeResource
            {
                splatMap = splatMap,
                baseMapArray = baseMapArray,
                maskMapArray = maskMapArray
            };

            if (target.isActiveAndEnabled)
            {
                target.enabled = false;
                target.enabled = true;
            }
        }
    }
}