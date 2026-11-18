#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

///  
///  目标 : 创建的时候会创建一个大父集
///  删除后不会删除碰撞体
///  如何保存数据->保存节点的xz轴->保存到一个Script中，可以根据这些进行还原。  //最后在做
///  如何平衡编辑界面和实际碰撞体之间的关系   Debug 按钮
///  
///  
///  第一步  点击初始化->在一个大父级上有碰撞体和点两个节点，脚本挂载到节点上。碰撞体生成在碰撞体节点上
///  第二步  数据存储
///  第三步  创建放在窗口上，并且可以保存在美术工具箱中

// by Pepijn Willekens
// https://twitter.com/PepijnWillekens
// pepijnwillekens@gmail.com
//Editor Window Editor Window 
namespace SYArtToolkit.CreateEasyWallCollider
{


    [ExecuteAlways]
    public class EasyWallCollider : MonoBehaviour
    {


        [HideInInspector]
        public Transform colliderContainer;

        [HideInInspector]
        public List<Transform> corners = new List<Transform>();

        [Header("Settings")]
        public float radius = 0.5f;
        //
        public float heigth = 2;
        public float depth = 0;
        public float extraWidth;
        public bool invert;
        public bool loop;
        public bool makeRenderers;
        public bool isTrigger;
        public PhysicMaterial physicsMaterial;
        public bool copyTag = true;

        public Color lineColor = Color.green;
        public Color buttonColor = Color.yellow;
        public bool alwaysShowLine = true;

        [HideInInspector]
        public bool DEBUG = true;

        private List<Vector2> cachedCornerList = new List<Vector2>();
        private List<Vector2> cachedCornerListToOutset = new List<Vector2>();

        [HideInInspector]
        public int lastHash;

        private Mesh _cubeMesh;
        private Mesh _cylinderMesh;
        private Material _defaultMaterial;


        private Mesh CubeMesh
        {
            get
            {
                if (_cubeMesh == null)
                {
                    FixPrimitiveRefs();
                }

                return _cubeMesh;
            }
        }

        private Mesh CylinderMesh
        {
            get
            {
                if (_cylinderMesh == null)
                {
                    FixPrimitiveRefs();
                }

                return _cylinderMesh;
            }
        }

        private Material DefaultMaterial
        {
            get
            {
                if (_defaultMaterial == null)
                {
                    FixPrimitiveRefs();
                }

                return _defaultMaterial;
            }
        }

        private void FixPrimitiveRefs()
        {
            //得到立方体网格  材质球  胶囊网格
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _cubeMesh = cube.GetComponent<MeshFilter>().sharedMesh;
            _cylinderMesh = cylinder.GetComponent<MeshFilter>().sharedMesh;
            _defaultMaterial = cube.GetComponent<MeshRenderer>().sharedMaterial;
            if (Application.isPlaying)
            {
                GameObject.Destroy(cube);
                GameObject.Destroy(cylinder);
            }
            else
            {
                GameObject.DestroyImmediate(cube);
                GameObject.DestroyImmediate(cylinder);
            }
        }

