using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace SYArtToolkit
{

    public class GetScriptableObjectDataArtToolBox
    {

        public static ArtToolBoxLocalData UpdateData(ArtToolBoxLocalData localData)
        {
            if (localData != null) return localData;

            localData = AssetDatabase.LoadAssetAtPath<ArtToolBoxLocalData>("Assets/SYPackages/ArtToolBox/Editor/ArtToolBoxDataLoacal.asset");

            if (localData == null)
            {
                localData = ScriptableObject.CreateInstance<ArtToolBoxLocalData>();
                //if (!Directory.Exists("Assets/SYPackages"))
                //{
                //    Directory.CreateDirectory("Assets/SYPackages");
                //}
                //if (!Directory.Exists("Assets/SYPackages/ArtToolBox"))
                //{
                //    Directory.CreateDirectory("Assets/SYPackages/ArtToolBox");
                //}
                if (!Directory.Exists("Assets/SYPackages/ArtToolBox/Editor"))
                {
                    Directory.CreateDirectory("Assets/SYPackages/ArtToolBox/Editor");
                }
                AssetDatabase.CreateAsset(localData, "Assets/SYPackages/ArtToolBox/Editor/ArtToolBoxDataLoacal.asset");
                localData = AssetDatabase.LoadAssetAtPath<ArtToolBoxLocalData>("Assets/SYPackages/ArtToolBox/Editor/ArtToolBoxDataLoacal.asset");
                AssetDatabase.Refresh();

            }
            return localData;
        }

        public static ArtToolBoxGlobalData UpdateData(ArtToolBoxGlobalData globalData)
        {
            if (globalData != null) return globalData;

            globalData = AssetDatabase.LoadAssetAtPath<ArtToolBoxGlobalData>("Assets/SYPackages/ArtToolBox/Editor/ArtToolBoxDataGlobal.asset");

            if (globalData == null)
            {
                globalData = ScriptableObject.CreateInstance<ArtToolBoxGlobalData>();
                //if (!Directory.Exists("Assets/SYPackages"))
                //{
                //    Directory.CreateDirectory("Assets/SYPackages");
                //}
                //if (!Directory.Exists("Assets/SYPackage/ArtToolBox"))
                //{
                //    Directory.CreateDirectory("Assets/SYPackage/ArtToolBox");
                //}
                if (!Directory.Exists("Assets/SYPackages/ArtToolBox/Editor"))
                {
                    Directory.CreateDirectory("Assets/SYPackages/ArtToolBox/Editor");
                }
                AssetDatabase.CreateAsset(globalData, "Assets/SYPackages/ArtToolBox/Editor/ArtToolBoxDataGlobal.asset");
                globalData = AssetDatabase.LoadAssetAtPath<ArtToolBoxGlobalData>("Assets/SYPackages/ArtToolBox/Editor/ArtToolBoxDataGlobal.asset");
                AssetDatabase.Refresh();
            }
            return globalData;

        }

        public static void SaveData(ArtToolBoxGlobalData globalData)
        {
            if (globalData == null) return;
            if (!Directory.Exists("Assets/SYPackages/ArtToolBox/Editor"))
            {
                Directory.CreateDirectory("Assets/SYPackages/ArtToolBox/Editor");
            }
            if (File.Exists("Assets/SYPackages/ArtToolBox/Editor/ArtToolBoxDataGlobal.asset"))
            {
                File.Delete("Assets/SYPackages/ArtToolBox/Editor/ArtToolBoxDataGlobal.asset");
            }

            AssetDatabase.CreateAsset(globalData, "Assets/SYPackages/ArtToolBox/Editor/ArtToolBoxDataGlobal.asset");
            AssetDatabase.Refresh();
        }

        public static void SaveData(ArtToolBoxLocalData localData)
        {
            if (localData == null) return;
            if (!Directory.Exists("Assets/SYPackages/ArtToolBox/Editor"))
            {
                Directory.CreateDirectory("Assets/SYPackages/ArtToolBox/Editor");
            }
            if (File.Exists("Assets/SYPackages/ArtToolBox/Editor/ArtToolBoxDataLocal.asset"))
            {
                File.Delete("Assets/SYPackages/ArtToolBox/Editor/ArtToolBoxDataLocal.asset");
            }

            AssetDatabase.CreateAsset(localData, "Assets/SYPackages/ArtToolBox/Editor/ArtToolBoxDataLocal.asset");
            AssetDatabase.Refresh();
        }

        public static AssetRefCache GetAssetRefCache()
        {

            var assetRefCache = AssetDatabase.LoadAssetAtPath<AssetRefCache>("Assets/SYPackages/ArtToolBox/Editor/AssetRefCache.asset");
            if (assetRefCache == null)
            {
                assetRefCache = ScriptableObject.CreateInstance<AssetRefCache>();
                //if (!Directory.Exists("Assets/SYPackages"))
                //{
                //    Directory.CreateDirectory("Assets/SYPackages");
                //}
                //if (!Directory.Exists("Assets/SYPackage/ArtToolBox"))
                //{
                //    Directory.CreateDirectory("Assets/SYPackage/ArtToolBox");
                //}
                if (!Directory.Exists("Assets/SYPackages/ArtToolBox/Editor"))
                {
                    Directory.CreateDirectory("Assets/SYPackages/ArtToolBox/Editor");
                }
                AssetDatabase.CreateAsset(assetRefCache, "Assets/SYPackages/ArtToolBox/Editor/AssetRefCache.asset");
                assetRefCache = AssetDatabase.LoadAssetAtPath<AssetRefCache>("Assets/SYPackages/ArtToolBox/Editor/AssetRefCache.asset");
                AssetDatabase.Refresh();
            }
            return assetRefCache;
        }

        public static void DeleteOriginSource()
        {
            try
            {
                //string path = "Assets/SYPackages/ArtToolBox/Editor";
                //if (Directory.Exists(path))
                //{
                //    string[] subFloders = Directory.GetDirectories(path);
                //    foreach (string dir in subFloders)
                //    {
                //        AssetDatabase.DeleteAsset(dir);
                //        //Directory.Delete(dir, true);
                //    }
                //}
            }
            catch
            {

            }
        }

        public static void CheckGlobal()
        {
            string filePath = "/SYPackages/ArtToolBox/ArtToolBoxGlobalData.asset";

            if (!File.Exists("Assets" + filePath))
            {
                return;
            }

            string fileContent = File.ReadAllText(Application.dataPath + filePath);

            // 根据换行符分割字符串为字符串数组
            string[] lines = fileContent.Split('\n');
            ArtToolBoxGlobalData artToolBoxGlobalData = ScriptableObject.CreateInstance<SYArtToolkit.ArtToolBoxGlobalData>();
            string matchingString = "standardPath:";
            int i = 0;
            while (i < lines.Length)
            {
                if (lines[i].IndexOf(matchingString) > -1)
                {
                    ++i;
                    matchingString = "materialPath: ";
                    int pathIndex = lines[i].IndexOf(matchingString);
                    if (pathIndex > -1)
                    {
                        artToolBoxGlobalData.standardPath.materialPath = lines[i].Substring(pathIndex + matchingString.Length);
                    }
                    else
                    {
                        Debug.Log(lines[i]);
                        break;
                    }

                    ++i;
                    matchingString = "texturePath: ";
                    pathIndex = lines[i].IndexOf(matchingString);
                    if (pathIndex > -1)
                    {
                        artToolBoxGlobalData.standardPath.texturePath = lines[i].Substring(pathIndex + matchingString.Length);
                    }
                    else
                    {
                        Debug.Log(lines[i]);
                        break;
                    }
                    ++i;
                    matchingString = "meshPath: ";
                    pathIndex = lines[i].IndexOf(matchingString);
                    if (pathIndex > -1)
                    {
                        artToolBoxGlobalData.standardPath.meshPath = lines[i].Substring(pathIndex + matchingString.Length);
                    }
                    else
                    {
                        Debug.Log(lines[i]);
                        break;
                    }

                    ++i;
                    matchingString = "animatorPath: ";
                    pathIndex = lines[i].IndexOf(matchingString);
                    if (pathIndex > -1)
                    {
                        artToolBoxGlobalData.standardPath.animatorPath = lines[i].Substring(pathIndex + matchingString.Length);
                    }
                    else
                    {
                        Debug.Log(lines[i]);
                        break;
                    }


                    ++i;
                    matchingString = "shaderPools:";
                    pathIndex = lines[i].IndexOf(matchingString);
                    if (pathIndex < 0)
                    {
                        Debug.Log(lines[i]);
                        break;
                    }

                    ++i;
                    matchingString = "guid: ";
                    pathIndex = lines[i].IndexOf(matchingString);
                    while (pathIndex > -1)
                    {
                        string guid = lines[i].Substring(pathIndex + matchingString.Length, 32);
                        artToolBoxGlobalData.shaderPools.Add(AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guid)));
                        ++i;
                        pathIndex = lines[i].IndexOf(matchingString);
                    }

                    if (lines[i].IndexOf("presupposeList:") > -1)
                    {
                        ++i;
                    }
                    else
                    {
                        Debug.Log(lines[i]);
                        break;
                    }



                    int indexOfList = 0;
                    while (lines[i].IndexOf("presupposeSelect:") < 0)
                    {
                        if (artToolBoxGlobalData.presupposeList.Count <= indexOfList)
                        {
                            artToolBoxGlobalData.presupposeList.Add(new Presuppose());
                        }
                        matchingString = "- name: ";
                        pathIndex = lines[i].IndexOf(matchingString);
                        if (pathIndex > -1)
                        {
                            string temp = lines[i].Substring(pathIndex + matchingString.Length + 1);
                            temp = temp.Substring(0, temp.Length - 1);
                            artToolBoxGlobalData.presupposeList[indexOfList].name = Regex.Unescape(temp);
                        }
                        else break;
                        ++i;
                        matchingString = "vertexNum: ";
                        pathIndex = lines[i].IndexOf(matchingString);
                        if (pathIndex > -1)
                        {
                            string temp = lines[i].Substring(pathIndex + matchingString.Length);
                            artToolBoxGlobalData.presupposeList[indexOfList].vertexNum = int.Parse(temp);
                        }
                        else break;
                        ++i;

                        matchingString = "trisNum: ";
                        pathIndex = lines[i].IndexOf(matchingString);
                        if (pathIndex > -1)
                        {
                            string temp = lines[i].Substring(pathIndex + matchingString.Length);
                            artToolBoxGlobalData.presupposeList[indexOfList].trisNum = int.Parse(temp);
                        }
                        else break;
                        ++i;

                        matchingString = "particleSystemNum: ";
                        pathIndex = lines[i].IndexOf(matchingString);
                        if (pathIndex > -1)
                        {
                            string temp = lines[i].Substring(pathIndex + matchingString.Length);
                            artToolBoxGlobalData.presupposeList[indexOfList].particleSystemNum = int.Parse(temp);
                        }
                        else break;
                        ++i; matchingString = "particleNum: ";
                        pathIndex = lines[i].IndexOf(matchingString);
                        if (pathIndex > -1)
                        {
                            string temp = lines[i].Substring(pathIndex + matchingString.Length);
                            artToolBoxGlobalData.presupposeList[indexOfList].particleNum = int.Parse(temp);
                        }
                        else break;
                        ++i; matchingString = "meshNum: ";
                        pathIndex = lines[i].IndexOf(matchingString);
                        if (pathIndex > -1)
                        {
                            string temp = lines[i].Substring(pathIndex + matchingString.Length);
                            artToolBoxGlobalData.presupposeList[indexOfList].meshNum = int.Parse(temp);
                        }
                        else break;
                        ++i; matchingString = "skinMesh: ";
                        pathIndex = lines[i].IndexOf(matchingString);
                        if (pathIndex > -1)
                        {
                            string temp = lines[i].Substring(pathIndex + matchingString.Length);
                            artToolBoxGlobalData.presupposeList[indexOfList].skinMesh = int.Parse(temp);
                        }
                        else break;
                        ++i; matchingString = "textureMemory: ";
                        pathIndex = lines[i].IndexOf(matchingString);
                        if (pathIndex > -1)
                        {
                            string temp = lines[i].Substring(pathIndex + matchingString.Length);
                            artToolBoxGlobalData.presupposeList[indexOfList].textureMemory = int.Parse(temp);
                        }
                        else break;
                        ++i; matchingString = "textureNum: ";
                        pathIndex = lines[i].IndexOf(matchingString);
                        if (pathIndex > -1)
                        {
                            string temp = lines[i].Substring(pathIndex + matchingString.Length);
                            artToolBoxGlobalData.presupposeList[indexOfList].textureNum = int.Parse(temp);
                        }
                        else break;
                        ++i;
                        ++indexOfList;
                    }

                    if (lines[i].IndexOf("presupposeSelect:") > -1)
                    {
                        ++i;

                        matchingString = "name: ";
                        pathIndex = lines[i].IndexOf(matchingString);
                        if (pathIndex > -1)
                        {
                            string temp = lines[i].Substring(pathIndex + matchingString.Length + 1);
                            temp = temp.Substring(0, temp.Length - 1);
                            artToolBoxGlobalData.presupposeSelect.name = Regex.Unescape(temp);
                        }
                        else break;
                        ++i;
                        matchingString = "vertexNum: ";
                        pathIndex = lines[i].IndexOf(matchingString);
                        if (pathIndex > -1)
                        {
                            string temp = lines[i].Substring(pathIndex + matchingString.Length);
                            artToolBoxGlobalData.presupposeSelect.vertexNum = int.Parse(temp);
                        }
                        else break;
                        ++i;

                        matchingString = "trisNum: ";
                        pathIndex = lines[i].IndexOf(matchingString);
                        if (pathIndex > -1)
                        {
                            string temp = lines[i].Substring(pathIndex + matchingString.Length);
                            artToolBoxGlobalData.presupposeSelect.trisNum = int.Parse(temp);
                        }
                        else break;
                        ++i;

                        matchingString = "particleSystemNum: ";
                        pathIndex = lines[i].IndexOf(matchingString);
                        if (pathIndex > -1)
                        {
                            string temp = lines[i].Substring(pathIndex + matchingString.Length);
                            artToolBoxGlobalData.presupposeSelect.particleSystemNum = int.Parse(temp);
                        }
                        else break;
                        ++i; matchingString = "particleNum: ";
                        pathIndex = lines[i].IndexOf(matchingString);
                        if (pathIndex > -1)
                        {
                            string temp = lines[i].Substring(pathIndex + matchingString.Length);
                            artToolBoxGlobalData.presupposeSelect.particleNum = int.Parse(temp);
                        }
                        else break;
                        ++i; matchingString = "meshNum: ";
                        pathIndex = lines[i].IndexOf(matchingString);
                        if (pathIndex > -1)
                        {
                            string temp = lines[i].Substring(pathIndex + matchingString.Length);
                            artToolBoxGlobalData.presupposeSelect.meshNum = int.Parse(temp);
                        }
                        else break;
                        ++i; matchingString = "skinMesh: ";
                        pathIndex = lines[i].IndexOf(matchingString);
                        if (pathIndex > -1)
                        {
                            string temp = lines[i].Substring(pathIndex + matchingString.Length);
                            artToolBoxGlobalData.presupposeSelect.skinMesh = int.Parse(temp);
                        }
                        else break;
                        ++i; matchingString = "textureMemory: ";
                        pathIndex = lines[i].IndexOf(matchingString);
                        if (pathIndex > -1)
                        {
                            string temp = lines[i].Substring(pathIndex + matchingString.Length);
                            artToolBoxGlobalData.presupposeSelect.textureMemory = int.Parse(temp);
                        }
                        else break;
                        ++i; matchingString = "textureNum: ";
                        pathIndex = lines[i].IndexOf(matchingString);
                        if (pathIndex > -1)
                        {
                            string temp = lines[i].Substring(pathIndex + matchingString.Length);
                            artToolBoxGlobalData.presupposeSelect.textureNum = int.Parse(temp);
                        }
                        else break;


                    }
                    else break;

                    GetScriptableObjectDataArtToolBox.SaveData(artToolBoxGlobalData);

                    File.Delete("Assets" + filePath);
                    break;
                }

                ++i;
            }
            return;
        }


        // public static IdEnum GetProjectId()
        // {
        //     IdEnum id = IdEnum.ys;
        //     var project = AssetDatabase.LoadAssetAtPath<Submit.ProjectSet>("Packages/com.sy.arttoolkit/Editor/ProjectSet.asset");
        //     if (project != null) { id = project.id; }
        //
        //
        //     return id;
        // }

        // public static string GetProjectName()
        // {
        //     return AboutStringAttribute.GetEnumString(GetProjectId());
        //
        // }


    }

}
