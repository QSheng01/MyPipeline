// Made with Amplify Shader Editor v1.9.1.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Hidden/ArtToolkit/TexRotateBlur"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "white" {}
		_XPoint("XPoint", Float) = 0.5
		_YPoint("YPoint", Float) = 0.5
		_Count("Count", Float) = 300

	}
	
	SubShader
	{
		
		
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
	LOD 100

		CGINCLUDE
		#pragma target 3.0
		ENDCG
		Blend SrcAlpha OneMinusSrcAlpha
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

			uniform float _Count;
			uniform sampler2D _MainTex;
			uniform float4 _MainTex_ST;
			uniform float _XPoint;
			uniform float _YPoint;

			
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
				sampler2D MainTex71 = _MainTex;
				float4 EndColor71 = float4( 0,0,0,0 );
				float2 appendResult65 = (float2(_XPoint , _YPoint));
				float2 Point71 = appendResult65;
				float Rotate71 = ( 0.01 * UNITY_PI );
				{
				float rrr = 0.5f ;
				float2 EndUV = UV71;
				for(int i = 0; i < Count71;i++)
				{
				rrr = lerp( 0.005, 0.0 , float(i)/Count71);
				//rrr = (i/Count71)/Rotate71;
				EndUV =mul( EndUV-Point71,float2x2( cos(rrr),-sin(rrr),sin(rrr),cos(rrr) ))+Point71;
				EndColor71+= tex2D(MainTex71,EndUV);
				}
				//EndColor71 /= Count71;
				EndColor71 /= Count71;
				}
				float4 tex2DNode114 = tex2D( _MainTex, uv_MainTex );
				float4 ifLocalVar112 = 0;
				if( _Count <= 1.0 )
				ifLocalVar112 = tex2DNode114;
				else
				ifLocalVar112 = EndColor71;
				
				
				finalColor = ifLocalVar112;
				return 	finalColor;
			}
			ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
	
	Fallback Off
}
/*ASEBEGIN
Version=19105
Node;AmplifyShaderEditor.RangedFloatNode;63;-2383.992,1394.577;Inherit;False;Property;_XPoint;XPoint;1;0;Create;True;0;0;0;False;0;False;0.5;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;64;-2385.992,1465.577;Inherit;False;Property;_YPoint;YPoint;2;0;Create;True;0;0;0;False;0;False;0.5;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;79;-2178.895,1450.792;Inherit;False;Constant;_Rotate;Rotate;5;0;Create;True;0;0;0;False;0;False;0.01;2.4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;72;-2612.515,1288.993;Inherit;False;Property;_Count;Count;3;0;Create;False;0;0;0;False;0;False;300;133.14;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;61;-2827.782,1209.882;Inherit;False;0;1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;65;-2204.993,1356.577;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode;1;-2412.108,817.1096;Inherit;True;Property;_MainTex;MainTex;0;0;Create;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.PiNode;84;-1960.484,1452.663;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;71;-1568.395,993.1071;Inherit;False;float rrr = 0.5f @$float2 EndUV = UV@$for(int i = 0@ i < Count@i++)${$rrr = lerp( 0.005, 0.0 , i/Count)@$//rrr = (i/Count)/Rotate@$EndUV =mul( EndUV-Point,float2x2( cos(rrr),-sin(rrr),sin(rrr),cos(rrr) ))+Point@$EndColor+= tex2D(MainTex,EndUV)@$}$//EndColor /= Count@$EndColor /= Count@;7;Create;6;True;UV;FLOAT2;0,0;In;;Float;False;True;Count;INT;0;In;;Half;False;True;MainTex;SAMPLER2D;;In;;Float;False;True;EndColor;FLOAT4;0,0,0,0;Out;;Float;False;True;Point;FLOAT2;0,0;In;;Float;False;True;Rotate;FLOAT;0;In;;Float;False;FSBlur;True;False;0;;False;7;0;FLOAT;0;False;1;FLOAT2;0,0;False;2;INT;0;False;3;SAMPLER2D;;False;4;FLOAT4;0,0,0,0;False;5;FLOAT2;0,0;False;6;FLOAT;0;False;2;FLOAT;0;FLOAT4;5
Node;AmplifyShaderEditor.SamplerNode;114;-1641.911,756.5729;Inherit;True;Property;_TextureSample1;Texture Sample 1;5;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;113;-1180.911,1140.573;Inherit;False;Constant;_Float0;Float 0;5;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ConditionalIfNode;112;-892.3571,1044.993;Inherit;False;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT4;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;0;-706.12,1031.844;Float;False;True;-1;2;ASEMaterialInspector;100;5;VFXTools/Editor/TexRotateBlur;0770190933193b94aaa3065e307002fa;True;Unlit;0;0;Unlit;2;False;True;2;5;False;;10;False;;0;1;False;;0;False;;True;0;False;;0;False;;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;2;False;;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;True;2;False;;True;3;False;;True;False;0;False;;0;False;;True;2;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;True;2;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;0;;0;0;Standard;1;Vertex Position,InvertActionOnDeselection;1;0;0;1;True;False;;False;0
WireConnection;65;0;63;0
WireConnection;65;1;64;0
WireConnection;84;0;79;0
WireConnection;71;1;61;0
WireConnection;71;2;72;0
WireConnection;71;3;1;0
WireConnection;71;5;65;0
WireConnection;71;6;84;0
WireConnection;114;0;1;0
WireConnection;114;1;61;0
WireConnection;112;0;72;0
WireConnection;112;1;113;0
WireConnection;112;2;71;5
WireConnection;112;3;114;0
WireConnection;112;4;114;0
WireConnection;0;0;112;0
ASEEND*/
//CHKSM=2B8B59E1FBB6E87DC432149F80603A4C6EF80535