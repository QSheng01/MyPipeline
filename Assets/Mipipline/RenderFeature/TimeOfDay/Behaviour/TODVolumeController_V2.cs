using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.Environment;
using UnityEngine;
using UnityEngine.Rendering;
using Sirenix.OdinInspector;
using SYPackages.TimeOfDay;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Serialization;
using RenderSettings = UnityEngine.RenderSettings;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using SYPackages.AtmosphericScatteringFog;
using SYPackages.AtmosphericScattering;
using SYPackages.Utillity;
using UnityEngine.Rendering.LookDev;

[ExecuteAlways]
public class TODVolumeController_V2 : MonoBehaviour
{
    public TimeOfDayManager todM;
    public SunMoonCompContrl sunMoonCompContrl;
    public Material SkyMat;
    public GameObject DirectionalLight;
    public bool DeepSetting = false;

    [FoldoutGroup("环境")][LabelText("漫反射顶部")][GradientUsage(true, ColorSpace.Linear)]
    public Gradient ABColorTop = new Gradient();
    [FoldoutGroup("环境")][LabelText("漫反射中部")][GradientUsage(true, ColorSpace.Linear)]
    public Gradient ABColorMid = new Gradient();
    [FoldoutGroup("环境")][LabelText("漫反射底部")][GradientUsage(true, ColorSpace.Linear)]
    public Gradient ABColorDown = new Gradient();
    [FoldoutGroup("环境")][LabelText("镜面反射强度")]
    public AnimationCurve ReflectionIntensity = new AnimationCurve();

    [FoldoutGroup("天空")][LabelText("天空顶部")][GradientUsage(true, ColorSpace.Linear)]
    public Gradient TopColor = new Gradient();
    [FoldoutGroup("天空")][LabelText("地平线")][GradientUsage(true, ColorSpace.Linear)]
    public Gradient MidColor = new Gradient();
    [FoldoutGroup("天空")][LabelText("底部")]
    public Gradient DownColor = new Gradient();
    [FoldoutGroup("天空")][LabelText("阳光散射颜色")][GradientUsage(true, ColorSpace.Linear)]
    public Gradient SkyScatterColor = new Gradient();
    [FoldoutGroup("天空")][LabelText("空气散射颜色")][GradientUsage(true, ColorSpace.Linear)]
    public Gradient AirScatterColor = new Gradient();
    [FoldoutGroup("天空")] [LabelText("高度雾_浓度(相反)")][ShowIf("DeepSetting")]
    public AnimationCurve AirScatterHeight = new AnimationCurve();
    [FoldoutGroup("天空")] [LabelText("高度雾_高度")][ShowIf("DeepSetting")]
    public AnimationCurve HeightFogHeight = new AnimationCurve();
    [FoldoutGroup("天空")] [LabelText("地平线散射_强度")][ShowIf("DeepSetting")]
    public AnimationCurve AirScatterDensity = new AnimationCurve();
    [FoldoutGroup("天空")] [LabelText("Rayleigh散射海拔高度")][Tooltip("可理解为高空与地平线过渡程度")][ShowIf("DeepSetting")]
    public AnimationCurve kr = new AnimationCurve();
    [FoldoutGroup("天空")] [LabelText("天空散射_强度")][Tooltip("可理解为天光强度")][ShowIf("DeepSetting")]
    public AnimationCurve km = new AnimationCurve();
    [FoldoutGroup("天空")] [LabelText("Mie散射海拔高度")][Tooltip("可理解为光晕的影响范围")][ShowIf("DeepSetting")]
    public AnimationCurve rayleigh = new AnimationCurve();
    [FoldoutGroup("天空")] [LabelText("太阳散射_光晕强度")][Tooltip("可理解为光晕强度")][ShowIf("DeepSetting")]
    public AnimationCurve mie = new AnimationCurve();
    [FoldoutGroup("天空")] [LabelText("整体散射强度")]
    public AnimationCurve scattering = new AnimationCurve();
    [FoldoutGroup("天空/太阳")] [LabelText("太阳散射_散度")]
    public AnimationCurve mieG = new AnimationCurve();
    [FoldoutGroup("天空/太阳")] [LabelText("太阳散射_强度2")]
    public AnimationCurve sunScatterIntensity = new AnimationCurve();
    [FoldoutGroup("天空/太阳")] [LabelText("太阳散射_太阳大小")]
    public AnimationCurve sunRadius = new AnimationCurve();
    [FoldoutGroup("天空/太阳")] [LabelText("太阳散射_太阳散射颜色")][GradientUsage(true, ColorSpace.Linear)]
    public Gradient sunSkyScatteringColor = new Gradient();
    [FoldoutGroup("天空/太阳")] [LabelText("太阳散射_太阳光晕颜色")][GradientUsage(true, ColorSpace.Linear)]
    public Gradient sunHaloColor = new Gradient();
    [FoldoutGroup("天空/太阳")] [LabelText("太阳散射_太阳光晕大小")]
    public AnimationCurve sunHaloExponent = new AnimationCurve();
    [FoldoutGroup("天空/月亮")] [LabelText("月亮_升起落下控制")]
    public AnimationCurve moonSpeed = new AnimationCurve();


