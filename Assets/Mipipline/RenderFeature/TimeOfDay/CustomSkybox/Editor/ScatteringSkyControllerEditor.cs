using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ScatteringSkyController))]
public class ScatteringSkyControllerEditor : UnityEditor.Editor
{
    // Serialized properties
    private SerializedProperty m_referencesHeaderGroup;
    private SerializedProperty m_scatteringHeaderGroup;
    private SerializedProperty m_outerSpaceHeaderGroup;
    private SerializedProperty m_cloudsHeaderGroup;
    
    //reference
    private SerializedProperty m_skyMaterial;
    private SerializedProperty m_sunTransform;
    private SerializedProperty m_moonTransform;
    private SerializedProperty m_dynamicCloudsTexture;
    private SerializedProperty m_moonTexture;
    private SerializedProperty m_sunTexture;
    
    //Scattering
    private SerializedProperty m_molecularDensity;
    private SerializedProperty m_wavelengthR;
    private SerializedProperty m_wavelengthG;
    private SerializedProperty m_wavelengthB;
    private SerializedProperty m_kr;
    private SerializedProperty m_km;
    private SerializedProperty m_rayleigh;
    private SerializedProperty m_mie;
    private SerializedProperty m_mieG;
    private SerializedProperty m_mieDistance;
    private SerializedProperty m_scattering;
    private SerializedProperty m_luminance;
    private SerializedProperty m_exposure;
    private SerializedProperty m_rayleighColor;
    private SerializedProperty m_mieColor;
    private SerializedProperty m_scatteringColor;
    private SerializedProperty m_scatteringMode;
    
    //Clouds
    private SerializedProperty m_dynamicCloudsAltitude;
    private SerializedProperty m_dynamicCloudsDirection;
    private SerializedProperty m_dynamicCloudsSpeed;
    private SerializedProperty m_dynamicCloudsDensity;
    private SerializedProperty m_dynamicCloudsColor1;
    private SerializedProperty m_dynamicCloudsColor2;
    
    //OuterSpace
    private SerializedProperty m_sunTextureSize;
    private SerializedProperty m_sunTextureIntensity;
    private SerializedProperty m_sunTextureColor;
    private SerializedProperty m_moonTextureSize;
    private SerializedProperty m_moonTextureIntensity;
    private SerializedProperty m_moonTextureColor;
    
    private Rect m_controlRect;
    private Rect m_headerRect;
    private void OnEnable()
    {
        // Find the serialized properties
        m_referencesHeaderGroup = serializedObject.FindProperty("m_referencesHeaderGroup");
        m_scatteringHeaderGroup = serializedObject.FindProperty("m_scatteringHeaderGroup");
        m_outerSpaceHeaderGroup = serializedObject.FindProperty("m_outerSpaceHeaderGroup");
        m_cloudsHeaderGroup = serializedObject.FindProperty("m_cloudsHeaderGroup");
        
        // m_dynamicCloudsTexture = serializedObject.FindProperty("m_dynamicCloudsTexture");
        // m_moonTexture = serializedObject.FindProperty("m_moonTexture");
        // m_sunTexture = serializedObject.FindProperty("m_sunTexture");
        m_skyMaterial = serializedObject.FindProperty("m_skyMaterial");
        // m_sunTransform = serializedObject.FindProperty("m_sunTransform");
        // m_moonTransform = serializedObject.FindProperty("m_moonTransform");

        // m_molecularDensity = serializedObject.FindProperty("molecularDensity");
        // m_wavelengthR = serializedObject.FindProperty("wavelengthR");
        // m_wavelengthG = serializedObject.FindProperty("wavelengthG");
        // m_wavelengthB = serializedObject.FindProperty("wavelengthB");
        m_kr = serializedObject.FindProperty("kr");
        m_km = serializedObject.FindProperty("km");
        m_rayleigh = serializedObject.FindProperty("rayleigh");
        m_mie = serializedObject.FindProperty("mie");
        m_mieG = serializedObject.FindProperty("mieG");
        m_scattering = serializedObject.FindProperty("scattering");
        // m_luminance = serializedObject.FindProperty("luminance");
        // m_exposure = serializedObject.FindProperty("exposure");
        // m_rayleighColor = serializedObject.FindProperty("rayleighColor");
        // m_mieColor = serializedObject.FindProperty("mieColor");
        // m_scatteringMode = serializedObject.FindProperty("m_scatteringMode");
        // m_scatteringColor = serializedObject.FindProperty("scatteringColor");
        
        // m_dynamicCloudsAltitude = serializedObject.FindProperty("dynamicCloudsAltitude");
        // m_dynamicCloudsDirection = serializedObject.FindProperty("dynamicCloudsDirection");
        // m_dynamicCloudsSpeed = serializedObject.FindProperty("dynamicCloudsSpeed");
        // m_dynamicCloudsDensity = serializedObject.FindProperty("dynamicCloudsDensity");
        // m_dynamicCloudsColor1 = serializedObject.FindProperty("dynamicCloudsColor1");
        // m_dynamicCloudsColor2 = serializedObject.FindProperty("dynamicCloudsColor2");
        //
        // m_sunTextureSize = serializedObject.FindProperty("sunTextureSize");
        // m_sunTextureIntensity = serializedObject.FindProperty("sunTextureIntensity");
        // m_sunTextureColor = serializedObject.FindProperty("sunTextureColor");
        // m_moonTextureSize = serializedObject.FindProperty("moonTextureSize");
        // m_moonTextureIntensity = serializedObject.FindProperty("moonTextureIntensity");
        // m_moonTextureColor = serializedObject.FindProperty("moonTextureColor");
    }

