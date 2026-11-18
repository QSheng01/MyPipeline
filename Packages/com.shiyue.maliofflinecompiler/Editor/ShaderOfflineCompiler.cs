using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using OfficeOpenXml;
using OfficeOpenXml.Drawing;
using OfficeOpenXml.Drawing.Chart;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace UnityShaderAnalyzer
{
    public class ShaderOfflineCompiler
    {
	    private static string _maliocPath = Application.dataPath.Replace("Assets", "Packages") + "/com.shiyue.maliofflinecompiler/Editor/mali_offline_compiler/malioc.exe";
	    private static List<ShaderPassData> m_Datas;

	    private static ShaderType m_VertexType = ShaderType.Vertex;
	    private static ShaderType m_FragmentType = ShaderType.Fragment;
	    private static ShaderCompilerPlatform m_ShaderCompilerPlatform = ShaderCompilerPlatform.GLES3x;
	    private static BuildTarget m_BuildTarget = BuildTarget.Android;
	    private static GraphicsTier m_GraphicsTier = GraphicsTier.Tier3;

	    [MenuItem("Assets/Mali Offline Compiler/Generate Shader Info")]
	    public static void MaliOfflineCompilerShader()
	    {
		    m_Datas = new List<ShaderPassData>();

			if (Selection.activeObject is Shader)
			{
				var shader = Selection.activeObject as Shader;
				var shaderData = ShaderUtil.GetShaderData(shader);
				var subShaderCount = shaderData.SubshaderCount;

				ShaderPerformanceWindow.Show(shader);
				return;
				// for (int i = 0; i < subShaderCount; i++) {
				// 	var subShaderID = i;
				// 	var subShader = shaderData.GetSubshader(subShaderID);
				// 	for (int j = 0; j < subShader.PassCount; j++)
				// 	{
				// 		var pass = subShader.GetPass(j);
				// 
				// 		if (pass == null)
				// 		{
				// 			Debug.LogFormat("Failed to get pass {0}", j);
				// 		}
				// 		else
				// 		{
				// 			var shaderTag = pass.FindTagValue(new ShaderTagId("LightMode")).name;
				// 			var identifier = new PassIdentifier();
				// 			var type = typeof(PassIdentifier);
				// 			var fieldInfo = type.GetField("m_PassIndex", BindingFlags.NonPublic | BindingFlags.Instance);
				// 			var obj = identifier as object;
				// 			if (fieldInfo != null)
				// 			{
				// 				fieldInfo.SetValue(obj, (uint) j);
				// 			}
				// 			identifier = (PassIdentifier)obj;
				// 
				// 			// Initialize KeyWords
				// 			var allKeysWords = ShaderUtil.GetPassKeywords(shader, identifier).ToList();
				// 			for (int k = allKeysWords.Count - 1; k >= 0 ; k--)
				// 			{
				// 				if (allKeysWords[k].type != ShaderKeywordType.UserDefined)
				// 				{
				// 					allKeysWords.RemoveAt(k);
				// 				}
				// 				else
				// 				{
				// 					//Debug.Log(shaderTag + " : " + allKeysWords[k]);
				// 				}
				// 			}
				// 
				// 			// Permutation
				// 			LoopingAllKeyWords(shader, pass, allKeysWords, subShaderID, shaderTag);
				// 
				// 			// Analysis
				// 			// Analysis(pass);
				// 		}
				// 	}
				// }
				// 
				// CreateAsset();
			}
	    }

	    public static List<List<ShaderData.Pass>> GetAllShaderPass(Shader shader)
	    {
		    var shaderData = ShaderUtil.GetShaderData(shader);
		    var subShaderCount = shaderData.SubshaderCount;

		    var subPasses = new List<List<ShaderData.Pass>>();
		    for (int i = 0; i < subShaderCount; i++) {
			    var subShaderID = i;
			    var subShader = shaderData.GetSubshader(subShaderID);

			    var passes = new List<ShaderData.Pass>();
			    for (int j = 0; j < subShader.PassCount; j++) {
				    var pass = subShader.GetPass(j);

				    if (pass == null)
				    {
					    Debug.LogFormat("Failed to get pass {0}", j);
				    } else {
					    passes.Add(pass);
				    }
			    }

			    subPasses.Add(passes);
		    }

		    return subPasses;
	    }

	    public static List<string> GetAllKeyWords(Shader shader, ShaderData.Pass pass, uint passIndex)
	    {
		    // var shaderTag = pass.FindTagValue(new ShaderTagId("LightMode")).name;
		    
		    // var identifier = new PassIdentifier();
		    // var type = typeof(PassIdentifier);
		    // var fieldInfo = type.GetField("m_PassIndex", BindingFlags.NonPublic | BindingFlags.Instance);
		    // var obj = identifier as object;
		    // if (fieldInfo != null)
		    // {
			//     fieldInfo.SetValue(obj, (uint) passIndex);
		    // }
		    // identifier = (PassIdentifier)obj;
			// 
		    // var allKeysWords = ShaderUtil.GetPassKeywords(shader, identifier).ToList();

		    var code = pass.SourceCode;
		    var lineList = code.Split('\n');
		    var allKeysWords = new List<string>();
		    for (int i = lineList.Length - 1; i > 0 ; i--)
		    {
			    if (lineList[i].Contains("shader_feature") || lineList[i].Contains("multi_compile"))
			    {
				    if (lineList[i].Contains("instancing"))
				    {
					    allKeysWords.Add("INSTANCING_ON");
				    }
				    if (lineList[i].Contains("fog"))
				    {
					    allKeysWords.Add("FOG_LINEAR");
					    allKeysWords.Add("FOG_EXP");
					    allKeysWords.Add("FOG_EXP2");
				    }
				    
				    var list = lineList[i].TrimStart().TrimEnd().Split(' ');
				    
				    if (list.Length > 2)
				    {
					    for (int j = 2; j < list.Length; j++)
					    {
						    if (!list[j].Equals("_"))
						    {
							    allKeysWords.Add(list[j]);
						    }
					    }
				    }
			    }
		    }

		    return allKeysWords;
	    }

	    public static List<string[]> GetAllKeyWordCombine(List<string> allKeyWords)
	    {
		    var allKeyWordCombine = new List<string[]>();

		    for (int i = 0; i < allKeyWords.Count; i++) {
			    var keyWord = allKeyWords[i];
			    var list = new List<string>();
			    list.Add(keyWord);
			    allKeyWordCombine.Add(list.ToArray());

			    var prevList = list.ToArray();
			    var startIndex = i + 1;

			    while (startIndex < allKeyWords.Count) {
				    for (int j = startIndex; j < allKeyWords.Count; j++) {
					    var nextKeyWord = allKeyWords[j];
					    var nextList = prevList.ToList();
					    nextList.Add(nextKeyWord);
					    allKeyWordCombine.Add(nextList.ToArray());

					    prevList = nextList.ToArray();
				    }

				    prevList = list.ToArray();
				    startIndex++;
			    }
		    }

		    return allKeyWordCombine;
	    }

	    private static void LoopingAllKeyWords(Shader shader, ShaderData.Pass pass, List<string> allKeyWords, int id, string passName)
	    {
		    for (int i = 0; i < allKeyWords.Count; i++) {
			    var keyWord = allKeyWords[i];
			    var list = new List<string>();
			    list.Add(keyWord);
			    // var info = Generate(pass,list.ToArray());
			    var firstData = new ShaderPassData {
				    shader = shader,
				    subShaderID = id,
				    passName = passName,
				    keyWords = list.ToArray(),
				    // info = info
			    };
			    m_Datas.Add(firstData);

			    // if (!info.Equals(string.Empty)) {
				//     m_Datas.Add(firstData);
			    // }

			    var prevData = firstData;
			    var startIndex = i + 1;

			    while (startIndex < allKeyWords.Count) {
				    for (int j = startIndex; j < allKeyWords.Count; j++) {
					    var nextKeyWord = allKeyWords[j];
					    var nextList = prevData.keyWords.ToList();
					    nextList.Add(nextKeyWord);

					    // var nextInfo = Generate(pass,nextList.ToArray());
					    var nextData = new ShaderPassData {
						    shader = shader,
						    subShaderID = id,
						    passName = passName,
						    keyWords = nextList.ToArray(),
						    // info = nextInfo
					    };
					    m_Datas.Add(nextData);
					    // if (!nextInfo.Equals(string.Empty)) {
						//     m_Datas.Add(nextData);
					    // }

					    prevData = nextData;
				    }

				    prevData = firstData;
				    startIndex++;
			    }
		    }
	    }

	    public static string GenerateText(ShaderData.Pass pass, string[] keywords)
	    {
		    try {
			    var vertCode = ProcessPass(pass, m_VertexType, keywords, m_ShaderCompilerPlatform, m_BuildTarget);
			    var fragCode = ProcessPass(pass, m_FragmentType, keywords, m_ShaderCompilerPlatform, m_BuildTarget);
			    if (vertCode.Equals(string.Empty) || fragCode.Equals(string.Empty)) {
				    return string.Empty;
			    }

			    string vertPath;
			    if (TryGetFilePath(out vertPath, ShaderType.Vertex)) {
				    TryWriteCompiledCodeToFile(vertPath, vertCode);
			    }

			    string fragPath;
			    if (TryGetFilePath(out fragPath, ShaderType.Fragment)) {
				    TryWriteCompiledCodeToFile(fragPath, fragCode);
			    }

			    var builder = new StringBuilder();
			    builder.Append("Vertex: \n");
			    builder.Append(GetTextAnalysis(vertPath) + "\n");
			    builder.Append("Fragment: \n");
			    builder.Append(GetTextAnalysis(fragPath) + "\n");

			    return builder.ToString();

		    } catch (Exception e) {
				// Ignore
		    }

		    return string.Empty;
		    // Debug.Log("KeyWords: " + builder);
		    // Debug.Log(vertAnalysisText);
		    // Debug.Log(fragAnalysisText);
	    }

	    public static List<MaliOfflineCompilerItem> GenerateVertJson(ShaderData.Pass pass, CompilerShaderInfo shaderInfo)
	    {
		    var vertCode = ProcessPass(pass, m_VertexType, shaderInfo.keyWords, m_ShaderCompilerPlatform, m_BuildTarget);

		    string vertPath;
		    if (TryGetFilePath(out vertPath, ShaderType.Vertex)) {
			    TryWriteCompiledCodeToFile(vertPath, vertCode);
		    }
		    var vertJson = GetJsonAnalysis(vertPath);
		    var vertItem = ConvertMaliOfflineCompilerItem(vertJson, ShaderType.Vertex, shaderInfo);

		    return vertItem;
	    }
	    
	    public static List<MaliOfflineCompilerItem> GenerateFragJson(ShaderData.Pass pass, CompilerShaderInfo shaderInfo)
	    {
		    var fragCode = ProcessPass(pass, m_FragmentType, shaderInfo.keyWords, m_ShaderCompilerPlatform, m_BuildTarget);

		    string fragPath;
		    if (TryGetFilePath(out fragPath, ShaderType.Fragment)) {
			    TryWriteCompiledCodeToFile(fragPath, fragCode);
		    }

		    var fragJson = GetJsonAnalysis(fragPath);
		    var fragItem = ConvertMaliOfflineCompilerItem(fragJson, ShaderType.Fragment, shaderInfo);
		    return fragItem;
	    }

	    private static string ProcessPass(ShaderData.Pass pass, ShaderType shaderType, string[] keywords, ShaderCompilerPlatform shaderCompilerPlatform, BuildTarget buildTarget)
	    {
		    var info = pass.CompileVariant(ShaderType.Vertex, keywords, shaderCompilerPlatform, buildTarget);
		    // switch (shaderCompilerPlatform)
		    // {
			   //  case ShaderCompilerPlatform.Vulkan:
			   //  case ShaderCompilerPlatform.GLES3x:
			   //  case ShaderCompilerPlatform.GLES20:
			   //  case ShaderCompilerPlatform.OpenGLCore:
			   //  case ShaderCompilerPlatform.Switch:
		    //
				  //   break;
		    // }

		    if (!info.Success) {
			    Debug.LogError("Failed to compile shader variant");

			    foreach (var message in info.Messages) {
				    Debug.LogErrorFormat("{0}: {1}. {2}", message.severity, message.message, message.messageDetails);
			    }
		    }

		    string code = Encoding.ASCII.GetString(info.ShaderData).Trim();
		    code = code.Replace("#version 310 es", "")
			    .Replace("#version 300 es", "")
			    .Insert(0, "#version 310 es\n");

		    int fragmentStart = code.IndexOf("#ifdef FRAGMENT");
		    if (fragmentStart == -1)
		    {
			    Debug.LogFormat("Failed to find #ifdef FRAGMENT");
			    return string.Empty;
		    }

		    // Take only vertex method OR fragment method
		    if (shaderType == ShaderType.Vertex)
		    {
			    code = code.Substring(0, fragmentStart);
		    }
		    else if (shaderType == ShaderType.Fragment)
		    {
			    code = code.Substring(fragmentStart);
			    code = "#version 310 es\n" + code;
		    }

		    // Remove #ifdef VERTEX/FRAGMENT and #endif
		    int closingBrace = code.LastIndexOf('}');
		    if (closingBrace == -1)
		    {
			    Debug.LogFormat("Failed to find last closing brace in code");
		    }
		    else
		    {
			    code = code.Substring(0, closingBrace + 1).Replace("#ifdef VERTEX", "").Replace("#ifdef FRAGMENT", "");
		    }

		    return code;
		    //_compiledCode = code;
	    }

	    private static void CreateAsset()
	    {
		    var datas = ScriptableObject.CreateInstance<ShaderVarientsData>();
		    var filename = $"Assets/MaliOfflineCompiler/ShaderVariants-{DateTime.Now:yyMMdd HHmmss}.asset";

		    for (int i = 0; i < m_Datas.Count; i++) {
			    var data = m_Datas[i];
			    // var str = Generate(pass, data.keyWords);
			    // if (!str.Equals(string.Empty)) {
			    //
			    //
			    // }

			    datas.datas.Add(data);
		    }

		    AssetDatabase.CreateAsset(datas, filename);
	    }

	    private static bool TryGetFilePath(out string path, ShaderType shaderType)
	    {
		    string filename;

		    if (shaderType == ShaderType.Vertex) {
			    filename = $"{Guid.NewGuid()}.vert";
		    } else if (shaderType == ShaderType.Fragment) {
			    filename = $"{Guid.NewGuid()}.frag";
		    } else {
			    path = null;
			    return false;
		    }

		    path = Path.Combine("Temp", filename);
		    return true;
	    }

	    private static bool TryWriteCompiledCodeToFile(string path, string code)
	    {
		    if (string.IsNullOrEmpty(code)) {
			    return false;
		    }

		    try {
			    File.WriteAllText(path, code);
		    } catch (Exception e) {
			    Debug.LogException(e);
			    return false;
		    }

		    return true;
	    }

	    private static string GetJsonAnalysis(string filePath)
	    {
		    string args = $"\"{filePath}\" --format json";

		    return RunMalioc(args);
	    }

	    private static string GetTextAnalysis(string filePath)
	    {
		    return RunMalioc(filePath);
	    }

	    private static List<MaliOfflineCompilerItem> ConvertMaliOfflineCompilerItem(string args, ShaderType shaderType, CompilerShaderInfo shaderInfo)
	    {
		    switch (shaderType) {
			    case ShaderType.Vertex:
				    return MaliOfflineCompilerVertJsonDecoder.JsonDecode(args, shaderInfo);
			    case ShaderType.Fragment:
				    return MaliOfflineCompilerFragJsonDecoder.JsonDecode(args, shaderInfo);
		    }

		    return null;
	    }

	    private class MaliOfflineCompilerItemCompare : IComparer<MaliOfflineCompilerItem>
	    {
		    public int Compare(MaliOfflineCompilerItem x, MaliOfflineCompilerItem y)
		    {
			    switch (x.shaderType)
			    {
				    case "Varying":
					case "Position":
					    return
						    (y.TotalCycles[0] + y.TotalCycles[4] > y.TotalCycles[5] ? y.TotalCycles[4] : y.TotalCycles[5]).
						    CompareTo(
							    x.TotalCycles[0] + x.TotalCycles[4] > x.TotalCycles[5] ? x.TotalCycles[4] : x.TotalCycles[5]
						    );
					    break;
				    
				    case "Fragment":
					    return
						    (y.TotalCycles[0] + Mathf.Max((float)y.TotalCycles[4], (float)Mathf.Max((float)y.TotalCycles[5], (float)y.TotalCycles[6]))).
						    CompareTo(
							    x.TotalCycles[0] + Mathf.Max((float)x.TotalCycles[4], (float)Mathf.Max((float)x.TotalCycles[5], (float)x.TotalCycles[6]))
						    );
					    break;
			    }

			    return 0;
		    }
	    }
	    public static void CreateExcel(Dictionary<string, List<MaliOfflineCompilerItem>> itemsDict)
	    {
		    if (itemsDict.Count <= 0)
		    {
			    Debug.Log("编译失败");
			    return;
		    }
		    
		    var dirPath = Application.dataPath + "/PerformanceExcels";
		    var dirInfo = new DirectoryInfo(dirPath);
		    if (!dirInfo.Exists)
		    {
			    dirInfo.Create();
		    }

		    var outputPath = dirPath + "/shader性能检测报告" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";
		    var newFile = new FileInfo(outputPath);
		    if(newFile.Exists)
		    {
			    newFile.Delete();
		    }
		
		    using (var package = new ExcelPackage(newFile))
		    {
			    foreach (var shaderType in itemsDict.Keys)
			    {
				    var items = itemsDict[shaderType];
				    items.Sort(new MaliOfflineCompilerItemCompare());
				    
				    var sheet = package.Workbook.Worksheets.Add(shaderType);
				    sheet.Cells[1, 1].Value = "Shader Name";
				    sheet.Cells[1, 2].Value = "ShaderType";
				    sheet.Cells[1, 3].Value = "SubShader Index";
				    sheet.Cells[1, 4].Value = "Pass Index(Name)";
				    sheet.Cells[1, 5].Value = "KeyWords";
				    for (int i = 0; i < items[0].Pipeline.Length; i++)
				    {
					    sheet.Cells[1, 6 + i].Value = items[0].Pipeline[i];
				    }
			    
				    for (int i = 0; i < items.Count; i++)
				    {
					    var item = items[i];
					    var x = 2 + i;
					
					    sheet.Cells[x, 1].Value = item.shaderName + " " + i;
					    sheet.Cells[x, 2].Value = item.shaderType;
					    sheet.Cells[x, 3].Value = item.subShaderIndex;
					    sheet.Cells[x, 4].Value = item.passName;
					    sheet.Cells[x, 5].Value = String.Join(" ", item.keyWords);
					    for (int j = 0; j < item.TotalCycles.Length; j++)
					    {
						    sheet.Cells[x, 6 + j].Value = item.TotalCycles[j];
					    }
				    }

				    sheet.Cells[1, 6 + items[0].Pipeline.Length].Value = "Total Cycles";
				    for (int i = 0; i < items.Count; i++)
				    {
					    var item = items[i];
					    
					    var maxCycles = double.MinValue;
					    for (int j = 4; j < item.TotalCycles.Length; j++)
					    {
						    maxCycles = Mathf.Max((float)maxCycles, (float)item.TotalCycles[j]);
					    }
					    sheet.Cells[2 + i, 6 + items[0].Pipeline.Length].Value = item.TotalCycles[0] + maxCycles;
				    }

				    var xSerie = sheet.Cells[2, 1, 2 + items.Count, 1];
				    for (int i = -1; i < items[0].Pipeline.Length; i++)
				    {
					    if (i == -1)
					    {
						    var ySerie = sheet.Cells[2, 6 + items[0].Pipeline.Length, 2 + items.Count, 6 + items[0].Pipeline.Length];	
						    var chart = sheet.Drawings.AddChart("chart" + i, eChartType.ColumnClustered);
						    chart.Series.Add(ySerie, xSerie);
						    chart.Title.Text = "总消耗";
						    chart.SetPosition(0, 1000);//设置位置
						    chart.SetSize(900, 400);//设置大小
						    chart.Title.Font.Size = 15;//标题的大小
						    chart.Title.Font.Bold = true;//标题的粗体
						    chart.Style = eChartStyle.Style15;//设置图表的样式
						    chart.Legend.Remove();
					    }
					    else
					    {
						    var ySerie = sheet.Cells[2, 6 + i, 2 + items.Count, 6 + i];
						    var chart = sheet.Drawings.AddChart("chart" + i, eChartType.ColumnClustered);
						    chart.Series.Add(ySerie, xSerie);
						    chart.Title.Text = items[0].Pipeline[i] + "消耗";
						    chart.SetPosition(400 * (i + 1), 1000);//设置位置
						    chart.SetSize(900, 400);//设置大小
						    chart.Title.Font.Size = 15;//标题的大小
						    chart.Title.Font.Bold = true;//标题的粗体
						    chart.Style = eChartStyle.Style15;//设置图表的样式
						    chart.Legend.Remove();   
					    }
				    }
			    }

			    if(package.Workbook.Worksheets.Count != 0)
			    {
				    package.Save();
				    Debug.Log("检测文档导出路径:" + outputPath);
			    }    
		    }       
		
		    AssetDatabase.SaveAssets();
		    AssetDatabase.Refresh();
	    }
	    
	    private static string RunMalioc(string args)
	    {
		    // Debug.LogFormat("Running malioc with args: {0}", args);

		    var processStartInfo = new ProcessStartInfo(_maliocPath, args) {
			    UseShellExecute = false,
			    RedirectStandardOutput = true,
			    CreateNoWindow = true
		    };

		    var process = Process.Start(processStartInfo);

		    string output = process.StandardOutput.ReadToEnd();
		    process.WaitForExit();

		    // Debug.LogFormat("Received {0} characters back from malioc", output.Length);

		    return output;
	    }
    }
    
    public struct CompilerShaderInfo
    {
	    public string shaderName;
	    public int subShaderIndex;
	    public string passName;
	    public string[] keyWords;
    }
}
