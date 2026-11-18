// using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;


namespace SYPackages.TimeOfDay
{
    [ExecuteInEditMode]
    public abstract class TodCompContrl : MonoBehaviour
    {
        // [ShowInInspector]
        public TodState State { set; get; } = TodState.Play;
        
        [HideInInspector]public int2 time = new int2(0,0);

        [HideInInspector]public float TimeOfDay;
        
        [HideInInspector]public float contrlInterp;

        protected bool isRegister = false;
        
        public void SetState(TodState state)
        {
            State = state;
        }

        public virtual void Register()
        {
        }

        public virtual void UnRegister()
        {
        }

        public virtual void Play()
        {
            if (!isRegister) return;
        }

        public virtual void Pause()
        {
        }

        public virtual void Stop()
        {
        }

        // Update is called once per frame
        void Update()
        {
            switch (State)
            {
                case TodState.Play:
                    Play();
                    break;
                case TodState.Stop:
                    Stop();
                    break;
            }
        }
#if UNITY_EDITOR
        [ContextMenu("绑定数据给TOD系统")]
        void SetData()
        {
            Register();
        }
        
        [ContextMenu("接触数据给TOD系统")]
        void ResetData()
        {
            UnRegister();
        }
#endif
    }
}