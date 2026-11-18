using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VoxelVolumetricLightingRenderFeature : ScriptableRendererFeature
{

    //体积光增强Pass
    class GodRayPass : ScriptableRenderPass
    {
        private const string godrayPassIdentifier = "GodRayVolumetricPass";
        private const string godrayShaderPath = "ShiYue/URP/VoxelVolumetric/GodRayVolumetric";
        private VoxelVolumetricFog m_VolumetricVolume;
        private Material m_godrayMaterial;
        private Texture2D ditherTex;
        private RTHandle source;
        private RTHandle dest;
        private RTHandle destTempRT;
        private RTHandle blur01;
        private RTHandle blur02;
        private RTHandle quarterResDepthBuffer;
        private RenderTextureDescriptor descriptor;
        public Shader godRayShader;
        
        public GodRayPass()
        {
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_godrayMaterial == null)
            {
                return;
            }

            if (!renderingData.cameraData.postProcessEnabled)
            {
                return;
            }

            var stack = VolumeManager.instance.stack;                                                // 获取所有继承Volume框架的脚本。
            m_VolumetricVolume = stack.GetComponent<VoxelVolumetricFog>();                           // 查找对应的属性参数组件
            var cmd = CommandBufferPool.Get(godrayPassIdentifier);                 // 从命令缓冲区池中获取一个带标签的命令缓冲区，该标签名可以在后续帧调试器中见到

            Render(cmd, ref renderingData);                                                          // 调用渲染函数

            context.ExecuteCommandBuffer(cmd);                                                       // 执行命令缓冲区
            CommandBufferPool.Release(cmd);                                                          // 释放缓存区
        } 
        
        void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (m_VolumetricVolume.godrayIntensity.value==0)
            {
                return;
            }
            // 判断组件是否开启，且非Scene视图摄像机    因为如果不开启非Scene视图 场景也会爆。
            if (m_VolumetricVolume.IsActive() && !renderingData.cameraData.isSceneViewCamera)
            { 
                SetMatData(renderingData.cameraData.camera);
                //深度降采样 1/4分辨率
                Blitter.BlitCameraTexture(cmd, source, quarterResDepthBuffer, m_godrayMaterial, 3);
                cmd.SetGlobalTexture("_QuarterResDepthBuffer",quarterResDepthBuffer);
                
                //GodRay步进
                Blitter.BlitCameraTexture(cmd, source,blur01,m_godrayMaterial,0);
                
                //双边滤波模糊
                Blitter.BlitCameraTexture(cmd,blur01, blur02, m_godrayMaterial,2);

                //升采样
                Blitter.BlitCameraTexture(cmd, blur02, destTempRT, m_godrayMaterial, 4);

                //合并
                cmd.SetGlobalTexture("_FinalTex",destTempRT);
                Blitter.BlitCameraTexture(cmd, source, dest, m_godrayMaterial, 1);
                Blitter.BlitCameraTexture(cmd, dest, source);
            }
        }
        
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigureTarget(source);
            descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = 0;
            descriptor.colorFormat = RenderTextureFormat.DefaultHDR;
            RenderingUtils.ReAllocateIfNeeded(ref dest, descriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "GodRayDest");
            RenderingUtils.ReAllocateIfNeeded(ref destTempRT,descriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "GodRayDestTemp");
            
            descriptor.width = Mathf.Max(1, descriptor.width/4);
            descriptor.height = Mathf.Max(1, descriptor.height/4);
            RenderingUtils.ReAllocateIfNeeded(ref quarterResDepthBuffer, descriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "GodRayQuaterDepth");
            RenderingUtils.ReAllocateIfNeeded(ref blur01, descriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "GodRayBlur01");
            RenderingUtils.ReAllocateIfNeeded(ref blur02, descriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "GodRayBlur02");
        }
        
        public void Dispose()
        {
            dest?.Release();
            destTempRT?.Release();
            blur01?.Release();
            blur02?.Release();
            quarterResDepthBuffer?.Release();
        }
        
        public void SetTarget(RTHandle colorHandle)
        {
            source = colorHandle;
        }

        internal bool SetupMaterials(Settings settings)
        {
            // var shader = ShaderFindUtil.FindShader(godrayShaderPath);
            var shader = godRayShader;
            ditherTex = settings.DitherTex;
            if (shader == null)
            {
                return false;
            }
            if (m_godrayMaterial == null)
            {
                m_godrayMaterial = CoreUtils.CreateEngineMaterial(shader);
            }
            if (m_godrayMaterial) 
                return true;
            else
                return false;
        }
        
        void SetMatData(Camera cam)
        {
            m_godrayMaterial.SetFloat("_MaxRayLength", m_VolumetricVolume.godrayLength.value);
            m_godrayMaterial.SetColor("_GodRayColor", m_VolumetricVolume.godrayColor.value);
            m_godrayMaterial.SetFloat("_DensityCoef", m_VolumetricVolume.godrayIntensity.value);
            m_godrayMaterial.SetFloat("_MaxStep", m_VolumetricVolume.maxStep.value);
            m_godrayMaterial.SetFloat("_ExtinctionCoef", m_VolumetricVolume.extinctionCoef.value);
            m_godrayMaterial.SetFloat("_SkyBoxExtinctionCoef", 1-m_VolumetricVolume.skyBoxExtinction.value);
            m_godrayMaterial.SetVector("_MieG", new Vector4(1 - (m_VolumetricVolume.godrayMieG.value * m_VolumetricVolume.godrayMieG.value), 1 + (m_VolumetricVolume.godrayMieG.value * m_VolumetricVolume.godrayMieG.value), 2 * m_VolumetricVolume.godrayMieG.value, 1.0f / (4.0f * Mathf.PI)));
            var renderSize = new Vector2Int(cam.pixelWidth, cam.pixelHeight);
            m_godrayMaterial.SetVector("_FrameSize", new Vector4(renderSize.x, renderSize.y, 1f / renderSize.x, 1f / renderSize.y));
            m_godrayMaterial.SetFloat("_Space_Sigma", m_VolumetricVolume.Space_S.value);
            m_godrayMaterial.SetFloat("_Range_Sigma", m_VolumetricVolume.Space_R.value);
            m_godrayMaterial.SetFloat("_KernelSize", 5);

            m_godrayMaterial.SetTexture("_DitherTex", ditherTex);
        }
    }
    
    //体素体积雾Pass
    class RenderFogPass : ScriptableRenderPass
    {
        private RTHandle source;
        private RenderTextureDescriptor descriptor;
        
        private RTHandle isolatedBlitTarget;

        private VoxelVolumetricFog volumetricFog;
        private Material upscaleMaterial;
        private const string isolatedBlitPath = "ShiYue/URP/VoxelVolumetric/IsolatedBlit";
        private const string vresTextureId = "_VBufferResTexture";
        private const string passIdentifier = "VoxelVolumetricPass";
        public Shader isolateBlitShader;
        
        private RenderTextureDescriptor GetVolumeDescriptor(Vector3Int volumeSize)
        {
            RenderTextureDescriptor volumeDescriptor = new RenderTextureDescriptor();
            volumeDescriptor.dimension = TextureDimension.Tex3D;
            volumeDescriptor.width = volumeSize.x;
            volumeDescriptor.height = volumeSize.y;
            volumeDescriptor.volumeDepth = volumeSize.z;
            volumeDescriptor.colorFormat = RenderTextureFormat.ARGBHalf;
            volumeDescriptor.enableRandomWrite = true;
            volumeDescriptor.msaaSamples = 1;
            return volumeDescriptor;
        }

        //所用材质及数据初始化
        internal bool SetupMaterials(Camera c, Settings settings)
        {
            if (volumetricFog.fogDensity.value==0)
            {
                return false;
            }
            GetShaderAndSetupMaterial(isolateBlitShader, ref upscaleMaterial);
            if (!upscaleMaterial)
            {
                return false;
            }

            mediaCompute = settings.mediaCompute;           //构建介质Voxel
            lightingCompute = settings.lightingCompute;     //构建光照
            integratorCompute = settings.integratorCompute; //算最终效果

            if (!mediaCompute || !integratorCompute || !lightingCompute)
            {
                return false;
            }

            //存储VoxelRT尺寸
            Vector3Int volumeSize = volumetricFog.GetVolumeSize(new Vector2Int(c.pixelWidth, c.pixelHeight));
            Vector3Int storedVolumeSize = new Vector3Int(160,90,64);
            
            if (kvps.TryGetValue(c, out RTData rt))
            {
                storedVolumeSize = rt.volumeSize;
            }
            else
            {
                rt = new RTData();
                kvps.Add(c, rt);
            }
            rt.refreshTextures = false;
            if (volumeSize != storedVolumeSize || rt.lightingDataRtPrev == null)
            {
                rt.refreshTextures = true;
            }

            mediaCompute.GetKernelThreadGroupSizes(0, out uint threadGroupSizeX, out uint threadGroupSizeY, out uint threadGroupSizeZ);

            groupsX = GetGroupCount(volumeSize.x, threadGroupSizeX);
            groupsY = GetGroupCount(volumeSize.y, threadGroupSizeY);
            groupsZ = GetGroupCount(volumeSize.z, threadGroupSizeZ);

            rt.time = Time.realtimeSinceStartup;

            rt.volumeSize = volumeSize;
            rt.cellSize = volumetricFog.GetCellSize(volumeSize);
            return true;
        }

        void GetShaderAndSetupMaterial(Shader pShader, ref Material target)
        {
            if (target != null)
                return;

            Shader s = pShader;
            if (s != null)
            {
                target = CoreUtils.CreateEngineMaterial(s);
            }
            else
            {
                Debug.Log("VoxelVolume missing shader reference at " + pShader.name);
            }
        }

        //相机及RT数据结构，用于TAART及Voxel尺寸设置
        Dictionary<Camera, RTData> kvps = new Dictionary<Camera, RTData>();
        private class RTData
        {
            public RenderTexture lightingDataRtPrev;
            public RenderTexture mediaDataRtPrev;
            public float time;
            public Matrix4x4 previousInverseTransform;
            public Vector3Int volumeSize;
            public Vector3 cellSize;
            public bool refreshTextures;
        }
        List<Camera> removeList = new List<Camera>();
        
        public void CleanupDictionary()
        {
            if (removeList.Count > 0)
                removeList.Clear();

            foreach (KeyValuePair<Camera, RTData> kvp in kvps)
            {
                if (kvp.Value.time - Time.realtimeSinceStartup > 0.5f)
                {
                    removeList.Add(kvp.Key);
                }
            }

            foreach (Camera cam in removeList)
            {
                kvps.Remove(cam);
            }
        }

        ComputeShader mediaCompute;
        ComputeShader lightingCompute;
        ComputeShader integratorCompute;
        int groupsX;
        int groupsY;
        int groupsZ;

        private int GetGroupCount(int textureDimension, uint groupSize)
        {
            return Mathf.CeilToInt((textureDimension + groupSize - 1) / Mathf.Max(groupSize,1));
        }
        
        RTHandle mediaTarget;
        RTHandle lightingTarget;
        RTHandle integratorTarget;

        public RenderFogPass()
        {
            isolatedBlitTarget = RTHandles.Alloc(Shader.PropertyToID(vresTextureId), name: vresTextureId);
            mediaTarget = RTHandles.Alloc("_MediaTarget", name: "_MediaTarget");
            lightingTarget = RTHandles.Alloc("_LightingTarget", name: "_LightingTarget");
            integratorTarget = RTHandles.Alloc("_IntegratorTarget", name: "_IntegratorTarget");
        }

        public bool RegisterStackComponent()
        {
            volumetricFog = VolumeManager.instance.stack.GetComponent<VoxelVolumetricFog>();

            if (volumetricFog == null)
                return false;

            return volumetricFog.IsActive();
        }

        public void SetTarget(RTHandle colorHandle)
        {
            source = colorHandle;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigureTarget(source);
            descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = 0;
            descriptor.colorFormat = RenderTextureFormat.DefaultHDR;
            descriptor.width = Mathf.Max(1, descriptor.width);
            descriptor.height = Mathf.Max(1, descriptor.height);
            RenderingUtils.ReAllocateIfNeeded(ref isolatedBlitTarget, descriptor, FilterMode.Point, TextureWrapMode.Clamp, name: vresTextureId);
            
            Vector3Int volumeSize = volumetricFog.GetVolumeSize(new Vector2Int(renderingData.cameraData.camera.pixelWidth, renderingData.cameraData.camera.pixelHeight));
            RenderTextureDescriptor volumeDescriptor = GetVolumeDescriptor(volumeSize);
            RenderingUtils.ReAllocateIfNeeded(ref mediaTarget, volumeDescriptor);
            RenderingUtils.ReAllocateIfNeeded(ref lightingTarget, volumeDescriptor);
            RenderingUtils.ReAllocateIfNeeded(ref integratorTarget, volumeDescriptor);
        }

        //Local Volume 数据结构
        Matrix4x4[] localFogMatrix = new Matrix4x4[VoxelVolumCommon._MAXVOLUMECOUNT];
        Vector4[] localAlbedo = new Vector4[VoxelVolumCommon._MAXVOLUMECOUNT];
        float[] intensity = new float[VoxelVolumCommon._MAXVOLUMECOUNT];
        float[] blendMode = new float[VoxelVolumCommon._MAXVOLUMECOUNT];
        float[] blendDistance = new float[VoxelVolumCommon._MAXVOLUMECOUNT];

        //Additional Light 数据结构
        Vector4[] lightPosition = new Vector4[VoxelVolumCommon._MAXLIGHTCOUNT];
        float[] lightStrength = new float[VoxelVolumCommon._MAXLIGHTCOUNT];
        Vector4[] color = new Vector4[VoxelVolumCommon._MAXLIGHTCOUNT];
        Vector4[] direction = new Vector4[VoxelVolumCommon._MAXLIGHTCOUNT];
        Vector4[] angle = new Vector4[VoxelVolumCommon._MAXLIGHTCOUNT];

        Vector4[] simpleColorSet = new Vector4[3];
        Matrix4x4 toPreviousView = Matrix4x4.identity;
        Vector3[] near = new Vector3[3];
        Vector3[] far = new Vector3[3];

        //相机及材质参数初始化
        private void SetupCameraProperties(CommandBuffer cmd, Camera camera)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Voxel_Setup Camera Properties");
            
            //计算远近裁面
            float nearClip = camera.nearClipPlane;
            float farClip = camera.farClipPlane;
            near[0] = camera.ViewportToWorldPoint(new Vector3(0, 0, nearClip));
            near[1] = camera.ViewportToWorldPoint(new Vector3(1, 0, nearClip));
            near[2] = camera.ViewportToWorldPoint(new Vector3(0, 1, nearClip));
            far[0] = camera.ViewportToWorldPoint(new Vector3(0, 0, farClip));
            far[1] = camera.ViewportToWorldPoint(new Vector3(1, 0, farClip));
            far[2] = camera.ViewportToWorldPoint(new Vector3(0, 1, farClip));

            Vector2 frustumNearSize = new Vector2(Vector3.Distance(near[1], near[0]), Vector3.Distance(near[2], near[0])) * 0.5f;
            Vector2 frustumFarSize = new Vector2(Vector3.Distance(far[1], far[0]), Vector3.Distance(far[2], far[0])) * 0.5f;
            
            float fog_distance = volumetricFog.maxDistanceVolumetric.value;
            Vector2 fogFarSize = Vector2.Lerp(frustumNearSize, frustumFarSize, (fog_distance - nearClip) / (farClip - nearClip));
            Vector2 fogNearSize = Vector2.zero;
            if (camera.orthographic)
            {
                fogNearSize = fogFarSize;
            }
            
            toPreviousView = kvps[camera].previousInverseTransform * camera.transform.localToWorldMatrix;
            
            cmd.SetGlobalVector(
                Params.fog_volume_size.Id,
                new Vector4(kvps[camera].volumeSize.x, kvps[camera].volumeSize.y, kvps[camera].volumeSize.z, 0)
            );
            cmd.SetGlobalVector(Params.fog_cell_size.Id, kvps[camera].cellSize);
            cmd.SetGlobalVector(Params.fogNearSize.Id, fogNearSize);
            cmd.SetGlobalVector(Params.fogFarSize.Id, fogFarSize);
            cmd.SetGlobalFloat(Params.cameraFarPlane.Id, camera.farClipPlane);
            cmd.SetGlobalFloat(Params._depth_ratio.Id, volumetricFog.depthRatio.value);
            cmd.SetGlobalMatrix(Params.toPreviousView.Id, toPreviousView);
            cmd.SetGlobalMatrix(Params.os_CameraInvProjection.Id, camera.nonJitteredProjectionMatrix.inverse);
            cmd.SetGlobalMatrix(Params.os_CameraToWorld.Id, camera.cameraToWorldMatrix);
            cmd.SetGlobalMatrix(Params.os_WorldToCamera.Id, camera.worldToCameraMatrix);
            cmd.SetGlobalVector(Params.os_WorldSpaceCameraPosition.Id, camera.transform.position);

            // Set Previous Inverse Transform
            kvps[camera].previousInverseTransform = camera.transform.worldToLocalMatrix;

            // Finish Profiler
            UnityEngine.Profiling.Profiler.EndSample();
        }

        Camera camera;

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            UnityEngine.Profiling.Profiler.BeginSample(passIdentifier);
            CommandBuffer cmd = CommandBufferPool.Get(passIdentifier);
            camera = renderingData.cameraData.camera;

            SetupData(cmd, camera);

            // ComputeShader介质信息构建
            cmd.SetGlobalTexture(Params._ScreenTexture.Id, source.nameID);
            cmd.SetGlobalTexture(Params.MediaData.Id, mediaTarget);
            cmd.DispatchCompute(mediaCompute, 0, groupsX, groupsY, groupsZ);

            // ComputeShader光照计算
            cmd.SetGlobalTexture(Params.LightingDataTex.Id, lightingTarget);
            cmd.DispatchCompute(lightingCompute, 0, groupsX, groupsY, groupsZ);

            // ComputeShader计算结果写入VBuffer 
            cmd.SetGlobalTexture(Params.IntegratorData.Id, integratorTarget);
            cmd.DispatchCompute(integratorCompute, 0, groupsX, groupsY, 1);

            // VBuffer结果写入RT
            Blitter.BlitCameraTexture(cmd, source, isolatedBlitTarget, upscaleMaterial, 0);
            
            // 写入屏幕
            Blitter.BlitCameraTexture(cmd, isolatedBlitTarget, source); 

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
            UnityEngine.Profiling.Profiler.EndSample();
        }

        public void Dispose()
        {
            mediaTarget?.Release();
            lightingTarget?.Release();
            integratorTarget?.Release();
            isolatedBlitTarget?.Release();

            mediaTarget = null;
            lightingTarget = null;
            integratorTarget = null;
            isolatedBlitTarget = null;
            
            kvps.Clear();
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            CleanupDictionary();
        }

        //数据传入
        private void SetupData(CommandBuffer cmd, Camera camera)
        {
            SetupCameraProperties(cmd, camera);
            SetupSphericalHarmonics(cmd);
            SetupMaterialData(cmd, camera);
            SetupAdditionalLightData(cmd, camera);
            SetupVolumeData(cmd, camera);
        }
        
        private Texture3D whiteTexture;
        private Texture3D WhiteTexture
        {
            get
            {
                if (whiteTexture == null)
                {
                    whiteTexture = new Texture3D(1, 1, 1, TextureFormat.RGBA32, false);
                    whiteTexture.SetPixel(0, 0, 0, Color.white);
                    whiteTexture.Apply();
                }
                return whiteTexture;
            }
        }
        //传入材质数据
        private void SetupMaterialData(CommandBuffer cmd, Camera cam)
        {
            UnityEngine.Profiling.Profiler.BeginSample("VoxelVolume_Setup Material Data");

            cmd.SetGlobalFloat(Params.MaxDistanceVolumetric.Id, volumetricFog.maxDistanceVolumetric.value);
            float inverseMaxDistance = Mathf.Max(1.0f / volumetricFog.maxDistanceVolumetric.value, 1e-7f);
            float inverseDepthRatio = 1.0f / volumetricFog.depthRatio.value;
            cmd.SetGlobalFloat("_InverseMaxDistanceVolumetric", inverseMaxDistance);
            cmd.SetGlobalFloat("_inverse_depth_ratio", inverseDepthRatio);

            cmd.SetGlobalFloat(
                "_InverseLightDistanceVolumetric",
                volumetricFog.overrideDefaultMaxLightDistance.value
                    ? Mathf.Max(1.0f / volumetricFog.maxLightDistance.value, 1e-7f)
                    : inverseMaxDistance
            );
            
            cmd.SetGlobalVector("_WindOffsetVoxel", volumetricFog.noiseWindSpeed.value);
            cmd.SetGlobalFloat(Params.Anisotropy.Id, volumetricFog.anisotropy.value);
            cmd.SetGlobalFloat(Params.BaseHeight.Id, volumetricFog.baseHeight.value);
            cmd.SetGlobalFloat(Params.AttenuationBoundarySize.Id, volumetricFog.attenuationBoundarySize.value);

            float density = volumetricFog.fogDensity.value * 0.01f;
            float globalExtinction = 1 / volumetricFog.fogAttenuation.value;
            Vector4 globalScatteringAndExtinction = volumetricFog.fogAlbedo.value;
            globalScatteringAndExtinction.w = globalExtinction;
            
            cmd.SetGlobalVector("_GlobalScatteringAndExtinction",globalScatteringAndExtinction);
            cmd.SetGlobalFloat(Params.FogDensity.Id, density);
            cmd.SetGlobalFloat(Params.LightIntensity.Id, volumetricFog.lightIntensity.value);
            cmd.SetGlobalFloat(Params.ShadowIntensity.Id, volumetricFog.shadowIntensity.value);

            
            simpleColorSet[0] = (Vector4)volumetricFog.litColor.value;
            simpleColorSet[1] = (Vector4)volumetricFog.shadowedColor.value;
            simpleColorSet[2] = (Vector4)volumetricFog.emitColor.value;
            cmd.SetGlobalVectorArray(Params.SimpleColor.Id, simpleColorSet);
            cmd.SetGlobalFloat(Params.ColorInfluence.Id, volumetricFog.colorInfluence.value);
            cmd.SetGlobalFloat(Params._Inverse_AttenuationBoundarySize.Id, Mathf.Max(1.0f / volumetricFog.attenuationBoundarySize.value, 1e-7f));
            
            //全局噪声
            if (volumetricFog.useNoise.value)
            {
                var ntexture = volumetricFog.noiseTexture.value;
            
                cmd.SetGlobalTexture(Params.NoiseTexture.Id, ntexture == null ? WhiteTexture :ntexture);   
                cmd.SetGlobalFloat(Params.NoiseTiling.Id, ntexture == null ? 0 : volumetricFog.noiseTiling.value);
                cmd.SetGlobalFloat(Params._InverseNoiseScale.Id, Mathf.Max(1.0f / volumetricFog.noiseTiling.value, 1e-7f));
                
                cmd.SetGlobalVector(Params.NoiseWindSpeed.Id, volumetricFog.noiseWindSpeed.value );
                cmd.SetGlobalFloat(Params.NoiseIntensityMin.Id, volumetricFog.noiseReMap.value.x);
                cmd.SetGlobalFloat(Params.NoiseIntensityMax.Id, volumetricFog.noiseReMap.value.y);
            }
            else
            {
                cmd.SetGlobalFloat(Params.NoiseTiling.Id, 0);
                cmd.SetGlobalTexture(Params.NoiseTexture.Id, WhiteTexture);
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }
        
        //传入附加光源数据
        void SetupAdditionalLightData(CommandBuffer cmd, Camera camera)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Voxel_Setup Additional Light Data");

            int lightCount = VoxelVolumeLight.Lights.Count;
            cmd.SetGlobalInt(Params.LightCountVoxel.Id, lightCount);

            if (lightCount > 0)
            {
                if (lightCount > VoxelVolumCommon._MAXLIGHTCOUNT)
                {
                    VoxelVolumeLight.SortByDistance(camera.transform.position);
                    lightCount = Mathf.Min(VoxelVolumeLight.Lights.Count, VoxelVolumCommon._MAXLIGHTCOUNT);
                }

                for (int i = 0; i < lightCount; i++)
                {
                    lightPosition[i] = VoxelVolumeLight.Lights[i].LightPosition;
                    lightStrength[i] = VoxelVolumeLight.Lights[i].LightIntensity;
                    color[i] = VoxelVolumeLight.Lights[i].LightColor;
                    direction[i] = VoxelVolumeLight.Lights[i].LightDirection;
                    angle[i] = VoxelVolumeLight.Lights[i].LightAngleData.GetAngleData();
                }

                cmd.SetGlobalVectorArray(Params.LightPosVoxel.Id, lightPosition);
                cmd.SetGlobalFloatArray(Params.LitIntensityVoxel.Id, lightStrength);
                cmd.SetGlobalVectorArray(Params.LightColorVoxel.Id, color);
                cmd.SetGlobalVectorArray(Params.LightDirectionVoxel.Id, direction);
                cmd.SetGlobalVectorArray(Params.LightAngleVoxel.Id, angle);
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }
        
        //传入LocalVolume数据
        void SetupVolumeData(CommandBuffer cmd, Camera camera)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Voxel_Setup Volume Data");

            int volumeCount = LocalVolumetricFog.FogVolumes.Count;
            cmd.SetGlobalInt(Params.LocalVolumeCount.Id, volumeCount);

            if (volumeCount > 0)
            {
                if (volumeCount > VoxelVolumCommon._MAXVOLUMECOUNT)
                {
                    LocalVolumetricFog.SortByDistance(camera.transform.position);
                    volumeCount = Mathf.Min(LocalVolumetricFog.FogVolumes.Count, VoxelVolumCommon._MAXVOLUMECOUNT);
                }

                for (int i = 0; i < volumeCount; i++)
                {
                    //volumePosition[i] = (Vector4)LocalVolumetricFog.FogVolumes[i].transform.position;
                    localFogMatrix[i] = LocalVolumetricFog.FogVolumes[i].GetLocalFogMatrix();
                    intensity[i] = LocalVolumetricFog.FogVolumes[i].DensityMultiplier;
                    localAlbedo[i] = LocalVolumetricFog.FogVolumes[i].Albedo;
                    blendMode[i] = (int)LocalVolumetricFog.FogVolumes[i].Mode;
                    blendDistance[i] = LocalVolumetricFog.FogVolumes[i].BlendDistance;
                }
                cmd.SetComputeMatrixArrayParam(mediaCompute,"_LocalFogMatrix", localFogMatrix);
                //cmd.SetGlobalMatrixArray("_LocalFogMatrix", localFogMatrix);
                //cmd.SetGlobalVectorArray(Params.VolumePosition.Id, volumePosition);
                cmd.SetGlobalFloatArray(Params.VolumeIntensity.Id, intensity);
                cmd.SetGlobalVectorArray(Params.VolumeAlbedo.Id, localAlbedo);
                cmd.SetGlobalFloatArray(Params.VolumeBlendMode.Id, blendMode);
                cmd.SetGlobalFloatArray(Params.VolumeBlendDistance.Id, blendDistance);
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }

        //传入环境光数据
        private void SetupSphericalHarmonics(CommandBuffer cmd)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Voxel_Setup Spherical Harmonics");
            SphericalHarmonicsL2 sh2 = RenderSettings.ambientProbe;
            Color ambient = new Color(sh2[0, 0] - sh2[0, 6], sh2[1, 0] - sh2[1, 6], sh2[2, 0] - sh2[2, 6]);
            cmd.SetGlobalColor(Params.WorldColor.Id, ambient);
            UnityEngine.Profiling.Profiler.EndSample();
        }
    }

    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
                
        public ComputeShader mediaCompute;
        public ComputeShader lightingCompute;
        public ComputeShader integratorCompute;
        public Texture2D DitherTex;
        
        public Shader godRayShader;
        public Shader isolateBlitShader;
    }

    public Settings settings = new Settings();

    RenderFogPass renderFogPass;
    GodRayPass godRayPass;
    
    public override void Create()
    {
        renderFogPass = new RenderFogPass();
        renderFogPass.renderPassEvent = settings.renderPassEvent;
        renderFogPass.isolateBlitShader = settings.isolateBlitShader;

        godRayPass = new GodRayPass();
        godRayPass.renderPassEvent = settings.renderPassEvent;
        godRayPass.godRayShader = settings.godRayShader;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.camera.cameraType == CameraType.Reflection)
                return;

