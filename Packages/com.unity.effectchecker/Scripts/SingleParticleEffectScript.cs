#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SY.EffectChecker
{
    /// <summary>
    /// 单个特效性能分析工具的管理类
    /// 将此类添加到特效上即可
    /// </summary>
    public class SingleParticleEffectScript : MonoBehaviour
    {

        public AnimationCurve ParticleCount = new AnimationCurve();
        public AnimationCurve DrawCall = new AnimationCurve();
        public AnimationCurve Overdraw = new AnimationCurve();
        //特效是否循环播放
        public bool enableLoop = false;
        [Range(1, 10)]
        public int effectRunTime = 3;

        EffectEvla m_EffectEvla;
        ParticleSystem[] m_ParticleSystems;
        MethodInfo m_CalculateEffectUIDataMethod;
        public int m_ParticleCount = 0;
        public int m_MaxParticleCount = 0;

        ParticleEffectCurve m_CurveParticleCount;
        ParticleEffectCurve m_CurveDrawCallCount;
        ParticleEffectCurve m_CurveOverdraw;
        [HideInInspector]
        public bool isAllStopped = false;
        [HideInInspector]
        public string memorySizeStr;//贴图占用内存
        [HideInInspector]
        public string textureCountStr;//贴图数量
        [HideInInspector]
        public string particleSystemCountStr;//粒子系统组件数量
        [HideInInspector]
        public string particleEffecDrawCallStr;//DrawCall数量
        [HideInInspector]
        public string particleCountStr;//粒子数量
        [HideInInspector]
        public string overDrawRateStr;//OverDraw率
        private float overDrawRate;

        private int parSysMaxParticleCount;

        string effectTestData;

        private bool allStopped;
        private Dictionary<string, bool> prewarnDataDic = new Dictionary<string, bool>();
        private float totalDuration;

        void Awake()
        {
            GetParticleEffectData.InitParticleEffectData();
            Application.targetFrameRate = ParticleEffectCurve.FPS;
            m_CurveParticleCount = new ParticleEffectCurve();
            m_CurveDrawCallCount = new ParticleEffectCurve();
            m_CurveOverdraw = new ParticleEffectCurve();
            m_EffectEvla = new EffectEvla(Camera.main);
        }

        void InitData()
        {
            totalDuration = 0;
            m_MaxParticleCount = 0;
            parSysMaxParticleCount = 0;
            prewarnDataDic.Clear();
            foreach (ParticleSystem ps in m_ParticleSystems)
            {
                if (!prewarnDataDic.ContainsKey(ps.name))
                {
                    prewarnDataDic.Add(ps.name, false);
                }
                prewarnDataDic[ps.name] = ps.main.prewarm;
                totalDuration += ps.main.duration;
                parSysMaxParticleCount += ps.main.maxParticles;
            }
        }

        void Start()
        {
            m_ParticleSystems = GetComponentsInChildren<ParticleSystem>();
            m_CalculateEffectUIDataMethod = typeof(ParticleSystem).GetMethod("CalculateEffectUIData", BindingFlags.Instance | BindingFlags.NonPublic);
            InitData();
        }


        //private void LateUpdate()
        //{
        //    m_EffectEvla.Update();

        //    UpdateParticleCountCurve();
        //    UpdateDrawCallCurve();
        //    UpdateOverdrawCurve();

        //    memorySizeStr = GetParticleEffectData.GetGetRuntimeMemorySizeStr(gameObject)[1];
        //    textureCountStr = GetParticleEffectData.GetGetRuntimeMemorySizeStr(gameObject)[3];
        //    particleSystemCountStr = GetParticleEffectData.GetParticleSystemCount(gameObject)[1];
        //    particleEffecDrawCallStr = GetParticleEffectData.GetOnlyParticleEffecDrawCallStr()[2];
        //    particleCountStr = GetParticleEffectData.GetParticleCountStr(this)[2];
        //    overDrawRate = float.Parse(GetParticleEffectData.GetPixRateStr(this)[1]);
        //}

        private void Update()
        {
            RecordParticleCount();
        }

        public string GetEffectTestData()
        {
            return effectTestData;
        }

        public EffectEvlaData[] GetEffectEvlaData()
        {
            return m_EffectEvla.GetEffectEvlaData();
        }

        public void RecordParticleCount()
        {
            m_ParticleCount = 0;
            foreach (var ps in m_ParticleSystems)
            {
                int particleCount = 0;
                object[] invokeArgs = new object[] { particleCount, 0.0f, Mathf.Infinity };
                m_CalculateEffectUIDataMethod.Invoke(ps, invokeArgs);
                particleCount = (int)invokeArgs[0];
                m_ParticleCount += particleCount;
            }
            if (m_MaxParticleCount < m_ParticleCount)
            {
                m_MaxParticleCount = m_ParticleCount < parSysMaxParticleCount ? m_ParticleCount : parSysMaxParticleCount;
            }
        }

        public int GetParticleCount()
        {
            return m_ParticleCount;
        }
        public int GetMaxParticleCount()
        {
            return m_MaxParticleCount;
        }

        void UpdateParticleCountCurve()
        {
            ParticleCount = m_CurveParticleCount.UpdateAnimationCurve(m_ParticleCount, enableLoop, effectRunTime);
        }
        void UpdateDrawCallCurve()
        {
            DrawCall = m_CurveDrawCallCount.UpdateAnimationCurve(GetParticleEffectData.GetOnlyParticleEffecDrawCall(), enableLoop, effectRunTime);
        }

        void UpdateOverdrawCurve()
        {
            EffectEvlaData[] effectEvlaData = this.GetEffectEvlaData();
            Overdraw = m_CurveOverdraw.UpdateAnimationCurve((int)effectEvlaData[0].GetPixRate(), enableLoop, effectRunTime);
        }

        //监听apply事件
        [InitializeOnLoadMethod]
        static void StartInitializeOnLoadMethod()
        {
            PrefabUtility.prefabInstanceUpdated = delegate (GameObject instance)
            {
                var particleEffectScript = instance.GetComponentsInChildren<SingleParticleEffectScript>(true);

                if (particleEffectScript.Length > 0)
                {
                    Debug.LogError("保存前请先删除ParticleEffectScript脚本！");
                }
            };
        }
    }
}
#endif