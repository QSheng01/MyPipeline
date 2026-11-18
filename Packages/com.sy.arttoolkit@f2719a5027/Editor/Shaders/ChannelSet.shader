Shader "Hidden/ArtToolkit/ChannelSet"
//Shader "Unlit/ChannelSet"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Channel("Channel",int) = 0
        _Value("Value",float) = 0
    }
    SubShader
    {

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
            int _Channel;
            float _Value;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                switch (_Channel)
                {
                    case 1:
                        col.r=_Value;
                        break;
                    case 2:
                        col.g=_Value;
                        break;
                    case 3:
                        col.b=_Value;
                        break;
                    case 4:
                        col.a=_Value;
                        break;
                    
                }
                return col;
            }
            ENDCG
        }
    }
}
