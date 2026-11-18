//Stylized Grass Shader
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA
#include "Common.hlsl"
//Properties
TEXTURE2D(_WindMap);       SAMPLER(sampler_WindMap);
float4 _GlobalWindParams;
//X: Strength
//W: (int bool) Wind zone present
float _WindStrength;
// float _WindBack;
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
    float mask;
    float speed;
    float4 direction;

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

WindSettings PopulateWindSettings(in float strength, float speed, float4 direction, float mask, float gustStrength, float gustFrequency)
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
    s.direction = direction;

    s.gustStrength = gustStrength;
    s.gustFrequency = gustFrequency;

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

float remapCus(float v1, float v2, float v3, float v4, float val)
{
    float _value = (v3 + (val - v1) * (v4 - v3) / (v2 - v1));;
    
    return _value;
}

float2 GetGustingUV2(float3 wPos, WindSettings s)
{
    return ((wPos.xz + s.direction.xz * _Time.y * s.WindGlobalspeed) * s.WindGlobaltilling.xy) ;//* -s.direction.xz;

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

float3 worldToLocal(float3 worldPos, float4x4 objToWorld)
{
    float3x3 worldToLocalRot = transpose((float3x3)objToWorld);
    float3 localDir = mul(worldToLocalRot, worldPos);
    return localDir;
}

float3 GetSimplerWind2(float3 posOS, float3 posWS, float2 rootOS, WindSettings s, float3 pivotWS, float4x4 objToWorld, out half bendAmount)
{
    pivotWS = float3(0, 0, 0);
    rootOS = float2(0, 0);
    float2 rootOS_New = float2(-rootOS.x, rootOS.y);
    
    float3 posOS_Origin = posOS;
    half3 windDirOS = worldToLocal(s.direction, objToWorld);
    half gustStrength = s.gustStrength;
    half length_Origin = length(posOS_Origin - float3(rootOS_New.x, 0, rootOS_New.y));

    float2 rootOS2 = rootOS_New * 2;
    float3 uvWS = float3(pivotWS.x + rootOS2.x, pivotWS.y, pivotWS.z + rootOS2.y);
    // float2 gustUV = GetGustingUV(uvWS, s) * 2.5;
    // half gust = Simplex2DNoise(gustUV) * 0.5 + 0.5;
    // gust *= gustStrength * posOS.y;
    half gust = SampleGustMap(posWS, s) * gustStrength * posOS.y;
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

float3 SizeNoise(float3 pivotWS, float3 posOS, half grassHeight = 1)
{
    float2 noiseUV2 = pivotWS.xz * 0.1;
    half colorNoise2 = SAMPLE_TEXTURE2D_LOD(_WindMap, sampler_WindMap, noiseUV2, 0).r;
    colorNoise2 = lerp(-0.35, 1.3, colorNoise2);
    posOS.y += colorNoise2 * saturate(posOS.y);

    return posOS;
}

VertexOutput GetVertexData(Attributes input, float4x4 m, half Scale, WindSettings wind, half grassHeight, out half bendAmount)
{
    //float3 color = float3(m[3][0], m[3][1], m[3][2]);
    float scale = m[1][1];
    VertexOutput data = (VertexOutput)0;
    //float4x4 objectToWorldMaterix = mul(_localToWorldMatrix, m);
    //float4x4 objectToWorldMaterix = m;
    float4 positionOS = input.positionOS;
    if (Scale > 0.5)
    {
        positionOS.xz *= scale;
    }

    //VertexOutput data;
    data.color = DecodeUint2Color(DecodeFloat2Uint(m[3][0]));
    data.randomColor = DecodeUint2Color(DecodeFloat2Uint(m[3][1]));
    data.terrainColor = DecodeUint2Color(DecodeFloat2Uint(m[3][2]));
    
    data.positionOS = positionOS;
    float3 wPos = mul(m, positionOS).xyz;
    data.orignPositionWS = wPos;
    data.normalWS = TransformObjectToWorldNormal(input.normalOS);//input.positionOS;//mul((float3x3)objectToWorldMaterix, input.normalOS);
    //data.pivotWS = float3(objectToWorldMaterix[0][3], objectToWorldMaterix[1][3] + 0.25, objectToWorldMaterix[2][3]);
    data.pivotWS = float3(m[0][3], m[1][3], m[2][3]);
    //data.color = float4(color, UNITY_MATRIX_M[3][3]);
    
    positionOS.xyz = SizeNoise(data.pivotWS, positionOS, grassHeight);
    positionOS.xyz = GetSimplerWind2(positionOS, wPos, input.uv.xy, wind, data.pivotWS, m, bendAmount);
    positionOS.y *= grassHeight;
    float3 newwPos = mul(m, positionOS).xyz;
    data.positionWS = newwPos;
    return data;
}


