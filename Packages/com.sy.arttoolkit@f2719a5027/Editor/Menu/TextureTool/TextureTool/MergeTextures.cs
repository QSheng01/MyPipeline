using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

//using System.Runtime.InteropServices;[FieldOffset(0)]  
namespace SYArtToolkit
{
    public class MergeTextures : TextureBase
    {
        public MergeTextures()
        {
            SetColorSpaceValue();
            texarrays = ScriptableObject.CreateInstance<Texture2DArrays>();
            serObj = new SerializedObject(texarrays);
            gosPty = serObj.FindProperty("searchResults");
        }

        public class Texture2DArrays : ScriptableObject
        {
            public List<Texture2D> searchResults = new List<Texture2D>();
        }

        private Texture2DArrays texarrays;
        private SerializedObject serObj;
        private SerializedProperty gosPty;

        private Texture2D outFinalTexture = null;
        private int outIndexSizeX = 0;
        private int outIndexSizeY = 0;
        private int outTextureSizeX = 512;
        private int outTextureSizeY = 512;

        private string outFileName = "OutTexture";

        private int emptyPixel = 1;

        //private bool isComposite = false;
        private bool mipMapEnable = false;
        private bool alphaIsTransparency = true;


        private Vector2 scroll1 = new Vector2(0, 0);


        public override void InterfaceShow(bool atThisWindow, Rect noRect)
        {
            scroll1 = EditorGUILayout.BeginScrollView(scroll1, false, false);
            GUILayout.Space(10);

            InInterface();

            GUILayout.Space(20);
            SetMipMapAndTransparency(ref mipMapEnable, ref alphaIsTransparency);
            if (GUILayout.Button("合成"))
            {
                List<Texture2D> tempTexList = new List<Texture2D>();
                //List<Vector2Int> tempVec2List = new List<Vector2Int>();
                foreach (var tex in texarrays.searchResults)
                {
                    if (tex != null)
                    {
                        tempTexList.Add(DeCompress(tex));
                        //处理输入
                    }
                }

                if (tempTexList.Count == 0)
                {
                    Debug.Log("输入贴图为空");
                    return;
                }

                int all = 0;
                if (all > outTextureSizeX * outTextureSizeY) //仅当texture全为正方形时成立
                {
                    Debug.Log("容量不足,请增加输出贴图的大小");
                    return;
                }

                outFinalTexture = new Texture2D(outTextureSizeX, outTextureSizeY);

                Color[] colors = new Color[outTextureSizeX * outTextureSizeY];

                outFinalTexture.SetPixels(colors);

                QuickSearchTexture2D quickSearchTexture2D = new QuickSearchTexture2D();
                quickSearchTexture2D.Construct(tempTexList);
                var res = DFS(quickSearchTexture2D, new Vector2Int(0, 0), new Vector2Int(outTextureSizeX, outTextureSizeY));
                if (quickSearchTexture2D.Empty() == false) Debug.Log("合成失败，请增加大小");
                else
                {
                    var c = res.target;
                    if (c != null) c.Merge(outFinalTexture, emptyPixel);
                }

                string absolutepath = Save(Application.dataPath, outFileName, outFinalTexture);
                TextureImporterSet(AbsoluteToRelativelyPath(absolutepath), mipMapEnable, alphaIsTransparency);


                Debug.Log("成功");
            }

            EditorGUILayout.EndScrollView();
        }

        private Vector2 insroll;

        // private class sortTex : IComparer<Texture2D>
        // {
        //     public int Compare(Texture2D x, Texture2D y)
        //     {
        //         return y.height.CompareTo(x.height); //y对比x ,从大到小排列
        //     }
        // }


