//Stylized Grass Shader
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA
#ifndef WIND_OLD_INCLUDED
#define WIND_OLD_INCLUDED

#include "Common.hlsl"
#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"

//Properties
TEXTURE2D(_WindMap);       SAMPLER(sampler_WindMap);
TEXTURE2D(_TerrainDepth);       //SAMPLER(sampler_TerrainDepth);
TEXTURE2D(_CameraDepthTexture);       //SAMPLER(sampler_CameraDepthTexture);
float4 _GlobalWindParams;
float4 _DLSSJitter;
//X: Strength
//W: (int bool) Wind zone present

//float _WindStrength;
//float _WindBack;

//Nature Renderer parameters
float4 GlobalWindDirectionAndStrength;
//X: Gust dir X
//Y: Gust dir Y
//Z: Gust speed
//W: Gust strength
float4 _GlobalShiver;
//X: Shiver speed
//Y: Shiver strength

struct WindSettings
{
    float4 direction;
    float mask;
    float speed;

    float gustStrength;
    float gustFrequency;

    float WindStrength;
    float WindBack;

    float4 WindGlobaltilling;
    float WindGlobalspeed;
};
struct WindSettings01
{
    float mask;
    float ambientStrength;
    float ambientSpeed;
    float time;
    float3 direction;
    float swinging;

    float randObject;
    float randVertex;
    float randObjectStrength;

    float3 gustOffset;
    float gustSpeed;

    float gustStrength;
    float gustFrequency;
};
WindSettings PopulateWindSettings(in float strength, float speed, float4 direction, float mask, float gustStrength, float gustFrequency, float winback)
{
    WindSettings s = (WindSettings)0;

    //Apply WindZone strength
    if (_GlobalWindParams.w > 0)
    {
        strength *= _GlobalWindParams.x;
        gustStrength *= _GlobalWindParams.x;
    }

    //Nature renderer params
    if (_GlobalShiver.y > 0)
    {
        strength += _GlobalShiver.y;
        speed += _GlobalShiver.x;
    }
    if (GlobalWindDirectionAndStrength.w > 0)
    {
        gustStrength += GlobalWindDirectionAndStrength.w;
        direction.xz += GlobalWindDirectionAndStrength.xy;
    }
    
    s.mask = mask;
    s.speed = speed;
    s.direction = normalize(direction);

    s.gustStrength = gustStrength;
    s.gustFrequency = gustFrequency;


    s.WindStrength = strength;
    s.WindBack = winback;

    return s;
}

WindSettings PopulateWindSettings(in float strength, float speed, float4 direction, float mask, float gustStrength, float gustFrequency, float winback, float2 WindGlobaltilling, float WindGlobalspeed)
{
    WindSettings s = (WindSettings)0;

    //Apply WindZone strength
    if (_GlobalWindParams.w > 0)
    {
        strength *= _GlobalWindParams.x;
        gustStrength *= _GlobalWindParams.x;
    }

    //Nature renderer params
    if (_GlobalShiver.y > 0)
    {
        strength += _GlobalShiver.y;
        speed += _GlobalShiver.x;
    }
    if (GlobalWindDirectionAndStrength.w > 0)
    {
        gustStrength += GlobalWindDirectionAndStrength.w;
        direction.xz += GlobalWindDirectionAndStrength.xy;
    }
    
    s.mask = mask;
    s.speed = speed;
    s.direction = normalize(direction);

    s.gustStrength = gustStrength;
    s.gustFrequency = gustFrequency;


    s.WindStrength = strength;
    s.WindBack = winback;

    s.WindGlobaltilling = float4(WindGlobaltilling, 0, 0);
    s.WindGlobalspeed = WindGlobalspeed;

    return s;
}

