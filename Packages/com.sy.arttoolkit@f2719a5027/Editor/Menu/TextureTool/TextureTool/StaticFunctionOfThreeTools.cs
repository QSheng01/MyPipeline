using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SYArtToolkit
{

    public class TextureFunctionTSA
    {

        private class Pair
        {
            public Texture2D tex;
            public string path;

            public Pair(Texture2D t = null, string p = null)
            {
                tex = t;
                path = p;
            }
        }



        [MenuItem("Assets/美术工具箱/黑底/去黑底", true)]
        [MenuItem("Assets/美术工具箱/黑底/加黑底", true)]
        [MenuItem("Assets/美术工具箱/Alpha设置/0", true)]
        [MenuItem("Assets/美术工具箱/Alpha设置/1", true)]
        [MenuItem("Assets/美术工具箱/旋转/顺时针旋转90度", true)]
        [MenuItem("Assets/美术工具箱/旋转/逆时针旋转90度", true)]
        [MenuItem("Assets/美术工具箱/旋转/左右翻转", true)]
        [MenuItem("Assets/美术工具箱/旋转/上下翻转", true)]
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


        [MenuItem("Assets/美术工具箱/黑底/去黑底")]
        private static void DeleteBlack()
        {
            List<Texture2D> tl = GetTexList(Selection.objects);
            string rootPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
            List<Pair> absolutePath = DeCompressTextures(tl, rootPath);

            foreach (var P in absolutePath)
            {
                Texture2D tex = P.tex;
                TextureImporter texImporter = AssetImporter.GetAtPath(P.path.Replace(rootPath, "")) as TextureImporter;
                if (texImporter.alphaIsTransparency)
                {
                    texImporter.alphaIsTransparency = false;
                    AssetDatabase.ImportAsset(P.path.Replace(rootPath, ""));
                    AssetDatabase.Refresh();
                    tex = AssetDatabase.LoadAssetAtPath<Texture2D>(P.path.Replace(rootPath, ""));
                    tex = DeCompress(tex);
                }

                DeleteBackSingle(tex, P.path);

                texImporter.alphaIsTransparency = true;
                AssetDatabase.ImportAsset(P.path.Replace(rootPath, ""));
                AssetDatabase.Refresh();
            }

            AssetDatabase.Refresh();
        }

        public static void DeleteBackSingle(Texture2D tex, string path)
        {
            if (tex == null) return;
            Color[] colors = colors = tex.GetPixels();
            for (int i = 0; i < tex.width * tex.height; i++)
            {
                float x = Mathf.Max(colors[i].r, colors[i].g, colors[i].b);
                colors[i].a = x;
            }
            tex.SetPixels(colors);
            var kz = System.IO.Path.GetExtension(path).ToLower();
            byte[] bytes;
            switch (kz)
            {
                case ".tga":
                    bytes = tex.EncodeToTGA(); break;
                case ".jpg":
                    bytes = tex.EncodeToJPG(); break;
                default:
                    bytes = tex.EncodeToPNG(); break;
            }
            File.WriteAllBytes(path, bytes);
            Submit.ArtToolkitData.Instance.AddTexture(1);

        }

        [MenuItem("Assets/美术工具箱/黑底/加黑底")]
        private static void AddBlack()
        {
            List<Texture2D> tl = GetTexList(Selection.objects);
            string rootPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
            List<Pair> absolutePath = DeCompressTextures(tl, rootPath);
            foreach (var P in absolutePath)
            {

                Texture2D tex = P.tex;
                int w = tex.width;
                int h = tex.height;


                TextureImporter texImporter = AssetImporter.GetAtPath(P.path.Replace(rootPath, "")) as TextureImporter;
                if (texImporter.alphaIsTransparency)
                {
                    texImporter.alphaIsTransparency = false;
                    AssetDatabase.ImportAsset(P.path.Replace(rootPath, ""));
                    AssetDatabase.Refresh();
                    tex = AssetDatabase.LoadAssetAtPath<Texture2D>(P.path.Replace(rootPath, ""));
                    tex = DeCompress(tex);
                }

                AddBackSingle(tex, P.path);
            }

            AssetDatabase.Refresh();
        }

        public static void AddBackSingle(Texture2D tex, string path)
        {

            if (tex == null) return;
            Color[] colors = colors = tex.GetPixels();
            for (int i = 0; i < tex.width * tex.height; i++)
            {
                colors[i].a = 1;
            }

            tex.SetPixels(colors);
            var kz = System.IO.Path.GetExtension(path).ToLower();
            byte[] bytes;
            switch (kz)
            {
                case ".tga":
                    bytes = tex.EncodeToTGA(); break;
                case ".jpg":
                    bytes = tex.EncodeToJPG(); break;
                default:
                    bytes = tex.EncodeToPNG(); break;
            }
            File.WriteAllBytes(path, bytes);
            Submit.ArtToolkitData.Instance.AddTexture(1);
        }

        [MenuItem("Assets/美术工具箱/Alpha设置/0")]
        private static void AlphaSet0()
        {
            List<Texture2D> tl = GetTexList(Selection.objects);
            string rootPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
            List<Pair> absolutePath = DeCompressTextures(tl, rootPath);
            foreach (var P in absolutePath)
            {

                Texture2D tex = P.tex;


                TextureImporter texImporter = AssetImporter.GetAtPath(P.path.Replace(rootPath, "")) as TextureImporter;
                if (texImporter.alphaIsTransparency)
                {
                    texImporter.alphaIsTransparency = false;
                    AssetDatabase.ImportAsset(P.path.Replace(rootPath, ""));
                    AssetDatabase.Refresh();
                    tex = AssetDatabase.LoadAssetAtPath<Texture2D>(P.path.Replace(rootPath, ""));
                    tex = DeCompress(tex);
                }


                AlphaSetSingle(tex, P.path, Channel.A, 0);
                texImporter.alphaIsTransparency = true;
                AssetDatabase.ImportAsset(P.path.Replace(rootPath, ""));
                AssetDatabase.Refresh();
            }

            AssetDatabase.Refresh();
        }

        public static void AlphaSetSingle(Texture2D tex, string path, Channel channel, float alpha)
        {
            if (tex == null) return;
            if (alpha > 1) alpha = 1;
            else if (alpha < 0) alpha = 0;
            Color[] colors = colors = tex.GetPixels();
            switch (channel)
            {
                case Channel.A:
                    for (int i = 0; i < tex.width * tex.height; i++)
                    {
                        colors[i].a = alpha;
                    }

                    break;
                case Channel.B:
                    for (int i = 0; i < tex.width * tex.height; i++)
                    {
                        colors[i].b = alpha;
                    }

                    break;
                case Channel.G:
                    for (int i = 0; i < tex.width * tex.height; i++)
                    {
                        colors[i].g = alpha;
                    }

                    break;
                case Channel.R:
                    for (int i = 0; i < tex.width * tex.height; i++)
                    {
                        colors[i].r = alpha;
                    }

                    break;
            }

            tex.SetPixels(colors);
            var kz = System.IO.Path.GetExtension(path).ToLower();
            byte[] bytes;
            switch (kz)
            {
                case ".tga":
                    bytes = tex.EncodeToTGA(); break;
                case ".jpg":
                    bytes = tex.EncodeToJPG(); break;
                default:
                    bytes = tex.EncodeToPNG(); break;
            }
            File.WriteAllBytes(path, bytes);
            Submit.ArtToolkitData.Instance.AddTexture(1);
        }


        [MenuItem("Assets/美术工具箱/Alpha设置/1")]
        private static void AlphaSet1()
        {
            List<Texture2D> tl = GetTexList(Selection.objects);
            string rootPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
            List<Pair> absolutePath = DeCompressTextures(tl, rootPath);
            foreach (var P in absolutePath)
            {

                Texture2D tex = P.tex;
                TextureImporter texImporter = AssetImporter.GetAtPath(P.path.Replace(rootPath, "")) as TextureImporter;
                if (texImporter.alphaIsTransparency == true)
                {
                    texImporter.alphaIsTransparency = false;
                    AssetDatabase.ImportAsset(P.path.Replace(rootPath, ""));
                    AssetDatabase.Refresh();
                    tex = AssetDatabase.LoadAssetAtPath<Texture2D>(P.path.Replace(rootPath, ""));
                    tex = DeCompress(tex);
                }


                AlphaSetSingle(tex, P.path, Channel.A, 1);

                texImporter.alphaIsTransparency = true;
                AssetDatabase.ImportAsset(P.path.Replace(rootPath, ""));
                AssetDatabase.Refresh();
            }

            AssetDatabase.Refresh();
        }


        [MenuItem("Assets/美术工具箱/旋转/左右翻转")]
        private static void LeftRightRotation()
        {
            List<Texture2D> tl = GetTexList(Selection.objects);
            string rootPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
            List<Pair> absolutePath = DeCompressTextures(tl, rootPath);
            foreach (var P in absolutePath)
            {
                Texture2D tex = P.tex;
                int w = tex.width;
                int h = tex.height;
                bool setalpha = false;
                TextureImporter texImporter = AssetImporter.GetAtPath(P.path.Replace(rootPath, "")) as TextureImporter;
                if (texImporter.alphaIsTransparency)
                {
                    texImporter.alphaIsTransparency = false;
                    AssetDatabase.ImportAsset(P.path.Replace(rootPath, ""));
                    AssetDatabase.Refresh();
                    tex = AssetDatabase.LoadAssetAtPath<Texture2D>(P.path.Replace(rootPath, ""));
                    tex = DeCompress(tex);
                    setalpha = true;
                }

                LeftRightRotationSingle(tex, P.path);

                if (setalpha)
                {
                    texImporter.alphaIsTransparency = true;
                    AssetDatabase.ImportAsset(P.path.Replace(rootPath, ""));
                    AssetDatabase.Refresh();
                }
            }

            AssetDatabase.Refresh();
        }

        public static void LeftRightRotationSingle(Texture2D tex, string path)
        {
            if (tex == null) return;
            Color[] colors = colors = tex.GetPixels();
            int w = tex.width;
            int h = tex.height;
            Texture2D textemp = new Texture2D(w, h, tex.format, false);
            for (int j = 0; j < h; j++)
            {
                for (int i = 0; i < w; i++)
                {
                    textemp.SetPixel(i, j, tex.GetPixel(w - 1 - i, j));
                }
            }

            var kz = System.IO.Path.GetExtension(path).ToLower();
            byte[] bytes;
            switch (kz)
            {
                case ".tga":
                    bytes = textemp.EncodeToTGA(); break;
                case ".jpg":
                    bytes = textemp.EncodeToJPG(); break;
                default:
                    bytes = textemp.EncodeToPNG(); break;
            }
            File.WriteAllBytes(path, bytes);
            Submit.ArtToolkitData.Instance.AddTexture(1);
        }


        [MenuItem("Assets/美术工具箱/旋转/上下翻转")]
        private static void UpDownRotation()
        {
            List<Texture2D> tl = GetTexList(Selection.objects);
            string rootPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
            List<Pair> absolutePath = DeCompressTextures(tl, rootPath);
            foreach (var P in absolutePath)
            {
                Texture2D tex = P.tex;
                int w = tex.width;
                int h = tex.height;
                bool setalpha = false;
                TextureImporter texImporter = AssetImporter.GetAtPath(P.path.Replace(rootPath, "")) as TextureImporter;
                if (texImporter.alphaIsTransparency)
                {
                    texImporter.alphaIsTransparency = false;
                    AssetDatabase.ImportAsset(P.path.Replace(rootPath, ""));
                    AssetDatabase.Refresh();
                    tex = AssetDatabase.LoadAssetAtPath<Texture2D>(P.path.Replace(rootPath, ""));
                    tex = DeCompress(tex);
                    setalpha = true;
                }

                UpDownRotationSingle(tex, P.path);
                if (setalpha)
                {
                    texImporter.alphaIsTransparency = true;
                    AssetDatabase.ImportAsset(P.path.Replace(rootPath, ""));
                    AssetDatabase.Refresh();
                }
            }

            AssetDatabase.Refresh();
        }


        public static void UpDownRotationSingle(Texture2D tex, string path)
        {
            if (tex == null) return;
            int w = tex.width;
            int h = tex.height;
            Texture2D textemp = new Texture2D(w, h, tex.format, false);
            for (int j = 0; j < h; j++)
            {
                for (int i = 0; i < w; i++)
                {
                    textemp.SetPixel(i, j, tex.GetPixel(i, h - j - 1));
                }
            }

            var kz = System.IO.Path.GetExtension(path).ToLower();
            byte[] bytes;
            switch (kz)
            {
                case ".tga":
                    bytes = textemp.EncodeToTGA(); break;
                case ".jpg":
                    bytes = textemp.EncodeToJPG(); break;
                default:
                    bytes = textemp.EncodeToPNG(); break;
            }
            File.WriteAllBytes(path, bytes);
            Submit.ArtToolkitData.Instance.AddTexture(1);
        }


        [MenuItem("Assets/美术工具箱/旋转/逆时针旋转90度")]
        private static void CounterClockwiseRotation()
        {
            List<Texture2D> tl = GetTexList(Selection.objects);
            string rootPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
            List<Pair> absolutePath = DeCompressTextures(tl, rootPath);
            foreach (var P in absolutePath)
            {
                Texture2D tex = P.tex;
                int w = tex.width;
                int h = tex.height;
                bool setalpha = false;
                TextureImporter texImporter = AssetImporter.GetAtPath(P.path.Replace(rootPath, "")) as TextureImporter;
                if (texImporter.alphaIsTransparency)
                {
                    texImporter.alphaIsTransparency = false;
                    AssetDatabase.ImportAsset(P.path.Replace(rootPath, ""));
                    AssetDatabase.Refresh();
                    tex = AssetDatabase.LoadAssetAtPath<Texture2D>(P.path.Replace(rootPath, ""));
                    tex = DeCompress(tex);
                    setalpha = true;
                }

                CounterClockwiseRotationSingle(tex, P.path);

                if (setalpha)
                {
                    texImporter.alphaIsTransparency = true;
                    AssetDatabase.ImportAsset(P.path.Replace(rootPath, ""));
                    AssetDatabase.Refresh();
                }
            }

            AssetDatabase.Refresh();
        }

        public static void CounterClockwiseRotationSingle(Texture2D tex, string path)
        {
            if (tex == null) return;
            int w = tex.width;
            int h = tex.height;
            Color[] colors = colors = tex.GetPixels();
            Texture2D textemp = new Texture2D(h, w, tex.format, false);
            for (int j = 0; j < w; j++)
            {
                for (int i = 0; i < h; i++)
                {
                    textemp.SetPixel(i, j, tex.GetPixel(j, h - 1 - i));
                }
            }

            var kz = System.IO.Path.GetExtension(path).ToLower();
            byte[] bytes;
            switch (kz)
            {
                case ".tga":
                    bytes = textemp.EncodeToTGA(); break;
                case ".jpg":
                    bytes = textemp.EncodeToJPG(); break;
                default:
                    bytes = textemp.EncodeToPNG(); break;
            }
            File.WriteAllBytes(path, bytes);
            Submit.ArtToolkitData.Instance.AddTexture(1);
        }




        [MenuItem("Assets/美术工具箱/旋转/顺时针旋转90度")]
        private static void ClockwiseRotation()
        {
            List<Texture2D> tl = GetTexList(Selection.objects);
            string rootPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
            List<Pair> absolutePath = DeCompressTextures(tl, rootPath);
            foreach (var P in absolutePath)
            {
                Texture2D tex = P.tex;
                int w = tex.width;
                int h = tex.height;

                bool setalpha = false;
                TextureImporter texImporter = AssetImporter.GetAtPath(P.path.Replace(rootPath, "")) as TextureImporter;
                if (texImporter.alphaIsTransparency)
                {
                    texImporter.alphaIsTransparency = false;
                    AssetDatabase.ImportAsset(P.path.Replace(rootPath, ""));
                    AssetDatabase.Refresh();
                    tex = AssetDatabase.LoadAssetAtPath<Texture2D>(P.path.Replace(rootPath, ""));
                    tex = DeCompress(tex);
                    setalpha = true;
                }

                ClockwiseRotationSingle(tex, P.path);

                if (setalpha)
                {
                    texImporter.alphaIsTransparency = true;
                    AssetDatabase.ImportAsset(P.path.Replace(rootPath, ""));
                    AssetDatabase.Refresh();
                }
            }

            AssetDatabase.Refresh();
        }


        public static void ClockwiseRotationSingle(Texture2D tex, string path)
        {
            if (tex == null) return;
            int w = tex.width;
            int h = tex.height;
            Color[] colors = colors = tex.GetPixels();
            Texture2D textemp = new Texture2D(h, w, tex.format, false);
            for (int j = 0; j < w; j++)
            {
                for (int i = 0; i < h; i++)
                {
                    textemp.SetPixel(i, j, tex.GetPixel(w - 1 - j, i));
                }
            }

            var kz = System.IO.Path.GetExtension(path).ToLower();
            byte[] bytes;
            switch (kz)
            {
                case ".tga":
                    bytes = textemp.EncodeToTGA(); break;
                case ".jpg":
                    bytes = textemp.EncodeToJPG(); break;
                default:
                    bytes = textemp.EncodeToPNG(); break;
            }
            Submit.ArtToolkitData.Instance.AddTexture(1);
            File.WriteAllBytes(path, bytes);
        }

        private static List<Pair> DeCompressTextures(List<Texture2D> tl, string rootPath)
        {
            List<Pair> res = new List<Pair>();
            foreach (var tex in tl)
            {
                string path = AssetDatabase.GetAssetPath(tex);
                Pair temPair = new Pair(DeCompress(tex), rootPath + path);
                res.Add(temPair);
            }

            return res;
        }

        public static Texture2D DeCompress(Texture2D source)
        {

            if (source == null)
            {
                Debug.Log("输入贴图是空的");
                return new Texture2D(1, 1);
            }

            RenderTexture renderTex = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Default);

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(source.width, source.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0, false);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            readableText.name = source.name;
            return readableText;
        }

        private static List<Texture2D> GetTexList(Object[] objs)
        {
            List<Texture2D> tl = new List<Texture2D>();
            foreach (var obj in objs)
            {
                tl.Add(obj as Texture2D);

            }

            return tl;
        }
    }
}