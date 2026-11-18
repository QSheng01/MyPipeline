using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

[CanEditMultipleObjects]
[VolumeComponentEditor(typeof(PlanarReflection))]
class PlanarReflectionInspector : VolumeComponentEditor
{
    protected SerializedDataParameter m_PlanarReflectionEnable;
    protected SerializedDataParameter m_PlanarReflectionQuality;
    protected SerializedDataParameter m_PlanarHeight;
    protected SerializedDataParameter m_RenderSkyBox;
    protected SerializedDataParameter m_OpenBluropenBlur;

    protected SerializedDataParameter m_SSPREnabled;
    protected SerializedDataParameter m_SSPR_renderSkyBox;
    protected SerializedDataParameter m_FadeOutScreenBorderWidthVerticle;
    protected SerializedDataParameter m_FadeOutScreenBorderWidthHorizontal;
    protected SerializedDataParameter m_BlurRadius;

    public override void OnEnable()
    {
        var o = new PropertyFetcher<PlanarReflection>(serializedObject);
        m_PlanarReflectionEnable = Unpack(o.Find(x => x.enabled));
        m_PlanarReflectionQuality = Unpack(o.Find(x => x.quality));
        m_PlanarHeight = Unpack(o.Find(x => x.planarHeight));
        m_RenderSkyBox = Unpack(o.Find(x => x.renderSkyBox));
        m_OpenBluropenBlur = Unpack(o.Find(x => x.openBlur));

        m_SSPREnabled = Unpack(o.Find(x => x.SSPREnabled));
        m_SSPR_renderSkyBox = Unpack(o.Find(x => x.SSPR_renderSkyBox));
        m_FadeOutScreenBorderWidthVerticle = Unpack(o.Find(x => x.fadeOutScreenBorderWidthVerticle));
        m_FadeOutScreenBorderWidthHorizontal = Unpack(o.Find(x => x.fadeOutScreenBorderWidthHorizontal));
        
        m_BlurRadius = Unpack(o.Find(x => x.blurRadius));
        base.OnEnable();
    }

    public override void OnInspectorGUI()
    {
        PropertyField(m_PlanarReflectionEnable);
        EditorGUILayout.Space();
        PropertyField(m_SSPREnabled);
        if (m_SSPREnabled.value.boolValue)
        {
            PropertyField(m_SSPR_renderSkyBox);
            PropertyField(m_FadeOutScreenBorderWidthVerticle);
            PropertyField(m_FadeOutScreenBorderWidthHorizontal);
            PropertyField(m_OpenBluropenBlur);
            PropertyField(m_BlurRadius);
        }
        else
        {
            if (m_PlanarReflectionEnable.value.boolValue)
            {
                PropertyField(m_PlanarReflectionQuality);
                PropertyField(m_PlanarHeight);
                PropertyField(m_RenderSkyBox);
                PropertyField(m_OpenBluropenBlur);
            }
        }
    }
}
