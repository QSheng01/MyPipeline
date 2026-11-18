using Unity.Profiling;
using UnityEditor;
using UnityEngine;


namespace SYArtToolkit
{

    public class DrawCallCalculateDynamic : DrawCallCalculate.TestInterface
    {

#if UNITY_2020_3_OR_NEWER
        private ProfilerRecorder batchesRecorder;
#endif
        private bool f = false;
        private int lastFrame = 0;
        private float testSetTime = 1;
        private float testTime;
        private int drawCall;
        private int firstDrawCall;
        private Camera testCamera;
        private GameObject testGameObject;
        private Camera testCopyCamera;
        private GameObject testCopyGameObject;
        private int mask = 0;
        private int maxDrawCall = 0;
        private int times = 0;
        private float averageDrawCall = 0;

        public DrawCallCalculateDynamic()
        {
#if UNITY_2020_3_OR_NEWER
            batchesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
#endif
        }
        ~DrawCallCalculateDynamic()
        {
#if UNITY_2020_3_OR_NEWER
            batchesRecorder.Dispose();
#endif
            Destroy();
        }

        public void Test()
        {
            GUILayout.Space(10);
            testCamera = EditorGUILayout.ObjectField(testCamera, typeof(Camera), true) as Camera;
            GUILayout.Space(10);
            testGameObject = EditorGUILayout.ObjectField(testGameObject, typeof(GameObject), true) as GameObject;
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            if (testTime > 0) GUILayout.Label("测试中:  " + testTime);
            else
            {
                testSetTime = EditorGUILayout.Slider("测试时间", testSetTime, 0.5f, 10.0f);
            }

            if (GUILayout.Button("开始测试") && testTime <= 0 && Application.isPlaying)
            {
                StartTest();

                Submit.ArtToolkitData.Instance.AddDrawCallTest(1);
            }

            GUILayout.Space(10);
            GUILayout.EndHorizontal();
            GUILayout.Label("实时Batches:        " + drawCall.ToString());
            GUILayout.Label("最大Batches:        " + maxDrawCall.ToString());
            GUILayout.Label("平均Batches:        " + averageDrawCall.ToString());
            if (testCopyGameObject == null || testCopyCamera == null) return;
            TestDrawCall();
        }

        private void TestDrawCall()
        {
            if (testTime <= 0)
            {
                Destroy();
                return;
            }

            testTime -= Time.deltaTime;
            if (Time.frameCount > lastFrame)
            {
                lastFrame = Time.frameCount;

#if UNITY_2020_3_OR_NEWER
                int value = (int)batchesRecorder.LastValue;
#else
                    int value = UnityStats.batches;
#endif
                if (f)
                {
                    testCopyCamera.cullingMask = mask;
                    drawCall = value - firstDrawCall;
                    if (drawCall < 0) drawCall = -drawCall;
                    if (drawCall != 0)
                    {
                        ++times;
                        averageDrawCall = (averageDrawCall * (times - 1) + drawCall) / times;
                        maxDrawCall = maxDrawCall > drawCall ? maxDrawCall : drawCall;
                    }
                }
                else
                {
                    firstDrawCall = value;
                    testCopyCamera.cullingMask = 0;
                }

                f = !f;
            }
        }

        public void Destroy()
        {
            if (testCopyGameObject != null) UnityEngine.Object.Destroy(testCopyGameObject);
            if (testCopyCamera != null)
            {
                UnityEngine.Object.Destroy(testCopyCamera.gameObject);
            }

            testTime = 0;

        }

        private void StartTest()
        {
            if (testCamera == null || testGameObject == null) return;
            testTime = testSetTime;
            if (testCopyCamera != null) UnityEngine.Object.Destroy(testCopyCamera.gameObject);
            if (testCopyGameObject != null) UnityEngine.Object.Destroy(testCopyGameObject);


            GameObject x = new GameObject();
            x.AddComponent<Camera>();
            testCopyCamera = Object.Instantiate(x.GetComponent<Camera>(), testCamera.transform.position,
                testCamera.transform.rotation);
            GameObject.Destroy(x);
            testCopyCamera.CopyFrom(testCamera);


            testCopyCamera.backgroundColor = Color.black;
            testCopyCamera.targetTexture = null;
            testCopyCamera.clearFlags = CameraClearFlags.Nothing;
            testCopyCamera.transform.position = new Vector3(testCopyCamera.transform.position.x + 5000,
                testCopyCamera.transform.position.y + 5000, testCopyCamera.transform.position.z + 5000);

            testCopyGameObject = Object.Instantiate(testGameObject, testGameObject.transform.position,
                testGameObject.transform.rotation);
            testCopyGameObject.transform.position = new Vector3(testGameObject.transform.position.x + 5000,
                testGameObject.transform.position.y + 5000, testGameObject.transform.position.z + 5000);
            drawCall = 0;

#if UNITY_2020_3_OR_NEWER
            firstDrawCall = (int)batchesRecorder.LastValue;
#else
                firstDrawCall = UnityStats.batches;
#endif

            mask = testCopyCamera.cullingMask;
            testCopyCamera.cullingMask = 0;
            lastFrame = Time.frameCount;
            f = true;

            maxDrawCall = 0;
            times = 0;
            averageDrawCall = 0;
        }




    }


}
