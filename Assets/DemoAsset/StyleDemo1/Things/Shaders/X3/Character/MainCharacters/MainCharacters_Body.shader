Shader "X3/Character/MainCharacters_Body"
{
    Properties
    {
        [CustomGroup] _BaseSetting("基础设置", Float) = 1.0
    	[CustomHeader(_BaseSetting)] _BaseMapTip("RGB:颜色  A:Alpha", Float) = 1
        [CustomTexture(_BaseSetting)][NoScaleOffset] _BaseMap("固有色贴图", 2D) = "white" {}
    	[CustomObject(_BaseSetting)] _BaseColor(" ", Color) = (1, 1, 1, 1)
        [CustomHeader(_BaseSetting)] _BumpMapTip("RG:法线  B:高光强度遮罩  A:空", Float) = 1
        [CustomTexture(_BaseSetting)][NoScaleOffset] _BumpMaskMap("法线遮罩图", 2D) = "white" {}
        [CustomObject(_BaseSetting)] _BumpStrength("法线强度", Range(0, 3)) = 1
        
        
        [CustomGroup] _MapSetting("遮罩图设置", Float) = 1.0 
        [CustomHeader(_MapSetting)] _LightMapTip("R:金属度  G:自发光  B:Ramp ID  A:光滑度", Float) = 1
        [CustomTexture(_MapSetting)][NoScaleOffset] _LightMap ("光照贴图", 2D) = "white" {}
    	[CustomObject(_MapSetting)] _Metallic("金属度缩放", Range(0, 1)) = 1
    	[CustomObject(_MapSetting)] _Smoothness("光滑度缩放", Range(0, 1)) = 1
//        [CustomHeader(_MapSetting)] _MaskTip("R:各向异性高光遮罩  G:丝袜遮罩  BA:空", Float) = 1
//        [CustomTexture(_MapSetting)][NoScaleOffset] _MaskMap ("特殊效果遮罩图", 2D) = "black" {}
//	    [CustomToggle(_MapSetting, _SILKSTOCKING_ON)] _UseSilkStocking("使用丝袜材质", Float) = 0.0
//        [CustomHeader(_MapSetting._SILKSTOCKING_ON)] _SilkStockingSkinTip("透肉设置", Float) = 1
//        [CustomObject(_MapSetting._SILKSTOCKING_ON)] _SilkStockingSkinColor("透肉颜色", Color) = (1, 1, 1, 1)
//        [CustomObject(_MapSetting._SILKSTOCKING_ON)] _SilkStockingSkinColorBrightness("透肉颜色亮度", Range(1, 3)) = 1
//        
//        [CustomHeader(_MapSetting._SILKSTOCKING_ON)] _SilkStockingDarkFresnelTip("暗色菲涅尔", Float) = 1
//        [CustomObject(_MapSetting._SILKSTOCKING_ON)] _SilkStockingDarkFresnelColor("暗色菲涅尔颜色", Color) = (0.5, 0.5, 0.5, 1)
//        [CustomObject(_MapSetting._SILKSTOCKING_ON)] _SilkStockingDarkFresnelContrast("暗边对比度", Range(0, 1)) = 1
//        [CustomObject(_MapSetting._SILKSTOCKING_ON)] _SilkStockingDarkFresnelIntensity("暗边强度", Range(0, 5)) = 1
//        [CustomObject(_MapSetting._SILKSTOCKING_ON)] _SilkStockingDarkFresnelRange("暗边范围", Range(-1, 1)) = 0
//        [CustomObject(_MapSetting._SILKSTOCKING_ON)] _SilkStockingDarkFresnelSoftness("暗边过渡柔软度", Range(0, 2)) = 0.5
//        
//        [CustomHeader(_MapSetting._SILKSTOCKING_ON)] _SilkStockingDarkSideTip("暗边", Float) = 1
//        [CustomObject(_MapSetting._SILKSTOCKING_ON)] _SilkStockingDarkSideColor("暗边颜色", Color) = (0.69034, 0.06412, 0, 1)
//        [CustomObject(_MapSetting._SILKSTOCKING_ON)] _SilkStockingDarkSideBrightness("暗边亮度", Range(0, 1)) = 1
//        [CustomObject(_MapSetting._SILKSTOCKING_ON)] _SilkStockingDarkSideRange("暗边范围", Range(0, 1)) = 0
//        [CustomObject(_MapSetting._SILKSTOCKING_ON)] _SilkStockingDarkSideSoftness("暗边过渡柔软度", Range(0, 1)) = 0.5
//        
//        [CustomHeader(_MapSetting._SILKSTOCKING_ON)] _SilkStockingRimTip("边缘光", Float) = 1
//        [CustomObject(_MapSetting._SILKSTOCKING_ON)] _SilkStockingRimColor("边缘光颜色", Color) = (1, 1, 1, 1)
//        [CustomObject(_MapSetting._SILKSTOCKING_ON)] _SilkStockingRimBrightness("边缘光亮度", Range(0, 10)) = 1
//        [CustomObject(_MapSetting._SILKSTOCKING_ON)] _SilkStockingRimWidth("边缘光宽度", Range(0, 1)) = 0.5
        
        
        [CustomGroup] _DiffuseSetting("漫反射阴影设置", Float) = 1.0
    	[CustomHeader(_DiffuseSetting)] _RampMapTip("ID通道：LightMap_B", Float) = 1 
        [CustomRampTexture(_DiffuseSetting)][NoScaleOffset] _RampMap("Ramp图", 2D) = "white" {} 
//        [CustomTexture(_DiffuseSetting)][NoScaleOffset] _RampMap("Ramp图", 2D) = "white" {} 
//        [CustomObject(_DiffuseSetting)] _RampYOffset("ID 偏移", Range(0, 1)) = 0 
        
        
//        [CustomGroup] _SpecularSetting("双层高光设置",Float) = 1.0 
        [CustomGroup] _SpecularSetting("高光设置",Float) = 1.0 
//		[CustomHeader(_SpecularSetting)] _SpecularTip("亮层高光", Float) = 0
		[CustomObject(_SpecularSetting)] _SpecularColor("高光颜色", Color) = (1, 1, 1, 1)
    	[CustomObject(_SpecularSetting)] _SpecularBlendAlbedoIntensity("高光颜色混合固有色强度", Range(0, 1)) = 0.6
		[CustomObject(_SpecularSetting)] _SpecularInstensity("高光强度", Range(0, 10)) = 1
    	[CustomObject(_SpecularSetting)] _MetaIntensity ("金属高光强度叠加", Range(0,8)) = 0//		[CustomHeader(_SpecularSetting)] _NdVSpecularTip("暗层高光", Float) = 0
    	[CustomObject(_SpecularSetting)] _SpecularYPosOffset("高光纵轴偏移", Float) = 0
//		[CustomObject(_SpecularSetting)] _NdVSpecularWeight("暗层高光混合强度", Range(0.02, 1)) = 0.2
//		[CustomObject(_SpecularSetting)] _NdVSpecularInstensity("暗层高光强度", Range(0, 1)) = 0.38
        
    	
    	[CustomGroup] _EnvShadingSetting("环境光设置", Float) = 1.0    
        [CustomObject(_EnvShadingSetting)] _SHBrightness("环境光亮度", Range(0, 3)) = 1
        [CustomHeader(_EnvShadingSetting)] _EnvMaskTip("Cubemap强度遮罩通道：LightMap_R", Float) = 1  
//    	[CustomTexture(_EnvShadingSetting)][NoScaleOffset] _MatCapMap("环境高光MatCap", 2D) = "black" {}
        [CustomTexture(_EnvShadingSetting)][NoScaleOffset] _EvnCubemap("环境球", CUBE) = "_skybox" {} 
    	[CustomObject(_EnvShadingSetting)] _EnvColor("环境高光颜色", Color) = (1, 1, 1, 1)
        [CustomObject(_EnvShadingSetting)] _EnvRotate ("环境球旋转", Range(0, 360)) = 0
    	[CustomObject(_EnvShadingSetting)] _EnvBlendAlbedoIntensity("Cubemap混合固有色强度", Range(0, 1))= 0
        [CustomObject(_EnvShadingSetting)] _EnvBrightness("环境高光亮度", Range(0, 10)) = 0
    	
    	
	    [CustomGroup] _EmissionSetting("自发光设置", Float) = 1.0 
        [CustomObject(_EmissionSetting)] [HDR]_EmissionColor("自发光颜色", Color) = (1, 1, 1, 1) 
        [CustomObject(_EmissionSetting)] _EmissionInstensity("自发光强度", Range(0, 10)) = 0
    	//        [CustomObject(_EmissionSetting)] _EmissionBlendAlbedoIntensity("自发光颜色OR混合固有色", Range(0, 1)) = 1 
    	[CustomHeader(_EmissionSetting)] _WholeEmissionColorTip("特效专用自发光（不使用遮罩）", Float) = 1  
    	[CustomObject(_EmissionSetting)] [HDR]_WholeEmissionColor("全身自发光颜色", Color) = (0, 0, 0, 1)
    	
    	
	    [CustomGroup] _RimSetting("背光边缘光设置", float) = 1.0 
        [CustomToggle(_RimSetting, _USERIM)] _useRim("使用背光边缘光", Float) = 0
        [CustomObject(_RimSetting._USERIM)][HDR] _RimColor("边缘光颜色", Color) = (1, 1, 0.8245283, 1)
    	[CustomObject(_RimSetting._USERIM)] _RimColorBlendAlbedo("颜色混合固有色强度", Range(0, 1)) = 0.5
//    	[CustomObject(_RimSetting._USERIM)] _RimLambertMaskThreshold("边缘光受光照影响程度", Range(0, 1)) = 0.5
        [CustomObject(_RimSetting._USERIM)] _RimThreshold("边缘光范围", Range(0, 1)) = 0.65
//        [CustomObject(_RimSetting._USERIM)] _RimSoftness("边缘光过渡柔软度", Range(0.001, 1)) = 0.5
        [CustomObject(_RimSetting._USERIM)] _RimDepthWidth("深度遮挡距离", Range(0, 1)) = 0.01
//        [CustomObject(_RimSetting._USERIM)] _Test1("范围测试", Range(0, 1)) = 0.5
//        [CustomObject(_RimSetting._USERIM)] _Test2("柔软度测试", Range(0, 20)) = 0.5
        
        
//        [CustomGroup] _SilkStockingSetting("丝袜设置", Float) = 1.0
//        [CustomToggle(_SilkStockingSetting, _SILKSTOCKING_ON)] _UseSilkStocking("使用丝袜材质", Float) = 0.0
//        [CustomHeader(_SilkStockingSetting._SILKSTOCKING_ON)] _SilkStockingSkinTip("透肉设置", Float) = 1
//        [CustomObject(_SilkStockingSetting._SILKSTOCKING_ON)] _SilkStockingSkinColor("透肉颜色", Color) = (1, 1, 1, 1)
//        [CustomObject(_SilkStockingSetting._SILKSTOCKING_ON)] _SilkStockingSkinColorBrightness("透肉颜色亮度", Range(1, 3)) = 1
//        
//        [CustomHeader(_SilkStockingSetting._SILKSTOCKING_ON)] _SilkStockingDarkFresnelTip("暗色菲涅尔", Float) = 1
//        [CustomObject(_SilkStockingSetting._SILKSTOCKING_ON)] _SilkStockingDarkFresnelColor("暗色菲涅尔颜色", Color) = (0.5, 0.5, 0.5, 1)
//        [CustomObject(_SilkStockingSetting._SILKSTOCKING_ON)] _SilkStockingDarkFresnelContrast("暗边对比度", Range(0, 1)) = 1
//        [CustomObject(_SilkStockingSetting._SILKSTOCKING_ON)] _SilkStockingDarkFresnelIntensity("暗边强度", Range(0, 5)) = 1
//        [CustomObject(_SilkStockingSetting._SILKSTOCKING_ON)] _SilkStockingDarkFresnelRange("暗边范围", Range(-1, 1)) = 0
//        [CustomObject(_SilkStockingSetting._SILKSTOCKING_ON)] _SilkStockingDarkFresnelSoftness("暗边过渡柔软度", Range(0, 2)) = 0.5
//        
//        [CustomHeader(_SilkStockingSetting._SILKSTOCKING_ON)] _SilkStockingDarkSideTip("暗边", Float) = 1
//        [CustomObject(_SilkStockingSetting._SILKSTOCKING_ON)] _SilkStockingDarkSideColor("暗边颜色", Color) = (0.69034, 0.06412, 0, 1)
//        [CustomObject(_SilkStockingSetting._SILKSTOCKING_ON)] _SilkStockingDarkSideBrightness("暗边亮度", Range(0, 1)) = 1
//        [CustomObject(_SilkStockingSetting._SILKSTOCKING_ON)] _SilkStockingDarkSideRange("暗边范围", Range(0, 1)) = 0
//        [CustomObject(_SilkStockingSetting._SILKSTOCKING_ON)] _SilkStockingDarkSideSoftness("暗边过渡柔软度", Range(0, 1)) = 0.5
//        
//        [CustomHeader(_SilkStockingSetting._SILKSTOCKING_ON)] _SilkStockingRimTip("边缘光", Float) = 1
//        [CustomObject(_SilkStockingSetting._SILKSTOCKING_ON)] _SilkStockingRimColor("边缘光颜色", Color) = (1, 1, 1, 1)
//        [CustomObject(_SilkStockingSetting._SILKSTOCKING_ON)] _SilkStockingRimBrightness("边缘光亮度", Range(0, 10)) = 1
//        [CustomObject(_SilkStockingSetting._SILKSTOCKING_ON)] _SilkStockingRimWidth("边缘光宽度", Range(0, 1)) = 0.5
    	
    	
//    	[CustomGroup] _AnisotropySetting("各向异性高光设置", Float) = 1.0
//    	[CustomToggle(_AnisotropySetting, _ANISOTROPY_ON)] _UseAnisotropySpecular("使用各向异性高光", Float) = 0.0
//    	[CustomObject(_AnisotropySetting._ANISOTROPY_ON)] _AnisotropySpecularColor("各向异性高光颜色", Color) = (1, 1, 1, 1)
//        [CustomObject(_AnisotropySetting._ANISOTROPY_ON)] _AnisotropyIntensity ("各向异性强度", Range(-1, 1)) = 0.0
//        [CustomObject(_AnisotropySetting._ANISOTROPY_ON)] _AnisotropyBrightness ("颜色亮度", Range(0, 2)) = 1
//    	[CustomObject(_AnisotropySetting._ANISOTROPY_ON)] _AnisotropySpecularBlendAlbedoIntensity("颜色混合固有色强度", Range(0, 1)) = 0.5
    	
    	
        [CustomGroup] _MultipleLightSetting("多光源设置",Float) = 1.0 
        [CustomObject(_MultipleLightSetting)] _MultipleLightIntensity("多光源亮度", Range(0, 10)) = 2.5
        [CustomObject(_MultipleLightSetting)] _MultipleLightBlend("多光源颜色混合强度", Range(0, 1)) = 0.5
    	
    	
    	[CustomGroup] _ShadowSetting("投影设置", Float) = 1.0 
        [CustomHeader(_ShadowSetting)] _ShadowColorTip("投影颜色", Float) = 1 
//        [CustomObject(_ShadowSetting)] _CharacterShadowColor ("投影颜色", Color) = (0.53, 0.53, 0.53, 1)
//        [CustomObject(_ShadowSetting)] _ShadowColorOffset("投影颜色与Ramp图颜色混合强度", Range(0, 1)) = 0.2
        [CustomObject(_ShadowSetting)] _ShadowIntensity("投影强度", Range(0, 1)) = 1
        [CustomHeader(_ShadowSetting)] _ShadowOffsetTip("投影位置偏移", Float) = 1 
        [CustomObject(_ShadowSetting)] _CustomShadowDepthBias("投影按光照方向偏移", Range(0.01, 20)) = 1
        [CustomObject(_ShadowSetting)] _CustomShadowNormalBias("投影按法线方向偏移", Range(0.01, 20)) = 0.01
    	
    	
    	[CustomGroup] _HeavyShadowSetting("重阴影设置",float) = 1.0
//        [CustomToggle(_HeavyShadowSetting, _HEAVYSHADOW)] _UseHeavyShadow("使用重阴影", Float) = 0
    	[CustomToggle(_HeavyShadowSetting)] _UseShadowMask("仅在投影范围生效", Float) = 1
        [CustomObject(_HeavyShadowSetting)] _HeavyShadowColor("重阴影颜色", Color) = (0.9056604, 0.6365254, 0.6743726, 1)
        [CustomObject(_HeavyShadowSetting)] _HeavyShadowRange("重阴影范围", Range(0, 1)) = 0.21
        [CustomObject(_HeavyShadowSetting)] _HeavyShadowSoftness("重阴影柔软度", Range(0, 1)) = 0.45
        [CustomObject(_HeavyShadowSetting)] _HeavyShadowIntensity("重阴影强度", Range(0, 1)) = 0.638
        
    	
	    [CustomGroup] _EffectFresnelSetting("Fresnel边缘光设置(特效专用)",float) = 1.0 
        [CustomToggle(_EffectFresnelSetting, _EFFECTFRESNEL_ON)] _UseEffectFresnel("使用Fresnel边缘光", Float) = 0.0
    	[CustomHeader(_EffectFresnelSetting._EFFECTFRESNEL_ON)] _EffectMaskTip("使用R通道作为Fresnel边缘光遮罩", Float) = 1
    	[CustomTexture(_EffectFresnelSetting._EFFECTFRESNEL_ON)][NoScaleOffset] _EffectMaskMap("特效遮罩图", 2D) = "white" {}
        [CustomObject(_EffectFresnelSetting._EFFECTFRESNEL_ON)][HDR] _EffectFresnelColor("边缘光颜色", Color) = (1, 1, 1, 1)
        [CustomObject(_EffectFresnelSetting._EFFECTFRESNEL_ON)] _EffectFresnelFactor("边缘光系数", Range(0, 1)) = 1
        [CustomObject(_EffectFresnelSetting._EFFECTFRESNEL_ON)] _EffectFresnelPower("边缘光亮度对比度", Range(1, 8)) = 2
        [CustomObject(_EffectFresnelSetting._EFFECTFRESNEL_ON)] _EffectFresnelThreshold("边缘光范围", Range(0, 1)) = 0
        [CustomObject(_EffectFresnelSetting._EFFECTFRESNEL_ON)] _EffectFresnelSmooth("边缘光柔软度", Range(0.001, 1)) = 1
    	
    	
        [CustomGroup] _HitSetting("受击边缘光设置", float) = 1
        [CustomObject(_HitSetting)][HDR]_HitRimColor("受击边缘光颜色", Color)=(1, 1, 1, 1)
        [CustomObject(_HitSetting)]_HitRimFactor("效果触发百分比", Range(0, 1)) = 0
        [CustomObject(_HitSetting)]_HitRimPower("菲涅尔强度", Range(1, 8))=1
        [CustomObject(_HitSetting)]_HitRimThreshold("边缘光范围", Range(0, 1))=0
        [CustomObject(_HitSetting)]_HitRimSmooth("边缘光光滑度", Range(0.001, 1))=1
    	
    	
	    [CustomGroup] _OutLineSetting("描边设置", Float) = 1.0
        [CustomToggle(_OutLineSetting, _USEOUTLINE)] _USEOUTLINE("开启描边", Float) = 0 //TODO: 应该用_USEOUTLINE // PASS _USEOUTLINE
        [CustomKeywordEnum(_OutLineSetting, _Normal, COLORS_AS_NORMALS, UV2_AS_NORMALS)] _OutlineType ("法线来源 ", Float) = 0
        [CustomObject(_OutLineSetting)] _OutlineColor("描边颜色", Color) = (0, 0, 0, 1)
        [CustomObject(_OutLineSetting)] _Outline_Width("描边宽度", Range(0, 10)) = 1
        [CustomObject(_OutLineSetting)] _Offset_Z("描边偏移", Range(-10, 10)) = 0
    	
    	
        //渲染设置
		[HideInInspector] _Cutoff("透明裁剪", Range(0.0, 1.0)) = 0.5
		[HideInInspector] _Surface("_surface", Float) = 0.0
		[HideInInspector] _TransparentShadow("__TransparentShadow", Float) = 0.0//在CustomShaderGUINew 238行附近
		[HideInInspector] _AlphaClip("_alphaclip", Float) = 0.0
        [HideInInspector] _Mode ("__mode", Float) = 0.0
		[HideInInspector] _Cull ("__cull", Float) = 2   //[Enum(Cull Off,0, Cull Front,1, Cull Back,2)]
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0
		[HideInInspector] _ZTest ("__zt", Float) = 4.0
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "CharacterBody"
            Tags
            {
            	"RenderType" = "Opaque"
            	"Queue" = "Geometry"
//                "LightMode" = "SRPDefaultUnlit"
                "LightMode" = "UniversalForward"
            }
        	
        	Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }
            
            Cull [_Cull]
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Blend [_SrcBlend] [_DstBlend]
            
            HLSLPROGRAM

            #pragma target 3.0
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            // #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            #pragma multi_compile_fog
            //#pragma skip_variants FOG_EXP FOG_EXP2
            
            #pragma multi_compile _ _WORLDFOG
            #pragma multi_compile_local_fragment _ _USERIM
            #pragma multi_compile_local_fragment _ _SILKSTOCKING_ON
            #pragma multi_compile_local_fragment _ _EFFECTFRESNEL_ON
            // #pragma multi_compile_local_fragment _ _ANISOTROPY_ON

            // #define _SCREEN_SPACE_OCCLUSION 1
            
            
            #pragma vertex Vertex_Scene
            #pragma fragment Fragment_Scene
            
            #include "../ShaderLibrary/MainCharacters_BodyInput.hlsl"
            #include "../ShaderLibrary/MainCharacters_BodyLibrary.hlsl" 

            
            ENDHLSL
        }
    	
    	Pass
        {
            Name "DepthOnly"
            Tags
            {
            	"RenderType" = "Opaque"
				"Queue" = "Geometry"
                "LightMode" = "DepthOnly"
            }

            ZWrite On
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM

            #pragma target 3.0

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            
            #pragma multi_compile_instancing

            #include "../ShaderLibrary/MainCharacters_BodyInput.hlsl"
            #include "../ShaderLibrary/CharacterDepthOnlyPass.hlsl"
            
            ENDHLSL
        }
    	Pass
        {
            Name "ShadowCaster"
            Tags
            {
            	"RenderType" = "Opaque"
				"Queue" = "Geometry"
                "LightMode" = "ShadowCaster"
            }

            ZWrite On
            ZTest LEqual
            Cull [_Cull]

            HLSLPROGRAM

            #pragma target 3.0

            #pragma vertex ShadowPassVertex 
            #pragma fragment ShadowPassFragmentBlend
            
            #pragma multi_compile_instancing
            

            #include "../ShaderLibrary/MainCharacters_BodyInput.hlsl"
            #include "../ShaderLibrary/CharacterShadowCasterPass.hlsl"
            
            ENDHLSL
        }
    	
    	Pass
        {
            Name "Outline"
            Tags
            {
                "LightMode" = "Outline"
            }
            Cull Front
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Blend [_SrcBlend] [_DstBlend]

            HLSLPROGRAM
            #pragma multi_compile _ _USEOUTLINE
            #pragma multi_compile _Normal COLORS_AS_NORMALS  UV2_AS_NORMALS
            
            
            #pragma vertex OutLineOctahedronPassVertex_Scene
            #pragma fragment OutlinePassFragmentNew

            #include "..\ShaderLibrary\MainCharacters_BodyInput.hlsl"
            #include "../ShaderLibrary/MainCharacters_BodyLibrary.hlsl" 
            
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals" 
            Tags
            {
                "LightMode" = "DepthNormals" 
            }
            
            // -------------------------------------
            // Render State Commands
            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            // #pragma target 2.0

             
            // -------------------------------------
            // Shader Stages
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Material Keywords
            // #pragma shader_feature_local _NORMALMAP
            // #pragma shader_feature_local _PARALLAXMAP
            // #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            // #pragma shader_feature_local_fragment _ALPHATEST_ON
            // #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            // -------------------------------------
            // Unity defined keywords
            // #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // -------------------------------------
            // Universal Pipeline keywords
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            //#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Includes
            #include "..\ShaderLibrary\MainCharacters_BodyInput.hlsl"
            #include "..\ShaderLibrary\DepthNormalsPass.hlsl"
            ENDHLSL
        }

    }
//    FallBack "Hidden/Universal Render Pipeline/FallbackError" 
    CustomEditor "CustomShaderEditor.CustomShaderGUI_Character" 
}
