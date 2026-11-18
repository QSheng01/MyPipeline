using System;
using System.Collections.Generic;
#if MAP_EDITOR
using Sirenix.OdinInspector;
#endif
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SYPackages.PerObjectShadow
{
    //[ExecuteAlways]
    [DisallowMultipleComponent]
    public class PerObjectShadowRenderers : MonoBehaviour
    {
        [SerializeField] public List<PerObjectShadowRenderContext> renderContexts = new List<PerObjectShadowRenderContext>(10);


        private void Awake()
        {
            SetUpPOSRenderers();
        }

        public void SetUp()
        {
            foreach (var renderContext in renderContexts)
            {
                renderContext.SetUp();
            }
        }
        // private void OnDisable()
        // {
        //     //var manager = GetComponentInParent<PerObjectShadowEntity>();
        //     if (_entity)
        //     {
        //         _entity.RemovePerObjectShadowItem(this);
        //     }
        //     _entity = null;
        // }

#if MAP_EDITOR
        [Button("刷新阴影信息")]
#endif
        public void SetUpPOSRenderers()
        {
            var renderers = this.GetComponentsInChildren<Renderer>(false);
            renderContexts.Clear();
            foreach (var renderer in renderers)
            {
                if (!renderer) continue;

                bool isMeshRenderer = renderer is MeshRenderer;
                bool isSkinnedMeshRenderer = renderer is SkinnedMeshRenderer;
                if (isMeshRenderer || isSkinnedMeshRenderer)
                {
                    var shareMesh = isMeshRenderer
                        ? renderer.GetComponent<MeshFilter>().sharedMesh
                        : ((SkinnedMeshRenderer)renderer).sharedMesh;
                    if (!shareMesh) continue;

                    renderer.shadowCastingMode = ShadowCastingMode.Off;

                    int count = shareMesh.subMeshCount;

                    // 防止subMesh和material不一致
                    if (isSkinnedMeshRenderer)
                    {
                        count = renderer.sharedMaterials.Length;
                    }
                    //for (int i = 0; i < 1; i++)
                    for (int i = 0; i < count; i++)
                    {
                        if (i >= renderer.sharedMaterials.Length)
                            continue;
                        var material = renderer.sharedMaterials[i];
                        if (!material)
                        {
                            Debug.Log($"存在空的材质球，{renderer.name}");
                            continue;
                        }

                        var shaderpass = material.FindPass(PerObjectShadowPass.DEPTHONLYPASS);
                        if (shaderpass >= 0)
                        {
                            renderContexts.Add(new PerObjectShadowRenderContext(renderer, material, i, shaderpass));
                        }
                    }
                }
                // renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        public void ResetShadowStatus(bool shadowcasterOn)
        {
            foreach (var rendererContext in renderContexts)
            {
                if (rendererContext!=null && rendererContext.Renderer!=null)
                {
                    if (shadowcasterOn)
                    {
                        rendererContext.Renderer.shadowCastingMode = ShadowCastingMode.On;
                    }
                    else
                    {
                        rendererContext.Renderer.shadowCastingMode = ShadowCastingMode.Off;
                    }
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var bounds = PerObjectShadowImpl.GetObjectBoundsInWorldSpace(renderContexts);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
#endif
    }
}