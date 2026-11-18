using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq.Expressions;
using System;
using System.Text;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;
using UnityEditorInternal;
using UnityEngine.Experimental.Rendering;

namespace Grass
{
    [CustomEditor(typeof(GrassTool))]
    public class GrassToolEditor : BaseEditor<GrassTool>
    {
        static public int s_DrawHash = "GPUInstance".GetHashCode();
        const float CLIP_PADDING = 10f;
        static readonly string GrassPipelineAsset = "GrassRenderPipelineAsset";
        static readonly string GrassColormapRenderer = "GrassColormapRenderer";
        static readonly string GrassLightMapRenderer = "GrassLightMapRenderer";
        static readonly string GrassShaderMapRenderer = "GrassShaderMapRenderer";
        static readonly string GrassNormalMapRenderer = "GrassNormalMapRenderer";
        static string[] reslist = new string[] { "2048x2048", "4096x4096" };
        static int[] resolutionValues = new int[] { 2048, 4096 };
        static List<GrassElementData> TempDatas = new List<GrassElementData>();
        //const string GrassShaderHeightPro = "_grassHeight";
        public const string GRASS_DATA_PATH = "Assets/Things/LightMap/GrassData/";

        const int kElementHeight = 70;
        const int kElementObjectFieldHeight = 16;
        const int kElementPadding = 2;
        const int kElementObjectFieldWidth = 160;
        const int kElementToggleWidth = 60;
        const int kElementImageWidth = 64;
        const int kElementImageHeight = 64;
        enum TextureType
        {
            Color,
            Lightmap,
            ShadowMask,
            Normalmap
        }

        SerializedProperty _terrainObjs;
        SerializedProperty _grassObjs;
        SerializedProperty _bounds;

        private Vector3 moveV3;

        SerializedProperty _colorTexture;
        SerializedProperty _lightmapTexture;
        SerializedProperty _shadowTexture;
        SerializedProperty _normalmapTexture;
        SerializedProperty _resolutionIndex;
        SerializedProperty _showBounds;
        //SerializedProperty _enableTransGrass;
        SerializedProperty _enablePainter;
        SerializedProperty _grassData;
        SerializedProperty _stylePainter;

        ReorderableList _grassObjList;
        TextureType previewType;
        int _terrainSize = 0;
        int _resolution = 2048;
        Camera _renderCamera;
        bool _refreshBrush;
        MaterialEditor _grassMatEditor;
        GameObject _plantObjPreview;

        GameObject _curDrawGrassObj;
#if UNITY_EDITOR
        GrassStyleColorEditor _styleColorEditor;
#endif

        [MenuItem("GameObject/Tools/NewGrassTool", priority = 0)]
        static void CreateTool(MenuCommand menuCommand)
        {
            var parent = new GameObject("New Grass Tools");
            parent.AddComponent<GrassTool>();
        }

        public static void ExportSceneGrassData()
        {
            // 找到场景中的带 GrassAssets 的脚本
            var assets = GameObject.FindObjectOfType<GrassAssets>();
            if (assets == null)
            {
                // 如果没有，就添加 GrassAssets
                GameObject obj = new GameObject("GrassAssets");
                assets = obj.AddComponent<GrassAssets>();
                obj.transform.SetParent(GameObject.Find("Setting").transform);
            }

            // 这里要遍历 GrassToolList 中的 tool，把 _grassData 全部存起来
            List<GrassPainterData> datas = new List<GrassPainterData>();
            foreach (var tool in GrassRenderer.GrassToolList)
            {
                if (tool._grassData != null)
                {
                    datas.Add(tool._grassData);
                }

                // 顺便初始化一下 LOD 数据防止报错
                tool.SetGrassObjs();
            }

            if (datas.Count > 0)
            {
                // 获取 data 文件所在的目录
                var path = AssetDatabase.GetAssetPath(datas[0]);
                var s = path.Split('/');
                // path = path.Replace(s[s.Length - 1], "");
                // Debug.Log(datas.Count);

                // 导出草的 Data
                // string tSceneName = s[4];
                string tSceneName = assets.gameObject.scene.name;
                string tOutPutPath = GRASS_DATA_PATH + tSceneName + "_";
                ExprotSceneGrassData(assets, datas, 2, tOutPutPath);
            }
            else
            {
                Debug.Log("编辑工具/导出场景草资源: 当前场景没有可导出的草数据！！！");
            }

        }

        /// <summary>
        /// 导出场景中的草的资源
        /// </summary>
        /// <param name="grassAssets"> 场景中的 GrassAssets 脚本</param>
        /// <param name="grassDatas"> 场景中所有 GrassTools 中的 GrassPainterData 信息</param>
        /// <param name="grassSize2"> 草的精度 </param>
        /// <param name="assetPath"> GrassPainterData 文件的目录</param>
        public static void ExprotSceneGrassData(GrassAssets grassAssets, IEnumerable<GrassPainterData> grassDatas, int grassSize2, string assetPath)
        {
            var grassPath = assetPath.Substring(assetPath.IndexOf("/")) + "grassBytes.bytes";
            var fullPath = Application.dataPath + grassPath;
            grassAssets.GrassSize2 = grassSize2;
            var rootBounds = new Bounds();
            Vector3 minSum = Vector3.one * 100000;
            Vector3 maxSum = -Vector3.one * 100000;
            // 遍历 GrassAssets 中所有的草，找到坐标点最小的，和坐标点最大的，组合成一个大的 bounds
            foreach (var data in grassDatas)
            {
                foreach (var grassData in data.Datas)
                {
                    var bounds = data.GetGrassBounds(grassData.Type);
                    bounds.center = grassData.Pos;
                    bounds.extents = grassData.HeightScale * bounds.extents;
                    var min = bounds.min;
                    var max = bounds.max;

                    if (max.x > maxSum.x) maxSum.x = max.x;
                    if (max.z > maxSum.z) maxSum.z = max.z;

                    if (min.x < minSum.x) minSum.x = min.x;
                    if (min.z < minSum.z) minSum.z = min.z;
                }
            }

            rootBounds.SetMinMax(minSum, maxSum);
            var boundSize2 = Mathf.CeilToInt(Mathf.Log(Mathf.Max(rootBounds.size.x, rootBounds.size.z), 2));
            var extent = 1 << (boundSize2 - 1);
            rootBounds.extents = new Vector3(extent, 2, extent);
            rootBounds.center = new Vector3(rootBounds.center.x, 0, rootBounds.center.z);
            grassAssets.RootSize2 = boundSize2;
            grassAssets.Center = rootBounds.center;
            // 继续导出草的数据
            ExproteGrassData(rootBounds.center, grassDatas, fullPath, false, grassAssets);
            grassAssets.DataPath = grassPath.Remove(0, 1);
        }

        private static void ExproteGrassData(Vector3 center, IEnumerable<GrassPainterData> grassDatas, string fulllPath, bool isHWorld, GrassAssets grassAssets = null)
        {
            // 打开或者创建文件
            FileStream stream = File.Open(fulllPath, FileMode.OpenOrCreate, FileAccess.Write);
            Dictionary<byte, TempTreeNode> nodes = new Dictionary<byte, TempTreeNode>();
            int rootSize2, grassSize2;
            byte type;

            rootSize2 = grassAssets.RootSize2;
            grassSize2 = grassAssets.GrassSize2;
            var length = 1 << (2 * (rootSize2 - grassSize2));
            // 初始化草的 GrassAsset数据
            grassAssets.ClearGrassObjs();
            // 对每个 GrassPainterData 进行遍历
            foreach (var grassData in grassDatas)
            {
                //Debug.Log(grassData.Datas.Count);
                // 对每个类型 prefab 的草进行遍历rendereXTEN
                for (byte k = 0; k < GrassTool.MaxBrush; k++)
                {
                    if (grassData.DrawObjArray[k] != null)
                    {
                        // 获取这个类型 prefab = grassData.DrawObjArray[k] 的草的 type
                        type = grassAssets.FindGrassType(grassData.DrawObjArray[k]);
                        //Debug.Log(type);
                        TempTreeNode node = null;
                        // 如果已经包含这个 type 的四叉树节点 node
                        if (nodes.TryGetValue(type, out node))
                        {
                            //直接把 GrassElementData 数据填入四叉树 node中，并自动构建新的四叉树。
                            node.AddData(grassData.Datas, k);
                        }
                        else // 如果之前没有包含这个 type 的四叉树节点 node
                        {
                            // 创建四叉树节点
                            node = TempTreeNode.CreateTree(k, center, rootSize2, grassSize2);
                            // 把 GrassElementData 数据填入四叉树 node中，并自动构建新的四叉树。
                            var num = node.AddData(grassData.Datas, k);
                            if (num > 0)
                            {
                                // type == 255 表示在 grassAssets 中没有扫描到 prefab = grassData.DrawObjArray[k] 的草的 type
                                // 所以给 grassAssets 添加上这个草的 type
                                if (type == 255)
                                {
                                    // 这里还会返回一个 type 值，让其不再 == 255
                                    type = grassAssets.AddGrassObj(grassData.DrawObjArray[k]);
                                }

                                if (type != 255)
                                {
                                    // 设定四叉树所有节点的 type
                                    node.ResetType(type);
                                    // 根据 type 把 node 添加到nodes中
                                    nodes.Add(type, node);
                                }
                            }
                        }
                        // 设置 prefab = grassData.DrawObjArray[k] 的草的资源 dirty 这样会刷新对这个预制体的引用。
                        EditorUtility.SetDirty(grassData.DrawObjArray[k]);
                        AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(grassData.DrawObjArray[k])));
                    }
                }
            }
            List<TempTreeNode> nodeDatas = new List<TempTreeNode>();
            foreach (var root in nodes.Values)
            {
                // 获取 root 所有的叶子节点，放到 nodeDatas 中
                root.GetAllLeafNode(nodeDatas);
            }

            int blockNum = 0;

