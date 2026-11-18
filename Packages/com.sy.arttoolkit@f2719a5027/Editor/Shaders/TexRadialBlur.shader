// Made with Amplify Shader Editor v1.9.1.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Hidden/ArtToolkit/TexRadialBlur"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "white" {}
		_XPoint("XPoint", Float) = 0.5
		_YPoint("YPoint", Float) = 0.5
		_BlurScale("BlurScale", Float) = 0
		_Count("Count", Float) = 300
		_MaskRange("MaskRange", Range( -1.5 , 5)) = 1.663781
		_MaskPower("MaskPower", Range( 0 , 20)) = 0.2352941

	}
	
	SubShader
	{
		
		
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
	LOD 100

		CGINCLUDE
		#pragma target 3.0
		ENDCG
		Blend Off
		AlphaToMask Off
		Cull Off
		ColorMask RGBA
		ZWrite Off
		ZTest LEqual
		
		
		
		Pass
		{
			Name "Unlit"

			CGPROGRAM

			

			#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
			//only defining to not throw compilation error over Unity 5.5
			#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
			#endif
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"
			

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			
			struct v2f
			{
				float4 vertex : SV_POSITION;
				#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				float3 worldPos : TEXCOORD0;
				#endif
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			uniform sampler2D _MainTex;
			uniform float4 _MainTex_ST;
			uniform float _Count;
			uniform float _BlurScale;
			uniform float _XPoint;
			uniform float _YPoint;
			uniform float _MaskRange;
			uniform float _MaskPower;

			
			v2f vert ( appdata v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				o.ase_texcoord1.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord1.zw = 0;
				float3 vertexValue = float3(0, 0, 0);
				#if ASE_ABSOLUTE_VERTEX_POS
				vertexValue = v.vertex.xyz;
				#endif
				vertexValue = vertexValue;
				#if ASE_ABSOLUTE_VERTEX_POS
				v.vertex.xyz = vertexValue;
				#else
				v.vertex.xyz += vertexValue;
				#endif
				o.vertex = UnityObjectToClipPos(v.vertex);

				#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				#endif
				return o;
			}
			float _ColorSpaceValue;
			fixed4 frag (v2f i ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				fixed4 finalColor;
				#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				float3 WorldPosition = i.worldPos;
				#endif
				float localFSBlur71 = ( 0.0 );
				float2 uv_MainTex = i.ase_texcoord1.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float2 UV71 = uv_MainTex;
				int Count71 = (int)_Count;
				float2 appendResult65 = (float2(_XPoint , _YPoint));
				float4 _Vector0 = float4(2,-1,1,-0.5);
				float temp_output_77_0 = length( (( uv_MainTex - ( appendResult65 - float2( 0.5,0.5 ) ) )*( _Vector0.x + _MaskRange ) + ( _Vector0.y + -( _MaskRange * 0.5 ) )) );
				float BlurScale71 = ( ( _BlurScale * 1E-05 ) * saturate( pow( temp_output_77_0 , _MaskPower ) ) );
				sampler2D MainTex71 = _MainTex;
				float4 EndColor71 = float4( 0,0,0,0 );
				float2 Point71 = appendResult65;
				{
				float2 Dir = UV71-Point71;
				for(int i = 0; i < Count71;i++)
				{
				UV71 = UV71-(BlurScale71*Dir*i);
				EndColor71+= tex2D(MainTex71,UV71);
				}
				EndColor71 /= Count71;
				}
				
				
				finalColor = saturate( EndColor71 );
				return finalColor;
			}
			ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
	
	Fallback Off
}
/*ASEBEGIN
Version=19105
Node;AmplifyShaderEditor.RangedFloatNode;89;-3026.215,2186.993;Inherit;False;Property;_MaskRange;MaskRange;5;0;Create;True;0;0;0;False;0;False;1.663781;0;-1.5;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;63;-2939.993,1438.077;Inherit;False;Property;_XPoint;XPoint;1;0;Create;True;0;0;0;False;0;False;0.5;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;64;-2947.993,1512.077;Inherit;False;Property;_YPoint;YPoint;2;0;Create;True;0;0;0;False;0;False;0.5;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;91;-2710.015,2179.692;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;65;-2782.994,1445.077;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;61;-2823.607,1232.681;Inherit;False;0;1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;109;-2902.49,1697.103;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.NegateNode;93;-2566.015,2186.692;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node;86;-2928.015,1998.692;Inherit;False;Constant;_Vector0;Vector 0;5;0;Create;True;0;0;0;False;0;False;2,-1,1,-0.5;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;108;-2637.288,1693.204;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;88;-2562.015,2094.692;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;87;-2612.015,1934.692;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;78;-2452.606,1827.441;Inherit;True;3;0;FLOAT2;0,0;False;1;FLOAT;2;False;2;FLOAT;-1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;81;-2170.606,2113.641;Inherit;False;Property;_MaskPower;MaskPower;6;0;Create;True;0;0;0;False;0;False;0.2352941;7.288374;0;20;0;1;FLOAT;0
Node;AmplifyShaderEditor.LengthOpNode;77;-2206.533,1820.854;Inherit;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;80;-1339.56,1965.289;Inherit;True;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;76;-1640.072,1719.389;Inherit;False;Constant;_Float1;Float 1;6;0;Create;True;0;0;0;False;0;False;1E-05;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;67;-1655.46,1645.712;Inherit;False;Property;_BlurScale;BlurScale;3;0;Create;False;0;0;0;False;0;False;0;74.63;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;106;-1094.249,1962.79;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;75;-1483.273,1643.79;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;1;-2122.149,1095.176;Inherit;True;Property;_MainTex;MainTex;0;0;Create;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;105;-1338.449,1673.789;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;72;-1738.64,1447.491;Inherit;False;Property;_Count;Count;4;0;Create;False;0;0;0;False;0;False;300;20.72;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;71;-1252.539,1213.592;Inherit;False;float2 Dir = UV-Point@$for(int i = 0@ i < Count@i++)${$UV = UV-(BlurScale*Dir*i)@$EndColor+= tex2D(MainTex,UV)@$}$EndColor /= Count@;7;Create;6;True;UV;FLOAT2;0,0;In;;Float;False;True;Count;INT;0;In;;Half;False;True;BlurScale;FLOAT;0;In;;Float;False;True;MainTex;SAMPLER2D;;In;;Float;False;True;EndColor;FLOAT4;0,0,0,0;Out;;Float;False;True;Point;FLOAT2;0,0;In;;Float;False;FSBlur;True;False;0;;False;7;0;FLOAT;0;False;1;FLOAT2;0,0;False;2;INT;0;False;3;FLOAT;0;False;4;SAMPLER2D;;False;5;FLOAT4;0,0,0,0;False;6;FLOAT2;0,0;False;2;FLOAT;0;FLOAT4;6
Node;AmplifyShaderEditor.OneMinusNode;79;-1958.191,1771.004;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;110;-878.3896,1366.903;Inherit;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;0;-605.0168,1197.661;Float;False;True;-1;2;ASEMaterialInspector;100;5;VFXTools/Editor/TexRadialBlur;0770190933193b94aaa3065e307002fa;True;Unlit;0;0;Unlit;2;False;True;0;5;False;;10;False;;0;1;False;;0;False;;True;0;False;;0;False;;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;2;False;;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;True;2;False;;True;3;False;;True;False;0;False;;0;False;;True;2;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;True;2;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;0;;0;0;Standard;1;Vertex Position,InvertActionOnDeselection;1;0;0;1;True;False;;False;0
WireConnection;91;0;89;0
WireConnection;65;0;63;0
WireConnection;65;1;64;0
WireConnection;109;0;65;0
WireConnection;93;0;91;0
WireConnection;108;0;61;0
WireConnection;108;1;109;0
WireConnection;88;0;86;2
WireConnection;88;1;93;0
WireConnection;87;0;86;1
WireConnection;87;1;89;0
WireConnection;78;0;108;0
WireConnection;78;1;87;0
WireConnection;78;2;88;0
WireConnection;77;0;78;0
WireConnection;80;0;77;0
WireConnection;80;1;81;0
WireConnection;106;0;80;0
WireConnection;75;0;67;0
WireConnection;75;1;76;0
WireConnection;105;0;75;0
WireConnection;105;1;106;0
WireConnection;71;1;61;0
WireConnection;71;2;72;0
WireConnection;71;3;105;0
WireConnection;71;4;1;0
WireConnection;71;6;65;0
WireConnection;79;0;77;0
WireConnection;110;0;71;6
WireConnection;0;0;110;0
ASEEND*/
//CHKSM=EA45B4F89F58F91CD1EFF9952B70FF101CD4EE0C