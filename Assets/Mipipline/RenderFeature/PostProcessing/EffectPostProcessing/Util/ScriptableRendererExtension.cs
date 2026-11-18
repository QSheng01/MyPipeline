using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EffectsPostProcess.Scripts.Util
{
    public static class ScriptableRendererExtension
    {
        private static readonly Dictionary<ScriptableRenderer, Dictionary<string, ScriptableRendererFeature>> s_renderFeatures = new Dictionary<ScriptableRenderer, Dictionary<string, ScriptableRendererFeature>>();

        public static ScriptableRendererFeature GetRendererFeature(this ScriptableRenderer renderer, string name)
        {
            if (!s_renderFeatures.TryGetValue(renderer, out var innerFeatures))
            {
                var propertyInfo = renderer.GetType().GetProperty("rendererFeatures", BindingFlags.Instance | BindingFlags.NonPublic);
                
                List<ScriptableRendererFeature> rendererFeatures = (List<ScriptableRendererFeature>)propertyInfo?.GetValue(renderer);
                
                if (rendererFeatures == null)
                {
                    s_renderFeatures[renderer] = null;
                }
                else
                {
                    innerFeatures = new Dictionary<string, ScriptableRendererFeature>();
                    for (var i = 0; i < rendererFeatures.Count; i++)
                    {
                        var feature = rendererFeatures[i];
                        innerFeatures[feature.name] = feature;
                    }
                    s_renderFeatures[renderer] = innerFeatures;
                }
            }
            
            if (innerFeatures != null)
            {
                innerFeatures.TryGetValue(name, out var result);
                return result;
            }
            
            return null;
        }
        
        public static bool ContainRendererFeature(this ScriptableRenderer renderer, string name)
        {
            if (!s_renderFeatures.TryGetValue(renderer, out var innerFeatures))
            {
                var propertyInfo = renderer.GetType().GetProperty("rendererFeatures", BindingFlags.Instance | BindingFlags.NonPublic);
                
                List<ScriptableRendererFeature> rendererFeatures = (List<ScriptableRendererFeature>)propertyInfo?.GetValue(renderer);
                
                if (rendererFeatures == null)
                {
                    s_renderFeatures[renderer] = null;
                }
                else
                {
                    innerFeatures = new Dictionary<string, ScriptableRendererFeature>();
                    for (var i = 0; i < rendererFeatures.Count; i++)
                    {
                        var feature = rendererFeatures[i];
                        innerFeatures[feature.name] = feature;
                    }
                    s_renderFeatures[renderer] = innerFeatures;
                }
            }
            
            if (innerFeatures != null)
            {
                return innerFeatures.TryGetValue(name, out var result);
            }
            
            return false;
        }

        // public static void GetAllRenderFeatures()
        // {
        //     TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom<ScriptableRendererFeature>();
        //     foreach (Type type in types)
        //     {
        //         Debug.Log(type);
        //     }
        // }
    }
}