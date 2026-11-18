using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace Baozang.Rendering.VolumetricClouds.Editor
{
    /// <summary>
    /// Custom inspector for the <see cref="VolumetricCloudsVolume"/> post-process component.
    /// Mirrors HDRP's behaviour of showing / hiding the cloud-map related fields depending on the
    /// selected <c>cloudControl</c> mode:
    ///  - Simple:   show the weather preset, hide the cloud map / LUT and the Advanced maps.
    ///  - Advanced: show the cumulus / alto-stratus / cumulonimbus / rain maps, hide the cloud map / LUT.
    ///  - Manual:   show the custom cloud map + LUT, hide the preset and the Advanced maps.
    /// All shape / density / profile-curve parameters stay visible in every mode.
    /// </summary>
    [CustomEditor(typeof(VolumetricCloudsVolume))]
    public class VolumetricCloudsVolumeEditor : VolumeComponentEditor
    {
        const int k_ModeSimple = 0;
        const int k_ModeAdvanced = 1;
        const int k_ModeManual = 2;

        // Manual mode only: user-authored cloud map + properties LUT.
        static readonly HashSet<string> s_ManualOnly = new HashSet<string> { "cloudMap", "cloudMapWorldSize", "cloudMapWorldOffset", "cloudLut" };

        // Advanced mode only: cloud-type distribution maps and their internal bake resolution.
        static readonly HashSet<string> s_AdvancedOnly = new HashSet<string>
        {
            "cumulusMap", "cumulusMapMultiplier",
            "altoStratusMap", "altoStratusMapMultiplier",
            "cumulonimbusMap", "cumulonimbusMapMultiplier",
            "rainMap", "cloudMapResolution",
        };

        // Ordered layout. Entries starting with '#' are section headers, everything else is a
        // serialized field name on VolumetricCloudsVolume.
        static readonly string[] s_Layout =
        {
            "#General",
            "enable", "localClouds", "earthCurvature",
            "#Shape - Cloud Map",
            "cloudControl", "cloudPreset", "cloudMap", "cloudMapWorldSize", "cloudMapWorldOffset", "cloudLut",
            "cumulusMap", "cumulusMapMultiplier", "altoStratusMap", "altoStratusMapMultiplier",
            "cumulonimbusMap", "cumulonimbusMapMultiplier", "rainMap", "cloudMapResolution",
            "#Shape - Density / Noise",
            "densityMultiplier", "shapeFactor", "shapeScale", "shapeOffset",
            "erosionFactor", "erosionScale", "erosionNoiseType",
            "densityCurve", "erosionCurve", "ambientOcclusionCurve", "cloudCoverage",
            "#Cloud Layer",
            "bottomAltitude", "altitudeRange", "fadeInMode", "fadeInStart", "fadeInDistance",
            "#Wind",
            "globalWindSpeed", "orientation", "altitudeDistortion",
            "cloudMapSpeedMultiplier", "shapeSpeedMultiplier", "erosionSpeedMultiplier",
            "verticalShapeWindSpeed", "verticalErosionWindSpeed",
            "#Lighting",
            "scatteringTint", "powderEffectIntensity", "multiScattering",
            "ambientLightProbeDimmer", "sunLightDimmer", "erosionOcclusion",
            "#Quality / Performance",
            "numPrimarySteps", "numLightSteps", "temporalAccumulationFactor",
            "ghostingReduction", "perceptualBlending",
            "#Shadows (未生效)",
            "shadows", "shadowResolution", "shadowPlaneHeightOffset",
            "shadowDistance", "shadowOpacity", "shadowOpacityFallback",
            "#Compositing (URP 扩展)",
            "fogDistance", "fogColor",
        };

        readonly Dictionary<string, SerializedDataParameter> m_Params = new Dictionary<string, SerializedDataParameter>();

        public override void OnEnable()
        {
            base.OnEnable();
            m_Params.Clear();
            foreach (var entry in s_Layout)
            {
                if (entry.Length > 0 && entry[0] == '#')
                    continue;
                var prop = serializedObject.FindProperty(entry);
                if (prop != null)
                    m_Params[entry] = Unpack(prop);
            }
        }

        public override void OnInspectorGUI()
        {
            int mode = m_Params.TryGetValue("cloudControl", out var ctrl) ? ctrl.value.enumValueIndex : k_ModeSimple;

            foreach (var entry in s_Layout)
            {
                if (entry.Length > 0 && entry[0] == '#')
                {
                    EditorGUILayout.Space(4f);
                    EditorGUILayout.LabelField(entry.Substring(1), EditorStyles.boldLabel);
                    continue;
                }

                if (!ShouldShow(entry, mode))
                    continue;
                if (!m_Params.TryGetValue(entry, out var param))
                    continue;

                if (entry == "cloudPreset")
                {
                    // Selecting a preset loads its values into the shape/density/curve/altitude
                    // parameters (à la HDRP), after which they remain hand-editable.
                    EditorGUI.BeginChangeCheck();
                    PropertyField(param);
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                        foreach (var t in targets)
                        {
                            var vol = (VolumetricCloudsVolume)t;
                            Undo.RecordObject(vol, "Apply Cloud Preset");
                            vol.ApplyPreset();
                            EditorUtility.SetDirty(vol);
                        }
                        serializedObject.Update();
                    }
                    continue;
                }

                PropertyField(param);
            }
        }

        static bool ShouldShow(string name, int mode)
        {
            // Weather preset is only meaningful in Simple mode.
            if (name == "cloudPreset")
                return mode == k_ModeSimple;

            // Custom cloud map / LUT: Manual only.
            if (s_ManualOnly.Contains(name))
                return mode == k_ModeManual;

            // Advanced cloud-type maps: Advanced only.
            if (s_AdvancedOnly.Contains(name))
                return mode == k_ModeAdvanced;

            return true;
        }
    }
}