    [FoldoutGroup("灯光")] [LabelText("镜头光晕_强度")]
    public AnimationCurve lightLensFlareIntensity = new AnimationCurve();
    [FoldoutGroup("灯光")] [LabelText("灯光_颜色")]
    public Gradient lightColor = new Gradient();
    [FoldoutGroup("灯光")] [LabelText("灯光_强度")]
    public AnimationCurve lightIntensity = new AnimationCurve();
    [FoldoutGroup("灯光")] [LabelText("灯光阴影_强度")]
    public AnimationCurve lightShadowIntensity = new AnimationCurve();

    [FoldoutGroup("雾气")] [LabelText("雾气模式")]
    public FogMode fogMode;
    [FoldoutGroup("雾气")] [LabelText("雾气颜色")][GradientUsage(true, ColorSpace.Linear)]
    public Gradient fogColor = new Gradient();
    [FoldoutGroup("雾气")] [LabelText("距离雾_起始距离")][ShowIf("fogMode", FogMode.Linear)]
    public AnimationCurve linearFogStart = new AnimationCurve();
    [FoldoutGroup("雾气")] [LabelText("距离雾_最大距离")][ShowIf("fogMode", FogMode.Linear)]
    public AnimationCurve linearFogEnd = new AnimationCurve();
    [FoldoutGroup("雾气")] [LabelText("指数雾_密度")][HideIf("fogMode", FogMode.Linear)]
    public AnimationCurve fogDensity = new AnimationCurve();
    [FoldoutGroup("雾气")] [LabelText("开启大气雾")][SerializeField]
    public bool enableAtmosphericFog = true;
    [FoldoutGroup("雾气/高度雾")] [LabelText("反射探针采样大气雾")][SerializeField][ShowIf("enableAtmosphericFog", true)][Indent(2)]
    public bool rpSampleATFog = true;
    [FoldoutGroup("雾气/高度雾")] [LabelText("高度雾_衰减距离")][SerializeField][ShowIf("enableAtmosphericFog", true)][Indent(2)]
    public AnimationCurve meanFreePath = new AnimationCurve();
    [FoldoutGroup("雾气/高度雾")] [LabelText("高度雾_起始距离")][SerializeField][ShowIf("enableAtmosphericFog", true)][Indent(2)]
    public AnimationCurve fogStartDisance = new AnimationCurve();
    [FoldoutGroup("雾气/高度雾")] [LabelText("高度雾_起始高度")][SerializeField][ShowIf("enableAtmosphericFog", true)][Indent(2)]
    public AnimationCurve baseHeight = new AnimationCurve();
    [FoldoutGroup("雾气/高度雾")] [LabelText("高度雾_最大高度")][SerializeField][ShowIf("enableAtmosphericFog", true)][Indent(2)]
    public AnimationCurve maximumHeight = new AnimationCurve();
    [FoldoutGroup("雾气/高度雾")] [LabelText("高度雾_颜色")][SerializeField][ShowIf("enableAtmosphericFog", true)][Indent(2)][GradientUsage(true, ColorSpace.Linear)]
    public Gradient tint = new Gradient();
    [FoldoutGroup("雾气/高度雾")] [LabelText("太阳散射颜色")][SerializeField][ShowIf("enableAtmosphericFog", true)][Indent(2)][GradientUsage(true, ColorSpace.Linear)]
    public Gradient sunColor = new Gradient();
    [FoldoutGroup("雾气/高度雾")] [LabelText("太阳散射范围")][SerializeField][ShowIf("enableAtmosphericFog", true)][Indent(2)]
    public AnimationCurve sunFallOff = new AnimationCurve();
    [FoldoutGroup("雾气/高度雾")] [LabelText("月亮散射颜色")][SerializeField][ShowIf("enableAtmosphericFog", true)][Indent(2)][GradientUsage(true, ColorSpace.Linear)]
    public Gradient moonColor = new Gradient();
    [FoldoutGroup("雾气/高度雾")] [LabelText("月亮散射范围")][SerializeField][ShowIf("enableAtmosphericFog", true)][Indent(2)]
    public AnimationCurve moonFallOff = new AnimationCurve();
    [FoldoutGroup("雾气")] [LabelText("丁达尔散射")]
    public AnimationCurve sunShafts = new AnimationCurve();

