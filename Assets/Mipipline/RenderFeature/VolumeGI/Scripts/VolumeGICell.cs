using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using EffectsPostProcess.Scripts.Util;
using Sirenix.OdinInspector;
using SYPackages.AtmosphericScatteringFog;
using SYPackages.TimeOfDay;
using Unity.Mathematics;
#if UNITY_EDITOR
#if MAP_EDITOR
using Unity.EditorCoroutines.Editor;
#endif
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Color = UnityEngine.Color;

[ExecuteAlways]
public class VolumeGICell : MonoBehaviour
{
    public GameObject siglePRTProbe;

    [HideInInspector]
    public bool isBaked;
    private Vector3 center;
    private Vector3 size;
    [Header("包围盒MIN")]
    public Vector3 min;
    [Header("包围盒MAX")]
    public Vector3 max;

    [Space(10)]
    [Header("X轴像素分辨率")]
    public int probeSizeX = 8;
    [Header("Y轴像素分辨率")]
    public int probeSizeY = 4;
    [Header("Z轴像素分辨率")]
    public int probeSizeZ = 8;
    [Header("Probe间隔(单位：米) 建议默认2不要修改")]
    public float probeGridSize = 2.0f;


    public ComputeBuffer coefficientVoxel;          // array for each probe's SH coefficient
    int[] cofficientVoxelClearValue;

    [Header("天光强度")]
    [Range(0.0f, 5.0f)]
    public float skyLightIntensity = 1.0f;
    [Header("环境强度")]
    [Range(0.0f, 5.0f)]
    public float giIntensity = 1.0f;
    [SerializeField, InspectorName("GI反弹强度")]
    [Range(0, 2)]
    public float _GIBounceIntensity = 0.6f;

    //[HideInInspector]
    public GameObject[] probes;

    [Space(10)]
    [Header("存储文件数量")]
    public int SplitDataCount;
    public List<ProbeVolumeData> dataList;

    [NonSerialized]
    public ComputeShader tex3Dcs;
    [HideInInspector]
    public ComputeShader tex3Dlicer;
    private int[] coefficientSH9Buffer;

    private VolumeGIRenderPass renderPass;
    private bool reSetTexture3D;
    private int preProbeSizeX;
    private int preProbeSizeY;
    private int preProbeSizeZ;
    #region debug

    private bool updating;
    [Space(10)]
    [Header("开启实时刷新？")]
    public bool realTimeTOD;
    [Space(10)]
    [Header("每帧实时刷新的数量")]
    public int updateCountPreFrame = 300;
    private int updateCountTotal = 0;

    [Space(10)]
    [Header("开启debug烘焙？")]
    public bool editorView;
    #endregion



    //public bool relightFlag = false;
    private void OnDestroy()
    {
        if(L0tex.IsCreated())
            L0tex.Release();
        if(L1_0tex.IsCreated())
            L1_0tex.Release();
        if(L1_1tex.IsCreated())
            L1_1tex.Release();
        if(L1_2tex.IsCreated())
            L1_2tex.Release();
        if (coefficientVoxel != null)
        {
            coefficientVoxel.Release();
            coefficientVoxel = null;
        }

    }


    bool initflag = false;
   void OnEnable()
   {
#if UNITY_EDITOR
       if (EditorApplication.isPlaying && !initflag)
#endif
       {
#if UNITY_EDITOR
//            Debug.Log("start set isplaying");
//            Debug.Log(" initflag" + initflag);
#endif
           tex3Dcs = Resources.Load<ComputeShader>("Tex3DGenerateCS");
           initflag = true;
           StartCoroutine(PlayingWaitAndStart());
       }
#if UNITY_EDITOR
#if MAP_EDITOR
       if (!EditorApplication.isPlaying && !initflag)
       {
//           Debug.Log("EditorApplication set isplaying");
//            Debug.Log(" initflag" + initflag);

           EditorCoroutineUtility.StartCoroutineOwnerless(EditorWaitAndStart());
           initflag = true;
       }
#endif
#endif

   }

