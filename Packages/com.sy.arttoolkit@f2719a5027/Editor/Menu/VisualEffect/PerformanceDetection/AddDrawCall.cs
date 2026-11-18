using System.Collections.Generic;
using Unity.Profiling;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SYArtToolkit
{

    class AddOverDrawTest
    {
        private bool openTest;
        public bool OpenTest
        {
            get { return openTest; }
        }

        private Camera cameraMain;
        private bool reset;
        private int mask;//加入   //重置时间
        private List<GameObject> lastGameObjects = new List<GameObject>();

        private ReorderableList reorderableList;
        private ReorderableList emptyList;
        private List<byte> bytes;
        private List<byte> empty;
        private ArtToolBoxGlobalData data;

        //Overdraw
        private Shader shader;
        private Camera cameraOverDraw;
        protected float overdrawGlobal;
        protected float maxOverdrawGlobal;
        protected RenderTexture rt;
        private ComputeShader computeShader;
        private ComputeBuffer resultBuffer;
        private ComputeBuffer resultBuffer2;
        private int[] inputData;
        private int[] resultData;
        private int[] resultData2;

#if UNITY_2020_3_OR_NEWER
        private ProfilerRecorder batchesRecorder;
#endif

        public AddOverDrawTest()
        {
#if UNITY_2020_3_OR_NEWER
            batchesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
#endif
            openTest = false;
            shader = Shader.Find("Hidden/ArtToolkit/ReplacementShader");
            mask = 27;
            computeShader = computeShader = Resources.Load<ComputeShader>("ComputerDrawCall");
            reset = true;
            bytes = new List<byte>();
            bytes.Add(0);
            bytes.Add(0);
            empty = new List<byte>();
            empty.Add(0);
            reorderableList = new ReorderableList(bytes, typeof(byte), false, true, false, false);
            reorderableList.drawHeaderCallback = (Rect rect) =>
            {
                openTest = EditorGUI.ToggleLeft(new Rect(rect.x, rect.y, rect.width / 2, rect.height), "开启渲染测试", openTest);
                EditorGUILayout.EndFoldoutHeaderGroup();
                if (openTest == false || !EditorApplication.isPlaying) Destroy();
                EditorGUI.BeginChangeCheck();
                cameraMain = EditorGUI.ObjectField(new Rect(rect.x + rect.width / 2, rect.y, rect.width / 2, rect.height), cameraMain, typeof(Camera), true) as Camera;
                if (EditorGUI.EndChangeCheck())
                {
                    reset = true;
                }
            };
            reorderableList.drawElementCallback = (Rect rect, int index, bool a, bool b) =>
            {
                if (index == 0)
                {
                    mask = EditorGUI.IntSlider(rect, "Mask", mask, 0, 31);
                }
                else
                {
                    GUI.Label(new Rect(rect.x, rect.y, rect.width / 2, rect.height), "渲染检测:");
                    if (data == null)
                    {
                        GUI.Label(new Rect(rect.x + rect.width / 2, rect.y, rect.width / 2, rect.height), overdrawGlobal + " / " + maxOverdrawGlobal);
                    }
                    else
                    {

                        GUI.Label(new Rect(rect.x + rect.width / 2, rect.y, rect.width / 2, rect.height), FormatMax(overdrawGlobal, data.presupposeSelect.overdraw)
                            + " / " + FormatMax(maxOverdrawGlobal, data.presupposeSelect.overdraw), PerformanceWindow.style);
                    }
                }
            };
            emptyList = new ReorderableList(empty, typeof(byte), false, true, false, true);
            emptyList.drawHeaderCallback = (Rect rect) =>
            {
                openTest = EditorGUI.ToggleLeft(new Rect(rect.x, rect.y, rect.width / 2, rect.height), "开启渲染测试", openTest);
                EditorGUILayout.EndFoldoutHeaderGroup();
                if (openTest == false || !EditorApplication.isPlaying) Destroy();
                EditorGUI.BeginChangeCheck();
                cameraMain = EditorGUI.ObjectField(new Rect(rect.x + rect.width / 2, rect.y, rect.width / 2, rect.height), cameraMain, typeof(Camera), true) as Camera;
                if (EditorGUI.EndChangeCheck())
                {
                    reset = true;
                }
            };
            emptyList.elementHeight = 0;
        }

        ~AddOverDrawTest()
        {
#if UNITY_2020_3_OR_NEWER
            batchesRecorder.Dispose();
#endif
        }


        private void Init()//相机，启动模式，物体存在且至少一个不为空=>>  相机改变或者物体改变
        {//新建两个相机/
         //双帧差的绝对值
            Destroy();
            rt = new RenderTexture(cameraMain.pixelWidth, cameraMain.pixelHeight, 0);
            GameObject x0 = new GameObject();
            cameraOverDraw = x0.AddComponent<Camera>();
            x0.transform.parent = cameraMain.transform;
            x0.transform.localPosition = new Vector3(0, 0, 0);
            cameraOverDraw.CopyFrom(cameraMain);

            cameraOverDraw.backgroundColor = Color.clear;
            cameraOverDraw.clearFlags = CameraClearFlags.Color;
            cameraOverDraw.targetTexture = rt;
            cameraOverDraw.SetReplacementShader(shader, "");
            int width = cameraOverDraw.pixelWidth;
            int height = cameraOverDraw.pixelHeight;
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
            maxOverdrawGlobal = 0;
            reset = false;

        }


        private string FormatMax(float x, float max)
        {
            string front;
            if (x > max) front = "<color=red>";
            else front = "<color=green>";
            return front + ((int)(x * 1000) / 1000.0f).ToString() + "</color>";
        }

        private bool CounldGo()
        {
            if (!openTest || cameraMain == null || !EditorApplication.isPlaying) return false;
            return true;
        }

        private void CheckAndSummit(List<GameObject> gameObjects)
        {
            bool update = false;
            if (lastGameObjects.Count != gameObjects.Count)
            {
                update = true;
            }
            else
            {
                var li = lastGameObjects.GetEnumerator();
                var gi = gameObjects.GetEnumerator();
                while (li.MoveNext() && gi.MoveNext())
                {
                    if (li.Current != gi.Current)
                    {
                        update = true;
                        break;
                    }
                }
            }
            if (update)
            {
                lastGameObjects.Clear();
                foreach (var gameObject in gameObjects)
                {
                    lastGameObjects.Add(gameObject);
                }
                Submit.ArtToolkitData.Instance.AddDrawCallTest(lastGameObjects.Count);
                maxOverdrawGlobal = 0;
            }

        }


        public void UpdatePerFrame(List<GameObject> gameObjects, ArtToolBoxGlobalData data)
        {
            this.data = data;
            if (CounldGo())
            {
                if (reset)
                {
                    Init();
                }
                List<int> masks = new List<int>();
                CheckAndSummit(gameObjects);
                foreach (var g in gameObjects)
                {
                    if (g == null)
                    {
                        masks.Add(0);
                        continue;
                    }
                    masks.Add(g.layer);
                    var ts = g.GetComponentsInChildren<Transform>();
                    foreach (Transform t in ts)
                        t.gameObject.layer = mask;

                }
                cameraOverDraw.cullingMask = 1 << mask;
                cameraOverDraw.Render();



                resultBuffer.SetData(inputData);
                resultBuffer2.SetData(inputData);
                int kernel = computeShader.FindKernel("CSMain");
                computeShader.SetInt("BufferSize", cameraOverDraw.pixelWidth / 32);
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

                overdrawGlobal = pixels / (float)(rt.width * rt.height);
                maxOverdrawGlobal = Mathf.Max(overdrawGlobal, maxOverdrawGlobal);


                var mItor = masks.GetEnumerator();
                var gItor = gameObjects.GetEnumerator();
                while (mItor.MoveNext() && gItor.MoveNext())
                {
                    if (gItor.Current == null) continue;
                    var ts = gItor.Current.GetComponentsInChildren<Transform>();
                    foreach (var t in ts)
                    {
                        t.gameObject.layer = mItor.Current;
                    }
                }


            }


            if (openTest)
            {
                reorderableList.DoLayoutList();
            }
            else
            {
                emptyList.DoLayoutList();
            }
        }


        public void Destroy()
        {

            if (cameraOverDraw != null)
            {
                if (cameraOverDraw.gameObject != null)
                {

                    Object.DestroyImmediate(cameraOverDraw.gameObject);
                }
                Object.DestroyImmediate(cameraOverDraw);
            }
            if (rt != null)
            {
                Object.DestroyImmediate(rt);
            }
            reset = true;
            if (resultBuffer != null)
            {
                resultBuffer.Release();
            }
            if (resultBuffer2 != null)
            {
                resultBuffer2.Release();
            }
        }

    }











}