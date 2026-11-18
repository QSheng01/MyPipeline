using UnityEngine;
using UnityEditor;
using MightyTerrainMesh;

public class MTLODSetting : MeshLODCreate
{
    public bool bEditorUIFoldout = true;
    public virtual void OnGUIDraw(int idx)
    {
        bEditorUIFoldout = EditorGUILayout.Foldout(bEditorUIFoldout, string.Format("LOD {0}", idx));
        if (!bEditorUIFoldout)
        {
            EditorGUI.indentLevel++;
            int curRate = Mathf.FloorToInt(Mathf.Pow(2, Subdivision));
            int sampleRate = EditorGUILayout.IntField("Sample(NxN)", curRate);
            if (curRate != sampleRate)
            {
                curRate = Mathf.NextPowerOfTwo(sampleRate);
                Subdivision = Mathf.FloorToInt(Mathf.Log(curRate, 2));
            }
            var error = EditorGUILayout.FloatField("Slope Angle Error", SlopeAngleError);
            SlopeAngleError = Mathf.Max(0.01f, error);
            EditorGUI.indentLevel--;
        }
    }
}
