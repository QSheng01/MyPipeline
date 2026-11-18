//-----------------------------------------------------------------------------
// Copyright(C) Yan Verde - All Rights Reserved
// Copyright protected under Unity Asset Store EULA
// Refer to https://unity3d.com/legal/as_terms for more informations
// -----------------------------------------------------------------------------
// URP Water
// Author : Yan Verde
// Date : April 10, 2021
// -----------------------------------------------------------------------------


#ifndef URPWATER_SCATTERING_INCLUDED
#define URPWATER_SCATTERING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "URPWaterVariables.hlsl"
#include "URPWaterHelpers.hlsl"


void ComputeScattering(inout GlobalData data, Varyings IN)
{	
	#if _SCATTERING_ON

		Light mainLight = data.mainLight;

		float3 lightColor = mainLight.color;

		// --- Translucency ---
		float3 L = mainLight.direction;
		float3 V = data.worldViewDir;
		float3 N = IN.worldNormal;// data.worldNormal;



		float3 H = normalize(L + N * _ScatteringRangeMin);
		float VdotH = pow(saturate(max(0, dot(V, -H))), _ScatteringRangeMax);
		
		float scatterMask = saturate(VdotH) * _ScatteringIntensity;
	

		float3 scatterColor = _ScatteringColor.rgb * saturate(scatterMask);

		data.scattering = scatterColor * saturate(lightColor);
	

	#endif
}

#endif