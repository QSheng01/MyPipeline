using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AtmosphereRenderFeature : ScriptableRendererFeature
{
    public Material skyViewLutMaterial;
    public Material transmittanceLutMaterial;
    public Material multiScatteringLutMaterial;
    public Material aerialPerspectiveLutMaterial;
    public AtmosphereSettings atmosphereSettings;

    private CustomRenderPass m_Pass;

    private class CustomRenderPass : ScriptableRenderPass
    {
        private static readonly int SkyViewId = Shader.PropertyToID("_skyViewLut");
        private static readonly int TransmittanceId = Shader.PropertyToID("_transmittanceLut");
        private static readonly int MultiScatteringId = Shader.PropertyToID("_multiScatteringLut");
        private static readonly int AerialPerspectiveId = Shader.PropertyToID("_aerialPerspectiveLut");
        private static readonly int AtmosphereCameraPositionId = Shader.PropertyToID("_AtmosphereCameraPosition");
        private static readonly int ViewRayBottomLeftId = Shader.PropertyToID("_AtmosphereViewRayBottomLeft");
        private static readonly int ViewRayBottomRightId = Shader.PropertyToID("_AtmosphereViewRayBottomRight");
        private static readonly int ViewRayTopLeftId = Shader.PropertyToID("_AtmosphereViewRayTopLeft");
        private static readonly int ViewRayTopRightId = Shader.PropertyToID("_AtmosphereViewRayTopRight");

        public Material skyViewMaterial;
        public Material transmittanceMaterial;
        public Material multiScatteringMaterial;
        public Material aerialPerspectiveMaterial;
        public AtmosphereSettings settings;

        private RTHandle m_SkyView;
        private RTHandle m_Transmittance;
        private RTHandle m_MultiScattering;
        private RTHandle m_AerialPerspective;

        public CustomRenderPass()
        {
            const GraphicsFormat format = GraphicsFormat.R32G32B32A32_SFloat;
            m_SkyView = RTHandles.Alloc(256, 128, colorFormat: format, filterMode: FilterMode.Bilinear,
                wrapMode: TextureWrapMode.Clamp, name: "Atmosphere Sky View");
            m_Transmittance = RTHandles.Alloc(256, 64, colorFormat: format, filterMode: FilterMode.Bilinear,
                wrapMode: TextureWrapMode.Clamp, name: "Atmosphere Transmittance");
            m_MultiScattering = RTHandles.Alloc(32, 32, colorFormat: format, filterMode: FilterMode.Bilinear,
                wrapMode: TextureWrapMode.Clamp, name: "Atmosphere Multi Scattering");
            m_AerialPerspective = RTHandles.Alloc(32 * 32, 32, colorFormat: format, filterMode: FilterMode.Bilinear,
                wrapMode: TextureWrapMode.Clamp, name: "Atmosphere Aerial Perspective");
        }

        private static void DrawLut(CommandBuffer cmd, RTHandle target, Material material, int globalId)
        {
            CoreUtils.SetRenderTarget(cmd, target);
            cmd.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3);
            cmd.SetGlobalTexture(globalId, target.nameID);
        }

        private static Vector3 ViewRay(Camera camera, float x, float y)
        {
            var direction = camera.ViewportPointToRay(new Vector3(x, y, 0)).direction;
            // Interpolate vectors on one view plane before normalizing in the shader.
            return direction / Vector3.Dot(direction, camera.transform.forward);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (settings == null || skyViewMaterial == null || transmittanceMaterial == null ||
                multiScatteringMaterial == null || aerialPerspectiveMaterial == null)
                return;

            var cmd = CommandBufferPool.Get("Atmosphere LUTs");
            var s = settings;
            var camera = renderingData.cameraData.camera;
            cmd.SetGlobalVector(AtmosphereCameraPositionId, camera.transform.position);
            cmd.SetGlobalVector(ViewRayBottomLeftId, ViewRay(camera, 0, 0));
            cmd.SetGlobalVector(ViewRayBottomRightId, ViewRay(camera, 1, 0));
            cmd.SetGlobalVector(ViewRayTopLeftId, ViewRay(camera, 0, 1));
            cmd.SetGlobalVector(ViewRayTopRightId, ViewRay(camera, 1, 1));
            cmd.SetGlobalFloat("_SeaLevel", s.SeaLevel);
            cmd.SetGlobalFloat("_PlanetRadius", s.PlanetRadius);
            cmd.SetGlobalFloat("_AtmosphereHeight", s.AtmosphereHeight);
            cmd.SetGlobalFloat("_SunLightIntensity", s.SunLightIntensity);
            cmd.SetGlobalColor("_SunLightColor", s.SunLightColor);
            cmd.SetGlobalFloat("_SunDiskAngle", s.SunDiskAngle);
            cmd.SetGlobalFloat("_RayleighScatteringScale", s.RayleighScatteringScale);
            cmd.SetGlobalFloat("_RayleighScatteringScalarHeight", s.RayleighScatteringScalarHeight);
            cmd.SetGlobalFloat("_MieScatteringScale", s.MieScatteringScale);
            cmd.SetGlobalFloat("_MieAnisotropy", s.MieAnisotropy);
            cmd.SetGlobalFloat("_MieScatteringScalarHeight", s.MieScatteringScalarHeight);
            cmd.SetGlobalFloat("_OzoneAbsorptionScale", s.OzoneAbsorptionScale);
            cmd.SetGlobalFloat("_OzoneLevelCenterHeight", s.OzoneLevelCenterHeight);
            cmd.SetGlobalFloat("_OzoneLevelWidth", s.OzoneLevelWidth);
            cmd.SetGlobalFloat("_AerialPerspectiveDistance", s.AerialPerspectiveDistance);
            cmd.SetGlobalVector("_AerialPerspectiveVoxelSize", new Vector4(32, 32, 32, 0));

            DrawLut(cmd, m_Transmittance, transmittanceMaterial, TransmittanceId);
            DrawLut(cmd, m_MultiScattering, multiScatteringMaterial, MultiScatteringId);
            DrawLut(cmd, m_SkyView, skyViewMaterial, SkyViewId);
            DrawLut(cmd, m_AerialPerspective, aerialPerspectiveMaterial, AerialPerspectiveId);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Release()
        {
            m_SkyView?.Release();
            m_Transmittance?.Release();
            m_MultiScattering?.Release();
            m_AerialPerspective?.Release();
        }
    }

    public override void Create()
    {
        m_Pass?.Release();
        m_Pass = new CustomRenderPass { renderPassEvent = RenderPassEvent.BeforeRenderingSkybox };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType != CameraType.Game ||
            renderingData.cameraData.renderType != CameraRenderType.Base)
            return;

        m_Pass.skyViewMaterial = skyViewLutMaterial;
        m_Pass.transmittanceMaterial = transmittanceLutMaterial;
        m_Pass.multiScatteringMaterial = multiScatteringLutMaterial;
        m_Pass.aerialPerspectiveMaterial = aerialPerspectiveLutMaterial;
        m_Pass.settings = atmosphereSettings;
        renderer.EnqueuePass(m_Pass);
    }

    protected override void Dispose(bool disposing)
    {
        m_Pass?.Release();
        m_Pass = null;
    }
}
