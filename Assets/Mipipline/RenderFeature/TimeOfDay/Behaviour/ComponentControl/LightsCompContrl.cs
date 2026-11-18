using System.Collections;
using System.Collections.Generic;
using SYPackages.TimeOfDay;
using UnityEngine;

public class LightsCompContrl : TodCompContrl
{
    
    public GameObject DayLightsParent;
    public GameObject NightLightsParent;
    
    private new const string ContrlKey = "LIGHTS";

    public override void Register()
    {
    }

    public override void UnRegister()
    {
        DayLightsParent = null;
        NightLightsParent = null;
    }

    public override void Play()
    {
        if (!DayLightsParent || !NightLightsParent) return;

        if (TimeOfDay is >= 21600f and <= 64800f) // if <= 18 ,because of the minute, ex: 18:59 will still be the day.
        {
            DayLightsParent.SetActive(true);
            NightLightsParent.SetActive(false);
        }
        else
        {
            DayLightsParent.SetActive(false);
            NightLightsParent.SetActive(true);
        }
    }

    public override void Pause()
    {
        base.Pause();
    }

    public override void Stop()
    {
        base.Stop();
    }
}
