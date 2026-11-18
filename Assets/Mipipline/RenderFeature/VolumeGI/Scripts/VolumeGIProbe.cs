using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class VolumeGIProbe : MonoBehaviour
{
    #region Sturct
    public struct Surfel
    {
        public float3 position;
        public float3 normal;
        public float3 albedo;
        public float skyMask;
        public float skyAO;

    }

    public struct Brick
    {
        public Surfel[] surfels;
        public float[] weights;
    }

    #endregion


    #region Public
    public RenderTexture posWorldSpaceRT;
    public RenderTexture normalWorldSpaceRT;
    public RenderTexture albedoWorldSpaceRT;
    public ComputeShader surfelSampleCS;
    public ComputeShader surfelReLightCS;
    public ComputeShader surfelClearCS;


    #endregion

    private List<MeshRenderer> objInScene = new List<MeshRenderer>();
    private List<Terrain> TerrainInScene = new List<Terrain>();
    private Dictionary<Material,string> matInScene = new Dictionary<Material, string>();
    [HideInInspector]
    public List<Material> testmat;

    //probe采集到的surfel的radiance数据
    //对应可视化出来的数据 debug用 真正使用时可删
    public ComputeBuffer surfelRadianceBuffer;
    private Vector3[] surfelRadianceBufferDebugData;
    ///probe算出来的sh数据 离线版本可以留 但是实时版本可以删
    public ComputeBuffer coefficientSH9Buffer;
    public int[] coefficientSH9BufferDebugData;
    //probe 算出来的sh数据buffer 用来做debug显示可以删除
    public ComputeBuffer surfelsBuffer;
    public Surfel[] surfelsBufferDebugData; // CPU side surfel array, for debug



    private MaterialPropertyBlock matPropBlock;

    private  int tX = 8;
    private  int tY = 8;
    private  int rayNum = 8 * 8;         // 512 per probe
    private  int surfelByteSize = sizeof(float)*11;  // sizeof(Surfel)
    private int[] coefficientClearValue;

    [HideInInspector]
    public int VolumeIndex = -1;

    [HideInInspector]
    public Camera cam;

    #region Debug
    public Material debugmat;
    public bool editorbuffer;
    #endregion

    Shader posWorldSpaceShader;
    Shader normalWorldSpaceShader;
    Shader albedoShader;

    public void OnDestroy()
    {
        // //初始化 释放buffer
        // if (surfelsBuffer != null)
        // {
        //     surfelsBuffer.Release();
        //     surfelsBuffer = null;
        // }
        //
        // if (surfelRadianceBuffer != null)
        // {
        //     surfelRadianceBuffer.Release();
        //     surfelRadianceBuffer = null;
        // }
        //
        // if (coefficientSH9Buffer != null)
        // {
        //     coefficientSH9Buffer.Release();
        //     coefficientSH9Buffer = null;
        // }

        ReleaseBuffer();

    }



    public void f1()
     {
         objInScene.Clear();
         matInScene.Clear();

         //BaseData
         posWorldSpaceShader = Shader.Find("PRT/GbufferWorldPos");
         normalWorldSpaceShader = Shader.Find("PRT/GbufferNormal");
         //albedoShader = Shader.Find("ShiYue/URP/Detail Props SnowV2");
         albedoShader = Shader.Find("ShiYue/URP/VertexColorPainterShader");
         //创建相机
         cam = this.gameObject.GetComponent<Camera>() != null ? gameObject.GetComponent<Camera>() : this.gameObject.AddComponent<Camera>();
         //cam.farClipPlane = 2000;
         cam.clearFlags = CameraClearFlags.SolidColor;
         cam.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
     }

    public void f2()
     {
         //记录场景物件的材质球
         objInScene = FindObjectsOfType<MeshRenderer>().ToList();
         //TerrainInScene;
         //Debug.Log(temp.Length + "   " + temp[0].materialTemplate.shader.name);
         for (int i = 0; i < objInScene.Count; i++)
         {
             for (int j = 0; j < objInScene[i].sharedMaterials.Length; j++)
             {
                 if(objInScene[i].sharedMaterials[j]!=null)
                     if(!matInScene.ContainsKey(objInScene[i].sharedMaterials[j]))
                         matInScene.Add(objInScene[i].sharedMaterials[j],objInScene[i].sharedMaterials[j].shader.name);
                 //Debug.Log("mesh Name : " + objInScene[i].name + " mat Name : " +  objInScene[i].sharedMaterials[j].name);
             }
         }
     }

    public void f3()
     {
         //var tempmat = new Material(Shader.Find("Amazing Assets/Terrain To Mesh/Splatmap"));
         //var tempmat2 = new Material(Shader.Find("Amazing Assets/Terrain To Mesh/Splatmap"));
         //var tempmat3 = new Material(Shader.Find("Amazing Assets/Terrain To Mesh/Splatmap"));
         //var tempmat4 = new Material(Shader.Find("Amazing Assets/Terrain To Mesh/Splatmap"));
         //
         //tempmat.SetFloat("terrainoutvalue",0.6f);
         //tempmat2.SetFloat("terrainoutvalue",1.6f);
         //tempmat3.SetFloat("terrainoutvalue",2.6f);
         //tempmat4.SetFloat("terrainoutvalue",0f);

         // for (int i = 0; i < TerrainInScene.Count; i++)
         // {
         //     TerrainInScene[i].materialTemplate = tempmat;
         // }
         //三个rendertexture的渲染
         //替换shader
         string terrainname = "Amazing Assets/Terrain To Mesh/Splatmap";

         foreach (Material mat in matInScene.Keys)
         {
             if (mat.shader.name == terrainname)
             {
                 mat.SetFloat("terrainoutvalue",2.6f);
                 continue;
             }
             if(mat.shader.name == "ShiYue/URP/TreeShaderV2" || mat.shader.name == "Universal Render Pipeline/IvyShader" || mat.shader.name == "ShiYue/URP/Detail Props Snow")
             {
                 continue;
             }
             mat.shader = albedoShader;
         }
         cam.RenderToCubemap(albedoWorldSpaceRT);

         foreach (Material mat in matInScene.Keys)
         {
             if (mat.shader.name == terrainname)
             {
                 mat.SetFloat("terrainoutvalue",0.6f);
                 continue;
             }
             mat.shader = posWorldSpaceShader;

         }
         cam.RenderToCubemap(posWorldSpaceRT);

         // for (int i = 0; i < TerrainInScene.Count; i++)
         // {
         //     TerrainInScene[i].materialTemplate = tempmat2;
         // }
         foreach (Material mat in matInScene.Keys)
         {
             if (mat.shader.name == terrainname)
             {
                 mat.SetFloat("terrainoutvalue",1.6f);
                 continue;
             }
             mat.shader = normalWorldSpaceShader;
         }
         cam.RenderToCubemap(normalWorldSpaceRT);


         foreach (Material mat in matInScene.Keys)
         {
             if (mat.shader.name == terrainname)
             {
                 mat.SetFloat("terrainoutvalue",0f);
                 continue;
             }
         }
         // for (int i = 0; i < TerrainInScene.Count; i++)
         // {
         //     TerrainInScene[i].materialTemplate = tempmat4;
         // }
     }

    public void f4()
     {
         //把场景材质球进行还原
         foreach (KeyValuePair<Material, string> entry in matInScene)
         {
             Material mat = entry.Key;
             string shaderName = entry.Value;
             Shader shader = Shader.Find(shaderName);
             if (shader != null)
             {
                 mat.shader = shader;

             }
             else
             {
                 #if UNITY_EDITOR
                 Debug.Log("some mat shader reset failed :  " + shader.name);
                 #endif
             }
         }
         //删除相机
         DestroyImmediate(this.GetComponent<UniversalAdditionalCameraData>());
         DestroyImmediate(cam);
     }

    public void InitBuffer()
    {

        if(coefficientSH9Buffer==null)
        {
            coefficientSH9Buffer = new ComputeBuffer(13, sizeof(int));
            coefficientClearValue = new int[13];
            for(int i=0; i<13; i++) coefficientClearValue[i] = 0;
        }

        if(editorbuffer && (surfelRadianceBuffer==null || !surfelRadianceBuffer.IsValid()))
            surfelRadianceBuffer = new ComputeBuffer(rayNum, sizeof(float) * 3);
        if (editorbuffer && surfelRadianceBufferDebugData == null)
            surfelRadianceBufferDebugData = new Vector3[rayNum];

        if(surfelsBuffer==null)
            surfelsBuffer = new ComputeBuffer(rayNum, surfelByteSize);
        if(surfelsBufferDebugData==null)
            surfelsBufferDebugData = new Surfel[rayNum];
        coefficientSH9BufferDebugData = new int[13];

        if(matPropBlock==null)
            matPropBlock = new MaterialPropertyBlock();
    }

    public void f5()
     {

         var sampleKernel = surfelSampleCS.FindKernel("CSMain");
         Vector3 probePos = gameObject.transform.position;
         surfelSampleCS.SetVector("_probePos", new Vector4(probePos.x, probePos.y, probePos.z, 1.0f));
         surfelSampleCS.SetFloat("_randSeed", UnityEngine.Random.Range(0.0f, 1.0f));
         surfelSampleCS.SetTexture(sampleKernel, "_worldPosCubemap", posWorldSpaceRT);
         surfelSampleCS.SetTexture(sampleKernel, "_normalCubemap", normalWorldSpaceRT);
         surfelSampleCS.SetTexture(sampleKernel, "_albedoCubemap", albedoWorldSpaceRT);
         surfelSampleCS.SetBuffer(sampleKernel, "_surfels", surfelsBuffer);

         // start CS
         surfelSampleCS.Dispatch(sampleKernel, 1, 1, 1);

         //debug

         #region debug
         //if(editorbuffer)
            surfelsBuffer.GetData(surfelsBufferDebugData);
         #endregion
     }


    public void ReleaseBuffer()
    {
        //初始化 释放buffer
        if (surfelsBuffer != null)
        {
            surfelsBuffer.Release();
            surfelsBuffer = null;
        }

        if (surfelRadianceBuffer != null)
        {
            surfelRadianceBuffer.Release();
            surfelRadianceBuffer = null;
        }

        if (coefficientSH9Buffer != null)
        {
            coefficientSH9Buffer.Release();
            coefficientSH9Buffer = null;
        }
    }

    public void f6(int k = 0)
     {

         var parent = transform.parent;

         if (surfelsBuffer == null || !surfelsBuffer.IsValid())
         {
             #if UNITY_EDITOR
             //Debug.LogWarning("surfel buffer == null");
             #endif
             return;
         }
         VolumeGICell probeVolume = parent ==null ? null : parent.gameObject.GetComponent<VolumeGICell>();
         if (!probeVolume.coefficientVoxel.IsValid())
         {
#if UNITY_EDITOR
  //              Debug.LogError("Error1 : parentBufferMissing");
#endif
             return;

         }

         var ClearKernel = surfelClearCS.FindKernel("CSMain");
         surfelClearCS.EnableKeyword("_VOLUMELOCALBAKE");
         surfelClearCS.SetBuffer(ClearKernel,"_coefficientVoxel",probeVolume.coefficientVoxel);
         surfelClearCS.SetBuffer(ClearKernel,"_coefficientSH9", coefficientSH9Buffer);
         surfelClearCS.SetInt("_indexInProbeVolume", VolumeIndex);
         surfelClearCS.Dispatch(ClearKernel, 1, 1, 1);

         //coefficientSH9BufferDebugData = new int[13];
         //for (int i = 0; i < coefficientSH9BufferDebugData.Length; i++)
         //    coefficientSH9BufferDebugData[i] = 0;
         //coefficientSH9Buffer.GetData(coefficientSH9BufferDebugData);

         //得到sh
         var relightKernel = surfelReLightCS.FindKernel("CSMain");

         if (editorbuffer)
         {
             surfelReLightCS.EnableKeyword("_EDITORVIEW");
         }
         else
         {
             surfelReLightCS.DisableKeyword("_EDITORVIEW");
         }

         surfelReLightCS.SetVector("_probePos", new Vector4(gameObject.transform.position.x, gameObject.transform.position.y, gameObject.transform.position.z, 1.0f));
         surfelReLightCS.SetBuffer(relightKernel,"_surfels", surfelsBuffer);
         coefficientSH9Buffer.SetData(coefficientClearValue);
         surfelReLightCS.SetBuffer(relightKernel,"_coefficientSH9", coefficientSH9Buffer);
         if(editorbuffer && surfelRadianceBuffer!=null)
            surfelReLightCS.SetBuffer(relightKernel,"_surfelRadiance", surfelRadianceBuffer);

         if(parent!=null)
         {
             surfelReLightCS.EnableKeyword("_VOLUMELOCALBAKE");
             ComputeBuffer coefficientVoxel = probeVolume.coefficientVoxel;
             surfelReLightCS.SetBuffer(relightKernel, "_coefficientVoxel", coefficientVoxel);
             surfelReLightCS.SetInt("_indexInProbeVolume", VolumeIndex);

         }
         else
         {
             surfelReLightCS.DisableKeyword("_VOLUMELOCALBAKE");
         }

         surfelReLightCS.Dispatch(relightKernel, 1, 1, 1);

         //不回读cpu
         //coefficientSH9Buffer.GetData(coefficientSH9BufferDebugData);
         #region debug
         if (editorbuffer)
         {
             surfelRadianceBuffer.GetData(surfelRadianceBufferDebugData);
             matPropBlock.SetBuffer("_coefficientSH9", coefficientSH9Buffer);
             this.GetComponent<MeshRenderer>().SetPropertyBlock(matPropBlock);
         }
         #endregion

#if UNITY_EDITOR
         if (!EditorApplication.isPlaying)
         {
             ReleaseBuffer();
         }
#endif

     }
    [Button("烘焙")]
    public void Bake()
    {

        var templist = FindObjectsOfType<VolumeGIProbe>();
        foreach (var temp in templist)
        {
            if(temp.gameObject.transform.parent == this.GetComponent<VolumeGIProbe>().transform.parent)
                temp.gameObject.GetComponent<MeshRenderer>().enabled = false;
        }

        f1();

        f2();

        f3();

        f4();

        //存储cubemap 只是作为debug
        //Cubemap debugAlbedoCubemap = new Cubemap(posWorldSpaceRT.width,GraphicsFormat.R8G8B8A8_UNorm,TextureCreationFlags.None);
        //对buffer数据进行卷积处理 (先处理skyocclusion)
        //buffer的初始化
        InitBuffer();
        f5();

        f6(0);

        foreach (var temp in templist)
        {
            if(temp.gameObject.transform.parent == this.GetComponent<VolumeGIProbe>().transform.parent)
                temp.gameObject.GetComponent<MeshRenderer>().enabled = true;
        }
    }


    [Button("debugSet")]
    public void DebugView()
    {
        if(matPropBlock==null)
            matPropBlock = new MaterialPropertyBlock();
        if(coefficientSH9Buffer==null || !coefficientSH9Buffer.IsValid())
            coefficientSH9Buffer = new ComputeBuffer(13, sizeof(int));
        coefficientSH9Buffer.GetData(coefficientSH9BufferDebugData);
        matPropBlock.SetBuffer("_coefficientSH9", coefficientSH9Buffer);
        this.GetComponent<MeshRenderer>().SetPropertyBlock(matPropBlock);
    }
//#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if(!editorbuffer)
            return;
        Vector3 probePos = gameObject.transform.position;

        debugmat.shader = Shader.Find("PRT/SHDebug");

        // read back the result from CS
        if(surfelsBuffer!=null && surfelsBufferDebugData!=null)
            ;
            //surfelsBuffer.GetData(readBackBuffer);
        else
        {

            Debug.LogWarning("drawgizmos buffer missing");
            return;
        }
        if(surfelRadianceBuffer!=null && surfelRadianceBufferDebugData!=null)
            surfelRadianceBuffer.GetData(surfelRadianceBufferDebugData);
        else
        {
            Debug.LogWarning("drawgizmos buffer missing");
            return;
        }
return;
for (int i=0; i<rayNum; i++)
        {
            Surfel surfel = surfelsBufferDebugData[i];
            Vector3 radiance = surfelRadianceBufferDebugData[i];

            Vector3 pos = surfel.position;

            Vector3 dir = pos - probePos;
            dir = Vector3.Normalize(dir);

            float temp = Vector3.Distance(pos, probePos);
            if (temp > 50)
            {
               // Debug.Log(temp + "  pos  " + pos);
            }

            bool isSky = surfel.skyMask >= 0.995;
            Gizmos.color = Color.yellow;


            Gizmos.color = Color.black;
            if(isSky)
            {
                Gizmos.color = Color.white ;
                Gizmos.DrawLine(probePos, probePos + dir * 25.0f);
            }
            else
            {
                Gizmos.DrawLine(probePos, pos);
                Gizmos.color = new Color(radiance.x, radiance.y, radiance.z,1);
                Gizmos.DrawSphere(pos, 0.05f);
            }
        }
    }
//#endif
    double DecodeFloatFromInt(int x)
    {
        return (float)(x) / 100000.0;
    }
}
