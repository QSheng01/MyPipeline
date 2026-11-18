using Illusion.Rendering.PRTGI;
using UnityEditor;
using UnityEngine;

namespace Illusion.Rendering.PRTGI.Editor
{
    [CustomEditor(typeof(PRTProbeAdjustmentVolume))]
    [CanEditMultipleObjects]
    internal sealed class PRTProbeAdjustmentVolumeEditor : UnityEditor.Editor
    {
        private bool _editDirection;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Influence Volume", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            var shape = serializedObject.FindProperty("shape");
            EditorGUILayout.PropertyField(shape, new GUIContent("Shape"));
            if (shape.enumValueIndex == (int)PRTProbeAdjustmentShape.Box)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("size"), new GUIContent("Size"));
            else EditorGUILayout.PropertyField(serializedObject.FindProperty("radius"), new GUIContent("Radius"));
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Probe Volume Overrides", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            var mode = serializedObject.FindProperty("mode");
            EditorGUILayout.PropertyField(mode, new GUIContent("Mode"));
            switch ((PRTProbeAdjustmentMode)mode.enumValueIndex)
            {
                case PRTProbeAdjustmentMode.ApplyVirtualOffset:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("virtualOffsetRotation"), new GUIContent("Rotation"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("virtualOffsetDistance"), new GUIContent("Distance"));
                    if (targets.Length == 1) _editDirection = GUILayout.Toggle(_editDirection, "Edit Direction in Scene", EditorStyles.miniButton);
                    break;
                case PRTProbeAdjustmentMode.OverrideVirtualOffsetSettings:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("geometryBias"), new GUIContent("Geometry Bias"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("rayOriginBias"), new GUIContent("Ray Origin Bias"));
                    break;
                case PRTProbeAdjustmentMode.IntensityScale:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("intensityScale"), new GUIContent("Intensity Scale"));
                    EditorGUILayout.HelpBox("Changes the baked probe intensity inside this volume. Rebake to apply changes.", MessageType.Info);
                    break;
                case PRTProbeAdjustmentMode.InvalidateProbes:
                    EditorGUILayout.HelpBox("Marks probes inside the volume invalid during baking. Rebake to apply changes.", MessageType.Info);
                    break;
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showVolumeBounds"), new GUIContent("Show Volume Bounds"));
            EditorGUI.indentLevel--;
            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            if (!_editDirection || targets.Length != 1) return;
            var volume = (PRTProbeAdjustmentVolume)target;
            if (volume.mode != PRTProbeAdjustmentMode.ApplyVirtualOffset) return;
            EditorGUI.BeginChangeCheck();
            Quaternion rotation = Handles.RotationHandle(Quaternion.Euler(0f, volume.virtualOffsetRotation, 0f), volume.transform.position);
            if (!EditorGUI.EndChangeCheck()) return;
            Undo.RecordObject(volume, "Change PRT Virtual Offset Direction");
            volume.virtualOffsetRotation = rotation.eulerAngles.y;
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.InSelectionHierarchy)]
        private static void DrawVolume(PRTProbeAdjustmentVolume volume, GizmoType gizmoType)
        {
            if (!volume.ShowVolumeBounds) return;
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = volume.transform.localToWorldMatrix;
            Gizmos.color = new Color(0.87f, 0.52f, 0.56f, 0.2f);
            if (volume.shape == PRTProbeAdjustmentShape.Box)
            {
                Gizmos.DrawCube(Vector3.zero, volume.size);
                Gizmos.color = new Color(0.87f, 0.52f, 0.56f, 0.85f);
                Gizmos.DrawWireCube(Vector3.zero, volume.size);
            }
            else
            {
                Gizmos.DrawSphere(Vector3.zero, volume.radius);
                Gizmos.color = new Color(0.87f, 0.52f, 0.56f, 0.85f);
                Gizmos.DrawWireSphere(Vector3.zero, volume.radius);
            }
            Gizmos.matrix = previous;
        }
    }
}
