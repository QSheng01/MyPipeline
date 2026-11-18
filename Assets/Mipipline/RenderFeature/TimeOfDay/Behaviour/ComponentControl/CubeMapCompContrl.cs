using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
namespace SYPackages.TimeOfDay
{
    public class CubeMapCompContrl : TodCompContrl
    {
        public enum ReflectionType
        {
            AtmosphericReflection,
            EnvironmentReflection
        }

        public enum Resolution
        {
            Resolution_64,
            Resolution_128
        }

        public Resolution refectionResolution = Resolution.Resolution_64;
        public ReflectionType reflectionType = ReflectionType.AtmosphericReflection;
        [SerializeField]
        Cubemap RP0;
        [SerializeField]
        Cubemap RP1;

        public TimeOfDayManager todm;
        public ReflectionProbe Probe;
        public AnimationCurve CubeMapLerp;
        [ReadOnly]public float Weight;
        public Cubemap[] AllCubemaps;

        public void UpdateCubeMap()
        {
            float lerpValue = CubeMapLerp.Evaluate(todm.timeOfDay);
            Weight = GetFractionalPart(lerpValue);
            Shader.SetGlobalFloat("_TOD_RPWeight", Weight);

            int stage = Mathf.FloorToInt(lerpValue);
            stage = stage % 24;
            RP0 = AllCubemaps[stage];
            RP1 = AllCubemaps[(stage+1) % 24];

            // 大气雾用的cubeMap
            if (reflectionType == ReflectionType.AtmosphericReflection)
            {
                Shader.SetGlobalTexture("_TOD_ReflectionProbe_0", RP0);
                Shader.SetGlobalTexture("_TOD_ReflectionProbe_1", RP1);
            }

            // 环境用的cubeMap
            if (reflectionType == ReflectionType.EnvironmentReflection)
            {
                Shader.SetGlobalTexture("_TOD_ReflectionProbe_EV_0", RP0);
                Shader.SetGlobalTexture("_TOD_ReflectionProbe_EV_1", RP1);
                Shader.SetGlobalInt("_UseTODBlendRP", 1);
            }
        }

        void OnDisable()
        {
            Shader.SetGlobalInt("_UseTODBlendRP", 0); // 没有这个组件的时候 会关闭混合
        }

        public override void Play()
        {
            base.Play();
            UpdateCubeMap();
        }

        float GetFractionalPart(float value)
        {
            return value - Mathf.Floor(value);
        }

        public int StartTime = 0;

        [Button("切换到下一个时间段")]
        public void SetToNextProbe()
        {
            Probe.resolution = (int)refectionResolution;
            todm.timeOfDay = StartTime;
            Probe.customBakedTexture = AllCubemaps[StartTime++];

            StartTime = StartTime % 24;
        }
    }
}


