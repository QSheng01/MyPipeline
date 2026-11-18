using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SYArtToolkit
{
    public class TextureCombine : TextureBase
    {
        public Texture2D backGroundtexture1;
        public Texture2D backGroundtexture2;
        private bool[] textureChannelmulAlpha;
        private Channel[] textureChannel;
        private int aboutChannel = 33825;
        private string outTextureName = "mergeChannel";
        private Texture2D[] textures;
        private Material mergeChannelMat;
        private Vector2Int resolution = new Vector2Int(256, 256);
        bool isAlpha = true;
        GUIStyle style = new GUIStyle();

        Texture2D preview = new Texture2D(360, 360);
        public TextureCombine()
        {
            backGroundtexture1 = Resources.Load<Texture2D>("底图1");
            backGroundtexture2 = Resources.Load<Texture2D>("底图2");
            mergeChannelMat = new Material(Shader.Find("Hidden/ArtToolkit/MergeChannel"));

            textures = new Texture2D[4];
            textureChannel = new Channel[4];
            for (int i = 0; i < 4; ++i)
            {
                textureChannel[i] = Channel.R;
            }

            textureChannelmulAlpha = new bool[4];
            for (int i = 0; i < 4; ++i)
            {
                textureChannelmulAlpha[i] = false;
            }
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 35;
            style.normal.textColor = Color.white;
        }

        private Vector2 scroll = new Vector2(0, 0);
        private Texture2D backPicture = new Texture2D(650, 540);

        public override void InterfaceShow(bool atThisWindow, Rect noRect)
        {
            scroll = EditorGUILayout.BeginScrollView(scroll, false, false);
            GUILayout.Label(backPicture);
            EditorGUI.DrawPreviewTexture(new Rect(0, 0, 780, 550), backGroundtexture1);
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginVertical(); //开始垂直布局，间隔30像素
            for (int i = 0; i < 4; ++i)
            {
                textures[i] =
                    EditorGUI.ObjectField(new Rect(5, 17 + i * 130, 150, 110), textures[i], typeof(Texture),
                        false) as Texture2D;
            }

            EditorGUILayout.EndVertical(); //结束垂直布局
            EditorGUILayout.EndHorizontal(); //结束水平布局
            if (EditorGUI.EndChangeCheck())
            {
                mergeChannelMat.SetTexture("_TextureR", textures[0]);
                mergeChannelMat.SetTexture("_TextureG", textures[1]);
                mergeChannelMat.SetTexture("_TextureB", textures[2]);
                mergeChannelMat.SetTexture("_TextureA", textures[3]);
            }


            GUI.Label(new Rect(230, 420, 50, 30), "输出像素:");
            resolution = EditorGUI.Vector2IntField(new Rect(280, 425, 120, 30), "", resolution); //显示图像大小输出，两个框
            GUI.Label(new Rect(400, 420, 30, 30), "名字:");
            outTextureName = EditorGUI.TextField(new Rect(430, 420, 120, 30), "", outTextureName); //名称选择模块

            isAlpha = GUI.Toggle(new Rect(560, 425, 50, 50), isAlpha, "alphaIsTransparency");

            EditorGUI.BeginChangeCheck();

            for (int i = 0; i < 4; ++i)
            {
                textureChannelmulAlpha[i] = GUI.Toggle(new Rect(110, 125 + 130 * i, 60, 15), textureChannelmulAlpha[i],
                    "乘Alpha");
                aboutChannel = textureChannelmulAlpha[i]
                    ? aboutChannel | (1 << (5 * i + 4))
                    : aboutChannel & (Int32.MaxValue - (1 << (5 * i + 4)));
            }

            for (int j = 0; j < 4; j++)
            {
                int startY = 15 + 131 * j;
                for (int i = 0; i < 4; ++i)
                {
                    if (GUI.Button(new Rect(165, startY + 28 * i, 25, 25), ShowButtonChannelSelect(i),
                            BaseShow.GetButtonStyle(textureChannel[j], Channel.R + i)))
                    {
                        textureChannel[j] = Channel.R + i;
                        aboutChannel = (aboutChannel & Int32.MaxValue - (15 << (j * 5))) | (1 << i << (5 * j));
                    }
                }
            }
            mergeChannelMat.SetInt("_AboutChannel", aboutChannel);
            Handles.Label(new Vector3(265, 10, 0), "贴图通道合并工具", style);

            RenderTexture rt = RenderTexture.GetTemporary(360, 360);
            RenderTexture prous = RenderTexture.active;
            RenderTexture.active = rt;
            Graphics.Blit(backGroundtexture2, mergeChannelMat);
            preview.ReadPixels(new Rect(0, 0, 360, 360), 0, 0);
            preview.Apply();
            RenderTexture.active = prous;
            RenderTexture.ReleaseTemporary(rt);
            GUI.Label(new Rect(225, 60, 360, 360), preview);

            if (GUI.Button(new Rect(350, 450, 100, 50), "保存")) //保存按钮
            {
                SaveCombine();
            }

            EditorGUILayout.EndScrollView();
        }

        void SaveCombine()
        {
            RenderTexture rt = RenderTexture.GetTemporary((int)resolution.x, (int)resolution.y);
            RenderTexture prous = RenderTexture.active;
            RenderTexture.active = rt;
            Graphics.Blit(backGroundtexture2, mergeChannelMat);
            Texture2D previewx = new Texture2D((int)resolution.x, (int)resolution.y);
            previewx.ReadPixels(new Rect(0, 0, (int)resolution.x, (int)resolution.y), 0, 0);
            previewx.Apply();
            RenderTexture.active = prous;
            RenderTexture.ReleaseTemporary(rt);
            string x = Save(Application.dataPath, outTextureName, previewx);
            TextureImporterSet(AbsoluteToRelativelyPath(x), false, isAlpha);
            Object.DestroyImmediate(previewx);
        }

        private string ShowButtonChannelSelect(int i)
        {
            if (i == 0) return "R";
            if (i == 1) return "G";
            if (i == 2) return "B";
            return "A";
        }

    }
}
