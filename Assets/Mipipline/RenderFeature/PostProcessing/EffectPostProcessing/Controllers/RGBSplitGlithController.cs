using System;
using UnityEngine;

namespace EffectsPostProcess.Scripts.Controllers
{
    public partial class EffectsPostProcessController
    {
        /*[Header("色散效果过渡")]
        [Range(0, 1.0f)]
        public float rgbSplitGlithAlpha = 0;*/

        [Header("分离方向")]
        public SplitDirection Splitdirection = SplitDirection.Horizontal;

        [Header("分离程度")]
        [Range(0, 5f)]
        public float Amount = 0;

        [Header("分离速度")]
        [Range(0, 50f)]
        public float Speed =0f;

        [Header("分离衰减")]
        [Range(0, 1f)]
        public float Fading = 1f;

        [Header("中心衰减")]
        [Range(0, 1f)]
        public float CenterFading = 1f;

        [Header("R通道")]
        [Range(0, 4f)]
        public float AmountR = 0f;
        [Header("B通道")]
        [Range(0, 4f)]
        public float AmountB = 0f;

    }

    [Serializable]
    public enum SplitDirection
    {
        Horizontal,
        Vertical,
        Horizontal_Vertical,
    }
}