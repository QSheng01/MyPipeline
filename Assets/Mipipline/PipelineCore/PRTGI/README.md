# PRTGI 场景迁移

源项目：`IllusionRP-master`，Unity 6000.6.0a3 / URP 17.6.0。目标项目：Unity 2022.3.62f2 / URP 14.0.12。

- `Scenes/PRTGI.unity` 保留场景层级、灯光、相机、探针体和调整体。原场景备份在 `OriginalScene~/PRTGI.unity`。
- `Scenes/PRTGI/` 保留材质、反射探针、LightingData 和 Lighting Settings。八个 Hybrid Lit 材质使用 `PipelineCore/PRTGI/Lit`，基于 URP 14 Lit 并接入 PRT SH 采样。
- `Materials/Skybox_Mat.mat` 已改用 Unity 内置的 `Skybox/Procedural`。
- `Runtime/` 保留原探针数据结构、分区与 Surfel 合并算法、资产类型和调整体的 GUID 与序列化字段。`PRTGpuRelighting` 默认在 compute shader 中连续更新砖块和探针 SH；`PRTRuntimeRelighting` 是 CPU 后备路径；`PRTGlobalIlluminationFeature` 发布 3D SH 纹理给场景材质。
- Unity 6 原始探针烘焙数据未迁入目标项目。场景使用同 GUID 的目标项目烘焙资产；当前资产为 `DXRPathTracingMaterial-URP14`，包含 4096 探针和 64 扇区。资产现在只存网格、探针元数据和扇区索引，传输数据分成 64 个独立文件。重烘焙前的索引与迁移前的完整资产保存在 `OriginalBakeBackup~/`，旧版扇区目录仍留在 `StreamingAssets`。`OriginalScene~/PRTGI/PRTGI Volume Profile.asset` 是原始后处理配置，当前同 GUID 的 URP 14 Profile 已加入 `PRTReflectionNormalization`。
- `OriginalSource~/` 保存 Illusion RP 1.4.4 完整包，包括 PRTGI Runtime、Editor、compute/raytracing shaders、Hybrid Lit 和 renderer feature；`OriginalSettings~/` 保存源管线设置。名称尾部的 `~` 使 Unity 不导入不兼容代码或资产。

## 在目标项目中烘焙

1. 打开 `Scenes/PRTGI.unity`，选中 **ProbeVolume**。Inspector 中调整探针数量、间距、采样数、扇区宽度和位置偏移。场景需要先保存，参与烘焙的 MeshRenderer / SkinnedMeshRenderer 需勾选 **Contribute GI**。
2. 可先点 **Preview Probe Placement** 或 **Preview Probe Placement (DXR)**，在 Scene 视图用绿色/红色球查看有效/无效探针。预览只保存在编辑器内存。DXR 选项仅在以 DX12 启动且 `SystemInfo.supportsRayTracing` 为 true 时可用。
3. 可选 **Generate Lighting (CPU Bake)** 或 **Generate Lighting (DXR Bake)**。CPU 工具在独立物理场景中为参与 GI 的网格建立临时 MeshCollider；DXR 工具构建 Unity 2022 的 RayTracingAccelerationStructure，用源模块的 26 方向、4 步探针放置规则和逐探针方向表在 GPU 捕获几何、法线与材质颜色。两种后端共享源模块的 Surfel 合并、砖块传输系数、天空可见性与扇区序列化，校验通过后才替换探针资产。进度窗口可取消；失败或取消时保留原资产。也可从 `Tools/PRTGI` 菜单操作。
4. **Bake Reflection Probes** 使用 Unity 内置反射探针烘焙，并对已添加 `PRTReflectionProbeAdditionalData` 的探针计算参考 SH。也可以在探针组件上或 `Tools/PRTGI/Bake Reflection Normalization` 单独烘焙参考 SH，完成后保存场景。示例 `PRTGI.unity` 没有 ReflectionProbe 对象；其他场景须先为目标探针添加此组件。**Clear Probe Transport** 清除探针传输数据。

