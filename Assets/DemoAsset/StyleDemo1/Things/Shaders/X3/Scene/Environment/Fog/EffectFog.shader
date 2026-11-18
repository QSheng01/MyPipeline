// Made with Amplify Shader Editor v1.9.1.8
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "X3/Scene/EffectFog"
{
	Properties
	{
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		_OpacityMap("云透明贴图", 2D) = "white" {}
		_Contrast("雾块对比度", Range( -1 , 1)) = 0
		_FadeDistance("地表接触距离渐隐", Float) = 100
		_SurfaceDistance("表面距离渐隐", Float) = 100
		_MainTex("主贴图", 2D) = "white" {}
		[HDR]_BaseColor("主颜色", Color) = (0,0,0,0)
		_BaseColorIntensity("光照强度", Float) = 0
		_EmiisonIntensity("自发光强度", Float) = 0
		_Normal("法线图", 2D) = "bump" {}
		_NormalScale("法线强度", Range( 0 , 10)) = 1
		_FresnelExp("菲尼尔宽度", Range( 0 , 2)) = 3
		_BlendFresnel("菲尼尔透明", Range( 0 , 1)) = 0
		_NoiseTex("扰动纹理", 2D) = "white" {}
		_NoiseSpeed("扰动速度", Vector) = (0,0,0,0)
		_NoiseIntensity("扰动强度", Range( 0 , 1)) = 0
		_DissolveTex("溶解贴图", 2D) = "white" {}
		_DissovleSpeed("溶解速度", Vector) = (0,0,0,0)
		_Dissolve("溶解上限", Range( 0 , 1)) = 1
		_DissolveTime("溶解周期", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}

	}

	Category
	{
		SubShader
		{
		LOD 0

			Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
			Cull Back
		    AlphaToMask Off
			
			HLSLINCLUDE
			#pragma target 3.5
			#pragma prefer_hlslcc gles
			// ensure rendering platforms toggle list is visible
			ENDHLSL

			Pass
			{
				Name "Forward"
				Blend SrcAlpha One, SrcAlpha One
				ZWrite On
				ZTest LEqual
				Offset 0 , 0
				ColorMask RGBA
				
				HLSLPROGRAM
				#define ASE_SRP_VERSION 140008
				#define REQUIRE_DEPTH_TEXTURE 1

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_instancing
				#define ASE_NEEDS_VERT_POSITION

				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
				#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
				#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

				struct appdata
				{
					float4 vertex : POSITION;
					float4 color : COLOR;
					float4 texcoord0 : TEXCOORD0;
					float4 textoord1 : TEXCOORD1;
					float4 tangent : TANGENT;
					float3 normal : NORMAL;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f
				{
					float4 vertex : SV_POSITION;
					float4 color : COLOR;
					float4 uv : TEXCOORD0;
					float4 uv1 : TEXCOORD1;
					float4 texcoord2 : TEXCOORD2;
					float4 texcoord3 : TEXCOORD3;
					float4 texcoord4 : TEXCOORD4;
					float4 texcoord5 : TEXCOORD5;
					float4 texcoord6 : TEXCOORD6;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};
				
				sampler2D _MainTex;
				sampler2D _Normal;
				sampler2D _NoiseTex;
				uniform float4 _CameraDepthTexture_TexelSize;
				sampler2D _OpacityMap;
				sampler2D _DissolveTex;
				CBUFFER_START( UnityPerMaterial )
				float4 _DissolveTex_ST;
				float4 _MainTex_ST;
				float4 _BaseColor;
				float4 _Normal_ST;
				float4 _NoiseTex_ST;
				float4 _OpacityMap_ST;
				float2 _NoiseSpeed;
				float2 _DissovleSpeed;
				float _BaseColorIntensity;
				float _BlendFresnel;
				float _FresnelExp;
				float _SurfaceDistance;
				float _EmiisonIntensity;
				float _Dissolve;
				float _NormalScale;
				float _NoiseIntensity;
				float _Contrast;
				float _FadeDistance;
				float _DissolveTime;
				CBUFFER_END

				v2f vert ( appdata v  )
				{
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_TRANSFER_INSTANCE_ID(v, o);
					float3 worldTangent = TransformObjectToWorldDir(v.tangent.xyz);
					o.texcoord2.xyz = worldTangent;
					float3 worldNormal = TransformObjectToWorldNormal(v.normal);
					o.texcoord3.xyz = worldNormal;
					float vertexTangentSign = v.tangent.w * ( unity_WorldTransformParams.w >= 0.0 ? 1.0 : -1.0 );
					float3 worldBitangent = cross( worldNormal, worldTangent ) * vertexTangentSign;
					o.texcoord4.xyz = worldBitangent;
					
					float4 clipPos = TransformObjectToHClip((v.vertex).xyz);
					float4 screenPos = ComputeScreenPos(clipPos);
					o.texcoord5 = screenPos;
					float3 objectToViewPos = TransformWorldToView(TransformObjectToWorld(v.vertex.xyz));
					float eyeDepth = -objectToViewPos.z;
					o.texcoord2.w = eyeDepth;
					float3 worldPos = TransformObjectToWorld( (v.vertex).xyz );
					o.texcoord6.xyz = worldPos;
					
					
					//setting value to unused interpolator channels and avoid initialization warnings
					o.texcoord3.w = 0;
					o.texcoord4.w = 0;
					o.texcoord6.w = 0;
					v.vertex.xyz +=  float3( 0, 0, 0 ) ;
					o.vertex = TransformObjectToHClip(v.vertex.xyz);
					o.color = v.color;
					o.uv = v.texcoord0;
					o.uv1 = v.textoord1;
					return o;
				}

				half4 frag ( v2f i  ) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID( i );
					float2 uv_MainTex = i.uv.xy * _MainTex_ST.xy + _MainTex_ST.zw;
					float lerpResult33 = lerp( ( 0.0 - _Contrast ) , ( _Contrast + 1.0 ) , tex2D( _MainTex, uv_MainTex ).r);
					float clampResult34 = clamp( lerpResult33 , 0.0 , 1.0 );
					float4 temp_output_25_0 = ( clampResult34 * _BaseColor );
					float3 normalizeResult87 = normalize( _MainLightPosition.xyz );
					float2 uv_Normal = i.uv.xy * _Normal_ST.xy + _Normal_ST.zw;
					float2 uv_NoiseTex = i.uv.xy * _NoiseTex_ST.xy + _NoiseTex_ST.zw;
					float2 panner174 = ( 1.0 * _Time.y * _NoiseSpeed + uv_NoiseTex);
					float temp_output_177_0 = ( tex2D( _NoiseTex, panner174 ).r * _NoiseIntensity );
					float3 unpack62 = UnpackNormalScale( tex2D( _Normal, ( uv_Normal + temp_output_177_0 ) ), _NormalScale );
					unpack62.z = lerp( 1, unpack62.z, saturate(_NormalScale) );
					float3 tex2DNode62 = unpack62;
					float4 appendResult63 = (float4(tex2DNode62.r , ( tex2DNode62.g * -1.0 ) , tex2DNode62.b , 0.0));
					float4 NormalTS69 = appendResult63;
					float3 worldTangent = i.texcoord2.xyz;
					float3 worldNormal = i.texcoord3.xyz;
					float3 worldBitangent = i.texcoord4.xyz;
					float3x3 tangentToWorldFast = float3x3(worldTangent.x,worldBitangent.x,worldNormal.x,worldTangent.y,worldBitangent.y,worldNormal.y,worldTangent.z,worldBitangent.z,worldNormal.z);
					float3 tangentToWorldDir165 = normalize( mul( tangentToWorldFast, NormalTS69.xyz ) );
					float3 break120 = -tangentToWorldDir165;
					float4 appendResult118 = (float4(break120.x , break120.y , break120.z , 0.0));
					float dotResult80 = dot( normalizeResult87 , tangentToWorldDir165 );
					float smoothstepResult116 = smoothstep( 0.0 , 1.0 , dotResult80);
					float4 lerpResult138 = lerp( appendResult118 , float4( tangentToWorldDir165 , 0.0 ) , smoothstepResult116);
					float dotResult140 = dot( float4( normalizeResult87 , 0.0 ) , lerpResult138 );
					float Light141 = saturate( dotResult140 );
					float3 clampResult170 = clamp( _MainLightColor.rgb , float3( 0,0,0 ) , float3( 1,1,1 ) );
					
					float4 screenPos = i.texcoord5;
					float4 screenPosNorm = screenPos / screenPos.w;
					screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? screenPosNorm.z : screenPosNorm.z * 0.5 + 0.5;
					float screenDepth2 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( screenPosNorm.xy ),_ZBufferParams);
					float distanceDepth2 = abs( ( screenDepth2 - LinearEyeDepth( screenPosNorm.z,_ZBufferParams ) ) / ( _FadeDistance ) );
					float2 uv_OpacityMap = i.uv.xy * _OpacityMap_ST.xy + _OpacityMap_ST.zw;
					float eyeDepth = i.texcoord2.w;
					float3 worldPos = i.texcoord6.xyz;
					float3 worldViewDir = ( _WorldSpaceCameraPos.xyz - worldPos );
					worldViewDir = normalize(worldViewDir);
					float fresnelNdotV68 = dot( worldNormal, worldViewDir );
					float fresnelNode68 = ( 0.0 + 1.0 * pow( 1.0 - fresnelNdotV68, _FresnelExp ) );
					float temp_output_168_0 = ( 1.0 - saturate( fresnelNode68 ) );
					float lerpResult76 = lerp( 1.0 , temp_output_168_0 , _BlendFresnel);
					float Alpha136 = ( min( distanceDepth2 , tex2D( _OpacityMap, ( uv_OpacityMap + temp_output_177_0 ) ).r ) * saturate( ( eyeDepth / _SurfaceDistance ) ) * lerpResult76 * temp_output_168_0 );
					float2 uv_DissolveTex = i.uv.xy * _DissolveTex_ST.xy + _DissolveTex_ST.zw;
					float2 panner184 = ( 1.0 * _Time.y * _DissovleSpeed + uv_DissolveTex);
					float mulTime196 = _TimeParameters.x * _DissolveTime;
					
					half4 col;
					col.xyz = ( ( _BaseColorIntensity * temp_output_25_0 * Light141 * float4( clampResult170 , 0.0 ) ) + ( temp_output_25_0 * _EmiisonIntensity * float4( clampResult170 , 0.0 ) ) ).rgb;
					col.w = ( Alpha136 * saturate( ( tex2D( _DissolveTex, panner184 ).r + 1.0 + ( ( _Dissolve * saturate( sin( mulTime196 ) ) ) * -2.0 ) ) ) );
					return col;
				}
				ENDHLSL
			}
		}
	}
	
	CustomEditor "ASEMaterialInspector"
	Fallback Off
}/*ASEBEGIN
Version=19108
Node;AmplifyShaderEditor.CommentaryNode;67;176.3842,718.3788;Inherit;False;1221.156;388.5013;UE来的法线图要反转G通道;6;62;63;66;65;42;69;UE来的法线图要反转G通道;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;37;-133.4738,-557.9413;Inherit;False;1491.208;685.6732;颜色;20;54;36;35;55;34;32;33;30;7;25;26;31;29;56;122;123;135;134;167;170;颜色;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;24;-1546.702,173.8391;Inherit;False;1125.893;938.9025;透明度;19;77;75;76;74;68;73;72;12;22;21;20;3;2;13;23;1;169;168;171;透明度;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;29;29.35252,-507.9413;Inherit;False;Constant;_Float0;Float 0;5;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;31;167.3524,-504.9413;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;25;783.7921,-238.0099;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;30;31.35253,-359.9413;Inherit;False;Constant;_Float1;Float 1;5;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;33;347.4525,-383.4413;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;32;181.3523,-378.9413;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;55;1002.312,-321.0506;Inherit;False;4;4;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;3;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;117;1579.22,722.7194;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;118;1721.239,592.4406;Inherit;True;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.ClampOpNode;34;544.6473,-381.7596;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;123;1220.209,-319.5464;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.DotProductOpNode;80;1655.835,172.2587;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;134;1186.801,-217.7206;Inherit;False;69;NormalTS;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.DotProductOpNode;140;2141.949,318.5721;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;143;2279.461,319.7786;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;141;2419.382,315.8908;Inherit;False;Light;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;138;2006.521,435.1687;Inherit;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SmoothstepOpNode;116;1802.324,171.4152;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LightColorNode;167;679.604,-487.8705;Inherit;False;0;3;COLOR;0;FLOAT3;1;FLOAT;2
Node;AmplifyShaderEditor.ClampOpNode;170;842.7698,-486.2933;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;1,1,1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;54;1002.692,-143.8144;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;65;829.5395,903.7333;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;63;965.5394,798.7333;Inherit;True;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;66;684.5392,987.7333;Inherit;False;Constant;_Float2;Float 2;10;0;Create;True;0;0;0;False;0;False;-1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;120;1396.86,591.7324;Inherit;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.RangedFloatNode;119;1405.33,741.7167;Inherit;False;Constant;_Float3;Float 2;10;0;Create;True;0;0;0;False;0;False;-1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;135;1213.989,-130.02;Inherit;False;Constant;_Float6;Float 6;12;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldSpaceLightDirHlpNode;81;1009.474,154.9165;Inherit;False;False;1;0;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.NormalizeNode;87;1218.492,166.8621;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WorldNormalVector;93;1038.587,309.1972;Inherit;False;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.NegateNode;91;1254.36,594.2495;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;69;1192.42,791.6158;Inherit;False;NormalTS;-1;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;70;992.641,447.096;Inherit;False;69;NormalTS;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TransformDirectionNode;165;1218.448,446.9217;Inherit;False;Tangent;World;True;Fast;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GetLocalVarNode;122;744.2985,-30.67056;Inherit;False;141;Light;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;26;-95.64748,-437.9413;Inherit;False;Property;_Contrast;雾块对比度;1;0;Create;False;0;0;0;False;0;False;0;-1;-1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;35;712.567,-356.974;Inherit;False;Property;_BaseColorIntensity;光照强度;6;0;Create;False;0;0;0;False;0;False;0;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;56;732.3748,-125.849;Inherit;False;Property;_EmiisonIntensity;自发光强度;7;0;Create;False;0;0;0;False;0;False;0;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;7;21.52619,-274.7108;Inherit;True;Property;_MainTex;主贴图;4;0;Create;False;0;0;0;False;0;False;-1;None;295048ac004731640a9159463ba034fd;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;36;505.4999,-221.3444;Inherit;False;Property;_BaseColor;主颜色;5;1;[HDR];Create;False;0;0;0;False;0;False;0,0,0,0;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;42;163.3232,947.3717;Inherit;False;Property;_NormalScale;法线强度;9;0;Create;False;0;0;0;False;0;False;1;1;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMinOpNode;13;-774.3038,231.1305;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade;2;-1267.965,223.8391;Inherit;False;True;False;True;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;21;-985.5228,581.5061;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;22;-855.5228,581.5061;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FresnelNode;68;-1102.514,806.32;Inherit;True;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;77;-953.4738,714.533;Inherit;False;Constant;_Float4;Float 4;12;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;72;-1521.935,801.0354;Inherit;False;69;NormalTS;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TransformDirectionNode;73;-1331.513,801.3062;Inherit;False;Tangent;World;True;Fast;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldNormalVector;171;-1475.867,635.1237;Inherit;False;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.LerpOp;76;-590.9801,874.0775;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;74;-1413.406,948.6986;Inherit;False;Property;_FresnelExp;菲尼尔宽度;10;0;Create;False;0;0;0;False;0;False;3;0.5;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;75;-1091.291,1030.243;Inherit;False;Property;_BlendFresnel;菲尼尔透明;11;0;Create;False;0;0;0;False;0;False;0;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SurfaceDepthNode;20;-1299.522,575.5062;Inherit;False;0;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;3;-1479.3,241.597;Inherit;False;Property;_FadeDistance;地表接触距离渐隐;2;0;Create;False;0;0;0;False;0;False;100;1000;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;12;-1197.179,657.3895;Inherit;False;Property;_SurfaceDistance;表面距离渐隐;3;0;Create;False;0;0;0;False;0;False;100;1000;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;169;-834.7925,911.1181;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;168;-740.5923,703.1181;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;23;-571.2081,434.7753;Inherit;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;136;-405.0421,450.8569;Inherit;False;Alpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;177;-1861.039,751.061;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;176;-1702.739,473.6611;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;1;-1319.72,335.1767;Inherit;True;Property;_OpacityMap;云透明贴图;0;0;Create;False;0;0;0;False;0;False;-1;None;5b90b474c5a148d4c956bd8bb793046b;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;179;-1953.55,312.8615;Inherit;False;0;1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;174;-2512.191,658.9659;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;173;-2890.191,601.9659;Inherit;False;0;172;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;172;-2179.151,608.0099;Inherit;True;Property;_NoiseTex;扰动纹理;12;0;Create;False;0;0;0;False;0;False;-1;None;295048ac004731640a9159463ba034fd;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;175;-2819.191,768.9659;Inherit;False;Property;_NoiseSpeed;扰动速度;13;0;Create;False;0;0;0;False;0;False;0,0;0.3,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;178;-2257.039,897.512;Inherit;False;Property;_NoiseIntensity;扰动强度;14;0;Create;False;0;0;0;False;0;False;0;0.1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;62;501.6273,768.3788;Inherit;True;Property;_Normal;法线图;8;0;Create;False;0;0;0;False;0;False;-1;None;622326949ec8ccf45bf311018092aef8;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;180;-202.4011,685.3591;Inherit;False;0;62;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;181;50.93701,859.6973;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;164;2025.755,-328.7793;Float;False;True;-1;2;ASEMaterialInspector;0;14;X3/Scene/EffectFog;bc75d45cc0374e04ab709b84dd15831a;True;Forward;0;0;Forward;3;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;Queue=Transparent=Queue=0;RenderType=Transparent=RenderType;True;3;True;12;all;0;True;True;8;5;False;;1;False;;8;5;False;;1;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;False;False;False;0;;0;0;Standard;0;0;1;True;False;;False;0
Node;AmplifyShaderEditor.GetLocalVarNode;137;1501.472,-130.4403;Inherit;False;136;Alpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;184;1755.371,-3.255402;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;183;1407.371,-36.2554;Inherit;False;0;182;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;187;2495.371,-14.25537;Inherit;True;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;188;2429.371,143.7446;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;190;2230.371,213.7446;Inherit;False;Constant;_Float7;Float 7;17;0;Create;True;0;0;0;False;0;False;-2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;191;2330.371,64.74463;Inherit;False;Constant;_Float8;Float 8;17;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;182;1968.371,-76.2554;Inherit;True;Property;_DissolveTex;溶解贴图;15;0;Create;False;0;0;0;False;0;False;-1;None;295048ac004731640a9159463ba034fd;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;185;1496.371,94.7446;Inherit;False;Property;_DissovleSpeed;溶解速度;16;0;Create;False;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;192;2896.371,-125.2554;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;193;2778.371,65.7446;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SinOpNode;195;2623.872,557.7822;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;196;2323.872,546.7822;Inherit;True;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;198;2938.872,563.7822;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;199;3193.872,540.7822;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;189;1965.371,132.7446;Inherit;False;Property;_Dissolve;溶解上限;17;0;Create;False;0;0;0;False;0;False;1;0.45;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;197;2150.872,611.7822;Inherit;False;Property;_DissolveTime;溶解周期;18;0;Create;False;0;0;0;False;0;False;0;1;0;0;0;1;FLOAT;0
WireConnection;31;0;29;0
WireConnection;31;1;26;0
WireConnection;25;0;34;0
WireConnection;25;1;36;0
WireConnection;33;0;31;0
WireConnection;33;1;32;0
WireConnection;33;2;7;1
WireConnection;32;0;26;0
WireConnection;32;1;30;0
WireConnection;55;0;35;0
WireConnection;55;1;25;0
WireConnection;55;2;122;0
WireConnection;55;3;170;0
WireConnection;117;0;120;2
WireConnection;117;1;119;0
WireConnection;118;0;120;0
WireConnection;118;1;120;1
WireConnection;118;2;120;2
WireConnection;34;0;33;0
WireConnection;123;0;55;0
WireConnection;123;1;54;0
WireConnection;80;0;87;0
WireConnection;80;1;165;0
WireConnection;140;0;87;0
WireConnection;140;1;138;0
WireConnection;143;0;140;0
WireConnection;141;0;143;0
WireConnection;138;0;118;0
WireConnection;138;1;165;0
WireConnection;138;2;116;0
WireConnection;116;0;80;0
WireConnection;170;0;167;1
WireConnection;54;0;25;0
WireConnection;54;1;56;0
WireConnection;54;2;170;0
WireConnection;65;0;62;2
WireConnection;65;1;66;0
WireConnection;63;0;62;1
WireConnection;63;1;65;0
WireConnection;63;2;62;3
WireConnection;120;0;91;0
WireConnection;87;0;81;0
WireConnection;91;0;165;0
WireConnection;69;0;63;0
WireConnection;165;0;70;0
WireConnection;13;0;2;0
WireConnection;13;1;1;1
WireConnection;2;0;3;0
WireConnection;21;0;20;0
WireConnection;21;1;12;0
WireConnection;22;0;21;0
WireConnection;68;3;74;0
WireConnection;73;0;72;0
WireConnection;76;0;77;0
WireConnection;76;1;168;0
WireConnection;76;2;75;0
WireConnection;169;0;68;0
WireConnection;168;0;169;0
WireConnection;23;0;13;0
WireConnection;23;1;22;0
WireConnection;23;2;76;0
WireConnection;23;3;168;0
WireConnection;136;0;23;0
WireConnection;177;0;172;1
WireConnection;177;1;178;0
WireConnection;176;0;179;0
WireConnection;176;1;177;0
WireConnection;1;1;176;0
WireConnection;174;0;173;0
WireConnection;174;2;175;0
WireConnection;172;1;174;0
WireConnection;62;1;181;0
WireConnection;62;5;42;0
WireConnection;181;0;180;0
WireConnection;181;1;177;0
WireConnection;164;0;123;0
WireConnection;164;1;192;0
WireConnection;184;0;183;0
WireConnection;184;2;185;0
WireConnection;187;0;182;1
WireConnection;187;1;191;0
WireConnection;187;2;188;0
WireConnection;188;0;199;0
WireConnection;188;1;190;0
WireConnection;182;1;184;0
WireConnection;192;0;137;0
WireConnection;192;1;193;0
WireConnection;193;0;187;0
WireConnection;195;0;196;0
WireConnection;196;0;197;0
WireConnection;198;0;195;0
WireConnection;199;0;189;0
WireConnection;199;1;198;0
ASEEND*/
//CHKSM=7C609D211A212914B79C54D247D93C41A81EFA48