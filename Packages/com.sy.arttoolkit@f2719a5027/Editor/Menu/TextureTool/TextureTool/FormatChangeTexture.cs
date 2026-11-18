using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SYArtToolkit
{

    public class FormatChangeTexture : TextureBase
    {
        private Texture2D tex2D = null;
        private GUILayoutOption[] texLayout = { GUILayout.Height(256), GUILayout.Width(256) };
        private ToFormat format = ToFormat.Png;


        private Vector2 scroll = new Vector2(0, 0);
        public override void InterfaceShow(bool atThisWindow, Rect noRect)
        {
            scroll = EditorGUILayout.BeginScrollView(scroll, false, false);
            GUILayout.BeginHorizontal();
            {
                tex2D = EditorGUILayout.ObjectField(tex2D, typeof(Texture2D), false, texLayout) as Texture2D;
                GUILayout.BeginVertical();
                {
                    GUILayout.Label("推荐使用右键->美术工具箱->格式转换");
                    GUILayout.Space(100);
                    format = (ToFormat)EditorGUILayout.EnumPopup("转换格式", (ToFormat)format);
                    if (GUILayout.Button("转换"))
                    {
                        if (tex2D == null) return;
                        string sourcePath = Application.dataPath.Substring(0, Application.dataPath.Length - 6) +
                                            AssetDatabase.GetAssetPath(tex2D);
                        string suffix = GetSuffix(format);
                        string targetPath = sourcePath.Substring(0, sourcePath.Length - 3) + suffix;
                        string oldAssetsPth = AssetDatabase.GetAssetPath(tex2D);
                        string newAssetsPath = oldAssetsPth.Substring(0, oldAssetsPth.Length - 3) + suffix;
                        FormatChangeDealSingle(DeCompress(tex2D), format, sourcePath, targetPath);
                        AssetDatabase.Refresh();
                        tex2D = AssetDatabase.LoadAssetAtPath<Texture2D>(newAssetsPath);

                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();

        }

        [MenuItem("Assets/美术工具箱/转换格式/tga", true)]
        [MenuItem("Assets/美术工具箱/转换格式/png", true)]
        [MenuItem("Assets/美术工具箱/转换格式/jpg", true)]
        private static bool DeleteValidate()
        {
            if (Selection.objects.Length > 0)
            {
                bool x = true;
                Object[] objs = Selection.objects;
                if (objs == null) return false;
                foreach (var obj in objs)
                {
                    if (obj as Texture2D == null) x = false;
                }

                return x;
            }

            return false;
        }


        [MenuItem("Assets/美术工具箱/转换格式/png", false)]
        private static void MyToolDeletePng()
        {
            MyToolDelete(ToFormat.Png);
        }


        [MenuItem("Assets/美术工具箱/转换格式/jpg", false)]
        private static void MyToolDeleteJpg()
        {
            MyToolDelete(ToFormat.Jpg);
        }


        [MenuItem("Assets/美术工具箱/转换格式/tga", false)]
        private static void MyToolDeleteTga()
        {
            MyToolDelete(ToFormat.Tga);
        }


        private static void MyToolDelete(ToFormat f)
        {
            List<Texture2D> tl = GetTexList(Selection.objects);
            List<Pair> absolutePath = DeCompressTextures(tl);

            foreach (var pair in absolutePath)
            {
                string sourcePath = pair.path;
                string suffix = GetSuffix(f);
                string targetPath = sourcePath.Substring(0, sourcePath.Length - 3) + suffix;
                Texture2D tex = pair.tex;
                FormatChangeDealSingle(tex, f, sourcePath, targetPath);

            }

            AssetDatabase.Refresh();
        }


        static private List<Pair> DeCompressTextures(List<Texture2D> tl)
        {
            List<Pair> res = new List<Pair>();
            foreach (var tex in tl)
            {
                string path = RelativelyToAbsolutePath(AssetDatabase.GetAssetPath(tex));
                Pair temPair = new Pair(DeCompress(tex), path);
                res.Add(temPair);
            }

            return res;
        }


        protected class Pair
        {
            public Texture2D tex;
            public string path;

            public Pair(Texture2D t = null, string p = null)
            {
                tex = t;
                path = p;
            }
        }



        private static void FormatChangeDealSingle(Texture2D tex, ToFormat f, string sourcePath, string targetPath)
        {
            ModifyFile(sourcePath, targetPath, tex, f);
        }


        private static string GetSuffix(ToFormat f)
        {
            if (f == ToFormat.Png) return "png";
            else if (f == ToFormat.Jpg) return "jpg";
            else return "tga";
        }

        private static void ModifyFile(string sourcePath, string targetPath, Texture2D tex, ToFormat f)
        {

            if (File.Exists(targetPath) || File.Exists(targetPath + ".meta"))
            {
                Debug.Log(tex + "或.meta文件已经存在-转化失败");
                return;
            }

            File.Copy(sourcePath + ".meta", targetPath + ".meta");
            File.Delete(sourcePath);
            File.Delete(sourcePath + ".meta");
            byte[] bytes = GetBytes(f, tex);
            File.WriteAllBytes(targetPath, bytes);
            Submit.ArtToolkitData.Instance.AddTexture(1);
            Debug.Log(tex.name + "-转化成功");
        }

        private static byte[] GetBytes(ToFormat f, Texture2D tex)
        {
            if (f == ToFormat.Png) return tex.EncodeToPNG();
            else if (f == ToFormat.Jpg) return tex.EncodeToJPG();
            else return tex.EncodeToTGA();
        }

        private static List<Texture2D> GetTexList(Object[] objs)
        {
            List<Texture2D> tl = new List<Texture2D>();
            foreach (var obj in objs)
            {
                var tex = obj as Texture2D;
                if (tex != null) tl.Add(tex);
            }

            return tl;
        }

    }
}