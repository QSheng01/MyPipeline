using UnityEngine;

internal static class VoxelVolumCommon 
{
    internal static readonly int _MAXLIGHTCOUNT = 8;

    internal static void CheckMaxLightCount(int c, Object o)
    {
        if (c > _MAXLIGHTCOUNT)
            Debug.LogWarning(
                $"场景中附加光源数超出 {_MAXLIGHTCOUNT}, 将仅保留离得最近的 {_MAXLIGHTCOUNT} 个附加光,但这会导致额外开销.",
                o
            );
    }

    internal static readonly int _MAXVOLUMECOUNT = 8;

    internal static void CheckMaxFogVolumeCount(int c, Object o)
    {
        if (c > _MAXVOLUMECOUNT)
            Debug.LogWarning(
                $"场景中局部雾效数量超出 {_MAXVOLUMECOUNT}, 将仅保留离得最近的 {_MAXVOLUMECOUNT} 个局部雾, 但这会导致额外开销.",
                o
            );
    }
}
