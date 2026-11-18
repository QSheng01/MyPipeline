using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Grass
{
	[CustomEditor(typeof(GrassBenderRenderer))]
	public class GrassBenderRendererInspector : UnityEditor.Editor
	{
		GrassBenderRenderer script;
		SerializedProperty renderExtends;
		SerializedProperty followSceneCamera;
		SerializedProperty followTarget;
		SerializedProperty renderLayer;
		SerializedProperty maskEdges;
		SerializedProperty colorMap;
		SerializedProperty listenToWindZone;
		SerializedProperty windZone;

		//SerializedProperty trailShader;
		//SerializedProperty meshShader;
		//SerializedProperty maskShader;

		private Vector2 benderScrollPos;

		private Texture benderIcon;

		private Vector4 windParams;

		private string[] layerStr;

		//[MenuItem("GameObject/Tools/GrassBenderRenderer", priority = 0)]
		//static void CreateTool(MenuCommand menuCommand)
		//{
		//	var coms = FindObjectsOfType<GrassBenderRenderer>();
		//	if (coms.Length > 0)
		//	{
		//		Debug.LogError("已经存在 GrassBenderRenderer 组件");
		//		return;
		//	}

		//	var parent = new GameObject("GrassBenderRenderer");
		//	parent.AddComponent<GrassBenderRenderer>();
		//}

		private void OnEnable()
		{
			script = (GrassBenderRenderer)target;
#if URP
            if (script.bendRenderer == null)
            {
                script.bendRenderer = PipelineUtilities.GetRenderer(DrawGrassBenders.AssetName);
                PipelineUtilities.ValidatePipelineRenderers(script.bendRenderer);

                script.OnEnable();
                script.OnDisable();
            }
#endif

			renderExtends = serializedObject.FindProperty("renderExtends");
			followSceneCamera = serializedObject.FindProperty("followSceneCamera");
			followTarget = serializedObject.FindProperty("followTarget");
			renderLayer = serializedObject.FindProperty("renderLayer");
			maskEdges = serializedObject.FindProperty("maskEdges");
			colorMap = serializedObject.FindProperty("colorMap");
			listenToWindZone = serializedObject.FindProperty("listenToWindZone");
			windZone = serializedObject.FindProperty("windZone");

			//maskShader = serializedObject.FindProperty("maskShader");
			//trailShader = serializedObject.FindProperty("trailShader");
			//meshShader = serializedObject.FindProperty("meshShader");
			layerStr = UnityEditorInternal.InternalEditorUtility.layers;

			//script.trailShader = Shader.Find(GrassBenderRenderer.TRAIL_SHADER_NAME);
			//script.meshShader = Shader.Find(GrassBenderRenderer.MESH_SHADER_NAME);
			//script.maskShader = Shader.Find(GrassBenderRenderer.MASK_SHADER_NAME);
		}

		public override void OnInspectorGUI()
		{
			this.Repaint();

			serializedObject.Update();
			windParams = Shader.GetGlobalVector("_GlobalWindParams");

			EditorGUI.BeginChangeCheck();

			ParameterGroup.DrawHeader(new GUIContent("Settings"));

			using (new EditorGUILayout.VerticalScope(ParameterGroup.Section))
			{
				EditorGUILayout.PropertyField(renderExtends);
				EditorGUILayout.HelpBox("Resolution: " + GrassBenderRenderer.CalculateResolution(renderExtends.floatValue) + "px (" + GrassBenderRenderer.TexelsPerMeter + " texels/m)", MessageType.None);
				script.resolution = GrassBenderRenderer.CalculateResolution(renderExtends.floatValue);
				using (new EditorGUILayout.HorizontalScope())
				{
					EditorGUILayout.PropertyField(maskEdges);
					if (!Application.isPlaying) EditorGUILayout.HelpBox("Only takes effect in Play mode", MessageType.None);
				}
				EditorGUILayout.PropertyField(followSceneCamera);
				EditorGUILayout.PropertyField(followTarget);
				//EditorGUILayout.PropertyField(trailShader);
				//EditorGUILayout.PropertyField(meshShader);
				//EditorGUILayout.PropertyField(maskShader);
				EditorGUILayout.Space();

				//renderLayer.intValue = EditorGUILayout.Popup("Render layer", renderLayer.intValue, layerStr);
				// EditorGUILayout.PropertyField(colorMap);
				EditorGUILayout.PropertyField(listenToWindZone);
				using (new EditorGUILayout.HorizontalScope())
				{
					if (listenToWindZone.boolValue)
					{
						EditorGUILayout.PropertyField(windZone);

						if (!windZone.objectReferenceValue)
						{
							if (GUILayout.Button("Create", GUILayout.MaxWidth(75f)))
							{
								GameObject obj = new GameObject();
								obj.name = "Wind Zone";
								WindZone wz = obj.AddComponent<WindZone>();

								windZone.objectReferenceValue = wz;
							}
						}

					}
				}
			}

			EditorGUILayout.Space();

			ParameterGroup.DrawHeader(new GUIContent("Grass benders (" + GrassBenderRenderer.benderCount + ")"));

			benderScrollPos = EditorGUILayout.BeginScrollView(benderScrollPos, ParameterGroup.Section, GUILayout.MaxHeight(150f));
			{
				var prevColor = GUI.color;
				var prevBgColor = GUI.backgroundColor;
				//var rect = EditorGUILayout.BeginHorizontal();

				int i = 0;

				foreach (KeyValuePair<int, List<GrassBender>> layer in GrassBenderRenderer.GrassBenders)
				{
					if (GrassBenderRenderer.GrassBenders.Count > 1)
						EditorGUILayout.LabelField("Layer " + layer.Key, EditorStyles.boldLabel);
					foreach (GrassBender bender in layer.Value)
					{
						var rect = EditorGUILayout.BeginHorizontal();
						GUI.color = i % 2 == 0 ? Color.white * 0.66f : Color.grey * (EditorGUIUtility.isProSkin ? 1.1f : 1.5f);

						if (rect.Contains(Event.current.mousePosition)) GUI.color = Color.white * 0.75f;
						if (Selection.activeGameObject == bender.gameObject) GUI.color = Color.white * 0.8f;

						EditorGUI.DrawRect(rect, GUI.color);

						//GUILayout.Space(5);
						GUI.color = prevColor;
						GUI.backgroundColor = prevBgColor;

						if (bender.benderType == GrassBenderBase.BenderType.Mesh) benderIcon = EditorGUIUtility.IconContent("MeshRenderer Icon").image;
						if (bender.benderType == GrassBenderBase.BenderType.Trail) benderIcon = EditorGUIUtility.IconContent("TrailRenderer Icon").image;
						if (bender.benderType == GrassBenderBase.BenderType.ParticleSystem) benderIcon = EditorGUIUtility.IconContent("ParticleSystem Icon").image;

						if (GUILayout.Button(new GUIContent(" " + bender.name, benderIcon), EditorStyles.miniLabel, GUILayout.MaxHeight(20f)))
						{
							Selection.activeGameObject = bender.gameObject;
						}

						EditorGUILayout.EndHorizontal();

						i++;

					}
				}
			}
			EditorGUILayout.EndScrollView();

			if (EditorGUI.EndChangeCheck())
			{
				serializedObject.ApplyModifiedProperties();
				// if (colorMap.objectReferenceValue) script.colorMap.SetActive();
			}

			EditorGUILayout.LabelField("- Staggart Creations -", EditorStyles.centeredGreyMiniLabel);
		}

		public override bool HasPreviewGUI()
		{
			return script.vectorRT;
		}

		public override void OnPreviewGUI(Rect r, GUIStyle background)
		{
			if (!script.vectorRT) return;

			GUI.DrawTexture(r, script.vectorRT, ScaleMode.ScaleToFit);

			Rect btnRect = r;
			btnRect.x += 5f;
			btnRect.y += 5f;
			btnRect.width = 150f;
			btnRect.height = 20f;
			script.debug = GUI.Toggle(btnRect, script.debug, new GUIContent(" Pin to viewport"));

			//   GUI.Label(new Rect(r.width * 0.5f - (175 * 0.5f), r.height - 5, 175, 25), string.Format("{0} texel(s) per meter", ColorMapEditor.GetTexelSize(script.vectorRT.height, script.resolution)), EditorStyles.toolbarButton);

		}
	}
}
