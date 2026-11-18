using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;



namespace SYArtToolkit
{
    public class RenderCubemap : ToolInterface
    {

        private static string[] names = new string[] { "16", "32", "64", "128", "256", "512", "1024", "2048" };
        private static int[] sizes = new int[] { 16, 32, 64, 128, 256, 512, 1024, 2048 };


        int maskValue = 0;
        private string[] layerNames;
        private GenMode genMode;
        private int selectedSize = 1024;
        private int lastSelectedSize;
        private RenderTexture[] rtexs;
        private Texture2D[] colorTexs;
        private Transform center;

        public RenderCubemap()
        {
            if (layerNames == null) layerNames = new string[32];
            for (int i = 0; i < 32; i++)
            {
                layerNames[i] = LayerMask.LayerToName(i);
            }
        }

        private static Vector3[] cameraAngles = new Vector3[]
        {
            new Vector3(0, 90, 0), new Vector3(0, -90, 0),
            new Vector3(-90, 0, 0), new Vector3(90, 0, 0),
            new Vector3(0, 0, 0), new Vector3(0, 180, 0),
        };

        private static string[] filePostfixs = new string[]
        {
            " Left", " Right", " Up", " Down", " Front", " Back"
        };

        public override void Exit()
        {
            layerNames = new string[32];
            DestroyResources();
        }


        public override void InterfaceShow(bool noBool, Rect noRect)
        {

            GUILayout.Space(5);

            EditorGUI.BeginChangeCheck();
            genMode = (GenMode)EditorGUILayout.EnumPopup("Mode", genMode);
            selectedSize = EditorGUILayout.IntPopup("Face Size", selectedSize, names, sizes);
            center = EditorGUILayout.ObjectField("Center", center, typeof(Transform), true) as Transform;
            //EditorGUILayout.PropertyField(serializedObject.FindProperty(" layerMask"));


            // 在inspector窗口绘制LayerMask下拉选择框
            maskValue = EditorGUILayout.MaskField("Layer Mask", maskValue, layerNames);
            GUILayout.Space(15);

            bool hasError = false;
            Color oldContentColor = GUI.contentColor;
            GUI.contentColor = Color.red;
            if (center == null)
            {
                GUILayout.Label("必须设置'Center'!");
                hasError = true;
            }

            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GUI.contentColor = Color.red;
                GUILayout.Label("场景中必须有Main Camera!");
                hasError = true;
            }

            GUI.contentColor = oldContentColor;

            if (hasError)
            {
                return;
            }


            //  layerMask = EditorToolUtility.LayerMaskField("Culling Mask",  layerMask);
            GUILayout.Space(15);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Bake", GUILayout.Width(100)))
            {

                Submit.ArtToolkitData.Instance.AddTexture(1);
                if (rtexs == null || selectedSize != lastSelectedSize)
                {
                    DestroyResources();
                    CreateResources();
                    lastSelectedSize = selectedSize;
                }


                GameObject go = GameObject.Instantiate(mainCam.gameObject);
                go.name = "  CubemapCamera  ";
                Camera cam = go.GetComponent<Camera>();
                cam.enabled = false;
                cam.backgroundColor = Camera.main.backgroundColor;
                cam.aspect = 1;
                cam.fieldOfView = 90;
                cam.cullingMask = maskValue;

                go.transform.position = center.position;

                RenderTexture oldRt = RenderTexture.active;
                for (int i = 0; i < 6; i++)
                {
                    go.transform.localEulerAngles = cameraAngles[i];

                    RenderTexture.active = rtexs[i];
                    cam.targetTexture = rtexs[i];
                    cam.Render();

                    colorTexs[i].ReadPixels(new Rect(0, 0, selectedSize, selectedSize), 0, 0);
                    colorTexs[i].Apply();
                }


                RenderTexture.active = oldRt;
                Object.DestroyImmediate(go);

                Texture2D tex = WriteAndImportTexture();
                if (tex != null)
                    Debug.Log("渲染到环境贴图完成！", tex);
            }

            EditorGUILayout.EndHorizontal();
        }

