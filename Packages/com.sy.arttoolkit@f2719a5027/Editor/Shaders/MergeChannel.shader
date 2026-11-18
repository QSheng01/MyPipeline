// Made with Amplify Shader Editor v1.9.1.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Hidden/ArtToolkit/MergeChannel"
{
	Properties
	{
		_TextureR("Tex0", 2D) = "black" {}
		_TextureG("Tex1", 2D) = "black" {}
		_TextureB("Tex2", 2D) = "black" {}
		_TextureA("Tex3", 2D) = "white" {}
		_AboutChannel("_AboutChannel",int)=0
	}
	
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
	LOD 100
		CGINCLUDE
		#pragma target 3.0
		ENDCG
		Blend Off
		Cull Off
		ZWrite Off
		
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
				float4 uv : TEXCOORD0;
			};
			
			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv:TEXCOORD0;
			};

			uniform float _ColorSpaceValue;
			uniform sampler2D _TextureR;
			uniform sampler2D _TextureG;
			uniform sampler2D _TextureB;
			uniform sampler2D _TextureA;
			uniform int _AboutChannel;

			v2f vert ( appdata v )
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv=v.uv.xy;
				return o;
			}
			
			float4 frag (v2f i ) : SV_Target
			{
				float4 RColor = tex2D(_TextureR,i.uv);
				float4 GColor = tex2D(_TextureG,i.uv);
				float4 BColor = tex2D(_TextureB,i.uv);
				float4 AColor = tex2D(_TextureA,i.uv);
				float R = RColor[log2(15 &_AboutChannel)];
				float G = GColor[log2(15 &_AboutChannel>>5)];
				float B = BColor[log2(15 &_AboutChannel>>10)];
				float A = AColor[log2(15 &_AboutChannel>>15)];
				return float4(R,G,B,A);
			}
			ENDCG
		}
	}
	
	Fallback Off
}
