#ifndef CHARACTERS_OUTLINE_HLSL
#define CHARACTERS_OUTLINE_HLSL

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// 顶点输入结构
struct a2v_Outline
{
    float4 positionOS : POSITION;
    float2 texcoord : TEXCOORD0;
    float3 normalOS : NORMAL;
    float4 color : COLOR;
    float4 tangentOS : TANGENT;

    #if defined(UV2_AS_NORMALS)
			    float4 uv1 : TEXCOORD1;
			    // float4 uv2 : TEXCOORD1;
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// 顶点着色器输出结构
struct v2f_Outline
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 color : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

// 顶点着色器输出结构
struct v2f_OutlineNormal
{
	float4 positionCS : SV_POSITION;
	float2 uv : TEXCOORD0;
	float4 color : TEXCOORD1;
	float3 normalWS : TEXCOORD2;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};


float3 OctahedronToUnitVector(float2 oct)
{
    // float3 unitVec = float3(oct, 1 - dot(float2(1, 1), abs(oct)));
    //
    // if (unitVec.z < 0)
    // {
    //     unitVec.xy = (1 - abs(unitVec.yx)) *(unitVec.xy >= 0 ? float2(1, 1) : float2(-1, -1));
    // }
    float3 unitVec = float3(oct.x, oct.y, 1.0f - abs(oct.x) - abs(oct.y));
    float t = max(-unitVec.z, 0.0f);
    unitVec.x += unitVec.x >= 0.0f ? -t : t;
    unitVec.y += unitVec.y >= 0.0f ? -t : t;
    return normalize(unitVec);
}

float3x3 GetTBNMatrix(a2v_Outline input)
{
    float3 normalOS = input.normalOS;
    float4 tangentOSOS = input.tangentOS;
    float3 binormal = cross(normalize(normalOS),
                            normalize(tangentOSOS.xyz)) * tangentOSOS.w;

    float3x3 objectToTangentMatrix = float3x3(tangentOSOS.xyz, binormal, normalOS);

    return objectToTangentMatrix;
}

float3 GetNormalOS(float3 normalTS, a2v_Outline input)
{
    TransformTangentToWorld(normalTS, GetTBNMatrix(input));

    float3x3 objectToTangentMatrix = GetTBNMatrix(input);
    float3 normalOS = normalize(mul(normalTS, objectToTangentMatrix));
    return normalOS;
}

v2f_Outline OutLineOctahedronPassVertex_Scene(a2v_Outline input)
{
    v2f_Outline output = (v2f_Outline)0;

    #if _USEOUTLINE
				
				output.uv = input.texcoord;
					            
    #if UV2_AS_NORMALS
				float3 normalTS = OctahedronToUnitVector(input.uv1.xy);
				normalTS = normalize(normalTS);
				float3 normalOS = normalize(input.normalOS);
				float3 tangentOS = normalize(input.tangentOS);
				float sign = input.tangentOS.w * GetOddNegativeScale();
				float3 bitangentOS = cross(normalOS, tangentOS) * sign;
				  bitangentOS  = normalize(bitangentOS);
				float3x3 tangentToNormal = float3x3(input.tangentOS.xyz, bitangentOS,
					                               input.normalOS.xyz);
				float3 normal = mul(normalTS, tangentToNormal);

				// float3x3 tanToWorld = float3x3(float3(tangentOS.x, bitangentOS.x, normalOS.x),
				//                    float3(tangentOS.y, bitangentOS.y, normalOS.y),
				//                    float3(tangentOS.z, bitangentOS.z, normalOS.z));
				// float3 normal = float3(dot(tanToWorld[0], normalTS), dot(tanToWorld[1], normalTS), dot(tanToWorld[2], normalTS));
				//normal = normalTS;
    #elif  _Normal
				float3 normal = input.normalOS.xyz;
				
    #elif COLORS_AS_NORMALS
				
				float3 normal = GetNormalOS(normalize(input.color.rgb * 2 - 1), input);
				
    #endif
				
				normal= normalize(normal);

				//将法线转到观察空间
				normal = mul((float3x3)UNITY_MATRIX_IT_MV, normal);
				normal.z = 0.001f;
				normal = normalize(normal); 
				//将顶点转到观察空间
				float3 positionVS = mul(GetWorldToViewMatrix(), float4(mul(GetObjectToWorldMatrix(), float4(input.positionOS.xyz, 1.0)).xyz, 1.0)).xyz;
				float3 viewDir = normalize(positionVS.xyz);

				///https://zhuanlan.zhihu.com/p/98948117?utm_source=qq
				///增加了一个linewidth的操作，因为unity_CameraProjection[1].y其实就是cos（FOV/2），所以这个操作的根本目的是为了保证轮廓线随着FOV的变换也是成一定比例，同时也不会随着镜头离物体的远近距离而变化。
				// y = cos（fov/2）
				float linewidth = -positionVS.z / (unity_CameraProjection[1].y);
				linewidth = sqrt(linewidth);
				linewidth *=0.01;
				linewidth *= _Outline_Width;
				linewidth *= input.color.a;
				//linewidth *= input.color.x;
				positionVS.z += _Offset_Z * 0.001;
				positionVS.xy = normal.xy * linewidth + positionVS.xy;
			  
				output.color.xyz = input.color.xyz;
				output.positionCS = TransformWViewToHClip(positionVS);
				//output.positionCS.z +=0.001 * _Offset_Z;
    #else
    output.color = input.color;
    output.uv = input.texcoord;
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.positionCS.z = output.positionCS.w + 1.0f; //不开启描边时，默认让描边无法通过裁剪矩阵测试
    #endif
    return output;
}

half4 OutlinePassFragmentNew(v2f_Outline input) : SV_Target
{
    #if defined(_USEOUTLINE)
			    half4 finalOutlineColor;
    #if defined(_USEOUTLINEMAP)
			    half4 outlineMapColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
			    finalOutlineColor.rgb = outlineMapColor.rgb * _OutlineColor.rgb;
    #else
			    finalOutlineColor.rgb = _OutlineColor.rgb;
    #endif
			    finalOutlineColor.a = 1;
			    return finalOutlineColor;
    #else
    return 0;
    #endif
}

v2f_OutlineNormal DepthNormalsVertex_Outline(a2v_Outline input)
{
    v2f_OutlineNormal output = (v2f_OutlineNormal)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 normal = 0;

    #if _USEOUTLINE
				
				output.uv = input.texcoord;
					            
    #if UV2_AS_NORMALS
				float3 normalTS = OctahedronToUnitVector(input.uv1.xy);
				normalTS = normalize(normalTS);
				float3 normalOS = normalize(input.normalOS);
				float3 tangentOS = normalize(input.tangentOS);
				float sign = input.tangentOS.w * GetOddNegativeScale();
				float3 bitangentOS = cross(normalOS, tangentOS) * sign;
				  bitangentOS  = normalize(bitangentOS);
				float3x3 tangentToNormal = float3x3(input.tangentOS.xyz, bitangentOS,
					                               input.normalOS.xyz);
				normal = mul(normalTS, tangentToNormal);

				// float3x3 tanToWorld = float3x3(float3(tangentOS.x, bitangentOS.x, normalOS.x),
				//                    float3(tangentOS.y, bitangentOS.y, normalOS.y),
				//                    float3(tangentOS.z, bitangentOS.z, normalOS.z));
				// float3 normal = float3(dot(tanToWorld[0], normalTS), dot(tanToWorld[1], normalTS), dot(tanToWorld[2], normalTS));
				//normal = normalTS;

    #elif  _Normal
				normal = input.normalOS.xyz;
				
    #elif COLORS_AS_NORMALS
				
				normal = GetNormalOS(normalize(input.color.rgb * 2 - 1), input);
				
    #endif
				
				normal= normalize(normal);

				//将法线转到观察空间
				normal = mul((float3x3)UNITY_MATRIX_IT_MV, normal);
				normal.z = 0.001f;
				normal = normalize(normal); 
				//将顶点转到观察空间
				float3 positionVS = mul(GetWorldToViewMatrix(), float4(mul(GetObjectToWorldMatrix(), float4(input.positionOS.xyz, 1.0)).xyz, 1.0)).xyz;
				float3 viewDir = normalize(positionVS.xyz);

				///https://zhuanlan.zhihu.com/p/98948117?utm_source=qq
				///增加了一个linewidth的操作，因为unity_CameraProjection[1].y其实就是cos（FOV/2），所以这个操作的根本目的是为了保证轮廓线随着FOV的变换也是成一定比例，同时也不会随着镜头离物体的远近距离而变化。
				// y = cos（fov/2）
				float linewidth = -positionVS.z / (unity_CameraProjection[1].y);
				linewidth = sqrt(linewidth);
				linewidth *=0.01;
				linewidth *= _Outline_Width;
				linewidth *= input.color.a;
				//linewidth *= input.color.x;
				positionVS.z += _Offset_Z * 0.001;
				positionVS.xy = normal.xy * linewidth + positionVS.xy;
            	
				output.positionCS = TransformWViewToHClip(positionVS);
				//output.positionCS.z +=0.001 * _Offset_Z;
    #else
    output.uv = input.texcoord;
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.positionCS.z = output.positionCS.w + 1.0f; //不开启描边时，默认让描边无法通过裁剪矩阵测试
    #endif

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz); //input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.normalWS = half3(normalInput.normalWS);
    #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR) || defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
			        float sign = input.tangentOS.w * float(GetOddNegativeScale());
			        half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
    #endif

