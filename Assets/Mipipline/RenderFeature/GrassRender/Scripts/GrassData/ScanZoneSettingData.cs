using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEngine;

namespace UnityEngine.Rendering.Environment
{
    [Serializable, VolumeComponentMenu("Environment/ScanZoneSettingData")]
    public class ScanZoneSettingData : VolumeComponent
    {
        [Header("色相")] public ClampedFloatParameter Hue = new ClampedFloatParameter(0f, -0.5f, 0.5f);
        [Header("饱和度")] public ClampedFloatParameter Saturation = new ClampedFloatParameter(0f, -1f, 1f);
        [Header("明度")] public ClampedFloatParameter Value = new ClampedFloatParameter(0f, -1f, 1f);
        [Header("对比度")] public ClampedFloatParameter Contrast = new ClampedFloatParameter(1f, 1f, 1.1f);
        [Header("亮度")] public ClampedFloatParameter Brightness = new ClampedFloatParameter(1f, 1f, 30f);
        // 实现接口
        public bool IsActive() => active;

        bool isIn = false;
        public override void Override(VolumeComponent state, float interpFactor)
        {
            if (!isIn)
            {
                Shader.EnableKeyword("_SCANZONE_ON");
                isIn = true;
            }


            base.Override(state, interpFactor);
            ScanZoneSettingData data = state as ScanZoneSettingData;
            if (!data || !data.IsActive())
            {
                //Shader.SetGlobalFloat("_Hue", 0);
                //Shader.SetGlobalFloat("_Saturation", 0);
                //Shader.SetGlobalFloat("_Value", 0);
                //Shader.SetGlobalFloat("_Contrast", 1);
                //Shader.SetGlobalFloat("_Brightness", 1);
                return;
            }

            if (data.Hue.overrideState)
            {
                data.Hue.value = Hue.value * interpFactor;
                //data.Hue.Interp(data.Hue.value, Hue.value * interpFactor, interpFactor);
                Shader.SetGlobalFloat("_Hue", data.Hue.value);
            }
            else
            {
                Shader.SetGlobalFloat("_Hue", 0);
            }

            if (data.Saturation.overrideState)
            {
                data.Saturation.value = Saturation.value * interpFactor;
                //data.Saturation.Interp(data.Saturation.value, Saturation.value * interpFactor, interpFactor);
                Shader.SetGlobalFloat("_Saturation", data.Saturation.value);
            }
            else
            {
                Shader.SetGlobalFloat("_Saturation", 0);
            }

            if (data.Value.overrideState)
            {
                data.Value.value = Value.value * interpFactor;
                //data.Value.Interp(data.Value.value, Value.value * interpFactor, interpFactor);
                Shader.SetGlobalFloat("_Value", data.Value.value);
            }
            else
            {
                Shader.SetGlobalFloat("_Value", 0);
            }

            if (data.Value.overrideState)
            {
                data.Contrast.value = Contrast.value * interpFactor;
                Shader.SetGlobalFloat("_Contrast", data.Contrast.value);
            }
            else
            {
                Shader.SetGlobalFloat("_Contrast", 1);
            }

            if (data.Brightness.overrideState)
            {
                data.Brightness.value = Brightness.value * interpFactor;
                Shader.SetGlobalFloat("_Brightness", data.Brightness.value);
            }
            else
            {
                Shader.SetGlobalFloat("_Brightness", 1);
            }
        }

        // public void OutBoundRefreshStage(VolumeComponent state, float interpFactor)
        // {
        //     base.OutBoundRefreshStage(state, interpFactor);
        //     if (isIn)
        //     {
        //         Shader.DisableKeyword("_SCANZONE_ON");
        //         isIn = false;
        //     }
        // }
    }
}
