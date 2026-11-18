#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace SY.EffectChecker
{
    /// <summary>
    /// 批处理特效性能分析工具的管理类
    /// 将此类添加到特效上即可
    /// </summary>
    public class ParticleEffectScript : MonoBehaviour
    {

        public string category = "";
        public AnimationCurve ParticleCount = new AnimationCurve();
        public AnimationCurve DrawCall = new AnimationCurve();
        public AnimationCurve Overdraw = new AnimationCurve();
        //特效是否循环播放
        public bool enableLoop = false;
        [Range(1, 10)]
        public int effectRunTime = 3;

        EffectEvla m_EffectEvla;
        ParticleSystem[] m_ParticleSystems;
        ParticleSystemRenderer[] m_ParticleSystemRenderers;
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
        [HideInInspector]
        public string effectPrePath;
        [HideInInspector]
        public int maxTrisCount;
        [HideInInspector]
        public string globaloverDrawRateStr; //特效原填充像素点平均值

        private float totalPixRate;
        private float overDrawRateCount;
        private float overDrawRate;
        private float maxOverDrawRate;


        private float globaltotalPixRate;
        private float globaloverDrawRateCount;
        private float globaloverDrawRate;
        private float globalmaxOverDrawRate;

        private int parSysMaxParticleCount;

        private bool allStopped;
        private Dictionary<string, bool> prewarnDataDic = new Dictionary<string, bool>();
        [HideInInspector]
        public int openPrewarnCount;
        [HideInInspector]
        public int closePrewarnCount;
        private float totalDuration;
        private int PartcleLoopCount;

        public bool isForceEnd;
        private bool isExceedPlayTimeLimit;
        private bool isBegin;

        public Camera overDrawCamera;

        public float speedRatio;

        public bool isOpenEffectWin = false;

        void Awake()
        {
            GetParticleEffectData.InitParticleEffectData();
            Application.targetFrameRate = ParticleEffectCurve.FPS;
            m_CurveParticleCount = new ParticleEffectCurve();
            m_CurveDrawCallCount = new ParticleEffectCurve();
            m_CurveOverdraw = new ParticleEffectCurve();

            if (EffectPreChecker.Instance.overDrawCamera)
            {
                m_EffectEvla = new EffectEvla(EffectPreChecker.Instance.overDrawCamera);
                isOpenEffectWin = true;
            }
            else
            {
                m_EffectEvla = new EffectEvla(Camera.main);
            }

            isBegin = true;
        }

        void InitData()
        {
            openPrewarnCount = 0;
            closePrewarnCount = 0;
            totalDuration = 0;
            PartcleLoopCount = 0;
            totalPixRate = 0;
            overDrawRateCount = 0;
            globaltotalPixRate = 0;
            globalmaxOverDrawRate = 0;
            globaloverDrawRateCount = 0;
            isForceEnd = false;
            isExceedPlayTimeLimit = false;
            maxOverDrawRate = 0;
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
                if (ps.main.loop == true)
                {
                    PartcleLoopCount++;
                }
                var mainModule = ps.main;
                if (mainModule.duration > 10)
                {
                    isExceedPlayTimeLimit = true;
                }
                parSysMaxParticleCount += mainModule.maxParticles;
            }
            if (isExceedPlayTimeLimit || PartcleLoopCount > 0)
            {
                Time.timeScale = speedRatio;
            }
            else
            {
                Time.timeScale = 1;
            }
            Debug.Log("当前播放速率:" + Time.timeScale);
            SceneView.RepaintAll();
        }

        void Start()
        {
            m_ParticleSystems = GetComponentsInChildren<ParticleSystem>();
            m_ParticleSystemRenderers = GetComponentsInChildren<ParticleSystemRenderer>();
#if UNITY_2017 || UNITY_2019 || UNITY_2020
            m_CalculateEffectUIDataMethod = typeof(ParticleSystem).GetMethod("CalculateEffectUIData", BindingFlags.Instance | BindingFlags.NonPublic);
#else
        m_CalculateEffectUIDataMethod = typeof(ParticleSystem).GetMethod("CountSubEmitterParticles", BindingFlags.Instance | BindingFlags.NonPublic);
#endif
            if (isOpenEffectWin)
            {
                InitData();
            }

            StartCoroutine(ForceEnd());
        }

        IEnumerator ForceEnd()
        {
            yield return new WaitForSeconds(10f);
            isForceEnd = true;
            if (CheckString(textureCountStr))
            {
                textureCountStr = "0";
            }
            if (CheckString(memorySizeStr))
            {
                memorySizeStr = "0";
            }
            if (CheckString(particleSystemCountStr))
            {
                particleSystemCountStr = "0";
            }
            if (CheckString(particleEffecDrawCallStr))
            {
                particleEffecDrawCallStr = "0";
            }
            if (CheckString(overDrawRateStr))
            {
                overDrawRateStr = "0";
            }
            if (CheckString(globaloverDrawRateStr))
            {
                globaloverDrawRateStr = "0";
            }
        }

        private void LateUpdate()
        {
            RecordParticleCount();
            m_EffectEvla.Update();

            UpdateParticleCountCurve();
            UpdateDrawCallCurve();
            UpdateOverdrawCurve();

            if (isOpenEffectWin)
            {
                JudgeIsAllStop();
            }
        }

        private bool CheckEffectInfo()
        {
            bool isPass = true;
            if (CheckString(textureCountStr) || CheckString(memorySizeStr) || CheckString(particleSystemCountStr) || CheckString(particleEffecDrawCallStr) || CheckString(overDrawRateStr) || CheckString(globaloverDrawRateStr))
            {
                isPass = false;
            }

            return isPass;
        }

        private bool CheckString(string str)
        {
            return string.IsNullOrEmpty(str);
        }

        private void JudgeIsAllStop()
        {
            //需要等待初始化粒子激活后再判断是否存在有活动的粒子
            if (!isBegin)
            {
                allStopped = true;
            }
            foreach (ParticleSystem ps in m_ParticleSystems)
            {
                //if (!ps.isStopped)
                //{
                //    allStopped = false;
                //}
                ParticleSystem.Particle[] p_Data = new ParticleSystem.Particle[ps.particleCount];
                int count = ps.GetParticles(p_Data);
                if (!isBegin && count != 0)
                {
                    allStopped = false;
                }
                if (count != 0 && isBegin)
                {
                    isBegin = false;
                    GetParticleEffectData.InitParticleEffectData();
                }
            }

            if (!allStopped)
            {
                overDrawRate = float.Parse(GetParticleEffectData.GetPixRateStr(this)[1]);
                totalPixRate += overDrawRate;
                if (overDrawRate > maxOverDrawRate)
                {
                    maxOverDrawRate = overDrawRate;
                }
                if (overDrawRate != 0)
                {
                    overDrawRateCount += 1.0f;
                }
                globaloverDrawRate = float.Parse(GetParticleEffectData.GetGlobalPixRateStr(this)[1]);
                globaltotalPixRate += globaloverDrawRate;
                if (globaloverDrawRate > globalmaxOverDrawRate)
                {
                    globalmaxOverDrawRate = globaloverDrawRate;
                }
                if (globaloverDrawRate != 0)
                {
                    globaloverDrawRateCount += 1.0f;
                }
            }
            if (allStopped && CheckEffectInfo() || isForceEnd)
            {
                if (isForceEnd)
                {
                    Debug.Log(gameObject.name + "超出10秒播放时长限制,切换下一个特效");
                }
                foreach (string psName in prewarnDataDic.Keys)
                {
                    if (prewarnDataDic[psName] == true)
                    {
                        openPrewarnCount++;
                    }
                    else
                    {
                        closePrewarnCount++;
                    }
                }

                #region 特效检测数据
                ParticleEffectInfo particleEffectInfo = new ParticleEffectInfo();
                particleEffectInfo.name = gameObject.name.Replace("(Clone)", "");
                particleEffectInfo.category = category;
                particleEffectInfo.memorySize = float.Parse(memorySizeStr);
                particleEffectInfo.textureCount = int.Parse(textureCountStr);
                if (string.IsNullOrEmpty(textureCountStr))
                {
                    Debug.Log(particleEffectInfo.name + "特效数据遗失");
                }
                particleEffectInfo.particleSystemCount = int.Parse(particleSystemCountStr);
                particleEffectInfo.particleEffecDrawCall = int.Parse(particleEffecDrawCallStr);
                particleEffectInfo.maxParticleCount = m_MaxParticleCount;
                particleEffectInfo.openPrewarnCount = openPrewarnCount;
                particleEffectInfo.closePrewarnCount = closePrewarnCount;
                particleEffectInfo.effectPrePath = effectPrePath;
                particleEffectInfo.isExceedPlayTimeLimit = isExceedPlayTimeLimit;
                MeshData meshData = EffectPreChecker.Instance.GetMeshData(effectPrePath);
                particleEffectInfo.colorMeshs = meshData.colorMeshData;
                particleEffectInfo.uv2Meshs = meshData.uv2MeshData;
                particleEffectInfo.uv3OrUv4Meshs = meshData.uv3OrUv4Data;
                particleEffectInfo.maxTrisCount = maxTrisCount;
                particleEffectInfo.overDrawRate = float.Parse(overDrawRateStr);
                particleEffectInfo.maxOverDrawRate = maxOverDrawRate;
                particleEffectInfo.globalOverdraw = float.Parse(globaloverDrawRateStr);
                particleEffectInfo.maxGlobalOverDraw = globalmaxOverDrawRate;

                if (EffectPreChecker.Instance != null)
                {
                    EffectPreChecker.Instance.AddParticleEffectInfo(particleEffectInfo);
                }
                #endregion
                isAllStopped = true;
            }
        }

        public void RecordEffectData()
        {
            memorySizeStr = GetParticleEffectData.GetGetRuntimeMemorySizeStr(gameObject)[1];
            textureCountStr = GetParticleEffectData.GetGetRuntimeMemorySizeStr(gameObject)[3];
            particleSystemCountStr = GetParticleEffectData.GetParticleSystemCount(gameObject)[1];
            particleEffecDrawCallStr = GetParticleEffectData.GetOnlyParticleEffecDrawCallStr()[2];
            particleCountStr = GetParticleEffectData.GetParticleCountStr(this)[2];
            float avePixRate = overDrawRateCount != 0 ? totalPixRate / overDrawRateCount : 0;
            overDrawRateStr = avePixRate.ToString("0.00");

            float globalavePixRate = globaloverDrawRateCount != 0 ? globaltotalPixRate / globaloverDrawRateCount : 0;
            globaloverDrawRateStr = globalavePixRate.ToString("0.00");
            int unityStatisTris = GetParticleEffectData.GetMaxTriCount();
            maxTrisCount = unityStatisTris > maxTrisCount ? unityStatisTris : maxTrisCount;

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
                var particleEffectScript = instance.GetComponentsInChildren<ParticleEffectScript>(true);

                if (particleEffectScript.Length > 0)
                {
                    Debug.LogError("保存前请先删除ParticleEffectScript脚本！");
                }
            };
        }
    }
}
#endif