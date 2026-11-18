using System;
using UnityEngine.Serialization;


namespace UnityEngine.Rendering.Environment
{
[Serializable, VolumeComponentMenu("Environment/CharacterLightingSettingsData")]

    public class CharacterLightSettingsData : VolumeComponent
    {
       //  [Tooltip("灯光设置"),InspectorName("光照强度")]
       //  public FloatParameter intensity = new ClampedFloatParameter(1f, 0, 10f);
       //  [Tooltip("阴影强度"),InspectorName("角色阴影强度")]
       //  public FloatParameter shadowStrength = new ClampedFloatParameter(0.3f,0f, 1f);
       //  [Tooltip("自定义灯光颜色"),InspectorName("自定义灯光颜色")]
       //  public ColorParameter lightColor = new ColorParameter(new Color(1,1,1),false,false,true);
       //  // [Tooltip("自定义灯光强度"),InspectorName("自定义灯光强度")]
       //  // public FloatParameter intensity = new ClampedFloatParameter(1.4f,0f, 3f);
       //  [Tooltip("环境光补光颜色"),InspectorName("环境光补光颜色")]
       //  public ColorParameter ambientAddColor = new ColorParameter(new Color(1,1,1),false,false,true);
       //  [Tooltip("环境光补光强度"),InspectorName("环境光补光强度")]
       //  public FloatParameter ambientAddIntensity = new ClampedFloatParameter(1f,0f, 1f);
       //
       //  [Tooltip("环境光(自定义)"),InspectorName("环境光(自定义)")]
       //  public ColorParameter bakeAmbientColor = new ColorParameter(new Color(1f, 1f, 1f,1f), false, false, true);
       //  [Tooltip("环境光(自定义<->场景)"),InspectorName("环境光(自定义<->场景)")]
       //  public FloatParameter bakeAmbientColorWeight = new ClampedFloatParameter(0f, 0, 1f);
       //  [Tooltip("环境光(自定义<->场景)"),InspectorName("环境光(自定义<->场景)")]
       //  public FloatParameter bakeAmbientIntensity = new ClampedFloatParameter(0f, 0, 1f);
       //
       // private BoolParameter LockLightZDir = new BoolParameter(false);
       // [Tooltip("使用自定义灯光颜色"),InspectorName("使用自定义灯光颜色")]
       // private BoolParameter isCustomColor = new BoolParameter(false);
       // private ColorParameter rimColor = new ColorParameter(new Color(1f, 1f, 1f), false, false, true);
       // private FloatParameter rimIntensity = new ClampedFloatParameter(0.1f, 0, 1f);
       //
       // [Tooltip("使用自定义灯光颜色"),InspectorName("钳制灯光角度")]
       // public FloatParameter zoffset = new ClampedFloatParameter(20f,-60f, 60f);
       //
       //
       //
       //
       // [Tooltip("自定义角色灯光方向"),InspectorName("自定义角色灯光方向")]
       // public Vector3Parameter customMainLightDir = new Vector3Parameter(new Vector3(50, 30, 0));
       // [Tooltip("环境光(自定义)"),InspectorName("环境光(自定义)")]
       // public ColorParameter ambientColor = new ColorParameter(new Color(1f, 1f, 1f,1f), false, false, true);
       // [Tooltip("环境光(自定义<->场景)"),InspectorName("环境光(自定义<->场景)")]
       // public FloatParameter ambientIntensity = new ClampedFloatParameter(0f, 0, 1f);
       // [Tooltip("环境金属高光"),InspectorName("环境金属高光")]
       // public ColorParameter envColor = new ColorParameter(new Color(1f, 1f, 1f), false, false, true);
       // [Tooltip("环境高光颜色"),InspectorName("环境高光颜色")]
       // public ColorParameter specularColor = new ColorParameter(new Color(1f, 1f, 1f), false, false, true);
       // [Tooltip("角色高光强度"),InspectorName("角色高光强度")]
       // public FloatParameter specularIntensity = new ClampedFloatParameter(0.1f, 0, 1f);
       // [Tooltip("描边颜色"),InspectorName("角色描边颜色")]
       // public ColorParameter OutlineColor = new ColorParameter(new Color(1f, 1f, 1f), false, false, true);
       //
       // [Tooltip("开启角色边缘光"),InspectorName("开启角色边缘光")]
       // public BoolParameter OpenRim = new BoolParameter(false);
       //
       // [Tooltip("Tonemapping"),InspectorName("Tonemapping")]
       // public FloatParameter RoleToneMaping = new ClampedFloatParameter(0.75f, 0, 2f);
       // [Tooltip("开启角色自阴影"),InspectorName("开启角色自阴影")]
       // public BoolParameter OpenShielding = new BoolParameter(true);
       //
       //
       // private Vector4 lightData0 = Vector4.zero;
       // private Vector4 lightData1 = Vector4.zero;
       // private float shieldFlag = 1;
       // private Vector3 lightDir;
       // private Vector4 lightData2 = Vector4.zero;
       // private Vector4 lightData3 = Vector4.zero;
       // private Vector4 lightData4 = Vector4.zero;
       // private Vector4 lightData5 = Vector4.zero;
       // private Vector4 lightColorData = Vector4.zero;
       // private float m_EularX;
       // private float m_EularY;

