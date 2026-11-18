using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using WaterScreenSpacePlanarReflection;

[Serializable, VolumeComponentMenu("Custom Post-Processing/PlanarReflection")]
public class PlanarReflection : VolumeComponent,IPostProcessComponent
{
    //PlanarReflection
    [Tooltip("开启平面反射"),InspectorName("开启平面反射")]
    public BoolParameter enabled = new BoolParameter(false);
    
    [Tooltip("反射质量"),InspectorName("反射质量")]
    public PlanarReflectionQuality quality = new PlanarReflectionQuality(PlanarReflectionFeature.HairShadowQuality.x1);
    
    [Tooltip("反射平面高度"), InspectorName("反射平面高度")]
    public FloatParameter planarHeight = new FloatParameter(50.0f);

    [Tooltip("是否反射天空"), InspectorName("是否反射天空")]
    public BoolParameter renderSkyBox = new BoolParameter(true);
    
    [Tooltip("开启模糊处理"), InspectorName("开启模糊处理")]
    public BoolParameter openBlur = new BoolParameter(true);
    
    //SSPR
    [Tooltip("开启SSPR"),InspectorName("开启SSPR")]
    public BoolParameter SSPREnabled = new BoolParameter(false);
    
    [Tooltip("反射天空盒"),InspectorName("反射天空盒")]
    public BoolParameter SSPR_renderSkyBox = new BoolParameter(false);
    
    [Tooltip("反射天空盒质量"),InspectorName("反射天空盒质量")]
    public SSPRQuality SSPR_renderSkyBox_Qulity = new SSPRQuality(WaterScreenSpacePlanarReflectionFeature.Quality.x1);
    
    [Tooltip("反射质量"),InspectorName("反射质量")]
    public SSPRQuality ssprquality = new SSPRQuality(WaterScreenSpacePlanarReflectionFeature.Quality.x1);

    [Tooltip("屏幕边框垂直淡出"), InspectorName("屏幕边框垂直淡出")]
    [Range(0,1)]public FloatParameter fadeOutScreenBorderWidthVerticle = new ClampedFloatParameter(0.125f,0,1);
    
    [Tooltip("屏幕边框水平淡出"), InspectorName("屏幕边框水平淡出")]
    [Range(0,1)]public FloatParameter fadeOutScreenBorderWidthHorizontal = new ClampedFloatParameter(0.329f,0,1);

    [Tooltip("模糊半径"), InspectorName("模糊半径")]
    public FloatParameter blurRadius = new ClampedFloatParameter(1.0f,0,5);
    public bool IsActive() => enabled.value;
    public bool IsTileCompatible()
    {
        return false;
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class PlanarReflectionQuality : VolumeParameter<PlanarReflectionFeature.HairShadowQuality>
    {
        public PlanarReflectionQuality(PlanarReflectionFeature.HairShadowQuality value, bool overrideState = false) : base(value, overrideState)
        {
        }
    }
    
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class SSPRQuality : VolumeParameter<WaterScreenSpacePlanarReflectionFeature.Quality>
    {
        public SSPRQuality(WaterScreenSpacePlanarReflectionFeature.Quality value, bool overrideState = false) : base(value, overrideState)
        {
        }
    }
}
