Shader "Hidden/ShiYue/ScenePerObjectShadowResolve"
{
	Properties
	{
//		[Toggle] _SoftShadow("Soft Shadow", Float) = 1.0
//		_POShadowBias("Shadow Bias", Float) = 0.02
//		shadowStrength("Shadow Strength",Float) = 1
	}

	SubShader
	{
		Tags {"IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline"}
		LOD 100

		Blend SrcAlpha OneMinusSrcAlpha
		
		ZWrite Off
		Cull off

//		Stencil
//		{
//			Ref 0
//			Comp Equal
//			Pass IncrSat
//		}

		Pass
		{
			Name "Resolve"

			HLSLPROGRAM

			#pragma vertex ResolvePassVertex
			#pragma fragment ResolvePassFragment
			//#pragma shader_feature_local_fragment _SOFTSHADOW_ON
			#pragma multi_compile_instancing
			#pragma multi_compile_local_fragment _ _SOFTSHADOW_ON
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			struct Attributes
			{
				float3 positionOS : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionHCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float4 screenPos : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			UNITY_INSTANCING_BUFFER_START(Props)
				UNITY_DEFINE_INSTANCED_PROP(float4x4, _WorldToUVMatrix)
				UNITY_DEFINE_INSTANCED_PROP(float4, _SliceUVOffsetExtend) // todo: use it!
			UNITY_INSTANCING_BUFFER_END(Props)

			// todo: find a way to support both srp batcher and gpu instancing
            CBUFFER_START(UnityPerMaterial)
			float _POShadowBias;
			float shadowStrength;
			CBUFFER_END

			CBUFFER_START(PerObjectShadowBuffer)
			float4 _PerObjectShadowAtlasTexelSize; // xy: texel size, zw: not used.
			CBUFFER_END
			
			TEXTURE2D_SHADOW(_PerObjectShadowMapAtlas);
			SAMPLER_CMP(sampler_PerObjectShadowMapAtlas);

			TEXTURE2D_X_FLOAT(_CameraDepthTexture);
			SAMPLER(sampler_CameraDepthTexture);

			float SampleShadow(float3 shadowCoords) {

				// uv offset (without texel size) for 4-tap pcf filtering
				const float3 shadowOffset0 = float3(-0.5, -0.5, 0);
				const float3 shadowOffset1 = float3(0.5, -0.5, 0);
				const float3 shadowOffset2 = float3(-0.5, 0.5, 0);
				const float3 shadowOffset3 = float3(0.5, 0.5, 0);

				float shadow = 0;
#if _SOFTSHADOW_ON
				// 4-tap hardware comparison
				float4 atten4;
				atten4.x = SAMPLE_TEXTURE2D_SHADOW(_PerObjectShadowMapAtlas, sampler_PerObjectShadowMapAtlas, shadowCoords + shadowOffset0 * _PerObjectShadowAtlasTexelSize.xyz);
				atten4.y = SAMPLE_TEXTURE2D_SHADOW(_PerObjectShadowMapAtlas, sampler_PerObjectShadowMapAtlas, shadowCoords + shadowOffset1 * _PerObjectShadowAtlasTexelSize.xyz);
				atten4.z = SAMPLE_TEXTURE2D_SHADOW(_PerObjectShadowMapAtlas, sampler_PerObjectShadowMapAtlas, shadowCoords + shadowOffset2 * _PerObjectShadowAtlasTexelSize.xyz);
				atten4.w = SAMPLE_TEXTURE2D_SHADOW(_PerObjectShadowMapAtlas, sampler_PerObjectShadowMapAtlas, shadowCoords + shadowOffset3 * _PerObjectShadowAtlasTexelSize.xyz);
				shadow = dot(atten4, 0.25);
#else
				// 1-tap hardware comparison
				shadow =  SAMPLE_TEXTURE2D_SHADOW(_PerObjectShadowMapAtlas, sampler_PerObjectShadowMapAtlas, shadowCoords);
#endif
				shadow = 1 - shadow; // 此处为了让shadow通过透明度混合，需要反转！
				return shadow;
			}

			Varyings ResolvePassVertex (Attributes input) 
			{
				Varyings output = (Varyings)0; // todo: should we use ZERO_INITIALIZE?

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);

				float3 positionWS = TransformObjectToWorld(input.positionOS);
				output.positionHCS = TransformWorldToHClip(positionWS);
				output.positionWS = positionWS;
				output.screenPos = ComputeScreenPos(output.positionHCS);

				return output;
			}

			half4 ResolvePassFragment(Varyings input) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(input);
				
				float2 uv = input.screenPos.xy / max(input.screenPos.w, 1e-5);

				float z = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
				
#if !UNITY_REVERSED_Z
				z = lerp(UNITY_NEAR_CLIP_VALUE, 1, z);
				_POShadowBias *= -1;
#endif

				float3 sceneWorldPos = ComputeWorldSpacePosition(uv, z, UNITY_MATRIX_I_VP);
				float3 shadowCoords = mul(UNITY_ACCESS_INSTANCED_PROP(Props, _WorldToUVMatrix), float4(sceneWorldPos, 1.0)).xyz;
				float4 wrap = UNITY_ACCESS_INSTANCED_PROP(Props, _SliceUVOffsetExtend);

#if UNITY_UV_STARTS_AT_TOP
				shadowCoords.y = 1 - shadowCoords.y;
#endif
				
				shadowCoords.x = shadowCoords.x * wrap.z + wrap.x;
				shadowCoords.y = shadowCoords.y * wrap.w + wrap.y;

				if (shadowCoords.x < wrap.x || shadowCoords.x > wrap.x + wrap.z || shadowCoords.y > wrap.y + wrap.w || shadowCoords.y < wrap.y) {
					return half4(1, 1, 1, 0);
				}
				
				shadowCoords.z = max(shadowCoords.z, 1e-5) + _POShadowBias;

				float shadow = SampleShadow(shadowCoords);

				return half4(1-shadowStrength,0,0,shadow);
			}
			ENDHLSL
		}
		
		Pass
        {
            Name "PerObjectShadow-Pass2"
            
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            	half3 positionWS: TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            half4 _MainTex_TexelSize;    //用于计算相邻像素的纹理坐标的偏移量
            half4 _MainTex_ST;
            
            TEXTURE2D(_TempWarFogTex);
            SAMPLER(sampler_TempWarFogTex);
            half4 _TempWarFogTex_ST;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv.xy = v.uv;
                return o;
            }
            
            half4 frag(v2f i) : SV_Target
			{
				// 卷积模糊
				float scale = 1.0f;
				float sampleW = _MainTex_TexelSize.x * scale;
				float sampleH = _MainTex_TexelSize.y * scale;
				
				half4 tWarFog0 = SAMPLE_TEXTURE2D(_TempWarFogTex, sampler_TempWarFogTex, i.uv.xy);
			    half4 tWarFog1 = SAMPLE_TEXTURE2D(_TempWarFogTex, sampler_TempWarFogTex, i.uv.xy + half2(sampleW, sampleH));
			    half4 tWarFog2 = SAMPLE_TEXTURE2D(_TempWarFogTex, sampler_TempWarFogTex, i.uv.xy + half2(sampleW, 0));
			    half4 tWarFog3 = SAMPLE_TEXTURE2D(_TempWarFogTex, sampler_TempWarFogTex, i.uv.xy + half2(sampleW, - sampleH));
			    half4 tWarFog4 = SAMPLE_TEXTURE2D(_TempWarFogTex, sampler_TempWarFogTex, i.uv.xy + half2(0, - sampleH));
				half4 tWarFog5 = SAMPLE_TEXTURE2D(_TempWarFogTex, sampler_TempWarFogTex, i.uv.xy + half2(- sampleW, - sampleH));
			    half4 tWarFog6 = SAMPLE_TEXTURE2D(_TempWarFogTex, sampler_TempWarFogTex, i.uv.xy + half2(- sampleW, 0));
			    half4 tWarFog7 = SAMPLE_TEXTURE2D(_TempWarFogTex, sampler_TempWarFogTex, i.uv.xy + half2(- sampleW, sampleH));
			    half4 tWarFog8 = SAMPLE_TEXTURE2D(_TempWarFogTex, sampler_TempWarFogTex, i.uv.xy + half2(0, sampleH));
				half4 tAvrWarFog0 = tWarFog0 + tWarFog1 + tWarFog2 + tWarFog3 + tWarFog4 + tWarFog5 + tWarFog6 + tWarFog7 + tWarFog8;
				tAvrWarFog0 /= 9.0h;

				// return half4(tWarFog0.rrr,1);
				return half4(tAvrWarFog0.rrr,1);
			}
            ENDHLSL
        }
	}
}