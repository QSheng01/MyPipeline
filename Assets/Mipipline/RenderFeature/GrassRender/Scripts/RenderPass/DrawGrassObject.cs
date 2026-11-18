
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DrawGrassObject : ScriptableRendererFeature
{
	public enum DebugMode
	{
		_DEBUG_THROUGH,
		_DEBUG_GI,
		_DEBUG_GI_DIFFUSE,
		_DEBUG_GI_IBL,
		_DEBUG_ALBEDO,
		_DEBUG_DIFFUSE,
		_DEBUG_SPECULAR,
		_DEBUG_NORMAL,
		_DEBUG_SMOOTHNESS,
		_DEBUG_METALLIC,
		_DEBUG_AO,
		_DEBUG_DIFFUSE_RADIANCE,
		_DEBUG_SPECULAR_RADIANCE,
		_DEBUG_EMISSION,
        _DEBUG_SHADOWMASK,
		_DEBUG_ENCODED_LIGHTMAP,
		_DEBUG_SHADOWMAP

	}
	[System.Serializable]
	public class FilterSettings
	{
		// TODO: expose opaque, transparent, all ranges as drop down
		public RenderQueueType RenderQueueType;
		public LayerMask LayerMask;

		public FilterSettings()
		{
			RenderQueueType = RenderQueueType.Opaque;
            LayerMask = 0;
          
         
		}
	}

	[System.Serializable]
	public class RenderObjectsSettings
	{
		public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;
		public FilterSettings filterSettings = new FilterSettings();

		public DebugMode debugMode;
	}

	public RenderObjectsSettings settings = new RenderObjectsSettings();
	DrawGrassObjectPass m_ScriptablePass;

	public override void Create()
	{

//#if UNITY_EDITOR
//        GrassTool.CheckLayer(TerrainPainterStringUtility.rayCastLayer);
//#endif
        FilterSettings filter = settings.filterSettings;
		int defaultLayer = 1 << LayerMask.NameToLayer("Default");
		int terrainLayer = 1 << LayerMask.NameToLayer(QSConstant.rayCastLayer);
		filter.LayerMask = defaultLayer | terrainLayer;

		m_ScriptablePass = new DrawGrassObjectPass(filter.RenderQueueType, filter.LayerMask);
		m_ScriptablePass.debugMode = settings.debugMode;

		m_ScriptablePass.renderPassEvent = settings.Event;
		
	}

	// Here you can inject one or multiple render passes in the renderer.
	// This method is called when setting up the renderer once per-camera.
	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		renderer.EnqueuePass(m_ScriptablePass);
	}

	class DrawGrassObjectPass : ScriptableRenderPass
	{
		FilteringSettings m_FilteringSettings;
		public DebugMode debugMode;
		List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

		public DrawGrassObjectPass(RenderQueueType renderQueueType, int layerMask)
		{
			RenderQueueRange renderQueueRange = (renderQueueType == RenderQueueType.Transparent)
			  ? RenderQueueRange.transparent
			  : RenderQueueRange.opaque;

			m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);

			m_ShaderTagIdList.Add(new ShaderTagId("UniversalForward"));
			m_ShaderTagIdList.Add(new ShaderTagId("LightweightForward"));
			m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
		}

		// Here you can implement the rendering logic.
		// Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
		// https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
		// You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, SortingCriteria.RenderQueue);

			var cmd = CommandBufferPool.Get("Draw LightMap And ShaderMask Data");
		

			cmd.EnableShaderKeyword(QSShaderKeyword.GRASSDRAWKEYWORD);
			cmd.SetGlobalInt("grassDrawMode", (int)debugMode);

			context.ExecuteCommandBuffer(cmd);
			context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings);	
			CommandBufferPool.Release(cmd);
		}

		public override void FrameCleanup(CommandBuffer cmd)
		{
			cmd.DisableShaderKeyword(QSShaderKeyword.GRASSDRAWKEYWORD);
			cmd.SetGlobalInt("grassDrawMode", (int)DebugMode._DEBUG_THROUGH);
		}
	}
}
