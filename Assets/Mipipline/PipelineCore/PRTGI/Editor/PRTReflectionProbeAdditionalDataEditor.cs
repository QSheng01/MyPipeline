using Illusion.Rendering.PRTGI;
using UnityEditor;
using UnityEngine;

namespace Illusion.Rendering.PRTGI.Editor
{
    [CustomEditor(typeof(PRTReflectionProbeAdditionalData))]
    internal sealed class PRTReflectionProbeAdditionalDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var data = (PRTReflectionProbeAdditionalData)target;
            EditorGUILayout.HelpBox(data.HasValidData
                ? "Reflection reference SH is baked. This probe can normalize PRTGI specular lighting."
                : "Capture the reflection reference SH to normalize specular lighting against nearby PRTGI.",
                data.HasValidData ? MessageType.Info : MessageType.Warning);
            using (new EditorGUI.DisabledScope(Application.isPlaying || PRTBakeManager.IsBaking))
            {
                if (GUILayout.Button("Bake Reflection Normalization")) PRTBakeManager.BakeReflectionNormalization(data);
                using (new EditorGUI.DisabledScope(!data.HasValidData))
                    if (GUILayout.Button("Clear Reflection Reference"))
                    {
                        Undo.RecordObject(data, "Clear PRT Reflection Reference");
                        data.Clear();
                        EditorUtility.SetDirty(data);
                    }
            }
        }
    }
}
