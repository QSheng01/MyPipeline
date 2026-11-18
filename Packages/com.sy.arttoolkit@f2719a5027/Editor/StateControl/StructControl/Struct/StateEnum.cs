

using SYArtToolkit.CaptureFrame;
using SYArtToolkit.CreateEasyWallCollider;
using System;
using System.Reflection;

namespace SYArtToolkit
{


    public enum About
    {
        [AboutString("通用")]
        Universal,
        [AboutString("贴图")]
        Texture,
        [AboutString("特效")]
        VisualEffect,
        [AboutString("角色")]
        Character,
        [AboutString("地编")]
        TerrainEditor
    }

    public enum Tool
    {
        #region toolEnum
        [ToolDescribe("快捷键", About.Universal, typeof(ShortCutKeyInterface))]
        SetShortCutKey = 0,



        [ToolDescribe("性能检测", About.VisualEffect, typeof(PerformanceWindow))]
        PerformanceTest = 1,


        [ToolDescribe("资产审核", About.VisualEffect, typeof(AssetsAudit))]
        AssetsAudit = 2,


        [ToolDescribe("快速打开\n文件夹", About.Universal, typeof(RapidOpenFolder))]
        RapidOpenFolder = 3,


        [ToolDescribe("快速打开\n场景", About.Universal, typeof(RapidOpenScene))]
        RapidOpenScene = 4,


        [ToolDescribe("清除无用\n材质球依赖", About.VisualEffect, typeof(MaterialCleanerWin))]
        MaterialClean = 5,

        [ToolDescribe("批量设置\nDelay", About.VisualEffect, typeof(ParDelaySet))]
        ParDelay = 6,


        [ToolDescribe("批量修改\n粒子", About.VisualEffect, typeof(BatchsettingOrder))]
        ParAndTrailOrder = 7,


        [ToolDescribe("序列拆分", About.Texture, typeof(FrameSplit))]
        FrameSplit = 8,


        [ToolDescribe("贴图合并", About.Texture, typeof(MergeTextures))]
        MergeTexture = 9,


        [ToolDescribe("渐变生成", About.Texture, typeof(RampGenerate))]
        RampGenerate = 10,


        [ToolDescribe("贴图缩放", About.Texture, typeof(ModifySize))]
        ModifySize = 11,


        [ToolDescribe("格式转变", About.Texture, typeof(FormatChangeTexture))]
        FormatChange = 12,


        [ToolDescribe("噪音生成", About.Texture, typeof(NoiseGenetarion))]
        NoiseGenerate = 13,


        [ToolDescribe("资产引用\n查找", About.Universal, typeof(AssetRefSearchWin))]
        AssetRefSearchWin = 14,


        [ToolDescribe("通道一键\n设置值", About.Texture, typeof(AlphaSet))]
        TextureChannelSet = 15,


        [ToolDescribe("贴图编辑", About.Texture, typeof(TextureEditorClass))]
        TextureEditor = 16,


        [ToolDescribe("贴图裁剪", About.Texture, typeof(CropTextures))]
        CropTexture = 17,


        [ToolDescribe("通道合并", About.Texture, typeof(TextureCombine))]
        TexCombine = 18,


        [ToolDescribe("贴图模糊", About.Texture, typeof(TextureBlur))]
        TexBlur = 19,


        [ToolDescribe("渲染检测", About.Universal, typeof(DrawCallCalculate))]
        DrawCallCalculate = 20,


        [ToolDescribe("合并Mesh", About.TerrainEditor, typeof(MergeMeshAtSameMaterial))]
        MeshMergeAtSameMaterial = 21,


        [ToolDescribe("烘焙\nCubeMap", About.TerrainEditor, typeof(RenderCubemap))]
        RenderCubemap = 22,


        [ToolDescribe("新建特效\n通用材质", About.VisualEffect, typeof(EasyCreateMaterial))]
        NewEffectUniversalMaterial = 23,


