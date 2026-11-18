using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Illusion.Rendering.PRTGI
{
    public sealed class PRTGlobalIlluminationFeature : ScriptableRendererFeature
    {
        static PRTGlobalIlluminationFeature()
        {
            RenderPipelineManager.beginCameraRendering += (_, __) =>
            {
                Shader.SetGlobalFloat("_PRTEnabled", 0f);
                Shader.SetGlobalInt("_PRTReflectionCount", 0);
            };
        }

        public static bool SuppressRelightForCapture { get; set; }
        public int LastVisibleReflectionProbes { get; private set; }
        public bool LastNormalizationEnabled { get; private set; }
        [SerializeField] private ComputeShader relightCompute;
        private PublicationPass _pass;

        public override void Create()
        {
            _pass?.Dispose();
            _pass = new PublicationPass(this);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_pass == null || SuppressRelightForCapture) return;
            CameraType type = renderingData.cameraData.cameraType;
            if (type == CameraType.Preview || type == CameraType.Reflection) return;
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            _pass?.Dispose();
            _pass = null;
        }

        private sealed class PublicationPass : ScriptableRenderPass
        {
            private static readonly int Coefficients = Shader.PropertyToID("_coefficientVoxel3D");
            private static readonly int Enabled = Shader.PropertyToID("_PRTEnabled");
            private static readonly int Origin = Shader.PropertyToID("_PRTGridOrigin");
            private static readonly int Count = Shader.PropertyToID("_PRTGridCount");
            private static readonly int Spacing = Shader.PropertyToID("_PRTGridSpacing");
            private static readonly int IndirectFill = Shader.PropertyToID("_PRTIndirectFill");
            private static readonly int Slots = Shader.PropertyToID("_PRTSlotCount");
            private static readonly int Cascades = Shader.PropertyToID("_PRTCascadeCount");
            private static readonly int Center = Shader.PropertyToID("_PRTCascadeCenter");
            private static readonly int Minimum = Shader.PropertyToID("_PRTCascadeMin");
            private static readonly int WindowCount = Shader.PropertyToID("_PRTCascadeWindowCount");
            private static readonly int LevelCount = Shader.PropertyToID("_PRTCascadeLevelCount");
            private static readonly int ReflectionCount = Shader.PropertyToID("_PRTReflectionCount");
            private static readonly int ReflectionParams = Shader.PropertyToID("_PRTReflectionParams");
            private static readonly int ReflectionL0L1 = Shader.PropertyToID("_PRTReflectionL0L1");
            private static readonly int ReflectionL2 = Shader.PropertyToID("_PRTReflectionL2");
            private static readonly int ReflectionL2Last = Shader.PropertyToID("_PRTReflectionL2Last");
            private static readonly int ReflectionBoxMin = Shader.PropertyToID("_PRTReflectionBoxMin");
            private static readonly int ReflectionBoxMax = Shader.PropertyToID("_PRTReflectionBoxMax");
            private readonly Vector4[] _l01 = new Vector4[8], _l2 = new Vector4[8],
                _l2Last = new Vector4[8], _boxMin = new Vector4[8], _boxMax = new Vector4[8];
            private PRTProbeVolume _volume;
            private readonly PRTGlobalIlluminationFeature _owner;
            private IPRTRelightRuntime _runtime;
            private PRTProbeVolumeAsset _asset;
            private int _assetVersion;
            private Vector3Int _requestedSlots;
            private int _requestedCascades;
            private bool _gpuUnavailable;
            private float _lastGpuRetry;

            public PublicationPass(PRTGlobalIlluminationFeature owner)
            {
                _owner = owner;
                renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
                profilingSampler = new ProfilingSampler("PRTGI Relight and Publish");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                Camera camera = renderingData.cameraData.camera;
                Scene cameraScene = camera.gameObject.scene;
                // Scene View cameras do not belong to the edited scene. Game cameras only use
                // the volume in their own scene, so additive scenes cannot inherit PRTGI.
                PRTProbeVolume volume = camera.cameraType == CameraType.SceneView
                    ? FindVolume(SceneManager.GetActiveScene()) : FindVolume(cameraScene);
                bool newVolume = volume != _volume;
                if (newVolume) _gpuUnavailable = false;
                if (_gpuUnavailable && _owner.relightCompute && Time.realtimeSinceStartup - _lastGpuRetry > 3f &&
                    _owner.relightCompute.IsSupported(_owner.relightCompute.FindKernel("CSBricks")))
                    _gpuUnavailable = false;
                bool wantGpu = volume && volume.useGpuRelighting && _owner.relightCompute &&
                    SystemInfo.supportsComputeShaders &&
                    SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12 && !_gpuUnavailable;
                if (newVolume || volume &&
                    (volume.asset != _asset || volume.asset.ContentVersion != _assetVersion ||
                     !volume.BakeGrid.Equals(volume.asset.Grid) ||
                     volume.voxelProbeSize != _requestedSlots || volume.cascadeCount != _requestedCascades ||
                     _runtime != null && (_runtime.Backend == "GPU Compute") != wantGpu))
                {
                    _volume?.StopRelight();
                    _runtime?.Dispose();
                    _volume = volume;
                    _asset = volume ? volume.asset : null;
                    _assetVersion = _asset ? _asset.ContentVersion : 0;
                    _requestedSlots = volume ? volume.voxelProbeSize : default;
                    _requestedCascades = volume ? volume.cascadeCount : 0;
                    _runtime = null;
                }
                var cmd = CommandBufferPool.Get("PRTGI Relight and Publish");
                if (volume && volume.isActiveAndEnabled && volume.asset && volume.asset.HasValidData &&
                    volume.BakeGrid.Equals(volume.asset.Grid))
                {
                    if (_runtime == null && volume.asset.TryValidate(out string error))
                    {
                        if (wantGpu)
                            try { _runtime = new PRTGpuRelighting(volume, _owner.relightCompute); }
                            catch (System.Exception exception)
                            {
                                _gpuUnavailable = true;
                                _lastGpuRetry = Time.realtimeSinceStartup;
                                Debug.LogWarning("PRTGI GPU relight initialization failed, using CPU: " + exception, volume);
                            }
                        if (_runtime == null) _runtime = new PRTRuntimeRelighting(volume);
                    }
                    if (_runtime != null)
                    {
                        long start = System.Diagnostics.Stopwatch.GetTimestamp();
                        _runtime.Tick(renderingData.cameraData.camera, cmd, ref renderingData);
                        float elapsed = (float)((System.Diagnostics.Stopwatch.GetTimestamp() - start) *
                            1000.0 / System.Diagnostics.Stopwatch.Frequency);
                        volume.ReportRelight(_runtime, elapsed);
                        if (_runtime is PRTGpuRelighting gpu)
                            volume.SetRadianceDebugBuffers(gpu.DebugSH, gpu.DebugMetadata, gpu.DebugOffsets);
                        else volume.SetRadianceDebugBuffers(null, null, null);
                        var grid = volume.asset.Grid;
                        var layout = _runtime.Layout;
                        cmd.SetGlobalFloat(Enabled, 1f);
                        cmd.SetGlobalVector(Origin, grid.origin);
                        cmd.SetGlobalVector(Count, new Vector4(grid.count.x, grid.count.y, grid.count.z, 0f));
                        cmd.SetGlobalFloat(Spacing, grid.spacing);
                        cmd.SetGlobalFloat(IndirectFill, Mathf.Clamp01(volume.indirectFill));
                        cmd.SetGlobalVector(Slots, new Vector4(layout.Slots.x, layout.Slots.y, layout.Slots.z, 0f));
                        cmd.SetGlobalInt(Cascades, layout.CascadeCount);
                        cmd.SetGlobalVector(Center, layout.Center);
                        cmd.SetGlobalVectorArray(Minimum, layout.MinimumVectors);
                        cmd.SetGlobalVectorArray(WindowCount, layout.WindowVectors);
                        cmd.SetGlobalVectorArray(LevelCount, layout.LevelVectors);
                        cmd.SetGlobalTexture(Coefficients, _runtime.Coefficients);
                        PublishReflectionNormalization(cmd, ref renderingData);
                    }
                    else { volume.StopRelight(); cmd.SetGlobalFloat(Enabled, 0f); cmd.SetGlobalInt(ReflectionCount, 0); }
                }
                else { volume?.StopRelight(); cmd.SetGlobalFloat(Enabled, 0f); cmd.SetGlobalInt(ReflectionCount, 0); }
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            private void PublishReflectionNormalization(CommandBuffer cmd, ref RenderingData renderingData)
            {
                var settings = VolumeManager.instance.stack.GetComponent<PRTReflectionNormalization>();
                var visible = renderingData.cullResults.visibleReflectionProbes;
                _owner.LastVisibleReflectionProbes = visible.Length;
                _owner.LastNormalizationEnabled = settings != null && settings.IsActive();
                if (settings == null || !settings.IsActive())
                {
                    cmd.SetGlobalInt(ReflectionCount, 0);
                    return;
                }
                int count = 0;
                for (int i = 0; i < visible.Length && count < _l01.Length; i++)
                {
                    ReflectionProbe probe = visible[i].reflectionProbe;
                    if (!probe || !probe.TryGetComponent(out PRTReflectionProbeAdditionalData additional) ||
                        !additional.TryGetLuminanceSH(out Vector4 l01, out Vector4 l2, out float l2Last)) continue;
                    Bounds bounds = probe.bounds;
                    _l01[count] = l01;
                    _l2[count] = l2;
                    _l2Last[count] = new Vector4(l2Last, 0, 0, 0);
                    _boxMin[count] = new Vector4(bounds.min.x, bounds.min.y, bounds.min.z, probe.blendDistance);
                    _boxMax[count] = new Vector4(bounds.max.x, bounds.max.y, bounds.max.z, 0);
                    count++;
                }
                cmd.SetGlobalInt(ReflectionCount, count);
                if (count == 0) return;
                cmd.SetGlobalVector(ReflectionParams, new Vector4(settings.minNormalizationFactor.value,
                    Mathf.Max(settings.minNormalizationFactor.value, settings.maxNormalizationFactor.value),
                    0, settings.probeVolumeWeight.value));
                cmd.SetGlobalVectorArray(ReflectionL0L1, _l01);
                cmd.SetGlobalVectorArray(ReflectionL2, _l2);
                cmd.SetGlobalVectorArray(ReflectionL2Last, _l2Last);
                cmd.SetGlobalVectorArray(ReflectionBoxMin, _boxMin);
                cmd.SetGlobalVectorArray(ReflectionBoxMax, _boxMax);
            }

            private static PRTProbeVolume FindVolume(Scene scene)
            {
                if (!scene.IsValid() || !scene.isLoaded) return null;
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var candidate in root.GetComponentsInChildren<PRTProbeVolume>(true))
                        if (candidate.isActiveAndEnabled && candidate.usePRTGIForSceneObjects &&
                            candidate.asset && candidate.asset.HasValidData)
                            return candidate;
                return null;
            }

            public void Dispose()
            {
                _volume?.StopRelight();
                _runtime?.Dispose();
                _runtime = null;
                _volume = null;
                _asset = null;
            }
        }
    }
}
