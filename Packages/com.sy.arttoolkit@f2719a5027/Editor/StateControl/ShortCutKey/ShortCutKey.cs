
using UnityEditor;
using UnityEngine;

namespace SYArtToolkit
{

    public class ShortCutKey
    {
        public const string OpenWindowName = "美术工具箱/打开美术工具箱";
        public const string RapidFileName = "美术工具箱/工具列表/便捷/快速打开文件夹";
        public const string RapidSceneName = "美术工具箱/工具列表/便捷/快速打开场景";
        public const string AssetRefSearchName = "美术工具箱/工具列表/便捷/资产引用查找";
        public const string RenderCubemap = "美术工具箱/工具列表/便捷/烘焙CubeMap";
        public const string MergeTextureName = "美术工具箱/工具列表/贴图/贴图合并";
        public const string RampGenerateName = "美术工具箱/工具列表/贴图/渐变生成";
        public const string SplitFrameName = "美术工具箱/工具列表/贴图/序列拆分";
        public const string ModifySizeName = "美术工具箱/工具列表/贴图/贴图缩放";
        public const string ChannelSetName = "美术工具箱/工具列表/贴图/通道一键设置";
        public const string TextureEditorName = "美术工具箱/工具列表/贴图/贴图编辑";
        public const string FormatChangeName = "美术工具箱/工具列表/贴图/格式转变";
        public const string NoiseGenerateName = "美术工具箱/工具列表/贴图/噪音生成";
        public const string ChannelCombineName = "美术工具箱/工具列表/贴图/通道合并";
        public const string TextureCropName = "美术工具箱/工具列表/贴图/贴图裁剪";
        public const string TextureBlurName = "美术工具箱/工具列表/贴图/贴图模糊";
        public const string SetParticleName = "美术工具箱/工具列表/粒子/批量修改粒子";
        public const string SetParticleDelayName = "美术工具箱/工具列表/粒子/批量设置delay";
        public const string NewParticlePrefabName = "美术工具箱/工具列表/粒子/新建粒子预设";
        public const string CleanMaterialName = "美术工具箱/工具列表/材质/清理无用材质依赖";
        public const string PerformanceName = "美术工具箱/工具列表/性能/性能检测";
        public const string AssetsAuditName = "美术工具箱/工具列表/性能/资产审核";
        public const string DrawCallTestName = "美术工具箱/工具列表/性能/渲染检测";
        public const string NewEffectUniversalMaterial = "美术工具箱/工具列表/性能/新建特效通用材质";
        public const string EffectsBatchOptimization = "美术工具箱/工具列表/性能/特效合批优化";
        public const string MaterialReplaceShader = "美术工具箱/工具列表/性能/材质球替换Shader";
        public const string PropertyFindPrefab = "美术工具箱/工具列表/性能/资产查找预制体";
        public const string PrefabUpdateNode = "美术工具箱/工具列表/性能/预制体更新节点";
        private const string ShortCutKeyName = "美术工具箱/快捷键设置";

        [MenuItem(OpenWindowName, false, 0)]
        public static void OpenWindow()
        {
            GetScriptableObjectDataArtToolBox.DeleteOriginSource();
            GetScriptableObjectDataArtToolBox.CheckGlobal();


            if (WindowControl.Instance)
            {
                WindowControl.Instance.Close();
                Object.DestroyImmediate(WindowControl.Instance);
                WindowControl.Instance = null;
            }
            else EditorWindow.GetWindow<WindowControl>("美术工具");


        }

        [MenuItem(RapidFileName)]
        public static void OpenTool0()
        {
            if (WindowControl.Instance == null)
            {
                OpenWindow();
            }

            WindowControl.Instance.SetTool(Tool.RapidOpenFolder);
            WindowControl.Instance.setAbout(About.Universal);
        }

