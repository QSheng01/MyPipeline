
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace SYArtToolkit
{
    public class RapidOpenScene : ToolInterface //需要配合using UnityEditor;
    {
        private ArtToolBoxLocalData data;
        public Vector2 beginScrollView;
        private GUIStyle style;
        public RapidOpenScene()
        {
            style = new GUIStyle();
            style.alignment = TextAnchor.MiddleCenter; //文本锚点
            style.fontSize = 15; //文字大小
            style.normal.textColor = new Color(1, 1f, 1f, 1); //文字颜色
        }
        public override void InterfaceShow(bool atThisWindow, Rect positionas)
        {

            data = GetScriptableObjectDataArtToolBox.UpdateData(data);

            GUILayout.Label("拖拽场景文件到本窗口 不可在运行时候切换场景", style);
            beginScrollView = GUILayout.BeginScrollView(beginScrollView); //开始滚动视图、列表
            {
                if (data.scenePathData.Count >= 1)
                {
                    for (int i = 0; i < data.scenePathData.Count; i++)
                    {
                        GUILayout.Label("-----------------------------------------------------");
                        GUILayout.BeginVertical();
                        Object aa = AssetDatabase.LoadAssetAtPath<Object>(
                            AssetDatabase.GUIDToAssetPath(data.scenePathData[i]));

                        if (GUILayout.Button("切换 : " + aa.name + "场景", GUILayout.Height(30)))
                        {
                            Submit.ArtToolkitData.Instance.AddConvenient(1);
                            Scene nowScene = EditorSceneManager.GetActiveScene();
                            EditorSceneManager.SaveScene(nowScene);
                            EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(data.scenePathData[i]));
                        }

                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("在文件管理器打开  ", GUILayout.Height(25)))
                        {
                            Submit.ArtToolkitData.Instance.AddConvenient(1);
                            EditorUtility.RevealInFinder(AssetDatabase.GUIDToAssetPath(data.scenePathData[i]));
                        }

                        if (GUILayout.Button("删除", GUILayout.Height(25)))
                        {
                            data.scenePathData.RemoveAt(i);
                        }

                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                    }
                }
            }
            GUILayout.EndScrollView(); //结束滚动视图、列表

            if (DragAndDrop.paths.Length > 0) //如果拖拽的资源路径数量大于0,则执行if语句块内的代码
            {
                if (atThisWindow) //获取鼠标当前悬停的窗口对象,存储在mouseOverWindow变量中。
                {
                    if (Event.current.type == EventType.DragUpdated)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                    }
                    //拖入窗口并松开鼠标,
                    else if (Event.current.type == EventType.DragExited)
                    {

                        if (DragAndDrop.paths.Length > 0)
                        {
                            string path = DragAndDrop.paths[0];
                            if (path != null && path.Length > 6 && path.Substring(path.Length - 6, 6) == ".unity")
                            {
                                data.scenePathData.Add(AssetDatabase.AssetPathToGUID(DragAndDrop.paths[0]));
                            }
                        }
                    }
                }
            }


            EditorUtility.SetDirty(data);

        }
    }

}