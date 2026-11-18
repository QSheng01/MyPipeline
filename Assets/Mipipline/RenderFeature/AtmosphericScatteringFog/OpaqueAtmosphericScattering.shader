Shader "ShiYue/URP/FOG/OpaqueAtmosphericScattering"
{ 
    HLSLINCLUDE
    //#pragma target 4.5

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "AtmosphericScattering_Input.hlsl"
    #include "AtmosphericScattering.hlsl"
    // #include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/Common.hlsl"
    //#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"

    #pragma multi_compile _ _SUNCOLOR_ON
    #pragma multi_compile _ _CONSTANTCOLOR

    SamplerState my_point_clamp_sampler;
    
    struct AttributesDefault
    {
        float3 vertex : POSITION;
    };
    
    struct VaryingsDefault
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord : TEXCOORD0;
        float3 screenUV : TEXCOORD1;
    };

    // Vertex manipulation
    float2 TransformTriangleVertexToUV(float2 vertex)
    {
        float2 uv = (vertex + 1.0) * 0.5;
        return uv;
    }
    
    VaryingsDefault VertDefault(AttributesDefault v)
    {
        VaryingsDefault o;
        o.positionCS = float4(v.vertex.xy, 0.0, 1.0);
        o.texcoord = TransformTriangleVertexToUV(v.vertex.xy);

    #if UNITY_UV_STARTS_AT_TOP
        o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
    #endif


        o.screenUV = o.positionCS.xyw;
        #if UNITY_UV_STARTS_AT_TOP
        o.screenUV.xy = o.screenUV.xy * float2(0.5, -0.5) + 0.5 * o.screenUV.z;
        #else
        o.screenUV.xy = o.screenUV.xy * 0.5 + 0.5 * o.screenUV.z;
        #endif
        return o;
    }
    
    void AtmosphericScatteringCompute(VaryingsDefault input, float3 V, float depth, out float3 color, out float3 opacity)
    {
        PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenParams.zw - 1.0f, depth,
                                                   UNITY_MATRIX_I_VP, //_ScreenSize
                                                   UNITY_MATRIX_V);

        if (depth == UNITY_RAW_FAR_CLIP_VALUE)
        {
            // When a pixel is at far plane, the world space coordinate reconstruction is not reliable.
            // So in order to have a valid position (for example for height fog) we just consider that the sky is a sphere centered on camera with a radius of 5km (arbitrarily chosen value!)
            // And recompute the position on the sphere with the current camera direction.
            posInput.positionWS = GetCurrentViewPosition() - V * _MaxFogDistance;

            // Warning: we do not modify depth values. Use them with care!
        }
// color = posInput.positionWS.zzz;
//         opacity = 1;
        EvaluateAtmosphericScattering(posInput, V, color, opacity); // Premultiplied alpha
    }

    // Generates a world-space view direction for sky and atmospheric effects
    float3 GetSkyViewDirWS(float2 positionCS)
    {
        //TAA 求回对应帧的方向
        //float4 viewDirWS = mul(float4(positionCS.xy + _TaaJitterStrength.xy, 1.0f, 1.0f), _PixelCoordToViewDirWS);
        float4 viewDirWS = mul(float4(positionCS.xy, 1.0f, 1.0f), _PixelCoordToViewDirWS); //UNITY_MATRIX_I_VP);
        return normalize(viewDirWS.xyz);
    }

    float LoadCameraDepth(float3 screenUV)
    {
        // return LOAD_TEXTURE2D_X_LOD(_CameraDepthTexture, pixelCoords, 0).r;
        float2 screen_uv = (screenUV.xy / screenUV.z);
        float d        = SAMPLE_TEXTURE2D_X_LOD(_CameraDepthTexture, my_point_clamp_sampler, screen_uv, 0).x; // raw depth value has UNITY_REVERSED_Z applied on most platforms.
        return d;
    }

    float4 Frag(VaryingsDefault input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        
        float2 positionSS = input.positionCS.xy;
      
        float3 V = GetSkyViewDirWS(positionSS);

        half distanceMask = saturate(V.yyy * 10);
        
        
        float depth = LoadCameraDepth(input.screenUV);
#if UNITY_REVERSED_Z 
    depth = depth;
#else
    // Adjust z to match NDC for OpenGL,手机移动平台上面就需要做这样的处理。
    // UNITY_NEAR_CLIP_VALUE：在Direct3d类平台为0，OpenGL类平台为-1.
    depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, depth);
#endif
        
        float3 volColor, volOpacity;
        AtmosphericScatteringCompute(input, V, depth, volColor, volOpacity);
        
        return float4(volColor, 1.0 - (volOpacity.x));
    }

    ENDHLSL
    SubShader
    {
        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off
            Blend One SrcAlpha,SrcAlpha Zero
//            Blend One Zero
            //Blend One SrcAlpha, Zero One // Premultiplied alpha for RGB, preserve alpha for the alpha channel
            //ZTest Less // Required for XR occlusion mesh optimization

            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment Frag
            ENDHLSL
        }

    }
}