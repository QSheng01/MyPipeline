using System;
using System.IO;
using System.Net;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using File = UnityEngine.Windows.File;
using Object = UnityEngine.Object;

namespace CustomShaderEditor
{
    public class RampTextureEditorWindow : OdinEditorWindow
    {
        public static void ShowWindow()
        {
            GetWindow<RampTextureEditorWindow>(false, "Ramp贴图编辑", true);
        }


        private string savePath;
        [NonSerialized] private Texture2D originTexture;
        [NonSerialized] private Texture2D tempTexture;
        [NonSerialized] private Material gradientMat;


        [NonSerialized] private GradientTexture.GradientInfo[] tempGradientInfos;
        [NonSerialized] private string rampName;

        [HideInInspector] public GradientTexture gradientTexture;
        private UnityEditor.Editor gradientTextureEditor;

        [OnInspectorGUI]
        [PropertyOrder(1)]
        private void ShowGradientEditor()
        {
            if (!gradientTexture) return;
            if (!gradientTextureEditor)
            {
                gradientTextureEditor = UnityEditor.Editor.CreateEditor(gradientTexture);
            }

            gradientTextureEditor.OnInspectorGUI();
        }


        [Button("同步到材(预览，用于调试，效果满意需要点击保存)")]
        [PropertyOrder(2)]
        public void UpdateToMat()
        {
            // Debug.Log(gradientTexture);
            // Debug.Log(gradientMat);
            if (gradientTexture != null && gradientMat != null)
            {
                tempTexture = gradientTexture.ToTexture();
                gradientMat.SetTexture(rampName, tempTexture);
            }
        }

        [Button("保存，真的包效果写到贴图上")]
        [PropertyOrder(3)]
        public void SaveTexture()
        {
            if (gradientTexture == null) return;
            var tempTexture = gradientTexture.ToTexture();
            var dataPath = AssetDatabase.GetAssetPath(gradientTexture);
            // Debug.Log(rampTexturePath );
            var fileInfo = new FileInfo(dataPath);
            var rampTexturePath = dataPath.Replace(fileInfo.Extension, ".tga");

            Debug.Log("rampTexturePath:" + rampTexturePath);

            byte[] bytes = tempTexture.EncodeToTGA();
            try
            {
                File.WriteAllBytes(rampTexturePath, bytes);
                AssetDatabase.Refresh();
                //重定向
                //Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture2D>(rampTexturePath);

                //Debug.Log(AssetDatabase.LoadAssetAtPath<Texture2D>(rampTexturePath));
                originTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(rampTexturePath);

                TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(rampTexturePath);            
                
                ti.textureType = TextureImporterType.Default;
                ti.sRGBTexture = true;
                ti.isReadable = false;
                ti.mipmapEnabled = false;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.filterMode = FilterMode.Bilinear;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.maxTextureSize = 256;
                
                TextureImporterPlatformSettings settingAndroid = new TextureImporterPlatformSettings();
                settingAndroid.name = BuildTarget.Android.ToString ();
                settingAndroid.maxTextureSize = 256;
                settingAndroid.format = TextureImporterFormat.RGB24;
                settingAndroid.overridden = true;
                
                TextureImporterPlatformSettings settingIOS = new TextureImporterPlatformSettings();
                settingIOS.name = BuildTarget.iOS.ToString ();
                settingIOS.maxTextureSize = 256;
                settingIOS.format = TextureImporterFormat.RGB24;
                settingIOS.overridden = true;
                
                TextureImporterPlatformSettings settingPC64 = new TextureImporterPlatformSettings();
                settingPC64.name = BuildTarget.StandaloneWindows64.ToString ();
                settingPC64.maxTextureSize = 256;
                settingPC64.format = TextureImporterFormat.RGB24;
                settingPC64.overridden = true;
                
                TextureImporterPlatformSettings settingPC = new TextureImporterPlatformSettings();
                settingPC.name = BuildTarget.StandaloneWindows.ToString ();
                settingPC.maxTextureSize = 256;
                settingPC.format = TextureImporterFormat.RGB24;
                settingPC.overridden = true;

                ti.SetPlatformTextureSettings(settingAndroid);
                ti.SetPlatformTextureSettings(settingIOS);
                ti.SetPlatformTextureSettings(settingPC64);
                ti.SetPlatformTextureSettings(settingPC);
                // ti.textureFormat = TextureImporterFormat.RGB24;

                ti.SaveAndReimport();
                
                gradientMat.SetTexture(rampName, originTexture);
                tempGradientInfos = gradientTexture.gradients;
                AssetDatabase.SaveAssetIfDirty(gradientMat);
                AssetDatabase.SaveAssets();
                Debug.Log("导出成功!");
            }
            catch
            {
                Debug.Log("导出失败!");
            }
        }


        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (gradientMat != null)
            {
                gradientMat.SetTexture(rampName, originTexture);
            }

