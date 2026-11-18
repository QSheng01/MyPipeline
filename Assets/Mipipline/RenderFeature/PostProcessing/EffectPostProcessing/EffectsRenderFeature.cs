using System;
using EffectsPostProcess.Scripts.Controllers;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EffectsPostProcess.Scripts
{
    public class EffectsRenderFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Setting
        {
            public RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        EffectsRenderPass _effectsPass = null;
        public Setting setting = new Setting();

        public override void Create()
        {
            this.name        = "Effects PostProcess RenderFeature";

            this._effectsPass = new EffectsRenderPass(setting);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            //this._effectsPass.Setup(renderer.cameraColorTarget);
            renderer.EnqueuePass(this._effectsPass);
        }
    }

    public class EffectsRenderPass : ScriptableRenderPass
    {
        [Header("Tag")]
        private static readonly string renderTag = "Effects PostProcess";

        [Header("Render Target")]
        private RTHandle  _currentTarget;

        private RenderTargetHandle src;
        private RenderTargetHandle dest;

        internal static readonly string srcName = "_EffectSrcTexture";
        internal static readonly string destName = "_EffectDestTexture";

        public EffectsRenderPass(EffectsRenderFeature.Setting setting)
        {
            this.renderPassEvent = setting.passEvent;
            this.src.Init(srcName);
            this.dest.Init(destName);
        }

        // public void Setup(RenderTargetIdentifier colorTarget)
        // {
        //     this._currentTarget = colorTarget;
        // }

        // public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        // {
        //     this._currentTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
        // }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get(renderTag);

            Render(cmd, ref renderingData);

            context.ExecuteCommandBuffer(cmd);

            CommandBufferPool.Release(cmd);
        }

        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            try
            {
                if(
                    EffectsPostProcessController.Instance == null                        ||
                    EffectsPostProcessController.Instance.gameObject.activeInHierarchy == false ||
                    EffectsPostProcessController.Instance.enabled == false
                )
                    return;
            }
            catch (Exception e)
            {
                return;
            }

            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;

            cmd.GetTemporaryRT(src.id, descriptor, FilterMode.Bilinear);
            cmd.GetTemporaryRT(dest.id, descriptor, FilterMode.Bilinear);

            try
            {
                for (int i = 0; i < EffectsPostProcessController.Instance.selectedRenderersList.Count; i++)
                {
                    var key = EffectsPostProcessController.Instance.selectedRenderersList[i];
                    var renderer = EffectsPostProcessController.AllRenderers[key];
                    _currentTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;

                    if (i == 0)
                    {
                        renderer.RenderImage(cmd, this._currentTarget, dest.Identifier(), descriptor);
                        if (EffectsPostProcessController.Instance.selectedRenderersList.Count == 1) //只有1个效果的情况下再blit回target
                        {
                            cmd.Blit(dest.Identifier(), _currentTarget);
                        }
                        continue;
                    }

                    if (i % 2 == 0)
                    {
                        if (i == EffectsPostProcessController.Instance.selectedRenderersList.Count - 1)
                        {
                            renderer.RenderImage(cmd, src.Identifier(), this._currentTarget, descriptor);
                        }
                        else
                        {
                            renderer.RenderImage(cmd, src.Identifier(), dest.Identifier(), descriptor);
                        }
                    }
                    else
                    {
                        if (i == EffectsPostProcessController.Instance.selectedRenderersList.Count - 1)
                        {
                            renderer.RenderImage(cmd, dest.Identifier(), this._currentTarget, descriptor);
                        }
                        else
                        {
                            renderer.RenderImage(cmd, dest.Identifier(), src.Identifier(), descriptor);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                cmd.ReleaseTemporaryRT(dest.id);
                cmd.ReleaseTemporaryRT(src.id);
            }

        }
    }

}