#if UNITY_EDITOR
            bool isSceneCamera = renderingData.cameraData.camera.cameraType == CameraType.SceneView;
            if (isSceneCamera)
            {
                bool fogEnabled = UnityEditor.SceneView.currentDrawingSceneView.sceneViewState.fogEnabled;
                bool isDrawingTextured = UnityEditor.SceneView.currentDrawingSceneView.cameraMode.drawMode == UnityEditor.DrawCameraMode.Textured;
                if (!fogEnabled || !isDrawingTextured)
                    return;
            }

            if (renderingData.cameraData.camera.cameraType == CameraType.Preview)
                return;
#endif

            if (!renderingData.cameraData.postProcessEnabled)
                return;

            if (!renderFogPass.RegisterStackComponent())
                return;
            
            bool shouldAdd = renderFogPass.SetupMaterials(renderingData.cameraData.camera, settings);
            bool shouldAddGodRay = godRayPass.SetupMaterials(settings);
            
            if (shouldAddGodRay)
            {
                renderer.EnqueuePass(godRayPass);
            }
            if (shouldAdd)
            {
                renderer.EnqueuePass(renderFogPass);
            }
    }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            renderFogPass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Motion);
            renderFogPass.SetTarget(renderer.cameraColorTargetHandle);
            godRayPass.ConfigureInput(ScriptableRenderPassInput.Color);
            godRayPass.SetTarget(renderer.cameraColorTargetHandle);
        }

        protected override void Dispose(bool disposing) 
        { 
            renderFogPass?.Dispose();
            renderFogPass = null;
            godRayPass?.Dispose();
            godRayPass = null;
            base.Dispose(disposing);
        }

        //Shader参数
        private static class Params
        {
            public readonly struct Param
            {
                public Param(string property)
                {
                    Property = property;
                    Id = Shader.PropertyToID(property);
                }

                readonly public string Property;
                readonly public int Id;
            }

            public static Param MediaData = new Param("MediaData");
            public static Param LightingDataTex = new Param("LightingDataTex");
            public static Param IntegratorData = new Param("_VBufferLighting");
            public static Param _ScreenTexture = new Param("_ScreenTexture");

            public static Param MaxDistanceVolumetric = new Param("_MaxDistanceVolumetric");

            public static Param FogDensity = new Param("_FogDensity");
            public static Param Anisotropy = new Param("_Anisotropy");
            public static Param LightIntensity = new Param("_LitIntensityVoxel");
            public static Param ShadowIntensity = new Param("_ShadowIntensityVoxel");

            public static Param BaseHeight = new Param("_BaseHeight");
            public static Param AttenuationBoundarySize = new Param("_AttenuationBoundarySize");
            
            public static Param SimpleColor = new Param("_SimpleColor");
            public static Param ColorInfluence = new Param("_ColorInfluence");

            //Noise
            public static Param NoiseTexture = new Param("_VoxelNoiseTexture");
            public static Param NoiseTiling = new Param("_VoxelNoiseTiling");
            public static Param NoiseWindSpeed = new Param("_NoiseWindSpeed");
            public static Param NoiseIntensityMin = new Param("_NoiseIntensityMin");
            public static Param NoiseIntensityMax = new Param("_NoiseIntensityMax");

            //Additional Light
            public static Param LightCountVoxel = new Param("_LightCountVoxel");
            public static Param LightPosVoxel = new Param("_LightPosVoxel");
            public static Param LitIntensityVoxel = new Param("_AddLightIntensityVoxel");
            public static Param LightColorVoxel = new Param("_LightColorVoxel");
            public static Param LightDirectionVoxel = new Param("_LightDirectionVoxel");
            public static Param LightAngleVoxel = new Param("_LightAngleVoxel");

            //Local Volume Data 
            public static Param LocalVolumeCount = new Param("_LocalVolumeCount"); 
            public static Param VolumeAlbedo = new Param("_LocalVolumeAlbedo");
            public static Param VolumeIntensity = new Param("_LocalVolumeIntensity");
            public static Param VolumeBlendMode = new Param("_VolumeBlendMode");
            public static Param VolumeBlendDistance = new Param("_VolumeBlendDistance");
            
            // Ambient Lighting
            public static Param WorldColor = new Param("_WorldColor");

            public static Param fog_volume_size = new Param("fog_volume_size");
            public static Param fog_cell_size = new Param("fog_cell_size");
            public static Param fogNearSize = new Param("fogNearSize");
            public static Param fogFarSize = new Param("fogFarSize");
            public static Param cameraFarPlane = new Param("cameraFarPlane");
            public static Param _depth_ratio = new Param("_depth_ratio");
            public static Param toPreviousView = new Param("toPreviousView");
            public static Param os_CameraInvProjection = new Param("os_CameraInvProjection");
            public static Param os_CameraToWorld = new Param("os_CameraToWorld");
            public static Param os_WorldToCamera = new Param("os_WorldToCamera");
            public static Param os_WorldSpaceCameraPosition = new Param("os_WorldSpaceCameraPosition");

            public static Param _InverseNoiseScale = new Param("_InverseNoiseScale");
            public static Param _Inverse_AttenuationBoundarySize = new Param("_Inverse_AttenuationBoundarySize");
        }
}
