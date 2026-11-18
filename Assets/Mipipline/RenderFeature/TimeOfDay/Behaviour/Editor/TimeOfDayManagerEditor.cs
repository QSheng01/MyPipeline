using System.Collections;
using System.Collections.Generic;
using SYPackages.TimeOfDay;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Timeline;

[CustomEditor(typeof(TimeOfDayManager))]
public class TimeOfDayManagerEditor : UnityEditor.Editor
{
    
    
    public void OnEnable()
    {
        PropertyFetcher<TimeOfDayManager> _timeOfDayManager = new PropertyFetcher<TimeOfDayManager>(serializedObject);
    }
    
    public override void OnInspectorGUI()
    {
        
        
        base.OnInspectorGUI();
        if (GUILayout.Button("跳转到当前VolumeProfile"))
        {
            OpenCurrentVolumeProfile();
        }
    }

    private void OpenCurrentVolumeProfile()
    {
        
        TimeOfDayManager _timeOfDayManager = (TimeOfDayManager)target;
        
        var volumeCompContrl = _timeOfDayManager.gameObject.GetComponentInChildren(typeof(VolumeCompContrl)) as VolumeCompContrl;
        var director = volumeCompContrl._todDirector;
        if (director != null && director.playableAsset != null)
        {
            double currentTime = director.time;
            var timelineAsset = director.playableAsset as TimelineAsset;
            var tracks = timelineAsset.GetOutputTracks();
            foreach (var track in tracks)
            {
                foreach (var clip in track.GetClips())
                {
                    if (currentTime >= clip.start && currentTime < clip.end)
                    {
                        if (clip.asset.GetType() == typeof(TodClip))
                        {
                            var curTodClip = clip.asset as TodClip;
                            if (curTodClip != null && curTodClip.volumeProfile != null)
                            {
                                Selection.activeObject = curTodClip.volumeProfile;
                                EditorGUIUtility.PingObject(curTodClip.volumeProfile);
                            }
                                
                            
                        }
                        
                        
                    }
                }
            }

        }
    }
}
