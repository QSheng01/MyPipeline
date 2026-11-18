#ifndef __ATMOSPHERE_HELPER__
#define __ATMOSPHERE_HELPER__

#ifndef PI
#define PI 3.14159265359
#endif

float3 UVToViewDir(float2 uv)
{
    float theta = (1.0 - uv.y) * PI;
    float phi = (uv.x * 2 - 1) * PI;
    
    float x = sin(theta) * cos(phi);
    float z = sin(theta) * sin(phi);
    float y = cos(theta);

    return float3(x, y, z);
}

float2 ViewDirToUV(float3 v)
{
    float2 uv = float2(atan2(v.z, v.x), asin(v.y));
    uv /= float2(2.0 * PI, PI);
    uv += float2(0.5, 0.5);

    return uv; 
}

float RayIntersectSphere(float3 center, float radius, float3 rayStart, float3 rayDir)
{
    float3 offset = rayStart - center;
    float distanceToCenter = length(offset);
    float b = dot(offset, rayDir);
    // Avoid subtracting two nearly equal squared radii at the planet surface.
    float c = (distanceToCenter - radius) * (distanceToCenter + radius);
    float discriminant = b * b - c;
    if (discriminant < 0.0) return -1.0;

    float root = sqrt(max(discriminant, 0.0));
    float nearHit = -b - root;
    float farHit = -b + root;
    // A ray leaving the surface has no forward ground intersection.
    if (farHit <= 0.0 && b >= 0.0) return -1.0;
    if (farHit < 0.0) return -1.0;
    return nearHit >= 0.0 ? nearHit : farHit;
}

void UvToTransmittanceLutParams(float bottomRadius, float topRadius, float2 uv, out float mu, out float r)
{
    float x_mu = uv.x;
    float x_r = uv.y;

    float H = sqrt(max(0.0f, topRadius * topRadius - bottomRadius * bottomRadius));
    float rho = H * x_r;
    r = sqrt(max(0.0f, rho * rho + bottomRadius * bottomRadius));

    float d_min = topRadius - r;
    float d_max = rho + H;
    float d = d_min + x_mu * (d_max - d_min);
    mu = d == 0.0f ? 1.0f : (H * H - rho * rho - d * d) / (2.0f * r * d);
    mu = clamp(mu, -1.0f, 1.0f);
}

float2 GetTransmittanceLutUv(float bottomRadius, float topRadius, float mu, float r)
{
    float H = sqrt(max(0.0f, topRadius * topRadius - bottomRadius * bottomRadius));
    float rho = sqrt(max(0.0f, r * r - bottomRadius * bottomRadius));

    float discriminant = r * r * (mu * mu - 1.0f) + topRadius * topRadius;
	float d = max(0.0f, (-r * mu + sqrt(discriminant)));

    float d_min = topRadius - r;
    float d_max = rho + H;

    float x_mu = (d - d_min) / (d_max - d_min);
    float x_r = rho / H;

    return float2(x_mu, x_r);
}

#endif
