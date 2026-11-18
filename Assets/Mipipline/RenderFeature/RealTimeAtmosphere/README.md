# Real Time Atmosphere (URP 14)

从 `RealTimeAtmosphere-main` 迁入的大气 LUT、天空和空气透视模块。目标工程使用 Unity 2022.3 / URP 14，因此两个 Renderer Feature 已从源工程的 RenderGraph 实现改为 `ScriptableRenderPass.Execute`。其余 Shader 和参数保持源工程逻辑。

## 接入

1. 在实际使用的 **Universal Renderer Data** 中依次添加 `AtmosphereRenderFeature` 和 `AerialPerspectiveRenderFeature`。
2. 给 `AtmosphereRenderFeature` 指定 `Materials` 目录的 `SkyViewLut_Mat`、`TransmittanceLut_Mat`、`MultiScatteringLut_Mat`、`AerialPerspectiveLut_Mat`，以及 `Settings/Atmosphere.asset`。
3. 给 `AerialPerspectiveRenderFeature` 的 `m_AerialPerspectiveMaterial` 指定 `Materials/AerialPerspective_Mat.mat`。
4. 如果需要显示大气天空，在 Lighting > Environment 中将 Skybox Material 指定为源工程的 `Materials/Skybox_Mat.mat`，并在场景中配置主方向光。

`AtmosphereRenderFeature` 在 Skybox 绘制前由当前 Game 视图的 Base 相机生成透射、多重散射、天空视图和空气透视 LUT。相机位置与四角视线由正在渲染的 Game 相机传入；Scene 视图和预览相机不会重算并覆盖 LUT。`AerialPerspectiveRenderFeature` 在后处理前读取这些 LUT 与相机深度，合成空气透视。请在同一个 Renderer Data 中同时添加两个 Feature。空气透视需要深度纹理；Feature 会向 URP 请求 `Depth` 输入。

散射积分和天空着色保留源工程实现。球面求交与地表命中距离为零时的截断做了数值边界修正，避免相机位于海平面时向星球内部积分而在 Sky View LUT 中生成 NaN 条纹。源场景相机位于地面上方约 484 米，并使用大尺寸场景网格覆盖地平线以下；Shader 本身不渲染地面。

所有迁入资源保留了源工程 `.meta` 的 GUID；目录使用新 GUID。源工程的示例场景、Renderer Data 和调试资源未迁入，避免覆盖目标工程的渲染配置。

源工程使用 MIT License，许可证文本见同目录的 `LICENSE.md`。
