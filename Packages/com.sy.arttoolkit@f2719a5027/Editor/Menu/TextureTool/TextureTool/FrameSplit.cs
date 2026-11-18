
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace SYArtToolkit
{

    public class FrameSplit : TextureBase
    {

        private Texture2D tex = null;
        private GUILayoutOption[] texLayout = { GUILayout.Height(256), GUILayout.Width(256) };
        private string outTextureFilePath = "";
        private string outTextureFileName = "";
        private int outX = 0, outY = 0;
        private Vector2 scroll;
        private bool mipMapEnable = false;
        private bool alphaIsTransparency = true;
        public override void InterfaceShow(bool atThisWindow, Rect noRect)
        {
            scroll = GUILayout.BeginScrollView(scroll, false, false);
            GUILayout.BeginHorizontal();
            GUILayout.BeginHorizontal();
            {
                tex = (Texture2D)EditorGUILayout.ObjectField(tex, typeof(Texture2D), false, texLayout);
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Space(50);

            //GUILayout.BeginHorizontal();
            {
                GUILayout.Label("选择拆分行数:");
                outX = EditorGUILayout.IntSlider(outX, 1, 6);

                GUILayout.Label("选择拆分列数:");
                outY = EditorGUILayout.IntSlider(outY, 1, 6);

            }
            //GUILayout.EndHorizontal();
            GUILayout.Label("请选择可以被整除的行和列，例如32X32像素不要选择拆分3X3");
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("路径:", GUILayout.Width(30f));
                GUILayout.Label(outTextureFilePath, GUILayout.Width(300));
                if (GUILayout.Button("浏览", GUILayout.Width(50f)))
                {
                    outTextureFilePath = EditorUtility.OpenFolderPanel("选择路径", outTextureFilePath, "");
                    GUIUtility.ExitGUI();
                }
                else if (outTextureFilePath == "") outTextureFilePath = Application.dataPath;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            SetMipMapAndTransparency(ref mipMapEnable, ref alphaIsTransparency);
            if (GUILayout.Button("生成"))
            {
                if (tex == null)
                {
                    Debug.Log("输入图片不存在");
                    return;
                }
                if (outTextureFileName == "") outTextureFileName = tex.name;

                int width = tex.width / outX;
                int height = tex.height / outY;

                Texture2D tempTex = DeCompress(tex);
                int row = outX;
                int col = outY;


                List<Texture2D> res = new List<Texture2D>();
                for (int j = col - 1; j >= 0; --j)
                {
                    for (int i = 0; i < row; ++i)
                    {
                        Texture2D result = new Texture2D(width, height);
                        Color[] colors = new Color[width * height];
                        for (int h = 0; h < height; ++h)
                        {
                            for (int w = 0; w < width; ++w)
                            {
                                float u = (float)(i * width + w) / tempTex.width;
                                float v = (float)(j * height + h) / tempTex.height;

                                colors[h * width + w] = tempTex.GetPixelBilinear(u, v);
                            }
                        }
                        result.SetPixels(colors);
                        result.Apply();
                        res.Add(result);
                    }
                }

                List<string> lt = Save(outTextureFilePath, outTextureFileName, res);
                TextureImporterSet(lt, mipMapEnable, alphaIsTransparency);
                GUIUtility.ExitGUI();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
        }




    }




}