#if UNITY_EDITOR
using System.Collections;

using System.Collections.Generic;
using UnityEngine;

namespace SY.EffectChecker
{
    public class EffectCheckerConfig
    {
        //检测数据
        public static string maxMemorySize = "1.0 MB";
        public static string maxTextureCount = "5";
        public static string maxParticleSystemCount = "5";
        public static string maxDrawCall = "10";
        public static string maxParticleCount = "50";
        public static string maxAveragePixelRate = "6";
        public static string localReportDirKey = "localReportDirCacheKey";

        //默认指定路径
        public static string particlesTestPath = "Assets/SYPackages/ParticleEffectProfiler/Examples/EffectPrefab";
        public static string exportCSVPath = "Assets/TestData.csv";

        //测试模板
        public static string particleTestModel = ",贴图内存建议:{0},贴图数量建议:{1},组件数量建议:{2},最高DrawCall数建议:{3},最高粒子数建议:{4},单个平均每像素overDraw率建议:{5} , ,\n" +
        ",贴图内存平均值:{6},贴图数量平均值:{7},组件数量平均值:{8},最高DrawCall数平均值:{9},最高粒子数平均值:{10},总体平均overDraw率:{11},平均开启Prewarn的粒子预制数:{12},平均未开启Prewarn的粒子预制数:{13} \n" +
        "特效名,贴图内存(MB),贴图数量,特效中所有粒子系统组件数量,最高DrawCall数,最高粒子数,平均每像素overDraw率,开启Prewarn的粒子预制数,未开启的Prewarn的粒子预制数\n";
    }
}
#endif