

using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SYArtToolkit
{
    //美术工具箱的启动入口窗口
    public class WindowControl : EditorWindow
    {

        public static WindowControl Instance;
        public BaseShow baseShow;

        private Scene scene;
        private bool isPlaying = false;
        private Vector2 scroll2 = new Vector2(0, 0);
        private Vector2 scroll1 = new Vector2(0, 0);
        private GUISkin skin;
        protected void OnEnable()
        {
            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            ScriptableObjectDataOfAll oldData =
                AssetDatabase.LoadAssetAtPath<ScriptableObjectDataOfAll>(scriptPath.Replace("WindowControl.cs", "ScriptableObjectDataOfAll.asset"));
            if (oldData != null)
            {
                ArtToolBoxGlobalData globalData = null;
                globalData = GetScriptableObjectDataArtToolBox.UpdateData(globalData);
                ArtToolBoxLocalData localData = null;
                localData = GetScriptableObjectDataArtToolBox.UpdateData(localData);
                globalData.presupposeSelect = oldData.PresupposeIndex;
                globalData.presupposeList = oldData.PresupposesList;
                globalData.standardPath = oldData.SelectPath;
                globalData.shaderPools = oldData.shaderPools;
                localData.folderPathData = oldData.FolderPath;
                localData.scenePathData = oldData.ScenePath;

                EditorUtility.SetDirty(localData);
                EditorUtility.SetDirty(globalData);
                AssetDatabase.DeleteAsset(scriptPath.Replace("WindowControl.cs", "ScriptableObjectDataOfAll.asset"));
            }



            baseShow = new BaseShow();
            wantsMouseMove = true;
            wantsMouseEnterLeaveWindow = true;

            skin = Resources.Load<GUISkin>("AllSkin");
            minSize = new Vector2(400, 400);
            Instance = this;
            scene = SceneManager.GetActiveScene();
            isPlaying = false;
            Submit.ArtToolkitData.Instance.Close();

        }


        private void AllShow()
        {

            GUILayout.BeginArea(new Rect(0, 0, 800, 90));
            GUILayout.Label("—————————————————————————————————————————————————————————————————————————————————————————————————————————————————————");
            scroll2 = EditorGUILayout.BeginScrollView(scroll2, false, false);
            baseShow.MainInterfaceShow();
            GUILayout.EndScrollView();
            GUILayout.Label("————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————");
            GUILayout.EndArea();
            //2
            GUILayout.BeginArea(new Rect(0, 90, 95, position.height - 100));
            scroll1 = GUILayout.BeginScrollView(scroll1, false, false);
            baseShow.ToolListShow();
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            ShowLine();

            GUILayout.BeginArea(new Rect(new Vector2(100, 100), new Vector2(position.width - 100, position.height - 100)));
            baseShow.ToolsInterfaceShow(mouseOverWindow, this.position);
            GUILayout.EndArea();
        }


        private void OnGUI()
        {
            //美化
            if (skin == null) skin = Resources.Load<GUISkin>("AllSkin");
            //GUIStyle priButton = GUI.skin.button;
            //GUIStyle priToggle = GUI.skin.toggle;
            //GUIStyle priText = GUI.skin.textField;
            //GUIStyle priHor = GUI.skin.horizontalSlider;
            //GUIStyle priHorThumb = GUI.skin.horizontalSliderThumb;

            //GUI.skin.button = skin.button;
            //GUI.skin.toggle = skin.toggle;
            //GUI.skin.textField = skin.textField;
            //GUI.skin.horizontalSlider = skin.horizontalSlider;
            //GUI.skin.horizontalSliderThumb = skin.horizontalSliderThumb;

            AllShow();
            //GUI.skin.button = priButton;
            //GUI.skin.toggle = priToggle;
            //GUI.skin.textField = priText;
            //GUI.skin.horizontalSlider = priHor;
            //GUI.skin.horizontalSliderThumb = priHorThumb;

        }

        private void Update()
        {
            if (isPlaying != Application.isPlaying || scene != SceneManager.GetActiveScene())
            {
                baseShow.Reset();
                isPlaying = Application.isPlaying;
                scene = SceneManager.GetActiveScene();
            }
            Repaint();
            if (this != Instance) Debug.Log("Instance!=this");
        }

        public void SetTool(Tool tool)
        {
            baseShow.tool = tool;
        }
        public void setAbout(About about)
        {
            //Author : 宁殿曜 at 诗悦网络
            baseShow.state = about;
        }

        private void OnDisable()
        {
            baseShow.Disable();
            Submit.ArtToolkitData.Instance.Close();
            Instance = null;
        }


        private void ShowLine()
        {
            GUILayout.BeginArea(new Rect(90, 90, 10, 300));
            {
                GUILayout.Label("|"); GUILayout.Label("|"); GUILayout.Label("|");
                GUILayout.Label("|"); GUILayout.Label("|"); GUILayout.Label("|");
                GUILayout.Label("|"); GUILayout.Label("|"); GUILayout.Label("|");
                GUILayout.Label("|"); GUILayout.Label("|"); GUILayout.Label("|");
                GUILayout.Label("|"); GUILayout.Label("|"); GUILayout.Label("|");
                GUILayout.Label("|"); GUILayout.Label("|"); GUILayout.Label("|");
            }
            GUILayout.EndArea();
        }
    }

}
