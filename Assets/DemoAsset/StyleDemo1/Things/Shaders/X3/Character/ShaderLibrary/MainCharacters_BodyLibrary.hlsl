#ifndef MAINCHARACTERS_BODYLIBRARY
#define MAINCHARACTERS_BODYLIBRARY


#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/Common.hlsl"
#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/SYPackages/ShaderLibrary/AdditionalInput.hlsl"
#include "../../Scene/ShaderInclude/ShaderOtherFunction.hlsl"


//=================================================================//
//                          基础结构体                              //
//=================================================================//

// 顶点输入结构
struct a2v_Scene
{ 
    float4 positionOS           : POSITION;
    float4 normalOS             : NORMAL;
    float4 tangentOS            : TANGENT;
    float2 uv                   : TEXCOORD0;
};

// 顶点输入结构
struct a2v_Scene_Fur
{
	float4 positionOS           : POSITION;
	float4 normalOS             : NORMAL;
	float4 tangentOS            : TANGENT;
	float2 uv                   : TEXCOORD0;
	float4 color                : COLOR;
};

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
struct v2f_Scene
{
    float4 positionCS           : SV_POSITION;
    float2 uv                   : TEXCOORD0; // xy: 主帖图UV
    float4 shadowCoord          : TEXCOORD1;
    float3 positionWS           : TEXCOORD2;
    float4 tangentWS             : TEXCOORD3; // xyz: tangent, w: viewDir.x
    float4 bitangentWS           : TEXCOORD4; // xyz: bitangent, w: viewDir.y
    float4 normalWS              : TEXCOORD5; // xyz: normal, w: viewDir.z
    half4 vertexSH              : TEXCOORD6;// xyz: SH, w: 雾效因子
	float4 screenPos          : TEXCOORD7;// NDC 空间的坐标
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

// 顶点着色器输出结构
struct v2f_UiShow
{
    float4 positionCS           : SV_POSITION;
    float2 uv                   : TEXCOORD0; // xy: 主帖图UV
    float4 screenPos          : TEXCOORD1;
    float3 positionWS           : TEXCOORD2;
    float4 tangentWS             : TEXCOORD3; // xyz: tangent, w: viewDir.x
    float4 bitangentWS           : TEXCOORD4; // xyz: bitangent, w: viewDir.y
    float4 normalWS              : TEXCOORD5; // xyz: normal, w: viewDir.z
	//half4 test                  : TEXCOOTD6; //调试毛发
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
//=============================END=================================//



//=================================================================//
//                          顶点着色器                              //
//=================================================================//
float3 tangentNormal(float3 normal, float4 tangent, float3 worldNormal, float3 worldPos)
{

	float3 worldTangent = mul((float3x3)unity_ObjectToWorld, tangent.xyz);
	float3 worldBinormal = cross(worldNormal, worldTangent) * tangent.w;

	float4 TtoW0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
	float4 TtoW1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
	float4 TtoW2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);

	return float3(dot(TtoW0.xyz, normal), dot(TtoW1.xyz, normal), dot(TtoW2.xyz, normal));

}
v2f_Scene Vertex_Scene(a2v_Scene i)
{
    v2f_Scene o;

    //Float
    VertexPositionInputs vertexInput = GetVertexPositionInputs(i.positionOS.xyz);
    o.positionCS = vertexInput.positionCS;

    //UV
    o.uv.xy = i.uv.xy;

    //ShadowMap UV
    // #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    o.shadowCoord = TransformWorldToShadowCoord(vertexInput.positionWS.xyz);
    // #else
    // o.shadowCoord = 0;
    // #endif

    //世界坐标
    o.positionWS = vertexInput.positionWS.xyz;
    
    //Half
    //TBN
    o.tangentWS.xyz = TransformObjectToWorldDir(i.tangentOS.xyz);
    o.normalWS.xyz = TransformObjectToWorldNormal(i.normalOS.xyz);
    o.bitangentWS.xyz = normalize(cross(o.normalWS.xyz, o.tangentWS.xyz)) * i.tangentOS.w;
    

    //观察方向
	float3 cameraPos = GetCameraPositionWS();
	cameraPos.y -= _SpecularYPosOffset;
            	
	float3 viewDirWS = cameraPos - vertexInput.positionWS.xyz;
    // half3 viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS.xyz);
    
    o.tangentWS.w = viewDirWS.x;
    o.bitangentWS.w = viewDirWS.y;
    o.normalWS.w = viewDirWS.z;

    //环境光
    o.vertexSH.xyz = SampleSH(o.normalWS.xyz).xyz;

    //雾效因子
    o.vertexSH.w = ComputeFogFactor(vertexInput.positionCS.z);

	//屏幕空间坐标
	o.screenPos = ComputeScreenPos(o.positionCS);
	
    return o;
}
v2f_Scene Vertex_Fur(a2v_Scene_Fur i, half FUR_OFFSET =0)
{
	v2f_Scene o;
	
	//TBN
	o.tangentWS.xyz = TransformObjectToWorldDir(i.tangentOS.xyz);
	o.normalWS.xyz = TransformObjectToWorldNormal(i.normalOS.xyz);
	o.bitangentWS.xyz = normalize(cross(o.normalWS.xyz, o.tangentWS.xyz)) * i.tangentOS.w;

	//短绒毛
	// half3 direction = lerp(o.normalWS, TransformObjectToWorld(_Gravity) * _GravityStrength + o.normalWS * (1 - _GravityStrength), _FUR_OFFSET) ;
	//长毛
	// half3 direction = lerp(IN.normal, _Gravity * _GravityStrength + IN.normal, _FUR_OFFSET);
	// 扰动
	// half4 DirTex = SAMPLE_TEXTURE2D_LOD(_XFurGroomData,sampler_XFurGroomData,i.uv.xy * _XFurGroomData_ST.xy + _XFurGroomData_ST.zw,0);
	// half3 furDir = lerp(0, tangentNormal((DirTex.xyz) * DirTex.w, i.tangentOS, TransformObjectToWorld(i.normalOS.xyz), mul(unity_ObjectToWorld, i.positionOS.xyz)), 1);
	// furDir = mul(unity_WorldToObject, furDir);
	// float offsetU = (DirTex.r - SAMPLE_TEXTURE2D_LOD(_XFurGroomData, sampler_XFurGroomData,i.uv + _XFurGroomData_TexelSize * float2(1, 0),0).r) * 1;
	// float offsetV = (DirTex.r - SAMPLE_TEXTURE2D_LOD(_XFurGroomData, sampler_XFurGroomData,i.uv + _XFurGroomData_TexelSize * float2(0, -1),0).r) * 1;
	// direction = lerp(direction , normalize(direction + float3(offsetU, offsetV, 0) * _FUR_OFFSET)  , _Noise);

	//方向
	float2 xy1 =  (SAMPLE_TEXTURE2D_LOD(_FlowMap, sampler_FlowMap, i.uv.xy,0) ).xy * 2.0 - 1.0;
	float  zz  =  max(1.0e-16, sqrt(1.0 - saturate(dot(xy1, xy1))));
	float3 WS = normalize(TransformTangentToWorld(float3(xy1, zz), half3x3(o.tangentWS.xyz, o.bitangentWS.xyz, o.normalWS.xyz)));
	half3 direction = lerp(o.normalWS.xyz, WS , _UVOffset);
	direction = lerp(direction, (_Gravity) * _GravityStrength + direction * (1 - _GravityStrength), _FUR_OFFSET) ;
	//o.test.xyz = direction;//调试
	
	//i.positionOS.xyz += normalize(direction + furDir) * _FurLength * _FUR_OFFSET * i.color.a + i.normalOS * - _SuoNormal * i.color.r;
	//i.positionOS.xyz += 0;//direction * _FurLength * _FUR_OFFSET * i.color.a + i.normalOS * - _SuoNormal * i.color.r;
	
	//VertexPositionInputs vertexInput = GetVertexPositionInputs(i.positionOS.xyz);
	o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
	
	float3 testWS = o.positionWS;
	float4 testCS = TransformWorldToHClip(o.positionWS.xyz);
	
	o.positionWS += direction * _FurLength * _FUR_OFFSET * i.color.a + o.normalWS * - _SuoNormal * i.color.r;
	o.positionCS = TransformWorldToHClip(o.positionWS.xyz);
	
	//UV
	o.uv.xy = i.uv.xy;

	//ShadowMap UV
	#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	o.shadowCoord = TransformWorldToShadowCoord(o.positionWS.xyz);
	#else
	o.shadowCoord = 0;
	#endif

	//世界坐标
	o.positionWS = o.positionWS.xyz;
	
	//观察方向
	half3 viewDirWS = GetWorldSpaceViewDir(o.positionWS.xyz);
    
	o.tangentWS.w = viewDirWS.x;
	o.bitangentWS.w = viewDirWS.y;
	o.normalWS.w = viewDirWS.z;

	//环境光
	o.vertexSH.xyz = SampleSH(o.normalWS.xyz).xyz;

	//雾效因子
	o.vertexSH.w = ComputeFogFactor(o.positionCS.z);

	// NDC 空间
	o.screenPos = ComputeScreenPos(testCS);
	
	return o;
}

v2f_UiShow Vertex_UiShow(a2v_Scene i)
{
    v2f_UiShow o;

    //Float
    VertexPositionInputs vertexInput = GetVertexPositionInputs(i.positionOS.xyz);
    // o.positionCS = vertexInput.positionCS;
    o.positionCS = mul(_CustomShowMatrix_P, mul(_CustomShowMatrix_V, float4(vertexInput.positionWS.xyz, 1.0)));
                
    //正交、透视深度映射
    float4 sourcePositionCS = TransformWorldToHClip(vertexInput.positionWS.xyz);
    o.positionCS.z = sourcePositionCS.z * o.positionCS.w;

    o.screenPos = ComputeScreenPos(o.positionCS);

    //UV
    o.uv.xy = i.uv.xy;

    //世界坐标
    o.positionWS = vertexInput.positionWS.xyz;
    
    //Half
    //TBN
    o.tangentWS.xyz = TransformObjectToWorldDir(i.tangentOS.xyz);
    o.normalWS.xyz = TransformObjectToWorldNormal(i.normalOS.xyz);
    o.bitangentWS.xyz = normalize(cross(o.normalWS.xyz, o.tangentWS.xyz)) * i.tangentOS.w;
    

    //观察方向
    half3 viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS.xyz);

    //UI相机虚拟位置
     // float3 UiCameraPos = unity_ObjectToWorld._14_24_34;
     // UiCameraPos.z -= 1;
     // UiCameraPos.y -= 0.28;
     //
     // half3 viewDirWS = UiCameraPos - vertexInput.positionWS.xyz;
    
    o.tangentWS.w = viewDirWS.x;
    o.bitangentWS.w = viewDirWS.y;
    o.normalWS.w = viewDirWS.z;

