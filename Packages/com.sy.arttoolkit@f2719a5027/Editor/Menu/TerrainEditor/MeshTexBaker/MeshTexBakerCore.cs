
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

using Object = UnityEngine.Object;


namespace SYArtToolkit
{

    [Serializable]
    public class MapBakePreSet
    {
        public Texture2D texture;
        public int size;
        public Rect uv;
    }

    /// <summary>

    /// ???????????
    /// </summary>
    public class MapBakePreSetComparer : IComparer<MapBakePreSet>
    {
        public int Compare(MapBakePreSet x, MapBakePreSet y)
        {
            if (x.size > y.size)
            {
                return 1;
            }
            else if (x.size < y.size)
            {
                return -1;
            }
            return 0;
        }
    }

    public class AtlasRectComparer : IComparer<Vector2>
    {
        public int Compare(Vector2 a, Vector2 b)
        {
            if (a.x * a.y > b.x * b.y)
            {
                return 1;
            }
            else if (a.x * a.y < b.x * b.y)
            {
                return -1;
            }
            return 0;
        }
    }

    public class MeshTexBakerCore
    {
        private readonly string shaderMainTexProperty = "_MainTex";
        List<MeshFilter> meshFilters = new List<MeshFilter>();
        List<Renderer> renderers = new List<Renderer>();
        GameObject[] m_SelectSceneObjs;
        public int blockSize;
        private int m_AtlasSize;
        private RenderTexture _blitRT;
        private Rect _rtRect = Rect.zero;
        private List<MapBakePreSet> m_MapBakeData;
        private Dictionary<Texture2D, MapBakePreSet> MapBakePreSetDic;
        private string exportAssetPathMesh;
        private string exportAssetPathMaterial;
        private string exportAssetPathTexture;

        public void CollectMaps(GameObject[] selectObjs, ref List<MapBakePreSet> mapBakeData)
        {
            m_MapBakeData = mapBakeData;
            m_SelectSceneObjs = selectObjs;
            List<GameObject> tempSceneObjs = new List<GameObject>();
            List<Renderer> tempRenderers = new List<Renderer>();
            foreach (var selectedObj in selectObjs)
            {
                GetAllChild(selectedObj, ref tempSceneObjs);
            }

            foreach (var obj in tempSceneObjs)
            {
                tempRenderers.AddRange(obj.GetComponentsInChildren<Renderer>());
            }
            GetAllMainTextures(tempRenderers.ToArray());
        }

        public void GetAllMainTextures(Renderer[] renderers)
        {
            int MainMap = Shader.PropertyToID(shaderMainTexProperty);
            var materials = new HashSet<Material>(renderers.SelectMany(r => r.sharedMaterials));

            var textures = new HashSet<Texture2D>(materials.Select(m =>
            {
                if (m)
                    return m.mainTexture as Texture2D;
                return null;
            }).Where(t => t != null)).ToList();
            MapBakePreSetDic = new Dictionary<Texture2D, MapBakePreSet>();
            foreach (var tex in textures)
            {
                MapBakePreSet mapBakePreSet = new MapBakePreSet();
                mapBakePreSet.texture = tex;
                mapBakePreSet.size = 256;
                mapBakePreSet.uv = Rect.zero;
                m_MapBakeData.Add(mapBakePreSet);
                MapBakePreSetDic[tex] = mapBakePreSet;
            }
        }
        private int textureIndex;
        private int materialIndex;
        private int meshIndex;
        private int objectIndex;

