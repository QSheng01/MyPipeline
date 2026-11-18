Shader "Hidden/ShiYue/URP/PostProcess/KawaseBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
    }
    
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    struct appdata
    {
        float4 vertex: POSITION;
        float2 uv: TEXCOORD0;
    };

    struct VaryingsDefault
    {
        float4 pos: SV_POSITION;
        float2 uv: TEXCOORD0;
    };

    VaryingsDefault VertDefault(appdata v)
    {
        VaryingsDefault o;
        o.pos = TransformObjectToHClip(v.vertex.xyz);
        o.uv = v.uv;
        return o;
    }

    TEXTURE2D (_MainTex);
    SAMPLER(sampler_MainTex);
    float4 _MainTex_TexelSize;
    half _BlurOffset;

    half4 KawaseBlur(float2 uv, float2 texelSize, half pixelOffset)
    {
        half4 o = 0;
        o += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(1.0h, 1.0h) * texelSize * pixelOffset);
        o += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-1.0h, +1.0h) * texelSize * pixelOffset);
        o += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-1.0h, -1.0h) * texelSize * pixelOffset);
        o += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(+1.0h, -1.0h) * texelSize * pixelOffset);
        return o * 0.25h;
    }

    half4 Frag(VaryingsDefault i): SV_Target
    {
        half4 tColor = KawaseBlur(i.uv, _MainTex_TexelSize.xy, _BlurOffset);
        return tColor;
    }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment Frag
            ENDHLSL
        }
    }
}