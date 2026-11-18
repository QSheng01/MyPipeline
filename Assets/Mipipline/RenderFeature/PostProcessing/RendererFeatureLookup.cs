using System.Reflection;
using UnityEngine.Rendering.Universal;

internal static class RendererFeatureLookup
{
    // URP 14 does not expose its renderer data list publicly.
    private static readonly FieldInfo RendererDataListField =
        typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList",
            BindingFlags.Instance | BindingFlags.NonPublic);

    public static T Find<T>(UniversalRenderPipelineAsset asset) where T : ScriptableRendererFeature
    {
        var rendererDataList = RendererDataListField?.GetValue(asset) as ScriptableRendererData[];
        if (rendererDataList == null)
            return null;

        foreach (var rendererData in rendererDataList)
        {
            if (rendererData == null)
                continue;

            foreach (var feature in rendererData.rendererFeatures)
            {
                if (feature is T result)
                    return result;
            }
        }

        return null;
    }
}
