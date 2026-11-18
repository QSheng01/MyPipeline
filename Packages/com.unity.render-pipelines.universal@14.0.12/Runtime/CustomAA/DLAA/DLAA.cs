using System;
#if UNITY_STANDALONE_WIN && UNITY_64 && ENABLE_NVIDIA
using UnityEngine.NVIDIA;
#endif

namespace UnityEngine.Rendering.Universal
{
    public static class DLAAPass
    {
        internal static bool IsSupported()
        {
#if UNITY_STANDALONE_WIN && UNITY_64 && ENABLE_NVIDIA
            _dlssData ??= new DLSSData();
            return NVUnityPlugin.Load() && _dlssData.IsSupported();
#else
            return false;
#endif
        }
#if UNITY_STANDALONE_WIN && UNITY_64 && ENABLE_NVIDIA
        private static DLSSData _dlssData;
#endif
        private static float dlaaRenderScale = 1.0f;
        private static bool lockDlaaRenderScale = false;
        //初始化数据
        internal static void UpdateData(ref CameraData cameraData, UniversalAdditionalCameraData additionalCameraData)
        {
#if UNITY_STANDALONE_WIN && UNITY_64 && ENABLE_NVIDIA
            _dlssData ??= new DLSSData();
            if (!_dlssData.IsSupported())
            {
                Debug.LogError("Not Support DLSS!");
                return;
            }
            cameraData.dlaaSettings = additionalCameraData.dlaaSettings;
            var des = cameraData.cameraTargetDescriptor;
            var set = additionalCameraData.dlaaSettings;
            dlaaRenderScale = _dlssData.UpdateSetting(ref set);
            lockDlaaRenderScale = (set.Quality == CustomAA.CustomAAQuality.Auto);
            //输出目标永远为scale 1.0
            //输入目标
            //如果开启local scale，根据local scale 修改quality
            _dlssData?.Update(des.width, des.height, des.colorFormat, cameraData.isHdrEnabled, set, cameraData.renderScale);
#endif
        }

        //释放数据
        internal static void ReleaseData()
        {
#if UNITY_STANDALONE_WIN && UNITY_64 && ENABLE_NVIDIA
            _dlssData?.OnDisable();
            _dlssData = null;
#endif
        }

        internal static void GetRenderScale(ref float scale)
        {
#if UNITY_STANDALONE_WIN && UNITY_64 && ENABLE_NVIDIA
            if (lockDlaaRenderScale)
                return;
            scale = dlaaRenderScale;
#endif
        }
        public static bool GetRenderScale(ref float scale, CustomAA.CustomAAQuality quality, bool optimal)
        {
#if UNITY_STANDALONE_WIN && UNITY_64 && ENABLE_NVIDIA
            // 不支持的话直接返回
            _dlssData ??= new DLSSData();

            // 加载 DLSS
            if (!NVUnityPlugin.Load())
            {
                return false;
            }

            if (!_dlssData.IsSupported())
                return false;
            // 自动的话则不需要设置
            if (quality == CustomAA.CustomAAQuality.Auto)
                return true;
            CustomAA.Settings set = CustomAA.Settings.Create();
            set.UseOptimalSetting = optimal;
            set.Quality = quality;
            scale = _dlssData.UpdateSetting(ref set);
            return true;
#endif
            return false;
        }
        internal static void ValidateAndWarn(ref CameraData cameraData)
        {
            string warning = null;
#if UNITY_STANDALONE_WIN && UNITY_64 && ENABLE_NVIDIA
            if(cameraData.camera.TryGetComponent<UniversalAdditionalCameraData>(out var additionalCameraData))
            {
                if (additionalCameraData.renderType == CameraRenderType.Overlay)
                {
                    warning = "Disabling DLAA because camera is stacked.";
                }
                else if (additionalCameraData.cameraStack.Count > 0)
                {
                    warning = "Disabling DLAA because the camera has an overlay camera stack.";
                }
            }
            if (warning == null && cameraData.cameraTargetDescriptor.msaaSamples != 1)
            {
                warning = "Disabling DLAA because MSAA is on.";
            }

            if (warning == null && cameraData.camera.allowDynamicResolution)
                warning = "Disabling DLAA because camera has dynamic resolution enabled. You can use a constant render scale instead.";

            if(warning == null && !cameraData.postProcessEnabled)
                warning = "Disabling DLAA because camera has post-processing disabled.";
            if(warning == null && (_dlssData == null))
                warning = "Not Support DLSS";
#else
             warning = "Android 平台无法使用DLSS";
#endif

            const int warningThrottleFrames = 60 * 1; // 60 FPS * 1 sec
            if(Time.frameCount % warningThrottleFrames == 0 && warning != null)
                Debug.LogWarning(warning);
        }

