

using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SYArtToolkit
{

    public class CropTextures : TextureBase
    {
        private MouseLocationStatus locationStatus = MouseLocationStatus.Else;
        private Vector2 mousePosition;
        private Vector2 lastMousePosition;
        private Texture2D sourceTex0;
        bool isDrag = false;
        private Material previewClipmat;
        private Material targetClipmat;

        Vector2Int vTemp = new Vector2Int(64, 64);
        private Vector4 clipArea = new Vector4(256 - 64, 256 - 64, 256 + 64, 256 + 64);
        private Vector4 clipRect;
        private Rect previewRect = new Rect(0, 0, 512, 512);

        private Vector2Int outTextureSize = new Vector2Int(0, 0);
        RenderTexture rt;
        Color[] colorAll;
        private string outTextureFilePath = "";
        private Texture2D basemap;
        private bool mipmap = false;
        private bool alphaistrans = true;


        Vector2 scroll = Vector2.one;
        private Texture2D background = new Texture2D(512, 512);

        public override void InterfaceShow(bool atThisWindow, Rect noRect)
        {
            scroll = EditorGUILayout.BeginScrollView(scroll, false, false);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(background);
            SetColorSpaceValue();
            mousePosition = Event.current.mousePosition;
            if (atThisWindow)
            {
                if (Event.current.type == EventType.DragUpdated)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                }
                else if (Event.current.type == EventType.DragExited)
                {
                    if (DragAndDrop.paths != null)
                    {

                        sourceTex0 = (Texture2D)AssetDatabase.LoadAssetAtPath(DragAndDrop.paths[0], typeof(Texture2D));
                        outTextureFilePath = AssetDatabase.GetAssetPath(sourceTex0);
                        sourceTex0 = DeCompress(sourceTex0);
                    }
                }
            }

            if (sourceTex0)
            {
                if (sourceTex0.width == sourceTex0.height) //裁剪框渲染
                {
                    previewRect = new Rect(0, 0, 512, 512);
                }
                else if (sourceTex0.width > sourceTex0.height)
                {
                    float aspect = sourceTex0.height / (float)sourceTex0.width;
                    if (aspect < 0.5625f)
                        previewRect = new Rect(0, 0 + (512 - (512 * aspect)) / 2, 512, 512 * aspect);
                    else previewRect = new Rect(0 + ((512 - (512 * aspect)) / 2), 0, 512 * aspect, 512);
                }
                else
                {
                    float aspect = sourceTex0.width / (float)sourceTex0.height;
                    previewRect = new Rect(0 + ((512 - (512 * aspect)) / 2), 0, 512 * aspect, 512);
                }

                RenderTexture rt = RenderTexture.GetTemporary((int)previewRect.width, (int)previewRect.height);
                RenderTexture prous = RenderTexture.active;
                RenderTexture.active = rt;
                Graphics.Blit(sourceTex0, previewClipmat);
                Texture2D preview = new Texture2D((int)previewRect.width, (int)previewRect.height);
                preview.ReadPixels(new Rect(0, 0, (int)previewRect.width, (int)previewRect.height), 0, 0);
                preview.Apply();
                RenderTexture.active = prous;
                RenderTexture.ReleaseTemporary(rt);
                GUI.Label(new Rect(previewRect.x + 5, previewRect.y + 3, preview.width, preview.height), preview);
                Object.DestroyImmediate(preview);
                //EditorGUI.DrawPreviewTexture(previewRect, sourceTex0, previewClipmat);
                if (!isDrag)
                {

                    if (Event.current.isScrollWheel)
                    {
                        float temp = Event.current.delta.y;
                        if (temp > 0)
                        {

                            if (clipArea.x - temp > previewRect.x) clipArea.x -= temp;
                            else clipArea.x = previewRect.x;
                            if (clipArea.z + temp < previewRect.x + previewRect.width) clipArea.z += temp;
                            else clipArea.z = previewRect.x + previewRect.width;
                            if (clipArea.y - temp > previewRect.y) clipArea.y -= temp;
                            else clipArea.y = previewRect.y;
                            if (clipArea.w + temp < previewRect.y + previewRect.height) clipArea.w += temp;
                            else clipArea.w = previewRect.y + previewRect.height;
                        }
                        else
                        {
                            if (clipArea.x - temp + 8 < clipArea.z)
                            {
                                clipArea.x -= temp;
                                clipArea.z += temp;
                            }

                            if (clipArea.y - temp + 8 < clipArea.w)
                            {
                                clipArea.y -= temp;
                                clipArea.w += temp;
                            }
                        }
                    }


                    MouseLocationStatus statusTemp = WhichLocationStatus(clipArea, mousePosition, 8);
                    MouseCursor ctemp = MouseCursor.Arrow;
                    switch (statusTemp)
                    {
                        case MouseLocationStatus.Left:
                        case MouseLocationStatus.Right:
                            ctemp = MouseCursor.ResizeHorizontal;
                            break;
                        case MouseLocationStatus.Bottom:
                        case MouseLocationStatus.Top:
                            ctemp = MouseCursor.ResizeVertical;
                            break;
                        case MouseLocationStatus.RightBottom:
                        case MouseLocationStatus.LeftTop:
                            ctemp = MouseCursor.ResizeUpLeft;
                            break;
                        case MouseLocationStatus.LeftBottom:
                        case MouseLocationStatus.RightTop:
                            ctemp = MouseCursor.ResizeUpRight;
                            break;
                        case MouseLocationStatus.Drag:
                            ctemp = MouseCursor.Pan;
                            break;
                    }

                    EditorGUIUtility.AddCursorRect(new Rect(0, 0, 512, 512), ctemp);
                    if (Event.current.type == EventType.MouseDown)
                    {
                        locationStatus = statusTemp;
                        isDrag = true;
                    }

                }
                else
                {
                    EditorGUIUtility.AddCursorRect(new Rect(0, 0, 512, 512), MouseCursor.MoveArrow);

                    if ((locationStatus & MouseLocationStatus.Left) ==
                        MouseLocationStatus.Left)
                        clipArea.x = Mathf.Clamp(mousePosition.x, previewRect.x, clipArea.z - 8);

                    else if ((locationStatus & MouseLocationStatus.Right) ==
                             MouseLocationStatus.Right)
                        clipArea.z = Mathf.Clamp(mousePosition.x, clipArea.x + 8,
                            previewRect.x + previewRect.width);

                    if ((locationStatus & MouseLocationStatus.Top) ==
                        MouseLocationStatus.Top)
                        clipArea.y = Mathf.Clamp(mousePosition.y, previewRect.y, clipArea.w - 8);

                    else if ((locationStatus & MouseLocationStatus.Bottom) ==
                             MouseLocationStatus.Bottom)
                        clipArea.w = Mathf.Clamp(mousePosition.y, clipArea.y + 8,
                            previewRect.y + previewRect.height);
                    else if (locationStatus == MouseLocationStatus.Drag)
                    {
                        float xtemp = mousePosition.x - lastMousePosition.x;
                        float ytemp = mousePosition.y - lastMousePosition.y;

                        if (xtemp + clipArea.x < previewRect.x)
                        {
                            clipArea.z = previewRect.x + (clipArea.z - clipArea.x);
                            clipArea.x = previewRect.x;
                        }
                        else if (xtemp + clipArea.z > previewRect.x + previewRect.width)
                        {
                            clipArea.x = previewRect.x + previewRect.width - (clipArea.z - clipArea.x);
                            clipArea.z = previewRect.x + previewRect.width;
                        }
                        else
                        {
                            clipArea.x += xtemp;
                            clipArea.z += xtemp;
                        }


                        if (ytemp + clipArea.y < previewRect.y)
                        {
                            clipArea.w = clipArea.y + (clipArea.w - clipArea.y);
                            clipArea.y = previewRect.y;
                        }
                        else if (ytemp + clipArea.w > previewRect.y + previewRect.height)
                        {
                            clipArea.y = previewRect.y + previewRect.height - (clipArea.w - clipArea.y);
                            clipArea.w = previewRect.y + previewRect.height;
                        }
                        else
                        {
                            clipArea.y += ytemp;
                            clipArea.w += ytemp;
                        }


                    }

                }



            }
            else
            {
                // if (basemap == null) basemap = Resources.Load<Texture2D>("底图1");
                // EditorGUI.DrawPreviewTexture(previewRect, basemap);
            }


            switch (Event.current.type)
            {
                case EventType.MouseUp:
                    locationStatus = MouseLocationStatus.Else;
                    isDrag = false;
                    break;
                case EventType.MouseEnterWindow:
                    break;
                case EventType.MouseLeaveWindow:
                    locationStatus = MouseLocationStatus.Else;
                    isDrag = false;
                    break;
            }

            if (sourceTex0)
            {
                clipRect = new Vector4(clipArea.x - previewRect.x, previewRect.y - clipArea.y, clipArea.z - clipArea.x,
                    clipArea.w - clipArea.y);

                clipRect.x /= previewRect.width;
                clipRect.y /= previewRect.height;
                clipRect.z /= previewRect.width;
                clipRect.w /= previewRect.height;
                float uu = (sourceTex0.width) * clipRect.z; //float uu = (sourceTex0.width * uvscale) * clipRect.z;
                float vv = (sourceTex0.height) * clipRect.w; //float vv = (sourceTex0.height * uvscale) * clipRect.w;
                outTextureSize = new Vector2Int((int)uu, (int)vv);
                targetClipmat.SetVector("_Till", clipRect);

                int wid = (int)(clipArea.z - clipArea.x);
                int hei = (int)(clipArea.w - clipArea.y);
                RenderTexture rt0 = RenderTexture.GetTemporary(wid, hei);
                RenderTexture prous0 = RenderTexture.active;
                RenderTexture.active = rt0;
                Graphics.Blit(sourceTex0, targetClipmat);
                Texture2D preview0 = new Texture2D(wid, hei);
                preview0.ReadPixels(new Rect(0, 0, wid, hei), 0, 0);
                preview0.Apply();
                RenderTexture.active = prous0;
                RenderTexture.ReleaseTemporary(rt0);
                GUI.Label(new Rect(clipArea.x + 5, clipArea.y + 3, clipArea.z - clipArea.x, clipArea.w - clipArea.y), preview0);
                Object.DestroyImmediate(preview0);





                //Graphics.DrawTexture(new Rect(clipArea.x, clipArea.y, clipArea.z - clipArea.x, clipArea.w - clipArea.y), sourceTex0, targetClipmat);
                GUI.Label(new Rect(550, 150, 200, 50), "输出像素大小:  " + outTextureSize.ToString());

                //GUI.Label(new Rect(520, 0, 60, 30), "输出像素:");
                vTemp = EditorGUI.Vector2IntField(new Rect(550, 100, 170, 30), "", vTemp);
                if (GUI.Button(new Rect(550, 130, 50, 30), "应用"))
                {
                    float cliprectz = (float)vTemp.x / sourceTex0.width * previewRect.width;
                    float cliprectw = (float)vTemp.y / sourceTex0.height * previewRect.height;

                    if (clipArea.x + cliprectz > previewRect.x + previewRect.width ||
                        clipArea.y + cliprectw > previewRect.y + previewRect.height)
                    {
                        Debug.Log("超出预选框,请重新输入");
                    }
                    else
                    {
                        outTextureSize = vTemp;
                        clipArea.z = clipArea.x + cliprectz;
                        clipArea.w = clipArea.y + cliprectw;
                    }

                }
            }

            EditorGUILayout.BeginVertical();
            GUILayout.Space(200);
            SetMipMapAndTransparency(ref mipmap, ref alphaistrans);

            GUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("另存"))
            {
                if (sourceTex0 != null)
                {
                    Texture2D outpicture = SaveCrop();
                    string path = Save(Application.dataPath, sourceTex0.name, outpicture, outTextureFilePath);
                    TextureImporterSet(AbsoluteToRelativelyPath(path), mipmap, alphaistrans);
                    Object.DestroyImmediate(outpicture);
                    GUIUtility.ExitGUI();

                }
                else
                {
                    Debug.Log("输入为空!");
                }
            }

            if (GUILayout.Button("替换"))
            {
                if (sourceTex0 != null)
                {
                    Texture2D outpicture = SaveCrop();
                    Save(RelativelyToAbsolutePath(outTextureFilePath), outpicture);
                    GUIUtility.ExitGUI();

                    Object.DestroyImmediate(outpicture);

                }
                else
                {
                    Debug.Log("输入为空!");
                }
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(20);
            GUILayout.Space(20);
            GUILayout.Label("拖入图片到窗口开始剪切");
            lastMousePosition = mousePosition;
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.EndScrollView();
        }
        public CropTextures()
        {
            previewClipmat = new Material(Shader.Find("Hidden/ArtToolkit/CropTarget"));
            targetClipmat = new Material(Shader.Find("Hidden/ArtToolkit/CropPreview"));
            SetColorSpaceValue();
        }

        public MouseLocationStatus WhichLocationStatus(Vector4 area, Vector2 position, float size)
        {
            if (inRect(new Vector4(area.x + size, area.y + size, area.z - size, area.w - size), position))
            {
                return MouseLocationStatus.Drag;
            }
            MouseLocationStatus status1, status2;
            if (inRect(new Vector4(area.x - size, area.y - size, area.x + size, area.w + size), position))
            {
                status1 = MouseLocationStatus.Left;
            }
            else if (inRect(new Vector4(area.z - size, area.y - size, area.z + size, area.w + size), position))
            {
                status1 = MouseLocationStatus.Right;
            }
            else status1 = MouseLocationStatus.Else;

            if (inRect(new Vector4(area.x - size, area.y - size, area.z + size, area.y + size), position))
            {
                status2 = MouseLocationStatus.Top;
            }
            else if (inRect(new Vector4(area.x - size, area.w - size, area.z + size, area.w + size), position))
            {
                status2 = MouseLocationStatus.Bottom;
            }
            else status2 = MouseLocationStatus.Else;

            return status1 | status2;
        }

        public bool inRect(Vector4 area, Vector2 pointPosition)
        {
            return pointPosition.x > area.x
                   && pointPosition.x < area.z
                   && pointPosition.y > area.y
                   && pointPosition.y < area.w;
        }

        Texture2D SaveCrop()
        {
            targetClipmat.SetVector("_Till", clipRect);
            rt = new RenderTexture(outTextureSize.x, outTextureSize.y, 0, RenderTextureFormat.ARGBFloat);
            RenderTexture.active = rt;//RT运行

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, rt.width, rt.height, 0);
            Graphics.DrawTexture(new Rect(0, 0, rt.width, rt.height), sourceTex0, targetClipmat);//渲染.绘制tex，参数传入矩形和要绘制进去的贴图
            GL.PopMatrix();

            Texture2D texture2D = new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false);//, TextureFormat.RGBAFloat, false
            texture2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
            texture2D.Apply();
            if (PlayerSettings.colorSpace == ColorSpace.Linear)
            {
                Color[] cols = texture2D.GetPixels();
                for (int i = 0; i < cols.Length; ++i)
                {
                    cols[i].r = Mathf.Pow(cols[i].r, 0.43f);
                    cols[i].g = Mathf.Pow(cols[i].g, 0.43f);
                    cols[i].b = Mathf.Pow(cols[i].b, 0.43f);
                }

                texture2D.SetPixels(cols);
                texture2D.Apply();
            }

            RenderTexture.active = null;//RT置空
            return texture2D;
        }
    }
}