       [Serializable]
       public enum LightSourceType
       {
           SceneLight,
           SelfController,
           FollowCameraWithSelf,
           FollowCameraWithSceneLight
       }

       [Serializable]
       public sealed class LightSourceTypeParameter : VolumeParameter<LightSourceType>
       {
           /// <summary>
           /// Creates a new <see cref="CloudTypeParameter"/> instance.
           /// </summary>
           /// <param name="value">The initial value to store in the parameter.</param>
           /// <param name="overrideState">The initial override state for the parameter.</param>
           public LightSourceTypeParameter(LightSourceType value, bool overrideState = false) : base(value, overrideState) { }
       }

        // TODO：如果没有使用某些功能就不要传入
        #region ReadOnly
        private readonly int characterLightColor = Shader.PropertyToID("_CharacterLightColor"); // xyz: color a: none
        private readonly int characterLightData0 = Shader.PropertyToID("_CharacterLightData0"); // x:toggle y:阴影强度， z:环境光强度 w:灯光强度
        private readonly int characterAmbientColor = Shader.PropertyToID("_CharacterAmbientColor"); // xyz: color， a:补光强度
        private readonly int characterBakeAmbientColor = Shader.PropertyToID("_characterBakeAmbientColor"); // xyz: 自定义环境光颜色， a:使用场景环境光颜色还是使用自定义环境光颜色
        private readonly int characterEnvColor = Shader.PropertyToID("_CharacterEnvColor"); // xyz：color, a:intensity
        //2023.3.7场景角色Shader不需要边缘光
        //private readonly int characterRimColor = Shader.PropertyToID("_CharacterRimColor"); // xyz:color, a:intensity
        private readonly int characterSpecularColor = Shader.PropertyToID("_CharacterSpecularColor"); // xyz:color, a:intensity
        private readonly int characterLightDir = Shader.PropertyToID("_CharacterLightDir"); // xyz:dir, w: 是否针对face + follow camera
        #endregion

        #region Public Filed
        [Tooltip("光源类型"),InspectorName("光源类型")]
        public LightSourceTypeParameter lightSourceType = new LightSourceTypeParameter(0);
        // public LightSourceType lightSourceType = LightSourceType.SceneLight;
        [Tooltip("显示方向(Debug)"),InspectorName("显示方向(Debug)")]
        public BoolParameter isDebug = new BoolParameter(true);
        // public bool isDebug = true;

        [Tooltip("是否限制场景中角色的光照方向"),InspectorName("是否限制场景中角色的光照方向")]
        public BoolParameter isRestrictCharacterLightDir = new BoolParameter(true);
        // public bool isRestrictCharacterLightDir = true;//限制场景中角色的光照方向

        [Tooltip("光照强度"),InspectorName("光照强度")]
        public FloatParameter intensity = new ClampedFloatParameter(1f, 0, 10f);
        [Tooltip("阴影强度"),InspectorName("阴影强度")]
        public FloatParameter shadowStrength = new ClampedFloatParameter(1f, 0, 1f);
        // 光照强度
        // [Range(0, 10)] public float intensity = 1;
        // 阴影强度
        // [Range(0, 1)] public float shadowStrength = 1;

        [Tooltip("是否使用自定义颜色"),InspectorName("是否使用自定义颜色")]
        public BoolParameter isCustomColor = new BoolParameter(true);
        // public bool isCustomColor;

        [Tooltip("灯光颜色"),InspectorName("灯光颜色")]
        public ColorParameter lightColor = new ColorParameter(new Color(1f, 1f, 1f,1f), false, false, true);
        // [ColorUsage(false)]public Color lightColor = Color.white;

        public Camera followCamera;
        public Vector3 followCameraOffset;

