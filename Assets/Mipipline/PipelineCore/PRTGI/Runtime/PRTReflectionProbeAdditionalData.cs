using UnityEngine;
using UnityEngine.Rendering;

namespace Illusion.Rendering.PRTGI
{
    [ExecuteAlways]
    [RequireComponent(typeof(ReflectionProbe))]
    public sealed class PRTReflectionProbeAdditionalData : MonoBehaviour
    {
        [SerializeField] private bool hasValidSHForNormalization;
        [SerializeField] private SphericalHarmonicsL2 shForNormalization;

        public bool HasValidData => hasValidSHForNormalization;

        public void SetSHCoefficients(float[] coefficients)
        {
            if (coefficients == null || coefficients.Length != 27)
                throw new System.ArgumentException("PRT reflection normalization requires 27 SH channel values.");
            for (int coefficient = 0; coefficient < 9; coefficient++)
                for (int channel = 0; channel < 3; channel++)
                    shForNormalization[channel, coefficient] = coefficients[coefficient * 3 + channel];
            hasValidSHForNormalization = true;
        }

        public void Clear()
        {
            hasValidSHForNormalization = false;
            shForNormalization = default;
        }

        public bool TryGetLuminanceSH(out Vector4 l0l1, out Vector4 l2, out float l2Last)
        {
            l0l1 = l2 = Vector4.zero;
            l2Last = 0;
            if (!hasValidSHForNormalization) return false;
            float Luma(int coefficient) => 0.2126f * shForNormalization[0, coefficient] +
                0.7152f * shForNormalization[1, coefficient] + 0.0722f * shForNormalization[2, coefficient];
            l0l1 = new Vector4(Luma(0) - Luma(6), Luma(1), Luma(2), Luma(3));
            l2 = new Vector4(Luma(4), Luma(5), Luma(6) * 3f, Luma(7));
            l2Last = Luma(8);
            return true;
        }
    }
}
