using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassBenderAssets : MonoBehaviour
{
	public Shader trailShader;
	public Shader meshShader;
	public Shader maskShader;

	public const string TRAIL_SHADER_NAME = "ShiYue/URP/Grass/Grass Bend Trail";
	public const string MESH_SHADER_NAME = "ShiYue/URP/Grass/Grass Bend Mesh";
	public const string MASK_SHADER_NAME = "ShiYue/URP/Grass/Grass BendRenderer";
	Material _TrailMaterial;

	public Material TrailMaterial
	{
		get
		{
			if (!_TrailMaterial)
			{
				if (!trailShader)
					trailShader = Shader.Find(TRAIL_SHADER_NAME);
				_TrailMaterial = new Material(trailShader);
				_TrailMaterial.enableInstancing = true;
			}

			return _TrailMaterial;
		}
	}
	Material _MeshMaterial;
	public Material MeshMaterial
	{
		get
		{
			if (!_MeshMaterial)
			{
				if (!meshShader)
				{
					meshShader = Shader.Find(MESH_SHADER_NAME);
				}
					
				_MeshMaterial = new Material(meshShader);
				_MeshMaterial.enableInstancing = true;
			}

			return _MeshMaterial;
		}
	}

	Material _MaskMaterial;
	public Material MaskMaterial
	{
		get
		{
			if (!_MaskMaterial)
			{
				if (!maskShader)
					maskShader = Shader.Find(MASK_SHADER_NAME);
				_MaskMaterial = new Material(maskShader);
			}
			return _MaskMaterial;
		}
	}
}
