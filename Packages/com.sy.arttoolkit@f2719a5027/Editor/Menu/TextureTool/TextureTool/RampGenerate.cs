
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


namespace SYArtToolkit
{

    public class RampGenerate : TextureBase
    {

        public RampGenerate()
        {
            SetColorSpaceValue();
            maxHistorySize = 300;
            gradientHistory = new List<Gradient>(maxHistorySize);
            RecordUndo();
        }
        private int maxHistorySize;
        private List<Gradient> gradientHistory;
        private bool isAlpha = true;
        private Gradient gradient = new Gradient();
        private int resolutionX = 2, resolutionY = 0;
        private Vector2Int resolution = new Vector2Int(256, 4);
        private string outFileName = "Ramp";
        private Vector2 v2;
        private bool mipMapEnable = false;
        private bool alphaIsTransparency = true;
        private bool undoPool = false;
        public override void InterfaceShow(bool atThisWindow, Rect noRect)
        {


            GUILayout.BeginArea(new Rect(0, 0, 300, 400));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("选择渐变Gradient");
            EditorGUI.BeginChangeCheck();

            var NewGradient = EditorGUILayout.GradientField("Gradient", gradient);
            if (EditorGUI.EndChangeCheck())
            {
                RecordUndo();
                gradient = NewGradient;
            }


            GUILayout.Space(10);

            EditorGUILayout.PrefixLabel("输出分辨率:     " + resolution[0].ToString() + " * " + resolution[1].ToString());
            //EditorGUILayout.BeginHorizontal();  
            int xtemp = (int)Mathf.Pow(2, resolutionX + 6);
            int ytemp = (int)Mathf.Pow(2, resolutionY + 2);
            resolutionX = EditorGUILayout.IntSlider("输出分辨率的长度：" + xtemp.ToString(), resolutionX, 0, 4);
            resolutionY = EditorGUILayout.IntSlider("输出分辨率的宽度：" + ytemp.ToString(), resolutionY, 0, 3);

            resolution[0] = xtemp;
            resolution[1] = ytemp;


            GUILayout.Space(10);

            isAlpha = GUILayout.Toggle(isAlpha, "是否包含A通道");
            outFileName = EditorGUILayout.TextField("输出图片的名字：", outFileName);
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("撤回", GUILayout.Width(40)))
            {
                Undo();
            }
            undoPool = GUILayout.Toggle(undoPool, "查看撤回池");
            EditorGUILayout.EndHorizontal();

            if (undoPool)
            {
                v2 = GUILayout.BeginScrollView(v2);
                var e = gradientHistory.GetEnumerator();
                while (e.MoveNext())
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.GradientField(e.Current, GUILayout.Width(50));
                    if (e.MoveNext())
                        EditorGUILayout.GradientField(e.Current, GUILayout.Width(50));
                    if (e.MoveNext())
                        EditorGUILayout.GradientField(e.Current, GUILayout.Width(50));
                    if (e.MoveNext())
                        EditorGUILayout.GradientField(e.Current, GUILayout.Width(50));
                    if (e.MoveNext())
                        EditorGUILayout.GradientField(e.Current, GUILayout.Width(50));
                    GUILayout.EndHorizontal();
                }

                GUILayout.EndScrollView();
            }
            GUILayout.Space(20);
            SetMipMapAndTransparency(ref mipMapEnable, ref alphaIsTransparency);

            if (GUILayout.Button("生成"))
            {
                Color[] color = new Color[(resolution.x * resolution.y)];
                for (int i = 0; i < resolution.y; i++)
                {
                    int sumi = i * resolution.x;
                    for (int j = 0; j < resolution.x; j++)
                    {
                        float t = j / (float)(resolution.x);
                        color[sumi + j] = gradient.Evaluate(t);
                    }
                }

                Texture2D tex = new Texture2D(resolution.x, resolution.y,
                    isAlpha ? TextureFormat.ARGB32 : TextureFormat.RGB24, false);
                tex.SetPixels(color);
                tex.Apply();
                var prefs = EditorPrefs.GetString("Arttoolkit.RampGenerate");
                string path = Save(string.IsNullOrEmpty(prefs) ? Application.dataPath : prefs, outFileName, tex);
                if (!string.IsNullOrEmpty(path))
                {
                    EditorPrefs.SetString("Arttoolkit.RampGenerate", Path.GetDirectoryName(path));
                }

                TextureImporterSet(AbsoluteToRelativelyPath(path), mipMapEnable, alphaIsTransparency);

                GUIUtility.ExitGUI();

                Debug.Log("成功生成");
            }

            GUILayout.EndArea();
        }


        void RecordUndo()
        {
            gradientHistory.Add(CopyGradient(gradient));
            if (gradientHistory.Count > maxHistorySize)
            {
                gradientHistory.RemoveAt(0);
            }
        }

        void Undo()
        {
            if (gradientHistory.Count > 1)
            {
                int index = gradientHistory.Count - 1;

                gradient = gradientHistory[index];
                gradientHistory.RemoveAt(index);
                //Repaint();
            }
        }

        Gradient CopyGradient(Gradient source)
        {
            Gradient result = new Gradient();
            result.colorKeys = source.colorKeys;
            result.alphaKeys = source.alphaKeys;
            return result;
        }
    }
}
