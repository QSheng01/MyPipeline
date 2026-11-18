using System;
using LitJson;
using System.Collections.Generic;
using UnityEngine;
using UnityShaderAnalyzer;

public class MaliOfflineCompilerFragJsonDecoder
{
	public static List<MaliOfflineCompilerItem> JsonDecode(string args, CompilerShaderInfo shaderInfo)
	{
		var items = new List<MaliOfflineCompilerItem>();
		ShaderPrefermanceObject obj = null;
		try
		{
			args = args.Replace("null", "0");
			obj = JsonMapper.ToObject<ShaderPrefermanceObject>(args);
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			obj = null;
		}
		finally
		{
			if (obj != null)
			{
				for (int i = 0; i < obj.shaders[0].variants.Length; i++) {
					var variant = obj.shaders[0].variants[i];

					var item = new MaliOfflineCompilerItem{
						Architecture = obj.shaders[0].hardware.architecture,
						Core = obj.shaders[0].hardware.core,
						API = obj.shaders[0].shader.api,
						Type = obj.shaders[0].shader.type,

						VariantName = variant.name,

						Pipeline = variant.performance.pipelines,
						LongestPathBound = variant.performance.longest_path_cycles.bound_pipelines,
						ShortestPathBound = variant.performance.shortest_path_cycles.bound_pipelines,
						TotalBound = variant.performance.total_cycles.bound_pipelines,

						LongestPathCycles = variant.performance.longest_path_cycles.cycle_count,
						ShortestPathCycles = variant.performance.shortest_path_cycles.cycle_count,
						TotalCycles = variant.performance.total_cycles.cycle_count,

						shaderName = shaderInfo.shaderName,
						subShaderIndex = shaderInfo.subShaderIndex,
						passName = shaderInfo.passName,
						keyWords = shaderInfo.keyWords,
						shaderType = "Fragment"
						//Properties = new [] {
						//	(int) variant.properties[0].value,
						//	(int) variant.properties[1].value,
						//	(int) variant.properties[2].value,
						//	((bool) variant.properties[3].value) ? 1 : 0,
						//	(int) variant.properties[4].value,
						//	(int) variant.properties[5].value,
						//}
					};

					items.Add(item);
				}

			}
		}
		
		return items;
	}

	public class ShaderPrefermanceObject
	{
	    public Producer producer { get; set; }
	    public Schema schema { get; set; }
	    public Shaders[] shaders { get; set; }
	}

	public class Producer
	{
	    public string build { get; set; }
	    public string documentation { get; set; }
	    public string name { get; set; }
	    public int[] version { get; set; }
	}

	public class Schema
	{
	    public string name { get; set; }
	    public int version { get; set; }
	}

	public class Shaders
	{
	    public string driver { get; set; }
	    public string filename { get; set; }
	    public Hardware hardware { get; set; }
	    public object[] notes { get; set; }
	    public Properties[] properties { get; set; }
	    public Shader shader { get; set; }
	    public Variants[] variants { get; set; }
	    public object[] warnings { get; set; }
	}

	public class Hardware
	{
	    public string architecture { get; set; }
	    public string core { get; set; }
	    public Pipelines[] pipelines { get; set; }
	    public string revision { get; set; }
	}

	public class Pipelines
	{
	    public string description { get; set; }
	    public string display_name { get; set; }
	    public string name { get; set; }
	}

	public class Properties
	{
	    public string description { get; set; }
	    public string display_name { get; set; }
	    public string name { get; set; }
	    public bool value { get; set; }
	}

	public class Shader
	{
	    public string api { get; set; }
	    public string type { get; set; }
	}

	public class Variants
	{
	    public string name { get; set; }
	    public Performance performance { get; set; }
	    public Properties1[] properties { get; set; }
	}

	public class Performance
	{
	    public Longest_path_cycles longest_path_cycles { get; set; }
	    public string[] pipelines { get; set; }
	    public Shortest_path_cycles shortest_path_cycles { get; set; }
	    public Total_cycles total_cycles { get; set; }
	}

	public class Longest_path_cycles
	{
	    public string[] bound_pipelines { get; set; }
	    public double[] cycle_count { get; set; }
	}

	public class Shortest_path_cycles
	{
	    public string[] bound_pipelines { get; set; }
	    public double[] cycle_count { get; set; }
	}

	public class Total_cycles
	{
	    public string[] bound_pipelines { get; set; }
	    public double[] cycle_count { get; set; }
	}

	public class Properties1
	{
	    public string description { get; set; }
	    public string display_name { get; set; }
	    public string name { get; set; }
	    public object value { get; set; }
	}
}
