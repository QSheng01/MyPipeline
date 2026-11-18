// Gavin_KG presents

using System.Collections.Generic;
using SYPackages.PerObjectShadow.RunTimePacker;
using UnityEngine;
using UnityEngine.Profiling;

namespace SYPackages.PerObjectShadow
{
    /// <summary>
    ///
    /// If used inside a URP-like custom SRP, this class can be directly implemented inside your ScriptableRenderer subclass.
    ///
    /// terms:
    /// - World Space (WS): Same as usual
    ///        | "View Matrix"
    /// - Shadow View Space: centered at near plane's center (bounds' back face), oriented as the light.
    ///        | "Projection Matrix"
    /// - Shadow Clip Space: clip space for a single shadow caster, just like a camera (directional->orthogonal, spot->perspective)
    ///
    /// </summary>
    public partial class PerObjectShadowImpl
    {
        // more or less like ShadowSliceData in URP, shared between shadow pass and resolve pass.
        public class SliceData
        {
            // // update when RegisterEntity (assume not changed during gameplay)
            // public GameObject gameObject = null; // tracked object, can be null to indicate a invalid slice
            // public Renderer[] renderers; // tracked renders inside this object
            public struct SliceDataPerFrame
            {
                public Bounds boundsWS; // bounds in world space
                public Matrix4x4 shadowViewToWorldMatrixForBounds; // [TEMP][DEBUG]
                public Matrix4x4 shadowViewMatrix;

                public Matrix4x4
                    shadowProjMatrix; // OpenGL-like proj matrix, following unity's convension. Do not pass directly to shader. Use Unity API instead!!

                // public Vector2Int
                //     sliceResolution; // slice resolution, might not be the same as settings.perObjectShadowResolution due to adaptive size turned on
                // // UpdateSliceData_AtlasPacking()
                // public Vector2Int sliceOffset; // in pixel, / ShadowTextureResolution to get uv offset
                public RectInt rect;

                public Bounds boundsSVS; // [TEMP][DEBUG] bounds in shadow view space, encapsulating boundsWS
                public Vector3 nearPlaneCenterWS; // [TEMP][DEBUG]
                public Matrix4x4 frustumLocalToWorldMatrix; // used directly by shader (in vertex)

                // UpdateSliceData_Culling()
                public bool disabled; // true: obj might be culled, no need to draw shadow/frustum.

                public float distanceSquare; // true: obj might be culled, no need to draw shadow/frustum.

                // UpdateSliceData_PrepareResolveData()
                public Matrix4x4
                    worldToAtlasUVMatrix; // used directly by shader (in fragment). UV are not flipped based on platform (unity conversion).

                public Vector4
                    sliceUVOffsetExtend; // offset + extend(size of uv range). Shader use this range to clamp uv, prevent sampling in other slices. xy: minUV, zw: extend
            }


            public PerObjectShadowEntity entity;

            // Updated per frame:
            // all variables marked with [TEMP] are not used in render passes. They might be removed when debugging is not necessary.
            // all variables marked with [TEMP][DEBUG] are used to draw debug gizmos.
            public SliceDataPerFrame sliceDataPerFrame;

            public bool IsValid
            {
                get { return entity != null &&
                             entity.gameObject != null &&
                             entity.gameObject.activeInHierarchy &&
                             entity.UseshadowCaster&entity.UsePOSShadow;
                }
            }

            public bool ShouldRender
            {
                get { return IsValid && !sliceDataPerFrame.disabled; }
            }

            /// <summary>
            /// or, "Validate"
            /// </summary>
            /// <param name="gameObject"></param>
            public void SetEntity(PerObjectShadowEntity entity)
            {
                this.entity = entity;
            }

            public void Invalidate()
            {
                entity = null;
            }


