using System;
using SYPackages.AtmosphericScattering;
using SYPackages.Utillity;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SYPackages.AtmosphericScatteringFog
{
    public class AtmosphericScatteringRendererFeature : ScriptableRendererFeature
    {
        [Serializable]
        public class AtmosphericScatteringRendererSetting
        {
            public LayerMask CameraLayerMask = 1;
            public LayerMask Fogmask;
            public RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingTransparents;
            // "ShiYue/URP/FOG/OpaqueAtmosphericScattering"
            public Shader shader;
        }

        private AtmosphericScatteringRenderPass m_ScriptablePass;
        private AtmosphericScattering.AtmosphericScatteringFog m_FogVolumeComponent;
        public AtmosphericScatteringRendererSetting setting = new AtmosphericScatteringRendererSetting();
        private static readonly string ShaderName = "ShiYue/URP/FOG/OpaqueAtmosphericScattering";
        private Material m_FogMat;

        /// <inheritdoc/>
        public override void Create()
        {
            m_ScriptablePass = new AtmosphericScatteringRenderPass();
            m_ScriptablePass.renderPassEvent = setting.passEvent;

            if (m_FogMat == null && setting.shader != null)
                m_FogMat = CoreUtils.CreateEngineMaterial(setting.shader);
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            //Debug.Log((1<< renderingData.cameraData.camera.gameObject.layer) +  "   "+  CameraLayerMask.value );
            bool isrenderlayer = CustomURPUtils.IsRenderLayer(renderingData.cameraData.camera.gameObject.layer,
                setting.CameraLayerMask);

            if (renderingData.cameraData.renderType == CameraRenderType.Base && isrenderlayer)
            {
                // When the scene containing volume profiles reloads, unity will destroy all volume components and recreate
                if (!m_FogVolumeComponent)
                {
                    var stack = VolumeManager.instance.stack;
                    m_FogVolumeComponent = stack.GetComponent<AtmosphericScattering.AtmosphericScatteringFog>();
                }

                if (TODVolumeController_V2.UseTOD) // 如果使用的TOD控制器，就可以单独控制大气雾开关
                {
                    if (TODVolumeController_V2.EnableAtmosphericFog)
                    {
                        m_ScriptablePass.SetUp(m_FogVolumeComponent, m_FogMat); //单独弄一张贴图画
                        renderer.EnqueuePass(m_ScriptablePass);
                    }
                }
                else
                {
                    m_ScriptablePass.SetUp(m_FogVolumeComponent, m_FogMat); //单独弄一张贴图画
                    renderer.EnqueuePass(m_ScriptablePass);
                }
            }
        }
    }

    public class AtmosphericScatteringRenderPass : ScriptableRenderPass
    {
        const string m_ProfilerTag = "FOG";
        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler(m_ProfilerTag);

        private AtmosphericScattering.AtmosphericScatteringFog mfog;

        private Material m_OpaqueAtmScatteringMaterial;

        private MaterialPropertyBlock m_OpaqueAtmScatteringBlock = new MaterialPropertyBlock();

        private RTHandle volumetricLighting;

        public static readonly int _VBufferLighting = Shader.PropertyToID("_VBufferLighting");
        public static readonly int MaxFogDistance = Shader.PropertyToID("_MaxFogDistance");
        public static readonly int CrBaseHeight = Shader.PropertyToID("_HeightFogBaseHeight");
        public static readonly int HeightFogExponents = Shader.PropertyToID("_HeightFogExponents");
        public static readonly int FogColor = Shader.PropertyToID("_FogColor");
        public static readonly int FogEndColorAndFallOff = Shader.PropertyToID("_FogEndColor");
        public static readonly int HeightFogBaseScattering = Shader.PropertyToID("_HeightFogBaseScattering");
        public static readonly int HeightFogBaseExtinction = Shader.PropertyToID("_HeightFogBaseExtinction");
        public static readonly int PixelCoordToViewDirWS = Shader.PropertyToID("_PixelCoordToViewDirWS");
        public static readonly int FogColorModed = Shader.PropertyToID("_FogColorMode");

        public static readonly int MipFogParameters = Shader.PropertyToID("_MipFogParameters");

        // public static readonly int DownSampleParameters = Shader.PropertyToID("_DownSampleParameters");
        public static readonly int JitterStrength = Shader.PropertyToID("_JitterStrength");
        public static readonly int SunColor = Shader.PropertyToID("_SunColor");
        public static readonly int SunFallOff = Shader.PropertyToID("_SunFallOff");
        public static readonly int MoonColor = Shader.PropertyToID("_MoonColor");
        public static readonly int MoonFallOff = Shader.PropertyToID("_MoonFallOff");
        public static readonly int FogStartDistance = Shader.PropertyToID("_FogStartDistance");

        // public static readonly int OqaqueAtmosphericScatteringRenderTex =
        //     Shader.PropertyToID("_OqaqueAtmosphericScatteringRenderTex");

        public static readonly string FogKeyWord = "_ATMOSPHERICSCATTERINGFOG";
        public static readonly string SelfFogKeyWord = "_SELF_ATMOSPHERICSCATTERINGFOG";
        public static readonly string SunModeOnKeyword = ("_SUNCOLOR_ON");
        public static readonly string ConstantColor = ("_CONSTANTCOLOR");

        void OnCameraSetup_TOD(ref RenderingData renderingData)
        {
            Shader.DisableKeyword(SelfFogKeyWord);
            Shader.DisableKeyword(FogKeyWord);
            Shader.DisableKeyword(SunModeOnKeyword);
            Shader.DisableKeyword(ConstantColor);
            float maxFogDistance = TODVolumeController_V2.MaxFogDistance;

            var fogmode = (TODVolumeController_V2.ColorMode == FogColorMode.ConstantColor) ? 0 : 1;

            Shader.SetGlobalFloat(FogColorModed, fogmode);

            Color fogColor = TODVolumeController_V2.Tint;
            Shader.SetGlobalColor(FogColor,
                new Color(fogColor.r, fogColor.g, fogColor.b, 0.0f));

            Shader.SetGlobalFloat(FogStartDistance, TODVolumeController_V2.FogStartDistance);

            if (TODVolumeController_V2.EnableSun)
            {
                Shader.EnableKeyword(SunModeOnKeyword);
                Shader.SetGlobalColor(SunColor, TODVolumeController_V2.SunColor);
                Shader.SetGlobalFloat(SunFallOff, TODVolumeController_V2.SunFallOff);

                // 月亮颜色
                Shader.SetGlobalColor(MoonColor, TODVolumeController_V2.MoonColor);
                Shader.SetGlobalFloat(MoonFallOff, TODVolumeController_V2.MoonFallOff);
            }

            Vector4 mipFogParameters = new Vector4(TODVolumeController_V2.MipFogNear, TODVolumeController_V2.MipFogFar,
                TODVolumeController_V2.MipFogMaxMip, 0.0f);

            // DensityVolumeArtistParameters param = new DensityVolumeArtistParameters(albedo.value, meanFreePath.value, anisotropy.value);
            // DensityVolumeEngineData data = param.ConvertToEngineData();

            var extinction = CustomURPUtils.ExtinctionFromMeanFreePath(TODVolumeController_V2.MeanFreePath);
            var scattering =
                CustomURPUtils.ScatteringFromExtinctionAndAlbedo(extinction, (Vector3)(Vector4)TODVolumeController_V2.Albedo);

            float crBaseHeight = TODVolumeController_V2.BaseHeight;

            float layerDepth = Mathf.Max(0.01f, TODVolumeController_V2.MaximumHeight - TODVolumeController_V2.BaseHeight);
            float H = CustomURPUtils.ScaleHeightFromLayerDepth(layerDepth);
            var heightFogExponents = new Vector2(1.0f / H, H);
            var jitterStrength = TODVolumeController_V2.JitterStrength;
            Camera camera = renderingData.cameraData.camera;

            Matrix4x4 pixelCoordToViewDirWS =
                CustomURPUtils.SetupCameraShaderConstants(camera,
                    renderingData.cameraData.isSceneViewCamera ? 1 : renderingData.cameraData.renderScale);

            // The texture can be null when volumetrics are disabled.
            if (volumetricLighting != null)
                m_OpaqueAtmScatteringBlock.SetTexture(_VBufferLighting, volumetricLighting);

            Shader.SetGlobalFloat(MaxFogDistance, maxFogDistance);
            Shader.SetGlobalFloat(CrBaseHeight, crBaseHeight);
            Shader.SetGlobalVector(HeightFogExponents, heightFogExponents);

            Shader.SetGlobalVector(HeightFogBaseScattering, scattering);
            Shader.SetGlobalFloat(HeightFogBaseExtinction, extinction);
            Shader.SetGlobalMatrix(PixelCoordToViewDirWS, pixelCoordToViewDirWS);
            Shader.SetGlobalVector(MipFogParameters, mipFogParameters);
            Shader.SetGlobalFloat(JitterStrength, jitterStrength);
        }

        void CameraSetup_Normal(ref RenderingData renderingData)
        {
            Shader.DisableKeyword(SelfFogKeyWord);
            Shader.DisableKeyword(FogKeyWord);
            Shader.DisableKeyword(SunModeOnKeyword);
            Shader.DisableKeyword(ConstantColor);
            float maxFogDistance = mfog.maxFogDistance.value;

            var fogmode = (mfog.colorMode.value == FogColorMode.ConstantColor) ? 0 : 1;

            Shader.SetGlobalFloat(FogColorModed, fogmode);

            // Color fogColor = (mfog.colorMode.value == FogColorMode.ConstantColor)
            //     ? mfog.FogStartColor.value
            //     : mfog.tint.value;
            Color fogColor = mfog.tint.value;
            Shader.SetGlobalColor(FogColor,
                new Color(fogColor.r, fogColor.g, fogColor.b, 0.0f));
            switch (mfog.colorMode.value)
            {
                // case  FogColorMode.SkyColor:
                //
                //     break;
                case FogColorMode.ConstantColor:

                    Shader.EnableKeyword(ConstantColor);
                    Vector4 fogEndColor = new Vector4(mfog.FogEndColor.value.r, mfog.FogEndColor.value.g,
                        mfog.FogEndColor.value.b, mfog.FogFallOff.value);
                    Shader.SetGlobalColor(FogEndColorAndFallOff,
                        fogEndColor);
                    break;
            }

            Shader.SetGlobalFloat(FogStartDistance, mfog.fogStartDistance.value);

            if (mfog.enableSun.value)
            {
                Shader.EnableKeyword(SunModeOnKeyword);
                Shader.SetGlobalColor(SunColor, mfog.sunColor.value);
                Shader.SetGlobalFloat(SunFallOff, mfog.sunFallOff.value);

                // 月亮颜色
                Shader.SetGlobalColor(MoonColor, mfog.moonColor.value);
                Shader.SetGlobalFloat(MoonFallOff, mfog.moonFallOff.value);
            }

            Vector4 mipFogParameters = new Vector4(mfog.mipFogNear.value, mfog.mipFogFar.value,
                mfog.mipFogMaxMip.value, 0.0f);

            // DensityVolumeArtistParameters param = new DensityVolumeArtistParameters(albedo.value, meanFreePath.value, anisotropy.value);
            // DensityVolumeEngineData data = param.ConvertToEngineData();

            var extinction = CustomURPUtils.ExtinctionFromMeanFreePath(mfog.meanFreePath.value);
            var scattering =
                CustomURPUtils.ScatteringFromExtinctionAndAlbedo(extinction, (Vector3)(Vector4)mfog.albedo.value);

            float crBaseHeight = mfog.baseHeight.value;

            float layerDepth = Mathf.Max(0.01f, mfog.maximumHeight.value - mfog.baseHeight.value);
            float H = CustomURPUtils.ScaleHeightFromLayerDepth(layerDepth);
            var heightFogExponents = new Vector2(1.0f / H, H);
            var jitterStrength = mfog.jitterStrength.value;
            Camera camera = renderingData.cameraData.camera;

            Matrix4x4 pixelCoordToViewDirWS =
                CustomURPUtils.SetupCameraShaderConstants(camera,
                    renderingData.cameraData.isSceneViewCamera ? 1 : renderingData.cameraData.renderScale);

            // The texture can be null when volumetrics are disabled.
            if (volumetricLighting != null)
                m_OpaqueAtmScatteringBlock.SetTexture(_VBufferLighting, volumetricLighting);

            Shader.SetGlobalFloat(MaxFogDistance, maxFogDistance);
            Shader.SetGlobalFloat(CrBaseHeight, crBaseHeight);
            Shader.SetGlobalVector(HeightFogExponents, heightFogExponents);

            Shader.SetGlobalVector(HeightFogBaseScattering, scattering);
            Shader.SetGlobalFloat(HeightFogBaseExtinction, extinction);
            Shader.SetGlobalMatrix(PixelCoordToViewDirWS, pixelCoordToViewDirWS);
            Shader.SetGlobalVector(MipFogParameters, mipFogParameters);
            Shader.SetGlobalFloat(JitterStrength, jitterStrength);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (TODVolumeController_V2.UseTOD)
            {
                OnCameraSetup_TOD(ref renderingData);
            }
            else
            {
                CameraSetup_Normal(ref renderingData);
            }

        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                var camera = renderingData.cameraData.camera;

                // Prepare for manual blit
                cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_OpaqueAtmScatteringMaterial);
                //Blitter.BlitTexture(cmd,Texture2D.blackTexture,new Vector4(1,1,0,0),new Vector4(1,1,0,0),0,true);
                // Blitter.BlitCameraTexture(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle,
                //     renderingData.cameraData.renderer.cameraColorTargetHandle, m_OpaqueAtmScatteringMaterial, 0);
                cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
            }

            context.ExecuteCommandBuffer(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            CoreUtils.SetKeyword(cmd, FogKeyWord, false);
        }

        public void SetUp(AtmosphericScattering.AtmosphericScatteringFog atmosphericScatteringFog, Material fogmat)
        {
            mfog = atmosphericScatteringFog;
            m_OpaqueAtmScatteringMaterial = fogmat;
        }
    }
}