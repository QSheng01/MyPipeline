using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MagnifierCompScripts : MonoBehaviour
{
    public Camera uiCamera;
    public float scale = 1.0f;
    public RenderTexture outputRt;
    
    private static MagnifierRenderFeature feature;
    
    // Start is called before the first frame update
    void Start()
    {
    }

    private static MagnifierRenderFeature GetMagnifierRenderFeature()
    {
        // 获取相机的渲染路径和渲染管线
        var renderPipelineAsset = GraphicsSettings.renderPipelineAsset;
            
        // 判断渲染管线是否为 URP
        if (renderPipelineAsset is UniversalRenderPipelineAsset urpAsset)
            return RendererFeatureLookup.Find<MagnifierRenderFeature>(urpAsset);
        return null;
    }
    
    // Update is called once per frame
    void Update()
    {
        // 获取Screen的像素坐标
        RectTransform rectTransform = this.GetComponent<RectTransform>();

        // 获取 RectTransform 的四个角的局部坐标
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        if (uiCamera && outputRt)
        {
            Vector3 leftDownScreePos = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]);
            Vector3 rightUpScreePos = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[2]);
            
            // 点击的屏幕中心
            Vector2 centerScreenPos = (leftDownScreePos + rightUpScreePos) / 2;

            // 点击的大小
            Vector2 rect = new Vector2();
            rect.x = rightUpScreePos.x - leftDownScreePos.x;
            rect.y = rightUpScreePos.y - leftDownScreePos.y;

            // 获取 feature
            if (!feature)
            {
                feature = GetMagnifierRenderFeature();
            }

            if (!feature)
            {
                return;
            }
            
            // 配置相关参数
            feature.m_Settings.isOpen = true;
            feature.m_Settings.scale = scale;
            feature.m_Settings.centerScreenPos = centerScreenPos;
            feature.m_Settings.rect = rect;
            // feature.m_Settings.renderTexture = outputRt;
        }
        else
        {
            CloseFeature();
        }
    }

    private void CloseFeature()
    {
        // 获取 feature
        if (!feature)
        {
            feature = GetMagnifierRenderFeature();
        }

        if (!feature)
        {
            return;
        }
        feature.m_Settings.isOpen = false;
    }
    
    private void OnDisable()
    {
        CloseFeature();
    }

    /// <summary>
    /// 获取对应的输出用的 RenderTexture
    /// </summary>
    /// <returns></returns>
    public static RenderTexture GetOutPutRenderTexture()
    {
        // 获取 feature
        if (!feature)
        {
            feature = GetMagnifierRenderFeature();
        }

        if (!feature)
        {
            return null;
        }
        
        // 返回输出的RenderTexture
        return feature.m_Settings.renderTexture;
    }
}
