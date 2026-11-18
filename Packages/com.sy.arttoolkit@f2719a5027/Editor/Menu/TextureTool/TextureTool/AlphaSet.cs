
using UnityEditor;
using UnityEngine;

namespace SYArtToolkit
{

    public class AlphaSet : TextureBase
    {


        private Channel channel = Channel.A;
        private float alpha = 1;
        private Texture2D tex = null;
        private Texture2D lastTex = null;
        private Texture2D decompressTexture = null;
        private bool alphais = true;
        private bool mipmap = false;

        private Material material;
        private RenderTexture rt = null;
        private Texture2D previewTex = null;

        public AlphaSet()
        {
            material = new Material(Shader.Find("Hidden/ArtToolkit/ChannelSet"));
            previewTex = new Texture2D(256, 256);
        }

        private Vector2 asnkj = new Vector2(0, 0);
        public override void InterfaceShow(bool atThisWindow, Rect noRect)
        {
            asnkj = EditorGUILayout.BeginScrollView(asnkj, false, false);
            if (atThisWindow)
            {
                if (Event.current.type == EventType.DragUpdated)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                }
                else if (Event.current.type == EventType.DragExited)
                {
                    if (DragAndDrop.paths != null)
                    {
                        tex = (Texture2D)AssetDatabase.LoadAssetAtPath(DragAndDrop.paths[0], typeof(Texture2D));
                        if (tex != null) decompressTexture = DeCompress(tex);
                    }
                }
            }

            GUILayout.BeginHorizontal();

            GUILayout.Label(previewTex);
            if (tex && tex == lastTex)
            {
                SetMaterial();
                if (tex != null)
                {
                    RenderPreview();
                }
            }
            lastTex = tex;
            GUILayout.Space(20);
            GUILayout.BeginVertical();
            GUILayout.Space(50);
            GUILayout.Label("图片拖入左边框开始编辑");
            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("设置为0"))
            {
                alpha = 0;
            }
            if (GUILayout.Button("设置为1"))
            {
                alpha = 1;
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            channel = (Channel)EditorGUILayout.EnumPopup("选择通道", channel);
            alpha = EditorGUILayout.Slider(alpha, 0, 1);
            GUILayout.EndHorizontal();
            GUILayout.Space(20);
            SetMipMapAndTransparency(ref mipmap, ref alphais);
            GUILayout.Space(20);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("另存"))
            {
                if (tex == null) return;
                Texture2D textureTemporary = decompressTexture;
                Color[] colors = colors = textureTemporary.GetPixels();
                switch (channel)
                {
                    case Channel.A:
                        for (int i = 0; i < textureTemporary.width * textureTemporary.height; i++)
                        {
                            colors[i].a = alpha;
                        }

                        break;
                    case Channel.B:
                        for (int i = 0; i < textureTemporary.width * textureTemporary.height; i++)
                        {
                            colors[i].b = alpha;
                        }

                        break;
                    case Channel.G:
                        for (int i = 0; i < textureTemporary.width * textureTemporary.height; i++)
                        {
                            colors[i].g = alpha;
                        }

                        break;
                    case Channel.R:
                        for (int i = 0; i < textureTemporary.width * textureTemporary.height; i++)
                        {
                            colors[i].r = alpha;
                        }
                        break;
                }
                textureTemporary.SetPixels(colors);
                textureTemporary.Apply();
                string absolute = Save(GetTextureFolder(tex), tex.name, textureTemporary, AssetDatabase.GetAssetPath(tex));
                TextureImporterSet(AbsoluteToRelativelyPath(absolute), mipmap, alphais);

                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button("替换"))
            {
                if (tex == null) return;
                Texture2D textureTemporary = decompressTexture;
                Color[] colors = colors = textureTemporary.GetPixels();
                switch (channel)
                {
                    case Channel.A:
                        for (int i = 0; i < textureTemporary.width * textureTemporary.height; i++)
                        {
                            colors[i].a = alpha;
                        }

                        break;
                    case Channel.B:
                        for (int i = 0; i < textureTemporary.width * textureTemporary.height; i++)
                        {
                            colors[i].b = alpha;
                        }

                        break;
                    case Channel.G:
                        for (int i = 0; i < textureTemporary.width * textureTemporary.height; i++)
                        {
                            colors[i].g = alpha;
                        }

                        break;
                    case Channel.R:
                        for (int i = 0; i < textureTemporary.width * textureTemporary.height; i++)
                        {
                            colors[i].r = alpha;
                        }
                        break;
                }
                textureTemporary.SetPixels(colors);
                textureTemporary.Apply();
                string absolute = RelativelyToAbsolutePath(AssetDatabase.GetAssetPath(tex));
                absolute = Save(absolute, textureTemporary);
                TextureImporterSet(AbsoluteToRelativelyPath(absolute), mipmap, alphais);

                GUIUtility.ExitGUI();
            }


            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();


        }


        private void SetMaterial()
        {
            material.SetInt("_Channel", (int)channel + 1);
            material.SetFloat("_Value", alpha);
        }

        private void RenderPreview()
        {
            rt = RenderTexture.GetTemporary(256, 256);
            RenderTexture temp = RenderTexture.active;
            RenderTexture.active = rt;
            Graphics.Blit(tex, rt, material);
            previewTex.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
            previewTex.Apply();
            RenderTexture.active = temp;
            RenderTexture.ReleaseTemporary(rt);

        }
    }
}