using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SYPackages.TimeOfDay
{
    [ExecuteInEditMode]
    public abstract class TodComp : MonoBehaviour
    {
        protected abstract void AttachToTodCompContrl(); 

#if UNITY_EDITOR
        private void Update()
        {
            AttachToTodCompContrl();
        }
#else   
        private void OnEnable()
        {
            AttachToTodCompContrl();
        }
#endif
        
        
#if UNITY_EDITOR
        [ContextMenu("绑定数据给TOD系统")]
        void SetData()
        {
            AttachToTodCompContrl();
        }
#endif
    }
}