WindSettings01 PopulateWindSettings(in float strength, float speed, float4 direction, float swinging, float mask, float randObject, float randVertex, float randObjectStrength, float gustStrength, float gustFrequency, float gustSpeed)
{
    WindSettings01 windSettings = (WindSettings01)0;

    //Apply WindZone
    // if (_GlobalWindParams.w > 0.5)
    // {
    // 	strength *= WIND_ZONE_MAIN;
    // 	gustStrength *= WIND_ZONE_TURBULENCE;
    // 	direction.xyz = WIND_ZONE_DIRECTION.xyz;
    
    // 	windSettings.time = _GlobalWindParams.x;
    // 	windSettings.gustOffset = _GlobalWindOffset.xyz;
    // 	windSettings.gustSpeed = gustSpeed;
    // }
    // else
    // {
    windSettings.time = _TimeParameters.x;
    windSettings.gustOffset = direction.xyz;
    windSettings.gustSpeed = gustSpeed * windSettings.time;
    //}
    
    windSettings.ambientStrength = strength;
    windSettings.ambientSpeed = speed;
    windSettings.direction.xyz = direction.xyz;
    windSettings.swinging = swinging;
    windSettings.mask = mask;
    windSettings.randObject = randObject;
    windSettings.randVertex = randVertex;
    windSettings.randObjectStrength = randObjectStrength;
    windSettings.gustStrength = gustStrength;
    windSettings.gustFrequency = gustFrequency * 0.01;

    return windSettings;
}

//World-align UV moving in wind direction
float2 GetGustingUV(float3 wPos, WindSettings s)
{
    return (wPos.xz * s.gustFrequency * 0.01) + (_TimeParameters.x * s.speed * s.gustFrequency * 0.01) ;//* -s.direction.xz;

}
float2 GetGustingUV2(float3 wPos, WindSettings s)
{
    return ((wPos.xz + s.direction.xz * _Time.y * s.WindGlobalspeed) * s.WindGlobaltilling.xy) ;//* -s.direction.xz;

}

float SampleGustMapLOD(float3 wPos, WindSettings s)
{

    float2 gustUV = GetGustingUV(wPos, s);
    //gustUV *=2;
    float gust = SAMPLE_TEXTURE2D_LOD(_WindMap, sampler_WindMap, gustUV, 0).r - s.WindBack;
    //gust = gust * 2 - 1;
    //gust *=.6;
    gust *= s.gustStrength * s.mask * 360;

    return gust;
}

float SampleGustMap(float3 wPos, WindSettings s)
{
    float2 gustUV = GetGustingUV2(wPos, s);

    float gust = SAMPLE_TEXTURE2D_LOD(_WindMap, sampler_WindMap, gustUV, 0).r;

    // gust *= s.gustStrength * s.mask;
    gust *= s.gustStrength ;

    //Apply WindZone strength
    if (_GlobalWindParams.w > 0)
    {
        gust *= _GlobalWindParams.x;
    }

    return gust;
}
float SampleGustMap2(float3 oPos, WindSettings s)
{
    float3 wPos = TransformObjectToWorld(oPos);
    
    float2 gustUV = GetGustingUV2(wPos, s);

    float gust = SAMPLE_TEXTURE2D_LOD(_WindMap, sampler_WindMap, gustUV, 0).r;

    // gust *= s.gustStrength * s.mask;
    gust *= s.gustStrength ;

    //Apply WindZone strength
    if (_GlobalWindParams.w > 0)
    {
        gust *= _GlobalWindParams.x;
    }

    return gust;
}

float4 GetWindOffset(in float3 positionWS, in float3 wPivotPos, WindSettings s)
{
    //Apply gusting
    float gust = SampleGustMapLOD(wPivotPos, s) ;
    
    //Mask by direction vector + gusting push
    float3 axis = cross(s.direction.xyz, float3(0, -1, 0));

    float angle = gust * UNITY_PI / 180;

    //float4 offset = float4(RotateAroundAxis(positionWS - wPivotPos, axis, angle), 1);
    float4 offset = float4(RotateAroundAxis(wPivotPos, positionWS, axis, angle), 1);
    return offset ;
}

