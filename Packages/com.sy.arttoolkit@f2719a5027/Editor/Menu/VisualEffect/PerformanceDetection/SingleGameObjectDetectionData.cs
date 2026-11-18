using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;


namespace SYArtToolkit
{


    public class GameObjectDetectionData
    {
        public GameObject gameObject;
        private (string name, long value, long maxValue)[] valueData;
        private ReorderableList reorderableList;
        private ReorderableList reorderableListEmpty;
        private bool fold = true;
        private Presuppose presuppose;

        public long[] defaultValue;
        public float defaultMemory;

        public float Height
        {
            get
            {
                if (fold) return 220;
                else return 35;
            }
        }
        public GameObjectDetectionData(GameObject inGameObject, List<Presuppose> lists, Presuppose select)
        {
            gameObject = inGameObject;
            valueData = new (string name, long value, long maxValue)[8];
            for (int i = 0; i < 8; ++i)
            {
                valueData[i] = (PerformanceDetectionClass.performanceName[i], 0, 0);
            }

            presuppose = select;
            foreach (var p in PerformanceDetectionClass.Instance.listPres)
            {
                if (GlobMatcher.IsMatch(p.glob, gameObject.name))
                {
                    presuppose = p; break;
                }
            }
            UpdatePreset();
            reorderableList = new ReorderableList(valueData, typeof((string name, long value, long maxValue)), false,
                true, false, false);
            reorderableList.drawHeaderCallback = (Rect rect) =>
            {
                fold = EditorGUI.BeginFoldoutHeaderGroup(rect, fold, "");
                EditorGUI.EndFoldoutHeaderGroup();
                float width = rect.width / 3;
                GUI.Label(new Rect(rect.x + 15, rect.y, width, rect.height), gameObject.name);
                if (GUI.Button(new Rect(rect.x + width, rect.y, width, rect.height), presuppose.name))
                {
                    GenericMenu genericMenu = new GenericMenu();
                    foreach (var p in lists)
                    {
                        genericMenu.AddItem(new GUIContent(p.name), false, () =>
                        {
                            presuppose = p;
                            UpdatePreset();
                        });
                    }
                    genericMenu.ShowAsContext();
                }

                GUI.Label(new Rect(rect.x + 2 * width, rect.y, width, rect.height), "当前/最大/预设");//添加预设值





            };
            reorderableList.drawElementCallback = (Rect rect, int index, bool noBool0, bool noBool1) =>
            {

                float width = rect.width / 3 * 2;
                if (index == 7)
                {

                    GUI.Label(new Rect(rect.x, rect.y, width, rect.height), PerformanceDetectionClass.performanceName[7]);
                    float res = valueData[7].value / (float)(1024 * 1024);
                    res = (int)(res * 1000) / (float)1000;
                    GUI.Label(new Rect(rect.x + width - 10, rect.y, width, rect.height), "<color=green>" + res + " MB</color>", PerformanceWindow.style);
                }
                else if (index == 4 || index == 5)
                {

                    GUI.Label(new Rect(rect.x, rect.y, width, rect.height), PerformanceDetectionClass.performanceName[index]);
                    string temp = "<color=green>" + valueData[index].value + "</color>";
                    //temp += "         /" + "<color=white>" + defaultValue[index] + " </color>";
                    GUI.Label(new Rect(rect.x + width + 10, rect.y, width, rect.height), temp, PerformanceWindow.style);
                }
                else
                {
                    GUI.Label(new Rect(rect.x, rect.y, width, rect.height), PerformanceDetectionClass.performanceName[index]);
                    string temp = FormatMax(valueData[index].value, defaultValue[index]) +
                                  " / ";
                    temp += FormatMax(valueData[index].maxValue, defaultValue[index]);
                    temp += "/" + "<color=white>" + defaultValue[index] + " </color>";
                    GUI.Label(new Rect(rect.x + width, rect.y, width, rect.height), temp, PerformanceWindow.style);
                }
            };

            reorderableListEmpty = new ReorderableList(valueData, typeof((string name, long value, long maxValue)), false,
                true, false, false);
            reorderableListEmpty.drawHeaderCallback = (Rect rect) =>
            {
                fold = EditorGUI.BeginFoldoutHeaderGroup(rect, fold, "");
                EditorGUI.EndFoldoutHeaderGroup();
                float width = rect.width / 3;
                GUI.Label(new Rect(rect.x + 15, rect.y, width, rect.height), gameObject.name);
                if (GUI.Button(new Rect(rect.x + width, rect.y, width, rect.height), presuppose.name))
                {
                    GenericMenu genericMenu = new GenericMenu();
                    foreach (var p in lists)
                    {
                        genericMenu.AddItem(new GUIContent(p.name), false, () =>
                        {
                            presuppose = p;
                            UpdatePreset();
                        });
                    }
                    genericMenu.ShowAsContext();
                }

                GUI.Label(new Rect(rect.x + 2 * width, rect.y, width, rect.height), "当前/最大/预设");//添加预设值




            };
            reorderableListEmpty.drawElementCallback = (Rect rect, int index, bool noBool0, bool noBool1) => { };
            reorderableListEmpty.elementHeight = 0;
        }

        public void UpdateData(ParticleAndTextureDetection particleAndTextureDetection)
        {
            //数据更新+传递
            (valueData[0].value, valueData[1].value) = particleAndTextureDetection.GetVertexNum(gameObject);
            (valueData[2].value, valueData[3].value) = particleAndTextureDetection.getParticleSystemNumber(gameObject);
            valueData[4].value = particleAndTextureDetection.GetMeshNum(gameObject);
            valueData[5].value = particleAndTextureDetection.GetSkinMeshNum(gameObject);
            (valueData[6].value, valueData[7].value) = particleAndTextureDetection.DetectTexture(gameObject);
            for (int i = 0; i < 8; ++i)
            {
                valueData[i].maxValue = valueData[i].maxValue > valueData[i].value
                    ? valueData[i].maxValue
                    : valueData[i].value;
            }

        }


        private void UpdatePreset()
        {

            defaultValue = new long[7];
            defaultMemory = presuppose.textureMemory;
            defaultValue[0] = presuppose.vertexNum;
            defaultValue[1] = presuppose.trisNum;
            defaultValue[2] = presuppose.particleSystemNum;
            defaultValue[3] = presuppose.particleNum;
            defaultValue[4] = presuppose.meshNum;
            defaultValue[5] = presuppose.skinMesh;
            defaultValue[6] = presuppose.textureNum;
        }

        public void Show(Rect rect)
        {
            if (fold)
                reorderableList.DoList(rect);
            else reorderableListEmpty.DoList(rect);
        }

        private string FormatMax(long x, long max)
        {
            string front;
            if (x > max) front = "<color=red>";
            else front = "<color=green>";
            return front + x.ToString() + "</color>";
        }

        private string FormatMax(long x, float max)
        {
            string front;
            float res = x / (float)(1024 * 1024);
            if (res > max) front = "<color=red>";
            else front = "<color=green>";
            res = (int)(res * 1000) / (float)1000;
            return front + res.ToString() + " MB</color>";
        }

    }

}
