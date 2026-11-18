using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace SYArtToolkit
{

    public class MaterialCleaner
    {
        //资源类型过滤匹配字符
        private static string[] searchPattern = new string[] { "t:Material" };

        //private static int clearCount = 0;

        public static async Task ClearMaterial(string[] searchInFolders, List<KeyValuePair<Material, List<string>>> dic)
        {
            dic.Clear();
            //clearCount = 0;
            string[] guids = AssetDatabase.FindAssets("t:Material", searchInFolders);
            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Material)) as Material;
                if (mat != null)
                {
                    ClearMaterial(mat, dic);
                    await Task.Yield();
                    //Resources.UnloadAsset(mat);
                }
            }


            //EditorUtility.DisplayProgressBar("Progress Bar", assetPath, (float)assetIndex / assetPaths.Count);
            //EditorUtility.ClearProgressBar();
        }

        public static void ClearMaterial(Material mat, List<KeyValuePair<Material, List<string>>> dic = null)
        {
            Shader shader = mat.shader;
            SerializedObject matSer = new SerializedObject(mat);
            SerializedProperty emissionProperty = matSer.FindProperty("m_SavedProperties");
            SerializedProperty texEnvs = emissionProperty.FindPropertyRelative("m_TexEnvs");
            List<string> matTexPropNames = new List<string>();
            List<string> shaderTexPropNames = new List<string>();
            List<string> redundancyTexNames = redundancyTexNames = new List<string>();
            SerializedProperty secondProp = null;
            SerializedProperty m_TexNameProp = null;
            string texName = "";
            foreach (SerializedProperty property in texEnvs)
            {
                secondProp = property.FindPropertyRelative("second");
                m_TexNameProp = property.FindPropertyRelative("m_Texture");
                texName = property.displayName;
                matTexPropNames.Add(texName);
            }
            if (secondProp != null)
            {
                secondProp.Dispose();
            }
            if (m_TexNameProp != null)
            {
                m_TexNameProp.Dispose();
            }
            for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
            {
                string propertyName = ShaderUtil.GetPropertyName(shader, i);
                shaderTexPropNames.Add(propertyName);
            }
            foreach (var propName in matTexPropNames)
            {
                if (!shaderTexPropNames.Contains(propName))
                {
                    redundancyTexNames.Add(propName);
                }
            }
            if (dic != null)
                dic.Add(new KeyValuePair<Material, List<string>>(mat, redundancyTexNames));

            //ClearMaterialSerializedProperty(texEnvs, mat, redundancyTexNames);
            //matSer.ApplyModifiedProperties();
            //EditorUtility.SetDirty(mat);
            //AssetDatabase.SaveAssets();
        }


        public static void ClearMaterial(List<KeyValuePair<Material, List<string>>> dic)
        {
            float assetIndex = 0;
            foreach (var kv in dic)
            {
                EditorUtility.DisplayProgressBar("Progress Bar", kv.Key.ToString() + "--", (float)assetIndex / dic.Count);

                SerializedObject matSer = new SerializedObject(kv.Key);
                SerializedProperty emissionProperty = matSer.FindProperty("m_SavedProperties");
                SerializedProperty texEnvs = emissionProperty.FindPropertyRelative("m_TexEnvs");
                ClearMaterialSerializedProperty(texEnvs, kv.Key, kv.Value);
                matSer.ApplyModifiedProperties();
                assetIndex++;
                EditorUtility.SetDirty(kv.Key);
            }
            Debug.Log("一共清除了" + assetIndex + "个材质球");
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ClearMaterialSerializedProperty(SerializedProperty property, Material mat, List<string> redundancyTexNames)
        {
            for (int j = property.arraySize - 1; j >= 0; j--)
            {
                string propertyName = property.GetArrayElementAtIndex(j).FindPropertyRelative("first").stringValue;
                if (redundancyTexNames.Contains(propertyName))
                {
                    property.DeleteArrayElementAtIndex(j);
                    Debug.Log(mat.name + " 清除无效贴图依赖 : " + propertyName);
                }
            }

        }
    }
}