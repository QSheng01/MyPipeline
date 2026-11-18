Shader "X3/Character/MainCharacters_Hair"
{
    Properties
    {
    	[HideInInspector]_CharacterEffectGrade("角色效果等级", Int) = 0
    	
	    [CustomGroup] _BaseSetting("基础设置", Float) = 1.0
    	[CustomHeader(_BaseSetting)] _BaseMapTip("RGB:颜色  A:Alpha", Float) = 1
        [CustomTexture(_BaseSetting)][NoScaleOffset] _BaseMap("固有色贴图", 2D) = "white" {}
    	[CustomObject(_BaseSetting)] _BaseColor(" ", Color) = (1, 1, 1, 1)
    	[CustomHeader(_BaseSetting)] _BumpMapTip("RG:法线  BA:空", Float) = 1
		[CustomTexture(_BaseSetting)][NoScaleOffset] _BumpMaskMap("法线遮罩图", 2D) = "white" {}
    	[CustomHeader(_BaseSetting)] _LightMapTip("R:高光强度遮罩  G:AO  B:高光描线遮罩  A:空", Float) = 1
    	[CustomTexture(_BaseSetting)][NoScaleOffset] _LightMap ("光照贴图", 2D) = "white" {}
        [CustomObject(_BaseSetting)] _BumpStrength("法线强度", Range(0, 3)) = 1
    	[CustomObject(_BaseSetting)] _AoStrength("AO强度", Range(0, 1)) = 0
        [CustomObject(_BaseSetting)] _AoColor("AO颜色", Color) = (0, 0, 0, 1)
        [CustomObject(_BaseSetting)] _AoColorBlendAlbedo("AO颜色混合固有色强度", Range(0, 1)) = 0
        
        
        [CustomGroup] _DiffuseSetting("阴影设置",float) = 1.0
//        [CustomTexture(_DiffuseSetting)] _ShadowMap ("阴影Ramp图", 2D) = "white" {}
        [CustomRampTexture(_DiffuseSetting)] _ShadowMap ("阴影Ramp图", 2D) = "white" {}
        [CustomObject(_DiffuseSetting)] _RampYOffset("Ramp贴图ID偏移", Range(0, 1)) = 0
        [CustomObject(_DiffuseSetting)] _RampXOffset("Ramp贴图X轴偏移", Range(-0.5, 0.5)) = 0
		[CustomToggle(_DiffuseSetting)] _DiffuseAddShadow("阴影叠加自投影", Float) = 1.0
        [CustomToggle(_DiffuseSetting)] _UseOtherLight("叠加对称阴影", Float) = 0.0
    	[CustomToggle(_DiffuseSetting)] _UseDiffuseBlendAO ("开启“阴影叠加AO”", Float) = 0
    	
    	
//        [CustomGroup] _PbrSpecularSetting("PBR高光设置", Float) = 1.0 
//    	[CustomObject(_PbrSpecularSetting)] _Roughness("头发粗糙度", Range(0.0078125, 1)) = 0.5
//    	[CustomObject(_PbrSpecularSetting)] _PbrSpecularColor("PBR高光颜色", Color) = (1, 1, 1, 1)
//    	[CustomObject(_PbrSpecularSetting)] _PbrSpecularBrightness("PBR高光亮度", Range(0, 1)) = 0.5
        
        
        [CustomGroup] _SpecSetting("头发高光设置",float) = 1.0
    	[CustomHeader(_SpecSetting)] _HairSpecularTip("整体高光强度遮罩使用LightMap.R通道", Float) = 1
		[CustomTexture(_SpecSetting)] _AnisotropyShapeMap("高光形状贴图", 2D) = "white" {}
        [CustomHeader(_SpecSetting)] _AnisotropyTip1("各向异性高光1", Float) = 1.0
		[CustomObject(_SpecSetting)] _HairSpecularCol("高光1颜色", Color) = (0,0,0,1)
        [CustomObject(_SpecSetting)] _HairSpread1("高光1扩散", Range(-1, 1)) = 0.0
        [CustomObject(_SpecSetting)] _HairSpecularShift("高光1位置", Range(-3,3)) = 1.0
        [CustomObject(_SpecSetting)] _HairSpecularStrength("高光1强度", Range(0, 10)) = 1.0
        [CustomObject(_SpecSetting)] _HairSpecularExponent("高光1面积", Range(1,1024)) = 1.0
        [CustomHeader(_SpecSetting)] _AnisotropyTip2("各向异性高光2", Float) = 1.0
		[CustomObject(_SpecSetting)] _HairSecondarySpecularCol("高光2颜色", Color) = (0, 0, 0, 1)
        [CustomObject(_SpecSetting)] _HairSpread2("高光2扩散", Range(-1, 1)) = 0.0
        [CustomObject(_SpecSetting)] _HairSecondarySpecularShift_Black("高光2位置", Range(-3, 3)) = 1.0
//        [CustomObject(_SpecSetting)] _HairSecondarySpecularShift_White("高光2白色遮罩位置", Range(-3, 3)) = 1.0
        [CustomObject(_SpecSetting)] _HairSecondarySpecularStrength("高光2强度", Range(0, 10)) = 1.0
        [CustomObject(_SpecSetting)] _HairSecondarySpecularExponent("高光2面积",Range(1, 1024)) = 1.0
        [CustomHeader(_SpecSetting)] _AnisotropyTip3("高光描线", Float) = 1.0
    	[CustomHeader(_SpecSetting)] _AnisotropyLineTip("高光描线遮罩使用LightMap.B通道", Float) = 1
//        [CustomToggle(_SpecSetting, _ANISOTROPYLINEMAP)] _UseAnisotropyLineMap ("使用高光描线", Float) = 0
		[CustomTexture(_SpecSetting)] _AnisotropyLineMap("高光描线贴图", 2D) = "white" {}
        [CustomObject(_SpecSetting)] _AnisotropyLineIntensity("高光描线强度", Range(0, 1)) = 1.0
        [CustomObject(_SpecSetting)] _AnisotropyLinePos("高光描线位置", Range(-3,3)) = 1
        [CustomObject(_SpecSetting)] _AnisotropyLineRange("高光描线范围", Range(1, 1024)) = 1
        
    	
    	[CustomGroup] _EnvShadingSetting("环境光设置", Float) = 1.0
        [CustomObject(_EnvShadingSetting)] _SHBrightness("环境漫反射亮度", Range(0, 3)) = 1
    	
    	
    	[CustomGroup] _EmissionSetting("自发光设置",float) = 1
    	[CustomHeader(_EmissionSetting)] _WholeEmissionColorTip("特效专用自发光（不使用遮罩）", Float) = 1  
    	[CustomObject(_EmissionSetting)] [HDR]_WholeEmissionColor("全身自发光颜色", Color) = (0, 0, 0, 1)
    	
    	
	    [CustomGroup] _RimSetting("边缘光设置", float) = 1.0 
        [CustomToggle(_RimSetting, _USERIM)] _useRim("使用边缘光", Float) = 0
        [CustomToggle(_RimSetting._USERIM)] _RimDirReverse("反转边缘光方向", Float) = 0
        [CustomObject(_RimSetting._USERIM)] _RimColorBlendAlbedo("颜色混合固有色强度", Range(0, 1)) = 0.5
    	[CustomObject(_RimSetting._USERIM)] _RimDepthWidth("深度遮挡距离", Range(0, 1)) = 0.01
    	
    	[CustomBlockBegin(_RimSetting._USERIM, 0.4, 0.4, 0.4, 1, 0.9, 0.9, 0.9, 1)] _RimBlockBegin("正光面", Float) = 1
    	[CustomObject(_RimSetting._USERIM)][HDR] _RimColor("正光面边缘光颜色", Color) = (1, 1, 0.8245283, 1)
        [CustomObject(_RimSetting._USERIM)] _RimThreshold("正光面边缘光宽度", Range(0, 1)) = 0.65
    	[CustomBlockEnd(_RimSetting._USERIM)] _RimBlockEnd("正光面", Float) = 1
    	
    	[CustomBlockBegin(_RimSetting._USERIM, 0.4, 0.4, 0.4, 1, 0.9, 0.9, 0.9, 1)] _BlackRimBlockBegin("逆光面", Float) = 1
    	[CustomObject(_RimSetting._USERIM)][HDR] _BlackRimColor("逆光面边缘光颜色", Color) = (1, 1, 0.8245283, 1)
    	[CustomObject(_RimSetting._USERIM)] _RimLambertMaskThreshold("逆光面范围", Range(0, 1)) = 0.5
    	[CustomObject(_RimSetting._USERIM)] _BlackRimThreshold("逆光面边缘光宽度", Range(0, 1)) = 0.65
    	[CustomObject(_RimSetting._USERIM)] _RimLambertMaskBrightness("逆光面边缘光强度", Range(0, 1)) = 0.5
    	[CustomBlockEnd(_RimSetting._USERIM)] _BlackRimBlockEnd("逆光面", Float) = 1
        //        [CustomObject(_RimSetting._USERIM)] _RimSoftness("边缘光过渡柔软度", Range(0.001, 1)) = 0.5
        //        [CustomObject(_RimSetting._USERIM)] _Test1("范围测试", Range(0, 1)) = 0.5
//        [CustomObject(_RimSetting._USERIM)] _Test2("柔软度测试", Range(0, 20)) = 0.5
    	
    	
//    	[CustomGroup] _MultipleLightSetting("多光源设置",float) = 1.0 
//        [CustomObject(_MultipleLightSetting)] _MultipleLightIntensity("多光源亮度", Range(0, 10)) = 2.5
//        [CustomObject(_MultipleLightSetting)] _MultipleLightBlend("多光源颜色混合强度", Range(0, 1)) = 0.5
    	
    	
	    [CustomGroup] _ShadowSetting("投影设置", float) = 1.0 
        [CustomHeader(_ShadowSetting)] _ShadowColorTip("投影颜色", float) = 1 
        [CustomObject(_ShadowSetting)] _CharacterShadowColor ("投影颜色", color) = (0.53, 0.53, 0.53, 1)
        [CustomObject(_ShadowSetting)] _ShadowIntensity("投影强度", Range(0, 1)) = 1
        [CustomHeader(_ShadowSetting)] _ShadowOffsetTip("投影位置偏移", float) = 1 
        [CustomObject(_ShadowSetting)] _CustomShadowDepthBias("投影按光照方向偏移", Range(0.01, 20)) = 1
        [CustomObject(_ShadowSetting)] _CustomShadowNormalBias("投影按法线方向偏移", Range(0.01, 20)) = 0.01
    	
    	
    	[CustomGroup] _HairShadowSetting("刘海投影设置",float) = 1.0
        [CustomHeader(_HairShadowSetting)] _HairShadowOffsetTip("调整在脸部上的刘海投影距离", Float) = 1.0
        [CustomObject(_HairShadowSetting)] _HairShadowOffset("刘海投影深度偏移值", Range(-0.02,0.02)) = 0.012
        [CustomObject(_HairShadowSetting)] _HairShadowOffsetLeftRight("刘海投影位置左右偏移",Range(-1,1)) = 0
        [CustomObject(_HairShadowSetting)] _HairShadowOffsetTopDown("刘海投影位置上下偏移",Range(-1,1)) = 0
    	
    	
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
            Name "CharacterHair"
            Tags
            {
            	"RenderType" = "Opaque"
            	"Queue" = "Geometry"
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

            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            // #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT

            #pragma multi_compile_fog
            //#pragma skip_variants FOG_EXP FOG_EXP2

            #pragma multi_compile _ _WORLDFOG
            #pragma multi_compile_local_fragment _ _USERIM
            #pragma multi_compile_local_fragment _ _EFFECTFRESNEL_ON

            // #define _SCREEN_SPACE_OCCLUSION 1
            // #pragma multi_compile _SCREEN_SPACE_OCCLUSION
            
            #pragma vertex vert
            #pragma fragment frag

            #include "../ShaderLibrary/Characters_Hair_Input.hlsl"
			#include "../ShaderLibrary/Characters_Hair_ForwardPass.hlsl"
            
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

            #include "../ShaderLibrary/Characters_Hair_Input.hlsl"
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

            #include "../ShaderLibrary/Characters_Hair_Input.hlsl"
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
            #pragma multi_compile_local _ _USEOUTLINE
            #pragma multi_compile_local_vertex _Normal COLORS_AS_NORMALS  UV2_AS_NORMALS
            
            
            #pragma vertex OutLineOctahedronPassVertex_Scene
            #pragma fragment OutlinePassFragmentNew

            #include "../ShaderLibrary/Characters_Hair_Input.hlsl"
            #include "../ShaderLibrary/Characters_Outline.hlsl"
            ENDHLSL
        }
    	
    	Pass
        {//刘海投影Pass
            Name "HairShadowStencil"
            Tags { "LightMode" = "HairShadowStencil" }

            Stencil
            {
                Ref 2
                Comp GEqual
                Pass Replace
            }

            ZTest LEqual
            ZWrite Off
            ColorMask 0
            
            HLSLPROGRAM

            #pragma vertex vert_HairShadow
            #pragma fragment frag_HairShadow

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../ShaderLibrary/Characters_Hair_Input.hlsl"
			#include "../ShaderLibrary/Characters_AdditionalLightingData.hlsl"
			#include "../ShaderLibrary/Characters_StructData.hlsl"
			#include "../ShaderLibrary/Characters_Function.hlsl"
            
            struct a2v_HairShadow
            {
                float4 positionOS: POSITION;
                float4 color: COLOR;
            };

            struct v2f_HairShadow
            {
                float4 positionCS: SV_POSITION;
                float4 color: COLOR;
            };

            v2f_HairShadow vert_HairShadow(a2v_HairShadow v)
            {
                v2f_HairShadow o;

                // VertexPositionInputs positionInputs = GetVertexPositionInputs(v.positionOS.xyz);
                // o.positionCS = positionInputs.positionCS;
                //
                // float2 lightOffset = normalize(TransformWorldToHClip(_MainLightPosition).xy);
                // //乘以_ProjectionParams.x是考虑裁剪空间y轴是否因为DX与OpenGL的差异而被翻转
                // //参照https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
                // //"Similar to Texture coordinates, the clip space coordinates differ between Direct3D-like and OpenGL-like platforms"
                // lightOffset *= _ProjectionParams.x;
                // lightOffset.x *= -1;
                // o.positionCS.xy -= lightOffset * _Offset;

               
				Light mainLight = GetCharacterMainLight();

                //直接世界空间偏移，刘海更自然
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz).xyz;
                half3 temp = half3(_HairShadowOffsetLeftRight,_HairShadowOffsetTopDown,0);
                positionWS -= (mainLight.direction+temp) * _HairShadowOffset;
                
                o.positionCS = TransformWorldToHClip(positionWS);

                o.color = v.color;
                return o;
            }

            half4 frag_HairShadow(v2f_HairShadow i): SV_Target
            {
                return 0;
            }
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
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "../ShaderLibrary/Characters_Hair_Input.hlsl"
            #include "../ShaderLibrary/DepthNormalsPass.hlsl"
            
            ENDHLSL
        }
    }
    CustomEditor "CustomShaderEditor.CustomShaderGUI_Character" 
}