    return o;
}
v2f_UiShow Vertex_FurUi(a2v_Scene_Fur i, half FUR_OFFSET =0)
{
	v2f_UiShow o;
	//短绒毛
	half3 direction = lerp(i.normalOS, _Gravity * _GravityStrength + i.normalOS * (1 - _GravityStrength), _FUR_OFFSET) ;
	//长毛
	// half3 direction = lerp(IN.normal, _Gravity * _GravityStrength + IN.normal, _FUR_OFFSET);
	
	//方向
	half4 DirTex = SAMPLE_TEXTURE2D_LOD(_XFurGroomData,sampler_XFurGroomData,i.uv.xy * _XFurGroomData_ST.xy +  _XFurGroomData_ST.zw,0);
	half3 furDir = lerp(0, tangentNormal((DirTex.xyz) * DirTex.w, i.tangentOS, TransformObjectToWorld(i.normalOS.xyz), mul(unity_ObjectToWorld, i.positionOS.xyz)), 1);
	//furDir = mul(unity_WorldToObject, furDir);
	float offsetU = (DirTex.r - SAMPLE_TEXTURE2D_LOD(_XFurGroomData, sampler_XFurGroomData, i.uv + _XFurGroomData_TexelSize * float2(1, 0),0).r) * 1;
	float offsetV = (DirTex.r - SAMPLE_TEXTURE2D_LOD(_XFurGroomData, sampler_XFurGroomData, i.uv + _XFurGroomData_TexelSize * float2(0, -1),0).r) * 1;

	direction = lerp(direction , normalize(direction + float3(offsetU, offsetV, 0) *  _FUR_OFFSET) , _Noise);
	//o.test.xyz = direction;
	
	i.positionOS.xyz += direction * _FurLength * _FUR_OFFSET * i.color.a + i.normalOS * - _SuoNormal * i.color.r;

	//Float
	VertexPositionInputs vertexInput = GetVertexPositionInputs(i.positionOS.xyz);
	// o.positionCS = vertexInput.positionCS;
	o.positionCS = mul(_CustomShowMatrix_P, mul(_CustomShowMatrix_V, float4(vertexInput.positionWS.xyz, 1.0)));
                
	//正交、透视深度映射
	float4 sourcePositionCS = TransformWorldToHClip(vertexInput.positionWS.xyz);
	o.positionCS.z = sourcePositionCS.z * o.positionCS.w;

	o.screenPos = ComputeScreenPos(o.positionCS);

	//UV
	o.uv.xy = i.uv.xy;

	//世界坐标
	o.positionWS = vertexInput.positionWS.xyz;
    
	//Half
	//TBN
	o.tangentWS.xyz = TransformObjectToWorldDir(i.tangentOS.xyz);
	o.normalWS.xyz = TransformObjectToWorldNormal(i.normalOS.xyz);
	o.bitangentWS.xyz = normalize(cross(o.normalWS.xyz, o.tangentWS.xyz)) * i.tangentOS.w;
    

	//观察方向
	half3 viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS.xyz);

	//UI相机虚拟位置
	// float3 UiCameraPos = unity_ObjectToWorld._14_24_34;
	// UiCameraPos.z -= 1;
	// UiCameraPos.y -= 0.28;
	//
	// half3 viewDirWS = UiCameraPos - vertexInput.positionWS.xyz;
    
	o.tangentWS.w = viewDirWS.x;
	o.bitangentWS.w = viewDirWS.y;
	o.normalWS.w = viewDirWS.z;

	//o.test.xyz = furDir;
	return o;
}

float3 OctahedronToUnitVector(float2 oct)
{
    // float3 unitVec = float3(oct, 1 - dot(float2(1, 1), abs(oct)));
    //
    // if (unitVec.z < 0)
    // {
    //     unitVec.xy = (1 - abs(unitVec.yx)) *(unitVec.xy >= 0 ? float2(1, 1) : float2(-1, -1));
    // }
    float3  unitVec = float3(oct.x, oct.y, 1.0f - abs(oct.x) -  abs(oct.y));
    float t = max( -unitVec.z, 0.0f );
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
		output.positionCS.z = output.positionCS.w + 1.0f;//不开启描边时，默认让描边无法通过裁剪矩阵测试
	#endif
	return output;

}

v2f_Outline OutLineOctahedronPassVertex_UiShow(a2v_Outline input)
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
	// normal = mul((float3x3)UNITY_MATRIX_IT_MV, normal);
	normal = TransformObjectToWorldNormal(normal);
	
	float3 normal_Source = TransformWorldToViewDir(normal);
	normal = mul((float3x3)_CustomShowMatrix_V, normal);
	
	normal.z = 0.001f;
	normal = normalize(normal);

	normal_Source.z = 0.001f;
	normal_Source = normalize(normal_Source);
	
	//将顶点转到观察空间
	float3 positionVS_Source = mul(GetWorldToViewMatrix(), float4(mul(GetObjectToWorldMatrix(), float4(input.positionOS.xyz, 1.0)).xyz, 1.0)).xyz;
	float3 positionVS = mul(_CustomShowMatrix_V, float4(mul(GetObjectToWorldMatrix(), float4(input.positionOS.xyz, 1.0)).xyz, 1.0)).xyz;
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

	positionVS_Source.z += _Offset_Z * 0.001;
	positionVS_Source.xy = normal_Source.xy * linewidth + positionVS_Source.xy;
  
	output.color.xyz = input.color.xyz;
	
	float4 positionCS_Source = TransformWViewToHClip(positionVS_Source);
	output.positionCS = mul(_CustomShowMatrix_P, float4(positionVS, 1.0));

	output.positionCS.z = positionCS_Source.z * output.positionCS.w;
	
	//output.positionCS.z +=0.001 * _Offset_Z;
	#else
		output.color = input.color;
		output.uv = input.texcoord;

		float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
	
		float3 positionCS_Source = TransformWorldToHClip(positionWS.xyz).xyz;
	
		output.positionCS = mul(_CustomShowMatrix_P, mul(_CustomShowMatrix_V, float4(positionWS, 1.0)));
		output.positionCS.z = positionCS_Source.z * output.positionCS.w;
	#endif

	return output;

}



//=============================END=================================//



//=================================================================//
//                          特殊结构体                              //
//=================================================================//
struct TextureData
{
    float2 uv;
    
    //颜色
    half3 albedo;
    half alpha;

    //法线BA通道
    half specularIntensity;

    //lightMap
    half metallic;
    half emission;
    half rampID;
    half roughness;
    half smoothness;

    //maskMap
    half anisotropyMask;
    half SilkStockingMask;

	half ao;
};

struct LightVectorData
{
    //向量
    float3 positionWS;
	float4 positionCS;
	float4 screenPos;

    half3 tangentWS;
    half3 bitangentWS;
    half3 normalTS;
    half3 normalWS;
    float3 viewDirectionWS;
    half3 lightDirWS;

    half NdotL;
    half halfLambert;
    half3 halfDir;
    half NdotH;
    half NdotVNoClamp;
    half NdotV;
};

struct LightColorData
{
    //基础颜色
    half3 bakedGI;
    half fogCoord;

    //光照颜色
    half3 lightColor;
    half atten;
	half absolute;//给边缘光做遮罩
    half addLightAtten;//多光源更改投影强度

    half3 diffuseAlbedo;
    half3 specularColor;
    half3 envColor;

	half grazingTerm;

	//多光源设置
	half multipleLightBrightness;
	half multipleLightRange;
	half multipleLightSoftness;
};
//=============================END=================================//



//=================================================================//
//                          初始化数据                              //
//=================================================================//
inline void InitializeTextureData_Scene(v2f_Scene i, out TextureData textureData)
{
    textureData = (TextureData)0;
    textureData.uv = i.uv.xy;
    
    //==============颜色==============
    //固有色、Alpha
    half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv.xy);

    textureData.albedo = albedoAlpha.rgb * _BaseColor.rgb;
    textureData.alpha = albedoAlpha.a * _BaseColor.a;
    
    
    //==============遮罩图==============
    half4 lightMap = SAMPLE_TEXTURE2D(_LightMap, sampler_LightMap, i.uv.xy);// R:金属度 G:AO B:高光强度 A:光滑度
    half4 maskMap = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, i.uv.xy);// R:RampID G:环境高光强度 B:自发光 A:空

    textureData.metallic = lightMap.r * _Metallic;
    textureData.emission = lightMap.g;
    textureData.rampID = lightMap.b;
	textureData.smoothness = lightMap.a * _Smoothness;
	

    half roughness = 1.0h - textureData.smoothness;
    roughness *= roughness;
    textureData.roughness = roughness;
    
    
    
    textureData.anisotropyMask = maskMap.r;
    textureData.SilkStockingMask = maskMap.g;

    textureData.specularIntensity = 1.0h;
	textureData.ao = 1.0h;
}

inline void InitializeTextureData_Fur(v2f_Scene i, out TextureData textureData)
{
	textureData = (TextureData)0;
	textureData.uv = i.uv.xy;
    
	//==============颜色==============
	//固有色、Alpha
	half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv.xy);

	textureData.albedo = albedoAlpha.rgb * _BaseColor.rgb;
	textureData.alpha = albedoAlpha.a * _BaseColor.a;
    
    
	//==============遮罩图==============
	half4 lightMap = SAMPLE_TEXTURE2D(_LightMap, sampler_LightMap, i.uv.xy);// R:金属度 G:AO B:高光强度 A:光滑度
	half4 maskMap = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, i.uv.xy);// R:RampID G:环境高光强度 B:自发光 A:空

	textureData.metallic = lightMap.r * _Metallic;
	textureData.emission = lightMap.g;
	textureData.rampID = lightMap.b;
	textureData.smoothness = lightMap.a * _Smoothness;
	

	half roughness = 1.0h - textureData.smoothness;
	roughness *= roughness;
	textureData.roughness = roughness;
    
    
    
	textureData.anisotropyMask = maskMap.r;
	textureData.SilkStockingMask = maskMap.g;

	textureData.specularIntensity = 1.0h;
	textureData.ao = 1.0h;
}

inline void InitializeTextureData_UiShow(v2f_UiShow i, out TextureData textureData)
{
    textureData = (TextureData)0;
    textureData.uv = i.uv.xy;
    
    //==============颜色==============
    //固有色、Alpha
    half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv.xy);

    textureData.albedo = albedoAlpha.rgb * _BaseColor.rgb;
    textureData.alpha = albedoAlpha.a * _BaseColor.a;
    
    
    //==============遮罩图==============
    half4 lightMap = SAMPLE_TEXTURE2D(_LightMap, sampler_LightMap, i.uv.xy);// R:金属度 G:AO B:高光强度 A:光滑度
    half4 maskMap = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, i.uv.xy);// R:RampID G:环境高光强度 B:自发光 A:空

	textureData.metallic = lightMap.r * _Metallic;
	textureData.emission = lightMap.g;
	textureData.rampID = lightMap.b;
	textureData.smoothness = lightMap.a * _Smoothness;
	

	half roughness = 1.0h - textureData.smoothness;
	roughness *= roughness;
	textureData.roughness = roughness;


    textureData.anisotropyMask = maskMap.r;
    textureData.SilkStockingMask = maskMap.g;

    textureData.specularIntensity = 1.0h;

	textureData.ao = 1.0h;
}

