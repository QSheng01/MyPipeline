using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Sirenix.OdinInspector;

[ExecuteAlways]
public class CloudComp : MonoBehaviour
{
    [LabelText("旋转偏移")] public float offset = 0;
    [LabelText("旋转速度")] public float speed = 0.1f;
    [LabelText("云影对比度")] public float shadowThreshold;
    [LabelText("云影范围")]public float shadowDepth;
    public Material[] mat;
    public int index;

    void OnEnable()
    {
        mat = GetComponent<MeshRenderer>().sharedMaterials;
    }

    void Update()
    {
        transform.rotation = Quaternion.Euler(0, Time.time * speed + offset, 0);
    }
}

