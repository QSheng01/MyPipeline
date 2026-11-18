using UnityEditor;
using UnityEngine;

namespace SYArtToolkit
{

    public class SetPerformancePreSupportWindow : PopupWindowContent
    {
        private static GUIStyle texField = new GUIStyle(Resources.Load<GUISkin>("chineseInput").textField);
        public static void GetPreSWindow(Presuppose pres, Vector2 start)
        {
            //SetPerformancePreSupportWindow psw = GetWindow<SetPerformancePreSupportWindow>(_pres.name);
            //psw.ShowPopup();

            //psw.position = new Rect(Event.current.mousePosition + start, new Vector2(200, 200));
            //psw.minSize = new Vector2(200, 150);


            PopupWindow.Show(new Rect(new Vector2(300, 0), new Vector2(300, 200)), new SetPerformancePreSupportWindow(pres));
            //psw.pres = _pres;
            //psw.iname = _pres.name;
            //psw.meshNum = _pres.meshNum;
            //psw.particleNum = _pres.particleNum;
            //psw.skinMesh = _pres.skinMesh;
            //psw.textureMemory = _pres.skinMesh;
            //psw.textureNum = _pres.textureNum;
            //psw.trisNum = _pres.trisNum;
            //psw.vertexNum = _pres.vertexNum;
            //psw.particleSystemNum = _pres.particleSystemNum;
            //psw.overdraw=_pres.overdraw;
            //psw.glob=_pres.glob;
        }


        private SetPerformancePreSupportWindow(Presuppose pres)
        {
            this.pres = pres;
            iname = pres.name;
            meshNum = pres.meshNum;
            particleNum = pres.particleNum;
            skinMesh = pres.skinMesh;
            textureMemory = pres.skinMesh;
            textureNum = pres.textureNum;
            trisNum = pres.trisNum;
            vertexNum = pres.vertexNum;
            particleSystemNum = pres.particleSystemNum;
            overdraw = pres.overdraw;
            glob = pres.glob;


        }


        private Presuppose pres;
        private string iname = "默认";
        private int vertexNum = 0;
        private int trisNum = 0;
        private int particleSystemNum = 0;
        private int particleNum = 0;
        private int meshNum = 0;
        private int skinMesh = 0;
        private float textureMemory = 0;
        private int textureNum = 0;
        private float overdraw = 0f;
        private string glob = "";
        public override void OnGUI(Rect rect)
        {
            iname = EditorGUILayout.TextField(iname, texField);
            vertexNum = EditorGUILayout.IntField("顶点数量: ", vertexNum);
            trisNum = EditorGUILayout.IntField("面数", trisNum);
            particleSystemNum = EditorGUILayout.IntField("粒子系统", particleSystemNum);
            particleNum = EditorGUILayout.IntField("粒子数", particleNum);
            // meshNum = EditorGUILayout.IntField("mesh数量", meshNum);
            // skinMesh = EditorGUILayout.IntField("skinMesh数量", skinMesh);
            // textureMemory = EditorGUILayout.FloatField("贴图内存(MB): ", textureMemory);
            textureNum = EditorGUILayout.IntField("贴图数量", textureNum);
            overdraw = EditorGUILayout.FloatField("Overdraw", overdraw);
            glob = EditorGUILayout.TextField(glob);
            GUILayout.BeginHorizontal();

            GUILayout.Label("");
            if (GUILayout.Button("保存"))
            {
                pres.name = iname;
                pres.vertexNum = vertexNum;
                pres.trisNum = trisNum;
                pres.meshNum = meshNum;
                pres.particleNum = particleNum;
                pres.skinMesh = skinMesh;
                pres.textureMemory = textureMemory;
                pres.particleSystemNum = particleSystemNum;
                pres.textureNum = textureNum;
                pres.overdraw = overdraw;
                pres.glob = glob;
                editorWindow.Close();
            }

            GUILayout.EndHorizontal();
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(300, 200);
        }
    }
}

