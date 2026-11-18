
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


namespace SYArtToolkit
{

    public class TextureBase : ToolInterface
    {


        public override void InterfaceShow(bool noBool, Rect noRect) { }

        protected string Save(string folderPath, string name, Texture2D target, ToFormat format = ToFormat.Png)
        {
            string path = EditorUtility.SaveFilePanel("Save Render Image", folderPath, name, format == ToFormat.Tga ? "tga" : "png");
            if (path == "") return "";
            Debug.Log(path);
            File.WriteAllBytes(path, format == ToFormat.Tga ? target.EncodeToTGA() : target.EncodeToPNG());
            AssetDatabase.Refresh();

            Submit.ArtToolkitData.Instance.AddTexture(1);
            return path;
        }

        protected string Save(string folderPath, string name, Texture2D target, string formalString)
        {
            ToFormat format = formalString.Substring(formalString.Length - 4, 4) == ".tga"
                ? ToFormat.Tga
                : ToFormat.Png;

            string path = EditorUtility.SaveFilePanel("Save Render Image", folderPath, name, format == ToFormat.Tga ? "tga" : "png");
            if (path == "") return "";

            File.WriteAllBytes(path, format == ToFormat.Tga ? target.EncodeToTGA() : target.EncodeToPNG());
            AssetDatabase.Refresh();
            Submit.ArtToolkitData.Instance.AddTexture(1);
            return path;
        }

        protected string Save(string absolutePath, Texture2D target)
        {
            ToFormat format = absolutePath.Substring(absolutePath.Length - 4, 4) == ".tga"
                ? ToFormat.Tga
                : ToFormat.Png;

            File.WriteAllBytes(absolutePath, format == ToFormat.Tga ? target.EncodeToTGA() : target.EncodeToPNG());
            AssetDatabase.Refresh();
            Submit.ArtToolkitData.Instance.AddTexture(1);
            return absolutePath;
        }

        protected List<string> Save(string absoluteFolderPath, string name, List<Texture2D> targets, ToFormat format = ToFormat.Png)
        {
            if (string.IsNullOrEmpty(absoluteFolderPath)) return new List<string>();
            if (string.IsNullOrEmpty(name)) name = "outTexNameIsEmpty";
            List<string> res = new List<string>();
            int i = 0;
            foreach (var target in targets)
            {
                if (target == null) continue;
                string path = absoluteFolderPath + "/" + name + "_" + i++.ToString() + ".png";
                File.WriteAllBytes(path, target.EncodeToPNG());
                res.Add(path);
            }
            Submit.ArtToolkitData.Instance.AddTexture(targets.Count);
            AssetDatabase.Refresh();
            return res;
        }


        protected static string GetTextureFolder(Texture2D tex)
        {
            if (tex == null) return "";
            string path = AssetDatabase.GetAssetPath(tex);
            int i = path.Length - 1;
            for (; i >= 0; --i)
            {
                if (path[i] == '/') break;
            }

            return path.Substring(0, i);
        }

        protected static string AbsoluteToRelativelyPath(string path)
        {

            return path.Replace(Application.dataPath.Substring(0, Application.dataPath.Length - 6), "");
        }

        protected static string RelativelyToAbsolutePath(string path)
        {
            return Application.dataPath.Substring(0, Application.dataPath.Length - 6) + path;
        }


        //只接受相对路径
        protected void TextureImporterSet(string path, bool mipmapEnable = false, bool alphaIsTransparency = false)
        {
            TextureImporter texImporter = AssetImporter.GetAtPath(path) as TextureImporter;//
            if (texImporter == null)
            {
                Debug.Log(path);
                return;
            }
            texImporter.mipmapEnabled = mipmapEnable;
            texImporter.alphaIsTransparency = alphaIsTransparency;
            AssetDatabase.ImportAsset(path);
            AssetDatabase.Refresh();

        }

        protected void TextureImporterSet(List<string> absolutePaths, bool mipmapEnable = false,
            bool alphaIsTransparency = false)
        {
            foreach (var absolutePath in absolutePaths)
            {
                string path = AbsoluteToRelativelyPath(absolutePath);
                TextureImporter texImporter = AssetImporter.GetAtPath(path) as TextureImporter; //
                if (texImporter == null)
                {
                    Debug.LogAssertion(path + "导入失败");
                    return;
                }
                texImporter.mipmapEnabled = mipmapEnable;
                texImporter.alphaIsTransparency = alphaIsTransparency;
                AssetDatabase.ImportAsset(path);
            }
            AssetDatabase.Refresh();

        }

