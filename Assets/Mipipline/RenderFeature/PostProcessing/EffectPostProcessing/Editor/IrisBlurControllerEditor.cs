using EffectsPostProcess.Scripts.Controllers;
using UnityEditor;
using UnityEngine;

namespace EffectsPostProcess.Scripts.ControllerEditors
{
    public partial class EffectsPostProcessControllerEditor
    {
        private bool _irisBlurGroup = false;

        private SerializedProperty irisBlurAlpha = null;
        private SerializedProperty irisBlurStrength = null;
        private SerializedProperty irisBlurRadius = null;
        private SerializedProperty irisBlurCenter = null;

        private void InitIrisBlurProperties()
        {
            irisBlurAlpha = serializedObject.FindProperty("irisBlurAlpha");
            irisBlurStrength = serializedObject.FindProperty("irisBlurStrength");
            irisBlurRadius = serializedObject.FindProperty("irisBlurRadius");
            irisBlurCenter = serializedObject.FindProperty("irisBlurCenter");
        }

        private void IrisBlurComp()
        {
            if (!((EffectsPostProcessController)target).selectedRenderersList.Contains(Controllers.EffectsPostProcess.IrisBlur))
            {
                return;
            }

            _irisBlurGroup = EditorGUILayout.BeginFoldoutHeaderGroup(_irisBlurGroup, "光圈模糊（IrisBlur）");
            EditorGUI.indentLevel++;
            if (_irisBlurGroup)
            {
                EditorGUILayout.BeginHorizontal();

                OptionButton(Controllers.EffectsPostProcess.IrisBlur);

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(irisBlurAlpha);
                EditorGUILayout.PropertyField(irisBlurStrength);
                EditorGUILayout.PropertyField(irisBlurRadius);
                EditorGUILayout.PropertyField(irisBlurCenter);
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(20);
        }
    }
}