        [Tooltip("灯光颜色"),InspectorName("灯光颜色")]
        public ColorParameter ambientAddColor = new ColorParameter(Color.gray, false, false, true);
        // 环境光补光颜色
        // [ColorUsage(false)][FormerlySerializedAs("ambientAddColor")] public Color ambientAddColor = Color.gray;//环境光补光颜色

        [Tooltip("环境光补光强度"),InspectorName("环境光补光强度")]
        public FloatParameter ambientAddIntensity = new ClampedFloatParameter(1f, 0, 1f);
        // 环境光补光强度
        // [FormerlySerializedAs("ambientIntensity")] [Range(0, 1)] public float ambientAddIntensity = 0;//环境光补光强度

        [Tooltip("灯光颜色"),InspectorName("灯光颜色")]
        public ColorParameter bakeAmbientColor = new ColorParameter(Color.gray, false, false, true);
        // 自定义环境光颜色
        // [ColorUsage(false)][FormerlySerializedAs("ambientColor")] public Color bakeAmbientColor = Color.gray;//自定义环境光颜色

        [Tooltip("场景环境光颜色<-->自定义环境光颜色"),InspectorName("场景环境光颜色<-->自定义环境光颜色")]
        public ClampedFloatParameter bakeAmbientColorWeight = new ClampedFloatParameter(1.0f,0f,1f);
        // 场景环境光颜色<-->自定义环境光颜色
        // [Range(0, 1)] public float bakeAmbientColorWeight = 1;//使用场景环境光颜色还是使用自定义环境光颜色

        [Tooltip("环境光整体强度"),InspectorName("环境光整体强度")]
        public ClampedFloatParameter bakeAmbientIntensity = new ClampedFloatParameter(1.0f,0f,1f);
        // 环境光整体强度
        // [Range(0, 1)] public float bakeAmbientIntensity = 1;

        [Tooltip("环境反射颜色"),InspectorName("环境反射颜色")]
        public ColorParameter envColor = new ColorParameter(Color.white, false, false, true);
        // 环境反射颜色
        // [ColorUsage(false)]public Color envColor = Color.white; // 环境球颜色

        [Tooltip("高光颜色"),InspectorName("高光颜色")]
        public ColorParameter specularColor = new ColorParameter(new Color(1, 1, 1, 0.1f), false, false, true);
        // 高光颜色
        // [ColorUsage(false)]public Color specularColor = new Color(1, 1, 1, 0.1f);

        [Tooltip("高光强度"),InspectorName("高光强度")]
        public ClampedFloatParameter specularIntensity = new ClampedFloatParameter(0.1f,0f,1f);
        // 高光强度
        // [Range(0, 1)] public float specularIntensity = 0.1f;


        [ColorUsage(false)]public Color rimColor = new Color(1, 1, 1, 0.1f);
        [Range(0, 1)] public float rimIntensity = 0.1f;
        #endregion

        #region UI Filed // TODO：是否需要暴露出来调？

        //public LayerMask uiCameraLayer = 1 << 5 ; // 默认UI

        #endregion

        #region Private Filed
        private Transform lightTf;
        private Vector4 lightData0 = Vector4.zero;
        private Vector4 lightDir = Vector4.zero;
        #endregion


        // 实现接口
        public bool IsActive() => active;

        private void SetupBuffer()
        {
            lightData0.x = 1;
            Shader.SetGlobalColor(characterLightColor, lightColor.value);
            Shader.SetGlobalColor(characterAmbientColor, ambientAddColor.value);
            Shader.SetGlobalColor(characterBakeAmbientColor, bakeAmbientColor.value);
            Shader.SetGlobalVector(characterLightData0, lightData0);
            Shader.SetGlobalVector(characterLightDir, lightDir);
            Shader.SetGlobalColor(characterEnvColor, envColor.value);
            //Shader.SetGlobalColor(characterRimColor, rimColor);
            Shader.SetGlobalColor(characterSpecularColor, specularColor.value);
        }

        private void ClearBuffer()
        {
            lightData0.x = 0;
            Shader.SetGlobalVector(characterLightData0, lightData0);
        }


