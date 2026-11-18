using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SYArtToolkit
{


    public class DrawCallCalculateStatic : DrawCallCalculate.TestInterface
    {
#if UNITY_2020_3_OR_NEWER
        private ProfilerRecorder batchesRecorder;
#endif
        private GameObject gameObjectPri;
        private GameObject gameObject;
        private float testSetTime = 0.5f;
        private float testTime = 0;
        private int drawCall = 0;
        private int maxDrawCall = 0;
        private float averageDrawCall = 0;
        private int times = 0;

        public DrawCallCalculateStatic()
        {
#if UNITY_2020_3_OR_NEWER
            batchesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
#endif
        }

        ~DrawCallCalculateStatic()
        {
#if UNITY_2020_3_OR_NEWER
            batchesRecorder.Dispose();
#endif
        }

        public void Init(GameObject gameObject, float testTime, Vector3 position = new Vector3())
        {
            if (gameObject == null) return;
            this.gameObject = gameObject;
            maxDrawCall = 0;
            averageDrawCall = 0;
            times = 0;
            drawCall = 0;
            this.testTime = testTime;
        }

        public bool PerFrameUpdate()
        {
            if (testTime <= 0)
            {
                return true;
            }
            testTime -= Time.deltaTime;
#if UNITY_2020_3_OR_NEWER
            drawCall = (int)batchesRecorder.LastValue;
#else
                drawCall = UnityStats.batches;
#endif
            ++times;
            averageDrawCall = (averageDrawCall * (times - 1) + drawCall) / times;
            maxDrawCall = maxDrawCall > drawCall ? maxDrawCall : drawCall;
            return false;
        }


        public (int a, int m) GetDrawCallValue()
        {
            return ((int)averageDrawCall, maxDrawCall);
        }

        public void Destroy()
        {
            if (gameObject) Object.Destroy(gameObject);
            drawCall = 0;
            testTime = 0;
            maxDrawCall = 0;
            averageDrawCall = 0;
            times = 0;
        }








        public void Test()
        {
            GUILayout.Space(10);
            gameObjectPri = EditorGUILayout.ObjectField(gameObjectPri, typeof(GameObject), false) as GameObject;
            PerFrameUpdate();
            if (testTime <= 0) { Destroy(); }
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (testTime > 0) GUILayout.Label("测试中:  " + testTime);
            else
            {
                testSetTime = EditorGUILayout.Slider("测试时间: ", testSetTime, 0.5f, 10.0f);
            }

            if (GUILayout.Button("开始测试") && testTime <= 0 && Application.isPlaying)
            {
                Submit.ArtToolkitData.Instance.AddDrawCallTest(1);
                var g = Object.Instantiate(gameObjectPri);
                Init(g, testSetTime);
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.Label("实时Batches:        " + drawCall.ToString());
            GUILayout.Label("最大Batches:        " + maxDrawCall.ToString());
            GUILayout.Label("平均Batches:        " + averageDrawCall.ToString());
            GUILayout.Space(10);
            GUILayout.Label("请新建一个场景并且测试,主相机对准需要测试的物体");
            GUILayout.Label("该测试能准确测试物体的Batches");
            if (GUILayout.Button("点击这里一键修改相机选项(非新建测试场景禁止修改)"))
            {
                if (Camera.main == null && testTime > 0 && Application.isPlaying == false)
                {
                    Debug.Log("相机为空——正在测试中——编辑模式");
                    return;
                }
                Camera.main.clearFlags = CameraClearFlags.Nothing;
                Camera.main.backgroundColor = Color.clear;
            }
        }












    }
}
