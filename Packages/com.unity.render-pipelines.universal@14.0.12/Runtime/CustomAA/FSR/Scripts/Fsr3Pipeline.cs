using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Base class for all of the compute passes that make up the FSR3 process.
    /// This loosely matches the FfxPipelineState struct from the original FSR3 codebase, wrapped in an object-oriented blanket.
    /// These classes are responsible for loading compute shaders, managing temporary resources, binding resources to shader kernels and dispatching said shaders.
    /// </summary>
    internal abstract class Fsr3Pipeline : IDisposable
    {
        internal const int ShadingChangeMipLevel = 4;   // This matches the FFX_FSR3_SHADING_CHANGE_MIP_LEVEL define

        protected readonly Fsr3.ContextDescription ContextDescription;
        protected readonly Fsr3Resources Resources;
        protected readonly ComputeBuffer Constants;

        protected ComputeShader ComputeShader;
        protected int KernelIndex;

        protected virtual bool AllowFP16 => true;

        protected Fsr3Pipeline(Fsr3.ContextDescription contextDescription, Fsr3Resources resources, ComputeBuffer constants) {
            ContextDescription = contextDescription;
            Resources = resources;
            Constants = constants;
        }

        public virtual void Dispose() {
            UnloadComputeShader();
        }

        public abstract void ScheduleDispatch(CommandBuffer commandBuffer, Fsr3.DispatchDescription dispatchParams, int frameIndex, int dispatchX, int dispatchY);

        protected void LoadComputeShader(string name) {
            LoadComputeShader(name, ContextDescription.Flags, ref ComputeShader, out KernelIndex);
        }

        private void LoadComputeShader(string name, Fsr3.InitializationFlags flags, ref ComputeShader shaderRef, out int kernelIndex) {

            if(shaderRef == null) {
                shaderRef = ContextDescription.Callbacks.LoadComputeShader(name);
                if(shaderRef == null)
                    throw new MissingReferenceException($"Shader '{name}' could not be loaded! Please ensure it is included in the project correctly.");
            }

            kernelIndex = shaderRef.FindKernel("CS");

            bool useLut = SystemInfo.computeSubGroupSize == 64;

            // Allow 16-bit floating point as a configuration option, except on passes that explicitly disable it
            bool supportedFP16 = ((flags & Fsr3.InitializationFlags.EnableFP16Usage) != 0 && AllowFP16);

            // This matches the permutation rules from the CreatePass* functions
            if((flags & Fsr3.InitializationFlags.EnableHighDynamicRange) != 0)
                shaderRef.EnableKeyword("FFX_FSR3UPSCALER_OPTION_HDR_COLOR_INPUT");
            if((flags & Fsr3.InitializationFlags.EnableDisplayResolutionMotionVectors) == 0)
                shaderRef.EnableKeyword("FFX_FSR3UPSCALER_OPTION_LOW_RESOLUTION_MOTION_VECTORS");
            if((flags & Fsr3.InitializationFlags.EnableMotionVectorsJitterCancellation) != 0)
                shaderRef.EnableKeyword("FFX_FSR3UPSCALER_OPTION_JITTERED_MOTION_VECTORS");
            if((flags & Fsr3.InitializationFlags.EnableDepthInverted) != 0)
                shaderRef.EnableKeyword("FFX_FSR3UPSCALER_OPTION_INVERTED_DEPTH");
            if(useLut)
                shaderRef.EnableKeyword("FFX_FSR3UPSCALER_OPTION_REPROJECT_USE_LANCZOS_TYPE");
            if(supportedFP16)
                shaderRef.EnableKeyword("FFX_HALF");
        }

        private void UnloadComputeShader() {
            UnloadComputeShader(ref ComputeShader);
        }

        private void UnloadComputeShader(ref ComputeShader shaderRef) {
            if(shaderRef == null)
                return;

            ContextDescription.Callbacks.UnloadComputeShader(shaderRef);
            shaderRef = null;
        }
    }

    internal class Fsr3ComputeLuminancePyramidPass : Fsr3Pipeline
    {
        private readonly ComputeBuffer _spdConstants;

        public Fsr3ComputeLuminancePyramidPass(Fsr3.ContextDescription contextDescription, Fsr3Resources resources, ComputeBuffer constants, ComputeBuffer spdConstants)
            : base(contextDescription, resources, constants) {
            _spdConstants = spdConstants;

            LoadComputeShader("FSR3/ffx_fsr3upscaler_compute_luminance_pyramid_pass");
        }

        public override void ScheduleDispatch(CommandBuffer commandBuffer, Fsr3.DispatchDescription dispatchParams, int frameIndex, int dispatchX, int dispatchY) {
            if(dispatchParams.Color.HasValue)
                commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvInputColor, dispatchParams.Color.Value, 0, RenderTextureSubElement.Color);

            commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.UavSpdAtomicCount, Resources.SpdAtomicCounter);
            commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.UavExposureMipLumaChange, Resources.SceneLuminance, ShadingChangeMipLevel);
            commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.UavExposureMip5, Resources.SceneLuminance, 5);
            commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.UavAutoExposure, Resources.AutoExposure);

            commandBuffer.SetComputeConstantBufferParam(ComputeShader, FSRShaderIDs.CbFsr3, Constants, 0, Marshal.SizeOf<Fsr3.Fsr3Constants>());
            commandBuffer.SetComputeConstantBufferParam(ComputeShader, FSRShaderIDs.CbSpd, _spdConstants, 0, Marshal.SizeOf<Fsr3.SpdConstants>());

            commandBuffer.DispatchCompute(ComputeShader, KernelIndex, dispatchX, dispatchY, 1);
        }
    }

    internal class Fsr3ReconstructPreviousDepthPass : Fsr3Pipeline
    {
        public Fsr3ReconstructPreviousDepthPass(Fsr3.ContextDescription contextDescription, Fsr3Resources resources, ComputeBuffer constants)
            : base(contextDescription, resources, constants) {
            LoadComputeShader("FSR3/ffx_fsr3upscaler_reconstruct_previous_depth_pass");
        }

        public override void ScheduleDispatch(CommandBuffer commandBuffer, Fsr3.DispatchDescription dispatchParams, int frameIndex, int dispatchX, int dispatchY) {
            if(dispatchParams.Color.HasValue)
                commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvInputColor, dispatchParams.Color.Value, 0, RenderTextureSubElement.Color);

            if(dispatchParams.Depth.HasValue) {
                if(dispatchParams.DepthFormat) {
                    commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvInputDepth, dispatchParams.Depth.Value, 0, RenderTextureSubElement.Depth);
                } else {
                    commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvInputDepth, dispatchParams.Depth.Value, 0, RenderTextureSubElement.Color);
                }
            }

            if(dispatchParams.MotionVectors.HasValue)
                commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvInputMotionVectors, dispatchParams.MotionVectors.Value);

            if(dispatchParams.Exposure.HasValue)
                commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvInputExposure, dispatchParams.Exposure.Value);

            commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.UavDilatedMotionVectors, Resources.DilatedMotionVectors[frameIndex]);

            commandBuffer.SetComputeConstantBufferParam(ComputeShader, FSRShaderIDs.CbFsr3, Constants, 0, Marshal.SizeOf<Fsr3.Fsr3Constants>());

            commandBuffer.DispatchCompute(ComputeShader, KernelIndex, dispatchX, dispatchY, 1);
        }
    }

    internal class Fsr3DepthClipPass : Fsr3Pipeline
    {
        public Fsr3DepthClipPass(Fsr3.ContextDescription contextDescription, Fsr3Resources resources, ComputeBuffer constants)
            : base(contextDescription, resources, constants) {
            LoadComputeShader("FSR3/ffx_fsr3upscaler_depth_clip_pass");
        }

        public override void ScheduleDispatch(CommandBuffer commandBuffer, Fsr3.DispatchDescription dispatchParams, int frameIndex, int dispatchX, int dispatchY) {
            if(dispatchParams.Color.HasValue)
                commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvInputColor, dispatchParams.Color.Value, 0, RenderTextureSubElement.Color);

            if(dispatchParams.Depth.HasValue) {
                if(dispatchParams.DepthFormat) {
                    commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvInputDepth, dispatchParams.Depth.Value, 0, RenderTextureSubElement.Depth);
                } else {
                    commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvInputDepth, dispatchParams.Depth.Value, 0, RenderTextureSubElement.Color);
                }
            }

            if(dispatchParams.MotionVectors.HasValue)
                commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvInputMotionVectors, dispatchParams.MotionVectors.Value);

            if(dispatchParams.Exposure.HasValue)
                commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvInputExposure, dispatchParams.Exposure.Value);

            if(dispatchParams.Reactive.HasValue)
                commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvReactiveMask, dispatchParams.Reactive.Value);

            if(dispatchParams.TransparencyAndComposition.HasValue)
                commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvTransparencyAndCompositionMask, dispatchParams.TransparencyAndComposition.Value);

            commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvReconstructedPrevNearestDepth, FSRShaderIDs.UavReconstructedPrevNearestDepth);
            commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvDilatedMotionVectors, Resources.DilatedMotionVectors[frameIndex]);
            commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvDilatedDepth, FSRShaderIDs.UavDilatedDepth);
            commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvPrevDilatedMotionVectors, Resources.DilatedMotionVectors[frameIndex ^ 1]);

            commandBuffer.SetComputeConstantBufferParam(ComputeShader, FSRShaderIDs.CbFsr3, Constants, 0, Marshal.SizeOf<Fsr3.Fsr3Constants>());

            commandBuffer.DispatchCompute(ComputeShader, KernelIndex, dispatchX, dispatchY, 1);
        }
    }

    internal class Fsr3LockPass : Fsr3Pipeline
    {
        public Fsr3LockPass(Fsr3.ContextDescription contextDescription, Fsr3Resources resources, ComputeBuffer constants)
            : base(contextDescription, resources, constants) {
            LoadComputeShader("FSR3/ffx_fsr3upscaler_lock_pass");
        }

        public override void ScheduleDispatch(CommandBuffer commandBuffer, Fsr3.DispatchDescription dispatchParams, int frameIndex, int dispatchX, int dispatchY) {
            commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvLockInputLuma, FSRShaderIDs.UavLockInputLuma);
            commandBuffer.SetComputeConstantBufferParam(ComputeShader, FSRShaderIDs.CbFsr3, Constants, 0, Marshal.SizeOf<Fsr3.Fsr3Constants>());

            commandBuffer.DispatchCompute(ComputeShader, KernelIndex, dispatchX, dispatchY, 1);
        }
    }

    internal class Fsr3AccumulatePass : Fsr3Pipeline
    {
        private const string SharpeningKeyword = "FFX_FSR3UPSCALER_OPTION_APPLY_SHARPENING";

        // Workaround: Disable FP16 path for the accumulate pass on NVIDIA due to reduced occupancy and high VRAM throughput.
        protected override bool AllowFP16 => SystemInfo.graphicsDeviceVendorID != 0x10DE;

        private readonly LocalKeyword _sharpeningKeyword;

        public Fsr3AccumulatePass(Fsr3.ContextDescription contextDescription, Fsr3Resources resources, ComputeBuffer constants)
            : base(contextDescription, resources, constants) {
            LoadComputeShader("FSR3/ffx_fsr3upscaler_accumulate_pass");
            _sharpeningKeyword = new LocalKeyword(ComputeShader, SharpeningKeyword);
        }

        public override void ScheduleDispatch(CommandBuffer commandBuffer, Fsr3.DispatchDescription dispatchParams, int frameIndex, int dispatchX, int dispatchY) {
            if(dispatchParams.EnableSharpening)
                commandBuffer.EnableKeyword(ComputeShader, _sharpeningKeyword);
            else
                commandBuffer.DisableKeyword(ComputeShader, _sharpeningKeyword);


            if((ContextDescription.Flags & Fsr3.InitializationFlags.EnableDisplayResolutionMotionVectors) == 0)
                commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvDilatedMotionVectors, Resources.DilatedMotionVectors[frameIndex]);
            else if(dispatchParams.MotionVectors.HasValue)
                commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvInputMotionVectors, dispatchParams.MotionVectors.Value);

            if(dispatchParams.Exposure.HasValue)
                commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvInputExposure, dispatchParams.Exposure.Value);

            commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvDilatedReactiveMasks, FSRShaderIDs.UavDilatedReactiveMasks);
            commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvInternalUpscaled, Resources.InternalUpscaled[frameIndex ^ 1]);
            commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvLockStatus, Resources.LockStatus[frameIndex ^ 1]);
            commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvPreparedInputColor, FSRShaderIDs.UavPreparedInputColor);
            commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvLanczosLut, Resources.LanczosLut);
            commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvUpscaleMaximumBiasLut, Resources.MaximumBiasLut);
            commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvSceneLuminanceMips, Resources.SceneLuminance);
            commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvAutoExposure, Resources.AutoExposure);
            commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvLumaHistory, Resources.LumaHistory[frameIndex ^ 1]);

            commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.UavInternalUpscaled, Resources.InternalUpscaled[frameIndex]);
            commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.UavLockStatus, Resources.LockStatus[frameIndex]);
            commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.UavLumaHistory, Resources.LumaHistory[frameIndex]);

            if(dispatchParams.Output.HasValue)
                commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.UavUpscaledOutput, dispatchParams.Output.Value);

            commandBuffer.SetComputeConstantBufferParam(ComputeShader, FSRShaderIDs.CbFsr3, Constants, 0, Marshal.SizeOf<Fsr3.Fsr3Constants>());

            commandBuffer.DispatchCompute(ComputeShader, KernelIndex, dispatchX, dispatchY, 1);
        }
    }

    internal class Fsr3RcasPass : Fsr3Pipeline
    {
        private readonly ComputeBuffer _rcasConstants;

        public Fsr3RcasPass(Fsr3.ContextDescription contextDescription, Fsr3Resources resources, ComputeBuffer constants, ComputeBuffer rcasConstants)
            : base(contextDescription, resources, constants) {
            _rcasConstants = rcasConstants;

            LoadComputeShader("FSR3/ffx_fsr3upscaler_rcas_pass");
        }

        public override void ScheduleDispatch(CommandBuffer commandBuffer, Fsr3.DispatchDescription dispatchParams, int frameIndex, int dispatchX, int dispatchY) {
            if(dispatchParams.Exposure.HasValue)
                commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvInputExposure, dispatchParams.Exposure.Value);

            commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.SrvRcasInput, Resources.InternalUpscaled[frameIndex]);

            if(dispatchParams.Output.HasValue)
                commandBuffer.SetComputeTextureParam(ComputeShader, KernelIndex, FSRShaderIDs.UavUpscaledOutput, dispatchParams.Output.Value);

            commandBuffer.SetComputeConstantBufferParam(ComputeShader, FSRShaderIDs.CbFsr3, Constants, 0, Marshal.SizeOf<Fsr3.Fsr3Constants>());
            commandBuffer.SetComputeConstantBufferParam(ComputeShader, FSRShaderIDs.CbRcas, _rcasConstants, 0, Marshal.SizeOf<Fsr3.RcasConstants>());

            commandBuffer.DispatchCompute(ComputeShader, KernelIndex, dispatchX, dispatchY, 1);
        }
    }
    
}
