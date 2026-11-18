using System.IO;
using UnityEditor;
using UnityEngine;


namespace SYArtToolkit
{
    public class EasyCreateMaterial : ToolInterface
    {

        public EasyCreateMaterial()
        {
            string shaderPath = EditorPrefs.GetString("SYEngineEasyCeateMaterial");
            shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);

        }

        public override void InterfaceShow(bool isBool, Rect rect)
        {

            shader = EditorGUILayout.ObjectField(shader, typeof(Shader), false) as Shader;
            if (GUILayout.Button("确定"))
            {
                if (shader != null)
                {
                    string shaderPath = AssetDatabase.GetAssetPath(shader);

                    EditorPrefs.SetString("SYEngineEasyCeateMaterial", shaderPath);
                    Debug.Log("更改成功");
                }
            }
        }

        [MenuItem("Assets/Create/新建特效通用材质", true, 299)]
        private static bool CheckIsFolder()
        {
            var def = Selection.activeObject;
            return Directory.Exists(AssetDatabase.GetAssetPath(def as DefaultAsset));

        }


        [MenuItem("Assets/Create/新建特效通用材质", false, 299)]
        private static void Do()
        {
            var def = Selection.activeObject;
            var savePath = AssetDatabase.GetAssetPath(def);
            string shaderPath = EditorPrefs.GetString("SYEngineEasyCeateMaterial");
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            if (shader == null) shader = Shader.Find("XianXia/Effect/EffectUniversal");
            if (shader == null)
            {
                Debug.Log("找不到" + shaderPath);
                return;
            }
            var mat = new Material(shader);
            string name = AssetDatabase.GetAssetPath(shader);
            string shaderPath1 = UnityEditor.AssetDatabase.GetAssetPath(shader);
            string shaderFileName = System.IO.Path.GetFileNameWithoutExtension(shaderPath1);
            mat.name = shaderFileName;
            if ((File.Exists(string.Format("{0}/{1}.mat", savePath, shaderFileName))))
            {
                int index = 0;
                mat.name = shaderFileName + index;
                while (File.Exists(string.Format("{0}/{1}.mat", savePath, mat.name)))
                {
                    ++index;
                    mat.name = shaderFileName + index;
                }
            }
            AssetDatabase.CreateAsset(mat, string.Format("{0}/{1}.mat", savePath, mat.name));
            Debug.Log(string.Format("{0}/{1}.mat", savePath, mat.name));
            AssetDatabase.Refresh();
            Submit.ArtToolkitData.Instance.AddConvenient(1);
        }


        private Shader shader;



    }


}