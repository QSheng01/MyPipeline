using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// URP (Universal Render Pipeline) renderer feature for the Diagonal Split Volume
// post-process. The renderer feature only injects the pass; authoring parameters
// live in DiagonalSplitVolume.
//
// Setup:
//   1. Add this feature to your Universal Renderer Data asset
//      (Add Renderer Feature -> Diagonal Split Feature).
//   2. Add a Diagonal Split override to a Volume profile, assign Image 0..9,
//      set Image Count, then animate or slide Split Pos.
public class DiagonalSplitFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Tooltip("Global switch for this renderer feature. Per-scene control lives in the Volume override.")]
        public bool enabled = true;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    public Settings settings = new Settings();

    private Material _material;
    private DiagonalSplitPass _pass;

    private void OnValidate()
    {
        if (settings == null)
            return;

        if (_pass != null)
            _pass.renderPassEvent = settings.renderPassEvent;
    }

    public override void Create()
    {
        _pass?.Dispose();
        CoreUtils.Destroy(_material);

        var shader = Shader.Find("PostProcess/DiagonalSplit");
        if (shader == null)
        {
            Debug.LogError("DiagonalSplitFeature: Shader 'PostProcess/DiagonalSplit' not found.");
            return;
        }

        _material = CoreUtils.CreateEngineMaterial(shader);
        _pass = new DiagonalSplitPass(_material)
        {
            renderPassEvent = settings.renderPassEvent
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!settings.enabled || _material == null || _pass == null)
            return;

        // Skip preview/reflection cameras so we only affect the actual view.
        var camType = renderingData.cameraData.cameraType;
        if (camType == CameraType.Preview || camType == CameraType.Reflection)
            return;

        DiagonalSplitVolume volume = VolumeManager.instance.stack.GetComponent<DiagonalSplitVolume>();
        if (volume == null || !volume.IsActive())
            return;

        _pass.Setup(renderer, volume);
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        _pass?.Dispose();
        CoreUtils.Destroy(_material);
    }

    private class DiagonalSplitPass : ScriptableRenderPass
    {
        private readonly Material _mat;
        private DiagonalSplitVolume _volume;
        private ScriptableRenderer _renderer;
        private RTHandle _temp;

        private static readonly int IdImageCount    = Shader.PropertyToID("_ImageCount");
        private static readonly int IdSplitPos      = Shader.PropertyToID("_SplitPos");
        private static readonly int IdSlant         = Shader.PropertyToID("_Slant");
        private static readonly int IdLineWidth     = Shader.PropertyToID("_LineWidth");
        private static readonly int IdLineColor      = Shader.PropertyToID("_LineColor");
        private static readonly int IdEdgeSoftness  = Shader.PropertyToID("_EdgeSoftness");
        private static readonly int IdEnableScreenDistortion = Shader.PropertyToID("_EnableScreenDistortion");
        private static readonly int IdDissolveStrength = Shader.PropertyToID("_DissolveStrength");
        private static readonly int IdDistortionCenter = Shader.PropertyToID("_DistortionCenter");
        private static readonly int IdDistortionSize = Shader.PropertyToID("_DistortionSize");
        private static readonly int IdEnableImageChange = Shader.PropertyToID("_EnableImageChange");
        private static readonly int IdNextImg = Shader.PropertyToID("_NextImg");
        private static readonly int IdUseNextImg = Shader.PropertyToID("_UseNextImg");
        private static readonly int IdImageChange = Shader.PropertyToID("_ImageChange");
        private static readonly int[] IdImages =
        {
            Shader.PropertyToID("_Image0"),
            Shader.PropertyToID("_Image1"),
            Shader.PropertyToID("_Image2"),
            Shader.PropertyToID("_Image3"),
            Shader.PropertyToID("_Image4"),
            Shader.PropertyToID("_Image5"),
            Shader.PropertyToID("_Image6"),
            Shader.PropertyToID("_Image7"),
            Shader.PropertyToID("_Image8"),
            Shader.PropertyToID("_Image9"),
        };
        private static readonly int[] IdUseImages =
        {
            Shader.PropertyToID("_UseImage0"),
            Shader.PropertyToID("_UseImage1"),
            Shader.PropertyToID("_UseImage2"),
            Shader.PropertyToID("_UseImage3"),
            Shader.PropertyToID("_UseImage4"),
            Shader.PropertyToID("_UseImage5"),
            Shader.PropertyToID("_UseImage6"),
            Shader.PropertyToID("_UseImage7"),
            Shader.PropertyToID("_UseImage8"),
            Shader.PropertyToID("_UseImage9"),
        };

        public DiagonalSplitPass(Material mat)
        {
            _mat = mat;
            profilingSampler = new ProfilingSampler("DiagonalSplit");
        }

        public void Setup(ScriptableRenderer renderer, DiagonalSplitVolume volume)
        {
            _renderer = renderer;
            _volume = volume;
            // Make sure the camera color is available as an input.
            ConfigureInput(ScriptableRenderPassInput.Color);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            RenderingUtils.ReAllocateIfNeeded(ref _temp, desc, FilterMode.Bilinear, name: "_DiagonalSplitTemp");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_mat == null || _renderer == null || _volume == null)
                return;

            // cameraColorTargetHandle is only valid here (not in AddRenderPasses).
            RTHandle source = _renderer.cameraColorTargetHandle;
            if (source == null || _temp == null)
                return;

            int imageCount = Mathf.Clamp(_volume.imageCount.value, 1, 10);
            for (int i = 0; i < IdImages.Length; ++i)
            {
                Texture image = GetImage(i);
                _mat.SetTexture(IdImages[i], image != null ? image : Texture2D.blackTexture);
                _mat.SetFloat(IdUseImages[i], image != null ? 1f : 0f);
            }

            _mat.SetInt(IdImageCount, imageCount);
            _mat.SetFloat(IdSplitPos, Mathf.Clamp(_volume.splitPos.value, 0f, imageCount));
            _mat.SetFloat(IdSlant, _volume.slant.value);
            _mat.SetFloat(IdLineWidth, _volume.lineWidth.value);
            _mat.SetColor(IdLineColor, _volume.lineColor.value);
            _mat.SetFloat(IdEdgeSoftness, _volume.edgeSoftness.value);
            _mat.SetFloat(IdEnableScreenDistortion, _volume.enableScreenDistortion.value ? 1f : 0f);
            _mat.SetFloat(IdDissolveStrength, _volume.dissolveStrength.value);
            Vector2 distortionSize = new Vector2(
                Mathf.Clamp01(_volume.distortionSize.value.x),
                Mathf.Clamp01(_volume.distortionSize.value.y));
            _mat.SetVector(IdDistortionCenter, _volume.distortionCenter.value);
            _mat.SetVector(IdDistortionSize, distortionSize);
            Texture nextImg = _volume.nextImg.value;
            _mat.SetFloat(IdEnableImageChange, _volume.enableImageChange.value ? 1f : 0f);
            _mat.SetTexture(IdNextImg, nextImg != null ? nextImg : Texture2D.blackTexture);
            _mat.SetFloat(IdUseNextImg, nextImg != null ? 1f : 0f);
            _mat.SetFloat(IdImageChange, _volume.imageChange.value);

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, profilingSampler))
            {
                // source -> temp (apply diagonal split), then temp -> source.
                Blitter.BlitCameraTexture(cmd, source, _temp, _mat, 0);
                Blitter.BlitCameraTexture(cmd, _temp, source);
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        private Texture GetImage(int index)
        {
            return index switch
            {
                0 => _volume.image0.value,
                1 => _volume.image1.value,
                2 => _volume.image2.value,
                3 => _volume.image3.value,
                4 => _volume.image4.value,
                5 => _volume.image5.value,
                6 => _volume.image6.value,
                7 => _volume.image7.value,
                8 => _volume.image8.value,
                9 => _volume.image9.value,
                _ => null
            };
        }

        public void Dispose()
        {
            _temp?.Release();
            _temp = null;
        }
    }
}
