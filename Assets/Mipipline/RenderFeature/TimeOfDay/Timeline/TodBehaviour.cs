using System.Collections;
using System.Collections.Generic;
// using SYPackages.GraphicsLoad;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Environment;
using UnityEngine.Rendering.Universal;

public class TodBehaviour : PlayableBehaviour
{
    public VolumeProfile VolumeProfile;

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        base.OnBehaviourPlay(playable, info);
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        base.OnBehaviourPause(playable, info);
    }
    
    // public override void OnGraphStart(Playable playable)
    // {
    //     _mainVolumeProfile = GraphicsManagerSingleton.Instance.MainVolume.sharedProfile; //todo:现在只是当前Volume,之后还要考虑Local Volume
    //     if (VolumeProfile)
    //     {
    //         Debug.unityLogger.Log("Clip: OnGraphStart : Find the Volume Profile succesed: " + VolumeProfile);
    //     }
    // }
    //
    // private void OverrideComponent<T>(VolumeProfile curTodClipVolumeProfile) where T: VolumeComponent
    // {
    //     T T1;
    //     T clipT1;
    //     _mainVolumeProfile.TryGet<T>(out T1);
    //     curTodClipVolumeProfile.TryGet<T>(out clipT1);
    //     clipT1.Override(T1,1);
    // }
    //
    // public override void OnBehaviourPlay(Playable playable, FrameData info)
    // {
    //     var curTodClipPlayable = (ScriptPlayable<TodBehaviour>)playable;
    //     var curTodClipBehaviour = curTodClipPlayable.GetBehaviour();
    //     var curTodClipVolumeProfile = curTodClipBehaviour.VolumeProfile;
    //     
    //     if (VolumeProfile.Has<ColorAdjustments>())
    //     {
    //         OverrideComponent<ColorAdjustments>(curTodClipVolumeProfile);
    //     }
    //     
    //     if (VolumeProfile.Has<RenderSettingsData>())
    //     {
    //         OverrideComponent<RenderSettingsData>(curTodClipVolumeProfile);
    //     }
    //     
    //     if (VolumeProfile.Has<LightingSettingsData>())
    //     {
    //         OverrideComponent<LightingSettingsData>(curTodClipVolumeProfile);
    //     }
    //     Debug.unityLogger.Log("Clip: OverrideProfile: " + VolumeProfile);
    // }
}
