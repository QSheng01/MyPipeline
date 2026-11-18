 Shader "ShiYue/URP/Sky/SimpleSky_TOD_V2"
{
    Properties
    {
        [CustomGroup] _SkySetting("天空颜色设置",float) = 1.0
        [CustomObject(_SkySetting)][HDR]                            _SkyTopColor("顶部天空颜色", Color) = (0.2,0.4,0.6,1)
        [CustomObject(_SkySetting)][HDR]                            _SkyBottomColor("中部便顶天空颜色", Color) = (0.3,0.35,0.4,1)
        [CustomObject(_SkySetting)]                                 _GroundColor("地面颜色", Color) = (0,0,0,0)
        [CustomObject(_SkySetting)]                                 _GroundPositionOffset ("地面位置偏移", Range(-1,1)) = 0.9
        [CustomObject(_SkySetting)]                                 _GroundPositionScale ("地面位置宽度", float) = 1.5
        [CustomObject(_SkySetting)]                                 _HeightFogHeight("高度雾高度", Float) = 1
        [CustomObject(_SkySetting)]                                 _AirScatteringHeight ("大气散射高度", float) = 1

        [CustomObject(_SkySetting)]                                 _Azure_FogScatteringScale ("散射强度", float) = 1
        [CustomObject(_SkySetting)]                                 _Azure_Scattering ("_Azure_Scattering", float) = 15
        [CustomObject(_SkySetting)]                                 _Azure_Kr ("Rayleigh散射海拔高度", float) = 8400
        [CustomObject(_SkySetting)]                                 _Azure_Km ("Mie散射海拔高度", float) = 1200
        [CustomObject(_SkySetting)]                                 _Azure_Rayleigh ("Rayleigh散射程度", Vector) = (1,1,1,1)
        [CustomObject(_SkySetting)]                                 _Azure_Mie ("Mie散射程度", Vector) = (1,1,1,1)
        [CustomObject(_SkySetting)]                                 _MieG ("Mie各项异性", float) = 0.76
        
        [CustomGroup] _EdgeSetting("交际线设置",float) = 1.0
//        [CustomObject(_EdgeSetting)]                                _HorizonOffset("交际线偏移", Range(0,0.85)) = 0
        [CustomObject(_EdgeSetting)][HDR]                           _MieScatteringColor("米氏散射颜色", Color) = (1,1,1,1)
        [CustomObject(_EdgeSetting)][HDR]                           _RayleighScatteringColor("瑞利散射颜色", Color) = (1,1,1,1)
        
        [CustomGroup] _SunMoonSetting("日月设置",float) = 1.0
        [CustomKeywordEnum(_SunMoonSetting,_DAY,_NIGHT)]            _IsNight("昼夜设置", Float) = 0
        [CustomObject(_SunMoonSetting)][HDR]                        _SunColor("太阳颜色", Color) = (1,1,1,1)
        [CustomObject(_SunMoonSetting)]                             _SunRadius("太阳大小", Range(0.1, 1)) = 0.5
        [CustomObject(_SunMoonSetting)][HDR][Gamma]                 _SunHalo("光晕颜色", Color) = (0,0,0,1)
        [CustomObject(_SunMoonSetting)]                             _SunHaloExponent ("光晕大小", float) = 125
        [CustomObject(_SunMoonSetting)]                             _SunScatterIntensity ("光晕强度", float) = 1
        [CustomObject(_SunMoonSetting)]                             _Azure_ScatteringColor ("太阳散射颜色", Color) = (1,1,1,1)
        
        [CustomTexture(_SunMoonSetting)]                            _MoonTex("月亮贴图(A:不透明度)", 2D) = "black" {}
        [CustomObject(_SunMoonSetting)]                             _MoonRadius("月亮大小", Range(1, 15)) = 3
        [CustomObject(_SunMoonSetting)][HDR][Gamma]                 _MoonColor("月亮颜色", Color) = (1,1,1,1)
        [CustomObject(_SunMoonSetting)][HDR][Gamma]                 _MoonHaloColor("光晕颜色", Color) = (0.2,0.3,0.5,1)
        [CustomObject(_SunMoonSetting)]                             _MoonHalo("月亮光晕", Range(0, 1)) = 0.4
        [CustomKeywordEnum(_SunMoonSetting,_SCREENPOS,_MATRIXPOS)]  _MoonType("坐标类型", Float) = 0

        [CustomGroup]_StarSetting("星星设置",float) = 1.0
        [CustomTexture(_StarSetting)]                               _StarsTex("星星贴图", 2D) = "black" {}
        [CustomObject(_StarSetting)][HDR]                           _StarsColor("星星颜色", Color) = (1,1,1,1)
        [CustomObject(_StarSetting)]                                _StarsSize("星星大小",Float) = 1
        [CustomObject(_StarSetting)]                                _StarsSpeed("星星移动速度", Range(-1, 1)) = 0.3
        [CustomTexture(_StarSetting)]                               _StarMaskTex("星星遮罩贴图", 2D) = "white" {}
        [CustomObject(_StarSetting)]                                _StarMaskSize("星星遮罩大小",Float) = 1
        [CustomObject(_StarSetting)]                                _StarPow("星星对比度",Range(1, 5)) = 1
        
        [CustomGroup] _FogSetting("雾设置",float) = 1.0
        [CustomObject(_FogSetting)]                                 _SkyFogExponent ("天空雾高度偏移", float) = 2.5
        [CustomObject(_FogSetting)]                                 _SkyFogIntensity("天空雾强度", Range(0,1)) = 1
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline""Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"
        }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile _ _ATMOSPERICSCATTERING
            #pragma shader_feature_local  _OpenDoubleColor

            #define _ATMOSPERICSCATTERING;

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            //#include "SimpleSkyInput.hlsl"
            
            // Constants
            #define PI 3.1415926535
            #define Pi316 0.0596831
            #define Pi14 0.07957747
            #define MieG float3(0.4375f, 1.5625f, 1.5f)

            float4 _SunDirectionWS;
            float4 _MoonDirectionWS;
            
            CBUFFER_START(UnityPerMaterial)
            half4   _SkyTopColor;
            half4   _SkyBottomColor, _GroundColor;
            half4   _RayleighScatteringColor;
            half4   _MieScatteringColor;
            
            half4   _SunColor;
            half4   _SunHalo;
            half    _SunRadius;
            half    _SunHaloExponent;
            
            half4   _MoonColor;
            half4   _MoonHaloColor;
            half    _MoonRadius;
            half    _MoonHalo, _GroundPositionOffset, _GroundPositionScale;
            
            half _AirScatteringHeight, _AirScatteringDensity, _HeightFogHeight;
            
            half4   _StarsColor;
            half    _StarsSize;
            half    _StarsSpeed;
            half    _StarMaskSize;
            half    _StarPow;
            
            half    _HorizonPower;
            half    _HorizonOffset;
            
            half    _SkyFogExponent, _SunScatterIntensity;
            half    _SkyFogIntensity;
            CBUFFER_END
            
            CBUFFER_START(TODBUFFER)
            #ifdef _ATMOSPERICSCATTERING
                float  _Azure_FogScatteringScale;
                int    _Azure_ScatteringMode;
            
                float  _Azure_Kr;
                float  _Azure_Km;
                float3 _Azure_Rayleigh;
                float3 _Azure_Mie;
                half   _MieG;
            
                float  _Azure_Scattering;
                float  _Azure_Luminance;
                float  _Azure_Exposure;
                float4 _Azure_RayleighColor;
                float4 _Azure_MieColor;
                float4 _Azure_ScatteringColor;
            #endif
            CBUFFER_END
            
            TEXTURE2D(_MoonTex);
            SAMPLER(sampler_MoonTex);
            SAMPLER(sampler_LinearClamp);
            
            TEXTURE2D(_StarsTex);
            SAMPLER(sampler_StarsTex);
            
            TEXTURE2D(_StarMaskTex);
            SAMPLER(sampler_StarMaskTex);

            uniform float4x4 _MoonMatrix;

            //#include "SkyInput.hlsl"
            #include "SkyLib.hlsl"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 uv : TEXCOORD0;
                float4 tangent :TANGENT0;
            };

            struct v2f
            {
                float3 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float4 pos: TEXCOORD3;
                float4 MoonPos : TEXCOORD5;
            };
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.worldPos = TransformObjectToWorldDir(v.vertex.xyz);
                o.pos = v.vertex;
                o.uv = v.uv;
                o.MoonPos.xyz =mul((float3x3)_MoonMatrix, v.vertex.xyz);
                o.MoonPos.x *= -1.0;
                return o;
            }
            
