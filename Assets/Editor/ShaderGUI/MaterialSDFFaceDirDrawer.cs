using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;

namespace CustomShaderEditor
{
    public class MaterialSDFFaceDirDrawer : MaterialPropertyDrawer
    {
        private readonly string groupName;
        public readonly string color;
       // private readonly GUIContent[] names;
       private readonly string[] names;
        private readonly float[] values;
        
        private int selIndex;
        private Dictionary<string, Color> _colorDic = new Dictionary<string, Color>();

        private static GUIStyle guistyle = new GUIStyle(EditorStyles.popup);
        
        public enum FaceDirEnum
        {
            X,
            Y,
            Z,
            Inver_X,
            Inver_Y,
            Inver_Z
        }

        public MaterialSDFFaceDirDrawer(string groupName,string color)
        {
            var enumNames = Enum.GetNames(typeof(FaceDirEnum));
            //this.names = new GUIContent[enumNames.Length];
            this.names = new string[enumNames.Length];
            for (int i = 0; i < enumNames.Length; ++i)
                //this.names[i] = new GUIContent(enumNames[i]);
                this.names[i] = enumNames[i];
            var enumVals = Enum.GetValues(typeof(FaceDirEnum));
            values = new float[enumVals.Length];
            for (var i = 0; i < enumVals.Length; ++i)
                values[i] = (int) enumVals.GetValue(i);
            this.groupName = groupName;
            this.color = color;
            _colorDic.Clear();
            _colorDic.Add("red",Color.red);
            _colorDic.Add("black", Color.black);
            _colorDic.Add("blue",Color.blue);
        }



        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            if (prop.type != MaterialProperty.PropType.Float && prop.type != MaterialProperty.PropType.Range)
            {
                return 16 * 2.5f;
            }

            return base.GetPropertyHeight(prop, label, editor);
        }

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            if (prop.type != MaterialProperty.PropType.Vector)
            {
                GUIContent c = CustomShaderGUIUtils.TempContent("SDFFaceDir used on a vector property: " + prop.name,
                    CustomShaderGUIUtils.GetHelpIcon(MessageType.Warning));
                EditorGUI.LabelField(position, c, EditorStyles.helpBox);
                return;
            }

            var color = Color.black;
            _colorDic.TryGetValue(this.color, out color);
            
            bool show = editor.IsHeaderShowInInspector(groupName);
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;
            var value = prop.vectorValue;
            int selectedIndex = -1;
            for (var index = 0; index < values.Length; index++)
            {
                var i = values[index];
                if (Mathf.Approximately(i, value.w))
                {
                    selectedIndex = index;
                    break;
                }
            }

            EditorGUI.showMixedValue = false;
            if (!show)
            {
                GUILayout.Space(-this.GetPropertyHeight(prop, label.text, editor) - 2);
            }
            else
            {
                guistyle.normal.textColor = color;
                selIndex = EditorGUI.Popup(position,label.text,selectedIndex,names,guistyle);
                //selIndex = EditorGUILayout.Popup(label.text, selectedIndex, names, guistyle);
                //GUILayout.Space(-this.GetPropertyHeight(prop, label.text, editor)* 0.5f + 2);
            }

            if (EditorGUI.EndChangeCheck())
            {
                var enums = Enum.GetValues(typeof(FaceDirEnum));
               
                var dir = new Vector3(prop.vectorValue.x, prop.vectorValue.y, prop.vectorValue.z);
                UpdateFaceDir(editor, (FaceDirEnum) enums.GetValue(selIndex), ref dir);
                prop.vectorValue = new Vector4(dir.x, dir.y, dir.z, values[selIndex]);
            }

            if (show)
            {
                if (Selection.activeGameObject != null)
                {
                    var dir = new Vector3(prop.vectorValue.x, prop.vectorValue.y, prop.vectorValue.z);
                    OnDrawGizmos(Selection.activeGameObject,dir);
                }
            }
        }

        private void OnDrawGizmos(GameObject target, Vector3 dir)
        {
            var renderer = target.GetComponent<Renderer>();
            Transform rootBone;
            if (renderer == null) return;
            if (renderer is SkinnedMeshRenderer)
            {
                rootBone = (renderer as SkinnedMeshRenderer).rootBone;
            }
            else if (renderer is MeshRenderer)
            {
                rootBone = (renderer as MeshRenderer).transform;
            }
            else
            {
                rootBone = target.transform;
            }
            var point = rootBone.transform.TransformPoint(Vector3.zero);
            var debug_forward = rootBone.transform.TransformDirection(dir).normalized;
            var color = Color.black;
            _colorDic.TryGetValue(this.color, out color);
            Debug.DrawLine(point, point + 0.5f * debug_forward, color, 0.5f);
        }

        public void UpdateFaceDir(MaterialEditor editor, FaceDirEnum targeEnum, ref Vector3 dir)
        {
            if (Selection.activeGameObject != null)
            {
                SetupLocalDir(Selection.activeGameObject, targeEnum, ref dir);
            }
        }

        /// <summary>
        /// 选择轴向
        /// </summary>
        private void SetupDirEnum(FaceDirEnum targetEnum, ref Vector3 dir)
        {
            dir = targetEnum switch
            {
                FaceDirEnum.X => Vector3.right,
                FaceDirEnum.Y => Vector3.up,
                FaceDirEnum.Z => Vector3.forward,
                FaceDirEnum.Inver_X => -Vector3.right,
                FaceDirEnum.Inver_Y => -Vector3.up,
                FaceDirEnum.Inver_Z => -Vector3.forward,
                _ => dir
            };
        }

        /// <summary>
        /// 计算局部的坐标，只需要计算一次。
        /// Editor 状态下可每帧都调用
        /// </summary>
        private void SetupLocalDir(GameObject target, FaceDirEnum targetEnum, ref Vector3 dir)
        {
            var renderer = target.GetComponent<Renderer>();
            Transform rootBone;
            if (renderer == null) return;
            if (renderer is SkinnedMeshRenderer)
            {
                rootBone = (renderer as SkinnedMeshRenderer).rootBone;
            }
            else if (renderer is MeshRenderer)
            {
                rootBone = (renderer as MeshRenderer).transform;
            }
            else
            {
                rootBone = target.transform;
            }

            SetupDirEnum(targetEnum, ref dir);

            // 先把局部朝向转到世界空间（得到世界空间的朝向）
            dir = target.transform.TransformDirection(dir);
            // 从世界空间中转到骨骼空间
            dir = rootBone.transform.InverseTransformDirection(dir);
        }
    }
}