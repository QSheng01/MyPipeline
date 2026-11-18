using System;
using EffectsPostProcess.Scripts.Controllers;
using UnityEngine;
using UnityEngine.Rendering;

namespace EffectsPostProcess.Scripts.PostProcessRenderers
{
    [Serializable]
    public class IrisBlurRenderer : EffectsPostProcessRenderer
    {
        private static readonly int IrisBlurTex = Shader.PropertyToID("_IrisBlurTex");
        private static readonly int BlurCenter = Shader.PropertyToID("_BlurCenter");
        private static readonly int IrisBlurParameter = Shader.PropertyToID("_IrisBlurParameter");

        public IrisBlurRenderer()
        {
            this.name = "IrisBlur(光圈模糊)";

            var shader = Shader.Find("Post/Effects/IrisBlur");

            this._material = CoreUtils.CreateEngineMaterial(shader);
        }

        public override void RenderImage(CommandBuffer cmd, RenderTargetIdentifier src, RenderTargetIdentifier dest, RenderTextureDescriptor desc)
        {
            float alpha = EffectsPostProcessController.Instance.irisBlurAlpha;

            if (this._material != null && alpha != 0)
            {
                var w = desc.width;// >> downSample;
                var h = desc.height;// >> downSample;

                RenderTexture temp0 = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.RGB111110Float);
                RenderTexture temp1 = RenderTexture.GetTemporary(w >> 1, h >> 1, 0, RenderTextureFormat.RGB111110Float);

                var blurCenter = EffectsPostProcessController.Instance.irisBlurCenter;
                var blurStrength = EffectsPostProcessController.Instance.irisBlurStrength;
                var blurRadius = EffectsPostProcessController.Instance.irisBlurRadius;
                var irisBlurProperties = new Vector4(blurStrength, blurRadius, 1, alpha);

                this._material.SetVector(BlurCenter, blurCenter);
                this._material.SetVector(IrisBlurParameter, irisBlurProperties);

                cmd.Blit(src, temp1, this._material, 0);
                cmd.Blit(temp1, temp0, this._material, 0);

                this._material.SetTexture(IrisBlurTex, temp0);
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