using Illusion.Rendering.PRTGI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Illusion.Rendering.PRTGI.Editor
{
    [CustomEditor(typeof(PRTProbeVolume))]
    internal sealed class PRTProbeVolumeEditor : UnityEditor.Editor
    {
        private bool _gridOpen = true, _placementOpen = true, _relightOpen = true;
        private bool _debugOpen = true, _bakeOpen = true;
        private double _nextStatusRepaint;
        private PRTProbeVolumeAsset _validatedAsset;
        private int _validatedVersion = -1;
        private bool _valid;
        private string _validationReason;

        private void OnEnable() => EditorApplication.update += RepaintRuntimeStatus;
        private void OnDisable() => EditorApplication.update -= RepaintRuntimeStatus;

        private void RepaintRuntimeStatus()
        {
            if (!target || EditorApplication.timeSinceStartup < _nextStatusRepaint) return;
            _nextStatusRepaint = EditorApplication.timeSinceStartup + 0.25;
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            var volume = (PRTProbeVolume)target;
            serializedObject.Update();
            DrawAssetStatus(volume);
            using (new EditorGUI.DisabledScope(PRTBakeManager.IsBaking))
            {
                DrawGridSettings();
                DrawPlacementSettings();
                DrawRelightSettings();
                DrawDebugSettings(volume);
                DrawBakeSettings();
            }
            serializedObject.ApplyModifiedProperties();
            using (new EditorGUI.DisabledScope(Application.isPlaying)) DrawActions(volume);
        }

        private void DrawAssetStatus(PRTProbeVolume volume)
        {
            var data = volume.asset;
            if (!data)
            {
                EditorGUILayout.HelpBox("Assign a probe volume asset, or generate lighting to create one.", MessageType.Warning);
                return;
            }
            if (data != _validatedAsset || data.ContentVersion != _validatedVersion)
            {
                _validatedAsset = data;
                _validatedVersion = data.ContentVersion;
                _valid = data.TryValidate(out _validationReason);
            }
            if (!_valid) EditorGUILayout.HelpBox(_validationReason, MessageType.Warning);
            else
            {
                EditorGUILayout.HelpBox($"{data.Probes.Length:N0} probes · {data.SectorCount} sectors · " +
                    $"{data.Signature.backend}" + (data.IsStreamed ? " · disk streamed" : " · embedded"), MessageType.Info);
                if (!data.Grid.Equals(volume.BakeGrid))
                    EditorGUILayout.HelpBox("Probe placement differs from the baked asset. Rebake transport to enable relight.", MessageType.Warning);
            }
        }

        private void DrawGridSettings()
        {
            _gridOpen = EditorGUILayout.Foldout(_gridOpen, "Probe Grid", true);
            if (!_gridOpen) return;
            EditorGUI.indentLevel++;
            SerializedProperty x = Property("probeSizeX"), y = Property("probeSizeY"), z = Property("probeSizeZ");
            var count = new Vector3Int(x.intValue, y.intValue, z.intValue);
            EditorGUI.BeginChangeCheck();
            count = EditorGUILayout.Vector3IntField(new GUIContent("Probe Count", "Number of probes on each axis."), count);
            if (EditorGUI.EndChangeCheck())
            {
                x.intValue = Mathf.Clamp(count.x, 1, 128);
                y.intValue = Mathf.Clamp(count.y, 1, 64);
                z.intValue = Mathf.Clamp(count.z, 1, 128);
            }
            EditorGUILayout.PropertyField(Property("probeGridSize"), new GUIContent("Probe Spacing"));
            int width = Mathf.Max(1, Property("sectorWidth").intValue);
            int sectors = (count.x + width - 1) / width * ((count.z + width - 1) / width);
            float spacing = Property("probeGridSize").floatValue;
            EditorGUILayout.LabelField($"{count.x * count.y * count.z:N0} probes · " +
                $"{(count.x - 1) * spacing:0.#} × {(count.y - 1) * spacing:0.#} × {(count.z - 1) * spacing:0.#} m · {sectors} sectors", EditorStyles.miniLabel);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        private void DrawPlacementSettings()
        {
            _placementOpen = EditorGUILayout.Foldout(_placementOpen, "Probe Placement", true);
            if (!_placementOpen) return;
            EditorGUI.indentLevel++;
            var automatic = Property("enableBakePreprocess");
            EditorGUILayout.PropertyField(automatic, new GUIContent("Auto Placement", "Search for positions outside solid geometry during baking."));
            if (automatic.boolValue)
            {
                EditorGUILayout.PropertyField(Property("virtualOffset"), new GUIContent("Offset"));
                EditorGUILayout.PropertyField(Property("geometryBias"), new GUIContent("Geometry Bias"));
                EditorGUILayout.PropertyField(Property("rayOriginBias"), new GUIContent("Ray Origin Bias"));
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        private void DrawRelightSettings()
        {
            _relightOpen = EditorGUILayout.Foldout(_relightOpen, "Relight", true);
            if (!_relightOpen) return;
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(Property("usePRTGIForSceneObjects"), new GUIContent(
                "Scene Objects Use PRTGI", "Enable PRTGI for this scene's PipelineCore/PRTGI/Lit materials. Off uses the regular URP GI and stops probe relighting."));
            if (EditorGUI.EndChangeCheck())
            {
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
            }
            EditorGUILayout.PropertyField(Property("useGpuRelighting"), new GUIContent("GPU Compute", "Use the DX12 compute solver; otherwise use the CPU fallback."));
            EditorGUILayout.PropertyField(Property("indirectFill"), new GUIContent("Indirect Fill", "Blend toward regular URP GI only where PRT indirect light is darker. Higher values brighten occluded surfaces but reduce their contrast."));
            EditorGUILayout.PropertyField(Property("sectorsPerFrame"), new GUIContent("Sectors Per Frame", "Maximum number of sectors updated on each render."));
            EditorGUILayout.PropertyField(Property("voxelProbeSize"), new GUIContent("Camera Window", "Probe slots per axis for each cascade."));
            EditorGUILayout.PropertyField(Property("cascadeCount"), new GUIContent("Cascades", "Maximum nested levels; levels that cover the whole volume are skipped."));
            EditorGUILayout.PropertyField(Property("uploadBudgetMiB"), new GUIContent("Upload Budget (MiB)", "Maximum sector transport uploaded per frame."));
            EditorGUILayout.PropertyField(Property("sectorBudgetMiB"), new GUIContent("Resident Budget (MiB)", "Maximum GPU memory for resident sector transport and visibility cache."));
            EditorGUILayout.PropertyField(Property("cpuSectorBudgetMiB"), new GUIContent("CPU Sector Budget (MiB)", "Maximum cached decoded sector transport on CPU; the current camera sector is protected."));
            EditorGUILayout.PropertyField(Property("enableRelightShadow"), new GUIContent("Shadows", "Sample URP shadow maps during relight and cache surfel visibility."));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        private void DrawDebugSettings(PRTProbeVolume volume)
        {
            _debugOpen = EditorGUILayout.Foldout(_debugOpen, "Debug", true);
            if (!_debugOpen) return;
            var mode = Property("debugMode");
            EditorGUI.BeginChangeCheck();
            var visualization = (PRTProbeVisualizationMode)EditorGUILayout.EnumPopup(
                new GUIContent("Visualization", "Scene view visualization of this probe volume."),
                (PRTProbeVisualizationMode)mode.intValue);
            if (EditorGUI.EndChangeCheck()) { mode.intValue = (int)visualization; SceneView.RepaintAll(); }
            EditorGUI.indentLevel++;
            if (visualization == PRTProbeVisualizationMode.ProbeRadiance)
            {
                var selectedMode = Property("selectedProbeDebugMode");
                EditorGUI.BeginChangeCheck();
                var option = (PRTSelectedProbeDebugMode)EditorGUILayout.EnumPopup(
                    new GUIContent("Selected Probe", "What to draw for the probe selected in the Scene view."),
                    (PRTSelectedProbeDebugMode)selectedMode.intValue);
                if (EditorGUI.EndChangeCheck()) { selectedMode.intValue = (int)option; SceneView.RepaintAll(); }
                if (option == PRTSelectedProbeDebugMode.IrradianceSphere &&
                    volume.RuntimeRelightBackend != "GPU Compute")
                    EditorGUILayout.HelpBox("Live SH irradiance spheres require GPU Compute relight. Gray markers indicate that the debug SH buffer is unavailable.", MessageType.Info);
                if ((option == PRTSelectedProbeDebugMode.SphereDistribution ||
                          option == PRTSelectedProbeDebugMode.SampleDirection) && volume.asset &&
                         volume.asset.HasValidData && volume.asset.Grid.Equals(volume.BakeGrid) &&
                         volume.SelectedProbeIndex >= 0 && volume.SelectedProbeIndex < volume.asset.Probes.Length)
                {
                    int index = volume.SelectedProbeIndex;
                    var sector = volume.asset.ReadSector(volume.asset.SectorIndex(index));
                    var probe = sector.probes[volume.asset.LocalProbeIndex(index)];
                    int surfels = 0;
                    for (int i = probe.factorStart; i < probe.factorStart + probe.factorCount; i++)
                        surfels += sector.bricks[sector.factors[i].BrickIndex].count;
                    EditorGUILayout.LabelField("Hit Surfels", $"Probe {index}: showing up to {Mathf.Min(surfels, 1024):N0} of {surfels:N0}; sky misses omitted", EditorStyles.wordWrappedLabel);
                }
            }
            else if (visualization == PRTProbeVisualizationMode.ShadowCache)
            {
                EditorGUILayout.PropertyField(Property("shadowCacheDebugReadbackInterval"), new GUIContent("Readback Interval"));
                EditorGUILayout.PropertyField(Property("shadowCacheDebugShowLabels"), new GUIContent("Show Labels"));
                EditorGUILayout.PropertyField(Property("shadowCacheDebugSurfelSize"), new GUIContent("Surfel Size"));
                EditorGUILayout.LabelField("Snapshot", volume.HasShadowCacheDebugSnapshot
                    ? $"Scene tick {volume.LatestShadowCacheDebugFrameIndex} · sector {volume.ShadowPreviewSector}"
                    : "Waiting for readback");
                if (volume.HasShadowCacheDebugSnapshot)
                {
                    EditorGUILayout.LabelField("Preview Light", volume.ShadowPreviewLightName);
                    if (volume.TryGetShadowCacheSummary(volume.SelectedProbeIndex,
                            out float mean, out int cached, out int uncovered))
                        EditorGUILayout.LabelField("Selected Probe", $"{volume.SelectedProbeIndex}: " +
                            $"visibility {mean:F2} · {cached:N0} cached · {uncovered:N0} uncovered");
                }
            }
            if (visualization != PRTProbeVisualizationMode.None)
                EditorGUILayout.PropertyField(Property("probeHandleSize"), new GUIContent("Handle Size"));
            if (visualization == PRTProbeVisualizationMode.ProbeGridWithVirtualOffset)
            {
                using (new EditorGUI.DisabledScope(Application.isPlaying || PRTBakeManager.IsBaking))
                    if (GUILayout.Button("Preview Placement"))
                        PRTBakeManager.PreviewPlacement(volume, CanUseDxr());
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.PropertyField(Property("enableShadowCacheStats"), new GUIContent("Shadow Statistics", "Read back evaluated surfels and cache counters."));
            if (Property("enableShadowCacheStats").boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(Property("shadowCacheStatsReadbackInterval"), new GUIContent("Statistics Interval", "Render ticks between GPU counter readbacks."));
                EditorGUI.indentLevel--;
            }
            DrawRuntimeStatus(volume);
            EditorGUILayout.Space();
        }

        private void DrawBakeSettings()
        {
            _bakeOpen = EditorGUILayout.Foldout(_bakeOpen, "Baking", true);
            if (!_bakeOpen) return;
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(Property("asset"), new GUIContent("Asset", "Baked transport created beside the scene if unset."));
            EditorGUILayout.PropertyField(Property("bakeSampleCount"), new GUIContent("Samples Per Probe"));
            EditorGUILayout.PropertyField(Property("bakeSeed"), new GUIContent("Sampling Seed"));
            EditorGUILayout.PropertyField(Property("surfelMergeDistance"), new GUIContent("Surfel Merge Distance"));
            EditorGUILayout.PropertyField(Property("sectorWidth"), new GUIContent("Sector Size", "Probe columns per sector on X and Z."));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        private void DrawRuntimeStatus(PRTProbeVolume volume)
        {
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
            if (!volume.usePRTGIForSceneObjects)
            {
                EditorGUILayout.HelpBox("PRTGI is disabled for this scene. PRT Lit materials use regular URP GI.", MessageType.Info);
                EditorGUILayout.Space();
                return;
            }
            if (volume.RuntimeRelightActive)
            {
                EditorGUILayout.LabelField("Backend", volume.RuntimeRelightBackend);
                EditorGUILayout.LabelField("Sectors", $"{volume.RuntimeUpdatedSectorsThisFrame} this render · {volume.RuntimeTotalSectorUpdates} total updates");
                EditorGUILayout.LabelField("Scheduler", $"near {volume.RuntimeNearSector} · next background {volume.RuntimeBackgroundSector}");
                EditorGUILayout.LabelField("Cascades", $"{volume.RuntimeCascadeCount} levels · {volume.RuntimeCascadeSlots.x} × {volume.RuntimeCascadeSlots.z} × {volume.RuntimeCascadeSlots.y * 9 * volume.RuntimeCascadeCount} texture");
                EditorGUILayout.LabelField("Sector Memory", $"{FormatBytes(volume.RuntimeResidentBytes)} · peak {FormatBytes(volume.RuntimePeakResidentBytes)}");
                EditorGUILayout.LabelField("CPU Sector Cache", $"{FormatBytes(volume.RuntimeCachedCpuSectorBytes)} · {volume.RuntimePendingCpuSectors} loading");
                EditorGUILayout.LabelField("Sector Activity", $"{volume.RuntimeUploadingSectors} uploading · {volume.RuntimeEvictingSectors} evicting");
                EditorGUILayout.LabelField("Upload / Publish", $"{FormatBytes(volume.RuntimeFrameUploadBytes)} · {volume.RuntimePublishedCells:N0} nodes");
                if (!string.IsNullOrEmpty(volume.RuntimeResidencyPressure))
                    EditorGUILayout.HelpBox(volume.RuntimeResidencyPressure, MessageType.Warning);
                if (volume.enableShadowCacheStats && volume.RuntimeShadowStatsValid)
                {
                    EditorGUILayout.LabelField("Solver Residual", "Unavailable in URP 14 adapter");
                    if (volume.enableRelightShadow)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Shadow Cache", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField("Preview Light", volume.ShadowPreviewLightName ?? "Waiting for readback");
                        EditorGUILayout.LabelField("Visibility", $"{volume.RuntimeShadowMapSamples:N0} shadow map · " +
                            $"{volume.RuntimeShadowCacheHits:N0} cached · {volume.RuntimeShadowUnknown:N0} unknown " +
                            $"of {volume.RuntimeShadowEvaluated:N0} evaluated");
                    }
                }
                EditorGUILayout.LabelField("Frame", $"{volume.RuntimeLastRelightFrame} · CPU scheduling {volume.RuntimeRelightMilliseconds:F1} ms");
            }
            else EditorGUILayout.HelpBox("Relight starts when a camera using URP-HighFidelity-Renderer renders a valid baked volume.", MessageType.Info);
            EditorGUILayout.Space();
        }

        private void DrawActions(PRTProbeVolume volume)
        {
            if (PRTBakeManager.IsBaking)
            {
                if (GUILayout.Button("Cancel Baking")) PRTBakeManager.CancelBaking();
                return;
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Lighting")) PRTBakeManager.GenerateLighting(volume, CanUseDxr());
            if (GUILayout.Button("▼", GUILayout.Width(26f)))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Generate Lighting (CPU)"), false, () => PRTBakeManager.GenerateLighting(volume));
                if (CanUseDxr())
                    menu.AddItem(new GUIContent("Generate Lighting (DXR)"), false, () => PRTBakeManager.GenerateLighting(volume, true));
                else menu.AddDisabledItem(new GUIContent("Generate Lighting (DXR)"));
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Bake Reflection Probe Normalization"), false,
                    () => PRTBakeManager.BakeAllReflectionNormalization(volume.gameObject.scene));
                if (volume.asset && volume.asset.HasValidData)
                    menu.AddItem(new GUIContent("Clear Baked Data"), false, () =>
                    {
                        if (EditorUtility.DisplayDialog("Clear PRT Transport", "Remove the baked probe data?", "Clear", "Cancel"))
                            PRTBakeManager.ClearBakedData(volume);
                    });
                else menu.AddDisabledItem(new GUIContent("Clear Baked Data"));
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();
        }

        private static bool CanUseDxr() => SystemInfo.supportsRayTracing &&
            SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12;

        private SerializedProperty Property(string name) => serializedObject.FindProperty(name);

        private static string FormatBytes(long bytes) => bytes >= 1048576
            ? $"{bytes / 1048576.0:0.##} MiB" : bytes >= 1024
                ? $"{bytes / 1024.0:0.#} KiB" : $"{bytes} B";

        private void OnSceneGUI()
        {
            var volume = (PRTProbeVolume)target;
            var mode = (PRTProbeVisualizationMode)Property("debugMode").intValue;
            if (mode != PRTProbeVisualizationMode.ProbeRadiance && mode != PRTProbeVisualizationMode.ShadowCache) return;
            int count = volume.BakeGrid.ProbeCount;
            int selected = volume.SelectedProbeIndex;
            if (selected >= 0 && selected < count)
            {
                Vector3 position = volume.BakeGrid.GetPosition(selected);
                if (mode == PRTProbeVisualizationMode.ProbeRadiance && volume.asset && volume.asset.HasValidData && selected < volume.asset.Probes.Length)
                    position += volume.asset.Probes[selected].captureOffset;
                if (mode != PRTProbeVisualizationMode.ShadowCache || volume.ShadowDebugShowLabels)
                    Handles.Label(position + Vector3.up * 0.4f, $"Probe {selected}");
            }
            var current = Event.current;
            if (current.type != EventType.MouseDown || current.button != 0 || current.alt) return;
            Ray ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
            float nearest = float.PositiveInfinity;
            int picked = -1;
            float radius = Mathf.Max(0.1f, volume.probeGridSize * Property("probeHandleSize").floatValue * 0.18f);
            for (int i = 0; i < count; i++)
            {
                Vector3 candidate = volume.BakeGrid.GetPosition(i);
                if (mode == PRTProbeVisualizationMode.ProbeRadiance && volume.asset && volume.asset.HasValidData && i < volume.asset.Probes.Length)
                    candidate += volume.asset.Probes[i].captureOffset;
                Vector3 offset = candidate - ray.origin;
                float along = Vector3.Dot(offset, ray.direction);
                if (along <= 0 || along >= nearest || (offset - ray.direction * along).sqrMagnitude > radius * radius) continue;
                nearest = along;
                picked = i;
            }
            if (picked < 0) return;
            Undo.RecordObject(volume, "Select PRT Probe");
            Property("selectedProbeIndex").intValue = picked;
            serializedObject.ApplyModifiedProperties();
            current.Use();
            Repaint();
            SceneView.RepaintAll();
        }
    }
}
