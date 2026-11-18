Shader "Hidden/ShiYue/URP/PostProcess/OilPainting"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    SAMPLER(sampler_BlitTexture);
    SAMPLER(sampler_BlitTexture_linear_clamp);

    float _FarStart;
    float _FarEnd;
    float _Step;
    float _Radius;
    float _IntensityBinCount;
    float2 _TexelSize;

    #define MAX_INTENSITY_BIN_COUNT 20 + 1
    static int intensityCount[MAX_INTENSITY_BIN_COUNT];
    static float3 average[MAX_INTENSITY_BIN_COUNT];

    // reinhard tone mapping
    float3 Tonemapping(float3 hdrColor)
    {
        return hdrColor / (hdrColor + (1.0));
    }

    float3 InverseTonemapping(float3 color)
    {
        return color / (1 - color);
    }

    void Tap(float2 uv, inout int maxIndex, inout int maxCount)
    {
        float3 c = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_BlitTexture_linear_clamp, uv, 0).rgb;
        c = Tonemapping(c);
        int curIntensity = (int)((c.r+c.g+c.b)/3*_IntensityBinCount);
        intensityCount[curIntensity]++;
        average[curIntensity] = c + average[curIntensity];

        if (intensityCount[curIntensity] > maxCount)
        {
            maxCount = intensityCount[curIntensity];
            maxIndex = curIntensity;
        }
    }

    float CalcRadius(float2 uv)
    {
         float depth = SampleSceneDepth(uv).x;
         depth = LinearEyeDepth(depth, _ZBufferParams);
         half coc = saturate((depth - _FarStart) / (_FarEnd - _FarStart));
         return _Radius * coc;
    }

    float4 GetFinalOutput(int maxIndex, int maxCount)
    {
        float3 final = average[maxIndex] / max(maxCount, 1);
        return float4(InverseTonemapping(final), 1);
    }

    float4 FragOilPainting(Varyings input) : SV_Target
    {
        float2 uv = input.texcoord;
        float radius = CalcRadius(uv);

        int maxIndex = 0;
        int maxCount = intensityCount[0];

        UNITY_LOOP
        for(float i = -radius; i <= radius; i += _Step)
        {
            UNITY_LOOP
            for(float j = -radius; j <= radius; j += _Step)
            {
                float2 uv2 = uv + _TexelSize * half2(i, j);
                Tap(uv2, maxIndex, maxCount);
           }
        }

        return GetFinalOutput(maxIndex, maxCount);
    }

    float4 OilPainting(Varyings input, float2 dir)
    {
        float2 uv = input.texcoord;
        float radius = CalcRadius(uv);

        int maxIndex = 0;
        int maxCount = intensityCount[0];

        UNITY_LOOP
        for(float i = -radius; i <= radius; i += _Step)
        {
            float2 uv2 = uv + _TexelSize * i * dir;
            Tap(uv2, maxIndex, maxCount);
        }

        return GetFinalOutput(maxIndex, maxCount);
    }

    float4 FragOilPaintingH(Varyings input) : SV_Target
    {
        return OilPainting(input, float2(1, 0));
    }

    float4 FragOilPaintingV(Varyings input) : SV_Target
    {
        return OilPainting(input, float2(0, 1));
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "OilPainting"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragOilPainting
            //#pragma enable_d3d11_debug_symbols
            ENDHLSL
        }

        Pass
        {
            Name "OilPaintingHorizontal"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragOilPaintingH
            //#pragma enable_d3d11_debug_symbols
            ENDHLSL
        }
        Pass
        {
            Name "OilPaintingVertical"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragOilPaintingV
            //#pragma enable_d3d11_debug_symbols
            ENDHLSL
        }
    }
    FallBack "Hidden/Shader Graph/FallbackError"
}