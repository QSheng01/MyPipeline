Shader "PRT/SHDebug"
{
    Properties
    {

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

            // 使用定点数存储小数, 因为 compute shader 的 InterlockedAdd 不支持 float
            // array size: 3x9=27
            //CBUFFER_START(UnityPerMaterial)
                StructuredBuffer<int> _coefficientSH9; 
            //CBUFFER_END

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.normal = TransformObjectToWorldNormal(v.normal);
                o.normal = normalize(o.normal);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 dir = i.normal;

                // decode sh
                
                float3 c[9];
                for(int i=0; i<9; i++)
                {
                    c[i].x = DecodeFloatFromInt(_coefficientSH9[i*3+0]);
                    //c[i].y = c[i].x;
                    //c[i].z = c[i].x;
                    c[i].y = DecodeFloatFromInt(_coefficientSH9[i*3+1]);
                    c[i].z = DecodeFloatFromInt(_coefficientSH9[i*3+2]);
                }
                
                // /*
                // // for validate
                // float3 c[9] = {
                //     float3( 0.7953949,  0.4405923,  0.5459412 ),
                //     float3( 0.3981450,  0.3526911,  0.6097158 ),
                //     float3(-0.3424573, -0.1838151, -0.2715583 ),
                //     float3(-0.2944621, -0.0560606,  0.0095193 ),
                //     float3(-0.1123051, -0.0513088, -0.1232869 ),
                //     float3(-0.2645007, -0.2257996, -0.4785847 ),
                //     float3(-0.1569444, -0.0954703, -0.1485053 ),
                //     float3( 0.5646247,  0.2161586,  0.1402643 ),
                //     float3( 0.2137442, -0.0547578, -0.3061700 )
                // };*/

                // decode irradiance
                float3 irradiance = IrradianceSH9(c, dir);
                float3 Lo = irradiance / PI;
                
                //Lo = SAMPLE_TEXTURECUBE_LOD(_GlossyEnvironmentCubeMap, sampler_GlossyEnvironmentCubeMap, dir, 0).rgb;

                //return float4(Evaluate(dir.xzy, c), 1.0);
                
                return float4(Lo, 1.0);
            }
            ENDHLSL
        }
    }
}