        private Texture2D WriteAndImportTexture()
        {
            if (genMode == GenMode.SingleCubemap)
            {
                string path = EditorUtility.SaveFilePanelInProject("Save Cubemap", "", "png",
                    "Please enter a file name to save cubemap to");
                if (!string.IsNullOrEmpty(path))
                {
                    Texture2D cubeTex = new Texture2D(selectedSize * 4, selectedSize * 3, TextureFormat.RGB24, false);

                    Color[] colors = colorTexs[0].GetPixels();
                    cubeTex.SetPixels(selectedSize * 2, selectedSize, selectedSize, selectedSize, colors);
                    colors = colorTexs[1].GetPixels();
                    cubeTex.SetPixels(0, selectedSize, selectedSize, selectedSize, colors);
                    colors = colorTexs[2].GetPixels();
                    cubeTex.SetPixels(selectedSize, selectedSize * 2, selectedSize, selectedSize, colors);
                    colors = colorTexs[3].GetPixels();
                    cubeTex.SetPixels(selectedSize, 0, selectedSize, selectedSize, colors);
                    colors = colorTexs[4].GetPixels();
                    cubeTex.SetPixels(selectedSize, selectedSize, selectedSize, selectedSize, colors);
                    colors = colorTexs[5].GetPixels();
                    cubeTex.SetPixels(selectedSize * 3, selectedSize, selectedSize, selectedSize, colors);

                    cubeTex.Apply();
                    byte[] bytes = cubeTex.EncodeToPNG();
                    File.WriteAllBytes(path, bytes);
                    AssetDatabase.ImportAsset(path);
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    //importer.textureType = TextureImporterType.Cubemap;
                    importer.textureShape = TextureImporterShape.TextureCube;
                    importer.generateCubemap = TextureImporterGenerateCubemap.AutoCubemap;
                    importer.maxTextureSize = selectedSize;
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    cubeTex = AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D)) as Texture2D;

                    return cubeTex;
                }

                return null;

            }
            else if (genMode == GenMode.SixTextures)
            {
                string path = EditorUtility.SaveFilePanelInProject("Save Textures", "", "png",
                    "Please enter a file name to save textures to");
                if (!string.IsNullOrEmpty(path))
                {
                    //Material material = new Material(Shader.Find("Skybox/6 Sided"));
                    //AssetDatabase.CreateAsset(material, path);

                    string header = path.Substring(0, path.Length - 4);
                    for (int i = 0; i < 6; i++)
                    {
                        if (header.EndsWith(filePostfixs[i]))
                        {
                            header = header.Substring(0, header.Length - filePostfixs[i].Length);
                            break;
                        }
                    }

                    Texture2D[] newTextures = new Texture2D[6];
                    for (int i = 0; i < 6; i++)
                    {
                        string outPath = header + filePostfixs[i] + ".png";

                        byte[] bytes = colorTexs[i].EncodeToPNG();
                        File.WriteAllBytes(outPath, bytes);

                        AssetDatabase.ImportAsset(outPath);
                        TextureImporter importer = AssetImporter.GetAtPath(outPath) as TextureImporter;
                        importer.textureType = TextureImporterType.Default;
                        importer.wrapMode = TextureWrapMode.Clamp;
                        importer.maxTextureSize = selectedSize;
                        AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);

                        newTextures[i] = AssetDatabase.LoadAssetAtPath(outPath, typeof(Texture2D)) as Texture2D;
                    }

                    return newTextures[0];
                }
            }

            return null;
        }
        void CreateResources()
        {
            rtexs = new RenderTexture[6];
            for (int i = 0; i < 6; i++)
            {
                rtexs[i] = new RenderTexture(selectedSize, selectedSize, 24);
                rtexs[i].hideFlags = HideFlags.HideAndDontSave;
            }

            colorTexs = new Texture2D[6];
            for (int i = 0; i < 6; i++)
                colorTexs[i] = new Texture2D(selectedSize, selectedSize, TextureFormat.RGB24, false);
        }

        void DestroyResources()
        {
            if (rtexs != null)
            {
                for (int i = 0; i < 6; i++)
                    Object.DestroyImmediate(rtexs[i]);
                rtexs = null;
            }

            if (colorTexs != null)
            {
                for (int i = 0; i < 6; i++)
                {
                    Object.DestroyImmediate(colorTexs[i]);
                }
            }
        }


        private enum GenMode
        {
            SingleCubemap = 0,
            SixTextures
        }
    }
}
