using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace CustomShaderEditor
{
    /// <summary>
    /// 折叠Group Header
    /// </summary>
    public class MaterialCustomGroupDrawer : MaterialPropertyDrawer
    {
        bool lastShow = false;

        public string groupname;
        
        //背景块颜色
        private readonly float labelColor_X;
        private readonly float labelColor_Y;
        private readonly float labelColor_Z;
        private readonly float labelColor_W;
        // //标题颜色
        // private readonly float textColor_X;
        // private readonly float textColor_Y;
        // private readonly float textColor_Z;
        // private readonly float textColor_W;

        public MaterialCustomGroupDrawer() : this("") { }

        public MaterialCustomGroupDrawer(string groupname)
        {
            this.groupname = groupname;
            
            this.labelColor_X = 1.0f;
            this.labelColor_Y = 1.0f;
            this.labelColor_Z = 1.0f;
            this.labelColor_W = 1.0f;
            
            // this.textColor_X = 1.0f;
            // this.textColor_Y = 1.0f;
            // this.textColor_Z = 1.0f;
            // this.textColor_W = 1.0f;
        }
        
        public MaterialCustomGroupDrawer(float labelColor_X, float labelColor_Y, float labelColor_Z, float labelColor_W)
        {
            this.groupname = string.Empty;
            
            this.labelColor_X = labelColor_X;
            this.labelColor_Y = labelColor_Y;
            this.labelColor_Z = labelColor_Z;
            this.labelColor_W = labelColor_W;
            
            // this.textColor_X = 1.0f;
            // this.textColor_Y = 1.0f;
            // this.textColor_Z = 1.0f;
            // this.textColor_W = 1.0f;
        }
        
        public MaterialCustomGroupDrawer(string groupname, float labelColor_X, float labelColor_Y, float labelColor_Z, float labelColor_W)
        {
            this.groupname = groupname;
            
            this.labelColor_X = labelColor_X;
            this.labelColor_Y = labelColor_Y;
            this.labelColor_Z = labelColor_Z;
            this.labelColor_W = labelColor_W;
            
            // this.textColor_X = 1.0f;
            // this.textColor_Y = 1.0f;
            // this.textColor_Z = 1.0f;
            // this.textColor_W = 1.0f;
        }
        
        // public MaterialCustomGroupDrawer(string groupname, float labelColor_X, float labelColor_Y, float labelColor_Z, float labelColor_W, float textColor_X, float textColor_Y, float textColor_Z, float textColor_W)
        // {
        //     this.groupname = groupname;
        //     
        //     this.labelColor_X = labelColor_X;
        //     this.labelColor_Y = labelColor_Y;
        //     this.labelColor_Z = labelColor_Z;
        //     this.labelColor_W = labelColor_W;
        //     
        //     this.textColor_X = textColor_X;
        //     this.textColor_Y = textColor_Y;
        //     this.textColor_Z = textColor_Z;
        //     this.textColor_W = textColor_W;
        // }
        
        
        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            Material material = editor.target as Material;
            GUILayout.Space(-12);
            
            GUI.color = new Color(labelColor_X, labelColor_Y, labelColor_Z, labelColor_W);

            if (groupname == "Feature")
                //GUI.color = Color.green;
                GUI.color = new Color(0.28f, 0.43f, 0.44f,1f).gamma;
            if(groupname == "TopFeature")
                //GUI.color = Color.cyan;
                GUI.color = new Color(0.38f, 0.73f, 0.54f,1f).gamma;
            lastShow = CustomShaderGUIUtils.Foldout(lastShow, label.text);
            prop.floatValue = lastShow ? 1 : 0;
            
            GUI.color = Color.white;
            
                        
        }
    }
}