    [FoldoutGroup("云")] [LabelText("漫反射颜色_亮部")][GradientUsage(true, ColorSpace.Linear)]
    public Gradient cloudColor_Bright = new Gradient();
    [FoldoutGroup("云")] [LabelText("漫反射颜色2_亮部")][GradientUsage(true, ColorSpace.Linear)]
    public Gradient cloudColor_Bright2 = new Gradient();
    [FoldoutGroup("云")] [LabelText("漫反射颜色_暗部")]
    public Gradient cloudColor_Dark = new Gradient();
    [FoldoutGroup("云")] [LabelText("漫反射颜色_地平线")]
    public Gradient cloudColor_Horizon = new Gradient();
    [FoldoutGroup("云")] [LabelText("透射颜色_补充")][GradientUsage(true, ColorSpace.Linear)]
    public Gradient cloudColor_ScatterColor = new Gradient();
    [FoldoutGroup("云")] [LabelText("透射颜色_太阳")][GradientUsage(true, ColorSpace.Linear)]
    public Gradient cloudColor_ScatterSun = new Gradient();
    [FoldoutGroup("云")] [LabelText("透射颜色_月亮")][GradientUsage(true, ColorSpace.Linear)]
    public Gradient cloudColor_ScatterMoon = new Gradient();
    [FoldoutGroup("云")] [LabelText("高光颜色_太阳")][GradientUsage(true, ColorSpace.Linear)]
    public Gradient cloudColor_HighLightSun = new Gradient();
    [FoldoutGroup("云")] [LabelText("高光颜色_月亮")][GradientUsage(true, ColorSpace.Linear)]
    public Gradient cloudColor_HighLightMoon = new Gradient();

    [FoldoutGroup("云")] [LabelText("地平线颜色_对比度")]
    public AnimationCurve cloud_HorizonPow = new AnimationCurve();
    [FoldoutGroup("云")] [LabelText("地平线颜色_偏移")]
    public AnimationCurve cloud_HorizonOffset = new AnimationCurve();

    // 给大气雾的Feature访问用的全局参数
    public static bool EnableAtmosphericFog = true;
    public static bool RPSampleATFog = true;
    public static bool EnableSun = true;
    public static float MaxFogDistance = 99999;
    public static FogColorMode ColorMode = FogColorMode.SkyColor;
    public static Color Tint;
    public static float FogStartDistance;
    public static Color MoonColor;
    public static Color SunColor;
    public static float SunFallOff;
    public static float MoonFallOff;
    // 设置雾气采样用的cubemap的mip等级
    public static float MipFogNear = 0f;
    public static float MipFogFar = 1000f;
    public static float MipFogMaxMip = 0.5f;
    public static float MeanFreePath;
    public static Vector4 Albedo = new Vector4(1, 1, 1, 1);
    public static float BaseHeight;
    public static float MaximumHeight;
    public static float JitterStrength;
    public static float sunShaftIntensity;

    public static bool UseTOD = false;


    [SerializeField][ReadOnly]
    private float timer = 0;

    LensFlareComponentSRP lensFlare;
    Light dLight;

