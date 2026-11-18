

using System.IO;
using UnityEditor;
using UnityEngine;

namespace SYArtToolkit
{
    //[CustomEditor(typeof(PhotoActorSettings))]
    public class PhotoActor : ToolInterface
    {

        static string[] PhotoTypeStr = new string[] { ".PNG", ".JPG", ".TGA" };
        static string[] SizeGroupStr = new string[] { "512", "1024", "2048" };
        static int[] SizeGroup = new int[] { 512, 1024, 2048 };

        int textureSize = 1024;
        int sizeIndex = 0;

        int photoType = 0;

        Camera viewCamera;
        GameObject targetGameObject;

        DefaultAsset defaultAsset = null;

        public PhotoActor()
        {

            defaultAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(EditorPrefs.GetString("PhotoActorSaveFolderPath"));
            viewCamera = Camera.main;
        }

        public override void InterfaceShow(bool atThisWindow, Rect positionRect)
        {
            EditorGUILayout.BeginVertical();


            viewCamera = EditorGUILayout.ObjectField("ViewCamera", viewCamera, typeof(Camera), true) as Camera;
            if (viewCamera == null)
                EditorGUILayout.HelpBox("请选择场景主相机！！！", MessageType.Warning);

            targetGameObject = EditorGUILayout.ObjectField("TargetObject", targetGameObject, typeof(GameObject), true) as GameObject;



            bool enable = targetGameObject != null;

            if (!enable)
            {
                EditorGUILayout.HelpBox("当前没有可以拍照的角色！！！", MessageType.Warning);
            }

            sizeIndex = EditorGUILayout.Popup("Size", sizeIndex, SizeGroupStr);
            textureSize = SizeGroup[sizeIndex];

            photoType = EditorGUILayout.Popup("PhotoType", photoType, PhotoTypeStr);

            var defaultAssetTemp = EditorGUILayout.ObjectField(new GUIContent("Export Folder", AssetDatabase.GetAssetPath(defaultAsset)), defaultAsset, typeof(DefaultAsset), false) as DefaultAsset;
            if (defaultAssetTemp != null && defaultAssetTemp != defaultAsset)
            {
                if (Directory.Exists(AssetDatabase.GetAssetPath(defaultAssetTemp)))
                {
                    defaultAsset = defaultAssetTemp;
                    EditorPrefs.SetString("PhotoActorSaveFolderPath", AssetDatabase.GetAssetPath(defaultAssetTemp));
                }
            }
            if (defaultAsset == null)
            {
                defaultAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets");
            }


            EditorGUI.BeginDisabledGroup(!enable || viewCamera == null);
            if (GUILayout.Button("CreateAndSave"))
            {
                //GameObject obj =  targetObjPro.objectReferenceValue as GameObject;
                CreateAndSavePhoto(AssetDatabase.GetAssetPath(defaultAssetTemp));
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
        }

        private void CreateAndSavePhoto(string assetPath)
        {
            if (assetPath == null)
                return;

            GameObject tempObj = new GameObject("tempCamera");
            tempObj.transform.position = viewCamera.transform.position;
            tempObj.transform.rotation = viewCamera.transform.rotation;
            Camera tempCamera = tempObj.AddComponent<Camera>();
            tempCamera.orthographic = viewCamera.orthographic;
            tempCamera.fieldOfView = viewCamera.fieldOfView;
            tempCamera.farClipPlane = viewCamera.farClipPlane;
            tempCamera.nearClipPlane = viewCamera.nearClipPlane;
            tempCamera.clearFlags = CameraClearFlags.SolidColor;
            tempCamera.orthographicSize = viewCamera.orthographicSize;
            tempCamera.backgroundColor = new Color(1, 1, 1, 0);
            tempCamera.depthTextureMode = DepthTextureMode.Depth;

            if (!Directory.Exists(Application.dataPath + "/" + assetPath.Replace("Assets/", "")))
            {
                Directory.CreateDirectory(Application.dataPath + "/" + assetPath.Replace("Assets/", ""));
            }

            RenderTexture rt = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGB32);

            tempCamera.targetTexture = rt;
            tempCamera.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(textureSize, textureSize, TextureFormat.ARGB32, false);
            tex.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
            byte[] bytes = null;
            string path = "";
            if (PhotoTypeStr[photoType].Equals(".PNG"))
            {
                bytes = tex.EncodeToPNG();

            }
            else if (PhotoTypeStr[photoType].Equals(".JPG"))
            {
                bytes = tex.EncodeToJPG();
            }
            else if (PhotoTypeStr[photoType].Equals(".TGA"))
            {
                bytes = tex.EncodeToTGA();
            }
            if (bytes == null)
            {
                Debug.Log("没有像素数据，拍照失败");
                return;
            }
            path = CheckPhotoPath(string.Format("{0}/{1} photo {2}", assetPath, targetGameObject.name, textureSize));
            File.WriteAllBytes(path, bytes);
            Debug.Log(path + "  " + "拍照成功");

            Submit.ArtToolkitData.Instance.AddTexture(1);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            RenderTexture.active = null;
            tempCamera.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tempCamera.gameObject);
        }

        private string CheckPhotoPath(string path)
        {
            int count = 0;

            while (true)
            {
                var tempPath = string.Format("{0} {1}{2}", path, count, PhotoTypeStr[photoType]);
                if (File.Exists(tempPath))
                {
                    count++;
                }
                else
                {
                    path = tempPath;
                    break;
                }
            }

            return path;

        }


        //      #region 导出路径设置
        ////      float  browseButtonWidth = 25;
        ////float  selectableLabelMinWidth = 120;
        ////float  fieldOffset = 18;
        ////float  labelWidth = 175;
        ////List<string>  savePaths = new List<string>();

