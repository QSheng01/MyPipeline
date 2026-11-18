using UnityEngine;
using UnityEngine.Assertions;

namespace NoOvertime.VirtualTexture
{
    [ExecuteAlways]
    public class VirtualTextureMono : MonoBehaviour
    {
        public Resource resource;
        public int terrainSize = 2048;
        public RuntimeResource runtimeResource;

        private void OnEnable()
        {
            if (resource == null || terrainSize <= 0)
                return;

            Assert.AreEqual(terrainSize % Constant.SectorSize, 0, $"TerrainSize 必须是 SectorSize({Constant.SectorSize}) 的整数倍.");
            if (Context.Instance.IsInitialized)
                return;

            Context.Instance.Initialize(terrainSize, resource, runtimeResource);
            AdaptiveVirtualTextureRuntime.EnsureHook();
        }

        /// <summary>
        /// Rebuilds the AVT context so physical-cache settings (e.g. BC7 feedback compression) take effect.
        /// </summary>
        public static void RebuildContext()
        {
            if (!Context.Instance.IsInitialized)
                return;

            AdaptiveVirtualTextureRuntime.Shutdown();
            Context.Instance.Dispose();
            var mono = UnityEngine.Object.FindObjectOfType<VirtualTextureMono>();
            if (mono != null)
                mono.OnEnable();
        }

        private void OnDisable()
        {
            if (!Context.Instance.IsInitialized)
                return;

            AdaptiveVirtualTextureRuntime.Shutdown();
            Context.Instance.Dispose();
        }
    }
}

