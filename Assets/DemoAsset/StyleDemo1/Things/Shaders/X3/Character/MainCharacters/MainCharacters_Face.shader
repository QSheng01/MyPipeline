Shader "X3/Character/MainCharacters_Face"
{
    Properties
    {
    	[HideInInspector]_CharacterEffectGrade("角色效果等级", Int) = 0
    	
	    // 固有色
        [CustomGroup] _MapSetting("基本设置",float) = 1.0
        [CustomTexture(_MapSetting)] _BaseMap("固有色贴图 A (0:眼睛部分)", 2D) = "white" {}
        [CustomObject(_MapSetting)] _BaseColor(" ", Color) = (1, 1, 1, 1)
        
        
        [CustomGroup] _SDFShadowSetting("SDF过渡阴影设置",float) = 1.0
        [CustomHeader(_SDFShadowSetting)]  _SDFTip1("R:SDF  G:自发光  B:脸部与脖子过渡遮罩  A:眼睛特殊阴影遮罩", Float) = 1.0
		[CustomTexture(_SDFShadowSetting)][NoScaleOffset] _LightMap("SDF贴图)",2D) = "white" {}
        [CustomHeader(_SDFShadowSetting)]  _SDFInfo1("（在看到该提示时可以在角色中心看到一红一蓝两条线）",float) = 1.0
		//[CustomHeader(_SDFShadowSetting)]  _SDFInfo2("           需更改“正面朝向”选项至 红线与角色正面朝向一致",float) = 1.
		[CustomHeader(_SDFShadowSetting)]  _SDFInfo2("提示：需更改“正面朝向”选项至 红线与角色正面朝向一致",float) = 1.
		[SDFFaceDir(_SDFShadowSetting,red)] _FaceForward("正面朝向", Vector) = (0, 0, 1, 0)
		[CustomHeader(_SDFShadowSetting)]  _SDFInfo3("提示：需更改“右侧朝向”选项至 蓝线与角色右手朝向一致",float) = 1.0
		[SDFFaceDir(_SDFShadowSetting,blue)] _FaceRight("右侧朝向", Vector) = (1, 0, 0, 0)
//		[CustomToggle(_SDFShadowSetting)] _Reversal ("左右反转",Float) = 0
//        [CustomObject(_SDFShadowSetting)] _FaceArea ("夹角最大范围",Range(0, 180)) = 0
    	[CustomHeader(_SDFShadowSetting)] _SpecialSDFTip ("特殊阴影：在不同角度下更多出现脸五分之一宽度的阴影", Float) = 1
        [CustomToggle(_SDFShadowSetting)] _UseSpecialSDF ("使用特殊阴影", Float) = 1
        [CustomObject(_SDFShadowSetting)] _SpecialSdfAcceleration_Front("（特殊阴影）正光面进入阴影速度",Range(0, 1)) = 0.5
    	[CustomObject(_SDFShadowSetting)] _SpecialSdfAcceleration_Black("（特殊阴影）进入背光面速度",Range(0, 1)) = 0.5
        
        
        [CustomGroup]_DiffuseSetting("阴影设置",float) = 1.0
//        [CustomKeywordEnum(_DiffuseSetting, _SDF_CEL, _SDF_RAMP, _RAMP, _SDF_MIX_RAMP)] _DiffuseType ("漫反射类型", float) = 0
        [CustomObject(_DiffuseSetting)] _HColor ("亮部颜色",Color) = (1,1,1,1)
        [CustomObject(_DiffuseSetting)] _DarkColor ("阴影颜色",Color) = (0.5,0.5,0.5,0.5)
        [CustomObject(_DiffuseSetting)] _SpecialSdfRange ("阴影范围",Range(0,1)) = 0.2
        [CustomObject(_DiffuseSetting)] _RampSize ("阴影过渡柔软度",Range(0.001,1)) = 0.3
        [CustomHeader(_DiffuseSetting)]  _DiffuseTip1("Ramp阴影",float) = 1.
        [CustomToggle(_DiffuseSetting)] _UseHalfLambert1 ("Ramp使用HalfLambert（偏卡通）", Float) = 1 // no key
        [CustomTexture(_DiffuseSetting)] _RampTex("Ramp过渡贴图",2D) = "white" {}
        [CustomObject(_DiffuseSetting)] _RampUV_Y ("ID 偏移",Range(0, 1)) = 0.5

        
        [CustomGroup] _EmissionSetting("自发光设置",float) = 1
        [CustomHeader(_EmissionSetting)] _EmissionTip("自发光通道：SDF贴图.G", Float) = 1 
        [CustomObject(_EmissionSetting)] [HDR]_EmissionColor ("自发光颜色",Color) = (1,1,1,1)
        [CustomObject(_EmissionSetting)] _Emission_Instensity("自发光强度",Range(0,1)) = 0
        [CustomObject(_EmissionSetting)] _Emission_Lerp("混合固有色强度",Range(0, 1)) = 0
    	
    	
    	[CustomGroup] _EnvShadingSetting("环境光设置", Float) = 1.0
        [CustomObject(_EnvShadingSetting)] _SHBrightness("环境光亮度", Range(0, 3)) = 1
    	
    	
	    [CustomGroup] _RimSetting("背光边缘光设置", float) = 1.0 
        [CustomToggle(_RimSetting, _USERIM)] _useRim("使用背光边缘光", Float) = 0
        [CustomObject(_RimSetting._USERIM)][HDR] _RimColor("边缘光颜色", Color) = (1, 1, 0.8245283, 1)
    	[CustomObject(_RimSetting._USERIM)] _RimColorBlendAlbedo("颜色混合固有色强度", Range(0, 1)) = 0.5
    	[CustomObject(_RimSetting._USERIM)] _RimLambertMaskThreshold("边缘光受光照影响程度", Range(0, 1)) = 0.5
        [CustomObject(_RimSetting._USERIM)] _RimThreshold("边缘光范围", Range(0, 1)) = 0.65
        [CustomObject(_RimSetting._USERIM)] _RimSoftness("边缘光过渡柔软度", Range(0.001, 1)) = 0.244    	
    	
    	
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
        [CustomObject(_HairShadowSetting)]_HairShadowColor("刘海投影颜色",Color) = (0.9, 0.72, 0.7534883, 0.5254902)
    	
    	
    	[CustomGroup] _HeavyShadowSetting("重阴影设置",float) = 1.0
//        [CustomToggle(_HeavyShadowSetting, _HEAVYSHADOW)] _UseHeavyShadow("使用重阴影", Float) = 0
        [CustomObject(_HeavyShadowSetting)] _HeavyShadowColor("重阴影颜色", Color) = (0.9150943, 0.7208526, 0.74760777, 1)
        [CustomObject(_HeavyShadowSetting)] _HeavyShadowRange("重阴影范围", Range(0, 1)) = 0.21
        [CustomObject(_HeavyShadowSetting)] _HeavyShadowSoftness("重阴影柔软度", Range(0, 1)) = 0.6
        [CustomObject(_HeavyShadowSetting)] _HeavyShadowIntensity("重阴影强度", Range(0, 1)) = 1

    	
    	
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
            Name "CharacterFace"
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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT

			
            
            #pragma multi_compile_fog
            //#pragma skip_variants FOG_EXP FOG_EXP2

            #pragma multi_compile _ _WORLDFOG
            #pragma multi_compile_local_fragment _ _EFFECTFRESNEL_ON
            
            #pragma vertex vert
            #pragma fragment frag

            #include "../ShaderLibrary/MainCharacters_Face_Input.hlsl"
            #include "../ShaderLibrary/MainCharacters_Face_ForwardPass.hlsl"
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

            #include "../ShaderLibrary/MainCharacters_Face_Input.hlsl"
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

            #include "../ShaderLibrary/MainCharacters_Face_Input.hlsl"
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
            
			#include "../ShaderLibrary/MainCharacters_Face_Input.hlsl"
            #include "../ShaderLibrary/Characters_Outline.hlsl"
			ENDHLSL
           
        }
    	Pass
        {
        	//刘海投影Pass
            Name "HairShadowStencilRender"
            Tags { "LightMode" = "HairShadowStencilRender" }

            Stencil
            {
                Ref 1
                Comp Less
                Pass Replace
            }
            ZTest LEqual
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            
            HLSLPROGRAM
            #pragma vertex vert_HairShadow
            #pragma fragment frag_HairShadow

            #pragma shader_feature_local _ _EMISSION
            #pragma multi_compile_local_fragment _ _DISSLOVE_ON
            #pragma multi_compile_local_fragment _ _WEAKNESS_ON

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/Effects_Addition.hlsl"
            #include "../ShaderLibrary/MainCharacters_Face_Input.hlsl"
            
            struct a2v_HairShadow
            {
                float4 positionOS: POSITION;
                float4 color: COLOR;
                float3 normal:NORMAL;
                float2 uv: TEXCOORD;
            };

            struct v2f_HairShadow
            {
                float4 positionCS: SV_POSITION;
                float4 color: COLOR;
                float2 uv: TEXCOORD0;
                float3 normal:TEXCOORD1;
                float3 posWS:TEXCOORD2;
            };
            
            v2f_HairShadow vert_HairShadow(a2v_HairShadow v)
            {
                v2f_HairShadow o;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = positionInputs.positionCS;
                o.normal = TransformObjectToWorldNormal(v.normal);
                o.posWS = TransformObjectToWorld(v.positionOS);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }
            
            half4 frag_HairShadow(v2f_HairShadow i): SV_Target
            {
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);

                /*#if _EMISSION
                
		        half emissionValue  = emissionMap.r;
	
		        // surfData.emission = _Emission_Instensity * lerp(surfData.albedo, _EmissionColor, emissionValue);
		        half3 emission = lerp(_EmissionColor, _EmissionColor * albedo, _Emission_Lerp);
		        emission *= emissionValue;
		        emission *= _Emission_Instensity;
                albedo += emission;
                #endif*/
                
                // albedo *= _FaceColor.rgb * _MainLightColor.rgb;


                //half alpha = _HairShadowColor.a * Luminance(albedo);
                half alpha = _HairShadowColor.a;
                // alpha *= 1 - lightMap.g;
                // alpha *= 1 - emissionMap.r;
                
                return half4(_HairShadowColor.rgb * albedo, alpha);
                
                return half4(albedo, 1);
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

            // //--------------------------------------
            // // GPU Instancing
            // #pragma multi_compile_instancing
            // #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"


            #include "../ShaderLibrary/MainCharacters_Face_Input.hlsl"
            #include "../ShaderLibrary/DepthNormalsPass.hlsl"
            ENDHLSL
        }
//    	Pass
//        {
//            Name "DepthNormals_Outline"
//            Tags
//            {
//                "LightMode" = "DepthNormalsOnly"
//            }
//            
//            // -------------------------------------
//            // Render State Commands
//            ZWrite On
//            Cull Front
//
//            HLSLPROGRAM
//            // #pragma target 2.0
//
//             
//            // -------------------------------------
//            // Shader Stages
//            #pragma vertex DepthNormalsVertex_Outline
//            #pragma fragment DepthNormalsFragment_Outline
//
//            // -------------------------------------
//            // Material Keywords
//            // #pragma shader_feature_local _NORMALMAP
//            // #pragma shader_feature_local _PARALLAXMAP
//            // #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
//            // #pragma shader_feature_local_fragment _ALPHATEST_ON
//            // #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
//            #pragma multi_compile_local _ _USEOUTLINE
//            #pragma multi_compile_local_vertex _Normal COLORS_AS_NORMALS  UV2_AS_NORMALS
//
//            // -------------------------------------
//            // Unity defined keywords
//            // #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
//
//            // -------------------------------------
//            // Universal Pipeline keywords
//            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
//
//            // //--------------------------------------
//            // // GPU Instancing
//            // #pragma multi_compile_instancing
//            // #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
//
//            // -------------------------------------
//            // Includes
//            //#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
//            #include "../ShaderLibrary/MainCharacters_Face_Input.hlsl"
//            #include "../ShaderLibrary/Characters_Outline.hlsl"
//            
//            ENDHLSL
//        }
    }
    CustomEditor "CustomShaderEditor.CustomShaderGUI_Character" 
}
