//Stylized Grass Shader
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering.Universal;

namespace Grass
{
	public class RenderGrassBenders : ScriptableRendererFeature
	{
		[System.Serializable]
		public class RenderBendersSettings
		{
			public FilterSettings filterSettings = new FilterSettings();
			public Material overierMaterial;
		}

		[System.Serializable]
		public class FilterSettings
		{
			// TODO: expose opaque, transparent, all ranges as drop down
			public RenderQueueType RenderQueueType;
			public LayerMask LayerMask;
			public string[] PassNames;

			public FilterSettings()
			{
				RenderQueueType = RenderQueueType.Opaque;
				LayerMask = 0;
			}
		}

		public RenderBendersSettings settings = new RenderBendersSettings();
		public RenderGrassBendersPass renderObjectsPass;

		public override void Create()
		{
			FilterSettings filter = settings.filterSettings;
			renderObjectsPass = new RenderGrassBendersPass(RenderPassEvent.BeforeRenderingPrePasses - 1, filter.PassNames, filter.RenderQueueType, filter.LayerMask, settings.overierMaterial);

			// default open
			Shader.EnableKeyword(QSShaderKeyword.INTERACTIVEBLENDGRASS);
			// if (!isActive)
			// {
			// 	Shader.DisableKeyword(SYShaderKeyword.INTERACTIVEBLENDGRASS);
			// }
			// else
			// {
			// 	Shader.EnableKeyword(SYShaderKeyword.INTERACTIVEBLENDGRASS);
			// }

		}

		// Here you can inject one or multiple render passes in the renderer.
		// This method is called when setting up the renderer once per-camera.
		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			if (renderingData.cameraData.cameraType != CameraType.Game && renderingData.cameraData.cameraType != CameraType.SceneView)
			{
				return;
			}

			if (renderingData.cameraData.camera == null ||
			  renderingData.cameraData.renderType == CameraRenderType.Overlay)
			{
				return;
			}

			// var cameraColorTarget = renderer.cameraColorTarget;
            // renderObjectsPass.Setup(renderer.cameraColorTarget/*, renderer.cameraDepthTarget, RenderTargetHandle.CameraTarget*/);
            renderer.EnqueuePass(renderObjectsPass);
		}
	}
}