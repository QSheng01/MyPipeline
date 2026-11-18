//using DG.DemiLib;
using UnityEngine;

namespace EffectsPostProcess.Scripts.Controllers
{
    public partial class EffectsPostProcessController
    {
        [Header("颜色")]
        [ColorUsageAttribute(false, true)]
        public Color colorFilter =Color.white;

        [Header("对比度")]
        [Range(0, 2)]
        public float contrast =1;

        [Header("饱和度")]
        [Range(0, 2)]
        public float saturation =1;

        [Header("明度")]
        [Range(-2,2)]
        public float brightness =0;
    }
}