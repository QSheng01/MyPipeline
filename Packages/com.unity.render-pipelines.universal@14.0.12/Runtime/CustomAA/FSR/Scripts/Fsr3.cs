using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering.Universal

{
    /// <summary>
    /// A collection of helper functions and data structures required by the FSR3 process.
    /// </summary>
    public static class Fsr3
    {
        /// <summary>
        /// Creates a new FSR3 context with standard parameters that are appropriate for the current platform. 
        /// </summary>
        public static Fsr3Context CreateContext(Vector2Int displaySize, Vector2Int maxRenderSize, IFsr3Callbacks callbacks, InitializationFlags flags = 0) {
            if(SystemInfo.usesReversedZBuffer)
                flags |= InitializationFlags.EnableDepthInverted;
            else
                flags &= ~InitializationFlags.EnableDepthInverted;
            

            var contextDescription = new ContextDescription {
                Flags = flags,
                DisplaySize = displaySize,
                MaxRenderSize = maxRenderSize,
                Callbacks = callbacks,
            };

            var context = new Fsr3Context();
            context.Create(contextDescription);
            return context;
        }

        

        public static int GetJitterPhaseCount(int renderWidth, int displayWidth) {
            const float basePhaseCount = 8.0f;
            int jitterPhaseCount = (int)(basePhaseCount * Mathf.Pow((float)displayWidth / renderWidth, 2.0f));
            return jitterPhaseCount;
        }
        
        public static float Lanczos2(float value) {
            return Mathf.Abs(value) < Mathf.Epsilon ? 1.0f : Mathf.Sin(Mathf.PI * value) / (Mathf.PI * value) * (Mathf.Sin(0.5f * Mathf.PI * value) / (0.5f * Mathf.PI * value));
        }

        [Flags]
        public enum InitializationFlags
        {
            EnableHighDynamicRange = 1 << 0,
            EnableDisplayResolutionMotionVectors = 1 << 1,
            EnableMotionVectorsJitterCancellation = 1 << 2,
            EnableDepthInverted = 1 << 3,
            EnableDepthInfinite = 1 << 4,
            EnableAutoExposure = 1 << 5,
            EnableDynamicResolution = 1 << 6,
            EnableFP16Usage = 1 << 7,
            EnableDebugChecking = 1 << 8,
        }

        public struct ContextDescription
        {
            public InitializationFlags Flags;
            public Vector2Int MaxRenderSize;
            public Vector2Int DisplaySize;
            public IFsr3Callbacks Callbacks;
        }

        /// <summary>
        /// The input and output resources are all optional. If they are null, the Fsr3Context won't try to bind them to any shaders.
        /// This allows for customized and more efficient resource management outside of Fsr3Context, tailored to the specific scenario.
        /// </summary>
        public class DispatchDescription
        {
            public RenderTargetIdentifier? Color;
            public RenderTargetIdentifier? Depth;
            public bool DepthFormat = true;
            public RenderTargetIdentifier? MotionVectors;
            public RenderTargetIdentifier? Exposure;
            public RenderTargetIdentifier? Reactive;
            public RenderTargetIdentifier? TransparencyAndComposition;
            public RenderTargetIdentifier? Output;
            public Vector2 JitterOffset;
            public Vector2 MotionVectorScale;
            public Vector2Int RenderSize;
            public Vector2Int InputResourceSize;
            public bool EnableSharpening;
            public float Sharpness;
            public float FrameTimeDelta;    // in seconds
            public float PreExposure;
            public bool Reset;
            public float CameraNear;
            public float CameraFar;
            public float CameraFovAngleVertical;
            public float ViewSpaceToMetersFactor;

            // EXPERIMENTAL reactive mask generation parameters
            public bool EnableAutoReactive;
            public RenderTargetIdentifier? ColorOpaqueOnly;
        }

        [Serializable, StructLayout(LayoutKind.Sequential)]
        internal struct Fsr3Constants
        {
            public Vector2Int renderSize;
            public Vector2Int maxRenderSize;
            public Vector2Int displaySize;
            public Vector2Int inputColorResourceDimensions;
            public Vector2Int lumaMipDimensions;
            public int lumaMipLevelToUse;
            public int frameIndex;

            public Vector4 deviceToViewDepth;
            public Vector2 jitterOffset;
            public Vector2 motionVectorScale;
            public Vector2 downscaleFactor;
            public Vector2 motionVectorJitterCancellation;
            public float preExposure;
            public float previousFramePreExposure;
            public float tanHalfFOV;
            public float jitterPhaseCount;
            public float deltaTime;
            public float dynamicResChangeFactor;
            public float viewSpaceToMetersFactor;

            public int dummy;
        }

        [Serializable, StructLayout(LayoutKind.Sequential)]
        internal struct SpdConstants
        {
            public uint mips;
            public uint numWorkGroups;
            public uint workGroupOffsetX, workGroupOffsetY;
            public uint renderSizeX, renderSizeY;
        }

        [Serializable, StructLayout(LayoutKind.Sequential)]
        internal struct GenerateReactiveConstants
        {
            public float scale;
            public float threshold;
            public float binaryValue;
            public uint flags;
        }

        [Serializable, StructLayout(LayoutKind.Sequential)]
        internal struct GenerateReactiveConstants2
        {
            public float autoTcThreshold;
            public float autoTcScale;
            public float autoReactiveScale;
            public float autoReactiveMax;
        }

        [Serializable, StructLayout(LayoutKind.Sequential)]
        internal struct RcasConstants
        {
            public RcasConstants(uint sharpness, uint halfSharp) {
                this.sharpness = sharpness;
                this.halfSharp = halfSharp;
                dummy0 = dummy1 = 0;
            }

            public readonly uint sharpness;
            public readonly uint halfSharp;
            public readonly uint dummy0;
            public readonly uint dummy1;
        }
    }
}
