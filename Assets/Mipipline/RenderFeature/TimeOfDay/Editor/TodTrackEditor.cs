using System;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Rendering;

namespace SYPackages.TimeOfDay.Editor
{
    [CustomEditor(typeof(TodTrack))]
    public class TodTrackEditor : UnityEditor.Editor
    {
        
        private TodTrack _mTarget;

        private VolumeProfile _morningProfile;
        private VolumeProfile _shineProfile;
        private VolumeProfile _afternoomProfile;
        private VolumeProfile _nightProfile;

        private void OnEnable()
        {
            _mTarget = target as TodTrack;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            GUILayout.Space(10);
            
                GUILayout.Label("基础参数：");
            
            GUILayout.Space(5);
            
            GUILayout.BeginHorizontal();
            
                GUILayout.Label("清晨时段VolumeProfile：");
            
        EditorGUI.BeginChangeCheck();
            
                _morningProfile = EditorGUILayout.ObjectField(_mTarget.editorContext._morningProfile, typeof(VolumeProfile), false) as VolumeProfile;
            
            GUILayout.EndHorizontal();
            
            GUILayout.Space(2);
            
            GUILayout.BeginHorizontal();
            
                GUILayout.Label("早上时段VolumeProfile：");
            
                _shineProfile = EditorGUILayout.ObjectField(_mTarget.editorContext._shineProfile, typeof(VolumeProfile), false) as VolumeProfile;
            
            GUILayout.EndHorizontal();
            
            GUILayout.Space(2);
            
            GUILayout.BeginHorizontal();
            
                GUILayout.Label("黄昏时段VolumeProfile：");
            
                _afternoomProfile = EditorGUILayout.ObjectField(_mTarget.editorContext._afternoomProfile, typeof(VolumeProfile), false) as VolumeProfile;
            
            GUILayout.EndHorizontal();
            
            GUILayout.Space(2);
            
            GUILayout.BeginHorizontal();
            
                GUILayout.Label("夜晚时段VolumeProfile：");
            
                _nightProfile = EditorGUILayout.ObjectField(_mTarget.editorContext._nightProfile, typeof(VolumeProfile), false) as VolumeProfile;
            
            GUILayout.EndHorizontal();
            
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_mTarget.editorContext, "Changed Context");
            _mTarget.editorContext._morningProfile = _morningProfile;
            _mTarget.editorContext._shineProfile = _shineProfile;
            _mTarget.editorContext._afternoomProfile = _afternoomProfile;
            _mTarget.editorContext._nightProfile = _nightProfile;
            EditorUtility.SetDirty(_mTarget.editorContext); 
            AssetDatabase.SaveAssets();
        }
            
            GUILayout.Space(10);
            
                GUILayout.Label("预设：");
            
            GUILayout.Space(5);
            
                if (GUILayout.Button("6 * 4 时间段",GUILayout.Width(80),GUILayout.Height(30)))
                {
                    CreateAssets();
                    TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
                }
        }
    
        public void CreateAssets()
        {
            
            var clips = _mTarget.GetClips();
            
            if (_mTarget.hasClips)
            {
                foreach (var clip in clips)
                {
                    _mTarget.DeleteClip(clip);
                }
            }
            
            var nightClip = _mTarget.CreateClip<TodClip>();
            nightClip.displayName = "Night";
            nightClip.start = 0;
            nightClip.duration = 6.5;
            ((TodClip)nightClip.asset).volumeProfile = _mTarget.editorContext._nightProfile;
            
            
            var morningClip = _mTarget.CreateClip<TodClip>();
            morningClip.displayName = "Morning";
            morningClip.start = 5.5;
            morningClip.duration = 2;
            ((TodClip)morningClip.asset).volumeProfile = _mTarget.editorContext._morningProfile;
            
            var shineClip = _mTarget.CreateClip<TodClip>();
            shineClip.displayName = "Shine";
            shineClip.start = 6.5;
            shineClip.duration = 11;
            ((TodClip)shineClip.asset).volumeProfile = _mTarget.editorContext._shineProfile;

            var afterClip = _mTarget.CreateClip<TodClip>();
            afterClip.displayName = "Afternoon";
            afterClip.start = 16.5;
            afterClip.duration = 2;
            ((TodClip)afterClip.asset).volumeProfile = _mTarget.editorContext._afternoomProfile;
            
            nightClip = _mTarget.CreateClip<TodClip>();
            nightClip.displayName = "Night";
            nightClip.start = 17.5;
            nightClip.duration = 6.5;
            ((TodClip)nightClip.asset).volumeProfile = _mTarget.editorContext._nightProfile;
            
        }
    }
}