inline void InitializeTextureData_FurUi(v2f_UiShow i, out TextureData textureData)
{
	textureData = (TextureData)0;
	textureData.uv = i.uv.xy;
    
	//==============颜色==============
	//固有色、Alpha
	half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv.xy);

	textureData.albedo = albedoAlpha.rgb * _BaseColor.rgb;
	textureData.alpha = albedoAlpha.a * _BaseColor.a;
    
    
	//==============遮罩图==============
	half4 lightMap = SAMPLE_TEXTURE2D(_LightMap, sampler_LightMap, i.uv.xy);// R:金属度 G:AO B:高光强度 A:光滑度
	half4 maskMap = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, i.uv.xy);// R:RampID G:环境高光强度 B:自发光 A:空

	textureData.metallic = lightMap.r * _Metallic;
	textureData.emission = lightMap.g;
	textureData.rampID = lightMap.b;
	textureData.smoothness = lightMap.a * _Smoothness;
	

	half roughness = 1.0h - textureData.smoothness;
	roughness *= roughness;
	textureData.roughness = roughness;


	textureData.anisotropyMask = maskMap.r;
	textureData.SilkStockingMask = maskMap.g;

	textureData.specularIntensity = 1.0h;
	textureData.ao = 1.0h;
}


inline void  InitializeLightVectorData_Scene(v2f_Scene i, inout TextureData textureData, out LightVectorData lightVectorData)
{
	lightVectorData = (LightVectorData) 0;
	
    //==============向量==============
    lightVectorData.positionWS = i.positionWS;
	lightVectorData.positionCS = i.positionCS;
	lightVectorData.screenPos = i.screenPos;
    
    //法线
    half4 normalMask = SAMPLE_TEXTURE2D(_BumpMaskMap, sampler_BumpMaskMap, i.uv.xy);
    textureData.specularIntensity = normalMask.b;
    
    half4 tangentNormal = half4(0, normalMask.y, 0, normalMask.x);
	tangentNormal = _BumpMaskMap_TexelSize.x == 1 ? half4(0, 0.5, 0, 0.5) : tangentNormal;//如果贴图为空
	
    tangentNormal.xyz = UnpackNormalAG(tangentNormal, _BumpStrength);
    // tangentNormal.xyz = UnpackNormalAG(tangentNormal);

    // tangentNormal.xyz = UnpackNormalScale(tangentNormal, _BumpStrength);
    lightVectorData.normalTS = tangentNormal.xyz;
    
    lightVectorData.normalWS = normalize(TransformTangentToWorld(lightVectorData.normalTS, half3x3(i.tangentWS.xyz, i.bitangentWS.xyz, i.normalWS.xyz)));

    //普通向量计算
    lightVectorData.viewDirectionWS = normalize(float3(i.tangentWS.w, i.bitangentWS.w, i.normalWS.w));

    
    lightVectorData.lightDirWS = half3(_MainLightPosition.xyz);

    //各向异性高光所需向量
    lightVectorData.tangentWS = Orthonormalize(i.tangentWS.xyz, lightVectorData.normalWS.xyz);
    lightVectorData.bitangentWS = normalize(i.bitangentWS.xyz);
    
    
}

inline void  InitializeLightVectorData_Fur(v2f_Scene i, inout TextureData textureData, out LightVectorData lightVectorData)
{
	lightVectorData = (LightVectorData) 0;
	
	//==============向量==============
	lightVectorData.positionWS = i.positionWS;
	lightVectorData.positionCS = i.positionCS;
	lightVectorData.screenPos = i.screenPos;
    
	//法线
	half4 normalMask = SAMPLE_TEXTURE2D(_BumpMaskMap, sampler_BumpMaskMap, i.uv.xy);
	textureData.specularIntensity = normalMask.b;
    
	half4 tangentNormal = half4(0, normalMask.y, 0, normalMask.x);
	tangentNormal = _BumpMaskMap_TexelSize.x == 1 ? half4(0, 0.5, 0, 0.5) : tangentNormal;//如果贴图为空
	
	// tangentNormal.xyz = UnpackNormalAG(tangentNormal, _BumpStrength);
	tangentNormal.xyz = UnpackNormalAG(tangentNormal, _BumpScale);

	// tangentNormal.xyz = UnpackNormalScale(tangentNormal, _BumpStrength);
	// half4 FurnormalMask = SAMPLE_TEXTURE2D(_LayerNormalTex, sampler_LayerNormalTex, i.uv.xy);
	// FurnormalMask.xy = FurnormalMask.xy * 2.0 - 1.0;
	// FurnormalMask.z = max(1.0e-16, sqrt(1.0 - saturate(dot(FurnormalMask.xy, FurnormalMask.xy))));
	// lightVectorData.normalTS = BlendNormal( UnpackNormal(FurnormalMask) , tangentNormal);
	lightVectorData.normalTS = tangentNormal.xyz;
    
	lightVectorData.normalWS = normalize(TransformTangentToWorld(lightVectorData.normalTS, half3x3(i.tangentWS.xyz, i.bitangentWS.xyz, i.normalWS.xyz)));

	//普通向量计算
	lightVectorData.viewDirectionWS = normalize(half3(i.tangentWS.w, i.bitangentWS.w, i.normalWS.w));

    
	lightVectorData.lightDirWS = half3(_MainLightPosition.xyz);

	//各向异性高光所需向量
	lightVectorData.tangentWS = Orthonormalize(i.tangentWS.xyz, lightVectorData.normalWS.xyz);
	lightVectorData.bitangentWS = normalize(i.bitangentWS.xyz);
}

inline void  InitializeLightVectorData_UiShow(v2f_UiShow i, inout TextureData textureData, out LightVectorData lightVectorData)
{
	lightVectorData = (LightVectorData) 0;
	
    //==============向量==============
    lightVectorData.positionWS = i.positionWS;
	lightVectorData.positionCS = i.positionCS;
	lightVectorData.screenPos = i.screenPos;
    
    //法线
    half4 normalMask = SAMPLE_TEXTURE2D(_BumpMaskMap, sampler_BumpMaskMap, i.uv.xy);
    textureData.specularIntensity = normalMask.b;
    
    half4 tangentNormal = half4(0, normalMask.y, 0, normalMask.x);
	tangentNormal = _BumpMaskMap_TexelSize.x == 1 ? half4(0, 0.5, 0, 0.5) : tangentNormal;//如果贴图为空
	
    // tangentNormal.xyz = UnpackNormalAG(tangentNormal, _BumpStrength);
    tangentNormal.xyz = UnpackNormalAG(tangentNormal);

    // tangentNormal.xyz = UnpackNormalScale(normalMask.xyzw, _BumpStrength);
    lightVectorData.normalTS = tangentNormal.xyz;
    
    lightVectorData.normalWS = normalize(TransformTangentToWorld(lightVectorData.normalTS, half3x3(i.tangentWS.xyz, i.bitangentWS.xyz, i.normalWS.xyz)));
    
    //普通向量计算
    lightVectorData.viewDirectionWS = normalize(half3(i.tangentWS.w, i.bitangentWS.w, i.normalWS.w));
    lightVectorData.lightDirWS = _CharacterLightUiDir.xyz;

    half NdotL = dot(lightVectorData.normalWS, lightVectorData.lightDirWS);
    lightVectorData.NdotL = NdotL;
    lightVectorData.halfLambert = NdotL * 0.5 + 0.5;
    
    half3 halfDir = SafeNormalize(lightVectorData.lightDirWS + lightVectorData.viewDirectionWS);
    lightVectorData.NdotH = saturate(dot(lightVectorData.normalWS.xyz, halfDir.xyz));

    lightVectorData.NdotVNoClamp = dot(lightVectorData.normalWS, lightVectorData.viewDirectionWS);
    lightVectorData.NdotV = saturate(lightVectorData.NdotVNoClamp);

    //各向异性高光所需向量
    
    lightVectorData.tangentWS = Orthonormalize(i.tangentWS.xyz, lightVectorData.normalWS.xyz);
    lightVectorData.bitangentWS = normalize(i.bitangentWS.xyz);
    
    lightVectorData.halfDir = halfDir;
}

inline void  InitializeLightVectorData_FurUi(v2f_UiShow i, inout TextureData textureData, out LightVectorData lightVectorData)
{
	lightVectorData = (LightVectorData) 0;
	
	//==============向量==============
	lightVectorData.positionWS = i.positionWS;
	lightVectorData.positionCS = i.positionCS;
	lightVectorData.screenPos = i.screenPos;
    
	//法线
	half4 normalMask = SAMPLE_TEXTURE2D(_BumpMaskMap, sampler_BumpMaskMap, i.uv.xy);
	textureData.specularIntensity = normalMask.b;
    
	half4 tangentNormal = half4(0, normalMask.y, 0, normalMask.x);
	tangentNormal = _BumpMaskMap_TexelSize.x == 1 ? half4(0, 0.5, 0, 0.5) : tangentNormal;//如果贴图为空
	
	// tangentNormal.xyz = UnpackNormalAG(tangentNormal, _BumpStrength);
	tangentNormal.xyz = UnpackNormalAG(tangentNormal, _BumpScale);

	// tangentNormal.xyz = UnpackNormalScale(tangentNormal, _BumpStrength);
	lightVectorData.normalTS = tangentNormal.xyz;
    
	lightVectorData.normalWS = normalize(TransformTangentToWorld(lightVectorData.normalTS, half3x3(i.tangentWS.xyz, i.bitangentWS.xyz, i.normalWS.xyz)));
    
	//普通向量计算
	lightVectorData.viewDirectionWS = normalize(half3(i.tangentWS.w, i.bitangentWS.w, i.normalWS.w));
	lightVectorData.lightDirWS = _CharacterLightUiDir.xyz;

	half NdotL = dot(lightVectorData.normalWS, lightVectorData.lightDirWS);
	lightVectorData.NdotL = NdotL;
	lightVectorData.halfLambert = NdotL * 0.5 + 0.5;
    
	half3 halfDir = SafeNormalize(lightVectorData.lightDirWS + lightVectorData.viewDirectionWS);
	lightVectorData.NdotH = saturate(dot(lightVectorData.normalWS.xyz, halfDir.xyz));

	lightVectorData.NdotVNoClamp = dot(lightVectorData.normalWS, lightVectorData.viewDirectionWS);
	lightVectorData.NdotV = saturate(lightVectorData.NdotVNoClamp);

	//各向异性高光所需向量
    
	lightVectorData.tangentWS = Orthonormalize(i.tangentWS.xyz, lightVectorData.normalWS.xyz);
	lightVectorData.bitangentWS = normalize(i.bitangentWS.xyz);
    
	lightVectorData.halfDir = halfDir;
}


inline half LinearOneMinusReflectivityFromMetallic(half metallic)
{
    half oneMinusDielectricSpec = kDielectricSpec.a;
    return oneMinusDielectricSpec - metallic * oneMinusDielectricSpec;
}

