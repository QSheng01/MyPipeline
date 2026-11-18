using EffectsPostProcess.Scripts.PostProcessRenderers;
using UnityEngine;

namespace EffectsPostProcess.Scripts.Controllers
{
    public partial class EffectsPostProcessController
    {

        [Header("效果过渡")]
        [Range(0, 1)]
        public float colorStyleAlpha = 0;

        [Header("黑白阈值")]
        [Range(0, 1)]
        public float grayCenter = 0.5f;
        
        [Header("黑白过渡")]
        [Range(0, 1)]
        public float grayOffset = 0;
        
        [Header("黑色区域颜色")]
        public Color blackAreaColor = Color.black;
        
        [Header("白色区域颜色")]
        public Color whiteAreaColor = Color.white;

        [Header("颜色反转")] 
        public bool colorInvert = false;

    }
}