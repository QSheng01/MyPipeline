# 从 HDRP 到 URP:手把手移植体积云,以及云里那棵会闪的树

> 这篇文章记录了我把 Unity HDRP 的 Volumetric Clouds 移植到 URP 的过程,重点不在"抄一遍代码",而在于把整套**多分辨率 + 时间重投影**管线讲清楚,以及分享一个非常典型的坑——**把带透贴的树放进云里,叶子边缘会闪**——的完整排查与解决思路。
>
> 面向读者:有一定 URP / Shader 基础,想搞懂"工业级体积云到底怎么又快又稳"的同学。

---

## 一、为什么要自己移植

HDRP 自带了一套非常成熟的体积云(基于 Horizon Zero Dawn / Nubis 的思路),但它深度绑定 HDRP 的基础设施:云影 cookie、大气散射、环境探针 SH、自己的一套 RTHandle 与帧管理……直接搬到 URP 是不现实的。

我的目标是做一个**自包含的 URP Renderer Feature**,保留最核心的东西:

- 云建模:Perlin-Worley + 侵蚀噪声 + CloudMap + LUT
- 自适应射线步进 + 多重散射 + HG 相位 + Powder 效果
- **完整的多分辨率 + 时间重投影管线**(这是画质与性能的关键,也是本文重点)
- 支持**局部云**(相机能穿进云里,有正确的深度遮挡)

参数走 URP 的 Volume 系统(和 HDRP 体验一致),用 Compute Shader 做射线步进。

---

## 二、整体架构:三级分辨率 + 时间重投影

体积云最贵的是射线步进(ray marching):每个像素要沿视线走几十步,每步还要采样 3D 噪声、做光照。如果全分辨率逐帧硬算,手机/中端卡根本扛不住。

工业界的标准答案是:**降分辨率 + 分帧 + 时间累积**。整条管线长这样:

```
[DepthDownsample 片元 Pass]        把全分辨率深度降采样到半分辨率
  → RenderClouds     (1/4 分辨率, checkerboard 分帧)   ← 真正的射线步进,最贵
  → ReprojectClouds  (1/2 分辨率, 时间累积 / 邻域裁剪)  ← 把历史帧攒起来
  → UpscaleAndCombine(全分辨率, 上采样 + 合成)          ← 拉回全分辨率叠到画面上
```

三级分辨率的比例约定很重要:**trace : intermediate 恒为 1 : 2**(因为 checkerboard 用的是 2×2 分帧),final 固定全分辨率。

一个关键工程细节:**Compute Shader 读不到 URP 全局绑定的 `_CameraDepthTexture`**。所以第一步的深度降采样必须用一个**片元 Shader**(fragment)来做——只有普通渲染管线的 shader 能采样全局深度纹理。这个坑后面还会再遇到一次。

---

## 三、云建模:密度场怎么来的

射线步进每走一步,都要问一句:"这个世界坐标点,有多少云?"这就是 `EvaluateCloudProperties` 干的事。密度由三层噪声叠出来:

1. **CloudMap(2D)**:决定"哪里有云、云的类型、覆盖度",相当于俯视的天气图。
2. **LUT(曲线)**:按高度和云类型,给出密度/侵蚀/环境光遮蔽的垂直廓形(积云底平顶圆等)。
3. **3D 噪声**:低频 Worley-Perlin 做大形状,高频侵蚀噪声啃边缘细节。

云层本身是套在地球上的**球壳几何**(内外两个半径),所以射线要先和球壳求交,得到步进区间。这里有个很多人会写错的数值稳定版**射线-球相交**:

```hlsl
int RaySphereIntersection(float3 startWS, float3 dir, float radius, out float2 result)
{
    float3 startPS = startWS + float3(0, _EarthRadius, 0);
    float a = dot(dir, dir);
    float b = 2.0 * dot(dir, startPS);
    float c = dot(startPS, startPS) - radius * radius;
    float d = b * b - 4.0 * a * c;
    ...
    float sqrtD = sqrt(d);
    // 不直接用 (-b ± sqrtD)/2a,而是先算数值稳定的 q
    float q = -0.5 * (b + FastSign(b) * sqrtD);
    result = float2(c / q, q / a);
    ...
}
```

