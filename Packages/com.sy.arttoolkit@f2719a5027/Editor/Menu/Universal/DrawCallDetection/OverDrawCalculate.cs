using UnityEditor;
using UnityEngine;



namespace SYArtToolkit
{
    public class OverDrawCalculate : OverDrawCalculateBase, DrawCallCalculate.TestInterface
    {
        private GameObject testGameObject;
        private Camera testCamera;
        private float testSetTime = 0;

        public void Test()
        {
            GUILayout.Label("请在运行场景下测试");
            GUILayout.Space(10);
            testCamera = EditorGUILayout.ObjectField(testCamera, typeof(Camera), true) as Camera;
            GUILayout.Space(10);
            testGameObject = EditorGUILayout.ObjectField(testGameObject, typeof(GameObject), true) as GameObject;
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (timeTest <= 0)
            {
                testSetTime = EditorGUILayout.Slider("测试时间：", testSetTime, 0.5f, 10.0f);
            }
            else GUILayout.Label("测试剩余时间：" + timeTest);

            if (GUILayout.Button("开始测试") && Application.isPlaying && timeTest <= 0)
            {
                Submit.ArtToolkitData.Instance.AddDrawCallTest(1);
                if (testGameObject == null || testCamera == null) return;
                if (camera != null) UnityEngine.Object.Destroy(camera.gameObject);
                if (gameObject != null) UnityEngine.Object.Destroy(gameObject);
                rt = new RenderTexture(testCamera.pixelWidth, testCamera.pixelHeight, 0);
                GameObject x = new GameObject();
                x.AddComponent<Camera>();
                var TestCopyCamera = Object.Instantiate(x.GetComponent<Camera>(), testCamera.transform.position,
                    testCamera.transform.rotation);
                GameObject.Destroy(x);
                TestCopyCamera.CopyFrom(testCamera);

                TestCopyCamera.backgroundColor = Color.clear;
                TestCopyCamera.clearFlags = CameraClearFlags.Color;
                TestCopyCamera.targetTexture = rt;
                TestCopyCamera.transform.position = new Vector3(TestCopyCamera.transform.position.x + 5000,
                    TestCopyCamera.transform.position.y + 5000, TestCopyCamera.transform.position.z + 5000);

                var TestCopyGameObject = Object.Instantiate(testGameObject, testGameObject.transform.position,
                    testGameObject.transform.rotation);
                TestCopyGameObject.transform.position = new Vector3(testGameObject.transform.position.x + 5000,
                    testGameObject.transform.position.y + 5000, testGameObject.transform.position.z + 5000);

                Init(TestCopyGameObject, TestCopyCamera, testSetTime);
            }

            GUILayout.EndHorizontal();


            PerFrameUpdate();
            if (timeTest <= 0) Destroy();
            GUILayout.Space(10);
            GUILayout.Label("实时全局OverDraw:  " + overdrawGlobal);
            GUILayout.Label("平均全局OverDraw:   " + averageOverdrawGlobal);
            GUILayout.Label("最大全局OverDraw:   " + maxOverdrawGlobal);
            GUILayout.Space(10);
            GUILayout.Label("实时局部OverDraw:  " + overdrawLocal);
            GUILayout.Label("平均局部OverDraw:   " + averageOverdrawLocal);
            GUILayout.Label("最大局部OverDraw:   " + maxOverdrawLocal);
        }



        public void Destroy()
        {
            if (gameObject) Object.Destroy(gameObject.gameObject);
            if (camera) Object.Destroy(camera.gameObject);
            DestroyBase();
        }

    }

}
