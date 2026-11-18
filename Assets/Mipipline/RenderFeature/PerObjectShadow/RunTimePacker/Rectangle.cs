using System;
using System.Collections.Generic;
using UnityEngine;

namespace SYPackages.PerObjectShadow.RunTimePacker
{
    public abstract class Rectangle : IEquatable<RectInt>
    {
        /// <summary>
        /// 矩形坐标
        /// </summary>
        public RectInt position;

        /// <summary>
        /// 是否已经被占用
        /// </summary>
        public bool used;

        public Rectangle(int x, int y, int w, int h)
        {
            position = new RectInt(x, y, w, h);
        }

        internal void ReSet(int x, int y, int w, int h)
        {
            position = new RectInt(x, y, w, h);
        }

        public int x
        {
            get => this.position.x;
            set => this.position.x = value;
        }


        /// <summary>
        ///   <para>Top coordinate of the rectangle.</para>
        /// </summary>
        public int y
        {
            get => this.position.y;
            set => this.position.y = value;
        }

        /// <summary>
        ///   <para>Width of the rectangle.</para>
        /// </summary>
        public int width
        {
            get => this.position.width;
            set => this.position.width = value;
        }

        /// <summary>
        ///   <para>Width of the rectangle.</para>
        /// </summary>
        public int height
        {
            get => this.position.height;
            set => this.position.height = value;
        }


        /// <summary>
        ///   <para>Returns true if the given position is within the RectInt.</para>
        /// </summary>
        /// <param name="position">Position to check.</param>
        /// <returns>
        ///   <para>Whether the position is within the RectInt.</para>
        /// </returns>
        public bool Contains(RectInt rect) => rect.x >= this.position.xMin && rect.y >= this.position.yMin &&
                                              rect.x < this.position.xMax && rect.y < this.position.yMax;


        public bool Equals(RectInt other)
        {
            return this.position.Equals(other);
        }

        /// <summary>
        /// 插入矩形
        /// </summary>
        public abstract Rectangle Insert(int w, int h);

        /// <summary>
        /// 移除矩形
        /// </summary>
        public abstract bool Remove(RectInt rect);

        public abstract void Clear();

        public abstract void actualityWH(ref int width, ref int height);
    }
}