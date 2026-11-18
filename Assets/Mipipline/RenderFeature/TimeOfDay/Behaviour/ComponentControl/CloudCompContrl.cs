using System.Collections;
using System.Collections.Generic;
// using SYPackages.GraphicsLoad;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Environment;

namespace SYPackages.TimeOfDay
{
    public class CloudCompContrl : TodCompContrl
    {
        // 弃用
        // public List<CloudComp> clouds = new List<CloudComp>();
        //
        // //private new const string ContrlKey = "CLOUD";
        //
        // private bool _hasRenderSettingsData = false;
        // private RenderSettingsData _renderSettingData;
        //
        // public override void Register()
        // {
        //     var volume = TimeOfDayManagerSingleton.Instance.TimeOfDayManager.GlobalVolume;
        //
        //     if (!volume) return;
        //
        //     _hasRenderSettingsData = volume.profile.TryGet<RenderSettingsData>(out _renderSettingData);
        //
        //     isRegister = true;
        //
        // }
        //
        // public override void UnRegister()
        // {
        //     isRegister = false;
        // }
        //
        // public override void Play()
        // {
        //     base.Play();
        //
        //     foreach (var cloud in clouds)
        //     {
        //         if (!cloud) continue;
        //
        //         if (cloud.angle > 360f)
        //         {
        //             cloud.angle = 0f;
        //         }
        //         cloud.angle = cloud.rotateSpeed * Time.deltaTime;
        //         cloud.gameObject.transform.Rotate(Vector3.up,cloud.angle,Space.World);
        //
        //         Color sunColor = RenderSettings.sun.color;
        //         MeshRenderer renderer = null;
        //         var hasMat = cloud.TryGetComponent<MeshRenderer>(out renderer);
        //         if (hasMat)
        //         {
        //             if (renderer.sharedMaterial != null)
        //             {
        //                 renderer.sharedMaterial.SetColor("_SunColor",sunColor);
        //                 renderer.sharedMaterial.SetFloat("_SunIntensity",RenderSettings.sun.intensity);
        //                 renderer.sharedMaterial.SetFloat("_CloudDissolveSpeed", 0.01f);
        //
        //                 if (_hasRenderSettingsData)
        //                 {
        //                     renderer.sharedMaterial.SetFloat("_GlobalFog", _renderSettingData.cloudFog.value);
        //                     renderer.sharedMaterial.SetColor("_GlobalLightColor", _renderSettingData.cloudLightColor.value);
        //                     renderer.sharedMaterial.SetColor("_GlobalDarkColor", _renderSettingData.cloudDarkColor.value);
        //                     renderer.sharedMaterial.SetColor("_GlobalSpecColor", _renderSettingData.cloudSpecColor.value);
        //                     renderer.sharedMaterial.SetColor("_GlobalScatterColor", _renderSettingData.cloudScatterColor.value);
        //                 }
        //             }
        //         }
        //     }
        // }
        //
        // public override void Pause()
        // {
        // }
        //
        // public override void Stop()
        // {
        // }
    }
}