目标编辑器经 `-force-d3d12` 启动后，RTX 3050 返回 `SystemInfo.supportsRayTracing=true`。DXR 烘焙要求参与 GI 的每个子网格材质提供与 PRT payload 兼容的 `PathTracing` pass；当前 `PipelineCore/PRTGI/Lit` 已实现，迁入场景的 57 个网格可用。该 pass 使用 URP 14 Lit 的材质求值，读取基础色、金属度、法线、视差和细节贴图；alpha test 的 any-hit 会忽略裁剪面，透明材质按 alpha 随机接受射线。与 Unity 6 原版的完整 PathTracing BSDF 和 payload 仍不同：PRT 捕获只存漫反射颜色与着色法线，不记录镜面、发光或带颜色的透射。CPU 后端继续兼容 DX11 与 DX12，使用可读 `_BaseMap`；不可读纹理只使用基础颜色。高采样数及较大场景可能耗时较长。

## 扇区磁盘流式加载

`PRTGI_ProbeVolume.asset` 是轻量索引（当前约 82 KiB）；压缩的扇区传输存于 `Assets/StreamingAssets/PRTGI/<资产 GUID>/<烘焙版本>/sector_####.prts`（当前 64 个文件共约 20.1 MiB）。索引记录每个扇区的坐标、边界、各类数据数量、文件长度与 SHA-256。加载时校验文件与索引；损坏或缺失会在 Inspector 的 Residency Pressure 中显示，不会将错误数据上传 GPU。烘焙工具自动导出新版本；现有未拆分资产可通过 `Tools/PRTGI/Convert Selected Asset to Streamed Sectors` 迁移。导出先逐个回读验证并备份旧资产，再替换索引。

运行时按相机请求扇区，后台读取并解码，限制同时最多四个磁盘任务。`CPU Sector Budget (MiB)` 限制已解码扇区和保存的阴影可见性缓存，保护相机所在扇区；`Resident Budget (MiB)` 独立限制 GPU 扇区缓冲区。`CPU Sector Cache` 显示已解码数据量估算和待加载文件数。扇区内存不包含固定探针 SH 缓冲、级联纹理、Unity 分配器开销和短暂的 I/O 压缩字节。当前文件总量的下降来自逐扇区 Deflate 压缩；2 km 地形仍需要后续按区域降低探针密度。

Windows 编辑器与独立程序从文件系统异步读取；`StreamingAssets` 为 URL 的平台使用 UnityWebRequest 后解码。直接查询调试 Surfel 的编辑器操作会同步读取一个扇区。旧烘焙版本在 `StreamingAssets` 中保留以便回滚；确认版本不再被索引引用后可清理旧目录。不要移动单个 `.prts` 文件或只复制主 `.asset`，部署时需连同其 `StreamingAssets/PRTGI` 目录一起保留。

## 运行时

迁入场景的 MainCamera 已指定 `URP-HighFidelity-Renderer`（Renderer Index 0），其 `PRTGlobalIlluminationFeature` 在不修改其他场景默认 Renderer 的情况下启用。默认通过 `PRTRelight.compute` 在 DX12 GPU 上每次渲染持续更新最多 `sectorsPerFrame` 个扇区：优先更新相机附近扇区，其余扇区轮流更新。光照变化、探针间接反馈会进入下一帧计算。探针系数沿用源项目的递归 3×3×3 tent filter 生成粗级 SH，按相机位置为每一级选取窗口，再把每级的 9 个 SH 系数写入**一张** `_coefficientVoxel3D`，尺寸为 `窗口 X × 窗口 Z × (窗口 Y × 9 × 实际级联数)`。材质按级联淡入权重采样，缺失细级数据时使用粗级，CPU 后备采用同一布局。当前场景网格为 `32×4×32`，窗口为 `16×4×16`，请求三级且实际启用三级，因此纹理是 `16×16×108`；窗口覆盖全域时会按源项目逻辑减少实际级联数。原有灯光直接照明照常由 URP 处理。关闭探针体的 `useGpuRelighting` 或在不支持的图形 API 上运行时使用 CPU 后备路径。没有有效烘焙数据或采样点离开探针域时，材质回退到 URP 自带 GI。

`ProbeVolume > Relight > Indirect Fill` 控制背光面的补光，默认 0.3。仅当 PRT 间接光低于材质原有的 URP GI 时，按此比例补足差值；0 表示纯 PRT，1 表示较亮的那一份。它不会改变烘焙数据，GPU 和 CPU 后备共用这一材质采样设置。数值过高会削弱遮挡与室内暗部的层次，建议在实际相机视角下调整。

