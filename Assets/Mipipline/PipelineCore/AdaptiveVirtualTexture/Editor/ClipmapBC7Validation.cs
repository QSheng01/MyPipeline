using System;
using System.IO;
using System.Text;
using System.Reflection;
using NoOvertime.VirtualTexture;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

public static class ClipmapBC7Validation
{
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Step(ClipmapTexture c)
    {
        typeof(ClipmapTexture).GetField("_advancedFrame",Flags).SetValue(c,-1);
        using(var cmd=new CommandBuffer()){ClipmapTexture.RenderActive(cmd,Camera.main);Graphics.ExecuteCommandBuffer(cmd);}
    }
    static byte[][] Read(Texture texture,int count)
    {
        RenderTexture decoded=null;
        try
        {
            Texture input=texture;
            if(texture is Texture2DArray array && array.format==TextureFormat.BC7)
            {
                decoded=new RenderTexture(texture.width,texture.height,0,GraphicsFormat.R8G8B8A8_UNorm)
                    {dimension=TextureDimension.Tex2DArray,volumeDepth=count,enableRandomWrite=true};decoded.Create();
                var cs=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Mipipline/PipelineCore/AdaptiveVirtualTexture/Editor/ClipmapBC7Validation.compute");
                cs.SetInt("Width",texture.width);cs.SetTexture(0,"Compressed",texture);cs.SetTexture(0,"Decoded",decoded);
                for(int i=0;i<count;i++){cs.SetInt("Slice",i);cs.Dispatch(0,(texture.width+7)/8,(texture.height+7)/8,1);}
                input=decoded;
            }
            var result=new byte[count][];
            for(int i=0;i<count;i++)
            {
                var request=AsyncGPUReadback.Request(input,0,0,input.width,0,input.height,i,1);request.WaitForCompletion();
                Require(!request.hasError,"BC7 validation GPU readback failed.");result[i]=request.GetData<byte>().ToArray();
            }
            return result;
        }
        finally{if(decoded!=null)UnityEngine.Object.DestroyImmediate(decoded);}
    }
    static void Equal(byte[][] a,byte[][] b,string name)
    {
        Require(a.Length==b.Length,name+" slice count changed.");
        for(int i=0;i<a.Length;i++)for(int j=0;j<a[i].Length;j++)Require(a[i][j]==b[i][j],name+" differs at slice "+i+", byte "+j);
    }
    static uint Bits(uint[] words,int block,ref int offset,int count)
    {
        int word=offset/32,shift=offset%32;
        ulong v=words[block*4+word];
        if(word<3)v|=(ulong)words[block*4+word+1]<<32;
        offset+=count;return (uint)((v>>shift)&((1UL<<count)-1));
    }
    static void ValidateBitstream(StringBuilder report)
    {
        var src=new Texture2DArray(16,16,1,TextureFormat.RGBA32,false,true);
        var colors=new Color32[256];var random=new System.Random(719);
        for(int y=0;y<16;y++)for(int x=0;x<16;x++)
        {
            int block=(y/4)*4+x/4,t=((y%4)*4+x%4)*17;
            if(block%4==0)colors[y*16+x]=new Color32(17,99,213,61);
            else if(block%4==1)colors[y*16+x]=new Color32((byte)t,(byte)(255-t),80,200);
            else if(block%4==2)colors[y*16+x]=new Color32(67,89,123,(byte)t);
            else colors[y*16+x]=new Color32((byte)random.Next(256),(byte)random.Next(256),(byte)random.Next(256),(byte)random.Next(256));
        }
        src.SetPixels32(colors,0);src.Apply();
        var blocks=new RenderTexture(4,4,0,GraphicsFormat.R32G32B32A32_UInt){enableRandomWrite=true};blocks.Create();
        var dst=new Texture2DArray(32,32,2,TextureFormat.BC7,false,true);dst.Apply(false,true);
        try
        {
            var encoder=Resources.Load<ComputeShader>("ClipmapBC7");encoder.SetInt("PagePitch",16);encoder.SetTexture(0,"Source",src);encoder.SetTexture(0,"Blocks",blocks);encoder.Dispatch(0,1,1,1);
            uint[] words=new uint[64];
            using(var buffer=new ComputeBuffer(16,16))
            {
                var reader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Mipipline/PipelineCore/AdaptiveVirtualTexture/Editor/ClipmapBC7Validation.compute");
                int kernel=reader.FindKernel("ReadBlocks");reader.SetTexture(kernel,"EncodedBits",blocks);reader.SetBuffer(kernel,"BlockWords",buffer);
                reader.Dispatch(kernel,1,1,1);buffer.GetData(words);
            }
            Graphics.CopyTexture(blocks,0,0,0,0,4,4,dst,1,0,8,8);
            byte[] gpu=Read(dst,2)[1];
            int[] weights={0,4,9,13,17,21,26,30,34,38,43,47,51,55,60,64};
            for(int block=0;block<16;block++)
            {
                int bit=0;Require(Bits(words,block,ref bit,7)==64,"Invalid BC7 mode-6 prefix at block "+block+": "+words[block*4].ToString("X8"));
                int[] e0=new int[4],e1=new int[4];
                for(int ch=0;ch<4;ch++){e0[ch]=(int)Bits(words,block,ref bit,7);e1[ch]=(int)Bits(words,block,ref bit,7);}
                int p0=(int)Bits(words,block,ref bit,1),p1=(int)Bits(words,block,ref bit,1);
                for(int ch=0;ch<4;ch++){e0[ch]=e0[ch]*2+p0;e1[ch]=e1[ch]*2+p1;}
                for(int pixel=0;pixel<16;pixel++)
                {
                    int w=weights[Bits(words,block,ref bit,pixel==0?3:4)];
                    int x=8+(block%4)*4+pixel%4,y=8+(block/4)*4+pixel/4;
                    for(int ch=0;ch<4;ch++)Require(gpu[(y*32+x)*4+ch]==((64-w)*e0[ch]+w*e1[ch]+32)/64,"CPU specification decoder differs from hardware BC7.");
                }
                Require(bit==128,"BC7 block length mismatch.");
            }
            report.AppendLine("PASS: all 128 block bits, RGBA endpoints/p-bits/anchor indices, nonzero slice and region offset verified by independent CPU decode vs GPU on flat/alpha/opposing-color/random blocks.");
        }
        finally{UnityEngine.Object.DestroyImmediate(src);UnityEngine.Object.DestroyImmediate(blocks);UnityEngine.Object.DestroyImmediate(dst);}
    }
    [MenuItem("Tools/Adaptive Virtual Texture/Validate GPU BC7 (Play Mode)")]
    public static void Run()
    {
        Require(Application.isPlaying,"Run in Play Mode.");
        var c=UnityEngine.Object.FindObjectOfType<ClipmapTexture>();Require(c!=null && c.CompressionSupported,"D3D11 BC7 path not supported.");
        string settings=JsonUtility.ToJson(c);Vector3 camera=Camera.main.transform.position;
        bool mode=ClipmapTexture.UseClipmap,debug=ClipmapTexture.DebugColors;
        var report=new StringBuilder();
        try
        {
            ValidateBitstream(report);
            ClipmapTexture.UseClipmap=true;ClipmapTexture.DebugColors=false;
            c.enabled=false;c.enabled=true;c.matchFeedbackQuality=false;c.resolution=256;c.finestWorldSize=2;c.levelCount=4;c.tileSize=64;c.tilesPerFrame=256;
            Camera.main.transform.position=new Vector3(1309.125f,camera.y,1069.125f);
            c.compressBaseMap=false;Step(c);
            var referenceBase=Read(c.BaseCache,5);var referenceMask=Read(c.MaskCache,5);
            c.compressBaseMap=true;Step(c);Require(c.CompressionActive,"Compression did not activate.");
            var encoded=Read(c.BaseCache,5);var mask=Read(c.MaskCache,5);
            Equal(referenceMask,mask,"Uncompressed Mask");
            report.AppendLine("PASS: Base format=BC7, Mask=RGBA8; all Mask channels byte-identical to uncompressed mode.");
            long pixels=0;double[] sum=new double[4],squared=new double[4];int[] maximum=new int[4];
            for(int layer=0;layer<5;layer++)for(int p=0;p<referenceBase[layer].Length;p+=4)
            {
                pixels++;
                for(int ch=0;ch<4;ch++){int diff=Math.Abs(referenceBase[layer][p+ch]-encoded[layer][p+ch]);sum[ch]+=diff;squared[ch]+=(double)diff*diff;maximum[ch]=Math.Max(maximum[ch],diff);}
            }
            for(int ch=0;ch<4;ch++)
            {
                double rmse=Math.Sqrt(squared[ch]/pixels),psnr=rmse>0?20*Math.Log10(255/rmse):100;
                report.AppendLine("Base "+"RGBA"[ch]+": MAE="+(sum[ch]/pixels).ToString("F3")+"/255, max="+maximum[ch]+", PSNR="+psnr.ToString("F2")+" dB");
                Require(psnr>=28,"BC7 material error exceeds validation floor in channel "+ch+": "+psnr+" dB");
            }
            Step(c);Require(c.LastBakedPageCount==0,"Stationary compression rebaked pages.");
            Camera.main.transform.position+=new Vector3(0.5f,0,0.5f);Step(c);
            Require(c.LastBakedPageCount>0 && c.LastBakedPageCount<64,"Move did not update only dirty pages.");
            int updated=c.LastBakedPageCount;
            var moved=Read(c.BaseCache,5);var movedMask=Read(c.MaskCache,5);
            c.enabled=false;c.enabled=true;Step(c);
            Equal(moved,Read(c.BaseCache,5),"Incremental BC7 vs full rebuild");
            Equal(movedMask,Read(c.MaskCache,5),"Incremental Mask vs full rebuild");
            report.AppendLine("PASS: diagonal move compressed "+updated+" dirty pages; hardware-decoded BC7 and Mask exactly match a fresh full rebuild.");
            c.tilesPerFrame=1;Camera.main.transform.position+=new Vector3(40,0,30);Step(c);
            Require(c.LastBakedPageCount==1 && c.DirtyPageCount==63,"Compressed teleport ignored budget or published invalid pages.");
            report.AppendLine("PASS: compressed teleport honors one-page budget, 63 pages remain invalid for coarse fallback.");
            c.tilesPerFrame=256;c.compressBaseMap=false;Step(c);
            Require(!c.CompressionActive && c.ReadyLevelCount==4,"RGBA8 toggle failed.");
            c.compressBaseMap=true;Step(c);Require(c.CompressionActive && c.ReadyLevelCount==4,"BC7 toggle failed.");
            report.AppendLine("PASS: RGBA8/BC7 toggling rebuilds and rebinds the correct texture type.");
        }
        catch(Exception e){report.AppendLine("FAIL: "+e);throw;}
        finally
        {
            File.WriteAllText("Temp/clipmap-bc7-validation.txt",report.ToString());
            Camera.main.transform.position=camera;c.enabled=false;JsonUtility.FromJsonOverwrite(settings,c);c.enabled=true;
            ClipmapTexture.UseClipmap=mode;ClipmapTexture.DebugColors=debug;
        }
    }
}