
using UnityEditor;
using UnityEditor.Rendering;
// using UnityEditor.Rendering.PostProcessing;
using UnityEngine;

namespace SYPackages.AtmosphericScattering
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AtmosphericScatteringFog))]
    class FogEditor : VolumeComponentEditor
    {
        protected SerializedDataParameter m_islinearFog;
        protected SerializedDataParameter m_linearFogColor;
        protected SerializedDataParameter m_linearFogMode;
        protected SerializedDataParameter m_linearFogStartDistance;
        protected SerializedDataParameter m_linearFogEndDistance;
        protected SerializedDataParameter m_linearFogDensity;

        protected SerializedDataParameter m_Enabled;
        protected SerializedDataParameter m_MaxFogDistance;
        // protected SerializedDataParameter m_DownSampleMode;
        // protected SerializedDataParameter m_renderMode;

        protected SerializedDataParameter m_ColorMode;
        protected SerializedDataParameter m_tint;
        protected SerializedDataParameter m_FogStartColor;
        protected SerializedDataParameter m_FogEndColor;
        protected SerializedDataParameter m_FogFallOff;
        protected SerializedDataParameter m_FogStartDistance;
        protected SerializedDataParameter m_Jitter;
        protected SerializedDataParameter m_MipFogNear;
        protected SerializedDataParameter m_MipFogFar;
        protected SerializedDataParameter m_MipFogMaxMip;
        protected SerializedDataParameter m_Albedo;
        protected SerializedDataParameter m_MeanFreePath;
        protected SerializedDataParameter m_BaseHeight;
        protected SerializedDataParameter m_MaximumHeight;

        protected SerializedDataParameter m_EnableSun;
        protected SerializedDataParameter m_SunColor;
        protected SerializedDataParameter m_MoonColor;
        protected SerializedDataParameter m_SunFallOff;
        protected SerializedDataParameter m_MoonFallOff;


        protected SerializedDataParameter m_EnableVolumetricFog;
        protected SerializedDataParameter m_Anisotropy;
        protected SerializedDataParameter m_DepthExtent;
        protected SerializedDataParameter m_GlobalLightProbeDimmer;
        protected SerializedDataParameter m_SliceDistributionUniformity;
        //protected SerializedDataParameter m_FogControlMode;
        //protected SerializedDataParameter m_ScreenResolutionPercentage;
        //protected SerializedDataParameter m_VolumeSliceCount;
        //protected SerializedDataParameter m_VolumetricFogBudget;
        //protected SerializedDataParameter m_ResolutionDepthRatio;
       // protected SerializedDataParameter m_DirectionalLightsOnly;
        protected SerializedDataParameter m_DenoisingMode;


        static GUIContent s_Enabled = new GUIContent("开启高度雾", "Check this to enable fog in your scene.");
        static GUIContent s_AlbedoLabel = new GUIContent("高度雾颜色", "Specifies the color this fog scatters light to.");

        static GUIContent s_MeanFreePathLabel = new GUIContent("高度雾衰减距离",
            "Controls the density at the base level (per color channel). Distance at which fog reduces background light intensity by 63%. Units: m.");
        static GUIContent s_FogStartDistanceLabel = new GUIContent("高度雾起始距离",
            "None.");

        static GUIContent s_DownSampleLabel = new GUIContent("Down Samping Mode",
            "None.");
        static GUIContent s_RenderModeLabel = new GUIContent("Render Mode",
            "None.");
        static GUIContent s_JitterStrengthLabel = new GUIContent("高度雾噪声强度",
            "None.");

        static GUIContent s_BaseHeightLabel = new GUIContent("高度雾起始高度",
            "Reference height (e.g. sea level). Sets the height of the boundary between the constant and exponential fog.");

        static GUIContent s_MaximumHeightLabel = new GUIContent("高度雾最大高度",
            "Max height of the fog layer. Controls the rate of height-based density falloff. Units: m.");

        static GUIContent s_AnisotropyLabel = new GUIContent("Anisotropy",
            "Controls the angular distribution of scattered light. 0 is isotropic, 1 is forward scattering, and -1 is backward scattering.");

        static GUIContent s_GlobalLightProbeDimmerLabel = new GUIContent("Ambient Light Probe Dimmer",
            "Controls the intensity reduction of the global Light Probe that the sky generates.");

        static GUIContent s_EnableSun =
            new GUIContent("高度雾光照影响", "When enabled, activates Sun.");
        static GUIContent s_SunColor =
            new GUIContent("太阳高度雾光照颜色", "Sun Color.");
        static GUIContent s_SunFallOff =
            new GUIContent("太阳高度雾光照范围", "Sun Color Fall Off.");


        static GUIContent s_MoonColor =
            new GUIContent("月亮高度雾光照颜色", "Sun Color.");
        static GUIContent s_MoonFallOff =
            new GUIContent("月亮高度雾光照范围", "Sun Color Fall Off.");

        static GUIContent s_EnableVolumetricFog =
            new GUIContent("Volumetric AtmosphericScatteringFog", "When enabled, activates volumetric fog.");

        static GUIContent s_DepthExtentLabel = new GUIContent("Volumetric AtmosphericScatteringFog Distance",
            "Sets the distance (in meters) from the Camera's Near Clipping Plane to the back of the Camera's volumetric lighting buffer. The lower the distance is, the higher the fog quality is.");

        //public override bool hasAdvancedMode => true;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<AtmosphericScatteringFog>(serializedObject);

            m_islinearFog= Unpack(o.Find(x => x.isLinearFog));
            m_linearFogColor= Unpack(o.Find(x => x.linearFogColor));
            m_linearFogMode= Unpack(o.Find(x => x.linearFogMode));
            m_linearFogStartDistance= Unpack(o.Find(x => x.linearFogStartDistance));
            m_linearFogEndDistance= Unpack(o.Find(x => x.linearFogEndDistance));
            m_linearFogDensity= Unpack(o.Find(x => x.linearfogDensity));

            m_Enabled = Unpack(o.Find(x => x.enabled));
            m_MaxFogDistance = Unpack(o.Find(x => x.maxFogDistance));
            // m_DownSampleMode = Unpack(o.Find(x => x.downsampleMode));
            // m_renderMode = Unpack(o.Find(x => x.renderMode));
            // AtmosphericScatteringFog Color
            m_ColorMode = Unpack(o.Find(x => x.colorMode));
            m_tint = Unpack(o.Find(x => x.tint));
            m_FogStartColor = Unpack(o.Find(x => x.FogStartColor));
            m_FogEndColor = Unpack(o.Find(x => x.FogEndColor));
            m_FogFallOff = Unpack(o.Find(x => x.FogFallOff));
            m_FogStartDistance = Unpack(o.Find(x => x.fogStartDistance));
            m_Jitter = Unpack(o.Find(x => x.jitterStrength));
            m_MipFogNear = Unpack(o.Find(x => x.mipFogNear));
            m_MipFogFar = Unpack(o.Find(x => x.mipFogFar));
            m_MipFogMaxMip = Unpack(o.Find(x => x.mipFogMaxMip));
            m_Albedo = Unpack(o.Find(x => x.albedo));
            m_MeanFreePath = Unpack(o.Find(x => x.meanFreePath));
            m_BaseHeight = Unpack(o.Find(x => x.baseHeight));
            m_MaximumHeight = Unpack(o.Find(x => x.maximumHeight));
            m_Anisotropy = Unpack(o.Find(x => x.anisotropy));
            m_GlobalLightProbeDimmer = Unpack(o.Find(x => x.globalLightProbeDimmer));
            m_EnableVolumetricFog = Unpack(o.Find(x => x.enableVolumetricFog));
            m_DepthExtent = Unpack(o.Find(x => x.depthExtent));
            m_SliceDistributionUniformity = Unpack(o.Find(x => x.sliceDistributionUniformity));

            m_EnableSun = Unpack(o.Find(x => x.enableSun));
            m_SunColor = Unpack(o.Find(x => x.sunColor));
            m_MoonColor = Unpack(o.Find(x => x.moonColor));
            m_SunFallOff = Unpack(o.Find(x => x.sunFallOff));
            m_MoonFallOff = Unpack(o.Find(x => x.moonFallOff));


            // m_FogControlMode = Unpack(o.Find(x => x.fogControlMode));
            // m_ScreenResolutionPercentage = Unpack(o.Find(x => x.screenResolutionPercentage));
            // m_VolumeSliceCount = Unpack(o.Find(x => x.volumeSliceCount));
            // m_VolumetricFogBudget = Unpack(o.Find(x => x.volumetricFogBudget));
            // m_ResolutionDepthRatio = Unpack(o.Find(x => x.resolutionDepthRatio));
            // m_DirectionalLightsOnly = Unpack(o.Find(x => x.directionalLightsOnly));

            m_DenoisingMode = Unpack(o.Find(x => x.denoisingMode));

            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("线性雾");
            PropertyField(m_islinearFog, new GUIContent("开启线性雾"));
            if (m_islinearFog.value.boolValue)
            {
                using (new IndentScope())
                {
                    PropertyField(m_linearFogColor, new GUIContent("线性雾颜色"));
                    PropertyField(m_linearFogMode, new GUIContent("线性雾模式"));
                    switch ((FogMode)m_linearFogMode.value.intValue)
                    {
                        case FogMode.Exponential:
                        case FogMode.ExponentialSquared:
                            PropertyField(m_linearFogDensity, new GUIContent("线性雾强度"));
                            break;
                        case FogMode.Linear:
                            PropertyField(m_linearFogStartDistance, new GUIContent("线性雾起始距离"));
                            PropertyField(m_linearFogEndDistance, new GUIContent("线性雾最远距离"));
                            break;
                    }
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("高度雾");
            PropertyField(m_Enabled, s_Enabled);
            if (m_Enabled.value.boolValue)
            {
                using (new IndentScope())
                {
                    // PropertyField(m_DownSampleMode, s_DownSampleLabel);
                    // PropertyField(m_renderMode,s_RenderModeLabel);
                    PropertyField(m_MeanFreePath, s_MeanFreePathLabel);
                    PropertyField(m_FogStartDistance, s_FogStartDistanceLabel);
                    PropertyField(m_BaseHeight, s_BaseHeightLabel);
                    PropertyField(m_MaximumHeight, s_MaximumHeightLabel);
                    PropertyField(m_MaxFogDistance);

                    if (m_MaximumHeight.value.floatValue < m_BaseHeight.value.floatValue)
                    {
                        m_MaximumHeight.value.floatValue = m_BaseHeight.value.floatValue;
                        serializedObject.ApplyModifiedProperties();
                    }

                    //PropertyField(m_ColorMode);

                    //using (new IndentScope())
                    {
                        // if (!m_ColorMode.value.hasMultipleDifferentValues &&
                        //     (FogColorMode) m_ColorMode.value.intValue == FogColorMode.ConstantColor)
                        // {
                        // PropertyField(m_FogStartColor);
                        // PropertyField(m_FogEndColor);
                        // PropertyField(m_FogFallOff);

                        //}
                        //else
                        //{
                        PropertyField(m_tint, new GUIContent("高度雾颜色"));
                        //if (isInAdvancedMode)
                        //{
                        // PropertyField(m_MipFogNear);
                        // PropertyField(m_MipFogFar);
                        // PropertyField(m_MipFogMaxMip);
                        //}
                        //}
                    }

                    PropertyField(m_Jitter);

                    PropertyField(m_EnableSun, s_EnableSun);
                    if (m_EnableSun.value.boolValue)
                    {
                        using (new IndentScope())
                        {
                            PropertyField(m_SunColor, s_SunColor);
                            PropertyField(m_SunFallOff, s_SunFallOff);

                            PropertyField(m_MoonColor, s_MoonColor);
                            PropertyField(m_MoonFallOff, s_MoonFallOff);
                        }
                    }

                    // bool volumetricLightingAvailable = false;
                    // // var hdpipe = HDRenderPipeline.currentAsset;
                    // // if (hdpipe != null)
                    // //     volumetricLightingAvailable = hdpipe.currentPlatformRenderPipelineSettings.supportVolumetrics;
                    // volumetricLightingAvailable = true;
                    // if (volumetricLightingAvailable)
                    // {
                    //     PropertyField(m_EnableVolumetricFog, s_EnableVolumetricFog);
                    //
                    //     using (new IndentScope())
                    //     {
                    //         PropertyField(m_Albedo, s_AlbedoLabel);
                    //         PropertyField(m_GlobalLightProbeDimmer, s_GlobalLightProbeDimmerLabel);
                    //         PropertyField(m_DepthExtent, s_DepthExtentLabel);
                    //         PropertyField(m_DenoisingMode);
                    //
                    //         //if (isInAdvancedMode)
                    //         {
                    //             PropertyField(m_SliceDistributionUniformity);
                    //
                    //             //base.OnInspectorGUI(); // Quality Setting
                    //
                    //             using (new IndentScope())
                    //                 // using (new QualityScope(this))
                    //                 // {
                    //                 //     PropertyField(m_FogControlMode);
                    //                 //
                    //                 //     using (new HDEditorUtils.IndentScope())
                    //                 //     {
                    //                 //         if ((FogControl) m_FogControlMode.value.intValue == FogControl.Balance)
                    //                 //         {
                    //                 //             PropertyField(m_VolumetricFogBudget);
                    //                 //             PropertyField(m_ResolutionDepthRatio);
                    //                 //         }
                    //                 //         else
                    //                 //         {
                    //                 //             PropertyField(m_ScreenResolutionPercentage);
                    //                 //             PropertyField(m_VolumeSliceCount);
                    //                 //         }
                    //                 //     }
                    //                 // }
                    //                 //PropertyField(m_DirectionalLightsOnly);
                    //
                    //                 PropertyField(m_Anisotropy, s_AnisotropyLabel);
                    //             if (m_Anisotropy.value.floatValue != 0.0f)
                    //             {
                    //                 EditorGUILayout.Space();
                    //                 EditorGUILayout.HelpBox(
                    //                     "When the value is not 0, the anisotropy effect significantly increases the performance impact of volumetric fog.",
                    //                     MessageType.Info, wide: true);
                    //             }
                    //         }
                    //     }
                    // }
                }
            }
        }

        internal class IndentScope : GUI.Scope
        {
            int m_Offset;

            public IndentScope(int offset = 16)
            {
                m_Offset = offset;

                // When using EditorGUI.indentLevel++, the clicking on the checkboxes does not work properly due to some issues on the C++ side.
                // This scope is a work-around for this issue.
                GUILayout.BeginHorizontal();
                EditorGUILayout.Space(offset, false);
                GUILayout.BeginVertical();
                EditorGUIUtility.labelWidth -= m_Offset;
            }

            protected override void CloseScope()
            {
                EditorGUIUtility.labelWidth += m_Offset;
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
        }



    }

}