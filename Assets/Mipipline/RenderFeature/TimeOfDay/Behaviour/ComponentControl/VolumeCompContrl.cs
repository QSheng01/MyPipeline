using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;
#if MAP_EDITOR
#if UNITY_EDITOR
using UnityEditor.Timeline;
#endif
#endif
 
namespace SYPackages.TimeOfDay
{
    [RequireComponent(typeof(PlayableDirector))]
    public class VolumeCompContrl : TodCompContrl
    {
        public PlayableAsset todTimeLine;
        public PlayableDirector _todDirector;

        public PlayableDirector GetDirector() { return _todDirector;}

        public void SetPlaySpeed(float speed)
        {
            if (!todTimeLine)
            {
                Debug.Log("set play speed failed, cause no todTimeLine.");
            }
            else
            {
                _todDirector.playableGraph.GetRootPlayable(0).SetSpeed(speed);
            }
        }

        public override void Register()
        {
            _todDirector = this.gameObject.GetComponent<PlayableDirector>();
            isRegister = true;
        }

        public override void UnRegister()
        {
            isRegister = false;
        }

        public override void Play()
        {
            base.Play();
            
            if (!todTimeLine || !_todDirector) return;
            _todDirector.time = _todDirector.duration * contrlInterp;
            _todDirector.Evaluate();
#if MAP_EDITOR
#if UNITY_EDITOR
            TimelineEditor.Refresh(RefreshReason.WindowNeedsRedraw);
#endif
#endif 
        }

        public override void Pause()
        {
        }

        public override void Stop()
        {
        }
    }
}
