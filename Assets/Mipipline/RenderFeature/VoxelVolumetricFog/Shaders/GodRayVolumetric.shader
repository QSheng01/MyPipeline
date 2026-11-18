Shader "ShiYue/URP/VoxelVolumetric/GodRayVolumetric"
{
 	SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

        ZWrite Off
        Cull Off
        ZTest Always
        
		//体积光 
		Pass
		{
			Name "GodRay Pass"
			HLSLPROGRAM
			#pragma vertex vertDir
			#pragma fragment fragDir
            #pragma multi_compile _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
			#define MAIN_LIGHT_CALCULATE_SHADOWS  
			#define _MAIN_LIGHT_SHADOWS_CASCADE
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
			
		    float _ShadowProjCoef;
		    float _DensityCoef;
		    float _MaxRayLength;
		    float _FogHeightCoef;
		    float _ExtinctionCoef;
		    float _SkyBoxExtinctionCoef;
			// x: scattering coef, y: extinction coef, z: range w: skybox extinction coef
		    float4 _VolumetricLight;
		    float4 _MieG;
		    float4 _FrameSize;
			half4 _GodRayColor;
		    int _MaxStep;
    
		    TEXTURE2D(_DitherTex);                   SAMPLER(sampler_DitherTex);
            TEXTURE2D(_QuarterResDepthBuffer);       SAMPLER(sampler_QuarterResDepthBuffer); 
		    float4 _DitherTex_TexelSize , _QuarterResDepthBuffer_TexelSize;
			
		    half GetShadowAttenuation(float3 wpos)
		    {
		    	float4 coords = TransformWorldToShadowCoord(wpos);
		    	half atten = MainLightRealtimeShadow(coords);
		    	return atten;
		    }
		    float GetDensity(float3 wpos)
		    {
		    	float density = 1.0f;
		    	density *= 0.2f;
		    	//density *= exp(min(-(wpos.y * _FogHeightCoef), 0));
		    	return density;
		    }
		    float4 MieScattering(float cosAngle, float4 g)
		    {
		    	return g.w * (g.x / (pow(g.y - g.z * cosAngle, 1.5)));
		    }
			
		    float4 RayMarch(float3 rayStart, float3 rayDir, float rayLength)
		    {
		    	int stepCount = _MaxStep; //步进次数
    
		    	float stepLen = rayLength/stepCount;
		    	float3 stepVec = rayDir * stepLen;

                Light mainLight = GetMainLight();
		    	float3 stepWpos = rayStart + stepVec * stepLen;
		    	float3 lightDir = -mainLight.direction.xyz;
		    	float extinction = 0;
		    	float4 resultLight = 0;
		    	float transmittance = 1;

		    	float d = 0;
		    	[loop]
		    	for (int i = 0; i < _MaxStep; ++i)
		    	{
		    		float shadowAttenuation = GetShadowAttenuation(stepWpos);//阴影扣减
		    		shadowAttenuation = step(0.2f, shadowAttenuation);
		    		if(d<0.2f)
		    		{
		    			stepWpos += stepVec;
		    			d += stepLen;
		    			continue;
		    		}
		    		
		    		float density = GetDensity(stepWpos);//雾浓度
		    		float scattering = density * stepLen * _DensityCoef;//散射
		    		extinction += stepLen * density * _ExtinctionCoef * 0.1f;
		    		transmittance *= exp(-extinction);
		    		half4 light = transmittance * (shadowAttenuation) * scattering;
		    		resultLight += light;
		    		stepWpos += stepVec;
		    	}
		    	float cosAngle = dot(lightDir, -rayDir);
		    	resultLight *= MieScattering(cosAngle, _MieG);
		    	resultLight.rgb *= mainLight.color;
		    	resultLight = max(0,resultLight);
		    	resultLight.a = exp(-extinction);
		    	return resultLight;
		    }

		    struct GodRayVaryings
		    {
		    	float4 positionHCS  : SV_POSITION;
		    	float2 uv           : TEXCOORD0;
		    	float3 viewRayWorld : TEXCOORD1;
		    };
			
			GodRayVaryings vertDir(Attributes IN)
			{
				GodRayVaryings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(IN);
                float4 pos = GetFullScreenTriangleVertexPosition(IN.vertexID);
                float2 uv  = GetFullScreenTriangleTexCoord(IN.vertexID);
				
				OUT.positionHCS = pos;
				OUT.uv.xy = uv;

		    	float sceneRawDepth = 1;
			    #if defined(UNITY_REVERSED_Z)
                    sceneRawDepth = 1 - sceneRawDepth;
                #endif
		    	float3 worldPos = ComputeWorldSpacePosition(OUT.uv,sceneRawDepth,UNITY_MATRIX_I_VP);
		    	OUT.viewRayWorld  = worldPos - _WorldSpaceCameraPos.xyz;
				return OUT;
			}

			half4 fragDir(GodRayVaryings IN) : SV_Target
			{
#if UNITY_REVERSED_Z
				float depth = SampleSceneDepth(IN.uv);
#else
				float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(IN.uv.xy));
#endif
				
                float linear01Depth = Linear01Depth(depth, _ZBufferParams);
                float3 positionWS = _WorldSpaceCameraPos.xyz + linear01Depth * IN.viewRayWorld;

                float offset = SAMPLE_TEXTURE2D(_DitherTex, sampler_DitherTex, IN.uv.xy * _DitherTex_TexelSize.xy * _FrameSize.xy ).a * 2 - 1;
				float near = _ProjectionParams.y;
				float3 ro = _WorldSpaceCameraPos.xyz;
				float3 rd = normalize(positionWS - ro);
				//ro += near * rd;
				half ditherLength = min(length(positionWS - ro),_MaxRayLength)/_MaxStep;
				ro += offset * rd * ditherLength * 1.5f;
				positionWS += offset * rd * ditherLength;
				float rayLength = min(length(positionWS - ro), _MaxRayLength);
				float4 color = RayMarch(ro, rd, rayLength) * _GodRayColor;
				
				if(linear01Depth>0.99f)
				{
					color.a *= _SkyBoxExtinctionCoef;
				}
				return color;
			}
			ENDHLSL
		}
		
		//合并结果
		Pass
		{
			Name "Combine Pass"
			
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "VoxelVolumetricCommon.hlsl"
            SAMPLER(sampler_BlitTexture);
            TEXTURE2D(_FinalTex);    SAMPLER(sampler_FinalTex);

            float4 Fragment (Varyings input) : SV_Target
            {
                float4 screenColor = _BlitTexture.SampleLevel(voxel_point_clamp_sampler, input.texcoord, 0);
                half4 raymarchColor = SAMPLE_TEXTURE2D(_FinalTex, sampler_FinalTex, input.texcoord);
            	half3 finalCol = screenColor.rgb + raymarchColor.rgb * raymarchColor.a;
				return float4(finalCol,1);
            }
            ENDHLSL
		}
		
        // 双边滤波
		Pass
        {
	        Name "BilateralBlur"
			Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            SAMPLER(sampler_BlitTexture);
            float4 _BlitTexture_TexelSize;
            half _KernelSize, _Space_Sigma, _Range_Sigma;
            
            struct appdata
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv[4] : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            }; 
            
            half4 BilateralBlur(float2 uv, float space_sigma, float range_sigma){
                float weight_sum = 0;
                float3 color_sum = 0;
            	float4 color_originfull = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv);
                float3 color_origin = color_originfull.rgb;   //  获取原始颜色
            	float centerDepth = (LinearEyeDepth(SampleSceneDepth(uv.xy),_ZBufferParams));
                float3 color = 0;

                // (i,j,k,l)
                for(int i = -_KernelSize; i < _KernelSize; i++){
                    for(int j = -_KernelSize; j < _KernelSize; j++){
                        //空域高斯
                        float2 varible = uv + float2(i * _BlitTexture_TexelSize.x, j * _BlitTexture_TexelSize.y);
                        float space_factor = i * i + j * j;
                        space_factor = (-space_factor) / (2 * space_sigma * space_sigma);
                        float space_weight = 1/(space_sigma * space_sigma * 2 * PI) * exp(space_factor);     // 空域权重space_weight

                        //值域高斯
                        float3 color_neighbor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, varible).rgb;
                        float3 color_distance = (color_neighbor - color_origin);
                    	float depth_neighbor = LinearEyeDepth(SampleSceneDepth(varible),_ZBufferParams);
                    	float depth_distance = abs(depth_neighbor-depth_distance) * 0.5;

                    	
                        float value_factor = color_distance.r * color_distance.r ;
                        value_factor = (-value_factor) / (2 * range_sigma * range_sigma);
                        float value_weight = (1 / (2 * PI * range_sigma)) * exp(value_factor);              // 值域权重value_weight

                        weight_sum += space_weight * value_weight;                                          // 像素点的空域权重的 总和
                        color_sum += color_neighbor * space_weight * value_weight;                          // 计算所有像素点的加权颜色总和
                    }
                }

                if(weight_sum > 0){                                                                         // 判断   加权颜色总和/权重总和 = 双边模糊后的颜色值
                    color = color_sum / weight_sum;
                }

                return half4(color,color_originfull.a);
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                VertexPositionInputs  PositionInputs = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = PositionInputs.positionCS;

            	float3 dir = float3(1,-1,0);
            	half2 delta = _BlitTexture_TexelSize.xy;
            	o.uv[0] = float4(v.texcoord, v.texcoord+dir.x*delta);
            	o.uv[1] = float4(v.texcoord + dir.yy * delta, v.texcoord + dir.xx * 2 * delta);
                o.uv[2] = float4(v.texcoord + dir.yy * 2 * delta , v.texcoord + dir.xx * 3 * delta); 
                o.uv[3] = float4(v.texcoord + dir.yy *3 * delta ,0,0); 
            	
                return o;
            }
            
            half4 frag (Varyings i) : SV_Target
            {
                half4 color = 0;
                color = BilateralBlur(i.texcoord, _Space_Sigma, _Range_Sigma);
                return color;
            }

            ENDHLSL
        } 
		
		// 降采样
		Pass
		{
			Name "Bilateral Downsample"
			HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            float4 Fragment (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 final = SampleSceneDepth(input.texcoord);
				return final;
            }
			
			ENDHLSL
		}
		
		// 升采样
		Pass
		{
			Name "Bilateral Upsample"
            HLSLPROGRAM
            #pragma vertex VertUpsample
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            SAMPLER(sampler_BlitTexture);
            TEXTURE2D(_QuarterResColor);             SAMPLER(sampler_QuarterResColor); 
            TEXTURE2D(_QuarterResDepthBuffer);       SAMPLER(sampler_QuarterResDepthBuffer); 
            float4 _BlitTexture_TexelSize , _QuarterResDepthBuffer_TexelSize;

		    struct VaringsUpsample
		    {
		    	float2 uv : TEXCOORD0;
		    	float2 uv00 : TEXCOORD1;
		    	float2 uv01 : TEXCOORD2;
		    	float2 uv10 : TEXCOORD3;
		    	float2 uv11 : TEXCOORD4;
		    	float4 positionCS : SV_POSITION;
		    };

            float4 BilateralUpsample(VaringsUpsample input, Texture2D loDepth, Texture2D loColor, SamplerState linearSampler, SamplerState pointSampler)
		    {
                const float threshold = 1.5f;
                float4 highResDepth = LinearEyeDepth(SampleSceneDepth(input.uv),_ZBufferParams).xxxx;
		    	float4 lowResDepth;
            	
                lowResDepth.x = LinearEyeDepth(SAMPLE_TEXTURE2D_X(loDepth, pointSampler, UnityStereoTransformScreenSpaceTex(input.uv00)),_ZBufferParams); 
                lowResDepth.y = LinearEyeDepth(SAMPLE_TEXTURE2D_X(loDepth, pointSampler, UnityStereoTransformScreenSpaceTex(input.uv10)),_ZBufferParams);
                lowResDepth.z = LinearEyeDepth(SAMPLE_TEXTURE2D_X(loDepth, pointSampler, UnityStereoTransformScreenSpaceTex(input.uv01)),_ZBufferParams);
                lowResDepth.w = LinearEyeDepth(SAMPLE_TEXTURE2D_X(loDepth, pointSampler, UnityStereoTransformScreenSpaceTex(input.uv11)),_ZBufferParams);
    
		    	float4 depthDiff = abs(lowResDepth - highResDepth);
    
		    	float accumDiff = dot(depthDiff, float4(1, 1, 1, 1));
    
		    	[branch]
		    	if (accumDiff < threshold) // small error, not an edge -> use bilinear filter
		    	{
		    		return loColor.Sample(linearSampler, input.uv);
		    	}
                
		    	// find nearest sample
		    	float minDepthDiff = depthDiff[0];
		    	float2 nearestUv = input.uv00;
    
		    	if (depthDiff[1] < minDepthDiff)
		    	{
		    		nearestUv = input.uv10;
		    		minDepthDiff = depthDiff[1];
		    	}
    
		    	if (depthDiff[2] < minDepthDiff)
		    	{
		    		nearestUv = input.uv01;
		    		minDepthDiff = depthDiff[2];
		    	}
    
		    	if (depthDiff[3] < minDepthDiff)
		    	{
		    		nearestUv = input.uv11;
		    		minDepthDiff = depthDiff[3];
		    	}
            	half4 final = SAMPLE_TEXTURE2D(_BlitTexture,sampler_BlitTexture,nearestUv);
                return final;
		    }

            VaringsUpsample VertUpsample(Attributes input)
            {
                VaringsUpsample output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
                float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);

            	output.positionCS = pos;
            	output.uv = uv;
                output.uv00 = output.uv + _QuarterResDepthBuffer_TexelSize.xy * float2(0,1);
                output.uv10 = output.uv00 + _QuarterResDepthBuffer_TexelSize.xy * float2(0,-1);
                output.uv01 = output.uv00 + _QuarterResDepthBuffer_TexelSize.xy * float2(1,0);
                output.uv11 = output.uv00 + _QuarterResDepthBuffer_TexelSize.xy * float2(0,1);
                return output;
            }
            
            half4 frag (VaringsUpsample i) : SV_Target
            {
                half4 color = 0;
                color = BilateralUpsample(i,  _QuarterResDepthBuffer, _BlitTexture, sampler_BlitTexture, sampler_QuarterResDepthBuffer);
            	
                return color;
            }
            
            ENDHLSL
		}
	}
}
