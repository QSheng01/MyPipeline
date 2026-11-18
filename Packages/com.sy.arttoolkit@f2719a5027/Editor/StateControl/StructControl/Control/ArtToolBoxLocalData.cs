using System.Collections.Generic;
using UnityEngine;

namespace SYArtToolkit
{

    public class ArtToolBoxLocalData : ScriptableObject
    {
        public List<string> scenePathData;
        public List<string> folderPathData;
        public List<GameObject> gameObjects;
        public List<string> texts;
        public List<string> names;
        public List<string> selectButton;
        public List<CreateEasyWallCollider.WallColliderData> wallColliderDatas;

        public ArtToolBoxLocalData()
        {
            scenePathData = new List<string>();
            folderPathData = new List<string>();
            gameObjects = new List<GameObject>();
            texts = new List<string>();
            names = new List<string>();
            selectButton = new List<string>();
            gameObjects.Add(null);
            texts.Add(null);
            names.Add(null);
            selectButton.Add("1");
            wallColliderDatas = new List<CreateEasyWallCollider.WallColliderData>();
        }

    }


}