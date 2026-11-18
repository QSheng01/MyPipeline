using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Grass
{
    public class GrassAssets:MonoBehaviour
    {
        public ComputeShader computeShader;
        [SerializeField]
        public int GrassSize2;
        [SerializeField]
        public int RootSize2;
        [SerializeField]
        public Vector3 Center;
        [SerializeField]
        public List<GameObject> mGrassObjs;
        [SerializeField]
        public string DataPath;

        GrassInstanceData[] _grassLodDatas;
        float[] _grassHeights;
        //bool _isInit;
        int _grassTypeNum;
        public int GrassTypeNum => _grassTypeNum;
        public float[] GrassHeights => _grassHeights;

        public Camera RenderCamera { set; get; }

        // 公开参数
        public float3 m_GrassLod = math.float3(20, 40, 80);
        public int m_GrassSize2 = 2;
        public int m_RenderExtent2 = 6;

        private void Start()
        {
            // 初始化单场景的渲染参数
            GrassRenderer.Instance.SetLodAndExtends(m_GrassLod, m_RenderExtent2, m_GrassSize2);
        }

        [ContextMenu("SetLodAndExtends")]
        public void SetLodAndExtends()
        {
            // 初始化单场景的渲染参数
            GrassRenderer.Instance.SetLodAndExtends(m_GrassLod, m_RenderExtent2, m_GrassSize2);
        }

#if UNITY_EDITOR

        public Transform FollowTarget;
        public bool DebugBounds;

        private void OnEnable()
        {
#if UNITY_EDITOR

            RenderCamera = Camera.main;

            // 配置地编端玩家位置
            GrassRenderer.Instance.SetFollowTarget(FollowTarget);

            // 读取并加载草的资源数据
            var grassData = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(string.Format("Assets/{0}", DataPath));
            if (grassData != null)
            {
                GrassRenderer.Instance.SetComputeShader(computeShader, true);
                GrassRenderer.Instance.SetHiZEnabled(true);
                GrassRenderer.Instance.LoadSceneGrassAssetsAsync(this, grassData.bytes);
                Resources.UnloadAsset(grassData);
            }

#endif
        }
        private void OnDisable()
        {
            GrassRenderer.Instance.UnLoadScenGrassAssets();
            GrassRenderer.Instance.OnDestory();
        }

        private void OnDrawGizmos()
        {
            if (DebugBounds)
                GrassRenderer.Instance.DrawGrassBounds();
        }

#endif
        int lodErrorTimeIndex = 0;
        private void Update()
        {
            if (RenderCamera)
                GrassRenderer.Instance.Update();

            // 资源丢失检测
            lodErrorTimeIndex++;
            if (lodErrorTimeIndex % 200 == 0)
            {
                for (int i = 0; i < mGrassObjs.Count; i++)
                {
                    if (mGrassObjs[i] == null)
                    {
                        Debug.LogError("引用丢失: Setting/GrassAssets中的 MGrassObjs 草预制体引用丢失！");
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if (RenderCamera)
                GrassRenderer.Instance.LateUpdate(RenderCamera);
        }

        public void Init()
        {
            //if (_isInit)
            //    return;

            int length = mGrassObjs.Count;
            _grassLodDatas = new GrassInstanceData[length];
            _grassHeights = new float[length];
            _grassTypeNum = length;
            for (int i = 0; i < length; i++)
            {
                if (mGrassObjs[i] != null)
                {
                    _grassLodDatas[i] = mGrassObjs[i].GetComponent<GrassInstanceData>();
                    _grassHeights[i] = _grassLodDatas[i].GetMeshHeight();
                }
            }
            //_isInit = true;
        }

        // 设置 LOD 和基础范围
        public void SetLodAndExtends(float3 grassLod, int renderExtent2)
        {
#if UNITY_EDITOR
#if MAP_EDITOR
            m_GrassLod = grassLod;
            m_RenderExtent2 = renderExtent2;
#endif
#endif
        }

        private void OnDestroy()
        {
            _grassLodDatas = null;
            _grassHeights = null;
        }

        public byte FindGrassType(GameObject gameObject)
        {
            byte type = 0;
            if (mGrassObjs != null)
            {
                foreach (var grass in mGrassObjs)
                {
                    if (grass == gameObject)
                        return type;
                    type++;
                }
            }

            return 255;
        }

        public byte AddGrassObj(GameObject gameObject)
        {
            var lodData = gameObject.GetComponent<GrassInstanceData>();
            if (lodData == null)
            {
                Debug.LogError("GrassAssets：：AddGrassObj failed  name = " + gameObject.name);
                return 255;
            }
            lodData.Init();
            if (mGrassObjs == null)
            {
                mGrassObjs = new List<GameObject>();
            }

            mGrassObjs.Add(gameObject);
            return (byte)(mGrassObjs.Count - 1);
        }

        public void ClearGrassObjs()
        {
            if (mGrassObjs != null)
            {
                // foreach (var VARIABLE in mGrassObjs)
                // {
                //     DestroyImmediate(VARIABLE);
                // }
                mGrassObjs.Clear();
            }
        }

        public float GetMeshHeight(byte type)
        {
            if (_grassHeights == null || _grassHeights.Length < type + 1 || _grassHeights[type] == 0)
            {
                Debug.LogError(string.Format("GrassAssets::GetMeshHeight不存在  type = {0} 的植被", type));
                return 0;
            }

            return _grassHeights[type];
        }

        public Mesh GetGrassMesh(int type, int lod)
        {
            if (_grassLodDatas.Length <= type || _grassLodDatas[type] == null)
            {
                Debug.LogError(string.Format("GrassAssets::GetGrassMesh  type = {0} 的植被", type));
                return null;
            }

            return _grassLodDatas[type].GetLodMesh(lod);
        }

        public Material GetGrassMat(int type)
        {
            if (_grassLodDatas.Length <= type || _grassLodDatas[type] == null)
            {
                Debug.LogError(string.Format("GrassAssets::GetGrassMesh  type = {0} 的植被", type));
                return null;
            }

            return _grassLodDatas[type].InstanceMat;
        }
        public Material GetGrassIndirectMat(int type)
        {
            if (_grassLodDatas.Length <= type || _grassLodDatas[type] == null)
            {
                Debug.LogError(string.Format("GrassAssets::GetGrassMesh  type = {0} 的植被", type));
                return null;
            }

            return _grassLodDatas[type].GetInidrectMat();
        }

        public uint GetIndexLength(int type, int lod)
        {
            var mesh = GetGrassMesh(type, lod);
            if (mesh)
                return mesh.GetIndexCount(0);
            return 0;
        }

        public uint GetIndexStart(int type, int lod)
        {
            var mesh = GetGrassMesh(type, lod);
            if (mesh)
                return mesh.GetIndexStart(0);
            return 0;
        }
    }
}
