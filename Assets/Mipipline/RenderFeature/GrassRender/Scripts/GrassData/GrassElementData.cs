using System;
using System.Collections.Generic;
using System.IO;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Grass
{
	public struct HWorldBounds
	{
		public float3 Center;
		public float3 Extents;

		public HWorldBounds(float3 center, float3 extents)
		{
			Center = center;
			Extents = extents;
		}
	}

	/// <summary>
	/// 草数据单元
	/// </summary>
	[Serializable]
	public struct GrassElementData : TreeElementData
	{
		static byte[] TempByte = new byte[4];

		public enum ColorType
		{
			Diff = 0,
			Normal = 1,
			LightMap = 2
		}

		[SerializeField]
		byte _objType;  //草的种类，草刷数组中的位置
		[SerializeField]
		Matrix4x4 _matrix;


		public int CullingSign { get; set; } //CullingGroup开启后 是否被剔除掉 1 剔除 0显示, 默认为0
		public int DrawCallInstanceID { get; set; } //渲染时所在drawcall的id
		public Vector4 Pos => _matrix.GetColumn(3);
		public byte Type { get { return _objType; } set { _objType = value; } }

		public Matrix4x4 Matrix
		{
			get { return _matrix; }
			set { _matrix = value;  }
		}
		public void InitData(Matrix4x4 matrix, Color diffColor, Color normal, Color lightmap, float height, float onlyScaleY, byte type)
		{
			_objType = type;
			_matrix = matrix;
			OnlyScaleY = onlyScaleY;
			SetColor(diffColor, ColorType.Diff);
			SetColor(normal, ColorType.Normal);
			SetColor(lightmap, ColorType.LightMap);
			HeightScale = height;
			CullingSign = 0;
		}

		public void InitData(Matrix4x4 matrix, float diffColor, float normal, float lightmap, float height, float onlyScaleY, byte type)
		{
			_objType = type;
			_matrix = matrix;
			OnlyScaleY = onlyScaleY;
			DiffColor = diffColor;
			NormalMapColor = normal;
			LightMapColor = lightmap;
			HeightScale = height;
			CullingSign = 0;
		}

		public void InitScatterData(Matrix4x4 matrix, Color styleColor0, Color styleColor1, Color terrainColor, byte type)
		{
			_objType = type;
			_matrix = matrix;
		}

		public void SetColor(Color color, ColorType colorType)
		{

			switch (colorType)
			{
				case ColorType.Diff:
					// DiffColor = ConvertColorToFloat(color, colorType);
					DiffColor = ConvertColorToFloatWY(color);
					break;
				case ColorType.LightMap:
					LightMapColor = ConvertColorToFloat(color, colorType);
					// LightMapColor = ConvertColorToFloatWY(color);
					break;
				case ColorType.Normal:
					// NormalMapColor = ConvertColorToFloat(color, colorType);
					NormalMapColor = ConvertColorToFloatWY(color);
					break;
			}
		}

		public float DiffColor
		{
			set
			{
				_matrix.m30 = value;
			}
			get
			{
				return _matrix.m30;
			}
		}

		public float NormalMapColor
		{
			set
			{
				_matrix.m31 = value;
			}
			get
			{
				return _matrix.m31;
			}
		}

		public float LightMapColor
		{
			set
			{
				_matrix.m32 = value;
			}
			get
			{
				return _matrix.m32;
			}
		}


		public float OnlyScaleY
		{

			get
			{
				return _matrix.m33;
			}

			set
			{
				_matrix.m33 = value;
			}
		}

		public float HeightScale
		{
			get
			{
				return _matrix.m11;
			}
			set
			{
				_matrix.m11 = value;
			}
		}

		public static float ConvertColorToFloat(Color color, ColorType colorType)
		{
			uint bitMask = 0x000000FF;
			TempByte[0] = (byte)(bitMask & ((uint)(color.r * 255)));
			TempByte[1] = (byte)(bitMask & ((uint)(color.g * 255)));
			TempByte[2] = (byte)(bitMask & ((uint)(color.b * 255)));
			if (colorType == ColorType.Diff)
			{
				TempByte[3] = (byte)(bitMask & ((uint)(color.a * 255)));
			}
			else
			{
				TempByte[3] = 0;
			}

			var value = BitConverter.ToSingle(TempByte, 0);
			return value;
		}

		/// <summary>
		/// WY 版本草的转换代码
		/// </summary>
		/// <param name="diffcColor"></param>
		/// <returns></returns>
		public static float ConvertColorToFloatWY(Color diffcColor)
		{
			uint bitMask = 0x000000FF;
			byte r = (byte)(bitMask & ((uint)(Mathf.Pow(diffcColor.r, 2.2f) * 255)));
			byte g = (byte)(bitMask & ((uint)(Mathf.Pow(diffcColor.g, 2.2f) * 255)));
			byte b = (byte)(bitMask & ((uint)(Mathf.Pow(diffcColor.b, 2.2f) * 255)));
			byte a = (byte)(bitMask & (uint)1);

			var value = BitConverter.ToSingle(new byte[] { r, g, b, a }, 0);
			return value;
		}

		public void SetDrawType(byte drawObjType)
		{
			_objType = drawObjType;
		}

		public void ChangedPosAndRotation(Vector3 dpos, Quaternion rotate)
		{
			var heightScale = HeightScale;
			var color = DiffColor;
			var lightMapColor = LightMapColor;
			var normalColor = NormalMapColor;
			var matrix = _matrix;

			matrix.m11 = 0;
			matrix.m30 = 0;
			matrix.m31 = 0;
			matrix.m32 = 0;
			matrix.m33 = 1;
			var mR = matrix.rotation;
			var mPos = matrix.GetColumn(3);

			var m = Matrix4x4.Rotate(rotate);
			mPos = m * mPos;
			var newPos = (Vector3)mPos + dpos;
			_matrix = Matrix4x4.TRS(newPos, mR, Vector3.one);
			HeightScale = heightScale;
			DiffColor = color;
			LightMapColor = lightMapColor;
			NormalMapColor = normalColor;
			//Debug.Log(_matrix.m11);
		}

		public void SetWorldPos(Vector3 wPos)
		{
			var matrix = _matrix;
			Vector4 mPos = matrix.GetColumn(3);
			mPos.x = wPos.x;
			mPos.y = wPos.y;
			mPos.z = wPos.z;
			matrix.SetColumn(3, mPos);
			_matrix = matrix;
		}

		/// <summary>
		/// 设置矩阵的up指向
		/// </summary>
		/// <param name="terrainNormal"></param>
		public void SetNormal(Vector3 terrainNormal)
		{
			Vector3 up = Vector3.up;
			Quaternion rotationYRandom = quaternion.RotateY(Random.Range(0, 360)); // 随机Y旋转
			Quaternion rotationUpToNormal = Quaternion.FromToRotation(up, terrainNormal); // Up朝向Normal
			Quaternion rotation = rotationUpToNormal * rotationYRandom;

			Vector4 pos = _matrix.GetColumn(3);
			// _matrix.SetColumn(3, -pos);//把矩阵以移动回原点
			// _matrix = _matrix * Matrix4x4.Rotate(rotation);//然后旋转
			// _matrix.SetColumn(3, pos);// 再移动回去
			_matrix = Matrix4x4.TRS(pos, rotation, Vector3.one);
		}

		public void SetHeightScale(float scale) {
			var heightScale = HeightScale;
			HeightScale = heightScale * scale;
		}

		public void WriteToStream(FileStream stream)
		{
			byte[] bytes = BitConverter.GetBytes(Pos.x);
			stream.Write(bytes, 0, bytes.Length);

			bytes = BitConverter.GetBytes(Pos.y);
			stream.Write(bytes, 0, bytes.Length);

			bytes = BitConverter.GetBytes(Pos.z);
			stream.Write(bytes, 0, bytes.Length);

			bytes = BitConverter.GetBytes(HeightScale);
			stream.Write(bytes, 0, bytes.Length);

			bytes = BitConverter.GetBytes(DiffColor);
			stream.Write(bytes, 0, bytes.Length);

			bytes = BitConverter.GetBytes(NormalMapColor);
			stream.Write(bytes, 0, bytes.Length);

			bytes = BitConverter.GetBytes(LightMapColor);
			stream.Write(bytes, 0, bytes.Length);

			var matrix = _matrix;
			matrix.m11 = 1;
			matrix.m30 = 0;
			matrix.m31 = 0;
			matrix.m32 = 0;

			if (!matrix.ValidTRS())
			{
				bytes = BitConverter.GetBytes(0.0f);
                stream.Write(bytes, 0, bytes.Length);
                bytes = BitConverter.GetBytes(0.0f);
                stream.Write(bytes, 0, bytes.Length);
                bytes = BitConverter.GetBytes(0.0f);
                stream.Write(bytes, 0, bytes.Length);
			}
			else
			{
				var rota = matrix.rotation;
				var angles = rota.eulerAngles;
				bytes = BitConverter.GetBytes(angles.x);
                stream.Write(bytes, 0, bytes.Length);
                bytes = BitConverter.GetBytes(angles.y);
                stream.Write(bytes, 0, bytes.Length);
                bytes = BitConverter.GetBytes(angles.z);
                stream.Write(bytes, 0, bytes.Length);
			}


			byte yScale = (byte)(OnlyScaleY > 0 ? 1 : 0);
			stream.WriteByte(yScale);
		}
		public override string ToString()
		{
			return string.Format("type = {0} , pos = {1} ，nodeIndex = {2}", _objType, Pos);
		}

	}

	[BurstCompile]
	public struct GrassBlockBinding
	{
		public byte Lod;
		public byte Type;
		public float Dist;
		public int BlockID;
		public int Start;
		public int Length;
		//public int2 CellID;
		public HWorldBounds Bounds;

		public void InitData(byte type, /*int2 cellId,*/ int blockId, int start, int length, HWorldBounds bounds)
		{
			//CellID = cellId;
			BlockID = blockId;
			Type = type;
			Start = start;
			Length = length;
			Bounds = bounds;
		}

		public void ResetOffset(int offset)
		{
			Start += offset;
		}

		public int Sort(GrassBlockBinding other)
		{
			if (Lod == 255)
				return 1;

			if (Type > other.Type)
				return 1;

			if (Type < other.Type)
				return -1;

			if (Dist >= other.Dist)
				return 1;

			return -1;
		}
	}

	public class TempGrassDataBinding
	{
		int _meshID;
		int2 _cellID;
		Vector3 _center;
		Dictionary<byte, List<GrassElementData>> _dataDic;
		int _meshSize2;

		public void Init(int2 cellPos, int meshId, int meshSize2, Vector3 center)
		{
			_meshID = meshId;
			_cellID = cellPos;
			_meshSize2 = meshSize2;
			_center = center;
		}

		public void AddGrassData(byte type, List<GrassElementData> datas)
		{
			if (_dataDic == null)
				_dataDic = new Dictionary<byte, List<GrassElementData>>();

			if (_dataDic.TryGetValue(type, out List<GrassElementData> mdatas))
			{
				mdatas.AddRange(datas);
			}
			else
			{
				_dataDic.Add(type, datas);
			}
		}
	}

	public class TempTreeNode : IComparable<TempTreeNode>
	{
		List<GrassElementData> _datas;
		TempTreeNode[] _childNodes;
		Bounds _bounds;
		int _elementCount;
		byte _type;
		public byte Type => _type;
		public Bounds mBounds => _bounds;
		public int mElementCount => _elementCount;
		public List<GrassElementData> mDatas => _datas;
		public static TempTreeNode CreateTree(byte type, Vector3 center, int size2, int minSize2)
		{
			TempTreeNode node = new TempTreeNode();
			node.InitData(type, center, size2, minSize2);
			return node;
		}

		private void InitData(byte type, Vector3 center, int size2, int minSize2)
		{
			_type = type;
			var size = 1 << size2;
			_bounds = new Bounds(center, new Vector3(size, 1, size));
			if (size2 > minSize2)
			{
				_childNodes = new TempTreeNode[4];
				for (int i = 0; i < 4; i++)
				{
					int signX = 1;
					int signY = 1;
					if (i == 1 || i == 2)
					{
						signX = -1;
					}
					if (i == 2 || i == 3)
					{
						signY = -1;
					}
					var childCenter = center + new Vector3(size / 4.0f * signX, 0, size / 4.0f * signY);
					_childNodes[i] = CreateTree(type, childCenter, size2 - 1, minSize2);
				}
			}
			else
			{
				_datas = new List<GrassElementData>();
			}
		}

		public int AddData(IEnumerable<GrassElementData> datas,byte type)
		{
			foreach (var data in datas)
			{
				AddData(data, type);
			}
			return _elementCount;
		}

		public bool AddData(GrassElementData data, byte type)
		{
			if (data.Type != type)
			{
				return false;
			}

			var pos = new Vector3(data.Pos.x, 0, data.Pos.z);
			if (_bounds.Contains(pos))
			{
				_elementCount++;
				if (_childNodes != null)
				{
					for (int i = 0; i < 4; i++)
					{
						if (_childNodes[i].AddData(data,type))
							return true;
					}
				}
				else
				{
					//Debug.Log("AddData success");
					_datas.Add(data);
					return true;
				}

			}

			return false;
		}

		public void ResetType(byte type)
		{
			if (type == _type)
				return;

			_type = type;
			if (_childNodes != null)
			{
				foreach (var node in _childNodes)
				{
					node.ResetType(type);
				}
			}
		}

		public HWorldBounds GetCullingBounds(float height)
		{
			HWorldBounds bounds = new HWorldBounds();

			if (_datas == null)
				return bounds;

			float minY = float.MaxValue;
			float maxY = float.MinValue;
			foreach (var data in _datas)
			{
				if (data.Pos.y < minY)
					minY = data.Pos.y;
				var h = data.Pos.y + height * data.Matrix.m11;
				if (h > maxY)
				{
					maxY = h;
				}
			}
			var rtBound = new Bounds();
			rtBound.SetMinMax(new Vector3(_bounds.min.x, minY, _bounds.min.z), new Vector3(_bounds.max.x, maxY, _bounds.max.z));
			bounds = new HWorldBounds(rtBound.center, rtBound.extents);
			return bounds;
		}

		public int CompareTo(TempTreeNode other)
		{
			if (other.Type != Type)
				return Type - other.Type;

			if (other._bounds.center.z > _bounds.center.z)
				return -1;
			else if (other._bounds.center.z < _bounds.center.z)
				return 1;
			else
			{
				if (other._bounds.center.x > _bounds.center.x)
					return -1;
				else
					return 1;
			}
		}

		/// <summary>
		/// 获取叶子节点，输出到 nodes
		/// </summary>
		/// <param name="nodes"> 输出的数据 </param>
		public void GetAllLeafNode(List<TempTreeNode> nodes)
		{
			//List<TempTreeNode> nodes = new List<TempTreeNode>();
			if (nodes == null)
				return;

			if (_childNodes != null)
			{
				foreach (var child in _childNodes)
				{
					child.GetAllLeafNode(nodes);
				}
			}
			else
			{
				nodes.Add(this);
			}
		}
	}
}