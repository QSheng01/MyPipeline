#include "Assets/DemoAsset/StyleDemo1/Things/Shaders/X3/Scene/Grass/Libraries/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

void SimpleWind(float3 vertexColor, float3 pivotWS, inout float3 positionOS)
{
    float cycle = frac(pivotWS.x + pivotWS.z);
    float3 positionWS = TransformObjectToWorld(positionOS);
    float3 globalAmbientWind = float3(0,0,0.6);
    float windStrength = sin(_Time.y) * length(globalAmbientWind);
    float3 axis = cross(globalAmbientWind,float3(0,-1,0));
    float angle = sin(_Time.y * cycle) * pow(vertexColor.r,3) * windStrength;
    float3 posOffsetX = RotateAroundAxis(pivotWS,positionWS, axis, 0.017453293 * angle);//PI / 180 = 0.017453293
    float angleY = cos(_Time.y * cycle) * pow(vertexColor.g,2) * windStrength * 2.5;
    float3 posOffsetY = RotateAroundAxis(pivotWS,posOffsetX, globalAmbientWind, 0.017453293 * angleY);
    
    positionWS = posOffsetY;
    positionOS = TransformWorldToObject(positionWS);
}

void GrassWind(float _WindGustStrength, float3 vertexColor, float2 pivotOS, inout float3 positionOS)
{
    float3 pivotWS = TransformObjectToWorld(float3(pivotOS.x,0,pivotOS.y));
    float3 positionWS = TransformObjectToWorld(positionOS);
    float cycle = frac(pivotWS.x + pivotWS.z);
    float3 windDir = float3(-1,0,0); 
    // float windStrength = (sin(_Time.y * cycle * 3) * 0.5 + 0.6) * 1.5;
    float windStrength = _WindGustStrength;

    //跟据风向先绕Y轴旋转
    // float2 temp = float2(positionOS.x - pivotOS.x,positionOS.z - pivotOS.y);
    // float theta = acos(dot(temp, globalAmbientWind)/length(temp) * length(globalAmbientWind)) * 57.29578f;
    // float angleY = lerp(0,theta,clamp(0,1,windStrength));
    // float3 posOffsetX = RotateAroundAxis(pivotWS,positionWS, float3(0,1,0), 0.017453293 * angleY);
    
    float3 axis = cross(windDir,float3(0,-1,0));
    float angle = (sin(_Time.y * cycle * 2) * 0.5 + 0.6) * windStrength * 5 *  pow(positionOS.y + 2,2);
    float3 posOffsetY = RotateAroundAxis(pivotWS,positionWS, axis, 0.017453293 * angle);//PI / 180 = 0.017453293

    float angleW = cos(_Time.y * cycle) * pow(positionOS.y,2) * windStrength * 10;
    float3 posOffsetW = RotateAroundAxis(pivotWS,posOffsetY, windDir, 0.017453293 * angleW);
    
    positionOS = TransformWorldToObject(posOffsetW);
}