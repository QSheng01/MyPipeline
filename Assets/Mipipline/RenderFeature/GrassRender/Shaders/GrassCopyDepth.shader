Shader "Hidden/Grass/CopyDepth"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        // Pass 0: 复制 _CameraDepthTexture → Hi-Z mip 0
        Pass
        {
            Name "CopyDepth"
            ZTest Always ZWrite Off Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                #if UNITY_UV_STARTS_AT_TOP
                output.uv.y = 1.0 - output.uv.y;
                #endif
                return output;
            }

            float Frag(Varyings input) : SV_Target
            {
                return SampleSceneDepth(input.uv);
            }
            ENDHLSL
        }

        // Pass 1: Hi-Z mip chain 降采样 (2x2 min/max)
        // 从 _HiZTexture 的 mip N-1 读取, 直接渲染到 Hi-Z RT 的 mip N
        // D3D11 像素着色器允许同一资源不同 mip 同时作为 SRV(读) 和 RTV(写)
        Pass
        {
            Name "HiZDownsample"
            ZTest Always ZWrite Off Cull Off

            HLSLPROGRAM
            #pragma vertex VertDown
            #pragma fragment FragDown

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            Texture2D<float> _HiZTexture;
            float _HiZSrcMip;

            struct Attributes { uint vertexID : SV_VertexID; };
            struct Varyings  { float4 positionCS : SV_POSITION; };

            Varyings VertDown(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                return output;
            }

            float FragDown(Varyings input) : SV_Target
            {
                int2 dstCoord = int2(input.positionCS.xy);
                int2 srcBase  = dstCoord * 2;
                int  srcMip   = (int)_HiZSrcMip;

                float d0 = _HiZTexture.Load(int3(srcBase,              srcMip));
                float d1 = _HiZTexture.Load(int3(srcBase + int2(1, 0), srcMip));
                float d2 = _HiZTexture.Load(int3(srcBase + int2(0, 1), srcMip));
                float d3 = _HiZTexture.Load(int3(srcBase + int2(1, 1), srcMip));

                #if UNITY_REVERSED_Z
                return min(min(d0, d1), min(d2, d3));
                #else
                return max(max(d0, d1), max(d2, d3));
                #endif
            }
            ENDHLSL
        }
    }
}
