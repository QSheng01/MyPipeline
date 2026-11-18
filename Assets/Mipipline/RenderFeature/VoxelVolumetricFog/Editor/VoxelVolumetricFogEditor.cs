using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

[CustomEditor(typeof(VoxelVolumetricFog))]
public class VoxelVolumetricFogEditor : VolumeComponentEditor
{
    SerializedDataParameter mode;

    SerializedDataParameter qualityLevel;
    SerializedDataParameter gridPixelSize;
    SerializedDataParameter gridSizeZ;
    
    SerializedDataParameter maxDistanceVolumetric;
    SerializedDataParameter fogAttenuation;
    SerializedDataParameter overrideDefaultMaxLightDistance;
    SerializedDataParameter maxLightDistance;

    SerializedDataParameter denoisingMode;
    SerializedDataParameter temporalAALighting;
    SerializedDataParameter temporalAAMedia;
    
    SerializedDataParameter fogDensity;
    SerializedDataParameter fogAlbedo;
    SerializedDataParameter anisotropy;

    SerializedDataParameter lightIntensity;
    SerializedDataParameter shadowIntensity;
    SerializedDataParameter baseHeight;
    private SerializedDataParameter attenuationBoundarySize;
    
    SerializedDataParameter litColor;
    SerializedDataParameter shadowedColor;
    SerializedDataParameter colorInfluence;

    SerializedDataParameter useNoise; 
    SerializedDataParameter noise3DTexture;
    SerializedDataParameter noiseTiling;
    SerializedDataParameter noiseWindSpeed;
    SerializedDataParameter noiseReMap;
    
    //GodRay
    SerializedDataParameter godRayColor;
    SerializedDataParameter godRayIntensity;
    SerializedDataParameter godRaySkyBoxExtinc;
    SerializedDataParameter godRayExtinction;
    SerializedDataParameter godRayStepCount;
    SerializedDataParameter godRayLength;
    SerializedDataParameter godRayMieG;
    SerializedDataParameter Space_R;
    SerializedDataParameter Space_S;

    public override void OnEnable()
    {
        PropertyFetcher<VoxelVolumetricFog> o = new PropertyFetcher<VoxelVolumetricFog>(serializedObject);
    
        mode = Unpack(o.Find(x => x.mode));
        qualityLevel = Unpack(o.Find(x => x.qualityLevel));
        gridPixelSize = Unpack(o.Find(x => x.gridPixelSize));
        gridSizeZ = Unpack(o.Find(x => x.gridSizeZ));
        
        maxDistanceVolumetric = Unpack(o.Find(x => x.maxDistanceVolumetric));
        fogAttenuation = Unpack(o.Find(x => x.fogAttenuation));
        overrideDefaultMaxLightDistance = Unpack(o.Find(x => x.overrideDefaultMaxLightDistance));
        maxLightDistance = Unpack(o.Find(x => x.maxLightDistance));
        
        denoisingMode = Unpack(o.Find(x => x.denoisingMode));
        temporalAALighting = Unpack(o.Find(x => x.temporalAALighting));
        temporalAAMedia = Unpack(o.Find(x => x.temporalAAMedia));
        
        fogDensity = Unpack(o.Find(x => x.fogDensity));
        fogAlbedo = Unpack(o.Find(x => x.fogAlbedo));
        anisotropy = Unpack(o.Find(x => x.anisotropy));
        lightIntensity = Unpack(o.Find(x => x.lightIntensity));
        shadowIntensity = Unpack(o.Find(x => x.shadowIntensity));
        
        baseHeight = Unpack(o.Find(x => x.baseHeight));
        attenuationBoundarySize = Unpack(o.Find(x => x.attenuationBoundarySize));
        
        litColor = Unpack(o.Find(x => x.litColor));
        shadowedColor = Unpack(o.Find(x => x.shadowedColor));
        colorInfluence = Unpack(o.Find(x => x.colorInfluence));
        
        useNoise = Unpack(o.Find(x => x.useNoise));
        noise3DTexture = Unpack(o.Find(x => x.noiseTexture));
        noiseTiling = Unpack(o.Find(x => x.noiseTiling));
        noiseReMap = Unpack(o.Find(x => x.noiseReMap));
        noiseWindSpeed = Unpack(o.Find(x => x.noiseWindSpeed));
        
        godRayIntensity = Unpack(o.Find(x => x.godrayIntensity));
        godRaySkyBoxExtinc = Unpack(o.Find(x => x.skyBoxExtinction));
        godRayColor = Unpack(o.Find(x => x.godrayColor));
        godRayExtinction = Unpack(o.Find(x => x.extinctionCoef));
        godRayStepCount = Unpack(o.Find(x => x.maxStep));
        godRayLength = Unpack(o.Find(x => x.godrayLength));
        godRayMieG = Unpack(o.Find(x => x.godrayMieG));
        Space_R = Unpack(o.Find(x => x.Space_R));
        Space_S = Unpack(o.Find(x => x.Space_S));
    }


