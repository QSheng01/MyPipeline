using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
// using UnityEngine.Timeline;

[SerializeField]
public class TodClip : PlayableAsset
{
    //public ExposedReference<VolumeProfile> volumeProfile;
    public VolumeProfile volumeProfile;
    
    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        ScriptPlayable<TodBehaviour> playable = ScriptPlayable<TodBehaviour>.Create(graph);
        TodBehaviour tod = playable.GetBehaviour();
        
        //tod.VolumeProfile = volumeProfile.Resolve(graph.GetResolver());
        tod.VolumeProfile = volumeProfile;
        
        return playable;
        //return Playable.Create(graph);
    }
    
    
}
