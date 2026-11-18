#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Text;
using System;

namespace SY.EffectChecker
{
    [Serializable]
public struct MeshData
{
    public string assetPath;
    public string colorMeshData;
    public string uv2MeshData;
    public string uv3OrUv4Data;
}


    public class EffectPreChecker : MonoBehaviour
    {
        public bool exitWhenCheckEnd = false;

        [HideInInspector]
        public List<string> prefabPathDatas = new List<string>();

        [HideInInspector]
        public List<MeshData> MeshCheckData = new List<MeshData>();

        private StringBuilder outTestData;

        private List<ParticleEffectInfo> particleEffectInfos;

        private bool isCheck = false;
        [HideInInspector]
        public bool isTestEnd = false;
        public int effectPreIndex = 0;
        public int endPreIndex = 0;
        [HideInInspector]
        public string exportCSVPath;

        private GameObject cur_EffectPre;
        private bool isPrepareForceEnd;
#if UNITY_EDITOR
        private ParticleEffectScript cur_ParticleEffectScript;
#endif

        public Camera mainCamera;
        public Camera overDrawCamera;

        private int recordIndex;

        public float playingSpeedRatio;

        private static EffectPreChecker instance;

        private static Transform root;

        // 输出路径、上报信息
        [HideInInspector]
        public string m_app_id, m_python_path, m_remark;
        [HideInInspector]
        public bool m_debug = false;
        [HideInInspector]
        public static string switchScene = "";
        [HideInInspector]
        public static Dictionary<string, string> sceneDict;
        public static Dictionary<string, string> sceneDictDesc;
        [HideInInspector]
        public string m_scene_1, m_scene_2, m_scene_3;

