using System;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Lets the AVT package bind a pixel-shader UAV after URP has set the camera or GBuffer target.
    /// Forward uses DrawObjectsPass; deferred uses GBufferPass. Both call this hook.
    /// </summary>
    public static class AdaptiveVirtualTextureDrawHook
    {
        public static Action<CommandBuffer> BeforeDrawRenderers;
        public static Action<CommandBuffer> AfterDrawRenderers;
    }
}