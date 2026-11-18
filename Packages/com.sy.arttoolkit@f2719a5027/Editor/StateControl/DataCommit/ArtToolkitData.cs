using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;


namespace SYArtToolkit.Submit
{
    using System;

    //矩阵变换

    [Serializable]
    public class ArtToolkitData
    {
        private static ArtToolkitData instance;

        public static ArtToolkitData Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ArtToolkitData();
                }

                return instance;
            }
        }

        [SerializeField] public List<Data> allData = new List<Data>();

        public static Task task = null;
        // private DateTime time;
        // private int textureCount;
        // private int assetReferenceCount;
        // private int convenientCount;
        // private int performanceTestCount;
        // private int batchModifyCount;
        // private int assetCheckCount;
        // private int drawCallTestCount;
        // private int effectOptimizationCount;
        // private int meshCombineCount;
        public void AddNewToolData(string name, int count, string version = null)
        {
            if (allData.Count > 0)
            {
                var dataTemp = allData[allData.Count - 1];
                if (dataTemp.name == name && dataTemp.time == DateTime.Now.ToString())
                {
                    dataTemp.count += count;
                }
                else
                {
                    allData.Add(new Data(name, version, count));
                }
            }
            else
            {
                allData.Add(new Data(name, version, count));
            }
        }

        public void AddNewToolData(string name, float count, string version = null)
        {
            allData.Add(new Data(name, version, count));
        }


        public void AddEffectOptimization(int number)
        {
            AddNewToolData("特效优化工具", number);
            UpdateSubmit();
        }

        public void AddMeshCombine(int number)
        {
            AddNewToolData("网格合并工具", number);
            UpdateSubmit();
        }

        public void AddDrawCallTest(int number)
        {
            AddNewToolData("渲染检测工具", number);
            UpdateSubmit();
        }

        public void AddTexture(int number)
        {
            AddNewToolData("贴图工具", number);
            UpdateSubmit();
        }

        public void AddAssetRef(int number)
        {
            AddNewToolData("资产引用查找工具", number);
            UpdateSubmit();
        }

        public void AddConvenient(int number)
        {
            AddNewToolData("便捷工具", number);
            UpdateSubmit();
        }

        public void AddPerformance(int number)
        {
            AddNewToolData("性能检测工具", number);
            UpdateSubmit();
        }

        public void AddBatchModify(int number)
        {
            AddNewToolData("批量操作工具", number);
            UpdateSubmit();
        }

        public void AddAssetCheck(int number)
        {
            AddNewToolData("资产审核工具", number);
            UpdateSubmit();
        }

        private ArtToolkitData()
        {
            //先检测上次失败的，然后保存
            string dataPath = ArtToolKitSetting.Instance.saveDataPath;
            try
            {
                NetworkRequest.SubmitPath(dataPath);
            }
            catch
            {

            }

            Clear();
        }

        ~ArtToolkitData()
        {
            Close();
        }

        private string GetString()
        {
            if (allData.Count < 1) return "";
            StringBuilder sb = new StringBuilder();

            foreach (var data in allData)
            {
                sb.Append(",");
                sb.Append(JsonUtility.ToJson(data));
            }

            return sb.ToString();


            //         int id = 4;
            //         // var ids = AssetDatabase.FindAssets("glob:\"Packages/*/ProjectSet.asset\" t:ProjectSet", new string[] { "Packages" });
            //         // if (ids != null && ids.Length > 0)
            //         // {
            //         //     var idTemp = AssetDatabase.LoadAssetAtPath<ProjectSet>(AssetDatabase.GUIDToAssetPath(ids[0]));
            //         //     if (idTemp != null) id = (int)idTemp.id;
            //         // }
            //
            //         string inputString = string.Format("<person>\n{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}\n{7}\n{8}\n{9}\n{10}\n{11}\n{12}\n</person>\n",
            // string.Format("<tool>{0}</tool>", "ArtToolkit"),
            // string.Format("<id>{0}</id>", id.ToString()),
            // string.Format("<user>{0}</user>", SystemInfo.deviceName.ToString()),
            // string.Format("<time>{0}</time>", time.ToString()),
            // string.Format("<生成贴图数量>{0}</生成贴图数量>", textureCount.ToString()),
            // string.Format("<资产引用查找数量>{0}</资产引用查找数量>", assetReferenceCount.ToString()),
            // string.Format("<便捷工具使用次数>{0}</便捷工具使用次数>", convenientCount.ToString()),
            // string.Format("<性能检测次数>{0}</性能检测次数>", performanceTestCount.ToString()),
            // string.Format("<批量修改次数>{0}</批量修改次数>", batchModifyCount.ToString()),
            // string.Format("<资产审核次数>{0}</资产审核次数>", assetCheckCount.ToString()),
            // string.Format("<渲染检测次数>{0}</渲染检测次数>", drawCallTestCount.ToString()),
            // string.Format("<特效优化次数>{0}</特效优化次数>", effectOptimizationCount.ToString()),
            // string.Format("<合并Mesh次数>{0}</合并Mesh次数>", meshCombineCount.ToString()));
            //
            //         
            //         return inputString;
        }

        private void UpdateSubmit()
        {
            //if(allData.Count>0)
            //
            if (task == null)
            {
                task = new Task(UpdateSubmitTask);
                task.Start();
            }
            else if (task.IsCompleted)
            {
                task.Dispose();
                task = new Task(UpdateSubmitTask);
                task.Start();
            }
            //超过一小时且数据不为0 提交
            //
        }

        private void UpdateSubmitTask()
        {


            Thread.Sleep(1000);
            Submit();
        }


        private void Submit()
        {

            if (allData.Count > 0)
            {
                string dataPath = ArtToolKitSetting.Instance.saveDataPath;
                try
                {
                    NetworkRequest.SaveData(GetString(), dataPath);
                    NetworkRequest.SubmitPath(dataPath);
                }
                catch
                {

                }
            }

            Clear();
        }

        public void Close()
        {
            Submit();
            //提交
            //失败则保存
        }

        private void Clear()
        {
            allData.Clear();
        }






        [Serializable]
        public class Data
        {

            [SerializeField] public string name; //工具名称
            [SerializeField] public string version; //版本号
            [SerializeField] public int count; //使用计数
            [SerializeField] public float spend; //使用时长(min)
            [SerializeField] public string time; //使用的时间
            [SerializeField] public string user; //用户名称
            [SerializeField] public string id; //项目名称
            [SerializeField] public string comment; //备注

            private Data()
            {
            }

            public Data(string name, string version = null, int count = 1, string time = null, string user = null,
                string id = null, string comment = null)
            {
                //time ??= System.DateTime.Now.ToString();
                //if (user == null) user = SystemInfo.deviceName;
                //if (id == null) id = ArtToolKitSetting.Instance.projectID;

                this.name = name ?? "未命名";
                this.version = version ?? "v0.0";
                this.time = time ?? System.DateTime.Now.ToString();
                this.user = user ?? SystemInfo.deviceName;
                this.id = id ?? ArtToolKitSetting.Instance.projectID;
                this.count = count;
                this.comment = comment ?? "";
                //this.spend = spend;
            }

            public Data(string name, string version = "v0.0", float spend = 1.0f, string time = null,
                string user = null, string id = null, string comment = null)
            {
                //time ??= System.DateTime.Now.ToString();
                //if (user == null) user = SystemInfo.deviceName;
                //if (id == null) id = ArtToolKitSetting.Instance.projectID;

                this.name = name ?? "未命名";
                this.version = version ?? "v0.0";
                this.time = time ?? System.DateTime.Now.ToString();
                this.user = user ?? SystemInfo.deviceName;
                this.id = id ?? ArtToolKitSetting.Instance.projectID;
                this.spend = spend;
                this.comment = comment ?? "";
                //this.spend = spend;
            }

        }
    }


    // public class ArtToolKitDataWindow : EditorWindow
    // {
    //     [MenuItem("美术工具箱/工具列表/其他/数据解析")]
    //     private static void Open()
    //     {
    //         GetWindow<ArtToolKitDataWindow>().Show();
    //     }
    //
    //     private TextAsset defaultAsset;
    //     private ArtToolkitData artToolkitData;
    //
    //     private void OnGUI()
    //     {
    //         if (defaultAsset == null)
    //         {
    //             defaultAsset = EditorGUILayout.ObjectField(defaultAsset, typeof(TextAsset), false) as TextAsset;
    //             if (defaultAsset != null)
    //             {
    //                 Debug.Log(defaultAsset.text);
    //                 artToolkitData = JsonUtility.FromJson<ArtToolkitData>(defaultAsset.text);
    //                 if (artToolkitData == null) defaultAsset = null;
    //
    //
    //             }
    //
    //         }
    //         else
    //         {
    //
    //             foreach (var data in artToolkitData.allData)
    //             {
    //                 GUILayout.BeginHorizontal();
    //                 GUILayout.Label(data.name);
    //                 GUILayout.Label(data.count.ToString());
    //                 GUILayout.EndHorizontal();
    //             }
    //
    //         }
    //     }
    // }
}