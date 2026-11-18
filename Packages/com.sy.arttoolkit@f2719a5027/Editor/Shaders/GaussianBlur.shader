// Made with Amplify Shader Editor v1.9.1.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Hidden/ArtToolkit/GaussianBlur"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "white" {}
		_BlurSpread("BlurSpread", Range( 0 , 0.1)) = 0.1
		_offset1("offset1", Vector) = (1,0,0,0)
		_offset2("offset2", Vector) = (-1,0,0,0)
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
			
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
			};
			
			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 ase_texcoord1 : TEXCOORD1;
			};

			uniform sampler2D _MainTex;
			uniform float2 _offset1;
			uniform float _BlurSpread;
			uniform float2 _offset2;
			uniform float _ColorSpaceValue;
			
			v2f vert ( appdata v )
			{
				v2f o;
				o.ase_texcoord1.xy = v.ase_texcoord.xy;
				o.ase_texcoord1.zw = 0;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}
			fixed4 frag (v2f i ) : SV_Target
			{
				fixed4 finalColor; 
				float2 UV13 = i.ase_texcoord1.xy;
				finalColor = saturate( ( ( tex2D( _MainTex, UV13 ) * 0.4 ) + ( tex2D( _MainTex, ( ( ( _offset1 * 1.0 ) * _BlurSpread ) + UV13 ) ) * 0.15 ) + ( tex2D( _MainTex, ( ( ( _offset1 * 2.0 ) * _BlurSpread ) + UV13 ) ) * 0.1 ) + ( tex2D( _MainTex, ( UV13 + ( ( _offset2 * 1.0 ) * _BlurSpread ) ) ) * 0.15 ) + ( tex2D( _MainTex, ( UV13 + ( ( _offset2 * 2.0 ) * _BlurSpread ) ) ) * 0.1 ) + ( 0.05 * tex2D( _MainTex, ( ( ( _offset1 * 3.0 ) * _BlurSpread ) + UV13 ) ) ) + ( 0.05 * tex2D( _MainTex, ( UV13 + ( ( _offset2 * 3.0 ) * _BlurSpread ) ) ) ) ) );

				return finalColor;
			}
			ENDCG
		}
	}
	Fallback Off
}
