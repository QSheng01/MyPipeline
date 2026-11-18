
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SYArtToolkit
{
    [ExecuteInEditMode]
    public class PartitionPlatVertex : MonoBehaviour
    {
        [Header("地块的偏移和放缩")]
        public Vector4 tilling = new Vector4(0, 0, 100, 100);

        [Header("地块顶点阈值")]
        public int indexThresholdValue = 100000;
        //private ComputeShader computeShader;

        [Header("开启地块标红显示")]
        public bool openGizmos = true;

        // Start is called before the first frame update
        void OnEnable()
        {

            //computeShader = null;
            //ComputerResult();

        }

        /// 思路
        /// <summary>
        ///  这个工具的做法如下：
        ///  首先获取到场景所有的MeshFilter
        ///  然后得到所有的Mesh
        ///  根据mesh的包围盒是否和相机相交进行剔除——不是和相机求交，
        ///  传入MVP矩阵和顶点数据
        ///  把数据传入到计算着色器--传入什么数据？ 
        ///  在计算着色器中计算  顶点的世界坐标位置  需要MVP矩阵  如何处理块?????????
        ///  如何剔除，选择哪一部分进行传入计算着色器
        ///  我需要从计算着色器中获取什么？？？？？？？？？？？？？？？？？？？？？？？？
        ///   1.每一个块的顶点数的和包围盒数量
        /// </summary>

        // Update is called once per frame
        void Update()
        {
            if (openGizmos)
            {
                ComputerResult();

            }
        }

        private void OnDrawGizmos()
        {
            if (openGizmos)
            {
                Gizmos.color = new Color(1, 0, 0, 0.32f);
                foreach (var b in drawBounds)
                {
                    Gizmos.DrawCube(b.Item1.center, b.Item1.extents);
                    Handles.Label(b.Item1.center, b.Item2.ToString() + "/" + indexThresholdValue.ToString());
                }
            }
        }


        private void ComputerResult()
        {
            var mfs = FindObjectsOfType<MeshFilter>();
            Dictionary<(int, int), GridAnalysis> dictionary = new Dictionary<(int i, int j), GridAnalysis>();
            foreach (var mf in mfs)
            {
                //网格不可读怎么办
                if (mf.sharedMesh == null) continue;
                //var e=mf.sharedMesh.vertices;
                //Debug.Log(e.Length);
                //var mvp = mf.transform.localToWorldMatrix;

                var v3 = mf.transform.position;
                float ym = v3.y;
                int IIndex = Mathf.FloorToInt((v3.x + tilling.x) / tilling.z);
                int JIndex = Mathf.FloorToInt((v3.z + tilling.y) / tilling.w);
                if (!dictionary.ContainsKey((IIndex, JIndex)))
                {
                    dictionary.Add((IIndex, JIndex), new GridAnalysis(IIndex, JIndex));
                }

                var grid = dictionary[(IIndex, JIndex)];
                grid.meshes.Add(mf.sharedMesh);
                grid.yMax = Mathf.Max(grid.yMax, ym);
                grid.yMin = Mathf.Min(grid.yMin, ym);
                for (int i = 0; i < mf.sharedMesh.subMeshCount; ++i)
                {

                    grid.count += mf.sharedMesh.GetSubMesh(i).vertexCount;
                }
                continue;


                //var bounds = TransformBounds(mf.sharedMesh.bounds, mf.transform);
                //var center = bounds.center;
                //var extent = bounds.extents;
                //float xMin = center.x - extent.x;
                //float yMin = center.y - extent.y;
                //float zMin = center.z - extent.z;
                //float xMax = center.x + extent.x;
                //float yMax = center.y + extent.y;
                //float zMax = center.z + extent.z;


                ////if(bounds.)
                //int iMin = Mathf.FloorToInt((xMin + tilling.x) / tilling.z);
                //int iMax = Mathf.FloorToInt((xMax + tilling.x) / tilling.z);
                //int jMin = Mathf.FloorToInt((zMin + tilling.y) / tilling.w);
                //int jMax = Mathf.FloorToInt((zMax + tilling.y) / tilling.w);
                //for (int i = iMin; i <= iMax; ++i)
                //{
                //    for (int j = jMin; j <= jMax; ++j)
                //    {
                //        if (!dictionary.ContainsKey((i, j)))
                //        {
                //            dictionary.Add((i, j), new GridAnalysis(i, j));
                //        }

                //        var ga = dictionary[(i, j)];

                //        //Debug.Log(yMin+"__"+yMax);
                //        ga.yMin = Mathf.Min(ga.yMin, yMin);
                //        ga.yMax = Mathf.Max(ga.yMax, yMax);
                //        ga.meshes.Add(mf.sharedMesh);
                //        //Debug.Log(ga.yMax+"_"+ga.yMin);
                //    }
                //}

            }
            drawBounds.Clear();
            //Debug.Log(dictionary.Count);
            foreach (var kv in dictionary)
            {
                //Debug.Log(kv.Key.Item1+"___"+kv.Key.Item2);
                //Debug.Log(kv.Value.GetCube(tilling));
                //if (kv.Value.IntersectCamera(SceneView.lastActiveSceneView.camera, tilling))
                //{
                if (kv.Value.count > indexThresholdValue)
                    drawBounds.Add((kv.Value.GetCube(tilling), kv.Value.count));
                //}
            }
            //如果包围盒在区域内 就放在里面

        }

        Bounds TransformBounds(Bounds localBounds, Transform transform)
        {


            Vector3[] localCorners = new Vector3[8];

            // 计算包围盒的八个局部空间顶点
            localCorners[0] = new Vector3(localBounds.min.x, localBounds.min.y, localBounds.min.z);
            localCorners[1] = new Vector3(localBounds.min.x, localBounds.min.y, localBounds.max.z);
            localCorners[2] = new Vector3(localBounds.min.x, localBounds.max.y, localBounds.min.z);
            localCorners[3] = new Vector3(localBounds.min.x, localBounds.max.y, localBounds.max.z);
            localCorners[4] = new Vector3(localBounds.max.x, localBounds.min.y, localBounds.min.z);
            localCorners[5] = new Vector3(localBounds.max.x, localBounds.min.y, localBounds.max.z);
            localCorners[6] = new Vector3(localBounds.max.x, localBounds.max.y, localBounds.min.z);
            localCorners[7] = new Vector3(localBounds.max.x, localBounds.max.y, localBounds.max.z);

            // 将局部空间顶点转换为世界空间
            Vector3[] worldCorners = new Vector3[8];

            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);


            for (int i = 0; i < 8; i++)
            {
                worldCorners[i] = transform.TransformPoint(localCorners[i]);
                min = Vector3.Min(worldCorners[i], min);
                max = Vector3.Max(worldCorners[i], max);
            }

            Bounds bounds = new Bounds(max / 2 + min / 2, max - min);

            return bounds;


        }


        public List<(Bounds, int)> drawBounds = new List<(Bounds, int)>();

        private class GridAnalysis
        {
            public float yMin;
            public float yMax;
            public int count;
            private int i;
            private int j;
            public List<Mesh> meshes;
            //这个工程预计很大，要继续这样做下去，还是简单了事?


            //相机是否和这个相交? 
            public GridAnalysis(int i, int j)
            {
                yMin = float.MaxValue;
                yMax = float.MinValue;
                this.i = i;
                this.j = j;
                meshes = new List<Mesh>();
                count = 0;
            }

            public Bounds GetCube(Vector4 tilling)
            {
                float iMin = i * tilling.z - tilling.x;
                float iMax = i * tilling.z - tilling.x + tilling.z;
                float jMin = j * tilling.w - tilling.y;
                float jMax = j * tilling.w - tilling.y + tilling.w;
                // Debug.Log(-jMin/2+jMax/2);
                return new Bounds(new Vector3(iMin / 2 + iMax / 2, yMin / 2 + yMax / 2, jMin / 2 + jMax / 2), new Vector3(-iMin * 2 + iMax * 2, -yMin * 2 + yMax * 2, -jMin * 2 + jMax * 2));

            }

            public bool IntersectCamera(Camera camera, Vector4 tilling)
            {
                FrustumPlanes frustumPlanes = new FrustumPlanes(camera);

                return IsBoundsVisible(GetCube(tilling), frustumPlanes);
            }

            private bool IsBoundsVisible(Bounds bounds, FrustumPlanes frustumPlanes)
            {
                // 判断六个面是否在视锥体内
                foreach (Plane plane in frustumPlanes.Planes)
                {
                    if (plane.GetDistanceToPoint(bounds.ClosestPoint(plane.normal)) < 0)
                        return false;
                }
                return true;
            }
            private struct FrustumPlanes
            {
                public Plane[] Planes;

                public FrustumPlanes(Camera camera)
                {
                    Planes = GeometryUtility.CalculateFrustumPlanes(camera);
                }
            }
        }

        //Terrain
    }
}
#endif