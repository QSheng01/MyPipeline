using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace WaterScreenSpacePlanarReflection{
    public class SSPRTexGenerator
    {
        ComputeShader m_CS;
        public Material _blurMaterial;

        private int m_KernelClear;
        private int m_KernelDrawReflectionTex;
        private int m_KernelFillHoles;
        private int m_KernelGenResult;



        /// <summary>
        /// 在生成反射贴图的时候，是否剔除掉无穷远的像素(例如天空盒)
        /// </summary>
        private bool _excludeBackground = false;

        private bool _isCSLoadTried = false;
        private int _quality = 1;
        private bool _fillHoles = false;

        public float fadeOutScreenBorderWidthVertical;
        public float fadeOutScreenBorderWidthHorizontal;

        RenderTextureDescriptor m_RenderTextureBufferDescriptor;

        Vector2Int m_ResultSize;
        Vector2Int m_ThreadSize;

        public SSPRTexGenerator()
        {
            m_ResultSize = new Vector2Int(512, 512);
            m_ThreadSize = new Vector2Int(1, 1);

            TryLoadCSIfNot();
        }

        void BindCS(ComputeShader cp)
        {
            m_CS = cp;
            this.UpdateKernelIndex();
        }

        private void UpdateKernelIndex()
        {
            try
            {
                m_KernelClear = m_CS.FindKernel("CSClear");
                m_KernelDrawReflectionTex = m_CS.FindKernel("DrawReflectionTex1");
                m_KernelGenResult = m_CS.FindKernel("GenResult");
                if (_excludeBackground)
                {
                    m_KernelDrawReflectionTex += 1;
                }

                m_KernelFillHoles = m_CS.FindKernel("FillHoles");
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                Debug.LogError("为啥能加载出shader，却Find不到Kernel？");
            }
        }

        public bool excludeBackground
        {
            get { return _excludeBackground; }
            set
            {
                _excludeBackground = value;
                if (m_CS)
                {
                    this.UpdateKernelIndex();
                }
            }
        }

        public bool fillHoles
        {
            get { return _fillHoles; }
            set
            {
                _fillHoles = value;
                // if(_computeShader){
                //     this.UpdateKernelIndex();
                // }
            }
        }

        public int quality
        {
            get { return _quality; }
            set { _quality = value; }
        }


        private void TryLoadCSIfNot()
        {
            // TODO: Give the water shader a variant without SSPR reflection
            if (!SystemInfo.supportsComputeShaders)
                return;
            if (m_CS)
                return;
            if (_isCSLoadTried)
                return;

            _isCSLoadTried = true;
            //#if UNITY_EDITOR
            m_CS = //UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Shader/Post/WaterSSPR/Resources/ReflectionTexCompute.compute");
                Resources.Load<ComputeShader>("ReflectionTexCompute");
            //#endif
            if (!m_CS)
            {
                Debug.LogWarning("missing compute shader");
                return;
            }

            this.BindCS(m_CS);
        }

        public bool IsSupported()
        {
            return m_CS;
        }

        public void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            float downScale = 1f / quality;
            var width = (int)(downScale * cameraTextureDescriptor.width);
            var height = (int)(downScale * cameraTextureDescriptor.height);
            m_ResultSize = new Vector2Int(width, height);

            // DrawReflectionTex1的输出贴图
            var descriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 0, 0);
            descriptor.enableRandomWrite = true;
            cmd.GetTemporaryRT(s_ResultID, descriptor);

            // 创建一个 Int 图
            descriptor.colorFormat = RenderTextureFormat.RInt;
            cmd.GetTemporaryRT(s_IntResultID, descriptor);

            if (_blurMaterial)
            {
                //降低分辨率提升模糊效果并节省性能
                width = m_ResultSize.x >> 1;
                height = m_ResultSize.y >> 1;
                int width2 = width >> 1;
                int height2 = height >> 1;

                var format = RenderTextureFormat.ARGB32;
                cmd.GetTemporaryRT(s_DualKawaseBlurBig, width, height, 0, FilterMode.Bilinear, format);
                cmd.GetTemporaryRT(s_DualKawaseBlurSmall0, width2, height2, 0, FilterMode.Bilinear, format);
                cmd.GetTemporaryRT(s_DualKawaseBlurSmall1, width2, height2, 0, FilterMode.Bilinear, format);
            }
        }

        void ExecuteClear(CommandBuffer cmd)
        {
            const int numThreadsX = 32;
            cmd.SetComputeTextureParam(m_CS, m_KernelClear, s_IntResultID, s_IntResultID);
            cmd.SetComputeIntParams(m_CS, s_ResultSizeID, m_ResultSize.x, m_ResultSize.y);
            m_ThreadSize.x = (m_ResultSize.x + numThreadsX - 1) / numThreadsX;
            m_ThreadSize.y = (m_ResultSize.y + numThreadsX - 1) / numThreadsX;
            cmd.DispatchCompute(m_CS, m_KernelClear, m_ThreadSize.x, m_ThreadSize.y, 1);
        }

        void ExecuteGenResult(CommandBuffer cmd, ref RenderingData renderingData, ref PlanarDescriptor planarDescriptor)
        {
            var cameraData = renderingData.cameraData;
            var viewMatrix = cameraData.camera.worldToCameraMatrix;
            //不知道为什么，第二个参数是false才能正常得到世界坐标
            var projectMatrix = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(), true);
            var matrixVP = projectMatrix * viewMatrix;
            var invMatrixVP = matrixVP.inverse;

            cmd.SetComputeMatrixParam(m_CS, s_MatrixVP, matrixVP);
            cmd.SetComputeMatrixParam(m_CS, s_MatrixInvVP, invMatrixVP);
            cmd.SetComputeVectorParam(m_CS, s_PlanarPositionID, planarDescriptor.position);
            cmd.SetComputeVectorParam(m_CS, s_PlanarNormalID, planarDescriptor.normal);

            cmd.SetComputeVectorParam(m_CS, "_ResultTex_TexelSize",
                new Vector4(1.0f / m_ResultSize.x, 1.0f / m_ResultSize.y));
            // 平面信息的W值
            float planarPositionW = -Vector3.Dot(planarDescriptor.normal, planarDescriptor.position);
            cmd.SetComputeFloatParam(m_CS, "_PlanarPositionW", planarPositionW);

            // 生效计算的 kernal 默认是 1，开EdgeStretch是 2
            cmd.SetComputeTextureParam(m_CS, m_KernelGenResult, s_IntResultID, s_IntResultID);
            cmd.DispatchCompute(m_CS, m_KernelGenResult, m_ThreadSize.x, m_ThreadSize.y, 1);
        }

        void ExecuteDrawReflectionTex(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // 主图转换读取 int 图进行转换
            RenderTargetIdentifier cameraColorTex = renderingData.cameraData.renderer.cameraColorTargetHandle;
            cmd.SetComputeTextureParam(m_CS, m_KernelDrawReflectionTex, s_CameraColorTextureID, cameraColorTex);
            cmd.SetComputeTextureParam(m_CS, m_KernelDrawReflectionTex, s_ResultID, s_ResultID);
            cmd.SetComputeTextureParam(m_CS, m_KernelDrawReflectionTex, s_IntResultID, s_IntResultID);

            // 用来计算crossFade的
            cmd.SetComputeVectorParam(m_CS, "_CameraFoward",
                renderingData.cameraData.camera.transform.forward);
            cmd.SetComputeFloatParam(m_CS, s_FadeOutScreenBorderWidthHorizontal,
                fadeOutScreenBorderWidthHorizontal);
            cmd.SetComputeFloatParam(m_CS, s_FadeOutScreenBorderWidthVertical,
                fadeOutScreenBorderWidthVertical);

            const int groupSizeX = 8;
            var threadGroupX = m_ResultSize.x / groupSizeX;
            var threadGroupY = m_ResultSize.y / groupSizeX;
            // 执行计算
            cmd.DispatchCompute(m_CS, m_KernelDrawReflectionTex, threadGroupX, threadGroupY, 1);
        }

        void ExecuteFillHoles(CommandBuffer cmd)
        {
            const int groupSizeX = 8;
            var threadGroupX = m_ResultSize.x / groupSizeX;
            var threadGroupY = m_ResultSize.y / groupSizeX;
            cmd.SetComputeTextureParam(m_CS, m_KernelFillHoles, s_ResultID, s_ResultID);
            cmd.DispatchCompute(m_CS, m_KernelFillHoles, threadGroupX, threadGroupY, 1);
        }

        void ExecuteBlur(CommandBuffer cmd)
        {
            //双重Kawase模糊
            //降采样
            cmd.Blit(s_ResultID, s_DualKawaseBlurBig, _blurMaterial, 0);
            cmd.Blit(s_DualKawaseBlurBig, s_DualKawaseBlurSmall0, _blurMaterial, 0);
            //升采样
            cmd.Blit(s_DualKawaseBlurSmall0, s_DualKawaseBlurSmall1, _blurMaterial, 1);
            cmd.Blit(s_DualKawaseBlurSmall1, s_DualKawaseBlurBig, _blurMaterial, 1);

            cmd.SetGlobalTexture(s_WaterReflectionTexID, s_DualKawaseBlurBig);
        }

        public void Render(CommandBuffer cmd, ref RenderingData renderingData, ref PlanarDescriptor planarDescriptor)
        {
            if (renderingData.cameraData.camera.cameraType == CameraType.Preview ||
                renderingData.cameraData.camera.cameraType == CameraType.SceneView) return;

            ExecuteClear(cmd);
            ExecuteGenResult(cmd, ref renderingData, ref planarDescriptor);
            ExecuteDrawReflectionTex(cmd, ref renderingData);

            if (_fillHoles)
                ExecuteFillHoles(cmd);

            if (_blurMaterial)
                ExecuteBlur(cmd);
            else
                cmd.SetGlobalTexture(s_WaterReflectionTexID, s_ResultID);
        }

        public void ReleaseTemporary(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(s_ResultID);
            cmd.ReleaseTemporaryRT(s_IntResultID);
            cmd.ReleaseTemporaryRT(s_DualKawaseBlurBig);
            cmd.ReleaseTemporaryRT(s_DualKawaseBlurSmall0);
            cmd.ReleaseTemporaryRT(s_DualKawaseBlurSmall1);
        }

        static readonly int s_WaterReflectionTexID = Shader.PropertyToID("_WaterReflectionTex");
        static readonly int s_IntResultID = Shader.PropertyToID("_IntResult");
        static readonly int s_ResultID = Shader.PropertyToID("_Result");
        static readonly int s_ResultSizeID = Shader.PropertyToID("_ResultSize");
        static readonly int s_CameraColorTextureID = Shader.PropertyToID("_CameraColorTexture");
        static readonly int s_PlanarPositionID = Shader.PropertyToID("_PlanarPosition");
        static readonly int s_PlanarNormalID = Shader.PropertyToID("_PlanarNormal");
        static readonly int s_MatrixVP = Shader.PropertyToID("_MatrixVP");
        static readonly int s_MatrixInvVP = Shader.PropertyToID("_MatrixInvVP");

        static readonly int s_FadeOutScreenBorderWidthVertical =
            Shader.PropertyToID("_FadeOutScreenBorderWidthVertical");

        static readonly int s_FadeOutScreenBorderWidthHorizontal =
            Shader.PropertyToID("_FadeOutScreenBorderWidthHorizontal");

        static readonly int s_DualKawaseBlurBig = Shader.PropertyToID("_DualKawaseBlurBig");
        static readonly int s_DualKawaseBlurSmall0 = Shader.PropertyToID("_DualKawaseBlurSmall0");
        static readonly int s_DualKawaseBlurSmall1 = Shader.PropertyToID("_DualKawaseBlurSmall1");
    }
}
