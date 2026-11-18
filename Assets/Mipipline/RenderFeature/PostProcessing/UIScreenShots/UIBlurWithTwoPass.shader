Shader "Hidden/ShiYue/URP/PostProcess/UIBlurWithTwoPass"
{
    Properties
    {
        [PerRendererData]_MainTex ("Albedo (RGB)", 2D) = "white" {}
        _BlurRadius("模糊半径", Range(0,3.0)) = 1
    }
    SubShader
    {
        ZTest Always Cull Off ZWrite Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"


        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_TexelSize;
        float _BlurRadius;
        CBUFFER_END

        struct a2v
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
        };
        struct v2fBase
        {
            float4 positionCS :SV_POSITION;
            float2 uv : TEXCOORD0;
        };
        struct v2f
        {
            float4 positionCS :SV_POSITION;
            float2 uv[7] : TEXCOORD0;
        };

         v2fBase vertBase(a2v i)
        {
            v2fBase o;

            o.positionCS = TransformObjectToHClip(i.positionOS.xyz);

            float2 uv = i.uv;
            o.uv = uv;
            return o;
        }
        v2f vertBlurVertical(a2v i)
        {
            v2f o;

            o.positionCS = TransformObjectToHClip(i.positionOS.xyz);

            float2 uv = i.uv;

            o.uv[0] = uv;
            o.uv[1] = uv + float2(0.0, _MainTex_TexelSize.y * 1.0) * _BlurRadius;
            o.uv[2] = uv - float2(0.0, _MainTex_TexelSize.y * 1.0) * _BlurRadius;
            o.uv[3] = uv + float2(0.0, _MainTex_TexelSize.y * 2.0) * _BlurRadius;
            o.uv[4] = uv - float2(0.0, _MainTex_TexelSize.y * 2.0) * _BlurRadius;
            o.uv[5] = uv + float2(0.0, _MainTex_TexelSize.y * 3.0) * _BlurRadius;
            o.uv[6] = uv - float2(0.0, _MainTex_TexelSize.y * 3.0) * _BlurRadius;
            return o;
        }

        v2f vertBlurHorizontal(a2v i)
        {
            v2f o;

            o.positionCS = TransformObjectToHClip(i.positionOS.xyz);

            float2 uv = i.uv;

            o.uv[0] = uv;
            o.uv[1] = uv + float2(_MainTex_TexelSize.x * 1.0, 0.0) * _BlurRadius;
            o.uv[2] = uv - float2(_MainTex_TexelSize.x * 1.0, 0.0) * _BlurRadius;
            o.uv[3] = uv + float2(_MainTex_TexelSize.x * 2.0, 0.0) * _BlurRadius;
            o.uv[4] = uv - float2(_MainTex_TexelSize.x * 2.0, 0.0) * _BlurRadius;
            o.uv[5] = uv + float2(_MainTex_TexelSize.x * 3.0, 0.0) * _BlurRadius;
            o.uv[6] = uv - float2(_MainTex_TexelSize.x * 3.0, 0.0) * _BlurRadius;

            return o;
        }

        half4 fragLinearToSRGB(v2fBase i) : SV_TARGET
        {
            half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
            color.rgb = LinearToSRGB(color.rgb);
            return color;
        }


        half4 fragBlur(v2f i) : SV_TARGET
        {
            half3 sum = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[0]).rgb * 0.4;
            sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[1]).rgb * 0.15;
            sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[2]).rgb * 0.15;
            sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[3]).rgb * 0.1;
            sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[4]).rgb * 0.1;
            sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[5]).rgb * 0.05;
            sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[6]).rgb * 0.05;
            return half4(sum, 1);
        }
        ENDHLSL

        Pass
        {
            Name "Vertical"
            HLSLPROGRAM
           
            #pragma vertex vertBlurVertical
            #pragma fragment fragBlur
            ENDHLSL

        }

        Pass
        {
            Name "Horizontal"
            HLSLPROGRAM
            #pragma vertex vertBlurHorizontal
            #pragma fragment fragBlur
            ENDHLSL
        }
        
        Pass
        {
            Name "LinearToSRGB"
            HLSLPROGRAM
            #pragma vertex vertBase
            #pragma fragment fragLinearToSRGB
            ENDHLSL
        }
        
    }
}