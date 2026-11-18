
using SYArtToolkit;
using UnityEditor;
using UnityEngine;

public class ExtraPlugin : ToolInterface
{
    string unityPackagesPath;
    public ExtraPlugin()
    {
        var df = Resources.Load<Object>("AllSkin");
        if (df != null)
        {
            unityPackagesPath = AssetDatabase.GetAssetPath(df);
            unityPackagesPath = unityPackagesPath.Replace("AllSkin.guiskin", "MaterialController.unitypackage");
        }
    }



    public override void InterfaceShow(bool atThisWindow, Rect position)
    {

        if (unityPackagesPath != null)
        {
            if (GUILayout.Button("µº»ÎMaterialController", GUILayout.Height(200)))
            {
                AssetDatabase.ImportPackage(unityPackagesPath, false);
            }
        }
        else
        {

            var df = Resources.Load<DefaultAsset>("MaterialController");
            if (df != null) unityPackagesPath = AssetDatabase.GetAssetPath(df);
        }

    }

}
