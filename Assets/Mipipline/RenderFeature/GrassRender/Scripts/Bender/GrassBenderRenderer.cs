using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Grass
{
	[ExecuteInEditMode]
	public class GrassBenderRenderer : MonoBehaviour
	{
		public static string VECTOR_MAP_PARAM = "_BendMap";
		public static string VECTOR_UV_PARAM = "_BendMapUV";

		public static GrassBenderRenderer Instance;
		public const int TexelsPerMeter = 16;

		public bool debug = false;
		[Tooltip("When not in play-mode, the renderer will follow the scene-view camera position.")]
		public bool followSceneCamera;
		[Tooltip("Controls how large the render area is. Small is better, since a large area thins out the rendering resolution")]
		[Range(8, 512)]
		public float renderExtends = 16f;
		[Tooltip("The renderer will follow this Transform's position. Ideally set to the player's transform.")]
		public Transform followTarget;
		public RenderTexture vectorRT;
		public Camera renderCam;
		[Tooltip("When enabled, the bend strength decreases smoothly at the edges of the render area. If disabled, benders crossing the edge will cause streaking lines of flat grass." +
			"\n\nBest enabled when using a small render area and a first-person perspective. For top-down games, using a large render area beyond the camera field of view also works.")]
		public bool maskEdges;

		public int resolution = 1024;
		private int m_resolution;
		private Vector4 uv;

		[Tooltip("When enabled the grass Ambient and Gust strength values are multiplied by the WindZone's Main value")]
		public bool listenToWindZone;
		public WindZone windZone;

		//GrassBender will register itself to this list
		public static SortedDictionary<int, List<GrassBender>> GrassBenders = new SortedDictionary<int, List<GrassBender>>();
		private static List<GrassBender> BenderLayer;
		public static int benderCount;

		public Transform actualFollowTarget;
		public Vector3 lastPos;
		public Bounds bounds;

		private bool _flipped;
		public bool Flipped => _flipped;

		GrassBenderAssets _benderAssets;
		public GrassBenderAssets BenderAssets { 
			get {
				if (_benderAssets == null)
					_benderAssets = FindObjectOfType<GrassBenderAssets>();
				return _benderAssets;
			} 
		}

        public static void RegisterBender(GrassBender gb)
		{

			if (GrassBenders.ContainsKey(gb.sortingLayer))
			{
				GrassBenders.TryGetValue(gb.sortingLayer, out BenderLayer);

				if (BenderLayer.Contains(gb) == false)
				{
					BenderLayer.Add(gb);
					GrassBenders[gb.sortingLayer] = BenderLayer;
					benderCount++;
				}
			}
			//Create new layer
			else
			{
				BenderLayer = new List<GrassBender>();
				BenderLayer.Add(gb);

				GrassBenders.Add(gb.sortingLayer, BenderLayer);
				benderCount++;
			}

			if (Instance)
			{
				gb.gameObject.layer = Instance.gameObject.layer;
			}
		}

		public static void UnRegisterBender(GrassBender gb)
		{
			if (GrassBenders.ContainsKey(gb.sortingLayer))
			{
				GrassBenders.TryGetValue(gb.sortingLayer, out BenderLayer);

				BenderLayer.Remove(gb);
				benderCount--;

				//If layer is now empty, remove it
				if (GrassBenders[gb.sortingLayer].Count == 0)
					GrassBenders.Remove(gb.sortingLayer);
			}
		}

		public static Color neutralVector = new Color(0.5f, 0f, 0.5f, 0f);


		public void OnEnable()
		{
			Instance = this;
			_flipped = true;
			Init();
#if UNITY_EDITOR
			UnityEditor.SceneView.duringSceneGui += OnSceneGUI;
#endif
		}

        public void OnDestroy()
		{
			Instance = null;

			//Shader needs to disable texture reading, since default global textures are gray
			uv.w = 0;
			Shader.SetGlobalVector("_BendMapUV", uv);

#if UNITY_EDITOR
			UnityEditor.SceneView.duringSceneGui -= OnSceneGUI;
#endif

			if (renderCam)
			{
				DestroyImmediate(renderCam.gameObject);
				DestroyImmediate(vectorRT);
				renderCam = null;
			}

			Shader.SetGlobalVector("_GlobalWindParams", new Vector4(0f, 0f, 0f, 0f));
		}

		private void Init()
		{
			resolution = CalculateResolution(renderExtends);
			m_resolution = resolution;
			// 创建适量图到 vectorRT
			CreateVectorMap();

			// 没有目标就跟随主相机
			if (!followTarget)
			{
				GameObject targetObj = GameObject.FindGameObjectWithTag("MainCamera");
				if (targetObj)
					actualFollowTarget = targetObj.transform;
			}
		}

		private void Update()
		{
			actualFollowTarget = followTarget ?? this.transform;
			
			// 配置输出 vectorRT 的相机，这个相机设置了 HideFlags 场景里面是找不到的。
			if (renderCam)
			{
				// 更新相机的位置
				UpdateCamera();
			}
			else
			{
				renderCam = CreateCamera();
			}

			//Assign to all shaders
			// _BendMap 设置为 vectorRT
			if (vectorRT && !maskEdges)
			{
				Shader.SetGlobalTexture(VECTOR_MAP_PARAM, vectorRT);
			}
			
			// 配置风的 Param
			if (listenToWindZone)
			{
				if (windZone)
					Shader.SetGlobalVector("_GlobalWindParams", new Vector4(windZone.windMain, 0f, 0f, 1f));
			}
			else
			{
				Shader.SetGlobalVector("_GlobalWindParams", new Vector4(0f, 0f, 0f, 0f));
			}
			
			// 计算两个向量的夹角（有正负）
			// Vector2 targetForwardXZ = new Vector2(actualFollowTarget.transform.forward.x, actualFollowTarget.transform.forward.z);
			// Vector2 positionXZ = new Vector2(1.0f, 1.0f);
			// float signedAngle2D = Vector2.SignedAngle(targetForwardXZ, positionXZ);
			// Debug.Log("打印一下现在的角度" + signedAngle2D);
			
			// 配置 _BendMapUV
			Shader.SetGlobalVector(VECTOR_UV_PARAM, uv);
			Shader.SetGlobalVector("_BendRenderParams", new Vector4(this.transform.position.y, renderExtends, 0f, 0f));
		}

		public void SetBenderFilpped(bool flipped)
		{
			_flipped = flipped;
		}

		private Camera CreateCamera()
		{
			Camera cam = new GameObject().AddComponent<Camera>();
			cam.gameObject.name = "GrassBendCamera " + GetInstanceID();
			cam.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            cam.gameObject.hideFlags = HideFlags.HideAndDontSave;
            if (actualFollowTarget)
				cam.gameObject.transform.position = new Vector3(actualFollowTarget.transform.position.x,
																actualFollowTarget.transform.position.y + renderExtends,
																actualFollowTarget.transform.position.z);
			cam.enabled = false;
			cam.orthographic = true;
			cam.depth = -100f;
			cam.allowHDR = false;
			cam.allowMSAA = false;
			cam.clearFlags = CameraClearFlags.SolidColor;
			cam.backgroundColor = neutralVector;

			cam.useOcclusionCulling = false;

			UniversalAdditionalCameraData camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
			camData.renderShadows = false;
			camData.renderPostProcessing = false;
			camData.antialiasing = AntialiasingMode.None;
			camData.requiresColorOption = CameraOverrideOption.Off;
			camData.requiresDepthOption = CameraOverrideOption.Off;
			camData.requiresColorTexture = false;
			camData.requiresDepthTexture = false;
			if (Application.isPlaying)
			{
				DontDestroyOnLoad(cam.gameObject);
			}
			return cam;
		}

		//Create the influence map for the shaders
		private void CreateVectorMap()
		{
			if (vectorRT != null)
			{
				if (renderCam)
					renderCam.targetTexture = null;
				DestroyImmediate(vectorRT);
			}

			RenderTextureDescriptor rtDsc = new RenderTextureDescriptor();
			rtDsc.width = resolution;
			rtDsc.height = resolution;
			rtDsc.depthBufferBits = 0;

			rtDsc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;

			rtDsc.enableRandomWrite = false; //Not supported on OpenGL
			rtDsc.autoGenerateMips = false;
			rtDsc.useMipMap = false;
			rtDsc.volumeDepth = 1;
			rtDsc.msaaSamples = 1;
			rtDsc.dimension = TextureDimension.Tex2D;
			rtDsc.sRGB = false;
			rtDsc.vrUsage = VRTextureUsage.None;
			rtDsc.bindMS = false;
			rtDsc.memoryless = RenderTextureMemoryless.None;
			rtDsc.shadowSamplingMode = ShadowSamplingMode.None;

			vectorRT = new RenderTexture(rtDsc)
			{
				useMipMap = false,
				filterMode = FilterMode.Bilinear,
				wrapMode = TextureWrapMode.Clamp,
				anisoLevel = 0,

				name = "BendMap" + GetInstanceID(),
				isPowerOfTwo = true,
				hideFlags = HideFlags.DontSave
			};
		}

		public static int CalculateResolution(float size)
		{
			int res = Mathf.RoundToInt(size * TexelsPerMeter);
			res = Mathf.NextPowerOfTwo(res);
			res = Mathf.Clamp(res, 256, 2048);
			return res;
		}

		private void UpdateCamera()
		{
			if (!renderCam)
				return;

			renderCam.orthographicSize = renderExtends;
			renderCam.farClipPlane = renderExtends * 2f;

			if (actualFollowTarget)
			{
				/// Move the renderer once the follow target has moved a distance equal to 10% of the render extends size
				// if (((actualFollowTarget.transform.position - lastPos).sqrMagnitude > renderExtends * 0.1f) ||
				// 	lastPos == Vector3.zero)
				// {
					renderCam.transform.position = new Vector3(actualFollowTarget.transform.position.x, actualFollowTarget.transform.position.y + renderExtends, actualFollowTarget.transform.position.z);
					lastPos = actualFollowTarget.transform.position;
				// }
			}
			else
			{
				renderCam.transform.position = new Vector3(transform.position.x, transform.position.y + renderExtends, transform.position.z);
			}

			bounds = new Bounds(new Vector3(
								renderCam.transform.position.x, renderCam.transform.position.y - renderExtends,
								renderCam.transform.position.z),
								Vector3.one * renderExtends * 2);

			//When changing resolution
			if (m_resolution != resolution)
				CreateVectorMap();
			m_resolution = resolution;

			uv.x = bounds.min.x;
			uv.y = bounds.min.z;
			uv.z = 1 / bounds.size.x;
			uv.w = 1f; //Enable bend map sampling in shader
		}

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			if (!renderCam) return;

			Gizmos.color = new Color(1, 1, 1, 1f); 
			Gizmos.DrawWireCube(bounds.center, bounds.size);
		}

		private void OnGUI() //Gameview 
		{
			DrawDebugGUI(false);
		}

		private void OnSceneGUI(UnityEditor.SceneView sceneView)
		{
			DrawDebugGUI(true);

			if (followSceneCamera)
			{
				actualFollowTarget = sceneView.camera.transform;
				UpdateCamera();
			}
		}

		void DrawDebugGUI(bool sceneView)
		{
			if (vectorRT == null) return;

			Rect imgRect = new Rect(5, 5, 256, 256);
			//Set to UI debug image
			if (debug && !sceneView)
			{
				GUI.DrawTexture(imgRect, vectorRT);
			}

			if (debug && sceneView)
			{
				UnityEditor.Handles.BeginGUI();

				GUILayout.BeginArea(imgRect);

				UnityEditor.EditorGUI.DrawTextureTransparent(imgRect, vectorRT);
				GUILayout.EndArea();
				UnityEditor.Handles.EndGUI();
			}
		}
#endif
	}
}