   IEnumerator EditorWaitAndStart()
   {
       yield return new WaitForEndOfFrame();
       renderPass = new VolumeGIRenderPass();
       Init();
       yield return new WaitForEndOfFrame();
       ReLight(0);
   }

   IEnumerator PlayingWaitAndStart()
   {
       yield return new WaitForEndOfFrame();
       renderPass = new VolumeGIRenderPass();
       Init();
       updating = true;
   }

   void Update()
   {
       int probeNum = probeSizeX * probeSizeY * probeSizeZ;

       if (updating)
       {
           ReLight(1);
           if (updateCountTotal >= probeNum)
           {
#if UNITY_EDITOR
               // Debug.Log("total finish relight");
                //Shader.SetGlobalFloat("relightFlag",relightFlag?1:0);
#endif
               updating = false;
               updateCountTotal = 0;
           }
       }
       else
       {
           updating = realTimeTOD;
       }


       if (reSetTexture3D)
       {
           InitTexture3D();
           reSetTexture3D = false;
       }
   }

    private void AddRenderPass()
    {
        RenderPipelineManager.beginCameraRendering += AddRenderPass;
    }

    private void AddRenderPass(ScriptableRenderContext context, Camera camera)
    {
        //if (!camera.CompareTag("MainCamera"))
        //    return;
        var uacd = Camera.main.GetComponent<UniversalAdditionalCameraData>();
        if (!uacd)
            return;
        if(renderPass==null)
            renderPass = new VolumeGIRenderPass();
        renderPass.Setup(RenderPassEvent.AfterRenderingOpaques,this);
        uacd.scriptableRenderer.EnqueuePass(renderPass);

        RenderPipelineManager.beginCameraRendering -= AddRenderPass;
    }



    // [Button("te")]
    // public void TT()
    // {
    //     for (int i = 0; i < dataList.Count; i++)
    //     {
    //         var go = dataList[i];
    //         var tg = Instantiate(go);
    //
    //         AssetDatabase.CreateAsset(tg, @"Assets/" + "ProbeData" + "Cell" + 0 + "_"+ i + ".asset");
    //     }
    // }
    [Button("初始化")]
    public void Init(int cellIndex = 0)
    {
        int probeNum = probeSizeX * probeSizeY * probeSizeZ;
        if (coefficientVoxel != null)
        {
            coefficientVoxel.Release();
            coefficientVoxel = null;
        }
        coefficientVoxel = new ComputeBuffer(probeNum * 13, sizeof(int));

        cofficientVoxelClearValue = new int[probeNum *  13];
//Array.Clear(cofficientVoxelClearValue,0,probeNum *  13);
 //       for(int i=0; i<cofficientVoxelClearValue.Length; i++) cofficientVoxelClearValue[i] = 0;

        coefficientVoxel.SetData(cofficientVoxelClearValue);


        InitTexture3D(cellIndex);
#if UNITY_EDITOR
//        Debug.Log("texture3d init");
#endif
        GenerateGIProbesPos();
#if UNITY_EDITOR
//        Debug.Log("finish generate");
#endif
        TryLoadSurfelData(this);
#if UNITY_EDITOR
//        Debug.Log("finish Load");
#endif
        TryRebuildcoefficientSH9Buffer();
#if UNITY_EDITOR
//        Debug.Log("finish tryRebuildsh");
#endif
    }

    //[Button("ReLight")]
    void RelightBtn()
    {
        AddRenderPass();
    }

