using System.Collections.Generic;
// using SYPackages.GraphicsLoad;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace SYPackages.TimeOfDay
{
    public enum TodState
    {
        Play,Stop
    }

    [ExecuteInEditMode]
    public class TimeOfDayManager : MonoBehaviour
    {
        //[ShowInInspector]
        private TodState _todState = TodState.Play;

        //[ShowInInspector]
        public bool artMode = true;

        //[ShowInInspector]
        public Vector2 LoopTime = new Vector3(0,1);
        //[ShowInInspector]
        [Range(0f, 24f)]
        public float timeOfDay = 12f;
        //[ShowInInspector]
        [Range(0f, 23f)]
        public int hour = 12;
        //[ShowInInspector]
        [Range(0f, 59f)]
        public int minute = 0;
        private float TimeOfDay = 0.5f;

        //[ShowInInspector]
        public List<TodCompContrl> TodComponents = new List<TodCompContrl>();

        public void Play() { _todState = TodState.Play;}
        public void Stop() { _todState = TodState.Stop;}

        [HideInInspector]
        public Volume GlobalVolume;

#if MAP_EDITOR
#if UNITY_EDITOR
        [ContextMenu("输出compent")]
        void LogComponent()
        {
            foreach (var component in TimeOfDayManagerSingleton.Instance.TimeOfDayManager.TodComponents)
            {
                Debug.unityLogger.Log(component.name);
            }
        }
#endif
#endif
        /// <summary>
        ///  设置时间
        /// </summary>
        public void SetTime(int2 time)
        {
            hour = time.x;
            minute = time.y;

            TimeOfDay = hour * 3600f + minute * 60f;

            if (TimeOfDay >= 86400f)
            {
                TimeOfDay = 0f;
            }

            timeOfDay = TimeOfDay / 86400f * 24f;
        }

        public void SetLoopTime(Vector2 time)
        {
            LoopTime = time;
        }

        // public bool Register(TodCompContrl compContrl)
        // {
        //     if (TodComponents.Contains(compContrl))
        //     {
        //         Debug.unityLogger.Log("register " + compContrl.name + " failed. because it already contains.");
        //         return false;
        //     }
        //     TodComponents.Add(compContrl);
        //     Debug.unityLogger.Log("register " + compContrl.name + " successed");
        //     return true;
        // }
        //
        // public bool UnRegister(TodCompContrl compContrl)
        // {
        //     if (TodComponents.Remove(compContrl))
        //     {
        //         Debug.unityLogger.Log("unregister " + compContrl.name + " successed");
        //         return true;
        //     }
        //     else
        //     {
        //         Debug.unityLogger.Log("unregister " + compContrl.name + " failed, doesn't find the key");
        //         return false;
        //     }
        //
        // }

        public void SetState(TodState mode)
        {
            this._todState = mode;

            foreach (var component in TimeOfDayManagerSingleton.Instance.TimeOfDayManager.TodComponents)
            {
                component.SetState(mode);
            }
        }

        private void UpdateTodComponents()
        {
            var time = new int2(hour,minute);

            foreach (var component in TimeOfDayManagerSingleton.Instance.TimeOfDayManager.TodComponents)
            {
                component.TimeOfDay = TimeOfDay;
                component.time = time;
                component.contrlInterp = timeOfDay / 24f;
            }
        }

        private void OnEnable()
        {
            if (Application.isPlaying) DontDestroyOnLoad(this);

            Init();
        }

        private void Init()
        {
            TimeOfDayManagerSingleton.Instance.Register(this);

            if (this.transform.parent != null)
            {
                Volume volume = this.transform.parent.GetComponentInChildren<Volume>();
                if (volume != null  && volume.isGlobal)
                {
                    GlobalVolume = volume;
                }
            }

            if (GlobalVolume == null)
            {
                Volume[] volumes = VolumeManager.instance.GetVolumes(LayerMask.GetMask("PostProcessing"));
                foreach (var volume2 in volumes)
                {
                    if (volume2.isGlobal)
                    {
                        GlobalVolume = volume2;
                    }
                }
            }

            InitTodCompContrl();
        }

        private void InitTodCompContrl()
        {
            var todCompContrls = gameObject.GetComponentsInChildren<TodCompContrl>(true);

            if (todCompContrls != null && todCompContrls.Length > 0)
            {
                TodComponents.Clear();
                TodComponents.AddRange(todCompContrls);
            }

            foreach (var compponent in TodComponents)
            {
                compponent.Register();
            }
        }

        private void OnDisable()
        {
            TimeOfDayManagerSingleton.Instance.UnRegister(this);

            hour = 12;
            minute = 0;
            TimeOfDay = 12f * 3600f;
            timeOfDay = 0.5f * 24f;
        }

        private void Update()
        {
#if MAP_EDITOR
#if UNITY_EDITOR
            if (!TimeOfDayManagerSingleton.Instance.TimeOfDayManager)
            {
                TimeOfDayManagerSingleton.Instance.Register(this);
            }
#endif
#endif
            if (_todState == TodState.Stop) return;
            UpdateTime();
            UpdateTodComponents();
        }

        private void UpdateTime()
        {
            float loopTime = (LoopTime.x + LoopTime.y / 60f);

            if (loopTime == 0f)
            {
                loopTime = 24f;
            }

            float timeSpeed = (24 / loopTime);

// #if UNITY_EDITOR
            if (artMode)
            {
                //TimeOfDay = hour * 3600f + minute * 60f;
                TimeOfDay = timeOfDay * 86400f / 24f;

                if (TimeOfDay >= 86400f)
                {
                    TimeOfDay = 0f;
                }

                float hPointM = TimeOfDay / 3600f;

                hour = Mathf.FloorToInt(hPointM);

                minute = Mathf.FloorToInt((hPointM - hour) * 59f);

                //timeOfday = TimeOfDay / 86400f;
                // if (Math.Abs(TimelineEditor.masterDirector.time - _interp * todTimeLineDirector.duration) > 0.001f)
                // {
                //     TimeOfDay = (float)(todTimeLineDirector.time / todTimeLineDirector.duration) * 86400f;
                // }
            }
            else
            {
                TimeOfDay += Time.deltaTime * timeSpeed;

                if (TimeOfDay >= 86400f)
                {
                    TimeOfDay = 0f;
                }

                float hPointM = TimeOfDay / 3600f;

                hour = Mathf.FloorToInt(hPointM);

                minute = Mathf.FloorToInt((hPointM - hour) * 59f);

                timeOfDay = TimeOfDay / 86400f * 24f;
            }
// #else
//             TimeOfDay += Time.deltaTime * timeSpeed;
//
//             if (TimeOfDay >= 86400f)
//             {
//                 TimeOfDay = 0f;
//             }
//
//             float HPointM = TimeOfDay / 3600f;
//
//             hour = Mathf.FloorToInt(HPointM);
//
//             minute = Mathf.FloorToInt((HPointM - hour) * 59f);
//
//             timeOfDay = TimeOfDay / 86400f * 24f;
// #endif
        }
    }
}