using System.Collections.Generic;
using UnityEngine;
using System.Linq;
// using Sirenix.OdinInspector;

[ExecuteAlways]
[AddComponentMenu("VoxelVolumetric/Voxel Volume Light")]
[RequireComponent(typeof(Light))]
public class VoxelVolumeLight : MonoBehaviour
{
    [SerializeField][Header("套用灯光组件数据")]
    private bool inheritDataFromLightComponent=true;

    [SerializeField][Header("体积光强度")]
    [Range(0,16)]
    private float volumetricMultiply = 1f;
    
    [Header("灯光组件")]
    private Light lightComponent;
    
    [SerializeField][Header("灯光颜色")]
    [ColorUsage(false, false)]
    private Color lightColor = Color.white;

    public Vector4 LightColor
    {
        get
        {
            if (inheritDataFromLightComponent && lightComponent != null)
                return lightComponent.color * lightComponent.intensity  ;

            return lightColor * lightIntensity;
        }
    }

    [SerializeField][Header("偏移")]
    [Min(0)]
    private float bias;

    [SerializeField][Header("灯光范围")]
    [Min(0)]
    private float lightRange = 5;
    public float LightRange
    {
        get
        {
            if (inheritDataFromLightComponent && lightComponent != null)
                return lightComponent.range;

            return lightRange;
        }
    }
    

    
    [SerializeField][Header("灯光强度")]
    [Min(0)]
    private float lightIntensity = 10f;
    public float LightIntensity
    {
        get
        {
            if (inheritDataFromLightComponent && lightComponent != null)
            {
                return volumetricMultiply ;
            }
            
            return volumetricMultiply;
        }
    }
    

    
    public Vector4 LightDirection
    {
        get { return -transform.localToWorldMatrix.GetColumn(2); }
    }

    public Vector4 LightPosition
    {
        get { return transform.localToWorldMatrix.GetColumn(3); }
    }

    LightAngle lightAngleData;
    public LightAngle LightAngleData
    {
        get
        {
            if (lightAngleData == null)
            {
                lightAngleData = new LightAngle(lightComponent.type, lightComponent.innerSpotAngle, lightComponent.spotAngle, LightRange, bias);
            }
            return lightAngleData;
        }
    }

    public class LightAngle
        {
            private LightType type;
            public LightType Type
            {
                get { return type; }
                set
                {
                    if (type != value)
                    {
                        type = value;
                        RecalculateShaderData();
                    }
                }
            }

            private float innerSpotAngle;
            public float InnerSpotAngle
            {
                get { return innerSpotAngle; }
                set
                {
                    if (innerSpotAngle != value)
                    {
                        innerSpotAngle = value;
                        RecalculateShaderData();
                    }
                }
            }
            private float spotAngle;
            public float SpotAngle
            {
                get { return spotAngle; }
                set
                {
                    if (spotAngle != value)
                    {
                        spotAngle = value;
                        RecalculateShaderData();
                    }
                }
            }

            private float bias;

            public float Bias
            {
                get { return bias; }
                set
                {
                    if (bias != value)
                    {
                        bias = value;
                        SqrBias = value * value;
                    }
                }
            }

            private float SqrBias;
            private float range;
            public float Range
            {
                get { return range; }
                set
                {
                    if (range != value)
                    {
                        range = value;
                        InverseSquareRange = 1.0f / (range * range);
                    }
                }
            }
            public float InverseSquareRange;

            private Vector2 shaderLightAngle;

            public Vector4 GetAngleData()
            {
                return new Vector4(shaderLightAngle.x, shaderLightAngle.y, SqrBias, InverseSquareRange);
            }

            public LightAngle(LightType type, float innerSpotAngle, float spotAngle, float range, float bias)
            {
                this.type = type;
                this.innerSpotAngle = innerSpotAngle;
                this.spotAngle = spotAngle;
                this.Range = range;
                this.Bias = bias;

                RecalculateShaderData();
            }

            private void RecalculateShaderData()
            {
                shaderLightAngle = CalculateLightAngles();
                SqrBias = Bias * Bias;
            }

            private Vector2 CalculateLightAngles()
            {
                if (type != LightType.Spot)
                    return new Vector2(0f, 1f);

                float innerHalfRads = Mathf.Cos(0.5f * innerSpotAngle * Mathf.Deg2Rad);
                float outerHalfRads = Mathf.Cos(0.5f * spotAngle * Mathf.Deg2Rad);
                float mapped = 1.0f / Mathf.Max(innerHalfRads - outerHalfRads, 0.001f);
                return new Vector2(mapped, mapped * -outerHalfRads);
            }
        }

    protected virtual float GetSqrMagnitude(Vector3 comparator)
    {
        return Vector3.SqrMagnitude(comparator - transform.position);
    }
    
     public static void SortByDistance(Vector3 c)
     {
         _Lights = _Lights.OrderBy(x => x.GetSqrMagnitude(c)).ToList();
     }

     private static List<VoxelVolumeLight> _Lights = new List<VoxelVolumeLight>();
     public static List<VoxelVolumeLight> Lights
     {
         get { return _Lights; }
     }

     protected void Reset()
     {
         VoxelVolumCommon.CheckMaxLightCount(Lights.Count, this);
     }

     private void OnValidate()
     {
         lightComponent = GetComponent<Light>();
         lightAngleData = new LightAngle(lightComponent.type, lightComponent.innerSpotAngle, lightComponent.spotAngle, LightRange, bias);
     }

     protected void OnEnable()
     {
         lightComponent = GetComponent<Light>();
         _Lights.Add(this);
     }

     private void Update()
     {
         LightAngleData.Type = lightComponent.type;
         LightAngleData.Range = LightRange;
         LightAngleData.Bias = bias;
         LightAngleData.InnerSpotAngle = lightComponent.innerSpotAngle;
         LightAngleData.SpotAngle = lightComponent.spotAngle;
     }

     protected void OnDisable()
     {
         _Lights.Remove(this);
     }

     public void CheckForLight()
     {
         lightComponent = GetComponent<Light>();
     }

     /// <summary>
     /// To make the light inherit the light component properties, set to true.
     /// </summary>
     /// <param name="state"></param>
     public void SetInheritance(bool state)
     {
         inheritDataFromLightComponent = state;
     }

     /// <summary>
     /// If you change the light type or angles during runtime, you should set the light dirty so that it recalculates some internal properties.
     /// </summary>
     public void SetDirty()
     {
         lightAngleData = new LightAngle(lightComponent.type, lightComponent.innerSpotAngle, lightComponent.spotAngle, LightRange, bias);
     }

     #region Editor
#if UNITY_EDITOR
     private void OnDrawGizmosSelected()
     {
         if (!inheritDataFromLightComponent)
         {
             Gizmos.color = LightColor;
             Gizmos.DrawWireSphere(transform.position, lightRange);
         }
     }
#endif
     #endregion
    
}

