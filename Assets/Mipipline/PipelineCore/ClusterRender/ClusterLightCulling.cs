using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MPipeline
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ClusterLightData
    {
        public Vector4 positionRange;
        public Vector4 color;
        public Vector4 directionType;
        public Vector4 spotParams;
    }

    /// <summary>
    /// Builds a fixed 32x16x64 view-space light grid for the URP Cluster draw.
    /// The list is conservative: spot lights are assigned by their range sphere
    /// and receive their cone attenuation in the material shader.
    /// </summary>
    public sealed class ClusterLightCulling : IDisposable
    {
        public const int GridX = 32;
        public const int GridY = 16;
        public const int GridZ = 64;
        internal const int MaxLightsPerCluster = 128;

        private static readonly int ClusterLightsId = Shader.PropertyToID("_ClusterLights");
        private static readonly int ClusterLightIndicesId = Shader.PropertyToID("_ClusterLightIndices");
        private static readonly int ClusterViewMatrixId = Shader.PropertyToID("_ClusterViewMatrix");
        private static readonly int ClusterCameraParamsId = Shader.PropertyToID("_ClusterCameraParams");
        private static readonly int ClusterGridParamsId = Shader.PropertyToID("_ClusterGridParams");
        private static readonly int ClusterLightCountId = Shader.PropertyToID("_ClusterLightCount");

        private readonly ComputeShader shader;
        private readonly int kernel;
        private ComputeBuffer lightsBuffer;
        private ComputeBuffer indicesBuffer;
        private ClusterLightData[] lightData = Array.Empty<ClusterLightData>();
        private int uploadedLightCount;

        internal ClusterLightCulling(ComputeShader shader)
        {
            this.shader = shader;
            kernel = shader != null && shader.HasKernel("CullLights")
                ? shader.FindKernel("CullLights")
                : -1;
        }

        internal ComputeBuffer LightsBuffer => lightsBuffer;
        internal ComputeBuffer IndicesBuffer => indicesBuffer;
        internal int LightCount => uploadedLightCount;
        internal Vector4 GridParams => new Vector4(GridX, GridY, GridZ, MaxLightsPerCluster);

        internal bool Prepare(
            CommandBuffer command,
            Camera camera,
            NativeArray<VisibleLight> visibleLights,
            int mainLightIndex)
        {
            if (command == null || camera == null || shader == null || kernel < 0
                || !SystemInfo.supportsComputeShaders)
                return false;

            int count = 0;
            for (int i = 0; i < visibleLights.Length; ++i)
            {
                if (i == mainLightIndex)
                    continue;
                Light light = visibleLights[i].light;
                if (light == null || !light.isActiveAndEnabled)
                    continue;
                if (light.type != LightType.Point && light.type != LightType.Spot)
                    continue;
                count++;
            }

            EnsureBuffers(Mathf.Max(1, count));
            uploadedLightCount = count;
            if (count > 0)
            {
                int writeIndex = 0;
                for (int i = 0; i < visibleLights.Length; ++i)
                {
                    if (i == mainLightIndex)
                        continue;
                    Light light = visibleLights[i].light;
                    if (light == null || !light.isActiveAndEnabled
                        || (light.type != LightType.Point && light.type != LightType.Spot))
                        continue;

                    VisibleLight visibleLight = visibleLights[i];
                    Vector3 position = visibleLight.localToWorldMatrix.GetColumn(3);
                    Vector3 direction = visibleLight.localToWorldMatrix.GetColumn(2);
                    float outerCos = light.type == LightType.Spot
                        ? Mathf.Cos(0.5f * light.spotAngle * Mathf.Deg2Rad)
                        : -1f;
                    float innerCos = light.type == LightType.Spot
                        ? Mathf.Cos(0.5f * light.innerSpotAngle * Mathf.Deg2Rad)
                        : 1f;

                    lightData[writeIndex++] = new ClusterLightData
                    {
                        positionRange = new Vector4(position.x, position.y, position.z, Mathf.Max(light.range, 0.001f)),
                        color = visibleLight.finalColor,
                        directionType = new Vector4(direction.x, direction.y, direction.z, light.type == LightType.Spot ? 1f : 0f),
                        spotParams = new Vector4(outerCos, innerCos, 0f, 0f)
                    };
                }
                lightsBuffer.SetData(lightData, 0, 0, count);
            }

            float farClip = Mathf.Max(camera.nearClipPlane + 0.001f, camera.farClipPlane);
            Matrix4x4 projection = camera.projectionMatrix;
            float inverseProjectionX = Mathf.Abs(projection.m00) > 0.0001f ? 1f / Mathf.Abs(projection.m00) : 1f;
            float inverseProjectionY = Mathf.Abs(projection.m11) > 0.0001f ? 1f / Mathf.Abs(projection.m11) : 1f;

            command.SetComputeBufferParam(shader, kernel, ClusterLightsId, lightsBuffer);
            command.SetComputeBufferParam(shader, kernel, ClusterLightIndicesId, indicesBuffer);
            command.SetComputeMatrixParam(shader, ClusterViewMatrixId, camera.worldToCameraMatrix);
            Vector4 cameraParams = new Vector4(
                camera.nearClipPlane,
                farClip,
                inverseProjectionX,
                inverseProjectionY);
            command.SetComputeVectorParam(shader, ClusterCameraParamsId, cameraParams);
            command.SetGlobalVector(ClusterCameraParamsId, cameraParams);
            command.SetComputeIntParams(shader, ClusterGridParamsId, GridX, GridY, GridZ, MaxLightsPerCluster);
            command.SetComputeIntParam(shader, ClusterLightCountId, count);
            command.DispatchCompute(shader, kernel, (GridX * GridY * GridZ + 63) / 64, 1, 1);
            return true;
        }

        private void EnsureBuffers(int requiredLightCount)
        {
            int requiredLightCapacity = Mathf.Max(1, requiredLightCount);
            if (lightsBuffer == null || lightsBuffer.count < requiredLightCapacity)
            {
                lightsBuffer?.Dispose();
                lightsBuffer = new ComputeBuffer(requiredLightCapacity, Marshal.SizeOf<ClusterLightData>(), ComputeBufferType.Structured);
                lightData = new ClusterLightData[requiredLightCapacity];
            }

            int requiredIndexCount = GridX * GridY * GridZ * (MaxLightsPerCluster + 1);
            if (indicesBuffer == null || indicesBuffer.count != requiredIndexCount)
            {
                indicesBuffer?.Dispose();
                indicesBuffer = new ComputeBuffer(requiredIndexCount, sizeof(uint), ComputeBufferType.Structured);
            }
        }

        public void Dispose()
        {
            lightsBuffer?.Dispose();
            indicesBuffer?.Dispose();
            lightsBuffer = null;
            indicesBuffer = null;
            lightData = Array.Empty<ClusterLightData>();
            uploadedLightCount = 0;
        }
    }
}
