using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VoxelVolumetricFog : VolumeComponent, IPostProcessComponent
{
    [Header("Advanced Options")]
    public VolumetricFogModeParameter mode = new VolumetricFogModeParameter(VolumetricFogMode.Off);
    public NoInterpMinIntParameter gridPixelSize = new NoInterpMinIntParameter(9, 2);
    public NoInterpClampedIntParameter gridSizeZ = new NoInterpClampedIntParameter(256, 32, 256);
    public QualityLevelParameter qualityLevel = new QualityLevelParameter(QualityLevel.Medium);
    
    private Vector3Int volumeSize = Vector3Int.zero;

    public static class ReferenceDimensions
    {
        public static Vector2Int ScreenSize = new Vector2Int(1920, 1080);
    }

    public Vector3Int VolumeSize
    {
        get
        {
            volumeSize.x = ReferenceDimensions.ScreenSize.x / gridPixelSize.value;
            volumeSize.y = ReferenceDimensions.ScreenSize.y / gridPixelSize.value;
            volumeSize.z = gridSizeZ.value;
            return volumeSize;
        }
    }

    //体素尺寸
    public Vector3Int GetVolumeSize(Vector2Int screenDimensions)
    {
        volumeSize.x = Mathf.Max(screenDimensions.x / gridPixelSize.value,160);
        volumeSize.y = Mathf.Max(screenDimensions.y / gridPixelSize.value,90);
        volumeSize.z = Mathf.Max(gridSizeZ.value,64);
        return volumeSize;
    }

    public Vector3 GetCellSize(Vector3 volumeSize)
    {
        return new Vector3(1.0f / volumeSize.x, 1.0f / volumeSize.y, 1.0f / volumeSize.z);
    }

    // Performance and baseline rendering
    public MinFloatParameter maxDistanceVolumetric = new MinFloatParameter(64, 10);
    public MinFloatParameter depthRatio = new MinFloatParameter(2.0f, 0.5f);

    public DenoisingModeParameter denoisingMode = new DenoisingModeParameter(DenoisingMode.Off);
    public ClampedFloatParameter temporalAALighting = new ClampedFloatParameter(0.1f, 0f, 1f);
    public ClampedFloatParameter temporalAAMedia = new ClampedFloatParameter(0.1f, 0f, 1f);

    // Fog Parameters
    public MinFloatParameter fogDensity = new MinFloatParameter(1, 0);
    public ColorParameter fogAlbedo = new ColorParameter(Color.white, true, false, false);
    public ClampedFloatParameter anisotropy = new ClampedFloatParameter(0.2f, -1, 1);
    public MinFloatParameter fogAttenuation =  new MinFloatParameter(100.0f, 0);
    public MinFloatParameter lightIntensity = new MinFloatParameter(1, 0);
    public MinFloatParameter shadowIntensity = new MinFloatParameter(1, 0);
    
    public BoolParameter overrideDefaultMaxLightDistance = new BoolParameter(true);
    public MinFloatParameter maxLightDistance = new MinFloatParameter(20f, 0f);

    // Geometry
    public FloatParameter baseHeight = new FloatParameter(50);
    public MinFloatParameter attenuationBoundarySize = new MinFloatParameter(5, 1);

    // Custom Colors
    public ColorParameter litColor = new ColorParameter(Color.white, true, false, false);
    public ColorParameter shadowedColor = new ColorParameter(Color.black, true, false, false);
    public ColorParameter emitColor = new ColorParameter(Color.black, true, false, false);
    public ClampedFloatParameter colorInfluence = new ClampedFloatParameter(0, 0, 1);

    // Noise
    public BoolParameter useNoise = new BoolParameter(false);
    public Texture3DParameter noiseTexture = new Texture3DParameter(null);
    public MinFloatParameter noiseTiling = new MinFloatParameter(30, 0);
    public Vector3Parameter noiseWindSpeed = new Vector3Parameter(new Vector3(0, -1, 0));
    public FloatRangeParameter noiseReMap = new FloatRangeParameter(new Vector2(0, 1), 0, 1);

    // 额外GodRayPass
    public ColorParameter godrayColor = new ColorParameter(Color.white, true, false, false);   // 体积光颜色
    public ClampedFloatParameter godrayIntensity = new ClampedFloatParameter(0f, 0f, 1f);                // 体积光强度
    public ClampedFloatParameter extinctionCoef = new ClampedFloatParameter(0f, 0.0f, 1f);
    public ClampedFloatParameter skyBoxExtinction = new ClampedFloatParameter(0f, 0.0f, 1f);                 
    public ClampedFloatParameter godrayLength = new ClampedFloatParameter(64,16,256);                       // 最大渲染距离
    public ClampedFloatParameter godrayMieG = new ClampedFloatParameter(0.5f,0,1);  
    public ClampedIntParameter  maxStep = new ClampedIntParameter (64,8,256);                               // 最大步进次数
    // 双边滤波的属性
    public ClampedFloatParameter Space_S = new ClampedFloatParameter(0.1f, 0.1f, 5f);
    public ClampedFloatParameter Space_R = new ClampedFloatParameter(0.3f, 0.1f, 5f);
    //public ClampedFloatParameter KernelSize = new ClampedFloatParameter(0.5f, 0.1f, 30f);
    
    public bool IsActive()
    {
        if (mode.value != VolumetricFogMode.On)
            return false;

        return true;
    }

    public bool IsTileCompatible() => false;
}

public enum VolumetricFogMode
{
    Off,
    On
}

[Serializable]
public sealed class VolumetricFogModeParameter : VolumeParameter<VolumetricFogMode>
{
    public VolumetricFogModeParameter(VolumetricFogMode value, bool overrideState = false)
        : base(value, overrideState) { }
}

public enum QualityLevel
{
    Low,
    Medium,
    High,
}

[Serializable]
public sealed class QualityLevelParameter : VolumeParameter<QualityLevel>
{
    public QualityLevelParameter(QualityLevel value, bool overrideState = false)
        : base(value, overrideState) { }
}

public enum DenoisingMode
{
    Off,
    Reprojection,
}

[Serializable]
public sealed class DenoisingModeParameter : VolumeParameter<DenoisingMode>
{
    public DenoisingModeParameter(DenoisingMode value, bool overrideState = false)
        : base(value, overrideState) { }
}
