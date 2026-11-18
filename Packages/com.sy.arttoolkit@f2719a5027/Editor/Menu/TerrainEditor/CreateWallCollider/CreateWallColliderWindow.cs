#if UNITY_EDITOR
namespace SYArtToolkit.CreateEasyWallCollider
{
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;

    public class CreateWallColliderWindow : ToolInterface
    {

        ArtToolBoxLocalData data;
        ReorderableList reorderable;

        public CreateWallColliderWindow()
        {
            data = GetScriptableObjectDataArtToolBox.UpdateData(data);
            reorderable = new ReorderableList(data.wallColliderDatas, typeof(WallColliderData), true, true, false, true);
            reorderable.drawHeaderCallback = (Rect rect) => { GUI.Label(rect, "读取编辑数据"); };
            reorderable.drawElementCallback = (Rect rect, int index, bool a, bool b) =>
            {
                float width = rect.width;
                WallColliderData wallColliderData = data.wallColliderDatas[index];
                if (data.wallColliderDatas[index] == null) return;
                GUI.Label(new Rect(rect.x, rect.y, width / 2, rect.height), string.IsNullOrEmpty(wallColliderData.name) ? "" : wallColliderData.name);
                GUI.Label(new Rect(rect.x + width / 2, rect.y, width / 4, rect.height), string.Format("节点数:{0}", wallColliderData.v2s.Length));
                if (GUI.Button(new Rect(rect.x + width / 4 * 3, rect.y, width / 4, rect.height), "创建"))
                {
                    GameObject gameObject = new GameObject();
                    gameObject.transform.position = wallColliderData.position;
                    var newEasy = gameObject.AddComponent<EasyWallCollider>();

                    Transform last = null;
                    Transform now = null;
                    for (int i = 0; i < wallColliderData.v2s.Length; i++)
                    {
                        if (last == null)
                        {
                            last = new GameObject().transform;
                            EasyWallColliderEditor.DrawIcon(last.gameObject, 0);
                            now = last;
                            now.parent = gameObject.transform;
                        }
                        else
                        {
                            last = now;
                            now = Object.Instantiate(last.gameObject, gameObject.transform, true).transform;
                        }

                        var v2 = wallColliderData.v2s[i];
                        now.position = new Vector3(v2.x, 0, v2.y);
                        now.SetSiblingIndex(i);
                        if (now.gameObject.GetComponent<NodeShowCollider>() == null)
                        {
                            now.gameObject.AddComponent<NodeShowCollider>();
                        }
                        newEasy.corners.Add(now);

                    }
                    newEasy.depth = wallColliderData.depth;
                    newEasy.invert = wallColliderData.invert;
                    newEasy.extraWidth = wallColliderData.extraWidth;
                    newEasy.heigth = wallColliderData.heigth;
                    newEasy.radius = wallColliderData.radius;
                    newEasy.physicsMaterial = wallColliderData.physicsMaterial;
                    newEasy.loop = wallColliderData.loop;
                    newEasy.isTrigger = wallColliderData.isTrigger;
                    newEasy.makeRenderers = wallColliderData.makeRenderers;
                    newEasy.copyTag = wallColliderData.copyTag;

                }
            };



        }
        Vector2 v2;

        public override void InterfaceShow(bool atThisWindow, Rect position)
        {
            if (data == null) data = GetScriptableObjectDataArtToolBox.UpdateData(data);
            if (GUILayout.Button("创建新的碰撞墙编辑器", GUILayout.Height(50)))
            {
                new GameObject("NewWallColliderEditor").AddComponent<EasyWallCollider>();
            }
            if (data.wallColliderDatas == null || data.wallColliderDatas.Count == 0) { }
            v2 = EditorGUILayout.BeginScrollView(v2);
            reorderable.DoLayoutList();

            EditorGUILayout.EndScrollView();


        }
    }











}
#endif