using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Illusion.Rendering.PRTGI
{
    // Freeze directional environment radiance until the skybox, ambient probe or sun changes.
    internal sealed class PRTEnvironmentCapture : IDisposable
    {
        private const int CubeSize = 64;
        private Material _skyMaterial;
        private Mesh _skyMesh;
        private RenderTexture _skyCube;
        private Cubemap _ambientCube;
        private int _hash;
        private bool _hasHash;
        internal Texture Cube { get; private set; }
        internal float Intensity { get; private set; }

        internal bool Refresh(CommandBuffer cmd, Matrix4x4 restoreView, Matrix4x4 restoreProjection)
        {
            var skybox = RenderSettings.skybox;
            bool useSkybox = RenderSettings.ambientMode == AmbientMode.Skybox && skybox;
            int hash = ComputeHash(skybox);
            if (_hasHash && hash == _hash && Cube) return false;
            _hash = hash;
            _hasHash = true;
            Intensity = useSkybox ? RenderSettings.ambientIntensity : 1f;
            if (useSkybox)
            {
                if (!_skyCube)
                {
                    _skyCube = new RenderTexture(CubeSize, CubeSize, 0, RenderTextureFormat.ARGBHalf)
                    {
                        name = "PRT environment radiance", dimension = TextureDimension.Cube,
                        filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp,
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    _skyCube.Create();
                }
                if (!_skyMesh) _skyMesh = CreateSkyMesh();
                if (_skyMaterial)
                {
                    Destroy(_skyMaterial);
                    _skyMaterial = null;
                }
                _skyMaterial = new Material(skybox) { hideFlags = HideFlags.HideAndDontSave };
                if (RenderSettings.sun &&
                    (_skyMaterial.IsKeywordEnabled("_SUNDISK_SIMPLE") || _skyMaterial.IsKeywordEnabled("_SUNDISK_HIGH_QUALITY")))
                {
                    _skyMaterial.DisableKeyword("_SUNDISK_SIMPLE");
                    _skyMaterial.DisableKeyword("_SUNDISK_HIGH_QUALITY");
                    _skyMaterial.EnableKeyword("_SUNDISK_NONE");
                }
                Vector4 sunDirection = RenderSettings.sun ? (Vector4)(-RenderSettings.sun.transform.forward) : Vector3.up;
                sunDirection.w = 0;
                Color sunColor = RenderSettings.sun ? RenderSettings.sun.color.linear * RenderSettings.sun.intensity : Color.white;
                var oldDirection = Shader.GetGlobalVector("_WorldSpaceLightPos0");
                var oldColor = Shader.GetGlobalVector("_LightColor0");
                cmd.SetGlobalVector("_WorldSpaceLightPos0", sunDirection);
                cmd.SetGlobalColor("_LightColor0", sunColor);
                Matrix4x4 projection = Matrix4x4.Scale(new Vector3(1, -1, 1)) * Matrix4x4.Perspective(90, 1, 0.1f, 10f);
                for (int face = 0; face < 6; face++)
                {
                    cmd.SetRenderTarget(_skyCube, 0, (CubemapFace)face);
                    cmd.ClearRenderTarget(false, true, Color.clear);
                    cmd.SetViewProjectionMatrices(FaceView(face), projection);
                    cmd.DrawMesh(_skyMesh, Matrix4x4.identity, _skyMaterial, 0, 0);
                }
                cmd.SetViewProjectionMatrices(restoreView, restoreProjection);
                cmd.SetGlobalVector("_WorldSpaceLightPos0", oldDirection);
                cmd.SetGlobalVector("_LightColor0", oldColor);
                Cube = _skyCube;
            }
            else
            {
                if (!_ambientCube)
                    _ambientCube = new Cubemap(CubeSize, TextureFormat.RGBAHalf, false)
                    {
                        name = "PRT ambient radiance", filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave
                    };
                FillAmbientCube();
                Cube = _ambientCube;
            }
            return true;
        }

        private static int ComputeHash(Material skybox)
        {
            var hash = new HashCode();
            hash.Add(RenderSettings.ambientMode);
            hash.Add(RenderSettings.ambientIntensity);
            hash.Add(RenderSettings.ambientLight);
            hash.Add(skybox ? skybox.ComputeCRC() : 0);
            var probe = RenderSettings.ambientProbe;
            for (int channel = 0; channel < 3; channel++)
                for (int coefficient = 0; coefficient < 9; coefficient++)
                    hash.Add(probe[channel, coefficient]);
            Light sun = RenderSettings.sun;
            if (sun)
            {
                hash.Add(sun.transform.rotation);
                hash.Add(sun.color);
                hash.Add(sun.intensity);
                hash.Add(sun.useColorTemperature);
                hash.Add(sun.colorTemperature);
            }
            return hash.ToHashCode();
        }

        private void FillAmbientCube()
        {
            SphericalHarmonicsL2 probe = RenderSettings.ambientProbe;
            for (int channel = 0; channel < 3; channel++)
                for (int coefficient = 1; coefficient < 9; coefficient++)
                    probe[channel, coefficient] *= coefficient < 4 ? 1.5f : 4f;
            bool flat = RenderSettings.ambientMode == AmbientMode.Flat;
            Color flatColor = RenderSettings.ambientLight.linear;
            var directions = new Vector3[CubeSize * CubeSize];
            var pixels = new Color[directions.Length];
            for (int face = 0; face < 6; face++)
            {
                for (int y = 0; y < CubeSize; y++)
                    for (int x = 0; x < CubeSize; x++)
                        directions[y * CubeSize + x] = Direction(face, (x + 0.5f) / CubeSize, (y + 0.5f) / CubeSize);
                if (!flat) probe.Evaluate(directions, pixels);
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color value = flat ? flatColor : pixels[i];
                    pixels[i] = new Color(Mathf.Max(0f, value.r), Mathf.Max(0f, value.g), Mathf.Max(0f, value.b), 1f);
                }
                _ambientCube.SetPixels(pixels, (CubemapFace)face);
            }
            _ambientCube.Apply(false, false);
        }

        private static Vector3 Direction(int face, float u, float v)
        {
            float x = u * 2f - 1f, y = v * 2f - 1f;
            switch (face)
            {
                case 0: return new Vector3(1f, -y, -x).normalized;
                case 1: return new Vector3(-1f, -y, x).normalized;
                case 2: return new Vector3(x, 1f, y).normalized;
                case 3: return new Vector3(x, -1f, -y).normalized;
                case 4: return new Vector3(x, -y, 1f).normalized;
                default: return new Vector3(-x, -y, -1f).normalized;
            }
        }

        private static Matrix4x4 FaceView(int face)
        {
            Vector3 forward = face == 0 ? Vector3.right : face == 1 ? Vector3.left :
                face == 2 ? Vector3.up : face == 3 ? Vector3.down : face == 4 ? Vector3.forward : Vector3.back;
            Vector3 up = face == 2 ? Vector3.back : face == 3 ? Vector3.forward : Vector3.up;
            return Matrix4x4.Scale(new Vector3(1, 1, -1)) * Matrix4x4.LookAt(Vector3.zero, forward, up).inverse;
        }

        private static Mesh CreateSkyMesh()
        {
            var vertices = new Vector3[8];
            for (int i = 0; i < 8; i++)
                vertices[i] = new Vector3((i & 1) != 0 ? 1 : -1, (i & 2) != 0 ? 1 : -1, (i & 4) != 0 ? 1 : -1);
            var mesh = new Mesh { name = "PRT environment cube", hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, vertices);
            mesh.SetTriangles(new[] { 0, 2, 3, 0, 3, 1, 4, 5, 7, 4, 7, 6, 0, 1, 5, 0, 5, 4,
                2, 6, 7, 2, 7, 3, 0, 4, 6, 0, 6, 2, 1, 3, 7, 1, 7, 5 }, 0);
            return mesh;
        }

        public void Dispose()
        {
            if (_skyCube) { _skyCube.Release(); Destroy(_skyCube); }
            if (_ambientCube) Destroy(_ambientCube);
            if (_skyMesh) Destroy(_skyMesh);
            if (_skyMaterial) Destroy(_skyMaterial);
            Cube = null;
        }

        private static void Destroy(UnityEngine.Object value)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
