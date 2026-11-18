using System;
using UnityEngine;

namespace SYPackages.PerObjectShadow
{
    [Serializable]
    public class PerObjectShadowRenderContext
    {
            public Renderer Renderer;
            public Material Material;
            public int submeshIndex;
            public int shaderPass;

            public PerObjectShadowRenderContext(Renderer renderer, Material material, int submeshIndex, int shaderPass)
            {
                this.Renderer = renderer;
                this.Material = material;
                this.submeshIndex = submeshIndex;
                this.shaderPass = shaderPass;
            }
            //避免记录错误，初始化时再找一次
            public void SetUp()
            {
                this.shaderPass = Material.FindPass(PerObjectShadowPass.DEPTHONLYPASS);
            }
    }
}