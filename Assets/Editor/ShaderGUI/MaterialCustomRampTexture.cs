using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;
using System.Collections.Generic;

namespace CustomShaderEditor
{
    public class MaterialCustomRampTextureDrawer : MaterialPropertyDrawer
    {
        public static readonly GUIContent bumpScaleNotSupported =
            new GUIContent("创建/修改渐变纹理图");

        public static readonly GUIContent EditTex = new GUIContent("Edit",
            "创建/修改渐变纹理图.");

        private readonly string groupName;
        private readonly List<string> enableKeyword;
        private readonly string _keyword;

        public MaterialCustomRampTextureDrawer() : this(string.Empty, string.Empty)
        {
        }

        public MaterialCustomRampTextureDrawer(string customHeader) : this(customHeader, string.Empty)
        {
        }

        private MaterialCustomRampTextureDrawer(string groupName, string keyword)
        {
            CustomShaderUtils.TrytoGetEnableKeyword(groupName, out this.groupName, ref this.enableKeyword);
            this._keyword = keyword;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
            Material material = editor.target as Material;

            bool show = editor.IsHeaderShowInInspector(groupName);
            show &= editor.IsShowInInspectorByKeyword(enableKeyword);
            EditorGUI.BeginChangeCheck();

            if (show)
            {
                editor.TextureProperty(position, prop, label, true);
                //prop.textureValue = editor.TextureProperty(position, prop, label, true);
                GUILayout.Space(52);
                if (editor.HelpBoxWithButton(bumpScaleNotSupported, EditTex))
                {
                    DoEditRampTex(material, prop);
                }
            }
            else
            {
                GUILayout.Space(-this.GetPropertyHeight(prop, label, editor) - 2);
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (!string.IsNullOrEmpty(_keyword))
                {
                    bool state = prop.textureValue != null;
                    material.SetKeyword(_keyword, state);
                }
            }
        }

        private void DoEditRampTex(Material material, MaterialProperty prop)
        {
            var texture = material.GetTexture(prop.name);
            var editorWindow = RampTextureEditorWindow.GetWindow<RampTextureEditorWindow>();
            editorWindow.SetOriginData(material,prop.name, texture as Texture2D);
        }

        public override void Apply(MaterialProperty prop)
        {
            base.Apply(prop);
            foreach (var target in prop.targets)
            {
                var m = target as Material;
                m.SetKeyword(_keyword /*"_NORMALMAP"*/, prop.textureValue != null); // 创建时设置一次 Keyword
            }
        }
    }
}