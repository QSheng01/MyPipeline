using System;
using System.Collections;
using System.Collections.Generic;
// using SYPackages.GraphicsLoad.Loader;
using UnityEngine;
using UnityEngine.Serialization;


namespace SYPackages.TimeOfDay
{
    public class LightsComp : TodComp
    {
        public LightType lightType;
        
        private LightsCompContrl lightsCompContrl;
        
        protected override void AttachToTodCompContrl()
        {
            
            foreach (var component in TimeOfDayManagerSingleton.Instance.TimeOfDayManager.TodComponents)
            {
                if (component.GetType() == typeof(LightsCompContrl))
                {
                    lightsCompContrl = component as LightsCompContrl;
                }
            }
            
            if (!lightsCompContrl) return;
            
            switch (lightType)
            {
                case LightType.Sun:
                    lightsCompContrl.DayLightsParent = this.gameObject;
                    break;
                case LightType.Moon:
                    lightsCompContrl.NightLightsParent = this.gameObject;
                    break;
            }
        } 
    }
}

