using System;
using System.Collections;
using System.Collections.Generic;
using SYPackages.AtmosphericScattering;
using UnityEngine;
using UnityEngine.Rendering;
[ExecuteInEditMode]
public class AtmosphereFogController : MonoBehaviour
{
    private AtmosphericScatteringFog _atmospherFog;
    [InspectorName("开启大气雾效")]
    public bool openfog = true;
    
    private void OnEnable()
    {
        _atmospherFog =  VolumeManager.instance.stack.GetComponent<AtmosphericScatteringFog>();
    }
    
    void Update()
    {
        if (_atmospherFog)
        {
            _atmospherFog.enabled = new BoolParameter(openfog);
        }
    }
}
