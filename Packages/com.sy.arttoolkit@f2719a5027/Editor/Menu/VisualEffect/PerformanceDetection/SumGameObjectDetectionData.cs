
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SYArtToolkit
{
    public class SumGameObjectDetectionData : ParticleAndTextureDetection
    {

        private bool fold = true;
        private (long value, long maxValue)[] sumData;

        private ReorderableList reorderableList;
        private ReorderableList reorderableListEmpty;

        public SumGameObjectDetectionData()
        {
            sumData = new (long value, long maxValue)[8];
            for (int i = 0; i < 8; ++i)
            {
                sumData[i] = (0, 0);
            }

            reorderableList = new ReorderableList(sumData, typeof((string, long, long)), false, true, false, false);
            reorderableList.drawHeaderCallback = (Rect rect) =>
            {
                fold = EditorGUI.BeginFoldoutHeaderGroup(rect, fold, "");
                EditorGUI.EndFoldoutHeaderGroup();
                float width = rect.width / 2;
                GUI.Label(new Rect(rect.x + 15, rect.y, width, rect.height), "总和");
                GUI.Label(new Rect(rect.x + width, rect.y, width, rect.height), "当前/最大");
            };
            reorderableList.drawElementCallback = (Rect rect, int index, bool a, bool f) =>
            {


                float width = rect.width / 2;
                if (index == 7)
                {

                    GUI.Label(new Rect(rect.x, rect.y, width, rect.height), PerformanceDetectionClass.performanceName[7]);
                    float res = sumData[7].value / (float)(1024 * 1024);
                    res = (int)(res * 1000) / (float)1000;
                    GUI.Label(new Rect(rect.x + width - 10, rect.y, width, rect.height), "<color=green>" + res + " MB</color>", PerformanceWindow.style);
                }
                else if (index == 4 || index == 5)
                {

                    GUI.Label(new Rect(rect.x, rect.y, width, rect.height), PerformanceDetectionClass.performanceName[index]);
                    string temp = "<color=green>" + sumData[index].value + "</color>";
                    GUI.Label(new Rect(rect.x + width + 10, rect.y, width, rect.height), temp, PerformanceWindow.style);
                }
                else
                {
                    GUI.Label(new Rect(rect.x, rect.y, width, rect.height), PerformanceDetectionClass.performanceName[index]);
                    string temp = FormatMax(sumData[index].value, PerformanceDetectionClass.defaultValue[index]) +
                                  " / ";
                    temp += FormatMax(sumData[index].maxValue, PerformanceDetectionClass.defaultValue[index]);
                    GUI.Label(new Rect(rect.x + width, rect.y, width, rect.height), temp, PerformanceWindow.style);
                }

            };

            reorderableListEmpty = new ReorderableList(sumData, typeof((string, long, long)), false, true, false, false);
            reorderableListEmpty.elementHeight = 0;
            reorderableListEmpty.drawHeaderCallback = (Rect rect) =>
            {
                fold = EditorGUI.BeginFoldoutHeaderGroup(rect, fold, "");
                EditorGUI.EndFoldoutHeaderGroup();
                float width = rect.width / 2;
                GUI.Label(new Rect(rect.x + 15, rect.y, width, rect.height), "总和");
                GUI.Label(new Rect(rect.x + width, rect.y, width, rect.height), "当前/最大");
            };
            reorderableListEmpty.drawElementCallback = (Rect rect, int index, bool a, bool f) => { };
        }

        public void ResetMax()
        {
            base.ResetValue();
            for (int i = 0; i < 8; ++i)
            {
                sumData[i] = (0, 0);
            }
        }

        public void SumShow()
        {
            if (fold)
                reorderableList.DoLayoutList();
            else reorderableListEmpty.DoLayoutList();
        }

        public void UpdateData()
        {
            sumData[0].value = vertexSumCount;
            sumData[1].value = triangleSumCount;
            sumData[2].value = particleSystemSumCount;
            sumData[3].value = particleSumCount;
            sumData[4].value = meshes.Count;
            sumData[5].value = skinMeshSumCount;
            sumData[6].value = texture2Ds.Count;
            long sumMemory = 0;
            foreach (var pair in texture2Ds)
            {
                sumMemory += pair.Value;
            }
            sumData[7].value = sumMemory;

            for (int i = 0; i < 8; ++i)
            {
                sumData[i].maxValue = sumData[i].maxValue > sumData[i].value ? sumData[i].maxValue : sumData[i].value;
            }

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