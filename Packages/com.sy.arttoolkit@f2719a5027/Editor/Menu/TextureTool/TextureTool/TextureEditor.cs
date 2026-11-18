
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SYArtToolkit
{
    // 2024/4/8修改计划: 统一展示的GUI函数，  把旋转纳入编辑函数，
    // 2024/4/8修改计划: 统一展示的GUI函数，  把旋转纳入编辑函数，
    public class TextureEditorClass : TextureBase
    {
        public TextureEditorClass()
        {
            SetTextureEditorDefaultValues();
        }
        // Start is called before the first frame update
        private Color editorColorTint;
        private float brightness, contrast, gamma, exposure, saturation, hue;
        private bool invert, greyscale, fullWhite, blackBackground, alphaGreyscale;
        private bool isFlipHorizontal, isFlipVertical, isLeftRotation;
        //两次左旋转相当于一次  水平+垂直
        //一次右旋转相当于一次 左旋转+水平+垂直
        private float exportScale = 1f;
        private Vector4 tilling;
        private bool mipmap = false;
        private bool alphais = true;

        private Texture2D editorTex, editorTexInput, showEditorTex, editorDecompressTex;

        private int rgba;

        private Vector2 scroll;

        //bool showOriginalImage = false;
        private int showImageChannel = 0;//0 展示RGB 1展示R  2展示G  3展示B  4展示A  5展示原始图像 
        private string[] showImageChannelString = new string[] { "展示编辑图像", "展示R通道", "展示G通道", "展示B通道", "展示A通道", "展示原始图像" };
        public override void InterfaceShow(bool atThisWindow, Rect noRect)
        {


            scroll = GUILayout.BeginScrollView(scroll, false, false);
            TextureEditor();
            GUILayout.EndScrollView();

            //EditorGUI.BeginChangeCheck();
            //GUILayout.Button("11");
            // if (EditorGUI.EndChangeCheck())
            // {
            //     Debug.Log("hfduishfjisdrgsfyhuguyfdgyusdgh");
            // }

        }

        private void TextureEditor()
        {
            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            editorTexInput = EditorGUILayout.ObjectField("拖入图片到这里开始编辑", editorTexInput, typeof(Texture2D), false,
                GUILayout.Width(200), GUILayout.Height(50)) as Texture2D;
            if (EditorGUI.EndChangeCheck())
            {
                if (editorTexInput != null)
                {
                    //解压
                    editorDecompressTex = DeCompress(editorTexInput);
                    editorTex = new Texture2D(editorDecompressTex.width, editorDecompressTex.height);
                    editorTex.SetPixels(editorDecompressTex.GetPixels());
                    editorTex.Apply();

                    float aspectRatio = (float)editorTex.width / (float)editorTex.height;
                    int width = Mathf.Min(editorTex.width, 256);
                    editorTex = ScaleTexture(editorTex, width, (int)(width / aspectRatio));

                    // cleanEditorTex = new Texture2D(editorTex.width, editorTex.height);
                    // cleanEditorTex.SetPixels(editorTex.GetPixels());
                    // cleanEditorTex.Apply();

                    SetTextureEditorDefaultValues();
                    RecalculateEditorTexture();
                }
                else editorTex = null;
            }

            EditorGUI.BeginChangeCheck();
            GUILayout.Space(100);
            GUILayout.BeginVertical();
            GUILayout.Label("编辑通道");
            GUILayout.BeginHorizontal();
            Color backgroundColorTemp = GUI.backgroundColor;
            GUI.backgroundColor = ((rgba >> 0) & 1) != 0 ? Color.red : backgroundColorTemp;
            if (GUILayout.Button("R", GUILayout.Width(30))) rgba ^= (1 << 0);
            GUI.backgroundColor = ((rgba >> 1) & 1) != 0 ? Color.green : backgroundColorTemp;
            if (GUILayout.Button("G", GUILayout.Width(30))) rgba ^= (1 << 1);
            GUI.backgroundColor = ((rgba >> 2) & 1) != 0 ? Color.blue : backgroundColorTemp;
            if (GUILayout.Button("B", GUILayout.Width(30))) rgba ^= (1 << 2);
            GUI.backgroundColor = ((rgba >> 3) & 1) != 0 ? Color.gray : backgroundColorTemp;
            if (GUILayout.Button("A", GUILayout.Width(30))) rgba ^= (1 << 3);
            GUI.backgroundColor = backgroundColorTemp;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            tilling = EditorGUILayout.Vector4Field("Tilling", tilling);

            GUILayout.EndHorizontal();
            //DrawLine(Color.grey, 1, 3);

            //if (editorTexInput != null )
            {
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label(editorTex, GUILayout.Width(256), GUILayout.Height(256)); //dfniusghfuisd```````
                    EditorGUILayout.BeginVertical();
                    {
                        TextureEditorColorParameter("颜色", ref editorColorTint, Color.white);
                        TextureEditorFloatParameter("明亮度", ref brightness, -1f, 5f);
                        TextureEditorFloatParameter("对比度", ref contrast, 0.0f, 5.0f, 1f);
                        TextureEditorFloatParameter("Gamma", ref gamma, 0.0f, 10f, 1f);
                        TextureEditorFloatParameter("曝光", ref exposure, -5f, 5f, 0f);
                        TextureEditorFloatParameter("饱和", ref saturation, 0f, 5f, 1f);
                        TextureEditorFloatParameter("色相", ref hue, 0f, 360f, 0f);

                        EditorGUILayout.BeginHorizontal();
                        {
                            invert = GUILayout.Toggle(invert, "反色", GUILayout.Width(253));
                            greyscale = GUILayout.Toggle(greyscale, "灰度");
                        }
                        EditorGUILayout.EndHorizontal();

                        // EditorGUILayout.BeginHorizontal();
                        // {
                        //     fullWhite = EditorGUILayout.Toggle("Fully white", fullWhite, GUILayout.Width(253));
                        //     blackBackground = EditorGUILayout.Toggle("黑底", blackBackground);
                        // }
                        // EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        alphaGreyscale = GUILayout.Toggle(alphaGreyscale, "去黑底(透明)", GUILayout.Width(253));
                        if (GUILayout.Button("一键重置", GUILayout.Width(100))) SetTextureEditorDefaultValues();
                        EditorGUILayout.EndHorizontal();




                        EditorGUILayout.Space();
                        EditorGUILayout.BeginHorizontal();
                        {

                            if (GUILayout.Button("顺时针旋转", GUILayout.MaxWidth(70)))
                            {
                                if (!isLeftRotation)
                                {
                                    isFlipHorizontal = !isFlipHorizontal;
                                    isFlipVertical = !isFlipVertical;
                                }
                                isLeftRotation = !isLeftRotation;

                            }
                            if (GUILayout.Button("逆时针旋转", GUILayout.MaxWidth(70)))
                            {
                                if (isLeftRotation)
                                {
                                    isFlipHorizontal = !isFlipHorizontal;
                                    isFlipVertical = !isFlipVertical;
                                }
                                isLeftRotation = !isLeftRotation;

                            }
                        }
                        //EditorGUILayout.EndHorizontal();

                        //EditorGUILayout.BeginHorizontal();
                        {
                            if (GUILayout.Button("水平翻转", GUILayout.MaxWidth(70)))
                                isFlipHorizontal = !isFlipHorizontal;
                            if (GUILayout.Button("垂直翻转", GUILayout.MaxWidth(70)))
                                isFlipVertical = !isFlipVertical;
                        }
                        EditorGUILayout.EndHorizontal();

                        if (EditorGUI.EndChangeCheck()) RecalculateEditorTexture();
                        EditorGUILayout.Space();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("显示图像:", GUILayout.Width(100));
                        if (EditorGUILayout.DropdownButton(new GUIContent(showImageChannelString[showImageChannel]), FocusType.Passive, GUILayout.Width(100)))
                        {
                            GenericMenu _menu = new GenericMenu();
                            for (int i = 0; i < showImageChannelString.Length; ++i)
                            {
                                _menu.AddItem(new GUIContent(showImageChannelString[i]), showImageChannelString[i] == showImageChannelString[showImageChannel], OnValueSelected, i);
                            }
                            _menu.ShowAsContext();
                        }
                        EditorGUILayout.EndHorizontal();


                        // GUILayout.BeginHorizontal();
                        // if (GUILayout.Button("原始图像"))
                        // {
                        //     showImageChannel = 5;
                        // }                       
                        // if (GUILayout.Button("编辑图像"))
                        // {
                        //     showImageChannel = 0;
                        // }
                        //             
                        // if (GUILayout.Button("R图像"))
                        // {
                        //     showImageChannel = 1;
                        // }
                        //
                        // if (GUILayout.Button("G图像"))
                        // {
                        //     showImageChannel = 2;
                        // }
                        //
                        // if (GUILayout.Button("B图像"))
                        // {
                        //     showImageChannel = 3;
                        // }
                        //
                        // if (GUILayout.Button("A图像"))
                        // {
                        //     showImageChannel = 4;
                        // }
                        // GUILayout.EndHorizontal();

                        // if (!showOriginalImage)
                        // {
                        //     if (GUILayout.Button("点击展示原始图像", GUILayout.MaxWidth(425)))
                        //         showOriginalImage = true;
                        // }
                        // else
                        // {
                        //     Color backgroundColor = GUI.backgroundColor;
                        //     GUI.backgroundColor = Color.yellow;
                        //     if (GUILayout.Button("点击展示原始图像", GUILayout.MaxWidth(425)))
                        //         showOriginalImage = false;
                        //     GUI.backgroundColor = backgroundColor;
                        // }
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();

                DrawLine(Color.grey, 1, 3);
                EditorGUILayout.Space();
                if (editorTexInput != null)
                {
                    int currWidth = Mathf.ClosestPowerOfTwo((int)(editorTexInput.width));
                    int currHeight = Mathf.ClosestPowerOfTwo((int)(editorTexInput.height));
                    GUILayout.Label(
                        "当前导出的大小: " + currWidth + " x " + currHeight + " pixel",
                        EditorStyles.boldLabel);
                    SetMipMapAndTransparency(ref mipmap, ref alphais);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("保存"))
                    {
                        ComputeFinalTexture();
                        string pingPath = Save(Path.GetDirectoryName(AssetDatabase.GetAssetPath(editorTexInput)),
                            editorTexInput.name, editorDecompressTex, AssetDatabase.GetAssetPath(editorTexInput));
                        AssetDatabase.Refresh();
                        TextureImporterSet(AbsoluteToRelativelyPath(pingPath), mipmap, alphais);
                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath(AbsoluteToRelativelyPath(pingPath),
                            typeof(Texture)));
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("替换"))
                    {
                        ComputeFinalTexture();
                        Save(
                            Application.dataPath.Substring(0, Application.dataPath.Length - 6) +
                            AssetDatabase.GetAssetPath(editorTexInput), editorDecompressTex);
                        editorDecompressTex = DeCompress(editorTexInput);
                        editorTex = new Texture2D(editorDecompressTex.width, editorDecompressTex.height);
                        editorTex.SetPixels(editorDecompressTex.GetPixels());
                        editorTex.Apply();

                        float aspectRatio = (float)editorTex.width / (float)editorTex.height;
                        int width = Mathf.Min(editorTex.width, 256);
                        editorTex = ScaleTexture(editorTex, width, (int)(width / aspectRatio));

                        // cleanEditorTex = new Texture2D(editorTex.width, editorTex.height);
                        // cleanEditorTex.SetPixels(editorTex.GetPixels());
                        // cleanEditorTex.Apply();

                        SetTextureEditorDefaultValues();
                        RecalculateEditorTexture();
                        GUIUtility.ExitGUI();
                    }

                    GUILayout.EndHorizontal();
                }
            }
            // else
            // {
            //     EditorGUILayout.BeginHorizontal();
            //     {
            //         if (!showOriginalImage) GUILayout.Label(editorTex); //dfniusghfuisd
            //         else GUILayout.Label(editorDecompressTex); //``````````````````
            //         EditorGUILayout.BeginVertical();
            //         {
            //             TextureEditorColorParameter("颜色", ref editorColorTint, Color.white);
            //             TextureEditorFloatParameter("明亮度", ref brightness, -1f, 5f);
            //             TextureEditorFloatParameter("对比度", ref contrast, 0.0f, 5.0f, 1f);
            //             TextureEditorFloatParameter("Gamma", ref gamma, 0.0f, 10f, 1f);
            //             TextureEditorFloatParameter("曝光", ref exposure, -5f, 5f, 0f);
            //             TextureEditorFloatParameter("饱和", ref saturation, 0f, 5f, 1f);
            //             TextureEditorFloatParameter("色相", ref hue, 0f, 360f, 0f);
            //
            //             EditorGUILayout.BeginHorizontal();
            //             {
            //                 EditorGUILayout.Toggle("反色", invert, GUILayout.Width(253));
            //                 EditorGUILayout.Toggle("灰度", greyscale);
            //             }
            //             EditorGUILayout.EndHorizontal();
            //
            //             // EditorGUILayout.BeginHorizontal();
            //             // {
            //             //     fullWhite = EditorGUILayout.Toggle("Fully white", fullWhite, GUILayout.Width(253));
            //             //     blackBackground = EditorGUILayout.Toggle("黑底", blackBackground);
            //             // }
            //             // EditorGUILayout.EndHorizontal();
            //             EditorGUILayout.Toggle("去黑底(透明)", alphaGreyscale);
            //
            //             EditorGUILayout.Space();
            //             EditorGUILayout.BeginHorizontal();
            //             {
            //                 GUILayout.Button("顺时针旋转", GUILayout.MaxWidth(210));
            //                 GUILayout.Button("逆时针旋转", GUILayout.MaxWidth(210));
            //             }
            //             EditorGUILayout.EndHorizontal();
            //
            //             EditorGUILayout.BeginHorizontal();
            //             {
            //                 GUILayout.Button("水平翻转", GUILayout.MaxWidth(210));
            //                 GUILayout.Button("垂直翻转", GUILayout.MaxWidth(210));
            //             }
            //             EditorGUILayout.EndHorizontal();
            //
            //             EditorGUILayout.Space();
            //             GUILayout.Button("点击展示原始图像", GUILayout.MaxWidth(425));
            //         }
            //         EditorGUILayout.EndVertical();
            //     }
            //     EditorGUILayout.EndHorizontal();
            // }

        }

        private string GetNewValidPath(string path, int i = 1)
        {
            int number = i;
            path = path.Replace(".png", "");
            string newPath = path + "_" + number.ToString();
            string fullPath = newPath + ".png";
            if (System.IO.File.Exists(fullPath))
            {
                number++;
                fullPath = GetNewValidPath(path, number);
            }

            return fullPath;
        }


        private void RecalculateEditorTexture()
        {
            if (editorTexInput == null) return;
            Color[] pixels = editorDecompressTex.GetPixels();
            int texWidth = editorDecompressTex.width;
            int texHeight = editorDecompressTex.height;

            ComputeImageColorFilters(pixels, texWidth, texHeight);
            FilterColor(pixels);
            ShowColor(pixels);
            editorTex = new Texture2D(texWidth, texHeight);
            editorTex.SetPixels(pixels);
            editorTex.Apply();
            //
            //在这里加入旋转操作.
        }

        private void FilterColor(Color[] des)
        {
            if ((rgba & 0b1111) != 0b1111)
            {
                var src = editorDecompressTex.GetPixels();
                for (int i = 0; i < 4; ++i)
                {
                    bool regain = ((rgba >> i) & 1) == 0;
                    if (regain)
                    {
                        for (int j = 0; j < src.Length; ++j)
                        {
                            des[j][i] = src[j][i];
                        }
                    }
                }
            }
        }

        private void ShowColor(Color[] des)
        {
            switch (showImageChannel)
            {
                case 1:
                case 2:
                case 3:
                case 4:
                    for (int i = 0; i < des.Length; ++i)
                    {
                        float c = des[i][showImageChannel - 1];
                        for (int j = 0; j < 3; ++j)
                        {
                            des[i][j] = c;
                        }
                        des[i][3] = 1;
                    }
                    break;
                case 5:
                    var src = editorDecompressTex.GetPixels();
                    for (int i = 0; i < des.Length; ++i)
                    {
                        des[i] = src[i];
                    }
                    break;
                default:
                    break;
            }
        }

        private void OnValueSelected(object i)
        {
            showImageChannel = (int)i;
            RecalculateEditorTexture();
        }

        private void SetTextureEditorDefaultValues()
        {
            tilling = new Vector4(0, 0, 1, 1);
            editorColorTint = Color.white;
            brightness = 0f;
            contrast = 1f;
            gamma = 1f;
            exposure = 0f;
            saturation = 1f;
            hue = 0f;
            invert = false;
            greyscale = false;
            fullWhite = false;
            blackBackground = false;
            alphaGreyscale = false;
            isFlipHorizontal = false;
            isFlipVertical = false;
            isLeftRotation = false;
            exportScale = 1f;
            rgba = 0b1111;
            showImageChannel = 0;
        }

        private void DrawLine(Color color, int thickness = 2, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += (padding / 2);
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }

        private Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            targetWidth = Mathf.ClosestPowerOfTwo(targetWidth);
            targetHeight = Mathf.ClosestPowerOfTwo(targetHeight);

            Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, true);
            Color[] scaledPixels = result.GetPixels(0);
            float incX = ((float)1 / source.width) * ((float)source.width / targetWidth);
            float incY = ((float)1 / source.height) * ((float)source.height / targetHeight);
            for (int px = 0; px < scaledPixels.Length; px++)
                scaledPixels[px] = source.GetPixelBilinear(incX * ((float)px % targetWidth),
                    incY * (float)Mathf.Floor(px / targetWidth));

            result.SetPixels(scaledPixels, 0);
            result.Apply();
            return result;
        }



        private void TextureEditorFloatParameter(string parameterName, ref float parameter, float rangeMin = -100f,
            float rangeMax = 100f, float resetValue = 0f)
        {
            EditorGUILayout.BeginHorizontal();
            {
                parameter = EditorGUILayout.Slider(parameterName, parameter, rangeMin, rangeMax,
                    GUILayout.MaxWidth(400));
                GUIContent resetButtonLabel = new GUIContent
                {
                    text = "R",
                    tooltip = "重设到默认值"
                };
                if (GUILayout.Button(resetButtonLabel, GUILayout.Width(20))) parameter = resetValue;
            }
            EditorGUILayout.EndHorizontal();
        }



        private void TextureEditorColorParameter(string parameterName, ref Color parameter, Color resetValue)
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUIContent colorLabel = new GUIContent
                {
                    text = parameterName,
                    tooltip = parameterName
                };
                parameter = EditorGUILayout.ColorField(colorLabel, parameter, true, true, true,
                    GUILayout.MaxWidth(400));
                GUIContent resetButtonLabel = new GUIContent
                {
                    text = "R",
                    tooltip = "重设到默认值"
                };
                if (GUILayout.Button(resetButtonLabel, GUILayout.Width(20))) parameter = resetValue;
            }
            EditorGUILayout.EndHorizontal();
        }

        // private void RotateEditorTextureLeft()
        // {
        //     Color[] pixels = editorTex.GetPixels();
        //     //Color[] pixelsClean = cleanEditorTex.GetPixels();
        //     int texWidth = editorTex.width;
        //     int texHeight = editorTex.height;
        //
        //     pixels = RotateClockWise(pixels, texWidth, texHeight);
        //     //pixelsClean = RotateClockWise(pixelsClean, texWidth, texHeight);
        //
        //     editorTex = new Texture2D(texHeight, texWidth); //Width and Height get swapped to account for rotation
        //     editorTex.SetPixels(pixels);
        //     editorTex.Apply();
        //     // cleanEditorTex = new Texture2D(texHeight, texWidth); //Width and Height get swapped to account for rotation
        //     // cleanEditorTex.SetPixels(pixelsClean);
        //     // cleanEditorTex.Apply();
        //
        //     rotationAmount = (rotationAmount + 1) % 4;
        // }

        private void RotateClockWise(Color[] pixels, int width, int height)
        {
            Color[] outputPixels = new Color[pixels.Length];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i1 = GetPixelIndex(x, height - y - 1, width);
                    int i2 = GetPixelIndex(y, x, height);
                    outputPixels[i2] = pixels[i1];
                }
            }

            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = outputPixels[i];
            }
        }

        // private void FlipEditorTexture(bool isHorizontal)
        // {
        //     Color[] pixels = editorTex.GetPixels();
        //     //Color[] pixelsClean = cleanEditorTex.GetPixels();
        //     int texWidth = editorTex.width;
        //     int texHeight = editorTex.height;
        //
        //     if (isHorizontal)
        //     {
        //         pixels = FlipHorizontal(pixels, texWidth, texHeight);
        //         //pixelsClean = FlipHorizontal(pixelsClean, texWidth, texHeight);
        //         isFlipHorizontal = !isFlipHorizontal;
        //     }
        //     else
        //     {
        //         pixels = FlipVertical(pixels, texWidth, texHeight);
        //         //pixelsClean = FlipVertical(pixelsClean, texWidth, texHeight);
        //         isFlipVertical = !isFlipVertical;
        //     }
        //
        //     editorTex = new Texture2D(texWidth, texHeight);
        //     editorTex.SetPixels(pixels);
        //     editorTex.Apply();
        //     // cleanEditorTex = new Texture2D(texWidth, texHeight);
        //     // cleanEditorTex.SetPixels(pixelsClean);
        //     // cleanEditorTex.Apply();
        // }

        private void FlipHorizontal(Color[] pixels, int width, int height)
        {
            Color[] outputPixels = new Color[pixels.Length];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i1 = GetPixelIndex(x, y, width);
                    int i2 = GetPixelIndex(width - 1 - x, y, width);
                    outputPixels[i1] = pixels[i2];
                }
            }

            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = outputPixels[i];
            }
        }

        private void FlipVertical(Color[] pixels, int width, int height)
        {
            Color[] outputPixels = new Color[pixels.Length];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i1 = GetPixelIndex(x, y, width);
                    int i2 = GetPixelIndex(x, height - 1 - y, width);
                    outputPixels[i1] = pixels[i2];
                }
            }

            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = outputPixels[i];
            }
        }

        private int GetPixelIndex(int x, int y, int width)
        {
            return y * width + x;
        }

        private void ComputeImageColorFilters(Color[] pixels, int width, int height)
        {

            float cosHsv = saturation * Mathf.Cos(hue * 3.14159265f / 180f);
            float sinHsv = saturation * Mathf.Sin(hue * 3.14159265f / 180f);

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i].r = Mathf.Clamp01(((pixels[i].r - 0.5f) * contrast) + 0.5f);
                pixels[i].g = Mathf.Clamp01(((pixels[i].g - 0.5f) * contrast) + 0.5f);
                pixels[i].b = Mathf.Clamp01(((pixels[i].b - 0.5f) * contrast) + 0.5f);

                pixels[i] = new Color(Mathf.Clamp01(pixels[i].r * (1 + brightness)),
                    Mathf.Clamp01(pixels[i].g * (1 + brightness)), Mathf.Clamp01(pixels[i].b * (1 + brightness)),
                    pixels[i].a);

                pixels[i].r = Mathf.Pow(Mathf.Abs(pixels[i].r), gamma);
                pixels[i].g = Mathf.Pow(Mathf.Abs(pixels[i].g), gamma);
                pixels[i].b = Mathf.Pow(Mathf.Abs(pixels[i].b), gamma);

                pixels[i].r = Mathf.Clamp01(pixels[i].r * Mathf.Pow(2, exposure));
                pixels[i].g = Mathf.Clamp01(pixels[i].g * Mathf.Pow(2, exposure));
                pixels[i].b = Mathf.Clamp01(pixels[i].b * Mathf.Pow(2, exposure));

                pixels[i] *= editorColorTint;

                Color hueShiftColor = pixels[i];
                hueShiftColor.r = Mathf.Clamp01((.299f + .701f * cosHsv + .168f * sinHsv) * pixels[i].r +
                                                (.587f - .587f * cosHsv + .330f * sinHsv) * pixels[i].g +
                                                (.114f - .114f * cosHsv - .497f * sinHsv) * pixels[i].b);
                hueShiftColor.g = Mathf.Clamp01((.299f - .299f * cosHsv - .328f * sinHsv) * pixels[i].r +
                                                (.587f + .413f * cosHsv + .035f * sinHsv) * pixels[i].g +
                                                (.114f - .114f * cosHsv + .292f * sinHsv) * pixels[i].b);
                hueShiftColor.b = Mathf.Clamp01((.299f - .3f * cosHsv + 1.25f * sinHsv) * pixels[i].r +
                                                (.587f - .588f * cosHsv - 1.05f * sinHsv) * pixels[i].g +
                                                (.114f + .886f * cosHsv - .203f * sinHsv) * pixels[i].b);
                pixels[i] = hueShiftColor;

                if (invert) pixels[i] = new Color(1 - pixels[i].r, 1 - pixels[i].g, 1 - pixels[i].b, pixels[i].a);
                if (blackBackground)
                {
                    if (pixels[i].a < 0.05f) pixels[i] = new Color(pixels[i].a, pixels[i].a, pixels[i].a, 1);
                    else pixels[i] = new Color(pixels[i].r, pixels[i].g, pixels[i].b, 1);
                }

                if (greyscale || fullWhite || alphaGreyscale)
                {
                    float greyScale = pixels[i].r * 0.59f + pixels[i].g * 0.3f + pixels[i].b * 0.11f;

                    if (fullWhite) pixels[i] = new Color(1, 1, 1, greyScale);
                    else if (greyscale) pixels[i] = new Color(greyScale, greyScale, greyScale, pixels[i].a);

                    if (alphaGreyscale) pixels[i] = new Color(pixels[i].r, pixels[i].g, pixels[i].b, greyScale);
                }
            }

            if (isLeftRotation)
            {
                RotateClockWise(pixels, width, height);
            }

            if (isFlipVertical)
            {
                FlipVertical(pixels, width, height);
            }

            if (isFlipHorizontal)
            {
                FlipHorizontal(pixels, width, height);
            }

            if (tilling != tillingDefault)
            {
                if (tilling.x < 0 || tilling.x > 1) tilling.x = Mathf.Max(Mathf.Min(1, tilling.x), 0);
                if (tilling.y < 0 || tilling.y > 1) tilling.y = Mathf.Max(Mathf.Min(1, tilling.y), 0);
                if (tilling.z < 0.125f || tilling.z > 8.0f) tilling.z = Mathf.Max(Mathf.Min(8.0f, tilling.z), 0.125f);
                if (tilling.w < 0.125f || tilling.w > 8.0f) tilling.w = Mathf.Max(Mathf.Min(8.0f, tilling.w), 0.125f);
                int newWidth = Mathf.RoundToInt(width * tilling.z);
                int newHeight = Mathf.RoundToInt(height * tilling.w);
                float offsetX = tilling.x;
                float offsetY = tilling.y;
                // 创建新的 Color 数组
                Color[] newColors = new Color[width * height];

                Func<float, float, Color> getBilinearFilteredColor = (u, v) =>
                {
                    int x0 = Mathf.FloorToInt(u * (width - 1));
                    int y0 = Mathf.FloorToInt(v * (height - 1));
                    int x1 = Mathf.CeilToInt(u * (width - 1));
                    int y1 = Mathf.CeilToInt(v * (height - 1));

                    Color c00 = pixels[y0 * width + x0];
                    Color c10 = pixels[y0 * width + x1];
                    Color c01 = pixels[y1 * width + x0];
                    Color c11 = pixels[y1 * width + x1];

                    float tx = u * (width - 1) - x0;
                    float ty = v * (height - 1) - y0;

                    Color color = Color.Lerp(Color.Lerp(c00, c10, tx), Color.Lerp(c01, c11, tx), ty);
                    return color;
                };
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // 计算原始纹理中的坐标
                        float u = (float)x / newWidth + offsetX;
                        float v = (float)y / newHeight + offsetY;
                        u -= Mathf.Floor(u);
                        v -= Mathf.Floor(v);

                        // 获取原始颜色数组中的颜色值
                        Color color = getBilinearFilteredColor(u, v);

                        // 将颜色值应用到新的颜色数组中
                        newColors[y * width + x] = color;
                    }
                }

                for (int i = 0; i < newColors.Length; ++i)
                {
                    pixels[i] = newColors[i];
                }
            }

        }

        private readonly Vector4 tillingDefault = new Vector4(0, 0, 1, 1);



        private void ComputeFinalTexture()
        {
            var pixels = editorDecompressTex.GetPixels();
            var texWidth = editorDecompressTex.width;
            var texHeight = editorDecompressTex.height;

            // for (int i = 0; i < rotationAmount; i++)
            // {
            //     texWidth = editorDecompressTex.width;
            //     texHeight = editorDecompressTex.height;
            //     pixels = editorDecompressTex.GetPixels();
            //     pixels = RotateClockWise(pixels, texWidth, texHeight);
            //     editorDecompressTex = new Texture2D(texHeight, texWidth);
            //     editorDecompressTex.SetPixels(pixels);
            //     editorDecompressTex.Apply();
            // }
            //
            // if (isFlipHorizontal) pixels = FlipHorizontal(pixels, texWidth, texHeight);
            // if (isFlipVertical) pixels = FlipVertical(pixels, texWidth, texHeight);

            ComputeImageColorFilters(pixels, texWidth, texHeight);
            FilterColor(pixels);
            editorDecompressTex = new Texture2D(texWidth, texHeight);
            editorDecompressTex.SetPixels(pixels);
            editorDecompressTex.Apply();

            if (Math.Abs(exportScale - 1f) > 0.05f)
                editorDecompressTex = ScaleTexture(editorDecompressTex, (int)(texWidth * exportScale),
                    (int)(texHeight * exportScale));
        }




    }
}