    bool hasDLight = false;
    bool hasLensFlare = false;
    [HideInInspector] public float shadowStrengthMultiplier = 1f;

    void OnEnable()
    {
        UseTOD = true;
    }

    void OnDisable()
    {
        UseTOD = false;
    }

    void Init()
    {
        if (DirectionalLight != null)
        {
            dLight = DirectionalLight.GetComponent<Light>();
            if (lensFlare == null)
            {
                lensFlare = DirectionalLight.GetComponent<LensFlareComponentSRP>();
                if (lensFlare == null)
                {
                    Debug.LogError("直射光没有lensFlare组件");
                }
                else
                {
                    hasLensFlare = true;
                }
            }

            hasDLight = true;
        }
        else
        {
            Debug.LogError("LOD控制器没有放入直射光GameObject");
            hasDLight = false;
            hasLensFlare = false;
            return;
        }
    }

    void Update()
    {
        Init();

        if (SkyMat == null || todM == null)
            return;

        RenderSettings.skybox = SkyMat;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;

        SkyMat.EnableKeyword("_ATMOSPERICSCATTERING");

        timer = todM.timeOfDay / 24f;

        SetAmbientColor();
        SetReflection();
        SetSkyColor();

        if (hasDLight)
            SetDLight();

        if (hasLensFlare)
            SetLensFlare();

        SetFog();
        SetCloud();
    }

    void SetCloud()
    {
        Shader.SetGlobalColor("_Cloud_BrightColor", EvaluateColor(cloudColor_Bright));
        Shader.SetGlobalColor("_Cloud_BrightColor2", EvaluateColor(cloudColor_Bright2));
        Shader.SetGlobalColor("_Cloud_BarkColor", EvaluateColor(cloudColor_Dark).linear);
        Shader.SetGlobalColor("_Cloud_HorizonColor", EvaluateColor(cloudColor_Horizon).linear);
        Shader.SetGlobalColor("_Cloud_SunScatterColor", EvaluateColor(cloudColor_ScatterSun));
        Shader.SetGlobalColor("_Cloud_MoonScatterColor", EvaluateColor(cloudColor_ScatterMoon));
        Shader.SetGlobalFloat("_Cloud_HorizonOffset", cloud_HorizonOffset.Evaluate(timer));
        Shader.SetGlobalFloat("_Cloud_HorizonPow", cloud_HorizonPow.Evaluate(timer));
        Shader.SetGlobalColor("_Cloud_HighLightSun", EvaluateColor(cloudColor_HighLightSun));
        Shader.SetGlobalColor("_Cloud_HighLightMoon", EvaluateColor(cloudColor_HighLightMoon));
        Shader.SetGlobalColor("_Cloud_ScatterColor_2", EvaluateColor(cloudColor_ScatterColor));
    }

    void SetFog()
    {
        RenderSettings.fogMode = fogMode;
        RenderSettings.fogColor = EvaluateColor(fogColor);
        RenderSettings.fogStartDistance = linearFogStart.Evaluate(timer);
        RenderSettings.fogEndDistance = linearFogEnd.Evaluate(timer);
        RenderSettings.fogDensity = fogDensity.Evaluate(timer) * 0.001f;

        SetAT();

        sunShaftIntensity = sunShafts.Evaluate(timer);
    }

    void SetDLight()
    {
        // LinearColor linearColor = new LinearColor();
        // linearColor.intensity = lightIntensity.Evaluate(timer);
        // Color color = EvaluateColor(lightColor).linear;
        // linearColor.red = color.r;
        // linearColor.green = color.g;
        // linearColor.blue = color.b;
        dLight.intensity = lightIntensity.Evaluate(timer);
        dLight.shadowStrength = lightShadowIntensity.Evaluate(timer) * shadowStrengthMultiplier;
        dLight.color = EvaluateColor(lightColor);

    }

    private void SetLensFlare()
    {
        lensFlare.intensity = lightLensFlareIntensity.Evaluate(timer);
    }

    private void SetAmbientColor()
    {
        RenderSettings.ambientSkyColor = EvaluateColor(ABColorTop);
        RenderSettings.ambientEquatorColor = EvaluateColor(ABColorMid);
        RenderSettings.ambientGroundColor = EvaluateColor(ABColorDown);
    }

