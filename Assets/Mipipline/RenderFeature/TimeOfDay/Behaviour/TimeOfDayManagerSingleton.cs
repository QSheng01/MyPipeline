using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;



namespace SYPackages.TimeOfDay
{
    [ExecuteInEditMode]
    public class TimeOfDayManagerSingleton : MonoBehaviour
    {
        private static readonly Lazy<TimeOfDayManagerSingleton> SInstance =
            new Lazy<TimeOfDayManagerSingleton>(() => new TimeOfDayManagerSingleton());

        public static TimeOfDayManagerSingleton Instance => SInstance.Value;
        
        public TimeOfDayManager TimeOfDayManager;

        public void Register(TimeOfDayManager timeOfDayManager)
        {
            TimeOfDayManager = timeOfDayManager;
        }
        
        public void UnRegister(TimeOfDayManager timeOfDayManager)
        {
            if (TimeOfDayManager == timeOfDayManager)
            {
                timeOfDayManager = null;
            }
        }
        
    }
}