inline void InitializeLighColorData_Scene(v2f_Scene i, inout TextureData textureData, LightVectorData lightVectorData, out LightColorData lighColorData)
{
	lighColorData = (LightColorData) 0;
	
    //基础颜色
    lighColorData.bakedGI = i.vertexSH.xyz;
    lighColorData.fogCoord = i.vertexSH.w;

    //光照颜色
    lighColorData.lightColor = _MainLightColor.rgb;
	
	// 原来的 shadowMap 采样代码
    half unityShadow = MainLightRealtimeShadow(i.shadowCoord);
	// half4 shadowMask = unity_ProbesOcclusion;
	// half unityShadow = MainLightPCFShadow(i.shadowCoord, lightVectorData.positionWS, shadowMask, _MainLightOcclusionProbes);

	
	// 逐对象引用的 shadowMap 采样代码
	half highQualityShadow = SAMPLE_TEXTURE2D(_ScreenSpaceShadowmapTexture, sampler_ScreenSpaceShadowmapTexture, i.screenPos.xy / i.screenPos.w).x;

	half atten = min(unityShadow, highQualityShadow);
	lighColorData.absolute = atten;
	atten = LerpWhiteTo(atten, _ShadowIntensity);
	
	lighColorData.atten = atten;
    // lighColorData.atten = LerpWhiteTo(lighColorData.atten, _ShadowIntensity);

    half oneMinusReflectivity = LinearOneMinusReflectivityFromMetallic(textureData.metallic);
    lighColorData.diffuseAlbedo = textureData.albedo * oneMinusReflectivity;

    lighColorData.specularColor = lerp(kDieletricSpec.rgb, textureData.albedo, textureData.metallic);
    lighColorData.specularColor *= lerp(_SpecularColor.rgb, _SpecularColor.rgb * textureData.albedo, _SpecularBlendAlbedoIntensity);
	
    lighColorData.envColor = _EnvColor.rgb;

	lighColorData.grazingTerm = saturate(textureData.smoothness + 1.0 - oneMinusReflectivity);

	#if defined(_LIGHT_COOKIES)
	real3 cookieColor = SampleMainLightCookie(lightVectorData.positionWS);
	light.color *= cookieColor;
	#endif

	AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(GetNormalizedScreenSpaceUV(i.positionCS), textureData.ao/*surfData.occlusion*/);
	#if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT)
	if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_AMBIENT_OCCLUSION))
	{
		lighColorData.lightColor *= aoFactor.directAmbientOcclusion;
	}
	#endif
    
    lighColorData.addLightAtten = lighColorData.atten;//多光源更改投影强度

	textureData.ao = min(aoFactor.indirectAmbientOcclusion, textureData.ao);//HBAO

	lighColorData.multipleLightBrightness = 10.0h;
	lighColorData.multipleLightRange = 0.5h;
	lighColorData.multipleLightSoftness = 1.0h;
}

inline void InitializeLighColorData_Fur(v2f_Scene i, TextureData textureData, out LightColorData lighColorData)
{
	lighColorData = (LightColorData) 0;
	
	//基础颜色
	lighColorData.bakedGI = i.vertexSH.xyz;
	lighColorData.fogCoord = i.vertexSH.w;

	//光照颜色
	lighColorData.lightColor = _MainLightColor.rgb;

	// 原来的 shadowMap 采样代码
	half unityShadow = MainLightRealtimeShadow(i.shadowCoord);

	// 逐对象引用的 shadowMap 采样代码
	half highQualityShadow = SAMPLE_TEXTURE2D(_ScreenSpaceShadowmapTexture, sampler_ScreenSpaceShadowmapTexture, i.screenPos.xy / i.screenPos.w).x;

	half atten = min(unityShadow, highQualityShadow);
	lighColorData.absolute = atten;
	atten = LerpWhiteTo(atten, _ShadowIntensity);
	
	lighColorData.atten = atten;

	half oneMinusReflectivity = LinearOneMinusReflectivityFromMetallic(textureData.metallic);
	lighColorData.diffuseAlbedo = textureData.albedo * oneMinusReflectivity;

	lighColorData.specularColor = lerp(kDieletricSpec.rgb, textureData.albedo, textureData.metallic);
	lighColorData.specularColor *= lerp(_SpecularColor.rgb, _SpecularColor.rgb * textureData.albedo, _SpecularBlendAlbedoIntensity);
	
	lighColorData.envColor = _EnvColor.rgb;

	lighColorData.grazingTerm = saturate(textureData.smoothness + 1.0 - oneMinusReflectivity);
    
	lighColorData.addLightAtten = lighColorData.atten;//多光源更改投影强度

	lighColorData.multipleLightBrightness = 10.0h;
	lighColorData.multipleLightRange = 0.5h;
	lighColorData.multipleLightSoftness = 1.0h;
}

inline void CharacterLightControllerData_Scene(inout LightVectorData lightVectorData, inout LightColorData lighColorData)
{
	//场景灯光脚本
	if(_CharacterLightData.x > 0.0h)
	{
		// lighColorData.lightColor = _MainLightColor.rgb;
		lightVectorData.lightDirWS = _CharacterLightDir.xyz;
		lighColorData.lightColor = _CharacterLightColor.rgb * _CharacterLightData.w;
		lighColorData.atten = LerpWhiteTo(lighColorData.atten, _CharacterLightData.y);
	    
		lighColorData.specularColor *= _CharacterSpecularColor.rgb * _CharacterSpecularColor.a * 10;

		lighColorData.bakedGI = lerp(lighColorData.bakedGI, _characterBakeAmbientColor.rgb, _characterBakeAmbientColor.a);
		lighColorData.bakedGI *= _CharacterLightData.z;
		lighColorData.bakedGI += _CharacterAmbientColor.rgb * _CharacterAmbientColor.a;
		
		lighColorData.envColor *= _CharacterEnvColor.rgb;

		lighColorData.multipleLightBrightness = _CharacterMultipleLight.x;
		lighColorData.multipleLightRange = _CharacterMultipleLight.y;
		lighColorData.multipleLightSoftness = _CharacterMultipleLight.z;
	}
}

inline void CalculateLightVectorData_Scene(inout LightVectorData lightVectorData)
{
	half NdotL = dot(lightVectorData.normalWS, lightVectorData.lightDirWS);
	lightVectorData.NdotL = NdotL;
	lightVectorData.halfLambert = NdotL * 0.5 + 0.5;
    
	half3 halfDir = SafeNormalize(lightVectorData.lightDirWS + lightVectorData.viewDirectionWS);
	lightVectorData.halfDir = halfDir;
	lightVectorData.NdotH = saturate(dot(lightVectorData.normalWS.xyz, halfDir.xyz));

	lightVectorData.NdotVNoClamp = dot(lightVectorData.normalWS, lightVectorData.viewDirectionWS);
	lightVectorData.NdotV = saturate(lightVectorData.NdotVNoClamp);
}

inline void InitializeLighColorData_UiShow(v2f_UiShow i, TextureData textureData, LightVectorData lightVectorData, out LightColorData lighColorData)
{
	lighColorData = (LightColorData) 0;
	
    //基础颜色
    // lighColorData.bakedGI = _CharacterUIAmbientColor.rgb;
    lighColorData.fogCoord = 1;
    
    //=========================自定义SH计算==================================
    half3 shNormal = lightVectorData.viewDirectionWS;
    // half3 shNormal = lightVectorData.normalWS;
    half4 vA = half4(shNormal, 1.0);
				
    half3 x1;
    // Linear (L1) + constant (L0) polynomial terms
    x1.r = dot(custom_SHAr, vA);
    x1.g = dot(custom_SHAg, vA);
    x1.b = dot(custom_SHAb, vA);

    half3 x2;
    // 4 of the quadratic (L2) polynomials
    half4 vB = shNormal.xyzz * shNormal.yzzx;
    x2.r = dot(custom_SHBr, vB);
    x2.g = dot(custom_SHBg, vB);
    x2.b = dot(custom_SHBb, vB);

    // Final (5th) quadratic (L2) polynomial
    half vC = shNormal.x * shNormal.x - shNormal.y * shNormal.y;
    half3 x3 = custom_SHC.rgb * vC;
				
    half3 shColor = x1 + x2 + x3;

    lighColorData.bakedGI = shColor;

    //光照颜色
    lighColorData.lightColor = 1;

    
    half highQualityShadow = SAMPLE_TEXTURE2D(_ScreenSpaceShadowMap, sampler_ScreenSpaceShadowMap, i.screenPos.xy / i.screenPos.w).x;
	half atten = LerpWhiteTo(highQualityShadow, _ShadowIntensity);
    lighColorData.atten = atten;
	lighColorData.absolute = atten;

    
    half oneMinusReflectivity = LinearOneMinusReflectivityFromMetallic(textureData.metallic);
    lighColorData.diffuseAlbedo = textureData.albedo * oneMinusReflectivity;

    lighColorData.specularColor = lerp(kDieletricSpec.rgb, textureData.albedo, textureData.metallic);
    lighColorData.specularColor *= lerp(_SpecularColor.rgb, _SpecularColor.rgb * textureData.albedo, _SpecularBlendAlbedoIntensity);
	
    lighColorData.envColor = _EnvColor.rgb;

	lighColorData.grazingTerm = saturate(textureData.smoothness + 1.0 - oneMinusReflectivity);
    
    lighColorData.addLightAtten = 1;//多光源更改投影强度
}

inline void InitializeLighColorData_FurUi(v2f_UiShow i, TextureData textureData, LightVectorData lightVectorData, out LightColorData lighColorData)
{
	lighColorData = (LightColorData) 0;
	
	//基础颜色
	// lighColorData.bakedGI = _CharacterUIAmbientColor.rgb;
	lighColorData.fogCoord = 1;
    
	//=========================自定义SH计算==================================
	half3 shNormal = lightVectorData.viewDirectionWS;
	// half3 shNormal = lightVectorData.normalWS;
	half4 vA = half4(shNormal, 1.0);
				
	half3 x1;
	// Linear (L1) + constant (L0) polynomial terms
	x1.r = dot(custom_SHAr, vA);
	x1.g = dot(custom_SHAg, vA);
	x1.b = dot(custom_SHAb, vA);

	half3 x2;
	// 4 of the quadratic (L2) polynomials
	half4 vB = shNormal.xyzz * shNormal.yzzx;
	x2.r = dot(custom_SHBr, vB);
	x2.g = dot(custom_SHBg, vB);
	x2.b = dot(custom_SHBb, vB);

	// Final (5th) quadratic (L2) polynomial
	half vC = shNormal.x * shNormal.x - shNormal.y * shNormal.y;
	half3 x3 = custom_SHC.rgb * vC;
				
	half3 shColor = x1 + x2 + x3;

	lighColorData.bakedGI = shColor;
    

	//光照颜色
	lighColorData.lightColor = 1;

    
	half highQualityShadow = SAMPLE_TEXTURE2D(_ScreenSpaceShadowMap, sampler_ScreenSpaceShadowMap, i.screenPos.xy / i.screenPos.w).x;
	half atten = LerpWhiteTo(highQualityShadow, _ShadowIntensity);
	lighColorData.atten = atten;
	lighColorData.absolute = atten;

    
	half oneMinusReflectivity = LinearOneMinusReflectivityFromMetallic(textureData.metallic);
	lighColorData.diffuseAlbedo = textureData.albedo * oneMinusReflectivity;

	lighColorData.specularColor = lerp(kDieletricSpec.rgb, textureData.albedo, textureData.metallic);
	lighColorData.specularColor *= lerp(_SpecularColor.rgb, _SpecularColor.rgb * textureData.albedo, _SpecularBlendAlbedoIntensity);
	
	lighColorData.envColor = _EnvColor.rgb;

	lighColorData.grazingTerm = saturate(textureData.smoothness + 1.0 - oneMinusReflectivity);
    
	lighColorData.addLightAtten = 1;//多光源更改投影强度
}
//=============================END=================================//


