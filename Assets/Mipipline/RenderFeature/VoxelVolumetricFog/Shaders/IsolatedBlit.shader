Shader "ShiYue/URP/VoxelVolumetric/IsolatedBlit"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        
        ZWrite Off
        Cull Off
        ZTest Always
        
        Pass
        {
            Name "Isolated Blit Pass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "VoxelVolumetricCommon.hlsl"
            
			Texture3D _VBufferLighting;
			float _HeightFogBaseExtinction;

            float4 Fragment (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 screenColor = _BlitTexture.SampleLevel(voxel_point_clamp_sampler, input.texcoord, 0).rgb;
            	
				float depthRaw = SampleSceneDepth(input.texcoord);
				float depthEye = GetDepthEye(depthRaw);
                float z = depthEye * GetRayLengthCompute(input.texcoord.xy) * _InverseMaxDistanceVolumetric;
				float3 uvw = float3(input.texcoord.xy, InverseRescaleDepth(z, _PERCENTILE, _RANGE));
				uvw = saturate(uvw);

				float4 integratorData = _VBufferLighting.SampleLevel(voxel_linear_clamp_sampler, uvw, 0).rgba;
            	
            	float3 output = (screenColor * integratorData.a) + integratorData.rgb ;
            	integratorData.rgb = output;
				return integratorData;
            }
            ENDHLSL
        }
    }
}