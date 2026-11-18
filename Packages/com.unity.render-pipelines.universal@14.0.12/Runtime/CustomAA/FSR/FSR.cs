namespace UnityEngine.Rendering.Universal
{
    static class FSRPass
    {
        private static FSRData _fsrData = null;
        #region JitterMatrix

        //效果更好？？？？？？？？？？？？？？
        private static int GetJitterPhaseCount(int renderWidth, int displayWidth) {
            const float basePhaseCount = 8.0f;
            int jitterPhaseCount = (int)(basePhaseCount * Mathf.Pow((float)displayWidth / renderWidth, 2.0f));
            return jitterPhaseCount;
        }
        private static float Halton(int index, int @base) {
            float f = 1.0f, result = 0.0f;

            for(int currentIndex = index; currentIndex > 0;) {

                f /= @base;
                result += f * (currentIndex % @base);
                currentIndex = (int)Mathf.Floor((float)currentIndex / @base);
            }

            return result;
        }

        private static void GetJitterOffset(out float outX, out float outY, int index, int phaseCount) {
            outX = Halton((index % phaseCount) + 1, 2) - 0.5f;
            outY = Halton((index % phaseCount) + 1, 3) - 0.5f;
        }
        internal static Matrix4x4 CalculateJitterMatrix(ref CameraData cameraData)
        {
            int renderWidth = cameraData.pixelWidth;
            int renderHeight = cameraData.pixelHeight;
            int jitterPhaseCount = GetJitterPhaseCount(renderWidth, (int)(renderWidth * 1.0));
            GetJitterOffset(out float jitterX, out float jitterY, Time.frameCount, jitterPhaseCount);
            //m_dispatchDescription.JitterOffset = new Vector2(jitterX, jitterY);

            jitterX = 2.0f * jitterX / (float)renderWidth;
            jitterY = 2.0f * jitterY / (float)renderHeight;

            float antiGhosting = 1.0f;
            jitterX += UnityEngine.Random.Range(-0.0001f * antiGhosting, 0.0001f * antiGhosting);
            jitterY += UnityEngine.Random.Range(-0.0001f * antiGhosting, 0.0001f * antiGhosting);

            var jitterMat = Matrix4x4.Translate(new Vector2(jitterX, jitterY));
            return jitterMat;
        }
        #endregion

        internal static void ValidateAndWarn(ref CameraData cameraData)
        {
            string warning = null;
            if(cameraData.camera.TryGetComponent<UniversalAdditionalCameraData>(out var additionalCameraData))
            {
                if (additionalCameraData.renderType == CameraRenderType.Overlay)
                {
                    warning = "Disabling FSR because camera is stacked.";
                }
                else if (additionalCameraData.cameraStack.Count > 0)
                {
                    warning = "Disabling FSR3 because the camera has an overlay camera stack.";
                }
            }
            if (warning == null && cameraData.cameraTargetDescriptor.msaaSamples != 1)
            {
                warning = "Disabling FSR because MSAA is on.";
            }
            if (warning == null && cameraData.camera.allowDynamicResolution)
                warning = "Disabling FSR because camera has dynamic resolution enabled. You can use a constant render scale instead.";
            if(warning == null && !cameraData.postProcessEnabled)
                warning = "Disabling FSR because camera has post-processing disabled.";
            const int warningThrottleFrames = 60 * 1; // 60 FPS * 1 sec
            if(Time.frameCount % warningThrottleFrames == 0 && warning != null)
                Debug.LogWarning(warning);
        }
        
        internal static void ExecutePass(CommandBuffer cmd, ref CameraData cameraData, RTHandle source, RTHandle destination, RTHandle motionVectors, RTHandle depthTexture, bool inverse)
        {
            if (_fsrData == null || source == null || depthTexture == null || motionVectors == null)
                return;
            _fsrData.Dispatch(cmd, source.nameID, depthTexture.nameID, motionVectors.nameID);
            if (destination == null)
                return;
            BlitToTarget(cmd, destination, inverse);
        }
        
        internal static void BlitToTarget(CommandBuffer cmd, RTHandle target, bool inverse)
        {
            Blitter.BlitCameraTexture(cmd, _fsrData.FsrOutput, target, inverse ? new Vector4(1, 1, 0, 0) : new Vector4(1, -1, 0, 1), 0, false);
        }
        
        internal static void GetRenderScale(ref float scale, CustomAA.CustomAAQuality quality)
        {
            scale = quality switch
            {
                CustomAA.CustomAAQuality.MaximumQuality => 1.0f,
                CustomAA.CustomAAQuality.Balanced => 0.8f,
                CustomAA.CustomAAQuality.MaximumPerformance => 0.6f,
                CustomAA.CustomAAQuality.UltraPerformance => 0.4f,
                _ => 1.0f
            };
        }
        internal static void UpdateData(ref CameraData cameraData, UniversalAdditionalCameraData additionalCameraData)
        {
            cameraData.fsrSettings = additionalCameraData.fsrSettings;
            _fsrData ??= new FSRData();
            var des = cameraData.cameraTargetDescriptor;
            var set = cameraData.fsrSettings;
            
            _fsrData?.Update(cameraData.camera, set.EnableSharpness, set.Sharpness, des.graphicsFormat, set.Quality, des.width, des.height);
        }

        internal static void ReleaseData()
        {
            _fsrData?.DisableFsr();
            _fsrData = null;
        }

        internal static RTHandle GetSource()
        {
            _fsrData ??= new FSRData();
            return _fsrData.FsrOutput;
        }
    }
}
