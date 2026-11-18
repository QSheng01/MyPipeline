using UnityEngine;

namespace MPipeline
{
    /// <summary>Patch mesh generation, following the reference MeshUtility.</summary>
    internal static class MeshUtility
    {
        internal static Mesh CreatePlaneMesh(int cells)
        {
            var vertices = new Vector3[(cells + 1) * (cells + 1)];
            var normals = new Vector3[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var indices = new int[cells * cells * 6];
            for (int z = 0; z <= cells; z++)
            for (int x = 0; x <= cells; x++)
            {
                int index = z * (cells + 1) + x;
                vertices[index] = new Vector3(x, 0, z);
                normals[index] = Vector3.up;
                uv[index] = new Vector2(x / (float)cells, z / (float)cells);
            }
            int triangle = 0;
            for (int z = 0; z < cells; z++)
            for (int x = 0; x < cells; x++)
            {
                int a = z * (cells + 1) + x;
                indices[triangle++] = a;
                indices[triangle++] = a + cells + 1;
                indices[triangle++] = a + 1;
                indices[triangle++] = a + 1;
                indices[triangle++] = a + cells + 1;
                indices[triangle++] = a + cells + 2;
            }
            var patch = new Mesh { name = "GPU Terrain Patch 16x16", hideFlags = HideFlags.HideAndDontSave };
            patch.vertices = vertices;
            patch.normals = normals;
            patch.uv = uv;
            patch.triangles = indices;
            patch.UploadMeshData(true);
            return patch;
        }
    }
}
