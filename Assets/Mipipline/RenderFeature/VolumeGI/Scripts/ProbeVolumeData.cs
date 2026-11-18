
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;

[Serializable]
[PreferBinarySerialization]
[CreateAssetMenu(fileName = "ProbeVolumeData", menuName = "ProbeVolumeData")]
public class ProbeVolumeData: ScriptableObject
{
    [SerializeField]
    public Vector3 volumePosition;

    [SerializeField]
    public float[] surfelStorageBuffer;

    [SerializeField]
    public int[] coefficientSH9Buffer;

    [SerializeField]
    public bool bakedFlag;

    public void StorageSurfelData(VolumeGICell volume,int splitCount,int index)
    {
        int probeNum = volume.probeSizeX * volume.probeSizeY * volume.probeSizeZ;
        int surfelPerProbe = 64;
        int floatPerSurfel = 11;
        int coefficientsh9 = 13;

        var size = (probeNum * surfelPerProbe * floatPerSurfel) / splitCount;
        Array.Resize<float>(ref surfelStorageBuffer, size);
        Array.Resize<int>(ref coefficientSH9Buffer, (probeNum * coefficientsh9)/splitCount);

        int j = 0;
        int startindex = (probeNum/splitCount)*index;
        int nextindex = (probeNum/splitCount)*(index+1);
        if (nextindex > probeNum)
            nextindex = probeNum-1;

        for (; startindex < (nextindex); startindex++)
        {
            VolumeGIProbe probe = volume.probes[startindex].GetComponent<VolumeGIProbe>();
            foreach (var surfel in probe.surfelsBufferDebugData)
            {
                surfelStorageBuffer[j++] = float.Parse(surfel.position.x.ToString("f3"));
                surfelStorageBuffer[j++] = float.Parse(surfel.position.y.ToString("f3"));
                surfelStorageBuffer[j++] = float.Parse(surfel.position.z.ToString("f3"));
                surfelStorageBuffer[j++] = float.Parse(surfel.normal.x.ToString("f2"));
                surfelStorageBuffer[j++] = float.Parse(surfel.normal.y.ToString("f2"));
                surfelStorageBuffer[j++] = float.Parse(surfel.normal.z.ToString("f2"));
                surfelStorageBuffer[j++] = float.Parse(surfel.albedo.x.ToString("f2"));
                surfelStorageBuffer[j++] = float.Parse(surfel.albedo.y.ToString("f2"));
                surfelStorageBuffer[j++] = float.Parse(surfel.albedo.z.ToString("f2"));
                surfelStorageBuffer[j++] = surfel.skyMask;
                surfelStorageBuffer[j++] = surfel.skyAO;
            }

        }
        startindex = (probeNum/splitCount)*index;
        j = 0;
        for (; startindex < nextindex; startindex++)
        {
            VolumeGIProbe probe = volume.probes[startindex].GetComponent<VolumeGIProbe>();
            foreach (var sh in probe.coefficientSH9BufferDebugData)
            {
                coefficientSH9Buffer[j++] = sh;
            }
        }

        volumePosition = volume.gameObject.transform.position;
        bakedFlag = true;
        // 保存
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
#endif
    }



    // pack all probe's data to 1D array
    public void StorageSurfelData(VolumeGICell volume)
    {
        int probeNum = volume.probeSizeX * volume.probeSizeY * volume.probeSizeZ;
        int surfelPerProbe = 64;
        int floatPerSurfel = 11;
        int coefficientsh9 = 13;
        //2  256  10
        Array.Resize<float>(ref surfelStorageBuffer, probeNum * surfelPerProbe * floatPerSurfel);
        Array.Resize<int>(ref coefficientSH9Buffer, probeNum * coefficientsh9);
        int j = 0;
        for(int i=0; i<volume.probes.Length; i++)
        {
            VolumeGIProbe probe = volume.probes[i].GetComponent<VolumeGIProbe>();
            foreach (var surfel in probe.surfelsBufferDebugData)
            {
                // surfelStorageBuffer[j++] = surfel.position.x;
                // surfelStorageBuffer[j++] = surfel.position.y;
                // surfelStorageBuffer[j++] = surfel.position.z;
                // surfelStorageBuffer[j++] = surfel.normal.x;
                // surfelStorageBuffer[j++] = surfel.normal.y;
                // surfelStorageBuffer[j++] = surfel.normal.z;
                // surfelStorageBuffer[j++] = surfel.albedo.x;
                // surfelStorageBuffer[j++] = surfel.albedo.y;
                // surfelStorageBuffer[j++] = surfel.albedo.z;
                // surfelStorageBuffer[j++] = surfel.skyMask;

                surfelStorageBuffer[j++] = float.Parse(surfel.position.x.ToString("f3"));
                surfelStorageBuffer[j++] = float.Parse(surfel.position.y.ToString("f3"));
                surfelStorageBuffer[j++] = float.Parse(surfel.position.z.ToString("f3"));
                surfelStorageBuffer[j++] = float.Parse(surfel.normal.x.ToString("f2"));
                surfelStorageBuffer[j++] = float.Parse(surfel.normal.y.ToString("f2"));
                surfelStorageBuffer[j++] = float.Parse(surfel.normal.z.ToString("f2"));
                surfelStorageBuffer[j++] = surfel.albedo.x;
                surfelStorageBuffer[j++] = surfel.albedo.y;
                surfelStorageBuffer[j++] = surfel.albedo.z;
                surfelStorageBuffer[j++] = surfel.skyMask;
                surfelStorageBuffer[j++] = surfel.skyAO;
            }
        }

        j = 0;
        for (int i = 0; i < volume.probes.Length; i++)
        {
            VolumeGIProbe probe = volume.probes[i].GetComponent<VolumeGIProbe>();
            foreach (var sh in probe.coefficientSH9BufferDebugData)
            {
                coefficientSH9Buffer[j++] = sh;
            }
        }

        volumePosition = volume.gameObject.transform.position;
        bakedFlag = true;
        // 保存
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
#endif
    }