        private void UpdateLightData( CharacterLightSettingsData data)
        {
            SetupLightData();
            // if (RenderSettings.sun != null)
            // {
            //     Vector3 forward =  RenderSettings.sun.transform.forward;
            //     lightDir.x = -forward.x;
            //     lightDir.y = -forward.y;
            //     lightDir.z = -forward.z;
            // }
            // else
            // {
            //     Debug.Log("no light");
            // }
            //
            // //lightData5= data.customMainLightDir.value.normalized;
            // if (data.customMainLightDir.overrideState)
            // {
            //     Vector4 customForward  = Vector4.one;
            //     if (PerObjectShadowCtrlManager.Instance != null&&PerObjectShadowCtrlManager.Instance.perObjectShadowSetting!=null)
            //     {
            //         //PerObjectShadowCtrlManager.Instance = lightDir;
            //
            //         // PerObjectShadowCtrlManager.Instance.perObjectShadowSetting.customDirBtn = true;
            //         // PerObjectShadowCtrlManager.Instance.perObjectShadowSetting.volumeLightRotation =
            //         //     Quaternion.Euler(data.customMainLightDir.value);
            //         lightData5   =  Quaternion.Euler(data.customMainLightDir.value) * Vector3.back;
            //     }
            //
            //     lightData5.w = 1;
            // }
            // else
            // {
            //     if (PerObjectShadowCtrlManager.Instance != null&&PerObjectShadowCtrlManager.Instance.perObjectShadowSetting!=null)
            //     {
            //         //PerObjectShadowCtrlManager.Instance = lightDir;
            //         // PerObjectShadowCtrlManager.Instance.perObjectShadowSetting.customDirBtn = false;
            //     }
            //     lightData5.w = 0;
            // }
            //
            // if (data.zoffset.overrideState)
            // {
            //     var lateRotation = RenderSettings.sun.transform.rotation;
            //     m_EularX = RenderSettings.sun.transform.eulerAngles.x;
            //     m_EularY = RenderSettings.sun.transform.eulerAngles.y;
            //
            //     var tempmin = m_EularX > 180 ? (360-data.zoffset.value) : 0;
            //     var tempmax = m_EularX > 180 ? 370 : data.zoffset.value;
            //
            //     // Rotate With X Axis
            //     var temp2 = Mathf.Clamp(m_EularX, tempmin, tempmax);
            //     RenderSettings.sun.transform.localEulerAngles = new Vector3(temp2, 0, 0);
            //
            //     // Rotate With Y Axis
            //     RenderSettings.sun.transform.Rotate(Vector3.up, this.m_EularY, Space.World);
            //     lightDir = -RenderSettings.sun.transform.forward;
            //     RenderSettings.sun.transform.rotation = lateRotation;
            // }
            //
            // lightColorData = lightColor.value.linear;
            //
            // if (!data.lightColor.overrideState)
            // {
            //     lightColorData = RenderSettings.sun.color.linear;
            // }
            // lightData0.x = 1;
            // lightData0.w = data.intensity.value;
            // lightData1.y = data.ambientIntensity.value;
            //
            // lightData1.x = shadowStrength.value;
            //
            // lightData2 = ambientColor.value;
            // lightData2.w = bakeAmbientIntensity.value;
            // //ambientColor.value.a = bakeAmbientIntensity.value;
            // lightData3 = specularColor.value;
            // lightData3.w = specularIntensity.value;
            //
            // lightData4 = rimColor.value;
            //
            // lightData4.w = (OpenRim.value && data.OpenRim.overrideState) ? 1 : 0;
            // shieldFlag = (OpenShielding.value && data.OpenShielding.overrideState) ? 1 : 0;
            //
            // ambientAddColor.a = ambientAddIntensity;
        }

