using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
#endif
using UnityEngine;

using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;

namespace Grass
{
    [ExecuteAlways]
    [ExecuteInEditMode]
    public class GrassTool : MonoBehaviour
    {
        public static readonly string GrassShaderHeightPro = "_grassHeight";
        public GrassPainterData _grassData;
        public GameObject[] _terrainObjs;
        public GameObject[] _grassObjs;
        public GrassInstanceData[] _grassLodDatas;
        public int[] _grassCountArray;
        public Bounds _rootBounds;
        public Texture2D _colorTexture;
        public Texture2D _lightmapTexture;
        public Texture2D _shadowTexture;
        public Texture2D _normalmapTexture;
        public int _resolutionIndex = 0;
        public bool _showBounds;
        public bool _enablePainter;//刷草
        public int _selectIndex = 0;
        public BrushDesc[] _brushList;
        //public GrassStyleColorPainter _stylePainter;
        public List<MeshCollider> _meshColliders = new List<MeshCollider>();

        // 如果有 Color Layer数据，则走 WY 的一套
        // public GrassStyleColorPainter grassStyleColorPainter;

        public int TerrainsLayerMask
        {
            get
            {
                var layer = LayerMask.NameToLayer(QSConstant.rayCastLayer);
                layer = 1 << layer;
                return layer;
            }
        }
        public LayerMask TerrainsLayer
        {
            get
            {
                var layer = LayerMask.NameToLayer(QSConstant.rayCastLayer);
                return layer;
            }
        }

        public int GroundLayerMask
        {
            get
            {
                var layer = LayerMask.NameToLayer(QSConstant.groundLayer);
                layer = 1 << layer;
                return layer;
            }
        }
        public LayerMask GroundLayer
        {
            get
            {
                var layer = LayerMask.NameToLayer(QSConstant.groundLayer);
                return layer;
            }
        }

        [System.Serializable]
        public struct BrushDesc
        {
            public Vector2 _heightVar2;
            public float _brushSize;
            public float _grassDensity;
            public bool _fixedAngleEnable;
            public float _fixedAngleY;
            public bool _onlyScaleY;
        }

        public const int NODE_MAX_NUM = 512;//每个树节点最大的数据数量
        public const int MaxBrush = 6;
        public LayerMask m_RemoveLayer;
        public bool _debugTree;

        public TreeNode<GrassElementData>[] _roodNodes;

        List<HWorldBounds> _debugNodeDataBounds;

        //刷色
        float _terrainResolution;
        public float TerrainResolution => _terrainResolution;

        private void OnEnable()
        {
            GrassRenderer.Instance.Register(this);
        }

        private void OnDisable()
        {
            GrassRenderer.Instance.UnRegister(this);
        }

        public void SetEditorDatas()
        {
            SetEditorNodeData();
            SetGrassBrushs();
            UpdateNodeBounds();
        }

        public void SetEditorNodeData()
        {
            var datas = _grassData ? _grassData.Datas : null;
            var bounds = _grassData ? _grassData.RootBound : _rootBounds;
            _grassCountArray = new int[MaxBrush];
            _roodNodes = TreeNode<GrassElementData>.GetRootNodes(NODE_MAX_NUM, MaxBrush, bounds, datas);
            int index = 0;
            foreach (var root in _roodNodes)
            {
                _grassCountArray[index] = root.mElementCount;
                index++;
            }
        }

        void SetGrassBrushs()
        {
            SetGrassObjs();
            _brushList = new BrushDesc[MaxBrush];
            for (int i = 0; i < MaxBrush; i++)
            {
                _brushList[i]._heightVar2 = Vector2.one;
                _brushList[i]._brushSize = 5;
                _brushList[i]._grassDensity = 0.5f;
            }
        }
        public void SetGrassObjs()
        {
            if (_grassData == null)
            {
                _grassObjs = new GameObject[1];
                return;
            }

            if (_grassLodDatas == null || _grassLodDatas.Length == 0)
                _grassLodDatas = new GrassInstanceData[MaxBrush];

            int count = MaxBrush;
            for (int i = MaxBrush - 1; i > 0; i--)
            {
                if (_grassData.DrawObjArray[i] == null)
                {
                    count--;
                }
                else
                {
                    break;
                }
            }
            _grassObjs = new GameObject[count];
            for (int i = 0; i < count; i++)
            {
                _grassObjs[i] = _grassData.DrawObjArray[i];
                if (_grassObjs[i] != null)
                {
                    _grassLodDatas[i] = _grassObjs[i].GetComponent<GrassInstanceData>();
                    if (_grassLodDatas[i] == null)
                    {
                        Debug.LogError(_grassObjs[i].gameObject.name + "草从预制体缺少 GrassInstanceData 组件");
                    }
                    else
                    {
                        _grassLodDatas[i].Init();
                    }
                }
                else
                {
                    _grassLodDatas[i] = null;
                }
            }
        }


        public void OnTerrainsChanged()
        {
            CalculateRootBounds();
        }

        public void CalculateRootBounds()
        {
            Vector3 minSum = Vector3.one * 100000;
            Vector3 maxSum = -Vector3.one * 100000;
            if (_terrainObjs == null)
            {
                return;
            }

            if (_terrainObjs != null)
            {
                foreach (GameObject item in _terrainObjs)
                {
                    if (item == null) continue;

                    Terrain t = item.GetComponent<Terrain>();
                    MeshRenderer r = t ? null : item.GetComponent<MeshRenderer>();
                    Vector3 min = Vector3.zero, max = Vector3.zero;
                    if (t)
                    {
                        //Min/max bounds corners in world-space
                        min = t.transform.position; //Safe to assume terrain starts at origin
                        max = t.transform.position + t.terrainData.size; //Note, size is slightly more correct in height than bounds
                    }

                    if (r)
                    {
                        //World-space bounds corners
                        min = r.bounds.min;
                        max = r.bounds.max;
                    }

                    if (max.x > maxSum.x) maxSum.x = max.x;
                    if (max.y > maxSum.y) maxSum.y = max.y;
                    if (max.z > maxSum.z) maxSum.z = max.z;

                    if (min.x < minSum.x) minSum.x = min.x;
                    if (min.y < minSum.y) minSum.y = min.y;
                    if (min.z < minSum.z) minSum.z = min.z;
                }
            }

            _rootBounds = new Bounds(Vector3.zero, Vector3.zero);

            if (minSum == maxSum || (minSum == Vector3.one * 100000 && maxSum == -Vector3.one * 100000))
                return;

            maxSum.y = maxSum.y + 1f;  //修改：增加包围盒高度
            minSum.y = minSum.y - 1f;  //修改：下移包围盒底部边界
            _rootBounds.SetMinMax(minSum, maxSum);
            _terrainResolution = Mathf.Max(_rootBounds.size.x, _rootBounds.size.z);

            //Increase bounds height for flat terrains
            if (_rootBounds.size.y < 2f)
            {
                _rootBounds.Encapsulate(new Vector3(_rootBounds.center.x, _rootBounds.center.y + 1f, _rootBounds.center.z));
                _rootBounds.Encapsulate(new Vector3(_rootBounds.center.x, _rootBounds.center.y - 1f, _rootBounds.center.z));
            }

            var extent = Mathf.Max(_rootBounds.extents.x, _rootBounds.extents.z);
            _rootBounds.extents = new Vector3(extent, _rootBounds.extents.y, extent);
        }

        public void ResetBounds()
        {
            //CalculateRootBounds();
            if (_grassData != null)
            {
                _grassData.RootBound = _rootBounds;
                _roodNodes = TreeNode<GrassElementData>.GetRootNodes(NODE_MAX_NUM, MaxBrush, _rootBounds, _grassData.Datas);
                UpdateNodeBounds();
            }
        }

        public Transform GetFirstTerrain()
        {
            Transform terrain = null;
            for (int i = 0; i < _terrainObjs.Length; i++)
            {
                var t = _terrainObjs[i];
                if (t != null)
                {
                    terrain = t.transform;
                    break;
                }
            }
            return terrain;
        }

        public void OnRemoveSelectGrassObj()
        {
            //Debug.Log("GrassTool::OnRemoveGrassObj index = " + _selectIndex);

            for (int i = _selectIndex; i < MaxBrush; i++)
            {
                if (i == MaxBrush - 1)
                {
                    _brushList[i]._heightVar2 = Vector2.one;
                    _brushList[i]._brushSize = 5;
                    _brushList[i]._grassDensity = 0.5f;
                    _grassData.DrawObjArray[i] = null;
                    _grassCountArray[i] = 0;
                }
                else
                {
                    _brushList[i] = _brushList[i + 1];
                    _grassData.DrawObjArray[i] = _grassData.DrawObjArray[i + 1];
                    _grassCountArray[i] = _grassCountArray[i + 1];
                }
            }

        }

