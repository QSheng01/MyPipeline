using System;
using System.Collections;
using System.Collections.Generic;
// using SYPackages.GraphicsLoad;
// using SYPackages.GraphicsLoad.Loader;
using SYPackages.Utillity;
using UnityEngine.Rendering.LookDev;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

public class ShotController : MonoBehaviour
{
    // public GameObject popUI;
    // public GameObject ScreenShotRaw;
    public RawImage testRawImage;

    private static ScreenShotsFeature feature;
    private static ScreenShotsFeature GetBlurWithoutLutRenderFeature()
    {
        // 获取相机的渲染路径和渲染管线
        var renderPipelineAsset = GraphicsSettings.renderPipelineAsset;

        // 判断渲染管线是否为 URP
        if (renderPipelineAsset is UniversalRenderPipelineAsset urpAsset)
            return RendererFeatureLookup.Find<ScreenShotsFeature>(urpAsset);
        return null;
    }

    public static void TakeScreenShot(RawImage pTestRawImage)
    {
        if (feature == null)
        {
            feature = GetBlurWithoutLutRenderFeature();
        }

        if (feature != null)
        {
            feature.CaptureScreenShotBySetting(
                (rt) =>
                {
                    pTestRawImage.texture = rt;
                });
        }
    }

    void Start()
    {
        // 测试拍照效果
        if (testRawImage)
        {
            StartCoroutine(AsyncTakeScreenShot());
        }
    }

    IEnumerator AsyncTakeScreenShot()
    {
        yield return new WaitForEndOfFrame();
        if (testRawImage)
        {
            TakeScreenShot(testRawImage);
            StartCoroutine(OpenTestScreenShot());
        }
    }

    // 帧结束后，打开界面
    IEnumerator OpenTestScreenShot()
    {
        yield return new WaitForEndOfFrame();
        if (testRawImage)
        {
            testRawImage.gameObject.SetActive(true);
        }
    }
}
