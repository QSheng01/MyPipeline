using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

[ExecuteAlways]
public class CloudContorl : MonoBehaviour
{


    [Range(0f, 1f)]
    public float switchSpeed = 0.1f;
    [Sirenix.OdinInspector.ReadOnly]public int CurrentCloudIndex = 0;
    public List<CloudComp> AllCloud;
    [BoxGroup("CloudShadow")]
    public float CloudShadowSize = 1f;
    [BoxGroup("CloudShadow")]
    public float CloudShadowMoveSpeed = 1f;
    [BoxGroup("CloudShadow")]
    public Texture2D CloudShadowTexture;

    [BoxGroup("CloudShadow")] [LabelText("启用云影")]
    public bool UseCloudShadow = true;


    [Sirenix.OdinInspector.ReadOnly]public float cloudShadowDepth = 0f;
    [Sirenix.OdinInspector.ReadOnly]public float cloudShadowThreshold = 0f;


    int dissolveProp = Shader.PropertyToID("_Dissolve");

    IEnumerator showCloudIE;



    void Start()
    {
        // 配置云投影的全局变量
        if (CloudShadowTexture != null)
        {
            Shader.SetGlobalTexture("_CloudNoiseMap", CloudShadowTexture);
        }
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) // 只有在编辑模式下 调整数值才有用，运行状态走协程
        {
            SetCloudShadowData();
            // 编辑模式下，强制不能开启其他的云
            for (int i = 0; i < AllCloud.Count; i++)
            {
                if (i != CurrentCloudIndex)
                {
                    AllCloud[i].gameObject.SetActive(false);
                }
                else
                {
                    AllCloud[i].gameObject.SetActive(true);
                }
            }
        }
        Shader.SetGlobalInt("_UseCloudShadow", UseCloudShadow? 1 : 0);
#endif

    }

    [Button("切换云")]
    public void SwitchCloud(int targetIndex)
    {
        if(targetIndex >= AllCloud.Count || targetIndex < 0)
        {
            Debug.LogError("切换云失败，目标云索引超出范围");
            return;
        }

        // 每个云自己记录一下index
        AllCloud[CurrentCloudIndex].index = CurrentCloudIndex;
        AllCloud[targetIndex].index = targetIndex;

#if UNITY_EDITOR
        if (Application.isPlaying)
#endif
        {
            if (showCloudIE != null) // 当前正在切换云，无法启动新的切换云
            {
                return;
            }
            showCloudIE = ShowCloud(AllCloud[CurrentCloudIndex], AllCloud[targetIndex]);
            StartCoroutine(showCloudIE);
        }
#if UNITY_EDITOR
        else
        {
            SwitchCloudImmediate(targetIndex);
        }
#endif
    }

    private void SwitchCloudImmediate(int targetIndex)
    {
        AllCloud[CurrentCloudIndex].gameObject.SetActive(false);
        AllCloud[targetIndex].gameObject.SetActive(true);

        SetMaterialDissolve(AllCloud[CurrentCloudIndex].mat, 2f);
        SetMaterialDissolve(AllCloud[targetIndex].mat, 0f);

        cloudShadowDepth = AllCloud[targetIndex].shadowDepth;
        cloudShadowThreshold = AllCloud[targetIndex].shadowThreshold;

        CurrentCloudIndex = targetIndex;
    }

    float dissolveValue = 0;
    IEnumerator ShowCloud(CloudComp cloud0, CloudComp cloud1)
    {
        cloud0.gameObject.SetActive(true);
        cloud1.gameObject.SetActive(true);

        while (dissolveValue <= 2)
        {
            // 设置溶解
            SetMaterialDissolve(cloud0.mat, dissolveValue);
            SetMaterialDissolve(cloud1.mat, 2 - dissolveValue); // 默认2是全溶解

            float lerp = dissolveValue / 2f;

            // 设置云影
            cloudShadowThreshold = Mathf.Lerp(cloud0.shadowThreshold, cloud1.shadowThreshold, lerp);
            cloudShadowDepth = Mathf.Lerp(cloud0.shadowDepth, cloud1.shadowDepth, lerp);
            SetCloudShadowData(cloudShadowThreshold, cloudShadowDepth);

            dissolveValue += Time.deltaTime * switchSpeed;
            yield return null;
        }
        cloud0.gameObject.SetActive(false);
        cloud1.gameObject.SetActive(true);

        dissolveValue = 0;
        showCloudIE = null; // 携程结束
        CurrentCloudIndex = cloud1.index;
    }

    public void SetCloudShadowData()
    {
        Shader.SetGlobalVector("_CloudMoveUV", new Vector4(CloudShadowMoveSpeed, CloudShadowMoveSpeed));
        Shader.SetGlobalFloat("_CloudShadowSize", CloudShadowSize);
        SetCloudShadowData(AllCloud[CurrentCloudIndex].shadowThreshold, AllCloud[CurrentCloudIndex].shadowDepth);
    }

    public void SetCloudShadowData(float threshold, float depth)
    {
        Shader.SetGlobalFloat("_CloudMapThreshold", threshold);
        Shader.SetGlobalFloat("_CloudMapDepth", depth);

    }

    /// <summary>
    /// 设置云的溶解，同时设置云和Depth两个材质球
    /// </summary>
    private void SetMaterialDissolve(Material[] mat, float dissolve)
    {
        for (int i = 0; i < mat.Length; i++)
        {
            mat[i].SetFloat(dissolveProp, dissolve);
        }
    }
}