    //k = 0 一次性更新
    void ReLight(int k = 0)
    {

        int probeNum = probeSizeX * probeSizeY * probeSizeZ;
        if (k == 0)
        {
            updateCountTotal = 0;
            updateCountPreFrame = probeNum;
        }
        int index = 0;
        int foreachOnce = 0;
        int i = updateCountTotal;

        #if UNITY_EDITOR
        //Debug.Log("begin Relight");
        #endif

        for (; i < probes.Length; i++)
        {
#if UNITY_EDITOR
//            EditorUtility.DisplayProgressBar("Relight",$"{index}/{probes.Length}",1.0f* index /probes.Length);
#endif
            VolumeGIProbe probe = probes[i].GetComponent<VolumeGIProbe>();
            probe.f6(1);
            foreachOnce++;
            index++;
            if (foreachOnce >= updateCountPreFrame)
            {
                updateCountTotal++;
                break;
            }
            updateCountTotal++;
        }
#if UNITY_EDITOR
//        EditorUtility.ClearProgressBar();
#endif

#if UNITY_EDITOR
    //    Debug.Log("finish Relight");
#endif
        if (k == 0)
        {
            updateCountTotal = 0;
            updateCountPreFrame = 300;
        }

        CSGenerateTexture3D();
    }
    void TryRebuildcoefficientSH9Buffer()
    {
        coefficientSH9Buffer = new[]{0};
        //if(coefficientSH9Buffer.Length != probeSizeX*probeSizeY*probeSizeZ * 13)
            Array.Resize<int>(ref coefficientSH9Buffer, probeSizeX*probeSizeY*probeSizeZ * 13);
        for (int i = 0; i < coefficientSH9Buffer.Length; i++)
        {
            coefficientSH9Buffer[i] = 0;
        }
        int j = 0;
        for (int i = 0; i < dataList.Count; i++)
        {
            for (int k = 0; k < dataList[i].coefficientSH9Buffer.Length; k++)
            {
                coefficientSH9Buffer[j++] = dataList[i].coefficientSH9Buffer[k];
            }
            System.GC.Collect();
        }
    }

    void  TryLoadSurfelData(VolumeGICell volume)
    {
        for (int i = 0; i < dataList.Count; i++)
        {
            if(!dataList[i].bakedFlag)
                continue;
            dataList[i].TryLoadSplitSurfelData(volume,SplitDataCount,i);
            System.GC.Collect();
        }
    }

    //[Button("关闭所有Probe显示")]
    public void CloseRenderProbe()
    {
        for (int i = 0; i < probes.Length; i++)
        {
            probes[i].GetComponent<MeshRenderer>().enabled = false;
        }
    }

    //[Button("开启所有Probe显示")]
    public void OpenRenderProbe()
    {
        for (int i = 0; i < probes.Length; i++)
        {
            probes[i].GetComponent<MeshRenderer>().enabled = true;
        }
    }

    // Update is called once per frame


    private void OnValidate()
    {

        Shader.SetGlobalFloat("_skyLightIntensity",skyLightIntensity);
        Shader.SetGlobalFloat("_GIIntensity",giIntensity);
        Shader.SetGlobalFloat("_GIBounceIntensity", _GIBounceIntensity);
        if (preProbeSizeX != probeSizeX || preProbeSizeY != probeSizeY || preProbeSizeZ != probeSizeZ)
        {
            reSetTexture3D = true;
        }

        preProbeSizeX = probeSizeX;
        preProbeSizeY = probeSizeY;
        preProbeSizeZ = probeSizeZ;
    }

