using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace SYArtToolkit.Submit
{

    //public partial class PrivateClass
    //{


    internal class ReadNetWorkData : EditorWindow
    {


        //[MenuItem("美术工具箱/工具列表/其他/获取当前服务器数据")]
        public static void Open()
        {
            //Debug.Log(ArtToolKitSetting.Instance.artToolKitDeveloperMode);
            if (ArtToolKitSetting.Instance.artToolKitDeveloperMode)
                GetWindow<ReadNetWorkData>().Show();
        }

        //UnityWebRequest webRequest;
        private string downloadingUrl = "http://192.168.21.29:4025/ToolCount/Asset/uploads/data.json";
        private ArtToolkitData artToolkitData;
        private Dictionary<string, int> dictionary;
        private void OnEnable()
        {
            Submit();
        }

        private Vector2 v2;

        private void OnGUI()
        {
            v2 = GUILayout.BeginScrollView(v2, false, true);
            if (artToolkitData != null)
            {
                foreach (var data in artToolkitData.allData)
                {
                    if (string.IsNullOrEmpty(data.name)) continue;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(data.name, GUILayout.Width(100));
                    GUILayout.Label(data.version, GUILayout.Width(100));
                    GUILayout.Label(data.count.ToString(), GUILayout.Width(30));
                    GUILayout.Label(data.user, GUILayout.Width(200));
                    GUILayout.Label(data.id, GUILayout.Width(100));
                    GUILayout.Label(data.time, GUILayout.Width(200));
                    GUILayout.Label(data.comment, GUILayout.Width(100));
                    GUILayout.EndHorizontal();
                }
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.Label("统计结果:");
                GUILayout.Label(string.Format("共计数据{0}条", artToolkitData.allData.Count));
                GUILayout.EndHorizontal();

                if (dictionary == null)
                {

                    dictionary = new Dictionary<string, int>();
                    foreach (var data in artToolkitData.allData)
                    {
                        if (string.IsNullOrEmpty(data.name)) continue;
                        if (!dictionary.ContainsKey(data.name))
                        {
                            dictionary.Add(data.name, data.count);
                        }
                        else
                        {
                            dictionary[data.name] += data.count;
                        }
                    }
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    foreach (var kv in dictionary)
                    {
                        GUILayout.Label(string.Format("{0}:{1}", kv.Key, kv.Value.ToString()));
                    }

                    GUILayout.EndHorizontal();
                }
            }

            if (GUILayout.Button("更新数据"))
            {
                dictionary = null;
                OnEnable();
            }


            GUILayout.EndScrollView();

        }
        // Use this for initialization
        // void Start()
        // {
        //
        //     //StartCoroutine(Down(downloadUrl));
        // }

        // Update is called once per frame

        private async void Submit()
        {
            //byte[] fileData = File.ReadAllBytes(dataPath);

            var webRequest = UnityWebRequest.Get(downloadingUrl);
            webRequest.timeout = 30; //设置超时，若webRequest.SendWebRequest()连接超时会返回，且isNetworkError为true
            await SendRequestAsync(webRequest);
        }



        private async Task SendRequestAsync(UnityWebRequest request)
        {
            var asyncOperation = request.SendWebRequest();

            while (!asyncOperation.isDone)
            {
                // 在这里可以添加处理异步操作的代码
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(request.error);
            }
            else
            {
                var file = request.downloadHandler.data;

                string jsonData = System.Text.Encoding.UTF8.GetString(file);
                artToolkitData = JsonUtility.FromJson<ArtToolkitData>(jsonData);
            }
        }




    }


    /// <summary>
    /// 根据URL下载文件
    /// </summary>
    /// <param name="downloadingUrl"></param>
    /// <returns></returns>
    // IEnumerator Down(string downloadingUrl)
    //
    // {
    //     //发送请求
    //     webRequest = UnityWebRequest.Get(downloadingUrl);
    //     webRequest.timeout = 30; //设置超时，若webRequest.SendWebRequest()连接超时会返回，且isNetworkError为true
    //     yield return webRequest.SendWebRequest();
    //
    //     if (webRequest.isNetworkError)
    //     {
    //         Debug.Log("Download Error:" + webRequest.error);
    //     }
    //     else
    //     {
    //         //获取二进制数据
    //
    //         var File = webRequest.downloadHandler.data;
    //
    //         string jsonData = System.Text.Encoding.UTF8.GetString(File);
    //         
    //         //创建文件写入对象
    //     }
    //
    //
    //
    // }

    /// <summary>
    /// 获取下载进度
    /// </summary>
    /// <returns></returns>
    // public float GetProcess()
    // {
    //     if (webRequest != null)
    //     {
    //         return webRequest.downloadProgress;
    //     }
    //
    //     return 0;
    // }
    //
    // /// <summary>
    // /// 获取当前下载内容长度
    // /// </summary>
    // /// <returns></returns>
    // public long GetCurrentLength()
    // {
    //     if (webRequest != null)
    //     {
    //         return (long)webRequest.downloadedBytes;
    //     }
    //
    //     return 0;
    // }

}