using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SYArtToolkit
{
    public class AssetRefSearchWin : ToolInterface
    {

        private static Object checkAsset;

        private static Object searchRootPath;
        public static readonly string m_AssetPathKey = "m_Asset";
        public static readonly string m_SearchPathKey = "m_SearchRootPath";

        private static string[] searchPatterns = new string[]
        {
            "t:Texture", /*"t:Mesh",*/ /*"t:AudioClip",*/ "t:AnimationClip", /*"t:Model ",*/ /*"t:Prefab",*/
            "t:Material", "t:Shader" /*, "t:Font"*/
        };

        private List<string> checkAssetGUIDs = new List<string>();
        private Dictionary<string, AssetRefData> allAssetRefData = new Dictionary<string, AssetRefData>();
        private List<AssetRefData> allAssetRefSortShowLst = new List<AssetRefData>();
        private string[] searchRootFolders = new string[1];
        Vector2 scrollViewPosProject;
        private bool searchMatType;
        private bool searchPrefabType;

        private static class Styles
        {
            public static readonly GUIStyle guiStyle = new GUIStyle();

            static Styles()
            {
                guiStyle.normal.textColor = Color.green;
            }
        }

        public AssetRefSearchWin()
        {
            checkAsset = CheckConfig(m_AssetPathKey);
            if (checkAsset == null)
            {
                SetConfig(m_AssetPathKey, "Assets");
                checkAsset = CheckConfig(m_AssetPathKey);
            }

            searchRootPath = CheckConfig(m_SearchPathKey);
            SetConfig(m_SearchPathKey, "Assets");
        }

        private string[] toolbar = { "目录检索", "指定检索" };
        private int selectToolbar = 0;

        public override void InterfaceShow(bool noBool, Rect noRect)
        {
            selectToolbar = GUILayout.Toolbar(selectToolbar, toolbar);
            GUILayout.BeginHorizontal();
            if (selectToolbar == 0)
            {
                GUILayout.Label("目录:", GUILayout.Width(90f));
                checkAsset = EditorGUILayout.ObjectField("", checkAsset, typeof(object), true, GUILayout.Width(320f));
            }
            else if (selectToolbar == 1)
            {
                GUILayout.Label("资源:", GUILayout.Width(90f));
                checkAsset = EditorGUILayout.ObjectField("", checkAsset, typeof(object), true, GUILayout.Width(320f));
            }

            //1111
            if (checkAsset != null)
            {
                SetConfig(m_AssetPathKey, AssetDatabase.GetAssetPath(checkAsset));
            }

            if (checkAsset == null && CheckConfig(m_AssetPathKey) != null)
            {
                checkAsset = CheckConfig(m_AssetPathKey);
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("搜索根目录:", GUILayout.Width(90f));
            searchRootPath =
                EditorGUILayout.ObjectField("", searchRootPath, typeof(object), true, GUILayout.Width(320f));
            if (searchRootPath != null)
            {
                SetConfig(m_SearchPathKey, AssetDatabase.GetAssetPath(searchRootPath));
            }

            if (searchRootPath == null && CheckConfig(m_SearchPathKey) != null)
            {
                SetConfig(m_SearchPathKey, "Assets");
                searchRootPath = CheckConfig(m_SearchPathKey);
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("搜索资源类型:", GUILayout.Width(90f));
            searchPrefabType = GUILayout.Toggle(searchPrefabType, "Prefab", GUILayout.Width(90f));
            searchMatType = GUILayout.Toggle(searchMatType, "Material", GUILayout.Width(90f));
            if (GUILayout.Button("升序"))
            {
                allAssetRefSortShowLst.Sort(delegate (AssetRefData a, AssetRefData b)
                {
                    return a.refAssetPath.Count.CompareTo(b.refAssetPath.Count);
                });
            }

            if (GUILayout.Button("降序"))
            {
                allAssetRefSortShowLst.Sort(delegate (AssetRefData a, AssetRefData b)
                {
                    return b.refAssetPath.Count.CompareTo(a.refAssetPath.Count);
                });
            }
            if (GUILayout.Button("选择0引用"))
            {
                List<Object> selects = new List<Object>();

                foreach (var a in allAssetRefSortShowLst)
                {
                    if (!a.hasRef)
                    {
                        selects.Add(AssetDatabase.LoadAssetAtPath<Object>(a.path));
                    }
                }
                Selection.objects = selects.ToArray();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("检索引用"))
            {
                allAssetRefData.Clear();
                string[] assetInFolders = new string[] { GetConfig(m_AssetPathKey) };
                searchRootFolders = new string[] { GetConfig(m_SearchPathKey) };
                SearchAsset(assetInFolders);
                CheckResRef();
            }

            if (GUILayout.Button("加载缓存"))
            {
                LoadRefCache();
            }

            if (GUILayout.Button("Clear"))
            {
                allAssetRefData.Clear();
                allAssetRefSortShowLst.Clear();
            }

            GUILayout.EndHorizontal();
            int assetSearchCount = allAssetRefSortShowLst.Count;
            if (assetSearchCount > 0)
            {
                GUILayout.Label("AssetsRef found:");
                scrollViewPosProject = EditorGUILayout.BeginScrollView(scrollViewPosProject);
                for (int i = 0; i < allAssetRefSortShowLst.Count; i++)
                {
                    var assetRefData = allAssetRefSortShowLst[i];
                    string path = assetRefData.path;
                    assetRefData.foldout = EditorGUILayout.Foldout(assetRefData.foldout,
                        new GUIContent(path + " ( has " + assetRefData.refAssetPath.Count + " Asset Ref)"));
                    if (assetRefData.foldout)
                    {
                        for (int j = 0; j < assetRefData.refAssetPath.Count; j++)
                        {
                            //有引用关系的资源
                            string refAssetPath = assetRefData.refAssetPath[j];
                            EditorGUILayout.BeginHorizontal();
                            if (assetRefData.foldout)
                            {
                                EditorGUILayout.LabelField((j + 1) + ".---" + refAssetPath, Styles.guiStyle);
                                if (GUILayout.Button("定位"))
                                {
                                    Object obj = AssetDatabase.LoadMainAssetAtPath(refAssetPath);
                                    Selection.activeObject = obj;
                                    EditorGUIUtility.PingObject(obj);
                                }
                            }

                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }



        private void SearchAsset(string[] assetFolders)
        {
            checkAssetGUIDs.Clear();
            allAssetRefData.Clear();
            allAssetRefSortShowLst.Clear();
            if (string.IsNullOrEmpty(searchRootFolders[0]))
            {
                searchRootFolders[0] = "Assets";
            }

            if (!Directory.Exists(searchRootFolders[0]))
            {
                Debug.Log("搜索目录不为文件夹");
                return;
            }

            if (Directory.Exists(assetFolders[0]))
            {
                Debug.Log("检测方式为目录检测");
                foreach (string filter in searchPatterns)
                {
                    string[] guids = AssetDatabase.FindAssets(filter, assetFolders);
                    checkAssetGUIDs.AddRange(guids);
                }

                foreach (var guid in checkAssetGUIDs)
                {
                    if (allAssetRefData.ContainsKey(guid)) continue;
                    AssetRefData assetRefData = new AssetRefData();
                    assetRefData.refAssetPath = new List<string>();
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    assetRefData.guid = guid;
                    assetRefData.path = path;
                    allAssetRefData.Add(guid, assetRefData);
                }
            }
            else if (File.Exists(assetFolders[0]))
            {
                Debug.Log("检测方式为文件检测");
                string guid = AssetDatabase.AssetPathToGUID(assetFolders[0]);
                checkAssetGUIDs.Add(guid);
                AssetRefData assetRefData = new AssetRefData();
                assetRefData.refAssetPath = new List<string>();
                assetRefData.guid = guid;
                assetRefData.path = assetFolders[0];
                allAssetRefData.Add(guid, assetRefData);
            }

            Debug.Log("搜索根目录:" + searchRootFolders[0]);
        }

        private void CheckResRef()
        {
            //string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
            string path;
            List<string> assetTypeFilter = new List<string>();
            if (searchMatType)
                assetTypeFilter.Add("t:Material");
            if (searchPrefabType)
                assetTypeFilter.Add("t:Prefab");

            List<string> allAssetPaths = new List<string>();
            foreach (string filter in assetTypeFilter)
            {
                string[] guids = AssetDatabase.FindAssets(filter, searchRootFolders);
                foreach (var guid in guids)
                {
                    path = AssetDatabase.GUIDToAssetPath(guid);
                    allAssetPaths.Add(path);
                }
            }

            for (int i = 0; i < allAssetPaths.Count; i++)
            {
                string allAssetPath = allAssetPaths[i];
                EditorUtility.DisplayProgressBar("Progress Bar " + (float)(i + 1) + "/" + allAssetPaths.Count,
                    allAssetPath, (float)i / allAssetPaths.Count);
                if (allAssetPath.EndsWith(".prefab") || allAssetPath.EndsWith(".mat"))
                {
                    string context = File.ReadAllText(allAssetPath);
                    if (context == null)
                    {
                        continue;
                    }

                    foreach (var guid in checkAssetGUIDs)
                    {
                        if (context.IndexOf(guid) > 0)
                        {
                            allAssetRefData[guid].refAssetPath.Add(allAssetPath);
                        }
                    }
                }
            }

            EditorUtility.ClearProgressBar();
            foreach (var assetRefData in allAssetRefData.Values)
            {
                allAssetRefSortShowLst.Add(assetRefData);
            }

            Submit.ArtToolkitData.Instance.AddAssetRef(allAssetRefSortShowLst.Count);
            SaveRefCache();
        }


        static Object CheckConfig(string key)
        {
            string value = EditorPrefs.GetString(key);
            if (!string.IsNullOrEmpty(value))
            {
                var checkAsset = AssetDatabase.LoadAssetAtPath<Object>(value);
                if (checkAsset != null)
                {
                    return checkAsset;
                }
            }

            return null;
        }

        void SaveRefCache()
        {
            AssetRefCache assetRefCache = GetScriptableObjectDataArtToolBox.GetAssetRefCache();
            assetRefCache.assetRefInfos = new List<AssetRefData>(); ;
            assetRefCache.assetRefInfos.AddRange(allAssetRefSortShowLst);
        }

        void LoadRefCache()
        {
            AssetRefCache assetRefCache = GetScriptableObjectDataArtToolBox.GetAssetRefCache();
            allAssetRefSortShowLst.AddRange(assetRefCache.assetRefInfos);
        }


        static void SetConfig(string key, string value)
        {
            EditorPrefs.SetString(key, value);
        }

        static string GetConfig(string key)
        {
            return EditorPrefs.GetString(key);
        }
    }

}
