# URP 体积云 — 对话交接文档（给 Codex 读）

> 这是一份把近期与 AI 的调试/开发对话整理出来的上下文交接文档，目的是让另一个 AI（Codex）能快速接手继续工作。
> 工程：`E:\UnityProduct\Baozang`，URP 14.0.9。模块目录：`Assets/RenderFeature/VolumetricClouds`。

---

## 1. 项目背景

把 HDRP 的 Volumetric Clouds 移植成 **URP 自包含 Render Feature**，放在 `Assets/RenderFeature/VolumetricClouds`。

核心特性（已实现）：
- Compute Shader 射线步进（Perlin-Worley + 侵蚀噪声 + CloudMap + LUT），云建模、太阳光/多重散射/powder/HG 相位、雾合成。
- **局部云**（相机可穿云、深度遮挡、世界空间锚定）。
- 完整**多分辨率 + 时间重投影**管线：checkerboard 分帧采样、16 帧累积、邻域裁剪。
- 参数走 **URP Volume 组件**（`VolumetricCloudsVolume`），并有自定义 Editor 按 cloudControl 模式隐藏参数、Cloud Preset 切换（对齐 HDRP）。
- 噪声/蓝噪声贴图可在 Inspector 配置，留空则运行期烘焙/哈希回退。

管线三级分辨率（默认 Quarter 档）：
```
[DepthDownsample fragment pass] 
  -> RenderClouds     (1/4 res, checkerboard 分帧)
  -> ReprojectClouds  (1/2 res, 时间累积/邻域裁剪)
  -> UpscaleAndCombine(full res, 上采样+合成)
```
约束：**trace : intermediate 恒为 1:2**（checkerboard 2x2 逻辑依赖），final 固定全分辨率。

---

## 2. 关键文件

- `VolumetricCloudsRenderFeature.cs` — ScriptableRendererFeature + `Settings`（含质量开关、分辨率档位、资源引用）。
- `VolumetricCloudsRenderPass.cs` — ScriptableRenderPass，管线调度、RT 分配、参数上传、程序化烘焙。
- `VolumetricCloudsVolume.cs` — URP VolumeComponent（所有 authoring 参数）。
- `Editor/VolumetricCloudsVolumeEditor.cs` — 条件隐藏参数 + Preset 切换。
- `Shaders/VolumetricClouds.compute` — 主 compute（RenderClouds / ReprojectClouds / UpscaleAndCombine + 上采样函数）。
- `Shaders/VolumetricCloudsDepthDownsample.shader` — fragment 降采样相机深度（compute 读不到全局 `_CameraDepthTexture`）。
- `Shaders/VolumetricCloudsComposite.shader` — **新增**，fragment 全分辨率深度感知合成（见下）。

---

## 3. 已实现的 RDR2 风格重建质量改进（都有开关，关闭回退旧逻辑）

在 `Settings`（Feature Inspector）里：
- **F1 `neighborhoodDepthRejection`**：时间重投影邻域裁剪时，按场景**线性深度差**剔除跨轮廓样本，减少运动拖影。阈值 `depthRejectionThreshold`。
- **F2 `minMaxDepthRayPlacement`**：深度降采样输出 min/max 双通道，用 2x2 tile 的 min/max 邻域分析驱动 checkerboard ray 放置，深度边缘优先补近/远样本。阈值 `rayPlacementDepthThreshold`。
  - `PickRayIndexByDepth` 采用 **coverage-based** 逻辑：读 `.x`(min/far) 与 `.y`(max/near) 两通道，算 tileFar/tileNear 及对应 sub-pixel index，再看 8 邻居的 base checkerboard 采样是否"覆盖"该极值，未覆盖才自适应放 ray；输出 `out bool wantNear` 供 RenderClouds 选近/远深度裁剪。
- **F2 变体 `checkerboardMinMaxDepth`**：单通道 R32 深度 RT（省一半内存/带宽），每 cell 按奇偶只存 min 或 max。**用户实测：勾上后锯齿更大，建议关闭。**
- **F3 `blueNoiseUpscale`**：全分辨率上采样用蓝噪声抖动 + 按透过率加权云深度决定核宽。

---

## 4. 本次对话的主线：解决"云中树叶边缘闪烁"

### 问题
带透贴（alpha-test）的树叶放进**局部云**里，叶片边缘逐帧闪烁。标准 alpha-test 修法（TAA / A2C / mips）都试过无效，确认是**云侧**问题；且勾 `checkerboardMinMaxDepth` 会更严重。

### 根因
半分辨率遮挡缓冲被**双线性拉伸**到全分辨率时，插值权重随 TAA 抖动连续漂移 + 跨边缘 bleed，在高频叶缘处放大成闪烁。

### 解决方案演进（重要，含反复）
1. 先实现"**全分辨率深度感知 fragment 合成**"（把第 4 步 compute upscale 换成能读全分辨率 `_CameraDepthTexture` 的 fragment pass，按深度相似度双边上采样，让云对齐几何边缘）。
   - 第一版用 `Blitter` + 引入核心 `Blit.hlsl`，**编译报错** `undeclared identifier 'sampler_LinearClamp'`。
   - **根因**：本工程的核心 `Blit.hlsl`（`Packages/com.unity.render-pipelines.core@14.0.9/ShaderLibrary/Blit.hlsl`）被改过——第 8 行 `GlobalSamplers.hlsl` 被注释掉，它又 include 了 URP 后处理 `Common.hlsl`，其 `FXAAFetch`(第 385 行) 用了未声明的 `sampler_LinearClamp`。
   - **修复**：合成 shader **不再 include `Blit.hlsl`/不走 `Blitter`**，改成和 `VolumetricCloudsDepthDownsample.shader` 一致的写法（自带 `Vert`/`GetFullScreenTriangleVertexPosition`，`CoreUtils.SetRenderTarget + CoreUtils.DrawFullScreen` 驱动，相机颜色通过显式 `_CameraColorCopy` 贴图 `.Load` 读取）。
