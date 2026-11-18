
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SYArtToolkit
{
    public class MaterialCleanerWin : ToolInterface
    {
        public static readonly string ClearMatsKey = "ArtToolKit.ClearMaterial";
        private DefaultAsset assetConfig;

        private List<KeyValuePair<Material, List<string>>> dic;




        public void OnEnable()
        {
            assetConfig = AssetDatabase.LoadAssetAtPath<DefaultAsset>(EditorPrefs.GetString(ClearMatsKey));
            dic = new List<KeyValuePair<Material, List<string>>>();

        }


        public MaterialCleanerWin()
        {
            assetConfig = AssetDatabase.LoadAssetAtPath<DefaultAsset>(EditorPrefs.GetString(ClearMatsKey));
            dic = new List<KeyValuePair<Material, List<string>>>();
        }
        Vector2 v2;
        public async override void InterfaceShow(bool atThisWindow, Rect position)
        {

            EditorGUI.BeginChangeCheck();
            assetConfig = EditorGUILayout.ObjectField("材质目录", assetConfig, typeof(DefaultAsset), true) as DefaultAsset;
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString(ClearMatsKey, AssetDatabase.GetAssetPath(assetConfig));
            }

            GUILayout.Label("说明:");
            GUILayout.Label("1.点击选择材质目录或者拖动文件夹到选择框里。");

            EditorGUI.BeginDisabledGroup(assetConfig == null);

            if (GUILayout.Button("搜索"))
            {
                Debug.Log("清除材质无用贴图依赖");
                string[] searchInFolders = new string[] { AssetDatabase.GetAssetPath(assetConfig) };

                await MaterialCleaner.ClearMaterial(searchInFolders, dic);
            }
            EditorGUI.EndDisabledGroup();

            v2 = EditorGUILayout.BeginScrollView(v2);
            if (dic != null)
            {
                foreach (var kv in dic)
                {
                    GUILayout.Label(kv.Key.ToString() + string.Format("有{0}个无效依赖", kv.Value.Count));
                }
            }

            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("一键清除"))
            {
                MaterialCleaner.ClearMaterial(dic);
            }


            //return true;
        }



    }
}