//=================================================================//
//                          光照计算                                //
//=================================================================//
half LinearStep(half minValue, half maxValue, half In)
{
    return saturate((In - minValue) / (maxValue - minValue));
}

inline void GetDiffuseColor(TextureData textureData, LightVectorData lightVectorData, LightColorData lighColorData, out half3 diffuseTerm, inout half3 finalColor)
{//漫反射计算
    half radiance = lightVectorData.halfLambert * lighColorData.atten;

    // half2 rampID = half2(radiance, textureData.rampID + _RampYOffset);
    half2 rampID = half2(radiance, textureData.rampID);
    rampID.y = frac(rampID.y);
    diffuseTerm = SAMPLE_TEXTURE2D(_RampMap, sampler_RampMap, rampID).rgb;

    finalColor = lighColorData.diffuseAlbedo * diffuseTerm;
}

inline void GetDiffuseColor_Fur(TextureData textureData, LightVectorData lightVectorData, LightColorData lighColorData, out half3 diffuseTerm, inout half3 finalColor)
{//漫反射计算
	half radiance = lightVectorData.halfLambert * lighColorData.atten;

	// half2 rampID = half2(radiance, textureData.rampID + _RampYOffset);
	half2 rampID = half2(radiance, textureData.rampID);
	rampID.y = frac(rampID.y);
	diffuseTerm = SAMPLE_TEXTURE2D(_RampMap, sampler_RampMap, rampID).rgb;

	finalColor = lighColorData.diffuseAlbedo * diffuseTerm;
	finalColor *= lighColorData.lightColor;
}

inline void GetAddLightColor(TextureData textureData, LightVectorData lightVectorData, inout LightColorData lighColorData, inout half3 finalColor)
{//多光源计算
    half3 MultipleLightColor = finalColor;//此时只算了Diffuse
	
    #if _ADDITIONAL_LIGHTS
    uint pixelLightCount = GetAdditionalLightsCount();
    for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
    {
    	//==================新多光源计算========================
    	//灯光
    	int perObjectLightIndex = GetPerObjectLightIndex(lightIndex);
    	Light light = GetAdditionalPerObjectLight(perObjectLightIndex, lightVectorData.positionWS);

    	//衰减后的灯光颜色
    	half3 currentLightColor = light.color * light.distanceAttenuation * light.shadowAttenuation;

    	//阴影
    	half diffuseTerm = dot(lightVectorData.normalWS, light.direction);
    	diffuseTerm = diffuseTerm * 0.5h + 0.5h;
    	
    	diffuseTerm = LinearStep(lighColorData.multipleLightRange, lighColorData.multipleLightRange + lighColorData.multipleLightSoftness, diffuseTerm);

    	//最终多光源颜色
    	half3 multipleLDiffuseColor = currentLightColor * Luminance(lighColorData.diffuseAlbedo);
    	multipleLDiffuseColor *= diffuseTerm;
    	MultipleLightColor += multipleLDiffuseColor;
    }
	half maxRGBValue = Luminance(MultipleLightColor);
	maxRGBValue /= lighColorData.multipleLightBrightness;
	MultipleLightColor = maxRGBValue > 1.0h ? (MultipleLightColor / maxRGBValue) : MultipleLightColor;
    #endif

    finalColor = MultipleLightColor;
}

inline void GetSpecularColor(TextureData textureData, LightVectorData lightVectorData, LightColorData lighColorData, inout half3 finalColor)
{//双层高光计算
    //粗糙度
    half roughness = textureData.roughness;
    roughness += HALF_MIN_SQRT;
    roughness = saturate(roughness);

    //混合高光计算
    half NdotHmixNdotV = lightVectorData.NdotH;
    
    // half mixedSpecularIntensity = lightVectorData.NdotH - lightVectorData.NdotV;

    //混合权重
    // half mixedSpecularWeight = saturate(mixedSpecularIntensity * 1000);//ceil()优化模拟版
    // NdotHmixNdotV = lerp(lightVectorData.NdotV, lightVectorData.NdotH, mixedSpecularWeight);
    // NdotHmixNdotV = lerp(lightVectorData.NdotH, NdotHmixNdotV, _UseNdVSpecular);
    

    //亮度强度
    // mixedSpecularIntensity = mixedSpecularIntensity * 0.5 + 0.5;
    // mixedSpecularIntensity = lerp(0.5, mixedSpecularIntensity, _NdVSpecularWeight * 50);//增强对比度操作
    // mixedSpecularIntensity = saturate(mixedSpecularIntensity);

    //简化版PBR高光
    half OneMinusNoHSqr = 1.0 - NdotHmixNdotV * NdotHmixNdotV;

    half n = NdotHmixNdotV * roughness;
    half p = roughness / (OneMinusNoHSqr + n * n);
    half specularTerm = p * p;

    // specularTerm *= lerp(_NdVSpecularInstensity * _SpecularInstensity, _SpecularInstensity, mixedSpecularIntensity);
    specularTerm *= lerp(_SpecularInstensity, _SpecularInstensity * (1 + _MetaIntensity), textureData.metallic);
    specularTerm *= textureData.specularIntensity;
    
    half3 specularColor = lighColorData.specularColor * specularTerm;
    // specularColor *= diffuseTerm;
    specularColor *= lightVectorData.halfLambert;
    specularColor *= lighColorData.atten;
    
    finalColor += specularColor;
    finalColor *= lighColorData.lightColor;
}

float3 RotateAround(float3 target, float degree)
{
	float rad = degree * 0.01745f;
	float2x2 m_rotate = float2x2(cos(rad), -sin(rad),
		sin(rad), cos(rad));
	float2 dir_rotate = mul(m_rotate, target.xz);
	target = float3(dir_rotate.x, target.y, dir_rotate.y);
	return target;
}

inline void GetGIColor(TextureData textureData, LightVectorData lightVectorData, LightColorData lighColorData, inout half3 finalColor)
{//GI计算
    //漫反射
    half3 indirectDiffuse = lighColorData.diffuseAlbedo * lighColorData.bakedGI;
    indirectDiffuse *= _SHBrightness;
    
    
    //高光
    // //Matcap
    // float3 normalVS   = normalize(TransformWorldToViewDir(lightVectorData.normalWS));
    // float3 viewPosVS  = normalize(TransformWorldToView(lightVectorData.positionWS));
    // float3 viewCross  = cross(viewPosVS, normalVS);
    //
    // normalVS = float3(-viewCross.y, viewCross.x, 0);
    //
    // float2 matcapUV = normalVS.xy;
    // matcapUV = matcapUV * 0.5f + 0.5f;
    //
    // half3 matcapColor = SAMPLE_TEXTURE2D(_MatCapMap, sampler_MatCapMap, matcapUV).rgb;
    // matcapColor *= lighColorData.envColor;
    // matcapColor = lerp(matcapColor, matcapColor * textureData.albedo, _EnvBlendAlbedoIntensity);
    // matcapColor *= _EnvBrightness;
    // matcapColor *= textureData.metallic;

	//Cubemap
	half3 reflectVector = reflect(-lightVectorData.viewDirectionWS, lightVectorData.normalWS);
	half3 rotateReflect = RotateAround(reflectVector, _EnvRotate);
	
	half fresnelTerm = Pow4(1.0 - lightVectorData.NdotV);

	half mip = PerceptualRoughnessToMipmapLevel(1 - textureData.smoothness);

	half3 indirectSpecular = 0;

	half4 encodedIrradiance = SAMPLE_TEXTURECUBE_LOD(_EvnCubemap, sampler_EvnCubemap, rotateReflect, mip);

	#if !defined(UNITY_USE_NATIVE_HDR)
	indirectSpecular = DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
	#else
	indirectSpecular = encodedIrradiance.rgb;
	#endif

	float surfaceReduction = 1.0 / (textureData.roughness * textureData.roughness + 1.0);

	indirectSpecular *= lighColorData.envColor;
	indirectSpecular = lerp(indirectSpecular, indirectSpecular * textureData.albedo, _EnvBlendAlbedoIntensity);
	indirectSpecular *= _EnvBrightness;
	indirectSpecular *= surfaceReduction;
	indirectSpecular *= lerp(lighColorData.specularColor, lighColorData.grazingTerm, fresnelTerm);
	// indirectSpecular *= textureData.metallic;

    //最终颜色
    half3 giColor = indirectDiffuse + indirectSpecular;
	giColor *= textureData.ao;//HBAO

    finalColor += giColor;
}

inline void GetEmissionColor(TextureData textureData, LightVectorData lightVectorData, LightColorData lighColorData, inout half3 finalColor)
{//自发光计算
    half3 emissionColor = textureData.emission * _EmissionColor.rgb * _EmissionInstensity;
    emissionColor = emissionColor * textureData.albedo;
    // emissionColor = lerp(emissionColor, emissionColor * textureData.albedo, _EmissionBlendAlbedoIntensity);

    finalColor += emissionColor;
}

