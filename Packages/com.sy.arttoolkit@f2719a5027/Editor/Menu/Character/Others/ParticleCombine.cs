using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SYArtToolkit
{

    public class ParticleCombine : MonoBehaviour
    {
        private static void Normalize(GameObject go, Transform transform)
        {
            go.transform.position = transform.position;
            go.transform.rotation = Quaternion.Euler(0, 0, 0);
            go.transform.localScale = Vector3.one;
        }

        private static void ParticleWithMesh(ParticleSystem system)
        {
            var systemShape = system.shape;
            systemShape.enabled = false;
            var render = system.GetComponent<ParticleSystemRenderer>();
            render.renderMode = ParticleSystemRenderMode.Mesh;
            render.alignment = ParticleSystemRenderSpace.Local;
            var emission = system.emission;
            emission.rateOverTime = 0;
            emission.rateOverDistance = 0;
            ParticleSystem.Burst burst = new ParticleSystem.Burst();
            burst.time = 0;
            burst.count = 1;
            burst.cycleCount = 1;
            burst.repeatInterval = 0.01f;
            burst.probability = 1;
            emission.SetBursts(new ParticleSystem.Burst[] { burst });

            var systemMain = system.main;
            // systemMain.duration = 1;
            systemMain.startDelay = 0;
            systemMain.startLifetime = 1;
            systemMain.startSpeed = 0;
        }

        //设置粒子系统的Scale
        private static Vector3 MulVector3(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
        }

        private static bool SetParticleScale(ParticleSystem system, out Vector3 sourceScale)
        {
            sourceScale = system.transform.localScale;
            if (system == null)
            {
                return false;
            }

            if (system.main.scalingMode == ParticleSystemScalingMode.Local)
            {
                return false;
            }

            var systemMain = system.main;
            Transform parent = system.transform.parent;
            Vector3 scale = Vector3.one;
            while (parent)
            {
                scale = MulVector3(scale, parent.localScale);
                parent = parent.parent;
            }

            system.transform.localScale = MulVector3(system.transform.localScale, scale);
            systemMain.scalingMode = ParticleSystemScalingMode.Local;
            return true;
        }

        [MenuItem("GameObject/特效工具/合并粒子发射模型网格")]
        public static void Combine(MenuCommand command)
        {
            //create mesh loader
            GameObject selectObject = command.context as GameObject;
            if (selectObject == null)
            {
                return;
            }

            Vector3 finalPos = selectObject.transform.position;
            selectObject.transform.position = Vector3.zero;
            List<ParticleSystemRenderer> renderers = new List<ParticleSystemRenderer>();
            renderers.AddRange(selectObject.GetComponentsInChildren<ParticleSystemRenderer>());
            if (renderers.Count < 1)
            {
                return;
            }

            List<MeshFilter> goes = new List<MeshFilter>(renderers.Count);

            //set final material and mesh path
            Material material = null;
            string meshPath = null;
            foreach (var systemRenderer in renderers)
            {
                if (systemRenderer.sharedMaterial != null)
                {
                    material = systemRenderer.sharedMaterial;
                    meshPath = AssetDatabase.GetAssetPath(systemRenderer.mesh);
                    break;
                }
            }

            if (material == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(meshPath))
            {
                return;
            }

            //collect particle system
            foreach (var particleSystemRenderer in renderers)
            {
                GameObject go = new GameObject();
                Normalize(go, particleSystemRenderer.transform);
                go.AddComponent<MeshRenderer>().sharedMaterial = material;

                Mesh tempMesh = new Mesh();
                var particleSystem = particleSystemRenderer.GetComponent<ParticleSystem>();
                bool reset = SetParticleScale(particleSystem, out Vector3 sourceScale);
                particleSystemRenderer.BakeMesh(tempMesh);
                if (reset)
                {
                    particleSystem.transform.localScale = sourceScale;
                    var particleSystemMain = particleSystem.main;
                    particleSystemMain.scalingMode = ParticleSystemScalingMode.Hierarchy;
                }

                MeshFilter filter = go.AddComponent<MeshFilter>();
                filter.sharedMesh = tempMesh;
                goes.Add(filter);

                particleSystemRenderer.enabled = false;
            }

            //combine mesh
            List<CombineInstance> combine = new List<CombineInstance>(renderers.Count);
            Mesh outMesh = new Mesh();
            outMesh.Clear();
            foreach (var filter in goes)
            {
                combine.Add(new CombineInstance()
                {
                    mesh = filter.sharedMesh,
                    transform = filter.transform.localToWorldMatrix,
                });
            }

            outMesh.CombineMeshes(combine.ToArray());

            //set mesh and particle system
            GameObject final = new GameObject(selectObject.name + "_combine");
            final.transform.position = finalPos;
            selectObject.transform.position = finalPos;
            var particle = final.AddComponent<ParticleSystem>();
            var pRender = particle.GetComponent<ParticleSystemRenderer>();
            pRender.sharedMaterial = material;
            pRender.mesh = outMesh;
            ParticleWithMesh(particle);

            //destroy mesh loader
            foreach (var go in goes)
            {
                DestroyImmediate(go.gameObject);
            }

            Selection.activeGameObject = final;

            //store mesh
            string combineMeshPath = null;
            int index = 0;
            while (AssetDatabase.LoadAssetAtPath<Mesh>(combineMeshPath =
                       meshPath.Split('.')[0] + "_combine_" + index++ + ".mesh")) ;

            AssetDatabase.Refresh();
            AssetDatabase.CreateAsset(outMesh, combineMeshPath);
            AssetDatabase.Refresh();
        }
    }
}