        // private class ContainBlock
        // {
        //     private Texture2D tex = null;
        //     private List<ContainBlock> listCon = null;
        //     private int iStart = 0, jStart = 0;
        //     private int size;
        //
        //     public ContainBlock(List<Texture2D> listTex, ref int listIndex, int _size = 0, int istart = 0,
        //         int jstart = 0, int level = 0)
        //     {
        //         if (listTex.Count <= listIndex || _size == 0 || listTex[listIndex] == null) return;
        //
        //         size = _size;
        //
        //         if (listTex[listIndex].height == _size)
        //         {
        //             tex = listTex[listIndex];
        //
        //             ++listIndex;
        //             iStart = istart;
        //             jStart = jstart;
        //         }
        //         else
        //         {
        //             int nextSize = size / 2;
        //             int imid = istart + nextSize;
        //             int jmid = jstart + nextSize;
        //             listCon = new List<ContainBlock>();
        //             listCon.Add(new ContainBlock(listTex, ref listIndex, nextSize, istart, jmid, level + 1)); //lu
        //             listCon.Add(new ContainBlock(listTex, ref listIndex, nextSize, imid, jmid, level + 1)); //ru
        //             listCon.Add(new ContainBlock(listTex, ref listIndex, nextSize, istart, jstart, level + 1)); //ld
        //             listCon.Add(new ContainBlock(listTex, ref listIndex, nextSize, imid, jstart, level + 1)); //rd
        //         }
        //     }
        //
        //     public void print(Texture2D outTex, int outSize, int empty = 0)
        //     {
        //         if (tex != null)
        //         {
        //             for (int i = iStart + empty; i < iStart + size - empty; ++i)
        //             {
        //                 for (int j = jStart + empty; j < jStart + size - empty; ++j)
        //                 {
        //                     outTex.SetPixel(i, j, tex.GetPixel(i - iStart, j - jStart));
        //                 }
        //             }
        //         }
        //         else if (listCon != null)
        //         {
        //             foreach (var con in listCon)
        //             {
        //                 con.print(outTex, outSize, empty);
        //             }
        //         }
        //         else return;
        //     }
        // }
        //

        private void InInterface()
        {
            if (texarrays == null)
            {
                texarrays = ScriptableObject.CreateInstance<Texture2DArrays>();
                serObj = new SerializedObject(texarrays);
                gosPty = serObj.FindProperty("searchResults");
            }

            GUILayout.BeginHorizontal();
            insroll = GUILayout.BeginScrollView(insroll, GUILayout.Width(320), GUILayout.Height(300));
            EditorGUILayout.PropertyField(gosPty, new GUIContent("输入贴图"), true, GUILayout.Width(300));
            GUILayout.EndScrollView();

            serObj.ApplyModifiedProperties();

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("输出像素的宽度：   " + outTextureSizeX.ToString() + "像素");
                outIndexSizeX = EditorGUILayout.IntSlider(GUIContent.none, outIndexSizeX, 0, 4, GUILayout.Width(200));
                outTextureSizeX = (int)Mathf.Pow(2, outIndexSizeX + 6);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("输出像素的高度：   " + outTextureSizeY.ToString() + "像素");
                outIndexSizeY = EditorGUILayout.IntSlider(GUIContent.none, outIndexSizeY, 0, 4, GUILayout.Width(200));
                outTextureSizeY = (int)Mathf.Pow(2, outIndexSizeY + 6);
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("输出像素的大小：   " + outTextureSizeX.ToString() + " * " + outTextureSizeY.ToString());


            GUILayout.Space(10);

            emptyPixel = EditorGUILayout.IntSlider("图片之间的空隙:", emptyPixel, 0, 4, GUILayout.Width(300));
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            //OutFilePath = EditorGUILayout.TextField("输出文件的位置：.../Assets", OutFilePath);

            outFileName = EditorGUILayout.TextField("输出图片的名字：", outFileName);
            if (outFileName == "") outFileName = "OutTexture";
        }




