using System;
using System.Collections.Generic;

namespace SYArtToolkit
{


    [Serializable]
    public class AssetRefData
    {
        public string path;
        public string guid;
        public List<string> refAssetPath;

        public bool hasRef
        {
            get { return refAssetPath != null && refAssetPath.Count != 0; }
        }

        public bool foldout;
    }
}