
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;



namespace SYArtToolkit
{



    public class MeshTexBaker : ToolInterface
    {
        private enum ReName
        {
            Custom = 0,
            Prefix = 1,
            Suffix = 2,
        }



        ReorderableList mapPreSetSizeList;
        public List<MapBakePreSet> MapBakePreSetLst = new List<MapBakePreSet>();
        private Vector2 scrollPosition; // 滚动条位置
        public GameObject[] selectedSceneObjs;
        private MeshTexBakerCore meshTexBaker;
        private int atlasSize;
        private int padding;
        private DefaultAsset defaultAssetMesh;
        private DefaultAsset defaultAssetTexture;
        private DefaultAsset defaultAssetMaterial;

        static private ReName reName;
        static private string reNameString;

        public static string GetReName(string name, int index = 0)
        {
            string result = null;
            switch (reName)
            {
                case ReName.Custom:
                    result = reNameString + index.ToString("D2"); break;
                case ReName.Prefix:
                    result = reNameString + name; break;
                case ReName.Suffix:
                    result = name + reNameString; break;
                default: result = name; break;
            }
            return result;
        }

        public MeshTexBaker()
        {

            reName = ReName.Prefix;
            reNameString = "CM_";
            var defaultAssetPathMesh = EditorPrefs.GetString("ArttoolkitMeshTexBakerMesh");
            var defaultAssetPathMaterial = EditorPrefs.GetString("ArttoolkitMeshTexBakerMaterial");
            var defaultAssetPathTexture = EditorPrefs.GetString("ArttoolkitMeshTexBakerTexture");

            if (!string.IsNullOrEmpty(defaultAssetPathMesh))
            {
                defaultAssetMesh = AssetDatabase.LoadAssetAtPath<DefaultAsset>(defaultAssetPathMesh);
            }
            if (!string.IsNullOrEmpty(defaultAssetPathMaterial))
            {
                defaultAssetMaterial = AssetDatabase.LoadAssetAtPath<DefaultAsset>(defaultAssetPathMaterial);
            }
            if (!string.IsNullOrEmpty(defaultAssetPathTexture))
            {
                defaultAssetTexture = AssetDatabase.LoadAssetAtPath<DefaultAsset>(defaultAssetPathTexture);
            }

            mapPreSetSizeList = new ReorderableList(MapBakePreSetLst, typeof(MapBakePreSet));
            mapPreSetSizeList.drawElementCallback = DrawElementCallback;
            mapPreSetSizeList.drawHeaderCallback = DrawHeaderCallback;
            mapPreSetSizeList.onAddCallback = AddCallback;
            mapPreSetSizeList.elementHeight = 50;
            meshTexBaker = new MeshTexBakerCore();
            padding = 2;

            EmptyPreSetSizeList = new ReorderableList(EmptyMapBakePreSetLst, typeof(MapBakePreSet));
            EmptyPreSetSizeList.drawHeaderCallback = DrawHeaderCallback;
            EmptyPreSetSizeList.displayAdd = false;
            EmptyPreSetSizeList.displayRemove = false;
            EmptyPreSetSizeList.elementHeight = 0;
        }

        private bool fold = true;
        ReorderableList EmptyPreSetSizeList;
        public List<MapBakePreSet> EmptyMapBakePreSetLst = new List<MapBakePreSet>();


