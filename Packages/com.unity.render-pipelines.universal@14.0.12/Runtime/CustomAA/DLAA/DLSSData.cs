#if UNITY_STANDALONE_WIN && UNITY_64 && ENABLE_NVIDIA
using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.NVIDIA;

namespace UnityEngine.Rendering.Universal
{
    public struct DlssViewData
    {
        public Resolution inputRes;

        public float sharpness;

        /*public float jitterX;
        public float jitterY;*/
        public bool reset;
    }

    public sealed class DLSSData
    {
        public RTHandle dlssOutput;
        public GraphicsFormat CameraGraphicsOutput = GraphicsFormat.B10G11R11_UFloatPack32;
        public DlssViewData dlssData;
        private RenderTextureDescriptor _descriptor;
        private UnityEngine.NVIDIA.DLSSContext dlssContext = null;
        private Resolution m_BackbufferRes;

        public bool IsSupported()
        {
            return device != null && device.IsFeatureAvailable(GraphicsDeviceFeature.DLSS);
        }

        private GraphicsDevice _device;

        private GraphicsDevice device
        {
            get
            {
                if (_device != null) return _device;
                if (!NVUnityPlugin.IsLoaded())
                    return null;
                if (!SystemInfo.graphicsDeviceVendor.ToLowerInvariant().Contains("nvidia"))
                    return null;
                _device = GraphicsDevice.CreateGraphicsDevice();
                return _device;
            }
        }

        private DLSSCommandInitializationData _dlssSetting;

        public DLSSData()
        {
            _descriptor = new RenderTextureDescriptor()
            {
                width = Screen.width,
                height = Screen.height,
                msaaSamples = 1,
                enableRandomWrite = true,
                graphicsFormat = CameraGraphicsOutput,
                volumeDepth = 1,
                useMipMap = false,
                dimension = TextureDimension.Tex2D,
            };
            //只能在dx运行，部分参数固定
            _dlssSetting = new DLSSCommandInitializationData();
            _dlssSetting.SetFlag(UnityEngine.NVIDIA.DLSSFeatureFlags.MVLowRes, true);
            _dlssSetting.SetFlag(UnityEngine.NVIDIA.DLSSFeatureFlags.DepthInverted, true);
            _dlssSetting.SetFlag(UnityEngine.NVIDIA.DLSSFeatureFlags.DoSharpening, true);
            _dlssSetting.SetFlag(UnityEngine.NVIDIA.DLSSFeatureFlags.MVJittered, true);
            _resetDlssFeature = true;
        }

        private bool _resetDlssFeature;

        //@return render scale
        public float UpdateSetting(ref CustomAA.Settings settings)
        {
            //如果自定义了分辨率
            if (settings.UseOptimalSetting)
            {
                device.GetOptimalSettings((uint) Screen.width, (uint) Screen.height, (DLSSQuality) settings.Quality, out var optSettings);
                settings.Sharpness = optSettings.sharpness;
                settings.EnableSharpness = true;
                float s = Mathf.Min((float) optSettings.outRenderWidth / Screen.width,
                    (float) optSettings.outRenderHeight / Screen.height);
                return s > 0.1f ? s : 0.1f;
            }

            var scale = CustomAA.CustomAAQualityScale[(int) settings.Quality];
            return scale;
        }

