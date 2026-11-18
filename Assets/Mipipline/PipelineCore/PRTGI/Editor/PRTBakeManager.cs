using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Illusion.Rendering.PRTGI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Illusion.Rendering.PRTGI.Editor
{
    public static class PRTBakeManager
    {
        private static CancellationTokenSource _cancellation;
        public static bool IsBaking => _cancellation != null;

        [MenuItem("Tools/PRTGI/Generate Lighting")]
        private static void GenerateLightingMenu() => GenerateLighting(FindVolume());

        [MenuItem("Tools/PRTGI/Generate Lighting (DXR)")]
        private static void GenerateRayTracedLightingMenu() => GenerateLighting(FindVolume(), true);

        [MenuItem("Tools/PRTGI/Cancel Baking")]
        private static void CancelMenu() => CancelBaking();

        [MenuItem("Tools/PRTGI/Cancel Baking", true)]
        private static bool CanCancel() => IsBaking;

        [MenuItem("Tools/PRTGI/Clear Baked Data")]
        private static void ClearMenu() => ClearBakedData(FindVolume());

        [MenuItem("Tools/PRTGI/Bake Reflection Probes")]
        private static void BakeReflectionsMenu() => BakeReflectionProbes(SceneManager.GetActiveScene());

        [MenuItem("Tools/PRTGI/Bake Reflection Normalization")]
        private static void BakeNormalizationMenu() => BakeAllReflectionNormalization(SceneManager.GetActiveScene());

        public static void BakeAllReflectionNormalization(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || IsBaking || Application.isPlaying) return;
            foreach (var data in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PRTReflectionProbeAdditionalData>(true)))
                if (data.isActiveAndEnabled) BakeReflectionNormalization(data);
        }

        [MenuItem("Tools/PRTGI/Preview Probe Placement")]
        private static void PreviewMenu() => PreviewPlacement(FindVolume());

        [MenuItem("Tools/PRTGI/Preview Probe Placement (DXR)")]
        private static void PreviewRayTracedMenu() => PreviewPlacement(FindVolume(), true);

        [MenuItem("GameObject/Light/PRT Probe Volume", false, 300)]
        private static void CreateVolume(MenuCommand command)
        {
            var gameObject = new GameObject("PRT Probe Volume");
            GameObjectUtility.SetParentAndAlign(gameObject, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(gameObject, "Create PRT Probe Volume");
            gameObject.AddComponent<PRTProbeVolume>();
            Selection.activeGameObject = gameObject;
        }

        [MenuItem("GameObject/Light/PRT Probe Adjustment Volume", false, 301)]
        private static void CreateAdjustment(MenuCommand command)
        {
            var gameObject = new GameObject("PRT Probe Adjustment Volume");
            GameObjectUtility.SetParentAndAlign(gameObject, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(gameObject, "Create PRT Probe Adjustment Volume");
            gameObject.AddComponent<PRTProbeAdjustmentVolume>();
            Selection.activeGameObject = gameObject;
        }

        private static PRTProbeVolume FindVolume()
        {
            var selected = Selection.activeGameObject ? Selection.activeGameObject.GetComponentInParent<PRTProbeVolume>() : null;
            if (selected) return selected;
            var scene = SceneManager.GetActiveScene();
            return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PRTProbeVolume>(true)).FirstOrDefault();
        }

        public static async void GenerateLighting(PRTProbeVolume volume, bool useRayTracing = false)
        {
            if (!volume || IsBaking || Application.isPlaying) return;
            _cancellation = new CancellationTokenSource();
            try
            {
                await BakeVolumeAsync(volume, _cancellation.Token, (status, value) =>
                {
                    if (EditorUtility.DisplayCancelableProgressBar("PRTGI Bake", status, value)) _cancellation.Cancel();
                }, useRayTracing);
                Debug.Log("PRTGI: baked transport for " + volume.name + ".");
            }
            catch (OperationCanceledException) { Debug.Log("PRTGI: bake cancelled; previous asset retained."); }
            catch (Exception exception) { Debug.LogException(exception); }
            finally
            {
                EditorUtility.ClearProgressBar();
                _cancellation.Dispose();
                _cancellation = null;
            }
        }

        public static void CancelBaking() => _cancellation?.Cancel();

        public static async void PreviewPlacement(PRTProbeVolume volume, bool useRayTracing = false)
        {
            if (!volume || IsBaking || Application.isPlaying) return;
            _cancellation = new CancellationTokenSource();
            try
            {
                PRTProbeGrid grid = volume.BakeGrid;
                var adjustments = Adjustments(volume.gameObject.scene);
                var positions = new Vector3[grid.ProbeCount];
                var validity = new bool[positions.Length];
                if (useRayTracing)
                {
                    using (var rayTracing = new PRTRayTracingBakeScene(volume.gameObject.scene))
                    {
                        PrepareRayTracedPlacement(rayTracing, volume, grid, adjustments,
                            out positions, out validity, out _);
                    }
                }
                else using (var snapshot = new PRTCpuBakeScene(volume.gameObject.scene))
                    for (int i = 0; i < positions.Length; i++)
                    {
                        _cancellation.Token.ThrowIfCancellationRequested();
                        positions[i] = PlaceProbe(snapshot, volume, grid.GetPosition(i), adjustments,
                            out validity[i], out _);
                        if ((i & 31) != 31) continue;
                        if (EditorUtility.DisplayCancelableProgressBar("PRT Probe Placement",
                                $"Place probes {i + 1}/{positions.Length}", (float)(i + 1) / positions.Length))
                            _cancellation.Cancel();
                        await Task.Yield();
                    }
                _cancellation.Token.ThrowIfCancellationRequested();
                volume.SetPlacementPreview(positions, validity);
                SceneView.RepaintAll();
            }
            catch (OperationCanceledException) { Debug.Log("PRTGI: placement preview cancelled."); }
            catch (Exception exception) { Debug.LogException(exception); }
            finally
            {
                EditorUtility.ClearProgressBar();
                _cancellation.Dispose();
                _cancellation = null;
            }
        }

        public static void ClearBakedData(PRTProbeVolume volume)
        {
            if (!volume || !volume.asset || IsBaking) return;
            Undo.RecordObject(volume.asset, "Clear PRTGI Transport");
            volume.asset.Clear();
            EditorUtility.SetDirty(volume.asset);
            AssetDatabase.SaveAssets();
        }

        public static void BakeReflectionProbes(Scene scene)
        {
            if (!scene.IsValid() || string.IsNullOrEmpty(scene.path) || IsBaking || Application.isPlaying) return;
            string folder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(scene.path),
                System.IO.Path.GetFileNameWithoutExtension(scene.path)).Replace('\\', '/');
            System.IO.Directory.CreateDirectory(folder);
            foreach (var probe in scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<ReflectionProbe>(true)))
            {
                if (!probe.isActiveAndEnabled) continue;
                string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + probe.name + ".exr");
                if (!Lightmapping.BakeReflectionProbe(probe, path))
                    Debug.LogWarning("PRTGI: reflection probe bake failed for " + probe.name);
                if (probe.TryGetComponent(out PRTReflectionProbeAdditionalData additional))
                    BakeReflectionNormalization(additional);
            }
            AssetDatabase.Refresh();
        }

        public static void BakeReflectionNormalization(PRTReflectionProbeAdditionalData data)
        {
            if (!data || Application.isPlaying) return;
            var compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(
                "Assets/Mipipline/PipelineCore/PRTGI/Shaders/PRTReflectionProbeSample.compute");
            if (!compute || !compute.IsSupported(compute.FindKernel("CSMain")))
                throw new InvalidOperationException("PRT reflection normalization compute shader is unavailable.");
            var cameraObject = new GameObject("PRT reflection reference capture");
            SceneManager.MoveGameObjectToScene(cameraObject, data.gameObject.scene);
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.cullingMask = -1;
            camera.farClipPlane = 10000f;
            camera.transform.position = data.transform.position;
            var cube = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGBFloat)
            { dimension = UnityEngine.Rendering.TextureDimension.Cube, hideFlags = HideFlags.HideAndDontSave };
            cube.Create();
            int oldEnabled = Mathf.RoundToInt(Shader.GetGlobalFloat("_PRTEnabled"));
            try
            {
                PRTGlobalIlluminationFeature.SuppressRelightForCapture = true;
                Shader.SetGlobalFloat("_PRTEnabled", 0);
                if (!camera.RenderToCubemap(cube)) throw new InvalidOperationException("Reflection reference capture failed.");
                var directions = PRTBakeSampling.GenerateDirections(512, 0);
                using (var samples = new ComputeBuffer(directions.Length, 16))
                using (var coefficients = new ComputeBuffer(27, sizeof(float)))
                {
                    samples.SetData(directions);
                    int kernel = compute.FindKernel("CSMain");
                    compute.SetInt("_sampleCount", directions.Length);
                    compute.SetTexture(kernel, "_inputCubemap", cube);
                    compute.SetBuffer(kernel, "_sampleDirections", samples);
                    compute.SetBuffer(kernel, "_coefficientSH9", coefficients);
                    compute.Dispatch(kernel, 1, 1, 1);
                    var request = UnityEngine.Rendering.AsyncGPUReadback.Request(coefficients);
                    request.WaitForCompletion();
                    if (request.hasError) throw new InvalidOperationException("PRT reflection SH readback failed.");
                    var values = new float[27];
                    request.GetData<float>().CopyTo(values);
                    data.SetSHCoefficients(values);
                    EditorUtility.SetDirty(data);
                    EditorSceneManager.MarkSceneDirty(data.gameObject.scene);
                }
            }
            finally
            {
                PRTGlobalIlluminationFeature.SuppressRelightForCapture = false;
                Shader.SetGlobalFloat("_PRTEnabled", oldEnabled);
                cube.Release();
                UnityEngine.Object.DestroyImmediate(cube);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        // Exposed as Task so editor automation can verify a small bake without changing the demo asset.
        public static async Task BakeVolumeAsync(PRTProbeVolume volume, CancellationToken token,
            Action<string, float> progress = null, bool useRayTracing = false)
        {
            if (!volume || !volume.gameObject.scene.IsValid() || string.IsNullOrEmpty(volume.gameObject.scene.path))
                throw new InvalidOperationException("Save the scene before baking a PRT probe volume.");
            if (volume.transform.rotation != Quaternion.identity || volume.transform.lossyScale != Vector3.one)
                throw new InvalidOperationException("PRT probe volumes require an unrotated, unscaled world-space grid.");
            PRTProbeGrid grid = volume.BakeGrid;
            if (grid.count.x < 1 || grid.count.y < 1 || grid.count.z < 1 || grid.spacing <= 0 ||
                volume.sectorWidth < 1 || volume.BakeSampleCount < 16 || volume.SurfelMergeDistance <= 0)
                throw new InvalidOperationException("PRT probe count, spacing, sample count, sector width and merge distance must be positive.");

            Hash128 settings = SettingsSignature(volume);
            Vector4[] directions = PRTBakeSampling.GenerateDirections(volume.BakeSampleCount, volume.BakeSeed);
            var adjustments = Adjustments(volume.gameObject.scene);
            progress?.Invoke("Snapshot Contribute GI meshes", 0f);
            using (var snapshot = useRayTracing ? null : new PRTCpuBakeScene(volume.gameObject.scene))
            using (var rayTracing = useRayTracing ? new PRTRayTracingBakeScene(volume.gameObject.scene) : null)
            {
                var partition = new PRTSectorBake(grid, volume.sectorWidth, volume.SurfelMergeDistance);
                Vector3[] rayPositions = null;
                bool[] rayValidity = null;
                float[] rayIntensity = null;
                if (rayTracing != null)
                {
                    progress?.Invoke("Place probes with DXR", 0.02f);
                    PrepareRayTracedPlacement(rayTracing, volume, grid, adjustments,
                        out rayPositions, out rayValidity, out rayIntensity);
                    token.ThrowIfCancellationRequested();
                }
                const int probesPerBatch = 4;
                for (int first = 0; first < grid.ProbeCount; first += probesPerBatch)
                {
                    token.ThrowIfCancellationRequested();
                    int count = Mathf.Min(probesPerBatch, grid.ProbeCount - first);
                    var samples = new PRTCaptureSample[count][];
                    var captures = new Vector3[count];
                    var offsets = new Vector3[count];
                    var validity = new uint[count];
                    for (int i = 0; i < count; i++)
                    {
                        int id = first + i;
                        Vector3 nominal = grid.GetPosition(id);
                        Vector3 capture;
                        bool valid;
                        float intensity;
                        if (rayTracing != null)
                        {
                            capture = rayPositions[id];
                            valid = rayValidity[id];
                            intensity = rayIntensity[id];
                        }
                        else capture = PlaceProbe(snapshot, volume, nominal, adjustments, out valid, out intensity);
                        offsets[i] = capture - nominal;
                        captures[i] = capture;
                        validity[i] = PRTProbeValidity.Pack(intensity, valid ? 1f : 0f);
                        if (rayTracing == null)
                        {
                            samples[i] = new PRTCaptureSample[directions.Length];
                            for (int direction = 0; direction < directions.Length; direction++)
                            {
                                if ((direction & 127) == 0) token.ThrowIfCancellationRequested();
                                samples[i][direction] = snapshot.Capture(capture, (Vector3)directions[direction]);
                            }
                        }
                    }
                    if (rayTracing != null)
                    {
                        token.ThrowIfCancellationRequested();
                        samples = rayTracing.Capture(captures, directions);
                    }
                    var batch = new PRTProbeBakeSamples[count];
                    for (int i = 0; i < count; i++) batch[i] = new PRTProbeBakeSamples(captures[i], samples[i]);
                    partition.AddProbes(first, batch, directions, offsets, validity);
                    progress?.Invoke($"Capture probes {first + count}/{grid.ProbeCount}",
                        0.05f + 0.85f * (first + count) / grid.ProbeCount);
                    token.ThrowIfCancellationRequested();
                    await Task.Yield();
                }
                progress?.Invoke("Serialize probe sectors", 0.92f);
                var sectors = partition.Complete();
                token.ThrowIfCancellationRequested();
                if (!grid.Equals(volume.BakeGrid) || settings != SettingsSignature(volume))
                    throw new InvalidOperationException("Probe settings changed during baking; transport was not saved.");
                var signature = new PRTBakeSignature
                {
                    geometry = rayTracing != null ? rayTracing.geometrySignature : snapshot.geometrySignature,
                    materials = rayTracing != null ? rayTracing.materialSignature : snapshot.materialSignature,
                    settings = settings,
                    authoringInputs = settings,
                    backend = rayTracing != null ? "DXRPathTracingMaterial-URP14" : "CPUPhysicsCapture-URP14",
                    sampleCount = directions.Length,
                    seed = volume.BakeSeed,
                    sceneTime = Shader.GetGlobalVector("_Time").y
                };
                PRTProbeVolumeAsset output = volume.asset ? volume.asset : ScriptableObject.CreateInstance<PRTProbeVolumeAsset>();
                try { output.SetBakedData(grid, signature, rayTracing != null ? rayTracing.bounds : snapshot.bounds,
                    volume.sectorWidth, partition.Metadata, sectors); }
                catch { if (!volume.asset) UnityEngine.Object.DestroyImmediate(output); throw; }
                if (!volume.asset)
                {
                    string scenePath = volume.gameObject.scene.path;
                    string folder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(scenePath),
                        System.IO.Path.GetFileNameWithoutExtension(scenePath)).Replace('\\', '/');
                    System.IO.Directory.CreateDirectory(folder);
                    string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + volume.name + "_ProbeVolume.asset");
                    AssetDatabase.CreateAsset(output, path);
                    volume.asset = output;
                    EditorUtility.SetDirty(volume);
                    EditorSceneManager.MarkSceneDirty(volume.gameObject.scene);
                }
                EditorUtility.SetDirty(output);
                AssetDatabase.SaveAssets();
                PRTStreamedAssetExporter.Convert(output);
                progress?.Invoke("PRT transport saved", 1f);
            }
        }

        private static PRTProbeAdjustmentVolume[] Adjustments(Scene scene) => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PRTProbeAdjustmentVolume>(true))
            .Where(item => item.isActiveAndEnabled)
            .OrderBy(item => GlobalObjectId.GetGlobalObjectIdSlow(item).ToString(), StringComparer.Ordinal).ToArray();

        private static void PrepareRayTracedPlacement(PRTRayTracingBakeScene scene, PRTProbeVolume volume,
            PRTProbeGrid grid, PRTProbeAdjustmentVolume[] adjustments,
            out Vector3[] positions, out bool[] validity, out float[] intensities)
        {
            int count = grid.ProbeCount;
            positions = new Vector3[count];
            validity = new bool[count];
            intensities = new float[count];
            var biases = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                Vector3 nominal = grid.GetPosition(i);
                positions[i] = nominal + (volume.enableBakePreprocess ? volume.virtualOffset : Vector3.zero);
                biases[i] = new Vector2(volume.geometryBias, volume.rayOriginBias);
                intensities[i] = 1f;
                validity[i] = true;
                foreach (var adjustment in adjustments)
                {
                    if (!adjustment.Contains(nominal)) continue;
                    intensities[i] *= adjustment.GetIntensityScale();
                    if (adjustment.ShouldInvalidateProbe()) validity[i] = false;
                    if (!volume.enableBakePreprocess) continue;
                    if (adjustment.mode == PRTProbeAdjustmentMode.ApplyVirtualOffset)
                        positions[i] += adjustment.GetAdditionalVirtualOffset();
                    else if (adjustment.mode == PRTProbeAdjustmentMode.OverrideVirtualOffsetSettings)
                        biases[i] = new Vector2(adjustment.geometryBias, adjustment.rayOriginBias);
                }
            }
            if (!volume.enableBakePreprocess) return;
            Vector4[] placed = scene.Place(positions, biases, grid.spacing);
            var downward = new System.Collections.Generic.List<int>();
            for (int i = 0; i < count; i++)
                if (placed[i].w > 0.5f && placed[i].y < -biases[i].x - 0.01f)
                    downward.Add(i);
            // Some imported meshes have inverted triangle winding on their upper face.
            // The DXR inside/outside vote can then move a floor probe below the slab.
            // Check the nearby top surface from above before accepting that exit.
            if (downward.Count > 0)
            {
                var overhead = new Vector3[downward.Count];
                float height = Mathf.Min(grid.spacing * 0.5f, 1.5f);
                for (int i = 0; i < downward.Count; i++)
                    overhead[i] = positions[downward[i]] + Vector3.up * height;
                PRTCaptureSample[][] surface = scene.Capture(overhead, new[] { new Vector4(0, -1, 0, 1) });
                for (int i = 0; i < downward.Count; i++)
                {
                    int probe = downward[i];
                    PRTCaptureSample hit = surface[i][0];
                    float rise = hit.position.y - positions[probe].y;
                    if ((hit.flags & PRTCaptureSample.SkyMiss) != 0 || hit.normal.y < 0.7f ||
                        rise <= 0f || rise > Mathf.Min(grid.spacing * 0.25f, 1f)) continue;
                    Vector3 corrected = hit.position + Vector3.up * biases[probe].x;
                    placed[probe] = new Vector4(corrected.x - positions[probe].x,
                        corrected.y - positions[probe].y, corrected.z - positions[probe].z, 1f);
                }
            }
            for (int i = 0; i < count; i++)
            {
                positions[i] += (Vector3)placed[i];
                validity[i] &= placed[i].w > 0.5f;
            }
        }

        private static Vector3 PlaceProbe(PRTCpuBakeScene snapshot, PRTProbeVolume volume, Vector3 nominal,
            PRTProbeAdjustmentVolume[] adjustments, out bool valid, out float intensity)
        {
            Vector3 initial = nominal;
            float bias = volume.geometryBias;
            bool invalid = false;
            intensity = 1f;
            if (volume.enableBakePreprocess) initial += volume.virtualOffset;
            foreach (var adjustment in adjustments)
            {
                if (!adjustment.Contains(nominal)) continue;
                intensity *= adjustment.GetIntensityScale();
                if (adjustment.ShouldInvalidateProbe()) invalid = true;
                if (!volume.enableBakePreprocess) continue;
                if (adjustment.mode == PRTProbeAdjustmentMode.ApplyVirtualOffset)
                    initial += adjustment.GetAdditionalVirtualOffset();
                else if (adjustment.mode == PRTProbeAdjustmentMode.OverrideVirtualOffsetSettings)
                    bias = adjustment.geometryBias;
            }
            bool placed = true;
            Vector3 capture = volume.enableBakePreprocess
                ? snapshot.Place(initial, bias, volume.probeGridSize, out placed) : initial;
            valid = placed && !invalid;
            return capture;
        }

        private static Hash128 SettingsSignature(PRTProbeVolume volume)
        {
            Hash128 hash = default;
            var grid = volume.BakeGrid;
            hash.Append(grid.count.x); hash.Append(grid.count.y); hash.Append(grid.count.z);
            hash.Append(grid.origin.x); hash.Append(grid.origin.y); hash.Append(grid.origin.z);
            hash.Append(grid.spacing); hash.Append(volume.BakeSampleCount);
            hash.Append(unchecked((int)volume.BakeSeed)); hash.Append(volume.sectorWidth);
            hash.Append(volume.SurfelMergeDistance); hash.Append(volume.geometryBias);
            hash.Append(volume.rayOriginBias); hash.Append(volume.enableBakePreprocess ? 1 : 0);
            hash.Append(volume.virtualOffset.x); hash.Append(volume.virtualOffset.y); hash.Append(volume.virtualOffset.z);
            foreach (var item in volume.gameObject.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<PRTProbeAdjustmentVolume>(true))
                .Where(item => item.isActiveAndEnabled)
                .OrderBy(item => GlobalObjectId.GetGlobalObjectIdSlow(item).ToString(), StringComparer.Ordinal))
            {
                hash.Append(GlobalObjectId.GetGlobalObjectIdSlow(item).ToString());
                hash.Append((int)item.mode); hash.Append((int)item.shape);
                for (int i = 0; i < 16; i++) hash.Append(item.transform.localToWorldMatrix[i]);
                hash.Append(item.size.x); hash.Append(item.size.y); hash.Append(item.size.z);
                hash.Append(item.radius); hash.Append(item.intensityScale);
                hash.Append(item.geometryBias); hash.Append(item.rayOriginBias);
                hash.Append(item.virtualOffsetRotation); hash.Append(item.virtualOffsetDistance);
            }
            return hash;
        }
    }
}
