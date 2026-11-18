using UnityEngine;
using System;
// using Sirenix.OdinInspector;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GTM.URP.SunShaft
{
    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <c>LayerMask</c> value.
    /// </summary>
    [Serializable, System.Diagnostics.DebuggerDisplay(k_DebuggerDisplay)]
    public class RenderPassEventParameter : VolumeParameter<RenderPassEvent>
    {
        /// <summary>
        /// Creates a new <see cref="RenderPassEventParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public RenderPassEventParameter(RenderPassEvent value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    [Serializable, VolumeComponentMenu("Post-processing/SunShafts")]
    public class SunShafts : VolumeComponent, IPostProcessComponent
    {
        [InspectorName("开启光束效果")]
        public BoolParameter on = new BoolParameter(false, false);

        [Tooltip("屏幕边缘也依然生效"),InspectorName("强制开启")]
        public BoolParameter forceOn = new BoolParameter(false, false);
        [InspectorName("光束强度")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0.2f, 0f, 5f);
        
        [HideInInspector]
        public BoolParameter useRenderPassEvent = new BoolParameter(false);
        [HideInInspector]
        public RenderPassEventParameter renderPassEvent = new RenderPassEventParameter(RenderPassEvent.AfterRenderingSkybox);
        [HideInInspector]
        public BoolParameter useStencilMaskTex = new BoolParameter(false);
        


        [InspectorName("套用直线光颜色")]
        public BoolParameter useSunLightColor = new BoolParameter(true);
        [InspectorName("光束颜色")]
        public ColorParameter shaftsColor = new ColorParameter(Color.black);
        [InspectorName("噪声强度")]
        public ClampedFloatParameter shaftsNoiseIntensity = new ClampedFloatParameter(0f, 0f, 1f);
        
        [HideInInspector]
        public BoolParameter useSunPosition = new BoolParameter(false);
        [HideInInspector]
        public Vector3Parameter sunPosition = new Vector3Parameter(Vector3.zero);
        
        [InspectorName("亮度阈值")]
        public ClampedFloatParameter sunThresholdSky = new ClampedFloatParameter(0.25f, 0f, 1f);
        
        [InspectorName("降采样")]
        public ClampedIntParameter depthDownscalePow2 = new ClampedIntParameter(0, 0, 4);
        [InspectorName("模糊半径")]
        public FloatParameter blurRadius = new FloatParameter(0.6f);
        [InspectorName("模糊迭代次数")]
        public ClampedIntParameter blurStepsCount = new ClampedIntParameter(3, 1, 4);

        public bool IsActive()
        {
            return (bool)on;
        }

        public bool IsTileCompatible() => false;
    }
}

