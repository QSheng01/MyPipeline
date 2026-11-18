namespace MightyTerrainMesh
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// One visible quadtree cell, rendered as a pooled GameObject.
    /// Texturing is handled globally by AVT (world-position driven), so every patch
    /// shares a single material and carries no per-patch texture state.
    /// </summary>
    public class MTPooledRenderMesh
    {
        private static Queue<MTPooledRenderMesh> _qPool = new Queue<MTPooledRenderMesh>();
        public static MTPooledRenderMesh Pop()
        {
            if (_qPool.Count > 0)
            {
                return _qPool.Dequeue();
            }
            return new MTPooledRenderMesh();
        }
        public static void Push(MTPooledRenderMesh p)
        {
            p.OnPushBackPool();
            _qPool.Enqueue(p);
        }
        public static void Clear()
        {
            while (_qPool.Count > 0)
            {
                _qPool.Dequeue().DestroySelf();
            }
        }
        private MTRenderMesh mRM;
        private GameObject mGo;
        private MeshFilter mMesh;
        private MeshRenderer mRenderer;
        public MTPooledRenderMesh()
        {
            mGo = new GameObject("_mtpatch");
            mMesh = mGo.AddComponent<MeshFilter>();
            mRenderer = mGo.AddComponent<MeshRenderer>();
            mRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        public void Reset(Material material, MTRenderMesh m, Vector3 offset)
        {
            mRM = m;
            mGo.SetActive(true);
            mGo.transform.position = offset;
            mMesh.mesh = mRM.mesh;
            mRenderer.sharedMaterial = material;
        }
        public void OnPushBackPool()
        {
            if (mGo != null)
                mGo.SetActive(false);
            mRM = null;
        }
        private void DestroySelf()
        {
            if (mGo != null)
                MonoBehaviour.Destroy(mGo);
            mGo = null;
            mMesh = null;
            mRenderer = null;
        }
    }

    public class MTRenderMesh
    {
        public Mesh mesh;
        public Vector2 uvmin;
        public Vector2 uvmax;
        public void Clear()
        {
            MonoBehaviour.Destroy(mesh);
            mesh = null;
        }
    }
}
