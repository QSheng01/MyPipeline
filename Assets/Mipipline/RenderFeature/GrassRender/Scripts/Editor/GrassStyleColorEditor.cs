#if UNITY_EDITOR
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.IO;
using Grass;

[CustomEditor(typeof(GrassStyleColorPainter))]
    public class GrassStyleColorEditor: UnityEditor.Editor
    {
        static readonly string BrushTexPath = "Assets/SYPackages/HWorld/Editor/Brushes/";
        public GrassStyleColorPainter _target;
        int _brushSizeInPourcent;
        ReorderableList _layerList;
        const int kElementHeight = 64;
        const int kElementPadding = 2;
        const int kElementNameHeight = 25;
        const int kElementNameWidth = 60;
        const int kElementColorHeight = 25;
        const int kElementColorWidth = 120;
        const int kElementBtnHeight = 20;
        const int kElementBtnWidth = 50;
        const int kElementImageWidth = 64;
        const int kElementImageHeight = 64;

        private SerializedProperty ownerTool;

        private void OnEnable()
        {
            if (_target == null)
                _target = target as GrassStyleColorPainter;

            ownerTool = serializedObject.FindProperty("_ownerTool");
        }



        private void InitBrush()
        {
            if (_target._brushTex != null && _target._brushTex.Length > 0) return;

            List<Texture> BrushList = new List<Texture>();
            Texture BrushesTL;
            int BrushNum = 0;
            do
            {
                BrushesTL = (Texture)AssetDatabase.LoadAssetAtPath(BrushTexPath + "Brush" + BrushNum + ".png", typeof(Texture));
                if (BrushesTL)
                {
                    BrushList.Add(BrushesTL);
                }
                BrushNum++;
            } while (BrushesTL);
            _target._brushTex = BrushList.ToArray();
        }

        public override void OnInspectorGUI()
        {
            InitBrush();
            _target._brushSize = EditorGUILayout.Slider("Brush Size", _target._brushSize, 1, 50);//Brush size
            _target._brushStronger = EditorGUILayout.Slider("Brush Stronger", _target._brushStronger, 0, 1f);//Brush Strength
            // EditorGUILayout.PropertyField(ownerTool);//_ownerTool

            using (new EditorGUILayout.HorizontalScope("box"))
            {
                _target._selBrush = GUILayout.SelectionGrid(_target._selBrush, _target._brushTex, 9, "gridlist", GUILayout.Width(340), GUILayout.Height(70));
            }

            if (_layerList == null)
            {
                _layerList = new ReorderableList(serializedObject, serializedObject.FindProperty("_layerColors"), false, true, false, false);
                _layerList.elementHeight = kElementHeight;
                _layerList.drawHeaderCallback = DrawHeader;
                _layerList.drawElementCallback = DrawLayerElement;
                _layerList.onSelectCallback = OnSelectLayerElement;
            }
            _layerList.DoLayoutList();

            _target._selLayer = Mathf.Clamp(_target._selLayer, 0, GrassStyleColorPainter.LayerNum);
            _layerList.index = _target._selLayer;
            serializedObject.ApplyModifiedProperties();
        }
        private void OnSelectLayerElement(ReorderableList list)
        {
            _target._selLayer = list.index;
        }

        private void DrawLayerElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect.y += kElementPadding;
            var rectImage = new Rect(rect.x + kElementPadding, rect.y, kElementImageWidth, kElementImageHeight);
            var rectName = new Rect(rectImage.x + kElementImageWidth + 10, rect.y, kElementNameWidth, kElementNameHeight);
            var rectColor = new Rect(rectImage.x + kElementImageWidth + 10, rectName.y + kElementHeight - kElementNameHeight - 5, kElementColorWidth, kElementColorHeight);
            var rectBtnUp = new Rect(rect.x + rect.width - kElementBtnWidth, rectName.y + 2, kElementBtnWidth, kElementBtnHeight);
            var rectBtnMid = new Rect(rect.x + rect.width - kElementBtnWidth, rectName.y + (kElementHeight - kElementBtnHeight)/2, kElementBtnWidth, kElementBtnHeight);
            var rectBtnDown = new Rect(rect.x + rect.width - kElementBtnWidth, rectName.y + kElementHeight - kElementBtnHeight - 2, kElementBtnWidth, kElementBtnHeight);
            GUI.DrawTexture(rectImage, _target._layerCatchTextures[index]);
            EditorGUI.LabelField(rectName, "Layer_" + index);
            _target._layerColors[index] = EditorGUI.ColorField(rectColor, _target._layerColors[index]);
            //if (GUI.Button(rectBtnUp, "导 入"))
            //{
            //    var path = EditorUtility.OpenFilePanelWithFilters("导入图层", Application.dataPath, new string[] { "Image files", "png,jpg,jpeg" });
            //    if (!string.IsNullOrEmpty(path))
            //    {
            //        FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read);
            //        byte[] bytes = new byte[stream.Length];
            //        stream.Read(bytes, 0, bytes.Length);
            //        var temp = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            //        temp.LoadImage(bytes);
            //        var size = _target._layerCatchTextures[index].width;
            //        if (temp.width != _target._resolution || temp.height != _target._resolution)
            //        {
            //            EditorUtility.DisplayDialog("错误提示", string.Format("需要{0}*{1}，该图片尺寸符合要求！", size, size), "确定");
            //        }
            //        else
            //        {
            //            _target._layerCatchTextures[index] = temp;
            //            _target._layerCatchTextures[index].Apply();
            //            _target._ownerTool.ResetCacheDataStyleColor(index);
            //        }
            //        stream.Close();
            //        stream.Dispose();
            //    }
            //}

            //if (GUI.Button(rectBtnDown, "导 出"))
            //{
            //    var path = EditorUtility.SaveFilePanelInProject("导出图层", "layer_" + index, "png", string.Empty);
            //    if (!string.IsNullOrEmpty(path))
            //    {
            //        var fullPath = Application.dataPath + "/" + path.Replace("Assets/", "");
            //        var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            //        var bytes = _target._layerCatchTextures[index].EncodeToPNG();
            //        stream.Write(bytes, 0, bytes.Length);
            //        stream.Close();
            //        stream.Dispose();
            //        AssetDatabase.Refresh();
            //    }
            //}


            if (GUI.Button(rectBtnUp, "覆 盖"))
            {
                ResetLayerColor(index);
            }
            // if (GUI.Button(rectBtnMid, "还 原"))
            // {
            //     RestoreLayerColorByTexture(index);
            // }
        }

        private void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "色层列表", EditorStyles.boldLabel);
        }

        /// <summary>
        /// 点击了 开启刷色 之后触发的代码
        /// </summary>
        /// <param name="raycastHit"></param>
        /// <param name="pixelUV"></param>
        /// <param name="rate"></param>
        public void OnSecenGUI(RaycastHit raycastHit, Vector2 pixelUV, float rate)
        {
            var e = Event.current;
            var eventType = e.type;
            float orthographicSize = _target._brushSize * 0.5f;
            //Debug.Log("orthographicSize:" + orthographicSize);
            _brushSizeInPourcent = (int)Mathf.Round(_target._brushSize * rate);//The size of the brush on the model

            Handles.color = new Color(1f, 1f, 0f, 1f);
            Handles.DrawWireDisc(raycastHit.point, raycastHit.normal, orthographicSize);//Display a circle at the mouse position according to the brush size

            Handles.color = new Color(1f, 0f, 0f, 1f);
            Handles.DrawLine(raycastHit.point, raycastHit.point + raycastHit.normal);//Displays the normal direction at the mouse position according to the brush size
            if (e.type != EventType.Repaint &&
                   e.type != EventType.Layout &&
                   e.type != EventType.MouseMove &&
                   e.type != EventType.MouseEnterWindow &&
                   e.type != EventType.MouseLeaveWindow &&
                   !e.shift && !e.control && e.button == 0 && e.isMouse && !e.alt)
            {
                //Debug.Log("styleColorChanged");
                var size = _target._layerCatchTextures[_target._selLayer].width;
                int puX = Mathf.FloorToInt(pixelUV.x * size);
                int puY = Mathf.FloorToInt(pixelUV.y * size);
                int x = Mathf.Clamp(puX - _brushSizeInPourcent / 2, 0, size - 1);
                int y = Mathf.Clamp(puY - _brushSizeInPourcent / 2, 0, size - 1);
                int width = Mathf.Clamp((puX + _brushSizeInPourcent / 2), 0, size) - x;
                int height = Mathf.Clamp((puY + _brushSizeInPourcent / 2), 0, size) - y;
                Texture2D TBrush = _target._brushTex[_target._selBrush] as Texture2D;//Get brush trait map
                float[] brushAlpha = new float[_brushSizeInPourcent * _brushSizeInPourcent];//Brush transparency
                for (int i = 0; i < _brushSizeInPourcent; i++)
                {
                    for (int j = 0; j < _brushSizeInPourcent; j++)
                    {
                        brushAlpha[j * _brushSizeInPourcent + i] = TBrush.GetPixelBilinear(((float)i) / _brushSizeInPourcent, ((float)j) / _brushSizeInPourcent).a;
                    }
                }
                Color[] terrainBay = _target._layerCatchTextures[_target._selLayer].GetPixels(x, y, width, height);

                //Debug.Log(string.Format("x = {0} y = {1} w = {2} h = {3} pixelUV = {4} ", x, y, width, height, pixelUV));
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        int index = (i * width) + j;
                        float Stronger = brushAlpha[Mathf.Clamp((y + i) - (puY - _brushSizeInPourcent / 2), 0, _brushSizeInPourcent - 1) * _brushSizeInPourcent +
                                            Mathf.Clamp((x + j) - (puX - _brushSizeInPourcent / 2), 0, _brushSizeInPourcent - 1)] * _target._brushStronger;

                        terrainBay[index] = Color.Lerp(terrainBay[index], _target._layerColors[_target._selLayer], Stronger);
                    }
                }

                _target._layerCatchTextures[_target._selLayer].SetPixels(x, y, width, height, terrainBay);
                _target._layerCatchTextures[_target._selLayer].Apply();

                _target._ownerTool.ResetCacheDataStyleColor((GrassElementData.ColorType)_target._selLayer, raycastHit.point, orthographicSize);
            }
        }

        public void RestGrassColorByTexture()
        {
            if (_target._ownerTool)
            {
                _target._ownerTool.ResetCacheDataStyleColor((GrassElementData.ColorType)_target._selLayer);
            }
        }

        private void ResetLayerColor(int index)
        {
            int size = _target._layerTextures[index].width;
            Color[] temp = new Color[size * size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    var id = (i * size) + j;
                    temp[id] = _target._layerColors[index];
                }
            }
            _target._layerCatchTextures[index].SetPixels(0, 0, size, size, temp);
            _target._layerCatchTextures[index].Apply();
            _target._ownerTool.ResetCacheDataStyleColor((GrassElementData.ColorType)_target._selLayer);
        }

        // private void RestoreLayerColor(int index)
        // {
        //     int size = _target._layerTextures[index].width;
        //     Color[] temp = new Color[size * size];
        //     for (int i = 0; i < size; i++)
        //     {
        //         for (int j = 0; j < size; j++)
        //         {
        //             var id = (i * size) + j;
        //             if (_target._layerTextures[index].GetPixel(j - size, i).r == _target._layerTextures[index].GetPixel(j - size, i).g)
        //             {
        //                 temp[id] = _target._layerColors[index];
        //             }
        //             else
        //             {
        //                 temp[id] = _target._layerTextures[index].GetPixel(j - size, i);
        //             }
        //         }
        //     }
        //     _target._layerCatchTextures[index].SetPixels(0, 0, size, size, temp);
        //     _target._layerCatchTextures[index].Apply();
        //     _target._ownerTool.ResetCacheDataStyleColor(_target._selLayer);
        // }
        //
        // private void RestoreLayerColorByTexture(int index)
        // {
        //     int LoadIndex = index + 1;
        //     var Tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/" + LoadIndex + ".png");
        //     int size = _target._layerTextures[index].width;
        //     Color[] temp = new Color[size * size];
        //     for (int i = 0; i < size; i++)
        //     {
        //         for (int j = 0; j < size; j++)
        //         {
        //             var id = (i * size) + j;
        //             temp[id] = Tex.GetPixel(j - size, i);
        //         }
        //     }
        //     _target._layerCatchTextures[index].SetPixels(0, 0, size, size, temp);
        //     _target._layerCatchTextures[index].Apply();
        //     _target._ownerTool.ResetCacheDataStyleColor(_target._selLayer);
        // }
    }
#endif
#endif
