using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MagnifierStoreResource : MonoBehaviour
{
    public Material magnifierMat;

    private MagnifierRenderFeature feature;
    
    // Start is called before the first frame update
    void Start()
    {
        // // 获取 feature
        // if (!feature)
        // {
        //     feature = GetMagnifierRenderFeature();
        // }
        //
        // if (!feature)
        // {
        //     return;
        // }
        //
        // // 配置相关参数
        // feature.m_Settings.blitMat = magnifierMat;
    }

    // MagnifierRenderFeature GetMagnifierRenderFeature()
    // {
    //     // 获取相机的渲染路径和渲染管线
    //     var renderPipelineAsset = GraphicsSettings.renderPipelineAsset;
    //         
    //     // 判断渲染管线是否为 URP
    //     if (renderPipelineAsset != null && renderPipelineAsset.GetType() == typeof(UniversalRenderPipelineAsset)) {
    //         // 将渲染管线转换为 UniversalRenderPipelineAsset 类型
    //         var urpAsset = (UniversalRenderPipelineAsset) renderPipelineAsset;
    //
    //         // 获取 URP 渲染路径数据
    //         var rendererDataList = urpAsset.m_RendererDataList;
    //         for (int i = 0; i < rendererDataList.Length; i++)
    //         {
    //             var rendererData = rendererDataList[i];
    //             foreach (var rendererDataRendererFeature in rendererData.rendererFeatures)
    //             {
    //                 if (rendererDataRendererFeature.name == "MagnifierRenderFeature")
    //                 {
    //                     MagnifierRenderFeature feature = rendererDataRendererFeature as MagnifierRenderFeature;
    //                     return feature;
    //                 }
    //             }
    //         }
    //     }
    //     return null;
    // }
}
