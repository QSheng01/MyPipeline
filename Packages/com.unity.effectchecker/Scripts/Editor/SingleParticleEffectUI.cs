#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace SY.EffectChecker
{
    /// <summary>
    /// 单个特效的性能数据显示到Scene
    /// </summary>
    [CustomEditor(typeof(SingleParticleEffectScript))]
    public class SingleParticleEffectUI : Editor
    {

        string[] m_Label = new string[20];

        void OnSceneGUI()
        {
            SingleParticleEffectScript particleEffectScript = (SingleParticleEffectScript)target;
            if (particleEffectScript == null)
            {
                return;
            }

            int index = 0;
            m_Label[index] = GetParticleEffectData.GetGetRuntimeMemorySizeStr(particleEffectScript.gameObject)[0];//计算贴图占用内存和数量
            m_Label[++index] = GetParticleEffectData.GetParticleSystemCount(particleEffectScript.gameObject)[0];//粒子系统组件数量

            if (EditorApplication.isPlaying)
            {
                m_Label[++index] = GetParticleEffectData.GetOnlyParticleEffecDrawCallStr()[0];//特效DrawCall数量
                m_Label[++index] = GetParticleEffectData.GetParticleCountStr(particleEffectScript)[0];//粒子数量
                m_Label[++index] = GetParticleEffectData.GetPixDrawAverageStr(particleEffectScript)[0];//填充像素点
                m_Label[++index] = GetParticleEffectData.GetPixActualDrawAverageStr(particleEffectScript)[0];//实际填充像素点
                m_Label[++index] = GetParticleEffectData.GetPixRateStr(particleEffectScript)[0];//overDraw率
                m_Label[++index] = string.Format("特效播放面数：<color=green>{0}</color>,最大值：{1}", GetParticleEffectData.GetEffectTris(), GetParticleEffectData.GetMaxTriCount());//三角面数
            }

            ShowUI();
        }

        void ShowUI()
        {
            //开始绘制GUI
            Handles.BeginGUI();

            //规定GUI显示区域
            GUILayout.BeginArea(new Rect(10, 10, 300, 300));
            GUIStyle style = new GUIStyle();
            style.richText = true;
            style.fontStyle = FontStyle.Bold;

            for (int i = 0; i < m_Label.Length; i++)
            {
                if (!string.IsNullOrEmpty(m_Label[i]))
                {
                    GUILayout.Label(m_Label[i], style);
                }
            }
            GUILayout.EndArea();

            Handles.EndGUI();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            SingleParticleEffectScript particleEffectScript = (SingleParticleEffectScript)target;

            string autoCullingTips = GetParticleEffectData.GetCullingSupportedString(particleEffectScript.gameObject);
            if (!string.IsNullOrEmpty(autoCullingTips))
            {
                GUILayout.Label("ParticleSystem以下选项会导致无法自动剔除：", EditorStyles.whiteLargeLabel);
                GUILayout.Label(autoCullingTips);
            }
        }
    }
}
#endif