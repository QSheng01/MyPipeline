
using UnityEditor;
using UnityEngine;

namespace SYArtToolkit
{

    public class RapidOpenFolder : ToolInterface
    {
        private ArtToolBoxLocalData data;
        private Vector2 beginScrollView;
        private GUIStyle style;
        public RapidOpenFolder()
        {
            style = new GUIStyle();
            style.alignment = TextAnchor.MiddleCenter; //文本锚点
            style.fontSize = 15; //文字大小
            style.normal.textColor = new Color(1, 1f, 1f, 1); //文字颜色
        }
        public override void InterfaceShow(bool atThisWindow, Rect positionRect)
        {
            //这行代码会使用bgmat材质在整个编辑器界面上绘制bgTex纹理,实现全屏预览效果

            data = GetScriptableObjectDataArtToolBox.UpdateData(data);

            GUILayout.Label("拖拽指定文件夹到本窗口 即可添加打开对应文件夹的按钮", style);
            beginScrollView = GUILayout.BeginScrollView(beginScrollView); //开始滚动视图、列表
            {
                if (data.folderPathData.Count >= 1)
                {
                    for (int i = 0; i < data.folderPathData.Count; i++)
                    {
                        GUILayout.Label("-------------------------------------");
                        GUILayout.BeginVertical();
                        //GUILayout.BeginHorizontal()方法用于开始一个水平布局组。
                        Object tempObject = AssetDatabase.LoadAssetAtPath<Object>(
                            AssetDatabase.GUIDToAssetPath(data.folderPathData[i]));
                        if (GUILayout.Button("在Unity中找到 : " + tempObject.name + "文件夹(双击进入)", GUILayout.Height(30)))
                        {
                            Submit.ArtToolkitData.Instance.AddConvenient(1);
                            AssetDatabase.OpenAsset(tempObject, 1);
                        }
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("在文件管理器打开  ", GUILayout.Height(25)))
                        {
                            Submit.ArtToolkitData.Instance.AddConvenient(1);
                            EditorUtility.RevealInFinder(AssetDatabase.GUIDToAssetPath(data.folderPathData[i]));
                        }
                        if (GUILayout.Button("删除", GUILayout.Height(25)))
                        {
                            data.folderPathData.RemoveAt(i);
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
                            DefaultAsset folder =
                                AssetDatabase.LoadAssetAtPath(DragAndDrop.paths[0], typeof(DefaultAsset)) as
                                    DefaultAsset;
                            if (folder != null)
                            {
                                data.folderPathData.Add(AssetDatabase.AssetPathToGUID(DragAndDrop.paths[0]));
                            }
                        }
                    }
                }
            }
            EditorUtility.SetDirty(data);
        }

    }

}