#ifdef _ATMOSPERICSCATTERING
             float MiePhaseFunction(float cosAngle)
            {
                float g = _MieG;
                float g2 = g * g;
                float phase = 3.0 * (1.0 - g2) / (2.0 * (2.0 + g2)) * ((1 + cosAngle * cosAngle) / pow((1 + g2 - 2 * g*cosAngle), 3.0 / 2.0));
                return phase;
            }
#endif

            half4 GetMoons(float2 MoonPos,half radius)
            {
                half4 moonDisc = saturate(SAMPLE_TEXTURE2D( _MoonTex,sampler_MoonTex,MoonPos.xy * _MoonRadius + float2(0.5,0.5)));
                half moonmask = step(-0.2,dot(UNITY_MATRIX_V[2].xyz, -_MoonDirectionWS.xyz)); //月亮遮罩: 摄像机与灯光方向的夹角 这样可以避免2个月亮 
                moonDisc.a *=saturate(moonmask);
                
                return moonDisc;
            }

            void GradientReckon(inout half3 color, float worldPosY, half3 skybottomColor, half3 skytopColor,
                       float skyGradientHeight, float skyGradientWidth , float SkyGradientExponent)
            {
                float gradient = smoothstep( saturate(skyGradientHeight - skyGradientWidth), skyGradientHeight, worldPosY);
                gradient = pow(gradient, SkyGradientExponent);
                color = lerp(skybottomColor.rgb, skytopColor.rgb, gradient);
            }

            void GradientReckon_V2(inout half3 color, float worldPosY, half3 skybottomColor, half3 skytopColor,
                       float skyGradientWidth , float SkyGradientExponent)
            {
                half weight = pow(saturate(worldPosY * skyGradientWidth), SkyGradientExponent * abs(1-worldPosY));
                half heightFog = saturate(worldPosY * _HeightFogHeight);
                weight = smoothstep(0, 1, pow(weight, 0.01));
                // weight = lerp(weight, heightFog, 1 - heightFog);
                half3 fC = lerp(skybottomColor, skytopColor, weight);
                color = fC;
            }

            half3 GetStar(float3 worldPos)
            {
                float2 skyuv = worldPos.xz / max(0, worldPos.y / 2);
                half3 stars = SAMPLE_TEXTURE2D( _StarsTex, sampler_StarsTex, skyuv * _StarsSize/*+ _StarsSpeed * _Time.x */).rrr;
                stars = pow(stars, _StarPow);
                half starsmask = SAMPLE_TEXTURE2D( _StarMaskTex, sampler_StarMaskTex, skyuv * _StarMaskSize + _StarsSpeed * _Time.x).r;
            
                return saturate(_MainLightPosition.y) * starsmask * stars * _StarsColor.rgb; //使星星只在夜晚显示
            }

            half4 frag(v2f i) : SV_Target
            {
                /*single scattering*/
                // Directions
                float3 worldPos = normalize(i.worldPos);
                float3 offsetPos = normalize(worldPos + float3(0,_HorizonOffset,0));
                
                float3 inScatter = 0;
                float3 moonInScatter = 0;
                _Azure_ScatteringMode = 0;
                
                #ifdef _ATMOSPERICSCATTERING
                    float3 viewDir = offsetPos;
                    float VdotS = dot(worldPos, _SunDirectionWS);
                    float VdotM = dot(worldPos, _MoonDirectionWS);
                    float r = length(float3(0.0, 50.0, 0.0));
                    float sunRise = saturate(dot(float3(0.0, 500.0, 0.0), _SunDirectionWS) / r);
                    float moonRise = saturate(dot(float3(0.0, 100.0, 0.0), _MoonDirectionWS) / r); //月光的地平线光晕更弱

                    // Optical depth
                    float zenith = acos(saturate(dot(float3(0.0, 1.0, 0.0), viewDir))) * _Azure_FogScatteringScale;
                    float z = (cos(zenith) + 0.15 * pow(93.885 - ((zenith * 180.0f) / PI), -1.253));
                    float SR = _Azure_Kr / z;
                    float SM = _Azure_Km / z;

                    // Extinction
                    float3 fex = exp(-(_Azure_Rayleigh * SR  + _Azure_Mie * SM));
                    float moonExtinction = saturate((viewDir.y) * 2.5);
                    float sunset = clamp(dot(float3(0.0, 1.0, 0.0), _SunDirectionWS), 0.0, 0.5);
                    float3 Esun = _Azure_ScatteringMode == 0 ? lerp(fex, (1.0 - fex), sunset) * _Azure_ScatteringColor : _Azure_ScatteringColor;

                    // Sun inScattering
                    float  rayPhase = 2.0 + 0.5 * pow(VdotS, 2.0);
                    //float  miePhase = MieG.x / pow(MieG.y - MieG.z * VdotS, 1.5);
                    float  miePhase = MiePhaseFunction(VdotS); //mieG参数改为可调
                    float3 BrTheta  = Pi316 * _Azure_Rayleigh * rayPhase * _RayleighScatteringColor; //Rayleigh散射系数 * 相位函数  3/16pi*(1+costheta^2)//_Azure_RayleighColor
                    float3 BmTheta  = Pi14  * _Azure_Mie * miePhase * _MieScatteringColor; //Mie散射系数 * 相位函数 //_Azure_MieColor
                    float3 BrmTheta = (BrTheta + BmTheta) / (_Azure_Rayleigh + _Azure_Mie);
                    inScatter = BrmTheta * _Azure_Scattering * Esun * (1.0 - fex) * _SunScatterIntensity;
                    //inScatter *= sunRise;
                
                    // Moon inScattering
                    rayPhase = 2.0 + 0.5 * pow(VdotM, 2.0);
                    //miePhase = MieG.x / pow(MieG.y - MieG.z * VdotM, 1.5); //mieG参数改为可调
                    miePhase = MiePhaseFunction(VdotM);
                    BrTheta  = Pi316 * _Azure_Rayleigh * rayPhase * _MoonHaloColor;
                    BmTheta  = Pi14  * _Azure_Mie * miePhase * _MoonColor;
                    BrmTheta = (BrTheta + BmTheta) / (_Azure_Rayleigh + _Azure_Mie);
                    Esun = _Azure_ScatteringMode == 0 ? (1.0 - fex) * _Azure_ScatteringColor : _Azure_ScatteringColor;
                    moonInScatter = BrmTheta * _Azure_Scattering * Esun * (1.0 - fex) * 0.1;
                    //moonInScatter *= 1.0 - sunRise;
                #endif
                
                float dotViewUp = dot(offsetPos, float3(0, 1, 0));
                half dotViewDown = dot(offsetPos, half3(0,-1, 0));

                half3 combined = 0;
                
                GradientReckon_V2(combined, i.worldPos.y, _SkyBottomColor.rgb, _SkyTopColor.rgb,
                    _AirScatteringHeight, _AirScatteringDensity);
                // return half4(combined, 1);
                // rongqingmei 测试
                // return float4(combined,1);
                
                #if !_DAY
                    //星星
                    half3 starColor = GetStar(i.worldPos);
                    StarsReckon(combined,starColor);
                #endif
                
                //月亮
                half4 moonColor=0;
                float2 screenUV = i.MoonPos.xy / i.MoonPos.w;
                half moonMask = saturate(worldPos.y * 3);
                moonMask *= moonMask;
                moonColor = GetMoons(i.MoonPos.xy, _MoonRadius) * moonMask;
                
                float3 ray = normalize(mul((float3x3)unity_ObjectToWorld, i.pos.xyz));

                half groundMask = saturate(worldPos.y * 30);
                
                MoonReckon(combined, moonColor,_MoonHaloColor.rgb,_MoonDirectionWS.xyz, ray, _MoonHalo, groundMask);
                SunReckon(combined, worldPos,_SunDirectionWS.xyz, dotViewUp, _SunColor, _SunRadius, _SunHalo,
                    _SunHaloExponent, moonMask, groundMask);
                
                combined = combined + inScatter + 0;
                //
                half offset = saturate((worldPos.y + _GroundPositionOffset) * _GroundPositionScale) ;
                combined = lerp(_GroundColor, combined, offset);
                
                return half4(combined, 1);
            }
            ENDHLSL
        }
    }
   // CustomEditor "CustomShaderEditor.CustomShaderGUI"
}