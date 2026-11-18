using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace EffectsPostProcess.Scripts.PostProcessRenderers
{
    [Serializable]
    public class EffectsPostProcessRenderer
    { 
        protected Material _material;

        public string name = "";
        
        public virtual void RenderImage(CommandBuffer cmd, RenderTargetIdentifier src, RenderTargetIdentifier dest, RenderTextureDescriptor desc){}
        //public virtual void RenderImage(CommandBuffer cmd, RenderTexture src, RenderTexture dest){}  
    }
}