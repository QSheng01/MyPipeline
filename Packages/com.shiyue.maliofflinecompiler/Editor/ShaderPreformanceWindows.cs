using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;
using UnityShaderAnalyzer;

public class ShaderPerformanceWindow : EditorWindow
{
	private static int showCount = 10;
	private static int m_CurrentSelect;
	private static int m_CurrentPage;
	private static int m_AllPage;
	private static Shader m_Shader;
	private static List<List<ShaderData.Pass>> m_SubShaders;
	private static Dictionary<ShaderData.Pass, Dictionary<string, List<MaliOfflineCompilerItem>>> m_ShaderInfo;
	private static Dictionary<ShaderData.Pass, List<string>> m_CatchKeyWords;
	private static Dictionary<ShaderData.Pass, bool> m_Show;
	private static Dictionary<ShaderData.Pass, bool[]> m_KeyWordIDs;
	private static Dictionary<ShaderData.Pass, List<MaliOfflineCompilerItem>> m_PerfermanceInfo;

	private Vector2 m_ScrollPosition = Vector2.zero;

	private static int m_MHZ = 981;
	private static int m_Resolution = 1920 * 1080;
	private static int m_FrameRatio = 60;

	private static int m_SelectedIndex = 0;
	private static string[] m_Resolutions = new[]
	{
		"1280x720",
		"1920x1080",
		"2160x1080",
		"2560x1440"
	};
	private static float m_VertexCounts = 600000;

	public static void Show(Shader shader)
	{
		if (shader != null) {
			var win = GetWindow(typeof(ShaderPerformanceWindow));
			win.Show();

			m_Shader = shader;
			m_SubShaders = ShaderOfflineCompiler.GetAllShaderPass(m_Shader);
			m_Show = new Dictionary<ShaderData.Pass, bool>();
			m_ShaderInfo = new Dictionary<ShaderData.Pass, Dictionary<string, List<MaliOfflineCompilerItem>>>();
			m_CatchKeyWords = new Dictionary<ShaderData.Pass, List<string>>();
			m_KeyWordIDs = new Dictionary<ShaderData.Pass, bool[]>();
			m_PerfermanceInfo = new Dictionary<ShaderData.Pass, List<MaliOfflineCompilerItem>>();
			m_SelectedIndex = 0;
			m_VertexCounts = 600000;

			Init();
		} else {
			Debug.Log("Shader is null");
		}
	}

	private void OnGUI()
	{
		EditorGUILayout.BeginVertical();
		EditorGUILayout.LabelField("当前shader： ");
		EditorGUILayout.ObjectField(m_Shader, typeof(Shader), false);
		
		EditorGUILayout.LabelField("同屏顶点数: ");
		m_VertexCounts = EditorGUILayout.FloatField(m_VertexCounts);
		
		EditorGUILayout.LabelField("当前分辨率: ");
		m_SelectedIndex = GUILayout.Toolbar(m_SelectedIndex, m_Resolutions);
		EditorGUILayout.EndVertical();

		EditorGUILayout.BeginVertical();
		EditorGUILayout.LabelField("请选择对应的变体：");

		m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
		ShowKeyWords();
		// ShowOnePage();
		EditorGUILayout.EndScrollView();

		EditorGUILayout.EndVertical();
	}

	private static void Init()
	{
		for (int i = 0; i < m_SubShaders.Count; i++) {
			var subShader = m_SubShaders[i];

			for (int j = 0; j < subShader.Count; j++) {
				var pass = subShader[j];
				var index = i;

				m_Show[pass] = false;

				var allKeyWords = ShaderOfflineCompiler.GetAllKeyWords(m_Shader, pass, (uint)index);
				var keyWordCombine = ShaderOfflineCompiler.GetAllKeyWordCombine(allKeyWords);

				var list = new List<string>();
				foreach (var keyWords in keyWordCombine) {
					var builder = new StringBuilder();
					foreach (var str in keyWords) {
						builder.Append(str);
						builder.Append(" ");

					}
					list.Add(builder.ToString());
				}

				m_CatchKeyWords.Add(pass, list);
				m_ShaderInfo.Add(pass, new Dictionary<string, List<MaliOfflineCompilerItem>>());
				m_KeyWordIDs.Add(pass, new bool[allKeyWords.Count]);
				m_PerfermanceInfo.Add(pass, new List<MaliOfflineCompilerItem>());
			}
		}
	}

