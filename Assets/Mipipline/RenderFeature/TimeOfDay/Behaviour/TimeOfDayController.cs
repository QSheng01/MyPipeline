// using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;


namespace SYPackages.TimeOfDay
{
    public class TimeOfDayController : MonoBehaviour
    {
        public Vector3 sunRotVec = Vector3.right;
        public Vector3 moonRotVec = Vector3.right;

        // [HideLabel] public AnimationCurve sunIntensityCurve =
        //     new AnimationCurve(new Keyframe[2] { new Keyframe(0, 1), new Keyframe(24, 1) });
        //
        // [HideLabel] public AnimationCurve moonIntensityCurve =
        //     new AnimationCurve(new Keyframe[2] { new Keyframe(0, 1), new Keyframe(24, 1) });
        [Range(0f,24f)]
        public float StartTime = 0f;
        public PlayableAsset todTimeLine;

        private TimeOfDayManager _timeOfDayManager;

        void Awake()
        {
            _timeOfDayManager = GameObject.Find("TimeOfDayRoot").GetComponent<TimeOfDayManager>();
            if (_timeOfDayManager)
            {
                // _timeOfDayManager.sunRotVec = sunRotVec;
                // _timeOfDayManager.moonRotVec = moonRotVec;
                // _timeOfDayManager.todTimeLineDirector.playableAsset = todTimeLine;
                // _timeOfDayManager.SetTime(StartTime);
                //_timeOfDayManager.Play();
            }
        }
    }
}