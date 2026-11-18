using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SYArtToolkit
{
    public class MergeMeshAtSameMaterial : ToolInterface
    {

        private Materials inputMaterials;
        private SerializedObject inputMaterialsObject;
        private SerializedProperty inputMaterialsProperty;
        private Vector2 scroll;
        private List<SingleMeshMerge> meshesMerge;
        private ReorderableList SetSizeList;
        public static DefaultAsset defaultAsset;



        public MergeMeshAtSameMaterial()
        {
            inputMaterials = ScriptableObject.CreateInstance<Materials>();
            inputMaterialsObject = new SerializedObject(inputMaterials);
            inputMaterialsProperty = inputMaterialsObject.FindProperty("materials");
            meshesMerge = new List<SingleMeshMerge>();
            SetSizeList = new ReorderableList(meshesMerge, typeof(SingleMeshMerge), false, true, false, false);
            SetSizeList.drawElementCallback = DrawElementCallback;
            SetSizeList.drawFooterCallback = DrawFooterCallback;
            SetSizeList.elementHeightCallback = ElementHeightCallback;
            SetSizeList.drawHeaderCallback = (Rect rect) => { GUI.Label(rect, "查找到以下物体"); };
        }

        public override void InterfaceShow(bool noBool, Rect noRect)
        {
            if (inputMaterials == null)
            {
                inputMaterials = ScriptableObject.CreateInstance<Materials>();
                inputMaterialsObject = new SerializedObject(inputMaterials);
                inputMaterialsProperty = inputMaterialsObject.FindProperty("materials");
            }
            EditorGUILayout.PropertyField(inputMaterialsProperty, new GUIContent("输入材质"));
            inputMaterialsObject.ApplyModifiedProperties();
            GUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(inputMaterials.materials.Count == 0);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("一键搜索", "只合并挂载MeshRenderer的物体")))
            {
                meshesMerge.Clear();
                var objs = GameObject.FindObjectsOfType(typeof(GameObject));

                var set = new HashSet<Material>();
                foreach (var mat in inputMaterials.materials)
                {
                    if (mat == null || set.Contains(mat)) continue;
                    set.Add(mat);
                }

                var dic = new Dictionary<Material, List<GameObject>>();
                foreach (var mat in set)
                {
                    dic.Add(mat, new List<GameObject>());
                }

                foreach (var o in objs)
                {

                    var gameobj = o as GameObject;
                    if (gameobj == null || gameobj.GetComponent<MeshFilter>() == null) continue;
                    var renderer = gameobj.GetComponent<MeshRenderer>();
                    if (renderer == null) continue;
                    var mat = renderer.sharedMaterial;
                    if (mat != null && set.Contains(mat))
                    {
                        dic[mat].Add(gameobj);
                    }
                }

                foreach (var mat in inputMaterials.materials)
                {
                    if (set.Contains(mat))
                    {
                        meshesMerge.Add(new SingleMeshMerge(mat, dic[mat]));
                        set.Remove(mat);
                    }
                }
            }


            EditorGUI.BeginDisabledGroup(meshesMerge.Count == 0 || defaultAsset == null);
            if (GUILayout.Button("一键合成"))
            {
                List<GameObject> all = new List<GameObject>();
                foreach (var variaSingleMeshMerge in meshesMerge)
                {
                    variaSingleMeshMerge.MergeMesh(defaultAsset);
                }

                Submit.ArtToolkitData.Instance.AddMeshCombine(meshesMerge.Count);

            }

            EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            defaultAsset =
                EditorGUILayout.ObjectField("资源导出路径", defaultAsset, typeof(DefaultAsset), true) as
                    DefaultAsset;

            GUILayout.Space(10);

            scroll = GUILayout.BeginScrollView(scroll);

            SetSizeList.DoLayoutList();
            GUILayout.EndScrollView();

            EditorGUI.EndDisabledGroup();


        }

        #region 回调


        void DrawElementCallback(Rect rect, int index, bool selected, bool focused)
        {
            GUI.Label(new Rect(rect.x, rect.y, 20, rect.height), "--");
            meshesMerge[index].Show(new Rect(rect.x + 20, rect.y, rect.width - 30, rect.height));
        }

        private float ElementHeightCallback(int index)
        {
            return meshesMerge[index].Height() + 10;
        }

        private void DrawFooterCallback(Rect rect)
        {

        }

        public class Materials : ScriptableObject
        {
            public List<Material> materials;

            public Materials()
            {
                materials = new List<Material>();
            }
        }

        #endregion

    }

}