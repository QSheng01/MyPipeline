using System;
using System.Collections.Generic;
using UnityEngine;
#if MAP_EDITOR
using Sirenix.OdinInspector;
#endif
#if UNITY_EDITOR
using UnityEditor;

#endif


namespace SYPackages.PerObjectShadow
{
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class PerObjectShadowEntity : MonoBehaviour
    {
#if MAP_EDITOR
        [LabelText("使用阴影")]
#endif
        public bool UseshadowCaster = true;

#if MAP_EDITOR
        [ShowIf("UseshadowCaster")] [LabelText("是否使用高清阴影")]
#endif
        public bool UsePOSShadow = true;

#if MAP_EDITOR
        [Space(5)] [BoxGroup("阴影质量相关")] [LabelText("最大占用分辨率")] [CustomValueDrawer("IsmaxResolutionValid")]
#endif
        public PerObjectShadowSettings.SliceResolution maxResolution = PerObjectShadowSettings.SliceResolution._256;

#if MAP_EDITOR
        [Space(5)] [BoxGroup("阴影质量相关")] [LabelText("最小占用分辨率")] [CustomValueDrawer("IsminResolutionValid")]
#endif
        public PerObjectShadowSettings.SliceResolution minResolution = PerObjectShadowSettings.SliceResolution._128;

        private List<PerObjectShadowRenderers> m_ShadowRenderersList = new List<PerObjectShadowRenderers>(4);

        private List<PerObjectShadowRenderContext> m_RenderContexts =
            new List<PerObjectShadowRenderContext>(16);

        public List<PerObjectShadowRenderContext> RenderContexts => m_RenderContexts;


        private void OnEnable()
        {
            // rongqingmei 临时测试修改
// #if UNITY_EDITOR
            SetUpPOSRenderers();
// #endif
            PerObjectShadowCtrlManager.Instance.AddPerObjectShadowEntity(this);
        }

        private void OnDisable()
        {
            PerObjectShadowCtrlManager.Instance.RemovePerObjectShadowEntity(this);
            Clear();
        }


        private void AddPerObjectShadowItem(PerObjectShadowRenderers entity)
        {
            if (m_ShadowRenderersList != null && !m_ShadowRenderersList.Contains(entity))
            {
                m_ShadowRenderersList.Add(entity);
                UpdateRenderContexts();
            }
        }

        private void RemovePerObjectShadowItem(PerObjectShadowRenderers entity)
        {
            if (m_ShadowRenderersList != null && m_ShadowRenderersList.Contains(entity))
            {
                m_ShadowRenderersList.Remove(entity);
                UpdateRenderContexts();
            }
        }

        private void UpdateRenderContexts()
        {
            m_RenderContexts.Clear();
            foreach (var entity in m_ShadowRenderersList)
            {
                if (!entity)
                {
                    continue;
                }

                foreach (var renderContext in entity.renderContexts)
                {
                    if (!m_RenderContexts.Contains(renderContext))
                    {
                        m_RenderContexts.Add(renderContext);
                    }
                }
            }
        }

        public void Clear(bool cleanAll = true)
        {
            if (cleanAll)
            {
                m_ShadowRenderersList?.Clear();
            }
            else
            {
                if (m_ShadowRenderersList == null)
                {
                    return;
                }

                for (int i = m_ShadowRenderersList.Count - 1; i >= 0; i--)
                {
                    m_ShadowRenderersList.RemoveAt(i);
                }
            }
        }

        [ContextMenu("更新逐对象阴影")]
        public void SetUpPOSRenderers()
        {
            SetUpPOSRenderers(UseshadowCaster, UsePOSShadow);
        }

        public void SetUpPOSRenderers(bool useshadowCaster = true, bool usePOSShadow = true)
        {
            if (!this.enabled)
            {
                return;
            }

            Clear();
            this.UsePOSShadow = usePOSShadow;
            var items = GetComponentsInChildren<PerObjectShadowRenderers>();
            foreach (var item in items)
            {
                item.SetUp();
                // item.ResetShadowStatus(UseshadowCaster & !UsePOSShadow);
                if (UseshadowCaster && usePOSShadow)
                {
                    AddPerObjectShadowItem(item);
                }
            }
        }

#if UNITY_EDITOR
        private PerObjectShadowSettings.SliceResolution IsmaxResolutionValid(
            PerObjectShadowSettings.SliceResolution value, GUIContent label)
        {
            return (PerObjectShadowSettings.SliceResolution) EditorGUILayout.EnumPopup(label, value);
        }

        private PerObjectShadowSettings.SliceResolution IsminResolutionValid(
            PerObjectShadowSettings.SliceResolution value, GUIContent label)
        {
            if ((int) value > (int) maxResolution)
            {
                value = maxResolution;
            }

            return (PerObjectShadowSettings.SliceResolution) EditorGUILayout.EnumPopup(label, value);
        }
#endif
    }
}