    private void SetReflection()
    {
        RenderSettings.reflectionIntensity = ReflectionIntensity.Evaluate(timer);
    }

    private void SetSkyColor()
    {
        SkyMat.SetInt("_Azure_ScatteringMode", 1); // 这个模式调太阳散射颜色更方便

        SkyMat.SetColor("_SkyTopColor",  EvaluateColor(TopColor));
        SkyMat.SetColor("_SkyBottomColor",  EvaluateColor(MidColor));
        SkyMat.SetColor("_GroundColor",  EvaluateColor(DownColor));
        SkyMat.SetColor("_MieScatteringColor",  EvaluateColor(SkyScatterColor));
        SkyMat.SetColor("_RayleighScatteringColor",  EvaluateColor(AirScatterColor));
        SkyMat.SetFloat("_HeightFogHeight", HeightFogHeight.Evaluate(timer));
        SkyMat.SetFloat("_AirScatteringHeight", AirScatterHeight.Evaluate(timer));
        SkyMat.SetFloat("_AirScatteringDensity", AirScatterDensity.Evaluate(timer));
        SkyMat.SetFloat("_SunScatterIntensity", sunScatterIntensity.Evaluate(timer));
        SkyMat.SetFloat("_SunRadius", sunRadius.Evaluate(timer));
        SkyMat.SetColor("_Azure_ScatteringColor", EvaluateColor(sunSkyScatteringColor));
        SkyMat.SetColor("_SunHalo", EvaluateColor(sunHaloColor));
        SkyMat.SetFloat("_SunHaloExponent", sunHaloExponent.Evaluate(timer));

        SkyMat.EnableKeyword("_ATMOSPERICSCATTERING");
        //Scattering
        SkyMat.SetFloat("_Azure_FogScatteringScale", 1);
        SkyMat.SetFloat("_Azure_Kr", kr.Evaluate(timer) * 1000f);
        SkyMat.SetFloat("_Azure_Km", km.Evaluate(timer) * 1000f);
        SkyMat.SetVector("_Azure_Rayleigh", ComputeRayleigh() * rayleigh.Evaluate(timer));
        SkyMat.SetVector("_Azure_Mie", ComputeMie() * mie.Evaluate(timer));
        SkyMat.SetFloat("_MieG", mieG.Evaluate(timer));
        //mat.SetFloat("_Azure_MieDepth", mieDistance);
        SkyMat.SetFloat("_Azure_Scattering", scattering.Evaluate(timer) * 60f);

        if (sunMoonCompContrl != null)
            sunMoonCompContrl.moonTimer = moonSpeed.Evaluate(timer);
    }


    private Color EvaluateColor(Gradient gradient)
    {
        return gradient.Evaluate(timer);
    }

    private Vector3 ComputeRayleigh()
    {
        Vector3 rayleigh = Vector3.one;
        rayleigh = new Vector3(5.8f, 13.5f, 33.1f) * 0.000001f;
        return rayleigh;
    }

    private Vector3 ComputeMie()
    {
        Vector3 mie;
        mie = new Vector3(2.0f, 2.0f, 2.0f) * 0.00001f;
        return mie;
    }

    private void SetAT()
    {
        EnableAtmosphericFog = enableAtmosphericFog;
        RPSampleATFog = rpSampleATFog;
        EnableSun = true;
        MaxFogDistance = 99999;
        ColorMode = FogColorMode.SkyColor;
        Tint = EvaluateColor(tint);
        FogStartDistance = fogStartDisance.Evaluate(timer);
        MoonColor = EvaluateColor(moonColor);
        SunColor = EvaluateColor(sunColor);
        SunFallOff = sunFallOff.Evaluate(timer);
        MoonFallOff = moonFallOff.Evaluate(timer);
        // 设置雾气采样用的cubemap的mip等级
        MipFogNear = 0f;
        MipFogFar = 1000f;
        MipFogMaxMip = 0.5f;
        MeanFreePath = meanFreePath.Evaluate(timer);
        Albedo = new Vector4(1, 1, 1, 1);
        BaseHeight = baseHeight.Evaluate(timer);
        MaximumHeight = maximumHeight.Evaluate(timer);
        JitterStrength = 0;
    }
}