为什么不用课本里的 `(-b ± √d) / 2a`?因为当 `b` 很大、`√d ≈ |b|` 时,`-b + √d` 会出现**灾难性抵消**(两个相近的大数相减,有效位全丢)。用 `q = -0.5·(b + sign(b)·√d)` 保证相加的两项同号,再用韦达定理 `x1·x2 = c/a` 反推另一个根,精度稳得多。地球半径 6378 km 这种量级,不这么写会肉眼可见地抖。

---

## 四、Checkerboard 分帧采样:本文第一个重点

### 4.1 它是分帧的

`RenderClouds` 只在 **1/4 分辨率**上跑,而且**每帧只算 2×2 里的一个子像素**,四帧才凑齐 1/2 分辨率的完整结果:

```hlsl
int cb = ComputeCheckerBoardIndex((int2)traceCoord, _SubPixelIndex);
intermediateCoord = traceCoord * 2 + HalfResolutionIndexToOffset(cb);
```

`_SubPixelIndex` 每帧在 0~3 之间轮换,`HalfResolutionIndexToOffset` 把它映射成 2×2 里的一个偏移。所以:

- 屏幕 100×100 → 1/4 降采样是 25×25 的 trace
- 每帧在 25×25 里各算一个子像素,填进 50×50 的 intermediate
- 四帧填满 50×50

也就是说,**单帧真正 ray march 的像素数只有最终的 1/16**。这就是它快的根本原因。

### 4.2 那相机快速旋转为什么不抖?

这是最多人问的问题。既然是四帧才凑一张,快速转头时旧数据不就错位了吗?

答案是靠 `ReprojectClouds` 这一步的**时间重投影 + 校验 + 裁剪**:

1. **运动矢量重投影**:用相机矩阵把当前像素投影回上一帧的位置去取历史,相机转动被补偿掉。
2. **历史有效性校验**:超出屏幕、深度差太大 → 判定历史无效,直接退回当前帧新采样,不硬用错的旧值。
3. **邻域裁剪(neighborhood clip)**:把历史值 clamp 到当前帧邻域的 min/max 颜色盒里,防止"拖影/鬼影"。
4. **自适应累积因子**:样本越多越信任历史(最多 16 帧),但一旦校验失败就快速丢弃。

所以 checkerboard 分帧 + 激进的历史校验,合起来才既省又稳。**分帧负责省,重投影负责稳**,缺一不可。

---

## 五、上采样与合成:4-tap、双线性 vs 蓝噪声

最后一步 `UpscaleAndCombine` 把 1/2 分辨率的云拉回全分辨率,叠到相机颜色上。合成公式是标准的**预乘散射 + 透过率**:

```hlsl
float4 currentColor = _CameraColorCopy.Load(int3(fullCoord, 0));
float T = EvaluateFinalTransmittance(currentColor.rgb, currentClouds.w);
// 最终色 = 背景 * 透过率 + 云的内散射
result = float4(currentColor.xyz * T + currentClouds.xyz, currentColor.a);
```

注意 `currentClouds.w` 是**透过率**(1=全透明无云,0=完全遮挡),不是不透明度;`currentClouds.xyz` 是沿途累积好的内散射。

上采样有两种实现,**都是 2×2 = 4 tap**(tap = 一次贴图采样,来自信号处理里的 "filter tap" 抽头一词)。区别不在 tap 数量,而在**采样中心是否抖动**:

**`BilateralUpsample`(确定性)**:采样中心固定,每帧同一像素采同样 4 个 tap、同样权重。对远景云会做前景/背景一致性加权(避免天空与几何交界处出现光晕),对局部云就是纯双线性。

**`BlueNoiseUpsample`(抖动 4-tap)**:在双线性基础上多两件事——

```hlsl
// 1) 按云深度自适应核宽:近云更平滑,远云保细节
float kernel = lerp(1.0, 0.35, saturate(cloudDepth / _MaxCloudDistance));
// 2) 给采样中心加逐像素、逐帧的动画抖动,靠 TAA 积分出更多"虚拟采样"
float2 jitter = (Hash21(fullCoord, frameIndex) - 0.5) * kernel;
float2 halfUV = (fullCoord + 0.5) * upscaleRatio + jitter;
```