    public override void OnInspectorGUI()
    {
        // Start custom inspector
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();
        
        // References header group
        m_controlRect = EditorGUILayout.GetControlRect();
        m_headerRect = new Rect(m_controlRect.x + 15, m_controlRect.y, m_controlRect.width - 20, EditorGUIUtility.singleLineHeight);
        m_referencesHeaderGroup.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(m_headerRect, m_referencesHeaderGroup.isExpanded, GUIContent.none, "RL Header");
        EditorGUI.Foldout(m_headerRect, m_referencesHeaderGroup.isExpanded, GUIContent.none);
        EditorGUI.LabelField(m_headerRect, new GUIContent("资产引用", ""));
        if (m_referencesHeaderGroup.isExpanded)
        {
            EditorGUILayout.Space(2);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_skyMaterial);
            // EditorGUILayout.PropertyField(m_sunTransform);
            // EditorGUILayout.PropertyField(m_moonTransform);
            // EditorGUILayout.PropertyField(m_dynamicCloudsTexture);
            // EditorGUILayout.PropertyField(m_sunTexture);
            // EditorGUILayout.PropertyField(m_moonTexture);

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(2);
        
        // Scattering header group
        m_controlRect = EditorGUILayout.GetControlRect();
        m_headerRect = new Rect(m_controlRect.x + 15, m_controlRect.y, m_controlRect.width - 20, EditorGUIUtility.singleLineHeight);
        m_scatteringHeaderGroup.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(m_headerRect, m_scatteringHeaderGroup.isExpanded, GUIContent.none, "RL Header");
        EditorGUI.Foldout(m_headerRect, m_scatteringHeaderGroup.isExpanded, GUIContent.none);
        EditorGUI.LabelField(m_headerRect, new GUIContent("大气散射", ""));
        if (m_scatteringHeaderGroup.isExpanded)
        {
            EditorGUILayout.Space(2);
            EditorGUI.indentLevel++;
            // EditorGUILayout.Slider(m_molecularDensity, 0.1f, 3.0f,"大气分子密度（不清楚不要改）");
            // EditorGUILayout.Slider(m_wavelengthR, 380f, 740f,"可见光红光波长（不清楚不要改）");
            // EditorGUILayout.Slider(m_wavelengthG, 380f, 740f,"可见光绿光波长（不清楚不要改）");
            // EditorGUILayout.Slider(m_wavelengthB, 380f, 740f,"可见光蓝光波长（不清楚不要改）");
            EditorGUILayout.Slider(m_kr, 0f, 20f,"Rayleigh散射海拔高度");
            EditorGUILayout.Slider(m_km, 0f, 10f,"Mie散射海拔高度");
            EditorGUILayout.Slider(m_rayleigh, 0f, 5f, "Rayleigh散射程度");
            EditorGUILayout.Slider(m_mie, 0f, 5f,"Mie散射程度");
            EditorGUILayout.Slider(m_mieG, 0f, 1f,"Mie各向异性");
            // EditorGUILayout.Slider(m_scattering, 0f, 1f,"整体散射强度");
            // EditorGUILayout.Slider(m_luminance, 0f, 5f,"保底天空亮度");
            // EditorGUILayout.Slider(m_exposure, 0f, 10f,"整体曝光程度");
            // EditorGUILayout.PropertyField(m_rayleighColor,new GUIContent("Rayleigh散射颜色"));
            // EditorGUILayout.PropertyField(m_mieColor,new GUIContent("Mie散射颜色"));
            // EditorGUILayout.PropertyField(m_scatteringMode,new GUIContent("散射模式"));
            // EditorGUILayout.PropertyField(m_scatteringColor,new GUIContent("整体散射颜色"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(2);
        
        // Outer space header group
        //m_controlRect = EditorGUILayout.GetControlRect();
        //m_headerRect = new Rect(m_controlRect.x + 15, m_controlRect.y, m_controlRect.width - 20, EditorGUIUtility.singleLineHeight);
        //m_outerSpaceHeaderGroup.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(m_headerRect, m_outerSpaceHeaderGroup.isExpanded, GUIContent.none, "RL Header");
        // EditorGUI.Foldout(m_headerRect, m_outerSpaceHeaderGroup.isExpanded, GUIContent.none);
        // //EditorGUI.LabelField(m_headerRect, new GUIContent("天空外层", ""));
        // // if (m_outerSpaceHeaderGroup.isExpanded)
        // // {
        // //     //EditorGUILayout.Space(2);
        // //     //EditorGUI.indentLevel++;
        // //     // EditorGUILayout.Slider(m_sunTextureSize, 0.5f, 10f,"太阳贴图尺寸");
        // //     // EditorGUILayout.Slider(m_sunTextureIntensity, 0f, 2f,"太阳贴图强度");
        // //     // EditorGUILayout.PropertyField(m_sunTextureColor,new GUIContent("太阳颜色"));
        // //     // EditorGUILayout.Slider(m_moonTextureSize, 1f, 20f,"月亮贴图尺寸");
        // //     // EditorGUILayout.Slider(m_moonTextureIntensity, 0f, 2f,"月亮贴图强度");
        // //     // EditorGUILayout.PropertyField(m_moonTextureColor,new GUIContent("月亮颜色"));
        // //     //EditorGUI.indentLevel--;
        // // }
        // EditorGUILayout.EndFoldoutHeaderGroup();
        // EditorGUILayout.Space(2);
        
        // Clouds header group
        // m_controlRect = EditorGUILayout.GetControlRect();
        // m_headerRect = new Rect(m_controlRect.x + 15, m_controlRect.y, m_controlRect.width - 20, EditorGUIUtility.singleLineHeight);
        // m_cloudsHeaderGroup.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(m_headerRect, m_cloudsHeaderGroup.isExpanded, GUIContent.none, "RL Header");
        // EditorGUI.Foldout(m_headerRect, m_cloudsHeaderGroup.isExpanded, GUIContent.none);
        // EditorGUI.LabelField(m_headerRect, new GUIContent("云", ""));
        // if (m_cloudsHeaderGroup.isExpanded)
        // {
        //     EditorGUILayout.Space(2);
        //     EditorGUI.indentLevel++;
        //     EditorGUILayout.Slider(m_dynamicCloudsAltitude, 1f, 20f,"云高度");
        //     EditorGUILayout.Slider(m_dynamicCloudsDirection, 0f, 1f,"云方向");
        //     EditorGUILayout.Slider(m_dynamicCloudsSpeed, 0f, 1f,"云速度");
        //     EditorGUILayout.Slider(m_dynamicCloudsDensity, 0f, 1f,"云厚度");
        //     EditorGUILayout.PropertyField(m_dynamicCloudsColor1,new GUIContent("云颜色"));
        //     EditorGUILayout.PropertyField(m_dynamicCloudsColor2,new GUIContent("云颜色2"));
        //     EditorGUI.indentLevel--;
        // }
        // EditorGUILayout.EndFoldoutHeaderGroup();
        // EditorGUILayout.Space(2);
        
        // Update the inspector when there is a change
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }
    }

}
