Shader "Hidden/ArtToolkit/ReplacementShader"
{
    Properties
    {
        _Cull ("Cull Mode", Range(0 , 2)) = 0
        _ZWrite ("ZWrite", Range(0, 1)) = 0
            



    }
    SubShader{
        
            Tags { "RenderType"="Transparent" }
            LOD 100
            Cull [_Cull]
            Blend One One
            Zwrite [_ZWrite]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
            };
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                return float4(0.02,0,0,1);
            }
            ENDCG
        }
    }
}
