#ifndef AVT_TERRAIN_INPUT_INCLUDED
#define AVT_TERRAIN_INPUT_INCLUDED

// Stand-in for URP's Shaders/Terrain/TerrainLitInput.hlsl.
//
// That header opens and closes CBUFFER(UnityPerMaterial) itself and is behind an include
// guard, so there is no way to add this shader's own material properties to it. Declaring
// them outside makes the whole shader SRP Batcher incompatible, which costs a per-draw
// constant rebind for every terrain patch.
//
// Only the terrain symbols this shader actually references are reproduced here; the splat
// layers, hole map and layer normal maps are unused. Mirrors URP 14.0.12 -- revisit when
// upgrading the package.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
    half4 _BaseColor;
    half _Cutoff;
    float _NearBlendDistance;
    float _FarBlendDistance;
CBUFFER_END

#define _Surface 0.0 // Terrain is always opaque

CBUFFER_START(_Terrain)
    #ifdef UNITY_INSTANCING_ENABLED
    float4 _TerrainHeightmapRecipSize;   // float4(1/width, 1/height, 1/(width-1), 1/(height-1))
    float4 _TerrainHeightmapScale;       // float4(hmScale.x, hmScale.y / kMaxHeight, hmScale.z, 0)
    #endif
    #ifdef SCENESELECTIONPASS
    int _ObjectId;
    int _PassValue;
    #endif
CBUFFER_END

#if defined(UNITY_INSTANCING_ENABLED) && defined(_TERRAIN_INSTANCED_PERPIXEL_NORMAL)
#define ENABLE_TERRAIN_PERPIXEL_NORMAL
#endif

#ifdef UNITY_INSTANCING_ENABLED
TEXTURE2D(_TerrainHeightmapTexture);
TEXTURE2D(_TerrainNormalmapTexture);
SAMPLER(sampler_TerrainNormalmapTexture);
#endif

UNITY_INSTANCING_BUFFER_START(Terrain)
UNITY_DEFINE_INSTANCED_PROP(float4, _TerrainPatchInstanceData)  // float4(xBase, yBase, skipScale, ~)
UNITY_INSTANCING_BUFFER_END(Terrain)

void TerrainInstancing(inout float4 positionOS, inout float3 normal, inout float2 uv)
{
#ifdef UNITY_INSTANCING_ENABLED
    float2 patchVertex = positionOS.xy;
    float4 instanceData = UNITY_ACCESS_INSTANCED_PROP(Terrain, _TerrainPatchInstanceData);

    float2 sampleCoords = (patchVertex.xy + instanceData.xy) * instanceData.z; // (xy + float2(xBase,yBase)) * skipScale
    float height = UnpackHeightmap(_TerrainHeightmapTexture.Load(int3(sampleCoords, 0)));

    positionOS.xz = sampleCoords * _TerrainHeightmapScale.xz;
    positionOS.y = height * _TerrainHeightmapScale.y;

#ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
    normal = float3(0, 1, 0);
#else
    normal = _TerrainNormalmapTexture.Load(int3(sampleCoords, 0)).rgb * 2 - 1;
#endif
    uv = sampleCoords * _TerrainHeightmapRecipSize.zw;
#endif
}

void TerrainInstancing(inout float4 positionOS, inout float3 normal)
{
    float2 uv = { 0, 0 };
    TerrainInstancing(positionOS, normal, uv);
}

#endif // AVT_TERRAIN_INPUT_INCLUDED