        public void OnGrassObjChanged(int index)
        {
            //Debug.Log("OnGrassObjChanged index = " + index);
            _grassData.DrawObjArray[index] = _grassObjs[index];
            if (_grassObjs[index] != null)
            {
                _grassLodDatas[index] = _grassObjs[index].GetComponent<GrassInstanceData>();
            }
            else
            {
                _grassLodDatas[index] = null;
            }
        }

        public void AddCacheDatasByCollider(byte grassType, BoxCollider collider, BrushDesc brushDesc)
        {
            if (_grassData == null)
            {
                Debug.LogError("GrassTool::AddCacheDatasByBounds::草数据不能为空，请先添加草数据！！！");
                return;
            }

            var grassObj = _grassData.DrawObjArray[grassType];

            if (grassObj == null)
            {
                Debug.LogError("GrassTool::AddCacheDatasByBounds:请先设置笔刷草预制！！！");
                return;
            }

            GrassInstanceData lodData;
            lodData = grassObj.GetComponent<GrassInstanceData>();
            if (lodData == null)
            {
                lodData = grassObj.AddComponent<GrassInstanceData>();
                lodData.Init();
            }

            if (lodData.GetLodMesh(0) == null || lodData.InstanceMat == null)
            {
                Debug.LogError("笔刷草预制不正确，请检查该预制是否存在 Mesh 和 Material ！！！");
                return;
            }

            if (!lodData.InstanceMat.enableInstancing)
            {
                Debug.LogError("请检查该预制 Material 是否支持 GPUInstance ！！！");
                return;
            }
            var bounds = new Bounds(collider.transform.position + (Vector3)(collider.transform.localToWorldMatrix * collider.center), collider.size);
            var centerPos = bounds.center;
            var hit = new RaycastHit();
            if (!Physics.Raycast(centerPos + 1000 * Vector3.up, Vector3.down, out hit, 10000, -1) && _rootBounds.Contains(hit.point))
            {
                return;
            }
            var grassSpacing = (1.1f - brushDesc._grassDensity) * 3;
            //var localToWorld = Matrix4x4.TRS(hit.point, collider.transform.rotation, Vector3.one);
            var localToWorld = collider.transform.localToWorldMatrix;
            var drawRadius = new Vector2(bounds.extents.x, bounds.extents.z).magnitude;
            var render = lodData.GetMeshRender();
            var magnitude = render.bounds.extents.magnitude;
            var treeCountOneAxis = (int)(new Vector2(collider.transform.lossyScale.x, collider.transform.lossyScale.z).magnitude / (magnitude * grassSpacing * 0.5f));
            var treeCount = (int)((treeCountOneAxis * treeCountOneAxis) * .5f);
            List<Vector3> posList = new List<Vector3>();
            List<Quaternion> rotationList = new List<Quaternion>();
            int index = 0;
            while (index < treeCount)
            {
                var rotation = Quaternion.Euler(Vector3.zero);
                //草刷绕Y轴随机旋转
                if (brushDesc._fixedAngleEnable)
                {
                    rotation = Quaternion.Euler(new Vector3(0, 360 * brushDesc._fixedAngleY, 0));
                }
                else
                {
                    rotation = Quaternion.Euler(new Vector3(0, UnityEngine.Random.Range(0, 360), 0));
                }

                var randomOffset = UnityEngine.Random.insideUnitCircle;
                randomOffset.x *= drawRadius;
                randomOffset.y *= drawRadius;

                index++;
                if (bounds.Contains(centerPos + new Vector3(randomOffset.x, 0, randomOffset.y)))
                {
                    Vector3 position = localToWorld * new Vector4(randomOffset.x, 0, randomOffset.y, 1);

                    var checkSucces = false;

                    foreach (var child in posList)
                    {
                        if (Vector3.Distance(child, position) < grassSpacing * 0.5f)
                        {
                            checkSucces = true;
                            break;
                        }
                    }

                    if (!checkSucces)
                    {
                        posList.Add(position);
                        rotationList.Add(rotation);
                    }
                }
            }

            index = 0;
            List<GrassElementData> datas = new List<GrassElementData>();
            foreach (var pos in posList)
            {
                if (Physics.Raycast(pos + 1000 * Vector3.up, Vector3.down, out hit, 10000, GroundLayerMask) && _rootBounds.Contains(hit.point))
                {
                    var onlyScaleY = brushDesc._onlyScaleY ? 0 : 1;
                    var diffcolor = SampleColor(hit.point);
                    var normal = SampleNormal(hit.point);
                    var lightmap = SampleLight(hit.point);
                    var aoMap = SampleShadow(hit.point);
                    diffcolor.a = aoMap.r;
                    var height = Random.Range(brushDesc._heightVar2.x, brushDesc._heightVar2.y);
                    var scale = render.transform.lossyScale;
                    var matrix = Matrix4x4.TRS(hit.point, rotationList[index], scale);
                    GrassElementData data = new GrassElementData();
                    data.InitData(matrix, diffcolor, normal, lightmap, height, onlyScaleY, grassType);
                    //Debug.Log(data.OnlyScaleY);
                    datas.Add(data);
                }
                index++;
            }

            AddCacheData(datas, grassSpacing);
        }


        public void AddCacheData(IEnumerable<GrassElementData> datas, float space)
        {
            _grassCountArray[_selectIndex] += _roodNodes[_selectIndex].AddDatas(datas, true, space);
        }

        public void RemoveCacheData(LayerMask checkLayer, Vector3 position, float radius = float.MaxValue)
        {
            int index = 0;
            foreach (var root in _roodNodes)
            {
                var layer = 1 << index;
                if ((checkLayer & layer) > 0)
                {
                    _grassCountArray[index] -= root.RemoveData(position, radius);
                }
                index++;
            }
        }

        public void ResetCacheDataHeight(Vector3 position, float radius = float.MaxValue)
        {
            _roodNodes[_selectIndex].ResetHieghtData(position, radius, _brushList[_selectIndex]._heightVar2);
        }

        public bool CheckBoundChanged()
        {
            if (_rootBounds != _grassData.RootBound)
            {
                return true;
            }
            return false;
        }

        public void DeleteAllCacheData()
        {
            foreach (var root in _roodNodes)
            {
                root.ClearData();
            }
            _grassCountArray = new int[MaxBrush];
        }

        public void DeleteSelectCacheData()
        {

            var bitData = 1 << _selectIndex;
            var removeLayer = 0 | bitData;
            RemoveCacheData(removeLayer, Vector3.zero);
        }

        // 重置 Datas数据， 把rootNodes中的临时数据，应用到_grassData的总数据中
        public void SaveCacheData()
        {
            _grassData.Datas.Clear();
            foreach (var root in _roodNodes)
            {
                if (root.mElementCount>0)
                    _grassData.Datas.AddRange(root.GetTreeAllElement());

            }
            //Debug.Log(_grassData.Datas.Count);
            UpdateNodeBounds();
        }

        public void EditorRenderGrass(CameraData cameraData, CommandBuffer cmd, int pass, bool depthWrite)
        {
            if (!SystemInfo.supportsInstancing ||
                cameraData.camera == null ||
                //cameraData.cameraType != CameraType.SceneView||
                _grassData == null)
                return;

            if (_roodNodes == null)
            {
                // 配置 Editor 专用草的显示数据
                SetEditorDatas();
                // 配置 Editor 专用的剔除用的 bound
                UpdateNodeBounds();
            }

            int index = 0;
            foreach (var root in _roodNodes)
            {
                if (_grassLodDatas!= null && _grassLodDatas.Length > index && _grassLodDatas[index] != null)
                {
                    // 执行渲染
                    root.RenderGrass(cameraData.camera,cmd, _grassLodDatas[index].GetLodMesh(0), _grassLodDatas[index].InstanceMat, pass, depthWrite);
                }
                index++;
            }
        }

        public bool SampleTextureToGrassData()
        {
            if (_grassData == null)
            {
                return false;
            }

            foreach (var rootNode in _roodNodes)
            {
                rootNode.SampleTextureToGrassData(this);
            }
            _grassData.RootBound = _rootBounds;
            return true;
        }
        public Color SampleColor(Vector3 position)
        {
            var colorMapUv = getUV(position);
            if (_colorTexture == null)
            {
                return Color.white;
            }
            Color color = _colorTexture.GetPixelBilinear(colorMapUv.x, colorMapUv.y);
            //Debug.Log("SampleColor : " + color);
            return color;
        }

        public Color SampleLight(Vector3 position)
        {
            Vector3 colorMapUv = getUV(position);
            if (_lightmapTexture == null)
            {
                return Color.white;
            }
            Color color = _lightmapTexture.GetPixelBilinear(colorMapUv.x, colorMapUv.y);
            //Debug.Log("SampleLight : " + color);
            return color;
        }

        public Color SampleShadow(Vector3 position)
        {
            Vector3 colorMapUv = getUV(position);
            if (_shadowTexture == null)
            {
                return Color.white;
            }
            Color color = _shadowTexture.GetPixelBilinear(colorMapUv.x, colorMapUv.y);
            //Debug.Log("SampleShadow : " + color);
            return color;
        }

