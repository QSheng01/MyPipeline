#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SYArtToolkit.BatchesGamma
{

    public class BatchesGamma : EditorWindow
    {
        [MenuItem("美术工具箱/批量设置Gamma")]
        static void Opne()
        {
            GetWindow<BatchesGamma>().Show();
        }
        Vector2 v2;

        DefaultAsset folder = null;

        string glob = "**";

        float gamma = 2.2f;

        public List<Texture2D> texs = new List<Texture2D>();

        SerializedProperty serializedProperty;
        SerializedObject serializedObject;

        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);
            serializedProperty = serializedObject.FindProperty("texs");
        }


        private void OnGUI()
        {


            folder = EditorGUILayout.ObjectField("选择文件夹:", folder, typeof(DefaultAsset), false) as DefaultAsset;

            glob = EditorGUILayout.DelayedTextField("选择匹配字符串:", glob);

            gamma = EditorGUILayout.Slider("Gamma:", gamma, 0f, 5.0f);

            if (GUILayout.Button("点击查看所有满足条件的图片"))
            {
                texs.Clear();

                var gs = AssetDatabase.FindAssets("glob:\"" + glob + "\" t:Texture", new string[] { AssetDatabase.GetAssetPath(folder) });
                foreach (var g in gs)
                {
                    Debug.Log(gs.Length);
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(g));
                    if (tex != null)
                    {
                        texs.Add(tex);
                    }

                }
                serializedObject = new SerializedObject(this);
                serializedProperty = serializedObject.FindProperty("texs");
            }


            v2 = EditorGUILayout.BeginScrollView(v2);
            EditorGUILayout.PropertyField(serializedProperty);
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.EndScrollView();



            if (GUILayout.Button("一键设置"))
            {

                Debug.Log(texs.Count);
                foreach (var g in texs)
                {


                    bool flag = false;
                    //if (!g.isReadable)
                    //{
                    //    var ti = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(g)) as TextureImporter;
                    //    ti.isReadable = true;
                    //    EditorUtility.SetDirty(ti);
                    //    AssetDatabase.SaveAssetIfDirty(ti);
                    //    AssetDatabase.Refresh();

                    //}
                    var xx = new Texture2D(g.width, g.height);
                    var cs = g.GetPixels();
                    for (int i = 0; i < cs.Length; ++i)
                    {
                        cs[i] = new Color(Mathf.Pow(cs[i].r, gamma), Mathf.Pow(cs[i].g, gamma), Mathf.Pow(cs[i].b, gamma), cs[i].a);
                    }
                    xx.SetPixels(cs);
                    string path = AssetDatabase.GetAssetPath(g);

                    if (path.EndsWith(".tga")) { flag = true; }

                    if (flag)
                    {
                        System.IO.File.WriteAllBytes(path, xx.EncodeToTGA());

                    }
                    else
                    {
                        System.IO.File.WriteAllBytes(path.Substring(0, path.Length - 3) + ".png", xx.EncodeToPNG());
                    }

                    //if (flag)
                    //{

                    //    var ti = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(g)) as TextureImporter;
                    //    ti.isReadable = false;
                    //    EditorUtility.SetDirty(ti);
                    //    AssetDatabase.SaveAssetIfDirty(ti);
                    //    AssetDatabase.Refresh();
                    //}



                }



            }

        }



    }



}
#endif