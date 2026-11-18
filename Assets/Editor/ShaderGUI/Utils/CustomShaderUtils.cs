using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CustomShaderEditor
{
    public static class CustomShaderUtils
    {
        public static void SetKeyword(this Material mat, string keyword, bool enable)
        {
            if (enable)
                mat.EnableKeyword(keyword);
            else
                mat.DisableKeyword(keyword);
        }


        public static bool Enable(this MaterialProperty prop)
        {
            return prop.floatValue >= 1;
        }


        public static bool IsHeaderShowInInspector(this MaterialEditor editor, string groupName)
        {
            var properties = MaterialEditor.GetMaterialProperties(editor.targets);

            bool show = true;
            if (!string.IsNullOrEmpty(groupName))
            {
                foreach (var property in properties)
                {
                    if (property.name == groupName && !property.Enable())
                    {
                        show = false;
                        break;
                    }
                }
            }

            return show;
        }

        public static bool IsShowInInspectorByKeyword(this MaterialEditor editor, List<string> enableKeywords)
        {
            bool show = false;

            if (enableKeywords == null || enableKeywords.Count == 0)
            {
                return true;
            }
            foreach (var sstr in enableKeywords)
            {
                if (!string.IsNullOrEmpty(sstr))
                {
                    var mat = editor.target as Material; 
                    Assert.IsNotNull(mat);
                    show = mat.IsKeywordEnabled(sstr);
                    if (show)
                    {
                        break;
                    }
                } 
            }
            
            return show;
        }


        public static List<string> enableKeywords = new List<string>();
        public static bool TrytoGetEnableKeyword(string str, out string groupname, ref List<string> enableKeywords)
        {
            string[] strs = str.Split(new char[] {'.'}, StringSplitOptions.RemoveEmptyEntries);
            bool success = false;
            if (enableKeywords == null)
            {
                enableKeywords = new List<string>();
            }
            else
            {
                enableKeywords.Clear();
            }
            groupname = null;
            if (strs != null)
            {
                groupname = strs[0];
                if (strs.Length > 1)
                {
                    for (var i = 1; i < strs.Length; ++i)
                    {
                        enableKeywords.Add(strs[i]);
                    }
                    success = true;
                }
            }
            return success;
        }


        internal class PerformanceChecks
        {
            static readonly string[] kShadersWithMobileVariants =
            {
                "VertexLit",
                "Diffuse",
                "Bumped Diffuse",
                "Bumped Specular",
                "Particles/Additive",
                "Particles/VertexLit Blended",
                "Particles/Alpha Blended",
                "Particles/Multiply",
                "RenderFX/Skybox"
            };

            private static bool IsMobileBuildTarget(BuildTarget target)
            {
                return target == BuildTarget.iOS || target == BuildTarget.Android || target == BuildTarget.Tizen;
            }

            private static string FormattedTextContent(string localeString, params object[] args)
            {
                // var content = EditorGUIUtility.TextContent(localeString);
                var content = localeString;
                return string.Format(content, args);
            }

            /// <summary>
            /// 性能检测，源码copy
            /// </summary>
            /// <param name="mat"></param>
            /// <param name="buildTarget"></param>
            /// <returns></returns>
            public static string CheckMaterial(Material mat, BuildTarget buildTarget)
            {
                if (mat == null || mat.shader == null)
                    return null;
                string shaderName = mat.shader.name;
                //int shaderLOD = ShaderUtil.GetLOD(mat.shader);
                bool hasMobileVariant = Array.Exists(kShadersWithMobileVariants, s => s == shaderName);
                bool isMobileTarget = IsMobileBuildTarget(buildTarget);

                // Skip all performance-related checks if shader explicitly indicated that via a PerformanceChecks=False tag.
                bool noPerfChecks = (mat.GetTag("PerformanceChecks", true).ToLower() == "false");
                if (!noPerfChecks)
                {
                    // shaders that have faster / simpler equivalents already
                    if (hasMobileVariant)
                    {
                        // has default white color?
                        if (isMobileTarget && mat.HasProperty("_Color") &&
                            mat.GetColor("_Color") == new Color(1.0f, 1.0f, 1.0f, 1.0f))
                        {
                            return FormattedTextContent(
                                "Shader is using white color which does nothing; Consider using {0} shader for performance.",
                                "Mobile/" + shaderName);
                        }

                        // recommend Mobile particle shaders on mobile platforms
                        if (isMobileTarget && shaderName.StartsWith("Particles/"))
                        {
                            return FormattedTextContent("Consider using {0} shader on this platform for performance.",
                                "Mobile/" + shaderName);
                        }

                        // has default skybox tint color?
                        if (shaderName == "RenderFX/Skybox" && mat.HasProperty("_Tint") &&
                            mat.GetColor("_Tint") == new Color(0.5f, 0.5f, 0.5f, 0.5f))
                        {
                            return FormattedTextContent(
                                "Skybox shader is using gray color which does nothing; Consider using {0} shader for performance.",
                                "Mobile/Skybox");
                        }
                    }

                    //// recommend "something simpler" for complex shaders on mobile platforms
                    //if (shaderLOD >= 300 && isMobileTarget && !shaderName.StartsWith("Mobile/"))
                    //{
                    //    return FormattedTextContent("Shader might be expensive on this platform. Consider switching to a simpler shader; look under Mobile shaders.");
                    //}

                    // vertex lit shader with max. emission: recommend Unlit shaders
                    if (shaderName.Contains("VertexLit") && mat.HasProperty("_Emission"))
                    {
                        bool isColor = false;

                        Shader shader = mat.shader;
                        int count = ShaderUtil.GetPropertyCount(shader);

                        for (int i = 0; i < count; i++)
                        {
                            if (ShaderUtil.GetPropertyName(shader, i) == "_Emission")
                            {
                                isColor = (ShaderUtil.GetPropertyType(shader, i) ==
                                           ShaderUtil.ShaderPropertyType.Color);
                                break;
                            }
                        }

                        if (isColor)
                        {
                            Color col = mat.GetColor("_Emission");
                            if (col.r >= 0.5f && col.g >= 0.5f && col.b >= 0.5f)
                            {
                                return FormattedTextContent(
                                    "Looks like you're using VertexLit shader to simulate an unlit object (white emissive). Use one of Unlit shaders instead for performance.");
                            }
                        }
                    }

                    // normalmapped shader without a normalmap: recommend non-normal mapped one
                    if (mat.HasProperty("_BumpMap") && mat.GetTexture("_BumpMap") == null)
                    {
                        return FormattedTextContent(
                            "Normal mapped shader without a normal map. Consider using a non-normal mapped shader for performance.");
                    }
                }

                return null;
            }
        }
    }
}