            if (gradientTexture)
            {
                gradientTexture.gradients = tempGradientInfos;
            }
            //gradientTexture.OnDestroy();
        }

        public void SetOriginData(Material material, string name, Texture2D texture)
        {
            this.gradientMat = material;
            this.rampName = name;
            this.originTexture = texture;


            string rampTextureDataPath = null;
            if (texture == null)
            {
                var path = AssetDatabase.GetAssetPath(material);
                var fileinfo = new FileInfo(path);
                //Debug.LogError("texture is null");
                var floder = EditorUtility.OpenFolderPanel("选择需要创建的ramp贴图的路径", fileinfo.DirectoryName,
                    fileinfo.Directory.Name);
                if (string.IsNullOrEmpty(floder))
                {
                    this.Close();
                    return;
                }

                rampTextureDataPath = floder + "/" + material.name + "_ramp.asset";
                rampTextureDataPath = rampTextureDataPath.Remove(0, rampTextureDataPath.IndexOf("Assets"));
            }
            else
            {
                var rampTexturePath = AssetDatabase.GetAssetPath(texture);
                if (string.IsNullOrEmpty(rampTexturePath))
                {
                    Debug.LogError("texture is null");
                    return;
                }

                // Debug.Log(rampTexturePath );
                var fileInfo = new FileInfo(rampTexturePath);
                rampTextureDataPath = rampTexturePath.Replace(fileInfo.Extension, ".asset");
            }

            // Debug.Log(rampTextureDataPath );
            if (!File.Exists(rampTextureDataPath))
            {
                gradientTexture = ScriptableObject.CreateInstance<GradientTexture>();
                AssetDatabase.CreateAsset(gradientTexture, rampTextureDataPath);
            }
            else
            {
                gradientTexture = AssetDatabase.LoadAssetAtPath<GradientTexture>(rampTextureDataPath);
            }

            if (gradientTexture.gradients ==null|| gradientTexture.gradients.Length < 1)
            {
                gradientTexture.gradients = new GradientTexture.GradientInfo[5];
                for (int i = 0; i < gradientTexture.gradients.Length; i++)
                {
                    GradientTexture.GradientInfo gradientInfo = new GradientTexture.GradientInfo();
                    gradientInfo.nickName = string.Empty;
                    gradientInfo.gradient = new Gradient();
                    gradientTexture.gradients[i] = gradientInfo;
                }
            }

            //记录打开时的状态
            tempGradientInfos = new GradientTexture.GradientInfo[gradientTexture.gradients.Length];
            for (int i = 0; i < tempGradientInfos.Length; i++)
            {
                GradientTexture.GradientInfo temp = new GradientTexture.GradientInfo();
                temp.nickName = gradientTexture.gradients[i].nickName;
                temp.gradient = new Gradient();
                var colorkeys = (GradientColorKey[]) gradientTexture.gradients[i].gradient.colorKeys.Clone();
                var alphakeys = (GradientAlphaKey[]) gradientTexture.gradients[i].gradient.alphaKeys.Clone();
                temp.gradient.SetKeys(colorkeys, alphakeys);
                tempGradientInfos[i] = temp;
            }

            if (texture == null)
            {
                SaveTexture();
            }
        }
    }
}