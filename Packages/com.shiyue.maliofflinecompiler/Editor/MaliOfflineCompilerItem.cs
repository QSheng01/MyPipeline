using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

public struct MaliOfflineCompilerItem
{
	public string Architecture;
	public string Core;
	public string API;
	public string Type;
	public string VariantName;

	public string shaderName;
	public int subShaderIndex;
	public string passName;
	public string[] keyWords;
	public string shaderType;

	// total
	// fma
	// cvt
	// sfu
	// load_store
	// texture
	public string[] Pipeline;
	public string[] LongestPathBound;
	public string[] ShortestPathBound;
	public string[] TotalBound;
	public double[] LongestPathCycles;
	public double[] ShortestPathCycles;
	public double[] TotalCycles;

	// work_registers_used
	// thread_occupancy
	// uniform_registers_used
	// has_stack_spilling
	// stack_spill_bytes
	// fp16_arithmetic
	public int[] Properties;
}
