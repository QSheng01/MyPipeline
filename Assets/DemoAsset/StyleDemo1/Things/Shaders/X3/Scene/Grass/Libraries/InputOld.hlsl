
#define AO_MASK				input.uv.y
#define BEND_MASK			input.uv.y

struct Attributes
{
	float4 positionOS   : POSITION;
	float3 normalOS     : NORMAL;

	float2 uv           : TEXCOORD0;
	float2 uv2  : TEXCOORD1;
	float2 lightmapUV	: TEXCOORD2;

	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS               : SV_POSITION;

	float3 uvAndEdge                : TEXCOORD0;
	half3  normalWS                 : TEXCOORD1;	
	float4 color					: TEXCOORD2;
	float4 positionWSAndFogFactor   : TEXCOORD3; // xyz: positionWS, w: vertex fog factor	

	DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 4);

	// /*nointerpolation */float3 lightMapColor	: TEXCOORD5;
	float3 pivotWS                   : TEXCOORD5;

#ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
	float4 shadowCoord              : TEXCOORD6; // compute shadow coord per-vertex for the main light
#endif
	float4 positionOS               : TEXCOORD7;// w用来存Height数据
#ifdef _ADDITIONAL_LIGHTS_VERTEX
	float3 vertexLight				: TEXCOORD8;
#endif

	float4 terrainColor             : TEXCOORD9; // A用来存距离衰减
	half4 var_0						: VAR_0;// xyz reflectDir, w bendLength
	float4 screenPos				: VAR_SCREENPOS;
	float gust				: TEXCOORD10;
	float4 uv				: TEXCOORD11;
	float4 positionWS               : TEXCOORD12;// w用来存Height数据
	
	//half3 specular					: TEXCOORD7;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

struct VertexOutput
{
	float4 positionOS;
	float3 positionWS;
	float3 normalWS;

	float3 pivotWS;
	float3 rootWS;
	float4 color;
	float3 lightMapColor;
	float3 terrainNormalWS;
};


float4x4 _localToWorldMatrix;

float ObjectPosRand01()
{
	return frac(UNITY_MATRIX_M[0][3] + UNITY_MATRIX_M[1][3] + UNITY_MATRIX_M[2][3]);
}

float4 DecodeFloat2Color(float input)
{
    uint cur = asuint(input);
	uint4 next = uint4(cur >> 0,cur >> 8,cur >> 16,cur >> 24)&0xFF;
   
    return float4(next) * rcp(255);
}

VertexOutput GetVertexData(Attributes input)
{	
	//float3 color = float3(UNITY_MATRIX_M[3][0], UNITY_MATRIX_M[3][1], UNITY_MATRIX_M[3][2]);
	float scale = UNITY_MATRIX_M[1][1];
	float4x4 objectToWorldMaterix = UNITY_MATRIX_M;

	VertexOutput data;
	data.color = DecodeFloat2Color(UNITY_MATRIX_M[3][0]);
	data.terrainNormalWS = DecodeFloat2Color(UNITY_MATRIX_M[3][1]).xyz;
	data.lightMapColor = DecodeFloat2Color(UNITY_MATRIX_M[3][2]).xyz;

	float4 positionOS = input.positionOS;
	// if (UNITY_MATRIX_M[3][3] > 0.5)
	// 	positionOS.xz *= scale; // 不知道是什么作用的代码

	data.positionOS = positionOS;
	data.positionWS = TransformObjectToWorld(positionOS);

	data.normalWS = TransformObjectToWorldNormal(input.normalOS);//input.positionOS;//mul((float3x3)objectToWorldMaterix, input.normalOS);
	//data.pivotWS = float3(objectToWorldMaterix[0][3], objectToWorldMaterix[1][3] + 0.25, objectToWorldMaterix[2][3]);
	data.pivotWS = float3(objectToWorldMaterix[0][3], objectToWorldMaterix[1][3], objectToWorldMaterix[2][3]);
	data.rootWS = TransformObjectToWorld(float3(-input.uv.x, 0, input.uv.y));
	return data;
}

VertexOutput GetVertexData(Attributes input, float4x4 m)
{	
	float scale = m[1][1];
	float4x4 objectToWorldMaterix = mul(_localToWorldMatrix, m);

	VertexOutput data;
	data.color = DecodeFloat2Color(m[3][0]);
	data.terrainNormalWS = DecodeFloat2Color(m[3][1]).xyz;
	data.lightMapColor = DecodeFloat2Color(m[3][2]).xyz;

	float4 positionOS = input.positionOS;
	if (m[3][3] > 0.5)
		positionOS.xz *= scale;

	data.positionOS = positionOS;
	data.positionWS = mul(objectToWorldMaterix, positionOS).xyz;
	data.normalWS = TransformObjectToWorldNormal(input.normalOS);
	//data.normalWS = TransformObjectToWorldNormal(input.normalOS);//input.positionOS;//mul((float3x3)objectToWorldMaterix, input.normalOS);
	//data.pivotWS = float3(objectToWorldMaterix[0][3], objectToWorldMaterix[1][3] + 0.25, objectToWorldMaterix[2][3]);
	data.pivotWS = float3(objectToWorldMaterix[0][3], objectToWorldMaterix[1][3], objectToWorldMaterix[2][3]);
	data.rootWS = TransformObjectToWorld(float3(-input.uv.x, 0, input.uv.y));
	return data;
}