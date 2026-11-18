using UnityEditor;

namespace Grass
{
    [CustomEditor(typeof(GrassPainterData))]
    public class GrassPainterDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("RootBound"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("DrawObjArray"));
                var length = serializedObject.FindProperty("Datas").arraySize;
                if (length > 100)
                {
                    EditorGUILayout.LabelField("Datas Count :" + length);
                }
                else
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("Datas"));
                }

            }
        }

    }
}