        public Color SampleNormal(Vector3 position)
        {
            Vector3 colorMapUv = getUV(position);
            if (_normalmapTexture == null)
            {
                return Color.green;
            }
            Color color = _normalmapTexture.GetPixelBilinear(colorMapUv.x, colorMapUv.y);
            //Debug.Log("SampleNormal : " + color);
            return color;
        }
        Vector2 getUV(Vector3 position)
        {
            Vector2 colorMapUv;
            var size = Mathf.Max(_rootBounds.size.x, _rootBounds.size.z);
            var point = _rootBounds.center - new Vector3(size * 0.5f, size * 0.5f, size * 0.5f);

            colorMapUv.x = (position.x - point.x) / size;
            colorMapUv.y = (position.z - point.z) / size;

            return colorMapUv;
        }

        Vector2 getTempGrassColorUV(Vector3 position)
        {
            Bounds rootBounds = new Bounds(new Vector3(500,300,1264.072F), new Vector3(1264.072F,301,1264.072F));
            Vector2 colorMapUv;
            var size = Mathf.Max(rootBounds.size.x, rootBounds.size.z);
            var point = rootBounds.center - new Vector3(size * 0.5f, size * 0.5f, size * 0.5f);

            colorMapUv.x = (position.x - point.x) / size;
            colorMapUv.y = (position.z - point.z) / size;

            return colorMapUv;
        }

