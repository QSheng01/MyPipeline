//Stylized Grass Shader
//Staggart Creations (http://staggart.xyz)
//Copyright protected under Unity Asset Store EULA

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Grass
{
	public class RenderGrassBendersPass : ScriptableRenderPass
	{
        public static readonly string bendShader = "ShiYue/URP/Grass/Grass Bend Mesh";

        RenderTargetIdentifier m_ColorAttachment;
		//RenderTargetIdentifier m_CameraDepthAttachment;
		//RenderTargetHandle m_Destination;

		MaterialPropertyBlock props;
		ProfilingSampler profilerSampler;

		private Material overierMaterial;
		private bool trailEnabled;
		private Bounds renderBounds;
		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
			m_ColorAttachment = renderingData.cameraData.renderer.cameraColorTarget;
		}
		
		public RenderGrassBendersPass(RenderPassEvent renderPassEvent, string[] shaderTags, RenderQueueType renderQueueType, int layerMask, Material overierMaterial)
		{
			this.renderPassEvent = renderPassEvent;
			this.overierMaterial = overierMaterial;
			this.profilerSampler = new ProfilingSampler("RenderGrassBenders");
		}

		public void Setup(RenderTargetIdentifier source/*, RenderTargetIdentifier cameraDepth, RenderTargetHandle destination*/)
		{
			m_ColorAttachment = source;
			//m_CameraDepthAttachment = cameraDepth;
			//m_Destination = destination;
		}
		// This method is called before executing the render pass.
		// It can be used to configure render targets and their clear state. Also to create temporary render target textures.
		// When empty this render pass will render to the active camera render target.
		// You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
		// The render pipeline will ensure target setup and clearing happens in an performance manner.
		public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
		{			
			if (props == null)
				props = new MaterialPropertyBlock();
		}

		private MeshRenderer m_MeshRenderer;
		private TrailRenderer m_TrailRenderer;
		private ParticleSystemRenderer m_ParticleRenderer;

		// Here you can implement the rendering logic.
		// Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
		// https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
		// You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (GrassBenderRenderer.Instance == null ||
				GrassBenderRenderer.Instance.renderCam == null||
				GrassBenderRenderer.Instance.BenderAssets == null)
				return;

            ref CameraData cameraData = ref renderingData.cameraData;

            //if (cameraData.camera != Camera.main)
            //    return;

            var cmd = CommandBufferPool.Get();
			//var overideMaterial = overierMaterial == null ?
			//					  new Material(Shader.Find(bendShader)) : overierMaterial;

			//using (new ProfilingScope(cmd, profilerSampler))
			{
				//ref CameraData cameraData = ref renderingData.cameraData;				
				
				// 用 GrassBenderRenderer 中生成维护的 renderCam 来输出 vectorRT
				var renderCam = GrassBenderRenderer.Instance.renderCam;
				var extends = GrassBenderRenderer.Instance.renderExtends;

				var viewMatrix = renderCam.worldToCameraMatrix;

				var projectionMatrix = Matrix4x4.Ortho(-extends, extends, -extends, extends, renderCam.nearClipPlane, renderCam.farClipPlane);
				///抗锯齿，平台差异，都会影响这个值的结果。这个值的结果会导致SceneView 和GameView视角Camera贴图是否需要翻转，暂时在编辑器是以这种结果准确。真机尚未测试
				bool flipped = false;

				if (!cameraData.isSceneViewCamera)
				{
					//flipped = !cameraData.IsCameraProjectionMatrixFlipped();
					//Debug.Log(flipped);
					//flipped = true;
					// if (GrassBenderRenderer.Instance != null)
					// {
					// 	flipped = GrassBenderRenderer.Instance.Flipped;
					// } else
					// {
						flipped = !cameraData.IsCameraProjectionMatrixFlipped();
					// }
				}
				
				projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, flipped);
					// 配置 view 矩阵和 project 矩阵，并设置输出对象，vectorRT
					RenderingUtils.SetViewAndProjectionMatrices(cmd, viewMatrix, projectionMatrix, false);
					CoreUtils.SetRenderTarget(cmd, new RenderTargetIdentifier(GrassBenderRenderer.Instance.vectorRT),
													RenderBufferLoadAction.DontCare,
													RenderBufferStoreAction.Store,
													ClearFlag.All,
													GrassBenderRenderer.Instance.renderCam.backgroundColor);
				
				// 遍历所有的 GrassBender 草交互组件
				foreach (KeyValuePair<int, List<GrassBender>> layer in GrassBenderRenderer.GrassBenders)
				{
					foreach (GrassBender b in layer.Value)
					{
						if (b.enabled == false) continue;
						
						// 给拖尾渲染器 m_TrailRenderer 或者粒子系统 ParticleSystem 配置 props 数据
						props.SetVector("_Params", new Vector4(b.strength, b.heightOffset, b.pushStrength, b.scaleMultiplier));

						if (b.benderType == GrassBenderBase.BenderType.Trail)
						{
							if (!b.trailRenderer) continue;

							if (!b.trailRenderer.emitting) continue;
							
							// 配置拖尾渲染器
							m_TrailRenderer = b.trailRenderer;
							m_TrailRenderer.SetPropertyBlock(props);

							//Trail
							m_TrailRenderer.emitting = b.gameObject.activeInHierarchy;
							m_TrailRenderer.generateLightingData = true;
							m_TrailRenderer.widthMultiplier = b.trailRadius;
							m_TrailRenderer.time = b.trailLifetime;
							m_TrailRenderer.minVertexDistance = b.trailAccuracy;
							m_TrailRenderer.widthCurve = b.widthOverLifetime;
							m_TrailRenderer.colorGradient = GrassBenderBase.GetGradient(b.strengthOverLifetime);

							//If disabled, temporarly enable in order to bake mesh
							trailEnabled = m_TrailRenderer.enabled ? true : false;
							if (!trailEnabled) m_TrailRenderer.enabled = true;

							if (b.bakedMesh == null)
								b.bakedMesh = new Mesh();

							m_TrailRenderer.BakeMesh(b.bakedMesh, renderingData.cameraData.camera, false);
							//cmd.DrawMesh(b.bakedMesh, Matrix4x4.identity, overideMaterial, 0, 0, props);
							// 把拖尾，渲染到 renderCam 来输出 vectorRT
							cmd.DrawMesh(b.bakedMesh, Matrix4x4.identity, GrassBenderRenderer.Instance.BenderAssets.TrailMaterial, 0, 0, props);
							if (!trailEnabled) m_TrailRenderer.enabled = false;
						}
						if (b.benderType == GrassBenderBase.BenderType.ParticleSystem)
						{
							if (!b.particleSystem) continue;
							
							// 配置粒子系统
							m_ParticleRenderer = b.particleRenderer;
							m_ParticleRenderer.SetPropertyBlock(props);

							var grad = b.particleSystem.colorOverLifetime;
							grad.enabled = true;
							grad.color = GrassBenderBase.GetGradient(b.strengthOverLifetime);
							bool localSpace = b.particleSystem.main.simulationSpace == ParticleSystemSimulationSpace.Local;

							//Note: DrawRenderes with particle systems appear to be broken. Only renders to scene cam when it redraws. Bake the mesh down and render it instead.
							//Todo: Create repo project and file bug report. 
							//cmd.DrawRenderer(m_ParticleRenderer, m_Material, 0, 0);
							if (!b.bakedMesh) b.bakedMesh = new Mesh();
							//将粒子系统打个快照，几何体烘焙到网格中
							m_ParticleRenderer.BakeMesh(b.bakedMesh, renderingData.cameraData.camera);

							// 把粒子烘焙到 Mesh，并把 Mesh 渲染到 renderCam 来输出 vectorRT
							cmd.DrawMesh(b.bakedMesh, localSpace ? m_ParticleRenderer.localToWorldMatrix : Matrix4x4.identity, GrassBenderRenderer.Instance.BenderAssets.MeshMaterial, 0, b.alphaBlending ? 1 : 0, props);

							//Also draw particle trails
							if (b.hasParticleTrails)
							{
								if (!b.particleTrailMesh) b.particleTrailMesh = new Mesh();
								// BakeTrailsMesh会将轨迹模块（Trails module）烘焙到网格中。
								m_ParticleRenderer.BakeTrailsMesh(b.particleTrailMesh, renderingData.cameraData.camera);
								cmd.DrawMesh(b.particleTrailMesh, m_ParticleRenderer.localToWorldMatrix, GrassBenderRenderer.Instance.BenderAssets.TrailMaterial, 1, 0, props);
							}
						}
						if (b.benderType == GrassBenderBase.BenderType.Mesh)
						{
							if (!b.meshRenderer) continue;

							m_MeshRenderer = b.meshRenderer;
							m_MeshRenderer.SetPropertyBlock(props);

							cmd.DrawRenderer(m_MeshRenderer, GrassBenderRenderer.Instance.BenderAssets.MeshMaterial, 0, b.alphaBlending ? 1 : 0);
						}
					}
				}

				context.ExecuteCommandBuffer(cmd);
				cmd.Clear();

				// if (!cameraData.isStereoEnabled)
				if (!cameraData.xr.enabled)
				{
					RenderingUtils.SetViewAndProjectionMatrices(cmd, cameraData.GetViewMatrix(), GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(), false), false);
					CoreUtils.SetRenderTarget(cmd, m_ColorAttachment, ClearFlag.None);
				}
			}
			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}
		/// Cleanup any allocated resources that were created during the execution of this render pass.

		public override void FrameCleanup(CommandBuffer cmd)
		{
		}
	}

}