它同样是 4-tap,但每帧采样中心不同,**TAA 把多帧抖动积分起来,等效于时间超采样**,能在半分辨率源上"变出"额外细节。代价是没有 TAA 或 TAA 弱时可能引入轻微噪动。

---

## 六、一个真实的坑:云里那棵会闪的树

### 6.1 现象

把一棵**带透贴(alpha-test)叶片**的树放进局部云里,叶子边缘开始逐帧闪烁。我先怀疑是 alpha-test 老问题,把能试的都试了:TAA、Alpha-to-Coverage、贴图 mip……**全无效**。关掉云,闪烁消失——确认是**云侧**的锅。而且勾上"单通道 checkerboard 深度"优化后,闪得更凶。

### 6.2 根因

病灶在上采样:**半分辨率的遮挡缓冲被双线性拉伸到全分辨率**时,

- 双线性的插值权重会随 TAA 抖动**连续漂移**,同一个叶缘像素每帧混出的值都不一样;
- 半分辨率纹素跨越了叶片边缘,近处叶子和远处天空的云量被**混到一起**(bleed);

在高频的叶片轮廓上,这两点被放大成肉眼可见的闪烁。**这是"低分辨率遮挡叠加到高分辨率几何"的通病。**

### 6.3 方案一:全分辨率深度感知合成(HDRP 的做法)

正解是把最后的合成放到**全分辨率**,并用**真实的全分辨率场景深度**来引导上采样——让每个全分辨率像素优先采用"深度和自己接近"的那个云 tap,从而把云对齐到几何轮廓。

但前面说过:**Compute 读不到全局 `_CameraDepthTexture`**。所以这一步必须改成**片元 Pass**。核心逻辑:

```hlsl
float4 DepthAwareUpsample(float2 uv, float fullDepthRaw)
{
    float fullLin = LinearEyeDepth(fullDepthRaw, _CloudsZBufferParams);
    // 2×2 taps
    for (each of 4 taps)
    {
        float tapLin = LinearEyeDepth(tapSceneDepth, _CloudsZBufferParams);
        // 深度越接近,权重越高;留一个下限避免"没有匹配 tap"时变黑洞
        float rel = abs(fullLin - tapLin) / max(fullLin, 1.0);
        float depthW = exp(-rel * 8.0) + 0.05;
        float w = bilinearWeight * depthW;
        sum += cloudTap * w; wsum += w;
    }
    return sum / wsum;
}
```

**这里踩了个工程坑值得单独说**:第一版我用 `Blitter` + `#include Blit.hlsl` 来写这个全屏 Pass,结果编译报错:

```
undeclared identifier 'sampler_LinearClamp'   (Common.hlsl:385)
```

排查发现,这个工程的核心 `Blit.hlsl` 被人改过——把 `GlobalSamplers.hlsl` 的 include 注释掉了,而它又间接拉进了 URP 后处理的 `Common.hlsl`,里面的 `FXAAFetch` 用到了没声明的 `sampler_LinearClamp`。**结论:凡是 include 了 `Blit.hlsl` 的 shader 都会中招。**

修复方式:合成 shader **不走 `Blitter`、不 include `Blit.hlsl`**,改成和深度降采样 shader 一样的写法——自带 `Vert`(`GetFullScreenTriangleVertexPosition`),用 `CoreUtils.DrawFullScreen` 驱动,相机颜色通过显式绑定的贴图 `.Load` 读取。干净利落,不引入任何后处理 sampler 依赖。

### 6.4 方案二:分辨率档位

另一条路是直接堆分辨率。我加了一个 `ResolutionTier` 开关:

- **Quarter**(默认):trace 1/4 + intermediate 1/2,最省。
- **Half**:trace 1/2 + intermediate **全分辨率** → 上采样退化成 **1:1**,根本没有半分辨率拉伸,叶缘立刻就稳了。

为此要把 Compute 里 full→intermediate 的坐标映射从写死的 `>>1 / *0.5` 改成**按比例**,Half 档才能 1:1 精确命中:

```hlsl
float2 upscaleRatio = _IntermediateScreenSize.xy / _FinalScreenSize.xy; // ≈0.5 或 ≈1.0
float2 halfUV = (fullCoord + 0.5) * upscaleRatio;
```

