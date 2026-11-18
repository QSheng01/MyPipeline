using System;
using EffectsPostProcess.Scripts.Controllers;
using UnityEngine;
using UnityEngine.Rendering;

namespace EffectsPostProcess.Scripts.PostProcessRenderers
{
    [Serializable]
    public class RadialBlurRenderer : EffectsPostProcessRenderer
    {
        private static readonly int RadiaBlurTex = Shader.PropertyToID("_RadiaBlurTex");
        private static readonly int BlurCenter = Shader.PropertyToID("_BlurCenter");
        private static readonly int RadialBlurParameter = Shader.PropertyToID("_RadialBlurParameter");
        private static readonly int ColorOffsetParameter = Shader.PropertyToID("_ColorOffsetParameter");

        public RadialBlurRenderer()
        {
            this.name = "RadialBlur(径向模糊)";
            
            var shader = Shader.Find("Post/Effects/RadiaBlur");

            this._material = CoreUtils.CreateEngineMaterial(shader);
        }
        
        public override void RenderImage(CommandBuffer cmd, RenderTargetIdentifier src, RenderTargetIdentifier dest, RenderTextureDescriptor desc)
        {
            float alpha        = EffectsPostProcessController.Instance.radialBlurAlpha;
            
            if (this._material != null && alpha != 0)
            {
                // var downSample = EffectsPostProcessController.Instance.radialBlurDownSample;
                
                var w = desc.width;// >> downSample;
                var h = desc.height;// >> downSample;

                RenderTexture temp0 = RenderTexture.GetTemporary(w, h,    0, RenderTextureFormat.RGB111110Float);
                RenderTexture temp1 = RenderTexture.GetTemporary(w>>1, h>>1, 0, RenderTextureFormat.RGB111110Float);

                if (EffectsPostProcessController.Instance.radialBlurRGBSplitEnable)
                {
                    this._material.EnableKeyword("_RGBSPLIT");
                    
                    var rOffset  = EffectsPostProcessController.Instance.radialBlurROffset;
                    var gOffset  = EffectsPostProcessController.Instance.radialBlurGOffset;
                    var bOffset  = EffectsPostProcessController.Instance.radialBlurBOffset;
                    var strength = EffectsPostProcessController.Instance.radialBlurRGBSplitStrength;
                    
                    this._material.SetVector(ColorOffsetParameter, new Vector4(rOffset, gOffset, bOffset, strength));
                }
                else
                {
                    this._material.DisableKeyword("_RGBSPLIT");
                }
                
                //Vector2 blurCenter = camera.WorldToViewportPoint(this._radialBlur.blurObjectWorldPos);
                var blurCenter     = EffectsPostProcessController.Instance.radialBlurCenter;
                var blurStrength     = EffectsPostProcessController.Instance.radialBlurStrength;
                var blurRadius       = EffectsPostProcessController.Instance.radialBlurRadius;
                var radialBlurProperties  = new Vector4(blurStrength, blurRadius, 1, alpha);
                
                this._material.SetVector(BlurCenter,    blurCenter);
                this._material.SetVector(RadialBlurParameter, radialBlurProperties);
                
                cmd.Blit(src, temp1, this._material, 0);
                cmd.Blit(temp1, temp0, this._material, 0);
                
                this._material.SetTexture(RadiaBlurTex, temp0);
                cmd.Blit(src, dest, this._material, 1);
                
                RenderTexture.ReleaseTemporary(temp0);
                RenderTexture.ReleaseTemporary(temp1);
            }
            else
            {
                cmd.Blit(src, dest);
            }
        }
    }
}