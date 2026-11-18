using System.Collections.Generic;
using UnityEngine;

namespace SYArtToolkit
{
    public class OverDrawCalculateBase
    {
        protected GameObject gameObject;
        protected Camera camera;
        protected Material replacement;
        protected float timeTest = 0;
        protected RenderTexture rt;
        protected float overdrawLocal = 0.0f;
        protected float averageOverdrawLocal = 0.0f;
        protected float maxOverdrawLocal = 0.0f;

        protected float overdrawGlobal = 0.0f;
        protected float averageOverdrawGlobal = 0.0f;
        protected float maxOverdrawGlobal = 0.0f;
        public OverDrawCalculateBase()
        {
            replacement = new Material(Shader.Find("Hidden/ArtToolkit/ReplacementShader"));
            computeShader = Resources.Load<ComputeShader>("ComputerDrawCall");
        }

        private ComputeShader computeShader;
        private ComputeBuffer resultBuffer;
        private ComputeBuffer resultBuffer2;
        private int[] inputData;
        private int[] resultData;
        private int[] resultData2;
        private int times = 0;
        private Color backgroundColor;
        private CameraClearFlags clearFlags;
        private RenderTexture targetTexture;




        public void Init(GameObject gameObject, Camera camera, float time)
        {
            rt = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 0);
            backgroundColor = camera.backgroundColor;
            camera.backgroundColor = Color.clear;
            clearFlags = camera.clearFlags;
            camera.clearFlags = CameraClearFlags.Color;
            targetTexture = camera.targetTexture;
            camera.targetTexture = rt;


            this.camera = camera;
            this.gameObject = gameObject;

            ReplaceMaterial(GetChilid(gameObject.transform));
            int width = camera.pixelWidth;
            int height = camera.pixelHeight;
            int threadX = width / 32;
            int threadY = height / 32;
            inputData = new int[threadX * threadY];
            for (int i = 0; i < inputData.Length; i++)
                inputData[i] = 0;
            resultData = new int[threadX * threadY];
            resultBuffer = new ComputeBuffer(resultData.Length, 4);
            resultData2 = new int[threadX * threadY];
            resultBuffer2 = new ComputeBuffer(resultData2.Length, 4);
            overdrawGlobal = 0;
            averageOverdrawGlobal = 0;
            maxOverdrawGlobal = 0;
            averageOverdrawLocal = 0;
            maxOverdrawLocal = 0;
            times = 0;
            timeTest = time;
        }

        public bool PerFrameUpdate()//逻辑还要写很久
        {
            if (camera == null || gameObject == null) return true;
            if (timeTest <= 0) return true;
            timeTest -= Time.deltaTime;
            resultBuffer.SetData(inputData);
            resultBuffer2.SetData(inputData);
            int kernel = computeShader.FindKernel("CSMain");
            computeShader.SetInt("BufferSize", camera.pixelWidth / 32);
            computeShader.SetTexture(kernel, "OverDraw", rt);
            computeShader.SetBuffer(kernel, "Output", resultBuffer);
            computeShader.SetBuffer(kernel, "Output1", resultBuffer2);

            // Summing up the fragments
            int xGroups = rt.width / 32;
            int yGroups = rt.height / 32;
            computeShader.Dispatch(kernel, xGroups, yGroups, 1);
            resultBuffer.GetData(resultData);
            resultBuffer2.GetData(resultData2);

            int pixels = 0;
            int sum = 0;
            foreach (var x in resultData)
            {
                pixels += x;
            }

            foreach (var x in resultData2)
            {
                sum += x;
            }

            ++times;
            overdrawGlobal = pixels / (float)(rt.width * rt.height);
            averageOverdrawGlobal = (averageOverdrawGlobal * (times - 1) + overdrawGlobal) / times;
            maxOverdrawGlobal = Mathf.Max(overdrawGlobal, maxOverdrawGlobal);

            overdrawLocal = sum != 0 ? pixels / (float)sum : 0;
            averageOverdrawLocal = (averageOverdrawLocal * (times - 1) + overdrawLocal) / times;
            maxOverdrawLocal = Mathf.Max(overdrawLocal, maxOverdrawLocal);

            return false;
        }

        public void DestroyBase()
        {

            if (rt)
            {
                rt.Release();
                Object.Destroy(rt);
                rt = null;
            }
            timeTest = 0;
            if (resultBuffer != null)
            {
                resultBuffer.Release();
                resultBuffer = null;
            }
            if (resultBuffer2 != null)
            {
                resultBuffer2.Release();
                resultBuffer2 = null;
            }
        }
        private void ReplaceMaterial(List<Transform> ts)
        {
            if (ts == null) return;
            foreach (var t in ts)
            {
                var r = t.GetComponent<Renderer>();
                if (r) r.material = replacement;
                var rr = t.GetComponent<ParticleSystemRenderer>();
                if (rr) rr.material = replacement;
            }
        }

        private List<Transform> GetChilid(Transform x)
        {
            List<Transform> res = new List<Transform>();
            if (x == null) return res;
            res.Add(x);
            for (int i = 0; i < x.childCount; ++i)
            {
                GetChilid(x.GetChild(i));
            }

            return res;
        }

    }

}