using System;
// using Sirenix.OdinInspector;
// using SYPackages.GraphicsLoad;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Environment;
using UnityEngine.Serialization;

namespace SYPackages.TimeOfDay
{
    public class SunMoonCompContrl : TodCompContrl
    {
        public Light directionLight = null;
        public Transform sun = null;
        public Transform moon = null;

        public Vector3 sunRotVec = Vector3.right;
        public Vector3 moonRotVec = Vector3.right;
        [Range(0f, 360f)]
        public float SunMoonRot = 0;
        // 控制月亮升起和落下的时间曲线
        public float moonTimer = 1;
        // [HideLabel] public AnimationCurve sunIntensityCurve =
        //     new AnimationCurve(new Keyframe[2] { new Keyframe(0, 1), new Keyframe(24, 1) });
        //
        // [HideLabel] public AnimationCurve moonIntensityCurve =
        //     new AnimationCurve(new Keyframe[2] { new Keyframe(0, 1), new Keyframe(24, 1) });

        // public float _sunIntensity = 1f;
        // public float _moonIntensity = 1f;
        // public float _sunShadowIntensity = 1f;
        // public float _moonShadowIntensity = 1f;

        //private new const string ContrlKey = "SUNMOON";

        private LensFlareComponentSRP _lensFlare;

        private bool _hasRenderSettingsData = false;
        private RenderSettingsData _renderSettingData;



        private void UpdateSunMoon()
        {
            if (!directionLight) return;
            RenderSettings.sun = directionLight;

            if (!sun || !moon) return;

            var sunRotDegree = Mathf.Lerp(-90, 270, contrlInterp);
            //var sunInterp = sunIntensityCurve.Evaluate(24f * contrlInterp);
            sun.transform.rotation = Quaternion.AngleAxis(sunRotDegree, Vector3.Normalize(sunRotVec));

            var moonRotDegree = Mathf.Lerp(-270, 90, contrlInterp * moonTimer);
            //var moonInterp = moonIntensityCurve.Evaluate(24f * contrlInterp);
            moon.transform.rotation = Quaternion.AngleAxis(moonRotDegree, Vector3.Normalize(moonRotVec));

            sun.transform.rotation = Quaternion.Euler(0, SunMoonRot, 0) * sun.transform.rotation;
            moon.transform.rotation = Quaternion.Euler(0, SunMoonRot, 0) * moon.transform.rotation;

            if (TimeOfDay is >= 21600f and <= 64800f) // if <= 18 ,because of the minute, ex: 18:59 will still be the day.
            {
                directionLight.transform.rotation = sun.transform.rotation;
                //directionLight.intensity = _sunIntensity * sunInterp;
                //directionLight.shadowStrength = _sunShadowIntensity * sunInterp;

                // if (_lensFlare)
                // {
                //     _lensFlare.enabled = true;
                //     _lensFlare.intensity = sunInterp;
                // }
            }
            else
            {
                directionLight.transform.rotation = moon.transform.rotation;
                //directionLight.intensity = _moonIntensity * moonInterp;
                //directionLight.shadowStrength = _moonShadowIntensity * moonInterp;

                // if (_lensFlare)
                //     _lensFlare.enabled = false;

            }

            Vector4 dirSunWS = -sun.localToWorldMatrix.GetColumn(2);
            Vector4 dirMoonWS = -moon.localToWorldMatrix.GetColumn(2);
            // if (_hasRenderSettingsData)
            // {
            //     _renderSettingData.skybox.value.SetVector("_SunDirection", transform.InverseTransformDirection(-sun.forward));
            //     _renderSettingData.skybox.value.SetVector("_MoonDirection", transform.InverseTransformDirection(-moon.forward));
            //     _renderSettingData.skybox.value.SetMatrix("_MoonMatrix", moon.transform.worldToLocalMatrix);
            //
            //     Shader.SetGlobalVector("_SunDirectionWS", dirSunWS);
            //     Shader.SetGlobalVector("_MoonDirectionWS", dirMoonWS);
            // }
            Shader.SetGlobalVector("_SunDirectionWS", dirSunWS);
            Shader.SetGlobalVector("_MoonDirectionWS", dirMoonWS);
            Shader.SetGlobalMatrix("_MoonMatrix", moon.transform.worldToLocalMatrix);
        }

        public override void Register()
        {

            var volume = TimeOfDayManagerSingleton.Instance.TimeOfDayManager.GlobalVolume;

            if (!volume) return;

            _hasRenderSettingsData = volume.profile.TryGet<RenderSettingsData>(out _renderSettingData);

            if (directionLight)
                _lensFlare = directionLight.gameObject.GetComponent<LensFlareComponentSRP>();

            isRegister = true;
        }

        public override void UnRegister()
        {
            // sun = null;
            // moon = null;

            // directionLight.intensity = _sunIntensity;
            // directionLight.shadowStrength = _sunShadowIntensity;
            // directionLight.intensity = _moonIntensity;
            // directionLight.shadowStrength = _moonShadowIntensity;

            // _sunIntensity = 0;
            // _sunShadowIntensity = 0;
            // _moonIntensity = 0;
            // _moonShadowIntensity = 0;
            isRegister = false;
        }

        public override void Play()
        {
            base.Play();
            UpdateSunMoon();
        }

        public override void Pause()
        {
        }

        public override void Stop()
        {
        }
    }
}
