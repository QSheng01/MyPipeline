using System.Collections.Generic;
using UnityEngine;

namespace SYArtToolkit
{

    public class ScriptableObjectDataOfAll : ScriptableObject
    {
        public List<string> ScenePath = new List<string>();

        public List<string> FolderPath = new List<string>();

        public SelectPath SelectPath = new SelectPath();

        public List<Shader> shaderPools = new List<Shader>();

        public List<Presuppose> PresupposesList;
        public Presuppose PresupposeIndex;

        public ScriptableObjectDataOfAll()
        {
            PresupposesList = new List<Presuppose>();
            PresupposesList.Add(new Presuppose());
            PresupposeIndex = PresupposesList[0];
        }
    }

    [System.Serializable]
    public class SelectPath
    {
        public string materialPath = "";
        public string texturePath = "";
        public string meshPath = "";
        public string animatorPath = "";
    }

    [System.Serializable]
    public class Presuppose
    {
        public string name = "默认";
        public int vertexNum = 0;
        public int trisNum = 0;
        public int particleSystemNum = 0;
        public int particleNum = 0;
        public int meshNum = 0;
        public int skinMesh = 0;
        public float textureMemory = 0;
        public int textureNum = 0;
        public float overdraw = 0;
        public float drawcal = 1;
        public string glob = "";
    }
}