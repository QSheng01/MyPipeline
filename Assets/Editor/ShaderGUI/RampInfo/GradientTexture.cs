using System;
using System.IO;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace CustomShaderEditor
{
    public class GradientTexture : ScriptableObject
    {
        [Serializable]
        public class GradientInfo
        {
            [HorizontalGroup("",Width = 60)][HideLabel]
            public string nickName;
            [HorizontalGroup("")][HideLabel]
            public Gradient gradient;
        }

        [ReadOnly]public int texWidth = 256;
        [ReadOnly]public int perheight = 2;

        [ListDrawerSettings(
            //IsReadOnly = true,
            Expanded = true,
            DraggableItems = false,
            HideAddButton = true,
            HideRemoveButton = true
        )]
        public GradientInfo[] gradients;

        public Texture2D ToTexture()
        {
            if (gradients == null || gradients.Length < 1)
            {
                return null;
            }

            int height = gradients.Length * perheight;
           Texture2D tempTexture = new Texture2D(texWidth, height,TextureFormat.ARGB32,false);
            for (int j = 0; j < height; j++)
            {
                int gradientIndex = Mathf.FloorToInt(j / (float) height * gradients.Length);
                var gradient = gradients[gradientIndex];
                for (int i = 0; i < texWidth; i++)
                {
                    float percent = i / (float) texWidth;
                    Color col = gradient.gradient.Evaluate(percent);
                    tempTexture.SetPixel(i, j, col);
                    tempTexture.Apply();
                }
            }
            tempTexture.wrapMode = TextureWrapMode.Clamp;
            tempTexture.filterMode = FilterMode.Point;
            
            return tempTexture;
        }

    }
}