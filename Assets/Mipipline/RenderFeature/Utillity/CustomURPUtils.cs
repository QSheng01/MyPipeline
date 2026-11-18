// using SYPackages.GraphicsLoad.Loader;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SYPackages.Utillity
{
    public static class CustomURPUtils
    {
        public static bool IsRenderLayer(int layer, LayerMask rendermMask)
        {
            return (1 << layer & rendermMask.value) > 0;
        }

        public static Shader GetPostProcessShader(PostProcessData postProcessData, string shaderName)
        {
            Shader targetShader = null;
            //var customShaders = postProcessData.customRes.customPostShaders;
            //foreach (var shader in customShaders)
            //{
            //    if (shader.name == shaderName)
            //    {
            //        targetShader = shader;
            //        break;
            //    }
            //}

            return targetShader;
        }
        //
        // public static bool SupportRendererFeature<T>(this GraphicPipelineAssetLoader loader, int index = -1)
        //     where T : ScriptableRendererFeature
        // {
        //     var list = loader.GetSceneRendererFeatures(index);
        //     if (list == null)
        //     {
        //         return false;
        //     }
        //
        //     foreach (var rendererFeature in list)
        //     {
        //         if (rendererFeature is T)
        //         {
        //             return true;
        //             break;
        //         }
        //     }
        //
        //     return false;
        // }

        // public static T GetRendererFeature<T>(this GraphicPipelineAssetLoader loader, int index = -1)
        //     where T : ScriptableRendererFeature
        // {
        //     var list = loader.GetSceneRendererFeatures(index);
        //     if (list == null)
        //     {
        //         return null;
        //     }
        //
        //     foreach (var rendererFeature in list)
        //     {
        //         if (rendererFeature is T)
        //         {
        //             return rendererFeature as T;
        //             break;
        //         }
        //     }
        //
        //     return null;
        // }

        // public static bool SupportRendererFeature<T>(this GraphicPipelineAssetLoader loader, out T t, int index = -1)
        //     where T : ScriptableRendererFeature
        // {
        //     t = default(T);
        //     var list = loader.GetSceneRendererFeatures(index);
        //     if (list == null)
        //     {
        //         return false;
        //     }
        //
        //     foreach (var rendererFeature in list)
        //     {
        //         if (rendererFeature is T)
        //         {
        //             t = (T) rendererFeature;
        //             return true;
        //         }
        //     }
        //
        //     return false;
        // }

        // public static Shader LoadShader(PostProcessData data, string shadername)
        // {
        //     Shader shader = null;
        //     //var customShaders = data.customRes.customPostShaders;
        //     //foreach (var customShader in customShaders)
        //     //{
        //     //    if (customShader.name == shadername)
        //     //    {
        //     //    }
        //        //}
        //
        //        if (shader == null)
        //        {
        //            Debug.Log(string.Format("can not find {0}, please check!~", shadername));
        //        }
        //
        //        return shader;
        //    }         //        shader = customShader;
        //


        public static float ScaleHeightFromLayerDepth(float d)
        {
            // Exp[-d / H] = 0.001
            // -d / H = Log[0.001]
            // H = d / -Log[0.001]
            return d * 0.144765f;
        }

        public static float ExtinctionFromMeanFreePath(float meanFreePath)
        {
            return 1.0f / meanFreePath;
        }

        public static Vector3 ScatteringFromExtinctionAndAlbedo(float extinction, Vector3 albedo)
        {
            return extinction * albedo;
        }

        /// <summary>Get the aspect ratio of a projection matrix.</summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        internal static float ProjectionMatrixAspect(in Matrix4x4 matrix)
            => -matrix.m11 / matrix.m00;


        public static Matrix4x4 SetupCameraShaderConstants(Camera camera, float renderScale)
        {
            float scaledCameraWidth = (float) camera.pixelWidth * renderScale;
            float scaledCameraHeight = (float) camera.pixelHeight * renderScale;
            Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
            //Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
            //Matrix4x4 viewProjMatrix = projMatrix * viewMatrix;
            //Matrix4x4 invViewProjMatrix = Matrix4x4.Inverse(viewProjMatrix);
            //
            Vector4 resolution = new Vector4(scaledCameraWidth, scaledCameraHeight, 1 / scaledCameraWidth,
                1 / scaledCameraHeight);
            //var transform = Matrix4x4.Scale(new Vector3(-1.0f, -1.0f, -1.0f)) * invViewProjMatrix;
            // transform = transform * Matrix4x4.Scale(new Vector3(1.0f, -1.0f, 1.0f));
            // transform = transform * Matrix4x4.Translate(new Vector3(-1.0f, -1.0f, 0.0f));
            // transform = transform * Matrix4x4.Scale(new Vector3(2.0f * resolution.z, 2.0f * resolution.w, 1.0f));
            // Matrix4x4 pixelCoordToViewDirWS=transform.transpose;
            //

            var proj = camera.projectionMatrix;
            var view = camera.worldToCameraMatrix;
            var cameraPosition = camera.transform.position;

            var nonJitteredCameraProj = projMatrix;
            // var cameraProj = jitterProjectionMatrix
            //             ? GetJitteredProjectionMatrix(nonJitteredCameraProj)
            //             : nonJitteredCameraProj;
            var cameraProj = nonJitteredCameraProj;
            var gpuProj = GL.GetGPUProjectionMatrix(cameraProj, true);
            var gpuProjAspect = ProjectionMatrixAspect(gpuProj);

            //var gpuAspect=ProjectionMatrixAspect(projMatrix);

            //UpdateViewConstants(ref mainViewConstants, proj, view, cameraPosition, jitterProjectionMatrix, updatePreviousFrameConstants);

            Matrix4x4 pixelCoordToViewDirWS =
                ComputePixelCoordToWorldSpaceViewDirectionMatrix(camera, resolution, gpuProjAspect); //screenSize

            return pixelCoordToViewDirWS;

            Matrix4x4 ComputePixelCoordToWorldSpaceViewDirectionMatrix(Camera camera, Vector4 resolution,
                float aspect = -1)
            {
                // In XR mode, use a more generic matrix to account for asymmetry in the projection
                // var useGenericMatrix = xr.enabled;

                // // Asymmetry is also possible from a user-provided projection, so we must check for it too.
                // // Note however, that in case of physical camera, the lens shift term is the only source of
                // // asymmetry, and this is accounted for in the optimized path below. Additionally, Unity C++ will
                // // automatically disable physical camera when the projection is overridden by user.
                // useGenericMatrix |= HDUtils.IsProjectionMatrixAsymmetric(camera.projMatrix) && !camera.usePhysicalProperties;

                // if (useGenericMatrix)
                // {
                // Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
                // Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
                // Matrix4x4 viewProjMatrix = projMatrix * viewMatrix;
                // Matrix4x4 invViewProjMatrix = Matrix4x4.Inverse(viewProjMatrix);
                // var viewSpaceRasterTransform = new Matrix4x4(
                //     new Vector4(2.0f * resolution.z, 0.0f, 0.0f, -1.0f),
                //     new Vector4(0.0f, -2.0f * resolution.w, 0.0f, 1.0f),
                //     new Vector4(0.0f, 0.0f, 1.0f, 0.0f),
                //     new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

                // var transformT = invViewProjMatrix.transpose * Matrix4x4.Scale(new Vector3(-1.0f, -1.0f, -1.0f));
                // return viewSpaceRasterTransform * transformT;
                // }
                Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
                //Matrix4x4 projMatrix = camera.projectionMatrix;
                Matrix4x4 viewMatrix = camera.worldToCameraMatrix;

                float verticalFoV = camera.GetGateFittedFieldOfView() * Mathf.Deg2Rad;
                // if (!camera.usePhysicalProperties)   //??
                // {
                //     verticalFoV = Mathf.Atan(-1.0f / projMatrix[1, 1]) * 2;
                // }
                Vector2 lensShift = camera.GetGateFittedLensShift();

                return ComputePixelCoordToWorldSpaceViewDirectionMatrix2(verticalFoV, lensShift, resolution,
                    viewMatrix,
                    false, aspect, camera.orthographic);
            }

            static Matrix4x4 ComputePixelCoordToWorldSpaceViewDirectionMatrix2(float verticalFoV, Vector2 lensShift,
                Vector4 screenSize, Matrix4x4 worldToViewMatrix, bool renderToCubemap, float aspectRatio = -1,
                bool isOrthographic = false)
            {
                Matrix4x4 viewSpaceRasterTransform;

                if (isOrthographic)
                {
                    // For ortho cameras, project the skybox with no perspective
                    // the same way as builtin does (case 1264647)
                    viewSpaceRasterTransform = new Matrix4x4(
                        new Vector4(-2.0f * screenSize.z, 0.0f, 0.0f, 0.0f),
                        new Vector4(0.0f, -2.0f * screenSize.w, 0.0f, 0.0f),
                        new Vector4(1.0f, 1.0f, -1.0f, 0.0f),
                        new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
                }
                else
                {
                    // Compose the view space version first.
                    // V = -(X, Y, Z), s.t. Z = 1,
                    // X = (2x / resX - 1) * tan(vFoV / 2) * ar = x * [(2 / resX) * tan(vFoV / 2) * ar] + [-tan(vFoV / 2) * ar] = x * [-m00] + [-m20]
                    // Y = (2y / resY - 1) * tan(vFoV / 2)      = y * [(2 / resY) * tan(vFoV / 2)]      + [-tan(vFoV / 2)]      = y * [-m11] + [-m21]

                    aspectRatio = aspectRatio < 0 ? screenSize.x * screenSize.w : aspectRatio;
                    float tanHalfVertFoV = Mathf.Tan(0.5f * verticalFoV);

                    // Compose the matrix.
                    float m21 = (1.0f - 2.0f * lensShift.y) * tanHalfVertFoV;
                    float m11 = -2.0f * screenSize.w * tanHalfVertFoV;

                    float m20 = (1.0f - 2.0f * lensShift.x) * tanHalfVertFoV * aspectRatio;
                    float m00 = -2.0f * screenSize.z * tanHalfVertFoV * aspectRatio;

                    if (renderToCubemap)
                    {
                        // Flip Y.
                        m11 = -m11;
                        m21 = -m21;
                    }

                    viewSpaceRasterTransform = new Matrix4x4(new Vector4(m00, 0.0f, 0.0f, 0.0f),
                        new Vector4(0.0f, m11, 0.0f, 0.0f),
                        new Vector4(m20, m21, -1.0f, 0.0f),
                        new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                }

                // Remove the translation component.
                var homogeneousZero = new Vector4(0, 0, 0, 1);
                worldToViewMatrix.SetColumn(3, homogeneousZero);

                // Flip the Z to make the coordinate system left-handed.
                worldToViewMatrix.SetRow(2, -worldToViewMatrix.GetRow(2));

                // Transpose for HLSL.
                return Matrix4x4.Transpose(worldToViewMatrix.transpose * viewSpaceRasterTransform);
            }
        }
    }
}