            // public bool CompareTo(object obj)
            // {
            //     var other = obj as SliceData;
            //     if (other == null) return false;
            //
            //     if (this.sliceDataPerFrame.rect.y != other.sliceDataPerFrame.rect.y)
            //     {
            //         return this.sliceDataPerFrame.rect.y < other.sliceDataPerFrame.rect.y;
            //     }
            //     else
            //     {
            //         return this.GetHashCode() < other.GetHashCode();
            //     }
            // }
            public static bool operator <(SliceData lhs, SliceData rhs)
            {
                // return lhs.renderPassEvent < rhs.renderPassEvent;
                if (!lhs.ShouldRender)
                {
                    return true;
                }

                if (lhs.sliceDataPerFrame.rect.height != rhs.sliceDataPerFrame.rect.height)
                {
                    return lhs.sliceDataPerFrame.rect.height < rhs.sliceDataPerFrame.rect.height;
                }
                else
                {
                    return lhs.GetHashCode() < rhs.GetHashCode();
                }
            }

            public static bool operator >(SliceData lhs, SliceData rhs)
            {
                if (!lhs.ShouldRender)
                {
                    return false;
                }
                return lhs.sliceDataPerFrame.distanceSquare < rhs.sliceDataPerFrame.distanceSquare;

                // if (lhs.sliceDataPerFrame.rect.height != rhs.sliceDataPerFrame.rect.height)
                // {
                //     return lhs.sliceDataPerFrame.rect.height > rhs.sliceDataPerFrame.rect.height;
                // }
                // else
                // {
                //     return lhs.GetHashCode() > rhs.GetHashCode();
                // }
            }
        }

        #region Properties

        // Settings exposed to Unity inspector. Read only
        private PerObjectShadowSettings _settings;

        /// <summary>
        /// All registered gameobject will have its slice data store in here.
        /// Length = maxObjects, will NOT contain null.
        /// This is NOT the final list used in render pass, since gameobject can be destroyed or culled.
        /// </summary>
        //public List<SliceData> SliceDataList { get; private set; }
        public SliceData[] SliceDataList { get; private set; }

        private static GreedyPacker _packer;

        /// <summary>
        /// Shadow atlas texture resolution.
        /// </summary>
        public Vector2Int AtlasResolution { get; private set; }

        #endregion

        #region Getters

        // All getters should be used AFTER UpdateData is called to get the up-to-date result.

        public Vector2 TexelSize
        {
            get { return Vector2.one / AtlasResolution; }
        }
        /// <summary>
        /// valid: object exists,entity are not null;
        /// </summary>
        public int ValidSliceCount
        {
            get
            {
                int c = 0;
                foreach (SliceData data in SliceDataList)
                {
                    if (data.IsValid)
                    {
                        ++c;
                    }
                }
                return c;
            }
        }

        /// <summary>
        /// valid: object exists + object are not being culled (if frustum culling is enabled).
        /// </summary>
        public int ValidRenderSliceCount
        {
            get
            {
                int c = 0;
                foreach (SliceData data in SliceDataList)
                {
                    if (data.ShouldRender)
                    {
                        ++c;
                    }
                }

                return c;
            }
        }

        private Matrix4x4[] _worldToAtlasUVMatrixArray;

        /// <summary>
        /// Commonly used in MaterialPropertyBlock.SetMatrixArray
        /// </summary>
        public Matrix4x4[] WorldToAtlasUVMatrixArray
        {
            get
            {
                int validCount = ValidRenderSliceCount;
                if (_worldToAtlasUVMatrixArray == null || _worldToAtlasUVMatrixArray.Length != validCount)
                {
                    _worldToAtlasUVMatrixArray = new Matrix4x4[validCount];
                }

                int index = 0;
                foreach (SliceData data in SliceDataList)
                {
                    if (!data.ShouldRender) continue;
                    _worldToAtlasUVMatrixArray[index++] = data.sliceDataPerFrame.worldToAtlasUVMatrix;
                }

                return _worldToAtlasUVMatrixArray;
            }
        }


        private Matrix4x4[] _frustumLocalToWorldMatrixArray;

