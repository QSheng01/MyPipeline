using System.Collections;
using System.Collections.Generic;
using SYPackages.AtmosphericScattering;
using SYPackages.TimeOfDay;
// using SYPackages.GraphicsLoad;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Environment;
using UnityEngine.Rendering.Universal;

public class TodClipMixer : PlayableBehaviour
{
    public VolumeProfile VolumeProfile;
    
    private VolumeProfile _blendVolumeProfile;
    
    public override void OnGraphStart(Playable playable) 
    {
#if UNITY_EDITOR
        Volume[] volumes = VolumeManager.instance.GetVolumes(LayerMask.GetMask("PostProcessing"));
        foreach (var volume in volumes)
        {
            if (volume.isGlobal)
            {
                VolumeProfile = volume.profile;
            }
        }
#else
        // if (!VolumeProfile || !GraphicsManagerSingleton.Instance.MainManager)
        // {
        //     VolumeProfile = GraphicsManagerSingleton.Instance.MainManager.GetComponent<Volume>().profile;
        //     Debug.unityLogger.Log("VolumeProfile: " + VolumeProfile);
        // }
        Volume volume = TimeOfDayManagerSingleton.Instance.TimeOfDayManager.GlobalVolume;
        if (volume != null  && volume.isGlobal)
        {
            VolumeProfile = volume.profile;
        }
#endif
        
        // var Volume = VolumeManager.instance.stack;
        // var colorAd = Volume.GetComponent(typeof(ColorAdjustments));
        // var rendersettings = Volume.GetComponent(typeof(RenderSettingsData));
        // var characterLSD = Volume.GetComponent(typeof(CharacterLightSettingsData));
        // var lightingSD = Volume.GetComponent(typeof(LightingSettingsData));
        // var VoxelVolumeFog = Volume.GetComponent(typeof(VoxelVolumetricFog));
        // var AtmosphericScatteringFog = Volume.GetComponent(typeof(AtmosphericScatteringFog));
        // var WeatherSettingData = Volume.GetComponent(typeof(WeatherSettingsData));
        
        
        /*--Create Temp VolumeProfile To Mix--*/
        if (!_blendVolumeProfile)
        {
            _blendVolumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            _blendVolumeProfile.name = "TodTempVolumeProfile";
            if(VolumeProfile.Has(typeof(RenderSettingsData)))
                _blendVolumeProfile.Add<RenderSettingsData>(true);
            if(VolumeProfile.Has(typeof(CharacterLightSettingsData)))
                _blendVolumeProfile.Add<CharacterLightSettingsData>(true);
            if(VolumeProfile.Has(typeof(LightingSettingsData)))
                _blendVolumeProfile.Add<LightingSettingsData>(true);
            if(VolumeProfile.Has(typeof(VoxelVolumetricFog)))
                _blendVolumeProfile.Add<VoxelVolumetricFog>(true);
            if(VolumeProfile.Has(typeof(AtmosphericScatteringFog)))
                _blendVolumeProfile.Add<AtmosphericScatteringFog>(true);
            if(VolumeProfile.Has(typeof(WeatherSettingsData)))
                _blendVolumeProfile.Add<WeatherSettingsData>(true);
        }
        
        // Volume[] volumes = VolumeManager.instance.GetVolumes(LayerMask.GetMask("Area"));
        // Debug.unityLogger.Log("OnGraphStart:"+volumes.Length);
        // foreach (var volume in volumes)
        // {
        //     if (volume.isGlobal)
        //     {
        //         VolumeProfile = volume.sharedProfile; //todo:现在只是当前Volume,之后还要考虑Local Volume
        //     }
        // }
        // if (VolumeProfile)
        // {
        //     BlendVolumeProfile(_blendVolumeProfile, VolumeProfile, 1);
        //     Debug.unityLogger.Log("Mixer: OnGraphStart: Find the Volume Profile succesed: " + VolumeProfile);
        // }
    }
    
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        // if (!VolumeProfile)
        // {
        //     VolumeProfile = GraphicsManagerSingleton.Instance.MainManager.GetComponent<Volume>().sharedProfile;
        //     
        //     if (VolumeProfile)
        //     {
        //          BlendVolumeProfile(_blendVolumeProfile, VolumeProfile, 1);
        //         // Debug.unityLogger.Log("Mixer: ProcessFrame: Find the _blendVolumeProfile succesed: " + _blendVolumeProfile);
        //         // Debug.unityLogger.Log("Mixer: ProcessFrame: Find the Volume Profile succesed: " + VolumeProfile);
        //     }
        //     else
        //     {
        //         return;
        //     }
        //
        //      //貌似第一次查找就会返回空。
        // }

        //if (!_blendVolumeProfile || !VolumeProfile) return;
        
        var clipCount = playable.GetInputCount();
        
        for (var i = 0; i < clipCount; i++)
        {
            var weight = playable.GetInputWeight(i);
            
            if (weight > 0f)
            {
                var clipPlayable = (ScriptPlayable<TodBehaviour>)playable.GetInput(i);
                var clipPlayableBehaviour = clipPlayable.GetBehaviour();
                var clipVolumeProfile = clipPlayableBehaviour.VolumeProfile;

                if (clipVolumeProfile)
                {
                    BlendVolumeProfile(_blendVolumeProfile, clipVolumeProfile, weight);
                }
                //Debug.unityLogger.Log("Mixer: ProcessFrame: " + "clipCount:" + i + "weight: " + weight);
            }
        }
        
        //if (!VolumeProfile || !_blendVolumeProfile) return;
        BlendVolumeProfile(VolumeProfile, _blendVolumeProfile, 1);
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        if (_blendVolumeProfile)
        {
            ScriptableObject.DestroyImmediate(_blendVolumeProfile);
        }
    }

    private void BlendVolumeProfile(VolumeProfile to, VolumeProfile from, float interpFactor)
    {
        // if (to.Has<ColorAdjustments>())
        // {
        //     OverrideComponent<ColorAdjustments>(to, from, interpFactor);
        // 
        
        if (to.Has<RenderSettingsData>())
        {
            OverrideComponent<RenderSettingsData>(to, from, interpFactor);
        }
        
        if (to.Has<CharacterLightSettingsData>())
        {
            OverrideComponent<CharacterLightSettingsData>(to, from, interpFactor);
        }     
        
        if (to.Has<LightingSettingsData>())
        {
            OverrideComponent<LightingSettingsData>(to, from, interpFactor);
        }
        
        if (to.Has<VoxelVolumetricFog>())
        {
            OverrideComponent<VoxelVolumetricFog>(to, from, interpFactor);
        }
        
        if (to.Has<AtmosphericScatteringFog>())
        {
            OverrideComponent<AtmosphericScatteringFog>(to, from, interpFactor);
        }
        
        if (to.Has<WeatherSettingsData>())
        {
            OverrideComponent<WeatherSettingsData>(to, from, interpFactor);
        }
        
    }
    
    private void OverrideComponent<T>(VolumeProfile to, VolumeProfile from, float interpFactor) where T: VolumeComponent
    {
        
        T T1;
        T clipT1;
        
        to.TryGet<T>(out T1);
        from.TryGet<T>(out clipT1);
        clipT1.Override(T1,interpFactor);
    }
}
