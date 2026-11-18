using System;

namespace UnityEngine.Rendering.Environment
{
    [Serializable, VolumeComponentMenu("Environment/LightingSettingsData")]
        public class LightingSettingsData : VolumeComponent
        {
            // 彩色阴影过渡有关
            public ClampedFloatParameter shadowColorLerp = new ClampedFloatParameter(0.15f, 0f,1.0f);
            public ClampedFloatParameter shadowColorLerp2 = new ClampedFloatParameter(0.6f, 0f,1.0f);
            public ColorParameter shadowEdgeColor = new ColorParameter(Color.red, false, false, true);


            // 实现接口
            public bool IsActive() => active;


        public override void Override(VolumeComponent state, float interpFactor)
        {
            base.Override(state, interpFactor);
            LightingSettingsData data = state as LightingSettingsData;

            if (!data || !data.IsActive() || !RenderSettings.sun)
                return;


            // 彩色阴影有关
            if (data.shadowColorLerp.overrideState)
            {
                data.shadowColorLerp.overrideState = shadowColorLerp.overrideState;
                Shader.SetGlobalFloat("lerpstep",data.shadowColorLerp.value);
            }

            if (data.shadowColorLerp2.overrideState)
            {
                data.shadowColorLerp.overrideState = shadowColorLerp.overrideState;
                Shader.SetGlobalFloat("lerpstep2",data.shadowColorLerp2.value);
            }

            if (data.shadowEdgeColor.overrideState)
            {
                data.shadowEdgeColor.overrideState = shadowEdgeColor.overrideState;
                Shader.SetGlobalColor("_ShadowEdgeColor",data.shadowEdgeColor.value);
            }
        }
    }
}
