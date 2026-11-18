using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

public class RenderTex2Tex3D : MonoBehaviour
{
    public ComputeShader sliceCS;
    public string nameOfTheAsset = "unname";

    public int width, height, depth;

    RenderTexture Copy3DSliceToRenderTexture(RenderTexture source, int layer)
    {
        RenderTexture render = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
        render.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        render.enableRandomWrite = true;
        render.wrapMode = TextureWrapMode.Clamp;
        render.Create();

        int kernelIndex = sliceCS.FindKernel("CSMain");
        sliceCS.SetTexture(kernelIndex, "voxels", source);
        sliceCS.SetInt("layer", layer);
        sliceCS.SetTexture(kernelIndex, "Result", render);
        sliceCS.Dispatch(kernelIndex, width, height, 1);

        return render;
    }

    Texture2D ConvertFromRenderTexture(RenderTexture rt)
    {
        Texture2D output = new Texture2D(width, height);
        RenderTexture.active = rt;
        output.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        output.Apply();
        return output;
    }

    [Button("save")]
    public Texture3D Save(RenderTexture selectedRenderTexture,string name)
    {

        width = selectedRenderTexture.width;
        height = selectedRenderTexture.height;
        depth = selectedRenderTexture.volumeDepth;
        Texture3D output = new Texture3D(width, height, depth, TextureFormat.RGBA64, false);
        RenderTexture[] layers = new RenderTexture[depth];
        for (int i = 0; i < depth; i++)
            layers[i] = Copy3DSliceToRenderTexture(selectedRenderTexture, i);

        Texture2D[] finalSlices = new Texture2D[depth];
        for (int i = 0; i < depth; i++)
            finalSlices[i] = ConvertFromRenderTexture(layers[i]);
#if UNITY_EDITOR
        output.filterMode = FilterMode.Trilinear;
        output.wrapMode = TextureWrapMode.Clamp;
        Color[] outputPixels = output.GetPixels();
        for (int k = 0; k < depth; k++)
        {
            Color[] layerPixels = finalSlices[k].GetPixels();
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                {
                    outputPixels[i + j * width + k * width * height] = layerPixels[i + j * width];
                }
        }

        output.SetPixels(outputPixels);
        output.Apply();

        AssetDatabase.CreateAsset(output, "Assets/" + name + ".asset");
#endif
        return output;

    }
}