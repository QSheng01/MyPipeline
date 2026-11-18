using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Baozang.Rendering.VolumetricClouds
{
    /// <summary>
    /// URP post-process (Volume Override) component that holds all authoring parameters for the
    /// volumetric clouds. Mirrors HDRP's `VolumetricClouds` VolumeComponent 1:1 (names / defaults /
    /// ranges / enums), plus a few URP-only extras (cloudCoverage / fog). Resources (compute shaders,
    /// noise / blue-noise textures) live on <see cref="VolumetricCloudsRenderFeature"/> instead.
    ///
    /// Parameters the current URP shader does not consume yet are tagged 【未生效】 in their tooltips.
    /// </summary>
    [Serializable]
    [VolumeComponentMenu("Custom/Volumetric Clouds")]
    public sealed class VolumetricCloudsVolume : VolumeComponent, IPostProcessComponent
    {
        //--------------------------------------------------------------------------------------
        // Enums + VolumeParameter wrappers (ported from HDRP VolumetricClouds.cs)
        //--------------------------------------------------------------------------------------

        public enum CloudControl { Simple, Advanced, Manual }
        [Serializable] public sealed class CloudControlParameter : VolumeParameter<CloudControl>
        { public CloudControlParameter(CloudControl value, bool overrideState = false) : base(value, overrideState) { } }

        public enum CloudPresets { Sparse, Cloudy, Overcast, Stormy, Custom }
        [Serializable] public sealed class CloudPresetsParameter : VolumeParameter<CloudPresets>
        { public CloudPresetsParameter(CloudPresets value, bool overrideState = false) : base(value, overrideState) { } }

        public enum CloudShadowResolution { VeryLow64 = 64, Low128 = 128, Medium256 = 256, High512 = 512, Ultra1024 = 1024 }
        [Serializable] public sealed class CloudShadowResolutionParameter : VolumeParameter<CloudShadowResolution>
        { public CloudShadowResolutionParameter(CloudShadowResolution value, bool overrideState = false) : base(value, overrideState) { } }

        public enum CloudMapResolution { Low32x32 = 32, Medium64x64 = 64, High128x128 = 128, Ultra256x256 = 256 }
        [Serializable] public sealed class CloudMapResolutionParameter : VolumeParameter<CloudMapResolution>
        { public CloudMapResolutionParameter(CloudMapResolution value, bool overrideState = false) : base(value, overrideState) { } }

        public enum CloudErosionNoise { Worley32, Perlin32 }
        [Serializable] public sealed class CloudErosionNoiseParameter : VolumeParameter<CloudErosionNoise>
        { public CloudErosionNoiseParameter(CloudErosionNoise value, bool overrideState = false) : base(value, overrideState) { } }

        public enum CloudFadeInMode { Automatic, Manual }
        [Serializable] public sealed class CloudFadeInModeParameter : VolumeParameter<CloudFadeInMode>
        { public CloudFadeInModeParameter(CloudFadeInMode value, bool overrideState = false) : base(value, overrideState) { } }

        //--------------------------------------------------------------------------------------
        // General
        //--------------------------------------------------------------------------------------
        [Tooltip("启用/关闭体积云效果")]
        public BoolParameter enable = new BoolParameter(false);                                  // HDRP: enable

        [Tooltip("局部云: 相机可穿越云层且与场景几何体深度遮挡; 关闭则为远景天空盒云")]
        public BoolParameter localClouds = new BoolParameter(false);                             // HDRP: localClouds

        [Tooltip("【未生效】控制云体曲率, 决定云与地平线相交的距离")]
        public ClampedFloatParameter earthCurvature = new ClampedFloatParameter(0.0f, 0.0f, 1.0f); // HDRP: earthCurvature

        //--------------------------------------------------------------------------------------
        // Shape - Cloud Map
        //--------------------------------------------------------------------------------------
        [Tooltip("云控制模式: Simple(预设) / Advanced(云类型分布图) / Manual(自定义云图+LUT)")]
        public CloudControlParameter cloudControl = new CloudControlParameter(CloudControl.Simple); // HDRP: cloudControl

        [Tooltip("Simple 模式下的天气预设 (在渲染时驱动形状/密度/曲线/云层高度)")]
        public CloudPresetsParameter cloudPreset = new CloudPresetsParameter(CloudPresets.Cloudy); // HDRP: cloudPreset

        [Tooltip("云图 2D - Coverage(R) Rain(G) Type(B)。Manual 模式使用; 留空则程序化生成")]
        public TextureParameter cloudMap = new TextureParameter(null, TextureDimension.Tex2D);   // HDRP: cloudMap
        [Tooltip("CloudMap 覆盖的世界宽度(米)。0 = 使用 HDRP 风格自动范围; 手绘云图建议填关卡尺度, 例如 8000 或 16000")]
        public MinFloatParameter cloudMapWorldSize = new MinFloatParameter(0.0f, 0.0f);           // URP-only
        [Tooltip("CloudMap 中心对应的世界 XZ 坐标(米)。用于让手绘云图对齐关卡中心")]
        public Vector2Parameter cloudMapWorldOffset = new Vector2Parameter(Vector2.zero);         // URP-only
        [Tooltip("云剖面 LUT 2D - Coverage(R) Erosion(G) AO(B)。Manual 模式使用; 留空则由曲线烘焙")]
        public TextureParameter cloudLut = new TextureParameter(null, TextureDimension.Tex2D);   // HDRP: cloudLut

        [Tooltip("【未生效】Advanced 模式: 低层积云分布图")]
        public TextureParameter cumulusMap = new TextureParameter(null, TextureDimension.Tex2D); // HDRP: cumulusMap
        [Tooltip("【未生效】Advanced 模式: 积云覆盖率倍率")]
        public ClampedFloatParameter cumulusMapMultiplier = new ClampedFloatParameter(1.0f, 0.0f, 1.0f); // HDRP: cumulusMapMultiplier
        [Tooltip("【未生效】Advanced 模式: 高层高积云分布图")]
        public TextureParameter altoStratusMap = new TextureParameter(null, TextureDimension.Tex2D); // HDRP: altoStratusMap
        [Tooltip("【未生效】Advanced 模式: 高积云覆盖率倍率")]
        public ClampedFloatParameter altoStratusMapMultiplier = new ClampedFloatParameter(1.0f, 0.0f, 1.0f); // HDRP: altoStratusMapMultiplier
        [Tooltip("【未生效】Advanced 模式: 砧状积雨云分布图")]
        public TextureParameter cumulonimbusMap = new TextureParameter(null, TextureDimension.Tex2D); // HDRP: cumulonimbusMap
        [Tooltip("【未生效】Advanced 模式: 积雨云覆盖率倍率")]
        public ClampedFloatParameter cumulonimbusMapMultiplier = new ClampedFloatParameter(1.0f, 0.0f, 1.0f); // HDRP: cumulonimbusMapMultiplier
        [Tooltip("【未生效】Advanced 模式: 降雨分布图")]
        public TextureParameter rainMap = new TextureParameter(null, TextureDimension.Tex2D);    // HDRP: rainMap
        [Tooltip("【未生效】Advanced 模式云图的内部烘焙分辨率")]
        public CloudMapResolutionParameter cloudMapResolution = new CloudMapResolutionParameter(CloudMapResolution.Medium64x64); // HDRP: cloudMapResolution

        //--------------------------------------------------------------------------------------
        // Shape - Density / Noise
        //--------------------------------------------------------------------------------------
        [Tooltip("云体全局密度")]
        public ClampedFloatParameter densityMultiplier = new ClampedFloatParameter(0.4f, 0.0f, 1.0f); // HDRP: densityMultiplier
        [Tooltip("穿过云覆盖率的大尺度噪声。数值越大云越少越小")]
        public ClampedFloatParameter shapeFactor = new ClampedFloatParameter(0.9f, 0.0f, 1.0f);  // HDRP: shapeFactor
        [Tooltip("大尺度形状噪声的尺寸")]
        public MinFloatParameter shapeScale = new MinFloatParameter(5.0f, 0.1f);                 // HDRP: shapeScale
        [Tooltip("评估大尺度形状噪声时施加的世界空间偏移")]
        public Vector3Parameter shapeOffset = new Vector3Parameter(Vector3.zero);               // HDRP: shapeOffset
        [Tooltip("云边缘的小尺度噪声。数值越大侵蚀越明显")]
        public ClampedFloatParameter erosionFactor = new ClampedFloatParameter(0.8f, 0.0f, 1.0f); // HDRP: erosionFactor
        [Tooltip("小尺度侵蚀噪声的尺寸")]
        public MinFloatParameter erosionScale = new MinFloatParameter(107.0f, 1.0f);             // HDRP: erosionScale
        [Tooltip("密度随归一化高度的分布曲线 (X=高度, Y=密度)")]
        public AnimationCurveParameter densityCurve = new AnimationCurveParameter(new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.15f, 1.0f), new Keyframe(1.0f, 0.1f))); // HDRP: densityCurve
        [Tooltip("侵蚀随归一化高度的分布曲线 (X=高度, Y=侵蚀)")]
        public AnimationCurveParameter erosionCurve = new AnimationCurveParameter(new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.1f, 0.9f), new Keyframe(1.0f, 1.0f))); // HDRP: erosionCurve
        [Tooltip("环境光遮蔽随归一化高度的分布曲线 (X=高度, Y=AO)")]
        public AnimationCurveParameter ambientOcclusionCurve = new AnimationCurveParameter(new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.25f, 0.4f), new Keyframe(1.0f, 0.0f))); // HDRP: ambientOcclusionCurve

        [Tooltip("URP 扩展: 程序化云图的整体覆盖率 (未提供 cloudMap 时使用)")]
        public ClampedFloatParameter cloudCoverage = new ClampedFloatParameter(0.5f, 0.0f, 1.0f); // URP-only

        //--------------------------------------------------------------------------------------
        // Cloud Layer
        //--------------------------------------------------------------------------------------
        [Tooltip("云层底部海拔 (米)")]
        public MinFloatParameter bottomAltitude = new MinFloatParameter(1200.0f, 0.01f);         // HDRP: bottomAltitude
        [Tooltip("云层厚度 (米)")]
        public MinFloatParameter altitudeRange = new MinFloatParameter(2000.0f, 100.0f);         // HDRP: altitudeRange
        [Tooltip("云在靠近相机近平面时的淡入模式")]
        public CloudFadeInModeParameter fadeInMode = new CloudFadeInModeParameter(CloudFadeInMode.Automatic); // HDRP: fadeInMode
        [Tooltip("云开始出现的最小距离 (Manual 淡入模式)")]
        public MinFloatParameter fadeInStart = new MinFloatParameter(0.0f, 0.0f);                // HDRP: fadeInStart
        [Tooltip("云达到完整密度所需的距离 (Manual 淡入模式)")]
        public MinFloatParameter fadeInDistance = new MinFloatParameter(0.0f, 0.0f);            // HDRP: fadeInDistance

        //--------------------------------------------------------------------------------------
        // Wind
        //--------------------------------------------------------------------------------------
        [Tooltip("全局水平风速 (km/h)")]
        public MinFloatParameter globalWindSpeed = new MinFloatParameter(10.0f, 0.0f);           // HDRP: globalWindSpeed
        [Tooltip("风向相对 X 世界向量的朝向 (度)")]
        public ClampedFloatParameter orientation = new ClampedFloatParameter(0.0f, 0.0f, 360.0f); // HDRP: orientation
        [Tooltip("基于风的高度扭曲强度")]
        public ClampedFloatParameter altitudeDistortion = new ClampedFloatParameter(0.25f, -1.0f, 1.0f); // HDRP: altitudeDistortion
        [Tooltip("云图移动速度倍率")]
        public ClampedFloatParameter cloudMapSpeedMultiplier = new ClampedFloatParameter(0.5f, 0.0f, 1.0f); // HDRP: cloudMapSpeedMultiplier
        [Tooltip("大尺度形状移动速度倍率")]
        public ClampedFloatParameter shapeSpeedMultiplier = new ClampedFloatParameter(1.0f, 0.0f, 1.0f); // HDRP: shapeSpeedMultiplier
        [Tooltip("侵蚀形状移动速度倍率")]
        public ClampedFloatParameter erosionSpeedMultiplier = new ClampedFloatParameter(0.25f, 0.0f, 1.0f); // HDRP: erosionSpeedMultiplier
        [Tooltip("大尺度形状的垂直风速")]
        public FloatParameter verticalShapeWindSpeed = new FloatParameter(0.0f);                 // HDRP: verticalShapeWindSpeed
        [Tooltip("侵蚀形状的垂直风速")]
        public FloatParameter verticalErosionWindSpeed = new FloatParameter(0.0f);               // HDRP: verticalErosionWindSpeed

        //--------------------------------------------------------------------------------------
        // Lighting
        //--------------------------------------------------------------------------------------
        [Tooltip("云散射颜色的染色")]
        public ColorParameter scatteringTint = new ColorParameter(new Color(0.0f, 0.0f, 0.0f, 1.0f), false, false, true); // HDRP: scatteringTint
        [Tooltip("云内局部散射量, 越高越有粉状/漫射感")]
        public ClampedFloatParameter powderEffectIntensity = new ClampedFloatParameter(0.25f, 0.0f, 1.0f); // HDRP: powderEffectIntensity
        [Tooltip("云内多重散射量")]
        public ClampedFloatParameter multiScattering = new ClampedFloatParameter(0.5f, 0.0f, 1.0f); // HDRP: multiScattering
        [Tooltip("光照探针(环境光)对云的影响, 越低云越暗")]
        public ClampedFloatParameter ambientLightProbeDimmer = new ClampedFloatParameter(1.0f, 0.0f, 1.0f); // HDRP: ambientLightProbeDimmer
        [Tooltip("太阳光对云的影响, 越低云越暗")]
        public ClampedFloatParameter sunLightDimmer = new ClampedFloatParameter(1.0f, 0.0f, 1.0f); // HDRP: sunLightDimmer
        [Tooltip("计算 AO 时侵蚀因子的权重")]
        public ClampedFloatParameter erosionOcclusion = new ClampedFloatParameter(0.1f, 0.0f, 1.0f); // HDRP: erosionOcclusion

        //--------------------------------------------------------------------------------------
        // Quality / Performance
        //--------------------------------------------------------------------------------------
        [Tooltip("评估云透过率的步数。越高噪点越低、视距越远, 但更贵")]
        public ClampedIntParameter numPrimarySteps = new ClampedIntParameter(64, 32, 1024);      // HDRP: numPrimarySteps
        [Tooltip("评估云光照的步数。越高自阴影越平滑, 但更贵")]
        public ClampedIntParameter numLightSteps = new ClampedIntParameter(6, 1, 32);            // HDRP: numLightSteps
        [Tooltip("时间累积因子。越高质量越好但可能产生拖影")]
        public ClampedFloatParameter temporalAccumulationFactor = new ClampedFloatParameter(0.95f, 0.0f, 1.0f); // HDRP: temporalAccumulationFactor
        [Tooltip("去拖影。开启可显著减少拖影, 但低累积因子下可能闪烁")]
        public BoolParameter ghostingReduction = new BoolParameter(false);                       // HDRP: ghostingReduction
        [Tooltip("感知混合强度, 建议只取 0 或 1")]
        public ClampedFloatParameter perceptualBlending = new ClampedFloatParameter(1.0f, 0.0f, 1.0f); // HDRP: perceptualBlending

        //--------------------------------------------------------------------------------------
        // Shadows (【未生效】)
        //--------------------------------------------------------------------------------------
        [Tooltip("【未生效】启用体积云阴影(会覆盖方向光 cookie)")]
        public BoolParameter shadows = new BoolParameter(false);                                 // HDRP: shadows
        [Tooltip("【未生效】体积云阴影贴图分辨率")]
        public CloudShadowResolutionParameter shadowResolution = new CloudShadowResolutionParameter(CloudShadowResolution.Medium256); // HDRP: shadowResolution
        [Tooltip("【未生效】计算云阴影的垂直偏移(米)")]
        public FloatParameter shadowPlaneHeightOffset = new FloatParameter(0.0f);                // HDRP: shadowPlaneHeightOffset
        [Tooltip("【未生效】相机周围阴影覆盖区域大小")]
        public MinFloatParameter shadowDistance = new MinFloatParameter(8000.0f, 1000.0f);       // HDRP: shadowDistance
        [Tooltip("【未生效】体积云阴影的不透明度")]
        public ClampedFloatParameter shadowOpacity = new ClampedFloatParameter(1.0f, 0.0f, 1.0f); // HDRP: shadowOpacity
        [Tooltip("【未生效】阴影覆盖区域外的阴影不透明度")]
        public ClampedFloatParameter shadowOpacityFallback = new ClampedFloatParameter(0.0f, 0.0f, 1.0f); // HDRP: shadowOpacityFallback

        //--------------------------------------------------------------------------------------
        // Compositing (URP-only extras)
        //--------------------------------------------------------------------------------------
        [Tooltip("URP 扩展: 云雾距离, 0 关闭")]
        public MinFloatParameter fogDistance = new MinFloatParameter(0f, 0f);                    // URP-only
        [Tooltip("URP 扩展: 云雾颜色")]
        public ColorParameter fogColor = new ColorParameter(Color.gray, false, false, true);    // URP-only

        //--------------------------------------------------------------------------------------
        // IPostProcessComponent
        //--------------------------------------------------------------------------------------
        public bool IsActive() => enable.value;
        public bool IsTileCompatible() => false;

        //--------------------------------------------------------------------------------------
        // Effective shape/density values (preset-driven in Simple mode, à la HDRP)
        //--------------------------------------------------------------------------------------
        public struct EffectiveShape
        {
            public float densityMultiplier, shapeFactor, shapeScale, erosionFactor, erosionScale;
            public float bottomAltitude, altitudeRange;
            public AnimationCurve densityCurve, erosionCurve, ambientOcclusionCurve;
        }

        /// <summary>
        /// Returns the shape/density/curve/altitude values the ray marcher needs. These are simply
        /// the authored parameter values: selecting a preset writes its values into the parameters
        /// (see <see cref="ApplyPreset"/>), so "what you see is what renders".
        /// </summary>
        public EffectiveShape GetEffectiveShape()
        {
            EffectiveShape e;
            e.densityMultiplier = densityMultiplier.value;
            e.shapeFactor = shapeFactor.value;
            e.shapeScale = shapeScale.value;
            e.erosionFactor = erosionFactor.value;
            e.erosionScale = erosionScale.value;
            e.bottomAltitude = bottomAltitude.value;
            e.altitudeRange = altitudeRange.value;
            e.densityCurve = densityCurve.value;
            e.erosionCurve = erosionCurve.value;
            e.ambientOcclusionCurve = ambientOcclusionCurve.value;
            return e;
        }

        /// <summary>
        /// Writes the current preset's shape/density/curve/altitude values into the corresponding
        /// parameters (and enables their override state), mirroring HDRP's cloudPreset setter.
        /// Does nothing for the Custom preset. Call this when the preset selection changes.
        /// </summary>
        public void ApplyPreset()
        {
            if (cloudPreset.value == CloudPresets.Custom)
                return;

            FillPreset(cloudPreset.value, out var e);
            SetFloat(densityMultiplier, e.densityMultiplier);
            SetFloat(shapeFactor, e.shapeFactor);
            SetFloat(shapeScale, e.shapeScale);
            SetFloat(erosionFactor, e.erosionFactor);
            SetFloat(erosionScale, e.erosionScale);
            SetFloat(bottomAltitude, e.bottomAltitude);
            SetFloat(altitudeRange, e.altitudeRange);
            densityCurve.value = e.densityCurve; densityCurve.overrideState = true;
            erosionCurve.value = e.erosionCurve; erosionCurve.overrideState = true;
            ambientOcclusionCurve.value = e.ambientOcclusionCurve; ambientOcclusionCurve.overrideState = true;
        }

        static void SetFloat(FloatParameter p, float v)
        {
            p.value = v;
            p.overrideState = true;
        }

        static void FillPreset(CloudPresets preset, out EffectiveShape e)
        {
            e = default;
            e.shapeScale = 5.0f;
            switch (preset)
            {
                case CloudPresets.Sparse:
                    e.densityMultiplier = 0.4f; e.shapeFactor = 0.95f; e.erosionFactor = 0.8f; e.erosionScale = 107.0f;
                    e.densityCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.05f, 1.0f), new Keyframe(0.75f, 1.0f), new Keyframe(1.0f, 0.0f));
                    e.erosionCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.1f, 0.9f), new Keyframe(1.0f, 1.0f));
                    e.ambientOcclusionCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.25f, 0.5f), new Keyframe(1.0f, 0.0f));
                    e.bottomAltitude = 3000.0f; e.altitudeRange = 1000.0f;
                    break;
                case CloudPresets.Overcast:
                    e.densityMultiplier = 0.3f; e.shapeFactor = 0.5f; e.erosionFactor = 0.8f; e.erosionScale = 107.0f;
                    e.densityCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.05f, 1.0f), new Keyframe(0.9f, 0.0f), new Keyframe(1.0f, 0.0f));
                    e.erosionCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.1f, 0.9f), new Keyframe(1.0f, 1.0f));
                    e.ambientOcclusionCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1.0f, 0.0f));
                    e.bottomAltitude = 1500.0f; e.altitudeRange = 2500.0f;
                    break;
                case CloudPresets.Stormy:
                    e.densityMultiplier = 0.35f; e.shapeFactor = 0.85f; e.erosionFactor = 0.749f; e.erosionScale = 107.0f;
                    e.densityCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.037f, 1.0f), new Keyframe(0.6f, 1.0f), new Keyframe(1.0f, 0.0f));
                    e.erosionCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.05f, 0.8f), new Keyframe(0.2438f, 0.9498f), new Keyframe(0.5f, 1.0f), new Keyframe(0.93f, 0.9268f), new Keyframe(1.0f, 1.0f));
                    e.ambientOcclusionCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.1f, 0.4f), new Keyframe(1.0f, 0.0f));
                    e.bottomAltitude = 1000.0f; e.altitudeRange = 5000.0f;
                    break;
                default: // Cloudy
                    e.densityMultiplier = 0.4f; e.shapeFactor = 0.9f; e.erosionFactor = 0.8f; e.erosionScale = 107.0f;
                    e.densityCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.15f, 1.0f), new Keyframe(1.0f, 0.1f));
                    e.erosionCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.1f, 0.9f), new Keyframe(1.0f, 1.0f));
                    e.ambientOcclusionCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.25f, 0.4f), new Keyframe(1.0f, 0.0f));
                    e.bottomAltitude = 1200.0f; e.altitudeRange = 2000.0f;
                    break;
            }
        }
    }
}
