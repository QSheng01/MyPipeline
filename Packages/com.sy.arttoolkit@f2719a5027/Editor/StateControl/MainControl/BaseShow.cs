
using System;
using UnityEditor;
using UnityEngine;

namespace SYArtToolkit
{
    public class BaseShow
    {
        public About state;
        private string[] aboutenumValue;

        //private StateClassBase[] selectToolList;

        public Tool tool;
        public ToolInterface[] selectTools;
        public ToolDescribe[] toolDescribes;

        public static GUIStyle selectButton;//jfj
        public static GUIStyle normalButton;
        private GUIStyle style;
        private GUIStyle styleText;
        public BaseShow()
        {
            try
            {

                int stateInt = EditorPrefs.GetInt("ArtToolkit.BaseShow.state");
                if (Enum.IsDefined(typeof(About), stateInt)) { state = (About)stateInt; }
                else state = About.Texture;

                int toolInt = EditorPrefs.GetInt("ArtToolkit.BaseShow.tool");
                if (Enum.IsDefined(typeof(Tool), toolInt)) { tool = (Tool)toolInt; }
                else tool = Tool.MergeTexture;
            }
            catch
            {
                state = About.Texture;
                tool = Tool.MergeTexture;
            }
            //selectToolList = new StateClassBase[10];


            if (selectButton == null) selectButton = Resources.Load<GUISkin>("SelectButton").button;
            if (normalButton == null) normalButton = Resources.Load<GUISkin>("NormalButton").button;
            style = new GUIStyle();
            style.fontSize = 35;//文字大小
            style.normal.textColor = new Color(0.8f, 0.5f, 0.5f, 1);//文字颜色
            styleText = new GUIStyle();
            styleText.fontSize = 12;
            styleText.normal.textColor = new Color(0.7f, 0.7f, 0.7f, 1);//文字颜色
            int aboutenumValueCount = Enum.GetValues(typeof(About)).Length;
            aboutenumValue = new string[aboutenumValueCount];
            for (int i = 0; i < aboutenumValueCount; i++)
            {
                aboutenumValue[i] = AboutStringAttribute.GetEnumString((About)i);
            }

            int toolenumValueCount = Enum.GetValues(typeof(Tool)).Length;
            selectTools = new ToolInterface[toolenumValueCount];
            toolDescribes = new ToolDescribe[toolenumValueCount];
            for (int i = 0; i < toolenumValueCount; i++)
            {
                toolDescribes[i] = ToolDescribeAttribute.GetEnum((Tool)i);
            }
            try
            {
                var docguids = AssetDatabase.FindAssets("glob:\"Packages/*/Doc/美术工具箱使用说明.pdf\"", new string[] { "Packages" });

                doc = AssetDatabase.LoadAssetAtPath<DefaultAsset>(AssetDatabase.GUIDToAssetPath(docguids[0]));
            }
            catch
            {
                Debug.Log("找不到使用文档");
            }

        }
        DefaultAsset doc;
        public void MainInterfaceShow()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);

            for (int i = 0; i < aboutenumValue.Length; ++i)
            {
                if (GUILayout.Button(aboutenumValue[i], GetButtonStyle(state, (About)i), GUILayout.Width(70), GUILayout.Height(40)))
                {
                    state = (About)i;

                    EditorPrefs.SetInt("ArtToolkit.BaseShow.state", i);
                }
            }

            //if (GUILayout.Button("通用", GetButtonStyle(state, About.Universal), GUILayout.Width(70),
            //        GUILayout.Height(40)))
            //{
            //    state = About.Universal;
            //}
            //else if (GUILayout.Button("贴图", GetButtonStyle(state, About.Texture), GUILayout.Width(70),
            //             GUILayout.Height(40)))
            //{
            //    state = About.Texture;
            //}
            //else if (GUILayout.Button("特效", GetButtonStyle(state, About.Particle), GUILayout.Width(70),
            //             GUILayout.Height(40)))
            //{
            //    state = About.Particle;
            //}
            //else if (GUILayout.Button("角色", GetButtonStyle(state, About.Material), GUILayout.Width(70),
            //             GUILayout.Height(40)))
            //{
            //    state = About.Material;
            //}
            //else if (GUILayout.Button("地编", GetButtonStyle(state, About.Performance), GUILayout.Width(70),
            //             GUILayout.Height(40)))
            //{
            //    state = About.Performance;
            //}

