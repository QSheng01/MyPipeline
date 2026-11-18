//2024.4.3 新增乘渐变映射 
//2024.4.9 副纹理混合Add
//2024.4.17 副贴图影响透明度不乘颜色 提纯调整 扰动纹理极坐标问题  全局颜色 全局透明度  溶解边缘透明度控制
//2024.4.28 去色 反向菲涅尔
//2024.5.20 溶解维诺偏移
Shader "ShiYue/Particles/Effect_UniversalV2"
{
    Properties 
    {
        //基础设置
        [Enum(UnityEngine.Rendering.BlendMode)]_BlendModeSrc("混合模式Src",Int) = 5
        [Enum(UnityEngine.Rendering.BlendMode)]_BlendModeDst("混合模式Dst",Int) = 10
        [Enum(UnityEngine.Rendering.CullMode)]_CullMode("剔除模式",Int) = 2
        [Enum(Off,0,On,1)]_ZWrite("深度写入",float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)]_ZTest("深度测试",float) = 2
        _ZOffset ("深度偏移", float) = 0
        [HideInInspector]_BlendTemp("BlendTemp",float) = 0
        [Toggle(_ENABLE_FOG_TRANS)] _enableFog("启用场景雾", int) = 0
        [Toggle] _CloseColor("关闭顶点色", float) = 0

        //色相偏移
        _HUEoffset("色相位移",Range(0,1)) = 0//customdata
        _Saturation("去色",Range(0,1)) = 0

        //软粒子相关
        _Distance("软粒子距离",float) = 0
        [Toggle]_IsDistanceAdd("使用交界叠加",int) = 0
        [HDR]_DistancColor("交界颜色",Color) = (1,1,1,1)

        //主纹理
        [HDR]_MainColor ("主纹理颜色", Color) = (1,1,1,1)
        [HDR]_BackColor ("主纹理背面颜色", Color) = (1,1,1,1)
        _MainTex ("主纹理贴图", 2D) = "white" {}
        [Enum(RGB,0,R,1,G,2,B,3,A,4)]_MainChannel("主纹理颜色通道选择",float) = 0
        [Enum(A,0,R,1,G,2,B,3,None,4)]_MainAlphaChannel("主纹理透明通道选择",float) = 0
        [Enum(UV1,0,UV2,1,ScreenUV,2)]_MainUVChannel("主纹理UV选择",float) = 0
        _MainTex_PannerSpeedU("MainTex_PannerSpeedU",float) = 0
        _MainTex_PannerSpeedV("MainTex_PannerSpeedV",float) = 0
        [Toggle] _MainRota("是否使用主纹理旋转",float) = 0
        [Toggle] _MainClamp("是否使用主纹理阻断",float) = 0
        _Rotation("主纹理旋转角度",float) = 0
        _RotaSpeed("主纹理旋转速度",float) = 0
        _AlphaRemap("透明度衰减",Range(0,1)) = 0//customdata
        _AlphaExp("透明度范围",Range(0.2,8)) = 1//customdata
        _Refine("提纯调整？",vector) = (1,1,1,0)
        _MainCustomOffsetX("主纹理自定义数据偏移X",float) = 0//customdata
        _MainCustomOffsetY("主纹理自定义数据偏移Y",float) = 0//customdata

        
        //副贴图
        [HDR]_SubColor ("副纹理颜色", Color) = (1,1,1,1)
        _SubTex ("副纹理贴图", 2D) = "black" {}
        [Enum(Lerp,0,Multiply,1,Add,2)] _SubBlend("混合模式",float) = 0
        [Toggle]_SubMulAlpha("副纹理影响透明度",float) = 0
        [Enum(A,0,R,1,G,2,B,3,None,4)]_SubLerp("副纹理插值通道选择",float) = 0
        _SubNoiseIntensity("副纹理受扰动影响强度",Range(0,1)) = 0
        _SubTex_PannerSpeedU("SubTex_PannerSpeedU",float) = 0
        _SubTex_PannerSpeedV("SubTex_PannerSpeedV",float) = 0
        [Toggle] _SubRota("是否使用副纹理旋转",float) = 0
        _SubRotation("副纹理旋转角度",float) = 0
        _SubRotaSpeed("副纹理旋转速度",float) = 0
        [Enum(UV1,0,UV2,1,ScreenUV,2)]_SubUVChannel("副纹理UV选择",float) = 0
        [Enum(RGB,0,R,1,G,2,B,3,A,4)]_SubChannel("副纹理颜色通道选择",float) = 0
        _SubCustomOffsetX("副纹理自定义数据偏移X",float) = 0//customdata
        _SubCustomOffsetY("副纹理自定义数据偏移Y",float) = 0//customdata

        
        //渐变映射
        _RampTex("渐变纹理",2D) = "black" {}
        _RampPower("渐变采样偏移",Range(0.2,8)) = 1
        [Enum(UV1,0,UV2,1,ScreenUV,2)]_RampUVChannel("渐变纹理UV选择",float) = 0
        _Ramp_PannerSpeedU("Ramp_PannerSpeedU",float) = 0
        _Ramp_PannerSpeedV("Ramp_PannerSpeedV",float) = 0
        _RampCustomOffsetX("渐变纹理自定义数据偏移X",float) = 0//customdata
        _RampCustomOffsetY("渐变纹理自定义数据偏移Y",float) = 0//customdata
        
        
        //边缘光
        [HDR]_FresnelColor ("边缘光颜色",Color) = (0,0,0,0)
        _FresnelPower("边缘光宽度",float) = 5
        _FresnelSmooth("边缘光光滑度",Range(0,0.49)) = 0
        [Toggle]_OneMinusFresnel("反向菲涅尔",float) = 0
        
        //遮罩
        _MaskTex("MaskTex",2D) = "white"{}
        [Toggle]_MaskUV2("MaskUV2",float) = 0
        [Enum(A,0,R,1,G,2,B,3,None,4)]_MaskTexAlpha("MaskTexAlpha",Int) = 1
        [Enum(A,0,R,1,G,2,B,3,None,4)]_MaskTexSub("MaskTexSub",Int) = 4
        [Enum(A,0,R,1,G,2,B,3,None,4)]_MaskTexNoise("MaskTexNoise",Int) = 4
        [Enum(A,0,R,1,G,2,B,3,None,4)]_MaskTexFresnel("MaskTexFresnel",Int) = 4
        [Toggle] _MaskRota("是否使用遮罩旋转",float) = 0
        _MaskRotation("遮罩旋转角度",float) = 0
        _MaskRotaSpeed("遮罩旋转速度",float) = 0
        _MaskTex_PannerSpeedU("Mask_PannerSpeedU",float) = 0
        _MaskTex_PannerSpeedV("Mask_PannerSpeedV",float) = 0
        
        _MaskCustomOffsetX("遮罩纹理自定义数据偏移X",float) = 0//customdata
        _MaskCustomOffsetY("遮罩纹理自定义数据偏移Y",float) = 0//customdata

        
        //扰动
        _NoiseTex("NoiseTex",2D) = "white"{}
        [Toggle]_NoiseUV2("NoiseUV2",float) = 0
        [Enum(A,0,R,1,G,2,B,3,None,4)]_NoiseTexChannel("NoiseTexChannel",Int) = 1
        _NoiseIntensity("NoiseIntensity",float) = 0.01//customdata
        _NoiseSpeedPower("NoiseSP",Vector) = (0,0,1,1)
        [Toggle]_IsNormalizeNoise("是否归一化",float) = 0 
        _IsOffsetNoise("是否偏移扰动",Range(-3,3)) = 0
        
        _NoiseCustomOffsetX("扰动纹理自定义数据偏移X",float) = 0//customdata
        _NoiseCustomOffsetY("扰动纹理自定义数据偏移Y",float) = 0//customdata

        
        //溶解
        _DissolveTex ("Dissolve_Tex", 2D) = "white" {}
        [Toggle]_DissolveUV2("DissolveUV2",float) = 0
        [Enum(A,0,R,1,G,2,B,3)]_DissolveTexAlpha("DissolveTexAlpha",float) = 1
        _Dissolve_Amount ("Dissolve Amount", Range(0, 1)) = 0//customdata
        _DissolveTex_PannerSpeedU("DissolveTex_PannerSpeedU",float) = 0
        _DissolveTex_PannerSpeedV("DissolveTex_PannerSpeedV",float) = 0
        [HDR]_Dissolve_color ("Dissolve Color", Color) = (0.5,0.5,0.5,1)
        _OutlineIntensity ("Outline Intensity", Range(0,0.5) ) = 0.1
        _BlendIntensity ("Blend Intensity", Range(0,0.5) ) = 0.1
        _DissolveNoise("是否扰动溶解",Range(0,1)) = 0
        
        _VoroiSpeed("噪声角速度",float) = 0.1
        _VoroiTillingX("噪声重复度X",float) = 1
        _VoroiTillingY("噪声重复度Y",float) = 1
        _VoroiOffsetX("噪声偏移X",float) = 0
        _VoroiOffsetY("噪声偏移Y",float) = 0
        
        _DissolveCustomOffsetX("溶解纹理自定义数据偏移X",float) = 0//customdata
        _DissolveCustomOffsetY("溶解纹理自定义数据偏移Y",float) = 0//customdata
        
        _DissolveDirTex("DissolveDirTex", 2D) = "black" {}
        _DissolveDirIntensity("DissolveDirIntensity",Range(0,0.9)) = 0.9
        [Enum(A,0,R,1,G,2,B,3)]_DissolveDirAlpha("DissolveTDirAlpha",float) = 1
        _DissRotation("纹理旋转角度",float) = 0
        

        
        //顶点偏移
        _OffsetInt("OffsetInt",float) = 1//customdata
        _VO_tillingU("VO_tillingU",float) = 1
        _VO_tillingV("_VO_tillingV",float) = 1
        _VO_PannerSpeedU("VO_PannerSpeedU",float) = 0.1
        _VO_PannerSpeedV("VO_PannerSpeedV",float) = 0.1
        _XYZPower("XYZ_Power",Vector) = (1,1,1,0)
        [Toggle]_voUV2("VOuv2",int) = 0
        _VOtext("voText",2D) = "white" {}
        [Enum(A,0,R,1,G,2,B,3,None,4)]_VOChannel("voChannel",float) = 1
        _VOlerp("VOlerp",float) = 0

        _Voroi2Speed("噪声角速度",float) = 0.1

        //FlowMap
        _FlowMap("FlowMap",2D) = "white"{}
        _FlowLerp("FlowLerp", Range(0, 1)) = 0//customdata
        
        //CubeMap
        _CubeMap("CubeMap", CUBE) = "white" {}
		_CubeColor("CubeColor", Color) = (0.5,0.5,0.5,0)
        
        //Global
        [HDR]_GlobalColor("全局颜色",Color)= (1,1,1,1)
        _GlobalAlpha("全局透明度",Range(0,8)) = 1
        
        
        
        
        _Randomoffset("贴图随机偏移",float) = 0//customdata

        [HideInInspector] _ContrastValue0("色相位移",float)=0
        [HideInInspector] _ContrastValue1("透明度衰减",float)=0
        [HideInInspector] _ContrastValue2("透明度范围",float)=0
        [HideInInspector] _ContrastValue3("主纹理偏移X",float)=0
        [HideInInspector] _ContrastValue4("主纹理偏移Y",float)=0
        [HideInInspector] _ContrastValue5("副纹理偏移X",float)=0
        [HideInInspector] _ContrastValue6("副纹理偏移Y",float)=0
        [HideInInspector] _ContrastValue7("遮罩纹理偏移X",float)=0
        [HideInInspector] _ContrastValue8("遮罩纹理偏移Y",float)=0
        [HideInInspector] _ContrastValue9("DissolveAmount",float)=0
        [HideInInspector] _ContrastValue10("NoiseIntensity",float)=0
        [HideInInspector] _ContrastValue11("OffsetInt",float)=0
        [HideInInspector] _ContrastValue12("FlowLerp",float)=0
        [HideInInspector] _ContrastValue13("贴图随机偏移",float)=0
        [HideInInspector] _ContrastValue14("溶解偏移X",float)=0
        [HideInInspector] _ContrastValue15("溶解偏移Y",float)=0
        [HideInInspector] _ContrastValue16("扰动偏移X",float)=0
        [HideInInspector] _ContrastValue17("扰动偏移Y",float)=0
        [HideInInspector] _ContrastValue18("Ramp偏移X",float)=0
        [HideInInspector] _ContrastValue19("Ramp偏移Y",float)=0
        [HideInInspector] _ContrastValue20("全局透明度",float)=0
        
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
		[HideInInspector] _ColorMask ("Color Mask", Float) = 15
        



    }

    SubShader 
    {
        Tags 
        {
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
        Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

        Pass 
        {
            
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            
            Cull  [_CullMode]
            Blend [_BlendModeSrc] [_BlendModeDst]
            ZWrite [_ZWrite]
            ZTest [_ZTest]


            HLSLPROGRAM

            #pragma multi_compile_fog //内置雾
            #pragma shader_feature_local  _ _ENABLE_FOG_TRANS //雾效宏
            #pragma shader_feature_local  _ _Fresnel_ON  //边缘光宏
            #pragma shader_feature_local  _ _CustomData_ON//自定义数据宏
            #pragma shader_feature_local  _ _CubeMap_ON//反射宏
            #pragma shader_feature_local_vertex  _ _VERTEX_OFFSET_ON  //顶点偏移宏
            #pragma shader_feature_local_vertex  _ _VO_VOROI_ON  //顶点维诺宏
            #pragma shader_feature_local_fragment  _ _DISSOLVE_VOROI_ON  //溶解维诺宏 
            #pragma shader_feature_local_fragment  _ _SOFT_PARTICLE_ON//软粒子宏
            #pragma shader_feature_local_fragment  _ _TWOFACECOLOR_ON//双面颜色
            #pragma shader_feature_local_fragment  _ _ROTATION_ON   //旋转宏
            #pragma shader_feature_local_fragment  _ _MASK_ON   //遮罩宏
            #pragma shader_feature_local_fragment  _ _NOISE_ON  //扰动宏
            #pragma shader_feature_local_fragment  _ _DISSOLVE_SOFT   //溶解宏
            #pragma shader_feature_local_fragment  _ _DISSOLVE_DIR   //溶解宏
            #pragma shader_feature_local_fragment  _ _FlowMap_ON //FlowMap宏
            #pragma shader_feature_local_fragment  _ _Sub_ON //副纹理宏
            #pragma shader_feature_local_fragment  _ _MainPolar_ON //主纹理极坐标宏
            #pragma shader_feature_local_fragment  _ _NoisePolar_ON //扰动纹理极坐标宏
            #pragma shader_feature_local_fragment  _ _SubPolar_ON //副纹理极坐标宏
            #pragma shader_feature_local_fragment  _ _Ramp_ON //渐变宏
            #pragma shader_feature_local_fragment  _ _Ramp_Mul_ON //渐变宏
            #pragma shader_feature_local_fragment  _ _HUE_ON  //色相偏移宏


            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"


            #ifdef _SOFT_PARTICLE_ON
            #define REQUIRE_DEPTH_TEXTURE 1 //深度图
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #endif



            TEXTURE2D(_MainTex);	SAMPLER(sampler_MainTex);

            #ifdef _MASK_ON
            TEXTURE2D(_MaskTex);	SAMPLER(sampler_MaskTex);
            #endif

            #ifdef _NOISE_ON
            TEXTURE2D(_NoiseTex);	SAMPLER(sampler_NoiseTex);
            #endif

            #ifdef _DISSOLVE_SOFT
            TEXTURE2D(_DissolveTex);	SAMPLER(sampler_DissolveTex);
            #endif

            #if _Ramp_ON || _Ramp_Mul_ON
            TEXTURE2D(_RampTex);	SAMPLER(sampler_RampTex);
            #endif

            #ifdef _FlowMap_ON
            TEXTURE2D(_FlowMap);	SAMPLER(sampler_FlowMap);
            #endif

            #ifdef _Sub_ON
            TEXTURE2D(_SubTex);	SAMPLER(sampler_SubTex);
            #endif

            #ifdef _VERTEX_OFFSET_ON
            TEXTURE2D(_VOtext);	SAMPLER(sampler_VOtext);
            #endif

            #ifdef _DISSOLVE_DIR
            TEXTURE2D(_DissolveDirTex);	SAMPLER(sampler_DissolveDirTex);
            #endif

            #ifdef _CubeMap_ON
            TEXTURECUBE(_CubeMap);SAMPLER(sampler_CubeMap);;
            #endif


            CBUFFER_START(UnityPerMaterial)

            #ifdef _Fresnel_ON//边缘光
                half4 _FresnelColor;
                half _FresnelPower,_OneMinusFresnel,_FresnelSmooth;
            #endif

            #ifdef _FlowMap_ON//FlowMap
                float4 _FlowMap_ST;
            #endif

            #ifdef _NOISE_ON
                float4 _NoiseTex_ST;
                half4 _NoiseSpeedPower;
                half _NoiseUV2,_NoiseTexChannel,_IsOffsetNoise,_IsNormalizeNoise;//扰动
            #endif

            #ifdef _VERTEX_OFFSET_ON
                float4 _VOtext_ST;
                half _VO_tillingU,_VO_tillingV,_VO_PannerSpeedU,_VO_PannerSpeedV,_voUV2,_VOChannel,_VOlerp;
            #endif



            #ifdef _VO_VOROI_ON
                half _Voroi2Speed;
            #endif

            #ifdef  _SOFT_PARTICLE_ON
                half4 _DistancColor;
                half  _Distance,_IsDistanceAdd;//深度交互
            #endif

            #ifdef _TWOFACECOLOR_ON
                half4 _BackColor;
            #endif

            #ifdef _ROTATION_ON
                half _Rotation,_RotaSpeed,_MainRota,_MaskRota,_MaskRotation,_MaskRotaSpeed,_SubRota,_SubRotation,_SubRotaSpeed;
            #endif

            #ifdef _MASK_ON
                float4 _MaskTex_ST;
                half   _MaskTex_PannerSpeedU,_MaskTex_PannerSpeedV,_MaskTexAlpha,_MaskUV2;
            #endif

            #ifdef _DISSOLVE_SOFT
                float4 _DissolveTex_ST;
                half4 _Dissolve_color;
                half  _DissolveMode,_DissolveTexAlpha,_OutlineIntensity,_DissolveTex_PannerSpeedU,_DissolveTex_PannerSpeedV,_BlendIntensity,_DissolveDirIntensity;
            #endif

            #ifdef _DISSOLVE_DIR
                float4 _DissolveDirTex_ST;
                half _DissolveDirAlpha;
            #endif



            #if _Ramp_ON || _Ramp_Mul_ON
                float4 _RampTex_ST;
                half _RampPower;
            #endif

            #ifdef _Ramp_Mul_ON
                half _RampUVChannel;
                half _Ramp_PannerSpeedU;
                half _Ramp_PannerSpeedV;
            #endif





            #ifdef _DISSOLVE_VOROI_ON
                 half _VoroiSpeed,_VoroiTillingX,_VoroiTillingY,_VoroiOffsetX,_VoroiOffsetY;
            #endif

            #ifdef _Sub_ON
                float4 _SubTex_ST;
                half4 _SubColor;
                half _SubNoiseIntensity,_SubMulAlpha,_SubLerp,_SubTex_PannerSpeedU,_SubTex_PannerSpeedV,_SubBlend,_SubUVChannel,_SubChannel,_SubRamp;
            #endif


            #ifdef _CubeMap_ON
             half4 _CubeColor;
            #endif

            #ifdef _HUE_ON
                half _Saturation;
            #endif

            float4 _MainTex_ST;
            half4 _MainColor,_XYZPower,_Refine,_GlobalColor;

            half _CloseColor,_ZOffset,_MainTex_PannerSpeedU,_MainTex_PannerSpeedV,_MainClamp,_MainChannel,_MainAlphaChannel,_MainUVChannel,_AlphaRemap,_AlphaExp;//主纹理  
            half _FlowLerp,_NoiseIntensity,_OffsetInt,_MaskTexSub,_MaskTexNoise,_MaskTexFresnel,_DissolveUV2,_DissRotation,_DissolveNoise,_Dissolve_Amount,_HUEoffset,_GlobalAlpha;



            CBUFFER_END


            #ifdef _SOFT_PARTICLE_ON
            uniform float4 _CameraDepthTexture_TexelSize;
            #endif

            #ifdef _CustomData_ON
            float _ContrastValue0,_ContrastValue1,_ContrastValue2,_ContrastValue3,_ContrastValue4,_ContrastValue5,_ContrastValue6
            ,_ContrastValue7,_ContrastValue8,_ContrastValue9,_ContrastValue10,_ContrastValue11,_ContrastValue12,_ContrastValue13,_ContrastValue14,_ContrastValue15,_ContrastValue16,_ContrastValue17,_ContrastValue18,_ContrastValue19,_ContrastValue20,
            _Randomoffset,_MaskCustomOffsetX,_MaskCustomOffsetY,_SubCustomOffsetX,_SubCustomOffsetY,_MainCustomOffsetX,_MainCustomOffsetY,_DissolveCustomOffsetX,_DissolveCustomOffsetY,_NoiseCustomOffsetX,_NoiseCustomOffsetY,_RampCustomOffsetX,_RampCustomOffsetY
           ;
            #endif
            

            struct Attributes 
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 vertexColor : COLOR;
                
                #if _Fresnel_ON || _VERTEX_OFFSET_ON || _CubeMap_ON
                float3 vertexNormal : NORMAL;
                #endif

                
                #ifdef _CustomData_ON
                float4 customData1:TEXCOORD1;
                float4 customData2:TEXCOORD2;
                #endif

                float2 uv2: TEXCOORD3;
            };

            struct Varyings 
            {
                float4 pos : SV_POSITION;
                float4 uv : TEXCOORD0;
                half4 vertexColor : COLOR;

                #ifdef _CustomData_ON
                float4 customData1:TEXCOORD1;
                float4 customData2:TEXCOORD2;
                #endif
                
                #ifdef _ENABLE_FOG_TRANS
                half   fogFactor  : TEXCOORD4;
                #endif
                
                #if _Fresnel_ON || _CubeMap_ON
                   float3 worldPos : TEXCOORD3;
                   float3 worldNormal : TEXCOORD6;
                #endif
                
                float4 screenPos : TEXCOORD5;

               
 
            };

            //顶点偏移计算噪声    
            float2 randomVec(float2 noiseuv)
            {
                half vec = dot(noiseuv, float2(127.1f, 311.7f));
                return -1.0f + 2.0f * frac(sin(vec) * 43758.5453123f);
            }

            half perlinNoise(float2 noiseuv) 
            {				
                float2 pi = floor(noiseuv);
                float2 pf = noiseuv - pi;
                float2 w = pf * pf * (3.0f - 2.0f *  pf);

                float2 lerp1 = lerp(
                    dot(randomVec(pi + float2(0.0f, 0.0f)), pf - float2(0.0f, 0.0f)),
                    dot(randomVec(pi + float2(1.0f, 0.0f)), pf - float2(1.0f, 0.0f)), w.x);
                            
                float2 lerp2 = lerp(
                    dot(randomVec(pi + float2(0.0f, 1.0f)), pf - float2(0.0f, 1.0f)),
                    dot(randomVec(pi + float2(1.0f, 1.0f)), pf - float2(1.0f, 1.0f)), w.x);
                    
                return lerp(lerp1, lerp2, w.y).r;
            }


            half3 HSVToRGB( half3 c )
            {
                float4 K = float4( 1.0f, 2.0f / 3.0f, 1.0f / 3.0f, 3.0f );
                float3 p = abs( frac( c.xxx + K.xyz ) * 6.0f - K.www );
                return c.z * lerp( K.xxx, saturate( p - K.xxx ), c.y );
            }
            
            half3 RGBToHSV(half3 c)
            {
                float4 K = float4(0.0f, -1.0f / 3.0f, 2.0f / 3.0f, -1.0f);
                float4 p = lerp( float4( c.bg, K.wz ), float4( c.gb, K.xy ), step( c.b, c.g ) );
                float4 q = lerp( float4( p.xyw, c.r ), float4( c.r, p.yzx ), step( p.x, c.r ) );
                float d = q.x - min( q.w, q.y );
                float e = 1.0e-10;
                return float3( abs(q.z + (q.w - q.y) / (6.0f * d + e)), d / (q.x + e), q.x);
            }


            //UV旋转
            float2 RotationUV(float2 uv,float angle,float speed,float time)
            {
                float sinNum = sin(angle + speed * time);
                float cosNum = cos(angle + speed * time);
                float2 rotaUV = mul(uv - 0.5f , float2x2(cosNum,-sinNum,sinNum,cosNum)) + 0.5;
                return  rotaUV;
            }

            inline half3 GetChannelXYZ(half4 mask1, int ChannelId)
            {
                    const half3 CHANNEL[5] =
                    {
                            {mask1.x, mask1.y, mask1.z},
                            {mask1.x, mask1.x, mask1.x},
                            {mask1.y, mask1.y, mask1.y},
                            {mask1.z, mask1.z, mask1.z},
                            {mask1.w, mask1.w, mask1.w},

                    };

                    return CHANNEL[ChannelId];
                    
            }


            inline float2 GetChannelUV(float2 uv1,float2 uv2,float2 screenuv, int ChannelId)
            {
                    const float2 CHANNEL[3] =
                    {
                                        {uv1},
                                        {uv2},
                                        {screenuv},
                    };

                    return CHANNEL[ChannelId];
                    
            }

            inline half3 GetChannelBlend(half3 lerpColor,half3 mulColor,half3 addColor, int ChannelId)
            {
                    const half3 CHANNEL[3] =
                    {
                                        {lerpColor},
                                        {mulColor},
                                        {addColor},
                    };

                    return CHANNEL[ChannelId];
                    
            }

            inline half GetChannelMask(half4 maskColor, int ChannelId)
            {
                    const half CHANNEL[5] =
                    {
                                        {maskColor.a},
                                        {maskColor.r},
                                        {maskColor.g},
                                        {maskColor.b},
                                        {1.0f},

                    };

                    return CHANNEL[ChannelId];
                    
            }

            //计算维诺图
            float2 voronoihash16( float2 p )
			{
				
				p = float2( dot( p, float2( 127.1f, 311.7f ) ), dot( p, float2( 269.5f, 183.3f ) ) );
				return frac( sin( p ) *43758.5453f);
			}
	
			float voronoi16( float2 v, float time )
			{
				float2 n = floor( v );
				float2 f = frac( v );
				float F1 = 8.0f;
				float F2 = 8.0f; 
				for ( int j = -1; j <= 1; j++ )
				{
					for ( int i = -1; i <= 1; i++ )
					{
						float2 g = float2( i, j );
						float2 o = voronoihash16( n + g );
						o = ( sin( time + o * 6.2831f ) * 0.5f + 0.5f ); float2 r = f - g - o;
						float d = 0.5f * dot( r, r );
						if( d<F1 ) {
							F2 = F1;
							F1 = d; 
						} else if( d<F2 ) {
							F2 = d;
				
						}
					}
				}
				return F1;
			}



            Varyings vert (Attributes input) 
            {

                Varyings output = (Varyings)0;

                #ifdef _CustomData_ON
                
                output.customData1=input.customData1;
                output.customData2=input.customData2;
                
                
                
                
                
                #endif


                output.uv = float4(input.uv,input.uv2);

                //顶点偏移
                #ifdef _VERTEX_OFFSET_ON
                float time = fmod(_Time.y,2e5);
                
                float2 noiseUV = float2(input.positionOS.x+input.positionOS.z,input.positionOS.y);
                noiseUV = noiseUV * float2(_VO_tillingU,_VO_tillingV) + time * float2(_VO_PannerSpeedU,_VO_PannerSpeedV);
                
                float VOnoise = 1.0f;
                float votexNoise = 1.0f;
                

                //计算维诺噪声
                #ifdef _VO_VOROI_ON
                {
                    half timevoroi = _Voroi2Speed * _TimeParameters.x;
                    VOnoise = voronoi16(noiseUV,timevoroi);
                }
                #else
                {
                     VOnoise = perlinNoise(noiseUV);

                     float2 vouv = lerp(input.uv,input.uv2,_voUV2);
                     float2 uv5 = vouv * float2(_VO_tillingU,_VO_tillingV) + time * float2(_VO_PannerSpeedU,_VO_PannerSpeedV);
                     float2 votexUV = TRANSFORM_TEX(uv5, _VOtext);
                     float4 texvo = SAMPLE_TEXTURE2D_LOD(_VOtext,sampler_VOtext,votexUV,0);

                     votexNoise =  GetChannelMask(texvo,_VOChannel);
                }
                #endif
                
                VOnoise = lerp(VOnoise,votexNoise,_VOlerp);

                
                #ifdef _CustomData_ON

                float valueTemp[9];
                valueTemp[0]=_OffsetInt;
                valueTemp[1]=input.customData1.x;
                valueTemp[2]=input.customData1.y;
                valueTemp[3]=input.customData1.z;
                valueTemp[4]=input.customData1.w;
                valueTemp[5]=input.customData2.x;
                valueTemp[6]=input.customData2.y;
                valueTemp[7]=input.customData2.z;
                valueTemp[8]=input.customData2.w;
                _OffsetInt=valueTemp[(int)_ContrastValue11];
                
                #endif
                
                
                float3 vertexValue = input.vertexNormal * VOnoise * _OffsetInt * _XYZPower.xyz;//涉及CD计算  _OffsetInt
                input.positionOS += vertexValue;
                
                #endif

                
                output.vertexColor = input.vertexColor;
                float3 positionWS = TransformObjectToWorld(input.positionOS);
                float4 positionCS = TransformWorldToHClip(positionWS);
                positionCS.z += _ZOffset * 0.01f;
                output.pos = positionCS;

                //雾效宏
                #ifdef _ENABLE_FOG_TRANS
                half fogFactor = ComputeFogFactor(positionCS.z);
				output.fogFactor = fogFactor;
                #endif

                float4 screenPos = ComputeScreenPos(positionCS);
				output.screenPos = screenPos;

                //边缘光
                #if _Fresnel_ON || _CubeMap_ON
                {
                    output.worldNormal = TransformObjectToWorldNormal(input.vertexNormal);
                    output.worldPos = positionWS;
                }
                #endif
                return output;
            }




            half4 frag(Varyings input, bool vface : SV_IsFrontFace) : SV_TARGET
            {


                #ifdef _CustomData_ON
                float valueTemp[9];
                valueTemp[0]=_HUEoffset;
                valueTemp[1]=input.customData1.x;
                valueTemp[2]=input.customData1.y;
                valueTemp[3]=input.customData1.z;
                valueTemp[4]=input.customData1.w;
                valueTemp[5]=input.customData2.x;
                valueTemp[6]=input.customData2.y;
                valueTemp[7]=input.customData2.z;
                valueTemp[8]=input.customData2.w;

                
                _HUEoffset=valueTemp[(int)_ContrastValue0 ];
                valueTemp[0]=_AlphaRemap;
                _AlphaRemap=valueTemp[(int)_ContrastValue1 ];
                valueTemp[0]=_AlphaExp;
                _AlphaExp=valueTemp[(int)_ContrastValue2 ];
                valueTemp[0]=_MainCustomOffsetX;
                _MainCustomOffsetX=valueTemp[(int)_ContrastValue3 ];
                valueTemp[0]=_MainCustomOffsetY;
                _MainCustomOffsetY=valueTemp[(int)_ContrastValue4 ];
                valueTemp[0]=_SubCustomOffsetX;
                _SubCustomOffsetX=valueTemp[(int)_ContrastValue5 ];
                valueTemp[0]=_SubCustomOffsetY;
                _SubCustomOffsetY=valueTemp[(int)_ContrastValue6 ];
                valueTemp[0]=_MaskCustomOffsetX;
                _MaskCustomOffsetX=valueTemp[(int)_ContrastValue7 ];
                valueTemp[0]=_MaskCustomOffsetY;
                _MaskCustomOffsetY=valueTemp[(int)_ContrastValue8 ];
                valueTemp[0]=_Dissolve_Amount;
                _Dissolve_Amount=valueTemp[(int)_ContrastValue9 ];
                valueTemp[0]=_NoiseIntensity;
                _NoiseIntensity=valueTemp[(int)_ContrastValue10 ];
                valueTemp[0]=_FlowLerp;
                _FlowLerp = valueTemp[(int)_ContrastValue12 ];
                valueTemp[0]=_Randomoffset;
                _Randomoffset=valueTemp[(int)_ContrastValue13 ];
                valueTemp[0]=_DissolveCustomOffsetX;
                _DissolveCustomOffsetX=valueTemp[(int)_ContrastValue14 ];
                valueTemp[0]=_DissolveCustomOffsetY;
                _DissolveCustomOffsetY=valueTemp[(int)_ContrastValue15 ];
                valueTemp[0]=_NoiseCustomOffsetX;
                _NoiseCustomOffsetX=valueTemp[(int)_ContrastValue16 ];
                valueTemp[0]=_NoiseCustomOffsetY;
                _NoiseCustomOffsetY=valueTemp[(int)_ContrastValue17 ];
                valueTemp[0]=_RampCustomOffsetX;
                _RampCustomOffsetX=valueTemp[(int)_ContrastValue18 ];
                valueTemp[0]=_RampCustomOffsetY;
                _RampCustomOffsetY=valueTemp[(int)_ContrastValue19 ];
                valueTemp[0]=_GlobalAlpha;
                _GlobalAlpha=valueTemp[(int)_ContrastValue20 ];
                
                #endif
                


                

                
                //通用准备
                float time = fmod(_Time.y,2e5);
                float4 screenPos = input.screenPos;
				float4 screenPosNorm = screenPos / screenPos.w;
				screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? screenPosNorm.z : screenPosNorm.z * 0.5 + 0.5;
                half noiseChannel = 0;
                float2 normalizeUV = 0;
                float3 worldPos = 0;
                float3 worldViewDir= 0;
                float3 worldNormal = 0;

                //主纹理UV选择
                float2 mainuv = GetChannelUV(input.uv.xy,input.uv.zw,screenPosNorm.xy,_MainUVChannel);
                

                //主纹理旋转
                #ifdef _ROTATION_ON
                {
                    float2 mainRotaUV = RotationUV(mainuv,_Rotation,_RotaSpeed,time);
                    mainuv = lerp(mainuv,mainRotaUV,_MainRota);
                }
                #endif

                //遮罩
                half4 maskTex = 1.0f;
                #ifdef _MASK_ON
                {

                    float2 uv_mask = lerp(input.uv.xy,input.uv.zw,_MaskUV2);

                     // #ifdef _CustomData_ON
                     //    uv_mask += _Randomoffset;  //涉及CD计算 _Randomoffset
                     // #endif

                    //遮罩旋转
                    #ifdef _ROTATION_ON
                    {
                        float2 MaskrotaUV = RotationUV(uv_mask,_MaskRotation,_MaskRotaSpeed,time);
                        uv_mask = lerp(uv_mask,MaskrotaUV,_MaskRota);
                    }
                    #endif

                    uv_mask += time * float2(_MaskTex_PannerSpeedU,_MaskTex_PannerSpeedV) ;

                     #ifdef _CustomData_ON
                    {
                       uv_mask += float2(_MaskCustomOffsetX,_MaskCustomOffsetY);//涉及CD计算 _MaskCustomOffsetX _MaskCustomOffsetY
                    }
                    #else
                    {
                       uv_mask += time * float2(_MaskTex_PannerSpeedU,_MaskTex_PannerSpeedV) ;
                    }

                     #endif

                    
                    float2 colMask_uv = TRANSFORM_TEX(uv_mask, _MaskTex);
                    maskTex = SAMPLE_TEXTURE2D(_MaskTex,sampler_MaskTex,colMask_uv);

                }
                #endif

                //溶解UV 准备
                #ifdef _DISSOLVE_SOFT
                    float2 dissolveUV = lerp(input.uv.xy,input.uv.zw,_DissolveUV2)  ;
                
                    #ifdef _DISSOLVE_VOROI_ON
                        dissolveUV *= float2(_VoroiTillingX,_VoroiTillingY);
                        dissolveUV += float2(_VoroiOffsetX,_VoroiOffsetY);
                    #endif


                     #ifdef _CustomData_ON
                        dissolveUV += _Randomoffset;//涉及CD计算  _Randomoffset
                        dissolveUV += float2(_DissolveCustomOffsetX,_DissolveCustomOffsetY);
                     #endif

                #endif


                //主纹理极坐标
                #ifdef _MainPolar_ON
                {
                    //极坐标 29math
                    float2 CenterUV = mainuv - float2(0.5f,0.5f);
                    float2 PolarUV = 0;

                    #ifdef _CustomData_ON
                    {
                        PolarUV =  float2(length(CenterUV)  * 2.0f  + time * _MainTex_PannerSpeedU +  _MainCustomOffsetX , (atan2(CenterUV.x,CenterUV.y) + PI) * (1.0f / TWO_PI) + time * _MainTex_PannerSpeedV + _MainCustomOffsetY);
                    }
                    #else
                    {
                        PolarUV =  float2(length(CenterUV)  * 2.0f  + time * _MainTex_PannerSpeedU  , (atan2(CenterUV.x,CenterUV.y) + PI) * (1.0f / TWO_PI) + time * _MainTex_PannerSpeedV);
                    }
                    #endif

                    
                    mainuv = PolarUV;

                }
                #else
                {

                    mainuv += time * float2(_MainTex_PannerSpeedU,_MainTex_PannerSpeedV);

                    #ifdef _CustomData_ON
                    {
                         mainuv += float2(_MainCustomOffsetX,_MainCustomOffsetY);//涉及CD计算  _MainCustomOffsetX  _MainCustomOffsetY
                    }
                    #endif


                }
                #endif


                //Flowmap
                #ifdef _FlowMap_ON
                {
                    float2 flowmapUV = saturate(TRANSFORM_TEX(input.uv.xy, _FlowMap));
                    half2 flowmap = SAMPLE_TEXTURE2D(_FlowMap,sampler_FlowMap,flowmapUV).rg;
                    half flowmapCDlerp = saturate(_FlowLerp );//涉及CD计算  _FlowLerp
                    mainuv = lerp(mainuv,flowmap,flowmapCDlerp);

                    #ifdef _DISSOLVE_SOFT
                    dissolveUV = lerp (dissolveUV,flowmap,flowmapCDlerp);
                    #endif

                }
                #endif


                //扰动
                #ifdef _NOISE_ON
                {
                    float2 noiseuv = lerp(input.uv.xy,input.uv.zw,_NoiseUV2);

                    #ifdef _CustomData_ON
                        noiseuv += _Randomoffset;//涉及CD计算
                        noiseuv += float2(_NoiseCustomOffsetX,_NoiseCustomOffsetY);
                    #endif

                    
                    //扰动极坐标
                    #ifdef _NoisePolar_ON
                    {
                        //极坐标 29math
                        float2 CenterUV = noiseuv - half2(0.5f,0.5f);
                        
                        float2 PolarUV =  float2(length(CenterUV)  * 2.0f  + time * _NoiseSpeedPower.x  , (atan2(CenterUV.x,CenterUV.y) + PI) * (1.0 / TWO_PI) + time * _NoiseSpeedPower.y);
                        noiseuv = PolarUV;

                    }
                    #else
                    {
                        noiseuv += time * float2(_NoiseSpeedPower.x,_NoiseSpeedPower.y);
                        noiseuv = TRANSFORM_TEX(noiseuv, _NoiseTex);
                    }
                    #endif

                    
                    normalizeUV =  lerp( 1.0f,normalize(input.uv.xy * 2.0f - 1.0f),_IsNormalizeNoise);
                    noiseuv = TRANSFORM_TEX(noiseuv, _NoiseTex);
                    half4 NoiseTex =  SAMPLE_TEXTURE2D(_NoiseTex,sampler_NoiseTex,noiseuv)  ;

                    #ifdef _MASK_ON
                    {
                        half maskNoiseChannel = GetChannelMask(maskTex,_MaskTexNoise);
                        NoiseTex *= maskNoiseChannel;

                    }
                    #endif


                           
                    half noisePower = _NoiseIntensity ;  //涉及CD计算  _NoiseIntensity

                    NoiseTex =  ((NoiseTex * _IsOffsetNoise - _IsOffsetNoise) + NoiseTex )* noisePower *0.1f  ;

                    noiseChannel = GetChannelMask(NoiseTex,_NoiseTexChannel);
                    mainuv += noiseChannel.xx * normalizeUV * _NoiseSpeedPower.zw;


                    #ifdef _DISSOLVE_SOFT
                    dissolveUV += noiseChannel.xx * normalizeUV * _DissolveNoise * _NoiseSpeedPower.zw;
                    #endif


                    
                }
                #endif

                //主纹理
                float2 colMainTex_UV = TRANSFORM_TEX(mainuv, _MainTex);
                colMainTex_UV = lerp(colMainTex_UV,saturate(colMainTex_UV),_MainClamp);
                half4 colMainTex = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,colMainTex_UV);
                half3 colMain = GetChannelXYZ(colMainTex,_MainChannel);
                half colMainA = GetChannelMask(colMainTex,_MainAlphaChannel);
                

                #ifdef _TWOFACECOLOR_ON
                float4 facecolor = (vface)?(_MainColor):(_BackColor);
                half3 col = colMain * facecolor.rgb;
                half alpha = saturate(colMainA * facecolor.a);
                #else
                half3 col = colMain * _MainColor.rgb;
                half alpha = saturate(colMainA * _MainColor.a);
                #endif


                
                //half alpha = saturate(colMainA * _MainColor.a);
                alpha = saturate(alpha - _AlphaRemap  ) ;//涉及CD计算  _AlphaRemap
                alpha = saturate(1.0f - pow(1.0f - alpha,_AlphaExp));//涉及CD计算  _AlphaExp

                half4 finalColor = half4(col,alpha);

                #ifdef _Sub_ON
                {
                    float2 Subuv = GetChannelUV(input.uv.xy,input.uv.zw,screenPosNorm.xy,_SubUVChannel);

                    #ifdef _ROTATION_ON
                    {
                        float2 SubRotaUV = RotationUV(Subuv,_SubRotation,_SubRotaSpeed,time);
                        Subuv = lerp(Subuv,SubRotaUV,_SubRota);
                    }
                    #endif


                    #ifdef _SubPolar_ON
                    {
                        //极坐标 29math
                        float2 CenterUV = Subuv - half2(0.5f,0.5f);
                        float2 PolarUV =  float2(length(CenterUV)  * 2.0f  + time * _SubTex_PannerSpeedU  , (atan2(CenterUV.x,CenterUV.y) + PI) * (1.0f / TWO_PI) + time * _SubTex_PannerSpeedV);
                        Subuv = PolarUV;

                    }
                    #else
                    {
                        Subuv += time * float2(_SubTex_PannerSpeedU,_SubTex_PannerSpeedV);
                    }
                    #endif


                    #ifdef _NOISE_ON
                    {
                        Subuv +=  noiseChannel.xx * normalizeUV * _SubNoiseIntensity * _NoiseSpeedPower.zw;

                    }
                    #endif

                    



                    #ifdef _CustomData_ON
                        Subuv += float2(_SubCustomOffsetX,_SubCustomOffsetY);//涉及CD计算
                    #endif

                    float2 colSub_uv = TRANSFORM_TEX(Subuv, _SubTex);
                    
                    half4 colSubTex = SAMPLE_TEXTURE2D(_SubTex,sampler_SubTex,colSub_uv);
                    half sublerp = GetChannelMask(colSubTex,_SubLerp);
                    colSubTex.xyz = GetChannelXYZ(colSubTex,_SubChannel);
                    half3 subcol = colSubTex.rgb * _SubColor.rgb; 
                   



                    #ifdef _MASK_ON
                    {
                        half maskSubChannel = GetChannelMask(maskTex,_MaskTexSub);
                        half3 subMul = lerp(finalColor.rgb,finalColor.rgb * subcol,maskSubChannel);
                        half3 subBlendLerp = lerp(finalColor.rgb, subcol * finalColor.a,sublerp * maskSubChannel);
                        half3 subAdd = finalColor.rgb + subcol * maskSubChannel;
                        finalColor.rgb = GetChannelBlend(subBlendLerp,subMul,subAdd,_SubBlend);
                        finalColor.a = GetChannelBlend(finalColor.aaa,finalColor.aaa,subcol.rrr + finalColor.aaa,_SubBlend).r;
                        finalColor.a *= lerp(1,subcol.r,_SubMulAlpha );
                        finalColor.a = saturate(finalColor.a);
                        

                    }
                    #else
                    {
                        half3 subMul = lerp(finalColor.rgb * subcol,finalColor.rgb ,_SubMulAlpha) ;
                        half3 subBlendLerp = lerp(finalColor.rgb, subcol * finalColor.a,sublerp);
                        half3 subAdd = finalColor.rgb + subcol;
                        finalColor.rgb = GetChannelBlend(subBlendLerp,subMul,subAdd,_SubBlend);
                        finalColor.a = GetChannelBlend(finalColor.aaa,finalColor.aaa,subcol.rrr + finalColor.aaa,_SubBlend).r;
                        finalColor.a *= lerp(1,subcol.r,_SubMulAlpha);
                        finalColor.a = saturate(finalColor.a);
                    }
                    #endif

                }
                #endif

                #if _Fresnel_ON || _CubeMap_ON
                {
                    worldPos = input.worldPos;
                    worldViewDir= normalize(_WorldSpaceCameraPos.xyz - worldPos);
                    worldNormal = input.worldNormal;
                }
                #endif


                #ifdef _Fresnel_ON
                {
                    //边缘光 19math
                    half ndotv = dot(worldNormal,worldViewDir);
                    half fresnel = pow(max(1.0f - ndotv,0.0001f),_FresnelPower);

                    #ifdef _MASK_ON
                    {
                        half maskFresnelChannel =GetChannelMask(maskTex,_MaskTexFresnel);
                        fresnel *=maskFresnelChannel;
                    }
                    #endif

                    half OneMinusFresnel = saturate(abs(1.0f - fresnel));
                    OneMinusFresnel = smoothstep(_FresnelSmooth,1.0f -_FresnelSmooth,OneMinusFresnel);
                    fresnel = smoothstep(_FresnelSmooth,1.0f-_FresnelSmooth,fresnel);


                    finalColor.xyz += fresnel * _FresnelColor.xyz * (1.0f - _OneMinusFresnel);
                    finalColor.a = saturate(lerp(finalColor.a + fresnel , finalColor.a + OneMinusFresnel * finalColor.a ,_OneMinusFresnel) );

                    half4 addfresnel = half4(lerp(finalColor.xyz,_FresnelColor.xyz,fresnel),finalColor.a);
                    half4 omfresnel = lerp(finalColor,finalColor * OneMinusFresnel,_OneMinusFresnel);
                    finalColor = lerp(addfresnel,omfresnel,_OneMinusFresnel);
                    
                }
                #endif

       

                #ifdef  _CubeMap_ON
                 float3 cubeUV =    reflect( -worldViewDir , worldNormal );

                    #ifdef _NOISE_ON
                         cubeUV.xy +=  noiseChannel.xx * normalizeUV; 
                    #endif

                 half3 cubeColor =  saturate(SAMPLE_TEXTURECUBE(_CubeMap,sampler_CubeMap,cubeUV).rgb) * _CubeColor.rgb;
                 finalColor.rgb +=  cubeColor.rgb;

                #endif


                

                #ifdef _DISSOLVE_SOFT
                {

                    half dirtongdao = 0;
                    dissolveUV  +=  time * float2(_DissolveTex_PannerSpeedU,_DissolveTex_PannerSpeedV) ;
                    #ifdef  _DISSOLVE_DIR
                    float2 dissDirUV = lerp(input.uv.xy,input.uv.zw,_DissolveUV2);
                    dissDirUV = TRANSFORM_TEX(dissDirUV,_DissolveDirTex);;

                    dissDirUV = RotationUV(dissDirUV,_DissRotation,0,time);
                    
                    half4 dissdir = SAMPLE_TEXTURE2D(_DissolveDirTex,sampler_DissolveDirTex,dissDirUV) * _DissolveDirIntensity;
                    dirtongdao  = GetChannelMask (dissdir,_DissolveDirAlpha);

                    #endif

                    dissolveUV = RotationUV(dissolveUV,_DissRotation,0,time);
                    

                    half DissolveAmount = (_Dissolve_Amount  + _CloseColor*(1-input.vertexColor.a))*(1 + _DissolveDirIntensity) ; //涉及CD计算  _Dissolve_Amount
                    DissolveAmount *= 1.05f + _OutlineIntensity;


                    #ifdef _DISSOLVE_VOROI_ON
                    half timevoroi = _VoroiSpeed * _TimeParameters.x;
                    half4 colDissovleTex = voronoi16(dissolveUV,timevoroi).xxxx;
                    #else
                         half4 colDissovleTex = SAMPLE_TEXTURE2D(_DissolveTex,sampler_DissolveTex,TRANSFORM_TEX(dissolveUV, _DissolveTex));
                    #endif


                    half tongdao  = GetChannelMask (colDissovleTex,_DissolveTexAlpha);
                    _BlendIntensity = _BlendIntensity + 0.0001;
                    half slope = 1.0f / (_BlendIntensity);
                    half range = _BlendIntensity + lerp(0.0f, 1.0f - _BlendIntensity - _OutlineIntensity,tongdao + dirtongdao) - DissolveAmount;
                    half alphaRight = saturate((range + _OutlineIntensity) * slope);
                    half alphaLeft = saturate(range * slope);
                    half edgeAlpha = 1 - alphaLeft;

                    half3 emissive = alphaLeft * finalColor.rgb;
                    half3 finalColor_rgb = emissive + edgeAlpha * _Dissolve_color.rgb *_Dissolve_color.a ;
                    finalColor = half4 (finalColor_rgb,saturate(alphaRight * finalColor.a ));
                    
                }
                #endif


                #ifdef _MASK_ON
                {
                    half maskAlphaChannel = GetChannelMask(maskTex,_MaskTexAlpha);
                    finalColor.a *= maskAlphaChannel;
                }
                #endif


                finalColor = lerp(finalColor * input.vertexColor,finalColor,_CloseColor) ;


                #ifdef _SOFT_PARTICLE_ON
				half screenDepth = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( screenPosNorm.xy ),_ZBufferParams);
				half distanceDepth = abs( ( screenDepth - LinearEyeDepth( screenPosNorm.z,_ZBufferParams ) ) / ( _Distance ) );
                half OMdistanceDepth = saturate(1.0f - distanceDepth);
                half4 DistanceAddColor =  half4(lerp(finalColor.rgb,_DistancColor.rgb,OMdistanceDepth),finalColor.a);
                half4 softColor = finalColor * saturate(distanceDepth);
                finalColor = lerp(softColor,DistanceAddColor,_IsDistanceAdd);
                #endif

                



                #ifdef _Ramp_ON
                    float2 RampUV = saturate(pow(abs(float2(finalColor.a,0.0f)) ,_RampPower));
                    RampUV = TRANSFORM_TEX(RampUV, _RampTex);
                    half3 Rampcolor = SAMPLE_TEXTURE2D(_RampTex,sampler_RampTex,RampUV).rgb * _MainColor.rgb ;
                    finalColor.rgb = Rampcolor;
                #endif
                    

                #ifdef _Ramp_Mul_ON
                    float2 RampUV =  GetChannelUV(input.uv.xy,input.uv.zw,screenPosNorm.xy,_RampUVChannel);
                    RampUV = TRANSFORM_TEX(RampUV, _RampTex) + float2(_Ramp_PannerSpeedU,_Ramp_PannerSpeedV) *time;

                    #ifdef _CustomData_ON
                        RampUV += float2(_RampCustomOffsetX,_RampCustomOffsetY);
                    #endif

                
                    half3 Rampcolor = SAMPLE_TEXTURE2D(_RampTex,sampler_RampTex,RampUV).rgb;
                    finalColor.rgb *= Rampcolor; 
                #endif


                finalColor.rgb = lerp(finalColor.rgb * _Refine.x,pow(abs(finalColor.rgb),max(_Refine.z,0.01)) *_Refine.y,_Refine.w );
                

                #ifdef _HUE_ON
                {
                    half3 toHSV = RGBToHSV( finalColor.rgb );
                    half3 toRGB = HSVToRGB( half3(( toHSV.x  + _HUEoffset ),toHSV.y  ,toHSV.z) ); //涉及CD计算   _HUEoffset
                    finalColor.rgb = toRGB;

                    half grey = Luminance(finalColor.rgb);
                    finalColor.rgb = lerp(finalColor.rgb, half3(grey, grey, grey), _Saturation);
                }
                #endif


                #ifdef _ENABLE_FOG_TRANS
                finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);
                #endif


                finalColor *=  _GlobalColor;
                finalColor.a = saturate(finalColor.a * _GlobalAlpha);


                return half4(finalColor);
            }
            ENDHLSL
        }
    }
    CustomEditor "EffectUniversalV2_GUI"
}
