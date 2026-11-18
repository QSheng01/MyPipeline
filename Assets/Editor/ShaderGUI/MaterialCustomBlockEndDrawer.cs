using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;
using System.Collections.Generic;

namespace CustomShaderEditor
{
    public class MaterialCustomBlockEndDrawer : MaterialPropertyDrawer
    {
        private readonly string groupName;
        private readonly List<string> enableKeyword;

        public MaterialCustomBlockEndDrawer() : this(string.Empty, string.Empty) { }
        
        public MaterialCustomBlockEndDrawer(string customHeader) : this(customHeader, string.Empty) { }
        
        private MaterialCustomBlockEndDrawer(string groupName, string keyword)
        {
            CustomShaderUtils.TrytoGetEnableKeyword(groupName, out this.groupName, ref this.enableKeyword);
        }

        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
            Material material = editor.target as Material;

            bool show = editor.IsHeaderShowInInspector(groupName);
            show &= editor.IsShowInInspectorByKeyword(enableKeyword);
            
            if (show)
            {
                // editor.EndBlock();
                GUILayout.Space(-10);
                EditorGUILayout.EndVertical();
            }
            else
            {
                GUILayout.Space(-this.GetPropertyHeight(prop, label, editor) - 2);
            }
        }
    }
}
