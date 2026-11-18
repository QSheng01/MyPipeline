using UnityEngine;

namespace Grass
{
    [DisallowMultipleComponent]//不允许添加多个组件
    public class GrassInstanceData : MonoBehaviour
    {
        readonly static string FlowerTag = "flower";
        readonly static string FlowerIndirectShaderName = "ShiYue/URP/Grass/IndirectRenderFlower";
        readonly static string GrassIndirectShaderName = "ShiYue/URP/Grass/IndirectRenderGrass";
        
        [SerializeField]
        Mesh[] _lodMeshs;

        [SerializeField]
        Material _instanceMat;

        [SerializeField]
        Shader _indirectShader;

        Material _indirectMat;

        public Material InstanceMat
        {
            get
            {
                if (_instanceMat == null) Init();
                return _instanceMat;
            }
        }

        public Mesh[] LodMeshs
        {
            get
            {
                if (_lodMeshs == null || _lodMeshs.Length == 0) Init();
                return _lodMeshs;
            }
        }

        [ContextMenu("更新LOD的Mesh")]
        public bool Init()
        {
            bool needSave = false;
            //Debug.Log("GrassLodData::Init");
            var meshRender = GetMeshRender();

            if (meshRender == null)
            {
                Debug.LogError("GrassInstanceData 找不到材质信息，请检查预制！" + gameObject.name);
                return needSave;
            }

            var meshFilter = transform.GetComponentsInChildren<MeshFilter>();

            if (meshFilter == null || meshFilter.Length == 0)
            {
                Debug.LogError("GrassInstanceData 找不到Mesh信息，请检查预制！" + gameObject.name);
                return needSave;
            }

            if (_indirectShader == null)
            {
                if (gameObject.name.Contains(FlowerTag))
                {
                    _indirectShader = Shader.Find(FlowerIndirectShaderName);
                }
                else
                {
                    _indirectShader = Shader.Find(GrassIndirectShaderName);
                }
                needSave = true;
            }

            _instanceMat = GetMeshRender().sharedMaterial;
            _lodMeshs = new Mesh[meshFilter.Length];
            for (int i = 0; i < meshFilter.Length; i++)
            {
                _lodMeshs[i] = meshFilter[i].sharedMesh;
                if (_lodMeshs[i] == null)
                {
                    Debug.LogError(meshFilter[i].name + "meshFilter组件中 Mesh 丢失，请检查预制！" + gameObject.name);
                }

                if (i > 2)
                {
                    Debug.LogWarning(gameObject.name + "mesh数量大于3，3层LOD最多只能添加3个mesh，请检查预制！" + gameObject.name);
                    break;
                }
            }

            return needSave;
        }

        public Mesh GetLodMesh(int lod)
        {
            if (_lodMeshs == null || LodMeshs.Length == 0)
                return null;

            var length = LodMeshs.Length;
            lod = Mathf.Min(length - 1, lod);
            return LodMeshs[lod];
        }

        public float GetMeshHeight()
        {
            if (_lodMeshs == null || LodMeshs[0] == null)
                return 0;
            return LodMeshs[0].bounds.size.y;
        }

        public Material GetInidrectMat()
        {
            if (_indirectMat != null)
                return _indirectMat;

            if (_indirectShader == null)
            {
                Debug.LogError("GrassInstanceData::GetInidrectMat:: _indirectShader == null");
                return null;
            }

            _indirectMat = new Material(InstanceMat);
            _indirectMat.name = "_indirectMat_" + gameObject.name;
            _indirectMat.shader = _indirectShader;
            return _indirectMat;
        }

        public MeshRenderer GetMeshRender()
        {
            var meshRender = GetComponent<MeshRenderer>();
            if (meshRender == null)
            {
                meshRender = GetComponentInChildren<MeshRenderer>();
            }

            return meshRender;
        }
    }

}