            GUILayout.Space(10);

            GUILayout.Label("美术工具箱v1.6", style);
            GUILayout.BeginVertical();

            EditorGUILayout.ObjectField(doc, typeof(DefaultAsset), false, GUILayout.Width(70));
            GUILayout.Space(10);
            if (GUILayout.Button("   诗悦引擎组", styleText) && Event.current.button == 2)
            {
                Submit.ReadNetWorkData.Open();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            //CheckAndCreateBase();

        }
        public void ToolListShow()
        {
            //在此处修改
            //
            //
            GUILayout.BeginVertical();

            for (int i = 0; i < toolDescribes.Length; ++i)
            {
                //toolDescribes[i]
                var toolDescribe = toolDescribes[i];
                if (toolDescribe.about != state) continue;
                if (GUILayout.Button(toolDescribe.value, GetGUIStyle(tool, (Tool)i), GUILayout.Width(70), GUILayout.Height(40)))
                {
                    Tool newTool = (Tool)i;
                    if (newTool != tool)
                    {
                        selectTools[(int)tool].Exit();
                        tool = newTool;
                        CheckAndCreateTool();
                        selectTools[(int)tool].Enter();

                        EditorPrefs.SetInt("ArtToolkit.BaseShow.tool", i);
                    }
                }
            }

            GUILayout.EndVertical();


            //Tool newTool = selectToolList[(int)state].ToolsListShow(tool);
            //if (newTool != tool)
            //{
            //    selectTools[(int)tool].Exit();
            //    tool = newTool;
            //    CheckAndCreateTool();
            //    selectTools[(int)tool].Enter();
            //}



        }

        public void ToolsInterfaceShow(EditorWindow atThisWindow, Rect position)
        {
            CheckAndCreateTool();
            selectTools[(int)tool].InterfaceShow(atThisWindow, position);
        }


