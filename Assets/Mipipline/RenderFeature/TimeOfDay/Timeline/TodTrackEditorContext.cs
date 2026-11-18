using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public class TodTrackEditorContext : ScriptableObject
{
    public VolumeProfile _morningProfile;
    public VolumeProfile _shineProfile;
    public VolumeProfile _afternoomProfile;
    public VolumeProfile _nightProfile;
}