            // 一个节点一个 block 计算一共有多少个 block
            foreach (var node in nodeDatas)
            {
                if (node.mElementCount > 0)
                    blockNum++;
            }
            // 二进制流写入块的数量
            StreamWriter.WriteInt32(stream, blockNum);
            //Debug.Log(blockNum);
            //allnum = 0;
            if (nodeDatas.Count > 0)
            {
                nodeDatas.Sort();
                int offset = 0;
                byte typeNum = (byte)(nodeDatas.Count / length);
                //Debug.Log(length + " " + nodeDatas.Count);
                // 二进制流写入 type 的数量
                stream.WriteByte(typeNum);
                // offset 表示当前处理完了第几个 nodeDatas 叶子节点
                while (offset < nodeDatas.Count)
                {
                    type = nodeDatas[offset].Type;
                    // 这里的 length 表示一个植物块数据（具体看《植被3.0交接文档》 - 1.2植被块数据分布）的数据量的长度。
                    // 如果是一个 5 * 5 的数据块矩阵，则长度 length = 25
                    byte[] mark = new byte[length / 8];
                    // 每8个bit(代表数据块中8个有无草的数据)压缩成一个byte
                    // 一共组成 length 个 bit = length / 8 个 byte
                    for (int k = 0; k < length; k += 8)
                    {
                        int value0 = nodeDatas[offset + k].mElementCount > 0 ? 1 : 0;
                        int value1 = nodeDatas[offset + k + 1].mElementCount > 0 ? 1 : 0;
                        int value2 = nodeDatas[offset + k + 2].mElementCount > 0 ? 1 : 0;
                        int value3 = nodeDatas[offset + k + 3].mElementCount > 0 ? 1 : 0;
                        int value4 = nodeDatas[offset + k + 4].mElementCount > 0 ? 1 : 0;
                        int value5 = nodeDatas[offset + k + 5].mElementCount > 0 ? 1 : 0;
                        int value6 = nodeDatas[offset + k + 6].mElementCount > 0 ? 1 : 0;
                        int value7 = nodeDatas[offset + k + 7].mElementCount > 0 ? 1 : 0;
                        mark[k / 8] = (byte)(value0 << 7);
                        mark[k / 8] += (byte)(value1 << 6);
                        mark[k / 8] += (byte)(value2 << 5);
                        mark[k / 8] += (byte)(value3 << 4);
                        mark[k / 8] += (byte)(value4 << 3);
                        mark[k / 8] += (byte)(value5 << 2);
                        mark[k / 8] += (byte)(value6 << 1);
                        mark[k / 8] += (byte)value7;
                    }
                    // 把 type 和 byte[] 信息写入二进制流
                    stream.WriteByte(type);
                    stream.Write(mark, 0, mark.Length);
                    for (int k = 0; k < length; k++)
                    {
                        if (nodeDatas[offset + k].mElementCount == 0) continue;
                        //Debug.Log(string.Format("index = {0} num = {1}", offset + k, nodeDatas[offset + k].mElementCount));
                        // 把每个叶子节点 nodeDatas 的草数量都写入二进制流。
                        if (nodeDatas[offset + k].mElementCount > 200)
                        {
                            int a = 10;
                        }
                        StreamWriter.WriteInt32(stream, nodeDatas[offset + k].mElementCount);
                        //allnum += nodeDatas[offset + k].mElementCount;
                        // 遍历每个叶子节点中的每个 GrassElementData，也就是每个 Instance 单个草的 Data
                        foreach (var data in nodeDatas[offset + k].mDatas)
                        {
                            // 对于每个 Instance 单个草的 Data 都以特定方式写入二进制流
                            // 具体参考《植被3.0交接文档》 - 1.3 植被Bytes数据字节排列规则
                            data.WriteToStream(stream);
                        }
                    }
                    // 下一个数据块
                    offset += length;
                }
            }
            else
            {
                stream.WriteByte(0);
            }
            //Debug.Log(allnum);
            stream.Close();
            stream.Dispose();
            AssetDatabase.Refresh();
        }
        private void OnEnable()
        {
            //Debug.Log("OnEnable");
            _terrainObjs = FindProperty(x => x._terrainObjs);
            _grassObjs = FindProperty(x => x._grassObjs);
            _bounds = FindProperty(x => x._rootBounds);
            _colorTexture = FindProperty(x => x._colorTexture);
            _lightmapTexture = FindProperty(x => x._lightmapTexture);
            _shadowTexture = FindProperty(x => x._shadowTexture);
            _normalmapTexture = FindProperty(x => x._normalmapTexture);
            _showBounds = FindProperty(x => x._showBounds);
            _resolutionIndex = FindProperty(x => x._resolutionIndex);
            _enablePainter = FindProperty(x => x._enablePainter);
            //_enableTransGrass = FindProperty(x => x._enableTransGrass);
            _grassData = FindProperty(x => x._grassData);
            _stylePainter = FindProperty(x => x._stylePainter);

            _resolution = resolutionValues[_resolutionIndex.intValue];

            if (m_Target._roodNodes == null)
            {
                m_Target.SetEditorDatas();
            }
            RefreshGrassColor();

            #region AutoGrassTool
            #if UNITY_EDITOR
            ScatterGrassEnable();
            #endif
            #endregion
        }
        private void OnDisable()
        {
            DestoryPreView();
            #region AutoGrassTool
#if UNITY_EDITOR
            ScatterGrassDisable();
#endif
            #endregion
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(_grassData, new GUIContent("草数据："));
                if (GUILayout.Button(new GUIContent(" New", EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? "d_editicon.sml" : "editicon.sml").image), GUILayout.MaxWidth(70f)))
                {
                    _grassData.objectReferenceValue = CreateGrassDataAsset();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                // 导入望月的草颜色数据
                EditorGUILayout.PropertyField(_stylePainter, new GUIContent("草颜色数据："));
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                m_Target.SetEditorDatas();
            }

            EditorGUILayout.Space();
            ParameterGroup.DrawHeader(new GUIContent("地表吸色"));
            EditorGUILayout.Space();
            using (new EditorGUILayout.VerticalScope(ParameterGroup.Section))
            {
                if (m_Target._grassData == null)
                {
                    EditorGUILayout.HelpBox("草数据不能为空，请先添加草数据！！！", MessageType.Warning);
                }
                else
                {
                    EditorGUI.indentLevel++;
                    EditorGUI.BeginChangeCheck();
                    // 根据 _terrainObjs 显示地块列表
                    EditorGUILayout.PropertyField(_terrainObjs, new GUIContent("地块列表："));
                    if (EditorGUI.EndChangeCheck() || _terrainObjs.arraySize != _terrainSize)
                    {
                        _terrainSize = _terrainObjs.arraySize;
                        m_Target.OnTerrainsChanged();
                    }

                    EditorGUI.BeginDisabledGroup(true);
                    // 显示地图包围盒 title = "地图包围盒：" value = _bounds
                    EditorGUILayout.PropertyField(_bounds, new GUIContent("地图包围盒："));
                    EditorGUI.EndDisabledGroup();

                    if (m_Target._rootBounds.size == Vector3.zero)
                    {
                        m_Target._colorTexture = null;
                        m_Target._lightmapTexture = null;
                        m_Target._shadowTexture = null;
                        EditorGUILayout.HelpBox("当前地图大小为 0 * 0 ，请检查地块列表！！！", MessageType.Warning);
                    }
                    else
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUI.BeginDisabledGroup(true);
                            // 显示颜色，光照贴图，阴影，法线四个贴图。
                            EditorGUILayout.ObjectField(_colorTexture.objectReferenceValue, typeof(Texture2D), false);
                            EditorGUILayout.ObjectField(_lightmapTexture.objectReferenceValue, typeof(Texture2D), false);
                            EditorGUILayout.ObjectField(_shadowTexture.objectReferenceValue, typeof(Texture2D), false);
                            EditorGUILayout.ObjectField(_normalmapTexture.objectReferenceValue, typeof(Texture2D), false);
                            EditorGUI.EndDisabledGroup();
                        }

                        EditorGUILayout.Space();

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.PropertyField(_showBounds, new GUIContent("显示包围盒："));
                            EditorGUI.BeginChangeCheck();
                            _resolutionIndex.intValue = EditorGUILayout.Popup(_resolutionIndex.intValue, reslist);
                            if (EditorGUI.EndChangeCheck())
                            {
                                _resolution = resolutionValues[_resolutionIndex.intValue];
                            }
                            if (GUILayout.Button("渲染吸色"))
                            {
                                RenderTextures();
                            }

                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }

            ParameterGroup.DrawHeader(new GUIContent("风格化刷色"));
            EditorGUILayout.Space();
            using (new EditorGUILayout.VerticalScope(ParameterGroup.Section))
            {
                if (m_Target._grassData == null)
                {
                    EditorGUILayout.HelpBox("草数据不能为空，请先添加草数据！！！", MessageType.Warning);
                }
                else if (m_Target._rootBounds.size == Vector3.zero)
                {
                    EditorGUILayout.HelpBox("当前地图大小为 0 * 0 ，请检查地块列表！！！", MessageType.Warning);

                }
                else
                {
                    if (m_Target._stylePainter)
                    {
                        GUIStyle boolBtnOn = new GUIStyle(GUI.skin.GetStyle("Button"));
                        EditorGUI.BeginChangeCheck();

                        bool afterToggle = GUILayout.Toggle(m_Target._stylePainter._enablePainter, new GUIContent("开启刷色"), boolBtnOn);

                        // 点击开启刷色自动配置刷色图片
                        if (!m_Target._stylePainter._enablePainter && afterToggle)
                        {
                            // 自动配置刷色图片
                            m_Target._stylePainter._ownerTool = m_Target;
                            if (m_Target == null)
                            {
                                m_Target.SetEditorNodeData();
                            }
                            m_Target._stylePainter.RestoreLayerTexturesTexture();
                        }

                        m_Target._stylePainter._enablePainter = afterToggle;

                        if (EditorGUI.EndChangeCheck())
                        {
                            if (!m_Target._stylePainter._enablePainter)
                            {
                                SaveGrassData();
                                // 配置 grasstool
                                m_Target._stylePainter._ownerTool = m_Target;
                                //ExportGrassColorDataAsset();
                            }
                            serializedObject.ApplyModifiedProperties();
                        }
#if UNITY_EDITOR
                        if (m_Target._stylePainter._enablePainter)
                        {
                            if (_styleColorEditor == null)
                            {
                                _styleColorEditor = UnityEditor.Editor.CreateEditor(m_Target._stylePainter) as GrassStyleColorEditor;
                            }

                            _styleColorEditor.OnInspectorGUI();
                        }

#endif
                    }
                }
            }


            ParameterGroup.DrawHeader(new GUIContent("一键草附着在地面"));
            EditorGUILayout.Space();
            using (new EditorGUILayout.VerticalScope(ParameterGroup.Section))
            {
                if (m_Target._grassData == null)
                {
                    EditorGUILayout.HelpBox("草数据不能为空，请先添加草数据！！！", MessageType.Warning);
                }
                else if (m_Target._rootBounds.size == Vector3.zero)
                {
                    EditorGUILayout.HelpBox("当前地图大小为 0 * 0 ，请检查地块列表！！！", MessageType.Warning);

                }
                else
                {
                    if (GUILayout.Button("一键草附着在地面"))
                    {
                        // foreach (var grassElementData in m_Target._grassData.Datas)
                        for (int i = 0; i < m_Target._grassData.Datas.Count; i++)
                        {
                            GrassElementData grassElementData = m_Target._grassData.Datas[i];

                            // Debug.Log("打印一下草的Pos" + grassElementData.Pos.x + "-" + grassElementData.Pos.y + "-" + grassElementData.Pos.z);
                            Vector3 grassPos = new Vector3(grassElementData.Pos.x, grassElementData.Pos.y, grassElementData.Pos.z);
                            Vector3 normal = Vector3.up;
                            float grassPosY = GetHeightAtPoint(grassPos, out normal);
                            Debug.Log(grassPos);
                            // Debug.Log("打印一下草的PosY" + grassPosY);

                            // 进行吸附在地表
                            Vector4 tPos = new Vector4(grassElementData.Pos.x, grassPosY, grassElementData.Pos.z, grassElementData.Pos.w);
                            grassElementData.SetWorldPos(tPos);
                            grassElementData.SetNormal(normal);

                            // 生效修改
                            m_Target._grassData.Datas[i] = grassElementData;
                        }

                        // 刷新 grass_tool 数据
                        m_Target._roodNodes = null;
                        // 配置 Editor 专用草的显示数据
                        m_Target.SetEditorDatas();
                        // 配置 Editor 专用的剔除用的 bound
                        m_Target.UpdateNodeBounds();

                        // 保存资源
                        EditorUtility.SetDirty(m_Target._grassData);
                        AssetDatabase.SaveAssetIfDirty(m_Target._grassData);
                        AssetDatabase.Refresh();
                    }
                }
            }

            EditorGUILayout.Space();
            ParameterGroup.DrawHeader(new GUIContent("整体移动位置"));
            EditorGUILayout.Space();
            using (new EditorGUILayout.VerticalScope(ParameterGroup.Section))
            {
                moveV3 = EditorGUILayout.Vector3Field("",moveV3);

                if (GUILayout.Button("移动"))
                {
                    List<GrassElementData> datas = new List<GrassElementData>(m_Target._grassData.Datas.Count);
                    foreach(var data in m_Target._grassData.Datas)
                    {
                        var dataTemp=new GrassElementData();

                        dataTemp.InitData(data.Matrix, data.DiffColor, data.NormalMapColor, data.LightMapColor, data.HeightScale, data.OnlyScaleY, data.Type);

                        dataTemp.SetWorldPos( new Vector3( dataTemp.Pos.x, dataTemp.Pos.y, dataTemp.Pos.z) + moveV3);

                        datas.Add(dataTemp);
                    }

                    m_Target._grassData.Datas = datas;
                    m_Target._grassData.RootBound.center += moveV3;

                    // 刷新 grass_tool 数据
                    m_Target._roodNodes = null;
                    // 配置 Editor 专用草的显示数据
                    m_Target.SetEditorDatas();
                    // 配置 Editor 专用的剔除用的 bound
                    m_Target.UpdateNodeBounds();

                    EditorUtility.SetDirty(m_Target._grassData);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

            }

                EditorGUILayout.Space();




            ParameterGroup.DrawHeader(new GUIContent("刷草设置"));
            EditorGUILayout.Space();
            using (new EditorGUILayout.VerticalScope(ParameterGroup.Section))
            {

                if (m_Target._grassData == null)
                {
                    EditorGUILayout.HelpBox("草数据不能为空，请先添加草数据！！！", MessageType.Warning);
                }
                else
                {
                    using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                    {
                        if (_grassObjList == null)
                        {
                            _grassObjList = new ReorderableList(serializedObject, _grassObjs, false, true, true, true);
                            _grassObjList.elementHeight = kElementHeight;
                            _grassObjList.drawHeaderCallback = DrawGrassObjsHeader;
                            _grassObjList.drawElementCallback = DrawGrassObj;
                            _grassObjList.onSelectCallback = OnSelectGrassObj;
                            _grassObjList.onRemoveCallback = OnRemoveGrassObj;
                            _grassObjList.onAddCallback = OnAddGrassObj;
                        }
                        _grassObjList.DoLayoutList();
                        EditorGUILayout.Space();

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            m_Target._debugTree = EditorGUILayout.Toggle("Debug空间树", m_Target._debugTree);
                            if (GUILayout.Button("清除所有草数据") && EditorUtility.DisplayDialog("删除草数据", "是否删除全部草数据", "是", "否"))
                            {
                                Undo.RegisterCompleteObjectUndo(m_Target._grassData, GrassTool.UnDoStr);
                                m_Target.DeleteAllCacheData();
                                SaveGrassData();
                            }
                        }
                    }

                    m_Target._selectIndex = Mathf.Clamp(m_Target._selectIndex, 0, Mathf.Max(0, m_Target._grassObjs.Length - 1));
                    _grassObjList.index = m_Target._selectIndex;

                    EditorGUILayout.Space();

                    var currentObj = m_Target._grassData.DrawObjArray[m_Target._selectIndex];

                    if (m_Target._rootBounds.size == Vector3.zero)
                    {
                        EditorGUILayout.HelpBox("当前地图大小为 0 * 0 ，请检查地块列表！！！", MessageType.Warning);

                    }
                    else if (m_Target.CheckBoundChanged())
                    {
                        EditorGUILayout.HelpBox("当前地图大小已变化，请重新渲染吸色！！！", MessageType.Warning);
                    }
                    else if (currentObj == null)
                    {
                        EditorGUILayout.HelpBox("请先设置笔刷草预制！！！", MessageType.Warning);
                    }
                    else
                    {
                        GrassInstanceData lodData;
                        CheckLodData(currentObj, out lodData);
                        if (lodData.LodMeshs == null || lodData.LodMeshs.Length == 0 || lodData.InstanceMat == null)
                        {
                            EditorGUILayout.HelpBox("笔刷草预制不正确，请检查该预制是否存在 Mesh 和 Material ！！！", MessageType.Warning);

                        }
                        else if (!lodData.InstanceMat.enableInstancing)
                        {
                            EditorGUILayout.HelpBox("请检查该预制 Material 是否支持 GPUInstance ！！！", MessageType.Warning);

                        }
                        else
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                GUILayout.FlexibleSpace();
                                GUIStyle boolBtnOn = new GUIStyle(GUI.skin.GetStyle("Button"));//Get Button Style
                                EditorGUI.BeginChangeCheck();
                                // 编辑的按钮
                                _enablePainter.boolValue = GUILayout.Toggle(_enablePainter.boolValue, EditorGUIUtility.IconContent("EditCollider"), boolBtnOn, GUILayout.Width(35), GUILayout.Height(25));
                                // 按钮状态变动检测
                                if (EditorGUI.EndChangeCheck())
                                {
                                    // 从 Editor 绑定的 GrassTool 脚本中，获取 _meshColliders 的属性
                                    var pro = FindProperty(x => x._meshColliders);

                                    // 如果是关闭按钮
                                    if (!_enablePainter.boolValue)
                                    {
                                        // 保存 editor 草的数据
                                        SaveGrassData();
                                        for (int i = 0; i < pro.arraySize; i++)
                                        {
                                            // 获取地块 Mesh Collider 销毁掉
                                            var collider = pro.GetArrayElementAtIndex(i).objectReferenceValue;
                                            if (collider != null)
                                                DestroyImmediate(collider);
                                        }
                                        pro.ClearArray();
                                        serializedObject.ApplyModifiedProperties();
                                    }
                                    else // 如果是开启按钮
                                    {
                                        if (m_Target.CheckBoundChanged())
                                        {
                                            // 重置包围盒
                                            m_Target.ResetBounds();
                                        }

                                        // 遍历每个地块
                                        for (int i = 0; i < m_Target._terrainObjs.Length; i++)
                                        {
                                            var terrain = m_Target._terrainObjs[i];
                                            if (terrain == null) continue;
                                            Terrain t = terrain.GetComponent<Terrain>();
                                            // 获取地形的 MeshRenderer 以及 MeshCollider
                                            MeshRenderer r = t ? null : terrain.GetComponent<MeshRenderer>();
                                            var collider = terrain.GetComponent<MeshCollider>();
                                            if (t == null && collider == null)
                                            {
                                                // 把临时的更改，应用到引用的 prefab对象，或者gameObject对象上去。
                                                collider = terrain.AddComponent<MeshCollider>();
                                                collider.sharedMesh = terrain.GetComponent<MeshFilter>().sharedMesh;
                                                pro.arraySize++;
                                                pro.GetArrayElementAtIndex(pro.arraySize - 1).objectReferenceValue = collider;
                                            }
                                        }
                                        serializedObject.ApplyModifiedProperties();
                                    }

                                }
                                GUILayout.FlexibleSpace();
                            }

                            if (_enablePainter.boolValue && m_Target._brushList != null && m_Target._brushList.Length > 0)
                            {
                                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                                {

                                    EditorGUI.BeginChangeCheck();

                                    m_Target._brushList[m_Target._selectIndex]._brushSize =
                                        EditorGUILayout.Slider("笔刷大小", m_Target._brushList[m_Target._selectIndex]._brushSize, 1, 25);

                                    EditorGUILayout.MinMaxSlider(string.Format("高度：{0}--{1}", m_Target._brushList[m_Target._selectIndex]._heightVar2.x.ToString("f2"),
                                        m_Target._brushList[m_Target._selectIndex]._heightVar2.y.ToString("f2")),
                                        ref m_Target._brushList[m_Target._selectIndex]._heightVar2.x, ref m_Target._brushList[m_Target._selectIndex]._heightVar2.y, 0.1f, 4);

                                    m_Target._brushList[m_Target._selectIndex]._grassDensity =
                                        EditorGUILayout.Slider("密度", m_Target._brushList[m_Target._selectIndex]._grassDensity, 0.1f, 1);

                                    //m_Target._grassSpacing = (1.1f - m_Target._brushList[m_Target._selectIndex]._grassDensity) * 3;

                                    m_Target._brushList[m_Target._selectIndex]._onlyScaleY = EditorGUILayout.Toggle("只缩放Y轴", m_Target._brushList[m_Target._selectIndex]._onlyScaleY);
                                    m_Target._brushList[m_Target._selectIndex]._fixedAngleEnable = EditorGUILayout.Toggle("选择旋转角度", m_Target._brushList[m_Target._selectIndex]._fixedAngleEnable);
                                    EditorGUI.BeginDisabledGroup(!m_Target._brushList[m_Target._selectIndex]._fixedAngleEnable);
                                    m_Target._brushList[m_Target._selectIndex]._fixedAngleY = EditorGUILayout.Slider("Angle Y", m_Target._brushList[m_Target._selectIndex]._fixedAngleY, 0f, 1f);
                                    EditorGUI.EndDisabledGroup();

                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        _refreshBrush = true;
                                    }
                                }

                                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                                {
                                    var mat = lodData.InstanceMat;

                                    if (_grassMatEditor == null || _grassMatEditor.target != mat)
                                    {
                                        _grassMatEditor = UnityEditor.Editor.CreateEditor(lodData.InstanceMat) as MaterialEditor;
                                    }
                                    if (_grassMatEditor.PropertiesGUI())
                                    {
                                        _refreshBrush = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

#if UNITY_EDITOR
            #region AutoGrassTool
            ParameterGroup.DrawHeader(new GUIContent("地表自动种草"));
            EditorGUILayout.Space();
            using (new EditorGUILayout.VerticalScope(ParameterGroup.Section))
            {
                EditorGUI.BeginChangeCheck();
                m_Target.scatting = EditorGUILayout.Toggle(new GUIContent("开启自动种草"), m_Target.scatting);
                if (EditorGUI.EndChangeCheck())
                {
                    if (m_Target.scatting)
                    {
                        m_Target.scatterType = GrassTool.ScatterType.Terrain;
                    }

                    if (m_Target.scatterType == GrassTool.ScatterType.Terrain)
                    {
                        if (m_Target.scatting == false)
                        {
                            m_Target.ScatterGrassDisable(GrassTool.ScatterType.Terrain);
                        }
                        else
                        {
                            m_Target.ScatterGrassEnable(GrassTool.ScatterType.Terrain);
                        }
                    }
                }

                if (m_Target.scatting && m_Target.scatterType == GrassTool.ScatterType.Terrain)
                {
                    ScatterGrassGUI();
                    ConvertToGrassData();
                }
            }

            // ParameterGroup.DrawHeader(new GUIContent("石头自动种草"));
            // EditorGUILayout.Space();
            // using (new EditorGUILayout.VerticalScope(ParameterGroup.Section))
            // {
            //     EditorGUI.BeginChangeCheck();
            //     m_Target.scatting = EditorGUILayout.Toggle(new GUIContent("开启自动种草"), m_Target.scatting);
            //
            //     if (EditorGUI.EndChangeCheck())
            //     {
            //         if (m_Target.scatting)
            //         {
            //             m_Target.scatterType = GrassTool.ScatterType.UnityMesh;
            //         }
            //
            //         if (m_Target.scatting == false)
            //         {
            //             m_Target.ScatterGrassDisable(GrassTool.ScatterType.UnityMesh);
            //         }
            //         else
            //         {
            //             m_Target.ScatterGrassEnable(GrassTool.ScatterType.UnityMesh);
            //         }
            //     }
            //
            //     if (m_Target.scatting && m_Target.scatterType == GrassTool.ScatterType.UnityMesh)
            //     {
            //         ScatterGrassGUI();
            //         ConvertToGrassData();
            //     }
            // }

            DeleteGenerateGrassData();
            #endregion
#endif
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.EndVertical();

        }
#if UNITY_EDITOR
        #region AutoGrassTool
        // 自动种草

        // private Terrain _terrain;
        // private Texture2D _alphamapTexture;
        // private ComputeBuffer _appendPointBuffer;
        // private ComputeBuffer _appendMatrixBuffer;
        // private Material _material;

        // private SerializedProperty terrain;
        private SerializedProperty meshes;

        private SerializedProperty iteration;
        private SerializedProperty blurSize;

        private SerializedProperty scatterPointTransformOffset;
        private SerializedProperty scatterPointTransition;
        private SerializedProperty scatterPointSize;
        private SerializedProperty scatterPointSizeOffset;
        private SerializedProperty scatterPointUVRandom;
        private SerializedProperty scatterPointRotate;
        private SerializedProperty scatterPointDensity;
        private SerializedProperty noiseTilling;
        private SerializedProperty noiseType;

        private float[] density;
        private void ScatterGrassGUI()
        {
            switch (m_Target.scatterType)
            {
                case GrassTool.ScatterType.Terrain:
                {
                    ScatterGrassTerrainInspector();
                    break;
                }
                case GrassTool.ScatterType.UnityMesh:
                {
                    ScatterGrassMeshInspector();
                    break;
                }
            }
        }

        private void ScatterGrassTerrainInspector()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUI.BeginChangeCheck();
            m_Target.terrain = EditorGUILayout.ObjectField("请拖入需要种草的地表", m_Target.terrain, typeof(Terrain)) as Terrain;
            if (EditorGUI.EndChangeCheck())
            {
                if (m_Target.terrain != null)
                {
                    m_Target.terrain.drawInstanced = true;
                    m_Target.InitHeightMap();
                }
            }
            // EditorGUILayout.PropertyField(terrain);
            if (m_Target.scatterGrassData == null)
            {
                m_Target.InitScatterGrassData();
            }
            var grassData = m_Target.scatterGrassData[m_Target._selectIndex];
            if (m_Target.terrain != null)
            {
                EditorGUILayout.BeginVertical();
                GUIContent dropDown = new GUIContent("选择地表层级");

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField("请拖入石头");
                if (_reorderableList == null)
                {
                    _reorderableList = new ReorderableList(serializedObject, meshes, true, false, true, true);
                }

                _reorderableList.drawElementCallback = DrawElement;
                _reorderableList.DoLayoutList();

                if (EditorGUILayout.DropdownButton(dropDown, FocusType.Keyboard))
                {
                    GenericMenu genericMenu = new GenericMenu();
                    for (int i = 0; i < m_Target.terrain.terrainData.terrainLayers.Length; i++)
                    {
                        genericMenu.AddItem(new GUIContent(
                                m_Target.terrain.terrainData.terrainLayers[i].name),
                            grassData.layerIndex == i,
                            SelectCurrentTerrainLayer,
                            i
                        );
                    }

                    genericMenu.ShowAsContext();
                }

                if (EditorGUI.EndChangeCheck())
                {
                    if (m_Target.meshes.Count > 0)
                    {
                        m_Target.InitVoxel();
                    }
                }

                if (_reorderableList.count > 0)
                {
                    var hasObject = true;
                    if (m_Target.meshes != null)
                    {
                        foreach (var obj in m_Target.meshes)
                        {
                            if (obj == null)
                            {
                                hasObject = false;
                                break;
                            }
                        }

                        if (hasObject)
                        {
                            ScatterGrassProperties(grassData);
                        }
                        else
                        {
                            EditorGUILayout.LabelField("列表里有空节点，请处理");
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("请拖入石头");
                    }
                }

                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.LabelField("请拖入需要种草的地表");
            }

            if (EditorGUI.EndChangeCheck())
            {
                m_Target.OnValidate();
            }
        }

        private void DrawElement(Rect rect, int index, bool isActice, bool isFocused)
        {
            m_Target.meshes[index] = (Transform)EditorGUI.ObjectField(rect, m_Target.meshes[index], typeof(Transform));
        }

        private ReorderableList _reorderableList;
        private void ScatterGrassMeshInspector()
        {
            EditorGUI.BeginChangeCheck();

            if (_reorderableList == null)
            {
                _reorderableList = new ReorderableList(serializedObject, meshes, true, false, true, true);
            }

            _reorderableList.drawElementCallback = DrawElement;
            _reorderableList.DoLayoutList();

            var grassData = m_Target.scatterGrassData[m_Target._selectIndex];
            if (m_Target.meshes != null)
            {
                EditorGUILayout.BeginVertical();

                ScatterGrassProperties(grassData);

                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.LabelField("请拖入需要种草的石头");
            }

            if (EditorGUI.EndChangeCheck())
            {
                m_Target.OnValidate();
            }
        }

        private void ScatterGrassProperties(GrassTool.GrassScatterData grassData)
        {
            EditorGUILayout.LabelField("基础设置");
            using (new EditorGUILayout.VerticalScope(new GUIStyle("HelpBox")))
            {
                grassData.scatterPointDensity = EditorGUILayout.IntSlider(new GUIContent("草密度"), grassData.scatterPointDensity, 0, 2048);
                grassData.scatterPointSize = EditorGUILayout.Slider(new GUIContent("草大小"), grassData.scatterPointSize, 0, 5);
                grassData.scatterPointTransformOffset = EditorGUILayout.Slider(new GUIContent("草位置随机偏移"), grassData.scatterPointTransformOffset, 0, 10);
                grassData.scatterPointSizeOffset = EditorGUILayout.Slider(new GUIContent("草大小随机偏移"), grassData.scatterPointSizeOffset, 0, 10);
                grassData.scatterPointRotate = EditorGUILayout.Slider(new GUIContent("草随机旋转偏移"), grassData.scatterPointRotate, 0.0f, 1.0f);
                grassData.scatterPointNormalCorrect = EditorGUILayout.Slider(new GUIContent("草坡度矫正"), grassData.scatterPointNormalCorrect, 0.0f, 1.0f);
            }

            EditorGUILayout.LabelField("草大小过渡设置");
            using (new EditorGUILayout.VerticalScope(new GUIStyle("HelpBox")))
            {
                grassData.scatterPointEdgeTransition = EditorGUILayout.Slider(new GUIContent("草边界剔除"), grassData.scatterPointEdgeTransition, 0, 1);
                grassData.scatterPointHeightTransition = EditorGUILayout.Slider(new GUIContent("草高度剔除"), grassData.scatterPointHeightTransition, 0, 10);
                grassData.iteration = EditorGUILayout.IntSlider(new GUIContent("模糊次数"), grassData.iteration, 0, 10);
                grassData.blurSize = EditorGUILayout.Slider(new GUIContent("模糊半径"), grassData.blurSize, 0, 5);
            }

            EditorGUILayout.LabelField("噪声设置（会影响大小和位置随机偏移）");
            using (new EditorGUILayout.VerticalScope(new GUIStyle("HelpBox")))
            {
                grassData.noiseTilling =
                    EditorGUILayout.Slider(new GUIContent("噪声Tilling"), grassData.noiseTilling, 1.0f, 1000);
                grassData.noiseType =
                    (GrassTool.GrassScatterData.NoiseType)EditorGUILayout.EnumPopup(new GUIContent("噪声类型"),
                        grassData.noiseType);
            }
        }

        private void ConvertToGrassData()
        {
            if (GUILayout.Button("转换草数据"))
            {
                m_Target.UpdateSelectingData();
                for (int i = 0; i < m_Target.scatterGrassData.Count; i++)
                {
                    var datas = m_Target.scatterGrassData[i].tempGrassData;
                    if(datas == null)
                        continue;

                    for (int j = 0; j < datas.Length; j++)
                    {
                        GrassElementData item = new GrassElementData();
                        item.InitScatterData(
                            datas[j],
                            Color.black,
                            Color.black,
                            Color.black,
                            (byte)i
                        );
                        m_Target._grassData.Datas.Add(item);
                    }
                }

                EditorUtility.SetDirty(m_Target._grassData);
                m_Target.SetEditorDatas();

                m_Target.ScatterGrassDisable(m_Target.scatterType);
                m_Target.scatting = false;
            }
        }

        private void DeleteGenerateGrassData()
        {
            if (GUILayout.Button("移除所有自动生成的草数据"))
            {
                for (int i = m_Target._grassData.Datas.Count - 1; i >= 0; i--)
                {
                    var data = m_Target._grassData.Datas[i].Matrix.m32;
                    Debug.Log(data);
                    if (data < 0)
                    {
                        m_Target._grassData.Datas.RemoveAt(i);
                    }
                }

                EditorUtility.SetDirty(m_Target._grassData);
                m_Target.SetEditorDatas();
            }
        }

        private void SelectCurrentTerrainLayer(object obj)
        {
            m_Target.scatterGrassData[m_Target._selectIndex].layerIndex = (int)obj;
            m_Target.UpdateTerrainScatterData(m_Target.scatterGrassData[m_Target._selectIndex]);
        }

        private void ScatterGrassEnable()
        {
            // terrain = FindProperty(x => x.terrain);
            meshes = FindProperty(x => x.meshes);
            // iteration = FindProperty(x => x.iteration);
            // blurSize = FindProperty(x => x.blurSize);
            // scatterPointTransformOffset = FindProperty(x => x.scatterPointTransformOffset);
            // scatterPointTransition = FindProperty(x => x.scatterPointTransition);
            // scatterPointSize = FindProperty(x => x.scatterPointSize);
            // scatterPointSizeOffset = FindProperty(x => x.scatterPointSizeOffset);
            // scatterPointUVRandom = FindProperty(x => x.scatterPointUVRandom);
            // scatterPointRotate = FindProperty(x => x.scatterPointRotate);
            // scatterPointDensity = FindProperty(x => x.scatterPointDensity);
            // noiseTilling = FindProperty(x => x.noiseTilling);
            // noiseType = FindProperty(x => x.noiseType);

            // EditorApplication.update += m_Target.ScatterGrassUpdate;
        }

        private void ScatterGrassDisable()
        {
            // EditorApplication.update -= m_Target.ScatterGrassUpdate;
        }

        #endregion
#endif
        float GetHeightAtPoint(Vector3 point, out Vector3 normal)
        {
            // 创建一个向下的射线
            Ray ray = new Ray(new Vector3(point.x, 10000f, point.z), Vector3.down);

            // 存储射线击中信息的变量
            RaycastHit hit;

            // 进行射线投射
            RaycastHit[] raycastHits = Physics.RaycastAll(ray);
            // if (Physics.RaycastAll(ray, out hit))
            foreach (RaycastHit raycastHit in raycastHits)
            {
                // 确保射线投射到的是 Ground
                if (raycastHit.collider.gameObject.layer == 3)
                {
                    normal = raycastHit.normal;
                    // 返回射线击中位置的高度
                    return raycastHit.point.y;
                }
            }
            foreach (RaycastHit raycastHit in raycastHits)
            {
                // 确保射线投射到的是地形
                if (raycastHit.collider.gameObject.GetComponent<Terrain>())
                {
                    normal = raycastHit.normal;
                    // 返回射线击中位置的高度
                    return raycastHit.point.y;
                }
            }

            // 如果没有击中地形或其他原因失败，则返回一个默认值，比如当前点的高度
            normal =Vector3.up;
            return point.y;
        }

        /// <summary>
        /// 场景中，草的绘制操作
        /// </summary>
        private void OnSceneGUI()
        {

            if (Application.isPlaying)
            {
                return;
            }
            // 锁定屏幕其他点击
            var controlId = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlId);
            var e = Event.current;
            var eventType = e.type;

            // 发射射线
            var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            var raycastHit = new RaycastHit();

            // 配置鼠标操作
            if (HandleMouseAction())
            {
                Event.current.Use();
                Repaint();
            }
#if UNITY_EDITOR
            // 刷颜色工具
            if (m_Target._stylePainter && m_Target._stylePainter._enablePainter)
            {
                if (_plantObjPreview)
                    DestoryPreView();

                if (Physics.Raycast(ray, out raycastHit, Mathf.Infinity, -1) && m_Target._rootBounds.Contains(raycastHit.point))
                {
                    Vector2 uv = new Vector2();
                    var pos = raycastHit.point - m_Target._rootBounds.min;
                    uv.x = pos.x / m_Target.TerrainResolution;
                    uv.y = pos.z / m_Target.TerrainResolution;
                    var rate = _resolution / m_Target.TerrainResolution;
                    //if (_styleColorEditor == null)
                    _styleColorEditor = UnityEditor.Editor.CreateEditor(m_Target._stylePainter) as GrassStyleColorEditor;
                    _styleColorEditor.OnSecenGUI(raycastHit, uv, rate);
                    SceneView.currentDrawingSceneView.Repaint();
                }
                return;
            }
#endif
            // 如果关掉了绘制按钮，则刷新场景
            if (!_enablePainter.boolValue)
            {
                // 删除掉显示在刷子上的草
                if (_plantObjPreview)
                    DestoryPreView();
                SceneView.currentDrawingSceneView.Repaint();
                return;
            }

            if (m_Target._brushList == null || m_Target._grassObjs[m_Target._selectIndex] == null)
                return;

            if (_curDrawGrassObj == null || _curDrawGrassObj != m_Target._grassObjs[m_Target._selectIndex])
            {
                _curDrawGrassObj = m_Target._grassObjs[m_Target._selectIndex];
                _refreshBrush = true;
            }

            // 发射射线
            if (Physics.Raycast(ray, out raycastHit, Mathf.Infinity, m_Target.TerrainsLayerMask) && m_Target._rootBounds.Contains(raycastHit.point))
            {
                if (e.keyCode == KeyCode.R)
                {
                    _refreshBrush = true;
                }

                if (e.type == EventType.KeyDown && e.keyCode == KeyCode.LeftBracket)
                {
                    // 限制笔刷大小
                    m_Target._brushList[m_Target._selectIndex]._brushSize = Mathf.Clamp(m_Target._brushList[m_Target._selectIndex]._brushSize - 0.1f, 1, 25);
                    _refreshBrush = true;
                }

                if (e.type == EventType.KeyDown && e.keyCode == KeyCode.RightBracket)
                {
                    // 限制笔刷大小
                    m_Target._brushList[m_Target._selectIndex]._brushSize = Mathf.Clamp(m_Target._brushList[m_Target._selectIndex]._brushSize + 0.1f, 1, 25);
                    _refreshBrush = true;
                }

                var drawRadius = m_Target._brushList[m_Target._selectIndex]._brushSize * 0.5f;
                var grassSpacing = (1.1f - m_Target._brushList[m_Target._selectIndex]._grassDensity) * 3;
                if (e.button == 0 && !e.alt && e.shift && !e.control)
                {
                    Handles.color = new Color(1f, 0f, 0f, 0.1f);
                    // 按了shift，绘制红色实心圆，开启删除功能
                    Handles.DrawSolidDisc(raycastHit.point, raycastHit.normal, drawRadius);
                    if (_plantObjPreview != null)
                    {
                        _plantObjPreview.gameObject.SetActive(false);
                        _plantObjPreview.transform.position = raycastHit.point;
                    }

                }
                else if (e.button == 0 && !e.alt && !e.shift && e.control)
                {
                    Handles.color = new Color(1f, 1f, 0f, 0.1f);
                    // 按了Ctrl，绘制黄色实心圆
                    Handles.DrawSolidDisc(raycastHit.point, raycastHit.normal, drawRadius);
                    if (_plantObjPreview != null)
                    {
                        _plantObjPreview.gameObject.SetActive(false);
                        _plantObjPreview.transform.position = raycastHit.point;
                    }
                }
                else
                {
                    // 绿色实心圆，
                    Handles.color = new Color(0f, 1f, 0f, 0.1f);
                    Handles.DrawSolidDisc(raycastHit.point, raycastHit.normal, drawRadius);

                    // 草的prefab 必须有 renderer
                    var renderer = m_Target._grassObjs[m_Target._selectIndex].GetComponent<GrassInstanceData>().GetMeshRender();
                    if (renderer != null)
                    {
                        // 如果需要刷新笔刷显示，而且笔刷上没有预览草，就生成预览草
                        if (_refreshBrush || !_plantObjPreview)
                        {
                            if (_plantObjPreview)
                                // 销毁笔刷上附着的草
                                DestoryPreView();
                            var magnitude = renderer.bounds.extents.magnitude;
                            var treeCountOneAxis = (int)(m_Target._brushList[m_Target._selectIndex]._brushSize / (magnitude * grassSpacing * 0.5f));
                            var treeCount = (int)((treeCountOneAxis * treeCountOneAxis) * .5f);
                            treeCount = Mathf.Clamp(treeCount, 1, 100);
                            // 用计算出来的草的数量，来进行显示
                            for (int i = 0; i != treeCount; i++)
                            {
                                var position = raycastHit.point;
                                var rotation = Quaternion.Euler(Vector3.zero);
                                //草刷绕Y轴随机旋转
                                if (m_Target._brushList[m_Target._selectIndex]._fixedAngleEnable)
                                {
                                    rotation = Quaternion.Euler(new Vector3(0, 360 * m_Target._brushList[m_Target._selectIndex]._fixedAngleY, 0));
                                }
                                else
                                {
                                    rotation = Quaternion.Euler(new Vector3(0, UnityEngine.Random.Range(0, 360), 0));
                                }

                                if (i != 0)
                                {
                                    // 绕圆来生成随机数
                                    var randomOffset = UnityEngine.Random.insideUnitCircle;
                                    randomOffset.x *= drawRadius;
                                    randomOffset.y *= drawRadius;

                                    position = raycastHit.point + new Vector3(randomOffset.x, 0, randomOffset.y);
                                }

                                var checkSucces = false;
                                if (_plantObjPreview != null)
                                {
                                    // 拿笔刷上的草，和现在想要绘制的草，进行距离对比
                                    foreach (var childList in _plantObjPreview.GetComponentsInChildren<Transform>())
                                    {
                                        if (Vector3.Distance(childList.position, position) < grassSpacing * 0.5f)
                                        {
                                            checkSucces = true;
                                            break;
                                        }
                                    }
                                }
                                // 如果这个点和笔刷上每个其他预览草，距离都远，就把生成一根预览草。
                                if (!checkSucces)
                                {
                                    var go = CreatePreviewObj(position, rotation);
                                    if (i == 0)
                                    {
                                        _plantObjPreview = go;
                                    }
                                    else
                                    {
                                        go.transform.SetParent(_plantObjPreview.transform);
                                    }
                                }
                            }

                            _refreshBrush = false;
                        }

                        if (_plantObjPreview != null)
                        {
                            _plantObjPreview.gameObject.SetActive(true);
                            _plantObjPreview.transform.position = raycastHit.point;
                        }
                    }
                }

                // 地图上绘制的过程
                if (e.type != EventType.Repaint &&
                    e.type != EventType.Layout &&
                    e.type != EventType.MouseMove &&
                    e.type != EventType.MouseEnterWindow &&
                    e.type != EventType.MouseLeaveWindow)
                {

                    if (e.shift && !e.control && e.button == 0 && e.isMouse && !e.alt)
                    {
                        //Debug.Log(e.type + " -1");
                        // 按了 shift + 左键， 删除信息
                        var removeLayer = (int)m_Target.m_RemoveLayer == 0 ? -1 : (int)m_Target.m_RemoveLayer;
                        m_Target.RemoveCacheData(removeLayer, raycastHit.point, drawRadius);
                    }
                    else if (!e.shift && e.control && e.button == 0 && e.isMouse && !e.alt)
                    {
                        // 按了 Ctrl + 左键， 重置草的高度
                        m_Target.ResetCacheDataHeight(raycastHit.point, drawRadius);
                    }
                    else if (!e.shift && !e.control && e.button == 0 && e.isMouse && !e.alt)
                    {
                        //Debug.Log("1");
                        // 按了左键，增加草的信息。
                        if (SystemInfo.supportsInstancing)
                        {
                            var render = m_Target._grassObjs[m_Target._selectIndex].GetComponent<GrassInstanceData>().GetMeshRender();
                            if (render)
                                render.sharedMaterial.enableInstancing = true;
                        }
                        //Save history for revocatio
                        TempDatas.Clear();
                        foreach (var render in _plantObjPreview.GetComponentsInChildren<Renderer>())
                        {
                            var hit = new RaycastHit();
                            // 用射线检测，是否在可行范围内绘制。
                            if (Physics.Raycast(render.transform.position + Vector3.up, Vector3.down, out hit, 10000, -1) && m_Target._rootBounds.Contains(hit.point))
                            {
                                // m_Target 表示 GrassTool
                                var noScaleXZ = m_Target._brushList[m_Target._selectIndex]._onlyScaleY ? 0 : 1;
                                var diffColor = m_Target.SampleColor(hit.point);
                                var lightmap = m_Target.SampleLight(hit.point);
                                var aoMap = m_Target.SampleShadow(hit.point);
                                diffColor.a = aoMap.r;
                                var normalColor = m_Target.SampleNormal(hit.point);
                                float height = 1;
                                if (render.sharedMaterial.HasProperty(GrassTool.GrassShaderHeightPro))
                                {
                                    height = render.sharedMaterial.GetFloat(GrassTool.GrassShaderHeightPro);
                                }
                                var scale = render.transform.lossyScale;
                                var matrix = Matrix4x4.TRS(hit.point, render.transform.rotation, scale);
                                GrassElementData data = new GrassElementData();
                                data.InitData(matrix, diffColor, normalColor, lightmap, height, noScaleXZ, (byte)m_Target._selectIndex);
                                // 生成 GrassElementData 存入到 List<GrassElementData> TempDatas 用来临时渲染展示。
                                TempDatas.Add(data);
                            }
                        }
                        // 把临时的 TempDatas 存入到 GrassTool 中的 _roodNodes
                        m_Target.AddCacheData(TempDatas, grassSpacing);
                    }
                }

                SceneView.currentDrawingSceneView.Repaint();
            }

        }

        private bool HandleMouseAction()
        {
            bool handle = false;

            if (m_Target._enablePainter)
            {
                if (Event.current.type == EventType.ScrollWheel && Event.current.control && !Event.current.shift && Event.current.alt)
                {
                    m_Target._brushList[m_Target._selectIndex]._brushSize = Mathf.Clamp(m_Target._brushList[m_Target._selectIndex]._brushSize - (Event.current.delta.y / 3), 1, 25);
                    _refreshBrush = true;
                    handle = true;
                }

                if (Event.current.type == EventType.ScrollWheel && !Event.current.control && Event.current.shift && Event.current.alt)
                {
                    m_Target._brushList[m_Target._selectIndex]._grassDensity = Mathf.Clamp(m_Target._brushList[m_Target._selectIndex]._grassDensity - (Event.current.delta.y / 50), 0, 1);
                    _refreshBrush = true;
                    handle = true;
                }
            }

            if (m_Target._enablePainter &&
                Event.current.control && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.S)
            {
                SaveGrassData();
                handle = true;
            }

            if (m_Target._enablePainter &&
                Event.current.isMouse && Event.current.button == 0 && (Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseDown))
            {
                //Debug.Log("undo");

                m_Target.SaveCacheData();
                Undo.RegisterCompleteObjectUndo(m_Target._grassData, GrassTool.UnDoStr);
                //handle = true;
            }
            return handle;
        }

        public void SaveGrassData(bool ctrlS = false)
        {
            if (ctrlS && !_enablePainter.boolValue) return;
            Debug.Log("GrassToolEditor::保存草数据");
            Undo.ClearUndo(m_Target._grassData);

            // 如果是望月草，带刷色
            if (m_Target._stylePainter)
            {
                Undo.ClearUndo(m_Target._stylePainter);
            }
            m_Target.SaveCacheData();
            EditorUtility.SetDirty(m_Target._grassData);
            AssetDatabase.SaveAssetIfDirty(m_Target._grassData);

            // 如果是望月草，带刷色
            if (m_Target._stylePainter)
            {
                EditorUtility.SetDirty(m_Target._stylePainter);
                AssetDatabase.SaveAssetIfDirty(m_Target._stylePainter);
            }
            AssetDatabase.Refresh();
        }

        private void OnAddGrassObj(ReorderableList list)
        {
            if (list.count == GrassTool.MaxBrush)
            {
                EditorUtility.DisplayDialog("添加草刷", string.Format("最多只能添加{0}个草刷，当前已达到数量上限！", GrassTool.MaxBrush), "确定");
                return;
            }

            _grassObjs.arraySize++;
            _grassObjs.GetArrayElementAtIndex(_grassObjs.arraySize - 1).objectReferenceValue = null;
            serializedObject.ApplyModifiedProperties();
        }

        private void OnRemoveGrassObj(ReorderableList list)
        {
            if (m_Target._grassCountArray[m_Target._selectIndex] > 0)
            {
                if (EditorUtility.DisplayDialog("删除草数据", "是否删除当前选中的草数据", "是", "否"))
                {
                    Undo.RegisterCompleteObjectUndo(m_Target._grassData, GrassTool.UnDoStr);
                    m_Target.DeleteSelectCacheData();
                    SaveGrassData();
                }
                return;
            }

            if (list.count == 1)
            {
                EditorUtility.DisplayDialog("删除草刷", string.Format("草刷数量不能为零！！！", GrassTool.MaxBrush), "确定");
                return;
            }

            if (EditorUtility.DisplayDialog("删除草刷", "是否删除当前选中的草刷", "是", "否"))
            {
                _grassObjs.GetArrayElementAtIndex(m_Target._selectIndex).objectReferenceValue = null;
                _grassObjs.DeleteArrayElementAtIndex(m_Target._selectIndex);
                serializedObject.ApplyModifiedProperties();
                m_Target.OnRemoveSelectGrassObj();
            }

        }

        private void OnSelectGrassObj(ReorderableList list)
        {
            m_Target._selectIndex = list.index;
            var element = _grassObjs.GetArrayElementAtIndex(list.index);
            if (element.objectReferenceValue != null)
            {
                var grassObj = element.objectReferenceValue as GameObject;
                GrassInstanceData lodData;
                CheckLodData(grassObj, out lodData);
            }
        }

        private void DrawGrassObj(Rect rect, int index, bool isActive, bool isFocused)
        {

            rect.y += kElementPadding;
            rect.x += 10;
            var rectImage = new Rect((rect.x + kElementPadding), rect.y, kElementImageWidth, kElementImageHeight);
            var rectObject = new Rect((rectImage.x + kElementImageWidth + 10), rect.y, kElementObjectFieldWidth, kElementObjectFieldHeight);
            var rectToggleLable = new Rect(rectObject.x, rectObject.y + 0.5f * kElementImageHeight, 0.5f * kElementToggleWidth, kElementObjectFieldHeight);
            var rectToggle = new Rect(rectToggleLable.x + 0.5f * kElementToggleWidth, rectToggleLable.y, 0.5f * kElementToggleWidth, kElementObjectFieldHeight);
            var rectNumLable = new Rect(rectToggle.x + 0.5f * kElementToggleWidth, rectToggle.y, kElementObjectFieldWidth, kElementObjectFieldHeight);


            var element = _grassObjs.GetArrayElementAtIndex(index);

            Texture2D icon = null;
            if (element.objectReferenceValue != null)
            {
                var grassObj = element.objectReferenceValue as GameObject;
                icon = AssetPreview.GetAssetPreview(grassObj);
            }
            GUI.Box(rectImage, icon);

            var colorHold = GUI.backgroundColor;
            var bitData = 1 << index;
            var toggleEnable = (bitData & m_Target.m_RemoveLayer) > 0;
            GUI.backgroundColor = toggleEnable ? Color.red : colorHold;
            EditorGUI.LabelField(rectToggleLable, "撤除");
            var enable = EditorGUI.Toggle(rectToggle, toggleEnable);
            if (enable)
            {
                m_Target.m_RemoveLayer |= bitData;
            }
            else
            {
                m_Target.m_RemoveLayer &= ~bitData;
            }
            GUI.backgroundColor = colorHold;
            EditorGUI.BeginChangeCheck();
            var newElement = EditorGUI.ObjectField(rectObject, element.objectReferenceValue, typeof(GameObject), false) as GameObject;
            if (EditorGUI.EndChangeCheck())
            {
                if (newElement != null && PrefabUtility.GetPrefabAssetType(newElement) != PrefabAssetType.Regular)
                {
                    EditorUtility.DisplayDialog("添加草预制", string.Format("当前选择的资源不是常规预制体！！！", GrassTool.MaxBrush), "确定");
                }
                else
                {
                    element.objectReferenceValue = newElement;

                    if (newElement != null)
                    {
                        GrassInstanceData lodData;
                        CheckLodData(newElement, out lodData);
                    }
                    serializedObject.ApplyModifiedProperties();
                    m_Target.OnGrassObjChanged(index);
                    //EditorUtility.SetDirty(m_Target.m_GrassRender.m_GrassData);
                }

            }

            EditorGUI.LabelField(rectNumLable, "当前数量：" + m_Target._grassCountArray[index]);

            serializedObject.ApplyModifiedProperties();

        }

        private void DrawGrassObjsHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "草刷列表", EditorStyles.boldLabel);
        }

        public override bool HasPreviewGUI()
        {
            if (m_Target._colorTexture == null) return false;

            return true;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (m_Target._colorTexture == null) return;

            if (previewType == TextureType.Color)
            {
                GUI.DrawTexture(r, m_Target._colorTexture, ScaleMode.ScaleToFit, false);
            }
            else if (previewType == TextureType.Lightmap)
            {
                GUI.DrawTexture(r, m_Target._lightmapTexture, ScaleMode.ScaleToFit, false);
            }
            else
            {
                GUI.DrawTexture(r, m_Target._shadowTexture, ScaleMode.ScaleToFit, false);
            }

            Rect btnRect = r;
            btnRect.width = 90f;
            btnRect.height = 20f;

            for (int i = 0; i <= (int)TextureType.ShadowMask; i++)
            {
                var type = GUI.Toggle(btnRect, (int)previewType == i, new GUIContent(Enum.GetName(typeof(TextureType), i)), "Button");
                btnRect.x += 90f;
                if (type)
                {
                    previewType = (TextureType)i;
                }
            }

            GUI.Label(new Rect(r.width * 0.5f - (175 * 0.5f), r.height - 5, 175, 25), string.Format("{0} texel(s) per meter", GetTexelSize(m_Target._colorTexture.height, m_Target._rootBounds.size.x)), EditorStyles.toolbarButton);
        }

        private GrassPainterData CreateGrassDataAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject("生成地形植被数据", target.name, "asset", string.Empty);
            if (!string.IsNullOrEmpty(path))
            {
                var assetObj = CreateInstance<GrassPainterData>();
                assetObj.Datas = new List<GrassElementData>();
                assetObj.DrawObjArray = new GameObject[GrassTool.MaxBrush];

                if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(assetObj, path);
                AssetDatabase.ImportAsset(path);
                var data = AssetDatabase.LoadAssetAtPath<GrassPainterData>(path);
                return data;
            }
            return null;
        }

        //private void ChangedGrassData()
        //{
        //    Quaternion q = Quaternion.Euler(m_Target._rotate1 - m_Target._rotate0);
        //    Matrix4x4 m = Matrix4x4.Rotate(q);
        //    var pos = m * m_Target._pos0;
        //    var dpos = m_Target._pos1 - (Vector3)pos;
        //    //Debug.Log("dpos = " + dpos);
        //    foreach (var data in m_Target._grassData.Datas)
        //    {
        //        data.ChangedPosAndRotation(dpos, q);
        //    }
        //    //EditorUtility.SetDirty(m_Target.m_GrassRender.m_GrassData);
        //    serializedObject.ApplyModifiedProperties();
        //    m_Target.ResetBounds();
        //    //SaveGrassData();
        //    //m_Target.ImportDataToRenderer();
        //    m_Target._pos0 = m_Target._pos1;
        //    m_Target._rotate0 = m_Target._rotate1;
        //    SceneView.RepaintAll();
        //}

        /// <summary>
        /// 外部调用的草吸色函数
        /// </summary>
        public static void ExternalRenderTextures()
        {
            // 保存之前的 Asset 设置
            RenderPipelineAsset tempAsset = GraphicsSettings.renderPipelineAsset;

            GrassTool[] objs = GameObject.FindObjectsOfType<GrassTool>();
            foreach (var grassTool in objs)
            {
                GrassToolEditor grassToolEditor = UnityEditor.Editor.CreateEditor(grassTool) as GrassToolEditor;
                grassToolEditor.RenderTextures();

                // 创建完之后删除
                DestroyImmediate(grassToolEditor);
            }

            // 恢复 Asset 设置
            GraphicsSettings.renderPipelineAsset = tempAsset;
        }

        public void RenderTextures()
        {
            var fogEnabled = RenderSettings.fog;
            var origin_qualitypipelineAsset = QualitySettings.renderPipeline;
            //这个值不为空时，GraphicsSettings.renderPipelineAsset 不会生效。
            QualitySettings.renderPipeline = null;
            var origin_pipelineAsset = GraphicsSettings.renderPipelineAsset;

            var grasspipelineAsset = PipelineUtilities.GetRenderPipelineAsset(GrassPipelineAsset);
            GraphicsSettings.renderPipelineAsset = grasspipelineAsset;
            RenderSettings.fog = false;

            RenderALLTexture();

            if (m_Target.SampleTextureToGrassData())
            {
                //EditorUtility.SetDirty(m_Target._grassData);
                //AssetDatabase.SaveAssetIfDirty(m_Target._grassData);
                //AssetDatabase.Refresh();

                // 把刷色数据覆盖上草
                // m_Target.ResetCacheDataTerrainColor();
                // m_Target.ResetCacheDataStyleColor(GrassElementData.ColorType.Diff);

                SaveGrassData();
            }
            //revert
            RenderSettings.fog = fogEnabled;
            QualitySettings.renderPipeline = origin_qualitypipelineAsset;
            GraphicsSettings.renderPipelineAsset = origin_pipelineAsset;
        }

        private void RenderALLTexture()
        {
            CreateRenderCamera();
            m_Target.ResetBounds();
            //SaveGrassData();
            //m_Target.ImportDataToRenderer();
            SetLodsEnabled(false);

            //手动设置 Camera 从上往下照相，输出 RT ，并保存在四张 Texture2D 中。
            SetUpRenderingData(GrassColormapRenderer);
            RenderToTexture(TextureType.Color);

            SetUpRenderingData(GrassLightMapRenderer);
            RenderToTexture(TextureType.Lightmap);

            SetUpRenderingData(GrassShaderMapRenderer);
            RenderToTexture(TextureType.ShadowMask);

            SetUpRenderingData(GrassNormalMapRenderer);
            RenderToTexture(TextureType.Normalmap);

            SetLodsEnabled(true);
            DestroyImmediate(_renderCamera.gameObject);
            _renderCamera = null;
            EditorUtility.SetDirty(m_Target);
        }

        private void CreateRenderCamera()
        {

            _renderCamera = new GameObject().AddComponent<Camera>();
            _renderCamera.name = "Grass color map renderCam";
            _renderCamera.enabled = false;

            //Camera set up
            _renderCamera.orthographic = true;
            _renderCamera.orthographicSize = Mathf.Max(m_Target._rootBounds.size.x * 0.5f,
                m_Target._rootBounds.size.z * 0.5f);
            _renderCamera.farClipPlane = m_Target._rootBounds.size.y + CLIP_PADDING;
            _renderCamera.cullingMask = m_Target.TerrainsLayerMask;
            _renderCamera.clearFlags = CameraClearFlags.Color;
            _renderCamera.backgroundColor = Color.black;

            //Position cam in given center of terrain(s)
            _renderCamera.transform.position = new Vector3(
                m_Target._rootBounds.center.x,
                m_Target._rootBounds.center.y + m_Target._rootBounds.extents.y + 4,
                m_Target._rootBounds.center.z
            );

            _renderCamera.transform.localEulerAngles = new Vector3(90, 0, 0);
            UniversalAdditionalCameraData camData =
                _renderCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camData.renderShadows = false;
            camData.renderPostProcessing = false;
            camData.antialiasing = AntialiasingMode.None;
            camData.requiresColorOption = CameraOverrideOption.Off;
            camData.requiresDepthOption = CameraOverrideOption.Off;
            camData.requiresColorTexture = false;
            camData.requiresDepthTexture = false;
        }

        private void SetUpRenderingData(string renderingdataName)
        {
            var camData = _renderCamera.gameObject.GetComponent<UniversalAdditionalCameraData>();
            if (UniversalRenderPipeline.asset)
            {
                if (UniversalRenderPipeline.asset.name == GrassPipelineAsset)
                {
                    var renderData = PipelineUtilities.GetRenderer(renderingdataName);

                    if (PipelineUtilities.ValidatePipelineRenderers(renderData))
                    {
                        PipelineUtilities.AssignRendererToCamera(camData, renderData);
                    }
                    else
                    {
                        Debug.Log("The " + renderingdataName + " ScriptableRendererFeature is not assigned to the pipeline asset");
                    }
                }
                else
                {
                    Debug.Log("Current Universal Render Pipeline isn't " + GrassPipelineAsset + ",please check.");
                }
            }
            else
            {
                Debug.LogError("No Universal Render Pipeline is currently active.");
            }
        }

        private void RenderToTexture(TextureType type)
        {
            //Set up render texture
            RenderTexture rt = new RenderTexture(_resolution, _resolution, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            _renderCamera.targetTexture = rt;
            _renderCamera.Render();
            RenderTexture.active = rt;

            if (type == TextureType.Color)
            {
                m_Target._colorTexture = new Texture2D(_resolution, _resolution, UnityEngine.Experimental.Rendering.GraphicsFormat.B8G8R8A8_UNorm, TextureCreationFlags.None);
                m_Target._colorTexture.ReadPixels(new Rect(0, 0, _resolution, _resolution), 0, 0);
                m_Target._colorTexture.Apply();
            }
            else if (type == TextureType.Lightmap)
            {
                //m_Target._lightmapTexture = new Texture2D(_resolution, _resolution, TextureFormat.RGBA32, false, true);
                m_Target._lightmapTexture = new Texture2D(_resolution, _resolution, UnityEngine.Experimental.Rendering.GraphicsFormat.B8G8R8A8_UNorm, TextureCreationFlags.None);
                m_Target._lightmapTexture.ReadPixels(new Rect(0, 0, _resolution, _resolution), 0, 0);
                m_Target._lightmapTexture.Apply();
            }
            else if (type == TextureType.ShadowMask)
            {
                m_Target._shadowTexture = new Texture2D(_resolution, _resolution, TextureFormat.RGBA32, false, true);
                m_Target._shadowTexture.ReadPixels(new Rect(0, 0, _resolution, _resolution), 0, 0);
                m_Target._shadowTexture.Apply();
            }
            else if (type == TextureType.Normalmap)
            {
                m_Target._normalmapTexture = new Texture2D(_resolution, _resolution, TextureFormat.RGBA32, false, true);
                m_Target._normalmapTexture.ReadPixels(new Rect(0, 0, _resolution, _resolution), 0, 0);
                m_Target._normalmapTexture.Apply();
            }

            //Cleanup
            RenderTexture.active = null;
            _renderCamera.targetTexture = null;
            DestroyImmediate(rt);

        }

        private static void SetLodsEnabled(bool enabled)
        {
            var lods = FindObjectsOfType<LODGroup>();
            foreach (var lod in lods)
            {
                lod.enabled = enabled;
                var childCount = lod.transform.childCount;
                for (int i = 1; i < childCount; i++)
                {
                    lod.transform.GetChild(i).gameObject.SetActive(enabled);
                }
            }
        }

        public static double GetTexelSize(float texelSize, float worldSize)
        {
            return System.Math.Round(texelSize / worldSize, 2);
        }

        /// <summary>
        /// 删掉附着在鼠标上的预览草
        /// </summary>
        public void DestoryPreView()
        {
            var filters = FindObjectsOfType<GameObject>();
            foreach (var obj in filters)
            {
                if (obj != null && obj.name == s_DrawHash.ToString())
                {
                    DestroyImmediate(obj);
                }
            }

            if (_plantObjPreview)
            {
                DestroyImmediate(_plantObjPreview);
                _plantObjPreview = null;
            }
        }

        private GameObject CreatePreviewObj(Vector3 position, Quaternion rotation, bool isShowScene = false, string name = "")
        {
            var brushConfig = m_Target._brushList[m_Target._selectIndex];
            var go = Instantiate(m_Target._grassObjs[m_Target._selectIndex], position, rotation);
            if (!isShowScene)
            {
                go.hideFlags = HideFlags.HideInHierarchy;
                go.name = s_DrawHash.ToString();
            }
            else
            {
                go.name = name;
            }

            GrassInstanceData lodData;
            CheckLodData(go, out lodData);

            var render = lodData.GetMeshRender();
            //关闭接收阴影
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var mat = new Material(render.sharedMaterial);
            mat.shader = Shader.Find("Hidden/ShiYue/URP/Grass/GrassPreview");
            render.sharedMaterial = mat;
            render.sharedMaterial.SetFloat("_grassHeight", UnityEngine.Random.Range(brushConfig._heightVar2.x, brushConfig._heightVar2.y));

            return go;
        }

        private bool CheckLodData(GameObject obj, out GrassInstanceData lodData)
        {
            lodData = obj.GetComponent<GrassInstanceData>();
            if (lodData == null)
            {
                lodData = obj.AddComponent<GrassInstanceData>();
                lodData.Init();
                return false;
            }
            return true;
        }

        private void RefreshGrassColor()
        {
#if UNITY_EDITOR
            _styleColorEditor = UnityEditor.Editor.CreateEditor(m_Target._stylePainter) as GrassStyleColorEditor;
            if (_styleColorEditor)
            {
                _styleColorEditor.RestGrassColorByTexture();
            }
#endif
        }
    }

    public class BaseEditor<T> : UnityEditor.Editor where T : MonoBehaviour
    {
        /// <summary>
        /// The target component.
        /// </summary>
        protected T m_Target
        {
            get { return (T)target; }
        }

        /// <summary>
        /// 从 BaseEditor<GrassTool> 对应的 GrassTool 中查找 cs 源码中的 Preoperty
        /// </summary>
        /// <typeparam name="TValue"> 返回的值类型，根据输入类型 T 决定</typeparam>
        /// <param name="expr"> Lambda表达式 </param>
        /// <returns></returns>
        protected SerializedProperty FindProperty<TValue>(Expression<Func<T, TValue>> expr)
        {
            return serializedObject.FindProperty(GetFieldPath(expr));
        }

        private string GetFieldPath<TType, TValue>(Expression<Func<TType, TValue>> expr)
        {
            MemberExpression me;
            // MemberExpression 需要支持 MemberAccess
            switch (expr.Body.NodeType)
            {
                case ExpressionType.MemberAccess:
                    me = expr.Body as MemberExpression;
                    break;
                default:
                    throw new InvalidOperationException();
            }

            // 这里的含义就是返回一下 expr 表达式中的结果参数名称，如果是 x => x._terrainObjs, 那么这个 sb 就是 "_terrainObjs"
            var members = new List<string>();
            while (me != null)
            {
                members.Add(me.Member.Name);
                me = me.Expression as MemberExpression;
            }

            var sb = new StringBuilder();
            for (int i = members.Count - 1; i >= 0; i--)
            {
                sb.Append(members[i]);
                if (i > 0) sb.Append('.');
            }

            return sb.ToString();
        }
    }
}

