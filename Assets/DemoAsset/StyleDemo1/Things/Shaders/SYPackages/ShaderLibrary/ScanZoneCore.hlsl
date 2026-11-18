#ifndef SCANZONE_CORE_INCLUDE
#define SCANZONE_CORE_INCLUDE

CBUFFER_START(ScanZoneInclude)
float3 _position01, _position02, _position03, _position04;
float _visibilityDistance, _fadeWidth;
half _Hue, _Saturation, _Value, _Contrast, _Brightness;
half _ColliderStyleNum;
CBUFFER_END

float CubeFade(float3 worldPosition, float3 center, float visibilityDistance, float fadeWidth)
{
    float distX = distance(worldPosition.x, center.x);
	float distY = distance(worldPosition.y, center.y);
	float distZ = distance(worldPosition.z, center.z);
	float fadeX = smoothstep(visibilityDistance, visibilityDistance + fadeWidth, distX);
	float fadeY = smoothstep(visibilityDistance, visibilityDistance + fadeWidth, distY);
	float fadeZ = smoothstep(visibilityDistance, visibilityDistance + fadeWidth, distZ);
	float f = max(fadeX, fadeZ);
	f = max(f, fadeY);
	return f;
}

float SphereFade(float3 worldPosition, float3 center, float visibilityDistance, float fadeWidth)
{
	float dist = distance(worldPosition, center);
	return smoothstep(visibilityDistance, visibilityDistance + fadeWidth, dist);
}

float Fade(float3 worldPosition)
{
	float f0 = 0;//, f1 = 0, f2 = 0, f3 = 0, f4 = 0;
	f0=lerp(f0,SphereFade(worldPosition, _position01, _visibilityDistance, _fadeWidth),saturate(_ColliderStyleNum));
	f0=lerp(f0,CubeFade(worldPosition, _position01, _visibilityDistance, _fadeWidth),saturate(_ColliderStyleNum-1));
	/*if(_ColliderStyleNum == -1)
	{
	    f0 = 0;
	}
	else if(_ColliderStyleNum == 0)
	{
		f1 = SphereFade(worldPosition, _position01, _visibilityDistance, _fadeWidth);
		f2 = SphereFade(worldPosition, _position02, _visibilityDistance, _fadeWidth);
		f3 = SphereFade(worldPosition, _position03, _visibilityDistance, _fadeWidth);
		f4 = SphereFade(worldPosition, _position04, _visibilityDistance, _fadeWidth);
		f0 = f1;
	}
	else if(_ColliderStyleNum == 1)
	{
		f1 = CubeFade(worldPosition, _position01, _visibilityDistance, _fadeWidth);		
		f2 = CubeFade(worldPosition, _position02, _visibilityDistance, _fadeWidth);
		f3 = CubeFade(worldPosition, _position03, _visibilityDistance, _fadeWidth);
		f4 = CubeFade(worldPosition, _position04, _visibilityDistance, _fadeWidth);
		f0 = f1;
	}*/
	return f0;
}

void ScanZone_SceneColor(inout float3 color, float3 worldPosition)
{
// #if defined(_SCANZONE_ON)
    float fade = Fade(worldPosition);
	fade = saturate(1 - fade);
	float3 ScanZoneColor = RgbToHsv(color.rgb);
	ScanZoneColor.x += _Hue;
	ScanZoneColor.x = fmod(ScanZoneColor.x,1);
	ScanZoneColor.y *= (_Saturation + 1);
	ScanZoneColor.z *= (_Value + 1);
	ScanZoneColor = HsvToRgb(ScanZoneColor.rgb);
	float3 avgColor = float3(0.5, 0.5, 0.5);
	ScanZoneColor = lerp(avgColor, ScanZoneColor, clamp(_Contrast, 1, 1.1));
	ScanZoneColor = abs(ScanZoneColor * clamp(_Brightness, 1, 30));
	//color.rgb = lerp(ScanZoneColor, color.rgb, fade);
	color.rgb = ScanZoneColor;
	// color.rgb = half3(0,0,0);
// #endif
}

void ScanZone_SceneColor(inout float3 color)
{
#if defined(_SCANZONE_ON)
	float3 ScanZoneColor = RgbToHsv(color.rgb);
	ScanZoneColor.x += _Hue;
	ScanZoneColor.x = fmod(ScanZoneColor.x,1);
	ScanZoneColor.y *= (_Saturation + 1);
	ScanZoneColor.z *= (_Value + 1);
	ScanZoneColor = HsvToRgb(ScanZoneColor.rgb);
	float3 avgColor = float3(0.5, 0.5, 0.5);
	ScanZoneColor = lerp(avgColor, ScanZoneColor, clamp(_Contrast, 1, 1.1));
	ScanZoneColor = abs(ScanZoneColor * clamp(_Brightness, 1, 30));
	color.rgb = ScanZoneColor;
#endif
}

void ScanZone_SceneColor(inout float4 color)
{
#if defined(_SCANZONE_ON)
	float3 ScanZoneColor = RgbToHsv(color.rgb);
	ScanZoneColor.x += _Hue;
	ScanZoneColor.x = fmod(ScanZoneColor.x,1);
	ScanZoneColor.y *= (_Saturation + 1);
	ScanZoneColor.z *= (_Value + 1);
	ScanZoneColor = HsvToRgb(ScanZoneColor.rgb);
	float3 avgColor = float3(0.5, 0.5, 0.5);
	ScanZoneColor = lerp(avgColor, ScanZoneColor, clamp(_Contrast, 1, 1.1));
	ScanZoneColor = abs(ScanZoneColor * clamp(_Brightness, 1, 30));
	color.rgb = ScanZoneColor;
#endif
}

void ScanZone_SceneColor_float(float3 color, float3 worldPosition, inout half3 finalColor)
{
#if defined(_SCANZONE_ON)
    float fade = Fade(worldPosition);
	fade = saturate(1 - fade);
	float3 ScanZoneColor = RgbToHsv(color.rgb);
    ScanZoneColor.x += _Hue;
	ScanZoneColor.x = fmod(ScanZoneColor.x,1);
	ScanZoneColor.y *= (_Saturation + 1);
	ScanZoneColor.z *= (_Value + 1);
	ScanZoneColor = HsvToRgb(ScanZoneColor.rgb);
	color.rgb = lerp(ScanZoneColor, color.rgb, fade);
	finalColor = color.rgb;
#endif
}

#endif