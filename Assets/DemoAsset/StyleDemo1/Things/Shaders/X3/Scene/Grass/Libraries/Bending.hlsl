//Stylized Grass Shader
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA
#include "Common.hlsl"
//Global parameters
float4 _BendMapUV;
TEXTURE2D(_BendMap); SAMPLER(sampler_BendMap);
float4 _BendMap_TexelSize;
float4 _BendRenderParams;

struct BendSettings
{
	float mask;
	float pushStrength;
	float flattenStrength;
	float perspectiveCorrection;
};

BendSettings PopulateBendSettings(float mask, float pushStrength, float flattenStrength, float perspCorrection)
{
	BendSettings s = (BendSettings)0;

	s.mask = mask;
	s.pushStrength = pushStrength;
	s.flattenStrength = flattenStrength;
	s.perspectiveCorrection = perspCorrection;

	return s;
}

//UV Utilities
float2 BoundsToWorldUV(in float3 wPos, in float4 b)
{
	//return (wPos.xz * b.z) - (b.xy * b.z);
	float2 uv = (wPos.xz - b.xy) * b.z;

	return uv;
}

//Bend map UV
float2 GetBendMapUV(in float3 wPos) {

	return BoundsToWorldUV(wPos, _BendMapUV);
}

//Texture sampling
float4 GetBendVector(float3 wPos)
{
	float2 uv = GetBendMapUV(wPos);

	float4 v = SAMPLE_TEXTURE2D(_BendMap, sampler_BendMap, uv).rgba;

	v.x = v.x * 2.0 - 1.0;
	v.z = v.z * 2.0 - 1.0;

	
	return v;
}

float2 Rotate(float2 v, float cos0, float sin0)
{
	return float2(v.x * cos0 - v.y * sin0,
		v.x * sin0 + v.y * cos0);
}

float4 GetBendVectorLOD(float3 wPos) 
{
	float2 uv = GetBendMapUV(wPos);

	// 对 uv 进行旋转 _BendRenderParams.w 度，保证总是向前压倒草
	// uv = Rotate(uv - half2(0.5f, 0.5f), cos(_BendRenderParams.w), sin(_BendRenderParams.w));
	// uv += half2(0.5f, 0.5f);
	
	float4 v = SAMPLE_TEXTURE2D_LOD(_BendMap, sampler_BendMap, uv, 0).rgba;

	//Remap from 0.1 to -1.1
	v.x = v.x * 2.0 - 1.0;
	v.z = v.z * 2.0 - 1.0;
	//v.y = 1 - saturate(length(float2(v.x,v.z)));

	// 和_BendMap法线图对照，发现 z 值其实是朝下的，所以需要翻转一下。
	v.z *= -1.0f;
	return v;
}

float4 GetBendOffset(float3 wPos, float3 wPivotPos, BendSettings b) 
{
	float4 vec = GetBendVectorLOD(wPivotPos);

	float grassHeight = wPos.y;
	float bendHeight = vec.y;
	float dist = grassHeight - bendHeight;

	//Note since 7.1.5 somehow this causes the grass to bend down after the bender reaches a certain height
	//dist = abs(dist); //If bender is below grass, dont bend up

	float weight = saturate(dist);
	//weight = vec.y;
	float mask = b.mask * vec.a * 60;
	//float mask = vec.a * 90;
	float2 dir = normalize(float2(vec.x,vec.z));
	float3 axis = cross(float3(dir.x, 0, dir.y), float3(0, -1, 0));
	float angle = weight * mask * UNITY_PI / 180;
	angle = vec.a * UNITY_PI; 
	//float4 offset = float4(RotateAroundAxis(wPos - wPivotPos, axis, angle), vec.a);
	// 以 pivotPos 为原点，bend图倒下的方向的法线为轴，让wPos倒下angle角度
	float4 offset = float4(RotateAroundAxis(wPivotPos,wPos, axis, angle), vec.a);

	return offset;
}