        /// <summary>
        /// Commonly used in DrawMeshInstanced
        /// </summary>
        public Matrix4x4[] FrustumLocalToWorldMatrixArray
        {
            get
            {
                int validCount = ValidRenderSliceCount;
                if (_frustumLocalToWorldMatrixArray == null || _frustumLocalToWorldMatrixArray.Length != validCount)
                {
                    _frustumLocalToWorldMatrixArray = new Matrix4x4[validCount];
                }

                int index = 0;
                foreach (SliceData data in SliceDataList)
                {
                    if (!data.ShouldRender) continue;
                    _frustumLocalToWorldMatrixArray[index++] = data.sliceDataPerFrame.frustumLocalToWorldMatrix;
                }

                return _frustumLocalToWorldMatrixArray;
            }
        }


        private Vector4[] _sliceUVOffsetExtendArray;

        public Vector4[] SliceUVOffsetExtendArray
        {
            get
            {
                int validCount = ValidRenderSliceCount;
                if (_sliceUVOffsetExtendArray == null || _sliceUVOffsetExtendArray.Length != validCount)
                {
                    _sliceUVOffsetExtendArray = new Vector4[validCount];
                }

                int index = 0;
                foreach (SliceData data in SliceDataList)
                {
                    if (!data.ShouldRender) continue;
                    _sliceUVOffsetExtendArray[index++] = data.sliceDataPerFrame.sliceUVOffsetExtend;
                }

                return _sliceUVOffsetExtendArray;
            }
        }

        public void GetShadowMatrixInfo(out Matrix4x4[] localtoWorldMatrix4X4s,
            out Matrix4x4[] worldToAtlasUVMatrixArray, out Vector4[] sliceUVOffsetExtendArray, out Vector4 texelSize)
        {
            localtoWorldMatrix4X4s = FrustumLocalToWorldMatrixArray;
            worldToAtlasUVMatrixArray = WorldToAtlasUVMatrixArray;
            sliceUVOffsetExtendArray = SliceUVOffsetExtendArray;
            texelSize = TexelSize;
        }


        // public bool ShouldEvenRender
        // {
        //     get
        //     {
        //         return PerObjectShadowCtrl.CharacterEntities.Count != 0 &&
        //                AtlasResolution != Vector2Int.zero;
        //     }
        // }

        #endregion


        /// <summary>
        /// Inited in PerObjectShadowPass's ctor
        /// </summary>
        // public PerObjectShadowImpl(PerObjectShadowSettings settings)
        // {
        //     this.Settings = settings;
        //     SliceDataList = new SliceData[64];
        //     for (int i = 0; i < SliceDataList.Length; ++i)
        //     {
        //         SliceDataList[i] = new SliceData();
        //     }
        // }
        public PerObjectShadowImpl()
        {
            _packer = new GreedyPacker(1024, 1024);
            SliceDataList = new SliceData[32];
            for (int i = 0; i < SliceDataList.Length; ++i)
            {
                SliceDataList[i] = new SliceData();
            }
        }

        private void SetPerObjectShadowSetting(PerObjectShadowSettings settings)
        {
            _settings = settings;
            int size = (int) _settings.ResolutionSize;
            _packer = new GreedyPacker(size, size);
        }

        #region API

        /// <summary>
        /// Clear all registered gameobjects.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < SliceDataList.Length; ++i)
            {
                SliceDataList[i].Invalidate();
            }
        }

        public void SortSliceData()
        {
            int j;
            for (int i = 1; i < SliceDataList.Length; ++i)
            {
                SliceData curr = SliceDataList[i];
                j = i - 1;
                for (; j >= 0 && curr > SliceDataList[j]; --j)
                    SliceDataList[j + 1] = SliceDataList[j];

                SliceDataList[j + 1] = curr;
            }
        }

        /// <summary>
        /// 获取需要渲染的对象
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="testIfUpperMost"></param>
        public void RegisterAllEntity(List<PerObjectShadowEntity> entities)
        {
            try
            {
                if (entities == null)
                {
                    return;
                }

                foreach (PerObjectShadowEntity entity in entities)
                {
                    RegisterEntity(entity);
                }
            }
            catch (System.Exception)
            {
                // ignored
            }
        }

