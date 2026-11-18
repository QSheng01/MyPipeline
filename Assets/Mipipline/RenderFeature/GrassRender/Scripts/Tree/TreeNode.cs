using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Grass
{
    /// <summary>
    /// 树节点
    /// </summary>

    public class TreeNode<T> where T : TreeElementData
    {

        List<T> _dataList;

        TreeNode<T>[] _childNodes;

        Bounds _bounds;

        int _maxDataNum;
        int _elementCount;
        Matrix4x4[] _matrixDatas;

        bool _needUpdate;
        int _type;
        public int Type => _type;
        public Bounds mBounds => _bounds;
        public int mElementCount => _elementCount;

        public TreeNode(int type)
        {
            _type = type;
        }

        public MaterialPropertyBlock mPropertyBlock
        {
            get
            {
                if (_propertyBlock == null)
                    _propertyBlock = new MaterialPropertyBlock();
                return _propertyBlock;
            }
        }

        MaterialPropertyBlock _propertyBlock;
        //SphericalHarmonicsL2[] _lightprobes;
        //Vector4[] _occlusionprobes;
        //public static TreeNode<T> GetRootNode(int maxNum, Bounds bound, IEnumerable<T> datas)
        //{
        //    var rootNode = new TreeNode<T>();
        //    rootNode.InitNode(maxNum, bound);
        //    if (datas != null)
        //    {
        //        rootNode.AddDatas(datas);
        //    }
        //    return rootNode;
        //}

        public static TreeNode<T>[] GetRootNodes(int maxNum, int length, Bounds bound, IEnumerable<T> datas)
        {
            var rootNodes = new TreeNode<T>[length];
            List<T>[] dataArray = new List<T>[length];
            if (datas != null)
            {
                var etor = datas.GetEnumerator();
                while (etor.MoveNext())
                {
                    var data = etor.Current;
                    if (dataArray[data.Type] == null)
                    {
                        dataArray[data.Type] = new List<T>();
                    }
                    dataArray[data.Type].Add(data);
                }
            }

            for (int i = 0; i < length; i++)
            {
                rootNodes[i] = new TreeNode<T>(i);
                rootNodes[i].InitNode(maxNum, bound);
            }

            int index = 0;
            foreach (var data in dataArray)
            {
                if (data != null)
                {
                    rootNodes[index].AddDatas(data);
                }
                index++;
            }

            return rootNodes;
        }

        public TreeNode<T>[] GetAllDataNode()
        {
            List<TreeNode<T>> temp = new List<TreeNode<T>>();
            if (_childNodes != null)
            {
                for (int i = 0; i < _childNodes.Length; i++)
                {
                    temp.AddRange(_childNodes[i].GetAllDataNode());
                }
            }
            else
            {
                if (_dataList.Count > 0)
                {
                    temp.Add(this);
                }
            }
            return temp.ToArray();
        }

        public List<T> GetTreeAllElement()
        {
            List<T> tempData = new List<T>();
            if (_childNodes == null)
            {
                tempData.AddRange(_dataList);
            }
            else
            {
                for (int i = 0; i < _childNodes.Length; i++)
                {
                    tempData.AddRange(_childNodes[i].GetTreeAllElement());
                }
            }
            return tempData;
        }

        public void InitNode(int maxNum, Bounds bound)
        {
            _bounds = bound;
            _maxDataNum = maxNum;
            _elementCount = 0;
            _dataList = new List<T>();
        }

        public void ResetMaxNum(int maxNum)
        {
            _maxDataNum = maxNum;
            var allData = GetTreeAllElement();
            ClearData();
            AddDatas(allData);
        }

        public void ResetBound(Bounds bound)
        {
            _bounds = bound;
            var allData = GetTreeAllElement();
            ClearData();
            AddDatas(allData);
        }

        public int AddDatas(IEnumerable<T> dataArray, bool needCheck = false, float space = 0)
        {
            int count = 0;

            foreach (var data in dataArray)
            {
                if (AddData(data, needCheck, space))
                {
                    count++;
                }
            }
            if (count > 0)
            {
                ResetTreeInstancesDatas();
            }
            return count;
        }

        private bool AddData(T data, bool needCheck = false, float space = 0)
        {
            if (_childNodes != null || _dataList.Count >= _maxDataNum)
            {
                if (AddToChildNode(data, needCheck, space))
                {
                    _elementCount++;
                    return true;
                }

                return false;
            }

            bool checkFail = false;
            if (needCheck)
            {
                for (int i = 0; i < _dataList.Count; i++)
                {
                    if (Vector3.Distance(data.Pos, _dataList[i].Pos) < space * 0.5f)
                    {
                        checkFail = true;
                        break;
                    }
                }
            }
            if (!checkFail)
            {
                _dataList.Add(data);
                _needUpdate = true;
                _elementCount++;
            }

            return !checkFail;
        }

        private bool AddToChildNode(T data, bool needCheck = false, float space = 0)
        {
            if (_childNodes == null)
            {
                CreateChildNode();
            }
            var node = GetChildNodeByElementPos(data.Pos);

            if (node == null)
            {
                return false;
            }
            return node.AddData(data, needCheck, space);
        }

        public int RemoveData(Vector3 centerPos, float radius)
        {
            int count = 0;
            var distance = Vector2.Distance(new Vector2(_bounds.center.x, _bounds.center.z), new Vector2(centerPos.x, centerPos.z));
            if ((_bounds.size.x * Mathf.Sqrt(2) + radius) >= distance)
            {
                if (_childNodes != null)
                {
                    for (int i = 0; i < _childNodes.Length; i++)
                    {
                        count += _childNodes[i].RemoveData(centerPos, radius);
                    }
                }
                else
                {
                    bool needReset = false;
                    for (int i = 0; i < _dataList.Count; i++)
                    {
                        var data = _dataList[i];
                        var x = centerPos.x - data.Pos.x;
                        var z = centerPos.z - data.Pos.z;
                        if (Mathf.Sqrt(x * x + z * z) < radius)
                        {
                            needReset = true;
                            _dataList.Remove(data);
                            count++;
                            i--;
                        }
                    }

                    if (needReset)
                        ResetInstancesDatas();
                }
            }

            _elementCount -= count;

            if (_childNodes != null && _elementCount <= _maxDataNum)
            {
                var allElement = GetTreeAllElement();
                for (int i = 0; i < _childNodes.Length; i++)
                {
                    _childNodes[i].ClearData();
                }
                _childNodes = null;
                _dataList.AddRange(allElement);
                ResetInstancesDatas();
            }
            return count;
        }

        public void ResetHieghtData(Vector3 centerPos, float radius, Vector2 heightRange)
        {
            var distance = Vector2.Distance(new Vector2(_bounds.center.x, _bounds.center.z), new Vector2(centerPos.x, centerPos.z));
            if ((_bounds.size.x * Mathf.Sqrt(2) + radius) >= distance)
            {
                if (_childNodes != null)
                {
                    for (int i = 0; i < _childNodes.Length; i++)
                    {
                        _childNodes[i].ResetHieghtData(centerPos, radius, heightRange);
                    }
                }
                else
                {
                    bool reset = false;
                    for (int i = 0; i < _dataList.Count; i++)
                    {
                        var data = _dataList[i];
                        var x = centerPos.x - data.Pos.x;
                        var z = centerPos.z - data.Pos.z;
                        if (Mathf.Sqrt(x * x + z * z) < radius)
                        {
                            //Debug.Log("ResetHieghtData");
                            var height = UnityEngine.Random.Range(heightRange.x, heightRange.y);
                            data.HeightScale = height;
                            _dataList[i] = data;
                            reset = true;
                        }
                    }
                    if (reset)
                    {
                        ResetInstancesDatas();
                    }
                }
            }
        }

        private void CreateChildNode()
        {
            _childNodes = new TreeNode<T>[4];
            for (int i = 0; i < _childNodes.Length; i++)
            {
                int signX = 1;
                int signY = 1;
                if (i == 1 || i == 2)
                {
                    signX = -1;
                }
                if (i == 2 || i == 3)
                {
                    signY = -1;
                }
                Vector3 centerOffset = new Vector3(_bounds.size.x / 4 * signX, 0, _bounds.size.z / 4 * signY);
                Vector3 size = new Vector3(_bounds.size.x / 2, _bounds.size.y, _bounds.size.z / 2);
                Bounds bound = new Bounds(_bounds.center + centerOffset, size);
                _childNodes[i] = new TreeNode<T>(_type);
                _childNodes[i].InitNode(_maxDataNum, bound);
            }

            for (int i = 0; i < _dataList.Count; i++)
            {
                var node = GetChildNodeByElementPos(_dataList[i].Pos);
                if (node != null)
                {
                    node.AddData(_dataList[i]);
                }

            }
            _dataList.Clear();
        }
        public List<T> GetResetData(Vector3 centerPos, float radius)
        {
            List<T> datas = new List<T>();
            var distance = Vector2.Distance(new Vector2(_bounds.center.x, _bounds.center.z), new Vector2(centerPos.x, centerPos.z));
            if ((_bounds.size.x * Mathf.Sqrt(2) + radius) >= distance)
            {
                if (_childNodes != null)
                {
                    for (int i = 0; i < _childNodes.Length; i++)
                    {
                        datas.AddRange(_childNodes[i].GetResetData(centerPos, radius));
                    }

                    var allElement = GetTreeAllElement();

                    if (allElement.Count <= _maxDataNum)
                    {
                        for (int i = 0; i < _childNodes.Length; i++)
                        {
                            _childNodes[i].ClearData();
                        }
                        _childNodes = null;
                        _dataList.AddRange(allElement);
                    }
                }
                else
                {
                    for (int i = 0; i < _dataList.Count; i++)
                    {
                        var data = _dataList[i];
                        var x = centerPos.x - data.Pos.x;
                        var z = centerPos.z - data.Pos.z;
                        if (Mathf.Sqrt(x * x + z * z) < radius)
                        {
                            datas.Add(data);
                        }
                    }
                }
            }
            return datas;
        }

        private TreeNode<T> GetChildNodeByElementPos(Vector3 pos)
        {
            int index = -1;
            for (int i = 0; i < _childNodes.Length; i++)
            {
                if (_childNodes[i]._bounds.Contains(pos))
                {
                    index = i;
                    break;
                }
            }
            if (index == -1)
            {
                return null;
            }
            return _childNodes[index];
        }

        public void ClearData()
        {
            if (_childNodes != null)
            {
                for (int i = 0; i < _childNodes.Length; i++)
                {
                    _childNodes[i].ClearData();
                }
                _childNodes = null;
            }
            else
            {
                _dataList.Clear();
                ResetInstancesDatas();
            }
            _elementCount = 0;
        }



        public void ResetTreeInstancesDatas(bool force = false)
        {
            if (_childNodes != null)
            {
                for (int i = 0; i < _childNodes.Length; i++)
                {
                    _childNodes[i].ResetTreeInstancesDatas(force);
                }
            }
            else if (force || _needUpdate)
            {
                ResetInstancesDatas();
            }
        }

        private void ResetInstancesDatas()
        {
            if (_dataList == null || _dataList.Count == 0)
            {
                _matrixDatas = null;
                return;
            }

            if (_matrixDatas == null)
                _matrixDatas = new Matrix4x4[_maxDataNum];

            for (int i = 0; i < _dataList.Count; i++)
            {
                _matrixDatas[i] = _dataList[i].Matrix;
            }
            //Debug.Log("ResetInstancesDatas");
            _needUpdate = false;

            //ResetLightProbes();
        }
        //public void ResetLightProbes()
        //{
        //    if (_lightprobes == null)
        //        _lightprobes = new SphericalHarmonicsL2[_maxDataNum];

        //    if (_occlusionprobes == null)
        //        _occlusionprobes = new Vector4[_maxDataNum];

        //    var pos = new Vector3[_dataList.Count];
        //    for (int i = 0; i < _dataList.Count; i++)
        //    {
        //        pos[i] = _dataList[i].Pos;
        //    }
        //    LightProbes.CalculateInterpolatedLightAndOcclusionProbes(pos, _lightprobes, _occlusionprobes);
        //    mPropertyBlock.CopySHCoefficientArraysFrom(_lightprobes);
        //    mPropertyBlock.CopyProbeOcclusionArrayFrom(_occlusionprobes);
        //}

        /// <summary>
        /// Editor 场景下专用草的显示函数
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="cmd"></param>
        /// <param name="mesh"></param>
        /// <param name="mat"></param>
        public void RenderGrass(Camera camera, CommandBuffer cmd, Mesh mesh, Material mat, int pass, bool depthWrite)
        {
            if (mesh == null || mat == null)
                return;

            // 有子节点，递归渲染子节点
            if (_childNodes != null)
            {
                foreach (var node in _childNodes)
                {
                    node.RenderGrass(camera, cmd, mesh, mat, pass, depthWrite);
                }
            }
            else if (_dataList.Count > 0)
            {
                // 这个函数取得给定的摄像机的 frustum 并返回它的六个面。
                Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
                // 检测 AABB 是否在 frustum 中， 并且检测是否在可见距离内。
                if (GeometryUtility.TestPlanesAABB(planes, _bounds) &&
                    CheckDist(camera.transform.position, _bounds.center, _bounds.extents.x))
                {
                    if (_matrixDatas == null)
                        ResetInstancesDatas();
                    // 开始渲染
                    mat.SetFloat("_ZTest", (float)(depthWrite ? CompareFunction.Less : CompareFunction.Equal));
                    mat.SetFloat("_ZWrite", depthWrite ? 1f : 0f);
                    cmd.DrawMeshInstanced(mesh, 0, mat, pass, _matrixDatas, _dataList.Count, mPropertyBlock);
                }
                //Debug.Log("RenderGrass " + _dataList.Count);
            }
        }

        // 检测是否在可见距离。
        private bool CheckDist(in float3 cameraPos, in float3 centerPos, in float extent)
        {
            if ((math.all(cameraPos.xz - extent > centerPos.xz) && math.all(cameraPos.xz + extent < centerPos.xz))||
                (math.length(cameraPos - centerPos) - extent < 400 && cameraPos.y - centerPos.y < 400))
                return true;

            return false;
        }

        public void ResetType(int type)
        {
            _type = type;
            if (_childNodes != null)
            {
                for (int i = 0; i < _childNodes.Length; i++)
                {
                    _childNodes[i].ResetType(type);
                }
            }
        }

        public HWorldBounds GetCullingBounds(float height)
        {
            HWorldBounds bounds = new HWorldBounds();

            if (_dataList == null)
                return bounds;

            float minY = float.MaxValue;
            float maxY = float.MinValue;
            foreach (var data in _dataList)
            {
                if (data.Pos.y < minY)
                    minY = data.Pos.y;
                var h = data.Pos.y + height * data.Matrix.m11;
                if (h > maxY)
                {
                    maxY = h;
                }
            }
            var rtBound = new Bounds();
            rtBound.SetMinMax(new Vector3(_bounds.min.x, minY, _bounds.min.z), new Vector3(_bounds.max.x, maxY, _bounds.max.z));
            bounds = new HWorldBounds(rtBound.center, rtBound.extents);
            return bounds;
        }



        public void ResetCacheDataStyleColor(GrassElementData.ColorType type, Vector3 position, float radius, System.Func<Vector3, GrassElementData.ColorType, Color> sampleStyleColor)
        {
            var distance = Vector2.Distance(new Vector2(_bounds.center.x, _bounds.center.z), new Vector2(position.x, position.z));
            if ((_bounds.size.x * Mathf.Sqrt(2) + radius) >= distance)
            {
                if (_childNodes != null)
                {
                    for (int i = 0; i < _childNodes.Length; i++)
                    {
                        _childNodes[i].ResetCacheDataStyleColor(type, position, radius, sampleStyleColor);
                    }
                }
                else
                {
                    bool reset = false;
                    for (int i = 0; i < _dataList.Count; i++)
                    {
                        var data = _dataList[i];
                        var x = position.x - data.Pos.x;
                        var z = position.z - data.Pos.z;
                        if (Mathf.Sqrt(x * x + z * z) < radius)
                        {
                            var color = sampleStyleColor(data.Pos, type);
                            // 刷色的数据需要刷在草的 LightMap 通道数据
                            data.SetColor(color, GrassElementData.ColorType.LightMap);
                            _dataList[i] = data;
                            reset = true;
                        }
                    }

                    if (reset)
                    {
                        ResetInstancesDatas();
                    }
                }
            }
        }

        internal void SampleTextureToGrassData(GrassTool grassTool)
        {
            if (_childNodes != null)
            {
                for (int i = 0; i < _childNodes.Length; i++)
                {
                    _childNodes[i].SampleTextureToGrassData(grassTool);
                }
            }
            else
            {
                SampleTexture(grassTool);
            }
        }

        private void  SampleTexture(GrassTool grassTool)
        {
            if (_dataList == null || _dataList.Count == 0)
            {
                _matrixDatas = null;
                return;
            }

            if (_matrixDatas == null)
                _matrixDatas = new Matrix4x4[_maxDataNum];

            for (int i = 0; i < _dataList.Count; i++)
            {
                var element = _dataList[i];
                var diff = grassTool.SampleColor(element.Pos);
                var light = grassTool.SampleLight(element.Pos);
                var aoMap = grassTool.SampleShadow(element.Pos);
                var normal = grassTool.SampleNormal(element.Pos);

                diff.a = aoMap.r;
                // 如果是 WY 这边导入的草数据，是走的吸色地形，外加刷2层颜色
                if (grassTool._stylePainter)
                {
                    // 前2通道
                    // diff = grassTool.SampleStyleColor(element.Pos, GrassElementData.ColorType.Diff);
                    // normal = grassTool.SampleStyleColor(element.Pos, GrassElementData.ColorType.Normal);
                    aoMap = Color.black;
                    var painterColor = grassTool.SampleStyleColor(element.Pos, GrassElementData.ColorType.Diff);
                    element.DiffColor = GrassElementData.ConvertColorToFloat(diff, GrassElementData.ColorType.Diff);
                    element.NormalMapColor = GrassElementData.ConvertColorToFloat(normal, GrassElementData.ColorType.Normal);
                    element.LightMapColor = GrassElementData.ConvertColorToFloat(painterColor, GrassElementData.ColorType.LightMap);
                }
                else
                {
                    element.DiffColor = GrassElementData.ConvertColorToFloat(diff, GrassElementData.ColorType.Diff);
                    element.NormalMapColor = GrassElementData.ConvertColorToFloat(normal, GrassElementData.ColorType.Normal);
                    element.LightMapColor = GrassElementData.ConvertColorToFloat(Color.black, GrassElementData.ColorType.LightMap);
                }

                //Debug.Log(light + " --- "+ element.LightMapColor);
                _dataList[i] = element;
                _matrixDatas[i] = _dataList[i].Matrix;
            }
            //for (int i = 0; i < elements.Count; i++)
            //{
            //    var element = elements[i];
            //    var lightMap = SampleLight(element.Pos);
            //    var aoMap = SampleShadow(element.Pos);
            //    lightMap.a = aoMap.r;
            //    element.DiffColor = GrassElementData.ConvertColorToFloat(SampleColor(element.Pos), GrassElementData.ColorType.Diff);
            //    element.LightMapColor = GrassElementData.ConvertColorToFloat(lightMap, GrassElementData.ColorType.LightMap);
            //    element.NormalMapColor = GrassElementData.ConvertColorToFloat(SampleNormal(element.Pos), GrassElementData.ColorType.Normal);
            //    //Debug.Log(element.LightMapColor);
            //    elements[i] = element;
            //}
        }
    }
}