Shader "CasualAtmosphere/AerialPerspectiveLut"
{
    Properties
    {

    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
            #include "Helper.hlsl"
            #include "Scattering.hlsl"
            #include "AtmosphereParameter.hlsl"
            #include "Raymarching.hlsl"

            struct appdata
            {
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = GetFullScreenTriangleVertexPosition(v.vertexID);
                o.uv = GetFullScreenTriangleTexCoord(v.vertexID);
                return o;
            }

            float _AerialPerspectiveDistance;
            float4 _AerialPerspectiveVoxelSize;

            SAMPLER(sampler_aerialLutLinearClamp);
            Texture2D _transmittanceLut;
            Texture2D _multiScatteringLut;
            float3 _AtmosphereCameraPosition;
            float3 _AtmosphereViewRayBottomLeft;
            float3 _AtmosphereViewRayBottomRight;
            float3 _AtmosphereViewRayTopLeft;
            float3 _AtmosphereViewRayTopRight;

            float4 frag (v2f i) : SV_Target
            {
                AtmosphereParameter param = GetAtmosphereParameter();

                float4 color = float4(0, 0, 0, 1);
                float3 uv = float3(i.uv, 0);
                uv.x *= _AerialPerspectiveVoxelSize.x * _AerialPerspectiveVoxelSize.z;  // X * Z
                uv.z = int(uv.x / _AerialPerspectiveVoxelSize.z) / _AerialPerspectiveVoxelSize.x;
                uv.x = fmod(uv.x, _AerialPerspectiveVoxelSize.z) / _AerialPerspectiveVoxelSize.x;
                uv.xyz += 0.5 / _AerialPerspectiveVoxelSize.xyz;

                float3 bottomRay = lerp(_AtmosphereViewRayBottomLeft, _AtmosphereViewRayBottomRight, uv.x);
                float3 topRay = lerp(_AtmosphereViewRayTopLeft, _AtmosphereViewRayTopRight, uv.x);
                float3 viewDir = normalize(lerp(bottomRay, topRay, uv.y));
                //return float4(viewDir, 1.0);

                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;
                
                float h = _AtmosphereCameraPosition.y - param.SeaLevel + param.PlanetRadius;
                float3 eyePos = float3(0, h, 0);

                float maxDis = uv.z * _AerialPerspectiveDistance;

                // inScattering
                color.rgb = GetSkyView(
                    param, eyePos, viewDir, lightDir, maxDis,
                    _transmittanceLut, _multiScatteringLut, sampler_aerialLutLinearClamp
                );

                // transmittance
                float3 voxelPos = eyePos + viewDir * maxDis;
                float3 t1 = TransmittanceToAtmosphere(param, eyePos, viewDir, _transmittanceLut, sampler_aerialLutLinearClamp);
                float3 t2 = TransmittanceToAtmosphere(param, voxelPos, viewDir, _transmittanceLut, sampler_aerialLutLinearClamp);
                float3 t = t1 / t2;
                color.a = dot(t, float3(1.0 / 3.0, 1.0 / 3.0, 1.0 / 3.0));

                return color;
            }
            ENDHLSL
        }
    }
}
