#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 处理特效整体相关的数据
/// </summary>
namespace SY.EffectChecker
{
    public class GetParticleEffectData
    {

        static int m_MaxDrawCall = 0;
        static int m_MaxTriCount = 0;

        public static void InitParticleEffectData()
        {
            m_MaxDrawCall = 0;
            m_MaxTriCount = 0;
        }

        static List<Texture> GetAllTexturesInMaterial(Material material)
        {

            List<Texture> textures = new List<Texture>();
            Shader shader = material.shader;
            for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    string propertyName = ShaderUtil.GetPropertyName(shader, i);
                    Texture propertyTexture = material.GetTexture(propertyName);
                    if (propertyTexture != Texture2D.blackTexture && propertyTexture != Texture2D.whiteTexture)
                    {
                        textures.Add(propertyTexture);
                    }
                }
            }
            return textures;
        }

        public static long GetRuntimeMemorySize(GameObject go, out int textureCount)
        {
            var textures = new List<Texture>();
            textureCount = 0;
            long sumSize = 0;

            var meshRendererlist = go.GetComponentsInChildren<ParticleSystemRenderer>(true);
            List<string> texPathLst = new List<string>();

            foreach (ParticleSystemRenderer item in meshRendererlist)
            {
                if (item.sharedMaterial)
                {
                    List<Texture> texturesInThisMat = GetAllTexturesInMaterial(item.sharedMaterial);
                    foreach (Texture texture in texturesInThisMat)
                    {
                        string path = AssetDatabase.GetAssetPath(texture);
                        if (texture && !textures.Contains(texture) && !texPathLst.Contains(path))
                        {
                            texPathLst.Add(path);
                            textures.Add(texture);
                            textureCount++;
                            sumSize = sumSize + GetStorageMemorySize(texture);
                        }
                    }
                    texturesInThisMat.Clear();
                }
            }
            return sumSize;
        }

        private static int GetStorageMemorySize(Texture texture)
        {
            return (int)InvokeInternalAPI("UnityEditor.TextureUtil", "GetStorageMemorySize", texture);
        }

        private static object InvokeInternalAPI(string type, string method, params object[] parameters)
        {
            Assembly assembly = typeof(AssetDatabase).Assembly;
            Type custom = assembly.GetType(type);
            MethodInfo methodInfo = custom.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
            return methodInfo != null ? methodInfo.Invoke(null, parameters) : 0;
        }

        /// <summary>
        /// 1、计算贴图占用内存和数量
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        public static string[] GetGetRuntimeMemorySizeStr(GameObject go)
        {
            int maxTextureCount = 5;
            int maxMemorySize = 1000 * 1024;
            int textureCount;
            string[] result = new string[20];
            long memorySize = GetRuntimeMemorySize(go, out textureCount);
            string memorySizeStr = EditorUtility.FormatBytes(memorySize);
            if (memorySizeStr.Substring(memorySizeStr.Length - 2) == "MB")
            {
                result[1] = memorySizeStr.Remove(memorySizeStr.Length - 2, 2);
            }
            else if (memorySizeStr.Substring(memorySizeStr.Length - 2) == "KB")
            {
                string[] str = memorySizeStr.Split(' ');
                result[1] = (Convert.ToSingle(str[0]) / 1024).ToString().Substring(0, 3);
            }
            string maxMemorySizeStr = EditorUtility.FormatBytes(maxMemorySize);

            if (maxMemorySize > memorySize)
                memorySizeStr = string.Format("<color=green>{0}</color>", memorySizeStr);
            else
                memorySizeStr = string.Format("<color=red>{0}</color>", memorySizeStr);

            result[0] = string.Format("贴图所占用的内存：{0}   建议：<{1}\n贴图数量：{2}     建议：<{3}", memorySizeStr, maxMemorySizeStr, FormatColorMax(textureCount, maxTextureCount), maxTextureCount);

            result[2] = maxMemorySizeStr;
            result[3] = textureCount.ToString();
            result[4] = maxTextureCount.ToString();
            return result;
        }
        /// <summary>
        /// 2、粒子系统组件数量
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        public static string[] GetParticleSystemCount(GameObject go)
        {
            int max = 5;
            string[] result = new string[20];
            var particleSystems = go.GetComponentsInChildren<ParticleSystem>(true);

            result[0] = string.Format("特效中所有粒子系统组件数量：{0}     建议：<{1}", FormatColorMax(particleSystems.Length, max), max);
            result[1] = particleSystems.Length.ToString();
            result[2] = max.ToString();
            return result;
        }

        public static int GetOnlyParticleEffecDrawCall()
        {
            //因为Camera 实际上渲染了两次，一次用作取样，一次用作显示。 这里给出了详细的说明：https://networm.me/2019/07/28/unity-particle-effect-profiler/#drawcall-%E6%95%B0%E5%80%BC%E4%B8%BA%E4%BB%80%E4%B9%88%E6%AF%94%E5%AE%9E%E9%99%85%E5%A4%A7-2-%E5%80%8D
            //mainCamera 和 overDrawCamera各采样一次，mainCamera渲染一次用作显示,共三次
            int drawCall = UnityStats.batches / 3;
            if (m_MaxDrawCall < drawCall)
            {
                m_MaxDrawCall = drawCall;
            }
            return drawCall;
        }

        public static int GetEffectTris()
        {
            //mainCamera 和 overDrawCamera各采样一次，mainCamera渲染一次用作显示,共三次
            int triCount = UnityStats.triangles / 3;
            if (m_MaxTriCount < triCount)
            {
                m_MaxTriCount = triCount;
            }
            return triCount;
        }

        /// <summary>
        /// 3、特效DrawCall数量
        /// </summary>
        /// <returns></returns>
        public static string[] GetOnlyParticleEffecDrawCallStr()
        {
            int max = 10;
            string[] result = new string[20];

            result[0] = string.Format("DrawCall: {0}   最高：{1}   建议：<{2}", FormatColorMax(GetOnlyParticleEffecDrawCall(), max), FormatColorMax(m_MaxDrawCall, max), max);
            result[1] = GetOnlyParticleEffecDrawCall().ToString();
            result[2] = m_MaxDrawCall.ToString();
            result[3] = max.ToString();
            return result;
        }

        public static int GetMaxTriCount()
        {
            return m_MaxTriCount;
        }

        /// <summary>
        /// 5、原填充像素点
        /// </summary>
        /// <param name="particleEffectGo"></param>
        /// <returns></returns>
        public static string[] GetPixDrawAverageStr(ParticleEffectScript particleEffectGo)
        {
            //index = 0：默认按高品质的算，这里你可以根本你们项目的品质进行修改。
            string[] result = new string[2];
            EffectEvlaData[] effectEvlaData = particleEffectGo.GetEffectEvlaData();
            int pixDrawAverage = effectEvlaData[0].GetPixDrawAverage();

            result[0] = string.Format("特效原填充像素点：{0}", FormatColorValue(pixDrawAverage));
            result[1] = pixDrawAverage.ToString();
            return result;
        }

        public static string[] GetPixDrawAverageStr(SingleParticleEffectScript particleEffectGo)
        {
            //index = 0：默认按高品质的算，这里你可以根本你们项目的品质进行修改。
            string[] result = new string[2];
            EffectEvlaData[] effectEvlaData = particleEffectGo.GetEffectEvlaData();
            int pixDrawAverage = effectEvlaData[0].GetPixDrawAverage();

            result[0] = string.Format("特效原填充像素点：{0}", FormatColorValue(pixDrawAverage));
            result[1] = pixDrawAverage.ToString();
            return result;
        }

        /// <summary>
        /// 6、实际填充像素点
        /// </summary>
        /// <param name="particleEffectGo"></param>
        /// <returns></returns>
        public static string[] GetPixActualDrawAverageStr(ParticleEffectScript particleEffectGo)
        {
            string[] result = new string[2];
            EffectEvlaData[] effectEvlaData = particleEffectGo.GetEffectEvlaData();
            float pixActualDrawAverage = effectEvlaData[0].GetPixActualDrawAverage();

            result[0] = string.Format("特效实际填充像素点：{0}", FormatColorValue(pixActualDrawAverage));
            result[1] = pixActualDrawAverage.ToString();
            return result;
        }

        public static string[] GetPixActualDrawAverageStr(SingleParticleEffectScript particleEffectGo)
        {
            string[] result = new string[2];
            EffectEvlaData[] effectEvlaData = particleEffectGo.GetEffectEvlaData();
            float pixActualDrawAverage = effectEvlaData[0].GetPixActualDrawAverage();

            result[0] = string.Format("特效实际填充像素点：{0}", FormatColorValue(pixActualDrawAverage));
            result[1] = pixActualDrawAverage.ToString();
            return result;
        }

        /// <summary>
        /// 7、overDraw率
        /// </summary>
        /// <param name="particleEffectGo"></param>
        /// <returns></returns>
        public static string[] GetPixRateStr(ParticleEffectScript particleEffectGo)
        {
            int max = 6;
            string[] result = new string[3];
            EffectEvlaData[] effectEvlaData = particleEffectGo.GetEffectEvlaData();
            float pixRate = effectEvlaData[0].GetPixRate();
            result[0] = string.Format("平均每像素overdraw率：{0}   建议：<{1}", FormatColorMax(pixRate, max), max);
            result[1] = pixRate.ToString();
            result[2] = max.ToString();
            return result;
        }

        public static string[] GetGlobalPixRateStr(ParticleEffectScript particleEffectGo)
        {
            int max = 6;
            string[] result = new string[3];
            EffectEvlaData[] effectEvlaData = particleEffectGo.GetEffectEvlaData();
            float pixRate = effectEvlaData[0].GetGlobalPixRate();
            result[0] = string.Format("平均每像素overdraw率：{0}   建议：<{1}", FormatColorMax(pixRate, max), max);
            result[1] = pixRate.ToString();
            result[2] = max.ToString();
            return result;
        }



        public static string[] GetPixRateStr(SingleParticleEffectScript particleEffectGo)
        {
            int max = 6;
            string[] result = new string[3];
            EffectEvlaData[] effectEvlaData = particleEffectGo.GetEffectEvlaData();
            float pixRate = effectEvlaData[0].GetPixRate();
            result[0] = string.Format("平均每像素overdraw率：{0}   建议：<{1}", FormatColorMax(pixRate, max), max);
            result[1] = pixRate.ToString();
            result[2] = max.ToString();
            return result;
        }

        /// <summary>
        /// 4、粒子数量
        /// </summary>
        /// <param name="particleEffectGo"></param>
        /// <returns></returns>
        public static string[] GetParticleCountStr(ParticleEffectScript particleEffectGo)
        {
            int max = 50;
            string[] result = new string[4];

            result[0] = string.Format("粒子数量：{0}   最高：{1}   建议：<{2}", FormatColorMax(particleEffectGo.GetParticleCount(), max), FormatColorMax(particleEffectGo.GetMaxParticleCount(), max), max);
            result[1] = particleEffectGo.GetParticleCount().ToString();
            result[2] = particleEffectGo.GetMaxParticleCount().ToString();
            result[3] = max.ToString();
            return result;
        }

        public static string[] GetParticleCountStr(SingleParticleEffectScript particleEffectGo)
        {
            int max = 50;
            string[] result = new string[4];

            result[0] = string.Format("粒子数量：{0}   最高：{1}   建议：<{2}", FormatColorMax(particleEffectGo.GetParticleCount(), max), FormatColorMax(particleEffectGo.GetMaxParticleCount(), max), max);
            result[1] = particleEffectGo.GetParticleCount().ToString();
            result[2] = particleEffectGo.GetMaxParticleCount().ToString();
            result[3] = max.ToString();
            return result;
        }

        public static string GetCullingSupportedString(GameObject go)
        {
            var particleSystems = go.GetComponentsInChildren<ParticleSystem>(true);
            string text = "";
            foreach (ParticleSystem item in particleSystems)
            {
                string str = CheckCulling(item);
                if (!string.IsNullOrEmpty(str))
                {
                    text += item.gameObject.name + ":" + str + "\n\n";
                }
            }
            return text;
        }

        static string CheckCulling(ParticleSystem particleSystem)
        {
            string text = "";
            if (particleSystem.collision.enabled)
            {
                text += "\n勾选了 Collision";
            }

            if (particleSystem.emission.enabled)
            {
                if (particleSystem.emission.rateOverDistance.curveMultiplier != 0)
                {
                    text += "\nEmission使用了Current(非线性运算)";
                }
            }

            if (particleSystem.externalForces.enabled)
            {
                text += "\n勾选了 External Forces";
            }

            if (particleSystem.forceOverLifetime.enabled)
            {
                if (GetIsRandomized(particleSystem.forceOverLifetime.x)
                    || GetIsRandomized(particleSystem.forceOverLifetime.y)
                    || GetIsRandomized(particleSystem.forceOverLifetime.z)
                    || particleSystem.forceOverLifetime.randomized)
                {
                    text += "\nForce Over Lifetime使用了Current(非线性运算)";
                }
            }
            if (particleSystem.inheritVelocity.enabled)
            {
                if (GetIsRandomized(particleSystem.inheritVelocity.curve))
                {
                    text += "\nInherit Velocity使用了Current(非线性运算)";
                }
            }
            if (particleSystem.noise.enabled)
            {
                text += "\n勾选了 Noise";
            }
            if (particleSystem.rotationBySpeed.enabled)
            {
                text += "\n勾选了 Rotation By Speed";
            }
            if (particleSystem.rotationOverLifetime.enabled)
            {
                if (GetIsRandomized(particleSystem.rotationOverLifetime.x)
                    || GetIsRandomized(particleSystem.rotationOverLifetime.y)
                    || GetIsRandomized(particleSystem.rotationOverLifetime.z))
                {
                    text += "\nRotation Over Lifetime使用了Current(非线性运算)";
                }
            }
            if (particleSystem.shape.enabled)
            {
                ParticleSystemShapeType shapeType = (ParticleSystemShapeType)particleSystem.shape.shapeType;
                switch (shapeType)
                {
                    case ParticleSystemShapeType.Cone:
                    case ParticleSystemShapeType.ConeVolume:
#if UNITY_2017_1_OR_NEWER
                    case ParticleSystemShapeType.Donut:
#endif
                    case ParticleSystemShapeType.Circle:
                        if (particleSystem.shape.arcMode != ParticleSystemShapeMultiModeValue.Random)
                        {
                            text += "\nShape的Circle-Arc使用了Random模式";
                        }
                        break;
                    case ParticleSystemShapeType.SingleSidedEdge:
                        if (particleSystem.shape.radiusMode != ParticleSystemShapeMultiModeValue.Random)
                        {
                            text += "\nShape的Edge-Radius使用了Random模式";
                        }
                        break;
                    default:
                        break;
                }
            }
            if (particleSystem.subEmitters.enabled)
            {
                text += "\n勾选了 SubEmitters";
            }
            if (particleSystem.trails.enabled)
            {
                text += "\n勾选了 Trails";
            }
            if (particleSystem.trigger.enabled)
            {
                text += "\n勾选了 Trigger";
            }
            if (particleSystem.velocityOverLifetime.enabled)
            {
                if (GetIsRandomized(particleSystem.velocityOverLifetime.x)
                    || GetIsRandomized(particleSystem.velocityOverLifetime.y)
                    || GetIsRandomized(particleSystem.velocityOverLifetime.z))
                {
                    text += "\nVelocity Over Lifetime使用了Current(非线性运算)";
                }
            }
            if (particleSystem.limitVelocityOverLifetime.enabled)
            {
                text += "\n勾选了 Limit Velocity Over Lifetime";
            }
            if (particleSystem.main.simulationSpace != ParticleSystemSimulationSpace.Local)
            {
                text += "\nSimulationSpace 不等于 Local";
            }
            if (particleSystem.main.gravityModifierMultiplier != 0)
            {
                text += "\nGravityModifier 不等于0";
            }
            return text;
        }

        static bool GetIsRandomized(ParticleSystem.MinMaxCurve minMaxCurve)
        {
            bool flag = AnimationCurveSupportsProcedural(minMaxCurve.curveMax);

            bool result;
            if (minMaxCurve.mode != ParticleSystemCurveMode.TwoCurves && minMaxCurve.mode != ParticleSystemCurveMode.TwoConstants)
            {
                result = flag;
            }
            else
            {
                bool flag2 = AnimationCurveSupportsProcedural(minMaxCurve.curveMin);
                result = (flag && flag2);
            }

            return result;
        }

        static bool AnimationCurveSupportsProcedural(AnimationCurve curve)
        {
            //switch (AnimationUtility.IsValidPolynomialCurve(curve)) //保护级别，无法访问，靠
            //{
            //    case AnimationUtility.PolynomialValid.Valid:
            //        return true;
            //    case AnimationUtility.PolynomialValid.InvalidPreWrapMode:
            //        break;
            //    case AnimationUtility.PolynomialValid.InvalidPostWrapMode:
            //        break;
            //    case AnimationUtility.PolynomialValid.TooManySegments:
            //        break;
            //}
            return false; //只能默认返回false了
        }

        static string FormatColorValue(float value)
        {
            return string.Format("<color=green>{0}</color>", value);
        }

        static string FormatColorMax(float value, float max)
        {
            if (max > value)
                return string.Format("<color=green>{0}</color>", value);
            else
                return string.Format("<color=red>{0}</color>", value);
        }
    }
}
#endif