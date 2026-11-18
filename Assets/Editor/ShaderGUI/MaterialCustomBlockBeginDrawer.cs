using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;
using System.Collections.Generic;

namespace CustomShaderEditor
{
    public class MaterialCustomBlockBeginDrawer : MaterialPropertyDrawer
    {
        private readonly string groupName;
        private readonly List<string> enableKeyword;
        //背景块颜色
        private readonly float labelColor_X;
        private readonly float labelColor_Y;
        private readonly float labelColor_Z;
        private readonly float labelColor_W;
        //标题颜色
        private readonly float textColor_X;
        private readonly float textColor_Y;
        private readonly float textColor_Z;
        private readonly float textColor_W;

        public MaterialCustomBlockBeginDrawer()
        {
            this.groupName = string.Empty;
            
            this.labelColor_X = 1.0f;
            this.labelColor_Y = 1.0f;
            this.labelColor_Z = 1.0f;
            this.labelColor_W = 1.0f;
            
            this.textColor_X = 1.0f;
            this.textColor_Y = 1.0f;
            this.textColor_Z = 1.0f;
            this.textColor_W = 1.0f;
            
            CustomShaderUtils.TrytoGetEnableKeyword(groupName,out this.groupName, ref this.enableKeyword);
        }

        public MaterialCustomBlockBeginDrawer(string groupName)
        {
            this.groupName = groupName;
            
            this.labelColor_X = 1.0f;
            this.labelColor_Y = 1.0f;
            this.labelColor_Z = 1.0f;
            this.labelColor_W = 1.0f;
            
            this.textColor_X = 1.0f;
            this.textColor_Y = 1.0f;
            this.textColor_Z = 1.0f;
            this.textColor_W = 1.0f;
            
            CustomShaderUtils.TrytoGetEnableKeyword(groupName,out this.groupName, ref this.enableKeyword);
        }
        
        public MaterialCustomBlockBeginDrawer(float labelColor_X, float labelColor_Y, float labelColor_Z, float labelColor_W)
        {
            this.groupName = string.Empty;
            
            this.labelColor_X = labelColor_X;
            this.labelColor_Y = labelColor_Y;
            this.labelColor_Z = labelColor_Z;
            this.labelColor_W = labelColor_W;
            
            this.textColor_X = 1.0f;
            this.textColor_Y = 1.0f;
            this.textColor_Z = 1.0f;
            this.textColor_W = 1.0f;
            
            CustomShaderUtils.TrytoGetEnableKeyword(groupName,out this.groupName, ref this.enableKeyword);
        }
        
        public MaterialCustomBlockBeginDrawer(float labelColor_X, float labelColor_Y, float labelColor_Z, float labelColor_W, float textColor_X, float textColor_Y, float textColor_Z, float textColor_W)
        {
            this.groupName = string.Empty;
            
            this.labelColor_X = labelColor_X;
            this.labelColor_Y = labelColor_Y;
            this.labelColor_Z = labelColor_Z;
            this.labelColor_W = labelColor_W;
            
            this.textColor_X = textColor_X;
            this.textColor_Y = textColor_Y;
            this.textColor_Z = textColor_Z;
            this.textColor_W = textColor_W;
            
            CustomShaderUtils.TrytoGetEnableKeyword(groupName,out this.groupName, ref this.enableKeyword);
        }

        public MaterialCustomBlockBeginDrawer(string groupName, float labelColor_X, float labelColor_Y, float labelColor_Z, float labelColor_W)
        {
            this.groupName = groupName;

            this.labelColor_X = labelColor_X;
            this.labelColor_Y = labelColor_Y;
            this.labelColor_Z = labelColor_Z;
            this.labelColor_W = labelColor_W;
            
            this.textColor_X = 1.0f;
            this.textColor_Y = 1.0f;
            this.textColor_Z = 1.0f;
            this.textColor_W = 1.0f;
            
            CustomShaderUtils.TrytoGetEnableKeyword(groupName,out this.groupName, ref this.enableKeyword);
        }
        
        public MaterialCustomBlockBeginDrawer(string groupName, float labelColor_X, float labelColor_Y, float labelColor_Z, float labelColor_W, float textColor_X, float textColor_Y, float textColor_Z, float textColor_W)
        {
            this.groupName = groupName;

            this.labelColor_X = labelColor_X;
            this.labelColor_Y = labelColor_Y;
            this.labelColor_Z = labelColor_Z;
            this.labelColor_W = labelColor_W;
            
            this.textColor_X = textColor_X;
            this.textColor_Y = textColor_Y;
            this.textColor_Z = textColor_Z;
            this.textColor_W = textColor_W;
            
            CustomShaderUtils.TrytoGetEnableKeyword(groupName,out this.groupName, ref this.enableKeyword);
        }

        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
            Material material = editor.target as Material;

            bool show = editor.IsHeaderShowInInspector(groupName);
            show &= editor.IsShowInInspectorByKeyword(enableKeyword);
            
            if (show)
            {
                GUILayout.Space(-14);
                GUI.color = new Color(labelColor_X, labelColor_Y, labelColor_Z, labelColor_W);
                // editor.BeginBlock(label);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                
                GUI.color = new Color(textColor_X, textColor_Y, textColor_Z, textColor_W);
            
                var style = new GUIStyle("ShurikenLabel");
                style.alignment = TextAnchor.MiddleLeft;
                style.fontSize = 11; // 默认 9
                // style.contentOffset = new Vector2(8, -11);
                style.fontStyle = FontStyle.Bold;
                style.overflow = new RectOffset(0, 0, -11, 0);
                style.normal.textColor = new Color(textColor_X, textColor_Y, textColor_Z, textColor_W);
            
                EditorGUILayout.LabelField(label, style);
                
                EditorGUILayout.EndHorizontal();

                    GUI.color = Color.white;
            }
            else
            {
                GUILayout.Space(-this.GetPropertyHeight(prop, label, editor) - 2);
            }
        }
    }
}