        public void RegisterEntity(PerObjectShadowEntity entity)
        {
            if (entity == null)
            {
                return;
                // throw new System.ArgumentNullException();
            }
            // check if already registered.
            foreach (SliceData data in SliceDataList)
            {
                if (data.entity == entity)
                {
                    return;
                }
            }
            // check again to find empty slot to register
            int i = 0;
            for (; i < SliceDataList.Length; ++i)
            {
                if (!SliceDataList[i].IsValid)
                {
                    SliceDataList[i].SetEntity(entity);
                    break;
                }
            }

            if (i == SliceDataList.Length)
            {
                // Debug.LogWarning("PerObjectShadowHelper::RegisterObject: already reaches size limit when registering new object: " + gameObject.name);
                // todo: FIFO
                return;
            }
        }

        public void RemoveEntity(PerObjectShadowEntity entity)
        {
            for (int i = 0; i < SliceDataList.Length; ++i)
            {
                if (SliceDataList[i].entity == entity)
                {
                    SliceDataList[i].Invalidate();
                    break;
                }
            }
        }

        /// <summary>
        /// Update shadow data for this frame.
        /// In URP, this should be called inside Execute()
        /// </summary>
        /// <param name="camera"></param>
        public void Update(Camera camera,PerObjectShadowSettings setting)
        {
            SetPerObjectShadowSetting(setting);

            UpdateSliceData(camera);
        }

        #endregion

        private bool IsOutOfRender(SliceData data, Camera camera, float distance)
        {
            if (distance < 0)
            {
                return false;
            }
            else
            {
                if (data == null || data.entity == null || camera == null)
                {
                    return true;
                }

                Vector3 a = data.entity.transform.position;
                Vector3 b = camera.transform.position;
                float num1 = a.x - b.x;
                float num2 = a.y - b.y;
                float num3 = a.z - b.z;
                return (num1 * num1 + num2 * num2 + num3 * num3) > (distance * distance);
            }
        }

        private float RenderDistanceSquare(SliceData data, Camera camera)
        {
            if (data == null || data.entity == null || camera == null)
            {
                return float.MaxValue;
            }

            Vector3 a = data.entity.transform.position;
            Vector3 b = camera.transform.position;
            float num1 = a.x - b.x;
            float num2 = a.y - b.y;
            float num3 = a.z - b.z;
            return (num1 * num1 + num2 * num2 + num3 * num3);
        }


        void UpdateSliceData(Camera camera)
        {
            Profiler.BeginSample("UpdateSliceData");
            GeometryUtility.CalculateFrustumPlanes(camera, globalCameraFrustumPlanes);

            if (_settings == null)
            {
                return;
            }

            foreach (SliceData data in SliceDataList)
            {
                if (!data.IsValid)
                {
                    continue;
                }

                data.sliceDataPerFrame.distanceSquare = RenderDistanceSquare(data, camera);
                if (data.sliceDataPerFrame.distanceSquare > _settings.shadowDistance * _settings.shadowDistance)
                {
                    data.sliceDataPerFrame.disabled = true;
                }
                else
                {
                    UpdateSliceData_Culling(globalCameraFrustumPlanes, ref data.sliceDataPerFrame);
                    UpdateSliceData_RetrieveSceneData(data);
                    UpdateSliceData_BeforeCulling(ref data.sliceDataPerFrame, _settings.LightRotation,
                        _settings.frustumExtend);
                }

                // 更新分辨率占比
                if (data.ShouldRender)
                {
                    // not culled
                    UpdateSliceData_AdaptiveRes(camera, _settings, data);
                }
                else
                {
                    data.sliceDataPerFrame.rect = new RectInt(-1, -1, -1, -1);
                    data.sliceDataPerFrame.distanceSquare = float.MaxValue;
                }
            }
            this.SortSliceData();
            _packer.Clear();
            int i = 0;
            foreach (SliceData data in SliceDataList)
            {
                if (!data.ShouldRender)
                {
                    continue;
                }

                //Debug.Log("CameraType:  "+camera.cameraType+ "  index:" +i+  "   Rect:" +data.sliceDataPerFrame.rect);
                data.sliceDataPerFrame.rect =
                    _packer.Insert(data.sliceDataPerFrame.rect.width, data.sliceDataPerFrame.rect.height);
                i++;
            }

            int width = 0;
            int height = 0;
            _packer.actualityWidth(ref width, ref height);
            // 修改为2次幂
            if (_settings.isPowBy2)
            {
                var xPow2 = Mathf.Log(width, 2);
                var yPow2 = Mathf.Log(height, 2);
                width = (int) Mathf.Pow(2, Mathf.Ceil(xPow2));
                height = (int) Mathf.Pow(2, Mathf.Ceil(yPow2));
            }

            AtlasResolution = new Vector2Int(width, height);
            //Debug.Log("AtlasResolution:"+AtlasResolution);
            foreach (SliceData data in SliceDataList)
            {
                if (!data.ShouldRender)
                {
                    continue;
                }

                UpdateSliceData_PrepareResolveData(ref data.sliceDataPerFrame, AtlasResolution);
            }

            Profiler.EndSample();
        }

