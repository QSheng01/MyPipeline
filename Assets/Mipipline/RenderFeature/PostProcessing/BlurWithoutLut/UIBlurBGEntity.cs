using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class UIBlurBGEntity : MonoBehaviour
{
    void OnEnable()
    {
        UIBlurBackgroundEntityManager.AddUIBlurBGEntity(this);
    }

    void OnDisable()
    {
        UIBlurBackgroundEntityManager.RemoveUIBlurBGEntity(this);
    }
}
