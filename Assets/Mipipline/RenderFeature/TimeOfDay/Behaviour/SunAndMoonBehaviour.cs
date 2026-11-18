using System.Collections;
using System.Collections.Generic;
// using SYPackages.GraphicsLoad.Loader;
using UnityEngine;


namespace SYPackages.TimeOfDay
{
    public enum LightType1
    {
        Sun,
        Moon
    };
    
    [RequireComponent(typeof(Light))]
    public class SunAndMoonBehaviour : MonoBehaviour
    {
        public LightType lightType;

        void Awake()
        {
            AttachToTOD();
        }

        private void AttachToTOD()
        {
            var todManager = GameObject.Find("TimeOfDayRoot");
            switch (lightType)
            {
                case LightType.Sun:
                    var sun = GetComponent<Light>();
                    // todManager.GetComponent<TimeOfDayManager>().Sun = sun;
                    // todManager.GetComponent<TimeOfDayManager>().sunIntensity = sun.intensity;
                    // todManager.GetComponent<TimeOfDayManager>().sunShadowIntensity = sun.shadowStrength;
                    break;
                case LightType.Moon:
                    var moon =GetComponent<Light>();
                    // todManager.GetComponent<TimeOfDayManager>().Moon = moon;
                    // todManager.GetComponent<TimeOfDayManager>().moonIntensity = moon.intensity;
                    // todManager.GetComponent<TimeOfDayManager>().moonShadowIntensity = moon.shadowStrength;
                    break;
            }
        } 
        
    }
}

