using UnityEditor;
using UnityEngine;

namespace Grass
{
	[CustomEditor(typeof(GrassBenderAssets))]
	public class GrassBenderAssetsEditor : UnityEditor.Editor
	{
		GrassBenderAssets script;
	
		SerializedProperty trailShader;
		SerializedProperty meshShader;
		SerializedProperty maskShader;


		[MenuItem("GameObject/Tools/GrassBenderAssets", priority = 0)]
		static void CreateTool(MenuCommand menuCommand)
		{
			var coms = FindObjectsOfType<GrassBenderAssets>();
			if (coms.Length > 0)
			{
				Debug.LogError("已经存在 GrassBenderAssets 组件");
				return;
			}

			var parent = new GameObject("GrassBenderAssets");
			parent.AddComponent<GrassBenderAssets>();
		}

#if UNITY_EDITOR
		private void OnEnable()
		{
			script = (GrassBenderAssets)target;

            maskShader = serializedObject.FindProperty("maskShader");
            trailShader = serializedObject.FindProperty("trailShader");
            meshShader = serializedObject.FindProperty("meshShader");

            script.trailShader = Shader.Find(GrassBenderAssets.TRAIL_SHADER_NAME);
            script.meshShader = Shader.Find(GrassBenderAssets.MESH_SHADER_NAME);
            script.maskShader = Shader.Find(GrassBenderAssets.MASK_SHADER_NAME);
        }
#endif
	}
}
