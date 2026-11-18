using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;

namespace SYPackages.TimeOfDay
{
    public class VolumeComp : TodComp
    {
        public PlayableAsset todTimeLine;
        
        private VolumeCompContrl volumeCompContrl;
        
        protected override void AttachToTodCompContrl()
        {
            foreach (var component in TimeOfDayManagerSingleton.Instance.TimeOfDayManager.TodComponents)
            {
                if (component.GetType() == typeof(VolumeCompContrl))
                {
                    volumeCompContrl = component as VolumeCompContrl;
                }
            }
            
            if (volumeCompContrl != null)
            {
                volumeCompContrl.todTimeLine = volumeCompContrl.GetDirector().playableAsset = todTimeLine;
            }
            
        }
    }
}