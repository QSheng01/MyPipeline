using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LocalVolumetricFogMenuItem : EditorWindow
{
    private static string objectName = "Local Volumetric Fog";
    private static List<System.Type> types = new List<System.Type>() {typeof(LocalVolumetricFog)};

    [MenuItem("GameObject/Rendering/局部体积雾")]
    public static void CreateFogDensityMask()
    {
        GameObject go = new GameObject(objectName, types.ToArray());
        go.transform.parent = Selection.activeTransform;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        Selection.activeGameObject = go;
    }
}