在 `ProbeVolume > Relight` 中用 **Scene Objects Use PRTGI** 选择当前场景的 PRT Lit 对象是否采样 PRTGI。关闭后该场景相机停止探针 relight，`PipelineCore/PRTGI/Lit` 回退到 URP 原有 GI；其他 shader 继续使用各自的光照实现。开关按相机所属场景选择探针体，Scene View 则采用当前激活场景。多个场景由同一相机同时渲染时，shader 全局采样状态仍由该相机所属场景决定。

GPU relight 使用 URP 14 主光/附加灯阴影图计算 Surfel 可见性，并用光源 epoch 缓存在扇区中；扇区淘汰时读回缓存，再驻留时恢复。天空盒模式捕获 64² 六面环境辐照度，场景天空或主光变化时重建；Flat/Gradient 环境使用 Ambient Probe。有效的反射探针参考 SH 与当前探针辐照度比较，按 `PRTReflectionNormalization` 的上下限和权重修正专用 Lit 的间接镜面光。

扇区按 `Upload Budget (MiB)` 分帧上传，受 `Resident Budget (MiB)` 限制并按最近使用情况淘汰；固定的探针 SH、级联金字塔和相机纹理不计入扇区预算。每台相机持有独立的级联纹理与发布修订记录，长期不用的相机纹理会回收；窗口沿 X/Z 滚动时仅发布新增条带及受更新扇区影响的区域，粗级金字塔也只更新受选中扇区影响的节点。选中场景中的 **ProbeVolume**，Inspector 会显示本次渲染的扇区、上传量、常驻/淘汰数量、显存估算和发布节点数；GPU Profiler 中可以查找 `PRTGI GPU Relight`。Inspector 的毫秒数是 CPU 提交耗时，不是 GPU 执行耗时。

## 编辑器界面

`ProbeVolume` Inspector 按源项目的 Probe Grid、Probe Placement、Relight、Debug、Baking 分组。Debug 包含源项目的五种 Visualization 模式；Probe Radiance 显示 Selected Probe 子模式，Shadow Cache 显示读回间隔、标签、Surfel 大小与选中扇区快照。虚拟偏移模式只有一个 Preview Placement 按钮：DX12 光追可用时选择 DXR，否则使用 CPU；两种后端也可从 Tools/PRTGI 菜单明确选择。Statistics 和运行状态在 Debug 中。Generate Lighting 是一个主按钮与详细操作下拉菜单，CPU/DXR、反射归一化及清除数据可从菜单选择。Irradiance Sphere 使用源项目的球面 SH 调试 shader，从 GPU 已提交的探针系数实时着色；CPU 后备无 GPU 系数时显示灰色标记。Scene 视图的 Sphere Distribution 将烘焙命中位置投影到探针周围的单位球面；Sample Direction 画的是从实际探针捕获位置到 Surfel 命中位置的线，不包括射向天空的方向。为避免大量 Gizmo 阻塞编辑器，按整个命中列表均匀展示最多 1024 个，Inspector 会显示原始命中总数。Surfel 和阴影缓存使用相同的完整范围抽样。源项目的求解器残差视图仍需移植。`PRTProbeAdjustmentVolume` 按形状与模式显示参数；选中 `PRTProbeVolumeAsset` 可查看采样和内存明细。

Irradiance Sphere 的实例中心使用烘焙资产中的 `captureOffset`，与实际光追捕获位置一致。DXR 放置对处于薄地板内部、上下出口接近的探针优先选择上表面；对于导入网格的反向三角面，再从附近上方确认可通行的向上表面。当前场景重烘焙后底层 1024 个探针位于地板上方，先前版本的烘焙资产留在 `OriginalBakeBackup~/`。球面的下半部可能与地面几何相交，这是调试球体的显示尺寸，并不意味着探针中心仍在地板下方。

当前路径保留源模块的扇区近处优先、后台轮转、局部级联 SH、阴影可见性缓存、每相机窗口及预算式常驻。与 Unity 6 原版仍有差别：URP 14 阴影图访问和材质命中采用适配实现，阴影缓存的详细 GPU 统计与残差诊断尚未接入；反射归一化当前在前向 Lit 通道对间接镜面分量修正，不覆盖源版所有材质和屏幕空间效果。CPU 后备使用定向 Ambient Probe，但不采样 URP 阴影图或实际天空六面捕获。场景以外的材质需显式改用 `PipelineCore/PRTGI/Lit` 才能采样探针。

