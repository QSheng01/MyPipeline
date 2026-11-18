using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using BaseShaderGUI = UnityEditor.BaseShaderGUI;

namespace CustomShaderEditor
{
    public class CustomLitShaderGUI : ShaderGUI
    {
        #region Variables

        protected MaterialEditor materialEditor { get; set; }

        protected MaterialProperty surfaceTypeProp { get; set; }

        protected MaterialProperty blendModeProp { get; set; }

        protected MaterialProperty cullingProp { get; set; }

        protected MaterialProperty alphaClipProp { get; set; }

        protected MaterialProperty alphaCutoffProp { get; set; }

        protected MaterialProperty receiveShadowsProp { get; set; }

        protected MaterialProperty queueOffsetProp { get; set; }
        

        #endregion

        public enum SurfaceType
        {
            Opaque,
            Transparent
        }

        public enum BlendMode
        {
            Alpha, // Old school alpha-blending mode, fresnel does not affect amount of transparency
            Premultiply, // Physically plausible transparency mode, implemented as alpha pre-multiply
            Additive,
            Multiply
        }

        public enum RenderFace
        {
            Front = 2,
            Back = 1,
            Both = 0
        }

        protected class Styles
        {
            // Catergories
            public static readonly GUIContent SurfaceOptions =
                new GUIContent("渲染设置", "控制物件的渲染状态.");

            public static readonly GUIContent SurfaceInputs = new GUIContent("Surface Inputs",
                "These settings describe the look and feel of the surface itself.");

            public static readonly GUIContent AdvancedLabel = new GUIContent("Advanced",
                "These settings affect behind-the-scenes rendering and underlying calculations.");

            public static readonly GUIContent surfaceType = new GUIContent("Surface Type",
                "Select a surface type for your texture. Choose between Opaque or Transparent.");

            public static readonly GUIContent blendingMode = new GUIContent("Blending Mode",
                "Controls how the color of the Transparent surface blends with the Material color in the background.");

            public static readonly GUIContent cullingText = new GUIContent("Render Face",
                "Specifies which faces to cull from your geometry. Front culls front faces. Back culls backfaces. None means that both sides are rendered.");

            public static readonly GUIContent alphaClipText = new GUIContent("Alpha Clipping",
                "Makes your Material act like a Cutout shader. Use this to create a transparent effect with hard edges between opaque and transparent areas.");

            public static readonly GUIContent alphaClipThresholdText = new GUIContent("Threshold",
                "Sets where the Alpha Clipping starts. The higher the value is, the brighter the  effect is when clipping starts.");

            public static readonly GUIContent receiveShadowText = new GUIContent("Receive Shadows",
                "When enabled, other GameObjects can cast shadows onto this GameObject.");

            public static readonly GUIContent baseMap = new GUIContent("Base Map",
                "Specifies the base Material and/or Color of the surface. If you’ve selected Transparent or Alpha Clipping under Surface Options, your Material uses the Texture’s alpha channel or color.");

            public static readonly GUIContent emissionMap = new GUIContent("Emission Map",
                "Sets a Texture map to use for emission. You can also select a color with the color picker. Colors are multiplied over the Texture.");

            public static readonly GUIContent normalMapText =
                new GUIContent("Normal Map", "Assigns a tangent-space normal map.");

            public static readonly GUIContent bumpScaleNotSupported =
                new GUIContent("Bump scale is not supported on mobile platforms");

            public static readonly GUIContent fixNormalNow = new GUIContent("Fix now",
                "Converts the assigned texture to be a normal map format.");

            public static readonly GUIContent queueSlider = new GUIContent("Priority",
                "Determines the chronological rendering order for a Material. High values are rendered first.");
        }

        public bool m_FirstTimeApply = true;

        // Header foldout states
        private string m_HeaderStateKey = null;
        private const string k_KeyPrefix = "UniversalRP:CustomMaterial:UI_State:";
        // SavedBool m_SurfaceOptionsFoldout;


        private const int queueOffsetRange = 50;

        public void FindProperties(MaterialProperty[] properties)
        {
            surfaceTypeProp = FindProperty("_Surface", properties);
            blendModeProp = FindProperty("_Blend", properties);
            cullingProp = FindProperty("_Cull", properties);
            alphaClipProp = FindProperty("_AlphaClip", properties);
            alphaCutoffProp = FindProperty("_Cutoff", properties);
            receiveShadowsProp = FindProperty("_ReceiveShadows", properties, false);
            queueOffsetProp = FindProperty("_QueueOffset", properties, false);
        }


        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            if (materialEditor == null)
                throw new ArgumentNullException("materialEditorIn");
            FindProperties(
                properties); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly

            this.materialEditor = materialEditor;
            Material material = materialEditor.target as Material;

            materialEditor.SetDefaultGUIWidths();
            if (m_FirstTimeApply)
            {
                OnOpenGUI(material, materialEditor);
                m_FirstTimeApply = false;
            }

            for (var i = 0; i < properties.Length; i++)
            {
                if ((properties[i].flags & (MaterialProperty.PropFlags.HideInInspector |
                                            MaterialProperty.PropFlags.PerRendererData)) != 0)
                    continue;
                float h = materialEditor.GetPropertyHeight(properties[i], properties[i].displayName);
                Rect r = EditorGUILayout.GetControlRect(true, h, EditorStyles.layerMaskField);
                materialEditor.ShaderProperty(r, properties[i], properties[i].displayName);
            }

            DrawSurfaceOptions(material);
        }

