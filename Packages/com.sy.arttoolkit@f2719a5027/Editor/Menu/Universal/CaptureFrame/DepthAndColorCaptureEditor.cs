using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SYArtToolkit.CaptureFrame
{

    [CustomEditor(typeof(DepthAndColorCapture))]
    public class DepthAndColorCaptureEditor : Editor
    {

        private string[] selectString;
        private DepthAndColorCapture depthAndColorCapture;





        public override VisualElement CreateInspectorGUI()
        {

            selectString = GetScraperMode();
            depthAndColorCapture = (DepthAndColorCapture)target;

            depthAndColorCapture.scraperMode = (ScraperMode)EditorPrefs.GetInt(depthAndColorCapture.scraperModeString);
            depthAndColorCapture.outPutMode = (OutPutMode)EditorPrefs.GetInt(depthAndColorCapture.outPutModeString);
            depthAndColorCapture.outPutFrame = (OutPutFrame)EditorPrefs.GetInt(depthAndColorCapture.outPutFrameString);
            depthAndColorCapture.open = EditorPrefs.GetBool(depthAndColorCapture.openString);
            selectString = GetScraperMode();
            if (!Directory.Exists("ScreenScraperOutPutFolder"))
            {
                Directory.CreateDirectory("ScreenScraperOutPutFolder");
            }
            depthAndColorCapture.number = EditorPrefs.GetInt(depthAndColorCapture.numberString);
            depthAndColorCapture.folder = EditorPrefs.GetString(depthAndColorCapture.folderString);
            if (string.IsNullOrEmpty(depthAndColorCapture.folder))
            {
                depthAndColorCapture.folder = "ScreenScraperOutPutFolder";
            }

            DepthAndColorCapture[] ds = FindObjectsOfType<DepthAndColorCapture>();
            foreach (DepthAndColorCapture d in ds)
            {
                if (d != depthAndColorCapture) DestroyImmediate(d);
            }


            return base.CreateInspectorGUI();
        }

        public override void OnInspectorGUI()
        {


            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
            EditorGUI.BeginChangeCheck();
            depthAndColorCapture.scraperMode = (ScraperMode)GUILayout.Toolbar((int)depthAndColorCapture.scraperMode, selectString);
            //GUILayout.EndHorizontal();
            //文件夹
            GUILayout.BeginHorizontal();
            depthAndColorCapture.folder = EditorGUILayout.DelayedTextField("输出文件夹:", depthAndColorCapture.folder);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                depthAndColorCapture.folder = EditorUtility.OpenFolderPanel("Select Folder", "", "ScreenScraperOutPutFolder");
            }
            GUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                if (Directory.Exists(depthAndColorCapture.folder))
                {
                    EditorPrefs.SetString(depthAndColorCapture.folderString, depthAndColorCapture.folder);
                }
                EditorPrefs.SetInt(depthAndColorCapture.scraperModeString, (int)depthAndColorCapture.scraperMode);
            }

            depthAndColorCapture.newFolderName = EditorGUILayout.DelayedTextField("输出文件命名(可不填)", depthAndColorCapture.newFolderName);

            //输出模式
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(AboutStringAttribute.GetEnumString(depthAndColorCapture.outPutFrame)))
            {
                GenericMenu genericMenu = new GenericMenu();
                int num = Enum.GetValues(typeof(OutPutFrame)).Length;
                for (int i = 0; i < num; i++)
                {
                    var frame = (OutPutFrame)i;
                    genericMenu.AddItem(new GUIContent(AboutStringAttribute.GetEnumString((OutPutFrame)i)), i == (int)depthAndColorCapture.outPutFrame, () =>
                    {
                        depthAndColorCapture.outPutFrame = frame;
                        EditorPrefs.SetInt(depthAndColorCapture.outPutFrameString, (int)frame);

                    });
                }
                genericMenu.ShowAsContext();
            }
            GUILayout.Space(10);
            EditorGUI.BeginChangeCheck();
            depthAndColorCapture.number = EditorGUILayout.DelayedIntField(depthAndColorCapture.number);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetInt(depthAndColorCapture.numberString, depthAndColorCapture.number);
            }
            GUILayout.EndHorizontal();

            //输出帧
            switch (depthAndColorCapture.scraperMode)
            {
                case ScraperMode.Screen:

                    if (!Directory.Exists(depthAndColorCapture.folder))
                    {
                        EditorGUILayout.HelpBox("文件夹不可用", MessageType.Error);
                        depthAndColorCapture.approval = false;
                    }
                    else
                    {
                        depthAndColorCapture.approval = true;

                    }
                    break;
                case ScraperMode.Camera:

                    depthAndColorCapture.cam = EditorGUILayout.ObjectField("输入相机:", depthAndColorCapture.cam, typeof(Camera), true) as Camera;

                    if (!Directory.Exists(depthAndColorCapture.folder))
                    {
                        EditorGUILayout.HelpBox("文件夹不可用", MessageType.Error);
                        depthAndColorCapture.approval = false;

                    }
                    else if (depthAndColorCapture.cam == null)
                    {
                        EditorGUILayout.HelpBox("请输入相机", MessageType.Error);
                        depthAndColorCapture.approval = false;

                    }
                    else
                    {
                        depthAndColorCapture.approval = true;

                    }

                    break;


                case ScraperMode.GameObject:

                    depthAndColorCapture.cam = EditorGUILayout.ObjectField("输入相机:", depthAndColorCapture.cam, typeof(Camera), true) as Camera;
                    EditorGUI.BeginChangeCheck();
                    depthAndColorCapture.obj = EditorGUILayout.ObjectField("输入物体:", depthAndColorCapture.obj, typeof(GameObject), true) as GameObject;
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (string.IsNullOrEmpty(depthAndColorCapture.newFolderName) && depthAndColorCapture.obj != null)
                        {
                            depthAndColorCapture.newFolderName = depthAndColorCapture.obj.name;
                        }
                    }
                    EditorGUI.BeginChangeCheck();
                    depthAndColorCapture.outPutMode = (OutPutMode)EditorGUILayout.EnumPopup("输出背景:", depthAndColorCapture.outPutMode);
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorPrefs.SetInt(depthAndColorCapture.outPutModeString, (int)depthAndColorCapture.outPutMode);
                    }
                    //if (depthAndColorCapture.outPutMode == OutPutMode.Alpha)
                    //{
                    //    //depthAndColorCapture.deleteColor = EditorGUILayout.ColorField("剔除颜色：",depthAndColorCapture.deleteColor);

                    //}

                    depthAndColorCapture.layer = EditorGUILayout.IntSlider("Layer:", depthAndColorCapture.layer, 1, 31);
                    if (!Directory.Exists(depthAndColorCapture.folder))
                    {
                        EditorGUILayout.HelpBox("文件夹不可用", MessageType.Error);
                        depthAndColorCapture.approval = false;
                    }
                    else if (depthAndColorCapture.cam == null)
                    {
                        EditorGUILayout.HelpBox("请输入相机", MessageType.Error);
                        depthAndColorCapture.approval = false;
                    }
                    else if (depthAndColorCapture.obj == null)
                    {
                        EditorGUILayout.HelpBox("请输入物体", MessageType.Error);
                        depthAndColorCapture.approval = false;
                    }
                    else
                    {
                        depthAndColorCapture.approval = true;

                    }


                    //EditorGUILayout.HelpBox("This is a warning message.", MessageType.Error);
                    break;
            }

            if (depthAndColorCapture.approval)
            {
                if (EditorApplication.isPlaying)
                {

                    EditorGUILayout.HelpBox("点击停止就可以输出", MessageType.Info);
                }
                else

                    EditorGUILayout.HelpBox("请点击运行", MessageType.Info);
            }




            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginChangeCheck();
            depthAndColorCapture.open = EditorGUILayout.ToggleLeft("截图完成后自动打开文件夹", depthAndColorCapture.open);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(depthAndColorCapture.openString, depthAndColorCapture.open);
            }










        }
        string[] GetScraperMode()
        {
            int num = Enum.GetValues(typeof(ScraperMode)).Length;
            string[] result = new string[num];
            for (int i = 0; i < num; ++i)
            {
                result[i] = AboutStringAttribute.GetEnumString((ScraperMode)i);
            }
            return result;
        }


    }

}
