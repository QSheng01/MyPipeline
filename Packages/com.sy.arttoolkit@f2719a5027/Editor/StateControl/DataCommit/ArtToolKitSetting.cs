using System;
using UnityEditor;
using UnityEngine;


namespace SYArtToolkit.Submit
{
    [CreateAssetMenu(fileName = "ArtToolKitSetting", menuName = "ScriptableObject/子弹数据", order = 0)]
    [Serializable]
    public class ArtToolKitSetting : ScriptableObject
    {
        public string saveDataPath = "Assets/SYPackages/ArtToolBox/Editor/ArtToolkitDataJson.txt";

        public bool artToolKitDeveloperMode = false;

        //private ArtToolKitSetting(){}

        private static ArtToolKitSetting instance;

        public static ArtToolKitSetting Instance
        {
            get
            {
                if (instance == null)
                {

                    var guids = AssetDatabase.FindAssets("glob:\"Packages/*/ArtToolKitSetting.asset\" t:ArtToolKitSetting", new string[] { "Packages" });
                    if (guids != null && guids.Length > 0)
                    {
                        instance = AssetDatabase.LoadAssetAtPath<ArtToolKitSetting>(
                            AssetDatabase.GUIDToAssetPath(guids[0]));
                    }

                    if (instance == null)
                    {

                        var guidss = AssetDatabase.FindAssets("t:ArtToolKitSetting");
                        if (guidss != null && guidss.Length > 0)
                        {
                            instance = AssetDatabase.LoadAssetAtPath<ArtToolKitSetting>(
                                AssetDatabase.GUIDToAssetPath(guidss[0]));
                        }
                    }

                    if (instance == null) instance = ScriptableObject.CreateInstance<ArtToolKitSetting>();
                }
                return instance;
            }
        }


        [SerializeField] public string projectID = "未知";




    }


    [CustomEditor(typeof(ArtToolKitSetting))]
    public class ProjectSetEditor : Editor
    {

        public static string[] IDEnum = new string[]
        {
            "未知",
            "闪烁之光",
            "元素",
            "仙凡幻想",
            "长安幻想",
            "石器",
            "勇士与宝藏小队",
            "机战",
            "云上城之歌",
            "望月",
            "蔚蓝",
            "永夜",
            "凤凰",
            "暮光",
            "元素",
            "修仙"
        };

        public override void OnInspectorGUI()
        {
            ArtToolKitSetting myObject = (ArtToolKitSetting)target;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("项目ID:");
            if (EditorGUILayout.DropdownButton(new GUIContent(myObject.projectID), FocusType.Passive))
            {
                GenericMenu _menu = new GenericMenu();
                foreach (var id in IDEnum)
                {
                    _menu.AddItem(new GUIContent(id), id == myObject.projectID, OnValueSelected, id);
                }
                _menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();

        }

        private void OnValueSelected(object value)
        {

            ArtToolKitSetting myObject = (ArtToolKitSetting)target;
            myObject.projectID = (string)value;
            EditorUtility.SetDirty(myObject);
            AssetDatabase.SaveAssets();

        }
    }

    // // public enum IdEnum
    // // {
    //   "闪烁之光")]
    // //     sszg = 2,
    //   "元素")]
    // //     ys = 4,
    //   "仙凡幻想")]
    // //     xfhx = 5,
    //   "长安幻想")]
    // //     cahx = 6,
    //   "石器")]
    // //     sq = 7,
    //   "勇士与宝藏小队")]
    // //     ysybaxd = 9,
    //   "机战")]
    // //     jz = 10,
    //   "云上城之歌")]
    // //     ysczg = 11,
    //   "望月")]
    // //     wy = 12,
    //   "蔚蓝")]
    // //     wl = 13,
    //   "永夜")]
    // //     yl = 15,
    //   "凤凰")]
    // //     fh = 22,
    //   "暮光")]
    // //     mg = 23
    // // }
}