    #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
			        output.tangentWS = tangentWS;
    #endif

    #if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
			        half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
			        half3 viewDirTS = GetViewDirectionTangentSpace(tangentWS, output.normalWS, viewDirWS);
			        output.viewDirTS = viewDirTS;
    #endif

    return output;
}

void DepthNormalsFragment_Outline(
    v2f_OutlineNormal input
    , out half4 outNormalWS : SV_Target0
    #ifdef _WRITE_RENDERING_LAYERS
			    , out float4 outRenderingLayers : SV_Target1
    #endif
)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #if defined(_ALPHATEST_ON)
			        Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
    #endif

    #if defined(LOD_FADE_CROSSFADE)
			        LODFadeCrossFade(input.positionCS);
    #endif

    #if defined(_GBUFFER_NORMALS_OCT)
			        float3 normalWS = normalize(input.normalWS);
			        float2 octNormalWS = PackNormalOctQuadEncode(normalWS);           // values between [-1, +1], must use fp32 on some platforms
			        float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);   // values between [ 0,  1]
			        half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);      // values between [ 0,  1]
			        outNormalWS = half4(packedNormalWS, 0.0);
    #else
    #if defined(_PARALLAXMAP)
    #if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
			                half3 viewDirTS = input.viewDirTS;
    #else
			                half3 viewDirTS = GetViewDirectionTangentSpace(input.tangentWS, input.normalWS, input.viewDirWS);
    #endif
			            ApplyPerPixelDisplacement(viewDirTS, input.uv);
    #endif

    #if defined(_NORMALMAP) || defined(_DETAIL)
			            float sgn = input.tangentWS.w;      // should be either +1 or -1
			            float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
			            float3 normalTS = SampleNormal(input.uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);

    #if defined(_DETAIL)
			                half detailMask = SAMPLE_TEXTURE2D(_DetailMask, sampler_DetailMask, input.uv).a;
			                float2 detailUv = input.uv * _DetailAlbedoMap_ST.xy + _DetailAlbedoMap_ST.zw;
			                normalTS = ApplyDetailNormal(detailUv, normalTS, detailMask);
    #endif

			            float3 normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));
    #else
    float3 normalWS = input.normalWS;
    #endif

    outNormalWS = half4(NormalizeNormalPerPixel(normalWS), 0.0);
    #endif

    #ifdef _WRITE_RENDERING_LAYERS
			        uint renderingLayers = GetMeshRenderingLayer();
			        outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
    #endif
}


#endif
