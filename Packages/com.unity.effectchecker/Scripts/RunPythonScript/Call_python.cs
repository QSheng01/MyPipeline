
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using LitJson;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using System.Text;


namespace SY.EffectChecker
{

    public class Call_python : MonoBehaviour
    {
        public static void RunPythonScript(string submitPath, int app_id, bool id_debug, string python_path, string remark = "")
        {
            DeveloperLocalHandlerInfo item = new DeveloperLocalHandlerInfo();
            item.label = "特效检测";
            item.app_id = 12;
            item.path_json = @submitPath;
            item.debug = false;
            item.remark = "baozang";
            string path = "Packages\\com.unity.effectchecker\\developer_local_handler_win\\performance.json";
            WriteJsonFile(path, item);
            string cmd = "Packages\\com.unity.effectchecker\\developer_local_handler_win\\win\\handler" + " -i "+path;
            Debug.Log(cmd);
            RunCmd(cmd);
            
        }

        public static void WriteJsonFile(string path, object jsonInfo)
        {
                string saveJsonStr = JsonMapper.ToJson(jsonInfo);
                StreamWriter sw = new StreamWriter(path);
                sw.Write(saveJsonStr);
                sw.Close();
        }
        public static void RunCmd(string cmd)
        {
            cmd = cmd.Trim().TrimEnd('&') + "&exit";//说明：不管命令是否成功均执行exit命令，否则当调用ReadToEnd()方法时，会处于假死状态
            using (Process p = new Process())
            {
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.UseShellExecute = false; //是否使用操作系统shell启动
                p.StartInfo.RedirectStandardInput = true; //接受来自调用程序的输入信息
                p.StartInfo.RedirectStandardOutput = true; //由调用程序获取输出信息
                p.StartInfo.RedirectStandardError = true; //重定向标准错误输出
                p.StartInfo.CreateNoWindow = false; //不显示程序窗口


                p.StartInfo.StandardOutputEncoding = Encoding.Default;
                p.StartInfo.StandardErrorEncoding = Encoding.Default;

                p.Start();
                p.StandardInput.WriteLine(cmd);
                p.StandardInput.AutoFlush = true;
                // 读取输出结果
                var output = p.StandardOutput.ReadToEnd();
                // 读取错误信息（如果有）
                string error = p.StandardError.ReadToEnd();
                p.WaitForExit();//等待程序执行完退出进程

                if (!string.IsNullOrEmpty(error))
                {
                    output += "\nError:\n" + error;
                }


                p.Close();

                Debug.Log(output);
            }
        }
        
    }
}
#endif