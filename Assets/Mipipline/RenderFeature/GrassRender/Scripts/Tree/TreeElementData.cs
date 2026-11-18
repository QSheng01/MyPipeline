using UnityEngine;

namespace Grass
{
    public interface TreeElementData
    {
        public Vector4 Pos { get; }
        public Matrix4x4 Matrix { get; }
        public byte Type { get; set; }  //元数据类型
        public float HeightScale { get; set; }
        public float DiffColor { get; set; }
        public float LightMapColor { get; set; }
        public float NormalMapColor { get; set; }

        public void SetColor(Color color, GrassElementData.ColorType layer);

    }
}