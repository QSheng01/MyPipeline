Shader "Hidden/ArtToolkit/CropTarget"
//Shader "VFXTools/Editor/CropTarget"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Till ("Till",Vector) = (0,0,0,0) 
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Till;
            float _ColorSpaceValue;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float x=i.uv.x*_Till.z+_Till.x;
                float y=(i.uv.y-1)*_Till.w+_Till.y;
                //float y=-(_Till.w-i.uv.y*_Till.w-_Till.y);
                fixed4 col = tex2D(_MainTex,float2(x,y));
                return col;
                return col;
            }
            ENDCG
        }
    }
}
