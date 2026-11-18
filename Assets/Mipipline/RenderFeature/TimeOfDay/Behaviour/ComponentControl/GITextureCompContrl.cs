using System;
using System.Collections;
using System.Collections.Generic;
using SYPackages.TimeOfDay;
using UnityEngine;
using Sirenix.OdinInspector;

namespace SYPackages.TimeOfDay
{
    public class GITextureCompContrl : TodCompContrl
    {
        public GIMode GIModeEnum = GIMode.realTime;

        //[SerializeField]
        Texture3D GIL00_0;
        //[SerializeField]
        Texture3D GIL00_1;

        //[SerializeField]
        Texture3D GIL10_0;
        //[SerializeField]
        Texture3D GIL10_1;

        //[SerializeField]
        Texture3D GIL11_0;
        //[SerializeField]
        Texture3D GIL11_1;

        //[SerializeField]
        Texture3D GIL12_0;
        //[SerializeField]
        Texture3D GIL12_1;

        public TimeOfDayManager todm;
        public AnimationCurve CubeMapLerp;
        [ReadOnly]public float Weight;

        //[SerializeField]
        [Header("包围盒MIN")]
        public Vector4 tempPosMin;
        //[SerializeField]
        [Header("包围盒MAX")]
        public Vector4 tempPosMax;

        [SerializeField]
        [Range(-0.5f,1.5f)]
        public float _GIClampMin = -0.1f;
        [SerializeField, InspectorName("")]
        [Range(-0.5f,1.5f)]
        public float _GIClampMax = 0.8f;

        [Header("GI强度")]
        [SerializeField, InspectorName("GI强度")]
        [Range(0, 1)]
        public float _GIClampIntensity = 1f;

        [Header("与默认GI的混合比例")]
        [SerializeField, InspectorName("与默认GI的混合比例")]
        [Range(0, 1)]
        public float _GIMixRange = 1f;
        [Header("GI采样偏移")]
        [SerializeField, InspectorName("GI采样偏移")]
        [Range(0f,3f)]
        public float GISampleOffSet = 0.8f;
        [Space(10)]

        [SerializeField]
        [Range(-0.5f,1.5f)]
        public float _AOClampMin = 0f;
        [SerializeField]
        [Range(-0.5f,1.5f)]
        public float _AOClampMax = 0.8f;
        [Header("天光AO强度")]
        [SerializeField, InspectorName("天光AO强度")]
        [Range(0, 1)]
        public float _AOMix = 0f;

        [Header("天光AO采样偏移")]
        [SerializeField, InspectorName("天光AO采样偏移")]
        [Range(0f,3f)]
        public float AOSampleOffSet = 1f;

        [SerializeField]
        [Header("勾上使用Unity默认GI")]
        public bool debugGI;
        [Header("勾上检查bakedGI效果")]
        [SerializeField]
        public bool debugGI2;
        [Header("勾上检查天光AO效果")]
        [SerializeField]
        public bool debugGI3;

        public Texture3D[] L0TexList;
        public Texture3D[] L10TexList;
        public Texture3D[] L11TexList;
        public Texture3D[] L12TexList;

        //public Texture BakeL00;
        //public Texture BakeL10;
        //public Texture BakeL11;
        //public Texture BakeL12;

        public enum GIMode
        {
            realTime,
            BakeTex
        }

        void Awake()
        {
            todm = FindObjectOfType<TimeOfDayManager>();
        }

        public override void Play()
        {
            base.Play();
            UpdateGITexture();
        }


        public void UpdateGITexture()
        {
            if (GIModeEnum == GIMode.BakeTex)
             {
                 if(todm==null)
                     return;

                 float lerpValue = CubeMapLerp.Evaluate(todm.timeOfDay);
                 int textureCount = L0TexList.Length;
                 lerpValue /= (24/textureCount);

                 Weight = GetFractionalPart(lerpValue);
                 Shader.SetGlobalFloat("_TOD_GI_Weight", Weight);
                 int stage = Mathf.FloorToInt(lerpValue);
                 int TodTextureLenght = L0TexList.Length;

                 if (TodTextureLenght == 0)
                 {
                     debugGI = true;
                 }

                 stage = Mathf.Max(0,stage % textureCount);

                 GIL00_0 = L0TexList[stage];
                 GIL00_1 = L0TexList[(stage+1) % textureCount];

                 GIL10_0 = L10TexList[stage];
                 GIL10_1 = L10TexList[(stage+1) % textureCount];

                 GIL11_0 = L11TexList[stage];

                 GIL11_1 = L11TexList[(stage+1) % textureCount];

                 GIL12_0 = L12TexList[stage];
                 GIL12_1 = L12TexList[(stage+1) % textureCount];

                 Shader.SetGlobalTexture("_TOD_GITexL0_0", GIL00_0);
                 Shader.SetGlobalTexture("_TOD_GITexL0_1", GIL00_1);

                 Shader.SetGlobalTexture("_TOD_GITexL10_0", GIL10_0);
                 Shader.SetGlobalTexture("_TOD_GITexL10_1", GIL10_1);

                 Shader.SetGlobalTexture("_TOD_GITexL11_0", GIL11_0);
                 Shader.SetGlobalTexture("_TOD_GITexL11_1", GIL11_1);

                 Shader.SetGlobalTexture("_TOD_GITexL12_0", GIL12_0);
                 Shader.SetGlobalTexture("_TOD_GITexL12_1", GIL12_1);

                 Shader.SetGlobalVector("_GITexMin",tempPosMin);
                 Shader.SetGlobalVector("_GITexMax",tempPosMax);
             }


            Shader.SetGlobalFloat("_GIClampMin",_GIClampMin);
            Shader.SetGlobalFloat("_GIClampMax",_GIClampMax);
            Shader.SetGlobalFloat("_GIClampIntensity",_GIClampIntensity);
            Shader.SetGlobalFloat("_GIMixRange",_GIMixRange);
            Shader.SetGlobalFloat("GISampleOffSet",GISampleOffSet);
            Shader.SetGlobalFloat("AOSampleOffSet",AOSampleOffSet);

            Shader.SetGlobalFloat("_AOClampMin",_AOClampMin);
            Shader.SetGlobalFloat("_AOClampMax",_AOClampMax);
            Shader.SetGlobalFloat("_AOMix",_AOMix);

            Shader.SetGlobalFloat("debugGI", debugGI ? 1 : 0);
            Shader.SetGlobalFloat("debugGI2", debugGI2 ? 1 : 0);

            Shader.SetGlobalFloat("debugGI3",debugGI3?1:0);

            Shader.SetGlobalFloat("debugGI4",GIModeEnum==GIMode.BakeTex?1:0);


            //Shader.SetGlobalTexture("BakeL00", BakeL00);
            //Shader.SetGlobalTexture("BakeL10", BakeL10);
            //Shader.SetGlobalTexture("BakeL11", BakeL11);
            //Shader.SetGlobalTexture("BakeL12", BakeL12);
        }

        float GetFractionalPart(float value)
        {
            return value - Mathf.Floor(value);
        }


    }

}
