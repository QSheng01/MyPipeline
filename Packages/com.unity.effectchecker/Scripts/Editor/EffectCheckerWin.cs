#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace SY.EffectChecker
{

    public class EffectCheckerWin : EditorWindow
    {
        private string exportCSVPath;
        private string exportCSVPathTemp;
        static private EffectPreChecker effectPreChecker;
        public static readonly string m_ParPathKey = "m_ParticleAsset";
        private static string[] searchPattern = new string[] { "t:Prefab _" };
        private static Object checkAsset;
        private DefaultAsset excelInput;
        private int startIndex = 1;
        private int endIndex = 1;
        private static float speedRatio=1;

        string m_app_id_text_field = "4";
        //private string python_path_text = Application.dataPath+"/SYTools/EffectChecker/developer_local_handler_win/win/handler.exe";
        string m_remark = "特效检测";
        List<string> effectCheckConfigIDs;
        private bool debugToggleValue = true;

        static string effect_path = "Assets/Things/Effect/Prefab";

        string versionLablel = "";
        static string effectLabel = "检测特效目录:";
         bool isImportCheckConfig = false;

        //private ReadExcel readExcel;

        [MenuItem("特效常用/EffectChecker - 特效测试工具", priority = 101)]
        [MenuItem("工具/EffectChecker - 特效测试工具")]
        public static void ShowWindow()
        {
            var window = CreateWindow<EffectCheckerWin>();
            window.minSize = new Vector2(500, 300);
            window.Show();

            window.Focus();
            Debug.Log("打开特效测试工具");


            SetConfig(m_ParPathKey, effect_path);
            checkAsset = CheckConfig(m_ParPathKey);

            speedRatio = 1;
            EditorPrefs.SetInt("speedRatio", 1);
            // 清空切换场景标志
            SetConfig(ExportDataConfig.sceneKey, "");
        }

        private bool first;

        public void Awake()
        {


            //python_path_text = Application.dataPath + "/SYTools/EffectChecker/developer_local_handler_win/win/handler.exe";
            EditorPrefs.SetInt(ExportDataConfig.appIdKey, Int32.Parse(m_app_id_text_field));

            exportCSVPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6) + "Output";
            Debug.Log(exportCSVPath);
            int appIdPath = EditorPrefs.GetInt(ExportDataConfig.appIdKey);
            if (appIdPath != 0 & appIdPath > 0)
            {
                m_app_id_text_field = appIdPath.ToString();
            }
            versionLablel = string.Format("特效检测工具 v{0}", ExportDataConfig.version);
            effectCheckConfigIDs = new List<string>();
            isImportCheckConfig = false;
            titleContent = new GUIContent(versionLablel);

            first = true;

            excelInput = AssetDatabase.LoadAssetAtPath<DefaultAsset>(EditorPrefs.GetString("EffectCheckerInput"));

            string[] searchInFolders = new string[] { GetConfig(m_ParPathKey) };
            startIndex = 1;
            if (effectCheckConfigIDs != null || effectCheckConfigIDs.Count != 0)
            {
                endIndex = GetPrefabCount(searchInFolders);
                if (endIndex == 0)
                {
                    endIndex = 1;
                }
            }

        }

        [SerializeField]
        public void OnGUI()
        {
            effectPreChecker = EffectPreChecker.Instance;
            GUILayout.BeginHorizontal();
            GUILayout.Label(effectLabel, GUILayout.Width(90f));
            EditorGUI.BeginChangeCheck();
            checkAsset = EditorGUILayout.ObjectField("", checkAsset, typeof(object), true, GUILayout.Width(320f)) as Object;
            if (checkAsset != null)
            {
                SetConfig(m_ParPathKey, AssetDatabase.GetAssetPath(checkAsset));
            }
            if (checkAsset == null && CheckConfig(m_ParPathKey) != null)
            {
                checkAsset = CheckConfig(m_ParPathKey);
            }
            if (EditorGUI.EndChangeCheck())
            {
                string[] searchInFolders = new string[] { GetConfig(m_ParPathKey) };
                startIndex = 1;
                if (effectCheckConfigIDs != null || effectCheckConfigIDs.Count != 0)
                {
                    endIndex = GetPrefabCount(searchInFolders);
                    if (endIndex == 0)
                    {
                        endIndex = 1;
                    }
                }
            }

            if (speedRatio <= 0)
            {
                speedRatio = 1;
                EditorPrefs.SetInt("speedRatio", 1);
            }

            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();

            GUILayout.Label("特效检测文档", GUILayout.Width(90f));
            EditorGUI.BeginChangeCheck();
            excelInput = EditorGUILayout.ObjectField(excelInput, typeof(DefaultAsset), false, GUILayout.Width(320f)) as DefaultAsset;
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString("EffectCheckerInput", AssetDatabase.GetAssetPath(excelInput));
            }


            GUILayout.EndHorizontal();
            //if (EditorGUI.EndChangeCheck())
            //{
            //    EditorPrefs.SetInt("EffectCheckerExcelPage", readExcel.page);
            //    EditorPrefs.SetBool("EffectCheckerExcelRank", readExcel.rank == ReadExcel.Rank.row);
            //    EditorPrefs.SetInt("EffectCheckerExcelID", readExcel.idNumber);
            //    EditorPrefs.SetInt("EffectCheckerExcelKind", readExcel.kindNumber);
            //    EditorPrefs.SetInt("EffectCheckerExcelStartIndex", readExcel.startIndex);
            //    EditorPrefs.SetInt("EffectCheckerExcelEndIndex", readExcel.endIndex);
            //}



            if (1 + 1 == 2)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(string.Format("当前检测播放速率:{0}", speedRatio), GUILayout.Width(150f));
                if (GUILayout.Button("1倍速", GUILayout.Width(80f)))
                {
                    EditorPrefs.SetInt("speedRatio", 1);
                }

                if (GUILayout.Button("2倍速", GUILayout.Width(80f)))
                {
                    EditorPrefs.SetInt("speedRatio", 2);
                }

                if (GUILayout.Button("3倍速", GUILayout.Width(80f)))
                {
                    EditorPrefs.SetInt("speedRatio", 3);
                }

                speedRatio = EditorPrefs.GetInt("speedRatio");
                effectPreChecker.playingSpeedRatio = speedRatio;
                GUILayout.EndHorizontal();



                GUILayout.BeginHorizontal();
                if (effectCheckConfigIDs == null || effectCheckConfigIDs.Count == 0)
                {
                    GUILayout.Label("目录检测范围索引:", GUILayout.Width(150f));
                    startIndex = EditorGUILayout.IntField(startIndex, GUILayout.Width(80f));
                    GUILayout.Label("-", GUILayout.Width(20f));
                    endIndex = EditorGUILayout.IntField(endIndex, GUILayout.Width(80f));
                }
                else
                {
                    GUILayout.Label("当前配置检测特效数：" + effectCheckConfigIDs.Count);
                }

                GUILayout.EndHorizontal();

            }

            if (2 + 2 == 4)
            {

                GUILayout.BeginHorizontal();
                GUILayout.Label("文档导出路径:", GUILayout.Width(90f));
                exportCSVPath = EditorGUILayout.TextField(exportCSVPath, GUILayout.Width(320f));
                if (GUILayout.Button("选择目录", GUILayout.Width(80f)))
                {
                    exportCSVPathTemp = EditorUtility.OpenFolderPanel("Load Resources of Directory", "", "");
                    if (exportCSVPathTemp != "" && exportCSVPathTemp != exportCSVPath)
                    {
                        exportCSVPath = exportCSVPathTemp;
                    }

                    if (exportCSVPath.Length != 0 || exportCSVPath == null)
                    {
                        exportCSVPath = GetAssetsPath(exportCSVPath);
                    }

                    effectPreChecker.exportCSVPath = exportCSVPath;
                }

                GUILayout.EndHorizontal();
                if (!string.IsNullOrEmpty(exportCSVPath))
                {
                    effectPreChecker.exportCSVPath = exportCSVPath;
                }



            }


            GUILayout.BeginHorizontal();
            if (GUILayout.Button("跳转到测试场景"))
            {
                Scene nowScene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.SaveScene(nowScene);
                EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath("a4f6eb63332300f4ab6144d4f9d72cf3"));
            }
            if (GUILayout.Button("打开使用说明"))
            {

                string guidToPathString = AssetDatabase.GUIDToAssetPath("d1b77b9ae9a27f54ca7dea5df7bdfa93");
                Application.OpenURL(Application.dataPath.Substring(0, Application.dataPath.Length - 6) +
                                    guidToPathString);
            }

            GUILayout.EndHorizontal();


            if (GUILayout.Button("开始测试并导出测试文档"))
            {
                //if (!File.Exists(python_path_text))
                //{
                //    Debug.LogError("请选择正确的handler.exe");
                //    return;
                //}

                ReadExcel readExcel = new ReadExcel();
                readExcel.IsOpen();

                if (!Directory.Exists(exportCSVPath))
                {
                    Directory.CreateDirectory(exportCSVPath);
                    //缓存本地报告导出路径           
                }
                ExecuteEffectCheck(GetConfig(m_ParPathKey));
            }

            if (GUILayout.Button("暂停并导出测试文档"))
            {
                EditorApplication.isPlaying = false;
                effectPreChecker.isTestEnd = true;
                checkAsset = CheckConfig(m_ParPathKey);
            }
            if (GUILayout.Button("查看本地报告"))
            {
                OpenLocalReportDir();
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("读取特效检测配置表"))
            {
                string configFilePath = "";
                effectCheckConfigIDs.Clear();
                configFilePath = EditorUtility.OpenFilePanel("选择配置文件", configFilePath, "csv");
                string[] IDs = CheckDataReporter.AnalysisCheckConfigFile(configFilePath);
                foreach (var id in IDs)
                {
                    effectCheckConfigIDs.Add(id);
                }
                string tip = string.Format("本次配置共导入 {0} 项特效预制", effectCheckConfigIDs.Count);
                Debug.Log(tip);
                isImportCheckConfig = true;
            }
            if (GUILayout.Button("清空特效配置表缓存"))
            {
                string tip = string.Format("清除 {0} 项检测特效配置", effectCheckConfigIDs.Count);
                Debug.Log(tip);
                effectCheckConfigIDs.Clear();
                isImportCheckConfig = false;
            }
            GUILayout.EndHorizontal();

            StringBuilder tempTips = new StringBuilder("");
            tempTips.Append("\n\n使用说明：\n");
            tempTips.Append("1.打开预设好的场景“EffectTestScene”进行测试。\n");
            tempTips.Append("2.选择文件夹或者特效预制体拖入选择框中。\n");
            tempTips.Append("3.选择目录中导出测试文档文件夹路径。\n");
            tempTips.Append("4.点击开始测试按钮。\n");
            tempTips.Append("\n 注意：\n");
            tempTips.Append("1.使用其他场景测试需要将相机的Environment中的Background Type 设置为Solid Color并设置为黑色才可以正确计算OverDraw。\n");
            tempTips.Append("2.如果将要写入数据的测试文档已打开，将导致测试数据写入原文档失败，会在同目录下新建文档写入测试数据。\n");
            tempTips.Append("3.如果测试特效的贴图相关测试数据为空，请核对特效引用资源是否丢失。\n");
            GUILayout.Label(tempTips.ToString());



            if (first)
            {
                //excelReadnSet = false;
                first = false;
            }
        }

        public static void ExecuteEffectCheckStatic() {
            var folderPath = "Assets/Things/Effect/Prefab/Combat";
            var excelPath = "Packages/com.unity.effectchecker/test.xlsx";
            var outputPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6) + "Output";
            ExecuteEffectCheckStatic(folderPath, excelPath, 0, int.MaxValue, outputPath, 3.0f);
            EffectPreChecker.Instance.exitWhenCheckEnd = true;
        }

        /// <summary>
        /// 调用这个会进行全部检测一遍，不过不能选择索引
        /// </summary>
        /// <param name="folderPath">"指定输入检测文件夹的路径"</param>
        /// <param name="excelPath">"指定读取excel表格的路径,默认没有表格"</param>
        /// <param name="startIndex">"指定读取excel表格的路径,默认没有表格"</param>
        /// <param name="endIndex">"指定读取excel表格的路径,默认没有表格"</param>
        /// <param name="outputPath">"指定输出excel表格的路径,默认Output文件夹"</param>
        /// <param name="speed">"运行速度，默认为1"</param>
        public static void ExecuteEffectCheckStatic(string folderPath, string excelPath="", int startIndex = 0, int endIndex = int.MaxValue, string outputPath="Output",float speed = 1.0f)
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;  
            }
            Scene nowScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(nowScene);
            EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath("a4f6eb63332300f4ab6144d4f9d72cf3"));
            effectPreChecker = EffectPreChecker.Instance; 
            EditorPrefs.SetString("EffectCheckerInput", excelPath);
            ReadExcel readExcel = new ReadExcel();
            readExcel.IsOpen();
            effectPreChecker.SetReporterConfig("9", "", "", false);
            effectPreChecker.playingSpeedRatio = speed;

            effectPreChecker.effectPreIndex = startIndex;
            effectPreChecker.endPreIndex = endIndex;

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
            effectPreChecker.exportCSVPath = outputPath;
            string[] searchInFolders = new string[] { folderPath };
            ParticleCheckStatic(searchInFolders);
        }



        private void ExecuteEffectCheck(string path)
        {
            effectPreChecker.SetReporterConfig(m_app_id_text_field, "", m_remark, debugToggleValue);
            effectPreChecker.effectPreIndex = startIndex - 1;
            effectPreChecker.endPreIndex = endIndex;
            effectPreChecker.playingSpeedRatio = speedRatio;
            string[] searchInFolders = new string[] { path };
            ParticleCheck(searchInFolders);
        }

        private int GetPrefabCount(string[] searchInFolders)
        {
            int count = 0;
            foreach (string filter in searchPattern)
            {
                string[] guids = AssetDatabase.FindAssets(filter, searchInFolders);
                count += guids.Length;
            }
            return count;
        }

        static private void SearchPrefabStatic(string[] searchInFolders)
        {
            List<string> assetPaths = new List<string>();
            List<string> categories = new List<string>();
            effectPreChecker.prefabPathDatas.Clear();
            string filter = "";
            string[] guids = null;
                filter = searchPattern[0];
                //
                guids = AssetDatabase.FindAssets(filter, searchInFolders);
                foreach (var guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!assetPaths.Contains(assetPath))
                    {
                        //在这里修改
                        var gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                        if (gameObject != null)
                        {
                            assetPaths.Add(assetPath);
                        }

                    }
                }

            
            if (guids.Length == 0 && assetPaths.Count == 0)
            {
                assetPaths.Add(searchInFolders[0]);
                categories.Add("特效检测");
                effectPreChecker.endPreIndex = 1;
            }
            else
            {
                effectPreChecker.endPreIndex = effectPreChecker.endPreIndex>assetPaths.Count? assetPaths.Count: effectPreChecker.endPreIndex;
            }

            if (effectPreChecker.effectPreIndex < 0) effectPreChecker.effectPreIndex = 0;
            else if (effectPreChecker.effectPreIndex > effectPreChecker.endPreIndex) effectPreChecker.effectPreIndex = effectPreChecker.endPreIndex;
            effectPreChecker.prefabPathDatas.AddRange(assetPaths);

            effectPreChecker.InitMeshData();
            GameObject tempPre = null;
            for (int i = 0; i < assetPaths.Count; i++)
            {
                Debug.Log(assetPaths[i]);
                tempPre = AssetDatabase.LoadAssetAtPath(assetPaths[i], typeof(GameObject)) as GameObject;
                if (tempPre != null)
                {
                    ParticleSystemRenderer[] m_ParticleSystemRenderers = tempPre.GetComponentsInChildren<ParticleSystemRenderer>(true);
                    if (m_ParticleSystemRenderers.Length == 0)
                    {
                        continue;
                    }
                    StringBuilder colorMeshData = new StringBuilder();
                    StringBuilder uv2MeshData = new StringBuilder();
                    StringBuilder uv3OrUv4MeshData = new StringBuilder();
                    foreach (var psRender in m_ParticleSystemRenderers)
                    {
                        if (psRender != null && psRender.renderMode == ParticleSystemRenderMode.Mesh && psRender.mesh != null)
                        {
                            Mesh mesh = psRender.mesh;
                            if (mesh.colors.Length > 0)
                            {
                                colorMeshData.Append(string.Format("\n{0}/{1}.", psRender.name, mesh.name));
                            }
                            if (mesh.uv2.Length > 0)
                            {
                                uv2MeshData.Append(string.Format("\n{0}/{1}.", psRender.name, mesh.name));
                            }
                            if (mesh.uv3.Length > 0 || mesh.uv4.Length > 0)
                            {
                                uv3OrUv4MeshData.Append(string.Format("\n{0}/{1}.", psRender.name, mesh.name));
                            }
                        }
                    }
                    MeshData meshData;
                    meshData.assetPath = assetPaths[i];
                    string colorMeshStr = colorMeshData.ToString();
                    colorMeshStr = FormatMeshStr(colorMeshStr);
                    meshData.colorMeshData = colorMeshStr;
                    string uv2MeshStr = uv2MeshData.ToString();
                    uv2MeshStr = FormatMeshStr(uv2MeshStr);
                    meshData.uv2MeshData = uv2MeshStr;
                    string uv3OrUv4MeshStr = uv3OrUv4MeshData.ToString();
                    uv3OrUv4MeshStr = FormatMeshStr(uv3OrUv4MeshStr);
                    meshData.uv3OrUv4Data = uv3OrUv4MeshStr;
                    effectPreChecker.AddMeshData(meshData);
                }
            }
            EditorUtility.UnloadUnusedAssetsImmediate();
        }




        private void SearchPrefab(string[] searchInFolders)
        {
            List<string> assetPaths = new List<string>();
            List<string> categories = new List<string>();
            effectPreChecker.prefabPathDatas.Clear();
            string filter = "";
            string[] guids = null;
            if (isImportCheckConfig)
            {
                foreach (var id in effectCheckConfigIDs)
                {
                    filter = string.Format("{0} {1}", searchPattern[0], id);
                    guids = AssetDatabase.FindAssets(filter, searchInFolders);
                    foreach (var guid in guids)
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        if (!assetPaths.Contains(assetPath))
                        {
                            assetPaths.Add(assetPath);
                        }
                    }
                }
                endIndex = assetPaths.Count;
            }
            else
            {
                filter = searchPattern[0];
                //
                guids = AssetDatabase.FindAssets(filter, searchInFolders);
                foreach (var guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!assetPaths.Contains(assetPath))
                    {
                        //在这里修改
                        var gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                        if (gameObject != null)
                        {
                            assetPaths.Add(assetPath);
                        }

                    }
                }

            }
            if (guids.Length == 0 && assetPaths.Count == 0)
            {
                assetPaths.Add(searchInFolders[0]);
                categories.Add("特效检测");
                effectPreChecker.endPreIndex = 1;
            }
            else
            {
                effectPreChecker.endPreIndex = effectPreChecker.endPreIndex > assetPaths.Count
                    ? assetPaths.Count
                    : effectPreChecker.endPreIndex;
            }
            effectPreChecker.prefabPathDatas.AddRange(assetPaths);

            effectPreChecker.InitMeshData();
            GameObject tempPre = null;
            for (int i = 0; i < assetPaths.Count; i++)
            {
                Debug.Log(assetPaths[i]);
                tempPre = AssetDatabase.LoadAssetAtPath(assetPaths[i], typeof(GameObject)) as GameObject;
                if (tempPre != null)
                {
                    ParticleSystemRenderer[] m_ParticleSystemRenderers = tempPre.GetComponentsInChildren<ParticleSystemRenderer>(true);
                    if (m_ParticleSystemRenderers.Length == 0)
                    {
                        continue;
                    }
                    StringBuilder colorMeshData = new StringBuilder();
                    StringBuilder uv2MeshData = new StringBuilder();
                    StringBuilder uv3OrUv4MeshData = new StringBuilder();
                    foreach (var psRender in m_ParticleSystemRenderers)
                    {
                        if (psRender != null && psRender.renderMode == ParticleSystemRenderMode.Mesh && psRender.mesh != null)
                        {
                            Mesh mesh = psRender.mesh;
                            if (mesh.colors.Length > 0)
                            {
                                colorMeshData.Append(string.Format("\n{0}/{1}.", psRender.name, mesh.name));
                            }
                            if (mesh.uv2.Length > 0)
                            {
                                uv2MeshData.Append(string.Format("\n{0}/{1}.", psRender.name, mesh.name));
                            }
                            if (mesh.uv3.Length > 0 || mesh.uv4.Length > 0)
                            {
                                uv3OrUv4MeshData.Append(string.Format("\n{0}/{1}.", psRender.name, mesh.name));
                            }
                        }
                    }
                    MeshData meshData;
                    meshData.assetPath = assetPaths[i];
                    string colorMeshStr = colorMeshData.ToString();
                    colorMeshStr = FormatMeshStr(colorMeshStr);
                    meshData.colorMeshData = colorMeshStr;
                    string uv2MeshStr = uv2MeshData.ToString();
                    uv2MeshStr = FormatMeshStr(uv2MeshStr);
                    meshData.uv2MeshData = uv2MeshStr;
                    string uv3OrUv4MeshStr = uv3OrUv4MeshData.ToString();
                    uv3OrUv4MeshStr = FormatMeshStr(uv3OrUv4MeshStr);
                    meshData.uv3OrUv4Data = uv3OrUv4MeshStr;
                    effectPreChecker.AddMeshData(meshData);
                }
            }
            EditorUtility.UnloadUnusedAssetsImmediate();
        }

        private void OpenLocalReportDir()
        {
            string localReportDirCache = EditorPrefs.GetString(EffectCheckerConfig.localReportDirKey);
            localReportDirCache = localReportDirCache.Replace("\\", "/");
            Debug.Log("打开本地报告路径:" + localReportDirCache);
            Application.OpenURL("file:///" + localReportDirCache);
        }
        private void ParticleCheck(string[] searchInFolders)
        {
            SearchPrefabStatic(searchInFolders);
            if (effectPreChecker.prefabPathDatas.Count == 0)
            {
                Debug.Log("未检测到特效资源");
                return;
            }
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;

                EditorApplication.isPlaying = true;
            }
            else
            {
                EditorApplication.isPlaying = true;
            }
        }
        static private void ParticleCheckStatic(string[] searchInFolders)
        {
            SearchPrefabStatic(searchInFolders);
            if (effectPreChecker.prefabPathDatas.Count == 0)
            {
                Debug.Log("未检测到特效资源");
                return;
            }
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;

                EditorApplication.isPlaying = true;
            }
            else
            {
                EditorApplication.isPlaying = true;
            }
        }

        private void OnDestroy()
        {
            EffectPreChecker.Destory();
        }

        private string GetAssetsPath(string serializePath)
        {
            string assetPath = " ";
            int assetStrIndex = serializePath.IndexOf("Assets");
            assetPath = serializePath.Substring(assetStrIndex);
            return assetPath;
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

        static string GetConfig(string key)
        {
            return EditorPrefs.GetString(key);
        }

        static void SetConfig(string key, string value)
        {
            EditorPrefs.SetString(key, value);
        }

        static string FormatMeshStr(string str)
        {
            string result = str;
            if (!string.IsNullOrEmpty(result) && result.Length > 0)
            {
                result = result.Remove(0, 1);
            }

            return result;
        }



    }
}
#endif