        public override void InterfaceShow(bool atThisWindow, Rect position)
        {

            EditorGUILayout.BeginVertical();
            if (GUILayout.Button("选择场景物体"))
            {
                CollectSelectedObj();
            }

            EditorGUI.BeginDisabledGroup(selectedSceneObjs == null || selectedSceneObjs.Length == 0);
            if (GUILayout.Button("合并贴图并网格烘焙"))
            {
                if (!CheckAtlasSize())
                {
                    EditorUtility.DisplayDialog("提示", "存在单图尺寸设置大于图集尺寸,无法生成图集", "关闭");
                    return;
                }

                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(defaultAssetMesh)))
                {
                    EditorUtility.DisplayDialog("提示", "未选择合批资源导出路径", "关闭");
                    return;
                }
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(defaultAssetTexture)))
                {
                    EditorUtility.DisplayDialog("提示", "未选择合批资源导出路径", "关闭");
                    return;
                }
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(defaultAssetMaterial)))
                {
                    EditorUtility.DisplayDialog("提示", "未选择合批资源导出路径", "关闭");
                    return;
                }
                SYArtToolkit.Submit.ArtToolkitData.Instance.AddNewToolData("网格烘焙工具", 1, "v1.3");

                ExecuteBacker();
                foreach (var obj in selectedSceneObjs)
                {
                    obj.SetActive(false);
                }
            }

            if (GUILayout.Button("清除已选择物体"))
            {
                selectedSceneObjs = null;
                MapBakePreSetLst.Clear();
                meshTexBaker.ClearCache();
            }

            defaultAssetMesh = EditorGUILayout.ObjectField("网格导出路径", defaultAssetMesh, typeof(DefaultAsset), true) as DefaultAsset;
            if (defaultAssetMesh != null)
            {
                EditorPrefs.SetString("ArttoolkitMeshTexBakerMesh", AssetDatabase.GetAssetPath(defaultAssetMesh));
            }
            defaultAssetMaterial = EditorGUILayout.ObjectField("材质导出路径", defaultAssetMaterial, typeof(DefaultAsset), true) as DefaultAsset;
            if (defaultAssetMaterial != null)
            {
                EditorPrefs.SetString("ArttoolkitMeshTexBakerMaterial", AssetDatabase.GetAssetPath(defaultAssetMaterial));
            }
            defaultAssetTexture = EditorGUILayout.ObjectField("图片导出路径", defaultAssetTexture, typeof(DefaultAsset), true) as DefaultAsset;
            if (defaultAssetTexture != null)
            {
                EditorPrefs.SetString("ArttoolkitMeshTexBakerTexture", AssetDatabase.GetAssetPath(defaultAssetTexture));
            }

            atlasSize = EditorGUILayout.IntField("合图尺寸:", atlasSize, GUILayout.Width(200f));
            padding = EditorGUILayout.IntField("合图间隔像素:", padding, GUILayout.Width(200f));
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            reName = (ReName)EditorGUILayout.EnumPopup(reName);
            if (EditorGUI.EndChangeCheck())
            {
                reNameString = "";
            }
            reNameString = EditorGUILayout.TextField(reNameString);


            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            if (fold)
            {
                mapPreSetSizeList.DoLayoutList();
            }
            else
            {
                EmptyPreSetSizeList.DoLayoutList();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndScrollView();
            EditorGUI.EndDisabledGroup();

        }

        #region ReorderableList操作回调

        void DrawHeaderCallback(Rect rect)
        {
            fold = EditorGUI.Foldout(rect, fold, "场景贴图列表");
        }

        void DrawElementCallback(Rect rect, int index, bool selected, bool focused)
        {
            if (MapBakePreSetLst.Count < 0)
                return;
            MapBakePreSetLst[index].texture = (Texture2D)EditorGUI.ObjectField(new Rect(rect.x, rect.y, 40, 40),
                MapBakePreSetLst[index].texture, typeof(Texture2D), false);
            EditorGUI.LabelField(new Rect(rect.x + 50, rect.y, 30, 20), "Size:");
            MapBakePreSetLst[index].size =
                EditorGUI.IntField(new Rect(rect.x + 90, rect.y, 40, 20), MapBakePreSetLst[index].size);
        }

        private void AddCallback(ReorderableList list)
        {
            if (EditorUtility.DisplayDialog("提示", "无法直接添加贴图,请重新选择场景物体", "关闭"))
            {
                return;
            }
        }

        #endregion

        void CollectSelectedObj()
        {
            MapBakePreSetLst.Clear();
            selectedSceneObjs = Selection.gameObjects;

            meshTexBaker.CollectMaps(selectedSceneObjs, ref MapBakePreSetLst);
            if (MapBakePreSetLst != null && MapBakePreSetLst.Count != 0)
                atlasSize = 1024;
        }

        void ExecuteBacker()
        {
            string path = AssetDatabase.GetAssetPath(defaultAssetMesh);
            var pathMateiral = AssetDatabase.GetAssetPath(defaultAssetMaterial);
            var pathTexture = AssetDatabase.GetAssetPath(defaultAssetTexture);
            meshTexBaker.Execute(atlasSize, padding, path, pathMateiral, pathTexture);

        }

        bool CheckAtlasSize()
        {
            foreach (var bakePreSet in MapBakePreSetLst)
            {
                if (bakePreSet.size > atlasSize)
                    return false;
            }

            return true;
        }

    }
}
