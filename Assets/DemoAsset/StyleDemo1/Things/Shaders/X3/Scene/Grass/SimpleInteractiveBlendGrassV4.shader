Shader "ShiYue/URP/Grass/SimpleInteractiveBlendGrassV4"
{
	Properties
	{
//		_MainTex("渐变控制图", 2D) = "white" {}

		[CustomGroup] _BaseSetting("基础设置", float) = 1
		[CustomObject(_BaseSetting)]_GrassHeight("草高度", Float) = 0
		[CustomObject(_BaseSetting)]_ColorCover("整体颜色偏移", Color) = (1,1,1,1)
		[CustomObject(_BaseSetting)]_BottomColor("中上部颜色", Color) = (0.2, 0.5, 0.15, 1)
		[CustomObject(_BaseSetting)]_BottomColor3("根部颜色", Color) = (0.2, 0.5, 0.15, 1)
		[CustomObject(_BaseSetting)]_BottomColor2("根部吸色颜色偏移", Color) = (1, 1, 1, 1)
		[CustomObject(_BaseSetting)]_BottomWeight("地表吸色，过渡范围",Range(0,10)) = 0.4
		[CustomObject(_BaseSetting)]_BottomLocation("地表吸色，过渡位置",Range(-2,2)) = 0.4
		[CustomObject(_BaseSetting)]_ReflectionIntensity("反射强度_近",Range(0,1)) = 0.5
		[CustomObject(_BaseSetting)]_ReflectionIntensity_Far("反射强度_远",Range(0,1)) = 1
		[CustomObject(_BaseSetting)]_ReflectionColor("反射颜色",Color) = (1,1,1,1)
		[CustomObject(_BaseSetting)]_RootSoftness("根部半透", Range(0, 3)) = 2
		
		[CustomGroup] _SSAOSetting("SSAO设置", float) = 1
		[CustomObject(_SSAOSetting)]_SSAOWeight("SSAO的占比",Range(0,10)) = 0.4
		[CustomObject(_SSAOSetting)]_SSAOIntensity("SSAO的强度",Range(0,1)) = 0

		// 菲尼尔有关
		[CustomGroup] _AllFresnelSetting("整体菲尼尔设置", float) = 1
		[CustomObject(_AllFresnelSetting)]_FresnelRange("整体菲尼尔过渡系数", Range(1, 10)) = 1
		[CustomObject(_AllFresnelSetting)]_FresnelBlendRange("菲尼尔单科草过渡区范围", Range(0.1, 5)) = 1
		[CustomObject(_AllFresnelSetting)]_FresnelBlendLoc("菲尼尔单科草过渡区上下位置", Range(-5, 5)) = 1
		[CustomObject(_AllFresnelSetting)]_FresnelIntensity("整体菲尼尔强度", Range(0,2)) = 1
		
		[CustomGroup] _HighLightSetting("高光设置", float) = 1
		[CustomObject(_HighLightSetting)]_FresnelSingleRange("整体高光过渡整体范围", Range(0.1, 20)) = 1
		[CustomObject(_HighLightSetting)]_FresnelSingleBlendRange("整体高光单科草过渡区范围", Range(0.1, 5)) = 1
		[CustomObject(_HighLightSetting)]_FresnelSingleBlendLoc("整体高光单科草过渡区上下位置", Range(-5, 5)) = 1
		[CustomObject(_HighLightSetting)]_FresnelSingleIntensity("整体高光强度", Range(0, 1)) = 1
		
//		[Header(Wind)]
		[CustomGroup] _WindSetting("风效设置", float) = 1
		[CustomTexture(_WindSetting)][NoScaleOffset] _WindMap("风噪声图", 2D) = "black" {}
		[CustomObject(_WindSetting)]_NoiseColor0("noise颜色0", Color) = (1,1,1,1)
		[CustomObject(_WindSetting)]_NoiseColor1("noise颜色1", Color) = (1,1,1,1)
		[CustomObject(_WindSetting)]_WindSpeed("风速", Range(0.0, 10.0)) = 3.0
		[CustomObject(_WindSetting)]_WindDirection("风力方向", vector) = (1,0,0,0)
		[CustomObject(_WindSetting)]_WindGustStrength("风强", Range(0.0, 5)) = 0.5
		[CustomObject(_WindSetting)]_WindGustFreq("扰乱程度", Range(0.0, 10.0)) = 2
		[CustomObject(_WindSetting)]_WindBack("回摆",Range(0,1)) = 0

//		[Header(Bend)]
		[CustomGroup] _BendSetting("人物碰撞设置", float) = 1
		[CustomObject(_BendSetting)]_BendPushStrength("Push Strength", Range(0.0, 1.0)) = 1.0
		[CustomObject(_BendSetting)]_BendFlattenStrength("Flatten Strength", Range(0.0, 1.0)) = 1.0
		[CustomObject(_BendSetting)]_PerspectiveCorrection("Perspective Correction", Range(0.0, 1.0)) = 0.0
		
		[HideInInspector]_grassHeight("_grassHeight",Float) = 1
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"}
		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }
			Cull Off

			Stencil {
                Ref  0
				Comp Always
				Pass Replace
            }

			HLSLPROGRAM

			// #pragma target 3.0
			// #pragma prefer_hlslcc gles

			#pragma vertex vert 
			#pragma fragment frag

			//#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			// #pragma multi_compile _ _HEIGHTFOG
			// #pragma multi_compile _ SHADOWS_SHADOWMASK
			// #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING

			// #pragma multi_compile _ _SHADESHADOWS _CLOUDSHADOWS
			// #pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile _ ENABLE_INTERACTIVEBLENDGRASS

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS
			#pragma multi_compile _ _FORWARD_PLUS
			// #pragma multi_compile _ _PEROBJECTSHADOW  
			//#pragma multi_compile _ _PBR_LIGHTING
			//#pragma multi_compile _ _CLOUD_SHADOW_ON
			#pragma multi_compile _ _WORLDFOG
			// #pragma multi_compile _ _WIND
			#define _WIND 1
			#define _OpenCloudShadow 1
			#define _PEROBJECTSHADOW 1

			#pragma multi_compile_fog
			#pragma multi_compile_instancing
			#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
			// #define _SCREEN_SPACE_OCCLUSION 0
            // #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            // #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"


			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"		
			#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/Light/SimpleAdditionalLight.hlsl"	
			#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/X3/Scene/ShaderInclude/ShaderOtherFunction.hlsl"
			#include "Libraries/InputOld.hlsl"
			#include "Libraries/Common.hlsl"
			#include "Libraries/WindOld.hlsl"
			#include "Libraries/Bending.hlsl"
			#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/CloudShadow.hlsl"
			#include "./GrassPBR/GrassLitForwardPassV4.hlsl"
			// #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/X3/Scene/ShaderInclude/ShadowFunction.hlsl"
			
			TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
			//TEXTURE2D(_TerrainColor); SAMPLER(sampler_TerrainColor);
			//TEXTURE2D(_TerrainDepth);  SAMPLER(sampler_TerrainDepth); 
			CBUFFER_START(UnityPerMaterial)
			float4 _MainTex_ST;
			float4 _WindDirection;
			float4 _CloudGustTint;
			// half4 _TopColor;
			// half4 _BottomColor;
			// half _TopWeight;
			//half3 _HightLightColor;
			half _GrassHeight;
			half _HightLight;
			half _Metallic;
			half _Smoothness;
			half _DiffuseNormalShift;
			//half _SpecularNormalShift;
			half _ReflectionIntensity, _ReflectionIntensity_Far;
			half4 _ReflectionColor;
			half4 _ColorCover;
			half _BottomWeight;
			half _BottomLocation;


			half _WindSpeed;		
			float _WindGustStrength;
			float _WindGustFreq;

			half _RootSoftness;


			float _BendPushStrength;
			float _BendFlattenStrength;
			float _PerspectiveCorrection;
			//float _grassHeight;
			float _WindBack;
			CBUFFER_END

			//UNITY_INSTANCING_BUFFER_START(Props)
			//UNITY_DEFINE_INSTANCED_PROP(half4, _giColor)
			//UNITY_DEFINE_INSTANCED_PROP(half4, _terrainNormalWS)
			//UNITY_DEFINE_INSTANCED_PROP(half, _allScale)
			//UNITY_DEFINE_INSTANCED_PROP(half, _LOD)
			//UNITY_INSTANCING_BUFFER_END(Props)

			//#define lodLevel  0
			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output); // necessary only if you want to access instanced properties in the fragment Shader.
				
				float3 pivotWS = float3(UNITY_MATRIX_M[0][3], UNITY_MATRIX_M[1][3], UNITY_MATRIX_M[2][3]);
				//input.positionOS.xyz = SizeNoise(pivotWS, input.positionOS, _GrassHeight);
				WindSettings wind = PopulateWindSettings(0, _WindSpeed, _WindDirection, AO_MASK,
					_WindGustStrength , _WindGustFreq,_WindBack);
				half bendAmount = 0;
			//	input.positionOS.xyz = GetSimplerWind(input.positionOS, input.uv.xy, wind, pivotWS, bendAmount);
				//input.positionOS.y *= _GrassHeight;
				
				VertexOutput data = GetVertexData(input);
				// 地表颜色往刷色过渡
				float lerpValue = max(max(data.lightMapColor.r, data.lightMapColor.g), data.lightMapColor.b);
				data.color.rgb = lerp(data.color.rgb, data.lightMapColor, lerpValue);
				
				// 颜色noise
				float2 noiseUV = data.pivotWS.xz * 0.02;
				half colorNoise = SAMPLE_TEXTURE2D_LOD(_WindMap, sampler_WindMap, noiseUV, 0).r;
				
				float3 wPos = data.positionWS;
				
				// 配置交互位移
				// 获取单科草的 SinglePivotWS
				half3 singlePivotOS = float3(-input.uv.x, 0.0h,input.uv.y);
				half3 singlePivotWS = TransformObjectToWorld(singlePivotOS);
				BendSettings bending = PopulateBendSettings(BEND_MASK, _BendPushStrength, _BendFlattenStrength, _PerspectiveCorrection);
				float4 bendVec = GetBendOffset(wPos, singlePivotWS, bending);
				// 偏移位置
				wPos.xyz = lerp(wPos.xyz, bendVec.xyz, bendVec.a);

				
				output.positionOS = input.positionOS;
				output.positionCS = TransformWorldToHClip(wPos);
				output.positionWSAndFogFactor = float4(wPos, ComputeFogFactor(output.positionCS.z));			

				output.var_0.w = bendAmount;
				output.normalWS = TransformObjectToWorldNormal(input.normalOS);
				half3 viewDir = normalize(GetWorldSpaceViewDir(output.positionWSAndFogFactor.xyz));
				output.var_0.xyz = reflect(viewDir, output.normalWS);
							
				output.color = data.color;
				output.uvAndEdge.xy = TRANSFORM_TEX(input.uv, _MainTex);
				// output.lightMapColor = data.lightMapColor;
				output.pivotWS = pivotWS;
				#ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
					output.shadowCoord = TransformWorldToShadowCoord(wPos);
				#endif

				// rongqingmei 测试 data.color 就是地表带光照的吸色
				// output.color.rgb = data.color;
				// return output;

				output.positionOS.w = input.lightmapUV.x;
				
				half distanceLerp = saturate((output.positionCS.w - 10) * 0.01); // 远处的就保持地表色
				output.terrainColor.a = distanceLerp;
				_BottomColor2 = lerp(_BottomColor2, 1, distanceLerp);
				half3 terrainColor = data.color * _BottomColor2;

				
				
				// 底部颜色控制基色
				
				half3 albedo = lerp(_BottomColor * terrainColor, terrainColor, distanceLerp);
				albedo *= lerp(_NoiseColor0, _NoiseColor1, colorNoise);
				albedo = lerp(_BottomColor3, albedo, input.lightmapUV.x);

				// albedo = distanceLerp;
				albedo = lerp(terrainColor, albedo, saturate(output.positionOS.w * _BottomWeight));
				
			// spec = lerp(0, spec, saturate((positionOSY - _FresnelSingleBlendLoc) * _FresnelSingleBlendRange));
				// 输出
				output.terrainColor.rgb = data.color;
				output.color.rgb = albedo * _ColorCover;
				output.color.rgb = lerp(output.color, terrainColor, distanceLerp);
				output.screenPos = ComputeScreenPos(output.positionCS);

				// rongqingmei
				// output.color.rgb = lerp(output.color.rgb, data.lightMapColor, lerpValue);
				// output.color = half4(data.lightMapColor.rgb, 1.0f);
				return output;
			}

			half4 frag(Varyings input, half facing : VFACE) : SV_Target
			{
				
				UNITY_SETUP_INSTANCE_ID(input);			
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				// rongqingmei
				// return input.color;
				
				// 配置 InputData
                InputData inputData = (InputData)0;
                half3 highLightNormal = half3(0, 1, 0);
                inputData.positionWS = input.positionWSAndFogFactor.xyz;
				inputData.positionCS = input.positionCS;
                inputData.normalWS.xyz = input.normalWS.xyz;
				// inputData.normalWS.xyz = highLightNormal;
                inputData.viewDirectionWS = SafeNormalize(GetCameraPositionWS() - input.positionWSAndFogFactor.xyz);
				float4 shadowCoord = TransformWorldToShadowCoord(input.positionWSAndFogFactor.xyz);
                inputData.shadowCoord = shadowCoord;
                inputData.fogCoord = input.positionWSAndFogFactor.w;
				inputData.bakedGI = SampleSH(inputData.normalWS);
                // 接受 gi 的强度
				half mask = 1.0;
                inputData.shadowMask = mask;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

				half height = input.positionOS.w;

				half dither = RootDither(input.screenPos, _RootSoftness);
				
				half4 shadowMask = CalculateShadowMask(inputData);

				half bakeSkyAO = 1;
				half oc = 1;
				half3 GIColor = inputData.bakedGI;
				// half3 GIColor = SampleProbeVolume(half4(inputData.normalWS,1),inputData.positionWS, oc, inputData.bakedGI,bakeSkyAO);

				
			    // // hbao 修改 这里对 SSAO 做特殊处理，直接后面直接乘以 AO 来达到风格化的效果
			    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData.normalizedScreenSpaceUV, oc);
				Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);

				half distanceToCame = input.terrainColor.a;
				
				// SurfaceData
				Light light = GetMainLight();
				half3 reflectV = reflect(inputData.viewDirectionWS, inputData.normalWS);
				half lamb = saturate(dot(-light.direction, reflectV));
				lamb *= Pow4(lamb);
				lamb *= Pow4(lamb);
				// 这里需要把阴影拿出来 乘进去才对，
				half3 heighLightAlbedo = (lamb * light.color) * mainLight.shadowAttenuation;
				
				half3 reflectSH = SampleSH(input.var_0.xyz) + heighLightAlbedo;
				reflectSH *= _ReflectionColor;
				half reflectionIntensity = lerp(_ReflectionIntensity, _ReflectionIntensity_Far, saturate(distanceToCame*3));
				half bendAmount = (input.var_0.w * input.var_0.w  * input.var_0.w) * reflectionIntensity;
				bendAmount *= mainLight.shadowAttenuation;
				half3 albedo = lerp(input.color, reflectSH, saturate(bendAmount));
				SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo + heighLightAlbedo * albedo * 0.2;
				// surfaceData.albedo = input.color;
                surfaceData.alpha = 1;
				half smooth = lerp(0.2, 0.5, input.var_0.w);
                surfaceData.smoothness = lerp(smooth, 0, distanceToCame) * height; // 远处和地表的数值一致
                surfaceData.metallic = 0;
                surfaceData.specular = 0;
				surfaceData.occlusion = oc;
				
				half4 color = 1;
				// surfaceData.albedo = input.terrainColor;
				color = UniversalFragmentPBR(inputData, surfaceData);
				color.a = 1;
				
				// 混合雾
				color.rgb = saturate(MixFog(color, input.positionWSAndFogFactor.w));
				
				#ifdef _WORLDFOG
					float3 positionWS = input.positionWSAndFogFactor.xyz;
					WarFogSample(color.rgb, positionWS);
                #endif
				// return half4(input.terrainColor.rgb, 1);
				return color;
			}
			ENDHLSL	
		}	

		Pass
		{
			Name "DepthOnly"
			Tags{"LightMode" = "DepthOnly"}

			ZWrite On
			ColorMask 0
			Cull Off

			HLSLPROGRAM
			// Required to compile gles 2.0 with standard srp library
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 3.0

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
			#pragma multi_compile _ ENABLE_INTERACTIVEBLENDGRASS
			#pragma vertex DepthOnlyVertex
			#pragma fragment DepthOnlyFragment

						#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"		
			#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/Light/SimpleAdditionalLight.hlsl"	

			#include "Libraries/InputOld.hlsl"
			#include "Libraries/Common.hlsl"
			#include "Libraries/WindOld.hlsl"
			#include "Libraries/Bending.hlsl"		

			CBUFFER_START(UnityPerMaterial)
			float4 _MainTex_ST;

			half4 _TopColor;
			half _TopWeight;

			half4 _BottomColor;
			half _BottomWeight;

			float _WindGustStrength;
			float _WindGustFreq;
			float4 _WindDirection;
			float _WindBack;

			half _WindSpeed;		
			half _RootSoftness;

			float4 _CloudGustTint;

			float _BendPushStrength;
			float _BendFlattenStrength;
			float _PerspectiveCorrection;
			CBUFFER_END

			// UNITY_INSTANCING_BUFFER_START(Props)
			// UNITY_DEFINE_INSTANCED_PROP(half, _allScale)
			// UNITY_INSTANCING_BUFFER_END(Props)

			Varyings DepthOnlyVertex(Attributes input)
			{
				Varyings output = (Varyings)0;

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output); // necessary only if you want to access instanced properties in the fragment Shader.

				//half allScale = UNITY_ACCESS_INSTANCED_PROP(Props, _allScale);
				float3 pivotWS = float3(UNITY_MATRIX_M[0][3], UNITY_MATRIX_M[1][3], UNITY_MATRIX_M[2][3]);
				input.positionOS.xyz = SizeNoise(pivotWS, input.positionOS);
				WindSettings wind = PopulateWindSettings(0, _WindSpeed, _WindDirection, AO_MASK,
					_WindGustStrength , _WindGustFreq,_WindBack);
				half bendAmount = 0;
				input.positionOS.xyz = GetSimplerWind(input.positionOS, input.uv.xy, wind, pivotWS, bendAmount);
				
				VertexOutput data = GetVertexData(input);
				
				// 配置交互位移
				float3 wPos = data.positionWS;
				// 获取单科草的 SinglePivotWS
				half3 singlePivotOS = float3(-input.uv.x, 0.0h,input.uv.y);
				half3 singlePivotWS = TransformObjectToWorld(singlePivotOS);
				BendSettings bending = PopulateBendSettings(BEND_MASK, _BendPushStrength, _BendFlattenStrength, _PerspectiveCorrection);
				float4 bendVec = GetBendOffset(wPos, singlePivotWS, bending);
				// 偏移位置
				wPos.xyz = lerp(wPos.xyz, bendVec.xyz, bendVec.a);

				//float3 offsets = lerp(windVec.xyz, bendVec.xyz, bendVec.a);
				//wPos.xyz += bendVec.xyz;
				//wPos.xyz += offsets.xyz;
				
				output.normalWS = normalize(data.normalWS);
				output.positionCS = TransformWorldToHClip(wPos);
				output.screenPos = ComputeScreenPos(output.positionCS);
				// output.positionCS = 0;
				return output;
			}

			half4 DepthOnlyFragment(Varyings input) : SV_TARGET
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				half dither = RootDither(input.screenPos, _RootSoftness);

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

            ZWrite On
            Cull Off

			HLSLPROGRAM
			// Required to compile gles 2.0 with standard srp library
			// #pragma prefer_hlslcc gles
			// #pragma exclude_renderers d3d11_9x
			#pragma target 3.0

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
			#pragma multi_compile _ ENABLE_INTERACTIVEBLENDGRASS
			#pragma vertex DepthOnlyVertex
			#pragma fragment DepthOnlyFragment

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"		
			#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/Light/SimpleAdditionalLight.hlsl"	

			#include "Libraries/InputOld.hlsl"
			#include "Libraries/Common.hlsl"
			#include "Libraries/WindOld.hlsl"
			#include "Libraries/Bending.hlsl"	

			CBUFFER_START(UnityPerMaterial)
			half _GrassHeight;
			float4 _MainTex_ST;

			half4 _TopColor;
			half _TopWeight;

			half4 _BottomColor;
			half _BottomWeight;

			//float4 _WindDirection;
			//float _WindSpeed;
			float _WindGustStrength;
			float _WindGustFreq;
			float4 _WindDirection;
			float _WindBack;

			half _WindSpeed;
			half _RootSoftness;
			
			float4 _CloudGustTint;

			float _BendPushStrength;
			float _BendFlattenStrength;
			float _PerspectiveCorrection;
			CBUFFER_END

			// UNITY_INSTANCING_BUFFER_START(Props)
			// UNITY_DEFINE_INSTANCED_PROP(half, _allScale)
			// UNITY_INSTANCING_BUFFER_END(Props)

			Varyings DepthOnlyVertex(Attributes input)
			{
				Varyings output = (Varyings)0;

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output); // necessary only if you want to access instanced properties in the fragment Shader.

				//half allScale = UNITY_ACCESS_INSTANCED_PROP(Props, _allScale);
				float3 pivotWS = float3(UNITY_MATRIX_M[0][3], UNITY_MATRIX_M[1][3], UNITY_MATRIX_M[2][3]);
				input.positionOS.xyz = SizeNoise(pivotWS, input.positionOS, _GrassHeight);
				WindSettings wind = PopulateWindSettings(0, _WindSpeed, _WindDirection, AO_MASK,
					_WindGustStrength , _WindGustFreq,_WindBack);
				half bendAmount = 0;
				input.positionOS.xyz = GetSimplerWind(input.positionOS, input.uv.xy, wind, pivotWS, bendAmount);
				input.positionOS.y *= _GrassHeight;
				
				VertexOutput data = GetVertexData(input);
				
				// 配置交互位移
				float3 wPos = data.positionWS;
				// 获取单科草的 SinglePivotWS
				half3 singlePivotOS = float3(-input.uv.x, 0.0h,input.uv.y);
				half3 singlePivotWS = TransformObjectToWorld(singlePivotOS);
				BendSettings bending = PopulateBendSettings(BEND_MASK, _BendPushStrength, _BendFlattenStrength, _PerspectiveCorrection);
				float4 bendVec = GetBendOffset(wPos, singlePivotWS, bending);
				// 偏移位置
				wPos.xyz = lerp(wPos.xyz, bendVec.xyz, bendVec.a);

				//float3 offsets = lerp(windVec.xyz, bendVec.xyz, bendVec.a);
				//wPos.xyz += bendVec.xyz;
				//wPos.xyz += offsets.xyz;
				
				output.normalWS = normalize(data.normalWS);
				output.positionCS = TransformWorldToHClip(wPos);
				output.screenPos = ComputeScreenPos(output.positionCS);
				// output.positionCS = 0;
				return output;
			}

			void DepthOnlyFragment(Varyings input, out half4 outNormalWS : SV_Target0)
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                // float3 normalWS = NormalizeNormalPerPixel(input.normalWS);

				half dither = RootDither(input.screenPos, _RootSoftness);
				
				float3 normalWS = normalize(input.normalWS);
                outNormalWS = half4(normalWS, 0.0);
			}

			ENDHLSL
		}
	}
	FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