        //private string ConvertToAssetRelativePath(string fullPathInAssets, bool requireSubdirectory = true)
        //{
        //	if (!Path.IsPathRooted(fullPathInAssets))
        //	{
        //		fullPathInAssets = Path.GetFullPath(fullPathInAssets);
        //	}
        //	var relativePath = GetRelativePath(Application.dataPath, fullPathInAssets);
        //	if (requireSubdirectory && relativePath.StartsWith(".."))
        //	{
        //		if (relativePath.Length == 2 || relativePath[2] == '/')
        //		{
        //			// The relative path has us pop out to another directory,
        //			// so return an empty string as requested.
        //			return "";
        //		}
        //	}
        //	return relativePath;
        //}
        //private string GetRelativePath(string fromDir, string toDir, char separator = '/')
        //{
        //	// https://stackoverflow.com/questions/275689/how-to-get-relative-path-from-absolute-path
        //	// Except... the MakeRelativeUri that ships with Unity is buggy.
        //	// e.g. https://bugzilla.xamarin.com/show bug.cgi?id=5921
        //	// among other bugs. So we roll our own.

        //	// Normalize the paths, assuming they're absolute paths (if they
        //	// aren't, they get normalized as relative paths)
        //	fromDir = NormalizePath(fromDir, isRelative: false);
        //	toDir = NormalizePath(toDir, isRelative: false);

        //	// Break them into path components.
        //	var fromDirs = fromDir.Split('/');
        //	var toDirs = toDir.Split('/');

        //	// Find the least common ancestor
        //	int lca = -1;
        //	for (int i = 0, n = System.Math.Min(fromDirs.Length, toDirs.Length); i < n; ++i)
        //	{
        //		if (fromDirs[i] != toDirs[i]) { break; }
        //		lca = i;
        //	}

        //	// Step up from the fromDir to the lca, then down from lca to the toDir.
        //	// If from = /a/b/c/d
        //	// and to  = /a/b/e/f/g
        //	// Then we need to go up 2 and down 3.
        //	var nStepsUp = (fromDirs.Length - 1) - lca;
        //	var nStepsDown = (toDirs.Length - 1) - lca;
        //	if (nStepsUp + nStepsDown == 0)
        //	{
        //		return ".";
        //	}

        //	var relDirs = new string[nStepsUp + nStepsDown];
        //	for (int i = 0; i < nStepsUp; ++i)
        //	{
        //		relDirs[i] = "..";
        //	}
        //	for (int i = 0; i < nStepsDown; ++i)
        //	{
        //		relDirs[nStepsUp + i] = toDirs[lca + 1 + i];
        //	}

        //	return string.Join("" + separator, relDirs);
        //}


        //private string NormalizePath(string path, bool isRelative, char separator = '/')
        //{
        //	if (path == null)
        //	{
        //		return null;
        //	}

        //	// Use slashes to simplify the code (we're going to clobber them all anyway).
        //	path = path.Replace('\\', '/');

        //	// If we're supposed to be an absolute path, but we're actually a
        //	// relative path, ignore the 'isRelative' flag.
        //	if (!isRelative && !Path.IsPathRooted(path))
        //	{
        //		isRelative = true;
        //	}

        //	// Build up a list of directory items.
        //	var dirs = path.Split('/');

        //	// Modify dirs in-place, reading from readIndex and remembering
        //	// what index we've written to.
        //	int lastWriteIndex = -1;
        //	for (int readIndex = 0, n = dirs.Length; readIndex < n; ++readIndex)
        //	{
        //		var dir = dirs[readIndex];

        //		// Skip duplicate path separators.
        //		if (string.IsNullOrEmpty(dir))
        //		{
        //			// Skip if it's not a leading path separator.
        //			if (lastWriteIndex >= 0)
        //			{
        //				continue;
        //			}

        //			// Also skip if it's leading and we have a relative path.
        //			if (isRelative)
        //			{
        //				continue;
        //			}
        //		}

        //		// Skip '.'
        //		if (dir == ".")
        //		{
        //			continue;
        //		}

        //		// Erase the previous directory we read on '..'.
        //		// Exception: we can start with '..'
        //		// Exception: we can have multiple '..' in a row.
        //		//
        //		// Note: this ignores the actual file system and the funny
        //		// results you see when there are symlinks.
        //		if (dir == "..")
        //		{
        //			if (lastWriteIndex == -1)
        //			{
        //				// Leading '..' => handle like a normal directory.
        //			}
        //			else if (dirs[lastWriteIndex] == "..")
        //			{
        //				// Multiple ".." => handle like a normal directory.
        //			}
        //			else
        //			{
        //				// Usual case: delete the previous directory.
        //				lastWriteIndex--;
        //				continue;
        //			}
        //		}

        //		// Copy anything else to the next index.
        //		++lastWriteIndex;
        //		dirs[lastWriteIndex] = dirs[readIndex];
        //	}

        //	if (lastWriteIndex == -1 || (lastWriteIndex == 0 && string.IsNullOrEmpty(dirs[lastWriteIndex])))
        //	{
        //		// If we didn't keep anything, we have the empty path.
        //		// For an absolute path that's / ; for a relative path it's .
        //		if (isRelative)
        //		{
        //			return ".";
        //		}
        //		else
        //		{
        //			return "" + separator;
        //		}
        //	}
        //	else
        //	{
        //		// Otherwise print out the path with the proper separator.
        //		return string.Join("" + separator, dirs, 0, lastWriteIndex + 1);
        //	}
        //}
        //#endregion
    }


}

