using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SYPackages.PerObjectShadow.RunTimePacker
{
    /// <summary>
    /// 宽度固定的矩形(列)
    /// </summary>
    public class Column : Rectangle
    {
        /// <summary>
        /// 子行
        /// </summary>
        private List<Row> rows;

        /// <summary>
        /// 剩余可分配高度
        /// </summary>
        private int heightAvailable
        {
            get
            {
                if(used)
                    return 0;
                if (rows == null)
                    return position.height;

                int height = position.height;
                foreach (var row in rows)
                    height -= row.position.height;

                return height;
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public Column(int x, int y, int w, int h) : base(x, y, w, h)
        {
        }


        /// <summary>
        /// 插入矩形
        /// </summary>
        public override Rectangle Insert(int width, int height)
        {
            // 已被占用，无法分配
            if(used)
                return null;

            // 宽度不足，无法分配
            if (width > position.width)
                return null;

            // 尺寸完全匹配，改为占用状态
            if (rows == null && position.width == width && position.height == height)
            {
                used = true;
                return this;
            }

            if (rows == null)
                rows = new List<Row>();

            // 尝试从已有子行中分配
            foreach (var row in rows)
            {
                var rect = row.Insert(width, height);
                if (rect != null)
                    return rect;
            }

            // 高度超过可用高度，分配失败
            if (height > heightAvailable)
                return null;

            // 生成新行
            Row newRow = null;
            if (GreedyPacker.unusedRows.Count > 0)
            {
                newRow = GreedyPacker.unusedRows[GreedyPacker.unusedRows.Count - 1];
                newRow.ReSet(position.x, position.y + position.height - heightAvailable, position.width, height);
                GreedyPacker.unusedRows.RemoveAt(GreedyPacker.unusedRows.Count - 1);
            }
            else
            {
                // 生成新行
                newRow = new Row(position.x, position.y + position.height - heightAvailable, position.width, height);
            }

            rows.Add(newRow);
            rows.Sort((lhs, rhs) => { return lhs.position.height.CompareTo(rhs.position.height); });
            // 从新行中分配
            return newRow.Insert(width, height);
        }

        /// <summary>
        /// 移除矩形
        /// </summary>
        public override bool Remove(RectInt rect)
        {
            if (!Contains(rect))
                return false;
            if (rows == null || rows.Count < 1)
            {
                if (Equals(rect))
                {
                    used = false;
                    GreedyPacker.unusedColumns.Add(this);
                    return true;
                }
            }
            if (rows != null)
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    if (row.Remove(rect))
                    {
                        rows.RemoveAt(i);
                        return true;
                    }
                }
            }
            return false;
        }

        public override void Clear()
        {
            if (rows == null)
            {
                used = false;
                return;
            }
            foreach (var row in rows)
            {
                row.Clear();
                GreedyPacker.unusedRows.Add(row);
            }
            rows.Clear();
        }

        public override void actualityWH(ref int width, ref int height)
        {
            if (rows == null || rows.Count < 1)
            {
                width = width < position.x + position.width ? position.x + position.width : width;
                height = height < position.y + position.height ? position.y + position.height : height;
            }
            else
            {
                foreach (var column in rows)
                {
                    column.actualityWH(ref width, ref height);
                }
            }
        }
    }
}