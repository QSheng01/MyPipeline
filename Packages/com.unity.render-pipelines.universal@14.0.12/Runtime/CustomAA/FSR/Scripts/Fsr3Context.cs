using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// This class loosely matches the FfxFsr3Context struct from the original FSR3 codebase.
    /// It manages the various resources and compute passes required by the FSR3 process.
    /// Note that this class does not know anything about Unity render pipelines; all it knows is CommandBuffers and RenderTargetIdentifiers.
    /// This should make it suitable for integration with any of the available Unity render pipelines.
    /// </summary>
    public class Fsr3Context
    {
        private const int MaxQueuedFrames = 16;

        private Fsr3.ContextDescription _contextDescription;

        private Fsr3Pipeline _depthClipPipeline;
        private Fsr3Pipeline _reconstructPreviousDepthPipeline;
        private Fsr3Pipeline _lockPipeline;
        private Fsr3Pipeline _accumulatePipeline;
        private Fsr3Pipeline _rcasPipeline;
        private Fsr3Pipeline _computeLuminancePyramidPipeline;
        private Fsr3Pipeline _generateReactivePipeline;

        private readonly Fsr3Resources _resources = new Fsr3Resources();

        private ComputeBuffer _fsr3ConstantsBuffer;
        private readonly Fsr3.Fsr3Constants[] _fsr3ConstantsArray = { new Fsr3.Fsr3Constants() };
        private ref Fsr3.Fsr3Constants Constants => ref _fsr3ConstantsArray[0];

        private ComputeBuffer _spdConstantsBuffer;
        private readonly Fsr3.SpdConstants[] _spdConstantsArray = { new Fsr3.SpdConstants() };
        private ref Fsr3.SpdConstants SpdConsts => ref _spdConstantsArray[0];

        private ComputeBuffer _rcasConstantsBuffer;
        private readonly Fsr3.RcasConstants[] _rcasConstantsArray = new Fsr3.RcasConstants[1];
        private ref Fsr3.RcasConstants RcasConsts => ref _rcasConstantsArray[0];

        private ComputeBuffer _generateReactiveConstantsBuffer;
        private readonly Fsr3.GenerateReactiveConstants[] _generateReactiveConstantsArray = { new Fsr3.GenerateReactiveConstants() };
        private ref Fsr3.GenerateReactiveConstants GenReactiveConsts => ref _generateReactiveConstantsArray[0];

        private ComputeBuffer _tcrAutogenerateConstantsBuffer;
        private readonly Fsr3.GenerateReactiveConstants2[] _tcrAutogenerateConstantsArray = { new Fsr3.GenerateReactiveConstants2() };
        private ref Fsr3.GenerateReactiveConstants2 TcrAutoGenConsts => ref _tcrAutogenerateConstantsArray[0];

        private bool _firstExecution;
        private Vector2 _previousJitterOffset;
        private int _resourceFrameIndex;

        public void Create(Fsr3.ContextDescription contextDescription) {
            _contextDescription = contextDescription;

            _fsr3ConstantsBuffer = CreateConstantBuffer<Fsr3.Fsr3Constants>();
            _spdConstantsBuffer = CreateConstantBuffer<Fsr3.SpdConstants>();
            _rcasConstantsBuffer = CreateConstantBuffer<Fsr3.RcasConstants>();
            _generateReactiveConstantsBuffer = CreateConstantBuffer<Fsr3.GenerateReactiveConstants>();
            _tcrAutogenerateConstantsBuffer = CreateConstantBuffer<Fsr3.GenerateReactiveConstants2>();

            // Set defaults
            _firstExecution = true;
            _resourceFrameIndex = 0;

            Constants.displaySize = _contextDescription.DisplaySize;

            _resources.Create(_contextDescription);
            CreatePipelines();
        }

        private void CreatePipelines() {
            _computeLuminancePyramidPipeline = new Fsr3ComputeLuminancePyramidPass(_contextDescription, _resources, _fsr3ConstantsBuffer, _spdConstantsBuffer);
            _reconstructPreviousDepthPipeline = new Fsr3ReconstructPreviousDepthPass(_contextDescription, _resources, _fsr3ConstantsBuffer);
            _depthClipPipeline = new Fsr3DepthClipPass(_contextDescription, _resources, _fsr3ConstantsBuffer);
            _lockPipeline = new Fsr3LockPass(_contextDescription, _resources, _fsr3ConstantsBuffer);
            _accumulatePipeline = new Fsr3AccumulatePass(_contextDescription, _resources, _fsr3ConstantsBuffer);
            _rcasPipeline = new Fsr3RcasPass(_contextDescription, _resources, _fsr3ConstantsBuffer, _rcasConstantsBuffer);
        }

        public void Destroy() {
            DestroyPipeline(ref _generateReactivePipeline);
            DestroyPipeline(ref _computeLuminancePyramidPipeline);
            DestroyPipeline(ref _rcasPipeline);
            DestroyPipeline(ref _accumulatePipeline);
            DestroyPipeline(ref _lockPipeline);
            DestroyPipeline(ref _reconstructPreviousDepthPipeline);
            DestroyPipeline(ref _depthClipPipeline);
            _resources.Destroy();
            DestroyConstantBuffer(ref _tcrAutogenerateConstantsBuffer);
            DestroyConstantBuffer(ref _generateReactiveConstantsBuffer);
            DestroyConstantBuffer(ref _rcasConstantsBuffer);
            DestroyConstantBuffer(ref _spdConstantsBuffer);
            DestroyConstantBuffer(ref _fsr3ConstantsBuffer);
        }


        public void Dispatch(Fsr3.DispatchDescription dispatchParams, CommandBuffer commandBuffer) {
            if(_firstExecution) {
                commandBuffer.SetRenderTarget(_resources.LockStatus[0]);
                commandBuffer.ClearRenderTarget(false, true, Color.clear);
                commandBuffer.SetRenderTarget(_resources.LockStatus[1]);
                commandBuffer.ClearRenderTarget(false, true, Color.clear);
            }
            
            int frameIndex = _resourceFrameIndex % 2;
            bool resetAccumulation = dispatchParams.Reset || _firstExecution;
            _firstExecution = false;

            // If auto exposure is enabled use the auto exposure SRV, otherwise what the app sends
            if((_contextDescription.Flags & Fsr3.InitializationFlags.EnableAutoExposure) != 0)
                dispatchParams.Exposure = _resources.AutoExposure;
            else if(dispatchParams.Exposure == null)
                dispatchParams.Exposure = _resources.DefaultExposure;

            if(dispatchParams.EnableAutoReactive) {
                // Create the auto-TCR resources only when we need them
                if(_resources.AutoReactive == null)
                    _resources.CreateTcrAutogenResources(_contextDescription);

                if(resetAccumulation)
                    commandBuffer.Blit(_resources.PrevPreAlpha[frameIndex ^ 1], dispatchParams.ColorOpaqueOnly ?? FSRShaderIDs.SrvOpaqueOnly);
            } else if(_resources.AutoReactive != null) {
                // Destroy the auto-TCR resources if we don't use the feature 
                _resources.DestroyTcrAutogenResources();
            }

            if(dispatchParams.Reactive == null)
                dispatchParams.Reactive = _resources.DefaultReactive;
            if(dispatchParams.TransparencyAndComposition == null)
                dispatchParams.TransparencyAndComposition = _resources.DefaultReactive;
            Fsr3Resources.CreateAliasableResources(commandBuffer, _contextDescription, dispatchParams);

            SetupConstants(dispatchParams, resetAccumulation);

            // Reactive mask bias
            const int threadGroupWorkRegionDim = 8;
            int dispatchSrcX = (Constants.renderSize.x + (threadGroupWorkRegionDim - 1)) / threadGroupWorkRegionDim;
            int dispatchSrcY = (Constants.renderSize.y + (threadGroupWorkRegionDim - 1)) / threadGroupWorkRegionDim;
            int dispatchDstX = (_contextDescription.DisplaySize.x + (threadGroupWorkRegionDim - 1)) / threadGroupWorkRegionDim;
            int dispatchDstY = (_contextDescription.DisplaySize.y + (threadGroupWorkRegionDim - 1)) / threadGroupWorkRegionDim;

            // Clear reconstructed depth for max depth store
            if(resetAccumulation) {
                commandBuffer.SetRenderTarget(_resources.LockStatus[frameIndex ^ 1]);
                commandBuffer.ClearRenderTarget(false, true, Color.clear);

                commandBuffer.SetRenderTarget(_resources.InternalUpscaled[frameIndex ^ 1]);
                commandBuffer.ClearRenderTarget(false, true, Color.clear);

                commandBuffer.SetRenderTarget(_resources.SceneLuminance);
                commandBuffer.ClearRenderTarget(false, true, Color.clear);

                // Auto exposure always used to track luma changes in locking logic
                commandBuffer.SetRenderTarget(_resources.AutoExposure);
                commandBuffer.ClearRenderTarget(false, true, new Color(0f, 1e8f, 0f, 0f));
                // Reset atomic counter to 0
                commandBuffer.SetRenderTarget(_resources.SpdAtomicCounter);
                commandBuffer.ClearRenderTarget(false, true, Color.clear);
            }

            // FSR3: need to clear here since we need the content of this surface for frame interpolation, so clearing in the lock pass is not an option
            bool depthInverted = (_contextDescription.Flags & Fsr3.InitializationFlags.EnableDepthInverted) == Fsr3.InitializationFlags.EnableDepthInverted;
            commandBuffer.SetRenderTarget(FSRShaderIDs.UavReconstructedPrevNearestDepth);
            commandBuffer.ClearRenderTarget(false, true, depthInverted ? Color.clear : Color.white);

            // Auto exposure
            SetupSpdConstants(dispatchParams, out var dispatchThreadGroupCount);

            // Initialize constant buffers data
            commandBuffer.SetBufferData(_fsr3ConstantsBuffer, _fsr3ConstantsArray);
            commandBuffer.SetBufferData(_spdConstantsBuffer, _spdConstantsArray);
 
            
            // Compute luminance pyramid
            
            _computeLuminancePyramidPipeline.ScheduleDispatch(commandBuffer, dispatchParams, frameIndex, dispatchThreadGroupCount.x, dispatchThreadGroupCount.y);
            // Reconstruct previous depth
            _reconstructPreviousDepthPipeline.ScheduleDispatch(commandBuffer, dispatchParams, frameIndex, dispatchSrcX, dispatchSrcY);
            // Depth clip
            _depthClipPipeline.ScheduleDispatch(commandBuffer, dispatchParams, frameIndex, dispatchSrcX, dispatchSrcY);
            // Create locks
            _lockPipeline.ScheduleDispatch(commandBuffer, dispatchParams, frameIndex, dispatchSrcX, dispatchSrcY);
            // Accumulate
            _accumulatePipeline.ScheduleDispatch(commandBuffer, dispatchParams, frameIndex, dispatchDstX, dispatchDstY);
            

            if(dispatchParams.EnableSharpening) {
                SetupRcasConstants(dispatchParams);
                commandBuffer.SetBufferData(_rcasConstantsBuffer, _rcasConstantsArray);
                const int threadGroupWorkRegionDimRcas = 16;
                int threadGroupsX = (Constants.renderSize.x + threadGroupWorkRegionDimRcas - 1) / threadGroupWorkRegionDimRcas;
                int threadGroupsY = (Constants.renderSize.y + threadGroupWorkRegionDimRcas - 1) / threadGroupWorkRegionDimRcas;
                _rcasPipeline.ScheduleDispatch(commandBuffer, dispatchParams, frameIndex, threadGroupsX, threadGroupsY);
            }

            _resourceFrameIndex = (_resourceFrameIndex + 1) % MaxQueuedFrames;
            Fsr3Resources.DestroyAliasableResources(commandBuffer);
        }

        private void SetupConstants(Fsr3.DispatchDescription dispatchParams, bool resetAccumulation) {


            ref Fsr3.Fsr3Constants constants = ref Constants;

            constants.jitterOffset = dispatchParams.JitterOffset;
            constants.renderSize = dispatchParams.RenderSize;
            constants.maxRenderSize = _contextDescription.MaxRenderSize;
            constants.inputColorResourceDimensions = dispatchParams.InputResourceSize;

            // Compute the horizontal FOV for the shader from the vertical one
            float aspectRatio = (float)dispatchParams.RenderSize.x / dispatchParams.RenderSize.y;
            float cameraAngleHorizontal = Mathf.Atan(Mathf.Tan(dispatchParams.CameraFovAngleVertical / 2.0f) * aspectRatio) * 2.0f;
            constants.tanHalfFOV = Mathf.Tan(cameraAngleHorizontal * 0.5f);
            constants.viewSpaceToMetersFactor = (dispatchParams.ViewSpaceToMetersFactor > 0.0f) ? dispatchParams.ViewSpaceToMetersFactor : 1.0f;

            // Compute params to enable device depth to view space depth computation in shader
            constants.deviceToViewDepth = SetupDeviceDepthToViewSpaceDepthParams(dispatchParams);

            // To be updated if resource is larger than the actual image size
            constants.downscaleFactor = new Vector2((float)constants.renderSize.x / _contextDescription.DisplaySize.x, (float)constants.renderSize.y / _contextDescription.DisplaySize.y);
            constants.previousFramePreExposure = constants.preExposure;
            constants.preExposure = (dispatchParams.PreExposure != 0) ? dispatchParams.PreExposure : 1.0f;

            // Motion vector data
            Vector2Int motionVectorsTargetSize = (_contextDescription.Flags & Fsr3.InitializationFlags.EnableDisplayResolutionMotionVectors) != 0 ? constants.displaySize : constants.renderSize;
            constants.motionVectorScale = dispatchParams.MotionVectorScale / motionVectorsTargetSize;

            // Compute jitter cancellation
            if((_contextDescription.Flags & Fsr3.InitializationFlags.EnableMotionVectorsJitterCancellation) != 0) {
                constants.motionVectorJitterCancellation = (_previousJitterOffset - constants.jitterOffset) / motionVectorsTargetSize;
                _previousJitterOffset = constants.jitterOffset;
            }

            int jitterPhaseCount = Fsr3.GetJitterPhaseCount(dispatchParams.RenderSize.x, _contextDescription.DisplaySize.x);
            if(resetAccumulation || constants.jitterPhaseCount == 0) {
                constants.jitterPhaseCount = jitterPhaseCount;
            } else {
                int jitterPhaseCountDelta = (int)(jitterPhaseCount - constants.jitterPhaseCount);
                if(jitterPhaseCountDelta > 0)
                    constants.jitterPhaseCount++;
                else if(jitterPhaseCountDelta < 0)
                    constants.jitterPhaseCount--;
            }

            // Convert delta time to seconds and clamp to [0, 1]
            constants.deltaTime = Mathf.Clamp01(dispatchParams.FrameTimeDelta);

            if(resetAccumulation)
                constants.frameIndex = 0;
            else
                constants.frameIndex++;

            // Shading change usage of the SPD mip levels
            constants.lumaMipLevelToUse = Fsr3Pipeline.ShadingChangeMipLevel;

            float mipDiv = 2 << constants.lumaMipLevelToUse;
            constants.lumaMipDimensions.x = (int)(constants.maxRenderSize.x / mipDiv);
            constants.lumaMipDimensions.y = (int)(constants.maxRenderSize.y / mipDiv);
        }

        private Vector4 SetupDeviceDepthToViewSpaceDepthParams(Fsr3.DispatchDescription dispatchParams) {
            bool inverted = (_contextDescription.Flags & Fsr3.InitializationFlags.EnableDepthInverted) != 0;
            bool infinite = (_contextDescription.Flags & Fsr3.InitializationFlags.EnableDepthInfinite) != 0;

            // make sure it has no impact if near and far plane values are swapped in dispatch params
            // the flags "inverted" and "infinite" will decide what transform to use
            float min = Mathf.Min(dispatchParams.CameraNear, dispatchParams.CameraFar);
            float max = Mathf.Max(dispatchParams.CameraNear, dispatchParams.CameraFar);

            if(inverted) {
                (min, max) = (max, min);
            }

            float q = max / (min - max);
            float d = -1.0f;

            Vector4 matrixElemC = new Vector4(q, -1.0f - Mathf.Epsilon, q, 0.0f + Mathf.Epsilon);
            Vector4 matrixElemE = new Vector4(q * min, -min - Mathf.Epsilon, q * min, max);

            // Revert x and y coords
            float aspect = (float)dispatchParams.RenderSize.x / dispatchParams.RenderSize.y;
            float cotHalfFovY = Mathf.Cos(0.5f * dispatchParams.CameraFovAngleVertical) / Mathf.Sin(0.5f * dispatchParams.CameraFovAngleVertical);

            int matrixIndex = (inverted ? 2 : 0) + (infinite ? 1 : 0);
            return new Vector4(
                d * matrixElemC[matrixIndex],
                matrixElemE[matrixIndex],
                aspect / cotHalfFovY,
                1.0f / cotHalfFovY);
        }

        private void SetupRcasConstants(Fsr3.DispatchDescription dispatchParams) {
            int sharpnessIndex = Mathf.RoundToInt(Mathf.Clamp01(dispatchParams.Sharpness) * (RcasConfigs.Count - 1));
            RcasConsts = RcasConfigs[sharpnessIndex];
        }

        private void SetupSpdConstants(Fsr3.DispatchDescription dispatchParams, out Vector2Int dispatchThreadGroupCount) {
            RectInt rectInfo = new RectInt(0, 0, dispatchParams.RenderSize.x, dispatchParams.RenderSize.y);
            SpdSetup(rectInfo, out dispatchThreadGroupCount, out var workGroupOffset, out var numWorkGroupsAndMips);

            // Downsample
            ref Fsr3.SpdConstants spdConstants = ref SpdConsts;
            spdConstants.numWorkGroups = (uint)numWorkGroupsAndMips.x;
            spdConstants.mips = (uint)numWorkGroupsAndMips.y;
            spdConstants.workGroupOffsetX = (uint)workGroupOffset.x;
            spdConstants.workGroupOffsetY = (uint)workGroupOffset.y;
            spdConstants.renderSizeX = (uint)dispatchParams.RenderSize.x;
            spdConstants.renderSizeY = (uint)dispatchParams.RenderSize.y;
        }

        private static void SpdSetup(RectInt rectInfo, out Vector2Int dispatchThreadGroupCount, out Vector2Int workGroupOffset, out Vector2Int numWorkGroupsAndMips, int mips = -1) {
            workGroupOffset = new Vector2Int(rectInfo.x / 64, rectInfo.y / 64);

            int endIndexX = (rectInfo.x + rectInfo.width - 1) / 64;
            int endIndexY = (rectInfo.y + rectInfo.height - 1) / 64;

            dispatchThreadGroupCount = new Vector2Int(endIndexX + 1 - workGroupOffset.x, endIndexY + 1 - workGroupOffset.y);

            numWorkGroupsAndMips = new Vector2Int(dispatchThreadGroupCount.x * dispatchThreadGroupCount.y, mips);
            if(mips < 0) {
                float resolution = Math.Max(rectInfo.width, rectInfo.height);
                numWorkGroupsAndMips.y = Math.Min(Mathf.FloorToInt(Mathf.Log(resolution, 2.0f)), 12);
            }
        }


        /// <summary>
        /// The FSR3 C++ codebase uses floats bitwise converted to ints to pass sharpness parameters to the RCAS shader.
        /// This is not possible in C# without enabling unsafe code compilation, so to avoid that we instead use a table of precomputed values.
        /// </summary>
        private static readonly List<Fsr3.RcasConstants> RcasConfigs = new List<Fsr3.RcasConstants>()
        {
            new Fsr3.RcasConstants(1048576000u, 872428544u),
            new Fsr3.RcasConstants(1049178080u, 877212745u),
            new Fsr3.RcasConstants(1049823372u, 882390168u),
            new Fsr3.RcasConstants(1050514979u, 887895276u),
            new Fsr3.RcasConstants(1051256227u, 893859143u),
            new Fsr3.RcasConstants(1052050675u, 900216232u),
            new Fsr3.RcasConstants(1052902144u, 907032080u),
            new Fsr3.RcasConstants(1053814727u, 914306687u),
            new Fsr3.RcasConstants(1054792807u, 922105590u),
            new Fsr3.RcasConstants(1055841087u, 930494326u),
            new Fsr3.RcasConstants(1056964608u, 939538432u),
            new Fsr3.RcasConstants(1057566688u, 944322633u),
            new Fsr3.RcasConstants(1058211980u, 949500056u),
            new Fsr3.RcasConstants(1058903587u, 955005164u),
            new Fsr3.RcasConstants(1059644835u, 960969031u),
            new Fsr3.RcasConstants(1060439283u, 967326120u),
            new Fsr3.RcasConstants(1061290752u, 974141968u),
            new Fsr3.RcasConstants(1062203335u, 981416575u),
            new Fsr3.RcasConstants(1063181415u, 989215478u),
            new Fsr3.RcasConstants(1064229695u, 997604214u),
            new Fsr3.RcasConstants(1065353216u, 1006648320),
        };

        private static ComputeBuffer CreateConstantBuffer<TConstants>() where TConstants : struct {
            return new ComputeBuffer(1, Marshal.SizeOf<TConstants>(), ComputeBufferType.Constant);
        }

        private static void DestroyConstantBuffer(ref ComputeBuffer bufferRef) {
            if(bufferRef == null)
                return;

            bufferRef.Release();
            bufferRef = null;
        }

        private static void DestroyPipeline(ref Fsr3Pipeline pipeline) {
            if(pipeline == null)
                return;

            pipeline.Dispose();
            pipeline = null;
        }
    }
}
