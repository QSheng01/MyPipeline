using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    public sealed class FSRData
    {

        private int _oldInWidth, _oldInHeight;
        private GraphicsFormat _oldGraphicsFormat;
        public RTHandle FsrOutput;
        private readonly Fsr3.DispatchDescription _dispatchDescription;
        private IFsr3Callbacks callbacks { get; set; } = new Fsr3CallbacksBase();
        private Fsr3Context _mContext;
        private RenderTextureDescriptor _descriptor;
        private CustomAA.CustomAAQuality _oldQuality;

        public void Dispatch(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier depth, RenderTargetIdentifier motionVectors)
        {
            _dispatchDescription.Color = source;
            _dispatchDescription.Depth = depth;
            _dispatchDescription.MotionVectors = motionVectors;
            this._mContext.Dispatch(this._dispatchDescription, cmd);
        }
        
        public FSRData()
        {
            _descriptor = new RenderTextureDescriptor()
            {
                width = Screen.width,
                height = Screen.height,
                msaaSamples = 1,
                enableRandomWrite = true,
                graphicsFormat = GraphicsFormat.B10G11R11_UFloatPack32,
                volumeDepth = 1,
                useMipMap = false,
                dimension = TextureDimension.Tex2D,
            };
            _dispatchDescription = new Fsr3.DispatchDescription()
            {
                Exposure = null,
                PreExposure = 1,
                ViewSpaceToMetersFactor = 1.0f,
                Reset = false,
                Reactive = null,
            };
        }

        public void Update(Camera cameras, bool enableSharpening, float sharpness, GraphicsFormat graphicsFormat, CustomAA.CustomAAQuality quality, int inWidth, int inHeight)
        {
            _dispatchDescription.EnableSharpening = enableSharpening;
            _dispatchDescription.Sharpness = sharpness;
            _dispatchDescription.MotionVectorScale.x = -inWidth;
            _dispatchDescription.MotionVectorScale.y = -inHeight;
            _dispatchDescription.RenderSize = new Vector2Int(inWidth, inHeight);
            _dispatchDescription.FrameTimeDelta = Time.deltaTime;
            _dispatchDescription.CameraNear = cameras.nearClipPlane;
            _dispatchDescription.CameraFar = cameras.farClipPlane;
            _dispatchDescription.CameraFovAngleVertical = cameras.fieldOfView * Mathf.Deg2Rad;
            if(SystemInfo.usesReversedZBuffer) {
                (_dispatchDescription.CameraNear, _dispatchDescription.CameraFar) = (_dispatchDescription.CameraFar, _dispatchDescription.CameraNear);
            }

            if (_oldInWidth == inWidth && _oldInHeight == inHeight && _oldGraphicsFormat == graphicsFormat && _mContext != null && quality == _oldQuality) 
                return;
            RebuildFsrContext(quality, inWidth, inHeight);
            _oldGraphicsFormat = graphicsFormat;
            _oldInWidth = inWidth;
            _oldInHeight = inHeight;
            _oldQuality = quality;
        }

        private void RebuildFsrContext(CustomAA.CustomAAQuality value, int inWidth, int inHeight)
        {
            Fsr3.InitializationFlags flags = Fsr3.InitializationFlags.EnableMotionVectorsJitterCancellation
                                             | Fsr3.InitializationFlags.EnableHighDynamicRange
                                             | Fsr3.InitializationFlags.EnableAutoExposure;

            if(_mContext != null) {
                _mContext.Destroy();
                _mContext = null;
            }

            var outWidth = Screen.width;
            var outHeight = Screen.height;
            _mContext = Fsr3.CreateContext(new Vector2Int(outWidth, outHeight), new Vector2Int(outWidth, outHeight), callbacks, flags);
            _descriptor.width = outWidth;
            _descriptor.height = outHeight;
            RenderingUtils.ReAllocateIfNeeded(ref FsrOutput, _descriptor, name : "Fsr Target");
            _dispatchDescription.InputResourceSize = new Vector2Int(inWidth, inHeight);
            _dispatchDescription.Output = FsrOutput;
        }

        public void DisableFsr() {
            FsrOutput?.Release();

            if (_mContext == null) 
                return;
            _mContext.Destroy();
            _mContext = null;
        }
    }
}
