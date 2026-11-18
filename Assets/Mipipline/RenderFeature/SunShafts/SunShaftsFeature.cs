using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GTM.URP.SunShaft
{
    public class SunShaftsFeature : ScriptableRendererFeature
    {
        public static readonly string BUILD_SKY_SHADER_NAME = "ShiYue/URP/SunShaft/BuildSkyForBlurShader";
        public static readonly string BLUR_SHADER_NAME = "ShiYue/URP/SunShaft/DirectionalBlurShader";
        public static readonly string FINAL_BLEND_SHADER_NAME = "ShiYue/URP/SunShaft/FinalBlendShader";

        SunShaftsPass m_ShaftsPass;
        public SunShaftsProperties props;
        public RenderPassEvent shaftsPassEvent = RenderPassEvent.AfterRenderingTransparents;

        public override void Create()
        {
            if (props == null)
            {
                props = new SunShaftsProperties();
            }
            
            m_ShaftsPass = new SunShaftsPass(props)
            {
                renderPassEvent = shaftsPassEvent,
                originRenderPassEvent = shaftsPassEvent,
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (props == null)
                return;

            props.CacheSunShafts();
            if (props.sunShafts == null)
                return;

            props.CopyVolumeProperty();

            var camera = renderingData.cameraData.camera;
            if (camera.cameraType == CameraType.Reflection || camera.cameraType == CameraType.Preview)
                return;
            
            renderer.EnqueuePass(m_ShaftsPass);
        }
    }
}