        /// <summary>
        /// 设置光源方向类型。
        /// </summary>
        private void SetupLightSource()
        {
            Transform cameraTf;
            switch (lightSourceType.value)
            {
                case LightSourceType.SceneLight:
                    lightDir.w = 0;
                    if (RenderSettings.sun != null)
                    {
                        lightTf = RenderSettings.sun.transform;
                    }
                    else
                    {
                        #if UNITY_EDITOR
                            Debug.LogError("Character Light: 场景没有主灯光，不能选择Scene模式");
                        #endif
                    }
                    break;
                // case LightSourceType.SelfController:
                //     lightDir.w = 0;
                //     lightTf = transform;
                //     break;
                // case LightSourceType.FollowCameraWithSelf :
                //     lightDir.w = 1;
                //     lightTf = transform;
                //     if (followCamera)
                //     {
                //         cameraTf = followCamera.transform;
                //     }
                //     else
                //     {
                //         cameraTf = Camera.main.transform;
                //     }
                //
                //     lightTf.rotation = cameraTf.rotation;
                //
                //     // 如果相机正好在角色背面，那么offset的y轴会渐变为0
                //     if (followCameraOffset.x != 0)
                //     {
                //         lightTf.transform.Rotate(lightTf.right, followCameraOffset.x, Space.World);
                //     }
                //     if (followCameraOffset.y != 0)
                //     {
                //         lightTf.transform.Rotate(lightTf.up, followCameraOffset.y, Space.World);
                //     }
                //
                //     break;
                // case LightSourceType.FollowCameraWithSceneLight :
                //     lightDir.w = 1;
                //     if (RenderSettings.sun != null)
                //     {
                //         lightTf = RenderSettings.sun.transform;
                //     }
                //     else
                //     {
                //         lightTf = transform;
                //         lightSourceType = LightSourceType.FollowCameraWithSelf;
                //         #if UNITY_EDITOR
                //             Debug.LogError("Character Light: 场景没有主灯光，不能选择Scene模式");
                //         #endif
                //     }
                //     if (followCamera)
                //     {
                //         cameraTf = followCamera.transform;
                //     }
                //     else
                //     {
                //         cameraTf = Camera.main.transform;
                //     }
                //
                //     lightTf.rotation = cameraTf.rotation;
                //
                //     // 如果相机正好在角色背面，那么offset的y轴会渐变为0
                //     if (followCameraOffset.x != 0)
                //     {
                //         lightTf.transform.Rotate(lightTf.right, followCameraOffset.x, Space.World);
                //     }
                //     if (followCameraOffset.y != 0)
                //     {
                //         lightTf.transform.Rotate(lightTf.up, followCameraOffset.y, Space.World);
                //     }
                //
                //     break;
            }

            lightDir.x = -lightTf.forward.x;
            lightDir.y = -lightTf.forward.y;
            lightDir.z = -lightTf.forward.z;

            if (isRestrictCharacterLightDir.value)
            {//限制光照方向
                // Debug.Log("=====================================================");
                // Debug.Log("光照方向：" + lightDir.ToString("f5"));

                //光照夹角
                float lightAngle_Cos = Vector3.Dot(lightDir, Vector3.up);
                float lightAngle_Sin = Vector3.Cross(lightDir, Vector3.up).magnitude;

                // Debug.Log("光照夹角：" + Mathf.Acos(lightAngle_Cos) * Mathf.Rad2Deg);
                // Debug.Log("光照Y轴" + lightDir.magnitude * lightAngle_Cos);

                //新的光照方向
                if (lightAngle_Cos > 0.6427876f)//光照垂直夹角小于50°
                {
                    float newLightDirLength = lightAngle_Cos / 0.6427876f;//cos 50° = 0.6427876f
                    float newXZVector = 0.7660444f * newLightDirLength;//sin 50° = 0.7660444f
                    float xzScale = newXZVector / lightAngle_Sin;

                    lightDir.x *= xzScale;
                    lightDir.z *= xzScale;

                    Vector3 newLightDir = Vector3.Normalize(lightDir);
                    lightDir.x = newLightDir.x;
                    lightDir.y = newLightDir.y;
                    lightDir.z = newLightDir.z;

                    // Debug.Log( "新光照向量长度：" + newLightDirLength);
                    // Debug.Log("新光照向量：" + lightDir.ToString("f5"));
                }
            }
        }


        private void SetupLightData()
        {
            if (!isCustomColor.value && RenderSettings.sun != null)
            {
                lightColor.value = RenderSettings.sun.color.linear;
            }

            lightData0.x = 1;
            lightData0.y = shadowStrength.value;
            lightData0.z = bakeAmbientIntensity.value;
            lightData0.w = intensity.value;

            Color tAmbientAddColor = ambientAddColor.value;
            tAmbientAddColor.a = ambientAddIntensity.value;
            ambientAddColor.value = tAmbientAddColor;

            Color tBakeAmbientColor = bakeAmbientColor.value;
            tBakeAmbientColor.a = bakeAmbientColorWeight.value;
            bakeAmbientColor.value = tBakeAmbientColor;

            Color tSpecularColor = specularColor.value;
            tSpecularColor.a = specularIntensity.value;
            specularColor.value = tSpecularColor;

            rimColor.a = rimIntensity;

        }

        protected override void OnDisable()
        {
            ClearBuffer();
        }

        protected override void OnDestroy()
        {
            ClearBuffer();
        }


        public override void Override(VolumeComponent state, float interpFactor)
        {
            base.Override(state, interpFactor);
            //LightingSettingsData data = state as LightingSettingsData;
            CharacterLightSettingsData data = state as CharacterLightSettingsData;

            if (!data || !data.IsActive() || !RenderSettings.sun)
            {
                return;
            }

            SetupLightSource();
            UpdateLightData(data);
            SetupBuffer();
        }
    }
}
