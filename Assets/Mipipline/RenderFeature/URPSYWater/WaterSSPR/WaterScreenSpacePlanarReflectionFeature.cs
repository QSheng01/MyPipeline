using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace WaterScreenSpacePlanarReflection
{
    public class WaterScreenSpacePlanarReflectionFeature : ScriptableRendererFeature
    {
        public WaterScreenSpacePlanarReflectionSetting setting;
        [Serializable]
        public class WaterScreenSpacePlanarReflectionSetting
        {
            public bool renderSkyBox;
            public RenderPassEvent skyPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            public Quality skyquality = Quality.x1;
            public RenderPassEvent ssprPassEvent = RenderPassEvent.AfterRenderingTransparents;
            public Quality ssprquality = Quality.x1;
            public bool excludeBackground = false;
            [Range(0.01f, 1f)]
            public float fadeOutScreenBorderWidthVertical = 0.25f;
            [Range(0.01f, 1f)]
            public float fadeOutScreenBorderWidthHorizontal = 0.35f;
            public bool fillHoles = true;

            public bool blur = false;
            [Range(0.01f, 5f)]
            public float blurRadius = 0;

            [Header("模糊材质")]
            public Material blurMaterial;

            // 画质档位 0 1
            [Range(0, 1)]
            public int qualityLevel = 0;

        }

        [Serializable]
        public enum Quality
        {
            x1 = 1,
            x2 = 2,
            x3 = 3,
            x4 = 4,
            x5 = 5
        }

        //接入volume
        private static PlanarReflection _planarReflection;
        private static void SetUp()
        {
            VolumeStack stack = VolumeManager.instance.stack;
            _planarReflection = stack.GetComponent<PlanarReflection>();
        }

        private PlanarSkyboxPass _skyboxPass;
        private SSPRGeneratePass _ssprPass;
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if(renderingData.cameraData.renderType != CameraRenderType.Base)
                return;

            // 不需要在PlanarSkyboxPass和SSPRGeneratePass里再做判断了
            if (Water.visibles.Count == 0)
                return;

            // 画质控制 qualityLevel
            var qualityLevel = setting.qualityLevel;
            // 不支持 compute shader 自动降低画质
            if (!SystemInfo.supportsComputeShaders)
            {
                // 画质设置为最低
                qualityLevel = 1;
            }

            // 如果根据画质，自动设置所有 Water 反射方式
            if (qualityLevel == 0)
            {
                // 支持 compute shader 正常走 SSPR
                foreach(var water in Water.visibles){
                    var render = water.GetComponent<Renderer>();
                    if (render.sharedMaterial)
                    {
                        render.sharedMaterial.DisableKeyword("_REFLECTIONMODE_CUBEMAP");
                        render.sharedMaterial.DisableKeyword("_REFLECTIONMODE_PR");
                        render.sharedMaterial.EnableKeyword("_REFLECTIONMODE_SSPR");
                    }
                }
            }
            else
            {
                // 不支持 compute shader 走 cubemap
                foreach(var water in Water.visibles){
                    var render = water.GetComponent<Renderer>();
                    if (render.sharedMaterial)
                    {
                        render.sharedMaterial.EnableKeyword("_REFLECTIONMODE_CUBEMAP");
                        render.sharedMaterial.DisableKeyword("_REFLECTIONMODE_PR");
                        render.sharedMaterial.DisableKeyword("_REFLECTIONMODE_SSPR");
                    }
                }
            }

            if (setting.renderSkyBox && (bool)_planarReflection.SSPR_renderSkyBox && (bool)_planarReflection.SSPREnabled)
            {
                renderer.EnqueuePass(_skyboxPass);
            }

            if ((bool)_planarReflection.SSPREnabled && _ssprPass.IsSupported())
            {
                _ssprPass.SSPRVolumeSetter(_planarReflection);
                renderer.EnqueuePass(_ssprPass);
            }
        }

        public override void Create()
        {
            SetUp();
            _skyboxPass = new PlanarSkyboxPass(setting);
            _ssprPass = new SSPRGeneratePass(setting);
        }
    }

    public class SSPRGeneratePass : ScriptableRenderPass
    {
        private const string COMMAND_BUFFER_TAG = "WaterSSPRPass";
        private SSPRTexGenerator _generator;

        public SSPRGeneratePass(WaterScreenSpacePlanarReflectionFeature.WaterScreenSpacePlanarReflectionSetting Setting)
        {
            this.renderPassEvent = Setting.ssprPassEvent;
            _generator = new SSPRTexGenerator();
            _generator.excludeBackground = Setting.excludeBackground;
            _generator.quality =(int)Setting.ssprquality;
            _generator.fillHoles = Setting.fillHoles;

            _generator.fadeOutScreenBorderWidthHorizontal = Setting.fadeOutScreenBorderWidthHorizontal;
            _generator.fadeOutScreenBorderWidthVertical = Setting.fadeOutScreenBorderWidthVertical;
            _generator._blurMaterial = Setting.blurMaterial;

            if (Setting.blur)
            {
                //GetMaterial(ref _generator._blurMaterial,"Hidden/Blur_TwoPass");
                // GetMaterial(ref _generator._blurMaterial,"ShiYue/URP/PostProcessing/DualKawaseBlur");
                _generator._blurMaterial.SetFloat("_BlurRadius", Setting.blurRadius);
            }
        }

        public bool IsSupported() => _generator.IsSupported();

        public void SSPRVolumeSetter(PlanarReflection VolumeSetting)
        {
            _generator.fadeOutScreenBorderWidthHorizontal = VolumeSetting.fadeOutScreenBorderWidthHorizontal.value;
            _generator.fadeOutScreenBorderWidthVertical = VolumeSetting.fadeOutScreenBorderWidthVerticle.value;
            if (VolumeSetting.openBlur.value && _generator._blurMaterial != null)
            {
               // GetMaterial(ref _generator._blurMaterial,"Hidden/Blur_TwoPass");
               // _generator._blurMaterial = Setting.blurMaterial;
                // GetMaterial(ref _generator._blurMaterial,"ShiYue/URP/PostProcessing/DualKawaseBlur");
                _generator._blurMaterial.SetFloat("_BlurRadius", VolumeSetting.blurRadius.value);
            }
            else
            {
                if (_generator._blurMaterial)
                    GameObject.DestroyImmediate(_generator._blurMaterial, true);
            }
        }

        private void GetMaterial(ref Material mat, string shaderName)
        {
            if (mat == null || mat.shader == null)
            {
                if (mat != null)
                {
                    GameObject.DestroyImmediate(mat, true);
                    mat = null;
                }

                Shader shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    return;
                }
                mat = new Material(shader);
            }
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            _generator.Configure(cmd, cameraTextureDescriptor);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get(COMMAND_BUFFER_TAG);
            try
            {
                float mindisance = 10000;
                Water minWater=null;
                Vector3 minCenter = Vector3.zero;
                foreach (var w in Water.visibles)
                {
                    //算相机到水面模型的最近距离而不是坐标中心，避免误差
                    float aabbDistance = w.transform.GetComponent<MeshRenderer>().bounds
                        .SqrDistance(renderingData.cameraData.camera.transform.position);

                    //只构造一个水平的镜面反射。假如存在多个不同平面的水体，需要为不同平面分别生成反射贴图。
                    //float distance = Vector3.Distance(w.transform.position,
                    //    renderingData.cameraData.camera.transform.position);
                    if (aabbDistance < mindisance)
                    {
                        minWater = w;
                        mindisance = aabbDistance;
                        minCenter = w.transform.GetComponent<MeshRenderer>().bounds.center;
                    }
                }

                if (minWater!=null)
                {
                    var descriptor = new PlanarDescriptor()
                    {
                        position = minCenter,
                        normal = minWater.transform.up
                    };
                    _generator.Render(cmd, ref renderingData, ref descriptor);

                }
                context.ExecuteCommandBuffer(cmd);


            }finally{
                CommandBufferPool.Release(cmd);
            }
        }

        public override void FrameCleanup(CommandBuffer cmd) //重载释放RT
        {
            if (cmd == null) return;
            _generator.ReleaseTemporary(cmd);
        }
    }


    public class PlanarSkyboxPass : ScriptableRenderPass
    {
        const string REFLECTION_TEX_NAME = "_SkyboxReflectionTex";
        static readonly int s_PlanarRefTexID = Shader.PropertyToID(REFLECTION_TEX_NAME);

        private WaterScreenSpacePlanarReflectionFeature.WaterScreenSpacePlanarReflectionSetting setting;

        RTHandle m_SkyboxReflectionTex;

        public PlanarSkyboxPass(WaterScreenSpacePlanarReflectionFeature.WaterScreenSpacePlanarReflectionSetting Setting){
            this.renderPassEvent = Setting.skyPassEvent;
            this.setting = Setting;
            profilingSampler = new ProfilingSampler(nameof(PlanarSkyboxPass));
        }

        /// <inheritdoc/>
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            int downsampleDivider = (int)setting.skyquality; // 降采样比例
            var descriptor = cameraTextureDescriptor;
            descriptor.msaaSamples = 1;
            // 不需要Depth，节省内存
            descriptor.depthBufferBits = 0;
            descriptor.width /= downsampleDivider;
            descriptor.height /= downsampleDivider;
            RenderingUtils.ReAllocateIfNeeded(ref m_SkyboxReflectionTex, descriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: REFLECTION_TEX_NAME);
            ConfigureTarget(m_SkyboxReflectionTex);
            // TODO: We don't need to clear, since the skybox covers the whole RT.
            // But at moment, we need to set the clear flag to color to set LoadAction to DontCare
            ConfigureClear(ClearFlag.Color, Color.clear);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (Water.visibles.Count == 0)
                return;

            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, profilingSampler))
            {
                var camera = renderingData.cameraData.camera;
                float height = 0;
                float minDistSqr = float.MaxValue;
                foreach (var w in Water.visibles)
                {
                    float distSqr = Vector3.SqrMagnitude(w.transform.position - camera.transform.position);
                    if (distSqr < minDistSqr)
                    {
                        height = w.transform.position.y;
                        minDistSqr = distSqr;
                    }
                }

                Matrix4x4 originWoldToCam = camera.worldToCameraMatrix; //缓存原矩阵
                var reflectionMatrix =
                    CalculateReflectMatrix(Vector3.up, new Vector3(0, height, 0));
                Matrix4x4 reflWorldToCam = originWoldToCam * reflectionMatrix;

                cmd.SetViewMatrix(reflWorldToCam); // 更变矩阵为反转矩阵
                // NOTE: 天空盒都是双面渲染，无需设置正反面
                //cmd.SetInvertCulling(true); // 翻转剔除面
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                context.DrawSkybox(renderingData.cameraData.camera);

                //cmd.SetInvertCulling(false); //还原剔除面
                cmd.SetGlobalTexture(s_PlanarRefTexID, m_SkyboxReflectionTex);
                cmd.SetViewMatrix(originWoldToCam);
            }
        }

        Matrix4x4 CalculateReflectMatrix(Vector3 normal, Vector3 positionOnPlane)
        {
            var d = -Vector3.Dot(normal, positionOnPlane);
            var reflectM = new Matrix4x4();
            reflectM.m00 = 1 - 2 * normal.x * normal.x;
            reflectM.m01 = -2 * normal.x * normal.y;
            reflectM.m02 = -2 * normal.x * normal.z;
            reflectM.m03 = -2 * d * normal.x;

            reflectM.m10 = -2 * normal.x * normal.y;
            reflectM.m11 = 1 - 2 * normal.y * normal.y;
            reflectM.m12 = -2 * normal.y * normal.z;
            reflectM.m13 = -2 * d * normal.y;

            reflectM.m20 = -2 * normal.x * normal.z;
            reflectM.m21 = -2 * normal.y * normal.z;
            reflectM.m22 = 1 - 2 * normal.z * normal.z;
            reflectM.m23 = -2 * d * normal.z;

            reflectM.m30 = 0;
            reflectM.m31 = 0;
            reflectM.m32 = 0;
            reflectM.m33 = 1;
            return reflectM;
        }
    }
}

