using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable]
[VolumeComponentMenu("Custom Post-processing/Diagonal Split")]
public sealed class DiagonalSplitVolume : VolumeComponent, IPostProcessComponent
{
    [Tooltip("Enable the diagonal image sequence post-process.")]
    public BoolParameter enabled = new BoolParameter(false);

    [Header("Images")]
    [Tooltip("How many image slots are used by the sequence.")]
    public ClampedIntParameter imageCount = new ClampedIntParameter(2, 1, 10);

    [Tooltip("Image 0. If left empty, the camera render is used.")]
    public TextureParameter image0 = new TextureParameter(null, TextureDimension.Tex2D);
    [Tooltip("Image 1. If left empty, the camera render is used.")]
    public TextureParameter image1 = new TextureParameter(null, TextureDimension.Tex2D);
    public TextureParameter image2 = new TextureParameter(null, TextureDimension.Tex2D);
    public TextureParameter image3 = new TextureParameter(null, TextureDimension.Tex2D);
    public TextureParameter image4 = new TextureParameter(null, TextureDimension.Tex2D);
    public TextureParameter image5 = new TextureParameter(null, TextureDimension.Tex2D);
    public TextureParameter image6 = new TextureParameter(null, TextureDimension.Tex2D);
    public TextureParameter image7 = new TextureParameter(null, TextureDimension.Tex2D);
    public TextureParameter image8 = new TextureParameter(null, TextureDimension.Tex2D);
    public TextureParameter image9 = new TextureParameter(null, TextureDimension.Tex2D);

    [Header("Diagonal Line")]
    [Tooltip("Sequence position. 0 = Image 0, 1 = Image 1, 2 = Image 2, and fractional values wipe between them.")]
    public ClampedFloatParameter splitPos = new ClampedFloatParameter(0.5f, 0f, 10f);
    [Tooltip("How slanted the line is. 0 = vertical.")]
    public ClampedFloatParameter slant = new ClampedFloatParameter(0.3f, -1f, 1f);

    [Header("Divider Appearance")]
    public ClampedFloatParameter lineWidth = new ClampedFloatParameter(0.004f, 0f, 0.1f);
    public ColorParameter lineColor = new ColorParameter(Color.white, false, true, true);
    public ClampedFloatParameter edgeSoftness = new ClampedFloatParameter(0.002f, 0f, 0.05f);

    [Header("Screen Distortion")]
    [Tooltip("Enable a blocky screen distortion around the center of the image.")]
    public BoolParameter enableScreenDistortion = new BoolParameter(false);
    [Tooltip("Controls the strength of the central blocky distortion.")]
    public ClampedFloatParameter dissolveStrength = new ClampedFloatParameter(0f, 0f, 3f);
    [Tooltip("Center of the distortion region in screen UV space.")]
    public Vector2Parameter distortionCenter = new Vector2Parameter(new Vector2(0.5f, 0.5f));
    [Tooltip("Size of the distortion region in screen UV space.")]
    public Vector2Parameter distortionSize = new Vector2Parameter(new Vector2(0.56f, 0.4f));

    [Header("Next Image")]
    [Tooltip("Enable blending from the current output to NextImg.")]
    public BoolParameter enableImageChange = new BoolParameter(false);
    [Tooltip("Target image used by ImageChange.")]
    public TextureParameter nextImg = new TextureParameter(null, TextureDimension.Tex2D);
    [Tooltip("0 keeps the current output, 1 switches fully to NextImg.")]
    public ClampedFloatParameter imageChange = new ClampedFloatParameter(0f, 0f, 1f);

    public bool IsActive()
    {
        return enabled.value && (imageCount.value > 0 || enableScreenDistortion.value || enableImageChange.value);
    }

    public bool IsTileCompatible()
    {
        return false;
    }
}
