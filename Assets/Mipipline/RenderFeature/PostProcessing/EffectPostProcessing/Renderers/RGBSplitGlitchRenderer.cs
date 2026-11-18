using System;
using EffectsPostProcess.Scripts.Controllers;
using UnityEngine;
using UnityEngine.Rendering;

namespace EffectsPostProcess.Scripts.PostProcessRenderers
{
    [Serializable]
    public class RGBSplitGlitchRenderer : EffectsPostProcessRenderer
    {
        // private static readonly int ColorOffsetParameter = Shader.PropertyToID("_ColorOffsetParameter");
        // private static readonly int RGBSplitGlitchAlpha = Shader.PropertyToID("_RGBSplitGlitchAlpha");

        public RGBSplitGlitchRenderer()
        {
            this.name = "RGBSplitGlith(RGB分离)";

            var shader = Shader.Find("Post/Effects/RGBSplitGlitch");

            if (this._material == null)
            {
                this._material = CoreUtils.CreateEngineMaterial(shader);
            }
        }

        public override void RenderImage(CommandBuffer cmd, RenderTargetIdentifier src, RenderTargetIdentifier dest, RenderTextureDescriptor desc)
        {
            if(_material == null)
                return;
            
            int passID=0;
            switch (EffectsPostProcessController.Instance.Splitdirection)
            {
                case SplitDirection.Horizontal:
                    passID=0;
                    break;
                case SplitDirection.Vertical:
                    passID=1;
                    break;
                case SplitDirection.Horizontal_Vertical:
                    passID=2;
                    break;
            }

            if (EffectsPostProcessController.Instance.Amount != 0)
            {
                Vector4 param=new Vector4(EffectsPostProcessController.Instance.Fading,EffectsPostProcessController.Instance.Amount,EffectsPostProcessController.Instance.Speed,EffectsPostProcessController.Instance.CenterFading);
                Vector4 param2=new Vector4(0,EffectsPostProcessController.Instance.AmountR,EffectsPostProcessController.Instance.AmountB,0);

                this._material.SetVector("_Params", param);
                this._material.SetVector("_Params2", param2);

                cmd.Blit(src, dest, this._material,passID);   
            }
            else
            {
                cmd.Blit(src, dest);
            }
        }
    }
}