        private int CalculateSettingsHash()
        {
            unchecked
            {
                int hashCode = 0;
                for (int i = 0; i < corners.Count; i++)
                {
                    if (corners[i] != null)
                    {
                        hashCode = (hashCode * 397) ^ corners[i].localPosition.GetHashCode();
                    }
                }

                hashCode = (hashCode * 397) ^ radius.GetHashCode();
                hashCode = (hashCode * 397) ^ makeRenderers.GetHashCode();
                hashCode = (hashCode * 397) ^ transform.childCount.GetHashCode();
                hashCode = (hashCode * 397) ^ heigth.GetHashCode();
                hashCode = (hashCode * 397) ^ depth.GetHashCode();
                hashCode = (hashCode * 397) ^ extraWidth.GetHashCode();
                hashCode = (hashCode * 397) ^ invert.GetHashCode();
                hashCode = (hashCode * 397) ^ loop.GetHashCode();
                hashCode = (hashCode * 397) ^ gameObject.layer.GetHashCode();
                hashCode = (hashCode * 397) ^ isTrigger.GetHashCode();
                hashCode = (hashCode * 397) ^ copyTag.GetHashCode();
                if (copyTag) hashCode = (hashCode * 397) ^ gameObject.tag.GetHashCode();
                hashCode = (hashCode * 397) ^ gameObject.isStatic.GetHashCode();
                hashCode = (hashCode * 397) ^ GameObjectUtility.GetStaticEditorFlags(gameObject).GetHashCode();
                hashCode = (hashCode * 397) ^ ((physicsMaterial) ? physicsMaterial.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ DEBUG.GetHashCode();
                return hashCode;
            }
        }

        public int loopInt()
        {
            return (loop) ? 0 : -1;
        }

        private void Update()
        {
            if (Application.isPlaying) return;
            var lossyScale = transform.lossyScale;
            transform.localScale = Vector3.Scale(transform.localScale,
                new Vector3(1 / lossyScale.x, 1 / lossyScale.y, 1 / lossyScale.z));


            var newHash = CalculateSettingsHash();
            if (newHash != lastHash)
            {
                lastHash = newHash;

                corners.Clear();
                for (int i = 0; i < transform.childCount; i++)
                {
                    var child = transform.GetChild(i);
                    if (child != colliderContainer)
                    {
                        corners.Add(child);
                        if (CanBeEdited())
                        {
                            child.gameObject.name = corners.Count.ToString();
                        }
                    }

                }

                if (CanBeEdited())
                {
                    MakeColliders();
                }
            }
            //for (int i = 0; i < transform.childCount; i++) {
            //    var child = transform.GetChild(i);
            //    if (child != colliderContainer) {
            //        child.gameObject.hideFlags = (DEBUG || CanBeEdited()) ? HideFlags.None : HideFlags.HideInHierarchy;
            //    }

            //}
            //ColliderContainer.hideFlags = (DEBUG) ? HideFlags.None : HideFlags.HideInHierarchy;
        }


        private void Start()
        {
            MakeColliders();

        }

        public Transform ColliderContainer
        {
            get
            {
                if (colliderContainer == null)
                {
                    colliderContainer = new GameObject("Colliders").transform;
                    colliderContainer.SetParent(transform);
                    Reset(colliderContainer);
                }

                return colliderContainer;
            }
        }

        public void MakeColliders()
        {
            DestroyAllChildren(ColliderContainer);
            for (int i = 0; i < corners.Count; i++)
            {


                corners[i].localPosition = ChangeY(corners[i].localPosition, 0);
            }

            if (corners.Count >= 2)
            {
                MakeAllColliders();
            }
            else
            {
                for (int i = corners.Count; i < 2; i++)
                {
                    var newObj = new GameObject().transform;
                    newObj.SetParent(transform);
                    newObj.localPosition = 5 * Vector3.forward * i;
                    newObj.gameObject.name = i.ToString();

                    newObj.gameObject.AddComponent<NodeShowCollider>();
                    DrawIcon(newObj.gameObject, 0);
                }
            }
        }

        public void MakeAllColliders()
        {
            MakeAllEdgeColliders(InSetCorners(FillCachedCornerList(corners), radius));
            if (radius > 0)
            {
                MakeAllCornerColliders(InSetCorners(FillCachedCornerList(corners), radius));
            }
        }

        public bool CanBeEdited()
        {
#if UNITY_2018_3_OR_NEWER
            return PrefabUtility.GetPrefabInstanceStatus(gameObject) == PrefabInstanceStatus.NotAPrefab;
#else
            return true;
#endif
        }

        public void MakeAllEdgeColliders(List<Vector2> corners)
        {
            for (int i = 0; i < corners.Count + loopInt(); i++)
            {
                MakeEdgeCollider(string.Format("{0} - {1}", i + 1, ((i + 1) % corners.Count) + 1), corners[i],
                    corners[(i + 1) % corners.Count], radius, heigth + depth, depth, extraWidth);
            }
        }

        public void MakeAllCornerColliders(List<Vector2> corners)
        {
            for (int i = 0; i < corners.Count; i++)
            {
                MakeCornerCollider(string.Format("{0}", i + 1), corners[i], radius, heigth + depth, depth);
            }
        }

        private void MakeEdgeCollider(string name, Vector2 from, Vector2 to, float radius, float heigth, float yOffset,
                                      float extraWidth)
        {
            var go = new GameObject("EdgeCollider " + name);
            go.layer = gameObject.layer;
            GameObjectUtility.SetStaticEditorFlags(go, GameObjectUtility.GetStaticEditorFlags(gameObject));
            if (copyTag) go.tag = gameObject.tag;
            go.transform.SetParent(ColliderContainer);
            var collider = go.AddComponent<BoxCollider>();
            collider.sharedMaterial = physicsMaterial;
            collider.isTrigger = isTrigger;
            if (makeRenderers)
            {
                go.AddComponent<MeshFilter>().sharedMesh = CubeMesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = DefaultMaterial;
            }

            go.transform.localPosition = ChangeY(To3D((from + to) / 2 + Perp(from, to, true) * extraWidth / 2), (heigth / 2 - yOffset));
            go.transform.rotation = Quaternion.LookRotation(transform.TransformDirection(To3D((to - from).normalized)), transform.TransformDirection(Vector3.up));
            go.transform.localScale = new Vector3(radius * 2 + extraWidth, heigth, Vector2.Distance(from, to));
        }

        private void MakeCornerCollider(string name, Vector2 current, float radius, float heigth, float yOffset)
        {
            var go = new GameObject("CornerCollider " + name);
            go.layer = gameObject.layer;
            GameObjectUtility.SetStaticEditorFlags(go, GameObjectUtility.GetStaticEditorFlags(gameObject));
            if (copyTag) go.tag = gameObject.tag;
            go.transform.SetParent(ColliderContainer);
            var collider = go.AddComponent<CapsuleCollider>();
            go.transform.localPosition = ChangeY(To3D(current), (heigth / 2 - yOffset));
            collider.radius = radius;
            collider.height = heigth + radius * 2;
            collider.sharedMaterial = physicsMaterial;
            collider.isTrigger = isTrigger;
            go.transform.localRotation = Quaternion.identity;

            if (makeRenderers)
            {
                var r = new GameObject("CornerCollider");
                r.layer = gameObject.layer;
                GameObjectUtility.SetStaticEditorFlags(r, GameObjectUtility.GetStaticEditorFlags(gameObject));
                if (copyTag) r.tag = gameObject.tag;
                r.transform.SetParent(ColliderContainer);
                r.AddComponent<MeshFilter>().sharedMesh = CylinderMesh;
                r.AddComponent<MeshRenderer>().sharedMaterial = DefaultMaterial;
                r.transform.localPosition = ChangeY(To3D(current), (heigth / 2 - yOffset));
                r.transform.localScale = new Vector3(radius * 2, heigth / 2, radius * 2);
                r.transform.localRotation = Quaternion.identity;
            }
        }

        private List<Vector2> FillCachedCornerList(List<Transform> corners)
        {
            cachedCornerList.Clear();
            cachedCornerList.AddRange(corners.Select((e) => To2D(e.localPosition)));
            if (invert)
            {
                cachedCornerList.Reverse();
            }

            return cachedCornerList;
        }

        private List<Vector2> InSetCorners(List<Vector2> corners, float radius)
        {
            cachedCornerListToOutset.Clear();
            for (int i = 0; i < corners.Count; i++)
            {
                var prev = corners[(i - 1 + corners.Count) % corners.Count];
                var cur = corners[i];
                var next = corners[(i + 1) % corners.Count];
                if (!loop && i == 0)
                {
                    prev = cur + (cur - next);
                }

                if (!loop && i == corners.Count - 1)
                {
                    next = cur + (cur - prev);
                }

                cachedCornerListToOutset.Add(InsetCorner(prev, cur, next, radius));
            }

            corners.Clear();
            corners.AddRange(cachedCornerListToOutset);

            return corners;
        }

        private Vector2 InsetCorner(Vector2 prev, Vector2 current, Vector2 next, float radius)
        {
            Vector2 nextdir = (next - current).normalized;
            Vector2 prevdir = (prev - current).normalized;
            Vector2 perpdir = Perp(current, prev, false);

            float cos = Mathf.Cos(Vector2.Angle(perpdir, nextdir) * Mathf.Deg2Rad);
            float d = radius / cos;

            if (Mathf.Abs(cos) < 0.00001f)
            {
                return current + perpdir * radius;
            }

            return current
                   + d * nextdir
                   + d * prevdir;
        }


        public static void DestroyAllChildren(Transform transform)
        {
            if (Application.isPlaying)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    GameObject.Destroy(transform.GetChild(i).gameObject);
                }
            }
            else
            {
                while (transform.childCount > 0)
                {
                    GameObject.DestroyImmediate(transform.GetChild(0).gameObject);
                }
            }
        }