	private static void ShowKeyWords()
	{
		for (int i = 0; i < m_SubShaders.Count; i++) {
			var subShader = m_SubShaders[i];

			EditorGUILayout.BeginVertical(new GUIStyle("HelpBox"));
			EditorGUILayout.LabelField("SubShader " + i);
			for (int j = 0; j < subShader.Count; j++) {
				var pass = subShader[j];
				var info = m_ShaderInfo[pass];

				m_Show[pass] = EditorGUILayout.BeginFoldoutHeaderGroup(m_Show[pass], "当前Pass：" + pass.Name);
				if (m_Show[pass]) {
					var keyWords = ShaderOfflineCompiler.GetAllKeyWords(m_Shader, pass, (uint)j);
					var list = m_KeyWordIDs[pass];

					EditorGUILayout.LabelField("请选择Shader关键字：");
					for (int k = 0; k < keyWords.Count; k++) {
						var color = GUI.backgroundColor;
						if (list[k]) {
							GUI.backgroundColor = new Color(0.5f, 0.7f, 0.5f);
						}
						if (GUILayout.Button(keyWords[k].ToString(), GUILayout.Width(400))) {
							list[k] = !list[k];
						}
						GUI.backgroundColor = color;
					}

					if (GUILayout.Button("Compile Shader", GUILayout.Width(100))) {
						var passKeyWords = new List<string>();
						for (int k = 0; k < list.Length; k++) {
							if (list[k]) {
								passKeyWords.Add(keyWords[k].ToString());
							}
						}

						var builder = new StringBuilder();
						foreach (var str in passKeyWords) {
							builder.Append(str);
							builder.Append(" ");
						}

						var items = new List<MaliOfflineCompilerItem>();
						var shaderInfo = new CompilerShaderInfo()
						{
							shaderName = "",
							subShaderIndex = i,
							passName = pass.Name,
							keyWords = passKeyWords.ToArray()
						};
						items.AddRange(ShaderOfflineCompiler.GenerateVertJson(pass, shaderInfo));
						items.AddRange(ShaderOfflineCompiler.GenerateFragJson(pass, shaderInfo));
						// ShaderOfflineCompiler.GenerateJson(pass, passKeyWords.ToArray());
						if (m_PerfermanceInfo.ContainsKey(pass)) {
							m_PerfermanceInfo[pass] = items;
						} else {
							m_PerfermanceInfo.Add(pass, items);
						}
					}


					if (m_PerfermanceInfo[pass].Count > 0)
					{
						var item = m_PerfermanceInfo[pass][0]; 
						EditorGUILayout.LabelField(item.Architecture + " -- " + item.Core + " -- " + item.API, TitleText());
					}
					var cycles = 0.0;
					for (int k = 0; k < m_PerfermanceInfo[pass].Count; k++)
					{
						var multiplyFactor = m_VertexCounts;
						if (k == 2)
						{
							var resolution = m_Resolutions[m_SelectedIndex].Split('x');
							multiplyFactor = float.Parse(resolution[0]) * float.Parse(resolution[1]);
						}
						var item = m_PerfermanceInfo[pass][k];
						cycles += ShowItem(item, multiplyFactor);
					}

					if (m_PerfermanceInfo[pass] != null && m_PerfermanceInfo[pass].Count > 0)
					{
						var perFrameCycles = (m_MHZ * 1000000) / m_FrameRatio;
						var perFrameMHZ = m_MHZ / m_FrameRatio;
						var perPixelCycles = perFrameCycles / m_Resolution;
						var resolutionStr = m_Resolutions[m_SelectedIndex].Split('x');
						var resolution = float.Parse(resolutionStr[0]) * float.Parse(resolutionStr[1]);
						var shaderMHZ = cycles / 1000000;
						var MHZpercent = (shaderMHZ / perFrameMHZ) * 100;
					
						EditorGUILayout.BeginVertical(new GUIStyle("HelpBox"));
						EditorGUILayout.LabelField("GPU频率" + m_MHZ + "MHZ，" + m_FrameRatio + "帧每帧分配 " + perFrameMHZ + "MHZ，分辨率" + resolution);
						EditorGUILayout.LabelField("着色器一帧：" + $"{shaderMHZ:0.##}" + "MHZ");
						if (MHZpercent >= 50.0)
						{
							EditorGUILayout.LabelField("着色器周期占比：" + $"{MHZpercent:0.##}" + "%", WarningText());
							EditorGUILayout.LabelField("该着色器已超标，请优化！", WarningText());
						}
						else
						{
							EditorGUILayout.LabelField("着色器周期占比：" + $"{MHZpercent:0.##}" + "%");
						}
						// if (cycles >= perPixelCycles)
						// {
						// 	EditorGUILayout.LabelField("该着色器已超标，请优化！", WarningText());
						// }

						EditorGUILayout.EndVertical();
					}
				}

				EditorGUILayout.EndFoldoutHeaderGroup();
			}
			EditorGUILayout.EndVertical();
		}
	}

