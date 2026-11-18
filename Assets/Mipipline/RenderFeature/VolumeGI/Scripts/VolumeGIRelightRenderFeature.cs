using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumeGIRelightRenderFeature : ScriptableRendererFeature
{
    class VolumeGIReLightRenderPass : ScriptableRenderPass
    {
        //protected RenderTargetIdentifier[] _gbufferMRT = new RenderTargetIdentifier[3];
        protected RenderTargetIdentifier[] _gbufferMRT = new RenderTargetIdentifier[3];
        protected RenderTargetIdentifier _depthTex = new(Shader.PropertyToID("_DepthTex"));

        
        
        protected int _currentFace;
        public static readonly Vector3[] CubemapEulerAngles = {
            new(0.0f,90.0f,180.0f), // +X
            new(0.0f,-90.0f,-180.0f), // -X
            new(-90.0f,0.0f,-180.0f), // +Y
            new(90.0f,0.0f,180.0f), // -Y
            new(0.0f,0.0f,-180.0f), // +Z
            new(0.0f,180.0f,180.0f) // -Z
        };
        
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer _cmd = CommandBufferPool.Get();
            _cmd.GetTemporaryRT(Shader.PropertyToID("_DepthTex"), 64, 64, 24, FilterMode.Point, RenderTextureFormat.Depth);

            for (_currentFace = 0; _currentFace < 6; _currentFace++)
            {
                //修改相机参数 fov 
                CameraData cameraData = renderingData.cameraData;
                Camera camera = cameraData.camera;
                camera.aspect = 1f;
                camera.fieldOfView = 90f;
                
                var transform = camera.transform;
                transform.eulerAngles = CubemapEulerAngles[_currentFace];
                
                
                
                //_cmd.SetRenderTarget(_gbufferMRT, _depthTex, 0, (CubemapFace) _currentFace, 0);
                //_cmd.ClearRenderTarget(true, true, Color.black, 1f);
                context.ExecuteCommandBuffer(_cmd);
            }
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            
        }
    }

    VolumeGIReLightRenderPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new VolumeGIReLightRenderPass();
        
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game && (renderingData.cameraData.renderType == CameraRenderType.Overlay || renderingData.cameraData.camera.targetTexture != null))
        {
            return;
        }
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


