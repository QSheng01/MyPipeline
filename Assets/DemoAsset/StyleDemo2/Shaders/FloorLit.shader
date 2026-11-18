Shader "ZhongMoDi/FloorLit"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _MaskMap ("Mask Map", 2D) = "white" {}
        _VolumetricFogLut ("Volumetric Fog LUT", 3D) = "" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _OverlayColor ("Overlay Color", Color) = (1, 1, 1, 1)
        _UseFloorInstanceData ("Use Original Instance Data", Float) = 0
        _UseFloorFrameData ("Use Original Frame Matrices", Float) = 0
        _FloorJitterParams ("Jitter Params", Vector) = (0, 0, 0, 0)
        [Header(Floor Material)]
        _FloorExposureBlend ("Exposure Blend", Range(0, 1)) = 0
        _FloorColorIntensity ("Color Intensity", Float) = 1
        _FloorAlphaIntensity ("Alpha Intensity", Float) = 1
        _FloorMotionMaskStrength ("Motion Mask Strength", Range(0, 1)) = 0
        _FloorMotionEnabledParam ("Motion Enabled Param", Float) = 0
        _FloorOverlayAlphaScale ("Overlay Alpha Scale", Float) = 1
        _FloorDepthBlendNear ("Depth Blend Near", Float) = 0
        _FloorDepthBlendFar ("Depth Blend Far", Float) = 1
        _FloorSdfUseDistanceScale ("SDF Use Distance Scale", Float) = 0
        _FloorSdfDistanceStart ("SDF Distance Start", Float) = 0
        _FloorSdfDistanceEnd ("SDF Distance End", Float) = 1
        _FloorMotionAlphaGate ("Motion Alpha Gate", Range(0, 1)) = 1
        _BaseMapTransform ("Base Map Transform", Vector) = (1, 1, 0, 0)
        _MaskMapTransform ("Mask Map Transform", Vector) = (1, 1, 0, 0)
        [Header(Fog Approximation)]
        _FloorFogEnabled ("Fog Enabled", Float) = 0
        _FloorFogCameraMode ("Fog Camera Mode", Float) = 0
        _FloorFogMainLightDir ("Fog Main Light Dir", Vector) = (0, 1, 0, 0)
        _FloorFogDistanceParams ("Fog Distance Params", Vector) = (0, 0, 0, 0)
        _FloorFogBaseColor ("Fog Base Color", Color) = (0, 0, 0, 0)
        _FloorFogHeightParams0 ("Fog Height Params 0", Vector) = (0, 0, 0, 0)
        _FloorFogHeightParams1 ("Fog Height Params 1", Vector) = (0, 0, 0, 0)
        _FloorFogDistanceBlend ("Fog Distance Blend", Vector) = (0, 0, 0, 0)
        _FloorFogScatterColor ("Fog Scatter Color", Color) = (0, 0, 0, 0)
        _FloorFogScatterDir ("Fog Scatter Dir", Vector) = (0, 1, 0, 1)
        _FloorFogScatterColor2 ("Fog Scatter Color 2", Color) = (0, 0, 0, 1)
        _FloorFogVolumetricParams0 ("Fog Volumetric 0", Vector) = (0, 0, 1, 0)
        _FloorFogVolumetricParams1 ("Fog Volumetric 1", Vector) = (1, 0, 1, 0)
        _FloorFogVolumetricParams2 ("Fog Volumetric 2", Vector) = (1, 1, 0, 0)
        _FloorFogVolumetricParams3 ("Fog Volumetric 3", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
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
            TEXTURE2D(_MaskMap);
            SAMPLER(sampler_MaskMap);
            TEXTURE3D(_VolumetricFogLut);
            SAMPLER(sampler_VolumetricFogLut);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _MaskMap_ST;
                float4 _BaseColor;
                float4 _OverlayColor;
                float _UseFloorInstanceData;
                float _UseFloorFrameData;
                float4 _FloorJitterParams;
                float _FloorExposureBlend;
                float _FloorColorIntensity;
                float _FloorAlphaIntensity;
                float _FloorMotionMaskStrength;
                float _FloorMotionEnabledParam;
                float _FloorOverlayAlphaScale;
                float _FloorDepthBlendNear;
                float _FloorDepthBlendFar;
                float _FloorSdfUseDistanceScale;
                float _FloorSdfDistanceStart;
                float _FloorSdfDistanceEnd;
                float _FloorMotionAlphaGate;
                float4 _BaseMapTransform;
                float4 _MaskMapTransform;
            CBUFFER_END

            CBUFFER_START(FloorFrameData)
                float4x4 _FloorClipMatrix;
                float4x4 _FloorPreviousClipMatrix;
                float4x4 _FloorViewMatrix;
                float4x4 _FloorInvProjectionMatrix;
                float4 _FloorCameraPosition;
                float4 _FloorPreviousCameraPosition;
                float4 _FloorScreenSize;
                float _FloorGlobalMipBias;
                uint _FloorFrameCount;
                float4 _FloorExposureParams;
                float4 _FloorFogMainLightDir;
                float4 _FloorFogDistanceParams;
                float4 _FloorFogBaseColor;
                float4 _FloorFogHeightParams0;
                float4 _FloorFogHeightParams1;
                float4 _FloorFogDistanceBlend;
                float4 _FloorFogScatterColor;
                float4 _FloorFogScatterDir;
                float4 _FloorFogScatterColor2;
                float4 _FloorFogVolumetricParams0;
                float4 _FloorFogVolumetricParams1;
                float4 _FloorFogVolumetricParams2;
                float4 _FloorFogVolumetricParams3;
                float _FloorFogEnabled;
                float _FloorFogCameraMode;
            CBUFFER_END


            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float4 secondaryPositionOS : TEXCOORD2;
                uint instanceID : SV_InstanceID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionRWS : TEXCOORD1;
                float3 clipPositionXYW : TEXCOORD3;
                float3 previousClipPositionXYW : TEXCOORD4;
            };


            float NMax(float a, float b)
            {
                return max(a, b);
            }

            float2 NMin(float2 a, float2 b)
            {
                return min(a, b);
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

            float ReconstructViewDepth(Varyings input)
            {
                if (_UseFloorFrameData == 0.0)
                {
                    return max(length(input.positionRWS), 1e-5);
                }

                float2 ndc = ((input.positionCS.xy * _FloorScreenSize.zw) * 2.0) - 1.0;
                float4 clipPosition = float4(ndc.x, -ndc.y, input.positionCS.z, 1.0);
                float4 viewPosition = mul(clipPosition, _FloorInvProjectionMatrix);
                float3 viewPositionNdc = viewPosition.xyz / max(viewPosition.w, 1e-5);
                return abs(mul(float4(viewPositionNdc, 1.0), _FloorViewMatrix).z);
            }

            float3 GetFloorViewPosition(Varyings input)
            {
                if (_UseFloorFrameData == 0.0)
                {
                    return input.positionRWS;
                }

                float2 ndc = ((input.positionCS.xy * _FloorScreenSize.zw) * 2.0) - 1.0;
                float4 clipPosition = float4(ndc.x, -ndc.y, input.positionCS.z, 1.0);
                float4 viewPosition = mul(clipPosition, _FloorInvProjectionMatrix);
                return viewPosition.xyz / max(viewPosition.w, 1e-5);
            }

            float2 EncodeMotionVector(float3 currentClipXYW, float3 previousClipXYW)
            {
                float2 motion = (currentClipXYW.xy / max(currentClipXYW.z, 1e-8)) - (previousClipXYW.xy / max(previousClipXYW.z, 1e-8));
                motion.y = -motion.y;
                return (sqrt(sqrt(abs(motion * 0.5))) * sign(motion) * 0.5) + 0.5;
            }

            float HeightFogIntegral(float scale, float density, float height, float heightDelta)
            {
                float x = max(-127.0, scale * heightDelta);
                float integral = (abs(x) > 5.9604644775390625e-08) ? ((1.0 - exp2(-x)) / x) : (0.6931471824645996 - (0.24022650718688965 * x));
                return density * exp2(-max(-127.0, scale * height)) * integral;
            }

            void EvaluateFloorFog(float3 viewPosition, float3 viewDir, float viewDistance, float viewDepth, out float3 fogColor, out float fogTransmittance)
            {
                float distanceFade = saturate((viewDistance * _FloorFogDistanceBlend.w) + _FloorFogDistanceBlend.z);
                float heightDelta = viewPosition.y;

                float fogIntegral =
                    HeightFogIntegral(_FloorFogHeightParams0.z, _FloorFogHeightParams0.y, _FloorCameraPosition.y - _FloorFogHeightParams0.x, heightDelta) +
                    HeightFogIntegral(_FloorFogHeightParams1.x, _FloorFogHeightParams1.y, _FloorCameraPosition.y - _FloorFogHeightParams1.z, heightDelta);

                float distanceTransmittance = saturate(exp2(-(fogIntegral * viewDistance)));
                fogTransmittance = saturate(max(distanceTransmittance, _FloorFogBaseColor.w) + saturate((viewDistance * _FloorFogDistanceBlend.y) + _FloorFogDistanceBlend.x) + distanceFade);

                float scatterAmount = 1.0 - saturate(exp2(-(fogIntegral * max(viewDistance - _FloorFogScatterDir.w, 0.0))));
                float directionalScatter = pow(saturate(dot(-viewDir, normalize(_FloorFogScatterDir.xyz))), _FloorFogScatterColor2.w);
                fogColor = (_FloorFogBaseColor.xyz * (1.0 - fogTransmittance)) + ((_FloorFogScatterColor2.xyz * directionalScatter) * scatterAmount * (1.0 - distanceFade));

                // The original shader can additionally sample a volumetric fog 3D LUT.
                // This approximation keeps the height/distance fog path active without requiring that capture resource.
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
             
                float3 cameraPositionWS =  GetCameraPositionWS();
                float3 previousCameraPositionWS =  GetCameraPositionWS();

                float3 positionRWS = TransformObjectToWorld(input.positionOS) - cameraPositionWS;

                float4 originalPositionCS =TransformWorldToHClip(positionRWS + cameraPositionWS);

                float4 positionCS = originalPositionCS;
               
                output.positionCS = positionCS;
                output.uv = TRANSFORM_TEX(input.uv0, _BaseMap);
              
                
               

                return output;
            }

             float4 Frag(Varyings input) : SV_Target
            {
              
                uint2 pixelCoord = uint2(input.positionCS.xy);
                float viewDepth = ReconstructViewDepth(input);
                float3 viewPosition = GetFloorViewPosition(input);

                float3 cameraToPixel = (_FloorFogCameraMode == 0.0) ? (_FloorCameraPosition.xyz - viewPosition) : float3(_FloorViewMatrix._m20, _FloorViewMatrix._m21, _FloorViewMatrix._m22);
                float distanceSq = dot(cameraToPixel, cameraToPixel);
                float invDistance = rsqrt(max(distanceSq, 1e-8));
                float3 viewDir = cameraToPixel * invDistance;
                float viewDistance = distanceSq * invDistance;

                float4 baseSample = SAMPLE_TEXTURE2D_BIAS(_BaseMap, sampler_BaseMap, input.uv * _BaseMapTransform.xy + _BaseMapTransform.zw, _FloorGlobalMipBias);
                float4 albedo = baseSample * _BaseColor;
                float3 baseColor = (albedo.rgb * _FloorColorIntensity) / lerp(1.0, _FloorExposureParams.x, _FloorExposureBlend);
                float baseAlpha = albedo.a * _FloorAlphaIntensity;

                float4 maskSample = SAMPLE_TEXTURE2D_BIAS(_MaskMap, sampler_MaskMap, input.uv * _MaskMapTransform.xy + _MaskMapTransform.zw, _FloorGlobalMipBias);
                float maskAlpha = maskSample.a;
                float sdf = 0.5 - maskSample.r;
                float sdfWidth = max(length(float2(ddx(sdf), ddy(sdf))), 1e-5);
                float sdfDistanceBlend = saturate((viewDepth - _FloorSdfDistanceStart) / max(_FloorSdfDistanceEnd - _FloorSdfDistanceStart, 1e-5));
                float sdfEdge = saturate(0.5 - lerp(sdf, sdf / sdfWidth, sdfDistanceBlend));
                float overlayStrength = saturate((baseAlpha + maskAlpha) * _OverlayColor.a) * maskAlpha;
                float4 materialColor = (_OverlayColor * sdfEdge * overlayStrength) + float4(baseColor, albedo.a);

                float3 premultiplied = materialColor.rgb;
                float alphaOut = baseAlpha;

                float3 fogColor;
                float fogTransmittance;
                EvaluateFloorFog(viewPosition, viewDir, viewDistance, viewDepth, fogColor, fogTransmittance);
              
                float heightFactor = viewPosition.y * _FloorFogScatterColor.w;
                float densityHeight = max(0.01, heightFactor + _FloorFogScatterColor2.w);
                float3 aerialTransmittance = exp(_FloorFogBaseColor.xyz * ((-max(0.0, (viewDistance * _FloorFogDistanceParams.w) - _FloorFogDistanceParams.z)) * (((1.0 - exp(-densityHeight)) / densityHeight) * exp(heightFactor + _FloorFogMainLightDir.w))));
                float lightDot = dot(-viewDir, _FloorFogDistanceParams.xyz);
                float phaseG2 = _FloorFogBaseColor.w * _FloorFogBaseColor.w;
                float phaseDenominator = (1.0 + phaseG2) - ((2.0 * _FloorFogBaseColor.w) * lightDot);
                float3 phaseColor = clamp(
                    ((_FloorFogScatterColor.xyz * (0.05968310311436653 * (1.0 + (lightDot * lightDot)))) + _FloorFogMainLightDir.xyz) +
                    (_FloorFogScatterColor2.xyz * ((1.0 - phaseG2) / max((12.566370964050293 * phaseDenominator) * sqrt(phaseDenominator), 0.0010000000474974513))),
                    0.0,
                    1.0);

                float3 finalFogColor = (_FloorFogEnabled != 0.0)
                    ? (premultiplied * (aerialTransmittance * fogTransmittance)) + ((((phaseColor * 255.0) * (1.0 - aerialTransmittance)) * fogTransmittance) + fogColor)
                    : premultiplied;

                float4 target0 = float4(finalFogColor, alphaOut);
                 return target0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