2. 用户一度让"**回退这一版，走回 compute**"——已完整回退（删 shader、恢复 `m_UpscaleResult`/compute upscale）。
3. 之后加了 **分辨率档位开关 `ResolutionTier`**：
   - `Quarter`（默认）：trace 1/4 + intermediate 1/2。
   - `Half`：trace 1/2 + intermediate 全分辨率 → 上采样变 **1:1**，无半分辨率拉伸，叶缘明显更稳。
   - 为此把 compute 里 full→intermediate 的坐标映射从写死的 `>>1 / *0.5` 改成**按 `_IntermediateScreenSize/_FinalScreenSize` 比例**（`upscaleRatio`），使 Half 档 1:1 精确命中。
   - 用户反馈：**Half 画质可接受，但开销有点大**（大头是 RenderClouds ray march 从 1/4 提到 1/2，约 4×）。
4. **当前最终状态**：重新引入"全分辨率深度感知 fragment 合成"，但这次定位为**低开销替代 Half**，做成开关 `fullResolutionComposite`（默认开），可与任意分辨率档位组合：
   - `Quarter + fullResolutionComposite`（=Balanced，推荐）：开销≈Quarter，边缘稳定性介于 Quarter 与 Half 之间、通常接近 Half。这就是 HDRP 的合成做法。
   - 关闭则回退旧 compute 半分辨率上采样。
   - Pass 第 4 步按 `useFragmentComposite = fullResolutionComposite && m_CompositeMaterial != null` 分支；fragment 路径不分配 `m_UpscaleResult`；找不到 shader 自动回退 compute。

---

## 5. 当前代码状态（本次对话结束时）

- ✅ `VolumetricCloudsRenderFeature.cs`：有 `enum ResolutionTier { Quarter, Half }`、`resolutionTier`、`fullResolutionComposite`（默认 true），以及 F1/F2/F3 开关。
- ✅ `VolumetricCloudsRenderPass.cs`：按档位算 trace/intermediate 尺寸；第 4 步 fragment/compute 双路径；`m_CompositeMaterial` 懒创建 + Dispose 释放；`m_MaxCloudDistance` 字段缓存供合成用。
- ✅ `VolumetricClouds.compute`：`BilateralUpsample` / `BlueNoiseUpsample` / `UpscaleAndCombine` 的 full→intermediate 映射改为 `upscaleRatio` 比例式。
- ✅ `VolumetricCloudsComposite.shader`：CoreUtils 版（不含 Blit.hlsl），local 用 `exp(-rel*8)+0.05` 深度加权、distant 用前景/背景一致性加权、含雾 + `FinalTransmittance`。
- ✅ 所有 C# lint 通过。

---

## 6. 已知局限 / 下一步候选（尚未做）

- **全分辨率合成对局部云树叶只是"部分改善"**：因为 `history1.y` 存的是 `HalfResFarDepth`（far/min 场景深度），树叶（近）像素在所有 half-res tap 上都无匹配 → 回退近似双线性。天空↔几何边界、跨边缘 bleed 是硬解，但亚半分辨率叶片细节受限于"云遮挡本身是半分辨率追出来的"。
- **真正救树叶的候选方案（较大改动）**：让每个 half-res tap 携带它代表的**实际 clip 场景深度**（配合 min/max ray placement，近放置的 ray 存近深度），这样 fragment 双边上采样能给近像素匹配到近 tap → 正确的少云量 → 稳定。需要 RenderClouds 输出每条 ray 的 clip scene depth 并让 Reproject 存进 `history1.y`（目前存 far）。
- 省 Half 开销的另一条路：保持 Half 分辨率但降 `numPrimarySteps` 等，画质换性能。

---

## 7. 推荐验证组合

先设：`Resolution = Quarter`、`Full Resolution Composite = 开`、`checkerboardMinMaxDepth = 关`。
观察树叶边缘是否够稳：
- 够 → 省下了 Half 的 4× ray march 开销。
- 差一点 → `Resolution` 提到 `Half`（合成开着无害），或走第 6 节的"tap 携带近/远 clip 深度"方案。

---

## 8. 遗留的小问题（历史分析中提到，未修）

- `VolumetricCloudsRenderPass.cs` 顶部有 `using static UnityEditor.IMGUI.Controls.PrimitiveBoundsHandle;`——疑似误引入，打包（非 Editor）可能报错，建议清理。
- 程序化 `BuildCloudMap` 曾被简化为常量 1x1，导致 `cloudCoverage` 滑条对程序化贴图无效；`BuildCloudLut` 用 1 像素宽，`cloudType` 维度对 LUT 采样无效。（用户若提供贴图则不受影响。）
- compute 里 `UpscaleAndCombine` kernel 在 fragment 合成路径下不被 dispatch（保留无害）。

---

### 关键坐标/约定备注
- Reversed-Z：0=far，1=near；`UNITY_RAW_FAR_CLIP_VALUE` 判天空。阈值比较用 `LinearEyeDepth(depth, _CloudsZBufferParams)`。
- `_CloudsZBufferParams` 由 `GetZBufferParams(camera)` 生成（处理 reversed-Z）。
- 半分辨率深度 RT `_HalfResDepthBuffer`：默认 R32G32（.x=min/far，.y=max/near）；`checkerboardMinMaxDepth` 开时为 R32 单通道按奇偶存单极值。compute 里声明为 `Texture2D<float4>` 以安全绑定两种格式。
