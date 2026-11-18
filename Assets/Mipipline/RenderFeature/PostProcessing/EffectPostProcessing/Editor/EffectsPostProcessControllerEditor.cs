using System;
using System.Collections.Generic;
using EffectsPostProcess.Scripts.Util;
using EffectsPostProcess.Scripts.Controllers;
using PostProcess.Scripts.ExpandFunctionality;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = System.Object;

namespace EffectsPostProcess.Scripts.ControllerEditors
{
    [CustomEditor(typeof(EffectsPostProcessController))]
    [CanEditMultipleObjects]
    public partial class EffectsPostProcessControllerEditor : UnityEditor.Editor
    {

        private SerializedProperty ID = null;
        
        private Dictionary<Enum, Action> _controllerFuncDict = new Dictionary<Enum, Action>();

        private bool showCurrent;

        private void OnEnable()
        {
            InitID();
            
            InitColorStyleProperties(); 
            InitRGBSplitGlithProperties();
            InitRadialBlurProperties();
            InitIrisBlurProperties();
            InitLightnessProperties();

            InitFuncDict();
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (CheckRenderFeatureAttach())
            {
                ShowCurrentController();
                // IDContext();
                // ShowFirstPriority();
                
                DrawController();
                AddPostProcessButton();   
            }
            else
            {
                ShowWarningContext();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void InitFuncDict()
        {
            this._controllerFuncDict.Add(Controllers.EffectsPostProcess.ColorStyle      ,       ColorStyleComp);
            this._controllerFuncDict.Add(Controllers.EffectsPostProcess.RGBSplit        ,         RGBSplitComp);
            this._controllerFuncDict.Add(Controllers.EffectsPostProcess.RadialBlur      ,       RadialBlurComp);
            this._controllerFuncDict.Add(Controllers.EffectsPostProcess.IrisBlur        ,         IrisBlurComp);
            //this._controllerFuncDict.Add(Controllers.AdamasPostProcess.BrokehBlur      ,       BrokehBlurComp);
            this._controllerFuncDict.Add(Controllers.EffectsPostProcess.ColorAdjustment     , ColorAdjustmentComp);
        }

        private void InitID()
        {
            ID = serializedObject.FindProperty("ID");
        }

        private void ShowCurrentController()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("当前显示节点:");

            if (EffectsPostProcessController.Instance != null)
            {
                EditorGUILayout.ObjectField(EffectsPostProcessController.Instance.gameObject, typeof(GameObject), false);
            }
            EditorGUILayout.EndHorizontal();
        }
        
        // private void ShowFirstPriority()
        // {
        //     EditorGUILayout.BeginHorizontal();
        //     EditorGUILayout.LabelField("Editor优先显示:");   
        //     showCurrent = EditorGUILayout.Toggle(showCurrent);  
        //     EditorGUILayout.EndHorizontal();
        //     
        //     if (showCurrent)
        //     {
        //         var controller = target as EffectsPostProcessController;
        //         EffectsPostProcessController.FirstPriority = controller;
        // 
        //         if (controller is { })
        //         {
        //             EditorUtility.SetDirty(target);
        //         }
        //     }
        //     else
        //     {
        //         EffectsPostProcessController.FirstPriority = null;
        //     }
        //     
        //     EditorGUILayout.Space(10);
        //     
        // }
        
        private bool CheckRenderFeatureAttach()
        {
            var renderer = UniversalRenderPipeline.asset.scriptableRenderer;

            if (renderer.ContainRendererFeature("Effects PostProcess RenderFeature"))
            {
                return true;
            }
            
            return false;
        }
        
        // private void IDContext()
        // {
        //     var nodeName = PriorityQueue.GetFirstItem<MonoSynthesizer>();
        //     EditorGUILayout.PropertyField(ID);
        //     if (nodeName)
        //     {
        //         EditorGUILayout.LabelField("当前第一个节点为：" + nodeName.name);               
        //     }
        //     
        // }
        
        private void ShowWarningContext()
        {
            EditorGUILayout.Space(30);
            EditorGUILayout.LabelField("请添加Effects Render Feature.", EditorStyles.whiteLabel);
        }
        
        private void DrawController()
        {
            var controller = target as EffectsPostProcessController;

            using (null)
            {
                if (controller is { } && controller.selectedRenderersList.Count > 0)
                    for (int i = 0; i < controller.selectedRenderersList.Count; i ++ )
                    {
                        var item = controller.selectedRenderersList[i];

                        var action = this._controllerFuncDict[item];
                        action.Invoke();
                    }
            }
        }

        private void AddPostProcessButton()
        {
            var menus = new GenericMenu();

            foreach (var item in EffectsPostProcessController.AllRenderers)
            {
                var k = item.Key;
                var r = item.Value;

                var content = new GUIContent(r.name);

                if ( ! ( (EffectsPostProcessController)target ).selectedRenderersList.Contains(k) )
                {
                    menus.AddItem(content, false, delegate
                    {
                        ( (EffectsPostProcessController)target ).selectedRenderersList.Add(k);
                        EditorUtility.SetDirty(target);
                    });
                    
                }
            }
            
            if (GUILayout.Button("点击添加后处理效果"))
            {
                menus.ShowAsContext();
            }
        }

        private void OptionButton(Controllers.EffectsPostProcess effectsPostProcess)
        {
            if (GUILayout.Button("上移"))
            {
                var list = ((EffectsPostProcessController) target).selectedRenderersList;

                var index = list.IndexOf(effectsPostProcess);
                if (index != 0)
                {
                    (list[index], list[index - 1]) = (list[index - 1], list[index]);
                    
                    EditorUtility.SetDirty(target);
                }
            }
            
            if (GUILayout.Button("下移"))  
            {
                var list = ((EffectsPostProcessController) target).selectedRenderersList;

                var index = list.IndexOf(effectsPostProcess);
                if (index != list.Count - 1)
                {
                    (list[index], list[index + 1]) = (list[index + 1], list[index]);
                    
                    EditorUtility.SetDirty(target);
                }
            }
            
            if (GUILayout.Button("删除"))
            {
                var list = ((EffectsPostProcessController) target).selectedRenderersList;
                list.Remove(effectsPostProcess);
                
                EditorUtility.SetDirty(target);
            }
        }
    }
}