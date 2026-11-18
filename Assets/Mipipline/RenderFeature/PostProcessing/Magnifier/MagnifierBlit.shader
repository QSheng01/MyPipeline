Shader "PostProcess/MagnifierBlit"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always
            Name "MagnifierBlit"
            
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MagnifierPosAndRect;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                
                // o.pos = mul(UNITY_MATRIX_MVP, v.vertex);

                o.pos = TransformObjectToHClip(v.vertex.xyz);
                float uMin = _MagnifierPosAndRect.x - _MagnifierPosAndRect.z / 2.0f;
                // float uMax = _MagnifierPosAndRect.x + _MagnifierPosAndRect.z / 2.0f;
                float vMin = _MagnifierPosAndRect.y - _MagnifierPosAndRect.w / 2.0f;
                // float vMax = _MagnifierPosAndRect.y + _MagnifierPosAndRect.z / 2.0f;
                
                // uv 由原来的 u => 0,1 v => 0,1
                // 变成 u = > uMin, uMax;
                // 变成 v = > vMin, vMax;
                o.uv.x = uMin + _MagnifierPosAndRect.z * v.uv.x;
                o.uv.y = vMin + _MagnifierPosAndRect.w * v.uv.y;

                return o;
            }

            half4 frag(v2f i) : SV_TARGET
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
            }

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}