inline void GetRimColor(TextureData textureData, LightVectorData lightVectorData, LightColorData lighColorData, inout half3 finalColor)
{//边缘光计算

  //       //深度边缘光
  //       half3 rimLightColor = 0;
	 //    half rimIntensity = 0;
	 //    half3 RimColor = 0;
  //   
  //
		// half3 rimDir = lerp(lightVectorData.lightDirWS.xyz, _RimDir.xyz, _RimCustom);
  //       float Ndotl = saturate(dot(lightVectorData.normalWS, rimDir));//inputDotData.NdotL;
  //
  //   //#if defined(_PERSPECTIVE)
  //       float2 N_View = normalize(mul((float3x3)_CustomShowMatrix_V, lightVectorData.normalWS).xy);
  //       float3 pos_V  = mul(_CustomShowMatrix_V, float4(lightVectorData.positionWS, 1.0)).xyz;
  //   /*#else
  //       float2 N_View = normalize(mul((float3x3)UNITY_MATRIX_V, inputData.normalWS).xy);
  //       float3 pos_V  = TransformWorldToView(inputData.positionWS);
  //   #endif*/
  //   
  //       
  //       float3 samplePositionVS = float3(pos_V.xy, pos_V.z); // 保持z不变（CS.w = -VS.z） rimWidth add
  //       float3 samplePositionVS2 = float3(pos_V.xy + N_View.xy * _RimLightPower2 * 0.01, pos_V.z); // 保持z不变（CS.w = -VS.z） rimWidth add
  //   
  //   //#if defined(_PERSPECTIVE)
  //   float4 pos	  = mul(_CustomShowMatrix_P, float4(samplePositionVS, 1.0));
  //   float4 pos2	  = mul(_CustomShowMatrix_P, float4(samplePositionVS2, 1.0));
  //   /*#else
  //   float4 pos	  = TransformWViewToHClip(samplePositionVS);
  //   float4 pos2	  = TransformWViewToHClip(samplePositionVS2);
  //   #endif*/
  //
  //   
  //       float4 screenPos = ComputeScreenPos(pos);
  //       float4 screenPos2 = ComputeScreenPos(pos2);
  //       float3  SSUV = screenPos.xyz / (screenPos.w + 1e-3);
  //       float3  SSUV2 = screenPos2.xyz / (screenPos2.w + 1e-3);
  //   
  //       float thisDepthZ = -pos_V.z; // 离相机越近越小
  //       float rawDepth = SAMPLE_TEXTURE2D_X(_CurrentCameraDepthTexture, sampler_CurrentCameraDepthTexture, SSUV.xy).r;//tex2D(DepthTex, SSUV.xy);
  //       float rawDepth2 = SAMPLE_TEXTURE2D_X(_CurrentCameraDepthTexture, sampler_CurrentCameraDepthTexture, SSUV2.xy).r;//tex2D(DepthTex, SSUV.xy);
  //
  //       #if !UNITY_REVERSED_Z
  //       rawDepth = 1.0 - rawDepth;
  //       rawDepth2 = 1.0 - rawDepth2;
  //       #endif
  //   
  //
  //       float near = _ProjectionParams.y;
  //       float far = _ProjectionParams.z;
  //       //#if UNITY_REVERSED_Z
  //       //rawDepth = 1.0 - rawDepth;
  //       //#endif
  //   
  //       float depthDiff = rawDepth - rawDepth2;
  //       rimIntensity = LinearStep(_RimLightThreshold2 * 0.05 * _RimLightEdgeSmooth2, _RimLightThreshold2 * 0.06, depthDiff); // 注意min和max不能相等！
  //       rimIntensity *= lerp(1, saturate(_RimLightContribution2 - (1.0 - Ndotl)) * lightVectorData.halfLambert, _RimLightContribution2);
  //       half hdrRim = max(max(_RimColor.r,  _RimColor.g), _RimColor.b) ;//* _RimStrength;
  //       rimLightColor = lerp(_RimColor.rgb, hdrRim * textureData.albedo, _UseRimColorOrBaseMap2);
  //       rimLightColor *= rimLightColor * rimIntensity * lighColorData.atten;//

    //菲涅尔边缘光
    half rimHalfLambert = dot(-_RimLightDir.xyz, lightVectorData.normalWS) * 0.5 + 0.5;
    // half RimTerm  = LinearStep(_RimThreshold, _RimThreshold + _RimSoftness, 1 - lightVectorData.NdotVNoClamp);
    // half rimMask = LinearStep(_RimLambertMaskThreshold, _RimLambertMaskThreshold + _RimSoftness, 1 - rimHalfLambert);
    half RimTerm  = LinearStep(_RimThreshold, _RimThreshold + 0.5h, 1 - lightVectorData.NdotVNoClamp);
    half rimMask = LinearStep(_RimLambertMaskThreshold, _RimLambertMaskThreshold + 0.5h, 1 - rimHalfLambert);
    RimTerm *= rimMask;

    half3 rimLightColor = _RimColor.rgb * RimTerm;
    // rimLightColor *= lerp(half3(1, 1, 1), textureData.albedo, _RimColorBlend) * _useRim;
    rimLightColor *= _useRim;

    finalColor += rimLightColor;
}

inline void GetBacklightRimColor(TextureData textureData, LightVectorData lightVectorData, LightColorData lighColorData, inout half3 finalColor)
{//背光边缘光计算

    // //背光遮罩计算
    // float3 N_Shift = lightVectorData.normalWS + lightVectorData.lightDirWS;
    // half backlightValue = saturate(dot(-N_Shift, lightVectorData.viewDirectionWS));
    //
    // half VdotL = dot(lightVectorData.viewDirectionWS, lightVectorData.lightDirWS);
    // VdotL = VdotL * 0.5 + 0.5;
    //
    // //边缘光计算
    // half fresnel = 1 - lightVectorData.NdotV;
    // fresnel = Pow4(fresnel);
    //
    // half backlightRimTerm  = smoothstep(_RimThreshold, _RimThreshold + 0.5, fresnel);
    //
    //
    // //光照方向背光遮罩
    // half backlightMask = LinearStep(_RimLambertMaskThreshold, _RimLambertMaskThreshold + 1, lightVectorData.halfLambert) * backlightValue;
    // backlightMask *= lighColorData.atten;
    //
    //
    // //光照方向背光强度
    // half BacklightDirIntensity = 1 - VdotL;
    // BacklightDirIntensity = LinearStep(0.5, 0.9, BacklightDirIntensity);
    // BacklightDirIntensity *= BacklightDirIntensity;
    // BacklightDirIntensity *= BacklightDirIntensity;
    //
    //
    // half3 backlightColor = _RimColor.rgb * backlightRimTerm;
    // backlightColor *= BacklightDirIntensity;
    // backlightColor *= backlightMask;
    //
    // backlightColor *= _useRim;
    //
    // finalColor += backlightColor;


	//=======================================================================
	//边缘光计算
	half fresnel = 1 - lightVectorData.NdotV;
	fresnel = Pow4(fresnel);

	// half backlightRimTerm  = smoothstep(_RimThreshold, _RimThreshold + _RimSoftness, fresnel);
	half backlightRimTerm  = smoothstep(_RimThreshold, _RimThreshold + 0.5h, fresnel);

	//边缘光遮罩
	half3 backlightDir = lightVectorData.lightDirWS;
	backlightDir.x *= -1.0h;
	backlightDir.z *= -1.0h;
	
	half halfLambert = dot(lightVectorData.normalWS, backlightDir) * 0.5h + 0.5h;
	// halfLambert = LinearStep(_RimLambertMaskThreshold, 1, halfLambert);
	halfLambert = LinearStep(0.5h, 1.0h, halfLambert);

	//光照深度遮罩
	// _CameraDepthTexture
	float3 reverseLightDepthUV_WS = lightVectorData.positionWS + backlightDir * _RimDepthWidth;
	float4 reverseLightDepthUV_CS = TransformWorldToHClip(reverseLightDepthUV_WS);
	float4 reverseLightDepthUV_NDC = ComputeScreenPos(reverseLightDepthUV_CS);
	
	float screenDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, reverseLightDepthUV_NDC.xy / reverseLightDepthUV_NDC.w).r;
	screenDepth = LinearEyeDepth(screenDepth, _ZBufferParams);

	float reverseLightDepth = reverseLightDepthUV_NDC.z / reverseLightDepthUV_NDC.w;
	reverseLightDepth = LinearEyeDepth(reverseLightDepth, _ZBufferParams);
	

	float currentPosDepth = lightVectorData.screenPos.z / lightVectorData.screenPos.w;
	currentPosDepth = LinearEyeDepth(currentPosDepth, _ZBufferParams);

	float differentDepth = smoothstep(0, 1, screenDepth - reverseLightDepth);
	// float differentDepth = smoothstep(_Test1, _Test1 + _Test2, screenDepth - currentPosDepth);
	
	
	half3 backlightColor = lerp(_RimColor.rgb, _RimColor.rgb * textureData.albedo, _RimColorBlendAlbedo);
	backlightColor *= backlightRimTerm;
	backlightColor *= halfLambert;
	backlightColor *= differentDepth;
	backlightColor *= _useRim;

    #if _USERIM
	finalColor += backlightColor;
    #endif
	
	// finalColor = differentDepth;
}

inline void GetSilkStockingColor(TextureData textureData, LightVectorData lightVectorData, half3 diffuseTerm, inout half3 finalColor)
{//丝袜计算
    #if _SILKSTOCKING_ON
    //丝袜暗边颜色
    half fresnel = 1 - lightVectorData.NdotV;

    //菲涅尔透肉
    half skinWeight = lightVectorData.NdotV;
    skinWeight *= skinWeight;
    skinWeight *= skinWeight;
    skinWeight *= skinWeight;
    
    half3 silkStockingSkinColor = lerp(textureData.albedo, textureData.albedo * _SilkStockingSkinColor.rgb * _SilkStockingSkinColorBrightness, skinWeight);
    
    
    //菲涅尔暗区域
    half darkFresnelColorWeight = lerp(fresnel, _SilkStockingDarkFresnelContrast, fresnel);
    darkFresnelColorWeight *= _SilkStockingDarkFresnelIntensity;
    darkFresnelColorWeight = LinearStep(_SilkStockingDarkFresnelRange, _SilkStockingDarkFresnelRange + _SilkStockingDarkFresnelSoftness, darkFresnelColorWeight);

    half3 baseSilkStockingColor = lerp(silkStockingSkinColor, silkStockingSkinColor * _SilkStockingDarkFresnelColor.rgb, darkFresnelColorWeight);

    //黑边
    half firstDarkSideColorWeight = lerp(fresnel, _SilkStockingDarkSideBrightness, fresnel);//叠加原来的菲涅尔
    
    half firstDarkSideColorMask = 1 - firstDarkSideColorWeight;
    firstDarkSideColorMask *= firstDarkSideColorMask;
    firstDarkSideColorMask = LinearStep(_SilkStockingDarkSideRange, _SilkStockingDarkSideRange + _SilkStockingDarkSideSoftness, firstDarkSideColorMask);

    firstDarkSideColorWeight *= (1 - firstDarkSideColorMask);
    half3 firstDarkSideColor = lerp(1, _SilkStockingDarkSideColor.rgb, firstDarkSideColorWeight);
    // firstDarkSideColor = lerp(firstDarkSideColor, 1, firstDarkSideColorMask);
    
    //边缘光
    half rimColorWeight = fresnel;
    rimColorWeight *= rimColorWeight;
    rimColorWeight = LinearStep(_SilkStockingRimWidth, _SilkStockingRimWidth + 0.5, rimColorWeight);

    
    //最终颜色
    half3 silkStockingColor = baseSilkStockingColor * firstDarkSideColor;
    silkStockingColor = lerp(silkStockingColor, silkStockingSkinColor * _SilkStockingRimColor.rgb * _SilkStockingRimBrightness, rimColorWeight);

    // silkStockingColor *= diffuseTerm;

    // finalColor = firstDarkSideColor;
    // finalColor = skinWeight.xxx;
    finalColor = lerp(finalColor, silkStockingColor, textureData.SilkStockingMask);
    #endif
}