        public void Execute(int atlasSize, int padding, string pathMesh, string pathMaterial, string pathTexture)
        {
            textureIndex = 0;
            materialIndex = 0;
            meshIndex = 0;
            objectIndex = 0;

            exportAssetPathMesh = pathMesh;
            exportAssetPathMaterial = pathMaterial;
            exportAssetPathTexture = pathTexture;
            m_AtlasSize = atlasSize;

            m_MapBakeData.Sort(new MapBakePreSetComparer());

            List<GameObject> allCopySceneObjs = new List<GameObject>();

            foreach (var sceneObj in m_SelectSceneObjs)
            {
                var copyObj = Object.Instantiate(sceneObj, sceneObj.transform.parent);
                copyObj.name = MeshTexBaker.GetReName(sceneObj.name, ++objectIndex);
                GetAllChild(copyObj, sceneObj, ref allCopySceneObjs);
            }
            foreach (var obj in allCopySceneObjs)
            {
                //meshFilters.AddRange(obj.GetComponentsInChildren<MeshFilter>());
                //renderers.AddRange(obj.GetComponentsInChildren<Renderer>());
                meshFilters.Add(obj.GetComponent<MeshFilter>());
                renderers.Add(obj.GetComponent<Renderer>());

            }

            Dictionary<Texture2D, List<MeshFilter>> meshFilerByT2D = new Dictionary<Texture2D, List<MeshFilter>>();
            Dictionary<Texture2D, List<Renderer>> rendererByT2D = new Dictionary<Texture2D, List<Renderer>>();

            int MainMap = Shader.PropertyToID(shaderMainTexProperty);
            for (int i = 0; i < renderers.Count; i++)
            {
                Debug.Log(renderers[i]);
                if (renderers[i] == null) continue;
                var sharedMaterials = renderers[i].sharedMaterials;
                for (int j = 0; j < sharedMaterials.Length; j++)
                {
                    var t2d = sharedMaterials[j].mainTexture as Texture2D;
                    if (t2d == null)
                        continue;
                    if (!meshFilerByT2D.ContainsKey(t2d))
                    {
                        meshFilerByT2D.Add(t2d, new List<MeshFilter>());
                        rendererByT2D.Add(t2d, new List<Renderer>());
                    }
                    meshFilerByT2D[t2d].Add(meshFilters[i]);
                    rendererByT2D[t2d].Add(renderers[i]);
                }
            }

            //??????????????????????????????
            List<Texture2D> combineMapBatch = new List<Texture2D>();
            List<Renderer> renderersBatch = new List<Renderer>();
            List<MeshFilter> meshFilterBatch = new List<MeshFilter>();

            int atlasPixels = m_AtlasSize * m_AtlasSize;
            //????????????????????
            List<Vector2> curBatchAltasRect = new List<Vector2>();
            curBatchAltasRect.Add(new Vector2(m_AtlasSize, m_AtlasSize));
            for (int i = 0; i < m_MapBakeData.Count; i++)
            {
                int subMapWidth = m_MapBakeData[i].size;
                int subMapHeight = m_MapBakeData[i].size;
                bool isCanPack = isCanPackCurAtlas(curBatchAltasRect, subMapWidth, subMapHeight);
                if (!isCanPack)
                {
                    Texture2D textureAtlas = null;
                    Rect[] uvs = PackThumbnailAtlas(combineMapBatch.ToArray(), out textureAtlas, padding, atlasSize);
                    textureAtlas = SaveAtlasPicture(textureAtlas, combineMapBatch[0].name);
                    GenerateMesh(meshFilterBatch, renderersBatch, textureAtlas);
                    curBatchAltasRect.Clear();
                    curBatchAltasRect.Add(new Vector2(m_AtlasSize, m_AtlasSize));
                    combineMapBatch.Clear();
                    meshFilterBatch.Clear();
                    renderersBatch.Clear();
                }

                CuttingAtlas(ref curBatchAltasRect, subMapWidth, subMapHeight);
                var subMap = m_MapBakeData[i].texture;
                var meshFilters = meshFilerByT2D[subMap];
                var renders = rendererByT2D[subMap];
                combineMapBatch.Add(subMap);
                meshFilterBatch.AddRange(meshFilters);
                renderersBatch.AddRange(renders);
            }
            //??????????????????????
            if (combineMapBatch.Count != 0)
            {
                Texture2D textureAtlas = null;
                Rect[] uvs = PackThumbnailAtlas(combineMapBatch.ToArray(), out textureAtlas, padding, atlasSize);
                textureAtlas = SaveAtlasPicture(textureAtlas, combineMapBatch[0].name);
                GenerateMesh(meshFilterBatch, renderersBatch, textureAtlas);
                curBatchAltasRect.Clear();
                curBatchAltasRect.Add(new Vector2(m_AtlasSize, m_AtlasSize));
                combineMapBatch.Clear();
                meshFilterBatch.Clear();
                renderersBatch.Clear();
            }

            AssetDatabase.Refresh();
        }

