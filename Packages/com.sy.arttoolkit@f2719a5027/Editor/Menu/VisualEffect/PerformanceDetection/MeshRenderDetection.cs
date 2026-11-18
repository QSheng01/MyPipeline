
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SYArtToolkit
{
    public class PerformanceDetectionClass
    {
        public static string[] performanceName =
        {
            "顶点数量", "三角形面数", "粒子系统", "粒子数", "mesh数量", "skinMeshRenderer数量", "贴图数量", "贴图内存"
        };
        private static PerformanceDetectionClass instance;

        public static PerformanceDetectionClass Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new PerformanceDetectionClass();
                }

                return instance;
            }
        }

        private SumGameObjectDetectionData sumGameObjectDetectionData;
        private List<GameObjectDetectionData> gameObjectDetectionDatas;
        private ReorderableList reorderableList;
        private ReorderableList reorderableListEmpty;
        private Dictionary<GameObject, GameObjectDetectionData> hash;
        public static long[] defaultValue;
        public static float defaultMemory;

        public Presuppose select;
        public List<Presuppose> listPres;


        private bool fold = true;
        private PerformanceDetectionClass()
        {
            sumGameObjectDetectionData = new SumGameObjectDetectionData();
            gameObjectDetectionDatas = new List<GameObjectDetectionData>();
            hash = new Dictionary<GameObject, GameObjectDetectionData>();
            reorderableList = new ReorderableList(gameObjectDetectionDatas, typeof(GameObjectDetectionData), false,
                true, false, false);

            reorderableList.drawHeaderCallback = (Rect rect) =>
            {
                fold = EditorGUI.BeginFoldoutHeaderGroup(rect, fold, "");
                EditorGUI.EndFoldoutHeaderGroup();
                GUI.Label(new Rect(rect.x + 15, rect.y, rect.width, rect.height), "性能检测列表");
            };
            reorderableList.elementHeightCallback = (int index) => { return gameObjectDetectionDatas[index].Height; };
            reorderableList.drawElementCallback = (Rect rect, int index, bool _x, bool _a) =>
            {
                gameObjectDetectionDatas[index].Show(rect);
            };
            reorderableListEmpty = new ReorderableList(gameObjectDetectionDatas, typeof(GameObjectDetectionData), false,
                true, false, false);
            reorderableListEmpty.elementHeight = 0;
            reorderableListEmpty.drawElementCallback = (Rect rect, int index, bool _x, bool _a) => { };
            reorderableListEmpty.drawHeaderCallback = (Rect rect) =>
            {
                fold = EditorGUI.BeginFoldoutHeaderGroup(rect, fold, "");
                EditorGUI.EndFoldoutHeaderGroup();
                GUI.Label(new Rect(rect.x + 15, rect.y, rect.width, rect.height), "性能检测列表");
            };


        }

        private void SetPresuppose(Presuppose pres, List<Presuppose> l)
        {
            select = pres;
            defaultValue = new long[7];
            defaultMemory = pres.textureMemory;
            defaultValue[0] = pres.vertexNum;
            defaultValue[1] = pres.trisNum;
            defaultValue[2] = pres.particleSystemNum;
            defaultValue[3] = pres.particleNum;
            defaultValue[4] = pres.meshNum;
            defaultValue[5] = pres.skinMesh;
            defaultValue[6] = pres.textureNum;
            listPres = l;
        }

        public void UpdateData(List<GameObject> inGameObjects, Presuppose pres, List<Presuppose> l)
        {
            SetPresuppose(pres, l);
            List<GameObject> gameObjects = new List<GameObject>();
            foreach (var variable in inGameObjects)
            {
                if (variable != null) gameObjects.Add(variable);
            }

            bool reset = false;
            if (gameObjects.Count == gameObjectDetectionDatas.Count)
            {
                for (int i = 0; i < gameObjects.Count; ++i)
                {
                    if (gameObjects[i] != gameObjectDetectionDatas[i].gameObject)
                    {
                        reset = true;
                        break;
                    }
                }
            }
            else reset = true;

            gameObjectDetectionDatas.Clear();

            foreach (var gameObject in gameObjects)
            {
                if (gameObject == null) continue;
                if (hash.ContainsKey(gameObject))
                {
                    gameObjectDetectionDatas.Add(hash[gameObject]);
                }
                else
                {
                    gameObjectDetectionDatas.Add(new GameObjectDetectionData(gameObject, listPres, select));
                }
            }

            hash.Clear();
            foreach (var variable in gameObjectDetectionDatas)
            {
                if (!hash.ContainsKey(variable.gameObject))
                    hash.Add(variable.gameObject, variable);
            }
            sumGameObjectDetectionData.ResetValue();
            foreach (var variable in gameObjectDetectionDatas)
            {
                variable.UpdateData(sumGameObjectDetectionData);
            }


            if (reset)
            {
                sumGameObjectDetectionData.ResetMax();

                Submit.ArtToolkitData.Instance.AddPerformance(inGameObjects.Count);
            }
            sumGameObjectDetectionData.UpdateData();

        }

        public void Show()
        {
            if (fold)
                reorderableList.DoLayoutList();
            else reorderableListEmpty.DoLayoutList();
            sumGameObjectDetectionData.SumShow();
        }


    }
}
