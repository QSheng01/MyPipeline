using System;
using System.Collections.Generic;
using System.Linq;
using Illusion.Rendering.PRTGI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Illusion.Rendering.PRTGI.Editor
{
    // A geometry snapshot in its own physics scene. No colliders are added to the authored scene.
    internal sealed class PRTCpuBakeScene : IDisposable
    {
        private sealed class Surface
        {
            public Renderer renderer;
            public Material[] materials;
            public int[] triangleEnds;
            public uint materialKey;
        }

        private readonly Scene _scene;
        private readonly PhysicsScene _physics;
        private readonly Dictionary<Collider, Surface> _surfaces = new Dictionary<Collider, Surface>();
        private readonly List<Mesh> _temporaryMeshes = new List<Mesh>();
        private readonly Collider[] _overlaps = new Collider[64];
        public readonly Bounds bounds;
        public readonly Hash128 geometrySignature;
        public readonly Hash128 materialSignature;

        public PRTCpuBakeScene(Scene authoredScene)
        {
            _scene = EditorSceneManager.NewPreviewScene();
            _physics = _scene.GetPhysicsScene();
            try
            {
                Hash128 geometry = default, materials = default;
                Bounds total = default;
                bool hasBounds = false;
                var renderers = authoredScene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                    .Where(r => r.enabled && r.gameObject.activeInHierarchy &&
                        (GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.ContributeGI) != 0)
                    .OrderBy(r => GlobalObjectId.GetGlobalObjectIdSlow(r).ToString(), StringComparer.Ordinal);
                foreach (Renderer renderer in renderers)
                {
                    Mesh mesh;
                    if (renderer is MeshRenderer && renderer.TryGetComponent(out MeshFilter filter)) mesh = filter.sharedMesh;
                    else if (renderer is SkinnedMeshRenderer skinned)
                    {
                        mesh = new Mesh();
                        skinned.BakeMesh(mesh);
                        _temporaryMeshes.Add(mesh);
                    }
                    else throw new NotSupportedException("PRT bake requires a MeshRenderer or SkinnedMeshRenderer: " + renderer.name);
                    if (!mesh || mesh.subMeshCount == 0) continue;
                    Material[] slots = renderer.sharedMaterials;
                    if (slots.Length < mesh.subMeshCount || slots.Take(mesh.subMeshCount).Any(m => !m))
                        throw new InvalidOperationException("PRT bake has a missing material slot: " + renderer.name);

                    var proxy = new GameObject("PRT Bake: " + renderer.name);
                    SceneManager.MoveGameObjectToScene(proxy, _scene);
                    proxy.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
                    proxy.transform.localScale = renderer.transform.lossyScale;
                    var collider = proxy.AddComponent<MeshCollider>();
                    collider.sharedMesh = mesh;
                    if (!collider.sharedMesh)
                        throw new InvalidOperationException("PRT bake cannot create a mesh collider for " + renderer.name);
                    var surface = new Surface
                    {
                        renderer = renderer,
                        materials = slots,
                        triangleEnds = new int[mesh.subMeshCount],
                        materialKey = (uint)_surfaces.Count + 1
                    };
                    int triangleCount = 0;
                    for (int i = 0; i < mesh.subMeshCount; i++)
                    {
                        triangleCount += (int)mesh.GetIndexCount(i) / 3;
                        surface.triangleEnds[i] = triangleCount;
                        string materialPath = AssetDatabase.GetAssetPath(slots[i]);
                        materials.Append(slots[i].name);
                        if (!string.IsNullOrEmpty(materialPath)) materials.Append(AssetDatabase.GetAssetDependencyHash(materialPath).ToString());
                    }
                    _surfaces.Add(collider, surface);
                    geometry.Append(GlobalObjectId.GetGlobalObjectIdSlow(renderer).ToString());
                    string meshPath = AssetDatabase.GetAssetPath(mesh);
                    if (!string.IsNullOrEmpty(meshPath)) geometry.Append(AssetDatabase.GetAssetDependencyHash(meshPath).ToString());
                    Matrix4x4 matrix = renderer.localToWorldMatrix;
                    for (int i = 0; i < 16; i++) geometry.Append(matrix[i]);
                    if (!hasBounds) { total = renderer.bounds; hasBounds = true; }
                    else total.Encapsulate(renderer.bounds);
                }
                if (_surfaces.Count == 0) throw new InvalidOperationException("No active Contribute GI mesh renderers were found in the scene.");
                bounds = total;
                geometrySignature = geometry;
                materialSignature = materials;
                Physics.SyncTransforms();
            }
            catch { Dispose(); throw; }
        }

        public Vector3 Place(Vector3 point, float bias, float searchDistance, out bool valid)
        {
            const float epsilon = 0.005f;
            for (int attempt = 0; attempt < 4; attempt++)
            {
                if (_physics.OverlapSphere(point, epsilon, _overlaps, ~0, QueryTriggerInteraction.Ignore) == 0)
                { valid = true; return point; }
                Vector3 best = point;
                float nearest = float.PositiveInfinity;
                float normalY = -2f;
                for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++) for (int z = -1; z <= 1; z++)
                {
                    if (x == 0 && y == 0 && z == 0) continue;
                    Vector3 direction = new Vector3(x, y, z).normalized;
                    Vector3 start = point + direction * searchDistance;
                    if (!_physics.Raycast(start, -direction, out RaycastHit hit, searchDistance * 2f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    Vector3 candidate = hit.point + hit.normal * Mathf.Max(epsilon * 2f, bias);
                    float distance = (candidate - point).sqrMagnitude;
                    if (distance < nearest - 0.0001f ||
                        (Mathf.Abs(distance - nearest) <= 0.0001f && hit.normal.y > normalY))
                    {
                        best = candidate;
                        nearest = distance;
                        normalY = hit.normal.y;
                    }
                }
                if (nearest == float.PositiveInfinity) break;
                point = best;
            }
            valid = _physics.OverlapSphere(point, epsilon, _overlaps, ~0, QueryTriggerInteraction.Ignore) == 0;
            return point;
        }

        public PRTCaptureSample Capture(Vector3 position, Vector3 direction)
        {
            if (!_physics.Raycast(position, direction, out RaycastHit hit, float.MaxValue, ~0, QueryTriggerInteraction.Ignore))
                return new PRTCaptureSample { flags = PRTCaptureSample.SkyMiss, position = position + direction };
            Surface surface = _surfaces[hit.collider];
            int submesh = 0;
            while (submesh < surface.triangleEnds.Length - 1 && hit.triangleIndex >= surface.triangleEnds[submesh]) submesh++;
            Material material = surface.materials[submesh];
            Color albedo = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : Color.white;
            if (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") is Texture2D texture && texture.isReadable)
            {
                Vector2 uv = hit.textureCoord;
                Vector2 scale = material.GetTextureScale("_BaseMap"), offset = material.GetTextureOffset("_BaseMap");
                albedo *= texture.GetPixelBilinear(Mathf.Repeat(uv.x * scale.x + offset.x, 1f),
                    Mathf.Repeat(uv.y * scale.y + offset.y, 1f));
            }
            return new PRTCaptureSample
            {
                position = hit.point,
                normal = hit.normal.normalized,
                albedo = new Vector3(Mathf.Max(0f, albedo.r), Mathf.Max(0f, albedo.g), Mathf.Max(0f, albedo.b)),
                renderingLayerMask = surface.renderer.renderingLayerMask,
                objectLayerMask = 1u << surface.renderer.gameObject.layer,
                materialKey = surface.materialKey
            };
        }

        public void Dispose()
        {
            if (_scene.IsValid()) EditorSceneManager.ClosePreviewScene(_scene);
            foreach (var mesh in _temporaryMeshes) if (mesh) UnityEngine.Object.DestroyImmediate(mesh);
        }
    }
}
