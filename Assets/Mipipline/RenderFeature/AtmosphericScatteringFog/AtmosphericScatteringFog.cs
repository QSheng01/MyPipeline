using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SYPackages.AtmosphericScattering
{
    /// <summary>
    /// AtmosphericScatteringFog Volume Component.
    /// </summary>
    [Serializable, VolumeComponentMenu("Custom Post-processing/AtmosphericScatteringFog")]
    public class AtmosphericScatteringFog : VolumeComponent,IPostProcessComponent
    {
        ///<summary>Enable Linear Fog.</summary>
        public BoolParameter isLinearFog =new BoolParameter(false);
        public ColorParameter linearFogColor = new ColorParameter(Color.gray,false, true,true);
        public FogModeParameter linearFogMode = new FogModeParameter(FogMode.Linear);
        public FloatParameter linearFogStartDistance = new FloatParameter(0);
        public FloatParameter linearFogEndDistance = new FloatParameter(300);
        public ClampedFloatParameter linearfogDensity = new ClampedFloatParameter(0.01f,0,1);


        /// <summary>Enable Atmospheric Scattering Fog.</summary>
        [Tooltip("Enables the Atmospheric Scattering Fog.")]
        public BoolParameter enabled = new BoolParameter(false);
        // public DownSamplingModeParameter downsampleMode = new DownSamplingModeParameter(DownSamplingMode.Normal);
        // public RenderModeParameter renderMode = new RenderModeParameter(RenderMode.PostProcess);
        /// <summary>AtmosphericScatteringFog color mode.</summary>
        [HideInInspector]public FogColorParameter colorMode = new FogColorParameter(FogColorMode.SkyColor);
        /// <summary>AtmosphericScatteringFog color.</summary>
        [Tooltip("Specifies the tint of the fog.")]
        public ColorParameter tint = new ColorParameter(Color.grey, hdr: true, showAlpha: false, showEyeDropper: true);

        /*----Hide---*/
        /// <summary>Specifies the tint of the fog when using Sky Color.</summary>
        [HideInInspector][Tooltip(" Specifies the constant start color of the fog.")]
        public ColorParameter FogStartColor = new ColorParameter(Color.white, hdr: true, showAlpha: false, showEyeDropper: true);
        [HideInInspector][Tooltip(" Specifies the constant End color of the fog.")]
        public ColorParameter FogEndColor = new ColorParameter(Color.white, hdr: true, showAlpha: false, showEyeDropper: true);
        [HideInInspector][Tooltip(" Falloff of the fog.")]
        public ClampedFloatParameter FogFallOff = new ClampedFloatParameter(1f,0f,25f);
        /*----Hide---*/


        /*----Hide---*/
        /// <summary>Controls the maximum mip map HDRP uses for mip fog (0 is the lowest mip and 1 is the highest mip).</summary>
        [HideInInspector][Tooltip("Controls the maximum mip map HDRP uses for mip fog (0 is the lowest mip and 1 is the highest mip).")]
        public ClampedFloatParameter mipFogMaxMip = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        /// <summary>Sets the distance at which HDRP uses the minimum mip image of the blurred sky texture as the fog color.</summary>
        [HideInInspector][Tooltip("Sets the distance at which HDRP uses the minimum mip image of the blurred sky texture as the fog color.")]
        public MinFloatParameter mipFogNear = new MinFloatParameter(0.0f, 0.0f);
        /// <summary>Sets the distance at which HDRP uses the maximum mip image of the blurred sky texture as the fog color.</summary>
        [HideInInspector][Tooltip("Sets the distance at which HDRP uses the maximum mip image of the blurred sky texture as the fog color.")]
        public MinFloatParameter mipFogFar = new MinFloatParameter(1000.0f, 0.0f);
        /*----Hide---*/


        // Height AtmosphericScatteringFog
        /// <summary>Height fog base height.</summary>
        [InspectorName("高度雾起始高度")]public FloatParameter baseHeight = new FloatParameter(0.0f);
        /// <summary>Height fog maximum height.</summary>
        [InspectorName("高度雾最大高度")]public FloatParameter maximumHeight = new FloatParameter(50.0f);
        /// <summary>AtmosphericScatteringFog mean free path.</summary>
        [InspectorName("高度雾起始距离")]
        public ClampedFloatParameter fogStartDistance = new ClampedFloatParameter(10f, 0.0f,500f);
        /// <summary>Maximum fog distance.</summary>
        [InspectorName("高度雾最远距离")]
        public MinFloatParameter maxFogDistance = new MinFloatParameter(5000.0f, 0.0f);
        [InspectorName("高度雾衰减距离")]
        public MinFloatParameter meanFreePath = new MinFloatParameter(5000000.0f, 1.0f);
        [InspectorName("高度雾噪声强度")]
        public ClampedFloatParameter jitterStrength = new ClampedFloatParameter(0f, 0.0f, 100.0f);

        //Sun
        [InspectorName("高度雾太阳光照影响")]
        public BoolParameter enableSun = new BoolParameter(false);
        [InspectorName("太阳高度雾光照颜色")]
        public ColorParameter sunColor = new ColorParameter(Color.white, hdr: true, showAlpha: true, showEyeDropper: true);
        [InspectorName("月亮高度雾光照颜色")]
        public ColorParameter moonColor = new ColorParameter(Color.white, hdr: true, showAlpha: true, showEyeDropper: true);
        [InspectorName("高度雾光照范围")]
        public MinFloatParameter sunFallOff = new MinFloatParameter(1.0f, 0.1f);
        [InspectorName("高度雾光照范围")]
        public MinFloatParameter moonFallOff = new MinFloatParameter(1.0f, 0.1f);


        /*----Hide---*/
        // Optional Volumetric AtmosphericScatteringFog
        /// <summary>Enable volumetric fog.</summary>
        [HideInInspector][DisplayInfo(name = "Volumetric AtmosphericScatteringFog")]
        public BoolParameter enableVolumetricFog = new BoolParameter(false);
        // Common AtmosphericScatteringFog Parameters (Exponential/Volumetric)
        /// <summary>Stores the fog albedo. This defines the color of the fog.</summary>
        [HideInInspector]public ColorParameter albedo = new ColorParameter(Color.white);
        /// <summary>Multiplier for ambient probe contribution.</summary>
        [HideInInspector][DisplayInfo(name = "Ambient Light Probe Dimmer")]
        public ClampedFloatParameter globalLightProbeDimmer = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        /// <summary>Sets the distance (in meters) from the Camera's Near Clipping Plane to the back of the Camera's volumetric lighting buffer. The lower the distance is, the higher the fog quality is.</summary>
        [HideInInspector]public MinFloatParameter depthExtent = new MinFloatParameter(64.0f, 0.1f);
        /// <summary>Controls which denoising technique to use for the volumetric effect.</summary>
        /// <remarks>Reprojection mode is effective for static lighting but can lead to severe ghosting artifacts with highly dynamic lighting. Gaussian mode is effective with dynamic lighting. You can also use both modes together which produces high-quality results, but increases the resource intensity of processing the effect.</remarks>
        [HideInInspector][Tooltip("Specifies the denoising technique to use for the volumetric effect.")]
        public FogDenoisingModeParameter denoisingMode = new FogDenoisingModeParameter(FogDenoisingMode.Gaussian);

        // Advanced parameters
        /// <summary>Controls the angular distribution of scattered light. 0 is isotropic, 1 is forward scattering, and -1 is backward scattering.</summary>
        [HideInInspector]public ClampedFloatParameter anisotropy = new ClampedFloatParameter(0.0f, -1.0f, 1.0f);
        /// <summary>Controls the distribution of slices along the Camera's focal axis. 0 is exponential distribution and 1 is linear distribution.</summary>
        [HideInInspector][Tooltip("Controls the distribution of slices along the Camera's focal axis. 0 is exponential distribution and 1 is linear distribution.")]
        public ClampedFloatParameter sliceDistributionUniformity = new ClampedFloatParameter(0.75f, 0, 1);
        /*----Hide---*/


        // 实现接口
        public bool IsActive() => enabled.value;

        public bool IsTileCompatible()
        {
            return false;
        }

        float Interp(float from, float to, float t)
        {
            return from + (to - from) * t;
        }

        public override void Override(VolumeComponent state, float interpFactor)
        {
            // if (state == null) return;
            //
            // base.Override(state, interpFactor);
            //
            // AtmosphericScatteringFog data = state as AtmosphericScatteringFog;
            //
            // if (!data || !data.IsActive())
            //     return;
            //
            // if (tint.overrideState)
            // {
            //     data.tint.value = Color.Lerp(data.tint.value, tint.value, interpFactor);
            // }
            //
            // if (meanFreePath.overrideState)
            // {
            //     data.meanFreePath.value = Interp(data.meanFreePath.value, meanFreePath.value, interpFactor);
            // }
            //
            // if (fogStartDistance.overrideState)
            // {
            //     data.fogStartDistance.value = Interp(data.fogStartDistance.value, fogStartDistance.value, interpFactor);
            // }
            //
            // if (baseHeight.overrideState)
            // {
            //     data.baseHeight.value = Interp(data.baseHeight.value, baseHeight.value, interpFactor);
            // }
            //
            // if (maximumHeight.overrideState)
            // {
            //     data.maximumHeight.value = Interp(data.maximumHeight.value, maximumHeight.value, interpFactor);
            // }
            //
            // if (maxFogDistance.overrideState)
            // {
            //     data.maxFogDistance.value = Interp(data.maxFogDistance.value, maxFogDistance.value, interpFactor);
            // }
            //
            // if (jitterStrength.overrideState)
            // {
            //     data.jitterStrength.value = Interp(data.jitterStrength.value, jitterStrength.value, interpFactor);
            // }
            //
            // if (sunColor.overrideState)
            // {
            //     data.sunColor.value = Color.Lerp(data.sunColor.value, sunColor.value, interpFactor);
            // }
            //
            // if (moonColor.overrideState)
            // {
            //     data.moonColor.value = Color.Lerp(data.moonColor.value, moonColor.value, interpFactor);
            // }
            //
            // if (sunFallOff.overrideState)
            // {
            //     data.sunFallOff.value = Interp(data.sunFallOff.value, sunFallOff.value, interpFactor);
            // }
            //
            // if (moonFallOff.overrideState)
            // {
            //     data.moonFallOff.value = Interp(data.moonFallOff.value, moonFallOff.value, interpFactor);
            // }
            //
            // RenderSettings.fog = data.isLinearFog.value;
            // RenderSettings.fogMode = data.linearFogMode.value;
            // RenderSettings.fogColor = data.linearFogColor.value;
            // RenderSettings.fogStartDistance = data.linearFogStartDistance.value;
            // RenderSettings.fogEndDistance = data.linearFogEndDistance.value;
            // RenderSettings.fogDensity = data.linearfogDensity.value;
        }

    }

    /// <summary>
    /// AtmosphericScatteringFog Color parameter.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class FogColorParameter : VolumeParameter<FogColorMode>
    {
        /// <summary>
        /// AtmosphericScatteringFog Color Parameter constructor.
        /// </summary>
        /// <param name="value">AtmosphericScatteringFog Color Parameter.</param>
        /// <param name="overrideState">Initial override state.</param>
        public FogColorParameter(FogColorMode value, bool overrideState = false)
            : base(value, overrideState) { }
    }
    public enum FogColorMode
    {
        ConstantColor,
        SkyColor,
    }


    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class RenderModeParameter : VolumeParameter<RenderMode>
    {
        /// <summary>
        /// AtmosphericScatteringFog Color Parameter constructor.
        /// </summary>
        /// <param name="value">AtmosphericScatteringFog Color Parameter.</param>
        /// <param name="overrideState">Initial override state.</param>
        public RenderModeParameter(RenderMode value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class FogModeParameter : VolumeParameter<FogMode>
    {
        public FogModeParameter(FogMode value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    public enum RenderMode
    {
        ObjectSelf,
        PostProcess,
    }


    // [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    // public sealed class DownSamplingModeParameter : VolumeParameter<DownSamplingMode>
    // {
    //     /// <summary>
    //     /// AtmosphericScatteringFog Color Parameter constructor.
    //     /// </summary>
    //     /// <param name="value">AtmosphericScatteringFog Color Parameter.</param>
    //     /// <param name="overrideState">Initial override state.</param>
    //     public DownSamplingModeParameter(DownSamplingMode value, bool overrideState = false)
    //         : base(value, overrideState) { }
    // }
    // public enum DownSamplingMode
    // {
    //     Normal,
    //     Half,
    //     Quarter,
    // }


    /// <summary>
    /// Options that control which denoising algorithms Unity should use on the volumetric fog signal.
    /// </summary>
    public enum FogDenoisingMode
    {
        /// <summary>
        /// Use this mode to not filter the volumetric fog.
        /// </summary>
        None = 0,
        /// <summary>
        /// Use this mode to reproject data from previous frames to denoise the signal. This is effective for static lighting, but it can lead to severe ghosting artifacts for highly dynamic lighting.
        /// </summary>
        Reprojection = 1 << 0,
        /// <summary>
        /// Use this mode to reduce the aliasing patterns that can appear on the volumetric fog.
        /// </summary>
        Gaussian = 1 << 1,
        /// <summary>
        /// Use this mode to use both Reprojection and Gaussian filtering techniques. This produces high visual quality, but significantly increases the resource intensity of the effect.
        /// </summary>
        Both = Reprojection | Gaussian
    }
    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="FogDenoisingMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class FogDenoisingModeParameter : VolumeParameter<FogDenoisingMode>
    {
        /// <summary>
        /// Creates a new <see cref="FogDenoisingModeParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public FogDenoisingModeParameter(FogDenoisingMode value, bool overrideState = false) : base(value, overrideState) { }
    }

}

