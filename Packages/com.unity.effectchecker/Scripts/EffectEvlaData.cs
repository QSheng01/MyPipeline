#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 处理像素计算
/// </summary>
namespace SY.EffectChecker
{
    public class EffectEvlaData
    {
        public int pixDrawTimes = 0;               // 总调用的渲染次数
        public int pixTotal = 0;                  // n次后的理论上总渲染数
        public float pixActualDrawTotal = 0;        // n次后的实际上渲染次数
        public string quality;
        public int allpixel = 0;
        //获取指定区域内的所有像素
        public int GetPixDrawAverage()
        {
            if (pixDrawTimes == 0)
            {
                return 0;
            }
            return pixTotal / pixDrawTimes;
        }

        //获取指定区域内的实际每个像素的绘制总数
        public float GetPixActualDrawAverage()
        {
            if (pixDrawTimes == 0)
            {
                return 0;
            }
            return pixActualDrawTotal / pixDrawTimes;
        }

        //平均像素绘制次数
        public float GetGlobalPixRate()
        {
            if (allpixel == 0 || pixActualDrawTotal == 0)
            {
                return 0;
            }

            //实际总绘制次数 / 总像素点
            return GetPixActualDrawAverage() / allpixel / pixDrawTimes;
        }


        public float GetPixRate()
        {
            int pixDrawAverage = GetPixDrawAverage();
            if (pixDrawAverage == 0)
            {
                return 0;
            }
            //实际总绘制次数 / 总像素点
            return GetPixActualDrawAverage() / GetPixDrawAverage();
        }

    }
}
#endif