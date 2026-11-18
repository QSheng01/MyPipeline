using UnityEditor;
using UnityEngine;

namespace SYArtToolkit
{

    public class VFX_CustomizeRightButton
    {
        [MenuItem("GameObject/快速创建/", false, 11)]
        static public void CreatePar0()
        {
            string name = "";
            GameObject vfx = GameObject.Instantiate(AssetDatabase.LoadAssetAtPath("", typeof(Object)) as GameObject);
            if (Selection.gameObjects.Length >= 1)
            {
                vfx.transform.parent = Selection.gameObjects[0].transform;
            }

            if (name == "")
            {
                string vfxpath = "";
                string[] temp = vfxpath.Split('/');
                string[] temp1 = temp[temp.Length - 1].Split('.');
                name = temp1[0];
            }

            vfx.name = name;
            Selection.activeObject = vfx;
            EditorGUIUtility.PingObject(vfx);
        }
    }
}