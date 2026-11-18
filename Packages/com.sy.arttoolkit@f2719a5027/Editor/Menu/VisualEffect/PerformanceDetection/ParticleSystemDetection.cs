
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;

namespace SYArtToolkit
{

    public class ParticleAndTextureDetection
    {

        protected HashSet<Mesh> meshes;
        protected Dictionary<Texture, long> texture2Ds;
        protected long vertexSumCount;
        protected long triangleSumCount;
        protected long particleSystemSumCount;
        protected long particleSumCount;
        protected long skinMeshSumCount;


        private MethodInfo textureMethodInfo;
        //private MethodInfo particleMethodInfo;
        public ParticleAndTextureDetection()
        {
            meshes = new HashSet<Mesh>();
            texture2Ds = new Dictionary<Texture, long>();
            vertexSumCount = 0;
            triangleSumCount = 0;
            particleSystemSumCount = 0;
            particleSumCount = 0;
            skinMeshSumCount = 0;
            Assembly assembly = typeof(AssetDatabase).Assembly;
            Type custom = assembly.GetType("UnityEditor.TextureUtil");
            textureMethodInfo = custom.GetMethod("GetStorageMemorySize", BindingFlags.Public | BindingFlags.Static);
            // particleMethodInfo= typeof(ParticleSystem).GetMethod("CalculateEffectUIData",
            //     BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public void ResetValue()
        {
            meshes.Clear();
            texture2Ds.Clear();
            vertexSumCount = 0;
            triangleSumCount = 0;
            particleSystemSumCount = 0;
            particleSumCount = 0;
            skinMeshSumCount = 0;
        }

        public (int, int) getParticleSystemNumber(GameObject obj)
        {
            if (obj == null) return (0, 0);
            var particles = obj.GetComponentsInChildren<ParticleSystem>();
            int res = 0;
            foreach (var p in particles)
            {
                if (p != null) res += p.particleCount;

                // if (p != null)
                // {
                //     int count = 0;
                //     object[] invokeArgs = {count, 0.0f, Mathf.Infinity};
                //     ParameterInfo.Invoke(p, invokeArgs);
                //     count = (int) invokeArgs[0];
                //     m_ParticleCount += count;
                // }
            }
            particleSumCount += res;
            particleSystemSumCount += particles.Length;
            return (particles.Length, res);
        }

        public int GetMeshNum(GameObject obj)
        {
            if (obj == null) return 0;
            HashSet<Mesh> resMeshes = new HashSet<Mesh>();
            var meshes0 = obj.GetComponentsInChildren<MeshFilter>();
            var meshes1 = obj.GetComponentsInChildren<ParticleSystemRenderer>();
            var meshes2 = obj.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var m in meshes0)
            {
                var mesh = m.sharedMesh;
                if (mesh != null && !resMeshes.Contains(mesh))
                {
                    resMeshes.Add(mesh);
                    if (!meshes.Contains(mesh))
                    {
                        meshes.Add(mesh);
                    }
                }
            }
            foreach (var m in meshes1)
            {
                var mesh = m.mesh;
                if (mesh != null && !resMeshes.Contains(mesh))
                {
                    resMeshes.Add(mesh);
                    if (!meshes.Contains(mesh))
                    {
                        meshes.Add(mesh);
                    }
                }
            }
            foreach (var m in meshes2)
            {
                var mesh = m.sharedMesh;
                if (mesh != null && !resMeshes.Contains(mesh))
                {
                    resMeshes.Add(mesh);
                    if (!meshes.Contains(mesh))
                    {
                        meshes.Add(mesh);
                    }
                }
            }
            return resMeshes.Count;
        }

        public int GetSkinMeshNum(GameObject obj)
        {
            if (obj == null) return 0;
            var length = obj.transform.GetComponentsInChildren<SkinnedMeshRenderer>().Length;
            skinMeshSumCount += length;
            return length;
        }

        public (int, int) GetVertexNum(GameObject obj)
        {
            if (obj == null) return (0, 0);

            //双帧





            MeshFilter[] mtemp = obj.transform.GetComponentsInChildren<MeshFilter>(false);
            int vertexnum = 0;
            int meshnum = 0;
            foreach (var _m in mtemp)
            {
                vertexnum += _m.sharedMesh.vertexCount;
                meshnum += _m.sharedMesh.triangles.Length / 3;
            }

            SkinnedMeshRenderer[] renders = obj.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer render in renders)
            {
                if (render.sharedMesh != null)
                {
                    vertexnum += render.sharedMesh.vertexCount;
                    meshnum += render.sharedMesh.triangles.Length / 3;
                }
            }