    public override void OnInspectorGUI()
    {
        PropertyField(mode,new GUIContent("开启体积雾"));
        if (mode.value.enumValueIndex == ((int) VolumetricFogMode.On))
        {
            EditorGUI.BeginChangeCheck();
            DrawQualityOption();
            //DrawQualitySettings();
            DrawCharacteristics();
            GodRayRendering();
            DrawVolumeNoiseRendering();
            DrawIntensityOverrides();

        }
        
        //质量选项
        void DrawQualityOption()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                new GUIContent("质量选项"),
                EditorStyles.boldLabel
            );
            EditorGUI.BeginChangeCheck();
            PropertyField(qualityLevel, new GUIContent("品质"));
            if (EditorGUI.EndChangeCheck())
            {
                gridPixelSize.overrideState.boolValue = true;
                gridSizeZ.overrideState.boolValue = true;
                SetQualitySettings(Quality.GetCurrentSetting(qualityLevel.value.intValue));
            }
        }
        
        //体素具体尺寸设置
        void DrawQualitySettings()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                new GUIContent("质量设置"),
                EditorStyles.boldLabel
            );
            PropertyField(
                gridPixelSize,
                new GUIContent("每格像素大小", "每个体素包含的像素数. 越低 -> 质量越高")
            );
            PropertyField(
                gridSizeZ,
                new GUIContent(
                    "体素深度切片数",
                    "由近到远平面之间的深度切片数. 越高 -> 质量越高."
                )
            );
            EditorGUI.indentLevel++;
            VoxelVolumetricFog obj = (VoxelVolumetricFog)serializedObject.targetObject;

            Vector3Int estimatedVolumeSize = obj.GetVolumeSize(VoxelVolumetricFog.ReferenceDimensions.ScreenSize);
            EditorGUILayout.LabelField(
                new GUIContent(
                    $" @ {VoxelVolumetricFog.ReferenceDimensions.ScreenSize.x}p下 体素3D贴图分辨率参考 : {estimatedVolumeSize.x}x{estimatedVolumeSize.y}x{estimatedVolumeSize.z}"
                ),
                EditorStyles.miniLabel
            );
            EditorGUI.indentLevel--;
        }
        
        //雾效设置
        void DrawCharacteristics()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("雾效设置", EditorStyles.boldLabel);

            PropertyField(fogDensity,new GUIContent("雾浓度"));
            PropertyField(fogAlbedo,new GUIContent("雾颜色"));
            PropertyField(
                fogAttenuation,
                new GUIContent("雾衰减距离")
            );
            PropertyField(
                anisotropy,
                new GUIContent("散射分布",
                    "对光的散射分布，1 为集中在光源方向，-1 为集中光源的反方向"
                )
            );
            PropertyField(
                maxDistanceVolumetric,
                new GUIContent("雾视野距离", "基于相机位置的最大距离内渲染雾")
            );
            PropertyField(
                baseHeight,
                new GUIContent("雾效基础高度", "世界坐标高度低于该值雾效浓度正常，高于高度则渐渐消失")
            );
            PropertyField(
                attenuationBoundarySize,
                new GUIContent("高度边界过渡")
            );
            PropertyField(overrideDefaultMaxLightDistance, new GUIContent("重载附加光显示距离"));
            using (new IndentLevelScope(15))
            {
                PropertyField(maxLightDistance,new GUIContent("附加光显示距离"));
            }
        }
        
        //光照设置
        void DrawIntensityOverrides()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(new GUIContent("光照/颜色设置"), EditorStyles.boldLabel);
            PropertyField(colorInfluence, new GUIContent("自定义颜色权重"));
            PropertyField(litColor,       new GUIContent("光照区域雾颜色"));
            PropertyField(shadowedColor,  new GUIContent("阴影区域雾颜色"));
            PropertyField(lightIntensity, new GUIContent("直线光照区域雾强度"));
            PropertyField(shadowIntensity,new GUIContent("直线光阴影区域雾强度"));
        }
        
        //GodRay设置
        void GodRayRendering()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(new GUIContent("GodRay增强设置"), EditorStyles.boldLabel);
            PropertyField(godRayColor, new GUIContent("体积光颜色"));
            PropertyField(godRayIntensity, new GUIContent("体积光强度"));
            PropertyField(godRayMieG, new GUIContent("体积光散射方向"));
            PropertyField(godRayExtinction, new GUIContent("消光值"));
            PropertyField(godRaySkyBoxExtinc, new GUIContent("天空盒消光值"));
            PropertyField(godRayStepCount, new GUIContent("体积光步进次数"));
            PropertyField(godRayLength, new GUIContent("体积光距离"));
    
            PropertyField(Space_S, new GUIContent("模糊程度"));
        }
        
        // 3D噪声扰动设置
        void DrawVolumeNoiseRendering()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(new GUIContent("3D噪声设置"), EditorStyles.boldLabel);
            PropertyField(useNoise, new GUIContent("开启噪声扰动"));
            PropertyField(noise3DTexture, new GUIContent("3D噪声图"));
            PropertyField(noiseTiling, new GUIContent("贴图尺寸"));
            PropertyField(noiseReMap, new GUIContent("强度范围"));
            PropertyField(noiseWindSpeed, new GUIContent("风效强度"));
        }

        //质量设置
        void SetQualitySettings(Quality.Settings settings)
        {
            if (settings == null)
                return;
            
            gridPixelSize.value.intValue = settings.gridPixelSize;
            gridSizeZ.value.intValue = settings.gridSizeZ;
        }
    }

    private static class Quality
    {
        public class Settings
        {
            public int gridPixelSize;
            public int gridSizeZ;

            public Settings( int gridPixelSize, int gridSizeZ)
            {
                this.gridSizeZ = gridSizeZ;
                this.gridPixelSize = gridPixelSize;
            }
        }
                    
        public static Settings Low = new Settings( 16, 128);
        public static Settings Medium = new Settings(8, 160);
        public static Settings High = new Settings(8, 256);

        public static Settings GetCurrentSetting(int id)
        {
            switch (id)
            {
                case 0:
                    return Low;
                case 1:
                    return Medium;
                case 2:
                    return High;
                default:
                    return Medium;
            }
        }

    }
    
    
}

