using EffectsPostProcess.Scripts.Controllers;
using UnityEditor;
using UnityEngine;

namespace EffectsPostProcess.Scripts.ControllerEditors
{
    public partial class EffectsPostProcessControllerEditor
    {
        private bool _radialBlurGroup      = false; 
        
        // private SerializedProperty radialBlurDownSample = null;
        private SerializedProperty radialBlurAlpha      = null;
        private SerializedProperty radialBlurStrength   = null;
        private SerializedProperty radialBlurRadius     = null;
        private SerializedProperty radialBlurCenter     = null;

        private SerializedProperty radialBlurRGBSplitEnable        = null;
        private SerializedProperty radialBlurROffset               = null;
        private SerializedProperty radialBlurGOffset               = null;
        private SerializedProperty radialBlurBOffset               = null;
        private SerializedProperty radialBlurRGBSplitStrength      = null;

        private void InitRadialBlurProperties()
        {
            // radialBlurDownSample = serializedObject.FindProperty("radialBlurDownSample");
            radialBlurAlpha      = serializedObject.FindProperty("radialBlurAlpha");
            radialBlurStrength   = serializedObject.FindProperty("radialBlurStrength");
            radialBlurRadius     = serializedObject.FindProperty("radialBlurRadius");
            radialBlurCenter     = serializedObject.FindProperty("radialBlurCenter");
            
            radialBlurRGBSplitEnable   = serializedObject.FindProperty("radialBlurRGBSplitEnable");
            radialBlurROffset          = serializedObject.FindProperty("radialBlurROffset");
            radialBlurGOffset          = serializedObject.FindProperty("radialBlurGOffset");
            radialBlurBOffset          = serializedObject.FindProperty("radialBlurBOffset");
            radialBlurRGBSplitStrength = serializedObject.FindProperty("radialBlurRGBSplitStrength");
        }
           
        private void RadialBlurComp()
        {
            if ( ! ( (EffectsPostProcessController)target ).selectedRenderersList.Contains(Controllers.EffectsPostProcess.RadialBlur) )
            {
                return;
            }
            
            _radialBlurGroup = EditorGUILayout.BeginFoldoutHeaderGroup(_radialBlurGroup, "径向模糊（RadialBlur）");
            EditorGUI.indentLevel++;
            if (_radialBlurGroup)
            {
                EditorGUILayout.BeginHorizontal();
                
                OptionButton(Controllers.EffectsPostProcess.RadialBlur);
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.PropertyField(radialBlurAlpha     );
                //EditorGUILayout.PropertyField(radialBlurDownSample);
                EditorGUILayout.PropertyField(radialBlurStrength  );
                EditorGUILayout.PropertyField(radialBlurRadius    );
                EditorGUILayout.PropertyField(radialBlurCenter    );

                EditorGUILayout.PropertyField(radialBlurRGBSplitEnable);
                if (radialBlurRGBSplitEnable.boolValue)
                {
                    EditorGUILayout.PropertyField(radialBlurRGBSplitStrength);
                    EditorGUILayout.PropertyField(radialBlurROffset);
                    EditorGUILayout.PropertyField(radialBlurGOffset);
                    EditorGUILayout.PropertyField(radialBlurBOffset);
                }
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(20);
        }
    }
}