        [MenuItem(RapidSceneName)]
        public static void OpenTool1()
        {

            if (WindowControl.Instance == null)
            {
                OpenWindow();
            }

            WindowControl.Instance.SetTool(Tool.RapidOpenScene);
            WindowControl.Instance.setAbout(About.Universal);
        }

        [MenuItem(AssetRefSearchName)]
        public static void OpenTool18()
        {

            if (WindowControl.Instance == null)
            {
                OpenWindow();
            }

            WindowControl.Instance.SetTool(Tool.AssetRefSearchWin);
            WindowControl.Instance.setAbout(About.Universal);
        }


        [MenuItem(RenderCubemap)]
        public static void OpenTool22()
        {

            if (WindowControl.Instance == null)
            {
                OpenWindow();
            }

            WindowControl.Instance.SetTool(Tool.RenderCubemap);
            WindowControl.Instance.setAbout(About.Universal);
        }

        [MenuItem(MergeTextureName)]
        public static void OpenTool2()
        {
            if (WindowControl.Instance == null)
            {
                OpenWindow();
            }

            WindowControl.Instance.SetTool(Tool.MergeTexture);
            WindowControl.Instance.setAbout(About.Texture);

        }

        [MenuItem(RampGenerateName)]
        public static void OpenTool3()
        {

            if (WindowControl.Instance == null)
            {
                OpenWindow();
            }

            WindowControl.Instance.SetTool(Tool.RampGenerate);
            WindowControl.Instance.setAbout(About.Texture);
        }

        [MenuItem(SplitFrameName)]
        public static void OpenTool4()
        {
            if (WindowControl.Instance == null)
            {
                OpenWindow();
            }

            WindowControl.Instance.SetTool(Tool.FrameSplit);
            WindowControl.Instance.setAbout(About.Texture);

        }

        [MenuItem(ModifySizeName)]
        public static void OpenTool5()
        {
            if (WindowControl.Instance == null)
            {
                OpenWindow();
            }

            WindowControl.Instance.SetTool(Tool.ModifySize);
            WindowControl.Instance.setAbout(About.Texture);

        }

        [MenuItem(ChannelSetName)]
        public static void OpenTool6()
        {
            if (WindowControl.Instance == null)
            {
                OpenWindow();
            }

            WindowControl.Instance.SetTool(Tool.TextureChannelSet);
            WindowControl.Instance.setAbout(About.Texture);

        }

        [MenuItem(TextureEditorName)]
        public static void OpenTool7()
        {
            if (WindowControl.Instance == null)
            {
                OpenWindow();
            }

            WindowControl.Instance.SetTool(Tool.TextureEditor);
            WindowControl.Instance.setAbout(About.Texture);

        }

        [MenuItem(FormatChangeName)]
        public static void OpenTool8()
        {
            if (WindowControl.Instance == null)
            {
                OpenWindow();
            }

            WindowControl.Instance.SetTool(Tool.FormatChange);
            WindowControl.Instance.setAbout(About.Texture);

        }

        [MenuItem(NoiseGenerateName)]
        public static void OpenTool9()
        {
            if (WindowControl.Instance == null)
            {
                OpenWindow();
            }

            WindowControl.Instance.SetTool(Tool.NoiseGenerate);
            WindowControl.Instance.setAbout(About.Texture);

        }

        [MenuItem(ChannelCombineName)]
        public static void OpenTool10()
        {

            if (WindowControl.Instance == null)
            {
                OpenWindow();
            }

            WindowControl.Instance.SetTool(Tool.TexCombine);
            WindowControl.Instance.setAbout(About.Texture);
        }

        [MenuItem(TextureCropName)]
        public static void OpenTool11()
        {

            if (WindowControl.Instance == null)
            {
                OpenWindow();
            }

            WindowControl.Instance.SetTool(Tool.CropTexture);
            WindowControl.Instance.setAbout(About.Texture);
        }

        [MenuItem(TextureBlurName)]
        public static void OpenTool12()
        {

            if (WindowControl.Instance == null)
            {
                OpenWindow();
            }

            WindowControl.Instance.SetTool(Tool.TexBlur);
            WindowControl.Instance.setAbout(About.Texture);
        }

