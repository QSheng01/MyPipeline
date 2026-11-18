
#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SYArtToolkit.CaptureFrame
{

    public class DepthAndColorCapture : MonoBehaviour
    {

        //private  RenderTexture depthTextureTarget;    // 深度图的目标 Render Texture
        //private   RenderTexture colorTextureTarget;    // 颜色图的目标 Render Texture
        //private   Texture2D copiedDepthTexture;       // 用于存储拷贝的深度图
        //private   Texture2D copiedColorTexture;       // 用于存储拷贝的颜色图
        public readonly string scraperModeString = "ScreenScraper.scraperMode";
        public readonly string outPutFrameString = "ScreenScraper.outPutFrame";
        public readonly string outPutModeString = "ScreenScraper.outPutMode";
        public readonly string numberString = "ScreenScraper.number";
        public readonly string folderString = "ScreenScraper.folder";
        public readonly string openString = "ScreenScraper.open";

        //public Color deleteColor = new Color(0.1f,0.1f,0.1f,1);




        //private 
        public ScraperMode scraperMode;
        public OutPutMode outPutMode;
        public OutPutFrame outPutFrame;
        public int number;
        public string folder;
        public Camera cam;
        public GameObject obj;
        public bool approval = false;
        public bool open;
        public string newFolderName;
        public float maxTime = 60.0f;
        private int index;
        private int frame;
        private float time;
        private Dictionary<int, Texture2D> dic;
        private int frequency;
        [Range(0, 31)]
        public int layer = 26;

        private float originaltimescaleTime;


        private void Start()
        {
            //创建新文件夹
            if (string.IsNullOrEmpty(newFolderName)) newFolderName = "newScreenCapture";
            if (!System.IO.Directory.Exists(string.Format("{0}/{1}", folder, newFolderName)))
            {
                Directory.CreateDirectory(string.Format("{0}/{1}", folder, newFolderName));
            }
            else
            {
                int i = 0;
                while (System.IO.Directory.Exists(string.Format("{0}/{1}{2}", folder, newFolderName, i.ToString()))) ++i;
                Directory.CreateDirectory(string.Format("{0}/{1}{2}", folder, newFolderName, i.ToString()));
                newFolderName += i.ToString();
            }
            frequency = 0;
            dic = new Dictionary<int, Texture2D>();


            index = 0;
            frame = int.MaxValue;
            time = float.MaxValue;

            if (scraperMode == ScraperMode.GameObject)
            {
                dicLayer = new Dictionary<Transform, int>();
                if (obj != null)
                {
                    Queue<Transform> que = new Queue<Transform>();
                    que.Enqueue(obj.transform);
                    while (que.Count != 0)
                    {
                        var t = que.Dequeue();
                        for (int i = 0; i < t.childCount; ++i)
                        {
                            que.Enqueue((Transform)t.GetChild(i));
                        }
                        dicLayer.Add(t, t.gameObject.layer);
                    }

                }
            }


            if (outPutFrame == OutPutFrame.FrameFix)
            {

                Time.captureFramerate = number;
                originaltimescaleTime = Time.timeScale;
            }

        }

        private void Update()
        {
            if (Time.time > maxTime) return;
            else if (approval == false) return;


            if (outPutFrame == OutPutFrame.FrameFix)
            {

                StartCoroutine(CaptureFrame());

            }


            switch (outPutFrame)
            {
                case OutPutFrame.Frame:

                    if (frame >= number)
                    {
                        frame = 0;
                    }
                    else
                    {
                        frame++;
                        return;
                    }

                    break;
                case OutPutFrame.time:
                    if (time > number / 1000.0f)
                    {
                        time = 0;
                    }
                    else
                    {
                        time += Time.deltaTime;
                        return;
                    }
                    break;
                case OutPutFrame.Number:

                    if (CheckisAssces(frequency++))
                    {

                    }
                    else return;
                    break;


            }

            index++;
            switch (scraperMode)
            {

                case ScraperMode.Screen:
                    UpdateScreen();

                    break;
                case ScraperMode.Camera: UpdateCamera(); break;
                case ScraperMode.GameObject: UpdateGameObject(); break;



            }


            //if (ScreenScraperWindow.instance==null|| ScreenScraperWindow.instance.approval==false)
            //{
            //    approval = false;
            //    approval = false ;

            //    UnityEditor.EditorApplication.update -= Update;



            //}

            //Debug.Log(camera);


            //commandBuffer.ClearRenderTarget(true, true, Color.clear);

            //cam.targetTexture = colorTextureTarget;
            //cam.Render();

            //cam.targetTexture = depthTextureTarget;
            //cam.Render();
            //cam.targetTexture = null;

            // 执行渲染命令
            //Graphics.ExecuteCommandBuffer(commandBuffer); 
            //Camera.main.Render();
            // 重置渲染目标
            //commandBuffer.Release();

            //if (Input.GetKeyDown(KeyCode.X))
            //{
            //ReadDepthAndColorTextureData(index++);
            //}
            // 读取深度图和颜色图数据（可选）
        }


        IEnumerator CaptureFrame()
        {

            Time.timeScale = 0;
            // Yield to next frame and then start the rendering  
            // this is important, otherwise will ha
            // ve error  
            yield return new WaitForEndOfFrame();



            switch (scraperMode)
            {

                case ScraperMode.Screen:
                    UpdateScreen();

                    break;
                case ScraperMode.Camera: UpdateCamera(); break;
                case ScraperMode.GameObject: UpdateGameObject(); break;



            }


            Time.timeScale = originaltimescaleTime;
        }



        int nowcount = 0;

        bool CheckisAssces(int count)
        {
            if (count >= number)
            {
                //12    4
                int beishu = (int)Mathf.Log(count / number, 2);//1
                int value = ((int)Mathf.Pow(2, beishu + 1));//4
                if (count % value == 0)
                {
                    //            2   + 12  -         8
                    nowcount = count;
                    dic.Remove(value / 2 + count - (int)Mathf.Pow(2, beishu) * number);

                    return true;
                }
                else
                {
                    return false;
                }


            }
            else
            {
                nowcount = count;
                return true;
            }
        }

        void SavePicture(Texture2D tex)
        {

            switch (outPutFrame)
            {

                case OutPutFrame.Number:
                    dic.Add(nowcount, tex);
                    break;

                default:
                    //直接保存

                    System.IO.File.WriteAllBytes(string.Format("{0}/{1}/{1}.{2:D4}.png", folder, newFolderName, index), tex.EncodeToPNG());
                    break;
            }
        }

        void UpdateScreen()
        {
            var newTex = ScreenCapture.CaptureScreenshotAsTexture();
            SavePicture(newTex);

        }

        void UpdateCamera()
        {

            if (cam == null) return;
            RenderTexture renderTexture = RenderTexture.GetTemporary(cam.pixelWidth, cam.pixelHeight, 16, RenderTextureFormat.ARGBFloat);
            var originRenderTexture = cam.targetTexture;
            cam.targetTexture = renderTexture;
            cam.Render();
            cam.targetTexture = originRenderTexture;
            RenderTexture.active = renderTexture;
            var texture = new Texture2D(cam.pixelWidth, cam.pixelHeight, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, cam.pixelWidth, cam.pixelHeight), 0, 0);
            texture.Apply();
            RenderTexture.ReleaseTemporary(renderTexture);
            SavePicture(texture);
            //target
            //render
            //
        }

        Dictionary<Transform, int> dicLayer;
        void UpdateGameObject()
        {
            if (cam == null) return;
            else if (obj == null) return;





            RenderTexture renderTexture = RenderTexture.GetTemporary(cam.pixelWidth, cam.pixelHeight, 16, RenderTextureFormat.ARGBFloat);
            var originClearFlags = cam.clearFlags;
            var originColor = cam.backgroundColor;
            var origintRT = cam.targetTexture;
            var mask = cam.cullingMask;

            foreach (var t in dicLayer)
            {
                t.Key.gameObject.layer = layer;

            }
            cam.cullingMask = 1 << layer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            if (outPutMode == OutPutMode.White)
                cam.backgroundColor = Color.white;
            else if (outPutMode == OutPutMode.Black)
                cam.backgroundColor = Color.black;
            else
                cam.backgroundColor = Color.clear;

            cam.targetTexture = renderTexture;
            cam.Render();

            RenderTexture.active = renderTexture;
            var texture = new Texture2D(cam.pixelWidth, cam.pixelHeight, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, cam.pixelWidth, cam.pixelHeight), 0, 0);
            texture.Apply();


            //if (outPutMode == OutPutMode.Alpha)
            //{
            //    RenderTexture renderTextureDepth = RenderTexture.GetTemporary(cam.pixelWidth, cam.pixelHeight, 24, RenderTextureFormat.Depth);
            //    cam.targetTexture = renderTextureDepth;
            //    cam.Render();
            //    var textureDepth = new Texture2D(cam.pixelWidth, cam.pixelHeight, TextureFormat.R16, false);

            //    Graphics.Blit(renderTextureDepth, renderTexture, new Material(Shader.Find("Hidden/ArtToolkit/DepthColor")));

            //    RenderTexture.active = renderTexture;
            //    textureDepth.ReadPixels(new Rect(0, 0, cam.pixelWidth, cam.pixelHeight), 0, 0);
            //    textureDepth.Apply();
            //    var colors=texture.GetPixels();
            //    var depthes=textureDepth.GetPixels();




            //    for (int index = 0; index<colors.Length; index++)
            //    {
            //        if (depthes[index].r == 0 && colors[index]==Color.black ) colors[index] = Color.clear;
            //    }

            //    texture.SetPixels(colors);
            //    texture.Apply();
            //    RenderTexture.ReleaseTemporary(renderTextureDepth);
            //}

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(renderTexture);
            SavePicture(texture);
            foreach (var t in dicLayer)
            {
                t.Key.gameObject.layer = t.Value;
            }
            cam.cullingMask = mask;
            cam.clearFlags = originClearFlags;
            cam.backgroundColor = originColor;
            cam.targetTexture = origintRT;





            //替换背景
            //更换layer
            //替换rt
            //深度rt
            //do
            //还原rt
            //还原layer
            //还原背景
        }









        private void OnDisable()
        {

            if (outPutFrame == OutPutFrame.Number)
            {


                var sortedDictionary = dic.ToList();

                sortedDictionary.Sort((x, y) => x.Key.CompareTo(y.Key));
                // 输出排序后的值的列表
                List<Texture2D> sortedValues = sortedDictionary.Select(pair => pair.Value).ToList();

                int index = 0;
                foreach (var key in sortedValues)
                {

                    System.IO.File.WriteAllBytes(string.Format("{0}/{1}/{1}{2:D4}.png", folder, newFolderName, ++index), key.EncodeToPNG());
                }


            }
            try
            {
            }
            catch { }
            if (approval && open)
                EditorUtility.RevealInFinder(string.Format("{0}/{1}", folder, newFolderName));


        }

        //  void ReadDepthAndColorTextureData(int index = 0)
        //{
        //    // 读取深度图数据


        //    // 读取颜色图数据
        //    RenderTexture.active = colorTextureTarget;
        //    copiedColorTexture.ReadPixels(new Rect(0, 0, colorTextureTarget.width, colorTextureTarget.height), 0, 0);
        //    copiedColorTexture.Apply();
        //    //var bytes = copiedColorTexture.EncodeToPNG();
        //    //System.IO.File.WriteAllBytes(string.Format("PNG_Animations/{0}Color.png", i), bytes);

        //    // 现在可以在 copiedDepthTexture 和 copiedColorTexture 中访问深度图和颜色图的数据
        //    Graphics.Blit(depthTextureTarget, colorTextureTarget, new Material(Shader.Find("Unlit/depthColor")));


        //    RenderTexture.active = colorTextureTarget;
        //    copiedDepthTexture.ReadPixels(new Rect(0, 0, depthTextureTarget.width, depthTextureTarget.height), 0, 0);
        //    copiedDepthTexture.Apply();

        //    Debug.Log(copiedDepthTexture.GetPixel(0, 0));

        //    //bytes=copiedDepthTexture.EncodeToPNG();
        //    //System.IO.File.WriteAllBytes(string.Format("PNG_Animations/{0}Depth.png",i), bytes);


        //    for (int i = 0; i < colorTextureTarget.width; i++)
        //    {
        //        for (int j = 0; j < colorTextureTarget.height; j++)
        //        {
        //            Color color = copiedColorTexture.GetPixel(i, j);
        //            Color depth = copiedDepthTexture.GetPixel(i, j);
        //            if (color.r < 0.001f && color.g < 0.001f && color.b < 0.001f && depth.r == 0)
        //            {
        //                copiedColorTexture.SetPixel(i, j, Color.clear);
        //            }
        //        }
        //    }

        //    var bytes = copiedColorTexture.EncodeToPNG();
        //    System.IO.File.WriteAllBytes(string.Format("PNG_Animations/{0}Color.png", index), bytes);
        //}
    }




    public enum ScraperMode
    {
        [AboutString("截取整个屏幕的图像")]
        Screen = 0,    //// 整个屏幕都截取
        [AboutString("截取单个相机的图像")]
        Camera = 1,     //// 对单个相机进行截取
        [AboutString("截取单个物体的图像")]
        GameObject = 2  //// 对单个物体进行截取
    }
    //输出模式
    public enum OutPutMode
    {
        Black = 0,  ////  黑底
        White = 1,  ////  白底
        Alpha = 2   ////  透明
    }

    public enum OutPutType
    {
        PNG = 0,
        JPG = 1,
        TGA = 2
    }

    public enum OutPutFrame
    {
        [AboutString("固定帧率运行")]
        FrameFix = 0,
        [AboutString("输出一张需要帧数:")]
        Frame = 3,  ////  每输出一张需要多少Frame
        [AboutString("输出一张需要时间(ms):")]
        time = 1,   ////  每输出一张需要多少ms
        [AboutString("固定输出张数:")]
        Number = 2,////  固定输出number张
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class AboutStringAttribute : Attribute
    {
        public string Value { get; }

        public AboutStringAttribute(string value)
        {
            Value = value;
        }
        public static string GetEnumString(Enum value)
        {
            var type = value.GetType();

            var fieldInfo = type.GetField(value.ToString());
            var attributes = fieldInfo.GetCustomAttributes(typeof(AboutStringAttribute), false);
            return (attributes[0] as AboutStringAttribute)?.Value;
        }
    }


}


#endif