        public static void Reset(Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
        }

        public static void DrawIcon(GameObject gameObject, int idx)
        {
            var largeIcons = GetTextures("sv_label_", string.Empty, 0, 8);
            var icon = largeIcons[idx];
            var egu = typeof(EditorGUIUtility);
            var flags = BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.NonPublic;
            var args = new object[] { gameObject, icon.image };

            var setIcon = egu.GetMethod("SetIconForObject", flags, null,
                new Type[] { typeof(UnityEngine.Object), typeof(Texture2D) }, null);
            setIcon.Invoke(null, args);
        }

        private static GUIContent[] GetTextures(string baseName, string postFix, int startIndex, int count)
        {
            GUIContent[] array = new GUIContent[count];
            for (int i = 0; i < count; i++)
            {
                array[i] = EditorGUIUtility.IconContent(baseName + (startIndex + i) + postFix);
            }

            return array;
        }



        public static Vector3 ChangeY(Vector3 parent, float newY)
        {
            return new Vector3(parent.x, newY, parent.z);
        }


        public static Vector3 ChangeY(Vector3 parent, FloatEdit edit)
        {
            return new Vector3(parent.x, edit(parent.y), parent.z);
        }


        public delegate float FloatEdit(float input);






        //private void OnDrawGizmos() {
        //    //if (!onlyWhenSelected && isActiveAndEnabled) DrawGizmos();
        //}