        static void UpdateSliceData_Culling(Plane[] frustumPlanes, ref SliceData.SliceDataPerFrame data)
        {
            Profiler.BeginSample("UpdateSliceData_Culling");
            Bounds frustumBoundsWS = GetEncapsulatedBoundsInSpace(FrustumCubeCorners, data.frustumLocalToWorldMatrix);
            data.disabled = !GeometryUtility.TestPlanesAABB(frustumPlanes, frustumBoundsWS);
            Profiler.EndSample();
        }

        /// <summary>
        /// 更新包围盒
        /// </summary>
        /// <param name="data"></param>
        void UpdateSliceData_RetrieveSceneData(SliceData data)
        {
            data.sliceDataPerFrame.boundsWS = GetObjectBoundsInWorldSpace(data.entity.RenderContexts);
        }


        #region Debug

        // since this class is not inherited from MonoBehaviour, a wrapper MonoBehaviour class can forward its OnDrawGizmos call to this function.
        public void DrawBoundsGizmos()
        {
            foreach (SliceData sliceData in SliceDataList)
            {
                if (!sliceData.ShouldRender)
                {
                    continue;
                }

                Color ogColor = Gizmos.color;
                Gizmos.color = Color.yellow;
                Matrix4x4 ogMatrix = Gizmos.matrix;
                Gizmos.matrix = sliceData.sliceDataPerFrame.shadowViewToWorldMatrixForBounds;
                Gizmos.DrawWireCube(sliceData.sliceDataPerFrame.boundsSVS.center,
                    sliceData.sliceDataPerFrame.boundsSVS.size);
                Gizmos.DrawWireSphere(sliceData.sliceDataPerFrame.boundsSVS.center, 0.05f);
                Gizmos.matrix = ogMatrix;
                Gizmos.DrawSphere(sliceData.sliceDataPerFrame.nearPlaneCenterWS, 0.05f);
                Gizmos.color = ogColor;
            }
        }

        public void DrawFrustumCubeGizmos()
        {
            foreach (SliceData sliceData in SliceDataList)
            {
                if (!sliceData.ShouldRender)
                {
                    continue;
                }

                Color ogColor = Gizmos.color;
                Gizmos.color = Color.white;
                Matrix4x4 ogMatrix = Gizmos.matrix;

                Gizmos.matrix = sliceData.sliceDataPerFrame.frustumLocalToWorldMatrix;
                Gizmos.DrawMesh(global::SYPackages.PerObjectShadow.PerObjectShadowImpl.FrustumCube);

                Gizmos.matrix = ogMatrix;
                Gizmos.DrawSphere(sliceData.sliceDataPerFrame.nearPlaneCenterWS, 0.05f);

                Gizmos.color = ogColor;
            }
        }

        #endregion
    }
}