        private void CheckAndCreateTool()
        {
            int index = (int)tool;
            if (selectTools[index] == null)
            {
                selectTools[index] = toolDescribes[index].construct.Invoke(null) as ToolInterface;
            }
            return;
            //if (selectTools[(int)tool] == null)
            //{
            //    switch (tool)
            //    {
            //        case Tool.ParDelay:
            //            selectTools[(int)tool] = new ParDelaySet();
            //            break;
            //        case Tool.RapidOpenFolder:
            //            selectTools[(int)tool] = new RapidOpenFolder();
            //            break;
            //        case Tool.ParAndTrailOrder:
            //            selectTools[(int)tool] = new BatchsettingOrder();
            //            break;
            //        case Tool.RapidOpenScene:
            //            selectTools[(int)tool] = new RapidOpenScene();
            //            break;
            //        case Tool.MaterialClean:
            //            selectTools[(int)tool] = new MaterialCleanerWin();
            //            break;
            //        case Tool.AssetsAudit:
            //            selectTools[(int)tool] = new AssetsAudit();
            //            break;
            //        case Tool.PerformanceTest:
            //            selectTools[(int)tool] = new PerformanceWindow();
            //            break;
            //        case Tool.FrameSplit:
            //            selectTools[(int)tool] = new FrameSplit();
            //            break;
            //        case Tool.MergeTexture:
            //            selectTools[(int)tool] = new MergeTextures();
            //            break;
            //        case Tool.ModifySize:
            //            selectTools[(int)tool] = new ModifySize();
            //            break;
            //        case Tool.RampGenerate:
            //            selectTools[(int)tool] = new RampGenerate();
            //            break;
            //        case Tool.NoiseGenerate:
            //            selectTools[(int)tool] = new NoiseGenetarion();
            //            break;
            //        case Tool.FormatChange:
            //            selectTools[(int)tool] = new FormatChangeTexture();
            //            break;
            //        case Tool.TextureChannelSet:
            //            selectTools[(int)tool] = new AlphaSet();
            //            break;
            //        case Tool.AssetRefSearchWin:
            //            selectTools[(int)tool] = new AssetRefSearchWin();
            //            break;
            //        case Tool.TextureEditor:
            //            selectTools[(int)tool] = new TextureEditorClass();
            //            break;
            //        case Tool.CropTexture:
            //            selectTools[(int)tool] = new CropTextures();
            //            break;
            //        case Tool.TexCombine:
            //            selectTools[(int)tool] = new TextureCombine();
            //            break;
            //        case Tool.TexBlur:
            //            selectTools[(int)tool] = new TextureBlur();
            //            break;
            //        case Tool.SetShortCutKey:
            //            selectTools[(int)tool] = new ShortCutKeyInterface();
            //            break;
            //        case Tool.DrawCallCalculate:
            //              selectTools[(int)tool] = new DrawCallCalculate();
            //              break;
            //        case Tool.MeshMergeAtSameMaterial:
            //            selectTools[(int)tool] = new MergeMeshAtSameMaterial();
            //            break;
            //        //case Tool.ReduceMesh:
            //        //    selectTools[(int)tool] = new ReduceMesh();
            //        //    break;
            //        case Tool.RenderCubemap:
            //            //selectTools[(int)tool] = ScriptableObject.CreateInstance<RenderCubemap>();
            //            selectTools[(int)tool] = new RenderCubemap();
            //            break;
            //        default:
            //            selectTools[(int)tool] = new WithOutTool();
            //            break;
            //    }
            //}
        }

        //private void CheckAndCreateBase()
        //{
        //    if (selectToolList[(int)state] == null)
        //    {
        //        switch (state)
        //        {
        //            case About.Universal:
        //                selectToolList[(int)state] = new ConvenientInterfaceClass();
        //                break;
        //            case About.Material:
        //                selectToolList[(int)state] = new MaterialInterfaceClass();
        //                break;
        //            case About.Particle:
        //                selectToolList[(int)state] = new ParticleInterfaceClass();
        //                break;
        //            case About.Performance:
        //                selectToolList[(int)state] = new PerformanceInterfaceClass();
        //                break;
        //            case About.Texture:
        //                selectToolList[(int)state] = new TextureInterfaceClass();
        //                break;
        //            default:
        //                selectToolList[(int)state] = new MainInterfaceClass();
        //                break;
        //        }
        //    }
        //}


        public static GUIStyle GetButtonStyle(About about, About thisAbout)
        {
            if (about == thisAbout) return selectButton;
            else return normalButton;
        }

        public static GUIStyle GetButtonStyle(Channel c, Channel t)
        {
            if (c == t) return selectButton;
            return normalButton;
        }
        public static GUIStyle GetButtonStyle(int c, int t)
        {
            if (c == t) return selectButton;
            return normalButton;
        }

        protected GUIStyle GetGUIStyle(Tool tool, Tool thisTool)
        {
            if (tool == thisTool) return BaseShow.selectButton;
            else return BaseShow.normalButton;
        }

        public void Reset()
        {
            selectTools[(int)Tool.NoiseGenerate] = null;
            selectTools[(int)Tool.TextureChannelSet] = null;
            selectTools[(int)Tool.TextureEditor] = null;
            selectTools[(int)Tool.CropTexture] = null;
            selectTools[(int)Tool.TexCombine] = null;
            selectTools[(int)Tool.TexBlur] = null;
        }

        public void Disable()
        {
            foreach (var variable in selectTools)
            {
                if (variable != null) variable.Disable();
            }
        }
    }

}

