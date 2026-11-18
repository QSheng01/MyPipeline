using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SYPackages.PerObjectShadow.RunTimePacker
{
    /// <summary>
    /// 高度固定的矩形(行)
    /// </summary>
    public class Row : Rectangle
    {
        private List<Column> columns;

        /// <summary>
        /// 剩余可分配宽度
        /// </summary>
        private int widthAvailable
        {
            get
            {
                if (used)
                    return 0;
                if (columns == null)
                    return position.width;

                int width = position.width;
                foreach (var column in columns)
                    width -= column.position.width;

                return width;
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public Row(int x, int y, int w, int h) : base(x, y, w, h)
        {
        }

        /// <summary>
        /// 插入矩形
        /// </summary>
        public override Rectangle Insert(int width, int height)
        {
            // 已被占用，无法分配
            if (used)
                return null;
            // 高度不足，无法分配
            if (height > position.height)
                return null;

            // 尺寸完全匹配，改为占用状态
            if (columns == null && width == position.width && height == position.height)
            {
                used = true;
                return this;
            }

            if (columns == null)
                columns = new List<Column>();

            // 尝试从已有子列中分配
            foreach (var column in columns)
            {
                var rect = column.Insert(width, height);
                if (rect != null)
                    return rect;
            }

            // 宽度超过可用宽度，分配失败
            if (width > widthAvailable)
                return null;

            Column newColumn = null;
            if (GreedyPacker.unusedColumns.Count > 0)
            {
                newColumn = GreedyPacker.unusedColumns[GreedyPacker.unusedColumns.Count - 1];
                newColumn.ReSet(position.x + position.width - widthAvailable, position.y, width,
                    position.height);
                GreedyPacker.unusedColumns.RemoveAt(GreedyPacker.unusedColumns.Count - 1);
            }
            else
            {
                // 生成新列
                newColumn = new Column(position.x + position.width - widthAvailable, position.y, width,
                    position.height);
            }

            columns.Add(newColumn);
            // 从新列中分配
            return newColumn.Insert(width, height);
        }

        /// <summary>
        /// 移除矩形
        /// </summary>
        public override bool Remove(RectInt rect)
        {
            if (!Contains(rect))
                return false;
            if (columns == null || columns.Count < 1)
            {
                if (Equals(rect))
                {
                    used = false;
                    GreedyPacker.unusedRows.Add(this);
                    return true;
                }
            }

            if (columns != null)
            {
                for (int i = 0; i < columns.Count; i++)
                {
                    var column = columns[i];
                    if (column.Remove(rect))
                    {
                        columns.RemoveAt(i);
                        return true;
                    }
                }
            }

            return false;
        }

        public override void Clear()
        {
            if (columns == null)
            {
                used = false;
                return;
            }

            foreach (var column in columns)
            {
                column.Clear();
                GreedyPacker.unusedColumns.Add(column);
            }

            columns.Clear();
        }

        public override void actualityWH(ref int width, ref int height)
        {
            if (columns == null || columns.Count < 1)
            {
                width = width < position.x + position.width ? position.x + position.width : width;
                height = height < position.y + position.height ? position.y + position.height : height;
            }
            else
            {
                foreach (var column in columns)
                {
                    column.actualityWH(ref width, ref height);
                }
            }
        }
    }
}