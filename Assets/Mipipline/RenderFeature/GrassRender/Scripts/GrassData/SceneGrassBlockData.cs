using Unity.Collections;
using UnityEngine;

namespace Grass
{
    public struct SceneGrassBlockData
    {
        public Matrix4x4[] Datas;
        //SphericalHarmonicsL2[] Lightprobes;
        //SHL2Data[] SHL2Datas;
        //Vector4[] Occlusionprobes;

        public void InitData(Matrix4x4[] datas)
        {
            Datas = datas;
            //Lightprobes = new SphericalHarmonicsL2[datas.Length];
            //Occlusionprobes = new Vector4[datas.Length];
        }

        public void GetGrassBlockData(
            ref NativeArray<Matrix4x4> matrixs,
            //ref NativeArray<Vector4> occlusionprobes,
            //ref NativeArray<SphericalHarmonicsL2> lightprobes,
            //ref NativeArray<SHL2Data> shl2Datas,
            ref int offset)
        {
            NativeArray<Matrix4x4>.Copy(Datas, 0, matrixs, offset, Datas.Length);
            //NativeArray<Vector4>.Copy(Occlusionprobes, 0, occlusionprobes, offset, Datas.Length);
            
            //if (HWorldController.Instance.EnableIndirect)
            //{
            //    NativeArray<SHL2Data>.Copy(SHL2Datas, 0, shl2Datas, offset, Datas.Length);
            //}
            //else
            //{
            //    NativeArray<SphericalHarmonicsL2>.Copy(Lightprobes, 0, lightprobes, offset, Datas.Length);
            //}
            
            offset += Datas.Length;
        }

        //public void UpdateLightProbes()
        //{
        //    if (Datas == null || Datas.Length == 0)
        //        return;

        //    var pos = new Vector3[Datas.Length];
        //    for (int i = 0; i < Datas.Length; i++)
        //    {
        //        pos[i] = Datas[i].GetColumn(3);
        //    }
        //    //LightProbes.CalculateInterpolatedLightAndOcclusionProbes(pos, Lightprobes, Occlusionprobes);

        //    if (HWorldController.Instance.EnableIndirect)
        //    {
        //        if (SHL2Datas == null)
        //        {
        //            SHL2Datas = new SHL2Data[Lightprobes.Length];
        //        }
        //        int index = 0;
        //        foreach (var lightProbe in Lightprobes)
        //        {
        //            SHL2Data data = new SHL2Data();
        //            data.Init(lightProbe);
        //            SHL2Datas[index] = data;
        //            index++;
        //        }
        //    }
        //}

        public HWorldBounds GetCullingBounds(Bounds bounds,float height)
        {
            if (Datas == null || Datas.Length == 0)
                return new HWorldBounds();

            float minY = float.MaxValue;
            float maxY = float.MinValue;
            foreach (var data in Datas)
            {
                var pos = data.GetColumn(3);
                if (pos.y < minY)
                    minY = pos.y;
                var h = pos.y + height * data.m11;
                if (h > maxY)
                {
                    maxY = h;
                }
            }
            bounds.SetMinMax(new Vector3(bounds.min.x, minY, bounds.min.z), new Vector3(bounds.max.x, maxY, bounds.max.z));
            HWorldBounds hwBounds = new HWorldBounds(bounds.center, bounds.extents);
            return hwBounds;
        }
    }
}
