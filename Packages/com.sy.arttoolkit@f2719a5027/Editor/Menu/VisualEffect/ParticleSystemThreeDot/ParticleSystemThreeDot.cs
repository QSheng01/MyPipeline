using UnityEngine;

namespace SYArtToolkit
{
    class ParticleSystemThreeDot : ToolInterface
    {
        Texture2D tex;
        GUIStyle style;

        public ParticleSystemThreeDot()
        {
            tex = Resources.Load<Texture2D>("ParticleSystemThreeDot");
            style = new GUIStyle();
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = Color.white;
            style.fontSize = 18;
        }

        public override void InterfaceShow(bool noBool, Rect noRect)
        {
            GUILayout.Label("如果使用特效通用材质并且使用自定义数据", style);
            GUILayout.Label("可以通过以下设置自动添加", style);
            GUILayout.Space(30);
            if (tex != null)
            {
                GUILayout.Label(tex);
            }
        }


    }


}