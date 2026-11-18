using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class URPWaterEditor2 : ShaderGUI
{

    MaterialEditor _Editor;
    MaterialProperty[] _Properties;
    Material _Target;
    bool _RefractionPanelVisible = true;
    bool _SpecularPanelVisible = true;
    bool _NormalsPanelVisible = true;
    bool _EdgeFadePanelVisible = true;
    bool _FoamPanelVisible = true;
    bool _CausticPanelVisible = true;
    bool _ScatteringPanelVisible = true;
    bool _ReflectionPanelVisible = true;
    bool _DisplacementPanelVisible = true;
    bool _OptionsPanelVisible = true;
    bool _TessellationPanelVisible = true;

    Gradient _RefractionGradient = new Gradient();
    List<GradientColorKey> _GradientColorKeys = new List<GradientColorKey>();

    GUIStyle _FoldoutStyle;
    GUIStyle _LabelStyle;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {

        this._Editor = materialEditor;
        this._Properties = properties;
        this._Target = _Editor.target as Material;

        _FoldoutStyle = new GUIStyle(EditorStyles.foldout);
        _FoldoutStyle.fontStyle = FontStyle.Bold;

        _LabelStyle = new GUIStyle(EditorStyles.label);
        _LabelStyle.fontStyle = FontStyle.Bold;

        DrawHeader();
        DrawRefraction();
        DrawReflection();
        DrawSpecular();
        DrawNormals();
        DrawEdgeFade();
        //DrawFoam();
        DrawCaustic();
        DrawScattering();
        DrawOptions();
    }

    void DrawHeader()
    {
        EditorGUIUtility.labelWidth = 0f;
        EditorGUIUtility.fieldWidth = 64f;
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    void DrawRefraction()
    {
        GUILayout.BeginVertical("GroupBox");
        EditorGUI.indentLevel += 1;
        _RefractionPanelVisible = EditorGUILayout.Foldout(_RefractionPanelVisible, "颜色深度以及折射", _FoldoutStyle);
        EditorGUI.indentLevel += 1;

        if (!_RefractionPanelVisible)
        {
            EditorGUI.indentLevel -= 2;
            GUILayout.EndVertical();
            return;
        }

        var ColorMode = FindProperty("_ColorMode");
        _Editor.ShaderProperty(ColorMode, "颜色模式");

        if (ColorMode.floatValue == 0)
        {
            var colorA = FindProperty("_Color");
            var colorB = FindProperty("_DepthColor");

            _Editor.ShaderProperty(colorA, "浅水颜色");
            _Editor.ShaderProperty(colorB, "深水颜色");
        }


        if (ColorMode.floatValue == 1)
        {
            var rColorProperty = FindProperty("_RefractionColor");
            EditorGUI.BeginChangeCheck();
            _RefractionGradient = EditorGUILayout.GradientField("渐变色", _RefractionGradient);

            if (EditorGUI.EndChangeCheck())
            {

                var width = 128;
                var height = 4;
                var isNewFile = false;
                
                if (rColorProperty.textureValue != null)
                {
                    var textureName = rColorProperty.textureValue.name;

                }
                EncodeGradientToMaterial(_RefractionGradient);
            }
        }
        var depthStart = FindProperty("_DepthStart");
        var depthEnd = FindProperty("_DepthEnd");
        var distortion = FindProperty("_Distortion");
        
        _Editor.ShaderProperty(depthStart, "深度起始");
        _Editor.ShaderProperty(depthEnd, "深度终止");
        _Editor.ShaderProperty(distortion, "折射强度");
        
        EditorGUI.indentLevel -= 2;
        GUILayout.EndVertical();
    }

    void DrawSpecular()
    {
        GUILayout.BeginVertical("GroupBox");
        EditorGUI.indentLevel += 1;
        _SpecularPanelVisible = EditorGUILayout.Foldout(_SpecularPanelVisible, "高光设置", _FoldoutStyle);
        EditorGUI.indentLevel += 1;

        if (!_SpecularPanelVisible)
        {
            EditorGUI.indentLevel -= 2;
            GUILayout.EndVertical();
            return;
        }

        var SpecColor = FindProperty("_SpecColor");
        var Smoothness = FindProperty("_Smoothness");

        _Editor.ShaderProperty(SpecColor, "高光颜色");
        _Editor.ShaderProperty(Smoothness, "光滑度");
        
        EditorGUI.indentLevel -= 2;
        GUILayout.EndVertical();
    }

    void DrawNormals()
    {
        GUILayout.BeginVertical("GroupBox");
        EditorGUI.indentLevel += 1;
        _NormalsPanelVisible = EditorGUILayout.Foldout(_NormalsPanelVisible, "法线设置", _FoldoutStyle);
        EditorGUI.indentLevel += 1;

        if (!_NormalsPanelVisible)
        {
            EditorGUI.indentLevel -= 2;
            GUILayout.EndVertical();
            return;
        }

        var normalsMode = FindProperty("_NormalsMode");
        _Editor.ShaderProperty(normalsMode, "模式");
        
        // Normal A
        var normalA = FindProperty("_NormalMapA");
        var normalMapATilings = FindProperty("_NormalMapATilings");
        var normalMapAIntensity = FindProperty("_NormalMapAIntensity");
        _Editor.TexturePropertySingleLine(MakeLabel("法线"), normalA);

        // Normal A Params
        if (normalsMode.floatValue == 0 || normalsMode.floatValue == 1)
        {
            var normalMapASpeeds = FindProperty("_NormalMapASpeeds");
            EditorGUI.indentLevel += 2;
            DrawDoubleVector2FromVector4(normalMapATilings, "第一层延展", "第二层延展");
            DrawDoubleVector2FromVector4(normalMapASpeeds, "第一层流速", "第二层流速");
            // _Editor.ShaderProperty(normalMapAIntensity, "法线强度");
            EditorGUI.indentLevel -= 2;
        }

        // Normal B
        if (normalsMode.floatValue == 1)
        {
            var normalB = FindProperty("_BlendMap");
            var normalBTilings = FindProperty("_BlendMapTilings");
            var normalBSpeeds = FindProperty("_BlendMapSpeeds");

            _Editor.TexturePropertySingleLine(MakeLabel("附加法线"), normalB);
            EditorGUI.indentLevel += 2;
            DrawVector2FromVector4(normalBTilings, "延展");
            DrawVector2FromVector4(normalBSpeeds, "流速");
            EditorGUI.indentLevel -= 2;
        }

        // Flow Map
        if (normalsMode.floatValue == 2)
        {
            var flowMap = FindProperty("_FlowMap");
            var flowSpeed = FindProperty("_FlowSpeed");
            var flowIntensity = FindProperty("_FlowIntensity");
            var flowTiling = FindProperty("_FlowTiling");

            EditorGUI.indentLevel += 2;
            DrawVector2FromVector4(normalMapATilings, "延展");
            _Editor.ShaderProperty(normalMapAIntensity, "强度");
            EditorGUI.indentLevel -= 2;

            _Editor.TexturePropertySingleLine(MakeLabel("流向图"), flowMap);
            EditorGUI.indentLevel += 2;
            DrawDoubleVector2FromVector4(flowTiling, "延展", "偏移");
            _Editor.ShaderProperty(flowSpeed, "流速");
            _Editor.ShaderProperty(flowIntensity, "强度");
            EditorGUI.indentLevel -= 2;
        }

        EditorGUI.indentLevel -= 2;
        GUILayout.EndVertical();
    }

    void DrawEdgeFade()
    {
        GUILayout.BeginVertical("GroupBox");
        EditorGUI.indentLevel += 1;
        _EdgeFadePanelVisible = EditorGUILayout.Foldout(_EdgeFadePanelVisible, "边缘渐变设置", _FoldoutStyle);
        EditorGUI.indentLevel += 1;

        if (!_EdgeFadePanelVisible)
        {
            EditorGUI.indentLevel -= 2;
            GUILayout.EndVertical();
            return;
        }

        var enableEdggeFade = FindProperty("_EdgeFade");
        var edgeSize = FindProperty("_EdgeSize");

        _Editor.ShaderProperty(enableEdggeFade, "是否开启");

        if (enableEdggeFade.floatValue == 1)
        {
            _Editor.ShaderProperty(edgeSize, "淡出程度");
        }

        EditorGUI.indentLevel -= 2;
        GUILayout.EndVertical();
    }

    void DrawFoam()
    {
        GUILayout.BeginVertical("GroupBox");
        EditorGUI.indentLevel += 1;
        _FoamPanelVisible = EditorGUILayout.Foldout(_FoamPanelVisible, "Foam", _FoldoutStyle);
        EditorGUI.indentLevel += 1;

        if (!_FoamPanelVisible)
        {
            EditorGUI.indentLevel -= 2;
            GUILayout.EndVertical();
            return;
        }

        var enableFoam = FindProperty("_Foam");
        var foamColor = FindProperty("_FoamColor");
        var foamTex = FindProperty("_FoamTex");
        var foamTiling = FindProperty("_FoamTiling");
        var foamSize = FindProperty("_FoamSize");
        var foamDistortion = FindProperty("_FoamDistortion");

        _Editor.ShaderProperty(enableFoam, "Enable");
        
        if (enableFoam.floatValue == 1)
        {
            _Editor.ShaderProperty(foamColor, "Color");
            _Editor.TexturePropertySingleLine(MakeLabel("Texture"), foamTex);
            EditorGUI.indentLevel += 2;
            DrawVector2FromVector4(foamTiling, "Tiling");
            //DrawVector2FromVector4(normalBSpeeds, "Speed");
            //_Editor.ShaderProperty(normalBIntensity, "Intensity");
            EditorGUI.indentLevel -= 2;

            _Editor.ShaderProperty(foamSize, "Size");
            _Editor.ShaderProperty(foamDistortion, "Distortion");

            var foamRipples = FindProperty("_FoamRipples");
            _Editor.ShaderProperty(foamRipples, "Ripples");

            if (foamRipples.floatValue == 1)
            {
                var rippleDistance = FindProperty("_FoamRippleDistance");
                var rippleSize = FindProperty("_FoamRippleSize");
                var rippleSpeed = FindProperty("_FoamRippleSpeed");
                EditorGUI.indentLevel += 2;
                _Editor.ShaderProperty(rippleDistance, "Ripple Distance");
                _Editor.ShaderProperty(rippleSize, "Ripple Size");
                _Editor.ShaderProperty(rippleSpeed, "Ripple Speed");
                EditorGUI.indentLevel -= 2;

            }
        }

        var whiteCaps = FindProperty("_FoamWhiteCaps");
        var displacementEnabled = _Target.IsKeywordEnabled("_DISPLACEMENTMODE_GERSTNER");
        if (displacementEnabled)
        {

            _Editor.ShaderProperty(whiteCaps, "White Caps");
        }

        if (whiteCaps.floatValue == 1 && displacementEnabled)
        {

            var capsIntensity = FindProperty("_FoamCapsIntensity");
            var min = FindProperty("_FoamCapsRangeMin");
            var max = FindProperty("_FoamCapsRangeMax");
            var minValue = min.floatValue;
            var maxValue = max.floatValue;

            EditorGUI.indentLevel += 2;

            EditorGUILayout.MinMaxSlider("Range", ref minValue, ref maxValue, 0f, 2f);

            min.floatValue = minValue;
            max.floatValue = maxValue;

            _Editor.ShaderProperty(capsIntensity, "Caps Intensity");
            EditorGUI.indentLevel -= 2;
        }


        EditorGUI.indentLevel -= 2;
        GUILayout.EndVertical();
    }

    void DrawScattering()
    {
        GUILayout.BeginVertical("GroupBox");
        EditorGUI.indentLevel += 1;
        _ScatteringPanelVisible = EditorGUILayout.Foldout(_ScatteringPanelVisible, "散射设置", _FoldoutStyle);
        EditorGUI.indentLevel += 1;

        if (!_ScatteringPanelVisible)
        {
            EditorGUI.indentLevel -= 2;
            GUILayout.EndVertical();
            return;
        }
        
        var enableScattering = FindProperty("_Scattering");

        _Editor.ShaderProperty(enableScattering, "是否开启");

        if (enableScattering.floatValue == 1f)
        {
            var color = FindProperty("_ScatteringColor");
            var intensity = FindProperty("_ScatteringIntensity");
            var min = FindProperty("_ScatteringRangeMin");
            var max = FindProperty("_ScatteringRangeMax");


            _Editor.ShaderProperty(color, "散射颜色");
            _Editor.ShaderProperty(intensity, "强度");

            var minValue = min.floatValue;
            var maxValue = max.floatValue;

            EditorGUILayout.MinMaxSlider("散射范围", ref minValue, ref maxValue, 0f, 2f);

            min.floatValue = minValue;
            max.floatValue = maxValue;
        }

        EditorGUI.indentLevel -= 2;
        GUILayout.EndVertical();
    }

    void DrawCaustic()
    {

        GUILayout.BeginVertical("GroupBox");
        EditorGUI.indentLevel += 1;
        _CausticPanelVisible = EditorGUILayout.Foldout(_CausticPanelVisible, "焦散设置", _FoldoutStyle);
        EditorGUI.indentLevel += 1;

        if (!_CausticPanelVisible)
        {
            EditorGUI.indentLevel -= 2;
            GUILayout.EndVertical();
            return;
        }

        var enableCaustic = FindProperty("_Caustics");
        var causticMode = FindProperty("_CausticsMode");

        var causticTex = FindProperty("_CausticsTex");
        var causticTiling = FindProperty("_CausticsTiling");
        var causticSpeed = FindProperty("_CausticsSpeed");
        var causticIntensity = FindProperty("_CausticsIntensity");
        var causticStart = FindProperty("_CausticsStart");
        var causticEnd = FindProperty("_CausticsEnd");
        var causticAngleMask = FindProperty("_CausticsAngleMask");
        var causticDirectionMask = FindProperty("_CausticsDirectionMask");
        //var causticDistortion = FindProperty("_CausticsDistortion");

        _Editor.ShaderProperty(enableCaustic, "是否开启");

        if (enableCaustic.floatValue == 1)
        {
            _Editor.TexturePropertySingleLine(MakeLabel("焦散遮罩（白色信息）"), causticTex);
            EditorGUI.indentLevel = 4;
            DrawDoubleVector2FromVector4(causticTiling, "第一层延展", "第二层延展");
            DrawDoubleVector2FromVector4(causticSpeed, "第一层流速", "第二层流速");
            EditorGUI.indentLevel = 2;
            _Editor.ShaderProperty(causticIntensity, "强度");
            _Editor.ShaderProperty(causticStart, "起始距离");
            _Editor.ShaderProperty(causticEnd, "终止距离");
        }
        EditorGUI.indentLevel -= 2;
        GUILayout.EndVertical();
    }

    void DrawReflection()
    {
        GUILayout.BeginVertical("GroupBox");
        EditorGUI.indentLevel += 1;
        _ReflectionPanelVisible = EditorGUILayout.Foldout(_ReflectionPanelVisible, "反射设置", _FoldoutStyle);
        EditorGUI.indentLevel += 1;

        if (!_ReflectionPanelVisible)
        {
            EditorGUI.indentLevel -= 2;
            GUILayout.EndVertical();
            return;
        }
        var reflectionMode = FindProperty("_ReflectionMode");
        var rFresnel = FindProperty("_ReflectionFresnel");
        var rFresnelNormal = FindProperty("_ReflectionFresnelNormal");
        var rIntensity = FindProperty("_ReflectionIntensity");
        var rDistortion = FindProperty("_ReflectionDistortion");
        var planarRefDistortion = FindProperty("_PlanarReflectionDistortion");
        var ssrpLerpScale = FindProperty("_SSPRLerpScale");
        var reflectionColor = FindProperty("_ReflectionColor");
        
        
        _Editor.ShaderProperty(reflectionMode, "反射模式");

        // if (reflectionMode.floatValue == 1)
        // {
            // 一级反射有关
            // var rCubeMap = FindProperty("_CubemapTexture");
            // //_Editor.ShaderProperty(rCubeMap, "CubeMap");
            // _Editor.TexturePropertySingleLine(MakeLabel("一级反射-球面反射贴图"), rCubeMap);
            //
            // var rFirstReflectionColor = FindProperty("_FirstReflectionColor");
            // _Editor.ShaderProperty(rFirstReflectionColor, "一级反射-颜色叠加");
            //
            // var rFirstReflectionTurnDark = FindProperty("_FirstReflectionTurnDark");
            // _Editor.ShaderProperty(rFirstReflectionTurnDark, "一级反射-整体变暗");
            //
            // var rFirstReflectionLightRange = FindProperty("_FirstReflectionLightRange");
            // _Editor.ShaderProperty(rFirstReflectionLightRange, "一级反射-亮度范围");
            //
            // var rFirstReflectionMostLight = FindProperty("_FirstReflectionMostLight");
            // _Editor.ShaderProperty(rFirstReflectionMostLight, "一级反射-最高亮度阈值");
            
        // }
        
        if (reflectionMode.floatValue != 0 && reflectionMode.floatValue != 4)
        {
            // var roughness = FindProperty("_ReflectionRoughness");

            _Editor.ShaderProperty(rIntensity, "反射-强度");
            _Editor.ShaderProperty(rFresnel, "反射-菲涅尔程度");
            _Editor.ShaderProperty(rFresnelNormal, "反射-菲尼尔法线影响程度");
            _Editor.ShaderProperty(rDistortion, "反射-扭曲程度");
            // _Editor.ShaderProperty(roughness, "反射-粗糙程度");
            _Editor.ShaderProperty(ssrpLerpScale, "反射-SSPR边缘天空盒与深度色插值");
            _Editor.ShaderProperty(reflectionColor, "反射-SSPR反射强度");
        }

        if (reflectionMode.floatValue == 4)
        {
            _Editor.ShaderProperty(rIntensity, "强度");
            _Editor.ShaderProperty(rFresnel, "菲涅尔程度");
            _Editor.ShaderProperty(rFresnelNormal, "菲尼尔法线影响程度");
            _Editor.ShaderProperty(planarRefDistortion, "平面反射扭曲程度");
        }

        EditorGUI.indentLevel -= 2;
        GUILayout.EndVertical();
    }

    void DrawOptions()
    {
        GUILayout.BeginVertical("GroupBox");
        EditorGUI.indentLevel = 1;
        _OptionsPanelVisible = EditorGUILayout.Foldout(_OptionsPanelVisible, "渲染设置", _FoldoutStyle);
        EditorGUI.indentLevel = 2;

        if (!_OptionsPanelVisible)
        {
            EditorGUI.indentLevel = 0;
            GUILayout.EndVertical();
            return;
        }

        EditorGUI.indentLevel = 3;
        // Render Queue
        EditorGUI.indentLevel = 2;
        _Target.renderQueue = EditorGUILayout.IntField("渲染队列", _Target.renderQueue);

        EditorGUI.indentLevel = 0;
        GUILayout.EndVertical();
    }

    void DrawVector2FromVector4(MaterialProperty prop, string newName, bool ZW = false)
    {
        var vectorValues = prop.vectorValue;

        var vectorA = ZW ? new Vector2(vectorValues.z, vectorValues.w) : new Vector2(vectorValues.x, vectorValues.y);

        var a = EditorGUILayout.Vector2Field(newName, vectorA);

        prop.vectorValue = ZW ? new Vector4(vectorValues.x, vectorValues.y, a.x, a.y) : new Vector4(a.x, a.y, vectorValues.z, vectorValues.w);
    }

    void DrawDoubleVector2FromVector4(MaterialProperty prop, string newNameA, string newNameB)
    {
        var vectorValues = prop.vectorValue;
        var vectorA = new Vector2(vectorValues.x, vectorValues.y);
        var vectorB = new Vector2(vectorValues.z, vectorValues.w);

        var a = EditorGUILayout.Vector2Field(newNameA, vectorA);
        var b = EditorGUILayout.Vector2Field(newNameB, vectorB);

        prop.vectorValue = new Vector4(a.x, a.y, b.x, b.y);
    }
    void EncodeGradientToMaterial(Gradient gradient)
    {
        for (var i = 0; i < 8; i++)
        {
            var gradientProp = FindProperty("_RefractionKey" + (i + 1));

            if (i >= gradient.colorKeys.Length)
            {
                gradientProp.vectorValue = new Vector4(-1, -1, -1, -1);
                continue;
            }
            var color = gradient.colorKeys[i].color;
            var time = gradient.colorKeys[i].time;
            var newData = new Vector4(color.r, color.g, color.b, time);
            gradientProp.vectorValue = newData;
        }
    }

    static GUIContent staticLabel = new GUIContent();

    static GUIContent MakeLabel(string text, string tooltip = null)
    {
        staticLabel.text = text;
        staticLabel.tooltip = tooltip;
        return staticLabel;
    }

    MaterialProperty FindProperty(string name)
    {
        return FindProperty(name, _Properties);
    }
}
