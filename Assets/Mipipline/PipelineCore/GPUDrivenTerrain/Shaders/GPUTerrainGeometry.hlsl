#ifndef MPIPELINE_GPU_TERRAIN_GEOMETRY
#define MPIPELINE_GPU_TERRAIN_GEOMETRY

StructuredBuffer<uint4> _GPUTerrainPatches;
Texture2D<float> _GPUTerrainHeight;
Texture2D<float2> _GPUTerrainNormal;
Texture2D<float> _GPUTerrainHoles;
SamplerState sampler_linear_clamp;
float4 _GPUTerrainOrigin, _GPUTerrainSize, _GPUTerrainInfo;
int _GPUTerrainDebug, _GPUTerrainStitch;
int _GPUTerrainUseBakedNormal;

void SetupGPUTerrain()
{
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
    unity_ObjectToWorld=float4x4(1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1);
    unity_WorldToObject=unity_ObjectToWorld;
#endif
}

float GPUTerrainHeight(float2 uv)
{
    float res=_GPUTerrainInfo.y;
    return _GPUTerrainHeight.SampleLevel(sampler_linear_clamp,(saturate(uv)*(res-1)+0.5)/res,0)*_GPUTerrainSize.y;
}

float3 GPUTerrainNormal(float2 uv)
{
    if (_GPUTerrainUseBakedNormal != 0)
    {
        float res = _GPUTerrainInfo.y;
        float2 coords = (saturate(uv) * (res - 1) + 0.5) / res;
        float2 xz = _GPUTerrainNormal.SampleLevel(sampler_linear_clamp, coords, 0).xy * 2 - 1;
        return normalize(float3(xz.x, sqrt(saturate(1 - dot(xz, xz))), xz.y));
    }
    float2 du=1.0/(_GPUTerrainInfo.y-1);
    float2 lo=max(uv-du,0),hi=min(uv+du,1);
    float dx=(GPUTerrainHeight(float2(hi.x,uv.y))-GPUTerrainHeight(float2(lo.x,uv.y)))/max((hi.x-lo.x)*_GPUTerrainSize.x,0.0001);
    float dz=(GPUTerrainHeight(float2(uv.x,hi.y))-GPUTerrainHeight(float2(uv.x,lo.y)))/max((hi.y-lo.y)*_GPUTerrainSize.z,0.0001);
    return normalize(float3(-dx,1,-dz));
}

float StitchHeight(float2 uv,bool alongZ,uint neighbourLOD)
{
    float step=exp2(neighbourLOD)/(_GPUTerrainInfo.x*_GPUTerrainInfo.w);
    float coord=alongZ?uv.y:uv.x;
    float low=floor(coord/step+0.00001)*step;
    float2 a=uv,b=uv;
    if(alongZ) {a.y=low;b.y=low+step;} else {a.x=low;b.x=low+step;}
    return lerp(GPUTerrainHeight(a),GPUTerrainHeight(b),saturate((coord-low)/step));
}

void ApplyGPUTerrain(inout float4 position,inout float3 normal,inout float2 uv,uint instanceID)
{
    uint4 patch=_GPUTerrainPatches[instanceID];
    float2 grid=position.xz;
    uv=(patch.xy+grid*exp2(patch.z)/_GPUTerrainInfo.w)/_GPUTerrainInfo.x;
    float height=GPUTerrainHeight(uv);
    if(_GPUTerrainStitch!=0)
    {
        uint4 delta=uint4(patch.w&255,(patch.w>>8)&255,(patch.w>>16)&255,patch.w>>24);
        // Put the fine edge exactly on the coarse edge's linear height segment.
        // Unlike local index snapping this also handles LOD deltas greater than 1.
        if(grid.x==0 && delta.x>0) height=StitchHeight(uv,true,patch.z+delta.x);
        else if(grid.x==_GPUTerrainInfo.w && delta.z>0) height=StitchHeight(uv,true,patch.z+delta.z);
        else if(grid.y==0 && delta.y>0) height=StitchHeight(uv,false,patch.z+delta.y);
        else if(grid.y==_GPUTerrainInfo.w && delta.w>0) height=StitchHeight(uv,false,patch.z+delta.w);
    }
    position=float4(_GPUTerrainOrigin.xyz+float3(uv.x*_GPUTerrainSize.x,height,uv.y*_GPUTerrainSize.z),1);
    normal=GPUTerrainNormal(uv);
}

void GPUTerrainClipHoles(float2 uv)
{
    int res=(int)_GPUTerrainInfo.y-1;
    clip(_GPUTerrainHoles.Load(int3(clamp((int2)(uv*res),0,res-1),0))-0.5);
}

float3 GPUTerrainDebugColor(float2 uv,uint lod)
{
    const float3 colors[8]={float3(1,.15,.08),float3(1,.6,.08),float3(.7,1,.1),float3(.1,1,.4),float3(.1,.7,1),float3(.3,.2,1),float3(.8,.15,1),float3(1,.3,.6)};
    float2 cell=uv*_GPUTerrainInfo.x/exp2(lod);
    float2 edge=min(frac(cell),1-frac(cell))/max(fwidth(cell),.00001);
    float border=saturate(min(edge.x,edge.y));
    return colors[min(lod,7)]*lerp(.16,1,border);
}
#endif
