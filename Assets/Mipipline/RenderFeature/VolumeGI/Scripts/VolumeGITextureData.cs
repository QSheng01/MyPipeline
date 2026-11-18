using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;

[Serializable]
[CreateAssetMenu(fileName = "GITextureData", menuName = "GITextureData")]
public class VolumeGITextureData : ScriptableObject
{
   
   [SerializeField] public Vector3 posMin;
   [SerializeField] public Vector3 posMax;
   
   [SerializeField] public List<Texture3D> GITexture_TOD;

   [SerializeField] public List<float> clampmax;
   [SerializeField] public List<float> clampmin;

}
 