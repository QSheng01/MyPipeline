
#define AO_MASK 			input.uv.y
#define BEND_MASK 		input.uv.y

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Common.hlsl"
struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float3 color : COLOR;
    float2 uv : TEXCOORD0;
    float2 lightmapUV : TEXCOORD1;
    float2 pivotOS : TEXCOORD2;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct SHL2Data
{
    float4 SHAr;
    float4 SHAg;
    float4 SHAb;
    float4 SHBr;
    float4 SHBg;
    float4 SHBb;
    float4 SHC;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 uvAndEdge : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float4 color : COLOR0;
    float4 positionWSAndFogFactor : TEXCOORD3; // xyz: positionWS, w: vertex fog factor

    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 4);

    #ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
        float4 shadowCoord : TEXCOORD5; // compute shadow coord per-vertex for the main light
    #endif

    // #ifdef _ADDITIONAL_LIGHTS_VERTEX
    float3 pivotWS : TEXCOORD6;
    // #endif
    float4 positionOS : TEXCOORD7;// w用来存Height数据
    #ifdef _ADDITIONAL_LIGHTS_VERTEX
        float3 vertexLight : TEXCOORD8;
    #endif
    
    
    // #if _TerrainBlend_ON
    
    float4 terrainColor : TEXCOORD9;
    half4 var_0 : VAR_0;
    float4 screenPos : VAR_SCREENPOS;
    // #endif
    float gust : TEXCOORD10;
    float4 uv : TEXCOORD11;
    float4 positionWS : TEXCOORD12;// w用来存Height数据
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct VertexOutput
{
    float4 positionOS;
    float3 positionWS;
    float3 normalWS;

    float3 pivotWS;
    half4 color;
    half4 randomColor;
    half4 terrainColor;
    half3 wPos;
    float3 orignPositionWS;
};


float4x4 _localToWorldMatrix;

float ObjectPosRand01()
{
    return frac(UNITY_MATRIX_M[0][3] + UNITY_MATRIX_M[1][3] + UNITY_MATRIX_M[2][3]);
}

VertexOutput GetVertexData(Attributes input, half allScale, half CustomY)
{
    float3 color = float3(UNITY_MATRIX_M[3][0], UNITY_MATRIX_M[3][1], UNITY_MATRIX_M[3][2]);
    float scale = UNITY_MATRIX_M[1][1];

    float4x4 objectToWorldMaterix = mul(_localToWorldMatrix, UNITY_MATRIX_M);
    float4 positionOS = input.positionOS;
    if (allScale > 0.5)
    //positionOS.xz *= pow(allScale, 10);
    positionOS.xz *= allScale;
    positionOS.y *= CustomY;

    // positionOS.xyz *= scale;
    
    VertexOutput data;
    data.color = DecodeUint2Color(DecodeFloat2Uint(UNITY_MATRIX_M[3][0]));
    data.randomColor = DecodeUint2Color(DecodeFloat2Uint(UNITY_MATRIX_M[3][1]));
    data.terrainColor = DecodeUint2Color(DecodeFloat2Uint(UNITY_MATRIX_M[3][2]));

    data.positionOS = positionOS;
    data.positionWS = mul(objectToWorldMaterix, positionOS).xyz;
    data.normalWS = TransformObjectToWorldNormal(input.normalOS);//input.positionOS;//mul((float3x3)objectToWorldMaterix, input.normalOS);
    //data.pivotWS = float3(objectToWorldMaterix[0][3], objectToWorldMaterix[1][3] + 0.25, objectToWorldMaterix[2][3]);
    data.pivotWS = float3(objectToWorldMaterix[0][3], objectToWorldMaterix[1][3], objectToWorldMaterix[2][3]);
    //data.color = float4(color, UNITY_MATRIX_M[3][3]);

    return data;
}

VertexOutput GetVertexData(Attributes input, float4x4 m, half Scale)
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
    //data.positionWS = mul(m, positionOS).xyz;

    data.normalWS = TransformObjectToWorldNormal(input.normalOS);//input.positionOS;//mul((float3x3)objectToWorldMaterix, input.normalOS);
    //data.pivotWS = float3(objectToWorldMaterix[0][3], objectToWorldMaterix[1][3] + 0.25, objectToWorldMaterix[2][3]);
    data.pivotWS = float3(m[0][3], m[1][3], m[2][3]);
    //data.color = float4(color, UNITY_MATRIX_M[3][3]);

    return data;
}

// VertexOutput GetVertexData(Attributes input, float4x4 m, half Scale, WindSettings wind, out half bendAmount)
// {
//     //float3 color = float3(m[3][0], m[3][1], m[3][2]);
//     float scale = m[1][1];
//     VertexOutput data = (VertexOutput)0;
//     //float4x4 objectToWorldMaterix = mul(_localToWorldMatrix, m);
//     //float4x4 objectToWorldMaterix = m;
//     float4 positionOS = input.positionOS;
//     if (Scale > 0.5)
//     {
//         positionOS.xz *= scale;
//     }

//     //VertexOutput data;
//     data.color = DecodeUint2Color(DecodeFloat2Uint(m[3][0]));
//     data.randomColor = DecodeUint2Color(DecodeFloat2Uint(m[3][1]));
//     data.terrainColor = DecodeUint2Color(DecodeFloat2Uint(m[3][2]));
    
//     data.positionOS = positionOS;
// 	float3 wPos = mul(m, positionOS).xyz;
//     data.orignPositionWS = wPos;
//     data.normalWS = TransformObjectToWorldNormal(input.normalOS);//input.positionOS;//mul((float3x3)objectToWorldMaterix, input.normalOS);
//     //data.pivotWS = float3(objectToWorldMaterix[0][3], objectToWorldMaterix[1][3] + 0.25, objectToWorldMaterix[2][3]);
//     data.pivotWS = float3(m[0][3], m[1][3], m[2][3]);
//     //data.color = float4(color, UNITY_MATRIX_M[3][3]);

//     positionOS.xyz = GetSimplerWind2(positionOS, input.uv.xy, wind, pivotWS, bendAmount);
//                  input.positionOS.y *= _GrassHeight;
//     float3 newwPos = mul(m, positionOS).xyz;
//     bendAmount = distance(data.orignPositionWS, newwPos);
// 	data.positionWS = newwPos;
//     return data;
// }




