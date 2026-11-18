using System.IO;
using NoOvertime.VirtualTexture;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class AdaptiveVirtualTextureSampleSetup
{
    private const string SampleRoot = "Assets/Mipipline/PipelineCore/AdaptiveVirtualTexture/SampleScene";
    private const string DeferredRendererPath = "Assets/Settings/URP-AVT-Deferred-Renderer.asset";
    private const string ScenePath = "Assets/Mipipline/PipelineCore/AdaptiveVirtualTexture/SampleScene/AVTSampleScene.unity";

    [MenuItem("Adaptive Virtual Texture/Setup URP Sample")]
    public static void Setup()
    {
        EnsureDeferredRenderer();
        AddFeatureToRenderer("Assets/Settings/URP-HighFidelity-Renderer.asset");
        AddFeatureToRenderer("Assets/Settings/URP-Balanced-Renderer.asset");
        AddFeatureToRenderer("Assets/Settings/URP-Performant-Renderer.asset");
        AddFeatureToRenderer(DeferredRendererPath);
        AppendRenderer("Assets/Settings/URP-HighFidelity.asset", DeferredRendererPath);
        AppendRenderer("Assets/Settings/URP-Balanced.asset", DeferredRendererPath);
        AppendRenderer("Assets/Settings/URP-Performant.asset", DeferredRendererPath);

        var shader = Shader.Find("URP/TerrainMaterialIDVT");
        if (shader == null)
            throw new System.InvalidOperationException("URP/TerrainMaterialIDVT shader was not found.");

        var material = AssetDatabase.LoadAssetAtPath<Material>(SampleRoot + "/TerrainMaterial.mat");
        if (material == null)
        {
            material = new Material(shader) { name = "TerrainMaterial" };
            material.SetFloat("_NearBlendDistance", 50f);
            material.SetFloat("_FarBlendDistance", 200f);
            material.SetFloat("_EnableInstancedPerPixelNormal", 1f);
            material.enableInstancing = true;
            AssetDatabase.CreateAsset(material, SampleRoot + "/TerrainMaterial.mat");
        }
        else
        {
            material.shader = shader;
            material.enableInstancing = true;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var sun = new GameObject("Sun");
        var light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;
        sun.transform.rotation = new Quaternion(0.38268343f, 0f, 0f, 0.92387956f);

        var cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        var camera = cameraGo.AddComponent<Camera>();
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 4000f;
        camera.fieldOfView = 60f;
        cameraGo.AddComponent<AudioListener>();
        cameraGo.AddComponent<UniversalAdditionalCameraData>();
        cameraGo.transform.position = new Vector3(1308.7407f, 58.14116f, 1069.9469f);
        cameraGo.transform.rotation = new Quaternion(0.024810724f, 0.9473099f, -0.07576568f, 0.3102385f);
        var demo = cameraGo.AddComponent<AdaptiveVirtualTextureDemo>();

        var terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(SampleRoot + "/SampleTerrain.asset");
        var terrainGo = Terrain.CreateTerrainGameObject(terrainData);
        terrainGo.name = "Terrain";
        var terrain = terrainGo.GetComponent<Terrain>();
        terrain.materialTemplate = material;
        terrain.drawInstanced = true;
        terrain.drawTreesAndFoliage = false;
        terrain.heightmapPixelError = 15f;

        var vtGo = new GameObject("VirtualTexture");
        var vt = vtGo.AddComponent<VirtualTextureMono>();
        vt.resource = AssetDatabase.LoadAssetAtPath<Resource>("Assets/Mipipline/PipelineCore/AdaptiveVirtualTexture/VTResource.asset");
        vt.terrainSize = 2048;
        var layers = vtGo.AddComponent<AdaptiveVirtualTextureLayerSet>();
        layers.target = vt;
        layers.terrainMaterial = material;
        layers.baseMapArray = AssetDatabase.LoadAssetAtPath<Texture2DArray>(SampleRoot + "/Textures/BaseMapArray.texture2darray");
        layers.maskMapArray = AssetDatabase.LoadAssetAtPath<Texture2DArray>(SampleRoot + "/Textures/MaskMapArray.texture2darray");
        layers.splatMap = AssetDatabase.LoadAssetAtPath<Texture2D>(SampleRoot + "/Textures/splatmap_uncompress.png");
        layers.globalMap = AssetDatabase.LoadAssetAtPath<Texture2D>(SampleRoot + "/Textures/globalmap_uncompress.tga");
        demo.virtualTexture = vt;
        layers.Apply();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("AVT URP sample scene is ready at " + ScenePath);
    }

    private static Texture2D[] LoadLayers(string key)
    {
        string json = File.ReadAllText(SampleRoot + "/LayerOrder.json");
        var order = JsonUtility.FromJson<LayerOrder>(Wrap(json, key));
        var textures = new Texture2D[order.names.Length];
        for (int i = 0; i < order.names.Length; i++)
            textures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(SampleRoot + "/Textures/" + order.names[i]);
        return textures;
    }

    private static string Wrap(string json, string key)
    {
        int start = json.IndexOf("\"" + key + "\"");
        int arrayStart = json.IndexOf('[', start);
        int arrayEnd = json.IndexOf(']', arrayStart);
        string array = json.Substring(arrayStart, arrayEnd - arrayStart + 1);
        return "{\"names\":" + array + "}";
    }

    private static void EnsureDeferredRenderer()
    {
        if (AssetDatabase.LoadAssetAtPath<UniversalRendererData>(DeferredRendererPath) == null)
        {
            if (!AssetDatabase.CopyAsset("Assets/Settings/URP-HighFidelity-Renderer.asset", DeferredRendererPath))
                throw new System.InvalidOperationException("Failed to copy the URP renderer.");
        }

        var deferred = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(DeferredRendererPath);
        deferred.renderingMode = RenderingMode.Deferred;
        EditorUtility.SetDirty(deferred);
    }

    private static void AddFeatureToRenderer(string path)
    {
        var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
        if (data == null)
            return;
        foreach (var feature in data.rendererFeatures)
        {
            if (feature is AdaptiveVirtualTextureRendererFeature)
                return;
        }

        var created = ScriptableObject.CreateInstance<AdaptiveVirtualTextureRendererFeature>();
        created.name = "AdaptiveVirtualTextureRendererFeature";
        AssetDatabase.AddObjectToAsset(created, data);
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(created, out _, out long localId);
        var so = new SerializedObject(data);
        var features = so.FindProperty("m_RendererFeatures");
        int index = features.arraySize;
        features.InsertArrayElementAtIndex(index);
        features.GetArrayElementAtIndex(index).objectReferenceValue = created;
        var map = so.FindProperty("m_RendererFeatureMap");
        map.InsertArrayElementAtIndex(index);
        map.GetArrayElementAtIndex(index).longValue = localId;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
    }

    private static void AppendRenderer(string pipelinePath, string rendererPath)
    {
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
        if (pipeline == null || renderer == null)
            return;

        var so = new SerializedObject(pipeline);
        var list = so.FindProperty("m_RendererDataList");
        for (int i = 0; i < list.arraySize; i++)
        {
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == renderer)
                return;
        }

        int index = list.arraySize;
        list.InsertArrayElementAtIndex(index);
        list.GetArrayElementAtIndex(index).objectReferenceValue = renderer;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(pipeline);
    }

    [System.Serializable]
    private class LayerOrder
    {
        public string[] names;
    }
}