using System;
// using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class SunMoonComponent : TodComponent
{
    public Light sun;
    public Light moon; 
    
    public Vector3 sunRotVec = Vector3.right;
    public Vector3 moonRotVec = Vector3.right;
    
    // [HideLabel] 
    public AnimationCurve sunIntensityCurve = new AnimationCurve(new Keyframe[2] { new Keyframe(0, 1), new Keyframe(24, 1) });
    // [HideLabel] 
    public AnimationCurve moonIntensityCurve = new AnimationCurve(new Keyframe[2] { new Keyframe(0, 1), new Keyframe(24, 1) });

    protected override string key { get; set; } = "SunMoon";

    public override void Play()
    {
    }

    public override void Pause()
    {
    }

    public override void Stop()
    {
    }
    
}
