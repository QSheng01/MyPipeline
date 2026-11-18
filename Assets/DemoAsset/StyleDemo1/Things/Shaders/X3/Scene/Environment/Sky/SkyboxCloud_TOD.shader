Shader "ShiYue/URP/Cloud/SkyboxCloud_TOD"
{
    Properties
    {
        [CustomGroup] _BaseSetting("基础设置", float) = 1.0
    	[CustomToggle(_BaseSetting,_TOD)]			_TOD					    ("是否受TOD影响", float) = 0
        [CustomTexture(_BaseSetting)]               _CloudTexture               ("【基础】云贴图(R:明暗Mask G:高光Mask B:透明Mask A:SDF)", 2D) = "white" {}
    	[CustomObject(_BaseSetting)][HDR]           _BaseDarkColor              ("【基础】【暗部】暗部颜色", Color) = (1,1,1,1)
    	[CustomObject(_BaseSetting)]                _DarkAlphaRange             ("【基础】【暗部】暗部过渡范围", Range(0.01, 10.0)) = 1
    	[CustomObject(_BaseSetting)]                _DarkAlphaIntensity         ("【基础】【暗部】暗部透明程度", Range(-1.0, 1.0)) = 0
        [CustomObject(_BaseSetting)][HDR]           _BaseLightColor             ("【基础】【亮部】亮部颜色", Color) = (1,1,1,1)
    	[CustomObject(_BaseSetting)]                _LightRange                 ("【基础】【亮部】亮部过渡范围", Range(0.01, 10.0)) = 1
    	
    	[CustomObject(_BaseSetting)][HDR]           _SpecColor                  ("【高光】颜色", Color) = (1,1,1,1)
    	[CustomObject(_BaseSetting)]                _SpecRange					("【高光】影响范围", Range(0.01, 10.0)) = 1
    	
//    	[CustomObject(_BaseSetting)]                _SunScatterRange            ("【基础】亮部暗部过渡范围", Range(0.01, 2.0)) = 1
//    	[CustomObject(_BaseSetting)]                _AddAlphaIntensity          ("【基础】整体柔和度", Range(0.1, 1.0)) = 1
//    	[CustomObject(_BaseSetting)]                _SunScatterOffset            ("【基础】亮部暗部过渡线偏移", Range(-1.0, 1.0)) = 0
    	[CustomObject(_BaseSetting)]                _SDFColorIntensity          ("【SDF图】对基础明暗颜色的影响", Range(0.00, 1.0)) = 1
    	
        [CustomObject(_BaseSetting)][HDR]           _ScatterColor				("【太阳散射】基色", Color) = (1,1,1,1)
    	[CustomObject(_BaseSetting)]                _SunScatterDarkRange        ("【太阳透射】【暗部】暗部范围", Range(0.01,10)) = 1
    	[CustomObject(_BaseSetting)]                _SunScatterDarkIntensity    ("【太阳透射】【暗部】暗部透射强度", Range(0, 2)) = 0
        [CustomObject(_BaseSetting)]                _SunScatterLightRange       ("【太阳透射】【亮部】亮部范围", Range(0.01,10)) = 1
        [CustomObject(_BaseSetting)]                _SunScatterLightIntensity   ("【太阳透射】【亮部】亮部透射强度", Range(0, 2)) = 0
    	[CustomObject(_BaseSetting)]                _SunScatterSpecRange        ("【太阳透射】【高光】高光范围", Range(0.01,10)) = 1
    	[CustomObject(_BaseSetting)]                _SunScatterSpecIntensity    ("【太阳透射】【高光】高光透射强度", Range(0, 2)) = 0


    	
        [CustomGroup] _NoiseSetting("uv扰动", float) = 1.0
        [CustomTexture(_NoiseSetting)]              _NoiseTex                   ("扰动贴图", 2D) = "white"{}
        [CustomObject(_NoiseSetting)]               _CloudSpeed                 ("扰动整体速率", float) = 0
        [CustomObject(_NoiseSetting)]               _CloudSpeedU                ("扰动U方向速率", float) = 1
        [CustomObject(_NoiseSetting)]               _CloudSpeedV                ("扰动V方向速率", float) = 1
        [CustomObject(_NoiseSetting)]               _CloudDistStrength          ("扰动整体强度", float) = 1
        
        [CustomGroup] _DisovleSetting("消散相关", float) = 1.0
        [CustomObject(_DisovleSetting)]             _CloudGlobalDissolve        ("整体消散程度(越大消融越多)", Range(0,1)) = 0.5
        [CustomObject(_DisovleSetting)]             _CloudDissolveStrength      ("消散过渡边缘柔和程度(越小越生硬)", float) = 1
        [CustomObject(_DisovleSetting)]             _CloudDissolveRange         ("消散过程速度(越小越快)", Range(0.01,1)) = 1
        [CustomObject(_DisovleSetting)]             _CloudDissolveSpeed         ("整体消散速度", float) = 1
        [CustomObject(_DisovleSetting)]             _CloudDissolveOffset        ("消散起始偏移", float) = 1
        
        [CustomGroup] _CloudFogSetting("雾相关", float) = 1.0
        [CustomObject(_CloudFogSetting)]            _CloudFogBase               ("雾的基础高度",float) = 0
        [CustomObject(_CloudFogSetting)]            _CloudFogHeight             ("雾的最大高度",float) = 500
        [CustomObject(_CloudFogSetting)]            _CloudFogPower              ("雾的过渡程度",Range(0.1,2)) = 1
    }
    
    SubShader
    {
    	Tags {  "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Opaque"}
        Pass 
        {
			Tags { "LightMode"="UniversalForward" }
			
			Blend SrcAlpha OneMinusSrcAlpha
//			Blend OneMinusDstColor One
//			Blend SrcAlpha One
//			Blend One One
			
	        ZWrite Off
	        Cull Off
	         
	        HLSLPROGRAM
	        
			#pragma vertex vert
			#pragma fragment frag

			#pragma shader_feature _ _TOD
			#pragma multi_compile _ _SCANZONE_ON 
			#pragma multi_compile_fog
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			CBUFFER_START(UnityPerMaterial)
			float4 _CloudTexture_ST, _NoiseTex_ST;

			/*LIGHTING*/
			float4 _BaseLightColor, _BaseDarkColor, _SpecColor;

			/*SCATTER*/
			float _SunScatterDarkRange, _SunScatterLightRange, _SunScatterRange;
			float4 _ScatterColor;
	         
			/*DISTORTION*/
			float _CloudSpeed, _CloudSpeedU, _CloudSpeedV, _CloudDistStrength;
	         
			/*DISSOLVE*/
			float _CloudGlobalDissolve, _CloudDissolveRange, _CloudDissolveStrength, _CloudDissolveSpeed, _CloudDissolveOffset;

			/*FOG*/
			float _CloudFogBase, _CloudFogHeight, _CloudFogPower;

			// 暗部不透明程度
			float _DarkAlphaIntensity;
			float _DarkAlphaRange;
			float _SunScatterIntensity;
			float _SDFColorIntensity;
			float _SpecRange;
			// float _AddAlphaIntensity;
			// float _SunScatterOffset;
			float _SunScatterLightIntensity;
			float _SunScatterDarkIntensity;
			float _SunScatterSpecIntensity;
			float _SunScatterSpecRange;
			float _LightRange;
			CBUFFER_END

	         
			CBUFFER_START(CustomCloudContrl)
			/*TOD BUFFER*/
			// float4 _SunColor, _GlobalLightColor, _GlobalDarkColor, _GlobalSpecColor, _GlobalScatterColor;
			float _SunIntensity, _GlobalFog;
			CBUFFER_END

			TEXTURE2D(_CloudTexture);                  SAMPLER(sampler_CloudTexture);
			TEXTURE2D_X_FLOAT(_CameraDepthTexture);    SAMPLER(sampler_CameraDepthTexture);
			TEXTURE2D(_NoiseTex);                      SAMPLER(sampler_NoiseTex);

			TEXTURE2D(_CameraOpaqueTexture);	SAMPLER(sampler_CameraOpaqueTexture);

			struct Attributes
			{
				float4 positionOS     :      POSITION;
				float4 tangentOS      :      TANGENT;
				float3 normalOS       :      NORMAL;
				float2 uv             :      TEXCOORD0;
				float2 uv1            :      TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS                :      SV_POSITION;
				float3 positionWS                :      TEXCOORD0;
				float3 normalWS                  :      TEXCOORD1;
				float3 tangent                   :      TANGENT;
				float3 bitangent                 :      TEXCOORD2;
				float2 uv                        :      TEXCOORD3;
				float2 uv1                       :      TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;
	             
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
	             
				output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				output.uv = input.uv;
				VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS.xyz, input.tangentOS);
				output.normalWS = normalInput.normalWS;
				return output;
			}

	        half4 frag(Varyings input) : SV_Target
	        {
	            /*NOISE*/
	        	float2 cloudNoiseUV = TRANSFORM_TEX(input.uv, _NoiseTex);
	        	cloudNoiseUV.x += _Time * _CloudSpeed * _CloudSpeedU;
	        	cloudNoiseUV.y += _Time * _CloudSpeed * _CloudSpeedV;
	        	float4 cloudNoiseTex = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, cloudNoiseUV);
	        	float2 cloudDistOffset = (float2(cloudNoiseTex.x, cloudNoiseTex.y) - 0.5) * 2;
	        	
	        	/*DISTORTION*/
	        	float2 cloudUV = TRANSFORM_TEX(input.uv, _CloudTexture);
	        	half4 cloudMask = SAMPLE_TEXTURE2D(_CloudTexture, sampler_CloudTexture, cloudUV);
	        	cloudUV += lerp(cloudDistOffset * _CloudDistStrength, 0, cloudMask.r);
	        	cloudMask = SAMPLE_TEXTURE2D(_CloudTexture, sampler_CloudTexture, cloudUV);
	        	
	        	/*SCATTER*/
	        	float3 positionWS = input.positionWS;
	        	float3 viewDirWS = -GetWorldSpaceNormalizeViewDir(positionWS);
	        	Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
	        	
	        	/*SDF DISSOLVE*/
	        	half sdfOffset = smoothstep( -_CloudDissolveRange, _CloudDissolveRange, sin(_Time.y * _CloudDissolveSpeed + _CloudDissolveOffset));
	        	// 暂停消散
	        	// half sdfOffset = smoothstep( -_CloudDissolveRange, _CloudDissolveRange, sin(_CloudDissolveOffset));
	        	half finalSdf = sdfOffset * _CloudGlobalDissolve;
	        	half sdfMask = smoothstep( finalSdf - _CloudDissolveStrength, finalSdf, min(finalSdf,cloudMask.a) ) * cloudMask.b;

	        	half sdfSpecMask = smoothstep( finalSdf - _CloudDissolveStrength * 0.5, finalSdf + _CloudDissolveStrength * 0.5, cloudMask.a) * cloudMask.b;
	        	half oneMinusSdfMask = 1 - sdfMask;
	        	
	        	/*FOG*/
	        	//half heightFogMask = pow( smoothstep( _CloudFogHeight, _CloudFogBase, positionWS.y), _CloudFogPower) * _GlobalFog; TODO:TOD
	        	half heightFogMask = pow( smoothstep( _CloudFogHeight, _CloudFogBase, positionWS.y), _CloudFogPower);
	             
	        	/*Mask*/
	        	float LdotV = (dot(mainLight.direction, viewDirWS) + 1) * 0.5;
	        	return half4(pow(LdotV, 10).xxx, 1);
	        	half baseMask = pow(cloudMask.r, _SunScatterRange);
	        	half specMask = saturate(baseMask * cloudMask.g) * _MainLightShadowParams.x;
	        	half sunScatterDarkMask = saturate( pow( LdotV, _SunScatterDarkRange));
	        	half sunScatterLightMask = saturate( pow( LdotV, _SunScatterLightRange));
	        	sunScatterDarkMask = saturate(sunScatterDarkMask - (1 - baseMask));
	        	
	        	sunScatterLightMask = saturate(sunScatterLightMask - (1 -baseMask));
	        	half sunScatterSpecMask = sunScatterDarkMask * specMask;

	        	// 读取天空球颜色
	        	float4 tempPosCS = TransformWorldToHClip(input.positionWS);
	        	float4 screenPos = ComputeScreenPos(tempPosCS);
	        	float2 screenUV = screenPos.xy / screenPos.w;
				float4 skyTex = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV);
	        	
	        	/*LIGHTING*/
	        	half3 baseDarkCol = _BaseDarkColor * skyTex;
	        	half3 baseLightCol = _BaseLightColor;
	        	half3 basespecCol = _SpecColor * mainLight.color.rgb;

	        	// 如果开启TOD，则启用全局的变量乘以局部
	        	#ifdef _TOD
	        		// baseDarkCol *= _GlobalDarkColor.rgb;
	        		// baseLightCol *= _GlobalLightColor.rgb;
	        		// basespecCol *= _GlobalSpecColor.rgb;
	        	#endif

	        	// 基本色 - 底色，天空色
	        	half3 baseCol = skyTex.rgb;
	        	
	        	// 基本色 - 暗部 = lerp (天空颜色 ,暗部颜色)
	        	half baseDarkLerpMask = pow(cloudMask.r, _DarkAlphaRange);
	        	// 只有暗部的区域
	        	half3 DarkSpace = saturate(1.0f - baseDarkLerpMask);
	        	// 暗部区域与底色进行过渡
	        	baseCol = lerp(baseCol, baseDarkCol * DarkSpace, saturate(1.0f - baseDarkLerpMask + _DarkAlphaIntensity));

	        	// 基本色 - 亮部 = 亮部叠加
	        	half baseLightLerpMask = pow(cloudMask.r, _LightRange);
	        	// 只有亮部的区域
	        	half3 LightSpace = saturate(baseLightLerpMask);
	        	baseCol = lerp(baseCol, baseLightCol, LightSpace);

				// 高光
				half specLerpMask = cloudMask.g;
				specLerpMask = pow(specLerpMask, _SpecRange);
				baseCol = baseCol + specLerpMask * basespecCol;
	        	
	        	// 透明度
	        	half alpha = saturate(cloudMask.b);

				// 消散控制
	        	alpha = min(alpha, sdfMask);
	        	alpha = alpha * pow( 1 - sdfOffset, 0.5);
	        	
				half3 color = baseCol;

	        	// rongqingmei
	        	// return half4(color, alpha);
	        	
				// #ifdef _TOD
					// 太阳光的散射, 应该是云的alpha乘以靠近太阳的地方
					// 暗部的散射： Lerp
	        		half sunScatterDarkMask2 = saturate( pow( LdotV, _SunScatterDarkRange));
	        		half sunScatterDarkSpace = sunScatterDarkMask2 * alpha * DarkSpace;
	        		color = lerp(color, _ScatterColor, sunScatterDarkSpace * _SunScatterDarkIntensity);
			  
	        		// 亮部的散射： Lerp
	        		half sunScatterLightMask2 = saturate( pow( LdotV, _SunScatterLightRange));
	        		half sunScatterLightSpace = sunScatterLightMask2 * alpha * LightSpace;
	        		color = lerp(color,  _ScatterColor, sunScatterLightSpace * _SunScatterLightIntensity);
			  
	        		// 高光的散射： 叠加
	        		half sunScatterSpecMask2 = saturate( pow( LdotV, _SunScatterSpecRange));
	        		half sunScatterSpecSpace = sunScatterSpecMask2 * alpha * specLerpMask;
	        		color = color + sunScatterSpecSpace * _SunScatterSpecIntensity * _ScatterColor;
				// #endif

	        	
				// sdf测试
	        	// color = lerp(color, color * cloudMask.a, _SDFColorIntensity);
	        	alpha = lerp(alpha, alpha * cloudMask.a, _SDFColorIntensity);
		  
	        	// 高度雾
	   			color.rgb = lerp( color.rgb, unity_FogColor, heightFogMask);

	        	
	        	return half4(color, alpha);

	        	
	   	// half3 baseCol = lerp( baseDarkCol, baseLightCol, baseMask);
				// half3 specCol = lerp( baseCol, basespecCol, _SunIntensity / 6);
	   //      	half3 scatterCol = _GlobalScatterColor * _ScatterColor * ( sunScatterDarkMask * 0.5 + sunScatterLightMask * 0.3 + sunScatterSpecMask * 0.2) * _SunColor * _SunIntensity / 6;
	   //      	
	   //      	/*ALPHA*/
	   //      	float alpha = cloudMask.b;
	   //      	
				// // rongqingmei 测试消除多余颜色
	   //      	alpha = min(alpha, saturate((cloudMask.b  - 0.3f)));
		  //
	   //      	
				// // rongqingmei 测试暗部透明
	   //      	alpha = min(alpha, saturate(baseMask + _DarkAlphaIntensity));
	   //      	
		  //
				// // 关键代码在这里	        	
	   //      	alpha = min(alpha, sdfMask);
	   //      	
	   //      	
	   //      	alpha = alpha * pow( 1 - sdfOffset, 0.5);
	   //      	
	   //      	/*OUTPUT*/
	   //      	half4 col = 0;
	   //      	
	   //      	col.rgb = lerp( baseCol, specCol, specMask) + scatterCol;
				// col.rgb = lerp( baseCol, specCol, specMask);
	   //      	// col.rgb = lerp( col.rgb, scatterCol, saturate(baseMask + _SunScatterIntensity));
	   //      	
	   //      	col.rgb = lerp( col.rgb, unity_FogColor, heightFogMask);
	   //      	col.a = alpha;

	        	// return col;
	        }
			
			
	        ENDHLSL   
        }

	    Pass
	    {
	    	Name "TransDepthOnly"
            Tags{ "LightMode" = "TransDepthOnly"}
		    
            Cull Off
            ZWrite On
            Blend One Zero
            ColorMask 0
            HLSLPROGRAM
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/Effects_Addition.hlsl"

            #pragma multi_compile _ _USETEXTURE
            #pragma multi_compile_fragment _ _DISSLOVE_ON
            #pragma multi_compile_fragment _ _WEAKNESS_ON
            
            #pragma vertex DepthNormalsPassVertex
            #pragma fragment DepthNormalsPassFragment
            
            float usemask;

            CBUFFER_START(UnityPerMaterial)
			float4 _CloudTexture_ST, _NoiseTex_ST;
	         
			/*DISTORTION*/
			float _CloudSpeed, _CloudSpeedU, _CloudSpeedV, _CloudDistStrength;
	         
			/*DISSOLVE*/
			float _CloudGlobalDissolve, _CloudDissolveRange, _CloudDissolveStrength, _CloudDissolveSpeed, _CloudDissolveOffset;
			CBUFFER_END

			TEXTURE2D(_CloudTexture);                  SAMPLER(sampler_CloudTexture);
			TEXTURE2D(_NoiseTex);                      SAMPLER(sampler_NoiseTex);
            
            //bloom mask功能
            // 顶点输入结构
            struct DNAttributes
            {
	            float4 positionOS : POSITION;
	            float2 texcoord : TEXCOORD0;
            	float3 normal :NORMAL;
                #if _MASKMAP
		        half4 tangentOS : TANGENT;
                #endif
	            UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // 顶点着色器输出结构
            struct DNVaryings
            {
	            float2 uv : TEXCOORD0;
	            float4 positionCS : SV_POSITION;
	            float4 projPos : TEXCOORD1;
            	float3 normalWS :TEXCOORD2;
                #if _MASKMAP
		        float3 tangentWS  : TEXCOORD3;    // xyz: tangent, w: viewDir.y
		        float3 bitangentWS : TEXCOORD4;    // xyz: bitangent, w: viewDir.z
                #endif
                float3 posWS:TEXCOORD5;
	            UNITY_VERTEX_INPUT_INSTANCE_ID
	            UNITY_VERTEX_OUTPUT_STEREO
            };
            
            DNVaryings DepthNormalsPassVertex(DNAttributes input)
            {
	            DNVaryings output;
	            output.uv = input.texcoord;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
	            output.positionCS = vertexInput.positionCS;
                output.projPos = ComputeScreenPos (output.positionCS);
                output.normalWS = mul(unity_ObjectToWorld,input.normal);
            	output.posWS = TransformObjectToWorld(input.positionOS);
                #if _MASKMAP
                VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(input.normal, input.tangentOS);
	            output.tangentWS = vertexNormalInput.tangentWS;
	            output.bitangentWS = vertexNormalInput.bitangentWS;
                #endif
	            return output;
            }

	        void DepthNormalsPassFragment( DNVaryings input, out half4 outNormalWS : SV_Target0
        		#ifdef _WRITE_RENDERING_LAYERS
					, out float4 outRenderingLayers : SV_Target1
				#endif
	        )
	        {
            	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            	half3 color = 0;

            	 /*NOISE*/
	        	float2 cloudNoiseUV = TRANSFORM_TEX(input.uv, _NoiseTex);
	        	cloudNoiseUV.x += _Time * _CloudSpeed * _CloudSpeedU;
	        	cloudNoiseUV.y += _Time * _CloudSpeed * _CloudSpeedV;
	        	float4 cloudNoiseTex = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, cloudNoiseUV);
	        	float2 cloudDistOffset = (float2(cloudNoiseTex.x, cloudNoiseTex.y) - 0.5) * 2;

	        	/*DISTORTION*/
	        	float2 cloudUV = TRANSFORM_TEX(input.uv, _CloudTexture);
	        	half4 cloudMask = SAMPLE_TEXTURE2D(_CloudTexture, sampler_CloudTexture, cloudUV);
	        	cloudUV += lerp(cloudDistOffset * _CloudDistStrength, 0, cloudMask.r);
	        	

            	/*SDF DISSOLVE*/
	        	half sdfOffset = smoothstep( -_CloudDissolveRange, _CloudDissolveRange, sin(_Time.y * _CloudDissolveSpeed + _CloudDissolveOffset));
	        	half finalSdf = sdfOffset * _CloudGlobalDissolve;
	        	half sdfMask = smoothstep( finalSdf - _CloudDissolveStrength, finalSdf, min(finalSdf,cloudMask.a) ) * cloudMask.b;
	        	
	        	/*ALPHA*/
	        	float alpha = SAMPLE_TEXTURE2D(_CloudTexture, sampler_CloudTexture, cloudUV).b;
	        	alpha = min(alpha,sdfMask)* pow((1 - sdfOffset),0.5);
            	
				clip(alpha - 0.25);
            	
				//传送门裁切效果
				#if defined(_ALPHATEST_ON)
				TeleportClip(input.posWS, color);
				#endif
            	
	            half3 normal = input.normalWS;
            	
	            #if _MASKMAP
	                half4 maskMap = SampleTexture(input.uv,TEXTURE2D_ARGS(_MaskMap,sampler_MaskMap));
		            half4 tangentNormal = half4(MergeTexUnpackNormalRG(maskMap,_BumpStrength),1.0h);
	                normal = TransformTangentToWorld(normal, half3x3(input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz));
	            #endif
            	
	            outNormalWS = half4(normal,1);
            	
	            #ifdef _WRITE_RENDERING_LAYERS
	            uint renderingLayers = GetMeshRenderingLayer();
	            outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
				#endif
	        }
			ENDHLSL   
		}
    }
}