        private (ContainBlock target, float ratio, List<Texture2D> texture2Ds) DFS(QuickSearchTexture2D quickSearch,
            Vector2Int startPoint, Vector2Int size)
        {
            List<Texture2D> texture2DList = new List<Texture2D>();
            if (quickSearch.Empty() || size.x < quickSearch.MinSize.x || size.y < quickSearch.MinSize.y)
                return (null, 0, texture2DList);
            float success = 0;
            ContainBlock containBlock = new ContainBlock(startPoint, size);
            if (size.y > quickSearch.MaxSize.y)
            {
                var down = DFS(quickSearch,
                    new Vector2Int(startPoint.x, startPoint.y + size.y / 2), new Vector2Int(size.x, size.y / 2));
                var up = DFS(quickSearch, startPoint,
                    new Vector2Int(size.x, size.y / 2));
                containBlock.ContainBlockStruct =
                    new ContainBlockStruct(new List<ContainBlock>() { down.target, up.target });
                containBlock.IsTex = false;
                success = 0.5f * up.ratio + 0.5f * down.ratio;
                texture2DList.AddRange(up.texture2Ds);
                texture2DList.AddRange(down.texture2Ds);
                return (containBlock, success, texture2DList); //一定没问题
            }

            if (size.x > quickSearch.MaxSize.x)
            {
                var left = DFS(quickSearch, startPoint,
                    new Vector2Int(size.x / 2, size.y));
                var right = DFS(quickSearch,
                    new Vector2Int(startPoint.x + size.x / 2, startPoint.y), new Vector2Int(size.x / 2, size.y));
                containBlock.ContainBlockStruct =
                    new ContainBlockStruct(new List<ContainBlock>() { left.target, right.target });
                containBlock.IsTex = false;
                success = 0.5f * left.ratio + 0.5f * right.ratio;
                texture2DList.AddRange(left.texture2Ds);
                texture2DList.AddRange(right.texture2Ds);
                return (containBlock, success, texture2DList); //一定没问题
            }

            var tempTex = quickSearch.GetTexture(size);
            if (tempTex != null)
            {
                containBlock.ContainBlockStruct = new ContainBlockStruct(tempTex);
                containBlock.IsTex = true;
                texture2DList.Add(tempTex);
                return (containBlock, 1, texture2DList);
            }
            else
            {
                var down = DFS(quickSearch,
                    new Vector2Int(startPoint.x, startPoint.y + size.y / 2), new Vector2Int(size.x, size.y / 2));
                var up = DFS(quickSearch, startPoint,
                    new Vector2Int(size.x, size.y / 2));
                containBlock.ContainBlockStruct =
                    new ContainBlockStruct(new List<ContainBlock>() { down.target, up.target });
                containBlock.IsTex = false;
                texture2DList.AddRange(up.texture2Ds);
                texture2DList.AddRange(down.texture2Ds);
                success = 0.5f * up.ratio + 0.5f * down.ratio;
                //左右
                //上下
                if (success == 1) return (containBlock, 1, texture2DList); //
                {

                    quickSearch.RefreshTexture(texture2DList);
                    List<Texture2D> listTex = new List<Texture2D>();

                    var left = DFS(quickSearch, startPoint,
                        new Vector2Int(size.x / 2, size.y));
                    var right = DFS(quickSearch,
                        new Vector2Int(startPoint.x + size.x / 2, startPoint.y), new Vector2Int(size.x / 2, size.y));
                    containBlock.IsTex = false;
                    float success2 = 0.5f * left.ratio + 0.5f * right.ratio;

                    listTex.AddRange(left.texture2Ds);
                    listTex.AddRange(right.texture2Ds);
                    if (success2 > success)
                    {
                        containBlock.ContainBlockStruct =
                            new ContainBlockStruct(new List<ContainBlock>() { left.target, right.target });
                        return (containBlock, success2, listTex);
                    }
                    else
                    {
                        quickSearch.RefreshTexture(listTex);

                        quickSearch.DeleteTexture(texture2DList);
                        return (containBlock, success, texture2DList);
                    }
                }
            }
        }

        private class QuickSearchTexture2D
        {
            private Vector2Int maxSize;
            private Vector2Int minSize;
            private Dictionary<(int width, int height), Stack<Texture2D>> elements; //元组

            public Vector2Int MaxSize
            {
                get { return maxSize; }
            }

            public Vector2Int MinSize
            {
                get { return minSize; }
            }

            public QuickSearchTexture2D()
            {
                maxSize = new Vector2Int(0, 0);
                minSize = new Vector2Int(Int32.MaxValue, Int32.MaxValue);
                elements = new Dictionary<(int, int), Stack<Texture2D>>();
            }

