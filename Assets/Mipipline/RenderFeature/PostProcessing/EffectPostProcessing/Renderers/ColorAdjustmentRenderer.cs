using EffectsPostProcess.Scripts.Controllers;
using UnityEngine;
using UnityEngine.Rendering;

namespace EffectsPostProcess.Scripts.PostProcessRenderers
{
    public class ColorAdjustmentRenderer : EffectsPostProcessRenderer
    {
        private static readonly int ColorFilter = Shader.PropertyToID("_ColorFilter");
        private static readonly int Contrast = Shader.PropertyToID("_Contrast");
        private static readonly int Saturation = Shader.PropertyToID("_Saturation");
        private static readonly int Brightness = Shader.PropertyToID("_Brightness");
        public ColorAdjustmentRenderer()
        {
            this.name = "ColorAdjustment(颜色调整)";

            var shader = Shader.Find("Post/Effects/ColorAdjustment");

            if (this._material == null)
            {
                this._material = CoreUtils.CreateEngineMaterial(shader);
            }
        }

        public override void RenderImage(CommandBuffer cmd, RenderTargetIdentifier src, RenderTargetIdentifier dest, RenderTextureDescriptor desc)
        {
            if(_material == null)
                return;

            var colorFilter = EffectsPostProcessController.Instance.colorFilter;
            var saturation = EffectsPostProcessController.Instance.saturation;
            var contrast = EffectsPostProcessController.Instance.contrast;
            var brightness = EffectsPostProcessController.Instance.brightness;

            this._material.SetColor(ColorFilter, colorFilter);
            this._material.SetFloat(Saturation, saturation);
            this._material.SetFloat(Contrast,contrast);
            this._material.SetFloat(Brightness,brightness);

            cmd.Blit(src, dest, this._material);
        }
    }
}