using System;
using System.IO;
using System.Reflection;
using System.Text;
using NoOvertime.VirtualTexture;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

public static class ClipmapTextureValidation
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static FieldInfo Field(string name) => typeof(ClipmapTexture).GetField(name, Private);
    static T Get<T>(ClipmapTexture c, string name) => (T)Field(name).GetValue(c);
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    static int Mod(int value, int n) => (value % n + n) % n;

    [MenuItem("Tools/Adaptive Virtual Texture/Validate Clipmap GPU Cache (Play Mode)")]
    public static void RunGpuChecks()
    {
        Require(Application.isPlaying, "Run in Play Mode to check Scene-camera isolation.");
        var c = UnityEngine.Object.FindObjectOfType<ClipmapTexture>(); var camera = Camera.main;
        Require(c != null && camera != null, "Clipmap and Main Camera are required.");
        string settings = JsonUtility.ToJson(c); Vector3 position = camera.transform.position;
        bool mode = ClipmapTexture.UseClipmap, colors = ClipmapTexture.DebugColors;
        var report = new StringBuilder();
        try
        {
            c.ApplyFeedbackQuality();
            Require(c.levelCount == 9, "Expected nine grades.");
            for (int i = 0; i < 9; i++) Require(Mathf.Approximately(c.resolution / c.LevelWorldSize(i), Constant.MaxTexelDensity / (float)(1 << i)), "Density mismatch L"+i);
            report.AppendLine("PASS: L0-L8 match Feedback Mip0-Mip8 densities.");
            c.enabled = false; c.enabled = true;
            c.compressBaseMap = false;
            c.matchFeedbackQuality = false; c.resolution = 256; c.levelCount = 4; c.tileSize = 64; c.finestWorldSize = 2; c.tilesPerFrame = 1;
            ClipmapTexture.UseClipmap = true; ClipmapTexture.DebugColors = false;
            camera.transform.position = new Vector3(1309.125f, position.y, 1069.125f);
            Step(c,camera);
            Require(c.LastBakedPageCount == 1 && c.DirtyPageCount == 63, "Initialization ignored page budget.");
            CheckTable(c,report,"first partial frame"); CompareRebake(c,report,"first partial frame");
            c.tilesPerFrame=256; Step(c,camera);
            Require(c.ReadyLevelCount == 4, "Not all pages completed.");
            CompareRebake(c,report,"initial"); CompareFeedback(c,report);
            camera.transform.position += new Vector3(0.0625f,0,0.0625f); Step(c,camera);
            Require(c.LastReassignedPageCount==0 && c.LastBakedPageCount==0, "Sub-page motion rebaked pages.");
            report.AppendLine("PASS: sub-page movement reuses all pages with zero baking.");
            Vector3[] deltas={new Vector3(0.5f,0,0),new Vector3(0.5f,0,0.5f),new Vector3(-0.5f,0,-0.5f),new Vector3(1.5f,0,-1),new Vector3(40,0,30),new Vector3(-4000,0,-4000),new Vector3(10000,0,10000)};
            for(int move=0;move<deltas.Length;move++)
            {
                var oldAnchors=new Vector2Int[c.levelCount];for(int i=0;i<c.levelCount;i++)oldAnchors[i]=c.GetPageAnchor(i);
                camera.transform.position+=deltas[move]; Step(c,camera);
                int expected=0,n=c.PagesPerAxis;
                for(int i=0;i<c.levelCount;i++)
                {
                    var d=c.GetPageAnchor(i)-oldAnchors[i];
                    expected+=n*n-Mathf.Max(0,n-Mathf.Abs(d.x))*Mathf.Max(0,n-Mathf.Abs(d.y));
                }
                Require(c.LastReassignedPageCount==expected && c.LastBakedPageCount==expected, "Incorrect dirty union or duplicate diagonal corner on move "+move);
                CheckTable(c,report,"move "+move);CompareRebake(c,report,"move "+move);
            }
            report.AppendLine("PASS: X/Y/diagonal/reverse moves, modulo wrap, teleport and both terrain boundaries preserve the overlap; no duplicate corner pages.");
            c.tilesPerFrame=1;camera.transform.position=new Vector3(700,position.y,900);Step(c,camera);
            Require(c.LastBakedPageCount==1 && c.DirtyPageCount==63,"Teleport exceeded budget or retained stale ready flags.");
            CheckTable(c,report,"budgeted teleport");CompareRebake(c,report,"budgeted teleport");
            // Move again before the old generation completes: any published page must belong to the new window.
            camera.transform.position+=new Vector3(0.5f,0,-0.5f);Step(c,camera);
            Require(c.LastBakedPageCount<=1,"Moving rebuild exceeded budget.");
            CheckTable(c,report,"moving partial generation");CompareRebake(c,report,"moving partial generation");
            c.tilesPerFrame=256;Step(c,camera);
            Vector2 center=c.FinestCenter;
            var go=new GameObject("Clipmap isolation probe");
            try { var sc=go.AddComponent<Camera>();sc.enabled=false;sc.cameraType=CameraType.SceneView;sc.transform.position=Vector3.one*-1000;Step(c,sc);Require(c.FinestCenter==center,"Scene camera moved cache."); }
            finally { UnityEngine.Object.DestroyImmediate(go); }
            c.finestWorldSize=4;Step(c,camera);
            Require(Mathf.Approximately(Shader.GetGlobalVectorArray("ClipmapPageWindows10")[0].z,2),"Debounce stretched old cache.");
            Field("_stableSince").SetValue(c,Time.realtimeSinceStartup-1);Step(c,camera);
            CheckTable(c,report,"partition resize");CompareRebake(c,report,"partition resize");
            c.levelCount=9;Step(c,camera);
            Require(Shader.GetGlobalVectorArray("ClipmapPageWindows10").Length==10 && c.ReadyLevelCount==9,"Nine levels or shader array truncated.");
            CheckTable(c,report,"nine levels + tail");CompareRebake(c,report,"nine levels + tail");
            c.tileSize=128;c.resolution=512;c.levelCount=2;Step(c,camera);
            CheckTable(c,report,"page size / resolution resize");CompareRebake(c,report,"page size / resolution resize");
            report.AppendLine("PASS: Scene isolation, partial-generation movement, parameter debounce and layout resize.");
            File.WriteAllText("Temp/clipmap-gpu-validation.txt",report.ToString());Debug.Log(report.ToString());
        }
        catch(Exception e) {report.AppendLine("FAIL: "+e);File.WriteAllText("Temp/clipmap-gpu-validation.txt",report.ToString());throw;}
        finally {camera.transform.position=position;c.enabled=false;JsonUtility.FromJsonOverwrite(settings,c);c.enabled=true;ClipmapTexture.UseClipmap=mode;ClipmapTexture.DebugColors=colors;}
    }
    static void Step(ClipmapTexture c,Camera camera)
    {
        Field("_advancedFrame").SetValue(c,-1);
        using(var cmd=new CommandBuffer()) {ClipmapTexture.RenderActive(cmd,camera);Graphics.ExecuteCommandBuffer(cmd);}
    }
    static byte[] Read(Texture rt,int slice)
    {
        var r=AsyncGPUReadback.Request(rt,0,0,rt.width,0,rt.height,slice,1);r.WaitForCompletion();
        Require(!r.hasError,"Readback failed");return r.GetData<byte>().ToArray();
    }
    static void CheckTable(ClipmapTexture c,StringBuilder report,string label)
    {
        int n=c.PagesPerAxis,invalid=0;
        for(int layer=0;layer<=c.levelCount;layer++)
        {
            byte[] table=Read(c.IndirectionTexture,layer);var anchor=c.GetPageAnchor(layer);
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                int i=(y*n+x)*4;var cpu=c.GetPageMapping(layer,x,y);
                Require(table[i]==Mod(anchor.x+x,n) && table[i+1]==Mod(anchor.y+y,n) && table[i+3]==layer,"Wrong GPU indirection slot: "+label);
                Require(table[i+2]==cpu.b,"CPU/GPU ready mismatch");
                if(layer==c.levelCount) Require(table[i+2]==255,"Tail missing");
                else if(table[i+2]==0)invalid++;
            }
        }
        Require(invalid==c.DirtyPageCount,"Published pending pages: "+label);
        report.AppendLine("PASS: "+label+" GPU page table, pending="+invalid);
    }
    static void CompareRebake(ClipmapTexture c,StringBuilder report,string label)
    {
        var ab=Get<RenderTexture>(c,"_base");var am=Get<RenderTexture>(c,"_mask");
        var eb=new RenderTexture(ab.descriptor);var em=new RenderTexture(am.descriptor);eb.Create();em.Create();
        try
        {
            var cs=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Mipipline/PipelineCore/AdaptiveVirtualTexture/Editor/ClipmapPageReference.compute");
            var windows=Shader.GetGlobalVectorArray("ClipmapPageWindows10");var origin=Get<Vector2>(c,"_bakedTerrainOrigin");
            int n=c.PagesPerAxis,pitch=c.tileSize+2*ClipmapTexture.PageBorder;
            cs.SetInt("PageWidth",c.tileSize);cs.SetInt("Border",ClipmapTexture.PageBorder);cs.SetInt("PerAxis",n);
            cs.SetVector("TerrainOrigin",new Vector4(origin.x,origin.y,0,0));
            cs.SetTexture(0,"_SplatMap",c.splatMap);cs.SetTexture(0,"_BaseMapArray",c.baseMapArray);cs.SetTexture(0,"_MaskMapArray",c.maskMapArray);
            cs.SetTexture(0,"ResultBase",eb);cs.SetTexture(0,"ResultMask",em);
            int maxError=0,samples=0;
            float maxFilterError=0;
            for(int layer=0;layer<=c.levelCount;layer++)
            {
                var anchor=c.GetPageAnchor(layer);cs.SetVector("Anchor",new Vector4(anchor.x,anchor.y,0,0));cs.SetInt("Layer",layer);cs.SetFloat("TexelSize",windows[layer].w);
                cs.Dispatch(0,(ab.width+7)/8,(ab.height+7)/8,1);
                var probes = new Vector4[c.resolution * c.resolution * 3];
                using (var buffer = new ComputeBuffer(probes.Length, 16))
                {
                    int probe = cs.FindKernel("ProbeSampling");
                    cs.SetVectorArray("ClipmapPageWindows10", windows);
                    cs.SetVector("ClipmapPageLayout", Shader.GetGlobalVector("ClipmapPageLayout"));
                    cs.SetVector("ClipmapSampling", Shader.GetGlobalVector("ClipmapSampling"));
                    cs.SetInt("ClipmapLevelCount", c.levelCount);
                    cs.SetTexture(probe, "ClipmapBase", ab); cs.SetTexture(probe, "ClipmapMask", am);
                    cs.SetTexture(probe, "ClipmapIndirection", c.IndirectionTexture);
                    cs.SetBuffer(probe, "ProbeResults", buffer);
                    cs.Dispatch(probe, (c.resolution + 7) / 8, (c.resolution + 7) / 8, 1);
                    buffer.GetData(probes);
                }
                foreach(bool mask in new[]{false,true})
                {
                    byte[] actual=Read(mask?am:ab,layer),expected=Read(mask?em:eb,layer);
                    for (int y=0;y<c.resolution;y++) for(int x=0;x<c.resolution;x++)
                    {
                        var map=c.GetPageMapping(layer,x/c.tileSize,y/c.tileSize);
                        int sample=(y*c.resolution+x)*3;
                        Require((probes[sample+2].x > 0.5f) == (map.b != 0), "Runtime sampler accepted stale page: " + label);
                        if(map.b==0) continue;
                        int px=map.r*pitch+ClipmapTexture.PageBorder+x%c.tileSize;
                        int py=map.g*pitch+ClipmapTexture.PageBorder+y%c.tileSize;
                        var filtered=probes[sample+(mask?1:0)];
                        for(int ch=0;ch<4;ch++)
                        {
                            float a=expected[(py*ab.width+px)*4+ch],b0=expected[(py*ab.width+px+1)*4+ch];
                            float a1=expected[((py+1)*ab.width+px)*4+ch],b1=expected[((py+1)*ab.width+px+1)*4+ch];
                            float reference=Mathf.Lerp(Mathf.Lerp(a,b0,0.375f),Mathf.Lerp(a1,b1,0.375f),0.375f)/255f;
                            maxFilterError=Mathf.Max(maxFilterError,Mathf.Abs(filtered[ch]-reference));
                        }
                    }
                    for(int y=0;y<n;y++)for(int x=0;x<n;x++)
                    {
                        var mapping=c.GetPageMapping(layer,x,y);if(mapping.b==0)continue;
                        for(int py=0;py<pitch;py++)for(int px=0;px<pitch;px++)for(int ch=0;ch<4;ch++)
                        {
                            int index=((mapping.g*pitch+py)*ab.width+mapping.r*pitch+px)*4+ch;
                            maxError=Math.Max(maxError,Math.Abs(actual[index]-expected[index]));samples++;
                        }
                    }
                }
            }
            Require(maxFilterError <= 1f/255f, label+" runtime indirection/border filter error: "+maxFilterError);
            Require(maxError==0,label+" page content/borders differ from full reference: "+maxError);
            report.AppendLine("PASS: "+label+", "+samples+" valid channel values INCLUDING page borders, max error=0; runtime filtered sampling within 1/255");
        }
        finally {UnityEngine.Object.DestroyImmediate(eb);UnityEngine.Object.DestroyImmediate(em);}
    }
    static void CompareFeedback(ClipmapTexture c,StringBuilder report)
    {
        var cs=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Mipipline/PipelineCore/AdaptiveVirtualTexture/ShaderLibrary/RenderingPhysicalPage.compute");
        var desc=new RenderTextureDescriptor(264,264,GraphicsFormat.R8G8B8A8_UNorm,0){dimension=TextureDimension.Tex2DArray,volumeDepth=1,enableRandomWrite=true};
        var b=new RenderTexture(desc);var m=new RenderTexture(desc);b.Create();m.Create();
        using(var buffer=new ComputeBuffer(6,4))
        {
            try
            {
                buffer.SetData(new uint[]{20,16,14,22,0,5});cs.SetBuffer(0,"_params",buffer);cs.SetInt("_targetIndex",0);
                cs.SetTexture(0,"_SplatMap",c.splatMap);cs.SetTexture(0,"_BaseMapArray",c.baseMapArray);cs.SetTexture(0,"_MaskMapArray",c.maskMapArray);
                cs.SetTexture(0,"_physicalPageBaseMapAtlas",b);cs.SetTexture(0,"_physicalPageMaskMapAtlas",m);cs.Dispatch(0,66,66,1);
                int maxError=0,pitch=c.tileSize+2*ClipmapTexture.PageBorder;
                foreach(bool mask in new[]{false,true})
                {
                    var atlas=Get<RenderTexture>(c,mask?"_mask":"_base");byte[] feedback=Read(mask?m:b,0),clip=Read(atlas,0);
                    for(int y=0;y<256;y++)for(int x=0;x<256;x++)for(int channel=0;channel<4;channel++)
                    {
                        var entry=c.GetPageMapping(0,x/c.tileSize,y/c.tileSize);
                        int px=entry.r*pitch+ClipmapTexture.PageBorder+x%c.tileSize,py=entry.g*pitch+ClipmapTexture.PageBorder+y%c.tileSize;
                        maxError=Math.Max(maxError,Math.Abs(feedback[((y+4)*264+x+4)*4+channel]-clip[(py*atlas.width+px)*4+channel]));
                    }
                }
                Require(maxError==0,"Feedback material mismatch: "+maxError);
                report.AppendLine("PASS: indirect paged base/mask match Feedback texels at equal density (max error=0).");
            }
            finally {UnityEngine.Object.DestroyImmediate(b);UnityEngine.Object.DestroyImmediate(m);}
        }
    }
}
