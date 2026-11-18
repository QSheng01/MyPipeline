#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SY.EffectChecker
{
    public static class ExportDataConfig
    {
        public static readonly string extension = "json";
        public static readonly string effectCheckReport = "effectCheckReport";
        public static readonly string pythonPathKey = "pythonPathKey";
        public static readonly string sceneKey = "sceneKey";
        public static readonly string appIdKey = "appIdKey";
        public static readonly string version = "1.2";

        public static string GetFile(string directory, string fileName)
        {
            return string.Format("{0}/{1}.{2}", directory, fileName, extension);
        }
    }
}
#endif