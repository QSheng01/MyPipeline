using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using SYPackages.Utillity;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    public class ScreenShotsFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class setting
        {
            public RenderPassEvent passEvent = RenderPassEvent.AfterRendering;

            [Tooltip("降采样次数")] public DownSamle downsample = DownSamle.x4;
            [Tooltip("模糊半径")] [Range(0, 3f)] public float blurRadius = 1;
            [Tooltip("模糊材质")]
            public Shader blurShader;
        }

        [Serializable]
        public enum DownSamle
        {
            x1 = 1,
            x2 = 2,
            x4 = 4,
            x6 = 6,
        }

        private static int screenShotsId = Shader.PropertyToID("_ScreenShotsTex");
        private static readonly string ShaderName = "Hidden/ShiYue/URP/PostProcess/UIBlurWithTwoPass";

        public setting defaultSetting;


        private ScreenShotsPass m_ScreenShots;
        [NonSerialized] private PostProcessData m_data;

        private Material m_blurMat;

        private bool m_CaptureScreenShot = false;
        RenderTargetIdentifier m_CaptureRT;

        private setting m_screenshot_setting;

        //[NonSerialized]
        //private  RenderTexture screenshot = null;
        public override void Create()
        {
            m_ScreenShots = new ScreenShotsPass();
            m_screenshot_setting = defaultSetting;
            m_CaptureScreenShot = false;
            m_data = null;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            // if (screenshot!=null)
            // {
            //     RenderTexture.ReleaseTemporary(screenshot);
            // }
        }

        public RenderTexture CaptureScreenShot(DownSamle downSapmle = DownSamle.x4,
            float blurRadius = 1.0f)
        {
            m_CaptureScreenShot = true;
            m_screenshot_setting = new setting()
            {
                passEvent = RenderPassEvent.AfterRendering,
                downsample = downSapmle,
                blurRadius = blurRadius,
            };

            int width = UnityEngine.Mathf.FloorToInt(Screen.width / (int) m_screenshot_setting.downsample);
            int height = UnityEngine.Mathf.FloorToInt(Screen.height / (int) m_screenshot_setting.downsample);
            RenderTexture screenshot = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);
            screenshot.name = "_ScreenShot";
            m_CaptureRT = new RenderTargetIdentifier(screenshot);
            return screenshot;
        }

        public RenderTexture CaptureScreenShotBySetting()
        {
            m_CaptureScreenShot = true;
            m_screenshot_setting = defaultSetting;

            int width = UnityEngine.Mathf.FloorToInt(Screen.width / (int) m_screenshot_setting.downsample);
            int height = UnityEngine.Mathf.FloorToInt(Screen.height / (int) m_screenshot_setting.downsample);
            width = Mathf.Max(2, width);
            height = Mathf.Max(2, height);

            RenderTexture screenshot = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);
            screenshot.name = "_ScreenShot";
            m_CaptureRT = new RenderTargetIdentifier(screenshot);
            return screenshot;
        }


        public void CaptureScreenShot(Action<RenderTexture> callback, DownSamle downSapmle = DownSamle.x4,
            float blurRadius = 1.0f)
        {
            m_CaptureScreenShot = true;
            m_screenshot_setting = new setting()
            {
                passEvent = RenderPassEvent.AfterRendering,
                downsample = downSapmle,
                blurRadius = blurRadius,
                // debug = false,
            };

            int width = UnityEngine.Mathf.FloorToInt(Screen.width / (int) m_screenshot_setting.downsample);
            int height = UnityEngine.Mathf.FloorToInt(Screen.height / (int) m_screenshot_setting.downsample);
            RenderTextureDescriptor descriptor = new RenderTextureDescriptor();

            RenderTexture screenshot = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);
            screenshot.name = "_ScreenShot";
            screenshot.DiscardContents();
            m_CaptureRT = new RenderTargetIdentifier(screenshot);

            Action<ScriptableRenderContext, Camera[]> action = null;
            action = (context, cameras) =>
            {
                callback?.Invoke(screenshot);
                RenderPipelineManager.endFrameRendering -= action;
            };
            RenderPipelineManager.endFrameRendering += action;
        }

        public void CaptureScreenShotBySetting(Action<RenderTexture> callback)
        {
            m_CaptureScreenShot = true;
            m_screenshot_setting = defaultSetting;

            int width = UnityEngine.Mathf.FloorToInt(Screen.width / (int) m_screenshot_setting.downsample);
            int height = UnityEngine.Mathf.FloorToInt(Screen.height / (int) m_screenshot_setting.downsample);
            width = Mathf.Max(2, width);
            height = Mathf.Max(2, height);

            RenderTexture screenshot = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);
            screenshot.name = "_ScreenShot";
            screenshot.DiscardContents();
            m_CaptureRT = new RenderTargetIdentifier(screenshot);

            Action<ScriptableRenderContext, Camera[]> action = null;
            action = (context, cameras) =>
            {
                callback?.Invoke(screenshot);
                RenderPipelineManager.endFrameRendering -= action;
            };
            RenderPipelineManager.endFrameRendering += action;
        }


        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // bool isrenderlayer = CustomURPUtils.IsRenderLayer(renderingData.cameraData.camera.gameObject.layer,
            //     m_screenshot_setting.CameraLayerMask);
            if (renderingData.cameraData.renderType == CameraRenderType.Overlay
                //&& renderingData.cameraData.isUICamera
            )
            {
                if (m_data == null)
                {
                    //m_data = (renderer as UniversalRenderer).PostProcessData;
                    // Shader blurshader = CustomURPUtils.GetPostProcessShader(m_data, ShaderName);
                    Shader blurshader = m_screenshot_setting.blurShader;

                    if (m_blurMat == null && blurshader != null)
                        m_blurMat = CoreUtils.CreateEngineMaterial(blurshader);
                }

                if (m_blurMat)
                {
                    if (m_CaptureScreenShot)
                    {
                        m_ScreenShots.Setup(m_CaptureRT, m_CaptureRT, m_screenshot_setting, m_blurMat);
                        renderer.EnqueuePass(m_ScreenShots);
                        m_CaptureScreenShot = false;
                    }
                }
            }
        }
    }

    public class ScreenShotsPass : ScriptableRenderPass
    {
        const string CommandBufferTag = "ScreenShotsPass";
        ScreenShotsFeature.setting m_setting = null;
        public Material m_Material;
        private int Iteration = 5; //增加循环次数模糊程度也会提高

        RenderTargetIdentifier source;
        RenderTargetIdentifier destination;

        RenderTargetHandle m_TemporaryColorTextureRT;
        //RenderTargetHandle m_TemporaryRT;

        private static readonly int BlurRadius = Shader.PropertyToID("_BlurRadius");

        public void Setup(RenderTargetIdentifier source, RenderTargetIdentifier destination,
            ScreenShotsFeature.setting setting, Material material)
        {
            this.source = source;
            this.destination = destination;
            this.m_setting = setting;
            this.m_Material = material;
            renderPassEvent = setting.passEvent;
        }

        public ScreenShotsPass()
        {
            m_TemporaryColorTextureRT.Init("_TemporaryColorTextureRT");
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);
            //这个是当前pass得colorTarget
            //if (!m_setting.debug)
            //{
            this.source = renderingData.cameraData.renderer.cameraColorTarget;
            //}
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_Material.SetFloat(BlurRadius, m_setting.blurRadius);
            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.width = (int) (opaqueDesc.width / (int) m_setting.downsample);
            opaqueDesc.height = (int) (opaqueDesc.height / (int) m_setting.downsample);
            opaqueDesc.depthBufferBits = 0;
            opaqueDesc.msaaSamples = 1;


            CommandBuffer cmd = CommandBufferPool.Get(CommandBufferTag);
            cmd.GetTemporaryRT(m_TemporaryColorTextureRT.id, opaqueDesc, FilterMode.Bilinear);

            cmd.Blit(source, m_TemporaryColorTextureRT.id, m_Material, 0);
            cmd.Blit(m_TemporaryColorTextureRT.id, destination, m_Material, 1);
            for (int i = 0; i < Iteration; i++)
            {
                cmd.Blit(destination, m_TemporaryColorTextureRT.id, m_Material, 0);
                cmd.Blit(m_TemporaryColorTextureRT.id, destination, m_Material, 1);
            }

            cmd.ReleaseTemporaryRT(m_TemporaryColorTextureRT.id);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            //cmd.ReleaseTemporaryRT(m_TemporaryColorTextureRT.id);
        }
    }
}