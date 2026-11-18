using UnityEditor;

namespace Grass
{
    // [CustomEditor(typeof(GrassInstanceData))]
    public class GrassInstanceDataEditor : UnityEditor.Editor
    {
        SerializedProperty _lodMeshs;
        SerializedProperty _instanceMat;
        SerializedProperty _indirectShader;
        GrassInstanceData _scrpits;
        private void OnEnable()
        {
            _scrpits = target as GrassInstanceData;
#if UNITY_EDITOR
            //if (_scrpits.Init())
            //{
            //    EditorUtility.SetDirty(_scrpits.gameObject);
                // var guid = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(_scrpits.gameObject));
                // if (guid != null)
                // {
                //     AssetDatabase.SaveAssetIfDirty(guid);
                // }
            //    AssetDatabase.SaveAssets();
            //}
#endif
        }

        public override void OnInspectorGUI()
        {
            _lodMeshs = serializedObject.FindProperty("_lodMeshs");
            _instanceMat = serializedObject.FindProperty("_instanceMat");
            _indirectShader = serializedObject.FindProperty("_indirectShader");
            EditorGUILayout.PropertyField(_indirectShader);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(_instanceMat);
            EditorGUILayout.PropertyField(_lodMeshs);
            EditorGUI.EndDisabledGroup();
            serializedObject.ApplyModifiedProperties();
        }
    }
}