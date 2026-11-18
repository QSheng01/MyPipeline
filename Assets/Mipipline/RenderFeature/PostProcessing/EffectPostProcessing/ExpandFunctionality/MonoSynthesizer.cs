using System;
using System.Collections.Generic;
using UnityEngine;

namespace PostProcess.Scripts.ExpandFunctionality
{
    public class MonoSynthesizer : MonoBehaviour, ISingleCell
    {
        // [Tooltip("数值越小优先级越高")]
        // [Header("优先级")] [Range(0, 16)]
        // public int ID = 0;

        // public List<IMono> IMonosList = new List<IMono>()
        // {
        //     new PriorityQueue()
        // };

        // public virtual void OnEnable()
        // {
        //     foreach (var iMono in IMonosList)
        //     {
        //         iMono.OnEnable(this);
        //     }
        // }
        // 
        // public virtual void OnDisable()
        // {
        //     foreach (var iMono in IMonosList)
        //     {
        //         iMono.OnDisable(this);
        //     }
        // }
        // 
        // public virtual void Update()
        // {
        //     foreach (var iMono in IMonosList)
        //     {
        //         iMono.OnUpdate(this);
        //     }
        // }
    }   

    // public interface IMono
    // {
    //     public void OnEnable(ISingleCell cell);
    //     public void OnDisable(ISingleCell cell);
    //     public void OnUpdate(ISingleCell cell);
    // }
}