float3 SizeNoise(float3 pivotWS, float3 posOS, half grassHeight = 1)
{
    float2 noiseUV2 = pivotWS.xz * 0.1;
    half colorNoise2 = SAMPLE_TEXTURE2D_LOD(_WindMap, sampler_WindMap, noiseUV2, 0).r;
    colorNoise2 = lerp(-0.35, 1.3, colorNoise2);
    posOS.y += colorNoise2 * saturate(posOS.y);

    return posOS;
}

// float3 GetSimplerWind_Old(in float3 positionWS, in float2 rootOS, float3 rootWS, WindSettings s ,float y, out half3 bend)
// {
// 	float3 oWS = positionWS;
// 	half length = distance(oWS, rootWS);
// 	float3 fWS = 0;
// 	float2 rootOS2 = rootOS * 5;
// 	float3 ws = float3(positionWS.x + rootOS2.x, positionWS.y, positionWS.z + rootOS2.y);
// 	float2 gustUV = GetGustingUV(ws, s) * 2.5;
// 	float gust = 0;
// 	gust = Simplex2DNoise(gustUV) * 0.5 + 0.5;
// 	gust *= s.gustStrength * y;
// 	half turbulenceSin = sin((_Time.y + rootWS.x) * 5) * cos((_Time.y + rootWS.y) * 2) * 1;
// 	half turbulence = lerp(0, turbulenceSin, saturate(gust - 0.2));
//     positionWS.xz += sin(gust) * s.direction.xz;
// 	positionWS.y += turbulence;
//
//
// 	half3 dir = normalize(positionWS - rootWS);
// 	float3 offset = dir * length;
// 	half3 WS2 = offset + rootWS;
// 	bend = distance(WS2, oWS);
// 	fWS = WS2 - 0;
//
//     return fWS ;
// }
//TransformObjectToWorld( input.positionOS.xyz )
float remapCus(float v1, float v2, float v3, float v4, float val)
{
    float _value = (v3 + (val - v1) * (v4 - v3) / (v2 - v1));;
    
    return _value;
}

float3 GetSimplerWind(float3 posOS, float2 rootOS, WindSettings s, float3 pivotWS, out half bendAmount)
{
    float2 rootOS_New = float2(-rootOS.x, rootOS.y);
    
    float3 posOS_Origin = posOS;
    half3 windDirOS = TransformWorldToObjectDir(s.direction);
    half gustStrength = s.gustStrength;
    half length_Origin = length(posOS_Origin - float3(rootOS_New.x, 0, rootOS_New.y));

    float2 rootOS2 = rootOS_New * 2;
    float3 uvWS = float3(pivotWS.x + rootOS2.x, pivotWS.y, pivotWS.z + rootOS2.y);
    float2 gustUV = GetGustingUV(uvWS, s) * 2.5;
    half gust = SAMPLE_TEXTURE2D_LOD(_WindMap, sampler_WindMap, gustUV, 0).r;
    gust *= gustStrength * posOS.y;

    half turbulenceSin = sin((_Time.y + uvWS.x) * 5) * cos((_Time.y + uvWS.z) * 2) * 0.75;
    half turbulence = lerp(0, turbulenceSin, saturate(gust - 0.2));

    posOS.xz += sin(gust) * posOS.y * windDirOS.xz;
    posOS.y -= turbulence;

    half3 dir = normalize(posOS - float3(rootOS_New.x, 0, rootOS_New.y));
    half3 newPosOS = dir * length_Origin + float3(rootOS_New.x, 0, rootOS_New.y);
    bendAmount = distance(posOS_Origin, newPosOS);

    return newPosOS;
}

