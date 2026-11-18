Shader "Hidden/AVT/FrameDebugger"
{
    Properties {}
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 vert(float4 positionOS : POSITION) : SV_POSITION { return positionOS; }
            float4 frag() : SV_Target { discard; return 0; }
            ENDHLSL
        }
    }
}