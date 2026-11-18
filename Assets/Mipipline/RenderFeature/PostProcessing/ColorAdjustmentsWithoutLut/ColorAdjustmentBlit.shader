Shader "PostProcess/ColorAdjustmentBlit"
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
        }

        Pass
        {
            Name "ColorAdjustmentBlit"
            ZTest Off
            
            HLSLPROGRAM
            #pragma vertex FullscreenVert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings FullscreenVert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            
            //自定义直接修改后处理颜色的功能
            half4 _ColorAdjustmentsColor;
            half4 _ColorAdjustmentsParams;//X:亮度、Y:对比度、Z:饱和度、W:空

            inline half3 ColorAdjustmentsWithoutLut(half3 inputColor)
            {
                //自定义直接修改后处理颜色的功能
                half3 finalColor = inputColor;
                
                //色调
                finalColor *= _ColorAdjustmentsColor.rgb;

                //饱和度
                half luminance = dot(half3(0.2125, 0.7154, 0.0721), finalColor);
                finalColor = lerp(luminance.xxx, finalColor, _ColorAdjustmentsParams.z);

                //对比度
                half3 avgColor = half3(0.5, 0.5, 0.5);
                finalColor = lerp(avgColor, finalColor, _ColorAdjustmentsParams.y);

                //亮度
                finalColor *= _ColorAdjustmentsParams.x;
                
                return finalColor;
            }

            half4 frag(Varyings i) : SV_TARGET
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                color.rgb = ColorAdjustmentsWithoutLut(color.rgb);
                return color;
            }

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
