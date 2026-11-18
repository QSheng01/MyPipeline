Shader "PRT/SHTex3Ddebug"
{
    Properties
    {
       _Rate("_Rate", Range( 0 , 100)) =50
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalRenderPipeline" "Queue"="Geometry"}
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "SH.hlsl"

            Texture3D<float4> _Tex3DGIL0;
            Texture3D<float4> _Tex3DGIL10;
            Texture3D<float4> _Tex3DGIL11;
            Texture3D<float4> _Tex3DGIL12;
            SamplerState  sampler_Tex3DGIL0;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD2;
                float3 posWS :TEXCOORD3;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.normal = TransformObjectToWorldNormal(v.normal);
                o.normal = normalize(o.normal);
                o.posWS = TransformObjectToWorld(v.vertex);
                return o;
            }

            float4 _GITexMin;
            float4 _GITexMax;
            float _Rate;


            
            float SH2(in int l, in int m, in float3 s)
            {
                #define k01 0.2820947918    // sqrt(  1/PI)/2
                #define k02 0.4886025119    // sqrt(  3/PI)/2
                #define k03 1.0925484306    // sqrt( 15/PI)/2
                #define k04 0.3153915652    // sqrt(  5/PI)/4
                #define k05 0.5462742153    // sqrt( 15/PI)/4

                //float3 n = s.zxy;
                float x = s.x;
                float y = s.z;
                float z = s.y;

                //----------------------------------------------------------
                if( l==0 )          return  k01;
                //----------------------------------------------------------
                if( l==1 && m==-1 ) return  k02*y;
                if( l==1 && m== 0 ) return  k02*z;
                if( l==1 && m== 1 ) return  k02*x;
                //----------------------------------------------------------
                if( l==2 && m==-2 ) return  k03*x*y;
                if( l==2 && m==-1 ) return  k03*y*z;
                if( l==2 && m== 0 ) return  k04*(2.0*z*z-x*x-y*y);
                if( l==2 && m== 1 ) return  k03*x*z;
                if( l==2 && m== 2 ) return  k05*(x*x-y*y);

                return 0.0;
            }
            half4 SampleProbeVolume (half4 normal, float3 worldPos)
            {
                float3 texCoord = float3(0,0,0);
                float3 position = worldPos;
                //position+=normal.xyz*1;
                //uv对应的世界空间采样范围
                //min max 版本
                texCoord.x = (position.x-_GITexMin.x)/(_GITexMax.x-_GITexMin.x);
                texCoord.y = (position.y-_GITexMin.y)/(_GITexMax.y-_GITexMin.y);
                texCoord.z = (position.z-_GITexMin.z)/(_GITexMax.z-_GITexMin.z);
                
                
                half4 SH_L0 = SAMPLE_TEXTURE3D(_Tex3DGIL0,sampler_Tex3DGIL0,texCoord);
                half4 SH_L10 = SAMPLE_TEXTURE3D(_Tex3DGIL10,sampler_Tex3DGIL0,texCoord);
                half4 SH_L11 = SAMPLE_TEXTURE3D(_Tex3DGIL11,sampler_Tex3DGIL0,texCoord);
                half4 SH_L12 = SAMPLE_TEXTURE3D(_Tex3DGIL12,sampler_Tex3DGIL0,texCoord);

                half rate = _Rate;
                SH_L0*=rate;
                SH_L10*=rate;
                SH_L11*=rate;
                SH_L12*=rate;


                half4 volumeGI = half4(0,0,0,0);
                volumeGI += SH2(0,0,normal)*SH_L0*PI;
                #define A1 2.0943

                volumeGI.xyz += SH2(1,-1,normal) * half3(SH_L10.x,SH_L10.y,SH_L10.z)*A1;
                volumeGI.xyz += SH2(1,0,normal) * half3(SH_L11.x,SH_L11.y,SH_L11.z) *A1;
                volumeGI.xyz += SH2(1,1,normal) * half3(SH_L12.x,SH_L12.y,SH_L12.z) *A1;

                volumeGI.xyz/=PI;

                return volumeGI;
            }

            
            float4 frag (v2f i) : SV_Target
            {
                float3 posWS = i.posWS;
                float3 dir = i.normal;
                float3 tex3DUvw = float3(posWS);

                
                half3 GIColor = SampleProbeVolume(half4(i.normal,1),posWS);

                
                return float4(GIColor.xyz, 1.0);
            }
            ENDHLSL
        }
    }
}
