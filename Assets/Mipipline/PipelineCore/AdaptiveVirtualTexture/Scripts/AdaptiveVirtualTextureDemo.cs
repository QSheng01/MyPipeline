using NoOvertime.VirtualTexture;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class AdaptiveVirtualTextureDemo : MonoBehaviour
{
    public VirtualTextureMono virtualTexture;
    public int forwardRendererIndex;
    public int deferredRendererIndex = 1;
    public float moveSpeed = 40f;
    public float boost = 3f;
    public float lookSpeed = 2f;

    private static readonly string[] LevelLabels = { "1", "2", "3", "4", "5", "6", "7", "8", "9" };
    private static readonly string[] ResolutionLabels = { "256", "512", "1024", "2048" };
    private static readonly int[] Resolutions = { 256, 512, 1024, 2048 };
    private static readonly float[] SizePresets = { 2f, 8f, 32f, 64f, 128f };

    private bool _deferred;
    private bool _showIndirection;
    private readonly List<Vector3> _indirectionAllocations = new List<Vector3>(256);
    private float _yaw;
    private float _pitch;
    private ClipmapTexture _clipmap;
    private Font _font;
    private Vector2 _hudScroll;

    private void Start()
    {
        Vector3 euler = transform.eulerAngles;
        _yaw = euler.y;
        _pitch = euler.x;
        ApplyPath();
        _clipmap = EnsureClipmap();
    }

    private void Update()
    {
        if (Input.GetMouseButton(1))
        {
            _yaw += Input.GetAxis("Mouse X") * lookSpeed;
            _pitch -= Input.GetAxis("Mouse Y") * lookSpeed;
            _pitch = Mathf.Clamp(_pitch, -85f, 85f);
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? boost : 1f);
        Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        if (Input.GetKey(KeyCode.E))
            move.y += 1f;
        if (Input.GetKey(KeyCode.Q))
            move.y -= 1f;
        transform.position += transform.TransformDirection(move) * (speed * Time.deltaTime);
    }

    private void OnGUI()
    {
        if (_showIndirection && Event.current.type == EventType.Repaint)
            DrawIndirectionOverlay();

        EnsureFont();
        Font previous = GUI.skin.font;
        if (_font != null)
            GUI.skin.font = _font;
        try
        {
            DrawHud();
            if (ClipmapTexture.DebugColors && Event.current.type == EventType.Repaint)
                DrawClipmapLegend(GetClipmap());
        }
        finally
        {
            GUI.skin.font = previous;
        }
    }

    private void DrawHud()
    {
        ClipmapTexture clipmap = GetClipmap();
        float height = Mathf.Min(Screen.height - 24f, clipmap != null ? 650f : 250f);
        GUILayout.BeginArea(new Rect(12f, 12f, 340f, height), GUI.skin.box);
        _hudScroll = GUILayout.BeginScrollView(_hudScroll);
        GUILayout.Label("Adaptive Virtual Texture");
        if (GUILayout.Button(virtualTexture != null && virtualTexture.enabled ? "VT: On" : "VT: Off"))
        {
            if (virtualTexture != null)
                virtualTexture.enabled = !virtualTexture.enabled;
        }

        if (GUILayout.Button(_deferred ? "Path: Deferred" : "Path: Forward"))
        {
            _deferred = !_deferred;
            ApplyPath();
        }

        if (GUILayout.Button(_showIndirection ? "Indirection: On" : "Indirection: Off"))
            _showIndirection = !_showIndirection;

        if (GUILayout.Button(ClipmapTexture.UseClipmap ? "Mode: Clipmap" : "Mode: Feedback"))
        {
            ClipmapTexture.UseClipmap = !ClipmapTexture.UseClipmap;
            if (!ClipmapTexture.UseClipmap)
                ClipmapTexture.DebugColors = false;
            if (ClipmapTexture.UseClipmap)
                _clipmap = EnsureClipmap();
        }
        if (GUILayout.Button(PhysicalPageAtlas.CompressionEnabled ? "Feedback压缩: Base BC7" : "Feedback压缩: 关闭"))
        {
            PhysicalPageAtlas.CompressionEnabled = !PhysicalPageAtlas.CompressionEnabled;
            VirtualTextureMono.RebuildContext();
        }
        if (PhysicalPageAtlas.CompressionEnabled && !PhysicalPageAtlas.CompressionSupported)
            GUILayout.Label("Feedback压缩: 当前平台回退 RGBA8");
        GUILayout.Label("Feedback缓存 " + PhysicalPageAtlas.CurrentMemoryMiB.ToString("0") + " MiB");

        if (GUILayout.Button(ClipmapTexture.DebugColors ? "Clipmap Color: On" : "Clipmap Color: Off"))
        {
            ClipmapTexture.DebugColors = !ClipmapTexture.DebugColors;
            if (ClipmapTexture.DebugColors)
            {
                ClipmapTexture.UseClipmap = true;
                _clipmap = EnsureClipmap();
            }
        }

        if (clipmap != null)
            DrawPartitionControls(clipmap);
        else
            GUILayout.Label("Clipmap component missing");

        GUILayout.Label("RMB look, WASD move, QE up/down");
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawPartitionControls(ClipmapTexture clipmap)
    {
        GUILayout.Space(4f);
        bool quality = GUILayout.Toggle(clipmap.matchFeedbackQuality, "Feedback 9档精度（1024–4纹素/米）");
        if (quality != clipmap.matchFeedbackQuality)
        {
            clipmap.matchFeedbackQuality = quality;
            if (quality) clipmap.ApplyFeedbackQuality();
        }
        GUILayout.Label("密度 " + clipmap.FinestTexelsPerMeter.ToString("0.#") + " 纹素/米  |  缓存 " + clipmap.CacheMemoryMiB.ToString("0") + " MiB");
        GUILayout.Label("就绪层 " + clipmap.ReadyLevelCount + "/" + clipmap.levelCount + "  灰色 = 独立保底（不计入9档）");
        if (GUILayout.Button(clipmap.compressBaseMap ? "压缩: Base BC7 / Mask RGBA8" : "压缩: RGBA8"))
            clipmap.compressBaseMap = !clipmap.compressBaseMap;
        if (clipmap.compressBaseMap && !clipmap.CompressionSupported)
            GUILayout.Label("当前平台使用 RGBA8 回退");
        GUILayout.Label("Page " + clipmap.tileSize + "px  |  每层 " + clipmap.PagesPerAxis + "×" + clipmap.PagesPerAxis);
        GUILayout.Label("本帧烘焙 " + clipmap.LastBakedPageCount + "  待更新 " + clipmap.DirtyPageCount);
        GUILayout.Label("红 = L0 最细，不是源贴图 Mip0");
        bool wasEnabled = GUI.enabled;
        GUI.enabled = wasEnabled && !clipmap.matchFeedbackQuality;
        int last = Mathf.Clamp(clipmap.levelCount, 1, ClipmapTexture.MaxLevels) - 1;
        string pending = clipmap.PartitionRebuildPending ? "  更新中" : "";
        GUILayout.Label("最细层 " + FormatMeters(clipmap.finestWorldSize)
            + "    最粗 " + FormatMeters(clipmap.LevelWorldSize(last)) + pending);

        float logMin = Mathf.Log(0.25f);
        float logMax = Mathf.Log(2048f);
        float t = Mathf.InverseLerp(logMin, logMax, Mathf.Log(Mathf.Clamp(clipmap.finestWorldSize, 0.25f, 2048f)));
        float next = Mathf.Exp(Mathf.Lerp(logMin, logMax, GUILayout.HorizontalSlider(t, 0f, 1f)));
        next = Mathf.Clamp(Mathf.Round(next * 4f) / 4f, 0.25f, 2048f);
        if (!Mathf.Approximately(next, clipmap.finestWorldSize))
            clipmap.finestWorldSize = next;

        GUILayout.BeginHorizontal();
        for (int i = 0; i < SizePresets.Length; i++)
        {
            float preset = SizePresets[i];
            if (GUILayout.Button(FormatMeters(preset)))
                clipmap.finestWorldSize = preset;
        }
        GUILayout.EndHorizontal();

        GUILayout.Label("层数  每层世界范围 x2");
        int levelIndex = Mathf.Clamp(clipmap.levelCount, 1, ClipmapTexture.MaxLevels) - 1;
        int nextLevel = GUILayout.SelectionGrid(levelIndex, LevelLabels, 3) + 1;
        if (nextLevel != clipmap.levelCount)
            clipmap.levelCount = nextLevel;

        GUILayout.Label("分辨率  不是 Mip");
        int resIndex = 2;
        for (int i = 0; i < Resolutions.Length; i++)
        {
            if (clipmap.resolution <= Resolutions[i])
            {
                resIndex = i;
                break;
            }
        }
        int nextRes = GUILayout.Toolbar(resIndex, ResolutionLabels);
        if (Resolutions[nextRes] != clipmap.resolution)
            clipmap.resolution = Resolutions[nextRes];

        GUI.enabled = wasEnabled;
        GUILayout.Label("跨层过渡 " + (clipmap.transitionWidth * 100f).ToString("0") + "%");
        clipmap.transitionWidth = GUILayout.HorizontalSlider(clipmap.transitionWidth, 0.02f, 0.3f);
        GUILayout.Label(BuildPartitionSummary(clipmap));
    }

    private static void DrawClipmapLegend(ClipmapTexture clipmap)
    {
        int count = clipmap != null ? Mathf.Clamp(clipmap.levelCount, 1, ClipmapTexture.MaxLevels) : ClipmapTexture.MaxLevels;
        float panelHeight = 82f + count * 22f;
        float x = Screen.width >= 760f ? Screen.width - 210f : 12f;
        float y = Screen.width >= 760f ? 12f : 582f;
        Rect panel = new Rect(x, y, 198f, panelHeight);
        GUI.Box(panel, GUIContent.none);
        GUI.Label(new Rect(panel.x + 8f, panel.y + 4f, 182f, 40f), "红 = L0 最细\n不是贴图 Mip0");
        for (int level = 0; level < count; level++)
        {
            float meters = clipmap != null ? clipmap.LevelWorldSize(level) : 32f * (1 << level);
            string name = "L" + level + "  " + FormatMeters(meters);
            if (level == 0)
                name += "  最细";
            Rect swatch = new Rect(panel.x + 8f, panel.y + 46f + level * 22f, 16f, 16f);
            Color previous = GUI.color;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(swatch.x - 1f, swatch.y - 1f, 18f, 18f), Texture2D.whiteTexture);
            GUI.color = Color.HSVToRGB(level / (float)ClipmapTexture.MaxLevels, 1f, 1f);
            GUI.DrawTexture(swatch, Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Label(new Rect(swatch.xMax + 6f, swatch.y - 2f, 160f, 20f), name);
        }
        GUI.Label(new Rect(panel.x + 8f, panel.y + 48f + count * 22f, 182f, 24f), "灰色：独立全地形保底");
    }

    private static string BuildPartitionSummary(ClipmapTexture clipmap)
    {
        int count = Mathf.Clamp(clipmap.levelCount, 1, ClipmapTexture.MaxLevels);
        string line = "";
        for (int level = 0; level < count; level++)
        {
            if (level > 0 && level % 4 == 0)
                line += "\n";
            else if (level > 0)
                line += "  ";
            line += "L" + level + " " + FormatMeters(clipmap.LevelWorldSize(level));
        }
        return line;
    }

    private static string FormatMeters(float meters)
    {
        if (meters >= 1000f)
            return (meters / 1000f).ToString("0.##") + "km";
        return meters.ToString("0.##") + "m";
    }

    private void DrawIndirectionOverlay()
    {
        if (ClipmapTexture.UseClipmap) { DrawClipmapPageTable(); return; }
        const float atlasSize = 1024f;
        Color previous = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.125f);
        GUI.DrawTexture(new Rect(0f, Screen.height - atlasSize, atlasSize, atlasSize), Texture2D.whiteTexture);

        if (virtualTexture != null && virtualTexture.isActiveAndEnabled && Context.Instance.IsInitialized)
        {
            Context.Instance.VirtualImageAtlas.CopyAllocations(_indirectionAllocations);
            for (int i = 0; i < _indirectionAllocations.Count; i++)
            {
                Vector3 allocation = _indirectionAllocations[i];
                float size = allocation.z - 2f;
                if (size <= 0f)
                    continue;

                float x = allocation.x + 1f;
                float y = Screen.height - (allocation.y + 1f + size);
                Color color = Color.HSVToRGB(Mathf.Log(allocation.z, 2f) / 8f, 1f, 1f);
                color.a = 0.5f;
                GUI.color = color;
                GUI.DrawTexture(new Rect(x, y, size, size), Texture2D.whiteTexture);
            }
        }

        GUI.color = previous;
    }

    private void DrawClipmapPageTable()
    {
        ClipmapTexture clipmap = GetClipmap();
        if (clipmap == null || clipmap.IndirectionTexture == null) return;
        Color saved = GUI.color;
        int count = clipmap.levelCount;
        const float cell = 144f;
        float width = Mathf.Min(Screen.width - 24f, cell * 3);
        float startX = Mathf.Max(12, Screen.width - width - 12);
        float startY = Mathf.Max(12, Screen.height - 3 * 172 - 40);
        GUI.Box(new Rect(startX, startY, width, 3 * 172 + 32), "Clipmap Page Table: color = physical slot, black = pending");
        for (int level = 0; level < count; level++)
        {
            float x0 = startX + 8 + (level % 3) * cell;
            float y0 = startY + 24 + (level / 3) * 172;
            var anchor = clipmap.GetPageAnchor(level);
            GUI.Label(new Rect(x0, y0, cell, 22), "L" + level + " [" + anchor.x + "," + anchor.y + "]");
            int n = clipmap.IndirectionTexture.width;
            float side = 128f / n;
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                Color32 entry = clipmap.GetPageMapping(level, x, y);
                GUI.color = entry.b == 0 ? Color.black : Color.HSVToRGB((entry.r + entry.g * n) / (float)(n * n), 0.8f, 1f);
                GUI.DrawTexture(new Rect(x0 + x * side, y0 + 24 + (n - 1 - y) * side, Mathf.Max(1, side - 1), Mathf.Max(1, side - 1)), Texture2D.whiteTexture);
            }
            GUI.color = saved;
        }
        GUI.color = saved;
    }

    private ClipmapTexture GetClipmap()
    {
        if (_clipmap != null)
            return _clipmap;
        return EnsureClipmap();
    }

    private ClipmapTexture EnsureClipmap()
    {
        ClipmapTexture clipmap = virtualTexture != null
            ? virtualTexture.GetComponent<ClipmapTexture>()
            : FindObjectOfType<ClipmapTexture>();
        if (clipmap == null && virtualTexture != null)
            clipmap = virtualTexture.gameObject.AddComponent<ClipmapTexture>();
        if (clipmap == null)
            return null;

        AdaptiveVirtualTextureLayerSet layers = clipmap.GetComponent<AdaptiveVirtualTextureLayerSet>();
        if (layers != null)
        {
            if (clipmap.splatMap == null)
                clipmap.splatMap = layers.splatMap;
            if (clipmap.baseMapArray == null)
                clipmap.baseMapArray = layers.baseMapArray;
            if (clipmap.maskMapArray == null)
                clipmap.maskMapArray = layers.maskMapArray;
        }

        _clipmap = clipmap;
        return clipmap;
    }

    private void EnsureFont()
    {
        if (_font != null)
            return;
        _font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei" }, 14);
    }

    private void ApplyPath()
    {
        var camera = GetComponent<Camera>();
        if (camera == null)
            return;
        camera.GetUniversalAdditionalCameraData().SetRenderer(_deferred ? deferredRendererIndex : forwardRendererIndex);
    }
}
