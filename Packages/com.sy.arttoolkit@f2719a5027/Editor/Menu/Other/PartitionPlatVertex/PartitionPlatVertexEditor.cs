
using UnityEditor;
using UnityEngine;

namespace SYArtToolkit
{

    public class PartitionPlatVertexEditor : ToolInterface
    {
        public override void InterfaceShow(bool atThisWindow, Rect position)
        {
            if (GUILayout.Button("开启分块顶点查看工具"))
            {
                var gameObject = new GameObject();
                gameObject.AddComponent<PartitionPlatVertex>();
                Selection.activeObject = gameObject;

            }

            if (GUILayout.Button("摧毁场景中所有分块顶点查看工具"))
            {
                var ps = UnityEngine.Object.FindObjectsOfType<PartitionPlatVertex>();
                foreach (var p in ps)
                {
                    Object.DestroyImmediate(p.gameObject);
                }

            }

        }
    }
}