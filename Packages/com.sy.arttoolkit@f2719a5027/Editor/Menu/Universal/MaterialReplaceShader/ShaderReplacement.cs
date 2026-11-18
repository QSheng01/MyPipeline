using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SYArtToolkit
{

    public class ShaderReplacement : ToolInterface
    {


        public Shader originShader;
        public Shader replacementShader;
        public DefaultAsset defaultAsset;


        public ShaderReplacement()
        {

        }

        public override void InterfaceShow(bool isBool, Rect rect)
        {
            GUILayout.Label("替换会更新原始资源,造成不可逆的后果");
            GUILayout.Space(10);
            originShader = EditorGUILayout.ObjectField("初始的Shader", originShader, typeof(Shader), true) as Shader;

            replacementShader = EditorGUILayout.ObjectField("替换的Shader", replacementShader, typeof(Shader), true) as Shader;

            GUILayout.Space(10);

            defaultAsset = EditorGUILayout.ObjectField("", defaultAsset, typeof(DefaultAsset), false) as DefaultAsset;
            GUILayout.Label("以上文件夹中的所有满足条件的材质球都会被替换shader");

            GUILayout.Space(10);
            if (GUILayout.Button("替换"))
            {
                if (!CheckVaild())
                {
                    Debug.Log("请检查输入是否有效");
                    return;
                }
                DO();

            }
        }



        private bool CheckVaild()
        {
            if (originShader == null || replacementShader == null || defaultAsset == null) return false;
            if (Directory.Exists(AssetDatabase.GetAssetPath(defaultAsset)))
            {
                return true;
            }
            return false;
        }

        private void DO()
        {
            string foldPath = AssetDatabase.GetAssetPath(defaultAsset);
            var files = Directory.GetFiles(foldPath, "*.mat", SearchOption.AllDirectories);
            List<(Material mat, string path)> materials = new List<(Material mat, string path)>();
            foreach (var item in files)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(item);
                if (mat == null || mat.shader != originShader) continue;
                materials.Add((mat, item));
            }
            Submit.ArtToolkitData.Instance.AddAssetRef(materials.Count);
            foreach (var item in materials)
            {
                item.mat.shader = replacementShader;
                Debug.Log(item.path + "替换成功");

            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

    }

}