        public bool isCanPackCurAtlas(List<Vector2> curAltasRect, int width, int height)
        {
            bool isCanPack = false;
            for (int i = 0; i < curAltasRect.Count; i++)
            {
                Vector2 curRect = curAltasRect[i]; //x??????y????
                //??????,?????????????????
                if (width <= curRect.y && height <= curRect.y)
                {
                    isCanPack = true;
                    break;
                }
            }
            return isCanPack;
        }

        public void CuttingAtlas(ref List<Vector2> curAltasRect, int width, int height)
        {
            //Debug.Log("?????:" + width + "," + height);
            int index = -1;
            for (int i = 0; i < curAltasRect.Count; i++)
            {
                Vector2 curRect = curAltasRect[i]; //x??????y????
                //??????,?????????????????????
                if (width <= curRect.y && height <= curRect.y)
                {
                    index = i;
                    //int longSide = Math.Max(width, height);
                    //int shortSide = Math.Min(width, height);
                    int size = width;
                    if (size != curRect.y)
                    {
                        Vector2 longRect = new Vector2(0, 0);
                        longRect.x = Math.Max(curRect.x, curRect.y - size);
                        longRect.y = Math.Min(curRect.x, curRect.y - size);
                        //Debug.Log("longRect:" + longRect.x + "," + longRect.y);

                        Vector2 shortRect = new Vector2(0, 0);
                        shortRect.x = Math.Max(curRect.x - size, size);
                        shortRect.y = Math.Min(curRect.x - size, size);
                        //Debug.Log("shortRect:" + shortRect.x + "," + shortRect.y);

                        curAltasRect.Add(longRect);
                        curAltasRect.Add(shortRect);
                    }
                    else
                    {
                        Vector2 simpleRect = new Vector2(0f, 0f);
                        simpleRect.x = Math.Max(curRect.x - size, size);
                        simpleRect.y = Math.Min(curRect.x - size, size);
                        curAltasRect.Add(simpleRect);
                        //Debug.Log("simpleRect:" + simpleRect.x + "," + simpleRect.y);
                    }
                    break;
                }
            }
            if (index != -1)
            {
                curAltasRect.RemoveAt(index);
            }
            //??????????????????????????
            curAltasRect.Sort(new AtlasRectComparer());
            //foreach (var rect in curAltasRect)
            //{
            //    Debug.Log("rect:" + rect.x + "," + rect.y);
            //}
        }

