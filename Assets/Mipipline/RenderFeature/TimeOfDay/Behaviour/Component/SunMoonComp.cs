using System;
using System.Collections;
using System.Collections.Generic;
// using SYPackages.GraphicsLoad.Loader;
using UnityEngine;


namespace SYPackages.TimeOfDay
{
    public enum LightType
    {
        Sun,
        Moon
    };
    
    public class SunMoonComp : TodComp
    {
        public LightType lightType;

        private SunMoonCompContrl sunMoonCompContrl;
        
        protected override void AttachToTodCompContrl()
        {
            if (!TimeOfDayManagerSingleton.Instance.TimeOfDayManager) return;
            
            foreach (var component in TimeOfDayManagerSingleton.Instance.TimeOfDayManager.TodComponents)
            {
                if (component.GetType() == typeof(SunMoonCompContrl))
                {
                    sunMoonCompContrl = component as SunMoonCompContrl;
                }
            }
            
            if (!sunMoonCompContrl) return;

            var sunmoon = GetComponent<Transform>();
            
            switch (lightType)
            {
                case LightType.Sun:
                    if (sunMoonCompContrl.sun != sunmoon)
                        sunMoonCompContrl.sun = sunmoon.transform;
                    break;
                case LightType.Moon:
                    if (sunMoonCompContrl.moon != sunmoon)
                        sunMoonCompContrl.moon = sunmoon.transform;
                    break;
            }
        }
    }
}

