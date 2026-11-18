using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace SYArtToolkit
{
    public class TextureFindPrefabAddSpace : ToolInterface
    {
        public TextureFindPrefabAddSpace()
        {

        }



        DefaultAsset defaultAsset;

        private List<GameObject> setGameObject = new List<GameObject>();//
        float radio = 0f;

        private class ObjectList : ScriptableObject
        {
            public List<UnityEngine.Object> textures = new List<UnityEngine.Object>();
        }



        private ObjectList textureInput;//
        private SerializedObject serializedObject;
        private SerializedProperty serializedProperty;

        private Thread searchThread;



        Vector2 vector = new Vector2();


        public override void InterfaceShow(bool isbool, Rect rect)
        {


            if (textureInput == null || serializedProperty == null)
            {

                textureInput = ScriptableObject.CreateInstance<ObjectList>();
                serializedObject = new SerializedObject(textureInput);
                serializedProperty = serializedObject.FindProperty("textures");
            }

            EditorGUILayout.PropertyField(serializedProperty);

            serializedObject.ApplyModifiedProperties();

            defaultAsset = EditorGUILayout.ObjectField(defaultAsset, typeof(DefaultAsset), false) as DefaultAsset;

            if (defaultAsset == null)
            {
                defaultAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets");
            }


            if (searchThread != null && searchThread.IsAlive)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("正在搜素中...  :" + (int)(radio * 100) + "%");
                if (GUILayout.Button("中止"))
                {
                    searchThread.Abort();
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                if (GUILayout.Button("搜索"))
                {
                    InitFindGameObjects();
                    if (searchThread == null || !searchThread.IsAlive)
                    {
                        searchThread = new Thread(FindGameObjects);
                        searchThread.Start();
                    }
                }

            }

            vector = GUILayout.BeginScrollView(vector);

            try
            {

                foreach (GameObject go in setGameObject)
                {
                    EditorGUILayout.BeginHorizontal();


                    GUILayout.Label(go.name + ".prefab");
                    if (GUILayout.Button("定位"))
                    {
                        Selection.activeObject = go;
                        EditorGUIUtility.PingObject(go);
                    }
                    else if (GUILayout.Button("资源管理器中打开"))
                    {
                        OpenLocation(Application.dataPath + AssetDatabase.GetAssetPath(go).Substring(6));
                    }
                    EditorGUILayout.EndHorizontal();

                }

            }
            catch
            {

            }


            GUILayout.EndScrollView();
            if (GUILayout.Button(".meta文件末尾添加空格"))
            {

                AddSpace();
            }
            GUILayout.Space(10);
        }


        public override void Exit()
        {
            if (searchThread != null && searchThread.IsAlive)
            {
                searchThread.Abort();
            }
        }


        HashSet<string> hashOfAll = new HashSet<string>();//搜索guid
        HashSet<string> textures = new HashSet<string>();//纹理的guid
        HashSet<Shader> shaders = new HashSet<Shader>();//shader
        Dictionary<GameObject, string> dicObject = new Dictionary<GameObject, string>();//预制体-文本
        Dictionary<string, (string, Shader)> dicMaterial = new Dictionary<string, (string, Shader)>(); //材质球guid-(文本，shader)



        private void InitFindGameObjects()
        {
            setGameObject.Clear();
            hashOfAll.Clear();
            textures.Clear();
            shaders.Clear();
            dicObject.Clear();
            dicMaterial.Clear();

            foreach (var tex in textureInput.textures)
            {
                if (tex == null) continue;
                if (tex is Shader)
                {
                    if (!shaders.Contains(tex as Shader))
                    {
                        shaders.Add(tex as Shader);
                    }
                }
                else if (tex is Texture)
                {
                    string pathTemp = AssetDatabase.GetAssetPath(tex);
                    if (pathTemp != "")
                    {
                        string guid = AssetDatabase.AssetPathToGUID(pathTemp);
                        if (!textures.Contains(guid)) textures.Add(guid);
                    }
                }
                else
                {
                    string pathTemp = AssetDatabase.GetAssetPath(tex);
                    if (pathTemp != "")
                    {
                        string guid = AssetDatabase.AssetPathToGUID(pathTemp);
                        if (!hashOfAll.Contains(guid)) hashOfAll.Add(guid);
                    }
                }
            }


            string[] prefabFiles = Directory.GetFiles(AssetDatabase.GetAssetPath(defaultAsset), "*.prefab", SearchOption.AllDirectories);
            string[] metaFiles = Directory.GetFiles(AssetDatabase.GetAssetPath(defaultAsset), "*.mat", SearchOption.AllDirectories);
            if (shaders.Count > 0 || textures.Count > 0)
            {

                for (int i = 0; i < metaFiles.Length; ++i)

                {
                    EditorUtility.DisplayProgressBar("搜索Material " + (float)(i + 1) + "/" + metaFiles.Length, metaFiles[i], (float)i / metaFiles.Length);

                    string assetPath = metaFiles[i];
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                    string guid = AssetDatabase.AssetPathToGUID(assetPath);

                    if (mat != null)
                    {

                        if (!dicMaterial.ContainsKey(guid))
                        {
                            dicMaterial.Add(guid, (assetPath, mat.shader));
                        }
                    }


                    EditorUtility.ClearProgressBar();

                }


            }




            // 修改对应的 .meta 文件
            for (int i = 0; i < prefabFiles.Length; ++i)
            {

                EditorUtility.DisplayProgressBar("搜索Prefab " + (float)(i + 1) + "/" + prefabFiles.Length, prefabFiles[i], (float)i / prefabFiles.Length);
                string assetPath = prefabFiles[i];
                var obj = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                //string fileContents = File.ReadAllText(assetPath);

                if (obj != null)
                {
                    dicObject.Add(obj, assetPath);
                }

                EditorUtility.ClearProgressBar();
                //开始计时
                // 读取文件内容
                //string fileContents = File.ReadAllText(assetPath);
                //foreach (var guid in materials)
                //{
                //    if (fileContents.IndexOf(guid) > -1 && !prefabs.Contains(obj))
                //    {
                //        prefabs.Add(obj);
                //    }
                //}
                //结束计时
                // 检查文件内容是否包含指定字符串
            }


        }


        private void FindGameObjects()
        {

            HashSet<GameObject> prefabs = new HashSet<GameObject>();
            int index = 0;
            List<string> materialGuids = new List<string>();
            foreach (var kv in dicMaterial)
            {
                ++index;
                radio = index / (float)dicMaterial.Count;
                bool have = true;
                string fileContents = File.ReadAllText(kv.Value.Item1);
                foreach (var tex in textures)
                {
                    if (fileContents.Contains(tex))
                    {
                        materialGuids.Add(kv.Key);
                        have = false;
                        break;
                    }
                }
                if (have)
                {
                    foreach (var tex in shaders)
                    {
                        if (tex == kv.Value.Item2)
                        {

                            materialGuids.Add(kv.Key);
                            break;
                        }
                    }
                }

            }



            index = 0;
            foreach (var kv in dicObject)
            {
                ++index;
                radio = index / (float)dicObject.Count;
                string content = File.ReadAllText(kv.Value);
                foreach (var guid in hashOfAll)
                {
                    if (content.Contains(guid))
                    {
                        prefabs.Add(kv.Key);
                        setGameObject.Add(kv.Key);
                        break;
                    }
                }
                if (!prefabs.Contains(kv.Key))
                {
                    foreach (var guid in materialGuids)
                    {
                        if (content.Contains(guid))
                        {
                            prefabs.Add(kv.Key);
                            setGameObject.Add(kv.Key);
                            break;
                        }
                    }
                }

            }

            Submit.ArtToolkitData.Instance.AddAssetRef(setGameObject.Count);
            return;

        }



        private void AddSpace()
        {
            foreach (var obj in setGameObject)
            {
                string materialAssetPath = AssetDatabase.GetAssetPath(obj);
                string materialMetaFile = materialAssetPath + ".meta";
                if (File.Exists(materialMetaFile))
                {
                    File.AppendAllText(materialMetaFile, " "); // 添加一个空格
                }
            }
        }

        public void OpenLocation(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                // 使用Process打开资源管理器，并将文件路径作为参数传递
                Process process = new Process();
                process.StartInfo.FileName = GetFileExplorerExecutable();
                process.StartInfo.Arguments = "/select, " + filePath.Replace("/", "\\"); // 使用反斜杠来转义文件路径
                process.Start();
            }
            else
            {
                UnityEngine.Debug.Log("找不到路径");
            }
        }
        // 获取文件资源管理器可执行文件的路径
        private string GetFileExplorerExecutable()
        {
            string explorer = "explorer.exe"; // Windows上的资源管理器
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                explorer = "open"; // macOS上的Finder
            }
            return explorer;
        }
    }
}