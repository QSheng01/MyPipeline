using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityShaderAnalyzer
{
	[Serializable]
	public struct ShaderPassData
	{
		public Shader shader;
		public int subShaderID;
		public string passName;
		public string[] keyWords;
	}

    public class ShaderVarientsData : ScriptableObject
    {
	    [SerializeField] public List<ShaderPassData> datas = new List<ShaderPassData>();
    }
}