	// private static void ShowOnePage()
	// {
	// 	for (int i = 0; i < m_SubShaders.Count; i++) {
	// 		var subShader = m_SubShaders[i];
	//
	// 		EditorGUILayout.BeginVertical(new GUIStyle("HelpBox"));
	// 		EditorGUILayout.LabelField("SubShader " + i);
	// 		for (int j = 0; j < subShader.Count; j++) {
	// 			var pass = subShader[j];
	// 			var info = m_ShaderInfo[pass];
	//
	// 			m_Show[pass] = EditorGUILayout.BeginFoldoutHeaderGroup(m_Show[pass], "当前Pass：" + pass.Name);
	// 			if (m_Show[pass]) {
	// 				var keyWordCombine = m_CatchKeyWords[pass];
	//
	// 				foreach (var keyWords in keyWordCombine) {
	// 					EditorGUILayout.BeginVertical(new GUIStyle("HelpBox"));
	// 					EditorGUILayout.BeginHorizontal(new GUIStyle("HelpBox"));
	// 					EditorGUILayout.LabelField(keyWords);
	// 					if (GUILayout.Button("Compile Shader", GUILayout.Width(100))) {
	// 						var items = ShaderOfflineCompiler.GenerateJson(pass, keyWords.Split());
	// 						if (info.ContainsKey(keyWords)) {
	// 							info[keyWords] = items;
	// 						} else {
	// 							info.Add(keyWords, items);
	// 						}
	// 					}
	// 					EditorGUILayout.EndHorizontal();
	//
	// 					if (m_ShaderInfo != null && info.ContainsKey(keyWords)) {
	// 						EditorGUILayout.BeginVertical(new GUIStyle("HelpBox"));
	// 						for (int k = 0; k < info[keyWords].Count; k++) {
	// 							ShowItem(info[keyWords][k]);
	// 						}
	// 						EditorGUILayout.EndVertical();
	// 					}
	// 					EditorGUILayout.EndVertical();
	// 				}
	// 			}
	//
	// 			EditorGUILayout.EndFoldoutHeaderGroup();
	// 		}
	// 		EditorGUILayout.EndVertical();
	// 	}
	// }

	private static double ShowItem(MaliOfflineCompilerItem item, float multiplyFactor = 1.0f)
	{
		EditorGUILayout.LabelField(item.Type + " --> " + item.VariantName, TitleText());

		var len = 0;
		var arithMax = double.MinValue;
		var unitMax = double.MinValue;
		EditorGUILayout.BeginHorizontal();
		for (int i = -1; i < item.Pipeline.Length; i++) {

			EditorGUILayout.BeginVertical();
			if (i < 0)
			{
				len = 125;
				EditorGUILayout.LabelField("                  ", TitleText(), GUILayout.Width(len));
				EditorGUILayout.LabelField("TotalCycles", TitleText(), GUILayout.Width(len));
				EditorGUILayout.LabelField("LongestPathCycles", TitleText(), GUILayout.Width(len));
				EditorGUILayout.LabelField("ShortestPathCycles", TitleText(), GUILayout.Width(len));
			} else {
				len = 75;
				EditorGUILayout.LabelField(item.Pipeline[i], TitleText(), GUILayout.Width(len));

				foreach (var bound in item.TotalBound) {
					if (item.Pipeline[i].Contains("arith"))
					{
						arithMax = item.TotalCycles[i] > arithMax ? item.TotalCycles[i] : arithMax;
					}
					else
					{
						unitMax = item.TotalCycles[i] > unitMax ? item.TotalCycles[i] : unitMax;
					}

					if (bound.Equals(item.Pipeline[i])) {
						EditorGUILayout.LabelField($"{item.TotalCycles[i] * multiplyFactor:0.##}", HightLightText(), GUILayout.Width(len));
						break;
					}
					else {
						EditorGUILayout.LabelField($"{item.TotalCycles[i] * multiplyFactor:0.##}", GUILayout.Width(len));
						break;
					}
				}

				foreach (var bound in item.LongestPathBound) {
					if (bound.Equals(item.Pipeline[i])) {
						EditorGUILayout.LabelField($"{item.LongestPathCycles[i] * multiplyFactor:0.##}", HightLightText(), GUILayout.Width(len));
						break;
					} 
					else {
						EditorGUILayout.LabelField($"{item.LongestPathCycles[i] * multiplyFactor:0.##}", GUILayout.Width(len));
						break;
					}
				}
				
				foreach (var bound in item.ShortestPathBound) {
					if (bound.Equals(item.Pipeline[i])) {
						EditorGUILayout.LabelField($"{item.ShortestPathCycles[i] * multiplyFactor:0.##}", HightLightText(), GUILayout.Width(len));
						break;
					}
					else {
						EditorGUILayout.LabelField($"{item.ShortestPathCycles[i] * multiplyFactor:0.##}", GUILayout.Width(len));
						break;
					}
				}
			}
			EditorGUILayout.EndVertical();
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.LabelField("____________________________________________________________________________________", TitleText());

		return (arithMax + unitMax) * multiplyFactor;
	}

	private static GUIStyle TitleText()
	{
		var style = new GUIStyle();
		style.fontStyle = FontStyle.Bold;
		style.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

		return style;
	}

	private static GUIStyle HightLightText()
	{
		var style = new GUIStyle();
		style.fontStyle = FontStyle.Bold;
		style.normal.textColor = new Color(0.8f, 0.5f, 0.3f);

		return style;
	}
	
	private static GUIStyle WarningText()
	{
		var style = new GUIStyle();
		style.fontStyle = FontStyle.Bold;
		style.normal.textColor = new Color(0.8f, 0.3f, 0.3f);

		return style;
	}

	private static GUIStyle SelectedButton()
	{
		var style = new GUIStyle();

		return style;
	}
}
