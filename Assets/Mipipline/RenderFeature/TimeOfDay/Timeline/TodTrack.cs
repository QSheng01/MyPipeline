using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;


// [TrackBindingType(typeof(VolumeProfile))]
[TrackClipType(typeof(TodClip))]
public class TodTrack : TrackAsset
{
    public ScriptPlayable<TodClipMixer> todClipMixerPlayable;/*Playable*/
    private TodClipMixer _todClipMixer;
    
#if UNITY_EDITOR
    [HideInInspector]
    public TodTrackEditorContext editorContext;
#endif
    
    private void OnEnable()
    {
#if UNITY_EDITOR
        if (editorContext) return;
            editorContext = ScriptableObject.CreateInstance<TodTrackEditorContext>();
#endif
    }

    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        /*Mixer's Playable*/
        todClipMixerPlayable = ScriptPlayable<TodClipMixer>.Create(graph);
        todClipMixerPlayable.SetInputCount(inputCount);
        
        /*Mixer's Behaviour*/
        _todClipMixer = todClipMixerPlayable.GetBehaviour();
        
        /*InitAttribute*/
        
        return todClipMixerPlayable;
    }
}
