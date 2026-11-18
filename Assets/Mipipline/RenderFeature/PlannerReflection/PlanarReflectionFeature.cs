using System;
using System.Collections.Generic;
// using RootMotion;
// using SYPackages.Utillity;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlanarReflectionFeature : ScriptableRendererFeature
{
    
    [Serializable]
    public class PlanarReflectionSetting
    {
        public RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingOpaques;
        public HairShadowQuality quality = HairShadowQuality.x1;
        public LayerMask reflectLayer = 1;
        public float planarHeight = 0;
        public bool renderSkyBox;
        public bool blur;
        public float lerpA = 0;
        public float lerpB = 0;
    }
    
    [Serializable]
    public enum HairShadowQuality
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

    class PlanarObjectPass : ScriptableRenderPass
    {
        // readonly
        private static readonly string m_ProfilerTag = "Render PlanarReflection";
        private static readonly int s_PlanarRefTexID = Shader.PropertyToID("_PlanarReflectionTex"); 
        private static readonly string PlanarPassKEYWORD = "_PLANAR_PASS";
        
        public PlanarReflectionSetting setting;
        
        private RenderStateBlock m_RenderStateBlock;
        private FilteringSettings  m_FilteringSettings;
        private ProfilingSampler m_ProfilingSampler; //性能采样器标记 
        private List<ShaderTagId> m_ShaderTagIDList;
        private Matrix4x4 originWoldToCam;

        // RT:
        public RenderTargetIdentifier m_OriginIdentifier;
        private RenderTargetIdentifier m_PlanarRelfTarget = new RenderTargetIdentifier(s_PlanarRefTexID, 0, CubemapFace.Unknown, -1);
        private RenderTextureDescriptor m_Descriptor;

        //Blur
        private RenderTargetHandle BlurBufferTex = new RenderTargetHandle();
        private RenderTargetHandle BlurTempBuffer = new RenderTargetHandle();
        public Shader BlurShader;
        private Material _material;
        private bool blur;
        internal bool Setup(PlanarReflectionSetting setting ,Material material)
        {
            this.setting = setting;
            m_ProfilingSampler = new ProfilingSampler(m_ProfilerTag);
            m_ShaderTagIDList = new List<ShaderTagId>
            {
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("SimpleSky"),
                new ShaderTagId("UniversalForwardReflection")
            };

            _material = material;
            
            RenderQueueRange queue = new RenderQueueRange();
            queue.lowerBound = 2000;
            queue.upperBound = 3100;
            m_FilteringSettings = new FilteringSettings(queue, setting.reflectLayer);
            blur = setting.blur;
            // if (blur)
            // {
            //     BlurBufferTex.Init("PlaneReflection Half");
            //     BlurTempBuffer.Init("BloomTempBuffer");
            // }
            SetUp();
            if ((bool)_planarReflection.openBlur && !(bool)_planarReflection.SSPREnabled)
            {
                BlurBufferTex.Init("PlaneReflection Half");
                BlurTempBuffer.Init("BloomTempBuffer");
            }
            
            return true;
        }

        /// <inheritdoc/>
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            //接入volume
            int downsampleDivider = (int)(HairShadowQuality)_planarReflection.quality;
            //int downsampleDivider = (int)setting.quality; // 降采样比例
            
            // Get temporary render textures
            m_Descriptor = cameraTextureDescriptor;
            m_Descriptor.msaaSamples = 1;
            m_Descriptor.depthBufferBits = 16;
            m_Descriptor.width /= downsampleDivider;
            m_Descriptor.height /= downsampleDivider;
            m_Descriptor.colorFormat = RenderTextureFormat.ARGB32;
            cmd.GetTemporaryRT(s_PlanarRefTexID, m_Descriptor, FilterMode.Bilinear);
            ConfigureTarget(m_PlanarRelfTarget);
            ConfigureClear(ClearFlag.All, Color.clear);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // if (renderingData.cameraData.camera.cameraType == CameraType.Preview || renderingData.cameraData.camera.cameraType == CameraType.SceneView) return;

            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            //cmd.BeginSample(m_ProfilerTag);
            context.ExecuteCommandBuffer(cmd);
            //using (new ProfilingScope(cmd, m_ProfilingSampler))
            //{
                CoreUtils.SetKeyword(cmd, PlanarPassKEYWORD, true);
                Matrix4x4 originWoldToCam = renderingData.cameraData.camera.worldToCameraMatrix;                             //缓存原矩阵 
                //var reflectionMatrix = CalculateReflectMatrix(Vector3.up, new Vector3(0, setting.planarHeight, 0));
                var reflectionMatrix = CalculateReflectMatrix(Vector3.up, new Vector3(0, (float)_planarReflection.planarHeight, 0));
                Shader.SetGlobalFloat("ReflectionHeight",setting.planarHeight);
                Shader.SetGlobalFloat("lerpA",setting.lerpA);
                Shader.SetGlobalFloat("lerpB",setting.lerpB);
                Matrix4x4 reflWorldToCam = originWoldToCam * reflectionMatrix;
                renderingData.cameraData.camera.cullingMatrix *= reflectionMatrix;

                cmd.ClearRenderTarget(true, true, Color.white);
                cmd.SetViewMatrix(reflWorldToCam);                          // 更变矩阵为反转矩阵
                cmd.SetInvertCulling(true);                                 // 翻转剔除面
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                
                var sortFlags = SortingCriteria.CommonTransparent;

                var drawSetting = CreateDrawingSettings(m_ShaderTagIDList, ref renderingData, sortFlags);

                context.DrawRenderers(renderingData.cullResults, ref drawSetting, ref m_FilteringSettings, ref m_RenderStateBlock);

                // if (setting.renderSkyBox)
                // {
                //     context.DrawSkybox(renderingData.cameraData.camera);
                // }
                //接入volume
                if ((bool)_planarReflection.renderSkyBox && !(bool)_planarReflection.SSPREnabled)
                {
                    context.DrawSkybox(renderingData.cameraData.camera);
                }

                var cam = renderingData.cameraData.camera;
                var nearclip = cam.nearClipPlane;
                var farclip = cam.farClipPlane;
                var fieldOfView = cam.fieldOfView;
                var aspect = cam.aspect;
                Vector3 srcPos = cam.transform.position;
                Vector3 srcCamRgt = cam.transform.right;
                Vector3 srcCamUp = cam.transform.up;
                Vector3 srcCamFwd = cam.transform.forward;
                var planeNormal = new Vector3(0, 1, 0);
                var reflectedDir = -ReflectVector(planeNormal, srcCamFwd);
                SetupConvolveParams(srcPos, srcCamRgt, srcCamUp, srcCamUp, reflectionMatrix, planeNormal,nearclip,farclip,fieldOfView,aspect);
            
                cmd.SetInvertCulling(false);                                //还原剔除面
                context.ExecuteCommandBuffer(cmd);
                // if (blur)
                // {
                //     RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor; 
                //     opaqueDesc.width = opaqueDesc.width>>1;
                //     opaqueDesc.height = opaqueDesc.height>>1;
                //     opaqueDesc.depthBufferBits = 0;
                //     opaqueDesc.msaaSamples = 1;
                //     
                //     cmd.GetTemporaryRT( BlurBufferTex.id, (opaqueDesc.width),(opaqueDesc.height),0,FilterMode.Bilinear);
                //     cmd.GetTemporaryRT( BlurTempBuffer.id, (opaqueDesc.width),(opaqueDesc.height),0,FilterMode.Bilinear);
                //     cmd.Blit(m_PlanarRelfTarget,BlurBufferTex.Identifier());
                //
                //     cmd.Blit(m_PlanarRelfTarget, BlurBufferTex.Identifier(), _material, 0);   //横向模糊
                //     cmd.Blit(BlurBufferTex.Identifier(), BlurTempBuffer.Identifier(), _material,1);   //纵向模糊
                //     cmd.Blit(BlurTempBuffer.Identifier(),m_PlanarRelfTarget);
                // }
                if((bool)_planarReflection.openBlur && !(bool)_planarReflection.SSPREnabled)
                {
                    RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor; 
                    opaqueDesc.width = opaqueDesc.width>>1;
                    opaqueDesc.height = opaqueDesc.height>>1;
                    opaqueDesc.depthBufferBits = 0;
                    opaqueDesc.msaaSamples = 1;
                    
                    cmd.GetTemporaryRT( BlurBufferTex.id, (opaqueDesc.width),(opaqueDesc.height),0,FilterMode.Bilinear);
                    cmd.GetTemporaryRT( BlurTempBuffer.id, (opaqueDesc.width),(opaqueDesc.height),0,FilterMode.Bilinear);
                    cmd.Blit(m_PlanarRelfTarget,BlurBufferTex.Identifier());
                
                    cmd.Blit(m_PlanarRelfTarget, BlurBufferTex.Identifier(), _material, 0);   //横向模糊
                    cmd.Blit(BlurBufferTex.Identifier(), BlurTempBuffer.Identifier(), _material,1);   //纵向模糊
                    cmd.Blit(BlurTempBuffer.Identifier(),m_PlanarRelfTarget);
                }

                cmd.SetGlobalTexture(s_PlanarRefTexID, m_PlanarRelfTarget);
                cmd.SetRenderTarget(m_OriginIdentifier);
                cmd.SetViewMatrix(originWoldToCam);
                
                
                CoreUtils.SetKeyword(cmd, PlanarPassKEYWORD, false);
            //}
            // 这些不能放在Using里面！
            //cmd.EndSample(m_ProfilerTag);
            context.ExecuteCommandBuffer(cmd);
            renderingData.cameraData.camera.ResetCullingMatrix();
            CommandBufferPool.Release(cmd);
        }

        static Vector3 ReflectVector(Vector3 vec, Vector3 normal) 
        {
            return 2f * Vector3.Dot(normal, vec) * normal - vec;
        }
        
        public float nearPlaneDistance = 0.1f;
        public float farPlaneDistance = 25f;
        
        void SetupConvolveParams(Vector3 camPos, Vector3 camRgt, Vector3 camUp, Vector3 camFwd, Matrix4x4 reflectionMatrix, Vector3 planeNormal,float nearClipPlane,float farClipPlane,float fieldOfView,float aspect) 
        {
            var power = 2048 >> 2;
            _material.SetFloat("_CosPower", (float)power / 1000f);
            _material.SetFloat("_SampleMip", (float)1);
            
            camPos = reflectionMatrix.MultiplyPoint(camPos);
            camRgt = -ReflectVector(camRgt, planeNormal);
            camUp = -ReflectVector(camUp, planeNormal);
            camFwd = -ReflectVector(camFwd, planeNormal);

            var camNear = nearClipPlane;
            var camFar = farClipPlane;
            var camFov = fieldOfView;
            var camAspect = aspect;

            var frustumCorners = Matrix4x4.identity;

            var fovWHalf = camFov * 0.5f;
            var tanFov = Mathf.Tan(fovWHalf * Mathf.Deg2Rad);

            var toRight = camRgt * camNear * tanFov * camAspect;
            var toTop = camUp * camNear * tanFov;

            var topLeft = (camFwd * camNear - toRight + toTop);
            var camScale = topLeft.magnitude * camFar / camNear;

            topLeft.Normalize();
            topLeft *= camScale;

            Vector3 topRight = camFwd * camNear + toRight + toTop;
            topRight.Normalize();
            topRight *= camScale;

            Vector3 bottomRight = camFwd * camNear + toRight - toTop;
            bottomRight.Normalize();
            bottomRight *= camScale;

            Vector3 bottomLeft = camFwd * camNear - toRight - toTop;
            bottomLeft.Normalize();
            bottomLeft *= camScale;

            frustumCorners.SetRow(0, topLeft);
            frustumCorners.SetRow(1, topRight);
            frustumCorners.SetRow(2, bottomRight);
            frustumCorners.SetRow(3, bottomLeft);
            
            Vector4 camPos4 = new Vector4(camPos.x, camPos.y, camPos.z, 1f);
            _material.SetMatrix("_FrustumCornersWS", frustumCorners);
            _material.SetVector("_CameraWS", camPos4);
            var zparams = Vector4.zero;
            zparams.y = farPlaneDistance / nearPlaneDistance;
            zparams.x = 1f - zparams.y;
            zparams.z = zparams.x / farPlaneDistance;
            zparams.z = zparams.y / farPlaneDistance;
    #if UNITY_5_5_OR_NEWER
    //requires version>5.5b10:
            if(SystemInfo.usesReversedZBuffer)
            {
                zparams.y += zparams.x;
                zparams.x = -zparams.x;
                zparams.w += zparams.z;
                zparams.z = -zparams.z;
            }
    #endif
            _material.SetVector("_PlaneReflectionZParams", zparams);
    }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
            {
                throw new ArgumentNullException("cmd");
            }
            cmd.ReleaseTemporaryRT(s_PlanarRefTexID);
            if (blur)
            {
                cmd.ReleaseTemporaryRT(BlurTempBuffer.id);
                cmd.ReleaseTemporaryRT(BlurBufferTex.id);
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

    PlanarObjectPass m_PlanarRefPass;
    Material _material = null;
    private static readonly string ShaderName = "ShiYue/URP/PostProcessing/Convolve";
    private PostProcessData m_data;
    private Shader m_blurShader;
    
    public PlanarReflectionSetting setting = new PlanarReflectionSetting();
    //全 半 四分之一

    //TODO 加入移除或取消激活RendererFeatrue后清除所持有的RT
    // 目前找不到监测Featrue取消激活或者移除的方法。

    public override void Create()
    {
        m_PlanarRefPass = new PlanarObjectPass();
        m_PlanarRefPass.renderPassEvent = setting.passEvent;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // var shader = Shader.Find(ShaderName);
        if (m_data == null)
        {
            var _renderer = UniversalRenderPipeline.asset.scriptableRenderer;
            //m_data = (_renderer as UniversalRenderer).PostProcessData;
        }

        if (m_blurShader == null)
            //m_blurShader = CustomURPUtils.LoadShader(m_data, ShaderName);
            m_blurShader = Shader.Find(ShaderName);
        if (m_blurShader == null)
        {
            return;
        }

        if (_material == null)
            _material = CoreUtils.CreateEngineMaterial(m_blurShader);
        
        bool shouldAdd = m_PlanarRefPass.Setup(setting,_material);
        
        if (_planarReflection.IsActive() && !(bool)_planarReflection.SSPREnabled)
        {
            if (shouldAdd&&renderingData.cameraData.renderType == CameraRenderType.Base)
            {
                //m_PlanarRefPass.m_OriginIdentifier = renderer.cameraColorTarget;
            
                renderer.EnqueuePass(m_PlanarRefPass);
            }
        }
    }
}



