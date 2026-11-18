using EffectsPostProcess.Scripts.Controllers;
using UnityEditor;
using UnityEngine;

namespace EffectsPostProcess.Scripts.ControllerEditors
{
    public partial class EffectsPostProcessControllerEditor
    {
        private bool _rgbSplitGlithGroup   = false;

       // private SerializedProperty rgbSplitGlithAlpha = null;
        
        private SerializedProperty Splitdirection   = null;
        private SerializedProperty Amount           = null;
        private SerializedProperty Speed            = null;
        private SerializedProperty Fading           = null;
        private SerializedProperty CenterFading     = null;
        private SerializedProperty AmountR          = null;
        private SerializedProperty AmountB          = null;
        private void InitRGBSplitGlithProperties()
        {
        //    rgbSplitGlithAlpha = serializedObject.FindProperty("rgbSplitGlithAlpha");
            
            Splitdirection = serializedObject.FindProperty("Splitdirection");
            Amount         = serializedObject.FindProperty("Amount");
            Speed          = serializedObject.FindProperty("Speed");
            Fading         = serializedObject.FindProperty("Fading");
            CenterFading   = serializedObject.FindProperty("CenterFading");
            AmountR        = serializedObject.FindProperty("AmountR");
            AmountB        = serializedObject.FindProperty("AmountB");
        }

        private void RGBSplitComp()
        {
            if ( ! ( (EffectsPostProcessController)target ).selectedRenderersList.Contains(Controllers.EffectsPostProcess.RGBSplit) )
            {
                return;
            }
            
            _rgbSplitGlithGroup = EditorGUILayout.BeginFoldoutHeaderGroup(_rgbSplitGlithGroup, "RGB分离（RGB Split）");
            EditorGUI.indentLevel++;
            
            if (_rgbSplitGlithGroup)
            {
                EditorGUILayout.BeginHorizontal();
                
                OptionButton(Controllers.EffectsPostProcess.RGBSplit);

                EditorGUILayout.EndHorizontal();
           //     EditorGUILayout.PropertyField(rgbSplitGlithAlpha);

                EditorGUILayout.PropertyField(Splitdirection);
                EditorGUILayout.PropertyField(Amount        );
                EditorGUILayout.PropertyField(Speed         );
                EditorGUILayout.PropertyField(Fading        );
                EditorGUILayout.PropertyField(CenterFading  );
                EditorGUILayout.PropertyField(AmountR       );
                EditorGUILayout.PropertyField(AmountB       );
                
            } 
            
            EditorGUI.indentLevel--;
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(20);
        }
    }
}