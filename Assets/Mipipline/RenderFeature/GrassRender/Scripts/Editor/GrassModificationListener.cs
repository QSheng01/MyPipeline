
using UnityEditor;
using UnityEngine;

namespace Grass
{
	[InitializeOnLoad]
	public static class GrassModificationListener
	{
		public class GrasseModificationProcessor : UnityEditor.AssetModificationProcessor
		{
			static string[] OnWillSaveAssets(string[] paths)
			{
				foreach(var path in paths)
				{
					if(path.EndsWith(".unity"))
					{
                        SaveTerrainData();
						SaveGrassData();
						break;
					}
				}
				return paths;
			}

           
        }

		static GrassModificationListener()
		{
			EditorApplication.playModeStateChanged += PlayStateApply;
			Undo.undoRedoPerformed += OnUndoRedoPerformed;
		}

        private static void OnUndoRedoPerformed()
        {
			//Debug.Log("OnUndoRedoPerformed"+ Undo.GetCurrentGroupName());
			var grassTools = GameObject.FindObjectsOfType<GrassTool>();
			foreach (var grassTool in grassTools)
			{
				grassTool.OnUndoRedoPerformed();
			}
		}

        static void PlayStateApply(PlayModeStateChange stateChange)
		{
			if (stateChange == PlayModeStateChange.ExitingEditMode)
			{
                SaveTerrainData();
			}
		}
	

        static void SaveTerrainData()
        {
            //var terrainPainter = GameObject.FindObjectsOfType<TerrainPainter>();
            //foreach(var painter in terrainPainter)
            //{
            //    if(painter.isExitMode)
            //    {
            //        var terrainEditor = UnityEditor.Editor.CreateEditor(painter) as TerrainPainterStyle;
            //        terrainEditor.SaveTerrainData();
            //    }
            //}

        }

		static void SaveGrassData()
		{
			//Debug.Log("SaveGrassData");
			var grassTools = GameObject.FindObjectsOfType<GrassTool>();
			foreach (var grassTool in grassTools)
			{
				var grassToolEditor = UnityEditor.Editor.CreateEditor(grassTool) as GrassToolEditor;
				grassToolEditor.SaveGrassData(true);
				GameObject.DestroyImmediate(grassToolEditor);
			}
		}
	}
}