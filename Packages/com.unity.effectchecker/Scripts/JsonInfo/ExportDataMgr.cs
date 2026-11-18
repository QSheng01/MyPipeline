#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;
using System;
using System.IO;
using UnityEditor;

namespace SY.EffectChecker
{
    public class ExportDataMgr
    {
        public EffectInfo effectInfo;
        static string m_app_id, m_python_path, m_remark;
        static bool m_debug = false;
        static string json_path = "";

        public void SaveDataToJson(string workDirectory)
        {
            string createDataTime = DateTime.Now.ToString("yyyyMMddHHmm");
            string effectCheckInfoPath = ExportDataConfig.GetFile(workDirectory, string.Format("{0}-{1}", ExportDataConfig.effectCheckReport, createDataTime));
            if (effectInfo != null)
            {
                json_path = effectCheckInfoPath;
                WriteJsonFile(effectCheckInfoPath, effectInfo);
                Debug.Log("Json文件导出路径:" + effectCheckInfoPath);
                AssetDatabase.Refresh();


                //在这里做上报


            }
        }

        public void WriteJsonFile(string path, object jsonInfo)
        {
            string saveJsonStr = JsonMapper.ToJson(jsonInfo);
            StreamWriter sw = new StreamWriter(path);
            sw.Write(saveJsonStr);
            sw.Close();
        }


        // 记录上报信息
        public static void ReportWeb(string app_id, string python_path, string remark, bool is_debug)
        {
            if (json_path == "")
            {
                Debug.Log("写入json异常");
                return;
            }

            m_app_id = app_id;
            m_python_path = python_path;
            m_remark = remark;
            m_debug = is_debug;
            Call_python.RunPythonScript(json_path.Replace(@"\", @"/"), int.Parse(m_app_id), m_debug, m_python_path, m_remark);
        }
    }
}
#endif