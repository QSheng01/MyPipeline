using CustomShaderEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomShaderEditor
{
    public class CustomShaderGUI_CharacterShow : ShaderGUI
    {
        public enum SurfaceType
        {
            Opaque,
            Transparent
        }

        public enum RenderFace
        {
            Front = 2,
            Back = 1,
            Both = 0
        }

        protected class PropertyInfo
        {
            public static readonly string Surface = "_Surface";
            // public static readonly string TransparentShadow = "_TransparentShadow";//半透投影
            public static readonly string Blend = "_Blend";
            public static readonly string Cull = "_Cull";
            public static readonly string AlphaTest = "_AlphaClip";
            public static readonly string Cutoff = "_Cutoff";
            public static readonly string ZTest = "_ZTest";
            public static readonly string ZWrite = "_ZWrite";
            public static readonly string SrcBlend = "_SrcBlend";
            public static readonly string DstBlend = "_DstBlend";
            public static readonly string QueueOffset = "_QueueOffset";
        }

        protected class MaterialKeyWord
        {
            public static readonly string ALPHATEST_ON = "ALPHATEST_ON";
        }

        protected class Styles
        {
            public static readonly GUIContent ResetOptionsLabel =
                new GUIContent("默认渲染设置", "还原为标准的不透明材质");

            public static readonly GUIContent ResetButtonLabel =
                new GUIContent("重设", "");

            public static readonly GUIContent SurfaceOptionsLabel =
                new GUIContent("渲染模式", "控制该材质是不透明还是半透明材质");

            public static readonly string[] SurfaceModeOptions = new string[] {"不透明", "半透明"};
            
            // public static GUIContent transparentShadowText =
            //     new GUIContent("打开半透投影", "是否开启半透材质的投影");

            public static GUIContent RenderFaceOptionsLabel =
                new GUIContent("渲染模式", "用于控制材质的渲染类型");

            // public static readonly string[] RenderModeOptions = new string[] {"正面剔除", "背面剔除", "双面显示"};
            public static readonly string[] RenderModeOptions = new string[] {"双面显示", "仅显示背面", "仅显示正面"};

            public static GUIContent alphaClipText =
                new GUIContent("透明裁剪", "是否开启透明裁剪");

            public static GUIContent alphaClipThresholdText = new GUIContent("裁剪阈值",
                "该值用于控制像素是否被裁剪，只有 alpha 大于该值的像素才不会被裁剪");

            public static GUIContent ZTestModeOptionsLabel = new GUIContent("深度检测",
                "该值用于控制材质的深度判断，非特殊材质不需要手动配置");

            public static readonly string[] ZTestModeOptions = new string[]
            {
                "关闭",
                "总不通过",
                "小于",
                "等于",
                "小于等于",
                "大于",
                "不等于",
                "大于等于",
                "总是通过"
            };

            public static readonly GUIContent ZWriteOptionsLabel = new GUIContent("深度写入",
                "该值用于配置是否进行深度写入，如非必要，不要修改");

            public static readonly string[] ZwriteOptions = new string[] {"关闭", "开启"};

            public static readonly GUIContent BlendOptionLabel = new GUIContent("混合模式",
                "像素的混合模式，如非必要，不要修改");

            public static readonly GUIContent SrcBlendOption = new GUIContent("SrcBlendOption");
            public static readonly GUIContent DstBlendOption = new GUIContent("DstBlendOption");

            public static readonly GUIContent queueSlider = new GUIContent("Queue Offset",
                "Determines the chronological rendering order for a Material. High values are rendered first.");
        }

        #region 渲染状态属性

        // Material Property
        private MaterialEditor materialEditor;
        protected MaterialProperty surfaceTypeProp;
        // protected MaterialProperty TransparentShadowProp;//半透投影
        protected MaterialProperty cullingProp;
        protected MaterialProperty alphaClipProp;
        protected MaterialProperty alphaCutoffProp;
        protected MaterialProperty zTestrop;
        protected MaterialProperty zWriteProp;
        protected MaterialProperty srcBlendProp;
        protected MaterialProperty dstBlendprop;
        protected MaterialProperty queueOffsetProp;

        #endregion

        private static int s_ControlHash = "EditorTextField".GetHashCode();

        private bool shadeSettingshow = false;
        private bool blendModeChanged { get; set; }
        private const int queueOffsetRange = 50;

        //自定义GI环境光面板
        private bool resetGI = false;
        private bool bakeGI = false;
        Color tipInfo = Color.red;
        


        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            if (materialEditor == null)
                throw new ArgumentNullException("materialEditorIn");
            // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
            FindProperties(props);
            this.materialEditor = materialEditor;
            var material = materialEditor.target as Material;
            ShaderPropertiesGUI(material, props);
        }

        public void FindProperties(MaterialProperty[] properties)
        {
            // Surface Option Props
            surfaceTypeProp = FindProp(PropertyInfo.Surface, properties);
            // TransparentShadowProp = FindProp(PropertyInfo.TransparentShadow, properties);//半透物投影开关
            //blendModeProp = FindProp(PropertyInfo.Blend, properties);
            cullingProp = FindProp(PropertyInfo.Cull, properties);
            alphaClipProp = FindProp(PropertyInfo.AlphaTest, properties);
            alphaCutoffProp = FindProp(PropertyInfo.Cutoff, properties);

            zTestrop = FindProp(PropertyInfo.ZTest, properties);
            zWriteProp = FindProp(PropertyInfo.ZWrite, properties);
            srcBlendProp = FindProp(PropertyInfo.SrcBlend, properties);
            dstBlendprop = FindProp(PropertyInfo.DstBlend, properties);
            queueOffsetProp = FindProperty(PropertyInfo.QueueOffset, properties, false);
            // _CullMode = FindProp(mCullModeName, properties);
            // _Cutoff = FindProp(mCullOff, properties);
        }


        public static MaterialProperty FindProp(string propertyName, MaterialProperty[] properties,
            bool propertyIsMandatory = false)
        {
            return FindProperty(propertyName, properties, propertyIsMandatory);
        }

        public void ShaderPropertiesGUI(Material material, MaterialProperty[] properties)
        {
            // EditorGUI.BeginChangeCheck();
            CustomPropertiesGUI(properties);
            // if (EditorGUI.EndChangeCheck())
            // {
            //     foreach (var obj in materialEditor.targets)
            //         MaterialChanged((Material) obj);
            // }
            
            
            // ShadeSettingGUI(material);
            shadeSettingshow = CustomShaderGUIUtils.Foldout(shadeSettingshow, "渲染设置");
            if (shadeSettingshow)
            {
                ResetGUI(material, materialEditor);
                
                EditorGUI.BeginChangeCheck();
                SurfaceGUI(material, materialEditor);
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var obj in materialEditor.targets)
                        MaterialChanged((Material) obj);
                }
                
                AlphaClipGUI(material, materialEditor);
                RenderFaceGUI(material, materialEditor);
                ZTestGUI(material, materialEditor);
                ZWriteGUI(material, materialEditor);
                this.materialEditor.EnableInstancingField();
                //this.materialEditor.RenderQueueField();
                DrawAdvancedOptions(material, materialEditor);
                materialEditor.RenderQueueField();
                BlendGUI(material, materialEditor);
            }

            EditorGUILayout.Space();
        }

        public void CustomPropertiesGUI(MaterialProperty[] props)
        {
            materialEditor.SetDefaultGUIWidths();
            GUIUtility.GetControlID(s_ControlHash, FocusType.Passive, new Rect(0, 0, 0, 0));
            
            
            for (var i = 0; i < props.Length; i++)
            {
                if ((props[i].flags & (MaterialProperty.PropFlags.HideInInspector |
                                       MaterialProperty.PropFlags.PerRendererData)) != 0)
                {
                    if (props[i].name.StartsWith("_CustomGIMap", StringComparison.Ordinal))
                    {//自定义球谐环境光贴图面板GUI
                        
                        //================基础贴图面板GUI==================
                        if (props[i + 1].floatValue > 0)
                        {
                            tipInfo = Color.green;
                        }

                        // GUI.contentColor = tipInfo;
                        GUI.backgroundColor = tipInfo;
                        
                        float h0 = materialEditor.GetPropertyHeight(props[i], props[i].displayName);
                        Rect r0 = EditorGUILayout.GetControlRect(true, h0, EditorStyles.layerMaskField);
                        materialEditor.ShaderProperty(r0, props[i], props[i].displayName);
                        
                        // GUI.contentColor = Color.white;
                        
                        
                        
                        //================功能开关==================
                        Material material = materialEditor.target as Material;
                        if (props[i-1].floatValue > 0)
                        {//利用Shader结构来做简易的折叠栏开关判断
                            //================重置自定义环境光颜色==================
                            Rect TogglegRect1 = new Rect(r0)
                            {
                                x = r0.xMin,
                                //y = (height > 16) ? position.y + 16 : position.y,
                                y = r0.y + 75,
                                width = 140,
                                height = 22
                            };
                            
                            resetGI = GUI.Toggle(TogglegRect1, resetGI, "重置自定义环境光颜色", "Button");
                            if (resetGI)
                            {
                                // material.SetVector("custom_SHAr", new Vector4(0, 0, 0, 0.2140411f));
                                // material.SetVector("custom_SHAg", new Vector4(0, 0, 0, 0.2140411f));
                                // material.SetVector("custom_SHAb", new Vector4(0, 0, 0, 0.2140411f));
                                // material.SetVector("custom_SHBr", new Vector4(0, 0, 0, 0));
                                // material.SetVector("custom_SHBg", new Vector4(0, 0, 0, 0));
                                // material.SetVector("custom_SHBb", new Vector4(0, 0, 0, 0));
                                // material.SetVector("custom_SHC", new Vector4(0, 0, 0, 0));

                                props[i + 2].vectorValue = new Vector4(0, 0, 0, 0.2140411f);
                                props[i + 3].vectorValue = new Vector4(0, 0, 0, 0.2140411f);
                                props[i + 4].vectorValue = new Vector4(0, 0, 0, 0.2140411f);
                                props[i + 5].vectorValue = new Vector4(0, 0, 0, 0);
                                props[i + 6].vectorValue = new Vector4(0, 0, 0, 0);
                                props[i + 7].vectorValue = new Vector4(0, 0, 0, 0);
                                props[i + 8].vectorValue = new Vector4(0, 0, 0, 0);
                                
                                tipInfo = Color.red;
                                resetGI = false;
                                props[i + 1].floatValue = 0;
                                
                                EditorApplication.QueuePlayerLoopUpdate();
                            }
                            
                            //================烘焙自定义环境光颜色==================
                            Rect TogglegRect2 = new Rect(r0)
                            {
                                x = r0.xMin + 160,
                                //y = (height > 16) ? position.y + 16 : position.y,
                                y = r0.y + 75,
                                width = 140,
                                height = 22
                            };
                            
                            bakeGI = GUI.Toggle(TogglegRect2, bakeGI, "烘焙自定义环境光颜色", "Button");
                            if (bakeGI)
                            {
                                Material skyboxMaterialCache = RenderSettings.skybox;
                                
                                RenderSettings.skybox = new Material(Shader.Find("Skybox/Cubemap"));
                                RenderSettings.skybox.SetTexture("_Tex", props[i].textureValue);
                                RenderSettings.skybox.SetColor("_Tint", new Color(0.7830188f, 0.7830188f, 0.7830188f));
                                RenderSettings.skybox.SetFloat("_Exposure", 0.6f);
                                RenderSettings.skybox.SetFloat("_Rotation", 161);

                                Lightmapping.Bake();
                                DynamicGI.UpdateEnvironment();
            
                                SphericalHarmonicsL2 shUnitySource = RenderSettings.ambientProbe;
            
                                // material.SetVector("custom_SHAr", new Vector4(shUnitySource[0, 3], shUnitySource[0, 1], shUnitySource[0, 2], shUnitySource[0, 0]) * 1.04995754f);
                                // material.SetVector("custom_SHAg", new Vector4(shUnitySource[1, 3], shUnitySource[1, 1], shUnitySource[1, 2], shUnitySource[1, 0] * 1.05453939f));
                                // material.SetVector("custom_SHAb", new Vector4(shUnitySource[2, 3], shUnitySource[2, 1], shUnitySource[2, 2], shUnitySource[2, 0] * 1.0573462f));
                                // material.SetVector("custom_SHBr", new Vector4(shUnitySource[0, 4], shUnitySource[0, 5], shUnitySource[0, 6] * 3.0f, shUnitySource[0, 7]));
                                // material.SetVector("custom_SHBg", new Vector4(shUnitySource[1, 4], shUnitySource[1, 5], shUnitySource[1, 6] * 3.0f, shUnitySource[1, 7]));
                                // material.SetVector("custom_SHBb", new Vector4(shUnitySource[2, 4], shUnitySource[2, 5], shUnitySource[2, 6] * 3.0f, shUnitySource[2, 7]));
                                // material.SetVector("custom_SHC", new Vector4(shUnitySource[0, 8], shUnitySource[1, 8], shUnitySource[2, 8], 1));
                                

                                props[i + 2].vectorValue = new Vector4(shUnitySource[0, 3], shUnitySource[0, 1], shUnitySource[0, 2], shUnitySource[0, 0] * 1.04995754f);
                                props[i + 3].vectorValue = new Vector4(shUnitySource[1, 3], shUnitySource[1, 1], shUnitySource[1, 2], shUnitySource[1, 0] * 1.05453939f);
                                props[i + 4].vectorValue = new Vector4(shUnitySource[2, 3], shUnitySource[2, 1], shUnitySource[2, 2], shUnitySource[2, 0] * 1.0573462f);
                                props[i + 5].vectorValue = new Vector4(shUnitySource[0, 4], shUnitySource[0, 5], shUnitySource[0, 6] * 3.0f, shUnitySource[0, 7]);
                                props[i + 6].vectorValue = new Vector4(shUnitySource[1, 4], shUnitySource[1, 5], shUnitySource[1, 6] * 3.0f, shUnitySource[1, 7]);
                                props[i + 7].vectorValue = new Vector4(shUnitySource[2, 4], shUnitySource[2, 5], shUnitySource[2, 6] * 3.0f, shUnitySource[2, 7]);
                                props[i + 8].vectorValue = new Vector4(shUnitySource[0, 8], shUnitySource[1, 8], shUnitySource[2, 8], 1);
                                
                                tipInfo = Color.green;
                                bakeGI = false;
                                props[i + 1].floatValue = 1;

                                RenderSettings.skybox = skyboxMaterialCache;
                                Lightmapping.Bake();
                                DynamicGI.UpdateEnvironment();
                                
                                EditorApplication.QueuePlayerLoopUpdate();
                            }
                            
                            
                            
                            
                            GUILayout.Space( 45);
                        }
                        GUI.backgroundColor = Color.white;
                    }

                    continue;
                }

                
                float h = materialEditor.GetPropertyHeight(props[i], props[i].displayName);
                Rect r = EditorGUILayout.GetControlRect(true, h, EditorStyles.layerMaskField);

                materialEditor.ShaderProperty(r, props[i], props[i].displayName);
            }

            GUILayout.Space(8);
        }

        // private void ShadeSettingGUI(Material material)
        // {
        //     shadeSettingshow = CustomShaderGUIUtils.Foldout(shadeSettingshow, "渲染设置");
        //     if (shadeSettingshow)
        //     {
        //         ResetGUI(material, materialEditor);
        //         SurfaceGUI(material, materialEditor);
        //         AlphaClipGUI(material, materialEditor);
        //         RenderFaceGUI(material, materialEditor);
        //         ZTestGUI(material, materialEditor);
        //         ZWriteGUI(material, materialEditor);
        //         this.materialEditor.EnableInstancingField();
        //         //this.materialEditor.RenderQueueField();
        //         DrawAdvancedOptions(material, materialEditor);
        //         BlendGUI(material, materialEditor);
        //     }
        // }

        void SurfaceGUI(Material mat, MaterialEditor editor)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.showMixedValue = surfaceTypeProp.hasMixedValue;
            var mode = surfaceTypeProp.floatValue;
            EditorGUI.BeginChangeCheck();
            mode = EditorGUILayout.Popup(Styles.SurfaceOptionsLabel, (int) mode, Styles.SurfaceModeOptions);
            blendModeChanged = false;
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo(Styles.SurfaceOptionsLabel.text);
                surfaceTypeProp.floatValue = mode;
                blendModeChanged = true;
            }

            EditorGUI.showMixedValue = false;
            EditorGUILayout.EndHorizontal();
            
            // if (mode > 0)
            // {//半透材质投影
            //     EditorGUILayout.BeginHorizontal();
            //     EditorGUI.BeginChangeCheck();
            //     EditorGUI.showMixedValue = TransparentShadowProp.hasMixedValue;
            //     var TransparentShadow = EditorGUILayout.Toggle(Styles.transparentShadowText, TransparentShadowProp.floatValue == 1);
            //     if (EditorGUI.EndChangeCheck())
            //     {
            //         TransparentShadowProp.floatValue = TransparentShadow ? 1 : 0;
            //     }
            //     EditorGUI.showMixedValue = false;
            //     EditorGUILayout.EndHorizontal();
            //
            //     if (Mathf.Approximately(TransparentShadowProp.floatValue, 1))
            //     {
            //         mat.SetShaderPassEnabled("ShadowCaster", true);
            //     }
            //     else
            //     {
            //         mat.SetShaderPassEnabled("ShadowCaster", false);
            //     }
            // }
        }

        void AlphaClipGUI(Material mat, MaterialEditor editor)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = alphaClipProp.hasMixedValue;
            var alphaClipEnabled = EditorGUILayout.Toggle(Styles.alphaClipText, alphaClipProp.floatValue == 1);
            if (EditorGUI.EndChangeCheck())
            {
                alphaClipProp.floatValue = alphaClipEnabled ? 1 : 0;
            }

            EditorGUI.showMixedValue = false;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (Mathf.Approximately(alphaClipProp.floatValue, 1))
                materialEditor.ShaderProperty(alphaCutoffProp, Styles.alphaClipThresholdText, 0);
            EditorGUILayout.EndHorizontal();
        }

        void RenderFaceGUI(Material material, MaterialEditor editor)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.showMixedValue = cullingProp.hasMixedValue;
            var culling = (RenderFace) cullingProp.floatValue;
            EditorGUI.BeginChangeCheck();
            
            // culling = (RenderFace) EditorGUILayout.EnumPopup(Styles.RenderFaceOptionsLabel, culling);
            culling = (RenderFace)EditorGUILayout.Popup(Styles.SurfaceOptionsLabel, (int)culling, Styles.RenderModeOptions);
            
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo(Styles.RenderFaceOptionsLabel.text);
                cullingProp.floatValue = (float) culling;
                material.doubleSidedGI = (RenderFace) cullingProp.floatValue != RenderFace.Front;
            }

            EditorGUI.showMixedValue = false;
            EditorGUILayout.EndHorizontal();
        }

        void ZTestGUI(Material mat, MaterialEditor editor)
        {
            EditorGUILayout.BeginHorizontal();
            var ztest = zTestrop.floatValue;
            EditorGUI.BeginChangeCheck();
            ztest = EditorGUILayout.Popup(Styles.ZTestModeOptionsLabel, (int) ztest, Styles.ZTestModeOptions);
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo(Styles.ZTestModeOptionsLabel.text);
                zTestrop.floatValue = ztest;
            }

            EditorGUILayout.EndHorizontal();
        }

        void ZWriteGUI(Material mat, MaterialEditor editor)
        {
            EditorGUILayout.BeginHorizontal();
            var zwrite = zWriteProp.floatValue;
            EditorGUI.BeginChangeCheck();
            zwrite = EditorGUILayout.Popup(Styles.ZWriteOptionsLabel, (int) zwrite, Styles.ZwriteOptions);
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo(Styles.ZWriteOptionsLabel.text);
                zWriteProp.floatValue = zwrite;
            }

            EditorGUILayout.EndHorizontal();
        }

        public virtual void DrawAdvancedOptions(Material material, MaterialEditor editor)
        {
            if (queueOffsetProp != null)
            {
                //EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = queueOffsetProp.hasMixedValue;
                //queueOffsetProp.floatValue = EditorGUILayout.Slider(Styles.queueSlider,queueOffsetProp.floatValue, -10, 10);
                GUILayout.Label(Styles.queueSlider);
                GUILayout.FlexibleSpace();
                var queue = EditorGUILayout.IntSlider( (int) queueOffsetProp.floatValue, -queueOffsetRange,
                    queueOffsetRange);
                if (EditorGUI.EndChangeCheck())
                    queueOffsetProp.floatValue = queue;
                EditorGUI.showMixedValue = false;
                //EditorGUILayout.EndHorizontal();
            }
        }

        void BlendGUI(Material mat, MaterialEditor editor)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            // var style = new GUIStyle("ShurikenLabel");
            // style.alignment = TextAnchor.MiddleLeft;
            // style.fontSize = 11; // 默认 9
            // style.fontStyle = FontStyle.Bold;
            // style.normal.textColor = Color.white;
            //GUILayout.Label(Styles.BlendOptionLabel, "BoldLabel");
            GUILayout.Label(Styles.BlendOptionLabel);
            var scr = (BlendMode) srcBlendProp.floatValue;
            EditorGUI.BeginChangeCheck();
            scr = (BlendMode) EditorGUILayout.EnumPopup(scr);
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo(Styles.SrcBlendOption.text);
                srcBlendProp.floatValue = (float) scr;
            }

            var dst = (BlendMode) dstBlendprop.floatValue;
            EditorGUI.BeginChangeCheck();
            dst = (BlendMode) EditorGUILayout.EnumPopup(dst);
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo(Styles.DstBlendOption.text);
                dstBlendprop.floatValue = (float) dst;
            }

            EditorGUILayout.EndHorizontal();
        }

        protected void ResetGUI(Material mat, MaterialEditor editor)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            GUILayout.Label(Styles.ResetOptionsLabel);
            GUILayout.FlexibleSpace();
            var reset = GUILayout.Button(Styles.ResetButtonLabel, GUILayout.MinWidth(65), GUILayout.Height(15),
                GUILayout.ExpandWidth(true));
            if (reset)
            {
                Reset(mat);
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Reset Shading Setting  as "Opaque"
        /// </summary>
        /// <param name="mat"></param>
        protected void Reset(Material mat)
        {
            surfaceTypeProp.floatValue = (float) SurfaceType.Opaque;
            cullingProp.floatValue = (float) RenderFace.Front; //渲染模式，渲染正面
            alphaClipProp.floatValue = 0;
            alphaCutoffProp.floatValue = 0.5f;

            zTestrop.floatValue = (float) CompareFunction.LessEqual; //深度测试， 小于等于
            zWriteProp.floatValue = 1; //深度写入
            var one = (int) UnityEngine.Rendering.BlendMode.One;
            var zero = (int) UnityEngine.Rendering.BlendMode.Zero;
            srcBlendProp.floatValue = one;
            dstBlendprop.floatValue = zero;

            mat.doubleSidedGI = false; //烘焙属性修改
        }

        //
        //
        // private float EditorGUILayoutFloatSlider(string desc, float val, float min, float max)
        // {
        //     EditorGUILayout.BeginHorizontal();
        //     GUILayout.Label(desc, GUILayout.ExpandWidth(false));
        //     val = GUILayout.HorizontalSlider(val, min, max, GUILayout.ExpandWidth(true));
        //     val = EditorGUILayout.FloatField(val, GUILayout.Width(64));
        //     EditorGUILayout.EndHorizontal();
        //     return val;
        // }
        //
        // material changed check
        public void MaterialChanged(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            SetMaterialBlendMode(material);
        }

        private void SetMaterialBlendMode(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            bool alphaClip = false;
            if (material.HasProperty(PropertyInfo.AlphaTest))
                alphaClip = material.GetFloat(PropertyInfo.AlphaTest) >= 0.5;

            if (alphaClip)
            {
                material.EnableKeyword("_ALPHATEST_ON");
            }
            else
            {
                material.DisableKeyword("_ALPHATEST_ON");
            }

            if (material.HasProperty(PropertyInfo.Surface))
            {
                SurfaceType surfaceType = (SurfaceType) material.GetFloat(PropertyInfo.Surface);

                material.renderQueue = 3001;
                material.renderQueue += material.HasProperty(PropertyInfo.QueueOffset)
                ? (int) material.GetFloat("_QueueOffset")
                : 0;
                    
                if (surfaceType == SurfaceType.Opaque)
                {
                    if (alphaClip)
                    {
                        // material.renderQueue = (int) RenderQueue.AlphaTest;
                        material.SetOverrideTag("RenderType", "TransparentCutout");
                    }
                    else
                    {
                        // material.renderQueue = (int) RenderQueue.Geometry;
                        material.SetOverrideTag("RenderType", "Opaque");
                    }

                    // material.renderQueue += material.HasProperty(PropertyInfo.QueueOffset)
                    //     ? (int) material.GetFloat("_QueueOffset")
                    //     : 0;
                    if (blendModeChanged)
                    {
                        material.SetInt(PropertyInfo.SrcBlend, (int) UnityEngine.Rendering.BlendMode.One);
                        material.SetInt(PropertyInfo.DstBlend, (int) UnityEngine.Rendering.BlendMode.Zero);
                        material.SetInt(PropertyInfo.ZWrite, 1);
                    }

                    material.SetShaderPassEnabled("ShadowCaster", true);
                }
                else
                {
                    // General Transparent Material Settings
                    material.SetOverrideTag("RenderType", "Transparent");
                    // BlendMode blendMode = (BlendMode) material.GetFloat("_Blend");
                    if (blendModeChanged)
                    {
                        material.SetInt(PropertyInfo.SrcBlend, (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt(PropertyInfo.DstBlend, (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.SetInt(PropertyInfo.ZWrite, 1);
                    }

                    // material.renderQueue = (int) RenderQueue.Transparent;
                    // material.renderQueue += material.HasProperty(PropertyInfo.QueueOffset)
                    //     ? (int) material.GetFloat("_QueueOffset")
                    //     : 0;
                    material.SetShaderPassEnabled("ShadowCaster", false);
                }
            }

            blendModeChanged = false;
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            base.AssignNewShaderToMaterial(material, oldShader, newShader);
            // if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
            // {
            //     SetMaterialBlendMode(material);
            //     return;
            // }
            MaterialChanged(material);
        }
    }
}