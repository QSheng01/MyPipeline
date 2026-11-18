using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class QSShaderKeyword
{
    //CharacterLightForUI
    public static readonly string SOFTSHADOW_ON = "_SOFTSHADOW_ON";
    public static readonly string PEROBJECTSHADOW = "_PEROBJECTSHADOW";
    public static readonly string DEPTHONLYPASS = "DepthOnly";
    public static readonly string PERSPECTIVE = "_PERSPECTIVE";
    
    //Light
    public static readonly string SIMPLE_MAIN_LIGHT = "_SIMPLE_MAIN_LIGHT";
    public static readonly string SIMPLE_ADDITIONAL_LIGHT = "_SIMPLE_ADDITIONAL_LIGHT";
    
    //DitherObject
    public static readonly string DITHER = "_DITHER";
    
    //Grass
    public static readonly string DEBUGKEYWORD = "_DEBUG";
    public static readonly string GRASSDRAWKEYWORD = "_GRASSDRAW";
    public static readonly string INTERACTIVEBLENDGRASS = "ENABLE_INTERACTIVEBLENDGRASS";
    
    //HeightFog
    public static readonly string HEIGHT_FOG_LINEAR = "HEIGHT_FOG_LINEAR";
    public static readonly string HeightFog = "_HEIGHTFOG";
    
    //OtherShadow
    public static readonly string WORLDFOG = "_WORLDFOG";
    public static readonly string CLOUDSHADOWS = "_CLOUDSHADOWS";
    public static readonly string SHADESHADOWS = "_SHADESHADOWS";
    public static readonly string WIND = "_WIND";
    
    //Terrain
    public static readonly string HEIGHTBLEND = "_HEIGHTBLEND";
    
    //Distortion
    public static readonly string SCREEN_SPACE_DISTORTION = "_SCREEN_SPACE_DISTORTION";
}