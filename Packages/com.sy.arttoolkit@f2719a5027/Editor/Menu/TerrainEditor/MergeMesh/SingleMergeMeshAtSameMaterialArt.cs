using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;


namespace SYArtToolkit
{
    public class SingleMeshMerge
    {
        private Material material;
        private bool isFold;
        private readonly List<(GameObject gameObject, bool isSelect)> gameObjectSelect;
        private readonly ReorderableList reorderableList;
        private static ReorderableList reorderablieListEmpty;
        private static List<(GameObject gameObject, bool isSelect)> gameobjectSelectEmpty;
        private static GUIStyle style;

        public SingleMeshMerge(Material mat, List<GameObject> gameObjects)
        {
            material = mat;
            gameObjectSelect = new List<(GameObject gameObject, bool isSelect)>();
            foreach (var gameObject in gameObjects)
            {
                gameObjectSelect.Add((gameObject, true));
            }

            reorderableList = new ReorderableList(gameObjectSelect, typeof((GameObject, bool)));
            reorderableList.drawElementCallback = DrawElementCallback;
            reorderableList.drawHeaderCallback = DrawHeaderCallback;
            reorderableList.drawFooterCallback = DrawFooterCallback;
            reorderableList.displayAdd = false;
            reorderableList.displayRemove = false;
            isFold = true;

            if (reorderablieListEmpty == null)
            {
                gameobjectSelectEmpty = new List<(GameObject gameObject, bool isSelect)>();
                reorderablieListEmpty = new ReorderableList(gameobjectSelectEmpty, typeof((GameObject, bool)));
                reorderablieListEmpty.elementHeight = 0;
                reorderablieListEmpty.drawFooterCallback = EmptyDrawFooterCallback;
                style = new GUIStyle();
                style.fontStyle = FontStyle.Italic;
            }
        }

        public void Show(Rect rect)
        {
            if (isFold)
            {
                reorderableList.DoList(rect);
            }
            else
            {
                reorderablieListEmpty.drawHeaderCallback = DrawHeaderCallback;
                reorderablieListEmpty.DoList(rect);
            }
        }

        public float Height()
        {
            if (isFold)
                return reorderableList.GetHeight();
            return reorderablieListEmpty.GetHeight();
        }

        public void MergeMesh(DefaultAsset defaultAsset)
        {
            List<GameObject> temp = new List<GameObject>();
            foreach (var o in gameObjectSelect)
            {
                if (o.isSelect && o.gameObject != null) temp.Add(o.gameObject);
            }

            if (temp.Count == 0)
            {
                return;
            }
;
            SingleMergeMeshAtSameMaterialFunction.MergeMesh(temp,
                material, defaultAsset);
        }

        private void DrawHeaderCallback(Rect rect)
        {
            isFold = EditorGUI.Foldout(new Rect(rect.position, new Vector2(20, rect.height)), isFold, "");
            if (GUI.Button(new Rect(rect.x + 20, rect.y, rect.width, rect.height),
                    "<color=grey>" + material.name + "</color>", style))
            {
                EditorGUIUtility.PingObject(material);
                Selection.activeObject = material;
            }
        }

        private void DrawElementCallback(Rect rect, int index, bool selected, bool focused)
        {
            if (gameObjectSelect == null || gameObjectSelect.Count <= 0) return;
            if (gameObjectSelect[index].gameObject == null)
            {
                GUI.Label(rect, "物体已被销毁");
                return;
            }

            bool boolTemp = EditorGUI.Toggle(new Rect(rect.x, rect.y, 10, 10), gameObjectSelect[index].isSelect);
            gameObjectSelect[index] = (gameObjectSelect[index].gameObject, boolTemp);
            if (GUI.Button(new Rect(rect.x + 40, rect.y, rect.width - 40, rect.height),
                    "<color=white>" + gameObjectSelect[index].gameObject.name + "</color>", style))
            {
                EditorGUIUtility.PingObject(gameObjectSelect[index].gameObject);
                Selection.activeObject = gameObjectSelect[index].gameObject;
            }
        }

        private void DrawFooterCallback(Rect rect)
        {
            if (GUI.Button(new Rect(rect.position, new Vector2(60, rect.height)), "全选"))
            {
                for (int i = 0; i < gameObjectSelect.Count; ++i)
                {
                    gameObjectSelect[i] = (gameObjectSelect[i].gameObject, true);
                }
            }

            if (GUI.Button(new Rect(rect.x + 60, rect.y, 60, rect.height), "反选"))
            {
                for (int i = 0; i < gameObjectSelect.Count; ++i)
                {
                    gameObjectSelect[i] = (gameObjectSelect[i].gameObject, !gameObjectSelect[i].isSelect);
                }
            }

            if (GUI.Button(new Rect(rect.x + 120, rect.y, 60, rect.height), "全不选"))
            {
                for (int i = 0; i < gameObjectSelect.Count; ++i)
                {
                    gameObjectSelect[i] = (gameObjectSelect[i].gameObject, false);
                }
            }

            EditorGUI.BeginDisabledGroup(gameObjectSelect.Count == 0 || MergeMeshAtSameMaterial.defaultAsset == null);
            if (GUI.Button(new Rect(rect.x + 180, rect.y, 60, rect.height), "合成"))
            {
                MergeMesh(MergeMeshAtSameMaterial.defaultAsset);
            }

            EditorGUI.EndDisabledGroup();
        }

        private static void EmptyDrawFooterCallback(Rect rect)
        {
            EditorGUI.BeginDisabledGroup(true);
            GUI.Button(new Rect(rect.position, new Vector2(60, rect.height)), "全选");
            GUI.Button(new Rect(rect.x + 60, rect.y, 60, rect.height), "反选");
            GUI.Button(new Rect(rect.x + 120, rect.y, 60, rect.height), "全不选");
            GUI.Button(new Rect(rect.x + 180, rect.y, 60, rect.height), "合成");
            EditorGUI.EndDisabledGroup();
        }
    }
}