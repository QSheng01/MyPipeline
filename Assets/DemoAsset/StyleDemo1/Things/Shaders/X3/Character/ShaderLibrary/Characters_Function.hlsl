#ifndef _CHARACTERS_FUNCTION_HLSL_
#define _CHARACTERS_FUNCTION_HLSL_

half LinearStep(half minValue, half maxValue, half In)
{
    return saturate((In - minValue) / (maxValue - minValue));
}

float3 RotateAround(float3 target, float degree)
{
    float rad = degree * 0.01745f;
    float2x2 m_rotate = float2x2(cos(rad), -sin(rad),
                                 sin(rad), cos(rad));
    float2 dir_rotate = mul(m_rotate, target.xz);
    target = float3(dir_rotate.x, target.y, dir_rotate.y);
    return target;
}

inline half GetChannel(half4 textureValue, int ChannelId)
{
    const half4 CHANNEL[4] =
    {
        {1, 0, 0, 0},
        {0, 1, 0, 0},
        {0, 0, 1, 0},
        {0, 0, 0, 1}
    };

    half value = dot(CHANNEL[ChannelId], textureValue);

    return value;
}

#endif
