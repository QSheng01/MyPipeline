# URP 体积云 — 技术问答存档

> 这份文档把移植/调试过程中一条条的技术分析问答整理归档,防止聊天记录被折叠后丢失。
> 按主题组织,每节 = 一个问题 + 分析。代码为工程内 `VolumetricClouds.compute` / 相关文件的片段(示意,省略部分边界处理)。
> 约定:**Reversed-Z**(0=far,1=near),`UNITY_RAW_FAR_CLIP_VALUE` 判天空;所有阈值比较尽量用 `LinearEyeDepth`。

---

## 目录

1. [第一个 Pass 为什么用 cloudCameraDepth(半分辨率深度)](#1)
2. [射线-球相交 RaySphereIntersection 逐行解析](#2)
3. [为什么用稳定版求根,而不是 (-b±√d)/2a](#3)
4. [insideClouds:判断相机在云层的上/中/下](#4)
5. [rayMarchRange:三种情形下的步进区间](#5)
6. [EvaluateCloudProperties:密度场怎么算出来的](#6)
7. [空步优化(empty-step)主循环](#7)
8. [EvaluateCloud:单步光照(消光/透过率/Powder/HG)](#8)
9. [PositivePow 与 PowderEffect 定义](#9)
10. [Checkerboard 分帧采样(原理 + 举例)](#10)
11. [ReprojectClouds:为什么需要这个 Pass,以及为什么会抖](#11)
12. [Depth Min/Max 与 PickRayIndexByDepth](#12)
13. [UpscaleAndCombine 与上采样(4-tap / tap 概念)](#13)

---

<a id="1"></a>
## 1. 第一个 Pass 为什么用 cloudCameraDepth(半分辨率深度)

**问**:为什么 RenderClouds Pass 里用的是 cloudCameraDepth,而不是全分辨率深度?实际传进来的是一张半分辨率灰度图。

**答**:因为 **Compute Shader 读不到 URP 全局绑定的 `_CameraDepthTexture`**。URP 把相机深度作为"全局纹理"绑定,普通渲染 shader 能通过 `SampleSceneDepth` 采样,但 compute kernel 拿不到这个全局绑定。

解决办法:先用一个**片元 Pass**(`VolumetricCloudsDepthDownsample.shader`)采样全局深度,把它降采样写进一张自己分配的半分辨率 RT(`_HalfResDepthBuffer`),再让 compute 去读这张自有 RT。

降采样用的是 **2×2 保守深度**:因为射线步进在 1/4 分辨率跑,一个低分辨率像素覆盖了原图 2×2 甚至更大区域,取其中"最远(reversed-Z 里是 min)"的深度作为保守值,避免云穿过前景物体。这就是"为什么是一张半分辨率灰度图"的由来——它是**降采样后的场景深度**,不是相机原始深度。

后来做 min/max ray 放置时,这张 RT 从单通道扩成了双通道:`.x = min(远)`,`.y = max(近)`。

---

<a id="2"></a>
## 2. 射线-球相交 RaySphereIntersection 逐行解析

```hlsl
int RaySphereIntersection(float3 startWS, float3 dir, float radius, out float2 result)
{
    float3 startPS = startWS + float3(0, _EarthRadius, 0); // 转到"以地心为原点"的坐标
    float a = dot(dir, dir);
    float b = 2.0 * dot(dir, startPS);
    float c = dot(startPS, startPS) - (radius * radius);
    float d = (b * b) - 4.0 * a * c;   // 判别式
    result = 0.0;
    int numSolutions = 0;
    if (d >= 0.0)
    {
        float sqrtD = sqrt(d);
        float q = -0.5 * (b + FastSign(b) * sqrtD);
        result = float2(c / q, q / a);
        numSolutions = 2;
        if (result.x < 0.0) { numSolutions--; result.x = result.y; } // 丢掉相机背后的交点
    }
    ...
}
```

**逐行**:
- 云层是套在地球上的球壳,所以先把射线起点平移到**以地心为原点**的坐标系 `startPS`(加上地球半径的 y 偏移)。
- 把射线 `P(t) = startPS + t·dir` 代入球面方程 `|P|² = radius²`,展开成关于 `t` 的二次方程 `a·t² + b·t + c = 0`,系数即 `a/b/c`。
- `d` 是判别式:`d < 0` 无交点(射线不碰球);`d ≥ 0` 有两个解。
- 用稳定版求根(见下一节)得到两个 `t`,存进 `result`。
- 最后把相机背后的交点(`t < 0`)剔除。

`√d` 的来历就是二次方程求根里的 `√(b²-4ac)`,并没有神秘之处——它就是判别式开根。

---

<a id="3"></a>
## 3. 为什么用稳定版求根,而不是 (-b±√d)/2a

**问**:课本明明是 `t = (-b ± √d) / 2a`,为什么这里写成 `q = -0.5·(b + sign(b)·√d)`,再 `result = (c/q, q/a)`?

**答**:为了避免**灾难性抵消(catastrophic cancellation)**。

当 `b` 的绝对值很大、且 `√d ≈ |b|` 时(地球半径 6378 km 这种量级非常常见),`-b + √d` 会变成"两个相近的大数相减",有效数字几乎全被吃掉,精度崩溃 → 云层边界肉眼可见地抖/裂。

稳定版的思路:**永远让相加的两项同号**,避免相减。
- `q = -0.5·(b + sign(b)·√d)`:因为 `sign(b)·√d` 和 `b` 同号,`b + sign(b)·√d` 是"同号相加",不会抵消,`q` 精度高。
- 另一个根用**韦达定理(Vieta)**反推:两根之积 `t1·t2 = c/a`,已知一根 `q/a`,另一根就是 `c/q`。

于是 `result = float2(c/q, q/a)` 就是两个高精度的根。这是数值计算里求二次方程根的标准稳健写法(《Numerical Recipes》里的经典技巧)。

---

<a id="4"></a>
## 4. insideClouds:判断相机在云层的上/中/下

```hlsl
ray.toEarthCenter = length(ray.originWS + float3(0, _EarthRadius, 0));
if (ray.toEarthCenter < _EarthRadius)
    ray.insideClouds = -2.0;                                   // 在地表以下(异常)
else if (ray.toEarthCenter <= (_LowestCloudAltitude + _EarthRadius))
    ray.insideClouds = -1.0;                                   // 在云层下方
else if (ray.toEarthCenter > (_HighestCloudAltitude + _EarthRadius))
    ray.insideClouds = 1.0;                                    // 在云层上方
else
    ray.insideClouds = 0.0;                                    // 正好在云层里
```

**作用**:算出相机(射线起点)到**地心**的距离 `toEarthCenter`,再和三个半径比较,判断相机相对云壳的位置:

- `-2`:在地表以下(基本是异常/保护)。
- `-1`:在云层**下方**(最常见,地面看天)。
- `0`:**就在云层内部**(飞进云里,局部云关键场景)。
- `+1`:在云层**上方**(高空/太空看下)。

这个分类决定了后面 `rayMarchRange` 怎么算步进区间——不同位置,射线进入/离开云壳的方式完全不同。

---

<a id="5"></a>
## 5. rayMarchRange:三种情形下的步进区间

```hlsl
// 云层下方
if (insideClouds < -0.5) {
    rayMarchRange.start = intersectionInter.x;                       // 从内壳交点开始
    rayMarchRange.distance = intersectionOuter.x - intersectionInter.x; // 到外壳
}
// 正好在云层内
else if (insideClouds == 0.0) {
    rayMarchRange.start = 0;                                          // 从相机自身开始
    if (intersectEarth)            rayMarchRange.distance = intersectionInter.x;      // 被地球挡住
    else if (numInterInner == 0)   rayMarchRange.distance = intersectionOuter.x;      // 不碰内壳,走到外壳
    else                           rayMarchRange.distance = intersectionInter.x;      // 走到内壳
}
// 云层上方
else {
    rayMarchRange.start = intersectionOuter.x;                       // 从外壳交点开始
    if (intersectEarth) rayMarchRange.distance = intersectionInter.x - intersectionOuter.x;
    else {
        if (numInterInner == 0) rayMarchRange.distance = intersectionOuter.y - intersectionOuter.x; // 穿过整层
        else                    rayMarchRange.distance = intersectionInter.x - intersectionOuter.x;
    }
}
rayMarchRange.distance = clamp(rayMarchRange.distance, 0.0, _MaxRayMarchingDistance);
return intersect && rayMarchRange.distance > 0.0;
```

其中:
- `intersectionInter` = 射线与**内壳**(最低云高)的交点,`intersectionOuter` = 与**外壳**(最高云高)的交点。
- `numInterInner` = 与内壳的交点数(0 表示射线擦过内壳上方,不进入内壳)。
- `intersectEarth` = 射线是否先撞到地球(撞到就提前截断)。

**三种情形直观理解**:
- **下方看天**:从内壳进,到外壳出,`[inter.x, outer.x]`。
- **云层内**:从相机自己(`start=0`)开始,终点取决于先碰到谁——地球 / 内壳 / 外壳。
- **上方看下**:从外壳进;若不碰地球也不碰内壳,就是**穿过整层**从外壳的近交点到远交点 `outer.y - outer.x`。

最后 `clamp` 到最大步进距离,并要求 `distance > 0` 才算真正需要步进。这段本质是**把"射线在云壳内的有效段"精确框出来**,避免在空区白走。

---

<a id="6"></a>
## 6. EvaluateCloudProperties:密度场怎么算出来的

这是射线每步都要调用的"这个点有多少云"函数。核心流程:

```hlsl
// 1) 不在云体内 / 在地表下 → 直接返回 0
if (!PointInsideCloudVolume(positionPS) || positionPS.y < 0.0f) return;

// 2) 归一化云内高度(0=底,1=顶)
properties.height = EvaluateNormalizedCloudHeight(positionPS);

// 3) 采样低频 3D 噪声(带风动画 + 高度扭曲)
float3 coord = AnimateBaseNoisePosition(positionPS).xzy / NORM * _ShapeScale - offset;
coord += properties.height * float3(_WindDirection.xy, 0) * _AltitudeDistortion;
float lowFreq = SAMPLE_TEXTURE3D_LOD(_Worley128RGBA, ..., coord, noiseMipOffset);

// 4) 采样 2D CloudMap(覆盖度 / 云类型 / 是否雨云)
GetCloudCoverageData(positionPS, cloudCoverageData);
if (coverage <= 阈值 || maxCloudHeight < height) return;

// 5) 用 (cloudType, height) 查 LUT,拿到密度/侵蚀/AO 的垂直廓形
float3 densityErosionAO = SAMPLE_TEXTURE2D_LOD(_CloudLutTexture, ..., float2(cloudType, height), ...);

// 6) 低频噪声 remap 出基础云
base_cloud = 1.0 - densityErosionAO.x * coverage.x * (1 - shapeFactor);
base_cloud = saturate(DensityRemap(lowFreq, base_cloud, 1, 0, 1)) * coverage.y;

// 7) 非廉价版:再用高频侵蚀噪声啃细节
if (!cheapVersion) {
    float highFreq = 1 - SAMPLE_TEXTURE3D_LOD(_ErosionNoise, ..., fineCoord, ...).x;
    highFreq = lerp(0, highFreq, erosionFactor * 0.75 * coverage.x * _ErosionFactorCompensation);
    base_cloud = DensityRemap(base_cloud, highFreq, 1, 0, 1);
}

// 8) 最终密度
properties.density = max(0, base_cloud) * _DensityMultiplier;
```

**关键点**:
- **三层叠加**:CloudMap(哪里有云)× LUT(垂直廓形)× 3D 噪声(形状/侵蚀细节)。
- `cheapVersion`:光照采样(向太阳步进)时用廉价版,跳过高频侵蚀,省性能。
- `DensityRemap` 是把噪声值按"下限/上限"重映射的工具函数,用来做"用噪声啃掉一部分密度"的效果。
- `sigmaT`(消光系数)按是否雨云在 `0.04~0.12` 之间插值——雨云更黑更实。
- AO(环境光遮蔽)也从 LUT 出,再按侵蚀/形状强度混合。

---

<a id="7"></a>
## 7. 空步优化(empty-step)主循环

```hlsl
if (activeSampling)
{
    EvaluateCloudProperties(currentPositionWS, 0, erosionMip, false, false, cloudProperties);
    cloudProperties.density *= densityAttenuationValue;

    if (cloudProperties.density > 阈值)
    {
        // 命中云:累加"平均距离"(给重投影用),做一次光照积分
        float txd = volumetricRay.transmittance * cloudProperties.density;
        volumetricRay.meanDistance += (rayMarchRange.start + currentDistance) * txd;
        meanDistanceDivider += txd;
        EvaluateCloud(cloudProperties, ..., stepS, currentDistance/totalDistance, volumetricRay);

        if (volumetricRay.transmittance < 0.003) { volumetricRay.transmittance = 0; break; } // 早退
        sequentialEmptySamples = 0;
    }
    else
        sequentialEmptySamples++;

    // 连续空采样够多 → 切换到"大步快进"模式
    if (sequentialEmptySamples == EMPTY_STEPS_BEFORE_LARGE_STEPS)
        activeSampling = false;

    float relStep = lerp(cloudRay.integrationNoise, 1.0, saturate(currentIndex));
    currentPositionWS += cloudRay.direction * stepS * relStep;
    currentDistance += stepS * relStep;
}
```

**这段是性能的核心优化——自适应步长**:

- **命中云**(密度 > 阈值):做精细步进 + 光照积分,并累加"透过率加权的平均距离"`meanDistance`(后面重投影/合成需要知道"云大概在多远")。
- **透过率早退**:`transmittance < 0.003`(几乎全黑)就 break,不再往后走。
- **连续空采样**:一旦连续 N 步都没云(`EMPTY_STEPS_BEFORE_LARGE_STEPS`),切到 `activeSampling=false` 的**大步模式**,快速跳过空气;再撞到云会切回精细模式。
- **步长抖动** `integrationNoise`:给步长加噪声抖动,把规则采样的"层带 artifact"打散,配合时间累积消掉。

一句话:**空的地方大步跑,有云的地方细步走,黑透了就早退。**

---

<a id="8"></a>
## 8. EvaluateCloud:单步光照(消光/透过率/Powder/HG)

```hlsl
void EvaluateCloud(...)
{
    const float extinction = cloudProperties.density * cloudProperties.sigmaT;
    const float transmittance = exp(-extinction * stepSize);   // Beer 定律

    float powder = PowderEffect(cloudProperties.density, envLighting.cosAngle, _PowderEffectIntensity);
    float3 sunColor = lerp(envLighting.sunColor0, envLighting.sunColor1, relativeRayDistance);
    float3 total = EvaluateSunLuminance(currentPositionWS, sunDir, sunColor, powder, phaseFunction);
    total += lerp(ambientBottom, ambientTop, cloudProperties.height) * cloudProperties.ambientOcclusion;

    const float3 integScatt = total - total * transmittance;   // 能量守恒的散射积分
    volumetricRay.inScattering += integScatt * volumetricRay.transmittance; // 乘当前累积透过率
    volumetricRay.transmittance *= transmittance;              // 更新累积透过率
}
```

**逐项**:
- **消光 extinction** = 密度 × sigmaT;单步**透过率** = `exp(-extinction·step)`(Beer-Lambert 定律)。
- **太阳光** `EvaluateSunLuminance`:向太阳方向做几步光照采样,算出这个点被太阳照亮多少,乘上 HG 相位函数(前向散射)和 Powder 效果。
- **环境光**:按云内高度在"底部环境色/顶部环境色"间插值,再乘 AO。
- **散射积分** `integScatt = total - total·transmittance`:这是**能量守恒的解析积分**形式——本步新增的散射量 = 入射光 × (1 - 单步透过率)。
- **累积**:新散射要乘上"到这一步为止的累积透过率"`volumetricRay.transmittance`(前面的云会挡住后面的散射),然后更新累积透过率。

这是标准的**前向 volumetric integration**:一边往前走,一边累加散射、累乘透过率。

---

<a id="9"></a>
## 9. PositivePow 与 PowderEffect 定义

**PositivePow**(URP/HDRP 通用工具):

```hlsl
real PositivePow(real base, real power)
{
    return pow(max(abs(base), FLT_EPS), power);
}
```

就是"安全版 pow":先 `abs` + 夹一个极小值,避免 `pow(负数)` 或 `pow(0)` 产生 NaN/Inf。

**PowderEffect**(粉状效果):

```hlsl
float PowderEffect(float cloudDensity, float cosAngle, float intensity)
{
    float powderEffect = 1.0 - exp(-cloudDensity * 2.0);
    powderEffect = saturate(powderEffect * 2.0);
    return lerp(1.0, lerp(1.0, powderEffect, smoothstep(0.5, -0.5, cosAngle)), intensity);
}
```

**作用**:模拟真实云在**背光薄边**变暗的现象。物理上,云朵朝太阳那一侧的薄边,因为多重散射还没积累起来,看起来反而更暗(不是更亮)——这就是 Nubis 提出的 "powder" 效果。

- `1 - exp(-density·2)`:密度越低(薄边),值越小 → 越暗。
- `smoothstep(0.5, -0.5, cosAngle)`:只在**背光**(视线朝向太阳,cosAngle 为负)时启用,顺光时不加。
- `intensity`:整体强度开关。

没有它,云的边缘会显得"发光/过曝",缺乏体积感。

---

<a id="10"></a>
## 10. Checkerboard 分帧采样(原理 + 举例)

**它是分帧的**。`RenderClouds` 在 1/4 分辨率跑,每帧只算 2×2 子像素块里的一个:

```hlsl
int cb = ComputeCheckerBoardIndex((int2)traceCoord, _SubPixelIndex);
intermediateCoord = traceCoord * 2 + HalfResolutionIndexToOffset(cb);
```

- `traceCoord`:1/4 分辨率下的坐标。
- `_SubPixelIndex`:每帧 0→1→2→3 轮换。
- `HalfResolutionIndexToOffset(cb)`:把索引映射成 2×2 里的偏移 (0,0)/(1,0)/(0,1)/(1,1)。
- 结果 `intermediateCoord`:写进 1/2 分辨率的目标位置。

**举例**(修正版):屏幕 100×100 → 1/4 降采样 trace = 25×25 → 每帧在 25×25 里各算一个子像素,填进 **50×50** 的 intermediate → **四帧填满 50×50**。

注意不是"四帧得到 100×100";四帧凑齐的是 **1/2 分辨率(50×50)**,最后再由 UpscaleAndCombine 拉到全分辨率。单帧真正 ray march 的像素数 = 25×25 = 最终全屏的 **1/16**。

**Checkerboard(棋盘)是什么**:就是把要采样的像素像棋盘格一样在时间上错开分布,每帧只采一部分格子,多帧拼成完整图。这样把一次昂贵计算摊到多帧,大幅降低单帧开销。

---

<a id="11"></a>
## 11. ReprojectClouds:为什么需要这个 Pass,以及为什么会抖

**问 1**:RenderClouds 都基本算完散射/透过率了,为什么不直接输出,还要 Reproject?

**答**:因为 RenderClouds **单帧只算了 1/16 的像素**(checkerboard),直接输出的话,一张图里只有四分之一的 intermediate 像素是"本帧新算的",其余四分之三是空的/旧的。Reproject 的职责就是:

1. 把**上一帧的完整结果**通过运动矢量重投影到当前帧;
2. 用当前帧新采的那 1/4 像素去**更新**对应位置;
3. 其余位置**沿用重投影后的历史**;
4. 多帧累积(最多 16),得到一张完整且时间上平滑的图。

没有它,画面要么是残缺的棋盘格,要么是没去噪的噪点图。

**问 2**:那我直接输出上一帧的散射/透过率不也差不多?

**答**:不行。相机一动,上一帧的像素位置就错了,直接用会**整体拖影/错位**。必须做运动矢量重投影 + 有效性校验,把"能用的历史"对齐过来,"不能用的历史"丢弃重采。

**问 3**:为什么画面会抖?

**答**:抖动来自"分帧 + 历史失效"。当:
- 相机快速运动 → 重投影后落在屏幕外 / 深度对不上 → 历史判无效 → 退回单帧新采样(此时只有 1/4 像素是新的)→ 短暂的欠采样 → 抖。
- 或邻域裁剪把历史 clamp 掉太多。

缓解手段就是这个 Pass 里的:运动矢量重投影、深度/屏幕边界校验、邻域 min/max 裁剪、自适应累积因子。关键 kernel 片段:

```hlsl
// 累积因子:样本越多越信任历史,但受校验因子压制
accumulationFactor = previousSampleCount >= 16.0 ? 0.941 : (prev / (prev + 1));
accumulationFactor *= _TemporalAccumulationFactor * validityFactor * _CloudHistoryInvalidation;
previousResult = accumulationFactor * previousResult + (1 - accumulationFactor) * traced;
```

**局部云**因为"到处都是云",所以**总是**做邻域裁剪;**远景云**只在背景像素裁剪。

---

<a id="12"></a>
## 12. Depth Min/Max 与 PickRayIndexByDepth

**问**:这套渲染里有 Depth Min/Max 吗?HDRP 有吗?

**答**:HDRP 的体积云模块**本身没有** min/max 深度那套(那是 RDR2 的做法)。我在移植版里参考 RDR2 **额外加**了:深度降采样输出 **min/max 双通道**(`.x=min/远`,`.y=max/近`),用来在深度边缘处更聪明地放置 checkerboard 的 ray。

**PickRayIndexByDepth**(coverage-based 版)干的事:

```hlsl
int PickRayIndexByDepth(int2 traceCoord, int cb, out bool wantNear)
{
    // 读 2×2 tile 的四个半分辨率深度纹素(各有 .x 远 / .y 近)
    // 算出整个 tile 的最远 tileFar 和最近 tileNear,以及持有它们的 sub-pixel 索引
    ...
    if (tileNear - tileFar < 1e-6) return cb;  // 深度平坦 → 保持棋盘节奏

    // 覆盖测试:假设 8 个邻居用基础棋盘放置,读它们的采样深度。
    // 只有当 tile 的近/远极值和"所有邻居"都差得够远(未被覆盖),才自适应补采。
    for (8 neighbors) {
        float nbLin = LinearEyeDepth(邻居采样深度, _CloudsZBufferParams);
        if (abs(curNearLin - nbLin) <= thr) nearUncovered = false;
        if (abs(curFarLin  - nbLin) <= thr) farUncovered  = false;
    }

    if (nearUncovered) { wantNear = true;  return nearIdx; } // 优先补近(轮廓/遮挡最要紧)
    if (farUncovered)  { wantNear = false; return farIdx;  }
    return cb;
}
```

**要点**:
- 同时用 `.x` 和 `.y` 两个通道,才能检测到"边缘穿过单个半分辨率纹素内部"的**亚纹素深度不连续**。
- `out bool wantNear` 告诉 RenderClouds:这帧的 ray 该在**近**表面(`.y`)还是**远**表面(`.x`)处裁剪。
- **覆盖测试**打破了循环依赖(假设邻居都用基础棋盘放置),保证 RenderClouds / ReprojectClouds 里结果一致、确定性。

**单通道省内存变体** `checkerboardMinMaxDepth`:深度 RT 用 R32,每个 cell 按奇偶只存 min 或 max,省一半带宽。**实测锯齿更大,一般关掉。**

---

<a id="13"></a>
## 13. UpscaleAndCombine 与上采样(4-tap / tap 概念)

**合成公式**(预乘散射 + 透过率):

```hlsl
float4 currentColor = _CameraColorCopy.Load(int3(fullCoord, 0));
float T = EvaluateFinalTransmittance(currentColor.rgb, currentClouds.w);
result = float4(currentColor.xyz * T + currentClouds.xyz, currentColor.a);
// 背景 * 透过率 + 云的内散射
```

- `currentClouds.w` = **透过率**(1=全透明,0=全遮挡),不是不透明度。
- `EvaluateFinalTransmittance` 按 `_ImprovedTransmittanceBlend` 在线性透过率和"亮度感知校正透过率"之间插值(HDRP 的 perceptual blending),让暗背景上的薄云更自然。

**tap 是什么**:tap = **一次贴图采样**,源自信号处理里的 "filter tap"(滤波器抽头)。一个 2×2 双线性 = **4-tap**(读周围 4 个纹素加权平均)。

**两种上采样都是 4-tap**,区别在采样中心是否抖动:

- **BilateralUpsample**(确定性):采样中心固定;远景云做前景/背景一致性加权,局部云纯双线性。
- **BlueNoiseUpsample**(抖动 4-tap):= Bilateral + **按云深度自适应核宽** + **动画抖动采样中心**,靠 TAA 把多帧抖动积分成时间超采样,变出额外细节。没有 TAA 时反而可能引入噪动。

**为什么这里是树叶闪烁的病灶**:整个上采样只能拿到**半分辨率**的深度和云,双线性权重随 TAA 抖动漂移 + 跨边缘 bleed → 高频叶缘闪。正解是新增的 `VolumetricCloudsComposite.shader`(片元全分辨率合成),用全分辨率 `_CameraDepthTexture` 引导上采样,把云对齐到几何边缘。它和 compute 版逻辑几乎一样,**唯一本质区别是深度来源**(全分辨率 vs 半分辨率),这一个差别就决定了边缘稳定性。

---

## 附:坐标 / 约定速查

- **Reversed-Z**:0 = far,1 = near;`UNITY_RAW_FAR_CLIP_VALUE` 判天空。
- 阈值比较用 `LinearEyeDepth(depth, _CloudsZBufferParams)`,避免 reversed-Z 非线性压缩影响。
- `_HalfResDepthBuffer`:默认 R32G32(`.x`=min/far,`.y`=max/near);`checkerboardMinMaxDepth` 开时为 R32 单通道按奇偶存单极值;compute 里声明为 `Texture2D<float4>` 以安全绑定两种格式。
- **trace : intermediate = 1 : 2**(checkerboard 依赖),final 固定全分辨率。
- Compute **读不到**全局 `_CameraDepthTexture` → 需要全分辨率深度的步骤都用片元 Pass。
