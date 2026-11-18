using UnityEditor;
using UnityEngine;

namespace MPipeline
{
    [CustomEditor(typeof(ClusterGeometryAsset))]
    public sealed class ClusterGeometryAssetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            ClusterGeometryAsset asset = (ClusterGeometryAsset)target;

            EditorGUILayout.HelpBox(
                "This asset contains baked GPU cluster data. Large vertex arrays are hidden to keep the Inspector responsive.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("Clusters", asset.ClusterCount);
                EditorGUILayout.IntField("Points", asset.PointCount);
                EditorGUILayout.IntField("Triangle Materials", asset.TriangleMaterials == null ? 0 : asset.TriangleMaterials.Length);
                EditorGUILayout.IntField("Source Materials", asset.SourceMaterials == null ? 0 : asset.SourceMaterials.Length);
                EditorGUILayout.BoundsField("Local Bounds", asset.LocalBounds);
            }
        }
    }
}