        #region JitterMatrix
        private static float GetHaltonValue(int index, int radix) {
            float result = 0f;
            float fraction = 1f / radix;

            while(index > 0) {
                result += (index % radix) * fraction;
                index /= radix;
                fraction /= radix;
            }
            return result;
        }
        internal static Matrix4x4 CalculateJitterMatrix(ref CameraData cameraData)
        {
            Matrix4x4 jitterMat = Matrix4x4.identity;
#if UNITY_STANDALONE_WIN && UNITY_64 && ENABLE_NVIDIA
            const int kMaxSampleCount = 8;
            int dlaaFrameIndex = Time.frameCount % kMaxSampleCount;
            float jitterX = GetHaltonValue((dlaaFrameIndex & 1023) + 1, 2) - 0.5f;
            float jitterY = GetHaltonValue((dlaaFrameIndex & 1023) + 1, 3) - 0.5f;
            float antiGhosting = 1.0f;
            jitterX += UnityEngine.Random.Range(-0.1f * antiGhosting, 0.1f * antiGhosting);
            jitterY += UnityEngine.Random.Range(-0.1f * antiGhosting, 0.1f * antiGhosting);
            Vector2 jitter = new Vector2(jitterX, jitterY);

            float actualWidth = cameraData.cameraTargetDescriptor.width;
            float actualHeight = cameraData.cameraTargetDescriptor.height;
            float offsetX = jitter.x * (2.0f / actualWidth);
            float offsetY = jitter.y * (2.0f / actualHeight);

            jitterMat = Matrix4x4.Translate(new Vector3(offsetX, offsetY, 0.0f));
#endif
            return jitterMat;
        }
        #endregion

        internal static void ExecutePass(CommandBuffer cmd, ref CameraData cameraData, RTHandle source,
            RTHandle destination, RTHandle motionVectors, RTHandle depthTexture, bool inverse)
        {
#if UNITY_STANDALONE_WIN && UNITY_64 && ENABLE_NVIDIA
            if (_dlssData == null || !_dlssData.IsSupported())
                return;
            var depthBuffer = depthTexture?.rt;
            var motionBuffer = motionVectors?.rt;
            var colorBuffer = source?.rt;

            if (colorBuffer == null || depthBuffer == null || motionBuffer == null)
                return;

            _dlssData.CameraGraphicsOutput = cameraData.cameraTargetDescriptor.graphicsFormat;
            _dlssData.CreateContext(cmd);
            _dlssData.UpdateDispatch(colorBuffer, depthBuffer, motionBuffer, null, _dlssData.dlssOutput?.rt, cmd);
            if (destination == null)
                return;
            BlitToTarget(cmd, destination, inverse);
#endif
        }

        internal static void BlitToTarget(CommandBuffer cmd, RTHandle target, bool inverse)
        {
#if UNITY_STANDALONE_WIN && UNITY_64 && ENABLE_NVIDIA
            if (_dlssData == null || !_dlssData.IsSupported())
                return;
            Blitter.BlitCameraTexture(cmd, _dlssData.dlssOutput, target, inverse ? new Vector4(1, 1, 0, 0) : new Vector4(1, -1, 0, 1), 0, false);
#endif
        }

        internal static RTHandle GetSource()
        {
#if UNITY_STANDALONE_WIN && UNITY_64 && ENABLE_NVIDIA
            if (_dlssData == null || !_dlssData.IsSupported())
                return null;
            return _dlssData.dlssOutput;
#endif
            return null;
        }
    }
}
