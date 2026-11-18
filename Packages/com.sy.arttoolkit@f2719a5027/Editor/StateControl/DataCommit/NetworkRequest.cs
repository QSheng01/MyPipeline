
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
namespace SYArtToolkit.Submit
{

    public static class NetworkRequest
    {
        private readonly static string url = "http://192.168.21.29:4025/ToolCount/Asset/upload.php";

        public static void SaveData(string data, string dataPath)
        {
            if (!Directory.Exists(Path.GetDirectoryName(dataPath)))
            {
                dataPath = Path.GetDirectoryName(dataPath);
                Directory.CreateDirectory(dataPath);
            }
            if (!File.Exists(dataPath))
            {
                File.Create(dataPath).Close();
            }
            File.AppendAllText(dataPath, data);
            return;
        }

        public static void SubmitPath(string dataPath)
        {
            try
            {

                if (!File.Exists(dataPath))
                {
                    return;
                }
                else
                {

                    byte[] fileData = File.ReadAllBytes(dataPath);
                    if (fileData.Length > 0)
                    {
                        Submit(dataPath);
                    }
                    else
                    {
                    }
                }
            }
            catch
            {

            }
        }


        public static async void SubmitData(string data)
        {

            byte[] fileData = System.Text.Encoding.Default.GetBytes(data);
            UnityWebRequest request = CreateRequest(fileData);
            await SendRequestAsync(request, null);
        }

        private static async void Submit(string dataPath)
        {
            byte[] fileData = File.ReadAllBytes(dataPath);
            UnityWebRequest request = CreateRequest(fileData);
            await SendRequestAsync(request, dataPath);
        }



        private static async Task SendRequestAsync(UnityWebRequest request, string filePath)
        {
#if UNITY_2022_1_OR_NEWER
            if (UnityEditor.PlayerSettings.insecureHttpOption == UnityEditor.InsecureHttpOption.NotAllowed)
            {
                UnityEditor.PlayerSettings.insecureHttpOption = UnityEditor.InsecureHttpOption.DevelopmentOnly;
            }
#endif
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
                if (!string.IsNullOrEmpty(filePath))
                    File.WriteAllText(filePath, string.Empty);
            }
        }
        private static UnityWebRequest CreateRequest(byte[] bytes)
        {
            WWWForm form = new WWWForm();
            form.AddBinaryData("file", bytes, "haha.txt", "text/plain");

            UnityWebRequest request = UnityWebRequest.Post(url, form);
            request.certificateHandler = new WebReqSkipCert();

            return request;
        }
        private class WebReqSkipCert : CertificateHandler
        {
            protected override bool ValidateCertificate(byte[] certificateData)
            {
                return true;
            }
        }








    }


}