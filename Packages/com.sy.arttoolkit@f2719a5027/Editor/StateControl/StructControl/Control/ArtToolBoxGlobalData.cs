using System.Collections.Generic;
using UnityEngine;

namespace SYArtToolkit
{

    public class ArtToolBoxGlobalData : ScriptableObject
    {
        public SelectPath standardPath;
        public List<Shader> shaderPools;
        public List<Presuppose> presupposeList;
        public Presuppose presupposeSelect;
        public ArtToolBoxGlobalData()
        {
            standardPath = new SelectPath();
            shaderPools = new List<Shader>();
            presupposeList = new List<Presuppose>();
            presupposeList.Add(new Presuppose());
            presupposeSelect = presupposeList[0];
        }
    }

}
