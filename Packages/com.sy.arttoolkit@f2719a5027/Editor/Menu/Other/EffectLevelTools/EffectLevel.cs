using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SYArtToolkit
{


    public class EffectLevel : ToolInterface
    {
        private string[] QualityStrs = { "低", "中", "高" };

        private GameObject gameObj;
        EffectClass effectClass;
        private bool openEditor;

        //���� renderer

        //private void 

        public EffectLevel()
        {
            effectClass = new EffectClass();
            InitGameObject();
        }


        private string LowName = "_L";
        private string MidName = "_M";
        private string HighName = "_H";
        private bool reset = false;

        public override void InterfaceShow(bool atThisWindow, Rect position)
        {
            EditorGUI.BeginChangeCheck();
            gameObj = EditorGUILayout.ObjectField(gameObj, typeof(GameObject), true) as GameObject;
            if (EditorGUI.EndChangeCheck())
            {
                InitGameObject();
            }

            if (GUILayout.Button(openEditor ? "关闭编辑" : "开启编辑"))
            {
                openEditor = !openEditor;
            }


            if (GUILayout.Button("播放低级特效"))
            {
                EditorQualityPlay(1);
            }


            if (GUILayout.Button("播放中级特效"))
            {

                EditorQualityPlay(2);
            }

            if (GUILayout.Button("播放高级特效"))
            {

                EditorQualityPlay(3);
            }

            EditorGUILayout.BeginHorizontal();

            LowName = EditorGUILayout.TextField(LowName);
            MidName = EditorGUILayout.TextField(MidName);
            HighName = EditorGUILayout.TextField(HighName);
            EditorGUILayout.EndHorizontal();


            if (GUILayout.Button("生成预制体"))
            {
                if (gameObj == null) return;

                var o = PrefabUtility.GetCorrespondingObjectFromSource(gameObj);
                string prefabPath = AssetDatabase.GetAssetPath(o);
                if (string.IsNullOrEmpty(prefabPath))
                {
                    prefabPath = EditorUtility.OpenFolderPanel("Select Folder", Application.dataPath, "");
                    if (string.IsNullOrEmpty(prefabPath))
                    {
                        return;
                    }
                }
                else
                {
                    prefabPath = Path.GetDirectoryName(prefabPath);
                }

                var saveObj = Object.Instantiate(gameObj);
                var obj = SavePrefab(saveObj, prefabPath, gameObj.name, HighName);
                if (obj != null)
                {
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);

                }

                var getRendererToTransform = GetRendererToTransform(saveObj);

                foreach (var t in getRendererToTransform.mid)
                {
                    if (t.childCount == 0)
                        Object.DestroyImmediate(t.gameObject);
                    else
                    {
                        Object.DestroyImmediate(t.GetComponent<Renderer>());
                        Object.DestroyImmediate(t.GetComponent<ParticleSystem>());
                    }
                }

                SavePrefab(saveObj, prefabPath, gameObj.name, MidName);

                foreach (var t in getRendererToTransform.low)
                {
                    if (t.childCount == 0)
                        Object.DestroyImmediate(t.gameObject);
                    else
                    {
                        Object.DestroyImmediate(t.GetComponent<Renderer>());
                        Object.DestroyImmediate(t.GetComponent<ParticleSystem>());
                    }
                }

                SavePrefab(saveObj, prefabPath, gameObj.name, LowName);
                Object.DestroyImmediate(saveObj);
            }









            EditorApplication.hierarchyWindowItemOnGUI = delegate (int instanceID, Rect selectionRect)
            {
                if (SYArtToolkit.WindowControl.Instance == null) EditorApplication.hierarchyWindowItemOnGUI = null;
                if (!openEditor) return;
                if (gameObj == null)
                    return;
                //��Hierarchy��ͼ��ѡ��һ����Դ
                GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
                if (gameObject == null || !IsChildObjt(gameObject.transform))
                {
                    return;
                }

                var render = gameObject.GetComponent<Renderer>();
                if (render != null)
                {
                    float width = 25f;
                    float height = 18f;
                    selectionRect.x += (selectionRect.width - width) - 125f;
                    selectionRect.width = width;
                    selectionRect.height = height;
                    Color oldColor = GUI.color;
                    for (int i = 3; i >= 1; i--)
                    {
                        selectionRect.x += 25f;
                        bool ChangesColor = GetQualityRender(i, render);
                        GUI.color = ChangesColor ? Color.green : Color.white;

                        if (GUI.Button(selectionRect, QualityStrs[i - 1]))
                        {
                            switch (i)
                            {
                                case 1:
                                    if (effectClass.Low.Contains(render)) effectClass.Low.Remove(render);
                                    if (effectClass.Middle.Contains(render)) effectClass.Middle.Remove(render);
                                    if (effectClass.High.Contains(render)) effectClass.High.Remove(render);

                                    effectClass.Low.Add(render);

                                    break;
                                case 2:
                                    if (effectClass.Low.Contains(render)) effectClass.Low.Remove(render);
                                    if (effectClass.Middle.Contains(render)) effectClass.Middle.Remove(render);
                                    if (effectClass.High.Contains(render)) effectClass.High.Remove(render);

                                    effectClass.Middle.Add(render);

                                    break;

                                case 3:
                                    if (effectClass.Low.Contains(render)) effectClass.Low.Remove(render);
                                    if (effectClass.Middle.Contains(render)) effectClass.Middle.Remove(render);
                                    if (effectClass.High.Contains(render)) effectClass.High.Remove(render);

                                    effectClass.High.Add(render);
                                    break;

                            }

                            //var selectionObjects = Selection.gameObjects;
                            //if (selectionObjects.Length > 1)
                            //{
                            //    foreach (var selectionObject in selectionObjects)
                            //    {
                            //        var selectRender = selectionObject.gameObject.GetComponent<Renderer>();
                            //        self.ChangeRenderQuality(selectRender, quality);
                            //    }
                            //}
                            //else
                            //{
                            //    self.ChangeRenderQuality(render, quality);
                            //}
                            ////EditorUtility.SetDirty(self);
                            //Repaint();
                        }
                    }

                    GUI.color = oldColor;
                }
            };

        }

        public override void Exit()
        {
            openEditor = false;
            EditorApplication.hierarchyWindowItemOnGUI = null;
            openEditor = false;
        }

        private GameObject SavePrefab(GameObject gameObject, string folder, string name, string lod)
        {
            string path = string.Format("{0}/{1}{2}.prefab", folder, name, lod);
            if (File.Exists(path))
            {
                int index = 0;
                path = string.Format("{0}/{1}{2}{3}.prefab", folder, name, lod, index.ToString());
                while (File.Exists(path))
                {
                    index++;
                    path = string.Format("{0}/{1}{2}{3}.prefab", folder, name, lod, index.ToString());
                }
            }

            return PrefabUtility.SaveAsPrefabAsset(gameObject, path);
        }


        private (List<Transform> mid, List<Transform> low) GetRendererToTransform(GameObject saveObject)
        {
            var resultMid = new List<Transform>();
            foreach (var renderer in effectClass.High)
            {
                resultMid.Add(FindTransform(renderer.transform, saveObject.transform));
            }

            var resultLow = new List<Transform>();
            foreach (var renderer in effectClass.Middle)
            {
                resultLow.Add(FindTransform(renderer.transform, saveObject.transform));
            }

            return (resultMid, resultLow);
        }

        private Transform FindTransform(Transform src, Transform des)
        {
            Stack<int> ints = new Stack<int>();
            while (src.parent != null)
            {
                ints.Push(src.GetSiblingIndex());
                src = src.parent;
            }

            while (ints.Count > 0)
            {
                des = des.GetChild(ints.Pop());
            }

            return des;
        }


        private bool GetQualityRender(int level, Renderer renderer)
        {
            if (effectClass == null) return false;
            switch (level)
            {
                case 1:
                    return effectClass.Low == null ? false : effectClass.Low.Contains(renderer);
                case 2:
                    return effectClass.Middle == null ? false : effectClass.Middle.Contains(renderer);
                case 3:
                    return effectClass.High == null ? false : effectClass.High.Contains(renderer);

            }

            return false;
        }

        //
        private bool IsChildObjt(Transform transform)
        {
            if (transform == null) return false;
            else if (transform.gameObject == gameObj) return true;
            while (transform.parent != null)
            {
                transform = transform.parent;
                if (transform.gameObject == gameObj) return true;
            }

            return false;
        }


        private void InitGameObject()
        {
            if (gameObj == null)
            {
                return;
            }

            effectClass.Animators = gameObj.GetComponentsInChildren<Animator>(true).ToList();
            effectClass.Low = gameObj.GetComponentsInChildren<Renderer>(true).ToList();
            effectClass.High = new List<Renderer>();
            effectClass.Middle = new List<Renderer>();


        }


        public void EditorQualityPlay(int quality)
        {
#if UNITY_EDITOR
            foreach (var render in gameObj.GetComponentsInChildren<Renderer>(true))
            {
                render.enabled = true;
                render.transform.transform.gameObject.SetActive(true);
            }

            List<GameObject> objects = new List<GameObject>();


            switch (quality)
            {
                case 3:

                    foreach (var render in effectClass.High)
                    {
                        if (render.enabled) objects.Add(render.gameObject);
                    }

                    foreach (var render in effectClass.Middle)
                    {
                        if (render.enabled) objects.Add(render.gameObject);
                    }

                    foreach (var render in effectClass.Low)
                    {
                        if (render.enabled) objects.Add(render.gameObject);
                    }

                    break;
                case 2:

                    foreach (var render in effectClass.Middle)
                    {
                        if (render.enabled) objects.Add(render.gameObject);
                    }

                    foreach (var render in effectClass.Low)
                    {
                        if (render.enabled) objects.Add(render.gameObject);
                    }

                    break;
                case 1:
                    foreach (var render in effectClass.Low)
                    {
                        if (render.enabled) objects.Add(render.gameObject);
                    }

                    break;

            }

            Selection.objects = objects.ToArray();
            foreach (var item in gameObj.GetComponentsInChildren<ParticleSystem>())
            {
                item.Stop();
                item.Clear();
                item.Play();
            }

            perTime = (float)EditorApplication.timeSinceStartup;
            startTime = perTime;

            if (reset)
            {
                RebindAnimatorUpdate();
            }

            reset = true;

            EditorApplication.update += AnimatorUpdate;



#endif
        }

        //
        float startTime;
        float perTime;

        void AnimatorUpdate()
        {
            float _nextTime = (float)EditorApplication.timeSinceStartup - perTime;
            perTime = (float)EditorApplication.timeSinceStartup;
            //Debug.Log(_nextTime);
            foreach (var animator in effectClass.Animators)
            {
                animator.Update(_nextTime);
            }

            if (perTime - startTime > 3)
            {
                reset = false;
                RebindAnimatorUpdate();
            }
        }


        void RebindAnimatorUpdate()
        {

            EditorApplication.update -= AnimatorUpdate;

            foreach (var animator in effectClass.Animators)
            {
                animator.Rebind();
            }
        }




        public class EffectClass
        {
            public List<Renderer> Low;
            public List<Renderer> Middle;
            public List<Renderer> High;
            public List<Animator> Animators;

            public EffectClass()
            {

            }

        }



    }
}