    public void TryLoadSplitSurfelData(VolumeGICell volume, int splitCount, int index)
    {
        int probeNum = volume.probeSizeX * volume.probeSizeY * volume.probeSizeZ;
        int surfelPerProbe = 64;
        int floatPerSurfel = 11;
        int size = (probeNum * surfelPerProbe * floatPerSurfel) / splitCount;
        bool dataDirty = surfelStorageBuffer.Length != size;

        //dataDirty = true;
        bool posDirty = volume.gameObject.transform.position != volumePosition;
        if(posDirty || dataDirty)
        {
            #if UNITY_EDITOR
            Debug.LogWarning("volume data is old! please re capture!");
            Debug.LogWarning("探针组数据需要重新捕获");
            #endif
            return;
        }

        int j = 0;

        int startindex = (probeNum/splitCount)*index;
        int nextindex = (probeNum/splitCount)*(index+1);
        if (nextindex > probeNum)
        {
            nextindex = probeNum-1;
        }


        j = 0;


        for(; startindex<nextindex; startindex++)
        {
            VolumeGIProbe probe = volume.probes[startindex].GetComponent<VolumeGIProbe>();
            probe.InitBuffer();

            for (int i = 0; i < surfelPerProbe; i++)
            {
                probe.surfelsBufferDebugData[i].position.x = surfelStorageBuffer[j++];
                probe.surfelsBufferDebugData[i].position.y = surfelStorageBuffer[j++];
                probe.surfelsBufferDebugData[i].position.z = surfelStorageBuffer[j++];
                probe.surfelsBufferDebugData[i].normal.x = surfelStorageBuffer[j++];
                probe.surfelsBufferDebugData[i].normal.y = surfelStorageBuffer[j++];
                probe.surfelsBufferDebugData[i].normal.z = surfelStorageBuffer[j++];
                probe.surfelsBufferDebugData[i].albedo.x = surfelStorageBuffer[j++];
                probe.surfelsBufferDebugData[i].albedo.y = surfelStorageBuffer[j++];
                probe.surfelsBufferDebugData[i].albedo.z = surfelStorageBuffer[j++];
                probe.surfelsBufferDebugData[i].skyMask = surfelStorageBuffer[j++];
                probe.surfelsBufferDebugData[i].skyAO = surfelStorageBuffer[j++];
            }
            probe.surfelsBuffer.SetData(probe.surfelsBufferDebugData);

        }
        #if UNITY_EDITOR
        //Debug.Log("finish InitBuffer");
        #endif
        return;
    }


    // load surfel data from storage
    public void TryLoadSurfelData(VolumeGICell volume)
    {
        int probeNum = volume.probeSizeX * volume.probeSizeY * volume.probeSizeZ;
        int surfelPerProbe = 64;
        int floatPerSurfel = 11;
        bool dataDirty = surfelStorageBuffer.Length != probeNum * surfelPerProbe * floatPerSurfel;

        bool posDirty = volume.gameObject.transform.position != volumePosition;
        if(posDirty || dataDirty)
        {
            #if UNITY_EDITOR
            Debug.LogWarning("volume data is old! please re capture!");
            Debug.LogWarning("探针组数据需要重新捕获");
            #endif
            return;
        }

        int j = 0;
        int z = 0;
        foreach (var go in volume.probes)
        {
            VolumeGIProbe probe = go.GetComponent<VolumeGIProbe>();
            probe.InitBuffer();
            for(int i=0; i<probe.surfelsBufferDebugData.Length; i++)
            {
                probe.surfelsBufferDebugData[i].position.x = surfelStorageBuffer[j++];
                probe.surfelsBufferDebugData[i].position.y = surfelStorageBuffer[j++];
                probe.surfelsBufferDebugData[i].position.z = surfelStorageBuffer[j++];
                probe.surfelsBufferDebugData[i].normal.x = surfelStorageBuffer[j++];
                probe.surfelsBufferDebugData[i].normal.y = surfelStorageBuffer[j++];
                probe.surfelsBufferDebugData[i].normal.z = surfelStorageBuffer[j++];
                probe.surfelsBufferDebugData[i].albedo.x = surfelStorageBuffer[j++];
                probe.surfelsBufferDebugData[i].albedo.y = surfelStorageBuffer[j++];
                probe.surfelsBufferDebugData[i].albedo.z = surfelStorageBuffer[j++];
                probe.surfelsBufferDebugData[i].skyMask = surfelStorageBuffer[j++];
                probe.surfelsBufferDebugData[i].skyAO = surfelStorageBuffer[j++];
            }
            probe.surfelsBuffer.SetData(probe.surfelsBufferDebugData);


#if UNITY_EDITOR
            Debug.Log("finish Rebuild");
#endif

        }
return;

    }
}
