using System.Collections.Generic;
using UnityEngine;

namespace Illusion.Rendering.PRTGI
{
    // Matches the source pyramid counts and camera-window selection. Publication is rebuilt per camera.
    internal sealed class PRTCascadeLayout
    {
        private readonly PRTProbeGrid _grid;
        private readonly Vector3Int[] _levels;
        private readonly Vector3Int[] _minimum;
        private readonly Vector3Int[] _window;
        private readonly Vector4[] _minimumVectors = new Vector4[4];
        private readonly Vector4[] _windowVectors = new Vector4[4];
        private readonly Vector4[] _levelVectors = new Vector4[4];

        public Vector3Int Slots { get; }
        public int CascadeCount => _levels.Length;
        public Vector3Int LevelCount(int level) => _levels[level];
        public Vector3Int Minimum(int level) => _minimum[level];
        public Vector3Int WindowCount(int level) => _window[level];
        public Vector4[] MinimumVectors => _minimumVectors;
        public Vector4[] WindowVectors => _windowVectors;
        public Vector4[] LevelVectors => _levelVectors;
        public Vector3 Center { get; private set; }
        public int TextureDepth => checked(Slots.y * 9 * CascadeCount);

        public PRTCascadeLayout(PRTProbeGrid grid, Vector3Int requestedSlots, int requestedCascades)
        {
            _grid = grid;
            Slots = new Vector3Int(Mathf.Clamp(requestedSlots.x, 1, grid.count.x),
                Mathf.Clamp(requestedSlots.y, 1, grid.count.y), Mathf.Clamp(requestedSlots.z, 1, grid.count.z));
            var levels = new List<Vector3Int> { grid.count };
            while (levels.Count < Mathf.Clamp(requestedCascades, 1, 4) &&
                   Vector3Int.Min(Slots, levels[levels.Count - 1]) != levels[levels.Count - 1])
            {
                Vector3Int fine = levels[levels.Count - 1];
                var coarse = new Vector3Int(Coarse(fine.x), Coarse(fine.y), Coarse(fine.z));
                if (coarse == fine) break;
                levels.Add(coarse);
            }
            _levels = levels.ToArray();
            _minimum = new Vector3Int[_levels.Length];
            _window = new Vector3Int[_levels.Length];
            for (int level = 0; level < _levels.Length; level++)
            {
                _window[level] = Vector3Int.Min(Slots, _levels[level]);
                _levelVectors[level] = new Vector4(_levels[level].x, _levels[level].y, _levels[level].z, 0);
            }
        }

        public static int Coarse(int fine) => fine == 1 ? 1 : fine / 2 + 1;

        public void Recenter(Vector3 cameraPosition)
        {
            Center = cameraPosition;
            Vector3 node = (cameraPosition - _grid.origin) / _grid.spacing - (Vector3)_grid.min;
            for (int level = 0; level < CascadeCount; level++)
            {
                Vector3 coordinate = node / (1 << level);
                Vector3Int size = _window[level], domain = _levels[level];
                var minimum = new Vector3Int(
                    Mathf.Clamp(Mathf.RoundToInt(coordinate.x) - size.x / 2, 0, domain.x - size.x),
                    Mathf.Clamp(Mathf.RoundToInt(coordinate.y) - size.y / 2, 0, domain.y - size.y),
                    Mathf.Clamp(Mathf.RoundToInt(coordinate.z) - size.z / 2, 0, domain.z - size.z));
                _minimum[level] = minimum;
                _minimumVectors[level] = new Vector4(minimum.x, minimum.y, minimum.z, 0);
                _windowVectors[level] = new Vector4(size.x, size.y, size.z, 0);
            }
        }
    }
}
