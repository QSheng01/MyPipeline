using UnityEditor;
using UnityEngine;


namespace SYArtToolkit
{

    public class ModifySize : TextureBase
    {

        public ModifySize()
        {
            SetColorSpaceValue();
        }

        private Texture2D tex = null;
        private GUILayoutOption[] texLayout = { GUILayout.Height(256), GUILayout.Width(256) };
        private Vector2Int outSize = new Vector2Int(64, 64);
        private bool mipMapEnable = false;
        private bool alphaIsTransparency = true;

        Vector2 scrollPos = new Vector2(0, 0);
        public override void InterfaceShow(bool atThisWindow, Rect noRect)
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos, false, false);
            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            tex = (Texture2D)EditorGUILayout.ObjectField(tex, typeof(Texture2D), false, texLayout);
            if (EditorGUI.EndChangeCheck())
            {
                outSize = new Vector2Int(tex.width, tex.height);
            }
            GUILayout.BeginVertical();
            GUILayout.Space(50);
            GUILayout.BeginHorizontal();
            GUILayout.Label("当前图片大小： ");
            if (tex != null)
            {
                GUILayout.Label(tex.width.ToString() + " * " + tex.height.ToString());
            }
            else
            {

                GUILayout.Label("0  *  0");
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("快捷选择宽度:");
                if (GUILayout.Button("32")) outSize.x = 32;
                if (GUILayout.Button("64")) outSize.x = 64;
                if (GUILayout.Button("128")) outSize.x = 128;
                if (GUILayout.Button("256")) outSize.x = 256;
                if (GUILayout.Button("512")) outSize.x = 512;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("快捷选择高度:");
                if (GUILayout.Button("32")) outSize.y = 32;
                if (GUILayout.Button("64")) outSize.y = 64;
                if (GUILayout.Button("128")) outSize.y = 128;
                if (GUILayout.Button("256")) outSize.y = 256;
                if (GUILayout.Button("512")) outSize.y = 512;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("输出贴图的大小");
                outSize = EditorGUILayout.Vector2IntField("", outSize);

            }
            GUILayout.EndHorizontal();
            if (outSize.x <= 1) outSize.x = 2;
            else if (outSize.x > 1024) outSize.x = 1024;
            if (outSize.y <= 1) outSize.y = 2;
            else if (outSize.y > 1024) outSize.y = 1024;


            GUILayout.Space(20);

            GUILayout.BeginHorizontal();
            mipMapEnable = GUILayout.Toggle(mipMapEnable, "Generate Mip Maps");
            alphaIsTransparency = GUILayout.Toggle(alphaIsTransparency, "Alpha Is Transparency");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("另存"))
            {
                if (tex == null)
                {
                    Debug.Log("输入图片不存在");
                }
                else
                {
                    string path = Save(GetTextureFolder(tex), tex.name, generate(tex), AssetDatabase.GetAssetPath(tex));
                    TextureImporterSet(AbsoluteToRelativelyPath(path), mipMapEnable, alphaIsTransparency);
                    GUIUtility.ExitGUI();
                }
            }
            GUIContent resetButtonLabel = new GUIContent
            {
                text = "替换",
                tooltip = "不会修改alphaIsTransparency和mipmapEnable"
            };
            if (GUILayout.Button(resetButtonLabel))
            {
                if (tex == null)
                {
                    Debug.Log("输入图片不存在");
                }
                else
                {
                    Save(RelativelyToAbsolutePath(AssetDatabase.GetAssetPath(tex)), generate(tex));
                    GUIUtility.ExitGUI();
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();

        }



        private Texture2D generate(Texture2D Tex)
        {
            //
            int widthTarget = outSize.x;
            int hetghtTarget = outSize.y;

            Texture2D temp = DeCompress(Tex);
            Texture2D result = new Texture2D(widthTarget, hetghtTarget);
            for (int i = 0; i < result.height; ++i)
            {
                for (int j = 0; j < result.width; ++j)
                {
                    Color newColor = temp.GetPixelBilinear(j / (float)result.width, i / (float)result.height);
                    result.SetPixel(j, i, newColor);
                }
            }

            result.Apply();
            return result;
        }




    }




}