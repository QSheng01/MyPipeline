using System;
using System.Collections.Generic;
using EffectsPostProcess.Scripts.PostProcessRenderers;
using PostProcess.Scripts.ExpandFunctionality;
using UnityEngine;

namespace EffectsPostProcess.Scripts.Controllers
{
    [Serializable]
    public enum EffectsPostProcess
    {
        ColorStyle,
        RGBSplit,
        RadialBlur,
        ColorAdjustment,
        IrisBlur
        //BrokehBlur
    }
    
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public sealed partial class EffectsPostProcessController : MonoBehaviour//MonoSynthesizer
    {
        //sealed：表示该类不能被继承。也就是说，没有其他类可以继承自 `EffectsPostProcessController`
        //partial：表示这个类的定义可以分散在多个文件中。只要所有这些文件都在同一个项目中并且拥有相同的命名空间，最终会被视为一个完整的类。
        public static EffectsPostProcessController Instance
        {
            get => m_Instance;
            set => m_Instance = value;
        }
        private static EffectsPostProcessController m_Instance;
        //单例模式表示这个EffectsPostProcessController场景中只能存在一个
        
// #if UNITY_EDITOR
//         public static EffectsPostProcessController FirstPriority;  
// #endif
//         public static void SetInstance(EffectsPostProcessController controller)
//         {
//             m_Instance = controller;
//         }
        
//         public static void LoadInstance()
//         {
//             if (Application.isPlaying)
//             {
//                 m_Instance = PriorityQueue.GetFirstItem<EffectsPostProcessController>();       
//             }
//             
// #if UNITY_EDITOR
//       
//             else
//             {
//                 if (FirstPriority != null)
//                     m_Instance = FirstPriority;
//                 else
//                 {
//                     m_Instance = PriorityQueue.GetFirstItem<EffectsPostProcessController>();           
//                 }
// 
//             }      
// #endif
//         }

        [Header("Effects渲染器")]
        private static Dictionary<EffectsPostProcess, EffectsPostProcessRenderer> m_AllRenderers = new Dictionary<EffectsPostProcess, EffectsPostProcessRenderer>();
        
        public static Dictionary<EffectsPostProcess, EffectsPostProcessRenderer> AllRenderers => m_AllRenderers;

        [Header("效果队列")]
        public List<EffectsPostProcess> selectedRenderersList = new List<EffectsPostProcess>();
        
        public void OnEnable()
        {
            InitEffectsRenderers();
            Instance = this;
        }

        private void OnDisable()
        {
            if (Instance.Equals(this))
            {
                Instance = null;
            }
        }

        // public void Update()
        // {
        //     LoadInstance();
        // }

        private void InitEffectsRenderers()
        {
            if (m_AllRenderers.Count == 0)
            {
                m_AllRenderers.Clear();
            
                m_AllRenderers.Add( EffectsPostProcess.ColorStyle,       new ColorStyleRenderer()      );
                m_AllRenderers.Add( EffectsPostProcess.RGBSplit,         new RGBSplitGlitchRenderer()  );
                m_AllRenderers.Add( EffectsPostProcess.ColorAdjustment,  new ColorAdjustmentRenderer() );
                m_AllRenderers.Add( EffectsPostProcess.RadialBlur,       new RadialBlurRenderer()      );
                m_AllRenderers.Add( EffectsPostProcess.IrisBlur,         new IrisBlurRenderer()        );
            }
        }
    }
}