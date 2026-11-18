using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Baozang.Rendering.VolumetricClouds.Editor
{
    /// <summary>
    /// Small authoring tool for painting the RGBA cloud map used by the URP volumetric clouds.
    /// R = coverage, G = rain, B = cloud type, A = max height.
    /// </summary>
    public sealed class VolumetricCloudMapPainterWindow : EditorWindow
    {
        enum PaintChannel
        {
            Coverage,
            Rain,
            CloudType,
            MaxHeight,
        }

        enum ScenePaintSurface
        {
            CloudMidPlane,
            CloudBottomPlane,
            GroundOrCollider,
        }

        const string k_DefaultPath = "Assets/RenderFeature/VolumetricClouds/Texture/PaintedCloudMap.png";

        VolumeProfile m_Profile;
        Texture2D m_Texture;
        string m_SavePath = k_DefaultPath;
        int m_NewResolution = 256;
        PaintChannel m_Channel = PaintChannel.Coverage;
        float m_BrushSize = 24f;
        float m_BrushStrength = 1f;
        float m_BrushValue = 1f;
        float m_BrushFalloff = 0.65f;
        bool m_EnableScenePainting;
        float m_SceneBrushRadius = 500f;
        float m_MapWorldSize = 8000f;
        Vector2 m_MapWorldOffset;
        ScenePaintSurface m_ScenePaintSurface = ScenePaintSurface.CloudMidPlane;
        float m_GroundPlaneY;
        bool m_WorldLocked = true;
        bool m_AssignLocalClouds = true;
        bool m_Dirty;
        Vector2 m_Scroll;

        [MenuItem("Tools/Baozang/Volumetric Clouds/Cloud Map Painter")]
        static void Open()
        {
            GetWindow<VolumetricCloudMapPainterWindow>("Cloud Map Painter");
        }

        void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        void OnGUI()
        {
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);

            EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                m_Profile = (VolumeProfile)EditorGUILayout.ObjectField("Volume Profile", m_Profile, typeof(VolumeProfile), false);
                if (GUILayout.Button("From Selection", GUILayout.Width(110f)))
                    PickProfileFromSelection();
            }

            m_Texture = (Texture2D)EditorGUILayout.ObjectField("Cloud Map", m_Texture, typeof(Texture2D), false);
            m_SavePath = EditorGUILayout.TextField("Save Path", m_SavePath);

            using (new EditorGUILayout.HorizontalScope())
            {
                m_NewResolution = EditorGUILayout.IntPopup("New Size", m_NewResolution,
                    new[] { "64", "128", "256", "512" },
                    new[] { 64, 128, 256, 512 });
                if (GUILayout.Button("Create", GUILayout.Width(90f)))
                    CreateNewTexture();
            }

            using (new EditorGUI.DisabledScope(m_Profile == null || m_Texture == null))
            {
                m_WorldLocked = EditorGUILayout.ToggleLeft("World Locked: set Cloud Map Speed to 0 when assigning", m_WorldLocked);
                m_AssignLocalClouds = EditorGUILayout.ToggleLeft("Scene painting needs Local Clouds: enable Local Clouds when assigning", m_AssignLocalClouds);
                if (GUILayout.Button("Assign To Volume Profile"))
                    AssignToProfile();
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Brush", EditorStyles.boldLabel);
            m_Channel = (PaintChannel)EditorGUILayout.EnumPopup("Channel", m_Channel);
            m_BrushValue = EditorGUILayout.Slider("Value", m_BrushValue, 0f, 1f);
            m_BrushStrength = EditorGUILayout.Slider("Strength", m_BrushStrength, 0.01f, 1f);
            m_BrushSize = EditorGUILayout.Slider("Size (pixels)", m_BrushSize, 1f, 128f);
            m_BrushFalloff = EditorGUILayout.Slider("Soft Edge", m_BrushFalloff, 0.01f, 1f);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Scene View Paint", EditorStyles.boldLabel);
            m_EnableScenePainting = EditorGUILayout.ToggleLeft("Enable Scene View Painting", m_EnableScenePainting);
            using (new EditorGUILayout.HorizontalScope())
            {
                m_MapWorldSize = EditorGUILayout.FloatField("Map World Size (m)", m_MapWorldSize);
                if (GUILayout.Button("Use Profile", GUILayout.Width(90f)))
                    PullMapTransformFromProfile();
            }
            m_MapWorldSize = Mathf.Max(1f, m_MapWorldSize);
            m_MapWorldOffset = EditorGUILayout.Vector2Field("Map Center XZ", m_MapWorldOffset);
            m_ScenePaintSurface = (ScenePaintSurface)EditorGUILayout.EnumPopup("Paint Surface", m_ScenePaintSurface);
            m_SceneBrushRadius = EditorGUILayout.Slider("World Radius (m)", m_SceneBrushRadius, 10f, 5000f);
            m_GroundPlaneY = EditorGUILayout.FloatField("Fallback Plane Y", m_GroundPlaneY);
            EditorGUILayout.HelpBox(
                "Scene painting maps world XZ to the same CloudMap UV used by the shader. Use CloudMidPlane to avoid ground-to-cloud parallax. It is accurate only for Local Clouds; distant sky clouds are camera-relative.",
                MessageType.Info);

            DrawChannelHelp();
            DrawPreview();

            using (new EditorGUI.DisabledScope(m_Texture == null || !m_Dirty))
            {
                if (GUILayout.Button("Save PNG"))
                    SaveTexture();
            }

            EditorGUILayout.EndScrollView();
        }

        void OnSceneGUI(SceneView sceneView)
        {
            if (!m_EnableScenePainting || m_Texture == null)
                return;

            Event e = Event.current;
            if (e.alt)
                return;

            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (!TryGetPaintPoint(ray, out Vector3 point))
                return;

            Handles.color = e.shift ? new Color(1f, 0.35f, 0.2f, 0.9f) : new Color(0.25f, 0.75f, 1f, 0.9f);
            DrawMapBounds();
            Handles.DrawWireDisc(point, Vector3.up, m_SceneBrushRadius);
            Handles.Label(point + Vector3.up * Mathf.Max(2f, m_SceneBrushRadius * 0.05f),
                e.shift ? "Erase CloudMap" : "Paint CloudMap");

            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlId);

            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
            {
                PaintWorld(point, e.shift ? 0f : m_BrushValue);
                e.Use();
            }

            sceneView.Repaint();
        }

        bool TryGetPaintPoint(Ray ray, out Vector3 point)
        {
            if (m_ScenePaintSurface == ScenePaintSurface.CloudMidPlane)
                return TryRayPlane(ray, GetCloudMidPlaneY(), out point);

            if (m_ScenePaintSurface == ScenePaintSurface.CloudBottomPlane)
                return TryRayPlane(ray, GetCloudBottomPlaneY(), out point);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000000f))
            {
                point = hit.point;
                return true;
            }

            return TryRayPlane(ray, m_GroundPlaneY, out point);
        }

        static bool TryRayPlane(Ray ray, float planeY, out Vector3 point)
        {
            Plane plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
            if (plane.Raycast(ray, out float enter))
            {
                point = ray.GetPoint(enter);
                return true;
            }

            point = default;
            return false;
        }

        float GetCloudBottomPlaneY()
        {
            if (m_Profile != null && m_Profile.TryGet(out VolumetricCloudsVolume volume))
                return Mathf.Max(1f, volume.bottomAltitude.value);
            return 1200f;
        }

        float GetCloudMidPlaneY()
        {
            if (m_Profile != null && m_Profile.TryGet(out VolumetricCloudsVolume volume))
                return Mathf.Max(1f, volume.bottomAltitude.value) + Mathf.Max(1f, volume.altitudeRange.value) * 0.5f;
            return 2200f;
        }

        void PaintWorld(Vector3 worldPoint, float targetValue)
        {
            float worldSize = GetCloudMapWorldSize();
            Vector2 worldOffset = GetCloudMapWorldOffset();
            Vector2 uv = WorldToCloudMapUV(worldPoint, worldSize, worldOffset);
            int centerX = Mathf.FloorToInt(uv.x * m_Texture.width);
            int centerY = Mathf.FloorToInt(uv.y * m_Texture.height);
            float radiusPixels = Mathf.Max(1f, m_SceneBrushRadius / Mathf.Max(worldSize, 1f) * m_Texture.width);

            Paint(centerX, centerY, targetValue, radiusPixels, true);
        }

        Vector2 WorldToCloudMapUV(Vector3 worldPoint, float worldSize, Vector2 worldOffset)
        {
            Vector2 uv = (new Vector2(worldPoint.x, worldPoint.z) - worldOffset) / Mathf.Max(worldSize, 1f) + Vector2.one * 0.5f;
            uv.x = Repeat01(uv.x);
            uv.y = Repeat01(uv.y);
            return uv;
        }

        void DrawMapBounds()
        {
            float half = GetCloudMapWorldSize() * 0.5f;
            Vector2 center = GetCloudMapWorldOffset();
            float y = GetActivePaintPlaneY();
            Vector3 p0 = new Vector3(center.x - half, y, center.y - half);
            Vector3 p1 = new Vector3(center.x - half, y, center.y + half);
            Vector3 p2 = new Vector3(center.x + half, y, center.y + half);
            Vector3 p3 = new Vector3(center.x + half, y, center.y - half);

            Color old = Handles.color;
            Handles.color = new Color(0.1f, 0.8f, 1f, 0.35f);
            Handles.DrawAAPolyLine(2f, p0, p1, p2, p3, p0);
            Handles.Label(new Vector3(center.x, y, center.y), "CloudMap Center");
            Handles.color = old;
        }

        float GetActivePaintPlaneY()
        {
            return m_ScenePaintSurface switch
            {
                ScenePaintSurface.CloudMidPlane => GetCloudMidPlaneY(),
                ScenePaintSurface.CloudBottomPlane => GetCloudBottomPlaneY(),
                _ => m_GroundPlaneY
            };
        }

        float GetCloudMapWorldSize()
        {
            return Mathf.Max(1f, m_MapWorldSize);
        }

        Vector2 GetCloudMapWorldOffset()
        {
            return m_MapWorldOffset;
        }

        float GetLegacyNormalizationFactor()
        {
            float bottomAltitude = 1200f;
            float altitudeRange = 2000f;
            if (m_Profile != null && m_Profile.TryGet(out VolumetricCloudsVolume volume))
            {
                bottomAltitude = Mathf.Max(1f, volume.bottomAltitude.value);
                altitudeRange = Mathf.Max(1f, volume.altitudeRange.value);
            }

            const float earthRadius = 6378100f;
            float highestAltitude = bottomAltitude + altitudeRange;
            float midRadius = (bottomAltitude + highestAltitude) * 0.5f;
            return Mathf.Sqrt((earthRadius + midRadius) * (earthRadius + midRadius) - earthRadius * earthRadius);
        }

        void PullMapTransformFromProfile()
        {
            if (m_Profile == null || !m_Profile.TryGet(out VolumetricCloudsVolume volume))
                return;

            m_MapWorldSize = volume.cloudMapWorldSize.value > 0f ? volume.cloudMapWorldSize.value : GetLegacyNormalizationFactor();
            m_MapWorldOffset = volume.cloudMapWorldOffset.value;
        }

        void DrawChannelHelp()
        {
            string text = m_Channel switch
            {
                PaintChannel.Coverage => "Coverage(R): 0 = no cloud, 1 = allow dense clouds.",
                PaintChannel.Rain => "Rain(G): increases rain/storm density response in the shader.",
                PaintChannel.CloudType => "Cloud Type(B): horizontal lookup into the cloud profile LUT.",
                PaintChannel.MaxHeight => "Max Height(A): local normalized cloud height cap.",
                _ => string.Empty
            };
            EditorGUILayout.HelpBox(text + "\nHold Shift while painting to erase the selected channel to 0.", MessageType.Info);
        }

        void DrawPreview()
        {
            if (m_Texture == null)
            {
                EditorGUILayout.HelpBox("Create or assign a cloud map texture first.", MessageType.None);
                return;
            }

            float size = Mathf.Min(position.width - 32f, 512f);
            Rect rect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));
            GUI.DrawTexture(rect, m_Texture, ScaleMode.ScaleToFit, false);
            GUI.Box(rect, GUIContent.none);

            Event e = Event.current;
            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && rect.Contains(e.mousePosition) && e.button == 0)
            {
                Vector2 local = e.mousePosition - rect.position;
                int px = Mathf.Clamp(Mathf.FloorToInt(local.x / rect.width * m_Texture.width), 0, m_Texture.width - 1);
                int py = Mathf.Clamp(Mathf.FloorToInt((1f - local.y / rect.height) * m_Texture.height), 0, m_Texture.height - 1);
                Paint(px, py, e.shift ? 0f : m_BrushValue, m_BrushSize, false);
                e.Use();
            }
        }

        void Paint(int centerX, int centerY, float targetValue, float radiusPixels, bool wrap)
        {
            EnsureEditableTexture();

            int radius = Mathf.CeilToInt(radiusPixels);
            float radiusSq = Mathf.Max(1f, radiusPixels * radiusPixels);
            int minX = wrap ? centerX - radius : Mathf.Max(0, centerX - radius);
            int maxX = wrap ? centerX + radius : Mathf.Min(m_Texture.width - 1, centerX + radius);
            int minY = wrap ? centerY - radius : Mathf.Max(0, centerY - radius);
            int maxY = wrap ? centerY + radius : Mathf.Min(m_Texture.height - 1, centerY + radius);

            for (int y = minY; y <= maxY; ++y)
            {
                for (int x = minX; x <= maxX; ++x)
                {
                    float dSq = (x - centerX) * (x - centerX) + (y - centerY) * (y - centerY);
                    if (dSq > radiusSq)
                        continue;

                    float t = Mathf.Sqrt(dSq / radiusSq);
                    float weight = Mathf.Pow(1f - t, Mathf.Lerp(8f, 1f, m_BrushFalloff)) * m_BrushStrength;
                    int px = wrap ? RepeatIndex(x, m_Texture.width) : x;
                    int py = wrap ? RepeatIndex(y, m_Texture.height) : y;
                    Color c = m_Texture.GetPixel(px, py);
                    SetChannel(ref c, Mathf.Lerp(GetChannel(c), targetValue, weight));
                    m_Texture.SetPixel(px, py, c);
                }
            }

            m_Texture.Apply(false, false);
            m_Dirty = true;
            Repaint();
            SceneView.RepaintAll();
        }

        static float Repeat01(float value)
        {
            return value - Mathf.Floor(value);
        }

        static int RepeatIndex(int value, int size)
        {
            int result = value % size;
            return result < 0 ? result + size : result;
        }

        float GetChannel(Color c)
        {
            return m_Channel switch
            {
                PaintChannel.Coverage => c.r,
                PaintChannel.Rain => c.g,
                PaintChannel.CloudType => c.b,
                PaintChannel.MaxHeight => c.a,
                _ => c.r
            };
        }

        void SetChannel(ref Color c, float value)
        {
            value = Mathf.Clamp01(value);
            switch (m_Channel)
            {
                case PaintChannel.Coverage: c.r = value; break;
                case PaintChannel.Rain: c.g = value; break;
                case PaintChannel.CloudType: c.b = value; break;
                case PaintChannel.MaxHeight: c.a = value; break;
            }
        }

        void CreateNewTexture()
        {
            m_Texture = new Texture2D(m_NewResolution, m_NewResolution, TextureFormat.RGBA32, false, true)
            {
                name = Path.GetFileNameWithoutExtension(m_SavePath),
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
            };

            Color clear = new Color(0f, 0f, 0.25f, 1f);
            Color[] pixels = new Color[m_NewResolution * m_NewResolution];
            for (int i = 0; i < pixels.Length; ++i)
                pixels[i] = clear;

            m_Texture.SetPixels(pixels);
            m_Texture.Apply(false, false);
            m_Dirty = true;
        }

        void EnsureEditableTexture()
        {
            if (m_Texture == null || m_Texture.isReadable)
                return;

            string path = AssetDatabase.GetAssetPath(m_Texture);
            if (string.IsNullOrEmpty(path))
                return;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            m_Texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        void SaveTexture()
        {
            if (m_Texture == null)
                return;

            string path = m_SavePath;
            if (string.IsNullOrWhiteSpace(path))
                path = k_DefaultPath;
            if (!path.StartsWith("Assets/"))
                path = k_DefaultPath;
            if (!path.EndsWith(".png"))
                path += ".png";

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllBytes(path, m_Texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.sRGBTexture = false;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            m_Texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            m_SavePath = path;
            m_Dirty = false;
        }

        void AssignToProfile()
        {
            if (m_Profile == null || m_Texture == null)
                return;

            if (m_Dirty)
                SaveTexture();

            Undo.RecordObject(m_Profile, "Assign Volumetric Cloud Map");
            if (!m_Profile.TryGet(out VolumetricCloudsVolume volume))
                volume = m_Profile.Add<VolumetricCloudsVolume>(true);

            volume.cloudControl.value = VolumetricCloudsVolume.CloudControl.Manual;
            volume.cloudControl.overrideState = true;
            volume.cloudMap.value = m_Texture;
            volume.cloudMap.overrideState = true;
            volume.cloudMapWorldSize.value = Mathf.Max(1f, m_MapWorldSize);
            volume.cloudMapWorldSize.overrideState = true;
            volume.cloudMapWorldOffset.value = m_MapWorldOffset;
            volume.cloudMapWorldOffset.overrideState = true;

            if (m_AssignLocalClouds)
            {
                volume.localClouds.value = true;
                volume.localClouds.overrideState = true;
            }

            if (m_WorldLocked)
            {
                volume.cloudMapSpeedMultiplier.value = 0f;
                volume.cloudMapSpeedMultiplier.overrideState = true;
            }

            EditorUtility.SetDirty(m_Profile);
            AssetDatabase.SaveAssets();
        }

        void PickProfileFromSelection()
        {
            GameObject go = Selection.activeGameObject;
            if (go == null)
                return;

            Volume volume = go.GetComponent<Volume>();
            if (volume == null)
                volume = go.GetComponentInParent<Volume>();

            if (volume != null)
                m_Profile = volume.sharedProfile != null ? volume.sharedProfile : volume.profile;
        }
    }
}
