using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SYArtToolkit
{
    public class PrecastRenewal : ToolInterface
    {
        public PrecastRenewal()
        {

            gameObjectsInput = new List<(string, GameObject)>();
            space = 2.0f;
        }


        public GameObject gameObjectFBX;
        [Range(-10, 10)] public float space;
        public GameObjects inputGameObject;
        private SerializedObject serializedObject;
        private SerializedProperty serializedProperty;

        private List<(string, GameObject)> gameObjectsInput;
        private GameObject father;

        public class GameObjects : ScriptableObject
        {
            public List<GameObject> inputGameObjects;
            public GameObjects()
            {
                inputGameObjects = new List<GameObject>();
            }
        }


        public override void InterfaceShow(bool atThisWindow, Rect position)
        {
            if (inputGameObject == null || serializedProperty == null)
            {
                inputGameObject = ScriptableObject.CreateInstance<GameObjects>();
                serializedObject = new SerializedObject(inputGameObject);
                serializedProperty = serializedObject.FindProperty("inputGameObjects");
            }
            GUILayout.Space(10);
            gameObjectFBX = EditorGUILayout.ObjectField("更新后预制体(或FBX)", gameObjectFBX, typeof(GameObject), false) as GameObject;
            EditorGUILayout.PropertyField(serializedProperty);
            serializedObject.ApplyModifiedProperties();
            GUILayout.Space(10);
            space = EditorGUILayout.Slider("生成物体间隔", space, -10, 10);
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("更新"))
            {
                Do();
            }
            if (GUILayout.Button("保存"))
            {
                foreach (var g in gameObjectsInput)
                {
                    PrefabUtility.SaveAsPrefabAsset(g.Item2, g.Item1);
                }
            }


            GUILayout.EndHorizontal();
        }



        private void Do()
        {
            gameObjectsInput.Clear();
            var dealGameObject = CheckInput();
            if (dealGameObject == null) return;
            List<string> names = new List<string>();
            foreach (var item in dealGameObject)
            {
                names.Add(item.name);
            }
            var dictionary = GetRendererDictionary(dealGameObject);
            var results = GetFBXStringDictionaty(dictionary, names);
            int count = results.Count;

            Submit.ArtToolkitData.Instance.AddBatchModify(count);
            for (int i = 0; i < count; ++i)
            {
                gameObjectsInput.Add((AssetDatabase.GetAssetPath(dealGameObject[i]), results[i]));
            }
        }

        private List<GameObject> CheckInput()
        {
            if (gameObjectFBX == null || inputGameObject == null || inputGameObject.inputGameObjects.Count == 0) return null;
            string FBXPath = AssetDatabase.GetAssetPath(gameObjectFBX);
            List<GameObject> dealGameObject = new List<GameObject>();
            foreach (GameObject gameObject in inputGameObject.inputGameObjects)
            {
                if (gameObject == null) continue;
                if (AssetDatabase.GetAssetPath(gameObject) == "") continue;
                dealGameObject.Add(gameObject);
            }
            if (dealGameObject.Count == 0) return null;
            return dealGameObject;
        }

        private Dictionary<string, Queue<Material>> GetRendererDictionary(List<GameObject> gameObjects)
        {
            Dictionary<string, Queue<Material>> result = new Dictionary<string, Queue<Material>>();
            foreach (GameObject gameObject in gameObjects)
            {
                var rs = gameObject.GetComponentsInChildren<Renderer>();
                foreach (var variable in rs)
                {
                    if (!result.ContainsKey(variable.gameObject.name))
                    {
                        result.Add(variable.gameObject.name, new Queue<Material>());
                    }
                    result[variable.gameObject.name].Enqueue(variable.sharedMaterial);
                    //Debug.Log(variable.sharedMaterial);
                }
            }
            return result;
        }

        private List<GameObject> GetFBXStringDictionaty(Dictionary<string, Queue<Material>> dictionary, List<string> names)
        {
            Queue<Transform> queue = new Queue<Transform>();
            List<GameObject> result = new List<GameObject>();
            foreach (var name in names)
            {
                queue.Clear();
                if (father == null) father = new GameObject();
                GameObject newGameObject = Object.Instantiate(gameObjectFBX, father.transform);
                if (result.Count > 0)
                {
                    var v3 = result[result.Count - 1].transform.position;
                    newGameObject.transform.position = new Vector3(v3.x + space, v3.y, v3.z);
                }
                newGameObject.name = name;
                result.Add(newGameObject);
                queue.Enqueue(newGameObject.transform);
                while (queue.Count > 0)
                {
                    Transform t = queue.Dequeue();
                    if (t == null) continue;
                    if (dictionary.ContainsKey(t.gameObject.name))
                    {
                        t.gameObject.GetComponent<Renderer>().sharedMaterial = dictionary[t.gameObject.name].Dequeue();
                        if (dictionary[t.gameObject.name].Count <= 0) dictionary.Remove(t.gameObject.name);
                    }

                    for (int j = 0; j < t.childCount; ++j)
                    {
                        queue.Enqueue(t.GetChild(j));
                    }
                }
            }
            return result;
        }



    }
}