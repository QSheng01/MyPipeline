using UnityEngine;

namespace SYArtToolkit
{
    public class CreateParticle : ToolInterface
    {
        public CreateParticle() { }
        public override void InterfaceShow(bool noBool, Rect noRect)
        {

        }



        // public ArtToolBoxLocalData assetSource;
        // public string resourcesPath;
        // private string[] scriptText = new string[0];//要生成的脚本的主体
        // private  TextAsset textAsset;
        // private List<GameObject> gamobjects = new List<GameObject>();
        // private List<string> texts = new List<string>();
        // private List<string> names = new List<string>();
        // private Vector2 beginScrollView;
        //
        // public CreateParticle()//将库中数据添加到列表当中
        // {
        //     gamobjects = new List<GameObject>();
        //     texts = new List<string>();
        //     names = new List<string>();
        //     textAsset = Resources.Load<TextAsset>("Template");
        //     scriptText = textAsset.text.Split('\n');
        //     resourcesPath = AssetDatabase.GetAssetPath(textAsset);
        //     //Debug.Log(resourcesPath);
        //
        //     string[] temp2 = resourcesPath.Split('/');
        //     string temp3 = "";
        //     for (int i = 0; i < temp2.Length - 1; i++)
        //     {
        //         temp3 += temp2[i] + '/';
        //     }
        //
        //     resourcesPath = temp3;
        //     temp3 += "ParticleExpandData.asset";
        //
        //     assetSource = GetScriptableObjectDataArtToolBox.UpdateData(assetSource);
        //     
        //     if (assetSource.gameObjects.Count>=1)
        //     {
        //         for (int i = 0; i < assetSource.gameObjects.Count; i++) //窗口数组拿取配置
        //         {
        //             gamobjects.Add(assetSource.gameObjects[i]);
        //             texts.Add(assetSource.texts[i]);
        //             names.Add(assetSource.names[i]);
        //         }
        //     }
        // }
        // private void RefreshElements()//更新数据
        // {
        //     assetSource.gameObjects = gamobjects;
        //     assetSource.texts = texts;
        //     assetSource.names = names;
        //     AssetDatabase.SaveAssets();
        //     AssetDatabase.Refresh();//创建完成后刷新
        //     //EditorUtility.SetDirty(assetSource);
        //     //Debug.Log("Destory");
        //     AddToFile();
        // }
        // private void AddToFile()//将库中的信息添加到生成文件夹中（CustomRightButton)
        // {
        //     string temp = "";
        //     temp += "using System.Collections;" + "\r\n" +
        //             "using System.Collections.Generic;" + "\r\n" +
        //             "using UnityEngine;" + "\r\n" +
        //             "using UnityEditor;" + "\r\n" +
        //             "public class VFX_CustomizeRightButton" + "\r\n" +
        //             "{" + "\r\n";//脚本头部
        //     for (int i = 0; i < gamobjects.Count; i++)//脚本主体
        //     {
        //         string[] temp1 = new string[scriptText.Length];
        //         for (int j = 0; j < temp1.Length; j++) 
        //         {
        //             temp1[j] = scriptText[j];
        //         }
        //         string Path = AssetDatabase.GetAssetPath(gamobjects[i]);
        //         temp1[0] = temp1[0].Replace("菜单名字", texts[i]);
        //         temp1[1] =  temp1[1].Replace("随机数",""+i);
        //         temp1[3] = temp1[3].Replace("物体名字", names[i]);
        //         temp1[4] = temp1[4].Replace("DataPath", Path);
        //         temp1[11] = temp1[11].Replace("DataPath", Path);
        //         for (int k = 0; k < temp1.Length; k++)
        //         {
        //             temp += temp1[k] + "\r\n";
        //         }
        //
        //         temp += "\t\t\t" + "string[] temp = vfxpath.Split('/');" + "\r\n" +
        //                 "\t\t\t" + "string[] temp1 = temp[temp.Length-1].Split('.');" + "\r\n" +
        //                 "\t\t\t" + "name = temp1[0];" + "\r\n" +
        //                 "\t\t" + "}" + "\r\n" +
        //                 "\t\t" + "vfx.name = name;" + "\r\n" +
        //                 "\t\t" + "Selection.activeObject = vfx;" + "\r\n" +
        //                 "\t\t" + "EditorGUIUtility.PingObject(vfx);" + "\r\n" +
        //                 "\t" + "}" + "\r\n";
        //     }
        //     temp = temp + "}";//脚本尾部
        //     
        //     //获取文档路径
        //     string[] temp4 = resourcesPath.Split('/');
        //     string temp5 = "";
        //     for (int i = 0; i < temp4.Length - 2; i++)
        //     {
        //         temp5 += temp4[i] + '/';
        //     }
        //     temp5 += "ParticleSystemSet/CustomRightButton.cs";
        //     //Debug.Log(temp);
        //     //将修改后的内容重新写入到文档中
        //     File.WriteAllText(temp5, temp);
        //     AssetDatabase.SaveAssets();
        //     AssetDatabase.Refresh();
        //     //Debug.Log("create success !!!!");
        // }
        //
        // public GUIStyle GUIStyles()
        // {
        //     GUIStyle GS = new GUIStyle();
        //     GS.alignment = TextAnchor.MiddleLeft;//文本锚点
        //     GS.fontSize = 16;//文字大小
        //     GS.normal.textColor = new Color(1, 1f, 1f, 1);//文字颜色
        //     return GS;
        // }
        // public override void InterfaceShow(bool noBool, Rect noRect)
        // {
        //     var style = GUIStyles();
        //     var options1 = new[] { GUILayout.Width(250), GUILayout.Height(40) }; //定义一个tex2d的宽高
        //     using (new GUILayout.HorizontalScope("box"))
        //     {
        //         GUILayout.Label("在本页面添加元素，即可按这些元素生成脚本，以在场景右键菜单 添加快速创建对应元素的按钮" + "\n" +
        //                         "1、点击【添加元素】，添加预制体" + "\n" +
        //                         "2、中间的文本框是预制体的在右键【快速创建】下的按钮名称，不输入将不会显示在按钮中" + "\n" + 
        //                         "3、右侧文本框是预制体名称，不输入则默认预制体名称" + "\n" +
        //                         "4、输入创建后的命名并点击【更新元素】" + "\n" +
        //                         "※（注意  命名只支持中文、英文和下划线  不支持其他任何符号  否则会报错）", style);
        //     }
        //    
        //     GUILayout.BeginHorizontal();
        //     if (GUILayout.Button("添加元素", GUILayout.Height(40)))
        //     {
        //         gamobjects.Add(null);
        //         texts.Add(null);
        //         names.Add(null);
        //     }
        //     GUILayout.Space(10);
        //     if (GUILayout.Button("更新元素", GUILayout.Height(40)))
        //     {
        //         RefreshElements();
        //     }
        //
        //     GUILayout.EndHorizontal();
        //     style.alignment = TextAnchor.MiddleCenter;
        //
        //     using (new GUILayout.HorizontalScope("box"))
        //     {
        //         GUILayout.BeginHorizontal();
        //         //GUILayout.Label("序号",style,GUILayout.Width(40), GUILayout.Height(30));
        //         GUILayout.Label("预制体",style,GUILayout.Width(250), GUILayout.Height(30));
        //         GUILayout.Space(10);
        //         GUILayout.Label("按钮名称",style,GUILayout.Height(30));
        //         GUILayout.Space(10);
        //         GUILayout.Label("生成物体名称",style,GUILayout.Height(30));
        //         GUILayout.Space(100);
        //         GUILayout.EndHorizontal();
        //     }
        //
        //     beginScrollView = GUILayout.BeginScrollView(beginScrollView); //开始滚动视图、列表
        //     {
        //         if (gamobjects.Count >= 1)
        //         {
        //             for (int i = 0; i < gamobjects.Count; i++)
        //             {
        //                 using (new GUILayout.HorizontalScope())
        //                 {
        //                     GUILayout.BeginHorizontal();
        //                     //GUILayout.Label("第" + (i + 1) + "个",style,GUILayout.Width(40), GUILayout.Height(40));
        //                     gamobjects[i] =
        //                         EditorGUILayout.ObjectField(gamobjects[i], typeof(GameObject), false, options1) as
        //                             GameObject;
        //                     GUILayout.Space(10);
        //                     texts[i] = EditorGUILayout.TextField(texts[i],GUILayout.Height(40));
        //                     GUILayout.Space(10);
        //                     names[i] = EditorGUILayout.TextField(names[i], GUILayout.Height(40));
        //                     if (GUILayout.Button("删除此元素", GUILayout.Width(100), GUILayout.Height(40)))
        //                     {
        //                         gamobjects.RemoveAt(i);
        //                         texts.RemoveAt(i);
        //                         names.RemoveAt(i);
        //                     }
        //                     GUILayout.EndHorizontal();
        //                 }
        //                 GUILayout.Space(3);
        //             }
        //         }
        //     }
        //     GUILayout.EndScrollView(); //结束滚动视图、列表
        // }
        //

    }
}
