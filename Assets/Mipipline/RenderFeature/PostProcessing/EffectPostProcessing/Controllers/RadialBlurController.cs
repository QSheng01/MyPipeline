using UnityEngine;

namespace EffectsPostProcess.Scripts.Controllers
{
    public partial class EffectsPostProcessController
    {
        [Header("径向模糊过渡")]
        [Range(0, 1)]
        public float radialBlurAlpha = 0;
    
        // [Header("径向模糊降采样")]
        // [Range(0, 3)]
        // public int radialBlurDownSample = 0;

        [Header("径向模糊强度")]
        [Range(0, 1)]
        public float radialBlurStrength = 0;
        
        [Header("径向模糊半径")]
        [Range(-1, 1)]
        public float radialBlurRadius = 0;
        
        [Header("径向模糊中心")]
        public Vector2 radialBlurCenter = new Vector2(0.5f , 0.5f);

        [Header("启用色散")] 
        public bool radialBlurRGBSplitEnable = false;

        [Header("色散强度")]
        [Range(0, 0.3f)]
        public float radialBlurRGBSplitStrength = 0;

        [Header("R 偏移")]
        [Range(0, 1.0f)]
        public float radialBlurROffset = 0;
        
        [Header("G 偏移")]
        [Range(0, 1.0f)]
        public float radialBlurGOffset = 0;
        
        [Header("B 偏移")]
        [Range(0, 1.0f)]
        public float radialBlurBOffset = 0;
    }
}