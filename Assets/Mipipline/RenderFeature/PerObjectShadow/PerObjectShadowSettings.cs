// Gavin_KG presents
using System;
#if MAP_EDITOR
using Sirenix.OdinInspector;
#endif
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace SYPackages.PerObjectShadow
{
    /// <summary>
    /// Can be changed per frame.
    /// </summary>
    [Serializable]
#if MAP_EDITOR
    [BoxGroup("Setting", showLabel: true)]
    [HideLabel]
#endif
    public class PerObjectShadowSettings
    {
        public enum SliceResolution
        {
            _16 = 16,
            _32 = 32,
            _64 = 64,
            _128 = 128,
            _256 = 256,
            _512 = 512,
            _1024 = 1024,
            _2048 = 2048
        }

        #region Unity Inspector
#if MAP_EDITOR
        [Title("阴影质量相关")]
        [LabelText("阴影贴图分辨率")]
#endif
        public SliceResolution ResolutionSize = SliceResolution._1024;
#if MAP_EDITOR
        [Space(5)] [LabelText("Power of 2")]
#endif
        public bool isPowBy2 = false;
#if MAP_EDITOR
        [LabelText("软阴影")]
#endif
        public bool solfShadow = false;
#if MAP_EDITOR
        [LabelText("阴影Frustum")]
#endif
        [Range(0, 50)] public float frustumExtend = 3f; // 防止穿透
        [Range(-0.5f, 0.5f)] public float shadowBias = 0.01f; // 防止穿透
        [FormerlySerializedAs("globalResScaleFactor")]
#if MAP_EDITOR
        [LabelText("降采样")] [Range(0.1f, 1.0f)]
#endif
        public float screenSpaceMapRenderScale = 1.0f;
#if MAP_EDITOR
        [LabelText("屏幕占比缩放（越小分辨率缩小越厉害）")]
#endif
        [Range(0, 4)]
        public int adaptiveResRemapFactor = 3;
#if MAP_EDITOR
        [LabelText("阴影距离")]
#endif
        [Range(0, 200)]
        public float shadowDistance = 50f;
        [Space(10)]
#if MAP_EDITOR
        [Title("资源相关")]
        [AssetSelector(Paths = @"Assets\Things\Shaders\SYPackages\PeObjectShadow")]
#endif
        [SerializeField]
        public Shader resolveShader;
        [HideInInspector] [NonSerialized] private Material resolveMaterial;
        public float ShadowStrength =>  DirectionLight ? DirectionLight.shadowStrength : 1.0f;
#if MAP_EDITOR
        [Title("灯光方向")]
#endif
        public Light DirectionLight;

        public Quaternion LightRotation => RoomLight ? RoomLightRotation : (DirectionLight ? DirectionLight.transform.rotation : Quaternion.identity);

        private bool RoomLight;
        private Quaternion RoomLightRotation;

        public Vector3 LightDirection {
            get {
                return LightRotation * Vector3.up;
            }
        }

        public Material resolveMat
        {
            get
            {
                if (!resolveMaterial)
                {
#if UNITY_EDITOR
                    if (!resolveShader)
                    {
                        //resolveShader = Shader.Find("Hidden/ShiYue/PerObjectShadowResolve");
                        resolveShader = AssetDatabase.LoadAssetAtPath<Shader>(@"Assets\Things\Shaders\SYPackages\PeObjectShadow.shader");
                    }

                    if (!resolveShader)
                    {
                        Debug.LogError(resolveShader);
                    }
#endif
                    if (resolveShader)
                    {
                        resolveMaterial = new Material(resolveShader)
                        {
                            hideFlags = HideFlags.DontSave,
                        };
                        resolveMaterial.enableInstancing = true;
                    }
                    else
                    {
                        Debug.LogError("Can't Find resolve Shader");
                    }
                }

                return resolveMaterial;
            }
        }

        public void SetShadowInfo(bool act, Vector3 rot,float st)
        {
            RoomLight = act;
            RoomLightRotation = new Quaternion();
            RoomLightRotation.eulerAngles = rot;
        }

        #endregion
    }
}