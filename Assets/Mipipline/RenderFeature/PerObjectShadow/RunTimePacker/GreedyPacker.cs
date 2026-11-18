using System.Collections.Generic;
using UnityEngine;

namespace SYPackages.PerObjectShadow.RunTimePacker
{
	/// <summary>
	/// 使用贪婪算法的二维打包器
	/// </summary>
	public class GreedyPacker
	{
		/// <summary>
		/// 矩形空间
		/// </summary>
		private Rectangle root;

		internal static List<Row> unusedRows = new List<Row>(20);
		internal static List<Column> unusedColumns = new List<Column>(20);

		/// <summary>
		/// 构造函数
		/// </summary>
		public GreedyPacker(int w, int h)
		{
			root = new Row(0, 0, w, h);
			unusedRows.Clear();
			unusedColumns.Clear();
		}

		public void actualityWidth(ref int width,ref int height)
		{
			root.actualityWH(ref width,ref height);
		}

		/// <summary>
		/// 插入矩形
		/// </summary>
		public RectInt Insert(int w, int h)
		{
			var rect = root.Insert(w, h);
			if(rect == null)
				return new RectInt(-1, -1, -1, -1);

			return rect.position;
		}

		/// <summary>
		/// 移除矩形
		/// </summary>
		public void Remove(RectInt rect)
		{
			root.Remove(rect);
		}

		public void Clear()
		{
			root.Clear();
		}
	}
}