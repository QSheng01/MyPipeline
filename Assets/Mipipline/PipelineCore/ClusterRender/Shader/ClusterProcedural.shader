Shader "MPipeline/Cluster/URP Lit"
{
    Properties
    {
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        _Roughness ("Roughness", Range(0, 1)) = 0.5
        [Toggle] _ClusterDebugMode ("Debug Cluster Colors", Float) = 0
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
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct ClusterPoint
            {
                float3 vertex;
                float3 normal;
                float4 tangent;
                float2 uv0;
            };

            StructuredBuffer<ClusterPoint> verticesBuffer;
            StructuredBuffer<uint> visibleIndices;
            StructuredBuffer<uint> _TriangleMaterialBuffer;
            struct BakedMaterialProperties
            {
                float4 color;
                float4 uvScaleOffset;
                int textureSlice;
                int padding0;
                int padding1;
                int padding2;
            };
            StructuredBuffer<BakedMaterialProperties> _MaterialBuffer;
            TEXTURE2D_ARRAY(_ClusterMaterialTextures);
            SAMPLER(sampler_ClusterMaterialTextures);
            float _UseBakedMaterials;

            struct ClusterLightData
            {
                float4 positionRange;
                float4 color;
                float4 directionType;
                float4 spotParams;
            };

            StructuredBuffer<ClusterLightData> _ClusterLights;
            StructuredBuffer<uint> _ClusterLightIndices;
            uint _ClusterLightEnabled;
            uint _ClusterLightCount;
            float4 _ClusterGridParams;
            float4 _ClusterCameraParams;

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _Roughness;
                float _ClusterDebugMode;
            CBUFFER_END

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                nointerpolation float3 clusterColor : TEXCOORD3;
                nointerpolation uint materialID : TEXCOORD4;
            };

            float3 GetClusterDebugColor(uint clusterIndex)
            {
                float cluster = (float)clusterIndex;
                float3 seed = frac(float3(
                    cluster * 0.1031,
                    cluster * 0.11369,
                    cluster * 0.13787));
                seed += dot(seed, seed.yzx + 33.33);
                float3 color = frac((seed.xxy + seed.yxx) * seed.zyx);
                return color * 0.75 + 0.25;
            }

            Varyings Vert(Attributes input)
            {
                uint clusterIndex = visibleIndices[input.instanceID];
                uint pointIndex = clusterIndex * 384 + input.vertexID;
                ClusterPoint clusterPoint = verticesBuffer[pointIndex];

                Varyings output;
                float3 positionWS = TransformObjectToWorld(clusterPoint.vertex);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.normalWS = TransformObjectToWorldNormal(clusterPoint.normal);
                output.uv = clusterPoint.uv0;
                output.clusterColor = GetClusterDebugColor(clusterIndex);
                output.materialID = _UseBakedMaterials > 0.5
                    ? _TriangleMaterialBuffer[clusterIndex * 128 + input.vertexID / 3] : 0;
                return output;
            }

            uint GetClusterLightCell(float3 positionWS)
            {
                float3 positionVS = TransformWorldToView(positionWS);
                float eyeDepth = max(-positionVS.z, _ClusterCameraParams.x);
                float2 ndc = positionVS.xy / eyeDepth / _ClusterCameraParams.zw;
                float2 uv = saturate(ndc * 0.5 + 0.5);
                uint x = min((uint)(uv.x * _ClusterGridParams.x), (uint)_ClusterGridParams.x - 1);
                uint y = min((uint)(uv.y * _ClusterGridParams.y), (uint)_ClusterGridParams.y - 1);
                float depthRatio = max(_ClusterCameraParams.y / _ClusterCameraParams.x, 1.0001);
                float normalizedDepth = log(eyeDepth / _ClusterCameraParams.x) / log(depthRatio);
                uint z = min(
                    (uint)(saturate(normalizedDepth) * _ClusterGridParams.z),
                    (uint)_ClusterGridParams.z - 1);
                return x + y * (uint)_ClusterGridParams.x
                    + z * (uint)(_ClusterGridParams.x * _ClusterGridParams.y);
            }

            half3 EvaluateClusterLights(float3 positionWS, half3 normalWS)
            {
                if (_ClusterLightEnabled == 0 || _ClusterLightCount == 0)
                    return 0;

                uint cell = GetClusterLightCell(positionWS);
                uint stride = (uint)_ClusterGridParams.w + 1;
                uint baseIndex = cell * stride;
                uint count = min(_ClusterLightIndices[baseIndex], (uint)_ClusterGridParams.w);
                half3 lighting = 0;

                [loop]
                for (uint i = 0; i < count; ++i)
                {
                    uint lightIndex = _ClusterLightIndices[baseIndex + 1 + i];
                    if (lightIndex >= _ClusterLightCount)
                        continue;

                    ClusterLightData light = _ClusterLights[lightIndex];
                    float3 toLight = light.positionRange.xyz - positionWS;
                    float distanceSquared = max(dot(toLight, toLight), 0.0001);
                    float inverseDistance = rsqrt(distanceSquared);
                    float3 lightDirection = toLight * inverseDistance;
                    float rangeSquared = max(light.positionRange.w * light.positionRange.w, 0.0001);
                    float smoothFactor = saturate(1.0 - distanceSquared / rangeSquared);
                    float attenuation = smoothFactor * smoothFactor / max(distanceSquared, 1.0);

                    if (light.directionType.w > 0.5)
                    {
                        float coneCos = dot(-lightDirection, normalize(light.directionType.xyz));
                        float coneRange = max(light.spotParams.y - light.spotParams.x, 0.0001);
                        float spotAttenuation = saturate((coneCos - light.spotParams.x) / coneRange);
                        attenuation *= spotAttenuation * spotAttenuation;
                    }

                    half ndotl = saturate(dot(normalWS, lightDirection));
                    lighting += light.color.rgb * (ndotl * attenuation);
                }
                return lighting;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                if (_ClusterDebugMode > 0.5)
                    return half4(input.clusterColor, 1.0);

                half3 albedo;
                half alpha;
                if (_UseBakedMaterials > 0.5)
                {
                    BakedMaterialProperties properties = _MaterialBuffer[input.materialID];
                    float2 bakedUV = input.uv * properties.uvScaleOffset.xy + properties.uvScaleOffset.zw;
                    half4 texel = SAMPLE_TEXTURE2D_ARRAY(_ClusterMaterialTextures,
                        sampler_ClusterMaterialTextures, bakedUV, properties.textureSlice);
                    albedo = texel.rgb * properties.color.rgb;
                    alpha = texel.a * properties.color.a;
                }
                else
                {
                    albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap,
                        TRANSFORM_TEX(input.uv, _BaseMap)).rgb * _BaseColor.rgb;
                    alpha = _BaseColor.a;
                }
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half ndotl = saturate(dot(normalize(input.normalWS), mainLight.direction));
                half3 directLighting = ndotl
                    * mainLight.color
                    * mainLight.shadowAttenuation;
                half3 additionalLighting = EvaluateClusterLights(
                    input.positionWS,
                    normalize(input.normalWS));
                half3 lighting = half3(0.2, 0.2, 0.2) + directLighting + additionalLighting;
                return half4(albedo * lighting, alpha);
            }
            ENDHLSL
        }

        // Procedural cluster geometry is not represented by a MeshRenderer, so
        // it must provide its own shadow-caster pass.  The renderer feature
        // submits this pass while URP's shadow atlas is bound.
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            Cull Back
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ClusterPoint
            {
                float3 vertex;
                float3 normal;
                float4 tangent;
                float2 uv0;
            };

            StructuredBuffer<ClusterPoint> verticesBuffer;
            StructuredBuffer<uint> visibleIndices;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowVert(Attributes input)
            {
                uint clusterIndex = visibleIndices[input.instanceID];
                uint pointIndex = clusterIndex * 384 + input.vertexID;
                ClusterPoint clusterPoint = verticesBuffer[pointIndex];

                float3 positionWS = TransformObjectToWorld(clusterPoint.vertex);
                float3 normalWS = TransformObjectToWorldNormal(clusterPoint.normal);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                positionWS = ApplyShadowBias(positionWS, normalWS, lightDirectionWS);

                Varyings output;
                // The shadow pass sets the active cascade's view/projection
                // matrices on the command buffer.  Use Unity's canonical
                // transform here so cascade near/far clipping and platform
                // reversed-Z handling match URP exactly.
                output.positionCS = TransformWorldToHClip(positionWS);
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
