using UnityEngine;

namespace EffectsPostProcess.Scripts.Controllers
{
    public partial class EffectsPostProcessController
    {
        [Header("光圈模糊过渡")]
        [Range(0, 1)]
        public float irisBlurAlpha = 0;

        [Header("光圈模糊强度")]
        [Range(0, 1)]
        public float irisBlurStrength = 0;

        [Header("光圈模糊半径")]
        [Range(-1, 1)]
        public float irisBlurRadius = 0;

        [Header("光圈模糊中心")]
        public Vector2 irisBlurCenter = new Vector2(0.5f, 0.5f);
    }
}