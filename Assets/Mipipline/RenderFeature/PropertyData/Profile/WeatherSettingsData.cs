using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.Environment
{
[Serializable, VolumeComponentMenu("Environment/WeatherSettingsData")]

    public class WeatherSettingsData : VolumeComponent
    {
        [FormerlySerializedAs("RainTrigger")] public BoolParameter LightsTrigger = new BoolParameter(false);
            
        public RainParticleParameter RainParticleParameter = new RainParticleParameter(null, false);
        
        public FloatParameter globalControl = new ClampedFloatParameter(1f, 0f, 1f);

        public ColorParameter cloudAmbientColor = new ColorParameter(Color.white);
        
        public FloatParameter cloudFogControl = new ClampedFloatParameter(1f, 0f, 1f);
        
        public FloatParameter cloudAlphaControl = new ClampedFloatParameter(1f, 0f, 1f); 
        
        private readonly int _globalEmissionIntensity = Shader.PropertyToID("_EmissionScaleGlobal");
        private readonly int _cloudPara1 = Shader.PropertyToID("_WeatherCloudParam");
        private readonly int _cloudPara2 = Shader.PropertyToID("_WeatherCloudAlpha");
        
        
        // 实现接口
        public bool IsActive() => active;

        private void ResetBuffer()
        {
            var cloudBlendColor = Color.white;
            var cloudBlendFog = 1f;
            Vector4 cloudParam = new Vector4(cloudBlendColor.r, cloudBlendColor.g, cloudBlendColor.b, cloudBlendFog);
            Shader.SetGlobalFloat(_globalEmissionIntensity,0);
            if (!RainParticleParameter._rainParticle) return;
            DestroyImmediate(RainParticleParameter._rainParticle);
        }

        private void UpdateRain(WeatherSettingsData data,float rainInterp,float interpFactor)
        {
            return;
            if (!LightsTrigger.value)
            {
                if (data.RainParticleParameter._rainParticle)
                {
                    DestroyImmediate(data.RainParticleParameter._rainParticle);
                }
                return;
            }
            
            /*Rain*/
            if (!data.RainParticleParameter._rainParticle)
            {
                GameObject rainPrefab = Resources.Load(RainParticleParameter.value.name) as GameObject;
                
                data.RainParticleParameter._rainParticle = Instantiate(rainPrefab, new Vector3(0f, 0f, 0f),
                    Quaternion.identity);
                
                
                // data.RainParticleParameter._rainParticles = data.RainParticleParameter._rainParticle.gameObject.GetComponentsInChildren<ParticleSystem>();
                // data.RainParticleParameter._overTimeRates.Clear();
                // foreach (var particle in data.RainParticleParameter._rainParticles)
                // {
                //     float rateOverTime = particle.emission.rateOverTime.constant;
                //     data.RainParticleParameter._overTimeRates.Add(rateOverTime);
                //     particle.Play();
                // }
            }
            // else
            // {
            //     if (interpFactor >= 0)
            //     {
            //         var i = 0;
            //         foreach (var particle in data.RainParticleParameter._rainParticles)
            //         {
            //             var emissionModule = particle.emission;
            //             emissionModule.rateOverTime = Mathf.Lerp(0, data.RainParticleParameter._overTimeRates[i], rainInterp);
            //             i++;
            //         }
            //     }
            // }

            /*ripple*/
            // Shader.SetGlobalFloat(_ripplePara,rainInterp);
        }

        private void UpdateCloud(WeatherSettingsData data, float interpFactor)
        {   
            /*cloud*/
            var cloudBlendColor = Color.Lerp(Color.white, cloudAmbientColor.value, interpFactor);
            var cloudBlendFog = Mathf.Lerp(1f, cloudFogControl.value, interpFactor);
            Vector4 cloudParam = new Vector4(cloudBlendColor.r, cloudBlendColor.g, cloudBlendColor.b, cloudBlendFog);
            Shader.SetGlobalVector(_cloudPara1,cloudParam);
            Shader.SetGlobalFloat(_cloudPara2,cloudAlphaControl.value);
        }
        
        protected override void OnDestroy()
        {
            ResetBuffer();
            base.OnDestroy();
        }
        
        protected override void OnDisable()
        {
            ResetBuffer();
            base.OnDisable();
        }
        
        public void OutBoundRefreshStage(VolumeComponent state, float interpFactor)
        {
            // base.OutBoundRefreshStage(state, interpFactor);
            
            var data = state as WeatherSettingsData;
            
            if (!data || !data.IsActive())
            {
                return;
            }
            
            if (Math.Abs(interpFactor - 1) > 0.0001) return;
            
            data.RainParticleParameter._overTimeRates.Clear();

            if (LightsTrigger.value)
            {
                Shader.SetGlobalFloat(_globalEmissionIntensity,1);
            }
            else
            {
                Shader.SetGlobalFloat(_globalEmissionIntensity,0);
            }
            
            
            var cloudBlendColor = Color.Lerp(cloudAmbientColor.value, Color.white, interpFactor);
            var cloudBlendFog = Mathf.Lerp(cloudFogControl.value, 1f, interpFactor);
            Vector4 cloudParam = new Vector4(cloudBlendColor.r, cloudBlendColor.g, cloudBlendColor.b, cloudBlendFog);
            Shader.SetGlobalVector(_cloudPara1,cloudParam);
            Shader.SetGlobalFloat(_cloudPara2,1);
            if (!data.RainParticleParameter._rainParticle) return;
            DestroyImmediate(data.RainParticleParameter._rainParticle);
            
            
            
        }
        
        public override void Override(VolumeComponent state, float interpFactor)
        {
            base.Override(state, interpFactor);
            
            var data = state as WeatherSettingsData;
            
            if (!data || !data.IsActive())
            {
                return;
            }
            
            if (LightsTrigger.value)
            {
                Shader.SetGlobalFloat(_globalEmissionIntensity,1);
            }
            else
            {
                Shader.SetGlobalFloat(_globalEmissionIntensity,0);
            }
            
            data.globalControl.Interp(globalControl.value,data.globalControl.value,interpFactor);
            
            var rainInterp = data.globalControl.value * interpFactor;

            UpdateRain(data,rainInterp,interpFactor);
            
            UpdateCloud(data,interpFactor);
        }
    }
    
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class RainParticleParameter : VolumeParameter<GameObject>
    {
        public GameObject _rainParticle = null;

        public ParticleSystem[] _rainParticles;

        public List<float> _overTimeRates = new List<float>();
        
        public RainParticleParameter(GameObject rainFX, bool overrideSate = false)
            : base(rainFX, overrideSate) {}
    } 
}