using System;
using SYPackages.TimeOfDay;
using UnityEngine;

public abstract class TodComponent : MonoBehaviour
{
    public enum TodState
    {
        Play,
        Pause,
        Stop
    }
    
    public float Time
    {
        set => Time = value;
        get => Time;
    }

    private float _interp;

    protected TodState state { set;get; } = TodState.Stop;
    
    protected virtual string key { set; get;}

    public void Register(string key)
    {
        //TimeOfDayManager.Instance.Register(key,this);
    }
    
    public void UnRegister(string key)
    {
        //TimeOfDayManager.Instance.UnRegister(key);
    }

    public virtual void Play(){}
    
    public virtual void Pause(){}

    public virtual void Stop(){}
    
    void OnEnable()
    {
        Register(key);
    }

    private void OnDestroy()
    {
        UnRegister(key);
    }

    // Update is called once per frame
    void Update()
    {
        switch (state)
        {
            case TodState.Play:
                Play();
                break;
            case TodState.Pause:
                Pause();
                break;
            case TodState.Stop:
                Stop();
                break;
        }
    }
}
