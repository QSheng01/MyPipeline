#if UNITY_EDITOR
namespace SYArtToolkit.CreateEasyWallCollider
{

    using System;
    using UnityEngine;




    [Serializable]
    public class WallColliderData : ScriptableObject
    {
        [SerializeField]
        new public string name;
        [SerializeField]
        public Vector3 position;
        [SerializeField]
        public Vector2[] v2s;
        [SerializeField]
        public float radius = 0.5f;
        [SerializeField]
        public float heigth = 2f;
        [SerializeField]
        public float depth = 0f;
        [SerializeField]
        public float extraWidth = 0f;
        [SerializeField]
        public bool invert = false;
        [SerializeField]
        public bool loop = false;
        [SerializeField]
        public bool makeRenderers = false;
        [SerializeField]
        public bool isTrigger = false;
        [SerializeField]
        public PhysicMaterial physicsMaterial = null;
        [SerializeField]
        public bool copyTag = false;

        public WallColliderData(EasyWallCollider collider)
        {
            position = collider.gameObject.transform.position;
            v2s = new Vector2[collider.corners.Count];
            for (int i = 0; i < collider.corners.Count; i++)
            {
                var v3 = collider.corners[i].position;
                v2s[i] = new Vector2(v3.x, v3.z);

            }
            radius = collider.radius;
            heigth = collider.heigth;
            depth = collider.depth;
            extraWidth = collider.extraWidth;
            invert = collider.invert;
            loop = collider.loop;
            makeRenderers = collider.makeRenderers;
            isTrigger = collider.isTrigger;
            physicsMaterial = collider.physicsMaterial;
            copyTag = collider.copyTag;
            name = System.DateTime.Now.ToString();

        }


    }
}
#endif