        //有路径的直接快速替换
        //文件夹+名字保存
        //文件夹+名字 mipmap修改保存
        // List<Texture> 保存
        //List<Texture> 保存和mipmap设置
        //
        //解压
        //解压并且修改alpha
        //

        protected static Texture2D DeCompress(Texture2D source)
        {
            if (source == null) return null;
            RenderTexture renderTex = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
               RenderTextureFormat.Default);
            string path = AssetDatabase.GetAssetPath(source);
            TextureImporter texImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            if (texImporter != null && (texImporter.textureType != TextureImporterType.Default || texImporter.alphaIsTransparency))
            {
                bool alphaIsTransparency = texImporter.alphaIsTransparency;
                UnityEditor.TextureImporterType tempType = texImporter.textureType;
                texImporter.alphaIsTransparency = false;
                texImporter.textureType = TextureImporterType.Default;
                AssetDatabase.ImportAsset(path);
                AssetDatabase.Refresh();
                Texture2D newSource = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Graphics.Blit(newSource, renderTex);
                texImporter.alphaIsTransparency = alphaIsTransparency;
                texImporter.textureType = tempType;
                AssetDatabase.ImportAsset(path);
                AssetDatabase.Refresh();
            }
            else
            {
                Graphics.Blit(source, renderTex);

            }
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(source.width, source.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            return readableText;
        }


        //protected Texture2D DeCompressAndNoTransparency(Texture2D source)
        //{

        //    if (source == null) return null;
        //    RenderTexture renderTex = RenderTexture.GetTemporary(
        //        source.width,
        //        source.height,
        //        0,
        //        RenderTextureFormat.Default,
        //        RenderTextureReadWrite.Default);

        //    if (source.alphaIsTransparency)
        //    {
        //        string path = AssetDatabase.GetAssetPath(source);
        //        TextureImporter texImporter = AssetImporter.GetAtPath(path) as TextureImporter;
        //        if(texImporter==null) Debug.LogError("解压出错");
        //        texImporter.alphaIsTransparency =false;
        //        AssetDatabase.ImportAsset(path);
        //        AssetDatabase.Refresh();
        //        Texture2D newSource = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        //        Graphics.Blit(newSource, renderTex);
        //        texImporter.alphaIsTransparency =true;
        //        AssetDatabase.ImportAsset(path);
        //        AssetDatabase.Refresh();
        //    }
        //    else
        //    {
        //        Graphics.Blit(source, renderTex);
        //    }

        //    RenderTexture previous = RenderTexture.active;
        //    RenderTexture.active = renderTex;
        //    Texture2D readableText = new Texture2D(source.width, source.height);
        //    readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
        //    readableText.Apply();
        //    RenderTexture.active = previous;
        //    RenderTexture.ReleaseTemporary(renderTex);
        //    readableText.name = source.name;
        //    return readableText;
        //}

        protected void SetColorSpaceValue()
        {
            if (PlayerSettings.colorSpace == ColorSpace.Linear) //判断色彩空间
            {
                Shader.SetGlobalFloat("_ColorSpaceValue", 1 / 2.2f);
            }
            else
            {
                Shader.SetGlobalFloat("_ColorSpaceValue", 1);
            }
        }

        protected void SetMipMapAndTransparency(ref bool mipmapEnable, ref bool alphaIsTransparency)
        {
            GUILayout.BeginHorizontal();
            mipmapEnable = GUILayout.Toggle(mipmapEnable, "是否生成mipmap");
            alphaIsTransparency = GUILayout.Toggle(alphaIsTransparency, "是否透明显示");
            GUILayout.EndHorizontal();
        }





        public enum MouseLocationStatus
        {
            Else = 0, Left = 1, Right = 2, Top = 4, LeftTop = 5, RightTop = 6, Bottom = 8, LeftBottom = 9, RightBottom = 10, Drag = 16
        }


    }
    public enum ToFormat
    {
        Png,
        Jpg,
        Tga
    }

    public enum Channel
    {
        R = 0,
        G,
        B,
        A
    }


}
