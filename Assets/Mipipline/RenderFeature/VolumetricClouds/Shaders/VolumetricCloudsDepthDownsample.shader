// Fragment pass that samples URP's global _CameraDepthTexture (compute shaders cannot read
// globally-bound textures) and writes a conservative min-depth into a half-resolution RT.
Shader "Hidden/Baozang/VolumetricCloudsDepthDownsample"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "CloudsDepthDownsample"
            ZTest Always ZWrite Off Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float4 _CloudsFullResTexelSize; // xy = 1/fullWidth, 1/fullHeight
            float  _CheckerboardDepthOutput; // 1 = single-channel checkerboard (even=min/far, odd=max/near)

            struct Attributes { uint vertexID : SV_VertexID; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                o.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return o;
            }

            // Output: R = min depth (reversed-Z: farthest, conservative background), G = max depth (nearest).
            // The min channel preserves the original (single-channel) behaviour; the max channel is
            // used by the RDR2-style min/max ray-placement analysis.
            float2 Frag(Varyings input) : SV_Target
            {
                float2 ts = _CloudsFullResTexelSize.xy;
                float d0 = SampleSceneDepth(input.uv + float2(-0.5, -0.5) * ts);
                float d1 = SampleSceneDepth(input.uv + float2( 0.5, -0.5) * ts);
                float d2 = SampleSceneDepth(input.uv + float2(-0.5,  0.5) * ts);
                float d3 = SampleSceneDepth(input.uv + float2( 0.5,  0.5) * ts);
                float dMin = min(min(d0, d1), min(d2, d3));
                float dMax = max(max(d0, d1), max(d2, d3));

                // Single-channel checkerboard (RDR2, memory-saving): the half-res RT is R32; each cell
                // stores only one extreme by parity (even = min/far, odd = max/near). Only .x (R) lands.
                if (_CheckerboardDepthOutput != 0.0)
                {
                    int2 pc = (int2)input.positionCS.xy;
                    bool isMax = ((pc.x + pc.y) & 1) != 0;
                    float v = isMax ? dMax : dMin;
                    return float2(v, v);
                }

                // Dual channel: R = min (far, conservative background), G = max (near).
                return float2(dMin, dMax);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
