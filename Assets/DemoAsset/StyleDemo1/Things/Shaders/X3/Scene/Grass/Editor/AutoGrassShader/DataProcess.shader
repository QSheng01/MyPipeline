Shader "ScatterGrass/DataProcess"
{
    Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_BlurSize ("Blur Size", Float) = 1.0
	}
	SubShader {
		CGINCLUDE
		
		#include "UnityCG.cginc"
		
		sampler2D _MainTex;  
		half4 _MainTex_TexelSize;    //用于计算相邻像素的纹理坐标的偏移量
		float _BlurSize;
		  
		struct v2f {
			float4 pos : SV_POSITION;
			half2 uv[5]: TEXCOORD0;
		};
		
		struct v2f_original {
			float4 pos : SV_POSITION;
			half2 uv: TEXCOORD0;
		};
		  
		v2f vertBlurVertical(appdata_img v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			
			half2 uv = v.texcoord;
			
			o.uv[0] = uv;
			o.uv[1] = uv + float2(0.0, _MainTex_TexelSize.y * 1.0) * _BlurSize;
			o.uv[2] = uv - float2(0.0, _MainTex_TexelSize.y * 1.0) * _BlurSize;
			o.uv[3] = uv + float2(0.0, _MainTex_TexelSize.y * 2.0) * _BlurSize;
			o.uv[4] = uv - float2(0.0, _MainTex_TexelSize.y * 2.0) * _BlurSize;
					 
			return o;
		}
		
		v2f vertBlurHorizontal(appdata_img v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			
			half2 uv = v.texcoord;
			
			o.uv[0] = uv;
			o.uv[1] = uv + float2(_MainTex_TexelSize.x * 1.0, 0.0) * _BlurSize;
			o.uv[2] = uv - float2(_MainTex_TexelSize.x * 1.0, 0.0) * _BlurSize;
			o.uv[3] = uv + float2(_MainTex_TexelSize.x * 2.0, 0.0) * _BlurSize;
			o.uv[4] = uv - float2(_MainTex_TexelSize.x * 2.0, 0.0) * _BlurSize;
					 
			return o;
		}
		
		float4 fragBlur(v2f i) : SV_Target {
			float weight[3] = {0.4026, 0.2442, 0.0545};

			#if ONLYBLURALPHA
			float4 original = tex2D(_MainTex, i.uv[0]);
			float sum = original.r * weight[0];
			
			for (int it = 1; it < 3; it++) {
				sum += tex2D(_MainTex, i.uv[it*2-1]).r * weight[it];
				sum += tex2D(_MainTex, i.uv[it*2]).r * weight[it];
			}
			
			return float4(sum, original.yzw);
			#else
			float3 sum = tex2D(_MainTex, i.uv[0]).rgb * weight[0];
			
			for (int it = 1; it < 3; it++) {
				sum += tex2D(_MainTex, i.uv[it*2-1]).rgb * weight[it];
				sum += tex2D(_MainTex, i.uv[it*2]).rgb * weight[it];
			}
			
			return float4(sum, 1.0);
			#endif
		}

		v2f_original vert(appdata_img v) {
			v2f_original o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = v.texcoord;
					 
			return o;
		}
		
		float4 fragDiffHeight(v2f_original i) : SV_Target {
			float4 original = tex2D(_MainTex, i.uv).xyzw;
			float left =   tex2D(_MainTex, i.uv + float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * float2(-1, 0)).y;
			float right =  tex2D(_MainTex, i.uv + float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * float2(+1, 0)).y;
			float top =    tex2D(_MainTex, i.uv + float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * float2(0, +1)).y;
			float bottom = tex2D(_MainTex, i.uv + float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * float2(0, -1)).y;

			float diff = abs(original.y - left) + abs(original.y - right) + abs(original.y - top) + abs(original.y - bottom);
			// if((diff * 0.25f) > 0.1f)
			// {
			// 	return float4(1, original.y, 0, original.a);
			// }
				return float4(saturate(original.a + step(0.05f, diff * 0.25f)), original.y, 0, original.a);
		}
		
		ENDCG
		
		ZTest Always Cull Off ZWrite Off
		
		Pass {
 
			NAME "GAUSSIAN_BLUR_VERTICAL"
			
			CGPROGRAM
			  
			#pragma vertex vertBlurVertical  
			#pragma fragment fragBlur
			#pragma shader_feature ONLYBLURALPHA
			  
			ENDCG  
		}
		
		Pass {  
			NAME "GAUSSIAN_BLUR_HORIZONTAL"
			
			CGPROGRAM  
			
			#pragma vertex vertBlurHorizontal  
			#pragma fragment fragBlur
			#pragma shader_feature ONLYBLURALPHA
			
			ENDCG
		}
		
		Pass {  
			NAME "SAMPLER_DIFFHEIGHT"
			
			CGPROGRAM  
			
			#pragma vertex vert
			#pragma fragment fragDiffHeight
			
			ENDCG
		}
	} 
}