            public void Construct(List<Texture2D> _listTexture2D = null)
            {
                if (_listTexture2D == null)
                {
                    Debug.LogWarning("输入为空");
                    return;
                }

                maxSize = new Vector2Int(0, 0);
                minSize = new Vector2Int(Int32.MaxValue, Int32.MaxValue);
                elements = new Dictionary<(int, int), Stack<Texture2D>>();
                foreach (var texture2D in _listTexture2D)
                {
                    if (texture2D == null) continue;
                    Vector2Int size = new Vector2Int(texture2D.width, texture2D.height);
                    maxSize = Vector2Int.Max(size, maxSize);
                    minSize = Vector2Int.Min(size, minSize);
                    var key = (texture2D.width, texture2D.height);
                    if (!elements.ContainsKey(key))
                        elements[key] = new Stack<Texture2D>();
                    elements[key].Push(texture2D);
                }
            }

            public bool Empty()
            {
                return elements.Count <= 0;
            }

            public Texture2D GetTexture(Vector2Int size)
            {
                Texture2D res = null;
                var key = (size.x, size.y);
                if (elements.ContainsKey(key))
                {
                    var que = elements[key];
                    res = que.Pop();
                    if (que.Count <= 0) elements.Remove(key);
                }

                return res;
            }

            public void RefreshTexture(List<Texture2D> texs)
            {
                int n = texs.Count;
                for (int i = n - 1; i >= 0; --i)
                {
                    var tex = texs[i];
                    var key = (tex.width, tex.height);
                    if (!elements.ContainsKey(key))
                    {
                        elements[key] = new Stack<Texture2D>();
                    }

                    elements[key].Push(tex);
                }
            }

            public void DeleteTexture(List<Texture2D> texs)
            {
                foreach (var tex in texs)
                {
                    var key = (tex.width, tex.height);
                    var que = elements[key];
                    foreach (var t in que)
                    {
                        var tt = que.Pop();
                        if (t == tex)
                        {
                            if (que.Count == 0) elements.Remove(key);
                            break;
                        }
                        else que.Push(tt);
                    }
                }
            }

        }


        [StructLayout(LayoutKind.Explicit)]
        private struct ContainBlockStruct
        {
            [FieldOffset(0)] public List<ContainBlock> containBlocks;
            [FieldOffset(0)] public Texture2D texture2D;

            public ContainBlockStruct(Texture2D x)
            {
                containBlocks = null;
                texture2D = x;
            }

            public ContainBlockStruct(List<ContainBlock> x)
            {
                texture2D = null;
                containBlocks = x;
            }


        }

        private class ContainBlock
        {
            private bool isTex;

            public bool IsTex
            {
                get { return isTex; }
                set { isTex = value; }
            }

            private Vector2Int startPoint;
            private Vector2Int size; //size.x   size.y 
            private ContainBlockStruct containBlockStruct;

            public ContainBlockStruct ContainBlockStruct
            {
                get { return containBlockStruct; }
                set { containBlockStruct = value; }
            }

            public void Print()
            {
                if (isTex)
                {
                    Debug.Log(startPoint + "   " + size + "   " + containBlockStruct.texture2D);
                }
                else
                {
                    foreach (var cc in containBlockStruct.containBlocks)
                    {
                        if (cc != null) cc.Print();
                    }
                }
            }

            public ContainBlock(Vector2Int _startPoint, Vector2Int _size, bool _isTex = false,
                ContainBlockStruct _containBlockStruct = new ContainBlockStruct()) =>
                (isTex, startPoint, size, containBlockStruct) = (_isTex, _startPoint, _size, _containBlockStruct);

            public void Merge(Texture2D outTex, int empty)
            {
                if (isTex)
                {
                    for (int i = empty; i < size.x - empty; ++i)
                    {
                        for (int j = empty; j < size.y - empty; ++j)
                        {
                            outTex.SetPixel(startPoint.x + i, startPoint.y + j, containBlockStruct.texture2D.GetPixel(i, j));
                        }
                    }
                }
                else if (containBlockStruct.containBlocks != null)
                {
                    foreach (var cb in containBlockStruct.containBlocks)
                    {
                        if (cb != null) cb.Merge(outTex, empty);
                    }
                }
            }

        }
    }

}