    [Button("生成烘焙点")]
    public void GenerateGIProbesPos(int cellindex = 0)
    {
        bool generateflag = false;
        int probeNum = probeSizeX * probeSizeY * probeSizeZ;

        if (probeNum != this.transform.childCount || probes[0].gameObject==null)
        {
            Transform transform = this.transform;
            for(int i = transform.childCount-1;i >=0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
            for(int i=0; i<probes.Length; i++)
            {
                DestroyImmediate(probes[i]);
            }

            generateflag = true;
        }

        if(probes!=null)
        {
            if (generateflag)
            {
                //生成probe点
                probes = new GameObject[probeNum];

                for(int x=0; x<probeSizeX; x++)
                {
                    for(int y=0; y<probeSizeY; y++)
                    {
                        for(int z=0; z<probeSizeZ; z++)
                        {
                            Vector3 relativePos = new Vector3(x, y, z) * probeGridSize;
                            Vector3 parentPos = gameObject.transform.position;
                            // setup probe
                            int index = x * probeSizeY * probeSizeZ + y * probeSizeZ + z;
                            probes[index] = Instantiate(siglePRTProbe, gameObject.transform) as GameObject;
                            //var temp = probes[index];
                            probes[index].transform.position = relativePos + parentPos;
                            probes[index].GetComponent<VolumeGIProbe>().VolumeIndex = index;
                            probes[index].GetComponent<VolumeGIProbe>().editorbuffer = editorView;
                            probes[index].hideFlags = 0;//HideFlags.HideInHierarchy;
                            if (!editorView)
                            {
                                if(probes[index].GetComponent<MeshRenderer>()!=null)
                                    DestroyImmediate(probes[index].GetComponent<MeshRenderer>());
                                if(probes[index].GetComponent<MeshFilter>()!=null)
                                    DestroyImmediate(probes[index].GetComponent<MeshFilter>());
                            }
                        }
                    }
                }
            }
        }

        // for (int x = 0; x < probeSizeX; x++)
        // {
        //     for (int y = 0; y < probeSizeY; y++)
        //     {
        //         for (int z = 0; z < probeSizeZ; z++)
        //         {
        //             int index = x * probeSizeY * probeSizeZ + y * probeSizeZ + z;
        //             probes[index].GetComponent<VolumeGIProbe>().InitBuffer();
        //         }
        //     }
        // }



    }


    [Button("开始烘焙")]
   public void GenerateGIProbesData(int cellindex = 0)
    {
        //关闭管线的srpbatch
        GraphicsSettings.useScriptableRenderPipelineBatching = false;
        //关闭管线的无效feature 会导致skyocculusion出现偏差
        var currentrender = ((UniversalRenderPipelineAsset) GraphicsSettings.currentRenderPipeline);
        if (currentrender.scriptableRenderer.ContainRendererFeature("AtmosphericScatteringRendererFeature"))
        {
          currentrender.scriptableRenderer.GetRendererFeature("AtmosphericScatteringRendererFeature").SetActive(false);
        }

        // hide debug sphere
        foreach (var go in probes)
        {
            if(go.GetComponent<MeshRenderer>()!=null)
                go.GetComponent<MeshRenderer>().enabled = false;
        }
        int index = 0;
        //cap

        foreach (var go in probes)
        {
#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("烘焙CubeMap",$"{index}/{probes.Length}",1.0f* index /probes.Length);
#endif
            VolumeGIProbe probe = go.GetComponent<VolumeGIProbe>();
            //probe.Bake();
            probe.editorbuffer = editorView;
            probe.InitBuffer();
            probe.f1();
            probe.f2();
            probe.f3();
            probe.f4();
            probe.f5();
            probe.f6(0);
            probe.ReleaseBuffer();
            if (editorView)
                probe.DebugView();
            index++;
        }
#if UNITY_EDITOR
        EditorUtility.ClearProgressBar();
#endif

        StorageSurfelData(this,cellindex);
        //data.StorageSurfelData(this);
        CSGenerateTexture3D();
        //ReLight();

        foreach (var go in probes)
        {
            VolumeGIProbe probe = go.GetComponent<VolumeGIProbe>();
        }

        isBaked = true;

        //烘焙完成
        //打开管线的srpbatch
        GraphicsSettings.useScriptableRenderPipelineBatching = true;
        if (currentrender.scriptableRenderer.ContainRendererFeature("AtmosphericScatteringRendererFeature"))
        {
            currentrender.scriptableRenderer.GetRendererFeature("AtmosphericScatteringRendererFeature").SetActive(true);
        }
    }

   [Button("重建")]
   public void EditorRebuild()
   {
       Init();
       RelightBtn();
   }
   void StorageSurfelData(VolumeGICell volumeGICell, int cellindex)
   {
       var fullCount = probeSizeX * probeSizeY * probeSizeZ;
       dataList.Clear();
#if UNITY_EDITOR
       for (int i = 0; i < SplitDataCount; i++)
       {

           EditorUtility.DisplayProgressBar("保存序列化文件",$"{i}/{SplitDataCount}",1.0f * i /SplitDataCount);
           var level = ScriptableObject.CreateInstance<ProbeVolumeData>();
           AssetDatabase.CreateAsset(level, @"Assets/" + "ProbeData" + "Cell" + cellindex + "_"+ i + ".asset");//在传入的路径中创建资源
           AssetDatabase.SaveAssets(); //存储资源
           AssetDatabase.Refresh(); //刷新
           level.StorageSurfelData(volumeGICell, SplitDataCount, i);
           dataList.Add(level);
       }

       EditorUtility.ClearProgressBar();
#endif
       // level.StorageSurfelData(volumeGIGroupController, 2, 1);

   }


    private Texture3D InitTexture(int resoluX, int resoluY, int resoluZ)
    {
        //var temptex = new Texture3D(resoluX, resoluY, resoluZ, DefaultFormat.HDR, 0);
        //var temptex = new Texture3D(resoluX, resoluY, resoluZ, GraphicsFormat.R16G16B16A16_SFloat, 0);//4.2M
        var temptex = new Texture3D(resoluX, resoluY, resoluZ, GraphicsFormat.R8G8B8A8_UNorm, 0);//2.1M
        //var temptex = new Texture3D(resoluX, resoluY, resoluZ, DefaultFormat.LDR, 0);//
        temptex.wrapMode = TextureWrapMode.Clamp;
        //texture = temptex;
        return temptex;// texture;
    }
    double DecodeFloatFromInt(int x)
    {
        return (float)(x) / 100000.0;
    }




    private RenderTexture L0tex;
    private RenderTexture L1_0tex;
    private RenderTexture L1_1tex;
    private RenderTexture L1_2tex;

    public void InitTexture3D(int cellIndex = 0)
    {
        #if UNITY_EDITOR
        //Debug.LogWarning("reset texture3d");
        #endif
        if (L0tex!=null && L0tex.IsCreated())
        {
            L0tex.Release();
        }

        if (L1_0tex!=null&& L1_0tex.IsCreated())
        {
            L1_0tex.Release();
        }

        if (L1_1tex!=null && L1_1tex.IsCreated())
        {
            L1_1tex.Release();
        }

        if (L1_2tex!=null && L1_2tex.IsCreated())
        {
            L1_2tex.Release();
        }

        //if (L0tex == null)
        {
            L0tex = new RenderTexture(probeSizeX, probeSizeY, 0);
            L0tex.format = RenderTextureFormat.ARGBFloat;
            L0tex.name = "VolumeGIL0";
            L0tex.enableRandomWrite = true;
            L0tex.filterMode = FilterMode.Trilinear;
            L0tex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            L0tex.volumeDepth = probeSizeZ;
            L0tex.Create();
        }


        //if (L1_0tex==null)
        {

            L1_0tex = new RenderTexture(probeSizeX, probeSizeY, 0);
            L1_0tex.format = RenderTextureFormat.ARGBFloat;
            L1_0tex.enableRandomWrite = true;
            L1_0tex.name = "VolumeGIL10";
            L1_0tex.filterMode = FilterMode.Trilinear;
            L1_0tex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            L1_0tex.volumeDepth = probeSizeZ;
            L1_0tex.Create();
        }
        //
        //if (L1_1tex==null)
        {
            L1_1tex = new RenderTexture(probeSizeX, probeSizeY, 0);
            L1_1tex.format = RenderTextureFormat.ARGBFloat;
            L1_1tex.enableRandomWrite = true;
            L1_1tex.name = "VolumeGIL11";
            L1_1tex.filterMode = FilterMode.Trilinear;
            L1_1tex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            L1_1tex.volumeDepth = probeSizeZ;
            L1_1tex.Create();
        }
        //
        //if (L1_2tex==null)
        {
            L1_2tex = new RenderTexture(probeSizeX, probeSizeY, 0);
            L1_2tex.format = RenderTextureFormat.ARGBFloat;
            L1_2tex.enableRandomWrite = true;
            L1_2tex.name = "VolumeGIL12";
            L1_2tex.filterMode = FilterMode.Trilinear;
            L1_2tex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            L1_2tex.volumeDepth = probeSizeZ;
            L1_2tex.Create();
        }

    }


    public void CSGenerateTexture3D()
    {
        if(tex3Dcs == null)
            tex3Dcs = Resources.Load<ComputeShader>("Tex3DGenerateCS");
        if (tex3Dcs == null)
        {
            Debug.LogWarning("resources log missing");
            return;
        }
        var texKernel = tex3Dcs.FindKernel("CSMain");
         if (this.coefficientVoxel == null)
         {
             this.coefficientVoxel = new ComputeBuffer(probeSizeX * probeSizeY * probeSizeZ * 13, sizeof(int));
             this.coefficientVoxel.SetData(coefficientSH9Buffer);
             #if UNITY_EDITOR
             //Debug.LogError("rebuild coefficientVoxelBuffer");
             #endif
             return;
         }

        tex3Dcs.SetBuffer(texKernel, "coefficientVoxel",this.coefficientVoxel);
        tex3Dcs.SetTexture(texKernel,"_Tex3DGIL0",L0tex);
        tex3Dcs.SetTexture(texKernel,"_Tex3DGIL10",L1_0tex);
        tex3Dcs.SetTexture(texKernel,"_Tex3DGIL11",L1_1tex);
        tex3Dcs.SetTexture(texKernel,"_Tex3DGIL12",L1_2tex);
        float4 groupsize = new float4(probeSizeX, probeSizeY, probeSizeZ,1.0f);
        tex3Dcs.SetVector("groupsize",groupsize);
        uint csx, csy, csz;
        tex3Dcs.GetKernelThreadGroupSizes(texKernel,out csx,out csy,out csz);

        tex3Dcs.Dispatch(texKernel,probeSizeX,probeSizeY,probeSizeZ);


        Shader.SetGlobalTexture("_TOD_GITexL0",L0tex);
        Shader.SetGlobalTexture("_TOD_GITexL10",L1_0tex);
        Shader.SetGlobalTexture("_TOD_GITexL11",L1_1tex);
        Shader.SetGlobalTexture("_TOD_GITexL12",L1_2tex);

        Shader.SetGlobalVector("_GITexMin",probes[0].transform.position);
        Shader.SetGlobalVector("_GITexMax",probes[probes.Length-1].transform.position);
        #if UNITY_EDITOR
        //Debug.Log("_GITexMin : " +  probes[0].transform.position);
        //Debug.Log("_GITexMax : " +  probes[probes.Length-1].transform.position);
        #endif

    }


    [Button("保存为离线资产")]
    public void SaveLocalTextureAsset()
    {
        if (L0tex == null)
        {
#if UNITY_EDITOR
           // Debug.LogWarning("RenderTexture3D Init Failed");
#endif
            return;
        }
        var texKernel = tex3Dcs.FindKernel("CSMain");
        if (this.coefficientVoxel == null || !coefficientVoxel.IsValid())
        {
            this.coefficientVoxel = new ComputeBuffer(probeSizeX * probeSizeY * probeSizeZ * 13, sizeof(int));
            this.coefficientVoxel.SetData(coefficientSH9Buffer);
#if UNITY_EDITOR
          //  Debug.LogError("rebuild coefficientVoxelBuffer");
#endif
            return;
        }

        tex3Dcs.SetBuffer(texKernel, "coefficientVoxel",this.coefficientVoxel);
        tex3Dcs.SetTexture(texKernel,"_Tex3DGIL0",L0tex);
        tex3Dcs.SetTexture(texKernel,"_Tex3DGIL10",L1_0tex);
        tex3Dcs.SetTexture(texKernel,"_Tex3DGIL11",L1_1tex);
        tex3Dcs.SetTexture(texKernel,"_Tex3DGIL12",L1_2tex);
        float4 groupsize = new float4(probeSizeX, probeSizeY, probeSizeZ,1.0f);
        tex3Dcs.SetVector("groupsize",groupsize);
        uint csx, csy, csz;
        tex3Dcs.GetKernelThreadGroupSizes(texKernel,out csx,out csy,out csz);

        tex3Dcs.Dispatch(texKernel,probeSizeX,probeSizeY,probeSizeZ);

        var todManager = FindObjectOfType<TimeOfDayManager>();
        string timeStr = "default";
        //没有todmanager的情况默认设置为default
        if (todManager != null)
        {
            timeStr = todManager.timeOfDay.ToString();
        }


        Shader.SetGlobalTexture("_Tex3DGIL0",L0tex);
        Shader.SetGlobalTexture("_Tex3DGIL10",L1_0tex);
        Shader.SetGlobalTexture("_Tex3DGIL11",L1_1tex);
        Shader.SetGlobalTexture("_Tex3DGIL12",L1_2tex);


        Shader.SetGlobalVector("_GITexMin",probes[0].transform.position);
        Shader.SetGlobalVector("_GITexMax",probes[probes.Length-1].transform.position);
#if UNITY_EDITOR
        //Debug.Log("_GITexMin" + probes[0].transform.position);
        //Debug.Log("_GITexMax" + probes[probes.Length-1].transform.position);
#endif
        RenderTex2Tex3D saveclass = new RenderTex2Tex3D();
        saveclass.sliceCS = tex3Dlicer;
        saveclass.Save(L0tex,"GI_L0_"+timeStr);
        saveclass.Save(L1_0tex,"GI_L10_"+timeStr);
        saveclass.Save(L1_1tex,"GI_L11_"+timeStr);
        saveclass.Save(L1_2tex,"GI_L12_"+timeStr);
    }

//     [Button("数据转二进制文件")]
//     public void ConverterToBytes(int cellindex = 0)
//     {
//         BinaryFormatter bf = new BinaryFormatter();
//         FileStream file = null;
//         for (int i = 0; i < dataList.Count; i++)
//         {
//             string path = @"Assets/" + "ProbeData" + "Cell" + cellindex + "_" + i + ".gidata";
//             file = File.Open(path, FileMode.Create, FileAccess.Read);
//             bf.Serialize(file, dataList[i]);
//             file.Close();
// #if UNITY_EDITOR
//             print("成功存储");
// #endif
//         }
//
//     }
    private void OnDrawGizmos()
    {
        Gizmos.color = isBaked?new Color(0,0,1,0.2f):new Color(1,0,0,0.2f);
        min = this.transform.position;
        max = min + new Vector3((probeSizeX-1)*probeGridSize,(probeSizeY-1)*probeGridSize,(probeSizeZ-1)*probeGridSize);
        center = new Vector3((max.x + min.x) / 2,(max.y + min.y) / 2,(max.z + min.z) / 2);
        var lenghty = (probeSizeY - 1) * (probeGridSize);
        var lenghtx = (probeSizeY - 1) * (probeGridSize);
        var lenghtz = (probeSizeZ - 1) * (probeGridSize);
        //Debug.Log("min" + min);
        //Debug.Log("max" + max);
        //Debug.Log(center);
        //Gizmos.DrawCube(center,max-min);
        Gizmos.DrawCube(center,new Vector3(lenghtx,lenghty,lenghtz));
    }


    public class VolumeGIRenderPass : ScriptableRenderPass
    {
        private ProfilingSampler _profilingSampler = new ProfilingSampler("UpdateVolumeGI");
        private VolumeGICell _bakeController = new VolumeGICell();
        public void Setup(RenderPassEvent passEventvent, VolumeGICell volumeGICell)//, GrassOcclusion occlusionProbes)
        {
            renderPassEvent = passEventvent;
            _bakeController = volumeGICell;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();
           // using (new ProfilingScope(cmd, _profilingSampler))
           Profiler.BeginSample("Relight");
            {
                cmd.Clear();
                _bakeController.ReLight(0);
            }
            Profiler.EndSample();

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }


}
