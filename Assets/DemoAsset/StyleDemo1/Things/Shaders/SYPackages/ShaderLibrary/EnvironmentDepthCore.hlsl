#ifndef ENVIRONMENT_DEPTH_INCLUDE
#define ENVIRONMENT_DEPTH_INCLUDE

TEXTURE2D(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);

#if defined(ENVIRONMENT_DEPTH_ON)
    CBUFFER_START(EnvironmentDepth)
        half4 _EnvironmentMixColor;
        half _EnvironmentDepthIns;
    CBUFFER_END
#endif

half3 EnvironmentLightDepthGI(half3 bakeGi,float3 positionWS)
{
    #if defined(ENVIRONMENT_DEPTH_ON)
        float4 postionCS = TransformWorldToHClip(positionWS);
        float4 scrPos = ComputeScreenPos(postionCS);
        float2 screenPos= scrPos .xy / scrPos .w;
        float depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, screenPos).r;
        depth = clamp(depth * _EnvironmentDepthIns,0.0,1.0); 
        bakeGi = lerp(bakeGi,_EnvironmentMixColor.rgb,depth);
    #endif

    return bakeGi;
}

#endif