        public void GenerateMesh(List<MeshFilter> meshFilters, List<Renderer> renderers, Texture2D textureAtlas)
        {

            Mesh offsetUVMesh = null;
            Dictionary<Material, List<Renderer>> renderDict = new Dictionary<Material, List<Renderer>>();
            Dictionary<Material, List<MeshFilter>> filterDict = new Dictionary<Material, List<MeshFilter>>();

            for (int i = 0; i < renderers.Count; i++)
            {
                Renderer renderer = renderers[i];
                MeshFilter meshFilter = meshFilters[i];
                Material[] materials = renderer.sharedMaterials;
                foreach (Material material in materials)
                {
                    if (renderDict.ContainsKey(material))
                    {
                        renderDict[material].Add(renderer);
                        filterDict[material].Add(meshFilter);
                    }
                    else
                    {
                        bool add = true;
                        foreach (var mat in renderDict.Keys)
                        {
                            if (EqualMaterialElseMainTexture(mat, material))
                            {
                                add = false;
                                renderDict[mat].Add(renderer);
                                filterDict[mat].Add(meshFilter);
                                break;
                            }
                        }
                        if (add)
                        {
                            List<Renderer> rendererLst = new List<Renderer>();
                            List<MeshFilter> meshFilterLst = new List<MeshFilter>();
                            rendererLst.Add(renderer);
                            meshFilterLst.Add(meshFilter);
                            renderDict[material] = rendererLst;
                            filterDict[material] = meshFilterLst;
                        }
                    }
                }
            }

            List<MeshFilter> tempMeshFilter = new List<MeshFilter>();
            List<Renderer> tempRenderer = new List<Renderer>();
            Dictionary<string, int> matName = new Dictionary<string, int>();

            //MergeMeshWindow.Instance.TransformMaterials = new List<Material>();
            foreach (var material in renderDict.Keys)
            {
                tempMeshFilter = filterDict[material];
                tempRenderer = renderDict[material];

                string shaderName = material.shader.name;
                Material cpoyMaterial = new Material(Shader.Find(shaderName));
                cpoyMaterial.name = MeshTexBaker.GetReName(tempRenderer[0].sharedMaterial.name, ++materialIndex);
                //string.Format("CM_{0}", tempRenderer[0].sharedMaterial.name);
                string materialPath = "";
                if (matName.ContainsKey(cpoyMaterial.name))
                {
                    matName[cpoyMaterial.name]++;
                    int count = matName[cpoyMaterial.name];
                    materialPath = string.Format("{0}/{1}_{2}.mat", exportAssetPathMaterial, cpoyMaterial.name, count);
                }
                else
                {
                    matName.Add(cpoyMaterial.name, 0);
                    materialPath = string.Format("{0}/{1}.mat", exportAssetPathMaterial, cpoyMaterial.name);
                }
                //MergeMeshWindow.Instance.TransformMaterials.Add(cpoyMaterial);
                int c = 0;
                //System.IO.Exists(path)==false Debug.Log("1111");
                while (System.IO.File.Exists(materialPath))
                {
                    materialPath = string.Format("{0}/{1} {2}.mat", exportAssetPathMaterial, cpoyMaterial.name, ++c);
                    if (c > 1000) break;
                }
                AssetDatabase.CreateAsset(cpoyMaterial, materialPath);
                cpoyMaterial.SetTexture("_MainTex", textureAtlas);

                //

                //var hasMeshFilter=new HashSet<MeshFilte>()

                Dictionary<Mesh, Mesh> hash = new Dictionary<Mesh, Mesh>();

                for (int i = 0; i < tempMeshFilter.Count; i++)
                {
                    var mf = tempMeshFilter[i];
                    if (mf == null)
                        continue;
                    var sharedMesh = mf.sharedMesh;

                    if (!sharedMesh)
                        continue;

                    if (hash.ContainsKey(sharedMesh))
                    {
                        tempRenderer[i].sharedMaterial = cpoyMaterial;
                        tempMeshFilter[i].sharedMesh = hash[sharedMesh];
                        continue;
                    }

                    bool flag = false;
                    if (!sharedMesh.isReadable)
                    {
                        string assetPath3 = AssetDatabase.GetAssetPath(sharedMesh);
                        Debug.Log(assetPath3 + "_可读性修改过");
                        if (!string.IsNullOrEmpty(assetPath3))
                        {
                            var importer = AssetImporter.GetAtPath(assetPath3) as ModelImporter;
                            if (importer)
                            {
                                importer.isReadable = true;
                                importer.SaveAndReimport();
                                flag = true;
                            }
                        }
                    }
                    offsetUVMesh = Object.Instantiate(sharedMesh);
                    var mr = mf.GetComponent<MeshRenderer>();
                    var shareMaterials = mr.sharedMaterials;
                    var offsetUV = offsetUVMesh.uv;
                    var colors = offsetUVMesh.colors;
                    if (colors == null || colors.Length == 0)
                        colors = new Color[offsetUV.Length];

                    var subMeshCount = Mathf.Min(offsetUVMesh.subMeshCount, shareMaterials.Length);
                    for (int j = 0; j < subMeshCount; j++)
                    {
                        var sharedMaterial = shareMaterials[Mathf.Min(j, shareMaterials.Length)];
                        var mainTexture = Object.Instantiate(Texture2D.grayTexture);

                        if (sharedMaterial)
                        {
                            var texture = sharedMaterial.mainTexture as Texture2D;
                            if (texture)
                                mainTexture = texture;
                        }

                        var uvOffset = MapBakePreSetDic[mainTexture].uv;
                        for (int k = 0; k < offsetUV.Length; k++)
                        {
                            var uvCoord = offsetUV[k];
                            uvCoord.x = Mathf.Lerp(uvOffset.xMin, uvOffset.xMax, uvCoord.x);
                            uvCoord.y = Mathf.Lerp(uvOffset.yMin, uvOffset.yMax, uvCoord.y);
                            offsetUV[k] = uvCoord;
                        }
                    }
                    offsetUVMesh.uv = offsetUV;
                    offsetUVMesh.colors = colors;

                    offsetUVMesh.name = MeshTexBaker.GetReName(tempMeshFilter[i].sharedMesh.name, ++meshIndex);
                    //string.Format("CM_{0}", tempMeshFilter[i].sharedMesh.name);

                    if (flag)
                    {

                        string assetPath4 = AssetDatabase.GetAssetPath(sharedMesh);
                        if (!string.IsNullOrEmpty(assetPath4))
                        {
                            var importer = AssetImporter.GetAtPath(assetPath4) as ModelImporter;
                            if (importer)
                            {
                                importer.isReadable = false;
                                importer.SaveAndReimport();
                            }
                        }
                    }

                    hash.Add(sharedMesh, offsetUVMesh);
                    tempRenderer[i].sharedMaterial = cpoyMaterial;
                    tempMeshFilter[i].sharedMesh = offsetUVMesh;
                    string meshPath = string.Format("{0}/{1}.fbx", exportAssetPathMesh, tempMeshFilter[i].sharedMesh.name);
                    int count = 0;
                    while (File.Exists(meshPath))
                    {
                        meshPath = string.Format("{0}/{1} {2}.fbx", exportAssetPathMesh, tempMeshFilter[i].sharedMesh.name, ++count);
                        if (count > 1000) break;
                    }

                    //for (int x = 0; x < 100; ++x)
                    //{
                    //    if (AssetDatabase.LoadAssetAtPath<Mesh>(meshPath) == null) break;
                    //    meshPath = string.Format("{0}/{1}.fbx", exportAssetPath, tempMeshFilter[i].sharedMesh.name + x);
                    //}
                    //AssetDatabase.CreateAsset(tempMeshFilter[i].sharedMesh, meshPath);
                    GameObject newGameObject = new GameObject(Path.GetFileName(meshPath));
                    var mfNew = newGameObject.AddComponent<MeshFilter>();
                    mfNew.sharedMesh = tempMeshFilter[i].sharedMesh;
                    newGameObject.AddComponent<MeshRenderer>();
                    var mr1 = newGameObject.GetComponent<MeshRenderer>();
                    mr1.sharedMaterial = null;

                    AsciiFBXExporter.FBXExporter.ExportGameObjToFBX(newGameObject, meshPath);

                    Object.DestroyImmediate(newGameObject);

                    tempMeshFilter[i].sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    tempRenderer[i].sharedMaterial = cpoyMaterial;


                    var assetPath = AssetDatabase.GetAssetPath(sharedMesh);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        var importer1 = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                        if (importer1)
                        {
                            Preset x = new Preset(importer1);
                            var importer2 = AssetImporter.GetAtPath(meshPath) as ModelImporter;
                            if (importer2)
                            {

                                x.ApplyTo(importer2);


                                importer2.SaveAndReimport();
                            }

                        }
                    }

                    var importer3 = AssetImporter.GetAtPath(meshPath) as ModelImporter;

                    importer3.importAnimation = false;
                    importer3.animationType = ModelImporterAnimationType.None;
                    importer3.materialImportMode = ModelImporterMaterialImportMode.None;
                    importer3.generateSecondaryUV = true;
                    importer3.SaveAndReimport();
                }


            }
            //MergeMeshWindow.Instance.ApplyTransform();
        }

