using System;
using System.Collections;
using System.Collections.Generic;
// using Sirenix.OdinInspector;
using UnityEngine;


[ExecuteInEditMode]
public class ScatteringSkyController : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField][HideInInspector]private bool m_referencesHeaderGroup;
    [SerializeField][HideInInspector]private bool m_scatteringHeaderGroup;
    [SerializeField] private bool m_outerSpaceHeaderGroup;
    [SerializeField][HideInInspector]private bool m_cloudsHeaderGroup;
#endif

    // References
    [SerializeField][InspectorName("天空盒材质")] private Material m_skyMaterial = null;
    // [SerializeField][Label("云贴图")] private Texture2D m_dynamicCloudsTexture = null;
    // [SerializeField][Label("日贴图")] private Texture2D m_sunTexture = null;
    // [SerializeField][Label("月贴图")] private Texture2D m_moonTexture = null;
    // [SerializeField][Label("太阳坐标")] private Transform m_sunTransform = null;
    // [SerializeField][Label("月亮坐标")] private Transform m_moonTransform = null;
    // Scattering
    // [LabelText("大气分子密度（不清楚不要改）")][Range(0,3)] public float molecularDensity = 2.545f;
    // [LabelText("可见光红光波长（不清楚不要改）")][Range(380,740)] public float wavelengthR = 680.0f;
    // [LabelText("可见光绿光波长（不清楚不要改）")][Range(380,740)] public float wavelengthG = 550.0f;
    // [LabelText("可见光蓝光波长（不清楚不要改）")][Range(380,740)] public float wavelengthB = 450.0f;
    [Tooltip("可理解为高空与地平线过渡程度")]
    [Header("Rayleigh散射海拔高度")][Range(0,10)]public float kr = 8.4f;
    [Tooltip("可理解为天光强度")]
    [Header("Rayleigh散射程度")][Range(0,5)]public float rayleigh = 1.5f;
    [Tooltip("可理解为光晕的影响范围")]
    [Header("Mie散射海拔高度")][Range(0,10)]public float km = 1.2f;
    [Tooltip("可理解为光晕强度")]
    [Header("Mie散射程度")][Range(0,5)]public float mie = 1.0f;
    [Header("Mie各项异性")][Range(0,5)]public float mieG = 0.76f;
    [Header("Mie散射强度")][Range(0,1)] public float scattering = 0.25f;
    [Tooltip("提供基础亮度，以免没有大气散射时一片死黑")]
    //[LabelText("保底天空亮度")][Range(0,5)]public float luminance = 1.5f;
    //[LabelText("整体曝光强度")][Range(0,5)]public float exposure = 2.0f;
    // [Tooltip("天空颜色")]
    // [LabelText("天空颜色")]public Color rayleighColor = Color.white;
    // [Tooltip("光晕颜色")]
    // [LabelText("光晕颜色")]public Color mieColor = Color.white;
    // [Tooltip("自动调整或手动设置整体散射颜色")]
    // [Label("散射颜色模式")] public AzureScatteringMode m_scatteringMode = AzureScatteringMode.Automatic;
    // [Tooltip("手动设置整体颜色.")]
    // [Label("整体散射颜色")] public Color scatteringColor = Color.white;

    // Outer space
    public float sunTextureSize = 1.5f;
    public float sunTextureIntensity = 1.0f;
    public Color sunTextureColor = Color.white;
    public float moonTextureSize = 1.5f;
    public float moonTextureIntensity = 1.0f;
    public Color moonTextureColor = Color.white;

    // Clouds
    [Header("云高度")]public float dynamicCloudsAltitude = 7.5f;
    [Header("云方向")]public float dynamicCloudsDirection = 0.0f;
    [Header("云浓度")][Range(0,3)]public float dynamicCloudsDensity = 0.75f;
    public float dynamicCloudsSpeed = 0.1f;
    public Color dynamicCloudsColor1 = Color.white;
    public Color dynamicCloudsColor2 = Color.white;
    private Vector2 m_dynamicCloudsDirection = Vector2.zero;

    private void Awake()
    {
        InitializeShaderUniforms();
    }

    private void OnDisable()
    {
        // if (m_skyMaterial)
        //     m_skyMaterial.DisableKeyword("_ATMOSPERICSCATTERING");
    }

    private void LateUpdate()
    {
        // In editor only
#if UNITY_EDITOR
        // if (!Application.isPlaying)
        // {
        //     InitializeShaderUniforms();
        // }
#endif
        // Clouds movement
        //m_dynamicCloudsDirection = ComputeCloudPosition();
        if (m_skyMaterial)
            UpdateLocalShader(m_skyMaterial);
    }

    private void InitializeShaderUniforms()
    {
        // m_skyMaterial.SetTexture("_Azure_DynamicCloudTexture", m_dynamicCloudsTexture);
        // m_skyMaterial.SetTexture("_Azure_SunTexture", m_sunTexture);
        // m_skyMaterial.SetTexture("_Azure_MoonTexture", m_moonTexture);
    }

    private void UpdateLocalShader(Material mat)
    {
        //Reference
        //mat.SetTexture("_Azure_DynamicCloudTexture", m_dynamicCloudsTexture);
        // m_skyMaterial.SetTexture("_Azure_SunTexure", m_sunTexture);
        // m_skyMaterial.SetTexture("_Azure_MoonTexture", m_moonTexture);
        // mat.SetVector("_Azure_SunDirection", transform.InverseTransformDirection(-m_sunTransform.forward));
        // mat.SetVector("_Azure_MoonDirection", transform.InverseTransformDirection(-m_moonTransform.forward));
        // mat.SetMatrix("_Azure_SunMatrix", m_sunTransform.worldToLocalMatrix);
        // mat.SetMatrix("_Azure_MoonMatrix", m_moonTransform.worldToLocalMatrix);



        // mat.EnableKeyword("_ATMOSPERICSCATTERING");
        // //Scattering
        // mat.SetFloat("_Azure_FogScatteringScale", 1);
        // mat.SetFloat("_Azure_Kr", kr * 1000f);
        // mat.SetFloat("_Azure_Km", km * 1000f);
        // mat.SetVector("_Azure_Rayleigh", ComputeRayleigh() * rayleigh);
        // mat.SetVector("_Azure_Mie", ComputeMie() * mie);
        // mat.SetFloat("_MieG", mieG);
        // //mat.SetFloat("_Azure_MieDepth", mieDistance);
        // mat.SetFloat("_Azure_Scattering", scattering * 60f);



        //mat.SetFloat("_Azure_Luminance", luminance);
        //mat.SetFloat("_Azure_Exposure", exposure);
        //mat.SetColor("_Azure_RayleighColor", rayleighColor);
        //mat.SetColor("_Azure_MieColor", mieColor);
        //mat.SetColor("_Azure_ScatteringColor", scatteringColor);

        // //Clouds
        // mat.SetFloat("_Azure_DynamicCloudAltitude", dynamicCloudsAltitude);
        // mat.SetVector("_Azure_DynamicCloudDirection", m_dynamicCloudsDirection);
        // mat.SetFloat("_Azure_DynamicCloudDensity", Mathf.Lerp(25.0f, 0.0f, dynamicCloudsDensity));
        // mat.SetVector("_Azure_DynamicCloudColor1", dynamicCloudsColor1);
        // mat.SetVector("_Azure_DynamicCloudColor2", dynamicCloudsColor2);
        //
        // mat.SetInt("_Azure_ScatteringMode", (int)m_scatteringMode);
        // mat.SetMatrix("_Azure_UpDirectionMatrix", transform.worldToLocalMatrix);

        // //Outer Space
        // mat.SetFloat("_Azure_SunTextureSize", sunTextureSize);
        // mat.SetFloat("_Azure_SunTextureIntensity", sunTextureIntensity);
        // mat.SetColor("_Azure_SunTextureColor", sunTextureColor);
        // mat.SetFloat("_Azure_MoonTextureSize", moonTextureSize);
        // mat.SetFloat("_Azure_MoonTextureIntensity", moonTextureIntensity);
        // mat.SetColor("_Azure_MoonTextureColor", moonTextureColor);
    }

    /// <summary>
    /// Total rayleigh computation.
    /// </summary>
    private Vector3 ComputeRayleigh()
    {
        Vector3 rayleigh = Vector3.one;
        // Vector3 lambda = new Vector3(wavelengthR, wavelengthG, wavelengthB) * 1e-9f;
        // float n = 1.0003f; // Refractive index of air
        // float pn = 0.035f; // Depolarization factor for standard air.
        // float n2 = n * n;
        // //float N = 2.545E25f;
        // float N = molecularDensity * 1E25f;
        // float temp = (8.0f * Mathf.PI * Mathf.PI * Mathf.PI * ((n2 - 1.0f) * (n2 - 1.0f))) / (3.0f * N) * ((6.0f + 3.0f * pn) / (6.0f - 7.0f * pn));
        //
        // rayleigh.x = temp / Mathf.Pow(lambda.x, 4.0f);
        // rayleigh.y = temp / Mathf.Pow(lambda.y, 4.0f);
        // rayleigh.z = temp / Mathf.Pow(lambda.z, 4.0f);
        rayleigh = new Vector3(5.8f, 13.5f, 33.1f) * 0.000001f;
        return rayleigh;
    }

    /// <summary>
    /// Total mie computation.
    /// </summary>
    private Vector3 ComputeMie()
    {
        Vector3 mie;

        //float c = (0.6544f * Turbidity - 0.6510f) * 1e-16f;
        // float c = (0.6544f * 5.0f - 0.6510f) * 10f * 1e-9f;
        // Vector3 k = new Vector3(686.0f, 678.0f, 682.0f);
        //
        // mie.x = (434.0f * c * Mathf.PI * Mathf.Pow((4.0f * Mathf.PI) / wavelengthR, 2.0f) * k.x);
        // mie.y = (434.0f * c * Mathf.PI * Mathf.Pow((4.0f * Mathf.PI) / wavelengthG, 2.0f) * k.y);
        // mie.z = (434.0f * c * Mathf.PI * Mathf.Pow((4.0f * Mathf.PI) / wavelengthB, 2.0f) * k.z);
        mie = new Vector3(2.0f, 2.0f, 2.0f) * 0.00001f;

        return mie;
    }

    /// <summary>
    /// Returns the cloud uv position based on the direction and speed.
    /// </summary>
    private Vector2 ComputeCloudPosition()
    {
        float x = m_dynamicCloudsDirection.x;
        float z = m_dynamicCloudsDirection.y;
        float dir = Mathf.Lerp(0f, 360f, dynamicCloudsDirection);
        float windSpeed = dynamicCloudsSpeed * 0.05f * Time.deltaTime;

        x += windSpeed * Mathf.Sin(0.01745329f * dir);
        z += windSpeed * Mathf.Cos(0.01745329f * dir);

        if (x >= 1.0f) x -= 1.0f;
        if (z >= 1.0f) z -= 1.0f;

        return new Vector2(x, z);
    }
}



public enum AzureScatteringMode
{
    [Header("自动")]
    Automatic,
    [Header("手动")]
    Custom,
}
