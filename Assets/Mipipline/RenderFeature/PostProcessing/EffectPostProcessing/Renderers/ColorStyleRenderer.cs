using System;
using EffectsPostProcess.Scripts.Controllers;
using UnityEngine;
using UnityEngine.Rendering;

namespace EffectsPostProcess.Scripts.PostProcessRenderers
{
    [Serializable]
    public class ColorStyleRenderer : EffectsPostProcessRenderer
    {
        private static readonly int ColorStyleParameter = Shader.PropertyToID("_ColorStyleParameter");
        private static readonly int BlackAreaColor = Shader.PropertyToID("_BlackAreaColor");
        private static readonly int WhiteAreaColor = Shader.PropertyToID("_WhiteAreaColor");

        public ColorStyleRenderer()
        {
            this.name = "ColorStyle(黑白闪)";

            var shader = Shader.Find("Post/Effects/ColorStyle");

            if (this._material == null)
            {
                this._material = CoreUtils.CreateEngineMaterial(shader);
            }
        }
        
        public override void RenderImage(CommandBuffer cmd, RenderTargetIdentifier src, RenderTargetIdentifier dest, RenderTextureDescriptor desc)
        {
            if(_material == null)
                return;

            foreach (var str in this._material.shaderKeywords)
            {
                this._material.DisableKeyword(str);
            }
            
            var alpha = EffectsPostProcessController.Instance.colorStyleAlpha;

            if (alpha != 0)
            {
                var x = EffectsPostProcessController.Instance.grayCenter;
                var y = EffectsPostProcessController.Instance.grayOffset;
                var z = 0.0f;

                var blackAreaColor = EffectsPostProcessController.Instance.blackAreaColor;
                var whiteAreaColor = EffectsPostProcessController.Instance.whiteAreaColor;

                this._material.SetVector(ColorStyleParameter, new Vector4(x,y,z,alpha));
                this._material.SetColor (BlackAreaColor, blackAreaColor);
                this._material.SetColor (WhiteAreaColor, whiteAreaColor);

                // Invert
                var invert = EffectsPostProcessController.Instance.colorInvert;
                if (invert)
                {
                    this._material.EnableKeyword("_INVERT");
                }
                else
                {
                    this._material.DisableKeyword("_INVERT");
                }

                cmd.Blit(src, dest, this._material);
            }
            else
            {
                cmd.Blit(src, dest);
            }
            
        }

        //public override void RenderImage(CommandBuffer cmd, RenderTexture src, RenderTexture dest)
        //{
        //    RenderImage(cmd,  src, (RenderTargetIdentifier)dest);
        //}
        
    }

}