        public virtual void OnOpenGUI(Material material, MaterialEditor materialEditor)
        {
            // Foldout states
            m_HeaderStateKey = k_KeyPrefix + material.shader.name; // Create key string for editor prefs
            // m_SurfaceOptionsFoldout = new SavedBool($"{m_HeaderStateKey}.SurfaceOptionsFoldout", true);

            foreach (var obj in materialEditor.targets)
                MaterialChanged((Material)obj);
        }

        public void DrawSurfaceOptions(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");
            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;
            // Detect any changes to the material

            EditorGUI.BeginChangeCheck();
            {
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                // m_SurfaceOptionsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_SurfaceOptionsFoldout.value, Styles.SurfaceOptions);
                // if (m_SurfaceOptionsFoldout.value)
                // {
                //     DrawCustomLitSurfaceOptions(material);
                //     EditorGUILayout.Space();
                //     DrawAdvancedOptions(material);
                // }

            }
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in blendModeProp.targets)
                    MaterialChanged((Material)obj);
            }
        }
        public void DoPopup(GUIContent label, MaterialProperty property, string[] options)
        {
            if (property == null)
                return;

            EditorGUI.showMixedValue = property.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            int value = EditorGUILayout.Popup(label, (int)property.floatValue, options);
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo(label.text);
                property.floatValue = value;
            }
            EditorGUI.showMixedValue = false;
        }
        private void DrawCustomLitSurfaceOptions(Material material)
        {
            DoPopup(Styles.surfaceType, surfaceTypeProp, Enum.GetNames(typeof(SurfaceType)));
            if ((SurfaceType)material.GetFloat("_Surface") == SurfaceType.Transparent)
                DoPopup(Styles.blendingMode, blendModeProp, Enum.GetNames(typeof(BlendMode)));

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = cullingProp.hasMixedValue;
            var culling = (RenderFace)cullingProp.floatValue;
            culling = (RenderFace)EditorGUILayout.EnumPopup(Styles.cullingText, culling);
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo(Styles.cullingText.text);
                cullingProp.floatValue = (float)culling;
                material.doubleSidedGI = (RenderFace)cullingProp.floatValue != RenderFace.Front;
            }

            EditorGUI.showMixedValue = false;

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = alphaClipProp.hasMixedValue;
            var alphaClipEnabled = EditorGUILayout.Toggle(Styles.alphaClipText, alphaClipProp.floatValue == 1);
            if (EditorGUI.EndChangeCheck())
                alphaClipProp.floatValue = alphaClipEnabled ? 1 : 0;
            EditorGUI.showMixedValue = false;

            if (alphaClipProp.floatValue == 1)
                materialEditor.ShaderProperty(alphaCutoffProp, Styles.alphaClipThresholdText, 1);

            if (receiveShadowsProp != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = receiveShadowsProp.hasMixedValue;
                var receiveShadows =
                    EditorGUILayout.Toggle(Styles.receiveShadowText, receiveShadowsProp.floatValue == 1.0f);
                if (EditorGUI.EndChangeCheck())
                    receiveShadowsProp.floatValue = receiveShadows ? 1.0f : 0.0f;
                EditorGUI.showMixedValue = false;
            }
        }

        public virtual void DrawAdvancedOptions(Material material)
        {
            materialEditor.EnableInstancingField();
            DrawQueueOffsetField();
        }
        protected void DrawQueueOffsetField()
        {
            if (queueOffsetProp != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = queueOffsetProp.hasMixedValue;
                var queue = EditorGUILayout.IntSlider(Styles.queueSlider, (int)queueOffsetProp.floatValue, -queueOffsetRange, queueOffsetRange);
                if (EditorGUI.EndChangeCheck())
                    queueOffsetProp.floatValue = queue;
                EditorGUI.showMixedValue = false;
            }
        }
        public void MaterialChanged(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");
            SetMaterialKeywords(material);
        }

        //创建 material 
        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            if (material == null)
                throw new ArgumentNullException("material");
            base.AssignNewShaderToMaterial(material, oldShader, newShader);
            MaterialChanged(material);
        }

        public static void SetMaterialKeywords(Material material)
        {
            // Clear all keywords for fresh start
            material.shaderKeywords = null;
            // Setup blending - consistent across all Universal RP shaders
            //BaseShaderGUI.SetupMaterialBlendMode(material);
            BaseShaderGUI.SetMaterialKeywords(material);
            // Receive Shadows
            if (material.HasProperty("_ReceiveShadows"))
                CoreUtils.SetKeyword(material, "_RECEIVE_SHADOWS_OFF", material.GetFloat("_ReceiveShadows") == 0.0f);

            if (material.HasProperty("_Emission"))
            {
                CoreUtils.SetKeyword(material, "_EMISSION", material.GetFloat("_Emission") >= 0.5f);

                var oldFlags = material.globalIlluminationFlags;
                var newFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                if (newFlags != oldFlags)
                    material.globalIlluminationFlags = newFlags;
            }

            if (material.HasProperty("_TerrainBlend"))
                CoreUtils.SetKeyword(material, "_TERRAINBLEND", material.GetFloat("_TerrainBlend") >= 0.0f);

            bool normalMap = false;
            if (material.HasProperty("_UseNormalMap"))
                normalMap = material.GetFloat("_UseNormalMap") >= 0.5;
            if (normalMap)
            {
                material.EnableKeyword("_NORMALMAP");
            }
            else
            {
                material.DisableKeyword("_NORMALMAP");
            }

        }
        }

    
}
