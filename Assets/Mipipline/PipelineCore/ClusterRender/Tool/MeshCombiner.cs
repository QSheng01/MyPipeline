using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace MPipeline
{
    /// <summary>
    /// Editor component that bakes child MeshRenderers into spatial clusters.
    /// Mesh data is stored relative to this object's transform so it can move at runtime.
    /// </summary>
    public sealed class MeshCombiner : MonoBehaviour
    {
        public ClusterGeometryAsset outputAsset;
        public string modelName = "ClusterModel";
        public Material materialOverride;

        [Range(1, 512)]
        public int voxelCount = 32;

        public bool skipLodMeshes = true;
        [Tooltip("Disable source MeshRenderers after baking so the GPU cluster path is the only visible geometry.")]
        public bool disableSourceRenderersAfterBake = false;

        [ContextMenu("Bake Cluster Asset")]
        public void BakeClusterAsset()
        {
#if UNITY_EDITOR
            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(false);
            HashSet<MeshRenderer> skipped = BuildSkippedLodSet();
            List<Point> points = new List<Point>(1024);
            List<int> triangleMaterials = new List<int>(1024);
            List<Material> sourceMaterials = new List<Material>(16);

            foreach (MeshRenderer renderer in renderers)
            {
                if (skipped.Contains(renderer))
                    continue;

                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                    continue;

                AppendMeshPoints(
                    filter.sharedMesh,
                    renderer.transform,
                    renderer.sharedMaterials,
                    points,
                    triangleMaterials,
                    sourceMaterials);
            }

            if (points.Count == 0)
            {
                Debug.LogError("No mesh data was found under " + name + ".", this);
                return;
            }

            ClusterBakeResult result = ClusterGenerator.Generate(points, triangleMaterials, voxelCount);
            if (result.clusters.Length == 0)
            {
                Debug.LogError("Cluster generation returned no clusters.", this);
                return;
            }

            if (outputAsset == null)
            {
                string folder = "Assets/Mipipline/PipelineCore/ClusterRender/ClusterData";
                EnsureFolder(folder);
                string safeName = string.IsNullOrWhiteSpace(modelName) ? name : modelName;
                string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + safeName + ".asset");
                outputAsset = ScriptableObject.CreateInstance<ClusterGeometryAsset>();
                outputAsset.name = safeName;
                AssetDatabase.CreateAsset(outputAsset, path);
            }

            outputAsset.SetData(
                result.clusters,
                result.points,
                result.triangleMaterials,
                result.localBounds,
                sourceMaterials.ToArray(),
                BuildMaterialData(sourceMaterials, out Texture2DArray baseMapArray),
                baseMapArray);
            EditorUtility.SetDirty(outputAsset);
            AssignRuntimeRenderer(outputAsset);
            if (disableSourceRenderersAfterBake)
                DisableSourceRenderers(renderers, skipped);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log("Baked " + result.clusters.Length + " clusters into " + AssetDatabase.GetAssetPath(outputAsset), outputAsset);
#else
            Debug.LogWarning("Cluster baking is only available in the Unity Editor.", this);
#endif
        }

#if UNITY_EDITOR
        private HashSet<MeshRenderer> BuildSkippedLodSet()
        {
            HashSet<MeshRenderer> skipped = new HashSet<MeshRenderer>();
            if (!skipLodMeshes)
                return skipped;

            foreach (LODGroup group in GetComponentsInChildren<LODGroup>(false))
            {
                LOD[] lods = group.GetLODs();
                for (int lodIndex = 1; lodIndex < lods.Length; ++lodIndex)
                {
                    foreach (Renderer renderer in lods[lodIndex].renderers)
                    {
                        MeshRenderer meshRenderer = renderer as MeshRenderer;
                        if (meshRenderer != null)
                            skipped.Add(meshRenderer);
                    }
                }
            }

            return skipped;
        }

        private void AppendMeshPoints(
            Mesh mesh,
            Transform meshTransform,
            Material[] sharedMaterials,
            List<Point> points,
            List<int> triangleMaterials,
            List<Material> sourceMaterials)
        {
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector4[] tangents = mesh.tangents;
            Vector2[] uvs = mesh.uv;
            Matrix4x4 meshToRoot = transform.worldToLocalMatrix * meshTransform.localToWorldMatrix;

            for (int subMesh = 0; subMesh < mesh.subMeshCount; ++subMesh)
            {
                int[] triangles = mesh.GetTriangles(subMesh);
                Material sourceMaterial = sharedMaterials != null && subMesh < sharedMaterials.Length
                    ? sharedMaterials[subMesh]
                    : null;
                int materialIndex = sourceMaterials.IndexOf(sourceMaterial);
                if (materialIndex < 0)
                {
                    materialIndex = sourceMaterials.Count;
                    sourceMaterials.Add(sourceMaterial);
                }

                for (int index = 0; index + 2 < triangles.Length; index += 3)
                {
                    AddPoint(triangles[index], vertices, normals, tangents, uvs, meshToRoot, points);
                    AddPoint(triangles[index + 1], vertices, normals, tangents, uvs, meshToRoot, points);
                    AddPoint(triangles[index + 2], vertices, normals, tangents, uvs, meshToRoot, points);
                    triangleMaterials.Add(materialIndex);
                }
            }
        }

        private static void AddPoint(
            int index,
            Vector3[] vertices,
            Vector3[] normals,
            Vector4[] tangents,
            Vector2[] uvs,
            Matrix4x4 meshToRoot,
            List<Point> points)
        {
            Vector3 normal = normals != null && normals.Length == vertices.Length ? normals[index] : Vector3.up;
            Vector4 tangent = tangents != null && tangents.Length == vertices.Length ? tangents[index] : new Vector4(1f, 0f, 0f, 1f);
            Vector3 transformedNormal = meshToRoot.MultiplyVector(normal).normalized;
            Vector3 transformedTangent = meshToRoot.MultiplyVector(new Vector3(tangent.x, tangent.y, tangent.z)).normalized;

            points.Add(new Point
            {
                vertex = meshToRoot.MultiplyPoint3x4(vertices[index]),
                normal = transformedNormal,
                tangent = new Vector4(transformedTangent.x, transformedTangent.y, transformedTangent.z, tangent.w),
                uv0 = uvs != null && uvs.Length == vertices.Length ? uvs[index] : Vector2.zero
            });
        }

        private static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; ++i)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private ClusterGeometryAsset.MaterialData[] BuildMaterialData(List<Material> materials, out Texture2DArray baseMapArray)
        {
            const int textureSize = 512;
            List<Texture2D> textures = new List<Texture2D> { Texture2D.whiteTexture };
            ClusterGeometryAsset.MaterialData[] data = new ClusterGeometryAsset.MaterialData[materials.Count];
            for (int i = 0; i < materials.Count; ++i)
            {
                Material material = materials[i];
                string property = material != null && material.HasProperty("_BaseMap")
                    ? "_BaseMap"
                    : material != null && material.HasProperty("_MainTex") ? "_MainTex" : null;
                Texture2D texture = material != null && property != null
                    ? material.GetTexture(property) as Texture2D : null;
                int slice = texture == null ? 0 : textures.IndexOf(texture);
                if (slice < 0)
                {
                    slice = textures.Count;
                    textures.Add(texture);
                }
                Color color = Color.white;
                if (material != null)
                {
                    if (material.HasProperty("_BaseColor")) color = material.GetColor("_BaseColor");
                    else if (material.HasProperty("_Color")) color = material.GetColor("_Color");
                }
                Vector2 scale = property != null ? material.GetTextureScale(property) : Vector2.one;
                Vector2 offset = property != null ? material.GetTextureOffset(property) : Vector2.zero;
                data[i] = new ClusterGeometryAsset.MaterialData
                {
                    color = color,
                    uvScaleOffset = new Vector4(scale.x, scale.y, offset.x, offset.y),
                    textureSlice = slice
                };
            }

            string assetPath = AssetDatabase.GetAssetPath(outputAsset);
            string arrayPath = assetPath.Substring(0, assetPath.Length - ".asset".Length) + "_BaseMapArray.asset";
            baseMapArray = AssetDatabase.LoadAssetAtPath<Texture2DArray>(arrayPath);
            if (baseMapArray != null && (baseMapArray.width != textureSize
                || baseMapArray.height != textureSize || baseMapArray.depth != textures.Count))
            {
                AssetDatabase.DeleteAsset(arrayPath);
                baseMapArray = null;
            }
            if (baseMapArray == null)
            {
                baseMapArray = new Texture2DArray(textureSize, textureSize, textures.Count, TextureFormat.RGBA32, true, false)
                {
                    name = outputAsset.name + "_BaseMapArray",
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Bilinear
                };
                AssetDatabase.CreateAsset(baseMapArray, arrayPath);
            }
            RenderTexture temporary = RenderTexture.GetTemporary(textureSize, textureSize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            try
            {
                for (int i = 0; i < textures.Count; ++i)
                {
                    Graphics.Blit(textures[i], temporary);
                    RenderTexture.active = temporary;
                    Texture2D readable = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, true, false);
                    readable.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0, false);
                    readable.Apply(true, false);
                    for (int mip = 0; mip < readable.mipmapCount; ++mip)
                        baseMapArray.SetPixels(readable.GetPixels(mip), i, mip);
                    DestroyImmediate(readable);
                }
                baseMapArray.Apply(false, false);
                EditorUtility.SetDirty(baseMapArray);
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(temporary);
            }
            return data;
        }

        private void AssignRuntimeRenderer(ClusterGeometryAsset asset)
        {
            ClusterRuntimeRenderer runtimeRenderer = GetComponent<ClusterRuntimeRenderer>();
            if (runtimeRenderer == null)
                runtimeRenderer = Undo.AddComponent<ClusterRuntimeRenderer>(gameObject);

            runtimeRenderer.geometry = asset;
            if (runtimeRenderer.materialOverride == null && materialOverride != null)
                runtimeRenderer.materialOverride = materialOverride;
            EditorUtility.SetDirty(runtimeRenderer);

            SceneStreaming streaming = GetComponent<SceneStreaming>();
            if (streaming != null)
            {
                streaming.clusterAsset = asset;
                EditorUtility.SetDirty(streaming);
            }
        }

        private static void DisableSourceRenderers(MeshRenderer[] renderers, HashSet<MeshRenderer> skipped)
        {
            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer == null || skipped.Contains(renderer))
                    continue;

                Undo.RecordObject(renderer, "Disable Source MeshRenderer After Cluster Bake");
                renderer.enabled = false;
                EditorUtility.SetDirty(renderer);
            }
        }
#endif
    }
}
