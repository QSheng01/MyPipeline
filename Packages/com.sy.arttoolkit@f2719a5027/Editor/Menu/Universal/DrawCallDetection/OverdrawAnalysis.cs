using System;
using System.Collections.Generic;
using UnityEngine;

namespace SYArtToolkit
{

    public class OverdrawAnalysis
    {
        #region 属性

        private struct pair
        {
            public int x; //实际
            public int y; //理论
        }

        private pair pixel;
        private Camera cam;
        private RenderTexture rt;
        private static OverdrawAnalysis instance;
        private Shader shader;
        private Dictionary<Camera, int> CamHash = new Dictionary<Camera, int>();

        private Dictionary<GameObject, Material[]> GameObjectHash = new Dictionary<GameObject, Material[]>();

        //private RenderPipelineAsset pri;
        private CameraClearFlags ccf;
        private Color cbc;
        private bool asd = false;

        private Material mat = null;

        #endregion

        #region 公共方法

        public static OverdrawAnalysis Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new OverdrawAnalysis();
                }

                return instance;
            }
        }



        public void setObj(GameObject obj, Camera camera, int mask)
        {

            cam = camera;
            ccf = cam.clearFlags;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cbc = cam.backgroundColor;
            cam.backgroundColor = new Color(0, 0, 0, 1);
            Camera[] C = Camera.allCameras;
            CamHash.Clear();
            foreach (var c in C)
            {
                CamHash.Add(c, c.cullingMask);
                c.cullingMask = 0;
            }

            cam.cullingMask = 1 << mask;

            if (mat == null) mat = new Material(Shader.Find("Unlit/OverDrawTest"));
            List<Transform> list = new List<Transform>();
            FindChilds(obj.transform, ref list);
            GameObjectHash.Clear();
            foreach (var o in list)
            {
                if (o.GetComponent<Renderer>() != null)
                {
                    GameObjectHash.Add(o.gameObject, o.GetComponent<Renderer>().sharedMaterials);
                    int num0 = o.GetComponent<Renderer>().sharedMaterials.Length;
                    Material[] tempm = new Material[num0];
                    for (int i = 0; i < num0; i++) tempm[i] = mat;
                    o.GetComponent<Renderer>().sharedMaterials = tempm;
                }
            }

            asd = true;
        }

        public void resetObj(bool isplaying)
        {
            if (!isplaying) asd = false;
            if (asd)
            {
                cam.clearFlags = ccf;
                cam.backgroundColor = cbc;
                foreach (var p in CamHash)
                {
                    p.Key.cullingMask = p.Value;
                }

                asd = false;

                foreach (var g in GameObjectHash)
                {
                    g.Key.GetComponent<Renderer>().sharedMaterials = g.Value;
                }
            }

            CamHash.Clear();
            GameObjectHash.Clear();
        }


        private void FindChilds(Transform obj, ref List<Transform> list)
        {
            list.Add(obj);
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                FindChilds(obj.transform.GetChild(i), ref list);
            }
        }


        //private List<GameObject> 

        public float GetRate0()
        {
            return pixel.x / Mathf.Max(pixel.y, 0.001f);
        }

        public float GetRate1()
        {
            return pixel.x / (float)(cam.pixelHeight * cam.pixelWidth);
        }

        // Update is called once per frame
        public void Update()
        {
            RecordOverDrawData();
        }

        public void Reset()
        {
            pixel.x = 0;
            pixel.y = 0;
        }

        #endregion

        #region 私有方法

        private OverdrawAnalysis()
        {

        }



        private void RecordOverDrawData()
        {

            RenderTexture temp = cam.targetTexture;
            RenderTexture activeTextrue = RenderTexture.active;
            rt = RenderTexture.GetTemporary(cam.pixelWidth, cam.pixelHeight);
            cam.targetTexture = rt;

            cam.Render();
            //cam.RenderWithShader(Shader.Find("Unlit/OverDrawTest"), "RenderType");
            RenderTexture.active = rt;
            Texture2D texture = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            pixel = GetOverDrawData(texture);
            //恢复之前激活的渲染纹理
            RenderTexture.active = activeTextrue;
            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.DestroyImmediate(texture);
            cam.targetTexture = temp;

        }
        //"RenderType"

        private pair GetOverDrawData(Texture2D texture)
        {
            int all = texture.width * texture.height;
            var pixels = texture.GetPixels();
            pair res;
            res.x = 0;
            res.y = 0;
            for (int index = 0; index < all; ++index)
            {
                float r = pixels[index].r;
                if (r > 0)
                {
                    ++res.y;
                    res.x += Convert.ToInt32(r / 0.00392);
                }
            }

            return res;
        }



        #endregion

    }
}