            var particleSystems = obj.GetComponentsInChildren<ParticleSystem>();
            foreach (var t in particleSystems)
            {
                //mesh顶点过多

                var item = t.gameObject.GetComponent<ParticleSystemRenderer>();
                if (!item) continue;

                int num = t.particleCount;
                if (t.trails.enabled)
                {
                    var trails = t.trails;
                    if (trails.mode == ParticleSystemTrailMode.Ribbon)
                    {
                        num = t.particleCount;
                        vertexnum += num * 2 + 2;
                        meshnum += num * 2;
                        if (trails.ribbonCount > 1)
                        {

                            if (trails.ribbonCount >= t.particleCount)
                            {
                                vertexnum += num * 2;
                                meshnum += num * 2;
                            }
                            else
                            {
                                vertexnum += trails.ribbonCount * 2;
                                meshnum += trails.ribbonCount * 2;
                            }
                        }
                    }
                    else
                    {
                        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[num];
                        t.GetParticles(particles);
                        num = 0;
                        foreach (var p in particles)
                        {
                            if (p.remainingLifetime > 0)
                            {
                                ++num;
                                if (Random.Range(0, 1) < trails.ratio)
                                {
                                    float time = p.startLifetime - p.remainingLifetime;
                                    if (time > trails.lifetime.constant) time = trails.lifetime.constant;
                                    float x = time * p.velocity.magnitude;
                                    int number = (int)Mathf.Ceil((x / Mathf.Max(0.1f, trails.minVertexDistance)));
                                    vertexnum += 6 * number;
                                    meshnum += 6 * number - 2;
                                }
                            }
                        }

                    }
                }

                //冗余mesh问题
                if (item.renderMode != ParticleSystemRenderMode.Mesh)
                {
                    if (item.mesh)
                    {
                        //Debug.Log(item+"Mesh冗余");
                    }

                    if (item.renderMode != ParticleSystemRenderMode.None)
                    {
                        vertexnum += 4 * num;
                        meshnum += 2 * num;
                    }
                }
                else
                {
                    if (item.mesh == null) continue;
                    vertexnum += item.mesh.vertexCount * num;
                    meshnum += item.mesh.triangles.Length / 3 * num;
                }


            }

            vertexSumCount += vertexnum;
            triangleSumCount += meshnum;
            return (vertexnum, meshnum);
        }

        private List<Texture> GetCertainMaterialTexture(Material[] mats) //获取shader里面的属性的方法
        {
            List<Texture> results = new List<Texture>();
            if (mats == null) return results;
            foreach (var mat in mats)
            {
                if (mat == null) continue;
                Shader shader = mat.shader;
                if (shader == null) continue;
                for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); ++i)
                {
                    if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                    {
                        string propertyName = ShaderUtil.GetPropertyName(shader, i);
                        Texture tex = mat.GetTexture(propertyName);
                        if (tex) results.Add(tex);
                    }
                }
            }

            return results;
        }

        public (int, long) DetectTexture(GameObject obj)
        {
            long memory = 0;
            int num = 0;
            if (obj == null || obj.activeSelf == false) return (0, 0);
            var renderers = obj.GetComponentsInChildren<Renderer>();
            HashSet<Texture> hash = new HashSet<Texture>();
            foreach (var r in renderers)
            {
                List<Texture> lTemp = GetCertainMaterialTexture(r.sharedMaterials);
                foreach (var t in lTemp)
                {
                    if (t == null || t.hideFlags == HideFlags.None) continue;
                    if (!hash.Contains(t))
                    {
                        hash.Add(t);
                    }
                }
            }

            foreach (var t in hash)
            {
                var tMemory = GetStorageMemorySize(t);
                if (!texture2Ds.ContainsKey(t)) texture2Ds.Add(t, tMemory);
                memory += tMemory;
            }
            num = hash.Count;
            return (num, memory);
        }

        private int GetStorageMemorySize(Texture texture)
        {
            return (int)InvokeInternalAPI(texture);

        }

        private object InvokeInternalAPI(params object[] parameters)
        {
            return textureMethodInfo != null ? textureMethodInfo.Invoke(null, parameters) : 0;
        }




        public (int, long) DetectTexture(List<GameObject> objs)
        {
            long memory = 0;
            int num = 0;
            if (objs == null) return (0, 0);
            HashSet<Texture> hash = new HashSet<Texture>();
            foreach (var obj in objs)
            {
                if (obj == null || obj.activeInHierarchy == false) continue;
                var renderers = obj.GetComponentsInChildren<Renderer>();
                //Dictionary<string, Texture> hash = new Dictionary<string, Texture>();
                foreach (var r in renderers)
                {
                    List<Texture> lTemp = GetCertainMaterialTexture(r.sharedMaterials);
                    foreach (var t in lTemp)
                    {
                        if (t == null) continue;
                        if (!hash.Contains(t)) hash.Add(t);
                    }
                }

            }

            foreach (var t in hash)
            {
                if (t.hideFlags == HideFlags.None)
                    continue;
                memory += Profiler.GetRuntimeMemorySizeLong(t);
            }

            num = hash.Count;
            return (num, memory);
        }
    }
}