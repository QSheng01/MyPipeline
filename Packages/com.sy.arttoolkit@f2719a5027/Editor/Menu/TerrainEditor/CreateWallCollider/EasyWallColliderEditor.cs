#if UNITY_EDITOR


using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SYArtToolkit.CreateEasyWallCollider
{
    [CustomEditor(typeof(EasyWallCollider))]
    class EasyWallColliderEditor : UnityEditor.Editor
    {
        protected virtual void OnSceneGUI()
        {

            EasyWallCollider easyWallCollider = (EasyWallCollider)target;
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



            //Transform selectedTransform = Selection.activeTransform;

            //if (selectedTransform == null || selectedTransform.GetComponent<Collider>() != null)
            //    return;
            //else if (selectedTransform.parent == null) return;
            //else if (selectedTransform.parent.GetComponent<EasyWallCollider>() == null) return;



            //// 获取所有子物体的碰撞体
            //Collider[] colliders = selectedTransform.parent.GetComponentsInChildren<Collider>();

            //// 绘制每个碰撞体的边界框
            //foreach (var collider in colliders)
            //{
            //    DrawColliderBounds(collider);
            //}



        }
        //private void DrawColliderBounds(Collider collider)
        //{
        //    // 获取碰撞体的边界框
        //    Bounds bounds = collider.bounds;

        //    // 设置Handles的颜色
        //    Handles.color = Color.yellow;

        //    // 绘制碰撞体的边界框
        //    Handles.DrawWireCube(bounds.center, bounds.size);
        //}
        public override void OnInspectorGUI()
        {
            EasyWallCollider easyWallCollider = (EasyWallCollider)target;
            if (easyWallCollider.CanBeEdited())
            {
                base.OnInspectorGUI();
                if (GUILayout.Button("销毁编辑界面，保留碰撞体"))
                {
                    SYArtToolkit.Submit.ArtToolkitData.Instance.AddMeshCombine(1);
                    WallColliderData wallColliderData = new WallColliderData(easyWallCollider);
                    ArtToolBoxLocalData data = null;
                    data = GetScriptableObjectDataArtToolBox.UpdateData(data);
                    data.wallColliderDatas.Add(wallColliderData);
                    EditorUtility.SetDirty(data);

                    if (Application.isPlaying)
                        Destroy(easyWallCollider);
                    else DestroyImmediate(easyWallCollider);
                }


            }
            else
            {
                GUILayout.Label("Editing polygon colliders is not allowed on Prefabs. Open the prefab and edit there instead");
            }
        }

        private void DrawButton(EasyWallCollider easyWallCollider, Vector3 from, Vector3 to, Transform cornerPrototype,
                                int newIndex)
        {
            Color prevColor = Handles.color;
            Handles.color = ((EasyWallCollider)target).buttonColor;

            float size = Vector3.Distance(from, to) / 20f;
            if (Handles.Button(ChangeY(((from + to) / 2), (y) => y - 0.001f),
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

        public static void DrawIcon(GameObject gameObject, int idx)
        {
            var largeIcons = GetTextures("sv_label_", string.Empty, 0, 8);
            var icon = largeIcons[idx];
#if UNITY_2021_2_OR_NEWER

            EditorGUIUtility.SetIconForObject(gameObject, (Texture2D)icon.image);
#else

            var egu = typeof(EditorGUIUtility);
            var flags = BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.NonPublic;
            var args = new object[] { gameObject, icon.image };
            var setIcon = egu.GetMethod("SetIconForObject", flags, null,
                new Type[] { typeof(UnityEngine.Object), typeof(Texture2D) }, null);
            setIcon.Invoke(null, args);

#endif
        }

        private static GUIContent[] GetTextures(string baseName, string postFix, int startIndex, int count)
        {
            GUIContent[] array = new GUIContent[count];
            for (int i = 0; i < count; i++)
            {
                array[i] = EditorGUIUtility.IconContent(baseName + (startIndex + i) + postFix);
            }

            return array;
        }




        public static Vector3 ChangeY(Vector3 parent, float newY)
        {
            return new Vector3(parent.x, newY, parent.z);
        }


        public static Vector3 ChangeY(Vector3 parent, FloatEdit edit)
        {
            return new Vector3(parent.x, edit(parent.y), parent.z);
        }


        public delegate float FloatEdit(float input);













    }
}
#endif