Shader "ShiYue/URP/Water/URPWater"
{
	Properties
	{
		//[Header(Refraction)]
		[KeywordEnum(Colors, Gradient)]
		_ColorMode("Color: Mode", Float) = 0

		_Color("Color", color) = (0.6,1,0.8,1)
		_DepthColor("DepthColor", color) = (0,0.26,0.4,1)
		_UnderWaterColor("UnderWaterColor", color) = (0.25, 0.33, 0.45, 0.25)
		_RefractionColor("Refraction Ramp", 2D) = "white" {}
		_DepthStart("Depth Start", float) = 0
		_DepthEnd("Depth End", float) = 1
		//_AmbientColor("AmbientColor", color) = (1,1,1,1)
		_Distortion("Distortion", range(0,128)) = 32
		_Smoothness("Smoothness", range(0,1)) = 0.5
		_SpecColor("SpecularColor", color) = (1,1,1,1)

		// Normals
		[KeywordEnum(Single,Dual,FlowMap)]
		_NormalsMode("NormalsMode", Float) = 0

		_NormalMapA("Normal Map A", 2D) = "bump" {}
		_NormalMapATilings("Normal Map A: Tilings", vector) = (1,1,1,1)
		_NormalMapASpeeds("Normal Map A: Speeds", vector) = (1,1,0.5,0.5)
		_NormalMapAIntensity("Normal Map A: Intensity", Range(0,1)) = 1

		_NormalMapB("Normal Map B", 2D) = "bump" {}
		_NormalMapBTilings("Normal Map B: Tilings", vector) = (1,1,1,1)
		_NormalMapBSpeeds("Normal Map B: Speeds", vector) = (1,1,1,1)
		_NormalMapBIntensity("Normal Map B: Intensity", Range(0,1)) = 1

		_FlowMap("Flow Map ", 2D) = "grey" {}
		_FlowTiling("Flow Tiling", vector) = (1,1,0,0)
		_FlowSpeed("Flow Speed", float) = 5
		_FlowIntensity("Flow Intensity", float) = 0.1

		// EdgeFade
		[Toggle(_EDGEFADE_ON)] _EdgeFade("Enable EdgeFade", Float) = 0
		_EdgeSize("EdgeFade: Size", float) = 1

		// Foam
		[Toggle(_FOAM_ON)] _Foam("Enable Foam", Float) = 0
		_FoamColor("Foam: Color", color) = (1,1,1,1)
		_FoamTex("Foam: Texture", 2D) = "white" {}
		_FoamTiling("Foam: Tiling", vector) = (1,1,1,1)
		_FoamSize("Foam: Size", float) = 0.1
		[Toggle(_FOAM_RIPPLE_ON)] _FoamRipples("Enable Foam Ripples", Float) = 0
		_FoamRippleDistance("Foam: RippleDistance", float) = 1
		_FoamRippleSize("Foam: RippleSize", float) = 1
		_FoamRippleSpeed("Foam: RippleSpeed", float) = 0.1
		_FoamDistortion("Foam: Distortion", float) = 0.1

		[Toggle(_FOAM_WHITECAPS_ON)] _FoamWhiteCaps("Enable White Caps", Float) = 0
		_FoamCapsIntensity("Foam: Caps Intensity", float) = 1
		_FoamCapsRangeMin("Foam: Caps Range Min", float) = 0
		_FoamCapsRangeMax("Foam: Caps Range Max", float) = 1

		// Caustics
		[Toggle(_CAUSTICS_ON)] _Caustics("Enable Caustics", Float) = 0
		[KeywordEnum(2D, 3D)] _CausticsMode("Use 3D Texture", Float) = 0
		[Toggle(_CAUSTICS_ANGLE_ON)] _CausticsAngleMask("Caustics Angle Mask", Float) = 0
		[Toggle(_CAUSTICS_DIRECTION_ON)] _CausticsDirectionMask("Caustics Direction Mask", Float) = 0

		_CausticsTex("Caustics Texture", 2D) = "black" {}
		_CausticsIntensity("Intensity", float) = 4
		_CausticsTiling("Caustics Tiling", vector) = (1,1,1,1)
		_CausticsStart("Start", float) = 0
		_CausticsEnd("End", float) = 1
		_CausticsSpeed("Speed", vector) = (1,1,-1,-1)
		_CausticsDistortion("Distortion", Range(0,1)) = 0.1

		// Scattering
		[Toggle(_SCATTERING_ON)] _Scattering("Enable Scattering", Float) = 0
		_ScatteringColor("Scattering: Color", color) = (1,1,1,1)
		_ScatteringIntensity("Scattering: Intensity", Float) = 1
		_ScatteringRangeMin("Scattering: Min", Float) = 0
		_ScatteringRangeMax("Scattering: Max", Float) = 1

		//[Header(Reflections)]
		[KeywordEnum(Off,CubeMap,Probes,SSPR,PR)]
		_ReflectionMode("Reflection: Mode", Float) = 0
		_CubemapTexture("Reflection: Cubemap", CUBE) = "" {}
		
		// 一级cubeMap反射有关
		_FirstReflectionColor("一级反射颜色叠加", color) = (1,1,1,1)
		_FirstReflectionTurnDark("一级反射整体变暗", Range(0, 2.0)) = 0.6
		_FirstReflectionLightRange("一级反射亮度范围", Range(0, 2.0)) = 0.3
		_FirstReflectionMostLight("一级反射最高亮度阈值", Range(0, 1.0)) = 0.4
		
		_ReflectionFresnel("Reflection: Fresnel", Range(0,16)) = 4
		_ReflectionFresnelNormal("Reflection: Fresnel Normal", Range(0,1)) = 0.25
		_ReflectionIntensity("Reflection: Intensity", Range(0,1)) = 1
		_ReflectionDistortion("Reflection: Distortion", Range(0,1)) = 0.5
		_ReflectionRoughness("Reflection: Roughness", Range(0,1)) = 0.1
		_PlanarReflectionDistortion("Planar Reflection Distortion", Range(0, 5)) = 1
		_SSPRLerpScale("SSRP Lerp Scale", Range(0,1)) = 0
		_ReflectionColor("反射颜色", color) = (1,1,1,1)
		
		_WaveStartDistance("Start Distance", float) = 0
		_WaveEndDistance("End Distance", float) = 1000
		
		[HideInInspector] _CullMode("CullMode", float) = 0
	}

	SubShader
	{
		Tags{
		"RenderType" = "Transparent"
		"Queue" = "Transparent"
		"RenderPipeline" = "UniversalPipeline"
		}
		Pass
		{
			Name "Front"
			Tags { "LightMode" = "UniversalForward" }
			Cull [_CullMode]

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			//#define MAINPASS	

			// #pragma multi_compile _ _FORWARD_PLUS
			// Lighting
			// #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			// #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			//#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			//#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
			//#pragma multi_compile _ _SHADOWS_SOFT
			// URP Water Options
			#pragma shader_feature_local _ _COLORMODE_COLORS _COLORMODE_GRADIENT
			#pragma shader_feature_local _ _NORMALSMODE_SINGLE _NORMALSMODE_DUAL _NORMALSMODE_FLOWMAP
			#pragma shader_feature_local _ _REFLECTIONMODE_CUBEMAP _REFLECTIONMODE_PROBES _REFLECTIONMODE_SSPR _REFLECTIONMODE_PR

			#pragma shader_feature_local _CAUSTICS_ON
			#pragma shader_feature_local _ _CAUSTICSMODE_2D _CAUSTICSMODE_3D
			#pragma shader_feature_local _CAUSTICS_ANGLE_ON
			#pragma shader_feature_local _CAUSTICS_DIRECTION_ON
		
			#pragma shader_feature_local _SCATTERING_ON
			#pragma shader_feature_local _CAPS_SCATTERING_ON
			#pragma shader_feature_local _EDGEFADE_ON
			#pragma shader_feature_local _FOAM_ON
			#pragma shader_feature_local _FOAM_RIPPLE_ON
			#pragma shader_feature_local _FOAM_WHITECAPS_ON

			// GPU Instancing
			#pragma multi_compile_instancing
			//Fog
			#pragma multi_compile_fog
			#pragma target 3.0

			#define _WORLD_UV
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "HLSL//URPWaterVariables.hlsl"  
			#include "HLSL//URPWaterLighting.hlsl"
			#include "HLSL//URPWaterScattering.hlsl"
			#include "HLSL//URPWaterRefraction.hlsl"
			#include "HLSL//URPWaterReflections.hlsl" 
			#include "HLSL//URPWaterNormals.hlsl"
			#include "HLSL//URPWaterFoam.hlsl"
			#include "HLSL//URPWaterAlpha.hlsl"
			#include "HLSL//URPWaterWave.hlsl"
			#include "HLSL//URPWaterCommon.hlsl"  
			ENDHLSL
		}
	}
	CustomEditor "URPWaterEditor"
}