float3 GetSimplerWind2(float3 posOS, float2 rootOS, WindSettings s, float3 pivotWS, out half bendAmount)
{
   // pivotWS = float3(0, 0, 0);
 //   rootOS = float2(0, 0);
    float2 rootOS_New = float2(-rootOS.x, rootOS.y);
    
    float3 posOS_Origin = posOS;
    half3 windDirOS = TransformWorldToObjectDir(s.direction);
    half gustStrength = s.gustStrength;
    half length_Origin = length(posOS_Origin - float3(rootOS_New.x, 0, rootOS_New.y));

    float2 rootOS2 = rootOS_New * 2;
    float3 uvWS = float3(pivotWS.x + rootOS2.x, pivotWS.y, pivotWS.z + rootOS2.y);
    // float2 gustUV = GetGustingUV(uvWS, s) * 2.5;
    // half gust = Simplex2DNoise(gustUV) * 0.5 + 0.5;
    // gust *= gustStrength * posOS.y;
    half gust = SampleGustMap2(posOS, s) * gustStrength * posOS.y;
    //没有被吹到也会有自然摆动
    //half gust2 = Simplex2DNoise(GetGustingUV(uvWS, s) * 2.5) * 0.5 + 0.5;
    gust = remapCus(0, 1, s.gustFrequency, 1, gust) ;
    
    
    half turbulenceSin = sin((_Time.y + uvWS.x) * 5) * cos((_Time.y + uvWS.z) * 2) * 0.75;
    half turbulence = lerp(0, turbulenceSin, saturate(gust - 0.2));

    posOS.xz += sin(gust) * posOS.y * windDirOS.xz;
    posOS.y -= turbulence;

    // 拉伸矫正
    half3 dir = normalize(posOS - float3(rootOS_New.x, 0, rootOS_New.y));
    half3 newPosOS = dir * length_Origin + float3(rootOS_New.x, 0, rootOS_New.y);
    bendAmount = distance(posOS_Origin, newPosOS);

    return newPosOS;
}

float3 ApplyWin(float3 wPos, float3 windVec, float3 positionOS)
{
    wPos.xyz = lerp(wPos.xyz, windVec.xyz, positionOS.y * 3);
    return wPos;
}


// 目前只用在ForwardPass中
half3 RootDither(float4 screenPos, half rootSoftness = 1.0h)
{
    float2 screenUV = screenPos.xy / screenPos.w;
    float localDepth = screenPos.w;
    float terrainDepth = SAMPLE_TEXTURE2D(_TerrainDepth, sampler_LinearClamp, screenUV);
    terrainDepth = LinearEyeDepth(terrainDepth, _ZBufferParams);
    half softMask = saturate((terrainDepth - localDepth) * rootSoftness);
    // Dither
    float2 screenUV_Raw = screenUV * _ScreenParams.xy;
    screenUV_Raw -= _DLSSJitter.xy * _ScreenParams.xy * 16;
    half dither = Dither8x8Bayer(fmod(screenUV_Raw.x, 8), fmod(screenUV_Raw.y, 8));
    // dither += 0.2;
    half mask = step(dither, softMask);

   // clip(mask - 0.1);
    
    return softMask;
}

float3 RotAroundY(half rotationRads, float3 positionOS)
{
    float3x3 rotationMatrix = float3x3(
        cos(rotationRads), 0, sin(rotationRads),
        0, 1, 0,
        - sin(rotationRads), 0, cos(rotationRads)
    );
    return mul(rotationMatrix, positionOS);
}

