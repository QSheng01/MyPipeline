using System.Collections.Generic;
using UnityEngine;

namespace Grass
{
    [System.Serializable]
    public class GrassPainterData : ScriptableObject
    {
        public Bounds RootBound;
        public GameObject[] DrawObjArray;
        public List<GrassElementData> Datas;
        public Bounds GetGrassBounds(int type)
        {
            var bounds = new Bounds();
            if (DrawObjArray == null || DrawObjArray.Length <= type || DrawObjArray[type] == null)
            {
                Debug.Log(string.Format("GrassPainterData::GetGrassBounds::找不到 type = {0}的植被！！！", type));
                return bounds;
            }

            var lodData = DrawObjArray[type].GetComponent<GrassInstanceData>();
            if (lodData == null)
            {
                lodData = DrawObjArray[type].AddComponent<GrassInstanceData>();
                lodData.Init();
            }

            // 返回一个草 prefab 的 bounds 数据
            return lodData.GetMeshRender().bounds;
        }
    }
}