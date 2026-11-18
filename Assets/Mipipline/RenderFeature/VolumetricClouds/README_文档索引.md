# URP 体积云 — 文档总索引

> 本目录(`Assets/RenderFeature/VolumetricClouds`)所有说明性 markdown 的导航中心。
> 模块 = HDRP Volumetric Clouds 的 URP 自包含移植:Compute 射线步进 + 多分辨率 + 时间重投影,支持局部云,参数走 URP Volume。

---

## 一、三份文档,分别看什么

| 文档 | 定位 | 什么时候看 |
| --- | --- | --- |
| [CONVERSATION_HANDOFF.md](./CONVERSATION_HANDOFF.md) | **给 AI/新人的交接文档** | 想快速了解"项目现状、代码结构、已做/未做、下一步候选" |
| [技术问答存档_URP体积云.md](./技术问答存档_URP体积云.md) | **逐条技术原理问答** | 想搞懂某个具体实现(射线求交/checkerboard/reproject/上采样…) |
| [知乎文章_URP体积云移植.md](./知乎文章_URP体积云移植.md) | **成文的科普/分享文章** | 想对外发布,或按叙事线整体理解一遍 |

一句话区分:**交接看现状,存档查原理,文章读故事。**

---

## 二、模块速览

### 管线(默认 Quarter 档)

```
[DepthDownsample 片元 Pass]        全分辨率深度 → 半分辨率(Compute 读不到全局深度)
  → RenderClouds     (1/4, checkerboard 分帧)   射线步进,最贵,单帧只算 1/16 像素
  → ReprojectClouds  (1/2, 时间累积/邻域裁剪)     把历史攒起来,去噪去残缺
  → UpscaleAndCombine(全分辨率, 上采样+合成)       拉回全屏叠到画面
```
约定:**trace : intermediate = 1 : 2**(checkerboard 依赖),final 固定全分辨率。

### 关键文件

| 文件 | 职责 |
| --- | --- |
| `VolumetricCloudsRenderFeature.cs` | Feature + `Settings`(质量开关 / 分辨率档位 / 资源引用) |
| `VolumetricCloudsRenderPass.cs` | 管线调度、RT 分配、参数上传、程序化烘焙 |
| `VolumetricCloudsVolume.cs` | URP VolumeComponent(所有 authoring 参数) |
| `Editor/VolumetricCloudsVolumeEditor.cs` | 条件隐藏参数 + Cloud Preset 切换 |
| `Shaders/VolumetricClouds.compute` | 主 compute(RenderClouds / ReprojectClouds / UpscaleAndCombine + 上采样) |
| `Shaders/VolumetricCloudsDepthDownsample.shader` | 片元降采样相机深度(min/max) |
| `Shaders/VolumetricCloudsComposite.shader` | 片元**全分辨率深度感知合成**(解决叶缘闪烁) |

---

## 三、按主题找内容(统一目录)

> 链接指向对应文档的章节锚点。

### 原理与实现(→ 技术问答存档)

- [第一个 Pass 为什么用半分辨率深度](./技术问答存档_URP体积云.md#1)
- [射线-球相交逐行解析](./技术问答存档_URP体积云.md#2) · [为什么用稳定版求根](./技术问答存档_URP体积云.md#3)
- [insideClouds 上/中/下判断](./技术问答存档_URP体积云.md#4) · [rayMarchRange 三种情形](./技术问答存档_URP体积云.md#5)
- [EvaluateCloudProperties 密度场](./技术问答存档_URP体积云.md#6) · [空步优化主循环](./技术问答存档_URP体积云.md#7)
- [EvaluateCloud 单步光照](./技术问答存档_URP体积云.md#8) · [PositivePow / PowderEffect](./技术问答存档_URP体积云.md#9)
- [Checkerboard 分帧采样](./技术问答存档_URP体积云.md#10)
- [ReprojectClouds 为什么需要 / 为什么抖](./技术问答存档_URP体积云.md#11)
- [Depth Min/Max 与 PickRayIndexByDepth](./技术问答存档_URP体积云.md#12)
- [UpscaleAndCombine 与上采样(4-tap / tap)](./技术问答存档_URP体积云.md#13)

### 工程现状与决策(→ 交接文档)

- [项目背景与已实现特性](./CONVERSATION_HANDOFF.md#1-项目背景)
- [RDR2 风格重建改进 F1/F2/F3](./CONVERSATION_HANDOFF.md#3-已实现的-rdr2-风格重建质量改进都有开关关闭回退旧逻辑)
- [树叶边缘闪烁:排查与解决全过程](./CONVERSATION_HANDOFF.md#4-本次对话的主线解决云中树叶边缘闪烁)
- [已知局限 / 下一步候选](./CONVERSATION_HANDOFF.md#6-已知局限--下一步候选尚未做)
- [遗留小问题](./CONVERSATION_HANDOFF.md#8-遗留的小问题历史分析中提到未修)

### 对外分享(→ 知乎文章)

- [从 HDRP 到 URP:手把手移植体积云,以及云里那棵会闪的树](./知乎文章_URP体积云移植.md)

---

## 四、速查(Cheat Sheet)

- **Reversed-Z**:0=far,1=near;`UNITY_RAW_FAR_CLIP_VALUE` 判天空。阈值比较用 `LinearEyeDepth(depth, _CloudsZBufferParams)`。
- **Compute 读不到全局 `_CameraDepthTexture`** → 需要全分辨率深度的步骤(降采样、深度感知合成)都用片元 Pass。
- `_HalfResDepthBuffer`:默认 R32G32(`.x`=min/far,`.y`=max/near);`checkerboardMinMaxDepth` 开时 R32 单通道按奇偶存单极值;compute 里声明 `Texture2D<float4>` 安全绑定两种格式。
- **tap** = 一次贴图采样;2×2 双线性 = 4-tap。
- **`.w` 是透过率**(1=全透明,0=全遮挡),不是不透明度。合成 = `背景*透过率 + 云内散射`。

### 推荐参数起点(平衡开销与稳定)

```
Resolution              = Quarter
Full Resolution Composite = 开     (HDRP 做法, 开销≈Quarter, 边缘接近 Half)
checkerboardMinMaxDepth = 关       (实测锯齿更大)
```
不够稳 → `Resolution` 提到 `Half`(合成开着无害);想更省 → 保持 Half 但降 `numPrimarySteps`。

### 已知坑

- 本工程核心 `Blit.hlsl` 被改过(注释掉 `GlobalSamplers.hlsl`)→ 任何 include 它的 shader 会报 `sampler_LinearClamp` 未声明。**自写全屏 Pass(CoreUtils.DrawFullScreen)绕开。**
- 全分辨率合成对**亚半分辨率树叶**只是部分改善;彻底解决需让 half-res tap 携带真实近/远 clip 深度(见交接文档下一步候选)。

---

## 五、文件清单

```
Assets/RenderFeature/VolumetricClouds/
├─ README_文档索引.md              ← 你在这里(总索引)
├─ CONVERSATION_HANDOFF.md         ← 交接文档(现状/结构/决策)
├─ 技术问答存档_URP体积云.md        ← 原理问答(13 节)
├─ 知乎文章_URP体积云移植.md        ← 对外文章
├─ VolumetricCloudsRenderFeature.cs
├─ VolumetricCloudsRenderPass.cs
├─ VolumetricCloudsVolume.cs
├─ Editor/VolumetricCloudsVolumeEditor.cs
└─ Shaders/
   ├─ VolumetricClouds.compute
   ├─ VolumetricCloudsDepthDownsample.shader
   └─ VolumetricCloudsComposite.shader
```
