#if UNITY_EDITOR
using System.Collections;

using System.Collections.Generic;
using UnityEngine;

namespace SY.EffectChecker
{
    public class EffectInfo
    {
        public List<EffectData> effectList;
    }

    public struct EffectData
    {
        /// <summary>
        /// 特效预制体名字
        /// </summary>
        public string effects_preform;

        /// <summary>
        /// 贴图内存
        /// </summary>
        public double chartlet_memory;

        /// <summary>
        /// 贴图数量
        /// </summary>
        public int no_repeat_chartlet;

        /// <summary>
        /// 特效中所有粒子系统组件数量
        /// </summary>
        public int particle_component;

        /// <summary>
        /// 最高DrawCall数
        /// </summary>
        public int most_drawcall;

        /// <summary>
        /// 最高粒子数
        /// </summary>
        public int most_particle;

        /// <summary>
        /// 平均每像素overDraw率
        /// </summary>
        public double average_overdraw_ratio;

        /// <summary>
        /// 最大平均每像素overDraw率
        /// </summary>
        public double most_overdraw_ratio;

        /// <summary>
        /// 开启Prewarn的粒子预制数
        /// </summary>
        public int open_preman_preform;

        /// <summary>
        /// 未开启的Prewarn的粒子预制数
        /// </summary>
        public int close_preman_preform;

        /// <summary>
        /// 预制体路径
        /// </summary>
        public string effects_preform_path;

        /// <summary>
        /// 超出10秒播放时间的特效
        /// </summary>
        public bool more_10sec_effects;

        /// <summary>
        /// 包含Color属性的网格
        /// </summary>
        public string color_grid;

        /// <summary>
        /// 包含uv2属性的网格
        /// </summary>
        public string uv2_grid;

        /// <summary>
        /// 包含uv3或uv4属性的网格
        /// </summary>
        public string uv3_or_uv4_grid;

        /// <summary>
        /// 播放面数最大值
        /// </summary>
        public int most_play;

        /// <summary>
        /// 特效原填充像素点平均值
        /// </summary>
        public double avg_effect_pixel_orgin_fill;

        /// <summary>
        /// 特效实际填充像素点平均值
        /// </summary>
        public double avg_effect_pixel_reality_fill;
    }
}

#endif