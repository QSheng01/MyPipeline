//-----------------------------------------------------------------------------
// Copyright(C) Yan Verde - All Rights Reserved
// Copyright protected under Unity Asset Store EULA
// Refer to https://unity3d.com/legal/as_terms for more informations
// -----------------------------------------------------------------------------
// URP Water
// Author : Yan Verde
// Date : April 10, 2021
// -----------------------------------------------------------------------------

#ifndef URPWATER_WAVE_INCLUDED
#define URPWATER_WAVE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"   
#include "URPWaterHelpers.hlsl"
#include "URPWaterVariables.hlsl"


static uint DCount = 7;
static float2 Directions[] =
{
	float2(0.53, 0.45),
	float2(-0.209, 0.4),
	float2(-0.125, 0.592),
	float2(0.482, -0.876),
	float2(-0.729, -0.694),
	float2(-0.248, 0.968),
	float2(0.844, -0.538)
};

static uint LCount = 6;
static float Lengths[] =
{
	3.56,
	2.85,
	2.10,
	1.30,
	1.10,
	1.2
};

static uint SCount = 5;
static float SteepnessRange[] =
{
	1.0,
	1.8,
	1.6,
	1.25,
	0.5
};

static uint SpCount = 9;
static float Speeds[] =
{
	0.62,
	-0.8,
	0.45,
	-0.75,
	0.88,
	0.70,
	-0.56,
	0.35,
	-0.71
};

struct WaveData {
	float4 wave;
	float speed;
};

struct Wave
{
	float Length;
	float Steepness;
	float Speed;
	float Amplitude;
	float2 Direction;
};


//#define SOLVE_NORMAL_Z 1
#define SteepnessThreshold 1
#define GRAVITY 9.8

void SingleGerstnerWave(float3 WorldPos, Wave w, out float3 WPO, out float3 Normal)
{

	float dispersion = 6.28318 / w.Length;
	float c = sqrt(GRAVITY / dispersion) * w.Speed;
	float2 d = w.Direction;
	float Steepness = w.Steepness;
	float Speed = w.Speed;

	float f = dispersion * (dot(d, WorldPos.xz) - c * _Time.x);

	float cf;
	float sf;
	sincos(f, sf, cf);

	float a = Steepness / (dispersion * 1.5);
	float wKA = a * dispersion;

	WPO.xz = d.xy * (a * cf);
	WPO.y = a * sf;

	Normal.xz = -(cf * wKA * d);
	Normal.y = 0;

	/*
	#if SOLVE_NORMAL_Z
		Normal.y = sf * w.Steepness * saturate((a * SteepnessThreshold) / w.Length);
	#else
		Normal.y = 0;
	#endif
	*/
}

#endif
