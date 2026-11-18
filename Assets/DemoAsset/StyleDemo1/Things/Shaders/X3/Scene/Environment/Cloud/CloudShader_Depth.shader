Shader "ShiYue/URP/Sky/Cloud_Depth"
{
	Properties
	{
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		[ASEBegin]_TextureSample0("Texture Sample 0", 2D) = "white" {}
		_lightingStrength("高光强度", Range( 0 , 100)) = 1
		_lightingRangePow("光源Pow", Range( 0 , 100)) = 0
		[Toggle]_useUV2("useUV2", Float) = 1
		_uvScale("uvScale", Vector) = (1,1,0,0)
		_Float2("高光遮罩Pow", Float) = 0
		_Float0("透射R通道遮罩Pow", Float) = 1
		_HorizonHeight("地平线颜色高度", Range( 0 , 10)) = 1
		_HorizonPow("地平线颜色宽度", Range( 0 , 10)) = 1
		_windNoise("windNoise", 2D) = "white" {}
		_WindStrength("风力强度", Range( 0 , 0.01)) = 0.001
		_WindSpeed("风速", Float) = 0.01
		_windUVScale("风力Noise图Scale", Vector) = (15,15,0,0)
		_DissolveTexture("DissolveTexture", 2D) = "white" {}
		_Dissolve("溶解", Range( 0 , 2)) = 0
		[ASEEnd][Toggle(_INVERTDISSOLVE_ON)] _InvertDissolve("反向溶解", Float) = 0


		//_TessPhongStrength( "Tess Phong Strength", Range( 0, 1 ) ) = 0.5
		//_TessValue( "Tess Max Tessellation", Range( 1, 32 ) ) = 16
		//_TessMin( "Tess Min Distance", Float ) = 10
		//_TessMax( "Tess Max Distance", Float ) = 25
		//_TessEdgeLength ( "Tess Edge length", Range( 2, 50 ) ) = 16
		//_TessMaxDisp( "Tess Max Displacement", Float ) = 25

		[HideInInspector] _QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector] _QueueControl("_QueueControl", Float) = -1

        [HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
	}

	SubShader
	{
		LOD 0
		
		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="AlphaTest" "Queue"="AlphaTest" "UniversalMaterialType"="Unlit" }

		Cull Off
		AlphaToMask Off

		

		HLSLINCLUDE
		#pragma target 4.5
		#pragma prefer_hlslcc gles
		// ensure rendering platforms toggle list is visible

		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

		#ifndef ASE_TESS_FUNCS
		#define ASE_TESS_FUNCS
		float4 FixedTess( float tessValue )
		{
			return tessValue;
		}

		float CalcDistanceTessFactor (float4 vertex, float minDist, float maxDist, float tess, float4x4 o2w, float3 cameraPos )
		{
			float3 wpos = mul(o2w,vertex).xyz;
			float dist = distance (wpos, cameraPos);
			float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0) * tess;
			return f;
		}

		float4 CalcTriEdgeTessFactors (float3 triVertexFactors)
		{
			float4 tess;
			tess.x = 0.5 * (triVertexFactors.y + triVertexFactors.z);
			tess.y = 0.5 * (triVertexFactors.x + triVertexFactors.z);
			tess.z = 0.5 * (triVertexFactors.x + triVertexFactors.y);
			tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
			return tess;
		}

		float CalcEdgeTessFactor (float3 wpos0, float3 wpos1, float edgeLen, float3 cameraPos, float4 scParams )
		{
			float dist = distance (0.5 * (wpos0+wpos1), cameraPos);
			float len = distance(wpos0, wpos1);
			float f = max(len * scParams.y / (edgeLen * dist), 1.0);
			return f;
		}

		float DistanceFromPlane (float3 pos, float4 plane)
		{
			float d = dot (float4(pos,1.0f), plane);
			return d;
		}

		bool WorldViewFrustumCull (float3 wpos0, float3 wpos1, float3 wpos2, float cullEps, float4 planes[6] )
		{
			float4 planeTest;
			planeTest.x = (( DistanceFromPlane(wpos0, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[0]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.y = (( DistanceFromPlane(wpos0, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[1]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.z = (( DistanceFromPlane(wpos0, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[2]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.w = (( DistanceFromPlane(wpos0, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[3]) > -cullEps) ? 1.0f : 0.0f );
			return !all (planeTest);
		}

		float4 DistanceBasedTess( float4 v0, float4 v1, float4 v2, float tess, float minDist, float maxDist, float4x4 o2w, float3 cameraPos )
		{
			float3 f;
			f.x = CalcDistanceTessFactor (v0,minDist,maxDist,tess,o2w,cameraPos);
			f.y = CalcDistanceTessFactor (v1,minDist,maxDist,tess,o2w,cameraPos);
			f.z = CalcDistanceTessFactor (v2,minDist,maxDist,tess,o2w,cameraPos);

			return CalcTriEdgeTessFactors (f);
		}

		float4 EdgeLengthBasedTess( float4 v0, float4 v1, float4 v2, float edgeLength, float4x4 o2w, float3 cameraPos, float4 scParams )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;
			tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
			tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
			tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
			tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			return tess;
		}

		float4 EdgeLengthBasedTessCull( float4 v0, float4 v1, float4 v2, float edgeLength, float maxDisplacement, float4x4 o2w, float3 cameraPos, float4 scParams, float4 planes[6] )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;

			if (WorldViewFrustumCull(pos0, pos1, pos2, maxDisplacement, planes))
			{
				tess = 0.0f;
			}
			else
			{
				tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
				tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
				tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
				tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			}
			return tess;
		}
		#endif //ASE_TESS_FUNCS
		ENDHLSL

		
//		Pass
//		{
//			
//			Name "Forward"
//			Tags { "LightMode"="UniversalForward" }
//
//			Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha
//			ZWrite On
//			ZTest LEqual
//			Offset 0 , 0
//			ColorMask RGBA
//
//			
//
//			HLSLPROGRAM
//
//			#pragma multi_compile_instancing
//			#define _ALPHAPREMULTIPLY_ON 1
//			#define _SURFACE_TYPE_TRANSPARENT 1
//			#define ASE_SRP_VERSION 140009
//
//
//			#pragma instancing_options renderinglayer
//
//			#pragma multi_compile _ LIGHTMAP_ON
//			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
//			#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
//        	#pragma multi_compile_fragment _ DEBUG_DISPLAY
//        	#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
//        	#pragma multi_compile_fragment _ _WRITE_RENDERING_LAYERS
//
//			#pragma vertex vert
//			#pragma fragment frag
//
//			#define SHADERPASS SHADERPASS_UNLIT
//
//			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
//			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
//			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
//			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
//			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
//			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
//			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
//			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
//
//			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
//			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
//			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
//			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
//
//			#define ASE_NEEDS_FRAG_WORLD_POSITION
//			#pragma shader_feature_local _INVERTDISSOLVE_ON
//
//
//			struct VertexInput
//			{
//				float4 vertex : POSITION;
//				float3 ase_normal : NORMAL;
//				float4 texcoord : TEXCOORD0;
//				float4 texcoord1 : TEXCOORD1;
//				float4 color : COLOR;
//				UNITY_VERTEX_INPUT_INSTANCE_ID
//			};
//
//			struct VertexOutput
//			{
//				float4 clipPos : SV_POSITION;
//				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
//					float3 worldPos : TEXCOORD0;
//				#endif
//				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
//					float4 shadowCoord : TEXCOORD1;
//				#endif
//				#ifdef ASE_FOG
//					float fogFactor : TEXCOORD2;
//				#endif
//				float4 texcoord3 : TEXCOORD3;
//				float4 color : COLOR;
//				UNITY_VERTEX_INPUT_INSTANCE_ID
//				UNITY_VERTEX_OUTPUT_STEREO
//			};
//
//			CBUFFER_START(UnityPerMaterial)
//			float2 _uvScale;
//			float2 _windUVScale;
//			float _useUV2;
//			float _WindSpeed;
//			float _WindStrength;
//			float _Float0;
//			float _HorizonHeight;
//			float _HorizonPow;
//			float _lightingRangePow;
//			float _Float2;
//			float _lightingStrength;
//			float _Dissolve;
//			#ifdef ASE_TESSELLATION
//				float _TessPhongStrength;
//				float _TessValue;
//				float _TessMin;
//				float _TessMax;
//				float _TessEdgeLength;
//				float _TessMaxDisp;
//			#endif
//			CBUFFER_END
//
//			sampler2D _TextureSample0;
//			sampler2D _windNoise;
//			float4 _Cloud_SunScatterColor;
//			float3 _SunDirectionWS;
//			float3 _MoonDirectionWS;
//			float4 _Cloud_MoonScatterColor;
//			float4 _Cloud_BarkColor;
//			float4 _Cloud_HorizonColor;
//			float _Cloud_HorizonOffset;
//			float _Cloud_HorizonPow;
//			float4 _Cloud_ScatterColor_2;
//			float4 _Cloud_BrightColor;
//			float4 _Cloud_HighLightSun;
//			float4 _Cloud_HighLightMoon;
//			sampler2D _DissolveTexture;
//
//
//			real3 ASESafeNormalize(float3 inVec)
//			{
//				real dp3 = max(FLT_MIN, dot(inVec, inVec));
//				return inVec* rsqrt( dp3);
//			}
//			
//			real4 ASESafeNormalize(float4 inVec)
//			{
//				real dp3 = max(FLT_MIN, dot(inVec, inVec));
//				return inVec* rsqrt( dp3);
//			}
//			
//
//			VertexOutput VertexFunction ( VertexInput v  )
//			{
//				VertexOutput o = (VertexOutput)0;
//				UNITY_SETUP_INSTANCE_ID(v);
//				UNITY_TRANSFER_INSTANCE_ID(v, o);
//				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
//
//				o.texcoord3.xy = v.texcoord.xy;
//				o.texcoord3.zw = v.texcoord1.xy;
//				o.color = v.color;
//
//				#ifdef ASE_ABSOLUTE_VERTEX_POS
//					float3 defaultVertexValue = v.vertex.xyz;
//				#else
//					float3 defaultVertexValue = float3(0, 0, 0);
//				#endif
//
//				float3 vertexValue = defaultVertexValue;
//
//				#ifdef ASE_ABSOLUTE_VERTEX_POS
//					v.vertex.xyz = vertexValue;
//				#else
//					v.vertex.xyz += vertexValue;
//				#endif
//
//				v.ase_normal = v.ase_normal;
//
//				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
//				float4 positionCS = TransformWorldToHClip( positionWS );
//
//				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
//					o.worldPos = positionWS;
//				#endif
//
//				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
//					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
//					vertexInput.positionWS = positionWS;
//					vertexInput.positionCS = positionCS;
//					o.shadowCoord = GetShadowCoord( vertexInput );
//				#endif
//
//				#ifdef ASE_FOG
//					o.fogFactor = ComputeFogFactor( positionCS.z );
//				#endif
//
//				o.clipPos = positionCS;
//
//				return o;
//			}
//
//			#if defined(ASE_TESSELLATION)
//			struct VertexControl
//			{
//				float4 vertex : INTERNALTESSPOS;
//				float3 ase_normal : NORMAL;
//				float4 texcoord : TEXCOORD0;
//				float4 texcoord1 : TEXCOORD1;
//				float4 color : COLOR;
//
//				UNITY_VERTEX_INPUT_INSTANCE_ID
//			};
//
//			struct TessellationFactors
//			{
//				float edge[3] : SV_TessFactor;
//				float inside : SV_InsideTessFactor;
//			};
//
//			VertexControl vert ( VertexInput v )
//			{
//				VertexControl o;
//				UNITY_SETUP_INSTANCE_ID(v);
//				UNITY_TRANSFER_INSTANCE_ID(v, o);
//				o.vertex = v.vertex;
//				o.ase_normal = v.ase_normal;
//				o.texcoord = v.texcoord;
//				o.texcoord1 = v.texcoord1;
//				o.color = v.color;
//				return o;
//			}
//
//			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
//			{
//				TessellationFactors o;
//				float4 tf = 1;
//				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
//				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
//				#if defined(ASE_FIXED_TESSELLATION)
//				tf = FixedTess( tessValue );
//				#elif defined(ASE_DISTANCE_TESSELLATION)
//				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
//				#elif defined(ASE_LENGTH_TESSELLATION)
//				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
//				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
//				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
//				#endif
//				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
//				return o;
//			}
//
//			[domain("tri")]
//			[partitioning("fractional_odd")]
//			[outputtopology("triangle_cw")]
//			[patchconstantfunc("TessellationFunction")]
//			[outputcontrolpoints(3)]
//			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
//			{
//			   return patch[id];
//			}
//
//			[domain("tri")]
//			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
//			{
//				VertexInput o = (VertexInput) 0;
//				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
//				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
//				o.texcoord = patch[0].texcoord * bary.x + patch[1].texcoord * bary.y + patch[2].texcoord * bary.z;
//				o.texcoord1 = patch[0].texcoord1 * bary.x + patch[1].texcoord1 * bary.y + patch[2].texcoord1 * bary.z;
//				o.color = patch[0].color * bary.x + patch[1].color * bary.y + patch[2].color * bary.z;
//				#if defined(ASE_PHONG_TESSELLATION)
//				float3 pp[3];
//				for (int i = 0; i < 3; ++i)
//					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
//				float phongStrength = _TessPhongStrength;
//				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
//				#endif
//				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
//				return VertexFunction(o);
//			}
//			#else
//			VertexOutput vert ( VertexInput v )
//			{
//				return VertexFunction( v );
//			}
//			#endif
//
//			half4 frag ( VertexOutput IN
//				#ifdef _WRITE_RENDERING_LAYERS
//				, out float4 outRenderingLayers : SV_Target1
//				#endif
//				 ) : SV_Target
//			{
//				UNITY_SETUP_INSTANCE_ID( IN );
//				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );
//
//				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
//					float3 WorldPosition = IN.worldPos;
//				#endif
//
//				float4 ShadowCoords = float4( 0, 0, 0, 0 );
//
//				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
//					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
//						ShadowCoords = IN.shadowCoord;
//					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
//						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
//					#endif
//				#endif
//
//				float mulTime508 = _TimeParameters.x * _WindSpeed;
//				float2 temp_output_498_0 = ((tex2D( _windNoise, ( ( (( _useUV2 )?( ( _uvScale * IN.texcoord3.zw ) ):( ( IN.texcoord3.xy * _uvScale ) )) + mulTime508 ) * _windUVScale ) )*2.0 + -1.0)).rg;
//				float2 uv341 = ( (( _useUV2 )?( ( _uvScale * IN.texcoord3.zw ) ):( ( IN.texcoord3.xy * _uvScale ) )) + ( temp_output_498_0 * _WindStrength ) );
//				float4 tex2DNode5 = tex2D( _TextureSample0, uv341 );
//				float4 mainTex266 = tex2DNode5;
//				float3 normalizeResult63 = ASESafeNormalize( _SunDirectionWS );
//				float4 transform454 = mul(GetObjectToWorldMatrix(),float4( 0,0,0,1 ));
//				float4 normalizeResult61 = ASESafeNormalize( ( float4( WorldPosition , 0.0 ) - transform454 ) );
//				float dotResult51 = dot( float4( normalizeResult63 , 0.0 ) , normalizeResult61 );
//				float temp_output_54_0 = saturate( ( ( dotResult51 + 1.0 ) * 0.5 ) );
//				float3 normalizeResult125 = ASESafeNormalize( _MoonDirectionWS );
//				float dotResult126 = dot( normalizeResult61 , float4( normalizeResult125 , 0.0 ) );
//				float temp_output_130_0 = saturate( pow( ( ( dotResult126 + 1.0 ) * 0.5 ) , 3.0 ) );
//				float lightMask2290 = saturate( ( temp_output_54_0 + temp_output_130_0 ) );
//				float saferPower198 = abs( lightMask2290 );
//				float temp_output_457_0 = saturate( pow( ( IN.color.a * _HorizonHeight ) , _HorizonPow ) );
//				float height408 = temp_output_457_0;
//				float saferPower414 = abs( height408 );
//				float temp_output_134_0 = pow( temp_output_130_0 , _lightingRangePow );
//				float moonMask_1332 = temp_output_134_0;
//				float saferPower480 = abs( moonMask_1332 );
//				float temp_output_36_0 = pow( saturate( ( ( 1.0 - temp_output_457_0 ) + _Cloud_HorizonOffset ) ) , _Cloud_HorizonPow );
//				float4 lerpResult40 = lerp( _Cloud_BarkColor , _Cloud_HorizonColor , temp_output_36_0);
//				float temp_output_55_0 = pow( temp_output_54_0 , _lightingRangePow );
//				float temp_output_166_0 = ( temp_output_55_0 + temp_output_134_0 );
//				float lightMask148 = temp_output_166_0;
//				float4 lerpResult419 = lerp( lerpResult40 , _Cloud_ScatterColor_2 , ( _Cloud_ScatterColor_2.a * lightMask148 ));
//				float4 barkSideColor242 = lerpResult419;
//				float4 lerpResult442 = lerp( _Cloud_BrightColor , _Cloud_ScatterColor_2 , ( _Cloud_ScatterColor_2.a * pow( lightMask148 , 1.0 ) ));
//				float4 brightColor142 = lerpResult442;
//				float4 lerpResult14 = lerp( barkSideColor242 , brightColor142 , tex2DNode5.r);
//				float temp_output_100_0 = pow( pow( tex2DNode5.g , _Float2 ) , 1.0 );
//				float sunMask_1227 = temp_output_55_0;
//				
//				float2 uv_Raw513 = (( _useUV2 )?( ( _uvScale * IN.texcoord3.zw ) ):( ( IN.texcoord3.xy * _uvScale ) ));
//				float temp_output_526_0 = saturate( ( tex2D( _DissolveTexture, uv_Raw513 ).r + 1.0 + ( _Dissolve * -1.0 ) ) );
//				#ifdef _INVERTDISSOLVE_ON
//				float staticSwitch528 = ( 1.0 - temp_output_526_0 );
//				#else
//				float staticSwitch528 = temp_output_526_0;
//				#endif
//				float dissolve517 = staticSwitch528;
//				float alpha219 = ( tex2DNode5.b * dissolve517 );
//				
//				float3 BakedAlbedo = 0;
//				float3 BakedEmission = 0;
//				float3 Color = ( ( ( pow( mainTex266.r , _Float0 ) * _Cloud_SunScatterColor * pow( saferPower198 , 3.0 ) * pow( saferPower414 , 2.0 ) ) + ( _Cloud_MoonScatterColor * pow( saferPower480 , 3.0 ) * mainTex266.r ) ) + lerpResult14 + ( ( ( temp_output_100_0 * _Cloud_HighLightSun * _lightingStrength ) * sunMask_1227 ) + ( ( temp_output_100_0 * _Cloud_HighLightMoon * _lightingStrength ) * moonMask_1332 ) ) ).rgb;
//				float Alpha = alpha219;
//				float AlphaClipThreshold = 0.5;
//				float AlphaClipThresholdShadow = 0.5;
//
//				#ifdef _ALPHATEST_ON
//					clip( Alpha - AlphaClipThreshold );
//				#endif
//
//				#if defined(_DBUFFER)
//					ApplyDecalToBaseColor(IN.clipPos, Color);
//				#endif
//
//				#if defined(_ALPHAPREMULTIPLY_ON)
//				Color *= Alpha;
//				#endif
//
//				#ifdef LOD_FADE_CROSSFADE
//					LODFadeCrossFade( IN.clipPos );
//				#endif
//
//				#ifdef ASE_FOG
//					Color = MixFog( Color, IN.fogFactor );
//				#endif
//
//				#ifdef _WRITE_RENDERING_LAYERS
//					uint renderingLayers = GetMeshRenderingLayer();
//					outRenderingLayers = float4( EncodeMeshRenderingLayer( renderingLayers ), 0, 0, 0 );
//				#endif
//
//				return half4( Color, Alpha );
//			}
//			ENDHLSL
//		}
//		
//		Pass
//		{
//			
//			Name "DepthOnly"
//			Tags { "LightMode"="DepthOnly" }
//
//			ZWrite On
//			ColorMask 0
//			AlphaToMask Off
//
//			HLSLPROGRAM
//
//			#pragma multi_compile_instancing
//			#define _ALPHAPREMULTIPLY_ON 1
//			#define _SURFACE_TYPE_TRANSPARENT 1
//			#define ASE_SRP_VERSION 140009
//
//
//			#pragma vertex vert
//			#pragma fragment frag
//
//			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
//			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
//			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
//			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
//			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
//
//			#pragma shader_feature_local _INVERTDISSOLVE_ON
//
//
//			struct VertexInput
//			{
//				float4 vertex : POSITION;
//				float3 ase_normal : NORMAL;
//				float4 texcoord : TEXCOORD0;
//				float4 texcoord1 : TEXCOORD1;
//				UNITY_VERTEX_INPUT_INSTANCE_ID
//			};
//
//			struct VertexOutput
//			{
//				float4 clipPos : SV_POSITION;
//				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
//				float3 worldPos : TEXCOORD0;
//				#endif
//				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
//				float4 shadowCoord : TEXCOORD1;
//				#endif
//				float4 texcoord2 : TEXCOORD2;
//				UNITY_VERTEX_INPUT_INSTANCE_ID
//				UNITY_VERTEX_OUTPUT_STEREO
//			};
//
//			CBUFFER_START(UnityPerMaterial)
//			float2 _uvScale;
//			float2 _windUVScale;
//			float _useUV2;
//			float _WindSpeed;
//			float _WindStrength;
//			float _Float0;
//			float _HorizonHeight;
//			float _HorizonPow;
//			float _lightingRangePow;
//			float _Float2;
//			float _lightingStrength;
//			float _Dissolve;
//			#ifdef ASE_TESSELLATION
//				float _TessPhongStrength;
//				float _TessValue;
//				float _TessMin;
//				float _TessMax;
//				float _TessEdgeLength;
//				float _TessMaxDisp;
//			#endif
//			CBUFFER_END
//
//			sampler2D _TextureSample0;
//			sampler2D _windNoise;
//			sampler2D _DissolveTexture;
//
//
//			
//			VertexOutput VertexFunction( VertexInput v  )
//			{
//				VertexOutput o = (VertexOutput)0;
//				UNITY_SETUP_INSTANCE_ID(v);
//				UNITY_TRANSFER_INSTANCE_ID(v, o);
//				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
//
//				o.texcoord2.xy = v.texcoord.xy;
//				o.texcoord2.zw = v.texcoord1.xy;
//
//				#ifdef ASE_ABSOLUTE_VERTEX_POS
//					float3 defaultVertexValue = v.vertex.xyz;
//				#else
//					float3 defaultVertexValue = float3(0, 0, 0);
//				#endif
//
//				float3 vertexValue = defaultVertexValue;
//
//				#ifdef ASE_ABSOLUTE_VERTEX_POS
//					v.vertex.xyz = vertexValue;
//				#else
//					v.vertex.xyz += vertexValue;
//				#endif
//
//				v.ase_normal = v.ase_normal;
//
//				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
//
//				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
//					o.worldPos = positionWS;
//				#endif
//
//				o.clipPos = TransformWorldToHClip( positionWS );
//				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
//					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
//					vertexInput.positionWS = positionWS;
//					vertexInput.positionCS = o.clipPos;
//					o.shadowCoord = GetShadowCoord( vertexInput );
//				#endif
//
//				return o;
//			}
//
//			#if defined(ASE_TESSELLATION)
//			struct VertexControl
//			{
//				float4 vertex : INTERNALTESSPOS;
//				float3 ase_normal : NORMAL;
//				float4 texcoord : TEXCOORD0;
//				float4 texcoord1 : TEXCOORD1;
//
//				UNITY_VERTEX_INPUT_INSTANCE_ID
//			};
//
//			struct TessellationFactors
//			{
//				float edge[3] : SV_TessFactor;
//				float inside : SV_InsideTessFactor;
//			};
//
//			VertexControl vert ( VertexInput v )
//			{
//				VertexControl o;
//				UNITY_SETUP_INSTANCE_ID(v);
//				UNITY_TRANSFER_INSTANCE_ID(v, o);
//				o.vertex = v.vertex;
//				o.ase_normal = v.ase_normal;
//				o.texcoord = v.texcoord;
//				o.texcoord1 = v.texcoord1;
//				return o;
//			}
//
//			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
//			{
//				TessellationFactors o;
//				float4 tf = 1;
//				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
//				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
//				#if defined(ASE_FIXED_TESSELLATION)
//				tf = FixedTess( tessValue );
//				#elif defined(ASE_DISTANCE_TESSELLATION)
//				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
//				#elif defined(ASE_LENGTH_TESSELLATION)
//				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
//				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
//				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
//				#endif
//				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
//				return o;
//			}
//
//			[domain("tri")]
//			[partitioning("fractional_odd")]
//			[outputtopology("triangle_cw")]
//			[patchconstantfunc("TessellationFunction")]
//			[outputcontrolpoints(3)]
//			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
//			{
//			   return patch[id];
//			}
//
//			[domain("tri")]
//			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
//			{
//				VertexInput o = (VertexInput) 0;
//				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
//				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
//				o.texcoord = patch[0].texcoord * bary.x + patch[1].texcoord * bary.y + patch[2].texcoord * bary.z;
//				o.texcoord1 = patch[0].texcoord1 * bary.x + patch[1].texcoord1 * bary.y + patch[2].texcoord1 * bary.z;
//				#if defined(ASE_PHONG_TESSELLATION)
//				float3 pp[3];
//				for (int i = 0; i < 3; ++i)
//					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
//				float phongStrength = _TessPhongStrength;
//				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
//				#endif
//				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
//				return VertexFunction(o);
//			}
//			#else
//			VertexOutput vert ( VertexInput v )
//			{
//				return VertexFunction( v );
//			}
//			#endif
//
//			half4 frag(VertexOutput IN  ) : SV_TARGET
//			{
//				UNITY_SETUP_INSTANCE_ID(IN);
//				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );
//
//				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
//					float3 WorldPosition = IN.worldPos;
//				#endif
//
//				float4 ShadowCoords = float4( 0, 0, 0, 0 );
//
//				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
//					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
//						ShadowCoords = IN.shadowCoord;
//					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
//						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
//					#endif
//				#endif
//
//				float mulTime508 = _TimeParameters.x * _WindSpeed;
//				float2 temp_output_498_0 = ((tex2D( _windNoise, ( ( (( _useUV2 )?( ( _uvScale * IN.texcoord2.zw ) ):( ( IN.texcoord2.xy * _uvScale ) )) + mulTime508 ) * _windUVScale ) )*2.0 + -1.0)).rg;
//				float2 uv341 = ( (( _useUV2 )?( ( _uvScale * IN.texcoord2.zw ) ):( ( IN.texcoord2.xy * _uvScale ) )) + ( temp_output_498_0 * _WindStrength ) );
//				float4 tex2DNode5 = tex2D( _TextureSample0, uv341 );
//				float2 uv_Raw513 = (( _useUV2 )?( ( _uvScale * IN.texcoord2.zw ) ):( ( IN.texcoord2.xy * _uvScale ) ));
//				float temp_output_526_0 = saturate( ( tex2D( _DissolveTexture, uv_Raw513 ).r + 1.0 + ( _Dissolve * -1.0 ) ) );
//				#ifdef _INVERTDISSOLVE_ON
//				float staticSwitch528 = ( 1.0 - temp_output_526_0 );
//				#else
//				float staticSwitch528 = temp_output_526_0;
//				#endif
//				float dissolve517 = staticSwitch528;
//				float alpha219 = ( tex2DNode5.b * dissolve517 );
//				
//
//				float Alpha = alpha219;
//				float AlphaClipThreshold = 0.5;
//
//				#ifdef _ALPHATEST_ON
//					clip(Alpha - AlphaClipThreshold);
//				#endif
//
//				#ifdef LOD_FADE_CROSSFADE
//					LODFadeCrossFade( IN.clipPos );
//				#endif
//				return 0;
//			}
//			ENDHLSL
//		}
//		
//		Pass
//		{
//			
//            Name "SceneSelectionPass"
//            Tags { "LightMode"="SceneSelectionPass" }
//
//			Cull Off
//
//			HLSLPROGRAM
//
//			#pragma multi_compile_instancing
//			#define _ALPHAPREMULTIPLY_ON 1
//			#define _SURFACE_TYPE_TRANSPARENT 1
//			#define ASE_SRP_VERSION 140009
//
//
//			#pragma vertex vert
//			#pragma fragment frag
//
//			#define ATTRIBUTES_NEED_NORMAL
//			#define ATTRIBUTES_NEED_TANGENT
//			#define SHADERPASS SHADERPASS_DEPTHONLY
//
//			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
//			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
//			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
//			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
//			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
//			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
//			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
//
//			#pragma shader_feature_local _INVERTDISSOLVE_ON
//
//
//			struct VertexInput
//			{
//				float4 vertex : POSITION;
//				float3 ase_normal : NORMAL;
//				float4 texcoord : TEXCOORD0;
//				float4 texcoord1 : TEXCOORD1;
//				UNITY_VERTEX_INPUT_INSTANCE_ID
//			};
//
//			struct VertexOutput
//			{
//				float4 clipPos : SV_POSITION;
//				float4 texcoord : TEXCOORD0;
//				UNITY_VERTEX_INPUT_INSTANCE_ID
//				UNITY_VERTEX_OUTPUT_STEREO
//			};
//
//			CBUFFER_START(UnityPerMaterial)
//			float2 _uvScale;
//			float2 _windUVScale;
//			float _useUV2;
//			float _WindSpeed;
//			float _WindStrength;
//			float _Float0;
//			float _HorizonHeight;
//			float _HorizonPow;
//			float _lightingRangePow;
//			float _Float2;
//			float _lightingStrength;
//			float _Dissolve;
//			#ifdef ASE_TESSELLATION
//				float _TessPhongStrength;
//				float _TessValue;
//				float _TessMin;
//				float _TessMax;
//				float _TessEdgeLength;
//				float _TessMaxDisp;
//			#endif
//			CBUFFER_END
//
//			sampler2D _TextureSample0;
//			sampler2D _windNoise;
//			sampler2D _DissolveTexture;
//
//
//			
//			int _ObjectId;
//			int _PassValue;
//
//			struct SurfaceDescription
//			{
//				float Alpha;
//				float AlphaClipThreshold;
//			};
//
//			VertexOutput VertexFunction(VertexInput v  )
//			{
//				VertexOutput o;
//				ZERO_INITIALIZE(VertexOutput, o);
//
//				UNITY_SETUP_INSTANCE_ID(v);
//				UNITY_TRANSFER_INSTANCE_ID(v, o);
//				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
//
//				o.texcoord.xy = v.texcoord.xy;
//				o.texcoord.zw = v.texcoord1.xy;
//
//				#ifdef ASE_ABSOLUTE_VERTEX_POS
//					float3 defaultVertexValue = v.vertex.xyz;
//				#else
//					float3 defaultVertexValue = float3(0, 0, 0);
//				#endif
//
//				float3 vertexValue = defaultVertexValue;
//
//				#ifdef ASE_ABSOLUTE_VERTEX_POS
//					v.vertex.xyz = vertexValue;
//				#else
//					v.vertex.xyz += vertexValue;
//				#endif
//
//				v.ase_normal = v.ase_normal;
//
//				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
//				o.clipPos = TransformWorldToHClip(positionWS);
//
//				return o;
//			}
//
//			#if defined(ASE_TESSELLATION)
//			struct VertexControl
//			{
//				float4 vertex : INTERNALTESSPOS;
//				float3 ase_normal : NORMAL;
//				float4 texcoord : TEXCOORD0;
//				float4 texcoord1 : TEXCOORD1;
//
//				UNITY_VERTEX_INPUT_INSTANCE_ID
//			};
//
//			struct TessellationFactors
//			{
//				float edge[3] : SV_TessFactor;
//				float inside : SV_InsideTessFactor;
//			};
//
//			VertexControl vert ( VertexInput v )
//			{
//				VertexControl o;
//				UNITY_SETUP_INSTANCE_ID(v);
//				UNITY_TRANSFER_INSTANCE_ID(v, o);
//				o.vertex = v.vertex;
//				o.ase_normal = v.ase_normal;
//				o.texcoord = v.texcoord;
//				o.texcoord1 = v.texcoord1;
//				return o;
//			}
//
//			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
//			{
//				TessellationFactors o;
//				float4 tf = 1;
//				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
//				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
//				#if defined(ASE_FIXED_TESSELLATION)
//				tf = FixedTess( tessValue );
//				#elif defined(ASE_DISTANCE_TESSELLATION)
//				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
//				#elif defined(ASE_LENGTH_TESSELLATION)
//				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
//				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
//				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
//				#endif
//				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
//				return o;
//			}
//
//			[domain("tri")]
//			[partitioning("fractional_odd")]
//			[outputtopology("triangle_cw")]
//			[patchconstantfunc("TessellationFunction")]
//			[outputcontrolpoints(3)]
//			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
//			{
//				return patch[id];
//			}
//
//			[domain("tri")]
//			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
//			{
//				VertexInput o = (VertexInput) 0;
//				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
//				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
//				o.texcoord = patch[0].texcoord * bary.x + patch[1].texcoord * bary.y + patch[2].texcoord * bary.z;
//				o.texcoord1 = patch[0].texcoord1 * bary.x + patch[1].texcoord1 * bary.y + patch[2].texcoord1 * bary.z;
//				#if defined(ASE_PHONG_TESSELLATION)
//				float3 pp[3];
//				for (int i = 0; i < 3; ++i)
//					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
//				float phongStrength = _TessPhongStrength;
//				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
//				#endif
//				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
//				return VertexFunction(o);
//			}
//			#else
//			VertexOutput vert ( VertexInput v )
//			{
//				return VertexFunction( v );
//			}
//			#endif
//
//			half4 frag(VertexOutput IN ) : SV_TARGET
//			{
//				SurfaceDescription surfaceDescription = (SurfaceDescription)0;
//
//				float mulTime508 = _TimeParameters.x * _WindSpeed;
//				float2 temp_output_498_0 = ((tex2D( _windNoise, ( ( (( _useUV2 )?( ( _uvScale * IN.texcoord.zw ) ):( ( IN.texcoord.xy * _uvScale ) )) + mulTime508 ) * _windUVScale ) )*2.0 + -1.0)).rg;
//				float2 uv341 = ( (( _useUV2 )?( ( _uvScale * IN.texcoord.zw ) ):( ( IN.texcoord.xy * _uvScale ) )) + ( temp_output_498_0 * _WindStrength ) );
//				float4 tex2DNode5 = tex2D( _TextureSample0, uv341 );
//				float2 uv_Raw513 = (( _useUV2 )?( ( _uvScale * IN.texcoord.zw ) ):( ( IN.texcoord.xy * _uvScale ) ));
//				float temp_output_526_0 = saturate( ( tex2D( _DissolveTexture, uv_Raw513 ).r + 1.0 + ( _Dissolve * -1.0 ) ) );
//				#ifdef _INVERTDISSOLVE_ON
//				float staticSwitch528 = ( 1.0 - temp_output_526_0 );
//				#else
//				float staticSwitch528 = temp_output_526_0;
//				#endif
//				float dissolve517 = staticSwitch528;
//				float alpha219 = ( tex2DNode5.b * dissolve517 );
//				
//
//				surfaceDescription.Alpha = alpha219;
//				surfaceDescription.AlphaClipThreshold = 0.5;
//
//				#if _ALPHATEST_ON
//					float alphaClipThreshold = 0.01f;
//					#if ALPHA_CLIP_THRESHOLD
//						alphaClipThreshold = surfaceDescription.AlphaClipThreshold;
//					#endif
//					clip(surfaceDescription.Alpha - alphaClipThreshold);
//				#endif
//
//				half4 outColor = half4(_ObjectId, _PassValue, 1.0, 1.0);
//				return outColor;
//			}
//			ENDHLSL
//		}
		
		Pass
		{
			
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }

			ZTest LEqual
			ZWrite On


			HLSLPROGRAM

			#pragma multi_compile_instancing
			#define _ALPHAPREMULTIPLY_ON 1
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ASE_SRP_VERSION 140009


			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile_fragment _ _WRITE_RENDERING_LAYERS
        	#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define VARYINGS_NEED_NORMAL_WS

			#define _ALPHATEST_ON 1 // 深度需要剔除

			#define SHADERPASS SHADERPASS_DEPTHNORMALSONLY

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"

			#pragma shader_feature_local _INVERTDISSOLVE_ON
		

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 texcoord : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				float3 normalWS : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
				float4 screenPos : TEXCOORD4;
			};

			CBUFFER_START(UnityPerMaterial)
			float2 _uvScale;
			float2 _windUVScale;
			float _useUV2;
			float _WindSpeed;
			float _WindStrength;
			float _Float0;
			float _HorizonHeight;
			float _HorizonPow;
			float _lightingRangePow;
			float _Float2;
			float _lightingStrength;
			float _Dissolve;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			sampler2D _TextureSample0;
			sampler2D _windNoise;
			sampler2D _DissolveTexture;


			
			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			VertexOutput VertexFunction(VertexInput v  )
			{
				VertexOutput o;
				ZERO_INITIALIZE(VertexOutput, o);

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.texcoord1.xy = v.texcoord.xy;
				o.texcoord1.zw = v.texcoord1.xy;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = v.ase_normal;
				
				float3 normalWS = TransformObjectToWorldNormal(v.ase_normal);

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				float3 positionVS = TransformWorldToView(positionWS);
				glstate_matrix_projection[2][2] *= 0.1;
				float4 positionCS = TransformWViewToHClip(positionVS);
				
				o.clipPos = positionCS;
				o.screenPos = ComputeScreenPos(o.clipPos);
				o.normalWS.xyz =  normalWS;

				return o;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 texcoord : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.texcoord = v.texcoord;
				o.texcoord1 = v.texcoord1;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.texcoord = patch[0].texcoord * bary.x + patch[1].texcoord * bary.y + patch[2].texcoord * bary.z;
				o.texcoord1 = patch[0].texcoord1 * bary.x + patch[1].texcoord1 * bary.y + patch[2].texcoord1 * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			inline float Dither8x8Bayer( int x, int y )
			{
			    const float dither[ 64 ] = {
			        0, 32,  8, 40,  2, 34, 10, 42,
			        48, 16, 56, 24, 50, 18, 58, 26,
			        12, 44,  4, 36, 14, 46,  6, 38,
			        60, 28, 52, 20, 62, 30, 54, 22,
			         3, 35, 11, 43,  1, 33,  9, 41,
			        51, 19, 59, 27, 49, 17, 57, 25,
			        15, 47,  7, 39, 13, 45,  5, 37,
			        63, 31, 55, 23, 61, 29, 53, 21};
			    int r = y * 8 + x;
			    return dither[r] / 64; // same # of instructions as pre-dividing due to compiler magic
			}

			void frag( VertexOutput IN
				, out half4 outNormalWS : SV_Target0
			#ifdef _WRITE_RENDERING_LAYERS
				, out float4 outRenderingLayers : SV_Target1
			#endif
				 )
			{
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				float mulTime508 = _TimeParameters.x * _WindSpeed;
				float2 temp_output_498_0 = ((tex2D( _windNoise, ( ( (( _useUV2 )?( ( _uvScale * IN.texcoord1.zw ) ):( ( IN.texcoord1.xy * _uvScale ) )) + mulTime508 ) * _windUVScale ) )*2.0 + -1.0)).rg;
				float2 uv341 = ( (( _useUV2 )?( ( _uvScale * IN.texcoord1.zw ) ):( ( IN.texcoord1.xy * _uvScale ) )) + ( temp_output_498_0 * _WindStrength ) );
				float4 tex2DNode5 = tex2D( _TextureSample0, uv341 );
				
				float2 uv_Raw513 = (( _useUV2 )?( ( _uvScale * IN.texcoord1.zw ) ):( ( IN.texcoord1.xy * _uvScale ) ));
				float temp_output_526_0 = saturate( ( tex2D( _DissolveTexture, uv_Raw513 ).r * tex2DNode5.r + 1.0 + ( _Dissolve * -1.0 ) ) );
				#ifdef _INVERTDISSOLVE_ON
				float staticSwitch528 = ( 1.0 - temp_output_526_0 );
				#else
				float staticSwitch528 = temp_output_526_0;
				#endif
				float dissolve517 = staticSwitch528;
				float alpha219 = ( tex2DNode5.b * dissolve517 );

	

				// 半透打孔
				float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
				screenUV *= _ScreenParams.xy;
				half dither = Dither8x8Bayer( fmod(screenUV.x, 8), fmod(screenUV.y, 8) );
				dither = dither + 0.01 * (1 - alpha219); // 补上一些洞洞
				alpha219 = step(dither, alpha219);
				
				surfaceDescription.Alpha = alpha219;
				surfaceDescription.AlphaClipThreshold = 0.5;

				#if _ALPHATEST_ON
					clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODFadeCrossFade( IN.clipPos );
				#endif

				#if defined(_GBUFFER_NORMALS_OCT)
					float3 normalWS = normalize(IN.normalWS);
					float2 octNormalWS = PackNormalOctQuadEncode(normalWS);           // values between [-1, +1], must use fp32 on some platforms
					float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);   // values between [ 0,  1]
					half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);      // values between [ 0,  1]
					outNormalWS = half4(packedNormalWS, 0.0);
				#else
					float3 normalWS = IN.normalWS;
					outNormalWS = half4(NormalizeNormalPerPixel(normalWS), 0.0);
				#endif

				#ifdef _WRITE_RENDERING_LAYERS
					uint renderingLayers = GetMeshRenderingLayer();
					outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
				#endif
			}

			ENDHLSL
		}

	
	}
	
	CustomEditor "UnityEditor.ShaderGraphUnlitGUI"
	FallBack "Hidden/Shader Graph/FallbackError"
	
	Fallback "Hidden/InternalErrorShader"
}