        /// <summary>
        /// ???????????
        /// </summary>
        /// <param name="textures">??</param>
        /// <param name="atlas">??????</param>
        /// <param name="padding">???</param>
        /// <param name="size">?????????????</param>
        /// <param name="maxSize">????????</param>
        /// <returns>????UVs</returns>
        public Rect[] PackThumbnailAtlas(Texture2D[] textures, out Texture2D atlas, int padding = 2, int maxSize = 2048)
        {
            atlas = new Texture2D(maxSize, maxSize, TextureFormat.RGBA32, false, false);
            Texture2D[] ResizeTextures = new Texture2D[textures.Length];
            for (var i = 0; i < textures.Length; i++)
            {
                MapBakePreSet mapBakePreSet = MapBakePreSetDic[textures[i]];
                var texture = textures[i];
                int size = mapBakePreSet.size;
                _blitRT = RenderTexture.GetTemporary(size - padding, size - padding, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                _rtRect.Set(0, 0, _blitRT.width, _blitRT.height);
                RenderTexture.active = _blitRT;
                Graphics.Blit(texture, _blitRT);
                ResizeTextures[i] = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
                //???????
                ResizeTextures[i].SetPixels(new Color[size * size]);
                ResizeTextures[i].ReadPixels(_rtRect, 1, 1);
                ResizeTextures[i].Apply();
            }
            RenderTexture.ReleaseTemporary(_blitRT);
            _rtRect = Rect.zero;
            var uvs = atlas.PackTextures(ResizeTextures, 0, maxSize);
            for (int i = 0; i < uvs.Length; i++)
            {
                var t2d = textures[i];
                MapBakePreSetDic[t2d].uv = uvs[i];
                //????????
                int subX = (int)(uvs[i].x * atlas.width * 1.0f);
                int subY = (int)(uvs[i].y * atlas.height * 1.0f);
                int subWidth = (int)(uvs[i].width * atlas.width * 1.0f);
                int subHeight = (int)(uvs[i].height * atlas.height * 1.0f);

                //??
                for (int x = subX; x < subX + subWidth; x++)
                {
                    Color color = atlas.GetPixel(x, subY + 1);
                    atlas.SetPixel(x, subY, color);
                }
                //??
                for (int x = subX; x < subX + subWidth; x++)
                {
                    Color color = atlas.GetPixel(x, subY + subHeight - 2);
                    atlas.SetPixel(x, subY + subHeight - 1, color);
                }
                //??
                for (int y = subY; y < subY + subHeight; y++)
                {
                    Color color = atlas.GetPixel(subX + 1, y);
                    atlas.SetPixel(subX, y, color);
                }
                //??
                for (int y = subY; y < subY + subHeight; y++)
                {
                    Color color = atlas.GetPixel(subX + subWidth - 2, y);
                    atlas.SetPixel(subX + subWidth - 1, y, color);
                }
            }
            atlas.Apply();
            RenderTexture.ReleaseTemporary(_blitRT);
            _rtRect = Rect.zero;

            return uvs;
        }

        #region ExportResouces
        public Texture2D SaveAtlasPicture(Texture2D texture, string name)
        {
            string nowName = MeshTexBaker.GetReName(name, ++textureIndex);
            string fileName = string.Format("{0}.png", nowName);
            Debug.Log("ReportAtlas:" + fileName);
            var bytes = texture.EncodeToPNG();
            string path = Path.Combine(exportAssetPathTexture, fileName);
            int count = 0;
            while (File.Exists(path))
            {
                fileName = string.Format("{0} {1}.png", nowName, ++count);
                path = Path.Combine(exportAssetPathTexture, fileName);
                if (count > 1000) break;
            }


            var file = new FileStream(path, FileMode.Create);
            var binary = new BinaryWriter(file);
            binary.Flush();
            binary.Write(bytes);
            file.Close();
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        #endregion

        public void GetAllChild(GameObject go, ref List<GameObject> objects)
        {
            if (go.GetComponent<Renderer>() != null)
            {
                objects.Add(go);

            }

            for (int i = 0; i < go.transform.childCount; i++)
            {
                GetAllChild(go.transform.GetChild(i).gameObject, ref objects);
            }
        }

        public void GetAllChild(GameObject go, GameObject origin, ref List<GameObject> objects)
        {
            if (go.GetComponent<MeshRenderer>() != null)
            {
                objects.Add(go);
                var mr = go.GetComponent<MeshRenderer>();

                mr.lightmapIndex =
                    origin.GetComponent<MeshRenderer>().lightmapIndex;
                mr.lightmapScaleOffset =
                    origin.GetComponent<MeshRenderer>().lightmapScaleOffset;
            }

            for (int i = 0; i < go.transform.childCount; i++)
            {
                GetAllChild(go.transform.GetChild(i).gameObject, origin.transform.GetChild(i).gameObject, ref objects);
            }
        }

        public void ClearCache()
        {
            meshFilters.Clear();
            renderers.Clear();
            MapBakePreSetDic.Clear();
            m_SelectSceneObjs = null;
        }


        private bool EqualMaterialElseMainTexture(Material a, Material b)
        {
            bool res = true;
            if (a == b) return true;
            if (a.shader != b.shader) return false;
            int allIndex = ShaderUtil.GetPropertyCount(a.shader);
            for (int i = 0; i < allIndex; ++i)
            {
                ShaderUtil.ShaderPropertyType type = ShaderUtil.GetPropertyType(a.shader, i);
                string propertyName = ShaderUtil.GetPropertyName(a.shader, i);
                switch (type)
                {
                    case ShaderUtil.ShaderPropertyType.Color:
                        if (a.GetColor(propertyName) != b.GetColor(propertyName)) res = false;
                        break;
                    case ShaderUtil.ShaderPropertyType.Float:
                    case ShaderUtil.ShaderPropertyType.Range:
                        if (a.GetFloat(propertyName) != b.GetFloat(propertyName)) res = false;
                        break;
                    case ShaderUtil.ShaderPropertyType.Vector:
                        if (a.GetVector(propertyName) != b.GetVector(propertyName)) res = false;
                        break;
                }
            }
            return res;
        }


        //[MenuItem("Assets/美术工具箱/一键处理Model/包含uv")]
        //private static void CheckModel1()
        //{
        //    CheckModel3(true);
        //}
        //[MenuItem("Assets/美术工具箱/一键处理Model/不包含uv")]
        //private static void CheckModel2()
        //{
        //    CheckModel3(false);
        //}


        //private static void CheckModel3(bool uv) {
        //    foreach(var o in Selection.objects)
        //    {


        //        var path = AssetDatabase.GetAssetPath(o);
        //        if(string.IsNullOrEmpty(path) )continue;
        //        var importer3 = AssetImporter.GetAtPath(path) as ModelImporter;
        //        if (importer3 != null)
        //        {
        //            importer3.importAnimation = false;
        //            importer3.animationType = ModelImporterAnimationType.None;
        //            importer3.materialImportMode = ModelImporterMaterialImportMode.None;
        //            importer3.generateSecondaryUV = uv;
        //            importer3.SaveAndReimport();
        //        }
        //    }

        //}

    }
}
