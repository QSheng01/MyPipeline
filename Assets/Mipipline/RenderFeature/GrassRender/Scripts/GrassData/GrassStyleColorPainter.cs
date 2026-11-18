#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Grass
{
   [System.Serializable]
    public class GrassStyleColorPainter : ScriptableObject
    {
        public static readonly int LayerNum = 2;
        public float _brushSize = 16f;
        public float _brushStronger = 0.5f;
        public int _selBrush = 0;
        public Texture[] _brushTex;

        public int _selLayer = 0;
        public Texture2D[] _layerTextures;
        public Texture2D[] _layerCatchTextures;
        public Color[] _layerColors;
        public bool _enablePainter;
        public GrassTool _ownerTool;
        public int _resolution;
        public void Init(GrassTool tool,int resolution)
        {
            _resolution = resolution; 
            _ownerTool = tool;
            InitStyleLayer();
        }

        private void InitStyleLayer()
        {
            if (_layerCatchTextures != null && _layerCatchTextures.Length > 0)
            {
                _layerTextures = new Texture2D[LayerNum];
                for (int i = 0; i < LayerNum; i++)
                {
                    _layerTextures[i] = new Texture2D(_resolution, _resolution, TextureFormat.ARGB32, false);
                }

                SaveCatchLayerTexture();
                return;
            } 

            _layerTextures = new Texture2D[LayerNum];
            _layerCatchTextures = new Texture2D[LayerNum];
            _layerColors = new Color[LayerNum];
            for (int i = 0; i < LayerNum; i++)
            {
                _layerTextures[i] = new Texture2D(_resolution, _resolution, TextureFormat.ARGB32, false);
                _layerCatchTextures[i] = new Texture2D(_resolution, _resolution, TextureFormat.ARGB32, false);
                _layerColors[i] = Color.green;
            }

            SaveTextureToAssets();
        }

        public void SaveCatchLayerTexture()
        {
            for (int i = 0; i < LayerNum; i++)
            {
                if (_layerCatchTextures[i]!= null && _layerTextures[i] != null)
                {
                    Graphics.CopyTexture(_layerCatchTextures[i], _layerTextures[i]);
                }
            }
        }

        public void RestoreLayerTexturesTexture()
        {
            for (int i = 0; i < LayerNum; i++)
            {
                if (_layerCatchTextures[i]!= null && _layerTextures[i] == null)
                {
                    _layerTextures[i] = new Texture2D(_resolution, _resolution, TextureFormat.ARGB32, false);
                    Graphics.CopyTexture(_layerCatchTextures[i], _layerTextures[i]);
                }
            }
        }
        
        public void SaveTextureToAssets()
        {
#if UNITY_EDITOR
            //test add
            string path = AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(path))
            {
                for (int i = 0; i < _layerCatchTextures.Length; i++)
                {
                    GrassStyleColorPainter ap = AssetDatabase.LoadAssetAtPath<GrassStyleColorPainter>(path);
                    AssetDatabase.AddObjectToAsset(_layerCatchTextures[i], ap);
                    EditorUtility.SetDirty(ap);
                    AssetDatabase.SaveAssets();
                }
            }
#endif
        }

        public void OnUndoRedoPerformed()
        {
            for (int i = 0; i < LayerNum; i++)
            {
                // 如果 _layerTextures 是失效状态，则重新修复
                if (_layerTextures[i] == null)
                {
                    _layerTextures[i] = new Texture2D(_resolution, _resolution, TextureFormat.ARGB32, false);
                    Graphics.CopyTexture(_layerCatchTextures[i], _layerTextures[i]);
                }
                Graphics.CopyTexture(_layerTextures[i], _layerCatchTextures[i]);
            }

        }
    }
}