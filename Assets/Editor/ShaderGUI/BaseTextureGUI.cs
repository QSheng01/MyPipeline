
using UnityEditor;
using UnityEngine;

namespace CustomShaderEditor
{
    public class BaseTextureGUI1 : ShaderGUI
    {
        MaterialEditor m_MaterialEditor;
        bool cutOffFlag = false;
        bool gpuInstance = false;

        MaterialProperty albedoMap = null;
        MaterialProperty albedoColor = null;
        MaterialProperty intensity = null;
        MaterialProperty cutoff = null;

        const string alphaClippingKeyWord = "_ALPHATEST_ON";
        bool m_FirstTimeApply = true;

        private static class Styles
        {
            public static GUIContent albedoText = EditorGUIUtility.TrTextContent("Albedo", "Albedo (RGB) and Transparency (A)");
            public static GUIContent intensityText = EditorGUIUtility.TrTextContent("Intensity", "out Color * Intensity");
            public static GUIContent alphaClippingText = EditorGUIUtility.TrTextContent("Alpha Clipping");
            public static GUIContent cutOffText = EditorGUIUtility.TrTextContent("Cut Off");
            public static GUIContent gupInstance = EditorGUIUtility.TrTextContent("Enable GPU Instancing");
        }
        public void FindProperties(MaterialProperty[] props)
        {
            albedoMap = FindProperty("_BaseMap", props);
            albedoColor = FindProperty("_BaseColor", props);
            intensity = FindProperty("_Surface", props);
            cutoff = FindProperty("_Cutoff", props);
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            FindProperties(properties);
            m_MaterialEditor = materialEditor;
            Material material = materialEditor.target as Material;

            if (m_FirstTimeApply)
            {
                cutOffFlag = material.IsKeywordEnabled(alphaClippingKeyWord);
                gpuInstance = material.enableInstancing;
                m_FirstTimeApply = false;
            }

            ShaderPropertiesGUI(material);
            DoGpuInstance(material);
        }

        public void ShaderPropertiesGUI(Material material)
        {
            EditorGUI.BeginChangeCheck();
            {
                m_MaterialEditor.TexturePropertySingleLine(Styles.albedoText, albedoMap, albedoColor);
                m_MaterialEditor.ShaderProperty(intensity, Styles.intensityText);
                DoCutOffArea(material);
            }
            if (EditorGUI.EndChangeCheck())
            {
                m_MaterialEditor.ShaderProperty(intensity, Styles.intensityText);
            }
        }

        void DoCutOffArea(Material material)
        {
            if (CutOffEnableProperty(material))
            {
                SetKeyword(material, alphaClippingKeyWord,true);
                m_MaterialEditor.ShaderProperty(cutoff, Styles.cutOffText);
            }
            else
            {
                SetKeyword(material, alphaClippingKeyWord, false);
            }
        }

        bool CutOffEnableProperty(Material material)
        {
            EditorGUI.BeginChangeCheck();
            cutOffFlag = EditorGUILayout.Toggle(Styles.alphaClippingText, cutOffFlag);
            return cutOffFlag;
        }

        void DoGpuInstance(Material mat)
        {
            EditorGUI.BeginChangeCheck();
            {
                gpuInstance = EditorGUILayout.Toggle(Styles.gupInstance, gpuInstance);
                mat.enableInstancing = gpuInstance;
            }
        }


        static void SetKeyword(Material m, string keyword, bool state)
        {
            if (state)
                m.EnableKeyword(keyword);
            else
                m.DisableKeyword(keyword);
        }
    }
}

