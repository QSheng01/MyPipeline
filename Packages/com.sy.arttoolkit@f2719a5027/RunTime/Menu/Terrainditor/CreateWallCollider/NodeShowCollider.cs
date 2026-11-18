#if UNITY_EDITOR

using UnityEngine;
namespace SYArtToolkit.CreateEasyWallCollider
{
    [ExecuteAlways]
    public class NodeShowCollider : MonoBehaviour
    {
        // Start is called before the first frame update
        public EasyWallCollider easyWallCollider;

        private void Start()
        {
            easyWallCollider = transform.parent.GetComponent<EasyWallCollider>();
        }


        private void OnDrawGizmosSelected()
        {
            if (easyWallCollider != null && !easyWallCollider.alwaysShowLine)
                DrawColliderShape();
        }
        private void OnDrawGizmos()
        {

            if (easyWallCollider != null && easyWallCollider.alwaysShowLine)
                DrawColliderShape();
        }

        private void DrawColliderShape()
        {






            Gizmos.color = easyWallCollider.lineColor;

            Transform selectedTransform = transform;

            if (selectedTransform == null || selectedTransform.GetComponent<Collider>() != null)
                return;
            else if (selectedTransform.parent == null) return;
            else if (selectedTransform.parent.GetComponent<EasyWallCollider>() == null) return;



            // 获取所有子物体的碰撞体
            Collider[] colliders = selectedTransform.parent.GetComponentsInChildren<Collider>();

            // 绘制每个碰撞体的边界框
            foreach (var collider in colliders)
            {


                if (collider is BoxCollider boxCollider)
                {
                    DrawWireCube(boxCollider.transform.TransformPoint(boxCollider.center), boxCollider.gameObject.transform.localScale, boxCollider.transform.rotation);
                }
                //else if (collider is SphereCollider sphereCollider)
                //{
                //    DrawWireSphere(sphereCollider.transform.TransformPoint(sphereCollider.center), sphereCollider.radius);
                //}
                else if (collider is CapsuleCollider capsuleCollider)
                {

                    DrawWireCapsule(capsuleCollider.transform.TransformPoint(capsuleCollider.center), capsuleCollider.radius, capsuleCollider.height, capsuleCollider.transform.rotation);
                }



            }
            // 添加其他碰撞体类型的处理...

            // 可以根据需要添加更多的碰撞体类型
        }

        private void DrawWireCube(Vector3 center, Vector3 size, Quaternion rotation)
        {
            Gizmos.matrix = Matrix4x4.TRS(center, rotation, size);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

            Gizmos.matrix = Matrix4x4.identity;



        }


        private void DrawWireCapsule(Vector3 center, float radius, float height, Quaternion rotation)
        {




            // 绘制胶囊体的柱体部分

            // 获取胶囊体的轴向
            Vector3 upAxis = rotation * Vector3.up;

            // 绘制胶囊体的两个盖子
            DrawWireArc(center + upAxis * (height * 0.5f - radius), upAxis, rotation * Vector3.forward, 360f, radius);
            DrawWireArc(center + upAxis * (-height * 0.5f + radius), upAxis, rotation * Vector3.forward, 360f, radius);
        }



        private void DrawWireArc(Vector3 center, Vector3 axis, Vector3 from, float angle, float radius)
        {
            float angleStep = 10f;
            Vector3 lastPoint = center + Quaternion.AngleAxis(0, axis) * from * radius;

            for (float a = angleStep; a <= angle; a += angleStep)
            {
                float radians = a * Mathf.Deg2Rad;
                Vector3 nextPoint = center + Quaternion.AngleAxis(a, axis) * from * radius;
                Gizmos.DrawLine(lastPoint, nextPoint);
                lastPoint = nextPoint;
            }
        }



        //private void DrawWireSphere(Vector3 center, float radius)
        //{
        //    Gizmos.DrawWireSphere(center, radius);
        //}


    }
}
#endif