using System;
using System.Collections.Generic;
#if MAP_EDITOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

namespace SYPackages.PerObjectShadow
{
    public class PerObjectShadowCtrlManager
    {
        private static PerObjectShadowCtrlManager instance;

        /// <summary>
        /// 实现类
        /// </summary>
        private PerObjectShadowImpl _impl;
        /// <summary>
        /// 参数控制类
        /// </summary>
        private PerObjectShadowCtrl _currentCtrl;


        public static PerObjectShadowCtrlManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new PerObjectShadowCtrlManager();
                }

                return instance;
            }
        }

        public PerObjectShadowCtrl CurrentCtrl
        {
            get { return _currentCtrl; }
            private set
            {
                _currentCtrl = value;
            }
        }

        public PerObjectShadowImpl perObjectShadowImpl
        {
            get { return _impl; }
        }

        public PerObjectShadowSettings perObjectShadowSetting
        {
            get { return CurrentCtrl?.perObjectShadowSettings; }
        }


        public PerObjectShadowCtrlManager()
        {
            _impl = new PerObjectShadowImpl();
        }

        internal void SetCurrentPerObjectShadowCtrl(PerObjectShadowCtrl ctrl)
        {
            CurrentCtrl = ctrl;
        }

        internal void TryToRemovePerObjectShadowCtrl(PerObjectShadowCtrl ctrl)
        {
            if (CurrentCtrl == ctrl)
            {
                CurrentCtrl = null;
            }
        }

        private void RefreshShadowEntity()
        {
#if UNITY_EDITOR
            var entities = GameObject.FindObjectsOfType<PerObjectShadowEntity>();
            foreach (var entity in entities)
            {
                AddPerObjectShadowEntity(entity);
            }
#endif
        }

        internal void AddPerObjectShadowEntity(PerObjectShadowEntity entity)
        {
            if (entity != null)
            {
                perObjectShadowImpl?.RegisterEntity(entity);
            }
        }

        internal void RemovePerObjectShadowEntity(PerObjectShadowEntity entity)
        {
            if (entity != null)
            {
                perObjectShadowImpl?.RemoveEntity(entity);
            }
        }

        public bool IsPoSValid()
        {
            return perObjectShadowImpl.ValidSliceCount > 0 &&
                   CurrentCtrl != null &&
                   CurrentCtrl.perObjectShadowSettings.resolveMat != null;
        }
        // public bool IsPoSRenderValid()
        // {
        //     return perObjectShadowImpl.ValidRenderSliceCount > 0 &&
        //            CurrentCtrl != null &&
        //            CurrentCtrl.perObjectShadowSettings.resolveMat != null;
        // }


        public bool IsResolutionValid()
        {
            return perObjectShadowImpl.AtlasResolution.x > 0 &&
                      perObjectShadowImpl.AtlasResolution.y > 0;
        }

        public void Update(Camera camera)
        {
            if (_currentCtrl == null ||_currentCtrl.perObjectShadowSettings==null)
            {
                return;
            }
            perObjectShadowImpl?.Update(camera,_currentCtrl.perObjectShadowSettings);
        }

        public Vector2Int GetAtlasResolution()
        {
            return perObjectShadowImpl.AtlasResolution;
        }
    }

    [ExecuteAlways]
    public class PerObjectShadowCtrl:MonoBehaviour
    {
        public PerObjectShadowSettings perObjectShadowSettings = new PerObjectShadowSettings();


#if UNITY_EDITOR
#if MAP_EDITOR
        [Title("Debug相关")] [LabelText("显示阴影包围")]
        public bool debugShowFrustum = false;
#endif
#endif

        public void DoLoad()
        {
            Init();
            PerObjectShadowCtrlManager.Instance.SetCurrentPerObjectShadowCtrl(this);
            //Debug.Log("load shadowCtrl~~~");
        }

        [ContextMenu("使用默认阴影")]
        public void UseNormalShadow()
        {
            var entities =  GameObject.FindObjectsOfType<PerObjectShadowEntity>(true);
            if (entities!=null)
            {
                foreach (var entity in entities)
                {
                    entity.UseshadowCaster = true;
                    entity.UsePOSShadow = false;
                    entity.SetUpPOSRenderers();
                }
            }
        }

        private void Init()
        {
            //perObjectShadowSettings.DirectionLight =  RenderSettings.sun? RenderSettings.sun: null;
        }

        public void UnLoad()
        {
            PerObjectShadowCtrlManager.Instance.TryToRemovePerObjectShadowCtrl(this);
        }

        // 配置启动
        private void OnEnable()
        {
            DoLoad();
        }

#if UNITY_EDITOR

        public void DrawFrustumCubeGizmos()
        {
            foreach (PerObjectShadowImpl.SliceData sliceData in PerObjectShadowCtrlManager.Instance.perObjectShadowImpl.SliceDataList)
            {
                if (!sliceData.ShouldRender)
                {
                    continue;
                }

                Color ogColor = Gizmos.color;
                Gizmos.color = Color.red;
                Matrix4x4 ogMatrix = Gizmos.matrix;

                Gizmos.matrix = sliceData.sliceDataPerFrame.frustumLocalToWorldMatrix;
                Gizmos.DrawWireMesh(PerObjectShadowImpl.FrustumCube);

                Gizmos.matrix = ogMatrix;
                Gizmos.DrawWireSphere(sliceData.sliceDataPerFrame.nearPlaneCenterWS, 0.05f);

                Gizmos.color = ogColor;
            }
        }

        private void OnDrawGizmos()
        {
            #if MAP_EDITOR
            if (!debugShowFrustum) return;
            DrawFrustumCubeGizmos();
            #endif
        }
#endif
    }
}