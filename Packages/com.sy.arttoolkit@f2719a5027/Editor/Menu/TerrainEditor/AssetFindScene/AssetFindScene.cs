using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;


namespace SYArtToolkit
{

    public class AssetFindScene : ToolInterface
    {
        //

        private DefaultAsset defaultAsset;
        private Object asset;
        private Task mainTask;
        private readonly string folderPath = "SYArtToolkitAssetFindScene";
        private float schedule;
        private bool stop;
        public AssetFindScene()
        {
            schedule = 0.0f;
            string path = EditorPrefs.GetString(folderPath);
            if (!string.IsNullOrEmpty(path))
                defaultAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
            result = new List<string>();
            mainTask = null;
            stop = false;
        }

        private List<string> result;


        Vector2 v2;


        private void SingleAssetAtScenes(string assetGuid, List<string> paths, List<string> resultGuids)
        {
            Parallel.ForEach(paths, path =>
            {
                if (resultGuids == null || stop) return;

                if (File.Exists(path))
                {
                    var texts = File.ReadAllText(path);
                    if (texts.Contains(assetGuid) && !resultGuids.Contains(path))
                    {
                        resultGuids.Add(path);
                    }
                    schedule += 1.0f / paths.Count;
                }


            });
        }

        private void TextureFindScene(string assetGuid, List<string> scenePaths, Dictionary<string, string> matGuidPaths, List<string> resulePaths)
        {


            //Dictionary<string, string> scenePathTexts = new Dictionary<string, string>();
            List<(string, string)> scenePathTexts = new List<(string, string)>(scenePaths.Count);
            //Parallel.For(,)

            Parallel.ForEach(scenePaths, scenePath =>
            {
                if (resulePaths == null || stop) return;
                if (File.Exists(scenePath))
                {
                    scenePathTexts.Add((scenePath, File.ReadAllText(scenePath)));
                }
                schedule += 1.0f / scenePaths.Count;
            });
            schedule = 0.0f;

            Parallel.ForEach(matGuidPaths, matGuidPath =>
            {
                if (resulePaths == null || stop) return;
                if (File.Exists(matGuidPath.Value))
                {
                    if (TextureAtMaterial(assetGuid, File.ReadAllText(matGuidPath.Value)))
                    {
                        AssetsAtScene(matGuidPath.Key, scenePathTexts, result);
                    }
                }
                schedule += 1 / matGuidPaths.Count;
            });
        }



        private void AssetsAtScene(string assetGuid, List<(string, string)> scenePathTexts, List<string> resultGuids)
        {
            Parallel.ForEach(scenePathTexts, guidText =>
            {
                if (resultGuids == null || stop) return;
                AssetAtScene(assetGuid, guidText.Item2, guidText.Item1, resultGuids);

            });
        }

        //private void 


        //材质球是否包含了贴图
        private bool TextureAtMaterial(string assetGuid, string text)
        {
            return text.Contains(assetGuid);
        }


        //场景是否包含了资源
        private void AssetAtScene(string assetGuid, string text, string sceneGuid, List<string> sceneAssets)
        {

            if (sceneAssets.Contains(sceneGuid)) return;
            if (text.Contains(assetGuid)) sceneAssets.Add(sceneGuid);

        }

        public override void Disable()
        {
            if (mainTask != null && !mainTask.IsCompleted) stop = true;
        }

        public override void InterfaceShow(bool atThisWindow, Rect position)
        {
            asset = EditorGUILayout.ObjectField(asset, typeof(Object), false);
            if (asset != null && (asset is Shader || asset is Texture))
            {
                EditorGUI.BeginChangeCheck();
                defaultAsset = EditorGUILayout.ObjectField(defaultAsset, typeof(DefaultAsset), false) as DefaultAsset;
                if (EditorGUI.EndChangeCheck())
                {
                    if (defaultAsset != null)
                    {
                        string path = AssetDatabase.GetAssetPath(asset);
                        if (Directory.Exists(path)) EditorPrefs.SetString(folderPath, path);
                    }
                }
            }
            EditorGUILayout.Space();
            v2 = EditorGUILayout.BeginScrollView(v2);
            foreach (var guid in result)
            {
                EditorGUILayout.ObjectField(AssetDatabase.LoadAssetAtPath<SceneAsset>(guid), typeof(SceneAsset), false);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();

            if (mainTask != null && !mainTask.IsCompleted)
            {
                GUILayout.Label(string.Format("正在查找中：{0}%", (schedule * 100).ToString("F2")));
            }
            else
            {
                EditorGUI.BeginDisabledGroup(asset == null || ((asset is Shader || asset is Texture) && defaultAsset == null));
                if (GUILayout.Button("开始查找"))
                {
                    schedule = 0.0f;
                    result.Clear();
                    //Stopwatch sw = Stopwatch.StartNew();
                    //sw.Start();
                    var assetGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));

                    var sceneGuids = AssetDatabase.FindAssets("t:Scene");
                    //Dictionary<string, string> guidTexts = new Dictionary<string, string>();
                    List<string> scenePaths = new List<string>();
                    //UnityEngine.Debug.Log(sceneGuids.Length);

                    foreach (var scene in sceneGuids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(scene);
                        scenePaths.Add(path);
                    }
                    if (asset is Shader || asset is Texture)
                    {
                        var matGuids = AssetDatabase.FindAssets("t:material", new string[] { AssetDatabase.GetAssetPath(defaultAsset) });
                        Dictionary<string, string> matGuidPaths = new Dictionary<string, string>();
                        foreach (var matGuid in matGuids)
                        {
                            matGuidPaths.Add(matGuid, AssetDatabase.GUIDToAssetPath(matGuid));
                        }
                        mainTask = new Task(() => TextureFindScene(assetGuid, scenePaths, matGuidPaths, result)
                    );

                    }
                    else
                    {

                        mainTask = new Task(() =>
                        {
                            SingleAssetAtScenes(assetGuid, scenePaths, result);
                        });
                    }
                    mainTask.Start();
                }
                EditorGUI.EndDisabledGroup();

            }



        }
    }
}