inline void GetAnisotropySpecularColor(TextureData textureData, LightVectorData lightVectorData, LightColorData lighColorData, half3 diffuseTerm, inout half3 finalColor)
{//各向异性高光计算
    
    #if _ANISOTROPY_ON
    
    float TdotH = dot(lightVectorData.tangentWS, lightVectorData.halfDir);
    float TdotL = dot(lightVectorData.tangentWS, lightVectorData.lightDirWS);
    float BdotH = dot(lightVectorData.bitangentWS, lightVectorData.halfDir);
    float BdotL = dot(lightVectorData.bitangentWS, lightVectorData.lightDirWS);
    float TdotV = dot(lightVectorData.tangentWS,   lightVectorData.viewDirectionWS);
    float BdotV = dot(lightVectorData.bitangentWS, lightVectorData.viewDirectionWS);

    half roughnessT = textureData.roughness * (1 + _AnisotropyIntensity);
    half roughnessB = textureData.roughness * (1 - _AnisotropyIntensity);

    half partLambdaV = GetSmithJointGGXAnisoPartLambdaV(TdotV, BdotV, lightVectorData.NdotVNoClamp, roughnessT, roughnessB);

    half LdotH = max(dot(lightVectorData.lightDirWS, lightVectorData.halfDir), 0.0001);
    // float3 F = F_Schlick(lighColorData.specularColor, LdotH);
    float3 F = 1;

    float DV = DV_SmithJointGGXAniso(
        TdotH, BdotH, lightVectorData.NdotH, lightVectorData.NdotV, TdotL, BdotL, lightVectorData.NdotL,
        roughnessT, roughnessB, partLambdaV); // D_GGXAniso() * V_SmithJointGGXAniso()

    half3 specularTerm = max(0.001, F * DV) * saturate(lightVectorData.NdotL);
    specularTerm *= diffuseTerm;
    specularTerm *= lighColorData.atten;
    specularTerm *= textureData.anisotropyMask;
    specularTerm *= _AnisotropyBrightness;

    half3 AnisotropySpecularColor = lerp(_AnisotropySpecularColor.rgb, _AnisotropySpecularColor.rgb * textureData.albedo, _AnisotropySpecularBlendAlbedoIntensity);
    AnisotropySpecularColor *= specularTerm;
    
    finalColor += AnisotropySpecularColor;
    
    #endif
}

inline void GetShadowColor_Scene(TextureData textureData, LightColorData lighColorData, half3 diffuseTerm, inout half3 finalColor)
{//投影颜色计算
    // finalColor = lerp(_CharacterShadowColor.rgb * finalColor, finalColor, lighColorData.addLightAtten);

    half3 shadowColor = diffuseTerm * textureData.albedo;
    finalColor = lerp(shadowColor, finalColor, lighColorData.atten);
}

inline void GetShadowColor_Show(TextureData textureData, LightColorData lighColorData, half3 diffuseTerm, inout half3 finalColor)
{//投影颜色计算
    //模拟投影SSS边缘过渡色
    // half3 shadowColor = lerp(_CharacterShadowColor.rgb * finalColor, diffuseTerm * finalColor, _ShadowColorOffset);
    
    // half3 shadowColor = lerp(_CharacterShadowColor.rgb * textureData.albedo, diffuseTerm * textureData.albedo, _ShadowColorOffset);
    half3 shadowColor = diffuseTerm * textureData.albedo;
    finalColor = lerp(shadowColor, finalColor, lighColorData.atten);
    
}

inline void GetHeavyShadowWeight(LightVectorData lightVectorData, LightColorData lighColorData, inout half3 finalColor)
{//重阴影颜色计算
	//重阴影
	half heavyShadowWeight = smoothstep(_HeavyShadowRange, _HeavyShadowRange + _HeavyShadowSoftness, lightVectorData.NdotV);
	
	heavyShadowWeight = lerp(heavyShadowWeight, 1, lighColorData.atten * _UseShadowMask);
	heavyShadowWeight = LerpWhiteTo(heavyShadowWeight, _HeavyShadowIntensity);

	finalColor = lerp(finalColor * _HeavyShadowColor.rgb, finalColor, heavyShadowWeight);
}

inline void GetEffectFresnelColor_Scene(TextureData textureData, LightVectorData lightVectorData, LightColorData lighColorData, inout half3 finalColor)
{//特效边缘光计算
    #if _EFFECTFRESNEL_ON
    half3 effectFresnelColor = 0;
    
    half effectFresnelMask = SAMPLE_TEXTURE2D(_EffectMaskMap, sampler_EffectMaskMap, textureData.uv.xy).r;
    
    half effectFresnel = pow(1 - lightVectorData.NdotV, _EffectFresnelPower);
    half effectFresnelvalue = smoothstep(_EffectFresnelThreshold, _EffectFresnelThreshold + _EffectFresnelSmooth, effectFresnel);
    effectFresnelColor = _EffectFresnelColor.rgb * effectFresnelvalue * _EffectFresnelFactor;
    effectFresnelColor *= effectFresnelMask;

    finalColor += effectFresnelColor;
    #endif
}

inline void GetTransparentFresnelColor_Scene_Transparent(inout TextureData textureData, LightVectorData lightVectorData, LightColorData lighColorData, inout half3 finalColor)
{//角色半透效果计算
    
    textureData.alpha *= _CharacterTransparentValue;
    half fresnel1 = 1 - lightVectorData.NdotV;
    fresnel1 = pow(fresnel1, _TransparentFresnelRange);
    fresnel1 *=  (1 - textureData.alpha);
    fresnel1 = saturate(fresnel1 * _TransparentFresnelBrightness);
    textureData.alpha = lerp(textureData.alpha, 1, fresnel1);

    finalColor = lerp(finalColor, _TransparentFresnelColor.rgb * _TransparentFresnelBrightness, fresnel1);//角色半透效果
}

inline void GetHitRimColor_Scene(TextureData textureData, LightVectorData lightVectorData, LightColorData lighColorData, inout half3 finalColor)
{//特效边缘光计算
    half hitrim = pow(1 - lightVectorData.NdotV, _HitRimPower);
    half hitrimvalue = LinearStep(_HitRimThreshold, _HitRimThreshold + _HitRimSmooth, hitrim);
    half3 hitrimcolor = _HitRimColor.rgb * hitrimvalue * _HitRimFactor;

    finalColor += hitrimcolor;
}

inline void GetHitRimColor_Scene_Transparent(TextureData textureData, LightVectorData lightVectorData, LightColorData lighColorData, inout half3 finalColor)
{//特效边缘光计算
    half hitrim = pow(1 - lightVectorData.NdotV, _HitRimPower);
    half hitrimvalue = LinearStep(_HitRimThreshold, _HitRimThreshold + _HitRimSmooth, hitrim);
    half3 hitrimcolor = _HitRimColor.rgb * hitrimvalue * _HitRimFactor;

    finalColor = lerp(finalColor, hitrimcolor, hitrimvalue * _HitRimFactor);//受击边缘光
}

inline void GetFogColor(v2f_Scene i, LightColorData lighColorData, inout half3 finalColor)
{
    //雾效
    #ifdef _WORLDFOG
    WarFogSample(finalColor.rgb, i.positionWS.xyz);
    #endif

    finalColor.rgb = MixFog(finalColor, lighColorData.fogCoord);
}

inline half FabricScatterFresnelLerp(half nv, half scale)
{
	half t0 = Pow4 (1 - nv); 
	half t1 = 0.4 * (1 - nv);
	return (t1 - t0) * scale + t0;
}
inline void GetFabricColor(LightVectorData lightVectorData, LightColorData lighColorData , inout half3 finalColor)
{
	finalColor.rgb += _FabricScatterColor * lightVectorData.halfLambert * FabricScatterFresnelLerp(lightVectorData.NdotV, _FabricScatterScale) ;//* lighColorData.atten;

	//环境光
	float3 normal = normalize(mul(UNITY_MATRIX_V, float4(lightVectorData.normalWS,0)).xyz);
	half3 SH = saturate(normal.y *0.25+0.35) ;
	
	//毛发长度影响
	half Occlusion =_FUR_OFFSET*_FUR_OFFSET; //伽马转线性最精简版
	Occlusion +=0.04 ;

	//算毛发AO
	half3 SHL1 = lerp (_OcclusionColor*SH,SH,Occlusion);
	half3 SHL2 = lerp(lerp(_ShadowColor.rgb, 1, _FUR_OFFSET ), 1, 1-_ShadowLerp);

	//算边缘光
	half Fresnel =saturate( pow( 1-max(0,lightVectorData.NdotV) , _FurEdgePow) );
	half RimLight = Fresnel * Occlusion;//AO的深度剔除 很重要
	RimLight *= _FresnelLV * SH ; //加上环境光因数
	RimLight = lerp(RimLight , RimLight * lighColorData.absolute, _FurEdgeAtten);

	finalColor.rgb = finalColor.rgb * SHL2 + RimLight * _FurEdgeColor.xyz;
}

//=============================END=================================//


