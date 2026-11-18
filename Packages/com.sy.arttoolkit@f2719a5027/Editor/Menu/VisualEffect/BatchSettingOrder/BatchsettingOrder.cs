
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace SYArtToolkit
{

    public class BatchsettingOrder : ToolInterface
    {

        private class ListObj : ScriptableObject
        {
            public List<GameObject> gameObjects = new List<GameObject>();
        }

        private ListObj objs;
        private SerializedObject serializedObject;
        private SerializedProperty serializedProperty;

        private int orderValue;
        private ParticleSystemSimulationSpace simulationSpaceValue;
        private ParticleSystemScalingMode scalingModeValue;
        private int maxParticleNum;
        private bool orderValueIncludeChild;
        private bool simulationSpaceIncludeChild;
        private bool scalingModeIncludeChild;
        private bool maxParticleIncludeChild;

        public BatchsettingOrder()
        {
            objs = ScriptableObject.CreateInstance<ListObj>();
            serializedObject = new SerializedObject(objs);
            serializedProperty = serializedObject.FindProperty("gameObjects");
            orderValue = 0;
            simulationSpaceValue = ParticleSystemSimulationSpace.Local;
            scalingModeValue = ParticleSystemScalingMode.Local;
            maxParticleNum = 20;
            orderValueIncludeChild = true;
            simulationSpaceIncludeChild = true;
            scalingModeIncludeChild = true;
            maxParticleIncludeChild = true;
        }
        public override void InterfaceShow(bool noBool, Rect noRect)
        {
            if (objs == null)
            {
                objs = ScriptableObject.CreateInstance<ListObj>();
                serializedObject = new SerializedObject(objs);
                serializedProperty = serializedObject.FindProperty("gameObjects");
            }
            EditorGUILayout.PropertyField(serializedProperty, new GUIContent { tooltip = "尽量避免同时输入具有父子关系的两个物体", text = "请输入物体" });
            serializedObject.ApplyModifiedProperties();
            GUILayout.Space(10);
            GUILayout.Label("----------------修改粒子或条带的Order-------------");
            orderValue = EditorGUILayout.IntField("修改值:", orderValue);
            EditorGUILayout.BeginHorizontal();
            orderValueIncludeChild = GUILayout.Toggle(orderValueIncludeChild, "包含所有的子物体");
            if (GUILayout.Button("修改"))
            {
                var tl = GetChild(orderValueIncludeChild);
                GetAndSetOrder(tl);

                Submit.ArtToolkitData.Instance.AddBatchModify(tl.Count);
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.Label("----------------修改粒子的SimulationSpace-------------");
            simulationSpaceValue =
                (ParticleSystemSimulationSpace)EditorGUILayout.EnumPopup("请选择SimulationSpace:", simulationSpaceValue);
            EditorGUILayout.BeginHorizontal();
            simulationSpaceIncludeChild = GUILayout.Toggle(simulationSpaceIncludeChild, "包含所有的子物体");
            if (GUILayout.Button("修改"))
            {
                var tl = GetChild(orderValueIncludeChild);
                GetAndSetSimulationSpace(tl);
                Submit.ArtToolkitData.Instance.AddBatchModify(tl.Count);
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.Label("----------------修改粒子的ScalingMode-------------");
            scalingModeValue =
                (ParticleSystemScalingMode)EditorGUILayout.EnumPopup("请选择ScalingMode:", scalingModeValue);
            EditorGUILayout.BeginHorizontal();
            scalingModeIncludeChild = GUILayout.Toggle(scalingModeIncludeChild, "包含所有的子物体");
            if (GUILayout.Button("修改"))
            {
                var tl = GetChild(scalingModeIncludeChild);
                GetAndSetScallingMode(tl);
                Submit.ArtToolkitData.Instance.AddBatchModify(tl.Count);
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.Label("----------------修改粒子的最大粒子数-------------");
            maxParticleNum = EditorGUILayout.IntField("修改值:", maxParticleNum);
            EditorGUILayout.BeginHorizontal();
            maxParticleIncludeChild = GUILayout.Toggle(maxParticleIncludeChild, "包含所有的子物体");
            if (GUILayout.Button("修改"))
            {
                var tl = GetChild(maxParticleIncludeChild);
                GetAndSetMaxNum(tl);
                Submit.ArtToolkitData.Instance.AddBatchModify(tl.Count);
            }

            EditorGUILayout.EndHorizontal();





        }

        private List<Transform> GetChild(bool IncludeChild)
        {
            Queue<Transform> queue = new Queue<Transform>();
            foreach (var obj in objs.gameObjects)
            {
                if (obj != null) queue.Enqueue(obj.transform);
            }

            List<Transform> res = new List<Transform>();
            while (queue.Count != 0)
            {
                Transform t = queue.Dequeue();
                if (t == null) continue;
                res.Add(t);
                if (IncludeChild)
                {
                    for (int i = 0; i < t.childCount; ++i)
                    {
                        queue.Enqueue(t.GetChild(i));
                    }
                }
            }

            return res;
        }


        private void GetAndSetOrder(List<Transform> tl)
        {
            foreach (var t in tl)
            {
                ParticleSystemRenderer p = t.gameObject.GetComponent<ParticleSystemRenderer>();
                TrailRenderer tp = t.gameObject.GetComponent<TrailRenderer>();
                if (p) p.sortingOrder = orderValue;
                if (tp) tp.sortingOrder = orderValue;
            }
        }

        private void GetAndSetSimulationSpace(List<Transform> tl)
        {
            foreach (var t in tl)
            {
                ParticleSystem p = t.gameObject.GetComponent<ParticleSystem>();
                if (p)
                {
                    var main = p.main;
                    main.simulationSpace = simulationSpaceValue;
                }
            }
        }

        private void GetAndSetScallingMode(List<Transform> tl)
        {
            foreach (var t in tl)
            {
                var p = t.gameObject.GetComponent<ParticleSystem>();
                if (p)
                {
                    var main = p.main;
                    main.scalingMode = scalingModeValue;
                }
            }
        }

        private void GetAndSetMaxNum(List<Transform> tl)
        {
            foreach (var t in tl)
            {
                var p = t.gameObject.GetComponent<ParticleSystem>();
                if (p)
                {
                    var main = p.main;
                    main.maxParticles = maxParticleNum;
                }
            }
        }
    }
}