        //private void OnDrawGizmosSelected()
        //{
        //    if (onlyWhenSelected && isActiveAndEnabled) DrawGizmos();
        //}

        //private void DrawGizmos()
        //{
        //    Color prevHandlesColor = Handles.color;
        //    Color prevGizmosColor = Gizmos.color;
        //    Gizmos.color = Handles.color = GizmoColor;

        //    if (corners.Count >= 2)
        //    {
        //        DrawPolygon(InSetCorners(FillCachedCornerList(corners), radius));
        //        //Draw
        //    }

        //    Handles.color = prevHandlesColor;
        //    Gizmos.color = prevGizmosColor;
        //}

        //private void DrawPolygon(List<Vector2> corners)
        //{
        //    for (int i = 0; i < corners.Count + loopInt(); i++)
        //    {
        //        DrawEdge(corners[i], corners[(i + 1) % corners.Count], radius);
        //    }
        //}

        //private void DrawEdge(Vector2 from, Vector2 to, float radius)
        //{
        //    var offset = Perp(from, to, false) * radius;
        //    Vector3 start = To3D(from + offset);
        //    Vector3 end = To3D(to + offset);
        //    DrawLine(start, end);
        //    DrawLine(start.ChangeY(-depth), end.ChangeY(-depth));
        //    DrawLine(start.ChangeY(heigth), end.ChangeY(heigth));


        //    float distance = Vector3.Distance(start, end);
        //    int n = Mathf.CeilToInt(distance / GizmoLineInterval);
        //    n = Mathf.Min(n, 250);
        //    for (int i = 0; i <= n; i++)
        //    {
        //        if (n != 0)
        //        {
        //            DrawBar(Vector3.Lerp(start, end, (float)i / n));
        //        }
        //    }
        //}

        //void DrawBar(Vector3 pos)
        //{
        //    DrawLine(pos.ChangeY(-depth), pos.ChangeY(heigth));
        //}




        //private void DrawLine(Vector3 from, Vector3 to)
        //{
        //    Gizmos.DrawLine(transform.TransformPoint(from), transform.TransformPoint(to));
        //}


        private Vector3 To3D(Vector2 vec)
        {
            return new Vector3(vec.x, 0, vec.y);
        }

        private Vector2 To2D(Vector3 vec)
        {
            return new Vector2(vec.x, vec.z);
        }

        private Vector2 Perp(Vector2 dir, bool inverse)
        {
            if (inverse)
            {
                return new Vector2(dir.y, -dir.x);
            }
            else
            {
                return new Vector2(-dir.y, dir.x);
            }
        }

        private Vector2 Perp(Vector2 from, Vector2 to, bool inverse)
        {
            var dir = to - from;
            return Perp(dir, inverse).normalized;
        }

        //private float Atan2angle(Vector2 vector2) {
        //    return Mathf.Atan2(vector2.y, vector2.x) * Mathf.Rad2Deg;
        //}

        //private float FromToAngle(Vector2 prev, Vector2 current, Vector2 next) {
        //    return (Atan2angle(prev - current) - Atan2angle(current - next) + 360) % 360;
        //}

        private void OnDestroy()
        {
            //if (colliderContainer)
            //{
            if (Application.isPlaying)
            {
                foreach (Transform t in corners)
                {


                    Destroy(t.gameObject);
                }
            }
            else
            {
                foreach (Transform t in corners)
                {
                    DestroyImmediate(t.gameObject);
                }
            }

            //}
        }

    }
}

#endif