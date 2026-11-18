using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Illusion.Rendering.PRTGI
{
    [Serializable]
    [VolumeComponentMenu("PipelineCore/PRT Reflection Normalization")]
    public sealed class PRTReflectionNormalization : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enable = new BoolParameter(false);
        public ClampedFloatParameter minNormalizationFactor = new ClampedFloatParameter(0.1f, 0.01f, 1f);
        public ClampedFloatParameter maxNormalizationFactor = new ClampedFloatParameter(1f, 1f, 10f);
        public ClampedFloatParameter probeVolumeWeight = new ClampedFloatParameter(1f, 0f, 1f);

        public bool IsActive() => enable.value && probeVolumeWeight.value > 0f;
        public bool IsTileCompatible() => true;
    }
}
