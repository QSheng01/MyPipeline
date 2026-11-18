#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;


namespace SYArtToolkit.CreateEasyWallCollider
{
    [CustomEditor(typeof(NodeShowCollider))]
    public class NodeShowEditor : Editor
    {
        protected virtual void OnSceneGUI()
        {


            EasyWallCollider easyWallCollider = ((NodeShowCollider)target).easyWallCollider;
            if (!easyWallCollider.CanBeEdited() || !easyWallCollider.isActiveAndEnabled) return;
            var corners = easyWallCollider.corners;
            if (corners.Count < 2) return;
            for (int i = 0; i < corners.Count + easyWallCollider.loopInt(); i++)
            {
                Vector3 from = corners[i].position;
                Vector3 to = corners[(i + 1) % corners.Count].position;
                DrawButton(easyWallCollider, from, to, corners[i], corners[i].GetSiblingIndex() + 1);
            }

            if (!easyWallCollider.loop)
            {
                DrawButton(easyWallCollider, corners[0].position,
                    corners[0].position + (corners[0].position - corners[1].position), corners[0],
                    corners[0].GetSiblingIndex());
                int lastIx = corners.Count - 1;
                DrawButton(easyWallCollider, corners[lastIx].position,
                    corners[lastIx].position + (corners[lastIx].position - corners[lastIx - 1].position),
                    corners[lastIx], corners[lastIx].GetSiblingIndex() + 1);
            }






        }


        private void DrawButton(EasyWallCollider easyWallCollider, Vector3 from, Vector3 to, Transform cornerPrototype,
                            int newIndex)
        {
            Color prevColor = Handles.color;
            Handles.color = ((NodeShowCollider)target).easyWallCollider.buttonColor;

            float size = Vector3.Distance(from, to) / 20f;
            if (Handles.Button(EasyWallColliderEditor.ChangeY(((from + to) / 2), (y) => y - 0.001f),
                Quaternion.LookRotation(from - to) * Quaternion.Euler(90, 0, 0), size, size,
                Handles.ConeHandleCap))
            {
                var newCorner = Instantiate(cornerPrototype, (from + to) / 2, Quaternion.identity,
                    easyWallCollider.transform);
                newCorner.SetSiblingIndex(newIndex);
                if (newCorner.gameObject.GetComponent<NodeShowCollider>() == null)
                {

                    newCorner.gameObject.AddComponent<NodeShowCollider>();

                }
                Selection.activeGameObject = newCorner.gameObject;
            }

            Handles.color = prevColor;
        }


    }
}
#endif