//=================================================================//
//                          片元着色器                              //
//=================================================================//
half4 Fragment_Scene(v2f_Scene i) : SV_TARGET
{
	
	//Dither
	DitherThreshold(i.screenPos,i.positionCS.xy,_DitherTimer);
	
    //================基础数据计算==================
    TextureData textureData;
    InitializeTextureData_Scene(i, textureData);

    LightVectorData lightVectorData;
    InitializeLightVectorData_Scene(i, textureData, lightVectorData);

    LightColorData lighColorData;
    InitializeLighColorData_Scene(i, textureData, lightVectorData, lighColorData);

	CharacterLightControllerData_Scene(lightVectorData, lighColorData);
	CalculateLightVectorData_Scene(lightVectorData);
	
    //================最终颜色计算==================
    half3 finalColor = 0;
    half3 diffuseTerm = 0;
	
    GetDiffuseColor(textureData, lightVectorData, lighColorData, diffuseTerm, finalColor);//漫反射计算
	// GetSilkStockingColor(textureData, lightVectorData, diffuseTerm, finalColor);//丝袜计算
    // GetAddLightColor(textureData, lightVectorData, lighColorData, finalColor);//多光源计算
    GetSpecularColor(textureData, lightVectorData, lighColorData, finalColor);//双层高光计算
	GetShadowColor_Scene(textureData, lighColorData, diffuseTerm, finalColor);//投影颜色计算
    GetGIColor(textureData, lightVectorData, lighColorData, finalColor);//GI计算
	GetHeavyShadowWeight(lightVectorData, lighColorData, finalColor);//重阴影计算
	GetAddLightColor(textureData, lightVectorData, lighColorData, finalColor);//多光源计算
	GetBacklightRimColor(textureData, lightVectorData, lighColorData, finalColor);//边缘光计算
	GetEmissionColor(textureData, lightVectorData, lighColorData, finalColor);//自发光计算
    GetEffectFresnelColor_Scene(textureData, lightVectorData, lighColorData, finalColor);//特效边缘光计算
    GetHitRimColor_Scene(textureData, lightVectorData, lighColorData, finalColor);//受击边缘光
    GetFogColor(i, lighColorData, finalColor);

    // GetAnisotropySpecularColor(textureData, lightVectorData, lighColorData, diffuseTerm, finalColor);//各向异性高光计算
    // return half4(SAMPLE_TEXTURE2D(_BumpMaskMap, sampler_BumpMaskMap, i.uv.xy).xxx, 1);

	//特效专用自发光（不使用遮罩）
	finalColor += _WholeEmissionColor.rgb;
 
    return half4(finalColor, textureData.alpha);
}
half4 Fragment_Fur(v2f_Scene i, half FUR_OFFSET = 0) : SV_TARGET
{
    //================基础数据计算==================
    TextureData textureData;
    InitializeTextureData_Fur(i, textureData);

    LightVectorData lightVectorData;
    InitializeLightVectorData_Fur(i, textureData, lightVectorData);

    LightColorData lighColorData;
    InitializeLighColorData_Fur(i, textureData, lighColorData);

	CharacterLightControllerData_Scene(lightVectorData, lighColorData);
	CalculateLightVectorData_Scene(lightVectorData);
	
    //================最终颜色计算==================
    half3 finalColor = 0;
    half3 diffuseTerm = 0;
	
    GetDiffuseColor_Fur(textureData, lightVectorData, lighColorData, diffuseTerm, finalColor);//漫反射计算
	// GetSilkStockingColor(textureData, lightVectorData, diffuseTerm, finalColor);//丝袜计算
	// GetAddLightColor(textureData, lightVectorData, lighColorData, finalColor);//多光源计算
	// GetSpecularColor(textureData, lightVectorData, lighColorData, finalColor);//双层高光计算
	GetFabricColor(lightVectorData,lighColorData,finalColor);//算毛发AO，边缘光等
	GetShadowColor_Scene(textureData, lighColorData, diffuseTerm, finalColor);//投影颜色计算
	GetGIColor(textureData, lightVectorData, lighColorData, finalColor);//GI计算
	GetHeavyShadowWeight(lightVectorData, lighColorData, finalColor);//重阴影计算
	GetAddLightColor(textureData, lightVectorData, lighColorData, finalColor);//多光源计算
	GetBacklightRimColor(textureData, lightVectorData, lighColorData, finalColor);//边缘光计算
	// GetEmissionColor(textureData, lightVectorData, lighColorData, finalColor);//自发光计算
    // GetAnisotropySpecularColor(textureData, lightVectorData, lighColorData, diffuseTerm, finalColor);//各向异性高光计算

	//算毛发Alpha
	half2 uvoffset = SAMPLE_TEXTURE2D(_FlowMap, sampler_FlowMap, i.uv.xy).rg * 2 - half2(1.0,1.0);//+ _UVOffset * uvoffset * _FUR_OFFSET
	half alpha = SAMPLE_TEXTURE2D(_LayerTex, sampler_LayerTex , i.uv.xy * _LayerTex_ST.xy * textureData.alpha + _LayerTex_ST.zw ).r;
	FUR_OFFSET = _FUR_OFFSET / 20;
	alpha = (alpha * 2 - (FUR_OFFSET * FUR_OFFSET +(FUR_OFFSET * 1 * 1))) * 0.5;
	alpha = step(lerp(0, _CutoffEnd, _FUR_OFFSET), alpha);
	alpha = alpha * max((1 - _FUR_OFFSET * _FUR_OFFSET) + lightVectorData.NdotV - _EdgeFade,0);
	if (alpha < _AlphaCutout) discard;
	
	GetEffectFresnelColor_Scene(textureData, lightVectorData, lighColorData, finalColor);//特效边缘光计算
	GetHitRimColor_Scene(textureData, lightVectorData, lighColorData, finalColor);//受击边缘光
	GetFogColor(i, lighColorData, finalColor);
	
	//特效专用自发光（不使用遮罩）
	finalColor += _WholeEmissionColor.rgb;
	//return half4(lighColorData.atten.xxx,alpha);
	
    return half4(finalColor, alpha);
}

half4 Fragment_UiShow(v2f_UiShow i) : SV_TARGET
{
    //================基础数据计算==================
    TextureData textureData;
    InitializeTextureData_UiShow(i, textureData);
    
    LightVectorData lightVectorData;
    InitializeLightVectorData_UiShow(i, textureData, lightVectorData);
    
    LightColorData lighColorData;
    InitializeLighColorData_UiShow(i, textureData, lightVectorData, lighColorData);
    
    //================最终颜色计算==================
    half3 finalColor = 0;
    half3 diffuseTerm = 0;
    
	GetDiffuseColor(textureData, lightVectorData, lighColorData, diffuseTerm, finalColor);//漫反射计算
	// GetSilkStockingColor(textureData, lightVectorData, diffuseTerm, finalColor);//丝袜计算
	// GetAddLightColor(textureData, lightVectorData, lighColorData, finalColor);//多光源计算
	GetSpecularColor(textureData, lightVectorData, lighColorData, finalColor);//双层高光计算
	GetShadowColor_Show(textureData, lighColorData, diffuseTerm, finalColor);//投影颜色计算
	GetGIColor(textureData, lightVectorData, lighColorData, finalColor);//GI计算
	GetHeavyShadowWeight(lightVectorData, lighColorData, finalColor);//重阴影计算
	GetAddLightColor(textureData, lightVectorData, lighColorData, finalColor);//多光源计算
	GetRimColor(textureData, lightVectorData, lighColorData, finalColor);//边缘光计算
	GetEmissionColor(textureData, lightVectorData, lighColorData, finalColor);//自发光计算
    
    //应对UI相机强制转Gamma空间
    // finalColor = LinearToSRGB(finalColor);
    
    return half4(finalColor, textureData.alpha);
}
half4 Fragment_FurUi(v2f_UiShow i, half FUR_OFFSET = 0) : SV_TARGET
{
	//================基础数据计算==================
	TextureData textureData;
	InitializeTextureData_FurUi(i, textureData);
    
	LightVectorData lightVectorData;
	InitializeLightVectorData_FurUi(i, textureData, lightVectorData);
    
	LightColorData lighColorData;
	InitializeLighColorData_FurUi(i, textureData, lightVectorData, lighColorData);
    
	//================最终颜色计算==================
	half3 finalColor = 0;
	half3 diffuseTerm = 0;
    
	GetDiffuseColor_Fur(textureData, lightVectorData, lighColorData, diffuseTerm, finalColor);//漫反射计算
	// GetSilkStockingColor(textureData, lightVectorData, diffuseTerm, finalColor);//丝袜计算
	// GetAddLightColor(textureData, lightVectorData, lighColorData, finalColor);//多光源计算
	// GetSpecularColor(textureData, lightVectorData, lighColorData, finalColor);//双层高光计算
	GetShadowColor_Show(textureData, lighColorData, diffuseTerm, finalColor);//投影颜色计算
	GetGIColor(textureData, lightVectorData, lighColorData, finalColor);//GI计算
	GetHeavyShadowWeight(lightVectorData, lighColorData, finalColor);//重阴影计算
	GetAddLightColor(textureData, lightVectorData, lighColorData, finalColor);//多光源计算
	GetRimColor(textureData, lightVectorData, lighColorData, finalColor);//边缘光计算
	// GetEmissionColor(textureData, lightVectorData, lighColorData, finalColor);//自发光计算
    
	//应对UI相机强制转Gamma空间
	// finalColor = LinearToSRGB(finalColor);
	
	//算毛发Alpha
	half2 uvoffset = SAMPLE_TEXTURE2D(_FlowMap, sampler_FlowMap, i.uv.xy).rg * 2 - half2(1.0,1.0);
	half alpha = SAMPLE_TEXTURE2D(_LayerTex, sampler_LayerTex , i.uv.xy * _LayerTex_ST.xy * textureData.alpha + _LayerTex_ST.zw + _UVOffset * uvoffset * _FUR_OFFSET).r;
	FUR_OFFSET = _FUR_OFFSET / 20;
	alpha = (alpha * 2 - (FUR_OFFSET * FUR_OFFSET +(FUR_OFFSET * 1 * 1))) * 0.5;
	alpha = step(lerp(0, _CutoffEnd, _FUR_OFFSET), alpha);
	alpha = alpha * max(((1 - _FUR_OFFSET * _FUR_OFFSET) + lightVectorData.NdotV - _EdgeFade),0);
	
	finalColor = finalColor * lerp(lerp(_ShadowColor.rgb, 1, _FUR_OFFSET), 1, 1-_ShadowLerp);
	return half4(finalColor, alpha);
}//动法线

half4 Fragment_Scene_Transparent(v2f_Scene i) : SV_TARGET
{
	//================基础数据计算==================
	TextureData textureData;
	InitializeTextureData_Scene(i, textureData);

	LightVectorData lightVectorData;
	InitializeLightVectorData_Scene(i, textureData, lightVectorData);

	LightColorData lighColorData;
	InitializeLighColorData_Scene(i, textureData, lightVectorData, lighColorData);

	CharacterLightControllerData_Scene(lightVectorData, lighColorData);
	CalculateLightVectorData_Scene(lightVectorData);

	//================最终颜色计算==================
	half3 finalColor = 0;
	half3 diffuseTerm = 0;
    
	GetDiffuseColor(textureData, lightVectorData, lighColorData, diffuseTerm, finalColor);//漫反射计算
	// GetSilkStockingColor(textureData, lightVectorData, diffuseTerm, finalColor);//丝袜计算
	// GetAddLightColor(textureData, lightVectorData, lighColorData, finalColor);//多光源计算
	GetSpecularColor(textureData, lightVectorData, lighColorData, finalColor);//双层高光计算
	GetShadowColor_Scene(textureData, lighColorData, diffuseTerm, finalColor);//投影颜色计算
	GetGIColor(textureData, lightVectorData, lighColorData, finalColor);//GI计算
	GetHeavyShadowWeight(lightVectorData, lighColorData, finalColor);//重阴影计算
	GetAddLightColor(textureData, lightVectorData, lighColorData, finalColor);//多光源计算
	GetBacklightRimColor(textureData, lightVectorData, lighColorData, finalColor);//边缘光计算
	GetEmissionColor(textureData, lightVectorData, lighColorData, finalColor);//自发光计算
	GetEffectFresnelColor_Scene(textureData, lightVectorData, lighColorData, finalColor);//特效边缘光计算
	GetTransparentFresnelColor_Scene_Transparent(textureData, lightVectorData, lighColorData, finalColor);//角色半透效果
	GetHitRimColor_Scene_Transparent(textureData, lightVectorData, lighColorData, finalColor);//受击边缘光
	GetFogColor(i, lighColorData, finalColor);
    
	// GetAnisotropySpecularColor(textureData, lightVectorData, lighColorData, diffuseTerm, finalColor);//各向异性高光计算

	//特效专用自发光（不使用遮罩）
	finalColor += _WholeEmissionColor.rgb;
    
	return half4(finalColor, textureData.alpha);
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

v2f_Scene vert_Fur(a2v_Scene_Fur i)
{
	return Vertex_Fur(i, 0);
}
half4 fragment_Fur(v2f_Scene i) : SV_Target
{
	return Fragment_Fur(i, .0);
}
v2f_UiShow vert_FurUi(a2v_Scene_Fur i)
{
	return Vertex_FurUi(i, 0);
}
half4 fragment_FurUi(v2f_UiShow i) : SV_Target
{
	return Fragment_FurUi(i, .0);
}

//=============================END=================================//


#endif