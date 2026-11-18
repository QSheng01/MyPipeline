Shader "ZhongMoDi/LutPost"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
        _LutTex ("Lut Texture", 2D) = "white" { }
        _LutWeight ("Lut Intensity", Range(0.0, 1.0)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            Name "LerpAimingScope"
            Tags { "LightMode" = "MyUtilPass" }

            ZWrite Off
            ZTest Always
            
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            TEXTURE2D(_MainTex);
            TEXTURE2D(_LutTex);
            SAMPLER(sampler_LutTex);

            TEXTURE2D_X(_BlitTexture);
            SamplerState sampler_LinearClamp;
            float _LutWeight;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = GetFullScreenTriangleVertexPosition(v.vertexID);
                o.uv = GetFullScreenTriangleTexCoord(v.vertexID);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 mainTex_var = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.uv);
                float3 originColor = mainTex_var.xyz;
                
                float3 lutInput = mainTex_var.xyz;

                
                lutInput = lutInput * float3(5.55555582, 5.55555582, 5.55555582) + float3(0.0479959995, 0.0479959995, 0.0479959995);
                lutInput = max(float3(0, 0, 0), lutInput);
                lutInput = log2(lutInput);
                lutInput = saturate(lutInput * float3(0.0734997839, 0.0734997839, 0.0734997839) + float3(0.386036009, 0.386036009, 0.386036009));

                
                float COLORS = 32.0;
                float maxColor = COLORS - 1.0;
                float halfColX = 0.5 / (COLORS * COLORS);
                float halfColY = 0.5 / COLORS;
                float threshold = maxColor / COLORS;

                
                
                float sliceOffset = lutInput.b * maxColor;
                float slice0 = floor(sliceOffset);
                float slice1 = min(slice0 + 1.0, maxColor);
                float sliceWeight = frac(sliceOffset);

                
                float xOffset = halfColX + lutInput.r * threshold / COLORS;
                
                
                
                float yOffset = 1.0 - (halfColY + lutInput.g * threshold);

                
                float2 uv0 = float2(xOffset +slice0 / COLORS, yOffset);
                float2 uv1 = float2(xOffset +slice1 / COLORS, yOffset);

                
                float3 color0 = SAMPLE_TEXTURE2D(_LutTex, sampler_LinearClamp, uv0).rgb;
                float3 color1 = SAMPLE_TEXTURE2D(_LutTex, sampler_LinearClamp, uv1).rgb;
                float3 finalColor = lerp(color0, color1, sliceWeight);

                finalColor = lerp(originColor, finalColor, _LutWeight);

                
                float2 pixelCoord = i.uv * _ScreenSize.xy;
                float ditherDot = dot(float2(171.0, 231.0), pixelCoord);
                float3 ditherNoise = float3(0.00970873795, 0.0140845068, 0.010309278) * ditherDot;
                ditherNoise = frac(ditherNoise);
                ditherNoise = ditherNoise - float3(0.5, 0.5, 0.5);
                finalColor = finalColor + (ditherNoise * 0.0013725491 * _LutWeight);
                
                
                return float4(finalColor, mainTex_var.a);
            }
            ENDHLSL
        }
    }
}
