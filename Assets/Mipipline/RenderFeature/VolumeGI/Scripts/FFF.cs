using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

[ExecuteAlways]
public class FFF: MonoBehaviour
{

    #region ShaderValume

    [Range(-2f, 2)] 
    public float _aoadd = 0;
    
    public bool _mixao;
    public bool _view;

    public Texture3D _Tex3DGIL0;
    public Texture3D _Tex3DGIL10;
    public Texture3D _Tex3DGIL11;
    public Texture3D _Tex3DGIL12;
    
    public enum MyEnum
    {
        bakedGI,
        gitex,
        gicolor,
        finalColorWithGitex,
        finalColorWithoutGitex,
        buffergiColor
    }

    public MyEnum colormode;

    public bool localset;
    
    #endregion
    private void Update()
    {
        Shader.SetGlobalInt("colormode",(int)colormode);
        Shader.SetGlobalFloat("_mixao",_mixao?1:0);
        Shader.SetGlobalFloat("_view",_view?1:0);
        Shader.SetGlobalFloat("_aoadd",_aoadd);
        if (localset)
        {
            Shader.SetGlobalTexture("_Tex3DGIL0",_Tex3DGIL0);
            Shader.SetGlobalTexture("_Tex3DGIL10",_Tex3DGIL10);
            Shader.SetGlobalTexture("_Tex3DGIL11",_Tex3DGIL11);
            Shader.SetGlobalTexture("_Tex3DGIL12",_Tex3DGIL12);
            
        }
    }

    // [Button("get")]
    // void GetSphere()
    // {
    //     int count = 25;
    //     for (int i = 0; i < 25; i++)
    //     {
    //         float z = (2.0f * i - 1) / 25.0f - 1;
    //         float x = Mathf.Sqrt(1 - z * z) * Mathf.Cos(2 * 3.14f * i * 0.618f);
    //         float y = Mathf.Sqrt(1 - z * z) * Mathf.Sin(2 * 3.14f * i * 0.618f);
    //         Instantiate(obj, new Vector3(x,y,z),Quaternion.identity);
    //     }
    //     
    // }
}
