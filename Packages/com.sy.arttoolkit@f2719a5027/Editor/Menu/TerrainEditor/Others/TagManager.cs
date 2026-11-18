

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace SYArtToolkit
{

    public class TagManagerAutoAdd
    {
        #region 属性

        private Dictionary<GameObject, int> Layer = new Dictionary<GameObject, int>();
        private int currentLayer = -1;
        private static TagManagerAutoAdd instance;

        #endregion

        #region 公共方法


        public static TagManagerAutoAdd Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TagManagerAutoAdd();
                }

                return instance;
            }
        }

        public int layer()
        {
            return currentLayer;
        }

        public void createLayer(string x)
        {
            currentLayer = AutoAddLayer(x);
        }

        public void deletelayer()
        {
            if (currentLayer == -1) return;
            AutoDeleteLayer(currentLayer);
            currentLayer = -1;
        }

        public void setObj(GameObject obj)
        {
            Transform[] ts = obj.GetComponentsInChildren<Transform>();

            Layer.Clear();

            foreach (var t in ts)
            {
                Layer.Add(t.gameObject, t.gameObject.layer);
                t.gameObject.layer = currentLayer;
            }

        }

        public void reserObj()
        {
            foreach (var l in Layer)
            {
                l.Key.layer = l.Value;
            }

            Layer.Clear();
        }


        #endregion

        #region 私有方法

        private void AutoDeleteLayer(int i)
        {
            if (i < 0 && i > 31) return;
            SerializedObject tagManager =
                new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Tagmanager.asset"));
            SerializedProperty it = tagManager.GetIterator();
            while (it.NextVisible(true))
            {
                if (it.name.Equals("layers"))
                {
                    SerializedProperty sp = it.GetArrayElementAtIndex(i);
                    sp.stringValue = string.Empty;
                    tagManager.ApplyModifiedProperties();
                }
            }
        }

        private int AutoAddLayer(string layer)
        {
            if (!HasThisLayer(layer))
            {
                SerializedObject tagMagager =
                    new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Tagmanager.asset"));
                SerializedProperty it = tagMagager.GetIterator();
                while (it.NextVisible(true))
                {
                    if (it.name.Equals("layers"))
                    {
                        for (int i = 0; i < it.arraySize; i++)
                        {
                            if (i <= 7)
                            {
                                continue;
                            }

                            SerializedProperty sp = it.GetArrayElementAtIndex(i);
                            if (string.IsNullOrEmpty(sp.stringValue))
                            {
                                sp.stringValue = layer;
                                tagMagager.ApplyModifiedProperties();
                                return i;
                            }
                        }
                    }
                }
            }

            return -1;
        }

        private bool HasThisLayer(string layer)
        {
            //先清除已保存的
            SerializedObject tagManager =
                new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Tagmanager.asset"));
            SerializedProperty it = tagManager.GetIterator();
            while (it.NextVisible(true))
            {
                if (it.name.Equals("layers"))
                {
                    for (int i = 0; i < it.arraySize; i++)
                    {
                        if (i <= 7)
                        {
                            continue;
                        }

                        SerializedProperty sp = it.GetArrayElementAtIndex(i);
                        if (!string.IsNullOrEmpty(sp.stringValue))
                        {
                            if (sp.stringValue.Equals(layer))
                            {
                                sp.stringValue = string.Empty;
                                tagManager.ApplyModifiedProperties();
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < UnityEditorInternal.InternalEditorUtility.layers.Length; i++)
            {
                if (UnityEditorInternal.InternalEditorUtility.layers[i].Contains(layer))
                {
                    return true;
                }
            }

            return false;
        }


        #endregion

    }
}