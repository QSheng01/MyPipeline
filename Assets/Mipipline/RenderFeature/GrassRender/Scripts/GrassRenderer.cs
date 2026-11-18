using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Grass
{
    public class Singleton<T> where T : new()
    {
        private static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new T();
                return _instance;
            }
        }
    }

    public class GrassRenderer : Singleton<GrassRenderer>
    {
        static readonly int NameID_Matrix = Shader.PropertyToID("_IndirectMatrix");
        //static readonly int NameID_SHL2 = Shader.PropertyToID("_IndirectSHL2");
        static readonly int NameID_ArgsBuffer = Shader.PropertyToID("_ArgsBuffer");
        static readonly int NameID_ArgsOffset = Shader.PropertyToID("_ArgsOffset");
        static readonly int MinLength = 1024;
        private static readonly int LodNum = 3;

        static bool ENABLE_DEBUG = false;
        static bool EnableIndirect = false;
        static int INSTANCE_MAX = 511;

        //场景植被数据
        public static List<GrassTool> GrassToolList = new List<GrassTool>();
        GrassAssets _curGrassAssets;
        SceneGrassBlockData[] _blocks;
        NativeArray<GrassBlockBinding> _sceneGrassData;        //整个场景内数据
        NativeArray<uint> _sceneGrassArgsResult;
        uint[] _tempSceneGrassArgs;
        Material[] _sceneGrassMats;
        Material[] _sceneGrassIndirectMats;
        Mesh[][] _sceneGrassMeshs;

        //待渲染植被数据
        int _resultDataLength;
        NativeArray<GrassBlockBinding> _grassCacheData;         //渲染区域内数据
        NativeArray<GrassBlockBinding> _grassDataResult;        //裁剪后渲染数据
        NativeArray<int> _grassNumResult;
        NativeArray<Matrix4x4> _resultMatrixs;

        //Instance渲染方式
        Matrix4x4[] _tempMatrixData;

        //Indirect渲染
        ComputeBuffer _sceneArgsBuffer;
        ComputeBuffer _matrixBuffer;
        MaterialPropertyBlock _sceneIndirectPropertyBlock;
        //场景数据加载
        bool _isLoaded;
        bool _exiteGrass;
        bool _initSuccess;
        private bool _sceneInfoSuccess;
        float _updateDist;
        Matrix4x4 _fvp;
        float3 _grassLod;
        Transform _followTarget;
        float2 _centerPos;
        int _renderExtent2;

        //异步加载
        struct BlockParseResult
        {
            public byte Type;
            public Matrix4x4[] Matrixs;
            public HWorldBounds HWBounds;
        }
        volatile bool _isLoading;
        BlockParseResult[] _parseResults;
        int _parseBlockCount;
        byte[] _cachedDataBytes;
        int _cachedReadOffset;
        int _cachedTypeNum;
        int _cachedRootSize2;
        int _cachedGrassSize2;
        float[] _cachedMeshHeights;
        int _writeBackIndex;
        const int BLOCKS_PER_FRAME = 100;

        //Compute Shader 裁剪
        struct GPUBlockInfo
        {
            public uint type;
            public uint matrixStart;
            public uint matrixCount;
            public Vector3 boundsCenter;
            public Vector3 boundsExtents;
        }
        bool _useComputeShader;
        ComputeShader _cullingCS;
        int _kernelCullBlocks;
        int _kernelBuildArgs;
        int _kernelOutputInstances;
        int _kernelGenerateHiZ;
        ComputeBuffer _gpuAllBlockBuffer;
        ComputeBuffer _gpuAllMatrixBuffer;
        ComputeBuffer _gpuVisibleBlockBuffer;
        ComputeBuffer _gpuCounterBuffer;
        ComputeBuffer _gpuOutputMatrixBuffer;
        ComputeBuffer _gpuIndirectArgsBuffer;
        ComputeBuffer _gpuDispatchArgsBuffer;
        ComputeBuffer _gpuPrefixSumBuffer;      
        MaterialPropertyBlock _gpuPropertyBlock;
        int _gpuBlockCount;
        int _gpuTotalMatrixCount;
        uint[] _gpuZeroCounters;

        //Hi-Z 遮挡剔除 (Hi-Z RT 由独立的 GrassHiZPass 管理)
        [Header("Hi-Z Occlusion Culling")]
        public bool debugDisableHiZ = false;
        [Tooltip("勾选后 Hi-Z 强制剔除所有草块 (用于验证 Hi-Z 管线是否生效)")]
        public bool debugForceOccludeAll = false;
        bool _hiZEnabled;
        GrassHiZPass _hiZPass;

        /// <summary>
        /// 初始化
        /// </summary>
        public void Init()
        {
            // 如果已经初始化过了，就不需要再初始化了
            if(_initSuccess) return;

            _matrixBuffer = new ComputeBuffer(MinLength, 16 * sizeof(float), ComputeBufferType.Default);
            _tempMatrixData = new Matrix4x4[INSTANCE_MAX];
            _grassNumResult = new NativeArray<int>(2, Allocator.Persistent);
            _resultMatrixs = new NativeArray<Matrix4x4>(MinLength, Allocator.Persistent);

#if UNITY_EDITOR
            ENABLE_DEBUG = true;
#endif

            if (ENABLE_DEBUG)
            {
                _debugGrassBounds = new List<HWorldBounds>();
            }
            else
            {
                INSTANCE_MAX = 126;
                var max = SystemInfo.maxTextureSize / (SystemInfo.constantBufferOffsetAlignment * 4);
                if (SystemInfo.constantBufferOffsetAlignment == 16)
                {
                    max--;
                }
                if (max > INSTANCE_MAX)
                {
                    INSTANCE_MAX = max;
                }
            }
            _initSuccess = true;
        }

        /// 设置 LOD 和基础范围
        /// <param name="grassLod"> 植被lod参数, 例如float2(10, 60)表示小于10的时候LOD0,大于10小于60则表示LOD1,大于60则裁剪</param>
        /// <param name="grassSize2"> 植被裁剪精度 2^n</param>
        /// <param name="renderExtent2"> 植被渲染范围 2^n</param>
        /// <param name="followTarget"> 植被渲染范围中心点Transform</param>
        public void SetLodAndExtends(float3 grassLod, int renderExtent2, int grassSize2 = 2)
        {
            // 如果没有初始化就进行初始化。
            if (!_initSuccess)
            {
                Init();
            }

            // 配置场景的基础信息成功
            _sceneInfoSuccess = true;

            // 数据初始化
            _grassLod = grassLod;
            _renderExtent2 = renderExtent2;
            _updateDist = 1 << (renderExtent2 - grassSize2);
            int resultLength = (1 << (2 * (renderExtent2 + 1 - grassSize2))) * 4;
            if (_grassDataResult.IsCreated)
            {
                _grassDataResult.Dispose();
            }
            _grassDataResult = new NativeArray<GrassBlockBinding>(resultLength, Allocator.Persistent);
        }

        public void SetIndirectEnabled(bool enable)
        {
            EnableIndirect = enable && SystemInfo.supportsComputeShaders &&
                             SystemInfo.maxComputeBufferInputsVertex >= 2 &&
                             SystemInfo.maxComputeBufferInputsFragment >= 2;
        }
        public void SetFollowTarget(Transform transform)
        {
            _followTarget = transform;
            // 强刷
            _centerPos = new float2(-99999f,-99999f);
            _fvp = Matrix4x4.zero;
        }

        /// <summary>
        /// 设置 Compute Shader 裁剪模式
        /// 传入 null 则关闭 Compute Shader 路径，回退到 CPU Job 路径
        /// </summary>
        public void SetComputeShader(ComputeShader cullingCS, bool openUse = true)
        {
            _cullingCS = cullingCS;
            _useComputeShader = cullingCS != null && openUse;

            if (_useComputeShader)
            {
                _kernelCullBlocks = _cullingCS.FindKernel("CullBlocks");
                _kernelBuildArgs = _cullingCS.FindKernel("BuildIndirectArgs");
                _kernelOutputInstances = _cullingCS.FindKernel("OutputInstances");
                _kernelGenerateHiZ = _cullingCS.FindKernel("GenerateHiZ");
            }

            if (_isLoaded && _useComputeShader)
                UploadGrassDataToGPU();
            else if (!_useComputeShader)
                ReleaseGPUBuffers();
        }

        /// <summary>
        /// 启用/关闭 Hi-Z 遮挡剔除 (需要先 SetComputeShader)
        /// </summary>
        public void SetHiZEnabled(bool enable)
        {
            _hiZEnabled = enable;
        }

        /// <summary>
        /// 由 RenderGrass Feature 在 AddRenderPasses 中调用, 绑定独立的 Hi-Z Pass
        /// </summary>
        public void BindHiZPass(GrassHiZPass hiZPass)
        {
            _hiZPass = hiZPass;
            _hiZEnabled = hiZPass != null;
        }

        /// <summary>
        /// 同步加载（原始方式），调用后立即可渲染
        /// </summary>
        public void LoadSceneGrassAssets(GrassAssets grassAssets, byte[] dataBytes)
        {
            LoadSceneGrassAssetsInternal(grassAssets, dataBytes, false);
        }

        /// <summary>
        /// 异步加载，不阻塞主线程，草会在后台解析完成后才开始渲染
        /// </summary>
        public void LoadSceneGrassAssetsAsync(GrassAssets grassAssets, byte[] dataBytes)
        {
            LoadSceneGrassAssetsInternal(grassAssets, dataBytes, true);
        }

        private void LoadSceneGrassAssetsInternal(GrassAssets grassAssets, byte[] dataBytes, bool async)
        {
            // 如果没有初始化就进行初始化。
            if (!_initSuccess)
            {
                Init();
            }

            // 没有数据，或者已经加载了
            if (grassAssets == null || dataBytes == null || _isLoaded || _isLoading)
            {
                Debug.LogError(string.Format("grassAssets is Null = {0} dataBytes is Null = {1} _isLoaded = {2} _isLoading = {3}", grassAssets == null, dataBytes == null, _isLoaded, _isLoading));
                return;
            }
            _curGrassAssets = grassAssets;

            // 初始化预制体信息
            _curGrassAssets.Init();
            int assetsTypeNum = _curGrassAssets.GrassTypeNum;

            // 读取块长
            int readOffset = 0;
            int blockLength = StreamReader.ReadInt32(dataBytes, ref readOffset);
            if (blockLength == 0) {
                _exiteGrass = false;
                return;
            }

            // 读取草的 type 数量
            var dataTypeNum = StreamReader.ReadByte(dataBytes, ref readOffset);
            if (dataTypeNum != assetsTypeNum)
            {
                Debug.LogError(string.Format("bytes 数据与资源不匹配 dataTypeNum = {0} assetsTypeNum = {1}", dataTypeNum, assetsTypeNum));
                return;
            }

            _exiteGrass = dataTypeNum > 0;
            if (_exiteGrass)
            {
                // 给以下数据，初始化内存大小
                _sceneGrassArgsResult = new NativeArray<uint>(5 * assetsTypeNum * LodNum, Allocator.Persistent);
                _tempSceneGrassArgs = new uint[5 * assetsTypeNum * LodNum];
                _sceneGrassMeshs = new Mesh[assetsTypeNum][];
                _sceneGrassMats = new Material[assetsTypeNum];
                _sceneGrassIndirectMats = new Material[assetsTypeNum];
                _sceneArgsBuffer = new ComputeBuffer(assetsTypeNum * LodNum, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);

                // Instance 的 PropertyBlock
                _sceneIndirectPropertyBlock = new MaterialPropertyBlock();
                _sceneIndirectPropertyBlock.SetBuffer(NameID_ArgsBuffer, _sceneArgsBuffer);
                _sceneIndirectPropertyBlock.SetBuffer(NameID_Matrix, _matrixBuffer);

                // 配置每种 type 的草的信息
                for (int i = 0; i < assetsTypeNum; i++)
                {
                    _sceneGrassMeshs[i] = new Mesh[LodNum];
                    _sceneGrassMats[i] = _curGrassAssets.GetGrassMat(i);
                    _sceneGrassIndirectMats[i] = _curGrassAssets.GetGrassIndirectMat(i);

                    for (int j = 0; j < LodNum; j++)
                    {
                        _sceneGrassArgsResult[(i * LodNum + j) * 5] = _curGrassAssets.GetIndexLength(i, j);
                        _sceneGrassArgsResult[(i * LodNum + j) * 5 + 2] = _curGrassAssets.GetIndexStart(i, j);
                        _sceneGrassMeshs[i][j] = _curGrassAssets.GetGrassMesh(i, j);
                    }
                }

                // 初始化 block 信息
                _blocks = new SceneGrassBlockData[blockLength];
                _sceneGrassData = new NativeArray<GrassBlockBinding>(blockLength, Allocator.Persistent);
                _grassCacheData = new NativeArray<GrassBlockBinding>(blockLength, Allocator.Persistent);

                if (async)
                {
                    // ====== 异步路径: 启动子线程解析 ======
                    _cachedMeshHeights = new float[assetsTypeNum];
                    for (int i = 0; i < assetsTypeNum; i++)
                    {
                        _cachedMeshHeights[i] = _curGrassAssets.GetMeshHeight((byte)i);
                    }

                    _parseResults = new BlockParseResult[blockLength];
                    _parseBlockCount = 0;
                    _writeBackIndex = 0;
                    _cachedDataBytes = dataBytes;
                    _cachedReadOffset = readOffset;
                    _cachedTypeNum = dataTypeNum;
                    _cachedRootSize2 = _curGrassAssets.RootSize2;
                    _cachedGrassSize2 = _curGrassAssets.GrassSize2;
                    _isLoading = true;

                    Task.Run(() => ParseGrassDataAsync());
                }
                else
                {
                    // ====== 同步路径: 原始方式，直接在主线程解析 ======
                    ReadSceneGrassData(dataBytes, readOffset, dataTypeNum, _curGrassAssets.RootSize2, _curGrassAssets.GrassSize2);
                    _isLoaded = true;

                    if (_useComputeShader && _cullingCS != null)
                        UploadGrassDataToGPU();
                }
            }
            else if (dataTypeNum > 0 && assetsTypeNum == 0)
            {
                Debug.LogError("GrassRenderer::LoadSceneGrassAssets:: 场景植被数据异常，植被预制资源为空！！！");
            }
        }

        public void UnLoadScenGrassAssets()
        {
            if (_isLoaded || _isLoading)
            {
                ReleaseSceneGrassAssets();
            }
        }

        public void OnDestory()
        {
            ReleaseGPUBuffers();

            _tempMatrixData = null;
            //_tempOcclusionprobeData = null;
            //_tempLightprobeData = null;
            _tempSceneGrassArgs = null;
            //_propertyBlock = null;
            _sceneGrassIndirectMats = null;

            //_resultSHL2.Dispose();
            if (_grassCacheData != null && _grassCacheData.IsCreated)
            {
                _grassCacheData.Dispose();
            }
            _grassDataResult.Dispose();
            _grassNumResult.Dispose();
            _resultMatrixs.Dispose();
            _resultDataLength = 0;
            //_resultOcclusionprobes.Dispose();
            //_resultLightprobes.Dispose();

            if (_matrixBuffer != null)
                _matrixBuffer.Release();
            //if (_shl2Buffer != null)
            //    _shl2Buffer.Release();
            _matrixBuffer = null;
            //_shl2Buffer = null;

            _initSuccess = false;
        }
        internal void ReleaseSceneGrassAssets()
        {
            ReleaseGPUBuffers();

            _isLoaded = false;
            _isLoading = false;
            _exiteGrass = false;
            _curGrassAssets = null;
            _sceneGrassMats = null;
            _parseResults = null;
            _cachedDataBytes = null;
            _cachedMeshHeights = null;
            _writeBackIndex = 0;
            _sceneGrassMeshs = null;
            _sceneGrassIndirectMats = null;
            _blocks = null;
            _grassNumResult[0] = 0;
            //if (_loadTask != null)
            //{
            //    _loadTask.Dispose();
            //    _loadTask = null;
            //}
            if (_sceneGrassData.IsCreated)
                _sceneGrassData.Dispose();
            if (_grassCacheData.IsCreated)
                _grassCacheData.Dispose();

            if (_sceneGrassArgsResult.IsCreated)
                _sceneGrassArgsResult.Dispose();
            if (_sceneArgsBuffer != null)
                _sceneArgsBuffer.Release();
        }

        public void RenderGrass(CameraData cameraData, CommandBuffer cmd, int pass, bool depthWrite)
        {

            //#if UNITY_EDITOR
            //            RenderGrassEditor(cameraData, cmd);
            //#else
            //RenderSceneGrass(cmd, pass);
            //#endif

            // 配置渐隐参数, Vector 属于值类型，每帧 new 不造成内存泄漏
            cmd.SetGlobalVector("_LODDistance", new Vector4(_grassLod.x, _grassLod.y, _grassLod.z));

            if (Application.isPlaying)
            {
                RenderSceneGrass(cmd, pass, depthWrite);
            }
            else
            {
                RenderGrassEditor(cameraData, cmd, pass, depthWrite);
            }
        }

        private void RenderGrassEditor(CameraData cameraData, CommandBuffer cmd, int pass, bool depthWrite)
        {
            foreach (var tool in GrassToolList)
            {
                tool.EditorRenderGrass(cameraData, cmd, pass, depthWrite);
            }
        }

        public void Update()
        {
            if (_useComputeShader)
                return;

            if (_initSuccess && _sceneInfoSuccess && _exiteGrass && _isLoaded && _curGrassAssets != null && _followTarget!= null)
            {
                var pos = _followTarget.position;
                float2 newPos = math.float2(pos.x, pos.z);
                float2 newCameraPos = math.float2(nowCameraPos.x, nowCameraPos.z);
                // 如果玩家移动距离大于刷新距离,或者相机移动距离大于刷新距离
                if (math.any(math.abs( _centerPos - newPos) > _updateDist) || math.any(math.abs( _centerPos - newCameraPos) > _updateDist))
                {
                    // 刷新 target 坐标
                    _centerPos = newPos;
                    // 更新预裁剪数据 step1
                    var loadGrassDataJobHandler = new LoadGrassDataJob()
                    {
                        CenterPos = _centerPos,
                        RenderExtent2 = _renderExtent2,
                        SceneGrassData = _sceneGrassData

                    }.Schedule(_sceneGrassData.Length, 32);
                    // 更新预裁剪数据 step2
                    var collectJobHandler = new CollectGrassDataJob()
                    {
                        SceneGrassData = _sceneGrassData,
                        GrassCacheData = _grassCacheData,
                        LengthResult = _grassNumResult

                    }.Schedule(loadGrassDataJobHandler);

                    collectJobHandler.Complete();
                }
            }
        }

        private Vector3 nowCameraPos;
        public void LateUpdate(Camera renderCamera)
        {
            if (_initSuccess && _sceneInfoSuccess  && renderCamera != null && _curGrassAssets != null)
            {
                nowCameraPos = renderCamera.transform.position;

                // 世界空间 => View空间
                Matrix4x4 v = renderCamera.worldToCameraMatrix;
                // 透视投影矩阵 P
                Matrix4x4 fp = Matrix4x4.Perspective(renderCamera.fieldOfView, renderCamera.aspect, renderCamera.nearClipPlane, renderCamera.farClipPlane);
                // fvp矩阵，可以让点从世界空间 => 裁剪空间
                Matrix4x4 fvp = fp * v;

                // 先处理异步加载回写（无论相机是否变动都需要推进）
                if (!CheckGrassLoad())
                    return;

                if (_useComputeShader && _gpuAllBlockBuffer != null)
                {
                    // ====== Compute Shader 路径 ======
                    Vector3 playerPos = _followTarget != null ? _followTarget.position : nowCameraPos;
                    float2 newPlayerPos = math.float2(playerPos.x, playerPos.z);
                    bool cameraChanged = !_fvp.Equals(fvp);
                    bool playerMoved = math.any(math.abs(_centerPos - newPlayerPos) > _updateDist);

                    //if (!cameraChanged && !playerMoved)
                    //    return;

                    _fvp = fvp;
                    _centerPos = newPlayerPos;
                    ExecuteComputeCulling(renderCamera, nowCameraPos, playerPos, fvp);
                }
                else
                {
                    // ====== CPU Job 路径 (Fallback) ======
                    if (_fvp.Equals(fvp))
                        return;
                    _fvp = fvp;
                    ExecuteSceneGrassDataPipeline(nowCameraPos, fvp);
                }
            }
        }

        private void ExecuteSceneGrassDataPipeline(float3 cameraPos, float4x4 fvp/*, bool updateLightProbes*/)
        {
            // 执行正式裁剪主函数
            if (ExecuteGrassDataPipeline(cameraPos, fvp))
            {
                int index = 0;
                int offset = 0;

                // 根据 Lod 视锥裁剪以及排序之后的 _grassDataResult 数据，来对 blocks 数据进行筛选。
                foreach (var binding in _grassDataResult)
                {
                    if (index >= _grassNumResult[0])
                        break;
                    var block = _blocks[binding.BlockID];
                    //if (updateLightProbes)
                    //{
                    //    block.UpdateLightProbes();
                    //    _blocks[binding.MeshID] = block;
                    //}
                    // 根据 _grassDataResult 中的 binding 信息，把 blocks 信息筛选到 _resultMatrixs 中，最后用来渲染。
                    block.GetGrassBlockData(ref _resultMatrixs, /*ref _resultOcclusionprobes, ref _resultLightprobes, ref _resultSHL2,*/ ref offset);

                    if (ENABLE_DEBUG)
                        AddDebugGrassBounds(binding.Bounds);

                    index++;
                }

                if (EnableIndirect)
                {
                    //_shl2Buffer.SetData(_resultSHL2);
                    _matrixBuffer.SetData(_resultMatrixs);
                    _sceneArgsBuffer.SetData(_sceneGrassArgsResult);
                }
            }
        }

        private bool CheckGrassLoad()
        {
            if (_isLoaded)
                return true;

            if (_isLoading || _parseResults == null)
                return false;

            // 阶段3: 子线程完成，主线程分帧回写 NativeArray 和 _blocks
            int end = Mathf.Min(_writeBackIndex + BLOCKS_PER_FRAME, _parseBlockCount);
            for (int i = _writeBackIndex; i < end; i++)
            {
                var result = _parseResults[i];

                SceneGrassBlockData block = new SceneGrassBlockData();
                block.InitData(result.Matrixs);
                _blocks[i] = block;

                GrassBlockBinding binding = new GrassBlockBinding();
                binding.InitData(result.Type, i, 0, result.Matrixs.Length, result.HWBounds);
                _sceneGrassData[i] = binding;
            }
            _writeBackIndex = end;

            // 还没写完，下一帧继续
            if (_writeBackIndex < _parseBlockCount)
                return false;

            // 全部写完
            _parseResults = null;
            _cachedDataBytes = null;
            _cachedMeshHeights = null;
            _isLoaded = true;

            if (_useComputeShader && _cullingCS != null)
                UploadGrassDataToGPU();

            // 强制下一帧 Update 重新执行距离预裁剪填充 _grassCacheData
            _centerPos = new float2(-99999f, -99999f);
            // 强制下一帧 LateUpdate 重新执行视锥裁剪
            _fvp = Matrix4x4.zero;
            // 本帧返回 false，让 LateUpdate 跳过本帧的渲染管线
            // 等下一帧 Update 填充完 _grassCacheData 后，LateUpdate 再执行
            return false;
        }

        private bool ExecuteGrassDataPipeline( float3 cameraPos, float4x4 fvp)
        {
            // 正式裁剪主函数
            var frustumCullingGrassJob = new FrustumCullingGrassJob()
            {
                FVP = fvp,
                CameraPos = cameraPos,
                GrassLod = _grassLod,
                MaxLodNum = LodNum,
                BindingDatas = _grassCacheData,
            };

            var cullingHandler = frustumCullingGrassJob.Schedule(_grassCacheData.Length, 32);

            // 多线程异步，只有上面的执行完了，下面的才会执行。
            // 根据 Lod 进行排序
            var sortJob = new SortGrassDataJob()
            {
                LodNum = LodNum,
                Num = _grassNumResult,
                Args = _sceneGrassArgsResult,
                GrassDatas = _grassCacheData,
                // 这里的 _grassDataResult 是 Lod 和视锥裁剪之后最终的处理结果。
                GrassResult = _grassDataResult
            };
            var handler = sortJob.Schedule(cullingHandler);
            handler.Complete();


            if (ENABLE_DEBUG)
            {
                ClearDebugGrassBounds();
            }

            if (_grassNumResult[1] > 0)
            {
                CheckResultDataLenght();
                return true;
            }

            return false;
        }
        private void RenderSceneGrass(CommandBuffer cmd, int pass, bool depthWrite)
        {
//#if !MAP_EDITOR
//            if (_initSuccess && !_isLoaded && _exiteGrass) {
//                Debug.LogError("刷草工具: 没有执行草的加载流程中的LoadSceneGrassAssets()阶段，可能需要检查相关代码。");
//            }
//#endif
            if (!_exiteGrass || !_isLoaded)
                return;

            if (_useComputeShader && _gpuOutputMatrixBuffer != null)
            {
                RenderGrassCompute(_curGrassAssets.GrassTypeNum, pass, cmd);
            }
            else if (!_grassNumResult.IsCreated || _grassNumResult[1] == 0)
            {
                return;
            }
            else if (EnableIndirect)
            {
                RenderGrassIndircet(_curGrassAssets.GrassTypeNum, pass, cmd, _sceneArgsBuffer, _sceneGrassArgsResult, _sceneGrassMeshs, _sceneGrassIndirectMats, _sceneIndirectPropertyBlock);
            }
            else
            {
                NativeArray<uint>.Copy(_sceneGrassArgsResult, _tempSceneGrassArgs);
                RenderGrass(_curGrassAssets.GrassTypeNum, pass, cmd, _tempSceneGrassArgs, _sceneGrassMeshs, _sceneGrassMats, depthWrite);
            }
        }
        internal void Register(GrassTool grassTool)
        {
            GrassToolList.Add(grassTool);
        }
        internal void UnRegister(GrassTool grassTool)
        {
            GrassToolList.Remove(grassTool);
        }
        private void RenderGrass(int typeNum, int pass, CommandBuffer cmd, uint[] args, Mesh[][] meshs, Material[] mats, bool depthWrite)
        {
            Profiler.BeginSample("ModifyMaterial");
            float zTest = (float)(depthWrite ? CompareFunction.Less : CompareFunction.Equal);
            float zWrite = depthWrite ? 1f : 0f;
            for (int j = 0; j < typeNum; j++)
            {
                if (mats[j])
                {
                    mats[j].SetFloat("_ZTest", zTest);
                    mats[j].SetFloat("_ZWrite", zWrite);
                }
            }
            Profiler.EndSample();

            int lodNum = LodNum;

            for (int i = 0; i < lodNum; i++)
            {
                // 配置渐隐参数
                cmd.SetGlobalFloat("_NowLOD", i);

                uint length = 0;
                for (int j = 0; j < typeNum; j++)
                {
                    // args 里面，根据 lod 以及 prefab 的 type 存放了这批次草的数据。
                    length += args[(j * lodNum + i) * 5 + 1];
                }
                while (length > 0)
                {
                    // 每次 Draw 一批草，就减去 length 的长度。
                    for (int j = 0; j < typeNum; j++)
                    {
                        if (meshs[j][i] == null || mats[j] == null)
                        {
                            // Debug.LogError(string.Format("RenderGrass Error !!! grassType = {0} meshIsNull:{1} matIsNull:{2}", j, meshs[j][i] == null, mats[j] == null));
                            return;
                        }

                        //_propertyBlock.Clear();
                        // 如果超过一个 GPUInstance 最大批次，就刷一个 INSTANCE_MAX，剩余的留给下个批次。
                        if (args[(j * lodNum + i) * 5 + 1] >= INSTANCE_MAX)
                        {
                            Profiler.BeginSample("CopyMatrix");
                            NativeArray<Matrix4x4>.Copy(_resultMatrixs, (int)args[(j * lodNum + i) * 5 + 4], _tempMatrixData, 0, INSTANCE_MAX);
                            Profiler.EndSample();
                            //NativeArray<Vector4>.Copy(_resultOcclusionprobes, (int)args[(j * lodNum + i) * 5 + 4], _tempOcclusionprobeData, 0, _instanceMax);
                            //NativeArray<SphericalHarmonicsL2>.Copy(_resultLightprobes, (int)args[(j * lodNum + i) * 5 + 4], _tempLightprobeData, 0, _instanceMax);
                            Profiler.BeginSample("Calc");
                            args[(j * lodNum + i) * 5 + 4] += (uint)INSTANCE_MAX;
                            args[(j * lodNum + i) * 5 + 1] -= (uint)INSTANCE_MAX;
                            length -= (uint)INSTANCE_MAX;
                            Profiler.EndSample();

                            Profiler.BeginSample("DrawMeshInstanced");
                            //_propertyBlock.CopyProbeOcclusionArrayFrom(_tempOcclusionprobeData);
                            //_propertyBlock.CopySHCoefficientArraysFrom(_tempLightprobeData);
                            cmd.DrawMeshInstanced(meshs[j][i], 0, mats[j], pass, _tempMatrixData, INSTANCE_MAX/*, _propertyBlock*/);
                            Profiler.EndSample();
                        }
                        else if (args[(j * lodNum + i) * 5 + 1] > 0)
                        {
                            Profiler.BeginSample("CopyMatrix");
                            NativeArray<Matrix4x4>.Copy(_resultMatrixs, (int)args[(j * lodNum + i) * 5 + 4], _tempMatrixData, 0, (int)args[(j * lodNum + i) * 5 + 1]);
                            Profiler.EndSample();
                            //NativeArray<Vector4>.Copy(_resultOcclusionprobes, (int)args[(j * lodNum + i) * 5 + 4], _tempOcclusionprobeData, 0, (int)args[(j * lodNum + i) * 5 + 1]);
                            //NativeArray<SphericalHarmonicsL2>.Copy(_resultLightprobes, (int)args[(j * lodNum + i) * 5 + 4], _tempLightprobeData, 0, (int)args[(j * lodNum + i) * 5 + 1]);
                            //_propertyBlock.CopyProbeOcclusionArrayFrom(_tempOcclusionprobeData);
                            //_propertyBlock.CopySHCoefficientArraysFrom(_tempLightprobeData);
                            // 配置渐隐过渡
                            // mats[j].SetVector("_LODDistance", new Vector4(_grassLod.x, _grassLod.y, _grassLod.z));

                            Profiler.BeginSample("DrawMeshInstanced");
                            cmd.DrawMeshInstanced(meshs[j][i], 0, mats[j], pass, _tempMatrixData, (int)args[(j * lodNum + i) * 5 + 1]/*, _propertyBlock*/);
                            Profiler.EndSample();
                            Profiler.BeginSample("Calc");
                            length -= args[(j * lodNum + i) * 5 + 1];
                            args[(j * lodNum + i) * 5 + 1] = 0;
                            Profiler.EndSample();
                        }
                    }
                }
            }
        }
        private void RenderGrassIndircet(int typeNum, int pass, CommandBuffer cmd, ComputeBuffer argsBuffer, NativeArray<uint> args, Mesh[][] meshs, Material[] mats, MaterialPropertyBlock propertyBlock = null)
        {
            int argsOffset;

            for (int i = 0; i < LodNum; i++)
            {
                // 配置渐隐参数
                cmd.SetGlobalFloat("_NowLOD", i);

                // 渲染草
                for (int j = 0; j < typeNum; j++)
                {

                    if (args[(j * LodNum + i) * 5 + 1] == 0)
                        continue;

                    //Debug.Log(string.Format("lod = {0} type = {1} num = {2}", i, j, args[(j * LodNum + i) * 5 + 1]));
                    if (meshs[j][i] == null || mats[j] == null)
                    {
                        Debug.LogError(string.Format("RenderGrass Error !!! grassType = {0} meshIsNull:{1} matIsNull:{2}", j, meshs[j][i] == null, mats[j] == null));
                        continue;
                    }
                    argsOffset = (j * LodNum + i) * 5;
                    propertyBlock.SetInt(NameID_ArgsOffset, argsOffset + 4);
                    cmd.DrawMeshInstancedIndirect(meshs[j][i], 0, mats[j], pass, argsBuffer, argsOffset * sizeof(uint), propertyBlock);
                }
            }
        }
        /// <summary>
        /// 同步方式：主线程直接解析二进制草数据（原始方式）
        /// </summary>
        private void ReadSceneGrassData(byte[] grassBytes, int readOffset, int typeNum, int rootSize2, int grassSize2)
        {
            int grid = 1 << (rootSize2 - grassSize2);
            int length = grid * grid;
            int markLenght = length / 8;
            int[] grassNum = new int[length];
            byte[] marks = new byte[markLenght];
            var startPos = _curGrassAssets.Center - new Vector3(1, 0, 1) * (grid * (1 << grassSize2) / 2.0f);
            int count = 0;
            int index = 0;
            while (count < typeNum)
            {
                byte type = StreamReader.ReadByte(grassBytes, ref readOffset);
                for (int i = 0; i < markLenght; i++)
                {
                    marks[i] = StreamReader.ReadByte(grassBytes, ref readOffset);
                }

                for (int i = 0; i < length; i++)
                {
                    var mark = marks[i / 8];
                    int layer = 7 - i % 8;
                    if ((mark & (byte)(1 << layer)) > 0)
                    {
                        SceneGrassBlockData block = new SceneGrassBlockData();
                        Bounds bounds = new Bounds();
                        var grid_x = i % grid;
                        var grid_z = i / grid;
                        var min = startPos + new Vector3(grid_x, 0, grid_z) * (1 << grassSize2);
                        var max = startPos + new Vector3(grid_x + 1, 0, grid_z + 1) * (1 << grassSize2);
                        bounds.SetMinMax(min, max);
                        List<Matrix4x4> matrixs = new List<Matrix4x4>();
                        var num = StreamReader.ReadInt32(grassBytes, ref readOffset);
                        grassNum[i] += num;
                        for (int j = 0; j < num; j++)
                        {
                            var px = StreamReader.ReadFloat32(grassBytes, ref readOffset);
                            var py = StreamReader.ReadFloat32(grassBytes, ref readOffset);
                            var pz = StreamReader.ReadFloat32(grassBytes, ref readOffset);
                            var heightScale = StreamReader.ReadFloat32(grassBytes, ref readOffset);
                            var diffColor = StreamReader.ReadFloat32(grassBytes, ref readOffset);
                            var normalColor = StreamReader.ReadFloat32(grassBytes, ref readOffset);
                            var lightmapColor = StreamReader.ReadFloat32(grassBytes, ref readOffset);
                            var rotaX = StreamReader.ReadFloat32(grassBytes, ref readOffset);
                            var rotaY = StreamReader.ReadFloat32(grassBytes, ref readOffset);
                            var rotaZ = StreamReader.ReadFloat32(grassBytes, ref readOffset);
                            var onlyScaleY = StreamReader.ReadByte(grassBytes, ref readOffset);
                            Vector3 pos = new Vector3(px, py, pz);
                            Quaternion q = Quaternion.Euler(rotaX, rotaY, rotaZ);
                            Vector3 scale = Vector3.one;
                            Matrix4x4 m = Matrix4x4.TRS(pos, q, scale);
                            GrassElementData data = new GrassElementData();
                            data.InitData(m, diffColor, normalColor, lightmapColor, heightScale, onlyScaleY, type);
                            matrixs.Add(data.Matrix);
                        }

                        block.InitData(matrixs.ToArray());
                        _blocks[index] = block;
                        HWorldBounds hwBounds = block.GetCullingBounds(bounds, _curGrassAssets.GetMeshHeight(type));
                        GrassBlockBinding binding = new GrassBlockBinding();
                        binding.InitData(type, index, 0, matrixs.Count, hwBounds);
                        _sceneGrassData[index] = binding;
                        index++;
                    }
                }
                count++;
            }
        }

        /// <summary>
        /// 异步方式：子线程中解析二进制草数据，不访问任何 Unity 主线程 API
        /// </summary>
        private void ParseGrassDataAsync()
        {
            try
            {
                int rootSize2 = _cachedRootSize2;
                int grassSize2 = _cachedGrassSize2;
                int grid = 1 << (rootSize2 - grassSize2);
                int length = grid * grid;
                int markLength = length / 8;
                byte[] marks = new byte[markLength];

                Vector3 center = _curGrassAssets.Center;
                var startPos = center - new Vector3(1, 0, 1) * (grid * (1 << grassSize2) / 2.0f);

                int readOffset = _cachedReadOffset;
                byte[] grassBytes = _cachedDataBytes;
                int typeNum = _cachedTypeNum;

                int count = 0;
                int index = 0;

                while (count < typeNum)
                {
                    byte type = StreamReader.ReadByte(grassBytes, ref readOffset);
                    for (int i = 0; i < markLength; i++)
                    {
                        marks[i] = StreamReader.ReadByte(grassBytes, ref readOffset);
                    }

                    for (int i = 0; i < length; i++)
                    {
                        var mark = marks[i / 8];
                        int layer = 7 - i % 8;
                        if ((mark & (byte)(1 << layer)) == 0) continue;

                        var grid_x = i % grid;
                        var grid_z = i / grid;
                        var min = startPos + new Vector3(grid_x, 0, grid_z) * (1 << grassSize2);
                        var max = startPos + new Vector3(grid_x + 1, 0, grid_z + 1) * (1 << grassSize2);
                        Bounds bounds = new Bounds();
                        bounds.SetMinMax(min, max);

                        var num = StreamReader.ReadInt32(grassBytes, ref readOffset);
                        Matrix4x4[] matrixs = new Matrix4x4[num];

                        for (int j = 0; j < num; j++)
                        {
                            var px = StreamReader.ReadFloat32(grassBytes, ref readOffset);
                            var py = StreamReader.ReadFloat32(grassBytes, ref readOffset);
                            var pz = StreamReader.ReadFloat32(grassBytes, ref readOffset);
                            var heightScale = StreamReader.ReadFloat32(grassBytes, ref readOffset);
                            var diffColor = StreamReader.ReadFloat32(grassBytes, ref readOffset);
                            var normalColor = StreamReader.ReadFloat32(grassBytes, ref readOffset);
                            var lightmapColor = StreamReader.ReadFloat32(grassBytes, ref readOffset);
                            var rotaX = StreamReader.ReadFloat32(grassBytes, ref readOffset);
                            var rotaY = StreamReader.ReadFloat32(grassBytes, ref readOffset);
                            var rotaZ = StreamReader.ReadFloat32(grassBytes, ref readOffset);
                            var onlyScaleY = StreamReader.ReadByte(grassBytes, ref readOffset);
                            Vector3 pos = new Vector3(px, py, pz);
                            Quaternion q = Quaternion.Euler(rotaX, rotaY, rotaZ);
                            Matrix4x4 m = Matrix4x4.TRS(pos, q, Vector3.one);
                            GrassElementData data = new GrassElementData();
                            data.InitData(m, diffColor, normalColor, lightmapColor, heightScale, onlyScaleY, type);
                            matrixs[j] = data.Matrix;
                        }

                        float meshHeight = _cachedMeshHeights[type];
                        HWorldBounds hwBounds = CalcCullingBounds(matrixs, bounds, meshHeight);

                        _parseResults[index] = new BlockParseResult
                        {
                            Type = type,
                            Matrixs = matrixs,
                            HWBounds = hwBounds
                        };
                        index++;
                    }
                    count++;
                }
                _parseBlockCount = index;
            }
            catch (Exception e)
            {
                Debug.LogError("ParseGrassDataAsync failed: " + e.Message + "\n" + e.StackTrace);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private static HWorldBounds CalcCullingBounds(Matrix4x4[] datas, Bounds bounds, float height)
        {
            if (datas == null || datas.Length == 0)
                return new HWorldBounds();

            float minY = float.MaxValue;
            float maxY = float.MinValue;
            foreach (var data in datas)
            {
                var pos = data.GetColumn(3);
                if (pos.y < minY) minY = pos.y;
                var h = pos.y + height * data.m11;
                if (h > maxY) maxY = h;
            }
            bounds.SetMinMax(
                new Vector3(bounds.min.x, minY, bounds.min.z),
                new Vector3(bounds.max.x, maxY, bounds.max.z));
            return new HWorldBounds(bounds.center, bounds.extents);
        }
        private void CheckResultDataLenght()
        {
            if (_grassNumResult[1] > MinLength && _grassNumResult[1] > _resultDataLength)
            {
                _resultMatrixs.Dispose();
                //_resultLightprobes.Dispose();
                //_resultOcclusionprobes.Dispose();
                _resultDataLength = _grassNumResult[1];
                _resultMatrixs = new NativeArray<Matrix4x4>(_resultDataLength, Allocator.Persistent);
                //_resultOcclusionprobes = new NativeArray<Vector4>(_resultDataLength, Allocator.Persistent);
                //_resultLightprobes = new NativeArray<SphericalHarmonicsL2>(_resultDataLength, Allocator.Persistent);

                if (EnableIndirect)
                {
                    //_resultSHL2.Dispose();
                    _matrixBuffer.Release();
                    //_shl2Buffer.Release();

                    //_resultSHL2 = new NativeArray<SHL2Data>(_resultDataLength, Allocator.Persistent);
                    _matrixBuffer = new ComputeBuffer(_resultDataLength, 16 * sizeof(float));
                    //_shl2Buffer = new ComputeBuffer(_resultDataLength, 28 * sizeof(float));
                    _sceneIndirectPropertyBlock.SetBuffer(NameID_Matrix, _matrixBuffer);
                    //materialPropertyBlock.SetBuffer(NameID_SHL2, _shl2Buffer);
                }
            }
        }

        // ====== Compute Shader 裁剪方法 ======

        /// <summary>
        /// 将所有草数据一次性上传到 GPU (加载完成后调用)
        /// </summary>
        private void UploadGrassDataToGPU()
        {
            ReleaseGPUBuffers();

            if (_blocks == null || _blocks.Length == 0 || _curGrassAssets == null)
                return;

            int validBlockCount = 0;
            int totalMatrixCount = 0;
            for (int i = 0; i < _blocks.Length; i++)
            {
                if (_blocks[i].Datas != null && _blocks[i].Datas.Length > 0)
                {
                    validBlockCount++;
                    totalMatrixCount += _blocks[i].Datas.Length;
                }
            }

            if (validBlockCount == 0 || totalMatrixCount == 0)
                return;

            // 构建展平的矩阵数组和 Block 信息数组
            var blockInfos = new GPUBlockInfo[validBlockCount];
            var allMatrices = new Matrix4x4[totalMatrixCount];
            int blockIdx = 0;
            int matrixOffset = 0;

            for (int i = 0; i < _blocks.Length; i++)
            {
                var block = _blocks[i];
                if (block.Datas == null || block.Datas.Length == 0)
                    continue;

                var binding = _sceneGrassData[i];
                blockInfos[blockIdx] = new GPUBlockInfo
                {
                    type = binding.Type,
                    matrixStart = (uint)matrixOffset,
                    matrixCount = (uint)block.Datas.Length,
                    boundsCenter = new Vector3(binding.Bounds.Center.x, binding.Bounds.Center.y, binding.Bounds.Center.z),
                    boundsExtents = new Vector3(binding.Bounds.Extents.x, binding.Bounds.Extents.y, binding.Bounds.Extents.z)
                };
                System.Array.Copy(block.Datas, 0, allMatrices, matrixOffset, block.Datas.Length);
                matrixOffset += block.Datas.Length;
                blockIdx++;
            }

            _gpuBlockCount = validBlockCount;
            _gpuTotalMatrixCount = totalMatrixCount;

            // 静态数据缓冲区 (加载后不再变化)
            _gpuAllBlockBuffer = new ComputeBuffer(validBlockCount, 36);
            _gpuAllBlockBuffer.SetData(blockInfos);

            _gpuAllMatrixBuffer = new ComputeBuffer(totalMatrixCount, 64);
            _gpuAllMatrixBuffer.SetData(allMatrices);

            // 每帧临时缓冲区
            int typeNum = _curGrassAssets.GrassTypeNum;
            int counterSize = 1 + typeNum * LodNum;

            _gpuVisibleBlockBuffer = new ComputeBuffer(validBlockCount, 20); // VisibleBlock: 5 * uint = 20 bytes
            _gpuCounterBuffer = new ComputeBuffer(counterSize, sizeof(uint));
            _gpuOutputMatrixBuffer = new ComputeBuffer(totalMatrixCount, 64);
            _gpuIndirectArgsBuffer = new ComputeBuffer(5 * typeNum * LodNum, sizeof(uint), ComputeBufferType.IndirectArguments);
            _gpuDispatchArgsBuffer = new ComputeBuffer(3, sizeof(uint), ComputeBufferType.IndirectArguments);
            _gpuPrefixSumBuffer = new ComputeBuffer(validBlockCount + 1, sizeof(uint));           

            // 预填充 IndirectArgs 中来自 Mesh 的静态字段 (indexCount, startIndex)
            uint[] indirectArgsInit = new uint[5 * typeNum * LodNum];
            for (int i = 0; i < typeNum; i++)
            {
                for (int j = 0; j < LodNum; j++)
                {
                    int idx = (i * LodNum + j) * 5;
                    indirectArgsInit[idx + 0] = _curGrassAssets.GetIndexLength(i, j);
                    indirectArgsInit[idx + 2] = _curGrassAssets.GetIndexStart(i, j);
                }
            }
            _gpuIndirectArgsBuffer.SetData(indirectArgsInit);

            // 零计数器 (每帧清零用)
            _gpuZeroCounters = new uint[counterSize];

            // 渲染用 PropertyBlock
            _gpuPropertyBlock = new MaterialPropertyBlock();
            _gpuPropertyBlock.SetBuffer(NameID_Matrix, _gpuOutputMatrixBuffer);
            _gpuPropertyBlock.SetBuffer(NameID_ArgsBuffer, _gpuIndirectArgsBuffer);

            // 绑定 Compute Shader 缓冲区
            _cullingCS.SetBuffer(_kernelCullBlocks, "_AllBlockBuffer", _gpuAllBlockBuffer);
            _cullingCS.SetBuffer(_kernelCullBlocks, "_VisibleBlocks", _gpuVisibleBlockBuffer);
            _cullingCS.SetBuffer(_kernelCullBlocks, "_Counters", _gpuCounterBuffer);         

            _cullingCS.SetBuffer(_kernelBuildArgs, "_Counters", _gpuCounterBuffer);
            _cullingCS.SetBuffer(_kernelBuildArgs, "_IndirectArgs", _gpuIndirectArgsBuffer);
            _cullingCS.SetBuffer(_kernelBuildArgs, "_DispatchArgs", _gpuDispatchArgsBuffer);
            _cullingCS.SetBuffer(_kernelBuildArgs, "_VisibleBlocks", _gpuVisibleBlockBuffer);
            _cullingCS.SetBuffer(_kernelBuildArgs, "_VisibleBlockPrefixSum", _gpuPrefixSumBuffer);

            _cullingCS.SetBuffer(_kernelOutputInstances, "_VisibleBlocks", _gpuVisibleBlockBuffer);
            _cullingCS.SetBuffer(_kernelOutputInstances, "_AllMatrixBuffer", _gpuAllMatrixBuffer);
            _cullingCS.SetBuffer(_kernelOutputInstances, "_Counters", _gpuCounterBuffer);
            _cullingCS.SetBuffer(_kernelOutputInstances, "_IndirectArgs", _gpuIndirectArgsBuffer);
            _cullingCS.SetBuffer(_kernelOutputInstances, "_OutputMatrix", _gpuOutputMatrixBuffer);
            _cullingCS.SetBuffer(_kernelOutputInstances, "_VisibleBlockPrefixSum", _gpuPrefixSumBuffer);

            _cullingCS.SetInt("_BlockCount", validBlockCount);
            _cullingCS.SetInt("_TypeNum", typeNum);
            _cullingCS.SetInt("_LodNum", LodNum);

            // 强刷，确保下一帧会执行 Compute 裁剪
            _centerPos = new float2(-99999f, -99999f);
            _fvp = Matrix4x4.zero;
        }

        private void ReleaseGPUBuffers()
        {
            if (_gpuAllBlockBuffer != null) { _gpuAllBlockBuffer.Release(); _gpuAllBlockBuffer = null; }
            if (_gpuAllMatrixBuffer != null) { _gpuAllMatrixBuffer.Release(); _gpuAllMatrixBuffer = null; }
            if (_gpuVisibleBlockBuffer != null) { _gpuVisibleBlockBuffer.Release(); _gpuVisibleBlockBuffer = null; }
            if (_gpuCounterBuffer != null) { _gpuCounterBuffer.Release(); _gpuCounterBuffer = null; }
            if (_gpuOutputMatrixBuffer != null) { _gpuOutputMatrixBuffer.Release(); _gpuOutputMatrixBuffer = null; }
            if (_gpuIndirectArgsBuffer != null) { _gpuIndirectArgsBuffer.Release(); _gpuIndirectArgsBuffer = null; }
            if (_gpuDispatchArgsBuffer != null) { _gpuDispatchArgsBuffer.Release(); _gpuDispatchArgsBuffer = null; }
            if (_gpuPrefixSumBuffer != null) { _gpuPrefixSumBuffer.Release(); _gpuPrefixSumBuffer = null; }         
            _gpuPropertyBlock = null;
            _gpuZeroCounters = null;
            _gpuBlockCount = 0;
            _gpuTotalMatrixCount = 0;
        }

        /// <summary>
        /// 每帧 Compute Shader 裁剪管线:
        /// 1. CullBlocks       - 距离 + 视锥裁剪 + LOD 分级
        /// 2. BuildIndirectArgs - 构建 Indirect 参数 (前缀和)
        /// 3. OutputInstances   - 将可见 block 的矩阵拷贝到输出缓冲区
        /// </summary>
        private void ExecuteComputeCulling(Camera renderCamera, Vector3 cameraPos, Vector3 playerPos, Matrix4x4 fvp)
        {
            Profiler.BeginSample("GrassComputeCulling");

            // Hi-Z: 使用上一帧渲染管线内生成的 Hi-Z RT (由独立的 GrassHiZPass 管理)
            RenderTexture hiZRT = _hiZPass?.HiZTexture;
            bool hiZActive = _hiZEnabled && !debugDisableHiZ && hiZRT != null && hiZRT.IsCreated();

            // 清零计数器
            _gpuCounterBuffer.SetData(_gpuZeroCounters);

            // 设置每帧参数
            _cullingCS.SetMatrix("_FVP", fvp);
            _cullingCS.SetVector("_CameraPos", new Vector4(cameraPos.x, cameraPos.y, cameraPos.z, 0));
            _cullingCS.SetVector("_PlayerPos", new Vector4(playerPos.x, playerPos.y, playerPos.z, 0));
            _cullingCS.SetVector("_GrassLod", new Vector4(_grassLod.x, _grassLod.y, _grassLod.z, 0));
            _cullingCS.SetFloat("_RenderExtent2Sq", (float)(1 << (2 * _renderExtent2)));

            // debugForceOccludeAll: _HiZParams.w = 2 表示"强制剔除所有"
            if (debugForceOccludeAll)
            {
                _cullingCS.SetVector("_HiZParams", new Vector4(1, 1, 0, 2));
                _cullingCS.SetTexture(_kernelCullBlocks, "_HiZTexture", Texture2D.whiteTexture);
            }
            else if (hiZActive)
            {
                Matrix4x4 gpuProj = GL.GetGPUProjectionMatrix(renderCamera.projectionMatrix, true);
                Matrix4x4 gpuVP = gpuProj * renderCamera.worldToCameraMatrix;
                _cullingCS.SetMatrix("_GPUVP", gpuVP);
                _cullingCS.SetVector("_HiZParams", new Vector4(
                    _hiZPass.HiZWidth, _hiZPass.HiZHeight, _hiZPass.HiZMipCount - 1, 1));
                _cullingCS.SetTexture(_kernelCullBlocks, "_HiZTexture", hiZRT);
            }
            else
            {
                _cullingCS.SetVector("_HiZParams", new Vector4(0, 0, 0, 0));
                _cullingCS.SetTexture(_kernelCullBlocks, "_HiZTexture", Texture2D.whiteTexture);
            }

            // Dispatch CullBlocks
            int threadGroups = (_gpuBlockCount + 63) / 64;
            _cullingCS.Dispatch(_kernelCullBlocks, threadGroups, 1, 1);        

            // Dispatch BuildIndirectArgs (构建前缀和 + 逐 block 写入偏移 + 逐矩阵 dispatch 参数)
            _cullingCS.Dispatch(_kernelBuildArgs, 1, 1, 1);

            // Dispatch OutputInstances (逐矩阵并行, 线程数由 GPU 决定)
            _cullingCS.DispatchIndirect(_kernelOutputInstances, _gpuDispatchArgsBuffer, 0);

            Profiler.EndSample();
        }

        /// <summary>
        /// Compute Shader 路径的渲染方法
        /// </summary>
        private void RenderGrassCompute(int typeNum, int pass, CommandBuffer cmd)
        {
            for (int i = 0; i < LodNum; i++)
            {
                cmd.SetGlobalFloat("_NowLOD", i);

                for (int j = 0; j < typeNum; j++)
                {
                    if (_sceneGrassMeshs[j][i] == null || _sceneGrassIndirectMats[j] == null)
                        continue;

                    int argsOffset = (j * LodNum + i) * 5;
                    _gpuPropertyBlock.SetInt(NameID_ArgsOffset, argsOffset + 4);
                    cmd.DrawMeshInstancedIndirect(_sceneGrassMeshs[j][i], 0, _sceneGrassIndirectMats[j], pass,
                        _gpuIndirectArgsBuffer, argsOffset * sizeof(uint), _gpuPropertyBlock);
                }
            }
        }

        //=========================================================DEBUG=============================================
        List<HWorldBounds> _debugGrassBounds;
        internal void ClearDebugGrassBounds()
        {
            _debugGrassBounds.Clear();
        }
        internal void AddDebugGrassBounds(HWorldBounds grassBounds)
        {
            _debugGrassBounds.Add(grassBounds);
        }
        internal void DrawGrassBounds()
        {
            if (_debugGrassBounds != null)
            {
                Gizmos.color = Color.green;
                foreach (var bounds in _debugGrassBounds)
                {
                    Gizmos.DrawWireCube(bounds.Center, 2 * bounds.Extents);
                }
            }
        }
    }
}