        public void UpdateNodeBounds()
        {
            if (_debugNodeDataBounds == null)
            {
                _debugNodeDataBounds = new List<HWorldBounds>();
            }

            _debugNodeDataBounds.Clear();

            foreach (var root in _roodNodes)
            {
                foreach (var node in root.GetAllDataNode())
                {
                    if (_grassLodDatas != null && _grassLodDatas[node.Type] != null && _grassLodDatas.Length > node.Type)
                    {
                        var b = node.GetCullingBounds(_grassLodDatas[node.Type].GetMeshHeight());
                        _debugNodeDataBounds.Add(b);
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_rootBounds.size == Vector3.zero || !_showBounds)
                return;

            Color color = new Color(0f, 0.66f, 1f, 0.25f);
            Gizmos.color = color;
            Gizmos.DrawCube(_rootBounds.center, _rootBounds.size);

            color = new Color(0f, 0.66f, 1f, 1f);
            Gizmos.color = color;
            Gizmos.DrawWireCube(_rootBounds.center, _rootBounds.size);
        }

        private void OnDrawGizmos()
        {
            if (_debugTree)
            {
                if (_debugNodeDataBounds != null && _debugNodeDataBounds.Count > 0)
                {
                    for (int i = 0; i < _debugNodeDataBounds.Count; i++)
                    {
                        Gizmos.color = Color.white;
                        Gizmos.DrawWireCube(_debugNodeDataBounds[i].Center, _debugNodeDataBounds[i].Extents * 2);
                    }
                }
            }

            #if UNITY_EDITOR
            var cameraPos = allMeshBound.center;
            cameraPos.y += allMeshBound.size.y;
            Gizmos.DrawSphere(cameraPos, 1);
            Handles.DrawWireCube(allMeshBound.center, allMeshBound.size);
            #endif
        }
#if UNITY_EDITOR
        public static void CheckLayer(string layer)
        {
            UnityEditor.SerializedObject tagMagager = new UnityEditor.SerializedObject(UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Tagmanager.asset"));
            UnityEditor.SerializedProperty it = tagMagager.GetIterator();
            while (it.NextVisible(true))
            {
                if (it.name.Equals("layers"))
                {
                    var layerI = 0;
                    for (int i = 0; i < it.arraySize; i++)
                    {
                        var sp = it.GetArrayElementAtIndex(i);
                        if (i <= 7)
                            continue;

                        if (sp.stringValue == layer)
                        {
                            if (i == 31)
                            {
                                sp.stringValue = "";
                                tagMagager.ApplyModifiedProperties();
                            }
                            else
                            {
                                return;
                            }

                        }
                    }

                    for (int i = 0; i < it.arraySize; i++)
                    {
                        var sp = it.GetArrayElementAtIndex(i);
                        if (i <= 7)
                            continue;

                        if (string.IsNullOrEmpty(sp.stringValue))
                        {
                            layerI = i;
                            break;
                        }
                    }

                    if (layerI != 0)
                    {
                        var sp = it.GetArrayElementAtIndex(layerI);
                        sp.stringValue = layer;
                        tagMagager.ApplyModifiedProperties();
                    }
                }
            }
        }
#endif

        #region UnDo

        public const string UnDoStr = "grassPaint";
        public const string UnDoStyleStr = "stylePaint";
        public void OnUndoRedoPerformed()
        {
            SetEditorNodeData();
            if (_stylePainter != null)
                _stylePainter.OnUndoRedoPerformed();
            UpdateNodeBounds();
        }

        #endregion

        #region 刷草颜色
        public void ResetCacheDataStyleColor(GrassElementData.ColorType layer)
        {
            ResetCacheDataStyleColor(layer, Vector3.zero, float.MaxValue);
        }

        public void ResetCacheDataStyleColor(GrassElementData.ColorType layer, Vector3 position, float radius)
        {
            foreach (var root in _roodNodes)
            {
                root.ResetCacheDataStyleColor(layer,  position, radius, SampleStyleColor);
            }
        }
        public GrassStyleColorPainter _stylePainter;
        public Color SampleStyleColor(Vector3 position, GrassElementData.ColorType layer)
        {
            var colorMapUv = getUV(position);
            if (_stylePainter == null || _stylePainter._layerCatchTextures[(int)layer] == null)
            {
                return Color.black;
            }

            var color = _stylePainter._layerCatchTextures[(int)layer].GetPixelBilinear(colorMapUv.x, colorMapUv.y);
            return color;
        }
        #endregion

        #region 刷草吸色color
        // 获取计算好的 Terrain 颜色
        public void ResetCacheDataTerrainColor()
        {
            foreach (var root in _roodNodes)
            {
                root.ResetCacheDataStyleColor(GrassElementData.ColorType.LightMap,  Vector3.zero, float.MaxValue, SampleTerrainColor);
            }
        }

        // 获取计算好的 Terrain 颜色
        public Color SampleTerrainColor(Vector3 position, GrassElementData.ColorType layer)
        {
            return SampleColor(position);
        }
        #endregion

        #region AutoGrassTool
        #if UNITY_EDITOR
        #if UNITY_EDITOR
        public Terrain terrain;
        public List<Transform> meshes;
        private Texture2D _alphamapTexture;
        private ComputeBuffer _targetPointBuffer;
        private Material _material;

        public bool scatting = false;
        public ComputeShader cs;
        public Shader processShader;

        public Dictionary<int, GrassScatterData> scatterGrassData;

        public ScatterType scatterType;
        public enum ScatterType
        {
            Terrain,
            UnityMesh
        }

        public class GrassScatterData
        {
            public int layerIndex;

            public Matrix4x4[] tempGrassData;

            public ComputeBuffer occlusionCullPointBuffer;

            [Header("模糊次数")]
            [Range(0, 10)]
            public int iteration;

            [Header("模糊半径")]
            [Range(0, 5)]
            public float blurSize;

            [Header("点云法线矫正")]
            [Range(0, 1)]
            public float scatterPointNormalCorrect;

            [Header("点云位置偏移")]
            [Range(0, 1000)]
            public float scatterPointTransformOffset;

            [Header("点云边缘过渡")]
            [Range(0, 1)]
            public float scatterPointEdgeTransition;

            [Header("点云高度过渡")]
            [Range(0, 1)]
            public float scatterPointHeightTransition;

            [Header("点云大小")]
            [Range(0, 5)]
            public float scatterPointSize;

            [Header("点云大小偏移")]
            [Range(0.0f, 10.0f)]
            public float scatterPointSizeOffset;

            [Header("点云随机旋转种子")]
            [Range(0.0f, 1.0f)]
            public float scatterPointRotate;

            [Header("点云密度")]
            [Range(0, 4096)]
            public int scatterPointDensity;

            [Header("噪声Tilling")]
            [Range(1.0f, 1000)]
            public float noiseTilling;

            [Header("噪声类型")]
            public NoiseType noiseType;
            public enum NoiseType
            {
                None,
                Perlin,
                ValueNoise,
                SimplexNoise
            }
        }

        public void ScatterGrassEnable(ScatterType scatterType)
        {
            this.scatterType = scatterType;

            if (processShader == null)
            {
                processShader = Shader.Find("ScatterGrass/DataProcess");
            }

            if (processShader != null)
            {
                _material = CoreUtils.CreateEngineMaterial(processShader);
            }

            switch (scatterType)
            {
                case ScatterType.Terrain:
                {
                    if (terrain != null)
                    {
                        terrain.drawInstanced = true;
                    }

                    #region AutoGrassTool-Voxel
                    InitVoxel();
                    InitHeightMap();
                    #endregion
                    InitScatterGrassData();
                    for (int i = 0; i < scatterGrassData.Count; i++)
                    {
                        UpdateTerrainScatterData(scatterGrassData[i]);
                    }

                    RenderPipelineManager.beginCameraRendering += RegisterDrawGrassPass;
                    TerrainCallbacks.textureChanged += TerrainTextureChange;
                    TerrainCallbacks.heightmapChanged += TerrainHeightMapChange;
                }
                    break;
                case ScatterType.UnityMesh:
                {
                    positionMap = new RenderTexture(4096, 4096, 32, RenderTextureFormat.ARGBFloat);
                    normalMap   = new RenderTexture(4096, 4096, 32, RenderTextureFormat.ARGBFloat);
                    depthMap    = new RenderTexture(4096, 4096, 32, RenderTextureFormat.Depth);
                    RenderPipelineManager.beginCameraRendering += RegisterDrawPositionMapPass;

                    InitScatterGrassData();
                    for (int i = 0; i < scatterGrassData.Count; i++)
                    {
                        UpdateMeshScatterData(scatterGrassData[i]);
                    }
                }
                    break;
            }
        }

        public void ScatterGrassDisable(ScatterType scatterType)
        {
            switch (scatterType)
            {
                case ScatterType.Terrain:
                {
                    if (terrain != null)
                    {
                        terrain.drawInstanced = false;
                    }

                    RenderPipelineManager.beginCameraRendering -= RegisterDrawGrassPass;
                    TerrainCallbacks.textureChanged -= TerrainTextureChange;
                    TerrainCallbacks.heightmapChanged -= TerrainHeightMapChange;
                }
                    break;
                case ScatterType.UnityMesh:
                {
                    positionMap.Release();
                    normalMap.Release();
                    depthMap.Release();
                    RenderPipelineManager.beginCameraRendering -= RegisterDrawPositionMapPass;
                }
                    break;
            }
            if (_targetPointBuffer != null)
            {
                _targetPointBuffer.Release();
            }
            _targetPointBuffer = null;

            foreach (var v in scatterGrassData.Values)
            {
                if (v.occlusionCullPointBuffer != null)
                {
                    v.occlusionCullPointBuffer.Release();
                }
                v.occlusionCullPointBuffer = null;
            }

            scatterGrassData.Clear();
            scatterGrassData = null;
        }

        public void ScatterGrassUpdate()
        {
            if(!scatting)
                return;

            if (scatterType == ScatterType.UnityMesh)
            {
                if(meshes == null)
                    return;
                GetAllMeshBounds();
            }

            CheckTransformChanged();
            CheckSelectIndexChanged();

            InitScatterGrassData();

            // if (scatterType == ScatterType.Terrain)
            // {
            //     UpdateTerrainScatterData(scatterGrassData[_selectIndex]);
            // }

            if (scatterType == ScatterType.UnityMesh)
            {
                UpdateMeshScatterData(scatterGrassData[_selectIndex]);
            }

            RenderAppendBufferGrass();
            for (int i = 0; i < scatterGrassData.Count; i++)
            {
                if (i != _selectIndex)
                {
                    if (scatterGrassData[i].tempGrassData != null)
                    {
                        RenderTempBufferGrass(scatterGrassData[i].tempGrassData, i);
                    }
                }
            }

            RenderVoxels();
        }

        private void TerrainHeightMapChange(Terrain terrain, RectInt heightRegion, bool synched)
        {
            // for (int i = 0; i < scatterGrassData.Count; i++)
            // {
            //     UpdateData(scatterGrassData[i]);
            // }
        }

        private void TerrainTextureChange(Terrain terrain, string textureName, RectInt texelRegion, bool synched)
        {
            // for (int i = 0; i < scatterGrassData.Count; i++)
            // {
            //     UpdateData(scatterGrassData[i]);
            // }
        }

        private void InitPointBuffer(int scatterPointDensity)
        {
            if (_targetPointBuffer != null)
            {
                _targetPointBuffer.Release();
                _targetPointBuffer = null;
            }

            if (scatterPointDensity != 0)
            {
                _targetPointBuffer = new ComputeBuffer(scatterPointDensity * scatterPointDensity, 16 * sizeof(float), ComputeBufferType.Append);
            }
        }

        private Texture2D _heightMap;
        public void InitHeightMap()
        {
            var res = 1025;
            _heightMap = new Texture2D(res, res, TextureFormat.RFloat, 0, false);
            if (terrain != null)
            {
                var pivot = terrain.transform.position;

                for (int i = 0; i < res; i++)
                {
                    for (int j = 0; j < res; j++)
                    {
                        var x = ((float)i / (res - 1)) * terrain.terrainData.size.x;
                        var z = ((float)j / (res - 1)) * terrain.terrainData.size.z;
                        var height = terrain.SampleHeight(pivot + new Vector3(x,0, z));
                        _heightMap.SetPixel(i, j, new Color(height, 0.0f, 0.0f,0.0f));
                    }
                }

                _heightMap.wrapMode = TextureWrapMode.Clamp;
                _heightMap.Apply();
            }
        }

        private void InitMeshScatterPointMap()
        {
            if(colorMap == null)
                colorMap = new RenderTexture(4096, 4096, 32, RenderTextureFormat.ARGBFloat);
            if (positionMap == null)
                positionMap = new RenderTexture(4096, 4096, 32, RenderTextureFormat.ARGBFloat);
            if (normalMap == null)
                normalMap   = new RenderTexture(4096, 4096, 32, RenderTextureFormat.ARGBFloat);
            if (depthMap == null)
                depthMap    = new RenderTexture(4096, 4096, 32, RenderTextureFormat.Depth);
        }

        private int _lateSelectIndex = -1;
        public void CheckSelectIndexChanged()
        {
            if (_selectIndex != _lateSelectIndex)
            {
                if (_lateSelectIndex != -1)
                {
                    if (scatterGrassData.ContainsKey(_lateSelectIndex))
                    {
                        scatterGrassData[_lateSelectIndex].tempGrassData = GetBufferData(_targetPointBuffer);

                        switch (scatterType)
                        {
                            case ScatterType.Terrain:
                                UpdateTerrainScatterData(scatterGrassData[_selectIndex]);
                                break;
                            case ScatterType.UnityMesh:
                                UpdateMeshScatterData(scatterGrassData[_selectIndex]);
                                break;
                        }
                    }
                }
            }

            _lateSelectIndex = _selectIndex;
        }

        public Matrix4x4[] GetBufferData(ComputeBuffer targetBuffer)
        {
            if (targetBuffer == null)
                return Array.Empty<Matrix4x4>();

            var countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
            ComputeBuffer.CopyCount(targetBuffer, countBuffer, 0);
            int[] counter = new int[1] { 0 };
            countBuffer.GetData(counter);
            var count = counter[0];

            if(count == 0)
                return new Matrix4x4[count];

            var data = new Matrix4x4[count];
            targetBuffer.GetData(data, 0, 0, count);

            countBuffer.Release();
            return data;
        }

        public Matrix4x4[] GetBufferData(ComputeBuffer targetBuffer, out int count)
        {
            var countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
            ComputeBuffer.CopyCount(targetBuffer, countBuffer, 0);
            int[] counter = new int[1] { 0 };
            countBuffer.GetData(counter);
            count = counter[0];
            countBuffer.Release();

            if(count <= 0)
                return Array.Empty<Matrix4x4>();

            var data = new Matrix4x4[count];
            if (targetBuffer != null)
            {
                targetBuffer.GetData(data);
            }
            else
            {
                return Array.Empty<Matrix4x4>();
            }

            return data;
        }

        public void UpdateSelectingData()
        {
            scatterGrassData[_selectIndex].tempGrassData = GetBufferData(_targetPointBuffer);
        }

        private int targetPointCloudCount;
        public void UpdateTerrainScatterData(GrassScatterData scatterData)
        {
            if(!scatting)
                return;
            if(terrain == null)
                return;

            var threadGroups = Mathf.CeilToInt(scatterData.scatterPointDensity / 8) + 1;

            _alphamapTexture = terrain.terrainData.alphamapTextures[Mathf.FloorToInt(scatterData.layerIndex/4)];
            var temp = RenderTexture.GetTemporary(_alphamapTexture.width, _alphamapTexture.height, 0);
            Graphics.Blit(_alphamapTexture, temp);
            var blurTarget = RenderTexture.GetTemporary(_alphamapTexture.width, _alphamapTexture.height, 0);
            Blur(scatterData, temp, blurTarget);
            Shader.SetGlobalTexture("_TerrainControlMap", blurTarget);
            Shader.SetGlobalTexture("_TerrainNormalMap", terrain.normalmapTexture);
            Shader.SetGlobalTexture("_TerrainHeightMap", terrain.terrainData.heightmapTexture);

            if (_colorTexture != null)
            {
                Shader.SetGlobalTexture("_TerrainColorMap", _colorTexture);
            }
            else
            {
                Shader.SetGlobalTexture("_TerrainColorMap", Texture2D.blackTexture);
            }

            if (_stylePainter != null)
            {
                Shader.SetGlobalTexture("_BrushColorMap", _stylePainter._layerTextures[1]);
            }

            InitPointBuffer(scatterData.scatterPointDensity);
            if(_targetPointBuffer == null)
                return;
            _targetPointBuffer.SetCounterValue(0);
            if (cs == null)
            {
                var shaderGUID = AssetDatabase.FindAssets("DataProcessComputerShader").FirstOrDefault();
                cs = AssetDatabase.LoadAssetAtPath<ComputeShader>(AssetDatabase.GUIDToAssetPath(shaderGUID));
            }
            var kernel = cs.FindKernel("AppendTerrainPointCloud");
            cs.SetInt("_TargetLayer", scatterData.layerIndex % 4);
            cs.SetInt("_NoiseType", (int)scatterData.noiseType);
            cs.SetInt("_ControlTexSize", scatterData.scatterPointDensity);
            cs.SetFloat("_ScatterPointSize", scatterData.scatterPointSize);
            cs.SetFloat("_ScatterPointRotate", scatterData.scatterPointRotate * Mathf.PI);
            cs.SetFloat("_ScatterPointSizeOffset", scatterData.scatterPointSizeOffset);
            cs.SetFloat("_ScatterPointEdgeTransition", scatterData.scatterPointEdgeTransition);
            cs.SetFloat("_ScatterPointHeightTransition", scatterData.scatterPointHeightTransition);
            cs.SetFloat("_ScatterPointTransformOffset", scatterData.scatterPointTransformOffset);
            cs.SetFloat("_ScatterPointNormalCorrect", scatterData.scatterPointNormalCorrect);
            cs.SetFloat("_NoiseTilling", scatterData.noiseTilling);
            var terrainScale = new Vector4(
                terrain.terrainData.size.x,
                terrain.terrainData.heightmapScale.y / terrain.terrainData.size.y,
                terrain.terrainData.size.z
            );

            cs.SetVector("_GrassToolBoundCenter", _rootBounds.center);
            cs.SetVector("_GrassToolBoundSize", _rootBounds.size);
            cs.SetVector("_TerrainHeightmapScale", terrainScale);
            cs.SetVector("_TerrainPositionWS", terrain.transform.position);
            cs.SetBuffer(kernel, "_FinalPointPositionBuffer", _targetPointBuffer);
            cs.SetTexture(kernel, "_HeightMap", _heightMap);
            if (_stylePainter._layerTextures[0] != null)
            {
                cs.SetTexture(kernel, "_BrushColorMap0", _stylePainter._layerTextures[0]);
            }
            else
            {
                cs.SetTexture(kernel, "_BrushColorMap0", Texture2D.blackTexture);
            }

            if (_stylePainter._layerTextures[1] != null)
            {
                cs.SetTexture(kernel, "_BrushColorMap1", _stylePainter._layerTextures[1]);
            }
            else
            {
                cs.SetTexture(kernel, "_BrushColorMap1", Texture2D.blackTexture);
            }

            // Clear Grass
            var voxelBuffer = new ComputeBuffer(_voxels.Length, Marshal.SizeOf(typeof(GPUVoxelizer.Voxel_t)), ComputeBufferType.Structured);
            voxelBuffer.SetData(_voxels);
            cs.SetVector("_VoxelBoundMin", allMeshBound.min);
            cs.SetVector("_VoxelBoundMax", allMeshBound.max);
            cs.SetVector("_VoxelDataSize", new Vector4(_data.Width, _data.Height, _data.Depth, _data.UnitLength));
            cs.SetBuffer(kernel, "_VoxelDataBuffer", voxelBuffer);

            cs.Dispatch(kernel,  threadGroups, threadGroups, 1);

            GetBufferData(_targetPointBuffer, out targetPointCloudCount);

            voxelBuffer.Release();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(temp);
            RenderTexture.ReleaseTemporary(blurTarget);
        }

        public RenderTexture colorMap;
        public RenderTexture positionMap;
        public RenderTexture normalMap;
        public RenderTexture depthMap;
        public void UpdateMeshScatterData(GrassScatterData scatterData)
        {
            if(!scatting)
                return;
            if(meshes == null)
                return;

            var threadGroups = Mathf.CeilToInt(scatterData.scatterPointDensity / 8);
            InitPointBuffer(scatterData.scatterPointDensity);
            if(_targetPointBuffer == null)
                return;
            _targetPointBuffer.SetCounterValue(0);
            if (cs == null)
            {
                var shaderGUID = AssetDatabase.FindAssets("DataProcessComputerShader").FirstOrDefault();
                cs = AssetDatabase.LoadAssetAtPath<ComputeShader>(AssetDatabase.GUIDToAssetPath(shaderGUID));
            }
            var kernel = cs.FindKernel("AppendMeshPointCloud");

            InitMeshScatterPointMap();

            cs.SetTexture(kernel, "_MeshPositionMapColorRT", colorMap);
            cs.SetTexture(kernel, "_MeshPositionMapPositionRT", positionMap);
            cs.SetTexture(kernel, "_MeshPositionMapNormalRT", normalMap);
            // Shader.SetGlobalTexture("_BrushColorMap", _stylePainter._layerTextures[1]);
            cs.SetTexture(kernel, "_BrushColorMap", Texture2D.blackTexture);

            cs.SetInt("_NoiseType", (int)scatterData.noiseType);
            cs.SetFloat("_ScatterPointDensity", scatterData.scatterPointDensity);
            cs.SetFloat("_ScatterPointSize", scatterData.scatterPointSize);
            cs.SetFloat("_ScatterPointRotate", scatterData.scatterPointRotate * Mathf.PI);
            cs.SetFloat("_ScatterPointSizeOffset", scatterData.scatterPointSizeOffset);
            cs.SetFloat("_ScatterPointEdgeTransition", scatterData.scatterPointEdgeTransition);
            cs.SetFloat("_ScatterPointHeightTransition", scatterData.scatterPointHeightTransition);
            cs.SetFloat("_ScatterPointTransformOffset", scatterData.scatterPointTransformOffset);
            cs.SetFloat("_ScatterPointNormalCorrect", scatterData.scatterPointNormalCorrect);
            cs.SetFloat("_NoiseTilling", scatterData.noiseTilling);

            cs.SetVector("_TerrainPosition", terrain.transform.position);
            cs.SetVector("_TerrainScale", terrain.terrainData.size);
            cs.SetVector("_MeshRendererBoundPositionWS", allMeshBound.center);
            cs.SetVector("_MeshRendererBoundSize", allMeshBound.size);

            cs.SetBuffer(kernel, "_FinalPointPositionBuffer", _targetPointBuffer);
            cs.Dispatch(kernel,  threadGroups, threadGroups, 1);

            GetBufferData(_targetPointBuffer, out targetPointCloudCount);
        }

        private void RenderTempBufferGrass(Matrix4x4[] data, int index)
        {
            var count = data.Length;

            if (count <= 0)
                return;

            // var sceneCamera = Camera.current;
            var allPointBuffer = new ComputeBuffer(count, 16 * sizeof(float), ComputeBufferType.Structured);
            allPointBuffer.SetData(data);

            if (scatterGrassData[index].occlusionCullPointBuffer != null)
            {
                scatterGrassData[index].occlusionCullPointBuffer.Release();
                scatterGrassData[index].occlusionCullPointBuffer = null;
            }
            scatterGrassData[index].occlusionCullPointBuffer = new ComputeBuffer(count, 16 * sizeof(float), ComputeBufferType.Append);
            scatterGrassData[index].occlusionCullPointBuffer.SetCounterValue(0);

            var kernel = cs.FindKernel("OcclusionCullingPointCloud");
            cs.SetInt("_InstanceCount", count);
            cs.SetFloat("_CullDistance", 100);
            // cs.SetMatrix("_VPMatrix", sceneCamera.projectionMatrix * sceneCamera.worldToCameraMatrix);
            // cs.SetVector("_CameraPos", sceneCamera.transform.position);
            // cs.SetVector("_CameraForward", sceneCamera.transform.forward);
            cs.SetBuffer(kernel, "_AllPointPositionBuffer", allPointBuffer);
            cs.SetBuffer(kernel, "_RenderPointPositionBuffer", scatterGrassData[index].occlusionCullPointBuffer);
            cs.Dispatch(kernel, Mathf.CeilToInt(count / 64.0f), 1, 1);
            allPointBuffer.Release();
        }

        private void RenderAppendBufferGrass()
        {
            if(targetPointCloudCount <= 0 || _targetPointBuffer == null)
                return;

            if (scatterGrassData[_selectIndex].occlusionCullPointBuffer != null)
            {
                scatterGrassData[_selectIndex].occlusionCullPointBuffer.Release();
                scatterGrassData[_selectIndex].occlusionCullPointBuffer = null;
            }
            scatterGrassData[_selectIndex].occlusionCullPointBuffer = new ComputeBuffer(targetPointCloudCount, 16 * sizeof(float), ComputeBufferType.Append);
            scatterGrassData[_selectIndex].occlusionCullPointBuffer.SetCounterValue(0);

            // var sceneCamera = Camera.current;

            // if (sceneCamera != null)
            {
                var kernel = cs.FindKernel("OcclusionCullingPointCloud");
                cs.SetInt("_InstanceCount", targetPointCloudCount);
                // cs.SetFloat("_CullDistance", CullingDis);
                cs.SetFloat("_CullDistance", 100);
                // cs.SetMatrix("_VPMatrix", sceneCamera.projectionMatrix * sceneCamera.worldToCameraMatrix);
                // cs.SetVector("_CameraPos", sceneCamera.transform.position);
                // cs.SetVector("_CameraForward", sceneCamera.transform.forward);
                cs.SetBuffer(kernel, "_AllPointPositionBuffer", _targetPointBuffer);
                cs.SetBuffer(kernel, "_RenderPointPositionBuffer", scatterGrassData[_selectIndex].occlusionCullPointBuffer);
                cs.Dispatch(kernel, Mathf.CeilToInt(targetPointCloudCount / 64.0f), 1, 1);

                // var countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
                // ComputeBuffer.CopyCount(scatterGrassData[_selectIndex].occlusionCullPointBuffer, countBuffer, 0);
                // int[] counter = new int[1] { 0 };
                // countBuffer.GetData(counter);
                // var renderCount = counter[0];
                // countBuffer.Release();
                //
                // if (renderCount <= 0)
                // {
                //     return;
                // }
                //
                // var grass = _grassObjs[_selectIndex];
                // var mesh = grass.GetComponent<MeshFilter>().sharedMesh;
                // var material = grass.GetComponent<Renderer>().sharedMaterial;
                //
                // var bounds = terrain.terrainData.bounds;
                // var positionBounds = new Bounds(
                //     terrain.transform.localToWorldMatrix.MultiplyPoint(
                //         bounds.center
                //     ),
                //     bounds.size
                // );
                //
                // var mat = new Material(material);
                // mat.SetBuffer("_PointMatrix", scatterGrassData[_selectIndex].occlusionCullPointBuffer);
                // Graphics.DrawMeshInstancedProcedural(mesh, 0, mat, positionBounds, renderCount);
            }
        }

        private ComputeShader _voxelizer;
        private GPUVoxelizer.Voxel_t[] _voxels;
        private GPUVoxelizer.GPUVoxelData _data;
        public void InitVoxel()
        {
            if (meshes == null)
            {
                return;
            }

            if (_voxelizer == null)
            {
                var shaderGUID = AssetDatabase.FindAssets("Voxelizer").FirstOrDefault();
                _voxelizer = AssetDatabase.LoadAssetAtPath<ComputeShader>(AssetDatabase.GUIDToAssetPath(shaderGUID));
            }
            var allCombines = new List<CombineInstance[]>();
            for (int i = 0; i < meshes.Count; i++)
            {
                if (meshes[i] != null)
                {
                    var meshFilters = meshes[i].GetComponentsInChildren<MeshFilter>();
                    var combines = new CombineInstance[meshFilters.Length];

                    for (int j = 0; j < meshFilters.Length; j++)
                    {
                        combines[j].mesh = meshFilters[j].sharedMesh;
                        combines[j].transform = meshFilters[j].transform.localToWorldMatrix;
                    }

                    allCombines.Add(combines);
                }
            }

            var mesh = new Mesh
            {
                indexFormat = IndexFormat.UInt32
            };
            foreach (var combine in allCombines)
            {
                mesh.CombineMeshes(combine);
            }

            mesh.bounds.SetMinMax(mesh.bounds.min * 1.1f, mesh.bounds.max * 1.1f);
            allMeshBound = mesh.bounds;
            if (mesh.bounds.size != Vector3.zero)
            {
                _data = GPUVoxelizer.Voxelize(_voxelizer, mesh, 512);
                _voxels = _data.GetData();
            }
        }

        private void RenderVoxels()
        {
            // if (scatterType == ScatterType.Terrain)
            // {
            //     if (_voxels != null)
            //     {
            //         var list = new List<Matrix4x4>();
            //         foreach (var voxel in _voxels)
            //         {
            //             if (!voxel.IsEmpty())
            //             {
            //                 list.Add(Matrix4x4.TRS(voxel.position, Quaternion.identity, Vector3.one * _data.UnitLength));
            //             }
            //         }
            //         Graphics.DrawMeshInstanced(mesh, 0, material, list);
            //     }
            // }

            // if (_targetPointBuffer != null)
            // {
            //     var list = new Matrix4x4[targetPointCloudCount];
            //     _targetPointBuffer.GetData(list);
            //     Graphics.DrawMeshInstanced(mesh, 0, material, list);
            // }
        }

        private Vector3 latePosition = Vector3.zero;
        private void CheckTransformChanged()
        {
            if (_terrainObjs[0].transform.position != latePosition)
            {
                InitScatterGrassData();
            }

            latePosition = _terrainObjs[0].transform.position;
        }

        public void InitScatterGrassData()
        {
            if (scatterGrassData == null)
            {
                scatterGrassData = new Dictionary<int, GrassScatterData>();
            }
            if (_grassObjs == null) return;
            for (int i = 0; i < _grassObjs.Length; i++)
            {
                if (!scatterGrassData.ContainsKey(i))
                {
                    scatterGrassData.Add(i, null);
                }

                if (scatterGrassData[i] == null)
                {
                    scatterGrassData[i] = new GrassScatterData();

                    scatterGrassData[i].scatterPointDensity = 0;//Random.Range(100, 150);
                    scatterGrassData[i].scatterPointSize = 2;
                    scatterGrassData[i].scatterPointTransformOffset = Random.Range(0.0f, 1000.0f);
                    scatterGrassData[i].scatterPointSizeOffset = 1;
                    scatterGrassData[i].scatterPointRotate = Random.Range(0.0f, 1.0f);
                    scatterGrassData[i].scatterPointNormalCorrect = 0.75f;

                    scatterGrassData[i].scatterPointEdgeTransition = 0;
                    scatterGrassData[i].iteration = 0;
                    scatterGrassData[i].blurSize = 0;

                    scatterGrassData[i].noiseTilling = Random.Range(101, 199);
                    scatterGrassData[i].noiseType = GrassScatterData.NoiseType.Perlin;
                }
            }
        }

        private void Blur(GrassScatterData scatterGrassData, RenderTexture src, RenderTexture dest)
        {
            int rtW = src.width;
            int rtH = src.height;

            RenderTexture buffer0 = RenderTexture.GetTemporary(rtW, rtH, 0);
            buffer0.filterMode = FilterMode.Bilinear;

            Graphics.Blit(src, buffer0);

            for (int i = 0; i < scatterGrassData.iteration; i++) {
                _material.SetFloat("_BlurSize", 1.0f + i * scatterGrassData.blurSize);

                RenderTexture buffer1 = RenderTexture.GetTemporary(rtW, rtH, 0);

                Graphics.Blit(buffer0, buffer1, _material, 0);

                RenderTexture.ReleaseTemporary(buffer0);
                buffer0 = buffer1;
                buffer1 = RenderTexture.GetTemporary(rtW, rtH, 0);

                Graphics.Blit(buffer0, buffer1, _material, 1);

                RenderTexture.ReleaseTemporary(buffer0);
                buffer0 = buffer1;
            }

            Graphics.Blit(buffer0, dest);
            RenderTexture.ReleaseTemporary(buffer0);
        }

        public Bounds AllMeshBound => allMeshBound;
        private Bounds allMeshBound;
        private List<Renderer> _renderers;
        private Material positionMaterial;
        private void GetAllMeshBounds()
        {
            _renderers = new List<Renderer>();
            foreach (var mesh in meshes)
            {
                if (mesh != null)
                {
                    _renderers.AddRange(mesh.GetComponentsInChildren<Renderer>());
                }
            }

            if (_renderers.Count > 0)
            {
                allMeshBound = _renderers[0].bounds;
                foreach (var renderer in _renderers)
                {
                    allMeshBound.Encapsulate(renderer.bounds);
                }
            }
        }

        public Matrix4x4[] GetVPM()
        {
            var cameraPos = allMeshBound.center;
            cameraPos.y += allMeshBound.size.y * 2;

            var V = Matrix4x4.identity;
            V.m22 = -V.m22;

            V = Matrix4x4.TRS(cameraPos, Quaternion.Euler(90, 0, 0), Vector3.one) * V;
            V = V.inverse;

            var edge = Mathf.Max(allMeshBound.size.x, allMeshBound.size.z) / 2;
            var P = Matrix4x4.Ortho(-edge, edge, -edge, edge, 0.0f, 1000);

            return new Matrix4x4[2]{V, P};
        }

        public void DrawData(CommandBuffer cmd)
        {
            if (positionMaterial == null)
            {
                positionMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("DrawPosition"));
            }

            if (_renderers != null)
            {
                foreach (var renderer in _renderers)
                {
                    cmd.DrawRenderer(renderer, positionMaterial);
                }
            }
        }

        public void DrawAlbedo(CommandBuffer cmd)
        {
            foreach (var renderer in _renderers)
            {
                cmd.DrawRenderer(renderer, renderer.sharedMaterial, 0, 0);
            }
        }

        public void DrawTempGrass(CommandBuffer cmd)
        {
            for (int index = 0; index < _grassObjs.Length; index++)
            {
                if (_selectIndex != index &&
                    scatterGrassData[index] != null &&
                    scatterGrassData[index].occlusionCullPointBuffer != null
                )
                {
                    var countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
                    ComputeBuffer.CopyCount(scatterGrassData[index].occlusionCullPointBuffer, countBuffer, 0);
                    int[] counter = new int[1] { 0 };
                    countBuffer.GetData(counter);
                    var renderCount = counter[0];
                    countBuffer.Release();

                    if (renderCount > 0)
                    {
                        var renderData = new Matrix4x4[renderCount];
                        scatterGrassData[index].occlusionCullPointBuffer.GetData(renderData);

                        var bounds = terrain.terrainData.bounds;
                        var positionBounds = new Bounds(
                            terrain.transform.localToWorldMatrix.MultiplyPoint
                            (
                                bounds.center
                            ),
                            bounds.size
                        );

                        var grass = _grassObjs[index];
                        var mesh = grass.GetComponent<MeshFilter>().sharedMesh;
                        var material = grass.GetComponent<Renderer>().sharedMaterial;

                        var mat = new Material(material);
                        mat.SetBuffer("_PointMatrix", scatterGrassData[index].occlusionCullPointBuffer);

                        cmd.DrawMeshInstancedProcedural(mesh, 0, mat, 0,  renderCount);
                        // Graphics.DrawMeshInstancedProcedural(mesh, 0, mat, positionBounds, renderCount);
                    }
                }
            }
        }

        public void DrawSelectingGrass(CommandBuffer cmd)
        {
            var countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
            ComputeBuffer.CopyCount(scatterGrassData[_selectIndex].occlusionCullPointBuffer, countBuffer, 0);
            int[] counter = new int[1] { 0 };
            countBuffer.GetData(counter);
            var renderCount = counter[0];
            countBuffer.Release();

            if (renderCount <= 0)
            {
                return;
            }

            var grass = _grassObjs[_selectIndex];
            var mesh = grass.GetComponent<MeshFilter>().sharedMesh;
            var material = grass.GetComponent<Renderer>().sharedMaterial;

            if (terrain != null)
            {
                var bounds = terrain.terrainData.bounds;
                var positionBounds = new Bounds(
                    terrain.transform.localToWorldMatrix.MultiplyPoint(
                        bounds.center
                    ),
                    bounds.size
                );

                var mat = new Material(material);
                mat.SetBuffer("_PointMatrix", scatterGrassData[_selectIndex].occlusionCullPointBuffer);
                cmd.DrawMeshInstancedProcedural(mesh, 0, mat, 0,  renderCount);
            }
        }

        private void RegisterDrawPositionMapPass(ScriptableRenderContext context, Camera camera)
        {
            if (camera.TryGetComponent(out UniversalAdditionalCameraData data))
            {
                var drawDataPass = new DrawPositionPass
                {
                    renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses
                };
                drawDataPass.SetUp(this);
                data.scriptableRenderer.EnqueuePass(drawDataPass);

                var drawColorPass = new DrawColorPass
                {
                    renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses
                };
                drawColorPass.SetUp(this);
                data.scriptableRenderer.EnqueuePass(drawColorPass);
            }
            else
            {
                var addData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();

                var drawDataPass = new DrawPositionPass
                {
                    renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses
                };
                drawDataPass.SetUp(this);
                addData.scriptableRenderer.EnqueuePass(drawDataPass);

                var drawColorPass = new DrawColorPass
                {
                    renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses
                };
                drawColorPass.SetUp(this);
                addData.scriptableRenderer.EnqueuePass(drawColorPass);
            }
        }

        private void RegisterDrawGrassPass(ScriptableRenderContext context, Camera camera)
        {
            if (camera.cameraType == CameraType.SceneView)
            {
                if (camera.TryGetComponent(out UniversalAdditionalCameraData data))
                {
                    var drawGrassPass = new DrawGrassPass
                    {
                        renderPassEvent = RenderPassEvent.BeforeRenderingOpaques
                    };
                    drawGrassPass.SetUp(this);
                    data.scriptableRenderer.EnqueuePass(drawGrassPass);
                }
                else
                {
                    var addData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();

                    var drawGrassPass = new DrawGrassPass
                    {
                        renderPassEvent = RenderPassEvent.BeforeRenderingOpaques
                    };
                    drawGrassPass.SetUp(this);
                    addData.scriptableRenderer.EnqueuePass(drawGrassPass);
                }
            }
        }

        private class DrawPositionPass : ScriptableRenderPass
        {
            internal ProfilingSampler profilingSampler = new ProfilingSampler("DrawPositionMap");

            private GrassTool _drawMeshPosition;

            public void SetUp(GrassTool drawMeshPosition)
            {
                _drawMeshPosition = drawMeshPosition;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, profilingSampler))
                {
                    var lateViewMatrix = renderingData.cameraData.GetViewMatrix();
                    var lateProjMatrix = renderingData.cameraData.GetProjectionMatrix();
                    var matrix = _drawMeshPosition.GetVPM();

                    context.ExecuteCommandBuffer(cmd);

                    cmd.Clear();
                    cmd.SetGlobalTexture("_MeshPositionMapPositionRT", _drawMeshPosition.positionMap);
                    cmd.SetGlobalTexture("_MeshPositionMapNormalRT", _drawMeshPosition.normalMap);
                    cmd.SetGlobalTexture("_MeshPositionMapDepthRT", _drawMeshPosition.depthMap);

                    cmd.SetViewProjectionMatrices(matrix[0], matrix[1]);

                    // Draw Data
                    cmd.SetRenderTarget(
                        new RenderTargetIdentifier[]
                        {
                            _drawMeshPosition.positionMap,
                            _drawMeshPosition.normalMap
                        },
                        _drawMeshPosition.depthMap
                    );
                    cmd.ClearRenderTarget(true, true, Color.black);
                    _drawMeshPosition.DrawData(cmd);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    cmd.SetViewProjectionMatrices(lateViewMatrix, lateProjMatrix);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    // Blur
                    int rtW = _drawMeshPosition.positionMap.width;
                    int rtH = _drawMeshPosition.positionMap.height;

                    var buffer0 = Shader.PropertyToID("Buffer0");
                    var buffer1 = Shader.PropertyToID("Buffer1");
                    cmd.EnableShaderKeyword("ONLYBLURALPHA");
                    cmd.GetTemporaryRT(buffer0, rtW, rtH, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBFloat);
                    cmd.GetTemporaryRT(buffer1, rtW, rtH, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBFloat);
                    cmd.Blit(_drawMeshPosition.positionMap, buffer0, _drawMeshPosition._material, 2);

                    var scatterData = _drawMeshPosition.scatterGrassData[_drawMeshPosition._selectIndex];
                    for (int i = 0; i < scatterData.iteration; i++) {
                        _drawMeshPosition._material.SetFloat("_BlurSize", 1.0f + i * scatterData.blurSize);

                        cmd.Blit(buffer0, buffer1, _drawMeshPosition._material, 0);
                        cmd.Blit(buffer1, buffer0, _drawMeshPosition._material, 1);
                    }

                    cmd.Blit(buffer0, _drawMeshPosition.positionMap);
                    cmd.ReleaseTemporaryRT(buffer0);
                    cmd.ReleaseTemporaryRT(buffer1);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    cmd.DisableShaderKeyword("ONLYBLURALPHA");
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        private class DrawColorPass : ScriptableRenderPass
        {
            internal ProfilingSampler profilingSampler = new ProfilingSampler("DrawColorMap");

            private GrassTool _drawMeshPosition;

            public void SetUp(GrassTool drawMeshPosition)
            {
                _drawMeshPosition = drawMeshPosition;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, profilingSampler))
                {
                    var lateViewMatrix = renderingData.cameraData.GetViewMatrix();
                    var lateProjMatrix = renderingData.cameraData.GetProjectionMatrix();
                    var matrix = _drawMeshPosition.GetVPM();

                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    cmd.SetGlobalTexture("_MeshPositionMapColorRT", _drawMeshPosition.colorMap);
                    cmd.SetViewProjectionMatrices(matrix[0], matrix[1]);
                    cmd.SetRenderTarget(_drawMeshPosition.colorMap);
                    cmd.ClearRenderTarget(true, true, Color.black);
                    cmd.EnableShaderKeyword("DEBUG_ALBEDO");
                    _drawMeshPosition.DrawAlbedo(cmd);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    cmd.DisableShaderKeyword("DEBUG_ALBEDO");
                    cmd.SetViewProjectionMatrices(lateViewMatrix, lateProjMatrix);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        private class DrawGrassPass : ScriptableRenderPass
        {
            internal ProfilingSampler profilingSampler = new ProfilingSampler("DrawGrassPass");

            private GrassTool _drawMeshPosition;

            public void SetUp(GrassTool drawMeshPosition)
            {
                _drawMeshPosition = drawMeshPosition;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, profilingSampler))
                {
                    _drawMeshPosition.DrawTempGrass(cmd);
                    _drawMeshPosition.DrawSelectingGrass(cmd);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        public class GPUVoxelizer
	    {
	        public static GPUVoxelData Voxelize(ComputeShader voxelizer, Mesh mesh, int resolution = 64)
	        {
	            mesh.RecalculateBounds();
                var bounds = mesh.bounds;
                bounds.SetMinMax(bounds.min * 1.1f, bounds.max * 1.1f);
	            return Voxelize(voxelizer, mesh.bounds, mesh, resolution);
	        }

	        public static GPUVoxelData Voxelize(ComputeShader voxelizer, Bounds bounds, Mesh mesh, int resolution = 64)
	        {
	            // From the specified resolution, calculate the unit length of one voxel
	            float maxLength = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
	            var unit = maxLength / resolution;

	            // half of the unit length
	            var hunit = unit * 0.5f;

	            // The bounds extended by "half of the unit length constituting one voxel" is defined as the scope of voxelization
	            var start = bounds.min - new Vector3(hunit, hunit, hunit);  // Minimum bounds to voxel
	            var end = bounds.max + new Vector3(hunit, hunit, hunit);    // Maximum bounds to voxel
	            var size = end - start;                                     // Size of bounds to voxel

	            // The size of three-dimensional voxel data is determined based on the unit length of the voxel and the scope of voxelization
	            int width = Mathf.CeilToInt(size.x / unit);
	            int height = Mathf.CeilToInt(size.y / unit);
	            int depth = Mathf.CeilToInt(size.z / unit);

	            // generate ComputeBuffer representing Voxel_t array
	            var voxelBuffer = new ComputeBuffer(width * height * depth, Marshal.SizeOf(typeof(Voxel_t)));
	            var voxels = new Voxel_t[voxelBuffer.count];
	            voxelBuffer.SetData(voxels); // initialize

	            // send voxel data to GPU
	    		voxelizer.SetVector("_Start", start);
	    		voxelizer.SetVector("_End", end);
	    		voxelizer.SetVector("_Size", size);

	    		voxelizer.SetFloat("_Unit", unit);
	    		voxelizer.SetFloat("_InvUnit", 1f / unit);
	    		voxelizer.SetFloat("_HalfUnit", hunit);
	    		voxelizer.SetInt("_Width", width);
	    		voxelizer.SetInt("_Height", height);
	    		voxelizer.SetInt("_Depth", depth);

	            // generate ComputeBuffer representing vertex array
	            var vertices = mesh.vertices;
	    		var vertBuffer = new ComputeBuffer(vertices.Length, Marshal.SizeOf(typeof(Vector3)));
	    		vertBuffer.SetData(vertices);

	            // generate ComputeBuffer representing triangle array
	    		var triangles = mesh.triangles;
	    		var triBuffer = new ComputeBuffer(triangles.Length, Marshal.SizeOf(typeof(int)));
	    		triBuffer.SetData(triangles);

	            // send mesh data to GPU kernel "SurfaceFront" and "SurfaceBack"
	    		var surfaceFrontKer = new Kernel(voxelizer, "SurfaceFront");
	    		voxelizer.SetBuffer(surfaceFrontKer.Index, "_VoxelBuffer", voxelBuffer);
	    		voxelizer.SetBuffer(surfaceFrontKer.Index, "_VertBuffer", vertBuffer);
	    		voxelizer.SetBuffer(surfaceFrontKer.Index, "_TriBuffer", triBuffer);

	            // set triangle count in a mesh
	            var triangleCount = triBuffer.count / 3;
	    		voxelizer.SetInt("_TriangleCount", triangleCount);

	            // execute surface construction in front triangles
	    		voxelizer.Dispatch(surfaceFrontKer.Index, triangleCount / (int)surfaceFrontKer.ThreadX + 1, (int)surfaceFrontKer.ThreadY, (int)surfaceFrontKer.ThreadZ);

	            // execute surface construction in back triangles
	    		var surfaceBackKer = new Kernel(voxelizer, "SurfaceBack");
	    		voxelizer.SetBuffer(surfaceBackKer.Index, "_VoxelBuffer", voxelBuffer);
	    		voxelizer.SetBuffer(surfaceBackKer.Index, "_VertBuffer", vertBuffer);
	    		voxelizer.SetBuffer(surfaceBackKer.Index, "_TriBuffer", triBuffer);
	    		voxelizer.Dispatch(surfaceBackKer.Index, triangleCount / (int)surfaceBackKer.ThreadX + 1, (int)surfaceBackKer.ThreadY, (int)surfaceBackKer.ThreadZ);

	            // send voxel data to GPU kernel "Volume"
	            var volumeKer = new Kernel(voxelizer, "Volume");
	            voxelizer.SetBuffer(volumeKer.Index, "_VoxelBuffer", voxelBuffer);

	            // execute to fill voxels inside of a mesh
	            voxelizer.Dispatch(volumeKer.Index, width / (int)volumeKer.ThreadX + 1, height / (int)volumeKer.ThreadY + 1, (int)surfaceFrontKer.ThreadZ);

	    		// dispose unnecessary mesh data
	    		vertBuffer.Release();
	    		triBuffer.Release();

	    		return new GPUVoxelData(voxelBuffer, width, height, depth, unit);
	        }

	        public class GPUVoxelData : System.IDisposable {

	    	    public ComputeBuffer Buffer { get { return buffer; } }

	    	    public int Width { get { return width; } }
	    	    public int Height { get { return height; } }
	    	    public int Depth { get { return depth; } }
	    	    public float UnitLength { get { return unitLength; } }

	    	    int width, height, depth;
	    	    float unitLength;

	    	    ComputeBuffer buffer;
	    	    Voxel_t[] voxels;

	    	    public GPUVoxelData(ComputeBuffer buf, int w, int h, int d, float u) {
	    		    buffer = buf;
	    		    width = w;
	    		    height = h;
	    		    depth = d;
	    		    unitLength = u;
	    	    }

	    	    public Voxel_t[] GetData() {
	    		    // cache
	    		    if(voxels == null) {
	    			    voxels = new Voxel_t[Buffer.count];
	    			    Buffer.GetData(voxels);
	    		    }
	    		    return voxels;
	    	    }

	    	    public void Dispose() {
	    		    buffer.Release();
	    	    }

	        }

	        [StructLayout(LayoutKind.Sequential)]
	        public struct Voxel_t
	        {
	    	    public Vector3 position;
	    	    public uint fill;
	    	    public uint front;

	    	    public bool IsFrontFace()
	    	    {
	    		    return fill > 0 && front > 0;
	    	    }

	    	    public bool IsBackFace()
	    	    {
	    		    return fill > 0 && front < 1;
	    	    }

	    	    public bool IsEmpty()
	    	    {
	    		    return fill < 1;
	    	    }
	        }

	        public class Kernel
	        {
	    	    public int Index { get { return index; } }
	    	    public uint ThreadX { get { return threadX; } }
	    	    public uint ThreadY { get { return threadY; } }
	    	    public uint ThreadZ { get { return threadZ; } }

	    	    int index;
	    	    uint threadX, threadY, threadZ;

	    	    public Kernel(ComputeShader shader, string key)
	    	    {
	    		    index = shader.FindKernel(key);
	    		    if (index < 0)
	    		    {
	    			    Debug.LogWarning("Can't find kernel");
	    			    return;
	    		    }
	    		    shader.GetKernelThreadGroupSizes(index, out threadX, out threadY, out threadZ);
	    	    }
	        }
        }
        public void OnValidate()
        {
            InitScatterGrassData();
            if (scatterGrassData.Count == 0) return;
            switch (scatterType)
            {
                case ScatterType.Terrain:
                    UpdateTerrainScatterData(scatterGrassData[_selectIndex]);
                    break;
                case ScatterType.UnityMesh:
                    UpdateMeshScatterData(scatterGrassData[_selectIndex]);
                    break;
            }
        }

        private void Update()
        {
            ScatterGrassUpdate();
        }
#endif
#endif
#endregion
    }
}