half3 BulrushWind(float3 positionOS, float2 staticLightmapUV, half4 color, half windSpeed, half4 windDir,
half gustStrength, half gustFreq)
{
    float3 pivotWS = float3(UNITY_MATRIX_M[0][3], UNITY_MATRIX_M[1][3], UNITY_MATRIX_M[2][3]);
    positionOS.xyz = SizeNoise(pivotWS, positionOS);
    if (staticLightmapUV.x != 0)
    {
        half3 winDirOS = TransformWorldToObjectDir(half3(windDir.x, 0, windDir.z));
        half3 objectNegXAxis = half3(-1, 0, 0);

        // 计算旋转轴
        half ss = winDirOS.z > 0 ? 1 : - 1;
        half3 rotationAxis = half3(0, ss, 0);
        rotationAxis = normalize(rotationAxis);

        // 计算旋转角度
        half rotationAngle = acos(dot(objectNegXAxis, winDirOS));

        // 创建旋转矩阵
        half3x3 rotationMatrix = float3x3(
            cos(rotationAngle) + rotationAxis.x * rotationAxis.x * (1 - cos(rotationAngle)), rotationAxis.x * rotationAxis.y * (1 - cos(rotationAngle)) - rotationAxis.z * sin(rotationAngle), rotationAxis.x * rotationAxis.z * (1 - cos(rotationAngle)) + rotationAxis.y * sin(rotationAngle),
            rotationAxis.y * rotationAxis.x * (1 - cos(rotationAngle)) + rotationAxis.z * sin(rotationAngle), cos(rotationAngle) + rotationAxis.y * rotationAxis.y * (1 - cos(rotationAngle)), rotationAxis.y * rotationAxis.z * (1 - cos(rotationAngle)) - rotationAxis.x * sin(rotationAngle),
            rotationAxis.z * rotationAxis.x * (1 - cos(rotationAngle)) - rotationAxis.y * sin(rotationAngle), rotationAxis.z * rotationAxis.y * (1 - cos(rotationAngle)) + rotationAxis.x * sin(rotationAngle), cos(rotationAngle) + rotationAxis.z * rotationAxis.z * (1 - cos(rotationAngle))
        );

        // 使用旋转矩阵旋转物体的顶点
        positionOS.xyz = mul(rotationMatrix, positionOS);
        // positionOS.xyz = RotAroundY(rotationAngle, positionOS);
        
        positionOS.xyz += half3(-staticLightmapUV.x, 0, staticLightmapUV.y);
    }

    // 旋转芦苇 朝着风的方向
    
    WindSettings wind = PopulateWindSettings(0, windSpeed, windDir, color.w,
    gustStrength, gustFreq, 0, float2(0, 0), 0);
    half bendAmount = 0;
    positionOS.xyz = GetSimplerWind(positionOS, color.xy, wind, pivotWS, bendAmount);
    return positionOS;
}

float2 GetGustingUV01(float3 positionWS, WindSettings01 windSettings)
{
    return (positionWS.xz - windSettings.gustOffset.xz * windSettings.gustSpeed.xx) * windSettings.gustFrequency;
}


#if defined(SHADER_STAGE_VERTEX) || defined(SHADER_STAGE_DOMAIN)
    #define SAMPLE_GUST_MAP(texName, sampler, uv) SAMPLE_TEXTURE2D_LOD(texName, sampler, uv, 0)
#else
    #define SAMPLE_GUST_MAP(texName, sampler, uv) SAMPLE_TEXTURE2D(texName, sampler, uv)
#endif
float SampleGustMap01(float3 positionWS, WindSettings01 windSettings)
{
    float2 gustUV = GetGustingUV01(positionWS, windSettings);

    float gust = SAMPLE_GUST_MAP(_WindMap, sampler_WindMap, gustUV).r;

    gust *= windSettings.gustStrength;

    return gust;
}

float4 GetWindOffset(in float3 positionOS, in float3 positionWS, float rand, WindSettings01 windSettings)
{
    float4 offset = 0;


    //Random offset per vertex
    float f = length(positionOS.xz) * windSettings.randVertex;
    float strength = windSettings.ambientStrength * 0.5 * lerp(1, rand, windSettings.randObjectStrength);
    
    //Combine
    float2 sine = sin(windSettings.ambientSpeed * (windSettings.time + (rand * windSettings.randObject) + f));
    //Remap from -1/1 to 0/1
    sine = lerp(sine * 0.5 + 0.5, sine, windSettings.swinging);

    //Apply gusting
    float2 gust = SampleGustMap01(positionWS, windSettings).xx;

    //Scale sine
    sine = sine * strength;

    //Mask by direction vector + gusting push
    offset.xz = windSettings.direction.xz * (sine + gust) * windSettings.mask;
    offset.y = windSettings.mask;

    //Summed offset strength
    float windWeight = length(offset.xz) + 0.0001;
    //Slightly negate the triangle-shape curve
    windWeight = pow(windWeight, 1.5);
    offset.y *= windWeight;

    //Wind strength in alpha
    offset.a = windWeight;


    return offset;
}


#endif
