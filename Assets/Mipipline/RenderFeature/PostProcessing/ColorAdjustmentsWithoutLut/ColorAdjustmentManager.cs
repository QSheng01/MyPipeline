using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ColorAdjustmentManager
{
    private static ColorAdjustmentRenderFeature feature;
    
    private static ColorAdjustmentRenderFeature GetColorAdjustmentRenderFeature()
    {
        // 获取相机的渲染路径和渲染管线
        var renderPipelineAsset = GraphicsSettings.renderPipelineAsset;
            
        // 判断渲染管线是否为 URP
        if (renderPipelineAsset is UniversalRenderPipelineAsset urpAsset)
            return RendererFeatureLookup.Find<ColorAdjustmentRenderFeature>(urpAsset);
        return null;
    }
    
    //修改画面色调
    public static void UpdateValueColor(Color color)
    {
        feature.m_Settings.color = color;
    }
    
    // 修改画面亮度
    public static void UpdateValueBrightness(float brightness)
    {
        feature.m_Settings.brightness = brightness;
    }
    
    // 修改画面对比度
    public static void UpdateValueContrast(float contrast)
    {
        feature.m_Settings.contrast = contrast;
    }
    
    // 修改画面饱和度
    public static void UpdateValueSaturation(float saturation)
    {
        feature.m_Settings.saturation = saturation;
    }
    
    // Update is called once per frame
    public static void OpenFeature()
    {
        // 获取 feature
        if (!feature)
        {
            feature = GetColorAdjustmentRenderFeature();
        }

        if (!feature)
        {
            return;
        }
        feature.m_Settings.isOpen = true;
    }
    
    public static void CloseFeature()
    {
        // 获取 feature
        if (!feature)
        {
            feature = GetColorAdjustmentRenderFeature();
        }

        if (!feature)
        {
            return;
        }
        feature.m_Settings.isOpen = false;
    }
}