        [MenuItem(SetParticleName)]
        public static void OpenTool13()
        {

            if (WindowControl.Instance == null)
            {
                OpenWindow();
            }

            WindowControl.Instance.SetTool(Tool.ParAndTrailOrder);
            //WindowControl.Instance.setAbout(About.Particle);
        }

        [MenuItem(SetParticleDelayName)]
        public static void OpenTool14()
        {

            if (WindowControl.Instance == null)
            {
                OpenWindow();
            }

            WindowControl.Instance.SetTool(Tool.ParDelay);
            //WindowControl.Instance.setAbout(About.Particle);
        }



        [MenuItem(CleanMaterialName)]
        public static void OpenTool15()
        {

            if (WindowControl.Instance == null)
            {
                OpenWindow();
            }

            WindowControl.Instance.SetTool(Tool.MaterialClean);
            //WindowControl.Instance.setAbout(About.Material);
        }

        [MenuItem(PerformanceName)]
        public static void OpenTool16()
        {
            if (WindowControl.Instance == null)
            {
                OpenWindow();
            }

            WindowControl.Instance.SetTool(Tool.PerformanceTest);
            //WindowControl.Instance.setAbout(About.Performance);

        }

        [MenuItem(AssetsAuditName)]
        public static void OpenTool17()
        {
            if (WindowControl.Instance == null)
            {
                OpenWindow();
            }
            WindowControl.Instance.SetTool(Tool.AssetsAudit);
            //WindowControl.Instance.setAbout(About.Performance);

        }

        [MenuItem(DrawCallTestName)]
        public static void OpenTool20()
        {
            if (WindowControl.Instance == null)
            {
                OpenWindow();
            }
            WindowControl.Instance.SetTool(Tool.DrawCallCalculate);
            //WindowControl.Instance.setAbout(About.Performance);

        }

        [MenuItem(NewEffectUniversalMaterial)]
        public static void OpenTool19()
        {
            if (WindowControl.Instance == null)
            {
                OpenWindow();
            }
            WindowControl.Instance.SetTool(Tool.NewEffectUniversalMaterial);
            //WindowControl.Instance.setAbout(About.Performance);

        }


        [MenuItem(MaterialReplaceShader)]
        public static void OpenTool21()
        {
            if (WindowControl.Instance == null)
            {
                OpenWindow();
            }
            WindowControl.Instance.SetTool(Tool.MaterialReplaceShader);
            //WindowControl.Instance.setAbout(About.Performance);

        }


        [MenuItem(EffectsBatchOptimization)]
        public static void OpenTool23()
        {
            if (WindowControl.Instance == null)
            {
                OpenWindow();
            }
            WindowControl.Instance.SetTool(Tool.EffectsBatchOptimization);
            //WindowControl.Instance.setAbout(About.Performance);

        }


        [MenuItem(PrefabUpdateNode)]
        public static void OpenTool24()
        {
            if (WindowControl.Instance == null)
            {
                OpenWindow();
            }
            WindowControl.Instance.SetTool(Tool.PrefabUpdateNode);
            //WindowControl.Instance.setAbout(About.Performance);

        }


        [MenuItem(PropertyFindPrefab)]
        public static void OpenTool25()
        {
            if (WindowControl.Instance == null)
            {
                OpenWindow();
            }
            WindowControl.Instance.SetTool(Tool.PropertyFindPrefab);
            //WindowControl.Instance.setAbout(About.Performance);

        }

        [MenuItem(ShortCutKeyName, false, 1001)]
        public static void ShortCutKeySet()
        {
            if (WindowControl.Instance == null)
            {
                OpenWindow();
            }
            WindowControl.Instance.SetTool(Tool.SetShortCutKey);
            WindowControl.Instance.setAbout(About.Universal);
        }


    }
}