        public static EffectPreChecker Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<EffectPreChecker>();
                    if (instance == null)
                    {
                        var go = new GameObject("New Particle Tool");
                        instance = go.AddComponent<EffectPreChecker>();
                    }
                }
                return instance;
            }
        }

        public static void Destory()
        {
            if (instance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(instance.gameObject);
                }
                else
                {
                    DestroyImmediate(instance.gameObject);
                }
                instance = null;
            }
        }


        public void SetSceneInfo(string scene1, string scene2, string scene3)
        {
            m_scene_1 = scene1;
            m_scene_2 = scene2;
            m_scene_3 = scene3;
        }

        public void SetReporterConfig(string app_id, string python_path, string remark, bool debug)
        {
            m_app_id = app_id;
            m_python_path = python_path;
            m_remark = remark;
            m_debug = debug;
        }

        private System.Diagnostics.Stopwatch stopwatch;
        // Start is called before the first frame update
        void Start()
        {
            if (prefabPathDatas.Count != 0)
            {
                Debug.Log("检测预制数：" + prefabPathDatas.Count);
            }
            //运行后聚焦到SceneView
            if (EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.titleContent.text == "Scene")
            {
                Debug.Log("Scene window in focused");
            }
            else
            {
                SceneView.FocusWindowIfItsOpen<SceneView>();
                Debug.Log("聚焦SceneView");
            }
            InitData();
            stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            mainCamera = Camera.main;
            CreateOverDrawCamera();

            root = GameObject.Find("EffectNode").transform;

        }

        void CreateOverDrawCamera()
        {
            if (overDrawCamera != null)
            { return; }
            var OverDrawCameraPre = new GameObject("OverDrawCamera");
            UnityEditorInternal.ComponentUtility.CopyComponent(mainCamera);
            UnityEditorInternal.ComponentUtility.PasteComponentAsNew(OverDrawCameraPre);
            overDrawCamera = OverDrawCameraPre.GetComponent<Camera>();
            overDrawCamera.depth = -5;
        }

        public void InitMeshData()
        {
            MeshCheckData.Clear();
        }

        public void AddMeshData(MeshData meshData)
        {
            MeshCheckData.Add(meshData);
        }

        #region 输出检测记录文件,需要时打开注释
        private static string logTxtPath;

        void CreatTxtLogFile(string logData)
        {
            FileStream file = new FileStream(logTxtPath, FileMode.Create);
            Debug.Log("检测记录文本文件:" + logTxtPath);
            byte[] bts = Encoding.UTF8.GetBytes(logData);
            file.Write(bts, 0, bts.Length);
            if (file != null)
            {
                //清空缓存
                file.Flush();
                // 关闭流
                file.Close();
                //销毁资源
                file.Dispose();
            }
            AssetDatabase.Refresh();
        }

        void WriteLogTxt(string data)
        {
            StreamWriter sw;
            FileInfo fileInfo = new FileInfo(logTxtPath);
            sw = fileInfo.AppendText();
            sw.WriteLine(data);
            sw.Close();
            sw.Dispose();
        }

        string RecordCurInfo()
        {
            Debug.Log("备份当前检测记录" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            string checkData = "";
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = recordIndex; i < particleEffectInfos.Count; i++)
            {
                var info = particleEffectInfos[i];
                stringBuilder.Append(info.name);
                stringBuilder.Append(",");
                stringBuilder.Append(info.memorySize);
                stringBuilder.Append(",");
                stringBuilder.Append(info.textureCount);
                stringBuilder.Append(",");
                stringBuilder.Append(info.particleSystemCount);
                stringBuilder.Append(",");
                stringBuilder.Append(info.particleEffecDrawCall);
                stringBuilder.Append(",");
                stringBuilder.Append(info.maxParticleCount);
                stringBuilder.Append(",");
                stringBuilder.Append(info.overDrawRate);
                stringBuilder.Append(",");
                stringBuilder.Append(info.maxOverDrawRate);
                stringBuilder.Append(",");
                stringBuilder.Append(info.openPrewarnCount);
                stringBuilder.Append(",");
                stringBuilder.Append(info.closePrewarnCount);
                stringBuilder.Append(",");
                stringBuilder.Append(info.effectPrePath);
                stringBuilder.Append(",");
                stringBuilder.Append(info.isExceedPlayTimeLimit);
                stringBuilder.Append(",");
                string colorMeshs = info.colorMeshs.Replace('\n', ' ');
                stringBuilder.Append(colorMeshs);
                stringBuilder.Append(",");
                string uv2Meshs = info.uv2Meshs.Replace('\n', ' ');
                stringBuilder.Append(uv2Meshs);
                stringBuilder.Append(",");
                string uv3OrUv4Meshs = info.uv2Meshs.Replace('\n', ' ');
                stringBuilder.Append(uv3OrUv4Meshs);
                stringBuilder.Append(",");
                stringBuilder.Append(info.maxTrisCount);
                stringBuilder.Append("\n");
            }
            recordIndex = particleEffectInfos.Count;
            checkData = stringBuilder.ToString();
            return checkData;
        }
        #endregion

        public MeshData GetMeshData(string assetPath)
        {
            MeshData newData;
            newData.assetPath = "";
            newData.colorMeshData = "";
            newData.uv2MeshData = "";
            newData.uv3OrUv4Data = "";
            foreach (var meshData in MeshCheckData)
            {
                if (meshData.assetPath.Equals(assetPath))
                {
                    newData = meshData;
                }
            }
            if (string.IsNullOrEmpty(newData.assetPath))
            {
                Debug.Log(assetPath + "资源未检测");
            }
            return newData;
        }

        // Update is called once per frame
        void Update()
        {
#if UNITY_EDITOR
            ProcessChecking();
#endif
        }

#if UNITY_EDITOR
        private void ProcessChecking()
        {
            if (cur_ParticleEffectScript != null && cur_ParticleEffectScript.isAllStopped == true)
            {
                Destroy(cur_EffectPre);
                isCheck = true;
            }

            if (isCheck == true && effectPreIndex < endPreIndex)
            {

                isCheck = false;
                Debug.Log(effectPreIndex);
                EffectPlay(prefabPathDatas[effectPreIndex], effectPreIndex);
                effectPreIndex++;
            }

            if (isCheck && !isTestEnd && effectPreIndex == endPreIndex && endPreIndex > 0)
            {
                Debug.Log("测试结束");
                isTestEnd = true;
                stopwatch.Stop();
                TimeSpan timespan = stopwatch.Elapsed;
                Debug.Log("总执行时间:" + timespan);
                effectPreIndex++;
                EditorUtility.UnloadUnusedAssetsImmediate();
                Time.timeScale = 1;

                isTestEnd = false;
                isCheck = false;
                effectPreIndex = 0;
                endPreIndex = 0;
#if UNITY_EDITOR
                // 自动上报数据
                CheckDataReporter.ExportReporter(exportCSVPath, m_app_id, m_python_path, m_remark, m_debug);
                // 自动退出
                if (exitWhenCheckEnd) {
                    EditorApplication.Exit(0);
                }
#endif
            }
        }
