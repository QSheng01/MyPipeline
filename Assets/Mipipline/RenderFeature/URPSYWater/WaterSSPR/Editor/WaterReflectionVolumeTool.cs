using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WaterScreenSpacePlanarReflection;

public class WaterReflectionVolumeTool : MonoBehaviour
{
    public const string WaterVolumeMenu = "工具/场景工具/自动添加场景水体后效设置";
    private static string[] PrefabPaths;
    #if UNITY_EDITOR
    [MenuItem(WaterVolumeMenu)]
    private static void SetUpWaterVolume()
    {
        List<GameObject> prefabs = GetAllPrefabs();
        foreach (var prefab in prefabs)
        {
            if (prefab.GetComponent<MeshRenderer>() &&  prefab.GetComponent<MeshRenderer>().sharedMaterial != null)
            {
                if (prefab.GetComponent<MeshRenderer>().sharedMaterial.shader ==
                    Shader.Find("ShiYue/URP/Water/URPWater"))
                {
                    if (!prefab.GetComponent<Water>())
                    {
                        prefab.AddComponent<Water>();
                    }
                }
            }
        }
        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
    }
    
    private static List<GameObject> GetAllPrefabs()
    {
        List<GameObject> prefabs = new List<GameObject>();
        var resourcesPath = Application.dataPath;
        var absolutePaths = System.IO.Directory.GetFiles(resourcesPath, "*.prefab", System.IO.SearchOption.AllDirectories);
        for (int i = 0; i < absolutePaths.Length; i++)
        {
            EditorUtility.DisplayProgressBar("设置水体信息中", "获取预制体中……", (float)i/absolutePaths.Length);

            string path = "Assets" + absolutePaths[i].Remove(0, resourcesPath.Length);
            path = path.Replace("\\", "/");
            GameObject prefab = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
            if (prefab != null)
                prefabs.Add(prefab);
            else
                Debug.Log("预制体不存在！ "+path);
        }
        EditorUtility.ClearProgressBar();
        return prefabs;
    }
    #endif
}