        public void Update(int width, int height, RenderTextureFormat colorFormat, bool isHDR, CustomAA.Settings set, float localScale)
        {
            if (!IsSupported() || width < 1 || height < 1)
            {
                Debug.LogError("Not Support DLSS");
                return;
            }
            
            _descriptor.width = Screen.width;
            _descriptor.height = Screen.height;
            _descriptor.colorFormat = colorFormat;
            RenderingUtils.ReAllocateIfNeeded(ref dlssOutput, _descriptor, name : "Dlss Target");
            dlssData.inputRes = new Resolution() {width = width, height = height};
            
            this.dlssData.sharpness = set.Sharpness;
            var quality = (DLSSQuality)set.Quality;
            if (set.Quality == CustomAA.CustomAAQuality.Auto)
            {
                //remap quality
                if (localScale < 0.4)
                {
                    quality = DLSSQuality.UltraPerformance;
                }
                else if (localScale < 0.6)
                {
                    quality = DLSSQuality.MaximumPerformance;
                }
                else if (localScale < 0.8)
                {
                    quality = DLSSQuality.Balanced;
                }
                else
                {
                    quality = DLSSQuality.MaximumQuality;
                }
            }

            //设置 _dlssSetting
            if (_dlssSetting.GetFlag(UnityEngine.NVIDIA.DLSSFeatureFlags.IsHDR) != isHDR)
            {
                _dlssSetting.SetFlag(UnityEngine.NVIDIA.DLSSFeatureFlags.IsHDR, true);
                _resetDlssFeature = true;
            }

            if (
                _dlssSetting.inputRTWidth != (uint) width ||
                _dlssSetting.inputRTHeight != (uint) height ||
                _dlssSetting.outputRTWidth != (uint) Screen.width ||
                _dlssSetting.outputRTHeight != (uint) Screen.height ||
                dlssContext == null
            )
            {
                _dlssSetting.inputRTWidth = (uint) width;
                _dlssSetting.inputRTHeight = (uint) height;
                _dlssSetting.outputRTWidth = (uint) Screen.width;
                _dlssSetting.outputRTHeight = (uint) Screen.height;
                _dlssSetting.quality = quality;
                _resetDlssFeature = true;
            }
        }

        public void OnDisable()
        {
            if (dlssOutput != null)
            {
                dlssOutput.Release();
                dlssOutput = null;
            }

            //删除临时feature
            //Cleanup(dlssCMD);
        }


        public void CreateContext(CommandBuffer cmdBuffer)
        {
            dlssData.reset = false;
            if (_resetDlssFeature || dlssContext == null)
            {
                Cleanup(cmdBuffer);
                dlssContext = device.CreateFeature(cmdBuffer, _dlssSetting);
                dlssData.reset = true;
            }

            _resetDlssFeature = false;
        }

        public void UpdateDispatch(Texture source, Texture depth, Texture motionVectors, Texture biasColorMask,
            Texture output, CommandBuffer cmdBuffer)
        {
            if (dlssContext == null)
                return;

            dlssContext.executeData.sharpness = dlssData.sharpness;
            dlssContext.executeData.mvScaleX = -((float) dlssData.inputRes.width);
            dlssContext.executeData.mvScaleY = -((float) dlssData.inputRes.height);
            dlssContext.executeData.subrectOffsetX = 0;
            dlssContext.executeData.subrectOffsetY = 0;
            dlssContext.executeData.subrectWidth = (uint) dlssData.inputRes.width;
            dlssContext.executeData.subrectHeight = (uint) dlssData.inputRes.height;
            /*dlssContext.executeData.jitterOffsetX = m_Data.jitterX;
            dlssContext.executeData.jitterOffsetY = m_Data.jitterY;*/
            dlssContext.executeData.preExposure = 1f;
            dlssContext.executeData.invertYAxis = 0u;
            dlssContext.executeData.invertXAxis = 0u;
            dlssContext.executeData.reset = dlssData.reset ? 1 : 0;

            var textureTable = new UnityEngine.NVIDIA.DLSSTextureTable()
            {
                colorInput = source,
                colorOutput = output,
                depth = depth,
                motionVectors = motionVectors,
                biasColorMask = biasColorMask
            };
            device.ExecuteDLSS(cmdBuffer, dlssContext, textureTable);
        }

        private void Cleanup(CommandBuffer cmdBuffer)
        {
            try
            {
                if (dlssContext != null)
                {
                    device.DestroyFeature(cmdBuffer, dlssContext);
                    dlssContext = null;
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
#endif