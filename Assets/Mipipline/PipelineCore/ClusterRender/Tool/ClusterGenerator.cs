#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MPipeline
{
    public sealed class ClusterBakeResult
    {
        public Cluster[] clusters;
        public Point[] points;
        public int[] triangleMaterials;
        public Bounds localBounds;
    }

    /// <summary>
    /// Groups triangle data into fixed-size spatial clusters.
    /// Each cluster contains 128 triangles, padded to 384 vertices.
    /// </summary>
    public static class ClusterGenerator
    {
        public const int VerticesPerCluster = 384;
        public const int TrianglesPerCluster = 128;

        public static ClusterBakeResult Generate(List<Point> sourcePoints, int voxelCount)
        {
            return Generate(sourcePoints, null, voxelCount);
        }

        public static ClusterBakeResult Generate(
            List<Point> sourcePoints,
            List<int> sourceTriangleMaterials,
            int voxelCount)
        {
            if (sourcePoints == null || sourcePoints.Count < 3)
                return new ClusterBakeResult
                {
                    clusters = Array.Empty<Cluster>(),
                    points = Array.Empty<Point>(),
                    triangleMaterials = Array.Empty<int>(),
                    localBounds = new Bounds()
                };

            if ((sourcePoints.Count % 3) != 0)
                throw new InvalidOperationException("Cluster input must contain complete triangles.");

            voxelCount = Mathf.Max(1, voxelCount);
            Bounds bounds = CalculateBounds(sourcePoints);
            Vector3 min = bounds.min;
            Vector3 size = bounds.size;
            size.x = Mathf.Max(size.x, 0.0001f);
            size.y = Mathf.Max(size.y, 0.0001f);
            size.z = Mathf.Max(size.z, 0.0001f);

            int triangleCount = sourcePoints.Count / 3;
            if (sourceTriangleMaterials != null && sourceTriangleMaterials.Count != triangleCount)
                throw new InvalidOperationException("Triangle material count must match the triangle count.");

            Dictionary<int, List<int>> voxels = new Dictionary<int, List<int>>();
            for (int triangle = 0; triangle < triangleCount; ++triangle)
            {
                Vector3 center = (
                    ToVector3(sourcePoints[triangle * 3].vertex) +
                    ToVector3(sourcePoints[triangle * 3 + 1].vertex) +
                    ToVector3(sourcePoints[triangle * 3 + 2].vertex)) / 3f;

                Vector3 normalized = Vector3.Scale(center - min, new Vector3(
                    1f / size.x,
                    1f / size.y,
                    1f / size.z));
                Vector3Int coordinate = new Vector3Int(
                    Mathf.Clamp(Mathf.FloorToInt(normalized.x * voxelCount), 0, voxelCount - 1),
                    Mathf.Clamp(Mathf.FloorToInt(normalized.y * voxelCount), 0, voxelCount - 1),
                    Mathf.Clamp(Mathf.FloorToInt(normalized.z * voxelCount), 0, voxelCount - 1));

                int key = Flatten(coordinate, voxelCount);
                List<int> bucket;
                if (!voxels.TryGetValue(key, out bucket))
                {
                    bucket = new List<int>();
                    voxels.Add(key, bucket);
                }
                bucket.Add(triangle);
            }

            List<int> orderedTriangles = new List<int>(triangleCount);
            for (int z = 0; z < voxelCount; ++z)
            {
                for (int y = 0; y < voxelCount; ++y)
                {
                    for (int x = 0; x < voxelCount; ++x)
                    {
                        List<int> bucket;
                        if (voxels.TryGetValue(Flatten(new Vector3Int(x, y, z), voxelCount), out bucket))
                            orderedTriangles.AddRange(bucket);
                    }
                }
            }

            int clusterCount = Mathf.CeilToInt(orderedTriangles.Count / (float)TrianglesPerCluster);
            Cluster[] clusters = new Cluster[clusterCount];
            Point[] points = new Point[clusterCount * VerticesPerCluster];
            int[] materials = new int[clusterCount * TrianglesPerCluster];

            for (int clusterIndex = 0; clusterIndex < clusterCount; ++clusterIndex)
            {
                int firstTriangle = clusterIndex * TrianglesPerCluster;
                int triangleInCluster = Mathf.Min(TrianglesPerCluster, orderedTriangles.Count - firstTriangle);
                Vector3 clusterMin = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
                Vector3 clusterMax = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

                for (int triangle = 0; triangle < triangleInCluster; ++triangle)
                {
                    int sourceTriangle = orderedTriangles[firstTriangle + triangle];
                    int sourceOffset = sourceTriangle * 3;
                    int targetOffset = clusterIndex * VerticesPerCluster + triangle * 3;

                    points[targetOffset] = sourcePoints[sourceOffset];
                    points[targetOffset + 1] = sourcePoints[sourceOffset + 1];
                    points[targetOffset + 2] = sourcePoints[sourceOffset + 2];
                    materials[clusterIndex * TrianglesPerCluster + triangle] =
                        sourceTriangleMaterials == null ? 0 : sourceTriangleMaterials[sourceTriangle];

                    ExpandBounds(ToVector3(points[targetOffset].vertex), ref clusterMin, ref clusterMax);
                    ExpandBounds(ToVector3(points[targetOffset + 1].vertex), ref clusterMin, ref clusterMax);
                    ExpandBounds(ToVector3(points[targetOffset + 2].vertex), ref clusterMin, ref clusterMax);
                }

                Vector3 clusterCenter = (clusterMin + clusterMax) * 0.5f;
                clusters[clusterIndex] = new Cluster
                {
                    position = clusterCenter,
                    extent = clusterMax - clusterCenter,
                    index = 0
                };
            }

            return new ClusterBakeResult
            {
                clusters = clusters,
                points = points,
                triangleMaterials = materials,
                localBounds = bounds
            };
        }

        private static int Flatten(Vector3Int coordinate, int size)
        {
            return coordinate.z * size * size + coordinate.y * size + coordinate.x;
        }

        private static Bounds CalculateBounds(List<Point> points)
        {
            Vector3 first = ToVector3(points[0].vertex);
            Bounds bounds = new Bounds(first, Vector3.zero);
            for (int i = 1; i < points.Count; ++i)
                bounds.Encapsulate(ToVector3(points[i].vertex));
            return bounds;
        }

        private static void ExpandBounds(Vector3 point, ref Vector3 min, ref Vector3 max)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }

        private static Vector3 ToVector3(Unity.Mathematics.float3 value)
        {
            return new Vector3(value.x, value.y, value.z);
        }
    }
}
#endif
