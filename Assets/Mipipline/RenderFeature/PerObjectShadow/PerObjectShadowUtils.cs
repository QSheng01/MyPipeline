using System;
using UnityEngine;

namespace SYPackages.PerObjectShadow
{
    public static class PerObjectShadowUtils
    {
        private static Vector3[] boundingBoxCorner = new Vector3[8];
        /// <summary>
        /// 把当前 shadowCaster 的包围盒转换到光源View空间下
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="_localBounds"></param>
        /// <returns></returns>
        public static Bounds TransformBounds(Matrix4x4 mat, Bounds _localBounds)
        {
            boundingBoxCorner[0] = new Vector3(_localBounds.min.x, _localBounds.min.y, _localBounds.min.z);
            boundingBoxCorner[1] = new Vector3(_localBounds.min.x, _localBounds.min.y, _localBounds.max.z);
            boundingBoxCorner[2] = new Vector3(_localBounds.min.x, _localBounds.max.y, _localBounds.min.z);
            boundingBoxCorner[3] = new Vector3(_localBounds.min.x, _localBounds.max.y, _localBounds.max.z);
            boundingBoxCorner[4] = new Vector3(_localBounds.max.x, _localBounds.min.y, _localBounds.min.z);
            boundingBoxCorner[5] = new Vector3(_localBounds.max.x, _localBounds.min.y, _localBounds.max.z);
            boundingBoxCorner[6] = new Vector3(_localBounds.max.x, _localBounds.max.y, _localBounds.min.z);
            boundingBoxCorner[7] = new Vector3(_localBounds.max.x, _localBounds.max.y, _localBounds.max.z);
            Bounds output = new Bounds(mat.MultiplyPoint(boundingBoxCorner[0]), new Vector3(0, 0, 0));
            for (int i = 1; i < boundingBoxCorner.Length; i++)
                output.Encapsulate(mat.MultiplyPoint(boundingBoxCorner[i]));
            return output;
        }
        /// <summary>
        /// 根据 shadowCaster 包围盒在 lightView 空间下的大小重新生成 灯光的Project矩阵
        /// </summary>
        /// <param name="lightview_bounds"></param>
        /// <param name="projection_mat"></param>
        /// <param name="z_dis"></param>
        /// <param name="near_plane"></param>
        /// <returns></returns>
        public static Matrix4x4 ProjMatFromBounds(Bounds lightview_bounds, Matrix4x4 projection_mat, out float z_dis, out float near_plane)
        {
            float width = 1.0f / projection_mat.m00;
            float height = 1.0f / projection_mat.m11;
            float projection_n = (projection_mat.m23 + 1) / projection_mat.m22;
            float projection_f = (projection_mat.m23 - 1) / projection_mat.m22;

            float l = Mathf.Max(-width, lightview_bounds.min.x);
            float r = Mathf.Min(width, lightview_bounds.max.x);
            float b = Mathf.Max(-height, lightview_bounds.min.y);
            float t = Mathf.Min(height, lightview_bounds.max.y);
            float f = -lightview_bounds.min.z;
            float n = -lightview_bounds.max.z;
            z_dis = f - n;
            near_plane = n;
            Matrix4x4 proj = Matrix4x4.Ortho(l, r, b, t, n, f);
            return proj;
        }
    }
}