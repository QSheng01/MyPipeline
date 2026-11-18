using System;
using System.Diagnostics;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.Environment
{
    [Serializable, VolumeComponentMenu("Environment/RenderSettingsData")]
    public class RenderSettingsData: VolumeComponent
    {
        public SkyboxParameter skybox= new SkyboxParameter(null);
        public BoolParameter skyDayOrNight = new BoolParameter(true);
        public ColorParameter skyboxTopColor = new ColorParameter(Color.white, true, false, true);
        public ColorParameter skyboxBottomColor = new ColorParameter(Color.white, true, false, true);
        public ColorParameter skyboxGroundColor = new ColorParameter(Color.black, false, false, true);
        public ColorParameter skyEdgeTopColor = new ColorParameter(Color.white, true, false, true);
        public ColorParameter skyEdgeBottomColor = new ColorParameter(Color.white, true, false, true);

        public BoolParameter useAtmoScattering = new BoolParameter(true);
        [Range(0,10)]public ClampedFloatParameter kr = new ClampedFloatParameter(8.4f, 0f, 20f);
        [Range(0,10)]public ClampedFloatParameter km = new ClampedFloatParameter(1.2f, 0f, 10f);
        [Range(0,5)]public ClampedFloatParameter rayleigh =  new ClampedFloatParameter(1.5f, 0f, 5f);
        [Range(0,5)]public ClampedFloatParameter mie = new ClampedFloatParameter(1.0f, 0f, 5f);
        [Range(0,5)]public ClampedFloatParameter mieG = new ClampedFloatParameter(0.76f, 0f, 1f);
        [Range(0,1)] public FloatParameter scattering = new FloatParameter(0.25f);

        public FloatParameter skyGradientExponent = new FloatParameter(2.5f);

        public ColorParameter cloudLightColor = new ColorParameter(Color.white, true, false, true);
        public ColorParameter cloudDarkColor = new ColorParameter(Color.white, true, false, true);
        public ColorParameter cloudSpecColor = new ColorParameter(Color.white, true, false, true);
        public ColorParameter cloudScatterColor = new ColorParameter(Color.white, true, false, true);
        public ClampedFloatParameter cloudFog = new ClampedFloatParameter(1, 0, 1);


        public ColorParameter realtimeShadowColor= new ColorParameter(new Color(0.42f, 0.48f, 0.628f), false, true,true);
        public AmbientModeParameter ambientMode = new AmbientModeParameter(AmbientMode.Skybox);
        public ColorParameter ambientLight = new ColorParameter(new Color(0.212f,0.227f,0.259f), true, false,true);
        public ColorParameter ambientSkyColor= new ColorParameter(new Color(0.212f,0.227f,0.259f), true, false,true);
        public ColorParameter ambientEquatorColor= new ColorParameter(new Color(0.114f,0.125f,0.133f), true, false,true);
        public ColorParameter ambientGroundColor= new ColorParameter(new Color(0.047f,0.043f,0.035f), true, false,true);
        public ClampedFloatParameter ambientIntensity = new ClampedFloatParameter(1, 0, 8);


        public DefaultReflectionModeParameter defaultReflectionMode = new DefaultReflectionModeParameter(DefaultReflectionMode.Skybox);
        [HideInInspector]
        public IntParameter defaultReflectionResolution = new IntParameter(128);
        public ClampedFloatParameter reflectionIntensity =  new ClampedFloatParameter(1, 0, 1);
        [HideInInspector]
        public ClampedIntParameter reflectionBounces = new ClampedIntParameter(1,1,5);

        public EnvRefCubeParameter environmentReflectCube = new EnvRefCubeParameter(null);

        public BoolParameter isFog=new BoolParameter(false);
        public ColorParameter fogColor =  new ColorParameter(Color.gray,false, true,true);
        public FogModeParameter fogMode = new FogModeParameter(FogMode.Linear);
        public FloatParameter fogStartDistance = new FloatParameter(0);
        public FloatParameter fogEndDistance = new FloatParameter(300);
        public ClampedFloatParameter fogDensity = new ClampedFloatParameter(0.01f,0,1);

        // 实现接口
        public bool IsActive()=>active;

        private Material cacheMat;
        public override void Override(VolumeComponent state, float interpFactor)
        {
            if (state == null) return;
            base.Override(state, interpFactor);
            RenderSettingsData data = state as RenderSettingsData;

            if (!data || !data.IsActive())
                return;

            // if( data.ambientMode.overrideState)
            RenderSettings.ambientMode = data.ambientMode.value;
            if (RenderSettings.ambientMode == AmbientMode.Custom)
            {
                RenderSettings.ambientMode = AmbientMode.Flat;
            }
            if (RenderSettings.ambientMode == AmbientMode.Flat)
            {
                RenderSettings.ambientLight = data.ambientEquatorColor.value;
            }
            else if (RenderSettings.ambientMode == AmbientMode.Skybox)
            {
                RenderSettings.ambientSkyColor = data.ambientSkyColor.value;
            }
            else if (RenderSettings.ambientMode == AmbientMode.Trilight)
            {
                RenderSettings.ambientSkyColor = data.ambientSkyColor.value;
                RenderSettings.ambientGroundColor = data.ambientGroundColor.value;
                RenderSettings.ambientEquatorColor = data.ambientEquatorColor.value;
            }

            RenderSettings.ambientIntensity = data.ambientIntensity.value;

            if (RenderSettings.skybox != data.skybox.value)
            {
                RenderSettings.skybox = data.skybox.value;
            }

            // 不在这里控制反射球了
            // RenderSettings.defaultReflectionMode = data.defaultReflectionMode.value;
            // RenderSettings.defaultReflectionResolution = data.defaultReflectionResolution.value;
            // RenderSettings.reflectionIntensity = data.reflectionIntensity.value;
            // RenderSettings.reflectionBounces = data.reflectionBounces.value;
            // RenderSettings.customReflectionTexture = data.environmentReflectCube.value;

            // RenderSettings.fog = data.isFog.value;
            // RenderSettings.fogMode = data.fogMode.value;
            // RenderSettings.fogColor = data.fogColor.value;
            // RenderSettings.fogStartDistance = data.fogStartDistance.value;
            // RenderSettings.fogEndDistance = data.fogEndDistance.value;
            // RenderSettings.fogDensity = data.fogDensity.value;

            // LightmapSettings.lightmaps = null;
            // LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional;

            if (cloudDarkColor.overrideState)
            {
                data.cloudDarkColor.value = Color.Lerp(data.cloudDarkColor.value, cloudDarkColor.value, interpFactor);
            }
            if (cloudLightColor.overrideState)
            {
                data.cloudLightColor.value = Color.Lerp(data.cloudLightColor.value, cloudLightColor.value, interpFactor);
            }
            if (cloudSpecColor.overrideState)
            {
                data.cloudSpecColor.value = Color.Lerp(data.cloudSpecColor.value, cloudSpecColor.value, interpFactor);
            }
            if (cloudScatterColor.overrideState)
            {
                data.cloudScatterColor.value = Color.Lerp(data.cloudScatterColor.value, cloudScatterColor.value, interpFactor);
            }
            if (cloudFog.overrideState)
            {
                data.cloudFog.value = Mathf.Lerp(data.cloudFog.value, cloudFog.value, interpFactor);
            }

            //lerp skybox
            Material skyMat = data.skybox.value;
            if (skybox == null)
            {
                return;
            }
            if (skyMat == null)
            {
                skyMat = skybox.value;
            }

            // if (skyMat != null)
            // {
            //     LocalKeyword atKW = new LocalKeyword(skyMat.shader, "_ATMOSPERICSCATTERING");
            //     skyMat.SetKeyword(atKW, true);
            //     // if (useAtmoScattering.overrideState)
            //     // {
            //     //     skyMat.SetKeyword(atKW, useAtmoScattering.value);
            //     // }
            //
            //     if (skyDayOrNight.value)
            //     {
            //         skyMat.EnableKeyword("_DAY");
            //         skyMat.DisableKeyword("_NIGHT");
            //     }
            //     else
            //     {
            //         skyMat.EnableKeyword("_NIGHT");
            //         skyMat.DisableKeyword("_DAY");
            //     }
            //
            //     if (skyboxTopColor.overrideState)
            //     {
            //         skyMat.SetColor("_SkyTopColor",
            //             Color.Lerp(data.skyboxTopColor.value, skyboxTopColor.value, interpFactor));
            //     }
            //
            //     if (skyboxBottomColor.overrideState)
            //     {
            //         skyMat.SetColor("_SkyBottomColor",
            //             Color.Lerp(data.skyboxBottomColor.value, skyboxBottomColor.value, interpFactor));
            //     }
            //
            //     if (skyboxGroundColor.overrideState)
            //     {
            //         skyMat.SetColor("_GroundColor",
            //             Color.Lerp(data.skyboxGroundColor.value, skyboxGroundColor.value, interpFactor));
            //     }
            //
            //     if (skyGradientExponent.overrideState)
            //     {
            //         skyMat.SetFloat("m_skyGradientExponent",
            //             Interp(data.skyGradientExponent.value, skyGradientExponent.value, interpFactor));
            //     }
            //
            //     if (skyEdgeTopColor.overrideState)
            //     {
            //         skyMat.SetColor("_RayleighScatteringColor",
            //             Color.Lerp(data.skyEdgeTopColor.value, skyEdgeTopColor.value, interpFactor));
            //     }
            //
            //     if (kr.overrideState)
            //     {
            //         skyMat.SetFloat("_Azure_Kr",
            //             Interp(data.kr.value * 1000, kr.value * 1000, interpFactor));
            //     }
            //
            //     if (km.overrideState)
            //     {
            //         float value = Interp(data.km.value * 1000, km.value * 1000, interpFactor);
            //         skyMat.SetFloat("_Azure_Km",value);
            //     }
            //
            //     if (rayleigh.overrideState)
            //     {
            //         Vector3 value = Vector3.Lerp(ComputeRayleigh() * data.rayleigh.value, ComputeRayleigh() * rayleigh.value, interpFactor);
            //         skyMat.SetVector("_Azure_Rayleigh",value);
            //     }
            //
            //     if (mie.overrideState)
            //     {
            //         Vector3 value = Vector3.Lerp(ComputeMie() * data.mie.value , ComputeMie() * mie.value, interpFactor);
            //         skyMat.SetVector("_Azure_Mie",value);
            //     }
            //
            //     if (mieG.overrideState)
            //     {
            //         float value = Interp(data.mieG.value, mieG.value, interpFactor);
            //         skyMat.SetFloat("_MieG",value);
            //     }
            //
            //     if (skyEdgeBottomColor.overrideState)
            //     {
            //         skyMat.SetColor("_MieScatteringColor",
            //             Color.Lerp(data.skyEdgeBottomColor.value, skyEdgeBottomColor.value, interpFactor));
            //     }
            //
            //     // if (skySunAndMoonColor.overrideState)
            //     // {
            //     //     if (skyDayOrNight.value)
            //     //     {
            //     //         skyMat.SetColor("_SunColor",
            //     //             Color.Lerp(data.skySunAndMoonColor.value, skySunAndMoonColor.value, interpFactor));
            //     //     }
            //     //     else
            //     //     {
            //     //         skyMat.SetColor("_MoonColor",
            //     //             Color.Lerp(data.skySunAndMoonColor.value, skySunAndMoonColor.value, interpFactor));
            //     //     }
            //     // }
            //     //
            //     // if (skySunAndMoonHaloColor.overrideState)
            //     // {
            //     //     if (skyDayOrNight.value)
            //     //     {
            //     //         skyMat.SetColor("_SunHalo",
            //     //             Color.Lerp(data.skySunAndMoonHaloColor.value, skySunAndMoonHaloColor.value, interpFactor));
            //     //     }
            //     //     else
            //     //     {
            //     //         skyMat.SetColor("_MoonHaloColor",
            //     //             Color.Lerp(data.skySunAndMoonHaloColor.value, skySunAndMoonHaloColor.value, interpFactor));
            //     //     }
            //     // }
            // }
        }

        private Vector3 ComputeRayleigh()
        {
            Vector3 rayleigh = Vector3.one;
            rayleigh = new Vector3(5.8f, 13.5f, 33.1f) * 0.000001f;
            return rayleigh;
        }

        private Vector3 ComputeMie()
        {
            Vector3 mie;
            mie = new Vector3(2.0f, 2.0f, 2.0f) * 0.00001f;

            return mie;
        }
        float Interp(float from, float to, float t)
        {
            return from + (to - from) * t;
        }
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class SkyboxParameter : VolumeParameter<Material>
    {

        /// <param name="overrideState">Initial override state.</param>
        public SkyboxParameter(Material value, bool overrideState = false)
            : base(value, overrideState) { }

        public sealed override void Interp(Material from, Material to, float t)
        {
            if (t > 0.5f)
            {
                m_Value = to;
            }

        }

        void SetColorValue(string name,Material from,Material to, float lerp)
        {
            if (from.HasProperty(name))
            {
                if (from.GetColor(name) != to.GetColor(name))
                {
                    m_Value.SetColor(name,
                        Color.Lerp(from.GetColor(name), to.GetColor(name), lerp));
                }
            }
        }


        void SetTextureValue(string name, Material from, Material to)
        {
            if (from.HasProperty(name))
            {
                if (to.GetTexture(name) != to.GetTexture(name))
                {
                    m_Value.SetTexture(name, to.GetTexture(name));
                }
            }
        }


        void SetVectorValue(string name, Material from, Material to)
        {
            if (from.HasProperty(name))
            {
                if (from.GetVector(name) != to.GetVector(name))
                {
                    m_Value.SetVector(name, to.GetVector(name));
                }
            }
        }


        void SetFloatValue(string name, Material from, Material to, float lerp)
        {
            if (from.HasProperty(name))
            {
                if (from.GetFloat(name) != to.GetFloat(name))
                {
                    m_Value.SetFloat(name,
                        Interp(from.GetFloat(name), to.GetFloat(name), lerp));
                }
            }
        }

        void EnableKeyWord(string name)
        {
            m_Value.EnableKeyword(name);
        }

        void DisableKeyWord(string name)
        {
            m_Value.DisableKeyword(name);
        }

        float Interp(float from, float to, float t)
        {
            return from + (to - from) * t;
        }
    }


    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class AmbientModeParameter : VolumeParameter<AmbientMode>
    {
        public AmbientModeParameter(AmbientMode value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class DefaultReflectionModeParameter : VolumeParameter<DefaultReflectionMode>
    {
        public DefaultReflectionModeParameter(DefaultReflectionMode value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class FogModeParameter : VolumeParameter<FogMode>
    {
        public FogModeParameter(FogMode value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class EnvRefCubeParameter : VolumeParameter<Cubemap>
    {
        public EnvRefCubeParameter(Cubemap value, bool overrideState = false)
            : base(value, overrideState) { }

        public override void Interp(Cubemap from, Cubemap to, float t)
        {
            m_Value = t > 0.5f ? to : from;
        }
    }
}