Half 档画质确实稳,但**开销大**——大头是 RenderClouds 的 ray march 从 1/4 提到 1/2,约 4× 成本。

### 6.5 最终选择:Balanced

冷静想一下:**闪烁的病根是"半分辨率合成",跟 trace 分辨率其实没关系。** 所以最划算的组合是——

> **trace / intermediate 保持最省的 Quarter,但合成用全分辨率深度感知的片元 Pass。**

这正是 HDRP 的做法:开销≈Quarter(只多一个全屏 Pass,几乎不增加成本),而边缘稳定性接近 Half。我把它做成开关 `fullResolutionComposite`(默认开),可与任意分辨率档位组合。

**一句实话**:全分辨率合成对"天空↔几何边界"和跨边缘 bleed 是硬解;但对**亚半分辨率的树叶细节**只是"改善但受限"——因为云遮挡本身还是半分辨率追出来的。要彻底救树叶,得让每个 half-res tap 携带它代表的**真实近/远 clip 深度**(配合下面的 min/max ray 放置),这是更进一步的改造。

---

## 七、RDR2 风格的重建改进

参考 RDR2 的 SIGGRAPH 分享,我给重建管线加了三个可开关的改进(关掉就回退旧逻辑):

**F1 · 邻域深度剔除**:时间重投影做邻域裁剪时,按场景**线性深度差**剔除跨轮廓的样本,减少运动拖影。用 `LinearEyeDepth` 比较,避免 reversed-Z 的非线性压缩把阈值搞乱。

**F2 · min/max 深度 ray 放置**:深度降采样时输出 min/max **双通道**;用 2×2 tile 的 min/max 邻域分析来驱动 checkerboard 的 ray 放置——在深度边缘处**优先补采近/远样本**,而不是死守固定的分帧节奏。判据是"覆盖测试":看某个深度极值是否被 8 个邻居的基础采样覆盖,没覆盖才自适应补一个 ray。

**F3 · 蓝噪声上采样**:即上一节讲的抖动 4-tap。

还有个 F2 的变体 `checkerboardMinMaxDepth`:深度 RT 用单通道 R32(省一半内存/带宽),每个 cell 按奇偶只存 min 或 max。**实测:它会让锯齿更明显,一般建议关掉。**

---

## 八、经验总结 / 参数建议

1. **分帧负责省,重投影负责稳**——checkerboard 单独用会抖,必须配上激进的历史校验与邻域裁剪。
2. **数值稳定性别偷懒**——地球半径级别的射线-球相交,老老实实用稳定版求根公式。
3. **Compute 读不到全局深度纹理**——需要全分辨率深度的步骤(降采样、深度感知合成)都得用片元 Pass。
4. **低分辨率遮挡 + 高频几何 = 边缘闪烁**,通用解法是"全分辨率深度感知合成",而不是无脑堆分辨率。
5. **小心被改过的引擎源码**——本工程被注释掉的 `GlobalSamplers.hlsl` 让 `Blit.hlsl` 变成地雷,自己写全屏 Pass 反而更省心。

推荐的验证起点:

> `Resolution = Quarter`、`Full Resolution Composite = 开`、`checkerboardMinMaxDepth = 关`。

先看这套"Quarter 开销 + 全分辨率合成"够不够稳;不够再把 `Resolution` 提到 `Half`,或者去做"tap 携带近/远 clip 深度"的进阶改造。

---

## 九、结语

体积云真正的难点从来不是"怎么画一朵云",而是"怎么在预算内又快又稳地画满一整屏、还能穿进去、还不跟场景几何打架"。多分辨率 + checkerboard + 时间重投影这套组合拳,本质是拿**时间维度**换**空间采样**,再用一堆校验把时间带来的不稳定摁住。

移植过程里最有价值的部分,反而是那些"看起来是 alpha-test 问题、其实是合成分辨率问题"的坑——它们逼着你把整条管线的数据流真正理清楚。

如果这篇对你有帮助,欢迎点赞收藏。有问题也欢迎评论区交流。

---

*(本文基于个人在 URP 14.x 上的移植实践,代码片段为示意,已省略部分边界处理。)*