        [ToolDescribe("特效\n合批优化", About.VisualEffect, typeof(AutomaticToolForOptimizingSpecialEffectsBatches))]
        EffectsBatchOptimization = 24,


        [ToolDescribe("材质球\n替换Shader", About.Universal, typeof(ShaderReplacement))]
        MaterialReplaceShader = 25,


        [ToolDescribe("查找引用\n预制体", About.Character, typeof(TextureFindPrefabAddSpace))]
        PropertyFindPrefab = 26,

        #endregion

        [ToolDescribe("预制体\n更新网格", About.Character, typeof(PrecastRenewal))]
        PrefabUpdateNode = 27,


        [ToolDescribe("Custom\nData添加", About.VisualEffect, typeof(ParticleSystemThreeDot))]
        ParticleSystemThreeDot = 28,


        [ToolDescribe("角色拍照", About.Character, typeof(PhotoActor))]
        PhotoActor = 29,


        [ToolDescribe("SH系数\n生成", About.Character, typeof(CubemapSHProjector))]
        CubemapSHProjector = 30,


        [ToolDescribe("场景资源\n检查", About.TerrainEditor, typeof(ResourceChecker))]
        ResourceChecker = 31,
        //[ToolDescribe("NewToolName", NewToolAbout, typeof(NewToolClass))]
        //NewTool = 28
        [ToolDescribe("创建\n碰撞墙", About.TerrainEditor, typeof(CreateWallColliderWindow))]
        CreateWallColliderWindow = 32,

        [ToolDescribe("序列帧\n截屏", About.Universal, typeof(CaptureFrameWindow))]
        CaptureFrameWindow = 33,

        [ToolDescribe("资产查找\n引用场景", About.TerrainEditor, typeof(AssetFindScene))]
        AssetFindScene = 34,

        [ToolDescribe("特效分级", About.VisualEffect, typeof(EffectLevel))]
        EffectLevel = 35,

        [ToolDescribe("地编顶点\n查看", About.TerrainEditor, typeof(PartitionPlatVertexEditor))]
        PartitionPlatVertex = 36,
        //AssetFindScene

        [ToolDescribe("合并场景\n物体材质", About.TerrainEditor, typeof(MeshTexBaker))]
        MeshTexBaker = 37,

        [ToolDescribe("批量改名", About.TerrainEditor, typeof(DealModelSet))]
        DealModelSet = 38,
    }


    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class AboutStringAttribute : Attribute
    {
        public string Value { get; }

        public AboutStringAttribute(string value)
        {
            Value = value;
        }
        public static string GetEnumString(Enum value)
        {
            var type = value.GetType();
            var fieldInfo = type.GetField(value.ToString());
            var attributes = fieldInfo.GetCustomAttributes(typeof(AboutStringAttribute), false);
            return (attributes[0] as AboutStringAttribute)?.Value;
        }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class ToolDescribeAttribute : Attribute
    {
        public string Value { get; }
        public About AAbout { get; }

        public ConstructorInfo Construct { get; }

        public ToolDescribeAttribute(string value, About about, System.Type type)
        {
            Value = value;
            AAbout = about;
            // 获取类型的所有构造函数
            ConstructorInfo[] constructors = type.GetConstructors();
            Construct = constructors[0];
        }
        public static ToolDescribe GetEnum(Enum value)
        {
            var type = value.GetType();
            var fieldInfo = type.GetField(value.ToString());
            var attributes = fieldInfo.GetCustomAttributes(typeof(ToolDescribeAttribute), false);
            var attribute = attributes[0] as ToolDescribeAttribute;
            if (attribute != null)
            {
                ToolDescribe res = new ToolDescribe(attribute.Value, attribute.AAbout, attribute.Construct);
                return res;
            }
            else return null;
        }
    }

    public class ToolDescribe
    {
        public string value { get; }
        public About about { get; }
        public ConstructorInfo construct { get; }
        public ToolDescribe(string value, About about, ConstructorInfo vonstruct)
        {
            this.value = value;
            this.about = about;
            this.construct = vonstruct;
        }
    }



}



