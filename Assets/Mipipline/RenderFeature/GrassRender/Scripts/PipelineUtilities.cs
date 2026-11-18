//Stylized Grass Shader
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Grass
{
    public static class PipelineUtilities
    {
        
        private const string renderDataListFieldName = "m_RendererDataList";
        
        /// <summary>
        /// Retrieves a ForwardRenderer asset in the project, based on name
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public static UniversalRendererData GetRenderer(string assetName)
        {
#if UNITY_EDITOR
            string[] GUIDs = AssetDatabase.FindAssets(assetName + " t:UniversalRendererData");

            if (GUIDs.Length == 0)
            {
                Debug.LogError("The <i>" + assetName + "</i> asset could not be found in the project. Was it renamed or not imported?");
                return null;
            }

            string assetPath = AssetDatabase.GUIDToAssetPath(GUIDs[0]);

            UniversalRendererData data = (UniversalRendererData)AssetDatabase.LoadAssetAtPath(assetPath, typeof(UniversalRendererData));

            return data;
#else
            Debug.LogError("StylizedGrass.PipelineUtilities.GetRenderer() cannot be called in a build, it requires AssetDatabase. References to renderers should be saved beforehand!");
            return null;
#endif
        }

		public static UniversalRenderPipelineAsset GetRenderPipelineAsset(string assetName)
		{
#if UNITY_EDITOR
			string[] GUIDs = AssetDatabase.FindAssets(assetName + " t:UniversalRenderPipelineAsset");

			if (GUIDs.Length == 0)
			{
				Debug.LogError("The <i>" + assetName + "</i> asset could not be found in the project. Was it renamed or not imported?");
				return null;
			}

			string assetPath = AssetDatabase.GUIDToAssetPath(GUIDs[0]);

			UniversalRenderPipelineAsset data = (UniversalRenderPipelineAsset)AssetDatabase.LoadAssetAtPath(assetPath, typeof(UniversalRenderPipelineAsset));

			return data;
#else
            Debug.LogError("StylizedGrass.PipelineUtilities.GetRenderer() cannot be called in a build, it requires AssetDatabase. References to renderers should be saved beforehand!");
            return null;
#endif
		}

		/// <summary>
		/// Checks if a ForwardRenderer has been assigned to the pipeline asset, if not it is added
		/// </summary>
		/// <param name="pass"></param>
		public static bool ValidatePipelineRenderers(ScriptableRendererData pass)
        {
            if (pass == null)
            {
                Debug.LogError("Pass is null");
                return false;
            }
            
            BindingFlags bindings = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            ScriptableRendererData[] m_rendererDataList = (ScriptableRendererData[])typeof(UniversalRenderPipelineAsset).GetField(renderDataListFieldName, bindings).GetValue(UniversalRenderPipeline.asset);
            bool isPresent = false;
            
            for (int i = 0; i < m_rendererDataList.Length; i++)
            {
                if (m_rendererDataList[i] == pass) isPresent = true;
            }

            return isPresent;
            //不给污染的机会。

            // if (!isPresent)
            // {
            //     AddRendererToPipeline(pass);
            // }
            // else
            // {
            //     //Debug.Log("The " + AssetName + " ScriptableRendererFeature is assigned to the pipeline asset");
            // }
        }
        
        private static void AddRendererToPipeline(ScriptableRendererData pass)
        {
            if (pass == null) return;

            BindingFlags bindings = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            ScriptableRendererData[] m_rendererDataList = (ScriptableRendererData[])typeof(UniversalRenderPipelineAsset).GetField(renderDataListFieldName, bindings).GetValue(UniversalRenderPipeline.asset);
            List<ScriptableRendererData> rendererDataList = new List<ScriptableRendererData>();

            for (int i = 0; i < m_rendererDataList.Length; i++)
            {
                rendererDataList.Add(m_rendererDataList[i]);
            }
            rendererDataList.Add(pass);

            typeof(UniversalRenderPipelineAsset).GetField(renderDataListFieldName, bindings).SetValue(UniversalRenderPipeline.asset, rendererDataList.ToArray());

            //Debug.Log("The <i>" + DrawGrassBenders.AssetName + "</i> renderer is required and was automatically added to the \"" + UniversalRenderPipeline.asset.name + "\" pipeline asset");
        }

        public static void RemoveRendererFromPipeline(ScriptableRendererData pass)
        {
            if (pass == null) return;
            
            BindingFlags bindings = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            ScriptableRendererData[] m_rendererDataList = (ScriptableRendererData[])typeof(UniversalRenderPipelineAsset).GetField(renderDataListFieldName, bindings).GetValue(UniversalRenderPipeline.asset);
            List<ScriptableRendererData> rendererDataList = new List<ScriptableRendererData>(m_rendererDataList);
            
            if(rendererDataList.Contains(pass)) rendererDataList.Remove((pass));
            
            typeof(UniversalRenderPipelineAsset).GetField(renderDataListFieldName, bindings).SetValue(UniversalRenderPipeline.asset, rendererDataList.ToArray());
        }

        public static void AssignRendererToCamera(UniversalAdditionalCameraData camData, ScriptableRendererData pass)
        {
            // if (UniversalRenderPipeline.asset)
            // {
            if (pass)
            {
                //list is internal, so perform reflection workaround
                ScriptableRendererData[] rendererDataList =
                    (ScriptableRendererData[]) typeof(UniversalRenderPipelineAsset).GetField(renderDataListFieldName,
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(UniversalRenderPipeline.asset);

                    for (int i = 0; i < rendererDataList.Length; i++)
                    {
                        if (rendererDataList[i] == pass)
							camData.SetRenderer(i);
                    }
                }
                // }
                // else
                // {
                //     Debug.LogError("[StylizedGrassRenderer] No Universal Render Pipeline is currently active.");
                // }
        }

        public static void SwitchFeatrue(bool active,string featureName)
        {
            UniversalAdditionalCameraData uacd = Camera.main.GetUniversalAdditionalCameraData();
            var feature = uacd.scriptableRenderer.GetRendererFeature(featureName);
            if (feature != null)
            {
                feature.SetActive(active);
            }
            else
            {
                Debug.LogError("Not Find Feature featureName = " + featureName);
            }
        }
    }

    public static class ScrptableRendererExtension
    {
        private static readonly Dictionary<ScriptableRenderer, Dictionary<string, ScriptableRendererFeature>> s_renderFeatures
            = new Dictionary<ScriptableRenderer, Dictionary<string, ScriptableRendererFeature>>();

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

                    for (int i = 0; i < rendererFeatures.Count; i++)
                    {
                        var featrue = rendererFeatures[i];
                        innerFeatures[featrue.name] = featrue;
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
    }
}
