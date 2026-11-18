using System;
using System.Collections.Generic;
using System.Linq;
using Illusion.Rendering.PRTGI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Illusion.Rendering.PRTGI.Editor
{
    // Unity 2022 DXR adapter for the source PRT capture shader and sample layout.
    internal sealed class PRTRayTracingBakeScene : IDisposable
    {
        private const string ShaderPath = "Assets/Mipipline/PipelineCore/PRTGI/Shaders/PRTBakeTrace.raytrace";
        private readonly RayTracingShader _shader;
        private readonly RayTracingAccelerationStructure _acceleration;
        private readonly ComputeBuffer _instances;
        private readonly ComputeBuffer _materialKeys;
        public readonly Bounds bounds;
        public readonly Hash128 geometrySignature;
        public readonly Hash128 materialSignature;

        public PRTRayTracingBakeScene(Scene scene)
        {
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D12 || !SystemInfo.supportsRayTracing)
                throw new NotSupportedException("PRT DXR capture requires an editor launched with Direct3D12 and ray tracing support.");
            _shader = AssetDatabase.LoadAssetAtPath<RayTracingShader>(ShaderPath);
            if (!_shader) throw new InvalidOperationException("PRT DXR ray generation shader was not imported: " + ShaderPath);
            _acceleration = new RayTracingAccelerationStructure(new RayTracingAccelerationStructure.RASSettings
            {
                managementMode = RayTracingAccelerationStructure.ManagementMode.Manual,
                rayTracingModeMask = RayTracingAccelerationStructure.RayTracingModeMask.Everything,
                layerMask = -1
            });
            try
            {
                var renderers = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                    .Where(r => r.enabled && r.gameObject.activeInHierarchy &&
                        (GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.ContributeGI) != 0)
                    .OrderBy(r => GlobalObjectId.GetGlobalObjectIdSlow(r).ToString(), StringComparer.Ordinal).ToArray();
                var table = new uint[Mathf.Max(1, renderers.Length) * 4];
                var keys = new List<Vector2Int>();
                var materialIds = new Dictionary<(Material, Renderer), int>();
                Hash128 geometry = default, materials = default;
                materials.Append(AssetDatabase.GetAssetDependencyHash(ShaderPath).ToString());
                Bounds total = default;
                bool hasBounds = false;
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))
                        throw new NotSupportedException("PRT DXR capture requires a mesh renderer: " + renderer.name);
                    Mesh mesh = renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh :
                        renderer.GetComponent<MeshFilter>()?.sharedMesh;
                    if (!mesh || mesh.subMeshCount < 1)
                        throw new InvalidOperationException("PRT DXR capture has no mesh: " + renderer.name);
                    var slots = renderer.sharedMaterials;
                    if (slots.Length != mesh.subMeshCount || slots.Any(m => !m ||
                        m.shader.name != "PipelineCore/PRTGI/Lit" || m.FindPass("PathTracing") < 0))
                        throw new InvalidOperationException("PRT DXR capture requires PipelineCore/PRTGI/Lit with a PathTracing pass on every submesh: " + renderer.name);
                    var flags = new RayTracingSubMeshFlags[slots.Length];
                    bool solid = true, cull = true, frontCull = true;
                    int keyStart = keys.Count;
                    for (int j = 0; j < slots.Length; j++)
                    {
                        Material material = slots[j];
                        bool anyHit = material.renderQueue >= (int)RenderQueue.AlphaTest ||
                            material.IsKeywordEnabled("_ALPHATEST_ON") || material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT");
                        flags[j] = RayTracingSubMeshFlags.Enabled | (anyHit
                            ? RayTracingSubMeshFlags.UniqueAnyHitCalls : RayTracingSubMeshFlags.ClosestHitOnly);
                        var properties = new MaterialPropertyBlock();
                        renderer.GetPropertyBlock(properties, j);
                        if (properties.isEmpty) renderer.GetPropertyBlock(properties);
                        CullMode mode = material.HasProperty("_Cull")
                            ? (CullMode)Mathf.RoundToInt(properties.HasFloat("_Cull")
                                ? properties.GetFloat("_Cull") : material.GetFloat("_Cull")) : CullMode.Off;
                        solid &= mode == CullMode.Back && !anyHit && !material.doubleSidedGI;
                        cull &= mode != CullMode.Off;
                        frontCull &= mode == CullMode.Front;
                        var identity = (material, renderer.HasPropertyBlock() ? renderer : null);
                        if (!materialIds.TryGetValue(identity, out int key))
                            materialIds.Add(identity, key = materialIds.Count + 1);
                        keys.Add(new Vector2Int(mesh.GetSubMesh(j).indexStart, key));
                    }
                    _acceleration.AddInstance(renderer, flags, cull, frontCull, solid ? 0x3u : 0x1u, (uint)i);
                    table[i * 4] = renderer.renderingLayerMask;
                    table[i * 4 + 1] = 1u << renderer.gameObject.layer;
                    table[i * 4 + 2] = (uint)keyStart;
                    table[i * 4 + 3] = (uint)slots.Length;
                    geometry.Append(GlobalObjectId.GetGlobalObjectIdSlow(renderer).ToString());
                    string meshPath = AssetDatabase.GetAssetPath(mesh);
                    if (!string.IsNullOrEmpty(meshPath)) geometry.Append(AssetDatabase.GetAssetDependencyHash(meshPath).ToString());
                    Matrix4x4 matrix = renderer.localToWorldMatrix;
                    for (int j = 0; j < 16; j++) geometry.Append(matrix[j]);
                    for (int j = 0; j < mesh.subMeshCount; j++)
                    {
                        string path = AssetDatabase.GetAssetPath(slots[j]);
                        materials.Append(slots[j].name);
                        if (!string.IsNullOrEmpty(path)) materials.Append(AssetDatabase.GetAssetDependencyHash(path).ToString());
                        foreach (string keyword in slots[j].shaderKeywords.OrderBy(value => value, StringComparer.Ordinal))
                            materials.Append(keyword);
                        AppendPropertyBlock(ref materials, renderer, slots[j], j);
                    }
                    if (!hasBounds) { total = renderer.bounds; hasBounds = true; }
                    else total.Encapsulate(renderer.bounds);
                }
                if (!hasBounds) throw new InvalidOperationException("No active Contribute GI meshes found for DXR capture.");
                _instances = new ComputeBuffer(Mathf.Max(1, renderers.Length), 16);
                _instances.SetData(table);
                _materialKeys = new ComputeBuffer(Mathf.Max(1, keys.Count), 8);
                _materialKeys.SetData(keys.ToArray());
                bounds = total;
                geometrySignature = geometry;
                materialSignature = materials;
                _acceleration.Build();
            }
            catch { Dispose(); throw; }
        }

        public PRTCaptureSample[][] Capture(Vector3[] positions, Vector4[] directions)
        {
            int probeCount = positions.Length, sampleCount = directions.Length;
            var points = positions.Select(point => (Vector4)point).ToArray();
            using (var probes = new ComputeBuffer(probeCount, 16))
            using (var samples = new ComputeBuffer(sampleCount, 16))
            using (var output = new ComputeBuffer(checked(probeCount * sampleCount), PRTCaptureSample.Stride))
            using (var cmd = new CommandBuffer { name = "PRT DXR Probe Capture" })
            {
                probes.SetData(points);
                samples.SetData(directions);
                cmd.SetRayTracingShaderPass(_shader, "PathTracing");
                cmd.SetRayTracingAccelerationStructure(_shader, "_PRTBakeScene", _acceleration);
                // Material closest-hit passes read global buffers, not ray-generation local bindings.
                cmd.SetGlobalBuffer("_PRTBakeInstances", _instances);
                cmd.SetGlobalBuffer("_PRTBakeMaterialKeys", _materialKeys);
                cmd.SetGlobalFloat("_PRTBakeConeSpread", Mathf.Sqrt(4f * Mathf.PI / sampleCount));
                cmd.SetRayTracingBufferParam(_shader, "_PRTBakeProbes", probes);
                cmd.SetRayTracingBufferParam(_shader, "_PRTBakeDirections", samples);
                cmd.SetRayTracingBufferParam(_shader, "_PRTBakeSamples", output);
                cmd.SetRayTracingIntParam(_shader, "_PRTBakeSampleCount", sampleCount);
                cmd.DispatchRays(_shader, "CaptureRayGen", (uint)sampleCount, (uint)probeCount, 1);
                Graphics.ExecuteCommandBuffer(cmd);
                var readback = new PRTCaptureSample[probeCount * sampleCount];
                output.GetData(readback);
                var result = new PRTCaptureSample[probeCount][];
                for (int i = 0; i < probeCount; i++)
                {
                    result[i] = new PRTCaptureSample[sampleCount];
                    Array.Copy(readback, i * sampleCount, result[i], 0, sampleCount);
                }
                return result;
            }
        }

        // Matches the source baker's 26-direction, four-step solid-geometry placement.
        public Vector4[] Place(Vector3[] positions, Vector2[] biases, float searchDistance)
        {
            if (positions.Length != biases.Length) throw new ArgumentException("PRT placement bias count differs from probe count.");
            var points = positions.Select(position => (Vector4)position).ToArray();
            using (var probes = new ComputeBuffer(points.Length, 16))
            using (var biasBuffer = new ComputeBuffer(biases.Length, 8))
            using (var output = new ComputeBuffer(points.Length, 16))
            using (var cmd = new CommandBuffer { name = "PRT DXR Probe Placement" })
            {
                probes.SetData(points);
                biasBuffer.SetData(biases);
                cmd.SetRayTracingShaderPass(_shader, "PathTracing");
                cmd.SetRayTracingAccelerationStructure(_shader, "_PRTBakeScene", _acceleration);
                cmd.SetGlobalBuffer("_PRTBakeInstances", _instances);
                cmd.SetGlobalBuffer("_PRTBakeMaterialKeys", _materialKeys);
                cmd.SetRayTracingBufferParam(_shader, "_PRTBakeProbes", probes);
                cmd.SetRayTracingBufferParam(_shader, "_PRTBakeBiases", biasBuffer);
                cmd.SetRayTracingBufferParam(_shader, "_PRTBakePlacements", output);
                cmd.SetRayTracingFloatParam(_shader, "_PRTBakeSearchDistance", searchDistance);
                cmd.DispatchRays(_shader, "PlacementRayGen", (uint)points.Length, 1, 1);
                Graphics.ExecuteCommandBuffer(cmd);
                var result = new Vector4[points.Length];
                output.GetData(result);
                return result;
            }
        }

        public void Dispose()
        {
            _instances?.Dispose();
            _materialKeys?.Dispose();
            _acceleration?.Dispose();
        }

        private static void AppendPropertyBlock(ref Hash128 hash, Renderer renderer, Material material, int submesh)
        {
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties, submesh);
            if (properties.isEmpty) renderer.GetPropertyBlock(properties);
            if (properties.isEmpty) return;
            Shader shader = material.shader;
            for (int i = 0; i < shader.GetPropertyCount(); i++)
            {
                string name = shader.GetPropertyName(i);
                if (!properties.HasProperty(name)) continue;
                hash.Append(name);
                switch (shader.GetPropertyType(i))
                {
                    case ShaderPropertyType.Color: hash.Append(properties.GetColor(name).ToString("R")); break;
                    case ShaderPropertyType.Vector: hash.Append(properties.GetVector(name).ToString("R")); break;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range: hash.Append(properties.GetFloat(name)); break;
                    case ShaderPropertyType.Int: hash.Append(properties.GetInt(name)); break;
                    case ShaderPropertyType.Texture:
                        Texture texture = properties.GetTexture(name);
                        if (texture is RenderTexture)
                            throw new NotSupportedException("PRT DXR capture requires a frozen property-block texture: " + name);
                        if (texture)
                        {
                            string path = AssetDatabase.GetAssetPath(texture);
                            hash.Append(string.IsNullOrEmpty(path) ? texture.GetInstanceID().ToString() :
                                AssetDatabase.GetAssetDependencyHash(path).ToString());
                        }
                        else hash.Append("null");
                        break;
                }
            }
        }
    }
}
