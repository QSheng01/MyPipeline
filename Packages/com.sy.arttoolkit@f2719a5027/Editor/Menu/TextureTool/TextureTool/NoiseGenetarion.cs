
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SYArtToolkit
{

    public class NoiseGenetarion : TextureBase
    {


        private Vector2 scroll = new Vector2(0, 0);

        public override void InterfaceShow(bool atThisWindow, Rect noRect)
        {
            scroll = EditorGUILayout.BeginScrollView(scroll, false, false);
            NoiseCreator();
            EditorGUILayout.EndScrollView();
        }


        private enum TextureSizes
        {
            _32 = 32,
            _64 = 64,
            _128 = 128,
            _256 = 256,
            _512 = 512,
        }



        private string noiseName = "NoiseTexture";
        private Texture2D noisePreview = null;
        RenderTexture noiseRenderTarget = null;
        Material noiseMaterial;
        private float noiseScaleX = 10f, noiseScaleY = 10f, noiseContrast = 1f, noiseBrightness = 0f;
        private float noiseFractalAmount = 1f, noiseJitter = 1f;
        private int noiseSeed = 0;
        private bool noiseSquareScale = false, noiseInverted = false, isFractalNoise;

        private void CheckCreationNoiseTextures()
        {
            if (noisePreview == null) noisePreview = new Texture2D(256, 256);
            if (noiseRenderTarget == null)
                noiseRenderTarget =
                    new RenderTexture(noisePreview.width, noisePreview.height, 0, RenderTextureFormat.ARGB32);
        }

        private void NoiseSetMaterial()
        {
            if (noiseType == NoiseTypes.Fractal || noiseType == NoiseTypes.Perlin || noiseType == NoiseTypes.Billow)
            {
                isFractalNoise = true;
                noiseMaterial = new Material(Shader.Find("Hidden/ArtToolkit/FractalNoise"));
                noiseScaleX = 4f;
                noiseScaleY = 4f;
            }
            else
            {
                isFractalNoise = false;
                noiseMaterial = new Material(Shader.Find("Hidden/ArtToolkit/WorleyNoise"));
                noiseScaleX = 10f;
                noiseScaleY = 10f;
            }

            switch (noiseType)
            {
                case NoiseTypes.Fractal:
                    noiseFractalAmount = 8f;
                    noiseMaterial.SetFloat("_Fractal", 1);
                    break;
                case NoiseTypes.Perlin:
                    noiseFractalAmount = 1f;
                    noiseMaterial.SetFloat("_Fractal", 1);
                    break;
                case NoiseTypes.Billow:
                    noiseFractalAmount = 4f;
                    noiseMaterial.SetFloat("_Fractal", 0);
                    break;
                case NoiseTypes.Voronoi:
                    noiseMaterial.SetFloat("_NoiseType", 0f);
                    break;
                case NoiseTypes.Water:
                    noiseMaterial.SetFloat("_NoiseType", 3f);
                    break;
                case NoiseTypes.Cellular:
                    noiseMaterial.SetFloat("_NoiseType", 4f);
                    break;
                case NoiseTypes.Cells1:
                    noiseMaterial.SetFloat("_NoiseType", 1f);
                    break;
                case NoiseTypes.Cells2:
                    noiseMaterial.SetFloat("_NoiseType", 2f);
                    break;
            }
        }

        private TextureSizes noiseSize = TextureSizes._64;
        private FilterMode noiseFiltering = FilterMode.Bilinear;

        private enum NoiseTypes
        {
            Fractal,
            Perlin,
            Billow,
            Voronoi,
            Water,
            Cellular,
            Cells1,
            Cells2
        }

        private NoiseTypes noiseType = NoiseTypes.Fractal;


        public NoiseGenetarion()
        {
            NoiseSetMaterial();
            CheckCreationNoiseTextures();
            UpdateNoiseMatAndRender();

        }


        private void NoiseCreator()
        {
            //GUILayout.Label("Tileable Noise Creator", bigLabel);
            //GUILayout.Space(20);

            EditorGUILayout.BeginHorizontal();
            {

                if (noisePreview != null) GUILayout.Label(noisePreview);
                GUILayout.Space(20);
                EditorGUILayout.BeginVertical(GUILayout.MaxWidth(550));
                {
                    if (noisePreview == null)
                        GUILayout.Label("选择一个类型开始编辑", EditorStyles.boldLabel);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    {
                        GUILayout.Label("类型:", GUILayout.MaxWidth(145));
                        noiseType = (NoiseTypes)EditorGUILayout.EnumPopup(noiseType, GUILayout.MaxWidth(200));
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        NoiseSetMaterial();
                        CheckCreationNoiseTextures();
                        UpdateNoiseMatAndRender();
                    }

                    GUIContent resetButtonLabel = new GUIContent
                    {
                        text = "R",
                        tooltip = "一键重设到默认值"
                    };
                    if (GUILayout.Button(resetButtonLabel, GUILayout.Width(20)))
                    {

                        if (isFractalNoise)
                        {
                            noiseScaleX = 4;
                        }
                        else
                        {
                            noiseScaleX = 10;
                        }

                        noiseScaleY = noiseScaleX;
                        noiseSquareScale = false;
                        if (noiseType == NoiseTypes.Fractal)
                            noiseFractalAmount = 8f;
                        else if (noiseType == NoiseTypes.Perlin)
                            noiseFractalAmount = 1f;
                        else if (noiseType == NoiseTypes.Billow)
                            noiseFractalAmount = 4;
                        else noiseJitter = 1;
                        noiseContrast = 1f;
                        noiseBrightness = 0f;
                        noiseSeed = 0;
                        noiseInverted = false;


                        if (noiseMaterial == null) NoiseSetMaterial();
                        CheckCreationNoiseTextures();

                        UpdateNoiseMatAndRender();
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUI.BeginChangeCheck();
                    if (isFractalNoise)
                    {
                        TextureEditorFloatParameter("Scale X", ref noiseScaleX, 0.1f, 50f, 4f);
                        if (!noiseSquareScale) TextureEditorFloatParameter("Scale Y", ref noiseScaleY, 0.1f, 50f, 4f);
                    }
                    else
                    {
                        TextureEditorFloatParameter("Scale X", ref noiseScaleX, 0.1f, 50f, 10f);
                        if (!noiseSquareScale) TextureEditorFloatParameter("Scale Y", ref noiseScaleY, 0.1f, 50f, 10f);
                    }

                    noiseSquareScale = GUILayout.Toggle(noiseSquareScale, "等比缩放", GUILayout.MaxWidth(200));
                    if (noiseSquareScale) noiseScaleY = noiseScaleX;
                    if (noiseType == NoiseTypes.Fractal)
                        TextureEditorFloatParameter("分形数", ref noiseFractalAmount, 1f, 10f, 8f);
                    else if (noiseType == NoiseTypes.Perlin)
                        TextureEditorFloatParameter("分形数", ref noiseFractalAmount, 1f, 10f, 1f);
                    else if (noiseType == NoiseTypes.Billow)
                        TextureEditorFloatParameter("分形数", ref noiseFractalAmount, 1f, 10f, 4f);
                    else TextureEditorFloatParameter("抖动", ref noiseJitter, 0.0f, 2f, 1f);
                    TextureEditorFloatParameter("对比度", ref noiseContrast, 0.1f, 10f, 1f);
                    TextureEditorFloatParameter("明亮", ref noiseBrightness, -1f, 1f, 0f);
                    TextureEditorIntParameter("随机种子", ref noiseSeed, 0, 100, 0);
                    noiseInverted = GUILayout.Toggle(noiseInverted, "反色");

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (noiseMaterial == null) NoiseSetMaterial();
                        CheckCreationNoiseTextures();

                        UpdateNoiseMatAndRender();
                    }

                    GUILayout.Space(20);
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("大小:", GUILayout.MaxWidth(145));
                        noiseSize = (TextureSizes)EditorGUILayout.EnumPopup(noiseSize, GUILayout.MaxWidth(200));
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("过滤: ", GUILayout.MaxWidth(145));
                        noiseFiltering = (FilterMode)EditorGUILayout.EnumPopup(noiseFiltering, GUILayout.MaxWidth(200));
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();

                //if (noisePreview != null) GUILayout.Label(noisePreview);
            }
            EditorGUILayout.EndHorizontal();

            //
            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            GUILayout.Label("命名:", GUILayout.Width(30));
            noiseName = GUILayout.TextField(noiseName);
            if (GUILayout.Button("保存"))
            {
                int texSize = (int)noiseSize;
                Texture2D finalNoiseTex = new Texture2D(texSize, texSize);
                RenderTexture finalRenderTarget = new RenderTexture(finalNoiseTex.width, finalNoiseTex.height, 0,
                    RenderTextureFormat.ARGB32);
                Graphics.Blit(finalNoiseTex, finalRenderTarget, noiseMaterial);
                finalNoiseTex.ReadPixels(new Rect(0, 0, finalRenderTarget.width, finalRenderTarget.height), 0, 0);
                finalNoiseTex.Apply();

                var prefs = EditorPrefs.GetString("Arttoolkit.NoiseGenerate");
                var path = Save(string.IsNullOrEmpty(prefs) ? Application.dataPath : prefs, noiseName, finalNoiseTex);
                if (!string.IsNullOrEmpty(path))
                {
                    EditorPrefs.SetString("Arttoolkit.NoiseGenerate", Path.GetDirectoryName(path));
                }
                GUIUtility.ExitGUI();
            }

            GUILayout.EndHorizontal();

        }

        private void UpdateNoiseMatAndRender()
        {
            if (noiseType == NoiseTypes.Fractal || noiseType == NoiseTypes.Perlin || noiseType == NoiseTypes.Billow)
            {
                noiseMaterial.SetFloat("_EndBand", noiseFractalAmount);
            }
            else noiseMaterial.SetFloat("_Jitter", noiseJitter);

            noiseMaterial.SetFloat("_ScaleX", noiseScaleX);
            noiseMaterial.SetFloat("_ScaleY", noiseScaleY);
            noiseMaterial.SetFloat("_Offset", (float)noiseSeed);
            noiseMaterial.SetFloat("_Contrast", noiseContrast);
            noiseMaterial.SetFloat("_Brightness", noiseBrightness);
            noiseMaterial.SetFloat("_Invert", noiseInverted ? 1f : 0f);

            Graphics.Blit(noisePreview, noiseRenderTarget, noiseMaterial);
            noisePreview.ReadPixels(new Rect(0, 0, noiseRenderTarget.width, noiseRenderTarget.height), 0, 0);
            noisePreview.Apply();
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

        private void TextureEditorIntParameter(string parameterName, ref int parameter, int rangeMin = -100,
            int rangeMax = 100, int resetValue = 0)
        {
            EditorGUILayout.BeginHorizontal();
            {
                parameter = EditorGUILayout.IntSlider(parameterName, parameter, rangeMin, rangeMax,
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


        //
        // private void HandleSaveFolderEditorPref(string keyName, string defaultPath, string logsFeatureName)
        // {
        //     if (!PlayerPrefs.HasKey(keyName)) PlayerPrefs.SetString(keyName, defaultPath);
        //     materialTargetFolder =
        //         (DefaultAsset)AssetDatabase.LoadAssetAtPath(PlayerPrefs.GetString(keyName), typeof(DefaultAsset));
        //     if (materialTargetFolder == null)
        //     {
        //         PlayerPrefs.SetString(keyName, defaultPath);
        //         materialTargetFolder =
        //             (DefaultAsset)AssetDatabase.LoadAssetAtPath(PlayerPrefs.GetString(keyName), typeof(DefaultAsset));
        //         if (materialTargetFolder == null)
        //         {
        //             materialTargetFolder = (DefaultAsset)AssetDatabase.LoadAssetAtPath("Assets/", typeof(DefaultAsset));
        //             if (materialTargetFolder == null)
        //                 Debug.LogWarning("The desired save folder doesn't exist. " + PlayerPrefs.GetString(keyName) +
        //                                  "\n Go to Window -> AllIn1VfxToolkitWindow and set a valid folder");
        //             else PlayerPrefs.SetString("Assets/", defaultPath);
        //         }
        //     }
        //
        //     materialTargetFolder = (DefaultAsset)EditorGUILayout.ObjectField("拖入文件夹",
        //         materialTargetFolder, typeof(DefaultAsset), false);
        //
        //     if (materialTargetFolder != null && IsAssetAFolder(materialTargetFolder))
        //     {
        //         string path = AssetDatabase.GetAssetPath(materialTargetFolder);
        //         PlayerPrefs.SetString(keyName, path);
        //         EditorGUILayout.HelpBox("Valid folder! " + logsFeatureName + " save path: " + path, MessageType.Info, true);
        //     }
        //     else EditorGUILayout.HelpBox("Select the new " + logsFeatureName + " Folder", MessageType.Warning, true);
        // }
        //
        // private static bool IsAssetAFolder(Object obj)
        // {
        //     string path = "";
        //
        //     if (obj == null) return false;
        //
        //     path = AssetDatabase.GetAssetPath(obj.GetInstanceID());
        //
        //     if (path.Length > 0)
        //     {
        //         if (Directory.Exists(path)) return true;
        //         else return false;
        //     }
        //
        //     return false;
        // }


        // GUILayout.Space(20);
        // GUILayout.Label("选择文件夹", EditorStyles.boldLabel);
        // HandleSaveFolderEditorPref("All1VfxNoise", noiseSavesPath, "Noises");
        //
        // string prefSavedPath = PlayerPrefs.GetString("All1VfxNoise") + "/";
        // if (Directory.Exists(prefSavedPath) && noisePreview != null)
        // {
        //     if (GUILayout.Button("保存"))
        //     {
        //         string path = prefSavedPath + "Noise.png";
        //         if (System.IO.File.Exists(path)) path = GetNewValidPath(path);
        //         string texName = path.Replace(prefSavedPath, "");
        //
        //         path = EditorUtility.SaveFilePanel("Save texture as PNG", prefSavedPath, texName, "png");
        //         if (path.Length != 0)
        //         {
        //             int texSize = (int)noiseSize;
        //             Texture2D finalNoiseTex = new Texture2D(texSize, texSize);
        //             RenderTexture finalRenderTarget = new RenderTexture(finalNoiseTex.width, finalNoiseTex.height, 0,
        //                 RenderTextureFormat.ARGB32);
        //             Graphics.Blit(finalNoiseTex, finalRenderTarget, noiseMaterial);
        //             finalNoiseTex.ReadPixels(new Rect(0, 0, finalRenderTarget.width, finalRenderTarget.height), 0, 0);
        //             finalNoiseTex.Apply();
        //
        //             byte[] pngData = finalNoiseTex.EncodeToPNG();
        //             if (pngData != null) File.WriteAllBytes(path, pngData);
        //             AssetDatabase.Refresh();
        //
        //             if (path.IndexOf("Assets/") >= 0)
        //             {
        //                 string subPath = path.Substring(path.IndexOf("Assets/"));
        //                 TextureImporter importer = AssetImporter.GetAtPath(subPath) as TextureImporter;
        //                 if (importer != null)
        //                 {
        //                     Debug.Log("Noise saved inside the project: " + subPath);
        //                     importer.filterMode = noiseFiltering;
        //                     importer.SaveAndReimport();
        //                     EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath(subPath, typeof(Texture)));
        //                 }
        //             }
        //             else Debug.Log("Noise saved outside the project: " + path);
        //         }
        //     }
        // }

    }
}
