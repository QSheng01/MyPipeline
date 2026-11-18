#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SY.EffectChecker
{
    public class ParticleEffectInfo
    {
        /// <summary>
        /// 特效预制体名字
        /// </summary>
        public string name;

        /// <summary>
        /// 特效预制体所属的类别
        /// </summary>
        public string category;

        /// <summary>
        /// 贴图内存
        /// </summary>
        public float memorySize;

        /// <summary>
        /// 贴图数量
        /// </summary>
        public int textureCount;

        /// <summary>
        /// 特效中所有粒子系统组件数量
        /// </summary>
        public int particleSystemCount;

        /// <summary>
        /// 最高DrawCall数
        /// </summary>
        public int particleEffecDrawCall;

        /// <summary>
        /// 最高粒子数
        /// </summary>
        public int maxParticleCount;

        /// <summary>
        /// 平均每像素overDraw率
        /// </summary>
        public float overDrawRate;

        /// <summary>
        /// 最大平均每像素overDraw率
        /// </summary>
        public float maxOverDrawRate;

        /// <summary>
        /// 开启Prewarn的粒子预制数
        /// </summary>
        public int openPrewarnCount;

        /// <summary>
        /// 未开启的Prewarn的粒子预制数
        /// </summary>
        public int closePrewarnCount;

        /// <summary>
        /// 预制体路径
        /// </summary>
        public string effectPrePath;

        /// <summary>
        /// 超出10秒播放时间的特效
        /// </summary>
        public bool isExceedPlayTimeLimit;

        /// <summary>
        /// 包含Color属性的网格
        /// </summary>
        public string colorMeshs;

        /// <summary>
        /// 包含uv2属性的网格
        /// </summary>
        public string uv2Meshs;

        /// <summary>
        /// 包含uv3或uv4属性的网格
        /// </summary>
        public string uv3OrUv4Meshs;

        /// <summary>
        /// 播放面数最大值
        /// </summary>
        public int maxTrisCount;

        /// <summary>
        /// 特效原填充像素点平均值
        /// </summary>
        public float globalOverdraw;

        /// <summary>
        /// 特效实际填充像素点
        /// </summary>
        public float maxGlobalOverDraw;
    }
}
#endif