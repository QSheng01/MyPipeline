using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace SYArtToolkit
{
    public class DealModelSet : ToolInterface
    {
        private List<Object> objects;
        private bool uvs;
        public DealModelSet()
        {
            objects = new List<Object>();
            uvs = true;
        }

        private Vector2 v2;
        public override void InterfaceShow(bool atThisWindow, Rect position)
        {

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            if (GUILayout.Button("一键选择物体"))
            {
                foreach (var o in Selection.objects)
                {
                    if (o != null)
                        objects.Add(o);
                }
            }
            if (GUILayout.Button("一键清空物体"))
            {
                objects.Clear();
            }
            GUILayout.Space(20);
            uvs = GUILayout.Toggle(uvs, "包含2u(Lightmap Uvs)");

            if (GUILayout.Button("一键去除模型的材质球,骨骼,动作"))
            {
                foreach (var o in objects)
                {
                    var path = AssetDatabase.GetAssetPath(o);
                    if (string.IsNullOrEmpty(path)) continue;
                    var importer3 = AssetImporter.GetAtPath(path) as ModelImporter;
                    if (importer3 != null)
                    {
                        importer3.importAnimation = false;
                        importer3.animationType = ModelImporterAnimationType.None;
                        importer3.materialImportMode = ModelImporterMaterialImportMode.None;
                        importer3.generateSecondaryUV = uvs;
                        importer3.SaveAndReimport();
                    }


                }
            }
            GUILayout.Button("一键改名(制作ing)");


            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.Label("选中的物体");
            v2 = GUILayout.BeginScrollView(v2);

            if (objects == null || objects.Count == 0)
            {

                EditorGUILayout.ObjectField(null, typeof(Object), false);

            }
            else
            {

                foreach (var o in objects)
                {
                    EditorGUILayout.ObjectField(o, typeof(Object), false);
                }
            }
            GUILayout.EndScrollView();

            GUILayout.EndVertical();




            GUILayout.EndHorizontal();




        }


    }

}