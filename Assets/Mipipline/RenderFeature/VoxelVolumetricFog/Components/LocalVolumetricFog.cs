using System.Collections.Generic;
using UnityEngine;
using System.Linq;
// using Sirenix.OdinInspector;

[ExecuteAlways]
[AddComponentMenu("VoxelVolumetric/Local Volumetric Fog")]
public class LocalVolumetricFog : MonoBehaviour
{
    public enum BlendMode
    {
        [Header("相乘")]
        Multiplicative,
        [Header("覆盖")]
        Exclusive
    }
    

    
    private Vector3 Abs(Vector3 v)
    {
        return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }

    private Matrix4x4 localFogMatrix;
    public Matrix4x4 GetLocalFogMatrix() 
    {
        return localFogMatrix;
    }
    void UpdateMatrix() 
    {
        Vector3 scale = transform.localScale;
        float w = scale.x * 1f;
        float h = scale.y * 1f;
        float z = scale.z * 1f;
        var proj = Matrix4x4.Ortho(-w, w, h, -h, z, -z);
        var local = transform.worldToLocalMatrix;
        localFogMatrix = local;
    }
    
    public Vector3 Size
    {
        get => Abs((transform.localScale));
    }
    [Header("颜色")]
    public Color Albedo = Color.white;

    [SerializeField][Header("浓度模式")]
    private BlendMode mode = BlendMode.Multiplicative;
    public BlendMode Mode
    {
        get { return mode; }
    }
    
    [SerializeField, Min(0)][Header("浓度")]
    private float densityMultiplier = 1;
    public float DensityMultiplier
    {
        get { return densityMultiplier; }
    }
    
    [SerializeField, Min(0)][Header("边界柔化")]
    private float blendDistance = 0;
    public float BlendDistance
    {
        get { return blendDistance; }
    }
    
    protected virtual float GetSqrMagnitude(Vector3 comparator)
    {
        return Vector3.SqrMagnitude(comparator - transform.position);
    }
    public static void SortByDistance(Vector3 c)
    {
        fogVolumes = fogVolumes.OrderBy(x => x.GetSqrMagnitude(c)).ToList();
    }
    
    private static List<LocalVolumetricFog> fogVolumes = new List<LocalVolumetricFog>();
    public static List<LocalVolumetricFog> FogVolumes
    {
        get { return fogVolumes; }
    }

    protected void Reset()
    {
        VoxelVolumCommon.CheckMaxFogVolumeCount(FogVolumes.Count, this);
    }

    protected void OnEnable()
    {
        fogVolumes.Add(this);
    }

    protected void OnDisable()
    {
        fogVolumes.Remove(this);
    }
    
    void Update()
    {
        if (transform.hasChanged)
        {
            UpdateMatrix();
            transform.hasChanged = false;
        }
    }
    
    #region Editor
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {

        DrawBox();
        //Gizmos.matrix = oldMatrix;
    }

    private void OnDrawGizmosSelected()
    {
        var oldMatrix = Gizmos.matrix;
        var rotationMatrix =transform.localToWorldMatrix;
        Gizmos.matrix = rotationMatrix;
        DrawBoxSelected();
        Gizmos.matrix = oldMatrix;
    }

    private void DrawBoxSelected()
    {
        if (mode == BlendMode.Exclusive)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            Gizmos.color = Color.red * new Color(1, 1, 1, 0.3f);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one + Vector3.one * blendDistance*0.1f);
        }

        if (mode == BlendMode.Multiplicative)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            Gizmos.color = Color.green * new Color(1, 1, 1, 0.3f);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one + Vector3.one * blendDistance*0.1f);
        }
    }
    
    private void DrawBox()
    {
        var oldMatrix = Gizmos.matrix;
        var rotationMatrix = transform.localToWorldMatrix;
        Gizmos.matrix = rotationMatrix;
        if (mode == BlendMode.Exclusive)
        {
            Gizmos.color = Color.red * new Color(1, 1, 1, 0.05f);
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
        }

        if (mode == BlendMode.Multiplicative)
        {
            Gizmos.color = Color.green * new Color(1, 1, 1, 0.05f);
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
        }
        Gizmos.matrix = oldMatrix;
    }
    #endif
#endregion
}
