using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class SkyboxSystem : MonoBehaviour
{
    private Transform t;
    
    public int scalefactor = 64;
    public static float SkyboxScale { get; set; }

    public Renderer[] renderList;

    private void OnEnable()
    {
        // 初始化位置和particle
        transform.position = Camera.main.transform.position;
        ParticleSystem particle = this.GetComponentInChildren<ParticleSystem>();
        particle.Play();
        
        t = transform;
        RenderPipelineManager.beginCameraRendering += OnCamera;
        RenderPipelineManager.endCameraRendering += OnCameraFinish;
        
        CollectRenderers();
    }

    private void CollectRenderers()
    {
        renderList = GetComponentsInChildren<Renderer>();
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnCamera;
        RenderPipelineManager.endCameraRendering -= OnCameraFinish;
    }

    private void OnCamera(ScriptableRenderContext context, Camera camera)
    {
        // t = transform;
        t = Camera.main.transform;
        
        var scaleRatio = 1.0f / scalefactor;
        SkyboxScale = scaleRatio * 8.0f;
        Shader.SetGlobalFloat("_BA_SkyboxRatio", scaleRatio);
        // t.position = camera.transform.position * (1 - scaleRatio);
        t.localScale = Vector3.one * scaleRatio;
        Shader.SetGlobalMatrix("_BA_SkyboxMatrix", Matrix4x4.TRS(-t.position, Quaternion.identity, t.localScale * scalefactor));
    }

    private void OnCameraFinish(ScriptableRenderContext context, Camera camera)
    {
        // t.position = Vector3.zero;
        t.localScale = Vector3.one;
    }
}
