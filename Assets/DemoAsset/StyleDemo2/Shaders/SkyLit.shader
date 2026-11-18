Shader "ZhongMoDi/SkyLit"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _SkyPushMax ("Camera Push Max", Float) = 0
        _SkyCameraPushDisabled ("Camera Push Disabled", Range(0, 1)) = 0
        _SkyUvBlendScale ("UV Blend Scale", Float) = 1
        _SkyUseRawVertexColor ("Use Raw Vertex Color", Float) = 1
        _SkyUvScroll0 ("UV Scroll 0", Vector) = (0, 0, 0, 0)
        _SkyUvRotate ("UV Rotate Matrix", Vector) = (1, 0, 0, 1)
        _SkyUvBlendParams ("UV Blend Params", Vector) = (1, 0, 0, 0)
        _SkyUvST ("UV ST", Vector) = (1, 1, 0, 0)
        _SkyGlobal97 ("Global 97", Vector) = (0, 0, 0, 0)
        _UseSkyInstanceData ("Use Original Instance Data", Float) = 0
        _UseSkyFrameData ("Use Original Frame Matrices", Float) = 0
        [Header(Fragment Parameters)]
        _SkyAlphaDitherStrength ("Alpha Dither Strength", Range(0, 1)) = 0
        _SkyAlphaSuppress ("Alpha Suppress", Range(0, 1)) = 0
        _SkyMotionMaskStrength ("Motion Mask Strength", Range(0, 1)) = 0
        _SkyMotionEnabledParam ("Motion Enabled Param", Float) = 0
        _SkyForceWhiteVertexColor ("Force White Vertex Color", Float) = 0
        _SkyColorIntensity ("Color Intensity", Float) = 1
        _SkyAlphaIntensity ("Alpha Intensity", Float) = 1
        _SkyBloomThreshold ("Bloom Threshold", Float) = 1000
        _SkyBloomBoost ("Bloom Boost", Float) = 0
        _SkyFinalBlend ("Final Style Blend", Range(0, 1)) = 0
        _SkyExposureBlend ("Exposure Blend", Range(0, 1)) = 0
        _SkyTextureAlphaMode ("Texture Alpha Mode", Range(0, 1)) = 0
        _SkyTextureMipLevel ("Texture Mip Level", Float) = 0
        _SkyDepthFadeEnabled ("Depth Fade Enabled", Float) = 0
        _SkyDepthFadeNear0 ("Depth Fade Near 0", Float) = 0
        _SkyDepthFadeNear1 ("Depth Fade Near 1", Float) = 1
        _SkyDepthFadeFar0 ("Depth Fade Far 0", Float) = 0
        _SkyDepthFadeFar1 ("Depth Fade Far 1", Float) = 1
        _SkyGradientCurveUse ("Gradient Curve Use", Range(0, 1)) = 0
        _SkyDissolveGlobal ("Dissolve Global", Range(0, 1)) = 0
        _SkyGradientEnabled ("Gradient Enabled", Float) = 0
        _SkyGradientScale ("Gradient Scale", Float) = 0
        _SkyGradientSoftness ("Gradient Softness", Float) = 1
        _SkyGradientCenter ("Gradient Center", Float) = 0
        _SkyGradientEnd ("Gradient End", Float) = 1
        _SkyGradientMix ("Gradient Mix", Float) = 1
        _SkyGradientColor ("Gradient Color", Color) = (1, 1, 1, 1)
        _SkyExposureScale ("Exposure Scale", Float) = 1
        _SkyStyleColor ("Style Color", Color) = (1, 1, 1, 1)
        _SkyStyleSaturation ("Style Saturation", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _SkyPushMax;
                float _SkyCameraPushDisabled;
                float _SkyUvBlendScale;
                float _SkyUseRawVertexColor;
                float4 _SkyUvScroll0;
                float4 _SkyUvRotate;
                float4 _SkyUvBlendParams;
                float4 _SkyUvST;
                float4 _SkyGlobal97;
                float _UseSkyInstanceData;
                float _UseSkyFrameData;
                float _SkyAlphaDitherStrength;
                float _SkyAlphaSuppress;
                float _SkyMotionMaskStrength;
                float _SkyMotionEnabledParam;
                float _SkyForceWhiteVertexColor;
                float _SkyColorIntensity;
                float _SkyAlphaIntensity;
                float _SkyBloomThreshold;
                float _SkyBloomBoost;
                float _SkyFinalBlend;
                float _SkyExposureBlend;
                float _SkyTextureAlphaMode;
                float _SkyTextureMipLevel;
                float _SkyDepthFadeEnabled;
                float _SkyDepthFadeNear0;
                float _SkyDepthFadeNear1;
                float _SkyDepthFadeFar0;
                float _SkyDepthFadeFar1;
                float _SkyGradientCurveUse;
                float _SkyDissolveGlobal;
                float _SkyGradientEnabled;
                float _SkyGradientScale;
                float _SkyGradientSoftness;
                float _SkyGradientCenter;
                float _SkyGradientEnd;
                float _SkyGradientMix;
                float4 _SkyGradientColor;
                float _SkyExposureScale;
                float4 _SkyStyleColor;
                float _SkyStyleSaturation;
            CBUFFER_END

            CBUFFER_START(SkyFrameData)
                float4x4 _SkyClipMatrix;
                float4x4 _SkyPreviousClipMatrix;
                float4x4 _SkyViewMatrix;
                float4x4 _SkyInvProjectionMatrix;
                float4 _SkyCameraPosition;
                float4 _SkyPreviousCameraPosition;
                float4 _SkyJitterParams;
                float4 _SkyScreenSize;
                float _SkyGlobalMipBias;
            CBUFFER_END

            struct SkyInstanceData
            {
                float4x4 objectMatrix;
                float4 flags;
                float4 skinOffsets;
                float4x4 secondaryMatrix;
                float4 secondaryPositionMode;
                float4 unused0;
                float4 unused1;
                float4 instanceColorMask;
                float4 unused2;
                float4 unused3;
            };

            StructuredBuffer<SkyInstanceData> _SkyInstanceData;
            StructuredBuffer<float4> _SkySkinningBuffer;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv0 : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uv : TEXCOORD0;
                float4 color : TEXCOORD2;
                float3 clipPositionXYW : TEXCOORD3;
                float3 previousClipPositionXYW : TEXCOORD4;
            };

            struct FragmentOutput
            {
                float4 target0 : SV_Target0;
                float4 target1 : SV_Target1;
            };

            float NMin(float a, float b)
            {
                return min(a, b);
            }

            float NMax(float a, float b)
            {
                return max(a, b);
            }

            float3 TransformByRows(float3 value, float4x4 transformMatrix)
            {
                return float3(
                    dot(float3(transformMatrix._m00, transformMatrix._m01, transformMatrix._m02), value),
                    dot(float3(transformMatrix._m10, transformMatrix._m11, transformMatrix._m12), value),
                    dot(float3(transformMatrix._m20, transformMatrix._m21, transformMatrix._m22), value));
            }

            float3 TransformPositionByRows(float3 value, float4x4 transformMatrix)
            {
                return TransformByRows(value, transformMatrix) + float3(transformMatrix._m03, transformMatrix._m13, transformMatrix._m23);
            }

            float3 TransformPositionToCameraRelative(float3 positionOS, float4x4 transformMatrix, float3 cameraPositionWS)
            {
                return TransformPositionByRows(positionOS, transformMatrix) - cameraPositionWS;
            }

            float2 MulGlslMat2(float4 matrixColumns, float2 value)
            {
                return float2(
                    matrixColumns.x * value.x + matrixColumns.z * value.y,
                    matrixColumns.y * value.x + matrixColumns.w * value.y);
            }

            float SafeRange01(float value, float minValue, float maxValue)
            {
                return saturate((value - minValue) / max(maxValue - minValue, 1e-5));
            }

            float ScreenDither(float2 pixelPosition)
            {
                return abs(frac(52.98291778564453 * frac(dot(pixelPosition, float2(0.06711056083440781, 0.005837149918079376)))));
            }

            float3 Luminance3(float3 color)
            {
                return dot(color, float3(0.2126729041337967, 0.7151522040367126, 0.07217500358819962)).xxx;
            }

            float ReconstructOriginalViewDepth(Varyings input)
            {
                if (_UseSkyFrameData == 0.0)
                {
                    return abs(input.positionCS.w);
                }

                float2 ndc = ((input.positionCS.xy * _SkyScreenSize.zw) * 2.0) - 1.0;
                float4 clipPosition = float4(ndc.x, -ndc.y, input.positionCS.z, 1.0);
                float4 viewPosition = mul(clipPosition, _SkyInvProjectionMatrix);
                float3 viewPositionNdc = viewPosition.xyz / max(viewPosition.w, 1e-5);
                return abs(mul(float4(viewPositionNdc, 1.0), _SkyViewMatrix).z);
            }

        

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                

                float3 skinnedPositionOS;
             
                    skinnedPositionOS = input.positionOS;

                float3 objectPositionWS = TransformObjectToWorld(skinnedPositionOS);
                float3 cameraPositionWS =  GetCameraPositionWS();
                float3 previousCameraPositionWS = GetCameraPositionWS();
                float3 cameraToObject = cameraPositionWS - objectPositionWS;
                float cameraDistance = max(length(cameraToObject), 0.0001);
                float pushDistance = NMin(NMax(0.0, cameraDistance - 1.0), _SkyPushMax) + 0.0010000000474974513;
                float3 cameraPush = (cameraToObject / cameraDistance) * pushDistance * (1.0 - _SkyCameraPushDisabled);

                float3 positionRWS = (objectPositionWS - cameraPositionWS) + cameraPush;
                float4 originalPositionCS =  TransformWorldToHClip(objectPositionWS + cameraPush);
                float4 positionCS = originalPositionCS;
          

                float2 uvBlendScaled = input.uv0 * _SkyUvBlendScale;
                input.uv0.y = 1 - input.uv0.y;
                float2 uvSource =
                    input.uv0.xy * _SkyUvBlendParams.x +
                    (((input.uv0.xy * _SkyUvBlendScale) - (input.uv0.xy * _SkyUvBlendScale)) + input.uv0.xy) * _SkyUvBlendParams.y +
                    _SkyUvScroll0.xy * _SkyGlobal97.w +
                    _SkyUvScroll0.zw * uvBlendScaled.x;

                float2 uv = (MulGlslMat2(_SkyUvRotate, uvSource - 0.5) + 0.5) * _SkyUvST.xy + _SkyUvST.zw;
                output.uv = float4(uv, uvBlendScaled.x, input.uv0.y);

                output.positionCS = positionCS;
                output.color =input.color ;
                output.clipPositionXYW = originalPositionCS.xyw;

                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
              
                FragmentOutput output;

                float viewDepth = ReconstructOriginalViewDepth(input);
             
                float4 vertexColor = lerp(input.color, float4(1.0, 1.0, 1.0, 1.0), (_SkyForceWhiteVertexColor != 0.0) ? 1.0 : 0.0);
                float4 baseTint = _BaseColor * vertexColor;
                baseTint.rgb *= _SkyColorIntensity;
                baseTint.a *= _SkyAlphaIntensity;

                float mipBias = float((int)_SkyTextureMipLevel) + _SkyGlobalMipBias;
                float4 baseSample = SAMPLE_TEXTURE2D_BIAS(_BaseMap, sampler_BaseMap, input.uv.xy, mipBias);
                float4 textureColor = lerp(baseSample, float4(1.0, 1.0, 1.0, baseSample.r), _SkyTextureAlphaMode);
                float4 shadedColor = baseTint * textureColor * (1.0 - _SkyDissolveGlobal);

                float gradientSelector = _SkyGradientScale * _SkyGradientMix;
                float gradientRamp = -lerp(
                    -1.0,
                    ((_SkyGradientCenter + input.uv.z) * 2.0199999809265137) - 1.0099999904632568,
                    (1.0 - gradientSelector) + ((1.0 - _SkyGradientCurveUse) * gradientSelector));

                float gradientEnter = saturate(gradientRamp * _SkyGradientSoftness);
                float gradientExit = saturate((_SkyGradientEnd - gradientRamp) * _SkyGradientSoftness);

                float4 gradientColor;
                gradientColor.a = shadedColor.a * gradientEnter;
                gradientColor.rgb = lerp(shadedColor.rgb, (_SkyGradientColor.rgb * gradientExit) * _SkyColorIntensity, gradientExit);
                float4 colorAfterGradient = (_SkyGradientEnabled != 0.0) ? gradientColor : shadedColor;

                float ditherValue = ScreenDither(input.positionCS.xy);
                float depthFade = (_SkyDepthFadeEnabled != 0.0)
                    ? SafeRange01(viewDepth, _SkyDepthFadeNear0, _SkyDepthFadeNear1) * SafeRange01(viewDepth, _SkyDepthFadeFar0, _SkyDepthFadeFar1)
                    : 1.0;

                float2 instanceAlphaDither = float2(0.0, 0.0);
          
                float signedDither = (instanceAlphaDither.x >= 0.0) ? ditherValue : -ditherValue;
                float alphaDither = ((instanceAlphaDither.x - signedDither) > 0.0) ? 1.0 : (1.0 - _SkyAlphaDitherStrength);
                float alpha = saturate(colorAfterGradient.a * depthFade) * (1.0 - instanceAlphaDither.y) * alphaDither;

                float3 exposedColor = clamp(
                    (colorAfterGradient.rgb + (max(colorAfterGradient.rgb - float3(_SkyBloomThreshold, _SkyBloomThreshold, _SkyBloomThreshold), 0.0) * _SkyBloomBoost)) /
                    ((1.0 - _SkyExposureBlend) + (_SkyExposureScale * _SkyExposureBlend)),
                    0.0,
                    1000.0);

                float alphaOut = alpha * (1.0 - _SkyAlphaSuppress);
                float2 motionData = float2(0.0, 0.0);
                if (saturate((_SkyMotionEnabledParam + 1.0) - _SkyAlphaDitherStrength) > 0.5)
                {
                    float2 currentNdc = input.clipPositionXYW.xy / max(input.clipPositionXYW.z, 1e-8);
                    float2 previousNdc = input.previousClipPositionXYW.xy / max(input.previousClipPositionXYW.z, 1e-8);
                    float2 motion = currentNdc - previousNdc;
                    motion.y = -motion.y;
                    motionData = (sqrt(sqrt(abs(motion * 0.5))) * sign(motion) * 0.5) + 0.5;
                }

                float3 premultipliedColor = exposedColor * alpha;
                float luminance = dot(premultipliedColor, float3(0.2126729041337967, 0.7151522040367126, 0.07217500358819962));
                float3 styledColor =
                    (lerp(luminance.xxx, premultipliedColor, _SkyStyleSaturation) * _SkyStyleColor.rgb) * _SkyFinalBlend +
                    premultipliedColor * (1.0 - _SkyFinalBlend);

                float4 target0 = float4(styledColor, alphaOut);
                return target0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
