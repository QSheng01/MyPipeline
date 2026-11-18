
using SYArtToolkit.Submit;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace SYArtToolkit
{

    public class PerformanceWindow : ToolInterface
    {
        private ArtToolBoxGlobalData data;
        public static GUIStyle style = new GUIStyle();
        private Vector2 scroll;
        private bool fold;
        AddOverDrawTest addOverDrawTest;

        public class ListGameObject : ScriptableObject
        {
            public List<GameObject> gameObjects;

            public ListGameObject()
            {
                gameObjects = new List<GameObject>();
            }
        }//都删除

        //private PerformanceDetectionClass performanceDetectionClass;
        private ListGameObject gameobjs;
        private SerializedObject serobj;
        private SerializedProperty serpro;
        private ReorderableList reorderableList;
        private ReorderableList reorderableListEmpty;
        private Vector2 position;

        public override void InterfaceShow(bool noBool, Rect winPosition)
        {
            position = winPosition.position;
            data = GetScriptableObjectDataArtToolBox.UpdateData(data);
            if (gameobjs == null)
            {
                gameobjs = ScriptableObject.CreateInstance<ListGameObject>();
                serobj = new SerializedObject(gameobjs);
                serpro = serobj.FindProperty("gameObjects");
            }

            if (data.presupposeList.Count == 0) data.presupposeList.Add(new Presuppose());
            if (data.presupposeSelect == null) data.presupposeSelect = data.presupposeList[0];
            GUILayout.Space(10);
            EditorGUILayout.PropertyField(serpro, new GUIContent("请输入物体"));
            serobj.ApplyModifiedProperties();

            if (fold) reorderableList.DoLayoutList();
            else reorderableListEmpty.DoLayoutList();
            EditorUtility.SetDirty(data);
            try
            {
                if (ArtToolKitSetting.Instance.projectID == "长安幻想")
                    addOverDrawTest.UpdatePerFrame(gameobjs.gameObjects, data);
            }
            catch
            {

            }

            scroll = EditorGUILayout.BeginScrollView(scroll, false, false);

            PerformanceDetectionClass.Instance.UpdateData(gameobjs.gameObjects, data.presupposeSelect, data.presupposeList);
            PerformanceDetectionClass.Instance.Show();

            GUILayout.EndScrollView();
        }

        public PerformanceWindow()
        {
            if (ArtToolKitSetting.Instance.projectID == "长安幻想")
                addOverDrawTest = new AddOverDrawTest();
            style.fontStyle = FontStyle.Italic;
            gameobjs = ScriptableObject.CreateInstance<ListGameObject>();

            serobj = new SerializedObject(gameobjs);
            serpro = serobj.FindProperty("gameObjects");

            data = GetScriptableObjectDataArtToolBox.UpdateData(data);
            reorderableList = new ReorderableList(data.presupposeList, typeof(Presuppose), true, true, true, true);
            reorderableList.drawHeaderCallback = (Rect rect) =>
            {
                fold = EditorGUI.BeginFoldoutHeaderGroup(rect, fold, "");
                EditorGUI.EndFoldoutHeaderGroup();
                //if (GUI.Button(new Rect(rect.x+15,rect.y,rect.width-15,rect.height), ""))
                //{
                //    fold = !fold;
                //}
                float width = rect.width / 2;
                GUI.Label(new Rect(rect.x + 15, rect.y, rect.width, rect.height), "当前预设: ");
                GUI.Label(new Rect(rect.x + width, rect.y, width, rect.height), data.presupposeSelect.name);
            };
            reorderableList.drawElementCallback = (Rect rect, int index, bool a, bool b) =>
            {
                GUI.Label(rect, data.presupposeList[index].name);
                if (b)
                {
                    float width = rect.width / 4;
                    float rectX = rect.x + rect.width / 2;

                    if (GUI.Button(new Rect(rectX, rect.y, width - 5, rect.height), "选择"))
                    {
                        data.presupposeSelect = data.presupposeList[index];
                    }

                    if (GUI.Button(new Rect(rectX + width + 5, rect.y, width - 5, rect.height), "修改"))
                    {

                        SetPerformancePreSupportWindow.GetPreSWindow(data.presupposeList[index],
                             position + new Vector2(150, 100));
                    }

                }
            };
            reorderableList.onAddCallback = (ReorderableList list) =>
            {
                data.presupposeList.Add(new Presuppose());
            };


            reorderableListEmpty = new ReorderableList(data.presupposeList, typeof(Presuppose), false, true, false, false);
            reorderableListEmpty.drawHeaderCallback = (Rect rect) =>
            {

                fold = EditorGUI.BeginFoldoutHeaderGroup(rect, fold, "");
                EditorGUI.EndFoldoutHeaderGroup();
                float width = rect.width / 2;
                GUI.Label(new Rect(rect.x + 15, rect.y, rect.width, rect.height), "当前预设: ");
                GUI.Label(new Rect(rect.x + width, rect.y, width, rect.height), data.presupposeSelect.name);
            };
            reorderableListEmpty.drawElementCallback = (Rect rect, int index, bool a, bool b) => { };
            reorderableListEmpty.elementHeight = 0;
        }
        //

        public override void Exit()
        {
            try
            {
                if (addOverDrawTest != null)
                    addOverDrawTest.Destroy();
            }
            catch { }
        }
    }

}