Shader "X3/WarFogBlitShader"
{
    Properties
    {
        _NoiseTex ("NoiseTexture", 2D) = "white" {}
    	_MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry"}
        LOD 100
        
    	Pass
        {
            Name "WarFogASaveB-Pass0"
            
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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
			half _WarFogMaxLerpTime;
			half _WarFogNowLerpTime;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i ) : SV_Target
			{
				half4 tWarFog0 = tex2D(_MainTex, i.uv);

				// 把当前的状态，映射到 G 通道
			    // 时间满的时候，选 g 通道，时间不够的时候选 r 通道
			    half percentG = saturate(_WarFogNowLerpTime) / _WarFogMaxLerpTime;
			    half tFogMask = tWarFog0.g * percentG + tWarFog0.r * (1 - percentG);
				
				return half4(0, tFogMask, 0, 1);
			}
            ENDHLSL
        }
    	
	    Pass
        {
            Name "WarFogLight-Pass1"
			Blend One One
        	
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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half _WarFogLightSpeed;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            half4 frag(v2f i ) : SV_Target
			{
				half4 tWarFogCRT = tex2D(_MainTex, i.uv);
				// 有视野的地方，亮起来
				half lightValue = step(0.2f, tWarFogCRT.r) * unity_DeltaTime *_WarFogLightSpeed;
				return half4(lightValue, 0, 0, 1);
			}
            ENDHLSL
        }
    	
        Pass
        {
            Name "WarFogBlur-Pass2"
            
//        	Blend One One
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
            half4 _MainTex_ST;
            
            TEXTURE2D(_TempWarFogTex);
            SAMPLER(sampler_TempWarFogTex);
            half4 _TempWarFogTex_ST;

            
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            half4 _NoiseTex_ST;
            
            // sampler2D _MainTex;
            // float4 _MainTex_ST;
            float _WarFogBlurSampleX;
            float _WarFogBlurSampleY;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv.xy = v.uv;
            	// rongqingmei 这里可以尝试加时间  + half2(sin(_Time.y),sin(_Time.y))
            	o.uv.zw = TRANSFORM_TEX(v.uv, _NoiseTex);
                return o;
            }
            
            half4 frag(v2f i ) : SV_Target
			{
				// 卷积模糊
				float sampleW = _WarFogBlurSampleX;
				float sampleH = _WarFogBlurSampleY;
				
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
				
				// 卷积和不卷积的根据 noise 的程度来进行混合
				half4 tNoiseTex = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, i.uv.zw);
				
				// rongqingmei 测试
				tNoiseTex.r = 0;
				
				half tWarFogResult = lerp(tAvrWarFog0.r, tWarFog0, tNoiseTex.r);

				
				return half4(tWarFogResult, 0, 0, 1);
			}
            ENDHLSL
        }
    	
        Pass
        {
            Name "WarFogDarkAll-Pass3"
            
//        	Blend One One
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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            half4 _MainTex_ST;
			half _WarFogDarkSpeed;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            half4 frag(v2f i ) : SV_Target
			{
				// 变暗参数
				half darkValue = unity_DeltaTime * _WarFogDarkSpeed;

				// 贴图
				half4 tMainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
				
				// 每间隔时间变暗
				half4 tResTex = half4(tMainTex.r - darkValue, tMainTex.g - darkValue, tMainTex.b - darkValue, tMainTex.a);
				tResTex = saturate(tResTex);

				// 输出
				return tResTex;
			}
            ENDHLSL
        }
    	
    	Pass
        {
            Name "CPUStepLighting-Pass4"
            
//        	Blend One One
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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            	float2 offsetUv: TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            half4 _MainTex_ST;

            TEXTURE2D(_WarFogLightOrinalMap);
            SAMPLER(sampler_WarFogLightOrinalMap);
            half4 _WarFogLightOrinalMap_ST;

            
            float4 _WarFogLightOffset;
            half _WarFogLightSpeed;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = v.uv;
            	o.offsetUv = float2(v.uv.x + _WarFogLightOffset.x, v.uv.y + _WarFogLightOffset.y);
                return o;
            }
            
            half4 frag(v2f i ) : SV_Target
			{
				// 需要从CRT的uv，转成SubCRT的uv并读取是否需要点亮,黑色需要点亮
				// float2 tSubCrtUv = (i.uv - _WarFogCharaPosAndSale.xy + _WarFogCharaPosAndSale.zw / 2.0f) / _WarFogCharaPosAndSale.zw;
				// half4 warFogCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, tSubCrtUv);
				//
				// // uv 需要检测是否超过范围，超过范围输出 0 0 0 0
				// if(tSubCrtUv.x > 1.0f || tSubCrtUv.y > 1.0f || tSubCrtUv.x < 0.0f || tSubCrtUv.y < 0.0f)
				// {
				// 	return half4(0,0,0,0);
				// }
				
				half4 warFogLightOrinalMap = SAMPLE_TEXTURE2D(_WarFogLightOrinalMap, sampler_WarFogLightOrinalMap, i.offsetUv);
				half4 warFogCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
				
				// 有视野的地方，亮起来, warFogCol.r 是 0 黑色的话,就代表这个地方需要点亮
				half lightValue = step(warFogCol.r, 0.2f) * unity_DeltaTime *_WarFogLightSpeed;

				// 叠加光
				return half4(warFogLightOrinalMap.r + lightValue, 0, 0, 1);
			}
            ENDHLSL
        }
    	
    }
}
