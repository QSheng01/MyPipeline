
#ifndef UNIVERSAL_COMMON_INCLUDED
#define UNIVERSAL_COMMON_INCLUDED

#define UNITY_PI            3.14159265359f
float3 RotateAroundAxis(float3 original, float3 axis, float angle)
{
	float C = cos(angle);
	float S = sin(angle);
	float t = 1 - C;
	float m00 = t * axis.x * axis.x + C;
	float m01 = t * axis.x * axis.y - S * axis.z;
	float m02 = t * axis.x * axis.z + S * axis.y;
	float m10 = t * axis.x * axis.y + S * axis.z;
	float m11 = t * axis.y * axis.y + C;
	float m12 = t * axis.y * axis.z - S * axis.x;
	float m20 = t * axis.x * axis.z - S * axis.y;
	float m21 = t * axis.y * axis.z + S * axis.x;
	float m22 = t * axis.z * axis.z + C;
	float3x3 finalMatrix = float3x3(m00, m01, m02, m10, m11, m12, m20, m21, m22);
	return mul(finalMatrix, original) - original;
}

float3 RotateAroundAxis(float3 center,float3 original, float3 axis, float angle)
{
	original -= center;
	float C = cos(angle);
	float S = sin(angle);
	float t = 1 - C;
	float m00 = t * axis.x * axis.x + C;
	float m01 = t * axis.x * axis.y - S * axis.z;
	float m02 = t * axis.x * axis.z + S * axis.y;
	float m10 = t * axis.x * axis.y + S * axis.z;
	float m11 = t * axis.y * axis.y + C;
	float m12 = t * axis.y * axis.z - S * axis.x;
	float m20 = t * axis.x * axis.z - S * axis.y;
	float m21 = t * axis.y * axis.z + S * axis.x;
	float m22 = t * axis.z * axis.z + C;
	float3x3 finalMatrix = float3x3(m00, m01, m02, m10, m11, m12, m20, m21, m22);
	return mul(finalMatrix, original) + center;
}

//Ҫ�����������0-16 ����
float Encode(float param1, float param2)
{
	return param1 / 16 + param2 / 256;
}

float2 Decode(float param)
{
	float param1 = param * 16;
	float param2 = param * 256 - 16 * param1;

	return float2(param1, param2);
}

uint4 DecodeFloat2Uint(float input)
{
    uint4 cur = asuint(input);
    return cur & uint4(0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000);
}

//uint4 DecodeFloat2Uint(float input)
//{
//    uint cur = asuint(input);
//    uint bitMask = 0x000000FF; 
//    uint output0 = (cur >> 0)  & bitMask;
//    uint output1 = (cur >> 8)  & bitMask;
//    uint output2 = (cur >> 16) & bitMask;
//    uint output3 = (cur >> 24) & bitMask;
//    return uint4(output0, output1, output2, output3);
//}

float4 DecodeUint2Color(uint4 input)
{
    float4 color = float4(input) * float4(0.00390625,1.52587891e-05,5.96046448e-08,5.96046448e-08);
    color.a *=0.00390625;
    return color;
}

#endif



