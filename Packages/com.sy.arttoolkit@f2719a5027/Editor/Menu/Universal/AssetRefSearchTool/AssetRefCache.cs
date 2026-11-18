using System;
using System.Collections.Generic;
using UnityEngine;

namespace SYArtToolkit
{
    //     [CreateAssetMenu(fileName = "BulletData", menuName = "ScriptableObject/子弹数据", order = 0)]
    [Serializable]
    public class AssetRefCache : ScriptableObject
    {
        public List<AssetRefData> assetRefInfos;
    }

}