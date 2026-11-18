using System.Threading.Tasks;
using EffectsPostProcess.Scripts.Controllers;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine; 

namespace EffectsPostProcess.Scripts.ControllerEditors
{
    public partial class EffectsPostProcessControllerEditor
    {
        private bool _colorStyleGroup      = false;
        
        //private SerializedProperty downSample = null;
        private SerializedProperty colorStyleAlpha = null;
        private SerializedProperty grayCenter = null;
        private SerializedProperty grayOffset = null;
        private SerializedProperty blackAreaColor = null;
        private SerializedProperty whiteAreaColor = null;
        private SerializedProperty colorInvert = null; 

        private void InitColorStyleProperties()
        {
            colorInvert       = serializedObject.FindProperty("colorInvert");
            colorStyleAlpha   = serializedObject.FindProperty("colorStyleAlpha");
            grayCenter        = serializedObject.FindProperty("grayCenter");
            grayOffset        = serializedObject.FindProperty("grayOffset");
            blackAreaColor    = serializedObject.FindProperty("blackAreaColor");
            whiteAreaColor    = serializedObject.FindProperty("whiteAreaColor");
        }
        
        private void ColorStyleComp()
        {
            if ( ! ( (EffectsPostProcessController)target ).selectedRenderersList.Contains(Controllers.EffectsPostProcess.ColorStyle) )
            {
                return;
            }
            
            _colorStyleGroup = EditorGUILayout.BeginFoldoutHeaderGroup(_colorStyleGroup, "黑白风格（ColorStyle）");
            EditorGUI.indentLevel++;
            
            if (_colorStyleGroup)
            {
                EditorGUILayout.BeginHorizontal();

                OptionButton(Controllers.EffectsPostProcess.ColorStyle);
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.PropertyField(colorStyleAlpha);
                EditorGUILayout.PropertyField(colorInvert    );
                
                EditorGUILayout.PropertyField(grayCenter     );
                EditorGUILayout.PropertyField(grayOffset     );
                EditorGUILayout.PropertyField(blackAreaColor );
                EditorGUILayout.PropertyField(whiteAreaColor );
            }
            
            EditorGUI.indentLevel--;
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(20);
        }
    }
}