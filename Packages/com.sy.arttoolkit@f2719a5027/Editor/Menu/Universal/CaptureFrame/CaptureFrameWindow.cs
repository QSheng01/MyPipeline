using UnityEditor;
using UnityEngine;


namespace SYArtToolkit.CaptureFrame
{


    public class CaptureFrameWindow : ToolInterface
    {
        public override void InterfaceShow(bool atThisWindow, Rect position)
        {

            if (GUILayout.Button("开始设置截屏功能", GUILayout.Height(60)))
            {
                var gg = new GameObject("ScreenFrameCaptureGameObject");
                gg.AddComponent<DepthAndColorCapture>();

                SYArtToolkit.Submit.ArtToolkitData.Instance.AddConvenient(1);
                Selection.activeObject = gg;
                EditorGUIUtility.PingObject(gg);
            }






        }

    }

}