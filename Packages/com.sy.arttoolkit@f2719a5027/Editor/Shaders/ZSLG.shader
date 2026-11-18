// Made with Amplify Shader Editor v1.9.1.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "VFXTools/Editor/ZSLG"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "white" {}
		_NoiseTiling("NoiseTiling", Vector) = (3,3,-1,0)
		_AngleTimeScale("AngleTimeScale", Float) = 0
		_MainColor("MainColor", Color) = (1,1,1,1)
		_Panner("Panner", Vector) = (1,1,0,0)

	}
	
	SubShader
	{
		
		
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
	LOD 100

		CGINCLUDE
		#pragma target 3.0
		ENDCG
		Blend One One
		AlphaToMask Off
		Cull Off
		ColorMask RGBA
		ZWrite Off
		ZTest LEqual
		Offset 0 , 0
		
		
		
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
			#include "UnityShaderVariables.cginc"


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
			uniform float4 _Panner;
			uniform float _AngleTimeScale;
			uniform float4 _NoiseTiling;
			uniform float4 _MainColor;
			uniform float _ColorSpaceValue;
					float2 voronoihash195( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi195( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0;
						for ( int j = -1; j <= 1; j++ )
						{
							for ( int i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash195( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.707 * sqrt(dot( r, r ));
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						
						 		}
						 	}
						}
						return F1;
					}
			

			
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
			
			fixed4 frag (v2f i ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				fixed4 finalColor;
				#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				float3 WorldPosition = i.worldPos;
				#endif
				float mulTime192 = _Time.y * _Panner.z;
				float2 appendResult191 = (float2(_Panner.x , _Panner.y));
				float2 texCoord181 = i.ase_texcoord1.xy * float2( 1,1 ) + float2( 0,0 );
				float2 panner189 = ( mulTime192 * appendResult191 + texCoord181);
				float mulTime178 = _Time.y * _AngleTimeScale;
				float time195 = mulTime178;
				float2 voronoiSmoothId195 = 0;
				float2 appendResult173 = (float2(_NoiseTiling.x , _NoiseTiling.y));
				float2 appendResult172 = (float2(_NoiseTiling.z , _NoiseTiling.w));
				float2 texCoord177 = i.ase_texcoord1.xy * appendResult173 + appendResult172;
				float2 coords195 = texCoord177 * 1.0;
				float2 id195 = 0;
				float2 uv195 = 0;
				float voroi195 = voronoi195( coords195, time195, id195, uv195, 0, voronoiSmoothId195 );
				float2 smoothstepResult196 = smoothstep( float2( 0,0 ) , float2( 1,0.0001 ) , id195);
				float2 appendResult187 = (float2(panner189.x , smoothstepResult196.x));
				float4 temp_cast_2 = (_ColorSpaceValue).xxxx;
				
				
				finalColor = pow( ( tex2D( _MainTex, appendResult187 ) * _MainColor ) , temp_cast_2 );
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
Node;AmplifyShaderEditor.Vector4Node;170;-2433.187,-354.424;Inherit;False;Property;_NoiseTiling;NoiseTiling;1;0;Create;True;0;0;0;False;0;False;3,3,-1,0;5,0.7,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;172;-2233.103,-262.0417;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;173;-2232.547,-352.6628;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;176;-2099.303,56.98377;Inherit;False;Property;_AngleTimeScale;AngleTimeScale;2;0;Create;True;0;0;0;False;0;False;0;0.35;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;178;-1848.303,55.98377;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node;190;-1799.439,-605.4742;Inherit;False;Property;_Panner;Panner;4;0;Create;True;0;0;0;False;0;False;1,1,0,0;1,1,1,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;177;-1853.39,-307.7053;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleTimeNode;192;-1573.439,-484.4742;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;181;-1752.303,-776.0163;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VoronoiNode;195;-1600.203,-240.7838;Inherit;True;0;1;1;0;1;False;1;False;False;False;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;3;FLOAT;0;FLOAT2;1;FLOAT2;2
Node;AmplifyShaderEditor.DynamicAppendNode;191;-1585.439,-621.4742;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;189;-1315.439,-528.4742;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SmoothstepOpNode;196;-1316.203,-214.7838;Inherit;True;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT2;1,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;187;-1106.439,-533.4742;Inherit;True;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ColorNode;185;-823.4395,-142.4742;Inherit;False;Property;_MainColor;MainColor;3;0;Create;True;0;0;0;False;0;False;1,1,1,1;0.2745098,0.2745098,0.2745098,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;182;-870.0783,-327.6198;Inherit;True;Property;_MainTex;MainTex;0;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;144;-820.4679,387.967;Inherit;False;Global;_ColorSpaceValue;_ColorSpaceValue;6;0;Create;True;0;0;0;True;0;False;2.2;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;184;-541.4395,-269.4742;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.PowerNode;143;-296.5932,-122.0428;Inherit;True;False;2;0;COLOR;0,0,0,0;False;1;FLOAT;1;False;1;COLOR;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;0;-14.3972,21.02365;Float;False;True;-1;2;ASEMaterialInspector;100;5;VFXTools/Editor/ZSLG;0770190933193b94aaa3065e307002fa;True;Unlit;0;0;Unlit;2;False;True;4;1;False;;1;False;;0;1;False;;0;False;;True;0;False;;0;False;;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;2;False;;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;True;2;False;;True;3;False;;True;True;0;False;;0;False;;True;2;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;True;2;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;0;;0;0;Standard;1;Vertex Position,InvertActionOnDeselection;1;0;0;1;True;False;;False;0
WireConnection;172;0;170;3
WireConnection;172;1;170;4
WireConnection;173;0;170;1
WireConnection;173;1;170;2
WireConnection;178;0;176;0
WireConnection;177;0;173;0
WireConnection;177;1;172;0
WireConnection;192;0;190;3
WireConnection;195;0;177;0
WireConnection;195;1;178;0
WireConnection;191;0;190;1
WireConnection;191;1;190;2
WireConnection;189;0;181;0
WireConnection;189;2;191;0
WireConnection;189;1;192;0
WireConnection;196;0;195;1
WireConnection;187;0;189;0
WireConnection;187;1;196;0
WireConnection;182;1;187;0
WireConnection;184;0;182;0
WireConnection;184;1;185;0
WireConnection;143;0;184;0
WireConnection;143;1;144;0
WireConnection;0;0;143;0
ASEEND*/
//CHKSM=1B29630C48936F2000AD911CD667329E7304E1BA