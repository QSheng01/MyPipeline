using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[ExecuteAlways]
public class Flare
{
    public string Comment = "";
	public bool IsActive = true;
    public FlareAtlas Atlas; // flare atlas
    public int Index; // flare texture
    [MinMaxSlider(0.0f, 15f)]
    public Vector2 ScaleRange;
    public Color Color; // 颜色
    [Range(0, 360)]
    public float Rotation; // 初始的旋转值，即正对着阳光时的姿势
    public bool RotateWith; // 随着视角变化，是否跟随转动 
    public bool OverridePixelPerUnit; // 
    public float PixelPerUnit;

    [Range(-5, 10)]
    public float Distance = 0; // 一个相对于光源的偏移距离
    [HideInInspector]
    public const float DistanceAmount = 10;

    public float DistanceAspect
    {
        get
        {
            return Distance / DistanceAmount;
        }
    }

    public FlareTexture Texture
    {
        get
        {
            if (Index < Atlas.SubTextures.Count)
                return Atlas.SubTextures[Index];
            return null;
        }
    }
}

// 光源
public class FlareSource : MonoBehaviour
{
    [SerializeField] private Camera m_GameCamera;
    //[SerializeField] private Camera m_FlareCamera;
    [SerializeField] private Light m_MainLight;

    public float PixelPerUnit = 100; // 用来控制缩放，在flare space生成网格的大小依据
    public AnimationCurve FadeCurve; // 淡入淡出的曲线
    public float FadeDuration;// 淡入淡出的时间间隔
    public AnimationCurve ScaleCurve;
    public AnimationCurve AlphaCurve;
    [Range(0.1f, 10)]
    public float SpreadAmount = 5; // 总长度/光源到光晕中心的长度，用来求得总长度
    public float SpreadMaximum = 5; // 最大扩散长度，单位Unit

    public Vector2 Center;

    [TextArea(1, 10)]
    public string Comment;
    public List<Flare> Flares;

    private Vector3 m_ViewportPosition;
    public Vector3 ViewportPosition
    {
        get
        {
            return m_ViewportPosition;
        }
    }

    private float m_AlphaBase = 0f;
    public float AlphaBase
    {
        get
        {
            return m_AlphaBase;
        }
    }
    private float m_FadeTime = 0;

    public bool IsVisible
    {
        get;private set;
    }

    private bool IsHitLast = true;

    // Start is called before the first frame update
    void Start()
    {
        IsHitLast = true;
        IsVisible = false;
        if (m_GameCamera == null)
            m_GameCamera = Camera.main;
        if (m_MainLight == null)
            m_MainLight = RenderSettings.sun;
    }

    void Update()
    {
        // m_GameCamera.transform.
        // Vector3 position = transform.position;
        Vector3 direction = m_MainLight.transform.TransformDirection(new Vector3(0, 0, -1)); //太阳的朝向
        Vector3 position = m_GameCamera.transform.position + direction * m_GameCamera.farClipPlane;
        m_ViewportPosition = m_GameCamera.WorldToViewportPoint(position);

        Ray ray = new Ray(m_GameCamera.transform.position, position);
        // Ray ray = new Ray(m_GameCamera.transform.position, position - m_GameCamera.transform.position);

        RaycastHit hit;
        //射线拾取遮挡或者不处于相机范围内
        int layerMask = (1 << LayerMask.NameToLayer("Default")) | (1 << LayerMask.NameToLayer("Role"));
        if (!IsVisableInCamera(position) || Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
        {
            //如果当前在显示，则计算渐变时间，设置渐变效果直至隐藏
            if (IsVisible)
            {
                if (!IsHitLast)
                    m_FadeTime = (1 - m_AlphaBase) * FadeDuration;

                m_FadeTime += Time.deltaTime;
                float fac = GetFadeCurveValue(m_FadeTime / FadeDuration);
                m_AlphaBase = Mathf.Lerp(1, 0, fac);

                if (m_AlphaBase <= 0)
                {
                    IsVisible = false;
                    m_FadeTime = 0;
                    m_AlphaBase = 0;
                }
            }
            IsHitLast = true;
        }
        else
        {
            if (IsHitLast)
                m_FadeTime = m_AlphaBase * FadeDuration;

            if (m_AlphaBase < 1)
            {
                m_FadeTime += Time.deltaTime;
                float fac = GetFadeCurveValue(m_FadeTime / FadeDuration);
                m_AlphaBase = Mathf.Lerp(0, 1, fac);

                if (m_AlphaBase >= 1)
                {
                    m_FadeTime = 0;
                    m_AlphaBase = 1;
                }
            }
            IsVisible = true;
            IsHitLast = false;
        }
        Debug.DrawLine(position, m_GameCamera.transform.position, IsHitLast ? Color.red : Color.white);
    }
    
    /// <summary>
    /// 判断一个点是否处于视椎体内
    /// </summary>
    /// <param name="pos">世界坐标下的位置坐标</param>
    /// <returns>布尔值，在显示范围为true</returns>
    public bool IsVisableInCamera(Vector3 pos)
    {
        //转化为视角坐标
        Vector3 viewPos = m_GameCamera.WorldToViewportPoint(pos);
        // z<0代表在相机背后
        if (viewPos.z < 0) return false;
        //太远了！看不到了！
        if (viewPos.z > m_GameCamera.farClipPlane)
            return false;
        // x,y取值在 0~1之外时代表在视角范围外；
        if (viewPos.x < 0 || viewPos.y < 0 || viewPos.x > 1 || viewPos.y > 1) return false;
        return true;
    }


    // 获取flare space下的初始大小，用来构建mesh
    public Vector2 GetFlareSize(Flare flare)
    {
        Vector2 size = flare.Texture.PixelSize;
        if(flare.OverridePixelPerUnit && flare.PixelPerUnit > 0)
        {
            size /= flare.PixelPerUnit;
        }
        else
        {
            size /= PixelPerUnit;
        }
        return size;
    }

    public float GetScaleCurveValue(float fac)
    {
        return ScaleCurve.Evaluate(fac);
    }

    public float GetAlphaCurveValue(float fac)
    {
        return AlphaCurve.Evaluate(fac);
    }

    public float GetFadeCurveValue(float fac)
    {
        return FadeCurve.Evaluate(fac);
    }

}