#endif

        private void EffectPlay(string prefabPath, int index)
        {
            cur_EffectPre = AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject)) as GameObject;
            var particleSystemRenderer = cur_EffectPre.GetComponentsInChildren<ParticleSystemRenderer>(true);
            if (particleSystemRenderer.Length == 0)
            {
                Debug.Log("当前进度:" + (index + 1) + "/" + prefabPathDatas.Count);
                Debug.Log("Path:" + prefabPath);
                Debug.Log("[" + cur_EffectPre.name + "]" + "不是特效无法测试");
                isCheck = true;
                return;
            }
            GameObject effectPre = Instantiate(cur_EffectPre);

            if (root)
            {
                effectPre.transform.SetParent(root);
            }
            effectPre.AddComponent<ParticleEffectScript>();

            //effectPre.transform.position = new Vector3(0, 0, 0);
            effectPre.transform.localPosition = Vector3.zero;
            effectPre.transform.localScale = Vector3.one;
            effectPre.SetActive(true);

            cur_ParticleEffectScript = effectPre.GetComponent<ParticleEffectScript>();
            FocusCameraOnGameObject(mainCamera, effectPre);
            overDrawCamera.transform.position = mainCamera.transform.position;
            overDrawCamera.transform.rotation = mainCamera.transform.rotation;

            cur_ParticleEffectScript.overDrawCamera = overDrawCamera;
            cur_ParticleEffectScript.effectPrePath = prefabPath;
            cur_EffectPre = effectPre;
            Selection.activeGameObject = effectPre;
            cur_ParticleEffectScript.speedRatio = playingSpeedRatio;
            Debug.Log("当前进度:" + (index + 1) + "/" + prefabPathDatas.Count);
            Debug.Log("Path:" + cur_ParticleEffectScript.effectPrePath);
            if (index % 50 == 0)
            {
                WriteLogTxt(RecordCurInfo());
            }
        }

        private Bounds CalculateBounds(GameObject go)
        {
            Bounds b = new Bounds(go.transform.position, Vector3.zero);
            object[] rList = go.GetComponentsInChildren(typeof(Renderer));
            foreach (Renderer r in rList)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }

        private void FocusCameraOnGameObject(Camera camera, GameObject go)
        {
            Bounds b = CalculateBounds(go);
            //占屏比例系数
            float ScreenScaleFacotr = 1;
            float xDis = b.size.x;
            float yDis = b.size.y;
            float sizeX = xDis / ScreenScaleFacotr / 2 / camera.aspect;
            float sizeY = yDis / ScreenScaleFacotr / 2;
            sizeX = sizeX > 0 ? sizeX : 2;
            sizeY = sizeY > 0 ? sizeY : 2;
            if (sizeX >= sizeY)
            {
                camera.orthographicSize = sizeX;
            }
            else
            {
                camera.orthographicSize = sizeY;
            }
            overDrawCamera.orthographicSize = camera.orthographicSize;
        }

#if UNITY_EDITOR
        public ParticleEffectScript GetCur_ParticleEffectScript()
        {
            return cur_ParticleEffectScript;
        }
#endif

        private void InitData()
        {
            if (exportCSVPath == null)
            {
                return;
            }
            recordIndex = 0;
            isCheck = true;
            particleEffectInfos = new List<ParticleEffectInfo>();
            logTxtPath = Path.Combine(exportCSVPath, "EffectCheckRecord.txt");
            string titleModel = "特效预制体,贴图内存,不重复贴图数,特效中所有粒子系统组件数,最高DrawCall数,最高粒子数,平均每像素OverDraw率,最高每像素overDraw率,开启Prewarn的粒子预制数,未开启Prewarn的粒子预制数,特效预制体路径,超出10秒特效播放时间,包含Color属性的网格,包含uv2属性的网格,包含uv3或uv4属性的网格,最高播放面数\n";
            CreatTxtLogFile(titleModel);
        }

        public void AddParticleEffectInfo(ParticleEffectInfo particleEffectInfo)
        {
            if (particleEffectInfos != null)
            {
                particleEffectInfos.Add(particleEffectInfo);
            }
        }

        public List<ParticleEffectInfo> GetAllParticleEffectInfo()
        {
            List<ParticleEffectInfo> EffectInfos = new List<ParticleEffectInfo>();
            EffectInfos.AddRange(particleEffectInfos);
            return EffectInfos;
        }
    }
}
#endif